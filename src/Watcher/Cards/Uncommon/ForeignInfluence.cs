using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Extensions;

namespace Watcher.Code.Cards.Uncommon;

[Pool(typeof(WatcherCardPool))]
public sealed class ForeignInfluence : WatcherCardModel
{
	public ForeignInfluence()
		: base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
		((ConstructedCardModel)this).WithKeywords(new CardKeyword[1] { CardKeyword.Exhaust });
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (((CardModel)(object)this).Owner.Creature.CombatState == null)
		{
			return;
		}
		Rng combatCardGeneration = ((CardModel)(object)this).Owner.RunState.Rng.CombatCardGeneration;
		Dictionary<CardRarity, List<CardModel>> dictionary = (from c in CardFactory.FilterForCombat(ModelDb.AllCards).FilterForPlayerCount(((CardModel)(object)this).Owner.RunState)
			where c.Type == CardType.Attack
			group c by c.Rarity).ToDictionary((IGrouping<CardRarity, CardModel> g) => g.Key, (IGrouping<CardRarity, CardModel> g) => g.ToList());
		List<CardModel> list = new List<CardModel>();
		HashSet<string> hashSet = new HashSet<string>();
		while (list.Count < 3)
		{
			int num = combatCardGeneration.NextInt(100);
			CardRarity key = ((num < 55) ? CardRarity.Common : ((num < 85) ? CardRarity.Uncommon : CardRarity.Rare));
			if (dictionary.TryGetValue(key, out var value))
			{
				CardModel cardModel = combatCardGeneration.NextItem(value);
				if (cardModel != null && hashSet.Add(cardModel.Id.Entry))
				{
					list.Add(((CardModel)(object)this).Owner.Creature.CombatState.CreateCard(cardModel, ((CardModel)(object)this).Owner));
				}
			}
		}
		CardModel cardModel2 = await CardSelectCmd.FromChooseACardScreen(choiceContext, list, ((CardModel)(object)this).Owner, canSkip: true);
		if (cardModel2 != null)
		{
			if (((CardModel)(object)this).IsUpgraded)
			{
				cardModel2.SetToFreeThisTurn();
			}
			await CardPileCmd.AddGeneratedCardToCombat(cardModel2, PileType.Hand, ((CardModel)(object)this).Owner);
		}
	}
}
