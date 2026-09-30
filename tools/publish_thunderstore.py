"""Publishes packages built by tools/package.ps1 to Thunderstore (community "schedule-i").

    python tools/publish_thunderstore.py --token-file <file with tss_... token> [--dry-run] [packages...]

Packages are dist/thunderstore/<Name>-<version>.zip; with no names given, every zip there is
published. The team is the one the service-account token belongs to. Uses the same REST flow as
Thunderstore CLI: initiate upload -> PUT parts to presigned URLs -> finish upload -> submit.
The token is only ever sent to thunderstore.io, never to the presigned storage URLs.

Needs: pip install certifi (Python's bundled CA list may lack the current Let's Encrypt roots).
"""
import argparse
import json
import pathlib
import ssl
import sys
import urllib.error
import urllib.request
import zipfile

import certifi

API = "https://thunderstore.io"
COMMUNITY = "schedule-i"
UA = "ScheduleI-Mods-publisher/1.0 (+https://github.com/BbIJABNPOBATEJb/ScheduleI-Mods)"
CTX = ssl.create_default_context(cafile=certifi.where())
ROOT = pathlib.Path(__file__).resolve().parents[1]

# Extra categories per package name (IL2CPP / Mono are added from the name).
EXTRA_CATEGORIES = {"QuietPause": ["audio"]}


def request(method, url, token=None, body=None, raw=None, headers=None):
    data = json.dumps(body).encode() if body is not None else raw
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("User-Agent", UA)
    if token:
        if not url.startswith(API + "/"):
            raise ValueError("refusing to send the token outside thunderstore.io")
        req.add_header("Authorization", f"Bearer {token}")
    if body is not None:
        req.add_header("Content-Type", "application/json")
    for k, v in (headers or {}).items():
        req.add_header(k, v)
    try:
        with urllib.request.urlopen(req, context=CTX) as r:
            content = r.read()
            parsed = json.loads(content) if content[:1] in (b"{", b"[") else content
            return r.status, r.headers, parsed
    except urllib.error.HTTPError as e:
        raise RuntimeError(f"{method} {url.split('?')[0]} -> {e.code}: {e.read().decode('utf-8', 'replace')[:500]}") from None


def categories_for(name):
    runtime = "mono" if name.endswith("_Mono") else "il2cpp"
    base = name[:-5] if name.endswith("_Mono") else name
    return ["mods", runtime] + EXTRA_CATEGORIES.get(base, [])


def publish(path, token, team, dry_run):
    manifest = json.loads(zipfile.ZipFile(path).read("manifest.json").decode("utf-8-sig"))
    name, version = manifest["name"], manifest["version_number"]
    cats = categories_for(name)
    print(f"{team}-{name}-{version}  categories={cats}  ({path.stat().st_size} bytes)")
    if dry_run:
        return
    size = path.stat().st_size
    _, _, init = request("POST", f"{API}/api/experimental/usermedia/initiate-upload/", token,
                         {"filename": path.name, "file_size_bytes": size})
    uuid = init["user_media"]["uuid"]
    content = path.read_bytes()
    parts = []
    for part in init["upload_urls"]:
        chunk = content[part["offset"]:part["offset"] + part["length"]]
        _, headers, _ = request("PUT", part["url"], raw=chunk)  # presigned: no token
        parts.append({"ETag": headers["ETag"], "PartNumber": part["part_number"]})
    request("POST", f"{API}/api/experimental/usermedia/{uuid}/finish-upload/", token, {"parts": parts})
    _, _, result = request("POST", f"{API}/api/experimental/submission/submit/", token, {
        "author_name": team,
        "communities": [COMMUNITY],
        "community_categories": {COMMUNITY: cats},
        "has_nsfw_content": False,
        "upload_uuid": uuid,
    })
    version_info = result.get("package_version", {})
    print(f"  published {version_info.get('full_name', name)}  "
          f"https://thunderstore.io/c/{COMMUNITY}/p/{team}/{name}/")


def latest_packages(folder):
    """The newest zip of every package in the folder (older builds may still lie around)."""
    newest = {}
    for path in folder.glob("*.zip"):
        name, _, version = path.stem.rpartition("-")
        key = tuple(int(x) for x in version.split("."))
        if name not in newest or key > newest[name][0]:
            newest[name] = (key, path)
    return [path for _, path in sorted(newest.values(), key=lambda v: v[1].name)]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--token-file", required=True)
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("packages", nargs="*", help="zip file names in dist/thunderstore (default: all)")
    args = parser.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")

    token = pathlib.Path(args.token_file).read_text(encoding="utf-8").strip()
    _, _, me = request("GET", f"{API}/api/experimental/current-user/", token)
    if len(me.get("teams", [])) != 1:
        raise SystemExit(f"expected exactly one team for this token, got {me.get('teams')}")
    team = me["teams"][0]

    folder = ROOT / "dist" / "thunderstore"
    paths = [folder / p for p in args.packages] if args.packages else latest_packages(folder)
    for path in paths:
        publish(path, token, team, args.dry_run)


if __name__ == "__main__":
    main()
