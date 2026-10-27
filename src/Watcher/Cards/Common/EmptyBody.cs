using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Commands;

namespace Watcher.Code.Cards.Common;

[Pool(typeof(WatcherCardPool))]
public sealed class EmptyBody : WatcherCardModel
{
	public EmptyBody()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
		((ConstructedCardModel)this).WithBlock(7, 3);
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await StanceCmd.ExitStance(ctx, ((CardModel)(object)this).Owner, cardPlay.Card);
	}
}
