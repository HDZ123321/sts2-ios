using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Watcher.Code.Abstract;
using Watcher.Code.Commands;

namespace Watcher.Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public sealed class Beta : WatcherCardModel
{
	public Beta()
		: base(2, CardType.Skill, CardRarity.Token, TargetType.Self)
	{
		((ConstructedCardModel)this).WithKeywords(new CardKeyword[1] { CardKeyword.Exhaust });
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(typeof(Omega)));
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await WatcherCmd.GiveCard<Omega>(((CardModel)(object)this).Owner, PileType.Draw, CardPilePosition.Random, 0.6f, CardPreviewStyle.HorizontalLayout, false, false);
	}
}
