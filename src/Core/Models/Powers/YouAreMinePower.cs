using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace MegaCrit.Sts2.Core.Models.Powers;

/// <summary>
/// Power applied as part of Queen's Dominion in Dread.
/// </summary>
public class YouAreMinePower : PowerModel
{
	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Single;

	protected override IEnumerable<IHoverTip> ExtraHoverTips
	{
		get
		{
			List<IHoverTip> list = new List<IHoverTip>();
			list.AddRange(HoverTipFactory.FromPowerWithPowerHoverTips<WeakPower>(base.Amount));
			list.AddRange(HoverTipFactory.FromPowerWithPowerHoverTips<VulnerablePower>(base.Amount));
			list.AddRange(HoverTipFactory.FromPowerWithPowerHoverTips<FrailPower>(base.Amount));
			return new _003C_003Ez__ReadOnlyList<IHoverTip>(list);
		}
	}

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player == base.Owner.Player)
		{
			PlayerCombatState? playerCombatState = player.PlayerCombatState;
			if (playerCombatState != null && playerCombatState.TurnNumber >= 2)
			{
				await PowerCmd.Apply<WeakPower>(choiceContext, player.Creature, base.Amount, null, null);
				await PowerCmd.Apply<VulnerablePower>(choiceContext, player.Creature, base.Amount, null, null);
				await PowerCmd.Apply<FrailPower>(choiceContext, player.Creature, base.Amount, null, null);
				await PowerCmd.Remove(this);
			}
		}
	}
}
