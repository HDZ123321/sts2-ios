using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Stances;

namespace Watcher.Code.Core;

public class WatcherModelDb
{
	public static T WatcherStance<T>() where T : WatcherStanceModel
	{
		return ModelDb.GetById<T>(ModelDb.GetId<T>());
	}
}
