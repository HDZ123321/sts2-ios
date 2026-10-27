using System.Collections.Generic;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.Relics;

namespace Watcher.Code.Patches;

[HarmonyPatch(typeof(PaelsEye))]
public static class PaelsEyeSourceTrackingPatches
{
	private static readonly HashSet<PaelsEye> SelfTriggered = new HashSet<PaelsEye>();

	[HarmonyPostfix]
	[HarmonyPatch("ShouldTakeExtraTurn")]
	public static void RecordVote(PaelsEye __instance, Player player, bool __result)
	{
		if (__result && player == __instance.Owner)
		{
			SelfTriggered.Add(__instance);
		}
		else
		{
			SelfTriggered.Remove(__instance);
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch("AfterTakingExtraTurn")]
	public static bool GateConsumption(PaelsEye __instance, Player player, ref Task __result)
	{
		if (player != __instance.Owner)
		{
			return true;
		}
		if (SelfTriggered.Remove(__instance))
		{
			return true;
		}
		__result = Task.CompletedTask;
		return false;
	}

	[HarmonyPostfix]
	[HarmonyPatch("AfterCombatEnd")]
	public static void Cleanup(PaelsEye __instance)
	{
		SelfTriggered.Remove(__instance);
	}
}
