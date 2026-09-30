# World Rates

Server-style rates for **Schedule I**, like "x2 / x3 / x10 servers" in Rust — configured per world,
in game, without restarting.

## What you can tune

| Tab | Rates |
|---|---|
| Experience | All experience + per source: your deals, dealer deals, free samples, counter-offers, harvesting, new mixes, quests, escaping police, graffiti, pickpocketing, other |
| Income | All income + per source: your deals (incl. bonuses), dealer sales, laundering, pawn shop, recycling |
| Storage | Placed storage (racks, shelves, safes, tables…) and your vehicles' trunks, ×1–×4, up to 20 slots |
| Presets | Vanilla ×1, All rates ×2 / ×3 / ×5 / ×10, "use these rates for new worlds" |

Per-source rates multiply with the "All" rate of their tab (the window shows the effective value).

## How to use

- Pause menu → **World Rates**, or press **F10** in game (key configurable in `UserData/MelonPreferences.cfg`).
- Changes apply immediately. Settings are saved per world in `UserData/WorldRates/worlds/`,
  new worlds start from `UserData/WorldRates/defaults.cfg`.

## Good to know

- Deal payments are scaled before the game uses them, so the deal popup, bonuses, dealer cuts and
  daily summary all show the boosted amounts.
- Storage never deletes items: shrinking only removes empty slots at the end, and loading a save
  grows a storage back to the size its items need. **Before uninstalling**, set storage to ×1 and
  empty the extra slots — without the mod the game only loads its own slot count.
- Multiplayer: rates are applied by the host; the window is read-only for other players.
  Some XP events that happen on a client's machine are not scaled yet.
- Works with the Polyglot language mod (the window is translated into all its languages).

Built for MelonLoader 0.7.3+, IL2CPP (default branch) and Mono (`alternate` branch).
