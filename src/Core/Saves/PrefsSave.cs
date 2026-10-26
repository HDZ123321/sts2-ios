using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Settings;

namespace MegaCrit.Sts2.Core.Saves;

/// <summary>
/// This class serves as a schema for the Preferences save file.
/// It's used for serializing and deserializing data in the options screen.
/// This file is synced across devices/platforms.
/// It is also scoped per-profile.
/// </summary>
public class PrefsSave : ISaveSchema
{
	/// <summary>
	/// The schema version of this save.
	/// </summary>
	[JsonPropertyName("schema_version")]
	public int SchemaVersion { get; set; }

	[JsonPropertyName("fast_mode")]
	public FastModeType FastMode { get; set; } = FastModeType.Normal;

	[JsonPropertyName("phobia_mode")]
	public bool PhobiaMode { get; set; }

	[JsonPropertyName("screenshake")]
	public int ScreenShakeOptionIndex { get; set; } = 2;

	[JsonPropertyName("show_run_timer")]
	public bool ShowRunTimer { get; set; }

	[JsonPropertyName("show_card_indices")]
	public bool ShowCardIndices { get; set; }

	[JsonPropertyName("upload_data")]
	public bool UploadData { get; set; } = true;

	[JsonPropertyName("mute_in_background")]
	public bool MuteInBackground { get; set; } = true;

	[JsonPropertyName("long_press")]
	public bool IsLongPressEnabled { get; set; } = DefaultIsLongPressEnabled;

	/// <summary>
	/// What <see cref="IsLongPressEnabled" /> should be before the player ever touches the setting.
	/// A touchscreen has no hover, so a stray tap on the end turn button ends the turn outright;
	/// requiring a press-and-hold makes that deliberate. Desktop keeps the instant click because a
	/// mouse is precise enough already. Kept as a static so the prefs file stays a pure data schema
	/// and the value is never serialised.
	/// </summary>
	public static bool DefaultIsLongPressEnabled => MobileDevice.IsTouchOnly;

	[JsonPropertyName("text_effects_enabled")]
	public bool TextEffectsEnabled { get; set; } = true;

	[JsonPropertyName("show_mp_drawings")]
	public bool ShowMultiplayerDrawings { get; set; } = true;
}
