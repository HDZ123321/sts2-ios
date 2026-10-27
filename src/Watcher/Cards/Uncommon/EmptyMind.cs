using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Commands;

namespace Watcher.Code.Cards.Uncommon;

[Pool(typeof(WatcherCardPool))]
public sealed class EmptyMind : WatcherCardModel
{
	public EmptyMind()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
		((ConstructedCardModel)this).WithCards(2, 1);
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Draw((CardModel)(object)this, ctx);
		await StanceCmd.ExitStance(ctx, ((CardModel)(object)this).Owner, cardPlay.Card);
	}
}
