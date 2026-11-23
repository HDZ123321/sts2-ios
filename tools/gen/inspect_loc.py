"""Ported generator: inspect_loc.py

Originally lived in the untracked build/ scratch dir and hardcoded
E:/sts2_port/ci_bundle. The path is now derived from this file's location so it runs
from any checkout. It edits tracked sources in place with literal replaces, which
makes re-runs no-ops; read the docstring below before pointing it at a dirty tree.

"""

import os

_REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

import struct, json, sys

p = _REPO + "/build/mods/Watcher/Watcher.pck"
d = open(p, 'rb').read()
diroff = struct.unpack_from('<Q', d, 32)[0]
cnt = struct.unpack_from('<I', d, diroff)[0]
o = diroff + 4
ent = {}
for _ in range(cnt):
    ln = struct.unpack_from('<I', d, o)[0]
    o += 4
    path = d[o:o + ln].split(b'\0')[0].decode('utf-8')
    o += ln
    off, size = struct.unpack_from('<QQ', d, o)
    o += 16
    md5 = d[o:o + 16]
    o += 16
    flags = struct.unpack_from('<I', d, o)[0]
    o += 4
    ent[path] = (off, size, flags)

print('entries', len(ent), 'consumed', o - diroff, 'diroff', diroff, 'filelen', len(d))

locs = sorted(k for k in ent if '/localization/' in k)
print('loc files', len(locs))
for k in locs[:8]:
    off, size, flags = ent[k]
    print(' ', k, 'off', off, 'size', size, 'flags', flags, d[off:off + 40])

k = 'Watcher/localization/eng/cards.json'
if k in ent:
    off, size, flags = ent[k]
    raw = d[off:off + size]
    print('cards eng flags', flags, 'head', raw[:60])
    try:
        j = json.loads(raw.decode('utf-8-sig'))
        print('keys', len(j), list(j)[:8])
    except Exception as e:
        print('json fail', e)
