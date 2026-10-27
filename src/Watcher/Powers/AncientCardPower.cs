using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Events;

namespace Watcher.Code.Powers;

public class AncientCardPower : WatcherPowerModel, IModifyWrathDamage
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public decimal ModifyWrathDamage(Player player, decimal amount)
	{
		if (player.Creature != ((PowerModel)(object)this).Owner)
		{
			return amount;
		}
		return amount + (decimal)((PowerModel)(object)this).Amount / 100m;
	}
}
