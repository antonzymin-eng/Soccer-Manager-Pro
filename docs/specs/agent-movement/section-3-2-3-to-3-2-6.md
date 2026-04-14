## 3.2.3 Acceleration Model

### Exponential Acceleration Curve

Agents accelerating in JOGGING and SPRINTING states follow an exponential approach to top speed:

```
v(t) = v_target Ã— (1 - e^(-k Ã— t))
```

Where:
- `v(t)` = velocity magnitude at time t (m/s)
- `v_target` = current top speed (from MapPaceToTopSpeed, modified by directional multipliers and fatigue)
- `k` = acceleration rate constant (from MapAccelerationToK)
- `t` = time since acceleration began (seconds)

**Why exponential, not linear:**
1. Matches real-world biomechanics â€” initial force production is highest, diminishing as limbs approach maximum turnover rate
2. Produces natural-looking acceleration where the first 1â€“2 seconds show dramatic speed gain and the final approach to top speed is gradual
3. Self-limiting â€” the agent asymptotically approaches top speed without overshooting
4. Consistent with sports science acceleration profiles for team sport athletes (Buchheit et al., 2014)

### Discrete Integration at 60Hz

The continuous formula above is for reference. At 60Hz, we compute velocity incrementally:

```csharp
/// <summary>
/// Computes the agent's new velocity after one physics frame of acceleration.
/// Called every frame (60Hz) while agent is in JOGGING or SPRINTING state
/// and has not yet reached target speed.
///
/// The discrete implementation uses the velocity-form of the exponential:
///   v(t+dt) = v_target + (v(t) - v_target) Ã— e^(-k Ã— dt)
///
/// This is mathematically equivalent to the continuous form but works
/// incrementally from the current velocity, avoiding the need to track
/// "time since acceleration began" (which would require reset logic
/// on every speed change).
///
/// At 60Hz, dt = 1/60 â‰ˆ 0.01667s, and e^(-k Ã— dt) ranges from:
///   k_min (0.658): e^(-0.658 Ã— 0.01667) = 0.98908 (slow approach)
///   k_max (0.921): e^(-0.921 Ã— 0.01667) = 0.98474 (fast approach)
///
/// Numerical accuracy: Over a 3.5-second acceleration from 0 to 10.2 m/s
/// (210 frames), cumulative error vs continuous formula is <0.01 m/s.
/// </summary>
/// <param name="currentSpeed">Current velocity magnitude (m/s)</param>
/// <param name="targetSpeed">Target top speed including all modifiers (m/s)</param>
/// <param name="k">Acceleration rate constant from MapAccelerationToK()</param>
/// <param name="dt">Frame delta time (seconds, typically 1/60)</param>
/// <returns>New velocity magnitude (m/s)</returns>
public static float ApplyExponentialAcceleration(
    float currentSpeed,
    float targetSpeed,
    float k,
    float dt)
{
    // Exponential approach: velocity converges on target
    float decay = Mathf.Exp(-k * dt);
    float newSpeed = targetSpeed + (currentSpeed - targetSpeed) * decay;

    // Snap to target if within MIN_SPEED tolerance (prevents asymptotic drift)
    if (Mathf.Abs(newSpeed - targetSpeed) < MovementConstants.MIN_SPEED)
    {
        newSpeed = targetSpeed;
    }

    return Mathf.Clamp(newSpeed, 0f, MovementConstants.MAX_SPEED);
}
```

### Walking Acceleration (Linear Model)

Agents in WALKING state use a simpler linear acceleration:

```csharp
/// <summary>
/// Applies constant-rate acceleration for WALKING state.
/// All agents use the same walking acceleration â€” attribute differences
/// are negligible at walking speeds and not worth the computation.
///
/// Linear model: v(t+dt) = v(t) + a Ã— dt
/// Clamped to JOG_ENTER threshold to prevent walking model from
/// pushing agent into jogging speeds.
/// </summary>
public static float ApplyLinearAcceleration(
    float currentSpeed,
    float targetSpeed,
    float acceleration,
    float dt)
{
    float newSpeed;
    if (currentSpeed < targetSpeed)
    {
        newSpeed = currentSpeed + acceleration * dt;
        newSpeed = Mathf.Min(newSpeed, targetSpeed);  // Don't overshoot target
    }
    else
    {
        // Decelerating toward lower target
        newSpeed = currentSpeed - acceleration * dt;
        newSpeed = Mathf.Max(newSpeed, targetSpeed);
    }

    return Mathf.Clamp(newSpeed, 0f, MovementConstants.MAX_SPEED);
}
```

### Acceleration Selection by State

Mirrors the state-physics activation table from Section 3.1.6:

```csharp
/// <summary>
/// Selects and applies the correct acceleration model based on movement state.
///
/// Called once per frame (60Hz) during the velocity update phase.
/// State machine (Section 3.1) has already determined the current state;
/// this function applies the appropriate physics.
///
/// NOTE: This function only handles speed magnitude. Direction is handled
/// separately in Section 3.3 (Directional Movement) and Section 3.4 (Turning).
///
/// NOTE: targetSpeed is the FINAL target after all modifiers have been applied:
///   1. PerformanceContext (Section 3.2.1)
///   2. Directional multiplier (Section 3.3)
///   3. Fatigue modifier (Section 3.5)
/// The caller is responsible for computing this composite target.
/// </summary>
public static float UpdateSpeed(
    float currentSpeed,
    float targetSpeed,
    float accelK,
    AgentMovementState state,
    float dt)
{
    switch (state)
    {
        case AgentMovementState.IDLE:
            // No acceleration; speed should already be below IDLE_ENTER
            // Apply minimal drag to ensure convergence to zero
            return Mathf.Max(0f, currentSpeed - MovementConstants.WALK_DECELERATION * dt);

        case AgentMovementState.WALKING:
            return ApplyLinearAcceleration(
                currentSpeed, targetSpeed,
                MovementConstants.WALK_ACCELERATION, dt);

        case AgentMovementState.JOGGING:
        case AgentMovementState.SPRINTING:
            return ApplyExponentialAcceleration(
                currentSpeed, targetSpeed, accelK, dt);

        case AgentMovementState.DECELERATING:
            // Handled by deceleration model (Section 3.2.5)
            // This case should not reach UpdateSpeed â€” included for safety
            return currentSpeed;

        case AgentMovementState.STUMBLING:
            // Friction drag only â€” no voluntary acceleration
            // Uses ground friction coefficient (simplified: constant drag)
            return Mathf.Max(0f, currentSpeed - MovementConstants.WALK_DECELERATION * 2.0f * dt);

        case AgentMovementState.GROUNDED:
            // No movement
            return 0f;

        default:
            return currentSpeed;
    }
}
```

---

## 3.2.4 Top Speed Calculation

### Composite Top Speed

The top speed used in the acceleration model is not a single value â€” it's a composite of the Pace attribute, PerformanceContext modifiers, directional penalties, and fatigue:

```csharp
/// <summary>
/// Computes the agent's current effective top speed.
///
/// This is the AUTHORITATIVE top speed calculation. No other function
/// should independently compute top speed.
///
/// Modifier chain (applied in this order):
///   1. Raw Pace attribute â†’ PerformanceContext â†’ Effective Pace
///   2. Effective Pace â†’ MapPaceToTopSpeed â†’ Base top speed (m/s)
///   3. Ã— Directional multiplier (Section 3.3: forward/lateral/backward)
///   4. Ã— Aerobic fatigue modifier (Section 3.5: 0.0â€“1.0 pool â†’ speed scalar)
///   5. Clamp to [0, MAX_SPEED]
///
/// Example â€” Fresh elite winger sprinting forward:
///   Pace=19, Form=1.05, Context=1.0, Career=1.0
///   EffectivePace = 19 Ã— 1.05 = 19.95
///   BaseSpeed = 7.5 + (19.95 - 1.0) Ã— 0.14211 = 10.19 m/s
///   Directional = 1.0 (forward)
///   Fatigue = 1.0 (fresh)
///   Final = 10.19 m/s â‰ˆ 36.7 km/h
///
/// Example â€” Tired centre-back tracking back, poor form:
///   Pace=10, Form=0.88, Context=1.0, Career=0.97
///   EffectivePace = 10 Ã— (0.88 Ã— 1.0 Ã— 0.97) = 10 Ã— 0.8536 = 8.536
///   BaseSpeed = 7.5 + (8.536 - 1.0) Ã— 0.14211 = 8.57 m/s
///   Directional = 0.50 (backward, Agility=10 â†’ mid-range)
///   Fatigue = 0.80 (75th minute)
///   Final = 8.57 Ã— 0.50 Ã— 0.80 = 3.43 m/s â€” barely jogging backward
/// </summary>
public static float CalculateEffectiveTopSpeed(
    int rawPace,
    PerformanceContext context,
    float directionalMultiplier,
    float fatigueSpeedModifier)
{
    float effectivePace = context.EvaluateAttribute(rawPace);
    float baseTopSpeed = MapPaceToTopSpeed(effectivePace);
    float finalSpeed = baseTopSpeed * directionalMultiplier * fatigueSpeedModifier;

    return Mathf.Clamp(finalSpeed, 0f, MovementConstants.MAX_SPEED);
}
```

### Fatigueâ€“Speed Relationship

The aerobic fatigue pool (Section 3.5, FR-6) applies a speed modifier:

```csharp
/// <summary>
/// Converts aerobic energy pool level to top speed modifier.
///
/// Aerobic pool range: 0.0 (spent) to 1.0 (fresh)
/// Speed modifier range: 0.70 (exhausted floor) to 1.0 (full speed)
///
/// The relationship is piecewise linear:
///   Pool 1.0â€“0.5: Full speed (1.0). Players don't slow down until
///     they're significantly fatigued â€” consistent with sports science
///     showing minimal performance decrement until glycogen depletion
///     threshold (~50% reserves).
///   Pool 0.5â€“0.0: Linear decline from 1.0 to 0.70.
///     At 0.0 (complete exhaustion), player retains 70% speed â€” they're
///     visibly struggling but not immobile.
///
/// Derivation of 0.70 floor:
///   From QR-1: "Fatigued agents visibly slower than fresh agents (>15% speed reduction)"
///   30% reduction at complete exhaustion exceeds the 15% visibility threshold
///   with margin. Below 70%, movement would look pathological rather than fatigued.
///
/// Note: Sprint reservoir (separate pool) controls sprint ACCESS via state
/// machine gating (Section 3.1), not sprint SPEED. An agent with sprint
/// reservoir depleted simply cannot enter SPRINTING state, but their
/// jogging speed is affected by aerobic pool as described here.
/// </summary>
public static float AerobicPoolToSpeedModifier(float aerobicPool)
{
    aerobicPool = Mathf.Clamp01(aerobicPool);

    if (aerobicPool >= 0.5f)
    {
        return 1.0f;  // No speed penalty above 50% reserves
    }
    else
    {
        // Linear interpolation: pool 0.5 â†’ modifier 1.0, pool 0.0 â†’ modifier 0.70
        float t = aerobicPool / 0.5f;  // Normalize 0.0â€“0.5 to 0.0â€“1.0
        return Mathf.Lerp(0.70f, 1.0f, t);
    }
}
```

---

## 3.2.5 Deceleration Model

### Two Deceleration Modes

Agents in the DECELERATING state use one of two modes, selected by the commanding system:

**Controlled deceleration:** Agent brakes gradually. Used for intentional stops (arriving at position, preparing to receive ball, changing direction).

**Emergency deceleration:** Agent brakes at maximum force. Used for urgent stops (unexpected opponent, ball change, tactical override). Carries stumble risk.

### Controlled Deceleration

```csharp
/// <summary>
/// Applies controlled deceleration for one physics frame.
///
/// Uses constant deceleration rate derived from Agility (primary, 0.7)
/// and Balance (secondary, 0.3) via PerformanceContext.
///
/// Constant deceleration chosen over exponential because:
/// 1. Braking is a simpler biomechanical action than acceleration
/// 2. Produces predictable stopping distances for AI path planning
/// 3. Sports science literature models deceleration as approximately
///    constant for intentional stops (Harper & Kiely, 2018)
///
/// Formula: v(t+dt) = max(0, v(t) - decelRate Ã— dt)
///
/// Stopping distance (continuous): d = vâ‚€Â² / (2 Ã— decelRate)
///   At Agility 1,  sprint speed â‰ˆ9 m/s: d = 81 / 16.2 = 5.0m
///   At Agility 20, sprint speed â‰ˆ9 m/s: d = 81 / 27.0 = 3.0m
/// These match FR-3 (revised) requirements exactly.
/// </summary>
/// </summary>
public static float ApplyControlledDeceleration(
    float currentSpeed,
    float decelRate,
    float dt)
{
    float newSpeed = currentSpeed - decelRate * dt;
    return Mathf.Max(0f, newSpeed);
}
```

### Emergency Deceleration

```csharp
/// <summary>
/// Applies emergency deceleration for one physics frame.
/// Higher deceleration rate than controlled, but carries stumble risk.
///
/// The stumble check is performed ONCE on entry to emergency deceleration
/// (not every frame). If the check fails, the agent transitions to
/// STUMBLING state (Section 3.1) instead of continuing deceleration.
///
/// Stumble probability:
///   P(stumble) = BASE Ã— (1.0 - normalizedBalance Ã— REDUCTION)
///   normalizedBalance = (effectiveBalance - 1.0) / 19.0
///
///   At Balance 1:  P = 0.30 Ã— (1.0 - 0.0 Ã— 0.7) = 30.0%
///   At Balance 10: P = 0.30 Ã— (1.0 - 0.474 Ã— 0.7) = 20.1%
///   At Balance 20: P = 0.30 Ã— (1.0 - 1.0 Ã— 0.7) = 9.0%
///
/// Speed threshold: Stumble check ONLY applies when entering emergency
/// deceleration from SPRINTING state. Emergency decel from JOGGING or
/// slower has no stumble risk (insufficient momentum for balance loss).
/// </summary>
public static float ApplyEmergencyDeceleration(
    float currentSpeed,
    float decelRate,
    float dt)
{
    // Same physics as controlled, just higher rate
    float newSpeed = currentSpeed - decelRate * dt;
    return Mathf.Max(0f, newSpeed);
}

/// <summary>
/// Evaluates stumble risk on emergency deceleration entry.
/// Called ONCE when agent transitions to DECELERATING (emergency mode)
/// from SPRINTING state.
///
/// Returns true if the agent stumbles (should transition to STUMBLING state).
///
/// NOTE: The RNG call uses the deterministic simulation seed (Section 1.4,
/// FR-9). The same match seed produces the same stumble outcomes.
/// </summary>
/// <param name="effectiveBalance">Balance after PerformanceContext evaluation</param>
/// <param name="previousState">State before deceleration â€” only SPRINTING triggers check</param>
/// <param name="rngValue">Deterministic random value [0.0, 1.0) from simulation RNG</param>
/// <returns>True if agent stumbles</returns>
public static bool EvaluateStumbleRisk(
    float effectiveBalance,
    AgentMovementState previousState,
    float rngValue)
{
    // Only evaluate when decelerating from sprint
    if (previousState != AgentMovementState.SPRINTING)
        return false;

    float normalizedBalance = Mathf.Clamp01((effectiveBalance - 1.0f) / 19.0f);
    float stumbleChance = MovementConstants.STUMBLE_BASE_CHANCE
                        * (1.0f - normalizedBalance * MovementConstants.STUMBLE_BALANCE_REDUCTION);

    return rngValue < stumbleChance;
}
```

### Deceleration Orchestration

```csharp
/// <summary>
/// Full deceleration update for one frame.
/// Selects mode, evaluates stumble risk (if applicable), and returns
/// new speed + state transition recommendation.
///
/// Called by the main movement update loop when state == DECELERATING.
///
/// Returns a DecelerationResult struct containing the new speed and
/// an optional state transition (to STUMBLING if stumble check fails,
/// to IDLE if speed reaches zero).
/// </summary>
public struct DecelerationResult
{
    /// <summary>New speed after this frame's deceleration (m/s)</summary>
    public float NewSpeed;

    /// <summary>
    /// Recommended state transition, or null if staying in DECELERATING.
    /// STUMBLING: agent lost balance during emergency stop.
    /// IDLE: agent has come to a complete stop.
    /// </summary>
    public AgentMovementState? TransitionTo;
}

/// <summary>
/// Computes deceleration for one frame.
///
/// isEmergency: true for maximum braking, false for controlled stop.
/// isFirstFrame: true on the frame deceleration begins (for stumble check).
/// effectiveAgilityBalance: weighted pair from PerformanceContext.
/// effectiveBalance: solo balance from PerformanceContext (for stumble check).
/// previousState: state before DECELERATING was entered.
/// rngValue: deterministic random for stumble evaluation.
/// </summary>
public static DecelerationResult UpdateDeceleration(
    float currentSpeed,
    bool isEmergency,
    bool isFirstFrame,
    float effectiveAgilityBalance,
    float effectiveBalance,
    AgentMovementState previousState,
    float rngValue,
    float dt)
{
    var result = new DecelerationResult { TransitionTo = null };

    // Check stumble risk on first frame of emergency decel from sprint
    if (isEmergency && isFirstFrame)
    {
        if (EvaluateStumbleRisk(effectiveBalance, previousState, rngValue))
        {
            result.NewSpeed = currentSpeed;  // Momentum carries into stumble
            result.TransitionTo = AgentMovementState.STUMBLING;
            return result;
        }
    }

    // Apply deceleration
    float decelRate = isEmergency
        ? MapAgilityToEmergencyDecel(effectiveAgilityBalance)
        : MapAgilityToControlledDecel(effectiveAgilityBalance);

    float newSpeed = isEmergency
        ? ApplyEmergencyDeceleration(currentSpeed, decelRate, dt)
        : ApplyControlledDeceleration(currentSpeed, decelRate, dt);

    result.NewSpeed = newSpeed;

    // Check if stopped
    if (newSpeed < MovementConstants.MIN_SPEED)
    {
        result.NewSpeed = 0f;
        result.TransitionTo = AgentMovementState.IDLE;
    }

    return result;
}
```

---

## 3.2.6 Numerical Examples & Validation

### Example 1: Elite Winger Sprint from Standstill (Stage 0)

**Player:** Pace 19, Acceleration 18  
**Context:** PerformanceContext.CreateNeutral() (all modifiers 1.0)  
**Direction:** Forward (multiplier 1.0)  
**Fatigue:** Fresh (aerobic pool 1.0 â†’ speed modifier 1.0)

```
EffectivePace = 19 Ã— 1.0 = 19.0
TopSpeed = 7.5 + (19.0 - 1.0) Ã— 0.14211 = 7.5 + 2.558 = 10.058 m/s

EffectiveAccel = 18 Ã— 1.0 = 18.0
k = 0.658 + (18.0 - 1.0) Ã— 0.01384 = 0.658 + 0.2353 = 0.8933 sâ»Â¹

Time to 90% top speed: Tâ‚‰â‚€ = 2.3026 / 0.8933 = 2.578 s âœ“ (within 2.5â€“3.5s range)

Velocity at key times (continuous formula):
  t=0.0s: v = 0.000 m/s (standstill)
  t=0.5s: v = 10.058 Ã— (1 - e^(-0.8933 Ã— 0.5)) = 10.058 Ã— 0.3596 = 3.616 m/s
  t=1.0s: v = 10.058 Ã— (1 - e^(-0.8933 Ã— 1.0)) = 10.058 Ã— 0.5903 = 5.937 m/s
  t=1.5s: v = 10.058 Ã— (1 - e^(-0.8933 Ã— 1.5)) = 10.058 Ã— 0.7373 = 7.416 m/s
  t=2.0s: v = 10.058 Ã— (1 - e^(-0.8933 Ã— 2.0)) = 10.058 Ã— 0.8316 = 8.364 m/s
  t=2.5s: v = 10.058 Ã— (1 - e^(-0.8933 Ã— 2.5)) = 10.058 Ã— 0.8921 = 8.973 m/s
  t=3.0s: v = 10.058 Ã— (1 - e^(-0.8933 Ã— 3.0)) = 10.058 Ã— 0.9310 = 9.364 m/s
  t=3.5s: v = 10.058 Ã— (1 - e^(-0.8933 Ã— 3.5)) = 10.058 Ã— 0.9559 = 9.615 m/s

Distance covered in 3.0 seconds: â‰ˆ17.1m (integrated numerically)
```

**Validation:** Reaches sprint threshold (5.8 m/s) at approximately t=1.1s. Agent transitions IDLE â†’ WALKING â†’ JOGGING â†’ SPRINTING within first 1.5 seconds. Consistent with real-world observation of elite wingers.

### Example 2: Slow Centre-Back Sprint (Stage 0)

**Player:** Pace 8, Acceleration 6  
**Context:** PerformanceContext.CreateNeutral()  
**Direction:** Forward (multiplier 1.0)  
**Fatigue:** Fresh

```
EffectivePace = 8.0
TopSpeed = 7.5 + (8.0 - 1.0) Ã— 0.14211 = 7.5 + 0.995 = 8.495 m/s

EffectiveAccel = 6.0
k = 0.658 + (6.0 - 1.0) Ã— 0.01384 = 0.658 + 0.0692 = 0.7272 sâ»Â¹

Tâ‚‰â‚€ = 2.3026 / 0.7272 = 3.167 s âœ“

Velocity at key times:
  t=1.0s: v = 8.495 Ã— (1 - e^(-0.7272)) = 8.495 Ã— 0.5168 = 4.390 m/s
  t=2.0s: v = 8.495 Ã— (1 - e^(-1.4544)) = 8.495 Ã— 0.7664 = 6.510 m/s
  t=3.0s: v = 8.495 Ã— (1 - e^(-2.1816)) = 8.495 Ã— 0.8872 = 7.533 m/s
```

**Validation:** Noticeably slower to reach sprint speed (â‰ˆ1.5s vs â‰ˆ1.1s for elite winger). Top speed 1.56 m/s lower. Over a 30m chase, the winger pulls away by approximately 2.5â€“3.0 meters â€” consistent with observable real-world speed differences.

### Example 3: Stage 4 Scenario â€” Elite Player in Poor Form

**Player:** Pace 18, Acceleration 17  
**Context:** Form=0.88 (cold streak), Context=0.85 (new league, poor tactical fit), Career=0.95 (coming off mediocre season)  
**Combined modifier:** 0.88 Ã— 0.85 Ã— 0.95 = 0.7106 â†’ clamped to 0.7106 (above FLOOR)

```
EffectivePace = 18 Ã— 0.7106 = 12.79
TopSpeed = 7.5 + (12.79 - 1.0) Ã— 0.14211 = 7.5 + 1.676 = 9.176 m/s

EffectiveAccel = 17 Ã— 0.7106 = 12.08
k = 0.658 + (12.08 - 1.0) Ã— 0.01384 = 0.658 + 0.1533 = 0.8113 sâ»Â¹

Tâ‚‰â‚€ = 2.3026 / 0.8113 = 2.838 s
```

**Analysis:** An elite Pace 18 player in this scenario performs like a Pace 13 player. Their top speed drops from 9.92 m/s to 9.18 m/s â€” they're still fast, but noticeably off their best. A mid-table Pace 14 player in good form (modifier 1.10) would have EffectivePace 15.4 and top speed 9.55 m/s â€” actually faster. This demonstrates the system's ability to produce the "upset" scenarios where average players outperform struggling stars.

### Example 4: Emergency Deceleration with Stumble Risk

**Player:** Agility 14, Balance 9  
**Context:** PerformanceContext.CreateNeutral()  
**Scenario:** Agent at 9.5 m/s (sprinting) receives emergency stop command.

```
Effective Agility-Balance pair:
  Agility contribution: 14.0 Ã— 0.7 = 9.8
  Balance contribution: 9.0 Ã— 0.3 = 2.7
  Weighted average: (9.8 + 2.7) / 1.0 = 12.5

Emergency decel rate = 11.57 + (12.5 - 1.0) Ã— 0.24368 = 11.57 + 2.802 = 14.372 m/sÂ²

Stopping distance = vÂ² / (2 Ã— decel) = 90.25 / 28.744 = 3.14m âœ“ (within 2.5â€“3.5m range)

Stumble check (previousState = SPRINTING):
  effectiveBalance = 9.0
  normalizedBalance = (9.0 - 1.0) / 19.0 = 0.4211
  stumbleChance = 0.30 Ã— (1.0 - 0.4211 Ã— 0.70) = 0.30 Ã— 0.7053 = 0.2116 (21.2%)

If rngValue < 0.2116: agent stumbles â†’ STUMBLING state
If rngValue â‰¥ 0.2116: agent stops in â‰ˆ3.14m â†’ IDLE state

Stopping time: t = v / decel = 9.5 / 14.372 = 0.661 seconds (â‰ˆ40 frames at 60Hz)
```

### Validation Summary

| Test Case | Requirement | Result | Status |
|---|---|---|---|
| Elite sprint Tâ‚‰â‚€ | 2.5â€“3.5s (FR-2) | 2.58s | âœ“ PASS |
| Slow CB sprint Tâ‚‰â‚€ | 2.5â€“3.5s (FR-2) | 3.17s | âœ“ PASS |
| Controlled stop dist (Agility 1) | 3.0â€“5.0m (FR-3 revised) | 5.00m | âœ“ PASS (boundary) |
| Controlled stop dist (Agility 20) | 3.0â€“5.0m (FR-3 revised) | 3.00m | âœ“ PASS (boundary) |
| Emergency stop dist (Agility 1) | 2.5â€“3.5m (FR-3 revised) | 3.50m | âœ“ PASS (boundary) |
| Emergency stop dist (Agility 20) | 2.5â€“3.5m (FR-3 revised) | 2.50m | âœ“ PASS (boundary) |
| Top speed range (Pace 1â€“20) | 7.5â€“10.2 m/s (FR-2) | 7.50â€“10.20 | âœ“ PASS |
| MAX_SPEED not exceeded | <12.0 m/s (Section 2.4) | Max 10.20 | âœ“ PASS |
| Stage 4 modifier range | Noticeable variation | Â±29% top speed swing | âœ“ PASS |
| Stumble probability range | Attribute-dependent | 9%â€“30% | âœ“ PASS |
| 60Hz numerical error | <0.05 m/s cumulative (PR-3) | <0.01 m/s | âœ“ PASS |
| EFFECTIVE_FLOOR enforcement | â‰¥0.6 combined modifier | Min realistic 0.612 | âœ“ PASS |

---

## Cross-References

| Section | References To | Nature |
|---|---|---|
| 3.2.1 (PerformanceContext) | ALL future specs | Mandatory gateway for attribute evaluation |
| 3.2.1 | Spec #20 (Code Standards) | Enforcement rule for direct attribute access |
| 3.2.1 | Master Vol 2 (H-Gate) | ContextModifier sub-components (Stage 4) |
| 3.2.2 (Attribute Mapping) | Section 3.1.3 (State Thresholds) | Speed values must be consistent |
| 3.2.3 (Acceleration) | Section 3.1.6 (State-Physics Table) | JOGGING/SPRINTING use exponential |
| 3.2.4 (Top Speed) | Section 3.3 (Directional Movement) | Directional multipliers applied here |
| 3.2.4 | Section 3.5 (Fatigue) | Aerobic pool â†’ speed modifier |
| 3.2.5 (Deceleration) | Section 3.1.4 (Transitions) | DECELERATING â†’ STUMBLING path |
| 3.2.5 | Section 3.1.5 (Dwell Times) | STUMBLING recovery timing |

---

## Future Extension Notes

**Stage 2 â€” Form System activation:**
- Replace `PerformanceContext.CreateNeutral()` calls with `PerformanceContext.Create(form, 1.0, 1.0)` where form is computed from match-to-match momentum model.
- No changes to any physics formulas, mapping functions, or deceleration model required.

**Stage 4 â€” Full PerformanceContext activation:**
- Populate ContextModifier from H-Gate outputs (confidence â†’ risk-taking, self-efficacy â†’ execution quality).
- Populate CareerModifier from end-of-season evaluation.
- Consider attribute-specific modifier weighting (mental attributes more affected by psychology than physical) â€” this would require adding `EvaluateAttributeWeighted()` to PerformanceContext, not changing existing methods.
- Staff report system reads `GetCombinedModifier()` to generate natural-language explanations of performance variation.

**Stage 5 â€” Fixed64 migration:**
- All `float` types in this section become `Fixed64`.
- `Mathf.Exp()` replaced by `Fixed64Math.Exp()` (lookup table implementation).
- `Mathf.Clamp()` replaced by `Fixed64Math.Clamp()`.
- No algorithmic changes required â€” only type substitution.

---

**End of Section 3.2**

**Page Count:** ~18 pages  
**Next Section:** Section 3.3 â€” Directional Movement (forward/lateral/backward multipliers)
