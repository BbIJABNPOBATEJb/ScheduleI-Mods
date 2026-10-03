# Changelog

## 1.2.1 - 2026-10-03

- Text messages: translations longer than the English no longer run out of their bubbles and over
  the next message. Bubbles are sized to the translated text.
- Translations for the new settings of Guide Arrows 1.1.0 and Damage Indicator 1.0.2.

## 1.2.0 - 2026-10-01

- Ready for the game's 0.4.7 beta: about 240 new strings per language - the item descriptions the
  beta rewrote, new clothing, hairstyles and furniture, and the special customer groups (Bikers,
  Hippies, Businessmen, Party Bus) with their screens and announcements. Game 0.4.6 stays fully
  translated; the same build runs on both versions.
- Translations for the new deal frequency rates of World Rates 1.1.0 and for the Electric Scooter mod.

## 1.1.0 - 2026-09-30

- Phone calls are translated on the default IL2CPP branch too (the game's call text processing is
  compiled inline there; highlighted words are now matched either way).
- Quest steps with a bullet ("• Collect the stash…"), dialogue options' upper-cased reasons
  ("'DOCKS' REGION MUST BE UNLOCKED"), deal steps ("1x OG Kush, Behind the …") and deal titles with
  their "(Begins in 5 min)" subtitle are translated.
- About 150 new strings per language: tutorial hints and quest steps that are never loaded in the
  scenes the corpus was built from (found by reading the game's asset files), the phone clock's
  weekdays, and the settings of Damage Indicator, Guide Arrows and Quiet Pause.
- Japanese: times read "午前7:52" instead of "午前7:52時"; a few leftover English words fixed in several languages.

## 1.0.0 - 2026-09-30

- First public release.
- 12 translations (Russian, Ukrainian, German, French, Spanish, Brazilian Portuguese, Italian,
  Polish, Turkish, Simplified Chinese, Japanese, Korean), about 4,000 strings each.
- Live language switching from Settings → Display and with the F9 hotkey.
- Open Sans / Caveat fallback fonts matching the game's look; Windows system fonts for CJK.
- Automatic fitting of longer translations into fixed-size labels.
- User translation files and untranslated-text log for translators.
- Supports both IL2CPP (default) and Mono (`alternate`) branches.
