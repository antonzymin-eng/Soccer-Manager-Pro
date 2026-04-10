# First Touch Mechanics Specification #4 â€” Section 1: Purpose & Scope

**Purpose:** Establishes the boundaries, responsibilities, and design philosophy for the First Touch Mechanics system. Defines what this specification covers, what it explicitly excludes, and how it interfaces with dependent specifications.

**Created:** February 16, 2026, 8:15 PM PST  
**Revised:** March 05, 2026  
**Version:** 1.1  
**Status:** Approved  
**Author:** Claude (AI) with Anton (Lead Developer)  
**Specification Number:** 4 of 20 (Stage 0 Physics Foundation)

**Changelog:**
- v1.1 (March 05, 2026): Comprehensive audit fixes applied:
  (1) §1.2.2 Implementation Formula rewritten to match Section 3 v1.1 authoritative formula — old formula used different velocity difficulty structure (`1.0 + BallSpeed/15`), wrong movement reference (10.2 m/s), and wrong penalty factor (0.3). All now align with §3.1.1.
  (2) §1.2.2 constants table updated — MOVEMENT_REFERENCE corrected from 10.2 to 7.0 m/s with [GT] tag; MOVEMENT_PENALTY corrected from 0.3 to 0.5; PRESSURE_WEIGHT (0.40) added.
  (3) §1.6.2 consumer table: "Tactical AI (#7)" corrected to "Perception System (#7)".
  (4) False cross-reference "Agent Movement §3.5.2 top sprint speed 7.0 m/s" removed; MOVEMENT_REFERENCE re-tagged as [GT] gameplay-tuned (7.0 m/s is not a value defined in Agent Movement).

---

## 1.1 Document Purpose

This section defines the scope boundaries for First Touch Mechanics Specification #4. It serves as the authoritative reference for:

1. **What this specification owns** â€” Control quality calculation, touch radius determination, possession transfer logic
2. **What other specifications own** â€” Ball trajectory, agent movement, collision detection
3. **Key design decisions** â€” Determinism, attribute weighting, pressure modeling
4. **Implementation timeline** â€” Stage 0 deliverables vs. future enhancements
5. **Dependencies** â€” Required interfaces from other specifications

---

## 1.2 What This Specification Covers

First Touch Mechanics governs the physics and gameplay systems that determine **what happens when an agent makes contact with the ball for the purpose of controlling it**. This is the bridge between Collision System (which detects contact) and Ball Physics (which simulates ball trajectory).

### 1.2.1 Core Responsibilities

| Responsibility | Description |
|----------------|-------------|
| **Control Quality Calculation** | Computes a [0.0â€“1.0] quality score based on agent attributes, ball velocity, agent velocity, pressure context, and body orientation |
| **Touch Radius Determination** | Maps control quality to ball displacement distance per threshold table below |
| **Possession Transfer** | Determines outcome: CONTROLLED, LOOSE_BALL, DEFLECTION, or INTERCEPTION |
| **Touch Direction Control** | Applies agent's intended direction with accuracy proportional to control quality |
| **Body Orientation Bonus** | Evaluates "half-turn" stance for +15% effective Technique bonus |
| **Pressure Degradation** | Queries nearby opponents and applies proximity-based penalty to control quality |
| **Dribbling State Activation** | Signals Agent Movement system to apply dribbling locomotion modifiers |

**Touch Radius Thresholds (from Master Vol 1 Â§6):**

| Control Quality | Classification | Touch Radius | Outcome Likelihood |
|-----------------|----------------|--------------|-------------------|
| â‰¥ 0.85 | Perfect Control | â‰¤ 0.3m | CONTROLLED: ~95% |
| 0.60 â€“ 0.84 | Good Control | 0.3m â€“ 0.6m | CONTROLLED: ~80% |
| 0.35 â€“ 0.59 | Poor Control | 0.6m â€“ 1.2m | LOOSE_BALL: ~60% |
| < 0.35 | Heavy Touch | 1.2m â€“ 2.0m | LOOSE_BALL/DEFLECTION: ~70% |

### 1.2.2 Governing Formula

From Master Volume 1 Â§6 (Touch Mechanics):

```
Control_Quality = Agent_Technique / (Ball_Velocity Ã— Agent_Inertia)
```

This specification expands this conceptual formula into a complete, normalised implementation.
The authoritative, step-by-step formula is defined in Section 3 §3.1.1. The summary below
is a condensed reference; **Section 3 takes precedence** in any conflict.

**Term Mapping:**
- `Agent_Technique` → Weighted attribute score (Technique + First Touch), normalised to [0.05, 1.0]
- `Ball_Velocity` → Incoming ball speed, divided by reference velocity, clamped
- `Agent_Inertia` → Agent’s movement state, modeled as difficulty receiving while moving

**Implementation Formula (matches §3.1.1):**

```
// — Step 1: Weighted attribute score ————————————————————
WeightedAttr   = (Technique × 0.70) + (FirstTouch × 0.30)
// Range: min = 1.0 ; max = 20.0

// — Step 2: Normalise to [0.05, 1.0] ———————————————————
NormAttr       = WeightedAttr / ATTR_MAX                   // ATTR_MAX = 20.0

// — Step 3: Orientation bonus (multiplicative) ———————————
AttrWithBonus  = NormAttr × (1.0 + orientationBonus)
// orientationBonus ∈ [0.0, +0.15]; applied by §3.6

// — Step 4: Velocity difficulty ———————————————————————
VelDifficulty  = ball.Speed / VELOCITY_REFERENCE            // VELOCITY_REFERENCE = 15.0 m/s
VelDifficulty  = Clamp(VelDifficulty, 0.1, VELOCITY_MAX_FACTOR)  // VELOCITY_MAX_FACTOR = 4.0

// — Step 5: Movement difficulty ———————————————————————
MoveDifficulty = 1.0 + (agent.Speed / MOVEMENT_REFERENCE) × MOVEMENT_PENALTY
// MOVEMENT_REFERENCE = 7.0 m/s [GT]; MOVEMENT_PENALTY = 0.5

// — Step 6: Raw quality before pressure —————————————————
RawQuality     = AttrWithBonus / (VelDifficulty × MoveDifficulty)

// — Step 7: Apply pressure degradation ——————————————————
q              = RawQuality × (1.0 - pressureScalar × PRESSURE_WEIGHT)
// PRESSURE_WEIGHT = 0.40; max pressure degrades quality by 40%

// — Step 8: Final clamp —————————————————————————————
q              = Clamp(q, 0.0, 1.0)
```

**Constant Definitions:**
| Constant | Value | Tag | Derivation |
|----------|-------|-----|------------|
| TECHNIQUE_WEIGHT | 0.70 | [GT] | Technique is broad ball mastery; primary driver. Design decision §1.4.2 |
| FIRST_TOUCH_WEIGHT | 0.30 | [GT] | First Touch is specialisation; secondary driver. Design decision §1.4.2 |
| ATTR_MAX | 20.0 | [FIXED] | Maximum attribute value (1–20 scale from Agent Movement §3.5.6) |
| VELOCITY_REFERENCE | 15.0 m/s | [EST] | Median pass speed in professional football (broadcast observation) |
| VELOCITY_MAX_FACTOR | 4.0 | [GT] | Caps difficulty at 60 m/s equivalent; prevents numerical degeneracy |
| MOVEMENT_REFERENCE | 7.0 m/s | [GT] | Gameplay-tuned sprint reference; typical average-player sprint speed. **Not** the Pace 20 maximum (10.2 m/s from Agent Movement §3.2) — chosen to make movement penalty scale meaningfully across the attribute range |
| MOVEMENT_PENALTY | 0.5 | [GT] | At full sprint (7 m/s), adds 50% difficulty; tuned from sports science literature |
| PRESSURE_WEIGHT | 0.40 | [GT] | Pressure can degrade quality by at most 40%; preserves non-zero floor |

### 1.2.3 Stage 0 Deliverables

The following components are **in scope** for Stage 0 implementation:

1. **Control Quality Model** â€” Full formula implementation with all modifier terms
2. **Touch Radius Mapping** â€” Piecewise linear function from quality to displacement
3. **Possession Outcome Logic** â€” State machine for CONTROLLED/LOOSE_BALL/DEFLECTION/INTERCEPTION
4. **Pressure Evaluation** â€” Spatial query integration with inverse-square falloff
5. **Body Orientation Detection** â€” Half-turn angle calculation with configurable tolerance
6. **Dribbling State Interface** â€” Signal to Agent Movement for locomotion modifier activation
7. **Event Emission** â€” `FirstTouchEvent` for statistics and replay systems
8. **Unit Tests** â€” Minimum 25 test scenarios covering formula edge cases
9. **Integration Tests** â€” Minimum 8 scenarios testing cross-system behavior

---

## 1.3 What Is OUT of Scope

### 1.3.1 Responsibilities Owned by Other Specifications

| Responsibility | Owner Specification | Interface Point | Rationale |
|----------------|---------------------|-----------------|-----------|
| Ball trajectory after touch | Ball Physics (#1) | First Touch outputs `BallState`; Ball Physics simulates trajectory | Ball Physics owns all ball motion simulation |
| Agent movement and positioning | Agent Movement (#2) | First Touch queries `Agent.Position`, `Agent.Velocity`; does not modify | Agent Movement owns all agent kinematics |
| Contact detection | Collision System (#3) | Collision detects contact and provides `AgentBallCollisionData`; First Touch evaluates quality | Collision System owns geometric intersection |
| Pass execution (kicking) | Pass Mechanics (#5) | Pass Mechanics initiates ball motion; First Touch receives and controls | Pass Mechanics owns ball delivery |
| Dribbling locomotion penalties | Agent Movement (#2) Â§6.1.2 | First Touch activates dribbling state; Agent Movement applies speed/turn penalties | Agent Movement owns all movement modifiers |
| Aerial first touch (heading) | Heading Mechanics (#9) | Ball above `GROUND_CONTROL_HEIGHT` (0.5m) routed to Heading Mechanics | Heading Mechanics owns aerial ball interaction |
| Goalkeeper catching/parrying | Goalkeeper Mechanics (#10) | Catching is distinct action with different physics model | Goalkeeper Mechanics owns all GK-specific actions |
| Foul adjudication | Referee System (#11) | First Touch provides contact data; Referee decides legality | Referee System owns rule enforcement |

### 1.3.2 Stage 1+ Deferrals

The following features are **explicitly deferred** to future development stages:

| Feature | Deferral Reason | Target Stage | Dependency |
|---------|-----------------|--------------|------------|
| Body part selection (foot/chest/thigh) | Requires animation system for visual feedback | Stage 1 | Animation System integration |
| Animation-driven touch types | Touch quality must inform animation selection | Stage 1 | Animation System |
| Skill moves (drag-back, Cruyff turn) | Advanced techniques requiring animation blending | Stage 2 | Skill Move System |
| Feint touches (dummy, let ball run) | Requires decision integration for AI agents | Stage 2 | Tactical Decision System |
| Surface condition effects | Wet/dry/frozen pitch affects ball grip | Stage 2 | Pitch Condition System |
| Closing speed pressure | Opponent's approach velocity compounds pressure | Stage 2 | Enhanced Pressure Model |
| Touch sound design | Audio feedback for touch quality | Stage 1 | Audio System |

**Deferral Rationale:** Stage 0 focuses on establishing correct mathematical foundations. Visual and audio feedback layers are additive enhancements that do not affect core simulation accuracy.

### 1.3.3 Permanent Exclusions

The following features are **permanently excluded** from this specification:

| Feature | Exclusion Rationale |
|---------|---------------------|
| Random "error" dice rolls | Control quality is **deterministic** from measurable inputs. No arbitrary bad touches. Same inputs â†’ same output. |
| Unrealistic "super touch" abilities | Even elite players (Technique 20) are bounded by physics. No "ball magnet" or "perfect first touch every time" abilities. |
| Luck-based touch outcomes | All factors affecting control quality are measurable: velocity, pressure, orientation, attributes. No hidden "luck" modifier. |
| Automatic trap animations | Stage 0 does not dictate animation. The animation system (Stage 1) will consume touch quality data to select appropriate visuals. |
| Touch-through-defender | Ball cannot pass through an opponent's body. Collision System prevents this geometrically. |

**Exclusion Philosophy:** These exclusions enforce the project's "zero magic" design principle from Master Volume 1 Â§1.3. Every gameplay outcome must be traceable to observable, measurable factors.

---

## 1.4 Key Design Decisions

The following decisions establish the philosophical foundation for First Touch Mechanics:

### 1.4.1 Control Quality Is Deterministic

**Decision:** Given identical inputs (agent attributes, ball velocity, agent velocity, pressure context, body orientation), the control quality calculation **always produces identical output**.

**Rationale:**
- Replay consistency â€” Replays must reproduce identical outcomes
- Networked determinism â€” All clients must agree on possession state
- Debugging â€” Reproducible issues enable systematic debugging
- Player trust â€” Outcomes are fair and verifiable, not "scripted"

**Implementation:** No use of `System.Random`, `UnityEngine.Random`, or any non-seeded randomness in control quality calculation. The only randomness is in collision system's stumble probability (seeded RNG), which is upstream of First Touch.

### 1.4.2 Technique Is Primary, First Touch Is Secondary

**Decision:** Effective technique for control quality uses weighted average: **70% Technique, 30% First Touch**.

**Rationale:**
- **Technique** represents general ball mastery â€” the foundation for all ball interaction
- **First Touch** represents specialization in receiving â€” a refinement, not a replacement
- This prevents "one-trick pony" players with high First Touch but low Technique from being elite receivers
- It rewards well-rounded technical players while still allowing specialists to excel

**Mathematical Impact:**
```
EffectiveTechnique = 0.70 Ã— Technique + 0.30 Ã— FirstTouch

Example 1: Rounded player (Technique 15, First Touch 15)
  EffectiveTechnique = 0.70 Ã— 15 + 0.30 Ã— 15 = 10.5 + 4.5 = 15.0

Example 2: Specialist (Technique 10, First Touch 20)
  EffectiveTechnique = 0.70 Ã— 10 + 0.30 Ã— 20 = 7.0 + 6.0 = 13.0

Example 3: Technical player (Technique 18, First Touch 12)
  EffectiveTechnique = 0.70 Ã— 18 + 0.30 Ã— 12 = 12.6 + 3.6 = 16.2
```

The rounded player (15/15) outperforms the specialist (10/20), correctly modeling that first touch skill cannot compensate for lack of fundamental technique.

### 1.4.3 Pressure Proximity Degrades Control

**Decision:** Nearby opponents degrade control quality through a measurable, spatial query-based system â€” not arbitrary "under pressure" flags.

**Rationale:**
- Pressure is physical â€” closer defenders are genuinely more threatening
- Spatial query enables transparency â€” player can observe defender positions
- Inverse-square falloff matches intuition â€” doubling distance quarters the effect
- Stacking opponents compounds pressure â€” defending in numbers is rewarded

**Pressure Radius:** 3.0 meters â€” represents the distance at which an opponent's presence begins to affect ball reception.

**Derivation Rationale:**
- At 3.0m distance, an opponent sprinting at 7 m/s can close in ~0.43 seconds
- Average first touch takes 0.2â€“0.4 seconds to complete
- This creates genuine threat: defender can contest if touch is heavy
- Value is subject to gameplay tuning; 3.0m is initial estimate based on:
  - FIFA/PES "pressure" indicator typically activates at ~3m
  - Real-world coaching defines "close down" as <3m
  - Balances gameplay tension without making every touch contested

### 1.4.4 Touch Direction Respects Agent Intent

**Decision:** The agent's movement command (`MovementCommand.TargetPosition`) determines where they are **trying** to direct the ball. Control quality determines how **accurately** they achieve that intent.

**Rationale:**
- Perfect control â†’ ball goes exactly where intended
- Poor control â†’ ball deflects toward original trajectory (physics wins)
- This creates meaningful skill expression â€” elite players can direct the ball while receiving
- It also creates risk/reward â€” attempting a difficult touch direction with poor control leads to loose balls

**Implementation:**
```
ActualTouchDirection = Lerp(IntendedDirection, IncomingBallDirection, 1.0 - ControlQuality)
```

At quality 1.0, actual direction equals intended direction. At quality 0.0, the ball continues largely in its original trajectory.

### 1.4.5 Body Orientation Matters

**Decision:** "Half-turn" stance (body at 45Â° Â± 15Â° to ball approach vector) provides **+15% effective Technique bonus**.

**Rationale:**
- Per Master Volume 1 Â§6: "Half-Turn enables faster next action"
- This encourages realistic receiving technique â€” facing sideways allows vision of field
- Players who blindly face the ball (0Â°) or fully turn away (90Â°+) receive no bonus
- The 15% bonus is significant but not overwhelming â€” good positioning enhances but doesn't replace technique

**Detection Logic:**
```
AngleToball = AngleBetween(AgentFacingDirection, IncomingBallDirection)
IsHalfTurn = (AngleToBall >= 30Â°) AND (AngleToBall <= 60Â°)  // 45Â° Â± 15Â°
OrientationBonus = IsHalfTurn ? 1.15 : 1.00
```

---

## 1.5 Implementation Timeline

### 1.5.1 Stage 0 (Year 1 â€” Current)

**Objective:** Establish mathematically correct control quality model with full integration to dependent systems.

| Deliverable | Description | Completion Criteria |
|-------------|-------------|---------------------|
| Control Quality Calculator | Implements expanded formula from Â§1.2.2 | Unit tests pass for all attribute combinations |
| Touch Radius Mapper | Piecewise linear function with correct thresholds | Output matches Â§1.2.1 table exactly |
| Possession Outcome State Machine | CONTROLLED/LOOSE_BALL/DEFLECTION/INTERCEPTION logic | All state transitions verified |
| Pressure Evaluator | Spatial query integration with falloff function | Pressure values match hand calculations |
| Orientation Detector | Half-turn angle detection with Â±15Â° tolerance | Bonus applies correctly at boundary angles |
| Dribbling State Interface | Signals Agent Movement system | Integration test confirms modifier activation |
| Event Emitter | `FirstTouchEvent` broadcast | Events contain all required data fields |
| Unit Test Suite | Minimum 25 scenarios | 100% pass rate, coverage >80% |
| Integration Test Suite | Minimum 8 scenarios | 100% pass rate |

### 1.5.2 Stage 1 (Year 2)

**Objective:** Visual feedback integration and body part differentiation.

- Animation system consumes touch quality to select appropriate animation
- Body part selection (foot, chest, thigh, head) based on ball height and agent state
- Visual indicators for touch quality (particle effects, sound)
- Skill attribute integration for advanced techniques

### 1.5.3 Stage 2 (Year 3-4)

**Objective:** Advanced techniques and environmental factors.

- Skill moves (drag-back, Cruyff turn, roulette)
- Feint touches (dummy, let ball run)
- Surface condition effects (wet pitch = heavier touches)
- Advanced pressure model (closing speed, angle of approach)
- Touch chains (multiple touches in quick succession)

---

## 1.6 Dependencies

### 1.6.1 Required Inputs (Stage 0)

| Dependency | Source Specification | Data Required | Interface Point |
|------------|---------------------|---------------|-----------------|
| Collision detection | Collision System (#3) | `AgentBallCollisionData` struct with contact point, agent velocity, ball velocity | Â§4.2.6 AgentBallCollisionData |
| Ball state | Ball Physics (#1) | Ball position, velocity, spin for incoming ball characteristics | Â§3.1 BallState |
| Agent attributes | Agent Movement (#2) | `PlayerAttributes.Technique`, `PlayerAttributes.FirstTouch` | Â§3.5.6 PlayerAttributes |
| Agent kinematic state | Agent Movement (#2) | `Agent.Position`, `Agent.Velocity`, `Agent.FacingDirection` | Â§3.5.1 Agent class |
| Spatial query | Collision System (#3) | Nearby opponents within pressure radius via `SpatialHashGrid.Query()` | Â§3.1.4 Query operations |
| Movement command | Agent Movement (#2) | `MovementCommand.TargetPosition` for intended touch direction | Â§3.5.3 MovementCommand |

### 1.6.2 Provided Outputs (Stage 0)

| Consumer | Output Provided | Data Content | Purpose |
|----------|-----------------|--------------|---------|
| Ball Physics (#1) | New `BallState` | Position (displaced), velocity (redirected), spin (modified) | Ball trajectory after touch |
| Agent Movement (#2) | `DribblingModifier` activation | Boolean flag + possession state | Applies dribbling locomotion penalties |
| Event System (#17) | `FirstTouchEvent` | Agent ID, control quality, outcome, touch radius, pressure level | Statistics, replay, AI learning |
| Perception System (#7) | Possession state change | Which team/agent has possession | Decision-making context |

### 1.6.3 Cross-Specification Interface Verification

| Interface | This Spec Section | Other Spec Section | Verification Status |
|-----------|-------------------|--------------------|---------------------|
| `AgentBallCollisionData` | Â§4.1 (input) | Collision System Â§4.2.6 | â³ Pending Collision System approval |
| `BallState` output format | Â§4.2 (output) | Ball Physics Â§3.1 | âœ“ Verified against Ball Physics v1.1 |
| `PlayerAttributes` struct | Â§3.1 (input) | Agent Movement Â§3.5.6 | âœ“ Verified against Agent Movement v1.2 |
| `DribblingModifier` signal | Â§4.3 (output) | Agent Movement Â§6.1.2 | â³ Forward reference |
| Spatial query API | Â§3.4 (input) | Collision System Â§3.1.4 | â³ Pending Collision System approval |

---

## 1.7 Quality Gate Requirements

This section summarizes quality criteria for Section 1 approval. Detailed performance targets and test specifications are defined in Section 5 (Testing) and Section 6 (Performance).

### 1.7.1 Section 1 Approval Criteria

| Criterion | Requirement |
|-----------|-------------|
| Scope boundaries | All responsibilities clearly assigned to owner specification |
| Design decisions | Each decision has documented rationale |
| Dependencies | All required interfaces identified with source specification |
| Exclusions | Permanent exclusions have immutable rationale |
| Deferrals | Each deferred feature has target stage and dependency |

### 1.7.2 Cross-Specification Consistency

| Requirement | Verification Method |
|-------------|---------------------|
| Formula terms match Master Vol 1 intent | Review with lead developer |
| Interface contracts match dependent specs | Diff against current spec versions |
| Attribute names match Agent Movement | Direct comparison to Â§3.5.6 |
| Collision data matches Collision System | Direct comparison to Â§4.2.6 |

---

## 1.8 Section Summary

| Subsection | Key Content |
|------------|-------------|
| **1.1 Document Purpose** | Scope definition objectives |
| **1.2 What This Covers** | Core responsibilities, touch radius thresholds, governing formula, Stage 0 deliverables |
| **1.3 What Is Out of Scope** | Other specs' responsibilities, Stage 1+ deferrals, permanent exclusions |
| **1.4 Key Design Decisions** | Determinism, attribute weighting, pressure model, direction control, orientation bonus |
| **1.5 Implementation Timeline** | Stage 0/1/2 deliverables |
| **1.6 Dependencies** | Required inputs, provided outputs, interface verification |
| **1.7 Quality Gate Requirements** | Approval criteria, cross-specification consistency |

---

## Cross-Reference Verification

| Reference | Target | Status |
|-----------|--------|--------|
| Master Vol 1 Â§6 (Touch Mechanics) | Control quality formula | âœ“ Verified |
| Master Vol 1 Â§6 (Half-Turn) | 15% orientation bonus | âœ“ Verified |
| Ball Physics Spec #1 Â§3.1 | BallState structure | âœ“ Verified |
| Agent Movement Spec #2 Â§3.5.6 | PlayerAttributes.Technique, FirstTouch | âœ“ Verified |
| Collision System Spec #3 Â§4.2.6 | AgentBallCollisionData | â³ Pending approval |
| Collision System Spec #3 Â§3.1.4 | Spatial query API | â³ Pending approval |

---

**End of Section 1**

**Page Count:** ~6 pages  
**Next Section:** Section 2 â€” System Overview (functional requirements, architecture, failure modes)