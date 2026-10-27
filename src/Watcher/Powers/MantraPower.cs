using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;
using Watcher.Code.Commands;
using Watcher.Code.Core;
using Watcher.Code.Stances;

namespace Watcher.Code.Powers;

public sealed class MantraPower : WatcherPowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new global::_003C_003Ez__ReadOnlyArray<IHoverTip>(new IHoverTip[2]
	{
		WatcherHoverTipFactory.FromStance<DivinityStance>(),
		HoverTipFactory.FromPower<MantraPower>((int?)null)
	});

	public override async Task AfterPowerAmountChanged(PlayerChoiceContext ctx, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		Player player = ((PowerModel)(object)this).Owner.Player;
		if (power is MantraPower && !(amount <= 0m) && applier == ((PowerModel)(object)this).Owner && player != null)
		{
			SfxCmd.Play("res://Watcher/audio/mantra_gain.ogg");
			int num = ((PowerModel)(object)this).Amount / 10;
			if (num > 0)
			{
				decimal num2 = (decimal)num * 10m;
				await PowerCmd.ModifyAmount(ctx, (PowerModel)(object)this, -num2, ((PowerModel)(object)this).Owner, cardSource);
				await StanceCmd.EnterDivinity(ctx, player, cardSource);
			}
		}
	}
}
