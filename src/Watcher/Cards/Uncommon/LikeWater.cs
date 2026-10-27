using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Powers;
using Watcher.Code.Stances;

namespace Watcher.Code.Cards.Uncommon;

[Pool(typeof(WatcherCardPool))]
public sealed class LikeWater : WatcherCardModel
{
	public LikeWater()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.None)
	{
		WithPower<LikeWaterPower>(5, 2, false);
		WithStanceTip<CalmStance>();
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(StaticHoverTip.Block));
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<LikeWaterPower>(ctx, (CardModel)(object)this, false);
	}
}
