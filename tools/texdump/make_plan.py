#!/usr/bin/env python3
"""Regenerate _plan.json for recook.gd: every desktop texture variant that needs a
mobile counterpart.

The v0.113.1 pack ships BPTC (BC7) and S3TC (DXT5/DXT1) GPU variants only, which the
iOS exporter cannot sample. For each one we need the same basename with an .astc /
.etc2 suffix:

    foo.png-<hash>.bptc.ctex  ->  foo.png-<hash>.astc.ctex   (mode: astc)
    foo.png-<hash>.s3tc.ctex  ->  foo.png-<hash>.etc2.ctex   (mode: etc2)

The hash in the name is Godot's import path hash, so it has to be preserved verbatim;
only the format suffix changes.

Workspace layout: this expects <workspace>/pck113/.godot/imported/ next to the repo,
same as recook.gd. Override with --tree / --out.

    python3 tools/texdump/make_plan.py
    python3 tools/texdump/make_plan.py --tree /path/to/pck113 --out /path/to/_plan.json
"""

from __future__ import annotations

import argparse
import json
import os

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
WORKSPACE = os.path.dirname(REPO)

IMPORTED = os.path.join(".godot", "imported")
SUFFIXES = ((".bptc.ctex", ".astc.ctex", "astc"), (".s3tc.ctex", ".etc2.ctex", "etc2"))


def build(tree: str) -> list[dict]:
    src_dir = os.path.join(tree, IMPORTED)
    if not os.path.isdir(src_dir):
        raise SystemExit("make_plan: no such directory: %s" % src_dir)
    items: list[dict] = []
    for name in sorted(os.listdir(src_dir)):
        for old, new, mode in SUFFIXES:
            if name.endswith(old):
                items.append({
                    "src": name,
                    "dst": name[: -len(old)] + new,
                    "mode": mode,
                })
                break
    return items


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--tree", default=os.path.join(WORKSPACE, "pck113"),
                        help="unpacked v0.113.1 tree (the one with .godot/imported)")
    parser.add_argument("--out", default=os.path.join(REPO, "tools", "texdump", "_plan.json"))
    parser.add_argument("--verify-out", default="",
                        help="also write a small sample here for verify.gd (e.g. _verify.json)")
    parser.add_argument("--verify-count", type=int, default=12,
                        help="how many entries the verify sample gets, split across both modes")
    args = parser.parse_args()

    items = build(args.tree)
    astc = sum(1 for i in items if i["mode"] == "astc")
    with open(args.out, "w", encoding="utf-8", newline="\n") as handle:
        json.dump(items, handle, indent=0)
        handle.write("\n")
    print("wrote %s: %d items (astc=%d etc2=%d)" % (args.out, len(items), astc, len(items) - astc))

    if args.verify_out:
        # Take the same number from each mode so both branches of verify.gd get exercised.
        per_mode = max(1, args.verify_count // 2)
        sample: list[dict] = []
        for mode in ("astc", "etc2"):
            sample.extend([i for i in items if i["mode"] == mode][:per_mode])
        with open(args.verify_out, "w", encoding="utf-8", newline="\n") as handle:
            json.dump(sample, handle, indent=0)
            handle.write("\n")
        print("wrote %s: %d sample items" % (args.verify_out, len(sample)))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
