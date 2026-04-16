# Agent Movement Specification â€” Section 3.4: Turning & Momentum

**Purpose:** Defines the speed-dependent turn rate model, minimum turn radius derivation, stumble risk mechanics for sharp turns at speed, lean angle output for the animation data contract, and integration with the facing direction system (Section 3.3) and movement state machine (Section 3.1). This section is the authoritative source for all angular velocity constraints on agent movement and facing direction rotation.

**Created:** February 10, 2026, 10:15 AM PST  
**Updated:** March 4, 2026, 12:00 AM PST  
**Version:** 1.1  
**Status:** In Review  
**Dependencies:** Section 3.1 (State Machine), Section 3.2 (Locomotion & PerformanceContext), Section 3.3 (Directional Movement & Facing Direction)

---

## v1.1 Changelog (March 4, 2026)

**MODERATE FIXES:**

1. **Duplicate "Facing rate multiplier" future extension removed:** Two entries described the same Stage 1 extension with slightly different range suggestions (1.2-1.5x vs 1.3-1.5x). Consolidated into single entry with range 1.2-1.5x. Lean cap saturation note preserved from the removed duplicate.

2. **MAX_LEAN_ANGLE declared authoritative at 40.0 degrees:** Section 3.5 MovementConstants had 45f. This section (Section 3.4) is authoritative for all turning/lean calculations. Section 3.5 must be updated to match.

---

## Table of Contents

- [3.4.1 Design Philosophy](#341-design-philosophy)
- [3.4.2 Turn Rate Model](#342-turn-rate-model)
- [3.4.3 Minimum Turn Radius](#343-minimum-turn-radius)
- [3.4.4 Stumble Risk Mechanics](#344-stumble-risk-mechanics)
- [3.4.5 Lean Angle Output](#345-lean-angle-output)
- [3.4.6 Integration with State Machine & Facing System](#346-integration-with-state-machine--facing-system)
- [3.4.7 Numerical Examples & Validation](#347-numerical-examples--validation)

---

## 3.4.1 Design Philosophy

Football players cannot change direction instantaneously. At walking pace, turning is nearly free â€” a player repositioning in the box can spin on the spot without penalty. At sprint speed, direction changes require committing body weight, planting a foot, and accepting the biomechanical consequences: wider arcs, forced deceleration, and risk of losing balance.

This section models these constraints through two complementary mechanisms:

- **Turn rate** (Â°/s): The angular velocity limit on both path direction changes and facing direction rotation. This is the primary constraint and the value consumed by Section 3.3's facing direction system.
- **Stumble risk**: A consequence model that applies when agents attempt turns near or beyond their safe threshold at speed. This feeds Section 3.1's STUMBLING state.

**Formula derivation note:** FR-5 specifies the turn radius formula as `r_min = vÂ² / (a_lateral_max Ã— agility_factor)`. This section inverts the approach: turn *rate* (Â°/s) is the primary model, and turn radius is *derived* from it (`r_min = v / Ï‰_rad`). The two approaches are mathematically equivalent â€” both produce speed-dependent radius scaled by Agility â€” but the turn-rate-first approach is used here because Section 3.3's facing direction system requires an angular velocity limit, not a radius constraint. Computing turn rate from radius would require an extra conversion step every frame with no benefit. The derived radius values in Section 3.4.3 satisfy the same spatial constraints FR-5 intended.

**Key design decisions:**

- **Turn rate is a continuous function of speed**, not a per-state constant. The state labels in Section 3.1.6 (Free, Moderate, Tight, Limited, None) describe the *result* of applying the speed-dependent formula at typical speeds for each state, not additional multipliers. This prevents discontinuities at state transitions.
- **Single turn rate governs both path and facing rotation.** Section 3.3 established that `UpdateFacingDirection()` rotates at the turn rate defined here. The same rate constrains movement vector rotation. An agent cannot look somewhere faster than they can steer there â€” both are limited by the same biomechanical capability (hip rotation, foot planting, body reorientation). **Known limitation:** In reality, head/torso rotation is faster than lower-body redirection. This simplification may create edge cases in TARGET_LOCK mode where an agent sprinting at top speed cannot visually track a fast-moving ball quickly enough. If playtesting in Stage 1 reveals this as a problem, a separate `facingRateMultiplier` (suggested range: 1.2â€“1.5Ã—) can be applied exclusively to facing rotation without altering path curvature constraints. This would be a modifier on the facing system's consumption of Ï‰_max, not a change to this section's core formula. See Future Extension Notes.
- **Two-tier safety model** (safe zone + risk zone). Below the safe turn rate, no consequences. Between safe and maximum, stumble probability scales linearly. This creates risk/reward decisions: a defender can attempt a sharp cut to track an attacker, but risks stumbling if their Balance is low.
- **Lean angle is a pure data output.** Computed from centripetal acceleration for the animation data contract (FR-8). Not rendered in Stage 0, no gameplay effect â€” but the calculation is present from day one so Stage 1 animation can consume it immediately.
- **Agility is primary, Balance is secondary** (FR-5). Agility determines the raw turn rate. Balance determines how much of that rate is safe (the width of the risk-free zone) and the stumble probability when pushing the limits.

### Relationship to FR-5 Turn Radius Formula

FR-5 (Sections 1-2) specifies: `r_min = vÂ² / (a_lateral_max Ã— agility_factor)`. This section implements a **turn-rate-first** model rather than a radius-first model. The turn radius is *derived* from the turn rate (Section 3.4.3), not computed independently.

**Rationale for divergence:** Section 3.3 established that `UpdateFacingDirection()` needs an angular velocity (Â°/s) to constrain rotation per frame. A radius-first model would require converting radius back to angular velocity every frame (`Ï‰ = v / r`), adding an unnecessary indirection. The turn-rate-first approach provides the angular velocity directly and derives radius when needed (for AI path planning, spatial reasoning).

**Equivalence:** Both approaches produce the same behavioral result â€” turn radius that increases with speed and decreases with Agility. The FR-5 formula `r_min = vÂ² / (a_lateral_max Ã— agility_factor)` is mathematically equivalent to `r = v / Ï‰_max` when `a_lateral_max Ã— agility_factor = v Ã— Ï‰_max`. The turn rate model simply parameterizes the same constraint through angular velocity rather than lateral acceleration.

---

## 3.4.2 Turn Rate Model

### Core Formula

Turn rate decreases as speed increases, following an inverse relationship. This produces the natural behavior where slow-moving agents turn freely and fast-moving agents are committed to wider arcs.

```csharp
/// <summary>
/// Turn rate constants.
///
/// The turn rate model uses the formula:
///   Ï‰_max = (TURN_RATE_BASE / (1 + k_turn Ã— v)) Ã— balance_modifier Ã— state_modifier
///
/// Where:
///   Ï‰_max    = maximum turn rate in degrees/second
///   v        = current speed in m/s
///   k_turn   = speed sensitivity coefficient, mapped from Agility
///   balance  = Balance modifier (0.85â€“1.0), secondary contribution
///   state    = state-specific modifier (1.0 for most states, reduced for DECELERATING)
///
/// The inverse model (1/(1+kv)) was chosen over linear or quadratic reduction because:
///   1. It produces a natural asymptotic curve â€” rate drops sharply at first then
///      levels off, matching how biomechanical turning constraints scale with speed
///   2. At v=0, the formula returns exactly TURN_RATE_BASE (no special case needed)
///   3. The formula never reaches zero â€” there is always some residual turning
///      capability, even at extreme speeds (clamped below by the state modifier)
///   4. The single parameter k_turn controls the entire shape, making Agility
///      scaling clean and predictable
///
/// Alternative considered: Per-state constant rates (e.g., JOGGING = 250Â°/s,
/// SPRINTING = 120Â°/s). Rejected because it creates discontinuities at state
/// transitions â€” an agent at 5.7 m/s (JOGGING) would have a dramatically
/// different turn rate from one at 5.9 m/s (SPRINTING). The continuous model
/// produces smooth behavior across all speeds.
/// </summary>
public static class TurnConstants
{
    // ================================================================
    // TURN RATE BASE
    // ================================================================

    /// <summary>
    /// Maximum turn rate at zero speed (degrees/second).
    /// 720Â°/s = 2 full rotations per second.
    /// At walking pace and below, this is effectively unrestricted â€” an agent
    /// can reorient in any direction within 2â€“3 frames (33â€“50ms).
    /// Matches Section 3.1.6 "Free" label for IDLE and WALKING states.
    /// </summary>
    public const float TURN_RATE_BASE = 720.0f;

    // ================================================================
    // SPEED SENSITIVITY (k_turn) â€” Agility-mapped
    // ================================================================

    /// <summary>
    /// Speed sensitivity at Agility 1 (stiffest turning).
    /// Higher k_turn = turn rate drops faster with speed.
    ///
    /// Produces at sprint speed (9 m/s):
    ///   Ï‰ = 720 / (1 + 0.78 Ã— 9) = 720 / 8.02 = 89.8Â°/s
    /// At jog speed (4 m/s):
    ///   Ï‰ = 720 / (1 + 0.78 Ã— 4) = 720 / 4.12 = 174.8Â°/s
    ///
    /// Validation: An Agility 1 agent at sprint needs ~1.0 second to turn 90Â°.
    /// This models a stiff, physically limited player who must decelerate
    /// significantly before changing direction â€” think of a tall, rangy
    /// centre-back caught in transition.
    /// </summary>
    public const float K_TURN_MAX = 0.78f;

    /// <summary>
    /// Speed sensitivity at Agility 20 (nimblest turning).
    ///
    /// Produces at sprint speed (9 m/s):
    ///   Ï‰ = 720 / (1 + 0.35 Ã— 9) = 720 / 4.15 = 173.5Â°/s
    /// At jog speed (4 m/s):
    ///   Ï‰ = 720 / (1 + 0.35 Ã— 4) = 720 / 2.40 = 300.0Â°/s
    ///
    /// Validation: An Agility 20 agent at sprint can turn 90Â° in ~0.52 seconds.
    /// This models an elite dribbler â€” someone like Messi or Hazard who
    /// changes direction at speed with minimal arc.
    ///
    /// Conservative calibration note: This value was raised from an initial
    /// 0.31 to 0.35 to keep peak centripetal acceleration at the elite end
    /// below 2.8g (comfortably within the 1.5â€“3.0g documented range for
    /// maximal change-of-direction tasks in team sport athletes). The 0.31
    /// value produced 3.04g at sprint â€” technically within human capability
    /// but at the absolute ceiling, leaving no headroom for future modifiers
    /// (dribbling, surface effects) that might push values higher.
    /// </summary>
    public const float K_TURN_MIN = 0.35f;

    /// <summary>
    /// Per-attribute-point reduction in k_turn.
    /// (0.78 - 0.35) / 19 = 0.02263 per Agility point.
    /// Linear mapping: higher Agility â†’ lower k_turn â†’ less speed penalty.
    /// </summary>
    public const float K_TURN_PER_POINT = (K_TURN_MAX - K_TURN_MIN) / 19.0f;

    // ================================================================
    // BALANCE MODIFIER â€” Secondary contribution
    // ================================================================

    /// <summary>
    /// Balance modifier at Balance 1 (worst stability).
    /// Reduces effective turn rate by 15% compared to Balance 20.
    ///
    /// Rationale: Balance affects body control and weight distribution during
    /// direction changes. A low-Balance player can attempt the same turn as
    /// a high-Balance player (same Agility) but executes it 15% slower
    /// because their body mechanics are less controlled.
    ///
    /// 15% chosen because Balance is the secondary attribute â€” Agility
    /// provides the primary 1.93Ã— range (89.8 to 173.5 at sprint),
    /// while Balance adds a 1.18Ã— range on top. Combined, the total
    /// turn rate range at sprint is:
    ///   Worst (Agi 1, Bal 1):  89.8 Ã— 0.85 = 76.3Â°/s
    ///   Best (Agi 20, Bal 20): 173.5 Ã— 1.0 = 173.5Â°/s
    ///   Ratio: 2.27Ã— â€” meaningful differentiation without cartoonish extremes.
    /// </summary>
    public const float BALANCE_MOD_MIN = 0.85f;

    /// <summary>
    /// Balance modifier at Balance 20 (best stability).
    /// 1.0 = no penalty. Balance 20 represents optimal body control.
    /// </summary>
    public const float BALANCE_MOD_MAX = 1.0f;

    /// <summary>
    /// Per-attribute-point Balance modifier increase.
    /// (1.0 - 0.85) / 19 = 0.007895 per Balance point.
    /// </summary>
    public const float BALANCE_MOD_PER_POINT =
        (BALANCE_MOD_MAX - BALANCE_MOD_MIN) / 19.0f;

    // ================================================================
    // STATE-SPECIFIC MODIFIERS
    // ================================================================

    /// <summary>
    /// Turn rate modifier during DECELERATING state.
    /// 0.60Ã— â€” braking reduces turning control because the agent is
    /// channeling force into longitudinal deceleration, not lateral
    /// redirection. Planting a foot to brake and planting to turn
    /// are competing actions.
    ///
    /// Produces at sprint entry (5.8 m/s), Agility 12, Balance 12:
    ///   Base Ï‰ = 720 / (1 + 0.531 Ã— 5.8) Ã— 0.937 = 720 / 4.080 Ã— 0.937 = 165.3Â°/s
    ///   Decel Ï‰ = 165.3 Ã— 0.60 = 99.2Â°/s â†’ "Limited" per Section 3.1.6 âœ“
    ///
    /// Note: As the agent decelerates and speed drops, the base turn rate
    /// increases (inverse model), so the DECEL modifier's absolute effect
    /// lessens. By the time the agent slows to walking speed, the 0.6Ã—
    /// penalty still leaves Ï‰ well above 300Â°/s â€” effectively unrestricted.
    /// This models the natural recovery of control as speed decreases.
    /// </summary>
    public const float DECEL_TURN_MODIFIER = 0.60f;

    /// <summary>
    /// Turn rate modifier during STUMBLING state.
    /// 0.0Ã— â€” no voluntary turning. The agent's direction is entirely
    /// determined by residual momentum. Matches Section 3.1.6 "None" label.
    /// </summary>
    public const float STUMBLE_TURN_MODIFIER = 0.0f;

    /// <summary>
    /// Turn rate modifier during GROUNDED state.
    /// 0.0Ã— â€” no movement at all, no turning. Matches Section 3.1.6 "None" label.
    /// </summary>
    public const float GROUNDED_TURN_MODIFIER = 0.0f;

    // ================================================================
    // ABSOLUTE CLAMPS
    // ================================================================

    /// <summary>
    /// Minimum turn rate floor (degrees/second).
    /// Even at theoretical extreme speeds, the agent retains some
    /// (minimal) ability to steer. Prevents division edge cases and
    /// ensures agents are never perfectly locked on a straight line.
    ///
    /// 15Â°/s = 6 seconds for a quarter-turn at absolute max speed.
    /// In practice this floor is never reached in normal gameplay
    /// because MAX_SPEED (12.0 m/s) produces:
    ///   Worst case: 720 / (1 + 0.78 Ã— 12) Ã— 0.85 = 59.1Â°/s > 15.
    /// The floor exists only as a safety net for numerical edge cases.
    /// </summary>
    public const float TURN_RATE_FLOOR = 15.0f;

    /// <summary>
    /// Maximum turn rate cap (degrees/second).
    /// Equal to TURN_RATE_BASE. Prevents modifier stacking from
    /// exceeding the physical maximum. Defensive clamp only.
    /// </summary>
    public const float TURN_RATE_CAP = 720.0f;
}
```

### Turn Rate Calculation

```csharp
/// <summary>
/// Calculates the maximum turn rate for an agent given current speed and attributes.
///
/// This is the AUTHORITATIVE turn rate calculation. All systems that constrain
/// angular velocity â€” including Section 3.3's facing direction rotation â€”
/// MUST use this function or its return value. No other function may
/// independently compute turn rate.
///
/// Modifier chain (applied in this order):
///   1. Raw Agility â†’ PerformanceContext â†’ Effective Agility
///   2. Raw Balance â†’ PerformanceContext â†’ Effective Balance
///   3. Effective Agility â†’ k_turn (speed sensitivity coefficient)
///   4. Effective Balance â†’ balance_modifier (0.85â€“1.0)
///   5. Base formula: Ï‰ = TURN_RATE_BASE / (1 + k_turn Ã— speed)
///   6. Ã— balance_modifier
///   7. Ã— state_modifier (1.0 normally, 0.6 for DECELERATING, 0.0 for STUMBLING/GROUNDED)
///   8. Clamped to [TURN_RATE_FLOOR, TURN_RATE_CAP]
///
/// Called once per frame (60Hz) during the movement update phase.
/// The returned value is used for:
///   - Clamping movement direction rotation (path curvature)
///   - Clamping facing direction rotation (Section 3.3)
///   - Stumble risk evaluation (Section 3.4.4)
///   - Lean angle calculation (Section 3.4.5)
/// </summary>
/// <param name="speed">Current agent speed in m/s</param>
/// <param name="effectiveAgility">Post-PerformanceContext Agility (1.0â€“20.0 range)</param>
/// <param name="effectiveBalance">Post-PerformanceContext Balance (1.0â€“20.0 range)</param>
/// <param name="state">Current movement state from state machine</param>
/// <returns>Maximum turn rate in degrees/second</returns>
public static float CalculateMaxTurnRate(
    float speed,
    float effectiveAgility,
    float effectiveBalance,
    AgentMovementState state)
{
    // Step 1: Map Agility to speed sensitivity coefficient.
    // Lower k_turn = less speed penalty = nimbler turning.
    float kTurn = TurnConstants.K_TURN_MAX
                - (effectiveAgility - 1.0f) * TurnConstants.K_TURN_PER_POINT;
    kTurn = Mathf.Clamp(kTurn, TurnConstants.K_TURN_MIN, TurnConstants.K_TURN_MAX);

    // Step 2: Calculate base turn rate from inverse speed model.
    // At v=0: returns TURN_RATE_BASE (720Â°/s).
    // As v increases: rate decreases asymptotically.
    float baseTurnRate = TurnConstants.TURN_RATE_BASE / (1.0f + kTurn * speed);

    // Step 3: Apply Balance modifier (secondary contribution).
    float balanceMod = TurnConstants.BALANCE_MOD_MIN
                     + (effectiveBalance - 1.0f) * TurnConstants.BALANCE_MOD_PER_POINT;
    balanceMod = Mathf.Clamp(balanceMod, TurnConstants.BALANCE_MOD_MIN,
                             TurnConstants.BALANCE_MOD_MAX);

    float turnRate = baseTurnRate * balanceMod;

    // Step 4: Apply state-specific modifier.
    float stateMod = state switch
    {
        AgentMovementState.IDLE         => 1.0f,
        AgentMovementState.WALKING      => 1.0f,
        AgentMovementState.JOGGING      => 1.0f,
        AgentMovementState.SPRINTING    => 1.0f,
        AgentMovementState.DECELERATING => TurnConstants.DECEL_TURN_MODIFIER,
        AgentMovementState.STUMBLING    => TurnConstants.STUMBLE_TURN_MODIFIER,
        AgentMovementState.GROUNDED     => TurnConstants.GROUNDED_TURN_MODIFIER,
        _ => 1.0f
    };

    turnRate *= stateMod;

    // Step 5: Clamp to safety bounds.
    return Mathf.Clamp(turnRate, TurnConstants.TURN_RATE_FLOOR,
                       TurnConstants.TURN_RATE_CAP);
}
```

### Applying Turn Rate to Direction Change

```csharp
/// <summary>
/// Applies the turn rate limit to a requested direction change.
/// Returns the actual direction the agent can achieve this frame.
///
/// Uses signed-angle rotation (consistent with Section 3.3's facing system)
/// to guarantee constant angular velocity regardless of angle magnitude.
///
/// This function is used for BOTH:
///   - Movement direction rotation (path curvature)
///   - Facing direction rotation (consumed by Section 3.3's UpdateFacingDirection)
///
/// The caller is responsible for determining the desired direction. This function
/// only enforces the angular velocity constraint.
/// </summary>
/// <param name="currentDirection">Current direction unit vector (XY plane)</param>
/// <param name="desiredDirection">Target direction unit vector (XY plane)</param>
/// <param name="maxTurnRate">Maximum turn rate in degrees/second (from CalculateMaxTurnRate)</param>
/// <param name="dt">Frame delta time in seconds</param>
/// <returns>Clamped direction unit vector achievable this frame</returns>
public static Vector2 ApplyTurnRateLimit(
    Vector2 currentDirection,
    Vector2 desiredDirection,
    float maxTurnRate,
    float dt)
{
    // Calculate signed angle from current to desired (degrees).
    // Positive = counterclockwise, Negative = clockwise.
    float angleDelta = SignedAngleDeg(currentDirection, desiredDirection);

    // Maximum angle change this frame.
    float maxAngleThisFrame = maxTurnRate * dt;

    // If the desired turn is within our budget, apply it exactly.
    if (Mathf.Abs(angleDelta) <= maxAngleThisFrame)
    {
        return desiredDirection.normalized;
    }

    // Otherwise, rotate by the maximum amount toward the desired direction.
    float clampedAngle = Mathf.Sign(angleDelta) * maxAngleThisFrame;
    return RotateByDeg(currentDirection, clampedAngle).normalized;
}

/// <summary>
/// Returns the signed angle in degrees from vector 'from' to vector 'to'.
/// Positive = counterclockwise. Uses Atan2 for uniform angular velocity
/// (Section 3.3 requirement â€” no Lerp-based rotation).
/// </summary>
private static float SignedAngleDeg(Vector2 from, Vector2 to)
{
    return Mathf.Atan2(
        from.x * to.y - from.y * to.x,
        from.x * to.x + from.y * to.y
    ) * Mathf.Rad2Deg;
}

/// <summary>
/// Rotates a 2D vector by the given angle in degrees.
/// </summary>
private static Vector2 RotateByDeg(Vector2 v, float degrees)
{
    float rad = degrees * Mathf.Deg2Rad;
    float cos = Mathf.Cos(rad);
    float sin = Mathf.Sin(rad);
    return new Vector2(
        v.x * cos - v.y * sin,
        v.x * sin + v.y * cos
    );
}
```

### Turn Rate Summary Table

Reference table showing turn rate at representative speeds and attributes. All values in degrees/second.

```
                        Agility 1          Agility 10         Agility 20
Speed    State Label    Bal 1   Bal 20     Bal 10  Bal 20     Bal 1   Bal 20
â”€â”€â”€â”€â”€â”€   â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€    â”€â”€â”€â”€â”€   â”€â”€â”€â”€â”€â”€     â”€â”€â”€â”€â”€â”€  â”€â”€â”€â”€â”€â”€     â”€â”€â”€â”€â”€   â”€â”€â”€â”€â”€â”€
0 m/s    (IDLE)         612.0   720.0      663.2   720.0      612.0   720.0
1.5 m/s  WALKING        282.0   331.8      355.7   386.2      401.3   472.1
4.0 m/s  JOGGING        148.5   174.8      200.6   217.8      255.0   300.0
5.8 m/s  JOG/SPRINT     110.8   130.3      152.7   165.8      202.0   237.6
9.0 m/s  SPRINTING       76.3    89.8      107.2   116.4      147.5   173.5
10.2 m/s MAX_SPRINT      68.3    80.4       96.4   104.7      133.9   157.5

State: DECELERATING (0.6Ã— modifier applied to above values)
5.8 m/s  DECEL           66.5    78.2       91.6    99.5      121.2   142.6
9.0 m/s  DECEL           45.8    53.9       64.3    69.8       88.5   104.1

State: STUMBLING / GROUNDED
(any)    NONE             0.0     0.0        0.0     0.0        0.0     0.0
```

**Validation against Section 3.1.6 labels:**

| State | Label | Expected Behavior | Achieved Range (Â°/s) | Assessment |
|---|---|---|---|---|
| IDLE | Free | Unrestricted | 612â€“720 | âœ“ Full rotation in <0.6s |
| WALKING | Free | Near-unrestricted | 282â€“472 | âœ“ Full rotation in <1.3s |
| JOGGING | Moderate | Noticeable constraint | 149â€“300 | âœ“ 90Â° turn in 0.30â€“0.60s |
| SPRINTING | Tight | Significant constraint | 76â€“174 | âœ“ 90Â° turn in 0.52â€“1.18s |
| DECELERATING | Limited | Reduced control | 46â€“143 | âœ“ Clearly slower than sprinting |
| STUMBLING | None | Momentum only | 0 | âœ“ No voluntary turning |
| GROUNDED | None | No movement | 0 | âœ“ No turning |

---

## 3.4.3 Minimum Turn Radius

The minimum turn radius is not an independent parameter â€” it is derived from the turn rate and current speed. It is provided here as a reference for spatial reasoning (how much space an agent needs to curve), collision prediction, and AI path planning.

### Derivation

```
Given:
  Ï‰_max = maximum turn rate (Â°/s) â†’ convert to rad/s: Ï‰_rad = Ï‰_max Ã— Ï€/180
  v     = current speed (m/s)

The minimum radius of curvature is:
  r_min = v / Ï‰_rad

Equivalently:
  r_min = v Ã— 180 / (Ï‰_max Ã— Ï€)

This is the tightest circular arc the agent can follow at their current speed
without exceeding the turn rate limit.
```

### Reference Values

```
Minimum turn radius (meters) at representative speeds:

                        Agility 1, Bal 10       Agility 12, Bal 12     Agility 20, Bal 20
Speed                   r_min                   r_min                  r_min
â”€â”€â”€â”€â”€â”€                  â”€â”€â”€â”€â”€â”€                  â”€â”€â”€â”€â”€â”€                 â”€â”€â”€â”€â”€â”€
1.5 m/s                 0.28m                   0.23m                  0.18m
4.0 m/s                 1.42m                   1.06m                  0.76m
5.8 m/s                 2.77m                   2.01m                  1.40m
9.0 m/s                 6.24m                   4.42m                  2.97m
10.2 m/s                7.89m                   5.56m                  3.71m
```

**Validation:**
- At walking speed (1.5 m/s), all agents turn within 0.28m â€” essentially on the spot. âœ“
- At jog speed (4 m/s), elite agent turns within 0.76m â€” a quick sidestep. âœ“
- At sprint (9 m/s), elite agent needs 2.97m â€” a sharp but committed cut. âœ“
- At sprint (9 m/s), Agility 1 agent needs 6.24m â€” a wide, sweeping arc. âœ“
- At sprint, the ratio between worst and best is 2.27Ã—, providing meaningful tactical differentiation. âœ“

### Centripetal Acceleration Check

The centripetal acceleration at maximum turn rate is:

```
a_c = v Ã— Ï‰_rad = vÂ² / r_min
```

At sprint speed (9 m/s):

| Agility | Balance | Ï‰_max (Â°/s) | r_min (m) | a_c (m/sÂ²) | g-force |
|---------|---------|-------------|-----------|-------------|---------|
| 1       | 1       | 76.3        | 6.76      | 11.99       | 1.22g   |
| 1       | 20      | 89.8        | 5.74      | 14.10       | 1.44g   |
| 12      | 12      | 116.7       | 4.42      | 18.33       | 1.87g   |
| 20      | 1       | 147.5       | 3.50      | 23.16       | 2.36g   |
| 20      | 20      | 173.5       | 2.97      | 27.25       | 2.78g   |

**Biomechanical assessment:** Peak centripetal accelerations of 12â€“27 m/sÂ² at sprint correspond to 1.2â€“2.8g. Sports science literature reports peak lateral ground reaction forces during maximal change-of-direction tasks at 1.5â€“3.0Ã— body weight (Dos'Santos et al., 2020; Shimokochi et al., 2013), which maps directly to 1.5â€“3.0g. The model's range sits comfortably within documented human capability, with the elite ceiling (2.78g) leaving headroom for future modifiers (Stage 1 dribbling penalty, Stage 2 surface effects) that may push effective values slightly higher.

**Critical note:** These are *peak instantaneous* values at *maximum* turn rate. In practice, agents do not sustain maximum centripetal acceleration â€” they either decelerate into turns or use a fraction of their available turn rate for gradual curves. The stumble risk system (Section 3.4.4) provides consequences when agents operate near peak centripetal values at high speed, preventing the system from feeling unrealistic.

---

