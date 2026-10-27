using System.Collections.Generic;

namespace BaseLib.Utils;

/// <summary>How a credits section lays its entries out.</summary>
public enum Layout
{
	Vertical = 1,
	Horizontal = 2
}

/// <summary>One named block of the credits screen.</summary>
public sealed class Section
{
	public Section(string title, Layout layout, Section[]? children = null)
	{
		Title = title;
		Layout = layout;
		Children = children ?? System.Array.Empty<Section>();
	}

	public string Title { get; }

	public Layout Layout { get; }

	public IReadOnlyList<Section> Children { get; }
}

/// <summary>
/// Credits metadata.  The baked-in build has a single fixed credits screen, so
/// registering only records the names for the in-game mod list.
/// </summary>
public static class ModCredits
{
	private static readonly Dictionary<string, IReadOnlyList<Section>> _registered = new Dictionary<string, IReadOnlyList<Section>>();

	public static IReadOnlyDictionary<string, IReadOnlyList<Section>> Registered => _registered;

	public static void Register(string modId, params Section[] sections)
	{
		if (string.IsNullOrEmpty(modId))
		{
			return;
		}
		_registered[modId] = sections ?? System.Array.Empty<Section>();
	}
}
