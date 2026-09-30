"""Splits corpus/source.jsonl into numbered chunks for translation.

Output: corpus/chunks/NN.jsonl   {"id", "key", "cat", "ctx"}  (ids are stable line numbers of source.jsonl)
"""
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
SIZE = int(sys.argv[1]) if len(sys.argv) > 1 else 200


def main() -> None:
    rows = [json.loads(line) for line in open(ROOT / "corpus" / "source.jsonl", encoding="utf-8")]
    out = ROOT / "corpus" / "chunks"
    out.mkdir(parents=True, exist_ok=True)
    for old in out.glob("*.jsonl"):
        old.unlink()
    for n, start in enumerate(range(0, len(rows), SIZE)):
        with (out / f"{n:02d}.jsonl").open("w", encoding="utf-8") as f:
            for i, r in enumerate(rows[start:start + SIZE], start):
                f.write(json.dumps({"id": i, "key": r["key"], "cat": r["cat"], "ctx": "; ".join(r["ctx"])[:160]},
                                   ensure_ascii=False) + "\n")
    print(f"{len(rows)} keys -> {n + 1} chunks of {SIZE}")


if __name__ == "__main__":
    main()
