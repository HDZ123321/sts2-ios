using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;

namespace Watcher.Code.Cards.Rare;

[Pool(typeof(WatcherCardPool))]
public sealed class Ragnarok : WatcherCardModel
{
	public Ragnarok()
		: base(3, CardType.Attack, CardRarity.Rare, TargetType.RandomEnemy)
	{
		((ConstructedCardModel)this).WithDamage(5, 1);
		((ConstructedCardModel)this).WithVars(new DynamicVar[1] { DynamicVarExtensions.WithUpgrade<RepeatVar>(new RepeatVar(5), 1m) });
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitCount(((CardModel)(object)this).DynamicVars.Repeat.IntValue).WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
	}
}
