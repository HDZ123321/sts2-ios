using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Commands;
using Watcher.Code.Extensions;
using Watcher.Code.Stances;

namespace Watcher.Code.Cards.Uncommon;

[Pool(typeof(WatcherCardPool))]
public sealed class InnerPeace : WatcherCardModel
{
	public InnerPeace()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
		((ConstructedCardModel)this).WithCards(3, 1);
		WithStanceTip<CalmStance>();
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)(object)this).Owner.IsInWatcherStance<CalmStance>())
		{
			await CommonActions.Draw((CardModel)(object)this, ctx);
		}
		else
		{
			await StanceCmd.EnterCalm(ctx, ((CardModel)(object)this).Owner, cardPlay.Card);
		}
	}
}
