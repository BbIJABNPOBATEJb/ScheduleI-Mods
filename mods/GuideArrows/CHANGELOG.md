# Changelog

## 1.2.1 - 2026-10-03 (Thunderstore page only)

- Screenshots of the outlines on the mod page. The mod itself is the same as 1.2.0 (it still reports
  1.2.0 in game, so players with 1.2.0 from GitHub or Nexus play together as before).

## 1.2.0 - 2026-10-03

- **All buyers and potential customers glow**, not only the ones the arrows point at: yellow and
  purple outlines on everyone within 100 m (the 12 nearest of each). Can be turned off or given another
  range (Look → Outline all buyers and customers, Outline range).
- The arrow of a kind always points at its nearest target. Before, once you came closer than "Hide
  when closer than" to it, the arrow swung round to the next nearest one, which looked as if the
  nearest one was skipped; now that arrow hides while the target keeps glowing.

## 1.1.0 - 2026-10-03

- New **buyers** arrow (yellow): the nearest of your customers who would buy from you right now -
  no deal arranged or offered, not served by one of your dealers and no purchase in the last few
  hours.
- **Outlines**: the people and stashes the arrows point at glow in the arrow's color, visible
  through walls and buildings. They keep glowing after the arrow hides as you walk up. Can be turned
  off (Look → Outline targets).
- The potential customer arrow skips people you cannot offer a sample to right now: those who
  already turned you down today, who are indoors or driving, or who live in a region you have not
  unlocked yet.
- New defaults: arrows stay shown however close you get (Hide when closer than: 0 m) and show
  labels (who or what they point at). Settings you changed from the old defaults are kept.
- Long labels (e.g. a stash's name) no longer run into the next arrow's label.
- Multiplayer: the home base arrow was missing for everyone but the host. Players who joined now
  get it too, and where they last slept is remembered per host's world.

## 1.0.1 - 2026-10-01

- Works on the game's 0.4.7 beta: the home arrow did not remember where you last slept there
  (the game moved its sleep code). The same build still runs on 0.4.6.

## 1.0.0 - 2026-09-30

- First release.
- 3D arrows under the compass to the nearest deal, stash, quest objective, potential customer and home base,
  each in its own color; smooth turning and tilting towards targets above/below.
- Modes: one arrow per kind, nearest only, nearest few. Distance (metric/imperial) and optional labels.
- Home base: where you last slept (remembered per world) or the property with the most valuable equipment.
- Native in-game settings screen (pause menu → Guide Arrows), drag-and-resize layout editor, F7 toggle.
- Supports both IL2CPP (default) and Mono (`alternate`) branches.
