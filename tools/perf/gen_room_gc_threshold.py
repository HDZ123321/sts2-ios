"""Stop paying a full blocking collection on every room when it frees nothing.

Probing the collection showed VRAM is byte-for-byte identical before and after
(1405->1405, 1553->1553, 1856->1856) and managed heap sits flat at 18-23MB for a
whole run, while the collection itself costs 8-26ms. Every room therefore paid a
blocking generation-2 pause for no measurable release.

The collection still has a purpose -- it forces finalizers on Godot objects that
dropped to zero references, which is what returns their native memory -- so it
stays, but it now runs only once the managed heap has actually grown since the
last one. On the measured profile that is roughly never, which is the point.

Threshold is deliberately small: the heap runs at ~20MB, so 8MB of growth since
the last collection is a real bump rather than noise.
"""

import io

PATH = r"E:\sts2_port\ci_bundle\src\Core\Assets\PreloadManager.cs"

OLD = """\t\tif (Perf.Enabled)
\t\t{
\t\t\tlong vramBefore = (long)(Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) / 1048576.0);
\t\t\tlong collectStart = Perf.ElapsedMs;
\t\t\tGC.Collect();
\t\t\tlong vramAfter = (long)(Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) / 1048576.0);
\t\t\tLog.Info($"[Perf] room gc after '{roomName}': {Perf.ElapsedMs - collectStart}ms managed={GC.GetTotalMemory(forceFullCollection: false) / 1048576}MB vram={vramBefore}MB->{vramAfter}MB", 3);
\t\t}
\t\telse
\t\t{
\t\t\tGC.Collect();
\t\t}
"""

NEW = """\t\tCollectIfHeapGrew(roomName);
"""

HELPER_ANCHOR = """\tprivate static AssetLoadingSession LoadAssets(IEnumerable<string> assetPaths, string name)
"""

HELPER = """\t/// <summary>
\t/// How much the managed heap may grow between collections before one is worth its pause.
\t/// A room transition used to force a full blocking collection unconditionally; measuring it
\t/// showed VRAM unchanged afterwards and the heap flat, so the pause bought nothing.
\t/// </summary>
\tprivate const long _gcGrowthThresholdBytes = 8L * 1024L * 1024L;

\tprivate static long _heapAtLastCollect;

\tprivate static void CollectIfHeapGrew(string roomName)
\t{
\t\tlong totalMemory = GC.GetTotalMemory(forceFullCollection: false);
\t\tlong num = totalMemory - _heapAtLastCollect;
\t\tif (num < _gcGrowthThresholdBytes)
\t\t{
\t\t\tif (Perf.Enabled)
\t\t\t{
\t\t\t\tLog.Info($"[Perf] room gc after '{roomName}': skipped, heap grew {num / 1024}KB", 3);
\t\t\t}
\t\t\treturn;
\t\t}
\t\tlong collectStart = Perf.ElapsedMs;
\t\tGC.Collect();
\t\t_heapAtLastCollect = GC.GetTotalMemory(forceFullCollection: false);
\t\tif (Perf.Enabled)
\t\t{
\t\t\tLog.Info($"[Perf] room gc after '{roomName}': {Perf.ElapsedMs - collectStart}ms, heap was {totalMemory / 1048576}MB", 3);
\t\t}
\t}

"""


def main():
    with io.open(PATH, "r", encoding="utf-8", newline="") as handle:
        text = handle.read()

    newline = "\r\n" if "\r\n" in text else "\n"
    body = text.replace("\r\n", "\n")

    if "CollectIfHeapGrew" in body:
        print("already patched")
        return

    if OLD not in body:
        raise SystemExit("MISS: room gc block")
    body = body.replace(OLD, NEW, 1)

    if HELPER_ANCHOR not in body:
        raise SystemExit("MISS: helper anchor")
    body = body.replace(HELPER_ANCHOR, HELPER + HELPER_ANCHOR, 1)

    with io.open(PATH, "w", encoding="utf-8", newline="") as handle:
        handle.write(body.replace("\n", newline))
    print("patched: thresholded room collection")


if __name__ == "__main__":
    main()