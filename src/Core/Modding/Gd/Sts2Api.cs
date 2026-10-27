using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Modding.Gd;

/// <summary>
/// The read-only surface a GDScript mod talks to.
///
/// Registered with <see cref="Engine.RegisterSingleton"/> as <c>Sts2</c>, so a mod calls
/// <c>Sts2.card_title(handle)</c> directly. Every object that crosses the boundary as a return value
/// is a handle string from <see cref="GdModRuntime.HandleFor"/>; the mod passes the same string back
/// in. Nothing here takes or returns a C# object reference, because GDScript cannot hold one.
///
/// Method names are snake_case on purpose — that is what GDScript callers expect, and Godot's
/// registration is name-based so the C# naming is free.
/// </summary>
[GlobalClass]
[ScriptPath("res://src/Core/Modding/Gd/Sts2Api.cs")]
public class Sts2Api : Godot.Node
{
	/// <summary>Registered singleton name. A mod calls <c>Sts2.*</c>.</summary>
	public const string SingletonName = "Sts2";

	/// <summary>
	/// Cached StringNames for the methods contained in this class, for fast lookup.
	/// </summary>
	public new class MethodName : Godot.Node.MethodName
	{
		public new static readonly StringName _Ready = "_Ready";

		public static readonly StringName GetRun = "get_run";
		public static readonly StringName GetPlayer = "get_player";
		public static readonly StringName GetMe = "get_me";
		public static readonly StringName GetCombat = "get_combat";
		public static readonly StringName GetPlayerCombat = "get_player_combat";
		public static readonly StringName IsInCombat = "is_in_combat";

		public static readonly StringName CreatureHp = "creature_hp";
		public static readonly StringName CreatureMaxHp = "creature_max_hp";
		public static readonly StringName CreatureBlock = "creature_block";
		public static readonly StringName CreatureName = "creature_name";
		public static readonly StringName CreatureIsPlayer = "creature_is_player";
		public static readonly StringName CreatureIsAlive = "creature_is_alive";
		public static readonly StringName CreaturePowers = "creature_powers";
		public static readonly StringName GetPowerAmount = "get_power_amount";

		public static readonly StringName Allies = "allies";
		public static readonly StringName Enemies = "enemies";
		public static readonly StringName HittableEnemies = "hittable_enemies";

		public static readonly StringName PlayerGold = "player_gold";
		public static readonly StringName PlayerEnergy = "player_energy";
		public static readonly StringName PlayerMaxEnergy = "player_max_energy";
		public static readonly StringName PlayerStars = "player_stars";
		public static readonly StringName PlayerDeck = "player_deck";
		public static readonly StringName PlayerRelics = "player_relics";
		public static readonly StringName PlayerPotions = "player_potions";

		public static readonly StringName Pile = "pile";
		public static readonly StringName PileCards = "pile_cards";
		public static readonly StringName AllCards = "all_cards";

		public static readonly StringName CardTitle = "card_title";
		public static readonly StringName CardId = "card_id";
		public static readonly StringName CardCost = "card_cost";
		public static readonly StringName CardStarCost = "card_star_cost";
		public static readonly StringName CardTypeName = "card_type";
		public static readonly StringName CardRarityName = "card_rarity";
		public static readonly StringName CardUpgradeLevel = "card_upgrade_level";
		public static readonly StringName CardOwner = "card_owner";
		public static readonly StringName PlayerCreature = "player_creature";

		public static readonly StringName ModelName = "model_name";
		public static readonly StringName ModelId = "model_id";
		public static readonly StringName Log = "log";

		public static readonly StringName GainEnergy = "gain_energy";
		public static readonly StringName LoseEnergy = "lose_energy";
		public static readonly StringName SetEnergy = "set_energy";
		public static readonly StringName GainStars = "gain_stars";
		public static readonly StringName LoseStars = "lose_stars";
		public static readonly StringName GainGold = "gain_gold";
		public static readonly StringName LoseGold = "lose_gold";
		public static readonly StringName Damage = "damage";
		public static readonly StringName LoseHp = "lose_hp";
		public static readonly StringName Heal = "heal";
		public static readonly StringName GainBlock = "gain_block";
		public static readonly StringName LoseBlock = "lose_block";
		public static readonly StringName DrawCards = "draw_cards";
		public static readonly StringName AddCardToPile = "add_card_to_pile";
		public static readonly StringName CreateCard = "create_card";
		public static readonly StringName ApplyPower = "apply_power";
	}

	/// <summary>
	/// Cached StringNames for the properties contained in this class, for fast lookup.
	/// </summary>
	public new class PropertyName : Godot.Node.PropertyName
	{
		public static readonly StringName Instance = "Instance";
	}

	public static Sts2Api? Instance { get; private set; }

	public override void _Ready()
	{
		Activate();
	}

	/// <summary>
	/// Registers the singleton. Called by <see cref="GdModHost.InstallApi"/> directly as well,
	/// because a node built with <c>new</c> from C# carries no script and therefore never receives the
	/// <c>_Ready</c> notification. Guarded so both paths agreeing costs nothing.
	/// </summary>
	public void Activate()
	{
		if (Instance == this)
		{
			return;
		}
		Instance = this;
		Engine.RegisterSingleton(SingletonName, this);
		Log.Info("[GdMod] Sts2Api registered as singleton 'Sts2'");
	}

	private static ThrowingPlayerChoiceContext Ctx => new ThrowingPlayerChoiceContext();

	// ---------------------------------------------------------------- top level objects

	public string get_run()
	{
		return GdModRuntime.HandleFor(RunManager.Instance.DebugOnlyGetState());
	}

	public string get_player()
	{
		RunState? run = RunManager.Instance.DebugOnlyGetState();
		if (run == null)
		{
			return string.Empty;
		}
		Player? me = LocalContext.GetMe(run.Players);
		return GdModRuntime.HandleFor(me);
	}

	public string get_me()
	{
		return get_player();
	}

	public string get_combat()
	{
		return GdModRuntime.HandleFor(CombatManager.Instance.DebugOnlyGetState());
	}

	public string get_player_combat()
	{
		return get_player();
	}

	public bool is_in_combat()
	{
		return CombatManager.Instance.DebugOnlyGetState() != null;
	}

	// ---------------------------------------------------------------- creature

	public int creature_hp(string handle)
	{
		return GdModRuntime.Resolve<Creature>(handle)?.CurrentHp ?? 0;
	}

	public int creature_max_hp(string handle)
	{
		return GdModRuntime.Resolve<Creature>(handle)?.MaxHp ?? 0;
	}

	public int creature_block(string handle)
	{
		return GdModRuntime.Resolve<Creature>(handle)?.Block ?? 0;
	}

	public string creature_name(string handle)
	{
		return GdModRuntime.Resolve<Creature>(handle)?.Name ?? string.Empty;
	}

	public bool creature_is_player(string handle)
	{
		return GdModRuntime.Resolve<Creature>(handle)?.IsPlayer ?? false;
	}

	public bool creature_is_alive(string handle)
	{
		return GdModRuntime.Resolve<Creature>(handle)?.IsAlive ?? false;
	}

	public Godot.Collections.Array<string> creature_powers(string handle)
	{
		Godot.Collections.Array<string> result = new Godot.Collections.Array<string>();
		Creature? creature = GdModRuntime.Resolve<Creature>(handle);
		if (creature == null)
		{
			return result;
		}
		foreach (PowerModel power in creature.Powers)
		{
			result.Add(GdModRuntime.HandleFor(power));
		}
		return result;
	}

	public int get_power_amount(string handle)
	{
		return GdModRuntime.Resolve<PowerModel>(handle)?.Amount ?? 0;
	}

	// ---------------------------------------------------------------- combat collections
	//
	// Each of these takes the combat handle it was handed by the host. The host fires
	// on_combat_started(combat), on_turn_started(combat) and friends, so a mod almost always has one
	// in hand; an empty handle falls back to whatever combat is currently running. The parameter is
	// not optional in the C# sense because Godot requires a fixed arity per method, and a mod that
	// passes one argument to a zero-argument method has its call dropped outright.

	/// <summary>Creatures fighting alongside the local player. Pass "" for the current combat.</summary>
	public Godot.Collections.Array<string> allies(string combatHandle)
	{
		return CollectCreatures(ResolveCombat(combatHandle)?.Allies);
	}

	/// <summary>Creatures fighting the local player. Pass "" for the current combat.</summary>
	public Godot.Collections.Array<string> enemies(string combatHandle)
	{
		return CollectCreatures(ResolveCombat(combatHandle)?.Enemies);
	}

	/// <summary>Enemies that can be targeted right now. Pass "" for the current combat.</summary>
	public Godot.Collections.Array<string> hittable_enemies(string combatHandle)
	{
		return CollectCreatures(ResolveCombat(combatHandle)?.HittableEnemies);
	}

	private static CombatState? ResolveCombat(string handle)
	{
		return GdModRuntime.Resolve<CombatState>(handle) ?? CombatManager.Instance.DebugOnlyGetState();
	}

	private static Godot.Collections.Array<string> CollectCreatures(IEnumerable<Creature>? creatures)
	{
		Godot.Collections.Array<string> result = new Godot.Collections.Array<string>();
		if (creatures == null)
		{
			return result;
		}
		foreach (Creature creature in creatures)
		{
			result.Add(GdModRuntime.HandleFor(creature));
		}
		return result;
	}

	// ---------------------------------------------------------------- player

	public int player_gold(string handle)
	{
		return GdModRuntime.Resolve<Player>(handle)?.Gold ?? 0;
	}

	public int player_energy(string handle)
	{
		return GdModRuntime.Resolve<Player>(handle)?.PlayerCombatState?.Energy ?? 0;
	}

	public int player_max_energy(string handle)
	{
		return GdModRuntime.Resolve<Player>(handle)?.PlayerCombatState?.MaxEnergy ?? 0;
	}

	public int player_stars(string handle)
	{
		return GdModRuntime.Resolve<Player>(handle)?.PlayerCombatState?.Stars ?? 0;
	}

	public Godot.Collections.Array<string> player_deck(string handle)
	{
		Godot.Collections.Array<string> result = new Godot.Collections.Array<string>();
		Player? player = GdModRuntime.Resolve<Player>(handle);
		if (player == null)
		{
			return result;
		}
		foreach (CardModel card in player.Deck.Cards)
		{
			result.Add(GdModRuntime.HandleFor(card));
		}
		return result;
	}

	public Godot.Collections.Array<string> player_relics(string handle)
	{
		Godot.Collections.Array<string> result = new Godot.Collections.Array<string>();
		Player? player = GdModRuntime.Resolve<Player>(handle);
		if (player == null)
		{
			return result;
		}
		foreach (RelicModel relic in player.Relics)
		{
			result.Add(GdModRuntime.HandleFor(relic));
		}
		return result;
	}

	public Godot.Collections.Array<string> player_potions(string handle)
	{
		Godot.Collections.Array<string> result = new Godot.Collections.Array<string>();
		Player? player = GdModRuntime.Resolve<Player>(handle);
		if (player == null)
		{
			return result;
		}
		foreach (PotionModel? potion in player.PotionSlots)
		{
			if (potion != null)
			{
				result.Add(GdModRuntime.HandleFor(potion));
			}
		}
		return result;
	}

	// ---------------------------------------------------------------- piles

	/// <summary>Returns a handle for one of the player's combat piles. pile is the <c>PileType</c> int.</summary>
	public string pile(string playerHandle, int pileType)
	{
		Player? player = GdModRuntime.Resolve<Player>(playerHandle);
		if (player == null)
		{
			return string.Empty;
		}
		return GdModRuntime.HandleFor(CardPile.Get((PileType)pileType, player));
	}

	public Godot.Collections.Array<string> pile_cards(string pileHandle)
	{
		Godot.Collections.Array<string> result = new Godot.Collections.Array<string>();
		CardPile? pile = GdModRuntime.Resolve<CardPile>(pileHandle);
		if (pile == null)
		{
			return result;
		}
		foreach (CardModel card in pile.Cards)
		{
			result.Add(GdModRuntime.HandleFor(card));
		}
		return result;
	}

	public Godot.Collections.Array<string> all_cards(string playerHandle)
	{
		Godot.Collections.Array<string> result = new Godot.Collections.Array<string>();
		Player? player = GdModRuntime.Resolve<Player>(playerHandle);
		if (player == null || player.PlayerCombatState == null)
		{
			return result;
		}
		foreach (CardModel card in player.PlayerCombatState.AllCards)
		{
			result.Add(GdModRuntime.HandleFor(card));
		}
		return result;
	}

	// ---------------------------------------------------------------- card

	public string card_title(string handle)
	{
		return GdModRuntime.Resolve<CardModel>(handle)?.Title ?? string.Empty;
	}

	public string card_id(string handle)
	{
		return GdModRuntime.Resolve<AbstractModel>(handle)?.Id.ToString() ?? string.Empty;
	}

	/// <summary>
	/// The energy this card costs right now, after every modifier, which is the number printed on the
	/// card. Star cost is a separate pool and has its own accessor.
	/// </summary>
	public int card_cost(string handle)
	{
		return GdModRuntime.Resolve<CardModel>(handle)?.EnergyCost.GetAmountToSpend() ?? 0;
	}

	/// <summary>The star cost this card charges right now. Most cards charge none.</summary>
	public int card_star_cost(string handle)
	{
		return GdModRuntime.Resolve<CardModel>(handle)?.CurrentStarCost ?? 0;
	}

	public string card_type(string handle)
	{
		CardModel? card = GdModRuntime.Resolve<CardModel>(handle);
		return card == null ? string.Empty : card.Type.ToString();
	}

	public string card_rarity(string handle)
	{
		CardModel? card = GdModRuntime.Resolve<CardModel>(handle);
		return card == null ? string.Empty : card.Rarity.ToString();
	}

	public int card_upgrade_level(string handle)
	{
		return GdModRuntime.Resolve<CardModel>(handle)?.CurrentUpgradeLevel ?? 0;
	}

	public string card_owner(string handle)
	{
		return GdModRuntime.HandleFor(GdModRuntime.Resolve<CardModel>(handle)?.Owner);
	}

	// ---------------------------------------------------------------- model metadata

	/// <summary>
	/// Returns the handle of the creature a player is driving. Hooks hand out either a
	/// <see cref="Player"/> handle or a <see cref="Creature"/> handle depending on the hook, and the
	/// creature accessors only accept the latter, so this is the bridge between the two.
	/// </summary>
	public string player_creature(string handle)
	{
		return GdModRuntime.HandleFor(GdModRuntime.Resolve<Player>(handle)?.Creature);
	}

	public string model_name(string handle)
	{
		return GdModRuntime.Resolve<AbstractModel>(handle)?.Id.Entry ?? string.Empty;
	}

	public string model_id(string handle)
	{
		return GdModRuntime.Resolve<AbstractModel>(handle)?.Id.ToString() ?? string.Empty;
	}

	public void log(string message)
	{
		Log.Info("[GdMod] " + message);
	}

	// ---------------------------------------------------------------- mutate: player resources

	public void gain_energy(string playerHandle, double amount)
	{
		Player? player = GdModRuntime.Resolve<Player>(playerHandle);
		if (player != null)
		{
			GdModRuntime.Fire(PlayerCmd.GainEnergy((decimal)amount, player), "gain_energy");
		}
	}

	public void lose_energy(string playerHandle, double amount)
	{
		Player? player = GdModRuntime.Resolve<Player>(playerHandle);
		if (player != null)
		{
			GdModRuntime.Fire(PlayerCmd.LoseEnergy((decimal)amount, player), "lose_energy");
		}
	}

	public void set_energy(string playerHandle, double amount)
	{
		Player? player = GdModRuntime.Resolve<Player>(playerHandle);
		if (player != null)
		{
			GdModRuntime.Fire(PlayerCmd.SetEnergy((decimal)amount, player), "set_energy");
		}
	}

	public void gain_stars(string playerHandle, double amount)
	{
		Player? player = GdModRuntime.Resolve<Player>(playerHandle);
		if (player != null)
		{
			GdModRuntime.Fire(PlayerCmd.GainStars((decimal)amount, player), "gain_stars");
		}
	}

	public void lose_stars(string playerHandle, double amount)
	{
		Player? player = GdModRuntime.Resolve<Player>(playerHandle);
		if (player != null)
		{
			GdModRuntime.Fire(PlayerCmd.LoseStars((decimal)amount, player), "lose_stars");
		}
	}

	public void gain_gold(string playerHandle, double amount)
	{
		Player? player = GdModRuntime.Resolve<Player>(playerHandle);
		if (player != null)
		{
			GdModRuntime.Fire(PlayerCmd.GainGold((decimal)amount, player), "gain_gold");
		}
	}

	public void lose_gold(string playerHandle, double amount)
	{
		Player? player = GdModRuntime.Resolve<Player>(playerHandle);
		if (player != null)
		{
			GdModRuntime.Fire(PlayerCmd.LoseGold((decimal)amount, player), "lose_gold");
		}
	}

	// ---------------------------------------------------------------- mutate: creature

	/// <summary>
	/// Deals real attack damage: block first, then HP, through the full damage pipeline so every
	/// damage hook fires and powers react. The amount is raw damage before modifiers, exactly as a
	/// card would declare it.
	/// </summary>
	public void damage(string creatureHandle, double amount)
	{
		Creature? creature = GdModRuntime.Resolve<Creature>(creatureHandle);
		if (creature != null)
		{
			GdModRuntime.Fire(CreatureCmd.Damage(Ctx, creature, (decimal)amount, ValueProp.Move, null, null), "damage");
		}
	}

	public void lose_hp(string creatureHandle, double amount)
	{
		Creature? creature = GdModRuntime.Resolve<Creature>(creatureHandle);
		if (creature != null)
		{
			creature.LoseHpInternal((decimal)amount, ValueProp.Move);
		}
	}

	public void heal(string creatureHandle, double amount)
	{
		Creature? creature = GdModRuntime.Resolve<Creature>(creatureHandle);
		creature?.HealInternal((decimal)amount);
	}

	public void gain_block(string creatureHandle, double amount)
	{
		Creature? creature = GdModRuntime.Resolve<Creature>(creatureHandle);
		creature?.GainBlockInternal((decimal)amount);
	}

	public void lose_block(string creatureHandle, double amount)
	{
		Creature? creature = GdModRuntime.Resolve<Creature>(creatureHandle);
		creature?.LoseBlockInternal((decimal)amount);
	}

	// ---------------------------------------------------------------- mutate: cards

	public void draw_cards(string playerHandle, int count)
	{
		Player? player = GdModRuntime.Resolve<Player>(playerHandle);
		if (player != null)
		{
			GdModRuntime.Fire(CardPileCmd.Draw(Ctx, count, player), "draw_cards");
		}
	}

	/// <summary>Moves an existing card into a pile. pile is the <c>PileType</c> int.</summary>
	public void add_card_to_pile(string cardHandle, int pileType)
	{
		CardModel? card = GdModRuntime.Resolve<CardModel>(cardHandle);
		if (card != null)
		{
			GdModRuntime.Fire(CardPileCmd.Add(card, (PileType)pileType), "add_card_to_pile");
		}
	}

	/// <summary>
	/// Mints a fresh combat copy of a canonical card by ModelId ("card.strike_ironclad") and returns
	/// its handle. Returns an empty string when the id is unknown or the player has no combat state.
	/// </summary>
	public string create_card(string modelId, string playerHandle)
	{
		Player? player = GdModRuntime.Resolve<Player>(playerHandle);
		if (player == null)
		{
			return string.Empty;
		}
		CombatState? combat = CombatManager.Instance.DebugOnlyGetState();
		if (combat == null)
		{
			return string.Empty;
		}
		CardModel? canonical;
		try
		{
			canonical = ModelDb.GetById<CardModel>(ModelId.Deserialize(modelId));
		}
		catch (Exception ex)
		{
			Log.Warn($"[GdMod] create_card: unknown card id '{modelId}': {ex.Message}");
			return string.Empty;
		}
		return GdModRuntime.HandleFor(combat.CreateCard(canonical, player));
	}

	// ---------------------------------------------------------------- mutate: powers

	/// <summary>
	/// Applies a power by ModelId ("power.strength") to a creature. Returns the power handle, or an
	/// empty string when the id is unknown or the power could not be applied.
	/// </summary>
	public string apply_power(string modelId, string creatureHandle, double amount)
	{
		Creature? creature = GdModRuntime.Resolve<Creature>(creatureHandle);
		if (creature == null)
		{
			return string.Empty;
		}
		PowerModel? canonical;
		try
		{
			canonical = ModelDb.GetById<PowerModel>(ModelId.Deserialize(modelId));
		}
		catch (Exception ex)
		{
			Log.Warn($"[GdMod] apply_power: unknown power id '{modelId}': {ex.Message}");
			return string.Empty;
		}
		PowerModel mutable = canonical.ToMutable();
		GdModRuntime.Fire(PowerCmd.Apply(Ctx, mutable, creature, (decimal)amount, null, null), "apply_power");
		return GdModRuntime.HandleFor(mutable);
	}

	// ---------------------------------------------------------------- boilerplate

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		List<MethodInfo> list = new List<MethodInfo>(45);
		list.Add(new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		list.Add(new MethodInfo(MethodName.GetRun, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		list.Add(new MethodInfo(MethodName.GetPlayer, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		list.Add(new MethodInfo(MethodName.GetMe, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		list.Add(new MethodInfo(MethodName.GetCombat, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		list.Add(new MethodInfo(MethodName.GetPlayerCombat, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		list.Add(new MethodInfo(MethodName.IsInCombat, new PropertyInfo(Variant.Type.Bool, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		list.Add(new MethodInfo(MethodName.CreatureHp, new PropertyInfo(Variant.Type.Int, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.CreatureMaxHp, new PropertyInfo(Variant.Type.Int, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.CreatureBlock, new PropertyInfo(Variant.Type.Int, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.CreatureName, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.CreatureIsPlayer, new PropertyInfo(Variant.Type.Bool, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.CreatureIsAlive, new PropertyInfo(Variant.Type.Bool, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.CreaturePowers, new PropertyInfo(Variant.Type.Array, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.GetPowerAmount, new PropertyInfo(Variant.Type.Int, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.Allies, new PropertyInfo(Variant.Type.Array, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo> { new PropertyInfo(Variant.Type.String, "combatHandle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false) }, null));
		list.Add(new MethodInfo(MethodName.Enemies, new PropertyInfo(Variant.Type.Array, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo> { new PropertyInfo(Variant.Type.String, "combatHandle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false) }, null));
		list.Add(new MethodInfo(MethodName.HittableEnemies, new PropertyInfo(Variant.Type.Array, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo> { new PropertyInfo(Variant.Type.String, "combatHandle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false) }, null));
		list.Add(new MethodInfo(MethodName.PlayerGold, new PropertyInfo(Variant.Type.Int, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.PlayerEnergy, new PropertyInfo(Variant.Type.Int, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.PlayerMaxEnergy, new PropertyInfo(Variant.Type.Int, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.PlayerStars, new PropertyInfo(Variant.Type.Int, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.PlayerDeck, new PropertyInfo(Variant.Type.Array, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.PlayerRelics, new PropertyInfo(Variant.Type.Array, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.PlayerPotions, new PropertyInfo(Variant.Type.Array, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.Pile, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "player_handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Int, "pile_type", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.PileCards, new PropertyInfo(Variant.Type.Array, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "pile_handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.AllCards, new PropertyInfo(Variant.Type.Array, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "player_handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.CardTitle, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.CardId, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.CardCost, new PropertyInfo(Variant.Type.Int, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.CardStarCost, new PropertyInfo(Variant.Type.Int, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.CardTypeName, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.CardRarityName, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.CardUpgradeLevel, new PropertyInfo(Variant.Type.Int, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.CardOwner, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.PlayerCreature, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.ModelName, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.ModelId, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.Log, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "message", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.GainEnergy, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "player_handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "amount", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.LoseEnergy, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "player_handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "amount", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.SetEnergy, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "player_handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "amount", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.GainStars, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "player_handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "amount", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.LoseStars, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "player_handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "amount", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.GainGold, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "player_handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "amount", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.LoseGold, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "player_handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "amount", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.Damage, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "creature_handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "amount", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.LoseHp, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "creature_handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "amount", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.Heal, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "creature_handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "amount", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.GainBlock, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "creature_handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "amount", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.LoseBlock, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "creature_handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "amount", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.DrawCards, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "player_handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Int, "count", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.AddCardToPile, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "card_handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Int, "pile_type", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.CreateCard, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "model_id", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.String, "player_handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		list.Add(new MethodInfo(MethodName.ApplyPower, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "model_id", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.String, "creature_handle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "amount", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		return list;
	}

	/// <inheritdoc />
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName._Ready && args.Count == 0)
		{
			_Ready();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.GetRun && args.Count == 0)
		{
			ret = VariantUtils.CreateFrom<string>(get_run());
			return true;
		}
		if (method == MethodName.GetPlayer && args.Count == 0)
		{
			ret = VariantUtils.CreateFrom<string>(get_player());
			return true;
		}
		if (method == MethodName.GetMe && args.Count == 0)
		{
			ret = VariantUtils.CreateFrom<string>(get_me());
			return true;
		}
		if (method == MethodName.GetCombat && args.Count == 0)
		{
			ret = VariantUtils.CreateFrom<string>(get_combat());
			return true;
		}
		if (method == MethodName.GetPlayerCombat && args.Count == 0)
		{
			ret = VariantUtils.CreateFrom<string>(get_player_combat());
			return true;
		}
		if (method == MethodName.IsInCombat && args.Count == 0)
		{
			ret = VariantUtils.CreateFrom<bool>(is_in_combat());
			return true;
		}
		if (method == MethodName.CreatureHp && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<int>(creature_hp(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.CreatureMaxHp && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<int>(creature_max_hp(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.CreatureBlock && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<int>(creature_block(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.CreatureName && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(creature_name(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.CreatureIsPlayer && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<bool>(creature_is_player(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.CreatureIsAlive && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<bool>(creature_is_alive(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.CreaturePowers && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Godot.Collections.Array<string>>(creature_powers(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.GetPowerAmount && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<int>(get_power_amount(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.Allies && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Godot.Collections.Array<string>>(allies(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.Enemies && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Godot.Collections.Array<string>>(enemies(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.HittableEnemies && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Godot.Collections.Array<string>>(hittable_enemies(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.PlayerGold && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<int>(player_gold(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.PlayerEnergy && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<int>(player_energy(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.PlayerMaxEnergy && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<int>(player_max_energy(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.PlayerStars && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<int>(player_stars(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.PlayerDeck && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Godot.Collections.Array<string>>(player_deck(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.PlayerRelics && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Godot.Collections.Array<string>>(player_relics(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.PlayerPotions && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Godot.Collections.Array<string>>(player_potions(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.Pile && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<string>(pile(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<int>(in args[1])));
			return true;
		}
		if (method == MethodName.PileCards && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Godot.Collections.Array<string>>(pile_cards(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.AllCards && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Godot.Collections.Array<string>>(all_cards(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.CardTitle && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(card_title(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.CardId && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(card_id(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.CardCost && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<int>(card_cost(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.CardStarCost && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<int>(card_star_cost(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.CardTypeName && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(card_type(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.CardRarityName && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(card_rarity(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.CardUpgradeLevel && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<int>(card_upgrade_level(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.CardOwner && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(card_owner(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.PlayerCreature && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(player_creature(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.ModelName && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(model_name(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.ModelId && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(model_id(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.Log && args.Count == 1)
		{
			log(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.GainEnergy && args.Count == 2)
		{
			gain_energy(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<double>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.LoseEnergy && args.Count == 2)
		{
			lose_energy(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<double>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.SetEnergy && args.Count == 2)
		{
			set_energy(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<double>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.GainStars && args.Count == 2)
		{
			gain_stars(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<double>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.LoseStars && args.Count == 2)
		{
			lose_stars(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<double>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.GainGold && args.Count == 2)
		{
			gain_gold(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<double>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.LoseGold && args.Count == 2)
		{
			lose_gold(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<double>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.Damage && args.Count == 2)
		{
			damage(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<double>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.LoseHp && args.Count == 2)
		{
			lose_hp(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<double>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.Heal && args.Count == 2)
		{
			heal(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<double>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.GainBlock && args.Count == 2)
		{
			gain_block(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<double>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.LoseBlock && args.Count == 2)
		{
			lose_block(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<double>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.DrawCards && args.Count == 2)
		{
			draw_cards(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<int>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.AddCardToPile && args.Count == 2)
		{
			add_card_to_pile(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<int>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.CreateCard && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<string>(create_card(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<string>(in args[1])));
			return true;
		}
		if (method == MethodName.ApplyPower && args.Count == 3)
		{
			ret = VariantUtils.CreateFrom<string>(apply_power(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]), VariantUtils.ConvertTo<double>(in args[2])));
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	/// <inheritdoc />
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName._Ready) { return true; }
		if (method == MethodName.GetRun) { return true; }
		if (method == MethodName.GetPlayer) { return true; }
		if (method == MethodName.GetMe) { return true; }
		if (method == MethodName.GetCombat) { return true; }
		if (method == MethodName.GetPlayerCombat) { return true; }
		if (method == MethodName.IsInCombat) { return true; }
		if (method == MethodName.CreatureHp) { return true; }
		if (method == MethodName.CreatureMaxHp) { return true; }
		if (method == MethodName.CreatureBlock) { return true; }
		if (method == MethodName.CreatureName) { return true; }
		if (method == MethodName.CreatureIsPlayer) { return true; }
		if (method == MethodName.CreatureIsAlive) { return true; }
		if (method == MethodName.CreaturePowers) { return true; }
		if (method == MethodName.GetPowerAmount) { return true; }
		if (method == MethodName.Allies) { return true; }
		if (method == MethodName.Enemies) { return true; }
		if (method == MethodName.HittableEnemies) { return true; }
		if (method == MethodName.PlayerGold) { return true; }
		if (method == MethodName.PlayerEnergy) { return true; }
		if (method == MethodName.PlayerMaxEnergy) { return true; }
		if (method == MethodName.PlayerStars) { return true; }
		if (method == MethodName.PlayerDeck) { return true; }
		if (method == MethodName.PlayerRelics) { return true; }
		if (method == MethodName.PlayerPotions) { return true; }
		if (method == MethodName.Pile) { return true; }
		if (method == MethodName.PileCards) { return true; }
		if (method == MethodName.AllCards) { return true; }
		if (method == MethodName.CardTitle) { return true; }
		if (method == MethodName.CardId) { return true; }
		if (method == MethodName.CardCost) { return true; }
		if (method == MethodName.CardStarCost) { return true; }
		if (method == MethodName.CardTypeName) { return true; }
		if (method == MethodName.CardRarityName) { return true; }
		if (method == MethodName.CardUpgradeLevel) { return true; }
		if (method == MethodName.CardOwner) { return true; }
		if (method == MethodName.PlayerCreature) { return true; }
		if (method == MethodName.ModelName) { return true; }
		if (method == MethodName.ModelId) { return true; }
		if (method == MethodName.Log) { return true; }
		if (method == MethodName.GainEnergy) { return true; }
		if (method == MethodName.LoseEnergy) { return true; }
		if (method == MethodName.SetEnergy) { return true; }
		if (method == MethodName.GainStars) { return true; }
		if (method == MethodName.LoseStars) { return true; }
		if (method == MethodName.GainGold) { return true; }
		if (method == MethodName.LoseGold) { return true; }
		if (method == MethodName.Damage) { return true; }
		if (method == MethodName.LoseHp) { return true; }
		if (method == MethodName.Heal) { return true; }
		if (method == MethodName.GainBlock) { return true; }
		if (method == MethodName.LoseBlock) { return true; }
		if (method == MethodName.DrawCards) { return true; }
		if (method == MethodName.AddCardToPile) { return true; }
		if (method == MethodName.CreateCard) { return true; }
		if (method == MethodName.ApplyPower) { return true; }
		return base.HasGodotClassMethod(in method);
	}

	/// <inheritdoc />
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	/// <inheritdoc />
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	/// <inheritdoc />
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		return new List<PropertyInfo>();
	}

	/// <inheritdoc />
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
	}

	/// <inheritdoc />
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
	}
}
