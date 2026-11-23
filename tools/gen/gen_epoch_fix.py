"""Ported generator: gen_epoch_fix.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import io

PATH = _REPO + "/src/Core/Saves/Managers/ProgressSaveManager.cs"

with io.open(PATH, "r", encoding="utf-8-sig", newline="") as f:
    text = f.read()

newline = "\r\n" if "\r\n" in text else "\n"

OLD = """							if (!(character is Deprived))
							{
								throw new ArgumentOutOfRangeException("character", character, null);
							}
							epochModel = null;""".replace("\n", newline)

NEW = """							if (!(character is Deprived))
							{
								// A character that is not one of the shipped ones (a ported or
								// modded character) has no fifteen-elite epoch to unlock.
								return;
							}
							epochModel = null;""".replace("\n", newline)

if OLD in text:
    text = text.replace(OLD, NEW, 1)
    changed = 1
elif "has no fifteen-elite epoch to unlock" in text:
    changed = 0
else:
    raise SystemExit("epoch guard not found")

with io.open(PATH, "w", encoding="utf-8-sig", newline="") as f:
    f.write(text)

print("patched %d site(s)" % changed)