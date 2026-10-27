using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Watcher.Code.Abstract;

namespace Watcher.Code.Powers;

public class OmegaPower : WatcherPowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (((PowerModel)(object)this).Owner.Side != side)
		{
			return;
		}
		decimal num = ((PowerModel)(object)this).Amount;
		if (!(num <= 0m))
		{
			IEnumerable<Creature> targets = from c in ((PowerModel)(object)this).CombatState.GetOpponentsOf(((PowerModel)(object)this).Owner)
				where c.IsAlive
				select c;
			await CreatureCmd.Damage(choiceContext, targets, num, ValueProp.Unpowered, ((PowerModel)(object)this).Owner);
			((PowerModel)(object)this).Flash();
		}
	}
}
