"""Ported generator: gen_epoch_fix3.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import io

PATH = _REPO + "/src/Core/Saves/Managers/ProgressSaveManager.cs"

with io.open(PATH, "r", encoding="utf-8-sig", newline="") as f:
    text = f.read()

newline = "\r\n" if "\r\n" in text else "\n"

OLD = """			string text = localPlayer.Character.Id.Entry.ToUpperInvariant();
			EpochModel epochModel = null;
			switch (act)
			{
			case 0:
				epochModel = EpochModel.Get(text + "2_EPOCH");
				break;
			case 1:
				epochModel = EpochModel.Get(text + "3_EPOCH");
				break;
			case 2:
				epochModel = EpochModel.Get(text + "4_EPOCH");
				break;""".replace("\n", newline)

NEW = """			string text = localPlayer.Character.Id.Entry.ToUpperInvariant();
			EpochModel epochModel = null;
			switch (act)
			{
			case 0:
				epochModel = TryGetCharUnlockEpoch(text + "2_EPOCH");
				break;
			case 1:
				epochModel = TryGetCharUnlockEpoch(text + "3_EPOCH");
				break;
			case 2:
				epochModel = TryGetCharUnlockEpoch(text + "4_EPOCH");
				break;""".replace("\n", newline)

if OLD in text:
    text = text.replace(OLD, NEW, 1)
    changed = 1
elif "TryGetCharUnlockEpoch" in text:
    changed = 0
else:
    raise SystemExit("ObtainCharUnlockEpoch switch not found")

HELPER_OLD = """	private void ObtainCharUnlockEpoch(Player localPlayer, int act)""".replace("\n", newline)

HELPER_NEW = """	/// <summary>
	/// A ported or modded character does not ship the per-act unlock epochs the base game
	/// characters have, and <see cref="T:MegaCrit.Sts2.Core.Timeline.EpochModel.Get" /> throws
	/// when the id is unknown.  Missing epochs simply mean "nothing to unlock".
	/// </summary>
	private static EpochModel? TryGetCharUnlockEpoch(string id)
	{
		if (!EpochModel.IsValid(id))
		{
			Log.Info($"No unlock epoch '{id}' for this character, skipping.");
			return null;
		}
		return EpochModel.Get(id);
	}

	private void ObtainCharUnlockEpoch(Player localPlayer, int act)""".replace("\n", newline)

if HELPER_OLD in text and "private static EpochModel? TryGetCharUnlockEpoch" not in text:
    text = text.replace(HELPER_OLD, HELPER_NEW, 1)
    changed += 1
elif "private static EpochModel? TryGetCharUnlockEpoch" in text:
    pass
else:
    raise SystemExit("ObtainCharUnlockEpoch declaration not found")

with io.open(PATH, "w", encoding="utf-8-sig", newline="") as f:
    f.write(text)

print("patched %d site(s)" % changed)