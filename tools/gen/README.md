# tools/gen — 历史一次性源码改写脚本

这些脚本原本散在 `build/`（被 `.gitignore` 忽略），现在收进仓库，因为 README 与
`docs/RELEASE_NOTES.md` 里讲的若干机制（BakedContent 挂载、BaseLib 兼容层、ModelDb 自愈、
autoslay 死亡门槛、ModelId 前缀、hook 契约……）都是它们产出的——不入库的话，那些描述就没有
可复现的来源。

## 它们的共同点

- **不是构建链的一环。** CI 不跑它们，`BUILD_MACOS.sh` 也不跑。它们只是「把某批改动写进
  `src/` 的一次性生成器」，产物早就提交在 `src/` 里了。
- **改的是 tracked 源码，靠字面量替换。** 每个脚本都先找锚点文本再替换，找不到就 assert 失败；
  锚点已经在位时直接跳过，所以**重跑是 no-op**，不会叠加。
- **路径已可移植。** 原来全部硬编码 `E:\sts2_port\ci_bundle`（旧 checkout 位置），现在统一
  用 `_REPO = <本文件>/../../../`，在任何 checkout 根下都能跑。

## 用法

```bash
# 从仓库根跑，看它会改什么之前先 git status
python3 tools/gen/gen_baked.py
git diff --stat
```

想回退就直接 `git checkout -- src/`。

## 清单

| 脚本 | 产出 |
|---|---|
| `gen_baked.py` | `src/Core/Modding/BakedContent.cs` + `OneTimeInitialization` 挂载点 + loc 表合并 |
| `gen_compat1/2/3.py` | `src/WatcherCompat/BaseLib/` 兼容层三个批次（身份存储、属性、tooltip、scry 接口、成员放宽） |
| `gen_modeldb.py` | `ModelDb.Init()` 期间的按需解析（Alpha 构造里读 Beta） |
| `gen_modelid.py` | `WATCHER-<SLUG>` 的 model id 方案，保住旧存档与 loc key |
| `gen_pool_fixes.py` | Token 卡池、`MockCardPool` 抛 `You monster!` 一类三处修复 |
| `gen_hook_contract.py` | GDScript hook 桥的「链式 vs 赋值式」返回值契约 |
| `gen_loc_extra.py` | scry hover tip 两条文案，合并进 `powers` 表 |
| `gen_autoslay_death.py` / `gen_autoslay_gate.py` | autoslay 的死亡判定与命令行门控 |
| `gen_architect_fix.py` / `gen_cardattack_fix.py` / `gen_cardblock_fix.py` / `gen_energy_color.py` / `gen_frame_fix.py` | Watcher 卡牌与 UI 的单点修复 |
| `gen_epoch_fix{,2,3,4}.py` | `ProgressSaveManager` 的 epoch 字段四轮迭代 |
| `fix_mobile_preload{,2}.py` / `fix_atlas_comment.py` / `fix_vfx_window.py` / `force_mobile_toggle.py` | 移动端预加载与图集/特效窗口 |
| `extract_regent.py` | 从上游 mod 的 PCK 抽 Regent 卡图到 `build/regent/extracted` |
| `stage_watcher.py` | 把 Watcher mod 源码摊进 `src/Watcher/` |
| `gen_workflow.py` | 往 `.github/workflows/ios.yml` 插「拷贝内容包」步骤并扩 sanity check |
| `checklit.py` / `inspect_loc{,2}.py` | 只读检查：编译产物里找字面量、dump PCK 的 loc 表 |

`extract_regent.py` 与 `stage_watcher.py` 依赖 `build/` 下的中间目录（上游 mod 的 PCK、
摊开的源码），那些目录没入库，跑之前得自己准备。
