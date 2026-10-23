using Godot;

// Normally emitted by Godot.SourceGenerators (GodotPluginsInitializerGenerator).
// The decompiled sources already contain every generated member (MethodName /
// PropertyName / SignalName nested classes, InvokeGodotClassMethod, ...), so the
// source generators are disabled in sts2.csproj. That also removes this assembly
// attribute, which ScriptManagerBridge.LookupScriptsInAssembly requires — without
// it no [ScriptPath] type is ever registered and every .tscn script node fails
// with "Cannot instantiate C# script because the associated class could not be found".
// The parameterless ctor sets RequiresLookup = true, so registration goes through
// reflection over the assembly instead of the generated ScriptTypes array.
[assembly: AssemblyHasScripts]
