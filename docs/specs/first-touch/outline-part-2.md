## SECTION 3: TECHNICAL SPECIFICATIONS (~15 pages)

### 3.1 Control Quality Model

#### 3.1.1 Core Formula

The control quality formula determines how well an agent controls an incoming ball. Derived from Master Vol 1 Â§6.4 "Touch Mechanics":

```
Control_Quality = Agent_Technique / (Ball_Velocity Ã— Agent_Inertia)
```

**Expanded implementation:**

```csharp
/// <summary>
/// Calculates control quality for a first touch.
/// 
/// Control quality determines how closely the ball stays to the agent
/// after receiving it. Higher quality = tighter control.
/// 
/// Formula derivation: Master Vol 1 Â§6.4
/// - Technique provides base control ability
/// - Ball velocity increases difficulty (faster ball = harder to control)
/// - Agent movement (inertia) increases difficulty (running = harder to control)
/// - Pressure from opponents degrades effective technique
/// - Body orientation can provide bonus
/// </summary>
/// <param name="technique">Agent Technique attribute [1-20]</param>
/// <param name="firstTouch">Agent First Touch attribute [1-20]</param>
/// <param name="ballVelocity">Incoming ball speed in m/s</param>
/// <param name="agentSpeed">Agent movement speed in m/s</param>
/// <param name="pressureScalar">Pressure from nearby opponents [0.0-1.0]</param>
/// <param name="hasOrientationBonus">True if agent in half-turn stance</param>
/// <returns>Control quality [0.0-1.0], higher is better</returns>
public float CalculateControlQuality(
    int technique,
    int firstTouch,
    float ballVelocity,
    float agentSpeed,
    float pressureScalar,
    bool hasOrientationBonus)
{
    // Step 1: Calculate effective attribute from Technique and First Touch
    // Technique is primary (70%), First Touch is secondary (30%)
    float rawAttribute = technique * TECHNIQUE_WEIGHT + firstTouch * FIRST_TOUCH_WEIGHT;
    
    // Step 2: Apply body orientation bonus if in half-turn stance
    float orientationMultiplier = hasOrientationBonus ? HALF_TURN_BONUS : 1.0f;
    float effectiveAttribute = rawAttribute * orientationMultiplier;
    
    // Step 3: Apply pressure penalty
    // Pressure scalar [0-1] reduces effective attribute
    float pressurePenalty = 1.0f - (pressureScalar * PRESSURE_DEGRADATION_FACTOR);
    effectiveAttribute *= pressurePenalty;
    
    // Step 4: Normalize attribute to [0-1] range
    float attributeBase = effectiveAttribute / MAX_ATTRIBUTE_SUM;
    
    // Step 5: Calculate velocity difficulty
    // Faster balls are harder to control
    // Reference velocity represents "typical" pass speed
    float velocityDifficulty = Mathf.Max(ballVelocity, MIN_VELOCITY) / VELOCITY_REFERENCE;
    
    // Step 6: Calculate movement difficulty (inertia)
    // Running while receiving is harder than standing still
    float movementDifficulty = 1.0f + (agentSpeed / MAX_SPRINT_SPEED) * MOVEMENT_PENALTY_FACTOR;
    
    // Step 7: Combine factors
    float controlQuality = attributeBase / (velocityDifficulty * movementDifficulty);
    
    // Step 8: Clamp to valid range
    return Mathf.Clamp01(controlQuality);
}
```

#### 3.1.2 Constants Definition

```csharp
/// <summary>
/// Constants for First Touch Mechanics.
/// All tunable parameters consolidated for ease of balancing.
/// 
/// Modification policy: Values frozen after Stage 0 approval.
/// Changes in Stage 1+ require regression testing.
/// </summary>
public static class FirstTouchConstants
{
    // ================================================================
    // ATTRIBUTE WEIGHTS
    // ================================================================
    
    /// <summary>Weight of Technique attribute in control calculation</summary>
    public const float TECHNIQUE_WEIGHT = 0.7f;
    
    /// <summary>Weight of First Touch attribute in control calculation</summary>
    public const float FIRST_TOUCH_WEIGHT = 0.3f;
    
    /// <summary>Maximum possible attribute sum (20 * 0.7 + 20 * 0.3 = 20)</summary>
    public const float MAX_ATTRIBUTE_SUM = 20.0f;
    
    // ================================================================
    // CONTROL QUALITY THRESHOLDS
    // ================================================================
    
    /// <summary>Quality threshold for perfect control (ball within 0.3m)</summary>
    public const float PERFECT_CONTROL_THRESHOLD = 0.85f;
    
    /// <summary>Quality threshold for good control (ball within 0.6m)</summary>
    public const float GOOD_CONTROL_THRESHOLD = 0.60f;
    
    /// <summary>Quality threshold for poor control (ball within 1.2m)</summary>
    public const float POOR_CONTROL_THRESHOLD = 0.35f;
    
    // Below POOR_CONTROL_THRESHOLD = heavy touch (up to 2.0m)
    
    // ================================================================
    // TOUCH RADIUS VALUES
    // ================================================================
    
    /// <summary>Maximum displacement for perfect control (m)</summary>
    public const float PERFECT_RADIUS = 0.3f;
    
    /// <summary>Maximum displacement for good control (m)</summary>
    public const float GOOD_RADIUS = 0.6f;
    
    /// <summary>Maximum displacement for poor control (m)</summary>
    public const float POOR_RADIUS = 1.2f;
    
    /// <summary>Maximum displacement for heavy touch (m)</summary>
    public const float HEAVY_RADIUS_MAX = 2.0f;
    
    // ================================================================
    // VELOCITY PARAMETERS
    // ================================================================
    
    /// <summary>Reference velocity for "typical" pass (m/s)</summary>
    public const float VELOCITY_REFERENCE = 15.0f;
    
    /// <summary>Minimum velocity to prevent division issues (m/s)</summary>
    public const float MIN_VELOCITY = 1.0f;
    
    /// <summary>Maximum expected ball velocity (m/s)</summary>
    public const float MAX_BALL_VELOCITY = 50.0f;
    
    /// <summary>Velocity above which control is severely limited (m/s)</summary>
    public const float THUNDERBOLT_THRESHOLD = 25.0f;
    
    /// <summary>Maximum control quality for thunderbolt receptions</summary>
    public const float THUNDERBOLT_QUALITY_CAP = 0.30f;
    
    // ================================================================
    // MOVEMENT PARAMETERS
    // ================================================================
    
    /// <summary>Maximum sprint speed for movement penalty calc (m/s)</summary>
    public const float MAX_SPRINT_SPEED = 10.0f;
    
    /// <summary>How much movement penalizes control [0-1]</summary>
    public const float MOVEMENT_PENALTY_FACTOR = 0.4f;
    
    /// <summary>Control bonus for stationary reception</summary>
    public const float STATIONARY_BONUS = 1.1f;
    
    // ================================================================
    // PRESSURE PARAMETERS
    // ================================================================
    
    /// <summary>Maximum radius for pressure detection (m)</summary>
    public const float PRESSURE_RADIUS_MAX = 3.0f;
    
    /// <summary>Radius for critical pressure / tight marking (m)</summary>
    public const float PRESSURE_RADIUS_CRITICAL = 1.0f;
    
    /// <summary>How much pressure degrades control [0-1]</summary>
    public const float PRESSURE_DEGRADATION_FACTOR = 0.4f;
    
    // ================================================================
    // ORIENTATION PARAMETERS
    // ================================================================
    
    /// <summary>Ideal angle for half-turn stance (degrees)</summary>
    public const float HALF_TURN_IDEAL_ANGLE = 45.0f;
    
    /// <summary>Tolerance for half-turn detection (degrees)</summary>
    public const float HALF_TURN_TOLERANCE = 15.0f;
    
    /// <summary>Control bonus for half-turn orientation</summary>
    public const float HALF_TURN_BONUS = 1.15f;
    
    // ================================================================
    // POSSESSION PARAMETERS
    // ================================================================
    
    /// <summary>Ball height threshold for ground control (m)</summary>
    public const float GROUND_CONTROL_HEIGHT = 0.5f;
    
    /// <summary>Distance within which opponent can intercept (m)</summary>
    public const float INTERCEPTION_RADIUS = 1.5f;
}
```

#### 3.1.3 Touch Radius Calculation

```csharp
/// <summary>
/// Converts control quality to ball displacement distance.
/// 
/// Uses piecewise linear interpolation between threshold points:
/// - Perfect (0.85-1.0): 0.0m - 0.3m
/// - Good (0.60-0.85): 0.3m - 0.6m
/// - Poor (0.35-0.60): 0.6m - 1.2m
/// - Heavy (0.0-0.35): 1.2m - 2.0m
/// </summary>
public float CalculateTouchRadius(float controlQuality)
{
    if (controlQuality >= PERFECT_CONTROL_THRESHOLD)
    {
        // Perfect control: interpolate from 0 to PERFECT_RADIUS
        float t = (controlQuality - PERFECT_CONTROL_THRESHOLD) / 
                  (1.0f - PERFECT_CONTROL_THRESHOLD);
        return Mathf.Lerp(PERFECT_RADIUS, 0f, t);
    }
    
    if (controlQuality >= GOOD_CONTROL_THRESHOLD)
    {
        // Good control: interpolate from PERFECT_RADIUS to GOOD_RADIUS
        float t = (controlQuality - GOOD_CONTROL_THRESHOLD) / 
                  (PERFECT_CONTROL_THRESHOLD - GOOD_CONTROL_THRESHOLD);
        return Mathf.Lerp(GOOD_RADIUS, PERFECT_RADIUS, t);
    }
    
    if (controlQuality >= POOR_CONTROL_THRESHOLD)
    {
        // Poor control: interpolate from GOOD_RADIUS to POOR_RADIUS
        float t = (controlQuality - POOR_CONTROL_THRESHOLD) / 
                  (GOOD_CONTROL_THRESHOLD - POOR_CONTROL_THRESHOLD);
        return Mathf.Lerp(POOR_RADIUS, GOOD_RADIUS, t);
    }
    
    // Heavy touch: interpolate from POOR_RADIUS to HEAVY_RADIUS_MAX
    float tHeavy = controlQuality / POOR_CONTROL_THRESHOLD;
    return Mathf.Lerp(HEAVY_RADIUS_MAX, POOR_RADIUS, tHeavy);
}
```

### 3.2 Body Orientation Bonus

#### 3.2.1 Half-Turn Detection

The "half-turn" is a fundamental receiving technique where the player positions their body at approximately 45Â° to the incoming ball. This allows them to see both the ball and the field, enabling faster subsequent actions.

```csharp
/// <summary>
/// Determines if agent is in half-turn stance for receiving.
/// 
/// Half-turn: Body oriented at ~45Â° to ball approach vector.
/// This position allows the receiver to:
/// - See the ball arriving
/// - See the field and options
/// - Execute next action faster
/// 
/// Per Master Vol 1 Â§6.4: Half-turn reduces L_rec by 15% and
/// provides control quality bonus.
/// </summary>
/// <param name="agentFacing">Agent's facing direction (unit vector)</param>
/// <param name="ballApproachVector">Direction ball is traveling (unit vector)</param>
/// <returns>True if agent is in valid half-turn orientation</returns>
public bool IsHalfTurnOrientation(Vector3 agentFacing, Vector3 ballApproachVector)
{
    // Ball approach vector points toward agent; negate to get "facing the ball" direction
    Vector3 facingBallDirection = -ballApproachVector.normalized;
    
    // Calculate angle between agent facing and ball approach
    float angle = Vector3.Angle(agentFacing, facingBallDirection);
    
    // Half-turn is valid within tolerance of ideal angle
    float minAngle = HALF_TURN_IDEAL_ANGLE - HALF_TURN_TOLERANCE;  // 30Â°
    float maxAngle = HALF_TURN_IDEAL_ANGLE + HALF_TURN_TOLERANCE;  // 60Â°
    
    return angle >= minAngle && angle <= maxAngle;
}
```

#### 3.2.2 Orientation Bonus Effects

When half-turn is detected:
1. **Control quality:** +15% effective Technique (applied in `CalculateControlQuality()`)
2. **Recognition latency:** -15% L_rec for next action (handled by Perception System, not this spec)

### 3.3 Pressure Context

#### 3.3.1 Pressure Scalar Calculation

```csharp
/// <summary>
/// Calculates pressure scalar based on nearby opponents.
/// 
/// Pressure is accumulated from all opponents within PRESSURE_RADIUS_MAX.
/// Uses inverse square falloff: closer opponents have exponentially more impact.
/// 
/// Multiple opponents stack but with diminishing returns (clamped to 1.0).
/// </summary>
/// <param name="agentPosition">Position of receiving agent</param>
/// <param name="opponents">List of opponent agents</param>
/// <returns>Pressure scalar [0.0-1.0], higher = more pressure</returns>
public float CalculatePressureScalar(Vector3 agentPosition, List<AgentState> opponents)
{
    float totalPressure = 0f;
    
    foreach (var opponent in opponents)
    {
        float distance = Vector3.Distance(agentPosition, opponent.Position);
        
        // Only consider opponents within pressure radius
        if (distance < PRESSURE_RADIUS_MAX)
        {
            // Inverse square falloff: closer = exponentially more pressure
            float normalizedDistance = distance / PRESSURE_RADIUS_MAX;
            float proximityFactor = 1.0f - normalizedDistance;
            
            // Square the factor for steeper falloff
            float pressureContribution = proximityFactor * proximityFactor;
            
            // Critical distance multiplier for very tight marking
            if (distance < PRESSURE_RADIUS_CRITICAL)
            {
                pressureContribution *= 1.5f;
            }
            
            totalPressure += pressureContribution;
        }
    }
    
    // Clamp total pressure to [0, 1]
    return Mathf.Clamp01(totalPressure);
}
```

#### 3.3.2 Pressure Query Optimization

```csharp
/// <summary>
/// Queries nearby opponents using Collision System's spatial hash.
/// 
/// Reuses existing spatial partitioning for O(1) cell lookup.
/// Only queries cells within PRESSURE_RADIUS_MAX of agent.
/// </summary>
public List<AgentState> QueryNearbyOpponents(
    Vector3 agentPosition, 
    int agentTeamID,
    ISpatialHash spatialHash)
{
    var nearbyAgents = spatialHash.QueryRadius(agentPosition, PRESSURE_RADIUS_MAX);
    
    // Filter to opponents only
    return nearbyAgents
        .Where(a => a.TeamID != agentTeamID)
        .ToList();
}
```

### 3.4 Touch Direction and Weight

#### 3.4.1 Intended Touch Direction

The agent's intended touch direction comes from their current movement command â€” where they want to go next.

```csharp
/// <summary>
/// Determines intended touch direction from agent's movement command.
/// 
/// If agent has a target position, touch direction is toward that target.
/// If no target (stationary), default to agent's facing direction.
/// </summary>
public Vector3 GetIntendedTouchDirection(
    Vector3 agentPosition,
    Vector3 agentFacing,
    MovementCommand command)
{
    if (command.HasTargetPosition)
    {
        Vector3 toTarget = command.TargetPosition - agentPosition;
        if (toTarget.sqrMagnitude > 0.01f)
        {
            return toTarget.normalized;
        }
    }
    
    // Default to facing direction
    return agentFacing.normalized;
}
```

#### 3.4.2 Touch Displacement Calculation

```csharp
/// <summary>
/// Calculates final ball displacement after touch.
/// 
/// Control quality determines:
/// - Distance: How far ball travels from agent (touch radius)
/// - Direction accuracy: How closely ball goes where agent intended
/// 
/// Poor control: Ball continues more in its original direction
/// Good control: Ball goes where agent wanted
/// </summary>
public Vector3 CalculateTouchDisplacement(
    float controlQuality,
    Vector3 intendedDirection,
    Vector3 ballApproachVector,
    Vector3 agentPosition)
{
    // Calculate touch radius from control quality
    float touchRadius = CalculateTouchRadius(controlQuality);
    
    // Direction accuracy scales with control quality
    // Perfect control: ball goes exactly where intended
    // Poor control: ball deflects toward original trajectory
    float directionAccuracy = controlQuality;
    
    // Interpolate between ball's original direction and agent's intended direction
    Vector3 deflectionDirection = ballApproachVector.normalized;
    Vector3 actualDirection = Vector3.Slerp(
        deflectionDirection,
        intendedDirection.normalized,
        directionAccuracy);
    
    // Final displacement from agent position
    return actualDirection * touchRadius;
}
```

### 3.5 Possession Transfer Logic

#### 3.5.1 Touch Result Enumeration

```csharp
/// <summary>
/// Possible outcomes of a first touch.
/// </summary>
public enum TouchResult
{
    /// <summary>
    /// Agent gains full possession. Ball "at feet".
    /// Triggers dribbling state in Agent Movement.
    /// </summary>
    CONTROLLED,
    
    /// <summary>
    /// Ball displaced but still within contest range.
    /// Either team can claim possession.
    /// </summary>
    LOOSE_BALL,
    
    /// <summary>
    /// Ball bounces away significantly.
    /// Original ball momentum partially preserved.
    /// No immediate possession change.
    /// </summary>
    DEFLECTION,
    
    /// <summary>
    /// Nearby opponent can intercept the heavy touch.
    /// Opponent gains possession if within interception radius.
    /// </summary>
    INTERCEPTION
}
```

#### 3.5.2 Possession Determination

```csharp
/// <summary>
/// Determines possession outcome based on control quality and context.
/// </summary>
public TouchResult DeterminePossession(
    float controlQuality,
    float touchRadius,
    bool hasNearbyOpponent,
    float nearestOpponentDistance)
{
    // Good control = agent keeps possession
    if (controlQuality >= GOOD_CONTROL_THRESHOLD)
    {
        return TouchResult.CONTROLLED;
    }
    
    // Poor but recoverable touch
    if (touchRadius <= INTERCEPTION_RADIUS)
    {
        // Ball within agent's reach, but opponent might contest
        if (hasNearbyOpponent && nearestOpponentDistance < touchRadius + 0.5f)
        {
            return TouchResult.LOOSE_BALL;  // 50/50 situation
        }
        return TouchResult.CONTROLLED;  // Agent recovers own touch
    }
    
    // Heavy touch - check if opponent can intercept
    if (hasNearbyOpponent && nearestOpponentDistance < touchRadius)
    {
        return TouchResult.INTERCEPTION;  // Opponent claims ball
    }
    
    // Ball goes away but no one immediately claims it
    return TouchResult.DEFLECTION;
}
```

#### 3.5.3 Integration with Ball Physics

```csharp
/// <summary>
/// Applies touch result to ball state via Ball Physics callback.
/// </summary>
public void ApplyTouchResult(
    FirstTouchResult result,
    IBallControlCallback ballPhysics,
    IAgentMovementSystem agentMovement)
{
    switch (result.PossessionOutcome)
    {
        case TouchResult.CONTROLLED:
            // Ball follows agent, low velocity relative to agent
            Vector3 controlledVelocity = result.AgentVelocity * 0.9f;
            ballPhysics.OnControlled(result.AgentID, controlledVelocity);
            
            // Activate dribbling state
            agentMovement.SetDribblingState(result.AgentID, true);
            break;
            
        case TouchResult.LOOSE_BALL:
            // Ball displaced, moving slowly
            Vector3 loosePosition = result.AgentPosition + result.BallDisplacement;
            Vector3 looseVelocity = result.BallDisplacement.normalized * 2.0f;
            ballPhysics.OnLooseBall(loosePosition, looseVelocity);
            break;
            
        case TouchResult.DEFLECTION:
            // Ball bounces away with significant velocity
            Vector3 deflectionVelocity = result.BallDisplacement.normalized * 
                                         result.IncomingBallVelocity * 0.5f;
            ballPhysics.OnDeflected(deflectionVelocity);
            break;
            
        case TouchResult.INTERCEPTION:
            // Opponent gains possession - trigger their first touch
            // (This creates a chain: original touch â†’ interception touch)
            ballPhysics.OnIntercepted(result.InterceptingAgentID);
            break;
    }
}
```

### 3.6 Edge Cases

#### 3.6.1 High-Velocity Balls (Thunderbolts)

Powerful shots and clearances (>25 m/s) are extremely difficult to control, regardless of attributes.

```csharp
/// <summary>
/// Applies thunderbolt cap for very fast incoming balls.
/// Even elite players struggle with 90+ mph shots.
/// </summary>
public float ApplyThunderboltCap(float controlQuality, float ballVelocity)
{
    if (ballVelocity > THUNDERBOLT_THRESHOLD)
    {
        // Cap control quality for very fast balls
        return Mathf.Min(controlQuality, THUNDERBOLT_QUALITY_CAP);
    }
    return controlQuality;
}
```

#### 3.6.2 Stationary Agent Receiving

Standing still to receive provides a bonus â€” the agent can set their body properly.

```csharp
/// <summary>
/// Applies stationary bonus when agent is not moving.
/// </summary>
public float ApplyStationaryBonus(float controlQuality, float agentSpeed)
{
    if (agentSpeed < 0.5f)  // Effectively stationary
    {
        return Mathf.Min(1.0f, controlQuality * STATIONARY_BONUS);
    }
    return controlQuality;
}
```

#### 3.6.3 Receiving While Sprinting

Maximum movement penalty applies when receiving at full sprint.

```csharp
/// <summary>
/// Sprint reception is particularly difficult.
/// Agent's momentum works against fine control.
/// </summary>
// Movement penalty is already factored into CalculateControlQuality()
// via the movementDifficulty term. At MAX_SPRINT_SPEED, penalty is maximum.
```

#### 3.6.4 Bouncing Ball Reception

Balls in the air (but below heading height) are harder to control.

```csharp
/// <summary>
/// Bouncing/airborne balls below control height have additional difficulty.
/// </summary>
public float ApplyBouncingPenalty(float controlQuality, float ballHeight, bool ballIsAirborne)
{
    if (ballIsAirborne && ballHeight < GROUND_CONTROL_HEIGHT)
    {
        // Bouncing ball is harder to judge
        return controlQuality * 0.85f;
    }
    return controlQuality;
}
```

#### 3.6.5 Goalkeeper First Touch

Goalkeepers use the same system for foot control but have special handling for catching (out of scope for this spec).

```csharp
/// <summary>
/// Goalkeeper first touch with feet uses normal system.
/// Catching/parrying is handled by Goalkeeper Mechanics (#10).
/// </summary>
// No special handling needed here - goalkeeper's Technique attribute
// applies normally. Catching is a separate action type.
```

### 3.7 Numerical Examples

#### 3.7.1 Elite Midfielder, Standard Pass

**Setup:**
- Technique: 17, First Touch: 18
- Ball velocity: 12 m/s
- Agent stationary (speed = 0)
- No pressure (scalar = 0)
- Half-turn orientation: Yes

**Calculation:**
```
Step 1: Effective attribute
  rawAttribute = 17 Ã— 0.7 + 18 Ã— 0.3 = 11.9 + 5.4 = 17.3
  orientationMultiplier = 1.15 (half-turn)
  effectiveAttribute = 17.3 Ã— 1.15 = 19.895
  
Step 2: Pressure (none)
  effectiveAttribute = 19.895 Ã— 1.0 = 19.895
  
Step 3: Normalize
  attributeBase = 19.895 / 20 = 0.995
  
Step 4: Velocity difficulty
  velocityDifficulty = 12 / 15 = 0.8
  
Step 5: Movement difficulty (stationary)
  movementDifficulty = 1.0 + (0 / 10) Ã— 0.4 = 1.0
  
Step 6: Control quality
  controlQuality = 0.995 / (0.8 Ã— 1.0) = 1.244 â†’ clamped to 1.0
  
Step 7: With stationary bonus
  controlQuality = min(1.0, 1.0 Ã— 1.1) = 1.0

Result: Perfect control (quality = 1.0)
Touch radius: ~0.0m (ball sticks to feet)
Outcome: CONTROLLED
```

#### 3.7.2 Average Defender, Under Pressure

**Setup:**
- Technique: 11, First Touch: 10
- Ball velocity: 18 m/s
- Agent jogging (speed = 4 m/s)
- Pressure: Two opponents at 2m and 2.5m (scalar â‰ˆ 0.45)
- Half-turn orientation: No

**Calculation:**
```
Step 1: Effective attribute
  rawAttribute = 11 Ã— 0.7 + 10 Ã— 0.3 = 7.7 + 3.0 = 10.7
  orientationMultiplier = 1.0 (no half-turn)
  effectiveAttribute = 10.7
  
Step 2: Pressure penalty
  pressurePenalty = 1.0 - (0.45 Ã— 0.4) = 0.82
  effectiveAttribute = 10.7 Ã— 0.82 = 8.774
  
Step 3: Normalize
  attributeBase = 8.774 / 20 = 0.439
  
Step 4: Velocity difficulty
  velocityDifficulty = 18 / 15 = 1.2
  
Step 5: Movement difficulty
  movementDifficulty = 1.0 + (4 / 10) Ã— 0.4 = 1.16
  
Step 6: Control quality
  controlQuality = 0.439 / (1.2 Ã— 1.16) = 0.439 / 1.392 = 0.315

Result: Heavy touch (quality = 0.315 < 0.35)
Touch radius: ~1.4m
Outcome: Likely INTERCEPTION or LOOSE_BALL
```

#### 3.7.3 Poor Technique, Thunderbolt

**Setup:**
- Technique: 8, First Touch: 7
- Ball velocity: 32 m/s (thunderbolt)
- Agent sprinting (speed = 9 m/s)
- Pressure: One opponent at 3m (scalar â‰ˆ 0.1)
- Half-turn orientation: No

**Calculation:**
```
Step 1: Effective attribute
  rawAttribute = 8 Ã— 0.7 + 7 Ã— 0.3 = 5.6 + 2.1 = 7.7
  
Step 2: Pressure penalty
  pressurePenalty = 1.0 - (0.1 Ã— 0.4) = 0.96
  effectiveAttribute = 7.7 Ã— 0.96 = 7.392
  
Step 3: Normalize
  attributeBase = 7.392 / 20 = 0.370
  
Step 4: Velocity difficulty
  velocityDifficulty = 32 / 15 = 2.133
  
Step 5: Movement difficulty
  movementDifficulty = 1.0 + (9 / 10) Ã— 0.4 = 1.36
  
Step 6: Control quality (pre-cap)
  controlQuality = 0.370 / (2.133 Ã— 1.36) = 0.370 / 2.901 = 0.127
  
Step 7: Thunderbolt cap (32 > 25 m/s)
  controlQuality = min(0.127, 0.30) = 0.127

Result: Very heavy touch (quality = 0.127)
Touch radius: ~1.85m
Outcome: DEFLECTION or INTERCEPTION
```

---

## SECTION 4: DATA STRUCTURES (~4 pages)

### 4.1 File Organization

```
FirstTouchMechanics/
â”œâ”€â”€ FirstTouchSystem.cs              // Main system coordinator
â”œâ”€â”€ ControlQualityCalculator.cs      // Formula implementation (Â§3.1)
â”œâ”€â”€ PressureEvaluator.cs             // Pressure context (Â§3.3)
â”œâ”€â”€ TouchResultProcessor.cs          // Possession logic (Â§3.5)
â”œâ”€â”€ FirstTouchConstants.cs           // All constants (Â§3.1.2)
â”œâ”€â”€ FirstTouchDataStructures.cs      // Structs defined below
â”œâ”€â”€ FirstTouchEvents.cs              // Event structs for logging
â””â”€â”€ Tests/
    â”œâ”€â”€ ControlQualityTests.cs
    â”œâ”€â”€ TouchRadiusTests.cs
    â”œâ”€â”€ PressureTests.cs
    â”œâ”€â”€ PossessionTests.cs
    â””â”€â”€ IntegrationTests.cs
```

### 4.2 Core Structs

#### 4.2.1 FirstTouchContext

```csharp
/// <summary>
/// Input context for evaluating a first touch.
/// Aggregates all data needed from external systems.
/// </summary>
public struct FirstTouchContext
{
    // Agent identification
    public int AgentID;
    public int TeamID;
    
    // Agent attributes
    public int Technique;
    public int FirstTouch;
    
    // Agent state
    public Vector3 AgentPosition;
    public Vector3 AgentVelocity;
    public Vector3 AgentFacing;
    
    // Movement intent
    public Vector3 IntendedTouchDirection;
    public bool HasMovementTarget;
    
    // Ball state at contact
    public Vector3 BallPosition;
    public Vector3 BallVelocity;
    public float BallHeight;
    public bool BallIsAirborne;
    
    // Pressure context
    public float PressureScalar;
    public bool HasNearbyOpponent;
    public float NearestOpponentDistance;
    
    // Orientation
    public bool IsHalfTurnOriented;
    
    // Flags
    public bool IsGoalkeeper;
}
```

#### 4.2.2 FirstTouchResult

```csharp
/// <summary>
/// Output of first touch evaluation.
/// Contains all data needed by Ball Physics and Agent Movement.
/// </summary>
public struct FirstTouchResult
{
    // Quality metrics
    public float ControlQuality;
    public float TouchRadius;
    
    // Ball outcome
    public Vector3 BallDisplacement;
    public Vector3 NewBallPosition;
    public Vector3 NewBallVelocity;
    
    // Possession outcome
    public TouchResult PossessionOutcome;
    public int PossessingAgentID;      // -1 if no one possesses
    public int InterceptingAgentID;     // -1 if no interception
    
    // State changes
    public bool TriggeredDribblingState;
    
    // Debug/logging
    public float IncomingBallVelocity;
    public float EffectiveAttribute;
}
```

#### 4.2.3 FirstTouchEvent

```csharp
/// <summary>
/// Event data for logging, replay, and statistics.
/// Published to Event System after each first touch.
/// </summary>
public struct FirstTouchEvent
{
    // Timing
    public int FrameNumber;
    public float MatchTime;
    
    // Agent
    public int AgentID;
    public int TeamID;
    
    // Touch details
    public float IncomingBallVelocity;
    public float ControlQuality;
    public float TouchRadius;
    public TouchResult Outcome;
    
    // Positions
    public Vector3 TouchPosition;
    public Vector3 BallEndPosition;
    
    // Context
    public float PressureScalar;
    public bool WasHalfTurn;
    public bool WasThunderbolt;
    
    // Derived stats
    public bool ResultedInPossessionChange;
    public int NewPossessingTeamID;
}
```

### 4.3 Interface Contracts

#### 4.3.1 Input from Collision System

First Touch receives contact data from Collision System via the `AgentBallCollisionData` struct (defined in Collision System Spec Â§3.3.4):

```csharp
// From Collision System Spec Â§3.3.4
public struct AgentBallCollisionData
{
    public Vector3 ContactPoint;
    public Vector3 AgentVelocity;
    public BodyPart BodyPart;        // Stage 0: Always TORSO
    public int AgentID;
    public int TeamID;
    public bool IsGoalkeeper;
}
```

#### 4.3.2 Output to Ball Physics

```csharp
/// <summary>
/// Callback interface for Ball Physics to receive touch results.
/// </summary>
public interface IBallControlCallback
{
    /// <summary>
    /// Agent gains possession. Ball velocity matches agent.
    /// </summary>
    void OnControlled(int agentID, Vector3 newBallVelocity);
    
    /// <summary>
    /// Ball deflects away. Partial momentum preserved.
    /// </summary>
    void OnDeflected(Vector3 deflectionVelocity);
    
    /// <summary>
    /// Ball is loose. Low velocity, contestable.
    /// </summary>
    void OnLooseBall(Vector3 ballPosition, Vector3 ballVelocity);
    
    /// <summary>
    /// Opponent intercepts heavy touch.
    /// Triggers their first touch evaluation.
    /// </summary>
    void OnIntercepted(int interceptingAgentID);
}
```

#### 4.3.3 Output to Agent Movement

```csharp
/// <summary>
/// Interface for activating dribbling state.
/// </summary>
public interface IAgentMovementSystem
{
    /// <summary>
    /// Sets dribbling state for agent.
    /// When true, DribblingModifier is applied to locomotion.
    /// </summary>
    void SetDribblingState(int agentID, bool isDribbling);
}
```

### 4.4 System Interface

```csharp
/// <summary>
/// Main entry point for First Touch evaluation.
/// Called by Collision System when agent-ball contact detected.
/// </summary>
public interface IFirstTouchSystem
{
    /// <summary>
    /// Evaluates first touch and returns result.
    /// </summary>
    FirstTouchResult EvaluateFirstTouch(FirstTouchContext context);
    
    /// <summary>
    /// Applies touch result to game state.
    /// </summary>
    void ApplyTouchResult(FirstTouchResult result);
}
```

---

## SECTION 5: TESTING (~5 pages)

### 5.1 Unit Tests

#### 5.1.1 Control Quality Tests (CQ-001 to CQ-012)

| Test ID | Description | Input | Expected Output | Tolerance |
|---------|-------------|-------|-----------------|-----------|
| CQ-001 | Elite player, standard pass | Tech=17, FT=18, v=12, stationary, no pressure | Quality â‰¥ 0.95 | Â±0.03 |
| CQ-002 | Average player, standard pass | Tech=12, FT=11, v=15, walking, low pressure | Quality 0.55-0.65 | Â±0.05 |
| CQ-003 | Poor player, fast pass | Tech=7, FT=6, v=22, jogging, medium pressure | Quality 0.20-0.35 | Â±0.05 |
| CQ-004 | Any player, thunderbolt | Tech=18, FT=19, v=35 | Quality â‰¤ 0.30 (capped) | exact |
| CQ-005 | Stationary reception bonus | Tech=12, v=12, speed=0 | Quality Ã— 1.1 | Â±0.02 |
| CQ-006 | Sprint reception penalty | Tech=12, v=12, speed=9.5 | Quality reduced ~30% | Â±0.05 |
| CQ-007 | Half-turn orientation bonus | Tech=12, half-turn=true | Quality Ã— 1.15 | Â±0.02 |
| CQ-008 | Maximum pressure degradation | Tech=15, pressure=1.0 | Quality reduced ~40% | Â±0.05 |
| CQ-009 | Zero pressure (open space) | Tech=12, pressure=0 | No penalty applied | exact |
| CQ-010 | Minimum attributes (1,1) | Tech=1, FT=1, v=15 | Quality 0.03-0.08 | Â±0.02 |
| CQ-011 | Maximum attributes (20,20) | Tech=20, FT=20, v=15 | Quality 0.90-1.0 | Â±0.03 |
| CQ-012 | Bouncing ball penalty | Tech=12, airborne=true | Quality Ã— 0.85 | Â±0.02 |

#### 5.1.2 Touch Radius Tests (TR-001 to TR-006)

| Test ID | Description | Input Quality | Expected Radius | Tolerance |
|---------|-------------|---------------|-----------------|-----------|
| TR-001 | Perfect control | 0.95 | 0.0-0.15m | Â±0.05m |
| TR-002 | Good control threshold | 0.85 | ~0.30m | Â±0.05m |
| TR-003 | Good control mid-range | 0.72 | 0.40-0.50m | Â±0.05m |
| TR-004 | Poor control threshold | 0.60 | ~0.60m | Â±0.05m |
| TR-005 | Heavy touch threshold | 0.35 | ~1.20m | Â±0.10m |
| TR-006 | Very heavy touch | 0.10 | 1.70-1.95m | Â±0.10m |

#### 5.1.3 Pressure Tests (PR-001 to PR-005)

| Test ID | Description | Setup | Expected Scalar | Tolerance |
|---------|-------------|-------|-----------------|-----------|
| PR-001 | No opponents nearby | All opponents > 3m | 0.0 | exact |
| PR-002 | One opponent at edge | 1 opponent at 2.9m | 0.01-0.05 | Â±0.02 |
| PR-003 | One opponent close | 1 opponent at 1.5m | 0.20-0.35 | Â±0.05 |
| PR-004 | Tight marking | 1 opponent at 0.8m | 0.60-0.80 | Â±0.10 |
| PR-005 | Multiple opponents | 2 opponents at 1.5m, 2.0m | 0.40-0.60 | Â±0.10 |

#### 5.1.4 Orientation Tests (OR-001 to OR-004)

| Test ID | Description | Agent Facing | Ball Approach | Expected |
|---------|-------------|--------------|---------------|----------|
| OR-001 | Perfect half-turn | (1,0,0) | (-0.707,-0.707,0) | IsHalfTurn = true |
| OR-002 | Facing ball directly | (1,0,0) | (-1,0,0) | IsHalfTurn = false |
| OR-003 | Back to ball | (1,0,0) | (1,0,0) | IsHalfTurn = false |
| OR-004 | Edge of tolerance | 31Â° angle | â€” | IsHalfTurn = true |

#### 5.1.5 Possession Tests (PO-001 to PO-006)

| Test ID | Description | Quality | Radius | Opponent | Expected |
|---------|-------------|---------|--------|----------|----------|
| PO-001 | Good control, no pressure | 0.75 | 0.45m | None | CONTROLLED |
| PO-002 | Good control, distant opponent | 0.70 | 0.50m | 2.5m away | CONTROLLED |
| PO-003 | Poor control, contested | 0.45 | 0.90m | 1.0m away | LOOSE_BALL |
| PO-004 | Heavy touch, interception | 0.25 | 1.50m | 1.2m away | INTERCEPTION |
| PO-005 | Heavy touch, no opponent | 0.20 | 1.70m | None | DEFLECTION |
| PO-006 | Marginal control, tight marking | 0.58 | 0.65m | 0.8m away | LOOSE_BALL |

#### 5.1.6 Edge Case Tests (EC-001 to EC-008)

| Test ID | Description | Input | Expected Behavior |
|---------|-------------|-------|-------------------|
| EC-001 | NaN velocity | ballVelocity = NaN | Clamp to 0, log warning |
| EC-002 | Zero velocity ball | ballVelocity = 0 | Use MIN_VELOCITY (1.0) |
| EC-003 | Negative attributes | Tech = -5 | Clamp to [1, 20] |
| EC-004 | Attributes > 20 | Tech = 25 | Clamp to [1, 20] |
| EC-005 | Extreme ball velocity | v = 60 m/s | Cap at MAX_BALL_VELOCITY |
| EC-006 | Agent at pitch boundary | Position = (0, 0, 0) | Allow touch, clamp result |
| EC-007 | Ball below ground | Height = -0.5m | Treat as ground level |
| EC-008 | Simultaneous touches | 2 agents contact same frame | Process primary contact only |

### 5.2 Integration Tests

| Test ID | Description | Systems Involved | Validation |
|---------|-------------|------------------|------------|
| IT-001 | Collision â†’ First Touch â†’ Ball Physics | All three | Ball state updated correctly |
| IT-002 | Possession change updates team state | First Touch, Match State | Team possession flag changes |
| IT-003 | Dribbling modifier activation | First Touch, Agent Movement | DribblingModifier set when CONTROLLED |
| IT-004 | Event logging | First Touch, Event System | FirstTouchEvent published |
| IT-005 | Replay determinism | All systems | Same inputs â†’ identical results |
| IT-006 | Goalkeeper foot control | First Touch, Goalkeeper flag | Normal processing for foot contact |
| IT-007 | Pressure query from spatial hash | First Touch, Collision System | Correct opponents returned |
| IT-008 | Consecutive touches (one-two) | First Touch (Ã—2), Ball Physics | Chain of touches works |

### 5.3 Validation Scenarios

#### VS-001: Mesut Ã–zil Receiving a Pass

**Scenario:** Elite playmaker receives a pass in space.
- Technique: 18, First Touch: 19
- Ball velocity: 14 m/s
- Agent jogging toward ball (4 m/s)
- Half-turn oriented
- One opponent 4m away (outside pressure radius)

**Expected:** Control quality ~0.95, ball within 0.2m, CONTROLLED outcome.

#### VS-002: Target Man Chest Control

**Scenario:** Physical striker receives long ball under pressure.
- Technique: 12, First Touch: 11
- Ball velocity: 22 m/s (long ball)
- Agent stationary, bracing for ball
- Two opponents at 1.5m and 2.0m

**Expected:** Control quality ~0.40, ball within 1.0m, LOOSE_BALL or CONTROLLED depending on direction.

#### VS-003: Defender Clearing Under Pressure

**Scenario:** Center-back receives back pass with striker pressing.
- Technique: 10, First Touch: 9
- Ball velocity: 18 m/s
- Agent moving backward (3 m/s)
- Striker 1.0m away, closing

**Expected:** Control quality ~0.25, ball escapes 1.5m+, likely INTERCEPTION or must clear first time.

---

## SECTION 6: PERFORMANCE ANALYSIS (~2 pages)

### 6.1 Computational Complexity

| Operation | Complexity | Notes |
|-----------|------------|-------|
| Control quality calculation | O(1) | Fixed arithmetic operations |
| Touch radius calculation | O(1) | Piecewise linear interpolation |
| Pressure scalar calculation | O(n) | n = opponents in pressure radius (typically 0-4) |
| Orientation check | O(1) | Single dot product |
| Possession determination | O(1) | Threshold comparisons |
| **Total per touch** | O(n) | Dominated by pressure query |

### 6.2 Operation Counts

```
Per first touch evaluation:
- Attribute calculation: 6 ops (multiply, add)
- Orientation check: 8 ops (dot product, comparison)
- Pressure query: ~20 ops per nearby opponent
- Velocity difficulty: 2 ops
- Movement difficulty: 4 ops
- Quality calculation: 6 ops
- Touch radius: 8 ops (interpolation)
- Displacement: 15 ops (vector math)
- Possession logic: 5 ops

Total: ~50 ops + 20 Ã— (nearby opponents)
Typical case (2 opponents): ~90 ops
```

### 6.3 Performance Budget

| Metric | Target | Rationale |
|--------|--------|-----------|
| Per-touch latency | <0.05ms | Touch is discrete event, not per-frame |
| Touches per match | 200-400 | Based on real match statistics |
| Total match impact | <20ms | Spread across 90 minutes |
| Frame impact | <0.01ms | Touches happen ~3-4 per second average |

### 6.4 Memory Footprint

| Structure | Size | Allocation |
|-----------|------|------------|
| FirstTouchContext | 128 bytes | Stack (per evaluation) |
| FirstTouchResult | 80 bytes | Stack (per evaluation) |
| FirstTouchEvent | 96 bytes | Event queue (pooled) |
| Constants | 200 bytes | Static (one-time) |

**Total runtime allocation:** Zero per-frame allocations in hot path. All structs are stack-allocated or pooled.

### 6.5 Profiling Markers

```csharp
Profiler.BeginSample("FirstTouch.Evaluate");
// ... evaluation code ...
Profiler.EndSample();

Profiler.BeginSample("FirstTouch.PressureQuery");
// ... pressure calculation ...
Profiler.EndSample();
```

---

