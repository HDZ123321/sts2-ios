#!/usr/bin/env python3
"""把 sts2_aot.xcframework 接进 Godot 生成的 Xcode 工程。

Godot 4.5.1 的 iOS 导出模板只带 libgodot.a 与 MoltenVK.xcframework；
C# 侧的 NativeAOT 产物（sts2.dylib 打成 xcframework）需要自己补进 pbxproj。
本脚本幂等：重复执行不会重复插入。

用法:
    py -3 patch_pbxproj.py <project.pbxproj> [framework_dir_name]

默认 framework_dir_name = sts2_aot.xcframework，必须在 pbxproj 同级目录。
"""
import os
import sys

BUILD_FILE_ID = "DEADBEEF3F582BE20003B888"
FILE_REF_ID = "DEADBEEF4F582BE20003B888"
ANCHOR_FRAMEWORK_FILE_REF = "DEADBEEF1F582BE20003B888"


def fail(msg):
    sys.stderr.write("patch_pbxproj: " + msg + "\n")
    sys.exit(1)


def main():
    if len(sys.argv) < 2:
        fail("usage: patch_pbxproj.py <project.pbxproj> [framework_dir_name]")

    pbx = sys.argv[1]
    fw_name = sys.argv[2] if len(sys.argv) > 2 else "sts2_aot.xcframework"
    fw_stem = fw_name[:-len(".xcframework")] if fw_name.endswith(".xcframework") else fw_name

    if not os.path.isfile(pbx):
        fail("not found: " + pbx)

    with open(pbx, encoding="utf-8") as fh:
        src = fh.read()

    if BUILD_FILE_ID in src:
        print("already patched, nothing to do")
        return

    # 1) PBXBuildFile：Frameworks 链接 + Embed Frameworks 拷贝，各一条
    build_files = (
        "\t\t{b} /* {n} in Frameworks */ = {{isa = PBXBuildFile; fileRef = {r} /* {n} */; }};\n"
        "\t\t{be} /* {n} in Embed Frameworks */ = {{isa = PBXBuildFile; fileRef = {r} /* {n} */; settings = {{ATTRIBUTES = (CodeSignOnCopy, RemoveHeadersOnCopy, ); }}; }};\n"
    ).format(b=BUILD_FILE_ID, be=BUILD_FILE_ID[:-1] + "9", r=FILE_REF_ID, n=fw_name)
    marker = "/* End PBXBuildFile section */"
    if marker not in src:
        fail("PBXBuildFile section marker missing")
    src = src.replace(marker, build_files + marker, 1)

    # 2) PBXFileReference
    file_ref = (
        '\t\t{r} /* {n} */ = {{isa = PBXFileReference; lastKnownFileType = wrapper.xcframework; '
        'name = {s}; path = "{n}"; sourceTree = "<group>"; }};\n'
    ).format(r=FILE_REF_ID, n=fw_name, s=fw_stem)
    marker = "/* End PBXFileReference section */"
    if marker not in src:
        fail("PBXFileReference section marker missing")
    src = src.replace(marker, file_ref + marker, 1)

    # 3) 链接进 Frameworks build phase（锚定在 godot 的 xcframework 那一行之后）
    anchor = "\t\t\t\t{a} /* sts2.xcframework */,".format(a=ANCHOR_FRAMEWORK_FILE_REF)
    if anchor not in src:
        fail("frameworks build phase anchor missing: " + anchor.strip())
    src = src.replace(
        anchor,
        anchor + "\n\t\t\t\t{b} /* {n} in Frameworks */,".format(b=BUILD_FILE_ID, n=fw_name),
        1,
    )

    # 4) 加进 Frameworks group（锚定 fileRef 那一行之后）
    anchor = "\t\t{a} /* sts2.xcframework */,".format(a=ANCHOR_FRAMEWORK_FILE_REF)
    if anchor not in src:
        fail("frameworks group anchor missing")
    src = src.replace(
        anchor,
        anchor + "\n\t\t\t\t{r} /* {n} */,".format(r=FILE_REF_ID, n=fw_name),
        1,
    )

    # 5) 加进 Embed Frameworks copy phase（dstSubfolderSpec = 10）
    embed_marker = 'name = "Embed Frameworks";'
    if embed_marker not in src:
        fail('"Embed Frameworks" copy phase missing')
    idx = src.index(embed_marker)
    files_open = src.rindex("files = (", 0, idx)
    files_close = src.index(");", files_open)
    embed_line = "\n\t\t\t\t{be} /* {n} in Embed Frameworks */,".format(
        be=BUILD_FILE_ID[:-1] + "9", n=fw_name
    )
    src = src[:files_close] + embed_line + "\n\t\t\t" + src[files_close:]

    with open(pbx, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(src)

    print("patched: " + fw_name)


if __name__ == "__main__":
    main()
