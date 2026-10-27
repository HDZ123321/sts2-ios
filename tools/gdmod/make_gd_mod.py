#!/usr/bin/env python3
"""Build a StS2 scripting mod folder from a source directory.

A mod is loaded from <mods>/<id>/<id>.json plus <mods>/<id>/<id>.pck. The pack is mounted with
ProjectSettings::load_resource_pack, which is additive: whatever paths the pack carries become
visible under res://, and replace_files=true means they shadow the game's own files at the same
path. The entry script therefore has to live at res://<id>/main.gd (or main.gdc).

The pack is built by driving Godot's own PCKPacker through a throwaway headless project. That is
deliberate: Godot's `--export-pack` also writes res://project.binary and res://.godot/* into the
archive, and mounting such a pack makes the engine reload the UID cache from the pack, shadowing
the game's own uid_cache.bin and breaking every uid:// reference in the base game. PCKPacker only
packs the entries we hand it.

GDScript is stored as readable source, not bytecode. The engine tokenises and compiles source at
load time, so a mod does not need a Godot editor to be authored, and a modder can ship a plain
.gd file. Pass --compile to have Godot export .gdc bytecode instead.

Usage:
    python make_gd_mod.py --source tools/gdmod --id example_gd --out build/mods \
        --godot "E:/sts2_port/godot/Godot_v4.5.1-stable_mono_win64/Godot_v4.5.1-stable_mono_win64_console.exe"

Output:
    build/mods/example_gd/example_gd.json
    build/mods/example_gd/example_gd.pck

Copy the whole example_gd folder into the game's mods directory. On iOS that is Documents/mods/,
reachable from the Files app because the app declares UIFileSharingEnabled.
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
import tempfile

BUILDER_SCRIPT = '''extends SceneTree

func _init() -> void:
	var job_path := OS.get_environment("GDMOD_JOB")
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
	print("GDMOD_PACKED ", count, " ", out_path)
	quit(0)
'''


def log(message: str) -> None:
    print(f"[make_gd_mod] {message}", flush=True)


def find_godot(explicit: str | None) -> str:
    if explicit:
        if not os.path.isfile(explicit):
            raise SystemExit(f"godot binary not found: {explicit}")
        return explicit
    for env in ("GODOT", "GODOT_BIN"):
        value = os.environ.get(env)
        if value and os.path.isfile(value):
            return value
    for name in (
        "Godot_v4.5.1-stable_mono_win64_console.exe",
        "Godot_v4.5.1-stable_mono_win64.exe",
        "Godot_v4.5.1-stable_mono_linux.x86_64",
        "godot",
    ):
        found = shutil.which(name)
        if found:
            return found
    raise SystemExit("could not find a Godot binary; pass --godot <path>")


def collect_entries(source_dir: str, mod_id: str, compile_bytecode: bool) -> list[dict[str, str]]:
    """Maps every file under source_dir to a res://<id>/... target inside the pack."""
    entries: list[dict[str, str]] = []
    for root, _dirs, files in os.walk(source_dir):
        relative_dir = os.path.relpath(root, source_dir)
        for name in files:
            if name in ("make_gd_mod.py", "README.md", "__pycache__"):
                continue
            if name.endswith((".uid", ".import")):
                continue
            source_path = os.path.join(root, name)
            relative = name if relative_dir == "." else os.path.join(relative_dir, name)
            relative = relative.replace(os.sep, "/")
            if compile_bytecode and relative.endswith(".gd"):
                relative = relative[:-3] + ".gdc"
            entries.append({"target": f"res://{mod_id}/{relative}", "source": source_path})
    if not entries:
        raise SystemExit(f"nothing to pack under {source_dir}")
    return entries


def compile_sources(godot: str, source_dir: str, mod_id: str, work_dir: str) -> str:
    """Exports the sources as .gdc bytecode through a throwaway project, returns the pack path."""
    project_dir = os.path.join(work_dir, "compile_project")
    target_dir = os.path.join(project_dir, mod_id)
    os.makedirs(target_dir, exist_ok=True)
    for root, _dirs, files in os.walk(source_dir):
        relative_dir = os.path.relpath(root, source_dir)
        destination = target_dir if relative_dir == "." else os.path.join(target_dir, relative_dir)
        os.makedirs(destination, exist_ok=True)
        for name in files:
            if name in ("make_gd_mod.py", "README.md"):
                continue
            shutil.copy2(os.path.join(root, name), os.path.join(destination, name))

    with open(os.path.join(project_dir, "project.godot"), "w", encoding="utf-8") as handle:
        handle.write('config_version=5\n\n[application]\n\nconfig/name="pckbuild"\n')
    with open(os.path.join(project_dir, "export_presets.cfg"), "w", encoding="utf-8") as handle:
        handle.write(
            '[preset.0]\n\n'
            'name="pckbuild"\n'
            'platform="Linux/X11"\n'
            "runnable=true\n"
            "advanced_options=false\n"
            "dedicated_server=false\n"
            "custom_features=\"\"\n"
            "export_filter=\"all_resources\"\n"
            "include_filter=\"\"\n"
            "exclude_filter=\"\"\n"
            "export_path=\"\"\n"
            "script_export_mode=2\n"
        )

    scratch = os.path.join(work_dir, "compiled.pck")
    command = [godot, "--headless", "--path", project_dir, "--export-pack", "pckbuild", scratch]
    result = subprocess.run(
        command,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
        timeout=600,
    )
    if result.returncode != 0 or not os.path.isfile(scratch):
        log(result.stdout[-4000:])
        log(result.stderr[-4000:])
        raise SystemExit(f"godot export failed (exit {result.returncode})")
    return scratch


def build_pack(godot: str, entries: list[dict[str, str]], out_path: str, work_dir: str) -> None:
    project_dir = os.path.join(work_dir, "packer")
    os.makedirs(project_dir, exist_ok=True)
    with open(os.path.join(project_dir, "project.godot"), "w", encoding="utf-8") as handle:
        handle.write('config_version=5\n\n[application]\n\nconfig/name="gdmod_packer"\n')
    with open(os.path.join(project_dir, "builder.gd"), "w", encoding="utf-8") as handle:
        handle.write(BUILDER_SCRIPT)

    job_path = os.path.join(work_dir, "job.json")
    with open(job_path, "w", encoding="utf-8") as handle:
        json.dump({"out": out_path, "entries": entries}, handle, indent=2)

    environment = dict(os.environ)
    environment["GDMOD_JOB"] = job_path
    command = [godot, "--headless", "--path", project_dir, "--script", "builder.gd"]
    result = subprocess.run(
        command,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
        timeout=600,
        env=environment,
    )
    if result.returncode != 0 or "GDMOD_PACKED" not in result.stdout:
        log(result.stdout[-4000:])
        log(result.stderr[-4000:])
        raise SystemExit(f"pack build failed (exit {result.returncode})")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--source", required=True, help="directory holding main.gd and any resources")
    parser.add_argument("--id", required=True, help="mod id; becomes the folder, pack and manifest name")
    parser.add_argument("--name", default=None, help="display name (defaults to the id)")
    parser.add_argument("--author", default="unknown")
    parser.add_argument("--description", default="")
    parser.add_argument("--version", default="1.0.0")
    parser.add_argument("--entry", default=None, help="entry script inside the pack (default res://<id>/main.gd)")
    parser.add_argument("--out", required=True, help="output root; the mod folder is created inside")
    parser.add_argument("--godot", default=None, help="path to the Godot binary")
    parser.add_argument("--compile", action="store_true", help="ship .gdc bytecode instead of .gd source")
    args = parser.parse_args()

    args.name = args.name or args.id
    source_dir = os.path.abspath(args.source)
    if not os.path.isdir(source_dir):
        raise SystemExit(f"source is not a directory: {source_dir}")

    mod_dir = os.path.join(os.path.abspath(args.out), args.id)
    os.makedirs(mod_dir, exist_ok=True)
    pck_path = os.path.join(mod_dir, f"{args.id}.pck")

    godot = find_godot(args.godot)
    log(f"using godot: {godot}")

    with tempfile.TemporaryDirectory(prefix="gdmod_") as work_dir:
        if args.compile:
            compiled_pack = compile_sources(godot, source_dir, args.id, work_dir)
            shutil.copy2(compiled_pack, pck_path)
            log("packed compiled bytecode via --export-pack")
        else:
            entries = collect_entries(source_dir, args.id, compile_bytecode=False)
            log(f"packing {len(entries)} file(s) as source")
            build_pack(godot, entries, pck_path, work_dir)

    if not os.path.isfile(pck_path):
        raise SystemExit("pack build reported success but produced no pack")
    log(f"wrote {pck_path} ({os.path.getsize(pck_path)} bytes)")

    entry = args.entry
    if entry is None:
        entry = f"res://{args.id}/main.gdc" if args.compile else f"res://{args.id}/main.gd"

    manifest = {
        "id": args.id,
        "name": args.name,
        "author": args.author,
        "description": args.description,
        "version": args.version,
        "has_pck": True,
        "has_dll": False,
        "affects_gameplay": True,
        "gd_entry": entry,
    }
    manifest_path = os.path.join(mod_dir, f"{args.id}.json")
    with open(manifest_path, "w", encoding="utf-8") as handle:
        json.dump(manifest, handle, indent=2)
        handle.write("\n")
    log(f"wrote {manifest_path}")

    log(f"done; copy {mod_dir} into the game's mods directory")
    return 0


if __name__ == "__main__":
    sys.exit(main())
