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

namespace Watcher.Code.Cards.Basic;

[Pool(typeof(WatcherCardPool))]
public sealed class Vigilance : WatcherCardModel
{
	public Vigilance()
		: base(2, CardType.Skill, CardRarity.Basic, TargetType.Self)
	{
		((ConstructedCardModel)this).WithBlock(8, 4);
		WithStanceTip<CalmStance>();
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await StanceCmd.EnterCalm(ctx, ((CardModel)(object)this).Owner, cardPlay.Card);
	}
}
