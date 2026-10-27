using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;

namespace Watcher.Code.Powers;

public sealed class MasterRealityPower : WatcherPowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Single;

	public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
	{
		if (creator?.Creature != ((PowerModel)(object)this).Owner)
		{
			return Task.CompletedTask;
		}
		if (card != null && card.IsUpgradable && !card.IsUpgraded)
		{
			CardCmd.Upgrade(card);
		}
		return Task.CompletedTask;
	}
}
