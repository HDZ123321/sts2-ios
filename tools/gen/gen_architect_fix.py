"""Ported generator: gen_architect_fix.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import io

PATH = _REPO + "/src/Core/Models/Events/TheArchitect.cs"

with io.open(PATH, "r", encoding="utf-8-sig", newline="") as f:
    text = f.read()

newline = "\r\n" if "\r\n" in text else "\n"

OLD = """	private async Task WinRun()
	{
		if (LocalContext.IsMe(base.Owner))
		{
			await AnimPlayerAttackIfNecessary(Dialogue.EndAttackers);
			await AnimArchitectAttackIfNecessary(Dialogue.EndAttackers);""".replace("\n", newline)

NEW = """	private async Task WinRun()
	{
		if (LocalContext.IsMe(base.Owner))
		{
			// A ported or modded character has no architect dialogue, so Dialogue is null
			// here and there is no closing attack to play.
			ArchitectAttackers endAttackers = Dialogue?.EndAttackers ?? ArchitectAttackers.None;
			await AnimPlayerAttackIfNecessary(endAttackers);
			await AnimArchitectAttackIfNecessary(endAttackers);""".replace("\n", newline)

if OLD in text:
    text = text.replace(OLD, NEW, 1)
    changed = 1
elif "ArchitectAttackers endAttackers" in text:
    changed = 0
else:
    raise SystemExit("WinRun body not found")

with io.open(PATH, "w", encoding="utf-8-sig", newline="") as f:
    f.write(text)

print("patched %d site(s)" % changed)