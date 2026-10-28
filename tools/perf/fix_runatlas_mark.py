"""Measure the actual duration of AtlasManager.LoadRunAtlases().

The first attempt wrapped the call in Perf.Mark("run atlases"), but Mark reports
the time since the *previous* mark, so it folded the whole character-select
phase into the number and read as 1583ms. This replaces it with an explicit
start/end sample so the figure is the atlas load itself.
"""

import io

PATH = r"E:\sts2_port\ci_bundle\src\Core\Assets\PreloadManager.cs"

OLD = """\t\t\tAtlasManager.LoadRunAtlases();
\t\t\tPerf.Mark("run atlases");
"""

NEW = """\t\t\tlong runAtlasStart = Perf.ElapsedMs;
\t\t\tAtlasManager.LoadRunAtlases();
\t\t\tif (Perf.Enabled)
\t\t\t{
\t\t\t\tLog.Info($"[Perf] run atlases: {Perf.ElapsedMs - runAtlasStart}ms", 3);
\t\t\t}
"""

MARKER = "runAtlasStart"


def main():
    with io.open(PATH, "r", encoding="utf-8-sig", newline="") as handle:
        text = handle.read()

    newline = "\r\n" if "\r\n" in text else "\n"
    text = text.replace("\r\n", "\n")

    if MARKER in text:
        print("already patched")
        return

    if OLD not in text:
        raise SystemExit("MISS: run atlases mark")

    text = text.replace(OLD, NEW, 1)
    text = text.replace("\n", newline)
    with io.open(PATH, "w", encoding="utf-8-sig", newline="") as handle:
        handle.write(text)
    print("patched: precise run atlas timing")


if __name__ == "__main__":
    main()