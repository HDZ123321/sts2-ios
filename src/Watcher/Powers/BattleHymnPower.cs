using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Watcher.Code.Abstract;
using Watcher.Code.Cards.Token;
using Watcher.Code.Commands;

namespace Watcher.Code.Powers;

public sealed class BattleHymnPower : WatcherPowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (((PowerModel)(object)this).Owner.Player == player)
		{
			await WatcherCmd.GiveCards<Smite>(((PowerModel)(object)this).Owner.Player, ((PowerModel)(object)this).Amount, PileType.Hand, CardPilePosition.Top, 0.6f, CardPreviewStyle.HorizontalLayout, false, true);
			((PowerModel)(object)this).Flash();
		}
	}
}
