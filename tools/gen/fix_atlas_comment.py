"""Ported generator: fix_atlas_comment.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import io

PATH = _REPO + "/src/Core/Assets/AtlasManager.cs"

OLD = '''	/// <summary>
	/// Atlases that only a run draws from. They used to be deferred until a run started so the main
	/// menu would not pay their VRAM, but deferring them means any scene that asks for one of their
	/// sprites during the startup preload triggers a synchronous AtlasManager.LoadAtlas from inside
	/// ResourceLoader's loader thread - new work on a thread that previously always found the atlas
	/// resident. That is the shape the phone crashed in, so they are resident from the first frame
	/// again and <see cref="M:MegaCrit.Sts2.Core.Assets.AtlasManager.LoadRunAtlases" /> is now only a
	/// cheap early-out.
	/// </summary>'''

NEW = '''	/// <summary>
	/// Atlases that only a run draws from. On desktop they are deferred until a run starts so the
	/// main menu does not pay their VRAM - card_atlas alone is 41 MPx. On mobile they are kept
	/// resident from the first frame instead: deferring them there means any scene that asks for one
	/// of their sprites during the startup preload triggers a synchronous AtlasManager.LoadAtlas from
	/// inside ResourceLoader's loader thread, which is new work on a thread that previously always
	/// found the atlas resident and is the shape the phone crashed in.
	/// </summary>'''


def newline_for(text):
    return "\r\n" if "\r\n" in text else "\n"


with io.open(PATH, encoding="utf-8") as handle:
    text = handle.read()

nl = newline_for(text)
old = OLD.replace("\n", nl)
new = NEW.replace("\n", nl)

if new in text:
    print("already applied")
elif old not in text:
    raise SystemExit("MISS")
else:
    text = text.replace(old, new, 1)
    with io.open(PATH, "w", encoding="utf-8", newline="") as handle:
        handle.write(text)
    print("comment updated")
