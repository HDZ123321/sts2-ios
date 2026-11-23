"""Ported generator: stage_watcher.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import os
import shutil

SRC = r"C:\Users\pc\AppData\Local\Temp\watcher\decomp"
DST = _REPO + "/src/Watcher"

SKIP_DIRS = ("Watcher.Code.Tests", "GodotPlugins.Game", "Properties",
             "System.Runtime.CompilerServices")

if os.path.isdir(DST):
    shutil.rmtree(DST)

copied = 0
for root, _dirs, files in os.walk(SRC):
    for name in files:
        if not name.endswith(".cs"):
            continue
        if name.startswith("--"):
            continue
        rel = os.path.relpath(os.path.join(root, name), SRC).replace(os.sep, "/")
        if rel == "Properties/AssemblyInfo.cs":
            continue
        if any(rel.startswith(d + "/") for d in SKIP_DIRS):
            continue
        parts = rel.split("/")
        if parts[0] == "Watcher.Code":
            parts = parts[1:]
        else:
            parts[0] = parts[0].replace("Watcher.Code.", "").replace(".", "/")
        out = os.path.join(DST, *parts)
        os.makedirs(os.path.dirname(out), exist_ok=True)
        shutil.copy2(os.path.join(root, name), out)
        copied += 1

print("copied", copied)
