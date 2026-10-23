using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

if (args.Length < 2)
{
    Console.Error.WriteLine("usage: BfiPatch <in.dll> <typename-list.txt> [out.dll]");
    return 1;
}

string asmPath = Path.GetFullPath(args[0]);
string listPath = Path.GetFullPath(args[1]);
string outputPath = args.Length > 2 ? Path.GetFullPath(args[2]) : asmPath;

if (!File.Exists(asmPath))
{
    Console.Error.WriteLine($"input assembly not found: {asmPath}");
    return 1;
}
if (!File.Exists(listPath))
{
    Console.Error.WriteLine($"type list not found: {listPath}");
    return 1;
}

var wanted = new HashSet<string>(
    File.ReadAllLines(listPath)
        .Select(l => l.Trim())
        .Where(l => l.Length > 0 && !l.StartsWith("#")),
    StringComparer.Ordinal);

string asmDir = Path.GetDirectoryName(asmPath)!;

var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(asmDir);
// 同一程序集所在目录之外，再补上发布输出目录与引用库目录，便于 Cecil 解析依赖。
foreach (string extra in new[]
{
    Environment.GetEnvironmentVariable("BFI_RESOLVER_DIR") ?? string.Empty,
    Path.Combine(asmDir, "libs"),
})
{
    if (!string.IsNullOrEmpty(extra) && Directory.Exists(extra))
    {
        resolver.AddSearchDirectory(extra);
    }
}

using var asm = AssemblyDefinition.ReadAssembly(asmPath, new ReaderParameters
{
    AssemblyResolver = resolver,
    ReadWrite = false,
    ReadingMode = ReadingMode.Deferred,
});

int patched = 0;
var seen = new HashSet<string>(StringComparer.Ordinal);

foreach (var type in Walk(asm.MainModule.Types))
{
    if (!wanted.Contains(type.FullName)) continue;
    seen.Add(type.FullName);
    if ((type.Attributes & TypeAttributes.BeforeFieldInit) != 0) continue;
    type.Attributes |= TypeAttributes.BeforeFieldInit;
    patched++;
    Console.WriteLine($"patched: {type.FullName}");
}

foreach (var miss in wanted.Where(w => !seen.Contains(w)).OrderBy(w => w, StringComparer.Ordinal))
{
    Console.Error.WriteLine($"MISSING TYPE: {miss}");
}

asm.Write(outputPath);
Console.WriteLine($"types requested={wanted.Count} matched={seen.Count} patched={patched}");
return seen.Count == wanted.Count ? 0 : 2;

static IEnumerable<TypeDefinition> Walk(IEnumerable<TypeDefinition> types)
{
    foreach (var t in types)
    {
        yield return t;
        foreach (var n in Walk(t.NestedTypes)) yield return n;
    }
}
