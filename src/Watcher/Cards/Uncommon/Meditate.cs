using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Commands;
using Watcher.Code.Stances;

namespace Watcher.Code.Cards.Uncommon;

[Pool(typeof(WatcherCardPool))]
public sealed class Meditate : WatcherCardModel
{
	public Meditate()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
		((ConstructedCardModel)this).WithCards(1, 1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(CardKeyword.Retain));
		WithStanceTip<CalmStance>();
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		List<CardModel> list = PileType.Discard.GetPile(((CardModel)(object)this).Owner).Cards.ToList();
		if (list.Count != 0)
		{
			int num = Math.Min(((CardModel)(object)this).DynamicVars.Cards.IntValue, list.Count);
			CardSelectorPrefs prefs = new CardSelectorPrefs(((CardModel)(object)this).SelectionScreenPrompt, num, num)
			{
				Cancelable = false,
				PretendCardsCanBePlayed = true
			};
			foreach (CardModel card in await CardSelectCmd.FromSimpleGrid(ctx, list, ((CardModel)(object)this).Owner, prefs))
			{
				await CardPileCmd.Add(card, PileType.Hand);
				card.GiveSingleTurnRetain();
			}
		}
		await StanceCmd.EnterCalm(ctx, ((CardModel)(object)this).Owner, cardPlay.Card);
		PlayerCmd.EndTurn(((CardModel)(object)this).Owner, canBackOut: false);
	}
}
