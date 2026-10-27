using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Cards.Token;
using Watcher.Code.Character;
using Watcher.Code.Powers;

namespace Watcher.Code.Cards.Uncommon;

[Pool(typeof(WatcherCardPool))]
public sealed class Study : WatcherCardModel
{
	public Study()
		: base(2, CardType.Power, CardRarity.Uncommon, TargetType.None)
	{
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(typeof(Insight)));
		WithPower<StudyPower>(1, false);
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<StudyPower>(ctx, (CardModel)(object)this, false);
	}
}
