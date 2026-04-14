# Shot Mechanics Specification #6 — Section 2: System Overview

**File:** `Shot_Mechanics_Spec_Section_2_v1_0.md`
**Purpose:** Establishes functional requirements, system architecture, frame pipeline
position, data structures, sub-system decomposition, performance constraints, and failure
modes for Shot Mechanics Specification #6. This section defines the "what" and "why"
before Section 3 defines the "how."

**Created:** February 22, 2026, 11:30 PM PST
**Version:** 1.0
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 6 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)
**Prerequisite:** Section 1 (Purpose & Scope) v1.1

**Open Dependency Flags:**
- None. All hard dependencies confirmed stable in Section 1. Pass Mechanics (Spec #5)
  confirmed approved — data structure patterns are reused directly in §2.4.

---

## Table of Contents

- [2.1 Functional Requirements](#21-functional-requirements)
- [2.2 System Architecture](#22-system-architecture)
  - [2.2.1 Architectural Position](#221-architectural-position)
  - [2.2.2 Sub-System Decomposition](#222-sub-system-decomposition)
  - [2.2.3 Evaluation Pipeline](#223-evaluation-pipeline)
- [2.3 Frame Pipeline Position](#23-frame-pipeline-position)
- [2.4 Data Structures](#24-data-structures)
  - [2.4.1 ShotRequest](#241-shotrequest)
  - [2.4.2 ShotResult](#242-shotresult)
  - [2.4.3 ShotExecutedEvent](#243-shotexecutedevent)
  - [2.4.4 ShotAnimationData (Stub)](#244-shotanimationdata-stub)
- [2.5 Physical Parameter Reference Table](#25-physical-parameter-reference-table)
- [2.6 Non-Functional Requirements](#26-non-functional-requirements)
- [2.7 Failure Modes and Recovery](#27-failure-modes-and-recovery)
- [2.8 Open Dependency Flags](#28-open-dependency-flags)
- [2.9 Version History](#29-version-history)

---

## 2.1 Functional Requirements

Eleven functional requirements govern the Shot Mechanics system for Stage 0. Each
requirement derives directly from the core responsibilities defined in Section 1.2 and
maps to one or more sub-systems in Section 3. All requirements include their Section 3
owner and test coverage identifiers.

| FR ID | Short Name | Priority | Section 3 Owner | Test IDs |
|-------|------------|----------|-----------------|----------|
| FR-01 | ShotRequest Validation | CRITICAL | §3.1 | PV-001–PV-008 |
| FR-02 | Velocity Calculation | CRITICAL | §3.2 | SV-001–SV-012 |
| FR-03 | Power–Accuracy Trade-off | CRITICAL | §3.2 | SV-013–SV-020 |
| FR-04 | Launch Angle Derivation | CRITICAL | §3.3 | LA-001–LA-008 |
| FR-05 | Spin Vector Calculation | HIGH | §3.4 | SN-001–SN-008 |
| FR-06 | Placement Resolution | CRITICAL | §3.5 | SP-001–SP-010 |
| FR-07 | Deterministic Error Model | CRITICAL | §3.6 | SE-001–SE-010 |
| FR-08 | Body Mechanics Evaluation | HIGH | §3.7 | BM-001–BM-008 |
| FR-09 | Weak Foot Penalty | HIGH | §3.8 | WF-001–WF-006 |
| FR-10 | Execution State Machine | CRITICAL | §3.9 | SSM-001–SSM-008 |
| FR-11 | Event Publishing | MEDIUM | §3.10 | IT-001–IT-012 |

---

### FR-01 — ShotRequest Validation

**Statement:** The system shall validate every inbound `ShotRequest` before execution
begins. Validation covers: agent possession state, `PowerIntent` range [0.0–1.0],
`SpinIntent` range [0.0–1.0], `ContactZone` enum membership, `PlacementTarget` (u,v)
range [0.0–1.0 per component], and non-zero `FrameNumber`. Invalid requests produce
`ShotOutcome.Invalid`, log a diagnostic error, and leave ball state unchanged.

**Inputs:**
- `ShotRequest` (full struct — see §2.4.1)
- `BallState.PossessingAgentId` — agent must have possession

**Outputs:**
- Pass/fail gate; on failure → `ShotResult{ Outcome = Invalid }`

**Rationale:** Shot execution applies extreme velocities (20–35 m/s) to the ball. A
malformed request reaching Ball Physics could cause simulation divergence. Validation is
the first and cheapest defence. This is the INITIATING state guard (§3.9).

**Acceptance Criteria:**
1. `PowerIntent > 1.0` or `PowerIntent < 0.0` → `Invalid`, no kick applied.
2. Agent without possession → `Invalid`, no kick applied.
3. `PlacementTarget.u` or `.v` outside [0.0, 1.0] → `Invalid`, no kick applied.
4. `ContactZone` value outside valid enum range → `Invalid`, no kick applied.
5. Valid request with all fields in range → proceeds to WINDUP.

---

### FR-02 — Velocity Calculation

**Statement:** The system shall compute kick speed (m/s) from `PowerIntent`, the agent's
`KickPower` and `Finishing` (or `LongShots`) attributes, fatigue modifier, and a
distance–attribute blend factor. The resulting speed shall be clamped to the physically
meaningful range [V_ABSOLUTE_MIN, V_ABSOLUTE_MAX] = [8.0, 35.0] m/s.

**Inputs:**
- `ShotRequest.PowerIntent` [0.0–1.0]
- `ShotRequest.DistanceToGoal` (metres) — drives Finishing/LongShots blend
- `PlayerAttributes.KickPower`, `Finishing`, `LongShots`
- `AgentState.Fatigue` [0.0–1.0]

**Outputs:**
- `kickSpeed: float` (m/s) — feeds into FR-04 to form velocity `Vector3`

**Rationale:** Real shot velocities range from 8 m/s (placed chip) to ~35 m/s (elite
driven shot). The attribute blend is distance-driven via a continuous sigmoid (OI-001
resolution): `Finishing` dominates within 20m; `LongShots` dominates beyond 20m. Hard
threshold avoided because a discontinuity at exactly 20m would produce jarring gameplay.
`KickPower` scales the ceiling — a low-KickPower agent cannot reach V_ABSOLUTE_MAX
regardless of PowerIntent.

**Acceptance Criteria:**
1. `PowerIntent = 1.0`, `KickPower = 20`, `Fatigue = 1.0`, distance ≤ 10m →
   `kickSpeed` within [28.0, 35.0] m/s.
2. `PowerIntent = 0.4`, `KickPower = 10`, `Fatigue = 0.5`, distance ≤ 10m →
   `kickSpeed` within [10.0, 18.0] m/s.
3. `Fatigue = 0.0` (exhausted) reduces `kickSpeed` by ≥ 10% relative to `Fatigue = 1.0`.
4. `kickSpeed` is never below `V_ABSOLUTE_MIN` (8.0) or above `V_ABSOLUTE_MAX` (35.0).
5. Deterministic: identical inputs → bitwise-identical output.

**Literature:** StatsBomb data indicates elite professional shot speeds of 28–34 m/s
(driven); 10–18 m/s (placed/chip). Lees & Nolan (1998) establish KickPower–velocity
relationship as approximately linear over the effective range of foot-speed contribution.

---

### FR-03 — Power–Accuracy Trade-off

**Statement:** The system shall apply a `PowerPenalty` scalar to the error cone that
widens monotonically as `PowerIntent` increases, using the quadratic form established in
KD-1. A player cannot simultaneously maximise kick velocity and minimise placement error.

**Formula (KD-1):**
```
PowerPenalty(p) = 1.0 + POWER_PENALTY_COEFFICIENT × p²
```
Where `p = PowerIntent ∈ [0.0, 1.0]` and `POWER_PENALTY_COEFFICIENT` is a
gameplay-tunable constant. Default: 1.5 [GT].

**Inputs:**
- `ShotRequest.PowerIntent`
- `POWER_PENALTY_COEFFICIENT` [GT]

**Outputs:**
- `powerPenalty: float ≥ 1.0` — multiplied into error cone width in FR-07

**Rationale:** Biomechanics literature (Lees & Nolan 1998) confirms that maximum-effort
kicks produce significantly higher placement variability than sub-maximal efforts due to
limb velocity oscillation and contact point instability. The quadratic form ensures error
grows slowly at moderate power and sharply at maximum effort — matching observed
behaviour where elite players can "really mean" a hard shot but still occasionally
skew it wide.

**Acceptance Criteria:**
1. `PowerIntent = 0.0` → `PowerPenalty = 1.0` (no penalty).
2. `PowerIntent = 0.5` → `PowerPenalty = 1.375` (at default coefficient).
3. `PowerIntent = 1.0` → `PowerPenalty = 2.5` (at default coefficient).
4. `PowerPenalty` is strictly monotonically increasing with `PowerIntent`.
5. `PowerPenalty ≥ 1.0` for all valid `PowerIntent` values.

---

### FR-04 — Launch Angle Derivation

**Statement:** The system shall compute a launch angle (degrees above horizontal) from
`ContactZone`, `PowerIntent`, `SpinIntent`, and `BodyMechanicsScore`. The angle shall be
bounded to prevent physically impossible trajectories.

**Inputs:**
- `ShotRequest.ContactZone` (Centre / BelowCentre / OffCentre)
- `ShotRequest.PowerIntent`, `ShotRequest.SpinIntent`
- `BodyMechanicsScore` (output of FR-08)
- Base angle constants per `ContactZone` [GT]

**Outputs:**
- `launchAngleDeg: float` — combined with kick direction and `kickSpeed` to form velocity `Vector3`

**Rationale:** Launch angle is the primary determinant of shot trajectory class. Centre
contact at high power produces low flat trajectories (goalkeeper-challenging). BelowCentre
at high SpinIntent produces chip arcs (goalkeeper-over). OffCentre at moderate power
produces curling mid-height trajectories. Body lean (captured in BodyMechanicsScore) is
the leading cause of unintentional skied shots — a key simulation fidelity requirement.

**Acceptance Criteria:**
1. Centre / `PowerIntent` = 1.0 / `SpinIntent` = 0.0 / good body shape →
   angle ∈ [2°, 8°].
2. BelowCentre / `SpinIntent` = 1.0 →
   angle ∈ [20°, 45°].
3. Poor body shape (`BodyMechanicsScore < 0.3`) adds ≥ 5° unintended loft.
4. Launch angle is clamped: never negative (drilling into ground), never > 65°.

---

### FR-05 — Spin Vector Calculation

**Statement:** The system shall compute a spin `Vector3` encoding topspin, sidespin, and
backspin components from `ContactZone`, `SpinIntent`, `PowerIntent`, and the agent's
`Technique` attribute. The spin vector is passed directly to `Ball.ApplyKick()` where
Ball Physics applies Magnus force (Ball Physics §3.1.4).

**Inputs:**
- `ShotRequest.ContactZone`, `ShotRequest.SpinIntent`, `ShotRequest.PowerIntent`
- `PlayerAttributes.Technique` [1–20]
- Base spin values per ContactZone [GT]

**Outputs:**
- `spinVector: Vector3` (rad/s) — axis encodes rotation axis; magnitude encodes spin rate

**Rationale:** Sidespin produces curl radius variation. Topspin produces dipping driven
shots. Backspin produces chip-style trajectory with check-stop on landing. `Technique`
modulates sidespin magnitude specifically — a high-Technique player applies more
deliberate curl for a given `SpinIntent`, producing visibly different curl radii between
players. This is a key differentiator from passing spin, where curl is less tactically
central.

**Acceptance Criteria:**
1. Centre / `SpinIntent = 0.0` / `PowerIntent = 1.0` → dominant topspin, sidespin ≈ 0.
2. OffCentre / `SpinIntent = 1.0` / `Technique = 20` → sidespin > sidespin at `Technique = 10`, all else equal.
3. BelowCentre / `SpinIntent = 1.0` → dominant backspin.
4. All spin components are finite (NaN guard — see FM-05).
5. Spin magnitude does not exceed `SPIN_ABSOLUTE_MAX` (240 rad/s) — physically observed
   maximum for professional football shots.

**Literature:** Inoue et al. (2014) — ball spin rates up to 150 rad/s for driven shots;
Asai et al. (2002) — sidespin mechanics in finesse/curl shots.

---

### FR-06 — Placement Resolution

**Statement:** The system shall resolve the intended `PlacementTarget (u, v)` on the
goal mouth into a world-space aim direction vector, accounting for goal geometry and
agent position. The aim direction is used to orient the final velocity `Vector3` before
error application in FR-07.

**Inputs:**
- `ShotRequest.PlacementTarget` (u, v) ∈ [0.0, 1.0]²
- `AgentState.Position` (world space)
- Goal geometry constants (post positions, crossbar height — from Match Config)

**Outputs:**
- `aimDirection: Vector3` (unit vector) — world-space direction toward resolved target

**Rationale:** Goal-relative coordinates (KD-2) provide a clean API for the Decision
Tree: "aim for bottom-left corner" is `(0.05, 0.05)` regardless of field position or
pitch orientation. The resolver converts this to world-space geometry at execution time.
This also ensures error interpretation is always intuitive: positive v-error → ball
skied high, negative u-error → pulled left.

**Acceptance Criteria:**
1. `PlacementTarget = (0.5, 0.5)` (goal centre) resolves to direction pointing at the
   centre of the goal mouth.
2. `PlacementTarget = (0.05, 0.05)` resolves to a direction pointing ≤ 0.5m from the
   bottom-left post, given a standard 7.32m × 2.44m goal.
3. Resolution is deterministic and independent of frame count.
4. Aim direction magnitude = 1.0 (unit vector enforced).

---

### FR-07 — Deterministic Error Model

**Statement:** The system shall apply a deterministic angular error offset to the aim
direction, derived from `Finishing`/`LongShots` attributes, `Composure`, `Fatigue`,
`PowerPenalty`, and `BodyMechanicsScore`. Error is applied in goal-relative (u, v)
space before conversion to world-space direction. All error sources use the deterministic
hash seed `matchSeed + agentId + frameNumber` (OI-004 resolution).

**Error Composition:**
```
ErrorMagnitude = BaseErrorAngle
                 × PowerPenalty(PowerIntent)           // FR-03
                 × PressurePenalty(Composure, Pressure)
                 × FatiguePenalty(Fatigue)
                 × BodyShapePenalty(BodyMechanicsScore)
                 × WeakFootMultiplier(IsWeakFoot, WeakFootRating)  // FR-09

ErrorOffset(u, v) = ErrorMagnitude × DeterministicHash(matchSeed, agentId, frameNumber)
```

Where `BaseErrorAngle` scales inversely with the blended finishing attribute.

**Inputs:**
- Attribute bundle: `Finishing`, `LongShots`, `Composure`, `KickPower`
- `AgentState.Fatigue`, active `Pressure` scalar
- `PowerPenalty` (from FR-03), `BodyMechanicsScore` (from FR-08)
- `ShotRequest.IsWeakFoot`, `PlayerAttributes.WeakFootRating`
- `matchSeed`, `AgentState.AgentId`, `ShotRequest.FrameNumber`

**Outputs:**
- `errorOffset: Vector2` (u, v goal-space) — added to `PlacementTarget` before
  world-space conversion

**Rationale:** Separating error from aim resolution (FR-06) keeps both sub-systems
testable in isolation. Goal-relative error space means post-mortem analysis is
immediately interpretable without world-space geometry reasoning. The multiplicative
chain means no single source can produce pathological error alone — all penalising
factors must combine to produce a badly errant shot, consistent with real match
observations.

**Acceptance Criteria:**
1. `Finishing = 20`, `Composure = 20`, `PowerIntent = 0.5`, `Fatigue = 1.0`,
   `BodyMechanicsScore = 0.9`, dominant foot → `ErrorMagnitude < 2.0°`.
2. `PowerIntent = 1.0` produces larger `ErrorMagnitude` than `PowerIntent = 0.5`,
   all else equal (FR-03 integration).
3. `IsWeakFoot = true` with `WeakFootRating = 1` produces larger error than
   `WeakFootRating = 5`, all else equal.
4. Identical inputs → bitwise-identical `errorOffset` across frames and machines.
5. No stochastic element (`System.Random`, etc.) in error pipeline.

---

### FR-08 — Body Mechanics Evaluation

**Statement:** The system shall compute a scalar `BodyMechanicsScore ∈ [0.0, 1.0]`
from run-up angle, plant foot offset, agent velocity at contact, and body lean. This
score modulates contact quality (§3.2), launch angle (§3.3, FR-04), and error magnitude
(§3.6, FR-07). Where `BodyMechanicsScore < STUMBLE_THRESHOLD` AND
`PowerIntent > STUMBLE_POWER_THRESHOLD`, a stumble trigger is emitted to Agent Movement.

**Inputs (read-only from Agent Movement §3):**
- `AgentPhysicalProperties.RunUpAngle` (degrees)
- `AgentPhysicalProperties.PlantFootOffset` (metres from ball)
- `AgentState.Velocity` (m/s, direction and magnitude)
- `AgentPhysicalProperties.BodyLean` (degrees, forward/backward)

**Outputs:**
- `BodyMechanicsScore: float ∈ [0.0, 1.0]`
- `staggerTrigger: bool` — emitted to Agent Movement if stumble conditions met (OI-003 resolution)

**Rationale:** Body mechanics is the primary source of unintentional shot quality
variation — identical attributes and intent parameters produce different outcomes from
different body positions. This is the central shot fidelity mechanism not present in
pass mechanics. A defender closing at pace forces a rushed body shape, degrading
`BodyMechanicsScore` and producing realistically imperfect shots. The stumble trigger
reuses Agent Movement's existing hysteresis pattern — no new interface required.

**Acceptance Criteria:**
1. Ideal run-up (30–45° to goal line), plant foot correctly positioned, forward lean,
   velocity directed toward goal → `BodyMechanicsScore ≥ 0.85`.
2. `RunUpAngle` deviation > 40° from ideal → `BodyMechanicsScore ≤ 0.5`.
3. `BodyMechanicsScore < STUMBLE_THRESHOLD` AND `PowerIntent > STUMBLE_POWER_THRESHOLD`
   → `staggerTrigger = true`.
4. Score is read-only from Agent Movement; Shot Mechanics does not mutate agent state.
5. `BodyMechanicsScore` is written to `ShotExecutedEvent` for GK difficulty use in Spec #11.

**Constants [GT]:**
- `STUMBLE_THRESHOLD` = 0.25
- `STUMBLE_POWER_THRESHOLD` = 0.75

---

### FR-09 — Weak Foot Penalty

**Statement:** The system shall apply a weak foot multiplier to error magnitude when
`ShotRequest.IsWeakFoot = true`. The multiplier is derived from `WeakFootRating ∈ [1–5]`
using a linear interpolation between `WEAK_FOOT_MAX_PENALTY` and 1.0 (no penalty at
rating 5). `IsWeakFoot` is set by the Decision Tree — Shot Mechanics applies the penalty
but does not determine foot preference (consistent with Pass Mechanics KD-5).

**Formula:**
```
WeakFootMultiplier = WEAK_FOOT_MAX_PENALTY - (WeakFootRating - 1) / 4.0
                     × (WEAK_FOOT_MAX_PENALTY - 1.0)
```
Where `WEAK_FOOT_MAX_PENALTY` = 2.0 [GT]. Rating 1 → multiplier 2.0; Rating 5 → 1.0.

Also applied as a velocity penalty:
```
kickSpeed *= WEAK_FOOT_VELOCITY_PENALTY[WeakFootRating]  // lookup table [GT]
```

**Inputs:**
- `ShotRequest.IsWeakFoot: bool`
- `PlayerAttributes.WeakFootRating` [1–5]

**Outputs:**
- `weakFootMultiplier: float ≥ 1.0` — multiplied into `ErrorMagnitude` (FR-07)
- `velocityMultiplier: float ≤ 1.0` — multiplied into `kickSpeed` (FR-02)

**Acceptance Criteria:**
1. `IsWeakFoot = false` → `weakFootMultiplier = 1.0`, `velocityMultiplier = 1.0`.
2. `IsWeakFoot = true`, `WeakFootRating = 1` → `weakFootMultiplier = 2.0`.
3. `IsWeakFoot = true`, `WeakFootRating = 5` → `weakFootMultiplier = 1.0`.
4. `velocityMultiplier` is monotonically increasing with `WeakFootRating`.
5. Deterministic: identical inputs → bitwise-identical output.

---

### FR-10 — Execution State Machine

**Statement:** The system shall manage shot execution through a six-state machine:
IDLE → INITIATING → WINDUP → CONTACT → FOLLOW_THROUGH → COMPLETE. WINDUP spans
`WINDUP_FRAMES` frames (varying by `PowerIntent`); FOLLOW_THROUGH spans
`FOLLOW_THROUGH_FRAMES` frames. A tackle interrupt during WINDUP transitions to IDLE
and publishes `ShotCancelledEvent`. A tackle interrupt during CONTACT or later is
ignored — the ball is already leaving the foot.

**States and Transitions:**
```
IDLE ──[ShotRequest received]──► INITIATING
INITIATING ──[validation pass]──► WINDUP
INITIATING ──[validation fail]──► IDLE (ShotResult.Invalid)
WINDUP ──[frames elapsed]──► CONTACT
WINDUP ──[tackle interrupt]──► IDLE (ShotCancelledEvent)
CONTACT ──[Ball.ApplyKick() called]──► FOLLOW_THROUGH
FOLLOW_THROUGH ──[frames elapsed]──► COMPLETE
COMPLETE ──[cleanup]──► IDLE
```

**Windup Duration (WINDUP_FRAMES) [GT]:**
- `PowerIntent ≥ 0.8` → 14 frames (~233ms at 60Hz) — full backswing required
- `PowerIntent 0.5–0.79` → 10 frames (~167ms)
- `PowerIntent < 0.5` → 7 frames (~117ms) — stabbed / quick shot

**Inputs:**
- `ShotRequest` (triggers INITIATING)
- Tackle interrupt flag (polling, consistent with Pass Mechanics §3.8 pattern)

**Outputs:**
- State transitions; `ShotCancelledEvent` on interrupt; `ShotResult` on COMPLETE

**Acceptance Criteria:**
1. Valid request → WINDUP state entered on same frame as INITIATING.
2. Tackle interrupt during WINDUP → IDLE within one frame; `ShotCancelledEvent` published.
3. Tackle interrupt during CONTACT → ignored; `Ball.ApplyKick()` completes.
4. Two consecutive `ShotRequest` submissions without reaching COMPLETE → second request
   rejected with `Invalid` (state machine is occupied).
5. COMPLETE → IDLE transition is automatic; no external trigger required.

---

### FR-11 — Event Publishing

**Statement:** The system shall publish `ShotExecutedEvent` exactly once per successful
`Ball.ApplyKick()` call, and `ShotCancelledEvent` exactly once per WINDUP tackle
interrupt. Events are published to the event bus (Event System Spec #17). The
`ShotExecutedEvent` struct carries sufficient data for Goalkeeper Mechanics (#11) to
consume when written (KD-5 / OI-005). No `IGkResponseSystem` interface is defined here.

**Inputs:**
- `ShotResult` (full struct)
- `BodyMechanicsScore`, `PowerIntent`, `EstimatedTarget`, `KickVelocity`
- `matchSeed`, `frameNumber`

**Outputs:**
- `ShotExecutedEvent` (see §2.4.3)
- `ShotCancelledEvent` (see §2.4.3)

**Acceptance Criteria:**
1. `ShotExecutedEvent` published exactly once per CONTACT state entry. Never published
   if state machine does not reach CONTACT.
2. `ShotCancelledEvent` published on WINDUP interrupt only. Not published on
   `ShotResult.Invalid`.
3. Event payload validated: `KickVelocity.magnitude ∈ [8.0, 35.0]`, `BodyMechanicsScore ∈ [0.0, 1.0]`.
4. Event bus delivery confirmed for at least one subscriber (integration test).

---

## 2.2 System Architecture

### 2.2.1 Architectural Position

Shot Mechanics is a discrete event processor — it does not execute every frame. It
executes only when a `ShotRequest` is submitted by the Decision Tree. Like Pass Mechanics,
it spans multiple frames through its state machine: INITIATING executes in one frame;
WINDUP spans 7–14 frames depending on `PowerIntent`; CONTACT executes in one frame;
FOLLOW_THROUGH spans `FOLLOW_THROUGH_FRAMES` frames.

In a 90-minute match at 60Hz, Shot Mechanics executes approximately 20–40 shot events
per match (combined both teams, consistent with StatsBomb average ~12–18 shots on target
per game plus off-target and blocked). Total CONTACT executions — the computationally
expensive frame — is bounded by this count. This is an extremely generous per-execution
budget.

Shot Mechanics executes **after** Pass Mechanics in the frame-step ordering because both
are discrete events that cannot occur simultaneously for the same agent. If an agent
possessing the ball receives a `ShotRequest`, no concurrent `PassRequest` is active.

---

### 2.2.2 Sub-System Decomposition

Shot Mechanics is decomposed into ten sub-systems. Each maps to one or more FRs and
has a corresponding Section 3 specification.

```
ShotMechanics (ShotExecutor.cs — main orchestrator)
├── ShotValidator              [§3.1]  FR-01  — ShotRequest field validation
├── VelocityCalculator         [§3.2]  FR-02, FR-03 — kick speed + power penalty base
├── LaunchAngleCalculator      [§3.3]  FR-04  — elevation angle from ContactZone + modifiers
├── SpinVectorCalculator       [§3.4]  FR-05  — spin encoding by zone + Technique
├── PlacementResolver          [§3.5]  FR-06  — goal-relative → world-space aim direction
├── ErrorCalculator            [§3.6]  FR-07  — deterministic error offset computation
├── BodyMechanicsEvaluator     [§3.7]  FR-08  — stance quality scalar + stumble trigger
│   └── StumbleTriggerEmitter         — emits to Agent Movement on threshold breach
├── WeakFootPenaltyApplier     [§3.8]  FR-09  — weak foot multiplier (error + velocity)
├── ShotStateMachine           [§3.9]  FR-10  — lifecycle: IDLE→INITIATING→WINDUP→CONTACT→…
└── ShotEventEmitter           [§3.10] FR-11  — ShotExecutedEvent + ShotCancelledEvent
```

---

### 2.2.3 Evaluation Pipeline

Within a single shot execution at CONTACT state, sub-systems execute in strict sequential
order. Each sub-system depends on the output of the preceding one. All inputs are
captured at INITIATING and held constant for the duration (Section 1 §1.6.1 rationale).

```
ShotRequest received (Decision Tree #8)
    │
    ▼
[1] ShotValidator (§3.1)
    Agent has possession? Fields in range? ContactZone valid?
    FAIL → ShotResult{ Outcome = Invalid }; IDLE; return
    │
    ▼ (WINDUP phase begins — attributes captured and frozen)
[2] BodyMechanicsEvaluator (§3.7)         ← read AgentPhysicalProperties (read-only)
    Compute BodyMechanicsScore
    Emit staggerTrigger if threshold breach
    │
    ▼
[3] VelocityCalculator (§3.2)
    Blend Finishing/LongShots by distance sigmoid
    Scale by KickPower, PowerIntent, Fatigue
    Compute PowerPenalty(PowerIntent) [FR-03]
    Apply BodyMechanicsScore → ContactQualityModifier
    → kickSpeed (m/s)
    │
    ▼
[4] LaunchAngleCalculator (§3.3)
    BaseAngle[ContactZone]
    + PowerLiftModifier × (1 − PowerIntent)
    + SpinLiftModifier × SpinIntent
    + BodyLeanPenalty
    + BodyShapePenalty
    → launchAngleDeg
    │
    ▼
[5] SpinVectorCalculator (§3.4)
    SpinBase[ContactZone] × SpinIntent × Technique modifier
    → spinVector (Vector3, rad/s)
    │
    ▼
[6] WeakFootPenaltyApplier (§3.8)
    If IsWeakFoot: apply velocityMultiplier to kickSpeed
    Compute weakFootMultiplier for error chain
    → kickSpeed (adjusted), weakFootMultiplier
    │
    ▼
[7] PlacementResolver (§3.5)
    PlacementTarget (u,v) → world-space aimDirection
    │
    ▼
[8] ErrorCalculator (§3.6)
    BaseErrorAngle × PowerPenalty × PressurePenalty × FatiguePenalty
    × BodyShapePenalty × weakFootMultiplier
    DeterministicHash(matchSeed, agentId, frameNumber) → errorOffset
    aimDirection adjusted by errorOffset
    → finalDirection (unit Vector3)
    │
    ▼
[9] Velocity Vector3 Construction
    velocity = finalDirection × kickSpeed
               tilted by launchAngleDeg
    │
    ▼
[10] NaN / bounds guard (FM-05)
    Any component NaN or Infinity → FM-05 recovery
    │
    ▼
[11] Ball.ApplyKick(velocity, spinVector, agentId, matchTime)  [Ball Physics §3.1.11.2]
    │
    ▼
[12] ShotEventEmitter (§3.10)
    Publish ShotExecutedEvent
    Populate ShotAnimationData stub
    │
    ▼
[13] State machine → FOLLOW_THROUGH → COMPLETE → IDLE
```

---

## 2.3 Frame Pipeline Position

```
Frame Loop (60 Hz):
  Step 1: Input Processing
  Step 2: Decision Tree (#8) — may submit ShotRequest THIS frame
  Step 3: Agent Movement (#2) — AgentPhysicalProperties updated
  Step 4: Ball Physics (gravity, drag, Magnus — ongoing in-flight ball)
  Step 5: Collision System (#3) — broad/narrow phase; tackle interrupt flag set
  Step 6: *** SHOT MECHANICS *** ← This specification
           (CONTACT: reads tackle interrupt; if clear, executes full pipeline)
           (WINDUP: checks tackle interrupt flag; transitions or remains)
  Step 7: Ball Physics update (applies new BallState from ApplyKick if CONTACT fired)
  Step 8: Event Queue flush (ShotExecutedEvent, ShotCancelledEvent, etc.)
  Step 9: Render / Statistics
```

**Step ordering rationale:**
- Shot Mechanics executes AFTER Collision System (Step 5) to read the tackle interrupt
  flag set this frame.
- Shot Mechanics executes BEFORE Ball Physics Step 7 so the kick velocity is consumed
  in the same frame it is applied.
- Decision Tree (#8) executes BEFORE Shot Mechanics (Step 2) so a same-frame
  ShotRequest arrives in the Step 6 INITIATING check. The request enters WINDUP and the
  first WINDUP frame is the next frame.
- Agent Movement (Step 3) executes BEFORE Shot Mechanics (Step 6) so
  `AgentPhysicalProperties` (run-up angle, body lean, plant foot) reflect the agent's
  physical state at the moment the shot begins. These values are frozen at INITIATING.

---

## 2.4 Data Structures

All structures are value types (structs) to avoid heap allocation. No garbage collector
pressure during CONTACT execution. Consistent with Pass Mechanics §2.4 policy.

---

### 2.4.1 ShotRequest

Created by: Decision Tree (#8). Consumed by: ShotExecutor.

```csharp
public struct ShotRequest
{
    // Agent submitting the shot. Must have possession (validated in FR-01).
    public int AgentId;

    // Physical intent — how hard the agent attempts to strike the ball.
    // [0.0 = minimal force, 1.0 = maximum effort]
    // Drives velocity calculation (§3.2) and power–accuracy trade-off (§3.3).
    public float PowerIntent;          // range [0.0, 1.0]

    // Where on the ball face the foot contacts.
    // Centre = driven/low, BelowCentre = chip/lift, OffCentre = curl/sidespin.
    // Determines BaseAngle (§3.3), SpinBase (§3.4), ContactQualityModifier (§3.2).
    public ContactZone ContactZone;    // enum: Centre | BelowCentre | OffCentre

    // Degree of deliberate spin application.
    // [0.0 = pure power, 1.0 = maximum curl / chip spin]
    // Combined with ContactZone drives spin and launch angle.
    public float SpinIntent;           // range [0.0, 1.0]

    // Intended target on goal mouth — goal-relative normalised coordinates.
    // u = [0.0, 1.0] (left post → right post)
    // v = [0.0, 1.0] (ground → crossbar)
    // KD-2: Error applied in this space before world-space conversion.
    public Vector2 PlacementTarget;    // components each [0.0, 1.0]

    // True if agent is shooting with non-preferred foot.
    // Set by Decision Tree (#8). Shot Mechanics applies penalty (FR-09).
    // Shot Mechanics does NOT determine foot preference.
    public bool IsWeakFoot;

    // Distance to goal (metres) at moment of request submission.
    // Used for Finishing/LongShots sigmoid blend (FR-02, OI-001 resolution).
    public float DistanceToGoal;       // metres; must be > 0

    // Frame timestamp. Required for deterministic hash seed (OI-004 resolution).
    // matchSeed + agentId + frameNumber → deterministic error vector.
    public int FrameNumber;
}

public enum ContactZone
{
    Centre,       // Low, driven trajectory. High topspin base.
    BelowCentre,  // Lifted trajectory. High backspin base. Chip/loft.
    OffCentre     // Curling trajectory. High sidespin base.
}
```

---

### 2.4.2 ShotResult

Created by: ShotExecutor. Consumed by: ShotEventEmitter, diagnostics, replay system.

```csharp
public struct ShotResult
{
    // Execution outcome.
    public ShotOutcome Outcome;          // Completed | Cancelled | Invalid

    // Final velocity vector passed to Ball.ApplyKick(). Zero if Outcome != Completed.
    public Vector3 FinalVelocity;

    // Final spin vector passed to Ball.ApplyKick(). Zero if Outcome != Completed.
    public Vector3 FinalSpin;

    // World-space aim direction BEFORE error application. Exposed for debug and analytics.
    public Vector3 IntendedDirection;

    // World-space aim direction AFTER error application (actual kick direction).
    public Vector3 FinalDirection;

    // Error offset applied in goal-relative (u, v) space. Zero if Outcome != Completed.
    // Positive v = skied above target; negative u = pulled left of target.
    public Vector2 ErrorOffset;

    // Body mechanics score computed during this shot. Exposed for analytics and GK.
    public float BodyMechanicsScore;     // [0.0, 1.0]

    // Power penalty scalar applied. Exposed for post-shot analysis.
    public float PowerPenaltyApplied;    // [1.0, ∞)

    // Kick speed (m/s) before direction encoding. Exposed for GK difficulty estimation.
    public float KickSpeed;              // [8.0, 35.0]

    // Launch angle (degrees) used. Exposed for trajectory debugging.
    public float LaunchAngleDeg;

    // Frame on which Ball.ApplyKick() was called. Replay synchronisation.
    public int ContactFrame;
}

public enum ShotOutcome
{
    Completed,  // Ball.ApplyKick() called; ball in flight.
    Cancelled,  // Tackle interrupt during WINDUP; ball not kicked.
    Invalid     // ShotRequest failed validation (programming error; log only).
}
```

---

### 2.4.3 ShotExecutedEvent

Published to event bus (Event System Spec #17) on every successful `Ball.ApplyKick()`.
Fields are designed to satisfy Goalkeeper Mechanics (#11) when written (KD-5 / OI-005
resolution). No `IGkResponseSystem` interface is defined here.

```csharp
public struct ShotExecutedEvent
{
    // Agent who took the shot.
    public int ShootingAgentId;

    // Team of shooting agent. Provided for event filtering by subscribers.
    public int TeamId;

    // Final kick velocity (world space). GK uses this to compute interception trajectory.
    public Vector3 KickVelocity;

    // Final spin vector (world space). GK uses this to anticipate trajectory deviation.
    public Vector3 KickSpin;

    // Intended goal-relative placement target (before error). Used for xG analytics.
    public Vector2 IntendedTarget;      // (u, v) ∈ [0.0, 1.0]²

    // Actual kick direction (after error). Combined with KickVelocity, fully defines trajectory.
    public Vector3 FinalDirection;

    // Body mechanics quality. GK difficulty estimation: poor mechanics → irregular trajectory.
    public float BodyMechanicsScore;    // [0.0, 1.0]

    // Power intent from ShotRequest. Preserved for xG model (Stage 1+).
    public float PowerIntent;           // [0.0, 1.0]

    // ContactZone used. Preserved for trajectory class inference.
    public ContactZone ContactZone;

    // Distance to goal at shot time (metres).
    public float DistanceToGoal;

    // Match time (seconds) at Ball.ApplyKick() call. Replay and statistics.
    public float MatchTime;

    // Frame number at Ball.ApplyKick(). Replay determinism.
    public int ContactFrame;

    // Stumble triggered this shot. Informs GK that shooter may be off-balance.
    public bool StumbleTriggered;
}

public struct ShotCancelledEvent
{
    public int AgentId;
    public int TeamId;
    public int CancelFrame;             // Frame on which tackle interrupt fired.
    public ShotCancelReason Reason;     // TackleInterrupt (only valid reason at Stage 0)
}

public enum ShotCancelReason
{
    TackleInterrupt  // Collision System reported tackle during WINDUP.
}
```

---

### 2.4.4 ShotAnimationData (Stub)

Populated by ShotEventEmitter but **unconsumed at Stage 0**. Defined now to establish
the animation contract for Stage 1+ without creating a forward interface dependency.

```csharp
public struct ShotAnimationData
{
    public int AgentId;
    public ContactZone ContactZone;     // Animation system selects clip based on zone.
    public float PowerIntent;           // Drives animation blend (slow → explosive windup).
    public float BodyMechanicsScore;    // Poor mechanics → unbalanced animation variant.
    public bool IsWeakFoot;             // Selects foot-side animation variant.
    public int WindupFrames;            // Duration — allows animation system to sync.
    // NOTE: Animation System (Stage 1+) subscribes to event bus for this struct.
    // Shot Mechanics populates and publishes it; Animation System owns consumption.
}
```

---

## 2.5 Physical Parameter Reference Table

The following table maps intent parameter combinations to emergent physical output ranges.
These are reference points for testing, tuning, and Appendix B numerical verification —
not enum entries or hard-coded lookup values. All output values emerge from the governing
formulas in §3.2–3.4.

**Velocity and angle values are initial estimates.** They will be derived and validated
against the governing formulas in Appendix B before Section 3 is approved. Values marked
`[EST]` are based on StatsBomb shot velocity benchmarks, Lees & Nolan (1998), and Inoue
et al. (2014); they are subject to revision.

| Intent Profile Description | PowerIntent | ContactZone | SpinIntent | Estimated Velocity | Estimated Launch Angle |
|---------------------------|-------------|-------------|------------|-------------------|------------------------|
| Driven shot, maximum power | 0.85–1.0 | Centre | 0.0–0.1 | 28–35 m/s `[EST]` | 2–8° `[EST]` |
| Placed shot, accuracy-first | 0.40–0.65 | Centre | 0.0–0.2 | 14–22 m/s `[EST]` | 4–12° `[EST]` |
| Chip over advancing GK | 0.35–0.55 | BelowCentre | 0.6–1.0 | 10–18 m/s `[EST]` | 20–40° `[EST]` |
| Volley / dropping ball | 0.75–1.0 | Centre | 0.1–0.3 | 22–32 m/s `[EST]` | 0–15° `[EST]` |
| Curl to far post | 0.45–0.70 | OffCentre | 0.5–1.0 | 14–24 m/s `[EST]` | 5–18° `[EST]` |
| Stabbed/rushed shot | 0.30–0.55 | Centre/OffCentre | 0.0–0.3 | 10–20 m/s `[EST]` | 3–15° `[EST]` |

> **Note:** These ranges assume good body mechanics (`BodyMechanicsScore ≥ 0.7`).
> Poor body mechanics (`BodyMechanicsScore < 0.4`) adds ≥ 5° unintended loft and
> reduces velocity by up to 15%, producing outcomes outside these ranges intentionally.

---

## 2.6 Non-Functional Requirements

| NFR ID | Category | Requirement | Rationale / Source |
|--------|----------|-------------|--------------------|
| NFR-01 | Determinism | Identical inputs → bitwise-identical outputs across all machines and replay simulations | Master Vol 1 §1.3. Multiplayer replay fidelity. |
| NFR-02 | Performance | CONTACT execution ≤ 0.05ms on reference CPU (single shot, 22-agent match) | Discrete-event; ~20–40 CONTACT executions per match. Extremely generous budget. |
| NFR-03 | Performance | State machine advancement (non-CONTACT frames during WINDUP) ≤ 0.005ms per active shot per frame | At most one active shot per team. Well within 16.67ms frame budget. |
| NFR-04 | Testability | All sub-systems independently testable with pure function interfaces; no Unity dependency in logic layer | Required for CI/CD unit test automation. Development Best Practices §4. |
| NFR-05 | No stochastic elements | `System.Random`, `UnityEngine.Random`, and all unseeded noise forbidden | Subset of NFR-01. Error hash must use deterministic formula only. |
| NFR-06 | Boundary safety | No NaN, Infinity, or out-of-range velocity may reach `Ball.ApplyKick()` | NaN in Ball Physics causes simulation divergence. FM-05 guard enforces this. |
| NFR-07 | Attribute immutability | All attributes are captured and frozen at INITIATING; no mid-execution mutation | Mid-shot attribute change corrupts deterministic replay. Section 1 §1.6.1. |
| NFR-08 | Memory | Zero heap allocation during CONTACT execution path | No GC pressure during match-critical frames. All structs are value types. |

---

