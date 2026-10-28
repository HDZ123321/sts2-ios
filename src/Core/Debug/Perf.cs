using System;
using System.Diagnostics;
using System.Text;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;

namespace MegaCrit.Sts2.Core.Debug;

/// <summary>
/// Opt-in timing instrumentation, enabled with <c>--perf</c>. It exists so the startup
/// sequence and the steady-state frame loop can be measured on a real device, where no
/// profiler can be attached: everything is written to the normal log.
/// </summary>
public static class Perf
{
	private const int ReportFrames = 600;

	private const double HitchThresholdMs = 33.0;

	private static readonly Stopwatch Clock = Stopwatch.StartNew();

	private static readonly StringBuilder Report = new StringBuilder(256);

	private static long _lastMarkMs;

	private static long _frames;

	private static double _sumMs;

	private static double _maxMs;

	private static int _hitches;

	private static int _gc0;

	private static int _gc1;

	private static int _gc2;

	private static bool _frameWatchRunning;

	/// <summary>True when the run was started with <c>--perf</c>.</summary>
	public static bool Enabled { get; } = CommandLineHelper.HasArg("perf");

	/// <summary>Wall-clock milliseconds since process start, for stall detection.</summary>
	public static long ElapsedMs => Clock.ElapsedMilliseconds;

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
		Log.Info("[Perf] frame watch started", 3);
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
		}
		if (_frames < ReportFrames)
		{
			return;
		}
		int num3 = GC.CollectionCount(0);
		int num4 = GC.CollectionCount(1);
		int num5 = GC.CollectionCount(2);
		Report.Clear();
		Report.Append("[Perf] frames=").Append(_frames)
			.Append(" avg=").Append((_sumMs / (double)_frames).ToString("F2"))
			.Append("ms max=").Append(_maxMs.ToString("F1"))
			.Append("ms hitch=").Append(_hitches)
			.Append(" gc0=").Append(num3 - _gc0)
			.Append(" gc1=").Append(num4 - _gc1)
			.Append(" gc2=").Append(num5 - _gc2)
			.Append(" managed=").Append((GC.GetTotalMemory(false) / 1048576).ToString())
			.Append("MB vram=").Append((Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) / 1048576.0).ToString("F0"))
			.Append("MB");
		Log.Info(Report.ToString(), 3);
		_gc0 = num3;
		_gc1 = num4;
		_gc2 = num5;
		_frames = 0L;
		_sumMs = 0.0;
		_maxMs = 0.0;
		_hitches = 0;
	}
}
