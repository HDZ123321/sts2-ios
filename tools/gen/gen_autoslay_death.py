"""Ported generator: gen_autoslay_death.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import io
import re

PATH = _REPO + "/src/Core/AutoSlay/AutoSlayer.cs"

with io.open(PATH, "r", encoding="utf-8-sig", newline="") as f:
    text = f.read()

newline = "\r\n" if "\r\n" in text else "\n"

OLD_WAIT = """	private async Task WaitForRewardsScreenAsync(CancellationToken ct)
	{
		AutoSlayLog.Action("Waiting for rewards screen");
		await WaitHelper.Until(() => NOverlayStack.Instance?.Peek() is NRewardsScreen || (NMapScreen.Instance?.IsOpen ?? false), ct, TimeSpan.FromSeconds(10L), "Rewards screen did not appear after combat");
	}""".replace("\n", newline)

NEW_WAIT = """	private async Task WaitForRewardsScreenAsync(CancellationToken ct)
	{
		AutoSlayLog.Action("Waiting for rewards screen");
		await WaitHelper.Until(() => NOverlayStack.Instance?.Peek() is NRewardsScreen || (NMapScreen.Instance?.IsOpen ?? false) || NOverlayStack.Instance?.Peek() is NGameOverScreen || RunManager.Instance.DebugOnlyGetState() == null, ct, TimeSpan.FromSeconds(10L), "Rewards screen did not appear after combat");
	}""".replace("\n", newline)

OLD_GUARD = """			else
			{
				await WaitForRewardsScreenAsync(ct);
			}
			await DrainOverlayScreensAsync(ct);""".replace("\n", newline)

NEW_GUARD = """			else
			{
				await WaitForRewardsScreenAsync(ct);
			}
			if (RunManager.Instance.DebugOnlyGetState() == null || NOverlayStack.Instance?.Peek() is NGameOverScreen)
			{
				AutoSlayLog.Action("The player died; draining the game over screen");
				await DrainOverlayScreensAsync(ct);
				await WaitForMainMenuAsync(ct);
				AutoSlayLog.Action("Run ended after the player died, back at the main menu");
				return;
			}
			await DrainOverlayScreensAsync(ct);""".replace("\n", newline)

changed = 0
if OLD_WAIT in text:
    text = text.replace(OLD_WAIT, NEW_WAIT, 1)
    changed += 1
elif "NGameOverScreen || RunManager.Instance.DebugOnlyGetState() == null" not in text:
    raise SystemExit("WaitForRewardsScreenAsync pattern not found")

if OLD_GUARD in text:
    text = text.replace(OLD_GUARD, NEW_GUARD, 1)
    changed += 1
elif "The player died; draining the game over screen" not in text:
    raise SystemExit("room loop guard pattern not found")

with io.open(PATH, "w", encoding="utf-8-sig", newline="") as f:
    f.write(text)

print("patched %d site(s)" % changed)