using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

// Scan for the BlitzRunModel class of bug: a type whose static constructor reads
// ModelDb.<Accessor><T>() for a T that ModelDb.Init has not built yet, i.e. T sits
// LATER in AllAbstractModelSubtypes than the type whose cctor is running.
if (args.Length < 1) { Console.Error.WriteLine("usage: bfiscan <built.dll>"); return 1; }

string dll = args[0];
var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(dll))!);
using var asm = AssemblyDefinition.ReadAssembly(dll, new ReaderParameters { AssemblyResolver = resolver, ReadingMode = ReadingMode.Deferred });
var mod = asm.MainModule;

// 1) Recover the AllAbstractModelSubtypes order from AbstractModelSubtypes' static ctor.
var order = new List<string>();
var subtypeType = mod.GetType("MegaCrit.Sts2.Core.Models.AbstractModelSubtypes");
if (subtypeType == null) { Console.Error.WriteLine("AbstractModelSubtypes not found"); return 1; }
var cctor = subtypeType.Methods.FirstOrDefault(m => m.Name == ".cctor");
// Field -> typeof(...) literal mapping comes from the cctor's stsfld sequence.
var fieldToType = new Dictionary<string, string>(StringComparer.Ordinal);
foreach (var m in subtypeType.Methods.Where(m => m.IsStatic && m.HasBody))
{
    foreach (var ins in m.Body.Instructions)
    {
        if (ins.OpCode == OpCodes.Ldtoken && ins.Operand is TypeReference tr)
        {
            // next stsfld names the field
            var nx = ins.Next;
            while (nx != null && nx.OpCode != OpCodes.Stsfld) nx = nx.Next;
            if (nx?.Operand is FieldReference fr) fieldToType[fr.Name] = tr.FullName;
        }
    }
}
// The array literal: newarr Type; dup; ldc.i4 idx; ldtoken T; stobj Type  ... OR
// stelem.ref with ldtoken. Handle both by walking for ldtoken inside the array init method.
var arrayField = subtypeType.Fields.FirstOrDefault(f => f.Name == "_subtypes" || f.Name.Contains("subtypes"));
string arrayInitName = null;
foreach (var m in subtypeType.Methods.Where(m => m.IsStatic && m.HasBody))
{
    if (m.Body.Instructions.Any(i => i.OpCode == OpCodes.Ldtoken && i.Operand is TypeReference t && t.FullName.Contains("Achievement")))
    { arrayInitName = m.Name; break; }
}
if (arrayInitName == null) { Console.Error.WriteLine("could not find the subtype array initializer"); return 1; }
var init = subtypeType.Methods.First(m => m.Name == arrayInitName);
foreach (var ins in init.Body.Instructions)
{
    if (ins.OpCode == OpCodes.Ldtoken && ins.Operand is TypeReference t2) order.Add(t2.FullName);
}
Console.WriteLine($"subtype order recovered: {order.Count} entries (initializer {arrayInitName})");
var index = new Dictionary<string, int>(StringComparer.Ordinal);
for (int i = 0; i < order.Count; i++) index[order[i]] = i;

// 2) For every type in the order, look at its .cctor for ModelDb accessor calls.
var modelDb = mod.GetType("MegaCrit.Sts2.Core.Models.ModelDb");
var accessors = new HashSet<string>(StringComparer.Ordinal);
foreach (var m in modelDb.Methods)
{
    if (!m.IsStatic || !m.HasGenericParameters || m.GenericParameters.Count != 1) continue;
    if (m.Name.StartsWith("get_")) continue;
    accessors.Add(m.Name);
}
Console.WriteLine($"ModelDb single-generic accessors: {string.Join(", ", accessors.OrderBy(x => x))}");

var problems = new List<string>();
int scanned = 0;
foreach (var type in order)
{
    var td = mod.GetType(type);
    if (td == null) continue;
    var sc = td.Methods.FirstOrDefault(m => m.Name == ".cctor");
    if (sc == null || !sc.HasBody) continue;
    bool precise = (td.Attributes & TypeAttributes.BeforeFieldInit) == 0;
    if (!precise) continue;   // deferred cctor never runs during ModelDb.Init
    scanned++;
    foreach (var ins in sc.Body.Instructions)
    {
        if (ins.OpCode != OpCodes.Call && ins.OpCode != OpCodes.Callvirt) continue;
        if (ins.Operand is not MethodReference mr) continue;
        if (mr.DeclaringType?.FullName != "MegaCrit.Sts2.Core.Models.ModelDb") continue;
        if (!accessors.Contains(mr.Name)) continue;
        string target;
        if (mr is GenericInstanceMethod gim && gim.GenericArguments.Count == 1)
        {
            target = gim.GenericArguments[0].FullName;
        }
        else if (mr.GenericParameters.Count == 1)
        {
            target = mr.GenericParameters[0].FullName;
        }
        else
        {
            continue;
        }
        if (!index.TryGetValue(type, out int self)) continue;
        if (!index.TryGetValue(target, out int other))
        {
            problems.Add($"UNINDEXED  {type} cctor -> ModelDb.{mr.Name}<{target}>()  (target not in subtype array)");
            continue;
        }
        if (other > self)
            problems.Add($"LATE-READ  {type} [idx {self}] cctor -> ModelDb.{mr.Name}<{target}>() [idx {other}]  delta {other - self}");
    }
}
Console.WriteLine($"scanned PRECISE cctors (no beforefieldinit): {scanned}");
Console.WriteLine($"problems: {problems.Count}");
foreach (var p in problems.OrderBy(x => x, StringComparer.Ordinal)) Console.WriteLine("  " + p);
return 0;
