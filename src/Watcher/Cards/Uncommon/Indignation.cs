using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Commands;
using Watcher.Code.Extensions;
using Watcher.Code.Stances;

namespace Watcher.Code.Cards.Uncommon;

[Pool(typeof(WatcherCardPool))]
public sealed class Indignation : WatcherCardModel
{
	public Indignation()
		: base(1, CardType.Skill, CardRarity.Uncommon, TargetType.AllEnemies)
	{
		((ConstructedCardModel)this).WithPower<VulnerablePower>(3, 2);
		WithStanceTip<WrathStance>();
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)(object)this).CombatState != null)
		{
			if (((CardModel)(object)this).Owner.IsInWatcherStance<WrathStance>())
			{
				await CommonActions.Apply<VulnerablePower>(ctx, (IEnumerable<Creature>)((CardModel)(object)this).CombatState.HittableEnemies, DynamicVarSource.op_Implicit((CardModel)(object)this), false);
			}
			else
			{
				await StanceCmd.EnterWrath(ctx, ((CardModel)(object)this).Owner, cardPlay.Card);
			}
		}
	}
}
