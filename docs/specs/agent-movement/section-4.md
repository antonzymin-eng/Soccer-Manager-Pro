# Agent Movement Specification â€” Section 4: Implementation Details

**Purpose:** Defines the code organization, file structure, dependencies, configuration management, execution order, memory layout, and profiling strategy for the Agent Movement System. This section translates the physics models from Sections 3.1â€“3.6 into an implementable architecture. All file names, class responsibilities, and execution sequences defined here are binding for Stage 0 implementation.

**Created:** February 13, 2026, 11:45 PM PST  
**Updated:** February 14, 2026, 12:15 AM PST  
**Version:** 1.1 (Revised)  
**Status:** Draft  
**Dependencies:** Section 3.1 (State Machine), Section 3.2 (Locomotion), Section 3.3 (Directional Movement), Section 3.4 (Turning & Momentum), Section 3.5 (Data Structures), Section 3.6 (Edge Cases & Error Handling), Ball Physics Spec #1 Section 4 (architectural precedent)

---

## v1.1 Changelog

**CRITICAL FIXES:**

1. **Namespace inconsistency with Ball Physics (Issue #1):** Changed from `TacticalDirector.Physics.Agent` â†’ `TacticalDirector.Core.Physics.Agent` to match Ball Physics Spec #1's established `TacticalDirector.Core.Physics.Ball` convention. Common namespace also updated.

2. **PerformanceContext.InvalidateCache() phantom method (Issue #2):** Removed references to `InvalidateCache()` which does not exist on the PerformanceContext struct (Section 3.5.6). PerformanceContext is a plain value type â€” `EvaluateAttribute()` multiplies raw attribute by the three modifier floats with no caching layer. The 10Hz heartbeat simply overwrites the `FatigueModifier` float directly. Section 4.4.3 rewritten to reflect actual struct semantics.

3. **Missing Match Simulator integration point (Issue #3):** Added Section 4.4.4 with concrete `UpdateAllAgents()` entry point signature and Match Simulator call site example, following Ball Physics Section 4.4.1 pattern. Shows goalkeeper skip logic, tactical tick coordination, and frame-level system ordering.

**MODERATE FIXES:**

4. **Telemetry event size undercount (Issue #4):** MovementTelemetryEvent actual size is ~96 bytes (9 fields including embedded MovementCommand), not ~24 bytes (4 fields). Section 3.5.7's inline estimate counted only 4 of 9 fields. Corrected all downstream calculations: per-agent buffer ~93.75 KB (was ~23.4 KB), total footprint ~2.07 MB (was ~521 KB). Flagged Section 3.5.7 discrepancy for correction. Added design concern note about embedded Command struct's string reference creating GC pressure.

5. **WALK_ENTER_THRESHOLD missing from constants table (Issue #5):** Added to Section 4.3.1 state machine constants table with note that it equals IDLE_EXIT_THRESHOLD (0.3 m/s).

6. **Stumble RNG ownership unclear (Issue #6):** Added Step 6b to pipeline showing that the main loop owns the RNG roll between Steps 6 and 7. AgentTurning returns probability only (pure function). If roll succeeds, a pending stumble flag is set and consumed at Step 2 on the next frame. Current frame continues with unchanged state.

7. **Deceleration mode missing from pipeline (Issue #7):** Step 4 now shows `cmd.DecelerationMode` as a parameter to `CalculateAcceleration()` with inline note that the function handles both acceleration (exponential approach) and deceleration (constant-force braking, mode-dependent) cases.

**MINOR FIXES:**

8. **PlayerAttributes size hedge (Issue #8):** Committed to 60 bytes (15 ints Ã— 4B) as Stage 0 reality. Section 3.5's ~80B estimate noted separately as forward projection including future attribute fields and alignment. Per-agent total pinned at ~240 bytes.

9. **Editor-vs-release telemetry behavior (Issue #9):** Added note in Section 4.5.2 documenting that release builds log state transitions only (~5â€“20 events/agent/minute) vs. editor's full 60Hz capture. Effective release-mode telemetry footprint is under 50 KB total.

10. **Validation check #5 deceleration ordering (Issue #10):** Expanded from single cross-mode check to three checks: min < max within each mode, plus emergency max < controlled min (emergency stops are always shorter).

---

## 4. IMPLEMENTATION DETAILS

### 4.1 Code Organization

#### 4.1.1 File Structure

```
Core/Physics/Agent/
â”œâ”€â”€ AgentMovementState.cs          // Enum + GroundedReason enum (Section 3.1)
â”œâ”€â”€ AgentStateMachine.cs           // EvaluateState(), all transition logic (Section 3.1)
â”‚   â”œâ”€â”€ EvaluateState()            // Pure function: current state + speed â†’ new state
â”‚   â”œâ”€â”€ IsTransitionAllowed()      // Forbidden transition checks (Section 3.1.4)
â”‚   â”œâ”€â”€ CheckStaminaGates()        // Sprint reservoir / aerobic pool gates
â”‚   â””â”€â”€ CheckDwellTime()           // Minimum dwell time enforcement (Section 3.1.5)
â”œâ”€â”€ AgentLocomotion.cs             // Acceleration, top speed, deceleration (Section 3.2)
â”‚   â”œâ”€â”€ CalculateAcceleration()    // Exponential approach: a(t) = k Ã— (v_target âˆ’ v)
â”‚   â”œâ”€â”€ CalculateTopSpeed()        // Linear interpolation: Pace 1â€“20 â†’ 7.5â€“10.2 m/s
â”‚   â”œâ”€â”€ CalculateDeceleration()    // Constant-force braking (controlled / emergency)
â”‚   â””â”€â”€ ApplyFatigueModifiers()    // Route through PerformanceContext
â”œâ”€â”€ AgentDirectionalMovement.cs    // Zone multipliers, facing modes (Section 3.3)
â”‚   â”œâ”€â”€ CalculateZoneMultiplier()  // Forward/lateral/backward speed penalty
â”‚   â”œâ”€â”€ DetermineMovementZone()    // Angle between velocity and facing â†’ zone
â”‚   â””â”€â”€ UpdateFacingDirection()    // AUTO_ALIGN / TARGET_LOCK facing logic
â”œâ”€â”€ AgentTurning.cs                // Turn rate, min radius, lean, stumble risk (Section 3.4)
â”‚   â”œâ”€â”€ CalculateTurnRate()        // Hyperbolic decay: Ï‰ = 720 / (1 + k_turn Ã— speed)
â”‚   â”œâ”€â”€ CalculateMinimumRadius()   // r_min from centripetal force limits
â”‚   â”œâ”€â”€ CalculateLeanAngle()       // Visual lean from centripetal acceleration
â”‚   â””â”€â”€ EvaluateStumbleRisk()      // Probability check against safe fraction threshold
â”œâ”€â”€ AgentMovementConstants.cs      // All constants organized by subsystem (Section 3.5)
â”œâ”€â”€ Agent.cs                       // Main data container + cached properties (Section 3.5)
â”œâ”€â”€ MovementCommand.cs             // Command struct + factory methods (Section 3.5)
â”œâ”€â”€ PerformanceContext.cs          // Attribute gateway struct (Section 3.2.1)
â”œâ”€â”€ AgentPhysicalProperties.cs     // Collision interface struct (Section 3.5.4)
â”œâ”€â”€ AnimationDataContract.cs       // Stage 1 forward reference struct (Section 3.5.5)
â”œâ”€â”€ AgentSafetySystem.cs           // NaN recovery, clamping, boundary enforcement (Section 3.6)
â”‚   â”œâ”€â”€ ValidateAgentState()       // NaN/Infinity detection and recovery
â”‚   â”œâ”€â”€ ApplySafetyClamps()        // Speed, acceleration, turn rate ceilings
â”‚   â”œâ”€â”€ EnforcePitchBoundaries()   // Position clamping to pitch dimensions
â”‚   â””â”€â”€ CheckOscillation()         // State change frequency limiter
â”œâ”€â”€ AgentEventLogger.cs            // Ring buffer logging, MessageCode enum (Section 3.5.7)
â”‚   â”œâ”€â”€ TryLogSnapshot()           // Conditional per-frame telemetry capture
â”‚   â””â”€â”€ LogStateTransition()       // State change event recording
â””â”€â”€ PitchConfiguration.cs          // Pitch dimensions struct (Section 3.6.5)

Tests/Physics/Agent/
â”œâ”€â”€ StateMachineTests.cs           // UT-SM-001 through UT-SM-012 (Section 3.7)
â”œâ”€â”€ LocomotionTests.cs             // UT-LOC-001 through UT-LOC-012 (Section 3.7)
â”œâ”€â”€ DirectionalTests.cs            // UT-DIR-001 through UT-DIR-005 (Section 3.7)
â”œâ”€â”€ TurningTests.cs                // UT-TRN-001 through UT-TRN-004 (Section 3.7)
â”œâ”€â”€ EdgeCaseTests.cs               // UT-EC-001 through UT-EC-019 (Section 3.7)
â”œâ”€â”€ IntegrationTests.cs            // IT-001 through IT-010 (Section 3.7)
â””â”€â”€ ValidationBenchmarks.cs        // VB-001 through VB-008 (Section 3.7)
```

**Design rationale:** The file structure mirrors Ball Physics Spec #1 (one file per logical subsystem) but requires more files because Agent Movement has four distinct physics subsystems (state machine, locomotion, directional movement, turning) versus Ball Physics' single update path. Each file targets under 500 lines to maintain readability.

**Shared infrastructure notes:**

PitchConfiguration.cs defines pitch dimensions used by both Agent Movement (boundary enforcement) and Ball Physics (out-of-bounds detection). In Stage 0, this file lives in `Core/Physics/Agent/` because Agent Movement defined it first. When Collision System (Spec #3) implementation begins, PitchConfiguration should migrate to the shared `Core/Physics/Common/` namespace to avoid duplication. This migration is tracked as a Stage 0 cleanup task, not a blocking dependency.

PerformanceContext.cs implements a project-wide singleton pattern for attribute evaluation. Other specs (Pass Mechanics #4, Shot Mechanics #5, First Touch #11) will reference this same struct for consistent attribute scaling. The struct is defined authoritatively in Agent Movement because movement physics was the first consumer, but its scope is project-wide.

#### 4.1.2 Namespace Convention

```csharp
namespace TacticalDirector.Core.Physics.Agent
{
    // All agent movement code: state machine, locomotion, directional,
    // turning, safety, logging, configuration
}

namespace TacticalDirector.Core.Physics.Agent.Tests
{
    // All test code: unit tests, integration tests, validation benchmarks
}

namespace TacticalDirector.Core.Physics.Common
{
    // Shared types used by multiple physics specs
    // Stage 0 candidates: PitchConfiguration, PerformanceContext
    // Populated as cross-spec dependencies are identified during implementation
}
```

This follows the Ball Physics precedent (`TacticalDirector.Core.Physics.Ball`) with the addition of a `Common` namespace for types that serve multiple specs. The `Common` namespace is created empty in Stage 0 and populated during the Collision System (Spec #3) implementation when shared types are identified.

#### 4.1.3 Class Responsibilities

**AgentStateMachine (Static class)**
- Pure function: receives current state, speed, stamina pools, timers â†’ returns new state
- No side effects, no state modification, no physics calculations
- All transition logic from Section 3.1 concentrated here
- Returns `AgentMovementState`, does not modify Agent directly

**AgentLocomotion (Static class)**
- Acceleration, top speed, and deceleration calculations
- All methods static, side-effect free
- Routes all attribute reads through PerformanceContext (enforced by code review)
- Does NOT perform velocity integration â€” returns acceleration vector only

**AgentDirectionalMovement (Static class)**
- Zone classification (forward, lateral, backward, transition) from angle between facing and velocity
- Speed multiplier lookup from zone + Agility attribute
- Facing direction updates (AUTO_ALIGN and TARGET_LOCK modes)

**AgentTurning (Static class)**
- Turn rate calculation via hyperbolic decay model
- Minimum turning radius computation
- Lean angle output for animation
- Stumble risk probability evaluation (returns probability, does not roll dice â€” caller handles RNG)

**AgentSafetySystem (Static class)**
- Post-integration validation: NaN/Infinity detection, speed clamping, boundary enforcement
- Oscillation detection and state lock management
- Recovery procedures when validation fails
- Calls into AgentEventLogger for error reporting

**Agent (Instance class)**
- Data container for all per-agent movement state
- Cached properties (Speed, PhysicalProperties, AnimationData)
- No physics logic â€” all computation happens in the static classes above
- Provides read interface for external systems (Collision, Perception, Tactical AI)

**AgentEventLogger (Instance class)**
- Ring buffer management (1000 events per agent)
- MessageCode enum for zero-allocation logging
- Conditional snapshot capture (every frame in debug, state transitions only in release)
- One instance per Agent, passed by reference to static methods that need logging

#### 4.1.4 Class Dependency Graph

```
AgentStateMachine
    â”œâ”€> AgentMovementState (enum, data only)
    â””â”€> AgentMovementConstants (constants, read only)

AgentLocomotion
    â”œâ”€> AgentMovementConstants
    â”œâ”€> PerformanceContext (attribute evaluation)
    â””â”€> AgentDirectionalMovement (zone multiplier query)

AgentDirectionalMovement
    â”œâ”€> AgentMovementConstants
    â””â”€> PerformanceContext

AgentTurning
    â”œâ”€> AgentMovementConstants
    â””â”€> PerformanceContext

AgentSafetySystem
    â”œâ”€> AgentMovementConstants
    â”œâ”€> PitchConfiguration (boundary limits)
    â””â”€> AgentEventLogger (optional, for error reporting)

Agent
    â”œâ”€> AgentMovementState (enum)
    â”œâ”€> PlayerAttributes (struct, readonly)
    â”œâ”€> PerformanceContext (struct)
    â”œâ”€> AgentPhysicalProperties (computed property)
    â”œâ”€> AnimationDataContract (computed property)
    â””â”€> AgentEventLogger (instance reference)
```

**Dependency rules:**
- No circular dependencies between any classes
- AgentEventLogger is always optional â€” all call sites use null-safe patterns
- AgentMovementConstants has zero imports (leaf node)
- Data structs (MovementCommand, AgentPhysicalProperties, AnimationDataContract) have zero dependencies
- Static computation classes never reference Agent directly â€” they accept primitives and structs as parameters to maintain testability

### 4.2 Dependencies

#### 4.2.1 Systems This Depends On

| System | Interface | What We Need | Frequency |
|--------|-----------|-------------|-----------|
| Physics Loop | Frame tick | deltaTime (float), frame counter (int) | 60Hz (every frame) |
| Tactical Heartbeat | 10Hz signal | Trigger for fatigue modifier refresh | Every 6th frame |
| AI Command Layer | MovementCommand | Target position, desired state, facing mode | Every frame |

Agent Movement has **no runtime dependencies on other Stage 0 specifications**. It receives commands from future AI systems (Spec #7 Tactical Brain) and provides data to downstream consumers, but executes independently. This architectural isolation matches Ball Physics Spec #1, which also operates as a self-contained system.

The 60Hz physics loop and 10Hz tactical heartbeat are infrastructure services provided by the Match Simulator (Spec #17). Agent Movement does not create or manage these timers â€” it responds to them.

#### 4.2.2 Systems That Depend On This

| Consumer | What They Read | Interface Contract | Spec |
|----------|---------------|-------------------|------|
| Collision System | Position, Velocity, Mass, HitboxRadius, Strength, IsGrounded | `AgentPhysicalProperties` struct (Section 3.5.4) | Spec #3 |
| Ball Physics | Agent velocity, mass, isGoalkeeper | At collision time via AgentPhysicalProperties | Spec #1 Â§3.1.10.2 |
| Pass Mechanics | Agent velocity, facing direction | For pass weighting and accuracy | Spec #4 |
| Shot Mechanics | Agent velocity, facing direction, balance | For shot power and accuracy | Spec #5 |
| First Touch | Agent momentum, body orientation | At ball contact for control quality | Spec #11 |
| Perception System | Facing direction, movement state | For awareness cone calculation | Spec #6 |
| Tactical Brain | Movement state, speed, position | For decision-making inputs | Spec #7 |

The `AgentPhysicalProperties` struct (Section 3.5.4) is the primary outbound interface. It is a computed property on the Agent class, regenerated on access â€” not cached across frames. This ensures consumers always read current-frame data without stale cache risk.

Ball Physics Spec #1 Section 8.3.5 already documents this dependency from the consumer side. Both specs agree on the interface contract.

#### 4.2.3 External Dependencies (Unity)

**Required:**
```csharp
using UnityEngine;  // Vector3, Vector2, Mathf (Clamp, Lerp, Sqrt, Exp, Pow, Atan2)
```

**Editor-only (conditional compilation):**
```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.Profiling;  // Profiler.BeginSample / EndSample
#endif
```

**Explicitly NOT used:**
- `UnityEngine.Physics` â€” all physics is custom implementation
- `UnityEngine.CharacterController` â€” movement is custom velocity integration
- `UnityEngine.AI.NavMesh` â€” pathfinding is handled by Tactical Brain (Spec #7)
- `UnityEngine.Rigidbody` â€” no Unity physics bodies on agents
- `UnityEngine.Collider` â€” collision detection is custom (Spec #3)

This matches Ball Physics Spec #1's policy of custom physics with minimal Unity dependencies. The only Unity imports are math utilities (`Vector3`, `Mathf`) and conditional profiling hooks.

#### 4.2.4 Optional Dependencies

**Fixed64 Math Library (Spec #8):**
- Stage 0: Use standard `float` (32-bit IEEE 754). Acceptable for single-player prototype where frame-perfect determinism is not required.
- Stage 5+: Replace with Fixed64 for multiplayer determinism. Migration path defined in Section 4.7.

**Event System (Spec #17):**
- Stage 0: AgentEventLogger writes to local ring buffer only. No cross-system event publishing.
- Stage 1+: State transition events (sprint start, stumble, grounded) publish to EventBus for UI notifications, statistics tracking, and replay capture. Publishing contract mirrors Ball Physics Section 4.4.2 pattern.

### 4.3 Configuration

#### 4.3.1 Tunable Constants Summary

All tunable constants live in `AgentMovementConstants.cs` (Section 3.5.6). The table below consolidates every configurable value by subsystem with its authoritative source section. Values marked **[FIXED]** are derived from FIFA regulations or biomechanical constraints and should not be changed without re-deriving dependent formulas. Values marked **[TUNABLE]** can be adjusted during playtesting within documented ranges.

**State Machine Constants (Section 3.1 â€” authoritative):**

| Constant | Value | Unit | Category |
|----------|-------|------|----------|
| IDLE_ENTER_THRESHOLD | 0.1 | m/s | [TUNABLE] |
| IDLE_EXIT_THRESHOLD | 0.3 | m/s | [TUNABLE] |
| WALK_ENTER_THRESHOLD | 0.3 | m/s | [TUNABLE] â€” equals IDLE_EXIT |
| JOG_ENTER_THRESHOLD | 2.2 | m/s | [TUNABLE] |
| JOG_EXIT_THRESHOLD | 1.9 | m/s | [TUNABLE] |
| SPRINT_ENTER_THRESHOLD | 5.8 | m/s | [TUNABLE] |
| SPRINT_EXIT_THRESHOLD | 5.2 | m/s | [TUNABLE] |
| STUMBLE_MIN_DURATION_BALANCE_1 | 0.8 | s | [TUNABLE] |
| STUMBLE_MIN_DURATION_BALANCE_20 | 0.3 | s | [TUNABLE] |
| GROUNDED_BASE_DWELL | 1.0 | s | [TUNABLE] |
| GROUNDED_MIN_DWELL | 0.6 | s | [FIXED] |
| GROUNDED_MAX_DWELL | 2.5 | s | [FIXED] |
| SPRINT_RESERVOIR_REENTRY | 0.20 | ratio | [TUNABLE] |
| AEROBIC_JOG_FLOOR | 0.15 | ratio | [TUNABLE] |

**Locomotion Constants (Section 3.2 â€” authoritative):**

| Constant | Value | Unit | Category |
|----------|-------|------|----------|
| TOP_SPEED_MIN | 7.5 | m/s | [FIXED] â€” Pace 1 floor |
| TOP_SPEED_MAX | 10.2 | m/s | [FIXED] â€” Pace 20 ceiling |
| ACCEL_K_MIN | 0.658 | sâ»Â¹ | [FIXED] â€” derived from Tâ‚‰â‚€ = 3.5s |
| ACCEL_K_MAX | 0.921 | sâ»Â¹ | [FIXED] â€” derived from Tâ‚‰â‚€ = 2.5s |
| ACCEL_TIME_MIN | 2.5 | s | [FIXED] â€” Acceleration 20 |
| ACCEL_TIME_MAX | 3.5 | s | [FIXED] â€” Acceleration 1 |
| CONTROLLED_DECEL_DIST_MIN | 3.0 | m | [TUNABLE] |
| CONTROLLED_DECEL_DIST_MAX | 5.0 | m | [TUNABLE] |
| EMERGENCY_DECEL_DIST_MIN | 2.5 | m | [TUNABLE] |
| EMERGENCY_DECEL_DIST_MAX | 3.5 | m | [TUNABLE] |

**Directional Movement Constants (Section 3.3 â€” authoritative):**

| Constant | Value | Unit | Category |
|----------|-------|------|----------|
| LATERAL_MULT_MIN | 0.65 | ratio | [TUNABLE] â€” Agility 1 |
| LATERAL_MULT_MAX | 0.75 | ratio | [TUNABLE] â€” Agility 20 |
| BACKWARD_MULT_MIN | 0.45 | ratio | [TUNABLE] â€” Agility 1 |
| BACKWARD_MULT_MAX | 0.55 | ratio | [TUNABLE] â€” Agility 20 |

**Turning Constants (Section 3.4 â€” authoritative):**

| Constant | Value | Unit | Category |
|----------|-------|------|----------|
| TURN_RATE_BASE | 720.0 | Â°/s | [FIXED] â€” all agents at zero speed |
| K_TURN_MIN | 0.35 | dimensionless | [FIXED] â€” Agility 20 |
| K_TURN_MAX | 0.78 | dimensionless | [FIXED] â€” Agility 1 |
| SAFE_FRACTION_MIN | 0.55 | ratio | [TUNABLE] â€” Balance 1 |
| SAFE_FRACTION_MAX | 0.85 | ratio | [TUNABLE] â€” Balance 20 |
| STUMBLE_PROB_MAX | 0.30 | per-frame | [TUNABLE] â€” Balance 1 |
| STUMBLE_PROB_MIN | 0.05 | per-frame | [TUNABLE] â€” Balance 20 |
| STUMBLE_SPEED_THRESHOLD | 2.2 | m/s | [FIXED] â€” equals JOG_ENTER |
| MAX_LEAN_ANGLE | 45 | degrees | [TUNABLE] â€” visual only |

**Safety Constants (Section 3.6 â€” authoritative):**

| Constant | Value | Unit | Category |
|----------|-------|------|----------|
| MAX_SPEED | 12.0 | m/s | [FIXED] â€” absolute physics clamp |
| MAX_ACCELERATION | 8.0 | m/sÂ² | [FIXED] â€” above max attribute-derived value |
| MAX_TURN_RATE | 720.0 | Â°/s | [FIXED] â€” equals TURN_RATE_BASE |
| MIN_VELOCITY_MAGNITUDE | 0.001 | m/s | [FIXED] â€” numerical drift floor |
| MAX_STATE_CHANGES_PER_SECOND | 6 | count | [TUNABLE] |
| STATE_LOCK_DURATION | 0.5 | s | [TUNABLE] |

**Cross-spec shared constants:**

| Constant | Value | Shared With | Notes |
|----------|-------|-------------|-------|
| Attribute range | 1â€“20 | All specs | Project-wide convention (Section 2.1) |
| Physics timestep | 1/60 s | Ball Physics, Collision | 60Hz shared frame rate |
| Tactical heartbeat | 1/10 s | Fatigue (Spec #13) | 10Hz modifier refresh |

**Runtime vs. compile-time classification:**

All constants in Stage 0 are compile-time (`const` or `static readonly`). No external configuration files, no runtime reloading. Recompilation is required for any change. This is intentional â€” runtime tuning adds complexity that is unnecessary for the specification validation phase. Stage 1 may introduce a ScriptableObject-based configuration system for designer-accessible tuning, but the constants and their valid ranges are defined here.

#### 4.3.2 Configuration Validation

At initialization (match start), the system validates all constants are within sane ranges. This catches accidental corruption from bad merges or typos during tuning.

**Validation checks (run once at startup):**

1. All speed thresholds are positive and ordered correctly: `IDLE_ENTER < IDLE_EXIT < JOG_EXIT < JOG_ENTER < SPRINT_EXIT < SPRINT_ENTER < MAX_SPEED`
2. Hysteresis gaps are positive: `JOG_ENTER - JOG_EXIT > 0`, `SPRINT_ENTER - SPRINT_EXIT > 0`
3. Acceleration constants are positive: `ACCEL_K_MIN > 0`, `ACCEL_K_MAX > ACCEL_K_MIN`
4. Top speed range is valid: `TOP_SPEED_MIN > 0`, `TOP_SPEED_MAX > TOP_SPEED_MIN`
5. Deceleration distances are ordered correctly: `EMERGENCY_DECEL_DIST_MIN < EMERGENCY_DECEL_DIST_MAX`, `CONTROLLED_DECEL_DIST_MIN < CONTROLLED_DECEL_DIST_MAX`, and emergency max < controlled min (emergency stops are always shorter than controlled stops)
6. All multipliers are in (0, 1]: lateral, backward
7. Turn rate base is positive, k_turn values are positive
8. Stumble probabilities are in [0, 1] and ordered: `STUMBLE_PROB_MIN < STUMBLE_PROB_MAX`
9. Safety limits exceed all attribute-derived maximums
10. PitchConfiguration dimensions are within FIFA range: length [90, 120]m, width [45, 90]m

**Failure behavior:** Validation errors log `Debug.LogError()` with the specific violated constraint and the actual value. In editor builds, validation failures pause the simulation. In release builds, the simulation continues with the invalid values (to avoid hard crashes) but the error is flagged in telemetry.

This matches the Ball Physics Spec #1 Section 4.3.2 pattern: validate on initialization, log warnings, never silently accept invalid configuration.

#### 4.3.3 Tuning Workflow

Identical to Ball Physics Spec #1 Section 4.3.2 tuning workflow:

1. **Record Baseline** â€” Run integration test suite (Section 3.7 IT-001 through IT-010). Record all test metrics. Commit baseline to Git.

2. **Adjust Single Parameter** â€” Modify one constant in `AgentMovementConstants.cs`. Document the reason in a code comment adjacent to the value.

3. **Retest** â€” Run full test suite (68 unit tests + 10 integration tests + 8 validation benchmarks). Compare results to baseline. Verify no regressions outside tolerance (tolerances defined in Section 3.7, consolidated in Appendix D).

4. **Visual Validation** â€” Run match simulation. Observe agent movement in debug visualization. Verify the change produces the intended effect without introducing unnatural behavior.

5. **Commit or Revert** â€” If improvement: commit with detailed explanation. If regression or no clear benefit: revert.

**Tuning log format (in code comments):**
```csharp
// 2026-XX-XX: Changed SPRINT_ENTER_THRESHOLD from 5.8 to 6.0
//             Reason: Agents entering sprint too easily during jogging
//             Result: Sprint frequency reduced ~15%, more natural pacing
//             Test impact: UT-SM-008 tolerance adjusted from Â±0.3 to Â±0.5
//             Approved by: Lead Developer
public const float SPRINT_ENTER_THRESHOLD = 6.0f;
```

### 4.4 Execution Order

This is the critical "how it all fits together" section. Unlike Ball Physics (single entity, single update path), Agent Movement processes 22 agents per frame across multiple subsystems with two different update frequencies.

#### 4.4.1 Per-Frame Pipeline (60Hz)

Every physics frame, for each of the 22 agents, the following steps execute in strict sequential order:

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚  AGENT MOVEMENT PER-FRAME PIPELINE (60Hz)                   â”‚
â”‚  Executed for each agent i âˆˆ [0, 21]                        â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                                             â”‚
â”‚  1. RECEIVE COMMAND                                         â”‚
â”‚     MovementCommand cmd = AILayer.GetCommand(agent[i])      â”‚
â”‚     If no command: use MovementCommand.Maintain()           â”‚
â”‚                                                             â”‚
â”‚  2. EVALUATE STATE                                          â”‚
â”‚     AgentMovementState newState =                           â”‚
â”‚       AgentStateMachine.EvaluateState(                      â”‚
â”‚         currentState, speed, sprintReservoir,               â”‚
â”‚         aerobicPool, timeInState, stateChanges)             â”‚
â”‚     Uses PREVIOUS frame's speed (no circular dependency)    â”‚
â”‚                                                             â”‚
â”‚  3. APPLY STATE TRANSITION (if state changed)               â”‚
â”‚     agent.PreviousState = agent.CurrentState                â”‚
â”‚     agent.CurrentState = newState                           â”‚
â”‚     agent.TimeInState = 0                                   â”‚
â”‚     agent.StateChangesThisSecond++                          â”‚
â”‚     AgentEventLogger.LogStateTransition(...)                â”‚
â”‚                                                             â”‚
â”‚  4. CALCULATE ACCELERATION (or deceleration)                â”‚
â”‚     Vector3 accel = AgentLocomotion.CalculateAcceleration(  â”‚
â”‚       currentState, speed, topSpeed, k_accel,               â”‚
â”‚       cmd.TargetDirection, cmd.DecelerationMode,            â”‚
â”‚       fatigueModifier)                                      â”‚
â”‚     Handles both cases internally:                          â”‚
â”‚       - If accelerating: exponential approach model         â”‚
â”‚       - If DECELERATING state: constant-force braking       â”‚
â”‚         using cmd.DecelerationMode (CONTROLLED/EMERGENCY)   â”‚
â”‚                                                             â”‚
â”‚  5. APPLY DIRECTIONAL PENALTY                               â”‚
â”‚     float zoneMult = AgentDirectionalMovement               â”‚
â”‚       .CalculateZoneMultiplier(                             â”‚
â”‚         velocity, facingDirection, agility)                  â”‚
â”‚     accel *= zoneMult                                       â”‚
â”‚                                                             â”‚
â”‚  6. CALCULATE TURN RATE                                     â”‚
â”‚     float turnRate = AgentTurning.CalculateTurnRate(        â”‚
â”‚       speed, k_turn)                                        â”‚
â”‚     float leanAngle = AgentTurning.CalculateLeanAngle(      â”‚
â”‚       speed, turnRate)                                      â”‚
â”‚     float stumbleProb = AgentTurning.EvaluateStumbleRisk(   â”‚
â”‚       turnRate, maxTurnRate, safeFraction, balanceProb)      â”‚
â”‚                                                             â”‚
â”‚  6b. STUMBLE RNG ROLL (if stumbleProb > 0)                  â”‚
â”‚      if (Random.value < stumbleProb)                        â”‚
â”‚        â†’ set pendingStumble flag on agent                   â”‚
â”‚        â†’ flag is consumed at Step 2 on NEXT frame           â”‚
â”‚        â†’ this frame continues with current state            â”‚
â”‚      RNG is owned by the main loop, not AgentTurning.       â”‚
â”‚      AgentTurning is a pure function â€” no RNG, no state.    â”‚
â”‚                                                             â”‚
â”‚  7. APPLY FACING UPDATE                                     â”‚
â”‚     AgentDirectionalMovement.UpdateFacingDirection(          â”‚
â”‚       ref facingDirection, cmd.FacingMode,                   â”‚
â”‚       cmd.FacingTarget, turnRate, dt)                       â”‚
â”‚                                                             â”‚
â”‚  8. INTEGRATE VELOCITY                                      â”‚
â”‚     velocity += accel Ã— dt                                  â”‚
â”‚     (Explicit Euler â€” same integrator as Ball Physics)      â”‚
â”‚                                                             â”‚
â”‚  9. INTEGRATE POSITION                                      â”‚
â”‚     position += velocity Ã— dt                               â”‚
â”‚                                                             â”‚
â”‚  10. SAFETY VALIDATION                                      â”‚
â”‚      AgentSafetySystem.ValidateAgentState(ref agent)        â”‚
â”‚      - NaN/Infinity â†’ recover from LastValidPosition        â”‚
â”‚      - Speed > MAX_SPEED â†’ clamp velocity magnitude         â”‚
â”‚      - Position outside pitch â†’ EnforcePitchBoundaries()    â”‚
â”‚      - Oscillation check â†’ lock state if threshold exceeded â”‚
â”‚                                                             â”‚
â”‚  11. UPDATE CACHES                                          â”‚
â”‚      agent.InvalidateSpeedCache()  // Force recalc          â”‚
â”‚      _ = agent.Speed               // Trigger cache refresh â”‚
â”‚      agent.LastValidPosition = agent.Position               â”‚
â”‚      agent.LastValidVelocity = agent.Velocity               â”‚
â”‚                                                             â”‚
â”‚  12. LOG TELEMETRY                                          â”‚
â”‚      AgentEventLogger.TryLogSnapshot(agent, matchTime)      â”‚
â”‚                                                             â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

**Critical ordering constraints:**

- Step 2 (state evaluation) uses previous frame's speed. The current frame's velocity is not yet integrated, so there is no circular dependency between state evaluation and velocity.
- Step 5 (directional penalty) must occur after Step 4 (acceleration) because it modifies the acceleration vector.
- Steps 8â€“9 (integration) must occur before Step 10 (safety validation) because safety operates on the new position/velocity.
- Step 10 (safety) must occur before Step 11 (cache update) to ensure cached values reflect validated state.
- Step 6 (stumble risk) returns a probability. If the caller rolls a stumble event, the state transition happens at Step 2 on the *next* frame, not the current frame. This one-frame delay is intentional â€” it prevents mid-pipeline state changes that would invalidate already-computed values.

#### 4.4.2 Agent Processing Order

All 22 agents are processed sequentially within each frame. The processing order is fixed (agent index 0 through 21) and does not change during a match. This is acceptable for Stage 0 because:

1. Agent Movement is self-contained â€” no agent reads another agent's movement state during its own update. Cross-agent interactions (collisions, spacing) are handled by Collision System (Spec #3) in a separate pass after all agents complete their movement update.
2. Fixed ordering guarantees determinism. Randomized or parallel processing would introduce ordering-dependent results.

**Frame structure (simplified):**
```
Frame N:
  1. All 22 agents: Movement update (this spec)
  2. Ball Physics update (Spec #1)
  3. Collision detection and resolution (Spec #3)
  4. Event processing (Spec #17)
```

Collision System runs after all movement updates complete, operating on the final positions and velocities of all 22 agents. This avoids the need for Agent Movement to know about other agents' positions.

#### 4.4.3 Tactical Heartbeat (10Hz)

Every 6th physics frame, an additional update occurs for all 22 agents:

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚  TACTICAL HEARTBEAT (10Hz â€” every 6th frame)                â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                                             â”‚
â”‚  13. REFRESH FATIGUE MODIFIERS                              â”‚
â”‚      For each agent i âˆˆ [0, 21]:                            â”‚
â”‚        agent[i].PerformanceContext.FatigueModifier =        â”‚
â”‚          FatigueSystem.GetCurrentModifier(agent[i])         â”‚
â”‚        (Stage 0: always returns 1.0)                        â”‚
â”‚                                                             â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

**60Hz / 10Hz interaction detail:**

This relationship requires precise documentation because it affects what values are "fresh" versus "stale" at any given frame.

**Updated at 10Hz (overwritten every 6th frame):** PerformanceContext modifier values â€” `FatigueModifier`, and in future stages: `FormModifier` (Stage 2+), `PsychologyModifier` (Stage 4+). Between updates, these float values are frozen at their last-written value. The 60Hz movement system reads the stored modifier directly via `EvaluateAttribute()`.

**Computed at 60Hz (every frame):** Acceleration, velocity, position, state transitions, turn rate, safety checks. These always use the current modifier values but recalculate physics every frame.

**Update mechanism:** The 10Hz heartbeat directly overwrites the modifier floats on the PerformanceContext struct. There is no cache invalidation layer â€” PerformanceContext is a plain value type (Section 3.5.6) where `EvaluateAttribute(rawAttribute)` simply returns `rawAttribute Ã— FormModifier Ã— FatigueModifier Ã— PsychologyModifier` with no intermediate caching. The 10Hz write and 60Hz reads are safe because all agent updates are sequential on the main thread (Section 4.4.2).

**Why 10Hz, not 60Hz:** Fatigue changes slowly â€” stamina pools deplete over minutes, not frames. Recalculating modifiers every frame wastes approximately 5 `Mathf.Exp()` calls per agent per frame (110 Exp calls/frame across 22 agents) for values that change by less than 0.001 between consecutive frames. The 10Hz rate provides adequate responsiveness (100ms latency, imperceptible to players) while eliminating approximately 540 unnecessary `Mathf.Exp()` calls per second.

**Frame alignment:** All 22 agents refresh on the same 10Hz tick, not staggered. This ensures all agents operate on the same fatigue snapshot within any given frame, preserving determinism. Staggered updates would create frame-dependent ordering artifacts where agent 0 uses a newer fatigue value than agent 21 within the same physics tick.

#### 4.4.4 Match Simulator Integration Point

The Match Simulator (Spec #17) calls into Agent Movement through a single entry point that encapsulates the full per-frame pipeline:

```csharp
/// <summary>
/// Updates all agents for one physics frame.
/// Called by MatchSimulator.Update() at 60Hz, after AI commands are issued
/// and before Ball Physics and Collision System updates.
///
/// Responsibilities:
///   - Iterates all 22 agents in fixed index order (0â€“21)
///   - Executes full pipeline (Steps 1â€“12) per agent
///   - On 10Hz tick frames: also executes fatigue refresh (Step 13)
///   - Skips goalkeeper agents (indices 0, 11) â€” they use Spec #10
///
/// Thread safety: Must be called on main simulation thread only.
/// Determinism: Produces identical results for identical inputs.
/// </summary>
public static void UpdateAllAgents(
    Agent[] agents,              // All 22 agent instances
    MovementCommand[] commands,  // One command per agent from AI layer
    float deltaTime,             // Frame time (1/60 s nominal)
    float matchTime,             // Current match time in seconds
    bool isTacticalTick,         // True every 6th frame (10Hz heartbeat)
    PitchConfiguration pitch     // Pitch boundary configuration
)
{
    // 10Hz heartbeat: refresh fatigue modifiers (Step 13)
    if (isTacticalTick)
    {
        for (int i = 0; i < agents.Length; i++)
        {
            if (agents[i].IsGoalkeeper) continue;
            // Stage 0: FatigueSystem.GetCurrentModifier() returns 1.0
            agents[i].PerformanceContext.FatigueModifier =
                FatigueSystem.GetCurrentModifier(agents[i]);
        }
    }

    // 60Hz: full movement pipeline for all agents (Steps 1â€“12)
    for (int i = 0; i < agents.Length; i++)
    {
        if (agents[i].IsGoalkeeper) continue;
        UpdateSingleAgent(ref agents[i], commands[i], deltaTime, matchTime, pitch);
    }
}
```

**Match Simulator call site:**
```csharp
public class MatchSimulator
{
    private Agent[] _agents;           // 22 agents
    private MovementCommand[] _cmds;   // AI-generated commands
    private PitchConfiguration _pitch;
    private int _frameCounter;

    public void PhysicsUpdate(float deltaTime, float matchTime)
    {
        bool isTacticalTick = (_frameCounter % 6 == 0);

        // 1. Agent Movement (this spec)
        AgentMovementSystem.UpdateAllAgents(
            _agents, _cmds, deltaTime, matchTime, isTacticalTick, _pitch);

        // 2. Ball Physics (Spec #1)
        BallPhysicsCore.UpdateBallPhysics(ref _ball, deltaTime, ...);

        // 3. Collision Detection & Resolution (Spec #3)
        CollisionSystem.ResolveAll(_agents, ref _ball, deltaTime);

        // 4. Event Processing (Spec #17)
        EventBus.ProcessPending();

        _frameCounter++;
    }
}
```

This mirrors Ball Physics Spec #1 Section 4.4.1's Match Simulator interface pattern. The key difference is that Agent Movement processes an array of 22 entities rather than a single ball struct.

### 4.5 Memory Layout

#### 4.5.1 Agent Instance Memory

The Agent class instance contains all per-agent state. Memory estimate derived from Section 3.5.1 field list:

```
Agent class instance memory breakdown:
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
SECTION                      FIELDS              BYTES
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Object header (CLR)          sync + vtable          16
Identity & Config
  AgentID                    int                     4
  TeamID                     int                     4
  IsGoalkeeper               bool                    1
  (padding)                                          3
  Attributes                 PlayerAttributes       60
    (6 physical + 9 technical = 15 ints Ã— 4B = 60B)
    (Section 3.5 header notes ~80B as forward estimate
     including alignment padding and future attribute
     fields â€” 60B is the current Stage 0 reality)

Kinematic State
  Position                   Vector3                12
  Velocity                   Vector3                12
  FacingDirection            Vector2                 8
  _cachedSpeed               float                   4
  _speedDirty                bool                    1
  (padding)                                          3

State Machine
  CurrentState               enum (int)              4
  PreviousState              enum (int)              4
  TimeInState                float                   4
  CommandedState             enum (int)              4
  GroundedReason             enum (int)              4
  CollisionForce             float                   4

Turning & Momentum
  LeanAngle                  float                   4
  CurrentTurnRate            float                   4
  IsInStumbleRiskZone        bool                    1
  (padding)                                          3

Fatigue & Stamina
  AerobicStaminaPool         float                   4
  SprintStaminaReservoir     float                   4
  FatigueLevel               float                   4
  StaminaRegenRate           float                   4

PerformanceContext           struct (3 floats)       12

Safety & Recovery
  LastValidPosition          Vector3                12
  LastValidVelocity          Vector3                12
  StateChangesThisSecond     int                     4
  StateLockExpiry            float                   4

AgentEventLogger reference   pointer (64-bit)        8
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
ESTIMATED TOTAL PER AGENT                    ~240 bytes
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
```

**22 agents total:** ~5.3 KB for all agent instances. This is trivial â€” fits entirely in L2 cache on any modern CPU.

**Cache efficiency note:** Unlike Ball Physics' 56-byte `BallState` struct (fits in a single L1 cache line), the Agent class is a heap-allocated reference type at ~240 bytes spanning 4 cache lines. This is acceptable for Stage 0 where clarity and extensibility are prioritized over cache performance. Stage 1 may refactor to a struct-of-arrays (SoA) or ECS pattern if profiling shows cache miss overhead. The public API is designed to support either layout without breaking external contracts (see Section 3.5.1 design note).

#### 4.5.2 Per-Agent Telemetry Buffer

Each agent maintains a ring buffer of 1000 `MovementTelemetryEvent` entries (Section 3.5.7). The actual struct size is derived from the full field list in Section 3.5.7:

```
MovementTelemetryEvent: ~96 bytes per event
  Timestamp          float                4
  AgentID            int                  4
  EventType          enum (int)           4
  Position           Vector3             12
  Velocity           Vector3             12
  State              enum (int)           4
  Command            MovementCommand    ~48
    TargetPosition     Vector3   12
    DesiredState       enum       4
    DecelerationMode   enum       4
    FacingMode         enum       4
    FacingTarget       Vector3   12
    OverrideSafety     bool+pad   4
    DebugLabel         string ref 8
  ContextValue       float                4
  Message            enum (int)           4
  (Total)                               ~96

Per agent:  1000 events Ã— 96 bytes = 96,000 bytes â‰ˆ 93.75 KB
All agents: 22 Ã— 93.75 KB â‰ˆ 2,062 KB â‰ˆ 2.01 MB
```

**âš ï¸ Discrepancy note:** Section 3.5.7's inline comment estimates "~24 bytes/event â‰ˆ 528 KB total." That estimate only counted 4 of the 9 fields. The actual footprint is ~2 MB. This is still acceptable for a development debugging tool (0.025% of 8 GB) but Section 3.5.7 should be corrected in its next revision to reflect the true ~96 byte size.

**Design concern â€” Command field:** Embedding a full `MovementCommand` (including a managed `string` reference for `DebugLabel`) in every telemetry event means each event holds a GC-tracked reference. In Stage 0 this is acceptable because telemetry is a debugging tool, not a shipping feature. Stage 1 should consider replacing the embedded Command with a compact `CommandSnapshot` struct that stores only the enum fields (DesiredState, DecelerationMode, FacingMode) plus a `MessageCode` instead of the full string, reducing per-event size to ~56 bytes and eliminating GC references entirely.

At 60Hz, the buffer holds approximately 16.7 seconds of continuous history per agent. This is sufficient for debugging and replay reconstruction in Stage 0. Stage 1 optimizations (selective logging, delta encoding, Command field compaction) are noted in Section 3.5.7 but not implemented in Stage 0.

**Editor vs. release telemetry behavior:** In editor/development builds, `TryLogSnapshot()` logs every frame (full 60Hz capture). In release builds, logging is restricted to state transitions and safety events only â€” typically 5â€“20 events per agent per minute rather than 3,600. This reduces effective telemetry memory usage in release builds to under 50 KB total across all agents, since buffers rarely fill beyond a few hundred entries.

#### 4.5.3 Static Memory

| Component | Size | Notes |
|-----------|------|-------|
| AgentMovementConstants | ~400 bytes | ~100 const/static readonly floats and ints |
| PitchConfiguration | ~48 bytes | 6 boundary floats Ã— 2 (min/max) Ã— 4 bytes |
| MessageCode enum | Negligible | Integer-backed enum, no runtime allocation |

**Total static memory:** Under 500 bytes. Loaded once at startup, never reallocated.

#### 4.5.4 Total Memory Footprint

| Component | Per-Agent | All 22 Agents |
|-----------|-----------|---------------|
| Agent instances | ~240 B | ~5.3 KB |
| Telemetry buffers | ~93.75 KB | ~2,062 KB (~2.01 MB) |
| Static constants | â€” | ~0.5 KB |
| **Total** | â€” | **~2.07 MB** |

For context, Ball Physics' total memory footprint is under 1 KB (one BallState struct + constants). Agent Movement is significantly larger due to 22 entities and per-agent telemetry buffers. However, 2.07 MB is still negligible on any target platform â€” less than 0.03% of a typical 8 GB system. The telemetry buffers (~2 MB) dominate the footprint and are a debugging tool that can be reduced or disabled in release builds.

### 4.6 Performance Profiling

#### 4.6.1 Profiling Hooks

Conditional compilation profiling hooks are inserted around each pipeline step, following the Ball Physics Spec #1 Section 4.6.1 pattern:

```csharp
public static Vector3 CalculateAcceleration(
    AgentMovementState state, float speed, float topSpeed,
    float k_accel, Vector3 targetDir, float fatigueMod)
{
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    UnityEngine.Profiling.Profiler.BeginSample("AgentMovement.Acceleration");
    #endif

    // ... acceleration calculation ...

    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    UnityEngine.Profiling.Profiler.EndSample();
    #endif

    return acceleration;
}
```

**Profiling labels (one per pipeline step):**
- `AgentMovement.StateEval` â€” State machine evaluation
- `AgentMovement.Acceleration` â€” Locomotion acceleration calculation
- `AgentMovement.DirectionalPenalty` â€” Zone multiplier application
- `AgentMovement.TurnRate` â€” Turn rate + lean + stumble calculation
- `AgentMovement.FacingUpdate` â€” Facing direction rotation
- `AgentMovement.Integration` â€” Velocity and position integration
- `AgentMovement.Safety` â€” Safety validation and clamping
- `AgentMovement.CacheUpdate` â€” Speed cache and property refresh
- `AgentMovement.Telemetry` â€” Event logging

**Aggregate label:**
- `AgentMovement.AllAgents` â€” Wraps the entire 22-agent loop. This is the primary measurement point for the Section 5 performance budget.

#### 4.6.2 Performance Target

**Per-frame budget (60Hz):**
- All 22 agents combined: **< 3.0ms** (hard ceiling from Ball Physics Spec #1 Section 6.2.1 budget table)
- Target average: **< 2.0ms** for all 22 agents
- Target per-agent average: **< 0.091ms** (2.0ms Ã· 22)
- Per-agent worst case: **< 0.14ms** (3.0ms Ã· 22)

These targets represent Agent Movement's allocation from the shared 16.7ms frame budget. Ball Physics consumes < 0.5ms. Collision System (Spec #3) is budgeted at < 2.0ms. The remaining ~11.2ms is available for AI, rendering, audio, and other systems.

**Measurement protocol:**

1. Run 10 simulated matches (minimum 900 simulated minutes of play)
2. Record Unity Profiler data every frame using `AgentMovement.AllAgents` label
3. Calculate: mean, p50, p95, p99, max
4. Flag any frame where `AgentMovement.AllAgents` exceeds 3.0ms for investigation
5. Report per-agent breakdown using individual pipeline step labels

Section 5 (Performance Analysis) derives the expected operation counts and validates that these targets are achievable.

### 4.7 Migration Path to Fixed64

Identical strategy to Ball Physics Spec #1 Section 4.7, adapted for Agent Movement's larger API surface.

**Stage 0: Use standard floats**
- All calculations use `float` (32-bit IEEE 754)
- `Vector3` and `Vector2` from Unity (float-backed)
- Acceptable for single-player prototype
- Cross-platform float behavior variations are tolerable for non-competitive play

**Stage 5+ Migration:**

1. **Wrapper Layer** â€” Create `AgentMovementMath.cs` abstraction defining `Add()`, `Multiply()`, `Sqrt()`, `Exp()`, `Pow()`, `Atan2()` interfaces. Initial implementation delegates to standard `Mathf`.

2. **Gradual Replacement** â€” Replace `Vector3`/`Vector2` with custom `AgentVector3`/`AgentVector2` types that use the math wrapper. Replace direct `Mathf` calls with `AgentMovementMath` calls. Test determinism after each file conversion. Agent Movement has more math operations than Ball Physics (4 subsystems vs. 1), so this migration is estimated at 2â€“3Ã— the effort.

3. **Fixed64 Backend** â€” Implement `Fixed64Math` backend per Spec #8. Switch backend via compile flag. Retest all 68 unit tests, 10 integration tests, and 8 validation benchmarks. Verify replay determinism across platforms.

4. **Validation** â€” Compare float vs. Fixed64 agent trajectories over 100 simulated matches. Accept if positional divergence is < 1cm per agent per match. Benchmark performance difference â€” accept if < 15% overhead (slightly higher tolerance than Ball Physics' 10% due to larger computation volume).

**Risk mitigation:**
- Float backend retained as compile-time fallback
- Migration is incremental (one file at a time)
- Each converted file is tested independently before proceeding
- 68 existing unit tests serve as regression suite throughout migration

---

**End of Section 4**

**Page Count:** ~10 pages (expanded from ~8 by integration point section and corrected telemetry analysis)  
**Status:** Draft v1.1, revised â€” all 10 critique issues resolved  
**Next Section:** Section 5 (Performance Analysis)
