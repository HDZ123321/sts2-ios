"""Ported generator: gen_hook_contract.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

Fix the GDScript hook bridge's value-passing contract.

Vanilla hooks come in two flavours:

  chained   -- the wrapper does `num = listener.Hook(..., num)`, so a listener returns
               the FULL new value and receives the value produced by the previous one.
               ModifyMaxEnergy, ModifyEnergyGain, ModifyHandDraw, ModifyXValue,
               ModifyOrbValue, ModifySummonAmount, ModifyGoldGained, ModifyMerchantPrice,
               ModifyRestSiteHealAmount all work this way.
  delta     -- the wrapper does `num += listener.Hook(..., num)`, so a listener returns
               an adjustment.  Only ModifyDamageAdditive and
               ModifyPowerAmountGivenAdditive do this.
  factor    -- the wrapper does `num *= listener.Hook(..., num)`.
               ModifyDamageMultiplicative and ModifyPowerAmountGivenMultiplicative.

The bridge used DispatchAdditive (a delta accumulator seeded with 0) for every one of
them.  With no mod loaded that returns 0, which vanilla then uses as the new value:
max energy became 0 and the hand draw count became 0 -- an empty energy orb and no cards.
Every chained hook has to go through a chain dispatcher seeded with the incoming value.

Run from the repository root:  python build/gen_hook_contract.py
"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import io
import os

ROOT = _REPO + ""
RUNTIME = r'src\Core\Modding\Gd\GdModRuntime.cs'
BRIDGE = r'src\Core\Modding\Gd\GdHookBridge.cs'


def read(path):
    return io.open(os.path.join(ROOT, path), 'r', encoding='utf-8-sig', newline='').read()


def write(path, text):
    io.open(os.path.join(ROOT, path), 'w', encoding='utf-8', newline='').write(text)


def nl_of(text):
    return '\r\n' if '\r\n' in text else '\n'


# ---------------------------------------------------------------- 1. the dispatcher
runtime = read(RUNTIME)
n = nl_of(runtime)

CHAIN_DOC = '''	/// <summary>
	/// Feeds <paramref name="current"/> through every loaded mod in turn and returns the last
	/// value produced.  This is the shape vanilla uses for hooks whose wrapper does
	/// <c>num = listener.Hook(..., num)</c>: a mod that abstains (returns nil) leaves the value
	/// alone, and a mod that answers replaces it.
	///
	/// Do not confuse this with <see cref="DispatchAdditive"/>: seeding a chained hook with 0
	/// would hand vanilla a value of 0 when no mod is installed (that is how max energy became
	/// 0 and the hand stopped being drawn).
	/// </summary>
	public static decimal DispatchChain(string method, decimal current, params Variant[] args)
	{
		foreach (Node mod in _mods)
		{
			Variant result = CallMod(mod, method, args);
			if (result.VariantType == Variant.Type.Nil)
			{
				continue;
			}
			if (!TryToDecimal(mod, method, result, out decimal value))
			{
				continue;
			}
			current = value;
		}
		return current;
	}

'''.replace('\n', n)

anchor = '\tpublic static decimal DispatchAdditive('.replace('\n', n)
if 'public static decimal DispatchChain(' in runtime:
    print('[1] DispatchChain already present')
else:
    if anchor not in runtime:
        raise SystemExit('MISS: DispatchAdditive declaration')
    runtime = runtime.replace(anchor, CHAIN_DOC + anchor, 1)
    write(RUNTIME, runtime)
    print('[1] added GdModRuntime.DispatchChain')

# ---------------------------------------------------------------- 2. the bridge
bridge = read(BRIDGE)
n = nl_of(bridge)

# method -> (dispatch method to use, seed expression)
CHAINED = [
    ('modify_hand_draw', 'count'),
    ('modify_x_value', 'originalValue'),
    ('modify_max_energy', 'amount'),
    ('modify_energy_gain', 'amount'),
    ('modify_orb_value', 'amount'),
    ('modify_summon_amount', 'amount'),
    ('modify_gold_gained', 'amount'),
    ('modify_merchant_price', 'cost'),
    ('modify_rest_site_heal_amount', 'amount'),
]

changed = 0
for method, seed in CHAINED:
    old = 'GdModRuntime.DispatchAdditive("%s", 0m,' % method
    if method == 'modify_x_value':
        old = 'GdModRuntime.DispatchAdditive("modify_x_value", originalValue,'
    if method == 'modify_hand_draw':
        old = 'GdModRuntime.DispatchAdditive("modify_hand_draw", 0m,'
    new = 'GdModRuntime.DispatchChain("%s", %s,' % (method, seed)
    old = old.replace('\n', n)
    new = new.replace('\n', n)
    if new in bridge:
        continue
    if old not in bridge:
        raise SystemExit('MISS in bridge: ' + old)
    bridge = bridge.replace(old, new, 1)
    changed += 1

write(BRIDGE, bridge)
print('[2] switched %d chained hooks to DispatchChain' % changed)

# ---------------------------------------------------------------- 3. sanity report
bridge = read(BRIDGE)
print('[3] remaining dispatch call sites:')
for line in bridge.split('\n'):
    if 'DispatchChain' in line or 'DispatchAdditive' in line or 'DispatchMultiplicative' in line:
        print('   ', line.strip())
