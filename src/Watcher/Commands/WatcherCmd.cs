using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Watcher.Code.Commands;

public class WatcherCmd
{
	public static async Task<CardModel?> GiveCard<T>(Player player, PileType pileType, CardPilePosition pos = CardPilePosition.Bottom, float animationTime = 0.6f, CardPreviewStyle animationStyle = CardPreviewStyle.HorizontalLayout, bool upgraded = false, bool skipAnimation = false) where T : CardModel
	{
		ICombatState combatState = player.Creature.CombatState;
		if (combatState == null)
		{
			return null;
		}
		CardModel card = combatState.CreateCard(ModelDb.Card<T>(), player);
		if (upgraded)
		{
			CardCmd.Upgrade(card);
		}
		CardPileAddResult result = await CardPileCmd.AddGeneratedCardToCombat(card, pileType, player, pos);
		if (skipAnimation)
		{
			return card;
		}
		CardCmd.PreviewCardPileAdd(result, animationTime, animationStyle);
		return card;
	}

	public static async Task GiveCards<T>(Player player, int amount, PileType pileType, CardPilePosition pos = CardPilePosition.Bottom, float animationTime = 0.6f, CardPreviewStyle animationStyle = CardPreviewStyle.HorizontalLayout, bool upgraded = false, bool skipAnimation = false) where T : CardModel
	{
		List<CardModel> list = new List<CardModel>();
		ICombatState combatState = player.Creature.CombatState;
		if (combatState == null)
		{
			return;
		}
		for (int i = 0; i < amount; i++)
		{
			CardModel cardModel = combatState.CreateCard(ModelDb.Card<T>(), player);
			if (upgraded)
			{
				CardCmd.Upgrade(cardModel);
			}
			list.Add(cardModel);
		}
		IReadOnlyList<CardPileAddResult> results = await CardPileCmd.AddGeneratedCardsToCombat(list, pileType, player, pos);
		if (!skipAnimation && pileType != PileType.Hand)
		{
			CardCmd.PreviewCardPileAdd(results, animationTime, animationStyle);
		}
	}
}
