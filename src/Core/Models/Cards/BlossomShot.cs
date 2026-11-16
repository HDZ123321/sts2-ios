using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class BlossomShot : CardModel
{
	protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.Shoot };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new global::_003C_003Ez__ReadOnlyArray<IHoverTip>(new IHoverTip[2]
	{
		base.ArrowHoverTip,
		HoverTipFactory.FromPower<StretchPower>()
	});

	protected override bool ShouldGlowRedInternal => !ArrowCmd.PlayerHasAnyArrows(base.Owner);

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DamageVar(11m, ValueProp.Move | ValueProp.FromArrow));

	public BlossomShot()
		: base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		if (!cardPlay.Target.IsDead)
		{
			await ArrowCmd.ShootArrowAtTarget(cardPlay.Target, base.Owner, choiceContext, this, ActionToPerform);
		}
		async Task ActionToPerform()
		{
			int num = (await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this, cardPlay).FromArrow()
				.Targeting(cardPlay.Target)
				.WithAttackerAnim(Fool.GetShootBowAnimIfApplicable(base.Owner.Character), Fool.GetShootBowDelayIfApplicable(base.Owner.Character))
				.WithAttackerFx(null, "event:/sfx/characters/fool/fool_shot")
				.WithHitFx("vfx/vfx_attack_slash")
				.Execute(choiceContext)).Results.SelectMany((List<DamageResult> r) => r).Sum((DamageResult r) => r.TotalDamage + r.OverkillDamage);
			await PowerCmd.Apply<StretchPower>(choiceContext, base.Owner.Creature, num, base.Owner.Creature, this);
		}
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Damage.UpgradeValueBy(3m);
	}
}
