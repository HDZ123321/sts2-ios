"""Turn a hard LC_LOAD_DYLIB reference into LC_LOAD_WEAK_DYLIB.

Why
---
v7 dies instantly with no godot.log. That means the failure happens inside dyld,
before Main::setup() creates the RotatedFileLogger. A hard dylib dependency is the
only thing in the image that can kill the process at that stage - and the only new
hard dependency in v7 is the fmod framework.

Weakening it makes a load failure non-fatal:
  - dyld records the failure, sets the image's symbols to zero, and keeps going
  - Godot starts, opens godot.log, and its GDExtensionManager later dlopen()s
    Frameworks/fmod.ios.template_release.framework itself
  - if fmod still cannot load we now have a log saying WHY

Nothing is lost by weakening: the main executable imports no fmod symbols. Its only
fmod-ish undefined entries are `_fmod` and `_fmodf`, both math functions from
libSystem, and we verified `_load_all_fmod_plugins` appears zero times in `sts2`.

Usage
    py -3 weaken_dylib_ref.py <macho-in> <macho-out> [substring]
"""
import struct
import sys

LC_LOAD_DYLIB = 0xC
LC_LOAD_WEAK_DYLIB = 0x80000018


def u32(b, o):
    return int.from_bytes(b[o:o + 4], "little")


def weaken(src, dst, needle):
    buf = bytearray(open(src, "rb").read())
    magic = u32(buf, 0)
    if magic != 0xFEEDFACF:
        raise SystemExit(f"not a 64-bit LE Mach-O (magic 0x{magic:08x})")
    ncmds = u32(buf, 16)
    off = 32
    hits = []
    for _ in range(ncmds):
        cmd, size = u32(buf, off), u32(buf, off + 4)
        if cmd == LC_LOAD_DYLIB:
            no = u32(buf, off + 8)
            name = bytes(buf[off + no:off + size]).split(b"\x00")[0].decode(
                "utf-8", "replace")
            print(f"  LC_LOAD_DYLIB {name}")
            if needle in name:
                hits.append((off, name))
        off += size

    if not hits:
        print(f"  no LC_LOAD_DYLIB matching {needle!r}; nothing to do")
        if src != dst:
            open(dst, "wb").write(buf)
        return 0

    for o, name in hits:
        struct.pack_into("<I", buf, o, LC_LOAD_WEAK_DYLIB)
        print(f"  weakened @0x{o:x}: {name}")

    open(dst, "wb").write(buf)
    print(f"  wrote {dst} ({len(buf):,} bytes)")
    return len(hits)


def verify(path, needle):
    buf = open(path, "rb").read()
    ncmds = u32(buf, 16)
    off = 32
    hard = weak = 0
    for _ in range(ncmds):
        cmd, size = u32(buf, off), u32(buf, off + 4)
        if cmd == LC_LOAD_DYLIB:
            no = u32(buf, off + 8)
            name = bytes(buf[off + no:off + size]).split(b"\x00")[0].decode(
                "utf-8", "replace")
            if needle in name:
                hard += 1
        elif cmd == LC_LOAD_WEAK_DYLIB:
            no = u32(buf, off + 8)
            name = bytes(buf[off + no:off + size]).split(b"\x00")[0].decode(
                "utf-8", "replace")
            if needle in name:
                weak += 1
                print(f"  verify: LC_LOAD_WEAK_DYLIB {name}")
        off += size
    print(f"  verify: hard={hard} weak={weak} "
          f"(ncmds={ncmds}, commands sane={off == 32 + u32(buf, 20)})")
    return weak > 0 and hard == 0


if __name__ == "__main__":
    s = sys.argv[1]
    d = sys.argv[2]
    n = sys.argv[3] if len(sys.argv) > 3 else "fmod.ios.template_release"
    n_weakened = weaken(s, d, n)
    verify(d, n)
