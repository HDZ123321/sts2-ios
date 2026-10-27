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

namespace Watcher.Code.Cards.Common;

[Pool(typeof(WatcherCardPool))]
public sealed class Evaluate : WatcherCardModel
{
	public Evaluate()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
	{
		((ConstructedCardModel)this).WithBlock(6, 4);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(typeof(Insight)));
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await WatcherCmd.GiveCard<Insight>(((CardModel)(object)this).Owner, PileType.Draw, CardPilePosition.Random, 0.6f, CardPreviewStyle.HorizontalLayout, false, false);
	}
}
