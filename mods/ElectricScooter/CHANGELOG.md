# Changelog

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
