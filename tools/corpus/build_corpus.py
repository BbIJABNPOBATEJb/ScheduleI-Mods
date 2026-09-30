"""Builds the English source corpus that Polyglot translations are made from.

Inputs (all optional, whatever exists is merged):
  test-runs/*corpus*/corpus_*.jsonl       runtime dump of serialized strings (Polyglot Dev scenario "corpus")
  corpus/raw/code_strings.jsonl            string literals / templates from decompiled code (extract_code.py)
  corpus/raw/untranslated_*.txt            texts seen in game without translation (Polyglot LogUntranslated)
Output:
  corpus/source.jsonl   {"key", "cat", "ctx"} one entry per unique key, grouped by category

Numbers are normalized to {0}, {1}... exactly like Polyglot's Translator does at runtime.
"""
import collections
import glob
import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "corpus" / "source.jsonl"

NUMBERS = re.compile(r"(?<![\w#])\d+(?:[.,:]\d+)*")
GUID = re.compile(r"[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", re.I)
TAG = re.compile(r"<[^<>]+>")
PLACEHOLDER = re.compile(r"<[A-Z][A-Z0-9_]*>")

# Runtime dump fields that never hold player-visible text.
EXCLUDE_SRC = re.compile("|".join([
    r"GUID", r"Guid", r"guid", r"BindingId", r"\.ID$", r"\._id$", r"\.Id$", r"\bId$", r"InlineId", r"Code$",
    r"CardID", r"ProductID", r"uniquenessCode", r"AssetPath", r"[Pp]ath$", r"ControlPath", r"VariableName",
    r"\.Key$", r"NodeLabel", r"ChoiceLabel", r"PropertyName", r"AnimationTrigger", r"LiquidType", r"TrackName",
    r"_trackName", r"WeatherSequence", r"WeatherProfile", r"AudioSettings", r"HapticsData", r"GamepadPointerData",
    r"^Console\.", r"LaunchArgs", r"startupCommands", r"CustomLink", r"Registry\.ItemRegistry", r"Listings\[\]\.name",
    r"icons\[\]\.name", r"SerializedGraffitiDrawing", r"LightVisibilityAffector", r"RouteName", r"PresetName",
    r"CharacterCustomizationOption\.Label", r"InputPromptsData\.Id", r"SpriteLabel", r"InlineLabel", r"QuestName",
    r"NPCUnlockedVariable", r"name[12]Library", r"ProductNames", r"FirstName", r"LastName", r"FirstNames",
    r"LastNames", r"contactName", r"ShopName", r"MatchingShopInterfaceName", r"AvatarLayer\.Name", r"FaceLayer\.Name",
    r"Hair\.Name", r"Accessory\.Name", r"EmotionPreset", r"Settings\.LaunchArgs", r"ActionName", r"\.name$",
    r"ConditionList\[\]\.Value", r"vehicleCode", r"RebindActionUI", r"DrawingName", r"StaticGUID", r"HomeName$",
    r"TrashItem\.", r"\.ShopCode", r"_serializedGUID", r"Jukebox", r"MusicTrack",
]))
JUNK_VALUES = {"New Text", "Button Name", "Option Name", "Text", "Label", "Title", "Placeholder", "Enter text...",
               "Heading", "Subtitle", "Description", "Name", "Item Name", "Value", "Lorem ipsum"}

CATEGORY_RULES = [
    ("dialogue", r"Dialogue|Lines\[\]|Greeting|ChoiceText|PhoneCallData|StringDatabase|OfferLines|Response|Message\w*Lines"),
    ("item", r"Definition\.(Name|Description)|PropertyUtility|ColorFont|ClothingUtility"),
    ("quest", r"Quest"),
    ("place", r"DeliveryLocation|DeadDrop|NPCEnterableBuilding|Map\.Regions|StorageEntity|Property|Region"),
    ("ui", r"TMP_Text|UI\.Text|Tooltip|InputPrompt|optionName|TMP_Dropdown|InputDescriptor|message|Label|Window"),
]


def normalize(text: str) -> str:
    text = text.replace("\r\n", "\n").strip()
    if PLACEHOLDER.search(text):
        return text  # templates keep their literal digits
    counter = iter(range(1000))
    return NUMBERS.sub(lambda m: "{" + str(next(counter)) + "}", text)


def useful(text: str) -> bool:
    if not text or len(text) > 3000 or text in JUNK_VALUES:
        return False
    plain = PLACEHOLDER.sub("", TAG.sub("", text))
    if sum(c.isalpha() for c in plain) < 2 or GUID.search(text):
        return False
    if re.fullmatch(r"[a-z0-9_.\-/]+", plain.strip()):
        return False
    if " " not in plain.strip() and re.fullmatch(r"[A-Za-z]+_[A-Za-z0-9_]+", plain.strip()):
        return False
    return True


def category(src: str) -> str:
    if src.startswith("code:"):
        return "code"
    if src.startswith("miss"):
        return "runtime"
    if src == "enum":
        return "ui"
    for name, pattern in CATEGORY_RULES:
        if re.search(pattern, src):
            return name
    return "data"


def load_rows():
    for path in sorted(glob.glob(str(ROOT / "test-runs" / "*corpus*" / "corpus_*.jsonl"))):
        for line in open(path, encoding="utf-8"):
            r = json.loads(line)
            if not EXCLUDE_SRC.search(r["src"]):
                yield r["text"], r["src"], r.get("obj", "")
    code = ROOT / "corpus" / "raw" / "code_strings.jsonl"
    if code.exists():
        for line in open(code, encoding="utf-8"):
            r = json.loads(line)
            yield r["text"], r["src"], r["kind"]
    extra = pathlib.Path(__file__).with_name("extra_en.txt")
    for line in open(extra, encoding="utf-8"):
        line = line.rstrip("\n")
        if line and not line.startswith("//"):
            yield line, "enum", ""
    for path in glob.glob(str(ROOT / "corpus" / "raw" / "untranslated_*.txt")):
        for line in open(path, encoding="utf-8"):
            key = line.rstrip("\n")
            if key.endswith("="):
                key = key[:-1].replace("\\n", "\n").replace("\\=", "=").replace("\\\\", "\\")
                yield key, "miss:" + pathlib.Path(path).stem, ""


def main() -> None:
    entries = collections.OrderedDict()
    for text, src, obj in load_rows():
        # Miss logs are already normalized by Polyglot; normalizing again would turn {0} into {{0}}.
        key = text if src.startswith("miss") else normalize(text)
        if not useful(key):
            continue
        e = entries.setdefault(key, {"key": key, "cat": category(src), "ctx": []})
        if len(e["ctx"]) < 3:
            ctx = src if not obj else f"{src} @ {obj[-60:]}"
            if ctx not in e["ctx"]:
                e["ctx"].append(ctx)
        # Prefer the most descriptive category.
        if e["cat"] in ("ui", "code", "runtime", "data") and category(src) in ("dialogue", "item", "quest", "place"):
            e["cat"] = category(src)

    order = ["ui", "item", "quest", "place", "dialogue", "code", "runtime", "data"]
    rows = sorted(entries.values(), key=lambda e: (order.index(e["cat"]), e["key"].lower()))
    OUT.parent.mkdir(parents=True, exist_ok=True)
    with OUT.open("w", encoding="utf-8") as f:
        for r in rows:
            f.write(json.dumps(r, ensure_ascii=False) + "\n")
    counts = collections.Counter(r["cat"] for r in rows)
    words = sum(len(TAG.sub("", r["key"]).split()) for r in rows)
    print(f"{len(rows)} keys, ~{words} words -> {OUT}")
    print(dict(counts))


if __name__ == "__main__":
    main()
