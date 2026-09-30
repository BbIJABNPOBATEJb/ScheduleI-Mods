"""Builds the fallback fonts embedded into Polyglot.

Schedule I renders UI with Open Sans (several weights) and Caveat. The game's font atlases only
contain Latin glyphs, so Polyglot adds matching-weight fallbacks that also cover Latin Extended,
Cyrillic, Greek and Vietnamese. Static instances are cut from the Google Fonts variable fonts
(SIL OFL 1.1) and subset to keep the mod small.

Usage: python tools/fonts/build_fonts.py   (needs: pip install fonttools)
"""
import io
import pathlib
import urllib.request

from fontTools import subset
from fontTools.ttLib import TTFont
from fontTools.varLib import instancer

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "mods" / "Polyglot" / "fonts"
BASE = "https://raw.githubusercontent.com/google/fonts/main/ofl/"
SOURCES = {
    "opensans": BASE + "opensans/OpenSans%5Bwdth%2Cwght%5D.ttf",
    "opensans-italic": BASE + "opensans/OpenSans-Italic%5Bwdth%2Cwght%5D.ttf",
    "caveat": BASE + "caveat/Caveat%5Bwght%5D.ttf",
}
# (output name, source, axis values)
INSTANCES = [
    ("OpenSans-Light", "opensans", {"wght": 300, "wdth": 100}),
    ("OpenSans-Regular", "opensans", {"wght": 400, "wdth": 100}),
    ("OpenSans-Medium", "opensans", {"wght": 500, "wdth": 100}),
    ("OpenSans-SemiBold", "opensans", {"wght": 600, "wdth": 100}),
    ("OpenSans-Bold", "opensans", {"wght": 700, "wdth": 100}),
    ("OpenSans-MediumItalic", "opensans-italic", {"wght": 500, "wdth": 100}),
    ("OpenSans-SemiBoldItalic", "opensans-italic", {"wght": 600, "wdth": 100}),
    ("OpenSans-BoldItalic", "opensans-italic", {"wght": 700, "wdth": 100}),
    ("Caveat-Regular", "caveat", {"wght": 400}),
]
# Basic Latin + Latin-1, Latin Extended-A/B, IPA/spacing, combining marks, Greek, Cyrillic (+ext),
# Latin Extended Additional (Vietnamese), general punctuation, currency, letterlike, arrows.
UNICODES = (
    "U+0000-024F,U+0250-02FF,U+0300-036F,U+0370-03FF,U+0400-052F,U+1C80-1C8F,U+1E00-1EFF,"
    "U+2000-206F,U+20A0-20CF,U+2100-214F,U+2190-21FF,U+2C60-2C7F,U+A640-A69F,U+A720-A7FF"
)


def fetch(url: str) -> bytes:
    with urllib.request.urlopen(url) as r:
        return r.read()


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    raw = {name: fetch(url) for name, url in SOURCES.items()}
    for name, src, axes in INSTANCES:
        font = instancer.instantiateVariableFont(TTFont(io.BytesIO(raw[src])), axes)
        options = subset.Options()
        options.layout_features = ["*"]
        options.name_IDs = ["*"]
        options.hinting = False
        options.notdef_outline = True
        sub = subset.Subsetter(options)
        sub.populate(unicodes=subset.parse_unicodes(UNICODES))
        sub.subset(font)
        path = OUT / f"{name}.ttf"
        font.save(path)
        print(f"{path.name}: {path.stat().st_size // 1024} KB, {len(font.getBestCmap())} glyphs")
    (OUT / "OFL.txt").write_bytes(fetch(BASE + "opensans/OFL.txt"))
    (OUT / "OFL-Caveat.txt").write_bytes(fetch(BASE + "caveat/OFL.txt"))


if __name__ == "__main__":
    main()
