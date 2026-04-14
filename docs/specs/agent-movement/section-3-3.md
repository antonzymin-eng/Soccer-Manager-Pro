# Agent Movement Specification â€” Section 3.3: Directional Movement

**Purpose:** Defines the speed multipliers applied when an agent moves in a direction other than the direction they are facing. Covers angle zone definitions, Agility-scaled multiplier ranges, facing direction independence from movement vector, and integration with the locomotion model (Section 3.2).

**Created:** February 9, 2026, 5:00 PM PST  
**Version:** 1.0  
**Status:** Draft  
**Dependencies:** Section 3.1 (State Machine), Section 3.2 (Locomotion & PerformanceContext)

---

## Table of Contents

- [3.3.1 Design Philosophy](#331-design-philosophy)
- [3.3.2 Direction Zones](#332-direction-zones)
- [3.3.3 Multiplier Calculation](#333-multiplier-calculation)
- [3.3.4 Facing Direction System](#334-facing-direction-system)
- [3.3.5 Integration with Locomotion](#335-integration-with-locomotion)
- [3.3.6 Numerical Examples & Validation](#336-numerical-examples--validation)

---

## 3.3.1 Design Philosophy

Football players move in all directions relative to where they're looking. A centre-back tracking a runner moves backward while watching the ball. A midfielder jockeying an opponent shuffles laterally. These non-forward movements are slower than forward sprinting â€” constrained by biomechanics (hip rotation, stride mechanics, visual orientation).

The directional multiplier system captures this by reducing top speed and acceleration rate when the angle between movement direction and facing direction increases. The reduction is Agility-scaled: agile players lose less speed when moving sideways or backward, reflecting superior hip mobility, footwork, and body coordination.

**Key design decisions:**

- **Three discrete zones** (forward, lateral, backward) with smooth interpolation at boundaries, rather than a continuous function. This matches how players actually move â€” there are distinct biomechanical modes (running forward, shuffling sideways, backpedaling), not a smooth continuum.
- **Facing direction is independent from movement direction.** An agent can face north while moving east (lateral shuffle) or face north while moving south (backpedal). This is critical for defensive behavior, receiving the ball while checking shoulders, and jockeying.
- **Multipliers apply to both top speed and acceleration rate** (FR-4). Moving backward is not just slower â€” it takes longer to reach that slower speed.
- **Only active in states with voluntary control** (Section 3.1.6): WALKING, JOGGING, SPRINTING, DECELERATING. Not applied in IDLE (no movement), STUMBLING, or GROUNDED (momentum-only).

---

## 3.3.2 Direction Zones

### Zone Definitions

The angle between movement direction and facing direction determines which zone the agent is in:

```
                 Facing Direction
                       â†‘
                       |
              â†30Â°     |     30Â°â†’
             /         |         \
            /  FORWARD |  FORWARD \
           /    (1.0Ã—) |   (1.0Ã—)  \
    â”€â”€â”€â”€â”€â”€/â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€\â”€â”€â”€â”€â”€â”€
         /             |             \
        /   LATERAL    |    LATERAL   \
       /  (0.65-0.75Ã—) | (0.65-0.75Ã—) \
  90Â°â†/â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€\â†’90Â°
      \                |                /
       \   BACKWARD    |   BACKWARD   /
        \ (0.45-0.55Ã—) | (0.45-0.55Ã—)/
         \             |             /
    â”€â”€â”€â”€â”€â”€\â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€/â”€â”€â”€â”€â”€â”€
           \           |           /
            â†150Â°      |      150Â°â†’
                       |
                    180Â° (directly backward)
```

### Zone Boundaries and Interpolation

```csharp
/// <summary>
/// Directional movement zone boundaries and multiplier ranges.
/// All angles in degrees, measured as the absolute angle between
/// the agent's facing direction and movement direction in the XY plane.
///
/// Zone boundaries use interpolation bands to prevent hard discontinuities
/// where a 1-degree change in direction would cause a sudden speed change.
/// Within an interpolation band, the multiplier transitions smoothly (linearly)
/// between the two adjacent zone values.
///
/// Design note: Interpolation bands are 10Â° wide. At typical turn rates
/// (Section 3.4), an agent sweeps through 10Â° in roughly 50â€“100ms,
/// so the transition is imperceptible during normal play but prevents
/// visual stuttering if an agent oscillates near a zone boundary.
/// </summary>
public static class DirectionalConstants
{
    // ================================================================
    // ZONE BOUNDARIES (degrees)
    // ================================================================

    /// <summary>
    /// Forward zone: 0Â° to FORWARD_LIMIT.
    /// Full speed â€” agent is running in approximately the direction they face.
    /// Â±30Â° chosen because biomechanically, the running gait is minimally
    /// affected by slight direction offsets up to about 30Â° (Dos'Santos et al., 2019).
    /// </summary>
    public const float FORWARD_LIMIT = 30.0f;

    /// <summary>
    /// Start of forward-to-lateral interpolation band.
    /// From FORWARD_LIMIT to LATERAL_START, multiplier transitions
    /// from 1.0 to the lateral value.
    /// </summary>
    public const float LATERAL_START = 40.0f;

    /// <summary>
    /// Lateral zone: LATERAL_START to LATERAL_LIMIT.
    /// Reduced speed â€” agent is shuffling sideways.
    /// Â±90Â° is the natural boundary where lateral shuffle transitions
    /// to backward movement (hip rotation reversal).
    /// </summary>
    public const float LATERAL_LIMIT = 80.0f;

    /// <summary>
    /// Start of lateral-to-backward interpolation band.
    /// From LATERAL_LIMIT to BACKWARD_START, multiplier transitions
    /// from lateral value to backward value.
    /// </summary>
    public const float BACKWARD_START = 90.0f;

    /// <summary>
    /// Backward zone: BACKWARD_START to 180Â°.
    /// Minimum speed â€” agent is backpedaling.
    /// The multiplier is constant from 90Â° to 180Â° because biomechanically,
    /// backward running speed doesn't significantly decrease whether you're
    /// moving at 100Â° or 170Â° relative to facing â€” you're backpedaling
    /// either way.
    /// </summary>
    public const float BACKWARD_FULL = 90.0f;

    // ================================================================
    // MULTIPLIER RANGES (Agility-scaled)
    // ================================================================

    /// <summary>Forward zone multiplier. Always 1.0 â€” no penalty for forward movement.</summary>
    public const float FORWARD_MULTIPLIER = 1.0f;

    /// <summary>
    /// Lateral multiplier at Agility = 1 (worst lateral movement).
    /// 0.65Ã— means a Pace 20 player (10.2 m/s forward) moves at 6.63 m/s sideways.
    ///
    /// Derivation: Sports science literature reports lateral shuffle speeds
    /// at approximately 55â€“75% of forward sprint speed for team sport athletes
    /// (Nimphius et al., 2016). 0.65 represents the low end for a stiff,
    /// non-agile player.
    /// </summary>
    public const float LATERAL_MULT_MIN = 0.65f;

    /// <summary>
    /// Lateral multiplier at Agility = 20 (best lateral movement).
    /// 0.75Ã— means a Pace 20 player (10.2 m/s forward) moves at 7.65 m/s sideways.
    ///
    /// 0.75 represents an elite-agility player â€” someone like N'Golo KantÃ©
    /// or Lionel Messi who barely loses speed when changing direction.
    /// </summary>
    public const float LATERAL_MULT_MAX = 0.75f;

    /// <summary>Per-attribute-point lateral multiplier increase.
    /// (0.75 - 0.65) / 19 = 0.005263 per point.</summary>
    public const float LATERAL_MULT_PER_POINT =
        (LATERAL_MULT_MAX - LATERAL_MULT_MIN) / 19.0f;

    /// <summary>
    /// Backward multiplier at Agility = 1 (worst backward movement).
    /// 0.45Ã— means a Pace 20 player (10.2 m/s forward) moves at 4.59 m/s backward.
    ///
    /// Derivation: Backward running speed is approximately 40â€“55% of forward
    /// sprint speed (Hewit et al., 2011). 0.45 represents a stiff, heavy
    /// centre-back backpedaling â€” significantly compromised.
    /// </summary>
    public const float BACKWARD_MULT_MIN = 0.45f;

    /// <summary>
    /// Backward multiplier at Agility = 20 (best backward movement).
    /// 0.55Ã— means a Pace 20 player (10.2 m/s forward) moves at 5.61 m/s backward.
    ///
    /// 0.55 represents an elite defender or goalkeeper with exceptional
    /// backward movement â€” but still substantially slower than forward.
    /// </summary>
    public const float BACKWARD_MULT_MAX = 0.55f;

    /// <summary>Per-attribute-point backward multiplier increase.
    /// (0.55 - 0.45) / 19 = 0.005263 per point.</summary>
    public const float BACKWARD_MULT_PER_POINT =
        (BACKWARD_MULT_MAX - BACKWARD_MULT_MIN) / 19.0f;
}
```

### Zone Summary Table

| Zone | Angle Range | Multiplier Range | Agility Scaling |
|---|---|---|---|
| Forward | 0Â°â€“30Â° | 1.0 (fixed) | None |
| Forwardâ†’Lateral transition | 30Â°â€“40Â° | 1.0 â†’ lateral (interpolated) | Yes (target depends on Agility) |
| Lateral | 40Â°â€“80Â° | 0.65â€“0.75 | Yes (linear Agility 1â†’20) |
| Lateralâ†’Backward transition | 80Â°â€“90Â° | lateral â†’ backward (interpolated) | Yes (both ends depend on Agility) |
| Backward | 90Â°â€“180Â° | 0.45â€“0.55 | Yes (linear Agility 1â†’20) |

---

## 3.3.3 Multiplier Calculation

### Core Function

```csharp
/// <summary>
/// Computes the directional speed multiplier based on the angle between
/// movement direction and facing direction.
///
/// This is the AUTHORITATIVE directional multiplier calculation.
/// Called by CalculateEffectiveTopSpeed() (Section 3.2.4) and also
/// applied to acceleration rate (Section 3.2.3).
///
/// The function uses Agility (through PerformanceContext) to determine
/// the lateral and backward multiplier values, then selects and
/// interpolates based on the movement angle.
///
/// INPUT: movementAngle is the ABSOLUTE angle (0Â°â€“180Â°) between facing
/// direction and movement direction, computed by GetMovementAngle().
/// Negative angles are not used â€” the system is symmetric (moving 30Â°
/// left of facing is identical to 30Â° right of facing).
///
/// INPUT: effectiveAgility is the Agility attribute after PerformanceContext
/// evaluation. This allows form/context/career modifiers to affect not
/// just how fast a player runs, but how well they move laterally â€”
/// a player in poor form loses lateral agility too.
///
/// OUTPUT: Multiplier in range [BACKWARD_MULT_MIN, FORWARD_MULTIPLIER]
/// i.e., [0.45, 1.0]. Applied to both top speed and acceleration k.
/// </summary>
/// <param name="movementAngle">Absolute angle between facing and movement (0Â°â€“180Â°)</param>
/// <param name="effectiveAgility">Agility after PerformanceContext (float, typically 1â€“20)</param>
/// <returns>Speed multiplier (0.45â€“1.0)</returns>
public static float CalculateDirectionalMultiplier(
    float movementAngle,
    float effectiveAgility)
{
    // Clamp angle to valid range
    movementAngle = Mathf.Clamp(movementAngle, 0f, 180f);

    // Compute Agility-scaled zone multipliers
    float lateralMult = DirectionalConstants.LATERAL_MULT_MIN
                      + (effectiveAgility - 1.0f) * DirectionalConstants.LATERAL_MULT_PER_POINT;
    lateralMult = Mathf.Clamp(lateralMult, DirectionalConstants.LATERAL_MULT_MIN,
                              DirectionalConstants.LATERAL_MULT_MAX);

    float backwardMult = DirectionalConstants.BACKWARD_MULT_MIN
                       + (effectiveAgility - 1.0f) * DirectionalConstants.BACKWARD_MULT_PER_POINT;
    backwardMult = Mathf.Clamp(backwardMult, DirectionalConstants.BACKWARD_MULT_MIN,
                               DirectionalConstants.BACKWARD_MULT_MAX);

    // Forward zone: 0Â° to FORWARD_LIMIT â€” full speed
    if (movementAngle <= DirectionalConstants.FORWARD_LIMIT)
    {
        return DirectionalConstants.FORWARD_MULTIPLIER;
    }

    // Forwardâ†’Lateral interpolation: FORWARD_LIMIT to LATERAL_START
    if (movementAngle <= DirectionalConstants.LATERAL_START)
    {
        float t = (movementAngle - DirectionalConstants.FORWARD_LIMIT)
                / (DirectionalConstants.LATERAL_START - DirectionalConstants.FORWARD_LIMIT);
        return Mathf.Lerp(DirectionalConstants.FORWARD_MULTIPLIER, lateralMult, t);
    }

    // Lateral zone: LATERAL_START to LATERAL_LIMIT â€” Agility-scaled
    if (movementAngle <= DirectionalConstants.LATERAL_LIMIT)
    {
        return lateralMult;
    }

    // Lateralâ†’Backward interpolation: LATERAL_LIMIT to BACKWARD_START
    if (movementAngle <= DirectionalConstants.BACKWARD_START)
    {
        float t = (movementAngle - DirectionalConstants.LATERAL_LIMIT)
                / (DirectionalConstants.BACKWARD_START - DirectionalConstants.LATERAL_LIMIT);
        return Mathf.Lerp(lateralMult, backwardMult, t);
    }

    // Backward zone: BACKWARD_START to 180Â° â€” Agility-scaled, constant
    return backwardMult;
}
```

### Angle Computation

```csharp
/// <summary>
/// Computes the absolute angle (0Â°â€“180Â°) between the agent's facing
/// direction and their movement direction.
///
/// Both vectors are in the XY plane (Z component ignored for outfield players).
/// Uses the dot product formula: cos(Î¸) = (a Â· b) / (|a| Ã— |b|)
///
/// Returns 0Â° if movement velocity is below MIN_SPEED (agent is effectively
/// stationary â€” no meaningful movement direction exists).
///
/// NOTE: This function computes the angle between facing and movement,
/// NOT the angle of movement in world space. An agent facing north and
/// moving east produces 90Â° (lateral), regardless of where north is
/// on the pitch.
/// </summary>
/// <param name="facingDirection">Agent's facing direction (unit vector, XY plane)</param>
/// <param name="movementDirection">Agent's current velocity direction (not necessarily unit length)</param>
/// <param name="movementSpeed">Agent's current speed magnitude (m/s)</param>
/// <returns>Absolute angle in degrees (0Â°â€“180Â°)</returns>
public static float GetMovementAngle(
    Vector2 facingDirection,
    Vector2 movementDirection,
    float movementSpeed)
{
    // No meaningful direction at very low speeds â€” treat as forward
    if (movementSpeed < MovementConstants.MIN_SPEED)
    {
        return 0f;
    }

    // Normalize movement direction (facing should already be unit length)
    Vector2 moveNorm = movementDirection.normalized;

    // Dot product gives cos(angle)
    float dot = Vector2.Dot(facingDirection, moveNorm);

    // Clamp to [-1, 1] to guard against floating-point error in Acos
    dot = Mathf.Clamp(dot, -1f, 1f);

    // Convert to degrees
    return Mathf.Acos(dot) * Mathf.Rad2Deg;
}
```

### Multiplier Reference Table

| Angle | Zone | Agility 1 | Agility 5 | Agility 10 | Agility 15 | Agility 20 |
|---|---|---|---|---|---|---|
| 0Â° | Forward | 1.000 | 1.000 | 1.000 | 1.000 | 1.000 |
| 15Â° | Forward | 1.000 | 1.000 | 1.000 | 1.000 | 1.000 |
| 30Â° | Forward | 1.000 | 1.000 | 1.000 | 1.000 | 1.000 |
| 35Â° | Fwdâ†’Lat | 0.825 | 0.836 | 0.849 | 0.862 | 0.875 |
| 40Â° | Lateral | 0.650 | 0.671 | 0.697 | 0.724 | 0.750 |
| 60Â° | Lateral | 0.650 | 0.671 | 0.697 | 0.724 | 0.750 |
| 80Â° | Lateral | 0.650 | 0.671 | 0.697 | 0.724 | 0.750 |
| 85Â° | Latâ†’Bwd | 0.550 | 0.571 | 0.597 | 0.624 | 0.650 |
| 90Â° | Backward | 0.450 | 0.471 | 0.497 | 0.524 | 0.550 |
| 120Â° | Backward | 0.450 | 0.471 | 0.497 | 0.524 | 0.550 |
| 180Â° | Backward | 0.450 | 0.471 | 0.497 | 0.524 | 0.550 |

**Derivation for Agility 5:** effectiveAgility = 5.0, lateral = 0.65 + 4.0 Ã— 0.005263 = 0.671, backward = 0.45 + 4.0 Ã— 0.005263 = 0.471.

**Derivation for 35Â° (midpoint of 30Â°â€“40Â° interpolation band), Agility 1:**
t = (35 - 30) / (40 - 30) = 0.5, Lerp(1.0, 0.65, 0.5) = 0.825

**Derivation for 85Â° (midpoint of 80Â°â€“90Â° interpolation band), Agility 10:**
t = (85 - 80) / (90 - 80) = 0.5, Lerp(0.697, 0.497, 0.5) = 0.597

### Zone Hysteresis for Downstream Consumers

The directional multiplier itself transitions smoothly via interpolation bands â€” no hysteresis needed for the physics value. However, downstream systems (primarily animation selection, audio cues, and UI state display) need to know which **discrete zone** the agent is in. Without hysteresis, an agent oscillating at exactly 30Â° would flip between FORWARD and FORWARD_TO_LATERAL every frame, causing animation flickering.

```csharp
/// <summary>
/// Discrete direction zone for downstream consumers (animation, audio, UI).
/// The physics layer does NOT use this â€” it uses the continuous multiplier
/// from CalculateDirectionalMultiplier(). This enum exists solely to provide
/// a stable discrete signal for systems that need one.
/// </summary>
public enum DirectionZone
{
    FORWARD,
    LATERAL,
    BACKWARD
}

/// <summary>
/// Computes the current discrete direction zone with hysteresis.
///
/// Hysteresis prevents rapid zone flipping when the movement angle
/// oscillates near a zone boundary. The zone only changes when the
/// angle moves HYSTERESIS_MARGIN degrees beyond the boundary into
/// the new zone. This creates a "sticky" region where the previous
/// zone is maintained.
///
/// Example at the Forwardâ†’Lateral boundary (nominal: 35Â°):
///   Currently FORWARD: stays FORWARD until angle exceeds 35Â° + 3Â° = 38Â°
///   Currently LATERAL: stays LATERAL until angle drops below 35Â° - 3Â° = 32Â°
///   Dead zone: 32Â°â€“38Â° maintains whichever zone was active
///
/// HYSTERESIS_MARGIN of 3Â° was chosen because:
/// 1. At typical movement speeds, 3Â° of angular change takes 2â€“4 frames,
///    so the dead zone is traversed quickly during intentional direction changes
/// 2. Small enough to not visibly delay animation transitions
/// 3. Large enough to suppress oscillation from physics noise / path corrections
///
/// NOTE: The hysteresis zone boundaries (35Â° and 85Â°) are the midpoints
/// of the interpolation bands (30Â°â€“40Â° and 80Â°â€“90Â°), which is where the
/// visual distinction between movement modes is most ambiguous â€” exactly
/// where flickering would be most distracting.
/// </summary>
public static class DirectionZoneConstants
{
    /// <summary>Nominal boundary between Forward and Lateral zones.
    /// Midpoint of the 30Â°â€“40Â° interpolation band.</summary>
    public const float FORWARD_LATERAL_BOUNDARY = 35.0f;

    /// <summary>Nominal boundary between Lateral and Backward zones.
    /// Midpoint of the 80Â°â€“90Â° interpolation band.</summary>
    public const float LATERAL_BACKWARD_BOUNDARY = 85.0f;

    /// <summary>Hysteresis margin (degrees) on each side of the boundary.</summary>
    public const float HYSTERESIS_MARGIN = 3.0f;
}

/// <summary>
/// Updates the discrete direction zone with hysteresis.
///
/// Called once per frame after CalculateDirectionalMultiplier().
/// The returned zone is used by animation, audio, and UI systems ONLY.
/// It has no effect on physics calculations.
///
/// previousZone: The zone from the last frame (stored in agent state).
/// movementAngle: Current absolute angle from GetMovementAngle().
/// Returns: The (possibly unchanged) discrete zone for this frame.
/// </summary>
public static DirectionZone UpdateDirectionZone(
    DirectionZone previousZone,
    float movementAngle)
{
    float fwdLatBoundary = DirectionZoneConstants.FORWARD_LATERAL_BOUNDARY;
    float latBwdBoundary = DirectionZoneConstants.LATERAL_BACKWARD_BOUNDARY;
    float margin = DirectionZoneConstants.HYSTERESIS_MARGIN;

    switch (previousZone)
    {
        case DirectionZone.FORWARD:
            // Stay FORWARD unless angle exceeds boundary + margin
            if (movementAngle > fwdLatBoundary + margin)
            {
                // Check if we jumped all the way to backward
                if (movementAngle > latBwdBoundary + margin)
                    return DirectionZone.BACKWARD;
                return DirectionZone.LATERAL;
            }
            return DirectionZone.FORWARD;

        case DirectionZone.LATERAL:
            // Drop to FORWARD if below boundary - margin
            if (movementAngle < fwdLatBoundary - margin)
                return DirectionZone.FORWARD;
            // Rise to BACKWARD if above boundary + margin
            if (movementAngle > latBwdBoundary + margin)
                return DirectionZone.BACKWARD;
            return DirectionZone.LATERAL;

        case DirectionZone.BACKWARD:
            // Stay BACKWARD unless angle drops below boundary - margin
            if (movementAngle < latBwdBoundary - margin)
            {
                // Check if we dropped all the way to forward
                if (movementAngle < fwdLatBoundary - margin)
                    return DirectionZone.FORWARD;
                return DirectionZone.LATERAL;
            }
            return DirectionZone.BACKWARD;

        default:
            return DirectionZone.FORWARD;
    }
}
```

---

## 3.3.4 Facing Direction System

### Independence of Facing and Movement

An agent's facing direction is a unit vector in the XY plane that represents where the agent is "looking." It is updated independently from movement direction by two mechanisms:

1. **Automatic alignment:** When no explicit facing target exists, the facing direction gradually rotates toward the movement direction. This models the natural tendency to face where you're going. Rotation rate is governed by Section 3.4 (Turning).

2. **Explicit facing target:** Higher-level systems (AI, tactical instructions) can command the agent to face a specific point â€” the ball, an opponent, a passing lane. When an explicit target is set, facing direction rotates toward the target instead of the movement direction. The agent moves in whatever direction the path planner commands while looking where they're told.

```csharp
/// <summary>
/// Agent facing direction state.
/// Stored as part of AgentMovementState (Section 3.1).
///
/// FacingDirection is always a unit vector in the XY plane (Z = 0).
/// It is NEVER derived from velocity â€” it is an independent variable
/// updated by the facing update system each frame.
///
/// FacingTarget is optional. When set, facing rotates toward the target
/// position. When null, facing auto-aligns with movement direction.
///
/// FacingMode determines the update behavior.
/// </summary>
public enum FacingMode
{
    /// <summary>
    /// Facing direction auto-aligns toward movement direction.
    /// Used when agent has no specific look target â€” running forward,
    /// making a run, moving to position.
    /// Rotation rate governed by turn rate (Section 3.4).
    /// </summary>
    AUTO_ALIGN,

    /// <summary>
    /// Facing direction rotates toward an explicit target position.
    /// Used when agent needs to watch the ball, mark an opponent,
    /// jockey, or receive a pass while moving to position.
    /// Rotation rate governed by turn rate (Section 3.4).
    /// </summary>
    TARGET_LOCK
}

/// <summary>
/// Updates the agent's facing direction for one frame.
///
/// In AUTO_ALIGN mode, facing rotates toward movement direction at the
/// agent's current turn rate (Section 3.4). If the agent is stationary
/// (speed < MIN_SPEED), facing direction is unchanged.
///
/// In TARGET_LOCK mode, facing rotates toward the target position at
/// the agent's current turn rate. The agent's movement direction is
/// irrelevant to facing â€” they move wherever commanded while looking
/// at the target.
///
/// The turn rate limit (Section 3.4) applies to BOTH modes. An agent
/// cannot snap their facing direction instantaneously at any speed above
/// WALKING â€” they rotate at a physically plausible rate.
///
/// ROTATION METHOD: Uses signed-angle rotation (Atan2-based) rather than
/// Vector2.Lerp().normalized. Lerp-based rotation produces non-uniform
/// angular velocity at large angles because the interpolation path cuts
/// through the interior of the unit circle. Signed-angle rotation
/// guarantees constant angular velocity regardless of the angle delta,
/// which is critical for consistent turn rate enforcement.
///
/// NOTE: This function updates facing DIRECTION only. It does not affect
/// movement direction, speed, or state. The directional multiplier is
/// computed AFTER this update, using the new facing direction and the
/// current movement direction.
/// </summary>
/// <param name="currentFacing">Current facing direction (unit vector, XY)</param>
/// <param name="agentPosition">Agent's current position (for TARGET_LOCK angle calc)</param>
/// <param name="movementDirection">Current movement direction (for AUTO_ALIGN)</param>
/// <param name="movementSpeed">Current speed (for stationary check)</param>
/// <param name="facingMode">Current facing mode</param>
/// <param name="facingTarget">Target position (only used in TARGET_LOCK mode)</param>
/// <param name="turnRateDegPerSec">Maximum turn rate from Section 3.4</param>
/// <param name="dt">Frame delta time (seconds)</param>
/// <returns>New facing direction (unit vector, XY)</returns>
public static Vector2 UpdateFacingDirection(
    Vector2 currentFacing,
    Vector3 agentPosition,
    Vector2 movementDirection,
    float movementSpeed,
    FacingMode facingMode,
    Vector3? facingTarget,
    float turnRateDegPerSec,
    float dt)
{
    // Determine desired facing direction
    Vector2 desiredFacing;

    if (facingMode == FacingMode.TARGET_LOCK && facingTarget.HasValue)
    {
        // Face toward target position
        Vector2 toTarget = new Vector2(
            facingTarget.Value.x - agentPosition.x,
            facingTarget.Value.y - agentPosition.y);

        if (toTarget.sqrMagnitude < 0.001f)
        {
            // Target is on top of agent â€” maintain current facing
            return currentFacing;
        }

        desiredFacing = toTarget.normalized;
    }
    else
    {
        // Auto-align: face toward movement direction
        if (movementSpeed < MovementConstants.MIN_SPEED)
        {
            // Stationary â€” maintain current facing
            return currentFacing;
        }

        desiredFacing = movementDirection.normalized;
    }

    // Compute SIGNED angle from current to desired facing (degrees)
    // Positive = counter-clockwise, Negative = clockwise
    float currentAngleRad = Mathf.Atan2(currentFacing.y, currentFacing.x);
    float desiredAngleRad = Mathf.Atan2(desiredFacing.y, desiredFacing.x);
    float deltaRad = desiredAngleRad - currentAngleRad;

    // Normalize to [-Ï€, Ï€] to always take the shortest rotation path
    while (deltaRad > Mathf.PI) deltaRad -= 2f * Mathf.PI;
    while (deltaRad < -Mathf.PI) deltaRad += 2f * Mathf.PI;

    float deltaDeg = deltaRad * Mathf.Rad2Deg;

    // Snap if within small threshold (prevents oscillation around target)
    if (Mathf.Abs(deltaDeg) < 0.5f)
    {
        return desiredFacing;
    }

    // Clamp rotation to maximum turn rate for this frame
    float maxRotationDeg = turnRateDegPerSec * dt;

    float rotationDeg;
    if (Mathf.Abs(deltaDeg) <= maxRotationDeg)
    {
        // Can reach desired this frame
        return desiredFacing;
    }
    else
    {
        // Partial rotation â€” rotate by maxRotation in the correct direction
        rotationDeg = Mathf.Sign(deltaDeg) * maxRotationDeg;
    }

    // Apply rotation to current angle
    float newAngleRad = currentAngleRad + rotationDeg * Mathf.Deg2Rad;
    return new Vector2(Mathf.Cos(newAngleRad), Mathf.Sin(newAngleRad));
}
```

### Gameplay Implications

The facing/movement independence creates these football-specific behaviors:

| Scenario | Facing Mode | Movement Direction | Facing Target | Directional Zone |
|---|---|---|---|---|
| Forward sprint | AUTO_ALIGN | Forward | â€” | Forward (1.0Ã—) |
| Defensive jockey | TARGET_LOCK | Lateral | Opponent | Lateral (0.65â€“0.75Ã—) |
| Backpedaling CB | TARGET_LOCK | Backward | Ball | Backward (0.45â€“0.55Ã—) |
| Checking shoulder | TARGET_LOCK (brief) | Forward | Behind/ball | Forwardâ†’Lateral (brief) |
| Pressing run | AUTO_ALIGN | Toward opponent | â€” | Forward (1.0Ã—) |
| Covering run | TARGET_LOCK | Diagonal back | Ball/space | Lateral (0.65â€“0.75Ã—) |

---

## 3.3.5 Integration with Locomotion

### Where the Multiplier is Applied

Per FR-4, the directional multiplier affects both **top speed** and **acceleration rate**. However, the penalties are applied asymmetrically: top speed receives the full multiplier, while acceleration receives a reduced penalty (`âˆšmultiplier`). This reflects the biomechanical reality that lateral/backward acceleration initiation is only moderately slower than forward, whereas sustained lateral/backward speed is substantially lower.

```csharp
/// <summary>
/// Computes the directional acceleration rate constant.
///
/// The acceleration penalty uses the SQUARE ROOT of the directional
/// multiplier rather than the full value. This prevents over-penalization
/// of lateral movement where the compounding effect (slower top speed Ã—
/// slower acceleration) would make lateral shuffles feel unresponsive.
///
/// Biomechanical rationale: A player can initiate a lateral movement
/// almost as quickly as a forward movement â€” the first step is similar.
/// The speed ceiling is lower (stride mechanics limit sustained lateral
/// speed), but the ramp-up is only moderately slower. Using sqrt()
/// captures this: a 0.70Ã— top speed penalty becomes a 0.837Ã— acceleration
/// penalty, not a 0.70Ã— double hit.
///
/// Formula: k_directional = k_base Ã— âˆš(directionalMultiplier)
///
/// Example â€” Agility 10 agent accelerating laterally:
///   k_base = 0.782 (from Acceleration 10)
///   directionalMult = 0.697 (lateral, Agility 10)
///   k_directional = 0.782 Ã— âˆš0.697 = 0.782 Ã— 0.835 = 0.653
///   Tâ‚‰â‚€ = 2.3026 / 0.653 = 3.526s (vs 2.94s forward)
///
/// Compare to full multiplier on k:
///   k_directional = 0.782 Ã— 0.697 = 0.545 â†’ Tâ‚‰â‚€ = 4.224s (too sluggish)
///
/// The sqrt approach gives a 20% slower acceleration laterally vs 44% with
/// the full multiplier â€” much closer to observed real-world behavior.
///
/// TUNING NOTE: The sqrt function is a Stage 0 default. If playtesting
/// shows lateral acceleration still feels too slow or too fast, this can
/// be replaced with a configurable power: k Ã— mult^ACCEL_DIRECTION_POWER
/// where ACCEL_DIRECTION_POWER defaults to 0.5 (sqrt). Values closer to
/// 0.0 reduce the penalty; values closer to 1.0 increase it.
///
/// NOTE: The directional multiplier is applied to k BEFORE passing it
/// to ApplyExponentialAcceleration() (Section 3.2.3). The acceleration
/// function itself doesn't know about direction â€” it just receives a
/// (potentially reduced) k value.
/// </summary>
public static float ApplyDirectionalToAccelK(float baseK, float directionalMultiplier)
{
    // Square root reduces the penalty: 0.70Ã— speed becomes 0.837Ã— acceleration
    return baseK * Mathf.Sqrt(directionalMultiplier);
}
```

### Full Integration Call Order

The complete locomotion update for one frame follows this order:

```
1. UpdateFacingDirection()            â† Section 3.3.4 (signed-angle rotation)
2. GetMovementAngle()                 â† Section 3.3.3
3. CalculateDirectionalMultiplier()   â† Section 3.3.3
4. UpdateDirectionZone()              â† Section 3.3.3 (hysteresis, for animation/audio/UI only)
5. CalculateEffectiveTopSpeed()       â† Section 3.2.4 (receives directionalMultiplier)
6. ApplyDirectionalToAccelK()         â† Section 3.3.5 (âˆšmultiplier applied to k)
7. UpdateSpeed()                      â† Section 3.2.3 (receives modified k and target speed)
8. Apply velocity to position         â† Section 3.2 (v Ã— dt)
```

Steps 1â€“4 compute the directional context. Steps 5â€“6 fold it into the locomotion parameters. Steps 7â€“8 execute the physics. The directional system produces three outputs consumed downstream: the continuous multiplier (for top speed), the modified k (for acceleration via âˆšmultiplier), and the discrete zone (for animation/audio/UI). It has no other side effects.

### State Applicability

From Section 3.1.6 state-physics activation table:

| State | Directional Multiplier Applied? | Rationale |
|---|---|---|
| IDLE | No | No movement â€” multiplier is irrelevant |
| WALKING | Yes | Agent can walk in any direction |
| JOGGING | Yes | Agent can jog in any direction |
| SPRINTING | Yes | Agent can sprint in any direction (backward sprint is rare but possible) |
| DECELERATING | Yes | Agent maintains directional penalty while braking |
| STUMBLING | No | Momentum-only â€” no voluntary directional control |
| GROUNDED | No | No movement |

When the multiplier is not applied, `CalculateEffectiveTopSpeed()` receives `directionalMultiplier = 1.0`.

---

