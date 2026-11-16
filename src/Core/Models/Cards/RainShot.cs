using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.FoolArrows;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class RainShot : CardModel
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips => new global::_003C_003Ez__ReadOnlySingleElementList<IHoverTip>(base.ArrowHoverTip);

	protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.Shoot };

	protected override bool ShouldGlowRedInternal => !ArrowCmd.PlayerHasAnyArrows(base.Owner);

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DamageVar(12m, ValueProp.Move | ValueProp.FromArrow));

	public RainShot()
		: base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		if (cardPlay.Target.IsDead)
		{
			return;
		}
		ArrowSlot arrow = await ArrowCmd.TryShootArrow(base.Owner, choiceContext, this);
		if (arrow == null)
		{
			return;
		}
		AttackContext context = await AttackCommand.CreateContextAsync(base.CombatState, choiceContext, cardPlay);
		try
		{
			await ArrowCmd.ImpaleTarget(cardPlay.Target, arrow, ActionToPerform, choiceContext, this);
		}
		finally
		{
			if (context != null)
			{
				await context.DisposeAsync();
			}
		}
		async Task ActionToPerform()
		{
			SfxCmd.Play("event:/sfx/characters/fool/fool_shot");
			await CreatureCmd.TriggerAnim(base.Owner.Creature, Fool.GetShootBowAnimIfApplicable(base.Owner.Character), Fool.GetShootBowDelayIfApplicable(base.Owner.Character));
			List<DamageResult> results = (await CreatureCmd.Damage(choiceContext, cardPlay.Target, base.DynamicVars.Damage.BaseValue, ValueProp.Move | ValueProp.FromArrow, this, cardPlay)).ToList();
			context.AddHit(results);
			await PowerCmd.Remove<AimPower>(base.Owner.Creature);
			await PowerCmd.Remove<QuietusPower>(base.Owner.Creature);
			DamageResult damageResult = results.FirstOrDefault();
			if (damageResult != null)
			{
				List<Creature> list = (from e in base.CombatState.GetTeammatesOf(damageResult.Receiver).Except(new global::_003C_003Ez__ReadOnlySingleElementList<Creature>(cardPlay.Target))
					where e.IsHittable
					select e).ToList();
				if (list.Count != 0)
				{
					IEnumerable<DamageResult> results2 = await CreatureCmd.Damage(choiceContext, list, damageResult.TotalDamage + damageResult.OverkillDamage, ValueProp.Move, base.Owner.Creature, this, cardPlay, ModifyDamageHookType.Cap);
					context.AddHit(results2);
				}
			}
		}
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Damage.UpgradeValueBy(3m);
	}
}
