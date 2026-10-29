using System;
using System.IO;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace MegaCrit.Sts2.Core.Modding;

/// <summary>
/// Replacement card portraits for the Regent (储君) set, baked into the build from the
/// RegentCardsAnimeRework mod. The upstream mod is a DLL that repaints portraits through
/// Harmony patches, and neither Harmony nor Reflection.Emit survives the NativeAOT build the
/// iOS client uses, so the art travels as its own PCK and is looked up from managed code
/// instead: <see cref="RegentCardArtTable"/> maps a card's CLR type name to the texture.
/// <para>
/// The pack carries only the imported textures and their <c>.import</c> remaps. The two
/// <c>res://.godot/</c> caches the upstream pack also contained are dropped on purpose:
/// mounting a pack that holds <c>uid_cache.bin</c> or <c>global_script_class_cache.cfg</c>
/// makes the engine reload those from the pack, shadowing the game's own copies.
/// </para>
/// </summary>
public static class RegentCardArt
{
	public const string PackFileName = "RegentArt.pck";

	private static bool _mountAttempted;

	private static bool _mounted;

	public static bool IsMounted => _mounted;

	public static string PackPath
	{
		get
		{
			string executablePath = OS.GetExecutablePath();
			return Path.Combine(Path.GetDirectoryName(executablePath) ?? ".", PackFileName);
		}
	}

	/// <summary>
	/// Mounts the pack. Safe to call more than once; the result is cached. Must run before
	/// <c>ModelDb.Preload</c> and before any card node asks for a portrait.
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
			Log.Warn("[RegentCardArt] " + PackFileName + " is not beside the executable (" + path + "), the card art rework will be missing");
			return false;
		}
		if (!ProjectSettings.LoadResourcePack(path))
		{
			Log.Error("[RegentCardArt] Godot refused to mount " + path);
			return false;
		}
		_mounted = true;
		Log.Info("[RegentCardArt] mounted " + path + " with " + RegentCardArtTable.Count + " replacement portrait(s)");
		return true;
	}

	/// <summary>True when <paramref name="cardType"/> ships its own portrait in the pack.</summary>
	public static bool HasReplacement(Type cardType)
	{
		return TryGetPath(cardType, out string _);
	}

	/// <summary>
	/// Replacement portrait for <paramref name="cardType"/>, if the pack is mounted and the
	/// texture is actually there. Callers keep their own default when this returns false.
	/// </summary>
	public static bool TryGetPath(Type cardType, out string path)
	{
		path = string.Empty;
		if (!_mounted || cardType == null)
		{
			return false;
		}
		string fullName = cardType.FullName;
		if (fullName == null || !RegentCardArtTable.TryGet(fullName, out string candidate))
		{
			return false;
		}
		if (!ResourceLoader.Exists(candidate))
		{
			return false;
		}
		path = candidate;
		return true;
	}
}
