using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Commands;
using Watcher.Code.Core;
using Watcher.Code.Stances;

namespace Watcher.Code.Potions;

[Pool(typeof(WatcherPotionPool))]
public class WrathPotion : WatcherPotionModel
{
	public override PotionRarity Rarity => PotionRarity.Uncommon;

	public override PotionUsage Usage => PotionUsage.CombatOnly;

	public override TargetType TargetType => TargetType.AnyPlayer;

	public override IEnumerable<IHoverTip> ExtraHoverTips => new global::_003C_003Ez__ReadOnlySingleElementList<IHoverTip>(WatcherHoverTipFactory.FromStance<WrathStance>());

	protected override async Task OnUse(PlayerChoiceContext ctx, Creature? target)
	{
		if (target?.Player != null)
		{
			await StanceCmd.EnterWrath(ctx, target.Player, null);
		}
	}
}
