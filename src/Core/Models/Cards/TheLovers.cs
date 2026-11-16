using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class TheLovers : CardModel
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => new global::_003C_003Ez__ReadOnlySingleElementList<CardKeyword>(CardKeyword.Exhaust);

	public TheLovers()
		: base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		List<CardPoolModel> list = base.Owner.UnlockState.CharacterCardPools.ToList();
		if (list.Count > 1)
		{
			list.Remove(base.Owner.Character.CardPool);
		}
		IEnumerable<CardModel> cards = list.SelectMany((CardPoolModel c) => c.GetUnlockedCards(base.Owner.UnlockState, base.Owner.RunState.CardMultiplayerConstraint));
		int count = CardPile.MaxCardsInHand - CardPile.GetCards(base.Owner, PileType.Hand).Count();
		IEnumerable<CardModel> forCombat = CardFactory.GetForCombat(base.Owner, cards, count, base.Owner.RunState.Rng.CombatCardGeneration);
		if (base.IsUpgraded)
		{
			foreach (CardModel item in forCombat)
			{
				if (base.IsUpgraded)
				{
					CardCmd.Upgrade(item);
				}
			}
		}
		await CardPileCmd.AddGeneratedCardsToCombat(forCombat, PileType.Hand, base.Owner);
	}
}
