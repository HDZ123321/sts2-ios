using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class TheDevil : CardModel
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => new global::_003C_003Ez__ReadOnlySingleElementList<CardKeyword>(CardKeyword.Exhaust);

	public TheDevil()
		: base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		List<CardModel> cards = PileType.Hand.GetPile(base.Owner).Cards.ToList();
		foreach (CardModel item in cards)
		{
			await CardCmd.Exhaust(choiceContext, item);
		}
		await CardPileCmd.AutoPlayFromDrawPile(choiceContext, base.Owner, cards.Count, CardPilePosition.Top, forceExhaust: false, delegate(CardModel c)
		{
			if (base.IsUpgraded)
			{
				CardCmd.Upgrade(c);
			}
		});
	}

	protected override void OnUpgrade()
	{
	}
}
