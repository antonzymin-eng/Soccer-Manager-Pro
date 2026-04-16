## 3.3 Collision Response

Collision response computes the physical effects of detected collisions: velocity changes (impulses), position corrections (separation), and state triggers (stumble/fall).

### 3.3.1 Response Constants

```csharp
/// <summary>
/// Constants for collision response calculations.
/// 
/// All physics constants include derivation rationale.
/// Values marked "empirically tuned" require gameplay validation.
/// </summary>
public static class CollisionResponseConstants
{
    // ================================================================
    // IMPULSE PHYSICS
    // ================================================================
    
    /// <summary>
    /// Coefficient of restitution for agent-agent collisions.
    /// 
    /// Value: 0.3 (highly inelastic â€” players are soft bodies)
    /// 
    /// Derivation:
    ///   e = 1.0 â†’ perfectly elastic (billiard balls)
    ///   e = 0.0 â†’ perfectly inelastic (clay)
    ///   Human bodies: e â‰ˆ 0.2â€“0.4 depending on padding/tension
    ///   Football players (muscular, braced): e â‰ˆ 0.3
    /// 
    /// Reference: Empirically tuned, no direct academic source.
    /// </summary>
    public const float COEFFICIENT_OF_RESTITUTION = 0.3f;
    
    /// <summary>
    /// Momentum scale for same-team collisions.
    /// 
    /// Value: 0.3 (teammates don't knock each other hard)
    /// 
    /// Rationale: Real players on the same team exhibit spatial
    /// awareness and naturally soften contact with teammates.
    /// Reduced momentum simulates this implicit coordination.
    /// </summary>
    public const float SAME_TEAM_MOMENTUM_SCALE = 0.3f;
    
    // ================================================================
    // FALL/STUMBLE THRESHOLDS
    // ================================================================
    
    /// <summary>
    /// Base force threshold for falling (Newtons).
    /// Agent with Strength 1 falls at forces above 550 N.
    /// 
    /// Derivation:
    ///   F_threshold = FALL_FORCE_BASE + (Strength Ã— FALL_FORCE_PER_STRENGTH)
    ///   Strength 1:  500 + (1 Ã— 50) = 550 N
    ///   Strength 10: 500 + (10 Ã— 50) = 1000 N
    ///   Strength 20: 500 + (20 Ã— 50) = 1500 N
    /// 
    /// Reference: Empirically tuned based on desired gameplay feel.
    /// Target: Strength 10 agent falls from full-sprint collision (~8 m/s).
    /// </summary>
    public const float FALL_FORCE_BASE = 500f;
    
    /// <summary>
    /// Additional force threshold per Strength point (Newtons per point).
    /// </summary>
    public const float FALL_FORCE_PER_STRENGTH = 50f;
    
    /// <summary>
    /// Stumble threshold as fraction of fall threshold.
    /// Agent stumbles when force is between 50%â€“100% of fall threshold.
    /// </summary>
    public const float STUMBLE_THRESHOLD_FRACTION = 0.5f;
    
    /// <summary>
    /// Force range over which fall probability transitions 0â†’1.
    /// Once force exceeds fall threshold, probability increases
    /// linearly until force exceeds threshold + this range.
    /// </summary>
    public const float FALL_PROBABILITY_RANGE = 500f;
    
    // ================================================================
    // GROUNDED DURATION
    // ================================================================
    
    /// <summary>
    /// Minimum time on ground after collision-induced fall (seconds).
    /// Modified by agent attributes â€” see CalculateGroundedDuration().
    /// </summary>
    public const float GROUNDED_DURATION_MIN = 0.5f;
    
    /// <summary>
    /// Maximum time on ground after collision-induced fall (seconds).
    /// </summary>
    public const float GROUNDED_DURATION_MAX = 2.0f;
    
    /// <summary>
    /// Base duration on ground for collision falls (seconds).
    /// Modified by Agility attribute.
    /// </summary>
    public const float GROUNDED_DURATION_BASE = 1.2f;
    
    /// <summary>
    /// Duration reduction per Agility point (seconds per point).
    /// Agility 20 agent: 1.2 - (20 Ã— 0.03) = 0.6s
    /// Agility 1 agent:  1.2 - (1 Ã— 0.03) = 1.17s
    /// </summary>
    public const float GROUNDED_DURATION_PER_AGILITY = 0.03f;
    
    // ================================================================
    // SAFETY LIMITS
    // ================================================================
    
    /// <summary>
    /// Maximum impulse magnitude (kgÂ·m/s).
    /// Prevents physics explosions from extreme inputs.
    /// 
    /// Derivation:
    ///   max_velocity = 10.2 m/s (sprint speed, Agent Movement Â§3.2)
    ///   max_mass = 100 kg (Strength 20, Agent Movement Â§3.5.4.2)
    ///   max_relative_velocity = 2 Ã— 10.2 = 20.4 m/s (head-on collision)
    ///   max_impulse â‰ˆ m Ã— Î”v = 100 Ã— 20.4 Ã— (1 + e) / 2 â‰ˆ 1300 kgÂ·m/s
    ///   
    ///   Safety ceiling: 2000 kgÂ·m/s (50% margin)
    /// </summary>
    public const float MAX_IMPULSE_MAGNITUDE = 2000f;
    
    /// <summary>
    /// Maximum penetration depth before flagging as tunneling (meters).
    /// If exceeded, use gentle separation to avoid physics explosion.
    /// </summary>
    public const float MAX_PENETRATION_DEPTH = 0.5f;
}
```

### 3.3.2 Impulse Calculation

```csharp
/// <summary>
/// Collision response calculations.
/// Implements impulse-based collision resolution with momentum conservation.
/// </summary>
public static class CollisionResponse
{
    /// <summary>
    /// Calculates collision response for two agents.
    /// 
    /// Physics model:
    ///   Conservation of momentum with coefficient of restitution.
    ///   Impulse applied along collision normal.
    ///   Same-team collisions receive reduced impulse.
    ///   Grounded agents do not receive impulses (obstacle only).
    /// 
    /// Formula derivation (Appendix A):
    ///   v_rel = dot(v1 - v2, normal)      // Relative velocity along normal
    ///   j = -(1 + e) Ã— v_rel / (1/m1 + 1/m2)  // Impulse magnitude
    ///   Î”v1 = (j / m1) Ã— normal            // Velocity change for agent 1
    ///   Î”v2 = -(j / m2) Ã— normal           // Velocity change for agent 2
    /// </summary>
    /// <param name="a1">First agent (modified if not grounded)</param>
    /// <param name="a2">Second agent (modified if not grounded)</param>
    /// <param name="manifold">Collision manifold from detection phase</param>
    /// <param name="isSameTeam">True if agents are on same team</param>
    /// <param name="rng">Deterministic RNG for fall/stumble probability</param>
    /// <returns>Response data including impulses and state triggers</returns>
    public static AgentAgentCollisionResult CalculateAgentAgentResponse(
        in AgentPhysicalProperties a1,
        in AgentPhysicalProperties a2,
        in CollisionManifold manifold,
        bool isSameTeam,
        ref DeterministicRNG rng)
    {
        var result = new AgentAgentCollisionResult();
        
        // ============================================================
        // STEP 1: Handle grounded agents
        // ============================================================
        
        // Grounded agents are obstacles but don't generate impulses
        bool a1Active = !a1.IsGrounded;
        bool a2Active = !a2.IsGrounded;
        
        if (!a1Active && !a2Active)
        {
            // Both grounded â€” no response needed
            return result;
        }
        
        // ============================================================
        // STEP 2: Calculate relative velocity along collision normal
        // ============================================================
        
        // 2D velocity projection (ignore Z component)
        Vector2 v1_2d = new Vector2(a1.Velocity.x, a1.Velocity.y);
        Vector2 v2_2d = new Vector2(a2.Velocity.x, a2.Velocity.y);
        
        // Relative velocity of a1 with respect to a2
        Vector2 relativeVelocity = v1_2d - v2_2d;
        
        // Component along collision normal
        float vRel = Vector2.Dot(relativeVelocity, manifold.Normal);
        
        // If agents are separating, no impulse needed
        if (vRel > 0)
        {
            // Still need to resolve penetration
            CalculateSeparation(in a1, in a2, in manifold, a1Active, a2Active, ref result);
            return result;
        }
        
        // ============================================================
        // STEP 3: Calculate impulse magnitude
        // ============================================================
        
        float e = CollisionResponseConstants.COEFFICIENT_OF_RESTITUTION;
        
        // Inverse masses (grounded agent has infinite mass â†’ inv_mass = 0)
        float invMass1 = a1Active ? (1.0f / a1.Mass) : 0f;
        float invMass2 = a2Active ? (1.0f / a2.Mass) : 0f;
        float invMassSum = invMass1 + invMass2;
        
        // Guard against division by zero (shouldn't happen with at least one active)
        if (invMassSum < 0.0001f)
        {
            return result;
        }
        
        // Impulse magnitude
        float j = -(1f + e) * vRel / invMassSum;
        
        // Apply same-team reduction
        if (isSameTeam)
        {
            j *= CollisionResponseConstants.SAME_TEAM_MOMENTUM_SCALE;
        }
        
        // Clamp to safety limit
        j = Mathf.Clamp(j, -CollisionResponseConstants.MAX_IMPULSE_MAGNITUDE, 
                           CollisionResponseConstants.MAX_IMPULSE_MAGNITUDE);
        
        // ============================================================
        // STEP 4: Calculate velocity impulses
        // ============================================================
        
        Vector2 impulse = j * manifold.Normal;
        
        if (a1Active)
        {
            result.VelocityImpulse1 = new Vector3(
                impulse.x * invMass1,
                impulse.y * invMass1,
                0f); // No Z component for ground collision
        }
        
        if (a2Active)
        {
            result.VelocityImpulse2 = new Vector3(
                -impulse.x * invMass2,
                -impulse.y * invMass2,
                0f);
        }
        
        // ============================================================
        // STEP 5: Calculate penetration separation
        // ============================================================
        
        CalculateSeparation(in a1, in a2, in manifold, a1Active, a2Active, ref result);
        
        // ============================================================
        // STEP 6: Calculate impact force and state triggers
        // ============================================================
        
        // Impact force (used for fall/stumble determination)
        // F = j / dt where dt = 1/60 s
        float impactForce = Mathf.Abs(j) * 60f; // Convert impulse to force
        
        result.ImpactForce = impactForce;
        
        // Determine fall/stumble for each active agent
        // 
        // STAGE 0 SIMPLIFICATION: Agility attribute is not exposed in AgentPhysicalProperties.
        // For grounded duration calculation, we use Strength as a proxy for Agility.
        // Rationale: Stronger players tend to have slower recovery (heavier build),
        // while weaker/lighter players recover faster. This approximation is acceptable
        // for Stage 0. In Stage 1+, AgentPhysicalProperties should be extended to include
        // Agility, or the collision system should access the full Agent class.
        //
        // Impact: Grounded duration will be slightly different than if using true Agility.
        // A Strength 20 player uses duration calculation as if Agility = 20, giving 0.6s.
        // If their true Agility is 10, the correct duration would be 0.9s.
        // This 0.3s difference is acceptable for Stage 0 prototype.
        
        if (a1Active)
        {
            DetermineFallOrStumble(a1.Strength, impactForce, isSameTeam, ref rng,
                out result.TriggerGrounded1, out result.TriggerStumble1,
                out result.GroundedDuration1, a1.Strength); // Using Strength as Agility proxy (Stage 0)
        }
        
        if (a2Active)
        {
            DetermineFallOrStumble(a2.Strength, impactForce, isSameTeam, ref rng,
                out result.TriggerGrounded2, out result.TriggerStumble2,
                out result.GroundedDuration2, a2.Strength); // Using Strength as Agility proxy (Stage 0)
        }
        
        return result;
    }
    
    /// <summary>
    /// Calculates position separation to resolve penetration.
    /// Distributes separation inversely proportional to mass.
    /// </summary>
    private static void CalculateSeparation(
        in AgentPhysicalProperties a1,
        in AgentPhysicalProperties a2,
        in CollisionManifold manifold,
        bool a1Active,
        bool a2Active,
        ref AgentAgentCollisionResult result)
    {
        if (manifold.PenetrationDepth <= 0) return;
        
        // Check for tunneling (excessive penetration)
        bool isTunneling = manifold.PenetrationDepth > CollisionResponseConstants.MAX_PENETRATION_DEPTH;
        
        // Separation amount (slightly more than penetration to prevent re-collision)
        float separation = manifold.PenetrationDepth * 1.01f;
        
        if (isTunneling)
        {
            // Gentle separation for tunneling â€” don't over-correct
            separation = CollisionResponseConstants.MAX_PENETRATION_DEPTH;
        }
        
        // Distribute separation based on inverse mass
        float invMass1 = a1Active ? (1.0f / a1.Mass) : 0f;
        float invMass2 = a2Active ? (1.0f / a2.Mass) : 0f;
        float invMassSum = invMass1 + invMass2;
        
        if (invMassSum < 0.0001f) return;
        
        Vector2 mtv = manifold.Normal * separation;
        
        if (a1Active)
        {
            float ratio1 = invMass1 / invMassSum;
            result.PositionCorrection1 = new Vector3(-mtv.x * ratio1, -mtv.y * ratio1, 0f);
        }
        
        if (a2Active)
        {
            float ratio2 = invMass2 / invMassSum;
            result.PositionCorrection2 = new Vector3(mtv.x * ratio2, mtv.y * ratio2, 0f);
        }
    }
    
    /// <summary>
    /// Determines if collision force causes fall or stumble.
    /// Uses deterministic RNG for probability rolls.
    /// </summary>
    private static void DetermineFallOrStumble(
        int strength,
        float impactForce,
        bool isSameTeam,
        ref DeterministicRNG rng,
        out bool triggerGrounded,
        out bool triggerStumble,
        out float groundedDuration,
        int agility)
    {
        triggerGrounded = false;
        triggerStumble = false;
        groundedDuration = 0f;
        
        // Same-team collisions cannot trigger GROUNDED (design decision Â§1.6.4)
        // but can trigger STUMBLE
        
        // Calculate thresholds
        float fallThreshold = CollisionResponseConstants.FALL_FORCE_BASE + 
                              (strength * CollisionResponseConstants.FALL_FORCE_PER_STRENGTH);
        float stumbleThreshold = fallThreshold * CollisionResponseConstants.STUMBLE_THRESHOLD_FRACTION;
        
        // Check fall condition (not for same-team)
        if (!isSameTeam && impactForce > fallThreshold)
        {
            // Calculate fall probability
            float excessForce = impactForce - fallThreshold;
            float fallProbability = Mathf.Clamp01(
                excessForce / CollisionResponseConstants.FALL_PROBABILITY_RANGE);
            
            float roll = rng.NextFloat();
            if (roll < fallProbability)
            {
                triggerGrounded = true;
                groundedDuration = CalculateGroundedDuration(agility);
                return; // Fall takes precedence over stumble
            }
        }
        
        // Check stumble condition
        if (impactForce > stumbleThreshold && impactForce <= fallThreshold)
        {
            float stumbleProbability = (impactForce - stumbleThreshold) / 
                                       (fallThreshold - stumbleThreshold);
            
            float roll = rng.NextFloat();
            if (roll < stumbleProbability)
            {
                triggerStumble = true;
            }
        }
    }
    
    /// <summary>
    /// Calculates how long an agent stays on the ground after falling.
    /// Based on Agility attribute.
    /// 
    /// Formula:
    ///   duration = GROUNDED_DURATION_BASE - (Agility Ã— GROUNDED_DURATION_PER_AGILITY)
    ///   Clamped to [GROUNDED_DURATION_MIN, GROUNDED_DURATION_MAX]
    /// 
    /// Examples:
    ///   Agility 1:  1.2 - 0.03 = 1.17s
    ///   Agility 10: 1.2 - 0.30 = 0.90s
    ///   Agility 20: 1.2 - 0.60 = 0.60s
    /// </summary>
    private static float CalculateGroundedDuration(int agility)
    {
        float duration = CollisionResponseConstants.GROUNDED_DURATION_BASE - 
                         (agility * CollisionResponseConstants.GROUNDED_DURATION_PER_AGILITY);
        
        return Mathf.Clamp(duration,
            CollisionResponseConstants.GROUNDED_DURATION_MIN,
            CollisionResponseConstants.GROUNDED_DURATION_MAX);
    }
}
```

### 3.3.3 Agent-Agent Collision Result Structure

```csharp
/// <summary>
/// Result of agent-agent collision response calculation.
/// Contains velocity impulses, position corrections, and state triggers for both agents.
/// </summary>
public struct AgentAgentCollisionResult
{
    // ================================================================
    // AGENT 1 RESPONSE
    // ================================================================
    
    /// <summary>Velocity change for agent 1 (m/s).</summary>
    public Vector3 VelocityImpulse1;
    
    /// <summary>Position correction for agent 1 (meters).</summary>
    public Vector3 PositionCorrection1;
    
    /// <summary>True if agent 1 should enter GROUNDED state.</summary>
    public bool TriggerGrounded1;
    
    /// <summary>True if agent 1 should enter STUMBLING state.</summary>
    public bool TriggerStumble1;
    
    /// <summary>Duration agent 1 stays grounded (seconds). Valid only if TriggerGrounded1.</summary>
    public float GroundedDuration1;
    
    // ================================================================
    // AGENT 2 RESPONSE
    // ================================================================
    
    /// <summary>Velocity change for agent 2 (m/s).</summary>
    public Vector3 VelocityImpulse2;
    
    /// <summary>Position correction for agent 2 (meters).</summary>
    public Vector3 PositionCorrection2;
    
    /// <summary>True if agent 2 should enter GROUNDED state.</summary>
    public bool TriggerGrounded2;
    
    /// <summary>True if agent 2 should enter STUMBLING state.</summary>
    public bool TriggerStumble2;
    
    /// <summary>Duration agent 2 stays grounded (seconds). Valid only if TriggerGrounded2.</summary>
    public float GroundedDuration2;
    
    // ================================================================
    // SHARED DATA
    // ================================================================
    
    /// <summary>Impact force in Newtons. Used for foul detection.</summary>
    public float ImpactForce;
}
```

### 3.3.4 Agent-Ball Collision Data

```csharp
/// <summary>
/// Data passed to Ball Physics when agent-ball collision is detected.
/// 
/// Ball Physics receives this via OnCollision() callback and handles:
///   - Ball deflection based on body part coefficients
///   - Spin transfer based on contact geometry
///   
/// First Touch Mechanics (Spec #11) receives this via separate callback and handles:
///   - Possession determination
///   - First touch quality based on attributes
/// </summary>
public struct AgentBallCollisionData
{
    /// <summary>
    /// Contact point in world coordinates (meters).
    /// Where the agent's hitbox touched the ball.
    /// </summary>
    public Vector3 ContactPoint;
    
    /// <summary>
    /// Agent velocity at moment of contact (m/s).
    /// Used by Ball Physics for momentum transfer calculation.
    /// </summary>
    public Vector3 AgentVelocity;
    
    /// <summary>
    /// Body part that contacted the ball.
    /// Stage 0: Always TORSO (simplification).
    /// Stage 1+: FOOT, SHIN, THIGH, TORSO, HEAD based on ball height and agent state.
    /// </summary>
    public BodyPart BodyPart;
    
    /// <summary>
    /// ID of agent that contacted the ball (0â€“21).
    /// </summary>
    public int AgentID;
    
    /// <summary>
    /// Team of agent that contacted the ball.
    /// Used for possession tracking.
    /// </summary>
    public int TeamID;
    
    /// <summary>
    /// True if contacting agent is a goalkeeper.
    /// Affects Ball Physics deflection behavior (goalkeeper can catch/parry).
    /// </summary>
    public bool IsGoalkeeper;
}
```

### 3.3.5 Foul Detection Data

```csharp
/// <summary>
/// Contact force data packaged for the Referee System.
/// 
/// Stage 0: Collision System populates this struct and includes it in CollisionEvent.
/// Stage 1+: Referee System consumes this to adjudicate fouls.
/// 
/// Design note: This is a data contract only. The Collision System does NOT
/// determine whether a foul occurred â€” it only provides the raw data.
/// </summary>
public struct ContactForceData
{
    /// <summary>Force magnitude in Newtons.</summary>
    public float ForceMagnitude;
    
    /// <summary>Normalized force direction (from instigator toward victim).</summary>
    public Vector3 ForceDirection;
    
    /// <summary>Type of contact for foul classification.</summary>
    public ContactType Type;
    
    /// <summary>Agent who initiated the contact (moved toward victim).</summary>
    public int InstigatorAgentID;
    
    /// <summary>Agent who received the contact.</summary>
    public int VictimAgentID;
    
    /// <summary>True if victim was in possession of the ball at contact time.</summary>
    public bool VictimHasBall;
    
    /// <summary>True if instigator was attempting to play the ball.</summary>
    public bool InstigatorPlayingBall;
}

/// <summary>
/// Classification of physical contact for foul determination.
/// Referee System uses this to apply different foul thresholds.
/// </summary>
public enum ContactType
{
    /// <summary>
    /// Shoulder-to-shoulder contact.
    /// Generally legal if force is reasonable and ball is within playing distance.
    /// </summary>
    SHOULDER_TO_SHOULDER,
    
    /// <summary>
    /// Contact from behind the victim.
    /// Higher likelihood of being ruled a foul.
    /// </summary>
    FROM_BEHIND,
    
    /// <summary>
    /// Contact from the side.
    /// Context-dependent â€” may be legal charge or foul.
    /// </summary>
    SIDE_IMPACT,
    
    /// <summary>
    /// Slide tackle contact (Stage 1+).
    /// Requires animation state to detect.
    /// </summary>
    SLIDE_TACKLE,
    
    /// <summary>
    /// Aerial challenge contact (Stage 1+).
    /// Requires jump state to detect.
    /// </summary>
    AERIAL_CHALLENGE
}
```

### 3.3.6 Determining Contact Type

```csharp
/// <summary>
/// Determines the type of contact for foul classification.
/// Uses relative positions and velocities of the two agents.
/// </summary>
public static class ContactTypeClassifier
{
    /// <summary>
    /// Angle threshold for "from behind" classification.
    /// Contact is "from behind" if instigator approaches from >120Â° behind victim's facing direction.
    /// </summary>
    private const float FROM_BEHIND_ANGLE_THRESHOLD = 120f; // degrees
    
    /// <summary>
    /// Angle threshold for "shoulder to shoulder" classification.
    /// Contact is "shoulder to shoulder" if angle between facing directions is <45Â°
    /// and relative position is roughly perpendicular to both.
    /// </summary>
    private const float SHOULDER_ANGLE_THRESHOLD = 45f; // degrees
    
    /// <summary>
    /// Classifies the type of contact between two agents.
    /// 
    /// Stage 0 implementation uses position-based heuristics.
    /// Stage 1+ will incorporate facing direction and animation state.
    /// </summary>
    /// <param name="instigator">Agent who initiated contact (higher approach velocity)</param>
    /// <param name="victim">Agent who received contact</param>
    /// <param name="collisionNormal">Direction from instigator toward victim</param>
    /// <returns>Contact type classification</returns>
    public static ContactType Classify(
        in AgentPhysicalProperties instigator,
        in AgentPhysicalProperties victim,
        Vector2 collisionNormal)
    {
        // Stage 0 simplified classification based on collision geometry
        // Full classification requires facing direction (available in Agent class but
        // not exposed in AgentPhysicalProperties â€” design decision for Stage 0)
        
        // Calculate approach angle
        Vector2 instigatorVel = new Vector2(instigator.Velocity.x, instigator.Velocity.y);
        float instigatorSpeed = instigatorVel.magnitude;
        
        if (instigatorSpeed < 0.1f)
        {
            // Stationary contact â€” classify as side impact
            return ContactType.SIDE_IMPACT;
        }
        
        // Normalize instigator velocity
        Vector2 approachDir = instigatorVel / instigatorSpeed;
        
        // Angle between approach direction and collision normal
        float approachAngle = Vector2.Dot(approachDir, collisionNormal);
        
        // If approaching nearly head-on (dot product close to 1)
        // and similar speeds, likely shoulder-to-shoulder
        Vector2 victimVel = new Vector2(victim.Velocity.x, victim.Velocity.y);
        float victimSpeed = victimVel.magnitude;
        
        if (victimSpeed > 0.1f)
        {
            Vector2 victimDir = victimVel / victimSpeed;
            float facingDot = Vector2.Dot(approachDir, victimDir);
            
            // Both moving in similar direction (parallel), contact from side
            if (facingDot > 0.7f)
            {
                return ContactType.SHOULDER_TO_SHOULDER;
            }
        }
        
        // If approaching from opposite side of victim's movement, likely from behind
        // This is a simplification â€” proper implementation needs facing direction
        if (approachAngle > 0.5f && victimSpeed > 1.0f)
        {
            Vector2 victimDir = victimVel / victimSpeed;
            float behindDot = Vector2.Dot(-collisionNormal, victimDir);
            
            if (behindDot > 0.5f)
            {
                return ContactType.FROM_BEHIND;
            }
        }
        
        // Default to side impact
        return ContactType.SIDE_IMPACT;
    }
    
    /// <summary>
    /// Determines which agent is the "instigator" (initiated contact).
    /// The instigator is the agent with higher approach velocity toward the other.
    /// </summary>
    public static void DetermineInstigatorAndVictim(
        in AgentPhysicalProperties a1,
        in AgentPhysicalProperties a2,
        Vector2 collisionNormal,
        out int instigatorIndex,
        out int victimIndex)
    {
        // Calculate approach velocities along collision normal
        Vector2 v1_2d = new Vector2(a1.Velocity.x, a1.Velocity.y);
        Vector2 v2_2d = new Vector2(a2.Velocity.x, a2.Velocity.y);
        
        // Velocity component toward the other agent
        float v1Approach = Vector2.Dot(v1_2d, collisionNormal);
        float v2Approach = Vector2.Dot(v2_2d, -collisionNormal);
        
        if (v1Approach > v2Approach)
        {
            instigatorIndex = 0;
            victimIndex = 1;
        }
        else
        {
            instigatorIndex = 1;
            victimIndex = 0;
        }
    }
}
```

---

