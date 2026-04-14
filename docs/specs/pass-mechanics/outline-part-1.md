# Pass Mechanics Specification #5 â€” Detailed Outline

**Purpose:** Planning document for Pass Mechanics specification, establishing scope, structure, mathematical framework, and technical approach before full section drafting begins. This document defines what will be written, not yet how it will be implemented in detail.

**Created:** February 19, 2026, 12:00 PM PST  
**Version:** 1.0  
**Status:** Awaiting Approval  
**Specification Number:** 5 of 20 (Stage 0)  
**Estimated Effort:** ~35 hours (larger than First Touch due to pass type taxonomy complexity)  
**Dependencies:** Ball Physics (#1), Agent Movement (#2), Collision System (#3), First Touch (#4)

---

## EXECUTIVE SUMMARY

Pass Mechanics governs the initiation and execution of all passes â€” the moment an agent decides to play the ball to another location and the physics of that kick are applied. This is the most frequently executed action in any football match; every passing event flows through this system.

The core responsibility of this specification is narrow but critical: **translate a pass intent into a ball state**. Everything before (the decision to pass) is owned by Decision Tree (#8). Everything after (the ball's flight) is owned by Ball Physics (#1). Everything on the receiving end is owned by First Touch (#4).

**Core model:**
```
PassResult = PassType Ã— (Velocity, Spin, LaunchAngle) Ã— ErrorVector
```

Where:
- `PassType` determines the physical profile (trajectory shape, spin characteristics)
- `Velocity`, `Spin`, `LaunchAngle` are derived from agent attributes, distance, and intent
- `ErrorVector` applies deterministic inaccuracy from attributes, pressure, fatigue, and orientation

**Output interface (defined in Ball Physics #1, Â§3.1.11.2):**
```
Ball.ApplyKick(velocity: Vector3, spin: Vector3, kickType: KickType)
```

---

## SECTION 1: PURPOSE & SCOPE (~3 pages)

### 1.1 What This Specification Covers

- Pass type classification (ground, driven, lofted, through ball, cross, chip, lobbed)
- Ball velocity calculation from pass type, distance, power attribute, and fatigue
- Launch angle derivation per pass type
- Spin/rotation vector calculation (topspin, backspin, sidespin/curve)
- Pass error model: deterministic inaccuracy from attribute, pressure, fatigue, orientation
- Target resolution: player-targeted vs space-targeted passes
- Lead distance calculation for through balls (receiver movement projection)
- Weak foot penalty model
- Pass execution state machine (IDLE â†’ INITIATING â†’ WINDUP â†’ CONTACT â†’ FOLLOW_THROUGH â†’ COMPLETE)
- Cancellation handling (tackle interrupts windup phase)
- Interface to Ball.ApplyKick() â€” the handoff to Ball Physics
- Event publishing (PassAttemptEvent, PassCompletedEvent, PassInterceptedEvent)
- Performance budget and operation count analysis

### 1.2 What Is OUT of Scope

#### 1.2.1 Responsibilities Owned by Other Specifications

| Responsibility | Owner Specification | Interface |
|----------------|---------------------|-----------|
| Decision to pass and target selection | Decision Tree (#8) | Decision Tree calls Pass Mechanics with PassRequest |
| Ball trajectory physics after kick | Ball Physics (#1) | Pass Mechanics calls Ball.ApplyKick(); Ball Physics simulates |
| First touch reception quality | First Touch (#4) | First Touch receives the result of what Pass Mechanics initiates |
| Collision detection during execution | Collision System (#3) | Collision System detects if tackle interrupts windup |
| Agent animation during pass | Animation System (Stage 1) | AnimationDataContract exposed; consumed in Stage 1 |
| Goalkeeper distribution | Goalkeeper Mechanics (#11) | Goal kicks, punts, and throws are distinct from outfield passing |
| Heading/flick-on passes | Heading Mechanics (#10) | All aerial ball contacts above 0.5m are Heading Mechanics territory |
| Shot mechanics | Shot Mechanics (#6) | Shots differ in power, trajectory intent, and outcome evaluation |
| Team passing style instructions | Formation/Instructions System (Stage 1) | Modifiers applied on top of base pass model in Stage 1 |

#### 1.2.2 Stage 1+ Deferrals

| Feature | Deferral Reason | Target Stage |
|---------|-----------------|--------------|
| Body part selection (instep, outside, laces, heel) | Stage 0 uses simplified foot contact | Stage 1 |
| Animation-driven pass timing | Requires animation system integration | Stage 1 |
| Team instruction modifiers (short passing, direct) | Requires Formation System | Stage 1 |
| Pass accuracy statistics per player | Requires Statistics Engine | Stage 1 |
| Surface condition effects on ball roll | Requires Pitch Condition System | Stage 2 |
| Curling free kicks | Set piece system not yet designed | Stage 2 |
| Player-specific passing tendencies | Requires extended attribute system | Stage 2 |

#### 1.2.3 Permanent Exclusions

| Feature | Exclusion Rationale |
|---------|---------------------|
| Random dice rolls for pass error | Error is deterministic from physics inputs; no arbitrary miss chance |
| "Scripted" failed passes | All outcomes emerge from the attribute/physics model |
| Context-unaware "perfect passes" | No ball-magnet or auto-completion â€” physics governs |
| Tactical AI reasoning inside this spec | Pass Mechanics executes; Decision Tree reasons |

### 1.3 Key Design Decisions

1. **Pass error is deterministic** â€” Given identical inputs (attributes, pressure, fatigue, body orientation), the error vector is always identical. No hidden randomness. This is mandatory for replay determinism.

2. **Pass Mechanics only initiates; Ball Physics owns flight** â€” Once `Ball.ApplyKick()` is called, this spec's responsibility ends. The trajectory, Magnus effect, bounce, and roll are fully owned by Spec #1.

3. **Pass type is explicitly classified, not implicitly derived** â€” Each pass type has a distinct profile (velocity range, launch angle, spin type). The Decision Tree selects a pass type; Pass Mechanics executes it.

4. **Error is angular, not positional** â€” Error is applied as a deviation angle from the intended direction at the kick point. The ball then flies in that direction. This is physically correct and avoids the need for target position manipulation.

5. **Through ball lead is linear projection only** â€” Stage 0 uses simple linear receiver position prediction. No AI lookahead, no probability weighting. Predictability over sophistication.

6. **Weak foot is a scalar penalty on accuracy, not a separate system** â€” A single `WeakFootPenalty` multiplier scales the error magnitude. Extensible to per-player preferred foot data in Stage 1.

7. **Windup is observable but non-interruptible after CONTACT** â€” A tackle during WINDUP cancels the pass. A tackle during or after CONTACT does not â€” the ball is already in flight.

### 1.4 Implementation Timeline

**Stage 0 (Year 1 â€” Current):**
- Full pass type classification (all 7 types)
- Velocity, launch angle, spin calculation per type
- Deterministic error model (attribute, pressure, fatigue, orientation)
- Player-targeted and space-targeted pass resolution
- Through ball lead distance (linear projection)
- Weak foot scalar penalty
- Pass execution state machine
- Integration with Ball.ApplyKick()
- Event publishing stubs
- Full unit and integration test suite

**Stage 1 (Year 2):**
- Body part differentiation (instep vs outside vs laces)
- Animation system integration (AnimationDataContract fields)
- Team instruction modifiers (passing style affects velocity/error)
- Pass accuracy statistics per player (consumed by Statistics Engine)
- Preferred foot data integration (replacing simplified weak foot scalar)

**Stage 2 (Year 3â€“4):**
- Surface condition modifiers (wet pitch slows ground pass roll â€” interaction with Ball Physics)
- Spin visualization feedback
- Advanced curling pass model (extended sidespin derivation)
- Player-specific pass style tendencies

**Stage 5+:**
- Fixed64 determinism for multiplayer
- Network-synchronized pass events

### 1.5 Dependencies

**Required (Stage 0):**

| Dependency | Source | What We Need |
|------------|--------|--------------|
| Ball state at kick | Ball Physics (#1) | `Ball.ApplyKick(velocity, spin, kickType)` interface |
| Agent attributes | Agent Movement (#2) | `AgentAttributes`: Passing, Power, Technique, Fatigue, WeakFoot rating |
| Agent state | Agent Movement (#2) | Position, velocity, facing direction, movement state |
| Pressure query | Collision System (#3) | Spatial hash query for nearby opponents during execution |
| Pass intent | Decision Tree (#8) â€” forward ref | `PassRequest` struct: target, pass type, urgency |
| Possession state | Ball Physics (#1) | Confirm agent has possession before pass initiation |

**Provided to (Stage 0):**

| Consumer | What We Provide |
|----------|-----------------|
| Ball Physics (#1) | Kick velocity vector, spin vector, kick type |
| First Touch (#4) | Pass was initiated (implicit â€” receiver sees inbound ball) |
| Event System (#17) | `PassAttemptEvent`, `PassCompletedEvent`, `PassInterceptedEvent` |
| Animation System (Stage 1) | `PassAnimationData` struct (populated but unconsumed in Stage 0) |

### 1.6 Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | February 19, 2026 | Specification Team | Initial outline created |
| 1.1 | March 25, 2026 | Claude (AI) / Anton | Post-audit fixes: Decision Tree #7→#8 (C-03, 6 instances); Chip/Lobbed synonym added to taxonomy diagram (Mod-02); OQ-6 marked resolved — parameter-based ApplyKick, no KickType enum (Mod-04). |

---

## SECTION 2: SYSTEM OVERVIEW (~4 pages)

### 2.1 Functional Requirements

**FR-1: Pass Type Classification**
- System must support all 7 pass types: Ground, Driven, Lofted, Through Ball (ground), Through Ball (aerial), Cross, Chip/Lobbed
- Each pass type must have a defined velocity range, launch angle range, and dominant spin type
- Pass type is provided by the caller (Decision Tree); Pass Mechanics does not select it

**FR-2: Velocity Calculation**
- Base velocity derived from pass type, intended distance, and agent Power attribute
- Fatigue modifier applied (reduces effective Power)
- Velocity clamped per pass type (minimum and maximum bounds)
- Formula must be derivable from first principles with documented rationale

**FR-3: Launch Angle Calculation**
- Launch angle derived from pass type and distance
- Ground passes: near-zero elevation (2Â°â€“5Â° to clear uneven ground)
- Driven passes: 5Â°â€“12Â° elevation
- Lofted passes: 20Â°â€“45Â° depending on distance
- Cross: 10Â°â€“35Â° depending on type (flat, whipped, high)
- Chip: 45Â°â€“65Â° for short chips; 35Â°â€“45Â° for longer lofted passes
- All launch angles must be physically reasonable

**FR-4: Spin Vector Calculation**
- Topspin: applied on driven/ground passes (increases speed of roll on landing)
- Backspin: applied on chip/lobbed passes (reduces run on landing, holds up)
- Sidespin: applied on curling crosses and outside-of-foot passes
- Spin magnitude scales with Technique attribute
- Spin vector passed to `Ball.ApplyKick()` for Magnus effect calculation in Ball Physics

**FR-5: Deterministic Error Model**
- Error applied as angular deviation from intended kick direction
- Error magnitude derived from: Passing attribute, pressure scalar, fatigue level, body orientation
- No use of System.Random or non-seeded randomness
- Same inputs â†’ identical error vector
- Error must be bounded: elite players (Passing â‰¥ 18) cannot produce catastrophically bad passes under low-pressure conditions

**FR-6: Player-Targeted Pass Resolution**
- When target is a player, compute aim point as receiver's projected position
- Projection uses linear extrapolation of receiver's current velocity (Stage 0)
- No AI lookahead or probability weighting â€” simple physics projection only

**FR-7: Space-Targeted Pass Resolution (Through Ball)**
- When target is a space coordinate, compute lead distance based on receiver movement
- Lead calculation: projected interception point where receiver arrives simultaneously with ball
- Stage 0: linear receiver velocity projection only
- If no valid interception point exists within pitch bounds, pass executes to raw target coordinate

**FR-8: Weak Foot Penalty**
- Detect if kick is made with non-preferred foot
- Apply `WeakFootPenalty` scalar to error magnitude
- Penalty derived from agent's WeakFoot attribute [1â€“5 scale, per Master Vol design]
- WeakFoot = 5 (both feet equally strong): no penalty
- WeakFoot = 1 (very poor with weak foot): significant accuracy degradation

**FR-9: Pass Execution State Machine**
- States: IDLE â†’ INITIATING â†’ WINDUP â†’ CONTACT â†’ FOLLOW_THROUGH â†’ COMPLETE
- Each state has defined duration and transition conditions
- Tackle collision during WINDUP cancels pass and returns to IDLE
- CONTACT state is the frame on which Ball.ApplyKick() is called
- FOLLOW_THROUGH is cosmetic; no physics effect

**FR-10: Event Publishing**
- `PassAttemptEvent`: published at CONTACT state (pass is committed)
- `PassCompletedEvent`: published when receiver takes first touch (cross-spec event, triggered by First Touch #4 callback)
- `PassInterceptedEvent`: published when opponent gains possession of pass in flight (triggered by Ball Physics possession change)

**FR-11: Determinism**
- All calculations deterministic for replay consistency
- No floating-point operation ordering ambiguity (document all operation sequences)
- Consistent with Fixed64 migration path (Stage 5)

### 2.2 System Architecture

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚                          PASS MECHANICS SYSTEM                           â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                                                          â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”    â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚  Decision Tree   â”‚â”€â”€â”€â–¶â”‚            PassRequest                   â”‚   â”‚
â”‚  â”‚  (#7 - caller)   â”‚    â”‚  PassType, TargetID/Position, Urgency    â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜    â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                             â”‚                            â”‚
â”‚                                             â–¼                            â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”    â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”   â”‚
â”‚  â”‚  Agent Movement  â”‚â”€â”€â”€â–¶â”‚          PassExecutor                    â”‚   â”‚
â”‚  â”‚  Attributes,     â”‚    â”‚                                          â”‚   â”‚
â”‚  â”‚  State, Facing   â”‚    â”‚  1. ValidateRequest()                    â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜    â”‚  2. ClassifyPassType()                   â”‚   â”‚
â”‚                           â”‚  3. ResolveTarget()                     â”‚   â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”    â”‚  4. CalculateVelocity()                  â”‚   â”‚
â”‚  â”‚  Collision Sys   â”‚â”€â”€â”€â–¶â”‚  5. CalculateLaunchAngle()               â”‚   â”‚
â”‚  â”‚  Pressure Query  â”‚    â”‚  6. CalculateSpin()                      â”‚   â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜    â”‚  7. CalculateError()                     â”‚   â”‚
â”‚                           â”‚  8. ApplyError()                        â”‚   â”‚
â”‚                           â”‚  9. Ball.ApplyKick()                    â”‚   â”‚
â”‚                           â”‚  10. PublishEvents()                    â”‚   â”‚
â”‚                           â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜   â”‚
â”‚                                             â”‚                            â”‚
â”‚               â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”       â”‚
â”‚               â–¼                             â–¼                   â–¼       â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”    â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â” â”‚
â”‚  â”‚  Ball Physics    â”‚    â”‚   Event System       â”‚  â”‚  Animation Data  â”‚ â”‚
â”‚  â”‚  ApplyKick()     â”‚    â”‚   PassAttemptEvent   â”‚  â”‚  (Stage 1)       â”‚ â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜    â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜ â”‚
â”‚                                                                          â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

**Data flow summary:**
1. Decision Tree creates `PassRequest` with selected pass type and target
2. PassExecutor validates request (possession confirmed, target reachable)
3. Target resolved to world position (player-targeted: lead projection; space-targeted: coordinate)
4. Physical parameters computed (velocity, launch angle, spin vector)
5. Error vector computed deterministically from agent state
6. Error applied: rotate kick direction by error angle
7. `Ball.ApplyKick(velocity, spin, kickType)` called â€” Ball Physics takes over
8. Events published for replay/statistics

### 2.3 Frame Pipeline Position

```
Frame N (60Hz, ~16.67ms budget):
  1.  Input processing
  2.  AI decision making (10Hz heartbeat â€” may produce PassRequest)
  3.  Agent Movement: update positions and velocities
  4.  Ball Physics: update ball trajectory
  5.  Collision System: detect all contacts
      5a. Agent-agent collisions â†’ Collision Response
      5b. Agent-ball collisions â†’ First Touch (#4)
      5c. Tackle-on-agent-with-ball â†’ may cancel Pass WINDUP state
  6.  Pass Mechanics: if agent in WINDUP and tackle detected â†’ cancel
  7.  Pass Mechanics: if agent reaches CONTACT state â†’ execute kick
      7a. Compute all pass parameters
      7b. Call Ball.ApplyKick()
      7c. Agent loses possession
  8.  Ball Physics: apply new kick velocity within same frame
  9.  Event System: process PassAttemptEvent
  10. Rendering (interpolated)
```

**Key timing:** Pass execution (step 7) must complete within the same frame as the CONTACT state. `Ball.ApplyKick()` is called synchronously; Ball Physics applies the velocity immediately.

### 2.4 Data Structures

#### 2.4.1 PassRequest

```csharp
/// <summary>
/// Input to the Pass Mechanics system from Decision Tree.
/// Represents a complete pass intent before physics calculation.
/// 
/// Created by: Decision Tree (#8)
/// Consumed by: PassExecutor
/// Lifetime: Single pass execution cycle (created â†’ consumed â†’ discarded)
/// </summary>
public struct PassRequest
{
    /// <summary>Unique ID of the passing agent</summary>
    public int AgentID;
    
    /// <summary>Requested pass type (ground, driven, lofted, through ball, cross, chip)</summary>
    public PassType PassType;
    
    /// <summary>
    /// Target: either a player ID or a world position.
    /// For player-targeted passes: TargetAgentID is valid, TargetPosition is ignored.
    /// For space-targeted passes: TargetPosition is valid, TargetAgentID is -1.
    /// </summary>
    public int TargetAgentID;        // -1 if space-targeted
    public Vector3 TargetPosition;   // World space; ignored if player-targeted
    
    /// <summary>
    /// Pass urgency [0.0 - 1.0]. Affects windup duration.
    /// 0.0 = unhurried (full windup). 1.0 = rushed (minimal windup, accuracy penalty).
    /// </summary>
    public float Urgency;
    
    /// <summary>True if pass is intended with non-preferred foot (detected by Decision Tree)</summary>
    public bool IsWeakFoot;
    
    /// <summary>Frame timestamp for determinism and replay</summary>
    public int FrameNumber;
}
```

#### 2.4.2 PassResult

```csharp
/// <summary>
/// Output of the Pass Mechanics computation.
/// Contains the physical state passed to Ball Physics and event data.
/// 
/// Created by: PassExecutor
/// Consumed by: Ball Physics (ApplyKick), Event System
/// Lifetime: Single pass cycle
/// </summary>
public struct PassResult
{
    /// <summary>Final kick velocity vector in world space (m/s)</summary>
    public Vector3 KickVelocity;
    
    /// <summary>Spin vector for Magnus effect (rad/s around each axis)</summary>
    public Vector3 SpinVector;
    
    /// <summary>Pass type as classified (for Ball Physics kick profile)</summary>
    public KickType KickType;
    
    /// <summary>Error angle applied (degrees) â€” for analytics/replay</summary>
    public float ErrorAngleDegrees;
    
    /// <summary>Whether pass was executed or cancelled</summary>
    public PassOutcome Outcome;
    
    /// <summary>Computed lead distance for through balls (0 if not applicable)</summary>
    public float LeadDistance;
    
    /// <summary>Frame number of execution (for replay synchronization)</summary>
    public int ExecutionFrame;
}
```

#### 2.4.3 PassType Enum

```csharp
/// <summary>
/// All supported pass types in Stage 0.
/// Each type maps to a distinct physical profile (velocity range, launch angle, spin).
/// Extensible: new types added with corresponding profile entries.
/// </summary>
public enum PassType
{
    Ground      = 0,    // Short-to-medium ground pass; topspin; low elevation
    Driven      = 1,    // Firm, fast ground pass; strong topspin; low elevation
    Lofted      = 2,    // High trajectory pass; backspin; 20â€“45Â° elevation
    ThroughBall = 3,    // Ground pass into space; leading the receiver
    AerialThrough = 4,  // Lofted ball into space; receiver must control aerial ball
    Cross       = 5,    // Wide delivery into the box; sidespin variants
    Chip        = 6,    // Lobbed over defender; high arc; backspin; short distance
}
```

#### 2.4.4 PassOutcome Enum

```csharp
public enum PassOutcome
{
    Executed    = 0,    // Pass completed successfully (ball in flight)
    Cancelled   = 1,    // Windup interrupted by tackle before CONTACT
    Invalid     = 2,    // Request rejected (no possession, invalid target, etc.)
}
```

### 2.5 Interface Contracts

#### 2.5.1 Ball Physics Interface (Upstream)

```csharp
// Defined in Ball Physics #1, Â§3.1.11.2
// Pass Mechanics CALLS this interface â€” it does not own it
Ball.ApplyKick(Vector3 velocity, Vector3 spin, KickType kickType)
```

- `velocity`: full 3D velocity vector including elevation component
- `spin`: angular velocity vector (rad/s) â€” passed directly to Magnus calculation
- `kickType`: matches `PassType` mapped to `KickType` enum (Ball Physics owns this enum)
- Must only be called when agent has confirmed possession (checked in ValidateRequest)

#### 2.5.2 Agent Movement Interface (Upstream)

```csharp
// Queried per pass execution â€” read only, no modifications
AgentAttributes attributes = AgentSystem.GetAttributes(agentID);
// Required fields: Passing [1-20], Power [1-20], Technique [1-20], Fatigue [0-1], WeakFootRating [1-5]

AgentState state = AgentSystem.GetState(agentID);
// Required fields: Position, Velocity, FacingDirection, MovementState
```

#### 2.5.3 Collision System Interface (Upstream)

```csharp
// Spatial hash query for pressure detection
List<AgentState> nearbyOpponents = SpatialHash.QueryRadius(agentPosition, PRESSURE_RADIUS);
// Filtered to opponents only within PassMechanics

// Tackle detection during windup
bool tackleInterrupt = CollisionSystem.HasTackleContact(agentID, currentFrame);
```

#### 2.5.4 Event System Interface (Downstream)

```csharp
// Defined in Event System #17 â€” Pass Mechanics publishes, does not own
EventBus.Publish(new PassAttemptEvent { AgentID, PassType, TargetPosition, Frame });
// PassCompletedEvent and PassInterceptedEvent triggered by downstream systems
```

### 2.6 Failure Modes and Recovery

| ID | Failure Mode | Detection | Recovery | Severity |
|----|--------------|-----------|----------|----------|
| FM-01 | Agent does not have possession at pass initiation | Possession check in ValidateRequest | Reject PassRequest, return PassOutcome.Invalid, log warning | High |
| FM-02 | NaN or Infinity in computed velocity | `float.IsNaN()` / `float.IsInfinity()` post-calculation | Clamp to zero, cancel pass, log error | High |
| FM-03 | Target position outside pitch bounds | Boundary check in ResolveTarget | Clamp target to nearest valid pitch position, proceed | Medium |
| FM-04 | No valid interception point for through ball | Lead calculation returns null | Fall back to raw target coordinate, proceed | Medium |
| FM-05 | Attribute values outside expected range | Range check [1â€“20] | Clamp to valid range, log warning | Low |
| FM-06 | Tackle interrupt arrives after CONTACT | Frame-order check | Ignore â€” CONTACT is irreversible; log for analytics | Low |
| FM-07 | Zero-distance pass (agent at target position) | Distance < MIN_PASS_DISTANCE | Reject request, return Invalid | Medium |

**Recovery philosophy:** Prefer graceful degradation over crash. Maintain determinism even in error paths (same error input â†’ same fallback output).

---

