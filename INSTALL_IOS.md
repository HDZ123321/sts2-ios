# 杀戮尖塔 2 · iOS 安装说明

## 成品

| 项目 | 值 |
|---|---|
| 文件 | `sts2-unsigned.ipa` |
| 大小 | 850,446,961 字节（811 MB） |
| SHA256 | `375b2548c18ed209cb29144f2142957d6e6aa58417d97422031933a5676d37ee` |
| Bundle ID | `com.placeholder.sts2` |
| 最低系统 | iOS 14.0 |
| 架构 | arm64（真机） |
| 签名 | **未签名**，必须自己签 |
| 版本 | 0.107.1（对应 PC 版 v0.107.1） |

包内结构：

```
Payload/sts2.app/sts2                      68,111,632   主程序（libgodot 链接产物）
Payload/sts2.app/Frameworks/sts2.dylib    199,195,584   NativeAOT 编译出的游戏逻辑
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
