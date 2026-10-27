namespace BaseLib.Patches.UI;

/// <summary>
/// The three sprite paths a custom relic touches: the big shop/altar icon and
/// the two atlases that the relic bar draws from.
/// </summary>
public sealed class RelicIconData
{
	public string BigIconPath { get; }

	public string PackedIconPath { get; }

	public string PackedIconOutlinePath { get; }

	public RelicIconData(string bigIconPath, string packedIconPath, string packedIconOutlinePath)
	{
		BigIconPath = bigIconPath;
		PackedIconPath = packedIconPath;
		PackedIconOutlinePath = packedIconOutlinePath;
	}
}
