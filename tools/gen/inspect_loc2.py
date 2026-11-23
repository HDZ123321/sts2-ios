"""Ported generator: inspect_loc2.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import struct, json

p = _REPO + "/build/mods/Watcher/Watcher.pck"
d = open(p, 'rb').read()
diroff = struct.unpack_from('<Q', d, 32)[0]


def parse(align):
    cnt = struct.unpack_from('<I', d, diroff)[0]
    o = diroff + 4
    ent = {}
    for _ in range(cnt):
        ln = struct.unpack_from('<I', d, o)[0]
        o += 4
        path = d[o:o + ln].split(b'\0')[0].decode('utf-8', 'replace')
        o += ln
        if align:
            o = diroff + ((o - diroff + 3) & ~3)
        off, size = struct.unpack_from('<QQ', d, o)
        o += 36
        ent[path] = (off, size)
    return ent, o


for align in (0, 1):
    ent, endo = parse(align)
    print('align', align, 'entries', len(ent), 'end', endo, 'filelen', len(d))
    k = 'Watcher/localization/eng/cards.json'
    if k in ent:
        off, size = ent[k]
        raw = d[off:off + size]
        print('  head', raw[:50])
        try:
            j = json.loads(raw.decode('utf-8-sig'))
            print('  keys', len(j), list(j)[:6])
        except Exception as e:
            print('  json fail', e)
