using System;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

/// <summary>
/// Potion base with mod-friendly atlas paths.
/// </summary>
public abstract class CustomPotionModel : PotionModel
{
	public virtual string CustomPackedImagePath => string.Empty;

	public virtual string CustomPackedOutlinePath => string.Empty;

	protected override string PackedImagePath
	{
		get
		{
			string custom = CustomPackedImagePath;
			if (string.IsNullOrEmpty(custom))
			{
				return base.PackedImagePath;
			}
			return custom.StartsWith("res://", StringComparison.Ordinal) ? custom : "res://" + custom;
		}
	}

	protected override string PackedOutlinePath
	{
		get
		{
			string custom = CustomPackedOutlinePath;
			if (string.IsNullOrEmpty(custom))
			{
				return base.PackedOutlinePath;
			}
			return custom.StartsWith("res://", StringComparison.Ordinal) ? custom : "res://" + custom;
		}
	}
}
