# Pass Mechanics Specification #5 — Appendices A, B, C

**File:** `Pass_Mechanics_Spec_Appendices_v1_1.md`

**Purpose:** Completes the Pass Mechanics Specification with (A) step-by-step formula
derivations from first principles, (B) numerical hand-calculation verification tables for
all key test scenarios, and (C) sensitivity analysis tables showing system behaviour across
the full attribute and input ranges. Together with Sections 1–8 and Section 9 (Approval
Checklist), these appendices satisfy all template requirements for formal approval.

**Created:** February 20, 2026, 11:59 PM PST
**Revised:** February 21, 2026
**Version:** 1.1
**Status:** DRAFT — Awaiting Lead Developer Review
**Author:** Claude (AI) with Anton (Lead Developer)
**Specification Number:** 5 of 20 (Stage 0 — Physics Foundation)

**Dependencies:**
- Section 3 (all subsections, v1.0) — authoritative formula source
- Section 5 (v1.0) — test scenarios referenced in Appendix B
- Section 8 (v1.0) — citation audit for derivation rationale

**Open Dependency Flags:**
- `[ERR-007-PENDING]` — `KickPower`, `WeakFootRating`, `Crossing` field names provisional.
  All derivations use these names. Numeric derivations are unaffected; only field names
  may change on ERR-007 resolution.
- `[ERR-006-PENDING]` — `Ball.ApplyKick()` interface awaiting Ball Physics amendment
  approval. Appendix B calculations assume the interface defined in Amendment AM-001-001.
- `[ERR-008-PENDING]` — `PossessingAgentId` design unresolved. Does not affect any
  derivation in this file.

> **Rendering note:** This document contains mathematical symbols (×, ÷, ≈, √, θ, °,
> ≤, ≥, ∑) that require UTF-8 encoding to display correctly.

> **Derivation vs. tuning:** Constants marked [GT] are gameplay-tuned with no unique
> physics derivation. Their values are design decisions calibrated during playtesting.
> This is explicitly acknowledged — fabricating post-hoc academic justifications for
> tuned values would be intellectually dishonest and would misrepresent what requires
> playtesting vs. what is physics-constrained.

---

## Table of Contents

- [Appendix A: Formula Derivations](#appendix-a-formula-derivations)
  - [A.1 Pass Velocity Model](#a1-pass-velocity-model)
  - [A.2 Launch Angle Derivation](#a2-launch-angle-derivation)
  - [A.3 Through-Ball Lead Distance](#a3-through-ball-lead-distance)
  - [A.4 Spin Vector Calculation](#a4-spin-vector-calculation)
  - [A.5 Error Model — Modifier Chain](#a5-error-model--modifier-chain)
  - [A.6 Weak Foot Penalty Model](#a6-weak-foot-penalty-model)
  - [A.7 Deterministic Error Direction (Phase 9)](#a7-deterministic-error-direction-phase-9)
- [Appendix B: Numerical Verification](#appendix-b-numerical-verification)
  - [B.1 Pass Velocity Verification (PV Tests)](#b1-pass-velocity-verification-pv-tests)
  - [B.2 Launch Angle Verification (LA Tests)](#b2-launch-angle-verification-la-tests)
  - [B.3 Error Model Verification (PE Tests)](#b3-error-model-verification-pe-tests)
  - [B.4 Spin Vector Verification (SV Tests)](#b4-spin-vector-verification-sv-tests)
  - [B.5 Validation Scenario Verification (VS)](#b5-validation-scenario-verification-vs)
- [Appendix C: Sensitivity Analysis](#appendix-c-sensitivity-analysis)
  - [C.1 Velocity vs. KickPower and Distance](#c1-velocity-vs-kickpower-and-distance)
  - [C.2 ErrorAngle vs. Passing Attribute](#c2-errorangle-vs-passing-attribute)
  - [C.3 ErrorAngle vs. Pressure Scalar](#c3-errorangle-vs-pressure-scalar)
  - [C.4 Combined Error: Pressure + Fatigue](#c4-combined-error-pressure--fatigue)
  - [C.5 Spin Magnitude vs. Technique Attribute](#c5-spin-magnitude-vs-technique-attribute)
  - [C.6 WeakFootModifier vs. WeakFootRating](#c6-weakfootmodifier-vs-weakfootrating)

---

## Appendix A: Formula Derivations

This appendix provides step-by-step derivations for every formula introduced in Section 3.
Where Section 3 presents the final implementation form, this appendix shows *how* each
formula was reached and *why* each design decision was made.

---

### A.1 Pass Velocity Model

**Source:** §3.2 — Pass Velocity Model
**Disposition:** ACADEMIC-INFORMED + [GT] (see §8.6.2)

#### A.1.1 Problem Statement

Given a desired pass distance D (metres) and an agent's KickPower attribute K ∈ [1, 20],
compute a scalar launch speed V (m/s) such that:

1. The ball has enough kinetic energy to reach the target before decelerating below a
   usable reception speed (~3 m/s for ground passes).
2. V scales monotonically with K — a stronger player hits the ball harder.
3. V is bounded to a per-type [V_MIN, V_MAX] range to prevent physically implausible
   outcomes.
4. A fatigue modifier degrades V monotonically as fatigue increases.

#### A.1.2 KickPower → Velocity Mapping

[LEES-1998] and [KELLIS-2007] establish that ball speed is approximately 1.1–1.5× foot
speed at contact for full-instep kicks. Translating attribute [1, 20] to a velocity
requires a minimum practical kick speed — a realistic mechanical floor below which a
valid football pass cannot be executed. This is captured by `V_OFFSET(PassType)` [GT]:

```
ATTR_MAX    = 20.0        (from [MASTER-VOL2])
V_OFFSET    = per-type minimum practical kick speed (see A.1.5)

V_base(K, D, PassType) = V_OFFSET(PassType)
                       + (K / ATTR_MAX) × (D / D_MAX(PassType)) × (V_MAX(PassType) - V_OFFSET(PassType))
```

**Why V_OFFSET instead of V_MIN as the interpolation base?**

Appendix B numerical verification (OI-App-B-01) showed that using V_MIN as the
interpolation base under-powered mid-range inputs by ~1.3 m/s (KickPower=10, Distance=20m
for Ground: 8.71 m/s vs. expected [10, 16] m/s). V_MIN is the absolute defensive clamp
floor — it exists to prevent Ball.ApplyKick() receiving zero velocity. V_OFFSET is the
practical minimum a player generates when executing a valid pass. V_OFFSET > V_MIN in
all cases. See Amendment AM-003-001 for full justification.

Where `DistanceScale(D)` adjusts for the fact that longer passes require more force to
arrive above minimum usable speed:

```
V_base = V_OFFSET + (K / ATTR_MAX) × (D / D_MAX) × (V_MAX - V_OFFSET)
```

**Design decision:** Linear scaling chosen over quadratic or exponential because:
(a) it is the simplest form consistent with monotonicity requirements,
(b) it is auditable without logarithmic or exponential edge-case concerns,
(c) non-linearity is applied via the clamping mechanism (natural "soft ceiling"
    at V_MAX), which produces a piecewise-linear response that approximates
    the concave-down curve observed in [KELLIS-2007] empirical data.

#### A.1.3 Fatigue Modifier Derivation

[ALI-2011] confirms monotonic degradation of passing power output with fatigue.
The functional form is deliberately linear (simplest consistent with [ALI-2011]
directional finding):

```
FatigueModifier(F) = 1.0 - (F × FATIGUE_POWER_REDUCTION)

Where:
  F                      = Fatigue [0.0, 1.0]
  FATIGUE_POWER_REDUCTION = [GT] constant ≈ 0.15–0.25
```

**Boundary check:**
- F = 0.0: FatigueModifier = 1.0 (no degradation) ✓
- F = 1.0: FatigueModifier = 1.0 - FATIGUE_POWER_REDUCTION ≈ 0.75–0.85 ✓
- FatigueModifier is always > 0 (cannot produce zero velocity from fatigue alone) ✓

#### A.1.4 Full Velocity Formula

```
V_unclamped = V_base(K, D) × FatigueModifier(F)
V_launch    = clamp(V_unclamped, V_MIN, V_MAX)
```

Where `clamp(x, lo, hi) = max(lo, min(hi, x))`.

**Why clamp after multiplication (not before)?**
Clamping V_base before applying FatigueModifier would mean fatigue degrades a
player already at V_MIN, potentially producing a V below V_MIN. Clamping after
multiplication ensures the physical floor is always enforced on the final output.

#### A.1.5 Reference Constants (Placeholder — [GT], finalised at playtesting)

| Pass Type    | V_OFFSET (m/s) | V_MIN (m/s) | V_MAX (m/s) | D_MAX (m) |
|--------------|---------------|------------|------------|----------|
| Ground       | 8.0           | 5.0        | 18.0       | 35.0     |
| Driven       | 12.0          | 10.0       | 28.0       | 50.0     |
| Lofted       | 9.0           | 8.0        | 22.0       | 55.0     |
| ThroughGround| 8.5           | 6.0        | 20.0       | 40.0     |
| ThroughAerial| 9.0           | 8.0        | 22.0       | 45.0     |
| Cross        | 10.0          | 8.0        | 26.0       | 50.0     |
| Chip         | 6.0           | 5.0        | 14.0       | 25.0     |

> ⚠ These are design-intent placeholder values. Final values require Ball Physics
> drag simulation validation (flagged in §8.6.2 as VALIDATION REQUIRED). Values
> must be confirmed before Section 3.7 (Physical Profile Constants) is finalised.

---

### A.2 Launch Angle Derivation

**Source:** §3.3 — Launch Angle Derivation
**Disposition:** DERIVED (lofted/chip) | ACADEMIC-INFORMED + [GT] (ground/driven/cross)

#### A.2.1 Ground and Driven Passes (Fixed Angle)

[LEES-1998] documents observed launch angles of 2°–8° for ground passes and 5°–15°
for driven passes in elite football. Stage 0 uses a fixed angle within these ranges
with a minor distance correction:

```
θ_ground(D) = ANGLE_MIN_Ground + (D / D_MAX_Ground) × (ANGLE_MAX_Ground - ANGLE_MIN_Ground)
```

Where ANGLE_MIN = 2°, ANGLE_MAX = 5° [GT, within [LEES-1998] range].

**Why a distance correction for "fixed" ground passes?**
A 5m ground pass at 5° elevation is appropriate; a 35m ground pass at 5° elevation
will bounce before reaching the target at typical ball speeds. The minor distance
correction accounts for this without introducing trajectory simulation into Pass Mechanics.

#### A.2.2 Lofted and Chip Passes (Distance-Dependent — Derived)

For a lofted pass with a desired apex height H above the ground and horizontal range D,
simplified projectile mechanics (ignoring drag — drag acts during flight in Ball Physics):

```
Standard projectile equations:
  x(t) = V_h × t                     (horizontal: V_h = V × cos θ)
  y(t) = V_v × t - (g/2) × t²       (vertical:   V_v = V × sin θ)
  g    = 9.81 m/s²

At apex (t = t_apex):
  dy/dt = 0  →  V_v = g × t_apex  →  t_apex = V_v / g

Apex height:
  H = V_v × t_apex - (g/2) × t_apex²
    = (V_v²) / (2g)
  →  V_v = √(2gH)

Time of flight (symmetric arc, ground return at t = 2 × t_apex):
  t_total = 2 × t_apex = 2V_v / g

Horizontal range:
  D = V_h × t_total = V_h × (2V_v / g)
  →  V_h = D × g / (2V_v) = Dg / (2√(2gH)) = D√(g / (8H))

Launch angle:
  tan θ = V_v / V_h = √(2gH) / (D√(g / (8H)))
        = √(2gH) × √(8H / g) / D
        = √(16H²) / D
        = 4H / D

  θ = atan(4H / D)
```

**Implementation in §3.3:**
The Section 3.3 formula `θ = atan2(2H, D)` is a simplified form where H is already
interpreted as *half-apex height*. Both are equivalent; the above derivation uses raw
apex height H.

For Stage 0, H is a per-pass-type [GT] constant chosen to produce visually appealing
arcs. H is not back-calculated from physics — it is a design input.

| Pass Type    | H (design apex) | D_ref | θ at D_ref |
|--------------|----------------|-------|-----------|
| Lofted       | 6.0 m          | 30 m  | ≈ 38.7°   |
| Chip         | 4.5 m          | 12 m  | ≈ 56.3°   |

#### A.2.3 Cross Passes (Sub-Type Dependent)

Cross angles are ACADEMIC-INFORMED [LEES-1998] with [GT] bounds:

| CrossSubType | ANGLE_MIN | ANGLE_MAX |
|--------------|-----------|-----------|
| Flat         | 8°        | 15°       |
| Whipped      | 15°       | 25°       |
| High         | 25°       | 40°       |

Distance-correction applied identically to ground pass formula (A.2.1).

#### A.2.4 Velocity Vector3 Construction

Once θ is determined, the velocity vector is constructed:

```
kickDirection = normalize(targetPosition - passerPosition)  // horizontal unit vector
kickSpeed     = V_launch (from A.1.4)

V_horizontal = kickDirection × kickSpeed × cos(θ)
V_vertical   = Vector3.up   × kickSpeed × sin(θ)
kickVelocity = V_horizontal + V_vertical
```

This vector is passed directly to `Ball.ApplyKick(ref BallState ball, Vector3 velocity,
Vector3 spin, int agentId, float matchTime)`.

---

### A.3 Through-Ball Lead Distance

**Source:** §3.6 — Target Resolution
**Disposition:** DERIVED (kinematics)

#### A.3.1 Problem Statement

For a through ball (space-targeted pass), the ball must be directed to where the receiver
will be when the ball arrives, not where they are now. Stage 0 uses a linear projection
assuming constant receiver velocity.

#### A.3.2 Linear Receiver Projection

```
Given:
  P_r   = receiver current position (Vector3)
  V_r   = receiver current velocity (Vector3)
  P_b   = ball launch position (passer position)
  V_b   = ball launch speed (scalar, from A.1)

Step 1 — Estimate time of flight:
  D_initial = |P_r - P_b|          (current distance to receiver)
  t_flight  ≈ D_initial / V_b      (simplified; ignores deceleration)

Step 2 — Project receiver position:
  P_target = P_r + V_r × t_flight  (linear projection)

Step 3 — Compute lead distance:
  D_lead = |P_target - P_r| = |V_r| × t_flight
```

**Limitation explicitly documented:** This derivation ignores:
- Ball deceleration during flight (Ball Physics handles drag; Pass Mechanics does not
  model it here)
- Receiver acceleration or deceleration
- Non-straight receiver paths

These limitations are acceptable at Stage 0. Non-linear receiver prediction is deferred
to Stage 1 (§7.1.1). The known underestimation of lead distance for long through balls
is flagged as a playtesting calibration point, not a formula error.

#### A.3.3 Edge Case: Near-Stationary Receiver

If |V_r| < V_THRESHOLD_STATIONARY (≈ 0.5 m/s), the receiver is considered stationary
and lead distance = 0. The pass targets the receiver's current position. This prevents
a degenerate lead calculation when the receiver has zero velocity.

---

### A.4 Spin Vector Calculation

**Source:** §3.4 — Spin Vector Calculation
**Disposition:** ACADEMIC for axis assignment; ACADEMIC-INFORMED + [GT] for magnitude

#### A.4.1 TechniqueScale Derivation

Technique attribute [1, 20] maps to a spin multiplier [0.5, 1.5]:

```
TechniqueScale(T) = 0.5 + ((T - 1) / (ATTR_MAX - 1)) × 1.0
                  = 0.5 + (T - 1) / 19.0

Boundary check:
  T = 1:  TechniqueScale = 0.5  (minimum — poor technique, reduced spin)
  T = 10: TechniqueScale ≈ 0.97 (mid-range — near-neutral)
  T = 20: TechniqueScale = 1.5  (maximum — elite technique, maximum spin)
```

**Why [0.5, 1.5] and not [0.0, 1.0]?**
A TechniqueScale of 0.0 would produce zero spin. Zero spin disables Magnus effect
silently in Ball Physics, producing flat trajectories for all passes regardless of
intent. A minimum of 0.5 ensures the spin floor is always active. 1.5 ceiling
provides meaningful upside for elite technical players without producing physically
implausible spin rates.

#### A.4.2 SpinMagnitude Formula

```
SpinMagnitude = max(SPIN_MIN, SPIN_BASE(PassType) × TechniqueScale(T))
```

Where SPIN_MIN ≈ 1.0 rad/s (minimum spin floor per [HONG-2012] knuckling threshold).

#### A.4.3 Spin Vector Construction

Spin axis assignment follows [ASAI-2002] and [BRAY-2003]:

```
// Unity coordinate system: Y = up, Z = forward, X = right

switch (PassType):
  Ground, Driven, ThroughGround:
    spinVector = Vector3(SpinMagnitude, 0, 0)    // topspin (forward roll)
  Chip:
    spinVector = Vector3(-SpinMagnitude, 0, 0)   // backspin (descends steeply)
  ThroughAerial, Lofted:
    spinVector = Vector3(SpinMagnitude * 0.7, 0, 0)  // mild topspin
  Cross (Flat, Whipped):
    // Sidespin for Magnus curve: direction depends on crossing foot side
    spinVector = Vector3(0, sidespinSign * SpinMagnitude, 0)
  Cross (High):
    // Mix of topspin and sidespin
    spinVector = Vector3(SpinMagnitude * 0.5, sidespinSign * SpinMagnitude * 0.5, 0)
```

Where `sidespinSign = +1 for left-foot cross, -1 for right-foot cross` — this flag is
provided on the `PassRequest` struct by the Decision Tree.

---

### A.5 Error Model — Modifier Chain

**Source:** §3.5 — Deterministic Error Model
**Disposition:** DESIGN-AUTHORITY (chain structure); [GT] (individual modifier formulae)

#### A.5.1 Multiplicative Chain Rationale

The multiplicative structure:

```
ErrorAngle = BASE_ERROR(PassType)
           × PassingModifier(Passing)
           × PressureModifier(PressureScalar)
           × FatigueModifier(Fatigue)
           × OrientationModifier(BodyAngle)
           × UrgencyModifier(Urgency)
           × WeakFootModifier(IsWeakFoot, WeakFootRating)
```

is consistent with the established Stage 0 pattern (First Touch §3.1, Collision System
§3.3). The multiplicative form ensures:
- Each factor is independent and testable in isolation (PE-001 through PE-010)
- The formula never produces negative error (all factors ≥ 0)
- Compound degradation is proportional: double pressure + double fatigue produces
  quadruple error contribution from those terms, which matches the non-linear
  accuracy collapse observed under extreme combined stress

#### A.5.2 Individual Modifier Derivations

**PassingModifier(P):** Maps Passing [1, 20] to error multiplier. Higher Passing = lower error.

```
PassingModifier(P) = PASSING_ERROR_MAX - ((P - 1) / 19.0) × (PASSING_ERROR_MAX - PASSING_ERROR_MIN)

Design constraints:
  PassingModifier(1)  = PASSING_ERROR_MAX  ≈ 2.5–3.0 (poor passer triples base error)
  PassingModifier(10) ≈ 1.0               (mid-range: neutral)
  PassingModifier(20) = PASSING_ERROR_MIN  ≈ 0.4–0.6 (elite passer halves base error)
```

This is a linear inverse — the simplest form satisfying monotonicity. Non-linear
(convex) passing modifier is flagged as a Stage 1 refinement if playtesting reveals
mid-range attributes feeling "flat."

**PressureModifier(S):** S = PressureScalar [0.0, 1.0] — accumulated from nearby opponents.

```
PressureModifier(S) = 1.0 + S × PRESSURE_WEIGHT

Design constraints:
  PressureModifier(0.0) = 1.0            (no pressure — neutral)
  PressureModifier(1.0) = 1.0 + PRESSURE_WEIGHT
  PRESSURE_WEIGHT       ≈ 0.4–0.6 [GT]  (maximum pressure adds 40–60% error)
```

[BEILOCK-2010] documents 20–50% accuracy degradation under peak pressure. PRESSURE_WEIGHT
calibrated to the upper end (50%) to account for compounding with fatigue.

**FatigueModifier(F):**

```
FatigueModifier(F) = 1.0 + F × FATIGUE_ACCURACY_REDUCTION

Note: FATIGUE_ACCURACY_REDUCTION is separate from FATIGUE_POWER_REDUCTION (A.1.3).
Power and accuracy are degraded independently by fatigue.

Design constraints:
  FatigueModifier(0.0) = 1.0
  FatigueModifier(1.0) = 1.0 + FATIGUE_ACCURACY_REDUCTION ≈ 1.15–1.25 [GT]
```

**OrientationModifier(α):** α = BodyAngle (degrees, 0° = fully aligned with pass direction).

```
OrientationModifier(α) = 1.0 + (α / 90.0) × ORIENTATION_MAX_PENALTY
  (clamped: α ∈ [0°, 90°])

Design constraints:
  OrientationModifier(0°)  = 1.0 (aligned — no penalty)
  OrientationModifier(90°) = 1.0 + ORIENTATION_MAX_PENALTY ≈ 1.5–2.0 [GT]
```

Linear interpolation from 0° to 90°. Values beyond 90° (passing fully backwards)
are clamped to 90° — extreme misalignment should trigger a pass refusal from the
Decision Tree before reaching Pass Mechanics.

**UrgencyModifier(U):** U ∈ [0.0, 1.0] from PassRequest.

```
UrgencyModifier(U) = 1.0 + U × URGENCY_ERROR_SCALE

Design constraints:
  UrgencyModifier(0.0) = 1.0             (measured pass — no urgency penalty)
  UrgencyModifier(1.0) = 1.0 + URGENCY_ERROR_SCALE ≈ 1.2–1.4 [GT]
```

#### A.5.3 Error Angle Clamping

```
ErrorAngle_final = clamp(ErrorAngle, MIN_ERROR_ANGLE, MAX_ERROR_ANGLE)

MIN_ERROR_ANGLE ≈ 0.1°   (floor: perfect passing still has microscopic error — PE-010)
MAX_ERROR_ANGLE ≈ 15–20° (ceiling: prevents backward ball redirection from extreme inputs)
```

---

### A.6 Weak Foot Penalty Model

**Source:** §3.7 — Weak Foot Penalty
**Disposition:** ACADEMIC-INFORMED [CAREY-2001] + [GT]

#### A.6.1 Derivation

[CAREY-2001] documents 15–25% accuracy degradation for weak-foot passes in elite
competition. WeakFootRating ∈ [1, 5] maps to a penalty multiplier:

```
WeakFootModifier(R) =
  if IsWeakFoot == false:
    return 1.0   (preferred foot — no penalty)
  else:
    PenaltyFraction = (1.0 - (R - 1) / 4.0)
    // R=1 → PenaltyFraction=1.0 (maximum penalty)
    // R=5 → PenaltyFraction=0.0 (ambidextrous — no penalty)

    return 1.0 + PenaltyFraction × WEAK_FOOT_BASE_PENALTY

WEAK_FOOT_BASE_PENALTY ≈ 0.25–0.35 [GT, calibrated within [CAREY-2001] 15–25% range]
```

**Boundary checks:**
- R = 1, IsWeakFoot = true:  WeakFootModifier = 1.0 + 1.0 × 0.30 = 1.30 (30% error increase)
- R = 5, IsWeakFoot = true:  WeakFootModifier = 1.0 + 0.0 × 0.30 = 1.00 (no penalty)
- IsWeakFoot = false:         WeakFootModifier = 1.00 regardless of R

> **Note:** `[ERR-007-PENDING]` — WeakFootRating field name is provisional. The [1, 5]
> range is distinct from the [1, 20] range used by most player attributes. This smaller
> range was chosen because foot preference is a coarser trait than passing skill —
> 5 levels (natural two-footer, reasonable, competent, decent, ambidextrous) are
> sufficient granularity.

---

### A.7 Deterministic Error Direction (Phase 9)

**Source:** §3.5 (continued) — Error Direction Application
**Disposition:** DESIGN-AUTHORITY [MASTER-VOL1 §1.3 determinism]

#### A.7.1 Problem Statement

The error model computes an error *magnitude* (ErrorAngle in degrees). A direction for
this deviation must be computed deterministically — no random number generator.

#### A.7.2 Deterministic Hash Approach

A perpendicular rotation axis is derived from a deterministic hash of inputs:

```
hashInput = AgentId × 73856093 XOR FrameNumber × 19349663 XOR PassTypeIndex × 83492791
// Prime multipliers chosen to distribute bits across the integer range

normalized = float(hashInput AND 0x7FFFFFFF) / float(0x7FFFFFFF)  // [0.0, 1.0]
errorAngleRadians = normalized × 2π                                // full circle

// Rotate kick direction by errorAngleRadians around the vertical (Y) axis
errorAxis     = Vector3.up
errorRotation = Quaternion.AngleAxis(ErrorAngle_deg, errorAxis × sin(errorAngleRadians))
kickVelocity  = errorRotation × kickVelocity
```

**Why is this deterministic?**
AgentId, FrameNumber, and PassTypeIndex are all deterministic simulation values — they
are identical on replay. The XOR-and-multiply hash produces the same output for the same
inputs on any platform using integer arithmetic (no floating-point involved in the hash).

**Why not use the passer's facing direction as the error axis?**
If error always rotates in the same plane as the passer's body, a player could exploit
this by always facing at an angle to the target. The Y-axis rotation distributes error
uniformly in the horizontal plane, which is both more realistic and more exploit-resistant.

---

## Appendix B: Numerical Verification

This appendix provides hand-calculated expected values for key test scenarios from
Section 5. These calculations use the placeholder constant values from Appendix A
(and the reference constants table in A.1.5). When constants are finalised during
playtesting, these calculations must be rerun to update expected test values in
`PassTestFixtures.cs`.

> **CRITICAL:** If any constant in `PassConstants.cs` changes, the affected rows in
> this appendix must be recalculated before the regression suite is re-run. Stale
> hand-calculation values are worse than none — they provide false confidence.

### Assumed Constants for All Appendix B Calculations

| Constant                    | Value Used  | Source     |
|-----------------------------|-------------|------------|
| V_MIN_Ground                | 5.0 m/s     | A.1.5 [GT] |
| V_MAX_Ground                | 18.0 m/s    | A.1.5 [GT] |
| D_MAX_Ground                | 35.0 m      | A.1.5 [GT] |
| V_MIN_Driven                | 10.0 m/s    | A.1.5 [GT] |
| V_MAX_Driven                | 28.0 m/s    | A.1.5 [GT] |
| D_MAX_Driven                | 50.0 m      | A.1.5 [GT] |
| V_MIN_Lofted                | 8.0 m/s     | A.1.5 [GT] |
| V_MAX_Lofted                | 22.0 m/s    | A.1.5 [GT] |
| D_MAX_Lofted                | 55.0 m      | A.1.5 [GT] |
| V_MIN_Chip                  | 5.0 m/s     | A.1.5 [GT] |
| V_MAX_Chip                  | 14.0 m/s    | A.1.5 [GT] |
| ATTR_MAX                    | 20.0        | [MASTER-VOL2] |
| FATIGUE_POWER_REDUCTION     | 0.20        | [GT]       |
| FATIGUE_ACCURACY_REDUCTION  | 0.20        | [GT]       |
| PRESSURE_WEIGHT             | 0.50        | [GT]       |
| PASSING_ERROR_MAX           | 2.8         | [GT]       |
| PASSING_ERROR_MIN           | 0.45        | [GT]       |
| ORIENTATION_MAX_PENALTY     | 1.5         | [GT]       |
| URGENCY_ERROR_SCALE         | 0.35        | [GT]       |
| WEAK_FOOT_BASE_PENALTY      | 0.30        | [GT]       |
| BASE_ERROR_Ground           | 1.5°        | [GT]       |
| BASE_ERROR_Lofted           | 3.0°        | [GT]       |
| SPIN_BASE_Ground            | 8.0 rad/s   | [GT]       |
| SPIN_BASE_Cross             | 15.0 rad/s  | [GT]       |
| SPIN_BASE_Chip              | 12.0 rad/s  | [GT]       |
| MIN_ERROR_ANGLE             | 0.1°        | [GT]       |
| MAX_ERROR_ANGLE             | 18.0°       | [GT]       |

---

### B.1 Pass Velocity Verification (PV Tests)

#### B.1.1 PV-001: Ground Pass, KickPower=10, Distance=20m, Fatigue=0

**Using corrected formula (AM-003-001) with V_OFFSET_Ground = 8.0:**

```
V_base = V_OFFSET + (K / ATTR_MAX) × (D / D_MAX) × (V_MAX - V_OFFSET)
       = 8.0 + (10/20) × (20/35) × (18.0 - 8.0)
       = 8.0 + 0.50 × 0.571 × 10.0
       = 8.0 + 2.857
       = 10.857 m/s

FatigueModifier(0) = 1.0 - (0 × 0.20) = 1.0

V_launch = 10.857 × 1.0 = 10.857 m/s

Clamp to [5.0, 18.0]: 10.857 m/s (no clamp needed)

Expected result: V ≈ 10.86 m/s
Section 5 test PV-001 expects: V ∈ [10, 16] m/s ✓ PASSES
```

**v1.0 status:** FAIL (8.71 m/s was below [10, 16] expected range)
**v1.1 status:** ✓ PASS (10.86 m/s is within [10, 16] expected range)

#### B.1.2 PV-002: Driven Pass, KickPower=18, Distance=35m, Fatigue=0

Using corrected formula with V_OFFSET_Driven = 12.0 m/s [GT]:

```
V_base = 12.0 + (18/20) × (35/50) × (28.0 - 12.0)
       = 12.0 + 0.90 × 0.70 × 16.0
       = 12.0 + 10.08
       = 22.08 m/s

FatigueModifier(0) = 1.0

V_launch = 22.08 m/s

Clamp to [10.0, 28.0]: 22.08 m/s (no clamp needed)

Expected result: V ≈ 22.1 m/s
Section 5 test PV-002 expects: V ∈ [20, 26] m/s ✓
```

#### B.1.3 PV-004: Ground Pass, KickPower=1, Distance=30m (Min-power, long range)

**Using corrected formula (AM-003-001) with V_OFFSET_Ground = 8.0:**

```
V_base = 8.0 + (1/20) × (30/35) × (18.0 - 8.0)
       = 8.0 + 0.05 × 0.857 × 10.0
       = 8.0 + 0.429
       = 8.429 m/s

FatigueModifier(0) = 1.0

V_launch = 8.429 m/s

Clamp to [5.0, 18.0]: 8.429 m/s (above V_MIN = 5.0, no clamp)
```

Section 5 v1.1 test PV-004 expects: `V ∈ [V_OFFSET_Ground, V_OFFSET_Ground + 1.5]`
= `V ∈ [8.0, 9.5]`

8.429 m/s is within [8.0, 9.5] ✓ **PASSES**

**Why this is correct:** A weak player (KickPower=1) cannot generate less than V_OFFSET
— the minimum mechanical energy of a valid kick. The V_OFFSET floor correctly reflects
that even a poor player must kick hard enough for the ball to travel meaningfully.
V_MIN (5.0 m/s) is the defensive guard preventing Ball.ApplyKick() errors, not the
minimum a player can actually generate.

#### B.1.4 PV-005: Chip Pass, KickPower=20, Distance=D_MAX_Chip=25m (corrected condition)

**Using corrected formula (AM-003-001) with V_OFFSET_Chip = 6.0:**

```
V_base = 6.0 + (20/20) × (25/25) × (14.0 - 6.0)
       = 6.0 + 1.00 × 1.00 × 8.0
       = 6.0 + 8.0
       = 14.0 m/s

FatigueModifier(0) = 1.0

V_launch = 14.0 m/s

Clamp to [5.0, 14.0]: 14.0 m/s (exactly at V_MAX_Chip — clamp applied) ✓

Section 5 v1.1 test PV-005 expects: V == V_MAX_Chip (14.0 m/s) ✓ PASSES
```

**v1.0 condition (KickPower=20, Distance=10m):** would have produced 9.20 m/s — no clamp.
Test condition was incorrect; corrected to Distance=D_MAX_Chip in Section 5 v1.1.

**Also verifies PV-010 pattern:** max V_MAX clamp requires KickPower=20 AND Distance=D_MAX.
PV-010 (Ground) corrected similarly in Section 5 v1.1 to Distance=D_MAX_Ground=35m.

---

### B.2 Launch Angle Verification (LA Tests)

#### B.2.1 LA-001: Ground Pass, Distance=20m

```
θ = ANGLE_MIN_Ground + (20/35) × (ANGLE_MAX_Ground - ANGLE_MIN_Ground)
  = 2.0° + 0.571 × (5.0° - 2.0°)
  = 2.0° + 1.714°
  = 3.71°

Expected: θ ∈ [2°, 5°] → 3.71° ✓
```

#### B.2.2 LA-002: Lofted Pass, Distance=30m

```
H_Lofted = 6.0 m (A.2.2)
θ = atan(4H / D) = atan(4 × 6.0 / 30.0) = atan(0.8) = 38.66°

Expected: θ ∈ [25°, 55°] → 38.66° ✓
```

#### B.2.3 LA-003: Chip Pass, Distance=12m

```
H_Chip = 4.5 m (A.2.2)
θ = atan(4H / D) = atan(4 × 4.5 / 12.0) = atan(1.5) = 56.31°

Expected: θ ∈ [40°, 70°] → 56.31° ✓
```

#### B.2.4 LA-006: Lofted Angle Decreases with Distance (corrected test name)

```
At D = 15m: θ = atan(4 × 6.0 / 15.0) = atan(1.6) = 57.99°
At D = 45m: θ = atan(4 × 6.0 / 45.0) = atan(0.533) = 28.07°

Result: Angle at 15m (57.99°) > Angle at 45m (28.07°) ✓
```

**Physics explanation:** With a fixed apex height H=6.0m, a shorter horizontal distance
requires a steeper launch angle to reach that apex. A 45m lofted pass at 28° produces
the same peak height as a 15m lofted pass at 58°. This is correct projectile mechanics.

**v1.0 test LA-006** expected "Angle at 45m > Angle at 15m" — this was physically
inverted. The test name ("increases with distance") was also wrong.

**v1.1 test LA-006** expects "Angle at 15m > Angle at 45m" with corrected test name
("decreases with distance"). Verified: 57.99° > 28.07° ✓ — test now passes.

---

### B.3 Error Model Verification (PE Tests)

#### B.3.1 PE-001: Elite Passer, No Pressure, No Fatigue, Aligned Body

```
BASE_ERROR_Ground = 1.5°

PassingModifier(20) = PASSING_ERROR_MAX - ((20-1)/19) × (PASSING_ERROR_MAX - PASSING_ERROR_MIN)
                    = 2.8 - 1.0 × (2.8 - 0.45)
                    = 2.8 - 2.35
                    = 0.45

PressureModifier(0)  = 1.0 + 0 × 0.50 = 1.0
FatigueModifier(0)   = 1.0 + 0 × 0.20 = 1.0
OrientationModifier(0°) = 1.0 + (0/90) × 1.5 = 1.0
UrgencyModifier(0)   = 1.0 + 0 × 0.35 = 1.0
WeakFootModifier     = 1.0 (IsWeakFoot = false)

ErrorAngle = 1.5° × 0.45 × 1.0 × 1.0 × 1.0 × 1.0 × 1.0 = 0.675°

Clamp to [0.1°, 18.0°]: 0.675° ✓

ELITE_ERROR(Ground) = BASE_ERROR_Ground × PASSING_ERROR_MIN = 1.5° × 0.45 = 0.675°

Section 5 v1.1 test PE-001 expects: ErrorAngle ≤ ELITE_ERROR(PassType) = 0.675°
0.675° ≤ 0.675° ✓ PASSES (exactly at the elite floor — correct)
```

**v1.0 issue (OI-App-B-05):** PE-001 referenced "MIN_ERROR(PassType)" which was undefined.
**v1.1 resolution:** Explicit constant `ELITE_ERROR(PassType) = BASE_ERROR × PASSING_ERROR_MIN`
defined. The name makes the semantics clear: it is the minimum error achievable by an
elite passer at neutral conditions, not an absolute floor (which is MIN_ERROR_ANGLE = 0.1°).

#### B.3.2 PE-002: Worst Case (Fully Degraded)

```
BASE_ERROR_Ground = 1.5°

PassingModifier(1)      = 2.8
PressureModifier(1.0)   = 1.0 + 1.0 × 0.50 = 1.50
FatigueModifier(1.0)    = 1.0 + 1.0 × 0.20 = 1.20
OrientationModifier(90°)= 1.0 + (90/90) × 1.5 = 2.50
UrgencyModifier(HIGH=1.0)= 1.0 + 1.0 × 0.35 = 1.35
WeakFootModifier(R=1)   = 1.0 + 1.0 × 0.30 = 1.30

ErrorAngle = 1.5° × 2.8 × 1.50 × 1.20 × 2.50 × 1.35 × 1.30
           = 1.5° × 2.8 × 1.50 × 1.20 × 2.50 × 1.755
           = 1.5° × 22.13
           = 33.19°

Clamp to [0.1°, 18.0°]: 18.0° (MAX_ERROR_ANGLE clamp applies)

Section 5 test PE-002 expects: ErrorAngle == MAX_ERROR(PassType) ✓
```

The clamp at 18.0° confirms that PE-002 will pass if MAX_ERROR_ANGLE = 18.0°.
The raw unclamped value (33.19°) validates that the worst-case multiplicative chain
does indeed exceed the cap — confirming the cap is a meaningful design boundary.

#### B.3.3 PE-005: PassingModifier Monotone Decrease

```
Passing values: {1, 5, 10, 15, 20}

PassingModifier(1)  = 2.8 - (0/19)  × 2.35 = 2.800
PassingModifier(5)  = 2.8 - (4/19)  × 2.35 = 2.800 - 0.495 = 2.305
PassingModifier(10) = 2.8 - (9/19)  × 2.35 = 2.800 - 1.113 = 1.687
PassingModifier(15) = 2.8 - (14/19) × 2.35 = 2.800 - 1.732 = 1.068
PassingModifier(20) = 2.8 - (19/19) × 2.35 = 2.800 - 2.350 = 0.450

Sequence: 2.800 > 2.305 > 1.687 > 1.068 > 0.450 — strictly decreasing ✓

PE-005 expects ErrorAngle strictly decreasing as Passing increases.
Since all other modifiers are held at neutral (1.0), this reduces to PassingModifier
being strictly decreasing. Verified above ✓
```

---

### B.4 Spin Vector Verification (SV Tests)

#### B.4.1 SV-004: High Technique Produces Higher Spin

```
TechniqueScale(18) = 0.5 + (18-1)/19 = 0.5 + 0.895 = 1.395
TechniqueScale(5)  = 0.5 + (5-1)/19  = 0.5 + 0.211 = 0.711

SpinMagnitude(18, Ground) = 8.0 × 1.395 = 11.16 rad/s
SpinMagnitude(5,  Ground) = 8.0 × 0.711 =  5.69 rad/s

11.16 > 5.69 ✓ — SV-004 passes
```

#### B.4.2 SV-005: Cross Spin Within Magnus Validation Bounds

```
TechniqueScale(10) = 0.5 + (10-1)/19 = 0.5 + 0.474 = 0.974

SpinMagnitude(10, Cross) = 15.0 × 0.974 = 14.61 rad/s

MAX_CROSS_SPIN is defined by Ball Physics Magnus calibration.
[BRAY-2003] documents free-kick spin rates of 50–63 rad/s; general passes 6–25 rad/s.
14.61 rad/s is within the [6, 25] general kicked-pass range.

Pending Ball Physics Magnus numerical validation (flagged §8.6.4 CROSS-SPEC VALIDATION
REQUIRED). At 14.61 rad/s, the Magnus force at 20 m/s ball speed should be checked
against Ball Physics §3.1 constants before SV-005 can be formally verified.
```

---

### B.5 Validation Scenario Verification (VS)

Validation scenarios test realistic inputs and check for football-plausible outputs.
Full VS calculations are intentionally narrative — they check ranges and comparisons,
not exact values.

#### B.5.1 VS-001: Elite Passer, Short Ground Pass

**Input profile:** Passing=18, Technique=16, KickPower=17, Distance=12m,
Fatigue=0.1, Pressure=0.1, BodyAngle=5°, Urgency=0.2, IsWeakFoot=false.

```
Velocity calculation (V_OFFSET_Ground = 8.0):
  V_base = 8.0 + (17/20) × (12/35) × (18.0 - 8.0)
         = 8.0 + 0.85 × 0.343 × 10.0
         = 8.0 + 2.914
         = 10.914 m/s
  FatigueModifier = 1.0 - 0.1 × 0.20 = 0.98
  V_launch = 10.914 × 0.98 = 10.70 m/s ← Short, crisp ground pass ✓

Error calculation:
  BASE_ERROR_Ground = 1.5°
  PassingModifier(18)     = 2.8 - (17/19) × 2.35 = 2.8 - 2.105 = 0.695
  PressureModifier(0.1)   = 1.0 + 0.1 × 0.50 = 1.05
  FatigueModifier(0.1)    = 1.0 + 0.1 × 0.20 = 1.02
  OrientationModifier(5°) = 1.0 + (5/90) × 1.5 = 1.083
  UrgencyModifier(0.2)    = 1.0 + 0.2 × 0.35 = 1.07
  WeakFoot                = 1.0

  ErrorAngle = 1.5° × 0.695 × 1.05 × 1.02 × 1.083 × 1.07 × 1.0
             = 1.5° × 0.868
             = 1.30°

Football realism check: An elite passer (Passing=18) making a 12m pass under minimal
pressure should produce < 2° error. At 12m range, 1.30° → lateral miss ≈ 12 × tan(1.30°)
≈ 0.27m. This is within a comfortable reception range. ✓
```

#### B.5.2 VS-002: Average Passer, Long-Range Lofted Pass Under Pressure

**Input profile:** Passing=10, Technique=9, KickPower=11, Distance=45m,
Fatigue=0.5, Pressure=0.7, BodyAngle=20°, Urgency=0.5, IsWeakFoot=false.

```
Velocity (V_OFFSET_Lofted = 9.0 [GT]):
  V_base = 9.0 + (11/20) × (45/55) × (22.0 - 9.0)
         = 9.0 + 0.55 × 0.818 × 13.0
         = 9.0 + 5.849
         = 14.849 m/s
  FatigueModifier = 1.0 - 0.5 × 0.20 = 0.90
  V_launch = 14.849 × 0.90 = 13.36 m/s ← Long lofted, reasonable velocity ✓

Error:
  BASE_ERROR_Lofted = 3.0°
  PassingModifier(10)     = 2.8 - (9/19) × 2.35 = 1.687
  PressureModifier(0.7)   = 1.0 + 0.7 × 0.50 = 1.35
  FatigueModifier(0.5)    = 1.0 + 0.5 × 0.20 = 1.10
  OrientationModifier(20°)= 1.0 + (20/90) × 1.5 = 1.333
  UrgencyModifier(0.5)    = 1.0 + 0.5 × 0.35 = 1.175

  ErrorAngle = 3.0° × 1.687 × 1.35 × 1.10 × 1.333 × 1.175
             = 3.0° × 3.502
             = 10.51°

  Clamp to [0.1°, 18.0°]: 10.51° (no clamp)

Football realism check: An average player attempting a 45m lofted pass under heavy
pressure and fatigue at 10.51° error → lateral miss ≈ 45 × tan(10.51°) ≈ 8.37m.
This is a poor pass — approximately 2 player-widths off target. Realistic for these
conditions. An elite midfielder would not be expected to complete this pass reliably. ✓
```

---

