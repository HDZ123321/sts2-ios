using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Commands;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;

namespace Watcher.Code.Cards.Common;

[Pool(typeof(WatcherCardPool))]
public sealed class CutThroughFate : WatcherCardModel
{
	public CutThroughFate()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
		((ConstructedCardModel)this).WithDamage(7, 2);
		WithScry(2, 1);
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
		await ScryCmd.Execute(choiceContext, (CardModel)(object)this);
		await CardPileCmd.Draw(choiceContext, 1m, ((CardModel)(object)this).Owner);
	}
}
