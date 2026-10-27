using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Powers;

namespace Watcher.Code.Cards.Uncommon;

[Pool(typeof(WatcherCardPool))]
public sealed class Nirvana : WatcherCardModel
{
	public Nirvana()
		: base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
	{
		WithPower<NirvanaPower>(3, 1, false);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(StaticHoverTip.Block));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(BaseLibTip.Scry));
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(((CardModel)(object)this).Owner.Creature, "Cast", ((CardModel)(object)this).Owner.Character.CastAnimDelay);
		await CommonActions.ApplySelf<NirvanaPower>(ctx, (CardModel)(object)this, false);
	}
}
