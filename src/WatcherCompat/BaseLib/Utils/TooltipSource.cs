using System;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

/// <summary>
/// A hover tip that depends on the card instance looking at it, so the text can
/// quote live dynamic vars instead of the canonical values.
/// </summary>
public sealed class TooltipSource
{
	private readonly Func<CardModel, IHoverTip> _factory;

	public TooltipSource(Func<CardModel, IHoverTip> factory)
	{
		_factory = factory;
	}

	public IHoverTip Resolve(CardModel card)
	{
		return _factory(card);
	}

	public static TooltipSource op_Implicit(IHoverTip tip)
	{
		return new TooltipSource(delegate { return tip; });
	}

	public static TooltipSource op_Implicit(StaticHoverTip tip)
	{
		return new TooltipSource(delegate { return HoverTipFactory.Static(tip); });
	}

	/// <summary>
	/// Resolves a model type to whatever tip fits it.  The mod passes card, power
	/// and relic types through the same call, so the lookup walks all three.
	/// </summary>
	public static TooltipSource op_Implicit(Type type)
	{
		return new TooltipSource(delegate
		{
			MegaCrit.Sts2.Core.Models.ModelId id = ModelDb.GetId(type);
			CardModel? card = ModelDb.GetByIdOrNull<CardModel>(id);
			if (card != null)
			{
				return HoverTipFactory.FromCard(card);
			}
			PowerModel? power = ModelDb.GetByIdOrNull<PowerModel>(id);
			if (power != null)
			{
				return HoverTipFactory.FromPower(power);
			}
			RelicModel? relic = ModelDb.GetByIdOrNull<RelicModel>(id);
			if (relic != null)
			{
				return HoverTipFactory.FromRelic(relic).FirstOrDefault() ?? BaseLibTip.Scry;
			}
			return BaseLibTip.Scry;
		});
	}

	public static TooltipSource op_Implicit(CardKeyword keyword)
	{
		return new TooltipSource(delegate { return HoverTipFactory.FromKeyword(keyword); });
	}

	public static TooltipSource op_Implicit(CardModel card)
	{
		return new TooltipSource(delegate { return HoverTipFactory.FromCard(card); });
	}

	public static TooltipSource op_Implicit(PowerModel power)
	{
		return new TooltipSource(delegate { return HoverTipFactory.FromPower(power); });
	}

	public static TooltipSource op_Implicit(RelicModel relic)
	{
		return new TooltipSource(delegate { return HoverTipFactory.FromRelic(relic).FirstOrDefault() ?? BaseLibTip.Scry; });
	}
}
