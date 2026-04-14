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
