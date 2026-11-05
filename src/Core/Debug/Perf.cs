using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace MegaCrit.Sts2.Core.Debug;

/// <summary>
/// Opt-in timing instrumentation, enabled with <c>--perf</c>. It exists so the startup
/// sequence and the steady-state frame loop can be measured on a real device, where no
/// profiler can be attached: everything is written to the normal log.
/// Mobile builds turn it on by themselves (pass <c>--noperf</c> to silence them), because a
/// phone is exactly the machine we can never attach a profiler to and the frame report is the
/// only way to tell a GPU-bound frame from a CPU-bound one after the fact.
/// </summary>
public static class Perf
{
	private const double HitchThresholdMs = 33.0;

	/// <summary>
	/// Frames per report line when the run was started with <c>--perf</c> (10s at 60fps).
	/// </summary>
	private const int ExplicitReportFrames = 600;

	/// <summary>
	/// Frames per report line when instrumentation turned itself on for a mobile build. A phone
	/// log is read hours later, so a sparser line is enough and costs less to write (30s at 60fps).
	/// </summary>
	private const int MobileReportFrames = 1800;

	/// <summary>
	/// How many individual hitches are spelled out per report window. A long stall usually produces
	/// a burst of them and the log is read off a phone, so keep the detail bounded; the window's
	/// <c>hitch=</c> counter still carries the true total.
	/// </summary>
	private const int MaxHitchLines = 12;

	private static readonly Stopwatch Clock = Stopwatch.StartNew();

	private static readonly StringBuilder Report = new StringBuilder(384);

	private static readonly StringBuilder Hitch = new StringBuilder(256);

	private static long _lastMarkMs;

	private static long _frames;

	private static double _sumMs;

	private static double _maxMs;

	private static int _hitches;

	private static int _hitchLines;

	private static int _gc0;

	private static int _gc1;

	private static int _gc2;

	private static bool _frameWatchRunning;

	private static bool? _enabled;

	private static int _reportFrames;

	/// <summary>
	/// What the game is currently doing, as a short tag set by the code that owns the work
	/// ("combat.preload", "combat.create-room", ...). A frame that runs long logs this tag, which is
	/// the only way to attribute a stall to a phase after the fact: the frame report alone says a
	/// hitch happened but not which piece of the game caused it.
	/// </summary>
	private static string _phase = "boot";

	/// <summary>Wall time per closed phase, see <see cref="ReportPhases" />.</summary>
	private static readonly List<(string phase, long milliseconds)> _phaseTimings = new List<(string phase, long milliseconds)>();

	private static long _phaseStart;

	/// <summary>
	/// True when the run was started with <c>--perf</c>, or when this is a mobile build that was
	/// not explicitly silenced with <c>--noperf</c>.
	/// </summary>
	public static bool Enabled => _enabled ?? ResolveEnabled();

	/// <summary>Wall-clock milliseconds since process start, for stall detection.</summary>
	public static long ElapsedMs => Clock.ElapsedMilliseconds;

	private static int ReportFrames
	{
		get
		{
			if (_reportFrames <= 0)
			{
				_reportFrames = (CommandLineHelper.HasArg("perf") ? 600 : 1800);
			}
			return _reportFrames;
		}
	}

	private static bool ResolveEnabled()
	{
		if (CommandLineHelper.HasArg("noperf"))
		{
			_enabled = false;
			return false;
		}
		bool flag = CommandLineHelper.HasArg("perf") || MobileDevice.IsMobile;
		_enabled = flag;
		return flag;
	}

	/// <summary>
	/// Records the time spent since the previous mark, so a sequence of calls reads as a
	/// breakdown of the phase each one closes.
	/// </summary>
	public static void Mark(string phase)
	{
		if (!Enabled)
		{
			return;
		}
		long elapsedMilliseconds = Clock.ElapsedMilliseconds;
		Log.Info($"[Perf] {phase}: {elapsedMilliseconds - _lastMarkMs}ms (t={elapsedMilliseconds}ms)", 3);
		_lastMarkMs = elapsedMilliseconds;
	}

	/// <summary>
	/// Names the work about to happen, so a long frame inside it is attributable. Costs one string
	/// reference assignment; pass a literal, never an interpolated string.
	/// </summary>
	/// <summary>
	/// Marks the start of a labelled piece of work. Whatever phase was current is closed and
	/// its wall time is kept, so <see cref="ReportPhases" /> can print the breakdown of a
	/// sequence - the frame report only says a frame was long, this says where the time went
	/// even when no single frame crosses the hitch threshold.
	/// </summary>
	public static void Phase(string phase)
	{
		long now = ElapsedMs;
		if (_phaseStart != 0 && _phase != null)
		{
			_phaseTimings.Add((_phase, now - _phaseStart));
		}
		_phase = phase;
		_phaseStart = now;
	}

	/// <summary>
	/// Closes the current phase and writes one line with every phase recorded since the last
	/// call, then clears them. Cheap: it only runs at the end of a sequence like entering a room.
	/// </summary>
	public static void ReportPhases(string label)
	{
		if (!Enabled)
		{
			_phaseTimings.Clear();
			_phaseStart = 0L;
			return;
		}
		if (_phaseStart != 0 && _phase != null)
		{
			_phaseTimings.Add((_phase, ElapsedMs - _phaseStart));
		}
		_phaseStart = 0L;
		if (_phaseTimings.Count == 0)
		{
			return;
		}
		StringBuilder builder = new StringBuilder();
		builder.Append("[Perf] ").Append(label).Append(" total=").Append(_phaseTimings.Sum((ValueTuple<string, long> p) => p.Item2)).Append("ms");
		foreach ((string phase, long milliseconds) in _phaseTimings)
		{
			builder.Append(' ').Append(phase).Append('=').Append(milliseconds).Append("ms");
		}
		Log.Info(builder.ToString(), 3);
		_phaseTimings.Clear();
	}

	/// <summary>The label a hitch in the current frame would be reported under.</summary>
	public static string CurrentPhase => _phase;

	/// <summary>
	/// Labels the current frame, then puts the previous label back once that frame has
	/// been reported. The restoration deliberately happens a frame later: a hitch is
	/// reported at the end of the frame it happened in, so restoring immediately would
	/// erase the label before anything reads it.
	/// </summary>
	public static void PhaseForThisFrame(Node host, string phase)
	{
		string previous = _phase;
		_phase = phase;
		TaskHelper.RunSafely(RestorePhase(host, previous));
	}

	private static async System.Threading.Tasks.Task RestorePhase(Node host, string previous)
	{
		await host.AwaitProcessFrame();
		_phase = previous;
	}

	/// <summary>Starts the per-frame sampler. Safe to call twice.</summary>
	public static void StartFrameWatch()
	{
		if (!Enabled || _frameWatchRunning)
		{
			return;
		}
		if (Engine.GetMainLoop() is not SceneTree tree)
		{
			Log.Warn("[Perf] no scene tree; frame watch not started");
			return;
		}
		_frameWatchRunning = true;
		_gc0 = GC.CollectionCount(0);
		_gc1 = GC.CollectionCount(1);
		_gc2 = GC.CollectionCount(2);
		tree.ProcessFrame += OnFrame;
		Log.Info($"[Perf] frame watch started (report every {ReportFrames} frames)", 3);
	}

	private static void OnFrame()
	{
		double num = Engine.GetMainLoop() is SceneTree sceneTree ? sceneTree.Root.GetProcessDeltaTime() : 0.0;
		double num2 = num * 1000.0;
		_frames++;
		_sumMs += num2;
		if (num2 > _maxMs)
		{
			_maxMs = num2;
		}
		if (num2 > HitchThresholdMs)
		{
			_hitches++;
			LogHitch(num2);
		}
		if (_frames < ReportFrames)
		{
			return;
		}
		int num3 = GC.CollectionCount(0);
		int num4 = GC.CollectionCount(1);
		int num5 = GC.CollectionCount(2);
		double num6 = _sumMs / (double)_frames;
		Report.Clear();
		Report.Append("[Perf] frames=").Append(_frames)
			.Append(" avg=").Append(num6.ToString("F2"))
			.Append("ms fps=").Append((num6 > 0.0) ? (1000.0 / num6).ToString("F1") : "0")
			.Append(" max=").Append(_maxMs.ToString("F1"))
			.Append("ms hitch=").Append(_hitches)
			.Append(" proc=").Append((GetMonitor(Performance.Monitor.TimeProcess) * 1000.0).ToString("F2"))
			.Append("ms phys=").Append((GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000.0).ToString("F2"))
			.Append("ms draw=").Append(GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame).ToString("F0"))
			.Append(" obj=").Append(GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame).ToString("F0"))
			.Append(" nodes=").Append(GetMonitor(Performance.Monitor.ObjectNodeCount).ToString("F0"))
			.Append(" gc0=").Append(num3 - _gc0)
			.Append(" gc1=").Append(num4 - _gc1)
			.Append(" gc2=").Append(num5 - _gc2)
			.Append(" managed=").Append((GC.GetTotalMemory(false) / 1048576).ToString())
			.Append("MB vram=").Append((GetMonitor(Performance.Monitor.RenderVideoMemUsed) / 1048576.0).ToString("F0"))
			.Append("MB");
		Log.Info(Report.ToString(), 3);
		_gc0 = num3;
		_gc1 = num4;
		_gc2 = num5;
		_frames = 0L;
		_sumMs = 0.0;
		_maxMs = 0.0;
		_hitches = 0;
		_hitchLines = 0;
	}

	/// <summary>
	/// One line per long frame, naming what was running and how loaded the frame was.
	/// <c>proc</c> is the engine's own process time for the frame just ended, so a GPU stall shows up
	/// as a big frame with a small <c>proc</c>, while a CPU stall shows both large.
	/// </summary>
	private static void LogHitch(double frameMs)
	{
		if (_hitchLines >= MaxHitchLines)
		{
			return;
		}
		_hitchLines++;
		string scene = "?";
		try
		{
			scene = Engine.GetMainLoop() is SceneTree tree ? tree.CurrentScene?.Name ?? "-" : "-";
		}
		catch (Exception)
		{
			// A freed scene during a room swap is not worth losing the line over.
		}
		Hitch.Clear();
		Hitch.Append("[Perf] hitch ")
			.Append(frameMs.ToString("F1"))
			.Append("ms phase=").Append(_phase)
			.Append(" scene=").Append(scene)
			.Append(" proc=").Append((GetMonitor(Performance.Monitor.TimeProcess) * 1000.0).ToString("F1"))
			.Append("ms nodes=").Append(GetMonitor(Performance.Monitor.ObjectNodeCount).ToString("F0"))
			.Append(" draw=").Append(GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame).ToString("F0"))
			.Append(" vram=").Append((GetMonitor(Performance.Monitor.RenderVideoMemUsed) / 1048576.0).ToString("F0"))
			.Append("MB managed=").Append((GC.GetTotalMemory(false) / 1048576).ToString())
			.Append("MB");
		Log.Info(Hitch.ToString(), 3);
	}

	/// <summary>
	/// Reads one engine monitor, returning 0 instead of throwing when the backend does not expose it
	/// (the mobile driver leaves a few of the render monitors unset).
	/// </summary>
	private static double GetMonitor(Performance.Monitor monitor)
	{
		try
		{
			return Performance.GetMonitor(monitor);
		}
		catch (Exception)
		{
			return 0.0;
		}
	}
}
