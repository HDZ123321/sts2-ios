using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

/// <summary>
/// Runs a hook over every model that is listening in this combat.  The base game
/// dispatches its own hooks through static <c>Hook</c> methods; mod hooks have no
/// such wrapper, so they share this one.
/// </summary>
public static class HookUtils
{
	public static IEnumerable<T> Listeners<T>(ICombatState? combatState) where T : class
	{
		if (combatState == null)
		{
			yield break;
		}
		foreach (AbstractModel model in combatState.IterateHookListeners())
		{
			if (model is T typed)
			{
				yield return typed;
			}
		}
	}

	public static async Task Dispatch<T>(ICombatState? combatState, PlayerChoiceContext choiceContext, Func<T, Task> action) where T : class
	{
		if (combatState == null || action == null)
		{
			return;
		}
		foreach (T listener in new List<T>(Listeners<T>(combatState)))
		{
			await action(listener);
		}
	}

	public static TValue Aggregate<T, TValue>(ICombatState? combatState, TValue baseValue, Func<T, TValue, TValue> fold) where T : class
	{
		if (combatState == null || fold == null)
		{
			return baseValue;
		}
		TValue current = baseValue;
		foreach (T listener in new List<T>(Listeners<T>(combatState)))
		{
			current = fold(listener, current);
		}
		return current;
	}
}
