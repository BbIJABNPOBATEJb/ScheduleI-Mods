"""Creates GitHub releases for the mods named on the command line: tag <Mod>-v<version> (version from
ModInfo.cs), the CHANGELOG entry of that version, and dist/<Mod>-<version>-IL2CPP.zip / -Mono.zip
(run tools/package.ps1 first).

Usage:
    python tools/publish_github.py --token-file <file> --tested "<game versions>" GuideArrows Polyglot      # dry run
    python tools/publish_github.py --token-file <file> --tested "..." --publish --latest Polyglot GuideArrows Polyglot

The token (a GitHub token with the repo scope) is only sent to api.github.com and uploads.github.com.
"""
import argparse
import json
import pathlib
import re
import subprocess
import sys
import urllib.error
import urllib.request

REPO = "BbIJABNPOBATEJb/ScheduleI-Mods"
ROOT = pathlib.Path(__file__).resolve().parents[1]
RAW = f"https://raw.githubusercontent.com/{REPO}/main/docs/images/"

INSTALL = """### Installation
1. Install [MelonLoader](https://github.com/LavaGang/MelonLoader/releases) **0.7.3** (0.7.1 is not supported).
2. Download the ZIP for your game branch:
   - `{mod}-{ver}-IL2CPP.zip` — default Steam branch (and `beta`)
   - `{mod}-{ver}-Mono.zip` — `alternate` / `alternate-beta` branches
3. Extract it into the `Schedule I` game folder (the DLL goes to `Mods/`). Don't install both builds.

{tested} Works together with the other mods in this repository."""

INFO = {
    "WorldRates": ("World Rates", "Server-style rates like on Rust servers: multiply **XP, income, deal frequency and storage capacity** — "
                                  "per source, per world, live in a native in-game window (pause menu → **World Rates**, or **F10**).\n\n"
                                  f"![Deals tab]({RAW}worldrates-deals.jpg)"),
    "Polyglot": ("Polyglot", "Play Schedule I in **13 languages** and switch between them instantly — **Settings → Display → Language** or **F9**.\n\n"
                             "English · Русский · Українська · Deutsch · Français · Español · Português (Brasil) · "
                             "Italiano · Polski · Türkçe · 简体中文 · 日本語 · 한국어"),
    "DamageIndicator": ("Damage Indicator", "**Health bars and floating damage numbers** — hidden until someone actually takes damage. "
                                            "Bars over characters, your own movable health bar, a yellow stun bar and KO / DEAD badges.\n\n"
                                            f"![Damage Indicator]({RAW}damageindicator-hit.jpg)"),
    "GuideArrows": ("Guide Arrows", "**3D arrows under the compass** that always point at the nearest thing that needs you, colored by what it is: "
                                    "🟢 deals · 🟡 buyers · 🔴 stashes with items · 🟠 quest objectives · 🟣 potential customers · 🔵 home base. "
                                    "What they point at **glows through walls** in the arrow's color.\n\n"
                                    f"![Guide Arrows]({RAW}guidearrows-glow-wall.jpg)"),
    "QuietPause": ("Quiet Pause", "**Mutes the game in the background** and **pauses world sounds in the pause menu** "
                                  "(menu clicks stay audible, music optional). Options are in the game's own **Settings → Audio** tab."),
    "ElectricScooter": ("Electric Scooter", "A new ride: an **electric scooter** sold at the skate shop for $5,000. It handles like the Golden "
                                            "Skateboard, but **rolls over curbs** instead of stopping at every one of them, **uses no stamina** "
                                            "and is **30% faster**.\n\n"
                                            f"![Electric Scooter]({RAW}electricscooter-ride.jpg)\n\n"
                                            f"![Skate shop]({RAW}electricscooter-shop.jpg)"),
}


def api(token: str, method: str, url: str, data=None, content_type="application/json"):
    assert url.startswith(("https://api.github.com/", "https://uploads.github.com/"))
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Authorization", f"token {token}")
    req.add_header("Accept", "application/vnd.github+json")
    if data is not None:
        req.add_header("Content-Type", content_type)
    try:
        with urllib.request.urlopen(req) as r:
            return json.loads(r.read() or b"{}")
    except urllib.error.HTTPError as e:
        raise SystemExit(f"{method} {url}: {e.code} {e.read().decode('utf-8', 'replace')[:400]}")


def version(mod: str) -> str:
    return re.search(r'Version = "([^"]+)"', (ROOT / "mods" / mod / "src" / "ModInfo.cs").read_text(encoding="utf-8")).group(1)


def changes(mod: str, ver: str) -> str:
    text = (ROOT / "mods" / mod / "CHANGELOG.md").read_text(encoding="utf-8").replace("\r\n", "\n")
    entry = re.search(rf"^## {re.escape(ver)}\b[^\n]*\n(.*?)(?=^## |\Z)", text, re.S | re.M).group(1).strip()
    # GitHub keeps hard line breaks inside list items: join wrapped lines.
    return re.sub(r"\n(?!\s*- )\s+", " ", entry)


def main() -> None:
    sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("mods", nargs="+", choices=sorted(INFO))
    parser.add_argument("--token-file", required=True)
    parser.add_argument("--tested", required=True, help='e.g. "Tested in game on Schedule I 0.4.7f12 (IL2CPP and Mono), MelonLoader 0.7.3."')
    parser.add_argument("--latest", help="the mod whose release becomes the repository's latest release")
    parser.add_argument("--publish", action="store_true", help="really create the releases (default: show them)")
    args = parser.parse_args()
    token = pathlib.Path(args.token_file).read_text(encoding="utf-8").strip()
    commit = subprocess.check_output(["git", "-C", str(ROOT), "rev-parse", "HEAD"], text=True).strip()
    print("commit", commit[:7], "" if args.publish else "(dry run)")
    for mod in dict.fromkeys(args.mods):
        ver = version(mod)
        title, intro = INFO[mod]
        body = (f"{intro}\n\n### {'Changes' if ver != '1.0.0' else 'Features'}\n{changes(mod, ver)}\n\n"
                f"{INSTALL.format(mod=mod, ver=ver, tested=args.tested)}\n\n"
                f"Full description: [mods/{mod}](https://github.com/{REPO}/tree/main/mods/{mod})")
        zips = [ROOT / "dist" / f"{mod}-{ver}-{label}.zip" for label in ("IL2CPP", "Mono")]
        for z in zips:
            if not z.exists():
                raise SystemExit(f"{z} not found: run tools/package.ps1 -Mods {mod}")
        if not args.publish:
            print(f"--- {mod}-v{ver}: '{title} {ver}' latest={mod == args.latest}\n{body}\n")
            continue
        created = api(token, "POST", f"https://api.github.com/repos/{REPO}/releases", json.dumps({
            "tag_name": f"{mod}-v{ver}", "target_commitish": commit, "name": f"{title} {ver}", "body": body,
            "draft": False, "prerelease": False, "make_latest": "true" if mod == args.latest else "false",
        }).encode())
        print("release", created["html_url"])
        for path in zips:
            asset = api(token, "POST", created["upload_url"].split("{")[0] + f"?name={path.name}", path.read_bytes(), "application/zip")
            print("  asset", asset["name"], asset["size"], asset["state"])


if __name__ == "__main__":
    main()
