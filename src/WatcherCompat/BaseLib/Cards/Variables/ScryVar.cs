using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace BaseLib.Cards.Variables;

/// <summary>
/// The "look at the top of the draw pile" amount.  The base game has no scry
/// system, so this var plus <see cref="T:BaseLib.Commands.ScryCmd"/> implement it.
/// </summary>
public sealed class ScryVar : DynamicVar
{
	public const string defaultName = "Scry";

	public ScryVar(decimal scry)
		: base(defaultName, scry)
	{
	}

	public ScryVar(string name, decimal scry)
		: base(name, scry)
	{
	}
}
