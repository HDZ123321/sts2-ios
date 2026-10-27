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
public sealed class WindmillStrike : WatcherCardModel
{
	private const string RetainedIncreaseKey = "RetainIncrease";

	public WindmillStrike()
		: base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
		((ConstructedCardModel)this).WithTags(new CardTag[1] { CardTag.Strike });
		((ConstructedCardModel)this).WithDamage(7, 3);
		((ConstructedCardModel)this).WithVar("RetainIncrease", 4, 1);
		((ConstructedCardModel)this).WithKeywords(new CardKeyword[1] { CardKeyword.Retain });
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Target != null)
		{
			await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
		}
	}

	public override Task AfterFlush(PlayerChoiceContext choiceContext, Player player, IReadOnlyCollection<CardModel> flushedCards, IReadOnlyCollection<CardModel> retainedCards)
	{
		if (!retainedCards.Contains((CardModel)(object)this))
		{
			return Task.CompletedTask;
		}
		((CardModel)(object)this).DynamicVars.Damage.UpgradeValueBy(((CardModel)(object)this).DynamicVars["RetainIncrease"].BaseValue);
		return Task.CompletedTask;
	}
}
