using BaseLib.Abstracts;
using Godot;

namespace Watcher.Code.Character;

public class WatcherRelicPool : CustomRelicPoolModel
{
	public override string EnergyColorName => "Watcher";

	public override Color LabOutlineColor => Watcher.Color;
}
