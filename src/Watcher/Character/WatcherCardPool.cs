using BaseLib.Abstracts;
using Godot;

namespace Watcher.Code.Character;

public sealed class WatcherCardPool : CustomCardPoolModel
{
	public override string Title => "Watcher";

	public override float H => 0.8f;

	public override float S => 0.5f;

	public override float V => 1f;

	public override Color DeckEntryCardColor => Watcher.Color;

	public override bool IsColorless => false;

	public override string? BigEnergyIconPath => "res://Watcher/images/ui/combat/watcher_energy_icon.png";

	public override string? TextEnergyIconPath => "res://Watcher/images/ui/combat/text_watcher_energy_icon.png";
}
