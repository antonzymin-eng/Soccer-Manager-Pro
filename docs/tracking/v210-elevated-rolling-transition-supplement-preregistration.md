# Ball Physics v2.10 elevated-Rolling transition-census supplement

**Date:** 2026-09-18  
**Status:** FROZEN BEFORE SUPPLEMENTARY RESULT  
**Parent characterization run:** `35395955770` at
`e6223dbe26f9fabad3f5cce00e9f2984bcb7156d`  
**Production baseline:** `33cf81443e2c5ab43d7a1befcb22e6c0cfea7289`  
**Semantic arms, seeds, run length, roster recipe and downstream metrics:** unchanged from
`v210-elevated-rolling-characterization-preregistration.md`.

## Why a supplement is required

The first governing run completed successfully and revealed a measurement-topology defect in the
**direct frequency probe**, not in the semantic arms.

The original probe observes raw `Rolling` state immediately before
`BallPhysicsCore.UpdateBallPhysics`. On shipped v2.10, a moving `Rolling` ball can cross above
`AirborneEnterThreshold` during integration and be reclassified by
`BallStateMachine.UpdateBallState` **inside that same Physics update**. The state is therefore
`Airborne` before the next MatchEngine Physics boundary. A zero pre-Physics elevated-`Rolling`
count cannot distinguish "the broadened rule never fired" from "the broadened rule fired and removed
the state before the next observation."

That makes the original boundary-residency count useful for persistence, but insufficient for the
open issue's requested **transition frequency**.

No first-run downstream metric is discarded or redefined. Its aggregate remains governing for the
originally frozen measurements.

## Supplementary exact transition census

At the same `RunPhysicsPhase` call site, capture the ball immediately before and immediately after
`BallPhysicsCore.UpdateBallPhysics` and add these descriptive counters:

- `rollingPhysicsEntries`: Physics updates that begin in `Rolling`;
- `rollingHeightCrosses`: begin in `Rolling` at/below `AirborneEnterThreshold` and finish above
  the threshold;
- `movingRollingHeightCrosses`: the above with post-Physics speed still
  `>= State.MinVelocity`;
- for those moving crosses, destination counts:
  `Airborne`, `Rolling`, or other.

On the v2.10 arm, `movingCrossPostAirborne` is the direct same-tick frequency of the broadened
height-first semantic at this boundary. On the narrow arm the corresponding moving-cross population
should remain `Rolling` if the counterfactual reconstruction is behaving as defined; this is a
descriptive cross-check, not an acceptance bound.

## Governance

- This supplement changes no production semantic, seed, run length, roster recipe or counterfactual.
- It was defined only because the first result demonstrated that the preregistered observation point
  could not see a same-tick transition it was intended to count.
- The original frozen preregistration remains untouched.
- No numeric result from run `35395955770` was used to set a threshold; the supplement has no
  numeric acceptance threshold.
- Current v2.10 remains contract-correct unless evidence establishes a separate contract defect.
