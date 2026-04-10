# Pass Mechanics Specification #5 — Section 3.5–3.6: Deterministic Error Model and Target Resolution

**File:** `Pass_Mechanics_Spec_Section_3_5_to_3_6_v1_0.md`
**Purpose:** Defines the complete deterministic error model (§3.5) — the multiplicative
modifier chain that produces an angular deviation from the intended pass direction —
and the target resolution system (§3.6) — resolving player-targeted and space-targeted
passes, computing through-ball lead distance, and applying the error vector to produce
the final aim direction. Together these subsections determine *where the ball actually
goes* after §3.2 (how fast) and §3.3 (at what angle).

**Created:** March 7, 2026, 1:00 PM PST
**Version:** 1.0
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 5 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)
**Prerequisites:** Section 3.1 v1.1, Section 3.2 v1.0, Section 3.3 v1.0

**Dependencies:**
- §3.1 v1.1 — PhysicalProfile (BASE_ERROR per type)
- §3.2 v1.0 — kickSpeed (for through-ball arrival time estimation)
- §3.3 v1.0 — launchAngleDeg (for velocity vector construction)
- Agent Movement Spec #2 — PlayerAttributes (Passing, Technique, WeakFootRating, Crossing),
  AgentState (Position, Velocity, FacingDirection, Fatigue)
- Collision System Spec #3 — SpatialHash.QueryRadius() for pressure detection
- Appendix A.5 (error chain derivation), A.7 (error direction), A.3 (lead distance)

---

## Table of Contents

- [3.5 Deterministic Error Model](#35-deterministic-error-model)
  - [3.5.1 Responsibilities and Scope](#351-responsibilities-and-scope)
  - [3.5.2 Inputs and Outputs](#352-inputs-and-outputs)
  - [3.5.3 Multiplicative Chain Formula](#353-multiplicative-chain-formula)
  - [3.5.4 Individual Modifier Derivations](#354-individual-modifier-derivations)
  - [3.5.5 Error Angle Clamping](#355-error-angle-clamping)
  - [3.5.6 Pressure Scalar Computation](#356-pressure-scalar-computation)
  - [3.5.7 Error Direction — Deterministic Hash](#357-error-direction--deterministic-hash)
  - [3.5.8 Full Error Computation — Implementation Reference](#358-full-error-computation--implementation-reference)
  - [3.5.9 Constants Reference](#359-constants-reference)
  - [3.5.10 Boundary Verification](#3510-boundary-verification)
  - [3.5.11 Failure Modes](#3511-failure-modes)
  - [3.5.12 Design Decisions and Rationale](#3512-design-decisions-and-rationale)
- [3.6 Target Resolution](#36-target-resolution)
  - [3.6.1 Responsibilities and Scope](#361-responsibilities-and-scope)
  - [3.6.2 Inputs and Outputs](#362-inputs-and-outputs)
  - [3.6.3 Player-Targeted Pass Resolution](#363-player-targeted-pass-resolution)
  - [3.6.4 Space-Targeted Pass Resolution](#364-space-targeted-pass-resolution)
  - [3.6.5 Through-Ball Lead Distance Calculation](#365-through-ball-lead-distance-calculation)
  - [3.6.6 Aim Point Pitch Boundary Clamping](#366-aim-point-pitch-boundary-clamping)
  - [3.6.7 Error Vector Application](#367-error-vector-application)
  - [3.6.8 Full Target Resolution — Implementation Reference](#368-full-target-resolution--implementation-reference)
  - [3.6.9 Constants Reference](#369-constants-reference)
  - [3.6.10 Boundary Verification](#3610-boundary-verification)
  - [3.6.11 Failure Modes](#3611-failure-modes)
  - [3.6.12 Design Decisions and Rationale](#3612-design-decisions-and-rationale)
- [Cross-Specification Dependencies](#cross-specification-dependencies)
- [Open Issues](#open-issues)
- [Version History](#version-history)

---

## 3.5 Deterministic Error Model

### 3.5.1 Responsibilities and Scope

§3.5 computes a **deterministic angular deviation** (in degrees) from the intended
pass direction. This error angle represents inaccuracy caused by the passer's
attributes, environmental pressure, fatigue, body orientation, urgency, and weak-foot
penalty. The error model produces identical output for identical inputs — no random
elements. This is mandatory for replay determinism (KD-1, §1.3).

§3.5 is called once per pass execution during the INITIATING state. It is the only
sub-system in Pass Mechanics that reads from the Collision System (pressure query).

§3.5 does **not**:
- Determine the direction of the error deviation (§3.5.7 handles direction via hash)
- Apply the error to the kick direction (§3.6.7 applies it)
- Compute whether the pass is successful (Ball Physics and First Touch determine this)

---

### 3.5.2 Inputs and Outputs

**Inputs:**

| Input | Source | Type | Range | Notes |
|-------|--------|------|-------|-------|
| `PassType` | `PassRequest.PassType` | enum | 7 values | Determines BASE_ERROR |
| `Passing` | `AgentAttributes.Passing` | float | [1.0, 20.0] | Primary accuracy attribute |
| `Fatigue` | `AgentState.Fatigue` | float | [0.0, 1.0] | 0 = rested, 1 = fatigued |
| `BodyAngle` | Computed | float (degrees) | [0°, 90°] | Angle between facing direction and pass direction |
| `Urgency` | `PassRequest.Urgency` | float | [0.0, 1.0] | From Decision Tree |
| `IsWeakFoot` | `PassRequest.IsWeakFoot` | bool | — | From Decision Tree |
| `WeakFootRating` | `AgentAttributes.WeakFootRating` | int | [1, 5] | 1 = worst, 5 = ambidextrous |
| `PressureScalar` | Computed from Collision System query | float | [0.0, 1.0] | See §3.5.6 |

**Outputs:**

| Output | Destination | Type | Notes |
|--------|-------------|------|-------|
| `errorAngleDeg` | §3.5.7 (direction), §3.6.7 (application) | float (degrees) | Clamped to [MIN_ERROR_ANGLE, MAX_ERROR_ANGLE] |

---

### 3.5.3 Multiplicative Chain Formula

```
ErrorAngle = BASE_ERROR(PassType)
           × PassingModifier(Passing)
           × PressureModifier(PressureScalar)
           × FatigueModifier(Fatigue)
           × OrientationModifier(BodyAngle)
           × UrgencyModifier(Urgency)
           × WeakFootModifier(IsWeakFoot, WeakFootRating)
```

The multiplicative structure ensures:
- Each factor is independent and testable in isolation (PE-001 through PE-010)
- The formula never produces negative error (all factors > 0)
- Compound degradation is proportional — double pressure × double fatigue produces
  quadruple error contribution from those two terms
- Future modifiers (FormModifier, PsychologyModifier) can be appended as additional
  multiplicands at value 1.0 (neutral) without changing existing behaviour

This structure is consistent with the modifier chain pattern in First Touch §3.1
and Collision System §3.3.

---

### 3.5.4 Individual Modifier Derivations

#### BASE_ERROR(PassType)

The per-type base error in degrees — the error an exactly average passer (Passing=10)
produces at neutral conditions (no pressure, no fatigue, aligned body, no urgency,
preferred foot).

| Pass Type | BASE_ERROR (°) | Tag | Rationale |
|-----------|---------------|-----|-----------|
| Ground | 1.5 | [GT] | Short-range, most frequent — low base error |
| Driven | 2.0 | [GT] | Firm pass requires more precision; slightly higher |
| Lofted | 3.0 | [GT] | Long-range amplifies angular error at distance |
| ThroughBall | 2.0 | [GT] | Space-targeted adds lead uncertainty on top |
| AerialThrough | 3.5 | [GT] | Combines aerial and space-targeting difficulty |
| Cross (Flat) | 2.5 | [GT] | Wide delivery under typical wing pressure |
| Cross (Whipped) | 3.0 | [GT] | Curling delivery adds technique-dependent error |
| Cross (High) | 2.5 | [GT] | Hanging cross is less precision-dependent |
| Chip | 2.5 | [GT] | Technical pass — Technique proxy affects error |

#### PassingModifier(P)

Maps Passing [1, 20] to error multiplier. Higher Passing = lower error.

```
PassingModifier(P) = PASSING_ERROR_MAX - ((P - 1) / (ATTR_MAX - 1)) × (PASSING_ERROR_MAX - PASSING_ERROR_MIN)
```

| P | PassingModifier | Interpretation |
|---|----------------|----------------|
| 1 (poor) | 2.800 | Triples base error |
| 10 (average) | 1.687 | Above neutral — average player is imprecise |
| 20 (elite) | 0.450 | Halves base error |

**Design note:** PassingModifier(10) ≈ 1.687, not 1.0. This is intentional — an
"average" attribute of 10 in a [1,20] system is not the centre of a normal
distribution. Most professional footballers would rate 12–16 on this scale. A
Passing=10 player is below-average at the professional level and should produce
above-neutral error. The neutral point (modifier = 1.0) falls at approximately
Passing ≈ 13, which represents a competent professional passer.

#### PressureModifier(S)

```
PressureModifier(S) = 1.0 + S × PRESSURE_WEIGHT
```

| PressureScalar | PressureModifier | Interpretation |
|----------------|-----------------|----------------|
| 0.0 (none) | 1.000 | No pressure — neutral |
| 0.5 (moderate) | 1.250 | +25% error |
| 1.0 (maximum) | 1.500 | +50% error |

[BEILOCK-2007] documents 20–50% accuracy degradation under peak pressure.
PRESSURE_WEIGHT = 0.50 calibrated to the upper end of this range.

#### FatigueModifier(F)

```
FatigueModifier(F) = 1.0 + F × FATIGUE_ACCURACY_REDUCTION
```

**Important:** This is a separate constant from FATIGUE_POWER_REDUCTION (§3.2.5).
Power and accuracy are degraded independently by fatigue. A fatigued player hits
both softer (§3.2) and less accurately (here), but the two effects are tuned
separately.

| Fatigue | FatigueModifier | Interpretation |
|---------|----------------|----------------|
| 0.0 (rested) | 1.000 | No accuracy reduction |
| 0.5 | 1.100 | +10% error |
| 1.0 (fatigued) | 1.200 | +20% error |

#### OrientationModifier(α)

```
OrientationModifier(α) = 1.0 + (clamp(α, 0, 90) / 90.0) × ORIENTATION_MAX_PENALTY
```

Where α = angle in degrees between the agent's facing direction and the pass
direction. Computed as:
```
α = acos(dot(normalize(FacingDirection), normalize(passDirection)))
```

| BodyAngle | OrientationModifier | Interpretation |
|-----------|--------------------|--------------------|
| 0° (aligned) | 1.000 | No penalty |
| 30° | 1.500 | +50% error (passing across body) |
| 60° | 2.000 | +100% error (severely misaligned) |
| 90° (perpendicular) | 2.500 | +150% error (maximum penalty) |

Values beyond 90° are clamped to 90°. Passing fully backwards (>90°) should trigger
a pass refusal from the Decision Tree before reaching Pass Mechanics.

#### UrgencyModifier(U)

```
UrgencyModifier(U) = 1.0 + U × URGENCY_ERROR_SCALE
```

| Urgency | UrgencyModifier | Interpretation |
|---------|----------------|----------------|
| 0.0 (measured) | 1.000 | Full windup — no penalty |
| 0.5 | 1.175 | +17.5% error |
| 1.0 (rushed) | 1.350 | +35% error |

---

### 3.5.5 Error Angle Clamping

```
errorAngleDeg = clamp(ErrorAngle_raw, MIN_ERROR_ANGLE, MAX_ERROR_ANGLE)
```

| Constant | Value | Rationale |
|----------|-------|-----------|
| `MIN_ERROR_ANGLE` | 0.1° [GT] | Even perfect passing has microscopic error — no pass is laser-precise |
| `MAX_ERROR_ANGLE` | 18.0° [GT] | Prevents backward ball redirection from extreme combined modifiers |

**Why 18.0° maximum?** At 18° angular error on a 20m pass, the lateral miss is
20 × tan(18°) ≈ 6.5m — approximately the width of a penalty area. Beyond this,
the pass is so wildly inaccurate that it becomes indistinguishable from a random
kick. The cap prevents the multiplicative chain from producing physically absurd
results under worst-case modifier stacking. See Appendix B.3.2: worst-case raw
error is ~33° before clamping.

---

### 3.5.6 Pressure Scalar Computation

PressureScalar quantifies how much defensive pressure the passer is under at the
moment of pass execution. It is the only component of §3.5 that reads from an
external system (Collision System #3 spatial hash).

```csharp
/// <summary>
/// Computes a pressure scalar [0.0, 1.0] from nearby opponents.
/// Uses Collision System spatial hash query. O(n) where n = opponents in radius.
/// </summary>
public static float ComputePressureScalar(
    Vector3 passerPosition,
    int passerTeamId)
{
    // Query opponents within PRESSURE_RADIUS
    var nearby = CollisionSystem.SpatialHash.QueryRadius(
        passerPosition,
        PassConstants.PRESSURE_RADIUS,
        filter: agent => agent.TeamId != passerTeamId);

    float totalPressure = 0f;

    foreach (var opponent in nearby)
    {
        float distance = Vector3.Distance(passerPosition, opponent.Position);

        // Inverse-distance weighting: closer opponents exert more pressure
        // Guard: distance = 0 produces maximum pressure (1.0 for that opponent)
        float contribution = (distance < 0.01f)
            ? 1.0f
            : Mathf.Clamp01(1.0f - (distance / PassConstants.PRESSURE_RADIUS));

        totalPressure += contribution;
    }

    // Normalise to [0.0, 1.0]: cap at PRESSURE_SCALAR_MAX opponents' worth
    return Mathf.Clamp01(totalPressure / PassConstants.PRESSURE_SCALAR_MAX);
}
```

| Constant | Value | Tag | Notes |
|----------|-------|-----|-------|
| `PRESSURE_RADIUS` | 3.0 m [GT] | Opponents beyond 3m do not meaningfully affect passing accuracy |
| `PRESSURE_SCALAR_MAX` | 2.0 [GT] | 2 opponents at close range saturates pressure; more does not increase further |

**Performance note:** This is the only O(n) operation in Pass Mechanics (§6.1).
Practical n ≤ 5 for 99.9% of evaluations. The spatial hash query cost is isolated
by the `PassMech.PressureQuery` profiling marker (§6.7).

---

### 3.5.7 Error Direction — Deterministic Hash

The error model computes an error *magnitude* (§3.5.3). The *direction* of this
deviation must also be determined deterministically — no random number generator.

```csharp
/// <summary>
/// Computes a deterministic error rotation angle [0, 2π) from simulation state.
/// Same inputs always produce same output — replay-safe.
/// </summary>
public static float ComputeErrorDirection(int agentId, int frameNumber, int passTypeIndex)
{
    // Prime-multiplied XOR hash distributes bits across integer range
    int hashInput = agentId * 73856093
                  ^ frameNumber * 19349663
                  ^ passTypeIndex * 83492791;

    // Map to [0.0, 1.0]
    float normalised = (float)(hashInput & 0x7FFFFFFF) / (float)0x7FFFFFFF;

    // Map to full circle [0, 2π)
    return normalised * Mathf.PI * 2.0f;
}
```

The error is then applied as a rotation of the kick direction by `errorAngleDeg`
around the vertical (Y) axis at the hash-determined angle. See §3.6.7 for the
application step.

**Why is this deterministic?** AgentId, FrameNumber, and PassTypeIndex are all
deterministic simulation values — identical on replay. The XOR-and-multiply hash
uses integer arithmetic only (no floating-point in the hash itself). The final
float conversion is a simple division, reproducible on any platform.

**Why not use the passer's facing direction as the error axis?** If error always
rotated in the same plane as the passer's body, a player could exploit this by
always facing at an angle to the target. The Y-axis rotation distributes error
uniformly in the horizontal plane, which is both more realistic and more
exploit-resistant. See Appendix A.7.2 for full rationale.

---

### 3.5.8 Full Error Computation — Implementation Reference

```csharp
/// <summary>
/// Computes the deterministic error angle for a pass.
/// Called once per pass during INITIATING state. Pure function — no side effects.
/// </summary>
public static float ComputeErrorAngle(
    PassType passType,
    float passing,
    float pressureScalar,
    float fatigue,
    float bodyAngleDeg,
    float urgency,
    bool isWeakFoot,
    int weakFootRating)
{
    // Step 1: Clamp inputs to valid ranges
    float P = Mathf.Clamp(passing, 1.0f, PassConstants.ATTR_MAX);
    float S = Mathf.Clamp01(pressureScalar);
    float F = Mathf.Clamp01(fatigue);
    float α = Mathf.Clamp(bodyAngleDeg, 0f, 90f);
    float U = Mathf.Clamp01(urgency);
    int   R = Mathf.Clamp(weakFootRating, 1, 5);

    // Step 2: Compute individual modifiers
    float passingMod = PassConstants.PASSING_ERROR_MAX
                     - ((P - 1f) / (PassConstants.ATTR_MAX - 1f))
                     * (PassConstants.PASSING_ERROR_MAX - PassConstants.PASSING_ERROR_MIN);

    float pressureMod = 1.0f + S * PassConstants.PRESSURE_WEIGHT;

    float fatigueMod = 1.0f + F * PassConstants.FATIGUE_ACCURACY_REDUCTION;

    float orientMod = 1.0f + (α / 90.0f) * PassConstants.ORIENTATION_MAX_PENALTY;

    float urgencyMod = 1.0f + U * PassConstants.URGENCY_ERROR_SCALE;

    float weakFootMod = 1.0f;
    if (isWeakFoot)
    {
        float penaltyFraction = 1.0f - ((float)(R - 1) / 4.0f);
        weakFootMod = 1.0f + penaltyFraction * PassConstants.WEAK_FOOT_BASE_PENALTY;
    }

    // Step 3: Multiply chain
    float baseError = PassConstants.GetBaseError(passType);
    float rawError = baseError * passingMod * pressureMod * fatigueMod
                   * orientMod * urgencyMod * weakFootMod;

    // Step 4: Clamp to bounds
    float errorAngle = Mathf.Clamp(rawError,
        PassConstants.MIN_ERROR_ANGLE,
        PassConstants.MAX_ERROR_ANGLE);

    return errorAngle;
}
```

> **Note:** This pseudocode is for specification clarity. The WeakFoot modifier is
> included here for completeness; §3.7 provides the authoritative derivation and
> rationale for the WeakFoot penalty model specifically.

---

### 3.5.9 Constants Reference

All constants stored in `PassConstants.cs`.

| Constant | Value | Tag | Source | Notes |
|----------|-------|-----|--------|-------|
| `PASSING_ERROR_MAX` | 2.8 | [GT] | Design decision | Poor passer modifier ceiling |
| `PASSING_ERROR_MIN` | 0.45 | [GT] | Design decision | Elite passer modifier floor |
| `PRESSURE_WEIGHT` | 0.50 | [GT] | [BEILOCK-2007] range | Maximum +50% error under pressure |
| `FATIGUE_ACCURACY_REDUCTION` | 0.20 | [GT] | [ALI-2011] direction | Maximum +20% error at full fatigue |
| `ORIENTATION_MAX_PENALTY` | 1.50 | [GT] | Design decision | Maximum +150% error at 90° misalignment |
| `URGENCY_ERROR_SCALE` | 0.35 | [GT] | Design decision | Maximum +35% error when fully rushed |
| `WEAK_FOOT_BASE_PENALTY` | 0.30 | [GT] | [CAREY-2001] 15–25% range | Maximum +30% error; see OI-App-C-01 |
| `PRESSURE_RADIUS` | 3.0 m | [GT] | Design decision | Spatial hash query radius |
| `PRESSURE_SCALAR_MAX` | 2.0 | [GT] | Design decision | Pressure saturation threshold |
| `MIN_ERROR_ANGLE` | 0.1° | [GT] | Design decision | Absolute floor — no laser passes |
| `MAX_ERROR_ANGLE` | 18.0° | [GT] | Design decision | Absolute ceiling — prevents absurd miss |

#### Per-Type BASE_ERROR Constants

| Pass Type | BASE_ERROR (°) | Tag |
|-----------|---------------|-----|
| Ground | 1.5 | [GT] |
| Driven | 2.0 | [GT] |
| Lofted | 3.0 | [GT] |
| ThroughBall | 2.0 | [GT] |
| AerialThrough | 3.5 | [GT] |
| Cross (Flat) | 2.5 | [GT] |
| Cross (Whipped) | 3.0 | [GT] |
| Cross (High) | 2.5 | [GT] |
| Chip | 2.5 | [GT] |

---

### 3.5.10 Boundary Verification

**Check 1: Elite passer, all neutral — PE-001**
```
ErrorAngle = 1.5° × 0.45 × 1.0 × 1.0 × 1.0 × 1.0 × 1.0 = 0.675°
Clamp to [0.1°, 18.0°]: 0.675° ✓
ELITE_ERROR(Ground) = 1.5° × 0.45 = 0.675° ✓
```

**Check 2: Worst case — all modifiers maximised — PE-002**
```
ErrorAngle = 1.5° × 2.8 × 1.50 × 1.20 × 2.50 × 1.35 × 1.30 = 33.19°
Clamp to [0.1°, 18.0°]: 18.0° ✓ (MAX_ERROR_ANGLE cap fires)
```

**Check 3: Pressure monotonicity — PE-003**
```
S=0.0 → PressureMod=1.0 → Error=2.53° (at Passing=10, Ground)
S=0.5 → PressureMod=1.25 → Error=3.16°
S=1.0 → PressureMod=1.50 → Error=3.80°
Strictly increasing ✓
```

**Check 4: PassingModifier monotone decrease — PE-005**
```
Passing: {1, 5, 10, 15, 20}
Modifiers: {2.800, 2.305, 1.687, 1.068, 0.450}
Strictly decreasing ✓ (verified in Appendix B.3.3)
```

**Check 5: Combined pressure + fatigue (Appendix C.4 cross-check)**
```
Passing=10, Ground, all neutral except:
F=0.5, S=0.5: Error = 1.5° × 1.687 × 1.25 × 1.10 × 1.0 × 1.0 × 1.0 = 3.47°
Appendix C.4 at (F=0.50, S=0.50) = 3.47° ✓
```

---

### 3.5.11 Failure Modes

| FM ID | Trigger | Response | Notes |
|-------|---------|----------|-------|
| FM-04 | ErrorAngle produces NaN | Return 0; log error; do not call ApplyKick() | Should not occur with clamped inputs |
| — | Spatial hash query returns empty | PressureScalar = 0; no failure | Normal — no opponents nearby |
| — | MAX_ERROR_ANGLE clamp fires | Log at DEBUG; not a failure mode | Expected under extreme combined stress |

---

### 3.5.12 Design Decisions and Rationale

**DD-3.5-01: Multiplicative chain (not additive)**

An additive chain would produce linear error scaling — double the modifiers, double
the error. The multiplicative chain produces non-linear compounding — two moderate
penalties combine to more than the sum of their parts. This matches observed
real-world performance collapse under combined stress (a fatigued player under heavy
pressure is disproportionately worse than either factor alone).

**DD-3.5-02: Separate fatigue constants for power and accuracy**

FATIGUE_POWER_REDUCTION (§3.2) and FATIGUE_ACCURACY_REDUCTION (§3.5) are
independent [GT] constants. A player's power loss and accuracy loss from fatigue
need not scale identically. For example, a technically gifted player may maintain
accuracy while losing power — this is captured by the Passing attribute being
independent of fatigue, while KickPower is degraded.

**DD-3.5-03: PassingModifier(10) ≈ 1.687, not 1.0**

See §3.5.4 PassingModifier design note. The "neutral" modifier of 1.0 falls at
approximately Passing ≈ 13, not Passing = 10. This reflects the asymmetric skill
distribution in professional football.

**DD-3.5-04: BodyAngle capped at 90°, not 180°**

Passing at angles > 90° to the body (effectively backwards) is physically possible
but should be prevented by Decision Tree logic. The 90° cap means a player facing
perpendicular to their pass direction receives maximum penalty — facing further away
does not increase it further. This prevents exploit scenarios where an agent facing
away produces unrealistically poor passes.

**DD-3.5-05: Deterministic hash for error direction (no PRNG)**

System.Random and UnityEngine.Random are forbidden (NFR-05, §2.5). The prime-XOR
hash produces a uniform distribution over [0, 2π) that is fully determined by
(AgentId, FrameNumber, PassTypeIndex). See Appendix A.7 for distribution analysis.

---

## 3.6 Target Resolution

### 3.6.1 Responsibilities and Scope

§3.6 resolves the pass target — where the ball should be directed — and produces
the final kick direction vector used by §3.3.6 (velocity Vector3 construction).

Two code paths exist:
- **Player-targeted passes** (PassRequest.TargetType = Agent): Ball is directed at
  the target agent's current position. Lead distance = 0 for stationary targets.
- **Space-targeted passes** (PassRequest.TargetType = Space): Ball is directed at a
  projected position computed from the target agent's velocity. Through-ball and
  cross pass types use this path.

After the aim point is determined, §3.6.7 applies the error vector from §3.5 to
produce the final deviated kick direction.

§3.6 does **not**:
- Select the target agent or position (Decision Tree #8)
- Compute the error magnitude (§3.5)
- Simulate ball flight to verify target reachability (Ball Physics #1)

---

### 3.6.2 Inputs and Outputs

**Inputs:**

| Input | Source | Type | Range | Notes |
|-------|--------|------|-------|-------|
| `TargetType` | `PassRequest.TargetType` | enum | Agent, Space | Determines code path |
| `TargetAgentId` | `PassRequest.TargetAgentId` | int | — | Only used if TargetType = Agent |
| `TargetPosition` | `PassRequest.TargetPosition` | Vector3 | — | Only used if TargetType = Space |
| `PasserPosition` | `AgentState.Position` | Vector3 | — | Launch point |
| `ReceiverPosition` | Target agent's `AgentState.Position` | Vector3 | — | Current position of target agent |
| `ReceiverVelocity` | Target agent's `AgentState.Velocity` | Vector3 | — | For lead distance projection |
| `kickSpeed` | §3.2 output | float (m/s) | — | For time-of-flight estimate |
| `errorAngleDeg` | §3.5 output | float (degrees) | — | Error magnitude |
| `errorDirectionRad` | §3.5.7 output | float (radians) | [0, 2π) | Error rotation angle |

**Outputs:**

| Output | Destination | Type | Notes |
|--------|-------------|------|-------|
| `kickDirection` | §3.3.6 (velocity Vector3 construction) | Vector3 (normalised) | Horizontal unit vector, post-error |
| `aimPoint` | `PassResult.AimPoint` | Vector3 | Pre-error aim point for analytics |
| `leadDistance` | `PassResult.LeadDistance` | float (m) | Zero for player-targeted stationary |

---

### 3.6.3 Player-Targeted Pass Resolution

For player-targeted passes, the aim point is the target agent's current position:

```
aimPoint = ReceiverPosition
kickDirection = normalize(aimPoint - PasserPosition)  // horizontal, Y = 0
leadDistance = 0
```

**Stationary receiver moving slowly:** If the receiver is moving (|ReceiverVelocity| > 0)
but the pass is player-targeted (not space-targeted), §3.6 still aims at the current
position. The receiver is expected to adjust their movement to receive the ball. This
is consistent with real football — a ground pass to a slowly jogging midfielder is
aimed at their feet, not ahead of them.

---

### 3.6.4 Space-Targeted Pass Resolution

For space-targeted passes (through balls, crosses), the Decision Tree may provide
either a `TargetPosition` (explicit space target) or a `TargetAgentId` with
`TargetType = Space` (meaning "play the ball into space ahead of this agent").

**Path A — Explicit space target:**
```
aimPoint = PassRequest.TargetPosition
kickDirection = normalize(aimPoint - PasserPosition)
leadDistance = |aimPoint - ReceiverPosition|
```

**Path B — Agent-based space target (through ball):**
Uses through-ball lead distance calculation (§3.6.5) to project the receiver's
future position and determine the aim point.

---

### 3.6.5 Through-Ball Lead Distance Calculation

For through balls (ThroughBall, AerialThrough), the ball must arrive where the
receiver *will be*, not where they are now. Stage 0 uses a linear receiver
projection assuming constant velocity. Full derivation in Appendix A.3.

```csharp
/// <summary>
/// Computes the through-ball aim point using linear receiver projection.
/// Stage 0 simplification — ignores receiver acceleration, ball deceleration,
/// and non-straight receiver paths. See §7.1 for Stage 1 upgrades.
/// </summary>
public static Vector3 ComputeThroughBallAimPoint(
    Vector3 passerPosition,
    Vector3 receiverPosition,
    Vector3 receiverVelocity,
    float kickSpeed)
{
    // Guard: near-stationary receiver — target current position
    if (receiverVelocity.magnitude < PassConstants.V_THRESHOLD_STATIONARY)
    {
        return receiverPosition;
    }

    // Step 1: Estimate time of flight (simplified — ignores deceleration)
    float D_initial = Vector3.Distance(passerPosition, receiverPosition);
    float t_flight = D_initial / kickSpeed;

    // Step 2: Project receiver position linearly
    Vector3 projectedPosition = receiverPosition + receiverVelocity * t_flight;

    return projectedPosition;
}
```

| Constant | Value | Tag | Notes |
|----------|-------|-----|-------|
| `V_THRESHOLD_STATIONARY` | 0.5 m/s [GT] | Below this, receiver is treated as stationary |

**Lead distance:**
```
leadDistance = |projectedPosition - receiverPosition| = |receiverVelocity| × t_flight
```

**Known limitation (documented in §1.3 KD-4):** Linear projection underestimates
lead distance for long through balls because it ignores ball deceleration during
flight (the ball takes longer to arrive than t_flight predicts, so the receiver
runs further than projected). This creates realistic failure modes — long through
balls under-led slightly — and is explicitly a Stage 1 upgrade point (§7.1).

---

### 3.6.6 Aim Point Pitch Boundary Clamping

After computing the aim point (§3.6.3 or §3.6.4), it is clamped to pitch bounds:

```csharp
aimPoint.x = Mathf.Clamp(aimPoint.x, 0f, PitchConstants.PITCH_LENGTH);
aimPoint.z = Mathf.Clamp(aimPoint.z, 0f, PitchConstants.PITCH_WIDTH);
```

If clamping fires, log a warning:
```
Debug.Log($"[TargetResolver] Aim point clamped to pitch bounds: original={original}, clamped={aimPoint}");
```

This handles edge cases where through-ball projection places the aim point outside
the pitch (corner or sideline through ball). The pass executes to the clamped
position — a realistic outcome where the ball runs out of play.

---

### 3.6.7 Error Vector Application

After the aim point is determined and the kick direction computed, the error
from §3.5 is applied as a horizontal rotation:

```csharp
/// <summary>
/// Applies the deterministic error deviation to the kick direction.
/// Rotates kickDirection by errorAngleDeg around the vertical (Y) axis
/// at the hash-determined error direction angle.
/// </summary>
public static Vector3 ApplyErrorToDirection(
    Vector3 kickDirection,
    float errorAngleDeg,
    float errorDirectionRad)
{
    // Construct rotation: errorAngleDeg around the vertical axis,
    // oriented by errorDirectionRad
    //
    // The error displaces the kick direction by errorAngleDeg in the
    // horizontal plane. errorDirectionRad determines which direction
    // in that plane the displacement goes (left, right, or anywhere
    // in between — uniformly distributed via the §3.5.7 hash).

    // Convert error angle to the specific direction
    float errorX = Mathf.Sin(errorDirectionRad) * errorAngleDeg;
    float errorZ = Mathf.Cos(errorDirectionRad) * errorAngleDeg;

    // Apply as a rotation around the Y axis
    Quaternion errorRotation = Quaternion.Euler(0f, errorX, 0f);
    Vector3 deviatedDirection = errorRotation * kickDirection;

    return deviatedDirection.normalized;
}
```

> **Note:** The exact rotation implementation may vary in final code. The key
> contract is: the kick direction is deviated by `errorAngleDeg` degrees in a
> direction determined by the §3.5.7 hash. The result is always a normalised
> horizontal vector.

---

### 3.6.8 Full Target Resolution — Implementation Reference

The complete target resolution pipeline, called once during INITIATING state:

```
1. Determine aim point:
   - If TargetType == Agent: aimPoint = ReceiverPosition
   - If TargetType == Space AND TargetPosition provided: aimPoint = TargetPosition
   - If TargetType == Space AND through ball: aimPoint = ComputeThroughBallAimPoint(...)

2. Clamp aim point to pitch bounds (§3.6.6)

3. Compute raw kick direction:
   kickDirection = normalize(aimPoint - PasserPosition)

4. Compute error (§3.5):
   errorAngleDeg = ComputeErrorAngle(...)
   errorDirectionRad = ComputeErrorDirection(...)

5. Apply error to direction (§3.6.7):
   finalDirection = ApplyErrorToDirection(kickDirection, errorAngleDeg, errorDirectionRad)

6. Compute lead distance:
   leadDistance = |aimPoint - ReceiverPosition|

7. Construct kick velocity (§3.3.6):
   kickVelocity = ConstructKickVelocity(kickSpeed, finalDirection, launchAngleDeg)
```

---

### 3.6.9 Constants Reference

| Constant | Value | Tag | Source | Notes |
|----------|-------|-----|--------|-------|
| `V_THRESHOLD_STATIONARY` | 0.5 m/s | [GT] | Design decision | Below this, receiver treated as stationary |
| `PITCH_LENGTH` | From PitchConstants | [FIXED] | [MASTER-VOL1] | Standard pitch length (105m) |
| `PITCH_WIDTH` | From PitchConstants | [FIXED] | [MASTER-VOL1] | Standard pitch width (68m) |

---

### 3.6.10 Boundary Verification

**Check 1: Player-targeted, stationary receiver — TR-001**
```
ReceiverPosition = (30, 0, 20), ReceiverVelocity = (0, 0, 0)
PasserPosition = (10, 0, 20)
aimPoint = (30, 0, 20) ✓
kickDirection = normalize((20, 0, 0)) = (1, 0, 0)
leadDistance = 0 ✓
```

**Check 2: Through ball, receiver running at 7 m/s — TR-009**
```
PasserPosition = (20, 0, 34), ReceiverPosition = (50, 0, 34)
ReceiverVelocity = (7, 0, 0)  // running forward at 7 m/s
kickSpeed = 15.0 m/s

D_initial = 30m
t_flight = 30 / 15 = 2.0s
projectedPosition = (50 + 7 × 2.0, 0, 34) = (64, 0, 34)
leadDistance = |14, 0, 0| = 14m ✓
```

**Check 3: Through ball, near-stationary receiver — TR-010**
```
ReceiverVelocity = (0.3, 0, 0)  // below V_THRESHOLD_STATIONARY = 0.5
aimPoint = ReceiverPosition (no lead) ✓
```

**Check 4: Aim point outside pitch — FM-05**
```
projectedPosition = (110, 0, 34)  // beyond PITCH_LENGTH = 105
Clamped to (105, 0, 34) ✓
Warning logged ✓
```

---

### 3.6.11 Failure Modes

| FM ID | Trigger | Response | Notes |
|-------|---------|----------|-------|
| FM-05 | Aim point outside pitch boundary | Clamp to pitch bounds; log warning; proceed | Corner/sideline through ball |
| FM-11 | Target agent not found (TargetAgentId invalid) | Return PassResult(Invalid); log error | Programming error in Decision Tree |
| — | kickSpeed ≈ 0 in through-ball t_flight calculation | t_flight becomes very large; lead is extreme; clamp handles it | FM-07 should prevent zero speed |

---

### 3.6.12 Design Decisions and Rationale

**DD-3.6-01: Linear projection for through balls (Stage 0)**

Non-linear prediction (accounting for receiver acceleration, change of direction,
and ball deceleration) is deferred to Stage 1. Linear projection is the simplest
physically-grounded model: position_future = position_now + velocity × time. The
known underestimation at long range creates realistic failure modes — not all through
balls should succeed — and matches the design philosophy of §1.3 KD-4.

**DD-3.6-02: V_THRESHOLD_STATIONARY = 0.5 m/s**

Below 0.5 m/s, a player is functionally stationary (decelerating from a stop, or
slowly repositioning). Computing a lead distance from 0.3 m/s velocity would produce
a tiny, meaningless lead that adds complexity without improving outcomes. The threshold
is consistent with Agent Movement §3.x deceleration model.

**DD-3.6-03: Error applied to kick direction, not aim point**

Error is angular (degrees of deviation from intended direction), not positional
(metres of displacement at the target). This is physically correct — the foot strikes
the ball at a slightly wrong angle, and the ball then travels in that deviated
direction. The positional miss at the target emerges naturally from
miss_distance = tan(errorAngle) × flightDistance. This approach requires no
knowledge of target distance in the error model itself.

**DD-3.6-04: Pitch boundary clamping is soft, not rejection**

An aim point outside the pitch is not an error — it is a natural consequence of
through-ball projection for a receiver running toward the touchline. The ball is
kicked toward the clamped position; it may go out of play during flight. This is
handled by Ball Physics and the Out-of-Play system, not by Pass Mechanics.

---

## Cross-Specification Dependencies

| Dependency | Owner | Status | Blocks |
|------------|-------|--------|--------|
| `CollisionSystem.SpatialHash.QueryRadius()` signature | Collision System §3.1.4 / XC-4.4-01 | ✅ Defined | None |
| `PRESSURE_RADIUS` ≤ spatial hash cell size | Collision System §3.1.3 / XC-4.4-03 | ⚠ REQUIRED | §3.5.6 |
| `AgentAttributes.Passing` field confirmed | Agent Movement §3.5 | ✅ Confirmed | None |
| `AgentAttributes.WeakFootRating` field confirmed | Agent Movement §3.5 / ERR-007 | ✅ Resolved | None |
| `AgentState.FacingDirection` is Vector2 (2D, Stage 0) | Agent Movement §3.5.3 / XC-4.3-02 | ⚠ REQUIRED | §3.5 BodyAngle calculation |
| PitchConstants (length, width) | Ball Physics §3.1 or shared constants | ✅ Defined | None |

---

## Open Issues

| OI ID | Issue | Severity | Resolution |
|-------|-------|----------|------------|
| OI-3.5-01 | PASSING_ERROR_MAX = 2.8 and PASSING_ERROR_MIN = 0.45 require calibration to produce target elite/poor completion rates | MODERATE | Calibrate during playtesting; see §8.6.5 target ranges |
| OI-3.5-02 | Pressure scalar computation assumes uniform opponent weighting; real pressure varies with opponent facing/intent | LOW | Stage 1 refinement; pressure model upgrade documented in §7 |
| OI-3.6-01 | Through-ball linear projection underestimates lead at long range | LOW (documented limitation) | Stage 1 upgrade; see §7.1, KD-4 |
| OI-3.6-02 | Error application implementation (§3.6.7) uses simplified Euler rotation; may need quaternion for precision | LOW | Verify during implementation; angular error ≤ 18° means small-angle approximation is reasonable |

---

## Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | March 7, 2026, 1:00 PM PST | Claude (AI) / Anton | Initial draft. Multiplicative error chain with 7 modifiers. Pressure scalar from Collision System spatial hash query. Deterministic error direction via prime-XOR hash. Player-targeted and space-targeted resolution paths. Through-ball linear projection. All formulas derived from Appendix A.3, A.5, A.7. Constants from Appendix B assumed values table. |

---

*End of Section 3.5–3.6 — Pass Mechanics Specification #5*

*Next: §3.7–3.9 — Weak Foot Penalty, Execution State Machine, and Event Publishing*
