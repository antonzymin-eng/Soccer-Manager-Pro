# Movement-system fix — arrival braking and target-speed control

> **Created:** November 12, 2026 · **Version:** 0.1 · **Status:** DRAFT — planned work, not implemented
> **Purpose:** Define a separate Spec #2 movement change that addresses arrival behavior for all
> players, with its own specification amendment and regression pass.
> **Inspected baseline:** `c7ec1131` (PR #545). Code inspection only; no behavioral measurements or
> compile/test results are claimed by this draft.

## Problem and evidence

`MovementCommand` carries a destination and a desired locomotion state, but no explicit speed in
metres per second or arrival policy (`src/agent-movement/MovementCommand.cs`).
`AgentMovementSystem.Update` maps that state to a speed band, caps acceleration at that speed, steers
toward the destination, and integrates position. It does not derive a lower command speed from the
remaining distance. `AgentLocomotion.ApplyDeceleration` already provides braking, but destination
proximity does not itself request that braking.

Consequently, reaching and settling at a positional target is not an explicit shared movement
contract. Overshoot or repeated corrections are plausible consequences; their extent must be
measured before implementation. The marking plan's pace-tier work addresses catching a moving man,
which is a separate concern from arriving and settling
([W13 §4](w-defender-marking-wiring-plan.md#4-design)).

## Recommended scope

Implement arrival control in the shared movement system. Start by specifying distance-aware braking
for commands that intend to arrive at a position, using the existing controlled/emergency braking
profiles. Evaluate an explicit target-speed command if the producer needs to distinguish settling,
following, and running through a point. Select and document the contract before coding; these are
design alternatives, not two mandatory implementations.

The movement layer owns physical braking and attainable speed. Tactical producers own the destination
and action intent. Do not introduce a marking-only slowdown radius, gain, or speed multiplier.
Any necessary new movement parameter belongs to Spec #2, with its source tag, units, range, default,
and evidence recorded under the existing tuning policy.

## Required contract decisions

1. Define which commands settle at their destination and which follow or run through a point.
   Audit every command factory and its callers, including MOVE, HOLD, DRIBBLE, PRESS, INTERCEPT,
   goalkeeper movement, and direct engine submissions. Do not apply a universal stop-at-target rule.
2. Define arrival by both position error and residual speed, with specified tolerances and a bounded
   settling time. Establish numeric limits from the baseline and existing movement constraints before
   implementation; this draft does not invent acceptance thresholds or tuning values.
3. Calculate braking at the 60 Hz movement tick using the held 10 Hz command. Specify behavior for
   destinations inside stopping distance, exact coincidence, lateral approaches, and targets behind
   the player. Preserve acceleration, turning, fatigue, collision, and grounded-state constraints;
   do not snap position or velocity to satisfy an arrival assertion.
4. Define how a moving or newly retargeted destination releases braking and resumes pursuit without
   repeated stop/start behavior. Keep TARGET_LOCK facing independent of travel direction.
5. If an explicit speed is added, specify units (m/s), valid values, zero/default behavior, validation,
   and precedence relative to DesiredState, physical limits, fatigue, and arrival braking. If that
   surface is unnecessary, retain the existing command shape.
6. Inventory every added cross-tick value. Held commands are serialized by
   `MatchEngine.WriteMovementCommand`/`ReadMovementCommand`; any added command or persistent state must
   round-trip, participate in the digest, and follow the owning schema/version policy.

## Delivery sequence

1. Capture the current behavior with stationary, moving, and retargeted destinations. Retain the
   exact source revision, toolchain, attributes, initial state, command cadence, and traces of target
   error, speed, state transitions, and fatigue. Use both teams and mirrored geometry.
2. Draft amendments to Spec #2's command, locomotion, state-transition, integration, constants, and
   test-plan sections. Read the complete owning spec before editing it. Identify affected #8 dispatch
   and #16 snapshot contracts; document numeric acceptance criteria and worked examples for formulas.
3. Land the smallest shared movement implementation with its normative specification changes and
   regression tests. Keep marking-policy changes in their own work item. This draft does not change
   any existing approval status or claim an implementation slot in the numbered W-series.
4. Complete the dedicated validation pass below and retain results at the tested revision. Record
   any intended movement/digest changes explicitly; deterministic replay does not imply unchanged
   match outcomes after a shared movement change.

## Dedicated test pass

| Area | Required evidence |
|---|---|
| Stationary arrival | Walk, jog, and sprint approaches from both rest and motion; short and long distances; controlled and emergency profiles. Assert the specified error, residual-speed, and settling bounds across a hold interval, not a single frame. |
| Approach geometry | Exact target coincidence, destination inside stopping distance, oblique approach, reversal, and mirrored teams. Assert finite values, physical limits, and no sustained orbit or overshoot cycle. |
| Moving targets | Constant-speed following, acceleration, stop, reversal, and 10 Hz retargeting. Assert the specified following error and recovery bounds; distinguish an unreachable target from a controller failure. |
| Existing actions | Exercise the real dispatch path for MOVE/HOLD, marking, PRESS, INTERCEPT/first touch, DRIBBLE, and goalkeeper movement. Verify each action's specified arrival or run-through intent. |
| Physical constraints | Representative attribute extremes and fatigue, exhausted sprint resources, TARGET_LOCK strafing, collisions, and grounded recovery. Verify braking does not bypass these constraints. |
| Replay and restore | Same-seed forward replay and save/restore continuation during braking and retargeting. Cover every added command/state value and default-command compatibility. |
| Repository regression | Run `bash tools/run-tests-local.sh --pr`; retain the result and affected-suite details. A targeted lower-level gate alone is not the repository policy pass. |
| Unity and match behavior | Complete the governing Unity compile/EditMode checks, including AgentMovement and whole MatchEngine coverage appropriate to a shared movement change. Compare representative match traces and performance; use the pinned host for certification claims. |

## Completion criteria

The work is complete when the arrival/speed contract and numeric acceptance criteria are specified,
the shared implementation meets them through production command paths, the dedicated pass has
retained evidence, and replay/restore remains deterministic at the new revision. A marking-specific
constant or a passing marking-only test does not close this work item.

## Version history

| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-11-12 | Initial movement-system work-item draft, grounded in the inspected command, integration, dispatch, and snapshot paths. |
