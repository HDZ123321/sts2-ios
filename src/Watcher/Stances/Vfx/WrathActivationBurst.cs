using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace Watcher.Code.Stances.Vfx;

[GlobalClass]
[ScriptPath("res://WatcherCode/Nodes/Stances/WrathActivationBurst.cs")]
public class WrathActivationBurst : Node2D
{
	private readonly struct BurstLine(Sprite2D sprite, float delay, float duration, float targetAlpha, float startScaleX, float endScaleY)
	{
		public readonly Sprite2D Sprite = sprite;

		public readonly float Delay = delay;

		public readonly float Duration = duration;

		public readonly float TargetAlpha = targetAlpha;

		public readonly float StartScaleX = startScaleX;

		public readonly float EndScaleY = endScaleY;
	}

	public new class MethodName : Node2D.MethodName
	{
		public new static readonly StringName _Ready = "_Ready";

		public new static readonly StringName _Process = "_Process";
	}

	public new class PropertyName : Node2D.PropertyName
	{
		public static readonly StringName _elapsed = "_elapsed";

		public static readonly StringName _texture = "_texture";
	}

	public new class SignalName : Node2D.SignalName
	{
	}

	private const string TexturePath = "res://Watcher/images/vfx/strike_line.png";

	private const int LineCount = 10;

	private const float TotalDuration = 1.1f;

	private const float CircleRadius = 44f;

	private float _elapsed;

	private BurstLine[] _lines;

	private Texture2D _texture;

	public override void _Ready()
	{
		float num = 0.9f;
		base.Position *= num;
		if (!StanceVfx.TryGetTexture("res://Watcher/images/vfx/strike_line.png", ref _texture))
		{
			QueueFree();
			return;
		}
		RandomNumberGenerator randomNumberGenerator = new RandomNumberGenerator();
		randomNumberGenerator.Randomize();
		_lines = new BurstLine[10];
		CanvasItemMaterial material = new CanvasItemMaterial
		{
			BlendMode = CanvasItemMaterial.BlendModeEnum.Add
		};
		for (int i = 0; i < 10; i++)
		{
			Sprite2D sprite2D = new Sprite2D();
			sprite2D.Texture = _texture;
			sprite2D.Material = material;
			float g = randomNumberGenerator.RandfRange(0.15f, 0.35f);
			sprite2D.Modulate = new Color(1f, g, 0.05f, 0f);
			float s = (float)i / 10f * ((float)Math.PI * 2f) + randomNumberGenerator.RandfRange(-0.2f, 0.2f);
			float num2 = (44f + randomNumberGenerator.RandfRange(-10f, 10f)) * num;
			sprite2D.Position = new Vector2(Mathf.Cos(s) * num2, Mathf.Sin(s) * num2 * 0.3f);
			sprite2D.Rotation = Mathf.DegToRad(90f);
			float num3 = randomNumberGenerator.RandfRange(0.8f, 1.2f) * num;
			float endScaleY = randomNumberGenerator.RandfRange(12.5f, 18.75f) * num;
			sprite2D.Scale = new Vector2(0.5f, num3);
			if (i == 0)
			{
				sprite2D.Position = new Vector2(0f, 0f);
			}
			AddChild(sprite2D, forceReadableName: false, InternalMode.Disabled);
			float delay = randomNumberGenerator.RandfRange(0f, 0.1f);
			_lines[i] = new BurstLine(sprite2D, delay, 0.9f, 0.4f, num3, endScaleY);
		}
	}

	public override void _Process(double delta)
	{
		_elapsed += (float)delta;
		if (_elapsed > 1.1f)
		{
			QueueFree();
			return;
		}
		for (int i = 0; i < _lines.Length; i++)
		{
			ref BurstLine reference = ref _lines[i];
			if (!GodotObject.IsInstanceValid(reference.Sprite))
			{
				continue;
			}
			float num = _elapsed - reference.Delay;
			if (num < 0f)
			{
				reference.Sprite.Modulate = new Color(reference.Sprite.Modulate, 0f);
				continue;
			}
			float num2 = num / reference.Duration;
			if (num2 > 1f)
			{
				reference.Sprite.Modulate = new Color(reference.Sprite.Modulate, 0f);
				continue;
			}
			float weight = Mathf.Min(num2 / 0.5f, 1f);
			float x = Mathf.Lerp(0.3f, reference.EndScaleY, weight);
			reference.Sprite.Scale = new Vector2(x, reference.StartScaleX);
			float a;
			if (num2 < 0.15f)
			{
				float num3 = num2 / 0.15f;
				a = num3 * num3 * num3 * reference.TargetAlpha;
			}
			else
			{
				float num4 = (num2 - 0.15f) / 0.85f;
				a = reference.TargetAlpha * (1f - num4);
			}
			reference.Sprite.Modulate = new Color(reference.Sprite.Modulate, a);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(2)
		{
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
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
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
		if (name == PropertyName._texture)
		{
			_texture = VariantUtils.ConvertTo<Texture2D>(in value);
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
		if (name == PropertyName._texture)
		{
			value = VariantUtils.CreateFrom(in _texture);
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
			new PropertyInfo(Variant.Type.Object, PropertyName._texture, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName._elapsed, Variant.From(in _elapsed));
		info.AddProperty(PropertyName._texture, Variant.From(in _texture));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName._elapsed, out var value))
		{
			_elapsed = value.As<float>();
		}
		if (info.TryGetProperty(PropertyName._texture, out var value2))
		{
			_texture = value2.As<Texture2D>();
		}
	}
}
