# 杀戮尖塔 2 · iOS 安装说明

## 成品

| 项目 | 值 |
|---|---|
| 文件 | `sts2-unsigned.ipa`（v2 修复版） |
| 大小 | 852,156,450 字节（813 MB） |
| SHA256 | `966bf4d54e3570d360145d14b74744f2d58ac42935ea9e1e96513a9bdf2788b6` |
| Bundle ID | `com.placeholder.sts2` |
| 最低系统 | iOS 14.0 |
| 架构 | arm64（真机） |
| 签名 | **未签名**，必须自己签 |
| 版本 | 0.107.1（对应 PC 版 v0.107.1） |

包内结构：

```
Payload/sts2.app/sts2                      68,111,632   主程序（libgodot 链接产物）
Payload/sts2.app/Frameworks/sts2.dylib    199,195,616   NativeAOT 编译出的游戏逻辑
Payload/sts2.app/sts2.pck               1,581,255,304   全部游戏资源
Payload/sts2.app/Info.plist                     1,547
```

`sts2.dylib` 导出了 `_godotsharp_game_main_init`，`sts2` 主程序通过 `@rpath/sts2.dylib` 链接它 —— 这条链是通的，引擎能在启动时找到托管入口。

## 安装（三条路，按你手头条件选）

### 路线 A：AltStore / SideStore（免费 Apple ID，7 天有效期）

需要：Windows 装 AltServer + iTunes（或 Apple Devices）+ iCloud。

1. 装 [AltServer](https://altstore.io/)，用 Apple ID 登录
2. iPhone 用数据线连电脑，AltServer 图标 → Install AltStore → 选你的设备
3. iPhone 上：设置 → 通用 → VPN与设备管理 → 信任你的 Apple ID 证书
4. 设置 → 隐私与安全性 → 开发者模式 → 打开（iOS 16+）
5. 把 `sts2-unsigned.ipa` 传到 iPhone（AirDrop / iCloud Drive / 文件 App）
6. AltStore 里 My Apps → 左上角 + → 选那个 ipa
7. 每 7 天在 AltStore 里点一次 Refresh（免费证书限制）

Bundle ID 是 `com.placeholder.sts2`，AltStore 会自动重签，不用改。

### 路线 B：Sideloadly（免费 Apple ID，同样 7 天）

1. 装 [Sideloadly](https://sideloadly.io/) + iTunes
2. IPA 拖进去，填 Apple ID，Start
3. 手机上信任证书（同路线 A 第 3、4 步）

比 AltStore 简单，但要每 7 天重签一次（重签不丢存档，存档在 App 沙盒里）。

### 路线 C：付费开发者账号（$99/年，1 年有效期）

有开发者账号的话，用 Xcode 或 Sideloadly 签名，证书 1 年有效，不用每周折腾。

### 路线 D：TrollStore（iOS 14.0–16.6.1 / 17.0，永久）

系统版本落在范围内的话，TrollStore 装完永久有效、不用签。ipa 直接丢进去就行。

## 已知功能缺失（必须提前知道）

这个包剔除了三个原生扩展，因为它们没有 iOS 二进制：

| 扩展 | 后果 |
|---|---|
| **FMOD** | **完全没声音**。所有音效、BGM 静音。 |
| **Spine** | **骨骼动画变成占位节点**。角色立绘、背景动画、部分 VFX 不动（静态图正常）。 |
| **Sentry** | 崩溃上报没了（无所谓） |

Spine 的影响范围：174 个场景文件引用 Spine 类型，主要是角色选择、商人、休息点、宝珠、Boss 背景、部分 VFX。战斗逻辑、卡牌、UI、地图全部正常。

本地实测（Windows 上模拟 iOS 配置，三个扩展全关）：**能跑到主菜单，5.1 秒**，卡牌图集 875 sprites、遗物 341、能力 264、意图 310 全部加载成功。Spine/Fmod 报 `Cannot get class` 是预期的，游戏用占位节点继续跑。

## 想补回音频 / 动画怎么办

**音频**：`NAudioManager` 只通过 `_audioNode.Call(...)` 调 GDScript 的 `audio_manager_proxy.gd`，C# 侧零 FMOD 依赖（`grep FmodServer src/ --include=*.cs` = 0 命中）。改法：写一个 C# 版 `AudioManagerProxy`，用 `AudioStreamPlayer` + 从 `banks/` 解出的音频资源，把 `_audioNode` 指向它。工作量中等，但要重做音频资源管线。

**动画**：spine-godot 官方有 iOS 目标（`SConstruct` 里 `env["platform"] == "ios"` 分支，产出 `libspine_godot.ios.template_{debug,release}.framework`）。需要 macOS + Xcode + SCons + godot-cpp 4.5 编译。有 Mac 的话可以自己出，然后放进 `addons/spine/ios/` 并恢复 `spine_godot_extension.gdextension`。

## 触摸操作

已加 `TouchInputAdapter`（`src/Core/Touch/TouchInputAdapter.cs`）：

- **单指长按 0.55 秒** → 右键（打开卡牌详情 / 查看遗物说明）
- **双指轻点** → ESC（关闭面板 / 返回）
- 其余全部走 Godot 默认的 `emulate_mouse_from_touch`：单指点击 = 左键，单指拖动 = 拖牌

## 存档

存档在 App 沙盒 `Documents` 里，卸载重装会丢。AltStore/Sideloadly 重签不卸载，所以 7 天刷新不会丢档。

## 如果装上了但打不开

| 现象 | 原因 |
|---|---|
| 闪退在启动画面 | 证书没签进 `Frameworks/sts2.dylib`。Sideloadly 勾上 "Advanced → Inject dylibs" 或确保重签整个 Payload |
| 卡在黑屏 | PCK 没被找到。确认 `sts2.pck` 与 `sts2` 在同一个 `sts2.app/` 目录下 |
| 提示 "Unable to install" | Bundle ID 冲突，改一个（Sideloadly 里可以改） |
| 提示未受信任的开发者 | 设置 → 通用 → VPN与设备管理 → 信任 |

## 构建来源

GitHub Actions 在 `macos-14` runner 上完整跑通：`dotnet publish -r ios-arm64`（NativeAOT）→ `xcodebuild -create-xcframework` → 注入 pbxproj → `xcodebuild archive`（无签名）→ 手工组 Payload → zip。

仓库：https://github.com/HDZ123321/sts2-ios

---

## 黑屏修复版（v2）说明

### 这一版修了什么

| # | 问题 | 表现 | 修法 |
|---|---|---|---|
| 1 | 非编辑器构建强制初始化 Steam | Steam 失败 → 弹 `STEAM_INIT_ERROR` 模态框 → 主菜单永远不出现 = 黑屏 | `NGame.InitializePlatform()` 把 `OS.HasFeature("mobile")` 与 `editor` 等同处理，iOS 上跳过 Steam |
| 2 | 导出包缺 14 个 `.tpsheet` | `AtlasManager: tpsheet not found` → `GetMissingTexture()` 空指针 → `Fatal error. 0xC0000005` | `export_presets.cfg` 的 `include_filter` 加上 `*.tpsheet, *.atlas, *.skel, *.spine` |
| 3 | 打包时缺 785 个 `.import`/artifact | 字体、光标图、GDScript remap 全部找不到 | `inject_pck2.py` 把 418 个 `.import` + 367 个 artifact（316.5 MB）注入 PCK |
| 4 | 手机上拿不到日志 | 只能看黑屏，没法诊断 | 开 `debug/file_logging/enable_file_logging=true` + `user_data/accessible_from_files_app=true` |

### 拿日志（黑屏时第一步做这个）

1. 装好后先跑一次游戏，让它黑屏/闪退
2. 打开 iOS「文件」App → 「我的 iPhone」→ 找到 **Slay the Spire 2**（或 `sts2`）
3. 进去 → `logs/` → `godot.log`，长按 → 共享 → 存到「文件」或发给自己
4. 里面关键几行：

```
.NET: hostfxr initialized              ← hostfxr 起来了
.NET: GodotPlugins initialized          ← AOT dylib 被 dlopen 且拿到 godotsharp_game_main_init
Steam initialization skipped            ← 修复 1 生效
AtlasManager: tpsheet not found: ...    ← 修复 2 没生效
[Startup] Time to main menu: 4,788ms    ← 真的到主菜单了
```

如果 `.NET: GodotPlugins initialized` 没出现，说明 `Frameworks/sts2.dylib` 没被加载 —— 那就去查签名：

```bash
codesign -dvvv Payload/sts2.app/Frameworks/sts2.dylib
codesign --verify --deep --strict --verbose=2 Payload/sts2.app
```

**最常见的签名错误**：只签了 `.app` 外壳，没签 `Frameworks/sts2.dylib`。iOS 上 `dlopen` 一个未签名/签名不匹配的 dylib 会被内核直接拒掉，Godot 侧只看到 `open_dynamic_library` 失败，表现就是黑屏。

正确签法（自签 p12）：

```bash
# 1. 先签内嵌的 dylib
codesign -f -s "iPhone Distribution: Your Name (TEAMID)" \
  --entitlements sts2/sts2.entitlements \
  Payload/sts2.app/Frameworks/sts2.dylib

# 2. 再签整个 app（--deep 会覆盖剩下的）
codesign -f -s "iPhone Distribution: Your Name (TEAMID)" \
  --entitlements sts2/sts2.entitlements \
  Payload/sts2.app

# 3. 验证
codesign --verify --deep --strict --verbose=2 Payload/sts2.app

# 4. 重新打包
zip -qry sts2-signed.ipa Payload
```

### 已知仍缺失（不影响启动）

- **FMOD** → 完全没声音。`FmodManager` autoload 已移除，C# 侧零 FMOD 依赖，不会崩
- **Spine** → 骨骼动画变占位节点（174 个场景）。角色选择/商人/休息点/Boss 背景会缺动画，但节点树完整，不崩
- **Sentry** → 崩溃上报没了
