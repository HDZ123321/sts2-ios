using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace BaseLib.Extensions;

/// <summary>
/// Remembers the upgrade delta for a dynamic var.  The base game only offers
/// <see cref="M:MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar.UpgradeValueBy(System.Decimal)"/>,
/// which has to be called from the card's own upgrade hook; the mod instead
/// declares the delta up front, so it is parked here until then.
/// </summary>
public static class DynamicVarExtensions
{
	private static readonly ConditionalWeakTable<DynamicVar, StrongBox<decimal>> Upgrades = new ConditionalWeakTable<DynamicVar, StrongBox<decimal>>();
	private static readonly ConditionalWeakTable<DynamicVar, StrongBox<int>> CostUpgrades = new ConditionalWeakTable<DynamicVar, StrongBox<int>>();

	public static T WithUpgrade<T>(this T variable, decimal upgradeAmount) where T : DynamicVar
	{
		Upgrades.AddOrUpdate(variable, new StrongBox<decimal>(upgradeAmount));
		return variable;
	}

	public static decimal GetUpgradeAmount(this DynamicVar variable)
	{
		return Upgrades.TryGetValue(variable, out StrongBox<decimal>? box) ? box.Value : 0m;
	}

	public static bool HasUpgradeAmount(this DynamicVar variable)
	{
		return Upgrades.TryGetValue(variable, out _);
	}

	public static void RegisterCostUpgrade(DynamicVar variable, int delta)
	{
		CostUpgrades.AddOrUpdate(variable, new StrongBox<int>(delta));
	}

	public static int GetCostUpgrade(DynamicVar variable)
	{
		return CostUpgrades.TryGetValue(variable, out StrongBox<int>? box) ? box.Value : 0;
	}
}
