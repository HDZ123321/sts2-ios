"""Ported generator: fix_mobile_preload2.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

Make the atlas residency platform-conditional.

History:
  cf9ffa4 split the 13 atlases into a menu group (10) and a run group (3: card_atlas,
  power_atlas, intent_atlas) so the main menu would not pay 41 MPx of card art. On
  desktop that is a straight win: -96 ms to the menu, -64 MB steady VRAM, and the
  deferred atlases are pulled in lazily the moment a run scene needs them.

  On iOS the build that shipped with that split (22d6197) hard-crashed during the
  Common preload with no managed exception in the log. The loader thread calling
  AtlasManager.LoadAtlas synchronously for a not-yet-resident atlas is a plausible
  trigger, and it is new work on a thread that previously always found the atlas
  resident. So the split stays a desktop optimisation only: on mobile every atlas is
  resident before the main menu, exactly the shape that ran fine before cf9ffa4.

The batch size is NOT part of this: 512 measured inside the noise of 128 on desktop
(482/475/453 ms against 501 ms) so there is nothing to win, and it quadruples the
peak of fully decoded resources held between frames. 128 everywhere.
"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import io
import os
import re

PATH = _REPO + "/src/Core/Assets/AtlasManager.cs"

MARKER = "public static void LoadAllAtlases()\n\t{\n\t\tif (OS.HasFeature(\"mobile\"))"


def newline_for(text):
    return "\r\n" if "\r\n" in text else "\n"


def patch(text, old, new):
    nl = newline_for(text)
    if "\r\n" in old:
        old = old.replace("\n", "\r\n")
        new = new.replace("\n", "\r\n")
    else:
        old = old.replace("\r\n", "\n")
        new = new.replace("\r\n", "\n")
    if new in text:
        return text, False
    if old not in text:
        raise SystemExit("MISS: " + old.splitlines()[0])
    return text.replace(old, new, 1), True


with io.open(PATH, encoding="utf-8") as handle:
    text = handle.read()

edits = 0

old_load_all = '''	/// <summary>
	/// Loads every atlas that is kept resident outside of a run.
	/// </summary>
	public static void LoadAllAtlases()
	{
		string[] menuAtlases = _menuAtlases;
		foreach (string atlasName in menuAtlases)
		{
			LoadAtlas(atlasName);
		}
		string[] runAtlases = _runAtlases;
		foreach (string atlasName2 in runAtlases)
		{
			LoadAtlas(atlasName2);
		}
	}'''

new_load_all = '''	/// <summary>
	/// Loads every atlas that is kept resident outside of a run. On mobile this is all thirteen,
	/// because deferring a run atlas there means the startup preload can ask for one of its sprites
	/// while ResourceLoader is running on a loader thread, and that thread would then call
	/// <see cref="M:MegaCrit.Sts2.Core.Assets.AtlasManager.LoadAtlas" /> synchronously - new work on a
	/// thread that previously always found the atlas resident. On desktop the run atlases stay
	/// deferred and <see cref="M:MegaCrit.Sts2.Core.Assets.AtlasManager.LoadRunAtlases" /> picks them up.
	/// </summary>
	public static void LoadAllAtlases()
	{
		if (OS.HasFeature("mobile"))
		{
			string[] allAtlases = _menuAtlases;
			foreach (string atlasName in allAtlases)
			{
				LoadAtlas(atlasName);
			}
			string[] deferredAtlases = _runAtlases;
			foreach (string atlasName2 in deferredAtlases)
			{
				LoadAtlas(atlasName2);
			}
			return;
		}
		string[] menuAtlases = _menuAtlases;
		foreach (string atlasName3 in menuAtlases)
		{
			LoadAtlas(atlasName3);
		}
	}'''

text, changed = patch(text, old_load_all, new_load_all)
edits += int(changed)

with io.open(PATH, "w", encoding="utf-8", newline="") as handle:
    handle.write(text)

print("AtlasManager.cs: %d edit(s)" % edits)
print("done")
