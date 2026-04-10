# Pass Mechanics Specification #5 — Section 2: System Overview

**File:** `Pass_Mechanics_Spec_Section_2_v1_0.md`
**Purpose:** Establishes functional requirements, system architecture, frame pipeline
position, data structures, sub-system decomposition, performance constraints, and failure
modes for Pass Mechanics Specification #5. This section defines the "what" and "why"
before Section 3 defines the "how."

**Created:** February 20, 2026, 5:30 PM PST
**Version:** 1.0
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 5 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)
**Prerequisite:** Section 1 (Purpose & Scope) v1.0

**Open Dependency Flags:**
- `[ERR-007-PENDING]` — KickPower, WeakFootRating, Crossing absent from PlayerAttributes;
  amendment AM-002-001 drafted. Affects FR-02, FR-08. Does not block Section 2.
- `[ERR-008-PENDING]` — PossessingAgentId design (Option A vs B) unresolved in BallState.
  Affects possession loss on Ball.ApplyKick(). Does not block Section 2.

---

## Table of Contents

- [2.1 Functional Requirements](#21-functional-requirements)
- [2.2 System Architecture](#22-system-architecture)
  - [2.2.1 Architectural Position](#221-architectural-position)
  - [2.2.2 Sub-System Decomposition](#222-sub-system-decomposition)
  - [2.2.3 Evaluation Pipeline](#223-evaluation-pipeline)
- [2.3 Frame Pipeline Position](#23-frame-pipeline-position)
- [2.4 Data Structures](#24-data-structures)
  - [2.4.1 PassRequest](#241-passrequest)
  - [2.4.2 PassResult](#242-passresult)
  - [2.4.3 PhysicalProfile (Internal)](#243-physicalprofile-internal)
- [2.5 Non-Functional Requirements](#25-non-functional-requirements)
- [2.6 Failure Modes and Recovery](#26-failure-modes-and-recovery)
- [2.7 Open Dependency Flags](#27-open-dependency-flags)
- [2.8 Version History](#28-version-history)

---

## 2.1 Functional Requirements

Ten functional requirements govern the Pass Mechanics system for Stage 0. Each requirement
derives directly from the core responsibilities in Section 1.1 and maps to one or more
sub-systems in Section 3. All requirements include their Section 3 owner and test coverage
identifiers.

| FR ID | Short Name | Priority | Section 3 Owner | Test IDs |
|-------|------------|----------|-----------------|----------|
| FR-01 | Pass Type Classification | CRITICAL | §3.1 | PT-001–PT-008 (unit) |
| FR-02 | Velocity Calculation | CRITICAL | §3.2 | PV-001–PV-012 (unit) |
| FR-03 | Launch Angle Derivation | CRITICAL | §3.3 | LA-001–LA-008 (unit) |
| FR-04 | Spin Vector Calculation | HIGH | §3.4 | SV-001–SV-008 (unit) |
| FR-05 | Deterministic Error Model | CRITICAL | §3.5 | PE-001–PE-010 (unit) |
| FR-06 | Player-Targeted Resolution | CRITICAL | §3.6 | TR-001–TR-008 (unit) |
| FR-07 | Space-Targeted Resolution | HIGH | §3.6 | TR-009–TR-016 (unit) |
| FR-08 | Weak Foot Penalty | HIGH | §3.7 | WF-001–WF-006 (unit) |
| FR-09 | Execution State Machine | CRITICAL | §3.8 | PSM-001–PSM-006 (unit) |
| FR-10 | Event Publishing | MEDIUM | §3.9 | IT-001–IT-012 (integration) |

---

### FR-01 — Pass Type Classification

**Statement:** The system shall support all 7 pass types — Ground, Driven, Lofted,
ThroughBall (ground), ThroughBall (aerial), Cross, and Chip — as discrete enum values.
Each pass type shall have an associated physical profile defining velocity range, launch
angle range, and dominant spin type. The pass type is provided by the caller (Decision
Tree #7) on the `PassRequest` struct; Pass Mechanics does not select it.

**Inputs:**
- `PassRequest.PassType` (enum, 7 values)
- `PassRequest.CrossSubType` (optional enum; Flat by default — KD-6)

**Outputs:**
- `PhysicalProfile` record from `PassTypeProfiles.cs` — used by FR-02, FR-03, FR-04

**Rationale:** The discrete enum approach is more auditable and testable than a continuous
parameter space, and prevents emergent exploits from unconstrained parameter combinations.
The physical profile lookup is O(1) and executes in the INITIATING state (§2.2.3).

**Acceptance Criteria:**
1. All 7 `PassType` values produce a non-null `PhysicalProfile`.
2. Cross type with `CrossSubType.Whipped` produces a different velocity/angle profile than `Flat`.
3. Invalid `PassType` value produces FM-01 failure mode.
4. Profile lookup is deterministic: same `PassType` always returns identical profile.

---

### FR-02 — Velocity Calculation

**Statement:** The system shall compute a scalar kick speed (m/s) from pass type, intended
distance, agent `KickPower` attribute `[ERR-007-PENDING]`, and fatigue modifier. The
resulting speed shall be clamped to the pass type's `[V_MIN, V_MAX]` profile range.

**Inputs:**
- `PassRequest.PassType`, `PassRequest.IntendedDistance`
- `AgentAttributes.KickPower` `[ERR-007-PENDING]`
- `AgentState.Fatigue` [0.0–1.0]
- `PhysicalProfile.V_MIN`, `V_MAX`

**Outputs:**
- `kickSpeed: float` (m/s) — used in FR-03 to form the full velocity `Vector3`

**Rationale:** Distance-responsive velocity is required for physical plausibility — a 5m
ground pass and a 40m ground pass require very different force application. Fatigue
degradation models real physiological performance drops under match load.

**Acceptance Criteria:**
1. `kickSpeed` is always within `[PhysicalProfile.V_MIN, PhysicalProfile.V_MAX]`.
2. `Fatigue = 0.0` (fully rested) produces higher `kickSpeed` than `Fatigue = 1.0` (fully fatigued), all else equal.
3. `KickPower = 20` with `IntendedDistance` at maximum produces `kickSpeed = V_MAX`.
4. Calculation is deterministic: identical inputs produce bitwise-identical output.

`[ERR-007-PENDING]` — KickPower absent from PlayerAttributes; fallback documented in FM-02.

---

### FR-03 — Launch Angle Derivation

**Statement:** The system shall compute a launch angle (degrees above horizontal) from
pass type and intended distance. The angle shall fall within the pass type's
`[ANGLE_MIN, ANGLE_MAX]` profile range. The launch angle is combined with kick direction
and `kickSpeed` to produce the final velocity `Vector3` for `Ball.ApplyKick()`.

**Inputs:**
- `PassRequest.PassType`, `PassRequest.IntendedDistance`, `PassRequest.CrossSubType`
- `PhysicalProfile.ANGLE_MIN`, `ANGLE_MAX`

**Outputs:**
- `launchAngleDeg: float` — consumed by velocity Vector3 construction

**Rationale:** Launch angle determines trajectory shape and therefore reception height
and difficulty. Ground passes use 2°–5° to clear surface irregularities. Chip passes
require 45°–65° to clear defenders. Lofted passes use 20°–45° for distance-proportional
arc. Trig cost (`atan()`, `sin()`) is acceptable for a discrete-event system; lookup table
evaluation deferred to Section 6 (outline risk #4).

**Acceptance Criteria:**
1. Ground pass: angle ∈ [2°, 5°].
2. Driven pass: angle ∈ [5°, 12°].
3. Lofted pass (long): angle ∈ [20°, 45°].
4. Chip (short): angle ∈ [45°, 65°].
5. Cross Flat: angle ∈ [8°, 15°]; Cross High: angle ∈ [25°, 40°].
6. No trig function called if `IntendedDistance = 0` (FM-07 guard fires first).

---

### FR-04 — Spin Vector Calculation

**Statement:** The system shall compute a spin `Vector3` encoding topspin, backspin, or
sidespin magnitude according to pass type. Spin magnitude shall scale with the agent's
`Technique` attribute. The spin vector is passed to `Ball.ApplyKick()` where Ball Physics
applies Magnus force.

**Inputs:**
- `PassRequest.PassType`, `PassRequest.CrossSubType`
- `AgentAttributes.Technique` [1–20]
- `PhysicalProfile.SPIN_BASE`

**Outputs:**
- `spinVector: Vector3` — axis encodes spin direction; magnitude encodes spin rate (rad/s)

**Rationale:** Without spin, passes would produce unrealistic trajectories under Magnus
effect. Chip passes require backspin to check on landing. Curling crosses require sidespin
for arc into the box. Spin magnitude scaling with Technique models the skill required to
impart controlled spin.

**Acceptance Criteria:**
1. Ground pass: spin vector has positive y-component (topspin).
2. Chip pass: spin vector has negative y-component (backspin).
3. Cross (Whipped): spin vector has non-zero x-component (sidespin).
4. `Technique = 20` produces higher spin magnitude than `Technique = 1`.
5. Spin magnitude is within the Ball Physics Magnus force calibration envelope
   (cross-spec numerical check required during §3.4 drafting — outline risk #5).

---

### FR-05 — Deterministic Error Model

**Statement:** The system shall apply a deterministic angular error to the kick direction
vector. Error magnitude shall be derived from `Passing` attribute, pressure scalar,
fatigue, body orientation relative to target, and urgency. Given identical inputs, the
error vector shall be bitwise identical across all machines and replay simulations.
`System.Random`, `UnityEngine.Random`, and all unseeded noise are forbidden.

**Inputs:**
- `AgentAttributes.Passing` [1–20]
- `pressureScalar: float` [0.0–1.0] (from Collision System §3 spatial query)
- `AgentState.Fatigue` [0.0–1.0]
- `orientationDelta: float` (angle between `FacingDirection` and kick direction)
- `PassRequest.Urgency` [0.0–1.0]
- `WeakFootMultiplier: float` (from FR-08; applied to final error angle)

**Outputs:**
- `errorVector: Vector3` — rotates intended kick direction by `errorAngle` about `errorAxis`

**Rationale:** Determinism is non-negotiable for replay fidelity (Master Vol 1 §1.3).
Angular deviation produces physically realistic miss patterns — a pass is off-line, not
teleported to a random field position. The error inputs model the real causes of passing
inaccuracy: poor technique, physical pressure, fatigue, body angle, and rushed execution.

**Acceptance Criteria:**
1. 100 calls with identical inputs → 100 bitwise-identical error vectors.
2. `Passing = 20`, `pressure = 0.0`, `fatigue = 1.0`, `urgency = 0.0` → `errorAngle ≤ 1.5°`.
3. `Passing = 1`, `pressure = 1.0`, `fatigue = 0.0`, `urgency = 1.0` → `errorAngle ≥ 12°`.
4. `errorAngle` is always ≥ `MIN_ERROR_ANGLE` (no zero floor — elite passers are not perfect).
5. Body orientation 180° from kick direction applies maximum orientation penalty.

---

### FR-06 — Player-Targeted Pass Resolution

**Statement:** When `PassRequest.TargetAgentID ≥ 0`, the system shall compute the pass
aim point as the receiver's projected position using linear extrapolation of the receiver's
current velocity vector. For stationary receivers, aim point = `receiver.Position`.

**Inputs:**
- `PassRequest.TargetAgentID`
- `TargetAgent.Position`, `TargetAgent.Velocity` (from AgentState)
- `kickSpeed` (from FR-02)

**Outputs:**
- `aimPoint: Vector3` — target position the kick direction vector aims at before error is applied

**Rationale:** Player-targeted passes must lead a moving receiver or the ball arrives
behind them and is uncontrollable. Linear projection is the correct Stage 0 approach: it
models what a real player does (estimate where they'll be), and if that estimate is wrong
because the receiver changes direction, the pass misses — a correct failure mode, not a bug.

**Acceptance Criteria:**
1. Stationary receiver: `aimPoint = receiver.Position`.
2. Receiver moving at 5 m/s: `aimPoint` is ahead of `receiver.Position`.
3. Lead distance increases proportionally with receiver speed.
4. If projected `aimPoint` is off-pitch, it is clamped to pitch boundary (FM-05).

---

### FR-07 — Space-Targeted Pass Resolution (Through Ball)

**Statement:** When `PassRequest.TargetAgentID = -1`, the system shall compute a projected
interception point where the intended receiver arrives simultaneously with the ball.
Receiver movement is projected using linear velocity extrapolation (Stage 0 — KD-4). If no
valid interception point exists within pitch bounds, the pass executes to the raw
`TargetPosition`. Lead distance is stored in `PassResult.LeadDistance`.

**Inputs:**
- `PassRequest.TargetPosition`
- Intended receiver `Position`, `Velocity`
- `kickSpeed` (from FR-02)

**Outputs:**
- `aimPoint: Vector3` (interception point, or raw `TargetPosition` if fallback)
- `PassResult.LeadDistance: float` (metres ahead of receiver current position)

**Rationale:** Through balls require space-targeted aim to be effective. The simultaneous
arrival constraint is the physically correct solution. Linear projection is an explicit
Stage 0 simplification — receiver direction changes will cause misses, which is a realistic
outcome of poor through ball timing (KD-4).

**Acceptance Criteria:**
1. Receiver moving at 7 m/s: interception point is ahead of current position.
2. No valid interception point (receiver too slow): falls back to raw `TargetPosition`.
3. Interception point outside pitch: clamped to boundary.
4. `LeadDistance = 0` for stationary intended receiver.
5. Interception calculation is deterministic.

---

### FR-08 — Weak Foot Penalty

**Statement:** When `PassRequest.IsWeakFoot = true`, the system shall multiply the error
angle by a `WeakFootMultiplier` derived from `AgentAttributes.WeakFootRating`
`[ERR-007-PENDING]` on a 1–5 scale. `WeakFootRating = 5` → multiplier = 1.0 (no penalty).
`WeakFootRating = 1` → multiplier = `WEAK_FOOT_BASE_PENALTY` [GT]. Pass Mechanics does
not inspect preferred foot data — the `IsWeakFoot` flag is set by the Decision Tree (KD-5).

**Inputs:**
- `PassRequest.IsWeakFoot` (bool)
- `AgentAttributes.WeakFootRating` [1–5] `[ERR-007-PENDING]`

**Outputs:**
- `WeakFootMultiplier: float` ∈ `[WEAK_FOOT_BASE_PENALTY, 1.0]` — applied to `errorAngle` in FR-05

**Acceptance Criteria:**
1. `IsWeakFoot = false`: `WeakFootMultiplier = 1.0` regardless of `WeakFootRating`.
2. `IsWeakFoot = true`, `WeakFootRating = 5`: `WeakFootMultiplier = 1.0`.
3. `IsWeakFoot = true`, `WeakFootRating = 1`: `WeakFootMultiplier = WEAK_FOOT_BASE_PENALTY`.
4. `WeakFootMultiplier` always ∈ `[WEAK_FOOT_BASE_PENALTY, 1.0]`.

`[ERR-007-PENDING]` — WeakFootRating absent from PlayerAttributes; amendment AM-002-001 drafted.

---

### FR-09 — Execution State Machine

**Statement:** The system shall manage the full pass lifecycle across six states:
`IDLE → INITIATING → WINDUP → CONTACT → FOLLOW_THROUGH → IDLE`. WINDUP state is
interruptible by a tackle collision event from the Collision System. CONTACT is the
single frame in which `Ball.ApplyKick()` is called. FOLLOW_THROUGH is cosmetic with no
physics effect. A tackle interrupt during WINDUP transitions to IDLE without calling
`Ball.ApplyKick()`.

**Inputs:**
- `PassRequest` (triggers IDLE → INITIATING)
- Collision System tackle interrupt signal (during WINDUP)
- Per-frame timer decrement

**Outputs:**
- `PassResult.Outcome` (Completed | Cancelled | Invalid)
- State transitions that gate `Ball.ApplyKick()` and event publication

**Rationale:** The state machine is mandatory because pass execution spans multiple frames.
Without it, there is no mechanism to interrupt a pass in progress or to guard
`Ball.ApplyKick()` from being called at the wrong time.

**Acceptance Criteria:**
1. Valid request → full progression: `IDLE → INITIATING → WINDUP → CONTACT → FOLLOW_THROUGH → IDLE`.
2. Tackle interrupt during WINDUP → IDLE, `Outcome = Cancelled`, `Ball.ApplyKick()` not called.
3. Tackle interrupt during CONTACT → ignored; pass proceeds.
4. `Ball.ApplyKick()` called exactly once per CONTACT state entry.
5. Invalid request (no possession) → `IDLE → INITIATING → IDLE`, no further progression.

---

### FR-10 — Event Publishing

**Statement:** The system shall publish `PassAttemptEvent` at CONTACT state and
`PassCancelledEvent` on tackle interrupt during WINDUP. `PassCompletedEvent` and
`PassInterceptedEvent` are published by First Touch #4 and Ball Physics #1 respectively
— not by this system. Invalid request rejection is silent (logged only, no event published).

**Inputs:**
- State machine transitions (CONTACT entry, WINDUP tackle interrupt)
- `PassResult` struct (event payload)

**Outputs:**
- `PassAttemptEvent` → Event System #17
- `PassCancelledEvent` → Event System #17 (tackle interrupt only)

**Rationale:** Event ownership follows publish-at-source: the system that causes an
observable game event publishes it. Pass Mechanics causes the attempt (CONTACT) and the
cancellation (WINDUP interrupt). It cannot observe ball interception or reception — those
systems publish their own events (KD-8).

**Acceptance Criteria:**
1. `PassAttemptEvent` published exactly once per CONTACT state entry.
2. `PassCancelledEvent` published on WINDUP tackle interrupt; not published on invalid request.
3. Event payload includes `AgentID`, `PassType`, `TargetPosition`, `FrameNumber`.
4. No events published if state machine never reaches CONTACT.

---

## 2.2 System Architecture

### 2.2.1 Architectural Position

Pass Mechanics is a discrete event processor — it does not execute every frame. It
executes only when a `PassRequest` is submitted by the Decision Tree. Unlike First Touch
which executes in a single frame, Pass Mechanics spans multiple frames through its state
machine. INITIATING executes in one frame; WINDUP spans `TYPE_WINDUP_FRAMES` frames;
CONTACT executes in one frame; FOLLOW_THROUGH spans `TYPE_FOLLOWTHROUGH_FRAMES` frames.

In a 90-minute match at 60 Hz, Pass Mechanics executes approximately 800–1,200 pass
events total (estimate based on 500–800 passes per team at elite level). Total CONTACT
executions — the computationally expensive frame — is bounded by this count. This is a
very generous per-execution budget compared to per-frame systems.

---

### 2.2.2 Sub-System Decomposition

Pass Mechanics is decomposed into eight sub-systems. Each maps to one or more FRs and
has a corresponding Section 3 specification.

```
PassMechanics (PassExecutor.cs — main orchestrator)
├── PassTypeProfiler          [§3.1]  FR-01  — physical profile lookup
├── VelocityCalculator        [§3.2]  FR-02  — kick speed derivation
├── LaunchAngleCalculator     [§3.3]  FR-03  — elevation angle derivation
├── SpinVectorCalculator      [§3.4]  FR-04  — spin encoding
├── ErrorCalculator           [§3.5]  FR-05  — deterministic angular error
├── TargetResolver            [§3.6]  FR-06, FR-07 — aim point computation
│   ├── PlayerTargetResolver           FR-06
│   └── SpaceTargetResolver            FR-07
├── WeakFootPenaltyApplier    [§3.7]  FR-08  — weak foot multiplier
├── ExecutionStateMachine     [§3.8]  FR-09  — lifecycle management
└── PassEventEmitter          [§3.9]  FR-10  — event publishing
```

---

### 2.2.3 Evaluation Pipeline

Within a single pass execution at CONTACT state, sub-systems execute in strict sequential
order. Each sub-system depends on the output of the preceding one.

```
PassRequest received (Decision Tree #8)
    │
    ▼
[1] Validation Guard
    Agent has possession? Valid PassType? Non-zero IntendedDistance?
    FAIL → FM-01 / FM-07 → IDLE (no event, log only)
    │
    ▼
[2] PassTypeProfiler
    PhysicalProfile ← PassTypeProfiles[PassRequest.PassType, CrossSubType]
    │
    ▼
[3] TargetResolver
    aimPoint ← player-targeted OR space-targeted projection
    intendedDistance ← |aimPoint - agent.Position|
    │
    ▼
[4] VelocityCalculator
    kickSpeed ← f(PassType, intendedDistance, KickPower, Fatigue)
    kickSpeed ← clamp(kickSpeed, V_MIN, V_MAX)
    │
    ▼
[5] LaunchAngleCalculator
    launchAngle ← f(PassType, intendedDistance) ∈ [ANGLE_MIN, ANGLE_MAX]
    │
    ▼
[6] SpinVectorCalculator
    spinVector ← f(PassType, CrossSubType, Technique)
    │
    ▼
[7] ErrorCalculator + WeakFootPenaltyApplier
    errorAngle ← f(Passing, pressureScalar, Fatigue, orientDelta, Urgency)
    errorAngle ← errorAngle × WeakFootMultiplier
    errorVector ← RotateAboutAxis(kickDir, errorAxis, errorAngle)
    │
    ▼
[8] Velocity Vector3 Construction
    finalDir   ← Normalize(aimPoint - agent.Position) rotated by errorVector
    velocityVec ← finalDir × kickSpeed × cos(launchAngle)
                  + UP × kickSpeed × sin(launchAngle)
    │
    ▼
[9] Ball.ApplyKick(velocityVec, spinVector, agentId, matchTime)
    Ball Physics takes ownership of ball trajectory
    │
    ▼
[10] PassEventEmitter
    PassAttemptEvent → Event System #17
    │
    ▼
    State → FOLLOW_THROUGH → IDLE
```

---

## 2.3 Frame Pipeline Position

Pass Mechanics participates in the frame loop in two distinct modes: state machine
advancement (every frame while a pass is in progress) and CONTACT execution (the single
frame in which all physics calculations occur and `Ball.ApplyKick()` is called).

```
Frame N (60Hz, ~16.67ms budget):
  Step 1:  Input processing
  Step 2:  Decision Tree heartbeat (10Hz) — may emit PassRequest
  Step 3:  Agent Movement — update positions and velocities
  Step 4:  Ball Physics — apply gravity, drag, Magnus (pre-kick)
  Step 5:  Collision System — broad phase → narrow phase → events
           Step 5a: Agent-agent collisions → Collision Response
           Step 5b: Agent-ball contacts → First Touch (#4) evaluation
           Step 5c: Tackle on agent in WINDUP → interrupt signal to Pass Mechanics
  Step 6:  *** PASS MECHANICS — State machine advancement ***
           If WINDUP and tackle interrupt received → IDLE; PassCancelledEvent
           If WINDUP timer expires this frame → advance to CONTACT
  Step 7:  *** PASS MECHANICS — CONTACT execution (if applicable) ***
           Execute full §2.2.3 pipeline (steps 1–10)
           Ball.ApplyKick() called synchronously
           Agent loses possession (Ball Physics updates PossessingAgentId)
           PassAttemptEvent published
  Step 8:  Ball Physics — apply new kick velocity within same frame
  Step 9:  Event System — flush PassAttemptEvent, PassCancelledEvent
  Step 10: FOLLOW_THROUGH timer advancement (cosmetic — no physics)
  Step 11: Rendering (interpolated)
```

**Key timing constraint:** `Ball.ApplyKick()` (Step 7) must complete within the same frame
as CONTACT state entry. It is called synchronously. Ball Physics applies the new velocity
in Step 8 of the same frame — there is no one-frame lag between kick and ball departure.

**Step ordering rationale:** Pass Mechanics (Steps 6–7) must execute after Collision System
(Step 5) because it consumes the tackle interrupt signal. Ball Physics Step 8 must execute
after `Ball.ApplyKick()` to immediately apply the new velocity. The Decision Tree heartbeat
(Step 2) runs at 10Hz — in frames where no heartbeat fires, no new `PassRequest` is
submitted.

---

## 2.4 Data Structures

### 2.4.1 PassRequest

Created by: Decision Tree (#8). Consumed by: PassExecutor. Lifetime: single pass
execution cycle (created → consumed → discarded).

```csharp
public struct PassRequest
{
    // Unique ID of the passing agent.
    public int AgentID;

    // Requested pass type. Set by Decision Tree. Pass Mechanics does not select it (KD-2).
    public PassType PassType;

    // Optional cross subtype; ignored for non-Cross pass types.
    // Default = Flat. Resolves OQ-1 (KD-6).
    public CrossSubType CrossSubType;   // default = Flat

    // Target encoding:
    //   Player-targeted: TargetAgentID >= 0; TargetPosition ignored.
    //   Space-targeted:  TargetAgentID = -1; TargetPosition valid.
    public int     TargetAgentID;       // -1 if space-targeted
    public Vector3 TargetPosition;      // world space; ignored if player-targeted

    // Intended distance in metres. Set by Decision Tree before submission.
    // Used for velocity and launch angle calculation.
    public float IntendedDistance;

    // Pass urgency [0.0–1.0]. Affects windup duration and error angle (§3.8, §3.5).
    // 0.0 = unhurried (full windup). 1.0 = rushed (minimum windup, max urgency penalty).
    public float Urgency;

    // True if pass requires non-preferred foot. Set by Decision Tree (KD-5).
    // Pass Mechanics applies penalty; does not determine foot preference.
    public bool IsWeakFoot;

    // Frame timestamp. Required for determinism and replay synchronisation.
    public int FrameNumber;
}
```

---

### 2.4.2 PassResult

Created by: PassExecutor. Consumed by: Event System (#17), replay system, analytics.

```csharp
public struct PassResult
{
    // Outcome of the pass execution attempt.
    public PassOutcome Outcome;             // Completed | Cancelled | Invalid

    // Velocity vector passed to Ball.ApplyKick(). Zero if Outcome != Completed.
    public Vector3 FinalVelocity;

    // Spin vector passed to Ball.ApplyKick(). Zero if Outcome != Completed.
    public Vector3 FinalSpin;

    // Aim point used (post-projection, pre-error). Exposed for analytics and debug.
    public Vector3 AimPoint;

    // Error angle applied in degrees. Zero if cancelled or invalid.
    // Exposed for post-match pass accuracy attribution.
    public float ErrorAngleDeg;

    // Lead distance: metres from receiver's current position to interception point.
    // Zero for player-targeted passes to stationary receivers.
    public float LeadDistance;

    // Pass type that was executed. Copied from PassRequest for event payload convenience.
    public PassType PassType;

    // Frame on which Ball.ApplyKick() was called. Used for replay synchronisation.
    public int ContactFrame;
}

public enum PassOutcome
{
    Completed,  // Ball.ApplyKick() was called; ball is in flight
    Cancelled,  // Tackle interrupt during WINDUP; ball not kicked
    Invalid     // PassRequest failed validation (programming error; logged only)
}
```

---

### 2.4.3 PhysicalProfile (Internal)

Internal data record. Not exposed across system boundaries. Populated from
`PassTypeProfiles.cs` constants at system initialisation.

```csharp
internal struct PhysicalProfile
{
    public float V_MIN;               // Minimum kick speed (m/s) for this pass type   [GT]
    public float V_MAX;               // Maximum kick speed (m/s) for this pass type   [GT]
    public float ANGLE_MIN;           // Minimum launch angle (degrees)                [GT]
    public float ANGLE_MAX;           // Maximum launch angle (degrees)                [GT]
    public SpinProfile Spin;          // Dominant spin type and base magnitude         [GT]
    public int WINDUP_FRAMES;         // Windup duration at Urgency = 0.0, 60Hz        [GT]
    public int FOLLOWTHROUGH_FRAMES;  // Follow-through duration, 60Hz                [GT]
}
// [GT] = Gameplay-Tuned. Requires playtest calibration. See §8 for scientific basis where available.
```

**⚠️ SUPERSEDED — The values below were provisional estimates from the initial draft. See §3.1.4 Master Physical Profile Table and §3.2.9 Constants Summary for authoritative values (which differ significantly from these).**

**Provisional physical profile values** (all [GT] — require calibration in §3):

| Pass Type | V_MIN m/s | V_MAX m/s | ANGLE_MIN | ANGLE_MAX | Dominant Spin | Windup Frames | FT Frames |
|-----------|-----------|-----------|-----------|-----------|---------------|---------------|-----------|
| Ground | 8 | 22 | 2° | 5° | Topspin | 8 | 6 |
| Driven | 18 | 30 | 5° | 12° | Topspin | 12 | 8 |
| Lofted | 12 | 24 | 20° | 45° | Topspin | 15 | 10 |
| ThroughBall | 10 | 22 | 2° | 5° | Topspin | 8 | 6 |
| AerialThrough | 12 | 22 | 15° | 28° | Topspin | 14 | 10 |
| Cross (Flat) | 16 | 28 | 10° | 18° | Sidespin | 12 | 8 |
| Cross (Whipped) | 18 | 30 | 12° | 20° | Sidespin | 12 | 8 |
| Cross (High) | 12 | 22 | 25° | 35° | Sidespin | 14 | 10 |
| Chip | 8 | 18 | 45° | 65° | Backspin | 10 | 8 |

> **Note:** V_MAX values require numerical validation against Ball Physics drag model before
> Section 3 is approved. A too-high V_MAX could produce passes that exit the pitch before
> meaningful deceleration. Cross subtypes having distinct profiles confirms KD-6 (discrete
> enum over scalar `CrossCurlAmount`).

---

## 2.5 Non-Functional Requirements

| NFR ID | Category | Requirement | Rationale / Source |
|--------|----------|-------------|--------------------|
| NFR-01 | Determinism | Identical inputs → bitwise-identical outputs across all machines and replay simulations | Master Vol 1 §1.3. Required for multiplayer replay fidelity. |
| NFR-02 | Performance | CONTACT execution ≤ 0.05ms on reference CPU (single pass, 22-agent match) | Discrete-event; generous budget. Full analysis in Section 6. |
| NFR-03 | Performance | State machine advancement (non-CONTACT frames) ≤ 0.005ms per active pass per frame | At most one active pass per team per frame. Well within 16.67ms budget. |
| NFR-04 | Testability | All sub-systems independently testable with pure function interfaces (no Unity dependency in logic layer) | Required for CI/CD unit test automation. See Development Best Practices. |
| NFR-05 | No stochastic elements | `System.Random`, `UnityEngine.Random`, and all unseeded noise are forbidden | Subset of NFR-01. Explicit prohibition to prevent inadvertent introduction. |
| NFR-06 | Boundary safety | All outputs are bounded; no NaN, Infinity, or out-of-range values can reach `Ball.ApplyKick()` | NaN propagation into Ball Physics causes simulation divergence. FM-04 guard enforces this. |

---

## 2.6 Failure Modes and Recovery

All failure modes produce a valid `PassResult` and never leave the state machine in an
undefined state. `Ball.ApplyKick()` is never called with invalid data.

| FM ID | Failure | Detection Point | Recovery Action | Notes |
|-------|---------|-----------------|-----------------|-------|
| FM-01 | PassRequest for agent without possession | INITIATING validation | IDLE; `Outcome = Invalid`; log error; no event | Programming error in Decision Tree |
| FM-02 | `KickPower` attribute unavailable `[ERR-007-PENDING]` | VelocityCalculator | Substitute `Technique` as proxy; log warning; proceed | Temporary fallback until amendment approved |
| FM-03 | Tackle interrupt during WINDUP | State machine Step 6 of frame loop | IDLE; `Outcome = Cancelled`; `PassCancelledEvent` | Normal game event |
| FM-04 | NaN or Infinity in computed velocity | Post-computation guard before `ApplyKick()` | `Outcome = Cancelled`; log error; `Ball.ApplyKick()` not called | Should not occur with valid inputs; guard for safety |
| FM-05 | Aim point outside pitch boundary | TargetResolver post-projection | Clamp `aimPoint` to pitch bounds; log warning; proceed | Corner or sideline through ball |
| FM-06 | Tackle interrupt during CONTACT | State machine Step 6 | Ignore interrupt; pass proceeds | Ball already leaving foot — cannot be cancelled |
| FM-07 | `IntendedDistance = 0` or negative | INITIATING validation | IDLE; `Outcome = Invalid`; log error | Would cause undefined trig behaviour |
| FM-08 | Invalid `CrossSubType` for non-Cross `PassType` | PassTypeProfiler | Ignore `CrossSubType` field; log warning; proceed | Defensive handling for Decision Tree misconfiguration |

---

## 2.7 Open Dependency Flags

Neither flag blocks Section 2. Both flags block Section 3.

```
[ERR-007-PENDING] — KickPower, WeakFootRating, Crossing absent from PlayerAttributes
                    (Agent Movement Spec #2, §3.5.4). Amendment AM-002-001 drafted.
                    Affects FR-02, FR-08. Fallback documented in FM-02.

[ERR-008-PENDING] — PossessingAgentId design (Option A vs B) unresolved in BallState.
                    Affects possession loss on Ball.ApplyKick(). Blocks AM-001-001 approval.
                    Monitor before Section 3 integration contracts are written.
```

---

## 2.8 Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | February 20, 2026, 5:30 PM PST | Claude (AI) / Anton | Initial draft. 10 FRs (expanded from outline). NFR section added. Physical profile table included. FM table formalised. ERR-007/008 flags carried forward from Section 1. |
| 1.1 | March 25, 2026 | Claude (AI) / Anton | Post-audit fixes: Decision Tree #7→#8 (C-03, 2 instances); FR-02 fatigue convention corrected 1.0=rested→0.0=rested (C-04); §2.4.3 profile table marked SUPERSEDED by §3.1.4 (M-03); FR-03 Lofted angle range 35°→45°, Cross ranges aligned with §3.1 (Mod-01). |

---

*End of Section 2 — Pass Mechanics Specification #5*

*Next: Section 3 — Technical Specifications (§3.1 Pass Type Classification through §3.9 Event Publishing)*
