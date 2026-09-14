# W6 Controlled Ball — Wiring Design

**Date:** 2026-09-14  
**Status:** IMPLEMENTATION / VALIDATION  
**Scope:** Match-engine wiring backlog W6 only. W2 tackle activation remains a separate post-W6 evidence decision.

## Problem

`BallStateType.Controlled` had no production producer even though Ball Physics #1 defines it as the physical state for agent possession. MatchEngine tracked possession only through `_possessingAgentId`, so a claimed ball could remain physically independent of its holder. This was visible most clearly for goalkeeper claims/carries and also made carrier/ball separation an expected condition in the W2 tackle census.

The engine also uses `_possessingAgentId` for restart-taker designation. That is intentionally not the same thing as physical open-play control: at kickoff, free kicks, corners, throw-ins, and goal kicks the ball is placed at the restart location while the selected taker may still be elsewhere.

## Decisions

### KD-W6-1 — Physical control is `BallStateType.Controlled`

A genuine open-play possession grant calls the Ball Physics `SetBallControlled` transition and records the MatchEngine holder. The production grant paths are first-touch control/interception, loose-ball pickup, tackle ball-won, and goalkeeper possession.

Restart-taker designation is explicitly excluded. A restart keeps the placed ball `Stationary`; `_possessingAgentId` there means "designated taker", not "ball physically attached to this agent".

### KD-W6-2 — Keep acquisition geometry with the adjudicating mechanic

Ball Physics `CheckPossession` remains a lower-level 0.5 m / relative-velocity predicate. MatchEngine's existing first-touch and loose-pickup mechanics intentionally use broader, already-tested acquisition geometry (including the 1.0 m reception/pickup domain). W6 does not re-run `CheckPossession` after those mechanics have already adjudicated a successful touch, because doing so would silently shrink the live acquisition domain and reopen the loose-ball bootstrap gap.

### KD-W6-3 — Controlled position is externally driven by the holder

At the end of Physics, after agent movement and the goalkeeper/heading 60 Hz drive, MatchEngine attaches a `Controlled` ball to the current holder.

- Outfield control: x/y follow the holder and z is the canonical ball rest height (foot control).
- Goalkeeper control: x/y follow the keeper while z preserves the actual claim/contact height, bounded below by ball rest height. This avoids inventing a hand-height tuning constant or adding a new serialized offset.

Velocity and spin are zero while controlled. The existing BallState recovery checkpoint is refreshed after external placement.

### KD-W6-4 — Every non-kick physical release exits `Controlled`

Kicks already leave `Controlled` through `BallCollision.ApplyKick`. W6 adds an explicit Ball Physics non-kick release transition (`ReleaseBallControl`) for tackle-loose and the six-second goalkeeper release. The latter places the ball at the keeper's feet before returning it to `Stationary`.

The goalkeeper/heading ball adapter now also clears MatchEngine holder identity when it applies a real kick, matching the pass and shot adapters.

### KD-W6-5 — Restart pseudo-possession is not tackleable

W2's tackle resolver now requires the ball to be physically `Controlled`. A stationary restart ball with a designated taker cannot therefore be challenged as though the remote taker were carrying it.

### KD-W6-6 — No new durable state, schema, or RNG

No field or latch is added. `BallState.State` and `BallState.Position` are already serialized, and the holder id already crosses the MatchContext snapshot boundary. Therefore the snapshot layout/version does not change. Snapshot bytes/digests are expected to change where real possession now correctly has a different state/position; that is a behavioral correction, not a schema-format change.

No RNG stream, reservation, or draw order changes.

## Regression requirements

W6 is not closed until tests prove all of the following:

1. Real loose-ball pickup enters `Controlled` and anchors the ball to the holder.
2. A controlled outfield ball follows the holder during Physics.
3. Restart-taker designation leaves the placed ball `Stationary` at the restart spot.
4. Goalkeeper physical control preserves claim height while following the keeper.
5. Forced-loose staging exits `Controlled` and derives a physical loose-ball state.
6. The six-second goalkeeper backstop exits `Controlled`, drops the ball to foot height, and arms the re-collect cooldown.
7. Ball Physics' direct Controlled entry/non-kick release transition preserves recovery checkpoints.
8. Existing first-touch, possession-bootstrap, goalkeeper, tackle, snapshot/restore, determinism, and full functional gates remain green.

## W2 follow-up boundary

W6 removes the carrier/ball-drift blocker for W2 but does **not** change `TackleContactRadiusM` from its governed disabled value. After W6 is green, rerun the armed tackle corpus/composed-match evidence and decide W2 activation separately. This prevents a state-model repair from smuggling in an unmeasured balance change.
