using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace BaseLib.Utils;

/// <summary>
/// The four things almost every card does, spelled once.
/// </summary>
public static class CommonActions
{
	/// <summary>
	/// Sets up an attack for <paramref name="card"/>.  A card carrying a
	/// <c>CalculatedDamage</c> var attacks with that instead of the flat damage.
	/// </summary>
	public static AttackCommand CardAttack(CardModel card, CardPlay cardPlay, int hitCount, string? vfx = null, string? sfx = null, string? tmpSfx = null)
	{
		ArgumentNullException.ThrowIfNull(card);
		ArgumentNullException.ThrowIfNull(cardPlay);
		AttackCommand command;
		if (card.DynamicVars.TryGetValue("CalculatedDamage", out DynamicVar calculated) && calculated is CalculatedDamageVar calculatedDamage)
		{
			command = DamageCmd.Attack(calculatedDamage);
		}
		else if (card.DynamicVars.TryGetValue("Damage", out DynamicVar flat) && flat is DamageVar damageVar)
		{
			command = DamageCmd.Attack(damageVar.BaseValue);
		}
		else
		{
			Log.Warn($"{card.Id} declares neither a Damage nor a CalculatedDamage var, attacking for nothing.");
			command = DamageCmd.Attack(0m);
		}
		command.FromCard(card);
		if (cardPlay.Target != null)
		{
			command.Targeting(cardPlay.Target);
		}
		else
		{
			command.TargetingAllOpponents(card.Owner.Creature.CombatState);
		}
		if (hitCount > 1)
		{
			command.WithHitCount(hitCount);
		}
		if (vfx != null || sfx != null || tmpSfx != null)
		{
			command.WithHitFx(vfx, sfx, tmpSfx);
		}
		return command;
	}

	/// <summary>
	/// Cards built with <c>WithCalculatedBlock</c> carry a <c>CalculatedBlock</c> var instead of
	/// the plain <c>Block</c> var, so asking for <c>DynamicVars.Block</c> would throw.  Prefer
	/// whichever var the card actually declares.
	/// </summary>
	public static Task<decimal> CardBlock(CardModel card, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(card);
		DynamicVarSet vars = card.DynamicVars;
		if (vars.TryGetValue("CalculatedBlock", out DynamicVar calculated) && calculated is CalculatedBlockVar calculatedBlock)
		{
			return CreatureCmd.GainBlock(card.Owner.Creature, calculatedBlock.Calculate(cardPlay.Target), calculatedBlock.Props, cardPlay);
		}
		if (vars.TryGetValue("Block", out DynamicVar block) && block is BlockVar blockVar)
		{
			return CreatureCmd.GainBlock(card.Owner.Creature, blockVar, cardPlay);
		}
		Log.Warn($"{card.Id} declares neither a Block nor a CalculatedBlock var, gaining nothing.");
		return Task.FromResult(0m);
	}

	/// <summary>Applies the card's matching power var to the player who played it.</summary>
	public static Task ApplySelf<T>(PlayerChoiceContext choiceContext, CardModel card, bool silent = false) where T : PowerModel
	{
		return Apply<T>(choiceContext, card.Owner.Creature, card, silent);
	}

	/// <summary>Applies the card's matching power var to whatever the card targeted.</summary>
	public static Task Apply<T>(PlayerChoiceContext choiceContext, CardModel card, CardPlay cardPlay, bool silent = false) where T : PowerModel
	{
		Creature target = cardPlay.Target ?? card.Owner.Creature;
		return Apply<T>(choiceContext, target, card, silent);
	}

	public static Task ApplySelf<T>(PlayerChoiceContext choiceContext, CardModel card, decimal amount, bool silent = false) where T : PowerModel
	{
		ArgumentNullException.ThrowIfNull(card);
		return PowerCmd.Apply<T>(choiceContext, card.Owner.Creature, amount, card.Owner.Creature, card, silent);
	}

	public static Task Apply<T>(PlayerChoiceContext choiceContext, Creature target, CardModel card, bool silent = false) where T : PowerModel
	{
		decimal amount = card.DynamicVars.Power<T>().BaseValue;
		return PowerCmd.Apply<T>(choiceContext, target, amount, card.Owner.Creature, card, silent);
	}

	public static Task Apply<T>(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets, DynamicVarSource source, bool silent = false) where T : PowerModel
	{
		decimal amount = 0m;
		if (source.Vars != null)
		{
			amount = source.Vars.Power<T>().BaseValue;
		}
		return PowerCmd.Apply<T>(choiceContext, targets, amount, null, null, silent);
	}

	public static Task Draw(CardModel card, PlayerChoiceContext choiceContext, int count = 1)
	{
		ArgumentNullException.ThrowIfNull(card);
		return CardPileCmd.Draw(choiceContext, count, card.Owner);
	}
}
