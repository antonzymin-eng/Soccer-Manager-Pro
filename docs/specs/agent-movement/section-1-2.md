# Agent Movement Specification â€” Sections 1 & 2

**Created:** February 9, 2026, 12:00 AM PST  
**Updated:** March 4, 2026, 12:00 AM PST  
**Version:** 1.1  
**Status:** In Review  
**Stage:** Stage 0 â€” Physics Foundation  
**Specification:** #2 of 20  
**Dependencies:** Ball Physics (Spec #1), Collision System (Spec #3), Fixed64 Math Library (Spec #9)  
**Implements:** Master Vol 1, Section 2 (Agent Physicality); Master Development Plan, Section 2.2 (Agent Movement System)


---

## v1.1 Changelog (March 4, 2026)

**CRITICAL FIXES:**

1. **FR-3 deceleration distances revised (FR3_Revision_Note applied):** Controlled stopping distance changed from 2.0-3.0m to **3.0-5.0m**. Emergency stopping distance changed from 1.0-1.5m to **2.5-3.5m**. Original values required 2.7-4.1g deceleration forces, biomechanically implausible. Revised values produce 0.8-1.65g, within documented human capability.

2. **Spec numbering corrected throughout:** Updated all cross-references to match current numbering per FILE_MANIFEST.md. First Touch = Spec #4 (was #11), Pass Mechanics = Spec #5 (was #4), Perception System = Spec #7 (was #6), Decision Tree = Spec #8 (was #7), Fixed64 = Spec #9 (was #8), Goalkeeper = Spec #11 (was #10).

3. **FR-7 mass range updated:** Changed from 62-92 kg to **72.5-100 kg** to match Section 3.5.4.2 derivation formula. Hitbox description updated to match Strength-based derivation.

**MODERATE FIXES:**

4. **Status updated:** Draft to In Review.
5. **FR-1 state count clarified:** 7 states explicitly noted.

---

## 1. PURPOSE & SCOPE

### 1.1 What This System Does

The Agent Movement System simulates realistic locomotion physics for all outfield players (20 per match). It governs how agents accelerate, decelerate, turn, and transition between movement states within the 60Hz physics simulation loop.

This is the physical layer only â€” it receives movement *commands* (target position, desired speed) from higher-level systems and produces physically plausible *motion* subject to momentum, inertia, fatigue, and attribute constraints.

**Core Responsibilities:**
- Calculate agent acceleration, deceleration, and top speed based on physical attributes
- Enforce momentum and inertia constraints on turning and direction changes
- Apply directional speed penalties (lateral, backward movement)
- Manage movement state transitions (idle, walking, jogging, sprinting, decelerating, stumbling)
- Apply fatigue modifiers to all movement parameters at 10Hz tactical heartbeat rate
- Define agent physical properties (mass, hitbox dimensions, strength rating) as interface for Collision System (Spec #3)
- Define animation data contract for Stage 1 rendering integration
- Provide deterministic movement simulation for replay and multiplayer support

### 1.2 What Is OUT of Scope

**Owned by Other Stage 0 Specifications:**
- Collision detection and spatial partitioning â†’ Spec #3: Collision System
- Collision *response* physics (momentum transfer, who falls, foul detection) â†’ Spec #3: Collision System
- Ball interaction (first touch, receiving, dribbling with ball at feet) â†’ Spec #4: First Touch Mechanics
- Goalkeeper-specific movement (diving, lateral shuffling, set positioning) â†’ Spec #11: Goalkeeper Mechanics
- Tactical decisions about *where* to move â†’ Spec #8: Decision Tree, Spec #12: Positioning AI
- Perception of surroundings â†’ Spec #7: Perception System

**Stage 1+ Deferrals:**
- Animation playback and blending (Stage 1) â€” this spec defines the data contract only
- Dribbling locomotion modifiers (Stage 1) â€” ball-at-feet reduces speed/agility
- Off-ball movement AI (Stage 1) â€” intelligent runs, decoy movements
- Surface degradation effects on movement (Stage 2) â€” muddy patches reduce traction

**Permanent Exclusions:**
- Individual muscle group simulation (performance prohibitive, no gameplay value)
- Clothing/kit physics affecting drag (negligible at player speeds)
- Shoe stud grip simulation (abstracted into surface friction modifier)
- Biomechanical injury susceptibility via kinetic profiles (see Section 7 â€” deferred to Stage 2+ when injury system exists)

### 1.3 Implementation Timeline

**Stage 0 (Year 1 â€” Current):**
- Core locomotion physics (acceleration, deceleration, top speed)
- Directional speed penalties (forward, lateral, backward)
- Turning radius and momentum constraints
- Movement state machine with hysteresis
- Fatigue integration (10Hz modifier updates)
- Agent physical property definitions (mass, hitbox, strength interface)
- Animation data contract (forward reference, not consumed)
- Unit and integration tests

**Stage 1 (Year 2):**
- Animation system consumes movement data contract
- Dribbling locomotion modifiers (speed reduction with ball)
- Visual feedback (lean angles, stride variation)
- Slow-motion replay movement interpolation

**Stage 2 (Year 3-4):**
- Per-tile surface traction modifiers (worn areas, wet patches)
- Kinetic profile system (heel-striker, forefoot, neutral â€” Master Vol 1, Section 2.2)
- Weather effects on movement (wet pitch, wind resistance)
- Altitude effects (reduced stamina recovery at elevation)

### 1.4 Dependencies

**Required (Stage 0):**

- **Collision System** (Spec #3)
  - This spec *defines* agent physical properties (mass, hitbox, strength)
  - Spec #3 *consumes* these properties for detection and response
  - Interface: `AgentPhysicalProperties` struct provided per agent per frame
  - Circular dependency note: both specs developed in parallel (Weeks 3-4); interface contract defined here, implementation in Spec #3

- **Fixed64 Math Library** (Spec #9) â€” Optional for Stage 0, required for Stage 5+ multiplayer
  - Provides deterministic fixed-point arithmetic
  - Fall back to standard floats acceptable for single-player prototype

- **Event System** (Spec #17)
  - Agent movement publishes: `AgentStumbleEvent`, `AgentSprintStartEvent`, `AgentFatigueThresholdEvent`
  - Match simulator and stats engine subscribe to events

**Consumed by Other Specs:**

- **Ball Physics** (Spec #1) â€” requires `Vector3 velocity`, `float mass`, `bool isGoalkeeper` at collision time (interface contract from Spec #1, Section 8.3.5)
- **Pass Mechanics** (Spec #5) â€” requires agent velocity for pass weighting
- **First Touch Mechanics** (Spec #4) â€” requires agent momentum and body orientation
- **Perception System** (Spec #7) â€” requires agent facing direction and movement state

**External (Unity):**
- `UnityEngine.Vector3` â€” 3D vector math
- `UnityEngine.Mathf` â€” Math utilities (clamp, lerp, sqrt)
- **NOT using:** Unity Physics engine, Unity CharacterController, Unity NavMesh (all custom implementation)

### 1.5 Relationship to Ball Physics Spec #1

Both specs operate at 60Hz within the same physics loop. Key architectural parallels to maintain consistency:

- State machine design: adopt same hysteresis pattern (separate ENTER/EXIT thresholds to prevent oscillation)
- Numerical safety: same `HasInvalidValues()` pattern with NaN/Infinity guards
- Clamping strategy: same MIN/MAX constant architecture
- Event logging: same ring buffer pattern for replay reconstruction
- Determinism: same approach to float vs Fixed64 fallback

---

## 2. REQUIREMENTS

### 2.1 Functional Requirements

**FR-1: State Management**
- System SHALL maintain agent movement state: position (Vector3), velocity (Vector3), facing direction (Vector2), current movement state (enum)
- System SHALL implement movement state machine: IDLE, WALKING, JOGGING, SPRINTING, DECELERATING, STUMBLING, GROUNDED (7 states)
- System SHALL transition between states based on physics conditions (speed thresholds, turn angle, fatigue level)
- System SHALL use hysteresis to prevent rapid oscillation between states (consistent with Ball Physics Spec #1 pattern)
- System SHALL prevent invalid transitions (e.g., GROUNDED â†’ SPRINTING without recovery period)
- System SHALL track facing direction independently from movement direction (agent can strafe)

**FR-2: Acceleration & Speed**
- Agent SHALL accelerate according to exponential curve: `a(t) = a_max Ã— (1 - e^(-kÃ—t))` where k is derived from Acceleration attribute
- Agent SHALL reach top speed within 2.5â€“3.5 seconds from standstill (attribute-dependent)
- Top speed SHALL be derived from Pace attribute with defined mapping function
- Top speed range SHALL be 7.5 m/s (low Pace) to 10.2 m/s (elite Pace), consistent with real professional footballer data
- System SHALL NOT allow instantaneous velocity changes; all speed changes go through acceleration/deceleration curves

**FR-3: Deceleration**
- System SHALL support two deceleration modes: CONTROLLED (gradual, intentional stop) and EMERGENCY (maximum braking force)
- Controlled deceleration SHALL bring agent to stop within 3.0â€“5.0 meters from sprint speed
- Emergency deceleration SHALL bring agent to stop within 2.5â€“3.5 meters but SHALL incur stumble risk if speed was above sprint threshold
- Deceleration rate SHALL be attribute-dependent (Agility, Balance)

**FR-4: Directional Movement**
- System SHALL apply speed multipliers based on movement direction relative to facing:
  - Forward (Â±30Â°): 1.0Ã— (full speed)
  - Lateral (Â±30Â° to Â±90Â°): 0.65â€“0.75Ã— (attribute-dependent, Agility scaling)
  - Backward (Â±90Â° to Â±180Â°): 0.45â€“0.55Ã— (attribute-dependent, Agility scaling)
- Directional multipliers SHALL apply to both top speed and acceleration rate
- System SHALL allow agents to update facing direction independently from movement vector (enabling jockeying, backward tracking)

**FR-5: Turning & Momentum**
- Turning radius SHALL increase with velocity: `r_min = vÂ² / (a_lateral_max Ã— agility_factor)`
- Agent SHALL NOT turn instantaneously at speeds above WALKING threshold
- Sharp turn attempts at high speed SHALL trigger deceleration or stumble risk
- Turn rate (degrees/second) SHALL be attribute-dependent (Agility primary, Balance secondary)
- System SHALL model lean angle during turns (data output for animation contract, not visually rendered in Stage 0)

**FR-6: Fatigue Integration (Dual-Energy Model)**
- System SHALL receive two energy pool values from 10Hz tactical heartbeat:
  - **Aerobic pool** (1.0 = fresh, 0.0 = spent): slow-draining match fitness
  - **Sprint reservoir** (1.0 = full, 0.0 = empty): fast-draining burst capacity that recovers during low-intensity states
- Sprint reservoir SHALL drain during SPRINTING and recover during IDLE, WALKING, and JOGGING states
- Sprint reservoir recovery rate SHALL be modulated by aerobic pool level (tired players recover sprint capacity slower)
- Aerobic pool SHALL apply a global movement modifier affecting top speed, acceleration, and turn rate
- Aerobic pool SHALL drain slowly throughout the match, scaled by Stamina attribute
- Sprint reservoir depletion below SPRINT_RESERVOIR_FLOOR (0.20) SHALL force exit from SPRINTING state
- Sprint reservoir must recover above SPRINT_RESERVOIR_REENTRY (0.35) before re-entering SPRINTING (hysteresis)
- Aerobic pool depletion below AEROBIC_JOG_FLOOR (0.15) SHALL force exit from JOGGING state
- Energy pool update frequency: every 100ms (10Hz), linearly interpolated between updates at 60Hz

**FR-7: Agent Physical Properties**
- System SHALL define and maintain per-agent physical properties: mass (kg), hitbox dimensions (capsule radius + height), strength rating (1â€“20)
- Mass range: 72.5â€“100 kg (derived from Strength attribute, see Section 3.5.4.2)
- Hitbox: capsule collider with radius 0.35â€“0.50m, height 1.65â€“1.95m (attribute-derived)
- Physical properties SHALL be exposed via `AgentPhysicalProperties` struct for Collision System (Spec #3)

**FR-8: Animation Data Contract**
- System SHALL output per-frame animation data: stride frequency (Hz), foot plant phase (0.0â€“1.0), lean angle (degrees), movement state
- This data is a forward contract â€” no system consumes it in Stage 0
- Data SHALL be computed as part of normal movement update (no additional CPU cost when unused)
- Contract SHALL be stable â€” fields may be added but never removed or renamed

**FR-9: Determinism**
- System SHALL produce bit-identical results given same input state and commands (if using Fixed64)
- System SHALL produce consistent results across Windows, Mac, and Linux platforms
- System SHALL support replay reconstruction from command log
- System SHALL maintain position accuracy over 90-minute simulation (cumulative drift < 5cm per agent)

### 2.2 Performance Requirements

**PR-1: Update Speed**
- All 20 outfield agent movement updates SHALL complete in <1.0ms total per frame at 60Hz
- Per-agent budget: <0.05ms (50 microseconds)
- Target: 0.7ms average for all 20 agents (measured via Unity Profiler)
- Budget: <6% of 16.7ms frame time
- Note: Goalkeeper movement (2 agents) budgeted separately in Spec #10

**PR-2: Memory Usage**
- `AgentMovementState` struct SHALL be â‰¤256 bytes per agent
- Total memory for 20 outfield agents: <5KB
- Zero heap allocations during `UpdateAgentMovement()` call
- Animation data contract output: â‰¤64 bytes per agent (included in above)
- Event logging: fixed-size ring buffer per agent, â‰¤1KB each

**PR-3: Numerical Accuracy**
- Position drift SHALL be <5cm per agent after 90 minutes (54,000 physics ticks)
- Velocity error SHALL be <0.05 m/s cumulative over match
- Facing direction error SHALL be <1Â° cumulative over match
- No accumulated floating-point errors causing agent position divergence from expected path

### 2.3 Quality Requirements

**QR-1: Realism**
- Sprint from standstill to top speed: 2.5â€“3.5 seconds (attribute-dependent), validated against GPS data from professional matches
- Top speed range: 7.5â€“10.2 m/s, matching real-world professional footballer performance data
- Controlled stop from sprint: 2.0â€“3.0 meters
- Turning circle at sprint speed: visually plausible, no "ice skating" or "tank turning"
- Backward movement noticeably slower than forward movement to observers
- Fatigued agents visibly slower than fresh agents (>15% speed reduction at exhaustion)

**QR-2: Stability**
- System SHALL NOT produce NaN or Infinity values under any input conditions
- System SHALL recover from invalid states without crashing
- System SHALL clamp extreme values to defined safety limits (MAX_SPEED, MAX_ACCELERATION, MAX_TURN_RATE)
- System SHALL handle edge cases gracefully (zero velocity commands, conflicting direction inputs, simultaneous state transitions)
- System SHALL handle 22 simultaneous agent updates without frame drops

**QR-3: Testability**
- All movement formulas SHALL have corresponding unit tests (>80% code coverage)
- All state transitions SHALL have integration tests
- All attribute boundary values (1 and 20) SHALL have dedicated tests
- All failure modes SHALL have regression tests
- Test suite SHALL complete in <15 seconds (enables rapid iteration)

**QR-4: Attribute Sensitivity**
- A Pace 20 agent SHALL be measurably faster than a Pace 10 agent in all movement modes
- An Agility 20 agent SHALL turn measurably sharper than an Agility 5 agent at equal speeds
- A Balance 20 agent SHALL stumble less frequently than a Balance 5 agent during emergency maneuvers
- Attribute effects SHALL be perceptible but not cartoonish â€” no attribute value produces physically impossible movement

### 2.4 Failure Modes & Recovery

**Critical failures requiring immediate recovery:**

- **Numerical Instability (NaN/Infinity)**
  - Detection: `HasInvalidValues()` check every frame (mirrors Ball Physics pattern)
  - Recovery: Restore `LastValidPosition`, set IDLE state, zero velocity
  - Logging: ERROR with full state snapshot including movement command that triggered failure

- **Ground Penetration**
  - Detection: `agent.Position.z < 0` during validation (agents operate on XY plane, Z is height â€” consistent with Ball Physics Spec #1)
  - Recovery: Clamp to ground level (`z = 0`), preserve XY velocity
  - Logging: WARNING

- **Runaway Velocity**
  - Detection: `speed > MAX_SPEED` (12 m/s â€” above any realistic sprint with fatigue buffer)
  - Recovery: Clamp magnitude, preserve direction
  - Logging: WARNING with velocity value and movement command

- **State Machine Oscillation**
  - Detection: >6 state changes per second for same agent
  - Recovery: Lock to current state for 500ms cooldown
  - Logging: WARNING with state transition history

- **Agent Overlap (Pre-Collision)**
  - Detection: Two agents occupying same position (distance < sum of hitbox radii)
  - Recovery: Flag for Collision System (Spec #3) to resolve; do NOT attempt position correction in movement system
  - Logging: INFO (normal gameplay occurrence, not an error)

- **Command Queue Overflow**
  - Detection: Movement command buffer exceeds capacity
  - Recovery: Drop oldest commands, execute most recent
  - Logging: WARNING with queue depth

**All failures trigger:**
1. Immediate state validation
2. Logging with context (position, velocity, state, command history)
3. Graceful degradation (simulation continues)
4. Debug visualization in editor builds (movement vector, state overlay, hitbox)

---

**End of Sections 1 & 2**

**Page Count:** ~10 pages  
**Next Section:** Section 3 â€” Core Formulas & Design