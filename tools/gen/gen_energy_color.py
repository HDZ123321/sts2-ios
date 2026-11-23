"""Ported generator: gen_energy_color.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

Give the ported Watcher its own energy colour without touching vanilla assets.

The mod shipped no ui_atlas sheet, so `card/energy_watcher` does not exist in the base
game's ui_atlas and the energy orb renders empty.  Two separate lookups are involved:

  CardPoolModel.EnergyIconPath  -> atlases/ui_atlas.sprites/card/energy_<colour>.tres
  EnergyIconsFormatter          -> images/packed/sprite_fonts/<colour>_energy_icon.png

The base game already ships images/packed/sprite_fonts/watcher_energy_icon.png, but
neither the atlas sprite nor a `watcher` energy colour exists.  Rather than editing the
1.9 GB sts2.pck, the pool claims the `ironclad` colour (the red energy sprite and red
frame both exist) and keeps the mod's own big icon for the orb art.

Run from the repository root:  python build/gen_energy_color.py
"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import io
import os

ROOT = _REPO + ""


def read(path):
    return io.open(os.path.join(ROOT, path), 'r', encoding='utf-8-sig', newline='').read()


def write(path, text):
    io.open(os.path.join(ROOT, path), 'w', encoding='utf-8', newline='').write(text)


POOL = r'src\WatcherCompat\BaseLib\Abstracts\CustomCardPoolModel.cs'
text = read(POOL)
n = '\r\n' if '\r\n' in text else '\n'

if 'VanillaEnergyColor' in text:
    print('already patched')
    raise SystemExit(0)

old = ('\tpublic override string EnergyColorName => Title.ToLowerInvariant();').replace('\n', n)
new = ('''	/// <summary>
	/// Vanilla builds every energy sprite and frame material from a colour name that the base
	/// game already ships, so a mod pool has to borrow one of those.  `ironclad` is the closest
	/// match for the Watcher's red/purple orb, and card_frame_red_mat.tres exists.
	/// </summary>
	public virtual string VanillaEnergyColor => "ironclad";

	public override string EnergyColorName => VanillaEnergyColor;''').replace('\n', n)

if old not in text:
    raise SystemExit('MISS: EnergyColorName line')

text = text.replace(old, new, 1)

# The frame material now follows the borrowed colour, so the hard-coded list is dead weight.
old_frame = ('''	/// <summary>
	/// Same story for the frame material: card_frame_&lt;colour&gt;_mat.tres only exists
	/// for the vanilla colours, so unknown colours borrow the closest one.
	/// </summary>
	public override string CardFrameMaterialPath
	{
		get
		{
			if (Array.IndexOf(KnownFrameColors, EnergyColorName) >= 0)
			{
				return "card_frame_" + EnergyColorName;
			}
			return "card_frame_pink";
		}
	}''').replace('\n', n)
new_frame = ('''	public override string CardFrameMaterialPath => "card_frame_" + EnergyColorName;''').replace('\n', n)
if old_frame in text:
    text = text.replace(old_frame, new_frame, 1)
    print('simplified CardFrameMaterialPath')
else:
    print('note: CardFrameMaterialPath block not matched, left as-is')

old_list = ('\t/// <summary>Frame colours the base game actually ships in materials/cards/frames.</summary>' + n +
            '\tprivate static readonly string[] KnownFrameColors = new string[8] { "red", "green", "blue", "pink", "orange", "colorless", "curse", "quest" };' + n + n)
if old_list in text:
    text = text.replace(old_list, '', 1)
    print('dropped KnownFrameColors')

write(POOL, text)
print('patched', POOL)