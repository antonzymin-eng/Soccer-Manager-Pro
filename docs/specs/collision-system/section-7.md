# Collision System Specification â€” Section 7: Future Extensions

**Purpose:** Authoritative roadmap for all planned collision system extensions â€” what changes at each stage, what architectural hooks exist today, what risks each extension introduces, and what is permanently excluded. This section is the single source of truth for collision system evolution.

**Created:** February 16, 2026, 12:30 PM PST  
**Updated:** February 16, 2026, 1:15 PM PST  
**Version:** 1.1  
**Status:** Draft  
**Author:** Claude (AI) with Anton (Lead Developer)  
**Dependencies:** Section 1 (Purpose & Scope v1.0), Section 2 (System Overview v1.0), Section 3 (Core Systems v1.0), Section 4 (Data Structures v1.0), Section 5 (Testing v1.0), Section 6 (Performance Analysis v1.1), Agent Movement Spec #2 Section 6 (Future Extensions), Ball Physics Spec #1 Section 7 (Future Extensions), Master Development Plan v1.0

---

## CHANGELOG v1.0 â†’ v1.1

**Issue #1 (Acknowledged): Section length exceeds outline estimate**

Outline estimated ~2 pages; delivered ~10 pages. Length justified by:
- Agent Movement Section 6 (comparable section) is ~15 pages
- Detailed risk analysis adds implementation value
- Extension complexity warrants thorough documentation

No change made. Length accepted as appropriate for specification quality.

**Issue #2 (Major): Slide tackle moved to Stage 2**

v1.0 placed slide tackle under Stage 1. Analysis shows slide tackle complexity (compound hitbox, timing-sensitive foul detection, animation synchronization) exceeds Stage 1 scope.

- Moved Section 7.1.4 â†’ Section 7.2.1
- Renumbered Stage 2+ sections
- Updated Stage/Year mapping table in Preamble

**Issue #3 (Moderate): Added animation-driven hitbox documentation**

v1.0 mentioned animation-driven hitbox without explaining data flow.

- Added Section 7.2.1.1 "Animation â†’ Collision Data Flow"
- Documents how animation system provides hitbox geometry
- Establishes data contract for Stage 2 implementation

**Issue #4 (Moderate): Added constraint solver reference**

v1.0 mentioned Sequential Impulse solver without reference.

- Added Ericson and Catto citations to Section 7.2.2
- Clarified that full algorithm is deferred to implementation document

**Issue #5 (Minor): Added soft-body performance estimate**

v1.0 rejected soft-body without quantitative justification.

- Added operation count estimate to Section 7.3.1
- Shows soft-body would consume 2â€“4ms, exceeding total physics budget

**Issue #6 (Minor): CollisionEvent version field concern added**

v1.0 guaranteed backward compatibility but CollisionEvent lacks version field.

- Added KR-8 documenting concern
- Recommends adding EventVersion field for forward compatibility

---

## Preamble: Role of This Section

This section is the **single authoritative source** for all planned and excluded collision system extensions. Future extension references in Sections 1.2 (Out of Scope), 3.3.5 (Body Part Detection note), and 4.2.4 (AgentBallCollisionData Stage 1 notes) are summaries derived from the analysis herein. If a conflict exists between those sections and this one, **this section takes precedence**.

**Design philosophy:** Stage 0 collision detection was designed with future extensions in mind. Specific architectural decisions â€” the `CollisionType` enum with reserved values, the `ContactType` enum extensibility, the `BodyPart` integration placeholder, the `ContactForceData` struct for future referee consumption, and the configurable cell size constant â€” were made to minimize rework when extensions are implemented. This section documents how each extension connects to existing hooks and identifies gaps where new architecture is required.

**Comparison to Agent Movement and Ball Physics:**

| Spec | Extension Complexity | Primary Extension Categories |
|------|---------------------|------------------------------|
| Ball Physics | Low | Surface types, weather, Fixed64 |
| Agent Movement | High | Animation, dribbling, LOD, psychology |
| **Collision System** | **Medium** | Aerial collision, body parts, constraint solver |

Collision System sits between Ball Physics (single entity, few extensions) and Agent Movement (22 entities, many modifier chains). Extensions primarily add new collision *types* rather than new modifier pipelines.

**Stage/Year Mapping (per Master Development Plan v1.0):**

| Stage | Years | Focus | Collision System Extensions |
|-------|-------|-------|----------------------------|
| Stage 0 | Year 1 | Physics Foundation | Core system (this spec) |
| Stage 1 | Year 2 | Tactical Demo | Aerial collision, body parts, goalkeeper |
| Stage 2 | Year 3â€“4 | V1 Release | Slide tackle, constraint solver |
| Stage 3 | Year 5â€“6 | Management Depth | Injury integration hooks |
| Stage 4 | Year 7â€“8 | Human Systems | None (psychology does not affect collision physics) |
| Stage 5 | Year 9â€“10 | Global Simulation | Fixed64 migration, CCD |
| Stage 6 | Year 11+ | Multiplayer | Determinism validation |

---

## 7.1 Stage 1 Extensions (Year 2)

Stage 1 adds aerial collision, height-based body part detection, and goalkeeper-specific collision handling. These extensions share a common characteristic: they enhance existing collision types without requiring fundamental architecture changes.

### 7.1.1 Aerial Collision Detection

**What it does:** Enables collision detection for agents in the air (jumping for headers, aerial duels). Stage 0 only handles ground-level (Z â‰ˆ 0) collision.

**Existing hooks:**
- `CollisionManifold.ContactPoint` already uses Vector3 (Z coordinate available)
- `CollisionType.AERIAL_DUEL` enum value reserved (Section 4.2.2)
- `ContactType.AERIAL_CHALLENGE` enum value reserved (Section 4.2.5)
- `CollisionEvent.ContactPoint.Z` field ready for non-zero values

**What changes:**

1. **Height-aware detection:** `CheckAgentAgentCollision()` (Section 3.2.1) must be extended to consider Z-axis:
   ```csharp
   // Stage 0: 2D check (XY plane only)
   float distanceSqXY = dx*dx + dy*dy;
   
   // Stage 1: 3D check when either agent is airborne
   if (agent1.IsAirborne || agent2.IsAirborne)
   {
       float dz = agent2.Position.z - agent1.Position.z;
       float distanceSq3D = dx*dx + dy*dy + dz*dz;
       // Use 3D distance for collision test
   }
   ```

2. **Jump state integration:** Agent Movement (Spec #2) must provide `IsAirborne` flag and `AltitudeZ` value. Collision System consumes these via extended `AgentPhysicalProperties`.

3. **Aerial collision response:** Different physics model â€” reduced friction, different fall probability, heading outcome determination.

**New data required from Agent Movement:**

| Field | Type | Description |
|-------|------|-------------|
| `IsAirborne` | bool | True if agent is jumping |
| `AltitudeZ` | float | Height above ground (meters) |
| `JumpPhase` | enum | ASCENDING, APEX, DESCENDING |
| `JumpStartTime` | float | When jump began (for timing) |

**Integration with Heading Mechanics (Spec #10):**

Heading Mechanics (Spec #10, Stage 0) defines aerial duel *outcomes* (who wins the header). Collision System detects the collision and provides contact data. The boundary is:
- **Collision System:** Detects two airborne agents within contact radius, computes collision manifold
- **Heading Mechanics:** Determines header winner based on timing, positioning, Heading attribute; controls ball deflection

This split prevents circular dependencies: Collision System does not need to understand heading; Heading Mechanics does not need to understand impulse physics.

**Performance impact:**
- Additional Z-axis calculation: +2 ops per agent pair (~10% narrow phase increase)
- Aerial agents are rare (typically 0â€“2 per frame): negligible total impact
- Estimated: +0.01ms typical, +0.03ms worst case (corner kick with multiple jumpers)

**Risk assessment:** Medium. The 3D extension is straightforward mathematically but requires careful integration with Agent Movement's jump system (not yet specified). **Mitigation:** Define Agent Movement jump interface before implementing aerial collision.

### 7.1.2 Body Part Detection

**What it does:** Determines which body part contacted the ball (FOOT, SHIN, TORSO, HEAD) based on contact point height and agent pose.

**Existing hooks:**
- `AgentBallCollisionData.BodyPart` field exists (Section 4.2.4), currently always TORSO
- `BodyPart` enum imported from Ball Physics Spec #1, Section 3.1.10.1
- Ball Physics already handles different coefficients per body part

**What changes:**

1. **Height-based detection algorithm:**
   ```csharp
   /// <summary>
   /// Determine body part from ball contact height relative to agent.
   /// 
   /// Height thresholds (âš ï¸ PLACEHOLDER â€” tune during Stage 1):
   ///   FOOT:  z < 0.30m (ankle height)
   ///   SHIN:  0.30m â‰¤ z < 0.55m (knee height)
   ///   TORSO: 0.55m â‰¤ z < 1.40m (shoulder height)
   ///   HEAD:  z â‰¥ 1.40m
   /// 
   /// Agent height variation (1.65mâ€“1.95m) affects thresholds.
   /// Thresholds scale with agent height: threshold_actual = threshold_base Ã— (agent_height / 1.80m)
   /// </summary>
   public BodyPart DetermineBodyPart(float contactZ, float agentHeight)
   ```

2. **Animation pose integration (Stage 2+):** When animation system is active, body part detection can use actual limb positions from animation skeleton rather than height heuristics.

**Data flow:**
```
Collision Detection
    â†“ (contact point)
Body Part Determination
    â†“ (BodyPart enum)
AgentBallCollisionData
    â†“ (callback)
Ball Physics.OnCollision()
    â†“ (applies body-part-specific coefficients)
Ball deflection
```

**Performance impact:** Negligible (+3 comparisons per agent-ball collision, typically 0â€“5 per frame).

**Risk assessment:** Low. The algorithm is simple; the main risk is tuning thresholds to feel correct during gameplay.

### 7.1.3 Goalkeeper Collision Handling

**What it does:** Enables special collision handling for goalkeeper-specific actions: diving saves, punching the ball, and collision while holding the ball.

**Existing hooks:**
- `CollisionType.AGENT_GOALKEEPER` enum value reserved (Section 4.2.2)
- `AgentBallCollisionData.IsGoalkeeper` flag exists (Section 4.2.4)
- Stage 0 treats goalkeeper as normal agent with flag set

**What changes:**

1. **Diving collision geometry:** Goalkeeper diving introduces a capsule hitbox (lying body) rather than circular hitbox:
   ```csharp
   // Stage 0: Circle hitbox for all agents
   // Stage 1: Capsule hitbox when goalkeeper is diving
   if (agent.IsGoalkeeper && agent.IsDiving)
   {
       return CheckCapsuleCircleCollision(goalkeeperCapsule, otherAgentCircle);
   }
   ```

2. **Ball catch/hold state:** When goalkeeper is holding the ball, agent-ball collision is suppressed (ball is "inside" goalkeeper's control).

3. **Protected goalkeeper rule:** Collision response differs when goalkeeper is in "protected" state (holding ball, on ground recovering).

**Integration with Goalkeeper Mechanics (Spec #11):**

Goalkeeper Mechanics (Spec #11, Stage 0) defines goalkeeper *behavior* (when to dive, when to catch). Collision System handles the *physics* of those actions. The boundary is:
- **Goalkeeper Mechanics:** Decides action (dive left, punch, catch attempt); provides pose/state
- **Collision System:** Detects collision using appropriate geometry; applies response with goalkeeper-specific rules

**New data required from Goalkeeper Mechanics:**

| Field | Type | Description |
|-------|------|-------------|
| `GoalkeeperState` | enum | STANDING, DIVING, RECOVERING, HOLDING_BALL |
| `DiveCapsule` | CapsuleGeometry | Capsule hitbox when diving |
| `IsProtected` | bool | True if goalkeeper cannot be challenged |

**Performance impact:**
- Capsule-circle intersection: +10 ops vs. circle-circle
- Only 2 goalkeepers per match: negligible impact
- Estimated: +0.005ms when goalkeeper is diving

**Risk assessment:** Medium. Capsule geometry is more complex than circle; diving state must be synchronized between Goalkeeper Mechanics and Collision System. **Mitigation:** Define clear state machine interface in Spec #10 before implementing here.

---

## 7.2 Stage 2 Extensions (Years 3â€“4)

Stage 2 adds slide tackle collision and the constraint solver foundation. These extensions require new collision geometry (capsules, compound hitboxes) and more sophisticated response algorithms.

### 7.2.1 Slide Tackle Collision

**What it does:** Enables collision detection for slide tackle actions, which use a different hitbox geometry (low, extended capsule representing sliding body and extended leg).

**Why Stage 2 (not Stage 1):**
- Requires compound hitbox (body + leg) â€” more complex than goalkeeper's single capsule
- Timing-sensitive foul detection requires sub-frame analysis or CCD approximation
- Heavy animation system dependency for hitbox geometry
- Lower priority than aerial collision and goalkeeper for Tactical Demo milestone

**Existing hooks:**
- `ContactType.SLIDE_TACKLE` enum value reserved (Section 4.2.5)
- `ContactForceData` struct captures foul-relevant data

**What changes:**

1. **Slide tackle hitbox:** Player performing slide tackle has:
   - Main body: Capsule (lying torso)
   - Extended leg: Line segment or thin capsule (the tackling leg)

2. **Foul sensitivity:** Slide tackles have higher foul probability:
   - Contact from behind: Almost always a foul
   - Contact with legs before ball: Foul
   - Contact with ball first, then player: Usually legal

3. **Timing-based outcome:** Whether ball or player was contacted first affects legality. Requires sub-frame collision ordering or simultaneous detection with "first contact" heuristics.

**New data required from Agent Movement / Tackle Mechanics:**

| Field | Type | Description |
|-------|------|-------------|
| `IsSlideTackling` | bool | True during slide tackle animation |
| `SlideTacklePhase` | enum | INITIATION, CONTACT, RECOVERY |
| `TackleBodyCapsule` | CapsuleGeometry | Torso hitbox during slide |
| `ExtendedLegCapsule` | CapsuleGeometry | Tackling leg hitbox |
| `SlideDirection` | Vector2 | Direction of slide motion |

#### 7.2.1.1 Animation â†’ Collision Data Flow

Slide tackle hitboxes are **animation-driven** â€” their geometry comes from the animation system, not from fixed shapes. This requires a new data contract:

```csharp
/// <summary>
/// Animation-driven collision geometry for dynamic hitboxes.
/// 
/// Populated by Animation System each frame for agents in special poses
/// (slide tackle, diving goalkeeper, etc.).
/// 
/// Consumed by Collision System to replace default circle hitbox.
/// 
/// Stage 0: Not used (all agents use circle hitbox)
/// Stage 2: Active for slide tackle, diving goalkeeper
/// </summary>
public struct AnimationCollisionGeometry
{
    /// <summary>
    /// Version for forward compatibility.
    /// Increment when adding fields.
    /// </summary>
    public int GeometryVersion;
    
    /// <summary>
    /// Type of geometry provided.
    /// </summary>
    public AnimationGeometryType Type;
    
    /// <summary>
    /// Primary collision shape (e.g., body capsule).
    /// Coordinates are world-space.
    /// </summary>
    public CapsuleGeometry PrimaryShape;
    
    /// <summary>
    /// Optional secondary shape (e.g., extended leg).
    /// Valid only if HasSecondaryShape is true.
    /// </summary>
    public CapsuleGeometry SecondaryShape;
    public bool HasSecondaryShape;
    
    /// <summary>
    /// Frame timestamp for synchronization verification.
    /// Must match current physics frame.
    /// </summary>
    public int FrameNumber;
}

public enum AnimationGeometryType
{
    NONE = 0,              // Use default circle hitbox
    SINGLE_CAPSULE = 1,    // Diving goalkeeper
    COMPOUND_CAPSULE = 2   // Slide tackle (body + leg)
}
```

**Data flow:**
```
Animation System
    â†“ (evaluates skeleton pose)
AnimationCollisionGeometry
    â†“ (published to shared buffer)
Collision System
    â†“ (reads geometry for special-state agents)
Appropriate intersection test
```

**Synchronization requirement:** Animation System must publish geometry BEFORE Collision System reads it. Frame pipeline order:
1. Agent Movement updates positions/states
2. Animation System evaluates poses, publishes `AnimationCollisionGeometry`
3. Collision System reads geometry, performs detection/response

**Performance impact:**
- Line segment vs. circle intersection: +5 ops per leg check
- Capsule vs. circle: +10 ops per body check
- Slide tackles are infrequent (0â€“3 per minute): +0.01ms per tackle frame
- AnimationCollisionGeometry read: +4 ops per special-state agent

**Risk assessment:** Medium-High. Slide tackle is the most complex collision extension due to:
1. Compound hitbox (body + leg)
2. Timing-sensitive foul detection
3. Animation synchronization requirements
4. New data contract between Animation and Collision systems

**Mitigation:** 
- Prototype slide tackle detection in isolation before full integration
- Consider simplification for Stage 2: treat entire sliding player as single elongated capsule rather than body + leg compound shape
- Full compound shape can be Stage 3 refinement if simplified version proves sufficient

### 7.2.2 Constraint Solver Foundation

**What it does:** Handles multi-body collisions (pile-ups) where more than two entities are in contact simultaneously. Stage 0â€“1 processes collisions pairwise, which can produce unstable results when 3+ agents collide.

**Problem statement:**

Pairwise collision resolution fails for pile-ups:
```
Agent A pushes Agent B â†’ B pushed right
Agent C pushes Agent B â†’ B pushed left
Result: B oscillates or jitters
```

A constraint solver processes all contacts simultaneously, finding a stable resolution.

**Existing hooks:**
- `MAX_COLLISION_PAIRS_PER_FRAME = 50` anticipates multi-collision frames (Section 3.1.1)
- `CollisionManifold` struct is independent of solver algorithm
- Response calculation is isolated in `CollisionResponse.cs`

**What changes:**

1. **Replace pairwise response with iterative solver:**
   ```csharp
   // Stage 0â€“1: Pairwise
   foreach (var collision in collisions)
   {
       ApplyResponse(collision);
   }
   
   // Stage 2: Iterative constraint solver
   for (int iteration = 0; iteration < MAX_SOLVER_ITERATIONS; iteration++)
   {
       foreach (var collision in collisions)
       {
           AccumulateConstraint(collision);
       }
       if (ConvergenceReached()) break;
   }
   ApplyAccumulatedConstraints();
   ```

2. **Contact caching:** Solver benefits from knowing which contacts persist frame-to-frame (warm starting). Requires tracking contact IDs across frames.

**Algorithm reference:**

The recommended algorithm is **Sequential Impulse (SI)**, also known as Projected Gauss-Seidel (PGS). This is the same algorithm used by Box2D and Bullet physics engines.

**References:**
- Catto, E. (GDC 2006). "Fast and Simple Physics using Sequential Impulses." â€” Original SI presentation
- Catto, E. (GDC 2014). "Understanding Constraints." â€” Detailed derivation and implementation guidance
- Ericson, C. (2005). "Real-Time Collision Detection," Chapter 13. â€” Constraint solver theory

Full algorithm specification will be documented in Stage 2 implementation document, not this futures section. This section establishes *what* will be implemented; implementation document specifies *how*.

**Performance impact:**
- Iterative solver: 4â€“8 iterations typical, 20 max
- Per-iteration cost: ~50 ops per contact
- Pile-up scenario (8 agents, 28 contacts): 28 Ã— 50 Ã— 8 = 11,200 ops â‰ˆ 0.15ms
- Normal scenario (2â€“3 contacts): Negligible overhead vs. pairwise

**Risk assessment:** Medium. The iterative solver is well-understood (Box2D, Bullet physics). Risk is mainly implementation complexity and tuning iteration count.

---

## 7.3 Stage 3+ Extensions (Years 5â€“10)

### 7.3.1 Injury System Hooks (Stage 3, Years 5â€“6)

**What it does:** Provides collision force data to the Injury System for determining whether a collision caused injury.

**Existing hooks:**
- `CollisionEvent.ImpactForce` already computed (Section 4.2.3)
- `ContactForceData.ForceMagnitude` captures force (Section 4.2.5)
- `ContactForceData.ForceDirection` captures direction

**What changes:**

1. **Injury probability interface:** Collision System publishes force data; Injury System (Master Vol 2) consumes it and calculates injury probability.

2. **Body part injury mapping:** Different body parts have different injury susceptibility:
   - Head collision â†’ concussion risk
   - Leg collision â†’ muscle/ligament risk
   - Torso collision â†’ rib/internal risk

3. **No physics changes:** Collision System only provides data; injury determination is owned by Human Systems (Master Vol 2).

**New data published:**

| Field | Destination | Description |
|-------|-------------|-------------|
| `ImpactForce` | Injury System | Already in CollisionEvent |
| `BodyPartImpacted` | Injury System | From body part detection (Stage 1) |
| `ImpactVelocity` | Injury System | Relative velocity at impact |

**Performance impact:** Zero â€” data is already computed, just needs to be routed.

**Risk assessment:** Low. This is a data interface extension, not a physics change.

### 7.3.2 Continuous Collision Detection (Stage 5, Years 9â€“10)

**What it does:** Prevents tunneling â€” fast-moving entities passing through each other between frames. Stage 0â€“4 uses discrete collision detection, which works for typical agent speeds but fails for extremely fast objects.

**When it matters:**
- Ball moving at 40+ m/s (powerful shot): travels 0.67m per frame at 60Hz
- Ball radius = 0.11m â†’ can pass through thin obstacles
- Agent radius = 0.35â€“0.50m â†’ less susceptible but not immune

**Existing hooks:**
- None. CCD requires architectural changes.

**What changes:**

1. **Swept sphere/circle tests:** Instead of checking overlap at frame end, check if paths intersected:
   ```csharp
   // Discrete: Did A and B overlap at t=1?
   bool collided = Distance(A.Position, B.Position) < A.Radius + B.Radius;
   
   // Continuous: Did A and B's paths intersect during t=[0,1]?
   float t_collision = SweptCircleIntersection(
       A.Position, A.Velocity, A.Radius,
       B.Position, B.Velocity, B.Radius
   );
   bool collided = t_collision >= 0 && t_collision <= 1;
   ```

2. **Time of impact (TOI) sorting:** When multiple CCD collisions detected, process in chronological order.

3. **Substep resolution:** Advance simulation to TOI, resolve collision, continue to frame end.

**Performance impact:**
- Swept sphere test: +20 ops vs. discrete (square root of quadratic)
- Only needed for high-velocity entities (ball after shot, rarely agents)
- Estimated: +0.05ms per frame when CCD active

**Risk assessment:** Low-Medium. CCD is standard technique; complexity is mainly in substep logic and TOI ordering. Deferring to Stage 5 allows learning from Ball Physics CCD implementation (if implemented there first).

### 7.3.3 Fixed64 Migration (Stage 5, Years 9â€“10)

**What it does:** Replaces float32 arithmetic with fixed-point (64-bit) for deterministic cross-platform results. Required for competitive multiplayer with replay verification.

**Existing hooks:**
- All physics calculations use float â†’ direct replacement path
- No platform-specific code in collision algorithms
- `DeterministicRNG` (Section 4.7) already ensures deterministic randomness

**What changes:**

1. **Type replacement:** `float` â†’ `Fixed64`, `Vector2` â†’ `Vector2Fixed`, `Vector3` â†’ `Vector3Fixed`

2. **Transcendental approximations:** `sqrt()` replaced with fixed-point approximation (same approach as Ball Physics Spec #1, Section 7.4.1)

3. **Tolerance adjustments:** Position tolerance (0.001m) and velocity tolerance (0.01 m/s) may need adjustment for fixed-point precision characteristics.

**Performance impact:**
- Fixed64 ops are 2â€“4Ã— slower than float on x86-64
- Collision System is already lightweight (~0.18ms typical)
- Estimated post-migration: ~0.45ms typical (still well under 1ms budget)
- SIMD optimizations (Section 6.4.3) can offset much of the overhead

**Risk assessment:** Low. Collision System uses simple arithmetic (add, subtract, multiply, one sqrt). Ball Physics Fixed64 migration (if completed first) will establish patterns and libraries.

**Prerequisite:** Complete Ball Physics Fixed64 migration first. Share `Fixed64Math` library between specs.

---

## 7.4 Permanently Excluded Features

The following features will **never** be implemented in the Collision System. They are either owned by other specifications or outside project scope.

| Feature | Reason for Exclusion | Owner |
|---------|---------------------|-------|
| Ball deflection physics | Ball Physics owns all ball trajectory calculations | Ball Physics Spec #1 |
| Agent locomotion | Agent Movement owns all movement before/after collision | Agent Movement Spec #2 |
| Foul adjudication | Referee System makes foul decisions using collision data | Referee System (Stage 1+) |
| Possession transfer | First Touch Mechanics decides when ball changes possession | First Touch Spec #11 |
| Tactical intent | AI brain decides what agents are trying to do | Decision Tree Spec #8 |
| Soft-body deformation | Complexity outweighs benefit (see 7.4.1) | N/A |
| Ragdoll physics | Falls use state machine, not physics simulation | Agent Movement Spec #2 |
| Clothing/hair collision | Outside project scope â€” visual-only, no gameplay impact | N/A |

### 7.4.1 Soft-Body Approximation (Rejected)

The outline (Section 7.2) listed "soft-body approximation for realistic deformation" as a potential Stage 2+ extension. After analysis, this is **rejected** as permanently excluded.

**Rationale:**

1. **No gameplay benefit:** Player deformation during collision is purely visual; it does not affect match outcome.

2. **Prohibitive performance cost:** Soft-body simulation typically requires:
   - 50â€“100 vertices per agent mesh
   - 5â€“10 constraint iterations per frame
   - 22 agents active
   - **Estimated cost:** 50 vertices Ã— 10 iterations Ã— 22 agents Ã— ~4 ops/constraint = 44,000 ops/frame â‰ˆ **2â€“4ms**
   - This would consume 50â€“100% of the total physics budget (4ms target per Master Vol 4)

3. **Animation handles it better:** Modern animation systems (blend shapes, IK adjustment) achieve visual deformation more efficiently than physics simulation.

4. **Industry precedent:** No football simulation game uses physics-based soft-body for player deformation. Even FIFA/EA FC uses animation blending.

**Alternative:** Animation system can apply procedural deformation based on `CollisionEvent.ImpactForce` and `ContactPoint` â€” visual approximation without physics simulation. This is outside Collision System scope.

---

## 7.5 Extensibility Design Analysis

### 7.5.1 Existing Extension Hooks

| Hook | Location | Purpose | Stage |
|------|----------|---------|-------|
| `CollisionType` enum | Section 4.2.2 | Add new collision categories | 1+ |
| `ContactType` enum | Section 4.2.5 | Add new contact classifications | 1+ |
| `BodyPart` integration | Section 4.2.4 | Height-based body part detection | 1 |
| `ContactForceData` struct | Section 4.2.5 | Foul detection interface | 1+ |
| `CollisionEvent.ContactPoint.Z` | Section 4.2.3 | Aerial collision height | 1 |
| `CELL_SIZE` constant | Section 3.1.1 | Adjustable grid resolution | 2+ |
| `CollisionResponseData.GroundedDuration` | Section 4.2.2 | Injury severity modulation | 3 |
| `DeterministicRNG` | Section 4.7 | Seeded randomness for multiplayer | 5 |

### 7.5.2 Architecture Gaps

| Gap | Affects | Resolution Required |
|-----|---------|---------------------|
| **No jump state input** | Aerial collision (7.1.1) | Agent Movement must expose `IsAirborne`, `AltitudeZ` |
| **No capsule geometry** | Goalkeeper diving (7.1.3), slide tackle (7.2.1) | Add `CapsuleGeometry` struct, `CheckCapsuleCircle()` function |
| **No animation-collision interface** | Slide tackle (7.2.1) | Add `AnimationCollisionGeometry` struct (defined in 7.2.1.1) |
| **No contact caching** | Constraint solver (7.2.2) | Add `ContactCache` for warm starting |
| **No swept collision** | CCD (7.3.2) | Add `SweptCircleIntersection()` function |
| **Float-only types** | Fixed64 (7.3.3) | Replace types after Fixed64Math library complete |
| **No CollisionEvent version** | Forward compatibility | Add `EventVersion` field (see KR-8) |

### 7.5.3 Dependency Map

```
Stage 1 Dependencies:
  Aerial Collision â† Agent Movement (jump state)
  Body Part Detection â† Ball Physics (BodyPart enum) âœ“ already imported
  Goalkeeper Collision â† Goalkeeper Mechanics (dive state)

Stage 2 Dependencies:
  Slide Tackle â† Tackle Mechanics (tackle state)
  Slide Tackle â† Animation System (AnimationCollisionGeometry)
  Constraint Solver â† None (internal refactor)

Stage 3 Dependencies:
  Injury Hooks â† Master Vol 2 (Injury System interface)

Stage 5 Dependencies:
  Fixed64 â† Fixed64Math library (shared with Ball Physics)
  CCD â† None (can reference Ball Physics implementation)
```

---

## 7.6 Backwards Compatibility Guarantees

When implementing extensions, the following invariants must be maintained:

1. **Stage 0 tests must pass unchanged.** All 32+ unit tests from Section 5 must produce identical results. Extensions add new tests; they do not modify expected results for existing tests.

2. **CollisionEvent struct remains backward compatible.** New fields must be added at end of struct. Existing consumers must not break. âš ï¸ See KR-8 regarding version field.

3. **Default behavior matches Stage 0.** When aerial/goalkeeper/slide tackle states are not active, collision behaves exactly as Stage 0 implementation.

4. **Performance budgets are maintained.** Extensions must not cause typical-case frame time to exceed 0.5ms. If budget is exceeded, P1 optimizations (Section 6.4.3) must be implemented.

5. **Determinism is preserved.** Extensions must not introduce platform-dependent behavior. All randomness must flow through `DeterministicRNG`.

6. **Enum values are stable.** `CollisionType` and `ContactType` enum values (0, 1, 2, ...) must not change. New values are appended.

---

## 7.7 Cross-References

| Topic | Section | Summary |
|-------|---------|---------|
| Out of scope items | Section 1.2 | Subset of 7.4 (this section is authoritative) |
| CollisionType enum | Section 4.2.2 | Enum values for extension |
| ContactType enum | Section 4.2.5 | Contact classifications |
| BodyPart integration | Section 4.2.4 | Ball Physics interface |
| ContactForceData | Section 4.2.5 | Foul detection interface |
| DeterministicRNG | Section 4.7 | Seeded randomness |
| Performance budget | Section 6.2 | Frame time targets |
| P1 optimizations | Section 6.4.3 | SIMD, batch insertion |
| Ball Physics Fixed64 | Spec #1, Section 7.4.1 | Parallel migration |
| Agent Movement jump | Spec #2, Section 6 (TBD) | Jump state interface |
| Goalkeeper Mechanics | Spec #10 | Goalkeeper state machine |
| Heading Mechanics | Spec #9 | Aerial duel outcomes |
| First Touch Mechanics | Spec #11 | Possession determination |
| Master Development Plan | v1.0 | Stage timeline |
| SI Solver | Catto (GDC 2014) | Constraint solver algorithm |
| Collision algorithms | Ericson (2005) | CCD, capsule intersection |

---

## 7.8 Known Risks and Open Questions

### KR-1: Aerial Collision Interface Not Yet Defined

Agent Movement Spec #2 does not yet define jump mechanics (deferred to Stage 1 implementation). Collision System's aerial collision depends on this interface.

**Recommendation:** Before Stage 1 implementation, add Section 6.1.x to Agent Movement defining `IsAirborne`, `AltitudeZ`, and `JumpPhase` interface. Collision System cannot implement aerial collision until this is defined.

### KR-2: Capsule Geometry Complexity

Goalkeeper diving and slide tackle both require capsule collision geometry. Two options:
1. **Capsule-circle intersection:** Standard algorithm, ~10 ops
2. **Capsule as two circles + rectangle:** Approximation, ~15 ops but reuses existing code

**Recommendation:** Implement true capsule-circle intersection. The algorithm is well-documented (Ericson, "Real-Time Collision Detection," Chapter 5). Approximation introduces edge cases at capsule endpoints.

### KR-3: Slide Tackle Timing Sensitivity

Slide tackle foul detection requires knowing whether ball or player was contacted first. This is difficult with discrete collision detection at 60Hz.

**Recommendation:** Options to resolve:
1. **Sub-frame interpolation:** Calculate precise contact times within frame (adds complexity)
2. **Simultaneous detection:** If both contacts occur same frame, use heuristics (ball distance, tackle angle)
3. **Defer to CCD:** Continuous collision detection naturally provides contact ordering

Recommend option 2 for Stage 2 (simpler), migrate to option 3 when CCD implemented in Stage 5.

### KR-4: Constraint Solver Iteration Count

Stage 2 constraint solver requires tuning iteration count:
- Too few iterations: Unstable pile-ups (jitter)
- Too many iterations: Performance overhead

**Recommendation:** Start with 8 iterations (Box2D default). Add profiling to track convergence. Expose `MAX_SOLVER_ITERATIONS` as tunable constant for gameplay testing.

### KR-5: Fixed64 Library Ownership

Both Collision System and Ball Physics require Fixed64 migration. Who develops the shared `Fixed64Math` library?

**Recommendation:** Fixed64Math is owned by neither spec individually. Create `Spec #8: Fixed64 Math Library` before Stage 5 implementation. Both physics specs depend on Spec #8.

**Cross-reference:** Ball Physics Spec #1, Section 7.4.1 raises same concern.

### KR-6: Body Part Height Thresholds

Section 7.1.2 specifies placeholder height thresholds (FOOT < 0.30m, etc.). These are untested estimates.

**Recommendation:** During Stage 1 implementation, conduct visual validation:
1. Place ball at various heights relative to stationary agent
2. Verify detected body part matches visual expectation
3. Adjust thresholds based on agent model proportions

### KR-7: Goalkeeper "Protected" State Definition

Section 7.1.3 mentions goalkeeper "protected" state but does not define when protection applies.

**Recommendation:** Goalkeeper Mechanics (Spec #11) must define protected state rules before Collision System can enforce them. Tentative rules:
- Protected while catching ball (cannot be challenged)
- Protected while on ground recovering (limited time, ~2s)
- Not protected while diving (can be challenged)

### KR-8: CollisionEvent Lacks Version Field

Section 7.6 guarantees backward compatibility for `CollisionEvent` struct, but unlike `AnimationDataContract` (which has `ContractVersion`), `CollisionEvent` has no version field.

**Risk:** Future extensions adding fields to CollisionEvent may break existing consumers if they expect specific struct size or field order.

**Recommendation:** Add `EventVersion` field to `CollisionEvent` in Stage 1:
```csharp
public struct CollisionEvent
{
    /// <summary>
    /// Event format version for forward compatibility.
    /// Stage 0: Version 1
    /// Increment when adding fields.
    /// </summary>
    public byte EventVersion;
    
    // ... existing fields ...
}
```

This is a minor breaking change from Stage 0 but prevents larger breaking changes in future stages. Alternatively, accept that CollisionEvent consumers will be updated alongside Collision System (tighter coupling but simpler).

---

**END OF SECTION 7**

---

## Document Status

**Section 7 Completion (v1.1):**
- âœ… Stage 1 extensions documented with existing hooks identified (7.1)
  - âœ… Aerial collision (7.1.1)
  - âœ… Body part detection (7.1.2)
  - âœ… Goalkeeper collision (7.1.3)
- âœ… Stage 2 extensions documented with integration requirements (7.2)
  - âœ… Slide tackle with animation data flow (7.2.1, 7.2.1.1) â€” **moved from Stage 1 in v1.1**
  - âœ… Constraint solver with algorithm reference (7.2.2) â€” **reference added in v1.1**
- âœ… Stage 3+ extensions documented (7.3)
  - âœ… Injury hooks (7.3.1)
  - âœ… Continuous collision detection (7.3.2)
  - âœ… Fixed64 migration (7.3.3)
- âœ… Permanently excluded features with rationale (7.4)
  - âœ… Soft-body rejection with performance estimate (7.4.1) â€” **estimate added in v1.1**
- âœ… Extensibility design analysis â€” hooks, gaps, dependencies (7.5)
- âœ… Backwards compatibility guarantees (7.6)
- âœ… Cross-references to all related sections (7.7)
- âœ… Known risks and open questions documented (7.8)
  - âœ… KR-8 CollisionEvent version field concern â€” **added in v1.1**

**Page Count:** ~12 pages

**Issues Resolved in v1.1:** 6/6 from v1.0 self-critique

**Quality Checks:**
- âœ… No circular dependencies in extension plan
- âœ… All existing hooks verified against Section 4 code
- âœ… Performance impact estimated for each extension
- âœ… Risk assessment provided for each major extension
- âœ… Backwards compatibility requirements explicit
- âœ… Algorithm references provided for constraint solver
- âœ… Animation-collision interface documented

**Ready for:** Review and approval

**Next Section:** Section 8 â€” References
