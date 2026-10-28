using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;

using BaseLib.Utils;

namespace BaseLib.Abstracts;

public abstract class CustomPotionPoolModel : PotionPoolModel, CustomPoolMarker
{
	protected override IEnumerable<PotionModel> GenerateAllPotions()
	{
		return WatcherRegistry.PotionsFor(GetType());
	}
}
