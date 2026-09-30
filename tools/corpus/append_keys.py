"""Appends new English keys to corpus/source.jsonl WITHOUT reordering existing ones.

Existing ids (= line numbers) stay valid, so translations in progress are not invalidated.
New keys go into a new chunk file corpus/chunks/NN.jsonl (next free number).

Usage: python tools/corpus/append_keys.py tools/corpus/extra_mods_en.txt [category]
"""
import json
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).parent))
from build_corpus import normalize  # noqa: E402

ROOT = pathlib.Path(__file__).resolve().parents[2]


def main() -> None:
    src_file = pathlib.Path(sys.argv[1])
    category = sys.argv[2] if len(sys.argv) > 2 else "ui"
    source = ROOT / "corpus" / "source.jsonl"
    existing = [json.loads(line)["key"] for line in open(source, encoding="utf-8")]
    known = set(existing)

    new = []
    for line in open(src_file, encoding="utf-8"):
        line = line.rstrip("\n")
        if not line or line.startswith("//"):
            continue
        key = normalize(line)
        if key and key not in known:  # hand-written lists: no heuristic filtering
            known.add(key)
            new.append(key)
    if not new:
        print("nothing new")
        return

    chunks = ROOT / "corpus" / "chunks"
    number = max(int(p.stem) for p in chunks.glob("[0-9]*.jsonl")) + 1
    with open(source, "a", encoding="utf-8") as f_src, open(chunks / f"{number:02d}.jsonl", "w", encoding="utf-8") as f_chunk:
        for i, key in enumerate(new, len(existing)):
            f_src.write(json.dumps({"key": key, "cat": category, "ctx": [f"mod:{src_file.name}"]}, ensure_ascii=False) + "\n")
            f_chunk.write(json.dumps({"id": i, "key": key, "cat": category, "ctx": "UI of the WorldRates/Polyglot mods"},
                                     ensure_ascii=False) + "\n")
    print(f"appended {len(new)} keys as ids {len(existing)}..{len(existing) + len(new) - 1} -> chunk {number:02d}")


if __name__ == "__main__":
    main()
