using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace Watcher.Code.Stances.Vfx;

[GlobalClass]
[ScriptPath("res://WatcherCode/Nodes/Stances/DivinityActivationBurst.cs")]
public class DivinityActivationBurst : Node2D
{
	private struct SparkInfo
	{
		public Sprite2D Sprite;

		public float Delay;

		public float Duration;

		public float StartX;

		public float StartY;

		public float ScaleStart;

		public Color BaseColor;
	}

	public new class MethodName : Node2D.MethodName
	{
		public new static readonly StringName _Ready = "_Ready";

		public new static readonly StringName _Process = "_Process";
	}

	public new class PropertyName : Node2D.PropertyName
	{
		public static readonly StringName _elapsed = "_elapsed";
	}

	public new class SignalName : Node2D.SignalName
	{
	}

	private const string TexturePath = "res://Watcher/images/vfx/glow_spark.png";

	private const int SparkCount = 20;

	private const float TotalDuration = 1.2f;

	private float _elapsed;

	private SparkInfo[] _sparks;

	public override void _Ready()
	{
		base.Position *= 0.9f;
		Texture2D texture2D = GD.Load<Texture2D>("res://Watcher/images/vfx/glow_spark.png");
		if (!GodotObject.IsInstanceValid(texture2D))
		{
			QueueFree();
			return;
		}
		RandomNumberGenerator randomNumberGenerator = new RandomNumberGenerator();
		randomNumberGenerator.Randomize();
		_sparks = new SparkInfo[20];
		CanvasItemMaterial material = new CanvasItemMaterial
		{
			BlendMode = CanvasItemMaterial.BlendModeEnum.Add
		};
		for (int i = 0; i < 20; i++)
		{
			Sprite2D sprite2D = new Sprite2D();
			sprite2D.Texture = texture2D;
			sprite2D.Material = material;
			float r = randomNumberGenerator.RandfRange(0.8f, 1f);
			float g = randomNumberGenerator.RandfRange(0.4f, 0.7f);
			float b = randomNumberGenerator.RandfRange(0.7f, 1f);
			sprite2D.Modulate = new Color(r, g, b, 0f);
			float s = randomNumberGenerator.RandfRange(0f, (float)Math.PI * 2f);
			float num = randomNumberGenerator.RandfRange(625f, 1000f) * 0.9f;
			float num2 = Mathf.Cos(s) * num;
			float num3 = Mathf.Sin(s) * num * 0.5f;
			sprite2D.Position = new Vector2(num2, num3);
			sprite2D.Rotation = Mathf.Atan2(0f - num3, 0f - num2) + (float)Math.PI / 2f;
			float num4 = randomNumberGenerator.RandfRange(1.88f, 3.75f) * 0.9f;
			sprite2D.Scale = new Vector2(num4 * 0.5f, num4 * 1.5f);
			AddChild(sprite2D, forceReadableName: false, InternalMode.Disabled);
			_sparks[i] = new SparkInfo
			{
				Sprite = sprite2D,
				Delay = randomNumberGenerator.RandfRange(0f, 0.25f),
				Duration = randomNumberGenerator.RandfRange(0.35f, 0.55f),
				StartX = num2,
				StartY = num3,
				ScaleStart = num4,
				BaseColor = new Color(r, g, b)
			};
		}
	}

	public override void _Process(double delta)
	{
		_elapsed += (float)delta;
		if (_elapsed > 1.2f)
		{
			QueueFree();
			return;
		}
		SparkInfo[] sparks = _sparks;
		for (int i = 0; i < sparks.Length; i++)
		{
			SparkInfo sparkInfo = sparks[i];
			if (GodotObject.IsInstanceValid(sparkInfo.Sprite))
			{
				float num = _elapsed - sparkInfo.Delay;
				if (num < 0f)
				{
					sparkInfo.Sprite.Modulate = new Color(sparkInfo.BaseColor, 0f);
					continue;
				}
				float num2 = Mathf.Clamp(num / sparkInfo.Duration, 0f, 1f);
				float weight = num2 * num2;
				float x = Mathf.Lerp(sparkInfo.StartX, 0f, weight);
				float y = Mathf.Lerp(sparkInfo.StartY, 0f, weight);
				sparkInfo.Sprite.Position = new Vector2(x, y);
				float num3 = sparkInfo.ScaleStart * (1f - num2 * 0.7f);
				sparkInfo.Sprite.Scale = new Vector2(num3 * 0.5f, num3 * 1.5f);
				float num4 = ((num2 < 0.15f) ? (num2 / 0.15f) : ((!(num2 < 0.5f)) ? ((1f - num2) / 0.5f) : 1f));
				float num5 = num4;
				num5 = Mathf.Clamp(num5 * 0.65f, 0f, 1f);
				sparkInfo.Sprite.Modulate = new Color(sparkInfo.BaseColor, num5);
			}
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
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		return new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, PropertyName._elapsed, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName._elapsed, Variant.From(in _elapsed));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName._elapsed, out var value))
		{
			_elapsed = value.As<float>();
		}
	}
}
