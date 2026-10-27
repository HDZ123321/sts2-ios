using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Helpers;

namespace Watcher.Code.Stances.Vfx;

[GlobalClass]
[ScriptPath("res://WatcherCode/Nodes/Stances/ScreenFlashEffect.cs")]
public class ScreenFlashEffect : CanvasLayer
{
	public new class MethodName : CanvasLayer.MethodName
	{
		public static readonly StringName Play = "Play";

		public new static readonly StringName _Ready = "_Ready";

		public new static readonly StringName _Process = "_Process";
	}

	public new class PropertyName : CanvasLayer.PropertyName
	{
		public static readonly StringName _elapsed = "_elapsed";

		public static readonly StringName _flashColor = "_flashColor";

		public static readonly StringName _texture = "_texture";

		public static readonly StringName _tex = "_tex";
	}

	public new class SignalName : CanvasLayer.SignalName
	{
	}

	private const string TexturePath = "res://Watcher/images/vfx/screenflash.png";

	private const float FadeInDuration = 0.12f;

	private const float FlashDuration = 0.7f;

	private float _elapsed;

	private Color _flashColor;

	private Texture2D _texture;

	private TextureRect _tex;

	public static void Play(Color color)
	{
		if (!NonInteractiveMode.IsActive)
		{
			ScreenFlashEffect screenFlashEffect = new ScreenFlashEffect();
			screenFlashEffect._flashColor = color;
			screenFlashEffect.Layer = 100;
			(Engine.GetMainLoop() as SceneTree)?.Root.AddChild(screenFlashEffect, forceReadableName: false, InternalMode.Disabled);
		}
	}

	public override void _Ready()
	{
		if (!StanceVfx.TryGetTexture("res://Watcher/images/vfx/screenflash.png", ref _texture))
		{
			QueueFree();
			return;
		}
		_tex = new TextureRect();
		_tex.MouseFilter = Control.MouseFilterEnum.Ignore;
		_tex.Texture = _texture;
		_tex.Material = new CanvasItemMaterial
		{
			BlendMode = CanvasItemMaterial.BlendModeEnum.Add
		};
		_tex.StretchMode = TextureRect.StretchModeEnum.Scale;
		_tex.AnchorRight = 1f;
		_tex.AnchorBottom = 1f;
		_tex.Modulate = _flashColor;
		AddChild(_tex, forceReadableName: false, InternalMode.Disabled);
	}

	public override void _Process(double delta)
	{
		if (!GodotObject.IsInstanceValid(_tex))
		{
			QueueFree();
			return;
		}
		_elapsed += (float)delta;
		if (_elapsed >= 0.7f)
		{
			QueueFree();
			return;
		}
		float a;
		if (_elapsed < 0.12f)
		{
			a = _elapsed / 0.12f;
		}
		else
		{
			float num = (_elapsed - 0.12f) / 0.58f;
			a = 1f - num * num;
		}
		_tex.Modulate = new Color(_flashColor.R, _flashColor.G, _flashColor.B, a);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(3)
		{
			new MethodInfo(MethodName.Play, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Color, "color", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName._Process, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "delta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.Play && args.Count == 1)
		{
			Play(VariantUtils.ConvertTo<Color>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName._Ready && args.Count == 0)
		{
			_Ready();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName._Process && args.Count == 1)
		{
			_Process(VariantUtils.ConvertTo<double>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.Play && args.Count == 1)
		{
			Play(VariantUtils.ConvertTo<Color>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName.Play)
		{
			return true;
		}
		if (method == MethodName._Ready)
		{
			return true;
		}
		if (method == MethodName._Process)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName._elapsed)
		{
			_elapsed = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._flashColor)
		{
			_flashColor = VariantUtils.ConvertTo<Color>(in value);
			return true;
		}
		if (name == PropertyName._texture)
		{
			_texture = VariantUtils.ConvertTo<Texture2D>(in value);
			return true;
		}
		if (name == PropertyName._tex)
		{
			_tex = VariantUtils.ConvertTo<TextureRect>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		if (name == PropertyName._elapsed)
		{
			value = VariantUtils.CreateFrom(in _elapsed);
			return true;
		}
		if (name == PropertyName._flashColor)
		{
			value = VariantUtils.CreateFrom(in _flashColor);
			return true;
		}
		if (name == PropertyName._texture)
		{
			value = VariantUtils.CreateFrom(in _texture);
			return true;
		}
		if (name == PropertyName._tex)
		{
			value = VariantUtils.CreateFrom(in _tex);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		return new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, PropertyName._elapsed, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Color, PropertyName._flashColor, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._texture, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._tex, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName._elapsed, Variant.From(in _elapsed));
		info.AddProperty(PropertyName._flashColor, Variant.From(in _flashColor));
		info.AddProperty(PropertyName._texture, Variant.From(in _texture));
		info.AddProperty(PropertyName._tex, Variant.From(in _tex));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName._elapsed, out var value))
		{
			_elapsed = value.As<float>();
		}
		if (info.TryGetProperty(PropertyName._flashColor, out var value2))
		{
			_flashColor = value2.As<Color>();
		}
		if (info.TryGetProperty(PropertyName._texture, out var value3))
		{
			_texture = value3.As<Texture2D>();
		}
		if (info.TryGetProperty(PropertyName._tex, out var value4))
		{
			_tex = value4.As<TextureRect>();
		}
	}
}
