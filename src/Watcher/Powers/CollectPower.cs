using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
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

public sealed class CollectPower : WatcherPowerModel
{
	public override PowerType Type => PowerType.Buff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
	{
		if (player == ((PowerModel)(object)this).Owner.Player)
		{
			await WatcherCmd.GiveCard<Miracle>(((PowerModel)(object)this).Owner.Player, PileType.Hand, CardPilePosition.Top, 0.6f, CardPreviewStyle.HorizontalLayout, true, true);
			await PowerCmd.Decrement((PowerModel)(object)this);
		}
	}
}
