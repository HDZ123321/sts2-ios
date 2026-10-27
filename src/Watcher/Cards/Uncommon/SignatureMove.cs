using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;

namespace Watcher.Code.Cards.Uncommon;

[Pool(typeof(WatcherCardPool))]
public sealed class SignatureMove : WatcherCardModel
{
	protected override bool IsPlayable
	{
		get
		{
			if (((CardModel)(object)this).Owner.PlayerCombatState == null)
			{
				return false;
			}
			return ((CardModel)(object)this).Owner.PlayerCombatState.Hand.Cards.Count((CardModel card) => card.Type == CardType.Attack && (object)card != this) == 0;
		}
	}

	public SignatureMove()
		: base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
		((ConstructedCardModel)this).WithDamage(30, 10);
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(choiceContext);
	}
}
