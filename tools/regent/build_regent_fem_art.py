#!/usr/bin/env python3
"""Bake the RegentFemPortraits card-art set into the build.

The upstream mod (``RegentFemPortraits`` 1.0, "Regent 760", 2026-06-21) ships a PCK with 60
replacement card portraits plus a DLL that applies them with Harmony. Neither the DLL nor the
mod loader that would host it survives the NativeAOT build the iOS client is compiled with, so
this tool lifts the art out of the pack and rebakes it the same way ``build_regent_art.py``
rebakes the anime set:

1. Reads the upstream PCK, keeps only the 60 portraits' ``.import`` remaps and the ``.ctex``
   blobs those remaps point at, and drops everything the native build must not mount:
   ``res://.godot/uid_cache.bin``, ``res://.godot/global_script_class_cache.cfg``, the mod's
   C# sources, its localization table, ``project.binary``, the store thumbnail and its
   ``card_replacements.json`` manifest. Mounting a pack that carries the two ``.godot`` caches
   shadows the game's own copies and breaks every ``uid://`` reference in the base game.
2. Repacks what is left into ``res/RegentFemArt.pck`` with Godot's own ``PCKPacker``. Paths
   inside the pack are kept verbatim (``res://RegentFemPortraits/card_portraits/regent/*.png``
   and the matching ``res://.godot/imported/*.ctex``) because each ``.import`` remap hardcodes
   the ``.ctex`` path it points at. Keeping the upstream namespace also means this pack never
   collides with ``RegentArt.pck``, which lives under ``res://generated/assets/card_art/``.
3. Generates ``src/Core/Modding/RegentFemCardArtTable.cs``: card CLR type name -> texture path,
   so ``RegentCardArt`` can pick the set without reading JSON at runtime.

The upstream art is imported as ``compress/mode`` lossless (the ``.ctex`` payloads are WEBP
mipmap chains, 1000x760 for the normal cards and 884x1244 for the one full-card ancient
illustration), which decodes to plain RGBA8 on any renderer -- no ASTC/ETC2 re-encode is
required for the iOS build.

Usage:
    python tools/regent/build_regent_fem_art.py --pack <upstream RegentFemPortraits.pck>
"""

from __future__ import annotations

import argparse
import io
import json
import os
import struct
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(os.path.dirname(HERE))
sys.path.insert(0, HERE)

from build_regent_art import build_pack, find_godot  # noqa: E402  (sibling tool, shared PCKPacker driver)

TABLE_PATH = os.path.join(REPO, "src", "Core", "Modding", "RegentFemCardArtTable.cs")
PACK_PATH = os.path.join(REPO, "res", "RegentFemArt.pck")
DEFAULT_PACK = os.path.join(REPO, "build", "regentfem", "src", "RegentFemPortraits", "RegentFemPortraits.pck")
EXTRACT_DIR = os.path.join(REPO, "build", "regentfem", "extracted")
CONFIG_PATH = "RegentFemPortraits/card_replacements.json"
PORTRAIT_PREFIX = "res://RegentFemPortraits/card_portraits/"
CARD_NAMESPACE = "MegaCrit.Sts2.Core.Models.Cards."


# --------------------------------------------------------------------------- pack reading

def read_pck(path: str) -> tuple[bytes, dict[str, tuple[int, int]]]:
    """Return the raw file bytes and ``path -> (offset, size)`` for every entry.

    Godot 3/4 PCK layout used here: 112-byte header (magic ``GDPC``, pack version, engine
    version, ``file_base`` at +24, directory offset at +32), then a directory of
    ``u32 path_len, path, u64 offset, u64 size, 16-byte md5, u32 flags`` rows whose offsets are
    relative to ``file_base``.
    """
    with open(path, "rb") as handle:
        blob = handle.read()
    if blob[:4] != b"GDPC":
        raise SystemExit(f"not a Godot PCK: {path}")
    file_base = struct.unpack_from("<Q", blob, 24)[0]
    dir_offset = struct.unpack_from("<Q", blob, 32)[0]
    cursor = dir_offset
    count = struct.unpack_from("<I", blob, cursor)[0]
    cursor += 4
    entries: dict[str, tuple[int, int]] = {}
    for _ in range(count):
        name_len = struct.unpack_from("<I", blob, cursor)[0]
        cursor += 4
        name = blob[cursor:cursor + name_len].rstrip(b"\x00").decode("utf-8")
        cursor += name_len
        offset, size = struct.unpack_from("<QQ", blob, cursor)
        cursor += 16 + 16 + 4
        entries[name] = (file_base + offset, size)
    return blob, entries


def read_entry(blob: bytes, entries: dict[str, tuple[int, int]], name: str) -> bytes:
    offset, size = entries[name]
    return blob[offset:offset + size]


def remap_targets(import_text: str) -> list[str]:
    """``res://`` paths an ``.import`` remap points at (single ``path=`` form)."""
    targets: list[str] = []
    for line in import_text.splitlines():
        line = line.strip()
        if line.startswith("path=") or line.startswith("path.") and "=" in line:
            value = line.split("=", 1)[1].strip().strip('"')
            if value.startswith("res://"):
                targets.append(value)
    return targets


# --------------------------------------------------------------------------- extraction

def extract(blob: bytes, entries: dict[str, tuple[int, int]], config_json: str) -> list[str]:
    """Write the portraits plus their remaps under EXTRACT_DIR; return the kept res:// paths."""
    # res://...png (the path managed code asks for) -> the .import remap that resolves it.
    wanted: dict[str, str] = {}
    for name in sorted(entries):
        if not name.endswith(".import"):
            continue
        resource = "res://" + name[: -len(".import")]
        if not resource.startswith(PORTRAIT_PREFIX):
            continue
        text = read_entry(blob, entries, name).decode("utf-8", "replace")
        for target in remap_targets(text):
            if target[len("res://"):] not in entries:
                raise SystemExit(f"{name} remaps to a path the pack does not carry: {target}")
        wanted[resource] = text

    # Every portrait the config asks for has to exist, otherwise the table would ship dead rows.
    config = json.loads(config_json)
    referenced: set[str] = set()
    for entry in config.get("normalReplacements") or []:
        referenced.add(entry["portraitPath"])
    for entry in config.get("ancientReplacements") or []:
        for key in ("ancientPortrait", "normalPortrait"):
            if entry.get(key):
                referenced.add(entry[key])
    carried = set(wanted)
    missing = sorted(referenced - carried)
    if missing:
        raise SystemExit("config references portraits the pack does not carry: " + ", ".join(missing))
    unused = sorted(carried - referenced)
    if unused:
        print("warning: dropping %d portrait(s) the config never references: %s" % (len(unused), ", ".join(unused)))

    # The pack carries the remap (``x.png.import``), never the source ``x.png`` itself: the
    # engine resolves the resource path through the remap, exactly like the anime set.
    keep: list[str] = []
    for path in sorted(referenced):
        keep.append(path + ".import")
        for target in remap_targets(wanted[path]):
            keep.append(target)

    for path in keep:
        inner = path[len("res://"):]
        if inner not in entries:
            raise SystemExit(f"missing pack entry for {path}")
        destination = os.path.join(EXTRACT_DIR, inner.replace("/", os.sep))
        os.makedirs(os.path.dirname(destination), exist_ok=True)
        with open(destination, "wb") as handle:
            handle.write(read_entry(blob, entries, inner))
    return keep


# --------------------------------------------------------------------------- table

def generate_table(config_json: str) -> int:
    config = json.loads(config_json)
    rows: dict[str, str] = {}

    def add(card_type: str, portrait: str) -> None:
        if not card_type or not portrait:
            return
        if not portrait.startswith(PORTRAIT_PREFIX):
            raise SystemExit(f"unexpected portrait path for {card_type}: {portrait}")
        source = os.path.join(REPO, "src", "Core", "Models", "Cards", card_type + ".cs")
        if not os.path.isfile(source):
            raise SystemExit(f"card type {card_type} has no src/Core/Models/Cards/{card_type}.cs")
        rows[CARD_NAMESPACE + card_type] = portrait

    for entry in config.get("normalReplacements") or []:
        add(entry["cardType"], entry["portraitPath"])
    for entry in config.get("ancientReplacements") or []:
        # The upstream mod keeps a second, framed portrait per ancient card and defaults the
        # whole-card illustration on (ConfigHelper.DefaultAncientStyleEnabled == true), which is
        # the variant a fresh install shows. Bake that one; the framed alternative would need a
        # runtime toggle the native build has no settings UI for.
        add(entry["cardType"], entry.get("ancientPortrait") or entry.get("normalPortrait") or "")

    ordered = sorted(rows.items())
    lines: list[str] = []
    lines.append("// <auto-generated>")
    lines.append("// Generated by tools/regent/build_regent_fem_art.py from the upstream")
    lines.append("// RegentFemPortraits mod's card_replacements.json. Do not edit by hand: rerun the tool.")
    lines.append("// </auto-generated>")
    lines.append("")
    lines.append("using System.Collections.Generic;")
    lines.append("")
    lines.append("namespace MegaCrit.Sts2.Core.Modding;")
    lines.append("")
    lines.append("/// <summary>")
    lines.append("/// Card CLR type name -> replacement portrait path, baked in by RegentFemArt.pck.")
    lines.append("/// Keyed by the fully qualified type name so a ported character that reuses a vanilla")
    lines.append("/// class name keeps its own art.")
    lines.append("/// </summary>")
    lines.append("internal static class RegentFemCardArtTable")
    lines.append("{")
    lines.append("\tinternal const int Count = " + str(len(ordered)) + ";")
    lines.append("")
    lines.append("\tprivate static readonly Dictionary<string, string> _byCardType = new Dictionary<string, string>")
    lines.append("\t{")
    for name, path in ordered:
        lines.append('\t\t{ "' + name + '", "' + path + '" },')
    lines.append("\t};")
    lines.append("")
    lines.append("\tinternal static bool TryGet(string cardTypeName, out string path)")
    lines.append("\t{")
    lines.append("\t\treturn _byCardType.TryGetValue(cardTypeName, out path);")
    lines.append("\t}")
    lines.append("}")
    lines.append("")

    os.makedirs(os.path.dirname(TABLE_PATH), exist_ok=True)
    with io.open(TABLE_PATH, "w", encoding="utf-8", newline="\n") as handle:
        handle.write("\n".join(lines))
    return len(ordered)


# --------------------------------------------------------------------------- main

def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--pack", default=DEFAULT_PACK)
    parser.add_argument("--godot", default=None)
    parser.add_argument("--skip-pack", action="store_true")
    parser.add_argument("--skip-extract", action="store_true")
    args = parser.parse_args()

    pack_path = os.path.abspath(args.pack)
    if not os.path.isfile(pack_path):
        raise SystemExit(f"missing upstream pack: {pack_path}")

    blob, entries = read_pck(pack_path)
    if CONFIG_PATH not in entries:
        raise SystemExit(f"{pack_path} does not carry {CONFIG_PATH}")
    config_json = read_entry(blob, entries, CONFIG_PATH).decode("utf-8")

    if args.skip_extract:
        kept = []
        for current, _, files in os.walk(EXTRACT_DIR):
            for name in files:
                absolute = os.path.join(current, name)
                relative = os.path.relpath(absolute, EXTRACT_DIR).replace(os.sep, "/")
                kept.append("res://" + relative)
    else:
        kept = extract(blob, entries, config_json)
    print(f"extract: {len(kept)} file(s) -> {EXTRACT_DIR}")

    count = generate_table(config_json)
    print(f"table: {count} card(s) -> {TABLE_PATH}")

    if args.skip_pack:
        return 0

    godot = find_godot(args.godot)
    entries_out = [
        {"target": path, "source": os.path.join(EXTRACT_DIR, path[len('res://'):].replace("/", os.sep))}
        for path in kept
    ]
    entries_out.sort(key=lambda row: row["target"])
    build_pack(godot, entries_out, PACK_PATH)
    print(f"pack: {os.path.getsize(PACK_PATH)} bytes -> {PACK_PATH}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
