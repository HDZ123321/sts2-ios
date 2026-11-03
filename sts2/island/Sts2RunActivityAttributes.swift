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

		public init(hp: Int, maxHp: Int, floor: Int, act: Int, gold: Int, character: String) {
			self.hp = hp
			self.maxHp = maxHp
			self.floor = floor
			self.act = act
			self.gold = gold
			self.character = character
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
