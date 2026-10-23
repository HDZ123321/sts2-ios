#!/usr/bin/env bash
# 用自己的 p12 给 sts2-unsigned.ipa 正确签名。
# 用法：./SIGN_IOS.sh <identity> [ipa]
#   identity 例：'iPhone Distribution: Your Name (ABCDE12XYZ)'
# 用 `security find-identity -v -p codesigning` 查可用 identity。
set -euo pipefail

IDENTITY="${1:?需要签名 identity}"
IPA="${2:-sts2-unsigned.ipa}"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

echo "==> 解包 $IPA"
unzip -q "$IPA" -d "$WORK"
APP="$WORK/Payload/sts2.app"
ENT="$APP/sts2.entitlements"
[ -f "$ENT" ] || ENT=""

sign() {
  local target="$1"
  echo "    sign $(basename "$target")"
  if [ -n "$ENT" ]; then
    codesign -f -s "$IDENTITY" --entitlements "$ENT" --timestamp=none "$target"
  else
    codesign -f -s "$IDENTITY" --timestamp=none "$target"
  fi
}

echo "==> 先签内嵌二进制（关键：Frameworks 里的 dylib 必须先签，否则 dlopen 被内核拒绝 → 黑屏）"
for f in "$APP"/Frameworks/*; do
  [ -e "$f" ] && sign "$f"
done
# dylibs 目录里如果还有别的动态库也一并签
if [ -d "$APP/dylibs" ]; then
  find "$APP/dylibs" -type f -name '*.dylib' -print0 | while IFS= read -r -d '' f; do sign "$f"; done
fi

echo "==> 再签 app 外壳"
sign "$APP"

echo "==> 校验"
codesign --verify --deep --strict --verbose=2 "$APP"
echo "--- 内嵌 dylib 的签名信息 ---"
codesign -dvvv "$APP/Frameworks/sts2.dylib" 2>&1 | grep -E 'Identifier|Authority|TeamIdentifier|CDHash' || true

OUT="sts2-signed.ipa"
rm -f "$OUT"
( cd "$WORK" && zip -qry "$OLDPWD/$OUT" Payload )
echo "==> 完成：$OUT ($(stat -c%s "$OUT") 字节)"
echo "用 Sideloadly / AltStore / Xcode Devices 安装这个文件。"
