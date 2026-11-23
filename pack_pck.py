"""Build the iOS sts2.pck from the unpacked v0.113.1 tree.

Swaps the desktop-only GPU texture variants (.bptc / .s3tc) for the iOS ones
(.astc / .etc2) that were recooked with Godot, and rewrites the matching
.import remap files. Everything else is copied byte for byte.

PCK layout (Godot 4.5, PACK_FORMAT_VERSION 3, PACK_REL_FILEBASE):
    u32 magic 'GDPC' | u32 version | u32 major | u32 minor | u32 patch
    u32 flags | u64 file_base | u64 dir_off | 16 * u32 reserved   -> 112 bytes
    file payloads, each starting on a 16-byte boundary
    u32 count | entries, each:
        u32 path_len (padded to 4) | path bytes | zero pad
        u64 offset - file_base | u64 size | 16 byte md5 | u32 flags

Usage, from the repo root:

    # default: look for ../pck113, ../pck113_tex, ../_newimports.json, write ../dist/sts2.pck
    python3 pack_pck.py

    # explicit paths
    python3 pack_pck.py --tree ../pck113 --tex ../pck113_tex \
                        --imports ../_newimports.json --out ../dist/sts2.pck
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import struct
import sys

REPO_ROOT = os.path.dirname(os.path.abspath(__file__))
WORKSPACE = os.path.dirname(REPO_ROOT)

DEFAULT_TREE = os.path.join(WORKSPACE, "pck113")
DEFAULT_TEX = os.path.join(WORKSPACE, "pck113_tex")
DEFAULT_IMPORTS = os.path.join(WORKSPACE, "_newimports.json")
DEFAULT_OUT = os.path.join(WORKSPACE, "dist", "sts2.pck")

PACK_HEADER_MAGIC = 0x43504447  # 'GDPC'
PACK_FORMAT_VERSION = 3
GODOT_VERSION = (4, 5, 1)
PACK_REL_FILEBASE = 2
PCK_PADDING = 16
HEADER_SIZE = 104  # 24 + 8 + 8 + 64; padding then lifts file_base to 112
REPLACED_SUFFIXES = (".bptc.ctex", ".s3tc.ctex")

# Files whose shipped bytes differ from what the game's own pack carries.
# The upstream fmod.gdextension points the iOS libraries at .xcframework
# bundles, and Godot treats any .xcframework path as a static library
# (is_static_library = path ends with ".a" or ".xcframework"), so it calls
# open_dynamic_library("") which returns RTLD_SELF and the entry symbol lookup
# then fails with "GDExtension entry point 'fmod_library_init' not found in
# library ." - i.e. no audio at all. The port's copy names the .framework
# directories that the iOS bundle actually ships under Frameworks/, which makes
# Godot resolve them through the real dlopen path in os_ae.mm.
OVERRIDES = {
    "addons/fmod/fmod.gdextension": os.path.join(REPO_ROOT, "addons/fmod/fmod.gdextension"),
}


def pad_to(alignment: int, n: int) -> int:
    rest = n % alignment
    return 0 if rest == 0 else alignment - rest


def walk_files(root: str) -> list[str]:
    out: list[str] = []
    for base, _dirs, files in os.walk(root):
        for name in files:
            full = os.path.join(base, name)
            rel = os.path.relpath(full, root).replace("\\", "/")
            out.append(rel)
    out.sort()
    return out


def collect_entries(src_tree: str, tex_tree: str, imports_json: str) -> list[tuple[str, bytes]]:
    """Return (pck path, payload) for every file the pack should hold."""
    with open(imports_json, encoding="utf-8") as fh:
        rewritten = {item["src"] + ".import": item["import"] for item in json.load(fh)}

    entries: list[tuple[str, bytes]] = []
    seen: set[str] = set()
    skipped = 0
    overridden = 0

    for rel in walk_files(src_tree):
        if rel.endswith(REPLACED_SUFFIXES):
            skipped += 1
            continue
        if rel in rewritten:
            payload = rewritten[rel].encode("utf-8")
        elif rel in OVERRIDES:
            with open(OVERRIDES[rel], "rb") as fh:
                payload = fh.read()
            overridden += 1
        else:
            with open(os.path.join(src_tree, rel), "rb") as fh:
                payload = fh.read()
        entries.append((rel, payload))
        seen.add(rel)

    absent = sorted(set(OVERRIDES) - seen)
    if absent:
        raise SystemExit(f"override targets absent from the tree: {absent}")

    missing = sorted(set(rewritten) - seen)
    if missing:
        raise SystemExit(f"rewritten .import files absent from the tree: {missing[:5]}")

    added = 0
    for rel in walk_files(tex_tree):
        if rel in seen:
            raise SystemExit(f"duplicate entry {rel}")
        with open(os.path.join(tex_tree, rel), "rb") as fh:
            entries.append((rel, fh.read()))
        seen.add(rel)
        added += 1

    print(f"[pack] tree files {len(entries) - added}, recooked {added}, dropped {skipped}, overridden {overridden}")
    return entries


def write_pck(entries: list[tuple[str, bytes]], out_path: str) -> None:
    entries.sort(key=lambda item: item[0])
    os.makedirs(os.path.dirname(os.path.abspath(out_path)), exist_ok=True)

    with open(out_path, "wb") as fh:
        fh.write(struct.pack("<4s5I", b"GDPC", PACK_FORMAT_VERSION, *GODOT_VERSION, PACK_REL_FILEBASE))
        file_base_ofs = fh.tell()
        fh.write(struct.pack("<Q", 0))
        dir_base_ofs = fh.tell()
        fh.write(struct.pack("<Q", 0))
        fh.write(struct.pack("<16I", *([0] * 16)))
        assert fh.tell() == HEADER_SIZE

        fh.write(b"\x00" * pad_to(PCK_PADDING, fh.tell()))
        file_base = fh.tell()

        records: list[tuple[bytes, int, int, bytes]] = []
        for rel, payload in entries:
            offset = fh.tell()
            fh.write(payload)
            fh.write(b"\x00" * pad_to(PCK_PADDING, fh.tell()))
            records.append((rel.encode("utf-8"), offset - file_base, len(payload), hashlib.md5(payload).digest()))

        fh.write(b"\x00" * pad_to(PCK_PADDING, fh.tell()))
        dir_off = fh.tell()
        fh.write(struct.pack("<I", len(records)))
        for path, offset, size, digest in records:
            path_pad = pad_to(4, len(path))
            fh.write(struct.pack("<I", len(path) + path_pad))
            fh.write(path)
            fh.write(b"\x00" * path_pad)
            fh.write(struct.pack("<QQ", offset, size))
            fh.write(digest)
            fh.write(struct.pack("<I", 0))

        fh.seek(file_base_ofs)
        fh.write(struct.pack("<Q", file_base))
        fh.seek(dir_base_ofs)
        fh.write(struct.pack("<Q", dir_off))

    print(f"[pack] wrote {out_path} ({os.path.getsize(out_path)} bytes, {len(entries)} entries)")


def main() -> int:
    ap = argparse.ArgumentParser(description="Rebuild the iOS sts2.pck from an unpacked tree.")
    ap.add_argument("--tree", default=DEFAULT_TREE, help="unpacked PCK tree (default: ../pck113)")
    ap.add_argument("--tex", default=DEFAULT_TEX, help="recooked ASTC/ETC2 tree (default: ../pck113_tex)")
    ap.add_argument("--imports", default=DEFAULT_IMPORTS, help="rewritten .import map (default: ../_newimports.json)")
    ap.add_argument("--out", default=DEFAULT_OUT, help="output PCK (default: ../dist/sts2.pck)")
    args = ap.parse_args()

    for label, path in (("tree", args.tree), ("tex", args.tex), ("imports", args.imports)):
        if not os.path.exists(path):
            raise SystemExit(f"{label} not found: {path}")

    entries = collect_entries(args.tree, args.tex, args.imports)
    write_pck(entries, args.out)
    return 0


if __name__ == "__main__":
    sys.exit(main())
