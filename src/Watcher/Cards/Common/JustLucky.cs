using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;

namespace Watcher.Code.Cards.Common;

[Pool(typeof(WatcherCardPool))]
public sealed class JustLucky : WatcherCardModel
{
	public JustLucky()
		: base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
		WithScry(1, 1);
		((ConstructedCardModel)this).WithBlock(2, 1);
		((ConstructedCardModel)this).WithDamage(3, 1);
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await ScryCmd.Execute(choiceContext, (CardModel)(object)this);
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
	}
}
