using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Watcher.Code.Patches;

[HarmonyPatch]
internal static class StanceModifyDamageMultiplicativePatch
{
	private static MethodBase TargetMethod()
	{
		return AccessTools.Method(AccessTools.TypeByName("MegaCrit.Sts2.Core.Hooks.Hook") ?? throw new MissingMethodException("Hook not found"), "ModifyDamageInternal") ?? throw new MissingMethodException("ModifyDamageInternal not found");
	}

	private static decimal AdditiveBridge(AbstractModel listener, decimal vanillaNum, Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource, CardPlay? cardPlay)
	{
		if (listener is IModifyDamageAdditive modifyDamageAdditive)
		{
			return vanillaNum + modifyDamageAdditive.ModifyDamageAdditiveCompability(target, amount, props, dealer, cardSource, cardPlay);
		}
		return vanillaNum;
	}

	private static decimal MultiplicativeBridge(AbstractModel listener, decimal vanillaNum, Creature target, decimal amount, ValueProp props, Creature dealer, CardModel cardSource, CardPlay? cardPlay)
	{
		if (listener is IModifyDamageMultiplicative modifyDamageMultiplicative)
		{
			return vanillaNum * modifyDamageMultiplicative.ModifyDamageMultiplicativeCompability(target, amount, props, dealer, cardSource, cardPlay);
		}
		return vanillaNum;
	}

	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
	{
		List<CodeInstruction> list = new List<CodeInstruction>(instructions);
		bool flag = original.GetParameters().Any((ParameterInfo p) => p.ParameterType == typeof(CardPlay));
		MethodInfo method = AccessTools.Method(typeof(AbstractModel), "ModifyDamageAdditive");
		MethodInfo method2 = AccessTools.Method(typeof(AbstractModel), "ModifyDamageMultiplicative");
		MethodInfo methodInfo = AccessTools.Method(typeof(StanceModifyDamageMultiplicativePatch), "AdditiveBridge");
		MethodInfo methodInfo2 = AccessTools.Method(typeof(StanceModifyDamageMultiplicativePatch), "MultiplicativeBridge");
		for (int num = 0; num < list.Count; num++)
		{
			bool flag2 = list[num].Calls(method);
			bool flag3 = list[num].Calls(method2);
			if (!flag2 && !flag3)
			{
				continue;
			}
			int num2 = num + 1;
			if (num2 < list.Count && !(list[num2].opcode != OpCodes.Stloc_S))
			{
				object operand = list[num2].operand;
				CodeInstruction codeInstruction = FindListenerLoadBackwards(list, num);
				if (codeInstruction != null)
				{
					List<CodeInstruction> list2 = new List<CodeInstruction>
					{
						codeInstruction.Clone(),
						new CodeInstruction(OpCodes.Ldloc_S, operand),
						new CodeInstruction(OpCodes.Ldarg_2),
						new CodeInstruction(OpCodes.Ldloc_0),
						new CodeInstruction(OpCodes.Ldarg_S, (byte)5),
						new CodeInstruction(OpCodes.Ldarg_3),
						new CodeInstruction(OpCodes.Ldarg_S, (byte)6),
						flag ? new CodeInstruction(OpCodes.Ldarg_S, (byte)7) : new CodeInstruction(OpCodes.Ldnull),
						new CodeInstruction(OpCodes.Call, flag2 ? methodInfo : methodInfo2),
						new CodeInstruction(OpCodes.Stloc_S, operand)
					};
					list.InsertRange(num2 + 1, list2);
					num = num2 + list2.Count;
				}
			}
		}
		return list;
	}

	private static CodeInstruction? FindListenerLoadBackwards(List<CodeInstruction> code, int callIndex)
	{
		int num = callIndex - 1;
		while (num >= 0 && num > callIndex - 10)
		{
			if (code[num].opcode == OpCodes.Ldarg_2)
			{
				return code[num - 1];
			}
			num--;
		}
		return null;
	}
}
