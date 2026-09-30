"""Extracts user-facing string literals from the decompiled game code.

Input : decompiled Assembly-CSharp (ilspycmd -p output), default S:/S1Modding/decompiled/Assembly-CSharp
Output: corpus/raw/code_strings.jsonl  {"text", "src", "kind"}

Interpolated strings become templates: every {expr} hole turns into a <TOKEN> placeholder named
after the expression ({npc.FirstName} -> <FIRST_NAME>), which Polyglot matches at runtime.
"""
import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
SRC = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else "S:/S1Modding/decompiled/Assembly-CSharp")
OUT = ROOT / "corpus" / "raw" / "code_strings.jsonl"

SKIP_DIRS = ("Beautify", "LiquidVolume", "FishNet", "FishySteamworks", "SFB", "ItemIconCreator",
             "Properties", "ScheduleOne.Development", "ScheduleOne.Experimental", "Pathfinding",
             "VLB", "AmplifyImpostors", "Funly", "RootMotion", "EasyButtons", "ScheduleOne.Networking")
SKIP_FILES = {"Console.cs", "DebugInterface.cs", "CommandListScreen.cs", "ReportManager.cs", "ReportInterface.cs",
              "FrameTimingsHUDDisplay.cs", "VirtualMouseDebugger.cs", "IconGenerator.cs", "PixelData.cs",
              "FlockController.cs", "PlayingCard.cs", "CanvasSampleSaveFileText.cs", "CanvasSampleOpenFileText.cs",
              "CanvasSampleOpenFileImage.cs", "CameraCaptureToPNG.cs", "BasicSample.cs"}
# Lines containing these are logging, lookups or engine plumbing, not UI text.
SKIP_LINE = re.compile("|".join([
    r"Debug\.", r"Console\.(Log|Warn|Error|Submit)", r"LogWarning", r"LogError", r"\bLog\(", r"Exception\(",
    r"throw ", r"PlayerPrefs", r"Resources\.Load", r"\.Find\(", r"FindObject", r"GetComponent", r"CompareTag",
    r"\.tag\b", r"Animator", r"SetTrigger", r"SetBool", r"SetFloat", r"SetInteger", r"GetBool", r"GetFloat",
    r"Shader\.", r"\.SetTexture", r"StartCoroutine\(\"", r"Invoke\(\"", r"nameof\(", r"\[Header", r"\[Tooltip",
    r"\[Range", r"\[MenuItem", r"GetValue<", r"SetVariableValue", r"GetVariable", r"VariableDatabase",
    r"HasVariable", r"NotifyVariable", r"Path\.Combine", r"\.json", r"LoadScene", r"Achievement", r"Analytics",
    r"RpcWriter", r"RpcReader", r"SteamFriends", r"SetLobbyData", r"GetLobbyData", r"SendLobbyMessage",
    r"\.Contains\(\"", r"\.StartsWith\(\"", r"\.EndsWith\(\"", r"==\s*\"", r"!=\s*\"", r"case \"",
    r"GetItem\(\"", r"Registry\.", r"SetRichPresence", r"AudioMixer", r"mixer\.", r"PropertyToID",
    r"ColorUtility", r"EditorPrefs", r"GUID", r"Guid", r"DllImport", r"Regex", r"RaiseEvent",
    r"TryGetValue\(\"", r"\[\"", r"DataType =", r"DataVersion", r"WriteString", r"ReadString", r"Keyword",
    r"LayerMask", r"GetMask", r"NameToLayer",
]))
STRING = re.compile(r'(\$@|@\$|\$|@)?"((?:[^"\\\n]|\\.|"")*)"')
HOLE = re.compile(r"\{([^{}]+)\}")
IDENT = re.compile(r"[A-Za-z_][A-Za-z0-9_]*")
NOISE_IDENTS = {"ToString", "Format", "Mathf", "RoundToInt", "FloorToInt", "CeilToInt", "Round", "Instance",
                "Singleton", "NetworkSingleton", "MoneyManager", "FormatAmount", "this", "base", "Value", "value"}
ESCAPES = {"n": "\n", "t": "\t", "r": "\r", "\\": "\\", '"': '"', "'": "'", "0": "\0"}


CHAR = re.compile(r"'((?:[^'\\]|\\.)+)'")


def token_for(expr: str, used: set) -> str:
    expr = expr.split(":")[0].split(",")[0].strip()
    expr = re.sub(r"_003C(\w+?)_003Ek__BackingField", r"\1", expr).replace("SyncAccessor_", "")
    names = [n for n in IDENT.findall(expr) if n not in NOISE_IDENTS]
    base = names[-1] if names else "VALUE"
    base = re.sub(r"^Get(?=[A-Z])", "", base)
    if not base.isupper():
        base = re.sub(r"(?<!^)(?=[A-Z])", "_", base)
    base = re.sub(r"_+", "_", base).upper().strip("_") or "VALUE"
    token, i = base, 2
    while token in used:
        token, i = f"{base}{i}", i + 1
    used.add(token)
    return f"<{token}>"


def unescape(body: str, verbatim: bool) -> str:
    if verbatim:
        return body.replace('""', '"')
    out, i = [], 0
    while i < len(body):
        c = body[i]
        if c == "\\" and i + 1 < len(body):
            n = body[i + 1]
            if n == "u" and i + 5 < len(body):
                out.append(chr(int(body[i + 2:i + 6], 16)))
                i += 6
                continue
            out.append(ESCAPES.get(n, n))
            i += 2
            continue
        out.append(c)
        i += 1
    return "".join(out)


def looks_user_facing(text: str) -> bool:
    plain = re.sub(r"<[^>]+>", "", text)
    if sum(c.isalpha() for c in plain) < 2:
        return False
    if re.fullmatch(r"[a-z0-9_.\-/]+", text):  # ids, paths, keys
        return False
    if " " not in text and re.fullmatch(r"[A-Za-z]+[A-Z][A-Za-z0-9]*", text):  # camelCase / PascalCase ids
        return False
    if "/" in text and " " not in text:
        return False
    return " " in text.strip() or text[:1].isupper()


def split_top_level(segment: str, sep: str) -> list:
    """Splits on `sep` outside strings and brackets."""
    parts, depth, cur, i = [], 0, [], 0
    while i < len(segment):
        m = STRING.match(segment, i)
        if m and segment[i] in '"$@':
            cur.append(m.group(0))
            i = m.end()
            continue
        c = segment[i]
        if c in "([{":
            depth += 1
        elif c in ")]}":
            depth -= 1
        if depth == 0 and segment.startswith(sep, i):
            parts.append("".join(cur))
            cur = []
            i += len(sep)
            continue
        cur.append(c)
        i += 1
    parts.append("".join(cur))
    return parts


def mask_strings(line: str) -> str:
    """Same length as `line`, string/char literal contents replaced by '_' so delimiters inside are ignored."""
    chars = list(line)
    for m in list(STRING.finditer(line)) + list(CHAR.finditer(line)):
        for i in range(m.start() + 1, m.end() - 1):
            chars[i] = "_"
    return "".join(chars)


def enclosing_expression(line: str, start: int, end: int) -> str:
    """The argument/assignment expression around the literal at [start, end)."""
    raw, line = line, mask_strings(line)
    depth, left = 0, start
    while left > 0:
        c = line[left - 1]
        if c in ")]}":
            depth += 1
        elif c in "([{":
            if depth == 0:
                break
            depth -= 1
        elif depth == 0 and c in ",;=?:":
            break
        left -= 1
    depth, right = 0, end
    while right < len(line):
        c = line[right]
        if c in "([{":
            depth += 1
        elif c in ")]}":
            if depth == 0:
                break
            depth -= 1
        elif depth == 0 and c in ",;?:":
            break
        right += 1
    expr = raw[left:right].strip()
    return re.sub(r"^return\s+", "", expr)


def concat_template(line: str, m: re.Match):
    expr = enclosing_expression(line, m.start(), m.end())
    parts = [p.strip() for p in split_top_level(expr, " + ")]
    if len(parts) < 2 or any(not p for p in parts):
        return None
    used: set = set()
    out = []
    for p in parts:
        lit = STRING.fullmatch(p)
        char = CHAR.fullmatch(p)
        if lit and "$" not in (lit.group(1) or ""):
            out.append(unescape(lit.group(2), "@" in (lit.group(1) or "")))
        elif char:
            out.append(unescape(char.group(1), False))
        elif re.fullmatch(r"\d+(\.\d+)?[fFdDmM]?", p):
            out.append(p.rstrip("fFdDmM"))
        elif re.search(r"&&|\|\||==|!=|<=|>=|\?", mask_strings(p)):
            return None
        else:
            out.append(token_for(p, used))
    text = "".join(out)
    literal_letters = sum(c.isalpha() for c in re.sub(r"<[^>]+>", "", text))
    return text if literal_letters >= 3 else None


def main() -> None:
    OUT.parent.mkdir(parents=True, exist_ok=True)
    rows, seen = [], set()
    for path in sorted(SRC.rglob("*.cs")):
        rel = path.relative_to(SRC).as_posix()
        if rel.split("/")[0].startswith(SKIP_DIRS) or path.name in SKIP_FILES:
            continue
        for lineno, line in enumerate(path.read_text(encoding="utf-8", errors="ignore").splitlines(), 1):
            stripped = line.strip()
            if stripped.startswith(("//", "using ", "[")) or SKIP_LINE.search(line):
                continue
            for m in STRING.finditer(line):
                prefix, body = m.group(1) or "", m.group(2)
                text = unescape(body, "@" in prefix)
                kind = "literal"
                if "$" in prefix:
                    used: set = set()
                    masked = text.replace("{{", "\x01").replace("}}", "\x02")
                    text = HOLE.sub(lambda h: token_for(h.group(1), used), masked)
                    text = text.replace("\x01", "{").replace("\x02", "}")
                    kind = "interpolated"
                elif "Format(" in line and re.search(r"\{\d+(:[^}]*)?\}", text):
                    text = re.sub(r"\{(\d+)(:[^}]*)?\}", lambda h: f"<ARG{h.group(1)}>", text)
                    kind = "format"
                elif " + " in line:
                    template = concat_template(line, m)
                    if template is not None:
                        text, kind = template, "concat"
                if not looks_user_facing(text):
                    continue
                if (text, rel) in seen:
                    continue
                seen.add((text, rel))
                rows.append({"text": text, "src": f"code:{rel}:{lineno}", "kind": kind})
    with OUT.open("w", encoding="utf-8") as f:
        for r in rows:
            f.write(json.dumps(r, ensure_ascii=False) + "\n")
    print(f"{len(rows)} strings -> {OUT}")


if __name__ == "__main__":
    main()
