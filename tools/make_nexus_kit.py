"""Builds dist/nexus/<Mod>/ — everything needed to create or update a Nexus Mods page by hand:

    fields.txt              every form field and what to put in it
    description.bbcode.txt  the mod's README converted to Nexus BBCode
    banner-1920x1080.png    16:9 image
    banner-1300x372.png     wide header image
    01_*.jpg ...            gallery screenshots
    <Mod>-<ver>-IL2CPP.zip / -Mono.zip   the files to upload (run tools/package.ps1 first)

Nexus has no API for creating pages or editing descriptions (only for uploading files to an
existing page), so the page itself is filled in by the account owner.

Usage: python tools/make_nexus_kit.py [Mod ...]        (needs Pillow)
"""
import pathlib
import re
import shutil
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT = ROOT / "dist" / "nexus"
FONTS = ROOT / "mods" / "Polyglot" / "fonts"
REPO = "https://github.com/BbIJABNPOBATEJb/ScheduleI-Mods"
RAW = "https://raw.githubusercontent.com/BbIJABNPOBATEJb/ScheduleI-Mods/main"
THUNDERSTORE = "https://thunderstore.io/c/schedule-i/p/BbIJABNPOBATEJb"
MELONLOADER = "https://github.com/LavaGang/MelonLoader/releases"

MODS = {
    "Polyglot": {
        "page_name": "Polyglot - 13 Languages with Live Switching",
        "category": "User Interface",
        "tagline": "Play in 13 languages.\nSwitch instantly, in game.",
        "summary": "Play Schedule I in 13 languages (Russian, Ukrainian, German, French, Spanish, Portuguese, Italian, "
                   "Polish, Turkish, Chinese, Japanese, Korean) and switch instantly in Settings or with F9. "
                   "Full translation with matching fonts. IL2CPP and Mono.",
        "banner": ("docs/images/polyglot-language-picker.jpg", None),
        "gallery": ["polyglot-language-picker.jpg", "polyglot-contacts-ru.jpg", "polyglot-products-ru.jpg",
                    "polyglot-contacts-zh.jpg"],
        "tags": "Translation, Localisation, Quality of Life, MelonLoader",
        "credits": "Open Sans and Caveat fonts by their authors (SIL Open Font License 1.1). "
                   "Developed with AI assistance (Claude by Anthropic).",
    },
    "WorldRates": {
        "page_name": "World Rates - XP Income and Storage Multipliers",
        "category": "Gameplay",
        "tagline": "Rust-style server rates:\nXP, income, storage.",
        "summary": "Server-style rates like on Rust servers: multiply XP, income, deal frequency and storage capacity "
                   "per source and per world, tuned live in a native in-game window (pause menu or F10). "
                   "Presets x1-x10. IL2CPP and Mono.",
        "banner": ("docs/images/worldrates-experience.jpg", None),
        "gallery": ["worldrates-experience.jpg", "worldrates-deals.jpg", "worldrates-presets.jpg", "worldrates-storage-x2.jpg",
                    "worldrates-pause-menu.jpg", "worldrates-ru.jpg"],
        "tags": "Gameplay, Cheats / Balance, Configurable, MelonLoader",
        "credits": "Developed with AI assistance (Claude by Anthropic).",
    },
    "DamageIndicator": {
        "page_name": "Damage Indicator - Health Bars and Damage Numbers",
        "category": "User Interface",
        "tagline": "Health bars and damage\nnumbers, hidden until\nsomeone gets hurt.",
        "summary": "Health bars over characters after they take damage, your own movable health bar, floating damage "
                   "numbers, a stun bar and KO badges. Nothing is shown until someone takes damage. "
                   "Fully configurable in game. IL2CPP and Mono.",
        "banner": ("docs/images/damageindicator-hit.jpg", None),
        "gallery": ["damageindicator-hit.jpg", "damageindicator-ko.jpg", "damageindicator-player.jpg",
                    "damageindicator-settings.jpg"],
        "tags": "HUD, Combat, Quality of Life, Configurable, MelonLoader",
        "credits": "Developed with AI assistance (Claude by Anthropic).",
    },
    "GuideArrows": {
        "page_name": "Guide Arrows - 3D Compass Arrows to Deals Stashes and Quests",
        "category": "User Interface",
        "tagline": "3D arrows under the\ncompass to your next\ndeal, stash or quest.",
        "summary": "3D arrows under the compass that point at the nearest deal, stash with items, quest objective, "
                   "potential customer and home base, each in its own color. Smooth, configurable and movable. "
                   "IL2CPP and Mono.",
        "banner": ("test-runs/DamageIndicator-combat-il2cpp-20260930-211749/hud_plain.png", (520, 0, 1400, 500)),
        "gallery": ["guidearrows-glow-wall.jpg", "guidearrows-glow-street.jpg", "guidearrows-glow-stash.jpg",
                    "guidearrows-hud.jpg", "guidearrows-labels.jpg", "guidearrows-settings.jpg"],
        "extra_gallery": [("test-runs/DamageIndicator-combat-il2cpp-20260930-211749/hud_plain.png", "in-game.jpg")],
        "tags": "HUD, Navigation, Quality of Life, Configurable, MelonLoader",
        "credits": "Developed with AI assistance (Claude by Anthropic).",
    },
    "QuietPause": {
        "page_name": "Quiet Pause - Mute in Background and When Paused",
        "category": "Audio",
        "tagline": "Silence when minimized.\nSilence in the pause menu.",
        "summary": "Mutes Schedule I while it is minimized or unfocused and pauses world sounds while the pause menu "
                   "is open (menu clicks stay audible, music optional). Options in the game's Settings > Audio tab. "
                   "IL2CPP and Mono.",
        "banner": ("docs/images/quietpause-settings.jpg", None),
        "gallery": ["quietpause-settings.jpg"],
        "tags": "Audio, Quality of Life, Bug Fix, MelonLoader",
        "credits": "Developed with AI assistance (Claude by Anthropic).",
    },
    "ElectricScooter": {
        "page_name": "Electric Scooter - Rolls Over Curbs No Stamina 30 Percent Faster",
        "category": "Gameplay",
        "tagline": "A scooter that rolls\nover curbs. No stamina,\n30% faster.",
        "summary": "An electric scooter sold at the skate shop for $5,000. Rides like the Golden Skateboard but rolls "
                   "over curbs instead of stopping at them, uses no stamina and is 30% faster. Own model, "
                   "configurable. IL2CPP and Mono.",
        "banner": ("docs/images/electricscooter-ride.jpg", None),
        "gallery": ["electricscooter-ride.jpg", "electricscooter-front.jpg", "electricscooter-shop.jpg",
                    "electricscooter-held.jpg"],
        "tags": "Gameplay, Vehicles, New Item, Configurable, MelonLoader",
        "credits": "Developed with AI assistance (Claude by Anthropic).",
    },
}


# --- README (Markdown) -> Nexus BBCode --------------------------------------------------------------

def absolute(url: str, mod: str) -> str:
    if re.match(r"https?://|#", url):
        return url
    if url.startswith("../../"):
        return f"{REPO}/tree/main/{url[6:]}"
    if url.startswith("../"):
        return f"{REPO}/tree/main/mods/{url[3:]}"
    return f"{REPO}/blob/main/mods/{mod}/{url}"


def inline(text: str, mod: str) -> str:
    text = re.sub(r"\[`?([^\]`]+)`?\]\(([^)]+)\)", lambda m: f"[url={absolute(m.group(2), mod)}]{m.group(1)}[/url]", text)
    text = re.sub(r"\*\*(.+?)\*\*", r"[b]\1[/b]", text)
    text = re.sub(r"(?<![\w*])\*(?!\s)(.+?)(?<!\s)\*(?![\w*])", r"[i]\1[/i]", text)
    text = re.sub(r"`([^`]+)`", r"[font=Courier New]\1[/font]", text)
    return text


def image_urls(line: str, mod: str):
    for src in re.findall(r'<img src="([^"]+)"', line):
        if "icon" in src:
            continue
        path = src.replace("../../", "") if src.startswith("../../") else f"mods/{mod}/{src}"
        yield f"{RAW}/{path}"


def install_section(mod: str) -> list:
    return [
        "[size=5][b]Installation[/b][/size]",
        "[list=1]",
        f"[*]Install [url={MELONLOADER}]MelonLoader[/url] 0.7.3 (0.7.1 is not supported).",
        "[*]In the [b]Files[/b] tab download the file for your game branch: [b]IL2CPP[/b] for the default Steam branch "
        "(and beta), [b]Mono[/b] for the alternate / alternate-beta branches.",
        f"[*]Extract it into the Schedule I game folder so that [font=Courier New]Mods/{mod}_Il2Cpp.dll[/font] "
        "(or [font=Courier New]_Mono.dll[/font]) is in place. Install only the one matching your branch.",
        "[/list]",
        f"Also available on [url={THUNDERSTORE}/{mod}/]Thunderstore[/url] (mod managers). "
        f"Source code: [url={REPO}/tree/main/mods/{mod}]GitHub[/url].",
        "",
    ]


def to_bbcode(markdown: str, mod: str) -> str:
    lines = markdown.replace("\r\n", "\n").split("\n")
    out = []
    i = 0
    skip_section = False
    list_open = None  # "ul" / "ol"

    def close_list():
        nonlocal list_open
        if list_open:
            out.append("[/list]")
            list_open = None

    while i < len(lines):
        line = lines[i]
        if line.startswith("# "):  # page title
            i += 1
            continue
        heading = re.match(r"(#{2,3}) (.+)", line)
        if heading:
            close_list()
            title = heading.group(2).strip()
            skip_section = title in ("Changelog", "Installation")
            if title == "Installation":
                out.extend(install_section(mod))
            elif not skip_section:
                size = 5 if len(heading.group(1)) == 2 else 4
                out.append(f"[size={size}][b]{title}[/b][/size]")
            i += 1
            continue
        if skip_section:
            i += 1
            continue
        if line.startswith("```"):
            close_list()
            block = []
            i += 1
            while i < len(lines) and not lines[i].startswith("```"):
                block.append(lines[i])
                i += 1
            out.append("[code]" + "\n".join(block) + "[/code]")
            i += 1
            continue
        if "<img" in line or line.strip() in ("<p>", "</p>"):
            close_list()
            out.extend(f"[img]{url}[/img]" for url in image_urls(line, mod))
            i += 1
            continue
        if line.startswith("|"):
            close_list()
            rows = []
            while i < len(lines) and lines[i].startswith("|"):
                cells = [c.strip() for c in lines[i].strip().strip("|").split("|")]
                if not all(re.fullmatch(r":?-+:?", c) for c in cells):
                    rows.append(cells)
                i += 1
            out.append("[list]")
            for cells in rows[1:]:  # first row is the header
                first, rest = cells[0], [c for c in cells[1:] if c]
                first = first if "[b]" in inline(first, mod) else f"[b]{inline(first, mod)}[/b]"
                out.append("[*]" + inline(first, mod) + (" — " + " — ".join(inline(c, mod) for c in rest) if rest else ""))
            out.append("[/list]")
            continue
        item = re.match(r"(\s*)(- |\d+\. )(.*)", line)
        if item:
            kind = "ol" if item.group(2)[0].isdigit() else "ul"
            if list_open != kind:
                close_list()
                out.append("[list=1]" if kind == "ol" else "[list]")
                list_open = kind
            text = item.group(3)
            while i + 1 < len(lines) and lines[i + 1].startswith("  ") and not re.match(r"\s*(- |\d+\. )", lines[i + 1]):
                i += 1
                text += " " + lines[i].strip()
            out.append("[*]" + inline(text, mod))
            i += 1
            continue
        if line.startswith("> "):
            close_list()
            out.append("[quote]" + inline(line[2:], mod) + "[/quote]")
            i += 1
            continue
        if not line.strip():
            close_list()
            if out and out[-1] != "":
                out.append("")
            i += 1
            continue
        # paragraph: join wrapped lines
        close_list()
        text = line.strip()
        while i + 1 < len(lines) and lines[i + 1].strip() and not re.match(r"(#|\||```|- |\d+\. |<|> )", lines[i + 1]):
            i += 1
            text += " " + lines[i].strip()
        out.append(inline(text, mod))
        i += 1
    close_list()
    return "\n".join(out).strip() + "\n"


# --- banner -------------------------------------------------------------------------------------------

def font(name: str, size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(str(FONTS / name), size)


BANNERS = {"banner-1920x1080.png": (1920, 1080), "banner-1300x372.png": (1300, 372)}  # the sizes Nexus asks for


def banner(mod: str, info: dict, title: str, size: tuple) -> Image.Image:
    width, height = size
    wide = width / height > 2.5  # a short strip: the icon sits beside the text instead of above it
    k = 1 if wide else height / 720

    def px(value: float) -> int:
        return round(value * k)

    area_x, icon_size, icon_at = (640, 150, (56, 52)) if wide else (px(470), px(190), (px(70), px(70)))
    text_x, title_y, title_size, title_width = (236, 30, 68, 560) if wide else (px(70), px(300), px(84), px(620))
    line_size, line_step, line_gap = (30, 40, 26) if wide else (px(36), px(48), px(34))
    footer_x, footer_y, footer_size = (58, height - 60, 22) if wide else (px(72), height - px(70), px(24))

    image = Image.new("RGB", (width, height))
    draw = ImageDraw.Draw(image)
    for y in range(height):
        t = y / (height - 1)
        draw.line([(0, y), (width, y)], fill=tuple(int(a + (b - a) * t) for a, b in zip((30, 34, 46), (12, 14, 20))))

    source, box = info["banner"]
    shot = Image.open(ROOT / source).convert("RGB")
    if box:
        shot = shot.crop(box)
    area_w = width - area_x
    scale = max(area_w / shot.width, height / shot.height)
    shot = shot.resize((round(shot.width * scale), round(shot.height * scale)), Image.LANCZOS)
    left = (shot.width - area_w) // 2
    top = (shot.height - height) // 2
    shot = shot.crop((left, top, left + area_w, top + height))
    fade = Image.new("L", (area_w, height), 255)
    fade_draw = ImageDraw.Draw(fade)
    fade_w = px(260)
    for x in range(fade_w):
        fade_draw.line([(x, 0), (x, height)], fill=int(255 * (x / fade_w) ** 1.6))
    image.paste(shot, (area_x, 0), fade)

    icon = Image.open(ROOT / "mods" / mod / "assets" / "icon-512.png").convert("RGBA")
    icon = icon.resize((icon_size, icon_size), Image.LANCZOS)
    image.paste(icon, icon_at, icon)

    while title_size > px(40) and draw.textlength(title, font=font("OpenSans-Bold.ttf", title_size)) > title_width:
        title_size -= 2
    draw.text((text_x, title_y), title, font=font("OpenSans-Bold.ttf", title_size), fill=(255, 255, 255),
              stroke_width=px(3), stroke_fill=(12, 14, 20))
    y = title_y + title_size + line_gap
    for line in info["tagline"].split("\n"):
        draw.text((text_x + px(2), y), line, font=font("OpenSans-SemiBold.ttf", line_size), fill=(205, 214, 228),
                  stroke_width=px(2), stroke_fill=(12, 14, 20))
        y += line_step
    draw.text((footer_x, footer_y), "Schedule I  •  MelonLoader  •  IL2CPP + Mono",
              font=font("OpenSans-Medium.ttf", footer_size), fill=(150, 160, 178),
              stroke_width=px(2), stroke_fill=(12, 14, 20))
    return image


# --- kit ----------------------------------------------------------------------------------------------

def build(mod: str) -> None:
    info = MODS[mod]
    mod_dir = ROOT / "mods" / mod
    version = re.search(r'Version = "([^"]+)"', (mod_dir / "src" / "ModInfo.cs").read_text(encoding="utf-8")).group(1)
    title = info["page_name"].split(" - ")[0]
    out = OUT / mod
    if out.exists():
        shutil.rmtree(out)
    out.mkdir(parents=True)

    (out / "description.bbcode.txt").write_text(to_bbcode((mod_dir / "README.md").read_text(encoding="utf-8"), mod),
                                                encoding="utf-8")
    for name, size in BANNERS.items():
        banner(mod, info, title, size).save(out / name)
    for index, name in enumerate(info["gallery"], 1):
        shutil.copy(ROOT / "docs" / "images" / name, out / f"{index:02d}_{name}")
    for source, name in info.get("extra_gallery", []):
        index = len(info["gallery"]) + 1
        Image.open(ROOT / source).convert("RGB").save(out / f"{index:02d}_{name}", quality=90)

    files = []
    for label, branch in (("IL2CPP", "default Steam branch (and beta)"), ("Mono", "alternate and alternate-beta branches")):
        archive = ROOT / "dist" / f"{mod}-{version}-{label}.zip"
        if archive.exists():
            shutil.copy(archive, out / archive.name)
        else:
            print(f"  warning: {archive.name} not found, run tools/package.ps1")
        short = "default branch" if label == "IL2CPP" else "alternate branch"
        files.append(f"""  File: {archive.name}
    File name:        {title} ({label} - {short})
    File version:     {version}
    Category:         Main Files
    File description: For the {branch}. Requires MelonLoader 0.7.3. Extract into the Schedule I game folder (the DLL goes to Mods/). Install only the file matching your branch.""")

    changelog = (mod_dir / "CHANGELOG.md").read_text(encoding="utf-8").split("## ", 2)[1].split("\n", 1)[1].strip()
    (out / "fields.txt").write_text(f"""NEXUS MODS PAGE — {title}
Game: Schedule I            https://www.nexusmods.com/games/schedule1  ->  Upload  ->  Add a mod

== Mod details ==
Mod name:            {info['page_name']}
Version:             {version}
Category:            {info['category']}
Language:            English
Author:              BbIJABNPOBATEJb
Brief overview / summary ({len(info['summary'])} characters):
{info['summary']}

Detailed description: paste the whole content of description.bbcode.txt
                      (switch the editor to BBCode / source mode first if it has a toggle)

Adult content:       No
Tags (pick the closest ones the form offers): {info['tags']}
AI tag (mandatory on Nexus): AI-Generated Content   (the code was written with an AI assistant; see chat notes)

== Media ==
Banners:             banner-1920x1080.png and banner-1300x372.png (use the one matching the size the field asks for)
Gallery images:      the numbered files in this folder, in order

== Files ==
{chr(10).join(files)}

== Requirements ==
Off-site requirement:
    Name:  MelonLoader
    URL:   {MELONLOADER}
    Notes: 0.7.3 (0.7.1 is not supported)
Nexus requirements / DLC: none

== Permissions and credits ==
Other users' assets:      All the assets in this file belong to the author, or are from free-to-use modder's resources
Upload permission:        You can upload this file to other sites but you must credit me as the creator
Modification permission:  You are allowed to modify my files and release bug fixes or improve on the features, so long as you credit me
Conversion permission:    You can convert this file to work with other games as long as you credit me
Asset use permission:     You are allowed to use the assets in this file without permission as long as you credit me
Commercial / donation:    your choice (the code is MIT licensed)
Credits / author notes:   {info['credits']} Source code (MIT): {REPO}

== Changelog for version {version} (optional field) ==
{changelog}

== After publishing ==
Send the page URL (https://www.nexusmods.com/schedule1/mods/<number>) so future versions can be
uploaded automatically with the API key.
""", encoding="utf-8")
    print(f"{mod} {version}: {out}")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    for name in (sys.argv[1:] or MODS):
        build(name)
