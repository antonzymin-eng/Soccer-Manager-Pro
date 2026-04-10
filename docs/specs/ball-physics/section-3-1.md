# Ball Physics Specification - Section 3.1: Core Formulas

**Document:** Ball Physics Specification  
**Section:** 3.1 Core Formulas  
**Created:** February 2, 2026, 10:45 PM PST  
**Version:** 2.6  
**Status:** READY FOR IMPLEMENTATION — ERR-006 resolved (AM-001-001 applied)

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
        /// <summary>Ball mass in kg (FIFA: 410-450g, using midpoint)</summary>
        public const float MASS = 0.43f;
        
        /// <summary>Ball radius in meters (FIFA: 68-70cm circumference)</summary>
        public const float RADIUS = 0.11f;
        
        /// <summary>Ball diameter in meters</summary>
        public const float DIAMETER = 0.22f;
        
        /// <summary>Cross-sectional area in mÃ‚Â² (Ãâ‚¬rÃ‚Â²)</summary>
        public const float CROSS_SECTION_AREA = 0.0380f;
        
        /// <summary>
        /// Moment of inertia in kgÃ‚Â·mÃ‚Â² (hollow sphere: 2/3 mrÃ‚Â²).
        /// Note: Real football differs by ~10-20% due to internal structure.
        /// Tune empirically if spin behavior doesn't match real footage.
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
        /// <summary>Drag coefficient for laminar flow (Re < 200,000)</summary>
        public const float COEFFICIENT_LAMINAR = 0.20f;
        
        /// <summary>Drag coefficient in turbulent flow (Re > 400,000)</summary>
        public const float COEFFICIENT_TURBULENT = 0.10f;
        
        /// <summary>Speed at which drag crisis begins (m/s)</summary>
        public const float CRISIS_SPEED_LOW = 20.0f;
        
        /// <summary>Speed at which drag crisis ends (m/s)</summary>
        public const float CRISIS_SPEED_HIGH = 25.0f;
    }
    
    // ================================================================
    // MAGNUS EFFECT PARAMETERS
    // ================================================================
    public static class Magnus
    {
        /// <summary>Base lift coefficient</summary>
        public const float LIFT_COEFFICIENT_BASE = 0.1f;
        
        /// <summary>Lift coefficient scaling factor</summary>
        public const float LIFT_COEFFICIENT_SCALE = 0.4f;
        
        /// <summary>Minimum spin parameter for valid calculation</summary>
        public const float MIN_SPIN_PARAMETER = 0.01f;
        
        /// <summary>Maximum spin parameter (clamped)</summary>
        public const float MAX_SPIN_PARAMETER = 1.0f;
    }
    
    // ================================================================
    // SPIN DYNAMICS PARAMETERS
    // ================================================================
    public static class Spin
    {
        /// <summary>Velocity-dependent spin decay coefficient (s/m)</summary>
        public const float DECAY_VELOCITY_FACTOR = 0.01f;
        
        /// <summary>Spin-rate-dependent decay coefficient (1/rad)</summary>
        public const float DECAY_SPIN_FACTOR = 0.005f;
        
        /// <summary>Aerodynamic torque coefficient</summary>
        public const float TORQUE_COEFFICIENT = 0.01f;
    }
    
    // ================================================================
    // BOUNCE PARAMETERS
    // ================================================================
    public static class Bounce
    {
        /// <summary>
        /// Ratio of spin angular velocity that converts to linear velocity on contact.
        /// Empirically derived: ~10% of contact point velocity transfers.
        /// </summary>
        public const float SPIN_TO_LINEAR_RATIO = 0.1f;
        
        /// <summary>
        /// Coefficient of restitution for grass (default).
        /// </summary>
        public const float COR_GRASS_DEFAULT = 0.65f;
        
        /// <summary>Surface friction coefficient for grass (default)</summary>
        public const float FRICTION_GRASS_DEFAULT = 0.60f;
        
        /// <summary>Spin retention factor on grass bounce</summary>
        public const float SPIN_RETENTION_GRASS = 0.80f;
    }
    
    // ================================================================
    // ROLLING FRICTION PARAMETERS
    // ================================================================
    public static class Rolling
    {
        /// <summary>Rolling resistance for dry grass</summary>
        public const float RESISTANCE_GRASS_DRY = 0.13f;
        
        /// <summary>Rolling resistance for wet grass (ball slides more)</summary>
        public const float RESISTANCE_GRASS_WET = 0.07f;
        
        /// <summary>Rolling resistance for long grass</summary>
        public const float RESISTANCE_GRASS_LONG = 0.22f;
    }
    
    // ================================================================
    // STATE MACHINE THRESHOLDS
    // ================================================================
    public static class State
    {
        /// <summary>Minimum velocity before ball considered stationary (m/s)</summary>
        public const float MIN_VELOCITY = 0.1f;
        
        /// <summary>Minimum spin before considered zero (rad/s)</summary>
        public const float MIN_SPIN = 0.1f;
        
        /// <summary>
        /// Height threshold to ENTER airborne state (m).
        /// Note: Position.z is ball CENTER. Ball on ground has z = RADIUS (0.11m).
        /// This threshold (0.17m) means center is 6cm above resting position.
        /// </summary>
        public const float AIRBORNE_ENTER_THRESHOLD = 0.17f;
        
        /// <summary>
        /// Height threshold to EXIT airborne state (m).
        /// Uses hysteresis: exit threshold lower than enter to prevent oscillation.
        /// At 0.13m, ball center is 2cm above resting position (RADIUS = 0.11m).
        /// </summary>
        public const float AIRBORNE_EXIT_THRESHOLD = 0.13f;
        
        /// <summary>Vertical velocity after bounce to stay airborne (m/s)</summary>
        public const float BOUNCE_VELOCITY_THRESHOLD = 0.5f;
    }
    
    // ================================================================
    // SAFETY LIMITS
    // ================================================================
    public static class Limits
    {
        /// <summary>Maximum ball velocity in m/s (fastest shot ~45 m/s)</summary>
        public const float MAX_VELOCITY = 50.0f;
        
        /// <summary>Maximum angular velocity in rad/s</summary>
        public const float MAX_SPIN = 80.0f;
        
        /// <summary>Maximum height in meters (sanity check)</summary>
        public const float MAX_HEIGHT = 50.0f;
        
        /// <summary>Buffer zone beyond pitch boundaries (m)</summary>
        public const float PITCH_BUFFER = 20.0f;
    }
    
    // ================================================================
    // POSSESSION THRESHOLDS
    // ================================================================
    public static class Possession
    {
        /// <summary>Max distance for possession (m)</summary>
        public const float CONTROL_RADIUS = 0.5f;
        
        /// <summary>Max relative ball speed for control (m/s)</summary>
        public const float CONTROL_VELOCITY = 2.0f;
        
        /// <summary>Min opponent distance for uncontested control (m)</summary>
        public const float CHALLENGE_RADIUS = 1.0f;
        
        /// <summary>Max ball height for ground control (m)</summary>
        public const float CONTROL_HEIGHT = 0.5f;
    }
    
    // ================================================================
    // PITCH DIMENSIONS (FIFA STANDARD)
    // ================================================================
    public static class Pitch
    {
        /// <summary>Pitch length in meters</summary>
        public const float LENGTH = 105.0f;
        
        /// <summary>Pitch width in meters</summary>
        public const float WIDTH = 68.0f;
        
        /// <summary>Goal width in meters</summary>
        public const float GOAL_WIDTH = 7.32f;
        
        /// <summary>Goal height (crossbar) in meters</summary>
        public const float GOAL_HEIGHT = 2.44f;
        
        /// <summary>Goal post diameter in meters</summary>
        public const float POST_DIAMETER = 0.12f;
    }
    
    // ================================================================
    // GOAL POST PROPERTIES
    // ================================================================
    public static class GoalPost
    {
        /// <summary>Coefficient of restitution (aluminum/steel)</summary>
        public const float COEFFICIENT_OF_RESTITUTION = 0.75f;
        
        /// <summary>Spin retention on metal surface</summary>
        public const float SPIN_RETENTION = 0.40f;
    }
    
    // ================================================================
    // RENDERING PARAMETERS
    // ================================================================
    public static class Rendering
    {
        /// <summary>Shadow offset per meter of height</summary>
        public const float SHADOW_OFFSET_FACTOR = 0.3f;
        
        /// <summary>Ball scale increase per meter of height</summary>
        public const float HEIGHT_SCALE_FACTOR = 0.02f;
    }
    
    // ================================================================
    // EVENT LOGGING
    // ================================================================
    public static class Logging
    {
        /// <summary>Interval between position snapshots (seconds)</summary>
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

### 3.1.7.1 Combined Spin Decay

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

---

## 3.1.8 Ground Interaction

### 3.1.8.1 Impulse-Based Bounce with Proper Contact Mechanics

```csharp
/// <summary>
/// Handles ball bounce on ground contact using proper contact mechanics.
/// The ground normal is always UP for regulation football pitches.
/// (FIFA allows max 1% slope for drainage, which is negligible.)
/// </summary>
public void ApplyBounce(ref BallState ball, SurfaceType surface, BallEventLogger logger, float matchTime)
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    UnityEngine.Profiling.Profiler.BeginSample("BallPhysics.Bounce");
    #endif
    
    // Ground normal is always UP for flat pitch
    Vector3 normal = Vector3.up;
    
    // Get surface properties
    float e = SurfaceProperties.GetCoefficientOfRestitution(surface);
    float mu = SurfaceProperties.GetFrictionCoefficient(surface);
    float spinRetention = SurfaceProperties.GetSpinRetention(surface);
    
    Vector3 v = ball.Velocity;
    Vector3 omega = ball.AngularVelocity;
    float r = BallPhysicsConstants.Ball.RADIUS;
    float m = BallPhysicsConstants.Ball.MASS;
    
    // ================================================================
    // STEP 1: Decompose velocity into normal and tangent
    // ================================================================
    float vn = Vector3.Dot(v, normal);      // Normal component (negative = into ground)
    Vector3 vt = v - vn * normal;           // Tangent component (horizontal)
    
    // ================================================================
    // STEP 2: Calculate contact point velocity
    // Contact point = center velocity + contribution from spin
    // v_contact = v + Ãâ€° Ãƒâ€” r_contact, where r_contact = -r * normal
    // ================================================================
    Vector3 r_contact = -r * normal;  // Vector from center to contact point
    Vector3 spinContribution = Vector3.Cross(omega, r_contact);
    Vector3 contactVelocity = vt + spinContribution;
    
    // ================================================================
    // STEP 3: Apply normal impulse (restitution)
    // ================================================================
    float vn_after = -e * vn;
    
    // ================================================================
    // STEP 4: Apply tangential friction impulse
    // Friction acts to reduce slip at contact point
    // ================================================================
    float J_n = (1 + e) * m * Mathf.Abs(vn);  // Normal impulse magnitude
    float J_t_max = mu * J_n;                  // Max friction impulse
    
    float contactSpeed = contactVelocity.magnitude;
    Vector3 vt_after = vt;
    
    if (contactSpeed > 0.01f)
    {
        // Impulse required to stop contact point sliding
        float J_t_required = m * contactSpeed;
        
        // Apply lesser of required and maximum
        float J_t = Mathf.Min(J_t_max, J_t_required);
        
        // Friction impulse direction opposes contact velocity
        Vector3 frictionDir = -contactVelocity.normalized;
        Vector3 frictionImpulse = frictionDir * J_t;
        
        // Update tangent velocity
        vt_after = vt + frictionImpulse / m;
        
        // Update angular velocity due to friction torque
        // Ãâ€ž = r Ãƒâ€” F, so ÃŽâ€Ãâ€° = IÃ¢ÂÂ»Ã‚Â¹ Ãƒâ€” (r Ãƒâ€” J)
        Vector3 angularImpulse = Vector3.Cross(r_contact, frictionImpulse);
        omega += angularImpulse / BallPhysicsConstants.Ball.MOMENT_OF_INERTIA;
    }
    
    // ================================================================
    // STEP 5: Combine and apply spin retention
    // ================================================================
    ball.Velocity = vt_after + vn_after * normal;
    ball.AngularVelocity = omega * spinRetention;
    
    // Ensure ball is at ground level (center at RADIUS height, not z=0)
    ball.Position = new Vector3(
        ball.Position.x, 
        ball.Position.y, 
        BallPhysicsConstants.Ball.RADIUS);
    
    // ================================================================
    // STEP 6: Log event for replay
    // ================================================================
    logger?.LogBounce(ball, surface, e, vn, vn_after, matchTime);
    
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    UnityEngine.Profiling.Profiler.EndSample();
    #endif
}
```

### 3.1.8.2 Rolling Friction

```csharp
/// <summary>
/// Calculates rolling friction force.
/// Only applied when ball state is ROLLING.
/// </summary>
public Vector3 CalculateRollingFriction(Vector3 velocity, SurfaceType surface)
{
    float speed = velocity.magnitude;
    if (speed < BallPhysicsConstants.State.MIN_VELOCITY)
    {
        return Vector3.zero;
    }
    
    float mu_r = SurfaceProperties.GetRollingResistance(surface);
    float forceMagnitude = mu_r * BallPhysicsConstants.Ball.MASS * 
                           BallPhysicsConstants.Environment.GRAVITY;
    
    return -velocity.normalized * forceMagnitude;
}
```

---

## 3.1.9 Numerical Integration

### Main Physics Loop (Restructured)

```csharp
/// <summary>
/// Main physics update loop. Called at 60Hz.
/// All states go through validation and state update.
/// </summary>
public void UpdateBallPhysics(
    ref BallState ball, 
    float dt, 
    SurfaceType surface,
    Vector3 windVelocity,
    BallEventLogger logger,
    float matchTime)
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    UnityEngine.Profiling.Profiler.BeginSample("BallPhysics.Update");
    #endif
    
    // Store last valid state for error recovery
    ball.LastValidPosition = ball.Position;
    ball.LastValidVelocity = ball.Velocity;
    
    // ================================================================
    // STEP 1: Handle BOUNCING state (applies impulse, then continues)
    // ================================================================
    if (ball.State == BallStateType.BOUNCING)
    {
        ApplyBounce(ref ball, surface, logger, matchTime);
        // Don't return - continue to validation and state update
    }
    
    // ================================================================
    // STEP 2: Calculate forces based on state
    // ================================================================
    Vector3 netForce = Vector3.zero;
    Vector3 relativeVelocity = ball.Velocity - windVelocity;
    
    switch (ball.State)
    {
        case BallStateType.AIRBORNE:
            netForce = GetGravityForce() + 
                       CalculateDragForce(relativeVelocity) + 
                       CalculateMagnusForce(relativeVelocity, ball.AngularVelocity);
            break;
            
        case BallStateType.ROLLING:
            netForce = CalculateDragForce(relativeVelocity) + 
                       CalculateRollingFriction(ball.Velocity, surface);
            break;
            
        case BallStateType.BOUNCING:
            // Forces already applied in bounce, but apply drag
            netForce = CalculateDragForce(relativeVelocity);
            break;
            
        default:
            // STATIONARY, CONTROLLED, OUT_OF_PLAY: No physics
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Profiling.Profiler.EndSample();
            #endif
            return;
    }
    
    // ================================================================
    // STEP 3: Semi-Implicit Euler integration
    // ================================================================
    Vector3 acceleration = netForce / BallPhysicsConstants.Ball.MASS;
    ball.Velocity += acceleration * dt;
    ball.Position += ball.Velocity * dt;
    
    // ================================================================
    // STEP 4: Update spin (airborne only)
    // ================================================================
    if (ball.State == BallStateType.AIRBORNE)
    {
        ball.AngularVelocity = UpdateSpinDecay(ball.AngularVelocity, ball.Velocity, dt);
    }
    
    // ================================================================
    // STEP 5: ALWAYS validate (all states)
    // ================================================================
    ValidatePhysicsState(ref ball);
    
    // ================================================================
    // STEP 6: ALWAYS update state machine
    // ================================================================
    ball.State = UpdateBallState(ball);
    
    // ================================================================
    // STEP 7: ALWAYS try to log
    // ================================================================
    logger?.TryLogSnapshot(ball, matchTime);
    
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    UnityEngine.Profiling.Profiler.EndSample();
    #endif
}
```

### Validation with NaN/Infinity Handling

```csharp
/// <summary>
/// Validates physics state and applies safety clamps.
/// Includes NaN/Infinity detection and recovery.
/// </summary>
public void ValidatePhysicsState(ref BallState ball)
{
    // ================================================================
    // NAN/INFINITY CHECK (Critical - must be first)
    // ================================================================
    if (HasInvalidValues(ball))
    {
        Debug.LogError("[BallPhysics] NaN/Infinity detected! Recovering to last valid state.");
        ball.Position = ball.LastValidPosition;
        ball.Velocity = ball.LastValidVelocity;
        ball.AngularVelocity = Vector3.zero;
        ball.State = BallStateType.STATIONARY;
        return;
    }
    
    // ================================================================
    // VELOCITY CLAMPING
    // ================================================================
    float speed = ball.Velocity.magnitude;
    if (speed > BallPhysicsConstants.Limits.MAX_VELOCITY)
    {
        ball.Velocity = ball.Velocity.normalized * BallPhysicsConstants.Limits.MAX_VELOCITY;
        Debug.LogWarning($"[BallPhysics] Velocity clamped from {speed:F1} m/s");
    }
    
    // ================================================================
    // SPIN CLAMPING
    // ================================================================
    float spinRate = ball.AngularVelocity.magnitude;
    if (spinRate > BallPhysicsConstants.Limits.MAX_SPIN)
    {
        ball.AngularVelocity = ball.AngularVelocity.normalized * BallPhysicsConstants.Limits.MAX_SPIN;
        Debug.LogWarning($"[BallPhysics] Spin clamped from {spinRate:F1} rad/s");
    }
    
    // ================================================================
    // HEIGHT CLAMPING
    // ================================================================
    if (ball.Position.z > BallPhysicsConstants.Limits.MAX_HEIGHT)
    {
        ball.Position = new Vector3(ball.Position.x, ball.Position.y, 
                                    BallPhysicsConstants.Limits.MAX_HEIGHT);
        ball.Velocity = new Vector3(ball.Velocity.x, ball.Velocity.y, 
                                    Mathf.Min(ball.Velocity.z, 0));
        Debug.LogWarning("[BallPhysics] Height clamped - possible instability");
    }
    
    // ================================================================
    // GROUND PENETRATION FIX
    // ================================================================
    float groundLevel = BallPhysicsConstants.Ball.RADIUS;
    if (ball.Position.z < groundLevel && ball.State != BallStateType.OUT_OF_PLAY)
    {
        ball.Position = new Vector3(ball.Position.x, ball.Position.y, groundLevel);
        if (ball.Velocity.z < 0)
        {
            ball.Velocity = new Vector3(ball.Velocity.x, ball.Velocity.y, 0);
        }
    }
    
    // ================================================================
    // POSITION BOUNDS
    // ================================================================
    float buffer = BallPhysicsConstants.Limits.PITCH_BUFFER;
    ball.Position = new Vector3(
        Mathf.Clamp(ball.Position.x, -buffer, BallPhysicsConstants.Pitch.LENGTH + buffer),
        Mathf.Clamp(ball.Position.y, -buffer, BallPhysicsConstants.Pitch.WIDTH + buffer),
        ball.Position.z
    );
}

/// <summary>
/// Checks for NaN or Infinity in ball state.
/// </summary>
private bool HasInvalidValues(BallState ball)
{
    return float.IsNaN(ball.Position.x) || float.IsInfinity(ball.Position.x) ||
           float.IsNaN(ball.Position.y) || float.IsInfinity(ball.Position.y) ||
           float.IsNaN(ball.Position.z) || float.IsInfinity(ball.Position.z) ||
           float.IsNaN(ball.Velocity.x) || float.IsInfinity(ball.Velocity.x) ||
           float.IsNaN(ball.Velocity.y) || float.IsInfinity(ball.Velocity.y) ||
           float.IsNaN(ball.Velocity.z) || float.IsInfinity(ball.Velocity.z) ||
           float.IsNaN(ball.AngularVelocity.x) || float.IsInfinity(ball.AngularVelocity.x) ||
           float.IsNaN(ball.AngularVelocity.y) || float.IsInfinity(ball.AngularVelocity.y) ||
           float.IsNaN(ball.AngularVelocity.z) || float.IsInfinity(ball.AngularVelocity.z);
}
```

---

## 3.1.10 Collision Systems

### 3.1.10.1 Agent Collision

```csharp
public enum BodyPart
{
    Foot, Shin, Thigh, Torso, Head, Arm
}

/// <summary>
/// Body part coefficients for deflections.
/// </summary>
public static class BodyPartCoefficients
{
    // Speed retention, Spin retention
    private static readonly Dictionary<BodyPart, (float, float)> _coefficients = new()
    {
        { BodyPart.Foot,  (0.75f, 0.30f) },
        { BodyPart.Shin,  (0.65f, 0.20f) },
        { BodyPart.Thigh, (0.60f, 0.40f) },
        { BodyPart.Torso, (0.55f, 0.50f) },
        { BodyPart.Head,  (0.70f, 0.10f) },
        { BodyPart.Arm,   (0.50f, 0.30f) }
    };
    
    public static (float speedRetention, float spinRetention) Get(BodyPart part)
    {
        return _coefficients.TryGetValue(part, out var coef) ? coef : (0.60f, 0.30f);
    }
}
```

### 3.1.10.2 Goal Post Collision

```csharp
/// <summary>
/// Handles ball collision with goal post or crossbar.
/// </summary>
public void ApplyGoalPostCollision(
    ref BallState ball, 
    Vector3 contactPoint, 
    Vector3 postCenter,
    BallEventLogger logger,
    float matchTime)
{
    Vector3 normal = (contactPoint - postCenter).normalized;
    
    float vn = Vector3.Dot(ball.Velocity, normal);
    Vector3 vt = ball.Velocity - vn * normal;
    
    float vn_after = -BallPhysicsConstants.GoalPost.COEFFICIENT_OF_RESTITUTION * vn;
    
    ball.Velocity = vt + vn_after * normal;
    ball.AngularVelocity *= BallPhysicsConstants.GoalPost.SPIN_RETENTION;
    
    // Log event for replay
    logger?.LogGoalPostHit(ball, contactPoint, matchTime);
}
```

### 3.1.10.3 Boundary Detection

```csharp
public enum RestartType
{
    NONE,
    THROW_IN,
    GOAL_KICK,
    CORNER,
    KICKOFF
}

/// <summary>
/// Checks if ball has left the field of play.
/// Ball must ENTIRELY cross the line.
/// </summary>
public (bool isOut, RestartType restart) CheckBoundaries(BallState ball, int lastTouchTeamID)
{
    float x = ball.Position.x;
    float y = ball.Position.y;
    float z = ball.Position.z;
    float r = BallPhysicsConstants.Ball.RADIUS;
    
    bool lowEnough = z < BallPhysicsConstants.Ball.DIAMETER;
    
    // Touchlines
    if (lowEnough && (y < -r || y > BallPhysicsConstants.Pitch.WIDTH + r))
    {
        return (true, RestartType.THROW_IN);
    }
    
    // Goal lines
    if (lowEnough && x < -r)
    {
        if (IsInGoal(ball.Position, isHomeGoal: true))
        {
            return (true, RestartType.KICKOFF);
        }
        return (true, lastTouchTeamID == 0 ? RestartType.CORNER : RestartType.GOAL_KICK);
    }
    
    if (lowEnough && x > BallPhysicsConstants.Pitch.LENGTH + r)
    {
        if (IsInGoal(ball.Position, isHomeGoal: false))
        {
            return (true, RestartType.KICKOFF);
        }
        return (true, lastTouchTeamID == 1 ? RestartType.CORNER : RestartType.GOAL_KICK);
    }
    
    return (false, RestartType.NONE);
}

private bool IsInGoal(Vector3 position, bool isHomeGoal)
{
    float halfGoalWidth = BallPhysicsConstants.Pitch.GOAL_WIDTH / 2f;
    float centerY = BallPhysicsConstants.Pitch.WIDTH / 2f;
    
    bool withinPosts = position.y > centerY - halfGoalWidth && 
                       position.y < centerY + halfGoalWidth;
    bool underCrossbar = position.z < BallPhysicsConstants.Pitch.GOAL_HEIGHT;
    
    return withinPosts && underCrossbar;
}
```

---

## 3.1.11 Control and Possession

> **Design principle (Option B — ERR-006 / ERR-008 resolution):**
> Possession tracking is **external to `BallState`**. `BallState` carries only physics data.
> The agent system (Spec #2) owns which agent (if any) has possession. When `ApplyKick()`
> is called, `BallState.State` transitions out of `CONTROLLED` — the agent system observes
> this transition and clears its own possession record. There is no `PossessingAgentId`
> field in `BallState`. This prevents synchronisation hazards and keeps BallState
> agent-unaware (separation of concerns).

---

### 3.1.11.1 CheckPossession

```csharp
/// <summary>
/// Checks if an agent can take possession of the ball.
///
/// Returns true if ALL of the following are met:
///   1. Agent is within CONTROL_RADIUS (0.5m) of the ball (XY-plane only, height ignored)
///   2. Relative velocity between ball and agent is below CONTROL_VELOCITY (2.0 m/s)
///   3. Ball height is below CONTROL_HEIGHT (0.5m)
///
/// Caller responsibility: If this returns true, the agent system should:
///   (a) Record the agent as possessing the ball in its own possession tracker
///   (b) Call SetBallControlled(ref ball) to transition ball state to CONTROLLED
///   (c) Update ball position each frame to match agent foot position
///
/// Note: This method does NOT modify BallState. It is a pure predicate.
/// Agent system is authoritative for possession; this is a physics-layer helper.
/// </summary>
/// <param name="ball">Current ball state</param>
/// <param name="agentPosition">Agent's world position (3D)</param>
/// <param name="agentVelocity">Agent's current velocity (3D)</param>
/// <returns>True if the agent physically can take possession this frame</returns>
public bool CheckPossession(BallState ball, Vector3 agentPosition, Vector3 agentVelocity)
{
    // XY-plane distance only — height handled separately by CONTROL_HEIGHT check
    float distance = Vector3.Distance(
        new Vector3(ball.Position.x, ball.Position.y, 0),
        new Vector3(agentPosition.x, agentPosition.y, 0));
    
    if (distance > BallPhysicsConstants.Possession.CONTROL_RADIUS)
        return false;
    
    // Relative velocity: agent must be moving at similar speed to ball to "catch" it
    Vector3 relativeVelocity = ball.Velocity - agentVelocity;
    if (relativeVelocity.magnitude > BallPhysicsConstants.Possession.CONTROL_VELOCITY)
        return false;
    
    // Ball must be low enough for foot control
    if (ball.Position.z > BallPhysicsConstants.Possession.CONTROL_HEIGHT)
        return false;
    
    return true;
}

/// <summary>
/// Transitions ball state to CONTROLLED.
/// Called by agent system immediately after CheckPossession returns true.
/// Zeroes ball velocity — ball position is thereafter driven by agent foot position.
///
/// This method does NOT record which agent has possession. That is the
/// agent system's responsibility (Option B design — see section preamble).
/// </summary>
/// <param name="ball">Ball state to modify</param>
public void SetBallControlled(ref BallState ball)
{
    ball.State = BallStateType.CONTROLLED;
    ball.Velocity = Vector3.zero;
    ball.AngularVelocity = Vector3.zero;
}
```

---

### 3.1.11.2 ApplyKick

```csharp
/// <summary>
/// Applies a kick to the ball, transitioning it from CONTROLLED (or any ground state)
/// into active flight or rolling.
///
/// POSSESSION MODEL (Option B — ERR-006 / ERR-008 resolution):
/// This method does NOT clear a PossessingAgentId field — no such field exists in BallState.
/// Instead, it transitions ball.State OUT of CONTROLLED. The agent system observes this
/// state transition and clears its own possession record. This is the correct separation:
///
///   Ball Physics:  owns physics state (position, velocity, spin, BallStateType)
///   Agent System:  owns possession state (which agent has the ball)
///
/// The state transition (CONTROLLED → AIRBORNE or ROLLING) is the signal the agent
/// system polls or subscribes to. No additional coupling is required.
///
/// PRECONDITIONS:
///   - ball.State should be CONTROLLED. The method will apply the kick regardless
///     (defensive design), but callers should only invoke from a CONTROLLED state.
///   - velocity must be a finite, non-NaN vector. Validated internally.
///   - spin may be Vector3.zero (no applied spin).
///
/// POST-CONDITIONS:
///   - ball.Velocity = velocity parameter (kick impulse applied instantly)
///   - ball.AngularVelocity = spin parameter
///   - ball.State = AIRBORNE if velocity.z > 0, else ROLLING if horizontal, else STATIONARY
///   - ball.LastValidPosition and ball.LastValidVelocity updated
///   - Kick event logged to logger (if logger is non-null)
///   - Agent system MUST observe the state transition and update possession on its side
///
/// PARAMETER NOTES:
///   velocity:  The full 3D kick velocity in m/s. Direction encodes both
///              horizontal trajectory and launch angle. Magnitude is kick power.
///              Validated: NaN/Infinity → kick rejected, ball left in CONTROLLED.
///   spin:      Angular velocity to apply (rad/s). For a right-footed inside-curl,
///              this is typically (0, 0, negative_z). Pass Vector3.zero for no spin.
///   agentId:   Kicking agent's ID (0–21). Used for event logging only. Not stored
///              in BallState (no possession field — see Option B design).
///   matchTime: Current match time in seconds. Used for event logging timestamp.
///   logger:    BallEventLogger for recording KICK event. May be null (no logging).
///              In production, should never be null. Null accepted for unit tests.
/// </summary>
/// <param name="ball">Ball state to modify (ref — modified in place)</param>
/// <param name="velocity">Kick velocity vector (m/s)</param>
/// <param name="spin">Applied angular velocity (rad/s)</param>
/// <param name="agentId">Kicking agent ID (for logging only)</param>
/// <param name="matchTime">Current match time in seconds (for logging)</param>
/// <param name="logger">Event logger (may be null)</param>
public void ApplyKick(
    ref BallState ball,
    Vector3 velocity,
    Vector3 spin,
    int agentId,
    float matchTime,
    BallEventLogger logger = null)
{
    // ── VALIDATION ──────────────────────────────────────────────────────────
    // Reject NaN/Infinity. Leave ball in current state rather than corrupting physics.
    if (!IsFiniteVector(velocity))
    {
        Debug.LogError($"[BallPhysics] ApplyKick: Invalid velocity {velocity} from agent {agentId}. Kick rejected.");
        return;
    }
    if (!IsFiniteVector(spin))
    {
        // Spin corruption is less catastrophic — zero it and continue
        Debug.LogWarning($"[BallPhysics] ApplyKick: Invalid spin {spin} from agent {agentId}. Spin zeroed.");
        spin = Vector3.zero;
    }

    // ── CLAMP VELOCITY TO PHYSICS LIMITS ────────────────────────────────────
    // Clamp magnitude, not individual components, to preserve direction
    if (velocity.magnitude > BallPhysicsConstants.Limits.MAX_VELOCITY)
    {
        velocity = velocity.normalized * BallPhysicsConstants.Limits.MAX_VELOCITY;
        Debug.LogWarning($"[BallPhysics] ApplyKick: Velocity clamped to MAX_VELOCITY ({BallPhysicsConstants.Limits.MAX_VELOCITY} m/s).");
    }
    if (spin.magnitude > BallPhysicsConstants.Limits.MAX_SPIN)
    {
        spin = spin.normalized * BallPhysicsConstants.Limits.MAX_SPIN;
    }

    // ── APPLY IMPULSE ────────────────────────────────────────────────────────
    // Kick is an instantaneous velocity change (impulse model).
    // We do not accumulate — the kick replaces current velocity entirely.
    // This is physically correct for foot-ball contact (contact time ~10ms,
    // effectively instantaneous at 60Hz simulation rate).
    ball.Velocity = velocity;
    ball.AngularVelocity = spin;

    // ── POSSESSION RELEASE (Option B) ───────────────────────────────────────
    // Transition ball state OUT of CONTROLLED. This is the signal the agent
    // system observes to release possession on its side. No PossessingAgentId
    // field exists in BallState — the agent system owns that data.
    //
    // State selection:
    //   velocity.z > 0          → ball is kicked upward → AIRBORNE
    //   velocity.z <= 0 AND
    //   horizontal speed > MIN  → ball stays on ground  → ROLLING
    //   otherwise               → kick was essentially zero → STATIONARY
    float horizontalSpeed = new Vector2(velocity.x, velocity.y).magnitude;

    if (velocity.z > 0f)
    {
        ball.State = BallStateType.AIRBORNE;
    }
    else if (horizontalSpeed > BallPhysicsConstants.State.MIN_VELOCITY)
    {
        ball.State = BallStateType.ROLLING;
    }
    else
    {
        // Edge case: kick with essentially zero velocity (e.g., short tap, testing)
        ball.State = BallStateType.STATIONARY;
    }

    // ── UPDATE VALIDATION SNAPSHOTS ─────────────────────────────────────────
    // Post-kick state is now the new "last known good" baseline for recovery
    ball.LastValidPosition = ball.Position;
    ball.LastValidVelocity = ball.Velocity;

    // ── EVENT LOGGING ────────────────────────────────────────────────────────
    // Log after state transition so the logged state reflects post-kick reality
    logger?.LogKick(ball, agentId, $"ApplyKick|v={velocity.magnitude:F1}m/s|s={spin.magnitude:F1}rad/s|→{ball.State}", matchTime);
}

/// <summary>
/// Helper: returns true if all vector components are finite (not NaN or Infinity).
/// Used for kick validation. Inline for performance (no method call overhead).
/// </summary>
private bool IsFiniteVector(Vector3 v)
{
    return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
}
```

#### §3.1.11.2 Design Notes

**Why is possession external to BallState?** (Option B rationale)

`BallState` is a physics data struct. Adding `PossessingAgentId` would introduce the only agent reference in the entire Ball Physics spec — breaking the principle that Ball Physics is agent-unaware. It would also create a synchronisation hazard: two systems (Ball Physics and Agent Movement) would each hold a copy of "who has the ball", and they could drift out of sync during the same frame. Option B eliminates this class of bug by making the agent system the single source of truth for possession, with the ball's `BallStateType` as a physics-visible signal only.

**Why does velocity replace rather than add?** Foot-ball contact during a kick lasts ~8–12ms (Shinkai et al., 2009). At 60Hz, this is sub-frame. The impulse model (replace velocity) is physically correct and numerically stable; an additive model would require knowing the pre-contact velocity to cancel it first, adding complexity with no accuracy benefit at simulation frame rates.

**Unit test hooks:**
- Call `ApplyKick` with `logger = null` in unit tests to avoid logger dependencies
- To test state transitions: verify `ball.State` after kick with various `velocity.z` values
- To test possession signal: confirm state changed from `CONTROLLED` before calling kick

---

## 3.1.12 Limits and Boundaries

All limits defined in `BallPhysicsConstants.Limits` and `BallPhysicsConstants.State`.

| Limit | Value | Justification |
|-------|-------|---------------|
| MAX_VELOCITY | 50 m/s | Fastest shot ~45 m/s + buffer |
| MAX_SPIN | 80 rad/s | Physical maximum for kicked ball |
| MAX_HEIGHT | 50 m | Sanity check (highest punt ~30m) |
| MIN_VELOCITY | 0.1 m/s | Below this = stationary |
| MIN_SPIN | 0.1 rad/s | Below this = no spin |
| AIRBORNE_ENTER | 0.17 m | Ball center 6cm above resting (RADIUS + 0.06m) |
| AIRBORNE_EXIT | 0.13 m | Ball center 2cm above resting (RADIUS + 0.02m) |
| BALL_RADIUS | 0.11 m | Ground level for ball center |

---

## 3.1.13 Event Logging

### Complete Logging Implementation

```csharp
public enum BallEventType
{
    POSITION_SNAPSHOT,
    KICK,
    HEADER,
    BOUNCE,
    DEFLECTION,
    GOAL_POST_HIT,
    OUT_OF_PLAY,
    GOAL,
    POSSESSION_CHANGE
}

public struct BallEvent
{
    public float Timestamp;
    public BallEventType Type;
    public Vector3 Position;
    public Vector3 Velocity;
    public int AgentID;
    public string Detail;
}

public class BallEventLogger
{
    private readonly List<BallEvent> _events = new();
    private float _lastSnapshotTime = -999f;
    
    /// <summary>
    /// Logs a position snapshot at regular intervals.
    /// </summary>
    public void TryLogSnapshot(BallState ball, float matchTime)
    {
        if (matchTime - _lastSnapshotTime >= BallPhysicsConstants.Logging.SNAPSHOT_INTERVAL)
        {
            _events.Add(new BallEvent
            {
                Timestamp = matchTime,
                Type = BallEventType.POSITION_SNAPSHOT,
                Position = ball.Position,
                Velocity = ball.Velocity,
                AgentID = -1,
                Detail = ""
            });
            _lastSnapshotTime = matchTime;
        }
    }
    
    /// <summary>
    /// Logs a bounce event.
    /// </summary>
    public void LogBounce(BallState ball, SurfaceType surface, float cor, 
                          float vnBefore, float vnAfter, float matchTime)
    {
        _events.Add(new BallEvent
        {
            Timestamp = matchTime,
            Type = BallEventType.BOUNCE,
            Position = ball.Position,
            Velocity = ball.Velocity,
            AgentID = -1,
            Detail = $"Surface:{surface},CoR:{cor:F2},Vn:{vnBefore:F1}Ã¢â€ â€™{vnAfter:F1}"
        });
    }
    
    /// <summary>
    /// Logs a goal post hit.
    /// </summary>
    public void LogGoalPostHit(BallState ball, Vector3 contactPoint, float matchTime)
    {
        _events.Add(new BallEvent
        {
            Timestamp = matchTime,
            Type = BallEventType.GOAL_POST_HIT,
            Position = ball.Position,
            Velocity = ball.Velocity,
            AgentID = -1,
            Detail = $"Contact:({contactPoint.x:F1},{contactPoint.y:F1},{contactPoint.z:F1})"
        });
    }
    
    /// <summary>
    /// Logs a kick event.
    /// </summary>
    public void LogKick(BallState ball, int agentID, string kickType, float matchTime)
    {
        _events.Add(new BallEvent
        {
            Timestamp = matchTime,
            Type = BallEventType.KICK,
            Position = ball.Position,
            Velocity = ball.Velocity,
            AgentID = agentID,
            Detail = kickType
        });
    }
    
    /// <summary>
    /// Logs a goal event.
    /// </summary>
    public void LogGoal(BallState ball, int scorerID, int teamID, float matchTime)
    {
        _events.Add(new BallEvent
        {
            Timestamp = matchTime,
            Type = BallEventType.GOAL,
            Position = ball.Position,
            Velocity = ball.Velocity,
            AgentID = scorerID,
            Detail = $"Team:{teamID}"
        });
    }
    
    /// <summary>
    /// Exports all events for replay storage.
    /// </summary>
    public List<BallEvent> ExportEvents() => new(_events);
    
    /// <summary>
    /// Clears all logged events (call at match start).
    /// </summary>
    public void Clear()
    {
        _events.Clear();
        _lastSnapshotTime = -999f;
    }
}
```

---

## 3.1.14 Validation Test Cases

### Derived Test Values

#### Magnus Curve Test

```
Test: 25m free kick should curve 1.5-3m

Derivation:
  Given: v = 22 m/s, Ãâ€° = 12 rad/s, distance = 25m
  
  Flight time: t = 25 / 22 = 1.14 seconds
  
  Spin parameter: S = (0.11 Ãƒâ€” 12) / 22 = 0.06
  Lift coefficient: C_L = 0.1 + 0.4 Ãƒâ€” 0.06 = 0.124
  
  Magnus force: F = 0.5 Ãƒâ€” 1.225 Ãƒâ€” 22Ã‚Â² Ãƒâ€” 0.038 Ãƒâ€” 0.124 = 1.40 N
  Lateral acceleration: a = 1.40 / 0.43 = 3.26 m/sÃ‚Â²
  
  Lateral displacement: d = 0.5 Ãƒâ€” a Ãƒâ€” tÃ‚Â² = 0.5 Ãƒâ€” 3.26 Ãƒâ€” 1.30 = 2.12 m
  
  Expected range: 1.5 - 3.0 m (accounting for spin decay)
```

#### Bounce Height Test

```
Test: Drop from 2m should bounce to 0.8-0.9m on dry grass

Derivation:
  Given: h_drop = 2m, e = 0.65 (dry grass CoR)
  
  Impact velocity: v = Ã¢Ë†Å¡(2gh) = Ã¢Ë†Å¡(2 Ãƒâ€” 9.81 Ãƒâ€” 2) = 6.26 m/s
  
  Rebound velocity: v_after = e Ãƒâ€” v = 0.65 Ãƒâ€” 6.26 = 4.07 m/s
  
  Rebound height: h_rebound = vÃ‚Â² / (2g) = 4.07Ã‚Â² / 19.62 = 0.84 m
  
  Expected range: 0.80 - 0.90 m (accounting for air resistance)
```

#### Rolling Distance Test

```
Test: Ball at 10 m/s should stop in 26-31m on dry grass

Derivation:
  Given: vâ‚€ = 10 m/s, Î¼_r = 0.13 (dry grass)
  
  Two forces act on rolling ball:
  
  1. Rolling friction:
     a_friction = Î¼_r Ã— g = 0.13 Ã— 9.81 = 1.28 m/sÂ²
  
  2. Air drag (at 10 m/s):
     F_drag = 0.5 Ã— 1.225 Ã— 100 Ã— 0.2 Ã— 0.038 = 0.47 N
     a_drag = 0.47 / 0.43 = 1.09 m/sÂ²
  
  Combined initial deceleration:
     a_total = 1.28 + 1.09 = 2.37 m/sÂ²
  
  Note: Drag decreases as ball slows (âˆ vÂ²), friction remains constant.
  Friction dominates below ~10 m/s; drag dominates above.
  
  Friction-only stopping distance (upper bound):
     d = vâ‚€Â² / (2 Ã— Î¼_r Ã— g) = 100 / (2 Ã— 1.28) = 39.2m
  
  60Hz numerical simulation (drag + friction): 28.3m in 6.3s
  
  Expected range: 26 - 31 m
```

### Test Implementation

```csharp
[Test]
public void MagnusForce_ZeroSpin_ReturnsZero()
{
    var force = CalculateMagnusForce(
        velocity: new Vector3(20, 0, 0),
        angularVelocity: Vector3.zero);
    
    Assert.AreEqual(Vector3.zero, force);
}

[Test]
public void Bounce_DryGrass_ReturnsExpectedHeight()
{
    // Drop from 2m (ball center at 2m + RADIUS, but we measure from center)
    // Impact velocity from 2m drop: v = sqrt(2gh) = sqrt(2 * 9.81 * 2) = 6.26 m/s
    float groundLevel = BallPhysicsConstants.Ball.RADIUS;
    
    var ball = new BallState
    {
        Position = new Vector3(50, 34, groundLevel),  // Ball resting on ground
        Velocity = new Vector3(0, 0, -6.26f),         // Impact velocity from 2m drop
        AngularVelocity = Vector3.zero,
        State = BallStateType.BOUNCING,
        LastValidPosition = new Vector3(50, 34, groundLevel),
        LastValidVelocity = new Vector3(0, 0, -6.26f)
    };
    
    ApplyBounce(ref ball, SurfaceType.GRASS_DRY, null, 0);
    
    // Calculate rebound height from velocity (relative to ground level)
    // v_after = e * v = 0.65 * 6.26 = 4.07 m/s
    // h = vÃ‚Â² / (2g) = 4.07Ã‚Â² / 19.62 = 0.84m above ground level
    float reboundHeight = ball.Velocity.z * ball.Velocity.z / 
                          (2 * BallPhysicsConstants.Environment.GRAVITY);
    
    Assert.Greater(reboundHeight, 0.80f, "Bounce should reach at least 0.80m");
    Assert.Less(reboundHeight, 0.90f, "Bounce should not exceed 0.90m");
}

[Test]
public void FreekickTrajectory_CurvesWithinExpectedRange()
{
    var ball = new BallState
    {
        State = BallStateType.AIRBORNE,
        Position = new Vector3(25, 34, 0),
        Velocity = new Vector3(22, 0, 6),
        AngularVelocity = new Vector3(0, 0, -12)
    };
    
    float dt = 1f / 60f;
    float startY = ball.Position.y;
    
    // Simulate until ball travels 25m in X
    while (ball.Position.x < 50 && ball.Position.z >= 0)
    {
        UpdateBallPhysics(ref ball, dt, SurfaceType.GRASS_DRY, Vector3.zero, null, 0);
    }
    
    float lateralDeviation = Mathf.Abs(ball.Position.y - startY);
    
    Assert.Greater(lateralDeviation, 1.5f, "Ball should curve at least 1.5m");
    Assert.Less(lateralDeviation, 3.0f, "Ball should not curve more than 3.0m");
}

[Test]
public void StateTransition_Hysteresis_PreventsOscillation()
{
    // Hysteresis band: EXIT=0.13m to ENTER=0.17m (ball center z values)
    // Ball resting on ground has z = RADIUS = 0.11m
    
    var ball = new BallState
    {
        State = BallStateType.ROLLING,
        Position = new Vector3(50, 34, 0.15f), // Between thresholds (0.13 < 0.15 < 0.17)
        Velocity = new Vector3(5, 0, 0.1f)
    };
    
    // From ROLLING at 0.15m (below ENTER threshold of 0.17m)
    var newState = UpdateBallState(ball);
    Assert.AreEqual(BallStateType.ROLLING, newState, "Should stay ROLLING below enter threshold");
    
    ball.Position = new Vector3(50, 34, 0.18f); // Above ENTER threshold (0.17m)
    newState = UpdateBallState(ball);
    Assert.AreEqual(BallStateType.AIRBORNE, newState, "Should become AIRBORNE above enter threshold");
    
    ball.State = BallStateType.AIRBORNE;
    ball.Position = new Vector3(50, 34, 0.15f); // Between thresholds — hysteresis keeps AIRBORNE
    ball.Velocity = new Vector3(5, 0, -0.1f);
    newState = UpdateBallState(ball);
    Assert.AreEqual(BallStateType.AIRBORNE, newState, "Should stay AIRBORNE above exit threshold (hysteresis)");
}

[Test]
public void Validation_DetectsNaN_AndRecovers()
{
    var ball = new BallState
    {
        Position = new Vector3(float.NaN, 34, 0),
        Velocity = new Vector3(10, 0, 0),
        LastValidPosition = new Vector3(50, 34, 0),
        LastValidVelocity = new Vector3(5, 0, 0)
    };
    
    ValidatePhysicsState(ref ball);
    
    Assert.AreEqual(50f, ball.Position.x, "Should recover to last valid X");
    Assert.AreEqual(BallStateType.STATIONARY, ball.State, "Should reset to STATIONARY");
}
```

---

## Document Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | Jan 20, 2026 | AI | Initial specification |
| 2.0 | Feb 2, 2026 | AI | Standard floats, 2.5D, state machine, collisions |
| 2.1 | Feb 2, 2026 | AI | Fixed spin conventions, contact mechanics, hysteresis, wind, validation, tests |
| 2.2 | Feb 3, 2026 | AI | Clarified spin axes, fixed bounce height, added factory methods, using statements |
| 2.3 | Feb 3, 2026 | AI | Fixed ground level consistency (RADIUS), updated thresholds, ready for implementation |
| 2.4 | Feb 8, 2026 | AI | Updated rolling resistance coefficients (µ_r) for all surfaces; REV-001 |
| 2.5 | Feb 21, 2026 | AI | Added §3.1.11.1/11.2 sub-labels; added ApplyKick() — ERR-006/ERR-008 resolution |
| 2.6 | Mar 2, 2026 | AI | Fixed §3.1.14 hysteresis test positions to match v2.2+ threshold constants |

---

## References

1. Asai, T., et al. (2007). "Aerodynamics of a new soccer ball" - Sports Engineering
2. CarrÃƒÂ©, M.J., et al. (2002). "The curve kick of a football" - Sports Engineering
3. Goff, J.E. (2013). "A review of recent research into aerodynamics of sport projectiles"
4. FIFA Laws of the Game (2024) - Ball specifications (Law 2)
5. Master Volume I: Physics & Simulation Core
6. Master Volume IV: Technical Implementation & Systems Engineering
7. Football Manager Replay System Research (Internal, Feb 2, 2026)
