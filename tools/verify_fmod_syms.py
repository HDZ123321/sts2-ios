#!/usr/bin/env python3
"""Fail the build if the shipped fmod framework has strong flat-namespace imports.

fmod's iOS dylib imports 30 symbols through the flat namespace, including
_load_all_fmod_plugins which nothing in the app or the engine defines. With a
strong reference dlopen(RTLD_NOW) aborts, the GDExtension never registers, and
every FmodServer call becomes "Identifier not declared". N_WEAK_REF (0x40) on
those entries makes dlopen succeed.
"""
import struct
import sys

LC_SYMTAB = 0x2
N_WEAK_REF = 0x40
DYNAMIC_LOOKUP = 0xFE


def check(path):
    d = open(path, "rb").read()
    ncmds = struct.unpack_from("<I", d, 16)[0]
    off = 32
    sym = None
    for _ in range(ncmds):
        cmd, size = struct.unpack_from("<II", d, off)
        if cmd == LC_SYMTAB:
            sym = struct.unpack_from("<IIII", d, off + 8)
        off += size
    if sym is None:
        raise SystemExit("LC_SYMTAB not found in " + path)
    symoff, nsyms, stroff, _ = sym
    strong = []
    weak = 0
    for i in range(nsyms):
        base = symoff + i * 16
        nx, ntype, nsect, ndesc, nval = struct.unpack_from("<IBBHQ", d, base)
        if nx == 0 or (ntype & 0x0E) != 0x00:
            continue
        if ((ndesc >> 8) & 0xFF) != DYNAMIC_LOOKUP:
            continue
        if ndesc & N_WEAK_REF:
            weak += 1
        else:
            end = d.index(b"\x00", stroff + nx)
            strong.append(d[stroff + nx:end].decode("utf-8", "replace"))
    return strong, weak


if __name__ == "__main__":
    if len(sys.argv) < 2:
        raise SystemExit("usage: verify_fmod_syms.py <framework-binary> [...]")
    bad = 0
    for p in sys.argv[1:]:
        strong, weak = check(p)
        print("%s: flat weak=%d strong=%d" % (p.split("/")[-1], weak, len(strong)))
        for s in strong:
            print("  STRONG " + s)
        if strong:
            bad = 1
    if bad:
        print("::error::fmod framework still has strong flat-namespace imports")
        sys.exit(1)
    print("OK: every flat-namespace import is a weak reference")
