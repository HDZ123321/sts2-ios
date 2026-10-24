#!/usr/bin/env python3
"""Mark every flat-namespace (DYNAMIC_LOOKUP) undefined symbol as a weak reference.

fmod's iOS dylib imports 30 symbols through the flat namespace (n_desc high byte
0xFE): _objc_msgSend, _OBJC_CLASS_$_AVAudioSession, _AVAudioSessionCategoryPlayAndRecord,
_AudioComponentFindNext, _AudioQueue*, _AudioUnit*, _AudioFile*, _AudioOutputUnit*,
and _load_all_fmod_plugins. With RTLD_NOW a single unresolved entry aborts the whole
dlopen, which is why the extension never registers on iOS.

Setting N_WEAK_REF (0x40) on those entries lets dlopen succeed; symbols that do
resolve keep working exactly as before.
"""
import struct
import sys

LC_SYMTAB = 0x2
N_WEAK_REF = 0x40
DYNAMIC_LOOKUP = 0xFE


def find_symtab(d):
    ncmds = struct.unpack_from("<I", d, 16)[0]
    off = 32
    for _ in range(ncmds):
        cmd, size = struct.unpack_from("<II", d, off)
        if cmd == LC_SYMTAB:
            return struct.unpack_from("<IIII", d, off + 8)
        off += size
    raise SystemExit("LC_SYMTAB not found")


def patch(src, dst):
    d = bytearray(open(src, "rb").read())
    symoff, nsyms, stroff, strsize = find_symtab(d)
    patched = []
    for i in range(nsyms):
        base = symoff + i * 16
        nx, ntype, nsect, ndesc, nval = struct.unpack_from("<IBBHQ", d, base)
        if nx == 0:
            continue
        if (ntype & 0x0E) != 0x00:          # N_UNDF only
            continue
        ordinal = (ndesc >> 8) & 0xFF
        if ordinal != DYNAMIC_LOOKUP:
            continue
        if ndesc & N_WEAK_REF:
            continue
        end = d.index(b"\x00", stroff + nx)
        name = d[stroff + nx:end].decode("utf-8", "replace")
        struct.pack_into("<H", d, base + 6, ndesc | N_WEAK_REF)
        patched.append(name)
    open(dst, "wb").write(d)
    print("weakened %d flat-namespace symbols" % len(patched))
    for n in patched:
        print("  " + n)
    return len(patched)


if __name__ == "__main__":
    if len(sys.argv) < 3:
        raise SystemExit("usage: weaken_flat_syms.py <in> <out>")
    if patch(sys.argv[1], sys.argv[2]) == 0:
        raise SystemExit("nothing patched")
