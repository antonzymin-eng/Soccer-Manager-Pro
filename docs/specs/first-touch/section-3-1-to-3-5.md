# First Touch Mechanics Specification #4 â€” Section 3: Technical Specifications

**Purpose:** Defines the complete technical implementation of all seven First Touch Mechanics
sub-systems: Control Quality Model, Touch Radius Calculation, Ball Displacement, Possession
State Machine, Pressure Evaluation, Body Orientation Detection, and Event Emission. This
section is the authoritative implementation reference for Stage 0.

**Created:** February 17, 2026, 8:00 PM PST
**Version:** 1.2
**Status:** Approved â€” Fixes applied (see changelog)
**Author:** Claude (AI) with Anton (Lead Developer)
**Specification Number:** 4 of 20 (Stage 0 Physics Foundation)
**Prerequisites:** Section 1 (Purpose & Scope) v1.0, Section 2 (System Overview) v1.0

**Changelog:**
- v1.2 (March 05, 2026): Comprehensive audit fixes applied:
  (1) C-02: MOVEMENT_REFERENCE derivation corrected — re-tagged as [GT] gameplay-tuned; false attribution to Agent Movement §3.5.2 removed (7.0 m/s is not defined in Agent Movement; Pace 20 max is 10.2 m/s).
  (2) M-01: §3.7 Event Emission annotated with deferral note per Section 4 v1.1 ERR-004; §3.7.2 Queue Dispatch marked as Stage 1 reference architecture.
  (3) MOD-02: §3.9 (Empirical Tuning) and §3.10 (Section Summary) renumbered to fix reversed ordering.
  (4) MOD-05: Coordinate system origin statement added to §3.3.
  (5) Cross-reference verification table: Collision System entries updated to “✓ Approved”.
- v1.1 (February 17, 2026): Applied 5 post-draft fixes: (1) angular blend magnitude guard Â§3.3.2; (2) INTERCEPTION/DEFLECTION priority documented Â§3.4.2; (3) Z=0 velocity simplification documented Â§3.3.5; (4) empirical tuning notes added Â§3.10; (5) IsGoalkeeper flag contract documented Â§3.8.

---

## 3.1 Control Quality Model

Control quality `q âˆˆ [0.0, 1.0]` is the single most important value in the First Touch
system. Every downstream calculation â€” touch radius, displacement direction, possession
outcome â€” derives from or is constrained by `q`. It must be deterministic: identical inputs
must always produce bitwise-identical output (Master Vol 1 Â§1.3).

### 3.1.1 Governing Formula

The conceptual formula from Master Volume 1 Â§6.4:

```
Control_Quality = Agent_Technique / (Ball_Velocity Ã— Agent_Inertia)
```

This specification expands that into a fully normalised, clamped, and implementable form:

```
// â”€â”€ Step 1: Weighted attribute score â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
WeightedAttr   = (Technique Ã— TECHNIQUE_WEIGHT) + (FirstTouch Ã— FIRST_TOUCH_WEIGHT)
               = (Technique Ã— 0.70) + (FirstTouch Ã— 0.30)
// Range: min = (1Ã—0.70)+(1Ã—0.30) = 1.0 ; max = (20Ã—0.70)+(20Ã—0.30) = 20.0

// â”€â”€ Step 2: Normalise to [0.04, 1.0] â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
NormAttr       = WeightedAttr / ATTR_MAX                   // ATTR_MAX = 20.0
// Range: [0.05, 1.0]

// â”€â”€ Step 3: Orientation bonus (additive before clamping) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
AttrWithBonus  = NormAttr Ã— (1.0 + orientationBonus)
// orientationBonus âˆˆ [0.0, +0.15]; applied by Â§3.6

// â”€â”€ Step 4: Velocity difficulty â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
VelDifficulty  = ball.Speed / VELOCITY_REFERENCE            // VELOCITY_REFERENCE = 15.0 m/s
VelDifficulty  = Clamp(VelDifficulty, 0.1, VELOCITY_MAX_FACTOR)  // VELOCITY_MAX_FACTOR = 4.0
// Slow ball (5 m/s) â†’ 0.33; typical pass (15 m/s) â†’ 1.0; hard shot (30 m/s) â†’ 2.0; cap = 4.0

// â”€â”€ Step 5: Movement difficulty â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
MoveDifficulty = 1.0 + (agent.Speed / MOVEMENT_REFERENCE) Ã— MOVEMENT_PENALTY
// MOVEMENT_REFERENCE = 7.0 m/s [GT]; MOVEMENT_PENALTY = 0.5
// Standing (0 m/s) â†’ 1.0; jogging (3 m/s) â†’ 1.21; sprinting (7 m/s) â†’ 1.50

// â”€â”€ Step 6: Raw quality before pressure â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
RawQuality     = AttrWithBonus / (VelDifficulty Ã— MoveDifficulty)

// â”€â”€ Step 7: Apply pressure degradation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
q              = RawQuality Ã— (1.0 - pressureScalar Ã— PRESSURE_WEIGHT)
// PRESSURE_WEIGHT = 0.40; pressureScalar âˆˆ [0.0, 1.0] from Â§3.5
// Max pressure (1.0) degrades quality by 40%

// â”€â”€ Step 8: Final clamp â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
q              = Clamp(q, 0.0, 1.0)
```

### 3.1.2 Constants

All constants are derived, not arbitrary. Rationale is provided for each.

| Constant | Value | Derivation / Rationale |
|---|---|---|
| `TECHNIQUE_WEIGHT` | 0.70 | Technique is broad ball mastery; primary driver |
| `FIRST_TOUCH_WEIGHT` | 0.30 | First Touch is specialisation; secondary driver |
| `ATTR_MAX` | 20.0 | Maximum attribute value (1â€“20 scale from Agent Movement Â§3.5.6) |
| `VELOCITY_REFERENCE` | 15.0 m/s | Median pass speed in professional football (broadcast observation) |
| `VELOCITY_MAX_FACTOR` | 4.0 | Caps difficulty at 60 m/s equivalent; prevents numerical degeneracy |
| `MOVEMENT_REFERENCE` | 7.0 m/s | [GT] Gameplay-tuned sprint reference; typical average-player sprint speed. Not derived from a single Agent Movement value (Pace 20 max is 10.2 m/s per §3.2). Chosen to scale movement penalty meaningfully across the attribute range |
| `MOVEMENT_PENALTY` | 0.5 | At full sprint, adds 50% difficulty; tuned from sports science literature |
| `PRESSURE_WEIGHT` | 0.40 | Pressure can degrade quality by at most 40%; preserves non-zero floor |

### 3.1.3 Verification Matrix

The following spot-checks must all pass before implementation is accepted:

| Scenario | Tech | FT | Ball m/s | Agent m/s | Pressure | HalfTurn | Expected q |
|---|---|---|---|---|---|---|---|
| Elite, slow ball, no pressure | 20 | 20 | 5.0 | 0.0 | 0.0 | No | â‰¥ 0.85 |
| Elite, slow ball, half-turn | 20 | 20 | 5.0 | 0.0 | 0.0 | Yes | â‰¥ 0.95 (cap 1.0) |
| Average, typical pass, no pressure | 12 | 11 | 15.0 | 2.0 | 0.0 | No | â‰ˆ 0.55â€“0.65 |
| Average, typical pass, medium pressure | 12 | 11 | 15.0 | 2.0 | 0.5 | No | â‰ˆ 0.35â€“0.45 |
| Poor, hard shot, full pressure, sprinting | 5 | 5 | 30.0 | 7.0 | 1.0 | No | â‰¤ 0.15 |
| Minimum possible inputs | 1 | 1 | 60.0 | 7.0 | 1.0 | No | â‰¥ 0.0 (clamp) |
| Maximum possible inputs | 20 | 20 | 1.0 | 0.0 | 0.0 | Yes | â‰¤ 1.0 (clamp) |

### 3.1.4 Implementation Pseudocode

```csharp
/// <summary>
/// Calculates normalised control quality for a first touch event.
///
/// DETERMINISM CONTRACT: No System.Random, no Time.deltaTime, no floating-point
/// non-deterministic operations. All arithmetic uses standard IEEE 754 operations
/// which are deterministic per-platform when compiler settings are fixed.
/// See Master Vol 1 Â§1.3 for determinism requirements.
///
/// CALLED BY: FirstTouchSystem.EvaluateFirstTouch() after pressure and
///            orientation sub-systems have run (Â§3.5, Â§3.6).
/// </summary>
/// <param name="technique">Agent Technique attribute [1â€“20]</param>
/// <param name="firstTouch">Agent First Touch attribute [1â€“20]</param>
/// <param name="ballSpeed">Incoming ball speed in m/s at moment of contact</param>
/// <param name="agentSpeed">Agent movement speed in m/s at moment of contact</param>
/// <param name="pressureScalar">Pressure from nearby opponents [0.0â€“1.0]; output of Â§3.5</param>
/// <param name="orientationBonus">Half-turn bonus [0.0, +0.15]; output of Â§3.6</param>
/// <returns>Control quality q âˆˆ [0.0, 1.0]</returns>
public static float CalculateControlQuality(
    int   technique,
    int   firstTouch,
    float ballSpeed,
    float agentSpeed,
    float pressureScalar,
    float orientationBonus)
{
    // â”€â”€ Step 1: Weighted attribute â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    float weightedAttr = (technique   * FirstTouchConstants.TECHNIQUE_WEIGHT)
                       + (firstTouch  * FirstTouchConstants.FIRST_TOUCH_WEIGHT);

    // â”€â”€ Step 2: Normalise â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    float normAttr = weightedAttr / FirstTouchConstants.ATTR_MAX;

    // â”€â”€ Step 3: Orientation bonus â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // orientationBonus is 0.0 if not in half-turn, 0.15 if half-turn (Â§3.6)
    float attrWithBonus = normAttr * (1.0f + orientationBonus);

    // â”€â”€ Step 4: Velocity difficulty â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    float velDifficulty = ballSpeed / FirstTouchConstants.VELOCITY_REFERENCE;
    velDifficulty = Mathf.Clamp(velDifficulty,
                                0.1f,
                                FirstTouchConstants.VELOCITY_MAX_FACTOR);

    // â”€â”€ Step 5: Movement difficulty â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    float moveDifficulty = 1.0f
        + (agentSpeed / FirstTouchConstants.MOVEMENT_REFERENCE)
        * FirstTouchConstants.MOVEMENT_PENALTY;

    // â”€â”€ Step 6: Raw quality â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    float denominator = velDifficulty * moveDifficulty;
    // denominator cannot be zero: velDifficulty clamped â‰¥ 0.1; moveDifficulty â‰¥ 1.0
    float rawQuality = attrWithBonus / denominator;

    // â”€â”€ Step 7: Pressure degradation â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    float pressurePenalty = pressureScalar * FirstTouchConstants.PRESSURE_WEIGHT;
    float q = rawQuality * (1.0f - pressurePenalty);

    // â”€â”€ Step 8: Final clamp â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    return Mathf.Clamp01(q);
}
```

---

## 3.2 Touch Radius Calculation

Touch radius `r` defines the maximum distance the ball can be displaced from the agent's
control point after the touch. Higher control quality produces a smaller radius (tighter
control). The mapping is piecewise linear within threshold bands, with a velocity modifier
that increases radius for high-speed incoming balls.

### 3.2.1 Threshold Bands

Bands are defined by Master Volume 1 Â§6 (Touch Mechanics). Band boundaries are inclusive
at the lower bound, exclusive at the upper bound.

| Band | Quality Range | Base Radius Min | Base Radius Max | Classification |
|---|---|---|---|---|
| Perfect | q â‰¥ 0.85 | 0.10 m | 0.30 m | Agent maintains immediate possession |
| Good | 0.60 â‰¤ q < 0.85 | 0.30 m | 0.60 m | Minor adjustment required |
| Poor | 0.35 â‰¤ q < 0.60 | 0.60 m | 1.20 m | Ball escapes, recoverable |
| Heavy | q < 0.35 | 1.20 m | 2.00 m | Ball escapes significantly |

### 3.2.2 Piecewise Linear Interpolation

Within each band, radius is linearly interpolated from the band's max radius (at the lower
quality boundary) to the band's min radius (at the upper quality boundary). This guarantees
monotonicity: higher `q` always produces smaller `r`.

```
// Band: Perfect (q âˆˆ [0.85, 1.0])
//   q = 0.85  â†’ r = 0.30m (band maximum)
//   q = 1.00  â†’ r = 0.10m (band minimum)
//   t = (q - 0.85) / (1.00 - 0.85) = (q - 0.85) / 0.15
//   r = Lerp(0.30, 0.10, t)

// Band: Good (q âˆˆ [0.60, 0.85))
//   q = 0.60  â†’ r = 0.60m
//   q = 0.85  â†’ r = 0.30m
//   t = (q - 0.60) / (0.85 - 0.60) = (q - 0.60) / 0.25
//   r = Lerp(0.60, 0.30, t)

// Band: Poor (q âˆˆ [0.35, 0.60))
//   q = 0.35  â†’ r = 1.20m
//   q = 0.60  â†’ r = 0.60m
//   t = (q - 0.35) / (0.60 - 0.35) = (q - 0.35) / 0.25
//   r = Lerp(1.20, 0.60, t)

// Band: Heavy (q âˆˆ [0.0, 0.35))
//   q = 0.00  â†’ r = 2.00m
//   q = 0.35  â†’ r = 1.20m
//   t = q / 0.35
//   r = Lerp(2.00, 1.20, t)
```

**Monotonicity proof:** Within each band, `t` increases as `q` increases, and `Lerp` from a
larger to a smaller value decreases `r`. At band boundaries, the radius is continuous:
band N's minimum equals band N+1's maximum. Therefore `r` is strictly monotonically
decreasing across the full [0.0, 1.0] quality range. âœ“

### 3.2.3 Velocity Modifier

A high-speed incoming ball is harder to deaden. After computing base radius from quality,
apply a multiplicative modifier capped to keep `r` within the 2.0 m absolute maximum:

```
VelocityExcess = Max(0.0, ball.Speed - VELOCITY_REFERENCE)  // Only excess above 15 m/s
VelocityMod    = 1.0 + (VelocityExcess / VELOCITY_REFERENCE) Ã— VELOCITY_RADIUS_FACTOR
// VELOCITY_RADIUS_FACTOR = 0.25
// Ball at 15 m/s â†’ VelocityMod = 1.0 (no change)
// Ball at 30 m/s â†’ VelocityMod = 1.25 (radius 25% larger)
// Ball at 45 m/s â†’ VelocityMod = 1.50 (radius 50% larger)

r_adjusted = r_base Ã— VelocityMod
r          = Min(r_adjusted, MAX_TOUCH_RADIUS)   // Hard cap at 2.0 m
```

| Constant | Value | Rationale |
|---|---|---|
| `VELOCITY_RADIUS_FACTOR` | 0.25 | 25% radius penalty per 15 m/s above reference; tuned for gameplay feel |
| `MAX_TOUCH_RADIUS` | 2.00 m | From Master Vol 1 Â§6; beyond this, possession is lost not degraded |
| `MIN_TOUCH_RADIUS` | 0.10 m | Even elite touches displace the ball fractionally |

### 3.2.4 Implementation Pseudocode

```csharp
/// <summary>
/// Maps control quality to touch radius using piecewise linear interpolation.
///
/// MONOTONICITY CONTRACT: r(q1) < r(q2) whenever q1 > q2, without exception.
/// This is verified by the band structure and proved in Â§3.2.2.
///
/// CALLED BY: FirstTouchSystem.EvaluateFirstTouch() after CalculateControlQuality().
/// </summary>
/// <param name="q">Control quality [0.0â€“1.0] from Â§3.1</param>
/// <param name="ballSpeed">Ball speed in m/s for velocity modifier</param>
/// <returns>Touch radius r âˆˆ [0.10m, 2.0m]</returns>
public static float CalculateTouchRadius(float q, float ballSpeed)
{
    // â”€â”€ Piecewise linear radius from quality â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    float r;

    if (q >= 0.85f)
    {
        // Perfect control band: radius 0.30m â†’ 0.10m
        float t = (q - 0.85f) / 0.15f;
        r = Mathf.Lerp(0.30f, 0.10f, t);
    }
    else if (q >= 0.60f)
    {
        // Good control band: radius 0.60m â†’ 0.30m
        float t = (q - 0.60f) / 0.25f;
        r = Mathf.Lerp(0.60f, 0.30f, t);
    }
    else if (q >= 0.35f)
    {
        // Poor control band: radius 1.20m â†’ 0.60m
        float t = (q - 0.35f) / 0.25f;
        r = Mathf.Lerp(1.20f, 0.60f, t);
    }
    else
    {
        // Heavy touch band: radius 2.00m â†’ 1.20m
        float t = q / 0.35f;
        r = Mathf.Lerp(2.00f, 1.20f, t);
    }

    // â”€â”€ Velocity modifier: high-speed balls harder to deaden â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    float velocityExcess = Mathf.Max(0.0f, ballSpeed - FirstTouchConstants.VELOCITY_REFERENCE);
    float velocityMod    = 1.0f + (velocityExcess / FirstTouchConstants.VELOCITY_REFERENCE)
                                * FirstTouchConstants.VELOCITY_RADIUS_FACTOR;

    r = Mathf.Min(r * velocityMod, FirstTouchConstants.MAX_TOUCH_RADIUS);

    // Note: MIN_TOUCH_RADIUS not enforced here; Lerp from 0.10m handles it.
    // q = 1.0 â†’ t = 1.0 â†’ r = 0.10m (minimum). q cannot exceed 1.0 (clamped in Â§3.1).
    return r;
}
```

---

## 3.3 Ball Displacement

Ball displacement determines where the ball ends up and at what velocity after the touch.
The agent has an intended direction (from their movement command), and control quality
determines how accurately that intent is realised. Poor quality introduces angular error â€”
the ball deviates toward the original incoming trajectory.

> **Coordinate System (MOD-05):** All position and velocity vectors in this section
> use the project-wide coordinate system defined in Ball Physics #1 §3.1.1: XY = pitch
> plane, Z = vertical (up), **corner origin** (0,0 at one corner of the pitch). Ball
> displacement calculations produce positions in this coordinate frame. No coordinate
> transforms are required between First Touch and Ball Physics.


### 3.3.1 Intended Direction Computation

The agent's intended touch direction derives from their movement command target, not their
facing direction. This allows for directional first touches (playing it into space).

```
// Agent's intended touch direction (normalised, XY plane only)
TargetOffset      = MovementCommand.TargetPosition - agent.Position   // 3D vector
IntendedDir       = Normalise(Vector2(TargetOffset.x, TargetOffset.y))

// Fallback: if agent has no movement command, use facing direction
if (MovementCommand.TargetPosition == Vector3.zero)
    IntendedDir = agent.FacingDirection
```

### 3.3.2 Angular Error Model

Control quality determines the maximum angular deviation from intended direction. The
deviation is not random â€” it is deterministic, computed from the mismatch between the ball's
incoming direction and the agent's intended direction, weighted by (1 - q).

```
// Incoming ball direction (XY plane)
IncomingDir   = Normalise(Vector2(-ball.Velocity.x, -ball.Velocity.y))
// Negated: the direction the ball came FROM, not where it is going

// Angular error: how far the ball's actual path deviates from intended
// At q = 1.0: no error, ball goes exactly where intended
// At q = 0.0: ball goes entirely along incoming direction (no control)
ErrorWeight   = 1.0 - q
blended       = IntendedDir Ã— q + IncomingDir Ã— ErrorWeight

// SAFETY: When IntendedDir and IncomingDir are nearly opposite (e.g. agent
// trying to play the ball back the way it came at q â‰ˆ 0.5), the linear blend
// can produce a near-zero vector. Normalising a zero vector is undefined.
// Fallback to IncomingDir â€” ball follows original path, which is the correct
// heavy-touch behaviour in this degenerate case.
if (blended.magnitude < BLEND_MIN_MAGNITUDE)    // BLEND_MIN_MAGNITUDE = 0.001f
    blended = IncomingDir

ActualDir     = Normalise(blended)
// Note: linear blend then normalise approximates great-circle interpolation for small angles
// Full SLERP is not warranted for this gameplay-level calculation
```

**Design rationale:** Using the incoming direction as the "error attractor" is physically
motivated â€” a poorly executed touch deflects the ball further along its original path rather
than in a random direction. This makes heavy touches predictable and defensible, rewarding
tactical positioning by opponents.

### 3.3.3 Displacement Distance

Within the touch radius `r`, the actual displacement distance is further modulated by the
agent's intent. A deliberate touch into space may use the full radius. A cushioning touch
aims for the minimum.

```
// For Stage 0: displacement distance = r (full radius, maximum displacement within band)
// Rationale: Stage 0 has no "intent weight" distinction; all touches use full band radius
// Stage 1 will introduce touch weight (cushion vs. firm vs. driven) via animation system

DisplacementDist = r    // Constrained âˆˆ [0.10m, 2.0m]
```

### 3.3.4 New Ball Position

```
// 2.5D: displacement computed in XY plane; Z = ball radius (ground contact)
DisplacementXY   = ActualDir Ã— DisplacementDist
newBallPosition  = Vector3(
    agent.Position.x + DisplacementXY.x,
    agent.Position.y + DisplacementXY.y,
    BallPhysicsConstants.Ball.RADIUS          // Â§3.1.2: 0.11m; ball rests on ground
)

// Pitch boundary enforcement
newBallPosition.x = Clamp(newBallPosition.x, 0.0, PITCH_LENGTH)   // 105.0m
newBallPosition.y = Clamp(newBallPosition.y, 0.0, PITCH_WIDTH)    // 68.0m
// Note: boundary clamping may slightly violate the radius constraint in edge cases
// at pitch margins. This is an acceptable Stage 0 simplification.
```

### 3.3.5 New Ball Velocity

After the touch, the ball receives a velocity derived from the agent's movement and the
degree of control achieved. A controlled touch redirects ball momentum; a heavy touch
partially preserves the original momentum.

```
// Agent's contribution: controlled dribble velocity in touch direction
AgentContrib     = ActualDir Ã— Min(agent.Speed, DRIBBLE_MAX_SPEED)
// DRIBBLE_MAX_SPEED = 5.5 m/s; ball speed at feet is less than sprint speed

// Ball's retained momentum: heavy touches preserve more incoming momentum
// At q = 1.0: no original momentum retained (full control)
// At q = 0.0: original momentum fully retained (deflection)
BallRetained     = ball.Velocity Ã— (1.0 - q) Ã— MOMENTUM_RETENTION
// MOMENTUM_RETENTION = 0.5; physical damping from body contact

// Combined new velocity (XY plane only; Z clamped to 0 on ground touch)
newBallVelocity  = Vector3(
    (AgentContrib.x Ã— q) + (BallRetained.x),
    (AgentContrib.y Ã— q) + (BallRetained.y),
    0.0f     // Ground touch kills vertical component; Stage 1 adds loft for chest/thigh
)
// STAGE 0 SIMPLIFICATION: Setting Z to 0.0 discards any residual vertical
// velocity from the incoming ball. This is correct for the common case (ball
// arriving at a shallow angle), but produces a slightly flat result for balls
// bouncing steeply upward at contact (ball.Velocity.z > 0 at contact moment).
// The height guard (Â§2.2) limits this to ball.z â‰¤ 0.5m, so the error is
// bounded. Stage 1 loft modelling (chest/thigh contacts) addresses this fully.

// Speed cap: ball cannot exceed a physically realistic maximum after a touch
float newSpeed   = newBallVelocity.magnitude
if (newSpeed > TOUCH_MAX_BALL_SPEED)
    newBallVelocity = newBallVelocity.normalized Ã— TOUCH_MAX_BALL_SPEED
// TOUCH_MAX_BALL_SPEED = 12.0 m/s; touches do not accelerate the ball as a kick would
```

| Constant | Value | Rationale |
|---|---|---|
| `DRIBBLE_MAX_SPEED` | 5.5 m/s | Ball at feet cannot exceed jogging speed of elite player |
| `MOMENTUM_RETENTION` | 0.5 | 50% of original ball momentum retained; physical body damping |
| `TOUCH_MAX_BALL_SPEED` | 12.0 m/s | Touch cannot produce kick-level ball speed |
| `BLEND_MIN_MAGNITUDE` | 0.001 | Minimum magnitude of direction blend before fallback to IncomingDir |

---

## 3.4 Possession State Machine

The Possession State Machine determines the categorical outcome of the first touch:
CONTROLLED, LOOSE_BALL, DEFLECTION, or INTERCEPTION. The outcome drives what happens
to the game state, possession tracking, and which systems receive the result.

### 3.4.1 Outcome Definitions (from Â§2.4)

| Outcome | Definition | Dribbling State | Possession Transfer |
|---|---|---|---|
| `CONTROLLED` | Agent gains possession; ball within dribble range | Activated | To touching agent |
| `LOOSE_BALL` | Ball displaced; no agent has possession | Not activated | Cleared (contested) |
| `DEFLECTION` | Ball bounces away; momentum substantially preserved | Not activated | Cleared |
| `INTERCEPTION` | Nearby opponent can claim the ball next frame | Not activated | Pending (next frame) |

### 3.4.2 Determination Logic

Outcomes are determined by a priority-ordered evaluation. The first matching condition wins.

```
// Priority 1: INTERCEPTION
// Condition: Heavy touch (r â‰¥ 1.2m) AND an opponent is within interception range
opponentInRange = SpatialQuery(newBallPosition, INTERCEPTION_RADIUS)
                  .Where(a => a.TeamID != agent.TeamID)
                  .Any()

if (r >= INTERCEPTION_THRESHOLD && opponentInRange)
    outcome = INTERCEPTION
    // Per Critical Issue #4 resolution: intercepting opponent evaluates THEIR
    // first touch in the NEXT frame. This prevents same-frame recursion.

// Priority 2: DEFLECTION
// Condition: Very heavy touch where ball retains most of original momentum
// Measured by alignment of newBallVelocity with original ball.Velocity
momentumAlignment = Dot(Normalise(newBallVelocity.xy), Normalise(ball.Velocity.xy))

else if (r >= DEFLECTION_THRESHOLD && momentumAlignment >= DEFLECTION_ALIGNMENT_MIN)
    outcome = DEFLECTION

// Priority 3: LOOSE_BALL
// Condition: Heavy touch but no interception, no deflection
else if (r >= LOOSE_BALL_THRESHOLD)
    outcome = LOOSE_BALL

// Priority 4: CONTROLLED (default for all good touches)
else
    outcome = CONTROLLED
```

**Priority note â€” INTERCEPTION supersedes DEFLECTION by design:** A ball at r â‰¥ 1.50m that
is aligned with the incoming direction AND has an opponent in range will be classified as
INTERCEPTION, not DEFLECTION. This is intentional and physically correct: a deflection that
travels directly toward an opponent *is* an interception. DEFLECTION is only reached when no
opponent is in position to exploit the heavy touch. Implementers should not treat the
unreachable DEFLECTION branch in this scenario as a bug.

| Constant | Value | Rationale |
|---|---|---|
| `INTERCEPTION_THRESHOLD` | 1.20 m | Corresponds to bottom of Poor band; ball genuinely escapes |
| `INTERCEPTION_RADIUS` | 2.50 m | Opponent must be close enough to realistically reach the ball |
| `DEFLECTION_THRESHOLD` | 1.50 m | Subset of heavy touches where momentum is substantially preserved |
| `DEFLECTION_ALIGNMENT_MIN` | 0.70 | Cos(45Â°) â‰ˆ 0.71; ball direction within 45Â° of incoming = deflection |
| `LOOSE_BALL_THRESHOLD` | 0.60 m | Bottom of Good band; ball needs recovery touch to control |

### 3.4.3 State Transition Diagram

```
Contact detected by Collision System
              â”‚
              â–¼
     [Height Guard: Â§2.2]
     ball.z > 0.5m? â†’ Heading Mechanics (Spec #10)
              â”‚ No
              â–¼
     [Control Quality: Â§3.1]
     q âˆˆ [0.0, 1.0]
              â”‚
              â–¼
     [Touch Radius: Â§3.2]
     r âˆˆ [0.10m, 2.0m]
              â”‚
        â”Œâ”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
        â”‚                                â”‚
   r < 0.60m                        r â‰¥ 0.60m
        â”‚                                â”‚
        â–¼                          â”Œâ”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
   CONTROLLED                      â”‚                    â”‚
   (possession gained)         r < 1.20m            r â‰¥ 1.20m
                                   â”‚                    â”‚
                                   â–¼              â”Œâ”€â”€â”€â”€â”€â”´â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
                               LOOSE_BALL         â”‚                â”‚
                                           opponent  in range?  No â†’
                                               Yes               DEFLECTION
                                               â”‚                 (if aligned)
                                               â–¼                     â”‚
                                         INTERCEPTION                â–¼
                                         (next frame)          LOOSE_BALL
```

### 3.4.4 CONTROLLED State: Dribbling Activation

When outcome is CONTROLLED, the system must activate dribbling modifiers in Agent Movement:

```csharp
// Signal Agent Movement to apply dribbling locomotion penalties
// See Agent Movement Spec Â§6.1.2 for DribblingModifier definition
agentMovementSystem.SetDribblingState(agent.AgentID, isDribbling: true);

// Dribbling state persists until one of:
//   a) Agent makes a pass or shot (Pass Mechanics #5 or Shooting Mechanics #6)
//   b) Agent loses possession (LOOSE_BALL, DEFLECTION, INTERCEPTION on subsequent touch)
//   c) Ball rolls beyond DRIBBLE_DETACH_RADIUS from agent position
// DRIBBLE_DETACH_RADIUS = 1.50m; if ball drifts this far, possession cleared automatically
```

### 3.4.5 INTERCEPTION State: Next-Frame Chain

Per the Critical Issue #4 resolution (confirmed Â§1.0 of this spec):

```
Frame N:
  Receiving agent evaluates first touch â†’ r â‰¥ 1.20m AND opponent within 2.50m
  Outcome = INTERCEPTION
  Ball velocity set toward intercepting opponent (not zero)
  No possession assigned

Frame N+1:
  Collision System detects intercepting agent contacts the ball
  First Touch System evaluates intercepting agent's touch normally
  Intercepting agent may achieve CONTROLLED, LOOSE_BALL, or DEFLECTION
```

This prevents infinite recursion and maintains frame-by-frame determinism
(Master Vol 1 Â§1.3).

---

## 3.5 Pressure Evaluation

Pressure evaluation computes `pressureScalar âˆˆ [0.0, 1.0]` representing the aggregate
difficulty imposed by nearby opponents at the moment of contact. This is consumed by the
Control Quality Model (Â§3.1, Step 7).

### 3.5.1 Spatial Query

Pressure is based on the positions of all opposing-team agents within the pressure radius
at the moment of contact. The Collision System's spatial hash provides this query.

```csharp
// Query all agents within PRESSURE_RADIUS of the receiving agent
// Uses Collision System Â§3.1.4 SpatialHashGrid.Query() API
List<AgentPhysicalProperties> nearbyOpponents =
    spatialHash.Query(agent.Position, PRESSURE_RADIUS)
               .Where(a => a.TeamID != agent.TeamID)
               .ToList();
// PRESSURE_RADIUS = 3.0m; empirically tuned; represents "clearly in the zone"
```

### 3.5.2 Per-Opponent Contribution

Each opponent within the pressure radius contributes a pressure value based on their
proximity. Closer opponents contribute more pressure via inverse-square falloff. This is
physically motivated: an opponent at 0.5m is far more disruptive than one at 2.5m.

```
// For each opponent i within PRESSURE_RADIUS:
distance_i      = |agent.Position_XY - opponent_i.Position_XY|
distance_i      = Max(distance_i, MIN_PRESSURE_DISTANCE)
// MIN_PRESSURE_DISTANCE = 0.3m; prevents division-by-zero; closer than this = same value

// Inverse-square falloff, normalised so that distance = MIN_PRESSURE_DISTANCE â†’ 1.0
rawContrib_i    = (MIN_PRESSURE_DISTANCE / distance_i)Â²
// distance = 0.30m â†’ rawContrib = 1.00 (maximum)
// distance = 0.60m â†’ rawContrib = 0.25
// distance = 1.00m â†’ rawContrib = 0.09
// distance = 2.00m â†’ rawContrib = 0.02
// distance = 3.00m â†’ rawContrib = 0.01
```

### 3.5.3 Aggregation and Normalisation

Multiple opponents stack their contributions, but the total is clamped to prevent
physically unrealistic super-pressure from distant players.

```
// Sum contributions from all opponents
rawPressure     = Î£ rawContrib_i   for all i in nearbyOpponents

// Normalise: map [0, PRESSURE_SATURATION] â†’ [0.0, 1.0]
// PRESSURE_SATURATION = 1.5
// Rationale: a single opponent at 0.3m (contrib = 1.0) already constitutes significant
// pressure. Two mid-range opponents (0.5 each) saturate at 1.0. Three or more distant
// opponents should not produce super-pressure beyond the scale.
pressureScalar  = Clamp(rawPressure / PRESSURE_SATURATION, 0.0, 1.0)
```

### 3.5.4 Failure Mode: Null Spatial Query

If the spatial query API returns null (Collision System not ready, frame startup edge case):

```csharp
if (nearbyOpponents == null)
{
    // FM-04 from Â§2.6: Zero-pressure fallback
    // Underestimating pressure (conservative) is safer than overestimating it.
    // A false LOOSE_BALL from phantom pressure is more disruptive than CONTROLLED
    // with zero-pressure assumption.
    pressureScalar = 0.0f;
    DiagnosticsCounter.Increment(DiagnosticsCounter.FM04_ZeroPressureCount);
    Debug.LogWarning($"[FirstTouch] SpatialQuery null at frame {frameNumber}, " +
                     $"agent {agent.AgentID}. Using zero pressure.");
}
```

| Constant | Value | Rationale |
|---|---|---|
| `PRESSURE_RADIUS` | 3.0 m | Empirically tuned; beyond 3m, opponent is not immediately threatening |
| `MIN_PRESSURE_DISTANCE` | 0.3 m | Prevents division-by-zero; hitbox overlap prevents < 0.3m in practice |
| `PRESSURE_SATURATION` | 1.5 | Raw pressure above this = full degradation; prevents unbounded stacking |

### 3.5.5 Worked Example

Scenario: Receiving agent at (52.0, 34.0). Two opponents:
- Opponent A at (51.5, 33.5) â†’ distance = âˆš(0.25 + 0.25) â‰ˆ 0.71m â†’ contrib = (0.3/0.71)Â² = 0.179
- Opponent B at (53.5, 35.0) â†’ distance = âˆš(2.25 + 1.00) â‰ˆ 1.80m â†’ contrib = (0.3/1.80)Â² = 0.028

```
rawPressure    = 0.179 + 0.028 = 0.207
pressureScalar = Clamp(0.207 / 1.5, 0.0, 1.0) = 0.138
```

This represents mild-to-moderate pressure: quality degraded by ~5.5%
(0.138 Ã— PRESSURE_WEIGHT 0.40 = 0.055).

---

