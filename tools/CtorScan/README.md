# tools/CtorScan

Mono.Cecil 静态扫描器：按 `AllAbstractModelSubtypes` 的数组序走每个类型的**实例构造函数**，
沿本程序集内的调用递归（默认 4 层）并传播泛型实参，报出所有「构造期读了排在后面的模型」的
`ModelDb.<T>()` 调用。

```bash
dotnet run --project tools/CtorScan -- <built.dll> [--max-depth N] [--verbose]
```

它和 `tools/BfiScan/` 查的是同一类坑的两张脸（那边管静态构造，这边管实例构造），
完整说明见 [`../BfiScan/README.md`](../BfiScan/README.md)。
