# W4 — Keeper perception wiring

> **Created:** September 11, 2026  
> **Status:** IMPLEMENTATION IN PROGRESS  
> **Authority:** `docs/tracking/match-engine-wiring-backlog.md` W4 correction (September 10, 2026)

## 1. Problem

The existing keeper save trigger is pure flight geometry. A keeper therefore reacts identically to a
clear shot and to a shot hidden behind bodies. W4 adds physical line-of-sight to save availability
without changing ordinary Stage-0 perception semantics.

The corrected W4 backlog also requires one save predicate across the Decision Tree and rush exclusion,
and an explicit signal that restarts reaction timing when a body deflection changes the live ball
flight.

## 2. Locked decisions

### KD-W4-1 — Keeper LOS is not `FilteredView.BallVisible`

`BallVisible` folds range, FoV, and occlusion together. W4 needs only physical occlusion. Keeper facing
can be stale under the current AUTO_ALIGN contract, so using `BallVisible` would incorrectly blind an
unobstructed keeper.

`KeeperPerceptionGate.SaveAvailable` therefore composes:

1. the existing `GkHeadingIntentSource.SaveArmed` flight geometry; and
2. keeper-specific all-body LOS from `OcclusionFilter.IsOccludedByAnyAgent`.

### KD-W4-2 — Friendly screens are keeper-specific

Ordinary `OcclusionFilter.IsOccluded` remains opponent-only at Stage 0 (OQ-1). W4 adds a separate
all-body query. A defender may unsight his own keeper without silently changing what every outfielder
can perceive.

### KD-W4-3 — One save predicate owns save/rush mutual exclusion

Both production sites must consume `KeeperPerceptionGate.SaveAvailable`; neither site may reproduce
flight geometry or occlusion locally. This preserves the ERR-011-007 rule that a ball treated as a save
threat cannot simultaneously arm a keeper rush.

The gate reads current `AgentState[]` directly. It does not depend on the previous perception heartbeat,
so it does not inherit the current ~100 ms filtered-view staleness. No reorder of general agent
perception is required by this implementation; the keeper LOS query is synchronous current-world
perception.

### KD-W4-4 — A deflection signal means an applied Ball Physics response

`BallCollisionHandler.OnAgentCollision` returns true only when
`BallCollision.ApplyAgentDeflection` actually applies a response. `CollisionEvent.BallDeflected`
propagates that fact. Controlled-ball contact, slow first-touch territory, separating overlap, and a
degenerate contact remain false.

This distinction is deterministic and requires no new RNG or cross-tick state.

### KD-W4-5 — Deflection reaction reset

The MatchEngine collision consumer will treat `AGENT_BALL && BallDeflected` as a new keeper threat
anchor. The event occurs after Ball Physics has already changed the live velocity, so the consumer must
use the post-deflection speed and the event timestamp.

Reset rule:

- before a dive is committed, the deflection overwrites the current reaction anchor because the redirected
  flight is a new threat;
- once the keeper is already in the committed dive/airborne chain, the existing attempt is not torn down;
  its frozen reaction score remains authoritative;
- the next save-availability evaluation uses the redirected flight and current all-body LOS;
- an unchanged-flight contact (`BallDeflected == false`) must not restart reaction timing.

No extra serialized flag is required: `BallDeflected` is event-local; Goalkeeper Mechanics' existing
reaction timing fields are already in its snapshot state.

## 3. Implementation slices

### Landed on `codex/w4-keeper-perception`

- `OcclusionFilter.IsOccludedByAnyAgent` — keeper-specific all-body LOS; ordinary OQ-1 path unchanged.
- allocation-free complete-agent LOS overload for the match-engine hot path.
- `KeeperPerceptionGate.SaveAvailable` — single pure save predicate.
- `BallCollisionHandler.OnAgentCollision` returns applied-deflection truth.
- `CollisionEvent.BallDeflected` and `CollisionSystem` propagation.
- focused regression tests for friendly screening, Stage-0 semantic isolation, clear/off-line shots, and
  true-vs-false deflection signalling.

### Remaining before W4 can be marked complete

1. Replace both `GkHeadingIntentSource.SaveArmed` production calls in `MatchEngine` with
   `KeeperPerceptionGate.SaveAvailable`.
2. Route `CollisionEvent.BallDeflected` through the match-flow collision consumer into the keeper
   reaction-reset seam using the post-deflection velocity and event time.
3. Add engine-level regression tests proving:
   - a friendly screen suppresses SAVE;
   - the same screened shot does not arm RUSH;
   - clearing the screen re-enables SAVE;
   - a real deflection restarts pre-commit reaction timing;
   - an unchanged-flight contact does not;
   - snapshot replay around the deflection remains deterministic.
4. Only then update the wiring backlog status/evidence row.

## 4. Snapshot / determinism impact

The all-body LOS calculation is pure and reads current world state only. The collision deflection flag is
per-event observation state. Neither adds a new cross-tick field. The eventual reaction reset writes only
Goalkeeper Mechanics state that is already captured/restored. Therefore W4 should not require a snapshot
schema bump unless the final consumer introduces additional persistent state (which this design forbids).
