using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Watcher.Code.Compatibility;

public static class CardCmdCompatibility
{
	private delegate Task<CardPileAddResult?> ExhaustDel(PlayerChoiceContext ctx, CardModel card, bool ethereal, bool skipVisuals);

	private delegate Task OldExhaustDel(PlayerChoiceContext ctx, CardModel card, bool ethereal, bool skipVisuals);

	private static readonly ExhaustDel ExhaustImpl = BuildExhaust();

	public static Task<CardPileAddResult?> Exhaust(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal = false, bool skipVisuals = false)
	{
		return ExhaustImpl(choiceContext, card, causedByEthereal, skipVisuals);
	}

	private static ExhaustDel BuildExhaust()
	{
		Type[] types = new Type[4]
		{
			typeof(PlayerChoiceContext),
			typeof(CardModel),
			typeof(bool),
			typeof(bool)
		};
		MethodInfo methodInfo = typeof(CardCmd).GetMethod("Exhaust", BindingFlags.Static | BindingFlags.Public, null, types, null) ?? throw new MissingMethodException("CardCmd.Exhaust not found");
		ParameterExpression parameterExpression = Expression.Parameter(typeof(PlayerChoiceContext), "ctx");
		ParameterExpression parameterExpression2 = Expression.Parameter(typeof(CardModel), "card");
		ParameterExpression parameterExpression3 = Expression.Parameter(typeof(bool), "ethereal");
		ParameterExpression parameterExpression4 = Expression.Parameter(typeof(bool), "skipVisuals");
		MethodCallExpression body = Expression.Call(methodInfo, parameterExpression, parameterExpression2, parameterExpression3, parameterExpression4);
		if (methodInfo.ReturnType == typeof(Task<CardPileAddResult?>))
		{
			return Expression.Lambda<ExhaustDel>(body, new ParameterExpression[4] { parameterExpression, parameterExpression2, parameterExpression3, parameterExpression4 }).Compile();
		}
		if (typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
		{
			OldExhaustDel oldDel = Expression.Lambda<OldExhaustDel>(body, new ParameterExpression[4] { parameterExpression, parameterExpression2, parameterExpression3, parameterExpression4 }).Compile();
			return async delegate(PlayerChoiceContext c, CardModel cd, bool e, bool s)
			{
				await oldDel(c, cd, e, s);
				return (CardPileAddResult?)null;
			};
		}
		throw new MissingMethodException($"CardCmd.Exhaust has unexpected return type {methodInfo.ReturnType}");
	}
}
