using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Cards.Variables;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Core;
using Watcher.Code.Extensions;
using Watcher.Code.Stances;

namespace Watcher.Code.Abstract;

public abstract class WatcherCardModel : ConstructedCardModel
{
	public sealed override string CustomPortraitPath => (Watcher.Code.Extensions.StringExtensions.RemovePrefix(((AbstractModel)this).Id.Entry).ToLowerInvariant() + ".png").CardImagePath();

	protected WatcherCardModel(int canonicalEnergyCost, CardType type, CardRarity rarity, TargetType targetType, bool shouldShowInCardLibrary = true)
		: base(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary, true)
	{
	}

	protected ConstructedCardModel WithPower<T>(int baseVal, int upgrade, bool showTooltip) where T : PowerModel
	{
		((ConstructedCardModel)this).WithVar(DynamicVarExtensions.WithUpgrade<DynamicVar>(new DynamicVar(typeof(T).Name, baseVal), (decimal)upgrade));
		if (showTooltip)
		{
			((ConstructedCardModel)this).WithTips((Func<CardModel, IEnumerable<IHoverTip>>)((CardModel e) => new global::_003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.FromPower<T>(DynamicVarSetExtensions.Power<T>(e.DynamicVars).IntValue))));
		}
		return (ConstructedCardModel)(object)this;
	}

	protected ConstructedCardModel WithScry(int baseVal, int upgrade)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		((ConstructedCardModel)this).WithVar((DynamicVar)(object)DynamicVarExtensions.WithUpgrade<ScryVar>(new ScryVar((decimal)baseVal), (decimal)upgrade));
		return (ConstructedCardModel)(object)this;
	}

	protected ConstructedCardModel WithPower<T>(int baseVal, bool showTooltip) where T : PowerModel
	{
		return WithPower<T>(baseVal, 0, showTooltip);
	}

	protected WatcherCardModel WithStanceTip<T>() where T : WatcherStanceModel
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		((ConstructedCardModel)this).WithTip(new TooltipSource((Func<CardModel, IHoverTip>)((CardModel _) => WatcherHoverTipFactory.FromStance<T>())));
		return this;
	}
}
