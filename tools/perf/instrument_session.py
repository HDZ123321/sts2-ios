"""Instrument AssetLoadingSession so we can tell how much of a preload is the
parallel asset phase and how much is the serial VFX tail.

The VFX queue loads one scene at a time, one status check per frame, so it is
plausible that most of the multi-second "Common" preload is spent there. Before
changing that behaviour we measure it.

Idempotent: every edit checks for its own marker first.
"""

import io
import os

PATH = r"E:\sts2_port\ci_bundle\src\Core\Assets\AssetLoadingSession.cs"

# Fields that track the two phases.
FIELDS_OLD = """\tprivate int _framesSinceStatusLog;
"""
FIELDS_NEW = """\tprivate int _framesSinceStatusLog;

\t/// <summary>
\t/// Timestamps (in stopwatch milliseconds) marking when the parallel asset phase
\t/// drained, so the completion log can split the total into "assets" and "vfx".
\t/// </summary>
\tprivate long _assetsDrainedAtMs = -1L;

\tprivate int _vfxLoaded;
"""

# Record the moment the parallel phase drains.
DRAIN_OLD = """\t\t\tProcessVfxQueue();
\t\t}
"""
DRAIN_NEW = """\t\t\tif (_assetsDrainedAtMs < 0L)
\t\t\t{
\t\t\t\t_assetsDrainedAtMs = _stopwatch.ElapsedMilliseconds;
\t\t\t}
\t\t\tProcessVfxQueue();
\t\t}
"""

# Count the vfx scenes as they land.
VFX_OK_OLD = """\t\t\tcase ResourceLoader.ThreadLoadStatus.Loaded:
\t\t\t\tAddToCache(ResourceLoader.LoadThreadedGet(_currentVfxPath), _currentVfxPath);
\t\t\t\t_vfxLoading = false;
\t\t\t\tbreak;
"""
VFX_OK_NEW = """\t\t\tcase ResourceLoader.ThreadLoadStatus.Loaded:
\t\t\t\tAddToCache(ResourceLoader.LoadThreadedGet(_currentVfxPath), _currentVfxPath);
\t\t\t\t_vfxLoaded++;
\t\t\t\t_vfxLoading = false;
\t\t\t\tbreak;
"""

# Split the completion log.
DONE_OLD = """\t\t\tLog.Info($"Preloading '{_name}' Complete: assets={_totalLoaded} time_elapsed={_stopwatch.ElapsedMilliseconds:N0}ms");
"""
DONE_NEW = """\t\t\tlong assetsOnlyMs = ((_assetsDrainedAtMs >= 0L) ? _assetsDrainedAtMs : _stopwatch.ElapsedMilliseconds);
\t\t\tLog.Info($"Preloading '{_name}' Complete: assets={_totalLoaded} vfx={_vfxLoaded} asset_phase={assetsOnlyMs:N0}ms vfx_phase={_stopwatch.ElapsedMilliseconds - assetsOnlyMs:N0}ms time_elapsed={_stopwatch.ElapsedMilliseconds:N0}ms");
"""


def patch(text, old, new, marker):
    if marker in text:
        return text, False
    if old not in text:
        raise SystemExit("MISS: " + marker)
    return text.replace(old, new, 1), True


def main():
    with io.open(PATH, "r", encoding="utf-8-sig", newline="") as handle:
        text = handle.read()

    newline = "\r\n" if "\r\n" in text else "\n"
    text = text.replace("\r\n", "\n")

    changed = 0
    for old, new, marker in (
        (FIELDS_OLD, FIELDS_NEW, "_assetsDrainedAtMs = -1L"),
        (DRAIN_OLD, DRAIN_NEW, "_assetsDrainedAtMs = _stopwatch.ElapsedMilliseconds"),
        (VFX_OK_OLD, VFX_OK_NEW, "_vfxLoaded++"),
        (DONE_OLD, DONE_NEW, "vfx_phase="),
    ):
        text, did = patch(text, old, new, marker)
        if did:
            changed += 1
            print("patched:", marker)
        else:
            print("already:", marker)

    text = text.replace("\n", newline)

    if changed:
        with io.open(PATH, "w", encoding="utf-8-sig", newline="") as handle:
            handle.write(text)
    print("files changed:", 1 if changed else 0)


if __name__ == "__main__":
    main()
