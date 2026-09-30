"""Lists texts from corpus/raw/assets.jsonl (see extract_assets.py) that corpus/source.jsonl lacks.

    python tools/corpus/find_missing.py              # summary per class + samples
    python tools/corpus/find_missing.py --write out.txt [Class ...]   # write keys of these classes

Only classes that hold player-visible text are considered (TEXT_CLASSES).
"""
import json
import pathlib
import sys
from collections import defaultdict

sys.path.insert(0, str(pathlib.Path(__file__).parent))
from build_corpus import normalize, useful  # noqa: E402

ROOT = pathlib.Path(__file__).resolve().parents[2]

# Script classes whose string fields are shown to the player.
TEXT_CLASSES = {
    "DialogueContainer", "DialogueModule", "QuestEntry", "Quest", "PhoneCallData", "NPCDataObject",
    "TextMeshProUGUI", "TextMeshPro", "Text", "DealerNPCDataObject", "StringDatabase", "VariableDatabase",
    "DialogueDatabase", "BuildableItemDefinition", "StorableItemDefinition", "ClothingDefinition",
    "PropertyItemDefinition", "InteractableObject", "ShopInterface", "ClothingShopInterface",
    "PawnShopInterface", "ATM", "DeliveryLocation", "DeadDrop", "Jukebox", "SystemTriggerObject",
    "InputPromptsDescriptorData", "NewMixScreen", "DelayedUnityEvent", "WorldStorageEntity",
    "Contract", "ContractInfo", "MessagesApp", "Tutorial", "HintDisplay", "CallInterface",
}


def main() -> None:
    source = {json.loads(line)["key"] for line in open(ROOT / "corpus" / "source.jsonl", encoding="utf-8")}
    missing = defaultdict(dict)
    for line in open(ROOT / "corpus" / "raw" / "assets.jsonl", encoding="utf-8"):
        row = json.loads(line)
        if row["class"] not in TEXT_CLASSES or not useful(row["text"]):
            continue
        key = normalize(row["text"])
        if key and key not in source:
            missing[row["class"]].setdefault(key, row["object"])

    if len(sys.argv) > 2 and sys.argv[1] == "--write":
        wanted = set(sys.argv[3:]) or set(missing)
        keys = [k for cls in sorted(wanted) for k in missing.get(cls, {})]
        pathlib.Path(sys.argv[2]).write_text("\n".join(dict.fromkeys(keys)) + "\n", encoding="utf-8")
        print(f"wrote {len(keys)} keys")
        return

    for cls, keys in sorted(missing.items(), key=lambda kv: -len(kv[1])):
        print(f"\n== {cls}: {len(keys)} missing")
        for key, obj in list(keys.items())[:12]:
            print(f"   [{obj}] {key[:140]!r}")


if __name__ == "__main__":
    main()
