using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Core;
using Watcher.Code.Stances;

namespace Watcher.Code.Events;

public class WatcherSubscriber
{
	public static void Subscribe()
	{
		ModHelper.SubscribeForCombatStateHooks("Watcher", CollectModels2);
	}

	private static IEnumerable<AbstractModel> CollectModels2(CombatState combatState)
	{
		return from s in combatState.Players.Select(WatcherModel.GetStanceModel)
			where !(s is NoStance)
			select s;
	}
}
