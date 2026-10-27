using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Watcher.Code.Abstract;
using Watcher.Code.Cards.Token;
using Watcher.Code.Character;
using Watcher.Code.Commands;

namespace Watcher.Code.Cards.Rare;

[Pool(typeof(WatcherCardPool))]
public sealed class Alpha : WatcherCardModel
{
	public Alpha()
		: base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
		((ConstructedCardModel)this).WithKeywords(new CardKeyword[1] { CardKeyword.Exhaust });
		HoverTipFactory.FromCardWithCardHoverTips<Beta>(false).Select((Func<IHoverTip, TooltipSource>)((IHoverTip m) => new TooltipSource((Func<CardModel, IHoverTip>)((CardModel _) => m)))).ToList()
			.ForEach(delegate(TooltipSource t)
			{
				((ConstructedCardModel)this).WithTip(t);
			});
		((ConstructedCardModel)this).WithKeyword(CardKeyword.Innate, (UpgradeType)1);
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await WatcherCmd.GiveCard<Beta>(((CardModel)(object)this).Owner, PileType.Draw, CardPilePosition.Random, 0.6f, CardPreviewStyle.HorizontalLayout, false, false);
	}
}
