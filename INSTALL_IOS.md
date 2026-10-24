# 杀戮尖塔 2 · iOS 安装说明

## 成品

| 项目 | 值 |
|---|---|
| 文件名 | `sts2-unsigned.ipa` |
| 大小 | 1,163,700,442 字节（约 1110 MB） |
| SHA256 | `4252ec2f5252e4b5744be0b46925ce4717823abb9d8a9230feb84cb2327cc5e8` |
| Bundle ID | `com.placeholder.sts2` |
| 最低系统 | iOS 14.0 |
| 架构 | arm64（真机，模拟器不支持） |
| 签名 | **未签名**，需要自己签 |
| 游戏版本 | 0.107.1 |
| 下载 | https://github.com/HDZ123321/sts2-ios/releases/download/v0.107.1-ios/sts2-unsigned.ipa |

包内结构：

```
Payload/sts2.app/sts2                                    68,111,632   主程序（libgodot 链接产物）
Payload/sts2.app/Frameworks/sts2.dylib                  199,447,816   NativeAOT 产物（游戏逻辑）
Payload/sts2.app/Frameworks/libspine_godot.ios.template_release.framework/
  libspine_godot.ios.template_release                     1,273,248   spine-godot（骨骼动画）
Payload/sts2.app/Frameworks/fmod.ios.template_release.framework/
  fmod.ios.template_release                               4,287,144   fmod-godot（音频引擎）
Payload/sts2.app/sts2.pck                             1,898,964,392   全部游戏资源（含 12 个 FMOD bank）
Payload/sts2.app/Info.plist                                   1,547
```

三个原生库都在 `LC_LOAD_DYLIB` 里，全部必须签通：

| 库 | 导出符号 | 漏签后果 |
|---|---|---|
| `sts2.dylib` | `_godotsharp_game_main_init` | 启动即黑屏（托管入口找不到） |
| `libspine_godot.ios.template_release` | `_spine_godot_library_init` | 点「单人游戏」崩在 `NBossMapPoint._Ready()` |
| `fmod.ios.template_release` | `_fmod_library_init` | 不崩，但所有 `FmodServer` 调用变「未声明标识符」→ 静音 |

主程序的实际引用形式（`otool -L` 看到的）：

```
@rpath/sts2.dylib
@rpath/libspine_godot.ios.template_release.framework/libspine_godot.ios.template_release
@rpath/fmod.ios.template_release.framework/fmod.ios.template_release
```

`LC_RPATH` 是 `@executable_path/Frameworks`，所以三个 framework 都在
`Payload/sts2.app/Frameworks/` 下，`codesign --deep` 能一次签完。

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

只剩一项：

| 扩展 | 后果 |
|---|---|
| **Sentry** | 崩溃上报没了（v3 起在 mobile 上主动关闭，避免它把启动流程打断） |

**FMOD 音频从 v7 起已经补回** —— 12 个 bank 全在包里，进游戏就有声音和 BGM。
**Spine 骨骼动画从 v6 起已经补回**。

## 音频（v7 起可用）

v7 把 FMOD 的 iOS 二进制补齐了。做法：游戏用的就是 utopia-rise `fmod-gdextension`
6.1.0-4.5.0（`fmod.dll` / `fmodstudio.dll` / `libGodotFmod.windows.template_release.x86_64.dll`
与官方 `addons.zip` 逐字节相同），那个 release 自带 iOS 库。

两个坑，都被绕过了：

1. **install name 必须是绝对 `@rpath` 形式。** 官方 dylib 的 `LC_ID_DYLIB` 是相对路径
   `demo/addons/fmod/libs/ios/...`，hardened runtime 的 dyld 会直接拒绝。已用脚本重写成
   `@rpath/fmod.ios.template_release.framework/fmod.ios.template_release`（69 字节，
   刚好塞进 `LC_ID_DYLIB` 的 80 字节槽）。
2. **framework 的 basename 必须等于包内二进制名。** Xcode 从 `.framework` 名字推导
   `-framework X`，于是 ld 去找 `X.framework/X`。所以整包必须叫
   `fmod.ios.template_release.framework`、里面的可执行文件也必须叫 `fmod.ios.template_release`。

`banks/desktop/` 下 12 个 bank（308 MB）通过 `export_presets.cfg` 的
`include_filter="*.tpsheet, *.atlas, *.skel, *.spine, *.bank"` 进 PCK —— 它们没有 importer，
不在资源依赖图里，不加过滤器会被静默丢掉。

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
| 进主菜单但点单人游戏闪退 | `Frameworks/libspine_godot.ios.template_release.framework` 没签。v6 起主程序 `LC_LOAD_DYLIB` 直接引用它，必须跟 `sts2.dylib` 一起签 |
| 进游戏但完全没声音 | `Frameworks/fmod.ios.template_release.framework` 没签。不崩，只是所有 `FmodServer` 调用变「未声明标识符」 |
| 卡在黑屏 | PCK 没被找到。确认 `sts2.pck` 与 `sts2` 在同一个 `sts2.app/` 目录下 |
| 日志里 `dlopen(...relative path not allowed in hardened program)` | 某个 framework 的 `LC_ID_DYLIB` 还是相对路径。v7 的三个库都已是 `@rpath/...` 形式 |
| 提示 "Unable to install" | Bundle ID 冲突，改一个（Sideloadly 里可以改） |
| 提示未受信任的开发者 | 设置 → 通用 → VPN与设备管理 → 信任 |

## 构建来源

GitHub Actions 在 `macos-14` runner 上完整跑通两条线：

**游戏本体**（`.github/workflows/ios.yml`）：`dotnet publish -r ios-arm64`（NativeAOT）→ `xcodebuild -create-xcframework` → 注入 pbxproj → `xcodebuild archive`（无签名）→ 手工组 Payload → zip。

**Spine 扩展**（`.github/workflows/spine-ios.yml`）：`git clone spine-runtimes@4.2` + `godot-cpp@4.5` → `scons platform=ios target=template_release arch=arm64` → 校验 `_spine_godot_library_init` 与 7 个导出类 → zip 发布到 tag `spine-godot-ios`。

仓库：https://github.com/HDZ123321/sts2-ios

---

## 当前版本（v7）：补回 FMOD，声音回来了

v7 在 v6 基础上只做了一件事：把 FMOD 的 iOS 二进制补齐，让游戏有声音和 BGM。
详细做法见上面「音频（v7 起可用）」一节。

判据（进游戏后）：

- 主菜单有 BGM
- 点按钮有 `event:/sfx/ui/clicks/...` 的音效
- 日志里不再出现 `Parse Error: Identifier "FmodServer" not declared`
- 日志里不再出现 `Failed to instantiate an autoload, script 'res://addons/fmod/FmodManager.gd'`

---

## v6：补回 Spine，单人游戏不再崩

v5 已经能进主菜单，但点「单人游戏」会崩：

```
ERROR: Cannot get class 'SpineSprite'.  at: _instantiate_internal (core/object/class_db.cpp:546)
ERROR: System.InvalidCastException: Specified cast is not valid.
   at MegaCrit.Sts2.Core.Nodes.Screens.Map.NBossMapPoint._Ready() + 0x148
C# backtrace:
   [6] GodotTreeExtensions.AddChildSafely(Node, Node)
   [7] NMapScreen.SetMap(ActMap, uint, bool)
   [8] RunManager+<GenerateMap>d__177.MoveNext()

[ERROR] Exception starting singleplayer run : System.NullReferenceException
   at MegaCrit.Sts2.Core.Nodes.Screens.Map.NBossMapPoint.RefreshColorInstantly() + 0xa0
   at NMapScreen.RefreshAllPointVisuals() + 0xb8
   at RunManager.<SetActInternal>d__197.MoveNext() + 0x10c
   at NGame.<StartRun>d__155.MoveNext() + 0x230
```

### 为什么

`NBossMapPoint.cs:178`：

```csharp
_spineSprite = GetNode<Node2D>("%SpineSprite");
```

场景里这个节点是 `type="SpineSprite"`。v5 包剔除了 spine gdextension，Godot 在 `class_db.cpp:546` 找不到类，就**创建一个普通 `Node` 当占位符**，于是 `GetNode<Node2D>()` 转型失败 → `_Ready()` 中途中断 → `_placeholderImage` 没赋值 → 后面 `RefreshColorInstantly()` 空引用。

同样的模式遍布全项目：`scenes/` 里 `SpineSprite 197 / SpineMesh2D 240 / SpineBoneNode 119 / SpineSlotNode 106`，39 处 `GetNode<T>` 会因为占位 Node 而转型失败。

### v6 的改法

用 GitHub Actions 自己编了 spine-godot 的 iOS framework（官方 `EsotericSoftware/spine-runtimes` **不发预编译二进制**，只能自编）：

- `spine-runtimes` **4.2 分支** —— 游戏 `.spskel` 文件头是 `4.2.03`，与 `spine-cpp/include/spine/Version.h` 的 `SPINE_VERSION_STRING "4.2"` 对齐
- `godot-cpp` **4.5 分支** —— 与引擎 Godot 4.5.1 对齐
- `macos-14` runner + `scons platform=ios target=template_release arch=arm64`
- 产物 `libspine_godot.ios.template_release.framework`，1,273,248 字节，`Mach-O 64-bit dynamically linked shared library arm64`，导出 `_spine_godot_library_init`

workflow 在 `.github/workflows/spine-ios.yml`，Release tag `spine-godot-ios` 存二进制。

framework 放进 `addons/spine/ios/` 后，Godot 的 iOS 导出器自动把它拷到 `sts2.app/Frameworks/` 并加进 Xcode 工程的 `Embed Frameworks` phase（带 `CodeSignOnCopy`），`spine_godot_extension.gdextension` 里的 `ios.release` 条目本来就有，不用改。

主程序现在有两条 `LC_LOAD_DYLIB`：

```
@rpath/sts2.dylib
@rpath/libspine_godot.ios.template_release.framework/libspine_godot.ios.template_release
```

**这意味着 spine framework 和 `sts2.dylib` 一样必须签通。** 漏签不是「动画不动」，是 dyld 启动就失败。

### v6 关键判据

装上后点单人游戏，日志里应该看到：

```
[INFO] [Startup] Time to main menu: 4,xxx ms
```

然后是地图。如果还看到 `Cannot get class 'SpineSprite'`，说明装的是 v5 或更早的包。

### 签名（v6 起多了一个 framework）

```bash
unzip -q sts2-unsigned.ipa -d work
APP=work/Payload/sts2.app

# 先签所有 framework 里的二进制
for f in "$APP"/Frameworks/*; do
  [ -e "$f" ] && codesign -f -s "你的 identity" --timestamp=none "$f"
done

# 再签 app 外壳
codesign -f -s "你的 identity" --timestamp=none "$APP"

codesign --verify --deep --strict --verbose=2 "$APP"
```

仓库里的 `SIGN_IOS.sh` 已经是这个逻辑（它会遍历 `Frameworks/*`，所以自动覆盖 spine）：

```bash
./SIGN_IOS.sh "你的 identity" sts2-unsigned.ipa
```

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
