## 3.4 Integration with Game Loop

### 3.4.1 CollisionSystem Class

```csharp
/// <summary>
/// Main collision system class.
/// Orchestrates spatial partitioning, detection, and response phases.
/// 
/// Usage:
///   Called once per frame by Match Simulator after Agent Movement and Ball Physics updates.
///   All agent positions must be finalized before calling UpdateCollisions().
/// 
/// Thread safety: NOT thread-safe. Single-threaded execution only.
/// Memory: Zero heap allocations during UpdateCollisions() after initialization.
/// </summary>
public class CollisionSystem
{
    // ================================================================
    // PRIVATE FIELDS
    // ================================================================
    
    private readonly SpatialHashGrid _spatialHash;
    private CollisionPairBitfield _processedPairs;
    private DeterministicRNG _rng;
    
    // Buffers for collision events (reused each frame)
    private readonly List<CollisionEvent> _collisionEvents;
    private int _collisionEventCount;
    
    // Agent reference for team lookup (set each frame)
    private Agent[] _agentRefs;
    
    // ================================================================
    // CONSTRUCTOR
    // ================================================================
    
    /// <summary>
    /// Initializes the collision system with pre-allocated buffers.
    /// Called once at simulation initialization.
    /// </summary>
    public CollisionSystem()
    {
        _spatialHash = new SpatialHashGrid();
        _processedPairs = new CollisionPairBitfield();
        _collisionEvents = new List<CollisionEvent>(SpatialHashConstants.MAX_COLLISION_PAIRS_PER_FRAME);
        
        // Pre-allocate event list capacity
        for (int i = 0; i < SpatialHashConstants.MAX_COLLISION_PAIRS_PER_FRAME; i++)
        {
            _collisionEvents.Add(default);
        }
    }
    
    // ================================================================
    // MAIN UPDATE
    // ================================================================
    
    /// <summary>
    /// Performs collision detection and response for all entities.
    /// 
    /// Called once per frame after Agent Movement and Ball Physics updates.
    /// Modifies agent velocities/positions and may trigger state changes.
    /// 
    /// Performance: <0.5ms worst case (see Section 2.4)
    /// </summary>
    /// <param name="agents">Array of 22 agents with finalized positions</param>
    /// <param name="ball">Ball state (modified if agent-ball collision)</param>
    /// <param name="matchSeed">Match seed for deterministic RNG</param>
    /// <param name="frameNumber">Current frame number (for RNG seeding)</param>
    /// <param name="matchTime">Current match time (for event timestamps)</param>
    /// <param name="eventBuffer">Output buffer for collision events</param>
    public void UpdateCollisions(
        Agent[] agents,
        ref BallState ball,
        ulong matchSeed,
        int frameNumber,
        float matchTime,
        ICollisionEventConsumer eventBuffer)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        using var _ = CollisionProfiler.UpdateMarker.Auto();
        #endif
        
        // Store agent references for team lookup
        _agentRefs = agents;
        
        // Initialize deterministic RNG for this frame
        _rng = new DeterministicRNG(matchSeed ^ (ulong)frameNumber);
        
        // Reset frame state
        _processedPairs.Clear();
        _collisionEventCount = 0;
        
        // ============================================================
        // PHASE 1: Populate spatial hash
        // ============================================================
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        using (CollisionProfiler.ClearMarker.Auto())
        #endif
        {
            _spatialHash.Clear();
        }
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        using (CollisionProfiler.InsertMarker.Auto())
        #endif
        {
            // Insert all agents
            for (int i = 0; i < agents.Length; i++)
            {
                var props = agents[i].PhysicalProperties;
                
                // Skip invalid positions
                if (HasInvalidValues(props.Position)) continue;
                
                _spatialHash.Insert(i, props.Position, props.HitboxRadius);
            }
            
            // Insert ball
            if (!HasInvalidValues(ball.Position))
            {
                _spatialHash.Insert(
                    SpatialHashConstants.BALL_ENTITY_ID, 
                    ball.Position, 
                    SpatialHashConstants.BALL_RADIUS);
            }
        }
        
        // ============================================================
        // PHASE 2 & 3: Broad phase query + Narrow phase detection
        // ============================================================
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        using (CollisionProfiler.BroadPhaseMarker.Auto())
        #endif
        {
            ProcessAllCollisionPairs(agents, ref ball, matchTime);
        }
        
        // ============================================================
        // PHASE 4: Publish events
        // ============================================================
        
        for (int i = 0; i < _collisionEventCount; i++)
        {
            eventBuffer?.OnCollisionEvent(_collisionEvents[i]);
        }
    }
    
    // ================================================================
    // COLLISION PAIR PROCESSING
    // ================================================================
    
    private void ProcessAllCollisionPairs(Agent[] agents, ref BallState ball, float matchTime)
    {
        int pairsProcessed = 0;
        
        for (int i = 0; i < agents.Length; i++)
        {
            var props_i = agents[i].PhysicalProperties;
            
            // Skip invalid agents
            if (HasInvalidValues(props_i.Position)) continue;
            
            // Query nearby entities
            var nearby = _spatialHash.Query(props_i.Position, props_i.HitboxRadius);
            
            foreach (int j in nearby)
            {
                // Skip self
                if (j == i) continue;
                
                // Enforce pair ordering (lower ID first) for deduplication
                int lowId = (j < i) ? j : i;
                int highId = (j < i) ? i : j;
                
                // Skip if already processed
                if (_processedPairs.IsSet(lowId, highId)) continue;
                _processedPairs.Set(lowId, highId);
                
                // Check collision pair limit
                if (++pairsProcessed > SpatialHashConstants.MAX_COLLISION_PAIRS_PER_FRAME)
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning($"[Collision] Exceeded MAX_COLLISION_PAIRS_PER_FRAME ({pairsProcessed})");
                    #endif
                    return;
                }
                
                if (j == SpatialHashConstants.BALL_ENTITY_ID)
                {
                    // Agent-ball collision
                    ProcessAgentBallCollision(agents[i], i, ref ball, matchTime);
                }
                else
                {
                    // Agent-agent collision
                    ProcessAgentAgentCollision(agents, i, j, matchTime);
                }
            }
        }
    }
    
    // ================================================================
    // AGENT-AGENT COLLISION
    // ================================================================
    
    private void ProcessAgentAgentCollision(Agent[] agents, int id1, int id2, float matchTime)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        using var _ = CollisionProfiler.NarrowPhaseMarker.Auto();
        #endif
        
        var props1 = agents[id1].PhysicalProperties;
        var props2 = agents[id2].PhysicalProperties;
        
        // Narrow phase: exact intersection test
        if (!CollisionDetection.CheckAgentAgentCollision(in props1, in props2, out var manifold))
        {
            return; // No collision
        }
        
        manifold.Entity1ID = id1;
        manifold.Entity2ID = id2;
        
        // Determine if same team
        bool isSameTeam = CollisionFiltering.IsSameTeamCollision(
            agents[id1].TeamID, agents[id2].TeamID);
        
        // Calculate response
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        using (CollisionProfiler.ResponseMarker.Auto())
        #endif
        {
            var response = CollisionResponse.CalculateAgentAgentResponse(
                in props1, in props2, in manifold, isSameTeam, ref _rng);
            
            // Apply responses to agents
            ApplyAgentResponse(agents[id1], response.VelocityImpulse1, response.PositionCorrection1,
                response.TriggerGrounded1, response.TriggerStumble1, response.GroundedDuration1);
            
            ApplyAgentResponse(agents[id2], response.VelocityImpulse2, response.PositionCorrection2,
                response.TriggerGrounded2, response.TriggerStumble2, response.GroundedDuration2);
            
            // Package foul detection data
            ContactTypeClassifier.DetermineInstigatorAndVictim(
                in props1, in props2, manifold.Normal,
                out int instigatorIdx, out int victimIdx);
            
            int instigatorId = (instigatorIdx == 0) ? id1 : id2;
            int victimId = (instigatorIdx == 0) ? id2 : id1;
            
            var foulData = new ContactForceData
            {
                ForceMagnitude = response.ImpactForce,
                ForceDirection = new Vector3(manifold.Normal.x, manifold.Normal.y, 0),
                Type = ContactTypeClassifier.Classify(
                    instigatorIdx == 0 ? in props1 : in props2,
                    instigatorIdx == 0 ? in props2 : in props1,
                    manifold.Normal),
                InstigatorAgentID = instigatorId,
                VictimAgentID = victimId,
                VictimHasBall = false, // TODO: Query possession system
                InstigatorPlayingBall = false // TODO: Query tactical system
            };
            
            // Record collision event
            RecordCollisionEvent(matchTime, CollisionType.AGENT_AGENT, id1, id2,
                new Vector3(manifold.ContactPoint.x, manifold.ContactPoint.y, 0),
                response.ImpactForce, foulData);
        }
    }
    
    // ================================================================
    // AGENT-BALL COLLISION
    // ================================================================
    
    private void ProcessAgentBallCollision(Agent agent, int agentId, ref BallState ball, float matchTime)
    {
        var props = agent.PhysicalProperties;
        
        // Narrow phase: exact intersection test
        if (!CollisionDetection.CheckAgentBallCollision(in props, in ball, out var contactPoint))
        {
            return; // No collision
        }
        
        // Package collision data for Ball Physics
        var collisionData = new AgentBallCollisionData
        {
            ContactPoint = contactPoint,
            AgentVelocity = props.Velocity,
            BodyPart = BodyPart.Torso, // Stage 0 simplification
            AgentID = agentId,
            TeamID = agent.TeamID,
            IsGoalkeeper = agent.IsGoalkeeper
        };
        
        // Callback to Ball Physics
        BallCollisionHandler.OnAgentCollision(ref ball, collisionData);
        
        // Record collision event (no foul data for agent-ball)
        RecordCollisionEvent(matchTime, CollisionType.AGENT_BALL, agentId,
            SpatialHashConstants.BALL_ENTITY_ID, contactPoint, 0f, default);
    }
    
    // ================================================================
    // RESPONSE APPLICATION
    // ================================================================
    
    private void ApplyAgentResponse(
        Agent agent,
        Vector3 velocityImpulse,
        Vector3 positionCorrection,
        bool triggerGrounded,
        bool triggerStumble,
        float groundedDuration)
    {
        // Apply velocity impulse
        if (velocityImpulse.sqrMagnitude > 0.0001f)
        {
            agent.ApplyVelocityImpulse(velocityImpulse);
        }
        
        // Apply position correction
        if (positionCorrection.sqrMagnitude > 0.0001f)
        {
            agent.ApplyPositionCorrection(positionCorrection);
        }
        
        // Trigger state changes
        if (triggerGrounded)
        {
            agent.TriggerGroundedState(GroundedReason.COLLISION, groundedDuration);
        }
        else if (triggerStumble)
        {
            agent.TriggerStumbleState();
        }
    }
    
    // ================================================================
    // EVENT RECORDING
    // ================================================================
    
    private void RecordCollisionEvent(
        float matchTime,
        CollisionType type,
        int entity1Id,
        int entity2Id,
        Vector3 contactPoint,
        float impactForce,
        ContactForceData foulData)
    {
        if (_collisionEventCount >= SpatialHashConstants.MAX_COLLISION_PAIRS_PER_FRAME)
        {
            return; // Event buffer full
        }
        
        _collisionEvents[_collisionEventCount++] = new CollisionEvent
        {
            MatchTime = matchTime,
            Type = type,
            Entity1ID = entity1Id,
            Entity2ID = entity2Id,
            ContactPoint = contactPoint,
            ImpactForce = impactForce,
            FoulData = foulData
        };
    }
    
    // ================================================================
    // VALIDATION
    // ================================================================
    
    private static bool HasInvalidValues(Vector3 position)
    {
        return float.IsNaN(position.x) || float.IsInfinity(position.x) ||
               float.IsNaN(position.y) || float.IsInfinity(position.y) ||
               float.IsNaN(position.z) || float.IsInfinity(position.z);
    }
}
```

### 3.4.2 Collision Event Interface

```csharp
/// <summary>
/// Interface for systems that consume collision events.
/// Implemented by Event System (Spec #17), statistics tracker, replay system.
/// </summary>
public interface ICollisionEventConsumer
{
    void OnCollisionEvent(CollisionEvent evt);
}

/// <summary>
/// Collision event record for replay, statistics, and foul detection.
/// </summary>
public struct CollisionEvent
{
    /// <summary>Match time when collision occurred (seconds from kickoff).</summary>
    public float MatchTime;
    
    /// <summary>Type of collision.</summary>
    public CollisionType Type;
    
    /// <summary>First entity ID (lower ID by convention).</summary>
    public int Entity1ID;
    
    /// <summary>Second entity ID (higher ID, or BALL_ENTITY_ID).</summary>
    public int Entity2ID;
    
    /// <summary>Contact point in world coordinates.</summary>
    public Vector3 ContactPoint;
    
    /// <summary>Impact force in Newtons (agent-agent only).</summary>
    public float ImpactForce;
    
    /// <summary>Foul detection data (agent-agent only).</summary>
    public ContactForceData FoulData;
}

/// <summary>
/// Collision type enumeration.
/// </summary>
public enum CollisionType
{
    /// <summary>Two agents collided.</summary>
    AGENT_AGENT,
    
    /// <summary>Agent contacted the ball.</summary>
    AGENT_BALL,
    
    /// <summary>Goalkeeper-specific collision (Stage 1+).</summary>
    AGENT_GOALKEEPER,
    
    /// <summary>Aerial duel collision (Stage 1+).</summary>
    AERIAL_DUEL
}
```

### 3.4.3 Ball Collision Handler

```csharp
/// <summary>
/// Static handler for ball collision callbacks.
/// Routes collision data to Ball Physics for deflection calculation.
/// 
/// Note: This is a stub interface. Full implementation is in Ball Physics (Spec #1).
/// The collision system only calls this; it does not modify ball state directly.
/// </summary>
public static class BallCollisionHandler
{
    /// <summary>
    /// Callback when agent contacts ball.
    /// Ball Physics handles deflection based on body part coefficients.
    /// </summary>
    public static void OnAgentCollision(ref BallState ball, AgentBallCollisionData data)
    {
        // Implementation in Ball Physics Spec #1, Section 3.1.10.1
        // This spec defines the interface; Ball Physics defines the behavior
        
        // Deflection calculation:
        //   1. Get body part coefficients (speed retention, spin retention)
        //   2. Calculate deflection normal (from contact geometry)
        //   3. Apply momentum transfer (agent velocity â†’ ball velocity)
        //   4. Update ball spin based on contact angle
        
        // Placeholder for spec documentation â€” actual code in Ball Physics
    }
}
```

---

## Section 3 Summary

| Subsection | Key Content |
|------------|-------------|
| **3.1 Spatial Partitioning** | Grid-based spatial hash (1.0m cells, 106Ã—69 grid); O(N) insert, O(1) query |
| **3.2 Collision Detection** | Circle-circle (agent-agent), circle-sphere (agent-ball); XY plane projection |
| **3.3 Collision Response** | Impulse-based momentum transfer; fall/stumble thresholds; deterministic RNG |
| **3.4 Game Loop Integration** | CollisionSystem class; event publishing; Ball Physics callback |

### Known Limitations (Stage 0)

| Limitation | Impact | Resolution Target |
|------------|--------|-------------------|
| **Agility proxy** | Grounded duration uses Strength as Agility proxy; may differ by ~0.3s | Stage 1: Add Agility to AgentPhysicalProperties |
| **Body part always TORSO** | All agent-ball contacts treated as torso contact | Stage 1: Height-based body part detection |
| **VictimHasBall always false** | Foul detection lacks possession context | Stage 1: Integration with possession system |
| **InstigatorPlayingBall always false** | Cannot determine if contact was ball-directed | Stage 1: Integration with tactical intent system |
| **Contact type classification simplified** | Uses velocity heuristics without facing direction | Stage 1: Add facing direction to classification |

---

## Cross-Reference Verification

| Reference | Verified | Notes |
|-----------|----------|-------|
| Agent Movement Â§3.5.4 (AgentPhysicalProperties) | âœ“ | All fields consumed correctly |
| Agent Movement Â§3.5.4.2 (Mass formula: 72.5â€“100 kg) | âœ“ | Used in impulse calculation |
| Agent Movement Â§3.5.4.3 (HitboxRadius formula: 0.35â€“0.50m) | âœ“ | Used in cell size derivation |
| Agent Movement Â§3.1.2 (GROUNDED/STUMBLING states) | âœ“ | State triggers defined |
| Ball Physics Â§3.1.10.1 (BodyPart enum, OnCollision) | âœ“ | Interface matches |
| Ball Physics Â§3.1.1 (Coordinate system) | âœ“ | Origin, axes consistent |
| Master Vol 1 Â§1.3 (Determinism) | âœ“ | Deterministic RNG implemented |

---

**End of Section 3**

**Page Count:** ~18 pages  
**Next Section:** Section 4 â€” Data Structures (comprehensive struct definitions)
