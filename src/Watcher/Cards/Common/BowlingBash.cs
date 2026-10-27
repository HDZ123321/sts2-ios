using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Watcher.Code.Abstract;
using Watcher.Code.Character;

namespace Watcher.Code.Cards.Common;

[Pool(typeof(WatcherCardPool))]
public sealed class BowlingBash : WatcherCardModel
{
	public BowlingBash()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
		((ConstructedCardModel)this).WithDamage(7, 3);
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ICombatState combatState = cardPlay.Target?.CombatState;
		if (combatState != null)
		{
			int hitCount = combatState.Enemies.Count((Creature e) => NCombatRoom.Instance?.GetCreatureNode(e)?.Visible ?? true);
			await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitCount(hitCount).WithHitFx("vfx/vfx_attack_slash")
				.Execute(choiceContext);
		}
	}
}
