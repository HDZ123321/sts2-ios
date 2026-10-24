"""Dump the library-ordinal distribution of a Mach-O's undefined symbols.

The high byte of n_desc holds the ordinal of the LC_LOAD_DYLIB the symbol comes
from.  That matters because deleting a load command silently renumbers every
later library, which would repoint every symbol that referenced ordinal >= N.
Before touching the command list we need to know whether the entry we want to
remove is actually referenced at all.
"""
import struct
import sys
from collections import Counter

LC_SEGMENT_64 = 0x19
LC_SYMTAB = 0x2
LC_DYSYMTAB = 0xB
LC_LOAD_DYLIB = 0xC
LC_LOAD_WEAK_DYLIB = 0x80000018


def load_commands(buf):
    ncmds, sizeofcmds = struct.unpack_from("<II", buf, 16)
    off = 32
    out = []
    for _ in range(ncmds):
        cmd, size = struct.unpack_from("<II", buf, off)
        out.append((off, cmd, size))
        off += size
    return out


def dylibs(buf, cmds):
    names = []
    for off, cmd, size in cmds:
        if cmd in (LC_LOAD_DYLIB, LC_LOAD_WEAK_DYLIB):
            no = struct.unpack_from("<I", buf, off + 8)[0]
            names.append(bytes(buf[off + no:off + size]).split(b"\x00")[0].decode())
    return names


def symtab(buf, cmds):
    for off, cmd, size in cmds:
        if cmd == LC_SYMTAB:
            symoff, nsyms, stroff, strsize = struct.unpack_from("<IIII", buf, off + 8)
            return symoff, nsyms, stroff, strsize
    raise SystemExit("no LC_SYMTAB")


def analyze(path):
    buf = open(path, "rb").read()
    cmds = load_commands(buf)
    libs = dylibs(buf, cmds)
    symoff, nsyms, stroff, strsize = symtab(buf, cmds)
    strtab = buf[stroff:stroff + strsize]

    print(f"file   = {path}  ({len(buf):,} bytes)")
    print(f"ncmds  = {len(cmds)}   LC_LOAD_DYLIB/WEAK = {len(libs)}")
    for i, n in enumerate(libs, 1):
        print(f"  ordinal {i:2d}  {n}")

    per_ord = Counter()
    undefined_by_ord = {}
    for k in range(nsyms):
        e = symoff + k * 16
        strx, ntype, nsect, ndesc, nvalue = struct.unpack_from("<IBBHQ", buf, e)
        if (ntype & 0x0E) != 0:
            continue  # defined (N_SECT / N_ABS / N_UNDF with value 0 handled below)
        if ntype & 0x01 and nvalue != 0:
            continue  # common
        name = strtab[strx:].split(b"\x00")[0].decode("utf-8", "replace")
        ordn = (ndesc >> 8) & 0xFF
        special = ndesc & 0xFF
        tag = "DYNAMIC_LOOKUP" if ordn == 0xFE else (
            "SELF" if ordn == 0 else f"lib#{ordn}")
        per_ord[tag] += 1
        undefined_by_ord.setdefault(tag, []).append((name, special))

    print("\nundefined symbols by library ordinal:")
    for tag, n in sorted(per_ord.items(), key=lambda x: -x[1]):
        print(f"  {tag:16s} {n:5d}")
        sample = undefined_by_ord[tag][:6]
        for name, sp in sample:
            print(f"        {name}   (n_desc flags 0x{sp:02x})")
        if len(undefined_by_ord[tag]) > 6:
            print(f"        ... {len(undefined_by_ord[tag]) - 6} more")

    # strength of every external reference to the fmod ordinal
    return libs, undefined_by_ord


if __name__ == "__main__":
    p = sys.argv[1] if len(sys.argv) > 1 else r"E:\sts2_port\_ipa_probe\sts2"
    analyze(p)
