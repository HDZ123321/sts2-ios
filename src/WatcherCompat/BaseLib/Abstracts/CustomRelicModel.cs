using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

/// <summary>
/// Relic base.  The <c>alwaysShowCounter</c> flag mirrors BaseLib: relics that
/// opt in show a counter even when their amount is zero.
/// </summary>
public abstract class CustomRelicModel : RelicModel
{
	private readonly bool _alwaysShowCounter;

	protected CustomRelicModel(bool alwaysShowCounter)
	{
		_alwaysShowCounter = alwaysShowCounter;
	}

	public override bool ShowCounter => _alwaysShowCounter || base.ShowCounter;

	/// <summary>
	/// The relic this one becomes once upgraded, or <c>null</c> when it does not upgrade
	/// into another relic.  Nothing in the base game calls this; the mod uses it to pick
	/// the replacement shown by the upgrade flow.
	/// </summary>
	public virtual RelicModel? GetUpgradeReplacement() => null;
}
