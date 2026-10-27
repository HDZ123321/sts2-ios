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
public sealed class EmptyFist : WatcherCardModel
{
	public EmptyFist()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
		((ConstructedCardModel)this).WithDamage(9, 5);
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitFx("vfx/vfx_attack_slash").Execute(ctx);
		await StanceCmd.ExitStance(ctx, ((CardModel)(object)this).Owner, cardPlay.Card);
	}
}
