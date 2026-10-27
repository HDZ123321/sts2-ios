namespace BaseLib.Utils;

/// <summary>
/// Tells a builder whether the thing it is adding applies always or only once
/// the card is upgraded.
/// </summary>
public enum UpgradeType
{
	Always = 1,
	Upgrade = 2,
	Never = 0
}
