using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Commands;
using Watcher.Code.Stances;

namespace Watcher.Code.Cards.Multiplayer;

[Pool(typeof(WatcherCardPool))]
public class SharedWisdom : WatcherCardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

	public SharedWisdom()
		: base(3, CardType.Skill, CardRarity.Rare, TargetType.AnyAlly)
	{
		WithStanceTip<DivinityStance>();
		((ConstructedCardModel)this).WithKeyword(CardKeyword.Ethereal, (UpgradeType)2);
		((ConstructedCardModel)this).WithKeyword(CardKeyword.Exhaust, (UpgradeType)0);
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (cardPlay.Target?.Player != null)
		{
			await StanceCmd.EnterDivinity(ctx, cardPlay.Target.Player, (CardModel?)(object)this);
		}
	}
}
