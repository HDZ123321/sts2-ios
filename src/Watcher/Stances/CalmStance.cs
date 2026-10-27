using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using Watcher.Code.Events;
using Watcher.Code.Vfx;

namespace Watcher.Code.Stances;

public class CalmStance : WatcherStanceModel
{
	public override bool ShouldReceiveCombatHooks => true;

	protected override StanceVfxConfig VfxConfig => new StanceVfxConfig("res://Watcher/scenes/watcher_mod/vfx/calm_aura.tscn", new Color(0.7f, 0.85f, 1.3f), "res://Watcher/audio/calm_enter.ogg", new Color(0.4f, 0.7f, 1f), ShakeStrength.None, "res://Watcher/audio/calm_loop.ogg");

	public override async Task OnExitStance(PlayerChoiceContext ctx, Player player, CardModel? source)
	{
		await PlayerCmd.GainEnergy(WatcherHook.ModifyCalmEnergyGain(player.Creature.CombatState, player, 2), player);
		await base.OnExitStance(ctx, player, source);
	}
}
