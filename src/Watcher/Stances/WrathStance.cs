using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.ValueProps;
using Watcher.Code.Events;
using Watcher.Code.Vfx;

namespace Watcher.Code.Stances;

public sealed class WrathStance : WatcherStanceModel
{
	public override bool ShouldReceiveCombatHooks => true;

	protected override StanceVfxConfig VfxConfig
	{
		get
		{
			Color? screenFlashColor = new Color(1f, 0.15f, 0.1f);
			return new StanceVfxConfig("res://Watcher/scenes/watcher_mod/vfx/wrath_aura.tscn", null, "res://Watcher/audio/wrath_enter.ogg", screenFlashColor, ShakeStrength.Medium, "res://Watcher/audio/wrath_loop.ogg");
		}
	}

	public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		if (props.HasFlag(ValueProp.Unpowered) || base.Owner.Creature.CombatState == null)
		{
			return 1m;
		}
		decimal num = WatcherHook.ModifyWrathDamage(base.Owner.Creature.CombatState, base.Owner, 0m);
		if (dealer == base.Owner.Creature)
		{
			return 2m + num;
		}
		if (target != base.Owner.Creature)
		{
			return 1m;
		}
		return 2m;
	}
}
