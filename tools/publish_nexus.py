"""Uploads new versions of the mods' files to their existing Nexus Mods pages (API v3).

Nexus pages cannot be created through the API: make the page by hand first (tools/make_nexus_kit.py
prepares everything for the form), add it to PAGES below, then use this script for every update.

For each mod it uploads dist/<Mod>-<version>-IL2CPP.zip and -Mono.zip (run tools/package.ps1 first) as
a new version of the page's "... (IL2CPP - default branch)" and "... (Mono - alternate branch)" files,
archives the previous version, sets the page's version and adds the changelog entry.

Usage:
    python tools/publish_nexus.py --key-file <api key file> WorldRates Polyglot             # dry run
    python tools/publish_nexus.py --key-file <api key file> --publish WorldRates Polyglot

The API key (Nexus > Site preferences > API keys > Personal API Key) is sent only to api.nexusmods.com,
never to the storage URL the archive itself is uploaded to.
"""
import argparse
import base64
import hashlib
import json
import pathlib
import re
import ssl
import sys
import time
import urllib.error
import urllib.request

import certifi

ROOT = pathlib.Path(__file__).resolve().parents[1]
API = "https://api.nexusmods.com/v3"
GAME = "schedule1"
USER_AGENT = "ScheduleI-Mods-publisher/1.0 (+https://github.com/BbIJABNPOBATEJb/ScheduleI-Mods)"
CONTEXT = ssl.create_default_context(cafile=certifi.where())

# Mod -> the number in its page address, nexusmods.com/schedule1/mods/<number>.
PAGES = {
    "Polyglot": 2748,
    "GuideArrows": 2750,
    "DamageIndicator": 2751,
    "QuietPause": 2752,
    "WorldRates": 2753,
}
# Archive label -> (a word in the name of the page's file it is a new version of, the name's suffix).
RUNTIMES = {"IL2CPP": ("IL2CPP", "IL2CPP - default branch"), "Mono": ("Mono", "Mono - alternate branch")}


class Nexus:
    def __init__(self, key: str):
        self._key = key

    def call(self, method: str, path: str, body=None):
        data = json.dumps(body).encode("utf-8") if body is not None else None
        headers = {"apikey": self._key, "Accept": "application/json", "User-Agent": USER_AGENT}
        if data is not None:
            headers["Content-Type"] = "application/json"
        request = urllib.request.Request(API + path, data=data, method=method, headers=headers)
        try:
            with urllib.request.urlopen(request, context=CONTEXT) as response:
                text = response.read().decode("utf-8")
        except urllib.error.HTTPError as error:
            raise SystemExit(f"{method} {path}: HTTP {error.code}: {error.read().decode('utf-8', 'replace')[:600]}")
        payload = json.loads(text) if text else {}
        return payload.get("data", payload)


def put_archive(url: str, archive: pathlib.Path, md5: bytes) -> None:
    """Sends the archive to the presigned storage URL (no API key here: it is another host)."""
    request = urllib.request.Request(url, data=archive.read_bytes(), method="PUT", headers={
        "Content-Disposition": f'attachment; filename="{archive.name}"',
        "Content-MD5": base64.b64encode(md5).decode("ascii"),
        "Content-Type": "application/octet-stream",
        "User-Agent": USER_AGENT,
    })
    try:
        with urllib.request.urlopen(request, context=CONTEXT) as response:
            response.read()
    except urllib.error.HTTPError as error:
        raise SystemExit(f"upload of {archive.name}: HTTP {error.code}: {error.read().decode('utf-8', 'replace')[:600]}")


def version_of(mod: str) -> str:
    text = (ROOT / "mods" / mod / "src" / "ModInfo.cs").read_text(encoding="utf-8")
    return re.search(r'Version = "([^"]+)"', text).group(1)


def changelog_of(mod: str, version: str) -> str:
    """The entry of this version from the mod's CHANGELOG.md, as plain lines."""
    text = (ROOT / "mods" / mod / "CHANGELOG.md").read_text(encoding="utf-8").replace("\r\n", "\n")
    match = re.search(rf"^## {re.escape(version)}\b[^\n]*\n(.*?)(?=^## |\Z)", text, re.S | re.M)
    if not match:
        raise SystemExit(f"{mod}: CHANGELOG.md has no entry for {version}")
    lines = []
    for raw in match.group(1).strip().split("\n"):
        line = re.sub(r"\*\*(.+?)\*\*", r"\1", raw).replace("`", "")
        if line.startswith(("- ", "  - ")):
            lines.append(line.strip()[2:])
        elif line.strip() and lines:
            lines[-1] += " " + line.strip()   # a wrapped bullet continues
    return "\n".join(lines)


def publish(nexus: Nexus, mod: str, do_publish: bool) -> None:
    version = version_of(mod)
    page = nexus.call("GET", f"/games/{GAME}/mods/{PAGES[mod]}")
    files = nexus.call("GET", f"/mods/{page['id']}/files")["mod_files"]
    changelog = changelog_of(mod, version)
    print(f"{mod} {version} -> '{page['name']}' (nexusmods.com/{GAME}/mods/{PAGES[mod]})")

    plan = []
    title = page["name"].split(" - ")[0]
    for label, (word, suffix) in RUNTIMES.items():
        archive = ROOT / "dist" / f"{mod}-{version}-{label}.zip"
        if not archive.exists():
            raise SystemExit(f"{archive} not found: run tools/package.ps1 -Mods {mod}")
        matches = [f for f in files if word.lower() in f["name"].lower() and f.get("is_active", True)]
        if len(matches) != 1:
            raise SystemExit(f"{mod}: expected one active file with '{word}' in its name, found {[f['name'] for f in files]}")
        plan.append((archive, matches[0], f"{title} ({suffix})"))
        print(f"    {archive.name} ({archive.stat().st_size} bytes) -> new version of '{matches[0]['name']}' as '{title} ({suffix})'")
    print("    changelog:\n" + "\n".join("      " + line for line in changelog.split("\n")))
    if not do_publish:
        return

    for archive, mod_file, name in plan:
        md5 = hashlib.md5(archive.read_bytes())
        upload = nexus.call("POST", "/uploads", {"size_bytes": archive.stat().st_size, "filename": archive.name,
                                                 "md5": md5.hexdigest()})
        put_archive(upload["presigned_url"], archive, md5.digest())
        nexus.call("POST", f"/uploads/{upload['id']}/finalise")
        for _ in range(60):
            if nexus.call("GET", f"/uploads/{upload['id']}")["state"] == "available":
                break
            time.sleep(2)
        else:
            raise SystemExit(f"{archive.name}: the upload did not become available")
        nexus.call("POST", f"/mod-files/{mod_file['id']}/versions", {
            "upload_id": upload["id"],
            "name": name,
            "version": version,
            "file_category": "main",
            "update_mod_version": True,
            "archive_existing_file": True,
        })
        print(f"    uploaded {archive.name}")
    nexus.call("POST", f"/mods/{page['id']}/changelogs", {"version": version, "changelog": changelog})
    print("    changelog added")


def main() -> None:
    sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("mods", nargs="+", choices=sorted(PAGES))
    parser.add_argument("--key-file", required=True, help="file with the Nexus personal API key")
    parser.add_argument("--publish", action="store_true", help="really upload (default: only show what would be done)")
    args = parser.parse_args()
    nexus = Nexus(pathlib.Path(args.key_file).read_text(encoding="utf-8").strip())
    for mod in args.mods:
        publish(nexus, mod, args.publish)
    if not args.publish:
        print("dry run: nothing was uploaded (add --publish)")


if __name__ == "__main__":
    main()
