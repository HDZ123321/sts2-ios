"""Ported generator: gen_compat1.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

Emit batch 1 of the BaseLib compatibility shim.

The Watcher port keeps the original mod sources untouched and supplies the
BaseLib surface they were compiled against.  Batch 1 covers the pieces that
have no dependency on the model classes: identity storage, attributes, the
tooltip plumbing, dynamic-var helpers and the scry interfaces.
"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import os

ROOT = _REPO + "/src/WatcherCompat"

FILES = {}

FILES[r"BaseLib\Utils\SpireField.cs"] = '''using System;
using System.Runtime.CompilerServices;

namespace BaseLib.Utils;

/// <summary>
/// Per-owner side table with the same access shape as BaseLib's SpireField.
/// Entries are collected together with their key, so a finished run cannot
/// leak state into the next one.
/// </summary>
public sealed class SpireField<TKey, TValue> where TKey : class where TValue : class
{
\tprivate readonly ConditionalWeakTable<TKey, TValue> _table = new ConditionalWeakTable<TKey, TValue>();

\tpublic SpireField()
\t{
\t}

\tpublic SpireField(Func<TValue> factory)
\t{
\t}

\tpublic TValue? this[TKey key]
\t{
\t\tget
\t\t{
\t\t\treturn _table.TryGetValue(key, out TValue? value) ? value : null;
\t\t}
\t\tset
\t\t{
\t\t\tif (value == null)
\t\t\t{
\t\t\t\t_table.Remove(key);
\t\t\t\treturn;
\t\t\t}
\t\t\t_table.AddOrUpdate(key, value);
\t\t}
\t}

\tpublic bool Has(TKey key)
\t{
\t\treturn _table.TryGetValue(key, out _);
\t}

\tpublic TValue GetOrCreate(TKey key, Func<TValue> factory)
\t{
\t\treturn _table.GetValue(key, delegate
\t\t{
\t\t\treturn factory();
\t\t});
\t}

\tpublic void Clear(TKey key)
\t{
\t\t_table.Remove(key);
\t}
}
'''

FILES[r"BaseLib\Utils\PoolAttribute.cs"] = '''using System;

namespace BaseLib.Utils;

/// <summary>
/// Declares which pool owns a card, relic, potion or power.  The registration
/// pass reads these instead of the reflection scan the original mod relied on.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class PoolAttribute : Attribute
{
\tpublic Type PoolType { get; }

\tpublic PoolAttribute(Type poolType)
\t{
\t\tPoolType = poolType;
\t}
}
'''

FILES[r"BaseLib\Utils\HookType.cs"] = '''namespace BaseLib.Utils;

/// <summary>
/// Which dispatch chain a singleton model subscribes to.
/// </summary>
public enum HookType
{
\tRun = 0,
\tCombat = 1,
\tRunAndCombat = 2
}
'''

FILES[r"BaseLib\Utils\TooltipSource.cs"] = '''using System;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

/// <summary>
/// A hover tip that depends on the card instance looking at it, so the text can
/// quote live dynamic vars instead of the canonical values.
/// </summary>
public sealed class TooltipSource
{
\tprivate readonly Func<CardModel, IHoverTip> _factory;

\tpublic TooltipSource(Func<CardModel, IHoverTip> factory)
\t{
\t\t_factory = factory;
\t}

\tpublic IHoverTip Resolve(CardModel card)
\t{
\t\treturn _factory(card);
\t}
}
'''

FILES[r"BaseLib\Utils\DynamicVarSource.cs"] = '''using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

/// <summary>
/// Points at the dynamic var set that supplies the amount for a builder call.
/// Cards are the only source today, but powers and relics read the same way.
/// </summary>
public readonly struct DynamicVarSource
{
\tprivate readonly DynamicVarSet? _vars;

\tprivate DynamicVarSource(DynamicVarSet vars)
\t{
\t\t_vars = vars;
\t}

\tpublic DynamicVarSet? Vars => _vars;

\tpublic static implicit operator DynamicVarSource(CardModel card)
\t{
\t\treturn new DynamicVarSource(card.DynamicVars);
\t}

\tpublic static implicit operator DynamicVarSource(PowerModel power)
\t{
\t\treturn new DynamicVarSource(power.DynamicVars);
\t}

\tpublic static implicit operator DynamicVarSource(RelicModel relic)
\t{
\t\treturn new DynamicVarSource(relic.DynamicVars);
\t}
}
'''

FILES[r"BaseLib\Patches\UI\RelicIconData.cs"] = '''namespace BaseLib.Patches.UI;

/// <summary>
/// The three sprite paths a custom relic touches: the big shop/altar icon and
/// the two atlases that the relic bar draws from.
/// </summary>
public sealed class RelicIconData
{
\tpublic string BigIconPath { get; }

\tpublic string PackedIconPath { get; }

\tpublic string PackedIconOutlinePath { get; }

\tpublic RelicIconData(string bigIconPath, string packedIconPath, string packedIconOutlinePath)
\t{
\t\tBigIconPath = bigIconPath;
\t\tPackedIconPath = packedIconPath;
\t\tPackedIconOutlinePath = packedIconOutlinePath;
\t}
}
'''

FILES[r"BaseLib\Cards\Variables\ScryVar.cs"] = '''using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace BaseLib.Cards.Variables;

/// <summary>
/// The "look at the top of the draw pile" amount.  The base game has no scry
/// system, so this var plus <see cref="T:BaseLib.Commands.ScryCmd"/> implement it.
/// </summary>
public sealed class ScryVar : DynamicVar
{
\tpublic const string defaultName = "Scry";

\tpublic ScryVar(decimal scry)
\t\t: base(defaultName, scry)
\t{
\t}

\tpublic ScryVar(string name, decimal scry)
\t\t: base(name, scry)
\t{
\t}
}
'''

FILES[r"BaseLib\Extensions\DynamicVarExtensions.cs"] = '''using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace BaseLib.Extensions;

/// <summary>
/// Remembers the upgrade delta for a dynamic var.  The base game only offers
/// <see cref="M:MegaCrit.Sts2.Core.Localization.DynamicVars.DynamicVar.UpgradeValueBy(System.Decimal)"/>,
/// which has to be called from the card's own upgrade hook; the mod instead
/// declares the delta up front, so it is parked here until then.
/// </summary>
public static class DynamicVarExtensions
{
\tprivate static readonly ConditionalWeakTable<DynamicVar, StrongBox<decimal>> Upgrades = new ConditionalWeakTable<DynamicVar, StrongBox<decimal>>();
\tprivate static readonly ConditionalWeakTable<DynamicVar, StrongBox<int>> CostUpgrades = new ConditionalWeakTable<DynamicVar, StrongBox<int>>();

\tpublic static T WithUpgrade<T>(this T variable, decimal upgradeAmount) where T : DynamicVar
\t{
\t\tUpgrades.AddOrUpdate(variable, new StrongBox<decimal>(upgradeAmount));
\t\treturn variable;
\t}

\tpublic static decimal GetUpgradeAmount(this DynamicVar variable)
\t{
\t\treturn Upgrades.TryGetValue(variable, out StrongBox<decimal>? box) ? box.Value : 0m;
\t}

\tpublic static bool HasUpgradeAmount(this DynamicVar variable)
\t{
\t\treturn Upgrades.TryGetValue(variable, out _);
\t}

\tpublic static void RegisterCostUpgrade(DynamicVar variable, int delta)
\t{
\t\tCostUpgrades.AddOrUpdate(variable, new StrongBox<int>(delta));
\t}

\tpublic static int GetCostUpgrade(DynamicVar variable)
\t{
\t\treturn CostUpgrades.TryGetValue(variable, out StrongBox<int>? box) ? box.Value : 0;
\t}
}
'''

FILES[r"BaseLib\Extensions\DynamicVarSetExtensions.cs"] = '''using System.Collections.Generic;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Extensions;

public static class DynamicVarSetExtensions
{
\t/// <summary>
\t/// Finds the var that feeds a power's tooltip.  Cards built through
\t/// <c>WithPower&lt;T&gt;</c> store a plain var named after the power's type,
\t/// while hand-written cards use <see cref="T:MegaCrit.Sts2.Core.Localization.DynamicVars.PowerVar`1"/>.
\t/// </summary>
\tpublic static DynamicVar Power<T>(this DynamicVarSet set) where T : PowerModel
\t{
\t\tstring name = typeof(T).Name;
\t\tforeach (KeyValuePair<string, DynamicVar> pair in set)
\t\t{
\t\t\tif (pair.Value is PowerVar<T>)
\t\t\t{
\t\t\t\treturn pair.Value;
\t\t\t}
\t\t}
\t\tforeach (KeyValuePair<string, DynamicVar> pair2 in set)
\t\t{
\t\t\tif (pair2.Value.Name == name)
\t\t\t{
\t\t\t\treturn pair2.Value;
\t\t\t}
\t\t}
\t\tthrow new KeyNotFoundException($"No dynamic var for power {name} on this model.");
\t}

\tpublic static bool TryGetPower<T>(this DynamicVarSet set, out DynamicVar? variable) where T : PowerModel
\t{
\t\ttry
\t\t{
\t\t\tvariable = set.Power<T>();
\t\t\treturn true;
\t\t}
\t\tcatch (KeyNotFoundException)
\t\t{
\t\t\tvariable = null;
\t\t\treturn false;
\t\t}
\t}
}
'''

FILES[r"BaseLib\Abstracts\ScryInterfaces.cs"] = '''using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Hooks;

/// <summary>Fired on every model in combat after a scry resolves.</summary>
public interface IAfterScryed
{
\tTask AfterScryed(PlayerChoiceContext ctx, Player player, int scryAmount, int discardAmount, List<CardModel> seen, List<CardModel> discarded);
}

/// <summary>Lets a relic or power widen a scry before the cards are revealed.</summary>
public interface IModifyScryAmount
{
\tint ModifyScryAmount(Player player, int amount);
}

/// <summary>Optional follow-up for <see cref="T:BaseLib.Hooks.IModifyScryAmount"/> holders.</summary>
public interface IAfterModifyingScryAmount
{
\tTask AfterModifyingScryAmount(PlayerChoiceContext ctx, Player player, int originalAmount, int modifiedAmount);
}
'''

FILES[r"BaseLib\Abstracts\CardInterfaces.cs"] = '''using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

/// <summary>Marks a basic card that a higher rarity card transforms into.</summary>
public interface ITranscendenceCard
{
\tCardModel GetTranscendenceTransformedCard();
}
'''

FILES[r"BaseLib\Patches\Localization\IAddDumbVariablesToPowerDescription.cs"] = '''using MegaCrit.Sts2.Core.Localization;

namespace BaseLib.Patches.Localization;

/// <summary>
/// Lets a power inject extra localization variables into its description without
/// going through the smart-description path.
/// </summary>
public interface IAddDumbVariablesToPowerDescription
{
\tvoid AddDumbVariablesToDescription(LocString description);
}
'''

FILES[r"BaseLib\Utils\BaseLibTip.cs"] = '''using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace BaseLib.Utils;

/// <summary>
/// Hover tips BaseLib added on top of the base game's
/// <see cref="T:MegaCrit.Sts2.Core.HoverTips.StaticHoverTip"/> enum.
/// </summary>
public static class BaseLibTip
{
\tpublic static IHoverTip Scry { get; } = Build("scry");

\tprivate static IHoverTip Build(string key)
\t{
\t\tLocString title = new LocString("powers", "BASE_TIP_" + key.ToUpperInvariant() + ".title");
\t\tLocString description = new LocString("powers", "BASE_TIP_" + key.ToUpperInvariant() + ".description");
\t\treturn new HoverTip(title, description.GetFormattedText(), null);
\t}
}
'''


def main():
    written = 0
    for relative, text in FILES.items():
        path = os.path.join(ROOT, relative)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)
        written += 1
        print("wrote", relative, len(text))
    print("total", written)


if __name__ == "__main__":
    main()
