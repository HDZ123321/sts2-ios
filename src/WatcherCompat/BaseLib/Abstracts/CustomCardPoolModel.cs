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
public abstract class CustomCardPoolModel : CardPoolModel, CustomPoolMarker
{
	public virtual float H => 0f;

	public virtual float S => 0f;

	public virtual float V => 0f;

	public virtual string? BigEnergyIconPath => null;

	public virtual string? TextEnergyIconPath => null;

	/// <summary>
	/// Vanilla builds every energy sprite and frame material from a colour name that the base
	/// game already ships, so a mod pool has to borrow one of those.  `ironclad` is the closest
	/// match for the Watcher's red/purple orb, and card_frame_red_mat.tres exists.
	/// </summary>
	public virtual string VanillaEnergyColor => "ironclad";

	public override string EnergyColorName => VanillaEnergyColor;

	/// <summary>
	/// The base game has one energy icon per vanilla character, so a mod pool's
	/// own colour name resolves to a sprite that does not exist.  Fall back to the
	/// mod-provided icon instead of rendering an empty orb.
	/// </summary>
	public override string EnergyIconPath
	{
		get
		{
			string path = base.EnergyIconPath;
			if (ResourceLoader.Exists(path))
			{
				return path;
			}
			if (!string.IsNullOrEmpty(BigEnergyIconPath) && ResourceLoader.Exists(BigEnergyIconPath))
			{
				return BigEnergyIconPath;
			}
			return path;
		}
	}

	/// <summary>
	/// The energy colour and the frame colour are separate names in the base game
	/// (ironclad ships <c>card_frame_red_mat.tres</c>, not <c>card_frame_ironclad_mat.tres</c>),
	/// so a mod pool cannot derive one from the other.
	/// </summary>
	public virtual string VanillaCardFrame => "card_frame_red";

	public override string CardFrameMaterialPath => VanillaCardFrame;

	protected override CardModel[] GenerateAllCards()
	{
		return WatcherRegistry.CardsFor(GetType()).ToArray();
	}
}
