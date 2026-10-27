"""Batch 5: bake the ported Watcher content into the build.

1. src/Core/Modding/BakedContent.cs  - mounts Watcher.pck next to the executable
   and exposes its localization tables (with unprefixed aliases).
2. OneTimeInitialization.ExecuteVeryEarly() mounts the pack before anything
   reads an atlas or a localization table.
3. ModManager.GetModdedLocTables() also yields the baked tables.
4. LocManager.LoadTablesFromPath() adds the unprefixed aliases when merging them.

Idempotent: every replacement is skipped when the marker text is already there.
"""

import io
import os
import sys

ROOT = r'E:\sts2_port\ci_bundle'
CORE = os.path.join(ROOT, 'src', 'Core')

BAKED_CONTENT = '''using System;
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
'''


def read(path):
    with io.open(path, 'r', encoding='utf-8-sig', newline='') as f:
        return f.read()


def write(path, text):
    with io.open(path, 'w', encoding='utf-8', newline='') as f:
        f.write(text)


def newline_of(text):
    return '\r\n' if '\r\n' in text else '\n'


def patch(path, replacements):
    text = read(path)
    nl = newline_of(text)
    changed = []
    for marker, old, new in replacements:
        if marker in text:
            continue
        old_t = old.replace('\n', nl)
        new_t = new.replace('\n', nl)
        if old_t not in text:
            print('  MISS %s' % path)
            print('    expected: %r' % old_t[:120])
            return False
        text = text.replace(old_t, new_t, 1)
        changed.append(marker)
    if changed:
        write(path, text)
        print('  patched %s -> %s' % (path, changed))
    else:
        print('  already patched %s' % path)
    return True


def main():
    ok = True

    target = os.path.join(CORE, 'Modding', 'BakedContent.cs')
    if not os.path.exists(target):
        write(target, BAKED_CONTENT)
        print('wrote %s' % target)
    else:
        print('exists %s' % target)

    ok &= patch(os.path.join(CORE, 'Helpers', 'OneTimeInitialization.cs'), [
        ('BakedContent.Mount();',
         '''		_state = State.VeryEarly;
		if (TestMode.IsOn)''',
         '''		_state = State.VeryEarly;
		BakedContent.Mount();
		if (TestMode.IsOn)'''),
    ])

    ok &= patch(os.path.join(CORE, 'Modding', 'ModManager.cs'), [
        ('BakedContent.GetLocTables',
         '''		foreach (Mod mod in _mods)
		{
			if (mod.state == ModLoadState.Loaded)
			{
				string text = $"res://{mod.manifest.id}/localization/{language}/{file}";
				if (ResourceLoader.Exists(text))
				{
					yield return text;
				}
			}
		}
	}''',
         '''		foreach (Mod mod in _mods)
		{
			if (mod.state == ModLoadState.Loaded)
			{
				string text = $"res://{mod.manifest.id}/localization/{language}/{file}";
				if (ResourceLoader.Exists(text))
				{
					yield return text;
				}
			}
		}
		foreach (string bakedLocTable in BakedContent.GetLocTables(language, file))
		{
			yield return bakedLocTable;
		}
	}'''),
    ])

    ok &= patch(os.path.join(CORE, 'Localization', 'LocManager.cs'), [
        ('BakedContent.WithUnprefixedAliases',
         '''				locTable.MergeWith(dictionary4);
			}
			dictionary2[fileNameWithoutExtension] = locTable;''',
         '''				locTable.MergeWith(dictionary4);
				if (BakedContent.IsBakedLocTable(moddedLocTable))
				{
					locTable.MergeWith(BakedContent.WithUnprefixedAliases(dictionary4));
				}
			}
			dictionary2[fileNameWithoutExtension] = locTable;'''),
    ])

    if not ok:
        sys.exit(1)
    print('batch 5 patch complete')


if __name__ == '__main__':
    main()
