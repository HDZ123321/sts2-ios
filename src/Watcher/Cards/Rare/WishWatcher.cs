using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Watcher.Code.Abstract;
using Watcher.Code.Cards.Token;
using Watcher.Code.Character;
using Watcher.Code.Powers;

namespace Watcher.Code.Cards.Rare;

[Pool(typeof(WatcherCardPool))]
public sealed class WishWatcher : WatcherCardModel
{
	public override bool CanBeGeneratedInCombat => false;

	public WishWatcher()
		: base(3, CardType.Skill, CardRarity.Rare, TargetType.None)
	{
		((ConstructedCardModel)this).WithVars(new DynamicVar[1] { DynamicVarExtensions.WithUpgrade<GoldVar>(new GoldVar(25), 5m) });
		((ConstructedCardModel)this).WithPower<StrengthPower>(3, 1);
		((ConstructedCardModel)this).WithPower<PlatedArmorPower>(6, 2);
		((ConstructedCardModel)this).WithKeywords(new CardKeyword[1] { CardKeyword.Exhaust });
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		List<CardModel> list = new CardModel[3]
		{
			(CardModel)(object)ModelDb.Card<BecomeAlmighty>(),
			(CardModel)(object)ModelDb.Card<FameAndFortune>(),
			(CardModel)(object)ModelDb.Card<LiveForever>()
		}.Select((CardModel e) => (CardModel)e.MutableClone()).ToList();
		foreach (CardModel item in list)
		{
			item.Owner = ((CardModel)(object)this).Owner;
			if (((CardModel)(object)this).IsUpgraded)
			{
				CardCmd.Upgrade(item);
			}
		}
		if (await CardSelectCmd.FromChooseACardScreen(choiceContext, list, ((CardModel)(object)this).Owner) is WishableWatcherCard wishableWatcherCard)
		{
			await wishableWatcherCard.OnWish(choiceContext, cardPlay);
		}
	}
}
