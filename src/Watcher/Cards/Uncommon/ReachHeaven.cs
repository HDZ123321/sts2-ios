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
public sealed class ReachHeaven : WatcherCardModel
{
	public ReachHeaven()
		: base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
		((ConstructedCardModel)this).WithDamage(10, 5);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(typeof(ThroughViolence)));
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(choiceContext);
		await WatcherCmd.GiveCard<ThroughViolence>(((CardModel)(object)this).Owner, PileType.Draw, CardPilePosition.Random, 0.6f, CardPreviewStyle.HorizontalLayout, false, false);
	}
}
