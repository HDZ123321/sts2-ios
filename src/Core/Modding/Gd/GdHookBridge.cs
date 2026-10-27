using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Modding.Gd;

/// <summary>
/// A model that exists only to receive gameplay hooks and forward them into GDScript.
///
/// It is never registered in the ModelDb and never enters a card pool — it is handed to
/// <see cref="ModHelper.SubscribeForCombatStateHooks"/> and
/// <see cref="ModHelper.SubscribeForRunStateHooks"/>, which append it to the listener list of every
/// hook dispatch. That makes the whole 183-method override surface of <see cref="AbstractModel"/>
/// reachable from a scripting language that has no way to subclass it.
///
/// Every override follows the same shape: pack the arguments as handle strings, ask each loaded mod
/// whether it wants to answer, and fall back to the base value when nobody does. The base call is
/// deliberately skipped — <see cref="AbstractModel"/>'s defaults already are the neutral value
/// (0 for additive, 1 for multiplicative, the incoming value for pass-through, true/false for
/// predicates), so calling it would be an extra virtual hop for no gain.
/// </summary>
public sealed class GdHookBridge : AbstractModel
{
	/// <summary>
	/// Combat hooks are the interesting ones for a gameplay mod, and this bridge is only ever
	/// registered in combat/runs, so both halves of the listener dispatch should reach it.
	/// </summary>
	public override bool ShouldReceiveCombatHooks => true;

	public override string ToString()
	{
		return "GdHookBridge";
	}

	private static Variant Props(ValueProp props)
	{
		return Variant.From((int)props);
	}

	private static Variant Result(DamageResult result)
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

	private static Variant Play(CardPlay play)
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

	// ---------------------------------------------------------------- damage

	public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return GdModRuntime.DispatchAdditive("modify_damage_additive", 0m,
			GdModRuntime.H(target), Variant.From((double)amount), Props(props), GdModRuntime.H(dealer), GdModRuntime.H(cardSource));
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		return GdModRuntime.DispatchMultiplicative("modify_damage_multiplicative", 1m,
			GdModRuntime.H(target), Variant.From((double)amount), Props(props), GdModRuntime.H(dealer), GdModRuntime.H(cardSource));
	}

	public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		GdModRuntime.DispatchVoid("after_damage_received",
			GdModRuntime.H(target), Result(result), Props(props), GdModRuntime.H(dealer), GdModRuntime.H(cardSource));
		return Task.CompletedTask;
	}

	public override Task AfterDamageReceivedLate(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		GdModRuntime.DispatchVoid("after_damage_received_late",
			GdModRuntime.H(target), Result(result), Props(props), GdModRuntime.H(dealer), GdModRuntime.H(cardSource));
		return Task.CompletedTask;
	}

	public override Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
	{
		GdModRuntime.DispatchVoid("after_block_gained",
			GdModRuntime.H(creature), Variant.From((double)amount), Props(props), GdModRuntime.H(cardSource));
		return Task.CompletedTask;
	}

	public override Task AfterBlockCleared(Creature creature)
	{
		GdModRuntime.DispatchVoid("after_block_cleared", GdModRuntime.H(creature));
		return Task.CompletedTask;
	}

	public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		GdModRuntime.DispatchVoid("after_current_hp_changed", GdModRuntime.H(creature), Variant.From((double)delta));
		return Task.CompletedTask;
	}

	public override bool ShouldDie(Creature creature)
	{
		return GdModRuntime.DispatchBool("should_die", true, GdModRuntime.H(creature));
	}

	public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		GdModRuntime.DispatchVoid("after_death", GdModRuntime.H(creature), Variant.From(wasRemovalPrevented));
		return Task.CompletedTask;
	}

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

	// ---------------------------------------------------------------- cards

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		GdModRuntime.DispatchVoid("before_card_played", Play(cardPlay));
		return Task.CompletedTask;
	}

	public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		GdModRuntime.DispatchVoid("after_card_played", Play(cardPlay));
		return Task.CompletedTask;
	}

	public override Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		GdModRuntime.DispatchVoid("after_card_played_late", Play(cardPlay));
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

	public override bool ShouldAddToDeck(CardModel card)
	{
		return GdModRuntime.DispatchBool("should_add_to_deck", true, GdModRuntime.H(card));
	}

	public override decimal ModifyHandDraw(Player player, decimal count)
	{
		return GdModRuntime.DispatchAdditive("modify_hand_draw", 0m, GdModRuntime.H(player), Variant.From((double)count));
	}

	public override int ModifyXValue(CardModel card, int originalValue)
	{
		return (int)GdModRuntime.DispatchAdditive("modify_x_value", originalValue, GdModRuntime.H(card), Variant.From(originalValue));
	}

	public override bool ShouldFlush(Player player)
	{
		return GdModRuntime.DispatchBool("should_flush", true, GdModRuntime.H(player));
	}

	// ---------------------------------------------------------------- energy / stars / gold

	public override decimal ModifyMaxEnergy(Player player, decimal amount)
	{
		return GdModRuntime.DispatchAdditive("modify_max_energy", 0m, GdModRuntime.H(player), Variant.From((double)amount));
	}

	public override decimal ModifyEnergyGain(Player player, decimal amount)
	{
		return GdModRuntime.DispatchAdditive("modify_energy_gain", 0m, GdModRuntime.H(player), Variant.From((double)amount));
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
		return GdModRuntime.DispatchBool("should_gain_stars", true, Variant.From((double)amount), GdModRuntime.H(player));
	}

	public override decimal ModifyGoldGained(Player player, decimal amount)
	{
		return GdModRuntime.DispatchAdditive("modify_gold_gained", 0m, GdModRuntime.H(player), Variant.From((double)amount));
	}

	public override Task AfterGoldGained(Player player)
	{
		GdModRuntime.DispatchVoid("after_gold_gained", GdModRuntime.H(player));
		return Task.CompletedTask;
	}

	public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost)
	{
		return GdModRuntime.DispatchAdditive("modify_merchant_price", 0m, GdModRuntime.H(player), Variant.From((double)cost));
	}

	public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
	{
		return GdModRuntime.DispatchAdditive("modify_rest_site_heal_amount", 0m, GdModRuntime.H(creature), Variant.From((double)amount));
	}

	public override decimal ModifyOrbValue(OrbModel orb, decimal value)
	{
		return GdModRuntime.DispatchAdditive("modify_orb_value", 0m, GdModRuntime.H(orb), Variant.From((double)value));
	}

	public override decimal ModifyPowerAmountGivenAdditive(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
	{
		return GdModRuntime.DispatchAdditive("modify_power_amount_given_additive", 0m,
			GdModRuntime.H(power), GdModRuntime.H(giver), Variant.From((double)amount), GdModRuntime.H(target), GdModRuntime.H(cardSource));
	}

	public override decimal ModifyPowerAmountGivenMultiplicative(PowerModel power, Creature giver, decimal amount, Creature? target, CardModel? cardSource)
	{
		return GdModRuntime.DispatchMultiplicative("modify_power_amount_given_multiplicative", 1m,
			GdModRuntime.H(power), GdModRuntime.H(giver), Variant.From((double)amount), GdModRuntime.H(target), GdModRuntime.H(cardSource));
	}

	public override decimal ModifySummonAmount(Player summoner, decimal amount, AbstractModel? source)
	{
		return GdModRuntime.DispatchAdditive("modify_summon_amount", 0m, GdModRuntime.H(summoner), Variant.From((double)amount));
	}

	public override bool ShouldPowerBeRemovedOnDeath(PowerModel power)
	{
		return GdModRuntime.DispatchBool("should_power_be_removed_on_death", true, GdModRuntime.H(power));
	}

	public override bool ShouldProcurePotion(PotionModel potion, Player player)
	{
		return GdModRuntime.DispatchBool("should_procure_potion", true, GdModRuntime.H(potion), GdModRuntime.H(player));
	}

	public override bool ShouldForcePotionReward(Player player, RoomType roomType)
	{
		return GdModRuntime.DispatchBool("should_force_potion_reward", false, GdModRuntime.H(player), Variant.From((int)roomType));
	}

	public override bool ShouldTakeExtraTurn(Player player)
	{
		return GdModRuntime.DispatchBool("should_take_extra_turn", false, GdModRuntime.H(player));
	}

	public override bool ShouldStopCombatFromEnding()
	{
		return GdModRuntime.DispatchBool("should_stop_combat_from_ending", false);
	}

	// ---------------------------------------------------------------- combat / run lifecycle

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

	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		GdModRuntime.DispatchVoid("after_player_turn_start", GdModRuntime.H(player));
		return Task.CompletedTask;
	}

	public override Task AfterPotionUsed(PotionModel potion, Creature? target)
	{
		GdModRuntime.DispatchVoid("after_potion_used", GdModRuntime.H(potion), GdModRuntime.H(target));
		return Task.CompletedTask;
	}
}
