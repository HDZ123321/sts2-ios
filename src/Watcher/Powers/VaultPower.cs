using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;

namespace Watcher.Code.Powers;

public class VaultPower : WatcherPowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override bool IsVisibleInternal => false;

	public override bool ShouldTakeExtraTurn(Player player)
	{
		return player.Creature == ((PowerModel)(object)this).Owner;
	}

	public override Task AfterTakingExtraTurn(Player player)
	{
		if (player.Creature != ((PowerModel)(object)this).Owner)
		{
			return Task.CompletedTask;
		}
		return PowerCmd.Decrement((PowerModel)(object)this);
	}
}
