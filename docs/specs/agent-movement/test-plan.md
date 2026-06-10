# Agent Movement Specification — Test Plan

**Created:** June 4, 2026
**Status:** Draft (Stage 0+1 regression anchor)
**Authoring spec:** Agent Movement #2
**Coverage tier:** Tier A (deterministic) per Deterministic Simulation #16 §1.1.1
**Test framework:** NUnit (Unity Test Runner, EditMode) per Code Standards #20 / Testing Strategy #19 §7.5 D2

---

## 1. PURPOSE AND SCOPE

Agent Movement #2 §5 is a *performance* analysis section, not a test plan; §6/§7/§9
do not enumerate tests either. The placeholder `AgentMovementTests.cs` (created
2026-05-26) cited a non-existent "§5.1 with 85 test scenarios" — that section was a
fiction. This document is the **authoritative** test catalogue for the Agent Movement
assembly until the Spec #2 body absorbs a §5.5 Test Plan.

The initial roster is **regression-anchored**: every test ID below names a specific
adversarial-review (AR) finding that produced an observable bug, hand-tracked through
the AR series for that fix. The point is to lock the fix in executable form so a
future refactor cannot silently re-introduce the bug.

The plan is open-ended — additional T-AM-IDs should be appended (not renumbered) as
new coverage is authored. Cross-spec test plans (Collision System #3 ↔ Agent Movement
collision interaction, First Touch #4 ↔ Agent Movement possession transition) belong
to the **consuming** spec, not this one.

---

## 2. TEST ID CONVENTION

`T-AM-NNN` — three-digit zero-padded, allocated in order of authoring. Once allocated,
**never reused** even if the test is deleted; tombstone the ID with a `RETIRED:`
note inline so callers can grep for the prior anchor.

Blocks are grouped by the file under test, not by the AR ordinal, so a new fix lands
next to its sibling assertions rather than at the end of the file.

| Block | Range | File under test |
|---|---|---|
| Dwell-formula unit tests | T-AM-001..009 | `AgentStateMachine.CalculateGroundedDwell` + `.CalculateStumbleDwell` |
| Pipeline collision integration | T-AM-010..018 | `AgentMovementSystem.Update` (Step 3) |
| Stumble-decision unit tests | T-AM-019..023 | `AgentStateMachine.ShouldStumble` |
| Safety-override integration | T-AM-030..033 | `AgentMovementSystem.Update` (Step 10/11) |
| Safety-system unit tests | T-AM-034..039 | `AgentSafetySystem` (`HasInvalidValues` / `ClampVelocity` / `ClampToPitch` / `Validate`) |
| OscillationGuard unit tests | T-AM-040..047 | `OscillationGuard.RecordAndCheck` |
| PerformanceContext unit tests | T-AM-050..052 | `PerformanceContext.EvaluateAttribute` |
| Locomotion formula tests | T-AM-070..083 | `AgentLocomotion` (`CalculateBaseTopSpeed` / `CalculateBaseAccelK` / `ApplyAcceleration` / `ApplyDeceleration` / `CalculateStoppingDistance` / `CalculateAerobicModifier`) |
| Directional formula tests | T-AM-084..099 | `AgentDirectionalMovement` (`LateralMultiplier` / `BackwardMultiplier` / `CalculateDirectionalMultiplier` / `ApplyDirectionalToAccelK` / `MovementAngleDeg` / `RotateFacingToward`) |
| Turning formula tests | T-AM-100..107 | `AgentTurning` (`CalculateMaxTurnRate` / `MinimumTurnRadius` / `CalculateLeanAngle`) |
| Deceleration-floor unit tests | T-AM-108..109 | `AgentLocomotion.ApplyDeceleration` (AR-12 H-3) |
| Closed-loop locomotion integration | T-AM-110..115 | `AgentMovementSystem.Update` full pipeline (AR-12 H-1/H-2/H-3, AR-13 M-1/M-2) |
| Future (fatigue, etc.) | T-AM-116..149 | reserved |

---

## 3. TEST ROSTER

Each row records: ID, file under test, AR anchor, scenario summary, regression
hazard. Detailed assertion code lives in the test source — this table is the index.

### 3.1 Dwell-formula unit tests — `AgentStateMachine.CalculateGroundedDwell`

| ID | AR anchor | Scenario | Regression hazard |
|---|---|---|---|
| T-AM-001 | AR-9 M-1 | Default attrs (balance=10, strength=10), `COLLISION`, force=1.0 → dwell = 2.0 s | If formula constants drift, max-force dwell collapses. |
| T-AM-002 | AR-9 M-1 | Default attrs, `COLLISION`, force=0.0 → dwell = 1.3 s (= base × CollisionDwellMin) | Pins the force=0 floor so AR-9 M-1 has a numeric anchor. |
| T-AM-003 | AR-2 H-1 | Default attrs, `SLIDING_TACKLE`, force=1.0 → dwell = base × SlidingTackleDwellMult, clamped | Verifies reason multiplier reaches the formula (AR-2 H-1 was the gap). |
| T-AM-004 | AR-2 L-2 | Min attrs (balance=1, strength=1) → dwell clamped at `GroundedDwellClampMax` | Prevents float division explosion when attribute denom approaches zero. |
| T-AM-005 | — | Max attrs (balance=20, strength=20) → dwell clamped at `GroundedDwellClampMin` | Prevents elite players being effectively immune to grounding. |
| T-AM-006 | AR-8 L-2 | `collisionForce` > 1.0 forwarded to formula → `Clamp01` floors `forceScale` at 1.0 | Cache writes also clamp (see T-AM-014); defence-in-depth. |
| T-AM-007 | — | `CalculateStumbleDwell(balance=10)` → ~1.2 s (within `[StumbleDwellClampMin, StumbleDwellClampMax]`) | Spec §3.1.5 stumble dwell formula default-attr anchor. |
| T-AM-008 | — | `CalculateStumbleDwell(balance=1)` → clamped at `StumbleDwellClampMax` (1.5 s) | Lower-attribute saturation. |
| T-AM-009 | — | `CalculateStumbleDwell(balance=20)` → ≥ `StumbleDwellClampMin`, < default-attrs dwell | Elite players recover faster but not instantly. |

### 3.2 Pipeline collision integration — `AgentMovementSystem.Update` Step 3

| ID | AR anchor | Scenario | Regression hazard |
|---|---|---|---|
| T-AM-010 | AR-9 M-1 | Entry frame: `isCollisionKnockdown=true`, force=1.0 → `state.CurrentState=GROUNDED`, `state.CollisionForce=1.0`, `state.GroundedReason=COLLISION` | Cache write contract — every downstream test depends on it. |
| T-AM-011 | AR-9 M-1 | Dwell frames: after entry-frame at force=1.0, run 90 frames (1.5 s) with `isCollisionKnockdown=false`, incoming `collisionForce=0` → still GROUNDED | **PRIMARY AR-9 M-1 regression lock.** If `EvaluateState` is passed the incoming `collisionForce` instead of `state.CollisionForce`, dwell collapses to ~1.3 s and this releases by frame 78. |
| T-AM-012 | AR-9 M-1 | After T-AM-011, run another 36 frames (to ~2.1 s total) → released to IDLE | Anchors the upper bound so T-AM-011 cannot be satisfied by an infinite-dwell regression. |
| T-AM-013 | AR-5 M-2 | Enter GROUNDED at force=0.5, advance 20 frames, deliver second knockdown at force=1.0 → `state.CollisionForce=1.0`, `state.TimeInState=0` | If second-hit refresh regresses, the second impulse is silently dropped and dwell rides out the first hit's lower force. |
| T-AM-014 | AR-8 L-2 | Knockdown delivered with `collisionForce=2.0` → `state.CollisionForce=1.0` (Clamp01 on cache write) | Both transition branch (line ~136) and refresh branch (line ~176) clamp; second-hit case at force=2.0 also clamps. |
| T-AM-015 | AR-6 M-1 | GROUNDED dwell expires on the same frame a fresh collision arrives → next-frame state is GROUNDED (refreshed), **not** a one-frame IDLE flicker | AR-6 M-1 dropped the `current != GROUNDED` guard on knockdown short-circuit; verifies no IDLE frame appears mid-knockdown. |
| T-AM-016 | AR-6 M-2 | Lock `OscillationGuard` via 7 fast transitions, then deliver knockdown → transition to GROUNDED bypasses the guard | If guard bypass regresses, a knockdown that follows a flap sequence is delayed by `LockDuration`. |
| T-AM-017 | AR-7 M-1 | After T-AM-016 transitions and dwell expires, the post-recovery `GROUNDED→IDLE` transition completes immediately | Without `OscillationGuard.Initialize()` on the collision branch, the stale lock blocks the recovery transition. |
| T-AM-018 | AR-3 R3-M-1 | After dwell expires and transition to IDLE fires → `state.GroundedReason=NONE`, `state.CollisionForce=0.0` | Restores the field invariant "GroundedReason == NONE when CurrentState != GROUNDED". |

### 3.3 Safety-override integration — `AgentMovementSystem.Update` Steps 10/11

| ID | AR anchor | Scenario | Regression hazard |
|---|---|---|---|
| T-AM-030 | AR-5 M-1 | Override command + pre-corrupted `state.Velocity = (NaN, 0)` → `LastValidPosition / LastValidVelocity / LastValidFacing` unchanged across the frame | **PRIMARY AR-5 M-1 regression lock.** Tooling-injected NaN must not poison the recovery cache, or the agent is stuck in a permanent recovery loop the moment override flips off. |
| T-AM-031 | AR-7 M-2 | Override command + pre-corrupted `state.Velocity = (NaN, 0)` → `state.Speed` preserved from prior frame (not assigned `NaN`) | If `state.Speed = Velocity.magnitude` escapes the validity gate, next-frame `EvaluateState` runs with NaN speed and silently flips to arbitrary states. |
| T-AM-032 | AR-5 M-1 | Override command + finite valid trajectory → `LastValid*` and `Speed` updated to current values | Verifies the gate is not too aggressive — happy-path tooling sessions must still refresh the cache. |
| T-AM-033 | AR-4 M-5 | Non-override (normal) frame + pre-corrupted `state.Position = (NaN, 0)` → after frame, `state.Position == state.LastValidPosition` (recovery snap) and `recovered` path taken (so `LastValid*` NOT overwritten with the post-recovery values) | Verifies the non-override recovery path also preserves the cache so subsequent NaN frames keep snapping to the same anchor. |

### 3.4 OscillationGuard unit tests — `OscillationGuard.RecordAndCheck`

| ID | AR anchor | Scenario | Regression hazard |
|---|---|---|---|
| T-AM-040 | — | Fresh `Initialize()` + single transition → not blocked | Sentinel `NegativeInfinity` initialisation; covers the AR-1 false-positive-at-t=0 hazard. |
| T-AM-041 | — | 7 transitions across 0.6 s (> `MaxTransitionsPerSecond`) → 7th call returns `true` (locked) | Lock activation; consumed by T-AM-016 setup. |
| T-AM-042 | AR-4 M-2 | After lock fires, a transition during the lock window returns `true` | Lock window enforcement. |
| T-AM-043 | AR-4 M-2 | After `LockDuration` elapses and ring buffer was reset on lock entry, the next transition returns `false` (no indefinite re-lock) | Closes the AR-4 M-2 indefinite-lockout corner case — pre-lock timestamps could keep `recentCount > 6` after the lock window expired without the ring-buffer reset. |
| T-AM-044 | — | 6 transitions inside the window followed by one transition `> WindowSeconds` later → not blocked | Sliding-window expiry; verifies stale timestamps drop out of `recentCount`. |
| T-AM-045 | — | Buffer wrap: 9 transitions in slow succession (well-spaced) → `_writeIndex` wraps cleanly via `% BufferSize` | Ring-buffer modular arithmetic; would surface an off-by-one in the wrap step. |
| T-AM-046 | — | Sparse pattern: 6 transitions inside the window then 6 more outside → no false lock | Verifies the rolling count is current-time-relative, not lifetime-cumulative. |
| T-AM-047 | — | Re-`Initialize()` after lock → guard fully reset, next transition succeeds | The supported way for tooling / collision-bypass to wipe a locked guard. |

### 3.5 Stumble-decision unit tests — `AgentStateMachine.ShouldStumble`

| ID | AR anchor | Scenario | Regression hazard |
|---|---|---|---|
| T-AM-019 | — | Low speed (= `StumbleSpeedThreshold`) + 90° turn + default attrs → `false` (`MinStumbleRisk` floor < resistance) | Floor only — not a stumble trigger. |
| T-AM-020 | — | Max speed + 180° turn + min attrs (agility=1, balance=1) → `true` | Worst case; verifies risk reaches above the resistance ceiling. |
| T-AM-021 | — | Moderate stress (speed=8, turn=120°): min attrs stumble; elite attrs resist | Attribute axis must discriminate at moderate stress. Note: at peak stress (speed=12, turn=180°) `stumbleRisk = 1.5` exceeds even max resistance (`1.0`), so elites are deliberately NOT immune at peak stress per §3.1.5. |
| T-AM-022 | — | Determinism: same inputs → same output (no RNG path) | Locks the §3.4.4 "stumble decision is deterministic at Stage 0" contract. |
| T-AM-023 | — | Zero turn angle → only `MinStumbleRisk` applies; result depends solely on attribute resistance | Risk-floor reachability across the attribute range. |

### 3.6 Safety-system unit tests — `AgentSafetySystem`

| ID | AR anchor | Scenario | Regression hazard |
|---|---|---|---|
| T-AM-034 | AR-4 M-5 | `HasInvalidValues` returns `true` for NaN position, Inf velocity, or zero facing | The three input categories the safety gate must catch. |
| T-AM-035 | AR-4 M-5 | `HasInvalidValues` returns `false` for fully valid inputs | Verifies the gate is not stuck closed. |
| T-AM-036 | — | `ClampVelocity` leaves below-clamp velocities unchanged | No false clamping on safe values. |
| T-AM-037 | — | `ClampVelocity` rescales above-clamp velocities to `MAX_SPEED_CLAMP` and preserves direction | Direction preservation is the §4.3.1 contract. |
| T-AM-038 | — | `ClampToPitch` keeps in-bounds positions unchanged; rescales out-of-bounds to `[−buffer, dim + buffer]` | The pitch boundary contract. |
| T-AM-039 | AR-4 M-5 | `Validate` happy-path leaves inputs untouched and reports `wasRecovered = false` | Sanity gate on the no-NaN path. |

### 3.7 PerformanceContext unit tests — `PerformanceContext.EvaluateAttribute`

| ID | AR anchor | Scenario | Regression hazard |
|---|---|---|---|
| T-AM-050 | — | Neutral context (all modifiers = 1.0) × any raw attribute → raw value (no scaling) | The §3.2.2 identity for unmodified attributes. |
| T-AM-051 | — | Partial-modifier context × raw attribute → raw × (form × context × career) | The multiplication chain itself. |
| T-AM-052 | — | Out-of-range modifier inputs to `Create` are `Clamp01`-bounded by the constructor | Defence against ill-formed caller data. |

### 3.8 Locomotion formula tests — `AgentLocomotion`

| ID | AR anchor | Scenario | Regression hazard |
|---|---|---|---|
| T-AM-070 | — | `CalculateBaseTopSpeed(pace=1)` → `TOP_SPEED_MIN` (7.5 m/s) | Spec §3.2.4 lower anchor. |
| T-AM-071 | — | `CalculateBaseTopSpeed(pace=20)` → `TOP_SPEED_MAX` (10.2 m/s) | Spec §3.2.4 upper anchor. |
| T-AM-072 | — | `CalculateBaseTopSpeed(pace<1 or pace>20)` → clamped at min/max | Out-of-range input does not extrapolate. |
| T-AM-073 | — | `CalculateBaseAccelK(accel=1)` → `AccelKMin`; `(accel=20)` → `AccelKMax` | Spec §3.2.3 endpoints. |
| T-AM-074 | — | `ApplyAcceleration(at top speed)` → unchanged (`decay × topSpeed + decay × topSpeed = topSpeed`) | Steady-state fixed point. |
| T-AM-075 | — | `ApplyAcceleration(from rest, k>0, dt>0)` → strictly between 0 and `topSpeed`; converges as `dt → ∞` | Exponential interp monotonicity. |
| T-AM-076 | — | `ApplyAcceleration` output clamped at `MAX_SPEED_CLAMP` | Spec §4.3.1 safety cap. |
| T-AM-077 | — | `ApplyDeceleration(currentSpeed < MIN_VELOCITY_MAGNITUDE)` → 0 | Early-return contract. |
| T-AM-078 | — | `ApplyDeceleration(small stoppingDistance)` → capped at `MAX_ACCELERATION × dt` decrement | Acceleration ceiling caps decel demand. |
| T-AM-079 | — | `ApplyDeceleration(normal stopping distance)` → reduces speed by `v²/(2d) × dt` | Spec §3.2.5 kinematic. |
| T-AM-080 | — | `CalculateStoppingDistance(CONTROLLED, pace=1)` → `ControlledDecelDistMin`; `(pace=20)` → `ControlledDecelDistMax` | Controlled-mode endpoints. |
| T-AM-081 | — | `CalculateStoppingDistance(EMERGENCY, pace=1)` → `EmergencyDecelDistMin`; `(pace=20)` → `EmergencyDecelDistMax` | Emergency-mode endpoints. |
| T-AM-082 | — | `CalculateAerobicModifier(pool ≥ AerobicModifierThreshold)` → 1.0 | Piecewise above-threshold branch. |
| T-AM-083 | — | `CalculateAerobicModifier(pool = 0)` → `AerobicModifierFloor`; midpoint → linear interp | Piecewise below-threshold branch. |

### 3.9 Directional formula tests — `AgentDirectionalMovement`

| ID | AR anchor | Scenario | Regression hazard |
|---|---|---|---|
| T-AM-084 | — | `LateralMultiplier(agility=1)` → `LateralMultMin`; `(agility=20)` → `LateralMultMax` | Spec §3.3.2 endpoints. |
| T-AM-085 | — | `BackwardMultiplier(agility=1)` → `BackwardMultMin`; `(agility=20)` → `BackwardMultMax` | Spec §3.3.2 endpoints. |
| T-AM-086 | — | `CalculateDirectionalMultiplier(angle ≤ 33°)` → 1.0 (forward + hysteresis) | Forward zone with hysteresis. |
| T-AM-087 | — | `CalculateDirectionalMultiplier(40 ≤ angle ≤ 80)` → lateral multiplier | Lateral zone. |
| T-AM-088 | — | `CalculateDirectionalMultiplier(angle ≥ 90)` → backward multiplier | Backward zone. |
| T-AM-089 | — | `CalculateDirectionalMultiplier(33 < angle < 40)` → strictly between 1.0 and lateral | Forward-lateral blend region. |
| T-AM-090 | — | `CalculateDirectionalMultiplier(80 < angle < 87)` → strictly between lateral and backward | Lateral-backward blend region. |
| T-AM-091 | — | `ApplyDirectionalToAccelK(kBase, mult=1.0)` → `kBase` | Identity at unit multiplier. |
| T-AM-092 | — | `ApplyDirectionalToAccelK(kBase, mult=0.5)` → `kBase × sqrt(0.5)` | Spec §3.3.5 sqrt scaling. |
| T-AM-093 | — | `ApplyDirectionalToAccelK(kBase, mult<0)` → `0` (clamped, no NaN from `sqrt`) | Defensive clamp. |
| T-AM-094 | — | `MovementAngleDeg(same direction)` → 0 | Identity. |
| T-AM-095 | — | `MovementAngleDeg(orthogonal)` → 90 | Quadrant anchor. |
| T-AM-096 | — | `MovementAngleDeg(opposite)` → 180 | Max angle. |
| T-AM-097 | — | `MovementAngleDeg(zero input vector)` → 0 (degenerate guard) | Zero-vector contract. |
| T-AM-098 | — | `RotateFacingToward(targetFacing = zero)` → currentFacing unchanged, `signedAngleApplied = 0` | Degenerate-target carve-out. |
| T-AM-099 | — | `RotateFacingToward(large requested angle, small maxTurnDeg)` → `signedAngleApplied` clamped to ±`maxTurnDeg` | Rate-limit enforcement. |

### 3.10 Turning formula tests — `AgentTurning`

| ID | AR anchor | Scenario | Regression hazard |
|---|---|---|---|
| T-AM-100 | — | `CalculateMaxTurnRate(speed=0, default attrs, IDLE)` → `≈ TURN_RATE_BASE × balanceMod` (clamped at `TURN_RATE_CAP`) | Zero-speed anchor — `1/(1+0)` denominator. |
| T-AM-101 | — | `CalculateMaxTurnRate(high speed)` strictly less than zero-speed rate | Speed-dependent reduction. |
| T-AM-102 | — | `CalculateMaxTurnRate(extreme speed)` → clamped at `TURN_RATE_FLOOR` | Lower-clamp contract. |
| T-AM-103 | — | `CalculateMaxTurnRate(GROUNDED)` → `TURN_RATE_FLOOR` (stateMod = 0 → rate = 0 → clamped up to floor) | GROUNDED can still rotate ≥ floor (clamp survives the StateModifier-0 multiply). |
| T-AM-104 | — | `MinimumTurnRadius(maxTurnRateDeg < TURN_RATE_EPSILON_DEG)` → `float.MaxValue` | Divide-by-zero guard. |
| T-AM-105 | — | `MinimumTurnRadius(normal)` → `v / (ω × Deg2Rad)` | Kinematic formula. |
| T-AM-106 | — | `CalculateLeanAngle(any speed, signedTurnRate = 0)` → 0 | Identity. |
| T-AM-107 | — | `CalculateLeanAngle(extreme centripetal)` → clamped at ±`MAX_LEAN_ANGLE` (sign preserved) | Lean clamp + sign-preservation contract. |

### 3.11 Deceleration-floor unit tests — `AgentLocomotion.ApplyDeceleration` (AR-12 H-3)

| ID | AR anchor | Scenario | Regression hazard |
|---|---|---|---|
| T-AM-108 | AR-12 H-3 | `ApplyDeceleration(v=0.4, d=4)` → decrement uses `MinDecelerationFloor` (raw v²/2d = 0.02 m/s² is below the floor) | Without the floor, low-speed braking is hyperbolic (Zeno) and never terminates. |
| T-AM-109 | AR-12 H-3 | Iterated stop from 6 m/s with d=4 crosses `IdleEnter` within 3 simulated seconds | Pre-fix this took ~78 s; bounds the whole braking profile, not one frame. |

### 3.12 Closed-loop locomotion integration — `AgentMovementSystem.Update` (AR-12)

First whole-seconds closed-loop coverage: every pre-AR-12 test exercised a pure
function or injected mid-flight state, which is exactly why H-1/H-2/H-3 survived
eleven AR rounds.

| ID | AR anchor | Scenario | Regression hazard |
|---|---|---|---|
| T-AM-110 | AR-12 H-1 | `CreateAtPosition` + `MoveTo`, 3 s → JOGGING, speed > JogEnter, position advanced | **PRIMARY H-1 lock.** Pre-fix the IDLE branch only decayed speed while `EvaluateFromIdle` required speed > IdleExit — every agent at rest was deadlocked at speed 0 forever. |
| T-AM-111 | AR-12 H-2 | `MoveTo` (jog intent), 8 s → never SPRINTING, never DECELERATING, speed ≤ SprintEnter | **PRIMARY H-2 lock.** Pre-fix topSpeed ignored commandSpeed: jog commands auto-promoted to SPRINTING (reservoir drain) and flap-cycled via DECELERATING. |
| T-AM-112 | AR-12 H-3 | JOGGING at 5.5 m/s + `Stop`, 3 s → IDLE, total travel < 8 m | **PRIMARY H-3 lock (pipeline level).** Pre-fix Zeno braking: ~78 s and ~32 m before IdleEnter. |
| T-AM-113 | AR-12 H-2 | `WalkTo` from rest, 5 s → settles WALKING at the JogEnter ceiling; never escalates above the walking band | Locks the new `WalkTo` factory + walk-band stability (pre-fix WALKING→JOGGING→DECELERATING flap). |
| T-AM-114 | AR-13 M-1 | `AerobicPool=0.05` + `MoveTo`, 5 s → settles WALKING; never JOGGING/SPRINTING/DECELERATING | Exhausted-agent command degradation — without the commandSpeed clamp the aerobic gate flaps WALKING→JOGGING→DECELERATING at ~3 Hz until the guard locks. |
| T-AM-115 | AR-13 M-2 | `StrafeWhileWatching(ownPosition, ball)` (DT HOLD shape) from rest, 2 s → stays IDLE at speed 0, position unchanged | Without the movement-intent offset gate, the H-1 launch path feeds newSpeed > 0 into Step 8 with a degenerate target, tripping the both-degenerate assert every frame. |

---

## 4. NON-COVERAGE (NAMED)

The following surfaces are **deliberately not covered** by this roster. Each line
records the reason and the issue that opens coverage:

- **`UpdateAllAgents` goalkeeper-skip / array-length validation.** Currently asserted
  via `Debug.Assert` in dev builds (AR-5 L-2 / AR-8 M-1). Promote to NUnit coverage
  when the §5.5 Test Plan defines the assert-vs-test boundary.
- **Fatigue accumulation (§3.1.3 table).** Per-state drain/recovery rates land at
  Stage 1+ alongside the §6.2 weather and §6.3 form modifiers. Stage 0 numbers are
  placeholder GT and a regression-anchored test would just re-codify the
  placeholders. Coverage opens with the first dual-energy spec edit.
- **`AgentStateMachine.EvaluateFromX` private branches.** Each branch is reachable
  only through `EvaluateState`; integration coverage exercises the GROUNDED branch
  (T-AM-010..018) and, since AR-12, the IDLE / WALKING / JOGGING / DECELERATING
  launch-and-stop transitions (T-AM-110..113). SPRINTING entry/exit and STUMBLING
  remain uncovered at pipeline level — coverage opens with a future expansion in
  the reserved T-AM-114..149 block.
- **`RotateVelocityToward` both-degenerate fallback.** The branch is `Debug.Assert(false, …)`
  by design — testing it would require `LogAssert.Expect` and would lock in a
  contract that explicitly says "unreachable in normal flow". Out of scope.
- **Cross-spec collision integration (Collision System #3 producing the
  `isCollisionKnockdown` signal).** Owned by Spec #3's test plan, not here.

---

## 5. DETERMINISM AND FRAMEWORK NOTES

- All tests **must be deterministic**. No `System.Random`, no `DateTime.Now`, no
  `Time.deltaTime` (FR-CS-036 / FR-CS-042). Time inputs are explicit `float t`
  accumulators.
- Default tick rate: 60 Hz physics, `dt = 1.0f / 60.0f`. Pipeline tests construct
  `AgentMovementSystem(60.0f)`.
- Tooling-only `MovementCommand` (override-safety branch) is constructed via the
  `internal static MovementCommand.ToolingOverrideOnly_NaNInjection(...)` factory.
  The test assembly is granted access via `InternalsVisibleTo` in
  `src/agent-movement/AssemblyInfo.cs`. **Production game logic MUST NOT call this
  factory** — it is a regression-test seam only.
- Floating-point assertions use `Assert.AreEqual(expected, actual, tolerance)` with
  an explicit tolerance. Default tolerance for dwell-time assertions: `0.001f`. For
  per-frame integration assertions, tolerance scales with `dt` (`dt × eps`).

---

## 6. VERSION HISTORY

| Version | Date       | Author | Notes                                                                                                |
|---------|------------|--------|------------------------------------------------------------------------------------------------------|
| 0.1     | 2026-06-04 | —      | Initial regression-anchored roster (T-AM-001..018, 030..033, 040..043). Locks AR-3 R3-M-1, AR-4 M-2, AR-4 M-5, AR-5 M-1, AR-5 M-2, AR-6 M-1, AR-6 M-2, AR-7 M-1, AR-7 M-2, AR-8 L-2, AR-9 M-1. |
| 0.2     | 2026-06-04 | —      | Pure-function coverage expansion. New IDs T-AM-019..023 (ShouldStumble), T-AM-034..039 (AgentSafetySystem unit), T-AM-044..047 (OscillationGuard edge cases), T-AM-050..052 (PerformanceContext), T-AM-070..083 (AgentLocomotion), T-AM-084..099 (AgentDirectionalMovement), T-AM-100..107 (AgentTurning). Non-coverage section rewritten — locomotion / turning / directional dropped from the carve-out (now covered); EvaluateFromX private-branch carve-out, fatigue table, UpdateAllAgents asserts, and the RotateVelocityToward both-degenerate `Debug.Assert(false)` branch remain explicitly non-covered. |
| 0.3     | 2026-06-09 | —      | AR-12/AR-13 fix-pass coverage. New IDs T-AM-108..109 (ApplyDeceleration MinDecelerationFloor unit + bounded-termination) and T-AM-110..115 (closed-loop pipeline: launch-from-rest H-1, jog band-respect H-2, bounded stop H-3, WalkTo walk-band stability, AR-13 M-1 exhausted-agent command degradation, AR-13 M-2 HOLD-at-own-position rest). T-AM-079 re-derived (v=4→v=6) for the deceleration floor. §4 EvaluateFromX carve-out narrowed to SPRINTING/STUMBLING pipeline transitions. |
