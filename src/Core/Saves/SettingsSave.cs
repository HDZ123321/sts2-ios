using System.Collections.Generic;
using System.Text.Json.Serialization;
using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Settings;

namespace MegaCrit.Sts2.Core.Saves;

/// <summary>
/// This class serves as a schema for the Settings save file.
/// It's used for serializing and deserializing data in the options screen.
/// This file is stored at the account level, not the profile level, and is NOT to be synced across devices/platforms.
/// See <see cref="T:MegaCrit.Sts2.Core.Saves.PrefsSave" /> for the profile-level options displayed in the options screen.
/// </summary>
public class SettingsSave : ISaveSchema
{
	/// <summary>
	/// The schema version of this save.
	/// </summary>
	[JsonPropertyName("schema_version")]
	public int SchemaVersion { get; set; }

	[JsonPropertyName("fps_limit")]
	public int FpsLimit { get; set; } = 60;

	[JsonPropertyName("language")]
	public string? Language { get; set; }

	[JsonPropertyName("window_position")]
	public Vector2I WindowPosition { get; set; } = new Vector2I(-1, -1);

	[JsonPropertyName("window_size")]
	public Vector2I WindowSize { get; set; } = new Vector2I(1920, 1080);

	[JsonPropertyName("fullscreen")]
	public bool Fullscreen { get; set; } = true;

	[JsonPropertyName("aspect_ratio")]
	public AspectRatioSetting AspectRatioSetting { get; set; } = DefaultAspectRatioSetting;

	/// <summary>
	/// The aspect ratio a fresh settings file starts with.
	/// On desktop the game ships a fixed 16:9 canvas, which is what the art was authored against.
	/// Handheld displays are almost never 16:9 (iPhone 16 Pro is 2796x1290, about 2.167:1), so a fixed
	/// 16:9 canvas would be letterboxed with black bars on the left and right. Auto lets the canvas follow
	/// the device instead, which is what every other mobile build of a desktop game does.
	/// </summary>
	public static AspectRatioSetting DefaultAspectRatioSetting => OS.HasFeature("mobile") ? AspectRatioSetting.Auto : AspectRatioSetting.SixteenByNine;

	[JsonPropertyName("target_display")]
	public int TargetDisplay { get; set; } = -1;

	[JsonPropertyName("resize_windows")]
	public bool ResizeWindows { get; set; } = true;

	[JsonPropertyName("vsync")]
	public VSyncType VSync { get; set; } = VSyncType.Adaptive;

	[JsonPropertyName("msaa")]
	public int Msaa { get; set; } = 2;

	[JsonPropertyName("volume_bgm")]
	public float VolumeBgm { get; set; } = 0.5f;

	[JsonPropertyName("volume_master")]
	public float VolumeMaster { get; set; } = 0.5f;

	[JsonPropertyName("volume_sfx")]
	public float VolumeSfx { get; set; } = 0.5f;

	[JsonPropertyName("volume_ambience")]
	public float VolumeAmbience { get; set; } = 0.5f;

	[JsonPropertyName("mod_settings")]
	public ModSettings? ModSettings { get; set; }

	[JsonPropertyName("skip_intro_logo")]
	public bool SkipIntroLogo { get; set; }

	[JsonPropertyName("keyboard_mapping")]
	public Dictionary<string, string> KeyboardMapping { get; set; } = new Dictionary<string, string>();

	[JsonPropertyName("controller_mapping_type")]
	public ControllerMappingType ControllerMappingType { get; set; }

	[JsonPropertyName("controller_mapping")]
	public Dictionary<string, string> ControllerMapping { get; set; } = new Dictionary<string, string>();

	[JsonPropertyName("limit_fps_in_background")]
	public bool LimitFpsInBackground { get; set; } = true;

	[JsonPropertyName("full_console")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool FullConsole { get; set; }

	[JsonPropertyName("seen_ea_disclaimer")]
	public bool SeenEaDisclaimer { get; set; }
}
