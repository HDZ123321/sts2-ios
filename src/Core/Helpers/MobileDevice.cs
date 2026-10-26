using System;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace MegaCrit.Sts2.Core.Helpers;

/// <summary>
/// Classifies the device the game is running on so that mobile-specific tuning can branch on
/// phone vs tablet instead of the single <c>OS.HasFeature("mobile")</c> bucket.
/// Everything here is cached after the first probe; the values never change during a session.
/// </summary>
public static class MobileDevice
{
	/// <summary>
	/// The coarse shape of the device we are running on.
	/// </summary>
	public enum FormFactor
	{
		/// <summary>Not a mobile device, or a mobile device we could not classify.</summary>
		Unknown,
		Phone,
		Tablet
	}

	/// <summary>
	/// Smallest physical edge (in pixels) at or above which an unclassified device is treated as a
	/// tablet. iPad mini is 1488x2266 and iPhone 16 Pro Max is 1320x2868, so the short edge is the
	/// only axis that separates them reliably.
	/// </summary>
	private const int _tabletShortEdgePixels = 1400;

	/// <summary>
	/// Apple's minimum recommended hit target, in points. Controls scale their touch rect towards
	/// this size on mobile.
	/// </summary>
	public const float minimumTouchTargetPoints = 44f;

	private static bool _probed;

	private static FormFactor _formFactor;

	private static Vector2I _screenSize;

	private static float _screenScale = 1f;

	/// <summary>
	/// True when the game runs on a touch-first platform (iOS, Android).
	/// </summary>
	public static bool IsMobile => OS.HasFeature("mobile");

	/// <summary>
	/// True when the only input is a touchscreen. Mobile builds have no pointer device, so hover,
	/// cursor shapes and mouse warping are all meaningless.
	/// </summary>
	public static bool IsTouchOnly => IsMobile;

	/// <summary>
	/// Phone, tablet, or unknown.
	/// </summary>
	public static FormFactor DeviceFormFactor
	{
		get
		{
			Probe();
			return _formFactor;
		}
	}

	/// <summary>
	/// True when running on a phone-shaped device.
	/// </summary>
	public static bool IsPhone => DeviceFormFactor == FormFactor.Phone;

	/// <summary>
	/// True when running on a tablet-shaped device.
	/// </summary>
	public static bool IsTablet => DeviceFormFactor == FormFactor.Tablet;

	/// <summary>
	/// The physical pixel size of the primary screen.
	/// </summary>
	public static Vector2I ScreenSize
	{
		get
		{
			Probe();
			return _screenSize;
		}
	}

	/// <summary>
	/// Physical pixels per point on the primary screen (2.0 or 3.0 on Apple hardware).
	/// </summary>
	public static float ScreenScale
	{
		get
		{
			Probe();
			return _screenScale;
		}
	}

	/// <summary>
	/// Converts a size in points into canvas units at the current content scale, clamped so that
	/// a hit target never shrinks below what the designer authored.
	/// </summary>
	public static Vector2 MinimumTouchTarget(Vector2 currentSize)
	{
		if (!IsMobile)
		{
			return currentSize;
		}
		Rect2 visible = GetCanvasRect();
		float pointsToCanvas = 1f;
		if (ScreenScale > 0f && visible.Size.X > 0f && ScreenSize.X > 0)
		{
			pointsToCanvas = visible.Size.X / ((float)ScreenSize.X / ScreenScale);
		}
		float target = minimumTouchTargetPoints * pointsToCanvas;
		return new Vector2(Mathf.Max(currentSize.X, target), Mathf.Max(currentSize.Y, target));
	}

	/// <summary>
	/// Physical screen pixels per canvas unit. This is the factor text has to be rasterised at to
	/// stay sharp on a retina panel; anything below 1 means the canvas is upscaled and glyphs are
	/// being magnified from a smaller atlas.
	/// </summary>
	public static float CanvasPixelRatio
	{
		get
		{
			if (!IsMobile)
			{
				return 1f;
			}
			Rect2 visible = GetCanvasRect();
			if (visible.Size.X <= 0f || visible.Size.Y <= 0f || ScreenSize.X <= 0 || ScreenSize.Y <= 0)
			{
				return 1f;
			}
			float ratio = Mathf.Max(ScreenSize.X / visible.Size.X, ScreenSize.Y / visible.Size.Y);
			return Mathf.Clamp(ratio, 1f, 2f);
		}
	}

	/// <summary>
	/// The logical canvas rect the game draws into. Falls back to a zero rect when no tree exists
	/// yet, which callers treat as "leave the control alone".
	/// </summary>
	private static Rect2 GetCanvasRect()
	{
		try
		{
			SceneTree tree = Engine.GetMainLoop() as SceneTree;
			Viewport root = tree?.Root;
			if (root == null)
			{
				return default(Rect2);
			}
			return root.GetVisibleRect();
		}
		catch (Exception)
		{
			return default(Rect2);
		}
	}

	private static void Probe()
	{
		if (_probed)
		{
			return;
		}
		_probed = true;
		if (!IsMobile)
		{
			_formFactor = FormFactor.Unknown;
			return;
		}
		try
		{
			_screenSize = DisplayServer.ScreenGetSize(DisplayServer.GetPrimaryScreen());
		}
		catch (Exception ex)
		{
			Log.Warn("[MobileDevice] Could not read the screen size: " + ex.Message);
			_screenSize = Vector2I.Zero;
		}
		try
		{
			_screenScale = DisplayServer.ScreenGetMaxScale();
			if (_screenScale <= 0f)
			{
				_screenScale = 1f;
			}
		}
		catch (Exception ex2)
		{
			Log.Warn("[MobileDevice] Could not read the screen scale: " + ex2.Message);
			_screenScale = 1f;
		}
		string model = OS.GetModelName() ?? string.Empty;
		_formFactor = ClassifyModel(model, _screenSize);
		Log.Info($"[MobileDevice] model={model} screen={_screenSize} scale={_screenScale} form={_formFactor}");
	}

	private static FormFactor ClassifyModel(string model, Vector2I screenSize)
	{
		if (model.StartsWith("iPad", StringComparison.OrdinalIgnoreCase))
		{
			return FormFactor.Tablet;
		}
		if (model.StartsWith("iPhone", StringComparison.OrdinalIgnoreCase) || model.StartsWith("iPod", StringComparison.OrdinalIgnoreCase))
		{
			return FormFactor.Phone;
		}
		if (screenSize.X <= 0 || screenSize.Y <= 0)
		{
			return FormFactor.Unknown;
		}
		int shortEdge = Mathf.Min(screenSize.X, screenSize.Y);
		return (shortEdge >= _tabletShortEdgePixels) ? FormFactor.Tablet : FormFactor.Phone;
	}
}
