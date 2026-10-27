using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace Watcher.Code.Stances.Vfx;

[GlobalClass]
[ScriptPath("res://WatcherCode/Nodes/Stances/DivinityEyeSpawner.cs")]
public class DivinityEyeSpawner : Node2D
{
	private struct EyeData
	{
		public Sprite2D Sprite;

		public float Age;

		public float Lifetime;

		public float Scale;

		public Color BaseColor;

		public float BobSpeed;

		public float StartY;
	}

	public new class MethodName : Node2D.MethodName
	{
		public static readonly StringName StopSpawning = "StopSpawning";

		public new static readonly StringName _Ready = "_Ready";

		public static readonly StringName EnsureFrames = "EnsureFrames";

		public new static readonly StringName _Process = "_Process";

		public static readonly StringName GetEyeFrame = "GetEyeFrame";

		public static readonly StringName GetVerticalBob = "GetVerticalBob";

		public static readonly StringName SpawnEye = "SpawnEye";
	}

	public new class PropertyName : Node2D.PropertyName
	{
		public static readonly StringName _frames = "_frames";

		public static readonly StringName _atlasSource = "_atlasSource";

		public static readonly StringName _mat = "_mat";

		public static readonly StringName _rng = "_rng";

		public static readonly StringName _s = "_s";

		public static readonly StringName _spawnTimer = "_spawnTimer";

		public static readonly StringName _stopping = "_stopping";
	}

	public new class SignalName : Node2D.SignalName
	{
	}

	private const string TexturePath = "res://Watcher/images/vfx/eye_anim.png";

	private const int FrameCount = 7;

	private const int FrameSize = 64;

	private const float SpawnInterval = 0.2f;

	private readonly List<EyeData> _eyes = new List<EyeData>();

	private AtlasTexture[] _frames;

	private Texture2D _atlasSource;

	private CanvasItemMaterial _mat;

	private RandomNumberGenerator _rng;

	private float _s;

	private float _spawnTimer;

	private bool _stopping;

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
		if (!EnsureFrames())
		{
			QueueFree();
			return;
		}
		for (int i = 0; i < 3; i++)
		{
			float initialAge = _rng.RandfRange(0f, 2f);
			SpawnEye(initialAge);
		}
	}

	private bool EnsureFrames()
	{
		if (_frames != null && GodotObject.IsInstanceValid(_atlasSource))
		{
			return true;
		}
		if (!StanceVfx.TryGetTexture("res://Watcher/images/vfx/eye_anim.png", ref _atlasSource))
		{
			return false;
		}
		_frames = new AtlasTexture[7];
		for (int i = 0; i < 7; i++)
		{
			_frames[i] = new AtlasTexture
			{
				Atlas = _atlasSource,
				Region = new Rect2(i * 64, 0f, 64f, 64f)
			};
		}
		return true;
	}

	public override void _Process(double delta)
	{
		float num = (float)delta;
		if (!_stopping)
		{
			_spawnTimer += num;
			while (_spawnTimer >= 0.2f)
			{
				_spawnTimer -= 0.2f;
				SpawnEye(0f);
			}
		}
		else if (_eyes.Count == 0)
		{
			QueueFree();
			return;
		}
		if (!EnsureFrames())
		{
			StopSpawning();
			return;
		}
		for (int num2 = _eyes.Count - 1; num2 >= 0; num2--)
		{
			EyeData value = _eyes[num2];
			value.Age += num;
			if (value.Age >= value.Lifetime)
			{
				if (GodotObject.IsInstanceValid(value.Sprite))
				{
					value.Sprite.QueueFree();
				}
				_eyes.RemoveAt(num2);
			}
			else
			{
				float num3 = value.Age / value.Lifetime;
				int eyeFrame = GetEyeFrame(num3);
				if (eyeFrame >= 0 && eyeFrame < _frames.Length)
				{
					value.Sprite.Texture = _frames[eyeFrame];
				}
				float verticalBob = GetVerticalBob(num3, value.Scale);
				value.Sprite.Position = new Vector2(value.Sprite.Position.X, value.StartY + verticalBob);
				float num4 = ((!(num3 < 0.5f)) ? ((1f - num3) * 2f) : (num3 * 2f));
				num4 = num4 * num4 * (3f - 2f * num4);
				value.Sprite.Modulate = new Color(value.BaseColor.R, value.BaseColor.G, value.BaseColor.B, num4);
				_eyes[num2] = value;
			}
		}
	}

	private static int GetEyeFrame(float progress)
	{
		if (!(progress < 0.15f))
		{
			if (!(progress < 0.2f))
			{
				if (!(progress < 0.25f))
				{
					if (!(progress < 0.3f))
					{
						if (!(progress < 0.35f))
						{
							if (!(progress < 0.4f))
							{
								if (!(progress < 0.55f))
								{
									if (!(progress < 0.62f))
									{
										if (!(progress < 0.7f))
										{
											if (!(progress < 0.75f))
											{
												if (!(progress < 0.8f))
												{
													if (progress < 0.85f)
													{
														return 1;
													}
													return 0;
												}
												return 2;
											}
											return 3;
										}
										return 4;
									}
									return 5;
								}
								return 6;
							}
							return 5;
						}
						return 4;
					}
					return 3;
				}
				return 2;
			}
			return 1;
		}
		return 0;
	}

	private float GetVerticalBob(float progress, float scale)
	{
		return GetEyeFrame(progress) switch
		{
			0 => 12f, 
			1 => 8f, 
			2 => 4f, 
			3 => 3f, 
			_ => 0f, 
		} * scale * _s;
	}

	private void SpawnEye(float initialAge)
	{
		if (!EnsureFrames())
		{
			StopSpawning();
			return;
		}
		Sprite2D sprite2D = new Sprite2D();
		sprite2D.Texture = _frames[0];
		sprite2D.Material = _mat;
		float num = _rng.RandfRange(1.25f, 1.88f) * _s;
		sprite2D.Scale = new Vector2(num, num);
		float num2 = _rng.RandfRange(-150f, 150f) * _s;
		float num3 = _rng.RandfRange(-175f, 75f) * _s;
		sprite2D.Position = new Vector2(num2, num3);
		float num4 = _rng.RandfRange(6f, 12f);
		if (num2 > 0f)
		{
			num4 = 0f - num4;
		}
		sprite2D.Rotation = Mathf.DegToRad(num4);
		float r = _rng.RandfRange(0.8f, 1f);
		float g = _rng.RandfRange(0.5f, 0.7f);
		float b = _rng.RandfRange(0.8f, 1f);
		Color modulate = new Color(r, g, b, 0f);
		sprite2D.Modulate = modulate;
		sprite2D.ZIndex = ((!(_rng.Randf() < 0.5f)) ? 1 : (-1));
		AddChild(sprite2D, forceReadableName: false, InternalMode.Disabled);
		float lifetime = num / _s + 0.8f;
		_eyes.Add(new EyeData
		{
			Sprite = sprite2D,
			Age = initialAge,
			Lifetime = lifetime,
			Scale = num,
			BaseColor = new Color(r, g, b),
			BobSpeed = 1f,
			StartY = num3
		});
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(7)
		{
			new MethodInfo(MethodName.StopSpawning, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.EnsureFrames, new PropertyInfo(Variant.Type.Bool, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName._Process, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "delta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.GetEyeFrame, new PropertyInfo(Variant.Type.Int, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "progress", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.GetVerticalBob, new PropertyInfo(Variant.Type.Float, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "progress", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "scale", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.SpawnEye, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
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
		if (method == MethodName.EnsureFrames && args.Count == 0)
		{
			ret = VariantUtils.CreateFrom<bool>(EnsureFrames());
			return true;
		}
		if (method == MethodName._Process && args.Count == 1)
		{
			_Process(VariantUtils.ConvertTo<double>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.GetEyeFrame && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<int>(GetEyeFrame(VariantUtils.ConvertTo<float>(in args[0])));
			return true;
		}
		if (method == MethodName.GetVerticalBob && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<float>(GetVerticalBob(VariantUtils.ConvertTo<float>(in args[0]), VariantUtils.ConvertTo<float>(in args[1])));
			return true;
		}
		if (method == MethodName.SpawnEye && args.Count == 1)
		{
			SpawnEye(VariantUtils.ConvertTo<float>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.GetEyeFrame && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<int>(GetEyeFrame(VariantUtils.ConvertTo<float>(in args[0])));
			return true;
		}
		ret = default(godot_variant);
		return false;
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
		if (method == MethodName.EnsureFrames)
		{
			return true;
		}
		if (method == MethodName._Process)
		{
			return true;
		}
		if (method == MethodName.GetEyeFrame)
		{
			return true;
		}
		if (method == MethodName.GetVerticalBob)
		{
			return true;
		}
		if (method == MethodName.SpawnEye)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName._frames)
		{
			_frames = VariantUtils.ConvertToSystemArrayOfGodotObject<AtlasTexture>(in value);
			return true;
		}
		if (name == PropertyName._atlasSource)
		{
			_atlasSource = VariantUtils.ConvertTo<Texture2D>(in value);
			return true;
		}
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
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		if (name == PropertyName._frames)
		{
			GodotObject[] frames = _frames;
			value = VariantUtils.CreateFromSystemArrayOfGodotObject(frames);
			return true;
		}
		if (name == PropertyName._atlasSource)
		{
			value = VariantUtils.CreateFrom(in _atlasSource);
			return true;
		}
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
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		return new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Array, PropertyName._frames, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._atlasSource, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._mat, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._rng, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._s, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._spawnTimer, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._stopping, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		StringName frames = PropertyName._frames;
		GodotObject[] frames2 = _frames;
		info.AddProperty(frames, Variant.CreateFrom(frames2));
		info.AddProperty(PropertyName._atlasSource, Variant.From(in _atlasSource));
		info.AddProperty(PropertyName._mat, Variant.From(in _mat));
		info.AddProperty(PropertyName._rng, Variant.From(in _rng));
		info.AddProperty(PropertyName._s, Variant.From(in _s));
		info.AddProperty(PropertyName._spawnTimer, Variant.From(in _spawnTimer));
		info.AddProperty(PropertyName._stopping, Variant.From(in _stopping));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName._frames, out var value))
		{
			_frames = value.AsGodotObjectArray<AtlasTexture>();
		}
		if (info.TryGetProperty(PropertyName._atlasSource, out var value2))
		{
			_atlasSource = value2.As<Texture2D>();
		}
		if (info.TryGetProperty(PropertyName._mat, out var value3))
		{
			_mat = value3.As<CanvasItemMaterial>();
		}
		if (info.TryGetProperty(PropertyName._rng, out var value4))
		{
			_rng = value4.As<RandomNumberGenerator>();
		}
		if (info.TryGetProperty(PropertyName._s, out var value5))
		{
			_s = value5.As<float>();
		}
		if (info.TryGetProperty(PropertyName._spawnTimer, out var value6))
		{
			_spawnTimer = value6.As<float>();
		}
		if (info.TryGetProperty(PropertyName._stopping, out var value7))
		{
			_stopping = value7.As<bool>();
		}
	}
}
