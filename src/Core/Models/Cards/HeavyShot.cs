using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class HeavyShot : CardModel
{
	private const string _aimMultiplierKey = "AimMultiplier";

	protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.Shoot };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new global::_003C_003Ez__ReadOnlyArray<IHoverTip>(new IHoverTip[2]
	{
		base.ArrowHoverTip,
		HoverTipFactory.FromPower<AimPower>()
	});

	protected override bool ShouldGlowRedInternal => !ArrowCmd.PlayerHasAnyArrows(base.Owner);

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlyArray<DynamicVar>(new DynamicVar[2]
	{
		new DamageVar(20m, ValueProp.Move | ValueProp.FromArrow),
		new DynamicVar("AimMultiplier", 2m)
	});

	public HeavyShot()
		: base(2, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		AttackCommand attackCommand = DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromArrow().FromCard(this, cardPlay)
			.WithAttackerAnim(Fool.GetShootBowAnimIfApplicable(base.Owner.Character), Fool.GetShootBowDelayIfApplicable(base.Owner.Character))
			.WithAttackerFx(null, "event:/sfx/characters/fool/fool_shot")
			.Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash");
		await ArrowCmd.ShootArrowAtTarget(cardPlay.Target, base.Owner, attackCommand, choiceContext);
	}

	public decimal ModifyAimAmount(decimal originalAmount)
	{
		return originalAmount * base.DynamicVars["AimMultiplier"].BaseValue;
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Damage.UpgradeValueBy(5m);
	}
}
