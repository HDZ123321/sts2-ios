"""Batch 3 of the BaseLib compatibility shim.

Everything here is either
  * a native member widened so the baked-in mod can override or reach it, or
  * a shim the mod calls with a shape the first pass guessed wrong.

Run it twice safely: every edit is a literal replace and re-runs are no-ops.
"""

import io
import os
import re
import sys

BUNDLE = r"E:\sts2_port\ci_bundle"
COMPAT = os.path.join(BUNDLE, "src", "WatcherCompat")
WATCHER = os.path.join(BUNDLE, "src", "Watcher")


def read(path):
    with io.open(path, "r", encoding="utf-8", newline="") as handle:
        return handle.read()


def write(path, text):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with io.open(path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(text)


def edit(rel, pairs, base=BUNDLE):
    path = os.path.join(base, rel)
    text = read(path)
    touched = 0
    for old, new in pairs:
        if old in text:
            text = text.replace(old, new, 1)
            touched += 1
        elif new not in text:
            print("  MISS", rel, "|", old.splitlines()[0][:70])
    if touched:
        write(path, text)
        print("edited", rel, touched)


# ---------------------------------------------------------------------------
# A. native widening
# ---------------------------------------------------------------------------

NATIVE_EDITS = [
    # PowerModel.Flash is only ever called from subclasses in the base game; the
    # mod calls it through a PowerModel-typed reference.
    ("src/Core/Models/PowerModel.cs", [
        ("\tprotected void Flash()", "\tpublic void Flash()"),
    ]),
    # Same story for the card selection prompt: the mod reads it off a CardModel.
    ("src/Core/Models/CardModel.cs", [
        ("\tprotected LocString SelectionScreenPrompt", "\tpublic LocString SelectionScreenPrompt"),
    ]),
    # Creature visuals: the mod's spine eye hangs off the body node.
    ("src/Core/Nodes/Combat/NCreatureVisuals.cs", [
        ("\tprivate Node2D _body;", "\tpublic Node2D _body;"),
    ]),
    # Rest site character index: read by the mod's rest-site pose override.
    ("src/Core/Nodes/RestSite/NRestSiteCharacter.cs", [
        ("\tprivate int _characterIndex;", "\tpublic int _characterIndex;"),
    ]),
    # Hover tips: let a caller pass a ready-made tip where the base game wants
    # one of its static enum values.
    ("src/Core/HoverTips/HoverTipFactory.cs", [
        (
            "\tpublic static IHoverTip FromKeyword(CardKeyword keyword)",
            "\tpublic static IHoverTip Static(IHoverTip tip, params DynamicVar[] vars)\n"
            "\t{\n"
            "\t\tif (vars == null || vars.Length == 0)\n"
            "\t\t{\n"
            "\t\t\treturn tip;\n"
            "\t\t}\n"
            "\t\treturn new HoverTip(tip.Title, tip.Description, vars);\n"
            "\t}\n\n"
            "\tpublic static IHoverTip FromKeyword(CardKeyword keyword)",
        ),
    ]),
]

# ---------------------------------------------------------------------------
# B. shim edits
# ---------------------------------------------------------------------------

SHIM_EDITS = [
    # TooltipSource: the decompiled mod calls the conversion explicitly, which C#
    # forbids for real operators, so these are plain static methods with the same
    # name the compiler would have generated.
    ("src/WatcherCompat/BaseLib/Utils/TooltipSource.cs", [
        (
            "\tpublic IHoverTip Resolve(CardModel card)\n\t{\n\t\treturn _factory(card);\n\t}\n}",
            "\tpublic IHoverTip Resolve(CardModel card)\n\t{\n\t\treturn _factory(card);\n\t}\n\n"
            "\tpublic static TooltipSource op_Implicit(BaseLibTip tip)\n\t{\n"
            "\t\treturn new TooltipSource(delegate { return tip; });\n\t}\n\n"
            "\tpublic static TooltipSource op_Implicit(Type type)\n\t{\n"
            "\t\treturn new TooltipSource(delegate { return HoverTipFactory.FromCard(type) ?? BaseLibTip.Scry; });\n\t}\n\n"
            "\tpublic static TooltipSource op_Implicit(CardKeyword keyword)\n\t{\n"
            "\t\treturn new TooltipSource(delegate { return HoverTipFactory.FromKeyword(keyword); });\n\t}\n\n"
            "\tpublic static TooltipSource op_Implicit(CardModel card)\n\t{\n"
            "\t\treturn new TooltipSource(delegate { return HoverTipFactory.FromCard(card); });\n\t}\n\n"
            "\tpublic static TooltipSource op_Implicit(PowerModel power)\n\t{\n"
            "\t\treturn new TooltipSource(delegate { return HoverTipFactory.FromPower(power); });\n\t}\n\n"
            "\tpublic static TooltipSource op_Implicit(RelicModel relic)\n\t{\n"
            "\t\treturn new TooltipSource(delegate { return HoverTipFactory.FromRelic(relic); });\n\t}\n}",
        ),
    ]),
    # DynamicVarSource: same explicit-call problem.
    ("src/WatcherCompat/BaseLib/Utils/DynamicVarSource.cs", [
        (
            "\tpublic static implicit operator DynamicVarSource(CardModel card)\n\t{\n\t\treturn new DynamicVarSource(card.DynamicVars);\n\t}\n\n"
            "\tpublic static implicit operator DynamicVarSource(PowerModel power)\n\t{\n\t\treturn new DynamicVarSource(power.DynamicVars);\n\t}\n\n"
            "\tpublic static implicit operator DynamicVarSource(RelicModel relic)\n\t{\n\t\treturn new DynamicVarSource(relic.DynamicVars);\n\t}",
            "\tpublic static DynamicVarSource op_Implicit(CardModel card)\n\t{\n\t\treturn new DynamicVarSource(card.DynamicVars);\n\t}\n\n"
            "\tpublic static DynamicVarSource op_Implicit(PowerModel power)\n\t{\n\t\treturn new DynamicVarSource(power.DynamicVars);\n\t}\n\n"
            "\tpublic static DynamicVarSource op_Implicit(RelicModel relic)\n\t{\n\t\treturn new DynamicVarSource(relic.DynamicVars);\n\t}",
        ),
    ]),
]


# ---------------------------------------------------------------------------
# C. ConstructedCardModel: the real builder surface
# ---------------------------------------------------------------------------

BUILDER_EXTRAS = '''
\tpublic ConstructedCardModel WithPower<T>(int baseValue, int upgrade) where T : PowerModel
\t{
\t\tPowerVar<T> variable = new PowerVar<T>(baseValue);
\t\tif (upgrade != 0)
\t\t{
\t\t\tvariable.WithUpgrade(upgrade);
\t\t}
\t\treturn WithVar(variable);
\t}

\tpublic ConstructedCardModel WithPower<T>(int baseValue, bool showTooltip = true) where T : PowerModel
\t{
\t\treturn WithPower<T>(baseValue, 0);
\t}

\tpublic ConstructedCardModel WithPower<T>(int baseValue, int upgrade, bool showTooltip) where T : PowerModel
\t{
\t\treturn WithPower<T>(baseValue, upgrade);
\t}

\tpublic ConstructedCardModel WithVar(string name, int baseValue, int upgrade)
\t{
\t\tDynamicVar variable = new DynamicVar(name, baseValue);
\t\tif (upgrade != 0)
\t\t{
\t\t\tvariable.WithUpgrade(upgrade);
\t\t}
\t\treturn WithVar(variable);
\t}

\t/// <summary>
\t/// Adds a keyword that only shows up once the card is upgraded.
\t/// </summary>
\tpublic ConstructedCardModel WithKeyword(CardKeyword keyword, UpgradeType upgradeType)
\t{
\t\tif (upgradeType == UpgradeType.Upgrade)
\t\t{
\t\t\t_upgradeOnlyKeywords.Add(keyword);
\t\t}
\t\telse
\t\t{
\t\t\t_constructedKeywords.Add(keyword);
\t\t}
\t\treturn this;
\t}
'''


def patch_builder():
    rel = "src/WatcherCompat/BaseLib/Abstracts/ConstructedCardModel.cs"
    path = os.path.join(BUNDLE, rel)
    text = read(path)
    if "WithPower<T>(int baseValue, int upgrade)" in text:
        print("  builder already patched")
        return
    text = text.replace(
        "\tprivate int _costUpgradeDelta;",
        "\tprivate readonly HashSet<CardKeyword> _upgradeOnlyKeywords = new HashSet<CardKeyword>();\n"
        "\tprivate int _costUpgradeDelta;",
        1,
    )
    text = text.replace(
        "\tpublic override IEnumerable<CardKeyword> CanonicalKeywords => _constructedKeywords;",
        "\tpublic override IEnumerable<CardKeyword> CanonicalKeywords\n"
        "\t{\n"
        "\t\tget\n"
        "\t\t{\n"
        "\t\t\tforeach (CardKeyword keyword in _constructedKeywords)\n"
        "\t\t\t{\n"
        "\t\t\t\tyield return keyword;\n"
        "\t\t\t}\n"
        "\t\t\tCardModel canonical = this;\n"
        "\t\t\tif (canonical.IsUpgraded)\n"
        "\t\t\t{\n"
        "\t\t\t\tforeach (CardKeyword keyword2 in _upgradeOnlyKeywords)\n"
        "\t\t\t\t{\n"
        "\t\t\t\t\tyield return keyword2;\n"
        "\t\t\t\t}\n"
        "\t\t\t}\n"
        "\t\t}\n"
        "\t}",
        1,
    )
    text = text.replace(
        "\t\tCalculatedDamageVar calculated = new CalculatedDamageVar(props).WithMultiplier(delegate (CardModel card, Creature? _)\n"
        "\t\t{\n"
        "\t\t\treturn multiplier(card, card.Owner.Creature);\n"
        "\t\t});",
        "\t\tCalculatedVar calculated = new CalculatedDamageVar(props).WithMultiplier(delegate (CardModel card, Creature? _)\n"
        "\t\t{\n"
        "\t\t\treturn multiplier(card, card.Owner.Creature);\n"
        "\t\t});",
        1,
    )
    text = text.replace(
        "\t\tCalculatedBlockVar calculated = new CalculatedBlockVar(props).WithMultiplier(delegate (CardModel card, Creature? _)\n"
        "\t\t{\n"
        "\t\t\treturn multiplier(card, card.Owner.Creature);\n"
        "\t\t});",
        "\t\tCalculatedVar calculated = new CalculatedBlockVar(props).WithMultiplier(delegate (CardModel card, Creature? _)\n"
        "\t\t{\n"
        "\t\t\treturn multiplier(card, card.Owner.Creature);\n"
        "\t\t});",
        1,
    )
    index = text.rstrip().rfind("}")
    text = text[:index] + BUILDER_EXTRAS + "}\n"
    write(path, text)
    print("edited", rel)


# ---------------------------------------------------------------------------
# D. CommonActions: the amount overload
# ---------------------------------------------------------------------------

def patch_common_actions():
    rel = "src/WatcherCompat/BaseLib/Utils/CommonActions.cs"
    path = os.path.join(BUNDLE, rel)
    text = read(path)
    if "decimal amount, bool silent" in text:
        print("  common actions already patched")
        return
    text = text.replace(
        "\tpublic static Task Apply<T>(PlayerChoiceContext choiceContext, Creature target, CardModel card, bool silent = false) where T : PowerModel",
        "\tpublic static Task ApplySelf<T>(PlayerChoiceContext choiceContext, CardModel card, decimal amount, bool silent = false) where T : PowerModel\n"
        "\t{\n"
        "\t\tArgumentNullException.ThrowIfNull(card);\n"
        "\t\treturn PowerCmd.Apply<T>(choiceContext, card.Owner.Creature, amount, card.Owner.Creature, card, silent);\n"
        "\t}\n\n"
        "\tpublic static Task Apply<T>(PlayerChoiceContext choiceContext, Creature target, CardModel card, bool silent = false) where T : PowerModel",
        1,
    )
    write(path, text)
    print("edited", rel)


# ---------------------------------------------------------------------------
# E. ScryCmd: ICombatState, and drop the invalid RelicModel loop shape
# ---------------------------------------------------------------------------

def patch_scry():
    rel = "src/WatcherCompat/BaseLib/Commands/ScryCmd.cs"
    path = os.path.join(BUNDLE, rel)
    text = read(path)
    text = text.replace(
        "\t\tCombatState? combatState = player.Creature.CombatState;\n\t\tif (combatState == null)\n\t\t{\n\t\t\treturn current;\n\t\t}",
        "\t\tICombatState? combatState = player.Creature.CombatState;\n\t\tif (combatState == null)\n\t\t{\n\t\t\treturn current;\n\t\t}",
        1,
    )
    text = text.replace(
        "\t\tCombatState? combatState = player.Creature.CombatState;\n\t\tif (combatState == null)\n\t\t{\n\t\t\treturn;\n\t\t}",
        "\t\tICombatState? combatState = player.Creature.CombatState;\n\t\tif (combatState == null)\n\t\t{\n\t\t\treturn;\n\t\t}",
        1,
    )
    write(path, text)
    print("edited", rel)


# ---------------------------------------------------------------------------
# F. the mod's own StringExtensions is short one method
# ---------------------------------------------------------------------------

def patch_mod_string_extensions():
    rel = "src/Watcher/Extensions/StringExtensions.cs"
    path = os.path.join(BUNDLE, rel)
    text = read(path)
    if "RemovePrefix" in text:
        print("  RemovePrefix already present")
        return
    text = text.replace(
        "\tpublic static string CardImagePath(this string path)",
        "\t/// <summary>\n"
        "\t/// Strips the model-id prefix a modded entity carries, so\n"
        "\t/// <c>WATCHER_COOKIE</c> becomes <c>COOKIE</c> before it is turned into an\n"
        "\t/// asset name.\n"
        "\t/// </summary>\n"
        "\tpublic static string RemovePrefix(this string value, string prefix = \"WATCHER_\")\n"
        "\t{\n"
        "\t\tif (value != null && prefix != null && value.StartsWith(prefix, global::System.StringComparison.Ordinal))\n"
        "\t\t{\n"
        "\t\t\treturn value.Substring(prefix.Length);\n"
        "\t\t}\n"
        "\t\treturn value;\n"
        "\t}\n\n"
        "\tpublic static string CardImagePath(this string path)",
        1,
    )
    write(path, text)
    print("edited", rel)


# ---------------------------------------------------------------------------
# G. new shim files
# ---------------------------------------------------------------------------

NEW_FILES = {}

NEW_FILES["src/WatcherCompat/BaseLib/Utils/UpgradeType.cs"] = '''namespace BaseLib.Utils;

/// <summary>
/// Tells a builder whether the thing it is adding applies always or only once
/// the card is upgraded.
/// </summary>
public enum UpgradeType
{
\tAlways = 1,
\tUpgrade = 2,
\tNever = 0
}
'''

NEW_FILES["src/WatcherCompat/BaseLib/Utils/HookUtils.cs"] = '''using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

/// <summary>
/// Runs a hook over every model that is listening in this combat.  The base game
/// dispatches its own hooks through static <c>Hook</c> methods; mod hooks have no
/// such wrapper, so they share this one.
/// </summary>
public static class HookUtils
{
\tpublic static IEnumerable<T> Listeners<T>(ICombatState? combatState) where T : class
\t{
\t\tif (combatState == null)
\t\t{
\t\t\tyield break;
\t\t}
\t\tforeach (AbstractModel model in combatState.IterateHookListeners())
\t\t{
\t\t\tif (model is T typed)
\t\t\t{
\t\t\t\tyield return typed;
\t\t\t}
\t\t}
\t}

\tpublic static async Task Dispatch<T>(ICombatState? combatState, PlayerChoiceContext choiceContext, Func<T, Task> action) where T : class
\t{
\t\tif (combatState == null || action == null)
\t\t{
\t\t\treturn;
\t\t}
\t\tforeach (T listener in new List<T>(Listeners<T>(combatState)))
\t\t{
\t\t\tawait action(listener);
\t\t}
\t}

\tpublic static TValue Aggregate<T, TValue>(ICombatState? combatState, TValue baseValue, Func<T, TValue, TValue> fold) where T : class
\t{
\t\tif (combatState == null || fold == null)
\t\t{
\t\t\treturn baseValue;
\t\t}
\t\tTValue current = baseValue;
\t\tforeach (T listener in new List<T>(Listeners<T>(combatState)))
\t\t{
\t\t\tcurrent = fold(listener, current);
\t\t}
\t\treturn current;
\t}
}
'''

NEW_FILES["src/WatcherCompat/BaseLib/Utils/ModCredits.cs"] = '''using System.Collections.Generic;

namespace BaseLib.Utils;

/// <summary>How a credits section lays its entries out.</summary>
public enum Layout
{
\tVertical = 1,
\tHorizontal = 2
}

/// <summary>One named block of the credits screen.</summary>
public sealed class Section
{
\tpublic Section(string title, Layout layout, Section[]? children = null)
\t{
\t\tTitle = title;
\t\tLayout = layout;
\t\tChildren = children ?? System.Array.Empty<Section>();
\t}

\tpublic string Title { get; }

\tpublic Layout Layout { get; }

\tpublic IReadOnlyList<Section> Children { get; }
}

/// <summary>
/// Credits metadata.  The baked-in build has a single fixed credits screen, so
/// registering only records the names for the in-game mod list.
/// </summary>
public static class ModCredits
{
\tprivate static readonly Dictionary<string, IReadOnlyList<Section>> _registered = new Dictionary<string, IReadOnlyList<Section>>();

\tpublic static IReadOnlyDictionary<string, IReadOnlyList<Section>> Registered => _registered;

\tpublic static void Register(string modId, params Section[] sections)
\t{
\t\tif (string.IsNullOrEmpty(modId))
\t\t{
\t\t\treturn;
\t\t}
\t\t_registered[modId] = sections ?? System.Array.Empty<Section>();
\t}
}
'''

NEW_FILES["src/WatcherCompat/BaseLib/Utils/WatcherRegistry.cs"] = '''using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

/// <summary>
/// Answers "which cards belong to this pool" without reflection.
/// <para>
/// BaseLib found pool members by scanning every loaded assembly, which the
/// NativeAOT build cannot do.  Baked in, the membership is known at build time,
/// so <c>WatcherRegistry.Generated.cs</c> spells it out with one generic call per
/// model and this class just groups them.
/// </para>
/// </summary>
public static partial class WatcherRegistry
{
\tprivate static readonly Dictionary<Type, List<AbstractModel>> _byPool = new Dictionary<Type, List<AbstractModel>>();

\tprivate static bool _populated;

\tpublic static IEnumerable<CardModel> CardsFor(Type poolType)
\t{
\t\treturn ItemsFor<CardModel>(poolType);
\t}

\tpublic static IEnumerable<RelicModel> RelicsFor(Type poolType)
\t{
\t\treturn ItemsFor<RelicModel>(poolType);
\t}

\tpublic static IEnumerable<PotionModel> PotionsFor(Type poolType)
\t{
\t\treturn ItemsFor<PotionModel>(poolType);
\t}

\tprivate static IEnumerable<T> ItemsFor<T>(Type poolType) where T : AbstractModel
\t{
\t\tEnsurePopulated();
\t\tif (!_byPool.TryGetValue(poolType, out List<AbstractModel>? items))
\t\t{
\t\t\tyield break;
\t\t}
\t\tforeach (AbstractModel item in items)
\t\t{
\t\t\tif (item is T typed)
\t\t\t{
\t\t\t\tyield return typed;
\t\t\t}
\t\t}
\t}

\tprivate static void EnsurePopulated()
\t{
\t\tif (_populated)
\t\t{
\t\t\treturn;
\t\t}
\t\t_populated = true;
\t\tRegisterAll();
\t}

\tprivate static void Add<TModel, TPool>() where TModel : AbstractModel
\t{
\t\tModelDb.Inject(typeof(TModel));
\t\tTModel model = ModelDb.GetById<TModel>(ModelDb.GetId<TModel>());
\t\tType poolType = typeof(TPool);
\t\tif (!_byPool.TryGetValue(poolType, out List<AbstractModel>? items))
\t\t{
\t\t\titems = new List<AbstractModel>();
\t\t\t_byPool[poolType] = items;
\t\t}
\t\titems.Add(model);
\t}

\tprivate static void AddCard<TModel, TPool>() where TModel : CardModel
\t{
\t\tAdd<TModel, TPool>();
\t}
}
'''

NEW_FILES["src/WatcherCompat/BaseLib/Utils/DynamicVarScryExtensions.cs"] = '''using System.Linq;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace BaseLib.Utils;

public static class DynamicVarScryExtensions
{
\t/// <summary>The scry amount a card carries, or zero when it does not scry.</summary>
\tpublic static DynamicVar Scry(this DynamicVarSet vars)
\t{
\t\tif (vars == null)
\t\t{
\t\t\tthrow new System.ArgumentNullException(nameof(vars));
\t\t}
\t\tif (vars.TryGetValue("Scry", out DynamicVar? value))
\t\t{
\t\t\treturn value;
\t\t}
\t\treturn vars.FirstOrDefault((DynamicVar v) => v is BaseLib.Cards.Variables.ScryVar) ?? new BaseLib.Cards.Variables.ScryVar(0m);
\t}
}
'''


# ---------------------------------------------------------------------------
# H. collect the mod's models, then emit the registry + subtype registration
# ---------------------------------------------------------------------------

FAMILY = {
    "WatcherCardModel": "card",
    "WatcherPowerModel": "power",
    "WatcherRelicModel": "relic",
    "WatcherPotionModel": "potion",
    "CustomCardPoolModel": "cardpool",
    "CustomRelicPoolModel": "relicpool",
    "CustomPotionPoolModel": "potionpool",
    "CustomCharacterModel": "character",
    "CustomSingletonModel": "singleton",
    "WatcherStanceModel": "stance",
}

CLASS_RE = re.compile(r"public\s+(?:sealed\s+)?class\s+(\w+)\s*(?:<[^>]*>)?\s*:\s*([\w\.<>]+)")
NS_RE = re.compile(r"^namespace\s+([\w\.]+)\s*;", re.MULTILINE)
POOL_RE = re.compile(r"\[Pool\(typeof\((\w+)\)\)\]")


def collect_models():
    found = []
    for root, _dirs, files in os.walk(WATCHER):
        for name in files:
            if not name.endswith(".cs"):
                continue
            path = os.path.join(root, name)
            text = read(path)
            namespace = NS_RE.search(text)
            pool = POOL_RE.search(text)
            for match in CLASS_RE.finditer(text):
                cls, base = match.group(1), match.group(2).split(".")[-1].split("<")[0]
                if base not in FAMILY:
                    continue
                # abstract declarations use "public abstract class"
                line_start = text.rfind("\n", 0, match.start()) + 1
                if "abstract" in text[line_start:match.start() + 40]:
                    continue
                found.append({
                    "class": cls,
                    "family": FAMILY[base],
                    "namespace": namespace.group(1) if namespace else "",
                    "pool": pool.group(1) if pool else None,
                })
    found.sort(key=lambda m: (m["family"], m["class"]))
    return found


def emit_registry(models):
    lines = [
        "// <auto-generated>",
        "// Written by build/gen_compat3.py from the [Pool(...)] attributes in src/Watcher.",
        "// Regenerate after adding content; do not hand-edit.",
        "// </auto-generated>",
        "namespace BaseLib.Utils;",
        "",
        "public static partial class WatcherRegistry",
        "{",
        "\tprivate static void RegisterAll()",
        "\t{",
    ]
    count = 0
    for model in models:
        if not model["pool"]:
            continue
        if model["family"] not in ("card", "relic", "potion"):
            continue
        method = {"card": "AddCard", "relic": "Add", "potion": "Add"}[model["family"]]
        lines.append("\t\t{0}<global::{1}.{2}, global::Watcher.Code.Character.{3}>();".format(
            method, model["namespace"], model["class"], model["pool"]))
        count += 1
    lines += ["\t}", "}", ""]
    write(os.path.join(COMPAT, "BaseLib/Utils/WatcherRegistry.Generated.cs"), "\n".join(lines))
    print("registry entries", count)


def emit_subtypes(models):
    rel = "src/Core/Models/AbstractModelSubtypes.cs"
    path = os.path.join(BUNDLE, rel)
    text = read(path)
    if "global::Watcher.Code" in text:
        print("  subtypes already extended")
        return 0

    text = text.replace("private static readonly Type[] _subtypes = new Type[1624]",
                        "private static readonly Type[] _subtypes = new Type[]", 1)
    text = text.replace("public static int Count => 1624;", "public static int Count => _subtypes.Length;", 1)

    entries = []
    for model in models:
        entries.append("\t\ttypeof(global::{0}.{1})".format(model["namespace"], model["class"]))

    marker = "\t\t_t1623\n\t};"
    if marker not in text:
        print("  MISS subtypes array tail")
        return 0
    text = text.replace(marker, "\t\t_t1623,\n" + ",\n".join(entries) + "\n\t};", 1)
    write(path, text)
    print("subtype entries", len(entries))
    return len(entries)


def main():
    for rel, pairs in NATIVE_EDITS:
        edit(rel, pairs)
    for rel, pairs in SHIM_EDITS:
        edit(rel, pairs)
    patch_builder()
    patch_common_actions()
    patch_scry()
    patch_mod_string_extensions()
    for rel, text in NEW_FILES.items():
        path = os.path.join(BUNDLE, rel)
        if not os.path.exists(path):
            write(path, text)
            print("wrote", rel)
    models = collect_models()
    print("collected models", len(models))
    emit_registry(models)
    emit_subtypes(models)
    return 0


if __name__ == "__main__":
    sys.exit(main())
