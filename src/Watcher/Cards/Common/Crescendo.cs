using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Commands;
using Watcher.Code.Stances;

namespace Watcher.Code.Cards.Common;

[Pool(typeof(WatcherCardPool))]
public sealed class Crescendo : WatcherCardModel
{
	public Crescendo()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.None)
	{
		WithStanceTip<WrathStance>();
		((ConstructedCardModel)this).WithKeywords(new CardKeyword[2]
		{
			CardKeyword.Retain,
			CardKeyword.Exhaust
		});
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await StanceCmd.EnterWrath(ctx, ((CardModel)(object)this).Owner, cardPlay.Card);
	}
}
