using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MegaCrit.Sts2.Core.Models.Cards;

public sealed class DingyDagger : CardModel
{
	public const int fineUpgradeLevel = 3;

	public const int pristineUpgradeLevel = 6;

	public override string Title
	{
		get
		{
			int currentUpgradeLevel = base.CurrentUpgradeLevel;
			LocString locString = ((currentUpgradeLevel < 3) ? new LocString("cards", "DINGY_DAGGER.title.dingy") : ((currentUpgradeLevel >= 6) ? new LocString("cards", "DINGY_DAGGER.title.pristine") : new LocString("cards", "DINGY_DAGGER.title.fine")));
			string formattedText = locString.GetFormattedText();
			if (base.CurrentUpgradeLevel > 0)
			{
				return $"{formattedText}+{base.CurrentUpgradeLevel}";
			}
			return formattedText;
		}
	}

	public override string PortraitPath => ImageHelper.GetImagePath($"atlases/card_atlas.sprites/{Pool.Title.ToLowerInvariant()}/{GetPortraitFilename()}.tres");

	protected override string PortraitPngPath => ImageHelper.GetImagePath($"packed/card_portraits/{Pool.Title.ToLowerInvariant()}/{GetPortraitFilename()}.png");

	public override int MaxUpgradeLevel => 999999999;

	protected override IEnumerable<DynamicVar> CanonicalVars => new global::_003C_003Ez__ReadOnlySingleElementList<DynamicVar>(new DamageVar(7m, ValueProp.Move));

	public DingyDagger()
		: base(0, CardType.Attack, CardRarity.Event, TargetType.AnyEnemy)
	{
	}

	private string GetPortraitFilename()
	{
		int currentUpgradeLevel = base.CurrentUpgradeLevel;
		if (currentUpgradeLevel >= 3)
		{
			if (currentUpgradeLevel < 6)
			{
				return "fine_dagger";
			}
			return "pristine_dagger";
		}
		return "dingy_dagger";
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).FromCard(this, cardPlay).Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_slash")
			.Execute(choiceContext);
	}

	protected override void OnUpgrade()
	{
		base.DynamicVars.Damage.UpgradeValueBy(3 + base.CurrentUpgradeLevel);
	}
}
