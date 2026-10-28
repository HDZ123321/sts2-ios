"""Strip a UTF-8 BOM from source files that upstream keeps without one.

The patch scripts opened files with encoding="utf-8-sig", which reads and writes
a BOM. Upstream .cs files have no BOM, so the BOM shows up as a spurious change
on the first line and trips the CRLF warning in git diff. This normalises the
two files that picked one up.
"""

import io

FILES = (
    r"E:\sts2_port\ci_bundle\src\Core\Assets\AssetLoadingSession.cs",
    r"E:\sts2_port\ci_bundle\src\Core\Assets\PreloadManager.cs",
)

BOM = "\ufeff"


def main():
    for path in FILES:
        with io.open(path, "r", encoding="utf-8-sig", newline="") as handle:
            text = handle.read()
        with io.open(path, "w", encoding="utf-8", newline="") as handle:
            handle.write(text)
        print("stripped:", path)


if __name__ == "__main__":
    main()