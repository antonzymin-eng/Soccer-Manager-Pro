# Movement-system fix — arrival braking and target-speed control

> **Created:** November 12, 2026 · **Version:** 0.3 · **Status:** DRAFT — planned work, not implemented
> **Purpose:** Plan shared arrival settling and speed control, with explicit layer ownership,
> specification changes, and a dedicated regression pass.
> **Planning branch baseline:** `a64c8169` (runtime unchanged from `c7ec1131`, PR #545).
> **W13 implementation located:** `80d60dc1` on `claude/great-sagan-rpcmhp`; inspected and tested in
> an isolated worktree. Retained results and limits: [baseline review](evidence/movement-arrival-braking-20261112/README.md).

## Problem and evidence

`MovementCommand` carries a destination and a desired locomotion state, but no explicit speed in
metres per second or arrival policy (`src/agent-movement/MovementCommand.cs`).
`AgentMovementSystem.Update` maps that state to a speed band, caps acceleration at that speed, steers
toward the destination, and integrates position. It does not derive a lower command speed from the
remaining distance. `AgentLocomotion.ApplyDeceleration` already provides braking, but destination
proximity does not itself request that braking.

`ActionDispatcher.DispatchMove` always requests WALKING below the 6 m jog threshold. It has no
arrival-stop branch or pace-tier hysteresis. Spec #8 §3.5.6 explicitly prescribes that walking band.
Spec #2 §3.4.6 assigns slowing **for turns** to AI; this supports AI-owned path decisions but does not
explicitly prescribe arrival ownership. Spec #2 §3.2.5 already supports AI-requested intentional stops.
Its FR-3 revision note also says AI path planning must account for braking zones. That supports
AI-owned planning, but an accurate physical stopping contract is a prerequisite for either owner.

### Authoritative W13 follow-ups and evidence scope

W13 owns the issue status and closure evidence. This plan defines shared movement work; it does not
create a second blocker register. Follow the owning records on `claude/great-sagan-rpcmhp`:

| Existing follow-up | Owning W13 record | Relationship to this work |
|---|---|---|
| W13-F5 | [Goal-bound runner collision](https://github.com/antonzymin-eng/Soccer-Manager-Pro/blob/claude/great-sagan-rpcmhp/docs/tracking/open-issues.md#w13-goalward-runner-collision) | Collision/closing-velocity investigation; arrival control alone is not assumed to resolve it. |
| W13-F6 | [Pace-gate churn and guard locks](https://github.com/antonzymin-eng/Soccer-Manager-Pro/blob/claude/great-sagan-rpcmhp/docs/tracking/open-issues.md#w13-gate-flapping-guard-locks) | Explicit-speed control. Current ordinary-runner evidence demonstrates state churn and locks while maintaining goal-side position and a bounded gap; gameplay cost is not established. |
| W13-F7 | [Stationary-target orbit](https://github.com/antonzymin-eng/Soccer-Manager-Pro/blob/claude/great-sagan-rpcmhp/docs/tracking/open-issues.md#w13-stationary-man-orbit) | Arrival settling and a valid stopping prediction. |
| W13-F8 | [Tight stand-off](https://github.com/antonzymin-eng/Soccer-Manager-Pro/blob/claude/great-sagan-rpcmhp/docs/tracking/open-issues.md#w13-tight-standoff-body-contact) | Marking policy; defer the floor/calibration decision until the mirror-asymmetry investigation below is resolved. |

The existing records also appear in W13's `match-engine-wiring-backlog.md`, Spec #8 §3.5 known
limitations, and Spec #14 §3.13.1. Those specifications on the W13 branch are the references for
its implemented contracts; the planning branch at `a64c8169` predates that wiring.

The retained rerun executes W13's own deterministic `[Explicit]` descriptive tests. Matching output
establishes repeatability of that implementation and rig, **not independent confirmation** of their
validity or the causal explanations. The added observer records the same rig at physics cadence.
See the [evidence review](evidence/movement-arrival-braking-20261112/README.md) for measurements and
limits rather than copying those measurements into a second live issue record.

Dedicated review follow-ups are owned on W13 as well:

- [W13-F9 — stale tackle requests after demotion](https://github.com/antonzymin-eng/Soccer-Manager-Pro/blob/claude/great-sagan-rpcmhp/docs/tracking/open-issues.md#w13-stale-tackle-after-demotion): promotes the existing ERR-014-010 known limitation; the actual #14 tick reproduces a request/final-assignment mismatch. Requests are **generated before** enforcement and exposed afterward without reconciliation; this is not proof of publication before enforcement. The defect-log record tracks the contract violation and future fix.
- [W13-F10 — home/away mirror asymmetry](https://github.com/antonzymin-eng/Soccer-Manager-Pro/blob/claude/great-sagan-rpcmhp/docs/tracking/open-issues.md#w13-mirror-asymmetry): unresolved differences block using these observations to choose or calibrate the tight-marking floor. Candidate explanations include rig inputs, attributes/identity, float/state thresholds, and collision/RNG processing order; none is confirmed.

Before comparing churn severity with failure to settle or knockdown, measure its gameplay cost:
response delay to a changed target, catch-up/escape, marking error, fatigue, and contact outcomes
under a controlled comparison. The current ordinary pursuit cases do not establish worse marking.
For goal-bound cases, distinguish speed **caps** from speed at contact: both ramps are identical up
to knockdown, so duplicate grounded counts do not establish a runner-speed response.

### Stopping-distance dependency — shared by both arrival designs

`Stop` disables voluntary steering; it does not remove velocity. At the default walking cap of
2.2 m/s, the existing controlled-braking profile is floor-limited at 2.5 m/s². Opus's continuous
estimate is therefore useful: `d = v² / (2a)` [DERIVED], with nonnegative speed `v` in m/s, constant positive
deceleration `a` in m/s², and path distance `d` in m. For `v = 2.2`, `a = 2.5`, `d = 0.968 m`.
Before the next 10 Hz command decision, phase delay `τ` in [0, 0.1) s can add `vτ` [DERIVED], up to
approximately 0.220 m at that speed. The combined estimate is about 1.19 m if the stop condition is
first detected at the destination. Inputs are [CROSS] to #2's `JogEnter`/`MinDecelerationFloor` and
the 10 Hz tactical cadence; these are calculations using defaults, not new tuning parameters.

This is an illustrative straight-line, constant-speed-before-braking calculation, not a measured
60 Hz stopping distance or a universal overshoot bound. The discrete velocity-first integrator,
state transitions, a pre-existing guard lock, collisions, and moving targets can change the result.
Distance travelled by the marker is also not necessarily error relative to a moving mark point.

The AR-12 deviation note in `AgentLocomotion.ApplyDeceleration` explicitly records that the code
recomputes braking from current speed each frame, with a floor and acceleration cap, whereas
Spec #2 §3.2.5 describes constant-rate braking. `CalculateStoppingDistance` returns a profile input,
not a verified prediction of full-pipeline stopping distance. FR-3's 3–5 m figures apply from
**sprint speed**; they must not be compared directly with this walking example.

Before selecting or implementing arrival ownership, reconcile that contract: either specify and
validate a prediction for the current discrete pipeline, or resolve the braking deviation with
its physical caps and amend the predictor accordingly. Amend #2's braking/AI-facing prediction
contract and test plan in either case; verify predictions against the real integrator across speeds,
attributes, modes, cadence phases, and relevant state/lock conditions. Do not export the nominal
profile distance to AI as though it were an accurate stopping prediction.

## Recommended scope

Arrival ownership is an **open design fork**. Both alternatives depend on the stopping-contract work
above and have a dedicated test pass; the draft does not prefer one before the baseline is assessed.

| Alternative | Spec #2 changes | Spec #8 changes |
|---|---|---|
| Decision-tree-owned arrival | Reconcile the braking deviation and specify a validated stopping prediction usable by AI. Preserve physics execution constraints and define prediction validity. | Amend §3.5.6's walking-only near band: request braking early enough, define settling/restart hysteresis and action-specific intent, and persist any added controller memory. |
| Movement-system-owned arrival | Reconcile the same braking contract, then specify arrival intent, distance-aware control, execution order, and interactions with turn limits, fatigue, states, and collisions. | Specify which dispatched commands request settling, following, or run-through behavior and how the existing distance tiers combine with arrival/speed intent. Avoid a second competing arrival controller. |

**Moving-target work is shared by either alternative.** Fold W13.1c into an explicit Spec #2
target-speed command/integration contract, with #8/#14's producer contracts. `MarkTargetSpeed` is
a tactical input, not a movement-command speed field; neither tier selection nor its speed gate
can request an exact 2.6 m/s. Specify attainable matching speed plus position-error recovery, since
matching the man's speed alone cannot close an existing gap. A tier change that removes one kind
of flicker must not be accepted without checking overshoot, repeated braking, and guard locks.

Goal-bound contact and stale tackle requests remain marking/collision work with their own regression
cases. A contact-aware tight-marking floor is a separate #14 policy amendment: derive it from both
body radii plus a justified clearance margin and test it under motion. Do not treat choosing a floor
as equivalent to resolving the observed loss of goal-side position.

The movement layer owns physical braking and attainable speed. Tactical producers own the destination
and action intent. Do not introduce a marking-only slowdown radius, gain, or speed multiplier.
Any necessary new movement parameter belongs to Spec #2, with its source tag, units, range, default,
and evidence recorded under the existing tuning policy.

## Baseline instrument

Use `MovementArrivalBaselineProbe`, retained with the [baseline review](evidence/movement-arrival-braking-20261112/README.md),
for direct 60 Hz state-before/state-after and guard flag/lock-entry observations beside commands and
positions. The baseline's 10 Hz descriptive summaries are supplementary; command changes alone
are not state transitions or proof of guard engagement. The retained instrument observes accepted
state changes, not rejected attempts. Add requested-state and guard-rejection observations when
validating a controller change; retain source identity, exact inputs, and all mirrored traces.

The retained W13 run covers blockers 1–4 with the mirrored engine rig and blocker 5 through the
actual #14 tick for both team IDs in its canonical frame. Complete the engine-level stale-tackle
resolution regression as part of the fix; the lower-level reproduction does not claim that coverage.

## Required contract decisions

1. Define which commands settle at their destination and which follow or run through a point.
   Audit every command factory and its callers, including MOVE, HOLD, DRIBBLE, PRESS, INTERCEPT,
   goalkeeper movement, and direct engine submissions. Do not apply a universal stop-at-target rule.
2. Define arrival by both position error and residual speed, with specified tolerances and a bounded
   settling time. Establish numeric limits from the baseline and existing movement constraints before
   implementation; this draft does not invent acceptance thresholds or tuning values.
3. Execute braking at the 60 Hz movement tick using the held 10 Hz command. Specify behavior for
   destinations inside stopping distance, exact coincidence, lateral approaches, and targets behind
   the player. Preserve acceleration, turning, fatigue, collision, and grounded-state constraints;
   do not snap position or velocity to satisfy an arrival assertion.
4. Define how a moving or newly retargeted destination releases braking and resumes pursuit without
   repeated stop/start behavior. Keep TARGET_LOCK facing independent of travel direction.
5. For the explicit speed command, specify units (m/s), valid values, zero/default behavior, validation,
   and precedence relative to DesiredState, physical limits, fatigue, and arrival braking. If that
   surface is unnecessary, retain the existing command shape.
6. Inventory every added cross-tick value. Held commands are serialized by
   `MatchEngine.WriteMovementCommand`/`ReadMovementCommand`; any added command or persistent state must
   round-trip, participate in the digest, and follow the owning schema/version policy.

## Delivery sequence

1. Capture the W13 implementation behavior with stationary, moving, and retargeted destinations. Retain the
   exact source revision, toolchain, attributes, initial state, command cadence, and traces of target
   error, speed, state transitions, and fatigue. Use both teams and mirrored geometry.
2. Reconcile #2's stopping contract first, assess the two arrival designs, then draft the selected
   #2/#8 amendments and explicit-speed extension with their test plans. Read each complete owning
   spec before editing it. Identify #14 marking
   and #16 snapshot consequences; document numeric acceptance criteria and worked formula examples.
   Any added arrival/hysteresis memory must also survive restore, even if owned by the decision tree.
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
| Pace boundaries | Replay the 2.6 m/s W13 speed-gate case and separate sweeps across the 6 m distance boundary. Record commands, actual state-transition attempts, and guard locks independently; bound all three according to the amended contract. |
| Marking enablement | On the W13 implementation revision, replay goal-bound runners, stationary marks, and tight marks for both teams. Verify contact/grounded time, following error, and goal-side position. Generate tackle requests only from eligible final assignments; a demoted duplicate must not retain a stale request. |
| Existing actions | Exercise the real dispatch path for MOVE/HOLD, marking, PRESS, INTERCEPT/first touch, DRIBBLE, and goalkeeper movement. Verify each action's specified arrival or run-through intent. |
| Physical constraints | Representative attribute extremes and fatigue, exhausted sprint resources, TARGET_LOCK strafing, collisions, and grounded recovery. Verify braking does not bypass these constraints. |
| Replay and restore | Same-seed forward replay and save/restore continuation during braking and retargeting. Cover every added command/state value and default-command compatibility. |
| Repository regression | Run `bash tools/run-tests-local.sh --pr`; retain the result and affected-suite details. A targeted lower-level gate alone is not the repository policy pass. |
| Unity and match behavior | Complete the governing Unity compile/EditMode checks, including AgentMovement and whole MatchEngine coverage appropriate to a shared movement change. Compare representative match traces and performance; use the pinned host for certification claims. |

## Completion criteria

The work is complete when the arrival/speed contract and numeric acceptance criteria are specified,
the shared implementation meets them through production command paths, the dedicated pass has
retained evidence, and replay/restore remains deterministic at the new revision. A marking-specific
constant or a passing marking-only test does not close this work item. Recommend keeping marking
disabled until all five reported failures have been reproduced and resolved or explicitly scoped out
with evidence. The tight-marking floor decision is held until W13-F10 is explained; behavior must still be validated afterward. W13 owns closure of F5–F10.

## Version history

| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-11-12 | Initial movement-system work-item draft, grounded in the inspected command, integration, dispatch, and snapshot paths. |
| 0.2 | 2026-11-12 | Incorporate both Fable/Opus review rounds: leave arrival ownership open pending an accurate #2 stopping contract; unify explicit-speed work with W13.1c; locate the W13 implementation, retain mirrored diagnostic results and physics-transition/guard traces, and separate reproduction from acceptance. |
| 0.3 | 2026-11-12 | Link authoritative W13-F5–F10 records, distinguish repeatability from independence and gameplay harm, block tight-floor calibration on mirror asymmetry, and register the moved evidence with repository integrity checks. |
