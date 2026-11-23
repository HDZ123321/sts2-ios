#!/usr/bin/env python3
"""把 docs/RELEASE_NOTES.md 里的 {{...}} 按这一次构建的实际产物回填，然后推到 Release 正文。

为什么要有这个脚本：Release 页上的体积、哈希、commit、run 号每次构建都会变，手工改必然
漂移（v13 那一版就还写着 1,163,703,233 字节的旧包）。CI 在 IPA 落地之后跑一次这里，网页
上的说明就永远和下面的 ipa 是同一份东西。

INSTALL_IOS.md 里的那套数字同理，所以这个脚本同时负责它：

    # 回填（默认就会写 INSTALL_IOS.md，不想动它加 --no-install）
    python3 tools/refresh_release_notes.py --ipa build/sts2-unsigned.ipa \
        --tag v0.113.1-ios --repo HDZ123321/sts2-ios --run 123456789 --sha <commit>

    # 只核对不改：数字与 ipa 不一致时 exit 1，CI 用它当门禁
    python3 tools/refresh_release_notes.py --verify INSTALL_IOS.md \
        --ipa build/sts2-unsigned.ipa --tag v0.113.1-ios --repo HDZ123321/sts2-ios

用法：
    GH_TOKEN=xxx python3 tools/refresh_release_notes.py \
        --ipa build/sts2-unsigned.ipa --tag v0.107.1-ios --repo HDZ123321/sts2-ios \
        --run 123456789 --sha <commit>

不给 --token 时只渲染到 stdout / --out，不发请求，方便本地试。
"""

from __future__ import annotations

import argparse
import hashlib
import io
import json
import os
import re
import sys
import urllib.error
import urllib.request
import zipfile
from pathlib import Path

API = "https://api.github.com"


def die(message: str) -> "NoReturn":  # noqa: F821 - 只在运行期用
    sys.stderr.write("refresh_release_notes: " + message + "\n")
    raise SystemExit(1)


def digest(path: Path, algorithm: str) -> str:
    h = hashlib.new(algorithm)
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1 << 22), b""):
            h.update(chunk)
    return h.hexdigest()


def member_sizes(ipa: Path) -> dict[str, int]:
    """包内几个大件的实际字节数，用来回填结构表。"""
    wanted = {
        "sts2": "Payload/sts2.app/sts2",
        "dylib": "Payload/sts2.app/Frameworks/sts2.dylib",
        "swift": "Payload/sts2.app/Frameworks/libswift_Concurrency.dylib",
        "spine": "Payload/sts2.app/Frameworks/libspine_godot.ios.template_release.framework/"
                 "libspine_godot.ios.template_release",
        "fmod": "Payload/sts2.app/Frameworks/fmod.ios.template_release.framework/"
                "fmod.ios.template_release",
        "appex": "Payload/sts2.app/PlugIns/Sts2Island.appex/Sts2Island",
        "appex_plist": "Payload/sts2.app/PlugIns/Sts2Island.appex/Info.plist",
        "pck": "Payload/sts2.app/sts2.pck",
        "watcher": "Payload/sts2.app/Watcher.pck",
        "regent_art": "Payload/sts2.app/RegentArt.pck",
        "regent_fem": "Payload/sts2.app/RegentFemArt.pck",
        "release_info": "Payload/sts2.app/release_info.json",
        "plist": "Payload/sts2.app/Info.plist",
    }
    sizes: dict[str, int] = {}
    try:
        with zipfile.ZipFile(ipa) as archive:
            lookup = {info.filename: info.file_size for info in archive.infolist()}
    except zipfile.BadZipFile as error:
        die("IPA is not a zip file: %s" % error)
    for key, name in wanted.items():
        if name in lookup:
            sizes[key] = lookup[name]
    return sizes


# INSTALL_IOS.md 里那些「会随构建变」的位置。每条是 (正则, values 里的 key)，
# 正则的第 2 组是数字本体，改写时只换这一组，其余排版原样保留。
INSTALL_HEADER = [
    (r"^(\|\s*大小\s*\|\s*)([^|\n]+?)(\s*\|\s*)$", "SIZE"),
    (r"^(\|\s*SHA256\s*\|\s*`)([0-9a-fA-F]{64})(`\s*\|\s*)$", "SHA256"),
    (r"^(\|\s*MD5\s*\|\s*`)([0-9a-fA-F]{32})(`\s*\|\s*)$", "MD5"),
    (r"^(\|\s*源码 commit\s*\|\s*`)([0-9a-fA-F]{7,40})(`\s*\|\s*)$", "COMMIT"),
    (r"^(\|\s*构建\s*\|[^`\n]*`)(\d+)(`[^|\n]*\|\s*)$", "RUN"),
]

INSTALL_STRUCT = [
    (r"^(Payload/sts2\.app/sts2\s+)([\d,]+)(\s)", "SIZE_STS2"),
    (r"^(Payload/sts2\.app/Frameworks/sts2\.dylib\s+)([\d,]+)(\s)", "SIZE_DYLIB"),
    (r"^(Payload/sts2\.app/Frameworks/libswift_Concurrency\.dylib\s+)([\d,]+)(\s)", "SIZE_SWIFT"),
    (r"^(  libspine_godot\.ios\.template_release\s+)([\d,]+)(\s)", "SIZE_SPINE"),
    (r"^(  fmod\.ios\.template_release\s+)([\d,]+)(\s)", "SIZE_FMOD"),
    (r"^(Payload/sts2\.app/PlugIns/Sts2Island\.appex/Sts2Island\s+)([\d,]+)(\s)", "SIZE_APPEX"),
    (r"^(Payload/sts2\.app/PlugIns/Sts2Island\.appex/Info\.plist\s+)([\d,]+)(\s)", "SIZE_APPEX_PLIST"),
    (r"^(Payload/sts2\.app/sts2\.pck\s+)([\d,]+)(\s)", "SIZE_PCK"),
    (r"^(Payload/sts2\.app/Watcher\.pck\s+)([\d,]+)(\s)", "SIZE_WATCHER"),
    (r"^(Payload/sts2\.app/RegentArt\.pck\s+)([\d,]+)(\s)", "SIZE_REGENT_ART"),
    (r"^(Payload/sts2\.app/RegentFemArt\.pck\s+)([\d,]+)(\s)", "SIZE_REGENT_FEM"),
    (r"^(Payload/sts2\.app/release_info\.json\s+)([\d,]+)(\s)", "SIZE_RELEASE_INFO"),
    (r"^(Payload/sts2\.app/Info\.plist\s+)([\d,]+)(\s*)$", "SIZE_PLIST"),
]


def rewrite_install(text: str, values: dict[str, str]) -> tuple[str, list[str]]:
    """把 INSTALL_IOS.md 的数字改成这一次构建的实测值。

    返回 (新文本, 改动列表)。只替换 values 里有供给的 key，缺的保持原样并在改动列表里
    标成 skipped，这样没打进包的成员不会把文档写成 0。
    """
    changes: list[str] = []

    def apply(pattern: str, key: str, body: str) -> str:
        if key not in values:
            changes.append("%s: skipped (not in ipa)" % key)
            return body
        new = values[key]

        def repl(match: re.Match[str]) -> str:
            old = match.group(2)
            if old != new:
                changes.append("%s: %s -> %s" % (key, old, new))
            # 结构表是等宽对齐的，数字变短时左侧补空格，别把注释列推歪。
            padded = new.rjust(len(old)) if len(new) < len(old) else new
            return match.group(1) + padded + match.group(3)

        return re.sub(pattern, repl, body, flags=re.MULTILINE)

    for pattern, key in INSTALL_HEADER + INSTALL_STRUCT:
        text = apply(pattern, key, text)
    return text, changes


def verify_install(text: str, values: dict[str, str]) -> list[str]:
    """只核对：返回与 values 不一致的位置，空列表表示全对。"""
    bad: list[str] = []

    def check(pattern: str, key: str) -> None:
        if key not in values:
            return

        def repl(match: re.Match[str]) -> str:
            if match.group(2) != values[key]:
                bad.append("%s: file has %s, ipa has %s" % (key, match.group(2), values[key]))
            return match.group(0)

        re.sub(pattern, repl, text, flags=re.MULTILINE)

    for pattern, key in INSTALL_HEADER + INSTALL_STRUCT:
        check(pattern, key)
    return bad


def render(template: str, values: dict[str, str]) -> str:
    def substitute(match: re.Match[str]) -> str:
        key = match.group(1)
        if key not in values:
            die("template uses {{%s}} but nothing supplies it" % key)
        return values[key]

    return re.sub(r"\{\{(\w+)\}\}", substitute, template)


def call(url: str, token: str, method: str = "GET", payload: dict | None = None) -> dict:
    data = None if payload is None else json.dumps(payload).encode("utf-8")
    request = urllib.request.Request(url, data=data, method=method)
    request.add_header("Accept", "application/vnd.github+json")
    request.add_header("Authorization", "Bearer " + token)
    request.add_header("X-GitHub-Api-Version", "2022-11-28")
    if data is not None:
        request.add_header("Content-Type", "application/json")
    try:
        with urllib.request.urlopen(request, timeout=30) as response:
            return json.loads(response.read())
    except urllib.error.HTTPError as error:
        detail = error.read().decode("utf-8", "replace")[:400]
        die("%s %s failed: %s %s" % (method, url, error.code, detail))
    except urllib.error.URLError as error:
        die("%s %s failed: %s" % (method, url, error))
    return {}


def patch_release(repo: str, tag: str, token: str, body: str) -> None:
    # PATCH /releases/tags/<tag> 不存在（那个路由只支持 GET，会直接 404），
    # 必须先按 tag 查出 id，再 PATCH /releases/<id>。gh release edit 也是这么走的。
    release = call("%s/repos/%s/releases/tags/%s" % (API, repo, tag), token)
    release_id = release.get("id")
    if not release_id:
        die("release %s not found in %s" % (tag, repo))
    data = call("%s/repos/%s/releases/%d" % (API, repo, release_id), token, "PATCH", {"body": body})
    print("release notes updated: %s (%d bytes of markdown)" % (data.get("html_url"), len(body)))


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--ipa", required=True, help="这一次构建出来的 ipa")
    parser.add_argument("--template", default="docs/RELEASE_NOTES.md")
    parser.add_argument("--tag", required=True)
    parser.add_argument("--repo", required=True, help="owner/name")
    parser.add_argument("--run", default="", help="GitHub Actions run id")
    parser.add_argument("--sha", default="", help="构建用的 commit")
    parser.add_argument("--token", default=os.environ.get("GH_TOKEN") or os.environ.get("GITHUB_TOKEN") or "")
    parser.add_argument("--out", default="", help="渲染结果写到这个路径（不发请求）")
    parser.add_argument("--install", default="INSTALL_IOS.md",
                        help="INSTALL_IOS.md 路径，随构建回填；给空串或配 --no-install 就不碰")
    parser.add_argument("--no-install", action="store_true", help="不回填 INSTALL_IOS.md")
    parser.add_argument("--verify", default="", metavar="INSTALL_PATH",
                        help="只核对 INSTALL_IOS.md，不一致就 exit 1（CI 门禁用）")
    args = parser.parse_args()

    ipa = Path(args.ipa)
    if not ipa.is_file():
        die("ipa not found: %s" % ipa)

    sizes = member_sizes(ipa)
    total = ipa.stat().st_size
    values = {
        "SIZE": "{:,} 字节（约 {:.0f} MiB）".format(total, total / 1048576),
        "MD5": digest(ipa, "md5"),
        "SHA256": digest(ipa, "sha256"),
        "COMMIT": args.sha or "(unknown)",
        "RUN": args.run or "(unknown)",
        "DOWNLOAD_URL": "https://github.com/%s/releases/download/%s/sts2-unsigned.ipa" % (args.repo, args.tag),
        "SIZE_STS2": "{:,}".format(sizes.get("sts2", 0)),
        "SIZE_DYLIB": "{:,}".format(sizes.get("dylib", 0)),
        "SIZE_SWIFT": "{:,}".format(sizes.get("swift", 0)),
        "SIZE_SPINE": "{:,}".format(sizes.get("spine", 0)),
        "SIZE_FMOD": "{:,}".format(sizes.get("fmod", 0)),
        "SIZE_APPEX": "{:,}".format(sizes.get("appex", 0)),
        "SIZE_APPEX_PLIST": "{:,}".format(sizes.get("appex_plist", 0)),
        "SIZE_PCK": "{:,}".format(sizes.get("pck", 0)),
        "SIZE_WATCHER": "{:,}".format(sizes.get("watcher", 0)),
        "SIZE_REGENT_ART": "{:,}".format(sizes.get("regent_art", 0)),
        "SIZE_REGENT_FEM": "{:,}".format(sizes.get("regent_fem", 0)),
        "SIZE_RELEASE_INFO": "{:,}".format(sizes.get("release_info", 0)),
        "SIZE_PLIST": "{:,}".format(sizes.get("plist", 0)),
    }

    # 核对模式：只读 INSTALL_IOS.md，把不一致的条目全列出来再 exit 1。
    if args.verify:
        target = Path(args.verify)
        if not target.is_file():
            die("install doc not found: %s" % target)
        text = io.open(target, encoding="utf-8").read()
        # 没给 --sha / --run 时不要把 (unknown) 当成期望值，否则本地试跑必然报假 stale。
        expected = dict(values)
        if not args.sha:
            expected.pop("COMMIT", None)
        if not args.run:
            expected.pop("RUN", None)
        bad = verify_install(text, expected)
        if bad:
            for line in bad:
                sys.stderr.write("refresh_release_notes: stale %s\n" % line)
            sys.stderr.write("refresh_release_notes: %s has %d stale value(s); "
                             "rerun without --verify to refresh it\n" % (target, len(bad)))
            return 1
        print("install doc verified: %s" % target)
        return 0

    template = io.open(args.template, encoding="utf-8").read()
    body = render(template, values)

    if args.install and not args.no_install:
        target = Path(args.install)
        if target.is_file():
            text = io.open(target, encoding="utf-8").read()
            updated, changes = rewrite_install(text, values)
            if updated != text:
                io.open(target, "w", encoding="utf-8", newline="\n").write(updated)
                print("install doc refreshed: %s" % target)
                for line in changes:
                    print("  " + line)
            else:
                print("install doc already current: %s" % target)
        else:
            print("install doc absent, skipped: %s" % target)

    if args.out:
        io.open(args.out, "w", encoding="utf-8", newline="\n").write(body)
        print("rendered to %s (%d bytes)" % (args.out, len(body)))
    if not args.token:
        if not args.out:
            sys.stdout.write(body)
        return 0
    patch_release(args.repo, args.tag, args.token, body)
    return 0


if __name__ == "__main__":
    sys.exit(main())
