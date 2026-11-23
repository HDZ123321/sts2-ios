"""Ported generator: gen_cardattack_fix.py

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

OLD = """		AttackCommand command;
		if (card.DynamicVars.ContainsKey("CalculatedDamage"))
		{
			command = DamageCmd.Attack(card.DynamicVars.CalculatedDamage);
		}
		else
		{
			command = DamageCmd.Attack(card.DynamicVars.Damage.BaseValue);
		}""".replace("\n", newline)

NEW = """		AttackCommand command;
		if (card.DynamicVars.TryGetValue("CalculatedDamage", out DynamicVar calculated) && calculated is CalculatedDamageVar calculatedDamage)
		{
			command = DamageCmd.Attack(calculatedDamage);
		}
		else if (card.DynamicVars.TryGetValue("Damage", out DynamicVar flat) && flat is DamageVar damageVar)
		{
			command = DamageCmd.Attack(damageVar.BaseValue);
		}
		else
		{
			Log.Warn($"{card.Id} declares neither a Damage nor a CalculatedDamage var, attacking for nothing.");
			command = DamageCmd.Attack(0m);
		}""".replace("\n", newline)

if OLD in text:
    text = text.replace(OLD, NEW, 1)
    changed = 1
elif "attacking for nothing" in text:
    changed = 0
else:
    raise SystemExit("CardAttack var block not found")

with io.open(PATH, "w", encoding="utf-8-sig", newline="") as f:
    f.write(text)

print("patched %d site(s)" % changed)