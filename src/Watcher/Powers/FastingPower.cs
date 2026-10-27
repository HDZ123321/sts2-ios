using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Abstract;

namespace Watcher.Code.Powers;

public class FastingPower : WatcherPowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player == ((PowerModel)(object)this).Owner.Player && ((PowerModel)(object)this).Owner.Player.PlayerCombatState != null)
		{
			((PowerModel)(object)this).Owner.Player.PlayerCombatState.LoseEnergy(((PowerModel)(object)this).Amount);
			await Task.CompletedTask;
			((PowerModel)(object)this).Flash();
		}
	}
}
