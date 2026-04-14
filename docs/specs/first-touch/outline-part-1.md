# First Touch Mechanics Specification #4 â€” Detailed Outline

**Purpose:** Planning document for First Touch Mechanics specification, establishing scope, structure, and technical approach before full drafting begins.

**Created:** February 16, 2026, 7:45 PM PST  
**Version:** 1.0  
**Status:** REFERENCE ONLY — SUPERSEDED by Sections 1–9  
**Specification Number:** 4 of 20 (Stage 0)  
**Estimated Effort:** ~30 hours  
**Dependencies:** Ball Physics (#1), Agent Movement (#2), Collision System (#3)

---

> ⚠️ **SUPERSEDED (March 25, 2026):** This outline was the planning document for First
> Touch Mechanics. All technical content has been superseded by the authoritative
> Sections 1–9. The worked examples in §3.7 use a **draft formula variant** that differs
> from the authoritative Section 3 v1.2 formula in three ways: different movement
> reference (10 vs 7.0), different penalty factor (0.4 vs 0.5), and different pressure
> application structure. **Do not use this document for implementation.** Consult
> Section 3 v1.2 for authoritative formulas and constants.
>
> Performance estimates in this outline (~90 ops) are also superseded by Section 6 v1.0
> (authoritative: ~143 equivalent operations). See Section 6 §6.7 KL-5.

---

## EXECUTIVE SUMMARY

First Touch Mechanics governs what happens when an agent makes contact with the ball â€” specifically, the quality of control, where the ball ends up after the touch, and whether the agent gains possession. This is the bridge between Collision System (which detects contact) and Ball Physics (which handles ball trajectory).

**Core concept from Master Vol 1 Â§6.4:**
```
Control_Quality = Agent_Technique / (Ball_Velocity Ã— Agent_Inertia)
```

**Outcomes:**
- Perfect control: Ball "sticks" within 0.3m radius
- Heavy touch: Ball escapes up to 2.0m (allows defender intercept)

---

## SECTION 1: PURPOSE & SCOPE (~2 pages)

### 1.1 What This Specification Covers

- Ball reception quality when agent contacts ball
- Control radius and possession determination
- "Heavy touch" vs. "close control" physics
- Touch direction and weight based on agent intent
- Technique and First Touch attribute integration
- Pressure effects on control quality
- Body orientation bonus ("half-turn" mechanic per Master Vol 1)

### 1.2 What Is OUT of Scope

#### 1.2.1 Responsibilities Owned by Other Specifications

| Responsibility | Owner Specification | Interface |
|----------------|---------------------|-----------|
| Ball trajectory physics after touch | Ball Physics (#1) | First Touch outputs new ball state; Ball Physics simulates |
| Agent movement and positioning | Agent Movement (#2) | First Touch queries agent state; does not modify movement |
| Contact detection | Collision System (#3) | Collision detects contact; First Touch evaluates quality |
| Pass execution (kicking the ball) | Pass Mechanics (#5) | Pass Mechanics initiates ball movement; First Touch receives it |
| Dribbling locomotion penalties | Agent Movement (#2) Â§6.1.2 | First Touch activates dribbling state; Agent Movement applies penalties |
| Aerial first touch (heading) | Heading Mechanics (#9) | Ball above 0.5m handled by Heading Mechanics |
| Goalkeeper catching/parrying | Goalkeeper Mechanics (#10) | Catching is distinct from "first touch" control |

#### 1.2.2 Stage 1+ Deferrals

| Feature | Deferral Reason | Target Stage |
|---------|-----------------|--------------|
| Animation-driven touch types | Requires animation system integration | Stage 1 |
| Body part selection (foot/chest/thigh) | Stage 0 uses simplified "torso" contact | Stage 1 |
| Skill moves (drag-back, Cruyff turn) | Advanced technique requiring animation | Stage 2 |
| Feint touches (dummy, let ball run) | Requires decision integration | Stage 2 |
| Surface condition effects on touch | Requires pitch condition system | Stage 2 |

#### 1.2.3 Permanent Exclusions

| Feature | Exclusion Rationale |
|---------|---------------------|
| Random "error" dice rolls | Control quality is deterministic from inputs; no arbitrary bad touches |
| Unrealistic "super touch" | Even elite players bounded by physics; no ball-magnet abilities |
| Touch quality affected by "luck" | All factors are measurable and reproducible |

### 1.3 Key Design Decisions

1. **Control quality is deterministic** â€” Given identical inputs (attributes, ball velocity, pressure, orientation), output is always identical. No hidden randomness.

2. **Technique is primary, First Touch is secondary** â€” Weighting: 70% Technique, 30% First Touch attribute. Technique represents general ball mastery; First Touch is specialization.

3. **Pressure proximity degrades control** â€” Measured via spatial query, not arbitrary. Closer opponents = worse control.

4. **Touch direction respects agent intent** â€” Agent's movement command determines where they're trying to direct the ball. Control quality determines how accurately they achieve it.

5. **Body orientation matters** â€” "Half-turn" stance (45Â° to ball approach) provides measurable bonus, encouraging realistic receiving technique.

### 1.4 Implementation Timeline

**Stage 0 (Year 1 â€” Current):**
- Core control quality model with attribute integration
- Touch radius calculation and ball displacement
- Possession transfer logic
- Pressure context evaluation
- Body orientation bonus
- Integration with Collision System and Ball Physics
- Unit and integration tests

**Stage 1 (Year 2):**
- Animation system consumes touch type data
- Body part differentiation (foot, chest, thigh, head)
- Visual feedback for touch quality
- Skill attribute integration for advanced techniques

**Stage 2 (Year 3-4):**
- Skill moves and tricks
- Feint touches (dummy, let ball run)
- Surface condition effects (wet pitch = heavier touches)
- Advanced pressure modeling (closing speed, angle of approach)

### 1.5 Dependencies

**Required (Stage 0):**

| Dependency | Source | What We Need |
|------------|--------|--------------|
| Collision detection | Collision System (#3) | `AgentBallCollisionData` struct with contact point, agent velocity |
| Ball state | Ball Physics (#1) | Ball position, velocity for incoming ball speed |
| Agent attributes | Agent Movement (#2) | Technique, First Touch attributes from `AgentAttributes` |
| Agent state | Agent Movement (#2) | Position, velocity, facing direction |
| Spatial query | Collision System (#3) | Nearby opponents for pressure calculation |

**Provided to (Stage 0):**

| Consumer | What We Provide |
|----------|-----------------|
| Ball Physics (#1) | New ball state after touch (position, velocity) |
| Agent Movement (#2) | Dribbling state activation (`DribblingModifier`) |
| Event System (#17) | `FirstTouchEvent` for replay/statistics |

---

## SECTION 2: SYSTEM OVERVIEW (~3 pages)

### 2.1 Functional Requirements

**FR-1: Control Quality Calculation**
- Implement control quality formula from Master Vol 1 Â§6.4
- Input: Agent attributes (Technique, First Touch), ball velocity, agent velocity, pressure context
- Output: Control quality score [0.0 - 1.0]

**FR-2: Touch Radius Mapping**
- Map control quality to ball displacement distance
- Perfect control (quality â‰¥ 0.85): Ball within 0.3m of agent
- Good control (quality â‰¥ 0.60): Ball within 0.6m
- Poor control (quality â‰¥ 0.35): Ball within 1.2m
- Heavy touch (quality < 0.35): Ball up to 2.0m away

**FR-3: Body Orientation Bonus**
- Detect "half-turn" stance (body at 45Â° Â± 15Â° to ball approach vector)
- Apply +15% effective Technique bonus for half-turn reception
- Per Master Vol 1 Â§6.4: "Half-Turn" enables faster next action

**FR-4: Pressure Degradation**
- Query nearby opponents within pressure radius (3.0m)
- Calculate pressure scalar based on proximity (inverse square falloff)
- Apply pressure penalty to control quality calculation

**FR-5: Touch Direction Control**
- Agent's intended touch direction from `MovementCommand.TargetPosition`
- Control quality determines accuracy of achieving intended direction
- Poor control: Ball deflects toward original trajectory
- Good control: Ball goes where agent intended

**FR-6: Possession Transfer**
- Determine possession outcome: CONTROLLED, LOOSE_BALL, DEFLECTION, INTERCEPTION
- CONTROLLED: Agent gains possession, ball "at feet", dribbling state activated
- LOOSE_BALL: Ball displaced but contestable by either team
- DEFLECTION: Ball bounces away, momentum preserved
- INTERCEPTION: Nearby opponent can claim ball

**FR-7: Dribbling State Activation**
- When possession is CONTROLLED, activate dribbling modifiers in Agent Movement
- Set `PerformanceContext.DribblingModifier` per Agent Movement Â§6.1.2
- Clear modifier when possession is lost

**FR-8: Determinism**
- All calculations must be deterministic for replay consistency
- No use of System.Random or non-seeded randomness
- Same inputs always produce same outputs

### 2.2 System Architecture

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚                        FIRST TOUCH SYSTEM                          â”‚
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚                                                                     â”‚
â”‚  â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”    â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”    â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”  â”‚
â”‚  â”‚ Collision System â”‚â”€â”€â”€â–¶â”‚ First Touch     â”‚â”€â”€â”€â–¶â”‚ Ball Physics   â”‚  â”‚
â”‚  â”‚ (Input)          â”‚    â”‚ Evaluator       â”‚    â”‚ (Output)       â”‚  â”‚
â”‚  â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜    â””â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”˜    â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜  â”‚
â”‚                                  â”‚                                  â”‚
â”‚                                  â–¼                                  â”‚
â”‚                         â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”                         â”‚
â”‚                         â”‚ Agent Movement â”‚                         â”‚
â”‚                         â”‚ (Dribbling)    â”‚                         â”‚
â”‚                         â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜                         â”‚
â”‚                                                                     â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜

Data Flow:
1. Collision System detects agent-ball contact
2. Packages AgentBallCollisionData and calls First Touch
3. First Touch queries:
   - Agent attributes (Technique, First Touch)
   - Agent state (position, velocity, facing)
   - Ball state (incoming velocity)
   - Pressure context (nearby opponents via spatial hash)
4. First Touch calculates:
   - Control quality score
   - Touch radius (ball displacement)
   - Touch direction (based on intent vs. quality)
   - Possession outcome
5. First Touch outputs:
   - To Ball Physics: New ball state (position, velocity)
   - To Agent Movement: Dribbling modifier activation
   - To Event System: FirstTouchEvent for logging
```

### 2.3 Integration Points

| System | Interface | Direction | Data |
|--------|-----------|-----------|------|
| Collision System (#3) | `AgentBallCollisionData` | Input | Contact point, agent velocity, agent ID |
| Ball Physics (#1) | `IBallControlCallback` | Output | New ball position, velocity, possession state |
| Agent Movement (#2) | `AgentAttributes` | Input | Technique, First Touch attributes |
| Agent Movement (#2) | `PerformanceContext` | Output | DribblingModifier activation |
| Agent Movement (#2) | `MovementCommand` | Input | Intended touch direction |
| Collision System (#3) | Spatial hash query | Input | Nearby opponents for pressure |
| Event System (#17) | `FirstTouchEvent` | Output | Touch data for replay/stats |

### 2.4 Frame Pipeline Position

```
Frame N Pipeline:
  1. Input processing
  2. AI decision making (10Hz heartbeat)
  3. Agent Movement updates positions/velocities
  4. Ball Physics updates ball trajectory
  5. Collision System detects contacts
     5a. Agent-agent collisions â†’ Collision Response
     5b. Agent-ball collisions â†’ First Touch System â† THIS SPEC
  6. First Touch evaluates control quality
  7. Ball Physics applies touch result
  8. Possession state updated
  9. Event System processes FirstTouchEvent
  10. Rendering (interpolated)
```

**Key timing:** First Touch executes AFTER Collision System detects contact but BEFORE Ball Physics finalizes frame state. This ensures the touch result is applied within the same frame as contact.

### 2.5 Failure Modes and Recovery

| Failure Mode | Detection | Recovery | Severity |
|--------------|-----------|----------|----------|
| NaN in ball velocity | `float.IsNaN()` check | Clamp to 0, log warning | Medium |
| Negative attribute values | Range check on input | Clamp to [1, 20] | Low |
| Missing agent data | Null check | Use default attributes (10, 10) | Medium |
| Extreme ball velocity (>50 m/s) | Threshold check | Cap at MAX_VELOCITY | Low |
| No nearby opponents found | Empty query result | Pressure scalar = 0 (no penalty) | None |
| Agent outside pitch bounds | Boundary check | Allow touch, clamp ball result to pitch | Low |

**Recovery philosophy:** Fail gracefully with sensible defaults. Log warnings but don't crash. Determinism preserved even in edge cases.

---

