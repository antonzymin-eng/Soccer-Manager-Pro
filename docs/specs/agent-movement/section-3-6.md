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
- Goalkeeper-specific edge cases (Spec #10)

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

## 3.6.5 Physical Boundary Violations

### 3.6.5.1 Pitch Edge Handling

Agents may attempt to move beyond pitch boundaries due to momentum or AI pathfinding errors.

**Pitch Configuration:**

Pitch dimensions are variable Ã¢â‚¬â€ different competitions use different sizes. The movement system receives pitch configuration at match initialization and uses it for all boundary calculations.

```csharp
/// <summary>
/// Pitch configuration for boundary enforcement.
/// Injected at match start, immutable during match.
/// 
/// Coordinate system:
///   Origin: Corner of pitch (0, 0, 0) Ã¢â‚¬â€ NOT center
///   X-axis: Goal line to goal line (0 to Length)
///   Y-axis: Touchline to touchline (0 to Width)
///   Z-axis: Height above ground (0 = ground)
/// 
/// CROSS-REFERENCE WARNING (v1.1): This corner-origin convention matches
/// Ball Physics Spec #1 (Section 3.1) which is the authoritative coordinate
/// system definition. However, Section 3.5 (Data Structures) v1.2 contains
/// a conflicting comment stating "Origin at pitch center." Section 3.5 is
/// INCORRECT and must be corrected in its next revision to match corner-origin.
/// Until then, all new code in Agent Movement System must use corner-origin
/// as defined here and in Ball Physics Spec #1.
/// 
/// Standard sizes (FIFA Law 1):
///   - Professional: 100-110m Ãƒâ€” 64-75m (105Ãƒâ€”68m typical)
///   - International: 100-110m Ãƒâ€” 64-75m (mandatory)
///   - Youth/Amateur: 90-120m Ãƒâ€” 45-90m (variable)
/// 
/// This struct is the interface contract between Match Configuration (Spec TBD)
/// and Agent Movement System. Match Config owns the authoritative values;
/// this system consumes them read-only.
/// 
/// Future unification note: When Match Configuration Specification is written,
/// this struct should be moved there and referenced here. Current definition
/// is provisional to unblock Stage 0 implementation.
/// </summary>
public readonly struct PitchConfiguration
{
    /// <summary>Pitch length in meters (goal line to goal line, X-axis).</summary>
    public readonly float Length;
    
    /// <summary>Pitch width in meters (touchline to touchline, Y-axis).</summary>
    public readonly float Width;
    
    /// <summary>
    /// Buffer zone beyond pitch lines for agent movement (meters).
    /// Agents can move into this zone (e.g., taking throw-ins, running out of play).
    /// Hard boundary = pitch edge + buffer.
    /// Default: 5.0m Ã¢â‚¬â€ enough for throw-in positioning, not enough to disappear.
    /// </summary>
    public readonly float AgentBuffer;
    
    /// <summary>
    /// Creates pitch configuration with specified dimensions.
    /// </summary>
    /// <param name="length">Goal line to goal line (meters). Range: 90-120m.</param>
    /// <param name="width">Touchline to touchline (meters). Range: 45-90m.</param>
    /// <param name="agentBuffer">Buffer beyond pitch lines (meters). Default: 5.0m.</param>
    public PitchConfiguration(float length, float width, float agentBuffer = 5.0f)
    {
        // Validate FIFA-legal ranges with tolerance
        Length = Mathf.Clamp(length, 85f, 125f);   // Slightly beyond FIFA for edge testing
        Width = Mathf.Clamp(width, 40f, 95f);
        AgentBuffer = Mathf.Clamp(agentBuffer, 2f, 10f);
    }
    
    /// <summary>
    /// Standard professional pitch (105m Ãƒâ€” 68m).
    /// Used as default and for most testing scenarios.
    /// </summary>
    public static PitchConfiguration Standard => new PitchConfiguration(105f, 68f, 5f);
    
    /// <summary>
    /// Minimum FIFA-legal pitch (100m Ãƒâ€” 64m).
    /// Used for edge case testing Ã¢â‚¬â€ tight spaces stress movement AI.
    /// </summary>
    public static PitchConfiguration MinimumFIFA => new PitchConfiguration(100f, 64f, 5f);
    
    /// <summary>
    /// Maximum FIFA-legal pitch (110m Ãƒâ€” 75m).
    /// Used for edge case testing Ã¢â‚¬â€ large spaces test stamina/positioning.
    /// </summary>
    public static PitchConfiguration MaximumFIFA => new PitchConfiguration(110f, 75f, 5f);
    
    /// <summary>
    /// Youth/small-sided pitch (90m Ãƒâ€” 60m).
    /// Used for training modes, youth career simulation.
    /// </summary>
    public static PitchConfiguration Youth => new PitchConfiguration(90f, 60f, 4f);
    
    // ================================================================
    // DERIVED BOUNDARIES
    // ================================================================
    
    /// <summary>Minimum X position for agents (left of goal line by buffer).</summary>
    public float MinX => -AgentBuffer;
    
    /// <summary>Maximum X position for agents (right of goal line by buffer).</summary>
    public float MaxX => Length + AgentBuffer;
    
    /// <summary>Minimum Y position for agents (below touchline by buffer).</summary>
    public float MinY => -AgentBuffer;
    
    /// <summary>Maximum Y position for agents (above touchline by buffer).</summary>
    public float MaxY => Width + AgentBuffer;
    
    /// <summary>Pitch center point (for kickoff positioning, etc.).</summary>
    public Vector2 Center => new Vector2(Length / 2f, Width / 2f);
}
```

**Boundary enforcement:**

```csharp
/// <summary>
/// Handles agent approaching or crossing pitch boundaries.
/// 
/// Behavior:
///   - Soft boundary (pitch lines): AI receives notification, no physics intervention
///   - Hard boundary (pitch + buffer): Position clamped, velocity component zeroed
/// 
/// Called every frame after position integration.
/// 
/// Parameters:
///   agent: Agent to check and clamp
///   pitch: Current match pitch configuration (injected at match start)
/// </summary>
public static void EnforcePitchBoundaries(ref Agent agent, in PitchConfiguration pitch)
{
    bool positionClamped = false;
    
    // X-axis (goal line to goal line)
    if (agent.Position.x < pitch.MinX)
    {
        agent.Position.x = pitch.MinX;
        agent.Velocity.x = Mathf.Max(0f, agent.Velocity.x); // Stop moving further out
        positionClamped = true;
    }
    else if (agent.Position.x > pitch.MaxX)
    {
        agent.Position.x = pitch.MaxX;
        agent.Velocity.x = Mathf.Min(0f, agent.Velocity.x);
        positionClamped = true;
    }
    
    // Y-axis (touchline to touchline)
    if (agent.Position.y < pitch.MinY)
    {
        agent.Position.y = pitch.MinY;
        agent.Velocity.y = Mathf.Max(0f, agent.Velocity.y);
        positionClamped = true;
    }
    else if (agent.Position.y > pitch.MaxY)
    {
        agent.Position.y = pitch.MaxY;
        agent.Velocity.y = Mathf.Min(0f, agent.Velocity.y);
        positionClamped = true;
    }
    
    if (positionClamped)
    {
        agent.InvalidateSpeedCache();
        // Note: This is normal gameplay behavior (ball going out), not logged as warning
    }
    
    // ================================================================
    // GROUND PENETRATION FIX (v1.1: moved here from ApplySafetyClamps)
    // Agents operate on XY plane, Z is height (for jumping headers, etc.)
    // Stage 0: Z should always be 0 (grounded). Clamp if somehow negative.
    // ================================================================
    if (agent.Position.z < 0f)
    {
        agent.Position.z = 0f;
        if (agent.Velocity.z < 0f)
        {
            agent.Velocity.z = 0f;
            agent.InvalidateSpeedCache();
        }
        Debug.LogWarning($"[AgentMovement] Agent {agent.AgentID}: Ground penetration corrected");
    }
}
```

### 3.6.5.2 Agent Overlap Detection

Two agents occupying the same space is physically impossible but can occur due to:
- Collision system timing gaps
- Spawning errors
- Extreme velocity edge cases

**Detection and response:**

```csharp
/// <summary>
/// Detects agent overlap (pre-collision flag).
/// Does NOT resolve overlap Ã¢â‚¬â€ flags for Collision System (Spec #3) to handle.
/// 
/// Called by spatial partitioning system during collision broad phase.
/// </summary>
public static bool DetectOverlap(Agent a, Agent b)
{
    float dx = a.Position.x - b.Position.x;
    float dy = a.Position.y - b.Position.y;
    float distanceSq = dx * dx + dy * dy;
    
    float combinedRadius = a.PhysicalProperties.HitboxRadius + 
                           b.PhysicalProperties.HitboxRadius;
    float combinedRadiusSq = combinedRadius * combinedRadius;
    
    return distanceSq < combinedRadiusSq;
}

/// <summary>
/// Flags overlapping agents for collision system resolution.
/// Movement system does NOT attempt to separate agents Ã¢â‚¬â€ this is
/// Collision System (Spec #3) responsibility.
/// 
/// Rationale: Movement system lacks knowledge of:
///   - Which agent has priority
///   - Foul implications
///   - Ball possession context
/// 
/// Movement system only:
///   1. Detects overlap
///   2. Sets Agent.OverlapFlag = true
///   3. Logs INFO (not WARNING Ã¢â‚¬â€ overlap is normal gameplay)
/// </summary>
public static void FlagOverlap(ref Agent a, ref Agent b)
{
    a.OverlapFlag = true;
    b.OverlapFlag = true;
    
    // INFO level Ã¢â‚¬â€ this happens during normal gameplay (tackles, challenges)
    Debug.Log($"[AgentMovement] Overlap detected: Agent {a.AgentID} and Agent {b.AgentID}");
}
```

### 3.6.5.3 Ground Penetration

Agents should never have negative Z position in Stage 0 (all movement is ground-based).

**IMPLEMENTATION NOTE (v1.1):** Ground penetration correction is performed inside
`EnforcePitchBoundaries()` as part of the unified position correction pass. It is
NOT duplicated in `ApplySafetyClamps()`. The code below is the authoritative
implementation that lives inside `EnforcePitchBoundaries()`:

```csharp
/// <summary>
/// Fixes ground penetration if somehow agent Z < 0.
/// Stage 0: Agents are always on ground (Z = 0).
/// Stage 1+: May have jumping/heading with Z > 0.
/// 
/// Recovery: Clamp Z to 0, zero downward velocity.
/// 
/// Called from: EnforcePitchBoundaries() ONLY (not ApplySafetyClamps).
/// </summary>
if (agent.Position.z < 0f)
{
    agent.Position.z = 0f;
    agent.Velocity.z = Mathf.Max(0f, agent.Velocity.z);
    Debug.LogWarning($"[AgentMovement] Agent {agent.AgentID}: Ground penetration corrected");
}
}
```

---

## 3.6.6 Error Recovery Procedures

### 3.6.6.1 Recovery Priority Matrix

When multiple errors occur simultaneously, recovery follows this priority:

| Priority | Error Type | Recovery Action |
|----------|------------|-----------------|
| 1 | NaN/Infinity | Full state reset to LastValid, force IDLE |
| 2 | Ground penetration | Position clamp, preserve horizontal velocity |
| 3 | Runaway velocity | Velocity magnitude clamp, preserve direction |
| 4 | Oscillation | State lock for 500ms, maintain current state |
| 5 | Pitch boundary | Position clamp, zero boundary-crossing velocity |
| 6 | Agent overlap | Flag for Collision System, no movement changes |

### 3.6.6.2 LastValid State Maintenance

**Initialization Guarantee (v1.1):**

`LastValidPosition` MUST be initialized before the first frame of simulation. If the first frame produces NaN before any valid state is saved, `RecoverFromNaN()` would restore to an uninitialized (zero) position, teleporting the agent to the pitch corner.

```csharp
/// <summary>
/// Initializes LastValid state from formation position.
/// MUST be called during agent spawning, BEFORE first physics frame.
/// 
/// v1.1: Added to prevent uninitialized LastValidPosition recovery.
/// If this method is not called, NaN recovery on frame 1 would teleport
/// agent to (0,0,0) â€” the pitch corner.
/// 
/// Called by: AgentFactory.CreateAgent() or equivalent spawning logic.
/// </summary>
public void InitializeLastValidState(Vector3 formationPosition, Vector2 initialFacing)
{
    LastValidPosition = formationPosition;
    LastValidVelocity = Vector3.zero;
    LastValidFacingDirection = initialFacing.normalized;
    LastValidState = AgentMovementState.IDLE;
    
    // Also set current state to match
    Position = formationPosition;
    Velocity = Vector3.zero;
    FacingDirection = initialFacing.normalized;
    CurrentState = AgentMovementState.IDLE;
}
```

**Per-frame update:**

```csharp
/// <summary>
/// Updates LastValid state snapshots.
/// Called every frame AFTER validation passes (no NaN/Infinity).
/// 
/// LastValid state is used for recovery if next frame produces invalid values.
/// </summary>
public void UpdateLastValidState()
{
    if (!HasInvalidValues(this))
    {
        LastValidPosition = Position;
        LastValidVelocity = Velocity;
        LastValidFacingDirection = FacingDirection;
        LastValidState = CurrentState;
    }
    // If HasInvalidValues is true, we're in recovery Ã¢â‚¬â€ don't update LastValid
}
```

### 3.6.6.3 Graceful Degradation Modes

When an agent enters recovery, it operates in degraded mode until stable:

**Degraded Mode Characteristics:**

- **Reduced responsiveness:** Commands are filtered more aggressively
- **Conservative state transitions:** Only downward transitions allowed (SPRINT Ã¢â€ â€™ JOG Ã¢â€ â€™ WALK Ã¢â€ â€™ IDLE)
- **Visual indicator:** Debug visualization shows agent in recovery (Stage 0 editor only)
- **Duration:** Minimum 0.5 seconds, extends if further errors occur

```csharp
/// <summary>
/// Degraded mode state for agents in recovery.
/// 
/// FIXED v1.1: ConsecutiveErrors now decays over time instead of hard-resetting
/// to 0 on first successful exit. This prevents perpetual recovery cycling when
/// an agent hits intermittent NaN (e.g., every ~0.6s), which would otherwise
/// reset to 0 then re-enter degraded mode without ever escalating.
/// </summary>
public struct DegradedModeState
{
    public bool IsActive;
    public float ExitTime;          // Time when degraded mode can end
    public int ConsecutiveErrors;   // Escalates recovery severity
    public float LastCleanTime;     // v1.1: Tracks last error-free timestamp for decay
    
    public void Enter(float currentTime)
    {
        IsActive = true;
        ExitTime = currentTime + 0.5f; // Minimum 500ms
        ConsecutiveErrors++;
        
        if (ConsecutiveErrors > 3)
        {
            // Escalate: longer recovery, more restrictive
            ExitTime = currentTime + 1.0f;
        }
    }
    
    public void TryExit(float currentTime)
    {
        if (IsActive && currentTime >= ExitTime)
        {
            IsActive = false;
            LastCleanTime = currentTime;
            // v1.1: Do NOT reset ConsecutiveErrors here.
            // Errors decay over time via DecayErrors() instead.
        }
    }
    
    /// <summary>
    /// v1.1: Decays ConsecutiveErrors over time during clean operation.
    /// Called every frame when NOT in degraded mode.
    /// Halves error count every ERROR_DECAY_INTERVAL (2.0s) of clean operation.
    /// This allows escalation for rapid-fire errors while still recovering
    /// from occasional one-off errors over time.
    /// </summary>
    private const float ERROR_DECAY_INTERVAL = 2.0f;
    
    public void DecayErrors(float currentTime)
    {
        if (!IsActive && ConsecutiveErrors > 0)
        {
            float cleanDuration = currentTime - LastCleanTime;
            if (cleanDuration >= ERROR_DECAY_INTERVAL)
            {
                // Halve errors for each decay interval elapsed
                int decaySteps = (int)(cleanDuration / ERROR_DECAY_INTERVAL);
                for (int i = 0; i < decaySteps && ConsecutiveErrors > 0; i++)
                {
                    ConsecutiveErrors = ConsecutiveErrors / 2; // Integer division rounds down
                }
                LastCleanTime = currentTime; // Reset decay timer
            }
        }
    }
}
```

---

## 3.6.7 Validation Scenarios & Expected Outcomes

### 3.6.7.1 Unit Test Scenarios

**Test ID: UT-EDGE-001**  
**Name:** `NaN_Position_TriggersRecovery`  
**Setup:** Set agent.Position.x = float.NaN  
**Expected:**
- HasInvalidValues() returns true
- Position restored to LastValidPosition
- Velocity zeroed
- State forced to IDLE
- ERROR logged  
**Tolerance:** Exact

---

**Test ID: UT-EDGE-002**  
**Name:** `Infinity_Velocity_TriggersRecovery`  
**Setup:** Set agent.Velocity = new Vector3(float.PositiveInfinity, 0, 0)  
**Expected:**
- HasInvalidValues() returns true
- Full state recovery executed
- State forced to IDLE  
**Tolerance:** Exact

---

**Test ID: UT-EDGE-003**  
**Name:** `ZeroVelocity_DirectionFallback`  
**Setup:** agent.Velocity = Vector3.zero, agent.FacingDirection = (0, 1)  
**Action:** Call GetMovementDirection(agent)  
**Expected:** Returns (0, 1, 0) Ã¢â‚¬â€ falling back to facing direction  
**Tolerance:** Exact

---

**Test ID: UT-EDGE-004**  
**Name:** `RunawayVelocity_Clamped`  
**Setup:** agent.Velocity = new Vector3(50, 0, 0) Ã¢â‚¬â€ 50 m/s exceeds 12 m/s limit  
**Expected:**
- Velocity clamped to (12, 0, 0)
- Direction preserved
- WARNING logged  
**Tolerance:** Velocity magnitude exactly 12.0 m/s

---

**Test ID: UT-EDGE-005**  
**Name:** `Oscillation_BlocksTransition`  
**Setup:** Attempt 7 state transitions within 1 second (first 6 succeed and are recorded)  
**Expected:**
- First 6 transitions succeed (recorded in ring buffer)
- 7th transition attempt is BLOCKED (exceeds MAX_STATE_CHANGES_PER_SECOND = 6)
- OscillationGuard.IsLocked = true
- State machine frozen for 500ms  
**Tolerance:** Lock duration Â±1 frame (16.67ms)

---

**Test ID: UT-EDGE-006**  
**Name:** `Oscillation_UnlocksAfterCooldown`  
**Setup:** Trigger oscillation lock, advance time by 600ms  
**Action:** Attempt state transition  
**Expected:** Transition allowed (lock expired)  
**Tolerance:** Exact

---

**Test ID: UT-EDGE-007**  
**Name:** `Attribute1_Agility_ProducesValidTurnRate`  
**Setup:** Agility = 1, Balance = 1, Speed = 9.0 m/s  
**Action:** CalculateMaxTurnRate()  
**Expected:** Turn rate Ã¢â€°Ë† 76.3Ã‚Â°/s (Ã‚Â±1.0Ã‚Â°/s)  
**Derivation:** 720 / (1 + 0.78 Ãƒâ€” 9) Ãƒâ€” 0.85 = 76.3Ã‚Â°/s  
**Tolerance:** Ã‚Â±1.0Ã‚Â°/s

---

**Test ID: UT-EDGE-008**  
**Name:** `Attribute20_AllParams_AtCeiling`  
**Setup:** All movement attributes = 20  
**Expected:**
- Top speed: 10.2 m/s (< MAX_SPEED 12.0) Ã¢Å“â€œ
- Turn rate at 10.2 m/s: 157.5Ã‚Â°/s (< MAX_TURN_RATE 720.0) Ã¢Å“â€œ
- Stumble probability: 5% per frame at max turn (low but non-zero) Ã¢Å“â€œ
- Mass: 100 kg (reasonable for elite athlete) Ã¢Å“â€œ  
**Tolerance:** Per-value tolerances from source sections

---

**Test ID: UT-EDGE-009**  
**Name:** `PitchBoundary_ClampsPosition`  
**Setup:** 
- pitch = PitchConfiguration.Standard (105m Ãƒâ€” 68m, 5m buffer)
- agent.Position = new Vector3(120, 50, 0) Ã¢â‚¬â€ X exceeds pitch.MaxX (110)  
**Expected:**
- Position.x clamped to 110 (pitch.Length + pitch.AgentBuffer)
- Velocity.x zeroed if was positive
- No state change  
**Tolerance:** Exact

---

**Test ID: UT-EDGE-009b**  
**Name:** `PitchBoundary_VariablePitchSize`  
**Setup:** 
- pitch = PitchConfiguration.MinimumFIFA (100m Ãƒâ€” 64m, 5m buffer)
- agent.Position = new Vector3(108, 50, 0) Ã¢â‚¬â€ X exceeds pitch.MaxX (105)  
**Expected:**
- Position.x clamped to 105 (100 + 5m buffer)
- Works correctly with non-standard pitch size  
**Tolerance:** Exact

---

**Test ID: UT-EDGE-010**  
**Name:** `GroundPenetration_Corrected`  
**Setup:** agent.Position.z = -0.5f  
**Expected:**
- Position.z = 0
- Velocity.z = 0 if was negative
- WARNING logged  
**Tolerance:** Exact

---

**Test ID: UT-EDGE-011**  
**Name:** `AgentOverlap_FlagsNotResolves`  
**Setup:** Two agents at same position  
**Action:** DetectOverlap(), FlagOverlap()  
**Expected:**
- Both agents have OverlapFlag = true
- Positions NOT modified (Collision System responsibility)
- INFO logged (not WARNING)  
**Tolerance:** Exact

---

**Test ID: UT-EDGE-012**  
**Name:** `ForbiddenTransition_Rejected`  
**Setup:** agent.CurrentState = IDLE, command.DesiredState = SPRINTING  
**Expected:**
- State transitions to WALKING (not SPRINTING)
- Speed progression follows normal curve  
**Tolerance:** State machine rules followed exactly

---

**Test ID: UT-EDGE-013**  
**Name:** `DirectionReversal_ForcesDecel`  
**Setup:** agent.Velocity = (8, 0, 0), targetDirection = (-1, 0, 0)  
**Expected:**
- HandleDirectionReversal returns DECELERATING
- Agent slows before reversing  
**Tolerance:** State = DECELERATING

---

**Test ID: UT-EDGE-014**  
**Name:** `CommandQueueOverflow_DropsOldest`  
**Setup:** Queue 10 commands (exceeds limit of 4)  
**Expected:**
- Oldest 6 commands dropped
- Most recent 4 commands retained
- WARNING logged  
**Tolerance:** Exact

---

**Test ID: UT-EDGE-015**  
**Name:** `TurnRadius_ZeroSpeed_ReturnsZero`  
**Setup:** speed = 0, turnRate = 720Ã‚Â°/s  
**Action:** CalculateMinTurnRadius()  
**Expected:** Returns 0.0 (no radius constraint)  
**Tolerance:** Exact

---

**Test ID: UT-EDGE-016**  
**Name:** `TurnRadius_ZeroTurnRate_ReturnsMax`  
**Setup:** speed = 9 m/s, turnRate = 0.5Â°/s (below 1.0 threshold)  
**Action:** CalculateMinTurnRadius()  
**Expected:** Returns float.MaxValue  
**Tolerance:** Exact

---

**Test ID: UT-EDGE-017** *(v1.1)*  
**Name:** `DegradedMode_EscalatesAfterFourErrors`  
**Setup:** Agent enters degraded mode 4 times in rapid succession (errors at t=0, 0.6, 1.2, 1.8s)  
**Expected:**
- First 3 entries: ExitTime = entry + 0.5s (standard recovery)
- 4th entry: ExitTime = entry + 1.0s (escalated recovery)
- ConsecutiveErrors = 4 at time of escalation  
**Tolerance:** ExitTime Â±1 frame (16.67ms)

---

**Test ID: UT-EDGE-018** *(v1.1)*  
**Name:** `DegradedMode_ErrorsDecayOverTime`  
**Setup:** Agent accumulates ConsecutiveErrors = 4, then operates cleanly for 4.0s  
**Expected:**
- After 2.0s clean: ConsecutiveErrors = 2 (halved once)
- After 4.0s clean: ConsecutiveErrors = 1 (halved again)
- Next error after decay does NOT trigger escalated 1.0s recovery  
**Tolerance:** Exact

---

### 3.6.7.2 Integration Test Scenarios

**Test ID: IT-EDGE-001**  
**Name:** `NaN_Recovery_ContinuesSimulation`  
**Scenario:**
1. Simulate match with 22 agents for 60 seconds
2. At t=30s, inject NaN into one agent's Position
3. Continue simulation  
**Expected:**
- Corrupted agent recovers within 1 frame
- Other 21 agents unaffected
- Match simulation continues normally
- No crash or freeze  
**Duration:** 60 seconds simulated time

---

**Test ID: IT-EDGE-002**  
**Name:** `MultiAgent_Oscillation_Isolated`  
**Scenario:**
1. Agent A triggers oscillation lock
2. Agent B receives rapid commands  
**Expected:**
- Agent A locked for 500ms
- Agent B state machine operates normally
- No cross-contamination between agents  
**Duration:** 2 seconds simulated time

---

**Test ID: IT-EDGE-003**  
**Name:** `Extreme_Attribute_Combinations_Stable`  
**Scenario:**
1. Create agents with all edge attribute combinations:
   - (1,1,1,1,1,1) Ã¢â‚¬â€ worst possible
   - (20,20,20,20,20,20) Ã¢â‚¬â€ best possible
   - (1,20,1,20,1,20) Ã¢â‚¬â€ alternating
   - (20,1,20,1,20,1) Ã¢â‚¬â€ alternating inverse
2. Simulate 90 minutes of match play  
**Expected:**
- Zero NaN/Infinity values
- All agents remain on pitch
- State machines never crash
- Position drift < 5cm per agent  
**Duration:** 90 minutes simulated time (54,000 frames)

---

**Test ID: IT-EDGE-004**  
**Name:** `Boundary_Stress_Test`  
**Scenario:**
1. Test with three pitch configurations:
   - PitchConfiguration.Standard (105Ãƒâ€”68m)
   - PitchConfiguration.MinimumFIFA (100Ãƒâ€”64m)
   - PitchConfiguration.MaximumFIFA (110Ãƒâ€”75m)
2. For each: command all agents to run toward corners simultaneously
3. Let physics carry them into boundary buffer
4. Verify clamping behavior at each pitch size  
**Expected:**
- All agents clamped at boundary (pitch.MaxX, pitch.MaxY)
- No agents exit buffer zone
- Velocity components appropriately zeroed
- No oscillation at boundary
- Behavior consistent across all pitch sizes  
**Duration:** 30 seconds simulated time per pitch size (90s total)

---

**Test ID: IT-EDGE-005**  
**Name:** `Rapid_Command_Burst_Stability`  
**Scenario:**
1. Issue 100 movement commands to single agent in 1 second
2. Commands include contradictory directions  
**Expected:**
- Agent executes most recent valid command
- No state machine crash
- Performance within budget (< 1ms total resolution)  
**Duration:** 5 seconds simulated time

---

### 3.6.7.3 Acceptance Criteria Summary

**Mandatory (must pass for Stage 0 approval):**

- Ã¢Å“â€œ All 19 unit tests pass (16 original + UT-EDGE-009b + UT-EDGE-017 + UT-EDGE-018)
- Ã¢Å“â€œ All 5 integration tests pass
- Ã¢Å“â€œ Zero NaN/Infinity values in 1000-match stress test
- Ã¢Å“â€œ Position drift < 5cm after 90 minutes (per agent)
- Ã¢Å“â€œ Oscillation detection triggers at exactly 6 transitions/second
- Ã¢Å“â€œ All attribute boundary combinations produce valid output
- Ã¢Å“â€œ Pitch boundary violations always corrected within 1 frame

**Quality:**

- Ã¢Å“â€œ All edge cases have explicit handling code (no implicit behavior)
- Ã¢Å“â€œ All recovery procedures logged with context
- Ã¢Å“â€œ All magic numbers replaced with named constants
- Ã¢Å“â€œ Error recovery deterministic (replay-safe)

---

## End of Section 3.6

**Page Count:** ~24 pages  
**Next Section:** Section 4 Ã¢â‚¬â€ Implementation Details (Code Organization, Dependencies, Configuration)

---

## CRITICAL CROSS-REFERENCES

This section consolidates edge case handling from:
- Section 2.4 (Failure Modes) Ã¢â‚¬â€ high-level recovery requirements
- Section 3.1 (State Machine) Ã¢â‚¬â€ oscillation guard, transition validation
- Section 3.2 (Locomotion) Ã¢â‚¬â€ velocity/acceleration limits
- Section 3.3 (Directional Movement) Ã¢â‚¬â€ facing direction fallbacks
- Section 3.4 (Turning) Ã¢â‚¬â€ stumble risk edge cases
- Section 3.5 (Data Structures) Ã¢â‚¬â€ MovementConstants safety limits

**New structures defined in this section:**
- `PitchConfiguration` Ã¢â‚¬â€ variable pitch dimensions for boundary enforcement

All implementations in this section are **authoritative** for error handling. When conflict exists with earlier sections, this section takes precedence for recovery procedures.

**PitchConfiguration unification note:** The `PitchConfiguration` struct defined in Section 3.6.5.1 is provisional. When Match Configuration Specification is written (Spec TBD, likely Spec #14 or #15), `PitchConfiguration` should be moved there as the single source of truth, and this section should import it. Current definition ensures Stage 0 implementation can proceed without blocking on that spec.

---

## IMPLEMENTATION NOTES

1. **HasInvalidValues() call frequency:** Every frame, at start of ValidateAgentState(). This is non-negotiable Ã¢â‚¬â€ NaN propagates faster than recovery can catch it if skipped.

2. **LastValid state update timing:** After all validation passes, before end of frame. Never update LastValid during recovery.

3. **Oscillation guard ring buffer:** Fixed 8-slot array, no heap allocation after initialization. Ring buffer avoids list resizing during gameplay.

4. **Boundary enforcement order:** Position clamps apply AFTER physics integration, BEFORE state machine evaluation. This ensures state machine sees corrected position.

5. **Overlap flag lifetime:** One frame only. Collision System must read and clear OverlapFlag within same frame, or flag is lost.

6. **Degraded mode extension:** If errors continue during recovery, extend degraded mode rather than reset timer. Prevents infinite recovery loops.

7. **Determinism requirement:** All recovery procedures use deterministic math only. No System.Random calls during recovery Ã¢â‚¬â€ RNG state must match for replay.

8. **PitchConfiguration injection:** The `PitchConfiguration` struct must be injected into the Agent Movement System at match initialization. Suggested pattern:
   ```csharp
   // At match start:
   AgentMovementSystem.Initialize(matchConfig.Pitch);
   
   // System stores reference:
   private readonly PitchConfiguration _pitch;
   
   // Used in boundary enforcement:
   EnforcePitchBoundaries(ref agent, in _pitch);
   ```
   This ensures all agents use consistent pitch dimensions without per-frame parameter passing overhead.

9. **Pitch size validation:** `PitchConfiguration` constructor clamps values to slightly beyond FIFA-legal ranges (85-125m Ã— 40-95m) to allow edge case testing while preventing obviously invalid configurations. Production matches should use FIFA-legal ranges only.

10. **v1.1: Frame execution order (consolidated).** The per-frame call sequence for edge case handling is:
   ```
   1. HasInvalidValues()        â†’ NaN/Infinity recovery if needed
   2. ApplySafetyClamps()       â†’ Velocity, turn rate, lean angle clamps
   3. EnforcePitchBoundaries()  â†’ Position bounds, ground penetration
   4. State machine evaluation  â†’ Sees corrected values
   5. UpdateLastValidState()    â†’ Snapshot for next-frame recovery
   6. DegradedModeState.DecayErrors() â†’ If not in degraded mode
   ```

11. **v1.1: Section 3.5 coordinate system correction required.** Section 3.5 v1.2 states "Origin at pitch center" in the Agent.Position comment. This contradicts Ball Physics Spec #1 and this section's PitchConfiguration, both of which use corner-origin (0,0,0). Section 3.5 must be corrected in its next revision. This is tracked as a blocking issue for Section 3.5 approval.

12. **v1.1: CommandSource and Timestamp fields.** The MovementCommand struct in Section 3.5 must add `CommandSource Source` and `float Timestamp` fields in its next revision to support the full priority resolution system defined in 3.6.3.3. Until then, all commands will resolve at DEFAULT priority with undefined tiebreaking.

13. **v1.1: MovementCommand.Maintain() factory method.** Must be added to Section 3.5 MovementCommand factory methods. Provisional definition provided in 3.6.3.3 pending that addition.
