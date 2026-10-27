using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;

namespace Watcher.Code.Cards.Common;

[Pool(typeof(WatcherCardPool))]
public sealed class FollowUp : WatcherCardModel
{
	protected override bool ShouldGlowGoldInternal => WasLastCardPlayedAttack;

	private bool WasLastCardPlayedAttack
	{
		get
		{
			CardPlayStartedEntry cardPlayStartedEntry = CombatManager.Instance.History.CardPlaysStarted.LastOrDefault((CardPlayStartedEntry e) => e.CardPlay.Card.Owner == ((CardModel)(object)this).Owner && (object)e.CardPlay.Card != this);
			if (cardPlayStartedEntry == null)
			{
				return false;
			}
			return cardPlayStartedEntry.CardPlay.Card.Type == CardType.Attack;
		}
	}

	public FollowUp()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
		((ConstructedCardModel)this).WithDamage(7, 4);
		((ConstructedCardModel)this).WithEnergy(1, 0);
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitFx("vfx/vfx_attack_slash").Execute(choiceContext);
		if (WasLastCardPlayedAttack)
		{
			((CardModel)(object)this).Owner.PlayerCombatState.GainEnergy(1m);
		}
	}
}
