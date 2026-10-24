"""Point a dylib's LC_LOAD_DYLIB at a name that does not exist, in place.

Why
---
`LC_LOAD_WEAK_DYLIB` only means "do not abort when the image cannot be found".
fmod's framework is present in Frameworks/, so dyld *does* find it, loads it and
runs its 489 C++ static constructors - still before Godot opens godot.log. Bending
the path to a nonexistent name makes the weak reference actually fail, so dyld
skips the image entirely, keeps the ordinal slot (no renumbering of the 19 later
libraries), and lets Godot's GDExtensionManager dlopen the real framework later,
once logging is up.

The main executable imports nothing from that ordinal, so nothing breaks: the
symbol dump shows lib#2 has 0 undefined symbols.

The path lives inside the load command's own storage, so the rewrite is a pure
in-place string overwrite padded with NULs - no command list surgery, no offset
shifts.
"""
import struct
import sys

LC_LOAD_DYLIB = 0xC
LC_LOAD_WEAK_DYLIB = 0x80000018
LC_REEXPORT_DYLIB = 0x8000001F
DYLIB_CMDS = (LC_LOAD_DYLIB, LC_LOAD_WEAK_DYLIB, LC_REEXPORT_DYLIB)


def rewrite(src, dst, needle, replacement):
    buf = bytearray(open(src, "rb").read())
    magic = struct.unpack_from("<I", buf, 0)[0]
    if magic != 0xFEEDFACF:
        raise SystemExit(f"not a 64-bit LE Mach-O (magic 0x{magic:08x})")

    ncmds, sizeofcmds = struct.unpack_from("<II", buf, 16)
    print(f"in  = {src}")
    print(f"      ncmds={ncmds} sizeofcmds={sizeofcmds} filesize={len(buf):,}")

    rep = replacement.encode()
    off = 32
    hits = 0
    for i in range(ncmds):
        cmd, size = struct.unpack_from("<II", buf, off)
        if cmd in DYLIB_CMDS:
            name_off = struct.unpack_from("<I", buf, off + 8)[0]
            slot = size - name_off                      # bytes available for the path
            cur = bytes(buf[off + name_off:off + size]).split(b"\x00")[0].decode(
                "utf-8", "replace")
            if needle in cur:
                if len(rep) + 1 > slot:
                    raise SystemExit(
                        f"replacement needs {len(rep)+1} bytes, slot has {slot}")
                buf[off + name_off:off + size] = rep + b"\x00" * (slot - len(rep))
                # A hard reference to a nonexistent image is a launch-time abort,
                # so the retarget only makes sense together with a weak bind.
                struct.pack_into("<I", buf, off, LC_LOAD_WEAK_DYLIB)
                print(f"      [{i:2d}] retargeted  {cur}")
                print(f"           ->           {replacement}   "
                      f"(slot {slot} bytes, used {len(rep)}, now LC_LOAD_WEAK_DYLIB)")
                hits += 1
        off += size
    if hits != 1:
        raise SystemExit(f"expected exactly 1 match for {needle!r}, got {hits}")

    open(dst, "wb").write(buf)
    print(f"out = {dst}  ({len(buf):,} bytes)")
    return dst


def verify(path, needle, replacement):
    buf = open(path, "rb").read()
    ncmds, sizeofcmds = struct.unpack_from("<II", buf, 16)
    off = 32
    consistent = True
    hard = weak = 0
    stale = 0
    for _ in range(ncmds):
        cmd, size = struct.unpack_from("<II", buf, off)
        if cmd in DYLIB_CMDS:
            if cmd == LC_LOAD_WEAK_DYLIB:
                weak += 1
            else:
                hard += 1
            name_off = struct.unpack_from("<I", buf, off + 8)[0]
            name = bytes(buf[off + name_off:off + size]).split(b"\x00")[0].decode(
                "utf-8", "replace")
            if needle in name and needle not in replacement:
                stale += 1
        off += size
    consistent = off == 32 + sizeofcmds
    print(f"\nverify {path}")
    print(f"  ncmds={ncmds} commands_end=0x{off:x} expected=0x{32+sizeofcmds:x} "
          f"sane={consistent}")
    print(f"  dylib refs: hard={hard} weak={weak}")
    print(f"  still pointing at {needle!r}: {stale}")
    return consistent and stale == 0


if __name__ == "__main__":
    s = sys.argv[1]
    d = sys.argv[2]
    n = sys.argv[3] if len(sys.argv) > 3 else "fmod.ios.template_release"
    r = sys.argv[4] if len(sys.argv) > 4 else "@rpath/fmod-disabled.framework/fmod-disabled"
    rewrite(s, d, n, r)
    ok = verify(d, n, r)
    print(f"\nresult: {'OK' if ok else 'FAILED'}")
    sys.exit(0 if ok else 1)
