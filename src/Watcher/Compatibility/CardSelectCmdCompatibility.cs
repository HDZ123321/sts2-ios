using System;
using System.Linq.Expressions;
using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.TestSupport;

namespace Watcher.Code.Compatibility;

public static class CardSelectCmdCompatibility
{
	private delegate IDisposable UseSelectorDel(ICardSelector selector, bool localOnly);

	private delegate IDisposable OneArgUseSelectorDel(ICardSelector selector);

	private static readonly UseSelectorDel UseSelectorImpl = BuildUseSelector();

	public static IDisposable UseSelector(ICardSelector selector, bool localOnly = false)
	{
		return UseSelectorImpl(selector, localOnly);
	}

	private static UseSelectorDel BuildUseSelector()
	{
		ParameterExpression parameterExpression = Expression.Parameter(typeof(ICardSelector), "selector");
		ParameterExpression parameterExpression2 = Expression.Parameter(typeof(bool), "localOnly");
		MethodInfo method = typeof(CardSelectCmd).GetMethod("UseSelector", BindingFlags.Static | BindingFlags.Public, null, new Type[2]
		{
			typeof(ICardSelector),
			typeof(bool)
		}, null);
		if (method != null)
		{
			return Expression.Lambda<UseSelectorDel>(Expression.Call(method, parameterExpression, parameterExpression2), new ParameterExpression[2] { parameterExpression, parameterExpression2 }).Compile();
		}
		MethodCallExpression body = Expression.Call(typeof(CardSelectCmd).GetMethod("UseSelector", BindingFlags.Static | BindingFlags.Public, null, new Type[1] { typeof(ICardSelector) }, null) ?? throw new MissingMethodException("CardSelectCmd.UseSelector not found"), parameterExpression);
		OneArgUseSelectorDel oneArgDel = Expression.Lambda<OneArgUseSelectorDel>(body, new ParameterExpression[1] { parameterExpression }).Compile();
		return (ICardSelector s, bool _) => oneArgDel(s);
	}
}
