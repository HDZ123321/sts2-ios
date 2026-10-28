"""Measure the cost of loading the three run atlases, and of the per-room GC.

Two questions the current numbers cannot answer:

1. The run atlases (card_atlas is 41MPx across three pages) were moved out of
   the menu preload into LoadRunAssets. If loading them blocks the run start for
   half a second, that trade is bad and has to be revisited.
2. Every room entry calls GC.Collect(), which is a full blocking generation-2
   collection. It is called while the screen is fading, so it may be free, or it
   may be a visible hitch. Measure before touching it.

Idempotent: each edit checks for its own marker first.
"""

import io

PRELOAD = r"E:\sts2_port\ci_bundle\src\Core\Assets\PreloadManager.cs"

RUN_ATLASES_OLD = """\t\t\tAtlasManager.LoadRunAtlases();
\t\t\tList<CharacterModel> list = characters.ToList();
"""
RUN_ATLASES_NEW = """\t\t\tAtlasManager.LoadRunAtlases();
\t\t\tPerf.Mark("run atlases");
\t\t\tList<CharacterModel> list = characters.ToList();
"""

# Time the blocking collection at the end of every room load.
GC_OLD = """\t\tawait (await LoadAssetSets(roomName, AssetSets.CommonAssets, AssetSets.RunSet, AssetSets.Act, hashSet2)).WaitForCompletion();
\t\tGC.Collect();
"""
GC_NEW = """\t\tawait (await LoadAssetSets(roomName, AssetSets.CommonAssets, AssetSets.RunSet, AssetSets.Act, hashSet2)).WaitForCompletion();
\t\tif (Perf.Enabled)
\t\t{
\t\t\tlong collectStart = Perf.ElapsedMs;
\t\t\tGC.Collect();
\t\t\tLog.Info($"[Perf] room gc after '{roomName}': {Perf.ElapsedMs - collectStart}ms (managed={GC.GetTotalMemory(forceFullCollection: false) / 1048576}MB)", 3);
\t\t}
\t\telse
\t\t{
\t\t\tGC.Collect();
\t\t}
"""

NEED_USING = "using MegaCrit.Sts2.Core.Debug;\nusing MegaCrit.Sts2.Core.Logging;\n"


def main():
    with io.open(PRELOAD, "r", encoding="utf-8-sig", newline="") as handle:
        text = handle.read()

    newline = "\r\n" if "\r\n" in text else "\n"
    text = text.replace("\r\n", "\n")

    changed = 0

    if "using MegaCrit.Sts2.Core.Debug;" not in text:
        anchor = "using MegaCrit.Sts2.Core.Entities.Cards;\n"
        if anchor not in text:
            raise SystemExit("MISS: cards using")
        text = text.replace(anchor, NEED_USING + anchor, 1)
        changed += 1
        print("patched: using Debug and Logging")

    if 'Perf.Mark("run atlases")' not in text:
        if RUN_ATLASES_OLD not in text:
            raise SystemExit("MISS: run atlases")
        text = text.replace(RUN_ATLASES_OLD, RUN_ATLASES_NEW, 1)
        changed += 1
        print("patched: run atlases mark")
    else:
        print("already: run atlases mark")

    if "[Perf] room gc after" not in text:
        if GC_OLD not in text:
            raise SystemExit("MISS: room gc")
        text = text.replace(GC_OLD, GC_NEW, 1)
        changed += 1
        print("patched: room gc timing")
    else:
        print("already: room gc timing")

    text = text.replace("\n", newline)
    if changed:
        with io.open(PRELOAD, "w", encoding="utf-8-sig", newline="") as handle:
            handle.write(text)
    print("files changed:", 1 if changed else 0)


if __name__ == "__main__":
    main()
