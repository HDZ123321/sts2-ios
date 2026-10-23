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

### 2.7 已剥离的组件（iOS 上不可用）
| 组件 | 原因 | 处理 |
|---|---|---|
| **Sentry** | gdextension 清单里根本没有 ios 条目 | 从 `[autoload]` 移除 |
| **FMOD** | 有 ios 清单，但 `.xcframework` 不在 PCK 里，需 Firelight 授权 | 从 `[autoload]` 移除；C# 侧只用到 `FmodSfx.cs` 的字符串常量，不影响逻辑 |
| **Spine** | 同上，需 Esoteric 授权 | 移除；C# 侧全是字符串反射（`SpineSprite`），动画降级不崩 |
| **Steam** | 无 iOS SDK | 走 `Platform.Null` 分支，单机可用 |

音效因此静音（FMOD bank 未打包）。要恢复得自己搞到 FMOD for Godot 的 iOS xcframework。

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
├── sts2.xcodeproj/
├── sts2/
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

1. **无音效** —— FMOD bank 未打包，FMOD/Spine/Sentry 三个 gdextension 都摘掉了
2. **无 Spine 动画** —— 骨骼动画缺失，静态帧正常
3. **无多人联机** —— Steam 传输层不可用，单机全功能
4. **模拟器跑不了** —— 需要 `compatibility` 渲染器；当前工程用默认 `forward_plus`。
   真机 arm64 没问题
5. **首次加载慢** —— 1.5 GB PCK + 解压纹理，A15 上约 20–40 秒
6. **`audio_manager_proxy.gd` 报 Parse error** —— `.gdc` 字节码重新加载的问题，
   该脚本只服务 FMOD，不影响 C# 主流程

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
