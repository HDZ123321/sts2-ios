using MegaCrit.Sts2.Core.Localization;

namespace BaseLib.Patches.Localization;

/// <summary>
/// Lets a power inject extra localization variables into its description without
/// going through the smart-description path.
/// </summary>
public interface IAddDumbVariablesToPowerDescription
{
	void AddDumbVariablesToPowerDescription(LocString description);
}
