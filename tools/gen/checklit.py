"""Ported generator: checklit.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import sys

def has(path, s):
    d = open(path, 'rb').read()
    enc = s.encode('utf-16-le')
    return enc in d, d.count(enc)

paths = [
    _REPO + "/.godot/mono/temp/bin/Release/sts2.dll",
    r'E:\SteamLibrary\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64\sts2.dll',
]
for p in paths:
    print(p)
    print('  frame watch started ->', has(p, 'frame watch started'))
    print('  [Perf]              ->', has(p, '[Perf] '))
    print('  Selecting character ->', has(p, 'Selecting character'))
