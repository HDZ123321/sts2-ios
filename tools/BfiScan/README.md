# tools/BfiScan + tools/CtorScan — 两类「构造期读后建模型」自查器

两个都是 Mono.Cecil 静态扫描器，只读 DLL，不改任何东西。它们查的是同一类 bug 的两张脸：
`ModelDb.Init()` 按 `AllAbstractModelSubtypes` 的数组序构造模型，所以任何在构造期去读**排在
自己后面**的模型，都会当场 `KeyNotFoundException`。

| 工具 | 查什么 | 覆盖 |
|---|---|---|
| `BfiScan` | 静态构造（`.cctor`）里的 `ModelDb.<T>()` | 只对**精确**初始化器有效（没有 `beforefieldinit` 位的类型，cctor 才会在 `Init` 期间跑） |
| `CtorScan` | 实例构造（`.ctor`）里的 `ModelDb.<T>()` | 沿本程序集内的调用递归（默认 4 层），并传播泛型实参 |

## 为什么需要两个

`beforefieldinit` 只管静态构造。v0.113.1 上真正炸的是 `BlitzRunModel`（index 26）的 cctor
要 `ModelDb.Enchantment<Sharp>()`（index 804）—— 这是 BfiScan 的领域，判据是
`bfi+cctor` 计数等于 **2568**。

而 `Alpha`（index 1867）是在**实例构造函数**里读 `Beta`（index 1871），BfiScan 看不见。
这处已经被 `ModelDb.Get<T>()` 的 `TryResolveMissing` 兜住（按需构造 + `_resolving` 挡重入），
所以 CtorScan 现在报出来的是「已知且已缓解」，不是待修。

## 跑法

```bash
# 先有编译产物（.godot/mono/temp/bin/Release/sts2.dll 或 AOT 之前的托管 dll）
dotnet run --project tools/BfiScan -- .godot/mono/temp/bin/Release/sts2.dll
dotnet run --project tools/CtorScan -- .godot/mono/temp/bin/Release/sts2.dll --verbose

# 想放宽调用递归深度
dotnet run --project tools/CtorScan -- sts2.dll --max-depth 6
```

两边都报 `scanned PRECISE cctors: 0` / `problems: 0` 才算干净。上游原版 DLL 应当也是
`problems: 0`，拿它当基线对比能区分「移植层引入的」和「上游本来就有的」。

## 与 BfiPatch 的分工

`tools/BfiPatch/` 是**修**（编译后把 `bfi_types.txt` 里 8 个类型的 `BeforeFieldInit` 位打回去，
挂在 `sts2.csproj` 的 `AfterTargets="CoreCompile"`）；这两个是**查**（扫出还有没有漏的）。
BfiPatch 在 CI 里自动跑，这两个按需手动跑。
