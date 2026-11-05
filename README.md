# 杀戮尖塔 2 · iOS 构建包

从 PC 版 `Slay the Spire 2`（v0.107.1，Godot 4.5.1 mono / .NET 9）反编译并重建的可移植工程，
面向 iOS 14+ 真机。**在 Windows 上能做完的部分已经全部做完**，剩下的必须 macOS + Xcode。

---

## 1. 这个目录里有什么

| 路径 | 大小 | 说明 |
|---|---|---|
| `sts2.pck` | 1.51 GB | 完整资源包，15703 条目。纹理已同时含 **ETC2（539 个）+ ASTC（683 个）**，iOS 直接可用 |
| `sts2.xcframework/` | 355 MB | Godot 引擎本体 `libgodot.a`（arm64 + simulator） |
| `MoltenVK.xcframework/` | 48 MB | Vulkan → Metal 转译层（Godot 的 Mobile/Forward+ 渲染器走这条） |
| `sts2.xcodeproj/` | 21 KB | Xcode 工程，已配好 `arm64` / `iOS 14.0` / 横屏 / 内嵌 PCK |
| `sts2/` | 294 KB | app 外壳：Info.plist、entitlements、启动图、dummy 入口 |
| `BUILD_MACOS.sh` | 4 KB | 一键构建脚本（6 步） |
| `.github/workflows/ios.yml` | 6 KB | GitHub Actions `macos-14` 自动出未签名 IPA |
| `tools/patch_pbxproj.py` | 4 KB | 把 `sts2_aot.xcframework` 接进 Xcode 工程 |

**没有的东西**：`sts2_aot.xcframework` —— 它是 `dotnet publish -r ios-arm64` 的 NativeAOT 产物，
必须在 macOS 上编译。这正是需要 Mac 的唯一原因。

---

## 2. 关键结论（先看这段，省得踩坑）

### 2.1 iOS 上 .NET 是走 NativeAOT，不是 Mono
Godot 4.2 起官方支持 C# 导出 iOS（标记 experimental）。Godot.NET.Sdk 4.5.1 在
`RuntimeIdentifier` 以 `ios` 开头时会自动 import `iOSNativeAOT.props`：

```
PublishAot                = true
PublishAotUsingRuntimePack= true
UseNativeAOTRuntime       = true
TrimmerSingleWarn         = false
```

C# 代码被 ILC 编译成一个 **`libsts2.dylib`**，再打成 xcframework 嵌进 app。
所以 `libgodot.a` 里搜不到任何 mono 运行时符号 —— 那是正常的。

### 2.2 三个必须的工程修补（源码里已包含，别删）
1. `src/_synthetic/AssemblyHasScripts.cs` —— `[assembly: AssemblyHasScripts]`。
   关掉了 Godot 源生成器，这个 attribute 就没人生成，缺了它 `ScriptManagerBridge.LookupScriptsInAssembly()`
   直接 return，所有 C# 节点变成空 Node。
2. `src/_synthetic/{InlineArray*,ReadOnlyArray,ReadOnlyList,ReadOnlySingleElementList}.cs` ——
   ilspy 导出的编译器合成类型，缺了报 1300+ 个 `CS0400`。
3. `src/_synthetic/PrivateImplementationDetails.cs` —— switch 表达式异常辅助类，13 处调用点。

### 2.3 `beforefieldinit` 必须回填（本项目最隐蔽的坑）
ilspy 把「静态字段初始化器」还原成显式 `static Xxx()`，重新编译后类型失去 `beforefieldinit`。
后果：`ModelDb.Init()` 按 `_subtypes` 顺序逐个 `Activator.CreateInstance`，
`Darv`（index 761）的 cctor 提前触发，而它要求 `Astrolabe`（index 1333）已在字典里 →
`KeyNotFoundException: 'RELIC.ASTROLABE'`，游戏卡在启动画面。

**已修**：`tools/BfiPatch/`（Mono.Cecil）在编译后把这 7 个类型的标志打回去，
`sts2.csproj` 里挂成 `AfterTargets="Build;CoreCompile"` + `BeforeTargets="IlcCompile"`，全自动。
清单在 `bfi_types.txt`。校验方法：`bfi+cctor` 应等于 **2227**（与原版一致）。

### 2.4 纹理格式：PCK 里原本只有 S3TC/BPTC，iOS 用不了
原版导出给 Windows，纹理只有 `s3tc`/`bptc` 变体。iOS 的 GPU 不支持 BC 格式，
Godot 会运行时 `image->decompress()` 转 RGBA8 —— 不崩，但 1.86 GP 纹理会膨胀到约 **3.5 GB 显存**。

**已修**：把 3485 个纹理从 ctex 反解回 PNG（`tex_png/`），重写 `.import` 为标准
`[params] compress/mode=2` 格式，让 Godot 重新导入，产出四套变体：

```
high_quality=false → s3tc + etc2
high_quality=true  → bptc + astc
```

iOS preset 的 feature 集合是 `{etc2, astc, arm64}`（见
`editor/export/editor_export_platform_apple_embedded.cpp:51-53`），导出器会按 feature 自动选对的那套。
现在 PCK 里 ETC2 539 个（220 MB）+ ASTC 683 个（702 MB）。

### 2.5 导出器会丢弃「源文件不存在」的 `.import`
`.import` / `.remap` 只有在源文件存在时才被打包。原 PCK 剥掉了源图，
导致 418 个 `.import` 被静默丢弃，连带 367 个 artifact（169 `.spatlas` + 163 `.spskel` + 17 `.fontdata`…，316 MB）。
症状是 `Resource file not found: res://fonts/kreon_bold.ttf`。

**已修**：`tools/inject_pck.py` 直接往 PCK 二进制里注入这 785 个条目。
现在挂载验证：`texture total=3557 ok=3557 bad=0`。

### 2.6 触屏
`InputEventScreenTouch` 在原代码里 **0 命中**，全靠 Godot 默认的 `emulate_mouse_from_touch`（单指 → 鼠标左键）。
但「右键看详情」和「ESC 返回」在 iOS 上没有触发方式，所以加了 `src/Core/Touch/TouchInputAdapter.cs`：

| 手势 | 映射 |
|---|---|
| 单指长按 550ms（位移 < 14px） | 右键按下 + 抬起 → 打开卡牌/遗物详情 |
| 双指轻点（260ms 内） | `ui_cancel` → 关闭面板 / 返回 |
| 其余 | 放行，交给引擎的触摸转鼠标 |

已注册为 autoload（`project.godot` 第 35 行）。

### 2.7 外部组件（哪些进了 App，哪些还是降级）
| 组件 | 状态 | 处理 |
|---|---|---|
| **Spine** | **已恢复**（v6 起） | `spine-godot` framework 随 `sts2-frameworks.zip` 一起发并链接进 App；C# 侧本来就是字符串反射，缺了只是动画降级，不崩 |
| **FMOD** | **已恢复**（v7 起） | fmod iOS framework 随 frameworks.zip 发；`LC_LOAD_DYLIB` 改 `LC_LOAD_WEAK_DYLIB` 指向 `@rpath/fmod-disabled.framework/fmod-disabled`（`tools/disable_dylib_path.py`），再把 flat-namespace 的 30 个 import 改弱绑定（`tools/weaken_fmod_bind.py`，bind opcode `0x40→0x41`）。framework 缺失或版本不匹配时是静音，不是启动即 dyld 崩 |
| **Sentry** | 仍然关闭 | `SentryService` 在 `OS.HasFeature("mobile")` 时直接 return——原生 sentry 库不在这个 build 里 |
| **Steam** | 无 iOS SDK | 走 `Platform.Null` 分支，单机可用 |

`N_WEAK_REF` 那条路走不通：dyld4 只认 bind opcode 的弱绑定位，改 Mach-O 符号表没用——这是当时连续几次启动崩溃的根因。

### 2.8 卡图替换：两套 Regent 立绘，启动时选一套
储君（Regent）的卡图有两条上游 mod，都是 Harmony DLL，AOT 里跑不了，所以由工具把美术从它们的 PCK 里抽出来重新烤成两个独立包，跟着 App 一起发：

| 包 | 上游 mod | 卡数 | 包内命名空间 | 重烤工具 |
|---|---|---|---|---|
| `res/RegentArt.pck` | RegentCardsAnimeRework 1.0（动漫风） | 94 | `res://generated/assets/card_art/` | `tools/regent/build_regent_art.py` |
| `res/RegentFemArt.pck` | RegentFemPortraits 1.0（娘化，Regent 760） | 60 | `res://RegentFemPortraits/card_portraits/` | `tools/regent/build_regent_fem_art.py` |

两个包各自的 `res://` 命名空间不重叠，所以能同时躺在 bundle 里；`src/Core/Modding/RegentCardArt.cs` 在 `OneTimeInitialization.ExecuteVeryEarly` 里一次挂载，`CardModel.PortraitPath` 按 CLR 全名查当前那套的表，没覆盖到的卡继续用原版图集精灵。

切换靠 `user://regent_art.txt`（iOS 上就是 App 的「文稿」目录，用「文件」App 可改）。第一行 `fem`（默认）或 `anime`，改完重启生效；文件不存在、内容不认识、或者对应的包没打进 bundle，都会退回默认那套。

上游包里那两份 `res://.godot/uid_cache.bin` / `global_script_class_cache.cfg` 必须在烤包时丢掉——挂载带它们的包会让引擎从包里重读 UID 缓存，把本体的 `uid://` 引用全部盖掉。

### 2.9 手机上的发热与帧率：一次性的省电档

抓日志看到的现象是这台机器在 120 Hz 屏上跑满帧、机身发烫。原因不在 C# 每帧逻辑（全工程的 `_Process` 方法体都扫过，最长的 1.5 KB，`_PhysicsProcess` 一个都没有），而在**渲染带宽**，且历史构建从来没动过 steady-state 帧循环。三处改动：

| 改动 | 位置 | 为什么 |
|---|---|---|
| **MSAA 关掉**（`Msaa: 2 → 0`） | `src/Core/Helpers/MobileGraphicsProfile.cs` | 2D 渲染器开 MSAA 只是把采样带宽翻倍，没有可见收益。原来 `SettingsSaveManager.ApplyPlatformDefaults` 只在**新建存档**时设 `Msaa=2`，老存档永远带着它 |
| **帧率钳到 [30, 60]** | 同上 + `NFpsPaginator` | 设置界面的档位到 500，ProMotion 会真的去追；`Engine.MaxFps` 在 iOS 上经 swapchain 生效，不是空设 |
| **VSync 固定 On** | 同上 | 原来是 `Adaptive`，帧时抖动时白烧功耗 |

`ApplyOnce` 写在 `user://mobile_profile_v1.txt` 里的标记只写一次：**升级后第一次启动改一次，之后玩家自己把 MSAA 开回去就不会再被覆盖**。想重跑就把那个文件删掉。

#### 可选：按设计画布渲染（默认关）

`user://render_scale.txt` 第一行写 `design`（或 `canvas`，或任意 `0 < x < 1` 的浮点数），重启后从 `canvas_items` 切到 `viewport` 拉伸，**渲染目标就是 1920×1080 设计画布，再双线性放大到屏幕**；iPhone 2796×1290 上相当于 0.70× 像素。

要点：`canvas_items` 下怎么设 `ContentScaleSize` 都不省像素——`Window::_update_viewport_size()` 里 `final_size = screen_size`，永远物理分辨率；只有 `viewport` 模式才 `final_size = (viewport_size / content_scale_factor).floor()`。切过去之后 **canvas 坐标系不变**（两边都是 `viewport_size`），所以布局和触摸目标一动不动，代价只是 1.19× 的轻微软化。写 `1` 或删文件即回原样。

#### 怎么量

手机上 `Perf` 是自动开的（`MobileDevice.IsMobile` 即启用），每 1800 帧（约 30 秒）打一行：

```
[Perf] frames=1800 avg=8.31ms fps=120.4 max=19.7ms hitch=0 proc=1.92ms phys=0.00ms draw=84 obj=612 nodes=2174 gc0=0 gc1=0 gc2=0 managed=61MB vram=214MB
```

`proc`（C# 每帧时间）远小于 `avg` 就是 GPU 受限；`draw`/`obj` 高则先砍绘制批次。想看更密的报表用 `--perf` 启动（600 帧一行），想彻底关掉用 `--noperf`。桌面端默认不开，`--perf` 才开。

#### 卡顿归因：`hitch` 行带阶段标签

帧报表只说了「有几次长帧」，说不出**长在哪**。所以超过 `HitchThresholdMs`（33 ms）的帧会单独打一行，带上当前阶段标签（每个报表窗口最多 12 行，总量仍由 `hitch=` 计）：

```
[Perf] hitch 187.4ms phase=combat.enemies scene=NCombatRoom proc=41.2ms nodes=2310 draw=126 vram=268MB managed=64MB
```

`phase` 是 `Perf.Phase("...")` 设的短标签，战斗进入这条链上埋了这些点：

| 阶段 | 覆盖的工作 |
|---|---|
| `combat.generate` | 生成怪物与站位 |
| `combat.preload` | `PreloadManager.LoadRoomCombatAssets` 的等待 |
| `combat.creatures` | `CombatState.CreateCreature` × N |
| `combat.create-room` | `combat_room.tscn` 实例化 |
| `combat.room-ready` / `.allies` / `.enemies` / `.room-ready.done` | `NCombatRoom._Ready` 的各段 |
| `combat.creature-node` | 每个 `NCreature` 节点创建 |
| `combat.populate` / `register-creatures` | 玩家战斗状态、手牌进 DB |
| `combat.combat-setup-hooks` / `ui-activate` / `background` | UI 激活、能量球、场景背景 |
| `combat.bgm` / `after-creature-added` | 战斗音乐（FMOD bank）、每只怪的入场 |

判读：`proc` 小、`draw`/`vram` 跳高 → GPU 侧（纹理上传或首次 PSO）；`proc` 大 → 主线程同步干活（实例化、Spine 建骨、卡面解码）。

另有两个不在调用链上、但仍会出现在进战后几帧里的标签：`spine.ready`（某只怪的 SpineSprite 把骨架建完、图集页上传完的那一帧，之前是空白）和 `combat.banner`（「战斗开始」横幅的 `MegaLabel` 自适应字号二分重排）。

`hitch` 只在某一帧真的超过 33 ms 时才有输出，而进战卡顿常常是**一串**二十几毫秒的帧叠出来的。所以进战这条链跑完时还会多打一行阶段总账（`Perf.ReportPhases`，在 `CombatRoom.StartCombat` 末尾和 `StartPreFinishedCombat` 末尾各调一次）：

```
[Perf] combat entry total=1120ms combat.generate=3ms combat.preload=812ms combat.creatures=44ms combat.create-room=171ms combat.room-ready=48ms combat.creature-node=12ms combat.creature-node=9ms combat.creature-node=11ms combat.ui-activate=26ms combat.background=31ms combat.bgm=4ms combat.after-creature-added=18ms
```

读法：`total` 是整段真实墙钟（含 await 的空帧），每项是**两个标签之间**的时间。`combat.preload` 里包含后台加载让出的帧，所以它大不代表卡；真正要看的是**同一帧里干完的那几项**——`creatures` / `create-room` / `room-ready` / 重复出现的 `creature-node` 加起来接近某次 `hitch` 的毫秒数，就是它。这样即使一帧都没超过 33 ms，也能看出时间被谁吃了。

已经因此改掉的两处（同一提交）：

| 改动 | 位置 | 为什么 |
|---|---|---|
| 房间加载后的全量 `GC.Collect()` 在移动端改成不等结果 | `src/Core/Assets/PreloadManager.cs` `CollectIfHeapGrew` | 它就落在进房间的转场帧上，5–20 ms 的停顿正好被玩家看见。`GC.Collect(2, Forced, blocking: false)` 照样回收，只是不在绘制那一帧上等 |
| 意图动画帧并入 run 资产集 | `PreloadManager.GetRunAssetPaths` + `IntentAnimData.AssetPaths` | `AbstractIntent.AssetPaths` 只报了静态图标，动画帧每次进战全部 miss，走 `AssetCache` 的同步 `ResourceLoader.Load` |
| 自适应字号的拟合结果做记忆化 | `addons/mega_text/MegaLabel.cs` `_fittedSizes` | 拟合是二分搜索，每一步都把整段字重新 shaping，中文尤其贵。战斗开始时血条、名牌、意图、横幅一起出现，几十个新 label 问的是同一批 (文本, 字体, 矩形)，缓存命中后一次 shaping 都不做 |

### 2.10 灵动岛：跑图状态挂在 Live Activity 上

iPhone 14 Pro 以后的机型（含 17 Pro Max）在 App 外能看到一条「第 X 幕 第 Y 层 · HP 32/80」的实时活动。实现刻意避开两处最容易翻车的地方：

**C# 不直接调 Swift。** NativeAOT 镜像里 `DllImport` 打到主镜像的 Swift 符号，这条路在本机完全无法验证（成了就是成了，不成就整个启动段崩），所以走文件通道：

```
C#  DynamicIslandReporter  每秒（只在内容变化时）写 user://island_state.json
                                 ↓  0.5 s 轮询
Swift Sts2IslandBridge     读文件 → Activity.update() / Activity.request()
```

写失败只丢灵动岛，一帧都不影响。文件内容是一行 JSON：`{"seq":12,"active":true,"hp":32,"maxHp":80,"floor":7,"act":2,"gold":143,"character":"铁甲战士","avatar":"island_avatar.png"}`；不在跑图时 `active` 为 false，扩展收到就把活动结束掉。

**角色 2D 形象跟着状态一起传。** 扩展进程有自己的沙盒，读不到 Godot 的 `user://`；这个构建又是 ad-hoc 签名，拿不到 App Group 去共享容器，所以图片只能塞进 `ContentState.avatar`（`Data`）走系统通道：

```
IslandAvatar.Ensure(角色)   角色变了才编码：取 char_select 立绘（回落 top_panel 图标）
                            → 解压 → 长边缩到 132 px RGBA8 → user://island_avatar.png
Swift Bridge                读字节（按 size+mtime 缓存）→ ContentState.avatar
Widget IslandAvatarView     Image(uiImage:) 圆环裁切；缺失就退回落一个 figure.stand 符号
```

132 px 是「44 pt × 3x」；单张 PNG 实测在 10–40 KB 区间，超过 48 KB 会掉到 96 px、再掉到 72 px，还超就不发（图标兜底）—— 因为 content state 每次更新都要归档并投递给扩展进程。展开态左侧、紧凑态前区、极简态、锁屏左侧都用这张图。

**扩展不建 Xcode target。** `tools/build_island_extension.py` 用 `swiftc` 直接编出 `.appex` 塞进 `sts2.app/PlugIns/`，版本号从主 app 的 `Info.plist` 回填。值得记的三件事：

| 细节 | 为什么 |
|---|---|
| `-Xlinker -e -Xlinker _NSExtensionMain` | 扩展入口不是 `main()`；不指定的话系统启动扩展时立刻崩 |
| `-Xlinker -application_extension` | 缺 `MH_APP_EXTENSION` 标记，系统以 "not a valid extension" 拒掉整个 bundle |
| `IPHONEOS_DEPLOYMENT_TARGET` 保持 14.0，但 `-weak_framework ActivityKit` + `-Xfrontend -disable-autolink-framework -Xfrontend ActivityKit` | ActivityKit 是 16.1 才有的库。强链接会让 dyld 在旧系统上拒绝启动 App；Swift 的 autolink 会把它链接成强引用，所以必须从两头掐掉。代价只是旧系统没有灵动岛 |

app 侧因此必须开 Swift：`tools/patch_app_sources.py` 往 `project.pbxproj` 里补三个源文件、`SWIFT_VERSION = 5.0`、上面那两个链接设置（跑两遍结果一样）。`sts2/dummy.cpp` 的 `godot_apple_embedded_plugins_initialize()` 与 `Sts2IslandBootstrap.m` 的 `+load` 各触发一次 `sts2_island_bootstrap()`，两处都幂等。

排查时先看 `user://island_status.txt`（和日志同目录）：扩展侧拿不到授权、请求失败、活动结束都会在那里留一行带时间戳的记录，同时进 `NSLog`。

### 2.11 iPhone 17 Pro Max（6.9"，1320×2868）

这块屏不需要单独的代码路径——布局全走 canvas 坐标系——但有三件事值得写下来：

- **横向安全区**：灵动岛在横屏时压住一侧，`DisplayServer.GetDisplaySafeArea()` 报的 insets 会非零，`NGame.ApplySafeArea()` 已经按 `uniformScale` 换算成 canvas 单位并缩进 UI。启动日志里那行 `[Display] model=... safeInsetsPx=... safeInsetsPt=...` 就是这件事的自证，换机型后先看它。
- **帧率**：这块 120 Hz 屏正是 §2.9 那套省电档的目标，`Engine.MaxFps` 被钳在 60，MSAA 关掉。
- **分辨率**：原生渲染是 3.8 MPx。想要更省电就在 `user://render_scale.txt` 写 `design`（见 §2.9），画面从 1920×1080 放大，布局与触摸目标不动。

---

## 3. 构建方式 A：本地 Mac

```bash
# 0. 准备
export TEAM_ID=ABCDE12XYZ              # 你的 Apple Developer Team ID（10 位）
export BUNDLE_ID=com.yourname.sts2     # 反向域名
cd /path/to/build_ios5

# 1-6. 跑脚本（内部依次做 publish / xcframework / patch pbxproj / archive / export）
chmod +x BUILD_MACOS.sh
./BUILD_MACOS.sh
```

产出 `build/ipa/sts2.ipa`。

**前提**：
- Xcode 15+（`xcode-select -p` 指向它）
- .NET 9 SDK
- 如果 `TEAM_ID` 留空会走未签名分支，只出 `.xcarchive`

---

## 4. 构建方式 B：GitHub Actions（无需自备 Mac）

把 `ci_bundle/` 的内容推到 GitHub 仓库，`.github/workflows/ios.yml` 会在 `macos-14` 上跑完全流程，
产出未签名 IPA 作为 artifact。

### 4.1 `sts2.pck` 怎么进仓库
1.51 GB 超过 GitHub 单文件 100 MB 限制，两条路：

**路线 1 — Git LFS**
```bash
git lfs install
git lfs track "*.pck"
git add .gitattributes sts2.pck
git commit -m "add pck via lfs"
git push
```
workflow 里已经开了 `with: lfs: true`。

**路线 2 — Release 资产**（更省 LFS 配额）
把 `sts2.pck` 传到一个 Release，然后在 workflow 的 `Verify PCK present` 步骤前插一段：
```yaml
      - name: Download PCK
        run: |
          curl -L -o sts2.pck \
            https://github.com/<owner>/<repo>/releases/download/v0.107.1-ios/sts2.pck
```

### 4.2 仓库结构
```
.
├── .github/workflows/ios.yml
├── sts2.csproj          # Godot.NET.Sdk/4.5.1
├── sts2.sln
├── bfi_types.txt
├── nuget.config
├── nuget_local/         # Godot.NET.Sdk / GodotSharp / Godot.SourceGenerators nupkg
├── libs/                # 12 个引用 DLL（Sentry/Steamworks.NET/0Harmony/SmartFormat/…）
├── src/                 # 3420 个 .cs（含 src/_synthetic/ 与 src/Core/Touch/）
├── addons/mega_text/    # 4 个 .cs
├── RiderTestRunner/     # 2 个 .cs
├── tools/BfiPatch/      # beforefieldinit 回填工具
├── tools/patch_pbxproj.py
├── tools/patch_app_sources.py          # 把灵动岛的 Swift/ObjC 挂进 app target
├── tools/build_island_extension.py     # swiftc 手工组装 .appex
├── sts2.xcodeproj/
├── sts2/                # 导出模板 + island/（灵动岛源码与扩展 Info.plist）
├── PrivacyInfo.xcprivacy
└── sts2.pck
```

`libs/` 里的 DLL 来自正版游戏安装目录 `data_sts2_windows_x86_64/`，不随仓库分发。

---

## 5. 装到 iPhone / iPad

拿到 `sts2-unsigned.ipa` 后，按 iOS 版本选：

| 工具 | 适用 | 说明 |
|---|---|---|
| **Sideloadly** | iOS 任意版本 | 用你的 Apple ID 免费签名，**7 天有效**，到期重签；需开「开发者模式」 |
| **AltStore / SideStore** | iOS 14+ | 同上，但支持自动续签（SideStore 无需常连电脑） |
| **TrollStore** | iOS 14.0–16.6.1 / 17.0 | 永久签名，无需账号，最省事 |
| **付费开发者账号** | 任意 | 证书 1 年有效，可用 `BUILD_MACOS.sh` 直接出签名版 |

**必读**：
- 游戏是横屏，`UISupportedInterfaceOrientations` 只有 `LandscapeLeft`（iPad 只有 `LandscapeRight`）
- 首次启动需要几十秒建缓存（要解压 PCK 里的纹理）
- 存档路径 `user://`，iOS 上在 app 沙盒的 `Library/Application Support/Godot/app_userdata/SlayTheSpire2/`

---

## 6. 已知限制

1. **音效取决于 framework 落位** —— FMOD framework 没进 bundle 时静音（弱链接，不崩）
2. **Spine 动画同理** —— framework 缺失只是退化到静态帧
3. **无多人联机** —— Steam 传输层不可用，单机全功能
4. **模拟器跑不了** —— 需要 `compatibility` 渲染器；当前工程用默认 `forward_plus`。
   真机 arm64 没问题
5. **首次加载慢** —— 1.5 GB PCK + 解压纹理，A15 上约 20–40 秒
6. **`audio_manager_proxy.gd` 报 Parse error** —— `.gdc` 字节码重新加载的问题，
   该脚本只服务 FMOD，不影响 C# 主流程
7. **灵动岛要 iOS 16.1+** —— ActivityKit 的下限；旧系统上 App 正常跑（弱链接），
   只是没有实时活动。另外必须在「设置 → 杀戮尖塔 2 → 实时活动」里允许，
   扩展侧被拒时会在 `user://island_status.txt` 里写明原因

---

## 7. 版本与来源

| 项 | 值 |
|---|---|
| 游戏版本 | v0.107.1 / commit 59260271 |
| 引擎 | Godot 4.5.1-stable mono（MegaDot 定制版） |
| 运行时 | .NET 9.0.7 → NativeAOT |
| 反编译 | ilspycmd 10.1.0.8386，3425 个 .cs，无混淆 |
| PCK 格式 | packver 3, flags 0x2, file_base 112, 无加密 |
| 桌面验证 | `Time to main menu: 4,479ms`，`Cannot instantiate = 0` |
| 纹理校验 | `texture total=3557 ok=3557 bad=0` |
