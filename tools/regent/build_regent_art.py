#!/usr/bin/env python3
"""Bake the Regent card-art rework into the build.

The upstream mod (RegentCardsAnimeRework 1.0 by DoublePigeon) ships a PCK with 94 replacement
card portraits plus a DLL that applies them with Harmony. Two things stop it from being used as
is:

* The DLL needs Harmony + Reflection.Emit, neither of which exists in the NativeAOT build the
  iOS client is compiled with, so the patches can never run.
* The PCK carries ``res://.godot/uid_cache.bin`` and ``res://.godot/global_script_class_cache.cfg``.
  Mounting a pack that contains those makes Godot reload the UID cache from the pack, which
  shadows the game's own ``uid_cache.bin`` and breaks every ``uid://`` reference in the base game.

So this tool does three things:

1. Extracts the pack, dropping the two ``.godot`` caches, the DLL-side C# sources and the mod
   manifest -- everything the native build does not need.
2. Repacks what is left (94 ``.ctex`` textures + their 94 ``.import`` remaps) into
   ``res/RegentArt.pck`` with Godot's own ``PCKPacker``. Paths inside the pack are kept verbatim
   (``res://generated/assets/card_art/<Class>_card_art.png`` and the matching
   ``res://.godot/imported/<Class>_card_art.png-<hash>.ctex``) because each ``.import`` remap
   hardcodes the ``.ctex`` path it points at.
3. Generates ``src/Core/Modding/RegentCardArtTable.cs``: card class name -> texture path, so the
   native ``CardModel.PortraitPath`` can look the replacement up without reading JSON at runtime.

Nothing here reads JSON at runtime and nothing depends on Harmony, so the result works in the
NativeAOT client.

Usage:
    python tools/regent/build_regent_art.py --source <extracted dir> --godot <godot console exe>
"""

from __future__ import annotations

import argparse
import io
import json
import os
import subprocess
import sys
import tempfile

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
TABLE_PATH = os.path.join(REPO, "src", "Core", "Modding", "RegentCardArtTable.cs")
PACK_PATH = os.path.join(REPO, "res", "RegentArt.pck")

BUILDER_SCRIPT = '''extends SceneTree

func _init() -> void:
	var job_path := OS.get_environment("REGENT_JOB")
	var job_file := FileAccess.open(job_path, FileAccess.READ)
	if job_file == null:
		push_error("cannot read job file " + job_path)
		quit(1)
		return
	var job = JSON.parse_string(job_file.get_as_text())
	job_file.close()
	if typeof(job) != TYPE_DICTIONARY:
		push_error("job file is not a JSON object")
		quit(1)
		return

	var out_path: String = job["out"]
	var packer := PCKPacker.new()
	var err := packer.pck_start(out_path)
	if err != OK:
		push_error("pck_start failed: %d" % err)
		quit(1)
		return

	var count := 0
	for entry in job["entries"]:
		var target: String = entry["target"]
		var source: String = entry["source"]
		err = packer.add_file(target, source)
		if err != OK:
			push_error("add_file failed for %s: %d" % [target, err])
			quit(1)
			return
		count += 1

	err = packer.flush()
	if err != OK:
		push_error("flush failed: %d" % err)
		quit(1)
		return

	print("REGENT_PACKED ", count, " ", out_path)
	quit(0)
'''


def find_godot(explicit: str | None) -> str:
    candidates = []
    if explicit:
        candidates.append(explicit)
    for name in ("GODOT", "GODOT_BIN"):
        value = os.environ.get(name)
        if value:
            candidates.append(value)
    root = "E:/sts2_port/godot"
    if os.path.isdir(root):
        for folder in sorted(os.listdir(root)):
            for exe in (
                "Godot_v4.5.1-stable_mono_win64_console.exe",
                "Godot_v4.5.1-stable_mono_win64.exe",
                "Godot_v4.5.1-stable_mono_linux.x86_64",
                "godot",
            ):
                candidates.append(os.path.join(root, folder, exe))
    for candidate in candidates:
        if candidate and os.path.isfile(candidate):
            return candidate
    raise SystemExit("cannot find a Godot binary; pass --godot")


def collect_entries(source_dir: str) -> list[dict[str, str]]:
    """Every file the pack needs, with its res:// path preserved verbatim."""
    entries: list[dict[str, str]] = []
    for current, _, files in os.walk(source_dir):
        for name in sorted(files):
            absolute = os.path.join(current, name)
            relative = os.path.relpath(absolute, source_dir).replace(os.sep, "/")
            if relative.endswith(".import"):
                # res://generated/assets/card_art/X_card_art.png.import
                entries.append({"target": "res://" + relative, "source": os.path.abspath(absolute)})
            elif relative.endswith(".ctex") and "_card_art.png-" in relative:
                # res://.godot/imported/X_card_art.png-<hash>.ctex
                entries.append({"target": "res://" + relative, "source": os.path.abspath(absolute)})
    if not entries:
        raise SystemExit(f"no .import/.ctex files under {source_dir}")
    return entries


def build_pack(godot: str, entries: list[dict[str, str]], out_path: str) -> None:
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="regent_pack_") as work:
        project_dir = os.path.join(work, "project")
        os.makedirs(project_dir, exist_ok=True)
        with io.open(os.path.join(project_dir, "project.godot"), "w", encoding="utf-8", newline="\n") as handle:
            handle.write('config_version=5\n\n[application]\n\nconfig/name="regent_pack"\n')
        with io.open(os.path.join(project_dir, "builder.gd"), "w", encoding="utf-8", newline="\n") as handle:
            handle.write(BUILDER_SCRIPT)
        job = {"out": os.path.abspath(out_path), "entries": entries}
        job_path = os.path.join(work, "job.json")
        with io.open(job_path, "w", encoding="utf-8", newline="\n") as handle:
            json.dump(job, handle)
        environment = dict(os.environ)
        environment["REGENT_JOB"] = os.path.abspath(job_path)
        result = subprocess.run(
            [godot, "--headless", "--path", project_dir, "--script", "builder.gd"],
            env=environment,
            capture_output=True,
            encoding="utf-8",
            errors="replace",
            timeout=900,
        )
        if "REGENT_PACKED" not in (result.stdout or ""):
            sys.stderr.write(result.stdout or "")
            sys.stderr.write(result.stderr or "")
            raise SystemExit("PCKPacker run did not report success")
        for line in (result.stdout or "").splitlines():
            if line.startswith("REGENT_PACKED"):
                print(line.strip())


def generate_table(source_dir: str) -> int:
    manifest = os.path.join(source_dir, "generated", "card_replacements.json")
    with io.open(manifest, encoding="utf-8") as handle:
        payload = json.load(handle)
    entries = payload["entries"] if isinstance(payload, dict) else payload
    rows: list[tuple[str, str]] = []
    for entry in entries:
        card_id = entry["cardId"]
        image = entry["image"]
        if not image.startswith("res://"):
            raise SystemExit(f"unexpected image path for {card_id}: {image}")
        # Keyed by the fully qualified CLR type name, not the bare class name: the ported
        # Watcher character ships an Eruption of its own, and a simple-name table would paint
        # Regent art onto it.
        rows.append((card_id, image))
    rows.sort(key=lambda row: row[0])

    lines: list[str] = []
    lines.append("// <auto-generated>")
    lines.append("// Generated by tools/regent/build_regent_art.py from the upstream mod's")
    lines.append("// generated/card_replacements.json. Do not edit by hand: rerun the tool.")
    lines.append("// </auto-generated>")
    lines.append("")
    lines.append("using System.Collections.Generic;")
    lines.append("")
    lines.append("namespace MegaCrit.Sts2.Core.Modding;")
    lines.append("")
    lines.append("/// <summary>")
    lines.append("/// Card CLR type name -> replacement portrait path, baked in by RegentArt.pck.")
    lines.append("/// </summary>")
    lines.append("internal static class RegentCardArtTable")
    lines.append("{")
    lines.append("\tinternal const int Count = " + str(len(rows)) + ";")
    lines.append("")
    lines.append("\tprivate static readonly Dictionary<string, string> _byCardType = new Dictionary<string, string>")
    lines.append("\t{")
    for card_id, image in rows:
        lines.append('\t\t{ "' + card_id + '", "' + image + '" },')
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
    return len(rows)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", default=os.path.join(REPO, "build", "regent", "extracted"))
    parser.add_argument("--godot", default=None)
    parser.add_argument("--skip-pack", action="store_true")
    args = parser.parse_args()

    source_dir = os.path.abspath(args.source)
    if not os.path.isdir(source_dir):
        raise SystemExit(f"missing extracted directory: {source_dir}")

    count = generate_table(source_dir)
    print(f"table: {count} card(s) -> {TABLE_PATH}")

    if args.skip_pack:
        return 0

    godot = find_godot(args.godot)
    entries = collect_entries(source_dir)
    build_pack(godot, entries, PACK_PATH)
    size = os.path.getsize(PACK_PATH)
    print(f"pack: {size} bytes -> {PACK_PATH}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
