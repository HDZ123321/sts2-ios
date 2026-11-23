"""Ported generator: fix_mobile_preload.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

Put the startup preload back into the shape that was known good on device.

Two things changed between the build that reached combat on the phone (a8b2bb3) and the
build that hard-crashed in the middle of the 'Common' preload (22d6197), and both of them
raise peak memory / off-main-thread work right when the phone is most fragile:

1. _batchSize went from 128 to 512. Loaded resources are retained until the next frame's
   FinalizeLoading, so a 512 wide window can hold four times as many fully decoded scenes
   and textures at once. The A/B run that justified the change measured 482/475/453 ms
   against 501 ms - inside the noise - so the larger window bought nothing.

2. card_atlas / power_atlas / intent_atlas were moved out of the main menu set. Any sprite
   from those atlases that a scene asks for during the preload now triggers a synchronous
   AtlasManager.LoadAtlas from inside ResourceLoader's loader thread, which is new work on
   a thread that used to always find the atlas resident.

The VFX concurrency window (2003 ms -> 501 ms) and the GC thresholding are untouched: they
are measured wins and neither of them runs during the main menu preload.
"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import io
import sys

ROOT = "E:/sts2_port/ci_bundle/"
SESSION = ROOT + "src/Core/Assets/AssetLoadingSession.cs"
ATLAS = ROOT + "src/Core/Assets/AtlasManager.cs"

BATCH_OLD = "\tprivate const int _batchSize = 512;"
BATCH_NEW = """\t/// <summary>
\t/// How many threaded load requests may be in flight at once. A loaded resource is not
\t/// finalised until the next frame, so this number is also the peak count of fully decoded
\t/// scenes and textures the session holds at the same time. 512 was measured on desktop and
\t/// bought nothing (482/475/453 ms against 501 ms for 128, all inside the noise), while on a
\t/// phone it quadruples that peak right when iOS is deciding whether to keep the app alive.
\t/// </summary>
\tprivate const int _batchSize = 128;"""

START_OLD = "\t\tLog.Info($\"Preloading '{name}' assets... count={_toLoad.Count} vfx={_vfxScenes.Count}\");"
START_NEW = """\t\tLog.Info($"Preloading '{name}' assets... count={_toLoad.Count} vfx={_vfxScenes.Count} batch={_batchSize} vfx_window={_vfxConcurrency}");"""

DONE_OLD = "\t\t\tLog.Info($\"Preloading '{_name}' Complete: assets={_totalLoaded} vfx={_vfxLoaded} asset_phase={assetsOnlyMs:N0}ms vfx_phase={_stopwatch.ElapsedMilliseconds - assetsOnlyMs:N0}ms time_elapsed={_stopwatch.ElapsedMilliseconds:N0}ms\");"
DONE_NEW = """\t\t\tlong peakStatic = (long)Performance.GetMonitor(Performance.Monitor.MemoryStatic);
\t\t\tLog.Info($"Preloading '{_name}' Complete: assets={_totalLoaded} vfx={_vfxLoaded} asset_phase={assetsOnlyMs:N0}ms vfx_phase={_stopwatch.ElapsedMilliseconds - assetsOnlyMs:N0}ms time_elapsed={_stopwatch.ElapsedMilliseconds:N0}ms static_mem={peakStatic / 1048576L}MB");"""

LOADALL_OLD = """\tpublic static void LoadAllAtlases()
\t{
\t\tstring[] menuAtlases = _menuAtlases;
\t\tforeach (string atlasName in menuAtlases)
\t\t{
\t\t\tLoadAtlas(atlasName);
\t\t}
\t}"""
LOADALL_NEW = """\tpublic static void LoadAllAtlases()
\t{
\t\tstring[] menuAtlases = _menuAtlases;
\t\tforeach (string atlasName in menuAtlases)
\t\t{
\t\t\tLoadAtlas(atlasName);
\t\t}
\t\tstring[] runAtlases = _runAtlases;
\t\tforeach (string atlasName2 in runAtlases)
\t\t{
\t\t\tLoadAtlas(atlasName2);
\t\t}
\t}"""

RUN_DOC_OLD = """\t/// <summary>
\t/// Atlases that only a run needs. <c>card_atlas</c> alone is three pages of 1920x1080-ish art
\t/// (41 megapixels, roughly 160 MB of VRAM once decompressed), so loading it eagerly spends that
\t/// memory on the main menu where no card is ever drawn. They are loaded when a run starts and
\t/// fall back to the lazy path in <see cref="T:MegaCrit.Sts2.Core.Assets.AtlasResourceLoader" /> if
\t/// something asks for a sprite before then.
\t/// </summary>"""
RUN_DOC_NEW = """\t/// <summary>
\t/// Atlases that only a run draws from. They used to be deferred until a run started so the main
\t/// menu would not pay their VRAM, but deferring them means any scene that asks for one of their
\t/// sprites during the startup preload triggers a synchronous AtlasManager.LoadAtlas from inside
\t/// ResourceLoader's loader thread - new work on a thread that previously always found the atlas
\t/// resident. That is the shape the phone crashed in, so they are resident from the first frame
\t/// again and <see cref="M:MegaCrit.Sts2.Core.Assets.AtlasManager.LoadRunAtlases" /> is now only a
\t/// cheap early-out.
\t/// </summary>"""


def patch(path, edits, newline="\r\n"):
    with io.open(path, "r", encoding="utf-8", newline="") as handle:
        text = handle.read()
    style = "\r\n" if "\r\n" in text else "\n"
    changed = 0
    for marker, old, new in edits:
        if marker in text:
            continue
        candidate = old.replace("\n", style) if style != "\n" else old
        replacement = new.replace("\n", style) if style != "\n" else new
        if candidate not in text:
            print("MISS in %s: %r" % (path, old.splitlines()[0]))
            continue
        text = text.replace(candidate, replacement, 1)
        changed += 1
    with io.open(path, "w", encoding="utf-8", newline="") as handle:
        handle.write(text)
    print("%s: %d edit(s)" % (path.rsplit("/", 1)[-1], changed))


patch(SESSION, [
    ("batch={_batchSize} vfx_window", BATCH_OLD, BATCH_NEW),
    ("vfx_window={_vfxConcurrency}", START_OLD, START_NEW),
    ("static_mem=", DONE_OLD, DONE_NEW),
])

patch(ATLAS, [
    ("foreach (string atlasName2 in runAtlases)", LOADALL_OLD, LOADALL_NEW),
    ("only a\n", RUN_DOC_OLD, RUN_DOC_NEW),
])

print("done")
