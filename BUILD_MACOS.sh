#!/bin/bash
# ============================================================================
# Slay the Spire 2 -> iOS 构建脚本
# 在 macOS 上运行。前置：Xcode 15+、.NET 9 SDK、Godot 4.5.1 mono
# ============================================================================
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ="${PROJ:-$HERE/../project}"
OUT="${OUT:-$HERE}"
GODOT="${GODOT:-/Applications/Godot_mono.app/Contents/MacOS/Godot}"
TEAM_ID="${TEAM_ID:-}"
BUNDLE_ID="${BUNDLE_ID:-com.yourname.sts2}"
CONFIG="${CONFIG:-Release}"

echo "== 0. 环境检查 =="
[ -n "$TEAM_ID" ] || { echo "请设置 TEAM_ID（Apple Developer Team ID，10 位，形如 ABCDE12XYZ）"; exit 1; }
command -v dotnet >/dev/null || { echo "缺少 dotnet"; exit 1; }
command -v xcodebuild >/dev/null || { echo "缺少 Xcode"; exit 1; }
[ -f "$PROJ/sts2.csproj" ] || { echo "找不到 $PROJ/sts2.csproj"; exit 1; }

echo "== 1. .NET NativeAOT 编译（ios-arm64）=="
# Godot 4.5 的 iOS 导出走 NativeAOT：ILC 把 sts2 编成静态库，包进 sts2_aot.xcframework
cd "$PROJ"
dotnet publish sts2.csproj \
    -c "$CONFIG" \
    -r ios-arm64 \
    -p:GodotTargetPlatform=ios \
    -p:PublishAot=true \
    -p:StripSymbols=false \
    -o "$PROJ/.godot/mono/temp/bin/$CONFIG-ios-arm64"

PUBLISH_DIR="$PROJ/.godot/mono/temp/bin/$CONFIG-ios-arm64"
DYLIB="$PUBLISH_DIR/sts2.dylib"
[ -f "$DYLIB" ] || { echo "未生成 $DYLIB —— 检查 dotnet publish 日志"; exit 1; }

echo "== 2. 打包成 xcframework =="
AOT_XCF="$OUT/sts2_aot.xcframework"
rm -rf "$AOT_XCF"
xcodebuild -create-xcframework -library "$DYLIB" -output "$AOT_XCF"

echo "== 3. 把 xcframework 链进 Xcode 工程 =="
PBX="$OUT/sts2.xcodeproj/project.pbxproj"
if ! grep -q 'sts2_aot.xcframework' "$PBX"; then
    python3 - "$PBX" <<'PY'
import sys, re
p = sys.argv[1]
s = open(p, encoding='utf-8').read()

BUILD_ID  = 'DEADBEEF3F582BE20003B888'
FILE_ID   = 'DEADBEEF4F582BE20003B888'

# PBXBuildFile
s = s.replace('/* End PBXBuildFile section */',
    '\t\t%s /* sts2_aot.xcframework in Frameworks */ = {isa = PBXBuildFile; fileRef = %s /* sts2_aot.xcframework */; };\n'
    '/* End PBXBuildFile section */' % (BUILD_ID, FILE_ID), 1)

# PBXFileReference
s = s.replace('/* End PBXFileReference section */',
    '\t\t%s /* sts2_aot.xcframework */ = {isa = PBXFileReference; lastKnownFileType = wrapper.xcframework; '
    'name = sts2_aot; path = "sts2_aot.xcframework"; sourceTree = "<group>"; };\n'
    '/* End PBXFileReference section */' % FILE_ID, 1)

# Frameworks build phase
s = s.replace('\t\t\t\tDEADBEEF2F582BE20003B888 /* sts2.xcframework */,',
    '\t\t\t\tDEADBEEF2F582BE20003B888 /* sts2.xcframework */,\n'
    '\t\t\t\t%s /* sts2_aot.xcframework in Frameworks */,' % BUILD_ID, 1)

# Frameworks group
s = s.replace('\t\t\t\tDEADBEEF1F582BE20003B888 /* sts2.xcframework */,',
    '\t\t\t\tDEADBEEF1F582BE20003B888 /* sts2.xcframework */,\n'
    '\t\t\t\t%s /* sts2_aot.xcframework */,' % FILE_ID, 1)

open(p, 'w', encoding='utf-8').write(s)
print('pbxproj patched')
PY
fi

echo "== 4. 写入签名配置 =="
/usr/libexec/PlistBuddy -c "Set :method development" "$OUT/sts2/export_options.plist" 2>/dev/null || true
/usr/libexec/PlistBuddy -c "Set :teamID $TEAM_ID" "$OUT/sts2/export_options.plist" 2>/dev/null || true

echo "== 5. archive =="
rm -rf "$OUT/build"
xcodebuild archive \
    -project "$OUT/sts2.xcodeproj" \
    -scheme sts2 \
    -configuration "$CONFIG" \
    -sdk iphoneos \
    -arch arm64 \
    -archivePath "$OUT/build/sts2.xcarchive" \
    DEVELOPMENT_TEAM="$TEAM_ID" \
    PRODUCT_BUNDLE_IDENTIFIER="$BUNDLE_ID" \
    CODE_SIGN_STYLE=Automatic \
    -allowProvisioningUpdates

echo "== 6. 导出 IPA =="
xcodebuild -exportArchive \
    -archivePath "$OUT/build/sts2.xcarchive" \
    -exportPath "$OUT/build/ipa" \
    -exportOptionsPlist "$OUT/sts2/export_options.plist" \
    -allowProvisioningUpdates

echo
echo "完成：$OUT/build/ipa/sts2.ipa"
ls -la "$OUT/build/ipa/" || true
