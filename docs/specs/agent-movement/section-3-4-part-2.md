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

