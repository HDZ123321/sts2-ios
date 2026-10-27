using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace BaseLib.Utils;

/// <summary>
/// Hover tips BaseLib added on top of the base game's
/// <see cref="T:MegaCrit.Sts2.Core.HoverTips.StaticHoverTip"/> enum.
/// </summary>
public static class BaseLibTip
{
	public static IHoverTip Scry { get; } = Build("scry");

	private static IHoverTip Build(string key)
	{
		LocString title = new LocString("powers", "BASE_TIP_" + key.ToUpperInvariant() + ".title");
		LocString description = new LocString("powers", "BASE_TIP_" + key.ToUpperInvariant() + ".description");
		return new HoverTip(title, description.GetFormattedText(), null);
	}
}
