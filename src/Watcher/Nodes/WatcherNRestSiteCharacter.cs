using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Nodes.RestSite;

namespace Watcher.Code.Nodes;

[GlobalClass]
[ScriptPath("res://WatcherCode/Nodes/WatcherNRestSiteCharacter.cs")]
public class WatcherNRestSiteCharacter : NRestSiteCharacter
{
	public new class MethodName : NRestSiteCharacter.MethodName
	{
		public new static readonly StringName _Ready = "_Ready";
	}

	public new class PropertyName : NRestSiteCharacter.PropertyName
	{
	}

	public new class SignalName : NRestSiteCharacter.SignalName
	{
	}

	public override void _Ready()
	{
		base._Ready();
		bool num = base._characterIndex % 2 == 1;
		TextureRect node = GetNode<TextureRect>("SpineSprite");
		if (num)
		{
			node.FlipH = true;
		}
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
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
	}
}
