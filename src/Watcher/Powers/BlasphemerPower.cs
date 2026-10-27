using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Watcher.Code.Abstract;

namespace Watcher.Code.Powers;

public sealed class BlasphemerPower : WatcherPowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (player == ((PowerModel)(object)this).Owner.Player)
		{
			await CreatureCmd.Damage(choiceContext, ((PowerModel)(object)this).Owner, 99999m, ValueProp.Unpowered, ((PowerModel)(object)this).Owner);
			((PowerModel)(object)this).Flash();
			await PowerCmd.TickDownDuration((PowerModel)(object)this);
		}
	}
}
