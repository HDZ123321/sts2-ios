#!/usr/bin/env python3
"""用 swiftc 手工组装灵动岛的 WidgetKit 扩展，并塞进已经打包好的 .app。

为什么不加 Xcode target：
  Godot 4.5.1 的 iOS 模板里连 Swift 都没开，加一个真正的 extension target 意味着手写
  PBXNativeTarget / PBXTargetDependency / PBXCopyFilesBuildPhase(Embed App Extensions) /
  "Validate Embedded Binary" 一整套 pbxproj 结构，还要接管 archive 的签名链路。手工组装
  一个 .appex 目录结构只需要 swiftc + 一个 Info.plist，出错最多是没有灵动岛，不会伤到
  主 app 的构建。

为什么是 `-Xlinker -e -Xlinker _NSExtensionMain`：
  扩展不是普通 App，入口是 libsystem 里的 NSExtensionMain，它读 Info.plist 的 NSExtension
  字典再回调 WidgetKit。不指定这个入口，进程会去找 main()（Swift 的 @main 生成的 main 只
  是给 App 用的），扩展被系统启动时直接崩。

为什么是 `-Xlinker -application_extension`：
  它给 Mach-O 打上 MH_APP_EXTENSION 标记。缺了这个标记，dyld 在扩展进程里会拒绝加载
  需要 app 能力的库，系统也会以 "not a valid extension" 拒掉这个 bundle。

Info.plist 里的 $(...) 占位符由本脚本从主 app 的 Info.plist 回填，所以扩展的
CFBundleShortVersionString / CFBundleVersion 永远跟着主 app 走，不会漂。

用法（在仓库根目录，macOS + Xcode CLT 环境）:
    python3 tools/build_island_extension.py --app build/sts2.xcarchive/Products/Applications/sts2.app
可选:
    --min-os 16.1        扩展的最低系统（ActivityKit 下限）
    --bundle-id <id>     默认 "<app bundle id>.island"
    --keep-build-dir     保留 build/island 中间产物
"""
import argparse
import os
import plistlib
import shutil
import subprocess
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
ISLAND_SRC = REPO / "sts2" / "island"
SOURCES = [
    ISLAND_SRC / "Sts2RunActivityAttributes.swift",
    ISLAND_SRC / "Sts2IslandWidget.swift",
]
PLIST_TEMPLATE = ISLAND_SRC / "Sts2Island-Info.plist"
EXECUTABLE_NAME = "Sts2Island"
APPEX_NAME = "Sts2Island.appex"
DEFAULT_MIN_OS = "16.1"


def log(line):
    print("[island] " + line, flush=True)


def die(line):
    sys.stderr.write("[island] error: " + line + "\n")
    sys.exit(1)


def run(cmd, **kwargs):
    log("$ " + " ".join(str(c) for c in cmd))
    result = subprocess.run([str(c) for c in cmd], capture_output=True, text=True, **kwargs)
    if result.returncode != 0:
        sys.stderr.write(result.stdout)
        sys.stderr.write(result.stderr)
        die("command failed (exit %d): %s" % (result.returncode, cmd[0]))
    return result.stdout


def read_plist(path):
    with open(path, "rb") as fh:
        return plistlib.load(fh)


def write_plist(data, path):
    with open(path, "wb") as fh:
        plistlib.dump(data, fh, sort_keys=False)


def sdk_path():
    return run(["xcrun", "--sdk", "iphoneos", "--show-sdk-path"]).strip()


def read_app_identity(app, override_bundle_id):
    info_path = app / "Info.plist"
    if not info_path.is_file():
        die("no Info.plist in %s" % app)
    info = read_plist(info_path)
    bundle_id = override_bundle_id or info.get("CFBundleIdentifier", "")
    if not bundle_id:
        die("the app Info.plist has no CFBundleIdentifier")
    short_version = info.get("CFBundleShortVersionString", "0.0")
    build_version = info.get("CFBundleVersion", "1")
    # 扩展的 CFBundleVersion 必须是递进的整数串，主 app 偶尔会写成非数字，兜一下。
    if not str(build_version).isdigit():
        build_version = "1"
    return bundle_id, short_version, str(build_version)


def build_extension(app, bundle_id, short_version, build_version, min_os, keep):
    for source in SOURCES:
        if not source.is_file():
            die("missing source: %s" % source)
    if not PLIST_TEMPLATE.is_file():
        die("missing plist template: %s" % PLIST_TEMPLATE)

    stage = REPO / "build" / "island"
    binary = stage / EXECUTABLE_NAME
    if stage.exists():
        shutil.rmtree(stage)
    stage.mkdir(parents=True, exist_ok=True)

    sdk = sdk_path()
    cmd = [
        "xcrun", "--sdk", "iphoneos", "swiftc",
        "-target", "arm64-apple-ios" + min_os,
        "-sdk", sdk,
        "-O", "-wmo",
        "-parse-as-library",
        "-module-name", EXECUTABLE_NAME,
        "-Xlinker", "-e", "-Xlinker", "_NSExtensionMain",
        "-Xlinker", "-application_extension",
        "-framework", "SwiftUI",
        "-framework", "WidgetKit",
        "-framework", "ActivityKit",
        "-framework", "UIKit",
        "-emit-executable",
        "-o", binary,
    ] + [str(s) for s in SOURCES]
    run(cmd)

    if not binary.is_file() or binary.stat().st_size == 0:
        die("swiftc produced no executable")

    # ld on the macos-14 runner ignores the -application_extension flag we pass through
    # -Xlinker (the linked image came back without MH_APP_EXTENSION), so stamp the bit
    # ourselves instead of losing a whole CI cycle per attempt.
    set_app_extension_flag(binary)

    # Info.plist：模板里的 $(ISLAND_*) 由主 app 的名义回填
    plist = read_plist(PLIST_TEMPLATE)
    substitutions = {
        "$(ISLAND_BUNDLE_ID)": bundle_id,
        "$(ISLAND_VERSION)": short_version,
        "$(ISLAND_MIN_OS)": min_os,
    }

    def fill(value):
        if isinstance(value, str):
            out = value
            for key, replacement in substitutions.items():
                out = out.replace(key, replacement)
            return out
        if isinstance(value, dict):
            return {k: fill(v) for k, v in value.items()}
        if isinstance(value, list):
            return [fill(v) for v in value]
        return value

    plist = fill(plist)
    plist["CFBundleIdentifier"] = bundle_id
    plist["CFBundleShortVersionString"] = short_version
    plist["CFBundleVersion"] = build_version
    plist["MinimumOSVersion"] = min_os
    plist["CFBundleExecutable"] = EXECUTABLE_NAME
    write_plist(plist, stage / "Info.plist")

    appex = stage / APPEX_NAME
    appex.mkdir(exist_ok=True)
    shutil.move(str(binary), str(appex / EXECUTABLE_NAME))
    shutil.move(str(stage / "Info.plist"), str(appex / "Info.plist"))
    os.chmod(appex / EXECUTABLE_NAME, 0o755)

    verify_binary(appex / EXECUTABLE_NAME, min_os)
    verify_plist(appex / "Info.plist", bundle_id, min_os)

    plugins = app / "PlugIns"
    plugins.mkdir(exist_ok=True)
    target = plugins / APPEX_NAME
    if target.exists():
        shutil.rmtree(target)
    shutil.copytree(appex, target)
    log("installed %s (%d bytes)" % (target, dir_size(target)))
    if not keep:
        shutil.rmtree(stage, ignore_errors=True)
    return target


def verify_binary(binary, min_os):
    arch = run(["lipo", "-archs", binary]).strip()
    if arch != "arm64":
        die("extension is not a single arm64 slice: %s" % arch)
    if not _has_app_extension_flag(binary):
        die("Mach-O is still missing MH_APP_EXTENSION after the header stamp")
    load_commands = run(["otool", "-l", binary])
    if "LC_MAIN" not in load_commands:
        die("extension has no LC_MAIN entry point (NSExtensionMain was not linked)")
    build = run(["vtool", "-show-build", binary])
    if "platform IOS" not in build:
        die("extension is not built for the iOS platform:\n" + build)
    if "minos " + min_os not in build:
        log("warning: expected minos %s, got:\n%s" % (min_os, build.strip()))


def set_app_extension_flag(binary):
    """OR MH_APP_EXTENSION (0x00800000) into the Mach-O header flags in place.

    ld is supposed to do this for -application_extension, but the Xcode 15.4 toolchain on
    the macos-14 runner drops the flag, and an extension image without it makes dyld skip
    the extension-safe library checks. The appex is unsigned at this point, so rewriting
    the header costs nothing.
    """
    path = Path(binary)
    data = bytearray(path.read_bytes())
    if len(data) < 32:
        die("Mach-O is too short to carry a header")
    magic = int.from_bytes(data[:4], "little")
    if magic not in (0xFEEDFACF, 0xFEEDFACE):
        die("not a Mach-O image: magic 0x%X" % magic)
    flags = int.from_bytes(data[24:28], "little")
    if flags & 0x800000:
        return False
    data[24:28] = (flags | 0x800000).to_bytes(4, "little")
    path.write_bytes(bytes(data))
    log("stamped MH_APP_EXTENSION into the header (ld did not)")
    return True


def _has_app_extension_flag(binary):
    data = Path(binary).read_bytes()
    if len(data) < 32:
        return False
    magic = int.from_bytes(data[:4], "little")
    if magic not in (0xFEEDFACF, 0xFEEDFACE):
        return False
    flags = int.from_bytes(data[24:28], "little")
    return bool(flags & 0x800000)


def verify_plist(plist_path, bundle_id, min_os):
    info = read_plist(plist_path)
    required = {
        "CFBundleExecutable": EXECUTABLE_NAME,
        "CFBundleIdentifier": bundle_id,
        "MinimumOSVersion": min_os,
    }
    for key, want in required.items():
        got = info.get(key)
        if got != want:
            die("Info.plist %s = %r, want %r" % (key, got, want))
    point = (info.get("NSExtension") or {}).get("NSExtensionPointIdentifier")
    if point != "com.apple.widgetkit-extension":
        die("Info.plist NSExtensionPointIdentifier = %r" % point)


def dir_size(path):
    total = 0
    for root, _dirs, files in os.walk(path):
        for name in files:
            total += os.path.getsize(os.path.join(root, name))
    return total


def main():
    parser = argparse.ArgumentParser(description="build the Dynamic Island widget extension")
    parser.add_argument("--app", required=True, help="path to the built sts2.app")
    parser.add_argument("--min-os", default=DEFAULT_MIN_OS, help="extension deployment target")
    parser.add_argument("--bundle-id", default=None, help="default: <app bundle id>.island")
    parser.add_argument("--keep-build-dir", action="store_true")
    args = parser.parse_args()

    app = Path(args.app)
    if not app.is_dir():
        die("app bundle not found: %s" % app)

    app_bundle_id, short_version, build_version = read_app_identity(app, None)
    extension_bundle_id = args.bundle_id or (app_bundle_id + ".island")
    log("app=%s version=%s (%s)" % (app_bundle_id, short_version, build_version))
    log("extension=%s min-os=%s" % (extension_bundle_id, args.min_os))

    target = build_extension(app, extension_bundle_id, short_version, build_version,
                             args.min_os, args.keep_build_dir)
    log("done: %s" % target)
    return 0


if __name__ == "__main__":
    sys.exit(main())
