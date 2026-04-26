# Collision System Specification â€” Outline

**Purpose:** Detailed outline for Specification #3 (Collision System) of Stage 0 Physics Foundation. Review and approve this outline before any full section drafting begins.

**Created:** February 15, 2026, 11:30 AM PST  
**Version:** Draft Outline v1.0  
**Status:** Awaiting Review  
**Author:** Claude (AI) with Anton (Lead Developer)  
**Target Duration:** Weeks 3-4 of 20-week specification phase  
**Estimated Size:** 18-25 pages (plus appendices)

**Precedents:**
- Ball Physics Spec #1 (approved Feb 8, 2026) â€” ~50 pages, 44 tests
- Agent Movement Spec #2 (in review) â€” ~130 pages, 83 tests

---

## EXECUTIVE SUMMARY

### What This Specification Covers

The Collision System is the **spatial reasoning layer** for the simulation. It answers:
1. "Which entities are near each other?" (spatial partitioning)
2. "Are they colliding right now?" (collision detection)
3. "What happens when they collide?" (collision response physics)

### Core Responsibilities

1. **Spatial Partitioning** â€” O(N) query performance for 22 agents + 1 ball via grid-based spatial hash
2. **Agent-Agent Collision Detection** â€” Circular hitbox intersection tests
3. **Agent-Ball Collision Detection** â€” Trigger Ball Physics' `OnCollision()` callback
4. **Collision Response Physics** â€” Momentum transfer, fall/stumble determination
5. **Foul Detection Groundwork** â€” Contact force thresholds for referee system (Spec TBD)
6. **GROUNDED State Trigger** â€” Signal Agent Movement when agent falls

### What Is OUT of Scope

**Owned by other specifications:**
- Ball physics after collision response â†’ Spec #1: Ball Physics
- Agent locomotion before/after collision â†’ Spec #2: Agent Movement
- Goalkeeper diving collision â†’ Spec #10: Goalkeeper Mechanics
- Ball possession mechanics â†’ Spec #11: First Touch Mechanics
- Referee foul decision logic â†’ Spec TBD (Stage 1+)
- Pitch boundary enforcement â†’ Already in Specs #1 and #2

**Stage 1+ deferrals:**
- Aerial collision (heading duels) â€” requires jump mechanics (Stage 1)
- Slide tackle collision geometry â€” requires animation-driven hitboxes (Stage 1)
- Multi-body pile-ups â€” requires constraint solver (Stage 2+)

---

## DEPENDENCY ANALYSIS

### Consumes From (Upstream Dependencies)

| Source Spec | Interface | Data Consumed |
|-------------|-----------|---------------|
| **Ball Physics (#1)** | `BallState` | Position, Velocity, Radius (0.11m) |
| **Agent Movement (#2)** | `AgentPhysicalProperties` | Position, Velocity, Mass (72.5â€“100 kg), HitboxRadius (0.35â€“0.50m), Strength (1â€“20), IsGrounded |
| **Agent Movement (#2)** | `Agent.TeamID` | For same-team collision filtering |

### Provides To (Downstream Dependencies)

| Target Spec | Interface | Data Provided |
|-------------|-----------|---------------|
| **Ball Physics (#1)** | `Ball.OnCollision(AgentCollisionData)` | Collision point, agent velocity, body part |
| **Agent Movement (#2)** | `CollisionResponse` | Velocity impulse, GROUNDED trigger, stumble trigger |
| **Event System (#17)** | `CollisionEvent` | For replay, statistics, foul detection |
| **Referee System (TBD)** | `ContactForceData` | Force magnitude, contact type, direction |

### Coordinate System (Inherited from Ball Physics Â§3.1.1)

- Origin (0, 0, 0): Corner flag at home team's left defensive corner
- X: Along pitch length (0 to 105m)
- Y: Along pitch width (0 to 68m)  
- Z: Vertical height (0 = ground)
- All distances in meters, all velocities in m/s

---

## SECTION OUTLINE

### Section 1: Purpose & Scope (~2 pages)

**1.1 What This System Does**
- Core responsibilities (spatial partitioning, detection, response)
- Position in simulation loop (runs AFTER all movement updates complete)
- 60Hz update rate requirement

**1.2 What Is OUT of Scope**
- Table: responsibility â†’ owning specification
- Stage 1+ deferrals with rationale

**1.3 Terminology**
- Hitbox: circular 2D collision boundary (XY plane)
- Collision pair: two entities close enough to require detailed check
- Narrow phase vs broad phase
- Contact manifold: collision point + normal + penetration depth
- Impulse: instantaneous velocity change from collision

**1.4 Implementation Timeline**
- Stage 0: Full implementation
- Stage 1: Add aerial collision, slide tackles
- Stage 2+: Add constraint solver for pile-ups

---

### Section 2: System Overview (~3 pages)

**2.1 Architecture Diagram**
```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚                    COLLISION SYSTEM                         â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                                             â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”    â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”    â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚  â”‚   SPATIAL   â”‚â”€â”€â”€â–¶â”‚   BROAD     â”‚â”€â”€â”€â–¶â”‚    NARROW       â”‚ â”‚
â”‚  â”‚    HASH     â”‚    â”‚   PHASE     â”‚    â”‚    PHASE        â”‚ â”‚
â”‚  â”‚  (Insert)   â”‚    â”‚ (Query)     â”‚    â”‚  (Intersect)    â”‚ â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜    â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜    â””â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â”‚                                                  â”‚          â”‚
â”‚                                                  â–¼          â”‚
â”‚                                        â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚                                        â”‚   COLLISION     â”‚ â”‚
â”‚                                        â”‚   RESPONSE      â”‚ â”‚
â”‚                                        â”‚   (Resolve)     â”‚ â”‚
â”‚                                        â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

**2.2 Data Flow**
1. All agents complete movement update â†’ positions finalized
2. Collision System receives 22 `AgentPhysicalProperties` + 1 `BallState`
3. Insert all entities into spatial hash
4. Query pairs within collision distance
5. Narrow phase: exact intersection test
6. Collision response: apply impulses, trigger state changes
7. Return modified velocities to Agent Movement, Ball Physics

**2.3 Collision Types**
| Type | Entities | Detection Method | Response |
|------|----------|------------------|----------|
| Agent-Agent | 2 agents | Circle-circle | Momentum transfer, possible fall |
| Agent-Ball | Agent + ball | Circle-sphere | Ball deflection, possession check |
| Agent-Boundary | Agent + pitch | Point-in-rect | Position correction (handled by Agent Movement Â§3.6) |

**2.4 Performance Budget**
- Per-frame target: <0.3ms (from Master Vol 4 budget allocation)
- Worst case (all 22 agents clustered): <0.5ms
- Justification: Ball Physics Â§6.2.1 allocated 0.5ms mean, Collision System should be comparable

**2.5 Failure Modes**
| Failure | Detection | Recovery |
|---------|-----------|----------|
| NaN in position | `HasInvalidValues()` check | Restore from LastValidPosition |
| Tunneling (agents pass through) | Penetration depth > radius | Separate by minimum translation vector |
| Infinite loop in spatial hash | Iteration counter | Break after MAX_ITERATIONS, log error |

---

### Section 3: Core Systems (~10-12 pages)

This is the technical heart of the specification.

#### 3.1 Spatial Partitioning (~3 pages)

**3.1.1 Grid-Based Spatial Hash**

Design decision: Use uniform grid (not quadtree, not bounding volume hierarchy) because:
- 22 agents is small N â€” O(NÂ²) = 231 pairs, O(N) grid = ~40-60 checks typically
- Agents are uniformly distributed across pitch (no dense clustering except corners)
- Simple to implement, debug, and maintain
- Cell size = 2Ã— max agent radius = 2 Ã— 0.50m = 1.0m (ensures adjacent cell queries catch all collisions)

```csharp
/// <summary>
/// Spatial hash grid for collision broad phase.
/// Cell size chosen to ensure any two colliding entities share a cell or adjacent cells.
/// </summary>
public class SpatialHashGrid
{
    // Cell size: 2Ã— max hitbox radius ensures no collision spans >2 cells
    private const float CELL_SIZE = 1.0f;
    
    // Pitch dimensions (from Ball Physics constants)
    private const int GRID_WIDTH = 106;   // ceil(105m / 1.0m) + 1
    private const int GRID_HEIGHT = 69;   // ceil(68m / 1.0m) + 1
    
    // Cell storage: List<int> per cell (agent IDs)
    private readonly List<int>[] _cells;
    
    // Methods to document:
    // - Clear()
    // - Insert(int entityId, Vector3 position, float radius)
    // - Query(Vector3 position, float radius) â†’ List<int>
    // - GetCellIndex(float x, float y) â†’ int
}
```

**3.1.2 Query Algorithm**
- Given query position and radius, check 3Ã—3 cell neighborhood
- Return all entity IDs in those cells (excluding self)
- Deduplication: use frame-local HashSet to avoid checking same pair twice

**3.1.3 Edge Cases**
- Entity outside pitch bounds â†’ clamp to boundary cells
- Entity radius larger than cell size â†’ insert into multiple cells
- Ball near corner â†’ query may span 4 cells

#### 3.2 Collision Detection (~3 pages)

**3.2.1 Agent-Agent Detection**

Circle-circle intersection in XY plane (ground projection):
```
collision if: distance(p1, p2) < radius1 + radius2
```

```csharp
/// <summary>
/// Checks if two agents are colliding.
/// Uses 2D (XY) projection â€” height (Z) ignored for ground-based collision.
/// </summary>
public static bool CheckAgentAgentCollision(
    in AgentPhysicalProperties a1,
    in AgentPhysicalProperties a2,
    out CollisionManifold manifold)
{
    // 2D distance (XY plane only)
    float dx = a2.Position.x - a1.Position.x;
    float dy = a2.Position.y - a1.Position.y;
    float distanceSq = dx * dx + dy * dy;
    
    float combinedRadius = a1.HitboxRadius + a2.HitboxRadius;
    float combinedRadiusSq = combinedRadius * combinedRadius;
    
    if (distanceSq >= combinedRadiusSq)
    {
        manifold = default;
        return false;
    }
    
    float distance = Mathf.Sqrt(distanceSq);
    float penetration = combinedRadius - distance;
    
    // Normal points from a1 to a2
    Vector2 normal = distance > 0.0001f 
        ? new Vector2(dx / distance, dy / distance)
        : Vector2.right; // Fallback if exactly overlapping
    
    // Contact point: midpoint weighted by radii
    float t = a1.HitboxRadius / combinedRadius;
    Vector2 contactPoint = new Vector2(
        a1.Position.x + dx * t,
        a1.Position.y + dy * t);
    
    manifold = new CollisionManifold
    {
        Normal = normal,
        ContactPoint = contactPoint,
        PenetrationDepth = penetration
    };
    
    return true;
}
```

**3.2.2 Agent-Ball Detection**

Circle-sphere intersection:
```
collision if: distance(agent_XY, ball_XY) < agent_radius + ball_radius
AND ball.Z < agent_height (simplified: always true in Stage 0)
```

**3.2.3 Same-Team Filtering**
- Same-team collisions: detection YES, momentum transfer REDUCED (teammates don't knock each other over hard)
- Opposing team collisions: full momentum transfer

**3.2.4 GROUNDED Agent Handling**
- If `IsGrounded == true`, agent is still a collision obstacle
- Other agents can collide with grounded agent
- Grounded agent does NOT generate collision responses (velocity frozen)

#### 3.3 Collision Response (~4 pages)

**3.3.1 Momentum Transfer (Agent-Agent)**

Conservation of momentum with coefficient of restitution:

```
// Relative velocity along collision normal
v_rel = dot(v1 - v2, normal)

// If separating, no collision response needed
if (v_rel > 0) return;

// Impulse magnitude (elastic collision formula)
e = 0.3  // coefficient of restitution (inelastic â€” players are soft)
j = -(1 + e) Ã— v_rel / (1/m1 + 1/m2)

// Apply impulse
Î”v1 = (j / m1) Ã— normal
Î”v2 = -(j / m2) Ã— normal
```

**3.3.2 Who Falls Down? (Strength-Based)**

Not every collision causes a fall. Criteria:

```
// Impact force
F_impact = j / dt  (where dt = 1/60 s)

// Threshold based on Strength attribute
F_threshold_fall = 500 + (Strength Ã— 50)  // Newtons
// Strength 1:  550 N threshold
// Strength 10: 1000 N threshold
// Strength 20: 1500 N threshold

// Probability of falling
if (F_impact > F_threshold_fall):
    P_fall = clamp((F_impact - F_threshold_fall) / 500, 0, 1)
    if (random() < P_fall):
        trigger GROUNDED state
```

**3.3.3 Stumble vs Fall**

Below fall threshold, check stumble threshold:

```
F_threshold_stumble = F_threshold_fall Ã— 0.5

if (F_impact > F_threshold_stumble AND F_impact <= F_threshold_fall):
    P_stumble = (F_impact - F_threshold_stumble) / (F_threshold_fall - F_threshold_stumble)
    if (random() < P_stumble):
        trigger STUMBLING state in Agent Movement
```

**3.3.4 Separation (Penetration Resolution)**

After impulse applied, separate overlapping agents:

```
// Minimum translation vector
MTV = normal Ã— penetration_depth

// Split proportional to inverse mass (heavier agent moves less)
total_inv_mass = 1/m1 + 1/m2
a1.Position -= MTV Ã— (1/m1) / total_inv_mass
a2.Position += MTV Ã— (1/m2) / total_inv_mass
```

**3.3.5 Agent-Ball Response**

Ball Physics handles ball side. Collision System provides:
- Contact point
- Agent velocity at contact
- Body part (Stage 0: always TORSO for simplicity; Stage 1: add FOOT, SHIN, etc.)

```csharp
/// <summary>
/// Data passed to Ball Physics when agent-ball collision detected.
/// </summary>
public struct AgentBallCollisionData
{
    public Vector3 ContactPoint;
    public Vector3 AgentVelocity;
    public BodyPart BodyPart;      // From Ball Physics enum
    public int AgentID;
    public int TeamID;
    public bool IsGoalkeeper;
}
```

**3.3.6 Foul Detection Data**

Package collision data for future referee system:

```csharp
/// <summary>
/// Contact force data for foul detection.
/// Consumed by Referee System (TBD, Stage 1+).
/// </summary>
public struct ContactForceData
{
    public float ForceMagnitude;     // Newtons
    public Vector3 ForceDirection;   // Normalized
    public ContactType Type;         // SHOULDER_TO_SHOULDER, FROM_BEHIND, SLIDE_TACKLE, etc.
    public int InstigatorAgentID;    // Who initiated contact
    public int VictimAgentID;        // Who received contact
    public bool VictimHasBall;       // Was victim in possession?
    public bool InstigatorPlayingBall; // Was instigator going for ball?
}

public enum ContactType
{
    SHOULDER_TO_SHOULDER,   // Legal (usually)
    FROM_BEHIND,            // Usually foul
    SIDE_IMPACT,            // Context-dependent
    SLIDE_TACKLE,           // Stage 1+
    AERIAL_CHALLENGE        // Stage 1+
}
```

#### 3.4 Integration with Game Loop (~2 pages)

**3.4.1 Frame Execution Order**

```
Frame N:
  1. Input processing
  2. Tactical AI (10Hz heartbeat, every 6th frame)
  3. Agent Movement update (all 22 agents) â† positions finalized
  4. Ball Physics update                    â† ball position finalized
  5. >>> COLLISION SYSTEM <<<
     5a. Clear spatial hash
     5b. Insert all 22 agents + ball
     5c. Broad phase: query collision pairs
     5d. Narrow phase: intersection tests
     5e. Collision response: apply impulses
     5f. Trigger state changes (GROUNDED, STUMBLING)
     5g. Package collision events for Event System
  6. Event System processing
  7. Rendering (decoupled, may interpolate)
```

**3.4.2 Collision System Update Pseudocode**

```csharp
public void UpdateCollisions(
    AgentPhysicalProperties[] agents,  // 22 agents
    ref BallState ball,
    CollisionEventBuffer eventBuffer,
    float dt)
{
    // Clear from previous frame
    _spatialHash.Clear();
    _processedPairs.Clear();
    
    // Insert all agents
    for (int i = 0; i < agents.Length; i++)
    {
        _spatialHash.Insert(i, agents[i].Position, agents[i].HitboxRadius);
    }
    
    // Insert ball (ID = -1 convention)
    _spatialHash.Insert(BALL_ID, ball.Position, BallPhysicsConstants.Ball.RADIUS);
    
    // Process each agent
    for (int i = 0; i < agents.Length; i++)
    {
        var nearby = _spatialHash.Query(agents[i].Position, agents[i].HitboxRadius);
        
        foreach (int j in nearby)
        {
            // Skip self
            if (j == i) continue;
            
            // Skip already-processed pairs
            var pairKey = (Mathf.Min(i, j), Mathf.Max(i, j));
            if (_processedPairs.Contains(pairKey)) continue;
            _processedPairs.Add(pairKey);
            
            if (j == BALL_ID)
            {
                // Agent-ball collision
                ProcessAgentBallCollision(ref agents[i], ref ball, eventBuffer);
            }
            else
            {
                // Agent-agent collision
                ProcessAgentAgentCollision(ref agents[i], ref agents[j], eventBuffer, dt);
            }
        }
    }
}
```

---

### Section 4: Data Structures (~3 pages)

**4.1 CollisionManifold**
```csharp
public struct CollisionManifold
{
    public Vector2 Normal;          // Collision normal (2D, XY plane)
    public Vector2 ContactPoint;    // Point of contact (2D)
    public float PenetrationDepth;  // How much entities overlap (meters)
}
```

**4.2 CollisionResponse**
```csharp
public struct CollisionResponse
{
    public Vector3 VelocityImpulse;  // Add to agent velocity
    public Vector3 PositionCorrection; // Add to agent position (separation)
    public bool TriggerGrounded;     // Set GROUNDED state
    public bool TriggerStumble;      // Set STUMBLING state
    public float GroundedDuration;   // How long to stay down (seconds)
}
```

**4.3 CollisionEvent**
```csharp
public struct CollisionEvent
{
    public float MatchTime;          // When collision occurred
    public CollisionType Type;       // AGENT_AGENT, AGENT_BALL
    public int Entity1ID;
    public int Entity2ID;
    public Vector3 ContactPoint;
    public float ImpactForce;        // Newtons
    public ContactForceData FoulData; // For referee system
}

public enum CollisionType
{
    AGENT_AGENT,
    AGENT_BALL,
    AGENT_GOALKEEPER,  // Stage 1+
    AERIAL_DUEL        // Stage 1+
}
```

**4.4 Constants**
```csharp
public static class CollisionConstants
{
    // Spatial hash
    public const float CELL_SIZE = 1.0f;
    public const int GRID_WIDTH = 106;
    public const int GRID_HEIGHT = 69;
    
    // Physics
    public const float COEFFICIENT_OF_RESTITUTION = 0.3f;
    public const float SAME_TEAM_MOMENTUM_SCALE = 0.3f;  // Teammates don't knock hard
    
    // Fall/stumble thresholds
    public const float FALL_FORCE_BASE = 500f;           // Newtons
    public const float FALL_FORCE_PER_STRENGTH = 50f;    // Additional N per Strength point
    public const float STUMBLE_THRESHOLD_FRACTION = 0.5f; // Stumble at 50% of fall threshold
    public const float FALL_PROBABILITY_RANGE = 500f;    // Force range over which P(fall) goes 0â†’1
    
    // Grounded duration
    public const float GROUNDED_DURATION_MIN = 0.5f;     // Seconds
    public const float GROUNDED_DURATION_MAX = 2.0f;     // Seconds
    
    // Safety limits
    public const float MAX_PENETRATION = 0.5f;           // Flag if exceeded
    public const int MAX_COLLISION_PAIRS_PER_FRAME = 50; // Sanity check
}
```

---

### Section 5: Testing (~4 pages)

**5.1 Unit Tests (Target: 25+)**

| Category | Test ID | Description |
|----------|---------|-------------|
| Spatial Hash | SH-001 | Insert at grid center, query returns self |
| Spatial Hash | SH-002 | Insert two agents 0.5m apart, query returns both |
| Spatial Hash | SH-003 | Insert two agents 2m apart, query returns only self |
| Spatial Hash | SH-004 | Insert at pitch boundary, no index-out-of-bounds |
| Spatial Hash | SH-005 | Insert 22 agents, verify all inserted |
| Detection | CD-001 | Two agents exactly touching (d = r1+r2), collision TRUE |
| Detection | CD-002 | Two agents not touching (d = r1+r2+0.01), collision FALSE |
| Detection | CD-003 | Two agents overlapping (d = 0), collision TRUE, penetration = r1+r2 |
| Detection | CD-004 | Agent-ball touching, collision TRUE |
| Detection | CD-005 | Agent-ball not touching, collision FALSE |
| Detection | CD-006 | Grounded agent still detectable as obstacle |
| Response | CR-001 | Equal mass head-on, velocities swap (Â±tolerance) |
| Response | CR-002 | Heavy agent hits light agent, heavy barely slows |
| Response | CR-003 | Same-team collision, reduced momentum transfer |
| Response | CR-004 | Separation resolves penetration completely |
| Response | CR-005 | Zero relative velocity (parallel motion), no impulse |
| Fall Logic | FL-001 | Force below stumble threshold, no state change |
| Fall Logic | FL-002 | Force at stumble threshold, P(stumble) > 0 |
| Fall Logic | FL-003 | Force at fall threshold, P(fall) > 0 |
| Fall Logic | FL-004 | Strength 20 requires more force than Strength 1 |
| Integration | INT-001 | 22 agents random positions, no crashes |
| Integration | INT-002 | Two agents collide mid-pitch, correct response |
| Integration | INT-003 | Agent-ball collision triggers Ball.OnCollision |
| Edge Cases | EC-001 | Agent at position NaN, recovers gracefully |
| Edge Cases | EC-002 | Penetration > MAX_PENETRATION, logged as warning |

**5.2 Integration Tests (Target: 8+)**

| Test ID | Scenario | Validation |
|---------|----------|------------|
| IT-001 | Two agents run toward each other | Both bounce back, neither falls (low speed) |
| IT-002 | Sprinting agent hits stationary agent | Stationary agent knocked back, may fall |
| IT-003 | Agent runs into ball | Ball.OnCollision called, ball deflects |
| IT-004 | Three agents cluster | All three processed without infinite loop |
| IT-005 | Agent falls, another agent runs over | Grounded agent is obstacle, runner stumbles |
| IT-006 | Full match simulation (90 min) | No crashes, no NaN propagation |
| IT-007 | Corner kick scenario (clustered agents) | Performance <0.5ms |
| IT-008 | Same-team collision | Reduced momentum, no fall |

**5.3 Performance Tests**

| Test ID | Scenario | Target |
|---------|----------|--------|
| PERF-001 | 22 agents evenly distributed | <0.15ms |
| PERF-002 | 22 agents clustered in penalty area | <0.4ms |
| PERF-003 | 22 agents + ball, worst case | <0.5ms |
| PERF-004 | 1000 consecutive frames | No memory growth (no leaks) |

---

### Section 6: Performance Analysis (~2 pages)

**6.1 Complexity Analysis**

| Phase | Complexity | Justification |
|-------|------------|---------------|
| Insert (all agents) | O(N) | Each agent â†’ one cell |
| Query (per agent) | O(1) average | 3Ã—3 cell neighborhood, typically 2-4 agents |
| Narrow phase | O(K) | K = number of actual collision pairs, typically <10 |
| Response | O(K) | Per collision pair |
| **Total** | **O(N + K)** | Linear in agents + collisions |

**6.2 Memory Budget**
- Spatial hash: 106 Ã— 69 = 7314 cells Ã— ~24 bytes/cell = ~175 KB
- Collision manifolds: 50 max Ã— 20 bytes = 1 KB
- Total: <200 KB

**6.3 Profiling Markers**
```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
Profiler.BeginSample("Collision.Update");
Profiler.BeginSample("Collision.SpatialHash.Clear");
Profiler.BeginSample("Collision.SpatialHash.Insert");
Profiler.BeginSample("Collision.BroadPhase");
Profiler.BeginSample("Collision.NarrowPhase");
Profiler.BeginSample("Collision.Response");
Profiler.EndSample();
#endif
```

---

### Section 7: Future Extensions (~2 pages)

**7.1 Stage 1 Extensions**
- Aerial collision (heading duels) â€” requires jump state from Agent Movement
- Slide tackle geometry â€” animation-driven capsule hitbox
- Goalkeeper collision special cases
- Body part detection (foot, shin, torso, head)

**7.2 Stage 2+ Extensions**
- Constraint solver for pile-ups (>2 agents in contact)
- Continuous collision detection (prevent tunneling at high speeds)
- Soft-body approximation for realistic deformation

**7.3 Extensibility Notes**
- `ContactType` enum designed for extension
- `CollisionResponse` struct has reserved fields for Stage 1+ data
- Spatial hash cell size is configurable constant

---

### Section 8: References (~1 page)

**8.1 Academic Sources**
- Ericson, C. (2005). "Real-Time Collision Detection." Morgan Kaufmann. â€” Spatial partitioning, narrow phase algorithms
- Catto, E. (GDC 2014). "Understanding Constraints." â€” Impulse-based collision response

**8.2 Internal Project Documents**
- Ball Physics Spec #1 â€” Coordinate system, BallState struct, OnCollision interface
- Agent Movement Spec #2 â€” AgentPhysicalProperties struct, GROUNDED/STUMBLING states
- Master Vol 1 â€” Â§6.10 Physical Contests (shoulder charge, holding, obstruction)
- Development Best Practices â€” Spatial hashing code pattern (Â§7)

---

### Appendices

**Appendix A: Formula Derivations**
- Impulse formula derivation from conservation of momentum
- Coefficient of restitution selection rationale
- Fall threshold formula derivation

**Appendix B: Test Data Sets**
- Sample agent configurations for unit tests
- Expected collision manifold values

**Appendix C: Tolerance Justifications**
- Why 0.001m tolerance for position comparisons
- Why 0.01 m/s tolerance for velocity comparisons

---

### Section 9: Approval Checklist

Follow Ball Physics Approval Checklist format exactly.

| # | Requirement | Status | Evidence |
|---|-------------|--------|----------|
| 1 | All template sections present (1â€“9 + Appendices) | â˜ | |
| 2 | Formulas include derivations | â˜ | Appendix A |
| 3 | Edge cases documented with recovery procedures | â˜ | Section 2.5, Section 3.1.3 |
| 4 | Test scenarios defined (min 10 unit + 5 integration) | â˜ | Section 5 (25+ unit, 8 integration) |
| 5 | Performance targets specified with budget context | â˜ | Section 2.4, Section 6 |
| 6 | Failure modes enumerated with recovery | â˜ | Section 2.5 |

---

## OUTLINE CRITIQUE

### Potential Issues Identified

1. **Aerial collision scope ambiguity** â€” Outline defers to Stage 1, but heading mechanics (Spec #9) is Stage 0. Need to clarify: does Spec #9 own aerial collision detection, or does it delegate to Collision System?
   - **Resolution:** Add clarification in Section 1.2 that Spec #9 (Heading Mechanics) defines aerial duel logic; Collision System only handles ground-based collision in Stage 0.

2. **Ball possession handoff** â€” When agent-ball collision occurs, who decides if possession transfers? Ball Physics? First Touch Mechanics? Collision System?
   - **Resolution:** Collision System detects contact and calls `Ball.OnCollision()`. Ball Physics handles deflection. First Touch Mechanics (Spec #11) handles possession transfer decision. Document this in Section 3.3.5.

3. **Goalkeeper collision** â€” Outline mentions "AGENT_GOALKEEPER" collision type but defers to Stage 1. However, Goalkeeper Mechanics (Spec #11) is Stage 0.
   - **Resolution:** Stage 0 collision treats goalkeeper as normal agent with special flag. Goalkeeper-specific collision (diving, punching ball) deferred to Spec #10 to handle.

4. **Determinism requirement** â€” Master Vol 1 Â§1.3 requires deterministic simulation. Collision System uses `random()` for fall/stumble probability. This breaks determinism unless seeded.
   - **Resolution:** Use deterministic RNG from Match seed, not `System.Random`. Document in Section 3.3.2.

5. **Same-team collision "reduced momentum"** â€” What is the gameplay justification? Teammates shouldn't bounce off each other like enemies.
   - **Resolution:** Add rationale in constants section: teammates naturally avoid hard contact, reduced momentum simulates awareness.

### Cross-Reference Verification Needed

- [ ] Ball Physics Â§3.1.10: Confirm `Ball.OnCollision()` interface matches our `AgentBallCollisionData`
- [ ] Agent Movement Â§3.5.4: Verify `AgentPhysicalProperties` fields match what we consume
- [ ] Agent Movement Â§3.1: Verify GROUNDED and STUMBLING state trigger interface
- [ ] Master Vol 1 Â§6.10: Verify foul detection thresholds align with physical contests description

---

## ESTIMATED EFFORT

| Section | Est. Hours | Complexity |
|---------|------------|------------|
| Section 1 (Purpose & Scope) | 1 | Low |
| Section 2 (System Overview) | 2 | Low |
| Section 3 (Core Systems) | 8 | **High** |
| Section 4 (Data Structures) | 2 | Low |
| Section 5 (Testing) | 3 | Medium |
| Section 6 (Performance) | 2 | Medium |
| Section 7 (Future Extensions) | 1 | Low |
| Section 8 (References) | 1 | Low |
| Appendices | 4 | Medium |
| Section 9 (Approval Checklist) | 1 | Low |
| **Total** | **25 hours** | |

Compared to Agent Movement (~80 hours), this is significantly smaller because:
- Fewer physics models (no fatigue, no state machine with hysteresis)
- Spatial hash is well-established algorithm
- Collision response is standard impulse formula

---

## NEXT STEPS

1. **Review this outline** â€” Approve or request changes
2. **Resolve aerial collision scope** â€” Confirm with Spec #9 (Heading Mechanics) ownership
3. **Begin Section 1 & 2** â€” Foundation sections
4. **Draft Section 3** â€” Core technical content
5. **Complete remaining sections** â€” Iterative drafting with critique cycles

---

**END OF OUTLINE**
