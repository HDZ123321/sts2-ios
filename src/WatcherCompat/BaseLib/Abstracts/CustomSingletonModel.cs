using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

/// <summary>
/// A model that exists once per save rather than per combat: it is registered
/// with the run so its hooks fire even outside combat.
/// </summary>
public abstract class CustomSingletonModel : AbstractModel
{
	protected CustomSingletonModel(HookType hookType)
	{
		HookType = hookType;
	}

	public HookType HookType { get; }

	public override bool ShouldReceiveCombatHooks => HookType != HookType.Run;
}
