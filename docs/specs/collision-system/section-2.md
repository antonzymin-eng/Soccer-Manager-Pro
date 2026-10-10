# Collision System Specification â€” Section 2: System Overview

**Purpose:** Formal functional requirements, system architecture, data flow, frame pipeline position,
and failure mode recovery for Collision System Specification #3 (Stage 0 Physics Foundation).
This section establishes the "what" and "why" before Section 3 defines the "how."

**Created:** February 15, 2026, 3:30 PM PST
**Revised:** October 10, 2026 (ERR-003-009); prior: March 05, 2026, audit session
**Version:** 1.3
**Status:** Draft â€” Revised
**Author:** Claude (AI) with Anton (Lead Developer)
**Specification:** #3 of 20 (Stage 0 Physics Foundation)
**Prerequisite:** Section 1 (Purpose & Scope) v1.1
**Changelog:**
- v1.3 (Oct 10, 2026): ERR-003-009. FR-03's `AgentBallCollisionData` field list gains
  `AgentPosition`, the agent-centre input to Ball Physics deflection added by ERR-003-007
  (July 27, 2026). That entry's back-prop updated only Section 3.4.3. Documentation only; the
  code has carried the field since July 27.

**Changelog:**
- v1.2 (Mar 5, 2026): Comprehensive audit fixes. (1) BeginFrame seed formula in
  §2.6.4 updated to match Section 4 §4.7.3 authoritative implementation — added
  `((ulong)frameNumber << 32)` term for better seed distribution [F-01]. (2) Added
  zero-state guard to DeterministicRNG constructor in §2.6.4 to match Section 4
  §4.7.2 [F-07]. (3) FR test ID ranges in §2.1 table corrected to match actual
  Section 5 test IDs [F-03]. (4) Memory budget in §2.5 updated from <180 KB to <300 KB to match Section 4
  §4.5 authoritative figure [F-06]. (5) Field naming
  _s0/_s1 updated to _state0/_state1 to match Section 4 [F-17].
- v1.1 (Feb 17, 2026): Added Â§2.1 Functional Requirements (FR-01 through FR-08) as distinct
  named section. Renumbered Â§2.1 Architecture â†’ Â§2.2, Â§2.2 Data Flow â†’ Â§2.3,
  Â§2.3 Collision Types â†’ Â§2.4, Â§2.4 Performance â†’ Â§2.5, Â§2.5 Failure Modes â†’ Â§2.6.
  Removed Agent-Boundary from collision type matrix (out-of-scope per Â§1.2.1). Added
  "estimates pending profiling" caveats to performance figures. Corrected frame loop
  cross-reference to Â§1.1 (specific). Added tunneling risk analysis note.
- v1.0 (Feb 15, 2026): Initial draft.

---

## 2.1 Functional Requirements

The eight functional requirements below are derived from the seven core responsibilities in
Section 1.1, plus performance and determinism constraints from Master Vol 1 Â§1.3 and Master
Vol 4 Â§3.2. Each FR maps to a sub-system in Section 3 and has explicit test coverage.

| FR ID | Short Name | Priority | Section 3 Owner | Test IDs |
|-------|------------|----------|-----------------|----------|
| FR-01 | Spatial Partitioning | CRITICAL | Â§3.1 | SH-001â€“SH-006 |
| FR-02 | Agent-Agent Detection | CRITICAL | Â§3.2 | CD-001â€“CD-007 |
| FR-03 | Agent-Ball Detection | CRITICAL | Â§3.2 | CD-004, CD-005, CD-007 |
| FR-04 | Collision Response Physics | HIGH | Â§3.3 | CR-001â€“CR-005 |
| FR-05 | State Change Triggers | HIGH | Â§3.3 | FL-001â€“FL-005 |
| FR-06 | Foul Detection Groundwork | MEDIUM | Â§3.3 | IT-002, IT-003 |
| FR-07 | Event Publishing | MEDIUM | Â§3.4 | IT-004â€“IT-008 |
| FR-08 | Deterministic Execution | CRITICAL | Â§3.5 | DT-001â€“DT-003 |

---

### FR-01 â€” Spatial Partitioning

**Statement:** The system shall partition all 22 agents and 1 ball into a grid-based spatial hash
each frame, achieving O(N) broad-phase query performance for N = 23 entities.

**Rationale:** Without partitioning, naive collision detection requires O(NÂ²) = 253 pair checks
per frame. While 253 checks would not breach the 0.30ms budget at current entity counts, the
spatial hash provides headroom for Stage 1+ expansion (more agents, more collision types) and
is consistent with industry-standard physics engine architecture.

**Acceptance Criteria:**
- Grid cell size = 1.0m (2Ã— maximum agent hitbox radius of 0.50m)
- Grid dimensions: 106 Ã— 69 cells (covers 105 Ã— 68m pitch with 0.5m boundary margin per edge)
- All 23 entities inserted before any pair queries execute
- Entities within 0.5m of a cell boundary inserted into adjacent cells (prevents missed collisions
  at cell edges)
- Clear operation reuses existing memory (zero heap allocation per FR-08)

**Failure consequence:** FR-02 and FR-03 are inoperative; no collision detection functions.

---

### FR-02 â€” Agent-Agent Collision Detection

**Statement:** The system shall detect hitbox overlaps between all agent pairs within the same
or adjacent spatial hash cells using circle-circle intersection in the XY plane.

**Rationale:** Player-to-player contact is the most frequent physically contested event in a
football match. Accurate detection is prerequisite to physically plausible response (FR-04)
and valid foul data (FR-06). XY-plane-only detection is a deliberate Stage 0 simplification;
aerial duels are deferred to Heading Mechanics (Spec #10).

**Acceptance Criteria:**
- Collision condition: `distance_2D(p1, p2) < r1 + r2` (circle-circle overlap in XY plane only)
- Z coordinate ignored for all agent-agent collision tests
- Same-team pair collisions detected but impulse scaled by SAME_TEAM_MOMENTUM_SCALE (= 0.3)
- Grounded agents (`IsGrounded == true`) act as static obstacles; receive no velocity impulse
- Each agent pair processed exactly once per frame (deduplication via processed-pairs bitfield)

**Failure consequence:** Agents pass through each other; contested duels produce no effect.

---

### FR-03 â€” Agent-Ball Collision Detection

**Statement:** The system shall detect contact between any agent hitbox and the ball using
circle-sphere intersection with a Z-axis height filter, invoking `Ball.OnCollision()` on contact.

**Rationale:** Agent-ball contact is the mechanism by which players control the ball. The
Collision System's sole role is detection and callback dispatch. Ball Physics (Spec #1) handles
deflection; First Touch Mechanics (Spec #11) handles possession transfer. This separation of
concerns prevents circular dependencies.

**Acceptance Criteria:**
- Collision condition: `distance_2D(agent, ball) < agent.HitboxRadius + ball.Radius`
  AND `ball.Position.z <= AGENT_REACH_HEIGHT` (2.0m; Stage 0 fixed value)
- ball.Radius = 0.11m (from `BallPhysicsConstants.Ball.RADIUS`)
- `AgentBallCollisionData` fields: ContactPoint, AgentPosition, AgentVelocity, BodyPart (TORSO in
  Stage 0), AgentID, TeamID, IsGoalkeeper
- `AgentPosition` (ERR-003-009, back-prop of ERR-003-007): the agent's centre, in world 3-D metres,
  at the moment of contact (production: the collision snapshot's `Position`). It is NOT the contact
  point: `ContactPoint` lies on the hitbox surface, while `AgentPosition` is the body axis from which
  Ball Physics #1 Section 3.1.10.1 (`BallCollision.ApplyAgentDeflection`) derives the planar,
  vertical-cylinder deflection normal. The consumer ignores Z. Example: an agent centred at
  (50.0, 34.0, 0) with hitbox radius 0.40 m touches a ball at (50.50, 34.0, 0.11). ContactPoint is
  about (50.40, 34.0, 0.11), AgentPosition is (50.0, 34.0, 0), and the deflection normal is +X.
- `Ball.OnCollision()` invoked exactly once per agent-ball overlap per frame
- Per-limb body part classification deferred to Stage 1

**Failure consequence:** Ball does not respond to player contact; game is unplayable.

---

### FR-04 â€” Collision Response Physics

**Statement:** The system shall compute velocity impulses for colliding agents using impulse-based
collision resolution, with penetration separation via minimum translation vector (MTV).

**Rationale:** Physically plausible collision response is essential for the project's "observable
tactical impact" design principle. Without momentum transfer, shoulder charges have no effect and
positional contests are meaningless. Impulse-based resolution is preferred over penalty forces
because it is instantaneous, numerically stable at 60Hz, and analytically verifiable.

**Acceptance Criteria:**
- Impulse magnitude derived from relative closing velocity, agent masses (72.5â€“100 kg), and
  coefficient of restitution (e = 0.3; empirically tuned for flesh-on-flesh contact)
- Penetration separated by mass-weighted MTV shares (heavier agent moves less)
- Momentum conserved within floating-point tolerance (< 0.001 kgÂ·m/s error per collision pair)
- All response computation completes within 0.30ms mean frame budget

**Failure consequence:** Collision detection produces no physical outcome; agents clip through.

---

### FR-05 â€” State Change Triggers

**Statement:** The system shall signal Agent Movement when collision impulse exceeds configured
thresholds, triggering GROUNDED (fall) or STUMBLING state transitions via `CollisionResponse`.

**Rationale:** Fall and stumble states are the primary visible and tactically significant
consequences of physical contact. The Collision System computes the force; Agent Movement owns
the state machine. This separation allows state logic to evolve independently of physics.

**Acceptance Criteria:**
- Fall probability: function of impact force magnitude, agent Strength (1â€“20), and possession
  status (ball-carrier has reduced fall resistance â€” modelled as reduced effective Strength)
- Stumble probability: calculated independently using a lower force threshold than fall
- All probability sampling uses DeterministicRNG (FR-08)
- Output fields: `CollisionResponse.TriggerGrounded` (bool), `CollisionResponse.TriggerStumble`
  (bool), `CollisionResponse.GroundedDuration` (seconds; range 0.5â€“3.0, force-dependent)
- Agent Movement applies state transition upon receiving trigger; duration managed by Agent Movement

**Failure consequence:** No fall or stumble animations; contact has no visible player consequence.

---

### FR-06 â€” Foul Detection Groundwork

**Statement:** The system shall package raw contact force data for every agent-agent collision
into a `ContactForceData` struct embedded in `CollisionEvent.FoulData`, for future Referee System
consumption.

**Rationale:** Stage 0 cannot implement referee logic (Referee System is Stage 1+), but the data
pipeline must be validated now. Collecting and publishing raw force data in Stage 0 confirms the
data is available and correctly structured when the Referee System is implemented, avoiding
costly pipeline rework.

**Acceptance Criteria:**
- `ContactForceData` fields: ForceMagnitude (float, Newtons), ForceDirection (Vector2),
  ContactType (enum: SHOULDER_TO_SHOULDER | FROM_BEHIND | SIDE_IMPACT),
  InstigatorAgentID (int), VictimAgentID (int), VictimHasBall (bool), InstigatorPlayingBall (bool)
- ContactType classified from relative velocity angle at contact and agent facing directions
- Data packaged for all agent-agent collisions in Stage 0; not acted upon (no foul decisions)
- `CollisionEvent.FoulData` is nullable; null for agent-ball collisions (no foul data needed)

**Failure consequence:** Referee System requires data pipeline rework in Stage 1; increased cost.

---

### FR-07 â€” Event Publishing

**Statement:** The system shall generate one `CollisionEvent` per detected collision per frame,
placed into the Event System queue for downstream consumption by replay, statistics, and UI.

**Rationale:** Per Master Vol 1 Â§1.3, every simulation event must be reconstructable from the
event log for replay. `CollisionEvent` records also feed the statistics system (tackles, fouls,
interceptions) and drive audio-visual feedback (tackle sound cues, foul flash indicators).

**Acceptance Criteria:**
- `CollisionEvent` fields: MatchTime (float, seconds), Type (AGENT_AGENT | AGENT_BALL),
  Entity1ID (int), Entity2ID (int), ContactPoint (Vector3), ImpactForce (float), FoulData
  (ContactForceData?, null for Agent-Ball type)
- One event per unique collision pair per frame
- Events queued into Event System buffer; Collision System does not dispatch events
- Maximum 50 events per frame (matches MAX_COLLISION_PAIRS_PER_FRAME ceiling)
- Event System (Spec #17) is forward-declared; this FR establishes the data contract

**Failure consequence:** Replay reconstruction fails; statistics and UI feedback non-functional.

---

### FR-08 â€” Deterministic Execution

**Statement:** The system shall produce identical collision outcomes for identical initial states
across all platforms, sessions, and replay playbacks. All stochastic decisions shall use a seeded
deterministic RNG; no system RNG calls are permitted.

**Rationale:** Per Master Vol 1 Â§1.3, the simulation must be fully deterministic for replay
fidelity and future multiplayer synchronisation. Fall/stumble probability is inherently stochastic.
Using platform-dependent RNG sources (`System.Random`, `UnityEngine.Random`) would produce
divergent outcomes across sessions, breaking replay and multiplayer consistency.

**Acceptance Criteria:**
- All stochastic calls use `DeterministicRNG` (xorshift128+ seeded from match seed + frame number)
- `BeginFrame(ulong matchSeed, int frameNumber)` called once per frame before `UpdateCollisions()`
- Zero calls to `System.Random`, `UnityEngine.Random`, `Guid.NewGuid()`, or `DateTime.Now`
- Identical (matchSeed, frameNumber, entity inputs) always produces identical `CollisionEvent` output
- Stage 5+: replace minimal xorshift128+ with Fixed64 Math Library RNG (Spec #9) when available

**Failure consequence:** Replays diverge; multiplayer synchronisation is impossible.

---

## 2.2 Architecture Diagram

The Collision System operates as a four-phase pipeline executed once per frame after all entity
movement updates are complete.

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚                           COLLISION SYSTEM                                  â”‚
â”‚                        (executes at 60Hz, post-movement)                    â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                                                             â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”    â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”    â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”  â”‚
â”‚  â”‚  SPATIAL HASH   â”‚â”€â”€â”€â–¶â”‚   BROAD PHASE   â”‚â”€â”€â”€â–¶â”‚     NARROW PHASE        â”‚  â”‚
â”‚  â”‚    (Insert)     â”‚    â”‚    (Query)      â”‚    â”‚    (Intersection)       â”‚  â”‚
â”‚  â”‚                 â”‚    â”‚                 â”‚    â”‚                         â”‚  â”‚
â”‚  â”‚ â€¢ Clear grid    â”‚    â”‚ â€¢ Query 3Ã—3     â”‚    â”‚ â€¢ Circle-circle test    â”‚  â”‚
â”‚  â”‚ â€¢ Insert 22     â”‚    â”‚   neighborhood  â”‚    â”‚ â€¢ Circle-sphere + Z     â”‚  â”‚
â”‚  â”‚   agents + ball â”‚    â”‚   per entity    â”‚    â”‚   height filter         â”‚  â”‚
â”‚  â”‚ â€¢ O(N) insert   â”‚    â”‚ â€¢ Deduplicate   â”‚    â”‚ â€¢ Compute manifold      â”‚  â”‚
â”‚  â”‚ â€¢ FR-01         â”‚    â”‚   pairs         â”‚    â”‚ â€¢ FR-02, FR-03          â”‚  â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜    â”‚ â€¢ O(N) average  â”‚    â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜  â”‚
â”‚                         â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜                â”‚                â”‚
â”‚                                                            â–¼                â”‚
â”‚                                                â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”  â”‚
â”‚                                                â”‚   COLLISION RESPONSE    â”‚  â”‚
â”‚                                                â”‚      (Resolve)          â”‚  â”‚
â”‚                                                â”‚                         â”‚  â”‚
â”‚                                                â”‚ â€¢ Impulse + MTV (FR-04) â”‚  â”‚
â”‚                                                â”‚ â€¢ Fall/stumble (FR-05)  â”‚  â”‚
â”‚                                                â”‚ â€¢ Foul data  (FR-06)    â”‚  â”‚
â”‚                                                â”‚ â€¢ Events     (FR-07)    â”‚  â”‚
â”‚                                                â”‚ â€¢ Det. RNG   (FR-08)    â”‚  â”‚
â”‚                                                â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜  â”‚
â”‚                                                                             â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

### 2.2.1 Phase Responsibilities

| Phase | Input | Output | Complexity | FR Coverage |
|-------|-------|--------|------------|-------------|
| **Spatial Hash** | 23 entity positions | Populated grid (7,314 cells) | O(N) | FR-01 |
| **Broad Phase** | Grid | Candidate collision pairs | O(N) average | FR-01 |
| **Narrow Phase** | Candidate pairs | Confirmed collisions + manifolds | O(K) | FR-02, FR-03 |
| **Response** | Confirmed manifolds | Impulses, state triggers, events | O(K) | FR-04â€“FR-07 |

N = 23 entities; K = actual collisions per frame (typically 0â€“10).

### 2.2.2 Design Rationale â€” Spatial Hash vs. Alternatives

| Criterion | Uniform Grid (selected) | Quadtree | BVH |
|-----------|------------------------|----------|-----|
| Implementation complexity | Low | Medium | High |
| Performance at N=23 | Sufficient | Overkill | Overkill |
| Worst-case predictability | High (constant cell ops) | Medium (tree depth varies) | Medium |
| Cache behaviour | Good (contiguous cells) | Poor (pointer chasing) | Poor |
| Stage 1+ extensibility | Adequate | Better | Best |

**Cell size derivation:**

```
max_hitbox_radius      = 0.50m  (Strength 20; Agent Movement Â§3.5.4.3)
ball_radius            = 0.11m  (BallPhysicsConstants.Ball.RADIUS)
max_combined_agent     = 0.50 + 0.50 = 1.00m
max_combined_ball      = 0.50 + 0.11 = 0.61m

Required: cell_size >= max_combined_agent â†’ cell_size = 1.0m âœ“
Ball case also satisfied: 0.61m < 1.0m âœ“

A 3Ã—3 neighborhood query (9 cells, 9.0mÂ² area) is guaranteed to contain
all potential collision partners for any entity in the grid.
```

---

## 2.3 Data Flow

### 2.3.1 Frame Pipeline Position

The Collision System occupies step 5 of the 7-step 60Hz simulation loop. The complete loop
is defined in Section 1.1. Key sequencing constraints:

1. **After Agent Movement and Ball Physics** â€” Positions must be final before detection runs
2. **Before Event System dispatch** â€” Events must be queued before Spec #17 distributes them
3. **Responses apply next frame** â€” Impulses modify velocity for frame N+1; current-frame
   positions are not retroactively corrected (avoids position rollback complexity)

### 2.3.2 Data Interface Contracts

**Inbound (consumed each frame):**

| Source | Interface | Fields Used |
|--------|-----------|-------------|
| Agent Movement (Spec #2) | `AgentPhysicalProperties` | Position, Velocity, Mass, HitboxRadius, Strength, IsGrounded |
| Agent Movement (Spec #2) | `Agent.TeamID` | Same-team impulse scaling |
| Ball Physics (Spec #1) | `BallState` | Position, Velocity |
| Ball Physics (Spec #1) | `BallPhysicsConstants.Ball.RADIUS` | 0.11m (static) |

**Outbound (produced each frame):**

| Target | Interface | Trigger |
|--------|-----------|---------|
| Ball Physics (Spec #1) | `Ball.OnCollision(AgentBallCollisionData)` | Agent-ball contact (FR-03) |
| Agent Movement (Spec #2) | `CollisionResponse` per agent | Agent-agent contact (FR-04, FR-05) |
| Event System (Spec #17) | `CollisionEvent` queue | Any confirmed collision (FR-07) |
| Referee System (TBD) | via `CollisionEvent.FoulData` | Agent-agent contact (FR-06) |

### 2.3.3 Coordinate System

Inherited from Ball Physics Spec #1 Â§3.1.1 (reproduced for reference):

| Axis | Range | Direction |
|------|-------|-----------|
| X | 0 â€“ 105m | Pitch length (goal-to-goal) |
| Y | 0 â€“ 68m | Pitch width (touchline-to-touchline) |
| Z | 0 â€“ âˆž | Vertical height (0 = ground) |

Origin (0, 0, 0): Corner flag at home team's left defensive corner.

**Stage 0 simplification:** All collision tests operate in XY plane only. Z is used solely for
the agent-ball height filter (FR-03). Full 3D collision for aerial duels deferred to Spec #10.

---

## 2.4 Collision Types

### 2.4.1 Stage 0 Collision Matrix

The Collision System owns exactly two entity-to-entity collision types in Stage 0.
Agent-boundary enforcement is explicitly excluded â€” it is owned by Agent Movement (Â§3.6) and
Ball Physics (Â§3.4) per Section 1.2.1 and is not represented in this matrix.

| Type | Entities | Detection | Response | Foul Data Generated |
|------|----------|-----------|----------|---------------------|
| **Agent-Agent** | 2 agents (any team mix) | Circle-circle, XY plane | Impulse + MTV; GROUNDED/STUMBLING trigger | Yes (FR-06) |
| **Agent-Ball** | 1 agent + ball | Circle-sphere; Z height filter | `Ball.OnCollision()` callback only | No |

### 2.4.2 Agent-Agent Collision

```
Condition:  distance_2D(agent1.pos, agent2.pos) < agent1.radius + agent2.radius

Where:
  distance_2D = sqrt((x2âˆ’x1)Â² + (y2âˆ’y1)Â²)   // Z ignored
  radius range: 0.3525m (Strength 1) to 0.50m (Strength 20)
```

Same-team impulse scaling: `momentumScale = (sameTeam) ? 0.3f : 1.0f`
Models natural spatial awareness â€” teammates avoid hard contact but are not frictionless.

Grounded agent rule: `IsGrounded == true` â†’ agent is a static obstacle; receives no impulse;
other agents can collide with and stumble over them.

### 2.4.3 Agent-Ball Collision

```
Condition:  distance_2D(agent.pos, ball.pos) < agent.radius + 0.11m
            AND ball.pos.z <= 2.0m  (AGENT_REACH_HEIGHT)
```

**AGENT_REACH_HEIGHT = 2.0m derivation:**

```
UEFA average player height:       1.78m
Typical torso contact zone:       0.6 â€“ 1.2m
Standing reach (arms extended): ~ 2.20m
Stage 0 conservative fixed value: 2.00m (all agents treated equally)

Any ball centre â‰¤ 2.0m: reachable without jumping â†’ Stage 0 collision applies
Any ball centre > 2.0m: requires aerial challenge â†’ Heading Mechanics (Spec #10)
Refinement to per-agent height attribute: Stage 1
```

Responsibility handoff (collision system is detection-only):
- Collision System: detects â†’ calls `Ball.OnCollision()`
- Ball Physics: computes deflection velocity
- First Touch (Spec #11): decides possession transfer

### 2.4.4 Stage 1+ Collision Types (Forward Reference)

| Type | Stage | Prerequisite |
|------|-------|--------------|
| Aerial Duel | 1 | Jump mechanics (Agent Movement Stage 1) |
| Slide Tackle | 1 | Animation-driven capsule hitboxes |
| Goalkeeper Dive | 1 | Goalkeeper Mechanics (Spec #11) |
| Multi-Body Pile-Up | 2+ | Constraint solver |

---

## 2.5 Performance Budget

> **Important:** All timing figures below are engineering estimates derived from algorithmic
> complexity analysis. They are subject to revision after profiling on target hardware during
> Stage 0 implementation. See Development Best Practices Â§7 for profiling methodology and
> Â§2.5.4 for Unity Profiler integration.

### 2.5.1 Per-Frame Targets

| Scenario | Target (mean) | Ceiling (p99) | Basis |
|----------|---------------|---------------|-------|
| Typical (agents distributed) | <0.15ms | 0.25ms | 5â€“10 pairs; hash ops dominate |
| Clustered (corner kick) | <0.30ms | 0.40ms | â‰¤10 agents in 10Ã—10m zone |
| Theoretical worst case | <0.40ms | 0.50ms | All 22 agents + ball co-located |

**Frame budget context (Master Vol 4 Â§3.2 and Ball Physics Spec #1 Â§6.2.1):**

```
Total frame budget:               16.67ms  (60Hz)
  Physics allocation:              ~6.00ms
    Ball Physics:                   0.50ms  mean
    Agent Movement (22 agents):     1.00ms  mean
    >>> Collision System:           0.30ms  mean / 0.50ms ceiling <<<
    Remaining physics budget:       4.20ms
```

The 0.30ms mean allocation is ~2Ã— the estimated 0.15ms typical cost, providing headroom for
corner kick clustering, profiler overhead, and Stage 1 expansion without regression.

### 2.5.2 Complexity Breakdown (Estimates)

| Phase | Complexity | Estimated Cost | Note |
|-------|------------|----------------|------|
| Spatial Hash Clear | O(C_occ) | ~0.02ms | Sparse clear of occupied cells only |
| Spatial Hash Insert | O(N) | ~0.03ms | N=23; boundary entities counted twice |
| Broad Phase Query | O(N) avg | ~0.05ms | 3Ã—3 cell lookup per entity |
| Narrow Phase Test | O(K) | ~0.03ms | K typically 0â€“10 |
| Response Calculation | O(K) | ~0.05ms | Impulse + MTV + trigger per pair |
| **Total** | **O(N + K)** | **~0.18ms** | Dominated by hash ops |

### 2.5.3 Memory Budget

| Component | Size | Basis |
|-----------|------|-------|
| Spatial hash grid | ~175 KB | 7,314 cells Ã— 24 bytes |
| Cell entity lists | ~2 KB | ~500 occupied cells Ã— list overhead |
| Manifold buffer | ~1 KB | 50 max pairs Ã— 20 bytes |
| Processed pairs bitfield | ~32 bytes | 253 max pairs as bit flags |
| **Total (static)** | **<300 KB (see Section 4 §4.5 for authoritative breakdown)** | Zero per-frame heap allocations |

All buffers pre-allocated at `Awake()`; reused every frame with clear-not-reallocate pattern.

### 2.5.4 Profiling Markers

```csharp
/// <summary>
/// Unity Profiler integration for per-phase timing measurement.
/// Used during Stage 0 implementation to validate budget estimates.
/// Active in UNITY_EDITOR and DEVELOPMENT_BUILD only â€” zero overhead in release.
/// </summary>
#if UNITY_EDITOR || DEVELOPMENT_BUILD
public static class CollisionProfiler
{
    public static readonly ProfilerMarker UpdateMarker       = new("Collision.Update");
    public static readonly ProfilerMarker ClearMarker        = new("Collision.SpatialHash.Clear");
    public static readonly ProfilerMarker InsertMarker       = new("Collision.SpatialHash.Insert");
    public static readonly ProfilerMarker BroadPhaseMarker   = new("Collision.BroadPhase");
    public static readonly ProfilerMarker NarrowPhaseMarker  = new("Collision.NarrowPhase");
    public static readonly ProfilerMarker ResponseMarker     = new("Collision.Response");
}
#endif
```

---

## 2.6 Failure Modes & Recovery

### 2.6.1 Critical Failures

**FM-01: NaN or Infinity in Entity Position**

| Detection | `HasInvalidValues()` pre-check before spatial hash insert |
|-----------|---|
| Cause | Upstream physics (Ball Physics or Agent Movement) produced invalid state |
| Recovery | Skip entity for this frame; log ERROR with entity ID and last valid position |
| Severity | HIGH â€” NaN in grid position corrupts all hash lookups for that cell |

```csharp
private static bool HasInvalidValues(Vector3 pos) =>
    float.IsNaN(pos.x) || float.IsInfinity(pos.x) ||
    float.IsNaN(pos.y) || float.IsInfinity(pos.y) ||
    float.IsNaN(pos.z) || float.IsInfinity(pos.z);
```

---

**FM-02: Tunneling (Entity Passes Through Another)**

| Detection | Penetration depth > max(r1, r2) post narrow phase |
|-----------|---|
| Cause | High relative velocity traversed combined radius in single 16.7ms timestep |
| Recovery | Apply gentle MTV separation; reduce impulse; log WARNING |
| Severity | MEDIUM â€” visually jarring; structurally recoverable |

> **Risk note:** Tunneling is extremely unlikely at football speeds. Maximum sprint ~10 m/s gives
> maximum displacement of 10 Ã— 0.0167 = 0.167m per frame, well below minimum combined radius of
> 0.705m. Tunneling would require corrupted velocity state from an upstream system failure.

---

**FM-03: Spatial Hash Query Iteration Limit Exceeded**

| Detection | Iteration counter > MAX_ITERATIONS (1,000) |
|-----------|---|
| Cause | Corrupted grid data or deduplication logic failure |
| Recovery | Break loop; log ERROR with grid diagnostic dump; continue with partial pairs |
| Severity | HIGH â€” infinite loop would freeze the simulation |

---

**FM-04: Excessive Collision Pairs Per Frame**

| Detection | Pair count > MAX_COLLISION_PAIRS_PER_FRAME (50) |
|-----------|---|
| Cause | Extreme clustering or corrupted position data placing all entities at one location |
| Recovery | Process first 50 pairs (sorted by entity ID for determinism); log WARNING |
| Severity | MEDIUM â€” 50 pairs exceeds any realistic game scenario; missed pairs acceptable |

### 2.6.2 Non-Critical Anomalies

**FM-05: Zero-Distance Collision (Two Entities at Identical Position)**

| Detection | distance_2D < EPSILON (0.001m) |
|-----------|---|
| Cause | Spawn error or teleportation bug in upstream system |
| Recovery | Use arbitrary collision normal (Vector2.right); separate by combined radii |
| Severity | LOW â€” rare; fallback produces a defined result |

**FM-06: Agent Position Outside Pitch Extents**

| Detection | pos.x < âˆ’1.0 or > 106.0; pos.y < âˆ’1.0 or > 69.0 |
|-----------|---|
| Cause | Agent Movement boundary enforcement failed |
| Recovery | Clamp to nearest boundary cell; log WARNING; do not discard entity |
| Severity | LOW â€” 1m boundary margin prevents grid array overflow |

### 2.6.3 Recovery Principles

All recovery follows the pattern established by Ball Physics (Spec #1 Â§5.3) and Agent Movement (Spec #2 Â§3.7):

1. Detect early â€” validate inputs before each processing phase
2. Fail gracefully â€” never crash; always produce a defined output
3. Log thoroughly â€” include entity IDs, positions, velocities, and frame number
4. Continue simulation â€” partial results beat halting the match
5. Visualise in editor â€” Debug Gizmos expose failure states on screen during development

### 2.6.4 Determinism Implementation (FR-08)

Fall and stumble decisions require random number sampling. Using platform-dependent RNG breaks
replay and multiplayer determinism. Resolution: xorshift128+ seeded per frame from match seed.

```csharp
private DeterministicRNG _rng;

/// <summary>Called once per frame BEFORE UpdateCollisions(). Initialises deterministic RNG.</summary>
public void BeginFrame(ulong matchSeed, int frameNumber)
{
    // Combine match seed with frame number.
    // XOR ensures different frames get different sequences.
    // Left-shift of frameNumber into upper 32 bits prevents collisions for
    // small seed + small frame number combinations.
    // Must match Section 4 §4.7.3 (authoritative implementation).
    ulong frameSeed = matchSeed ^ (ulong)frameNumber ^ ((ulong)frameNumber << 32);
    _rng = new DeterministicRNG(frameSeed);
}

/// <summary>
/// Minimal xorshift128+ deterministic RNG for Stage 0.
/// Replace with Fixed64 Math Library implementation at Stage 5+ (Spec #9).
/// Reference: Vigna, S. "An experimental exploration of Marsaglia's xorshift generators." (2016)
/// </summary>
public struct DeterministicRNG
{
    private ulong _state0, _state1;

    public DeterministicRNG(ulong seed)
    {
        // SplitMix64 initialises state safely from any seed (avoids all-zero state)
        _state0 = SplitMix64(seed);
        _state1 = SplitMix64(_state0);

        // Ensure non-zero state (xorshift requirement)
        if (_state0 == 0 && _state1 == 0)
        {
            _state0 = 0x853C49E6748FEA9BUL;
            _state1 = 0xDA3E39CB94B95BDBUL;
        }
    }

    private static ulong SplitMix64(ulong x)
    {
        x += 0x9E3779B97F4A7C15UL;
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        return x ^ (x >> 31);
    }

    public float NextFloat()
    {
        ulong s1 = _state0, s0 = _state1;
        ulong result = s0 + s1;
        _state0 = s0;
        s1 ^= s1 << 23;
        _state1 = s1 ^ s0 ^ (s1 >> 18) ^ (s0 >> 5);
        return (result >> 40) * (1.0f / (1UL << 24));  // Upper 24 bits â†’ float [0, 1)
    }
}
```

---

## Section 2 Summary

| Subsection | Key Content |
|------------|-------------|
| **2.1 Functional Requirements** | FR-01â€“FR-08 with rationale, acceptance criteria, and failure consequence |
| **2.2 Architecture** | Four-phase pipeline; uniform grid spatial hash (1.0m cells); spatial hash rationale |
| **2.3 Data Flow** | Step 5 of 7 in frame loop; interface contracts; coordinate system |
| **2.4 Collision Types** | 2 Stage 0 types (Agent-Agent, Agent-Ball); boundary excluded from scope |
| **2.5 Performance** | <0.15ms typical / <0.50ms ceiling (estimates); <180KB static memory; zero allocations |
| **2.6 Failure Modes** | 4 critical + 2 anomaly failure modes; recovery principles; deterministic RNG |

---

## Cross-Reference Verification

| Reference | Target | Verified | Notes |
|-----------|--------|----------|-------|
| Ball Physics Â§3.1.1 | Coordinate system | âœ“ | Â§2.3.3 reproduces definitions |
| Ball Physics Â§3.1.2 | ball.Radius = 0.11m | âœ“ | FR-03, Â§2.4.3 |
| Ball Physics Â§6.2.1 | 0.50ms performance ref | âœ“ | Â§2.5.1 budget context |
| Agent Movement Â§3.5.4 | `AgentPhysicalProperties` | âœ“ | Â§2.3.2 inbound table |
| Agent Movement Â§3.5.4.3 | HitboxRadius 0.3525â€“0.50m | âœ“ | FR-01 cell size derivation, Â§2.4.2 |
| Agent Movement Â§3.6 | Boundary enforcement (out of scope) | âœ“ | Â§2.4.1 explicitly excluded |
| Master Vol 1 Â§1.3 | Determinism requirement | âœ“ | FR-08, Â§2.6.4 |
| Master Vol 4 Â§3.2 | 6ms physics budget | âœ“ | Â§2.5.1 budget context |

---

**End of Section 2**

**Page Count:** ~10 pages (expanded from v1.0 ~8 pages)
**Version:** 1.1
**Next Section:** Section 3 â€” Core Systems (Spatial Partitioning, Collision Detection, Collision Response)
