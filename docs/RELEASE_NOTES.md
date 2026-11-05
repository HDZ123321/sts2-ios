<!-- 这个文件由 tools/refresh_release_notes.py 渲染成 Release 说明；
     每次 CI 构建完成后自动回填 {{...}}，请不要在 GitHub 网页上直接改正文。 -->

# 杀戮尖塔 2 · iOS 安装说明

## 成品

| 项目 | 值 |
|---|---|
| 文件名 | `sts2-unsigned.ipa` |
| 大小 | {{SIZE}} |
| SHA256 | `{{SHA256}}` |
| MD5 | `{{MD5}}` |
| Bundle ID | `com.placeholder.sts2` |
| 最低系统 | iOS 14.0（灵动岛要 16.1+） |
| 架构 | arm64 真机（模拟器不支持） |
| 签名 | **未签名**，需要自己签 |
| 游戏版本 | 0.107.1 |
| 源码 commit | `{{COMMIT}}` |
| 构建 | GitHub Actions run `{{RUN}}`（macos-14） |
| 下载 | {{DOWNLOAD_URL}} |

## 包内结构

```
Payload/sts2.app/sts2                    {{SIZE_STS2}}   主程序（libgodot 链接产物）
Payload/sts2.app/Frameworks/sts2.dylib   {{SIZE_DYLIB}}   NativeAOT 产物（游戏逻辑）
Payload/sts2.app/Frameworks/libswift_Concurrency.dylib      7,741,808   Swift 并发运行时（back-deploy）
Payload/sts2.app/Frameworks/libspine_godot.ios.template_release.framework/
  libspine_godot.ios.template_release                       1,273,248   spine-godot（骨骼动画）
Payload/sts2.app/Frameworks/fmod.ios.template_release.framework/
  fmod.ios.template_release                                 4,287,144   fmod-godot（音频引擎）
Payload/sts2.app/PlugIns/Sts2Island.appex/Sts2Island   {{SIZE_APPEX}}   灵动岛 Live Activity 扩展
Payload/sts2.app/PlugIns/Sts2Island.appex/Info.plist            1,132   com.placeholder.sts2.island
Payload/sts2.app/sts2.pck                {{SIZE_PCK}}   全部游戏资源（含 12 个 FMOD bank）
Payload/sts2.app/Watcher.pck                                 18,610,100   Watcher 角色资源
Payload/sts2.app/RegentArt.pck                               34,585,560   Regent 动漫立绘
Payload/sts2.app/RegentFemArt.pck                            62,555,600   Regent 女性立绘
Payload/sts2.app/release_info.json                                 167   构建溯源
```

三个原生库都被 `LC_LOAD_DYLIB` / `LC_LOAD_WEAK_DYLIB` 直接引用，**必须一起签通**：

| 库 | 导出符号 | 漏签后果 |
|---|---|---|
| `sts2.dylib` | `_godotsharp_game_main_init` | 启动黑屏（托管入口找不到） |
| `libspine_godot.ios.template_release` | `_spine_godot_library_init` | 点「单人游戏」崩在 `NBossMapPoint._Ready()` |
| `fmod.ios.template_release` | `_fmod_library_init` | 不崩，但所有 `FmodServer` 调用变「未声明标识符」→ 静音 |

主程序里 fmod 那条是**弱链接**且指向不存在的路径（`@rpath/fmod-disabled.framework/fmod-disabled`），真正的库由 Godot 的 `GDExtensionManager` 在日志系统就绪后 `dlopen` —— 加载失败只留痕，不会在 `main()` 之前静默 abort。

`LC_RPATH` 是 `@executable_path/Frameworks`，所以三个 framework 都在 `Payload/sts2.app/Frameworks/` 下，`codesign --deep` 能一次签完；`PlugIns/Sts2Island.appex` 同样要签，否则灵动岛不显示（游戏本体不受影响）。

## 这一版（v21）：角色立绘上岛 + 进战预热一路前置

### 灵动岛显示角色 2D 形象

灵动岛的展开态左侧、紧凑左侧、最小态、锁屏左侧现在都是**当前角色的立绘**（圆形裁切 + 血量配色描边），没有图时回退成一个小人符号。

立绘不是扩展自己去读的——`.appex` 是独立沙盒，读不到 Godot 的 `user://`，而自签名没有 App Group 可以共享目录。所以走的是唯一能跨进程的通道：**Live Activity 的 content state**。

```
IslandAvatar.Ensure()  →  user://island_avatar.png（132px，RGBA8，≤48 KB）
                       →  island_state.json 里写文件名
Sts2IslandBridge       →  按 size+mtime 缓存读盘，塞进 ContentState.avatar
Sts2IslandWidget       →  UIImage(data:) → 圆形 Image
```

每个角色只编码一次（换角色才重编码），`48 KB / 72–132 px` 之间会自动降级；实在编不出来就退回符号，不影响灵动岛的其他内容。

### 进战卡顿：先归因，再修

之前只知道「刚开始打怪会卡一下」，但帧报表说不出卡在哪。现在超过 33 ms 的帧会单独打一行带阶段标签：

```
[Perf] hitch 187.4ms phase=combat.enemies scene=NCombatRoom proc=41.2ms nodes=2310 draw=126 vram=268MB managed=64MB
```

`phase` 覆盖进战全链路（`combat.generate` / `preload` / `creatures` / `create-room` / `room-ready` / `allies` / `enemies` / `creature-node` / `populate` / `ui-activate` / `background` / `bgm`），另有 `spine.ready`（某只怪骨架建完、图集上传完的那一帧）和 `combat.banner`（「战斗开始」横幅的自适应字号二分重排）。判读口径：`proc` 小、`draw`/`vram` 跳高就是 GPU 侧；`proc` 大就是主线程同步干活。

进战这条链跑完时还会多打一行**阶段总账**，因为卡顿常常是一串二十几毫秒的帧叠出来的，不一定有哪一帧单独越过阈值：

```
[Perf] combat entry total=1120ms combat.generate=3ms combat.preload=812ms combat.creatures=44ms combat.create-room=171ms combat.room-ready=48ms combat.creature-node=12ms combat.creature-node=9ms combat.banner=7ms
```

`total` 是整段真实墙钟，每项是两个标签之间的时间。`combat.preload` 大不代表卡（里面混着后台加载让出的帧），真正的嫌疑是那些**同一帧里一口气干完**的项——`creatures` / `create-room` / `room-ready` / 重复出现的 `creature-node`。

顺着这条链已经改掉七处：

- **房间加载后的全量 `GC.Collect()` 在移动端不再阻塞当前帧**（`GC.Collect(2, Forced, blocking: false)`）。它原来正落在进房间的转场帧上，5–20 ms 的停顿直接可感知。
- **意图动画帧并入 run 资产集**。`AbstractIntent.AssetPaths` 只报了静态图标，动画帧每次进战全部 miss，每帧走 `AssetCache` 的同步 `ResourceLoader.Load`；现在进第一场战斗前就已经常驻。
- **房间树也提前建好**。`combat_room.tscn` 两千多个节点，`Instantiate()` 拆不开，原来落在换场景那一帧；现在地图还在屏幕上时就建好，`create-room` 只剩换场景。
- **怪物节点提前一帧一个建好**。原来每只怪的 `NCreature.Create`（PackedScene + visuals 子场景 + 纹理实例化）全都挤在建房间那一帧，敌人越多越重。现在这部分挪到地图还在屏幕上的时候，一帧建一只，房间创建时只做挂载。时序不变（`_Ready` 仍在原来那一帧跑，入场动画和血条判定都照旧）。想看老路径的表现，`user://combat_prewarm.txt` 写 `off` 即可退回。
- **玩家节点和站位场景也进了预热**。原来只预热敌人；玩家走 `CreateAllyNodes` → `AddCreature`，其实是同一条路，而角色立绘那套图集往往是全场最重的。手摆敌人的 encounter 还自带一个 slots 场景，几百个节点，也一起提前建好。
- **背景的视差层也提前建好**。`NCombatBackground.Create` 按层实例化场景，每层带自己的大图，原来同样落在换场景那一格里。现在它跟房间一起在地图还在屏幕上的时候建好，`SetUpBackground` 只做挂载。层组只由 encounter 和一个按地图点重新播种的 `Rng` 决定，提前算和晚算结果一致。
- **首回合也有了单独的账本**。「刚开始打怪」的手感往往落在房间已经出现之后——逐只怪的入场 `AfterCreatureAdded`、以及起手五张牌的 `CardPileCmd.Draw`（一张牌一个节点）。这两段现在分别打 `combat.creature-added` 和 `[Perf] combat turn setup total=<ms> combat.hand-draw=<ms>`，下一版就能直接照着数字动刀。
- **自适应字号的拟合结果做了记忆化**。`MegaLabel` 会用二分搜索把文字塞进框里，每一步都重新 shaping 整段文字；进战那一帧血条、名牌、意图、能量、横幅几十个新 label 同时要字号，问的却是同一批「文本 + 字体 + 矩形」。现在算过一次就记住，后来的实例一次 shaping 都不做。

## 上一版（v16）：灵动岛 + iPhone 17 Pro Max

### 灵动岛实时活动

退到桌面或锁屏后，灵动岛显示当前这局的状态：

| 区域 | 内容 |
|---|---|
| 紧凑左侧 | 角色立绘 + 当前层数 |
| 紧凑右侧 | 红心 + `当前血量` |
| 最小态 | 角色立绘 |
| 展开 | 角色立绘、`A<幕> · <层>`、`hp/maxHp`、血条、角色名、金币，描边颜色随血量绿→黄→红 |
| 锁屏 | 角色立绘 + 角色名 + 「第 X 幕 第 Y 层」+ 血条 + HP/金币 |

数据流不经过任何 RPC：C# 每秒组一次 `user://island_state.json`（内容没变就不写盘），Swift 侧每 0.5 秒轮询这个文件，驱动 `ActivityKit` 的请求 / 更新 / 结束。写文件失败只丢灵动岛，不影响帧率。

```json
{"seq":12,"active":true,"hp":32,"maxHp":80,"floor":7,"act":2,"gold":143,"character":"铁甲战士"}
```

约束：

- 需要 **iOS 16.1+**。ActivityKit 是弱链接（`-weak_framework ActivityKit` + `-Xfrontend -disable-autolink-framework`），所以 14/15/16.0 照常启动，只是没有实时活动。
- 需要在「设置 → 面容 ID 与密码/ App 设置」里允许**实时活动**；被拒绝时原因写在 `user://island_status.txt`。
- 扩展是 `swiftc` 手工组装的 `.appex`（`tools/build_island_extension.py`），没加 Xcode target；`MH_APP_EXTENSION` 由脚本在链接后自己打进 Mach-O 头（macos-14 上 Xcode 15.4 的 ld 会忽略 `-application_extension`）。

### 屏幕与省电

- 关掉 2D MSAA（原默认 2X，对 2D 无视觉收益、带宽翻倍），VSync 置 On，帧率上限钳到 **60**（ProMotion 上原来能跑到 120）。
- 老存档也会被这套一次性设置覆盖，不需要重装；`user://mobile_profile_v1.txt` 标记写过就不再动，之后你在设置里改的手动值会被尊重。
- iPhone 17 Pro Max 的原生分辨率是 1320×2868 点 / 3.8 MPx。想再省电，把 `user://render_scale.txt` 写一个 `design`，画布会改成 `viewport` 模式下按 1920×1080 渲染再放大（布局与触摸目标不变，画面略软）；删掉该文件即回退。
- 竖版立绘（如 `SovereignBlade_Ancient` 884×1244）不再被 `cover` 裁掉一半，按 `keep aspect centered` 摆。
- 启动日志里会打一行 `[Display] model=... screen=1320x2868 ... safeInsetsPx=l... t... r... b... refresh=120Hz form=Phone`，灵岛安全区就是靠它换算的。

### Regent 立绘两套

`RegentArt.pck`（动漫集）与 `RegentFemArt.pck`（女性集）都在包内，`user://regent_art.txt` 首行写 `anime` 或 `fem` 切换，默认 `fem`。两套包互不干扰，缺一套就回退到另一套，都没有则用原版图集。

## 安装（按手头条件选一条）

### A. AltStore / SideStore（免费 Apple ID，7 天有效期）

1. Windows 装 [AltServer](https://altstore.io/) + iTunes（或 Apple Devices）+ iCloud
2. iPhone 连数据线，AltServer 图标 → Install AltStore → 选设备
3. iPhone：设置 → 通用 → VPN与设备管理 → 信任证书；iOS 16+ 还要开「开发者模式」
4. 把 `sts2-unsigned.ipa` 传到手机（AirDrop / iCloud Drive / 文件 App）
5. AltStore → My Apps → 左上角 `+` → 选 ipa
6. 每 7 天点一次 Refresh（免费证书限制，重签不丢存档）

Bundle ID 是 `com.placeholder.sts2`，AltStore 会自动重签，不用改。

### B. Sideloadly（免费 Apple ID，同样 7 天）

IPA 拖进去 → 填 Apple ID → Start → 手机上信任证书。比 AltStore 简单，同样每 7 天重签一次。

### C. 付费开发者账号（$99/年，1 年有效）

用 Xcode 或 Sideloadly 签名，证书 1 年有效，不用每周折腾。

### D. TrollStore（iOS 14.0–16.6.1 / 17.0，永久）

系统落在范围内的话装完永久有效、不用签，ipa 直接丢进去。

## 已知限制

| 项 | 说明 |
|---|---|
| Sentry | 崩溃上报关闭（mobile 上没有原生产物，v3 起主动跳过，避免打断启动） |
| 模拟器 | 不支持，只有 arm64 真机；模拟器需要 compatibility 渲染器，当前是 forward_plus |
| 首次启动 | 1.5 GB PCK 解压纹理，A15 约 20–40 秒，之后正常 |
| 灵动岛 | iOS 16.1+ 且允许实时活动；被拒看 `user://island_status.txt` |
| 音频 | 取决于 `fmod.ios.template_release.framework` 是否签进证书；缺失时静音不崩 |
| 联机 | 没有多人 |

## 排错

| 现象 | 原因 |
|---|---|
| 闪退在启动画面 | 证书没签进 `Frameworks/sts2.dylib` |
| 进主菜单但点单人游戏闪退 | `Frameworks/libspine_godot.ios.template_release.framework` 没签 |
| 进游戏但完全没声音 | `Frameworks/fmod.ios.template_release.framework` 没签（不崩，只是静音） |
| 卡黑屏 | `sts2.pck` 没和 `sts2` 在同一个 `sts2.app/` 目录 |
| 日志里有 `dlopen(...relative path not allowed in hardened program)` | 某个 framework 的 `LC_ID_DYLIB` 还是相对路径（v7 起三个库都是 `@rpath/...`） |
| 灵动岛不显示 | 系统 < 16.1 / 关掉了实时活动 / `PlugIns/Sts2Island.appex` 没签；先看 `user://island_status.txt` |
| 提示 "Unable to install" | Bundle ID 冲突，Sideloadly 里改一个 |
| 提示未受信任的开发者 | 设置 → 通用 → VPN与设备管理 → 信任 |

**拿日志**：iOS「文件」App → 我的 iPhone → Slay the Spire 2 → `logs/godot.log`（存档与日志都在 App 沙盒，卸载重装才会丢）。

## 构建来源

- 游戏本体 `.github/workflows/ios.yml`（macos-14）：`dotnet publish -r ios-arm64`（NativeAOT）→ 验 `beforefieldinit` → `xcodebuild -create-xcframework` → 注入 pbxproj（含 Swift 源与 `ActivityKit` 弱链接）→ `xcodebuild archive`（无签名）→ `tools/build_island_extension.py` 组 `.appex` → 打 Payload → zip → 发布
- Spine 扩展 `.github/workflows/spine-ios.yml`：`spine-runtimes@4.2` + `godot-cpp@4.5` + `scons platform=ios target=template_release arch=arm64`，二进制发布在 tag `spine-godot-ios`

仓库：https://github.com/HDZ123321/sts2-ios

## 历史版本（摘要）

| 版本 | 内容 | 判据 |
|---|---|---|
| v15 | 触屏缺口收尾（面板返回、卡牌详情、拖拽与滑动） | 长按 0.55s = 右键，双指轻点 = ESC |
| v13–v14 | 加载期优化：Common preload 2.0s→1.1s，房间级 GC 只在托管堆涨了才收 | 日志里 `Preloading 'Common' Complete` 明显变快 |
| v10 | 声音真的响了：fmod 的 30 个 flat-namespace 符号打 `N_WEAK_REF`（`0x40`→`0x41`），`dlopen(RTLD_NOW)` 不再被无定义的 `_load_all_fmod_plugins` 拖死；旋转改成改 `sts2-Info.plist` 的双方向数组 | 主菜单有 BGM、转 180° 画面跟着翻、`Identifier "FmodServer" not declared` 归零 |
| v8 | fmod 引用降级为 `LC_LOAD_WEAK_DYLIB`；补 `NSMicrophoneUsageDescription` | 不再「秒退且无日志」 |
| v7 | 补回 FMOD 的 iOS 二进制（`LC_ID_DYLIB` 重写成 `@rpath` 绝对形式；framework basename 必须等于包内二进制名；12 个 bank 靠 `include_filter` 进 PCK） | 有音效与 BGM |
| v6 | 自编 spine-godot iOS framework（spine-runtimes 4.2 + godot-cpp 4.5） | 点单人游戏不再崩在 `NBossMapPoint._Ready()` |
| v5 | 给 7 个类型回填 `beforefieldinit`，且必须打在 ILC 真正读的 `obj/.../sts2.dll` 上 | 不再 `KeyNotFoundException 'RELIC.ASTROLABE'` |
| v4 | `AtlasManager` 换源生成 JSON 契约，修 NativeAOT 下 `Reflection-based serialization` 被禁用 | 日志出现 `AtlasManager: Loaded ui_atlas with 102 sprites` |
| v3 | `SentryService.Initialize()` 在 mobile 提前返回，修黑屏；补 14 个 `.tpsheet`、785 个 `.import`、Steam 走 Null 平台 | 日志出现 `[Startup] Time to main menu: N,NNNms` |
