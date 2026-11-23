"""Ported generator: gen_cardblock_fix.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import io

PATH = _REPO + "/src/WatcherCompat/BaseLib/Utils/CommonActions.cs"

with io.open(PATH, "r", encoding="utf-8-sig", newline="") as f:
    text = f.read()

newline = "\r\n" if "\r\n" in text else "\n"

OLD = """	public static Task<decimal> CardBlock(CardModel card, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(card);
		return CreatureCmd.GainBlock(card.Owner.Creature, card.DynamicVars.Block, cardPlay);
	}""".replace("\n", newline)

NEW = """	/// <summary>
	/// Cards built with <c>WithCalculatedBlock</c> carry a <c>CalculatedBlock</c> var instead of
	/// the plain <c>Block</c> var, so asking for <c>DynamicVars.Block</c> would throw.  Prefer
	/// whichever var the card actually declares.
	/// </summary>
	public static Task<decimal> CardBlock(CardModel card, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(card);
		DynamicVarSet vars = card.DynamicVars;
		if (vars.TryGetValue("CalculatedBlock", out DynamicVar calculated) && calculated is CalculatedBlockVar calculatedBlock)
		{
			return CreatureCmd.GainBlock(card.Owner.Creature, calculatedBlock.Calculate(cardPlay.Target), calculatedBlock.Props, cardPlay);
		}
		if (vars.TryGetValue("Block", out DynamicVar block) && block is BlockVar blockVar)
		{
			return CreatureCmd.GainBlock(card.Owner.Creature, blockVar, cardPlay);
		}
		Log.Warn($"{card.Id} declares neither a Block nor a CalculatedBlock var, gaining nothing.");
		return Task.FromResult(0m);
	}""".replace("\n", newline)

if OLD in text:
    text = text.replace(OLD, NEW, 1)
    changed = 1
elif "declares neither a Block nor a CalculatedBlock var" in text:
    changed = 0
else:
    raise SystemExit("CardBlock body not found")

if "using MegaCrit.Sts2.Core.Localization.DynamicVars;" not in text:
    anchor = "using MegaCrit.Sts2.Core.Models;" + newline
    if anchor in text:
        text = text.replace(anchor, anchor + "using MegaCrit.Sts2.Core.Localization.DynamicVars;" + newline, 1)
        changed += 1
    else:
        raise SystemExit("using anchor not found")

if "using MegaCrit.Sts2.Core.Logging;" not in text:
    anchor2 = "using MegaCrit.Sts2.Core.Models;" + newline
    text = text.replace(anchor2, anchor2 + "using MegaCrit.Sts2.Core.Logging;" + newline, 1)
    changed += 1

with io.open(PATH, "w", encoding="utf-8-sig", newline="") as f:
    f.write(text)

print("patched %d site(s)" % changed)