using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

// Scan for the Alpha/Beta class of bug: a model's INSTANCE constructor (directly or
// through a few calls) reads ModelDb.<Accessor><T>() for a T that ModelDb.Init has
// not built yet, i.e. T sits LATER in AllAbstractModelSubtypes than the type being
// constructed.
//
// The beforefieldinit scanner (_bfiscan) only looks at static constructors, so it
// cannot see this pattern. ModelDb.Get<T> resolves lazily, which makes these reads
// safe at runtime; this tool reports them so a future regression is visible.
//
// Generic arguments are propagated into callee bodies, so a read reached through
// HoverTipFactory.FromCardWithCardHoverTips<Beta>() is attributed to Beta.
//
// usage: ctorcan <built.dll> [--max-depth N] [--verbose]
if (args.Length < 1)
{
    Console.Error.WriteLine("usage: ctorcan <built.dll> [--max-depth N] [--verbose]");
    return 1;
}

string dll = args[0];
int maxDepth = 4;
bool verbose = false;
for (int i = 1; i < args.Length; i++)
{
    if (args[i] == "--max-depth" && i + 1 < args.Length && int.TryParse(args[i + 1], out int d)) maxDepth = d;
    if (args[i] == "--verbose") verbose = true;
}

var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(dll))!);
using var asm = AssemblyDefinition.ReadAssembly(dll, new ReaderParameters
{
    AssemblyResolver = resolver,
    ReadingMode = ReadingMode.Deferred,
});
var mod = asm.MainModule;

var subtypeType = mod.GetType("MegaCrit.Sts2.Core.Models.AbstractModelSubtypes");
if (subtypeType == null)
{
    Console.Error.WriteLine("AbstractModelSubtypes not found");
    return 1;
}

string arrayInitName = null;
foreach (var m in subtypeType.Methods.Where(m => m.IsStatic && m.HasBody))
{
    if (m.Body.Instructions.Any(ins => ins.OpCode == OpCodes.Ldtoken
            && ins.Operand is TypeReference t
            && t.FullName.Contains("Achievement")))
    {
        arrayInitName = m.Name;
        break;
    }
}
if (arrayInitName == null)
{
    Console.Error.WriteLine("could not find the subtype array initializer");
    return 1;
}

var order = new List<string>();
foreach (var ins in subtypeType.Methods.First(m => m.Name == arrayInitName).Body.Instructions)
{
    if (ins.OpCode == OpCodes.Ldtoken && ins.Operand is TypeReference t2) order.Add(t2.FullName);
}
Console.WriteLine($"subtype order recovered: {order.Count} entries");

var index = new Dictionary<string, int>(StringComparer.Ordinal);
for (int i = 0; i < order.Count; i++) index[order[i]] = i;

var modelDb = mod.GetType("MegaCrit.Sts2.Core.Models.ModelDb");
if (modelDb == null)
{
    Console.Error.WriteLine("ModelDb not found");
    return 1;
}
var accessors = new HashSet<string>(StringComparer.Ordinal);
foreach (var m in modelDb.Methods)
{
    if (!m.IsStatic || !m.HasGenericParameters || m.GenericParameters.Count != 1) continue;
    if (m.Name.StartsWith("get_", StringComparison.Ordinal)) continue;
    accessors.Add(m.Name);
}
Console.WriteLine($"ModelDb single-generic accessors: {string.Join(", ", accessors.OrderBy(x => x, StringComparer.Ordinal))}");

// Methods that are allowed to read any model: they never run during ModelDb.Init.
// GetByIdOrNull / GetLoadedOrNull are the duplicate guard inside AbstractModel's
// constructor, and they deliberately look up AbstractModel itself rather than a
// concrete subtype, so they are not evidence of a late read.
var exempt = new HashSet<string>(StringComparer.Ordinal)
{
    "MegaCrit.Sts2.Core.Models.ModelDb::GetLoadedOrNull",
    "MegaCrit.Sts2.Core.Models.ModelDb::GetByIdOrNull",
    "MegaCrit.Sts2.Core.Models.ModelDb::GetId",
};

var problems = new List<string>();
var seen = new HashSet<string>(StringComparer.Ordinal);
var scanned = 0;

// Resolve a type reference through the current generic substitution.
TypeReference Subst(TypeReference tr, Dictionary<string, TypeReference> env)
{
    if (tr is GenericParameter gp && env.TryGetValue(gp.Name, out TypeReference bound)) return bound;
    if (tr is GenericInstanceType git)
    {
        var args2 = git.GenericArguments.Select(a => Subst(a, env)).ToArray();
        var clone = new GenericInstanceType(git.ElementType);
        foreach (var a in args2) clone.GenericArguments.Add(a);
        return clone;
    }
    return tr;
}

void Walk(MethodDefinition method, string origin, int selfIdx, int depth,
          List<string> trail, Dictionary<string, TypeReference> env)
{
    if (method == null || !method.HasBody) return;
    if (depth > maxDepth) return;

    // Rebind the callee's own generic parameters onto the caller's substitution.
    var local = new Dictionary<string, TypeReference>(env, StringComparer.Ordinal);
    foreach (var gp in method.GenericParameters)
    {
        if (!local.ContainsKey(gp.Name)) local[gp.Name] = gp;
    }

    foreach (var ins in method.Body.Instructions)
    {
        if (ins.OpCode != OpCodes.Call && ins.OpCode != OpCodes.Callvirt) continue;
        if (ins.Operand is not MethodReference mr) continue;

        // Build the substitution for this call site.
        var callEnv = local;
        if (mr is GenericInstanceMethod gim)
        {
            callEnv = new Dictionary<string, TypeReference>(local, StringComparer.Ordinal);
            var calleeGenericParams = mr.GetElementMethod().GenericParameters;
            for (int i = 0; i < gim.GenericArguments.Count && i < calleeGenericParams.Count; i++)
            {
                callEnv[calleeGenericParams[i].Name] = Subst(gim.GenericArguments[i], local);
            }
        }

        if (mr.DeclaringType?.FullName == "MegaCrit.Sts2.Core.Models.ModelDb" && accessors.Contains(mr.Name))
        {
            string key = "MegaCrit.Sts2.Core.Models.ModelDb::" + mr.Name;
            if (exempt.Contains(key)) continue;

            TypeReference targetRef = null;
            if (mr is GenericInstanceMethod gim2 && gim2.GenericArguments.Count == 1) targetRef = gim2.GenericArguments[0];
            else if (mr.GenericParameters.Count == 1) targetRef = mr.GenericParameters[0];
            if (targetRef == null) continue;
            string target = Subst(targetRef, local).FullName;

            string where = string.Join(" -> ", trail);
            if (!index.TryGetValue(target, out int other))
            {
                string line = $"UNINDEXED   {origin} -> ModelDb.{mr.Name}<{target}>()  via {where}";
                if (seen.Add(line)) problems.Add(line);
                continue;
            }
            if (other > selfIdx)
            {
                string line = $"LATE-CTOR   {origin} [idx {selfIdx}] -> ModelDb.{mr.Name}<{target}>() [idx {other}] delta {other - selfIdx}  via {where}";
                if (seen.Add(line)) problems.Add(line);
            }
            continue;
        }

        if (depth == maxDepth) continue;
        if (mr.DeclaringType?.Scope != mod) continue;
        MethodDefinition resolved;
        try
        {
            resolved = mr.Resolve();
        }
        catch
        {
            resolved = null;
        }
        if (resolved == null || !resolved.HasBody) continue;
        trail.Add(mr.Name + "<" + mr.DeclaringType.Name + ">");
        Walk(resolved, origin, selfIdx, depth + 1, trail, callEnv);
        trail.RemoveAt(trail.Count - 1);
    }
}

foreach (var typeName in order)
{
    var td = mod.GetType(typeName);
    if (td == null) continue;
    var ctor = td.Methods.FirstOrDefault(m => m.Name == ".ctor" && !m.IsStatic);
    if (ctor == null || !ctor.HasBody) continue;
    scanned++;
    if (!index.TryGetValue(typeName, out int selfIdx)) continue;
    Walk(ctor, typeName, selfIdx, 0, new List<string>(), new Dictionary<string, TypeReference>(StringComparer.Ordinal));
}

Console.WriteLine($"scanned instance constructors: {scanned}");
Console.WriteLine($"problems: {problems.Count}");
foreach (var p in problems.OrderBy(x => x, StringComparer.Ordinal))
{
    Console.WriteLine("  " + p);
}
return 0;
