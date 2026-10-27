using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Powers;

namespace Watcher.Code.Cards.Uncommon;

[Pool(typeof(WatcherCardPool))]
public sealed class WaveOfTheHand : WatcherCardModel
{
	public WaveOfTheHand()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
	{
		WithPower<WaveOfTheHandPower>(1, 1, false);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(typeof(WeakPower)));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(StaticHoverTip.Block));
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<WaveOfTheHandPower>(ctx, (CardModel)(object)this, false);
	}
}
