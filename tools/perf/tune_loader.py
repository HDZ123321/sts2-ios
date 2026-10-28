"""Set the loader's in-flight window and sub-thread flag.

The first version of this rewrote `while (_loading.Count < 128 && ...` literally,
which stopped matching once the literal was replaced by the `_batchSize`
constant. This version handles both spellings, so it stays usable after the
constant has taken over.

Usage:
  python tune_loader.py --batch 512 --sub 0
"""

import argparse
import io
import re

PATH = r"E:\sts2_port\ci_bundle\src\Core\Assets\AssetLoadingSession.cs"

BATCH_RE = re.compile(r"(private const int _batchSize = )\d+(;)")
LOOP_RE = re.compile(r"(while \(_loading\.Count < )(?:_batchSize|\d+)( && _toLoad\.TryDequeue\(out result\)\))")
SUB_RE = re.compile(
    r"(ResourceLoader\.LoadThreadedRequest\(result, \"\", useSubThreads: )(?:false|true)"
    r"(, ResourceLoader\.CacheMode\.Reuse\) == Error\.Ok\)\n\t\t\t\t\{\n\t\t\t\t\t_loading\.Enqueue)"
)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--batch", type=int, required=True)
    parser.add_argument("--sub", type=int, choices=(0, 1), required=True)
    args = parser.parse_args()

    with io.open(PATH, "r", encoding="utf-8", newline="") as handle:
        text = handle.read()

    newline = "\r\n" if "\r\n" in text else "\n"
    body = text.replace("\r\n", "\n")

    body, n1 = BATCH_RE.subn(lambda m: m.group(1) + str(args.batch) + m.group(2), body, count=1)
    if n1 != 1:
        raise SystemExit("MISS: _batchSize constant")

    body, n2 = LOOP_RE.subn(lambda m: m.group(1) + "_batchSize" + m.group(2), body, count=1)
    if n2 != 1 and "while (_loading.Count < _batchSize && _toLoad.TryDequeue(out result))" not in body:
        raise SystemExit("MISS: loading loop")

    literal = "true" if args.sub else "false"
    body, n3 = SUB_RE.subn(lambda m: m.group(1) + literal + m.group(2), body, count=1)
    if n3 != 1:
        raise SystemExit("MISS: sub-thread flag")

    with io.open(PATH, "w", encoding="utf-8", newline="") as handle:
        handle.write(body.replace("\n", newline))
    print("batch=%d sub=%s" % (args.batch, literal))


if __name__ == "__main__":
    main()