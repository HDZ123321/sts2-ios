using System;
using System.Runtime.CompilerServices;

namespace BaseLib.Utils;

/// <summary>
/// Per-owner side table with the same access shape as BaseLib's SpireField.
/// Entries are collected together with their key, so a finished run cannot
/// leak state into the next one.
/// </summary>
public sealed class SpireField<TKey, TValue> where TKey : class where TValue : class
{
	private readonly ConditionalWeakTable<TKey, TValue> _table = new ConditionalWeakTable<TKey, TValue>();

	public SpireField()
	{
	}

	public SpireField(Func<TValue> factory)
	{
	}

	public TValue? this[TKey key]
	{
		get
		{
			return _table.TryGetValue(key, out TValue? value) ? value : null;
		}
		set
		{
			if (value == null)
			{
				_table.Remove(key);
				return;
			}
			_table.AddOrUpdate(key, value);
		}
	}

	public bool Has(TKey key)
	{
		return _table.TryGetValue(key, out _);
	}

	public TValue GetOrCreate(TKey key, Func<TValue> factory)
	{
		return _table.GetValue(key, delegate
		{
			return factory();
		});
	}

	public void Clear(TKey key)
	{
		_table.Remove(key);
	}
}
