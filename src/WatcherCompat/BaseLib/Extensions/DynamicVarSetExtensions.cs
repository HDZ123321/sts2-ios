using System.Collections.Generic;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Extensions;

public static class DynamicVarSetExtensions
{
	/// <summary>
	/// Finds the var that feeds a power's tooltip.  Cards built through
	/// <c>WithPower&lt;T&gt;</c> store a plain var named after the power's type,
	/// while hand-written cards use <see cref="T:MegaCrit.Sts2.Core.Localization.DynamicVars.PowerVar`1"/>.
	/// </summary>
	public static DynamicVar Power<T>(this DynamicVarSet set) where T : PowerModel
	{
		string name = typeof(T).Name;
		foreach (KeyValuePair<string, DynamicVar> pair in set)
		{
			if (pair.Value is PowerVar<T>)
			{
				return pair.Value;
			}
		}
		foreach (KeyValuePair<string, DynamicVar> pair2 in set)
		{
			if (pair2.Value.Name == name)
			{
				return pair2.Value;
			}
		}
		throw new KeyNotFoundException($"No dynamic var for power {name} on this model.");
	}

	/// <summary>The scry amount a model carries, or a zero var when it does not scry.</summary>
	public static DynamicVar Scry(this DynamicVarSet set)
	{
		foreach (KeyValuePair<string, DynamicVar> pair in set)
		{
			if (pair.Value is BaseLib.Cards.Variables.ScryVar)
			{
				return pair.Value;
			}
		}
		if (set.TryGetValue(BaseLib.Cards.Variables.ScryVar.defaultName, out DynamicVar? named))
		{
			return named;
		}
		return new BaseLib.Cards.Variables.ScryVar(0m);
	}

	public static bool TryGetPower<T>(this DynamicVarSet set, out DynamicVar? variable) where T : PowerModel
	{
		try
		{
			variable = set.Power<T>();
			return true;
		}
		catch (KeyNotFoundException)
		{
			variable = null;
			return false;
		}
	}
}
