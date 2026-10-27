using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;

namespace Watcher.Code.Cards.Rare;

[Pool(typeof(WatcherCardPool))]
public sealed class ScrawlWatcher : WatcherCardModel
{
	public ScrawlWatcher()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.None)
	{
		((ConstructedCardModel)this).WithKeywords(new CardKeyword[1] { CardKeyword.Exhaust });
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (((CardModel)(object)this).Owner.PlayerCombatState != null)
		{
			await CardPileCmd.Draw(choiceContext, 10 - ((CardModel)(object)this).Owner.PlayerCombatState.Hand.Cards.Count, ((CardModel)(object)this).Owner);
		}
	}
}
