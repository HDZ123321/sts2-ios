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
public sealed class Perseverance : WatcherCardModel
{
	private const string IncreaseKey = "Increase";

	public Perseverance()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
		((ConstructedCardModel)this).WithBlock(5, 2);
		((ConstructedCardModel)this).WithVar("Increase", 2, 1);
		((ConstructedCardModel)this).WithKeywords(new CardKeyword[1] { CardKeyword.Retain });
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
	}

	public override Task AfterFlush(PlayerChoiceContext choiceContext, Player player, IReadOnlyCollection<CardModel> flushedCards, IReadOnlyCollection<CardModel> retainedCards)
	{
		if (!retainedCards.Contains((CardModel)(object)this))
		{
			return Task.CompletedTask;
		}
		((CardModel)(object)this).DynamicVars.Block.UpgradeValueBy(((CardModel)(object)this).DynamicVars["Increase"].BaseValue);
		return Task.CompletedTask;
	}
}
