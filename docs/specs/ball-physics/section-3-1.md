# Ball Physics Specification - Section 3.1: Core Formulas

**Document:** Ball Physics Specification  
**Section:** 3.1 Core Formulas  
**Created:** February 2, 2026, 10:45 PM PST  
**Version:** 2.7  
**Status:** READY FOR IMPLEMENTATION — ERR-006 resolved (AM-001-001 applied)

**Changes from v2.6:**
- Gap 1: Added `[GT]`, `[EST]`, `[FIXED]`, `[DERIVED]` source tags to all constants in §3.1.2
- H-04-A: Added `Spin.ROLLING_SPIN_DECAY_PER_SECOND [EST]` constant to §3.1.2
- Mi-01-A/B/C: Removed duplicate `Bounce.COR_GRASS_DEFAULT`, `Bounce.FRICTION_GRASS_DEFAULT`,
  `Bounce.SPIN_RETENTION_GRASS` constants (identical to `SurfaceProperties.GetCoefficientOfRestitution(GRASS_DRY)`,
  `GetFrictionCoefficient(GRASS_DRY)`, and `GetSpinRetention(GRASS_DRY)` respectively)
- H-04-B: Added §3.1.7.2 `UpdateRollingSpinDecay()` — separate from airborne `UpdateSpinDecay()` because
  aerodynamic torque model is physically incorrect for ground-contact spin damping

**Changes from v2.5:**
- Fixed §3.1.14 hysteresis test case: position values updated from pre-v2.2 era (z=0 ground level)
  to current thresholds (z=RADIUS ground level). Test positions now within actual hysteresis band
  (0.13m–0.17m) instead of stale 0.045–0.065m range. Test logic unchanged; comments corrected.

**Changes from v2.4:**
- Added §3.1.11.1 sub-label to `CheckPossession()` for consistent cross-referencing
- Added §3.1.11.2 `ApplyKick()` — resolves ERR-006 (method was referenced in §8.3.5 but undefined)
- `ApplyKick()` implements Option B possession model: possession tracking external to BallState
- Updated TOC to include §3.1.11.1 and §3.1.11.2 anchors
- Document Revision History updated

**Changes from v2.3:**
- Updated rolling resistance coefficients (Î¼_r) for all surfaces to match verified 60Hz simulation data
- GRASS_DRY Î¼_r: 0.05 â†’ 0.13 (produces 28.3m stopping distance at 10 m/s, matching broadcast observations)
- All surface Î¼_r values rescaled proportionally (see REV-001)
- Rolling distance test case corrected: expected range now 26-31m (was 18-25m)

**Changes from v2.2:**
- Fixed ground penetration check to use RADIUS (not z=0)
- Updated AIRBORNE thresholds to be relative to RADIUS (enter=0.17m, exit=0.13m)
- Clarified that Position.z is ball CENTER in documentation
- Fixed test case to initialize ball at correct ground level

**Changes from v2.1:**
- Clarified spin conventions with concrete axis examples for topspin/backspin
- Fixed bounce to place ball at RADIUS height (not z=0)
- Added logger and matchTime parameters to ApplyGoalPostCollision
- Cleaned up rolling distance derivation
- Added factory methods for BallState initialization
- Added required using statements

**Changes from v2.0:**
- Fixed spin axis conventions (now relative to velocity direction)
- Proper contact mechanics for spin-to-velocity transfer
- State machine restructured with hysteresis
- Wind velocity parameter added
- Complete event logging functions
- NaN/Infinity error handling
- Derived test case values with calculations
- Fixed worked example
- Reorganized constants with nested classes
- Added profiling hooks
- Replaced magic numbers with named constants

---

## Table of Contents

- [3.1.1 Coordinate System](#311-coordinate-system)
- [3.1.2 Physical Constants](#312-physical-constants)
- [3.1.3 Ball State Machine](#313-ball-state-machine)
- [3.1.4 Magnus Effect](#314-magnus-effect)
- [3.1.5 Aerodynamic Drag](#315-aerodynamic-drag)
- [3.1.6 Gravity](#316-gravity)
- [3.1.7 Spin Dynamics](#317-spin-dynamics)
  - [3.1.7.1 Combined Spin Decay (Airborne)](#3171-combined-spin-decay-airborne)
  - [3.1.7.2 Rolling Spin Decay](#3172-rolling-spin-decay)
- [3.1.8 Ground Interaction](#318-ground-interaction)
- [3.1.9 Numerical Integration](#319-numerical-integration)
- [3.1.10 Collision Systems](#3110-collision-systems)
- [3.1.11 Control and Possession](#3111-control-and-possession)
  - [3.1.11.1 CheckPossession](#31111-checkpossession)
  - [3.1.11.2 ApplyKick](#31112-applykick)
- [3.1.12 Limits and Boundaries](#3112-limits-and-boundaries)
- [3.1.13 Event Logging](#3113-event-logging)
- [3.1.14 Validation Test Cases](#3114-validation-test-cases)

---

## 3.1.1 Coordinate System

### 2.5D Architecture

This specification uses **2.5D physics**: full 3D physics calculations with 2D top-down rendering.

```
       Z (height)
       Ã¢â€ â€˜
       Ã¢â€â€š    
       Ã¢â€â€š   Ã¢Å¡Â½ Ball position: (x, y, z)
       Ã¢â€â€š  Ã¢â€¢Â±
       Ã¢â€â€š Ã¢â€¢Â±
       Ã¢â€â€šÃ¢â€¢Â±_________ Y (pitch width)
      Ã¢â€¢Â±
     Ã¢â€¢Â±
    X (pitch length)
```

### Axis Definitions

| Axis | Direction | Range | Notes |
|------|-----------|-------|-------|
| **X** | Along pitch length | 0 to 105m | Goal-to-goal direction |
| **Y** | Along pitch width | 0 to 68m | Touchline-to-touchline |
| **Z** | Vertical (height) | 0 to Ã¢Ë†Å¾ | 0 = ground level |

### Coordinate Origin

- **Origin (0, 0, 0):** Corner flag at home team's left defensive corner
- **Home goal:** X = 0, centered at Y = 34m
- **Away goal:** X = 105m, centered at Y = 34m
- **Center spot:** (52.5, 34, 0)

### Unity Integration

```csharp
// Required using statements for all code in this specification:
using UnityEngine;
using UnityEngine.Profiling;
using System;
using System.Collections.Generic;

/// <summary>
/// Ball physics state. Stored as struct for cache efficiency and 
/// to match data-oriented design philosophy (Master Vol 4 Ã‚Â§1.1).
/// External functions operate on this data.
/// 
/// WARNING: Struct fields default to zero. Use CreateAtPosition() factory
/// to ensure valid initialization, especially for LastValidPosition.
/// </summary>
public struct BallState
{
    public Vector3 Position;        // (x, y, z) in meters
    public Vector3 Velocity;        // (vx, vy, vz) in m/s
    public Vector3 AngularVelocity; // (Ãâ€°x, Ãâ€°y, Ãâ€°z) in rad/s
    public BallStateType State;     // Current state machine state
    
    // For validation - tracks last known good state
    public Vector3 LastValidPosition;
    public Vector3 LastValidVelocity;
    
    /// <summary>
    /// Factory method to create a ball at a specific position.
    /// Ensures LastValidPosition is properly initialized.
    /// </summary>
    public static BallState CreateAtPosition(Vector3 position)
    {
        return new BallState
        {
            Position = position,
            Velocity = Vector3.zero,
            AngularVelocity = Vector3.zero,
            State = BallStateType.STATIONARY,
            LastValidPosition = position,
            LastValidVelocity = Vector3.zero
        };
    }
    
    /// <summary>
    /// Factory method for kickoff at center spot.
    /// </summary>
    public static BallState CreateForKickoff()
    {
        Vector3 centerSpot = new Vector3(
            BallPhysicsConstants.Pitch.LENGTH / 2f,
            BallPhysicsConstants.Pitch.WIDTH / 2f,
            BallPhysicsConstants.Ball.RADIUS);
        return CreateAtPosition(centerSpot);
    }
}

/// <summary>
/// Converts simulation position to 2D render position.
/// </summary>
public Vector2 ToRenderPosition(Vector3 simPos)
{
    // X maps to screen X, Y maps to screen Y
    // Z (height) represented via shadow offset and ball scale
    return new Vector2(simPos.x, simPos.y);
}

/// <summary>
/// Calculates shadow offset for height visualization.
/// Shadow separates from ball as height increases.
/// </summary>
public float GetShadowOffset(float height)
{
    return height * BallPhysicsConstants.Rendering.SHADOW_OFFSET_FACTOR;
}

/// <summary>
/// Calculates ball scale for height visualization.
/// Ball appears slightly larger when higher (perspective hint).
/// </summary>
public float GetBallScale(float height)
{
    return 1.0f + height * BallPhysicsConstants.Rendering.HEIGHT_SCALE_FACTOR;
}
```

---

## 3.1.2 Physical Constants

### Organized Constants

```csharp
/// <summary>
/// Physical constants for ball physics simulation.
/// Organized into logical groups for clarity.
/// All values in SI units (meters, kilograms, seconds).
/// </summary>
public static class BallPhysicsConstants
{
    // ================================================================
    // BALL PROPERTIES (FIFA Size 5)
    // ================================================================
    public static class Ball
    {
        /// <summary>Ball mass in kg (FIFA: 410-450g, using midpoint) [FIXED]</summary>
        public const float MASS = 0.43f;
        
        /// <summary>Ball radius in meters (FIFA: 68-70cm circumference) [FIXED]</summary>
        public const float RADIUS = 0.11f;
        
        /// <summary>Ball diameter in meters. Derived: 2 × RADIUS [DERIVED]</summary>
        public const float DIAMETER = 0.22f;
        
        /// <summary>Cross-sectional area in mÃ‚Â² (Ãâ‚¬rÃ‚Â²)</summary>
        public const float CROSS_SECTION_AREA = 0.0380f;
        
        /// <summary>
        /// Moment of inertia in kgÃ‚Â·mÃ‚Â² (hollow sphere: 2/3 mrÃ‚Â²).
        /// Note: Real football differs by ~10-20% due to internal structure.
        /// Tune empirically if spin behavior doesn't match real footage. [EST]
        /// </summary>
        public const float MOMENT_OF_INERTIA = 0.00347f;
    }
    
    // ================================================================
    // ENVIRONMENTAL PROPERTIES
    // ================================================================
    public static class Environment
    {
        /// <summary>Air density at sea level in kg/mÃ‚Â³</summary>
        public const float AIR_DENSITY = 1.225f;
        
        /// <summary>Gravitational acceleration in m/sÃ‚Â²</summary>
        public const float GRAVITY = 9.81f;
        
        /// <summary>Dynamic viscosity of air in PaÃ‚Â·s</summary>
        public const float AIR_VISCOSITY = 1.81e-5f;
    }
    
    // ================================================================
    // DRAG PARAMETERS
    // ================================================================
    public static class Drag
    {
        /// <summary>Drag coefficient for laminar flow (Re < 200,000) [GT]</summary>
        public const float COEFFICIENT_LAMINAR = 0.20f;
        
        /// <summary>Drag coefficient in turbulent flow (Re > 400,000) [GT]</summary>
        public const float COEFFICIENT_TURBULENT = 0.10f;
        
        /// <summary>Speed at which drag crisis begins (m/s) [EST]</summary>
        public const float CRISIS_SPEED_LOW = 20.0f;
        
        /// <summary>Speed at which drag crisis ends (m/s) [EST]</summary>
        public const float CRISIS_SPEED_HIGH = 25.0f;
    }
    
    // ================================================================
    // MAGNUS EFFECT PARAMETERS
    // ================================================================
    public static class Magnus
    {
        /// <summary>Base lift coefficient [GT]</summary>
        public const float LIFT_COEFFICIENT_BASE = 0.1f;
        
        /// <summary>Lift coefficient scaling factor [GT]</summary>
        public const float LIFT_COEFFICIENT_SCALE = 0.4f;
        
        /// <summary>Minimum spin parameter for valid calculation [GT]</summary>
        public const float MIN_SPIN_PARAMETER = 0.01f;
        
        /// <summary>Maximum spin parameter (clamped) [GT]</summary>
        public const float MAX_SPIN_PARAMETER = 1.0f;
    }
    
    // ================================================================
    // SPIN DYNAMICS PARAMETERS
    // ================================================================
    public static class Spin
    {
        /// <summary>Velocity-dependent spin decay coefficient (s/m) [GT]</summary>
        public const float DECAY_VELOCITY_FACTOR = 0.01f;
        
        /// <summary>Spin-rate-dependent decay coefficient (1/rad) [GT]</summary>
        public const float DECAY_SPIN_FACTOR = 0.005f;
        
        /// <summary>Aerodynamic torque coefficient [GT]</summary>
        public const float TORQUE_COEFFICIENT = 0.01f;
        
        /// <summary>
        /// Rate at which spin decays during rolling (rad/s per second). [EST]
        /// Ground-contact friction dominates over aerodynamic torque for rolling balls;
        /// this constant is independent of and must NOT be confused with the airborne
        /// aerodynamic torque model in UpdateSpinDecay().
        /// Tune empirically to match observed spin persistence on grass.
        /// </summary>
        public const float ROLLING_SPIN_DECAY_PER_SECOND = 5.0f;
    }
    
    // ================================================================
    // BOUNCE PARAMETERS
    // ================================================================
    public static class Bounce
    {
        /// <summary>
        /// Ratio of spin angular velocity that converts to linear velocity on contact.
        /// Empirically derived: ~10% of contact point velocity transfers. [EST]
        /// </summary>
        public const float SPIN_TO_LINEAR_RATIO = 0.1f;
    }
    
    // ================================================================
    // ROLLING FRICTION PARAMETERS
    // ================================================================
    public static class Rolling
    {
        /// <summary>Rolling resistance for dry grass [GT]</summary>
        public const float RESISTANCE_GRASS_DRY = 0.13f;
        
        /// <summary>Rolling resistance for wet grass (ball slides more) [GT]</summary>
        public const float RESISTANCE_GRASS_WET = 0.07f;
        
        /// <summary>Rolling resistance for long grass [GT]</summary>
        public const float RESISTANCE_GRASS_LONG = 0.22f;
    }
    
    // ================================================================
    // STATE MACHINE THRESHOLDS
    // ================================================================
    public static class State
    {
        /// <summary>Minimum velocity before ball considered stationary (m/s) [GT]</summary>
        public const float MIN_VELOCITY = 0.1f;
        
        /// <summary>Minimum spin before considered zero (rad/s) [GT]</summary>
        public const float MIN_SPIN = 0.1f;
        
        /// <summary>
        /// Height threshold to ENTER airborne state (m). [GT]
        /// Note: Position.z is ball CENTER. Ball on ground has z = RADIUS (0.11m).
        /// This threshold (0.17m) means center is 6cm above resting position.
        /// </summary>
        public const float AIRBORNE_ENTER_THRESHOLD = 0.17f;
        
        /// <summary>
        /// Height threshold to EXIT airborne state (m). [GT]
        /// Uses hysteresis: exit threshold lower than enter to prevent oscillation.
        /// At 0.13m, ball center is 2cm above resting position (RADIUS = 0.11m).
        /// </summary>
        public const float AIRBORNE_EXIT_THRESHOLD = 0.13f;
        
        /// <summary>Vertical velocity after bounce to stay airborne (m/s) [GT]</summary>
        public const float BOUNCE_VELOCITY_THRESHOLD = 0.5f;
    }
    
    // ================================================================
    // SAFETY LIMITS
    // ================================================================
    public static class Limits
    {
        /// <summary>Maximum ball velocity in m/s (fastest shot ~45 m/s) [EST]</summary>
        public const float MAX_VELOCITY = 50.0f;
        
        /// <summary>Maximum angular velocity in rad/s [EST]</summary>
        public const float MAX_SPIN = 80.0f;
        
        /// <summary>Maximum height in meters (sanity check) [EST]</summary>
        public const float MAX_HEIGHT = 50.0f;
        
        /// <summary>Buffer zone beyond pitch boundaries (m) [GT]</summary>
        public const float PITCH_BUFFER = 20.0f;
    }
    
    // ================================================================
    // POSSESSION THRESHOLDS
    // ================================================================
    public static class Possession
    {
        /// <summary>Max distance for possession (m) [GT]</summary>
        public const float CONTROL_RADIUS = 0.5f;
        
        /// <summary>Max relative ball speed for control (m/s) [GT]</summary>
        public const float CONTROL_VELOCITY = 2.0f;
        
        /// <summary>Min opponent distance for uncontested control (m) [GT]</summary>
        public const float CHALLENGE_RADIUS = 1.0f;
        
        /// <summary>Max ball height for ground control (m) [GT]</summary>
        public const float CONTROL_HEIGHT = 0.5f;
    }
    
    // ================================================================
    // PITCH DIMENSIONS (FIFA STANDARD)
    // ================================================================
    public static class Pitch
    {
        /// <summary>Pitch length in meters [FIXED]</summary>
        public const float LENGTH = 105.0f;
        
        /// <summary>Pitch width in meters [FIXED]</summary>
        public const float WIDTH = 68.0f;
        
        /// <summary>Goal width in meters [FIXED]</summary>
        public const float GOAL_WIDTH = 7.32f;
        
        /// <summary>Goal height (crossbar) in meters [FIXED]</summary>
        public const float GOAL_HEIGHT = 2.44f;
        
        /// <summary>Goal post diameter in meters [FIXED]</summary>
        public const float POST_DIAMETER = 0.12f;
    }
    
    // ================================================================
    // GOAL POST PROPERTIES
    // ================================================================
    public static class GoalPost
    {
        /// <summary>Coefficient of restitution (aluminum/steel) [GT]</summary>
        public const float COEFFICIENT_OF_RESTITUTION = 0.75f;
        
        /// <summary>Spin retention on metal surface [GT]</summary>
        public const float SPIN_RETENTION = 0.40f;
    }
    
    // ================================================================
    // RENDERING PARAMETERS
    // ================================================================
    public static class Rendering
    {
        /// <summary>Shadow offset per meter of height [GT]</summary>
        public const float SHADOW_OFFSET_FACTOR = 0.3f;
        
        /// <summary>Ball scale increase per meter of height [GT]</summary>
        public const float HEIGHT_SCALE_FACTOR = 0.02f;
    }
    
    // ================================================================
    // EVENT LOGGING
    // ================================================================
    public static class Logging
    {
        /// <summary>Interval between position snapshots (seconds) [GT]</summary>
        public const float SNAPSHOT_INTERVAL = 1.0f;
    }
}
```

### Surface Properties Table

| Surface Type | CoR (e) | Friction (ÃŽÂ¼) | Rolling (ÃŽÂ¼_r) | Spin Retention |
|--------------|---------|--------------|---------------|----------------|
| GRASS_DRY | 0.65 | 0.60 | 0.13 | 0.80 |
| GRASS_WET | 0.70 | 0.40 | 0.07 | 0.85 |
| GRASS_LONG | 0.55 | 0.70 | 0.22 | 0.70 |
| ARTIFICIAL | 0.72 | 0.55 | 0.09 | 0.75 |
| FROZEN | 0.80 | 0.20 | 0.04 | 0.90 |

```csharp
public enum SurfaceType
{
    GRASS_DRY,
    GRASS_WET,
    GRASS_LONG,
    ARTIFICIAL,
    FROZEN
}

/// <summary>
/// Returns surface properties for a given surface type.
/// 
/// FUTURE OPPORTUNITY (Stage 3+): Per-position surface queries.
/// Could model: worn goalmouths, wet patches, shadowed areas.
/// ROI assessment: High effort, low gameplay impact.
/// Recommendation: Defer unless playtesting reveals it matters.
/// </summary>
public static class SurfaceProperties
{
    public static float GetCoefficientOfRestitution(SurfaceType surface)
    {
        return surface switch
        {
            SurfaceType.GRASS_DRY => 0.65f,
            SurfaceType.GRASS_WET => 0.70f,
            SurfaceType.GRASS_LONG => 0.55f,
            SurfaceType.ARTIFICIAL => 0.72f,
            SurfaceType.FROZEN => 0.80f,
            _ => 0.65f
        };
    }
    
    public static float GetFrictionCoefficient(SurfaceType surface)
    {
        return surface switch
        {
            SurfaceType.GRASS_DRY => 0.60f,
            SurfaceType.GRASS_WET => 0.40f,
            SurfaceType.GRASS_LONG => 0.70f,
            SurfaceType.ARTIFICIAL => 0.55f,
            SurfaceType.FROZEN => 0.20f,
            _ => 0.60f
        };
    }
    
    public static float GetRollingResistance(SurfaceType surface)
    {
        return surface switch
        {
            SurfaceType.GRASS_DRY => 0.13f,
            SurfaceType.GRASS_WET => 0.07f,
            SurfaceType.GRASS_LONG => 0.22f,
            SurfaceType.ARTIFICIAL => 0.09f,
            SurfaceType.FROZEN => 0.04f,
            _ => 0.13f
        };
    }
    
    public static float GetSpinRetention(SurfaceType surface)
    {
        return surface switch
        {
            SurfaceType.GRASS_DRY => 0.80f,
            SurfaceType.GRASS_WET => 0.85f,
            SurfaceType.GRASS_LONG => 0.70f,
            SurfaceType.ARTIFICIAL => 0.75f,
            SurfaceType.FROZEN => 0.90f,
            _ => 0.80f
        };
    }
}
```

---

## 3.1.3 Ball State Machine

### State Definitions

```csharp
/// <summary>
/// Ball state machine states.
/// Determines which physics forces are applied.
/// </summary>
public enum BallStateType
{
    /// <summary>Ball at rest on ground, no movement</summary>
    STATIONARY,
    
    /// <summary>Ball moving along ground with continuous contact</summary>
    ROLLING,
    
    /// <summary>Ball in flight, no ground contact</summary>
    AIRBORNE,
    
    /// <summary>Brief moment of ground contact during bounce</summary>
    BOUNCING,
    
    /// <summary>Ball possessed by an agent</summary>
    CONTROLLED,
    
    /// <summary>Ball has left the field of play</summary>
    OUT_OF_PLAY
}
```

### State Transition with Hysteresis

```csharp
/// <summary>
/// Updates ball state based on current physics conditions.
/// Uses hysteresis to prevent rapid oscillation at boundaries.
/// </summary>
public BallStateType UpdateBallState(BallState ball)
{
    switch (ball.State)
    {
        case BallStateType.STATIONARY:
            // Transitions handled externally by kick/touch events
            return BallStateType.STATIONARY;
            
        case BallStateType.ROLLING:
            // Check if ball has stopped
            if (ball.Velocity.magnitude < BallPhysicsConstants.State.MIN_VELOCITY)
            {
                return BallStateType.STATIONARY;
            }
            // Check if ball went airborne (use ENTER threshold)
            if (ball.Position.z > BallPhysicsConstants.State.AIRBORNE_ENTER_THRESHOLD)
            {
                return BallStateType.AIRBORNE;
            }
            // Check boundaries
            if (IsOutOfBounds(ball.Position))
            {
                return BallStateType.OUT_OF_PLAY;
            }
            return BallStateType.ROLLING;
            
        case BallStateType.AIRBORNE:
            // Check for ground contact (use EXIT threshold for hysteresis)
            if (ball.Position.z <= BallPhysicsConstants.State.AIRBORNE_EXIT_THRESHOLD && 
                ball.Velocity.z < 0)
            {
                return BallStateType.BOUNCING;
            }
            // Check boundaries
            if (IsOutOfBounds(ball.Position))
            {
                return BallStateType.OUT_OF_PLAY;
            }
            return BallStateType.AIRBORNE;
            
        case BallStateType.BOUNCING:
            // After bounce applied, check vertical velocity
            if (Mathf.Abs(ball.Velocity.z) < BallPhysicsConstants.State.BOUNCE_VELOCITY_THRESHOLD)
            {
                return BallStateType.ROLLING;
            }
            return BallStateType.AIRBORNE;
            
        case BallStateType.CONTROLLED:
            // Transitions handled externally by agent system
            return BallStateType.CONTROLLED;
            
        case BallStateType.OUT_OF_PLAY:
            // Transitions handled externally by restart system
            return BallStateType.OUT_OF_PLAY;
            
        default:
            return BallStateType.STATIONARY;
    }
}

/// <summary>
/// Checks if position is outside pitch boundaries.
/// </summary>
private bool IsOutOfBounds(Vector3 position)
{
    float r = BallPhysicsConstants.Ball.RADIUS;
    
    // Ball must entirely cross line
    return position.x < -r || 
           position.x > BallPhysicsConstants.Pitch.LENGTH + r ||
           position.y < -r || 
           position.y > BallPhysicsConstants.Pitch.WIDTH + r;
}
```

### Physics Application by State

| State | Gravity | Magnus | Drag | Rolling Friction | Bounce Check |
|-------|:-------:|:------:|:----:|:----------------:|:------------:|
| STATIONARY | Ã¢Å“â€” | Ã¢Å“â€” | Ã¢Å“â€” | Ã¢Å“â€” | Ã¢Å“â€” |
| ROLLING | Ã¢Å“â€” | Ã¢Å“â€” | Ã¢Å“â€œ | Ã¢Å“â€œ | Ã¢Å“â€” |
| AIRBORNE | Ã¢Å“â€œ | Ã¢Å“â€œ | Ã¢Å“â€œ | Ã¢Å“â€” | Ã¢Å“â€œ |
| BOUNCING | Ã¢Å“â€œ | Ã¢Å“â€œ | Ã¢Å“â€œ | Ã¢Å“â€” | Apply |
| CONTROLLED | Ã¢Å“â€” | Ã¢Å“â€” | Ã¢Å“â€” | Ã¢Å“â€” | Ã¢Å“â€” |
| OUT_OF_PLAY | Ã¢Å“â€” | Ã¢Å“â€” | Ã¢Å“â€” | Ã¢Å“â€” | Ã¢Å“â€” |

---

## 3.1.4 Magnus Effect

The Magnus effect causes a spinning ball to curve in flight.

### Spin Conventions (Velocity-Relative)

**Critical:** Spin is defined relative to the ball's velocity direction, not world axes.

```
COORDINATE REMINDER:
  X = pitch length (toward away goal)
  Y = pitch width (left to right when facing away goal)
  Z = height (up)

TOPSPIN (ball dips faster, "heavy" flight):
  - Ball rotates so bottom surface moves in same direction as velocity
  - Contact point velocity adds to linear velocity
  - Magnus force points DOWN (Ãâ€° Ãƒâ€” v points -Z)
  
  Example: Ball moving in +X direction (toward goal)
    Ã¢â€ â€™ Topspin = Ãâ€°_y < 0 (negative Y rotation)
    Ã¢â€ â€™ Cross product: (-Y) Ãƒâ€” (+X) = -Z (downward force) Ã¢Å“â€œ
  
  Example: Ball moving in +Y direction (across pitch)
    Ã¢â€ â€™ Topspin = Ãâ€°_x > 0 (positive X rotation)
    Ã¢â€ â€™ Cross product: (+X) Ãƒâ€” (+Y) = -Z (downward force) Ã¢Å“â€œ

BACKSPIN (ball floats longer, "light" flight):
  - Ball rotates so bottom surface moves opposite to velocity
  - Contact point velocity opposes linear velocity
  - Magnus force points UP (Ãâ€° Ãƒâ€” v points +Z)
  
  Example: Ball moving in +X direction
    Ã¢â€ â€™ Backspin = Ãâ€°_y > 0 (positive Y rotation)
    Ã¢â€ â€™ Cross product: (+Y) Ãƒâ€” (+X) = +Z (upward force) Ã¢Å“â€œ

SIDESPIN (ball curves left or right):
  - Ball rotates around vertical axis (Z)
  - Magnus force is horizontal
  
  Example: Ball moving in +X direction
    Ã¢â€ â€™ Ãâ€°_z > 0 (CCW from above): Force in -Y, ball curves LEFT
    Ã¢â€ â€™ Ãâ€°_z < 0 (CW from above): Force in +Y, ball curves RIGHT
```

### Formula

```
F_magnus = 0.5 Ãƒâ€” ÃÂ Ãƒâ€” |v|Ã‚Â² Ãƒâ€” A Ãƒâ€” C_L Ãƒâ€” (Ãâ€°ÃŒâ€š Ãƒâ€” vÃŒâ€š)
```

**Where:**
| Symbol | Description | Unit |
|--------|-------------|------|
| F_magnus | Magnus force vector | N |
| ÃÂ | Air density | kg/mÃ‚Â³ |
| v | Ball velocity relative to air | m/s |
| A | Cross-sectional area | mÃ‚Â² |
| C_L | Lift coefficient | dimensionless |
| Ãâ€°ÃŒâ€š | Spin axis unit vector | dimensionless |
| vÃŒâ€š | Velocity unit vector | dimensionless |

### Lift Coefficient Model

```
S = (r Ãƒâ€” |Ãâ€°|) / |v|

C_L = 0.1 + 0.4 Ãƒâ€” Clamp01((S - 0.01) / 0.99)
```

### Implementation

```csharp
/// <summary>
/// Calculates Magnus force on a spinning ball.
/// Only applied when ball is AIRBORNE.
/// </summary>
/// <param name="velocity">Ball velocity relative to air (ball.Velocity - wind)</param>
/// <param name="angularVelocity">Ball angular velocity in rad/s</param>
/// <returns>Magnus force vector in Newtons</returns>
public Vector3 CalculateMagnusForce(Vector3 velocity, Vector3 angularVelocity)
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    UnityEngine.Profiling.Profiler.BeginSample("BallPhysics.Magnus");
    #endif
    
    float speed = velocity.magnitude;
    float spinRate = angularVelocity.magnitude;
    
    // Guard: No Magnus effect if ball not moving or not spinning
    if (speed < BallPhysicsConstants.State.MIN_VELOCITY || 
        spinRate < BallPhysicsConstants.State.MIN_SPIN)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        UnityEngine.Profiling.Profiler.EndSample();
        #endif
        return Vector3.zero;
    }
    
    // Calculate spin parameter S = (surface velocity) / (linear velocity)
    float spinParameter = (BallPhysicsConstants.Ball.RADIUS * spinRate) / speed;
    spinParameter = Mathf.Clamp(spinParameter, 
        BallPhysicsConstants.Magnus.MIN_SPIN_PARAMETER, 
        BallPhysicsConstants.Magnus.MAX_SPIN_PARAMETER);
    
    // Calculate lift coefficient
    float normalizedS = (spinParameter - BallPhysicsConstants.Magnus.MIN_SPIN_PARAMETER) / 
                        (BallPhysicsConstants.Magnus.MAX_SPIN_PARAMETER - 
                         BallPhysicsConstants.Magnus.MIN_SPIN_PARAMETER);
    float C_L = BallPhysicsConstants.Magnus.LIFT_COEFFICIENT_BASE + 
                BallPhysicsConstants.Magnus.LIFT_COEFFICIENT_SCALE * normalizedS;
    
    // Calculate force direction: F Ã¢Ë†Â Ãâ€° Ãƒâ€” v
    Vector3 forceDirection = Vector3.Cross(angularVelocity.normalized, velocity.normalized);
    
    // Guard: Cross product could be zero if Ãâ€° parallel to v
    if (forceDirection.sqrMagnitude < 0.0001f)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        UnityEngine.Profiling.Profiler.EndSample();
        #endif
        return Vector3.zero;
    }
    forceDirection.Normalize();
    
    // Calculate force magnitude: F = 0.5 Ãƒâ€” ÃÂ Ãƒâ€” vÃ‚Â² Ãƒâ€” A Ãƒâ€” C_L
    float forceMagnitude = 0.5f * BallPhysicsConstants.Environment.AIR_DENSITY * 
                           speed * speed * 
                           BallPhysicsConstants.Ball.CROSS_SECTION_AREA * C_L;
    
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    UnityEngine.Profiling.Profiler.EndSample();
    #endif
    
    return forceDirection * forceMagnitude;
}
```

### Worked Example (Corrected)

**Scenario:** Right-footed bending free kick, ball curving right (from kicker's view)

```
Initial conditions:
  Position: (25, 34, 0) - 25m from goal, centered
  Velocity: v = (20, 0, 6) m/s - forward toward goal, slightly upward
  Angular velocity: Ãâ€° = (0, 0, -12) rad/s - clockwise from above (sidespin)

Step 1: Calculate speed and spin rate
  speed = |v| = Ã¢Ë†Å¡(20Ã‚Â² + 0Ã‚Â² + 6Ã‚Â²) = Ã¢Ë†Å¡(400 + 36) = Ã¢Ë†Å¡436 = 20.88 m/s
  spinRate = |Ãâ€°| = 12 rad/s

Step 2: Calculate spin parameter S
  S = (r Ãƒâ€” |Ãâ€°|) / |v|
  S = (0.11 Ãƒâ€” 12) / 20.88
  S = 1.32 / 20.88
  S = 0.063

Step 3: Calculate lift coefficient C_L
  normalizedS = (0.063 - 0.01) / (1.0 - 0.01) = 0.053 / 0.99 = 0.054
  C_L = 0.1 + 0.4 Ãƒâ€” 0.054 = 0.1 + 0.022 = 0.122

Step 4: Calculate force direction (Ãâ€° Ãƒâ€” v)
  Ãâ€°ÃŒâ€š = (0, 0, -1) / 1 = (0, 0, -1)
  vÃŒâ€š = (20, 0, 6) / 20.88 = (0.958, 0, 0.287)
  
  Ãâ€°ÃŒâ€š Ãƒâ€” vÃŒâ€š = | i      j      k    |
           | 0      0     -1    |
           | 0.958  0      0.287|
  
  = i(0Ãƒâ€”0.287 - (-1)Ãƒâ€”0) - j(0Ãƒâ€”0.287 - (-1)Ãƒâ€”0.958) + k(0Ãƒâ€”0 - 0Ãƒâ€”0.958)
  = i(0) - j(0.958) + k(0)
  = (0, -0.958, 0)
  
  Normalized: (0, -1, 0) Ã¢â€ â€™ Force points in -Y direction

Step 5: Calculate force magnitude
  F = 0.5 Ãƒâ€” ÃÂ Ãƒâ€” vÃ‚Â² Ãƒâ€” A Ãƒâ€” C_L
  F = 0.5 Ãƒâ€” 1.225 Ãƒâ€” 436 Ãƒâ€” 0.038 Ãƒâ€” 0.122
  F = 0.5 Ãƒâ€” 1.225 Ãƒâ€” 436 Ãƒâ€” 0.00464
  F = 1.24 N

Step 6: Final Magnus force vector
  F_magnus = (0, -1.24, 0) N

Result: Ball experiences 1.24 N force in -Y direction.
        This curves the ball to the RIGHT (kicker's perspective, toward +Y is left).

Trajectory estimate:
  - Flight time to goal (25m at ~20 m/s): t Ã¢â€°Ë† 1.25 seconds
  - Lateral acceleration: a = F/m = 1.24/0.43 = 2.88 m/sÃ‚Â²
  - Lateral displacement: d = 0.5 Ãƒâ€” a Ãƒâ€” tÃ‚Â² = 0.5 Ãƒâ€” 2.88 Ãƒâ€” 1.56 = 2.25 m

The ball curves approximately 2.25 meters to the right over 25 meters.
```

---

## 3.1.5 Aerodynamic Drag

Air resistance slows the ball in flight.

### Formula

```
F_drag = -0.5 Ãƒâ€” ÃÂ Ãƒâ€” |v_rel|Ã‚Â² Ãƒâ€” C_d Ãƒâ€” A Ãƒâ€” vÃŒâ€š_rel
```

Where `v_rel = v - v_wind` (velocity relative to air)

### Drag Coefficient Selection

```csharp
/// <summary>
/// Returns drag coefficient based on ball speed.
/// Simplified drag crisis model using linear interpolation.
/// 
/// Note: Real behavior is hysteretic and sharp. Linear interpolation
/// is acceptable for gameplay; tune if knuckleball shots don't feel
/// right during playtesting.
/// </summary>
private float GetDragCoefficient(float speed)
{
    if (speed < BallPhysicsConstants.Drag.CRISIS_SPEED_LOW)
    {
        return BallPhysicsConstants.Drag.COEFFICIENT_LAMINAR;
    }
    else if (speed > BallPhysicsConstants.Drag.CRISIS_SPEED_HIGH)
    {
        return BallPhysicsConstants.Drag.COEFFICIENT_TURBULENT;
    }
    else
    {
        float t = (speed - BallPhysicsConstants.Drag.CRISIS_SPEED_LOW) / 
                  (BallPhysicsConstants.Drag.CRISIS_SPEED_HIGH - 
                   BallPhysicsConstants.Drag.CRISIS_SPEED_LOW);
        return Mathf.Lerp(
            BallPhysicsConstants.Drag.COEFFICIENT_LAMINAR,
            BallPhysicsConstants.Drag.COEFFICIENT_TURBULENT, t);
    }
}
```

### Implementation

```csharp
/// <summary>
/// Calculates aerodynamic drag force on the ball.
/// Applied when ball is AIRBORNE or ROLLING.
/// </summary>
/// <param name="relativeVelocity">Ball velocity relative to air (ball.Velocity - wind)</param>
public Vector3 CalculateDragForce(Vector3 relativeVelocity)
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    UnityEngine.Profiling.Profiler.BeginSample("BallPhysics.Drag");
    #endif
    
    float speed = relativeVelocity.magnitude;
    
    if (speed < BallPhysicsConstants.State.MIN_VELOCITY)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        UnityEngine.Profiling.Profiler.EndSample();
        #endif
        return Vector3.zero;
    }
    
    float C_d = GetDragCoefficient(speed);
    
    // F = -0.5 Ãƒâ€” ÃÂ Ãƒâ€” vÃ‚Â² Ãƒâ€” C_d Ãƒâ€” A Ãƒâ€” vÃŒâ€š
    float forceMagnitude = 0.5f * BallPhysicsConstants.Environment.AIR_DENSITY * 
                           speed * speed * C_d * 
                           BallPhysicsConstants.Ball.CROSS_SECTION_AREA;
    
    Vector3 force = -relativeVelocity.normalized * forceMagnitude;
    
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    UnityEngine.Profiling.Profiler.EndSample();
    #endif
    
    return force;
}
```

---

## 3.1.6 Gravity

### Formula

```
F_gravity = (0, 0, -m Ãƒâ€” g) = (0, 0, -4.22) N
```

### Implementation

```csharp
/// <summary>
/// Returns gravitational force. Only applied when AIRBORNE.
/// </summary>
public Vector3 GetGravityForce()
{
    return new Vector3(0, 0, 
        -BallPhysicsConstants.Ball.MASS * BallPhysicsConstants.Environment.GRAVITY);
}
```

---

## 3.1.7 Spin Dynamics

### 3.1.7.1 Combined Spin Decay (Airborne)

```csharp
/// <summary>
/// Updates angular velocity based on aerodynamic effects.
/// </summary>
public Vector3 UpdateSpinDecay(Vector3 angularVelocity, Vector3 velocity, float dt)
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    UnityEngine.Profiling.Profiler.BeginSample("BallPhysics.SpinDecay");
    #endif
    
    float spinRate = angularVelocity.magnitude;
    float speed = velocity.magnitude;
    
    if (spinRate < BallPhysicsConstants.State.MIN_SPIN)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        UnityEngine.Profiling.Profiler.EndSample();
        #endif
        return Vector3.zero;
    }
    
    // Aerodynamic torque: Ãâ€ž = -C_Ãâ€ž Ãƒâ€” ÃÂ Ãƒâ€” rÃ¢ÂÂµ Ãƒâ€” |Ãâ€°|Ã‚Â² Ãƒâ€” Ãâ€°ÃŒâ€š
    float r5 = Mathf.Pow(BallPhysicsConstants.Ball.RADIUS, 5);
    float torqueMagnitude = BallPhysicsConstants.Spin.TORQUE_COEFFICIENT * 
                            BallPhysicsConstants.Environment.AIR_DENSITY * r5 * 
                            spinRate * spinRate;
    Vector3 torque = -torqueMagnitude * angularVelocity.normalized;
    
    // Velocity-dependent decay
    float velocityDecay = BallPhysicsConstants.Spin.DECAY_VELOCITY_FACTOR * speed;
    
    // Spin-rate-dependent decay  
    float spinDecay = BallPhysicsConstants.Spin.DECAY_SPIN_FACTOR * spinRate;
    
    // Combined decay
    float totalDecayRate = velocityDecay + spinDecay;
    
    // Apply decay
    Vector3 newAngularVelocity = angularVelocity * (1.0f - totalDecayRate * dt);
    
    // Add torque effect
    Vector3 angularAcceleration = torque / BallPhysicsConstants.Ball.MOMENT_OF_INERTIA;
    newAngularVelocity += angularAcceleration * dt;
    
    if (newAngularVelocity.magnitude < BallPhysicsConstants.State.MIN_SPIN)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        UnityEngine.Profiling.Profiler.EndSample();
        #endif
        return Vector3.zero;
    }
    
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    UnityEngine.Profiling.Profiler.EndSample();
    #endif
    
    return newAngularVelocity;
}
```

### 3.1.7.2 Rolling Spin Decay

```csharp
/// <summary>
/// Decays angular velocity for a rolling ball using a surface-contact friction model.
///
/// IMPORTANT: Do NOT use UpdateSpinDecay() for rolling balls. The aerodynamic torque
/// model in that method (τ = -C_τ × ρ × r⁵ × |ω|²) is physically correct for
/// airborne flight only. For a rolling ball, spin decay is dominated by surface
/// contact friction, which is better approximated by a simple linear decay rate.
///
/// Post-bounce entry note: the AngularVelocity value entering this method already
/// reflects ApplyBounce()'s spinRetention multiplication. No additional cap or floor
/// is applied here beyond State.MIN_SPIN.
/// </summary>
/// <param name="angularVelocity">Current angular velocity in rad/s</param>
/// <param name="dt">Time step in seconds</param>
/// <returns>Updated angular velocity after rolling spin decay</returns>
public Vector3 UpdateRollingSpinDecay(Vector3 angularVelocity, float dt)
{
    float spinRate = angularVelocity.magnitude;

    if (spinRate < BallPhysicsConstants.State.MIN_SPIN)
    {
        return Vector3.zero;
    }

    // Linear decay: spin reduces by a fixed rate per second.
    // ROLLING_SPIN_DECAY_PER_SECOND [EST] captures surface contact friction;
    // aerodynamic torque is negligible at ground-contact speeds.
    float newSpinRate = spinRate - BallPhysicsConstants.Spin.ROLLING_SPIN_DECAY_PER_SECOND * dt;

    if (newSpinRate < BallPhysicsConstants.State.MIN_SPIN)
    {
        return Vector3.zero;
    }

    return angularVelocity.normalized * newSpinRate;
}
```

---

