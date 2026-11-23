"""Ported generator: gen_autoslay_gate.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import io, sys

P = _REPO + "/src/Core/Nodes/NGame.cs"
t = io.open(P, 'r', encoding='utf-8-sig', newline='').read()
nl = '\r\n' if '\r\n' in t else '\n'
orig = t

# 1) log the command line at the very start of GameStartup
old = '\tprivate async Task GameStartup()' + nl + '\t{' + nl + '\t\tawait OneTimeInitialization.ExecuteVeryEarly();'
new = ('\tprivate async Task GameStartup()' + nl + '\t{' + nl +
       '\t\tLog.Info($"[Startup] cmdline=\'{string.Join(" ", OS.GetCmdlineArgs())}\' perf={Perf.Enabled} release={IsReleaseGame()}");' + nl +
       '\t\tawait OneTimeInitialization.ExecuteVeryEarly();')
if 'cmdline=' not in t:
    assert old in t, 'GameStartup head not found'
    t = t.replace(old, new, 1)

# 2) let the dev-only autoslay harness actually run in a release build when asked explicitly
old2 = ('\tpublic static bool IsReleaseGame()' + nl + '\t{' + nl + '\t\treturn true;' + nl + '\t}')
new2 = ('\tpublic static bool IsReleaseGame()' + nl + '\t{' + nl +
        '\t\tif (CommandLineHelper.HasArg("autoslay"))' + nl + '\t\t{' + nl +
        '\t\t\treturn false;' + nl + '\t\t}' + nl +
        '\t\treturn true;' + nl + '\t}')
if 'CommandLineHelper.HasArg("autoslay")' + nl + '\t\t\treturn false;' not in t:
    assert old2 in t, 'IsReleaseGame body not found'
    t = t.replace(old2, new2, 1)

if t != orig:
    io.open(P, 'w', encoding='utf-8', newline='').write(t)
    print('patched NGame.cs')
else:
    print('no change (already patched)')
