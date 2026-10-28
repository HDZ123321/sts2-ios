using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;

using BaseLib.Utils;

namespace BaseLib.Abstracts;

public abstract class CustomRelicPoolModel : RelicPoolModel, CustomPoolMarker
{
	protected override IEnumerable<RelicModel> GenerateAllRelics()
	{
		return WatcherRegistry.RelicsFor(GetType());
	}
}
