using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Character;
using Watcher.Code.Extensions;
using Watcher.Code.Vfx;

namespace Watcher.Code.Stances;

public abstract class WatcherStanceModel : AbstractModel
{
	private Player? _player;

	private StanceVfxController? _vfx;

	public Player Owner => _player ?? throw new InvalidOperationException("Not a mutable instance");

	private LocString Title => new LocString("powers", base.Id.Entry + ".title");

	private LocString Description => new LocString("powers", base.Id.Entry + ".description");

	private string PackedIconPath => (Watcher.Code.Extensions.StringExtensions.RemovePrefix(base.Id.Entry).ToLowerInvariant() + ".png").PowerImagePath();

	private Texture2D Icon => ResourceLoader.Load<Texture2D>(PackedIconPath, null, ResourceLoader.CacheMode.Reuse);

	public HoverTip DumbHoverTip
	{
		get
		{
			LocString description = Description;
			AddDumbVariablesToDescription(description);
			return new HoverTip(Title, description.GetFormattedText(), Icon);
		}
	}

	protected abstract StanceVfxConfig VfxConfig { get; }

	public IEnumerable<string> AssetPaths => VfxConfig.AssetPaths;

	public WatcherStanceModel ToMutable(Player player)
	{
		WatcherStanceModel obj = (WatcherStanceModel)MutableClone();
		obj._player = player;
		return obj;
	}

	private void AddDumbVariablesToDescription(LocString description)
	{
		description.Add("singleStarIcon", "[img]res://images/packed/sprite_fonts/star_icon.png[/img]");
		CardPoolModel model = (base.IsMutable ? Owner.Character.CardPool : ((CardPoolModel)(object)ModelDb.CardPool<WatcherCardPool>()));
		description.Add("energyPrefix", EnergyIconHelper.GetPrefix(model));
	}

	public virtual async Task OnEnterStance(PlayerChoiceContext ctx, Player owner, CardModel? source)
	{
		_vfx = new StanceVfxController(VfxConfig);
		await _vfx.OnEnter(owner.Creature);
	}

	public virtual async Task OnExitStance(PlayerChoiceContext ctx, Player owner, CardModel? source)
	{
		if (_vfx != null)
		{
			await _vfx.OnExit(owner.Creature);
		}
		_vfx = null;
	}
}
