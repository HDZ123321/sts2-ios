using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace MegaCrit.Sts2.Core.Modding;

/// <summary>
/// Content that ships inside the build itself instead of arriving as a mod: the
/// ported Watcher character. It travels as its own PCK next to the executable,
/// because the base game PCK is a prebuilt release asset that CI reuses verbatim
/// rather than rebuilding (it is close to 2 GB).
/// <para>
/// The PCK was authored against a mod loader that prefixes every model key with
/// <see cref="LocKeyPrefix"/> ("WATCHER-CALM_STANCE.title"). Baked in, the vanilla
/// localization builders ask for the bare <c>ModelId</c> entry instead
/// ("CALM_STANCE.title"), so each prefixed key is also exposed unprefixed. The
/// stance models keep querying the prefixed form, and both resolve.
/// </para>
/// </summary>
public static class BakedContent
{
	public const string PackFileName = "Watcher.pck";

	public const string ContentRoot = "res://Watcher/";

	public const string LocKeyPrefix = "WATCHER-";

	private static bool _mountAttempted;

	private static bool _mounted;

	public static bool IsMounted => _mounted;

	/// <summary>Absolute path of the pack that lives beside the executable.</summary>
	public static string PackPath
	{
		get
		{
			string executablePath = OS.GetExecutablePath();
			return Path.Combine(Path.GetDirectoryName(executablePath) ?? ".", PackFileName);
		}
	}

	/// <summary>
	/// Mounts the pack. Safe to call more than once; the result is cached.
	/// Must run before atlases and localization are loaded.
	/// </summary>
	public static bool Mount()
	{
		if (_mountAttempted)
		{
			return _mounted;
		}
		_mountAttempted = true;
		string path = PackPath;
		if (!Godot.FileAccess.FileExists(path))
		{
			Log.Warn("[BakedContent] " + PackFileName + " is not beside the executable (" + path + "), the ported content will be missing");
			return false;
		}
		if (!ProjectSettings.LoadResourcePack(path))
		{
			Log.Error("[BakedContent] Godot refused to mount " + path);
			return false;
		}
		_mounted = true;
		Log.Info("[BakedContent] mounted " + path);
		return true;
	}

	/// <summary>Localization tables the baked pack provides for one language/file.</summary>
	public static IEnumerable<string> GetLocTables(string language, string file)
	{
		if (!_mounted)
		{
			yield break;
		}
		string path = ContentRoot + "localization/" + language + "/" + file;
		if (ResourceLoader.Exists(path))
		{
			yield return path;
		}
	}

	/// <summary>
	/// Strings the ported content needs but no pack provides: the mod loader supplied them from
	/// its own bundle, and the shim only has the key names to work with.
	/// </summary>
	private static readonly Dictionary<string, string> PowersExtra = new Dictionary<string, string>
	{
		{ "BASE_TIP_SCRY.title", "Scry" },
		{ "BASE_TIP_SCRY.description", "Look at the top cards of your draw pile, then discard any number of them." }
	};

	/// <summary>Merges <see cref="PowersExtra"/> into the powers table. Call after LocManager.Initialize.</summary>
	public static void InjectLocEntries()
	{
		if (!_mounted)
		{
			return;
		}
		Localization.LocManager instance = Localization.LocManager.Instance;
		if (instance == null)
		{
			return;
		}
		instance.AddEntries("powers", PowersExtra);
	}

	public static bool IsBakedLocTable(string path)
	{
		return path != null && path.StartsWith(ContentRoot, StringComparison.Ordinal);
	}

	/// <summary>
	/// Copy of <paramref name="table"/> with every "WATCHER-"-prefixed key also
	/// present under its bare name, so vanilla key builders find it too.
	/// </summary>
	public static Dictionary<string, string> WithUnprefixedAliases(Dictionary<string, string> table)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (KeyValuePair<string, string> item in table)
		{
			if (!item.Key.StartsWith(LocKeyPrefix, StringComparison.Ordinal))
			{
				continue;
			}
			string text = item.Key.Substring(LocKeyPrefix.Length);
			if (!table.ContainsKey(text))
			{
				dictionary[text] = item.Value;
			}
		}
		return dictionary;
	}
}
