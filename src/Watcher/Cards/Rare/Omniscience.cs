using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Compatibility;

namespace Watcher.Code.Cards.Rare;

[Pool(typeof(WatcherCardPool))]
public sealed class Omniscience : WatcherCardModel
{
	public Omniscience()
		: base(4, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
		((ConstructedCardModel)this).WithKeywords(new CardKeyword[1] { CardKeyword.Exhaust });
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		Omniscience source = this;
		await CreatureCmd.TriggerAnim(((CardModel)(object)source).Owner.Creature, "Cast", ((CardModel)(object)source).Owner.Character.CastAnimDelay);
		CardSelectorPrefs prefs = new CardSelectorPrefs(((CardModel)(object)source).SelectionScreenPrompt, 1)
		{
			PretendCardsCanBePlayed = true
		};
		List<CardModel> cardsIn = PileType.Draw.GetPile(((CardModel)(object)this).Owner).Cards.ToList();
		CardModel card = (await CardSelectCmd.FromSimpleGrid(choiceContext, cardsIn, ((CardModel)(object)this).Owner, prefs)).FirstOrDefault();
		if (card != null)
		{
			CardModel dupe = card.CreateDupeCompat();
			await CardCmd.AutoPlay(choiceContext, card, null);
			await CardCmd.AutoPlay(choiceContext, dupe, null);
			if (card.Type != CardType.Power)
			{
				await CardCmdCompatibility.Exhaust(choiceContext, card);
			}
		}
	}
}
