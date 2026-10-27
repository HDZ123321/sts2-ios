using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BaseLib.Abstracts;
using BaseLib.Patches.UI;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using Watcher.Code.Cards.Basic;
using Watcher.Code.Core;
using Watcher.Code.Extensions;
using Watcher.Code.Relics;
using Watcher.Code.Stances;

namespace Watcher.Code.Character;

public class Watcher : CustomCharacterModel
{
	public const string CharacterId = "Watcher";

	public static readonly Color Color = new Color(0.5f, 0f, 0.5f);

	public override Color MapDrawingColor => Color;

	public override string CustomIconTexturePath => "res://Watcher/images/watcher/character_icon_watcher.png";

	public override string CustomCharacterSelectIconPath => "res://Watcher/images/watcher/char_select_watcher.png";

	public override string CustomEnergyCounterPath => "res://Watcher/scenes/watcher/watcher_energy_counter.tscn";

	public override Color EnergyLabelOutlineColor => new Color(1428316927u);

	public override string CustomCharacterSelectLockedIconPath => "res://Watcher/images/watcher/char_select_watcher_locked.png";

	public override string CustomVisualPath => "res://Watcher/scenes/watcher/watcher.tscn";

	public override string CustomTrailPath => "res://Watcher/scenes/watcher/card_trail_watcher.tscn";

	public override string CustomIconPath => "res://Watcher/scenes/watcher/watcher_icon.tscn";

	public override string CustomIconOutlineTexturePath => "res://Watcher/images/watcher/character_icon_watcher_outline.png";

	public override string CustomRestSiteAnimPath => "res://Watcher/scenes/watcher/watcher_rest_site.tscn";

	public override string CustomMerchantAnimPath => "res://Watcher/scenes/watcher/watcher_merchant.tscn";

	public override string CustomArmPointingTexturePath => "res://Watcher/images/watcher/hands/multiplayer_hand_watcher_point.png";

	public override string CustomArmRockTexturePath => "res://Watcher/images/watcher/hands/multiplayer_hand_watcher_rock.png";

	public override string CustomArmPaperTexturePath => "res://Watcher/images/watcher/hands/multiplayer_hand_watcher_paper.png";

	public override string CustomArmScissorsTexturePath => "res://Watcher/images/watcher/hands/multiplayer_hand_watcher_scissors.png";

	public override string CustomCharacterSelectBg => "res://Watcher/scenes/watcher/char_select_bg_watcher2.tscn";

	public override string CustomCharacterSelectTransitionPath => "res://Watcher/images/watcher/transitions/watcher_transition_mat.tres";

	public override string CustomMapMarkerPath => "res://Watcher/images/watcher/map_marker_watcher.png";

	public override string CustomAttackSfx => "event:/sfx/characters/ironclad/ironclad_attack";

	public override string CharacterSelectSfx => "res://Watcher/audio/watcher_select.ogg";

	public override RelicIconData CustomYummyCookie => new RelicIconData("watcher_cookie.png".BigRelicImagePath(), "watcher_cookie.tres".TresRelicImagePath(), "watcher_cookie_outline.tres".TresRelicImagePath());

	public override Color NameColor => Color;

	public override CharacterGender Gender => CharacterGender.Feminine;

	protected override CharacterModel? UnlocksAfterRunAs => null;

	public override int StartingHp => 72;

	public override int StartingGold => 99;

	public override IEnumerable<CardModel> StartingDeck => new global::_003C_003Ez__ReadOnlyArray<CardModel>(new CardModel[10]
	{
		(CardModel)(object)ModelDb.Card<StrikeWatcher>(),
		(CardModel)(object)ModelDb.Card<StrikeWatcher>(),
		(CardModel)(object)ModelDb.Card<StrikeWatcher>(),
		(CardModel)(object)ModelDb.Card<StrikeWatcher>(),
		(CardModel)(object)ModelDb.Card<DefendWatcher>(),
		(CardModel)(object)ModelDb.Card<DefendWatcher>(),
		(CardModel)(object)ModelDb.Card<DefendWatcher>(),
		(CardModel)(object)ModelDb.Card<DefendWatcher>(),
		(CardModel)(object)ModelDb.Card<Vigilance>(),
		(CardModel)(object)ModelDb.Card<Eruption>()
	});

	protected override IEnumerable<string> ExtraAssetPaths
	{
		get
		{
			List<string> list = new List<string>();
			list.Add("res://Watcher/scenes/watcher_mod/vfx/calm_aura.tscn");
			list.Add("res://Watcher/scenes/watcher_mod/vfx/wrath_aura.tscn");
			list.Add("res://Watcher/scenes/watcher_mod/vfx/divinity_aura.tscn");
			list.AddRange(WatcherModelDb.WatcherStance<CalmStance>().AssetPaths);
			list.AddRange(WatcherModelDb.WatcherStance<WrathStance>().AssetPaths);
			list.AddRange(WatcherModelDb.WatcherStance<DivinityStance>().AssetPaths);
			list.Add("res://Watcher/images/vfx/screenflash.png");
			list.Add("res://Watcher/images/vfx/eye_anim.png");
			list.Add("res://Watcher/images/vfx/frost_streak.png");
			list.Add("res://Watcher/images/vfx/big_blur.png");
			list.Add("res://Watcher/images/vfx/strike_line.png");
			list.Add("res://Watcher/images/vfx/glow_spark.png");
			return new global::_003C_003Ez__ReadOnlyList<string>(list);
		}
	}

	public override IReadOnlyList<RelicModel> StartingRelics => new global::_003C_003Ez__ReadOnlySingleElementList<RelicModel>((RelicModel)(object)ModelDb.Relic<PureWater>());

	public override float AttackAnimDelay => 0.15f;

	public override float CastAnimDelay => 0.25f;

	public override CardPoolModel CardPool => (CardPoolModel)(object)ModelDb.CardPool<WatcherCardPool>();

	public override RelicPoolModel RelicPool => (RelicPoolModel)(object)ModelDb.RelicPool<WatcherRelicPool>();

	public override PotionPoolModel PotionPool => (PotionPoolModel)(object)ModelDb.PotionPool<WatcherPotionPool>();

	public override List<string> GetArchitectAttackVfx()
	{
		int num = 5;
		List<string> list = new List<string>(num);
		CollectionsMarshal.SetCount(list, num);
		Span<string> span = CollectionsMarshal.AsSpan(list);
		span[0] = "vfx/vfx_attack_blunt";
		span[1] = "vfx/vfx_heavy_blunt";
		span[2] = "vfx/vfx_attack_slash";
		span[3] = "vfx/vfx_bloody_impact";
		span[4] = "vfx/vfx_rock_shatter";
		return list;
	}
}
