using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;

namespace Watcher.Code.Patches;

public sealed class ModPatcher
{
	private readonly Harmony _harmony;

	private readonly Logger _logger;

	private readonly List<Type> _types = new List<Type>();

	private bool _applied;

	public bool Verbose { get; set; }

	private ModPatcher(string modId, Logger logger)
	{
		_harmony = new Harmony(modId);
		_logger = logger;
	}

	public static ModPatcher Create(string modId, Logger logger)
	{
		return new ModPatcher(modId, logger);
	}

	public ModPatcher Add(Type patchClass)
	{
		_types.Add(patchClass);
		return this;
	}

	public ModPatcher AddAllFrom(Assembly assembly)
	{
		List<Type> list = (from t in SafeGetTypes(assembly)
			where t.GetCustomAttributes(typeof(HarmonyPatch), inherit: false).Length != 0
			select t).ToList();
		_types.AddRange(list);
		_logger.Info($"Discovered {list.Count} patch classes in {assembly.GetName().Name}.");
		return this;
	}

	public ModPatcher Exclude(Type patchClass)
	{
		_types.Remove(patchClass);
		return this;
	}

	public void PatchAll()
	{
		if (_applied)
		{
			_logger.Warn("PatchAll called twice; ignoring.");
			return;
		}
		_applied = true;
		Stopwatch stopwatch = Stopwatch.StartNew();
		List<string> list = new List<string>();
		List<(string, long)> list2 = new List<(string, long)>();
		int num = 0;
		int num2 = 0;
		foreach (Type item in from t in _types.Distinct()
			orderby t.FullName
			select t)
		{
			Stopwatch stopwatch2 = Stopwatch.StartNew();
			(int, string) tuple = Apply(item);
			stopwatch2.Stop();
			list2.Add((item.Name, stopwatch2.ElapsedMilliseconds));
			if (tuple.Item2 != null)
			{
				list.Add(item.Name + ": " + tuple.Item2);
				continue;
			}
			num++;
			num2 += tuple.Item1;
		}
		stopwatch.Stop();
		if (list.Count == 0)
		{
			_logger.Info($"Patching complete: {num} classes, {num2} methods in {stopwatch.ElapsedMilliseconds}ms.");
		}
		else
		{
			_logger.Error($"Patching finished with {list.Count} FAILURE(S) ({num} classes / {num2} methods OK, {stopwatch.ElapsedMilliseconds}ms):\n  - " + string.Join("\n  - ", list));
		}
		IEnumerable<(string, long)> source = list2.OrderByDescending<(string, long), long>(((string Name, long Ms) t) => t.Ms).Take(10);
		_logger.Info("Slowest patches:\n  " + string.Join("\n  ", source.Select<(string, long), string>(((string Name, long Ms) t) => $"{t.Ms,5}ms  {t.Name}")));
	}

	private (int MethodCount, string? Error) Apply(Type patchClass)
	{
		try
		{
			List<MethodInfo> list = _harmony.CreateClassProcessor(patchClass).Patch();
			if (list == null || list.Count == 0)
			{
				return (MethodCount: 0, Error: "patched ZERO methods (TargetMethod(s) returned null, or wrong signature?)");
			}
			if (!Verbose)
			{
				return (MethodCount: list.Count, Error: null);
			}
			foreach (MethodInfo item2 in list)
			{
				_logger.Info("  " + patchClass.Name + " -> " + Describe(item2));
			}
			return (MethodCount: list.Count, Error: null);
		}
		catch (Exception ex)
		{
			string item = ex.Message.Split('\n')[0].Trim();
			_logger.Error($"{patchClass.Name}: FAILED to apply.\n{ex}");
			return (MethodCount: 0, Error: item);
		}
	}

	private static string Describe(MethodBase method)
	{
		return method.DeclaringType?.Name + "." + method.Name;
	}

	private static Type[] SafeGetTypes(Assembly a)
	{
		try
		{
			return a.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			return ex.Types.Where((Type t) => t != null).ToArray();
		}
	}
}
