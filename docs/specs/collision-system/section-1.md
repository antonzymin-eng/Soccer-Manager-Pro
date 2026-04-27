# Collision System Specification â€” Section 1: Purpose & Scope

**Purpose:** Defines the responsibilities, boundaries, and terminology for Specification #3 (Collision System) of Stage 0 Physics Foundation.

**Created:** February 15, 2026, 2:45 PM PST  
**Revised:** March 05, 2026, audit session  
**Version:** 1.1  
**Status:** Draft — Revised  
**Author:** Claude (AI) with Anton (Lead Developer)  
**Specification:** #3 of 20 (Stage 0 Physics Foundation)  
**Preceding Specs:** Ball Physics (#1, approved), Agent Movement (#2, in review)

**Changelog:**
- v1.1 (Mar 5, 2026): Corrected hitbox radius range from "0.35–0.50m" to "0.3525–0.50m"
  in §1.3 Terminology and §1.5.1 upstream dependency table to match Agent Movement
  §3.5.4.3 authoritative formula (Strength 1: 0.35 + 1/20 × 0.15 = 0.3525m). No
  structural changes.
- v1.0 (Feb 15, 2026): Initial draft.

---

## 1.1 What This System Does

The Collision System is the **spatial reasoning layer** for the match simulation. It executes after all entity movement updates complete each frame, detecting overlaps and computing physical responses.

**Core Responsibilities:**

1. **Spatial Partitioning** â€” Efficiently determine which entities are near each other using a grid-based spatial hash. Achieves O(N) query performance for 22 agents + 1 ball, avoiding the O(NÂ²) cost of checking all 231 agent-agent pairs plus 22 agent-ball pairs every frame.

2. **Agent-Agent Collision Detection** â€” Detect when two player hitboxes overlap using circle-circle intersection tests in the XY plane (ground projection). Height (Z-axis) is ignored for Stage 0 ground-based collision.

3. **Agent-Ball Collision Detection** â€” Detect when a player's hitbox overlaps the ball and trigger Ball Physics' `OnCollision()` callback with contact data. The Collision System detects contact; Ball Physics handles deflection physics.

4. **Collision Response Physics** â€” Apply momentum transfer between colliding agents using impulse-based resolution. Compute velocity changes, position separation, and determine whether impacts cause stumble or fall states.

5. **State Change Triggers** â€” Signal Agent Movement System when collision forces exceed thresholds, triggering STUMBLING or GROUNDED state transitions. The Collision System computes the force; Agent Movement handles the state machine transition.

6. **Foul Detection Groundwork** â€” Package contact force data (magnitude, direction, contact type) for consumption by the future Referee System. Stage 0 collects the data; foul adjudication is deferred to Stage 1+.

7. **Event Publishing** â€” Generate `CollisionEvent` records for the Event System (Spec #17), enabling replay reconstruction, statistics tracking, and match analysis.

**Position in Simulation Loop:**

The Collision System executes at step 5 of the 60Hz physics loop, after all movement updates are finalized:

```
Frame N:
  1. Input processing
  2. Tactical AI heartbeat (10Hz, every 6th frame)
  3. Agent Movement update (all 22 agents) â† positions finalized
  4. Ball Physics update                    â† ball position finalized
  5. >>> COLLISION SYSTEM <<<
     5a. Clear spatial hash from previous frame
     5b. Insert all 22 agents + ball into spatial hash
     5c. Broad phase: query collision pairs from spatial hash
     5d. Narrow phase: exact intersection tests
     5e. Collision response: compute and apply impulses
     5f. Trigger state changes (GROUNDED, STUMBLING)
     5g. Package collision events for Event System
  6. Event System processing
  7. Rendering (decoupled, may interpolate)
```

This ordering ensures all entities have committed their frame's movement before collision detection runs. Collision responses modify velocities for the *next* frameâ€”they do not retroactively change current-frame positions.

---

## 1.2 What Is OUT of Scope

### 1.2.1 Responsibilities Owned by Other Specifications

| Responsibility | Owner Specification | Interface |
|----------------|---------------------|-----------|
| Ball trajectory after deflection | Ball Physics (#1) | `Ball.OnCollision()` receives contact data; Ball Physics computes new velocity |
| Agent locomotion and state machine | Agent Movement (#2) | Collision System sends `CollisionResponse`; Agent Movement applies state transitions |
| Ball possession transfer decision | First Touch Mechanics (#11) | Collision System detects contact; First Touch decides if possession changes |
| Goalkeeper diving/punching collision | Goalkeeper Mechanics (#11) | Stage 0 treats goalkeeper as normal agent with `IsGoalkeeper` flag; Spec #11 defines diving/punching special cases |
| Aerial collision (heading duels) | Heading Mechanics (#10) | Spec #10 defines aerial duel logic; Collision System provides ground-only detection in Stage 0 |
| Foul decision and card issuance | Referee System (TBD) | Collision System provides `ContactForceData`; Referee System adjudicates |
| Pitch boundary enforcement | Ball Physics (#1), Agent Movement (#2) | Already implemented in respective specs |

### 1.2.2 Stage 1+ Deferrals

| Feature | Deferral Reason | Target Stage |
|---------|-----------------|--------------|
| Aerial collision detection | Requires jump mechanics not yet specified | Stage 1 |
| Slide tackle collision geometry | Requires animation-driven capsule hitboxes | Stage 1 |
| Body part detection (foot, shin, head) | Stage 0 uses TORSO for all contacts; refinement deferred | Stage 1 |
| Multi-body pile-ups (>2 agents) | Requires iterative constraint solver | Stage 2+ |
| Continuous collision detection | Prevents tunneling at extreme speeds; low priority for football | Stage 2+ |

### 1.2.3 Permanent Exclusions

| Feature | Exclusion Rationale |
|---------|---------------------|
| Soft-body deformation | Visual-only effect with negligible gameplay impact; performance cost not justified |
| Cloth physics for jerseys | Pure visual; no gameplay relevance |
| Joint-level ragdoll during falls | Excessive complexity for Stage 0; simple GROUNDED state sufficient |

---

## 1.3 Terminology

| Term | Definition |
|------|------------|
| **Hitbox** | Circular 2D collision boundary in the XY plane representing an agent's physical footprint. Radius derived from Strength attribute (0.3525–0.50m per Agent Movement §3.5.4.3). |
| **Broad Phase** | First pass of collision detection that quickly identifies *candidate* collision pairs using spatial partitioning. Eliminates obviously non-colliding pairs without expensive math. |
| **Narrow Phase** | Second pass that performs exact geometric intersection tests on candidate pairs from the broad phase. Computes collision manifold if overlap exists. |
| **Collision Pair** | Two entities close enough to require narrow-phase testing. A pair may or may not actually be colliding. |
| **Contact Manifold** | Data structure describing a collision: contact point (where entities touch), collision normal (direction of separation), and penetration depth (overlap distance in meters). |
| **Collision Normal** | Unit vector pointing from entity A toward entity B at the contact point. Used to compute impulse direction. |
| **Penetration Depth** | Distance (meters) that two hitboxes overlap. Used for position correction (separation). |
| **Impulse** | Instantaneous change in momentum applied to resolve a collision. Computed from relative velocity, masses, and coefficient of restitution. Units: kgÂ·m/s. |
| **Coefficient of Restitution (e)** | Ratio of separation speed to approach speed after collision. Range [0, 1]. Value of 0.3 used for agent-agent collisions (highly inelasticâ€”players are soft). |
| **Spatial Hash** | Data structure that divides the pitch into a grid of cells. Each entity is inserted into the cell(s) it occupies. Queries check only nearby cells, achieving O(1) average lookup. |
| **Minimum Translation Vector (MTV)** | Smallest displacement required to separate two overlapping entities. Computed as `normal Ã— penetration_depth`. |
| **Deterministic RNG** | Random number generator seeded from Match seed, ensuring identical collision outcomes across replays and networked clients. Required by Master Vol 1 Â§1.3. |

---

## 1.4 Implementation Timeline

### Stage 0 (Current â€” Weeks 3â€“4 of 20-Week Specification Phase)

**Full Implementation:**
- Grid-based spatial hash (1.0m cell size, 106Ã—69 grid covering 105mÃ—68m pitch)
- Agent-agent circle-circle collision detection (XY plane)
- Agent-ball collision detection with `Ball.OnCollision()` callback
- Impulse-based collision response with momentum conservation
- Stumble/fall determination based on impact force vs. Strength-derived thresholds
- Same-team collision filtering (reduced momentum transfer)
- GROUNDED agent handling (obstacle but no response generation)
- Position separation (penetration resolution)
- Deterministic RNG for fall/stumble probability
- `CollisionEvent` publishing for Event System
- `ContactForceData` packaging for future Referee System
- Comprehensive unit and integration tests (25+ unit, 8+ integration)
- Performance validation (<0.5ms worst case)

### Stage 1 (Year 2)

**Extensions:**
- Aerial collision detection for heading duels (integrates with Spec #10)
- Slide tackle geometry using animation-driven capsule hitboxes
- Body part detection (FOOT, SHIN, THIGH, TORSO, HEAD) for refined `BodyPart` in `AgentBallCollisionData`
- Goalkeeper-specific collision handling (diving saves, punching)
- Referee System integration for foul adjudication

### Stage 2+ (Year 3+)

**Extensions:**
- Iterative constraint solver for multi-body pile-ups (>2 simultaneous contacts)
- Continuous collision detection for edge cases at extreme velocities
- Context-aware collision responses (challenge intensity based on match state)

---

## 1.5 Dependency Summary

### 1.5.1 Upstream Dependencies (Consumes From)

| Source Spec | Interface | Data Consumed | Section Reference |
|-------------|-----------|---------------|-------------------|
| Ball Physics (#1) | `BallState` | Position, Velocity, Radius (0.11m) | Ball Physics Â§3.1.1 |
| Agent Movement (#2) | `AgentPhysicalProperties` | Position, Velocity, Mass (72.5â€“100 kg), HitboxRadius (0.3525–0.50m), Strength (1â€“20), IsGrounded | Agent Movement Â§3.5.4 |
| Agent Movement (#2) | `Agent.TeamID` | For same-team collision filtering | Agent Movement Â§3.5.1 |

### 1.5.2 Downstream Dependencies (Provides To)

| Target Spec | Interface | Data Provided | Section Reference |
|-------------|-----------|---------------|-------------------|
| Ball Physics (#1) | `Ball.OnCollision(AgentBallCollisionData)` | ContactPoint, AgentVelocity, BodyPart, AgentID, TeamID, IsGoalkeeper | Ball Physics Â§3.1.10.1 |
| Agent Movement (#2) | `CollisionResponse` | VelocityImpulse, PositionCorrection, TriggerGrounded, TriggerStumble, GroundedDuration | Agent Movement Â§3.1.2 (state triggers) |
| Event System (#17) | `CollisionEvent` | MatchTime, CollisionType, Entity IDs, ContactPoint, ImpactForce, FoulData | Event System (forward reference) |
| Referee System (TBD) | `ContactForceData` | ForceMagnitude, ForceDirection, ContactType, InstigatorID, VictimID, VictimHasBall, InstigatorPlayingBall | Stage 1+ |

### 1.5.3 Coordinate System

Inherited from Ball Physics Specification Â§3.1.1:

- **Origin (0, 0, 0):** Corner flag at home team's left defensive corner
- **X-axis:** Along pitch length (0 to 105m), goal-to-goal direction
- **Y-axis:** Along pitch width (0 to 68m), touchline-to-touchline
- **Z-axis:** Vertical height (0 = ground level)
- **Units:** All distances in meters, all velocities in m/s, all forces in Newtons

Stage 0 collision detection operates in the XY plane only (2D ground projection). Z-axis is ignored for hitbox intersection; aerial collision is deferred to Stage 1.

### 1.5.4 External Dependencies (Unity)

**Used:**
- `UnityEngine.Vector3` â€” 3D vector math
- `UnityEngine.Vector2` â€” 2D vector math (collision manifold)
- `UnityEngine.Mathf` â€” Clamp, Sqrt, Min, Max
- `UnityEngine.Profiling.Profiler` â€” Performance instrumentation (editor/development builds only)

**NOT Used:**
- Unity Physics engine (Rigidbody, Collider, Physics.Raycast)
- Unity NavMesh
- Unity CharacterController

All collision physics is custom implementation, consistent with Ball Physics and Agent Movement specifications.

---

## 1.6 Key Design Decisions

### 1.6.1 Spatial Hash vs. Alternatives

**Decision:** Use uniform grid spatial hash (1.0m cells).

**Alternatives Considered:**
- **Quadtree:** Better for non-uniform distributions, but 22 agents are roughly uniform across a football pitch. Quadtree's adaptive subdivision adds complexity without benefit.
- **Bounding Volume Hierarchy (BVH):** Excellent for static geometry, but agents move every frame. Rebuilding BVH each frame negates its advantages.
- **Brute Force O(NÂ²):** 22 agents = 231 pairs. Checking all pairs is viable but wasteful. Spatial hash reduces to ~40â€“60 checks typically.

**Rationale:** Uniform grid is simplest to implement, debug, and profile. Cell size of 1.0m (>= maximum combined radius of two agents = 0.50m + 0.50m = 1.00m, per Ericson-2005 heuristic and Appendix A.1.2) ensures any two colliding agents share a cell or are in adjacent cells. The 106x69 grid (7,314 cells) fits comfortably in memory (~175 KB).

### 1.6.2 2D vs. 3D Collision

**Decision:** Stage 0 uses 2D collision detection (XY plane projection).

**Rationale:** Ground-based football involves players running on a 2D surface. Vertical separation (jumping) is rare and handled by Heading Mechanics (Spec #10) in Stage 1. Simplifying to 2D reduces implementation complexity and avoids premature optimization for aerial scenarios not yet specified.

### 1.6.3 Determinism Requirement

**Decision:** Use deterministic RNG seeded from Match seed for all probabilistic outcomes (fall/stumble probability).

**Rationale:** Master Vol 1 Â§1.3 mandates deterministic simulation for replay fidelity and future networked multiplayer. Using `System.Random` or Unity's `Random.Range()` without explicit seeding would break determinism. All random calls in Collision System route through a match-seeded RNG instance.

### 1.6.4 Same-Team Collision Behavior

**Decision:** Detect same-team collisions but apply reduced momentum transfer (30% of normal).

**Rationale:** Teammates do not deliberately knock each other over. In real football, players on the same team exhibit spatial awareness and avoid hard contact. The 30% momentum scale simulates this implicit coordination.

**Open Question (Requires Validation):** Should same-team collisions be capable of triggering GROUNDED state at extreme forces (e.g., accidental collision at full sprint)? Current proposal: same-team collisions can trigger STUMBLING but not GROUNDED. This prevents teammates from knocking each other down while still allowing minor balance disruption. Confirm with gameplay testing in Stage 0 implementation.

---

**END OF SECTION 1**

---

## Cross-Reference Verification Checklist

| Dependency | Verified Against | Status |
|------------|------------------|--------|
| `BallState` struct | Ball Physics Â§3.1.1 | âœ“ Position, Velocity, Radius confirmed |
| `AgentPhysicalProperties` struct | Agent Movement Â§3.5.4 | âœ“ All fields confirmed (Position, Velocity, Mass, HitboxRadius, Strength, IsGrounded) |
| `BodyPart` enum | Ball Physics Â§3.1.10.1 | âœ“ Enum exists with Foot, Shin, Thigh, Torso, Head, Arm |
| Coordinate system | Ball Physics Â§3.1.1 | âœ“ Origin, axes, units confirmed |
| GROUNDED/STUMBLING states | Agent Movement Â§3.1.2 | âœ“ States exist in AgentMovementState enum |
| Determinism requirement | Master Vol 1 Â§1.3 | âœ“ Referenced, seeded RNG specified |
| Frame execution order | Agent Movement Â§4.4.1, Ball Physics Â§4.4.1 | âœ“ Collision runs after movement and ball physics |

---

**Next Section:** Section 2 (System Overview) â€” Architecture diagram, data flow, collision types, performance budget, failure modes.