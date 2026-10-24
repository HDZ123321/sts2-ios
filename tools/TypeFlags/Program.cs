using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

// usage:
//   TypeFlags <assembly.dll>                 -> print stats + every BFI type that has a cctor
//   TypeFlags <assembly.dll> <types.txt>     -> verify mode: every listed type must exist AND carry
//                                               TypeAttributes.BeforeFieldInit; exit 1 otherwise.
if (args.Length < 1)
{
    Console.Error.WriteLine("usage: TypeFlags <assembly.dll> [types.txt]");
    return 1;
}

string path = args[0];
using FileStream fs = File.OpenRead(path);
using PEReader pe = new PEReader(fs);
MetadataReader md = pe.GetMetadataReader();

int total = 0, beforeFieldInit = 0, withCctor = 0, bfiWithCctor = 0;
var hits = new List<string>();
var bfiTypes = new HashSet<string>(StringComparer.Ordinal);
var allTypes = new HashSet<string>(StringComparer.Ordinal);

foreach (TypeDefinitionHandle h in md.TypeDefinitions)
{
    TypeDefinition td = md.GetTypeDefinition(h);
    total++;
    string ns = md.GetString(td.Namespace);
    string name = md.GetString(td.Name);
    string full = string.IsNullOrEmpty(ns) ? name : ns + "." + name;
    allTypes.Add(full);

    bool bfi = (td.Attributes & TypeAttributes.BeforeFieldInit) != 0;
    if (bfi) { beforeFieldInit++; bfiTypes.Add(full); }

    bool hasCctor = false;
    foreach (MethodDefinitionHandle mh in td.GetMethods())
    {
        MethodDefinition m = md.GetMethodDefinition(mh);
        if (md.GetString(m.Name) == ".cctor") { hasCctor = true; break; }
    }
    if (hasCctor) withCctor++;
    if (bfi && hasCctor) { bfiWithCctor++; hits.Add(full); }
}

Console.WriteLine($"types={total} beforefieldinit={beforeFieldInit} withCctor={withCctor} bfi+cctor={bfiWithCctor}");

if (args.Length < 2)
{
    foreach (string s in hits) Console.WriteLine("BFI_CCTOR " + s);
    return 0;
}

// ---- verify mode -------------------------------------------------------
var wanted = new List<string>();
foreach (string raw in File.ReadAllLines(args[1]))
{
    string line = raw.Trim();
    if (line.Length == 0 || line.StartsWith("#")) continue;
    wanted.Add(line);
}

int missing = 0, noBfi = 0;
foreach (string typeName in wanted)
{
    if (!allTypes.Contains(typeName))
    {
        Console.Error.WriteLine($"FAIL missing-type   {typeName}");
        missing++;
        continue;
    }
    if (!bfiTypes.Contains(typeName))
    {
        Console.Error.WriteLine($"FAIL no-beforefieldinit {typeName}");
        noBfi++;
    }
}

Console.WriteLine($"verify: wanted={wanted.Count} missing={missing} noBeforeFieldInit={noBfi}");
if (missing != 0 || noBfi != 0)
{
    Console.Error.WriteLine("FAILED: BeforeFieldInit patch did not reach this assembly. "
        + "If this is the obj/ intermediate assembly, the BfiPatch MSBuild target is not running "
        + "before IlcCompile (check that it patches @(IntermediateAssembly), not $(TargetPath)).");
    return 1;
}

Console.WriteLine("OK: all requested types carry BeforeFieldInit");
return 0;
