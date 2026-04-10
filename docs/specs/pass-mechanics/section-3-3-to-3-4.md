# Pass Mechanics Specification #5 — Section 3.3–3.4: Launch Angle Derivation and Spin Vector Calculation

**File:** `Pass_Mechanics_Spec_Section_3_3_to_3_4_v1_0.md`
**Purpose:** Defines the complete launch angle derivation (§3.3) and spin vector
calculation (§3.4) for Pass Mechanics Specification #5. §3.3 computes the vertical
launch angle from pass type, intended distance, and per-type apex height constants.
§3.4 computes the three-component spin vector from pass type, agent Technique
attribute, and crossing foot direction. Together with §3.2 (kick speed), these two
subsections produce the full velocity and spin arguments to `Ball.ApplyKick()`.

**Created:** March 7, 2026, 12:00 PM PST
**Version:** 1.0
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 5 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)
**Prerequisites:** Section 3.1 v1.1 (PhysicalProfile record), Section 3.2 v1.0 (kickSpeed)

**Dependencies:**
- §3.1 v1.1 — PhysicalProfile angle bounds (angleMin, angleMax), dominant spin type,
  spinMagnitudeBase, spinMagnitudeMax
- §3.2 v1.0 — kickSpeed scalar (magnitude for velocity Vector3 construction)
- Ball Physics Spec #1 — `Ball.ApplyKick()` spin vector interpretation; Magnus force model
- Agent Movement Spec #2 — `AgentAttributes.Technique` [1–20]
- Appendix A.2 (launch angle derivation), A.4 (spin vector derivation)

---

## Table of Contents

- [3.3 Launch Angle Derivation](#33-launch-angle-derivation)
  - [3.3.1 Responsibilities and Scope](#331-responsibilities-and-scope)
  - [3.3.2 Inputs and Outputs](#332-inputs-and-outputs)
  - [3.3.3 Ground-Type Angle Formula (Linear Interpolation)](#333-ground-type-angle-formula-linear-interpolation)
  - [3.3.4 Aerial-Type Angle Formula (Apex-Derived)](#334-aerial-type-angle-formula-apex-derived)
  - [3.3.5 Angle Clamping Policy](#335-angle-clamping-policy)
  - [3.3.6 Velocity Vector3 Construction](#336-velocity-vector3-construction)
  - [3.3.7 Constants Reference](#337-constants-reference)
  - [3.3.8 Boundary Verification](#338-boundary-verification)
  - [3.3.9 Failure Modes](#339-failure-modes)
  - [3.3.10 Design Decisions and Rationale](#3310-design-decisions-and-rationale)
- [3.4 Spin Vector Calculation](#34-spin-vector-calculation)
  - [3.4.1 Responsibilities and Scope](#341-responsibilities-and-scope)
  - [3.4.2 Inputs and Outputs](#342-inputs-and-outputs)
  - [3.4.3 TechniqueScale Derivation](#343-techniquescale-derivation)
  - [3.4.4 SpinMagnitude Formula](#344-spinmagnitude-formula)
  - [3.4.5 Spin Axis Assignment](#345-spin-axis-assignment)
  - [3.4.6 Spin Vector Construction — Implementation Reference](#346-spin-vector-construction--implementation-reference)
  - [3.4.7 Constants Reference](#347-constants-reference)
  - [3.4.8 Boundary Verification](#348-boundary-verification)
  - [3.4.9 Failure Modes](#349-failure-modes)
  - [3.4.10 Design Decisions and Rationale](#3410-design-decisions-and-rationale)
- [Cross-Specification Dependencies](#cross-specification-dependencies)
- [Open Issues](#open-issues)
- [Version History](#version-history)

---

## 3.3 Launch Angle Derivation

### 3.3.1 Responsibilities and Scope

§3.3 computes a **launch angle in degrees above horizontal** for a given pass. It is
called once per pass execution during the INITIATING state of the Pass Execution State
Machine (§3.8). The output is consumed in two places:

1. As the elevation component when constructing the kick velocity `Vector3` (§3.3.6),
   combined with kickSpeed from §3.2 and kick direction from §3.6 (Target Resolution).
2. As context for through-ball arrival time estimation in §3.6 — aerial passes use a
   flight arc, not straight-line distance.

§3.3 does **not**:
- Compute kick speed (§3.2)
- Compute spin (§3.4)
- Simulate ball trajectory after launch (Ball Physics #1)
- Select pass type (Decision Tree #8)

Two distinct formula paths exist:
- **Ground-type passes** (Ground, Driven, ThroughBall, Cross Flat, Cross Whipped):
  Linear interpolation between angleMin and angleMax based on distance ratio.
- **Aerial-type passes** (Lofted, AerialThrough, Cross High, Chip): Inverse tangent
  derivation from a per-type design apex height constant H.

---

### 3.3.2 Inputs and Outputs

**Inputs:**

| Input | Source | Type | Range | Notes |
|-------|--------|------|-------|-------|
| `PassType` | `PassRequest.PassType` | enum | 7 values | Determines formula path |
| `CrossSubType` | `PassRequest.CrossSubType` | enum | 3 values | Only relevant for Cross |
| `IntendedDistance` | `PassRequest.IntendedDistance` | float | > 0.0 m | Distance to target |
| `PhysicalProfile` | §3.1 — PassTypeProfiles | record | — | Contains angleMin, angleMax, distMax |

**Outputs:**

| Output | Destination | Type | Notes |
|--------|-------------|------|-------|
| `launchAngleDeg` | §3.3.6 (Vector3 construction) | float (degrees) | Always within [angleMin, angleMax] |

---

### 3.3.3 Ground-Type Angle Formula (Linear Interpolation)

**Applies to:** Ground, Driven, ThroughBall, Cross (Flat), Cross (Whipped)

These pass types use a fixed angle range with a minor distance correction. Longer
passes within the type's envelope receive a slightly steeper angle to compensate for
surface interaction losses over distance. The correction is linear — the simplest
form consistent with the observed behaviour documented in [LEES-1998].

```
θ(D) = angleMin + (D / distMax) × (angleMax - angleMin)
```

Where:
- `D` = IntendedDistance (metres), capped at `distMax` before use
- `angleMin`, `angleMax`, `distMax` = from PhysicalProfile (§3.1.4)

**Interpretation:** At D = 0, θ = angleMin (minimum elevation). At D = distMax,
θ = angleMax (maximum elevation for this type). The relationship is strictly
monotone increasing in D — longer passes are launched steeper.

**Example — Ground pass at 20m:**
```
θ = 2° + (20/30) × (5° - 2°) = 2° + 0.667 × 3° = 4.0°
```

---

### 3.3.4 Aerial-Type Angle Formula (Apex-Derived)

**Applies to:** Lofted, AerialThrough, Cross (High), Chip

For passes with a significant aerial phase, the launch angle is derived from a
per-type **design apex height** H — the intended peak altitude of the ball's arc.
This produces physically correct angles that decrease as distance increases (a
longer pass at the same apex height requires a shallower launch).

```
θ(D) = atan(4H / D)
```

Where:
- `H` = APEX_HEIGHT constant per pass type [GT] (metres)
- `D` = IntendedDistance (metres), capped at `distMax` before use

**Derivation summary (full derivation in Appendix A.2.2):**

From simplified projectile mechanics with apex height H and horizontal range D:
```
tan(θ) = 4H / D
```

This relationship produces angles that are **inversely proportional to distance**:
shorter passes require steeper angles to reach the same apex. This is physically
correct — a 15m chip over a goalkeeper requires a much steeper trajectory than a
45m lofted diagonal.

**Example — Lofted pass at 30m (H = 6.0m):**
```
θ = atan(4 × 6.0 / 30.0) = atan(0.8) = 38.66°
```

**Example — Chip at 12m (H = 4.5m):**
```
θ = atan(4 × 4.5 / 12.0) = atan(1.5) = 56.31°
```

---

### 3.3.5 Angle Clamping Policy

```csharp
float launchAngleDeg = Mathf.Clamp(θ_computed, profile.angleMin, profile.angleMax);
```

After computing θ via the appropriate formula, the result is clamped to the
PhysicalProfile's `[angleMin, angleMax]` range. This is defensive — the formula
should always produce values within bounds for valid inputs, but extreme distance
values near the edges of the envelope may push the formula outside the design range.

**When does clamping fire?**

For the linear formula (§3.3.3): never, by construction — the formula is bounded
by [angleMin, angleMax] for D ∈ [0, distMax].

For the apex formula (§3.3.4): at extreme short distances. As D → 0,
atan(4H/D) → 90°. The angleMax clamp prevents physically implausible near-vertical
launches. Practically, this fires only when IntendedDistance < distMin (a Decision
Tree contract violation, logged as a warning).

If clamping fires, log at DEBUG level:
```
Debug.Log($"[LaunchAngle] Clamped for {passType}: computed={θ:F1}° → clamped={launchAngleDeg:F1}°");
```

---

### 3.3.6 Velocity Vector3 Construction

Once launchAngleDeg is determined, the full kick velocity Vector3 is constructed
by combining kickSpeed (§3.2), kickDirection (§3.6), and launchAngleDeg:

```csharp
/// <summary>
/// Constructs the 3D kick velocity vector from scalar speed, horizontal direction,
/// and launch angle. Called once per pass during INITIATING state.
/// </summary>
/// <param name="kickSpeed">Scalar speed (m/s) from §3.2</param>
/// <param name="kickDirection">Horizontal unit vector toward target from §3.6</param>
/// <param name="launchAngleDeg">Launch angle (degrees) from §3.3</param>
/// <returns>3D velocity vector for Ball.ApplyKick()</returns>
public static Vector3 ConstructKickVelocity(
    float kickSpeed,
    Vector3 kickDirection,  // normalised, horizontal (Y = 0)
    float launchAngleDeg)
{
    // Convert angle to radians for trig
    float θ_rad = launchAngleDeg * Mathf.Deg2Rad;

    // Decompose speed into horizontal and vertical components
    float V_horizontal = kickSpeed * Mathf.Cos(θ_rad);
    float V_vertical   = kickSpeed * Mathf.Sin(θ_rad);

    // Construct 3D vector (Unity: Y = up)
    Vector3 velocity = kickDirection * V_horizontal + Vector3.up * V_vertical;

    return velocity;
}
```

**Trig cost note:** This function calls `cos()` and `sin()` once each per pass.
§6.4 evaluated LUT vs runtime trig and concluded runtime trig is retained for
Stage 0 — the savings of ~0.067ms/match are below measurement noise. The profiling
marker `PassMech.LaunchAngle` (§6.7) isolates this cost for runtime validation.

**Coordinate system:** Unity Y-up convention. `kickDirection` is a horizontal unit
vector with Y = 0, produced by §3.6 (Target Resolution). The resulting velocity
vector has Y > 0 for all aerial passes and Y ≈ 0 for ground passes.

---

### 3.3.7 Constants Reference

All constants stored in `PassConstants.cs`. Per-type angle bounds are in
`PassTypeProfiles.cs` (§3.1.4).

#### Per-Type Angle Bounds (from §3.1.4 Master Table)

| Pass Type | angleMin | angleMax | Formula Path | Notes |
|-----------|----------|----------|--------------|-------|
| Ground | 2° | 5° | Linear | [GT] within [LEES-1998] 2°–8° |
| Driven | 5° | 12° | Linear | [GT] within [LEES-1998] 5°–15° |
| ThroughBall | 2° | 5° | Linear | Same as Ground |
| AerialThrough | 25° | 40° | Apex | H = APEX_HEIGHT_AERIAL_THROUGH |
| Cross (Flat) | 8° | 15° | Linear | [GT] |
| Cross (Whipped) | 15° | 25° | Linear | [GT] |
| Cross (High) | 25° | 40° | Apex | H = APEX_HEIGHT_CROSS_HIGH |
| Lofted | 20° | 45° | Apex | H = APEX_HEIGHT_LOFTED |
| Chip | 45° | 65° | Apex | H = APEX_HEIGHT_CHIP |

#### Apex Height Constants [GT]

| Constant | Value | Rationale |
|----------|-------|-----------|
| `APEX_HEIGHT_LOFTED` | 6.0 m [GT] | Produces ~39° at 30m; clears defensive line at typical 1.8m player height with margin |
| `APEX_HEIGHT_CHIP` | 4.5 m [GT] | Produces ~56° at 12m; sufficient to clear goalkeeper at close range |
| `APEX_HEIGHT_AERIAL_THROUGH` | 5.0 m [GT] | Between Lofted and Chip — needs to clear defenders but arrive lower than a lofted ball for easier reception |
| `APEX_HEIGHT_CROSS_HIGH` | 5.5 m [GT] | Produces high hanging crosses for headed finishes; clears near-post defenders |

**Tuning guidance:** Apex heights control the "shape" of aerial passes. Increasing
H produces steeper angles at short range and shallower angles at long range. If
aerial passes feel too "floaty," reduce H by 0.5m increments. If they feel too flat,
increase. All changes require re-running LA-* tests and VS-* validation scenarios.

---

### 3.3.8 Boundary Verification

**Check 1: Ground pass, minimum distance → angleMin**
```
D = distMin_Ground = 3m:
θ = 2° + (3/30) × 3° = 2° + 0.3° = 2.3°
Clamp to [2°, 5°]: 2.3° ✓ (above angleMin, no clamp)
```

**Check 2: Ground pass, maximum distance → angleMax**
```
D = distMax_Ground = 30m:
θ = 2° + (30/30) × 3° = 2° + 3° = 5.0°
Clamp to [2°, 5°]: 5.0° ✓ (exactly angleMax)
```

**Check 3: Lofted pass, D = 30m → §3.1 range [20°, 45°]**
```
θ = atan(4 × 6.0 / 30.0) = atan(0.8) = 38.66°
38.66° ∈ [20°, 45°] ✓
```

**Check 4: Chip, D = 12m → §3.1 range [45°, 65°]**
```
θ = atan(4 × 4.5 / 12.0) = atan(1.5) = 56.31°
56.31° ∈ [45°, 65°] ✓
```

**Check 5: Lofted angle decreases with distance (LA-006 corrected)**
```
D = 15m: θ = atan(24/15) = atan(1.6) = 57.99° → clamped to 45° (angleMax)
D = 45m: θ = atan(24/45) = atan(0.533) = 28.07°
45° > 28.07° ✓ (steeper angle for shorter distance — physically correct)
```

**Check 6: Aerial type at very short distance → angleMax clamp fires**
```
D = 3m, Lofted: θ = atan(24/3) = atan(8.0) = 82.87°
Clamp to [20°, 45°]: 45° (clamp fires — this is a DT contract violation warning)
```

---

### 3.3.9 Failure Modes

| FM ID | Trigger | Response | Notes |
|-------|---------|----------|-------|
| FM-07 | `IntendedDistance ≤ 0` | Caught by caller before §3.3 is invoked | §3.3 has defensive guard only |
| — | θ_computed > angleMax | Clamp to angleMax; log DEBUG | Normal for aerial types at short range |
| — | θ_computed < angleMin | Clamp to angleMin; log DEBUG | Should not occur with valid formula; defensive guard |
| FM-09 | atan() input produces NaN (D = 0 in apex formula) | Return angleMin; log error | FM-07 should prevent this; defensive only |

---

### 3.3.10 Design Decisions and Rationale

**DD-3.3-01: Two formula paths rather than one unified formula**

A single formula covering all pass types would require conditional logic for apex
height vs fixed-range types, producing the same two-path structure internally while
being harder to audit. Two explicit paths are clearer and map directly to the
physical distinction: ground-trajectory passes have angle proportional to distance,
aerial passes have angle inversely proportional to distance.

**DD-3.3-02: Design apex height H rather than back-calculated from trajectory**

H could be derived from ball speed and target distance via full projectile equations
including drag. This would couple §3.3 to Ball Physics' drag model — a dependency
we explicitly avoid (§1.3 KD-2). Instead, H is a [GT] constant chosen to produce
visually appealing arcs. Ball Physics handles the actual trajectory; Pass Mechanics
only sets the initial conditions.

**DD-3.3-03: Distance capped at distMax before formula (consistent with §3.2)**

If IntendedDistance > distMax, distance is silently capped to distMax in the formula.
A warning is logged (consistent with DD-3.2-03). The pass is not rejected — graceful
degradation is preferred. The capped distance produces the maximum angle the profile
allows, which is the best-effort result for an over-distance request.

**DD-3.3-04: Trig functions retained at runtime (§6.4 decision)**

`atan()`, `sin()`, and `cos()` are called once each per pass evaluation. §6.4
evaluated lookup tables and concluded runtime trig is retained for Stage 0 — savings
are below measurement noise. The `PassMech.LaunchAngle` profiling marker (§6.7)
monitors this at runtime. If Phase 5 exceeds 10% of total cost at p95, the LUT
option is reconsidered at Stage 1.

---

## 3.4 Spin Vector Calculation

### 3.4.1 Responsibilities and Scope

§3.4 computes the **spin Vector3** (angular velocity in rad/s) for a given pass.
The spin vector is passed to `Ball.ApplyKick()` where Ball Physics applies Magnus
force throughout the ball's flight. Spin determines curve (sidespin), dip/hold-up
(topspin/backspin), and knuckling behaviour (very low spin).

§3.4 is called once per pass execution during the INITIATING state. It is a pure
calculation with no side effects or state.

§3.4 does **not**:
- Compute kick speed (§3.2)
- Compute launch angle (§3.3)
- Simulate Magnus effect during flight (Ball Physics #1)
- Determine which foot the ball is struck with (Decision Tree #8 sets `IsWeakFoot`)

---

### 3.4.2 Inputs and Outputs

**Inputs:**

| Input | Source | Type | Range | Notes |
|-------|--------|------|-------|-------|
| `PassType` | `PassRequest.PassType` | enum | 7 values | Determines spin axis |
| `CrossSubType` | `PassRequest.CrossSubType` | enum | 3 values | Only relevant for Cross |
| `Technique` | `AgentAttributes.Technique` | float | [1.0, 20.0] | Scales spin magnitude |
| `IsWeakFoot` | `PassRequest.IsWeakFoot` | bool | — | Affects sidespin sign for crosses |
| `PhysicalProfile` | §3.1 — PassTypeProfiles | record | — | Contains spinMagnitudeBase, spinMagnitudeMax, dominantSpin |

**Outputs:**

| Output | Destination | Type | Notes |
|--------|-------------|------|-------|
| `spinVector` | `Ball.ApplyKick()` spin parameter | Vector3 (rad/s) | Axis encodes direction; magnitude encodes rate |

---

### 3.4.3 TechniqueScale Derivation

The Technique attribute [1, 20] maps to a spin multiplier in the range [0.5, 1.5]:

```
TechniqueScale(T) = TECHNIQUE_SPIN_MIN + ((T - 1) / (ATTR_MAX - 1)) × (TECHNIQUE_SPIN_MAX - TECHNIQUE_SPIN_MIN)
                  = 0.5 + (T - 1) / 19.0
```

**Boundary verification:**

| Technique | TechniqueScale | Interpretation |
|-----------|---------------|----------------|
| 1 (poor) | 0.500 | Half of base spin — poor contact reduces spin generation |
| 5 | 0.711 | Below-average spin |
| 10 (mid) | 0.974 | Near-neutral — base spin magnitude approximately preserved |
| 15 | 1.237 | Above-average spin |
| 20 (elite) | 1.500 | 50% above base — elite technique maximises spin transfer |

**Why [0.5, 1.5] and not [0.0, 1.0]?**

A TechniqueScale of 0.0 would produce zero spin. Zero spin disables Magnus effect
silently in Ball Physics, producing flat trajectories for all passes regardless of
pass type intent. A minimum of 0.5 ensures the spin floor is always active —
even a poor technician generates some spin through foot-to-ball contact. The 1.5
ceiling provides meaningful upside for elite players without producing physically
implausible spin rates. See Appendix A.4.1 for full rationale.

---

### 3.4.4 SpinMagnitude Formula

```
SpinMagnitude(T, PassType) = clamp(
    SPIN_BASE(PassType) × TechniqueScale(T),
    SPIN_MIN,
    profile.spinMagnitudeMax
)
```

Where:
- `SPIN_BASE(PassType)` = per-type base spin magnitude (rad/s) from PhysicalProfile [GT]
- `TechniqueScale(T)` = from §3.4.3
- `SPIN_MIN` = 1.0 rad/s — absolute floor preventing knuckling (see DD-3.4-01)
- `profile.spinMagnitudeMax` = per-type ceiling from PhysicalProfile [GT]

**Why a floor at SPIN_MIN = 1.0 rad/s?**

[HONG-2012] documents that below ~1.0 rad/s, a football enters a knuckling regime
where turbulent airflow produces unpredictable lateral deviations. Knuckling is not
modelled in Stage 0 Ball Physics. To prevent silent entry into an unmodelled
physical regime, a minimum spin of 1.0 rad/s is enforced. See DD-3.4-01.

---

### 3.4.5 Spin Axis Assignment

Spin axis assignment follows [ASAI-2002] and [BRAY-2003] physical conventions,
mapped to Unity's Y-up coordinate system (Y = up, Z = forward, X = right):

| Pass Type | Spin Axis | Component | Physical Effect |
|-----------|-----------|-----------|-----------------|
| Ground | +X (forward roll) | `(SpinMag, 0, 0)` | Topspin — ball accelerates on ground contact |
| Driven | +X (forward roll) | `(SpinMag, 0, 0)` | Strong topspin — keeps trajectory flat |
| ThroughBall | +X (forward roll) | `(SpinMag, 0, 0)` | Topspin — same as Ground |
| Lofted | +X (reduced) | `(SpinMag × 0.7, 0, 0)` | Mild topspin — ball dips slightly on descent |
| AerialThrough | +X (reduced) | `(SpinMag × 0.7, 0, 0)` | Mild topspin — same as Lofted |
| Chip | −X (reverse) | `(-SpinMag, 0, 0)` | Backspin — ball checks on landing |
| Cross (Flat) | ±Y (vertical axis) | `(0, sign × SpinMag, 0)` | Sidespin — produces inswing/outswing curl |
| Cross (Whipped) | ±Y (vertical axis) | `(0, sign × SpinMag, 0)` | Strong sidespin — maximum curl |
| Cross (High) | +X and ±Y (mixed) | `(SpinMag × 0.5, sign × SpinMag × 0.5, 0)` | Topspin + sidespin — hanging cross with curl |

**Sidespin sign convention for crosses:**

```
sidespinSign = IsWeakFoot ? -1 : +1
```

A right-footed player crossing from the right flank produces inswing (positive Y
spin in Unity convention). A left-footed player from the same position produces
outswing. The `IsWeakFoot` flag on PassRequest encodes which foot is being used;
§3.4 does not reason about foot preference — it only applies the sign.

**Design note:** The 0.7 multiplier on Lofted/AerialThrough topspin and the 0.5
split on Cross (High) are [GT] constants. They are embedded in the spin axis
assignment rather than extracted as named constants because they are axis-mixing
ratios, not standalone parameters. If playtesting requires per-type tuning, extract
to `LOFTED_TOPSPIN_FRACTION` and `CROSS_HIGH_MIX_FRACTION` in PassConstants.cs.

---

### 3.4.6 Spin Vector Construction — Implementation Reference

```csharp
/// <summary>
/// Computes the spin Vector3 for a pass. Called once per pass during INITIATING state.
/// </summary>
/// <param name="passType">Pass type enum</param>
/// <param name="crossSubType">Cross sub-type; only used when passType == Cross</param>
/// <param name="technique">Agent Technique attribute [1, 20]</param>
/// <param name="isWeakFoot">True if non-preferred foot (affects sidespin sign)</param>
/// <param name="profile">Physical profile from PassTypeProfiles (§3.1)</param>
/// <returns>Spin vector (rad/s) for Ball.ApplyKick()</returns>
public static Vector3 ComputeSpinVector(
    PassType passType,
    CrossSubType crossSubType,
    float technique,
    bool isWeakFoot,
    PhysicalProfile profile)
{
    // Step 1: Clamp Technique to valid range
    float T = Mathf.Clamp(technique, 1.0f, PassConstants.ATTR_MAX);

    // Step 2: Compute TechniqueScale [0.5, 1.5]
    float techScale = PassConstants.TECHNIQUE_SPIN_MIN
                    + ((T - 1.0f) / (PassConstants.ATTR_MAX - 1.0f))
                    * (PassConstants.TECHNIQUE_SPIN_MAX - PassConstants.TECHNIQUE_SPIN_MIN);

    // Step 3: Compute spin magnitude with floor and ceiling
    float spinMag = Mathf.Clamp(
        profile.spinMagnitudeBase * techScale,
        PassConstants.SPIN_MIN,
        profile.spinMagnitudeMax);

    // Step 4: Assign spin axis based on pass type
    float sidespinSign = isWeakFoot ? -1.0f : 1.0f;

    switch (passType)
    {
        case PassType.Ground:
        case PassType.Driven:
        case PassType.ThroughBall:
            // Topspin — forward roll around X axis
            return new Vector3(spinMag, 0f, 0f);

        case PassType.Lofted:
        case PassType.AerialThrough:
            // Mild topspin — reduced forward roll
            return new Vector3(spinMag * PassConstants.LOFTED_TOPSPIN_FRACTION, 0f, 0f);

        case PassType.Chip:
            // Backspin — reverse roll around X axis
            return new Vector3(-spinMag, 0f, 0f);

        case PassType.Cross:
            if (crossSubType == CrossSubType.High)
            {
                // Mixed topspin + sidespin
                float mixFraction = PassConstants.CROSS_HIGH_MIX_FRACTION;
                return new Vector3(
                    spinMag * mixFraction,
                    sidespinSign * spinMag * mixFraction,
                    0f);
            }
            else // Flat or Whipped
            {
                // Pure sidespin — curl
                return new Vector3(0f, sidespinSign * spinMag, 0f);
            }

        default:
            // Should not reach here — FM-01 catches invalid PassType
            Debug.LogError($"[SpinVector] Unexpected PassType: {passType}");
            return Vector3.zero;
    }
}
```

> **Note:** This pseudocode is for specification clarity. Actual implementation will
> follow Unity coding standards per Development Best Practices.

---

### 3.4.7 Constants Reference

All constants stored in `PassConstants.cs`.

#### Universal Spin Constants

| Constant | Value | Tag | Source | Notes |
|----------|-------|-----|--------|-------|
| `ATTR_MAX` | 20.0 | [FIXED] | [MASTER-VOL2] | Maximum attribute value |
| `TECHNIQUE_SPIN_MIN` | 0.5 | [GT] | Design decision | TechniqueScale floor |
| `TECHNIQUE_SPIN_MAX` | 1.5 | [GT] | Design decision | TechniqueScale ceiling |
| `SPIN_MIN` | 1.0 rad/s | [EST] | [HONG-2012] knuckling threshold | Absolute spin floor |
| `LOFTED_TOPSPIN_FRACTION` | 0.7 | [GT] | Design decision | Mild topspin for lofted/aerial types |
| `CROSS_HIGH_MIX_FRACTION` | 0.5 | [GT] | Design decision | Equal topspin/sidespin split for high crosses |

#### Per-Type Spin Constants (from §3.1.4 PhysicalProfile)

| Pass Type | spinMagnitudeBase (rad/s) | spinMagnitudeMax (rad/s) | Dominant Spin |
|-----------|--------------------------|--------------------------|---------------|
| Ground | 8.0 | 15.0 | Topspin |
| Driven | 10.0 | 18.0 | Strong Topspin |
| Lofted | 8.0 | 15.0 | Mild Topspin (×0.7) |
| ThroughBall | 8.0 | 15.0 | Topspin |
| AerialThrough | 8.0 | 15.0 | Mild Topspin (×0.7) |
| Cross (Flat) | 15.0 | 25.0 | Sidespin |
| Cross (Whipped) | 18.0 | 28.0 | Strong Sidespin |
| Cross (High) | 12.0 | 22.0 | Mixed (×0.5/×0.5) |
| Chip | 12.0 | 20.0 | Backspin |

> ⚠ All spin constants are [GT] estimates. Cross (Whipped) spinMagnitudeMax =
> 28.0 rad/s requires validation against Ball Physics Magnus force model before
> finalisation (XC-3.1-03). [BRAY-2003] documents general kicked-pass spin at
> 6–25 rad/s; 28 rad/s is slightly above this range and must be confirmed to
> not produce implausible curl at 50m delivery distance.

---

### 3.4.8 Boundary Verification

**Check 1: Minimum technique, Ground → spin floor**
```
TechniqueScale(1) = 0.5
SpinMag = max(1.0, 8.0 × 0.5) = max(1.0, 4.0) = 4.0 rad/s
Spin = (4.0, 0, 0) ✓ (above SPIN_MIN, topspin)
```

**Check 2: Maximum technique, Cross (Whipped) → spinMagnitudeMax**
```
TechniqueScale(20) = 1.5
SpinMag = clamp(18.0 × 1.5, 1.0, 28.0) = clamp(27.0, 1.0, 28.0) = 27.0 rad/s
Spin = (0, ±27.0, 0) ✓ (below ceiling, sidespin)
```

**Check 3: Mid-range technique, Cross → within [BRAY-2003] range**
```
TechniqueScale(10) = 0.974
SpinMag = clamp(15.0 × 0.974, 1.0, 25.0) = clamp(14.61, 1.0, 25.0) = 14.61 rad/s
14.61 ∈ [6, 25] rad/s ([BRAY-2003] general kicked-pass range) ✓
```

**Check 4: Chip backspin sign**
```
TechniqueScale(15) = 1.237
SpinMag = clamp(12.0 × 1.237, 1.0, 20.0) = clamp(14.84, 1.0, 20.0) = 14.84 rad/s
Spin = (-14.84, 0, 0) ✓ (negative X = backspin)
```

**Check 5: Sidespin sign convention (right-footed player, dominant foot)**
```
IsWeakFoot = false → sidespinSign = +1
Cross (Flat): Spin = (0, +14.61, 0) ✓ (positive Y = inswing from right)
```

**Check 6: Sidespin sign convention (right-footed player, weak foot = left)**
```
IsWeakFoot = true → sidespinSign = -1
Cross (Flat): Spin = (0, -14.61, 0) ✓ (negative Y = outswing from left)
```

---

### 3.4.9 Failure Modes

| FM ID | Trigger | Response | Notes |
|-------|---------|----------|-------|
| — | `Technique` attribute read fails | Use fallback `Technique = ATTR_MAX / 2 = 10.0`; log warning | Temporary fallback |
| FM-10 | Spin vector produces NaN (should be impossible with valid inputs) | Return `Vector3.zero`; log error | Defensive guard before ApplyKick() |
| — | SPIN_MIN clamp fires | Log at DEBUG; not a failure mode | Indicates very low technique; investigate if frequent |

---

### 3.4.10 Design Decisions and Rationale

**DD-3.4-01: SPIN_MIN = 1.0 rad/s floor prevents knuckling entry**

[HONG-2012] identifies ~1.0 rad/s as the boundary below which turbulent airflow
produces knuckling behaviour. Stage 0 Ball Physics does not model knuckling — it
assumes smooth-flow Magnus force. Allowing spin to reach zero would silently produce
incorrect aerodynamic behaviour. The 1.0 rad/s floor keeps the ball in the modelled
regime. Knuckling may be added in Stage 2+ as an explicit feature.

**DD-3.4-02: TechniqueScale [0.5, 1.5] range — not [0, 2] or [0.8, 1.2]**

The range [0.5, 1.5] was chosen to produce meaningful spin differentiation without
extreme values. [0, 2] would allow zero spin (violating DD-3.4-01 before the clamp)
and double spin (potentially exceeding Magnus model validity). [0.8, 1.2] would
produce too narrow a range — elite and poor technicians would be nearly identical
in spin output. [0.5, 1.5] provides a 3:1 ratio (1.5/0.5) between elite and poor,
consistent with the observed variation in spin rates across skill levels.

**DD-3.4-03: Axis-mixing ratios embedded vs extracted as constants**

The 0.7 (Lofted topspin fraction) and 0.5 (Cross High mix fraction) are initially
embedded in the switch statement rather than extracted as named constants. This
simplifies the initial implementation. If playtesting requires per-type tuning,
extract to `LOFTED_TOPSPIN_FRACTION` and `CROSS_HIGH_MIX_FRACTION` in PassConstants.cs.
The pseudocode in §3.4.6 already uses extracted constants anticipating this need.

**DD-3.4-04: Sidespin sign derived from IsWeakFoot, not from explicit left/right flag**

The PassRequest carries `IsWeakFoot` (bool) rather than `KickingFoot` (enum). This
is a Stage 0 simplification — `IsWeakFoot = false` means dominant foot (inswing for
right-footed players from right flank), `IsWeakFoot = true` means weak foot (outswing).
A full `PreferredFoot` enum replacing this bool is planned for Stage 1 (§7.1.4).

---

## Cross-Specification Dependencies

| Dependency | Owner | Status | Blocks |
|------------|-------|--------|--------|
| PhysicalProfile angle bounds | §3.1 v1.1 | ✅ Defined | None |
| kickSpeed scalar | §3.2 v1.0 | ✅ Defined | None |
| Ball Physics Magnus model handles spinMagnitudeMax values | Ball Physics §3.x / XC-3.1-03 | ⚠ REQUIRED before approval | §3.4 spin constants |
| Ball Physics interprets spin vector axes consistently with §3.4.5 | Ball Physics §3.1.11.2 | ⚠ REQUIRED before approval | §3.4 axis assignment |
| `AgentAttributes.Technique` field confirmed | Agent Movement §3.5 | ✅ Confirmed | None |

---

## Open Issues

| OI ID | Issue | Severity | Resolution |
|-------|-------|----------|------------|
| OI-3.3-01 | APEX_HEIGHT values are [GT] placeholders — require visual validation with Ball Physics trajectory rendering | LOW | Calibrate during playtesting; adjust in 0.5m increments |
| OI-3.4-01 | Cross (Whipped) spinMagnitudeMax = 28.0 rad/s slightly above [BRAY-2003] range | MODERATE | Complete XC-3.1-03 Ball Physics Magnus validation before approval |
| OI-3.4-02 | Sidespin sign convention (±Y) must be validated against Ball Physics Magnus force direction to confirm inswing/outswing mapping | MODERATE | Requires Ball Physics integration test IT-003 |
| OI-3.4-03 | Axis-mixing ratios (0.7, 0.5) embedded as magic numbers in §3.4.5 table | LOW | Extract to named constants if playtesting requires type-specific tuning |

---

## Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | March 7, 2026, 12:00 PM PST | Claude (AI) / Anton | Initial draft. Two formula paths for launch angle (linear interpolation, apex-derived). TechniqueScale [0.5, 1.5] spin multiplier. Per-type spin axis assignment with sidespin sign convention. 4 apex height constants. 6 boundary verification checks per subsection. Full pseudocode for both subsystems. All formulas derived from Appendix A.2 and A.4. |

---

*End of Section 3.3–3.4 — Pass Mechanics Specification #5*

*Next: §3.5–3.6 — Deterministic Error Model and Target Resolution*
