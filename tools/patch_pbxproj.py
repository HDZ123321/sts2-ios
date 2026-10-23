#!/usr/bin/env python3
"""把 sts2_aot.xcframework 接进 Godot 生成的 Xcode 工程。

Godot 4.5.1 的 iOS 导出模板只带 libgodot.a 与 MoltenVK.xcframework；
C# 侧的 NativeAOT 产物（sts2.dylib 打成 xcframework）需要自己补进 pbxproj。

插入四处（全部按行定位，幂等）：
  1. PBXBuildFile        —— Frameworks 链接条目 + Embed Frameworks 拷贝条目
  2. PBXFileReference    —— xcframework 文件引用
  3. PBXFrameworksBuildPhase.files —— 引用 Frameworks 链接条目（PBXBuildFile id）
  4. PBXGroup(Frameworks).children —— 引用文件引用（PBXFileReference id）
  5. PBXCopyFilesBuildPhase(Embed Frameworks).files —— 引用 Embed 条目

注意：group 的 children 只能放 PBXFileReference id，build phase 的 files 只能放
PBXBuildFile id。放错会让 xcodebuild 报
  -[PBXBuildFile group]: unrecognized selector sent to instance
并判定工程损坏。

用法:
    py -3 patch_pbxproj.py <project.pbxproj> [framework_dir_name]

默认 framework_dir_name = sts2_aot.xcframework，必须与 pbxproj 同级。
"""
import os
import re
import sys

BUILD_FILE_ID = "DEADBEEF3F582BE20003B888"       # Frameworks 链接
EMBED_FILE_ID = "DEADBEEF3F582BE20003B889"       # Embed Frameworks 拷贝
FILE_REF_ID = "DEADBEEF4F582BE20003B888"         # PBXFileReference
GODOT_BUILD_FILE_ID = "DEADBEEF2F582BE20003B888"  # 模板里 sts2.xcframework 的 build file
GODOT_FILE_REF_ID = "DEADBEEF1F582BE20003B888"    # 模板里 sts2.xcframework 的 file ref


def fail(msg):
    sys.stderr.write("patch_pbxproj: " + msg + "\n")
    sys.exit(1)


def indent_of(line):
    return line[: len(line) - len(line.lstrip("\t"))]


def find_line(lines, needle, start=0, end=None):
    end = len(lines) if end is None else end
    for i in range(start, end):
        if needle in lines[i]:
            return i
    return -1


def insert_after(lines, index, new_line):
    lines.insert(index + 1, new_line)


def validate(text):
    """确认 group.children 只引用 PBXFileReference，build phase.files 只引用 PBXBuildFile。

    只对 isa = PBXGroup 的 children 与 isa = PBX*BuildPhase 的 files 做归属检查；
    PBXVariantGroup 的 children 可以是 PBXFileReference，这里一并按 file ref 处理。
    """
    file_refs = set(re.findall(r"^\t\t([0-9A-F]{24}) /\*.*?\*/ = \{isa = PBXFileReference;",
                               text, re.M))
    build_files = set(re.findall(r"^\t\t([0-9A-F]{24}) /\*.*?\*/ = \{isa = PBXBuildFile;",
                                 text, re.M))
    variant_children = set()
    for m in re.finditer(r"isa = PBXVariantGroup;(.*?)\n\t\t\};", text, re.S):
        variant_children.update(re.findall(r"([0-9A-F]{24}) /\*", m.group(1)))
    file_refs |= variant_children

    problems = []

    # children / files 列表的成员归属
    for m in re.finditer(r"children = \((.*?)\);", text, re.S):
        for cid in re.findall(r"([0-9A-F]{24}) /\*", m.group(1)):
            if cid in build_files and cid not in file_refs:
                problems.append("PBXGroup.children 里混入 PBXBuildFile id %s" % cid)
    for m in re.finditer(r"files = \((.*?)\);", text, re.S):
        for cid in re.findall(r"([0-9A-F]{24}) /\*", m.group(1)):
            if cid in file_refs and cid not in build_files:
                problems.append("build phase.files 里混入 PBXFileReference id %s" % cid)

    return problems


def main():
    if len(sys.argv) < 2:
        fail("usage: patch_pbxproj.py <project.pbxproj> [framework_dir_name]")

    pbx = sys.argv[1]
    fw_name = sys.argv[2] if len(sys.argv) > 2 else "sts2_aot.xcframework"
    fw_stem = fw_name[: -len(".xcframework")] if fw_name.endswith(".xcframework") else fw_name

    if not os.path.isfile(pbx):
        fail("not found: " + pbx)

    with open(pbx, encoding="utf-8") as fh:
        text = fh.read()

    if BUILD_FILE_ID in text:
        problems = validate(text)
        if problems:
            fail("already patched but invalid:\n  " + "\n  ".join(problems))
        print("already patched, nothing to do")
        return

    lines = text.split("\n")

    # 1) PBXBuildFile 两条
    idx = find_line(lines, "/* End PBXBuildFile section */")
    if idx < 0:
        fail("PBXBuildFile section marker missing")
    lines.insert(idx,
                 '\t\t%s /* %s in Frameworks */ = {isa = PBXBuildFile; fileRef = %s /* %s */; };'
                 % (BUILD_FILE_ID, fw_name, FILE_REF_ID, fw_name))
    lines.insert(idx + 1,
                 '\t\t%s /* %s in Embed Frameworks */ = {isa = PBXBuildFile; fileRef = %s /* %s */; '
                 'settings = {ATTRIBUTES = (CodeSignOnCopy, RemoveHeadersOnCopy, ); }; };'
                 % (EMBED_FILE_ID, fw_name, FILE_REF_ID, fw_name))

    # 2) PBXFileReference
    idx = find_line(lines, "/* End PBXFileReference section */")
    if idx < 0:
        fail("PBXFileReference section marker missing")
    lines.insert(idx,
                 '\t\t%s /* %s */ = {isa = PBXFileReference; lastKnownFileType = wrapper.xcframework; '
                 'name = %s; path = "%s"; sourceTree = "<group>"; };'
                 % (FILE_REF_ID, fw_name, fw_stem, fw_name))

    # 3) Frameworks build phase 的 files —— 锚在模板里 sts2.xcframework 的 build file 行
    anchor = "%s /* sts2.xcframework */," % GODOT_BUILD_FILE_ID
    idx = find_line(lines, anchor)
    if idx < 0:
        fail("Frameworks build phase anchor missing: " + anchor)
    ind = indent_of(lines[idx])
    insert_after(lines, idx, "%s%s /* %s in Frameworks */," % (ind, BUILD_FILE_ID, fw_name))

    # 4) Frameworks group 的 children —— 锚在模板里 sts2.xcframework 的 file ref 行
    anchor = "%s /* sts2.xcframework */," % GODOT_FILE_REF_ID
    idx = find_line(lines, anchor)
    if idx < 0:
        fail("Frameworks group anchor missing: " + anchor)
    ind = indent_of(lines[idx])
    insert_after(lines, idx, "%s%s /* %s */," % (ind, FILE_REF_ID, fw_name))

    # 5) Embed Frameworks copy phase 的 files
    idx = find_line(lines, 'name = "Embed Frameworks";')
    if idx < 0:
        fail('"Embed Frameworks" copy phase missing')
    open_idx = -1
    for i in range(idx, -1, -1):
        if "files = (" in lines[i]:
            open_idx = i
            break
    if open_idx < 0:
        fail("Embed Frameworks files list not found")
    close_idx = find_line(lines, ");", open_idx + 1)
    if close_idx < 0:
        fail("Embed Frameworks files list not closed")
    ind = indent_of(lines[open_idx + 1]) if close_idx > open_idx + 1 else "\t\t\t\t"
    lines.insert(close_idx, "%s%s /* %s in Embed Frameworks */," % (ind, EMBED_FILE_ID, fw_name))

    text = "\n".join(lines)

    problems = validate(text)
    if problems:
        fail("patched result is invalid:\n  " + "\n  ".join(problems))

    with open(pbx, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(text)

    print("patched: " + fw_name)


if __name__ == "__main__":
    main()
