# Changelog

## 1.0.1 - 2026-09-30

- Fixed a crash (stack overflow) on the default IL2CPP branch when XP was awarded outside your own
  actions, e.g. when a dealer completed a deal. XP is now scaled at a single safe point.
- The "Storage capacity updated" log line no longer repeats while a slider is being dragged.
- Plays nicely with other mods that add their own settings screens to the pause menu.

## 1.0.0 - 2026-09-30

- First public release.
- Experience rates: all experience plus 11 sources (deals, dealer deals, samples, counter-offers,
  harvesting, new mixes, quests, police escapes, graffiti, pickpocketing, other).
- Income rates: all income plus your deals, dealer sales, laundering, pawn shop, recycling.
- Storage capacity for placed storage and player vehicle trunks (×1–×4, up to 20 slots), resized
  live; items are never deleted.
- Presets (×1, ×2, ×3, ×5, ×10) and default rates for new worlds.
- Native in-game window (pause menu → World Rates, F10), per-world settings.
- Supports both IL2CPP (default) and Mono (`alternate`) branches.
