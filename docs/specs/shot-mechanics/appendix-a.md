# Shot Mechanics Specification #6 — Appendices A, B, C

**File:** `Shot_Mechanics_Spec_Appendices_v1_1.md`

**Purpose:** Completes the Shot Mechanics Specification with (A) step-by-step formula
derivations from first principles, (B) numerical hand-calculation verification tables
validating all key test scenarios and boundary conditions, and (C) sensitivity analysis
tables showing system behaviour across the full attribute and input space. Together with
Sections 1–8 and Section 9 (Approval Checklist), these appendices satisfy all template
requirements for formal approval of Specification #6 of 20.

**Created:** February 23, 2026, 11:59 PM PST
**Version:** 1.0
**Status:** DRAFT — Awaiting Lead Developer Review
**Author:** Claude (AI) with Anton (Lead Developer)
**Specification Number:** 6 of 20 (Stage 0 — Physics Foundation)

**Dependencies:**
- Section 3 Part 1 v1.1 (§3.1–§3.3 — velocity, launch angle) — authoritative formula source
- Section 3 Part 2 v1.1 (§3.4–§3.10 — spin, placement, error, body mechanics, weak foot) — authoritative formula source
- Section 5 v1.3 (104 test scenarios) — test IDs referenced in Appendix B
- Section 8 v1.3 (citation audit — 92 constants/formulas audited) — derivation rationale
- Pass Mechanics Spec #5 Appendices v1.1 — structural template

**Open Dependency Flags:** None. All dependencies confirmed stable.

> **Rendering note:** This document contains mathematical symbols (×, ÷, ≈, √, θ, °,
> ≤, ≥, ∑, ∂, σ) that require UTF-8 encoding to display correctly.

> **Derivation vs. tuning:** Constants marked [GT] are gameplay-tuned with no unique
> physics derivation. Their values are design decisions calibrated during playtesting.
> This is explicitly acknowledged — fabricating post-hoc academic justifications for
> tuned values would be intellectually dishonest and would misrepresent what requires
> playtesting vs. what is physics-constrained.

> **[EST] notation:** Values marked [EST] are estimates derived from literature or
> StatsBomb data. They are plausible starting points, not independently verified against
> Ball Physics drag simulation. Appendix B provides the closest available verification.

---

## Table of Contents

- [Appendix A: Formula Derivations](#appendix-a-formula-derivations)
  - [A.1 V_BASE Velocity Model Derivation](#a1-v_base-velocity-model-derivation)
  - [A.2 Sigmoid Attribute Blend Derivation](#a2-sigmoid-attribute-blend-derivation)
  - [A.3 Launch Angle Full Formula Derivation](#a3-launch-angle-full-formula-derivation)
  - [A.4 Spin Vector Derivation](#a4-spin-vector-derivation)
  - [A.5 Error Cone Geometry and GOAL_RELATIVE_ERROR_SCALE](#a5-error-cone-geometry-and-goal_relative_error_scale)
  - [A.6 Weak Foot Penalty Derivation](#a6-weak-foot-penalty-derivation)
  - [A.7 BodyMechanicsScore Composite Derivation](#a7-bodymechanicsscore-composite-derivation)
- [Appendix B: Numerical Verification](#appendix-b-numerical-verification)
  - [B.1 Elite Player, Close-Range Centre Shot (Finishing-dominant)](#b1-elite-player-close-range-centre-shot-finishing-dominant)
  - [B.2 Average Player, Long-Range Shot (LongShots-dominant)](#b2-average-player-long-range-shot-longshots-dominant)
  - [B.3 Weak Foot Shot Under Pressure](#b3-weak-foot-shot-under-pressure)
  - [B.4 Finesse Shot (OffCentre, SpinIntent = 1.0)](#b4-finesse-shot-offcentre-spinintent--10)
  - [B.5 Chip Shot (BelowCentre, SpinIntent = 0.85)](#b5-chip-shot-belowcentre-spinintent--085)
  - [B.6 Velocity Clamping Boundary Verification](#b6-velocity-clamping-boundary-verification)
  - [B.7 Sigmoid Blend Boundary Cases](#b7-sigmoid-blend-boundary-cases)
  - [B.8 GOAL_RELATIVE_ERROR_SCALE Geometric Derivation Verification](#b8-goal_relative_error_scale-geometric-derivation-verification)
  - [B.9 Full Error Stack Verification — Elite vs. Poor Striker](#b9-full-error-stack-verification--elite-vs-poor-striker)
- [Appendix C: Sensitivity Analysis](#appendix-c-sensitivity-analysis)
  - [C.1 V_BASE Sensitivity to PowerIntent and EffectiveAttribute](#c1-v_base-sensitivity-to-powerintent-and-effectiveattribute)
  - [C.2 ContactZone Modifier Sensitivity](#c2-contactzone-modifier-sensitivity)
  - [C.3 SPIN_VELOCITY_TRADE_OFF Sensitivity](#c3-spin_velocity_trade_off-sensitivity)
  - [C.4 Error Model: POWER_PENALTY_COEFFICIENT Sensitivity](#c4-error-model-power_penalty_coefficient-sensitivity)
  - [C.5 Error Model: PRESSURE_MAX_PENALTY Sensitivity](#c5-error-model-pressure_max_penalty-sensitivity)
  - [C.6 D_MID / D_SCALE Blend Sensitivity](#c6-d_mid--d_scale-blend-sensitivity)
  - [C.7 WeakFootErrorMultiplier vs. WeakFootRating](#c7-weakfooterrormultiplier-vs-weakfootrating)

---

## Appendix A: Formula Derivations

This appendix provides step-by-step derivations for every formula in Section 3.
Where Section 3 presents the final implementation form, this appendix shows *how*
each formula was reached and *why* each design decision was made.

---

### A.1 V_BASE Velocity Model Derivation

**Source:** §3.2 — Velocity Calculation
**Disposition:** ACADEMIC-INFORMED + [GT] (see §8.6.2)

#### A.1.1 Problem Statement

Given a `PowerIntent` ∈ [0, 1], a blended effective attribute ∈ [1, 20], and a set of
secondary modifiers, compute a scalar kick speed V (m/s) such that:

1. V is bounded within physically and practically valid limits.
2. V scales monotonically with both PowerIntent and EffectiveAttribute.
3. At maximum power with elite attributes, V reaches a world-class upper ceiling.
4. At zero power, V equals a non-zero floor (physically: any kick has minimum speed).
5. Secondary modifiers (spin, fatigue, contact quality, weak foot) reduce V multiplicatively
   but cannot drive it below an absolute safety floor.

#### A.1.2 V_BASE Construction

Unlike Pass Mechanics, Shot Mechanics does not require a distance-adjusted V_BASE — shots
always target the goal, and the Decision Tree selects PowerIntent based on position.
The formula is therefore a simpler linear model:

```
V_BASE = V_FLOOR + (EffectiveAttribute / ATTR_MAX) × (V_CEILING − V_FLOOR) × PowerIntent
```

**Component derivation:**

- `V_FLOOR = 10.0 m/s` [GT]: The physical lower bound on any valid shot. A ball kicked
  with PowerIntent > 0 must at minimum exceed a typical goalkeeper's reflex reach (≈8 m/s).
  10.0 m/s is chosen as a round, conservative estimate above that physical threshold.

- `V_CEILING = 35.0 m/s`: Derived from [LEES-1998] and validated against StatsBomb Open
  Data elite shot observations. Represents a world-class player achieving maximum
  foot-to-ball velocity transfer on a perfect instep contact. At `EffectiveAttribute = 20`
  (maximum) and `PowerIntent = 1.0`:
  ```
  V_BASE = 10.0 + (20/20) × (35 − 10) × 1.0 = 10.0 + 25.0 = 35.0 m/s ✓
  ```

- `ATTR_MAX = 20.0`: Design authority from Master Vol 2 §PlayerAttributes. Fixed.

**Monotonicity verification:**
```
∂V_BASE/∂PowerIntent = (EffAttr / ATTR_MAX) × (V_CEILING − V_FLOOR) > 0
                        for any EffAttr ∈ [1, 20]                             ✓

∂V_BASE/∂EffAttr = (V_CEILING − V_FLOOR) × PowerIntent / ATTR_MAX ≥ 0
                   = 0 only at PowerIntent = 0                                ✓
```

At `PowerIntent = 0`, V_BASE = V_FLOOR regardless of attribute — correct: a player
telegraphing zero power produces only the minimum speed. This is not a meaningful shot
in gameplay; it is the mathematical floor of the formula.

#### A.1.3 Full Modifier Stack

Modifiers are applied multiplicatively to V_BASE:

```
kickSpeed =   V_BASE
            × ContactZoneModifier[ContactZone]
            × (1.0 − SpinIntent × SPIN_VELOCITY_TRADE_OFF)
            × (1.0 − Fatigue   × FATIGUE_POWER_REDUCTION)
            × ContactQualityModifier          // from §3.7
            × weakFootVelocityMultiplier       // from §3.8
                 (clamped to [V_ABSOLUTE_MIN, V_ABSOLUTE_MAX])
```

**Why multiplicative, not additive?**
Additive modifiers can drive velocity below zero. Multiplicative modifiers preserve sign:
all factors are positive, so the product is always positive. Each modifier scales the
current running total by a bounded fraction. The approach also preserves proportionality:
a 20% fatigue penalty degrades a 30 m/s shot and a 12 m/s shot by the same percentage,
not by the same absolute amount. This is consistent with physical force-output degradation.

#### A.1.4 V_FLOOR vs. V_ABSOLUTE_MIN Design Decision

`V_FLOOR` (10.0) is the formula minimum before secondary modifiers. It is where V_BASE
starts when PowerIntent > 0. `V_ABSOLUTE_MIN` (8.0) is the post-modifier safety clamp.

Rationale for the two-tier design: consider a weak-foot shot at PowerIntent 0.1 with full
fatigue and poor body mechanics. Each modifier degrades from V_FLOOR:

```
V_BASE = 10.0 + (10/20) × 25 × 0.1 = 10.0 + 1.25 = 11.25 m/s
After WeakFoot (×0.80):    9.00 m/s
After Fatigue  (×0.80):    7.20 m/s → below V_ABSOLUTE_MIN
Final (clamped):           8.00 m/s
```

If `V_ABSOLUTE_MIN` were set equal to `V_FLOOR`, this stacked-penalty case would clamp
too aggressively and mask the effect of weak foot + fatigue. Setting `V_ABSOLUTE_MIN` 2
m/s below V_FLOOR gives the modifier chain room to act while still protecting
`Ball.ApplyKick()` from receiving dangerous near-zero velocities.

---

### A.2 Sigmoid Attribute Blend Derivation

**Source:** §3.2.3 — Attribute Blend (Finishing / LongShots)
**Disposition:** GAMEPLAY-TUNED [GT] (design decision OI-001)

#### A.2.1 Problem Statement

At close range, `Finishing` (technique, composure, precision in the box) should dominate.
At long range, `LongShots` (power generation, long-range accuracy) should dominate.
The transition must be continuous — a player at 19.9m and 20.1m should have near-identical
effective attributes. A hard threshold at 20m was rejected (OI-001 resolution) because it
would produce a discontinuous step-change in gameplay.

#### A.2.2 Sigmoid Function Choice

The logistic sigmoid is the canonical smooth step function:

```
σ(x) = 1 / (1 + e^(−x))
```

Scaled for the Shot Mechanics distance blend:

```
w(d) = σ((d − D_MID) / D_SCALE) = 1 / (1 + exp(−(d − D_MID) / D_SCALE))
```

- `D_MID = 20.0 m` [GT] — the blend midpoint; at d = D_MID, w = 0.5 exactly (equal blend).
  Chosen as the approximate penalty area edge — within this range, finishing technique
  dominates; beyond it, long-range power and accuracy matter more.
- `D_SCALE = 8.0 m` [GT] — controls transition sharpness. A smaller scale produces a
  steeper transition (closer to a hard threshold); a larger scale produces a more gradual
  blend. At D_SCALE = 8.0, the transition from 10% LongShots to 90% LongShots spans
  roughly 17m (from ~11.5m to ~28.5m), which is a tactically reasonable blend zone.

**Boundary checks:**
```
d = 0   (goal line):    w(0)  = 1/(1+exp(20/8)) = 1/(1+exp(2.5)) ≈ 0.076
d = 20  (midpoint):     w(20) = 1/(1+exp(0))    = 0.500          ✓
d = 40  (long range):   w(40) = 1/(1+exp(−2.5)) ≈ 0.924
```

At the goal line, w ≈ 0.076 → ~92% Finishing, 8% LongShots ✓ (technique still dominant).
At 40m, w ≈ 0.924 → ~8% Finishing, 92% LongShots ✓ (power/long-range attribute dominant).

#### A.2.3 EffectiveAttribute Construction

```
EffectiveAttribute = (1.0 − w(d)) × Finishing + w(d) × LongShots
```

This is a convex combination. Since both Finishing and LongShots ∈ [1, 20] and
(1−w) + w = 1, EffectiveAttribute ∈ [1, 20] always. No additional clamping required
from the blend itself.

**Continuity check:** As d varies continuously, w(d) varies continuously, therefore
EffectiveAttribute varies continuously. No discontinuities. ✓

---

### A.3 Launch Angle Full Formula Derivation

**Source:** §3.3 — Launch Angle Derivation
**Disposition:** ACADEMIC-INFORMED + [GT] (base angles from literature; modifiers [GT])

#### A.3.1 Problem Statement

Compute a vertical launch angle θ (degrees above horizontal) that:

1. Produces fundamentally different trajectory families by contact zone (driven, chip, curl).
2. Reflects the real physics of how body lean, power, and spin affect ball elevation.
3. Is bounded to prevent physically implausible outcomes.
4. Emerges continuously from input parameters — no per-ShotType lookup table.

#### A.3.2 Base Angle Selection

The base angle encodes the contact zone's fundamental trajectory family:

| ContactZone | BaseAngle | Source Reasoning |
|---|---|---|
| Centre | 4.0° | [LEES-1998] documents 2°–8° for full instep driven shots. 4° is the midpoint, representing a standard driven shot with forward lean neutral. |
| BelowCentre | 18.0° | [INOUE-2014] confirms that contact below the ball's equator sharply increases elevation. 18° is a [GT] starting point within a literature-plausible range of 14°–24°. |
| OffCentre | 8.0° | [ASAI-2002] documents that curled shots have moderate elevation. 8° is [GT], approximately double the Centre base to reflect the slightly elevated trajectory of off-centre contact. |

**Rationale for discrete lookup (not continuous):** ContactZone is a discrete enum
representing three fundamentally different contact geometries. There is no physical
"linear interpolation" between hitting the centre of the ball and hitting below it — these
are categorically different movements producing categorically different trajectories.

#### A.3.3 Modifier Derivation

Each modifier is additive. They are physically motivated, not lookup-driven.

**PowerLiftModifier:**

```
PowerLiftModifier = POWER_LIFT_SCALE × (1.0 − PowerIntent)
POWER_LIFT_SCALE  = 4.0° [GT]
```

Physical reasoning: at maximum power, the foot drives flat through the ball (no additional
loft). At low power, the follow-through naturally lifts slightly. Maximum contribution
is POWER_LIFT_SCALE (4°) at PowerIntent = 0. This effect is intentionally small — it
distinguishes a placed shot from a driven one, not a driven shot from a chip.

**SpinLiftModifier:**

```
SpinLiftModifier = SPIN_LIFT_SCALE × SpinIntent
SPIN_LIFT_SCALE  = 14.0° [GT]
```

Physical reasoning: deliberate backspin (BelowCentre contact with high SpinIntent) lifts
the ball. This is the primary elevation mechanism for a chip over a goalkeeper. At
SpinIntent = 1.0 with BelowCentre base angle of 18°, the maximum combined base+spin
elevation is 18° + 14° = 32° before body lean or shape penalties. With perfect body
lean and a moderate shape penalty, a full chip sits at ~30°–35°, which is the correct
visual arc for a chipped ball over a rushing goalkeeper.

**BodyLeanPenalty:**

```
BodyLeanPenalty = BODY_LEAN_TRANSFER_COEFFICIENT × bodyLeanAngleDeg
BODY_LEAN_TRANSFER_COEFFICIENT = 0.60 [GT]
```

Physical reasoning: an agent leaning backward at contact applies force at an upward angle
through the ball. The coefficient of 0.60 means 60% of body lean angle translates to
additional launch elevation. At lean = +10°, this adds 6° to the launch angle. At lean
= +45° (maximum allowed lean backward), the maximum penalty contribution is 27°. Combined
with even a Centre base angle, this would push toward the 70° ceiling, which is correct —
an extreme lean-back from a shot taken while falling over should produce a skied shot.

The coefficient is intentionally less than 1.0 because not all body lean translates
directly to ball elevation — some is absorbed by foot contact geometry. [GT]

**BodyShapePenalty:**

```
BodyShapePenalty = BODY_SHAPE_MAX_PENALTY × (1.0 − bodyMechanicsScore)
BODY_SHAPE_MAX_PENALTY = 8.0° [GT]
```

Physical reasoning: poor body mechanics (cramped, off-balance, poor plant) makes the shot
trajectory less predictable. The penalty is modelled as always additive (more loft, never
less) because poor-stance mis-strikes most commonly produce higher-than-intended
trajectories — the foot slips slightly upward under the ball. If data eventually shows
that some poor-stance shots produce lower trajectories, a signed penalty can be introduced
(documented in DD-3.3-03).

#### A.3.4 Full Assembly and Clamping

```
launchAngleDeg = BaseAngle[ContactZone]
               + PowerLiftModifier
               + SpinLiftModifier
               + BodyLeanPenalty
               + BodyShapePenalty

launchAngleDeg = Clamp(launchAngleDeg, LAUNCH_ANGLE_MIN, LAUNCH_ANGLE_MAX)
               = Clamp(launchAngleDeg, −5.0°, 70.0°)
```

**Clamp justification:**
- `LAUNCH_ANGLE_MIN = −5°`: Allows slightly downward-struck balls (topspin-driven ground
  shots). Below −5° is physically implausible — the foot would need to strike through
  the ground.
- `LAUNCH_ANGLE_MAX = 70°`: Permits the highest realistic chip arc without allowing
  near-vertical trajectories (70°+ = lob almost straight up, which is outside the model's
  scope and would produce physically questionable results in Ball Physics).

---

### A.4 Spin Vector Derivation

**Source:** §3.4 — Spin Vector Calculation
**Disposition:** ACADEMIC-INFORMED + [GT] (spin magnitudes [VER])

#### A.4.1 Three Independent Components

Shot spin is decomposed into three orthogonal components:

| Component | Axis | ContactZone-Primary | Physical Source |
|---|---|---|---|
| Topspin | −X (forward rotation) | Centre | Foot drives through ball; topspin = forward rotation around lateral axis |
| Backspin | +X (backward rotation) | BelowCentre | Foot strikes under equator; ball rotated backward |
| Sidespin | ±Y (lateral rotation) | OffCentre | Off-centre contact wraps foot around ball; produces curl |

These are not mutually exclusive — a shot can have non-zero values in all three.
The ContactZone determines which component dominates, not which is exclusive.

#### A.4.2 Topspin Derivation

```
TopspinMagnitude = TOPSPIN_BASE[ContactZone] × PowerIntent × (1.0 − SpinIntent)
```

**Component choices:**
- `PowerIntent` term: topspin is a consequence of driving through the ball — more power
  produces more forward rotation.
- `(1.0 − SpinIntent)` term: when deliberate spin (curl, chip) is intended, the foot
  contact is modified to produce that spin type, reducing the clean topspin of a driven shot.
- ContactZone-by-zone values [GT] from §3.4.4:
  - Centre: 15.0 rad/s — primary zone for topspin (instep drive)
  - BelowCentre: 5.0 rad/s — reduced; under-ball contact prioritises backspin
  - OffCentre: 8.0 rad/s — moderate; off-centre contact produces some topspin

**Boundary check (Centre, PowerIntent=1.0, SpinIntent=0.0):**
```
TopspinMagnitude = 15.0 × 1.0 × 1.0 = 15.0 rad/s ✓ (within [BRAY-2003] observed range)
```

#### A.4.3 Backspin Derivation

```
BackspinMagnitude = BACKSPIN_BASE[ContactZone] × SpinIntent
```

**Component choices:**
- `SpinIntent` only — backspin is a deliberate technique choice, not a power consequence.
  A player chipping with low power applies high backspin by intent; the same player
  blasting a BelowCentre shot without intent generates minimal backspin.
- No `PowerIntent` term — deliberately asymmetric from Topspin. The physical distinction:
  driven contact (power → forward rotation); chip contact (technique → backward rotation).
- BelowCentre base value of 30.0 rad/s [GT] is high. At SpinIntent = 1.0, this produces
  30 rad/s backspin on a chip, which is within the upper range documented in [ASAI-2002].

#### A.4.4 Sidespin Derivation

```
SidespinMagnitude = SIDESPIN_BASE[ContactZone] × SpinIntent × TechniqueScale
```

**TechniqueScale:**
```
TechniqueScale = TECHNIQUE_SPIN_BASE + (Technique − 1) / (ATTR_MAX − 1)
                 × (TECHNIQUE_SPIN_MAX − TECHNIQUE_SPIN_BASE)
               = 0.60 + (Technique − 1) / 19.0 × 0.40
```

This linearly maps Technique ∈ [1, 20] to TechniqueScale ∈ [0.60, 1.00].

**Why only sidespin is technique-modulated:**
Topspin and backspin are physical consequences of contact geometry and power/intent.
Sidespin requires deliberately wrapping the foot around the ball — a specific technical
skill. A player with Technique=1 can still generate some sidespin (the 0.60 floor) from
OffCentre contact, but cannot achieve the maximum curl of an elite technician.

**Curl direction determination:**
The sign of SidespinMagnitude is determined at assembly time from the horizontal deviation
of `aimDirection` from the agent's forward vector — ensuring the curl creates a natural
inswing toward the intended target. [VER] against Ball Physics Magnus model required.

#### A.4.5 Assembly and Clamping

The three components are assembled into a shooter-local Vector3, then rotated to world
space via the agent's orientation quaternion. Total spin magnitude is then clamped to
`SPIN_ABSOLUTE_MAX = 80.0 rad/s` — the Ball Physics hard limit from `BallPhysicsConstants.Limits.MAX_SPIN`.
This is more conservative than the physical literature upper bound (150 rad/s in [INOUE-2014])
because the simulation constraint governs, not physical reality.

---

### A.5 Error Cone Geometry and GOAL_RELATIVE_ERROR_SCALE

**Source:** §3.6.10 and §3.6.13 — Error Application in Goal-Relative Space
**Disposition:** DERIVED (geometric calculation)

#### A.5.1 Problem Statement

The error model produces `ErrorMagnitude` in degrees. Error is applied in goal-relative
`(u, v)` space (§3.6, KD-2 enforcement). A scale factor is required to convert degrees of
angular error to goal-relative UV offset units.

#### A.5.2 Geometric Derivation

At a reference distance `d_ref = 20.0 m` from goal:

An angular deviation of 1° produces a lateral displacement at the goal mouth of:
```
lateral_displacement = d_ref × tan(1°)
                     = 20.0 × tan(1°)
                     = 20.0 × 0.01746
                     ≈ 0.349 m
```

This lateral displacement in metres must be converted to goal-relative UV units.
The goal width is `GOAL_WIDTH = 7.32 m` (FIFA Law 1).

```
UV_offset_per_degree = lateral_displacement / GOAL_WIDTH
                     = 0.349 / 7.32
                     ≈ 0.0477 ≈ 0.048 goal-width units per degree
```

Therefore `GOAL_RELATIVE_ERROR_SCALE = 0.048` per degree at 20m. [EST] — this is the
reference distance value. At other distances, the scale varies as:

```
GRSE(d) ≈ (d / d_ref) × GOAL_RELATIVE_ERROR_SCALE
         = (d / 20.0) × 0.048
```

**Why a fixed reference scale rather than distance-varying?**

The error model uses angular units internally. At distance 20m, 1° of error produces
0.048 UV units of displacement. At distance 10m, the same 1° error produces half the
lateral displacement (the ball hasn't travelled as far), so 0.024 UV units. Using a fixed
scale therefore slightly over-predicts error displacement for close shots and
under-predicts for long shots.

However: Shot Mechanics does not have access to instantaneous ball travel distance (the ball
has not been kicked yet — the trajectory is not computed by this system). Using a reference
distance scale is a pragmatic design choice. The 20m reference is the sigmoid blend midpoint
— it is the "typical shot distance" in the system. The error introduced by this simplification
is small compared to the [GT] tolerances on all error constants, and the system will be
calibrated against real match data during playtesting.

**Boundary verification (Appendix B.8 below):**
A 5° error at 20m should displace the shot ~1.75m laterally.
UV offset = 5.0 × 0.048 = 0.24 UV units = 0.24 × 7.32m = 1.757m ≈ 1.75m ✓

---

### A.6 Weak Foot Penalty Derivation

**Source:** §3.8 — Weak Foot Penalty
**Disposition:** ACADEMIC-INFORMED + [GT] ([CAREY-2001] provides direction; magnitudes [GT])

#### A.6.1 Problem Statement

When `IsWeakFoot = true`, apply a penalty to both error and velocity that:
1. Scales with `WeakFootRating ∈ [1, 5]` — lower rating = larger penalty.
2. Reaches zero penalty at Rating = 5 (ambidextrous).
3. Applies a larger error expansion for shots than for passes (shots demand more precision).
4. Applies a velocity penalty exclusive to shots (weak-foot passes lose accuracy, not power).

#### A.6.2 penaltyFraction Linear Mapping

```
penaltyFraction = (5 − WeakFootRating) / 4.0
```

This linearly maps the 5-point scale:
- Rating = 5: penaltyFraction = 0.0 (no penalty — ambidextrous)
- Rating = 4: penaltyFraction = 0.25 (25% of max penalty)
- Rating = 3: penaltyFraction = 0.50 (50%)
- Rating = 2: penaltyFraction = 0.75 (75%)
- Rating = 1: penaltyFraction = 1.00 (maximum penalty)

The divisor is 4 (not 5) because the scale spans 4 steps (1→2→3→4→5). A divisor of 5
would understate the penalty at Rating=1.

#### A.6.3 Error Multiplier

```
weakFootErrorMultiplier = 1.0 + penaltyFraction × SHOT_WF_BASE_ERROR_PENALTY
SHOT_WF_BASE_ERROR_PENALTY = 0.60 [GT]
```

Maximum multiplier at Rating=1: `1.0 + 1.0 × 0.60 = 1.60` (+60% error)

This is double the Pass Mechanics value (0.30 → maximum +30%) because:
- Shooting requires more precise contact than passing.
- [CAREY-2001] documents accuracy degradation for weak-foot passes; equivalent shooting
  data is absent. The factor-of-two differential is a conservative design starting point
  acknowledging that shooting clearly demands more foot precision. [GT]

#### A.6.4 Velocity Multiplier

```
weakFootVelocityMultiplier = 1.0 − penaltyFraction × SHOT_WF_VELOCITY_PENALTY
SHOT_WF_VELOCITY_PENALTY = 0.20 [GT]
```

Maximum penalty at Rating=1: `1.0 − 1.0 × 0.20 = 0.80` (20% velocity reduction)

Passes do not have this penalty because weak-foot passes primarily degrade accuracy, not
pace — the ball reaches the target at a usable speed regardless of which foot it is struck
with. Weak-foot shots lose both accuracy and power in observable football behaviour.

**Boundary check (Rating=5):**
- `weakFootErrorMultiplier = 1.0 + 0.0 × 0.60 = 1.00` ✓ (no penalty)
- `weakFootVelocityMultiplier = 1.0 − 0.0 × 0.20 = 1.00` ✓ (no penalty)

---

### A.7 BodyMechanicsScore Composite Derivation

**Source:** §3.7 — Body Mechanics Evaluation
**Disposition:** ACADEMIC-INFORMED + [GT] (academic: component identification; weights: [GT])

#### A.7.1 Component Score Formulas

**RunUpScore:**
```
RunUpAngleDev = |physProps.RunUpAngle − 37.5°|
RunUpScore    = 1.0 − Clamp01(RunUpAngleDev / 45.0°)
```
At ideal angle (37.5° deviation from goal bearing = 0), score = 1.0.
At 45° deviation from ideal (total 82.5° or below 0°), score = 0.0.
Linear falloff from ideal. [LEES-1998] establishes 30°–45° as the optimal run-up range.
37.5° is the midpoint of that range.

**PlantScore:**
```
PlantScore = 1.0 − Clamp01(|physProps.PlantFootOffset| / 0.35m)
```
At optimal plant (0m offset), score = 1.0.
At 0.35m deviation (approximately one foot-width away from ideal), score = 0.0.
[KELLIS-2007] establishes that plant foot position directly affects power and accuracy.
0.35m as the tolerance is a [GT] estimate of the boundary of functional contact quality.

**VelocityScore:**
Piecewise, with three regions — score = 1.0 in the ideal range [1.0, 5.0 m/s],
degrading below and above that range:
```
if v_agent < 1.0:   VelocityScore = max(0, v_agent / 1.0)               // Too slow
if 1.0 ≤ v_agent ≤ 5.0: VelocityScore = 1.0                             // Ideal range
if v_agent > 5.0:   VelocityScore = max(0, 1.0 − (v_agent − 5.0) / 4.0)// Overrunning
```
The piecewise form reflects the physical reality: a stationary shot and an overrunning
shot both degrade quality. [KELLIS-2007] documents approach velocity as a predictor of
shot quality.

**LeanScore:**
```
LeanScore = 1.0 − Clamp01(|physProps.BodyLeanAngle| / 20.0°)
```
At neutral lean (0°), score = 1.0. At 20° lean (either direction), score = 0.0.
[NUNOME-2006] establishes body lean as the primary predictor of shot elevation error.

#### A.7.2 Weighted Composite

```
BodyMechanicsScore =  0.25 × RunUpScore
                    + 0.30 × PlantScore
                    + 0.20 × VelocityScore
                    + 0.25 × LeanScore
```

**Weight sum:** 0.25 + 0.30 + 0.20 + 0.25 = 1.00 ✓

Weight rationale:
- PlantScore highest (0.30): The plant foot is the most direct mechanical determinant of
  ball contact quality. A misplaced plant foot causes both power loss and trajectory error.
- RunUpScore and LeanScore equal (0.25 each): Both affect the shot, but neither alone is
  dominant — a good lean can partially compensate for a non-ideal run-up angle.
- VelocityScore lowest (0.20): Being in the ideal velocity range is beneficial, but
  skilled players can shoot effectively from a wider velocity range than poor players;
  this factor is the most forgiving.

#### A.7.3 ContactQualityModifier

```
ContactQualityModifier = 0.70 + BodyMechanicsScore × (1.0 − 0.70)
                       = 0.70 + BodyMechanicsScore × 0.30
```

This linearly maps `BodyMechanicsScore ∈ [0.0, 1.0]` to `CQM ∈ [0.70, 1.00]`.
The floor of 0.70 ensures a 30% maximum velocity reduction — even a catastrophically
poor-stance shot retains 70% of the velocity it would generate in ideal conditions.
A harder floor (e.g., 0.50) was considered but rejected as producing implausibly weak
shots from non-trivial shots taken while stumbling.

---

