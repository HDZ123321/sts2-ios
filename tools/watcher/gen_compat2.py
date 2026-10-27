"""Batch 2 of the BaseLib compatibility shim.

Two jobs:

1. Make the handful of native members that the mod overrides overridable.
   The base game declares several icon/path members as `private` or
   non-virtual because nothing outside the assembly ever needed to change
   them.  Baking the mod in means widening them to `protected virtual` /
   `public virtual`, which is a no-op for existing callers.

2. Emit the model base classes the 181 mod files inherit from.
"""

import io
import os
import re
import sys

BUNDLE = r"E:\sts2_port\ci_bundle"
NATIVE = os.path.join(BUNDLE, "src", "Core", "Models")
COMPAT = os.path.join(BUNDLE, "src", "WatcherCompat")

# ---------------------------------------------------------------------------
# 1. native widening
# ---------------------------------------------------------------------------

# (file, exact old line, new line)
WIDEN = [
    (
        "CharacterModel.cs",
        "\tpublic string TrailPath => SceneHelper.GetScenePath(\"vfx/card_trail_\" + base.Id.Entry.ToLowerInvariant());",
        "\tpublic virtual string TrailPath => SceneHelper.GetScenePath(\"vfx/card_trail_\" + base.Id.Entry.ToLowerInvariant());",
    ),
    (
        "CharacterModel.cs",
        "\tprivate string VisualsPath => SceneHelper.GetScenePath(\"creature_visuals/\" + base.Id.Entry.ToLowerInvariant());",
        "\tprotected virtual string VisualsPath => SceneHelper.GetScenePath(\"creature_visuals/\" + base.Id.Entry.ToLowerInvariant());",
    ),
    (
        "CharacterModel.cs",
        "\tprivate string IconTexturePath => ImageHelper.GetImagePath(\"ui/top_panel/character_icon_\" + base.Id.Entry.ToLowerInvariant() + \".png\");",
        "\tprotected virtual string IconTexturePath => ImageHelper.GetImagePath(\"ui/top_panel/character_icon_\" + base.Id.Entry.ToLowerInvariant() + \".png\");",
    ),
    (
        "CharacterModel.cs",
        "\tprivate string IconOutlineTexturePath => ImageHelper.GetImagePath(\"ui/top_panel/character_icon_\" + base.Id.Entry.ToLowerInvariant() + \"_outline.png\");",
        "\tprotected virtual string IconOutlineTexturePath => ImageHelper.GetImagePath(\"ui/top_panel/character_icon_\" + base.Id.Entry.ToLowerInvariant() + \"_outline.png\");",
    ),
    (
        "CharacterModel.cs",
        "\tpublic string EnergyCounterPath => SceneHelper.GetScenePath(\"combat/energy_counters/\" + base.Id.Entry.ToLowerInvariant() + \"_energy_counter\");",
        "\tpublic virtual string EnergyCounterPath => SceneHelper.GetScenePath(\"combat/energy_counters/\" + base.Id.Entry.ToLowerInvariant() + \"_energy_counter\");",
    ),
    (
        "CharacterModel.cs",
        "\tpublic string MerchantAnimPath => SceneHelper.GetScenePath(\"merchant/characters/\" + base.Id.Entry.ToLowerInvariant() + \"_merchant\");",
        "\tpublic virtual string MerchantAnimPath => SceneHelper.GetScenePath(\"merchant/characters/\" + base.Id.Entry.ToLowerInvariant() + \"_merchant\");",
    ),
    (
        "CharacterModel.cs",
        "\tpublic string RestSiteAnimPath => SceneHelper.GetScenePath(\"rest_site/characters/\" + base.Id.Entry.ToLowerInvariant() + \"_rest_site\");",
        "\tpublic virtual string RestSiteAnimPath => SceneHelper.GetScenePath(\"rest_site/characters/\" + base.Id.Entry.ToLowerInvariant() + \"_rest_site\");",
    ),
    (
        "CharacterModel.cs",
        "\tprivate string ArmPointingTexturePath => ImageHelper.GetImagePath(\"ui/hands/multiplayer_hand_\" + base.Id.Entry.ToLowerInvariant() + \"_point.png\");",
        "\tprotected virtual string ArmPointingTexturePath => ImageHelper.GetImagePath(\"ui/hands/multiplayer_hand_\" + base.Id.Entry.ToLowerInvariant() + \"_point.png\");",
    ),
    (
        "CharacterModel.cs",
        "\tprivate string ArmRockTexturePath => ImageHelper.GetImagePath(\"ui/hands/multiplayer_hand_\" + base.Id.Entry.ToLowerInvariant() + \"_rock.png\");",
        "\tprotected virtual string ArmRockTexturePath => ImageHelper.GetImagePath(\"ui/hands/multiplayer_hand_\" + base.Id.Entry.ToLowerInvariant() + \"_rock.png\");",
    ),
    (
        "CharacterModel.cs",
        "\tprivate string ArmPaperTexturePath => ImageHelper.GetImagePath(\"ui/hands/multiplayer_hand_\" + base.Id.Entry.ToLowerInvariant() + \"_paper.png\");",
        "\tprotected virtual string ArmPaperTexturePath => ImageHelper.GetImagePath(\"ui/hands/multiplayer_hand_\" + base.Id.Entry.ToLowerInvariant() + \"_paper.png\");",
    ),
    (
        "CharacterModel.cs",
        "\tprivate string ArmScissorsTexturePath => ImageHelper.GetImagePath(\"ui/hands/multiplayer_hand_\" + base.Id.Entry.ToLowerInvariant() + \"_scissors.png\");",
        "\tprotected virtual string ArmScissorsTexturePath => ImageHelper.GetImagePath(\"ui/hands/multiplayer_hand_\" + base.Id.Entry.ToLowerInvariant() + \"_scissors.png\");",
    ),
    (
        "CharacterModel.cs",
        "\tpublic string CharacterSelectBg => SceneHelper.GetScenePath(\"screens/char_select/char_select_bg_\" + base.Id.Entry.ToLowerInvariant());",
        "\tpublic virtual string CharacterSelectBg => SceneHelper.GetScenePath(\"screens/char_select/char_select_bg_\" + base.Id.Entry.ToLowerInvariant());",
    ),
    (
        "CharacterModel.cs",
        "\tpublic string CharacterSelectTransitionPath => \"res://materials/transitions/\" + base.Id.Entry.ToLowerInvariant() + \"_transition_mat.tres\";",
        "\tpublic virtual string CharacterSelectTransitionPath => \"res://materials/transitions/\" + base.Id.Entry.ToLowerInvariant() + \"_transition_mat.tres\";",
    ),
    (
        "CharacterModel.cs",
        "\tpublic string AttackSfx => $\"event:/sfx/characters/{base.Id.Entry.ToLowerInvariant()}/{base.Id.Entry.ToLowerInvariant()}_attack\";",
        "\tpublic virtual string AttackSfx => $\"event:/sfx/characters/{base.Id.Entry.ToLowerInvariant()}/{base.Id.Entry.ToLowerInvariant()}_attack\";",
    ),
    (
        "PowerModel.cs",
        "\tpublic string PackedIconPath => ImageHelper.GetImagePath(\"atlases/power_atlas.sprites/\" + base.Id.Entry.ToLowerInvariant() + \".tres\");",
        "\tpublic virtual string PackedIconPath => ImageHelper.GetImagePath(\"atlases/power_atlas.sprites/\" + base.Id.Entry.ToLowerInvariant() + \".tres\");",
    ),
    (
        "PowerModel.cs",
        "\tprivate string BigIconPath => ImageHelper.GetImagePath(\"powers/\" + base.Id.Entry.ToLowerInvariant() + \".png\");",
        "\tprotected virtual string BigIconPath => ImageHelper.GetImagePath(\"powers/\" + base.Id.Entry.ToLowerInvariant() + \".png\");",
    ),
    (
        "PotionModel.cs",
        "\tprivate string PackedImagePath => ImageHelper.GetImagePath(\"atlases/potion_atlas.sprites/\" + base.Id.Entry.ToLowerInvariant() + \".tres\");",
        "\tprotected virtual string PackedImagePath => ImageHelper.GetImagePath(\"atlases/potion_atlas.sprites/\" + base.Id.Entry.ToLowerInvariant() + \".tres\");",
    ),
    (
        "PotionModel.cs",
        "\tprivate string PackedOutlinePath => ImageHelper.GetImagePath(\"atlases/potion_outline_atlas.sprites/\" + base.Id.Entry.ToLowerInvariant() + \".tres\");",
        "\tprotected virtual string PackedOutlinePath => ImageHelper.GetImagePath(\"atlases/potion_outline_atlas.sprites/\" + base.Id.Entry.ToLowerInvariant() + \".tres\");",
    ),
]


def apply_widening():
    by_file = {}
    for name, old, new in WIDEN:
        by_file.setdefault(name, []).append((old, new))

    failures = []
    for name, pairs in by_file.items():
        path = os.path.join(NATIVE, name)
        with io.open(path, "r", encoding="utf-8", newline="") as handle:
            text = handle.read()
        changed = 0
        for old, new in pairs:
            if new in text and old not in text:
                continue  # already widened
            if old not in text:
                failures.append((name, old.strip()[:70]))
                continue
            text = text.replace(old, new, 1)
            changed += 1
        if changed:
            with io.open(path, "w", encoding="utf-8", newline="") as handle:
                handle.write(text)
            print("widened", changed, "member(s) in", name)
    if failures:
        print("FAILED to widen:")
        for name, snippet in failures:
            print("  ", name, "|", snippet)
        return False
    return True


# ---------------------------------------------------------------------------
# 2. shim sources
# ---------------------------------------------------------------------------

FILES = {}

FILES[r"BaseLib\Abstracts\ConstructedCardModel.cs"] = '''using System;
using System.Collections.Generic;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace BaseLib.Abstracts;

/// <summary>
/// Card base that collects its contents from builder calls instead of field
/// initialisers.  Everything the builders record is folded into the native
/// virtuals, so the rest of the game sees an ordinary <see cref="T:MegaCrit.Sts2.Core.Models.CardModel"/>.
/// </summary>
public abstract class ConstructedCardModel : CardModel
{
\tprivate readonly List<DynamicVar> _constructedVars = new List<DynamicVar>();
\tprivate readonly List<TooltipSource> _tooltipSources = new List<TooltipSource>();
\tprivate readonly List<Func<CardModel, IEnumerable<IHoverTip>>> _tooltipFactories = new List<Func<CardModel, IEnumerable<IHoverTip>>>();
\tprivate readonly HashSet<CardTag> _constructedTags = new HashSet<CardTag>();
\tprivate readonly HashSet<CardKeyword> _constructedKeywords = new HashSet<CardKeyword>();
\tprivate int _costUpgradeDelta;

\tprotected ConstructedCardModel(int canonicalEnergyCost, CardType type, CardRarity rarity, TargetType targetType, bool shouldShowInCardLibrary, bool alwaysOwnPortrait)
\t\t: base(canonicalEnergyCost, type, rarity, targetType, shouldShowInCardLibrary)
\t{
\t}

\t/// <summary>Overridden by the mod's own card bases; empty means "use the base game layout".</summary>
\tpublic virtual string CustomPortraitPath => string.Empty;

\tpublic override string PortraitPath
\t{
\t\tget
\t\t{
\t\t\tstring custom = CustomPortraitPath;
\t\t\tif (string.IsNullOrEmpty(custom))
\t\t\t{
\t\t\t\treturn base.PortraitPath;
\t\t\t}
\t\t\treturn custom.StartsWith("res://", StringComparison.Ordinal) ? custom : "res://" + custom;
\t\t}
\t}

\tprotected override IEnumerable<DynamicVar> CanonicalVars => _constructedVars;

\tprotected override HashSet<CardTag> CanonicalTags => _constructedTags;

\tpublic override IEnumerable<CardKeyword> CanonicalKeywords => _constructedKeywords;

\tprotected override IEnumerable<IHoverTip> ExtraHoverTips
\t{
\t\tget
\t\t{
\t\t\tforeach (TooltipSource source in _tooltipSources)
\t\t\t{
\t\t\t\tyield return source.Resolve(this);
\t\t\t}
\t\t\tforeach (Func<CardModel, IEnumerable<IHoverTip>> factory in _tooltipFactories)
\t\t\t{
\t\t\t\tforeach (IHoverTip tip in factory(this))
\t\t\t\t{
\t\t\t\t\tyield return tip;
\t\t\t\t}
\t\t\t}
\t\t}
\t}

\tprotected override void OnUpgrade()
\t{
\t\tforeach (DynamicVar variable in _constructedVars)
\t\t{
\t\t\tif (variable.HasUpgradeAmount())
\t\t\t{
\t\t\t\tvariable.UpgradeValueBy(variable.GetUpgradeAmount());
\t\t\t}
\t\t}
\t\tif (_costUpgradeDelta != 0)
\t\t{
\t\t\tEnergyCost.UpgradeBy(_costUpgradeDelta);
\t\t}
\t}

\tpublic ConstructedCardModel WithVar(DynamicVar variable)
\t{
\t\t_constructedVars.Add(variable);
\t\treturn this;
\t}

\tpublic ConstructedCardModel WithVars(params DynamicVar[] variables)
\t{
\t\tforeach (DynamicVar variable in variables)
\t\t{
\t\t\t_constructedVars.Add(variable);
\t\t}
\t\treturn this;
\t}

\tpublic ConstructedCardModel WithVars(IEnumerable<DynamicVar> variables)
\t{
\t\tforeach (DynamicVar variable in variables)
\t\t{
\t\t\t_constructedVars.Add(variable);
\t\t}
\t\treturn this;
\t}

\tpublic ConstructedCardModel WithTip(TooltipSource source)
\t{
\t\t_tooltipSources.Add(source);
\t\treturn this;
\t}

\tpublic ConstructedCardModel WithTips(Func<CardModel, IEnumerable<IHoverTip>> factory)
\t{
\t\t_tooltipFactories.Add(factory);
\t\treturn this;
\t}

\tpublic ConstructedCardModel WithTips(params IHoverTip[] tips)
\t{
\t\t_tooltipFactories.Add(delegate
\t\t{
\t\t\treturn tips;
\t\t});
\t\treturn this;
\t}

\tpublic ConstructedCardModel WithDamage(int baseValue, int upgrade)
\t{
\t\tDamageVar variable = new DamageVar("Damage", baseValue, ValueProp.Move);
\t\tif (upgrade != 0)
\t\t{
\t\t\tvariable.WithUpgrade(upgrade);
\t\t}
\t\treturn WithVar(variable);
\t}

\tpublic ConstructedCardModel WithBlock(int baseValue, int upgrade)
\t{
\t\tBlockVar variable = new BlockVar("Block", baseValue, ValueProp.Move);
\t\tif (upgrade != 0)
\t\t{
\t\t\tvariable.WithUpgrade(upgrade);
\t\t}
\t\treturn WithVar(variable);
\t}

\tpublic ConstructedCardModel WithCards(int baseValue, int upgrade)
\t{
\t\tCardsVar variable = new CardsVar(baseValue);
\t\tif (upgrade != 0)
\t\t{
\t\t\tvariable.WithUpgrade(upgrade);
\t\t}
\t\treturn WithVar(variable);
\t}

\tpublic ConstructedCardModel WithEnergy(int baseValue, int upgrade)
\t{
\t\tEnergyVar variable = new EnergyVar(baseValue);
\t\tif (upgrade != 0)
\t\t{
\t\t\tvariable.WithUpgrade(upgrade);
\t\t}
\t\treturn WithVar(variable);
\t}

\tpublic ConstructedCardModel WithEnergyTip()
\t{
\t\treturn WithTips(delegate (CardModel card)
\t\t{
\t\t\treturn new IHoverTip[1] { HoverTipFactory.ForEnergy(card) };
\t\t});
\t}

\tpublic ConstructedCardModel WithKeyword(CardKeyword keyword)
\t{
\t\t_constructedKeywords.Add(keyword);
\t\treturn this;
\t}

\tpublic ConstructedCardModel WithKeywords(params CardKeyword[] keywords)
\t{
\t\tforeach (CardKeyword keyword in keywords)
\t\t{
\t\t\t_constructedKeywords.Add(keyword);
\t\t}
\t\treturn this;
\t}

\tpublic ConstructedCardModel WithTags(params CardTag[] tags)
\t{
\t\tforeach (CardTag tag in tags)
\t\t{
\t\t\t_constructedTags.Add(tag);
\t\t}
\t\treturn this;
\t}

\tpublic ConstructedCardModel WithCostUpgradeBy(int delta)
\t{
\t\t_costUpgradeDelta = delta;
\t\treturn this;
\t}

\t/// <summary>
\t/// Brilliance-shaped damage: <c>baseValue + extraValue * multiplier</c>, where the
\t/// multiplier is recomputed from live combat state every time the value is read.
\t/// </summary>
\tpublic ConstructedCardModel WithCalculatedDamage(int baseValue, Func<CardModel, Creature, decimal> multiplier, ValueProp props, int extraValue, int upgradeExtra)
\t{
\t\tCalculationBaseVar baseVar = new CalculationBaseVar(baseValue);
\t\tExtraDamageVar extraVar = new ExtraDamageVar(extraValue);
\t\tif (upgradeExtra != 0)
\t\t{
\t\t\textraVar.WithUpgrade(upgradeExtra);
\t\t}
\t\tCalculatedDamageVar calculated = new CalculatedDamageVar(props).WithMultiplier(delegate (CardModel card, Creature? _)
\t\t{
\t\t\treturn multiplier(card, card.Owner.Creature);
\t\t});
\t\treturn WithVars(baseVar, extraVar, calculated);
\t}

\t/// <summary>Spirit Shield-shaped block, same arithmetic as the damage variant.</summary>
\tpublic ConstructedCardModel WithCalculatedBlock(int baseValue, int extraValue, Func<CardModel, Creature, decimal> multiplier, ValueProp props, int upgradeBase, int upgradeExtra)
\t{
\t\tCalculationBaseVar baseVar = new CalculationBaseVar(baseValue);
\t\tif (upgradeBase != 0)
\t\t{
\t\t\tbaseVar.WithUpgrade(upgradeBase);
\t\t}
\t\tCalculationExtraVar extraVar = new CalculationExtraVar(extraValue);
\t\tif (upgradeExtra != 0)
\t\t{
\t\t\textraVar.WithUpgrade(upgradeExtra);
\t\t}
\t\tCalculatedBlockVar calculated = new CalculatedBlockVar(props).WithMultiplier(delegate (CardModel card, Creature? _)
\t\t{
\t\t\treturn multiplier(card, card.Owner.Creature);
\t\t});
\t\treturn WithVars(baseVar, extraVar, calculated);
\t}
}
'''

FILES[r"BaseLib\Abstracts\CustomPowerModel.cs"] = '''using System;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

/// <summary>
/// Power base with mod-friendly icon paths.  The base game derives both icons
/// from the model id, which does not exist on disk for baked-in content.
/// </summary>
public abstract class CustomPowerModel : PowerModel
{
\tpublic virtual string CustomPackedIconPath => string.Empty;

\tpublic virtual string CustomBigIconPath => string.Empty;

\tpublic override string PackedIconPath
\t{
\t\tget
\t\t{
\t\t\tstring custom = CustomPackedIconPath;
\t\t\tif (string.IsNullOrEmpty(custom))
\t\t\t{
\t\t\t\treturn base.PackedIconPath;
\t\t\t}
\t\t\treturn custom.StartsWith("res://", StringComparison.Ordinal) ? custom : "res://" + custom;
\t\t}
\t}

\tprotected override string BigIconPath
\t{
\t\tget
\t\t{
\t\t\tstring custom = CustomBigIconPath;
\t\t\tif (string.IsNullOrEmpty(custom))
\t\t\t{
\t\t\t\treturn base.BigIconPath;
\t\t\t}
\t\t\treturn custom.StartsWith("res://", StringComparison.Ordinal) ? custom : "res://" + custom;
\t\t}
\t}
}
'''

FILES[r"BaseLib\Abstracts\CustomRelicModel.cs"] = '''using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

/// <summary>
/// Relic base.  The <c>alwaysShowCounter</c> flag mirrors BaseLib: relics that
/// opt in show a counter even when their amount is zero.
/// </summary>
public abstract class CustomRelicModel : RelicModel
{
\tprivate readonly bool _alwaysShowCounter;

\tprotected CustomRelicModel(bool alwaysShowCounter)
\t{
\t\t_alwaysShowCounter = alwaysShowCounter;
\t}

\tpublic override bool ShowCounter => _alwaysShowCounter || base.ShowCounter;
}
'''

FILES[r"BaseLib\Abstracts\CustomPotionModel.cs"] = '''using System;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

/// <summary>
/// Potion base with mod-friendly atlas paths.
/// </summary>
public abstract class CustomPotionModel : PotionModel
{
\tpublic virtual string CustomPackedImagePath => string.Empty;

\tpublic virtual string CustomPackedOutlinePath => string.Empty;

\tprotected override string PackedImagePath
\t{
\t\tget
\t\t{
\t\t\tstring custom = CustomPackedImagePath;
\t\t\tif (string.IsNullOrEmpty(custom))
\t\t\t{
\t\t\t\treturn base.PackedImagePath;
\t\t\t}
\t\t\treturn custom.StartsWith("res://", StringComparison.Ordinal) ? custom : "res://" + custom;
\t\t}
\t}

\tprotected override string PackedOutlinePath
\t{
\t\tget
\t\t{
\t\t\tstring custom = CustomPackedOutlinePath;
\t\t\tif (string.IsNullOrEmpty(custom))
\t\t\t{
\t\t\t\treturn base.PackedOutlinePath;
\t\t\t}
\t\t\treturn custom.StartsWith("res://", StringComparison.Ordinal) ? custom : "res://" + custom;
\t\t}
\t}
}
'''

FILES[r"BaseLib\Abstracts\CustomCharacterModel.cs"] = '''using System;
using System.Collections.Generic;
using BaseLib.Patches.UI;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

/// <summary>
/// Character base that redirects every path the base game builds from the model
/// id onto a mod-supplied path.  An empty custom path falls back to the base
/// convention, so a partially customised character still loads.
/// </summary>
public abstract class CustomCharacterModel : CharacterModel
{
\tpublic virtual string CustomVisualPath => string.Empty;

\tpublic virtual string CustomTrailPath => string.Empty;

\tpublic virtual string CustomIconPath => string.Empty;

\tpublic virtual string CustomIconTexturePath => string.Empty;

\tpublic virtual string CustomIconOutlineTexturePath => string.Empty;

\tpublic virtual string CustomCharacterSelectIconPath => string.Empty;

\tpublic virtual string CustomCharacterSelectLockedIconPath => string.Empty;

\tpublic virtual string CustomCharacterSelectBg => string.Empty;

\tpublic virtual string CustomCharacterSelectTransitionPath => string.Empty;

\tpublic virtual string CustomMapMarkerPath => string.Empty;

\tpublic virtual string CustomEnergyCounterPath => string.Empty;

\tpublic virtual string CustomRestSiteAnimPath => string.Empty;

\tpublic virtual string CustomMerchantAnimPath => string.Empty;

\tpublic virtual string CustomArmPointingTexturePath => string.Empty;

\tpublic virtual string CustomArmRockTexturePath => string.Empty;

\tpublic virtual string CustomArmPaperTexturePath => string.Empty;

\tpublic virtual string CustomArmScissorsTexturePath => string.Empty;

\tpublic virtual string CustomAttackSfx => string.Empty;

\tpublic virtual RelicIconData? CustomYummyCookie => null;

\tprivate static string Pick(string custom, string fallback)
\t{
\t\tif (string.IsNullOrEmpty(custom))
\t\t{
\t\t\treturn fallback;
\t\t}
\t\treturn custom.StartsWith("res://", StringComparison.Ordinal) ? custom : "res://" + custom;
\t}

\tpublic override string VisualsPath => Pick(CustomVisualPath, base.VisualsPath);

\tpublic override string TrailPath => Pick(CustomTrailPath, base.TrailPath);

\tprotected override string IconPath => Pick(CustomIconPath, base.IconPath);

\tprotected override string IconTexturePath => Pick(CustomIconTexturePath, base.IconTexturePath);

\tprotected override string IconOutlineTexturePath => Pick(CustomIconOutlineTexturePath, base.IconOutlineTexturePath);

\tprotected override string CharacterSelectIconPath => Pick(CustomCharacterSelectIconPath, base.CharacterSelectIconPath);

\tprotected override string CharacterSelectLockedIconPath => Pick(CustomCharacterSelectLockedIconPath, base.CharacterSelectLockedIconPath);

\tpublic override string CharacterSelectBg => Pick(CustomCharacterSelectBg, base.CharacterSelectBg);

\tpublic override string CharacterSelectTransitionPath => Pick(CustomCharacterSelectTransitionPath, base.CharacterSelectTransitionPath);

\tprotected override string MapMarkerPath => Pick(CustomMapMarkerPath, base.MapMarkerPath);

\tpublic override string EnergyCounterPath => Pick(CustomEnergyCounterPath, base.EnergyCounterPath);

\tpublic override string RestSiteAnimPath => Pick(CustomRestSiteAnimPath, base.RestSiteAnimPath);

\tpublic override string MerchantAnimPath => Pick(CustomMerchantAnimPath, base.MerchantAnimPath);

\tprotected override string ArmPointingTexturePath => Pick(CustomArmPointingTexturePath, base.ArmPointingTexturePath);

\tprotected override string ArmRockTexturePath => Pick(CustomArmRockTexturePath, base.ArmRockTexturePath);

\tprotected override string ArmPaperTexturePath => Pick(CustomArmPaperTexturePath, base.ArmPaperTexturePath);

\tprotected override string ArmScissorsTexturePath => Pick(CustomArmScissorsTexturePath, base.ArmScissorsTexturePath);

\tpublic override string AttackSfx => Pick(CustomAttackSfx, base.AttackSfx);
}
'''

FILES[r"BaseLib\Abstracts\CustomCardPoolModel.cs"] = '''using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

/// <summary>
/// Card pool that fills itself from the registry: every card carrying
/// <c>[Pool(typeof(ThisPool))]</c> ends up here, so mod cards do not have to be
/// listed twice.
/// </summary>
public abstract class CustomCardPoolModel : CardPoolModel
{
\tpublic virtual float H => 0f;

\tpublic virtual float S => 0f;

\tpublic virtual float V => 0f;

\tpublic virtual string? BigEnergyIconPath => null;

\tpublic virtual string? TextEnergyIconPath => null;

\tpublic override string CardFrameMaterialPath => "card_frame_" + EnergyColorName.ToLowerInvariant();

\tprotected override CardModel[] GenerateAllCards()
\t{
\t\treturn WatcherRegistry.CardsFor(GetType()).ToArray();
\t}
}
'''

FILES[r"BaseLib\Abstracts\CustomRelicPoolModel.cs"] = '''using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

public abstract class CustomRelicPoolModel : RelicPoolModel
{
\tprotected override IEnumerable<RelicModel> GenerateAllRelics()
\t{
\t\treturn WatcherRegistry.RelicsFor(GetType());
\t}
}
'''

FILES[r"BaseLib\Abstracts\CustomPotionPoolModel.cs"] = '''using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

public abstract class CustomPotionPoolModel : PotionPoolModel
{
\tprotected override IEnumerable<PotionModel> GenerateAllPotions()
\t{
\t\treturn WatcherRegistry.PotionsFor(GetType());
\t}
}
'''

FILES[r"BaseLib\Abstracts\CustomSingletonModel.cs"] = '''using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

/// <summary>
/// A model that exists once per save rather than per combat: it is registered
/// with the run so its hooks fire even outside combat.
/// </summary>
public abstract class CustomSingletonModel : AbstractModel
{
\tprotected CustomSingletonModel(HookType hookType)
\t{
\t\tHookType = hookType;
\t}

\tpublic HookType HookType { get; }

\tpublic override bool ShouldReceiveCombatHooks => HookType != HookType.Run;
}
'''

FILES[r"BaseLib\Abstracts\CustomTemporaryPowerModelWrapper.cs"] = '''using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

/// <summary>
/// Marker power that stands in for a real power for one turn.  The card grants
/// the wrapper, the wrapper forwards its amount to <typeparamref name="TPower"/>,
/// and the wrapper itself is dropped when the side's turn ends.
/// </summary>
public abstract class CustomTemporaryPowerModelWrapper<TCard, TPower> : PowerModel
\twhere TCard : CardModel
\twhere TPower : PowerModel
{
\tpublic override PowerType Type => PowerType.Buff;

\tpublic override PowerStackType StackType => PowerStackType.Counter;

\tpublic override async Task AfterApplied(Creature? applier, CardModel? cardSource)
\t{
\t\tif (Amount <= 0)
\t\t{
\t\t\treturn;
\t\t}
\t\tTPower canonical = ModelDb.GetById<TPower>(ModelDb.GetId<TPower>());
\t\tPowerModel instance = canonical.ToMutable(Amount);
\t\tOwner.ApplyPowerInternal(instance);
\t\tawait Task.CompletedTask;
\t}
}
'''

FILES[r"BaseLib\Utils\CommonActions.cs"] = '''using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

/// <summary>
/// The four things almost every card does, spelled once.
/// </summary>
public static class CommonActions
{
\t/// <summary>
\t/// Sets up an attack for <paramref name="card"/>.  A card carrying a
\t/// <c>CalculatedDamage</c> var attacks with that instead of the flat damage.
\t/// </summary>
\tpublic static AttackCommand CardAttack(CardModel card, CardPlay cardPlay, int hitCount, string? vfx = null, string? sfx = null, string? tmpSfx = null)
\t{
\t\tArgumentNullException.ThrowIfNull(card);
\t\tArgumentNullException.ThrowIfNull(cardPlay);
\t\tAttackCommand command;
\t\tif (card.DynamicVars.ContainsKey("CalculatedDamage"))
\t\t{
\t\t\tcommand = DamageCmd.Attack(card.DynamicVars.CalculatedDamage);
\t\t}
\t\telse
\t\t{
\t\t\tcommand = DamageCmd.Attack(card.DynamicVars.Damage.BaseValue);
\t\t}
\t\tcommand.FromCard(card);
\t\tif (cardPlay.Target != null)
\t\t{
\t\t\tcommand.Targeting(cardPlay.Target);
\t\t}
\t\telse
\t\t{
\t\t\tcommand.TargetingAllOpponents(card.Owner.Creature.CombatState);
\t\t}
\t\tif (hitCount > 1)
\t\t{
\t\t\tcommand.WithHitCount(hitCount);
\t\t}
\t\tif (vfx != null || sfx != null || tmpSfx != null)
\t\t{
\t\t\tcommand.WithHitFx(vfx, sfx, tmpSfx);
\t\t}
\t\treturn command;
\t}

\tpublic static Task<decimal> CardBlock(CardModel card, CardPlay cardPlay)
\t{
\t\tArgumentNullException.ThrowIfNull(card);
\t\treturn CreatureCmd.GainBlock(card.Owner.Creature, card.DynamicVars.Block, cardPlay);
\t}

\t/// <summary>Applies the card's matching power var to the player who played it.</summary>
\tpublic static Task ApplySelf<T>(PlayerChoiceContext choiceContext, CardModel card, bool silent = false) where T : PowerModel
\t{
\t\treturn Apply<T>(choiceContext, card.Owner.Creature, card, silent);
\t}

\t/// <summary>Applies the card's matching power var to whatever the card targeted.</summary>
\tpublic static Task Apply<T>(PlayerChoiceContext choiceContext, CardModel card, CardPlay cardPlay, bool silent = false) where T : PowerModel
\t{
\t\tCreature target = cardPlay.Target ?? card.Owner.Creature;
\t\treturn Apply<T>(choiceContext, target, card, silent);
\t}

\tpublic static Task Apply<T>(PlayerChoiceContext choiceContext, Creature target, CardModel card, bool silent = false) where T : PowerModel
\t{
\t\tdecimal amount = card.DynamicVars.Power<T>().BaseValue;
\t\treturn PowerCmd.Apply<T>(choiceContext, target, amount, card.Owner.Creature, card, silent);
\t}

\tpublic static Task Apply<T>(PlayerChoiceContext choiceContext, IEnumerable<Creature> targets, DynamicVarSource source, bool silent = false) where T : PowerModel
\t{
\t\tdecimal amount = 0m;
\t\tif (source.Vars != null)
\t\t{
\t\t\tamount = source.Vars.Power<T>().BaseValue;
\t\t}
\t\treturn PowerCmd.Apply<T>(choiceContext, targets, amount, null, null, silent);
\t}

\tpublic static Task Draw(PlayerChoiceContext choiceContext, CardModel card, int count = 1)
\t{
\t\tArgumentNullException.ThrowIfNull(card);
\t\treturn CardPileCmd.Draw(choiceContext, count, card.Owner);
\t}
}
'''

FILES[r"BaseLib\Commands\ScryCmd.cs"] = '''using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Hooks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Commands;

/// <summary>
/// Scry: look at the top N cards of the draw pile, discard any number of them,
/// and put the rest back on top in the same order.
/// <para>
/// The base game has no scry system, so this is a native implementation.  It
/// reuses <see cref="T:MegaCrit.Sts2.Core.Commands.CardSelectCmd"/> for the
/// selection screen so it behaves like every other "choose a card" flow.
/// </para>
/// </summary>
public static class ScryCmd
{
\tpublic static async Task Execute(PlayerChoiceContext choiceContext, CardModel source)
\t{
\t\tArgumentNullException.ThrowIfNull(source);
\t\tPlayer? owner = source.Owner;
\t\tif (owner?.PlayerCombatState == null)
\t\t{
\t\t\treturn;
\t\t}
\t\tint requested = source.DynamicVars.ContainsKey("Scry") ? source.DynamicVars["Scry"].IntValue : 0;
\t\tawait Execute(choiceContext, owner, requested);
\t}

\tpublic static async Task Execute(PlayerChoiceContext choiceContext, Player player, int amount)
\t{
\t\tif (player.PlayerCombatState == null || amount <= 0)
\t\t{
\t\t\treturn;
\t\t}
\t\tint scryAmount = ModifyAmount(player, amount);
\t\tif (scryAmount <= 0)
\t\t{
\t\t\treturn;
\t\t}
\t\tCardPile drawPile = player.PlayerCombatState.DrawPile;
\t\tList<CardModel> seen = drawPile.Cards.Take(Math.Min(scryAmount, drawPile.Cards.Count)).ToList();
\t\tif (seen.Count == 0)
\t\t{
\t\t\treturn;
\t\t}

\t\tList<CardModel> discarded = new List<CardModel>();
\t\tCardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 0, seen.Count)
\t\t{
\t\t\tCancelable = true
\t\t};
\t\tIEnumerable<CardModel> chosen = await CardSelectCmd.FromSimpleGrid(choiceContext, seen, player, prefs);
\t\tforeach (CardModel card in chosen.ToList())
\t\t{
\t\t\tif (seen.Remove(card))
\t\t\t{
\t\t\t\tdiscarded.Add(card);
\t\t\t}
\t\t}

\t\tforeach (CardModel card in discarded)
\t\t{
\t\t\tCardPile? discardPile = CardPile.Get(PileType.Discard, player);
\t\t\tdiscardPile?.AddInternal(card, -1, true);
\t\t\tif (drawPile.Cards.Contains(card))
\t\t\t{
\t\t\t\tdrawPile.RemoveInternal(card, true);
\t\t\t}
\t\t}

\t\tawait Dispatch(choiceContext, player, scryAmount, discarded.Count, seen, discarded);
\t}

\tprivate static int ModifyAmount(Player player, int amount)
\t{
\t\tint current = amount;
\t\tCombatState? combatState = player.Creature.CombatState;
\t\tif (combatState == null)
\t\t{
\t\t\treturn current;
\t\t}
\t\tforeach (RelicModel relic in player.Relics)
\t\t{
\t\t\tif (relic is IModifyScryAmount modifier)
\t\t\t{
\t\t\t\tcurrent = modifier.ModifyScryAmount(player, current);
\t\t\t}
\t\t}
\t\ttry
\t\t{
\t\t\tforeach (AbstractModel model in combatState.IterateHookListeners())
\t\t\t{
\t\t\t\tif (model is IModifyScryAmount modifier2)
\t\t\t\t{
\t\t\t\t\tcurrent = modifier2.ModifyScryAmount(player, current);
\t\t\t\t}
\t\t\t}
\t\t}
\t\tcatch (InvalidOperationException)
\t\t{
\t\t\t// Not in combat after all; the relic pass above is enough.
\t\t}
\t\treturn current;
\t}

\tprivate static async Task Dispatch(PlayerChoiceContext choiceContext, Player player, int scryAmount, int discardAmount, List<CardModel> seen, List<CardModel> discarded)
\t{
\t\tCombatState? combatState = player.Creature.CombatState;
\t\tif (combatState == null)
\t\t{
\t\t\treturn;
\t\t}
\t\tforeach (AbstractModel model in combatState.IterateHookListeners().ToList())
\t\t{
\t\t\tif (model is IAfterScryed after)
\t\t\t{
\t\t\t\tawait after.AfterScryed(choiceContext, player, scryAmount, discardAmount, seen, discarded);
\t\t\t}
\t\t}
\t}
}
'''


def main():
    if not apply_widening():
        return 1
    for relative, text in FILES.items():
        path = os.path.join(COMPAT, relative)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with io.open(path, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)
        print("wrote", relative, len(text))
    print("files", len(FILES))
    return 0


if __name__ == "__main__":
    sys.exit(main())
