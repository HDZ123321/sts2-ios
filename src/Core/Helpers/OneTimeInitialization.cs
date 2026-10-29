using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;
using SmartFormat.Extensions;

namespace MegaCrit.Sts2.Core.Helpers;

/// <summary>
/// Contains methods that are called as part of initialization.
/// The methods are called both in normal startup and in test mode, so only call things that need to be done in both.
/// </summary>
public static class OneTimeInitialization
{
	private enum State
	{
		None,
		VeryEarly,
		Essential,
		Done
	}

	private static AtlasResourceLoader? _atlasResourceLoader;

	private static State _state;

	public static ReadSaveResult<SettingsSave> SettingsReadResult { get; private set; }

	/// <summary>
	/// Initializes stuff that has to happen very early.
	/// This gets called in both tests and in real games!! Only do stuff that needs to happen in both here.
	/// </summary>
	public static async Task ExecuteVeryEarly()
	{
		if (_state != State.None)
		{
			Log.Error($"Tried to call OneTimeInitialization.ExecuteVeryEarly in state {_state}!");
			return;
		}
		_state = State.VeryEarly;
		Perf.Mark("process start");
		BakedContent.Mount();
		RegentCardArt.Mount();
		if (TestMode.IsOn)
		{
			SettingsReadResult = SaveManager.Instance.InitSettingsDataForTest();
		}
		else
		{
			SettingsReadResult = SaveManager.Instance.InitSettingsData();
		}
		Perf.Mark("settings load");
		await ModManager.Initialize(new ModManagerFileIo(), SaveManager.Instance.SettingsSave.ModSettings, ReleaseInfoManager.Instance.SemVer);
		Perf.Mark("mod manager");
		UserDataPathProvider.IsRunningModded = ModManager.IsRunningModded();
		SentryService.DisableGdExtensionIfModded();
	}

	/// <summary>
	/// Essential initialization needed before main menu can display.
	/// Loads only ui_atlas and core systems (settings, mods, localization, model DB init).
	/// </summary>
	public static void ExecuteEssential()
	{
		if (_state != State.VeryEarly)
		{
			Log.Error($"Tried to call OneTimeInitialization.ExecuteEssential in state {_state}!");
			return;
		}
		_state = State.Essential;
		_atlasResourceLoader = new AtlasResourceLoader();
		ResourceLoader.AddResourceFormatLoader(_atlasResourceLoader, atFront: true);
		AtlasManager.LoadEssentialAtlases();
		Perf.Mark("essential atlases");
		LocManager.Initialize();
		BakedContent.InjectLocEntries();
		Perf.Mark("localization");
		ModelDb.Init();
		BaseLib.Utils.WatcherRegistry.Populate();
		Perf.Mark("model db");
		ModelIdSerializationCache.Init();
		ModelDb.InitIds();
		Perf.Mark("model ids");
		// The Watcher stance models are per-player state rather than canonical models,
		// so they subscribe to the combat hook chain on their own.
		global::Watcher.Code.Events.WatcherSubscriber.Subscribe();
		MessageTypes.Initialize();
		ActionTypes.Initialize();
		Perf.Mark("message and action types");
	}

	/// <summary>
	/// Deferred initialization that can run after main menu is displayed.
	/// Loads remaining atlases and runs expensive precomputation.
	/// </summary>
	public static void ExecuteDeferred()
	{
		if (_state != State.Essential)
		{
			Log.Error($"Tried to call OneTimeInitialization.ExecuteDeferred in state {_state}!");
			return;
		}
		_state = State.Done;
		AtlasManager.LoadAllAtlases();
		Perf.Mark("all atlases");
		if (!OS.HasFeature("editor"))
		{
			ModelDb.Preload();
			Perf.Mark("model db preload");
			PrewarmJit();
			Perf.Mark("jit prewarm");
			ConditionalFormatter conditionalFormatter = new ConditionalFormatter();
		}
	}

	private static void PrewarmJit()
	{
		Type typeFromHandle = typeof(PacketWriter);
		Type typeFromHandle2 = typeof(PacketReader);
		foreach (Type subtype in ReflectionHelper.GetSubtypes<IPacketSerializable>())
		{
			RuntimeHelpers.PrepareMethod(subtype.GetMethod("Serialize").MethodHandle);
			RuntimeHelpers.PrepareMethod(subtype.GetMethod("Deserialize").MethodHandle);
			RuntimeHelpers.PrepareMethod(typeFromHandle.GetMethod("WriteList").MethodHandle, new RuntimeTypeHandle[1] { subtype.TypeHandle });
			RuntimeHelpers.PrepareMethod(typeFromHandle.GetMethod("Write").MethodHandle, new RuntimeTypeHandle[1] { subtype.TypeHandle });
			RuntimeHelpers.PrepareMethod(typeFromHandle2.GetMethod("ReadList").MethodHandle, new RuntimeTypeHandle[1] { subtype.TypeHandle });
			RuntimeHelpers.PrepareMethod(typeFromHandle2.GetMethod("Read").MethodHandle, new RuntimeTypeHandle[1] { subtype.TypeHandle });
		}
		foreach (Type subtype2 in ReflectionHelper.GetSubtypes<INetMessage>())
		{
			RuntimeHelpers.PrepareMethod(subtype2.GetMethod("get_ShouldBuffer").MethodHandle);
			RuntimeHelpers.PrepareMethod(subtype2.GetMethod("get_LogLevel").MethodHandle);
			RuntimeHelpers.PrepareMethod(subtype2.GetMethod("get_Mode").MethodHandle);
			RuntimeHelpers.PrepareMethod(subtype2.GetMethod("get_ShouldBroadcast").MethodHandle);
		}
	}
}
