"""Ported generator: fix_vfx_window.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

Make the VFX load window platform-conditional.

The window of 6 cut the Common preload's VFX tail from 1632 ms to 215 ms on desktop and
that is worth keeping there. On mobile the queue was serialised for a reason that has
not been re-verified on that platform: VFX scenes share ext_resources such as
canvas_item_material_additive_shared.tres, and Godot's threaded loader races on shared
sub-resource loading. Nothing in the crash log pointed at the VFX phase, but the phone
gets the shape it last ran cleanly with, and the desktop keeps the win.
"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import io

PATH = _REPO + "/src/Core/Assets/AssetLoadingSession.cs"

OLD = '''	/// <summary>
	/// How many VFX scenes may be in flight at once. They used to be loaded strictly one
	/// at a time because concurrent loading was blamed for "Another resource is loaded from
	/// path" errors on shared ext_resources, but that serialised 92 scenes into a tail that
	/// cost four times more than every other asset in the same preload combined.
	/// </summary>
	private const int _vfxConcurrency = 6;'''

NEW = '''	/// <summary>
	/// How many VFX scenes may be in flight at once. They used to be loaded strictly one
	/// at a time because concurrent loading was blamed for "Another resource is loaded from
	/// path" errors on shared ext_resources, but that serialised 92 scenes into a tail that
	/// cost four times more than every other asset in the same preload combined. The window
	/// is a desktop win only: mobile keeps the serial shape it last ran cleanly with, because
	/// the shared-ext_resource race has never been re-verified on that platform.
	/// </summary>
	private static readonly int _vfxConcurrency = OS.HasFeature("mobile") ? 1 : 6;'''


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
    print("_vfxConcurrency is now platform-conditional")
