using System;
using System.Globalization;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace MegaCrit.Sts2.Core.Helpers;

/// <summary>
/// The phone half of the graphics settings.
/// <para>
/// Two unrelated jobs live here. <see cref="M:MegaCrit.Sts2.Core.Helpers.MobileGraphicsProfile.ApplyOnce" />
/// rewrites the settings of a touch build exactly once, because
/// <see cref="M:MegaCrit.Sts2.Core.Saves.Managers.SettingsSaveManager.ApplyPlatformDefaults(MegaCrit.Sts2.Core.Saves.SettingsSave)" />
/// only runs when a save file is created: every install that already had a settings.save kept
/// MSAA 2x on a 2D renderer, which doubles the sample bandwidth for no visible gain and is the
/// single most expensive thing this client was doing per frame.
/// </para>
/// <para>
/// <see cref="M:MegaCrit.Sts2.Core.Helpers.MobileGraphicsProfile.ApplyRenderScale(Godot.Window,MegaCrit.Sts2.Core.Saves.SettingsSave)" />
/// is the opt-in second half: it can render the frame at the 1920x1080 design canvas instead of
/// the phone's native pixel grid. Godot's <c>canvas_items</c> stretch mode - which this project
/// uses - always rasterises at the full physical resolution no matter what ContentScaleSize says
/// (see <c>Window::_update_viewport_size</c>: <c>final_size = screen_size</c>), so shrinking the
/// drawable requires switching to <c>viewport</c> stretch, which is why this is a mode switch and
/// not a scale factor.
/// </para>
/// </summary>
public static class MobileGraphicsProfile
{
	/// <summary>Marker written once the settings rewrite below has run.</summary>
	public const string MarkerPath = "user://mobile_profile_v1.txt";

	/// <summary>Optional file that asks for the design-canvas render path. Absent means native.</summary>
	public const string RenderScalePath = "user://render_scale.txt";

	/// <summary>Design canvas the scenes are authored against (matches project.godot).</summary>
	public const int DesignWidth = 1920;

	/// <summary>Design canvas the scenes are authored against (matches project.godot).</summary>
	public const int DesignHeight = 1080;

	/// <summary>Frame rate a touch build is allowed to aim for.</summary>
	public const int MaxFrameRate = 60;

	/// <summary>Frame rate below which the saved value is raised back up - the game is not playable slower.</summary>
	public const int MinFrameRate = 30;

	private const string LogPrefix = "[Graphics]";

	private static bool _appliedOnce;

	private static bool _renderScaleRead;

	private static bool _designCanvasRendering;

	/// <summary>
	/// True when this process renders at the design canvas instead of the native pixel grid.
	/// </summary>
	public static bool DesignCanvasRendering => MobileDevice.IsMobile && _designCanvasRendering;

	/// <summary>
	/// Visiting the settings once, on the first launch of a build that ships this class: turn MSAA
	/// off, pin vsync to the plain mode and pull the frame rate into a sane range. Writes
	/// <see cref="F:MegaCrit.Sts2.Core.Helpers.MobileGraphicsProfile.MarkerPath" /> whether or not
	/// anything changed so a player who deliberately re-enables MSAA keeps it.
	/// </summary>
	/// <returns>True when the saved settings were changed and need persisting.</returns>
	public static bool ApplyOnce(SettingsSave settings, Action persist)
	{
		if (settings == null || _appliedOnce)
		{
			return false;
		}
		_appliedOnce = true;
		if (!MobileDevice.IsMobile)
		{
			return false;
		}
		if (FileAccess.FileExists(MarkerPath))
		{
			Log.Info($"{LogPrefix} mobile profile already applied", 3);
			return false;
		}
		bool changed = false;
		if (settings.Msaa != 0)
		{
			Log.Info($"{LogPrefix} MSAA {settings.Msaa}x -> off (2D gains nothing from it, it doubles sample bandwidth)", 3);
			settings.Msaa = 0;
			changed = true;
		}
		if (settings.VSync != VSyncType.On)
		{
			Log.Info($"{LogPrefix} vsync {settings.VSync} -> On", 3);
			settings.VSync = VSyncType.On;
			changed = true;
		}
		int frameRate = ClampFrameRate(settings.FpsLimit);
		if (frameRate != settings.FpsLimit)
		{
			Log.Info($"{LogPrefix} frame limit {settings.FpsLimit} -> {frameRate}", 3);
			settings.FpsLimit = frameRate;
			changed = true;
		}
		WriteMarker();
		return changed;
	}

	/// <summary>
	/// Pulls a requested frame rate into what a phone should actually run at: anything above
	/// <see cref="F:MegaCrit.Sts2.Core.Helpers.MobileGraphicsProfile.MaxFrameRate" /> (or unlimited)
	/// becomes 60, because the paging setting offers up to 500 and ProMotion will happily burn the
	/// battery trying.
	/// </summary>
	public static int ClampFrameRate(int frameRate)
	{
		if (!MobileDevice.IsMobile)
		{
			return frameRate;
		}
		if (frameRate <= 0 || frameRate > MaxFrameRate)
		{
			return MaxFrameRate;
		}
		if (frameRate < MinFrameRate)
		{
			return MinFrameRate;
		}
		return frameRate;
	}

	/// <summary>
	/// Switches the window between native pixels (the default, <c>canvas_items</c>) and the design
	/// canvas (<c>viewport</c> at 1920x1080). Both keep the same canvas coordinate space, so layout
	/// and touch targets do not move; only the size of the drawable changes.
	/// </summary>
	public static void ApplyRenderScale(Window window, SettingsSave settings)
	{
		if (window == null || !MobileDevice.IsMobile)
		{
			_designCanvasRendering = false;
			return;
		}
		bool wantDesignCanvas = ReadDesignCanvasRequest();
		_designCanvasRendering = wantDesignCanvas;
		if (!wantDesignCanvas)
		{
			window.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
			Log.Info($"{LogPrefix} render scale: native {DisplayServer.WindowGetSize()} (canvas_items)", 3);
			return;
		}
		window.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
		window.ContentScaleFactor = 1f;
		window.ContentScaleSize = new Vector2I(1920, 1080);
		window.ContentScaleMode = Window.ContentScaleModeEnum.Viewport;
		Log.Info($"{LogPrefix} render scale: design canvas {window.ContentScaleSize} upscaled to {DisplayServer.WindowGetSize()} (viewport)", 3);
	}

	/// <summary>Writes the render-scale file. <paramref name="enable" /> false removes it.</summary>
	public static void SetDesignCanvasRendering(bool enable)
	{
		try
		{
			if (!enable)
			{
				if (FileAccess.FileExists(RenderScalePath))
				{
					DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(RenderScalePath));
				}
				_designCanvasRendering = false;
				_renderScaleRead = true;
				return;
			}
			using FileAccess file = FileAccess.Open(RenderScalePath, FileAccess.ModeFlags.Write);
			file?.StoreLine("design");
			_designCanvasRendering = true;
			_renderScaleRead = true;
			Log.Info($"{LogPrefix} wrote {RenderScalePath}; restart to render at the design canvas", 3);
		}
		catch (Exception ex)
		{
			Log.Warn($"{LogPrefix} cannot write {RenderScalePath}: {ex.Message}");
		}
	}

	private static bool ReadDesignCanvasRequest()
	{
		if (_renderScaleRead)
		{
			return _designCanvasRendering;
		}
		_renderScaleRead = true;
		_designCanvasRendering = false;
		string raw = ReadRenderScaleFile();
		if (string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}
		raw = raw.Trim();
		if (raw.Equals("design", StringComparison.OrdinalIgnoreCase) || raw.Equals("canvas", StringComparison.OrdinalIgnoreCase))
		{
			_designCanvasRendering = true;
			return true;
		}
		if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
		{
			_designCanvasRendering = value > 0f && value < 0.999f;
			if (_designCanvasRendering)
			{
				Log.Info($"{LogPrefix} {RenderScalePath} says {value.ToString(CultureInfo.InvariantCulture)}, treating it as the design canvas", 3);
			}
			return _designCanvasRendering;
		}
		Log.Warn($"{LogPrefix} ignoring unreadable {RenderScalePath} value '{raw}'");
		return false;
	}

	private static string ReadRenderScaleFile()
	{
		try
		{
			if (!FileAccess.FileExists(RenderScalePath))
			{
				return null;
			}
			using FileAccess file = FileAccess.Open(RenderScalePath, FileAccess.ModeFlags.Read);
			return file?.GetLine();
		}
		catch (Exception ex)
		{
			Log.Warn($"{LogPrefix} cannot read {RenderScalePath}: {ex.Message}");
			return null;
		}
	}

	private static void WriteMarker()
	{
		try
		{
			using FileAccess file = FileAccess.Open(MarkerPath, FileAccess.ModeFlags.Write);
			file?.StoreLine($"v1 {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
			Log.Info($"{LogPrefix} mobile profile marked in {MarkerPath}", 3);
		}
		catch (Exception ex)
		{
			Log.Warn($"{LogPrefix} cannot write {MarkerPath}: {ex.Message}");
		}
	}
}
