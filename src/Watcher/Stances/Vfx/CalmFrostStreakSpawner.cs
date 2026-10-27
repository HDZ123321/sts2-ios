using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace Watcher.Code.Stances.Vfx;

[GlobalClass]
[ScriptPath("res://WatcherCode/Nodes/Stances/CalmFrostStreakSpawner.cs")]
public class CalmFrostStreakSpawner : Node2D
{
	private struct StreakData
	{
		public Sprite2D Sprite;

		public float Age;

		public float Lifetime;

		public float VelocityX;

		public float VelocityY;

		public float AccelX;

		public float AccelY;

		public Color BaseColor;

		public float BaseScale;

		public bool RenderBehind;
	}

	public new class MethodName : Node2D.MethodName
	{
		public new static readonly StringName _Ready = "_Ready";

		public static readonly StringName StopSpawning = "StopSpawning";

		public new static readonly StringName _Process = "_Process";

		public static readonly StringName SpawnStreak = "SpawnStreak";
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

	private const string TexturePath = "res://Watcher/images/vfx/frost_streak.png";

	private const float SpawnInterval = 0.04f;

	private const float MinLifetime = 1.1f;

	private const float MaxLifetime = 1.7f;

	private readonly List<StreakData> _streaks = new List<StreakData>();

	private CanvasItemMaterial _mat;

	private RandomNumberGenerator _rng;

	private float _s;

	private float _spawnTimer;

	private bool _stopping;

	private Texture2D _texture;

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
		StanceVfx.TryGetTexture("res://Watcher/images/vfx/frost_streak.png", ref _texture);
		for (int i = 0; i < 15; i++)
		{
			float initialAge = _rng.RandfRange(0f, 1.7f);
			SpawnStreak(initialAge);
		}
	}

	public void StopSpawning()
	{
		_stopping = true;
	}

	public override void _Process(double delta)
	{
		float num = (float)delta;
		if (!_stopping)
		{
			_spawnTimer += num;
			while (_spawnTimer >= 0.04f)
			{
				_spawnTimer -= 0.04f;
				SpawnStreak(0f);
			}
		}
		else if (_streaks.Count == 0)
		{
			QueueFree();
			return;
		}
		for (int num2 = _streaks.Count - 1; num2 >= 0; num2--)
		{
			StreakData value = _streaks[num2];
			value.Age += num;
			if (value.Age >= value.Lifetime)
			{
				if (GodotObject.IsInstanceValid(value.Sprite))
				{
					value.Sprite.QueueFree();
				}
				_streaks.RemoveAt(num2);
			}
			else
			{
				value.VelocityX += value.AccelX * num;
				value.VelocityY += value.AccelY * num;
				value.VelocityX = Mathf.Min(value.VelocityX, -20f * _s);
				value.VelocityY = Mathf.Max(value.VelocityY, 0f);
				value.Sprite.Position += new Vector2(value.VelocityX * num, value.VelocityY * num);
				value.Sprite.Rotation = Mathf.Atan2(value.VelocityY, value.VelocityX) + (float)Math.PI / 2f;
				float num3 = value.Age / value.Lifetime;
				float num4 = ((num3 < 0.4f) ? (0.5f + num3 * 2.5f) : ((!(num3 < 0.7f)) ? (1.5f - (num3 - 0.7f) * 3.3f) : 1.5f));
				float a = num4;
				a = Mathf.Max(a, 0.2f);
				value.Sprite.Scale = new Vector2(value.BaseScale * 0.375f, value.BaseScale * 1.76f * a);
				num4 = ((num3 < 0.3f) ? (num3 / 0.3f) : ((!(num3 < 0.8f)) ? ((1f - num3) / 0.2f) : 1f));
				float num5 = num4;
				num5 = num5 * num5 * (3f - 2f * num5) * 0.75f;
				value.Sprite.Modulate = new Color(value.BaseColor.R, value.BaseColor.G, value.BaseColor.B, num5);
				_streaks[num2] = value;
			}
		}
	}

	private void SpawnStreak(float initialAge)
	{
		if (!StanceVfx.TryGetTexture("res://Watcher/images/vfx/frost_streak.png", ref _texture))
		{
			StopSpawning();
			return;
		}
		Sprite2D sprite2D = new Sprite2D();
		sprite2D.Texture = _texture;
		sprite2D.Material = _mat;
		float num = _rng.RandfRange(0.5f, 1f) * _s;
		sprite2D.Position = new Vector2(_rng.RandfRange(250f, 438f) * _s * (num / _s), _rng.RandfRange(-325f, -125f) * _s);
		float num2 = _rng.RandfRange(-413f, -288f) * _s;
		float num3 = _rng.RandfRange(225f, 275f) * _s;
		float accelX = 75f * (num / _s) * _s;
		float accelY = -106f * _s;
		float r = _rng.RandfRange(0.2f, 0.3f);
		float g = _rng.RandfRange(0.65f, 0.8f);
		Color modulate = new Color(r, g, 1f, 0f);
		sprite2D.Modulate = modulate;
		sprite2D.Scale = new Vector2(num * 0.375f, num * 1.76f);
		sprite2D.Rotation = Mathf.Atan2(num3, num2) + (float)Math.PI / 2f;
		bool flag = _rng.Randf() < 0.2f + (num / _s - 0.5f);
		sprite2D.ZIndex = ((!flag) ? 1 : (-1));
		AddChild(sprite2D, forceReadableName: false, InternalMode.Disabled);
		_streaks.Add(new StreakData
		{
			Sprite = sprite2D,
			Age = initialAge,
			Lifetime = _rng.RandfRange(1.1f, 1.7f),
			VelocityX = num2,
			VelocityY = num3,
			AccelX = accelX,
			AccelY = accelY,
			BaseColor = new Color(r, g, 1f),
			BaseScale = num,
			RenderBehind = flag
		});
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(4)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.StopSpawning, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName._Process, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "delta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.SpawnStreak, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "initialAge", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
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
		if (method == MethodName.StopSpawning && args.Count == 0)
		{
			StopSpawning();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName._Process && args.Count == 1)
		{
			_Process(VariantUtils.ConvertTo<double>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.SpawnStreak && args.Count == 1)
		{
			SpawnStreak(VariantUtils.ConvertTo<float>(in args[0]));
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
		if (method == MethodName.StopSpawning)
		{
			return true;
		}
		if (method == MethodName._Process)
		{
			return true;
		}
		if (method == MethodName.SpawnStreak)
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
