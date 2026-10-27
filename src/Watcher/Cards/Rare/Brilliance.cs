using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Watcher.Code.Abstract;
using Watcher.Code.Character;
using Watcher.Code.Powers;
using Watcher.Code.Stances;

namespace Watcher.Code.Cards.Rare;

[Pool(typeof(WatcherCardPool))]
public sealed class Brilliance : WatcherCardModel
{
	public override bool ShouldReceiveCombatHooks => true;

	public Brilliance()
		: base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
	{
		((ConstructedCardModel)this).WithCalculatedDamage(12, (Func<CardModel, Creature, decimal>)MantraGainedThisCombat, ValueProp.Move, 4, 0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(typeof(MantraPower)));
		WithStanceTip<DivinityStance>();
	}

	private static decimal MantraGainedThisCombat(CardModel card, Creature? creature)
	{
		return (from e in CombatManager.Instance.History.Entries.OfType<PowerReceivedEntry>()
			where e.Power is MantraPower && e.Applier?.Player == card.Owner && e.Amount > 0m
			select e).Sum((PowerReceivedEntry e) => e.Amount);
	}

	protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
	{
		await CommonActions.CardAttack((CardModel)(object)this, play, 1, (string)null, (string)null, (string)null).WithHitFx("vfx/vfx_attack_slash").Execute(context);
	}
}
