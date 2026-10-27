using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Assets;

namespace Watcher.Code.Stances.Vfx;

[GlobalClass]
[ScriptPath("res://WatcherCode/Nodes/Stances/WrathGlowSparkSpawner.cs")]
public class WrathGlowSparkSpawner : Node2D
{
	private struct SparkData
	{
		public Sprite2D Sprite;

		public float Age;

		public float Lifetime;

		public float FinalScaleY;

		public float BaseScaleX;

		public Color BaseColor;
	}

	public new class MethodName : Node2D.MethodName
	{
		public static readonly StringName StopSpawning = "StopSpawning";

		public new static readonly StringName _Ready = "_Ready";

		public new static readonly StringName _Process = "_Process";

		public static readonly StringName ResolveTexture = "ResolveTexture";

		public static readonly StringName SpawnSpark = "SpawnSpark";
	}

	public new class PropertyName : Node2D.PropertyName
	{
		public static readonly StringName _mat = "_mat";

		public static readonly StringName _rng = "_rng";

		public static readonly StringName _s = "_s";

		public static readonly StringName _spawnTimer = "_spawnTimer";

		public static readonly StringName _stopping = "_stopping";

		public static readonly StringName _texture = "_texture";
	}

	public new class SignalName : Node2D.SignalName
	{
	}

	private const string TexturePath = "res://Watcher/images/vfx/glow_spark.png";

	private const float SpawnInterval = 0.05f;

	private const float SparkLifetime = 1.4f;

	private const float SpawnRadius = 205f;

	private const float StartScaleX = 0.62f;

	private const float StartScaleY = 1.02f;

	private const float EndScaleY = 4.1f;

	private readonly List<SparkData> _sparks = new List<SparkData>();

	private CanvasItemMaterial _mat;

	private RandomNumberGenerator _rng;

	private float _s;

	private float _spawnTimer;

	private bool _stopping;

	private Texture2D _texture;

	public void StopSpawning()
	{
		_stopping = true;
	}

	public override void _Ready()
	{
		_s = 0.9f;
		base.Position *= _s;
		_rng = new RandomNumberGenerator();
		_rng.Randomize();
		_mat = new CanvasItemMaterial
		{
			BlendMode = CanvasItemMaterial.BlendModeEnum.Add
		};
		_texture = ResolveTexture();
		for (int i = 0; i < 20; i++)
		{
			float initialAge = _rng.RandfRange(0f, 1.4f);
			SpawnSpark(initialAge);
		}
	}

	public override void _Process(double delta)
	{
		float num = (float)delta;
		if (!_stopping)
		{
			_spawnTimer += num;
			while (_spawnTimer >= 0.05f)
			{
				_spawnTimer -= 0.05f;
				SpawnSpark(0f);
			}
		}
		else if (_sparks.Count == 0)
		{
			QueueFree();
			return;
		}
		for (int num2 = _sparks.Count - 1; num2 >= 0; num2--)
		{
			SparkData value = _sparks[num2];
			value.Age += num;
			if (value.Age >= value.Lifetime)
			{
				if (GodotObject.IsInstanceValid(value.Sprite))
				{
					value.Sprite.QueueFree();
				}
				_sparks.RemoveAt(num2);
			}
			else
			{
				float num3 = value.Age / value.Lifetime;
				float y = Mathf.Lerp(1.02f * _s, value.FinalScaleY, num3);
				value.Sprite.Scale = new Vector2(value.BaseScaleX, y);
				float a = ((num3 < 0.25f) ? (num3 / 0.25f) : ((!(num3 < 0.6f)) ? (1f - (num3 - 0.6f) / 0.4f) : 1f));
				value.Sprite.Modulate = new Color(value.BaseColor.R, value.BaseColor.G, value.BaseColor.B, a);
				_sparks[num2] = value;
			}
		}
	}

	private Texture2D ResolveTexture()
	{
		return PreloadManager.Cache.GetAsset<Texture2D>("res://Watcher/images/vfx/glow_spark.png");
	}

	private void SpawnSpark(float initialAge)
	{
		if (!GodotObject.IsInstanceValid(_texture))
		{
			_texture = ResolveTexture();
		}
		if (!GodotObject.IsInstanceValid(_texture))
		{
			StopSpawning();
			return;
		}
		Sprite2D sprite2D = new Sprite2D();
		sprite2D.Texture = _texture;
		sprite2D.Material = _mat;
		sprite2D.Offset = new Vector2(0f, -31f * _s);
		float s = _rng.RandfRange(0f, (float)Math.PI * 2f);
		float num = _rng.RandfRange(0f, 205f * _s);
		sprite2D.Position = new Vector2(Mathf.Cos(s) * num, Mathf.Sin(s) * num);
		sprite2D.Rotation = Mathf.DegToRad(_rng.RandfRange(-8f, 8f));
		float r = _rng.RandfRange(0.25f, 0.35f);
		float g = _rng.RandfRange(0f, 0.03f);
		Color modulate = new Color(r, g, 0.02f, 0f);
		sprite2D.Modulate = modulate;
		float num2 = _rng.RandfRange(0.52f, 0.72f) * _s;
		float finalScaleY = _rng.RandfRange(3.28f, 4.92f) * _s;
		sprite2D.Scale = new Vector2(num2, 1.02f * _s);
		if (_rng.Randf() < 0.5f)
		{
			sprite2D.ZAsRelative = false;
			sprite2D.ZIndex = -1;
		}
		AddChild(sprite2D, forceReadableName: false, InternalMode.Disabled);
		_sparks.Add(new SparkData
		{
			Sprite = sprite2D,
			Age = initialAge,
			Lifetime = _rng.RandfRange(1.12f, 1.6800001f),
			FinalScaleY = finalScaleY,
			BaseScaleX = num2,
			BaseColor = new Color(r, g, 0.02f)
		});
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(5)
		{
			new MethodInfo(MethodName.StopSpawning, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName._Process, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "delta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.ResolveTexture, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Texture2D"), exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.SpawnSpark, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "initialAge", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.StopSpawning && args.Count == 0)
		{
			StopSpawning();
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
		if (method == MethodName.ResolveTexture && args.Count == 0)
		{
			ret = VariantUtils.CreateFrom<Texture2D>(ResolveTexture());
			return true;
		}
		if (method == MethodName.SpawnSpark && args.Count == 1)
		{
			SpawnSpark(VariantUtils.ConvertTo<float>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName.StopSpawning)
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
		if (method == MethodName.ResolveTexture)
		{
			return true;
		}
		if (method == MethodName.SpawnSpark)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName._mat)
		{
			_mat = VariantUtils.ConvertTo<CanvasItemMaterial>(in value);
			return true;
		}
		if (name == PropertyName._rng)
		{
			_rng = VariantUtils.ConvertTo<RandomNumberGenerator>(in value);
			return true;
		}
		if (name == PropertyName._s)
		{
			_s = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._spawnTimer)
		{
			_spawnTimer = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._stopping)
		{
			_stopping = VariantUtils.ConvertTo<bool>(in value);
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
		if (name == PropertyName._mat)
		{
			value = VariantUtils.CreateFrom(in _mat);
			return true;
		}
		if (name == PropertyName._rng)
		{
			value = VariantUtils.CreateFrom(in _rng);
			return true;
		}
		if (name == PropertyName._s)
		{
			value = VariantUtils.CreateFrom(in _s);
			return true;
		}
		if (name == PropertyName._spawnTimer)
		{
			value = VariantUtils.CreateFrom(in _spawnTimer);
			return true;
		}
		if (name == PropertyName._stopping)
		{
			value = VariantUtils.CreateFrom(in _stopping);
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
			new PropertyInfo(Variant.Type.Object, PropertyName._mat, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._rng, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._s, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._spawnTimer, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._stopping, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._texture, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName._mat, Variant.From(in _mat));
		info.AddProperty(PropertyName._rng, Variant.From(in _rng));
		info.AddProperty(PropertyName._s, Variant.From(in _s));
		info.AddProperty(PropertyName._spawnTimer, Variant.From(in _spawnTimer));
		info.AddProperty(PropertyName._stopping, Variant.From(in _stopping));
		info.AddProperty(PropertyName._texture, Variant.From(in _texture));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName._mat, out var value))
		{
			_mat = value.As<CanvasItemMaterial>();
		}
		if (info.TryGetProperty(PropertyName._rng, out var value2))
		{
			_rng = value2.As<RandomNumberGenerator>();
		}
		if (info.TryGetProperty(PropertyName._s, out var value3))
		{
			_s = value3.As<float>();
		}
		if (info.TryGetProperty(PropertyName._spawnTimer, out var value4))
		{
			_spawnTimer = value4.As<float>();
		}
		if (info.TryGetProperty(PropertyName._stopping, out var value5))
		{
			_stopping = value5.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._texture, out var value6))
		{
			_texture = value6.As<Texture2D>();
		}
	}
}
