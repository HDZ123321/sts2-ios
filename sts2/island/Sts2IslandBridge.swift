import Foundation

#if canImport(ActivityKit)
import ActivityKit
#endif

// The app-side half of the Dynamic Island bridge.
//
// ActivityKit has no C or Objective-C entry point, so this has to be Swift. Rather than export a
// symbol for the NativeAOT assembly to P/Invoke - one untestable hop on this port - the two halves
// talk through a file: DynamicIslandReporter.cs writes user://island_state.json once a second,
// this poller reads it at 0.5 s and starts or updates the Live Activity. A missing or malformed
// file means "no island", never a broken frame.
//
// Entry point: sts2_island_bootstrap(), called from sts2/dummy.cpp at image load and again from
// Godot's apple-embedded plugin hook. It is idempotent.

private let islandTickSeconds: Double = 0.5
private let islandStateFileName = "island_state.json"
private let islandStatusFileName = "island_status.txt"
private let islandStatusLimit = 8000

private final class Sts2IslandPoller {
	static let shared = Sts2IslandPoller()

	private let queue = DispatchQueue(label: "com.placeholder.sts2.island.poll")
	private var timer: DispatchSourceTimer?
	private var started = false
	private var resolvedUserDir: URL?
	private var lastResolvedAt: Date = .distantPast
	private var lastPayloadHash: Int = 0
	private var lastActiveRunId: String?

	func start() {
		queue.async { [self] in
			guard !started else { return }
			started = true
			let source = DispatchSource.makeTimerSource(queue: queue)
			source.schedule(deadline: .now() + 2.0, repeating: islandTickSeconds)
			source.setEventHandler { [weak self] in self?.tick() }
			source.resume()
			timer = source
			status("poller started")
		}
	}

	// MARK: - One tick

	private func tick() {
		guard let dir = userDir() else { return }
		let stateURL = dir.appendingPathComponent(islandStateFileName)
		guard let data = try? Data(contentsOf: stateURL) else {
			// The reporter has not written anything yet, or the file vanished after the app died.
			// Either way there is nothing to show; leave any existing activity alone until the
			// staleness check below decides otherwise.
			return
		}
		let hash = data.hashValue
		guard hash != lastPayloadHash else { return }
		lastPayloadHash = hash
		guard let snapshot = Snapshot(data: data) else {
			status("ignored unparsable state (\(data.count) bytes)")
			return
		}
		if #available(iOS 16.1, *) {
			apply(snapshot)
		}
	}

	// MARK: - Live Activity

	@available(iOS 16.1, *)
	private func apply(_ snapshot: Snapshot) {
		let info = ActivityAuthorizationInfo()
		guard info.areActivitiesEnabled else {
			status("live activities are disabled in Settings")
			return
		}
		guard snapshot.active else {
			endActivity()
			return
		}
		let state = Sts2RunActivityAttributes.ContentState(
			hp: snapshot.hp,
			maxHp: snapshot.maxHp,
			floor: snapshot.floor,
			act: snapshot.act,
			gold: snapshot.gold,
			character: snapshot.character)
		let runId = "run-\(snapshot.act)-\(snapshot.character)"
		if let existing = Activity<Sts2RunActivityAttributes>.activities.first {
			lastActiveRunId = existing.attributes.runId
			Task {
				await existing.update(using: state)
			}
			return
		}
		if lastActiveRunId == nil {
			status("requesting activity hp=\(snapshot.hp)/\(snapshot.maxHp) floor=\(snapshot.floor)")
		}
		do {
			_ = try Activity<Sts2RunActivityAttributes>.request(
				attributes: Sts2RunActivityAttributes(runId: runId),
				contentState: state,
				pushType: nil)
			lastActiveRunId = runId
			status("activity started \(runId)")
		} catch {
			status("request failed: \(error.localizedDescription)")
		}
	}

	@available(iOS 16.1, *)
	private func endActivity() {
		let activities = Activity<Sts2RunActivityAttributes>.activities
		guard !activities.isEmpty else {
			lastActiveRunId = nil
			return
		}
		let state = activities.first?.contentState
		for activity in activities {
			Task {
				await activity.end(using: state, dismissalPolicy: .immediate)
			}
		}
		lastActiveRunId = nil
		status("activity ended")
	}

	// MARK: - Where the reporter writes

	/// Godot's iOS user directory is Library/Application Support/Godot/app_userdata/<project>/.
	/// The project name is baked into the pck, so rather than hard-code it we look for the state
	/// file (or the single app_userdata subdirectory, which is ours) and cache the result.
	private func userDir() -> URL? {
		if let cached = resolvedUserDir, (try? cached.checkResourceIsReachable()) == true {
			let stateFile = cached.appendingPathComponent(islandStateFileName)
			if FileManager.default.fileExists(atPath: stateFile.path) {
				return cached
			}
			if Date().timeIntervalSince(lastResolvedAt) < 30 {
				return cached
			}
		}
		let fm = FileManager.default
		var candidateRoots: [URL] = []
		if let appSupport = fm.urls(for: .applicationSupportDirectory, in: .userDomainMask).first {
			candidateRoots.append(appSupport.appendingPathComponent("Godot/app_userdata", isDirectory: true))
			candidateRoots.append(appSupport)
		}
		if let documents = fm.urls(for: .documentDirectory, in: .userDomainMask).first {
			candidateRoots.append(documents)
		}
		if let caches = fm.urls(for: .cachesDirectory, in: .userDomainMask).first {
			candidateRoots.append(caches)
		}
		for root in candidateRoots {
			if let direct = stateFileIn(root) {
				resolvedUserDir = direct
				lastResolvedAt = Date()
				return direct
			}
			guard let entries = try? fm.contentsOfDirectory(at: root, includingPropertiesForKeys: [.isDirectoryKey], options: [.skipsHiddenFiles]) else {
				continue
			}
			for entry in entries {
				if let found = stateFileIn(entry) {
					resolvedUserDir = found
					lastResolvedAt = Date()
					return found
				}
			}
		}
		// Nothing written yet. Fall back to the single app_userdata subdirectory so that the log
		// file lands next to where the state file will appear.
		if let godotRoot = candidateRoots.first,
		   let entries = try? fm.contentsOfDirectory(at: godotRoot, includingPropertiesForKeys: [.isDirectoryKey], options: [.skipsHiddenFiles]),
		   entries.count == 1 {
			resolvedUserDir = entries[0]
			lastResolvedAt = Date()
			return entries[0]
		}
		resolvedUserDir = nil
		lastResolvedAt = Date()
		return nil
	}

	private func stateFileIn(_ dir: URL) -> URL? {
		let stateFile = dir.appendingPathComponent(islandStateFileName)
		let fm = FileManager.default
		if fm.fileExists(atPath: stateFile.path) {
			return dir
		}
		// The reporter writes logs/ too; accept a directory that already contains Godot's log
		// folder as "ours" so the very first launch still finds the right place.
		if fm.fileExists(atPath: dir.appendingPathComponent("logs").path) {
			return dir
		}
		return nil
	}

	// MARK: - Diagnostics that survive a device without a debugger

	/// One line into island_status.txt next to the state file, so pulling the Godot log directory
	/// off the phone also brings the island's side of the story.
	private func status(_ line: String) {
		guard let dir = resolvedUserDir else { return }
		let url = dir.appendingPathComponent(islandStatusFileName)
		let stamp = ISO8601DateFormatter().string(from: Date())
		var existing = (try? String(contentsOf: url, encoding: .utf8)) ?? ""
		if existing.count > islandStatusLimit {
			existing = String(existing.suffix(islandStatusLimit / 2))
		}
		existing += "[\(stamp)] \(line)\n"
		try? existing.write(to: url, atomically: true, encoding: .utf8)
		NSLog("[Island] %@", line)
	}

	// MARK: - State payload

	private struct Snapshot {
		let active: Bool
		let hp: Int
		let maxHp: Int
		let floor: Int
		let act: Int
		let gold: Int
		let character: String

		init?(data: Data) {
			guard let object = try? JSONSerialization.jsonObject(with: data),
			      let dict = object as? [String: Any] else { return nil }
			active = (dict["active"] as? Bool) ?? false
			hp = (dict["hp"] as? Int) ?? 0
			maxHp = (dict["maxHp"] as? Int) ?? 0
			floor = (dict["floor"] as? Int) ?? 0
			act = (dict["act"] as? Int) ?? 1
			gold = (dict["gold"] as? Int) ?? 0
			character = (dict["character"] as? String) ?? ""
		}
	}
}

@_cdecl("sts2_island_bootstrap")
public func sts2_island_bootstrap() {
	Sts2IslandPoller.shared.start()
}
