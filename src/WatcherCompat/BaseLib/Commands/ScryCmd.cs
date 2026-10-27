using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Hooks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Commands;

/// <summary>
/// Scry: look at the top N cards of the draw pile, discard any number of them,
/// and put the rest back on top in the same order.
/// <para>
/// The base game has no scry system, so this is a native implementation.  It
/// reuses <see cref="T:MegaCrit.Sts2.Core.Commands.CardSelectCmd"/> for the
/// selection screen so it behaves like every other "choose a card" flow.
/// </para>
/// </summary>
public static class ScryCmd
{
	public static async Task Execute(PlayerChoiceContext choiceContext, CardModel source)
	{
		ArgumentNullException.ThrowIfNull(source);
		Player? owner = source.Owner;
		if (owner?.PlayerCombatState == null)
		{
			return;
		}
		int requested = source.DynamicVars.ContainsKey("Scry") ? source.DynamicVars["Scry"].IntValue : 0;
		await Execute(choiceContext, owner, requested);
	}

	public static async Task Execute(PlayerChoiceContext choiceContext, Player player, int amount)
	{
		if (player.PlayerCombatState == null || amount <= 0)
		{
			return;
		}
		int scryAmount = ModifyAmount(player, amount);
		if (scryAmount <= 0)
		{
			return;
		}
		CardPile drawPile = player.PlayerCombatState.DrawPile;
		List<CardModel> seen = drawPile.Cards.Take(Math.Min(scryAmount, drawPile.Cards.Count)).ToList();
		if (seen.Count == 0)
		{
			return;
		}

		List<CardModel> discarded = new List<CardModel>();
		CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 0, seen.Count)
		{
			Cancelable = true
		};
		IEnumerable<CardModel> chosen = await CardSelectCmd.FromSimpleGrid(choiceContext, seen, player, prefs);
		foreach (CardModel card in chosen.ToList())
		{
			if (seen.Remove(card))
			{
				discarded.Add(card);
			}
		}

		foreach (CardModel card in discarded)
		{
			CardPile? discardPile = CardPile.Get(PileType.Discard, player);
			discardPile?.AddInternal(card, -1, true);
			if (drawPile.Cards.Contains(card))
			{
				drawPile.RemoveInternal(card, true);
			}
		}

		await Dispatch(choiceContext, player, scryAmount, discarded.Count, seen, discarded);
	}

	private static int ModifyAmount(Player player, int amount)
	{
		int current = amount;
		ICombatState? combatState = player.Creature.CombatState;
		if (combatState == null)
		{
			return current;
		}
		foreach (RelicModel relic in player.Relics)
		{
			if (relic is IModifyScryAmount modifier)
			{
				current = modifier.ModifyScryAmount(player, current);
			}
		}
		try
		{
			foreach (AbstractModel model in combatState.IterateHookListeners())
			{
				if (model is IModifyScryAmount modifier2)
				{
					current = modifier2.ModifyScryAmount(player, current);
				}
			}
		}
		catch (InvalidOperationException)
		{
			// Not in combat after all; the relic pass above is enough.
		}
		return current;
	}

	private static async Task Dispatch(PlayerChoiceContext choiceContext, Player player, int scryAmount, int discardAmount, List<CardModel> seen, List<CardModel> discarded)
	{
		ICombatState? combatState = player.Creature.CombatState;
		if (combatState == null)
		{
			return;
		}
		foreach (AbstractModel model in combatState.IterateHookListeners().ToList())
		{
			if (model is IAfterScryed after)
			{
				await after.AfterScryed(choiceContext, player, scryAmount, discardAmount, seen, discarded);
			}
		}
	}
}
