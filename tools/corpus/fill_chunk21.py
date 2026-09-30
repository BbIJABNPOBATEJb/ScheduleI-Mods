"""Writes corpus/tr/<lang>/21.jsonl for the compass / purchase strings (chunk 21).

Single letters N/E/S/W are also keyboard key labels in input prompts (W/A/S/D, E to interact),
so they stay untranslated everywhere: translating "E" would break the key hints.
"""
import json
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
# key order in extra_runtime_en.txt: N, E, S, W, Purchase (${0})
T = {
    "ru": ["N", "E", "S", "W", "Купить (${0})"],
    "uk": ["N", "E", "S", "W", "Купити (${0})"],
    "de": ["N", "E", "S", "W", "Kaufen (${0})"],
    "fr": ["N", "E", "S", "W", "Acheter (${0})"],
    "es": ["N", "E", "S", "W", "Comprar (${0})"],
    "pt-BR": ["N", "E", "S", "W", "Comprar (${0})"],
    "it": ["N", "E", "S", "W", "Acquista (${0})"],
    "pl": ["N", "E", "S", "W", "Kup (${0})"],
    "tr": ["N", "E", "S", "W", "Satın Al (${0})"],
    "zh-CN": ["N", "E", "S", "W", "购买（${0}）"],
    "ja": ["N", "E", "S", "W", "購入（${0}）"],
    "ko": ["N", "E", "S", "W", "구매 (${0})"],
}


def main() -> None:
    chunk = [json.loads(line) for line in open(ROOT / "corpus" / "chunks" / "21.jsonl", encoding="utf-8")]
    order = ["N", "E", "S", "W", "Purchase (${0})"]
    for lang, values in T.items():
        by_key = dict(zip(order, values))
        with open(ROOT / "corpus" / "tr" / lang / "21.jsonl", "w", encoding="utf-8") as f:
            for row in chunk:
                f.write(json.dumps({"id": row["id"], "t": by_key[row["key"]]}, ensure_ascii=False) + "\n")
    print(f"wrote chunk 21 for {len(T)} languages ({len(chunk)} keys)")


if __name__ == "__main__":
    main()
