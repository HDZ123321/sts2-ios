using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Modding.Gd;

/// <summary>
/// Argument marshalling shared by the two hook bridges.
/// </summary>
internal static class GdHookArgs
{
	public static Variant Props(ValueProp props)
	{
		return Variant.From((int)props);
	}

	public static Variant Amount(decimal amount)
	{
		return Variant.From((double)amount);
	}

	public static Variant Result(DamageResult result)
	{
		Godot.Collections.Dictionary<string, Variant> dict = new Godot.Collections.Dictionary<string, Variant>
		{
			{ "receiver", GdModRuntime.H(result.Receiver) },
			{ "blocked", result.BlockedDamage },
			{ "unblocked", result.UnblockedDamage },
			{ "overkill", result.OverkillDamage },
			{ "total", result.TotalDamage },
			{ "props", (int)result.Props }
		};
		return Variant.From(dict);
	}

	public static Variant Play(CardPlay play)
	{
		Godot.Collections.Dictionary<string, Variant> dict = new Godot.Collections.Dictionary<string, Variant>
		{
			{ "card", GdModRuntime.H(play.Card) },
			{ "target", GdModRuntime.H(play.Target) },
			{ "result_pile", (int)play.ResultPile },
			{ "auto_play", play.IsAutoPlay },
			{ "play_index", play.PlayIndex },
			{ "play_count", play.PlayCount }
		};
		return Variant.From(dict);
	}
}

/// <summary>
/// Receives every hook that the game dispatches through the combat listener chain
/// (<c>combatState.IterateHookListeners()</c>, and therefore also <see cref="Hook.IterateCombatHookListeners"/>)
/// and forwards it into GDScript.
///
/// Two bridges exist rather than one, and this is load-bearing. A hook dispatched with
/// <c>runState.IterateHookListeners(combatState)</c> first appends the run subscribers and then
/// recurses into <c>combatState.IterateHookListeners()</c>, which appends the combat subscribers.
/// A single bridge registered in both lists would therefore appear twice in that chain, and every
/// additive or multiplicative hook would apply the mod's answer twice. Splitting the override
/// surface by chain — each bridge implements only the hooks its own chain dispatches — keeps every
/// hook dispatched exactly once.
///
/// Every override follows the same shape: pack the arguments as handle strings, ask each loaded mod
/// whether it wants to answer, and fall back to the neutral value when nobody does. The base call is
/// deliberately skipped — <see cref="AbstractModel"/>'s defaults already are that neutral value
/// (0 for additive, 1 for multiplicative, the incoming value for pass-through, true/false for
/// predicates), so calling it would be an extra virtual hop for no gain.
/// </summary>
public sealed class GdCombatHookBridge : AbstractModel
{
	public override bool ShouldReceiveCombatHooks => true;

	public override string ToString()
	{
		return "GdCombatHookBridge";
	}

	// ---------------------------------------------------------------- creatures

	public override bool ShouldClearBlock(Creature creature)
	{
		return GdModRuntime.DispatchBool("should_clear_block", true, GdModRuntime.H(creature));
	}

	public override bool ShouldAllowHitting(Creature creature)
	{
		return GdModRuntime.DispatchBool("should_allow_hitting", true, GdModRuntime.H(creature));
	}

	public override bool ShouldAllowTargeting(Creature target)
	{
		return GdModRuntime.DispatchBool("should_allow_targeting", true, GdModRuntime.H(target));
	}

	public override bool ShouldPowerBeRemovedOnDeath(PowerModel power)
	{
		return GdModRuntime.DispatchBool("should_power_be_removed_on_death", true, GdModRuntime.H(power));
	}

	public override Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
	{
		GdModRuntime.DispatchVoid("after_block_gained",
			GdModRuntime.H(creature), GdHookArgs.Amount(amount), GdHookArgs.Props(props), GdModRuntime.H(cardSource));
		return Task.CompletedTask;
	}

	public override Task AfterBlockCleared(Creature creature)
	{
		GdModRuntime.DispatchVoid("after_block_cleared", GdModRuntime.H(creature));
		return Task.CompletedTask;
	}

	// ---------------------------------------------------------------- cards

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		GdModRuntime.DispatchVoid("before_card_played", GdHookArgs.Play(cardPlay));
		return Task.CompletedTask;
	}

	public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		GdModRuntime.DispatchVoid("after_card_played", GdHookArgs.Play(cardPlay));
		return Task.CompletedTask;
	}

	public override Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		GdModRuntime.DispatchVoid("after_card_played_late", GdHookArgs.Play(cardPlay));
		return Task.CompletedTask;
	}

	public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		GdModRuntime.DispatchVoid("after_card_drawn", GdModRuntime.H(card), Variant.From(fromHandDraw));
		return Task.CompletedTask;
	}

	public override Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
	{
		GdModRuntime.DispatchVoid("after_card_discarded", GdModRuntime.H(card));
		return Task.CompletedTask;
	}

	public override Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
	{
		GdModRuntime.DispatchVoid("after_card_exhausted", GdModRuntime.H(card), Variant.From(causedByEthereal));
		return Task.CompletedTask;
	}

	public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
	{
		return GdModRuntime.DispatchBool("should_play", true, GdModRuntime.H(card), Variant.From((int)autoPlayType));
	}

	public override bool ShouldDraw(Player player, bool fromHandDraw)
	{
		return GdModRuntime.DispatchBool("should_draw", true, GdModRuntime.H(player), Variant.From(fromHandDraw));
	}

	public override bool ShouldFlush(Player player)
	{
		return GdModRuntime.DispatchBool("should_flush", true, GdModRuntime.H(player));
	}

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		return GdModRuntime.DispatchAdditive("modify_hand_draw", 0m,
			GdModRuntime.H(player), GdHookArgs.Amount(count));
	}

	public override int ModifyXValue(CardModel card, int originalValue)
	{
		return (int)GdModRuntime.DispatchAdditive("modify_x_value", originalValue,
			GdModRuntime.H(card), Variant.From(originalValue));
	}

	// ---------------------------------------------------------------- energy / stars / orbs

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		return GdModRuntime.DispatchAdditive("modify_max_energy", 0m,
			GdModRuntime.H(player), GdHookArgs.Amount(amount));
	}

	public override decimal ModifyEnergyGain(Player player, decimal amount)
	{
		return GdModRuntime.DispatchAdditive("modify_energy_gain", 0m,
			GdModRuntime.H(player), GdHookArgs.Amount(amount));
	}

	public override Task AfterEnergyReset(Player player)
	{
		GdModRuntime.DispatchVoid("after_energy_reset", GdModRuntime.H(player));
		return Task.CompletedTask;
	}

	public override Task AfterEnergySpent(CardModel card, int amount)
	{
		GdModRuntime.DispatchVoid("after_energy_spent", GdModRuntime.H(card), Variant.From(amount));
		return Task.CompletedTask;
	}

	public override bool ShouldGainStars(decimal amount, Player player)
	{
		return GdModRuntime.DispatchBool("should_gain_stars", true,
			GdHookArgs.Amount(amount), GdModRuntime.H(player));
	}

	public override decimal ModifyOrbValue(OrbModel orb, decimal value)
	{
		return GdModRuntime.DispatchAdditive("modify_orb_value", 0m,
			GdModRuntime.H(orb), GdHookArgs.Amount(value));
	}

	public override decimal ModifySummonAmount(Player summoner, decimal amount, AbstractModel? source)
	{
		return GdModRuntime.DispatchAdditive("modify_summon_amount", 0m,
			GdModRuntime.H(summoner), GdHookArgs.Amount(amount));
	}

	// ---------------------------------------------------------------- powers

	public override decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
	{
		return GdModRuntime.DispatchAdditive("modify_power_amount_given_additive", 0m,
			GdModRuntime.H(power), GdModRuntime.H(giver), GdHookArgs.Amount(amount),
			GdModRuntime.H(target), GdModRuntime.H(cardSource));
	}

	public override decimal ModifyPowerAmountGivenMultiplicative(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
	{
		return GdModRuntime.DispatchMultiplicative("modify_power_amount_given_multiplicative", 1m,
			GdModRuntime.H(power), GdModRuntime.H(giver), GdHookArgs.Amount(amount),
			GdModRuntime.H(target), GdModRuntime.H(cardSource));
	}

	// ---------------------------------------------------------------- turns / combat end

	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		GdModRuntime.DispatchVoid("after_player_turn_start", GdModRuntime.H(player));
		return Task.CompletedTask;
	}

	public override bool ShouldTakeExtraTurn(Player player)
	{
		return GdModRuntime.DispatchBool("should_take_extra_turn", false, GdModRuntime.H(player));
	}

	public override bool ShouldStopCombatFromEnding()
	{
		return GdModRuntime.DispatchBool("should_stop_combat_from_ending", false);
	}
}

/// <summary>
/// Receives every hook that the game dispatches through the run listener chain
/// (<c>runState.IterateHookListeners(...)</c>, with or without a combat state) and forwards it into
/// GDScript.
///
/// See <see cref="GdCombatHookBridge"/> for why the surface is split across two models instead of
/// one: a single model registered in both subscription lists is appended twice by the
/// <c>RUN_WITH_COMBAT</c> dispatch (run subscribers first, then the recursion into the combat chain),
/// which would double-apply every additive and multiplicative answer a mod gives.
/// </summary>
public sealed class GdRunHookBridge : AbstractModel
{
	public override bool ShouldReceiveCombatHooks => true;

	public override string ToString()
	{
		return "GdRunHookBridge";
	}

	// ---------------------------------------------------------------- damage

	public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return GdModRuntime.DispatchAdditive("modify_damage_additive", 0m,
			GdModRuntime.H(target), GdHookArgs.Amount(amount), GdHookArgs.Props(props),
			GdModRuntime.H(dealer), GdModRuntime.H(cardSource));
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return GdModRuntime.DispatchMultiplicative("modify_damage_multiplicative", 1m,
			GdModRuntime.H(target), GdHookArgs.Amount(amount), GdHookArgs.Props(props),
			GdModRuntime.H(dealer), GdModRuntime.H(cardSource));
	}

	public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		GdModRuntime.DispatchVoid("after_damage_received",
			GdModRuntime.H(target), GdHookArgs.Result(result), GdHookArgs.Props(props),
			GdModRuntime.H(dealer), GdModRuntime.H(cardSource));
		return Task.CompletedTask;
	}

	public override Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		GdModRuntime.DispatchVoid("after_damage_received_late",
			GdModRuntime.H(target), GdHookArgs.Result(result), GdHookArgs.Props(props),
			GdModRuntime.H(dealer), GdModRuntime.H(cardSource));
		return Task.CompletedTask;
	}

	public override bool ShouldDie(Creature creature)
	{
		return GdModRuntime.DispatchBool("should_die", true, GdModRuntime.H(creature));
	}

	public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		GdModRuntime.DispatchVoid("after_death",
			GdModRuntime.H(creature), Variant.From(wasRemovalPrevented), Variant.From(deathAnimLength));
		return Task.CompletedTask;
	}

	public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		GdModRuntime.DispatchVoid("after_current_hp_changed",
			GdModRuntime.H(creature), GdHookArgs.Amount(delta));
		return Task.CompletedTask;
	}

	// ---------------------------------------------------------------- gold / economy / deck

	public override decimal ModifyGoldGained(Player player, decimal amount)
	{
		return GdModRuntime.DispatchAdditive("modify_gold_gained", 0m,
			GdModRuntime.H(player), GdHookArgs.Amount(amount));
	}

	public override Task AfterGoldGained(Player player)
	{
		GdModRuntime.DispatchVoid("after_gold_gained", GdModRuntime.H(player));
		return Task.CompletedTask;
	}

	public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost)
	{
		return GdModRuntime.DispatchAdditive("modify_merchant_price", 0m,
			GdModRuntime.H(player), GdModRuntime.H(entry), GdHookArgs.Amount(cost));
	}

	public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
	{
		return GdModRuntime.DispatchAdditive("modify_rest_site_heal_amount", 0m,
			GdModRuntime.H(creature), GdHookArgs.Amount(amount));
	}

	public override bool ShouldAddToDeck(CardModel card)
	{
		return GdModRuntime.DispatchBool("should_add_to_deck", true, GdModRuntime.H(card));
	}

	// ---------------------------------------------------------------- potions

	public override bool ShouldProcurePotion(PotionModel potion, Player player)
	{
		return GdModRuntime.DispatchBool("should_procure_potion", true,
			GdModRuntime.H(potion), GdModRuntime.H(player));
	}

	public override bool ShouldForcePotionReward(Player player, RoomType roomType)
	{
		return GdModRuntime.DispatchBool("should_force_potion_reward", false,
			GdModRuntime.H(player), Variant.From((int)roomType));
	}

	public override Task AfterPotionUsed(PotionModel potion, Creature? target)
	{
		GdModRuntime.DispatchVoid("after_potion_used",
			GdModRuntime.H(potion), GdModRuntime.H(target));
		return Task.CompletedTask;
	}

	// ---------------------------------------------------------------- run / combat lifecycle

	public override Task BeforeCombatStart()
	{
		GdModRuntime.DispatchVoid("before_combat_start");
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		GdModRuntime.DispatchVoid("after_combat_end", GdModRuntime.H(room));
		return Task.CompletedTask;
	}

	public override Task AfterCombatVictory(CombatRoom room)
	{
		GdModRuntime.DispatchVoid("after_combat_victory", GdModRuntime.H(room));
		return Task.CompletedTask;
	}

	public override Task AfterActEntered()
	{
		GdModRuntime.DispatchVoid("after_act_entered");
		return Task.CompletedTask;
	}

	public override Task AfterRoomEntered(AbstractRoom room)
	{
		GdModRuntime.DispatchVoid("after_room_entered", GdModRuntime.H(room));
		return Task.CompletedTask;
	}
}
