## 3.6 Error Model

### 3.6.1 Responsibilities and Scope

§3.6 (ErrorCalculator) computes a **deterministic angular error offset** and applies it
to the aim direction produced by §3.5 (PlacementResolver). The result is the `finalDirection`
unit vector — the actual direction the ball is kicked, incorporating all inaccuracy from
the shooter's attributes, physical state, and situation.

This is the most gameplay-critical sub-system in Shot Mechanics. It is where the gap
between player intent and execution quality is quantified. The error model determines
whether a Messi-class striker slots the ball into the corner or a career squad player
skies it into row Z under pressure.

§3.6 **does**:
- Derive a base error angle from the blended finishing/long-shots attribute (mirroring §3.2)
- Apply five multiplicative penalty scalars: PowerPenalty, PressurePenalty, FatiguePenalty,
  BodyShapePenalty, and WeakFootMultiplier
- Compute a deterministic error direction using `DeterministicHash(matchSeed, agentId, frameNumber)`
- Apply the error in goal-relative `(u, v)` space, then convert back to a 3D direction
- Clamp the final error magnitude to `[MIN_ERROR_ANGLE, MAX_ERROR_ANGLE]`

§3.6 **does not**:
- Compute the blended finishing/long-shots attribute (§3.2 computes this via the same
  sigmoid; §3.6 re-uses the same `NormalisedEffectiveAttribute` scalar passed from §3.2)
- Apply weak foot velocity penalty (§3.8 owns velocity; §3.6 receives the
  `WeakFootMultiplier` scalar as an input from §3.8)
- Compute body mechanics penalties (§3.7 computes `BodyMechanicsScore`; §3.6 receives it)

**Position in pipeline:** §3.6 executes eighth, after §3.5 (PlacementResolver) and after
§3.8 (WeakFootPenaltyApplier) has provided `WeakFootMultiplier`.

---

### 3.6.2 Inputs and Outputs

**Inputs:**

| Input | Source | Type | Range | Notes |
|-------|--------|------|-------|-------|
| `aimDirection` | §3.5 PlacementResolver | Vector3 (unit) | — | Pre-error 3D direction |
| `normalisedEffectiveAttribute` | §3.2 VelocityCalculator | float | [0.0, 1.0] | Same blended Finishing/LongShots; do not recompute |
| `request.PowerIntent` | ShotRequest | float | [0.0, 1.0] | Pre-validated |
| `attributes.Composure` | PlayerAttributes | int | [1, 20] | Agent Movement §3.5.6 |
| `agentState.PressureRating` | AgentState | float | [0.0, 1.0] | Opponent proximity; Agent Movement §3.5.4 |
| `agentState.Fatigue` | AgentState | float | [0.0, 1.0] | 0 = fresh; 1 = exhausted |
| `bodyMechanicsScore` | §3.7 BodyMechanicsEvaluator | float | [0.0, 1.0] | Poor mechanics → more error |
| `weakFootMultiplier` | §3.8 WeakFootPenaltyApplier | float | [1.0, MAX_WF_MULTIPLIER] | 1.0 = no penalty |
| `matchSeed` | Match Config / determinism system | int | — | Fixed per match |
| `request.AgentId` | ShotRequest | int | — | Hash seed component |
| `request.FrameNumber` | ShotRequest | int | — | Hash seed component |
| `goalMouthOrigin`, `goalRightward` | Match Config | Vector3 | — | For goal-relative error application |

**Outputs:**

| Output | Destination | Type | Notes |
|--------|-------------|------|-------|
| `finalDirection` | Velocity vector construction | Vector3 (unit) | Post-error aim direction |
| `errorOffset` | ShotResult.ErrorOffset | Vector2 | `(u, v)` offset in goal-relative space; diagnostics |
| `errorMagnitudeDeg` | ShotResult diagnostics | float | Total angular error applied (degrees) |

---

### 3.6.3 Base Error Angle — Attribute Derivation

Base error scales inversely with `normalisedEffectiveAttribute`. A world-class finisher
(attribute = 20) at the optimum distance has minimal base error; a poor shooter (attribute = 1)
has maximum base error even before situational penalties.

```
BaseErrorAngle = BASE_ERROR_MAX − (normalisedEffectiveAttribute × (BASE_ERROR_MAX − BASE_ERROR_MIN))
               = BASE_ERROR_MAX × (1.0 − normalisedEffectiveAttribute)
                 + BASE_ERROR_MIN × normalisedEffectiveAttribute
```

Where:
- `BASE_ERROR_MAX` = 4.0° [GT] — worst-case base error at attribute = 0 (normalised)
- `BASE_ERROR_MIN` = 0.5° [GT] — best-case base error at attribute = 20

**Boundary checks:**
- `normalisedEffectiveAttribute = 0.0` (attribute = 1): `BaseErrorAngle = 4.0°`
- `normalisedEffectiveAttribute = 1.0` (attribute = 20): `BaseErrorAngle = 0.5°`

**Literature calibration:** StatsBomb data for elite strikers shows on-target shot
accuracy of ~75% from central positions (10–20m). Modelling suggests BASE_ERROR_MIN
around 0.4–0.6° at that range is consistent with observed outcomes when combined with
power and pressure penalties. [EST] — requires Appendix B simulation validation.

---

### 3.6.4 Power Penalty Scalar

The power–accuracy trade-off is the central mechanic of shot quality (KD-1). Error
grows quadratically with `PowerIntent`.

```
PowerPenalty = 1.0 + POWER_PENALTY_COEFFICIENT × PowerIntent²
```

Where `POWER_PENALTY_COEFFICIENT` = 1.5 [GT].

**Boundary checks:**
- `PowerIntent = 0.0`: `PowerPenalty = 1.0` (no penalty — gentle touch)
- `PowerIntent = 0.5`: `PowerPenalty = 1.375`
- `PowerIntent = 1.0`: `PowerPenalty = 2.5` (150% error increase at maximum power)

**Rationale:** Quadratic growth is consistent with [LEES-1998] biomechanics: the
relationship between maximal muscular effort and precision loss is non-linear, with
error accelerating sharply near maximum effort. A linear power penalty would understate
the accuracy degradation at high power and overstate it at moderate power.

---

### 3.6.5 Pressure Scalar

Pressure from nearby opponents degrades composure and widens the error cone.

```
PressurePenalty = 1.0 + (agentState.PressureRating × PRESSURE_MAX_PENALTY)
                         × (1.0 − NormalisedComposure)
```

Where:
- `PRESSURE_MAX_PENALTY` = 0.8 [GT] — maximum error multiplier from pressure alone
- `NormalisedComposure = (Composure − 1) / (ATTR_MAX − 1)` — maps [1, 20] to [0.0, 1.0]

**Boundary checks:**
- `PressureRating = 0, any Composure`: `PressurePenalty = 1.0` (no opponent pressure)
- `PressureRating = 1.0, Composure = 1`: `PressurePenalty = 1.8` (+80% error)
- `PressureRating = 1.0, Composure = 20`: `PressurePenalty = 1.0` (composed player fully resists)

**Design note:** `Composure` as a modifier on pressure (not on base error) means a composed
player fully resists maximum pressure at the cost of having no base error reduction.
Composure is a reactive attribute — it matters in high-pressure situations, not in clean-through-ball
scenarios. This creates the desired tactical differentiation.

---

### 3.6.6 Fatigue Scalar

Physical fatigue degrades control across all shot parameters.

```
FatiguePenalty = 1.0 + (agentState.Fatigue × FATIGUE_MAX_PENALTY)
```

Where `FATIGUE_MAX_PENALTY` = 0.4 [GT].

**Boundary checks:**
- `Fatigue = 0.0` (fresh): `FatiguePenalty = 1.0`
- `Fatigue = 0.5` (half-exhausted): `FatiguePenalty = 1.2`
- `Fatigue = 1.0` (exhausted): `FatiguePenalty = 1.4` (+40% error)

**Note:** `FATIGUE_MAX_PENALTY` for shots (0.4) is larger than for passes (0.25 in
Pass Mechanics §3.5) because shots demand more precise contact — fatigue has a larger
effect on shooting accuracy than passing accuracy. This is consistent with match
observation: tired strikers miss more shots than tired players misplace passes.

---

### 3.6.7 Body Shape Scalar

Poor body mechanics (low `BodyMechanicsScore`) degrades placement accuracy beyond the
directional effect already captured by `BodyShapePenalty` in §3.3 (which affects launch
angle). The error model adds a horizontal accuracy penalty from poor stance.

```
BodyShapeErrorPenalty = 1.0 + BODY_SHAPE_ERROR_COEFFICIENT × (1.0 − bodyMechanicsScore)²
```

Where `BODY_SHAPE_ERROR_COEFFICIENT` = 1.0 [GT].

**Boundary checks:**
- `BodyMechanicsScore = 1.0` (perfect): `BodyShapeErrorPenalty = 1.0`
- `BodyMechanicsScore = 0.5`: `BodyShapeErrorPenalty = 1.25`
- `BodyMechanicsScore = 0.0` (catastrophic): `BodyShapeErrorPenalty = 2.0`

**Rationale:** Quadratic form mirrors the power penalty: degradation accelerates as
body mechanics worsen. Small deviations from ideal stance are forgiven; critical body
shape failures (agent off-balance, shooting across the body) are severely penalised.

---

### 3.6.8 Full Error Magnitude Formula

```
ErrorMagnitude = BaseErrorAngle
                 × PowerPenalty
                 × PressurePenalty
                 × FatiguePenalty
                 × BodyShapeErrorPenalty
                 × weakFootMultiplier
```

**Example — elite striker, clean position, moderate power:**
- `BaseErrorAngle = 0.5°` (Finishing = 20)
- `PowerPenalty = 1.375` (PowerIntent = 0.5)
- `PressurePenalty = 1.0` (no pressure)
- `FatiguePenalty = 1.0` (fresh)
- `BodyShapeErrorPenalty = 1.06` (BodyMechanicsScore = 0.77)
- `weakFootMultiplier = 1.0` (dominant foot)
- **Total: ~0.73°**

**Example — poor striker, under heavy pressure, maximum power:**
- `BaseErrorAngle = 4.0°` (Finishing = 1)
- `PowerPenalty = 2.5` (PowerIntent = 1.0)
- `PressurePenalty = 1.72` (PressureRating = 0.9, Composure = 5)
- `FatiguePenalty = 1.3` (Fatigue = 0.75)
- `BodyShapeErrorPenalty = 1.5` (BodyMechanicsScore = 0.3)
- `weakFootMultiplier = 1.60` (weak foot, Rating = 1 → penaltyFraction = 1.0 → 1.0 + 1.0 × 0.60 = 1.60)
- **Pre-clamp total: ~53.7°** — exceeds `MAX_ERROR_ANGLE`
- **Post-clamp total: 25.0°** (clamped by §3.6.11) — a heavily misdirected shot; physically plausible

> **Note on clamping:** The raw multiplication in this worst-case scenario produces ~54°.
> The §3.6.11 clamp at `MAX_ERROR_ANGLE = 25.0°` is the operative result. This confirms the
> clamp is necessary and meaningful — without it, the error model would produce extreme
> misdirection inconsistent with the defined output contract.

These extremes are verified numerically in Appendix B (§B.9).

---

### 3.6.9 Deterministic Error Direction

An error *direction* must be computed for the angular offset. This must be deterministic
(KD-4 — no probabilistic dice rolls).

```
// Hash produces a float in [0, 1] from three integer seeds
float hashValue = DeterministicHash(matchSeed, request.AgentId, request.FrameNumber);

// Map hash to a full rotation angle [0°, 360°)
float errorAngleDeg = hashValue × 360.0f;

// Convert to a 2D direction in goal-relative (u, v) space
Vector2 errorDirection = new Vector2(
    Mathf.Cos(errorAngleDeg × Mathf.Deg2Rad),
    Mathf.Sin(errorAngleDeg × Mathf.Deg2Rad)
);
```

**DeterministicHash implementation (Stage 0 local version):**
```
// A simple but adequate hash for Stage 0.
// Identical signature to Deterministic Simulation Spec #15 RNG — migration is drop-in.
float DeterministicHash(int seed1, int seed2, int seed3)
{
    uint h = (uint)seed1;
    h ^= (uint)seed2 * 2654435761u;   // Knuth multiplicative hash
    h ^= (uint)seed3 * 2246822519u;
    h ^= h >> 15;
    h *= 2246822519u;
    h ^= h >> 13;
    // Map to [0.0, 1.0)
    return (h & 0x00FFFFFFu) / (float)0x01000000u;
}
```

This guarantees two agents shooting on the same frame produce distinct error directions.

---

### 3.6.10 Error Application in Goal-Relative Space

Error is applied as an offset in `(u, v)` goal-relative space, then converted back to
a 3D world-space aim direction.

```
// Step 1: Compute (u, v) offset
Vector2 errorOffset = errorDirection × ErrorMagnitudeDeg × GOAL_RELATIVE_ERROR_SCALE;
// GOAL_RELATIVE_ERROR_SCALE converts degrees to goal-mouth units; see §3.6.13

// Step 2: Apply offset to PlacementTarget (before clamping — clamping happens at §3.1)
// Note: This (u, v) is NOT clamped to [0,1]. The error may push outside the goal mouth.
// That is correct — a shot aimed at the corner and pushed by error may miss.
Vector2 actualTarget = request.PlacementTarget + errorOffset;

// Step 3: Convert actualTarget (u, v) back to world-space using §3.5 bilinear formula
Vector3 actualTargetWorld =
    goalMouthOrigin
    + goalRightward × (actualTarget.x × GOAL_WIDTH)
    + Vector3.up    × (actualTarget.y × GOAL_HEIGHT);

// Step 4: Re-derive aim direction from agent position to actualTargetWorld
// (same as §3.5.6 but using actualTargetWorld instead of targetWorldPoint)
Vector3 finalDirection = ComputeAimDirection(agentPosition, actualTargetWorld, launchAngleDeg);
```

**GOAL_RELATIVE_ERROR_SCALE** [GT] converts angular error degrees to goal-relative UV
offset units. At a reference distance of 20m, 1° of angular error corresponds to
approximately `0.35m / 7.32m ≈ 0.048 goal-width units`. This scale factor allows
the error model to remain in intuitive angular units internally while producing
physically correct spatial offsets. See Appendix B for derivation.

---

### 3.6.11 Error Clamping Policy

```
// After all penalties are multiplied, clamp before direction application
ErrorMagnitude = Mathf.Clamp(ErrorMagnitude, MIN_ERROR_ANGLE, MAX_ERROR_ANGLE);
```

| Constant | Value | Rationale |
|---|---|---|
| `MIN_ERROR_ANGLE` | 0.15° [GT] | Even elite players do not achieve perfect accuracy; floor prevents effectively zero error |
| `MAX_ERROR_ANGLE` | 25.0° [GT] | Prevents backward-pass-style extreme misdirection from shot mechanics |

---

### 3.6.12 Boundary Verification Table

| Scenario | Attribute | PowerIntent | Pressure | Fatigue | BMS | WF | Expected ErrorMagnitude |
|---|---|---|---|---|---|---|---|
| Elite, clean, moderate power | 20 | 0.5 | 0.0 | 0.0 | 1.0 | 1.0 | ~0.69° |
| Elite, clean, maximum power | 20 | 1.0 | 0.0 | 0.0 | 1.0 | 1.0 | ~1.25° |
| Average, moderate pressure | 10 | 0.6 | 0.5 | 0.2 | 0.75 | 1.0 | ~4–6° [EST] |
| Poor, heavy pressure, max power | 1 | 1.0 | 0.9 | 0.75 | 0.3 | 1.3 | ~25° (clamped) |
| Any, no pressure or fatigue | any | 0.0 | 0.0 | 0.0 | 1.0 | 1.0 | ≥ MIN_ERROR_ANGLE |
| Elite, weak foot Rating=1 | 20 | 0.5 | 0.0 | 0.0 | 1.0 | 2.0 | ~1.38° |

All values marked [EST] require Appendix B numerical verification.

---

### 3.6.13 Constants Reference

| Constant | Value | Type | Source |
|---|---|---|---|
| `BASE_ERROR_MAX` | 4.0° | [GT] | Design authority; calibration target vs. StatsBomb |
| `BASE_ERROR_MIN` | 0.5° | [GT] | Design authority; calibration target vs. StatsBomb |
| `POWER_PENALTY_COEFFICIENT` | 1.5 | [GT] | Informed by [LEES-1998] |
| `PRESSURE_MAX_PENALTY` | 0.8 | [GT] | Design authority |
| `FATIGUE_MAX_PENALTY` | 0.4 | [GT] | Design authority (larger than Pass Mechanics 0.25) |
| `BODY_SHAPE_ERROR_COEFFICIENT` | 1.0 | [GT] | Design authority |
| `MIN_ERROR_ANGLE` | 0.15° | [GT] | Design authority |
| `MAX_ERROR_ANGLE` | 25.0° | [GT] | Design authority |
| `GOAL_RELATIVE_ERROR_SCALE` | 0.048 per degree at 20m | [EST] [VER] | Geometric derivation; Appendix B |

---

### 3.6.14 Failure Modes

| ID | Condition | Detection | Response |
|---|---|---|---|
| FM-3.6-01 | ErrorMagnitude is NaN | `float.IsNaN()` check post-multiplication | Use MIN_ERROR_ANGLE; log error |
| FM-3.6-02 | `finalDirection` is zero vector | `magnitude < 0.001f` | Use `aimDirection` (no error applied); log error |
| FM-3.6-03 | DeterministicHash returns out-of-range | Assert `[0, 1)` range | Clamp; log error |
| FM-3.6-04 | `actualTargetWorld` unreachable (extreme error pushes behind shooter) | Dot product check on finalDirection vs. agentForward | Clamp to forward hemisphere; log warning |

---

### 3.6.15 Design Decisions and Rationale

**DD-3.6-01: Error applied in goal-relative (u, v) space, not in 3D angular space (KD-2)**

Applying error in angular space would require converting back to goal-relative coordinates
for the "skied high" / "pulled left" outcome interpretations needed by the Statistics Engine.
Applying in `(u, v)` space first keeps the model interpretable: the error offset is directly
readable as "how far off the intended corner did the shot land." The resulting world-space
direction is identical either way; the intermediate representation is the difference.

**DD-3.6-02: All five scalars are multiplicative, not additive**

An additive error model would allow individual penalty terms to independently overwhelm
the base, creating unrealistic single-factor dominance. Multiplicative composition means
all factors interact: high power on an already-pressured shot produces more degradation
than either factor alone, matching real football behaviour.

**DD-3.6-03: `normalisedEffectiveAttribute` is reused from §3.2, not recomputed**

The sigmoid blend of Finishing and LongShots by distance is computed once in §3.2 and
passed through the pipeline. Recomputing it in §3.6 would be redundant and could produce
inconsistencies if any input changed between the two calls (impossible in Stage 0 due to
the freeze at INITIATING, but a maintenance hazard in later stages).

---

### 3.6.16 Cross-Specification Dependencies

| Dependency | Spec | What is Required |
|---|---|---|
| `normalisedEffectiveAttribute` scalar | §3.2 VelocityCalculator | Must be passed through pipeline; not recomputed |
| `bodyMechanicsScore` | §3.7 BodyMechanicsEvaluator | Scalar [0.0, 1.0] |
| `weakFootMultiplier` | §3.8 WeakFootPenaltyApplier | Scalar ≥ 1.0 |
| `PlayerAttributes.Composure` | Agent Movement #2 §3.5.6 | Confirmed; range [1, 20] |
| `AgentState.PressureRating` | Agent Movement #2 §3.5.4 | Opponent proximity scalar [0.0, 1.0] |
| `AgentState.Fatigue` | Agent Movement #2 §3.5.4 | Confirmed; [0.0, 1.0] |
| `DeterministicHash` function | Deterministic Simulation Spec #15 (Stage 0: local impl) | Hash signature confirmed; drop-in migration |
| Goal geometry | Match Config | `goalMouthOrigin`, `goalRightward`, FIFA dimensions |

---

## 3.7 Body Mechanics Evaluation

### 3.7.1 Responsibilities and Scope

§3.7 (BodyMechanicsEvaluator) evaluates the physical quality of the shooting stance
and collapses four biomechanical inputs into a **scalar `BodyMechanicsScore ∈ [0.0, 1.0]`**
and a signed **`bodyLeanAngleDeg`** float. These two outputs are the primary inputs to
§3.3 (launch angle penalties) and §3.6 (error magnitude penalties), and the stumble
trigger in §3.9 (state machine).

Body mechanics evaluation is the sub-system that creates the realistic variance between
a striker shooting in stride versus one who is off-balance, cramped, or reaching.
It is the foundation of simulation fidelity for the physical reality of football: good
technique produces good outcomes; poor technique produces errors independent of skill ratings.

§3.7 **executes first** in the CONTACT evaluation chain — before §3.2, §3.3, and §3.6.
Its outputs are required by all three of those sub-systems. See §2.2.3 for pipeline order.

§3.7 **does**:
- Read run-up angle, plant foot offset, agent velocity vector, and body lean from
  `AgentPhysicalProperties` (frozen at INITIATING)
- Score each component on a [0.0, 1.0] scale
- Produce a weighted composite `BodyMechanicsScore`
- Extract `bodyLeanAngleDeg` as a signed float (negative = forward lean, positive = backward)
- Derive `ContactQualityModifier` for §3.2
- Evaluate the stumble trigger condition

§3.7 **does not**:
- Modify agent state (read-only access to AgentPhysicalProperties — KD-3 from Section 1)
- Determine what body position the agent is in (Agent Movement owns state transitions)

---

### 3.7.2 Inputs and Outputs

**Inputs:**

| Input | Source | Type | Notes |
|-------|--------|------|-------|
| `physProps.RunUpAngle` | AgentPhysicalProperties (frozen) | float (degrees) | Angle between run-up direction and ball-to-goal bearing |
| `physProps.PlantFootOffset` | AgentPhysicalProperties (frozen) | float (metres) | Signed lateral offset from ideal plant position |
| `physProps.AgentVelocityAtContact` | AgentPhysicalProperties (frozen) | Vector3 (m/s) | Agent's velocity vector at moment of contact |
| `physProps.BodyLeanAngle` | AgentPhysicalProperties (frozen) | float (degrees) | Signed; negative = forward lean, positive = backward |
| `request.PowerIntent` | ShotRequest | float | [0.0, 1.0]; used in stumble trigger |

**Outputs:**

| Output | Destination | Type | Range |
|--------|-------------|------|-------|
| `BodyMechanicsScore` | §3.3, §3.6, §3.9, ShotResult | float | [0.0, 1.0] |
| `bodyLeanAngleDeg` | §3.3 LaunchAngleCalculator | float (degrees) | [−45°, +45°] |
| `ContactQualityModifier` | §3.2 VelocityCalculator | float | [0.7, 1.0] |
| `stumbleTrigger` | §3.9 ShotStateMachine | bool | True if stumble threshold met |

---

### 3.7.3 Run-Up Angle Score

The ideal run-up angle for a shot is ~30–45° to the ball-to-goal bearing (informed by
biomechanics research on power generation in the kicking motion). Deviation degrades
both power transmission and accuracy.

```
RunUpAngleDev = |physProps.RunUpAngle − IDEAL_RUN_UP_ANGLE|   // Absolute deviation from ideal
RunUpScore    = 1.0 − Mathf.Clamp01(RunUpAngleDev / RUN_UP_TOLERANCE)
```

Where:
- `IDEAL_RUN_UP_ANGLE` = 37.5° [GT] — midpoint of 30–45° optimal range
- `RUN_UP_TOLERANCE`   = 45.0° [GT] — deviation at which `RunUpScore` reaches 0.0

**Boundary checks:**
- `RunUpAngleDev = 0°`: `RunUpScore = 1.0` (perfect approach)
- `RunUpAngleDev = 45°`: `RunUpScore = 0.0` (90° approach — shooting directly sideways)
- `RunUpAngleDev = 22.5°`: `RunUpScore = 0.5` (moderate deviation)

---

### 3.7.4 Plant Foot Offset Score

The plant foot should be placed approximately 15–25cm lateral to the ball (from
biomechanics studies of maximal kicking velocity). Too close cramps the swing; too far
causes off-balance contact.

```
PlantOffset = |physProps.PlantFootOffset|   // Unsigned; any lateral deviation penalised
PlantScore  = 1.0 − Mathf.Clamp01(PlantOffset / PLANT_FOOT_TOLERANCE)
```

Where `PLANT_FOOT_TOLERANCE` = 0.35m [GT] — offset at which `PlantScore` reaches 0.0.

**Boundary checks:**
- `PlantOffset = 0.0m`: `PlantScore = 1.0` (ideal placement)
- `PlantOffset = 0.35m`: `PlantScore = 0.0` (critically misplaced — very cramped or off-balance)
- `PlantOffset = 0.175m`: `PlantScore = 0.5` (moderate deviation)

---

### 3.7.5 Agent Velocity at Contact Score

An agent moving toward the goal at moderate speed transfers momentum effectively into
the kick. Moving away or laterally at speed degrades contact quality.

```
// Project agent velocity onto the goal direction (agentForward at contact)
float velocityTowardGoal = Vector3.Dot(physProps.AgentVelocityAtContact, agentForward);

VelocityScore:
  if (velocityTowardGoal ∈ [VELOCITY_IDEAL_MIN, VELOCITY_IDEAL_MAX]):
      VelocityScore = 1.0
  else if (velocityTowardGoal < VELOCITY_IDEAL_MIN):
      // Moving away from goal or static
      VelocityScore = Mathf.Clamp01(
          1.0 + velocityTowardGoal / VELOCITY_PENALTY_SCALE_NEGATIVE
      )
  else: // velocityTowardGoal > VELOCITY_IDEAL_MAX
      // Moving too fast — overrunning the ball
      VelocityScore = Mathf.Clamp01(
          1.0 − (velocityTowardGoal − VELOCITY_IDEAL_MAX) / VELOCITY_PENALTY_SCALE_POSITIVE
      )
```

Where [GT]:
- `VELOCITY_IDEAL_MIN` = 1.0 m/s — minimum forward velocity for ideal contact
- `VELOCITY_IDEAL_MAX` = 5.0 m/s — maximum before overrunning penalty begins
- `VELOCITY_PENALTY_SCALE_NEGATIVE` = 3.0 m/s — rate of score drop below ideal range
- `VELOCITY_PENALTY_SCALE_POSITIVE` = 4.0 m/s — rate of score drop above ideal range

---

### 3.7.6 Body Lean Measurement

Body lean is extracted directly from `AgentPhysicalProperties` — no calculation required.

```
bodyLeanAngleDeg = physProps.BodyLeanAngle
// Clamped to [−45°, +45°] at read time for safety
bodyLeanAngleDeg = Mathf.Clamp(bodyLeanAngleDeg, −45.0f, +45.0f);
```

Positive values (leaning backward) are the leading cause of skied shots. This value is
passed to §3.3 (BodyLeanPenalty) which uses it to add unintended loft.

A separate penalty for lean is incorporated into `BodyMechanicsScore` via `LeanScore`:

```
LeanScore = 1.0 − Mathf.Clamp01(|bodyLeanAngleDeg| / LEAN_TOLERANCE)
```

Where `LEAN_TOLERANCE` = 20.0° [GT] — lean angle at which `LeanScore` reaches 0.0.

---

### 3.7.7 Composite BodyMechanicsScore

The four component scores are combined with fixed weights into the composite scalar.

```
BodyMechanicsScore =
    WEIGHT_RUN_UP    × RunUpScore
  + WEIGHT_PLANT     × PlantScore
  + WEIGHT_VELOCITY  × VelocityScore
  + WEIGHT_LEAN      × LeanScore
```

Where weights [GT] must sum to 1.0:

| Weight Constant | Value | Rationale |
|---|---|---|
| `WEIGHT_RUN_UP` | 0.25 | Approach angle matters, but can be overcome by technique |
| `WEIGHT_PLANT` | 0.30 | Plant foot is the most direct determinant of contact quality |
| `WEIGHT_VELOCITY` | 0.20 | Movement contribution is significant but forgiving |
| `WEIGHT_LEAN` | 0.25 | Lean directly causes skied shots; equally weighted with run-up |

**Sum check:** `0.25 + 0.30 + 0.20 + 0.25 = 1.00` ✓

---

### 3.7.8 ContactQualityModifier Derivation

`ContactQualityModifier` is a velocity multiplier passed to §3.2. Poor body mechanics
reduces the effective velocity of the kick even when `PowerIntent` is high.

```
ContactQualityModifier = CQM_MIN + BodyMechanicsScore × (CQM_MAX − CQM_MIN)
```

Where:
- `CQM_MIN` = 0.7 [GT] — minimum velocity multiplier at `BodyMechanicsScore = 0.0`
- `CQM_MAX` = 1.0 [GT] — maximum velocity multiplier at `BodyMechanicsScore = 1.0`

**Boundary checks:**
- `BodyMechanicsScore = 0.0`: `ContactQualityModifier = 0.7` (30% velocity reduction — critically mis-struck)
- `BodyMechanicsScore = 0.5`: `ContactQualityModifier = 0.85`
- `BodyMechanicsScore = 1.0`: `ContactQualityModifier = 1.0` (no reduction)

---

### 3.7.9 Stumble Trigger Condition

A stumble is triggered when the agent is both off-balance and shooting at maximum effort.
This is the condition where the physical strain of the shot exceeds the agent's ability
to maintain stance.

```
bool stumbleTrigger =
    (BodyMechanicsScore < STUMBLE_BMS_THRESHOLD) &&
    (request.PowerIntent > STUMBLE_POWER_THRESHOLD);
```

Where:
- `STUMBLE_BMS_THRESHOLD`   = 0.35 [GT] — body mechanics quality below which stumble is possible
- `STUMBLE_POWER_THRESHOLD` = 0.75 [GT] — power intent above which stumble is possible

When `stumbleTrigger = true`:
- §3.9 (ShotStateMachine) transitions to STUMBLING at FOLLOW_THROUGH
- Agent Movement receives the stumble signal via the existing hysteresis pattern (§3.1
  of Agent Movement Spec #2 — no new interface required, per OI-003 resolution)
- `ShotExecutedEvent.StumbleTriggered = true` is published for GK difficulty estimation

**Rationale for threshold values:** STUMBLE_BMS_THRESHOLD = 0.35 means a player must
have poor mechanics on at least two or three components (not just one marginal one)
to be stumble-eligible. STUMBLE_POWER_THRESHOLD = 0.75 means only powerful shots can
trigger a stumble — a gentle touch cannot knock an agent off balance regardless of stance.

---

### 3.7.10 Boundary Verification Table

| Scenario | RunUpScore | PlantScore | VelScore | LeanScore | BMS | CQM | Stumble? |
|---|---|---|---|---|---|---|---|
| Perfect approach | 1.0 | 1.0 | 1.0 | 1.0 | 1.00 | 1.00 | No |
| Lateral approach | 0.0 | 0.8 | 0.9 | 0.9 | 0.635 | 0.891 | No (PowerIntent = 0.5) |
| Cramped plant foot | 0.9 | 0.0 | 0.8 | 0.7 | 0.555 | 0.867 | No |
| Off-balance, max power | 0.4 | 0.3 | 0.5 | 0.2 | 0.345 | 0.804 | Yes (PI > 0.75) |
| Critical mis-strike | 0.1 | 0.1 | 0.2 | 0.0 | 0.105 | 0.732 | Yes (PI > 0.75) |

---

### 3.7.11 Constants Reference

| Constant | Value | Type | Source |
|---|---|---|---|
| `IDEAL_RUN_UP_ANGLE` | 37.5° | [GT] | Midpoint of [LEES-1998] 30–45° optimal range |
| `RUN_UP_TOLERANCE` | 45.0° | [GT] | Design authority |
| `PLANT_FOOT_TOLERANCE` | 0.35 m | [GT] | Design authority |
| `VELOCITY_IDEAL_MIN` | 1.0 m/s | [GT] | Design authority |
| `VELOCITY_IDEAL_MAX` | 5.0 m/s | [GT] | Design authority |
| `VELOCITY_PENALTY_SCALE_NEGATIVE` | 3.0 m/s | [GT] | Design authority |
| `VELOCITY_PENALTY_SCALE_POSITIVE` | 4.0 m/s | [GT] | Design authority |
| `LEAN_TOLERANCE` | 20.0° | [GT] | Design authority |
| `WEIGHT_RUN_UP` | 0.25 | [GT] | Design authority |
| `WEIGHT_PLANT` | 0.30 | [GT] | Design authority |
| `WEIGHT_VELOCITY` | 0.20 | [GT] | Design authority |
| `WEIGHT_LEAN` | 0.25 | [GT] | Design authority |
| `CQM_MIN` | 0.7 | [GT] | Design authority |
| `CQM_MAX` | 1.0 | Design authority | Fixed upper bound |
| `STUMBLE_BMS_THRESHOLD` | 0.35 | [GT] | Design authority |
| `STUMBLE_POWER_THRESHOLD` | 0.75 | [GT] | Design authority |

---

### 3.7.12 Failure Modes

| ID | Condition | Detection | Response |
|---|---|---|---|
| FM-3.7-01 | `AgentPhysicalProperties` null or uninitialized | Null check at start of evaluation | Return `BodyMechanicsScore = 0.5` (neutral); log error |
| FM-3.7-02 | `BodyLeanAngle` out of [−90°, +90°] | Range check after read | Clamp to [−45°, +45°]; log warning |
| FM-3.7-03 | Composite weights do not sum to 1.0 | Compile-time assertion: `WEIGHT_SUM_ASSERT` | Build failure |
| FM-3.7-04 | NaN in any component score | `float.IsNaN()` on each score | Replace with 0.5 (neutral); log error |

---

### 3.7.13 Design Decisions and Rationale

**DD-3.7-01: §3.7 executes before §3.2 despite being numbered after it**

The pipeline order (§3.7 → §3.2 → §3.3 → §3.4 → §3.5 → §3.6) differs from the
section numbering (§3.2 → §3.3 → §3.4 → §3.5 → §3.6 → §3.7). The pipeline order
reflects data dependency (§3.2 needs `ContactQualityModifier`); the section numbering
reflects specification organisation (body mechanics is a supporting sub-system
described after the primary calculations it feeds). See §2.2.3 for the authoritative
13-step pipeline.

**DD-3.7-02: Four component scores with fixed weights rather than a formula per input**

An alternative design would derive `BodyMechanicsScore` directly from each physical
input via a single formula. The four-component weighted average is preferred because:
it makes each component's contribution inspectable and independently tunable;
the weights can be adjusted by gameplay designers without touching formulas;
and the approach naturally handles missing components (a future aerial shot spec might
omit `RunUpScore` — the weights can be redistributed).

**DD-3.7-03: §3.7 is read-only — no agent mutation**

Per KD-3 of Section 1: Shot Mechanics does not mutate agent state during calculation.
The stumble trigger produced by §3.7 is a signal emitted at FOLLOW_THROUGH (§3.9);
Agent Movement owns the state transition. §3.7 only evaluates and reports.

---

### 3.7.14 Cross-Specification Dependencies

| Dependency | Spec | What is Required |
|---|---|---|
| `AgentPhysicalProperties.RunUpAngle` | Agent Movement #2 §3.5.4 | Confirmed present |
| `AgentPhysicalProperties.PlantFootOffset` | Agent Movement #2 §3.5.4 | Confirmed present |
| `AgentPhysicalProperties.AgentVelocityAtContact` | Agent Movement #2 §3.5.4 | Confirmed present |
| `AgentPhysicalProperties.BodyLeanAngle` | Agent Movement #2 §3.5.4 | Confirmed present |
| Stumble hysteresis pattern | Agent Movement #2 §3.1 | Existing interface; no new signal required |

---

## 3.8 Weak Foot Penalty

### 3.8.1 Responsibilities and Scope

§3.8 (WeakFootPenaltyApplier) applies a penalty to both the **error cone** and
**kick velocity** when `ShotRequest.IsWeakFoot = true`. The penalties are derived from
`PlayerAttributes.WeakFootRating ∈ [1, 5]`.

The weak foot penalty architecture is identical to Pass Mechanics §3.7, with one
critical difference: **shot error cone expansion is larger than pass error cone
expansion** for the same `WeakFootRating`. Shots require more precise contact than
passes — the same technical deficit produces a larger accuracy penalty on a shot.

§3.8 **does**:
- Compute `weakFootErrorMultiplier` (≥ 1.0) for use in §3.6 error formula
- Compute `weakFootVelocityMultiplier` (≤ 1.0) for use in §3.2 velocity formula
- Return both as neutral (1.0 / 1.0) when `IsWeakFoot = false`

§3.8 **does not**:
- Determine which foot is weak (Decision Tree sets `IsWeakFoot` in `ShotRequest`)
- Modify spin components (weak foot degrades velocity and accuracy; spin remains
  controlled by ContactZone and SpinIntent — a weak-foot finesse shot still curls,
  just with less velocity and more error)

**Position in pipeline:** §3.8 executes second (immediately after §3.1 validation),
before §3.2–§3.7, so both multipliers are available to downstream sub-systems.

---

### 3.8.2 Inputs and Outputs

**Inputs:**

| Input | Source | Type | Range |
|-------|--------|------|-------|
| `request.IsWeakFoot` | ShotRequest | bool | — |
| `attributes.WeakFootRating` | PlayerAttributes | int | [1, 5] |

**Outputs:**

| Output | Destination | Type | Range |
|--------|-------------|------|-------|
| `weakFootErrorMultiplier` | §3.6 ErrorCalculator | float | [1.0, SHOT_WF_ERROR_MAX] |
| `weakFootVelocityMultiplier` | §3.2 VelocityCalculator | float | [SHOT_WF_VEL_MIN, 1.0] |

---

### 3.8.3 Error Cone Multiplier Formula

```
if (!request.IsWeakFoot)
    return (weakFootErrorMultiplier: 1.0f, weakFootVelocityMultiplier: 1.0f);

// Linear interpolation from maximum penalty (Rating=1) to no penalty (Rating=5)
float penaltyFraction = (5 − WeakFootRating) / 4.0f;
// Rating=1: penaltyFraction=1.0 (max penalty)
// Rating=5: penaltyFraction=0.0 (ambidextrous — no penalty)

weakFootErrorMultiplier = 1.0f + penaltyFraction × SHOT_WF_BASE_ERROR_PENALTY;
```

Where `SHOT_WF_BASE_ERROR_PENALTY` = 0.60 [GT].

**Boundary checks:**

| WeakFootRating | penaltyFraction | weakFootErrorMultiplier | Effect |
|---|---|---|---|
| 1 (worst) | 1.0 | 1.60 | +60% error expansion |
| 2 | 0.75 | 1.45 | +45% error expansion |
| 3 | 0.50 | 1.30 | +30% error expansion |
| 4 | 0.25 | 1.15 | +15% error expansion |
| 5 (ambi) | 0.00 | 1.00 | No penalty |

**Comparison to Pass Mechanics §3.7:** Pass Mechanics uses `WEAK_FOOT_BASE_PENALTY = 0.30`
(maximum +30% error). Shot Mechanics uses 0.60 (maximum +60% error) — double the
penalty — reflecting the greater precision demand of shooting vs. passing.

---

### 3.8.4 Velocity Reduction Formula

Weak foot shots also produce lower velocity, as the non-preferred foot generates less
consistent power transfer.

```
weakFootVelocityMultiplier = 1.0f − penaltyFraction × SHOT_WF_VELOCITY_PENALTY;
```

Where `SHOT_WF_VELOCITY_PENALTY` = 0.20 [GT].

**Boundary checks:**

| WeakFootRating | weakFootVelocityMultiplier | Effect |
|---|---|---|
| 1 (worst) | 0.80 | 20% velocity reduction |
| 3 | 0.90 | 10% velocity reduction |
| 5 (ambi) | 1.00 | No reduction |

**Rationale:** A 20% velocity reduction at Rating=1 means a shot capped at 35 m/s
on the dominant foot achieves at most 28 m/s on the weak foot. This is meaningful but
not so extreme that weak-foot shots become physically implausible. [GT] — calibrate
during playtesting.

---

### 3.8.5 Shot vs. Pass Penalty Differential

| Metric | Pass Mechanics §3.7 | Shot Mechanics §3.8 | Differential |
|---|---|---|---|
| Error multiplier at Rating=1 | ×1.30 | ×1.60 | Shot 2× worse per [CAREY-2001] extrapolation |
| Velocity penalty at Rating=1 | None | ×0.80 | Shots penalise velocity; passes do not |
| Rating range | [1, 5] | [1, 5] | Identical |
| Formula structure | Linear interpolation | Linear interpolation | Identical |

---

### 3.8.6 Boundary Verification Table

| IsWeakFoot | Rating | ErrorMultiplier | VelocityMultiplier |
|---|---|---|---|
| false | any | 1.00 | 1.00 |
| true | 1 | 1.60 | 0.80 |
| true | 2 | 1.45 | 0.85 |
| true | 3 | 1.30 | 0.90 |
| true | 4 | 1.15 | 0.95 |
| true | 5 | 1.00 | 1.00 |

All transitions are strictly monotone ✓. No discontinuities ✓.

---

### 3.8.7 Constants Reference

| Constant | Value | Type | Source |
|---|---|---|---|
| `SHOT_WF_BASE_ERROR_PENALTY` | 0.60 | [GT] | Design authority; 2× Pass Mechanics value |
| `SHOT_WF_VELOCITY_PENALTY` | 0.20 | [GT] | Design authority |

---

### 3.8.8 Failure Modes

| ID | Condition | Detection | Response |
|---|---|---|---|
| FM-3.8-01 | `WeakFootRating` out of [1, 5] | Range assertion | Clamp; log error |
| FM-3.8-02 | `weakFootErrorMultiplier` < 1.0 | Post-calculation assert | Clamp to 1.0; log error (cannot reduce error below baseline) |
| FM-3.8-03 | `weakFootVelocityMultiplier` > 1.0 | Post-calculation assert | Clamp to 1.0; log error |

---

### 3.8.9 Design Decisions and Rationale

**DD-3.8-01: Shot error penalty (0.60) is exactly double pass penalty (0.30)**

The factor-of-two differential is a design choice, not an academic derivation.
[CAREY-2001] documents passing accuracy degradation; equivalent shooting data is
not available in the literature at the required granularity. The doubling is a
conservative starting point: shooting clearly demands more precision than passing,
and a proportional penalty is defensible. [GT] — this is a primary playtesting target.

**DD-3.8-02: Velocity penalty applies to shots but not passes**

A weak-foot pass still reaches the intended target at the cost of accuracy. A weak-foot
shot loses both accuracy and power — the player cannot generate the same force through
the non-preferred leg. This reflects a well-observed phenomenon in football: players
shooting with their weak foot cannot consistently hit with the same power as their
dominant foot, while weak-foot passes are mainly an accuracy problem.

---

