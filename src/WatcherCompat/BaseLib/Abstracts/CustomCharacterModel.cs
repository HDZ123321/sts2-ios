using System;
using System.Collections.Generic;
using BaseLib.Patches.UI;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

/// <summary>
/// Character base that redirects every path the base game builds from the model
/// id onto a mod-supplied path.  An empty custom path falls back to the base
/// convention, so a partially customised character still loads.
/// </summary>
public abstract class CustomCharacterModel : CharacterModel
{
	public virtual string CustomVisualPath => string.Empty;

	public virtual string CustomTrailPath => string.Empty;

	public virtual string CustomIconPath => string.Empty;

	public virtual string CustomIconTexturePath => string.Empty;

	public virtual string CustomIconOutlineTexturePath => string.Empty;

	public virtual string CustomCharacterSelectIconPath => string.Empty;

	public virtual string CustomCharacterSelectLockedIconPath => string.Empty;

	public virtual string CustomCharacterSelectBg => string.Empty;

	public virtual string CustomCharacterSelectTransitionPath => string.Empty;

	public virtual string CustomMapMarkerPath => string.Empty;

	public virtual string CustomEnergyCounterPath => string.Empty;

	public virtual string CustomRestSiteAnimPath => string.Empty;

	public virtual string CustomMerchantAnimPath => string.Empty;

	public virtual string CustomArmPointingTexturePath => string.Empty;

	public virtual string CustomArmRockTexturePath => string.Empty;

	public virtual string CustomArmPaperTexturePath => string.Empty;

	public virtual string CustomArmScissorsTexturePath => string.Empty;

	public virtual string CustomAttackSfx => string.Empty;

	public virtual RelicIconData? CustomYummyCookie => null;

	private static string Pick(string custom, string fallback)
	{
		if (string.IsNullOrEmpty(custom))
		{
			return fallback;
		}
		return custom.StartsWith("res://", StringComparison.Ordinal) ? custom : "res://" + custom;
	}

	protected override string VisualsPath => Pick(CustomVisualPath, base.VisualsPath);

	public override string TrailPath => Pick(CustomTrailPath, base.TrailPath);

	protected override string IconPath => Pick(CustomIconPath, base.IconPath);

	protected override string IconTexturePath => Pick(CustomIconTexturePath, base.IconTexturePath);

	protected override string IconOutlineTexturePath => Pick(CustomIconOutlineTexturePath, base.IconOutlineTexturePath);

	protected override string CharacterSelectIconPath => Pick(CustomCharacterSelectIconPath, base.CharacterSelectIconPath);

	protected override string CharacterSelectLockedIconPath => Pick(CustomCharacterSelectLockedIconPath, base.CharacterSelectLockedIconPath);

	public override string CharacterSelectBg => Pick(CustomCharacterSelectBg, base.CharacterSelectBg);

	public override string CharacterSelectTransitionPath => Pick(CustomCharacterSelectTransitionPath, base.CharacterSelectTransitionPath);

	protected override string MapMarkerPath => Pick(CustomMapMarkerPath, base.MapMarkerPath);

	public override string EnergyCounterPath => Pick(CustomEnergyCounterPath, base.EnergyCounterPath);

	public override string RestSiteAnimPath => Pick(CustomRestSiteAnimPath, base.RestSiteAnimPath);

	public override string MerchantAnimPath => Pick(CustomMerchantAnimPath, base.MerchantAnimPath);

	protected override string ArmPointingTexturePath => Pick(CustomArmPointingTexturePath, base.ArmPointingTexturePath);

	protected override string ArmRockTexturePath => Pick(CustomArmRockTexturePath, base.ArmRockTexturePath);

	protected override string ArmPaperTexturePath => Pick(CustomArmPaperTexturePath, base.ArmPaperTexturePath);

	protected override string ArmScissorsTexturePath => Pick(CustomArmScissorsTexturePath, base.ArmScissorsTexturePath);

	public override string AttackSfx => Pick(CustomAttackSfx, base.AttackSfx);
}
