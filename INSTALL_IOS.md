# 杀戮尖塔 2 · iOS 安装说明

## 成品

| 项目 | 值 |
|---|---|
| 文件名 | `sts2-unsigned.ipa` |
| 大小 | 850,749,793 字节（约 811 MB） |
| SHA256 | `85b440a78e7d033294c4e2ecbfa191d3edc58f7087df7e061bbde87e104f4a88` |
| Bundle ID | `com.placeholder.sts2` |
| 最低系统 | iOS 14.0 |
| 架构 | arm64（真机，模拟器不支持） |
| 签名 | **未签名**，需要自己签 |
| 游戏版本 | 0.107.1 |
| 下载 | https://github.com/HDZ123321/sts2-ios/releases/download/v0.107.1-ios/sts2-unsigned.ipa |

包内结构：

```
Payload/sts2.app/sts2                         68,111,632   主程序（libgodot 链接产物）
Payload/sts2.app/Frameworks/sts2.dylib       199,447,816   NativeAOT 产物（游戏逻辑）
Payload/sts2.app/sts2.pck                  1,582,287,648   全部游戏资源
Payload/sts2.app/Info.plist                        1,547
```

`sts2.dylib` 导出 `_godotsharp_game_main_init`，`sts2` 主程序通过 `@rpath/sts2.dylib` 链接它 —— 两者必须一起签通，否则启动时找不到托管入口。

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
| **Sentry** | 崩溃上报没了（v3 起在 mobile 上主动关闭，避免它把启动流程打断） |

Spine 的影响范围：174 个场景文件引用 Spine 类型，主要是角色选择、商人、休息点、宝珠、Boss 背景、部分 VFX。战斗逻辑、卡牌、UI、地图全部正常。

本地实测（Windows 上模拟 iOS 配置，三个扩展全关 + 强制走 mobile 分支）：**能跑到主菜单，4.8 秒**，卡牌图集 875 sprites、遗物 341、能力 264、意图 310 全部加载成功，`.tpsheet not found` 计数 0，无 `Fatal error`。Spine/Fmod 报 `Cannot get class` 是预期的，游戏用占位节点继续跑。

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

## 当前版本（v5）：修掉 `RELIC.ASTROLABE`

v4 把「致命错误」里的 `Reflection-based serialization` 修好了，但同一位置又冒出一条新的：

```
System.TypeInitializationException: A type initializer threw an exception.
 ---> System.Collections.Generic.KeyNotFoundException:
      The given key 'RELIC.ASTROLABE' was not present in the dictionary.
   at MegaCrit.Sts2.Core.Models.ModelDb.Get[T]()
   at MegaCrit.Sts2.Core.Models.Events.Darv..cctor()
   at MegaCrit.Sts2.Core.Models.Events.Darv..ctor()
   at System.ActivatorImplementation.CreateInstance(Type, Boolean)
   at MegaCrit.Sts2.Core.Models.ModelDb.Init()
   at MegaCrit.Sts2.Core.Helpers.OneTimeInitialization.ExecuteEssential()
```

原因是**编译流水线里的补丁打错了程序集**，不是游戏代码的问题。

### 背景：`beforefieldinit` 为什么重要

游戏原始 `sts2.dll` 里，绝大多数带静态构造函数的类型都标着 `beforefieldinit`（C# 的默认语义：静态构造器可以在**首次访问该类型的任何静态字段之前**的任意时刻运行，运行时可以推迟到真正需要时才跑）。

用 ilspy 反编译再重新编译，`static Darv()` 变成了**显式静态构造函数**，`beforefieldinit` 丢了。语义就从「可以推迟」变成「触碰类型即执行」。

`ModelDb.Init()` 是按索引顺序 `0..1623` 逐个 `Activator.CreateInstance(type)` 的。`Darv` 在 index 761，`Astrolabe` 在 index 1333 —— 构造 `Darv` 时 `Astrolabe` 还没进 `_contentById`，`Darv` 的静态构造器却要 `ModelDb.Relic<Astrolabe>()`，于是 `KeyNotFoundException`。

修法是给这 7 个类型把 `beforefieldinit` 标志补回去（`Mono.Cecil` 改 `TypeAttributes.BeforeFieldInit`）：

```
MegaCrit.Sts2.Core.Assets.AssetSets
MegaCrit.Sts2.Core.DevConsole.ConsoleCommands.LeaderboardConsoleCmd
MegaCrit.Sts2.Core.Localization.LocManager
MegaCrit.Sts2.Core.Models.Events.Darv
MegaCrit.Sts2.Core.Models.Events.JungleMazeAdventure
MegaCrit.Sts2.Core.Multiplayer.Serialization.ModelIdSerializationCache
MegaCrit.Sts2.Core.Nodes.Screens.Timeline.NEraColumn
```

### v4 为什么没生效

补丁 MSBuild target 改的是 `$(TargetPath)` = `bin/Release/ios-arm64/sts2.dll`，但 **ILC（NativeAOT 的编译器）读的是 `@(IntermediateAssembly)` = `obj/Release/ios-arm64/sts2.dll`**（`Microsoft.NETCore.Native.targets:171` 的 `ManagedBinary Include="@(IntermediateAssembly)"`）。

补丁打在 ILC 从不读的那份副本上，所以构建日志明明打印了 `patched: ... Darv ...`、`types requested=7 matched=7 patched=7`，AOT 镜像里却一个 `BeforeFieldInit` 都没有。

还有个次要坑：`IntermediateAssembly` 是 **item** 不是 property，写成 `$(IntermediateAssembly)` 会展开成空串，让整个 target 静默跳过。

### v5 的改法

- target 改挂 `AfterTargets=CoreCompile` / `BeforeTargets=CopyFilesToOutputDirectory;IlcCompile`
- 直接就地改 `obj/` 那份（`bin/` 靠正常的复制步骤拿到已打补丁的字节）
- 用 `@(IntermediateAssembly->'%(FullPath)')` 取路径
- 给 Cecil 传第 4 个参数 `libs/`，否则在 `obj/` 旁边解析不到 `Sentry`/`Steamworks` 等引用
- **新增 CI 断言步骤**：`TypeFlags` 的 verify 模式检查 `obj/Release/ios-arm64/sts2.dll` 里这 7 个类型是否都带 `BeforeFieldInit`，缺一个就 `exit 1`，不让 dylib 出包

### v5 关键判据

```
[INFO] [Sentry.NET] Disabled on mobile: native sentry library is not shipped in this build
[INFO] Steam initialization skipped (editor mode). Use --force-steam to enable.
[INFO] AtlasManager: Loaded ui_atlas with 102 sprites
[INFO] ModelIdSerializationCache initialized. Categories: 20 Entries: 1622 Epochs: 57 Hash: 3954186980
[INFO] [Startup] Time to main menu: N,NNNms
```

出现 `Time to main menu` 就说明主菜单真的起来了。如果还看到 `KeyNotFoundException 'RELIC.ASTROLABE'`，说明装的是 v4 或更早的包。

本地复现这个 bug 的方法：`sts2.runtimeconfig.json` 里把
`System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault` 设 `false` 之外，
`beforefieldinit` 这条在 JIT 下也会炸（因为它是 C# 语义差异，不是 AOT 特有），
所以只要用重新编译的 `sts2.dll` 且不打卡，桌面跑 `--force-steam=off` 一样能看到 `RELIC.ASTROLABE`。

---

v3 之后游戏已经能进到启动流程，但弹出「遇到了致命错误」。v4 修的是这条：

```
System.InvalidOperationException: Reflection-based serialization has been disabled for this application.
   at System.Text.Json.JsonSerializer.Deserialize[TValue](String, JsonSerializerOptions)
   at MegaCrit.Sts2.Core.Assets.AtlasManager.LoadAtlasInternal(String atlasName)
   at MegaCrit.Sts2.Core.Assets.AtlasManager.LoadEssentialAtlases()
   at MegaCrit.Sts2.Core.Helpers.OneTimeInitialization.ExecuteEssential()
   at MegaCrit.Sts2.Core.Nodes.NGame.<GameStartup>d__128.MoveNext()
```

`AtlasManager` 读 `.tpsheet` 时用的是裸 `JsonSerializer.Deserialize<TpSheetData>(json, _jsonOptions)`，而 `_jsonOptions` 只设了 `PropertyNameCaseInsensitive`，没有 `TypeInfoResolver`。桌面 JIT 下 System.Text.Json 会退回反射实现，能跑；iOS 是 NativeAOT，`JsonSerializer.IsReflectionEnabledByDefault = false`，直接抛异常。

这一炸在 `OneTimeInitialization.ExecuteEssential()` 的第一步（`AtlasManager.LoadEssentialAtlases()`），后面 `LocManager.Initialize()` / `ModelDb.Init()` / `ModelIdSerializationCache.Init()` / `MessageTypes.Initialize()` / `ActionTypes.Initialize()` 一个都没跑，`GameStartup` 抛到 `GameStartupError()` → 致命错误弹窗。

**修法**：加源生成的 JSON 契约，把 `_jsonOptions.TypeInfoResolver` 指过去。

```csharp
// src/Core/Assets/TpSheetSerializerContext.cs
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(TpSheetData))]
[JsonSerializable(typeof(TpSheetTexture))]
[JsonSerializable(typeof(TpSheetSprite))]
[JsonSerializable(typeof(TpSheetRect))]
[JsonSerializable(typeof(TpSheetSize))]
internal sealed partial class TpSheetSerializerContext : JsonSerializerContext { }
```

```csharp
// src/Core/Assets/AtlasManager.cs
private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    TypeInfoResolver = TpSheetSerializerContext.Default
};
```

**怎么验的**：桌面 JIT 测不出这类问题，必须在 `sts2.runtimeconfig.json` 里显式关掉反射 JSON：

```json
"configProperties": {
  "System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault": false,
  "System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported": false
}
```

关掉后跑 Windows 导出包：`Reflection-based serialization` 计数 0、`tpsheet not found` 计数 0、13 个图集全部加载（`ui 102 / compressed 45 / epoch 57 / relic 341 / relic_outline 341 / power 264 / card 875 / potion 63 / potion_outline 63 / stats_screen 16 / intent 310 / era 27 / controller 112`）、`Time to main menu: 4,220ms`、`Preloading 'Common' Complete: assets=778`。

**顺手全量审计过**：`grep -rn "JsonSerializer\." src/ --include=*.cs | grep -v Generated` 只剩 26 处，全部走 `JsonSerializationUtility.GetTypeInfo<T>()` 或 `XxxSerializerContext.Default.Xxx`；裸调用只有 `AtlasManager.cs:112` 这一处，已修。`RuntimeHelpers.PrepareMethod`（`OneTimeInitialization.PrewarmJit()`）在 NativeAOT 下是 no-op，不抛。

### v4 关键判据

```
[INFO] AtlasManager: Loaded ui_atlas with 102 sprites      ← v4 新增，出现就说明过了
[INFO] AtlasManager: Loaded card_atlas with 875 sprites
[INFO] ModelIdSerializationCache initialized. Categories: 20 Entries: 1622 ...
[INFO] [Startup] Time to main menu: N,NNNms
```

如果日志里出现 `Reflection-based serialization has been disabled`，说明装的是旧包。

---

## 黑屏修复版（v3）说明

### v3 修的是真凶

v2 修的三条（Steam 强制初始化、缺 `.tpsheet`、缺 785 个 `.import`）都是真问题，但都不是你那次黑屏的原因。真凶在日志里躺着：

```
ERROR: System.DllNotFoundException: Unable to load shared library 'sentry-native'
   at Sentry.Native.C.sentry_options_new()
   at Sentry.SentrySdk.Init(Action`1)
   at MegaCrit.Sts2.Core.Debug.SentryService.Initialize()
   at MegaCrit.Sts2.Core.Nodes.NGame._EnterTree()
```

`NGame._EnterTree()` 第一件事就是 `SentryService.Initialize()`，而 iOS 包里没有 `sentry-native.dylib`。这个异常没人接，从 `_EnterTree()` 直接抛出去，后面 20 多个 `GetNode<>` 一个都没跑，`TaskHelper.RunSafely(GameStartupWrapper())` 永远没被调度 —— 所以主菜单从未开始构建，就是纯黑屏。

日志里 85 条 `NullReferenceException` 全是它的连锁反应：`NReactionWheel._Input()`（82 次，每次触摸都报）、`NBackgroundModeHandler.EnterBackgroundMode()`（2 次）、`NControllerManager.Instance`（2 次）。

**注意 `Registered 11 migrations` 是假线索**：那是 `NMuteInBackgroundHandler` 收到 `NOTIFICATION_APPLICATION_FOCUS_OUT` 后调 `Mute()` 触发 `SaveManager.Instance` 懒加载顺带打出来的，不代表启动流程走到那一步。

| # | 问题 | 表现 | 修法 |
|---|---|---|---|
| 1 | Sentry 初始化抛 `DllNotFoundException` 中断 `_EnterTree` | 纯黑屏，无闪退 | `SentryService.Initialize()` 在 `OS.HasFeature("mobile")` 时提前返回；`NGame._EnterTree()` 里再兜一层 try-catch |
| 2 | 半初始化的 `NGame` 被下游当非空用 | 85 条 `NullReferenceException` | `NReactionWheel._Input` / `NBackgroundModeHandler.EnterBackgroundMode` / `NControllerManager.Instance` 加空值保护 |
| 3 | 非编辑器构建强制初始化 Steam | Steam 失败 → `STEAM_INIT_ERROR` 模态框挡住主菜单 | `NGame.InitializePlatform()` 把 `OS.HasFeature("mobile")` 与 `editor` 等同处理 |
| 4 | 导出包缺 14 个 `.tpsheet` | `GetMissingTexture()` 空指针 → `Fatal error. 0xC0000005` | `export_presets.cfg` 的 `include_filter` 加 `*.tpsheet, *.atlas, *.skel, *.spine` |
| 5 | 打包时缺 785 个 `.import`/artifact | 字体、光标图、GDScript remap 全找不到 | `inject_pck2.py` 注入 418 个 `.import` + 367 个 artifact（316.5 MB） |
| 6 | 手机上拿不到日志 | 只能看黑屏，没法诊断 | `debug/file_logging/enable_file_logging=true` + `user_data/accessible_from_files_app=true` |

### 拿日志（黑屏时第一步做这个）

1. 装好后先跑一次游戏，让它黑屏/闪退
2. 打开 iOS「文件」App → 「我的 iPhone」→ 找到 **Slay the Spire 2**（或 `sts2`）
3. 进去 → `logs/` → `godot.log`，长按 → 共享 → 存到「文件」或发给自己

关键判据（按出现顺序）：

```
Godot Engine v4.5.1.stable.mono.official
Metal 3.1 - Forward Mobile - Using Device #0: Apple ...   ← 渲染起来了
[INFO] [Sentry.NET] Disabled on mobile ...                ← 修复 1 生效（v3 新增）
[INFO] Steam initialization skipped ...                   ← 修复 3 生效
[INFO] AtlasManager: Loaded ui_atlas with 102 sprites     ← 资源起来了
[INFO] [Startup] Time to main menu: N,NNNms               ← 真的到主菜单了
```

**别再靠 `.NET: GodotPlugins initialized` 判断 AOT 是否加载**：`GDMono` 的这些输出走的是原生 stdout，不进 `godot.log`。iOS 上的判据是看崩溃栈里有没有 `at sts2!<BaseAddress>+0x...` —— 有就说明 AOT dylib 已被 `dlopen` 且托管代码在跑。

### 签名（自签 p12 用仓库里的 SIGN_IOS.sh）

顺序不能错：**先签 `Frameworks/sts2.dylib`，再签 `.app` 外壳**。iOS 内核对 `dlopen` 未签名/签名不匹配的 dylib 直接拒绝，Godot 侧只看到 `open_dynamic_library` 失败，表现就是黑屏。

```bash
unzip -q sts2-v4.ipa -d work
codesign -f -s "你的 identity" --entitlements work/Payload/sts2.app/sts2.entitlements   work/Payload/sts2.app/Frameworks/sts2.dylib
codesign -f -s "你的 identity" --entitlements work/Payload/sts2.app/sts2.entitlements   work/Payload/sts2.app
codesign --verify --deep --strict --verbose=2 work/Payload/sts2.app
( cd work && zip -qry ../sts2-signed.ipa Payload )
```

仓库根目录的 `SIGN_IOS.sh` 就是这套流程，用法 `./SIGN_IOS.sh "你的 identity" sts2-v4.ipa`。

### 已知仍缺失（不影响启动）

- **FMOD** → 完全没声音。`FmodManager` autoload 已移除，C# 侧零 FMOD 依赖，不会崩
- **Spine** → 骨骼动画变占位节点（174 个场景）。角色选择/商人/休息点/Boss 背景会缺动画，但节点树完整，不崩
- **Sentry** → 崩溃上报没了
