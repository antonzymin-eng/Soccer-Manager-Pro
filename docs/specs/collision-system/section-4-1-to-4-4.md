# Collision System Specification â€” Section 4: Data Structures & Implementation Details

**Purpose:** Defines the comprehensive data structures, constants organization, memory layout, configuration management, and code architecture for the Collision System. This section consolidates all struct definitions referenced throughout Sections 1â€“3 and establishes the implementation blueprint for Stage 0.

**Created:** February 15, 2026, 5:30 PM PST  
**Revised:** March 05, 2026, audit session  
**Version:** 1.1  
**Status:** Draft — Revised  
**Author:** Claude (AI) with Anton (Lead Developer)  
**Prerequisites:** Section 1 (Purpose & Scope) v1.1, Section 2 (System Overview) v1.2, Section 3 (Core Systems) v1.1

**Changelog:**
- v1.1 (Mar 5, 2026): Updated prerequisite versions. Added note that Section 4 §4.7.3
  BeginFrame seed formula is authoritative (Section 2 §2.6.4 updated to match in v1.2).
  Cross-reference verification §2.4 memory note corrected to §2.5 (Section 2 renumbered
  in v1.1). (3) §4.1.1 DeterministicRNG "sole consumer" note updated to acknowledge
  Decision Tree §3.3 also implements SplitMix64; recommends shared extraction [F-14].
  (4) CollisionManifold memory comment clarified: ContactPoint is Vector2 in manifold,
  Vector3 in events/callbacks; explicit conversion note added [F-18]. (5) §4.6.2
  performance table clarified: <0.15ms typical, <0.30ms p95, <0.50ms p99 [F-19].
  No formula or constant changes.
- v1.0 (Feb 15, 2026): Initial draft.

---

## 4.1 Code Organization

### 4.1.1 File Structure

```
Core/Physics/Collision/
â”œâ”€â”€ CollisionSystem.cs              // Main entry point, frame update orchestration
â”‚   â”œâ”€â”€ UpdateCollisions()          // Main update loop (Section 3.4)
â”‚   â”œâ”€â”€ ProcessAgentAgentCollision() // Agent-agent collision pipeline
â”‚   â”œâ”€â”€ ProcessAgentBallCollision()  // Agent-ball collision pipeline
â”‚   â””â”€â”€ BeginFrame()                // Deterministic RNG initialization
â”œâ”€â”€ SpatialHashGrid.cs              // Spatial partitioning data structure (Section 3.1)
â”‚   â”œâ”€â”€ Clear()                     // Sparse clear for occupied cells
â”‚   â”œâ”€â”€ Insert()                    // Entity insertion with boundary overlap
â”‚   â””â”€â”€ Query()                     // 3Ã—3 neighborhood query
â”œâ”€â”€ CollisionDetection.cs           // Narrow phase intersection tests (Section 3.2)
â”‚   â”œâ”€â”€ CheckAgentAgentCollision()  // Circle-circle (XY plane)
â”‚   â””â”€â”€ CheckAgentBallCollision()   // Circle-sphere with height filter
â”œâ”€â”€ CollisionResponse.cs            // Impulse calculation and state triggers (Section 3.3)
â”‚   â”œâ”€â”€ ComputeAgentAgentResponse() // Momentum transfer, fall/stumble
â”‚   â”œâ”€â”€ ComputeImpulse()            // Impulse magnitude from restitution
â”‚   â”œâ”€â”€ ComputeSeparation()         // Penetration resolution via MTV
â”‚   â””â”€â”€ ClassifyContactType()       // Foul data: shoulder, behind, side
â”œâ”€â”€ CollisionConstants.cs           // All constants organized by category
â”œâ”€â”€ CollisionManifold.cs            // Collision geometry struct
â”œâ”€â”€ CollisionResponseData.cs        // Response output struct
â”œâ”€â”€ CollisionEvent.cs               // Event struct + CollisionType enum
â”œâ”€â”€ AgentBallCollisionData.cs       // Ball Physics callback data
â”œâ”€â”€ ContactForceData.cs             // Foul detection data struct
â”œâ”€â”€ DeterministicRNG.cs             // Seeded xorshift128+ implementation
â””â”€â”€ BallCollisionHandler.cs         // Stub interface to Ball Physics

Tests/Physics/Collision/
â”œâ”€â”€ SpatialHashTests.cs             // SH-001 through SH-005 (Section 5)
â”œâ”€â”€ CollisionDetectionTests.cs      // CD-001 through CD-006 (Section 5)
â”œâ”€â”€ CollisionResponseTests.cs       // CR-001 through CR-005 (Section 5)
â”œâ”€â”€ FallLogicTests.cs               // FL-001 through FL-004 (Section 5)
â”œâ”€â”€ EdgeCaseTests.cs                // EC-001 through EC-002 (Section 5)
â”œâ”€â”€ IntegrationTests.cs             // IT-001 through IT-008 (Section 5)
â””â”€â”€ PerformanceTests.cs             // PERF-001 through PERF-004 (Section 5)
```

**Design rationale:** The file structure mirrors Ball Physics Spec #1 (one file per logical subsystem). Collision System is simpler than Agent Movementâ€”fewer subsystems, less state to trackâ€”resulting in fewer files. Each file targets under 400 lines to maintain readability.

**Shared infrastructure notes:**

`DeterministicRNG.cs` implements the xorshift128+ algorithm required for deterministic fall/stumble probability. This implementation may migrate to `Core/Physics/Common/` if other specs require deterministic random numbers. For Stage 0, it remains in the Collision namespace. Note: Decision Tree Specification #8 (§3.3) independently implements SplitMix64 for its deterministic selection pipeline. A shared `Core/Physics/Common/DeterministicRNG` should be extracted before Stage 1 implementation to avoid divergent implementations.

`ContactForceData.cs` defines the foul detection interface consumed by the future Referee System (Stage 1+). The struct is defined now to establish the data contract, even though the consumer does not yet exist.

### 4.1.2 Namespace Convention

```csharp
namespace TacticalDirector.Core.Physics.Collision
{
    // All collision system code: spatial hash, detection, response, events
}

namespace TacticalDirector.Core.Physics.Collision.Tests
{
    // All test code: unit tests, integration tests, performance tests
}

namespace TacticalDirector.Core.Physics.Common
{
    // Shared types used by multiple physics specs
    // Candidates from Collision System: DeterministicRNG, PitchDimensions
    // Populated as cross-spec dependencies are identified
}
```

This follows the established precedent from Ball Physics (`TacticalDirector.Core.Physics.Ball`) and Agent Movement (`TacticalDirector.Core.Physics.Agent`).

### 4.1.3 Class Responsibilities

**CollisionSystem (Instance class)**
- Owns the spatial hash grid instance
- Owns the deterministic RNG instance
- Orchestrates per-frame update pipeline
- Manages collision event buffer
- Single instance per match simulation

**SpatialHashGrid (Instance class)**
- Grid cell storage (7,314 pre-allocated lists)
- Insert, query, and clear operations
- No physics calculations
- No external dependencies

**CollisionDetection (Static class)**
- Pure geometric intersection functions
- Circle-circle and circle-sphere tests
- Returns collision manifold
- All methods static, side-effect free

**CollisionResponse (Static class)**
- Impulse calculation from manifold + properties
- Fall/stumble probability evaluation
- Contact type classification for fouls
- Returns response struct, does not modify agents directly

**DeterministicRNG (Struct)**
- Value type for cache efficiency
- Seeded from match seed + frame number
- Implements xorshift128+ algorithm
- No static state (each frame gets fresh seed)

---

## 4.2 Core Data Structures

### 4.2.1 CollisionManifold

```csharp
/// <summary>
/// Describes the geometric relationship between two colliding entities.
/// 
/// Computed by narrow-phase collision detection (CollisionDetection.cs).
/// Consumed by collision response to compute impulses and separation.
/// 
/// Lifetime: Valid for one frame. Recomputed each frame collision is active.
/// Memory: 28 bytes (two Vector2 + float + two ints)
/// Note: ContactPoint is Vector2 (XY plane). CollisionEvent and
/// AgentBallCollisionData use Vector3 ContactPoint (Z=0 in Stage 0).
/// Conversion: new Vector3(manifold.ContactPoint.x, manifold.ContactPoint.y, 0)
/// in CollisionSystem.ProcessAgentAgentCollision() (Section 3.4.1).
/// </summary>
public struct CollisionManifold
{
    // ================================================================
    // COLLISION GEOMETRY (computed by narrow phase)
    // ================================================================
    
    /// <summary>
    /// Collision normal vector (2D, XY plane).
    /// 
    /// Points from Entity1 toward Entity2 by convention.
    /// Unit length (magnitude = 1.0).
    /// Used to compute impulse direction.
    /// 
    /// Example:
    ///   Entity1 at (10, 5), Entity2 at (11, 5)
    ///   Normal = (1, 0) â€” pointing right toward Entity2
    /// </summary>
    public Vector2 Normal;
    
    /// <summary>
    /// Point of contact in world coordinates (2D, XY plane).
    /// 
    /// Located on the line segment between entity centers,
    /// weighted by their respective radii (closer to smaller entity).
    /// 
    /// Formula:
    ///   t = radius1 / (radius1 + radius2)
    ///   ContactPoint = Position1 + (Position2 - Position1) * t
    /// </summary>
    public Vector2 ContactPoint;
    
    /// <summary>
    /// Penetration depth in meters.
    /// 
    /// How much the two hitboxes overlap.
    /// Used to compute minimum translation vector (MTV) for separation.
    /// 
    /// Formula:
    ///   PenetrationDepth = (radius1 + radius2) - distance
    /// 
    /// Range: [0, max_combined_radius] where max = 0.50 + 0.50 = 1.0m
    /// Typical values: 0.01â€“0.10m (agents brush past each other)
    /// Warning threshold: >0.50m (indicates tunneling)
    /// </summary>
    public float PenetrationDepth;
    
    // ================================================================
    // ENTITY IDENTIFICATION (set by caller)
    // ================================================================
    
    /// <summary>
    /// First entity ID (lower ID by convention).
    /// Range: 0â€“21 for agents, -1 for ball.
    /// </summary>
    public int Entity1ID;
    
    /// <summary>
    /// Second entity ID (higher ID by convention).
    /// Range: 0â€“21 for agents, -1 for ball.
    /// </summary>
    public int Entity2ID;
}
```

### 4.2.2 CollisionResponseData

```csharp
/// <summary>
/// Output from collision response calculation.
/// 
/// Describes the effects to apply to an agent after collision:
/// velocity change, position correction, and state triggers.
/// 
/// One instance per agent per collision (a collision produces two responses,
/// one for each agent involved).
/// 
/// Lifetime: Consumed immediately after collision processing.
/// Memory: 32 bytes (two Vector3 + two bool + float)
/// </summary>
public struct CollisionResponseData
{
    // ================================================================
    // VELOCITY & POSITION CHANGES
    // ================================================================
    
    /// <summary>
    /// Velocity impulse to add to agent velocity (m/s).
    /// 
    /// Computed from momentum conservation:
    ///   Î”v = (j / mass) Ã— normal
    /// 
    /// Where j = impulse magnitude, computed from relative velocity,
    /// masses, and coefficient of restitution (0.3 for agents).
    /// 
    /// Positive impulse pushes agent away from collision.
    /// Zero impulse if agents are separating (not approaching).
    /// </summary>
    public Vector3 VelocityImpulse;
    
    /// <summary>
    /// Position correction to add to agent position (meters).
    /// 
    /// Computed from minimum translation vector (MTV):
    ///   PositionCorrection = normal Ã— (penetration Ã— mass_ratio)
    /// 
    /// Where mass_ratio = (other_mass / total_mass).
    /// Heavier agents move less; lighter agents move more.
    /// 
    /// Applied after impulse to ensure agents are no longer overlapping.
    /// </summary>
    public Vector3 PositionCorrection;
    
    // ================================================================
    // STATE TRIGGERS
    // ================================================================
    
    /// <summary>
    /// If true, agent should transition to GROUNDED state.
    /// 
    /// Triggered when impact force exceeds fall threshold:
    ///   F_impact > F_threshold_fall = 500 + (Strength Ã— 50) Newtons
    /// 
    /// AND random roll succeeds (probability increases with force excess).
    /// 
    /// Only one of TriggerGrounded/TriggerStumble can be true.
    /// GROUNDED takes priority if both conditions met.
    /// </summary>
    public bool TriggerGrounded;
    
    /// <summary>
    /// If true, agent should transition to STUMBLING state.
    /// 
    /// Triggered when impact force exceeds stumble threshold but not fall:
    ///   F_threshold_stumble < F_impact â‰¤ F_threshold_fall
    /// 
    /// Where F_threshold_stumble = F_threshold_fall Ã— 0.5.
    /// 
    /// STUMBLING is a brief balance recovery (300â€“800ms based on Balance).
    /// Agent continues moving but at reduced effectiveness.
    /// </summary>
    public bool TriggerStumble;
    
    /// <summary>
    /// Duration to remain in GROUNDED state (seconds).
    /// 
    /// Only valid if TriggerGrounded is true.
    /// Range: [0.5, 2.0] seconds.
    /// 
    /// Formula:
    ///   duration = GROUNDED_DURATION_MAX - (strengthProxy Ã— duration_range)
    /// 
    /// Where strengthProxy approximates Agility (Stage 0 simplification).
    /// Stronger/more agile players recover faster.
    /// 
    /// Stage 1: Replace with actual Agility attribute.
    /// </summary>
    public float GroundedDuration;
}
```

### 4.2.3 CollisionEvent

```csharp
/// <summary>
/// Record of a collision for replay, statistics, and foul detection.
/// 
/// Published to Event System (Spec #17) after each collision.
/// Consumed by:
///   - Replay system (visualization)
///   - Statistics tracker (physical duels won/lost)
///   - Referee System (foul adjudication, Stage 1+)
///   - Match analysis (collision heat maps)
/// 
/// Memory: 56 bytes (float + enum + two ints + Vector3 + float + ContactForceData)
/// </summary>
public struct CollisionEvent
{
    /// <summary>
    /// Match time when collision occurred (seconds from kickoff).
    /// Range: [0, 5400+] for 90+ minute match with stoppage.
    /// Precision: Frame-accurate (1/60th second = 0.0167s).
    /// </summary>
    public float MatchTime;
    
    /// <summary>
    /// Type of collision (AGENT_AGENT, AGENT_BALL, etc.).
    /// See CollisionType enum for full list.
    /// </summary>
    public CollisionType Type;
    
    /// <summary>
    /// First entity ID (lower ID by convention).
    /// Range: 0â€“21 for agents, -1 for ball.
    /// </summary>
    public int Entity1ID;
    
    /// <summary>
    /// Second entity ID (higher ID, or BALL_ENTITY_ID for agent-ball).
    /// Range: 0â€“21 for agents, -1 for ball.
    /// </summary>
    public int Entity2ID;
    
    /// <summary>
    /// Contact point in world coordinates (3D).
    /// Z coordinate included for aerial collisions (Stage 1+).
    /// Stage 0: Z is always 0 (ground-level collision).
    /// </summary>
    public Vector3 ContactPoint;
    
    /// <summary>
    /// Impact force in Newtons.
    /// 
    /// Computed from impulse and timestep:
    ///   F_impact = j / dt (where dt = 1/60 s)
    /// 
    /// Used for foul severity assessment.
    /// Range: [0, ~5000N] for typical football collisions.
    /// Zero for agent-ball collisions (ball has no mass in this context).
    /// </summary>
    public float ImpactForce;
    
    /// <summary>
    /// Foul detection data for Referee System.
    /// Only populated for AGENT_AGENT collisions.
    /// Default (zeroed) for AGENT_BALL.
    /// </summary>
    public ContactForceData FoulData;
}

/// <summary>
/// Collision type enumeration.
/// 
/// Stage 0: AGENT_AGENT and AGENT_BALL only.
/// Stage 1+: Additional types for aerial duels, goalkeeper saves, etc.
/// </summary>
public enum CollisionType
{
    /// <summary>
    /// Two field players collided (most common).
    /// Generates foul data, may trigger fall/stumble.
    /// </summary>
    AGENT_AGENT = 0,
    
    /// <summary>
    /// Agent contacted the ball.
    /// Triggers Ball Physics OnCollision callback.
    /// No foul data (ball contact is not a foul).
    /// </summary>
    AGENT_BALL = 1,
    
    /// <summary>
    /// Goalkeeper-specific collision (Stage 1+).
    /// Diving save, punching ball, collision while holding ball.
    /// Special foul rules apply.
    /// </summary>
    AGENT_GOALKEEPER = 2,
    
    /// <summary>
    /// Aerial duel collision (Stage 1+).
    /// Two agents competing for headed ball.
    /// Height and jump timing affect outcome.
    /// </summary>
    AERIAL_DUEL = 3
}
```

### 4.2.4 AgentBallCollisionData

```csharp
/// <summary>
/// Data passed to Ball Physics when agent-ball collision detected.
/// 
/// Interface contract between Collision System and Ball Physics (Spec #1).
/// Ball Physics uses this to compute deflection based on body part coefficients.
/// 
/// Cross-reference: Ball Physics Spec #1, Section 3.1.10.1 (BodyPart enum)
/// 
/// Memory: 32 bytes (Vector3 + Vector3 + enum + two ints + bool + padding)
/// </summary>
public struct AgentBallCollisionData
{
    /// <summary>
    /// Point where agent contacted ball (world coordinates).
    /// Used by Ball Physics to determine deflection angle.
    /// </summary>
    public Vector3 ContactPoint;
    
    /// <summary>
    /// Agent's velocity at moment of contact (m/s).
    /// 
    /// Used by Ball Physics for momentum transfer:
    ///   - Faster agent â†’ ball gains more velocity
    ///   - Stationary agent â†’ ball retains most of its velocity
    /// </summary>
    public Vector3 AgentVelocity;
    
    /// <summary>
    /// Body part that contacted the ball.
    /// 
    /// From Ball Physics Spec #1 BodyPart enum.
    /// Each body part has different speed/spin retention coefficients.
    /// 
    /// Stage 0: Always TORSO (simplification).
    /// Stage 1: Height-based detection (FOOT if z<0.3m, etc.).
    /// </summary>
    public BodyPart BodyPart;
    
    /// <summary>
    /// Agent's unique identifier (0â€“21).
    /// Used for statistics: who touched the ball.
    /// </summary>
    public int AgentID;
    
    /// <summary>
    /// Agent's team identifier.
    /// Used for possession tracking: which team controls the ball.
    /// </summary>
    public int TeamID;
    
    /// <summary>
    /// True if agent is the designated goalkeeper.
    /// 
    /// Goalkeepers have special handling in Ball Physics:
    ///   - Can catch/hold ball in penalty area
    ///   - Different deflection physics for saves
    /// 
    /// Stage 0: Flag is set but not consumed (goalkeeper treated as normal agent).
    /// Stage 1: Full goalkeeper collision handling in Goalkeeper Mechanics Spec #11.
    /// </summary>
    public bool IsGoalkeeper;
}
```

### 4.2.5 ContactForceData

```csharp
/// <summary>
/// Contact force data for foul detection.
/// 
/// Consumed by Referee System (Stage 1+) to determine:
///   - Was contact a foul?
///   - Foul severity (free kick, yellow, red)
///   - Which player committed the foul?
/// 
/// Stage 0: Struct is populated but not consumed. Data contract established
/// now to ensure Collision System captures all information needed by future
/// Referee System.
/// 
/// Memory: 28 bytes (float + Vector3 + enum + two ints + two bools + padding)
/// </summary>
public struct ContactForceData
{
    // ================================================================
    // FORCE MAGNITUDE & DIRECTION
    // ================================================================
    
    /// <summary>
    /// Force magnitude in Newtons.
    /// 
    /// Computed from impulse:
    ///   ForceMagnitude = impulse / dt (where dt = 1/60 s)
    /// 
    /// Foul severity thresholds (tentative, subject to Stage 1 tuning):
    ///   - < 300N: Normal contact (no foul)
    ///   - 300â€“600N: Minor foul (free kick)
    ///   - 600â€“1000N: Reckless foul (yellow card possible)
    ///   - > 1000N: Excessive force (red card possible)
    /// 
    /// These thresholds are placeholders. Referee System (Stage 1+) will
    /// define actual adjudication logic considering context.
    /// </summary>
    public float ForceMagnitude;
    
    /// <summary>
    /// Force direction (normalized vector).
    /// 
    /// Points in direction of force applied to victim.
    /// Used to determine:
    ///   - Push vs. trip
    ///   - Direction relative to play (ball direction)
    /// </summary>
    public Vector3 ForceDirection;
    
    // ================================================================
    // CONTACT CLASSIFICATION
    // ================================================================
    
    /// <summary>
    /// Type of contact based on relative positioning.
    /// 
    /// Classification affects foul determination:
    ///   - SHOULDER_TO_SHOULDER: Usually legal
    ///   - FROM_BEHIND: Usually foul
    ///   - SIDE_IMPACT: Context-dependent
    /// 
    /// See ContactType enum for full list.
    /// </summary>
    public ContactType Type;
    
    // ================================================================
    // PARTICIPANT IDENTIFICATION
    // ================================================================
    
    /// <summary>
    /// Agent who initiated contact (higher relative velocity toward other).
    /// 
    /// Instigator determination:
    ///   - Compare velocity dot products with collision normal
    ///   - Agent with higher approach velocity is instigator
    ///   - Ties broken by lower agent ID
    /// </summary>
    public int InstigatorAgentID;
    
    /// <summary>
    /// Agent who received contact.
    /// The other agent in the collision (not the instigator).
    /// </summary>
    public int VictimAgentID;
    
    // ================================================================
    // CONTEXT FLAGS
    // ================================================================
    
    /// <summary>
    /// True if victim was in possession of the ball at contact time.
    /// 
    /// Contact with ball-holder has different foul rules:
    ///   - Must play the ball, not the player
    ///   - Higher scrutiny for FROM_BEHIND contacts
    /// 
    /// Stage 0: Always false (no possession tracking integration yet).
    /// Stage 1: Integration with First Touch Mechanics (Spec #11).
    /// </summary>
    public bool VictimHasBall;
    
    /// <summary>
    /// True if instigator was attempting to play the ball.
    /// 
    /// "Playing the ball" mitigates foul severity:
    ///   - Genuine attempt = more lenient
    ///   - No attempt (pure physical challenge) = stricter
    /// 
    /// Stage 0: Always false (no tactical intent system yet).
    /// Stage 1: Integration with AI intent signals from Tactical Brain (Spec #7).
    /// </summary>
    public bool InstigatorPlayingBall;
}

/// <summary>
/// Contact type classification based on relative positioning.
/// 
/// Determined by comparing velocity directions and facing directions
/// at moment of contact. See Section 3.3.3 for classification algorithm.
/// </summary>
public enum ContactType
{
    /// <summary>
    /// Both agents approaching from approximately equal angles.
    /// Shoulder-to-shoulder challenges are generally legal.
    /// 
    /// Criteria: |angle_between_velocities| < 45Â°
    /// </summary>
    SHOULDER_TO_SHOULDER = 0,
    
    /// <summary>
    /// Instigator approached victim from behind.
    /// Usually a foul unless ball was clearly won first.
    /// 
    /// Criteria: Instigator velocity points within 60Â° of victim's facing direction
    /// </summary>
    FROM_BEHIND = 1,
    
    /// <summary>
    /// Instigator approached victim from the side.
    /// Foul depends on severity and whether ball was contested.
    /// 
    /// Criteria: Neither SHOULDER_TO_SHOULDER nor FROM_BEHIND
    /// </summary>
    SIDE_IMPACT = 2,
    
    /// <summary>
    /// Slide tackle contact (Stage 1+).
    /// Special hitbox geometry and foul rules.
    /// </summary>
    SLIDE_TACKLE = 3,
    
    /// <summary>
    /// Aerial challenge contact (Stage 1+).
    /// Different physics (3D collision) and foul rules.
    /// </summary>
    AERIAL_CHALLENGE = 4
}
```

---

## 4.3 Constants Organization

### 4.3.1 CollisionConstants

```csharp
/// <summary>
/// All Collision System constants organized by category.
/// 
/// Follows Ball Physics Spec #1 pattern: static class with nested classes
/// for organizational clarity. All values are compile-time constants (const)
/// or static readonly for computed values.
/// 
/// Location: CollisionConstants.cs
/// </summary>
public static class CollisionConstants
{
    // ================================================================
    // SPATIAL HASH CONFIGURATION
    // ================================================================
    
    /// <summary>
    /// Spatial hash grid configuration.
    /// </summary>
    public static class SpatialHash
    {
        /// <summary>
        /// Cell size in meters.
        /// 
        /// Derivation (Section 2.1.2):
        ///   max_hitbox_radius = 0.50m (Strength 20 agent)
        ///   max_combined_radius = 0.50 + 0.50 = 1.00m
        ///   cell_size >= max_combined_radius
        ///   
        /// Value of 1.0m ensures any two colliding agents share
        /// the same cell or adjacent cells.
        /// </summary>
        public const float CELL_SIZE = 1.0f;
        
        /// <summary>
        /// Grid width in cells (X-axis, pitch length).
        /// Covers 0â€“105m with one cell of margin.
        /// </summary>
        public const int GRID_WIDTH = 106;
        
        /// <summary>
        /// Grid height in cells (Y-axis, pitch width).
        /// Covers 0â€“68m with one cell of margin.
        /// </summary>
        public const int GRID_HEIGHT = 69;
        
        /// <summary>
        /// Total number of cells in the grid.
        /// </summary>
        public const int TOTAL_CELLS = GRID_WIDTH * GRID_HEIGHT; // 7,314
        
        /// <summary>
        /// Ball entity ID convention.
        /// Agents use 0â€“21; ball uses -1 to avoid collision.
        /// </summary>
        public const int BALL_ENTITY_ID = -1;
        
        /// <summary>
        /// Maximum entities per cell before logging warning.
        /// Normal gameplay rarely exceeds 4.
        /// Extreme clustering (corner kick) may reach 6â€“8.
        /// </summary>
        public const int CELL_DENSITY_WARNING = 8;
    }
    
    // ================================================================
    // PHYSICS PARAMETERS
    // ================================================================
    
    /// <summary>
    /// Collision physics parameters.
    /// </summary>
    public static class Physics
    {
        /// <summary>
        /// Coefficient of restitution for agent-agent collision.
        /// 
        /// Range [0, 1]: 0 = perfectly inelastic, 1 = perfectly elastic.
        /// Value of 0.3 models human bodies as highly inelastic:
        ///   - Players absorb energy on impact
        ///   - Minimal "bounce" effect
        ///   - Matches observed football collision behavior
        /// </summary>
        public const float COEFFICIENT_OF_RESTITUTION = 0.3f;
        
        /// <summary>
        /// Momentum scale factor for same-team collisions.
        /// 
        /// Teammates don't knock each other over hard:
        ///   - Natural spatial awareness
        ///   - No intentional physicality
        ///   - Gentle brushing past
        /// 
        /// Value of 0.3 means teammates transfer 30% of normal momentum.
        /// </summary>
        public const float SAME_TEAM_MOMENTUM_SCALE = 0.3f;
        
        /// <summary>
        /// Ball radius in meters.
        /// From Ball Physics Spec #1 BallPhysicsConstants.Ball.RADIUS.
        /// </summary>
        public const float BALL_RADIUS = 0.11f;
        
        /// <summary>
        /// Agent reach height for ball collision filtering.
        /// 
        /// Balls above this height require aerial challenge (Stage 1).
        /// Value of 2.0m approximates standing reach of average player.
        /// </summary>
        public const float AGENT_REACH_HEIGHT = 2.0f;
        
        /// <summary>
        /// Minimum distance to avoid division by zero.
        /// If agents are closer than this, use fallback normal.
        /// </summary>
        public const float MIN_DISTANCE_EPSILON = 0.0001f;
    }
    
    // ================================================================
    // FALL/STUMBLE THRESHOLDS
    // ================================================================
    
    /// <summary>
    /// Fall and stumble determination thresholds.
    /// </summary>
    public static class FallThresholds
    {
        /// <summary>
        /// Base force threshold for falling (Newtons).
        /// 
        /// Formula: F_threshold_fall = FALL_FORCE_BASE + (Strength Ã— FALL_FORCE_PER_STRENGTH)
        /// 
        /// At Strength 1:  550N threshold
        /// At Strength 10: 1000N threshold
        /// At Strength 20: 1500N threshold
        /// </summary>
        public const float FALL_FORCE_BASE = 500f;
        
        /// <summary>
        /// Additional force threshold per Strength point (Newtons).
        /// </summary>
        public const float FALL_FORCE_PER_STRENGTH = 50f;
        
        /// <summary>
        /// Stumble threshold as fraction of fall threshold.
        /// 
        /// Stumble occurs when:
        ///   F_threshold_stumble < F_impact â‰¤ F_threshold_fall
        /// 
        /// Where: F_threshold_stumble = F_threshold_fall Ã— STUMBLE_THRESHOLD_FRACTION
        /// </summary>
        public const float STUMBLE_THRESHOLD_FRACTION = 0.5f;
        
        /// <summary>
        /// Force range over which P(fall) goes from 0 to 1.
        /// 
        /// Probability calculation:
        ///   P_fall = clamp((F_impact - F_threshold_fall) / FALL_PROBABILITY_RANGE, 0, 1)
        /// 
        /// At threshold: 0% chance
        /// At threshold + 500N: 100% chance
        /// Linear interpolation between.
        /// </summary>
        public const float FALL_PROBABILITY_RANGE = 500f;
    }
    
    // ================================================================
    // GROUNDED STATE PARAMETERS
    // ================================================================
    
    /// <summary>
    /// Grounded state duration parameters.
    /// </summary>
    public static class GroundedState
    {
        /// <summary>
        /// Minimum time on ground (seconds).
        /// Even strongest/most agile players need this to recover.
        /// </summary>
        public const float DURATION_MIN = 0.5f;
        
        /// <summary>
        /// Maximum time on ground (seconds).
        /// Weakest/least agile players stay down this long.
        /// </summary>
        public const float DURATION_MAX = 2.0f;
        
        /// <summary>
        /// Duration range (for interpolation).
        /// </summary>
        public const float DURATION_RANGE = DURATION_MAX - DURATION_MIN; // 1.5f
    }
    
    // ================================================================
    // SAFETY LIMITS
    // ================================================================
    
    /// <summary>
    /// Safety and sanity check limits.
    /// </summary>
    public static class Safety
    {
        /// <summary>
        /// Maximum penetration depth before logging warning.
        /// Values above this indicate tunneling (missed collision last frame).
        /// </summary>
        public const float MAX_PENETRATION = 0.5f;
        
        /// <summary>
        /// Maximum collision pairs to process per frame.
        /// Prevents runaway processing in degenerate cases.
        /// 50 pairs handles even extreme clustering (corner kicks).
        /// </summary>
        public const int MAX_COLLISION_PAIRS = 50;
        
        /// <summary>
        /// Maximum iterations for any loop.
        /// Prevents infinite loops from corrupted data.
        /// </summary>
        public const int MAX_ITERATIONS = 1000;
        
        /// <summary>
        /// Maximum collision events per frame.
        /// Same as MAX_COLLISION_PAIRS (one event per pair).
        /// </summary>
        public const int MAX_EVENTS_PER_FRAME = 50;
    }
    
    // ================================================================
    // CONTACT TYPE CLASSIFICATION
    // ================================================================
    
    /// <summary>
    /// Contact type classification thresholds.
    /// </summary>
    public static class ContactClassification
    {
        /// <summary>
        /// Maximum angle (degrees) between velocities for SHOULDER_TO_SHOULDER.
        /// If angle < this value, contact is shoulder-to-shoulder.
        /// </summary>
        public const float SHOULDER_MAX_ANGLE = 45f;
        
        /// <summary>
        /// Maximum angle (degrees) from victim's back for FROM_BEHIND.
        /// If instigator approaches within this cone behind victim, it's from behind.
        /// </summary>
        public const float BEHIND_MAX_ANGLE = 60f;
    }
}
```

### 4.3.2 Configuration Management

**Stage 0 Approach:**
- All constants in code (no external config files)
- Modify `CollisionConstants.cs` directly
- Recompile required for changes
- Version controlled via Git

**Tuning Workflow:**

1. **Record Baseline**
   - Run integration test suite (Section 5)
   - Record test results (collision counts, fall rates, performance)
   - Commit baseline metrics to Git

2. **Adjust Parameter**
   - Modify single constant in `CollisionConstants.cs`
   - Document reason in code comment with date

3. **Retest**
   - Run full test suite
   - Compare to baseline metrics
   - Verify no regressions in other subsystems

4. **Visual Validation**
   - Run match simulation
   - Observe collision behavior (do falls look natural?)
   - Check that stronger players win physical duels more often

5. **Commit or Revert**
   - If improvement: Commit with explanation
   - If worse: Revert and try different value

**Tuning Log Example:**
```csharp
// 2026-02-16: Reduced COEFFICIENT_OF_RESTITUTION from 0.4 to 0.3
//             Reason: Agents were bouncing too much on collision
//             Result: More natural "stick and slide" behavior
//             Test impact: CR-001 tolerance adjusted from 0.02 to 0.01
//             Approved by: Lead Developer
public const float COEFFICIENT_OF_RESTITUTION = 0.3f;
```

---

## 4.4 Dependencies

### 4.4.1 Internal Dependencies

```
CollisionSystem
    â”œâ”€> SpatialHashGrid (instance, owned)
    â”œâ”€> DeterministicRNG (value type, per-frame)
    â”œâ”€> CollisionConstants (static)
    â””â”€> CollisionEvent[] (pre-allocated buffer)

SpatialHashGrid
    â””â”€> CollisionConstants.SpatialHash (static)

CollisionDetection (static)
    â”œâ”€> CollisionManifold (output struct)
    â”œâ”€> AgentPhysicalProperties (input, from Agent Movement)
    â”œâ”€> BallState (input, from Ball Physics)
    â””â”€> CollisionConstants.Physics (static)

CollisionResponse (static)
    â”œâ”€> CollisionResponseData (output struct)
    â”œâ”€> ContactForceData (output struct)
    â”œâ”€> CollisionConstants.Physics (static)
    â”œâ”€> CollisionConstants.FallThresholds (static)
    â””â”€> CollisionConstants.GroundedState (static)

DeterministicRNG
    â””â”€> (no dependencies â€” standalone value type)
```

**Dependency Rules:**
- No circular dependencies
- Constants never import other classes
- Data structs have zero dependencies
- Static utility classes are pure (no side effects except logging)
- CollisionSystem is the only class with mutable state

### 4.4.2 External Dependencies (Within Project)

**Ball Physics (Spec #1):**
```csharp
using TacticalDirector.Core.Physics.Ball;  // BallState, BodyPart enum
```

**Agent Movement (Spec #2):**
```csharp
using TacticalDirector.Core.Physics.Agent; // AgentPhysicalProperties, Agent class
```

**Event System (Spec #17):**
```csharp
using TacticalDirector.Core.EventSystem;   // ICollisionEventConsumer
```

### 4.4.3 External Dependencies (Unity)

**Required:**
```csharp
using UnityEngine;           // Vector2, Vector3, Mathf
using UnityEngine.Profiling; // Profiler (editor/development builds only)
```

**NOT Used:**
- Unity Physics (Rigidbody, Collider, Physics.Raycast)
- Unity NavMesh
- Unity CharacterController

All collision physics is custom implementation, consistent with Ball Physics and Agent Movement specifications.

### 4.4.4 Match Simulator Integration

**Match simulator calls collision system at 60Hz:**

```csharp
public class MatchSimulator
{
    private Agent[] _agents;          // 22 agents
    private BallState _ball;
    private CollisionSystem _collisionSystem;
    private ICollisionEventConsumer _eventConsumer;
    private float _matchTime;
    private ulong _matchSeed;
    private int _frameNumber;
    
    public void Update(float deltaTime)
    {
        _matchTime += deltaTime;
        _frameNumber++;
        
        // 1. Agent Movement (Spec #2)
        AgentMovementSystem.UpdateAllAgents(_agents, _commands, deltaTime, ...);
        
        // 2. Ball Physics (Spec #1)
        BallPhysicsCore.UpdateBallPhysics(ref _ball, deltaTime, ...);
        
        // 3. Collision System (this spec)
        _collisionSystem.UpdateCollisions(
            _agents,
            ref _ball,
            _matchSeed,
            _frameNumber,
            _matchTime,
            deltaTime);
        
        // 4. Dispatch collision events to consumers
        _collisionSystem.DispatchEvents(_eventConsumer);
        
        // 5. Event System (Spec #17)
        EventBus.ProcessPending();
    }
}
```

---

