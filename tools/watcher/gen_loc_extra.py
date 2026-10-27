"""Batch 7: supply the scry hover tip text.

BaseLib ships that string in its own pack, which the port does not have, so the
two keys the compatibility shim asks for are authored here and merged into the
"powers" table after localization loads.
"""

import io
import os
import sys

ROOT = r'E:\sts2_port\ci_bundle'
CORE = os.path.join(ROOT, 'src', 'Core')


def read(path):
    with io.open(path, 'r', encoding='utf-8-sig', newline='') as f:
        return f.read()


def write(path, text):
    with io.open(path, 'w', encoding='utf-8', newline='') as f:
        f.write(text)


def patch(path, marker, old, new):
    text = read(path)
    if marker in text:
        print('  already patched %s' % path)
        return True
    nl = '\r\n' if '\r\n' in text else '\n'
    old_t, new_t = old.replace('\n', nl), new.replace('\n', nl)
    if old_t not in text:
        print('  MISS %s\n    %r' % (path, old_t[:120]))
        return False
    write(path, text.replace(old_t, new_t, 1))
    print('  patched %s' % path)
    return True


def main():
    ok = True

    ok &= patch(os.path.join(CORE, 'Localization', 'LocManager.cs'),
                'public void AddEntries(',
                '''	public LocTable GetTable(string name)''',
                '''	/// <summary>
	/// Adds entries to an already loaded table. Used for content that ships inside the build
	/// and therefore has no localization file of its own.
	/// </summary>
	public void AddEntries(string tableName, Dictionary<string, string> entries)
	{
		if (!_tables.TryGetValue(tableName, out LocTable value))
		{
			Log.Error("Cannot add entries to unknown localization table " + tableName);
			return;
		}
		value.MergeWith(entries);
	}

	public LocTable GetTable(string name)''')

    ok &= patch(os.path.join(CORE, 'Modding', 'BakedContent.cs'),
                'InjectLocEntries',
                '''	public static bool IsBakedLocTable(string path)''',
                '''	/// <summary>
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

	public static bool IsBakedLocTable(string path)''')

    ok &= patch(os.path.join(CORE, 'Helpers', 'OneTimeInitialization.cs'),
                'BakedContent.InjectLocEntries',
                '''		LocManager.Initialize();''',
                '''		LocManager.Initialize();
		BakedContent.InjectLocEntries();''')

    if not ok:
        sys.exit(1)
    print('batch 7 patch complete')


if __name__ == '__main__':
    main()
