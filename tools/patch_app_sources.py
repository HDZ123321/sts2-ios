#!/usr/bin/env python3
"""把 Dynamic Island 的 Swift/ObjC 源码接进 Godot 生成的 Xcode 工程。

Godot 4.5.1 的 iOS 导出模板只有 dummy.cpp 一个源文件，Swift 完全没开（模板里虽然放了
dummy.swift，但它不在 Sources phase 里，SWIFT_VERSION 也没设）。灵动岛需要 ActivityKit，
而 ActivityKit 只有 Swift 接口，所以必须自己开 Swift 并挂三个文件：

  sts2/island/Sts2RunActivityAttributes.swift   app + 扩展共用（这里只进 app）
  sts2/island/Sts2IslandBridge.swift            app 侧轮询器
  sts2/island/Sts2IslandBootstrap.m             +load 启动钩子

同时补两处构建设置：
  SWIFT_VERSION = 5.0                          有 .swift 就必须设，否则 xcodebuild 直接报错
  OTHER_SWIFT_FLAGS = -Xfrontend -disable-autolink-framework -Xfrontend ActivityKit
  OTHER_LDFLAGS += -weak_framework ActivityKit

后两项是为了让 app 保持在 iOS 14.0 上能启动：ActivityKit 是 16.1 才有的系统库，
强链接会让 dyld 在旧系统上直接拒绝启动，弱链接 + #available 守卫则只是没有灵动岛。
Swift 编译器默认给 import 的每个 framework 发 autolink 记录，那样链接出来仍是强引用，
所以必须同时禁掉 ActivityKit 的 autolink（-disable-autolink-framework 是 swift-frontend
选项，必须用 -Xfrontend 传），再手工弱链接。

插入四处（按行定位，幂等）：
  1. PBXBuildFile        —— "x in Sources"
  2. PBXFileReference    —— SOURCE_ROOT 相对路径，不受 group 层级影响
  3. PBXSourcesBuildPhase.files —— 引用 PBXBuildFile id
  4. PBXGroup(sts2).children    —— 引用 PBXFileReference id

id 由文件路径的 md5 派生，同一个输入永远得到同一个 id，重复执行无副作用。

用法:
    python3 patch_app_sources.py <project.pbxproj>
"""
import hashlib
import os
import re
import sys

# (相对仓库根的路径, lastKnownFileType)
SOURCES = [
    ("sts2/island/Sts2RunActivityAttributes.swift", "sourcecode.swift"),
    ("sts2/island/Sts2IslandBridge.swift", "sourcecode.swift"),
    ("sts2/island/Sts2IslandBootstrap.m", "sourcecode.c.objc"),
]

SWIFT_SETTINGS = [
    'SWIFT_VERSION = 5.0;',
    'OTHER_SWIFT_FLAGS = "-Xfrontend -disable-autolink-framework -Xfrontend ActivityKit";',
]

LDFLAGS_NEEDLE = "OTHER_LDFLAGS = "
LDFLAGS_WEAK = "-weak_framework ActivityKit"

# group children 的锚点：dummy.cpp 作为「子项」出现在 sts2 group 里那一行（带尾逗号、无等号）
GROUP_ANCHOR = "1FF8DBB01FBA9DE1009DE660 /* dummy.cpp */,"


def fail(msg):
    sys.stderr.write("patch_app_sources: " + msg + "\n")
    sys.exit(1)


def stable_id(path, salt):
    digest = hashlib.md5((salt + ":" + path).encode("utf-8")).hexdigest()
    return digest[:24].upper()


def indent_of(line):
    return line[: len(line) - len(line.lstrip("\t"))]


def find_line(lines, needle, start=0):
    for i in range(start, len(lines)):
        if needle in lines[i]:
            return i
    return -1


def list_spans(text, opener):
    """返回每个 `<opener>( ... );` 的字符区间，用于判定某个 id 到底躺在哪个列表里。"""
    spans = []
    start = 0
    while True:
        i = text.find(opener, start)
        if i < 0:
            return spans
        depth = 0
        j = i + len(opener)
        while j < len(text):
            ch = text[j]
            if ch == "(":
                depth += 1
            elif ch == ")":
                if depth == 0:
                    break
                depth -= 1
            j += 1
        spans.append((i, j))
        start = j


def ids_in_line(nid, text, opener):
    """该 id 的任意一次出现是否落在某个 `<opener>( ... );` 列表里。

    注意不能用 text.find 取首次出现 —— 首次出现是声明本身，永远在列表外面。
    """
    spans = list_spans(text, opener)
    if not spans:
        return False
    pos = 0
    while True:
        i = text.find(nid, pos)
        if i < 0:
            return False
        for begin, end in spans:
            if begin < i < end:
                return True
        pos = i + len(nid)


def validate(text):
    """结构校验：id 声明归属正确 + 每个新 id 正好出现在该出现的列表里。"""
    file_refs = set(re.findall(r"^\t\t([0-9A-F]{24}) /\*.*?\*/ = \{isa = PBXFileReference;", text, re.M))
    build_files = set(re.findall(r"^\t\t([0-9A-F]{24}) /\*.*?\*/ = \{isa = PBXBuildFile;", text, re.M))
    problems = []
    for m in re.finditer(r"children = \((.*?)\);", text, re.S):
        for cid in re.findall(r"([0-9A-F]{24}) /\*", m.group(1)):
            if cid in build_files and cid not in file_refs:
                problems.append("PBXGroup.children 里混入 PBXBuildFile id %s" % cid)
    for m in re.finditer(r"files = \((.*?)\);", text, re.S):
        for cid in re.findall(r"([0-9A-F]{24}) /\*", m.group(1)):
            if cid in file_refs and cid not in build_files:
                problems.append("build phase.files 里混入 PBXFileReference id %s" % cid)
    for rel_path, _ in SOURCES:
        build_file_id = stable_id(rel_path, "buildfile")
        file_ref_id = stable_id(rel_path, "fileref")
        name = os.path.basename(rel_path)
        if build_file_id not in build_files:
            problems.append("%s 的 PBXBuildFile 没声明" % name)
        if file_ref_id not in file_refs:
            problems.append("%s 的 PBXFileReference 没声明" % name)
        if not ids_in_line(build_file_id, text, "files = ("):
            problems.append("%s 不在任何 build phase 的 files 列表里" % name)
        if not ids_in_line(file_ref_id, text, "children = ("):
            problems.append("%s 不在任何 group 的 children 列表里" % name)
    if "SWIFT_VERSION" not in text:
        problems.append("SWIFT_VERSION 没写进去")
    return problems


def insert_sources(text):
    lines = text.split("\n")
    added = []
    for rel_path, file_type in SOURCES:
        name = os.path.basename(rel_path)
        build_file_id = stable_id(rel_path, "buildfile")
        file_ref_id = stable_id(rel_path, "fileref")
        if build_file_id in text:
            continue
        # 1) PBXBuildFile
        idx = find_line(lines, "/* End PBXBuildFile section */")
        if idx < 0:
            fail("PBXBuildFile section marker missing")
        lines.insert(idx, '\t\t%s /* %s in Sources */ = {isa = PBXBuildFile; fileRef = %s /* %s */; };'
                     % (build_file_id, name, file_ref_id, name))
        # 2) PBXFileReference —— SOURCE_ROOT 相对，避开 group 嵌套
        idx = find_line(lines, "/* End PBXFileReference section */")
        if idx < 0:
            fail("PBXFileReference section marker missing")
        lines.insert(idx, '\t\t%s /* %s */ = {isa = PBXFileReference; lastKnownFileType = %s; '
                          'name = %s; path = "%s"; sourceTree = SOURCE_ROOT; };'
                     % (file_ref_id, name, file_type, name, rel_path))
        # 3) Sources phase
        idx = find_line(lines, "isa = PBXSourcesBuildPhase;")
        if idx < 0:
            fail("PBXSourcesBuildPhase missing")
        open_idx = find_line(lines, "files = (", idx)
        if open_idx < 0:
            fail("Sources phase files list missing")
        # 挂在 dummy.cpp 那一条后面，沿用它的缩进
        anchor = find_line(lines, "/* dummy.cpp in Sources */", open_idx)
        if anchor < 0:
            fail("dummy.cpp build file anchor missing")
        ind = indent_of(lines[anchor])
        lines.insert(anchor + 1, "%s%s /* %s in Sources */," % (ind, build_file_id, name))
        # 4) group children —— 锚在 sts2 group 里 dummy.cpp 那条子项
        anchor = find_line(lines, GROUP_ANCHOR)
        if anchor < 0:
            fail("dummy.cpp group child anchor missing")
        ind = indent_of(lines[anchor])
        lines.insert(anchor + 1, "%s%s /* %s */," % (ind, file_ref_id, name))
        added.append(name)
    return "\n".join(lines), added


def insert_settings(text):
    """给项目级 XCBuildConfiguration 补 SWIFT_VERSION / 禁 autolink / 弱链接。"""
    changes = []
    lines = text.split("\n")
    out = []
    for line in lines:
        stripped = line.strip()
        if stripped.startswith(LDFLAGS_NEEDLE) and LDFLAGS_WEAK not in line:
            ind = indent_of(line)
            value = stripped[len(LDFLAGS_NEEDLE):].rstrip(";").strip()
            if value.startswith('"') and value.endswith('"'):
                inner = value[1:-1]
                new_value = '"%s %s"' % (inner.rstrip(), LDFLAGS_WEAK)
            else:
                new_value = '"%s %s"' % (value, LDFLAGS_WEAK)
            out.append("%sOTHER_LDFLAGS = %s;" % (ind, new_value))
            changes.append("OTHER_LDFLAGS += " + LDFLAGS_WEAK)
            continue
        out.append(line)
    text = "\n".join(out)

    if "SWIFT_VERSION" not in text:
        lines = text.split("\n")
        out = []
        for line in lines:
            out.append(line)
            if line.strip().startswith(LDFLAGS_NEEDLE):
                ind = indent_of(line)
                for setting in SWIFT_SETTINGS:
                    out.append(ind + setting)
                changes.append("SWIFT_VERSION / OTHER_SWIFT_FLAGS")
        text = "\n".join(out)
    return text, changes


def main():
    if len(sys.argv) < 2:
        fail("usage: patch_app_sources.py <project.pbxproj>")
    pbx = sys.argv[1]
    if not os.path.isfile(pbx):
        fail("not found: " + pbx)
    with open(pbx, encoding="utf-8") as fh:
        text = fh.read()

    for rel_path, _ in SOURCES:
        if not os.path.isfile(rel_path):
            fail("source file missing from the repo: " + rel_path)

    text, added = insert_sources(text)
    text, changed = insert_settings(text)

    problems = validate(text)
    if problems:
        fail("patched result is invalid:\n  " + "\n  ".join(problems))

    with open(pbx, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(text)

    if added:
        print("sources added: " + ", ".join(added))
    else:
        print("sources already present")
    if changed:
        print("settings applied: " + "; ".join(changed))
    else:
        print("settings already present")


if __name__ == "__main__":
    main()
