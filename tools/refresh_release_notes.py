#!/usr/bin/env python3
"""把 docs/RELEASE_NOTES.md 里的 {{...}} 按这一次构建的实际产物回填，然后推到 Release 正文。

为什么要有这个脚本：Release 页上的体积、哈希、commit、run 号每次构建都会变，手工改必然
漂移（v13 那一版就还写着 1,163,703,233 字节的旧包）。CI 在 IPA 落地之后跑一次这里，网页
上的说明就永远和下面的 ipa 是同一份东西。

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
        "appex": "Payload/sts2.app/PlugIns/Sts2Island.appex/Sts2Island",
        "pck": "Payload/sts2.app/sts2.pck",
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
    args = parser.parse_args()

    ipa = Path(args.ipa)
    if not ipa.is_file():
        die("ipa not found: %s" % ipa)
    template = io.open(args.template, encoding="utf-8").read()

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
        "SIZE_APPEX": "{:,}".format(sizes.get("appex", 0)),
        "SIZE_PCK": "{:,}".format(sizes.get("pck", 0)),
    }
    body = render(template, values)

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
