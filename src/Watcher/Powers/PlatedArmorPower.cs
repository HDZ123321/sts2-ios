using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Watcher.Code.Abstract;

namespace Watcher.Code.Powers;

public class PlatedArmorPower : WatcherPowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new global::_003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.Static(StaticHoverTip.Block));

	public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (target == ((PowerModel)(object)this).Owner && result.UnblockedDamage != 0)
		{
			await PowerCmd.Decrement((PowerModel)(object)this);
		}
	}

	public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		Player player = ((PowerModel)(object)this).Owner.Player;
		if (player == null || player.PlayerCombatState == null || side == ((PowerModel)(object)this).Owner.Side)
		{
			((PowerModel)(object)this).Flash();
			await CreatureCmd.GainBlock(((PowerModel)(object)this).Owner, ((PowerModel)(object)this).Amount, ValueProp.Unpowered, null);
		}
	}
}
