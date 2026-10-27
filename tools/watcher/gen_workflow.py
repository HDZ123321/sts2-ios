"""Batch 5b: ship res/Watcher.pck inside the iOS bundle.

Adds a workflow step right after release_info.json is stamped that copies the
content pack next to the executable (the game mounts it at startup), and extends
the IPA sanity check so a missing pack fails the build.
"""

import io
import sys

PATH = r'E:\sts2_port\ci_bundle\.github\workflows\ios.yml'

STEP = """      - name: Copy the ported Watcher content pack into the bundle
        run: |
          set -euo pipefail
          APP="build/sts2.xcarchive/Products/Applications/sts2.app"
          # The base game sts2.pck is a prebuilt release asset (~1.9 GB) that CI
          # reuses verbatim, so the ported character ships as its own pack beside
          # the executable. BakedContent.Mount() loads it before atlases and
          # localization, and its loc tables merge into the base ones.
          test -f res/Watcher.pck
          cp res/Watcher.pck "$APP/Watcher.pck"
          ls -lh "$APP/Watcher.pck"
          python3 - "$APP/Watcher.pck" <<'PY'
          import os, sys
          path = sys.argv[1]
          with open(path, 'rb') as f:
              head = f.read(4)
          assert head == b'GDPC', 'Watcher.pck is not a Godot PCK: %r' % head
          size = os.path.getsize(path)
          assert size > 1000000, 'Watcher.pck looks truncated: %d bytes' % size
          print('Watcher.pck ok: %d bytes' % size)
          PY

"""

SANITY_OLD = "          test -f build/Payload/sts2.app/sts2.pck\n"
SANITY_NEW = ("          test -f build/Payload/sts2.app/sts2.pck\n"
              "          test -f build/Payload/sts2.app/Watcher.pck\n")


def main():
    with io.open(PATH, 'r', encoding='utf-8', newline='') as f:
        text = f.read()

    if 'Copy the ported Watcher content pack' not in text:
        anchor = '      - name: Package unsigned IPA\n'
        assert anchor in text, 'anchor for the new step not found'
        text = text.replace(anchor, STEP + anchor, 1)
    else:
        print('step already present')

    if 'Payload/sts2.app/Watcher.pck' not in text:
        assert SANITY_OLD in text, 'sanity check anchor not found'
        text = text.replace(SANITY_OLD, SANITY_NEW, 1)
    else:
        print('sanity assert already present')

    if 'Payload/sts2.app/(sts2|sts2.pck' in text:
        text = text.replace("Payload/sts2.app/(sts2|sts2.pck|release_info.json|Frameworks/)",
                            "Payload/sts2.app/(sts2|sts2.pck|Watcher.pck|release_info.json|Frameworks/)", 1)

    with io.open(PATH, 'w', encoding='utf-8', newline='') as f:
        f.write(text)
    print('patched', PATH)


if __name__ == '__main__':
    main()
