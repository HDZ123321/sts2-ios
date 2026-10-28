"""Insert Perf.Mark() calls into the startup path.

Idempotent: every insertion is guarded by a marker check.
"""
from __future__ import annotations

import pathlib

ROOT = pathlib.Path(r"E:\sts2_port\ci_bundle")


def read(rel: str) -> tuple[pathlib.Path, str]:
    path = ROOT / rel
    return path, path.read_text(encoding="utf-8", newline="")


def write(path: pathlib.Path, text: str) -> None:
    path.write_text(text, encoding="utf-8", newline="")


def patch(rel: str, edits: list[tuple[str, str]]) -> None:
    path, text = read(rel)
    crlf = "\r\n" in text
    original = text
    for old, new in edits:
        variants = [(old, new)]
        if crlf:
            variants.append((old.replace("\n", "\r\n"), new.replace("\n", "\r\n")))
        else:
            variants.append((old.replace("\r\n", "\n"), new.replace("\r\n", "\n")))
        applied = False
        for variant_old, variant_new in variants:
            if variant_new in text:
                print(f"  skip (already applied): {rel}")
                applied = True
                break
            if variant_old in text:
                text = text.replace(variant_old, variant_new, 1)
                applied = True
                break
        if not applied:
            print(f"  MISS: {rel} :: {old.splitlines()[0][:70]}")
    if text != original:
        write(path, text)
        print(f"  patched {rel}")


# ---------------------------------------------------------------- GameStartup
patch(
    "src/Core/Nodes/NGame.cs",
    [
        (
            "\t\tawait OneTimeInitialization.ExecuteVeryEarly();\n"
            "\t\tNode child = NDevConsole.Create();",
            "\t\tawait OneTimeInitialization.ExecuteVeryEarly();\n"
            "\t\tPerf.Mark(\"very early\");\n"
            "\t\tNode child = NDevConsole.Create();",
        ),
        (
            "\t\tTask cloudSavesTask = DoCloudSync();\n"
            "\t\tInitPools();\n"
            "\t\tOneTimeInitialization.ExecuteEssential();",
            "\t\tTask cloudSavesTask = DoCloudSync();\n"
            "\t\tPerf.Mark(\"user data migrations\");\n"
            "\t\tInitPools();\n"
            "\t\tOneTimeInitialization.ExecuteEssential();\n"
            "\t\tPerf.Mark(\"essential init\");",
        ),
        (
            "\t\tif (!IsNodeReady())\n"
            "\t\t{\n"
            "\t\t\tawait ToSignal(this, Node.SignalName.Ready);\n"
            "\t\t}",
            "\t\tif (!IsNodeReady())\n"
            "\t\t{\n"
            "\t\t\tawait ToSignal(this, Node.SignalName.Ready);\n"
            "\t\t}\n"
            "\t\tPerf.Mark(\"node ready\");",
        ),
        (
            "\t\tLeaderboardManager.Initialize();\n"
            "\t\tSteamStatsManager.Initialize();\n"
            "\t\tTimeoutOverlay.Relocalize();",
            "\t\tLeaderboardManager.Initialize();\n"
            "\t\tSteamStatsManager.Initialize();\n"
            "\t\tTimeoutOverlay.Relocalize();\n"
            "\t\tPerf.Mark(\"audio and platform services\");",
        ),
        (
            "\t\tSaveManager.Instance.InitProfileId();\n"
            "\t\tReadSaveResult<SerializableProgress> progressReadResult = SaveManager.Instance.InitProgressData();\n"
            "\t\tReadSaveResult<PrefsSave> prefsReadResult = SaveManager.Instance.InitPrefsData();",
            "\t\tPerf.Mark(\"cloud save sync\");\n"
            "\t\tSaveManager.Instance.InitProfileId();\n"
            "\t\tReadSaveResult<SerializableProgress> progressReadResult = SaveManager.Instance.InitProgressData();\n"
            "\t\tReadSaveResult<PrefsSave> prefsReadResult = SaveManager.Instance.InitPrefsData();\n"
            "\t\tPerf.Mark(\"profile and save files\");",
        ),
        (
            "\t\tawait LoadMainMenu();\n"
            "\t\tLog.Info($\"[Startup] Time to main menu: {Time.GetTicksMsec():N0}ms\");",
            "\t\tawait LoadMainMenu();\n"
            "\t\tPerf.Mark(\"main menu scene\");\n"
            "\t\tLog.Info($\"[Startup] Time to main menu: {Time.GetTicksMsec():N0}ms\");\n"
            "\t\tPerf.StartFrameWatch();",
        ),
        (
            "\t\tOneTimeInitialization.ExecuteDeferred();\n"
            "\t\tawait PreloadManager.LoadCommonAndMainMenuAssets();",
            "\t\tOneTimeInitialization.ExecuteDeferred();\n"
            "\t\tPerf.Mark(\"deferred atlases and prewarm\");\n"
            "\t\tawait PreloadManager.LoadCommonAndMainMenuAssets();\n"
            "\t\tPerf.Mark(\"common and main menu assets\");",
        ),
    ],
)

# ------------------------------------------------------- OneTimeInitialization
patch(
    "src/Core/Helpers/OneTimeInitialization.cs",
    [
        (
            "\t\t_state = State.VeryEarly;\n"
            "\t\tBakedContent.Mount();",
            "\t\t_state = State.VeryEarly;\n"
            "\t\tPerf.Mark(\"process start\");\n"
            "\t\tBakedContent.Mount();",
        ),
        (
            "\t\tawait ModManager.Initialize(new ModManagerFileIo(), SaveManager.Instance.SettingsSave.ModSettings, ReleaseInfoManager.Instance.SemVer);",
            "\t\tPerf.Mark(\"settings load\");\n"
            "\t\tawait ModManager.Initialize(new ModManagerFileIo(), SaveManager.Instance.SettingsSave.ModSettings, ReleaseInfoManager.Instance.SemVer);\n"
            "\t\tPerf.Mark(\"mod manager\");",
        ),
        (
            "\t\t_atlasResourceLoader = new AtlasResourceLoader();\n"
            "\t\tResourceLoader.AddResourceFormatLoader(_atlasResourceLoader, atFront: true);\n"
            "\t\tAtlasManager.LoadEssentialAtlases();\n"
            "\t\tLocManager.Initialize();\n"
            "\t\tBakedContent.InjectLocEntries();\n"
            "\t\tModelDb.Init();\n"
            "\t\tBaseLib.Utils.WatcherRegistry.Populate();\n"
            "\t\tModelIdSerializationCache.Init();\n"
            "\t\tModelDb.InitIds();",
            "\t\t_atlasResourceLoader = new AtlasResourceLoader();\n"
            "\t\tResourceLoader.AddResourceFormatLoader(_atlasResourceLoader, atFront: true);\n"
            "\t\tAtlasManager.LoadEssentialAtlases();\n"
            "\t\tPerf.Mark(\"essential atlases\");\n"
            "\t\tLocManager.Initialize();\n"
            "\t\tBakedContent.InjectLocEntries();\n"
            "\t\tPerf.Mark(\"localization\");\n"
            "\t\tModelDb.Init();\n"
            "\t\tBaseLib.Utils.WatcherRegistry.Populate();\n"
            "\t\tPerf.Mark(\"model db\");\n"
            "\t\tModelIdSerializationCache.Init();\n"
            "\t\tModelDb.InitIds();\n"
            "\t\tPerf.Mark(\"model ids\");",
        ),
        (
            "\t\tMessageTypes.Initialize();\n"
            "\t\tActionTypes.Initialize();\n"
            "\t}",
            "\t\tMessageTypes.Initialize();\n"
            "\t\tActionTypes.Initialize();\n"
            "\t\tPerf.Mark(\"message and action types\");\n"
            "\t}",
        ),
        (
            "\t\tAtlasManager.LoadAllAtlases();\n"
            "\t\tif (!OS.HasFeature(\"editor\"))\n"
            "\t\t{\n"
            "\t\t\tModelDb.Preload();\n"
            "\t\t\tPrewarmJit();\n"
            "\t\t\tConditionalFormatter conditionalFormatter = new ConditionalFormatter();\n"
            "\t\t}",
            "\t\tAtlasManager.LoadAllAtlases();\n"
            "\t\tPerf.Mark(\"all atlases\");\n"
            "\t\tif (!OS.HasFeature(\"editor\"))\n"
            "\t\t{\n"
            "\t\t\tModelDb.Preload();\n"
            "\t\t\tPerf.Mark(\"model db preload\");\n"
            "\t\t\tPrewarmJit();\n"
            "\t\t\tPerf.Mark(\"jit prewarm\");\n"
            "\t\t\tConditionalFormatter conditionalFormatter = new ConditionalFormatter();\n"
            "\t\t}",
        ),
    ],
)

print("done")
