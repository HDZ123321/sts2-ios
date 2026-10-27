using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Models;

using BaseLib.Utils;

namespace BaseLib.Abstracts;

/// <summary>
/// Card pool that fills itself from the registry: every card carrying
/// <c>[Pool(typeof(ThisPool))]</c> ends up here, so mod cards do not have to be
/// listed twice.
/// </summary>
public abstract class CustomCardPoolModel : CardPoolModel
{
	public virtual float H => 0f;

	public virtual float S => 0f;

	public virtual float V => 0f;

	public virtual string? BigEnergyIconPath => null;

	public virtual string? TextEnergyIconPath => null;

	public override string EnergyColorName => Title.ToLowerInvariant();

	public override string CardFrameMaterialPath => "card_frame_" + EnergyColorName;

	protected override CardModel[] GenerateAllCards()
	{
		return WatcherRegistry.CardsFor(GetType()).ToArray();
	}
}
