using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

/// <summary>
/// Points at the dynamic var set that supplies the amount for a builder call.
/// Cards are the only source today, but powers and relics read the same way.
/// </summary>
public readonly struct DynamicVarSource
{
	private readonly DynamicVarSet? _vars;

	private DynamicVarSource(DynamicVarSet vars)
	{
		_vars = vars;
	}

	public DynamicVarSet? Vars => _vars;

	public static DynamicVarSource op_Implicit(CardModel card)
	{
		return new DynamicVarSource(card.DynamicVars);
	}

	public static DynamicVarSource op_Implicit(PowerModel power)
	{
		return new DynamicVarSource(power.DynamicVars);
	}

	public static DynamicVarSource op_Implicit(RelicModel relic)
	{
		return new DynamicVarSource(relic.DynamicVars);
	}
}
