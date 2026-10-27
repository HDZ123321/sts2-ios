using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Core;
using Watcher.Code.Events;
using Watcher.Code.Stances;

namespace Watcher.Code.Powers;

public sealed class RushdownPower : WatcherPowerModel, IOnStanceChange
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new global::_003C_003Ez__ReadOnlySingleElementList<IHoverTip>(WatcherHoverTipFactory.FromStance<WrathStance>());

	public async Task OnStanceChange(PlayerChoiceContext ctx, Player player, WatcherStanceModel oldStance, WatcherStanceModel newStance)
	{
		if (player.Creature == ((PowerModel)(object)this).Owner && newStance is WrathStance)
		{
			await CardPileCmd.Draw(ctx, ((PowerModel)(object)this).Amount, player);
			((PowerModel)(object)this).Flash();
		}
	}
}
