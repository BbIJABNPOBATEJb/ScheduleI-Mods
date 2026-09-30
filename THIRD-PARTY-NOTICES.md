# Third-party notices

## Fonts (embedded in Polyglot)

| Font | Source | License |
|---|---|---|
| Open Sans (Light, Regular, Medium, SemiBold, Bold, Medium/SemiBold/Bold Italic) | [google/fonts](https://github.com/google/fonts/tree/main/ofl/opensans) | SIL Open Font License 1.1 — [`mods/Polyglot/fonts/OFL.txt`](mods/Polyglot/fonts/OFL.txt) |
| Caveat Regular | [google/fonts](https://github.com/google/fonts/tree/main/ofl/caveat) | SIL Open Font License 1.1 — [`mods/Polyglot/fonts/OFL-Caveat.txt`](mods/Polyglot/fonts/OFL-Caveat.txt) |

The fonts are static instances cut from the variable fonts and subset to Latin, Latin Extended,
Cyrillic, Greek and Vietnamese by [`tools/fonts/build_fonts.py`](tools/fonts/build_fonts.py).
Neither font declares a Reserved Font Name, so the subsets keep their original family names.

## Runtime dependencies (not included)

- [MelonLoader](https://github.com/LavaGang/MelonLoader) (Apache-2.0) and the Harmony/Il2CppInterop
  libraries it ships.
- Schedule I by TVGS. No game files or extracted game assets are part of this repository; the
  translation files contain the game's English UI strings as lookup keys, as usual for translation mods.
