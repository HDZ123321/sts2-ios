using System.IO;

namespace Watcher.Code.Extensions;

public static class StringExtensions
{
	/// <summary>
	/// Strips the model-id prefix a modded entity carries, so
	/// <c>WATCHER-WATCHER_COOKIE</c> becomes <c>WATCHER_COOKIE</c> before it is turned into an
	/// asset name.
	/// </summary>
	public static string RemovePrefix(this string value, string prefix = "WATCHER-")
	{
		if (value != null && prefix != null && value.StartsWith(prefix, global::System.StringComparison.Ordinal))
		{
			return value.Substring(prefix.Length);
		}
		return value;
	}

	public static string CardImagePath(this string path)
	{
		return Path.Join("Watcher", "images", "card_portraits", path);
	}

	public static string PowerImagePath(this string path)
	{
		return Path.Join("Watcher", "images", "powers", path);
	}

	public static string BigRelicImagePath(this string path)
	{
		return Path.Join("Watcher", "images", "relics", path);
	}

	public static string TresRelicImagePath(this string path)
	{
		global::_003C_003Ey__InlineArray5<string> buffer = default(global::_003C_003Ey__InlineArray5<string>);
		buffer[0] = "Watcher";
		buffer[1] = "images";
		buffer[2] = "atlases";
		buffer[3] = "relic_atlas.sprites";
		buffer[4] = path;
		return Path.Join(buffer);
	}

	public static string PackedPotionImagePath(this string path)
	{
		global::_003C_003Ey__InlineArray5<string> buffer = default(global::_003C_003Ey__InlineArray5<string>);
		buffer[0] = "Watcher";
		buffer[1] = "images";
		buffer[2] = "potions";
		buffer[3] = "tres";
		buffer[4] = path;
		return Path.Join(buffer);
	}
}
