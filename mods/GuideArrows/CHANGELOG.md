# Changelog

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
