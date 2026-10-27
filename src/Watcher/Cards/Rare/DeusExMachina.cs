using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Watcher.Code.Abstract;
using Watcher.Code.Cards.Token;
using Watcher.Code.Character;
using Watcher.Code.Commands;
using Watcher.Code.Compatibility;

namespace Watcher.Code.Cards.Rare;

[Pool(typeof(WatcherCardPool))]
public sealed class DeusExMachina : WatcherCardModel
{
	public DeusExMachina()
		: base(-1, CardType.Skill, CardRarity.Rare, TargetType.Self)
	{
		((ConstructedCardModel)this).WithKeywords(new CardKeyword[1] { CardKeyword.Unplayable });
		((ConstructedCardModel)this).WithCards(2, 1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(typeof(Miracle)));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(CardKeyword.Exhaust));
	}

	public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel drawnCard, bool fromHandDraw)
	{
		if ((object)drawnCard == this)
		{
			await WatcherCmd.GiveCards<Miracle>(((CardModel)(object)this).Owner, ((CardModel)(object)this).DynamicVars.Cards.IntValue, PileType.Hand, CardPilePosition.Bottom, 0.1f, CardPreviewStyle.HorizontalLayout, false, false);
			await CardCmdCompatibility.Exhaust(choiceContext, (CardModel)(object)this);
		}
	}
}
