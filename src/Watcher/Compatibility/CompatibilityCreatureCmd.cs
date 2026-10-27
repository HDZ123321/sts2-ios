using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Watcher.Code.Compatibility;

public static class CompatibilityCreatureCmd
{
	private delegate Task<IEnumerable<DamageResult>> DamageDel(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, CardModel cardSource, CardPlay? cardPlay);

	private static readonly DamageDel DamageD = Build();

	public static Task<IEnumerable<DamageResult>> Damage(PlayerChoiceContext choiceContext, Creature target, decimal amount, ValueProp props, CardModel cardSource, CardPlay? cardPlay)
	{
		return DamageD(choiceContext, target, amount, props, cardSource, cardPlay);
	}

	private static DamageDel Build()
	{
		Type[] array = new Type[5]
		{
			typeof(PlayerChoiceContext),
			typeof(Creature),
			typeof(decimal),
			typeof(ValueProp),
			typeof(CardModel)
		};
		Type? typeFromHandle = typeof(CreatureCmd);
		Type[] array2 = array;
		int num = 0;
		Type[] array3 = new Type[1 + array2.Length];
		ReadOnlySpan<Type> readOnlySpan = new ReadOnlySpan<Type>(array2);
		readOnlySpan.CopyTo(new Span<Type>(array3).Slice(num, readOnlySpan.Length));
		num += readOnlySpan.Length;
		array3[num] = typeof(CardPlay);
		MethodInfo method = typeFromHandle.GetMethod("Damage", BindingFlags.Static | BindingFlags.Public, null, array3, null);
		if (method != null)
		{
			return method.CreateDelegate<DamageDel>();
		}
		MethodInfo method2 = typeof(CreatureCmd).GetMethod("Damage", BindingFlags.Static | BindingFlags.Public, null, array, null) ?? throw new MissingMethodException("CreatureCmd.Damage(ctx, Creature, decimal, ValueProp, CardModel) not found.");
		ParameterExpression[] array4 = new ParameterExpression[6]
		{
			Expression.Parameter(typeof(PlayerChoiceContext), "choiceContext"),
			Expression.Parameter(typeof(Creature), "target"),
			Expression.Parameter(typeof(decimal), "amount"),
			Expression.Parameter(typeof(ValueProp), "props"),
			Expression.Parameter(typeof(CardModel), "cardSource"),
			Expression.Parameter(typeof(CardPlay), "cardPlay")
		};
		return Expression.Lambda<DamageDel>(Expression.Call(method2, array4[..^1].Cast<Expression>()), array4).Compile();
	}
}
