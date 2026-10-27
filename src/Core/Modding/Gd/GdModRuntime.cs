using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace MegaCrit.Sts2.Core.Modding.Gd;

/// <summary>
/// Shared state for the GDScript mod bridge: the list of loaded mod instances, the object handle
/// table that lets GDScript refer to C# objects by string, and the dispatch helpers that forward a
/// hook to every loaded mod.
///
/// Handles exist because GDScript cannot hold a reference to a <see cref="Creature"/> or a
/// <see cref="CardModel"/> — those are plain C# classes with no Godot binding. Every object that
/// crosses the boundary gets a short string handle; the mod passes it back when it wants to act on
/// the object through <see cref="Sts2Api"/>.
///
/// The table is weak on both sides (ConditionalWeakTable for the forward direction, a WeakReference
/// inside the reverse dictionary) so a handle never keeps a dead combat object alive.
/// </summary>
public static class GdModRuntime
{
	private sealed class HandleBox
	{
		public string Handle = string.Empty;
	}

	private static readonly ConditionalWeakTable<object, HandleBox> _handleOf = new ConditionalWeakTable<object, HandleBox>();

	private static readonly Dictionary<string, WeakReference<object>> _objectOf = new Dictionary<string, WeakReference<object>>(StringComparer.Ordinal);

	private static readonly List<Node> _mods = new List<Node>();

	private static int _nextHandle = 1;

	/// <summary>Root node the mod script instances are parented to. Set by <see cref="GdModHost"/>.</summary>
	public static Node? ModRoot { get; set; }

	/// <summary>Every mod entry node that loaded successfully, in load order.</summary>
	public static IReadOnlyList<Node> Mods => _mods;

	public static void AddMod(Node mod)
	{
		if (mod == null)
		{
			return;
		}
		_mods.Add(mod);
	}

	public static void Clear()
	{
		_mods.Clear();
		_objectOf.Clear();
		_nextHandle = 1;
	}

	/// <summary>
	/// Returns a stable handle for an object, allocating one on first sight. Returns an empty string
	/// for null so callers can pass the result straight to GDScript.
	/// </summary>
	public static string HandleFor(object? target)
	{
		if (target == null)
		{
			return string.Empty;
		}
		if (_handleOf.TryGetValue(target, out HandleBox? box))
		{
			return box.Handle;
		}
		string handle = $"{Describe(target)}_{_nextHandle++}";
		box = new HandleBox { Handle = handle };
		_handleOf.Add(target, box);
		_objectOf[handle] = new WeakReference<object>(target);
		if (_objectOf.Count > 4096)
		{
			PruneDeadHandles();
		}
		return handle;
	}

	/// <summary>Resolves a handle back to its object, or null when the object has been collected.</summary>
	public static object? Resolve(string? handle)
	{
		if (string.IsNullOrEmpty(handle))
		{
			return null;
		}
		if (!_objectOf.TryGetValue(handle, out WeakReference<object>? weak))
		{
			return null;
		}
		return weak.TryGetTarget(out object? target) ? target : null;
	}

	/// <summary>Typed convenience wrapper over <see cref="Resolve"/>.</summary>
	public static T? Resolve<T>(string? handle) where T : class
	{
		return Resolve(handle) as T;
	}

	private static void PruneDeadHandles()
	{
		List<string> dead = new List<string>();
		foreach (KeyValuePair<string, WeakReference<object>> pair in _objectOf)
		{
			if (!pair.Value.TryGetTarget(out object? _))
			{
				dead.Add(pair.Key);
			}
		}
		foreach (string key in dead)
		{
			_objectOf.Remove(key);
		}
	}

	private static string Describe(object target)
	{
		return target switch
		{
			Creature => "creature",
			Player => "player",
			CardModel => "card",
			PowerModel => "power",
			RelicModel => "relic",
			PotionModel => "potion",
			AbstractRoom => "room",
			RunState => "run",
			_ => target.GetType().Name.ToLowerInvariant()
		};
	}

	/// <summary>
	/// Calls <paramref name="method"/> on every loaded mod that defines it and returns the sum of the
	/// returned deltas. A mod returns nothing (nil) to abstain; anything that is not a number is
	/// reported and treated as an abstention.
	/// </summary>
	public static decimal DispatchAdditive(string method, decimal delta, params Variant[] args)
	{
		foreach (Node mod in _mods)
		{
			Variant result = CallMod(mod, method, args);
			if (result.VariantType == Variant.Type.Nil)
			{
				continue;
			}
			if (!TryToDecimal(mod, method, result, out decimal value))
			{
				continue;
			}
			delta += value;
		}
		return delta;
	}

	/// <summary>
	/// Calls <paramref name="method"/> on every loaded mod and multiplies the returned factors
	/// together. A mod returns nothing (nil) to abstain.
	///
	/// A bad return is skipped, never treated as zero. Zero would look like a mod that answered and
	/// said "no damage", which is indistinguishable from a legitimate answer and silently cancels
	/// every attack in the run; skipping keeps the failure local to the mod that caused it.
	/// </summary>
	public static decimal DispatchMultiplicative(string method, decimal factor, params Variant[] args)
	{
		foreach (Node mod in _mods)
		{
			Variant result = CallMod(mod, method, args);
			if (result.VariantType == Variant.Type.Nil)
			{
				continue;
			}
			if (!TryToDecimal(mod, method, result, out decimal value))
			{
				continue;
			}
			factor *= value;
		}
		return factor;
	}

	/// <summary>
	/// Calls <paramref name="method"/> on every loaded mod and returns the last boolean that a mod
	/// actually answered, or <paramref name="fallback"/> when nobody answered.
	///
	/// The conversion is guarded: a predicate sits in the middle of the damage pipeline, so a mod that
	/// returns a string here would otherwise throw out of the hook and abort whatever was being
	/// resolved.
	/// </summary>
	public static bool DispatchBool(string method, bool fallback, params Variant[] args)
	{
		bool result = fallback;
		foreach (Node mod in _mods)
		{
			Variant answer = CallMod(mod, method, args);
			if (answer.VariantType == Variant.Type.Nil)
			{
				continue;
			}
			if (answer.VariantType != Variant.Type.Bool)
			{
				Log.Error($"[GdMod] {method} in {mod.Name} returned {answer.VariantType} instead of a bool; ignoring that answer");
				continue;
			}
			result = answer.AsBool();
		}
		return result;
	}

	/// <summary>Calls <paramref name="method"/> on every loaded mod for its side effects.</summary>
	public static void DispatchVoid(string method, params Variant[] args)
	{
		foreach (Node mod in _mods)
		{
			CallMod(mod, method, args);
		}
	}

	private static Variant CallMod(Node mod, string method, Variant[] args)
	{
		try
		{
			if (!GodotObject.IsInstanceValid(mod) || !mod.HasMethod(method))
			{
				return default(Variant);
			}
			return mod.Call(method, args);
		}
		catch (Exception ex)
		{
			Log.Error($"[GdMod] {method} in {mod.Name} threw: {ex.GetType().Name}: {ex.Message}");
			return default(Variant);
		}
	}

	private static bool TryToDecimal(Node mod, string method, Variant value, out decimal number)
	{
		switch (value.VariantType)
		{
			case Variant.Type.Int:
				number = value.AsInt64();
				return true;
			case Variant.Type.Float:
				number = (decimal)value.AsDouble();
				return true;
			default:
				Log.Error($"[GdMod] {method} in {mod.Name} returned {value.VariantType} instead of a number; ignoring that answer");
				number = 0m;
				return false;
		}
	}

	/// <summary>
	/// Fire-and-forget for the async command layer. The Godot binding is synchronous, so a command
	/// that awaits is left running; only a fault is reported.
	/// </summary>
	public static void Fire(Task task, string what)
	{
		if (task.IsCompletedSuccessfully)
		{
			return;
		}
		task.ContinueWith(delegate(Task t)
		{
			Log.Error($"[GdMod] {what} failed: {t.Exception?.GetBaseException().Message}");
		}, TaskContinuationOptions.OnlyOnFaulted);
	}

	/// <summary>Packs a handle into a Variant argument list.</summary>
	public static Variant H(object? target)
	{
		return Variant.From(HandleFor(target));
	}
}
