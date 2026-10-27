using System;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

/// <summary>
/// Power base with mod-friendly icon paths.  The base game derives both icons
/// from the model id, which does not exist on disk for baked-in content.
/// </summary>
public abstract class CustomPowerModel : PowerModel
{
	public virtual string CustomPackedIconPath => string.Empty;

	public virtual string CustomBigIconPath => string.Empty;

	public override string PackedIconPath
	{
		get
		{
			string custom = CustomPackedIconPath;
			if (string.IsNullOrEmpty(custom))
			{
				return base.PackedIconPath;
			}
			return custom.StartsWith("res://", StringComparison.Ordinal) ? custom : "res://" + custom;
		}
	}

	protected override string BigIconPath
	{
		get
		{
			string custom = CustomBigIconPath;
			if (string.IsNullOrEmpty(custom))
			{
				return base.BigIconPath;
			}
			return custom.StartsWith("res://", StringComparison.Ordinal) ? custom : "res://" + custom;
		}
	}
}
