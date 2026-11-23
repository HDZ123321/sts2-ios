"""Ported generator: gen_frame_fix.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import io

PATH = _REPO + "/src/WatcherCompat/BaseLib/Abstracts/CustomCardPoolModel.cs"

with io.open(PATH, "r", encoding="utf-8-sig", newline="") as f:
    text = f.read()

newline = "\r\n" if "\r\n" in text else "\n"

OLD = """	public override string CardFrameMaterialPath => "card_frame_" + EnergyColorName;""".replace("\n", newline)

NEW = """	/// <summary>
	/// The energy colour and the frame colour are separate names in the base game
	/// (ironclad ships <c>card_frame_red_mat.tres</c>, not <c>card_frame_ironclad_mat.tres</c>),
	/// so a mod pool cannot derive one from the other.
	/// </summary>
	public virtual string VanillaCardFrame => "card_frame_red";

	public override string CardFrameMaterialPath => VanillaCardFrame;""".replace("\n", newline)

if OLD in text:
    text = text.replace(OLD, NEW, 1)
    changed = 1
elif "VanillaCardFrame" in text:
    changed = 0
else:
    raise SystemExit("CardFrameMaterialPath line not found")

with io.open(PATH, "w", encoding="utf-8-sig", newline="") as f:
    f.write(text)

print("patched %d site(s)" % changed)