"""Extracts every serialized text string from the game's asset files.

The in-game corpus dump (Polyglot `corpus` smoke scenario) only sees objects that are loaded in
the scenes it visits. This script reads the asset files directly with UnityPy, so it also finds
phone calls, dialogue and quest data that are never loaded during a smoke run.

MonoBehaviours are read without type trees: every 4-byte aligned, length-prefixed UTF-8 string in
an object's data is taken, which is how Unity serializes `string` fields.

    pip install UnityPy
    python tools/corpus/extract_assets.py "S:/S1Modding/ScheduleI_Mono/Schedule I_Data" corpus/raw/assets.jsonl

Output: one JSON object per line: {"text", "class", "object", "file"}.
"""
import json
import struct
import sys
from collections import Counter
from pathlib import Path

import UnityPy

MAX_LEN = 20000


def scan_strings(data: bytes):
    """Yields (offset, text) for aligned length-prefixed UTF-8 strings."""
    i = 0
    n = len(data)
    while i + 4 <= n:
        length = struct.unpack_from("<i", data, i)[0]
        if 2 <= length <= MAX_LEN and i + 4 + length <= n:
            raw = data[i + 4:i + 4 + length]
            text = None
            if all(b >= 0x20 or b in (9, 10, 13) for b in raw):
                try:
                    text = raw.decode("utf-8")
                except UnicodeDecodeError:
                    text = None
            if text is not None and any(c.isalpha() for c in text):
                yield i, text
                i += 4 + length
                i = (i + 3) & ~3
                continue
        i += 4


def main():
    data_dir = Path(sys.argv[1])
    out_path = Path(sys.argv[2])
    out_path.parent.mkdir(parents=True, exist_ok=True)

    files = sorted(p for p in data_dir.iterdir()
                   if p.is_file() and (p.suffix == ".assets" or p.name.startswith("level")) and not p.name.endswith(".resS"))
    seen = set()
    classes = Counter()
    with out_path.open("w", encoding="utf-8") as out:
        for path in files:
            env = UnityPy.load(str(path))
            scripts = {}
            count = 0
            for obj in env.objects:
                if obj.type.name != "MonoBehaviour":
                    continue
                raw = obj.get_raw_data()
                # Header: m_GameObject PPtr (12), m_Enabled (4 incl. align), m_Script PPtr (12), m_Name string.
                try:
                    reader = obj.read(check_read=False)
                    script = reader.m_Script.read() if reader.m_Script else None
                    cls = script.m_ClassName if script else "?"
                    name = getattr(reader, "m_Name", "") or ""
                except Exception:
                    cls, name = "?", ""
                name_len = struct.unpack_from("<i", raw, 28)[0] if len(raw) >= 32 else 0
                body = 32 + ((max(name_len, 0) + 3) & ~3)
                for _, text in scan_strings(raw[body:]):
                    key = (text, cls)
                    if key in seen:
                        continue
                    seen.add(key)
                    classes[cls] += 1
                    count += 1
                    out.write(json.dumps({"text": text, "class": cls, "object": name, "file": path.name},
                                         ensure_ascii=False) + "\n")
            print(f"{path.name}: {count} strings", flush=True)
    print("\nstrings per class:")
    for cls, n in classes.most_common(60):
        print(f"  {n:6}  {cls}")


if __name__ == "__main__":
    main()
