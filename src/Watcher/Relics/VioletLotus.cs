using System.Collections.Generic;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Core;
using Watcher.Code.Events;
using Watcher.Code.Stances;

namespace Watcher.Code.Relics;

[Pool(typeof(WatcherRelicPool))]
public sealed class VioletLotus : WatcherRelicModel, IModifyCalmEnergyGain
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips => new global::_003C_003Ez__ReadOnlyArray<IHoverTip>(new IHoverTip[2]
	{
		WatcherHoverTipFactory.FromStance<CalmStance>(),
		HoverTipFactory.ForEnergy((RelicModel)(object)this)
	});

	public override RelicRarity Rarity => RelicRarity.Ancient;

	public int ModifyCalmEnergyGain(Player player, int amount)
	{
		if (((RelicModel)(object)this).Owner != player)
		{
			return amount;
		}
		return amount + 1;
	}
}
