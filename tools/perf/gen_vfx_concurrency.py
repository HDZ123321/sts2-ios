"""Load VFX scenes with bounded concurrency instead of strictly one at a time.

Measured baseline (local Windows, `Preloading 'Common'`): 92 VFX scenes cost
1,632ms of a 2,003ms preload while the 691 ordinary assets only cost 371ms.
The VFX queue was serialised on the theory that Godot's threaded loader races
on ext_resources shared between VFX scenes ("Another resource is loaded from
path"). Each scene therefore costs at least a full round trip of frames, and
that dominates the preload.

This patch keeps the threaded request path (useSubThreads: false, so sub
resources are still loaded on Godot's own loader thread) but keeps a small
window of scenes in flight. Failures are logged instead of silently dropping
the scene, and the window is deliberately modest so a race, if it happens at
all, surfaces in the log rather than as a missing effect.

Idempotent: checks for its own marker first.
"""

import io

PATH = r"E:\sts2_port\ci_bundle\src\Core\Assets\AssetLoadingSession.cs"

MARKER = "_vfxInFlight"

# 1. Replace the single-flight state with a window.
STATE_OLD = """\tprivate bool _vfxLoading;

\tprivate string? _currentVfxPath;
"""
STATE_NEW = """\t/// <summary>
\t/// How many VFX scenes may be in flight at once. They used to be loaded strictly one
\t/// at a time because concurrent loading was blamed for "Another resource is loaded from
\t/// path" errors on shared ext_resources, but that serialised 92 scenes into a tail that
\t/// cost four times more than every other asset in the same preload combined.
\t/// </summary>
\tprivate const int _vfxConcurrency = 6;

\tprivate readonly Queue<string> _vfxInFlight = new Queue<string>();
"""

# 2. Replace the vfx pump.
PUMP_START = "\tprivate void ProcessVfxQueue()\n\t{\n"
PUMP_END_MARK = "\tprivate void FinalizeLoading()\n"

PUMP_NEW = """\tprivate void ProcessVfxQueue()
\t{
\t\tint count = _vfxInFlight.Count;
\t\tfor (int i = 0; i < count; i++)
\t\t{
\t\t\tif (!_vfxInFlight.TryDequeue(out string current))
\t\t\t{
\t\t\t\tLog.Error("Failed to dequeue in-flight VFX scene!");
\t\t\t\tbreak;
\t\t\t}
\t\t\tswitch (ResourceLoader.LoadThreadedGetStatus(current))
\t\t\t{
\t\t\tcase ResourceLoader.ThreadLoadStatus.Loaded:
\t\t\t\tAddToCache(ResourceLoader.LoadThreadedGet(current), current);
\t\t\t\t_vfxLoaded++;
\t\t\t\tbreak;
\t\t\tcase ResourceLoader.ThreadLoadStatus.InProgress:
\t\t\t\t_vfxInFlight.Enqueue(current);
\t\t\t\tbreak;
\t\t\tcase ResourceLoader.ThreadLoadStatus.InvalidResource:
\t\t\tcase ResourceLoader.ThreadLoadStatus.Failed:
\t\t\t\tLog.Error("Failed to load VFX scene: " + current);
\t\t\t\tbreak;
\t\t\tdefault:
\t\t\t\tLog.Error("Failed to load VFX scene: " + current);
\t\t\t\tbreak;
\t\t\t}
\t\t}
\t\tstring result;
\t\twhile (_vfxInFlight.Count < _vfxConcurrency && _vfxScenes.TryDequeue(out result))
\t\t{
\t\t\tif (_cache.ContainsKey(result))
\t\t\t{
\t\t\t\tcontinue;
\t\t\t}
\t\t\tif (ResourceLoader.LoadThreadedRequest(result, "", useSubThreads: false, ResourceLoader.CacheMode.Reuse) == Error.Ok)
\t\t\t{
\t\t\t\t_vfxInFlight.Enqueue(result);
\t\t\t}
\t\t\telse
\t\t\t{
\t\t\t\tLog.Error("Error requesting VFX load for path: " + result);
\t\t\t}
\t\t}
\t}

"""

# 3. Completion test references the old single-flight flag.
FLAG_OLD = "&& _vfxScenes.Count == 0 && !_vfxLoading;"
FLAG_NEW = "&& _vfxScenes.Count == 0 && _vfxInFlight.Count == 0;"


def main():
    with io.open(PATH, "r", encoding="utf-8-sig", newline="") as handle:
        text = handle.read()

    newline = "\r\n" if "\r\n" in text else "\n"
    text = text.replace("\r\n", "\n")

    if MARKER in text:
        print("already patched")
        return

    # 1. state
    if STATE_OLD not in text:
        raise SystemExit("MISS: state block")
    text = text.replace(STATE_OLD, STATE_NEW, 1)

    # 2. pump: replace the whole method body.
    start = text.find(PUMP_START)
    if start < 0:
        raise SystemExit("MISS: ProcessVfxQueue")
    end = text.find(PUMP_END_MARK, start)
    if end < 0:
        raise SystemExit("MISS: ProcessVfxQueue end")
    text = text[:start] + PUMP_NEW + text[end:]

    # 3. completion flag
    if FLAG_OLD not in text:
        raise SystemExit("MISS: completion flag")
    text = text.replace(FLAG_OLD, FLAG_NEW, 1)

    text = text.replace("\n", newline)
    with io.open(PATH, "w", encoding="utf-8-sig", newline="") as handle:
        handle.write(text)
    print("patched: vfx concurrency window")


if __name__ == "__main__":
    main()
