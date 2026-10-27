using System;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using Watcher.Code.Stances;

namespace Watcher.Code.Events;

public class WatcherHook
{
	public static Task OnStanceChange(PlayerChoiceContext ctx, Player player, WatcherStanceModel oldStance, WatcherStanceModel newStance)
	{
		return HookUtils.Dispatch<IOnStanceChange>(player.Creature.CombatState, ctx, (Func<IOnStanceChange, Task>)((IOnStanceChange m) => m.OnStanceChange(ctx, player, oldStance, newStance)));
	}

	public static decimal ModifyCalmEnergyGain(ICombatState cs, Player player, int baseAmount)
	{
		return HookUtils.Aggregate<IModifyCalmEnergyGain, int>(cs, baseAmount, (Func<IModifyCalmEnergyGain, int, int>)((IModifyCalmEnergyGain m, int current) => m.ModifyCalmEnergyGain(player, current)));
	}

	public static decimal ModifyWrathDamage(ICombatState cs, Player player, decimal baseMultiplier)
	{
		return HookUtils.Aggregate<IModifyWrathDamage, decimal>(cs, baseMultiplier, (Func<IModifyWrathDamage, decimal, decimal>)((IModifyWrathDamage m, decimal current) => m.ModifyWrathDamage(player, current)));
	}
}
