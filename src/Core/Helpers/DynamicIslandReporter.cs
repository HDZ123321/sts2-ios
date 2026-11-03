using System;
using System.Globalization;
using System.IO;
using System.Text;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Helpers;

/// <summary>
/// Publishes the current run to the Dynamic Island through a file the native side polls.
/// <para>
/// The island itself is a Live Activity, which only Swift can drive (ActivityKit has no C or
/// Objective-C entry point). Calling into it directly would mean a P/Invoke into the app
/// executable from a NativeAOT image, and that symbol lookup is the one thing about this bridge
/// that cannot be tested off-device. So the two halves are decoupled instead: this writes a
/// 100-byte JSON every second into <c>user://island_state.json</c>, and
/// <c>sts2/island/Sts2IslandBridge.swift</c> watches that file and starts or updates the
/// activity. A failed write costs the island, never the frame.
/// </para>
/// <para>
/// One write per second, and only when the payload actually changed - idle cost is a formatted
/// string comparison, no allocation, no file IO.
/// </para>
/// </summary>
public static class DynamicIslandReporter
{
	private const string StateFileName = "island_state.json";

	/// <summary>How often the run is re-read. The island is not a HUD; once a second is plenty.</summary>
	private const ulong PollIntervalMs = 1000;

	private static readonly StringBuilder Payload = new StringBuilder(192);

	private static bool _started;

	private static bool _disabled;

	private static ulong _nextPollMs;

	private static string _lastPayload = string.Empty;

	private static ulong _sequence;

	/// <summary>
	/// Starts the one-second poll. Safe to call more than once, and does nothing off mobile.
	/// </summary>
	public static void Start(SceneTree tree)
	{
		if (_started || _disabled)
		{
			return;
		}
		if (!MobileDevice.IsMobile)
		{
			_disabled = true;
			return;
		}
		_started = true;
		tree.ProcessFrame += OnFrame;
		Log.Info($"[Island] reporting to {StatePath()}");
	}

	private static void OnFrame()
	{
		ulong ticksMsec = Time.GetTicksMsec();
		if (ticksMsec < _nextPollMs)
		{
			return;
		}
		_nextPollMs = ticksMsec + PollIntervalMs;
		try
		{
			Publish();
		}
		catch (Exception ex)
		{
			_disabled = true;
			Log.Error($"[Island] reporting stopped: {ex}");
		}
	}

	private static void Publish()
	{
		Build();
		string text = Payload.ToString();
		if (text == _lastPayload)
		{
			return;
		}
		File.WriteAllText(StatePath(), text);
		_lastPayload = text;
	}

	/// <summary>
	/// Formats one state line. The schema is fixed and tiny on purpose: the reader is Swift
	/// without a model type, it just checks a few keys.
	/// </summary>
	private static void Build()
	{
		RunState? runState = RunManager.Instance.IsInProgress ? RunManager.Instance.DebugOnlyGetState() : null;
		Player? me = runState?.Players.Count > 0 ? LocalContext.GetMe(runState) : null;
		_sequence++;
		Payload.Clear();
		Payload.Append("{\"seq\":").Append(_sequence);
		if (runState == null || me == null)
		{
			Payload.Append(",\"active\":false}");
			return;
		}
		Payload.Append(",\"active\":true")
			.Append(",\"hp\":").Append(me.Creature.CurrentHp.ToString(CultureInfo.InvariantCulture))
			.Append(",\"maxHp\":").Append(me.Creature.MaxHp.ToString(CultureInfo.InvariantCulture))
			.Append(",\"floor\":").Append(runState.ActFloor.ToString(CultureInfo.InvariantCulture))
			.Append(",\"act\":").Append((runState.CurrentActIndex + 1).ToString(CultureInfo.InvariantCulture))
			.Append(",\"gold\":").Append(me.Gold.ToString(CultureInfo.InvariantCulture))
			.Append(",\"character\":").Append(Quote(CharacterTitle(me)))
			.Append('}');
	}

	private static string CharacterTitle(Player me)
	{
		try
		{
			return me.Character.Title.GetFormattedText();
		}
		catch (Exception)
		{
			return string.Empty;
		}
	}

	/// <summary>
	/// Minimal JSON string quoting. Character titles are localized text, so they can contain
	/// anything a translation table can - quotes included.
	/// </summary>
	private static string Quote(string value)
	{
		StringBuilder builder = new StringBuilder(value.Length + 2);
		builder.Append('"');
		foreach (char c in value)
		{
			switch (c)
			{
			case '"':
				builder.Append("\\\"");
				break;
			case '\\':
				builder.Append("\\\\");
				break;
			case '\n':
				builder.Append("\\n");
				break;
			case '\r':
				builder.Append("\\r");
				break;
			case '\t':
				builder.Append("\\t");
				break;
			default:
				if (c < ' ')
				{
					builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
				}
				else
				{
					builder.Append(c);
				}
				break;
			}
		}
		builder.Append('"');
		return builder.ToString();
	}

	private static string StatePath()
	{
		return Path.Combine(OS.GetUserDataDir(), StateFileName);
	}
}
