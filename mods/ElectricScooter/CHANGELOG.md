# Changelog

## 1.3.0 - 2026-11-10

- New option `OffRoad` (off by default): the scooter rides over grass and gravel at full speed
  instead of slowing down there like the skateboards. Thanks to oOArachneOo for the idea.

## 1.2.0 - 2026-11-02

- A new riding pose by RebornReaper, ported from his ElectricScooter add-on: the rider stands
  upright, one foot ahead of the other, with the palms on the grips and the fingers round them.
  The handlebar comes up to the hands instead of the rider crouching down to it, and the pose stays
  still while you ride: no pushing, crouching or jumping animation.
- Multiplayer: players who have the mod now see your scooter, in your hands and under your feet,
  and your riding pose. Before, they saw a Golden Skateboard.
- Skateboard wheels are hidden more thoroughly, also those outside the skateboard's trucks.

## 1.1.0 - 2026-10-31

- Fixed a short stutter every 3 seconds on big saves: the mod kept looking for the skate shop's
  seller through every loaded object. It now looks once per loaded save. Thanks to RebornReaper for
  tracking it down.
- The rider stands facing forward with both feet on the deck and both hands on the handlebar,
  instead of sideways like on a skateboard.
- No more kicking the road to speed up: the motor does the pushing.
- Getting on no longer makes the scooter jump up and drop (it was lifted twice). With a high
  `StepHeightCm` the drop could throw you off.
- A price changed in the preferences while playing applies the next time a save is loaded.

## 1.0.0 - 2026-10-01

- First release.
- Electric Scooter item sold at the skate shop for $5,000; kept in saves like any other item.
- Rides like the Golden Skateboard, 30% faster, without using stamina.
- Rolls over curbs and steps up to 20 cm instead of stopping at them.
- Own model (big wheels, steering column, handlebar, headlight, battery), folded when carried;
  the rider holds the handlebar.
- Price, speed, step height and stamina use are configurable.
- Supports both IL2CPP (default) and Mono (`alternate`) branches.
