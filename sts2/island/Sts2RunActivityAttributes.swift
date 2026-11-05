import Foundation

#if canImport(ActivityKit)
import ActivityKit
#endif

/// The run snapshot the app hands to the Dynamic Island, and the widget extension renders.
///
/// This file is compiled into both targets (the app and `Sts2Island.appex`) instead of living in a
/// shared framework. ActivityKit matches the running activity to the widget by the attributes type
/// name, so the two copies have to stay identical - do not add members to one side only.
@available(iOS 16.1, *)
public struct Sts2RunActivityAttributes: ActivityAttributes {
	public struct ContentState: Codable, Hashable {
		public var hp: Int
		public var maxHp: Int
		public var floor: Int
		public var act: Int
		public var gold: Int
		public var character: String

		/// The character portrait as PNG bytes, ready for `UIImage(data:)`.
		///
		/// It travels inside the state because the widget extension has its own sandbox: it cannot
		/// read Godot's `user://` directory, and an ad-hoc-signed build has no App Group to share
		/// one. The app side keeps it small (132 px long edge, under 48 KB) because every update
		/// archives and ships this state to the widget; `nil` means "show the fallback symbol".
		public var avatar: Data?

		public init(hp: Int, maxHp: Int, floor: Int, act: Int, gold: Int, character: String, avatar: Data? = nil) {
			self.hp = hp
			self.maxHp = maxHp
			self.floor = floor
			self.act = act
			self.gold = gold
			self.character = character
			self.avatar = avatar
		}

		/// 0...1 health fraction, safe when maxHp is momentarily 0 (a fresh or dying run).
		public var hpFraction: Double {
			guard maxHp > 0 else { return 0 }
			return min(1, max(0, Double(hp) / Double(maxHp)))
		}
	}

	/// Stable for one run; a new run gets a new id so the island can tell them apart.
	public var runId: String

	public init(runId: String) {
		self.runId = runId
	}
}
