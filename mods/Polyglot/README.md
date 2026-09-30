# Polyglot

Play **Schedule I** in 13 languages and switch between them instantly, in game:
English, Русский, Українська, Deutsch, Français, Español, Português (Brasil), Italiano, Polski,
Türkçe, 简体中文, 日本語, 한국어.

- **Settings → Display → Language** (the game's own dropdown), or **F9** to cycle languages.
- Menus, HUD, phone apps, items, quests, dialogue, phone calls, hints and notifications are translated.
- Cyrillic, Latin Extended and Greek use the same Open Sans font as the game; Chinese, Japanese and
  Korean use Windows system fonts (set `CustomFontPath` in `UserData/MelonPreferences.cfg` on Linux).
- Longer translations are auto-fitted into the game's fixed-size labels.
- The game keeps its English text internally, so game logic is never affected.

## Your own translations

Put `.txt` files into `UserData/Polyglot/languages/<code>/` (they override built-in entries; a new code
adds a new language). Format:

```
@name=Русский
Continue=Продолжить
Day {0}=День {0}                              {0}, {1}… stand for numbers
It'll cost <PRICE>.=Это будет стоить <PRICE>.  game placeholders are re-inserted
r:"^Sold (.+) to (.+)$"=Продано: $1 → $2       regex rules
```

With `LogUntranslated = true` the mod collects untranslated texts into
`UserData/Polyglot/untranslated_<code>.txt`, ready to fill in.

Fonts: Open Sans and Caveat, SIL Open Font License 1.1 (see OFL.txt).
Built for MelonLoader 0.7.3+, IL2CPP (default branch) and Mono (`alternate` branch).
