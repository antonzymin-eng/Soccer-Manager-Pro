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

## 3.4.4 Stumble Risk Mechanics

### Design Overview

FR-5 requires: *"Sharp turn attempts at high speed SHALL trigger deceleration or stumble risk."*

The stumble system implements this through a two-tier safety model:

1. **Safe zone** (Ï‰_actual â‰¤ Ï‰_safe): No risk. Agent turns freely up to the safe threshold.
2. **Risk zone** (Ï‰_safe < Ï‰_actual â‰¤ Ï‰_max): Linearly increasing stumble probability. Agent can complete the turn but may lose balance.

This creates a per-frame risk decision: the movement system can use the full Ï‰_max for sharp turns, but only Ï‰_safe is guaranteed consequence-free. The gap between them is where gameplay skill differentiation occurs â€” high-Balance players have a wider safe zone and lower stumble probability, making sharp cuts at speed less risky.

### Interaction with Collision System Stumble Trigger

Section 3.1.2 defines STUMBLING state entry from **two independent sources**:

1. **This section** â€” turn angle exceeds safe threshold at speed (biomechanical loss of balance)
2. **Collision System (Spec #3)** â€” collision knockback (external force disruption)

Both triggers use the **same STUMBLING state** and the **same dwell time calculation** (Section 3.1.5). This is intentional â€” regardless of *cause*, the recovery process is identical: the agent has lost balance and must regain control before voluntary movement resumes. The dwell time calculation (Balance + Strength + collision force scaling) applies uniformly.

**Stacking rule:** If an agent is already STUMBLING (from a turn) and receives a collision, the collision does NOT reset the stumble timer â€” it transitions to GROUNDED if the collision force exceeds the knockdown threshold (Spec #3), or the existing stumble dwell continues otherwise. Conversely, if an agent in STUMBLING from a collision attempts a turn, the turn rate is 0.0Ã— (no voluntary control), so no turn-based stumble check occurs.

**Simultaneous trigger edge case:** If a collision occurs on the same frame as a turn-triggered stumble, only one STUMBLING entry fires. Turn-based stumble evaluation runs at pipeline step 6 (Section 3.4.6), while collision detection runs after position integration at step 9. The turn trigger takes precedence by execution order. The collision system MUST check `if (state != STUMBLING)` before applying its own stumble trigger to prevent dwell timer resets ("stumble chaining").

**Verification note:** When Spec #3 (Collision System) is drafted, verify that the collision-to-STUMBLING trigger uses the same `GroundedReason` / dwell infrastructure and does not introduce a parallel recovery path.

### Constants

```csharp
/// <summary>
/// Stumble risk constants for the turning system.
///
/// The stumble check runs every frame (60Hz) during movement update.
/// It evaluates the ACTUAL angular velocity used this frame (which may be
/// less than Ï‰_max if the desired turn was small) against the safe threshold.
///
/// Stumble probability is per-frame, not per-second. At 60Hz, the
/// probabilities below produce expected stumble frequencies of:
///   Balance 1, full risk zone: ~18 stumbles per second of risky turning
///     â†’ near-certain over a sustained sharp turn
///   Balance 20, full risk zone: ~3 stumbles per second of risky turning
///     â†’ survivable for brief cuts
///
/// In practice, agents operate in the risk zone for only 2â€“5 frames during
/// a sharp cut, so the actual stumble rate is much lower.
///
/// Calibration note: STUMBLE_PROB_MAX was reduced from an initial 0.40 to
/// 0.30 during design review. At 0.40, a Balance 1 player had ~78% stumble
/// chance over 3 risk-zone frames â€” this made low-Balance defenders feel
/// broken rather than merely disadvantaged. At 0.30, the same scenario
/// produces ~55%, which is punishing but survivable for occasional cuts.
/// The goal is that low Balance is a clear disadvantage that influences
/// tactical decisions (jockey instead of lunge), not a disability that
/// makes a player unusable. Can be tuned further during playtesting.
/// </summary>
public static class StumbleConstants
{
    // ================================================================
    // SAFE ZONE THRESHOLD
    // ================================================================

    /// <summary>
    /// Safe fraction of Ï‰_max at Balance 1 (narrowest safe zone).
    /// Agent can use only 55% of maximum turn rate without stumble risk.
    /// Remaining 45% of turn rate is available but risky.
    ///
    /// Rationale: A low-Balance player (think of a tall, top-heavy forward
    /// like Peter Crouch) has poor weight distribution during direction
    /// changes. More than half their theoretical turn rate puts them at risk.
    /// </summary>
    public const float SAFE_FRACTION_MIN = 0.55f;

    /// <summary>
    /// Safe fraction of Ï‰_max at Balance 20 (widest safe zone).
    /// Agent can use 85% of maximum turn rate without stumble risk.
    /// Only the final 15% of turn rate carries risk.
    ///
    /// Rationale: An elite-Balance player (think of Messi, low centre of
    /// gravity, exceptional proprioception) can push their turning limits
    /// with minimal risk of losing balance.
    /// </summary>
    public const float SAFE_FRACTION_MAX = 0.85f;

    /// <summary>
    /// Per-attribute-point safe fraction increase.
    /// (0.85 - 0.55) / 19 = 0.01579 per Balance point.
    /// </summary>
    public const float SAFE_FRACTION_PER_POINT =
        (SAFE_FRACTION_MAX - SAFE_FRACTION_MIN) / 19.0f;

    // ================================================================
    // STUMBLE PROBABILITY
    // ================================================================

    /// <summary>
    /// Per-frame stumble probability at Balance 1 when at Ï‰_max (top of risk zone).
    /// 0.30 = 30% chance per frame of stumbling at absolute maximum turn rate.
    ///
    /// At 60Hz, operating at Ï‰_max for 3 frames (one sharp cut):
    ///   P(no stumble over 3 frames) = (1 - 0.30)^3 = 0.343
    ///   P(stumble) = 65.7% â†’ Low Balance makes aggressive cuts very dangerous,
    ///   but not impossible. The agent has roughly 1-in-3 chance of pulling it off.
    /// </summary>
    public const float STUMBLE_PROB_MAX = 0.30f;

    /// <summary>
    /// Per-frame stumble probability at Balance 20 when at Ï‰_max.
    /// 0.05 = 5% chance per frame at absolute maximum turn rate.
    ///
    /// At 60Hz, operating at Ï‰_max for 3 frames:
    ///   P(no stumble over 3 frames) = (1 - 0.05)^3 = 0.857
    ///   P(stumble) = 14.3% â†’ High Balance can attempt aggressive cuts
    ///   with manageable risk.
    /// </summary>
    public const float STUMBLE_PROB_MIN = 0.05f;

    /// <summary>
    /// Per-attribute-point stumble probability reduction.
    /// (0.30 - 0.05) / 19 = 0.01316 per Balance point.
    /// </summary>
    public const float STUMBLE_PROB_PER_POINT =
        (STUMBLE_PROB_MAX - STUMBLE_PROB_MIN) / 19.0f;

    // ================================================================
    // SPEED THRESHOLD
    // ================================================================

    /// <summary>
    /// Minimum speed for stumble system activation (m/s).
    /// Below this speed, no stumble risk regardless of turn rate.
    ///
    /// Set to JOG_ENTER (2.2 m/s). At walking pace, players have enough
    /// time and ground contact to recover from any direction change.
    /// Stumble risk only applies at jogging speed and above.
    ///
    /// This is consistent with Section 3.1.6 where WALKING has "Free" turning
    /// and JOGGING begins "Moderate" constraints.
    /// </summary>
    public const float STUMBLE_SPEED_THRESHOLD = 2.2f;
}
```

### Stumble Evaluation

```csharp
/// <summary>
/// Evaluates stumble risk for a turn performed this frame.
/// Returns true if the agent stumbles (transitions to STUMBLING state).
///
/// Called AFTER the turn has been applied for this frame. The actual
/// angular velocity used is compared against the safe threshold.
///
/// If stumble is triggered:
///   - State machine transitions to STUMBLING (Section 3.1)
///   - Dwell time calculated from Balance + speed (Section 3.1.5)
///   - Agent loses voluntary control for dwell duration
///   - Current velocity preserved (momentum carries agent)
///   - AI notified via AgentStumbleEvent for tactical response
///
/// Determinism note: The probability check uses the match-level RNG seeded
/// at kickoff. This ensures replay-identical results. The RNG call is made
/// every frame the agent is in the risk zone, even if the probability is very
/// low, to maintain RNG sequence consistency for determinism.
/// </summary>
/// <param name="actualAngularVelocity">Actual turn rate used this frame (Â°/s)</param>
/// <param name="maxTurnRate">Maximum turn rate from CalculateMaxTurnRate (Â°/s)</param>
/// <param name="effectiveBalance">Post-PerformanceContext Balance (1.0â€“20.0)</param>
/// <param name="currentSpeed">Current agent speed (m/s)</param>
/// <param name="rng">Deterministic match RNG</param>
/// <returns>True if stumble triggered, false otherwise</returns>
public static bool EvaluateStumbleRisk(
    float actualAngularVelocity,
    float maxTurnRate,
    float effectiveBalance,
    float currentSpeed,
    System.Random rng)
{
    // No stumble risk below speed threshold (walking pace).
    if (currentSpeed < StumbleConstants.STUMBLE_SPEED_THRESHOLD)
        return false;

    // No stumble risk if agent is barely turning.
    if (actualAngularVelocity < 1.0f)
        return false;

    // Calculate safe threshold for this agent.
    float safeFraction = StumbleConstants.SAFE_FRACTION_MIN
                       + (effectiveBalance - 1.0f) * StumbleConstants.SAFE_FRACTION_PER_POINT;
    safeFraction = Mathf.Clamp(safeFraction, StumbleConstants.SAFE_FRACTION_MIN,
                               StumbleConstants.SAFE_FRACTION_MAX);

    float safeTurnRate = maxTurnRate * safeFraction;

    // If within safe zone, no risk.
    if (actualAngularVelocity <= safeTurnRate)
        return false;

    // In risk zone â€” calculate probability.
    // Linear interpolation: 0% at safeTurnRate, max% at maxTurnRate.
    float riskFraction = (actualAngularVelocity - safeTurnRate)
                       / (maxTurnRate - safeTurnRate);
    riskFraction = Mathf.Clamp01(riskFraction);

    float maxProb = StumbleConstants.STUMBLE_PROB_MAX
                  - (effectiveBalance - 1.0f) * StumbleConstants.STUMBLE_PROB_PER_POINT;
    maxProb = Mathf.Clamp(maxProb, StumbleConstants.STUMBLE_PROB_MIN,
                          StumbleConstants.STUMBLE_PROB_MAX);

    float stumbleProb = riskFraction * maxProb;

    // Deterministic probability check.
    float roll = (float)rng.NextDouble();
    return roll < stumbleProb;
}
```

### Stumble Risk Profiles

Summary of stumble behavior across attribute ranges:

```
Balance 1 (worst stability):
  Safe zone: 0%â€“55% of Ï‰_max â†’ no risk
  Risk zone: 55%â€“100% of Ï‰_max â†’ 0%â€“30% per frame
  At 80% of Ï‰_max: riskFrac = 0.556, P = 16.7% per frame
  Sharp cut (3 frames at 90% Ï‰_max): riskFrac = 0.778, P/frame = 23.3%
    â†’ P(stumble) = 1 - (1 - 0.233)^3 = 54.9%

Balance 10 (average):
  Safe zone: 0%â€“69% of Ï‰_max
  Risk zone: 69%â€“100% â†’ 0%â€“18.2% per frame
  At 80% of Ï‰_max: riskFrac = 0.350, P = 6.4% per frame
  Sharp cut (3 frames at 90% Ï‰_max): riskFrac = 0.675, P/frame = 12.3%
    â†’ P(stumble) = 1 - (1 - 0.123)^3 = 32.5%

Balance 20 (best stability):
  Safe zone: 0%â€“85% of Ï‰_max
  Risk zone: 85%â€“100% â†’ 0%â€“5% per frame
  At 80% of Ï‰_max: In safe zone â†’ 0% risk
  Sharp cut (3 frames at 90% Ï‰_max): riskFrac = 0.333, P/frame = 1.7%
    â†’ P(stumble) = 1 - (1 - 0.017)^3 = 4.9%
```

**Gameplay implications:** Balance creates a clear risk gradient without making low-Balance players unusable. A Balance 1 player attempting a sharp cut at sprint speed has a ~55% chance of stumbling â€” dangerous, but not certain. The agent can still pull off the cut roughly half the time, creating dramatic moments. A Balance 20 player faces only ~5% risk, rewarding players who scout and deploy agile, balanced defenders. The 80%-of-Ï‰_max threshold is particularly telling: a Balance 20 player is still in their safe zone at this intensity, while a Balance 1 player faces 16.7% risk *per frame*. This shapes AI behavior â€” low-Balance agents should prefer conservative turning profiles while high-Balance agents can afford to commit.

---

## 3.4.5 Lean Angle Output

### Purpose

Lean angle is the body tilt an agent exhibits during a turn, caused by centripetal force. In Stage 0, this is a **data-only output** for the animation data contract (FR-8). No system consumes it. In Stage 1, the animation system will use it for visual feedback: agents leaning into sharp turns, recovering upright when running straight.

### Formula

```csharp
/// <summary>
/// Calculates the lean angle for the animation data contract.
///
/// Lean angle = arctangent of (centripetal acceleration / gravity).
/// This is the physical tilt angle a body would assume to maintain
/// balance during a circular turn â€” the same principle as a cyclist
/// leaning into a bend.
///
/// Capped at MAX_LEAN_ANGLE to prevent extreme visual tilt. Real
/// football players rarely exceed 35â€“40Â° lean during the most
/// aggressive direction changes (Havens & Sigward, 2015).
///
/// Sign convention: Positive = leaning left (turning left).
///                  Negative = leaning right (turning right).
/// Consistent with Section 3.1.8 coordinate system.
///
/// Output: Part of AnimationDataContract struct (FR-8).
///   - Field: leanAngleDeg (float)
///   - Updated: every frame (60Hz)
///   - Cost: 1 atan2 + 1 multiply (negligible)
/// </summary>
public static class LeanConstants
{
    /// <summary>
    /// Maximum lean angle in degrees.
    /// Capped at 40Â° â€” beyond this, a real player would lose balance.
    /// Visual reference: Messi at peak direction change leans ~30â€“35Â°.
    /// 40Â° provides headroom for extreme cases without looking unrealistic.
    /// </summary>
    public const float MAX_LEAN_ANGLE = 40.0f;

    /// <summary>
    /// Minimum angular velocity to produce lean (degrees/second).
    /// Below this, lean angle is zero (agent is running straight).
    /// Prevents visual jitter from micro-corrections in facing direction.
    /// 5Â°/s is imperceptible â€” well below any deliberate turn.
    /// </summary>
    public const float LEAN_DEAD_ZONE = 5.0f;

    /// <summary>Gravity constant for lean calculation (m/sÂ²)</summary>
    public const float GRAVITY = 9.81f;
}

/// <summary>
/// Calculates lean angle from current speed and actual angular velocity.
///
/// Called every frame during movement update. Returns signed angle.
/// </summary>
/// <param name="speed">Current speed in m/s</param>
/// <param name="actualAngularVelocityDeg">Actual angular velocity this frame
/// (Â°/s, signed)</param>
/// <returns>Lean angle in degrees (positive = leaning left,
/// negative = leaning right)</returns>
public static float CalculateLeanAngle(float speed, float actualAngularVelocityDeg)
{
    // No lean if barely turning.
    if (Mathf.Abs(actualAngularVelocityDeg) < LeanConstants.LEAN_DEAD_ZONE)
        return 0.0f;

    // Convert angular velocity to rad/s for physics calculation.
    float omegaRad = actualAngularVelocityDeg * Mathf.Deg2Rad;

    // Centripetal acceleration: a_c = v Ã— Ï‰
    float centripetalAccel = speed * Mathf.Abs(omegaRad);

    // Lean angle: Î¸ = atan(a_c / g)
    float leanMagnitude = Mathf.Atan2(centripetalAccel, LeanConstants.GRAVITY)
                        * Mathf.Rad2Deg;

    // Cap to maximum.
    leanMagnitude = Mathf.Min(leanMagnitude, LeanConstants.MAX_LEAN_ANGLE);

    // Apply sign (lean in the direction of the turn).
    return Mathf.Sign(actualAngularVelocityDeg) * leanMagnitude;
}
```

### Lean Angle Reference Values

```
At sprint speed (9 m/s):

Angular Velocity    Centripetal Accel    Lean Angle
(Â°/s)               (m/sÂ²)              (degrees)
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€        â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€         â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
  30                  4.71                 25.7Â°
  60                  9.42                 43.9Â° â†’ capped to 40.0Â°
  90                 14.14                 55.2Â° â†’ capped to 40.0Â°
 150                 23.56                 67.5Â° â†’ capped to 40.0Â°

At jog speed (4 m/s):

  60                  4.19                 23.1Â°
 120                  8.38                 40.5Â° â†’ capped to 40.0Â°
 200                 13.96                 54.9Â° â†’ capped to 40.0Â°
```

**Observation:** Lean angle reaches the 40Â° cap at relatively moderate turn rates during sprinting (~55Â°/s) because the high speed amplifies centripetal acceleration. At jogging speed, agents can turn at ~120Â°/s before hitting the cap. This naturally produces the visual effect of agents leaning harder into turns at speed, which matches broadcast footage.

---

## 3.4.6 Integration with State Machine & Facing System

### Turn Rate as the Single Authority

Section 3.3 established that `UpdateFacingDirection()` rotates at the turn rate defined in this section. The critical integration point:

```
Section 3.4                    Section 3.3
CalculateMaxTurnRate()  â”€â”€â”€â†’  Used by UpdateFacingDirection()
         â”‚                    for BOTH AUTO_ALIGN and TARGET_LOCK
         â”‚
         â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â†’ Used by movement system to constrain
                              path direction change per frame
```

Both path rotation and facing rotation are constrained by the same Ï‰_max. An agent cannot simultaneously redirect their path and rotate their facing faster than the biomechanical limit allows.

**Implementation rule:** When facing direction and movement direction are changing simultaneously (common â€” e.g., an agent curves a run while tracking the ball), each rotation is constrained independently by the same Ï‰_max. This is physically justified because facing rotation (head/torso) and path rotation (legs/hips) are controlled by different muscle groups, both of which are constrained by overall speed.

**Known limitation â€” facing rate vs path rate:** In reality, a human can rotate their head and upper torso faster than they can redirect their legs mid-stride. This means a sprinting player can track the ball with their eyes (fast) even while their running arc changes slowly. Using the same Ï‰_max for both facing and path is a Stage 0 simplification. At sprint speeds where Ï‰_max drops to 76â€“174Â°/s, this may cause TARGET_LOCK mode to feel sluggish â€” an agent sprinting parallel to the ball at 9 m/s might take 0.5â€“1.2s to rotate their facing 90Â° toward the ball, which is slower than a real player's head tracking.
- **Lean cap saturation note:** At sprint speed, the 40° lean cap is reached at ~55°/s angular velocity, meaning virtually all deliberate turns at sprint produce maximum lean. Stage 1 may raise MAX_LEAN_ANGLE or implement non-linear scaling. Purely visual tuning with no gameplay impact.

**Stage 1 mitigation (non-binding, for planning only):** Introduce a `FACING_RATE_MULTIPLIER` (suggested range: 1.3â€“1.5Ã—) applied *only* to facing direction rotation, not path direction rotation. This would be a single constant multiplier on Ï‰_max when used by Section 3.3's `UpdateFacingDirection()`. It preserves the turn rate model intact â€” the multiplier applies after `CalculateMaxTurnRate()` returns, not within it. This is NOT implemented in Stage 0 to avoid premature complexity; it is flagged here so that Section 3.3 implementers are aware the facing rate may diverge from path rate in Stage 1.

**Known limitation and Stage 1 resolution path:** In reality, head and torso rotation is faster than lower-body redirection. The shared Ï‰_max simplification may create edge cases in TARGET_LOCK mode at high speed where an agent cannot visually track a fast-moving ball while sprinting. During Stage 1 playtesting, if this is confirmed as a problem, a `FACING_RATE_MULTIPLIER` (suggested: 1.2â€“1.5Ã—) can be applied exclusively to facing rotation without altering path curvature constraints. This would be a single-line change in `UpdateFacingDirection()` â€” multiply Ï‰_max by the facing multiplier before passing to `ApplyTurnRateLimit()`. The core turn rate formula and stumble system remain unchanged. See Future Extension Notes for formal specification.

### Per-Frame Movement Update Integration

The turn rate calculation integrates into the existing movement update pipeline as follows:

```
Per-frame movement update (60Hz):
  1. State machine evaluation (Section 3.1)
  2. PerformanceContext attribute evaluation (Section 3.2)
  3. â”€â”€â†’ Turn rate calculation (Section 3.4.2) â†â”€â”€ THIS SECTION
  4. Path direction update: constrain desired movement direction by Ï‰_max Ã— dt
  5. Facing direction update (Section 3.3): constrain facing rotation by Ï‰_max Ã— dt
  6. â”€â”€â†’ Stumble risk evaluation (Section 3.4.4) â†â”€â”€ THIS SECTION
  7. Directional multiplier calculation (Section 3.3)
  8. Top speed and acceleration (Section 3.2)
  9. Position integration
  10. â”€â”€â†’ Lean angle calculation (Section 3.4.5) â†â”€â”€ THIS SECTION
  11. Animation data contract output (FR-8)
```

### Forced Deceleration on Sharp Turns

When an agent requests a direction change that exceeds what the turn rate can achieve in a single frame, the movement system must choose between:

**Option A: Gradual turn** â€” Apply maximum turn rate, agent curves toward desired direction over multiple frames. Speed maintained.

**Option B: Forced deceleration** â€” Agent needs to decelerate to increase turn rate (inverse speed model means lower speed = higher Ï‰_max), then turn.

The system uses **Option A by default** (Section 3.4.2's `ApplyTurnRateLimit` handles this). Option B occurs organically when the AI or path planner issues a deceleration command because the desired path geometry requires a sharper turn than the current speed allows.

The movement system does NOT automatically decelerate agents for turns. The decision to slow down before a turn is an AI-layer responsibility (Spec #12: Positioning AI). This maintains the clean separation between physics layer (this spec) and decision layer. The physics layer tells the AI "at this speed, you can turn this fast." The AI decides whether to slow down.

**Exception:** If stumble is triggered (Section 3.4.4), the state machine forces STUMBLING, which inherently bleeds speed through friction drag (Section 3.1.6: STUMBLING deceleration = "Drag").

### Fatigue Interaction

FR-6 states the aerobic pool applies a "global movement modifier affecting top speed, acceleration, **and turn rate**." The fatigue system (Section 3.5) modifies turn rate through the PerformanceContext gateway:

```
Aerobic pool depleted â†’ PerformanceContext reduces Effective Agility and Effective Balance
  â†’ CalculateMaxTurnRate() receives lower attribute values
  â†’ Turn rate decreases naturally through the existing model
  â†’ No special fatigue modifier needed in this section
```

This is architecturally clean â€” fatigue affects turning through the same PerformanceContext gateway as all other movement parameters. No additional fatigue-specific code in the turning system.

### Momentum Preservation During Turning

The turning system interacts with agent momentum in three key ways:

1. **Speed is not automatically reduced during turns.** The turn rate model constrains how quickly the agent can change direction, but does not impose a speed penalty for turning. An agent sprinting at 9 m/s and executing a 45Â° turn over 0.5 seconds maintains 9 m/s throughout (subject to normal acceleration/deceleration from Section 3.2). Speed reduction for turns is an AI-layer decision, not a physics-layer enforcement. This prevents the "invisible wall" feel of forced deceleration.

2. **Stumble preserves velocity vector.** When `EvaluateStumbleRisk()` triggers STUMBLING, the agent's current velocity (speed Ã— direction) is preserved at the moment of stumble. The STUMBLING state applies friction drag (Section 3.1.6) to bleed speed over the dwell period, but the direction is locked â€” the agent skids in whatever direction they were traveling when they lost balance. No voluntary turning occurs (Ï‰ = 0).

3. **Centripetal force is implicit, not applied.** The model does not apply an inward centripetal force to the agent during turns. Instead, the movement direction vector rotates within the turn rate constraint, and the agent moves along the resulting curved path. This is a kinematic (position/velocity-based) model, not a dynamic (force-based) model. The centripetal acceleration values in Section 3.4.3 exist for biomechanical validation and lean angle calculation only â€” they do not produce forces that act on the agent's velocity.

### Performance Budget

The turning system adds the following per-agent per-frame computational cost:

| Operation | Ops | Cost |
|---|---|---|
| `CalculateMaxTurnRate()` | 1 divide, 2 multiply, 2 clamp, 1 switch | ~10 ops |
| `ApplyTurnRateLimit()` | 1 atan2, 1 cos, 1 sin, 2 multiply, 1 compare | ~15 ops |
| `EvaluateStumbleRisk()` | 3 compare, 2 multiply, 1 divide, 1 RNG call | ~12 ops |
| `CalculateLeanAngle()` | 1 atan2, 1 multiply, 1 compare, 1 sign | ~8 ops |
| **Total** | | **~45 ops** |

At ~1ns per arithmetic operation on modern hardware, the turning system costs approximately **0.045 Âµs per agent per frame**. Against the PR-1 budget of 50 Âµs per agent (for all movement systems combined), turning consumes **<0.1%** of the available budget. No optimization concerns.

---

## 3.4.7 Numerical Examples & Validation

### Example 1: Sprint-Speed Sharp Cut â€” Elite Winger

**Player:** Pace 18, Agility 18, Balance 15, Acceleration 16  
**Context:** PerformanceContext.CreateNeutral()  
**State:** SPRINTING at 9.0 m/s  
**Scenario:** Winger receives ball on wing, wants to cut inside (90Â° direction change)

```
Step 1: Calculate max turn rate
  EffectiveAgility = 18.0
  k_turn = 0.78 - (18.0 - 1.0) Ã— 0.02263 = 0.78 - 0.385 = 0.395
  baseTurnRate = 720 / (1 + 0.395 Ã— 9.0) = 720 / 4.555 = 158.1Â°/s

  EffectiveBalance = 15.0
  balanceMod = 0.85 + (15.0 - 1.0) Ã— 0.007895 = 0.85 + 0.111 = 0.961
  
  Ï‰_max = 158.1 Ã— 0.961 = 151.9Â°/s

Step 2: Time to complete 90Â° turn at full rate
  t = 90 / 151.9 = 0.592 seconds (36 frames at 60Hz)

Step 3: Stumble risk check
  safeFraction = 0.55 + (15.0 - 1.0) Ã— 0.01579 = 0.55 + 0.221 = 0.771
  safeTurnRate = 151.9 Ã— 0.771 = 117.1Â°/s
  
  Agent is at Ï‰_max (151.9Â°/s) > safeTurnRate (117.1Â°/s) â€” in risk zone.
  riskFraction = (151.9 - 117.1) / (151.9 - 117.1) = 1.0 (at max)
  maxProb = 0.30 - (15.0 - 1.0) Ã— 0.01316 = 0.30 - 0.184 = 0.116
  Per-frame stumble probability: 1.0 Ã— 0.116 = 11.6%
  
  Over 5 risk-zone frames: P(stumble) = 1 - (1 - 0.116)^5 = 46.0%

Step 4: Minimum turn radius
  Ï‰_rad = 151.9 Ã— Ï€/180 = 2.651 rad/s
  r_min = 9.0 / 2.651 = 3.39m

Step 5: Lean angle
  a_c = 9.0 Ã— 2.651 = 23.86 m/sÂ²
  lean = atan(23.86 / 9.81) Ã— 57.3 = 67.6Â° â†’ capped to 40.0Â°
```

**Gameplay analysis:** This elite winger can make a 90Â° cut at sprint speed in 0.59 seconds, but faces a ~46% stumble risk if they commit to maximum turn rate for the first 5 frames. The smart play: decelerate slightly before the cut (reducing speed to ~7 m/s increases turn rate and exits the risk zone), or accept the stumble risk for a more explosive direction change. This creates a meaningful risk/reward decision â€” roughly a coin flip at maximum effort. The 3.39m minimum radius means the cut sweeps through about 5m of pitch â€” visible and readable for opponents.

### Example 2: Centre-Back Tracking Runner â€” Low Agility

**Player:** Pace 12, Agility 7, Balance 11, Acceleration 10  
**Context:** PerformanceContext.CreateNeutral()  
**State:** SPRINTING at 8.5 m/s  
**Scenario:** CB is sprinting back, needs to follow attacker's diagonal run (45Â° direction change)

```
Step 1: Calculate max turn rate
  EffectiveAgility = 7.0
  k_turn = 0.78 - (7.0 - 1.0) Ã— 0.02263 = 0.78 - 0.136 = 0.644
  baseTurnRate = 720 / (1 + 0.644 Ã— 8.5) = 720 / 6.474 = 111.2Â°/s

  EffectiveBalance = 11.0
  balanceMod = 0.85 + (11.0 - 1.0) Ã— 0.007895 = 0.85 + 0.079 = 0.929
  
  Ï‰_max = 111.2 Ã— 0.929 = 103.3Â°/s

Step 2: Time to complete 45Â° turn
  t = 45 / 103.3 = 0.436 seconds (26 frames)

Step 3: Stumble risk check
  safeFraction = 0.55 + (11.0 - 1.0) Ã— 0.01579 = 0.55 + 0.158 = 0.708
  safeTurnRate = 103.3 Ã— 0.708 = 73.1Â°/s
  
  At Ï‰_max (103.3Â°/s) â€” in risk zone.
  riskFraction = (103.3 - 73.1) / (103.3 - 73.1) = 1.0
  maxProb = 0.30 - (11.0 - 1.0) Ã— 0.01316 = 0.30 - 0.132 = 0.168
  Per-frame: 1.0 Ã— 0.168 = 16.8%
  
  Over 3 risk frames: P(stumble) = 1 - (1 - 0.168)^3 = 42.5%

  BUT â€” the agent can use safe rate (73.1Â°/s) for zero risk:
  t_safe = 45 / 73.1 = 0.616 seconds (37 frames)
  Difference: 0.616 - 0.436 = 0.180 seconds slower, but 0% stumble risk.

Step 4: Minimum turn radius
  At Ï‰_max: r_min = 8.5 / (103.3 Ã— Ï€/180) = 8.5 / 1.803 = 4.71m
  At Ï‰_safe: r_min = 8.5 / (73.1 Ã— Ï€/180) = 8.5 / 1.276 = 6.66m
```

**Gameplay analysis:** This CB faces a stark choice. Using maximum turn rate to follow the runner's diagonal takes 0.44s but carries ~43% stumble risk. Using the safe rate takes 0.62s â€” 0.18s slower, during which a fast attacker covers an additional ~1.6m. This is the tactical depth the system creates: slower, less agile defenders must either commit to risky direction changes or accept being beaten for pace on cuts. Managers who pair low-Agility CBs with a high-Agility CDM covering the channels address this systematically.

### Example 3: Fatigued Player â€” Late Match Turn

**Player:** Pace 14, Agility 14, Balance 12  
**Context:** Minute 85, Aerobic pool at 0.35 (severe fatigue)  
**PerformanceContext modifier:** Combined = 0.78 (fatigue-driven reduction)  
**State:** JOGGING at 4.5 m/s  
**Scenario:** Midfielder tracking back, needs 60Â° direction change

```
Step 1: Effective attributes through PerformanceContext
  EffectiveAgility = 14.0 Ã— 0.78 = 10.92
  EffectiveBalance = 12.0 Ã— 0.78 = 9.36

Step 2: Calculate max turn rate
  k_turn = 0.78 - (10.92 - 1.0) Ã— 0.02263 = 0.78 - 0.225 = 0.555
  baseTurnRate = 720 / (1 + 0.555 Ã— 4.5) = 720 / 3.498 = 205.8Â°/s

  balanceMod = 0.85 + (9.36 - 1.0) Ã— 0.007895 = 0.85 + 0.066 = 0.916
  
  Ï‰_max = 205.8 Ã— 0.916 = 188.5Â°/s

Step 3: Compare to fresh version
  Fresh EffectiveAgility = 14.0, EffectiveBalance = 12.0
  k_turn_fresh = 0.78 - (14.0 - 1.0) Ã— 0.02263 = 0.486
  baseFresh = 720 / (1 + 0.486 Ã— 4.5) = 720 / 3.187 = 225.9Â°/s
  balanceFresh = 0.85 + (12.0 - 1.0) Ã— 0.007895 = 0.937
  Ï‰_max_fresh = 225.9 Ã— 0.937 = 211.7Â°/s

  Fatigue reduction: (211.7 - 188.5) / 211.7 = 11.0% slower turn rate

Step 4: Time for 60Â° turn
  Fatigued: 60 / 188.5 = 0.318 seconds
  Fresh:    60 / 211.7 = 0.283 seconds
  Difference: 35ms â†’ barely perceptible at jog speed

Step 5: Stumble risk (fatigued)
  safeFraction = 0.55 + (9.36 - 1.0) Ã— 0.01579 = 0.55 + 0.132 = 0.682
  safeTurnRate = 188.5 Ã— 0.682 = 128.6Â°/s
  
  Agent is at Ï‰_max (188.5Â°/s) â€” in risk zone if pushing max.
  But 60Â° turn at 188.5Â°/s takes only ~19 frames.
  The agent can use safe rate (128.6Â°/s) and still complete in 0.467s.
  At jog speed, this time difference is acceptable.
```

**Gameplay analysis:** Fatigue's effect on turning is subtle at jogging speed (11% reduction, 35ms difference) because the inverse model means lower speeds already produce high turn rates. The system correctly models the real-world observation that tired players lose more turning ability at sprint than at jog â€” fatigue devastates explosive direction changes but has modest impact on positional shuffling.

### Example 4: Deceleration State â€” Emergency Stop with Turn

**Player:** Agility 12, Balance 12  
**State:** DECELERATING at 7.0 m/s (was sprinting, now emergency braking)  
**Scenario:** Midfielder hit emergency decel and wants to turn 45Â°

```
Step 1: Turn rate with DECEL modifier
  k_turn = 0.78 - (12.0 - 1.0) Ã— 0.02263 = 0.531
  baseTurnRate = 720 / (1 + 0.531 Ã— 7.0) = 720 / 4.717 = 152.6Â°/s
  balanceMod = 0.85 + (12.0 - 1.0) Ã— 0.007895 = 0.937
  
  Before state modifier: 152.6 Ã— 0.937 = 143.0Â°/s
  After DECEL modifier:  143.0 Ã— 0.60 = 85.8Â°/s

Step 2: Time for 45Â° turn while decelerating
  t = 45 / 85.8 = 0.525 seconds

Step 3: Compare to same turn while sprinting (no decel modifier)
  Sprinting at 7.0 m/s: Ï‰_max = 143.0Â°/s (no 0.6Ã— modifier)
  t = 45 / 143.0 = 0.315 seconds
  
  Deceleration penalty: 0.525 - 0.315 = 0.210 seconds slower (67% longer)
```

**Gameplay analysis:** The DECEL modifier creates a meaningful penalty for trying to turn while braking. An agent who hit emergency decel and immediately tries to redirect faces a 67% time penalty compared to turning at the same speed without braking. This models the real-world difficulty of simultaneously planting to stop and cutting to a new direction â€” the competing force demands reduce both capabilities.

### Validation Summary

| Requirement | Source | Result | Status |
|---|---|---|---|
| Turn radius increases with velocity | FR-5 | r_min = v/Ï‰_rad; monotonically increases with v | âœ“ PASS |
| No instant turns above WALKING | FR-5 | Ï‰_max < 720Â°/s for v > 2.0 m/s | âœ“ PASS |
| Sharp turns at speed â†’ stumble risk | FR-5 | Risk zone active above JOG_ENTER speed | âœ“ PASS |
| Turn rate: Agility primary | FR-5 | 1.93Ã— range from Agility alone at sprint | âœ“ PASS |
| Turn rate: Balance secondary | FR-5 | 1.18Ã— range from Balance at sprint | âœ“ PASS |
| Combined turn rate range | FR-5 | 2.27Ã— at sprint (76.3 to 173.5Â°/s) | âœ“ PASS |
| Lean angle output for anim contract | FR-5, FR-8 | CalculateLeanAngle() in AnimationDataContract | âœ“ PASS |
| Agility 20 turns sharper than Agility 5 | QR-4 | r_min ratio: 2.27Ã— at sprint | âœ“ PASS |
| Balance 20 stumbles less than Balance 5 | QR-4 | Stumble prob ratio: 6Ã— at max turn | âœ“ PASS |
| Low Balance disadvantaged, not broken | Gameplay | Bal 1 at max: 55% stumble (3 frames) | âœ“ PASS |
| Centripetal accel within human range | Biomechanics | 12â€“27 m/sÂ² = 1.2â€“2.8g at sprint | âœ“ PASS |
| Centripetal headroom for future mods | Architecture | Peak 2.78g vs 3.0g ceiling = 7% buffer | âœ“ PASS |
| Fatigue affects turn rate | FR-6 | Via PerformanceContext gateway | âœ“ PASS |
| IDLE/WALKING: Free turning | 3.1.6 | 282â€“720Â°/s â†’ <1.3s full rotation | âœ“ PASS |
| JOGGING: Moderate constraint | 3.1.6 | 149â€“300Â°/s â†’ 90Â° in 0.30â€“0.60s | âœ“ PASS |
| SPRINTING: Tight constraint | 3.1.6 | 76â€“174Â°/s â†’ 90Â° in 0.52â€“1.18s | âœ“ PASS |
| DECELERATING: Limited control | 3.1.6 | 0.6Ã— modifier â†’ clearly slower | âœ“ PASS |
| STUMBLING/GROUNDED: None | 3.1.6 | 0.0Ã— modifier â†’ no voluntary turn | âœ“ PASS |
| Single turn rate governs path + facing | 3.3.4 | Same Ï‰_max passed to both systems | âœ“ PASS |
| Signed-angle rotation (not Lerp) | 3.3.4 | Atan2-based SignedAngleDeg() | âœ“ PASS |
| Turn radius formula equivalence | FR-5 | r = v/Ï‰ equivalent to r = vÂ²/(a_lat Ã— agi) | âœ“ PASS |
| Collision stumble shares dwell calc | 3.1.5 | Same STUMBLING state, same recovery | âœ“ PASS |
| Facing rate limitation documented | Architecture | Stage 1 facing multiplier path identified | âœ“ PASS |
| Speed preserved during turns | Momentum | No auto-deceleration; AI decides speed reduction | âœ“ PASS |
| Stumble preserves velocity vector | Momentum | Direction locked, friction drag bleeds speed | âœ“ PASS |
| Kinematic model (no centripetal force) | Architecture | Centripetal values for validation/lean only | âœ“ PASS |
| Per-agent cost <50Âµs | PR-1 | ~0.045Âµs per agent (<0.1% of budget) | âœ“ PASS |

---

## Cross-References

| Section | References To | Nature |
|---|---|---|
| 3.4.1 (Philosophy) | FR-5 (Sections 1-2) | Turn radius formula divergence explained |
| 3.4.2 (Turn Rate) | FR-5 (Sections 1-2) | Implements turn rate and turn radius requirements |
| 3.4.2 | Section 3.2.1 (PerformanceContext) | Agility and Balance evaluated through gateway |
| 3.4.2 | Section 3.1.6 (State-Physics Table) | Turn rate labels validated against model output |
| 3.4.2 | Section 3.3.4 (Facing Direction) | Turn rate consumed by UpdateFacingDirection() |
| 3.4.3 (Turn Radius) | FR-5 (Sections 1-2) | Derived from turn rate; equivalence to FR-5 formula demonstrated |
| 3.4.4 (Stumble) | Section 3.1.2 (STUMBLING state) | Stumble trigger â†’ state transition |
| 3.4.4 | Section 3.1.5 (Dwell Calculation) | Stumble dwell time from Balance + speed |
| 3.4.4 | Spec #3 (Collision System) | Shared STUMBLING state; stacking rules defined |
| 3.4.4 | Spec #17 (Event System) | AgentStumbleEvent published on trigger |
| 3.4.5 (Lean Angle) | FR-8 (Animation Contract) | Data output for Stage 1 animation |
| 3.4.6 (Integration) | Section 3.5 (Fatigue) | Fatigue affects turn rate via PerformanceContext |
| 3.4.6 | Spec #12 (Positioning AI) | AI consumes turn radius for path planning |

---

## Future Extension Notes

**Stage 1 â€” Facing rate multiplier:**
- If playtesting confirms that shared Ï‰_max creates unrealistic TARGET_LOCK behavior at sprint speed, introduce `FACING_RATE_MULTIPLIER` (1.2â€“1.5Ã—). Applied as a single multiplication in `UpdateFacingDirection()` before calling `ApplyTurnRateLimit()`. The core `CalculateMaxTurnRate()` formula is unchanged â€” only the facing system's consumption of Ï‰_max is modified. Path curvature constraints remain at base Ï‰_max. This maintains the principle that turning the body (path) is harder than turning the head (facing) while keeping a single authoritative turn rate source.

**Stage 1 â€” Animation-driven lean:**
- Animation system consumes `leanAngleDeg` from the animation data contract to tilt agent models during turns. No change to this section's calculations required â€” the data is already being produced.


**Stage 1 â€” Dribbling turn penalty:**
- Agent with ball at feet receives an additional turn rate penalty (harder to change direction while controlling the ball). Implemented as an external multiplier on the return value of `CalculateMaxTurnRate()`, not a change to the turn rate formula itself. Suggested modifier range: 0.65â€“0.85Ã— depending on Dribbling attribute.

**Stage 2 â€” Surface traction effects:**
- Wet or degraded pitch surfaces reduce available lateral grip, lowering the effective safe fraction (wider risk zone for stumble). Implemented as a surface modifier on `SAFE_FRACTION_MIN/MAX`, not a change to the turn rate model.

**Stage 2 â€” Kinetic profile interaction:**
- Bio-mechanical profile (heel-striker, forefoot, neutral â€” Master Vol 1, Â§2.2) could modify k_turn slightly: forefoot runners turn tighter (lower k_turn modifier), heel-strikers turn wider. This is a modifier on the k_turn calculation, preserving the existing formula.

**Stage 4 â€” Pressure-induced turn hesitation:**
- High-pressure situations (crowd hostility, late match importance) could add noise to the turn rate, simulating momentary indecision. This would be a random modifier on the `CalculateMaxTurnRate()` output, not a structural change.

**Stage 5 â€” Fixed64 migration:**
- All `float` types in this section become `Fixed64`.
- `Mathf.Atan2()` replaced by `Fixed64Math.Atan2()` (lookup table implementation).
- `Mathf.Cos()/Sin()` replaced by `Fixed64Math.Cos()/Sin()`.
- `Mathf.Clamp()` replaced by `Fixed64Math.Clamp()`.
- No algorithmic changes required â€” only type substitution.

---

**End of Section 3.4**

**Page Count:** ~17 pages  
**Next Section:** Section 3.5 â€” Fatigue Integration (dual-energy model, aerobic pool, sprint reservoir)
