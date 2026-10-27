using System.ComponentModel;
using Godot;
using Godot.Bridge;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace Watcher.Code.Nodes;

[GlobalClass]
[ScriptPath("res://WatcherCode/Nodes/WatcherNMerchantCharacter.cs")]
public class WatcherNMerchantCharacter : NMerchantCharacter
{
	public new class MethodName : NMerchantCharacter.MethodName
	{
	}

	public new class PropertyName : NMerchantCharacter.PropertyName
	{
	}

	public new class SignalName : NMerchantCharacter.SignalName
	{
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
