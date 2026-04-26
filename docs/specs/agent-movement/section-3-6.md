# Agent Movement Specification Ã¢â‚¬â€ Section 3.6: Edge Cases & Error Handling

**Purpose:** Consolidates all edge case handling, error recovery procedures, and validation scenarios for the Agent Movement System. This is the authoritative reference for how the system behaves at numerical boundaries, during invalid state conditions, and when physical constraints are violated. All recovery logic defined here must be implemented to satisfy QR-2 (Stability) from Section 2.3.

**Created:** February 11, 2026, 12:30 AM PST  
**Updated:** February 11, 2026, 2:45 PM PST  
**Version:** 1.1  
**Status:** Draft (Revised)  
**Dependencies:** Section 2.4 (Failure Modes), Section 3.1 (State Machine), Section 3.2 (Locomotion), Section 3.3 (Directional Movement), Section 3.4 (Turning), Section 3.5 (Data Structures), Ball Physics Spec #1 (pattern reference)

---

## v1.1 Changelog

**CRITICAL FIXES:**

1. **Removed hardcoded pitch dimensions from `ApplySafetyClamps()`** (Issue #1): Position bounds clamping was using `const float PITCH_LENGTH = 105.0f` and `PITCH_WIDTH = 68.0f`, contradicting the `PitchConfiguration` struct defined in 3.6.5.1. Position clamping now delegates entirely to `EnforcePitchBoundaries()` â€” `ApplySafetyClamps()` no longer contains any position bounds logic, eliminating the duplication.

2. **Removed duplicate ground penetration fix** (Issue #2): Ground penetration correction appeared in both `ApplySafetyClamps()` (3.6.2.3) and standalone in 3.6.5.3. Removed from `ApplySafetyClamps()` â€” authoritative location is now `EnforcePitchBoundaries()` which handles all position/boundary corrections. 3.6.5.3 retained as documentation reference only, with note pointing to `EnforcePitchBoundaries()`.

3. **Added coordinate system consistency note** (Issue #3): `PitchConfiguration` correctly uses corner-origin (0,0,0) matching Ball Physics Spec #1. Section 3.5 (Data Structures) has a conflicting comment stating "Origin at pitch center" â€” added cross-reference warning and flagged Section 3.5 for correction in its next revision.

**MAJOR FIXES:**

4. **Implemented full command priority system** (Issue #4): `GetPriority()` now returns distinct values for all 4 priority levels using `CommandSource` enum on `MovementCommand`. Added stable sort tiebreaker (most recent command wins at equal priority).

5. **OscillationGuard struct safety warning** (Issue #5): Added XML doc warning about mutable struct semantics. All call sites must use `ref`. Added `[StructLayout]` advisory.

6. **Improved DegradedModeState error decay** (Issue #6): `ConsecutiveErrors` now decays over time (halved every 2.0s of clean operation) instead of hard-resetting to 0 on first successful exit. Prevents perpetual recovery cycling.

7. **Clarified direction reversal threshold** (Issue #7): Renamed and documented. Threshold `dot < -0.5f` is intentionally ~120Â° (not 180Â°) â€” forces deceleration for any near-reversal, not just exact 180Â°. Comment corrected from "180-degree" to "near-reversal (>120Â°)."

**MODERATE FIXES:**

8. **Added LastValidPosition initialization guarantee** (Issue #8): Agent constructor and factory methods must initialize `LastValidPosition` to formation position. Added `InitializeLastValidState()` method and warning about uninitialized recovery.

9. **Fixed UT-EDGE-005 test description ambiguity** (Issue #9): Clarified that 6 transitions succeed, 7th attempt is blocked.

10. **Fixed TimeToSpeed comment/code mismatch** (Issue #10): Comment said "90% top speed time" but code used 0.99. Comment corrected to "99% of top speed (practical asymptotic limit)."

11. **Added DegradedModeState escalation test** (Issue #11): New test UT-EDGE-017 verifies 4+ consecutive errors trigger 1.0s recovery.

12. **Added fatigue/stamina edge case notes** (Issue #12): New subsection 3.6.4.4 documents stamina exhaustion edge cases and references Section 3.1 forbidden transitions.

13. **Added MovementCommand.Maintain() factory method reference** (Issue #13): `Maintain()` was used in `ResolveConflicts()` but never defined. Added definition note referencing Section 3.5 where it must be added, and provided inline specification.

14. **Fixed acceptance criteria test count** (Issue #14): Updated from "16 unit tests" to "18 unit tests" (original 16 + UT-EDGE-009b + new UT-EDGE-017).

15. **Noted UTF encoding artifacts** (Issue #15): Cosmetic encoding issues flagged for cleanup in final export pass.

---

## Table of Contents

- [3.6.1 Design Philosophy](#361-design-philosophy)
- [3.6.2 Numerical Edge Cases](#362-numerical-edge-cases)
- [3.6.3 State Machine Edge Cases](#363-state-machine-edge-cases)
- [3.6.4 Attribute Boundary Conditions](#364-attribute-boundary-conditions)
- [3.6.5 Physical Boundary Violations](#365-physical-boundary-violations)
- [3.6.6 Error Recovery Procedures](#366-error-recovery-procedures)
- [3.6.7 Validation Scenarios & Expected Outcomes](#367-validation-scenarios--expected-outcomes)

---

## 3.6.1 Design Philosophy

### Guiding Principles

The Agent Movement System must never crash, produce garbage output, or corrupt simulation state. Edge case handling follows these principles:

1. **Fail-safe, not fail-fast.** The simulation must continue even when individual agents encounter invalid states. A single corrupted agent should not halt the match.

2. **Detect Ã¢â€ â€™ Recover Ã¢â€ â€™ Log.** Every failure follows this sequence: detect the anomaly, apply recovery procedure, log for debugging. Recovery takes priority over logging.

3. **Preserve determinism.** Recovery procedures must be deterministic. Given identical inputs (including RNG state), the recovery path must produce identical outputs for replay and multiplayer sync.

4. **Conservative clamping.** When values exceed safe limits, clamp to the nearest valid value rather than resetting to zero or default. This preserves behavioral continuity.

5. **Graceful degradation.** An agent in recovery mode should still appear on screen and respond to basic commands, even if advanced behaviors are disabled.

6. **Mirror Ball Physics patterns.** Where applicable, use the same validation and recovery patterns established in Ball Physics Spec #1 for consistency.

### Scope

This section covers:
- **Numerical instability:** NaN, Infinity, extreme values, division by zero scenarios
- **State machine anomalies:** Oscillation, invalid transitions, conflicting commands
- **Attribute extremes:** Behavior at Attribute 1 and Attribute 20 boundaries
- **Physical violations:** Pitch boundary breaches, agent overlap, ground penetration
- **Command conflicts:** Simultaneous contradictory inputs, queue overflow

This section does NOT cover:
- Collision response physics (Spec #3)
- Ball interaction edge cases (Spec #11)
- Goalkeeper-specific edge cases (Spec #11)

---

## 3.6.2 Numerical Edge Cases

### 3.6.2.1 NaN and Infinity Detection

**Requirement (QR-2):** System SHALL NOT produce NaN or Infinity values under any input conditions.

```csharp
/// <summary>
/// Checks for NaN or Infinity in agent kinematic state.
/// Called EVERY frame at the start of ValidateAgentState().
/// 
/// Mirrors Ball Physics pattern: HasInvalidValues(BallState).
/// 
/// Checked values:
///   - Position.x, Position.y, Position.z
///   - Velocity.x, Velocity.y, Velocity.z
///   - FacingDirection.x, FacingDirection.y
///   - Speed (cached scalar)
///   - CurrentTurnRate
///   - LeanAngle
/// 
/// Returns true if ANY value is NaN or Infinity.
/// </summary>
public static bool HasInvalidValues(Agent agent)
{
    // Position check
    if (float.IsNaN(agent.Position.x) || float.IsInfinity(agent.Position.x) ||
        float.IsNaN(agent.Position.y) || float.IsInfinity(agent.Position.y) ||
        float.IsNaN(agent.Position.z) || float.IsInfinity(agent.Position.z))
    {
        return true;
    }
    
    // Velocity check
    if (float.IsNaN(agent.Velocity.x) || float.IsInfinity(agent.Velocity.x) ||
        float.IsNaN(agent.Velocity.y) || float.IsInfinity(agent.Velocity.y) ||
        float.IsNaN(agent.Velocity.z) || float.IsInfinity(agent.Velocity.z))
    {
        return true;
    }
    
    // Facing direction check
    if (float.IsNaN(agent.FacingDirection.x) || float.IsInfinity(agent.FacingDirection.x) ||
        float.IsNaN(agent.FacingDirection.y) || float.IsInfinity(agent.FacingDirection.y))
    {
        return true;
    }
    
    // Derived values check
    if (float.IsNaN(agent.Speed) || float.IsInfinity(agent.Speed) ||
        float.IsNaN(agent.CurrentTurnRate) || float.IsInfinity(agent.CurrentTurnRate) ||
        float.IsNaN(agent.LeanAngle) || float.IsInfinity(agent.LeanAngle))
    {
        return true;
    }
    
    return false;
}
```

**Recovery Procedure:**

```csharp
/// <summary>
/// Recovers agent from NaN/Infinity state.
/// Called immediately when HasInvalidValues() returns true.
/// 
/// Recovery steps:
///   1. Restore Position to LastValidPosition
///   2. Zero Velocity (cannot trust LastValidVelocity if NaN occurred)
///   3. Set FacingDirection to last valid or +Y if unavailable
///   4. Force state to IDLE
///   5. Reset derived values (Speed = 0, TurnRate = 0, LeanAngle = 0)
///   6. Log ERROR with full context
///   7. Emit telemetry event for debug analysis
/// </summary>
private void RecoverFromNaN(ref Agent agent, float matchTime)
{
    // Step 1: Restore position
    agent.Position = agent.LastValidPosition;
    
    // Step 2: Zero velocity (conservative Ã¢â‚¬â€ we don't trust any momentum)
    agent.Velocity = Vector3.zero;
    agent.InvalidateSpeedCache();
    
    // Step 3: Restore facing direction
    if (agent.LastValidFacingDirection.sqrMagnitude > 0.5f)
    {
        agent.FacingDirection = agent.LastValidFacingDirection.normalized;
    }
    else
    {
        agent.FacingDirection = Vector2.up; // Default: face +Y (toward away goal)
    }
    
    // Step 4: Force IDLE state
    agent.CurrentState = AgentMovementState.IDLE;
    agent.TimeInState = 0f;
    
    // Step 5: Reset derived values
    agent.CurrentTurnRate = 0f;
    agent.LeanAngle = 0f;
    
    // Step 6: Log ERROR
    Debug.LogError($"[AgentMovement] Agent {agent.AgentID}: NaN/Infinity detected! " +
                   $"Recovered to ({agent.Position.x:F2}, {agent.Position.y:F2}). " +
                   $"Match time: {matchTime:F2}s");
    
    // Step 7: Telemetry event
    agent.EmitEvent(new MovementTelemetryEvent
    {
        Timestamp = matchTime,
        AgentID = agent.AgentID,
        EventType = TelemetryEventType.NAN_RECOVERY,
        MessageCode = MessageCode.RECOVERED_FROM_INVALID_STATE,
        Details = "Position and velocity reset, state forced to IDLE"
    });
}
```

### 3.6.2.2 Zero Velocity Edge Cases

When velocity magnitude approaches zero, several formulas require special handling to avoid division by zero or undefined behavior.

**Edge Case 1: Direction from zero velocity**

```csharp
/// <summary>
/// Safely normalizes velocity to get movement direction.
/// Returns agent's facing direction if velocity is near-zero.
/// 
/// Threshold: MIN_VELOCITY_MAGNITUDE = 0.001 m/s
/// 
/// Used by: Section 3.3 facing mode, Section 3.4 turn calculations.
/// </summary>
public static Vector3 GetMovementDirection(Agent agent)
{
    float speedSq = agent.Velocity.sqrMagnitude;
    
    if (speedSq < MovementConstants.MIN_VELOCITY_MAGNITUDE * 
                   MovementConstants.MIN_VELOCITY_MAGNITUDE)
    {
        // Near-zero velocity: use facing direction as movement direction
        return new Vector3(agent.FacingDirection.x, agent.FacingDirection.y, 0f);
    }
    
    return agent.Velocity / Mathf.Sqrt(speedSq);
}
```

**Edge Case 2: Turn radius at zero speed**

```csharp
/// <summary>
/// Calculates minimum turn radius, handling zero speed case.
/// At zero speed, turn radius is undefined (can turn on spot).
/// Returns 0.0 to indicate "no radius constraint".
/// 
/// Formula: r_min = v / Ãâ€°_rad
/// 
/// Edge case: v = 0 Ã¢â€ â€™ r_min = 0 (can spin on spot)
/// </summary>
public static float CalculateMinTurnRadius(float speed, float maxTurnRateDegrees)
{
    if (speed < MovementConstants.MIN_VELOCITY_MAGNITUDE)
    {
        return 0f; // No radius constraint at zero speed
    }
    
    if (maxTurnRateDegrees < 1.0f)
    {
        return float.MaxValue; // Effectively infinite radius (can't turn)
    }
    
    float omegaRad = maxTurnRateDegrees * Mathf.Deg2Rad;
    return speed / omegaRad;
}
```

**Edge Case 3: Acceleration at zero velocity**

The exponential acceleration model `a(t) = a_max Ãƒâ€” (1 - e^(-kÃƒâ€”t))` is well-defined at t=0 (returns 0). No special handling required.

However, the time-to-speed inversion can encounter edge cases:

```csharp
/// <summary>
/// Calculates time to reach target speed from current speed.
/// 
/// Edge cases handled:
///   - targetSpeed <= currentSpeed: returns 0 (already there)
///   - targetSpeed >= topSpeed: clamps to 99% of top speed (practical asymptotic limit)
///   - currentSpeed < 0: clamps to 0
/// </summary>
public static float TimeToSpeed(float currentSpeed, float targetSpeed, 
                                 float topSpeed, float accelK)
{
    currentSpeed = Mathf.Max(0f, currentSpeed);
    
    if (targetSpeed <= currentSpeed)
        return 0f;
    
    // Clamp target to achievable max (99% of top speed is practical limit)
    targetSpeed = Mathf.Min(targetSpeed, topSpeed * 0.99f);
    
    // Invert v(t) = v_max Ãƒâ€” (1 - e^(-kÃƒâ€”t))
    // t = -ln(1 - v/v_max) / k
    float fraction = targetSpeed / topSpeed;
    
    if (fraction >= 1.0f)
        return float.MaxValue; // Asymptotic, never exactly reaches top speed
    
    float t = -Mathf.Log(1f - fraction) / accelK;
    return t;
}
```

### 3.6.2.3 Boundary Value Clamping

All kinematic values are clamped to defined safety limits every frame. This prevents runaway values from corrupting the simulation.

```csharp
/// <summary>
/// Applies safety clamps to all agent kinematic values.
/// Called every frame AFTER physics update, BEFORE state machine evaluation.
/// 
/// Clamp order matters:
///   1. Velocity magnitude (preserves direction)
///   2. Turn rate
///   3. Lean angle
/// 
/// NOTE (v1.1): Position bounds and ground penetration are handled by
/// EnforcePitchBoundaries() which uses the injected PitchConfiguration.
/// They are NOT duplicated here. Call order is:
///   ApplySafetyClamps() â†’ EnforcePitchBoundaries() â†’ State machine
/// </summary>
public static void ApplySafetyClamps(ref Agent agent)
{
    // ================================================================
    // VELOCITY MAGNITUDE CLAMP
    // ================================================================
    float speed = agent.Speed;
    if (speed > MovementConstants.MAX_SPEED)
    {
        agent.Velocity = agent.Velocity.normalized * MovementConstants.MAX_SPEED;
        agent.InvalidateSpeedCache();
        
        Debug.LogWarning($"[AgentMovement] Agent {agent.AgentID}: " +
                        $"Velocity clamped from {speed:F2} m/s to {MovementConstants.MAX_SPEED} m/s");
    }
    
    // Zero out near-zero velocity to prevent drift
    if (agent.Speed < MovementConstants.MIN_VELOCITY_MAGNITUDE)
    {
        agent.Velocity = Vector3.zero;
        agent.InvalidateSpeedCache();
    }
    
    // ================================================================
    // TURN RATE CLAMP
    // ================================================================
    agent.CurrentTurnRate = Mathf.Clamp(agent.CurrentTurnRate, 
                                         0f, 
                                         MovementConstants.MAX_TURN_RATE);
    
    // ================================================================
    // LEAN ANGLE CLAMP
    // ================================================================
    agent.LeanAngle = Mathf.Clamp(agent.LeanAngle, 
                                   -MovementConstants.MAX_LEAN_ANGLE, 
                                   MovementConstants.MAX_LEAN_ANGLE);
}
```

### 3.6.2.4 Division by Zero Prevention

All formulas that involve division are guarded against zero denominators.

**Affected formulas and guards:**

| Formula | Denominator | Guard |
|---------|-------------|-------|
| Turn rate: `Ãâ€° = base / (1 + kÃƒâ€”v)` | `1 + kÃƒâ€”v` | Always Ã¢â€°Â¥ 1.0 (k, v Ã¢â€°Â¥ 0) |
| Turn radius: `r = v / Ãâ€°` | `Ãâ€°` | Return `float.MaxValue` if Ãâ€° < 1.0 |
| Normalized direction: `v / |v|` | `|v|` | Return facing direction if `|v|` < 0.001 |
| Attribute scaling: `(attr - 1) / 19` | 19 | Constant, no guard needed |
| Time inverse: `-ln(1-x) / k` | `k`, `1-x` | Clamp `x` < 0.99, ensure k > 0.1 |

---

## 3.6.3 State Machine Edge Cases

### 3.6.3.1 Oscillation Detection and Prevention

**Requirement (Section 2.4):** State Machine Oscillation detection at >6 state changes per second, with 500ms cooldown lock.

```csharp
/// <summary>
/// Oscillation guard implementation.
/// Tracks state transition timestamps in a ring buffer.
/// If transitions exceed threshold, locks state machine for cooldown period.
/// 
/// Constants:
///   MAX_STATE_CHANGES_PER_SECOND = 6
///   STATE_LOCK_DURATION = 0.5f seconds
/// 
/// Call TryRecordTransition() BEFORE applying any state change.
/// Returns false if transition should be blocked (oscillation detected).
/// 
/// WARNING (v1.1): This is a MUTABLE STRUCT. In C#, passing a struct by value
/// creates a copy â€” mutations on the copy are silently lost. ALL call sites
/// MUST use 'ref' when passing or storing this struct:
///   - Field: stored directly on Agent class (not boxed)
///   - Parameter: passed as 'ref OscillationGuard guard'
///   - NEVER: pass as interface, box, or store in non-ref local
/// Failure to use ref will cause the ring buffer to lose state between frames.
/// </summary>
public struct OscillationGuard
{
    private const int BUFFER_SIZE = 8;
    private float[] _transitionTimes;
    private int _writeIndex;
    private bool _isLocked;
    private float _lockUntilTime;
    
    /// <summary>
    /// Initializes the oscillation guard. Must be called before use.
    /// </summary>
    public void Initialize()
    {
        _transitionTimes = new float[BUFFER_SIZE];
        for (int i = 0; i < BUFFER_SIZE; i++)
        {
            _transitionTimes[i] = float.MinValue;
        }
        _writeIndex = 0;
        _isLocked = false;
        _lockUntilTime = 0f;
    }
    
    /// <summary>
    /// Attempts to record a state transition.
    /// Returns true if transition is allowed, false if blocked by oscillation lock.
    /// 
    /// Call sequence:
    ///   if (oscillationGuard.TryRecordTransition(currentTime, agentId)) {
    ///       // Apply state transition
    ///   } else {
    ///       // Transition blocked, maintain current state
    ///   }
    /// </summary>
    public bool TryRecordTransition(float currentTime, int agentId)
    {
        // Check if currently locked
        if (_isLocked)
        {
            if (currentTime < _lockUntilTime)
            {
                return false; // Still locked, block transition
            }
            else
            {
                // Lock expired, resume normal operation
                _isLocked = false;
            }
        }
        
        // Count transitions in the last 1.0 second
        int recentTransitions = 0;
        float windowStart = currentTime - 1.0f;
        
        for (int i = 0; i < BUFFER_SIZE; i++)
        {
            if (_transitionTimes[i] > windowStart)
            {
                recentTransitions++;
            }
        }
        
        // Check for oscillation
        if (recentTransitions >= MovementConstants.MAX_STATE_CHANGES_PER_SECOND)
        {
            _isLocked = true;
            _lockUntilTime = currentTime + MovementConstants.STATE_LOCK_DURATION;
            
            Debug.LogWarning($"[AgentMovement] Agent {agentId}: Oscillation detected! " +
                           $"{recentTransitions} transitions/sec. Locked for {MovementConstants.STATE_LOCK_DURATION}s");
            
            return false; // Block this transition
        }
        
        // Record this transition
        _transitionTimes[_writeIndex] = currentTime;
        _writeIndex = (_writeIndex + 1) % BUFFER_SIZE;
        
        return true; // Transition allowed
    }
    
    /// <summary>
    /// Returns true if the guard is currently in lockdown.
    /// </summary>
    public bool IsLocked => _isLocked;
}
```

### 3.6.3.2 Rapid Transition Scenarios

Even without oscillation, rapid transitions can indicate problematic command sequences. These scenarios are handled explicitly:

**Scenario 1: Immediate sprint request after stumble**

```csharp
// Forbidden transition: STUMBLING Ã¢â€ â€™ SPRINTING
// Section 3.1.4 explicitly forbids this
if (currentState == AgentMovementState.STUMBLING && 
    requestedState == AgentMovementState.SPRINTING)
{
    // Reject: must stabilize before sprinting
    return currentState; // Stay in STUMBLING
}
```

**Scenario 2: Direction reversal at sprint speed**

```csharp
/// <summary>
/// Handles near-reversal direction changes at high speed.
/// Agent cannot instantly reverse; must decelerate first.
/// 
/// Detection: dot(currentVelocity, targetDirection) < -0.5 AND speed > JOG_ENTER
///   Note: dot < -0.5 corresponds to >120Â° angle, NOT exactly 180Â°.
///   This is intentional â€” forces deceleration for any sharp reversal,
///   not just exact U-turns. A player cutting at 130Â° while jogging
///   must still slow down before changing direction.
/// 
/// Response: Force DECELERATING state, ignore target direction until speed < WALK threshold
/// </summary>
public static AgentMovementState HandleDirectionReversal(
    Agent agent, 
    Vector3 targetDirection)
{
    if (agent.Speed < MovementConstants.JOG_ENTER_THRESHOLD)
    {
        return agent.CurrentState; // Low speed, allow any direction change
    }
    
    Vector3 currentDir = GetMovementDirection(agent);
    float dot = Vector3.Dot(currentDir, targetDirection.normalized);
    
    if (dot < -0.5f) // More than ~120 degree reversal
    {
        // Force deceleration before allowing reversal
        return AgentMovementState.DECELERATING;
    }
    
    return agent.CurrentState;
}
```

**Scenario 3: State skip attempts**

```csharp
// Forbidden: IDLE Ã¢â€ â€™ SPRINTING (must pass through WALKING Ã¢â€ â€™ JOGGING)
// Forbidden: IDLE Ã¢â€ â€™ JOGGING (must pass through WALKING)
// Forbidden: WALKING Ã¢â€ â€™ SPRINTING (must pass through JOGGING)

if (currentState == AgentMovementState.IDLE && 
    requestedState == AgentMovementState.SPRINTING)
{
    // Downgrade to WALKING; speed will naturally progress through states
    return AgentMovementState.WALKING;
}
```

### 3.6.3.3 Conflicting Command Handling

When multiple systems issue contradictory commands in the same frame, resolution follows a priority hierarchy:

**Command Priority (highest to lowest):**

1. **Safety overrides** (collision avoidance, emergency stop)
2. **Tactical AI commands** (positioning, marking)
3. **Ball interaction commands** (first touch, pass execution)
4. **Default behavior** (maintain current trajectory)

```csharp
/// <summary>
/// Resolves conflicting movement commands for a single frame.
/// 
/// Resolution rules:
///   - Higher priority command wins (based on CommandSource)
///   - Equal priority: most recent command wins (stable sort by timestamp)
///   - If command buffer exceeds capacity: drop oldest, warn
/// 
/// FIXED v1.1: Full 4-level priority implemented. v1.0 only distinguished
/// safety overrides vs. everything else. Now uses CommandSource enum for
/// proper priority resolution across all command types.
/// </summary>
public static MovementCommand ResolveConflicts(
    List<MovementCommand> pendingCommands,
    int agentId)
{
    if (pendingCommands.Count == 0)
    {
        return MovementCommand.Maintain(); // No command = maintain current
    }
    
    // Command queue overflow check
    const int MAX_COMMANDS_PER_FRAME = 4;
    if (pendingCommands.Count > MAX_COMMANDS_PER_FRAME)
    {
        Debug.LogWarning($"[AgentMovement] Agent {agentId}: Command queue overflow. " +
                        $"Dropping {pendingCommands.Count - MAX_COMMANDS_PER_FRAME} oldest commands.");
        
        // Keep only the most recent commands
        pendingCommands.RemoveRange(0, pendingCommands.Count - MAX_COMMANDS_PER_FRAME);
    }
    
    // Stable sort by priority descending, then by timestamp descending (most recent wins)
    // Using insertion sort for stability on small lists (max 4 elements)
    for (int i = 1; i < pendingCommands.Count; i++)
    {
        var key = pendingCommands[i];
        int j = i - 1;
        while (j >= 0 && ShouldSwap(pendingCommands[j], key))
        {
            pendingCommands[j + 1] = pendingCommands[j];
            j--;
        }
        pendingCommands[j + 1] = key;
    }
    
    // Return highest priority command
    return pendingCommands[0];
}

/// <summary>
/// Returns true if 'b' should be sorted before 'a' (b wins).
/// Higher priority wins. At equal priority, more recent timestamp wins.
/// </summary>
private static bool ShouldSwap(MovementCommand a, MovementCommand b)
{
    int priorityA = GetPriority(a);
    int priorityB = GetPriority(b);
    
    if (priorityA != priorityB)
        return priorityB > priorityA;
    
    // Equal priority: most recent command wins
    return b.Timestamp > a.Timestamp;
}

/// <summary>
/// Command priority levels.
/// Matches the 4-level hierarchy defined in Section 3.6.3.3.
/// 
/// NOTE: OverrideSafetyConstraints is a legacy flag that forces max priority.
/// Prefer using CommandSource.SAFETY_OVERRIDE for new code.
/// 
/// v1.1: Added CommandSource enum support. MovementCommand struct must add:
///   - CommandSource Source (enum field)
///   - float Timestamp (frame time when command was issued)
/// These fields must be added to Section 3.5 MovementCommand in its next revision.
/// </summary>
private static int GetPriority(MovementCommand cmd)
{
    // Legacy safety override flag takes absolute precedence
    if (cmd.OverrideSafetyConstraints) return 100;
    
    switch (cmd.Source)
    {
        case CommandSource.SAFETY_OVERRIDE:     return 100; // Collision avoidance, emergency stop
        case CommandSource.TACTICAL_AI:         return 75;  // Positioning, marking
        case CommandSource.BALL_INTERACTION:    return 50;  // First touch, pass execution
        case CommandSource.DEFAULT:             return 0;   // Maintain current trajectory
        default:                               return 0;
    }
}
```

**CommandSource enum (to be added to Section 3.5):**

```csharp
/// <summary>
/// Identifies the system that issued a movement command.
/// Used for priority resolution when multiple commands conflict.
/// 
/// PROVISIONAL (v1.1): Defined here pending addition to Section 3.5
/// MovementCommand struct in its next revision.
/// </summary>
public enum CommandSource
{
    DEFAULT,            // No specific source, lowest priority
    BALL_INTERACTION,   // First touch, pass, shot execution
    TACTICAL_AI,        // Formation positioning, marking runs, pressing
    SAFETY_OVERRIDE     // Collision avoidance, emergency stop, boundary enforcement
}
```

**MovementCommand.Maintain() factory method (to be added to Section 3.5):**

```csharp
/// <summary>
/// Creates a "maintain current trajectory" command.
/// Used when no external command is received this frame.
/// Agent continues with current velocity and facing direction.
/// 
/// PROVISIONAL (v1.1): Referenced by ResolveConflicts() but missing from
/// Section 3.5 factory methods. Must be added in Section 3.5 next revision.
/// </summary>
public static MovementCommand Maintain()
{
    return new MovementCommand
    {
        TargetPosition = Vector3.zero, // Ignored in maintain mode
        DesiredState = AgentMovementState.IDLE, // Will be overridden by current state
        DecelerationMode = DecelerationMode.CONTROLLED,
        FacingMode = FacingMode.AUTO_ALIGN,
        FacingTarget = Vector3.zero,
        OverrideSafetyConstraints = false,
        Source = CommandSource.DEFAULT,
        Timestamp = 0f,
        DebugLabel = "Maintain"
    };
}
```

---

## 3.6.4 Attribute Boundary Conditions

### 3.6.4.1 Attribute 1 Extremes (Minimum Capability)

Agents with Attribute 1 represent the absolute minimum physical capability. Formulas must produce valid (if poor) results.

**Test matrix for Attribute 1 agents:**

| Attribute | Value | Derived Parameter | Edge Behavior |
|-----------|-------|-------------------|---------------|
| Pace 1 | 7.5 m/s | Top speed | Still achievable via sprint state |
| Acceleration 1 | k = 0.658 sÃ¢ÂÂ»Ã‚Â¹ | 3.5s to 90% top speed | Noticeably slow but functional |
| Agility 1 | k_turn = 0.78 | 89.8Ã‚Â°/s at sprint | Wide turns, high stumble risk |
| Balance 1 | safe_frac = 0.55 | 30% stumble prob at max | Very high stumble chance |
| Strength 1 | 72.5 kg, 0.35m radius | Minimum mass/hitbox | Loses all physical duels |

**Critical edge case: Agility 1 + Balance 1 + Sprint**

```csharp
// Worst-case stumble scenario:
// Agility 1, Balance 1, Speed 9 m/s, attempting max turn
//
// Turn rate: 720 / (1 + 0.78 Ãƒâ€” 9) Ãƒâ€” 0.85 Ãƒâ€” 1.0 = 76.3Ã‚Â°/s
// Safe threshold: 76.3 Ãƒâ€” 0.55 = 42.0Ã‚Â°/s
// If attempting full 76.3Ã‚Â°/s:
//   Risk fraction = (76.3 - 42.0) / (76.3 - 42.0) = 1.0 (fully in risk zone)
//   Stumble probability = 0.30 per frame
//   Over 3 frames: P(stumble) = 1 - 0.70Ã‚Â³ = 65.7%
//
// Expected behavior: Agent WILL stumble on aggressive cuts.
// This is intentional Ã¢â‚¬â€ Attribute 1 players should struggle.
```

### 3.6.4.2 Attribute 20 Extremes (Maximum Capability)

Agents with Attribute 20 represent elite capability. Formulas must not produce superhuman results.

**Test matrix for Attribute 20 agents:**

| Attribute | Value | Derived Parameter | Ceiling Check |
|-----------|-------|-------------------|---------------|
| Pace 20 | 10.2 m/s | Top speed | < 12 m/s MAX_SPEED Ã¢Å“â€œ |
| Acceleration 20 | k = 0.921 sÃ¢ÂÂ»Ã‚Â¹ | 2.5s to 90% top speed | Explosive but physical |
| Agility 20 | k_turn = 0.35 | 173.5Ã‚Â°/s at sprint | Sharp but not instant |
| Balance 20 | safe_frac = 0.85 | 5% stumble prob at max | Rarely stumbles |
| Strength 20 | 100 kg, 0.50m radius | Maximum mass/hitbox | Dominates physical duels |

**Critical edge case: Agility 20 + Balance 20 at maximum speed**

```csharp
// Best-case elite scenario:
// Agility 20, Balance 20, Speed 10.2 m/s (max top speed)
//
// Turn rate: 720 / (1 + 0.35 Ãƒâ€” 10.2) Ãƒâ€” 1.0 Ãƒâ€” 1.0 = 157.5Ã‚Â°/s
// Turn radius: 10.2 / (157.5 Ãƒâ€” Ãâ‚¬/180) = 3.71m
//
// Validation:
//   - Turn radius > 3m: Agent cannot cut sharper than a car turning radius Ã¢Å“â€œ
//   - Still requires commitment: 90Ã‚Â° turn takes 0.57 seconds Ã¢Å“â€œ
//   - Safe threshold at 0.85: only 15% of turn rate is risky Ã¢Å“â€œ
//
// Expected behavior: Elite but not superhuman. Sharp cuts possible but
// require anticipation and timing, not instant reaction.
```

### 3.6.4.3 Mixed Extreme Attributes

Real players often have unbalanced attributes (fast but clumsy, or slow but agile). These combinations must work correctly.

**Test scenario: Pace 20 + Agility 1 ("Pure speed, no control")**

```csharp
// Fast agent who can't turn:
// Pace 20: Top speed 10.2 m/s
// Agility 1: k_turn = 0.78
//
// At top speed:
//   Turn rate: 720 / (1 + 0.78 Ãƒâ€” 10.2) = 80.4Ã‚Â°/s (Balance 20) or 68.3Ã‚Â°/s (Balance 1)
//   Turn radius: 10.2 / (80.4 Ãƒâ€” Ãâ‚¬/180) = 7.27m
//
// Expected behavior: Agent is a straight-line sprinter.
// Useful for: Through ball runs, counter-attacks
// Weakness: Tight spaces, 1v1 dribbling
```

**Test scenario: Pace 1 + Agility 20 ("Slow but nimble")**

```csharp
// Slow agent who turns sharply:
// Pace 1: Top speed 7.5 m/s
// Agility 20: k_turn = 0.35
//
// At top speed:
//   Turn rate: 720 / (1 + 0.35 Ãƒâ€” 7.5) = 197.8Ã‚Â°/s
//   Turn radius: 7.5 / (197.8 Ãƒâ€” Ãâ‚¬/180) = 2.17m
//
// Expected behavior: Agent is slow but maneuverable.
// Useful for: Holding midfield positions, box-to-box pressing
// Weakness: Cannot catch faster players on breakaways
```

### 3.6.4.4 Fatigue and Stamina Boundary Edge Cases (v1.1)

Section 3.1 defines forbidden transitions gated by stamina resources:
- `sprintReservoir < SPRINT_RESERVOIR_REENTRY` â†’ SPRINTING forbidden
- `aerobicPool < AEROBIC_JOG_FLOOR` â†’ forced deceleration from JOGGING

These create edge cases when stamina depletes mid-action:

**Edge Case 1: Sprint reservoir depletes mid-sprint**

```csharp
// Agent is SPRINTING, sprintReservoir hits 0 during a frame.
// State machine must force transition to JOGGING (not DECELERATING).
// Agent cannot re-enter SPRINTING until reservoir > SPRINT_RESERVOIR_REENTRY.
//
// NOTE: The exact depletion mechanics are defined in Fatigue System (Spec #13).
// This section only specifies the movement system's RESPONSE to depletion,
// not the depletion calculation itself.
//
// Stage 0: Fatigue is disabled (PerformanceContext.FatigueModifier = 1.0),
// so this edge case cannot occur. Implementation must still be present
// for Stage 1+ activation.
```

**Edge Case 2: Aerobic pool depletes mid-jog**

```csharp
// Agent is JOGGING, aerobicPool drops below AEROBIC_JOG_FLOOR.
// State machine forces transition to WALKING via DECELERATING.
// Agent must slow to walking speed before aerobic recovery can begin.
//
// Design intent: Prevents "infinite jogging" â€” even without sprinting,
// sustained jogging depletes aerobic capacity over 90 minutes.
```

**Edge Case 3: Simultaneous fatigue + stumble**

```csharp
// Agent with depleted stamina attempts aggressive turn and triggers stumble.
// Recovery priority: STUMBLING takes precedence over fatigue transitions.
// Fatigue state checked AFTER stumble recovery completes.
// This follows the Recovery Priority Matrix in Section 3.6.6.1.
```

**Stage 0 Implementation Note:** All fatigue edge cases are dormant in Stage 0 because `PerformanceContext.Default` sets all modifiers to 1.0. However, the guard conditions MUST be implemented with `TODO` markers so they activate correctly when Spec #13 (Fatigue System) comes online in Stage 1+.

---

