using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Platform.Steam;
using MegaCrit.Sts2.Core.Saves.Migrations;
using MegaCrit.Sts2.Core.Settings;
using Steamworks;

namespace MegaCrit.Sts2.Core.Saves.Managers;

public class SettingsSaveManager
{
	public const string settingsSaveFileName = "settings.save";

	private readonly ISaveStore _saveStore;

	private readonly MigrationManager _migrationManager;

	private string SettingsPath => "settings.save";

	public SettingsSave Settings { get; set; }

	public SettingsSaveManager(ISaveStore saveStore, MigrationManager migrationManager)
	{
		_saveStore = saveStore;
		_migrationManager = migrationManager;
	}

	public void SaveSettings()
	{
		Settings.SchemaVersion = _migrationManager.GetLatestVersion<SettingsSave>();
		string content = JsonSerializationUtility.ToJson(Settings);
		_saveStore.WriteFile(SettingsPath, content);
	}

	public ReadSaveResult<SettingsSave> LoadSettings()
	{
		ReadSaveResult<SettingsSave> readSaveResult = _migrationManager.LoadSave<SettingsSave>(SettingsPath);
		if (!readSaveResult.Success || readSaveResult.SaveData == null)
		{
			Settings = _migrationManager.CreateNewSave<SettingsSave>();
			ApplyPlatformDefaults(Settings);
			SaveSettings();
		}
		else
		{
			Settings = readSaveResult.SaveData;
		}
		return readSaveResult;
	}

	/// <summary>
	/// Applies platform-specific default settings when a settings file is recreated.
	/// </summary>
	/// <param name="settings">The settings object to apply defaults to</param>
	private void ApplyPlatformDefaults(SettingsSave settings)
	{
		if (SteamInitializer.Initialized && SteamUtils.IsSteamRunningOnSteamDeck())
		{
			settings.Fullscreen = true;
			settings.WindowPosition = new Vector2I(0, 0);
		}
		if (MobileDevice.IsMobile)
		{
			// A phone only ever runs fullscreen, so the saved window geometry and the aspect ratio
			// picked for a desktop monitor are both meaningless here.
			settings.Fullscreen = true;
			settings.WindowPosition = new Vector2I(0, 0);
			settings.AspectRatioSetting = AspectRatioSetting.Auto;
			settings.LimitFpsInBackground = true;
			if (MobileDevice.IsPhone)
			{
				// Phones get a tighter frame budget than tablets: 60 fps is already the panel limit on
				// most of them, and anything above it just drains the battery.
				settings.FpsLimit = 60;
				settings.Msaa = 2;
			}
		}
	}
}
