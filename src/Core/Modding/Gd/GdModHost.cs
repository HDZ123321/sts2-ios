using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Modding.Gd;

/// <summary>
/// Creates the scripting layer for mods once the mod list has been read.
///
/// Two things happen here and nowhere else:
///
/// 1. The two hook bridges are subscribed to the two hook dispatch chains. A hook is dispatched with
///    <c>runState.IterateHookListeners(combatState)</c>, which appends the run subscribers and then
///    recurses into <c>combatState.IterateHookListeners()</c>, which appends the combat subscribers.
///    One model registered in both lists would therefore be appended twice and double-apply every
///    additive answer, so <see cref="GdCombatHookBridge"/> and <see cref="GdRunHookBridge"/> each
///    implement only the hooks their own chain dispatches.
///
/// 2. Every loaded mod that ships a GDScript entry point gets an instance parented under this node.
///    The entry point is looked up in the mod's own PCK, which <see cref="ModManager.TryLoadMod"/>
///    already mounted at <c>res://&lt;modId&gt;/</c> convention-free — a PCK keeps whatever internal
///    paths it was built with, so the manifest supplies them: <c>entry</c> names a
///    <c>res://</c> path, and when it is absent the host probes
///    <c>res://&lt;id&gt;/main.gdc</c> then <c>res://&lt;id&gt;/main.gd</c>.
///
/// The host never fails the mod load: a mod with a broken script is reported and skipped, because a
/// bad script must not take the game down with it.
/// </summary>
public partial class GdModHost : Node
{
	public const string ScriptDir = "res://src/Core/Modding/Gd";

	private static readonly string[] _defaultEntryCandidates = new string[2] { "main.gdc", "main.gd" };

	private GdCombatHookBridge? _combatBridge;

	private GdRunHookBridge? _runBridge;

	private readonly List<Node> _instances = new List<Node>();

	/// <summary>Mod instances that attached successfully, in load order.</summary>
	public IReadOnlyList<Node> Instances => _instances;

	/// <summary>
	/// Builds the scripting layer. Call once, after <see cref="ModManager.Initialize"/> has completed
	/// and before the main menu is shown.
	/// </summary>
	public static GdModHost Install(Node parent)
	{
		GdModHost host = new GdModHost
		{
			Name = "GdModHost"
		};
		parent.AddChildSafely(host);
		host.Start();
		return host;
	}

	/// <summary>
	/// Does the work that would normally live in <c>_Ready</c>.
	///
	/// It cannot live in <c>_Ready</c>: Godot only dispatches <c>_Ready</c> to nodes that carry a
	/// script registered with the C# script bridge (a <c>[ScriptPath]</c> type, or a node the editor
	/// instantiated). This host is built with <c>new</c> from the game's own C#, so no script is
	/// attached and its <c>_Ready</c> is never dispatched. Calling the startup sequence explicitly is
	/// the whole reason this method exists and is public.
	/// </summary>
	public void Start()
	{
		GdModRuntime.ModRoot = this;
		InstallApi();
		SubscribeHooks();
		SubscribeLifecycle();
		LoadMods();
	}

	/// <summary>
	/// Adds the <see cref="Sts2Api"/> node, whose own <c>_Ready</c> registers it as the <c>Sts2</c>
	/// engine singleton. It has to happen before any mod script runs, because a mod resolves the
	/// singleton in its own <c>_ready</c> and gives up when it is missing.
	/// </summary>
	private void InstallApi()
	{
		if (Sts2Api.Instance != null)
		{
			return;
		}
		Sts2Api api = new Sts2Api
		{
			Name = "Sts2Api"
		};
		this.AddChildSafely(api);
		api.Activate();
	}

	/// <summary>
	/// Wires the run/combat events a mod would otherwise have no way to observe. These are plain C#
	/// events on the managers, not Godot signals, so a mod cannot connect to them itself — the host
	/// listens and rebroadcasts by name.
	///
	/// Both managers are static singletons that outlive this node, so the handlers are detached in
	/// <see cref="_ExitTree"/>; a leaked handler would call into a freed node.
	/// </summary>
	private void SubscribeLifecycle()
	{
		RunManager.Instance.RunStarted += OnRunStarted;
		RunManager.Instance.ActEntered += OnActEntered;
		RunManager.Instance.RoomEntered += OnRoomEntered;
		CombatManager.Instance.CombatSetUp += OnCombatSetUp;
		CombatManager.Instance.CombatEnded += OnCombatEnded;
		CombatManager.Instance.CombatWon += OnCombatWon;
		CombatManager.Instance.TurnStarted += OnTurnStarted;
		CombatManager.Instance.TurnEnded += OnTurnEnded;
	}

	private void UnsubscribeLifecycle()
	{
		RunManager.Instance.RunStarted -= OnRunStarted;
		RunManager.Instance.ActEntered -= OnActEntered;
		RunManager.Instance.RoomEntered -= OnRoomEntered;
		CombatManager.Instance.CombatSetUp -= OnCombatSetUp;
		CombatManager.Instance.CombatEnded -= OnCombatEnded;
		CombatManager.Instance.CombatWon -= OnCombatWon;
		CombatManager.Instance.TurnStarted -= OnTurnStarted;
		CombatManager.Instance.TurnEnded -= OnTurnEnded;
	}

	private void OnRunStarted(RunState run)
	{
		Broadcast("on_run_started", GdModRuntime.H(run));
	}

	private void OnActEntered()
	{
		Broadcast("on_act_entered");
	}

	private void OnRoomEntered()
	{
		Broadcast("on_room_entered");
	}

	private void OnCombatSetUp(CombatState combat)
	{
		Broadcast("on_combat_started", GdModRuntime.H(combat));
	}

	private void OnCombatEnded(CombatRoom room)
	{
		Broadcast("on_combat_ended", GdModRuntime.H(room));
	}

	private void OnCombatWon(CombatRoom room)
	{
		Broadcast("on_combat_won", GdModRuntime.H(room));
	}

	private void OnTurnStarted(CombatState combat)
	{
		Broadcast("on_turn_started", GdModRuntime.H(combat));
	}

	private void OnTurnEnded(CombatState combat)
	{
		Broadcast("on_turn_ended", GdModRuntime.H(combat));
	}

	private void SubscribeHooks()
	{
		if (_combatBridge != null || _runBridge != null)
		{
			return;
		}
		_combatBridge = new GdCombatHookBridge();
		_runBridge = new GdRunHookBridge();
		ModHelper.SubscribeForCombatStateHooks("gd_bridge", delegate
		{
			return new AbstractModel[1] { _combatBridge };
		});
		ModHelper.SubscribeForRunStateHooks("gd_bridge", delegate
		{
			return new AbstractModel[1] { _runBridge };
		});
		Log.Info("[GdMod] hook bridges subscribed to combat and run dispatch");
	}

	private void LoadMods()
	{
		int loaded = 0;
		int skipped = 0;
		foreach (Mod mod in ModManager.GetLoadedMods())
		{
			if (mod.manifest == null)
			{
				continue;
			}
			if (!TryAttachMod(mod))
			{
				skipped++;
				continue;
			}
			loaded++;
		}
		Log.Info($"[GdMod] {loaded} script mod(s) attached, {skipped} skipped, {ModManager.Mods.Count} manifest(s) total");
	}

	private bool TryAttachMod(Mod mod)
	{
		string? scriptPath = ResolveEntryPoint(mod);
		if (scriptPath == null)
		{
			Log.Info($"[GdMod] mod '{mod.manifest?.id}' ships no GDScript entry point, ignoring");
			return false;
		}
		Resource? resource;
		try
		{
			resource = ResourceLoader.Load(scriptPath, "", ResourceLoader.CacheMode.Reuse);
		}
		catch (Exception ex)
		{
			Log.Error($"[GdMod] mod '{mod.manifest?.id}': loading {scriptPath} threw {ex.GetType().Name}: {ex.Message}");
			return false;
		}
		if (resource is not GDScript script)
		{
			Log.Error($"[GdMod] mod '{mod.manifest?.id}': {scriptPath} is not a GDScript (got {resource?.GetType().Name ?? "null"})");
			return false;
		}
		if (!script.CanInstantiate())
		{
			Log.Error($"[GdMod] mod '{mod.manifest?.id}': {scriptPath} cannot be instantiated");
			return false;
		}
		Variant variant;
		try
		{
			variant = script.New();
		}
		catch (Exception ex)
		{
			Log.Error($"[GdMod] mod '{mod.manifest?.id}': instantiating {scriptPath} threw {ex.GetType().Name}: {ex.Message}");
			return false;
		}
		if (variant.AsGodotObject() is not Node node)
		{
			Log.Error($"[GdMod] mod '{mod.manifest?.id}': {scriptPath} did not produce a Node");
			return false;
		}
		node.Name = $"mod_{mod.manifest?.id}";
		this.AddChildSafely(node);
		_instances.Add(node);
		GdModRuntime.AddMod(node);
		Log.Info($"[GdMod] mod '{mod.manifest?.id}' attached from {scriptPath}");
		return true;
	}

	/// <summary>
	/// Finds the mod's script entry point. The manifest's optional <c>gd_entry</c> key wins when
	/// present; otherwise the conventional <c>res://&lt;id&gt;/main.gdc</c> and <c>main.gd</c> are
	/// probed.
	///
	/// <c>gd_entry</c> is read straight out of the manifest JSON rather than through
	/// <see cref="ModManifest"/>: that type is deserialized by hand-written metadata in
	/// <c>MegaCritSerializerContext</c>, so a new field on it would be ignored until the metadata is
	/// regenerated. Reading the raw node keeps this feature additive.
	/// </summary>
	private static string? ResolveEntryPoint(Mod mod)
	{
		string? declared = ReadDeclaredEntry(mod);
		if (!string.IsNullOrEmpty(declared))
		{
			if (ResourceLoader.Exists(declared))
			{
				return declared;
			}
			Log.Error($"[GdMod] mod '{mod.manifest?.id}': declared gd_entry '{declared}' does not exist in the mounted PCK");
			return null;
		}
		string id = mod.manifest?.id ?? string.Empty;
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		foreach (string candidate in _defaultEntryCandidates)
		{
			string path = $"res://{id}/{candidate}";
			if (ResourceLoader.Exists(path))
			{
				return path;
			}
		}
		return null;
	}

	/// <summary>Reads the optional <c>gd_entry</c> string out of the mod's manifest file.</summary>
	private static string? ReadDeclaredEntry(Mod mod)
	{
		string id = mod.manifest?.id ?? string.Empty;
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		string manifestPath = $"{mod.path}/{id}.json";
		if (!Godot.FileAccess.FileExists(manifestPath))
		{
			return null;
		}
		using Godot.FileAccess file = Godot.FileAccess.Open(manifestPath, Godot.FileAccess.ModeFlags.Read);
		if (file == null)
		{
			return null;
		}
		string text = file.GetAsText();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			Variant parsed = Json.ParseString(text);
			if (parsed.VariantType != Variant.Type.Dictionary)
			{
				return null;
			}
			Godot.Collections.Dictionary dictionary = parsed.AsGodotDictionary();
			if (!dictionary.TryGetValue("gd_entry", out Variant value))
			{
				return null;
			}
			return value.AsString();
		}
		catch (Exception ex)
		{
			Log.Warn($"[GdMod] mod '{id}': could not parse gd_entry: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// Calls a lifecycle method on every attached mod instance, skipping mods that do not define it.
	/// </summary>
	public void Broadcast(string method, params Variant[] args)
	{
		foreach (Node node in _instances)
		{
			if (!GodotObject.IsInstanceValid(node) || !node.HasMethod(method))
			{
				continue;
			}
			node.Call(method, args);
		}
	}

	public override void _ExitTree()
	{
		UnsubscribeLifecycle();
		foreach (Node node in _instances)
		{
			if (GodotObject.IsInstanceValid(node))
			{
				node.QueueFree();
			}
		}
		_instances.Clear();
		GdModRuntime.Clear();
	}
}
