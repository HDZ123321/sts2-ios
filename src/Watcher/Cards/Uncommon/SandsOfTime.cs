using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;

namespace Watcher.Code.Cards.Uncommon;

[Pool(typeof(WatcherCardPool))]
public sealed class SandsOfTime : WatcherCardModel
{
	public override bool ShouldReceiveCombatHooks => true;

	public SandsOfTime()
		: base(4, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
		((ConstructedCardModel)this).WithDamage(20, 6);
		((ConstructedCardModel)this).WithKeywords(new CardKeyword[1] { CardKeyword.Retain });
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(choiceContext);
	}

	public override Task AfterFlush(PlayerChoiceContext choiceContext, Player player, IReadOnlyCollection<CardModel> flushedCards, IReadOnlyCollection<CardModel> retainedCards)
	{
		if (!retainedCards.Contains((CardModel)(object)this))
		{
			return Task.CompletedTask;
		}
		int withModifiers = ((CardModel)(object)this).EnergyCost.GetWithModifiers(CostModifiers.Local);
		if (withModifiers > 0)
		{
			((CardModel)(object)this).EnergyCost.SetThisCombat(withModifiers - 1);
		}
		return Task.CompletedTask;
	}
}
