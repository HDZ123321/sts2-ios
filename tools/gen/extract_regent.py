"""Ported generator: extract_regent.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

Extract the safe entries of RegentCardsAnimeRework.pck and repack a lean art pack.
"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import hashlib, io, os, struct, sys

SRC = _REPO + "/build/regent/RegentCardsAnimeRework.pck"
OUT_DIR = _REPO + "/build/regent/extracted"

# Entries that must never reach a shipped pack: they would overwrite the base game's
# global script class table and UID map when the pack is mounted.
BLACKLIST_EXACT = {".godot/global_script_class_cache.cfg", ".godot/uid_cache.bin", "project.binary"}
BLACKLIST_PREFIX = ("CardHoverTooltipPatch.cs", "CardReplacementRegistry.cs", "CardUiModeSpoofPatch.cs",
                    "FramePatch.cs", "FrameReplacementRegistry.cs", "HoverTooltipTargetRegistry.cs",
                    "InspectCardHoverTooltipPatch.cs", "MainFile.cs", "PortraitPatch.cs", "TextPatch.cs",
                    "TextReplacementRegistry.cs", "mod/", "mod_manifest.json", "icon.svg")


def read_directory(path):
    with io.open(path, "rb") as f:
        head = f.read(112)
        magic = head[0:4].decode("ascii")
        if magic != "GDPC":
            raise SystemExit("bad magic %r" % magic)
        pack_version = struct.unpack_from("<I", head, 4)[0]
        file_base = struct.unpack_from("<Q", head, 24)[0]
        dir_off = struct.unpack_from("<Q", head, 32)[0]
        f.seek(dir_off)
        count = struct.unpack("<I", f.read(4))[0]
        entries = []
        for _ in range(count):
            plen = struct.unpack("<I", f.read(4))[0]
            raw = f.read(plen)
            entry_path = raw.rstrip(b"\x00").decode("utf-8")
            offset, size = struct.unpack("<QQ", f.read(16))
            md5 = f.read(16)
            flags = struct.unpack("<I", f.read(4))[0]
            entries.append((entry_path, offset, size, md5, flags))
        return pack_version, file_base, entries


def main():
    pack_version, file_base, entries = read_directory(SRC)
    print("pack_version=%d file_base=%d entries=%d" % (pack_version, file_base, len(entries)))
    os.makedirs(OUT_DIR, exist_ok=True)
    kept, dropped, bad = [], [], []
    with io.open(SRC, "rb") as f:
        for entry_path, offset, size, md5, flags in entries:
            if entry_path in BLACKLIST_EXACT or entry_path.startswith(BLACKLIST_PREFIX):
                dropped.append(entry_path)
                continue
            f.seek(file_base + offset)
            data = f.read(size)
            if hashlib.md5(data).digest() != md5:
                bad.append(entry_path)
                continue
            dest = os.path.join(OUT_DIR, entry_path.replace("/", os.sep))
            os.makedirs(os.path.dirname(dest), exist_ok=True)
            with io.open(dest, "wb") as out:
                out.write(data)
            kept.append(entry_path)
    print("kept=%d dropped=%d md5_mismatch=%d" % (len(kept), len(dropped), len(bad)))
    for name in dropped:
        print("  dropped", name)
    for name in bad:
        print("  BAD", name)


main()
