using System;
using System.Collections.Generic;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BaseLib.Abstracts;

/// <summary>
/// Card base that collects its contents from builder calls instead of field
/// initialisers.  Everything the builders record is folded into the native
/// virtuals, so the rest of the game sees an ordinary <see cref="T:MegaCrit.Sts2.Core.Models.CardModel"/>.
/// </summary>
public abstract class ConstructedCardModel : CardModel
{
	private readonly List<DynamicVar> _constructedVars = new List<DynamicVar>();
	private readonly List<TooltipSource> _tooltipSources = new List<TooltipSource>();
	private readonly List<Func<CardModel, IEnumerable<IHoverTip>>> _tooltipFactories = new List<Func<CardModel, IEnumerable<IHoverTip>>>();
	private readonly HashSet<CardTag> _constructedTags = new HashSet<CardTag>();
	private readonly HashSet<CardKeyword> _constructedKeywords = new HashSet<CardKeyword>();
	private readonly HashSet<CardKeyword> _upgradeOnlyKeywords = new HashSet<CardKeyword>();
	private int _costUpgradeDelta;

	protected ConstructedCardModel(int canonicalEnergyCost, CardType type, CardRarity rarity, TargetType targetType, bool shouldShowInCardLibrary, bool alwaysOwnPortrait)
		: base(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
	{
	}

	/// <summary>Overridden by the mod's own card bases; empty means "use the base game layout".</summary>
	public virtual string CustomPortraitPath => string.Empty;

	public override string PortraitPath
	{
		get
		{
			string custom = CustomPortraitPath;
			if (string.IsNullOrEmpty(custom))
			{
				return base.PortraitPath;
			}
			return custom.StartsWith("res://", StringComparison.Ordinal) ? custom : "res://" + custom;
		}
	}

	protected override IEnumerable<DynamicVar> CanonicalVars => _constructedVars;

	protected override HashSet<CardTag> CanonicalTags => _constructedTags;

	public override IEnumerable<CardKeyword> CanonicalKeywords
	{
		get
		{
			foreach (CardKeyword keyword in _constructedKeywords)
			{
				yield return keyword;
			}
			CardModel canonical = this;
			if (canonical.IsUpgraded)
			{
				foreach (CardKeyword keyword2 in _upgradeOnlyKeywords)
				{
					yield return keyword2;
				}
			}
		}
	}

	protected override IEnumerable<IHoverTip> ExtraHoverTips
	{
		get
		{
			foreach (TooltipSource source in _tooltipSources)
			{
				yield return source.Resolve(this);
			}
			foreach (Func<CardModel, IEnumerable<IHoverTip>> factory in _tooltipFactories)
			{
				foreach (IHoverTip tip in factory(this))
				{
					yield return tip;
				}
			}
		}
	}

	protected override void OnUpgrade()
	{
		foreach (DynamicVar variable in _constructedVars)
		{
			if (variable.HasUpgradeAmount())
			{
				variable.UpgradeValueBy(variable.GetUpgradeAmount());
			}
		}
		if (_costUpgradeDelta != 0)
		{
			EnergyCost.UpgradeBy(_costUpgradeDelta);
		}
	}

	public ConstructedCardModel WithVar(DynamicVar variable)
	{
		_constructedVars.Add(variable);
		return this;
	}

	public ConstructedCardModel WithVars(params DynamicVar[] variables)
	{
		foreach (DynamicVar variable in variables)
		{
			_constructedVars.Add(variable);
		}
		return this;
	}

	public ConstructedCardModel WithVars(IEnumerable<DynamicVar> variables)
	{
		foreach (DynamicVar variable in variables)
		{
			_constructedVars.Add(variable);
		}
		return this;
	}

	public ConstructedCardModel WithTip(TooltipSource source)
	{
		_tooltipSources.Add(source);
		return this;
	}

	public ConstructedCardModel WithTips(Func<CardModel, IEnumerable<IHoverTip>> factory)
	{
		_tooltipFactories.Add(factory);
		return this;
	}

	public ConstructedCardModel WithTips(params IHoverTip[] tips)
	{
		_tooltipFactories.Add(delegate
		{
			return tips;
		});
		return this;
	}

	public ConstructedCardModel WithDamage(int baseValue, int upgrade)
	{
		DamageVar variable = new DamageVar("Damage", baseValue, ValueProp.Move);
		if (upgrade != 0)
		{
			variable.WithUpgrade(upgrade);
		}
		return WithVar(variable);
	}

	public ConstructedCardModel WithBlock(int baseValue, int upgrade)
	{
		BlockVar variable = new BlockVar("Block", baseValue, ValueProp.Move);
		if (upgrade != 0)
		{
			variable.WithUpgrade(upgrade);
		}
		return WithVar(variable);
	}

	public ConstructedCardModel WithCards(int baseValue, int upgrade)
	{
		CardsVar variable = new CardsVar(baseValue);
		if (upgrade != 0)
		{
			variable.WithUpgrade(upgrade);
		}
		return WithVar(variable);
	}

	public ConstructedCardModel WithEnergy(int baseValue, int upgrade)
	{
		EnergyVar variable = new EnergyVar(baseValue);
		if (upgrade != 0)
		{
			variable.WithUpgrade(upgrade);
		}
		return WithVar(variable);
	}

	public ConstructedCardModel WithEnergyTip()
	{
		return WithTips(delegate (CardModel card)
		{
			return new IHoverTip[1] { HoverTipFactory.ForEnergy(card) };
		});
	}

	public ConstructedCardModel WithKeyword(CardKeyword keyword)
	{
		_constructedKeywords.Add(keyword);
		return this;
	}

	public ConstructedCardModel WithKeywords(params CardKeyword[] keywords)
	{
		foreach (CardKeyword keyword in keywords)
		{
			_constructedKeywords.Add(keyword);
		}
		return this;
	}

	public ConstructedCardModel WithTags(params CardTag[] tags)
	{
		foreach (CardTag tag in tags)
		{
			_constructedTags.Add(tag);
		}
		return this;
	}

	public ConstructedCardModel WithCostUpgradeBy(int delta)
	{
		_costUpgradeDelta = delta;
		return this;
	}

	/// <summary>
	/// Brilliance-shaped damage: <c>baseValue + extraValue * multiplier</c>, where the
	/// multiplier is recomputed from live combat state every time the value is read.
	/// </summary>
	public ConstructedCardModel WithCalculatedDamage(int baseValue, Func<CardModel, Creature, decimal> multiplier, ValueProp props, int extraValue, int upgradeExtra)
	{
		CalculationBaseVar baseVar = new CalculationBaseVar(baseValue);
		ExtraDamageVar extraVar = new ExtraDamageVar(extraValue);
		if (upgradeExtra != 0)
		{
			extraVar.WithUpgrade(upgradeExtra);
		}
		CalculatedVar calculated = new CalculatedDamageVar(props).WithMultiplier(delegate (CardModel card, Creature? _)
		{
			return multiplier(card, card.Owner.Creature);
		});
		return WithVars(baseVar, extraVar, calculated);
	}

	/// <summary>Spirit Shield-shaped block, same arithmetic as the damage variant.</summary>
	public ConstructedCardModel WithCalculatedBlock(int baseValue, int extraValue, Func<CardModel, Creature, decimal> multiplier, ValueProp props, int upgradeBase, int upgradeExtra)
	{
		CalculationBaseVar baseVar = new CalculationBaseVar(baseValue);
		if (upgradeBase != 0)
		{
			baseVar.WithUpgrade(upgradeBase);
		}
		CalculationExtraVar extraVar = new CalculationExtraVar(extraValue);
		if (upgradeExtra != 0)
		{
			extraVar.WithUpgrade(upgradeExtra);
		}
		CalculatedVar calculated = new CalculatedBlockVar(props).WithMultiplier(delegate (CardModel card, Creature? _)
		{
			return multiplier(card, card.Owner.Creature);
		});
		return WithVars(baseVar, extraVar, calculated);
	}

	public ConstructedCardModel WithPower<T>(int baseValue, int upgrade) where T : PowerModel
	{
		PowerVar<T> variable = new PowerVar<T>(baseValue);
		if (upgrade != 0)
		{
			variable.WithUpgrade(upgrade);
		}
		return WithVar(variable);
	}

	public ConstructedCardModel WithPower<T>(int baseValue, bool showTooltip = true) where T : PowerModel
	{
		return WithPower<T>(baseValue, 0);
	}

	public ConstructedCardModel WithPower<T>(int baseValue, int upgrade, bool showTooltip) where T : PowerModel
	{
		return WithPower<T>(baseValue, upgrade);
	}

	public ConstructedCardModel WithVar(string name, int baseValue, int upgrade)
	{
		DynamicVar variable = new DynamicVar(name, baseValue);
		if (upgrade != 0)
		{
			variable.WithUpgrade(upgrade);
		}
		return WithVar(variable);
	}

	/// <summary>
	/// Adds a keyword that only shows up once the card is upgraded.
	/// </summary>
	public ConstructedCardModel WithKeyword(CardKeyword keyword, UpgradeType upgradeType)
	{
		if (upgradeType == UpgradeType.Upgrade)
		{
			_upgradeOnlyKeywords.Add(keyword);
		}
		else
		{
			_constructedKeywords.Add(keyword);
		}
		return this;
	}
}
