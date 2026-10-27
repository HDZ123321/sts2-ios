using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using Watcher.Code.Abstract;

namespace Watcher.Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public sealed class Expunger : WatcherCardModel
{
	public Expunger()
		: base(1, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy)
	{
		((ConstructedCardModel)this).WithDamage(9, 6);
		((ConstructedCardModel)this).WithVars(new DynamicVar[1]
		{
			new RepeatVar(-1)
		});
	}

	protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
	{
		if (play.Target != null)
		{
			int intValue = ((CardModel)(object)this).DynamicVars.Repeat.IntValue;
			if (intValue > 0)
			{
				await CommonActions.CardAttack((CardModel)(object)this, play, 1, (string)null, (string)null, (string)null).WithHitCount(intValue).WithHitFx("vfx/vfx_attack_slash")
					.Execute(context);
			}
		}
	}
}
