# tools/texdump — 把桌面纹理重烤成 iOS 能采样的格式

v0.113.1 的 PCK 里只有 BPTC（BC7）与 S3TC（DXT5/DXT1）两种 GPU 纹理变体，iOS 的
Metal 侧采样不了。Godot 的 iOS preset 声明的 feature 集合是 `{etc2, astc, arm64}`
（`editor/export/editor_export_platform_apple_embedded.cpp`），导出器只会挑 ASTC / ETC2，
两种都不在包里 —— 所以移植层自己合成：

| 原变体 | 重烤成 | format |
|---|---|---|
| `.bptc.ctex`（BPTC / BC7） | `.astc.ctex` | 35（ASTC 4×4） |
| `.s3tc.ctex`（DXT5 / DXT1） | `.etc2.ctex` | 31 / 30（ETC2 RGBA8 / RGB8） |

实测 1573 个（ASTC 973 + ETC2 600），`done=1573 failed=0`，约 3 分钟。

## 为什么不能直接用编辑器导入器

`.ctex` 是 Godot 的运行时纹理容器，格式是 36 字节头 + 16 字节 image 头 + 裸 GPU 数据：

```
"GST2" | u32 version=1 | u32 w | u32 h | u32 df | u32 mipmap_limit | 3 × u32 reserved   (36 B)
u32 data_format=0 | u16 w | u16 h | u32 mipmaps | u32 format | payload                    (16 B + N)
```

`data_format=0` 时文件体就是裸压缩数据，没有长度前缀。所以这里绕开导入器：按上面的布局
把 payload 读出来交给 `Image.create_from_data`，解压成 RGBA8，再用 `Image.compress()`
压成目标格式，最后按同样的布局拼回去。头部其余字段（`df` / `mipmap_limit` / `mips`）
原样保留。

## 前置条件

```
<workspace>/pck113/                 解包后的 v0.113.1 树（要有 .godot/imported/）
<workspace>/pck113_tex/             输出树，镜像 pck113 的结构（脚本自己建）
<workspace>/ci_bundle/tools/texdump/  本目录
```

workspace 由脚本从 `res://` 往上找「含 `pck113` 的目录」得到，找不到就报
`WORKSPACE NOT FOUND`；也可以用环境变量 `STS2_WORKSPACE` 直接指定。这样 checkout
放在哪都能跑，不写死路径。

## 跑法

```bash
# 1) 生成工作清单（1573 条，astc=973 etc2=600）
python3 tools/texdump/make_plan.py
#    顺手生成 verify.gd 用的小样本
python3 tools/texdump/make_plan.py --verify-out tools/texdump/_verify.json

# 2) 重烤（Godot headless；重跑会跳过已存在的文件，中断了直接再跑）
<godot> --headless --path ci_bundle/tools/texdump --script recook.gd

# 3) 抽检 12 个样本，确认 header 与 format 都对
<godot> --headless --path ci_bundle/tools/texdump --script verify.gd
```

`_plan.json` / `_verify.json` 是生成物，不入库（见 `.gitignore`）。

## 打完之后的自查

```bash
# 挂载 dist/sts2.pck，逐个加载每个 .import 指向的 .ctex
<godot> --headless --path ci_bundle/tools/texdump --script pckcheck.gd
# 全量：每个 .import 都加载一次（含 .spatlas / .spskel / .fontdata / scene）
<godot> --headless --path ci_bundle/tools/texdump --script remapcheck.gd
# 只看包里有什么、ctex 的 format 分布
<godot> --headless --path ci_bundle/tools/texdump --script dump.gd
```

三个脚本都接受显式的包路径：`--script pckcheck.gd -- /path/to/sts2.pck`。

期望结果（2026-11-23 在 `dist/sts2.pck` 上实测）：

```
pckcheck.gd   [pck] unique remap targets=4465 ctex=4025
              [pck] RESULT ok=4025 failed=0 pixels=2021377134
              [pck] fmt histogram { 5: 1757, 35: 973, 31: 542, 30: 58, 4: 668, 15: 1, 1: 2, 0: 24 }
```

`35 / 31 / 30` 就是重烤出来的 ASTC 973 + ETC2 542 + 58，**直方图里没有 `22 / 23 / 24`
（BPTC / DXT5 / DXT1）**，说明桌面变体全换掉了；`5 / 4` 是本来就没压缩的 2452 个。

`remapcheck.gd` 会报 `ok=4119 failed=346`，**这 346 个是预期的**：正好等于
`.spatlas` 177 + `.spskel` 169。它按 `.import` 反推源路径去 `ResourceLoader.load`，
而 spine 的源 `.atlas` / `.skel` 本来就不在包里（只有编译后的 `.spatlas` / `.spskel`），
游戏也是直接加载编译产物，不走 remap。除这三类之外 `failed` 应当为 0。


## 与 pack_pck.py 的关系

`recook.gd` 只产出 `.ctex` 文件，**不碰 `.import`**。让 PCK 里出现这些变体是
`pack_pck.py` 的事：它读 `_newimports.json`（4025 条重写后的 `.import` 内容），把
`path=` 指向 `.astc.ctex` / `.etc2.ctex` 的那批写进包里，同时把 `pck113_tex/` 下的
重烤文件搬进去。所以顺序是 `make_plan.py → recook.gd → pack_pck.py`。
