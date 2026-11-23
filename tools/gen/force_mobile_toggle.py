"""Ported generator: force_mobile_toggle.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

Temporarily force the mobile branches so the desktop build can be checked against
the shape iOS will get. Run with --on to force, --off to restore.
"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import io
import sys

ATLAS = _REPO + "/src/Core/Assets/AtlasManager.cs"
SESSION = _REPO + "/src/Core/Assets/AssetLoadingSession.cs"

PAIRS = [
    (ATLAS, 'if (OS.HasFeature("mobile"))', 'if (true || OS.HasFeature("mobile"))'),
    (SESSION, 'OS.HasFeature("mobile") ? 1 : 6', 'true ? 1 : 6'),
]

mode = sys.argv[1] if len(sys.argv) > 1 else "--on"

for path, plain, forced in PAIRS:
    with io.open(path, encoding="utf-8") as handle:
        text = handle.read()
    src, dst = (plain, forced) if mode == "--on" else (forced, plain)
    if dst in text:
        print("%s: already %s" % (path.rsplit("\\", 1)[-1], mode))
        continue
    if src not in text:
        raise SystemExit("MISS in " + path)
    text = text.replace(src, dst)
    with io.open(path, "w", encoding="utf-8", newline="") as handle:
        handle.write(text)
    print("%s: %s" % (path.rsplit("\\", 1)[-1], mode))
