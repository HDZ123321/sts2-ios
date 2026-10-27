using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;

namespace Watcher.Code.Powers;

public sealed class DevaPower : WatcherPowerModel
{
	public override PowerType Type => PowerType.Buff;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new EnergyVar(1));

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task AfterEnergyReset(Player player)
	{
		if (player.Creature == ((PowerModel)(object)this).Owner)
		{
			await PlayerCmd.GainEnergy(((PowerModel)(object)this).DynamicVars.Energy.IntValue, player);
			((PowerModel)(object)this).DynamicVars.Energy.UpgradeValueBy(((PowerModel)(object)this).Amount);
			((PowerModel)(object)this).Flash();
		}
	}

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		return Task.CompletedTask;
	}
}
