"""Ported generator: gen_pool_fixes.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

Repairs for the baked Watcher port.

Three defects found from a local autoslay run (seed 4242, WATCHER):

1. Token cards (Miracle, Insight, ...) carry [Pool(typeof(TokenCardPool))], but the
   vanilla TokenCardPool never learns about them, so CardModel.Pool falls through to
   MockCardPool, whose GenerateAllCards() calls NeverEverCallThisOutsideOfTests_ClearOwner()
   and throws InvalidOperationException("You monster!") -- which kills the hand-draw task
   and leaves the combat with no cards.
2. CardPoolModel.EnergyIconPath resolves to atlases/ui_atlas.sprites/card/energy_watcher.tres,
   which the base game does not ship, so the energy orb renders empty.
3. CardFrameMaterialPath resolves to materials/cards/frames/card_frame_watcher_mat.tres,
   which does not exist either (only red/green/blue/pink/orange/colorless/curse/quest do).

Run from the repository root:  python build/gen_pool_fixes.py
"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import io
import os

ROOT = _REPO + ""

KNOWN_FRAMES = ['red', 'green', 'blue', 'pink', 'orange', 'colorless', 'curse', 'quest']


def read(path):
    p = os.path.join(ROOT, path)
    return io.open(p, 'r', encoding='utf-8-sig', newline='').read()


def write(path, text):
    p = os.path.join(ROOT, path)
    io.open(p, 'w', encoding='utf-8', newline='').write(text)


def nl_of(text):
    return '\r\n' if '\r\n' in text else '\n'


def patch(path, old, new, marker=None):
    """Replace one exact block. Skips when `marker` is already present."""
    text = read(path)
    n = nl_of(text)
    o = old.replace('\n', n)
    nw = new.replace('\n', n)
    if marker is not None and marker in text:
        print('  skip (already patched):', path)
        return False
    if o not in text:
        raise SystemExit('MISS in %s:\n%s' % (path, o[:200]))
    write(path, text.replace(o, nw, 1))
    print('  patched:', path)
    return True


print('[1] widen CardPoolModel.EnergyIconPath to virtual')
patch(
    r'src\Core\Models\CardPoolModel.cs',
    '\tpublic string EnergyIconPath => EnergyIconHelper.GetPath(EnergyColorName);',
    '\tpublic virtual string EnergyIconPath => EnergyIconHelper.GetPath(EnergyColorName);',
    marker='public virtual string EnergyIconPath',
)

print('[2] CustomCardPoolModel: fall back for missing energy icon and frame colour')
CUSTOM_POOL = r'src\WatcherCompat\BaseLib\Abstracts\CustomCardPoolModel.cs'
pool_src = r'''using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Models;

using BaseLib.Utils;

namespace BaseLib.Abstracts;

/// <summary>
/// Card pool that fills itself from the registry: every card carrying
/// <c>[Pool(typeof(ThisPool))]</c> ends up here, so mod cards do not have to be
/// listed twice.
/// </summary>
public abstract class CustomCardPoolModel : CardPoolModel
{
	/// <summary>Frame colours the base game actually ships in materials/cards/frames.</summary>
	private static readonly string[] KnownFrameColors = new string[8] { "red", "green", "blue", "pink", "orange", "colorless", "curse", "quest" };

	public virtual float H => 0f;

	public virtual float S => 0f;

	public virtual float V => 0f;

	public virtual string? BigEnergyIconPath => null;

	public virtual string? TextEnergyIconPath => null;

	public override string EnergyColorName => Title.ToLowerInvariant();

	/// <summary>
	/// The base game has one energy icon per vanilla character, so a mod pool's
	/// own colour name resolves to a sprite that does not exist.  Fall back to the
	/// mod-provided icon instead of rendering an empty orb.
	/// </summary>
	public override string EnergyIconPath
	{
		get
		{
			string path = base.EnergyIconPath;
			if (ResourceLoader.Exists(path))
			{
				return path;
			}
			if (!string.IsNullOrEmpty(BigEnergyIconPath) && ResourceLoader.Exists(BigEnergyIconPath))
			{
				return BigEnergyIconPath;
			}
			return path;
		}
	}

	/// <summary>
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
	}

	protected override CardModel[] GenerateAllCards()
	{
		return WatcherRegistry.CardsFor(GetType()).ToArray();
	}
}
'''
write(CUSTOM_POOL, pool_src)

print('[3] WatcherRegistry: register foreign-pool members through ModHelper, expose Populate()')
REG = r'src\WatcherCompat\BaseLib\Utils\WatcherRegistry.cs'
reg = read(REG)
n = nl_of(reg)

old_add = '''	private static void Add<TModel, TPool>() where TModel : AbstractModel
	{
		ModelDb.Inject(typeof(TModel));
		TModel model = ModelDb.GetById<TModel>(ModelDb.GetId<TModel>());
		Type poolType = typeof(TPool);
		if (!_byPool.TryGetValue(poolType, out List<AbstractModel>? items))
		{
			items = new List<AbstractModel>();
			_byPool[poolType] = items;
		}
		items.Add(model);
	}'''.replace('\n', n)

new_add = '''	/// <summary>
	/// Populate the registry eagerly.  Vanilla pools such as TokenCardPool cache the
	/// result of <c>AllCards</c> the first time anything reads it, and anything added
	/// afterwards is rejected with "it is too late", so this has to run before the
	/// first pool lookup of the boot.
	/// </summary>
	public static void Populate()
	{
		EnsurePopulated();
	}

	private static void Add<TModel, TPool>() where TModel : AbstractModel
	{
		ModelDb.Inject(typeof(TModel));
		TModel model = ModelDb.GetById<TModel>(ModelDb.GetId<TModel>());
		Type poolType = typeof(TPool);
		if (!_byPool.TryGetValue(poolType, out List<AbstractModel>? items))
		{
			items = new List<AbstractModel>();
			_byPool[poolType] = items;
		}
		items.Add(model);
		if (!typeof(CustomPoolMarker).IsAssignableFrom(poolType))
		{
			try
			{
				ModHelper.AddModelToPool(poolType, typeof(TModel));
			}
			catch (InvalidOperationException)
			{
				global::MegaCrit.Sts2.Core.Logging.Log.Warn($"[WatcherRegistry] could not add {typeof(TModel).Name} to {poolType.Name}: that pool was already frozen");
			}
		}
	}'''.replace('\n', n)

if 'public static void Populate()' in reg:
    print('  skip (already patched):', REG)
else:
    if old_add not in reg:
        raise SystemExit('MISS: WatcherRegistry.Add')
    reg = reg.replace(old_add, new_add, 1)

if 'CustomPoolMarker' in reg and 'interface CustomPoolMarker' not in reg:
    marker_iface = '''
/// <summary>Marks the pools this port creates itself, whose membership the registry supplies directly.</summary>
public interface CustomPoolMarker
{
}
'''.replace('\n', n)
    # append after the class closing brace: put it at the top of the file's namespace block
    head = 'namespace BaseLib.Utils;' + n
    reg = reg.replace(head, head + marker_iface.lstrip('\r\n') , 1)

if 'using MegaCrit.Sts2.Core.Modding;' not in reg:
    reg = reg.replace('using MegaCrit.Sts2.Core.Models;' + n,
                      'using MegaCrit.Sts2.Core.Models;' + n + 'using MegaCrit.Sts2.Core.Modding;' + n, 1)

write(REG, reg)
print('  wrote:', REG)

print('[4] mark the port\'s own pools so the registry does not double-register them')
for path, cls in (
    (r'src\WatcherCompat\BaseLib\Abstracts\CustomCardPoolModel.cs', 'CustomCardPoolModel'),
    (r'src\WatcherCompat\BaseLib\Abstracts\CustomRelicPoolModel.cs', 'CustomRelicPoolModel'),
    (r'src\WatcherCompat\BaseLib\Abstracts\CustomPotionPoolModel.cs', 'CustomPotionPoolModel'),
):
    text = read(path)
    if 'CustomPoolMarker' in text:
        print('  skip (already marked):', path)
        continue
    n2 = nl_of(text)
    old = 'public abstract class %s : ' % cls
    idx = text.find(old)
    if idx < 0:
        raise SystemExit('MISS class decl in ' + path)
    eol = text.find(n2, idx)
    decl = text[idx:eol]
    write(path, text.replace(decl, decl + ', CustomPoolMarker', 1))
    print('  marked:', path)

print('[5] populate the registry before anything reads a pool')
patch(
    r'src\Core\Helpers\OneTimeInitialization.cs',
    '\t\tModelDb.Init();',
    '\t\tModelDb.Init();\n\t\tBaseLib.Utils.WatcherRegistry.Populate();',
    marker='WatcherRegistry.Populate()',
)

print('done')
