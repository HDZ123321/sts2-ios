"""Ask whether the per-room blocking collection actually buys anything.

LoadRoomAssets calls GC.Collect() after every room finishes preloading. Measured
cost is 8-24ms per room, so a 50-room run pays close to a second of blocking
pauses. Before removing or keeping it, find out whether it moves VRAM at all:
if the number after the collection matches the number before, the pause buys
nothing and the collection can go.

Idempotent.
"""

import io

PATH = r"E:\sts2_port\ci_bundle\src\Core\Assets\PreloadManager.cs"

OLD = """\t\tif (Perf.Enabled)
\t\t{
\t\t\tlong collectStart = Perf.ElapsedMs;
\t\t\tGC.Collect();
\t\t\tLog.Info($"[Perf] room gc after '{roomName}': {Perf.ElapsedMs - collectStart}ms (managed={GC.GetTotalMemory(forceFullCollection: false) / 1048576}MB)", 3);
\t\t}
"""

NEW = """\t\tif (Perf.Enabled)
\t\t{
\t\t\tlong vramBefore = (long)(Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) / 1048576.0);
\t\t\tlong collectStart = Perf.ElapsedMs;
\t\t\tGC.Collect();
\t\t\tlong vramAfter = (long)(Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed) / 1048576.0);
\t\t\tLog.Info($"[Perf] room gc after '{roomName}': {Perf.ElapsedMs - collectStart}ms managed={GC.GetTotalMemory(forceFullCollection: false) / 1048576}MB vram={vramBefore}MB->{vramAfter}MB", 3);
\t\t}
"""

MARKER = "vramBefore"


def main():
    with io.open(PATH, "r", encoding="utf-8", newline="") as handle:
        text = handle.read()

    newline = "\r\n" if "\r\n" in text else "\n"
    body = text.replace("\r\n", "\n")

    if MARKER in body:
        print("already patched")
        return

    if OLD not in body:
        raise SystemExit("MISS: room gc block")

    body = body.replace(OLD, NEW, 1)
    if "using Godot;" not in body:
        body = body.replace("using System.Threading.Tasks;\n", "using System.Threading.Tasks;\nusing Godot;\n", 1)

    with io.open(PATH, "w", encoding="utf-8", newline="") as handle:
        handle.write(body.replace("\n", newline))
    print("patched: room gc vram probe")


if __name__ == "__main__":
    main()