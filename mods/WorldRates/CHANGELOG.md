# Changelog

## 1.1.0 - 2026-10-01

- New **Deals** tab with two frequency rates (Off to ×10):
  - **Deal requests** - how often customers text you asking for a deal.
  - **Dealer orders** - how often customers order from your dealers, i.e. how fast a dealer
    sells the product you give him.
- Order sizes stay as in the base game: ×2 means twice as many orders, not bigger ones. Low rates
  spread orders over several weeks, high rates add order windows during the day and shorten the
  wait between a customer's deals.
- The ×2–×10 presets leave the deal rates alone; "Vanilla ×1" resets them.
- Works on the game's 0.4.7 beta: the income rate of deals was not applied there. The same build
  still runs on 0.4.6.

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
