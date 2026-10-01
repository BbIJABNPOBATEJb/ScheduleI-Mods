# World Rates

<img src="assets/icon-512.png" width="128" align="right">

Server-style rates for **Schedule I**, like the "x2 / x3 / x10" servers in Rust: multiply XP,
income, deal frequency and storage capacity — per source, per world, from a native in-game window,
with no restart.

<p>
  <img src="../../docs/images/worldrates-experience.jpg" width="49%">
  <img src="../../docs/images/worldrates-presets.jpg" width="49%">
</p>
<p>
  <img src="../../docs/images/worldrates-deals.jpg" width="49%">
  <img src="../../docs/images/worldrates-storage-x2.jpg" width="49%">
</p>
<p>
  <img src="../../docs/images/worldrates-pause-menu.jpg" width="40%">
</p>

## Features

| Tab | Rates |
|---|---|
| **Experience** | *All experience*, plus per source: your deals, dealer deals, free samples, counter-offers, harvesting, new mixes, quests, escaping the police, graffiti, pickpocketing, other |
| **Income** | *All income*, plus per source: your deals (bonuses included), dealer sales, laundering, pawn shop, recycling |
| **Deals** | *Deal requests* — how often customers text you asking for a deal; *Dealer orders* — how often customers order from your dealers, i.e. how fast a dealer sells the product you give him. Off–×10 |
| **Storage** | Placed storage (racks, shelves, safes, tables, briefcases…) and your own vehicles' trunks, ×1–×4 up to 20 slots |
| **Presets** | Vanilla ×1 · All rates ×2 · ×3 · ×5 · ×10 · "use these rates for new worlds" |

- Per-source rates multiply with the *All* rate of their tab; the window shows the effective value,
  e.g. `×2 (×6)`. Rates range from 0 (off) to ×10.
- Changes apply instantly. Every world keeps its own settings.
- Deal payments are scaled before the game uses them, so the deal popup, bonuses, dealer cuts and
  the daily summary all show the boosted amounts.
- Deal rates change how often customers order, not how much: order sizes stay as in the base game,
  so ×2 is twice as many orders. "Off" stops new requests; running deals are not touched. The
  ×2–×10 presets leave these two rates alone.
- Storage is resized live. Items are never deleted: shrinking only removes empty slots at the end,
  and loading a save grows a storage back to the size its items need.
- The window is built from the game's own Settings screen (tabs, sliders, tooltips), so it looks and
  navigates like the rest of the game. Works with [Polyglot](../Polyglot) — the window is translated
  into all its languages:

  <img src="../../docs/images/worldrates-ru.jpg" width="49%">

## Installation

**With a mod manager** (r2modman, Thunderstore App): install [WorldRates](https://thunderstore.io/c/schedule-i/p/BbIJABNPOBATEJb/WorldRates/) for the default Steam branch or [WorldRates_Mono](https://thunderstore.io/c/schedule-i/p/BbIJABNPOBATEJb/WorldRates_Mono/) for the `alternate` branch. Also on [Nexus Mods](https://www.nexusmods.com/schedule1/mods/2753).

**Manually:**

1. Install [MelonLoader](https://github.com/LavaGang/MelonLoader/releases) 0.7.3 (0.7.1 is not supported).
2. Download `WorldRates-<version>-IL2CPP.zip` (default Steam branch) or `WorldRates-<version>-Mono.zip`
   (`alternate` branch) from [Releases](https://github.com/BbIJABNPOBATEJb/ScheduleI-Mods/releases).
3. Extract it into the game folder so that `Mods/WorldRates_Il2Cpp.dll` (or `_Mono.dll`) is in place.

Only install the build matching your game branch.

## Usage

Open **Pause menu → World Rates** or press **F10** while in a world.

## Configuration

`UserData/MelonPreferences.cfg`:

```ini
[WorldRates]
WindowHotkey = "F10"   # opens the window; "None" to disable
DebugLog = false       # log every scaled XP gain
```

Rates are stored per world in `UserData/WorldRates/worlds/<steam id>_<save slot>.cfg`
(plain `id=value` lines); new worlds start from `UserData/WorldRates/defaults.cfg`, which the
*"Use these rates for new worlds"* button writes.

## Multiplayer

Rates are applied by the host; for other players the window is read-only. Settings are not synced to
clients yet, and XP from a few actions that run on a client's machine (e.g. a client's pickpocketing)
is not scaled.

## Uninstalling

Set **Storage** back to ×1 and take items out of the extra slots first — without the mod the game
only loads its own slot count, so items in the extra slots would be lost.

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## Credits

Developed with AI assistance (Claude by Anthropic) and tested in game on both branches with automated in-game tests.

## License

MIT.
