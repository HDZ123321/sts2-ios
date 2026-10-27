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

namespace Watcher.Code.Cards.Uncommon;

[Pool(typeof(WatcherCardPool))]
public sealed class DeceiveReality : WatcherCardModel
{
	public DeceiveReality()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
		((ConstructedCardModel)this).WithBlock(4, 3);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(typeof(Safety)));
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await WatcherCmd.GiveCard<Safety>(((CardModel)(object)this).Owner, PileType.Hand, CardPilePosition.Top, 0.6f, CardPreviewStyle.HorizontalLayout, false, true);
	}
}
