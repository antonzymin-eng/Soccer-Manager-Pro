# Shot Mechanics Specification #6 — Section 3 (Part 1): Technical Specifications §3.1–§3.3

**File:** `Shot_Mechanics_Spec_Section_3_1_to_3_3_v1_1.md`
**Purpose:** Defines the authoritative technical implementation for three Shot Mechanics
sub-systems: ShotRequest Validation (§3.1), Velocity Calculation (§3.2), and Launch
Angle Derivation (§3.3). These three sub-systems represent the first half of the
execution pipeline prior to spin calculation and error application. Every formula,
constant, and failure mode is specified to implementation-ready detail. This file is
the primary reference for `ShotValidator.cs`, `VelocityCalculator.cs`, and
`LaunchAngleCalculator.cs`.

**Created:** February 22, 2026, 11:45 PM PST
**Version:** 1.1
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 6 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)

**Prerequisites confirmed:**
- Shot Mechanics Outline v1.2 (approved — ShotType enum eliminated, all OIs resolved)
- Shot Mechanics Section 1 v1.1 (scope, KDs 1–7 locked)
- Shot Mechanics Section 2 v1.0 (data structures, FR-01–FR-11, 13-step pipeline)
- Ball Physics Spec #1 (approved) — Ball.ApplyKick() interface stable
- Agent Movement Spec #2 (approved) — PlayerAttributes including Finishing, LongShots,
  Composure, KickPower, WeakFootRating confirmed in §3.5.6
- Pass Mechanics Spec #5 (approved) — formula patterns and citation methodology reused

**Dependency Flags:** None. All hard dependencies confirmed stable.

**[GT] Convention:** Constants marked [GT] are gameplay-tunable. Stored in
`ShotConstants.cs`, never hardcoded in logic. Values are physically-motivated
estimates requiring calibration during playtesting. Tuning guidance is provided
for every [GT] constant.

**[EST] Convention:** Reference range values marked [EST] are estimates derived from
literature and StatsBomb data. Must be validated against the Ball Physics drag model
simulation (Appendix B cross-check) before Section 3 finalisation.

---

## Table of Contents

- [3.1 ShotRequest Validation](#31-shotrequest-validation)
  - [3.1.1 Responsibilities and Scope](#311-responsibilities-and-scope)
  - [3.1.2 Inputs and Outputs](#312-inputs-and-outputs)
  - [3.1.3 Validation Rule Set](#313-validation-rule-set)
  - [3.1.4 Validation Execution Order](#314-validation-execution-order)
  - [3.1.5 Failure Response Policy](#315-failure-response-policy)
  - [3.1.6 Constants Reference](#316-constants-reference)
  - [3.1.7 Failure Modes](#317-failure-modes)
  - [3.1.8 Design Decisions and Rationale](#318-design-decisions-and-rationale)
- [3.2 Velocity Calculation](#32-velocity-calculation)
  - [3.2.1 Responsibilities and Scope](#321-responsibilities-and-scope)
  - [3.2.2 Inputs and Outputs](#322-inputs-and-outputs)
  - [3.2.3 Attribute Blend — Finishing / LongShots Sigmoid](#323-attribute-blend--finishing--longshots-sigmoid)
  - [3.2.4 Base Velocity Formula — V_BASE](#324-base-velocity-formula--v_base)
  - [3.2.5 ContactZone Velocity Modifier](#325-contactzone-velocity-modifier)
  - [3.2.6 Spin–Velocity Trade-off](#326-spinvelocity-trade-off)
  - [3.2.7 Fatigue Modifier](#327-fatigue-modifier)
  - [3.2.8 ContactQualityModifier](#328-contactqualitymodifier)
  - [3.2.9 Full Formula — Implementation Reference](#329-full-formula--implementation-reference)
  - [3.2.10 Velocity Clamping Policy](#3210-velocity-clamping-policy)
  - [3.2.11 Boundary Verification Table](#3211-boundary-verification-table)
  - [3.2.12 Constants Reference](#3212-constants-reference)
  - [3.2.13 Failure Modes](#3213-failure-modes)
  - [3.2.14 Design Decisions and Rationale](#3214-design-decisions-and-rationale)
  - [3.2.15 Cross-Specification Dependencies](#3215-cross-specification-dependencies)
- [3.3 Launch Angle Derivation](#33-launch-angle-derivation)
  - [3.3.1 Responsibilities and Scope](#331-responsibilities-and-scope)
  - [3.3.2 Inputs and Outputs](#332-inputs-and-outputs)
  - [3.3.3 Base Angle by ContactZone](#333-base-angle-by-contactzone)
  - [3.3.4 Power Lift Modifier](#334-power-lift-modifier)
  - [3.3.5 Spin Lift Modifier](#335-spin-lift-modifier)
  - [3.3.6 Body Lean Penalty](#336-body-lean-penalty)
  - [3.3.7 Body Shape Penalty](#337-body-shape-penalty)
  - [3.3.8 Full Formula — Implementation Reference](#338-full-formula--implementation-reference)
  - [3.3.9 Launch Angle Clamping Policy](#339-launch-angle-clamping-policy)
  - [3.3.10 Boundary Verification Table](#3310-boundary-verification-table)
  - [3.3.11 Constants Reference](#3311-constants-reference)
  - [3.3.12 Failure Modes](#3312-failure-modes)
  - [3.3.13 Design Decisions and Rationale](#3313-design-decisions-and-rationale)
  - [3.3.14 Cross-Specification Dependencies](#3314-cross-specification-dependencies)
- [Section 3 (Part 1) Summary](#section-3-part-1-summary)

---

## 3.1 ShotRequest Validation

### 3.1.1 Responsibilities and Scope

§3.1 (ShotValidator) is the mandatory first stage of the shot execution pipeline.
It receives every inbound `ShotRequest`, checks all fields against their legal ranges,
and either grants permission to continue or terminates the pipeline with a logged
`ShotResult{ Outcome = ShotOutcome.Invalid }`.

§3.1 **does**:
- Verify agent possession state against `BallState.PossessingAgentId`
- Validate every numeric field in `ShotRequest` against its declared legal range
- Validate `ContactZone` enum membership
- Produce a pass/fail gate output — it does not modify any field

§3.1 **does not**:
- Clamp or correct out-of-range values (no silent fixing — callers must send valid data)
- Apply business logic or tactical rules (e.g., "agent is too far away to shoot")
- Access Ball Physics or Agent Movement directly — it reads only `ShotRequest` and
  `BallState.PossessingAgentId`

**Rationale:** Shot execution applies extreme velocities (20–35 m/s) to the ball.
A malformed request reaching Ball Physics without validation could cause simulation
divergence. Validation is the cheapest defence and should never be skipped.
Clamping-instead-of-rejecting was considered and rejected — silent correction would
mask upstream bugs in the Decision Tree.

---

### 3.1.2 Inputs and Outputs

**Inputs:**

| Input | Source | Type | Range |
|-------|--------|------|-------|
| `request.ShootingAgentId` | ShotRequest | int | > 0 |
| `request.PowerIntent` | ShotRequest | float | [0.0, 1.0] |
| `request.SpinIntent` | ShotRequest | float | [0.0, 1.0] |
| `request.ContactZone` | ShotRequest | ContactZone enum | {Centre, BelowCentre, OffCentre} |
| `request.PlacementTarget.u` | ShotRequest | float | [0.0, 1.0] |
| `request.PlacementTarget.v` | ShotRequest | float | [0.0, 1.0] |
| `request.DistanceToGoal` | ShotRequest | float | > 0.0 |
| `request.FrameNumber` | ShotRequest | int | > 0 |
| `BallState.PossessingAgentId` | BallState (Ball Physics §3.1.11.1) | int | any |

**Outputs:**

| Output | Consumer | Notes |
|--------|----------|-------|
| Pass gate (implicit — execution continues to §3.2 at CONTACT) | ShotExecutor orchestrator | No output struct; failure terminates via early return |
| `ShotResult{ Outcome = ShotOutcome.Invalid }` | ShotEventEmitter (§3.10), diagnostics | Returned on any rule failure; `Ball.ApplyKick()` is NOT called |

---

### 3.1.3 Validation Rule Set

Eight validation rules are applied in strict sequential order. The first failure
terminates validation and returns `ShotOutcome.Invalid`. Later rules are not evaluated.

| Rule ID | Field | Condition | Failure Message |
|---------|-------|-----------|-----------------|
| VR-01 | `ShootingAgentId` | `AgentSystem.GetPossessor() == request.ShootingAgentId` — ERR-008 Option B: no `PossessingAgentId` on `BallState`; possession is agent-side state queried via agent system | "ShotRequest: agent {id} does not have possession" |
| VR-02 | `PowerIntent` | `request.PowerIntent >= 0.0f && request.PowerIntent <= 1.0f` | "ShotRequest: PowerIntent {v} out of range [0,1]" |
| VR-03 | `SpinIntent` | `request.SpinIntent >= 0.0f && request.SpinIntent <= 1.0f` | "ShotRequest: SpinIntent {v} out of range [0,1]" |
| VR-04 | `ContactZone` | `Enum.IsDefined(typeof(ContactZone), request.ContactZone)` | "ShotRequest: ContactZone {v} is not a valid enum member" |
| VR-05 | `PlacementTarget.u` | `request.PlacementTarget.x >= 0.0f && request.PlacementTarget.x <= 1.0f` | "ShotRequest: PlacementTarget.u {v} out of range [0,1]" |
| VR-06 | `PlacementTarget.v` | `request.PlacementTarget.y >= 0.0f && request.PlacementTarget.y <= 1.0f` | "ShotRequest: PlacementTarget.v {v} out of range [0,1]" |
| VR-07 | `DistanceToGoal` | `request.DistanceToGoal > 0.0f` | "ShotRequest: DistanceToGoal {v} must be > 0" |
| VR-08 | `FrameNumber` | `request.FrameNumber > 0` | "ShotRequest: FrameNumber {v} must be > 0" |

**Note on VR-01:** Possession check uses the agent system's possession record, not a
`BallState` field — ERR-008 was resolved as Option B (Ball Physics §3.1.11.2): `BallState`
has no `PossessingAgentId`. The check is `AgentSystem.GetPossessor() == request.ShootingAgentId`.
If the Decision Tree submits a `ShotRequest` for an agent that lost possession during the
same frame (e.g., tackle resolved earlier in the frame), VR-01 will correctly reject the
request. This is the intended behaviour — no shot should fire for an agent who no longer
controls the ball.

**Note on VR-04:** `Enum.IsDefined` guards against cast-from-integer bugs in calling
code. A `ContactZone` cast from an out-of-range integer would pass a range check but
fail VR-04. This is a programming-error guard, not a runtime gameplay guard.

---

### 3.1.4 Validation Execution Order

```
ShotRequest received
    │
    ├─ VR-01: Possession check       FAIL → Invalid (possession mismatch)
    ├─ VR-02: PowerIntent range      FAIL → Invalid (bad float)
    ├─ VR-03: SpinIntent range       FAIL → Invalid (bad float)
    ├─ VR-04: ContactZone enum       FAIL → Invalid (bad enum)
    ├─ VR-05: PlacementTarget.u      FAIL → Invalid (bad float)
    ├─ VR-06: PlacementTarget.v      FAIL → Invalid (bad float)
    ├─ VR-07: DistanceToGoal > 0     FAIL → Invalid (non-positive distance)
    └─ VR-08: FrameNumber > 0        FAIL → Invalid (bad frame)
    │
    PASS → Advance to WINDUP; proceed to §3.7 (BodyMechanicsEvaluator)
```

The order places the most computationally inexpensive and conceptually critical check
(possession) first. Float-range checks follow because they are O(1) comparisons.
Enum validation is slightly more expensive (`IsDefined` may involve reflection in
some runtimes — profile and replace with a direct bounds check if needed at 60 Hz).

---

### 3.1.5 Failure Response Policy

On any VR rule failure:

1. Log diagnostic error at `LogLevel.Error` with the failure message and full
   `ShotRequest` field dump for debugging. This is a programming error, not a
   gameplay outcome — every occurrence should be investigated.
2. Return `ShotResult { Outcome = ShotOutcome.Invalid }` immediately.
3. Do **not** call `Ball.ApplyKick()`. Ball state is unchanged.
4. Do **not** publish `ShotExecutedEvent`. Publish `ShotCancelledEvent` with
   `CancelReason = ValidationFailure` for diagnostic tracking only.
5. Transition state machine to `IDLE` (§3.9).

**Note on NaN / Infinity inputs:** IEEE 754 float comparisons involving NaN return
`false` for all comparisons including `==`. A `PowerIntent = NaN` input will fail
VR-02 (`NaN >= 0.0f` is false), producing `Invalid` — correct behaviour without
special-casing. An explicit `float.IsNaN` guard is not required but is acceptable
as a documentation aid if the implementation team prefers explicit clarity.

---

### 3.1.6 Constants Reference

§3.1 has no tunable constants. All validation bounds are fixed by the data structure
contracts defined in Section 2 (§2.4.1) and the `ContactZone` enum definition.

---

### 3.1.7 Failure Modes

| FM ID | Trigger | Response | Origin |
|-------|---------|----------|--------|
| FM-V01 | Agent does not have possession (VR-01) | Invalid, no kick, log Error | Decision Tree bug — agent lost possession before shot committed |
| FM-V02 | Any float field out of range (VR-02–VR-03, VR-05–VR-07) | Invalid, no kick, log Error | Caller contract violation |
| FM-V03 | ContactZone not a valid enum member (VR-04) | Invalid, no kick, log Error | Integer cast bug in caller |
| FM-V04 | FrameNumber ≤ 0 (VR-08) | Invalid, no kick, log Error | Frame counter bug; deterministic hash seed would be corrupted |

All failure modes produce valid `ShotResult` structs. `Ball.ApplyKick()` is never
called with invalid data.

---

### 3.1.8 Design Decisions and Rationale

**DD-3.1-01: Fail-fast on first rule violation**

Early-exit on first failure rather than collecting all failures because: (a) in a
running simulation at 60 Hz, collecting all failures provides negligible diagnostic
benefit over logging the first failure; (b) early exit avoids evaluating later rules
on demonstrably bad data; (c) a `ShotRequest` with multiple field errors is
categorically a programming bug, not a data distribution question.

**DD-3.1-02: No silent clamping**

`PowerIntent = 1.05f` is rejected, not clamped to `1.0f`. Silent clamping would allow
the Decision Tree to generate out-of-contract requests and never observe the bug.
Reject-and-log surfaces the problem immediately. This policy mirrors Pass Mechanics
§3.1 (PassRequest validation).

**DD-3.1-03: Possession check uses integer ID equality**

Possession is a hard boolean state — an agent either has the ball or does not. A fuzzy
check (e.g., "agent is within proximity of ball") would duplicate logic that belongs
to the Collision System. The ID-equality check is the minimal, correct implementation.

---

## 3.2 Velocity Calculation

### 3.2.1 Responsibilities and Scope

§3.2 (VelocityCalculator) computes the **scalar kick speed** (m/s) for a committed shot.
It is called once per shot execution during the WINDUP-to-CONTACT transition, after
§3.7 (BodyMechanicsEvaluator) has computed `BodyMechanicsScore` and
`ContactQualityModifier`. The output scalar is later combined with the aim direction
vector from §3.5 (PlacementResolver) and the launch angle from §3.3 to form the
final `velocity: Vector3` passed to `Ball.ApplyKick()`.

§3.2 **does**:
- Blend `Finishing` and `LongShots` attributes by distance via sigmoid function
- Compute `V_BASE` from `PowerIntent` and blended effective attribute
- Apply `ContactZoneModifier`, spin–velocity trade-off, fatigue, and contact quality
- Clamp output to `[V_ABSOLUTE_MIN, V_ABSOLUTE_MAX]`

§3.2 **does not**:
- Compute launch angle (§3.3)
- Compute spin vector (§3.4)
- Apply placement error (§3.6)
- Apply weak foot penalty to velocity (§3.8 does this — §3.2 receives the
  `WeakFootModifier` scalar as an input from §3.8, not as an internal computation)

**Position in pipeline:** §3.2 executes third in the evaluation chain (after §3.1
validation and §3.7 body mechanics evaluation). See §2.2.3 for the full 13-step pipeline.

---

### 3.2.2 Inputs and Outputs

**Inputs:**

| Input | Source | Type | Range | Notes |
|-------|--------|------|-------|-------|
| `request.PowerIntent` | ShotRequest | float | [0.0, 1.0] | Pre-validated by §3.1 |
| `request.SpinIntent` | ShotRequest | float | [0.0, 1.0] | Pre-validated by §3.1 |
| `request.ContactZone` | ShotRequest | ContactZone | enum | Pre-validated by §3.1 |
| `request.DistanceToGoal` | ShotRequest | float | > 0 | Pre-validated by §3.1 |
| `agentAttributes.Finishing` | PlayerAttributes (Agent Movement §3.5.6) | float | [1.0, 20.0] | Clamped to ATTR_MAX before use |
| `agentAttributes.LongShots` | PlayerAttributes (Agent Movement §3.5.6) | float | [1.0, 20.0] | Clamped to ATTR_MAX before use |
| `agentAttributes.KickPower` | PlayerAttributes (Agent Movement §3.5.6) | float | [1.0, 20.0] | Secondary ceiling modifier |
| `agentState.Fatigue` | AgentState | float | [0.0, 1.0] | 0 = fully rested; 1 = fully fatigued |
| `contactQualityModifier` | §3.7 BodyMechanicsEvaluator output | float | [0.7, 1.0] | Degrades if body shape is poor |
| `weakFootModifier` | §3.8 WeakFootPenaltyApplier output | float | [0.6, 1.0] | 1.0 = strong foot; <1.0 = weak foot penalty |

**Output:**

| Output | Destination | Type | Range |
|--------|-------------|------|-------|
| `kickSpeed` | §3.3 (LaunchAngleCalculator), §3.5 (PlacementResolver), `Ball.ApplyKick()` | float (m/s) | [V_ABSOLUTE_MIN, V_ABSOLUTE_MAX] = [8.0, 35.0] |

---

### 3.2.3 Attribute Blend — Finishing / LongShots Sigmoid

Shot velocity at any distance is governed by a blend of two attributes:
- **Finishing** — technique and composure in the penalty area; dominant within ~20m
- **LongShots** — power generation and accuracy from range; dominant beyond ~20m

The blend is continuous (OI-001 resolution — Option B). A hard threshold at 20m was
explicitly rejected because it would produce a discontinuity in gameplay feel: a player
at 19.9m and 20.1m should have near-identical effective attributes.

**Sigmoid weight function:**

```
w(d) = 1 / (1 + exp(−(d − D_MID) / D_SCALE))
```

Where:
- `d` = `ShotRequest.DistanceToGoal` (metres)
- `D_MID` = 20.0 [GT] — blend midpoint; approximately the penalty area edge
- `D_SCALE` = 8.0 [GT] — controls blend sharpness; larger values = gentler transition

**Weight interpretation:**

| Distance (m) | w(d) | Interpretation |
|---|---|---|
| 5 | ≈ 0.04 | ~96% Finishing, ~4% LongShots |
| 10 | ≈ 0.18 | ~82% Finishing, ~18% LongShots |
| 20 | 0.50 | Equal blend |
| 30 | ≈ 0.77 | ~23% Finishing, ~77% LongShots |
| 40 | ≈ 0.92 | ~8% Finishing, ~92% LongShots |

**Effective attribute:**

```
EffectiveAttribute = (1.0f − w(d)) × Finishing + w(d) × LongShots
```

`EffectiveAttribute` ∈ [1.0, 20.0] always (a convex combination of values in
the same range). No additional clamping required before formula application, though
both `Finishing` and `LongShots` are individually clamped to `[1.0, ATTR_MAX]`
as a defensive guard against data errors.

**Example — Elite striker (Finishing 18, LongShots 12) at 25m:**

```
w(25) = 1 / (1 + exp(−(25−20)/8)) = 1 / (1 + exp(−0.625)) ≈ 0.651
EffectiveAttribute = (1 − 0.651) × 18 + 0.651 × 12 ≈ 6.28 + 7.81 ≈ 14.09
```

The player is meaningfully better with Finishing in close but both attributes contribute
at 25m. A long-shot specialist (Finishing 8, LongShots 18) would have ≈ 14.50 at 25m —
nearly equal, with their advantage growing sharply beyond 30m.

---

### 3.2.4 Base Velocity Formula — V_BASE

`V_BASE` translates normalised effective attribute and power intent to a raw velocity
before secondary modifiers:

```
V_BASE = V_FLOOR + (EffectiveAttribute / ATTR_MAX) × (V_CEILING − V_FLOOR) × PowerIntent
```

Where:
- `V_FLOOR` = 10.0 m/s [GT] — minimum achievable shot velocity; a ball struck with
  any intent by any agent must clear the goalkeeper's immediate reach
- `V_CEILING` = 35.0 m/s — maximum achievable shot velocity; constrained by
  biomechanical limits confirmed in literature (Lees & Nolan 1998; StatsBomb elite data)
- `ATTR_MAX` = 20.0 (Master Vol 2 §PlayerAttributes — design authority)

**Design note — V_FLOOR vs. V_ABSOLUTE_MIN:**

`V_FLOOR` (10.0 m/s) is the minimum speed the formula can produce before secondary
modifiers. `V_ABSOLUTE_MIN` (8.0 m/s) is the absolute safety clamp applied after all
modifiers. A weak foot penalty on a low-power shot by a poor finisher may reduce the
speed below V_FLOOR but should not reach below V_ABSOLUTE_MIN. If the post-modifier
speed falls below 8.0 m/s, clamping applies (§3.2.10).

**Monotonicity verification:**

```
∂V_BASE/∂PowerIntent = (EffectiveAttribute / ATTR_MAX) × (V_CEILING − V_FLOOR) > 0
                        for any EffectiveAttribute > 0                               ✓

∂V_BASE/∂EffectiveAttribute = (V_CEILING − V_FLOOR) × PowerIntent / ATTR_MAX ≥ 0
                               = 0 only when PowerIntent = 0.0                       ✓
```

At `PowerIntent = 0.0`, V_BASE = V_FLOOR regardless of EffectiveAttribute. This is
correct — a player who attempts zero power produces the minimum kick. At
`PowerIntent = 1.0` and `EffectiveAttribute = 20.0` (elite attribute, maximum effort):

```
V_BASE = 10.0 + (20.0/20.0) × (35.0 − 10.0) × 1.0 = 10.0 + 25.0 = 35.0 m/s
```

This confirms the ceiling is only reachable by a world-class player at maximum effort,
which is the intended design.

---

### 3.2.5 ContactZone Velocity Modifier

The physical contact point on the ball determines how much of the agent's maximum
mechanical power transfers to linear ball velocity:

| ContactZone | Modifier Value | Rationale |
|-------------|----------------|-----------|
| `Centre` | 1.00 [GT] | Full instep contact; maximum energy transfer to linear momentum |
| `OffCentre` | 0.85 [GT] | Partial instep; some energy diverts to sidespin. Consistent with [ASAI-2002] sidespin-velocity relationship |
| `BelowCentre` | 0.75 [GT] | Under-ball contact; significant energy transfers to backspin/loft; reduced linear velocity. Consistent with [INOUE-2014] spin-speed trade-off |

```
v_after_zone = V_BASE × ContactZoneModifier[ContactZone]
```

**Tuning guidance:** The 0.75 / 0.85 values are the primary control for how much slower
chips and curlers feel compared to driven shots at the same PowerIntent. Raising
`BelowCentre` above 0.80 will make chips feel unnaturally fast; lowering below 0.65
will make them feel impossibly soft. Adjust in 0.02 increments during playtesting.

---

### 3.2.6 Spin–Velocity Trade-off

A player applying deliberate spin cannot simultaneously achieve maximum linear velocity.
Backspin (chip) and sidespin (curl) require the foot to strike across or under the ball,
partially sacrificing the clean instep contact that maximises pace.

```
SPIN_VELOCITY_TRADE_OFF = 0.25 [GT]

v_after_spin = v_after_zone × (1.0f − SpinIntent × SPIN_VELOCITY_TRADE_OFF)
```

**Effect at extremes:**
- `SpinIntent = 0.0` (pure power): no trade-off; `v_after_spin = v_after_zone`
- `SpinIntent = 1.0` (maximum deliberate spin): 25% velocity reduction

**Interaction with ContactZone:** A `BelowCentre`, `SpinIntent = 1.0` chip shot suffers
both the ContactZone penalty (×0.75) and the spin trade-off (×0.75), producing a ball
speed ≈ 56% of the equivalent Centre-contact, pure-power shot. This is physically
appropriate — chip shots are characteristically slow.

**Tuning guidance:** `SPIN_VELOCITY_TRADE_OFF ∈ [0.15, 0.35]` is the expected tuning
range. Values below 0.15 make curl shots feel unrealistically fast; values above 0.35
make them feel too slow to be dangerous.

---

### 3.2.7 Fatigue Modifier

```
FATIGUE_POWER_REDUCTION = 0.20 [GT]

FatigueModifier = 1.0f − (agentState.Fatigue × FATIGUE_POWER_REDUCTION)
```

| Fatigue | FatigueModifier | Velocity impact |
|---------|----------------|----------------|
| 0.0 (fully rested) | 1.00 | None |
| 0.25 | 0.95 | 5% reduction |
| 0.50 | 0.90 | 10% reduction |
| 0.75 | 0.85 | 15% reduction |
| 1.0 (fully fatigued) | 0.80 | 20% reduction |

`FatigueModifier ∈ [0.80, 1.00]` — it only reduces velocity, never amplifies it.
Minimum value at full fatigue = `1.0 − FATIGUE_POWER_REDUCTION = 0.80 > 0`.

**Academic support:** [ALI-2011] confirms monotonic degradation of peak force output
with cumulative fatigue in football-specific physical tests. The 20% maximum reduction
is a design choice within the literature-informed range of 15–30%.

**Note:** `FATIGUE_POWER_REDUCTION` (velocity) is independent of
`FATIGUE_ACCURACY_REDUCTION` (error angle, §3.6). They are tuned separately.

---

### 3.2.8 ContactQualityModifier

`ContactQualityModifier` is produced by §3.7 (BodyMechanicsEvaluator) and represents
the degree to which the agent's physical stance supported clean ball contact.

```
ContactQualityModifier ∈ [CONTACT_QUALITY_MIN, 1.0]
CONTACT_QUALITY_MIN = 0.70 [GT]
```

A value of 1.0 indicates optimal stance — the agent achieved maximum energy transfer
given their ContactZone. A value of 0.70 (the floor) indicates severely poor stance —
stumbling, off-balance, or cramped — producing a 30% velocity reduction.

§3.2 receives this value as an input from §3.7. §3.2 does not compute it.

---

### 3.2.9 Full Formula — Implementation Reference

All modifiers applied in sequence. This is the canonical implementation form.

```
// Step 1: Attribute blend
float w = Sigmoid(distanceToGoal, D_MID, D_SCALE);
float effectiveAttr = Mathf.Lerp(finishing, longShots, w);
effectiveAttr = Mathf.Clamp(effectiveAttr, 1.0f, ATTR_MAX);

// Step 2: Base velocity
float vBase = V_FLOOR + (effectiveAttr / ATTR_MAX) * (V_CEILING - V_FLOOR) * powerIntent;

// Step 3: ContactZone modifier
float vZone = vBase * ContactZoneModifier[contactZone];

// Step 4: Spin–velocity trade-off
float vSpin = vZone * (1.0f - spinIntent * SPIN_VELOCITY_TRADE_OFF);

// Step 5: Fatigue
float vFatigue = vSpin * (1.0f - fatigue * FATIGUE_POWER_REDUCTION);

// Step 6: Contact quality (from §3.7)
float vContact = vFatigue * contactQualityModifier;

// Step 7: Weak foot (from §3.8)
float vFinal = vContact * weakFootModifier;

// Step 8: Absolute clamp
kickSpeed = Mathf.Clamp(vFinal, V_ABSOLUTE_MIN, V_ABSOLUTE_MAX);
```

Where:
```csharp
private static float Sigmoid(float d, float mid, float scale)
{
    return 1.0f / (1.0f + Mathf.Exp(-(d - mid) / scale));
}
```

**Implementation note:** The Sigmoid function uses `Mathf.Exp` (float precision).
At Stage 0 this is acceptable. Fixed64 migration (Stage 5+) will require a fixed-point
sigmoid approximation documented in Section 7 (Future Extensions).

---

### 3.2.10 Velocity Clamping Policy

```
kickSpeed = Mathf.Clamp(vFinal, V_ABSOLUTE_MIN, V_ABSOLUTE_MAX)

V_ABSOLUTE_MIN = 8.0 m/s   — absolute floor; prevents zero-velocity kick reaching Ball Physics
V_ABSOLUTE_MAX = 35.0 m/s  — absolute ceiling; prevents superhuman velocity regardless of attribute
```

Clamping to `V_ABSOLUTE_MAX` should be a rare event in normal gameplay (requires elite
attributes + maximum PowerIntent + optimal body mechanics + fully rested). If telemetry
shows it triggering frequently at match speed, `V_CEILING` or the modifier values
should be reviewed.

Clamping to `V_ABSOLUTE_MIN` should also be rare — only extreme penalty stacking
(full fatigue + poor body mechanics + weak foot + minimum power intent) reaches this floor.
Log at DEBUG level when clamping occurs; log at WARNING level if clamping occurs more
than 5% of shot executions in a test session.

---

### 3.2.11 Boundary Verification Table

All rows are [EST] — require validation against Ball Physics drag model simulation.

| Scenario | PowerIntent | EffAttr | Fatigue | BM | WF | Pre-clamp speed | Expected range |
|---|---|---|---|---|---|---|---|
| Elite striker, close, max power | 1.0 | 18.0 | 0.0 | 1.0 | 1.0 | ~32.5 | [30, 35] [EST] |
| Average player, mid range, 60% power | 0.6 | 10.0 | 0.3 | 0.9 | 1.0 | ~16.2 | [14, 20] [EST] |
| Poor finisher, full fatigue, half power | 0.5 | 6.0 | 1.0 | 0.85 | 1.0 | ~9.8 | [8, 12] [EST] |
| Chip shot (BelowCentre, SpinIntent 1.0) | 0.7 | 14.0 | 0.2 | 0.95 | 1.0 | ~13.9 | [10, 18] [EST] |
| Weak foot, moderate power | 0.65 | 12.0 | 0.4 | 0.9 | 0.72 | ~11.4 | [8, 16] [EST] |

⚠ **Validation required:** Run Ball Physics simulation at each row's pre-clamp speed
for the corresponding ContactZone to confirm the ball reaches a plausible field
position before decelerating below 3 m/s usable speed. Document in Appendix B.

---

### 3.2.12 Constants Reference

| Constant | Value | Source | Tuning Range | Notes |
|---|---|---|---|---|
| `V_FLOOR` | 10.0 m/s | [GT] | [8.0, 12.0] | Minimum V_BASE output before modifiers |
| `V_CEILING` | 35.0 m/s | ACADEMIC-INFORMED [LEES-1998], [STATSBOMB] | fixed | Maximum achievable shot speed; confirmed by elite data |
| `V_ABSOLUTE_MIN` | 8.0 m/s | [GT] | [6.0, 10.0] | Post-modifier safety floor; Ball Physics floor guard |
| `V_ABSOLUTE_MAX` | 35.0 m/s | ACADEMIC-INFORMED | fixed | = V_CEILING; post-modifier ceiling clamp |
| `ATTR_MAX` | 20.0 | [MASTER-VOL2] | fixed — design authority | Maximum player attribute value |
| `D_MID` | 20.0 m | [GT] | [16.0, 24.0] | Sigmoid blend midpoint; penalty area edge approximation |
| `D_SCALE` | 8.0 m | [GT] | [5.0, 12.0] | Sigmoid sharpness; larger = more gradual transition |
| `ContactZoneModifier[Centre]` | 1.00 | [GT] | fixed | Reference value; do not change |
| `ContactZoneModifier[OffCentre]` | 0.85 | [GT] | [0.78, 0.92] | Tune: curl shot pace vs. driven |
| `ContactZoneModifier[BelowCentre]` | 0.75 | [GT] | [0.65, 0.82] | Tune: chip shot pace vs. driven |
| `SPIN_VELOCITY_TRADE_OFF` | 0.25 | [GT] | [0.15, 0.35] | Max velocity sacrifice for max spin intent |
| `FATIGUE_POWER_REDUCTION` | 0.20 | ACADEMIC-INFORMED [ALI-2011] | [0.15, 0.28] | 20% velocity loss at full fatigue |
| `CONTACT_QUALITY_MIN` | 0.70 | [GT] | [0.60, 0.80] | Floor for ContactQualityModifier (from §3.7) |

---

### 3.2.13 Failure Modes

| FM ID | Trigger | Response |
|-------|---------|----------|
| FM-V02 | `DistanceToGoal = 0` or negative | Cannot reach §3.2 — caught by VR-07 in §3.1 |
| FM-V05 | Attribute read failure (Finishing or LongShots) | Use fallback `ATTR_MAX / 2 = 10.0`; log Warning; proceed |
| FM-V06 | `kickSpeed` NaN after formula (e.g., exp overflow on extreme input) | Apply FM-05 recovery: use `V_FLOOR`; log Error |
| FM-V07 | `kickSpeed` clamps to `V_ABSOLUTE_MIN` | Log DEBUG; not a failure mode unless frequent |
| FM-V08 | `kickSpeed` clamps to `V_ABSOLUTE_MAX` | Log DEBUG; investigate if persistent |

---

### 3.2.14 Design Decisions and Rationale

**DD-3.2-01: Sigmoid blend over hard distance threshold (OI-001 resolution)**

A hard threshold at 20m (e.g., `if distance < 20m use Finishing else use LongShots`)
would produce a discontinuous jump in effective attribute when an agent crosses the
threshold mid-run. A sigmoid ensures the transition is gradual and smooth. The
transition midpoint and sharpness are tunable without code changes.

**DD-3.2-02: PowerIntent scales V_BASE above V_FLOOR, not from zero**

`V_BASE = V_FLOOR + ... × PowerIntent` means that even at `PowerIntent = 0.0`, the
ball receives a non-zero velocity (V_FLOOR). This prevents physically implausible
zero-velocity kicks and ensures `Ball.ApplyKick()` always receives a meaningful speed.
A "trivial touch" in gameplay would use a very low PowerIntent, not zero.

**DD-3.2-03: Modifiers are applied multiplicatively, not additively**

Additive modifiers would allow combinations to drive velocity below zero. Multiplicative
application preserves the sign guarantee (all modifiers > 0) and maintains a predictable
compositional behaviour — each modifier scales the current value by a well-defined
fraction. This matches the Pass Mechanics pattern (§3.2, Spec #5).

**DD-3.2-04: `V_FLOOR ≠ V_ABSOLUTE_MIN`**

`V_FLOOR` (10.0) is the formula minimum before secondary modifiers. `V_ABSOLUTE_MIN`
(8.0) is the post-modifier safety clamp. This two-tier design means secondary modifiers
(fatigue, weak foot, contact quality) can push speed below V_FLOOR without triggering
an immediate clamp, while the absolute floor protects `Ball.ApplyKick()` from receiving
dangerously low velocities. Conflating the two would either over-restrict modifier
effects or allow zero-velocity reaches.

---

### 3.2.15 Cross-Specification Dependencies

| Dependency | Spec | Section | What is Required |
|---|---|---|---|
| `Finishing` attribute | Agent Movement #2 | §3.5.6 | Confirmed present; exact field name stable |
| `LongShots` attribute | Agent Movement #2 | §3.5.6 | Confirmed present; exact field name stable |
| `KickPower` attribute | Agent Movement #2 | §3.5.6 | Confirmed present; secondary ceiling modifier |
| `Fatigue` field | Agent Movement #2 | AgentState | Confirmed present |
| `ContactQualityModifier` | Shot Mechanics §3.7 | — | Computed by BodyMechanicsEvaluator; passed in |
| `WeakFootModifier` | Shot Mechanics §3.8 | — | Computed by WeakFootPenaltyApplier; passed in |
| `Ball.ApplyKick()` | Ball Physics #1 | §3.1.11.2 | kickSpeed is magnitude component of velocity arg |

---

## 3.3 Launch Angle Derivation

### 3.3.1 Responsibilities and Scope

§3.3 (LaunchAngleCalculator) computes the **vertical launch angle** (degrees above
horizontal) at which the ball leaves the foot. This angle, combined with the horizontal
aim direction from §3.5 (PlacementResolver), fully specifies the 3D velocity direction
passed to `Ball.ApplyKick()`.

Launch angle is **not** assigned from a per-type lookup table. It emerges from four
additive terms: a base angle determined by `ContactZone`, a power-driven loft modifier,
a spin-driven loft modifier, and body mechanics penalties. Named shot labels (driven,
chip, finesse) are irrelevant to this calculation — they are Decision Tree vocabulary.

§3.3 **does**:
- Select base launch angle from `ContactZone` (the only zone-to-angle mapping)
- Apply `PowerLiftModifier` (lower power → slightly more loft)
- Apply `SpinLiftModifier` (`BelowCentre` + high `SpinIntent` → chip arc)
- Apply `BodyLeanPenalty` from body lean measurement (§3.7 input)
- Apply `BodyShapePenalty` from `BodyMechanicsScore` (§3.7 input)
- Clamp output to `[LAUNCH_ANGLE_MIN, LAUNCH_ANGLE_MAX]`

§3.3 **does not**:
- Compute the horizontal aim direction (§3.5)
- Apply error deviation to the launch angle (§3.6 applies error in goal-relative (u,v)
  space; §3.3 operates only on the vertical dimension before error application)
- Compute body lean or body mechanics score (these are §3.7 outputs passed as inputs)

---

### 3.3.2 Inputs and Outputs

**Inputs:**

| Input | Source | Type | Range | Notes |
|-------|--------|------|-------|-------|
| `request.ContactZone` | ShotRequest | ContactZone | enum | Pre-validated by §3.1 |
| `request.PowerIntent` | ShotRequest | float | [0.0, 1.0] | Pre-validated by §3.1 |
| `request.SpinIntent` | ShotRequest | float | [0.0, 1.0] | Pre-validated by §3.1 |
| `bodyLeanAngleDeg` | §3.7 BodyMechanicsEvaluator | float | [−45°, +45°] | Negative = leaning forward; positive = leaning back |
| `bodyMechanicsScore` | §3.7 BodyMechanicsEvaluator | float | [0.0, 1.0] | Low score → high BodyShapePenalty |

**Output:**

| Output | Destination | Type | Range |
|--------|-------------|------|-------|
| `launchAngleDeg` | §3.5 PlacementResolver / velocity vector construction | float (degrees) | [LAUNCH_ANGLE_MIN, LAUNCH_ANGLE_MAX] = [−5°, 70°] |

---

### 3.3.3 Base Angle by ContactZone

The contact point on the ball face is the primary determinant of trajectory elevation.
This is the only zone-to-value lookup in the launch angle calculation.

| ContactZone | `BaseAngle` (degrees) | Source | Physical interpretation |
|---|---|---|---|
| `Centre` | 4.0° | [GT]; informed by [LEES-1998] (2–8° for full instep) | Low, driven trajectory typical of powered shots |
| `BelowCentre` | 18.0° | [GT]; informed by [INOUE-2014] (lift increases sharply with under-ball contact) | Chipped/lifted trajectory; ball scooped upward |
| `OffCentre` | 8.0° | [GT]; informed by [ASAI-2002] (curled shots have modest elevation) | Moderate lift; curl trajectory stays usable |

**Rationale for discrete base angles by zone rather than a continuous function:**

`ContactZone` is a discrete enum (three values). The decision to use a lookup table
rather than a continuous function of, e.g., estimated contact angle, is intentional:
the physical contact geometry (where on the ball face the foot strikes) is the Decision
Tree's intent signal, encoded as one of three canonical positions. A continuous mapping
would imply precision in contact point measurement that does not exist in the model.
The three values capture the three fundamentally different trajectory families.

---

### 3.3.4 Power Lift Modifier

At lower power, the foot tends to come through the ball with a slightly more upward
trajectory (less committed "through" the ball), producing marginally more loft. At
maximum power, the foot drives through with a flat, forceful trajectory, keeping the
ball low.

```
PowerLiftModifier = POWER_LIFT_SCALE × (1.0f − PowerIntent)
POWER_LIFT_SCALE = 4.0° [GT]
```

| PowerIntent | Modifier contribution | Interpretation |
|---|---|---|
| 0.0 | +4.0° | Gentle tap; more loft than a driven shot |
| 0.5 | +2.0° | Moderate; slight elevation above base |
| 1.0 | 0.0° | Full power drive; no additional loft |

**Magnitude note:** The power lift effect is intentionally small (maximum 4°). Its
primary role is distinguishing a placed shot from a driven shot at the same ContactZone,
not producing dramatically different trajectories. Chip trajectory elevation is
governed primarily by `BelowCentre` base angle and `SpinLiftModifier`, not power lift.

---

### 3.3.5 Spin Lift Modifier

Deliberate backspin applied below the ball's equator produces additional lift.
This is the primary mechanism by which a chip arc is achieved — `BelowCentre` contact
combined with high `SpinIntent` generates the characteristic high, looping trajectory.

```
SpinLiftModifier = SPIN_LIFT_SCALE × SpinIntent
SPIN_LIFT_SCALE = 14.0° [GT]
```

| SpinIntent | Modifier contribution | Interpretation |
|---|---|---|
| 0.0 | 0.0° | No deliberate spin; no additional lift |
| 0.5 | +7.0° | Moderate curl/chip intent |
| 1.0 | +14.0° | Maximum chip intent; strongly lifted arc |

**Key interaction with ContactZone:** The full effect of `SpinLiftModifier` is only
meaningful when combined with `BelowCentre` contact. A `Centre` contact with
`SpinIntent = 1.0` would add 14° but the physical model for sidespin (OffCentre) would
be applied differently by §3.4. The designer should note that `SpinLiftModifier` applies
equally across all ContactZones — the gameplay interpreter (Decision Tree) must ensure
that high `SpinIntent` accompanies appropriate `ContactZone` selection for realistic chip
trajectories. This is documented as a caller responsibility.

---

### 3.3.6 Body Lean Penalty

Body lean is the most significant source of unintended shot elevation. An agent leaning
backward at contact applies force at an upward angle through the ball, lifting it. This
is the primary cause of shots skied over the crossbar in real football and is a key
differentiator between Shot Mechanics and Pass Mechanics (which rarely produces
dramatically ballooned trajectories).

The body lean angle (`bodyLeanAngleDeg`) is computed by §3.7 from
`AgentPhysicalProperties.BodyLeanAngle`. The sign convention:
- **Negative values**: leaning forward (forward over the ball) → reduces launch angle
- **Positive values**: leaning backward (standing upright or falling back) → increases launch angle

```
BodyLeanPenalty = bodyLeanAngleDeg × BODY_LEAN_TRANSFER_COEFFICIENT
BODY_LEAN_TRANSFER_COEFFICIENT = 0.60 [GT]
```

| Body lean | Penalty contribution | Interpretation |
|---|---|---|
| −20° (strongly forward) | −12°  | Lower than base angle; reduces risk of skying |
| −5° (slightly forward) | −3° | Slightly lower; forward press over ball |
| 0° (neutral) | 0° | Neutral stance; no modifier |
| +10° (slight lean back) | +6° | Modest additional lift |
| +25° (significant lean back) | +15° | Strongly lifted trajectory; high risk of crossbar or over |
| +40° (falling back) | +24° | Severely elevated; shot likely over bar |

**Rationale for coefficient (0.60) rather than 1:1 transfer:** A 1:1 transfer would mean
a 40° backward lean produces a 40° launch angle addition — an unrealistically direct
mechanical correspondence. In practice, the foot contact and body rotation are not
rigidly coupled. The 0.60 coefficient represents partial mechanical coupling. This value
will require empirical tuning against real match footage of ballooned shots.

**Important:** Negative `BodyLeanPenalty` (forward lean) can reduce `launchAngleDeg`
below `BaseAngle`. This is physically meaningful — an agent leaning strongly over
the ball at Centre contact could produce a trajectory close to ground level or even
slightly downward (daisy-cutter), which is clamped by `LAUNCH_ANGLE_MIN` (§3.3.9).

---

### 3.3.7 Body Shape Penalty

Where `BodyLeanPenalty` captures a directional effect from lean angle, `BodyShapePenalty`
captures general stance instability — cramped plant foot, off-balance run-up, or
uncoordinated approach — which produces **random-direction** launch angle error beyond
the intended elevation. Since §3.6 handles horizontal error, `BodyShapePenalty` here
adds unpredictable vertical scatter to the launch angle.

```
BodyShapePenalty = BODY_SHAPE_MAX_PENALTY × (1.0f − bodyMechanicsScore)
BODY_SHAPE_MAX_PENALTY = 8.0° [GT]
```

| BodyMechanicsScore | BodyShapePenalty | Interpretation |
|---|---|---|
| 1.0 (perfect) | 0.0° | No additional scatter |
| 0.8 (good) | +1.6° | Minor stance imperfection |
| 0.6 (poor) | +3.2° | Noticeable instability |
| 0.0 (worst case) | +8.0° | Severely compromised contact; shot trajectory unreliable |

**Note:** `BodyShapePenalty` is always positive (it adds scatter above the intended
angle). For the purpose of this formula, it operates as a positive addend. The physical
interpretation is that poor body shape tends to produce higher-than-intended trajectories
(the foot slips under the ball) more often than lower-than-intended (difficult to
compress the ball below intended angle without good stance). If playtesting indicates
this asymmetry is unrealistic, the penalty can be converted to a signed random term using
the deterministic hash — but this change requires §3.6 integration and is deferred.

---

### 3.3.8 Full Formula — Implementation Reference

```
launchAngleDeg = BaseAngle[ContactZone]
               + (POWER_LIFT_SCALE × (1.0f − PowerIntent))
               + (SPIN_LIFT_SCALE × SpinIntent)
               + (bodyLeanAngleDeg × BODY_LEAN_TRANSFER_COEFFICIENT)
               + (BODY_SHAPE_MAX_PENALTY × (1.0f − bodyMechanicsScore));

launchAngleDeg = Mathf.Clamp(launchAngleDeg, LAUNCH_ANGLE_MIN, LAUNCH_ANGLE_MAX);
```

**Worked example — Elite driven shot (Centre, PowerIntent 0.9, SpinIntent 0.0,
body lean −5°, BodyMechanicsScore 0.95):**

```
launchAngleDeg = 4.0                               // BaseAngle[Centre]
               + (4.0 × (1.0 − 0.9))              // +0.4° (small power lift)
               + (14.0 × 0.0)                      // +0.0° (no spin)
               + (−5.0 × 0.60)                     // −3.0° (slight forward lean)
               + (8.0 × (1.0 − 0.95))             // +0.4° (minor shape penalty)
             = 1.8°
```

A 1.8° launch angle on a 33 m/s shot is a low, fast, driven trajectory — realistic for
a well-struck penalty-area shot from a composed striker.

**Worked example — Chip shot (BelowCentre, PowerIntent 0.55, SpinIntent 0.85,
body lean +3°, BodyMechanicsScore 0.80):**

```
launchAngleDeg = 18.0                              // BaseAngle[BelowCentre]
               + (4.0 × (1.0 − 0.55))             // +1.8° (moderate power lift)
               + (14.0 × 0.85)                     // +11.9° (strong spin lift)
               + (3.0 × 0.60)                      // +1.8° (slight lean back)
               + (8.0 × (1.0 − 0.80))             // +1.6° (moderate shape penalty)
             = 35.1°
```

A 35° launch angle on a ~14 m/s shot (after velocity modifiers) produces the
characteristic high, looping chip trajectory over a rushing goalkeeper.

---

### 3.3.9 Launch Angle Clamping Policy

```
LAUNCH_ANGLE_MIN = −5.0°  [GT]  — permits daisy-cutter shots with strong forward lean
LAUNCH_ANGLE_MAX = 70.0°  [GT]  — permits extreme chip arcs; above this produces
                                  near-vertical trajectory physically implausible in football
```

**Rationale for `LAUNCH_ANGLE_MIN = −5°` (not 0°):** A slightly downward trajectory is
physically possible and desirable — elite players can deliberately strike the ball with
slight top-spin and downward force to bounce it powerfully along the ground. However,
angles below −5° represent physically implausible contact geometry (the foot would need
to strike downward through the ball) and are clamped.

**Rationale for `LAUNCH_ANGLE_MAX = 70°`:** Angles above 70° produce near-vertical
trajectory that is physically unreachable in a football context (it would require the
agent to scoop the ball from beneath with the toe — a different mechanical action
outside this system's scope). The 70° ceiling is permissive enough to cover the highest
realistic chip without constraining legitimate gameplay outcomes.

---

### 3.3.10 Boundary Verification Table

All values [EST] pending Appendix B numerical verification.

| Scenario | Zone | PowerIntent | SpinIntent | Lean° | BM Score | Expected angle | Est. range |
|---|---|---|---|---|---|---|---|
| Driven, elite | Centre | 0.95 | 0.0 | −5° | 0.95 | ~1.5° | [−2°, 5°] [EST] |
| Placed, composed | Centre | 0.55 | 0.0 | 0° | 0.85 | ~6.0° | [3°, 10°] [EST] |
| Chip, deliberate | BelowCentre | 0.50 | 0.90 | +5° | 0.80 | ~38° | [30°, 50°] [EST] |
| Curl, crossing | OffCentre | 0.75 | 0.70 | 0° | 0.90 | ~19° | [12°, 25°] [EST] |
| Ballooned (lean back) | Centre | 0.85 | 0.0 | +35° | 0.65 | ~27° | [20°, 35°] [EST] |
| Extreme sky (max lean) | Centre | 0.90 | 0.0 | +45° | 0.50 | ~35° | [25°, 45°] [EST] |

---

### 3.3.11 Constants Reference

| Constant | Value | Source | Tuning Range | Notes |
|---|---|---|---|---|
| `BaseAngle[Centre]` | 4.0° | [GT]; [LEES-1998] informed | [2.0°, 7.0°] | Low driven trajectory reference |
| `BaseAngle[BelowCentre]` | 18.0° | [GT]; [INOUE-2014] informed | [14.0°, 24.0°] | Chip arc base |
| `BaseAngle[OffCentre]` | 8.0° | [GT]; [ASAI-2002] informed | [5.0°, 12.0°] | Curl trajectory base |
| `POWER_LIFT_SCALE` | 4.0° | [GT] | [2.0°, 6.0°] | Max additional loft at PowerIntent=0 |
| `SPIN_LIFT_SCALE` | 14.0° | [GT] | [10.0°, 20.0°] | Max additional loft at SpinIntent=1 |
| `BODY_LEAN_TRANSFER_COEFFICIENT` | 0.60 | [GT] | [0.45, 0.75] | Fraction of body lean transferred to angle |
| `BODY_SHAPE_MAX_PENALTY` | 8.0° | [GT] | [5.0°, 12.0°] | Max scatter at BodyMechanicsScore=0 |
| `LAUNCH_ANGLE_MIN` | −5.0° | [GT] | [−10°, 0°] | Daisy-cutter floor; do not lower below −10° |
| `LAUNCH_ANGLE_MAX` | 70.0° | [GT] | [60°, 75°] | Near-vertical ceiling |

---

### 3.3.12 Failure Modes

| FM ID | Trigger | Response |
|-------|---------|----------|
| FM-LA01 | `bodyLeanAngleDeg` outside [−45°, +45°] (§3.7 contract violation) | Log Warning; clamp to [−45°, +45°] before applying formula |
| FM-LA02 | `bodyMechanicsScore` outside [0, 1] (§3.7 contract violation) | Log Warning; clamp to [0, 1] |
| FM-LA03 | `launchAngleDeg` NaN after formula | Log Error; use `BaseAngle[ContactZone]` as fallback |
| FM-LA04 | `launchAngleDeg` exceeds `LAUNCH_ANGLE_MAX` after formula | Clamp silently; log DEBUG |
| FM-LA05 | `launchAngleDeg` below `LAUNCH_ANGLE_MIN` after formula | Clamp silently; log DEBUG |

---

### 3.3.13 Design Decisions and Rationale

**DD-3.3-01: Launch angle is emergent, not table-driven**

An earlier design (Outline v1.1 — superseded) assigned launch angles from a per-ShotType
lookup table. This was eliminated when ShotType enum was removed (Outline v1.2).
The emergent approach has three advantages: (a) it is consistent with the parameter-based
model — physical output follows physical inputs; (b) it produces continuous variation
within each ContactZone rather than discrete jumps between types; (c) it requires no
table maintenance as new intent profiles are added.

**DD-3.3-02: Body lean is the primary "shots skied" mechanism**

The choice to model `bodyLeanAngleDeg` as a direct addend (via coefficient) rather than
a binary "lean flag" ensures that the severity of shot elevation scales continuously with
the degree of lean. A slightly rushed shot with minor lean produces minor elevation
increase; a fully off-balance shot produces severe elevation. This granularity is the
key differentiator between this system and simpler football games.

**DD-3.3-03: BodyShapePenalty always positive**

The asymmetry (poor body shape adds angle, never subtracts) is a conservative design
choice. Poor stance most commonly produces higher-than-intended trajectories in real
football. If playtesting reveals that some poor-stance shots should produce lower
trajectories (heavy, mis-struck shots that trickle), a signed penalty can be introduced
in a future revision — but this adds complexity to §3.6 interaction.

---

### 3.3.14 Cross-Specification Dependencies

| Dependency | Spec | What is Required |
|---|---|---|
| `ContactZone` enum definition | Shot Mechanics §2.4.1 (Section 2) | Three values: Centre, BelowCentre, OffCentre |
| `bodyLeanAngleDeg` | Shot Mechanics §3.7 BodyMechanicsEvaluator | Signed float; lean backward = positive; forward = negative |
| `bodyMechanicsScore` | Shot Mechanics §3.7 BodyMechanicsEvaluator | Scalar [0.0, 1.0] |
| `AgentPhysicalProperties.BodyLeanAngle` | Agent Movement #2 §3.5.4 | Confirmed present in approved spec |

---

## Section 3 (Part 1) Summary

| Sub-system | Owner file | Key output | Status |
|---|---|---|---|
| §3.1 ShotRequest Validation | `ShotValidator.cs` | Pass/fail gate; `ShotOutcome.Invalid` on failure | ✅ Specified |
| §3.2 Velocity Calculation | `VelocityCalculator.cs` | `kickSpeed` (m/s) ∈ [8.0, 35.0] | ✅ Specified |
| §3.3 Launch Angle Derivation | `LaunchAngleCalculator.cs` | `launchAngleDeg` ∈ [−5°, 70°] | ✅ Specified |
| §3.4 Spin Vector Calculation | `SpinVectorCalculator.cs` | `spinVector: Vector3` (rad/s) | ⬜ Part 2 |
| §3.5 Placement Resolution | `PlacementResolver.cs` | `aimDirection: Vector3` (unit) | ⬜ Part 2 |
| §3.6 Error Model | `ErrorCalculator.cs` | `finalDirection: Vector3` (unit) | ⬜ Part 2 |
| §3.7 Body Mechanics Evaluation | `BodyMechanicsEvaluator.cs` | `BodyMechanicsScore`, `BodyLeanAngle`, stumble trigger | ⬜ Part 2 |
| §3.8 Weak Foot Penalty | `WeakFootPenaltyApplier.cs` | `WeakFootModifier` scalar | ⬜ Part 2 |
| §3.9 Shot State Machine | `ShotStateMachine.cs` | Lifecycle management | ⬜ Part 2 |
| §3.10 Event Publishing | `ShotEventEmitter.cs` | `ShotExecutedEvent`, `ShotCancelledEvent` | ⬜ Part 2 |

**Critical validation required before approval:**
- Appendix B numerical verification: all boundary verification table rows must be
  computed against Ball Physics drag model simulation
- §3.2 V_BASE: verify that shots reach realistic field distance at each velocity
- §3.3 BaseAngles: verify trajectory arcs against StatsBomb shot height distribution data

**Next:** Section 3 Part 2 — §3.4 through §3.10.

---

## Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | February 22, 2026, 11:45 PM PST | Claude (AI) / Anton | Initial draft. §3.1–§3.3 fully specified. |
| 1.1 | February 23, 2026 | Claude (AI) / Anton | Cross-spec audit correction: (A3) VR-01 possession check updated from `BallState.PossessingAgentId` to `AgentSystem.GetPossessor()` — ERR-008 Option B; no `PossessingAgentId` field exists on `BallState`. VR-01 note updated to match. |
| 1.2 | March 6, 2026 | Claude (AI) / Anton | Comprehensive audit corrections: (1) Version header corrected 1.0→1.1 to match filename. (2) §3.1.2 output reference corrected from '§3.7' to '§3.2 (CONTACT phase)'. (3) Decision Tree #7→#8, Goalkeeper Mechanics #10→#11. |

---

*End of Section 3 (Part 1) — Shot Mechanics Specification #6*
*Tactical Director — Specification #6 of 20 | Stage 0: Physics Foundation*
