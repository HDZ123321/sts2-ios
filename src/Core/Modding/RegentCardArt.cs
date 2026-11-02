using System;
using System.IO;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace MegaCrit.Sts2.Core.Modding;

/// <summary>
/// Replacement card portraits for the Regent (储君) set, baked into the build from two upstream
/// mods and selected at runtime:
/// <list type="bullet">
/// <item><c>RegentArt.pck</c> — the anime rework (RegentCardsAnimeRework 1.0), 94 cards.</item>
/// <item><c>RegentFemArt.pck</c> — the 娘化 set (RegentFemPortraits 1.0, "Regent 760"), 60 cards.</item>
/// </list>
/// Both upstream mods are DLLs that repaint portraits through Harmony, and neither Harmony nor
/// Reflection.Emit survives the NativeAOT build the iOS client uses, so the art travels as its
/// own PCK and is looked up from managed code instead: <see cref="RegentCardArtTable"/> and
/// <see cref="RegentFemCardArtTable"/> map a card's CLR type name to the texture.
/// <para>
/// The packs carry only the imported textures and their <c>.import</c> remaps. The two
/// <c>res://.godot/</c> caches the upstream packs also contained are dropped on purpose:
/// mounting a pack that holds <c>uid_cache.bin</c> or <c>global_script_class_cache.cfg</c>
/// makes the engine reload those from the pack, shadowing the game's own copies.
/// </para>
/// <para>
/// Which set is live is read once, before anything asks for a portrait, from
/// <c>user://regent_art.txt</c> — the first line is <c>fem</c> (default) or <c>anime</c>. On iOS
/// that file sits in the app's Documents folder, so it can be edited through the Files app. A set
/// that is not baked into the bundle is skipped; with both missing every card keeps its vanilla
/// atlas sprite.
/// </para>
/// </summary>
public static class RegentCardArt
{
	public const string AnimePackFileName = "RegentArt.pck";

	public const string FemPackFileName = "RegentFemArt.pck";

	public const string SelectionFileName = "regent_art.txt";

	private const string AnimeSetId = "anime";

	private const string FemSetId = "fem";

	private static bool _mountAttempted;

	private static bool _animeMounted;

	private static bool _femMounted;

	/// <summary>False only when the selection file explicitly asks for the anime set.</summary>
	private static bool _femPreferred = true;

	public static bool IsMounted => _animeMounted || _femMounted;

	/// <summary>True when the 娘化 set is the one being handed out.</summary>
	public static bool FemActive => _femMounted && (_femPreferred || !_animeMounted);

	/// <summary>Name of the live set, for logs and the release report.</summary>
	public static string ActiveSetName => FemActive ? FemSetId : AnimeSetId;

	/// <summary>How many cards the live set can repaint.</summary>
	public static int ActiveCount => FemActive ? RegentFemCardArtTable.Count : RegentCardArtTable.Count;

	/// <summary>
	/// Mounts the baked packs. Safe to call more than once; the result is cached. Must run before
	/// <c>ModelDb.Preload</c> and before any card node asks for a portrait.
	/// </summary>
	public static bool Mount()
	{
		if (_mountAttempted)
		{
			return IsMounted;
		}
		_mountAttempted = true;
		_femPreferred = ReadSelection();
		_animeMounted = TryMount(AnimePackFileName, RegentCardArtTable.Count);
		_femMounted = TryMount(FemPackFileName, RegentFemCardArtTable.Count);
		if (_femPreferred && !_femMounted && _animeMounted)
		{
			Log.Warn("[RegentCardArt] " + FemPackFileName + " is missing, falling back to the anime set");
		}
		else if (!_femPreferred && !_animeMounted && _femMounted)
		{
			Log.Warn("[RegentCardArt] " + AnimePackFileName + " is missing, falling back to the fem set");
		}
		if (!IsMounted)
		{
			Log.Warn("[RegentCardArt] neither card art pack is beside the executable, the reworks will be missing");
			return false;
		}
		Log.Info("[RegentCardArt] active set '" + ActiveSetName + "' with " + ActiveCount + " replacement portrait(s)");
		return true;
	}

	/// <summary>True when <paramref name="cardType"/> gets a replacement portrait from the live set.</summary>
	public static bool HasReplacement(Type cardType)
	{
		return TryGetPath(cardType, out string _);
	}

	/// <summary>
	/// Replacement portrait for <paramref name="cardType"/>, if the live set is mounted, covers
	/// this card and the texture is actually there. Callers keep their own default (the vanilla
	/// atlas sprite) when this returns false.
	/// </summary>
	public static bool TryGetPath(Type cardType, out string path)
	{
		path = string.Empty;
		if (!IsMounted || cardType == null)
		{
			return false;
		}
		string fullName = cardType.FullName;
		if (fullName == null)
		{
			return false;
		}
		bool found = FemActive
			? RegentFemCardArtTable.TryGet(fullName, out string candidate)
			: RegentCardArtTable.TryGet(fullName, out candidate);
		if (!found || !ResourceLoader.Exists(candidate))
		{
			return false;
		}
		path = candidate;
		return true;
	}

	private static bool TryMount(string fileName, int portraitCount)
	{
		string path = Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath()) ?? ".", fileName);
		if (!Godot.FileAccess.FileExists(path))
		{
			Log.Warn("[RegentCardArt] " + fileName + " is not beside the executable (" + path + ")");
			return false;
		}
		if (!ProjectSettings.LoadResourcePack(path))
		{
			Log.Error("[RegentCardArt] Godot refused to mount " + path);
			return false;
		}
		Log.Info("[RegentCardArt] mounted " + path + " with " + portraitCount + " replacement portrait(s)");
		return true;
	}

	/// <summary>Reads <c>user://regent_art.txt</c>; anything unreadable means the default set.</summary>
	private static bool ReadSelection()
	{
		string userPath = "user://" + SelectionFileName;
		try
		{
			if (!Godot.FileAccess.FileExists(userPath))
			{
				return true;
			}
			string text = Godot.FileAccess.GetFileAsString(userPath).Trim().ToLowerInvariant();
			if (text.StartsWith(AnimeSetId, StringComparison.Ordinal))
			{
				return false;
			}
			if (text.StartsWith(FemSetId, StringComparison.Ordinal))
			{
				return true;
			}
			Log.Warn("[RegentCardArt] " + SelectionFileName + " says '" + text + "', expected '" + FemSetId + "' or '" + AnimeSetId + "'; using the default set");
		}
		catch (Exception exception)
		{
			Log.Warn("[RegentCardArt] could not read " + userPath + ": " + exception.Message + "; using the default set");
		}
		return true;
	}
}
