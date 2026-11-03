import ActivityKit
import SwiftUI
import WidgetKit

/// The Dynamic Island and Lock Screen presentation of a Slay the Spire 2 run.
///
/// Compiled into Sts2Island.appex only. The attributes struct next to this file is compiled into
/// both this extension and the app; ActivityKit matches them by type name, so the two copies must
/// stay identical.
@main
struct Sts2IslandWidgets: WidgetBundle {
	var body: some Widget {
		Sts2IslandLiveActivity()
	}
}

private enum IslandPalette {
	static let hpHigh = Color(red: 0.42, green: 0.80, blue: 0.45)
	static let hpMid = Color(red: 0.95, green: 0.76, blue: 0.31)
	static let hpLow = Color(red: 0.90, green: 0.31, blue: 0.28)
	static let gold = Color(red: 0.96, green: 0.82, blue: 0.42)

	static func hp(_ state: Sts2RunActivityAttributes.ContentState) -> Color {
		let fraction = state.hpFraction
		if fraction > 0.6 { return hpHigh }
		if fraction > 0.3 { return hpMid }
		return hpLow
	}
}

struct Sts2IslandLiveActivity: Widget {
	var body: some WidgetConfiguration {
		ActivityConfiguration(for: Sts2RunActivityAttributes.self) { context in
			Sts2IslandLockScreenView(state: context.state)
				.padding(.horizontal, 14)
				.padding(.vertical, 10)
				.activityBackgroundTint(Color(white: 0.08).opacity(0.9))
				.activitySystemActionForegroundColor(.white)
		} dynamicIsland: { context in
			DynamicIsland {
				DynamicIslandExpandedRegion(.leading) {
					HStack(spacing: 5) {
						Image(systemName: "map.fill")
							.font(.caption2)
							.foregroundStyle(IslandPalette.gold)
						Text("A\(context.state.act) · \(context.state.floor)")
							.font(.caption.weight(.semibold))
							.monospacedDigit()
					}
					.padding(.leading, 4)
				}
				DynamicIslandExpandedRegion(.trailing) {
					HStack(spacing: 4) {
						Image(systemName: "heart.fill")
							.font(.caption2)
							.foregroundStyle(IslandPalette.hp(context.state))
						Text("\(context.state.hp)/\(context.state.maxHp)")
							.font(.caption.weight(.bold))
							.monospacedDigit()
					}
					.padding(.trailing, 4)
				}
				DynamicIslandExpandedRegion(.center) {
					ProgressView(value: context.state.hpFraction)
						.progressViewStyle(.linear)
						.tint(IslandPalette.hp(context.state))
						.scaleEffect(x: 1, y: 0.7, anchor: .center)
				}
				DynamicIslandExpandedRegion(.bottom) {
					HStack(spacing: 14) {
						Label(context.state.character.isEmpty ? "Run" : context.state.character, systemImage: "person.fill")
							.labelStyle(.titleAndIcon)
						Label("\(context.state.gold)", systemImage: "circle.fill")
							.labelStyle(.titleAndIcon)
							.foregroundStyle(IslandPalette.gold)
					}
					.font(.caption2)
					.frame(maxWidth: .infinity, alignment: .center)
				}
			} compactLeading: {
				HStack(spacing: 3) {
					Image(systemName: "map.fill")
						.font(.system(size: 10))
						.foregroundStyle(IslandPalette.gold)
					Text("\(context.state.floor)")
						.font(.caption2.weight(.semibold))
						.monospacedDigit()
				}
			} compactTrailing: {
				HStack(spacing: 2) {
					Image(systemName: "heart.fill")
						.font(.system(size: 9))
						.foregroundStyle(IslandPalette.hp(context.state))
					Text("\(context.state.hp)")
						.font(.caption2.weight(.bold))
						.monospacedDigit()
				}
			} minimal: {
				Text("\(context.state.hp)")
					.font(.caption2.weight(.bold))
					.monospacedDigit()
					.foregroundStyle(IslandPalette.hp(context.state))
			}
			.keylineTint(IslandPalette.hp(context.state))
		}
	}
}

struct Sts2IslandLockScreenView: View {
	let state: Sts2RunActivityAttributes.ContentState

	var body: some View {
		HStack(spacing: 12) {
			ZStack {
				Circle()
					.fill(IslandPalette.hp(state).opacity(0.18))
					.frame(width: 38, height: 38)
				Image(systemName: "heart.fill")
					.font(.system(size: 16, weight: .bold))
					.foregroundStyle(IslandPalette.hp(state))
			}
			VStack(alignment: .leading, spacing: 4) {
				HStack(spacing: 6) {
					Text(state.character.isEmpty ? "Slay the Spire 2" : state.character)
						.font(.footnote.weight(.semibold))
					Text("· 第 \(state.act) 幕 第 \(state.floor) 层")
						.font(.caption2)
						.foregroundStyle(.secondary)
				}
				ProgressView(value: state.hpFraction)
					.progressViewStyle(.linear)
					.tint(IslandPalette.hp(state))
					.scaleEffect(x: 1, y: 0.8, anchor: .center)
			}
			VStack(alignment: .trailing, spacing: 2) {
				Text("\(state.hp)/\(state.maxHp)")
					.font(.footnote.weight(.bold))
					.monospacedDigit()
					.foregroundStyle(IslandPalette.hp(state))
				Text("\(state.gold) 金")
					.font(.caption2)
					.monospacedDigit()
					.foregroundStyle(IslandPalette.gold)
			}
		}
	}
}
