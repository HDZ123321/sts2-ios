"""Register every concrete Watcher model, including ones behind an abstract base.

The first pass matched `class X : <family>` by the *immediate* base class. Three
cards -- BecomeAlmighty, FameAndFortune, LiveForever -- derive from
WishableWatcherCard, which is abstract and itself derives from WatcherCardModel,
so they were skipped and ModelDb never constructed them. Asking for one by id
then threw ModelNotFoundException mid-run, which is exactly what killed the
Watcher run on seed 90210.

This walks the base chain instead: a class belongs to the model set if any
ancestor is one of the known families, and it is not abstract itself.
"""

import io
import os
import re

SRC = r"E:\sts2_port\ci_bundle\src\Watcher"
SUBTYPES = r"E:\sts2_port\ci_bundle\src\Core\Models\AbstractModelSubtypes.cs"

FAMILIES = {
    "WatcherCardModel",
    "WatcherPowerModel",
    "WatcherRelicModel",
    "WatcherPotionModel",
    "CustomCardPoolModel",
    "CustomRelicPoolModel",
    "CustomPotionPoolModel",
    "CustomCharacterModel",
    "CustomSingletonModel",
    "WatcherStanceModel",
}

CLASS_RE = re.compile(
    r"^\s*(?P<mods>(?:(?:public|internal|private|protected|sealed|abstract|static|partial)\s+)*)"
    r"class\s+(?P<name>[A-Za-z0-9_]+)\s*(?::\s*(?P<bases>[^{]+))?",
    re.MULTILINE,
)


def scan():
    """name -> (namespace, is_abstract, base names)"""
    found = {}
    for root, _dirs, files in os.walk(SRC):
        for filename in files:
            if not filename.endswith(".cs"):
                continue
            path = os.path.join(root, filename)
            with io.open(path, "r", encoding="utf-8-sig", errors="replace") as handle:
                text = handle.read()
            match_ns = re.search(r"^\s*namespace\s+([A-Za-z0-9_.]+)", text, re.MULTILINE)
            namespace = match_ns.group(1) if match_ns else ""
            for match in CLASS_RE.finditer(text):
                name = match.group("name")
                mods = match.group("mods") or ""
                bases = match.group("bases") or ""
                base_names = []
                for chunk in bases.split(","):
                    chunk = chunk.strip()
                    if not chunk:
                        continue
                    if "<" in chunk:
                        chunk = chunk.split("<", 1)[0]
                    chunk = chunk.split(".")[-1].strip()
                    if chunk:
                        base_names.append(chunk)
                found[name] = (namespace, "abstract" in mods, base_names)
    return found


def main():
    classes = scan()

    def is_model(name, seen=None):
        seen = seen or set()
        if name in seen or name not in classes:
            return False
        seen.add(name)
        _ns, _abstract, bases = classes[name]
        for base in bases:
            if base in FAMILIES:
                return True
            if is_model(base, seen):
                return True
        return False

    wanted = []
    for name, (namespace, abstract, _bases) in classes.items():
        if abstract or name in FAMILIES:
            continue
        if is_model(name):
            wanted.append(namespace + "." + name)

    wanted.sort()

    with io.open(SUBTYPES, "r", encoding="utf-8-sig", newline="") as handle:
        text = handle.read()
    newline = "\r\n" if "\r\n" in text else "\n"
    body = text.replace("\r\n", "\n")

    already = set(re.findall(r"typeof\(global::([A-Za-z0-9_.]+)\)", body))
    missing = [name for name in wanted if name not in already]

    if not missing:
        print("nothing missing; registered =", len(already & set(wanted)))
        return

    lines = body.split("\n")
    # Append after the last registered Watcher entry so the list stays contiguous.
    last_index = max(
        i for i, line in enumerate(lines) if line.strip().startswith("typeof(global::Watcher.")
    )
    if not lines[last_index].rstrip().endswith(","):
        lines[last_index] = lines[last_index].rstrip() + ","

    inserts = ["\t\ttypeof(global::%s)," % name for name in missing]
    # The final element must not carry a trailing comma; it will be followed by
    # whatever the list already had, so a trailing comma is correct here as long
    # as we insert before existing entries rather than at the very end.
    lines[last_index + 1:last_index + 1] = inserts

    body = "\n".join(lines)
    # Drop a trailing comma on the very last element if we landed there.
    body = re.sub(r",(\s*\n\t*\};)", r"\1", body)

    with io.open(SUBTYPES, "w", encoding="utf-8", newline="") as handle:
        handle.write(body.replace("\n", newline))

    print("registered %d missing model(s):" % len(missing))
    for name in missing:
        print("  " + name)


if __name__ == "__main__":
    main()