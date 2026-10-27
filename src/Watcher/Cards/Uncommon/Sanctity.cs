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

namespace Watcher.Code.Cards.Uncommon;

[Pool(typeof(WatcherCardPool))]
public sealed class Sanctity : WatcherCardModel
{
	protected override bool ShouldGlowGoldInternal => WasLastCardPlayedSkill;

	private bool WasLastCardPlayedSkill
	{
		get
		{
			CardPlayStartedEntry cardPlayStartedEntry = CombatManager.Instance.History.CardPlaysStarted.LastOrDefault((CardPlayStartedEntry e) => e.CardPlay.Card.Owner == ((CardModel)(object)this).Owner && (object)e.CardPlay.Card != this);
			if (cardPlayStartedEntry == null)
			{
				return false;
			}
			return cardPlayStartedEntry.CardPlay.Card.Type == CardType.Skill;
		}
	}

	public Sanctity()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
		((ConstructedCardModel)this).WithBlock(6, 3);
		((ConstructedCardModel)this).WithCards(2, 0);
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		if (WasLastCardPlayedSkill)
		{
			await CommonActions.Draw((CardModel)(object)this, ctx);
		}
	}
}
