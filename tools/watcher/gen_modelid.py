"""Batch 8: reproduce the mod loader's model-id scheme.

The real mod registers every model under `WATCHER-<SLUG>` (the live progress save
stores `CHARACTER.WATCHER-WATCHER`, and the content pack keys are
`WATCHER-AMBROSIA.title`). Without the prefix our port would invent new ids,
orphaning the stats already in the save and missing every localization key.
"""

import io
import os
import sys

ROOT = r'E:\sts2_port\ci_bundle'
CORE = os.path.join(ROOT, 'src', 'Core')
WATCHER = os.path.join(ROOT, 'src', 'Watcher')


def read(path):
    with io.open(path, 'r', encoding='utf-8-sig', newline='') as f:
        return f.read()


def write(path, text):
    with io.open(path, 'w', encoding='utf-8', newline='') as f:
        f.write(text)


def patch(path, marker, old, new):
    text = read(path)
    if marker in text:
        print('  already patched %s' % os.path.basename(path))
        return True
    nl = '\r\n' if '\r\n' in text else '\n'
    old_t, new_t = old.replace('\n', nl), new.replace('\n', nl)
    if old_t not in text:
        print('  MISS %s\n    %r' % (path, old_t[:160]))
        return False
    write(path, text.replace(old_t, new_t, 1))
    print('  patched %s' % os.path.basename(path))
    return True


def main():
    ok = True

    # 1. Model ids: Watcher.Code.* gets the loader's prefix so saves and loc keys line up.
    ok &= patch(os.path.join(CORE, 'Models', 'ModelDb.cs'),
                'ModEntryPrefix',
                '''	public static string GetEntry(Type type)''',
                '''	/// <summary>The prefix the content was published under, kept so existing saves still resolve.</summary>
	private const string ModNamespaceRoot = "Watcher.Code";

	private const string ModEntryPrefix = "WATCHER-";

	public static string GetEntry(Type type)''')

    ok &= patch(os.path.join(CORE, 'Models', 'ModelDb.cs'),
                'ModEntryPrefix + StringHelper.Slugify',
                '''		return StringHelper.Slugify(type.Name);''',
                '''		string slug = StringHelper.Slugify(type.Name);
		if (type.Namespace != null && type.Namespace.StartsWith(ModNamespaceRoot, StringComparison.Ordinal))
		{
			return ModEntryPrefix + slug;
		}
		return slug;''')

    # 2. Stance models used to prepend the prefix by hand; Id.Entry already carries it now.
    ok &= patch(os.path.join(WATCHER, 'Stances', 'WatcherStanceModel.cs'),
                '.title");',
                '''new LocString("powers", "Watcher".ToUpperInvariant() + "-" + base.Id.Entry + ".title")''',
                '''new LocString("powers", base.Id.Entry + ".title")''')

    ok &= patch(os.path.join(WATCHER, 'Stances', 'WatcherStanceModel.cs'),
                '.description");',
                '''new LocString("powers", "Watcher".ToUpperInvariant() + "-" + base.Id.Entry + ".description")''',
                '''new LocString("powers", base.Id.Entry + ".description")''')

    # 3. Asset names drop the same prefix, so it has to match the id prefix exactly.
    ok &= patch(os.path.join(WATCHER, 'Extensions', 'StringExtensions.cs'),
                'string prefix = "WATCHER-"',
                '''string prefix = "WATCHER_"''',
                '''string prefix = "WATCHER-"''')

    if not ok:
        sys.exit(1)
    print('batch 8 patch complete')


if __name__ == '__main__':
    main()
