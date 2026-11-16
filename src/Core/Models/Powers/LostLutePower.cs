using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace MegaCrit.Sts2.Core.Models.Powers;

public sealed class LostLutePower : PowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new global::_003C_003Ez__ReadOnlyArray<IHoverTip>(new IHoverTip[3]
	{
		HoverTipFactory.Static(StaticHoverTip.WahMelody),
		HoverTipFactory.Static(StaticHoverTip.ChoMelody),
		HoverTipFactory.Static(StaticHoverTip.KiMelody)
	});

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner != base.Owner.Player)
		{
			return;
		}
		int num = 0;
		if (cardPlay.Card.Tags.Contains(CardTag.Wah))
		{
			bool flag = CombatManager.Instance.History.CardPlaysStarted.Count((CardPlayStartedEntry e) => e.Actor == base.Owner && e.HappenedThisTurn(base.CombatState) && e.CardPlay.Card.Tags.Contains(CardTag.Wah)) == 1;
			num += (flag ? 1 : 0);
		}
		if (cardPlay.Card.Tags.Contains(CardTag.Cho))
		{
			bool flag2 = CombatManager.Instance.History.CardPlaysStarted.Count((CardPlayStartedEntry e) => e.Actor == base.Owner && e.HappenedThisTurn(base.CombatState) && e.CardPlay.Card.Tags.Contains(CardTag.Cho)) == 1;
			num += (flag2 ? 1 : 0);
		}
		if (cardPlay.Card.Tags.Contains(CardTag.Ki))
		{
			bool flag3 = CombatManager.Instance.History.CardPlaysStarted.Count((CardPlayStartedEntry e) => e.Actor == base.Owner && e.HappenedThisTurn(base.CombatState) && e.CardPlay.Card.Tags.Contains(CardTag.Ki)) == 1;
			num += (flag3 ? 1 : 0);
		}
		if (num > 0)
		{
			Flash();
			await CardPileCmd.AutoPlayFromDrawPile(choiceContext, base.Owner.Player, num * base.Amount, CardPilePosition.Top, forceExhaust: false);
		}
	}
}
