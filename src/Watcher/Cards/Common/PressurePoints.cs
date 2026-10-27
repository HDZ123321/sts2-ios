using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Compatibility;
using Watcher.Code.Powers;

namespace Watcher.Code.Cards.Common;

[Pool(typeof(WatcherCardPool))]
public sealed class PressurePoints : WatcherCardModel
{
	public PressurePoints()
		: base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
	{
		((ConstructedCardModel)this).WithPower<MarkPower>(8, 3);
	}

	private static decimal Calc(Creature? target)
	{
		return ((decimal?)target?.GetPowerAmount<MarkPower>()) ?? 0m;
	}

	protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Apply<MarkPower>(ctx, (CardModel)(object)this, cardPlay, false);
		IReadOnlyList<Creature> readOnlyList = ((CardModel)(object)this).CombatState?.HittableEnemies ?? Array.Empty<Creature>();
		foreach (Creature item in readOnlyList)
		{
			decimal num = Calc(item);
			if (!(num <= 0m))
			{
				await CompatibilityCreatureCmd.Damage(ctx, item, num, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, (CardModel)(object)this, cardPlay);
			}
		}
	}
}
