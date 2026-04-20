# Ball Physics Specification - Sections 1 & 2

**Created:** February 4, 2026, 12:15 AM PST  
**Version:** 1.4 (Revised)  
**Status:** Draft  
**Changes from v1.3:**
- Gap 3: Fixed Fixed64 spec number in §1.4 dependency: Spec #8 → Spec #9
- AUD-006 residual A: Fixed BallState struct size in §2.2 PR-2: 56 bytes → 64 bytes
- OBS-005: Added Stage 0 note to ring buffer requirement in §2.2 PR-2

**Changes from v1.2:**
- Corrected spec numbers: Pass Mechanics Spec #4 → #5, Shot Mechanics Spec #5 → #6 (per canonical numbering in PROGRESS.md)

---

## 1. PURPOSE & SCOPE

### 1.1 What This System Does

The Ball Physics System simulates realistic football movement in 2.5D space (full 3D physics with 2D top-down rendering). It is the foundation for all ball interactions in the match simulation.

**Core Responsibilities:**
- Calculate ball trajectory under aerodynamic forces (Magnus effect, drag, gravity)
- Manage ball state transitions (airborne, rolling, bouncing, stationary)
- Handle ground interactions (bounces, rolling friction, surface effects)
- Detect collisions (goal posts, pitch boundaries)
- Provide deterministic simulation for replay and multiplayer support
- Log ball events for replay reconstruction

### 1.2 What Is OUT of Scope

**Stage 0 Exclusions (Implemented Later):**
- Agent-to-ball interaction (kicks, headers, touches) Ã¢â€ â€™ Covered in Pass Mechanics Spec #5, Shot Mechanics Spec #6, etc.
- Match orchestration and game flow Ã¢â€ â€™ Covered in Match Simulator spec (Stage 0)
- UI and visual rendering Ã¢â€ â€™ Covered in 2D Rendering Spec (Stage 1)
- Weather system integration Ã¢â€ â€™ Stage 2
- Multiple surface types per pitch Ã¢â€ â€™ Stage 2 (pitch degradation)

**Permanent Exclusions (Never Implemented):**
- Ball deformation physics (visual only, negligible gameplay impact)
- Ball wear/degradation during match (high complexity, minimal value)
- Individual grass blade simulation (performance prohibitive)
- Ball seam aerodynamics (panel geometry effects too subtle)

### 1.3 Implementation Timeline

**Stage 0 (Year 1 - Current):**
- Core formulas (Magnus, drag, gravity, bounce)
- State machine with validation
- Single global surface type
- Event logging for replay
- Unit and integration tests

**Stage 1 (Year 2):**
- Wind velocity integration (parameter already present)
- Visual rendering (shadows, trails, height indicators)
- Slow-motion replay support

**Stage 2 (Year 3-4):**
- Per-position surface queries (worn goal mouths, wet patches)
- Weather effects (rain reduces friction, wind affects trajectory)
- Altitude/temperature effects (air density variation)

### 1.4 Dependencies

**Required (Stage 0):**
- **Fixed64 Math Library** (Spec #9) - Optional for Stage 0, required for Stage 5+ multiplayer
  - Provides deterministic fixed-point arithmetic
  - Fall back to standard floats acceptable for single-player prototype
  
- **Event System** (Spec #17)
  - Ball physics publishes: `GoalScoredEvent`, `BallOutOfPlayEvent`, `BounceEvent`
  - Match simulator subscribes to events

**External (Unity):**
- `UnityEngine.Vector3` - 3D vector math
- `UnityEngine.Mathf` - Math utilities (clamp, lerp, sqrt)
- **NOT using:** Unity Physics engine (custom implementation)

**Future Dependencies (Stage 1+):**
- `WeatherSystem` - Wind velocity input
- `PitchConditionSystem` - Per-position surface queries

---

## 2. REQUIREMENTS

### 2.1 Functional Requirements

**FR-1: State Management**
- System SHALL maintain ball state: position (Vector3), velocity (Vector3), angular velocity (Vector3)
- System SHALL implement state machine: STATIONARY, ROLLING, AIRBORNE, BOUNCING, CONTROLLED, OUT_OF_PLAY
- System SHALL transition between states based on physics conditions (height, velocity, ground contact)
- System SHALL use hysteresis to prevent rapid oscillation between states
- System SHALL prevent invalid transitions (e.g., STATIONARY Ã¢â€ â€™ AIRBORNE without external force)

**FR-2: Force Application**
- Ball SHALL remain stationary when in STATIONARY, CONTROLLED, or OUT_OF_PLAY states
- Ball SHALL experience gravity, drag, and Magnus forces when AIRBORNE
- Ball SHALL experience drag and rolling friction when ROLLING
- Magnus effect SHALL apply only when spin rate > 0.1 rad/s

**FR-3: Ground Interaction**
- System SHALL detect ground contact when ball center height Ã¢â€°Â¤ 0.13m (exit threshold)
- System SHALL trigger BOUNCING state when airborne ball contacts ground
- System SHALL apply impulse-based bounce with surface-specific coefficient of restitution
- System SHALL transfer spin to linear velocity via contact point mechanics
- System SHALL prevent ground penetration (ball center height < RADIUS)

**FR-4: Collision Detection**
- System SHALL detect collisions with goal posts (cylindrical geometry, 0.12m diameter)
- System SHALL detect pitch boundary violations (ball entirely crosses line)
- System SHALL distinguish between goal scoring and goal kick/corner scenarios
- System SHALL provide collision normal vector and contact point for deflections

**FR-5: Determinism**
- System SHALL produce bit-identical results given same input seed (if using Fixed64)
- System SHALL produce consistent results across Windows, Mac, and Linux platforms
- System SHALL support replay reconstruction from event log
- System SHALL maintain accuracy over 90-minute simulation (position error < 1cm)

### 2.2 Performance Requirements

**PR-1: Update Speed**
- Ball physics update SHALL complete in <0.5ms per frame at 60Hz
- Target: 0.3ms average (measured via Unity Profiler)
- Budget: <3% of 16.7ms frame time
- Worst-case: Multiple force calculations + bounce = 0.5ms maximum

**PR-2: Memory Usage**
- `BallState` struct SHALL be Ã¢â€°Â¤128 bytes (current: 64 bytes)
- Zero heap allocations during `UpdateBallPhysics()` call
- Event logger SHALL use fixed-size ring buffer (bounded memory growth)
  - **Stage 0 note:** Implementation uses `List<BallEvent>` (see §3.1.13); bounded by match duration, acceptable for single-player prototype. Ring buffer required at Stage 1+ when concurrent logging is introduced.
- Total memory per ball: <200 bytes (state + logging overhead)

**PR-3: Numerical Accuracy**
- Position drift SHALL be <1cm after 90 minutes (54,000 physics ticks)
- Velocity error SHALL be <0.1 m/s cumulative over match
- Energy conservation SHALL be within 5% for goal post collisions only
- No accumulated floating-point errors causing divergence

### 2.3 Quality Requirements

**QR-1: Realism**
- Physics behavior SHALL match derived test specifications in Section 3.1.14:
  - Free kick curve (25m distance, expected 1.5-3.0m lateral deviation)
  - Bounce height (2m drop on dry grass, expected 0.80-0.90m rebound)
  - Rolling distance (10 m/s initial velocity, expected 26-31m on dry grass)
- Spin decay rate SHALL match observed professional footage
- Visual trajectory comparison to real matches: >90% observer agreement on "feels realistic"

**QR-2: Stability**
- System SHALL NOT produce NaN or Infinity values under any input conditions
- System SHALL recover from invalid states without crashing
- System SHALL clamp extreme values to defined safety limits (MAX_VELOCITY, MAX_SPIN, MAX_HEIGHT)
- System SHALL handle edge cases gracefully (zero velocity, zero spin, simultaneous collisions)

**QR-3: Testability**
- All physics formulas SHALL have corresponding unit tests (>80% code coverage)
- All state transitions SHALL have integration tests
- All failure modes SHALL have regression tests
- Test suite SHALL complete in <10 seconds (enables rapid iteration)

### 2.4 Failure Modes & Recovery

**Critical failures requiring immediate recovery:**

- **Numerical Instability (NaN/Infinity)**
  - Detection: `HasInvalidValues()` check every frame
  - Recovery: Restore `LastValidPosition`, set `STATIONARY` state, zero velocities
  - Logging: ERROR with full state snapshot

- **Ground Penetration**
  - Detection: `ball.Position.z < RADIUS` during validation
  - Recovery: Clamp to ground level (`z = RADIUS`), zero downward velocity
  - Logging: WARNING

- **Runaway Velocity**
  - Detection: `speed > MAX_VELOCITY` (50 m/s)
  - Recovery: Clamp magnitude, preserve direction
  - Logging: WARNING with velocity value

- **Stuck in Invalid State**
  - Detection: >10 state changes per second
  - Recovery: Force to `STATIONARY`, reset physics
  - Logging: ERROR with state transition history

- **Out of Bounds Without Detection**
  - Detection: Position beyond `PITCH_BUFFER` (20m safety zone)
  - Recovery: Clamp to buffer boundary, set `OUT_OF_PLAY`
  - Logging: WARNING

**All failures trigger:**
1. Immediate state validation
2. Logging with context (position, velocity, state history)
3. Graceful degradation (simulation continues)
4. Debug visualization in editor builds

---

**End of Sections 1 & 2**

**Page Count:** ~6.5 pages  
**Next Section:** Section 4 - Implementation Details
