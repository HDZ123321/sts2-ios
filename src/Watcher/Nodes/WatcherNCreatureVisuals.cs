using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Watcher.Code.Nodes;

[GlobalClass]
[ScriptPath("res://WatcherCode/Nodes/WatcherNCreatureVisuals.cs")]
public class WatcherNCreatureVisuals : NCreatureVisuals
{
	public new class MethodName : NCreatureVisuals.MethodName
	{
		public static readonly StringName PlayEyeAnimation = "PlayEyeAnimation";

		public new static readonly StringName _Ready = "_Ready";

		public static readonly StringName SetEyeStance = "SetEyeStance";
	}

	public new class PropertyName : NCreatureVisuals.PropertyName
	{
		public static readonly StringName IsSpineEye = "IsSpineEye";

		public static readonly StringName _eye = "_eye";
	}

	public new class SignalName : NCreatureVisuals.SignalName
	{
	}

	private Node2D? _eye;

	private static readonly System.Reflection.MethodInfo SetAnimationMethod = typeof(SpineAnimationAccess).GetMethods().First((System.Reflection.MethodInfo m) => m.Name == "SetAnimation" && m.GetParameters()[0].ParameterType == typeof(string));

	private MegaSprite? SpineEye { get; set; }

	private bool IsSpineEye
	{
		get
		{
			if (GodotObject.IsInstanceValid(_eye))
			{
				return _eye.GetClass() == "SpineSprite";
			}
			return false;
		}
	}

	private SpineAnimationAccess SpineEyeAnimation => new SpineAnimationAccess(SpineEye);

	private void PlayEyeAnimation(string animation)
	{
		ParameterInfo[] parameters = SetAnimationMethod.GetParameters();
		object[] array = new object[parameters.Length];
		array[0] = animation;
		for (int i = 1; i < parameters.Length; i++)
		{
			array[i] = (parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null);
		}
		object obj = SpineEyeAnimation;
		SetAnimationMethod.Invoke(obj, array);
	}

	public override void _Ready()
	{
		base._Ready();
		_eye = base._body.GetNodeOrNull<Node2D>("%SpineEye");
		if (IsSpineEye)
		{
			SpineEye = new MegaSprite(_eye);
			if (SpineEye.GetSkeleton()?.GetData() == null)
			{
				GD.PushWarning($"Spine eye skeleton data failed to load for {base.Name}, disabling spine animation.");
				SpineEye = null;
			}
			PlayEyeAnimation("None");
		}
	}

	public void SetEyeStance(string stance)
	{
		PlayEyeAnimation(stance);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<Godot.Bridge.MethodInfo> GetGodotMethodList()
	{
		return new List<Godot.Bridge.MethodInfo>(3)
		{
			new Godot.Bridge.MethodInfo(MethodName.PlayEyeAnimation, new Godot.Bridge.PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<Godot.Bridge.PropertyInfo>
			{
				new Godot.Bridge.PropertyInfo(Variant.Type.String, "animation", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new Godot.Bridge.MethodInfo(MethodName._Ready, new Godot.Bridge.PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new Godot.Bridge.MethodInfo(MethodName.SetEyeStance, new Godot.Bridge.PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<Godot.Bridge.PropertyInfo>
			{
				new Godot.Bridge.PropertyInfo(Variant.Type.String, "stance", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.PlayEyeAnimation && args.Count == 1)
		{
			PlayEyeAnimation(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName._Ready && args.Count == 0)
		{
			_Ready();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.SetEyeStance && args.Count == 1)
		{
			SetEyeStance(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName.PlayEyeAnimation)
		{
			return true;
		}
		if (method == MethodName._Ready)
		{
			return true;
		}
		if (method == MethodName.SetEyeStance)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName._eye)
		{
			_eye = VariantUtils.ConvertTo<Node2D>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		if (name == PropertyName.IsSpineEye)
		{
			value = VariantUtils.CreateFrom<bool>(IsSpineEye);
			return true;
		}
		if (name == PropertyName._eye)
		{
			value = VariantUtils.CreateFrom(in _eye);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<Godot.Bridge.PropertyInfo> GetGodotPropertyList()
	{
		return new List<Godot.Bridge.PropertyInfo>
		{
			new Godot.Bridge.PropertyInfo(Variant.Type.Object, PropertyName._eye, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new Godot.Bridge.PropertyInfo(Variant.Type.Bool, PropertyName.IsSpineEye, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName._eye, Variant.From(in _eye));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName._eye, out var value))
		{
			_eye = value.As<Node2D>();
		}
	}
}
