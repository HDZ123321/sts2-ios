using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Watcher.Code.Abstract;

namespace Watcher.Code.Powers;

public sealed class BlockReturnPower : WatcherPowerModel, IAddDumbVariablesToPowerDescription
{
	private bool _applierIsAttacking;

	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;

	public override Task BeforeAttack(AttackCommand command)
	{
		if (command.Attacker == ((PowerModel)(object)this).Applier)
		{
			_applierIsAttacking = true;
		}
		return Task.CompletedTask;
	}

	public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (!(((PowerModel)(object)this).Applier == null || wasRemovalPrevented) && _applierIsAttacking && creature == ((PowerModel)(object)this).Owner)
		{
			await CreatureCmd.GainBlock(((PowerModel)(object)this).Applier, ((PowerModel)(object)this).Amount, ValueProp.Unpowered, null);
		}
	}

	public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
	{
		if (((PowerModel)(object)this).Applier == null || command.Attacker != ((PowerModel)(object)this).Applier)
		{
			return;
		}
		_applierIsAttacking = false;
		int count = command.Results.SelectMany((List<DamageResult> s) => s).Count((DamageResult e) => e.Receiver == ((PowerModel)(object)this).Owner);
		if (count != 0)
		{
			for (int i = 0; i < count; i++)
			{
				await CreatureCmd.GainBlock(((PowerModel)(object)this).Applier, ((PowerModel)(object)this).Amount, ValueProp.Unpowered, null);
			}
		}
	}

	public void AddDumbVariablesToPowerDescription(LocString description)
	{
		description.Add("IsApplierYou", LocalContext.IsMe(((PowerModel)(object)this).Applier));
	}
}
