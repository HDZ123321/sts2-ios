using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace Watcher.Code.Stances.Vfx;

[GlobalClass]
[ScriptPath("res://WatcherCode/Nodes/Stances/AuraBlobEmitter.cs")]
public class AuraBlobEmitter : Node2D
{
	public new class MethodName : Node2D.MethodName
	{
		public new static readonly StringName _Ready = "_Ready";
	}

	public new class PropertyName : Node2D.PropertyName
	{
		public static readonly StringName BlobColor = "BlobColor";

		public static readonly StringName _texture = "_texture";
	}

	public new class SignalName : Node2D.SignalName
	{
	}

	private const string TexturePath = "res://Watcher/images/vfx/big_blur.png";

	private Texture2D _texture;

	[Export(PropertyHint.None, "")]
	public Color BlobColor { get; set; } = new Color(0.15f, 0f, 0.03f);

	public override void _Ready()
	{
		base.Position *= 0.9f;
		if (!StanceVfx.TryGetTexture("res://Watcher/images/vfx/big_blur.png", ref _texture))
		{
			QueueFree();
			return;
		}
		Gradient gradient = new Gradient();
		gradient.Offsets = new float[5] { 0f, 0.3f, 0.5f, 0.7f, 1f };
		gradient.Colors = new Color[5]
		{
			new Color(1f, 1f, 1f, 0f),
			new Color(1f, 1f, 1f, 0.6f),
			new Color(1f, 1f, 1f, 0.6f),
			new Color(1f, 1f, 1f, 0.3f),
			new Color(1f, 1f, 1f, 0f)
		};
		CpuParticles2D cpuParticles2D = new CpuParticles2D();
		cpuParticles2D.Texture = _texture;
		cpuParticles2D.Material = new CanvasItemMaterial
		{
			BlendMode = CanvasItemMaterial.BlendModeEnum.Add
		};
		cpuParticles2D.Amount = 6;
		cpuParticles2D.Lifetime = 2.0;
		cpuParticles2D.Preprocess = 2.0;
		cpuParticles2D.Direction = new Vector2(0f, -1f);
		cpuParticles2D.Spread = 180f;
		cpuParticles2D.Gravity = Vector2.Zero;
		cpuParticles2D.InitialVelocityMin = 0f;
		cpuParticles2D.InitialVelocityMax = 9f;
		cpuParticles2D.ScaleAmountMin = 3.096f;
		cpuParticles2D.ScaleAmountMax = 4.0499997f;
		cpuParticles2D.AngleMin = 0f;
		cpuParticles2D.AngleMax = 360f;
		cpuParticles2D.AngularVelocityMin = -20f;
		cpuParticles2D.AngularVelocityMax = 20f;
		cpuParticles2D.Color = BlobColor;
		cpuParticles2D.ColorRamp = gradient;
		cpuParticles2D.EmissionShape = CpuParticles2D.EmissionShapeEnum.Rectangle;
		cpuParticles2D.EmissionRectExtents = new Vector2(34.2f, 56.699997f);
		cpuParticles2D.Emitting = true;
		AddChild(cpuParticles2D, forceReadableName: false, InternalMode.Disabled);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(1)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null)
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
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName._Ready)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName.BlobColor)
		{
			BlobColor = VariantUtils.ConvertTo<Color>(in value);
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
		if (name == PropertyName.BlobColor)
		{
			value = VariantUtils.CreateFrom<Color>(BlobColor);
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
			new PropertyInfo(Variant.Type.Color, PropertyName.BlobColor, PropertyHint.None, "", PropertyUsageFlags.Default | PropertyUsageFlags.ScriptVariable, exported: true),
			new PropertyInfo(Variant.Type.Object, PropertyName._texture, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName.BlobColor, Variant.From<Color>(BlobColor));
		info.AddProperty(PropertyName._texture, Variant.From(in _texture));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName.BlobColor, out var value))
		{
			BlobColor = value.As<Color>();
		}
		if (info.TryGetProperty(PropertyName._texture, out var value2))
		{
			_texture = value2.As<Texture2D>();
		}
	}
}
