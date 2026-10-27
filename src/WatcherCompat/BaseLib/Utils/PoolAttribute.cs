using System;

namespace BaseLib.Utils;

/// <summary>
/// Declares which pool owns a card, relic, potion or power.  The registration
/// pass reads these instead of the reflection scan the original mod relied on.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class PoolAttribute : Attribute
{
	public Type PoolType { get; }

	public PoolAttribute(Type poolType)
	{
		PoolType = poolType;
	}
}
