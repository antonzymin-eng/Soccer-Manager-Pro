# First Touch Mechanics Specification #4 â€” Section 2: System Overview

**Purpose:** Establishes functional requirements, system architecture, frame pipeline position,
data flow, touch outcome taxonomy, performance budget, and failure mode recovery for the
First Touch Mechanics system. This section defines the "what" and "why" before Section 3
defines the "how."

**Created:** February 17, 2026, 6:00 PM PST
**Version:** 1.1
**Status:** Approved
**Author:** Claude (AI) with Anton (Lead Developer)
**Specification Number:** 4 of 20 (Stage 0 Physics Foundation)
**Prerequisite:** Section 1 (Purpose & Scope) v1.0

---

## 2.1 Functional Requirements

Seven functional requirements govern the First Touch Mechanics system for Stage 0. Each
requirement derives from the core responsibilities established in Section 1.2, and each maps
to a sub-system in Section 3 with explicit test coverage.

| FR ID | Short Name | Priority | Section 3 Owner | Test IDs |
|-------|------------|----------|-----------------|----------|
| FR-01 | Control Quality Calculation | CRITICAL | Â§3.1 | CQ-001â€”CQ-012 |
| FR-02 | Touch Radius Determination | CRITICAL | Â§3.2 | TR-001â€”TR-008 |
| FR-03 | Ball Displacement | CRITICAL | Â§3.3 | BD-001â€”BD-010 |
| FR-04 | Possession Transfer Logic | CRITICAL | Â§3.4 | PT-001â€”PT-008 |
| FR-05 | Pressure Evaluation | HIGH | Â§3.5 | PE-001â€”PE-007 |
| FR-06 | Body Orientation Detection | HIGH | Â§3.6 | BO-001â€”BO-005 |
| FR-07 | Event Emission | MEDIUM | Â§3.7 | IT-001â€”IT-008 |

---

### FR-01 â€” Control Quality Calculation

**Statement:** The system shall compute a deterministic control quality scalar `q âˆˆ [0.0, 1.0]`
for every agent-ball contact event, using the governing formula from Master Vol 1 Â§6.4.
Identical inputs shall always produce identical output. No stochastic elements are permitted.

**Inputs:**
- `Agent.Technique` â€” integer [1â€“20], primary attribute (70% weight)
- `Agent.FirstTouch` â€” integer [1â€“20], secondary attribute (30% weight)
- `ball.Speed` â€” float, incoming ball speed in m/s
- `agent.Speed` â€” float, agent speed at moment of contact in m/s
- `pressureScalar` â€” float [0.0â€“1.0], output of FR-05
- `orientationBonus` â€” float [0.0, +0.15], output of FR-06

**Output:**
- `q` â€” float [0.0, 1.0], normalised control quality

**Rationale:** Control quality is the single most important value the system produces.
Everything downstream (touch radius, displacement direction, possession outcome) is derived
from or constrained by `q`. Determinism is non-negotiable: multiplayer replay requires
identical simulation across machines (Master Vol 1 Â§1.3).

**Acceptance Criteria:**
1. Output is always in [0.0, 1.0] regardless of input combinations.
2. Given identical inputs across frames and machines, output is bitwise identical.
3. Elite agent (Technique 20, First Touch 20) receiving slow ball (5 m/s) with no pressure
   produces `q â‰¥ 0.85`.
4. Poor agent (Technique 5, First Touch 5) receiving fast ball (30 m/s) with heavy pressure
   produces `q â‰¤ 0.25`.
5. Technique attribute has demonstrably greater influence than First Touch attribute on
   output value (70/30 weighting).

**Failure Consequence:** If control quality is wrong, every downstream calculation is
contaminated â€” this is a critical failure path.

---

### FR-02 â€” Touch Radius Determination

**Statement:** The system shall compute a touch radius `r âˆˆ [0.10m, 2.0m]` that defines the
maximum displacement of the ball from the agent's control point after the touch. Higher
control quality shall monotonically map to smaller radius (tighter control).

**Inputs:**
- `q` â€” control quality from FR-01
- `ball.Speed` â€” float, ball speed at contact (high speed = harder to control tightly)

**Output:**
- `r` â€” float [0.10m, 2.0m], touch radius in metres

**Monotonicity requirement:** `q1 > q2` must imply `r(q1) < r(q2)` without exception.
Monotonicity violations would produce counter-intuitive gameplay where skilled agents
occasionally produce worse touches than unskilled agents.

**Boundary constants:**
- `MIN_TOUCH_RADIUS = 0.10m` â€” minimum even for perfect control (ball always moves slightly)
- `MAX_TOUCH_RADIUS = 2.0m` â€” maximum (beyond this, contact type becomes LOOSE_BALL or
  DEFLECTION, not a controlled touch)

**Rationale for 0.10m minimum:** Even elite players' perfectly executed first touches
displace the ball some distance. A radius of zero would imply the ball is motionless at the
agent's foot after contact, which is physically unrealistic.

**Rationale for 2.0m maximum:** At 2.0m+ displacement the agent has not controlled the ball
in any meaningful sense. Possession transfer logic (FR-04) classifies this as LOOSE_BALL.

**Acceptance Criteria:**
1. `q = 1.0` produces `r â‰¤ 0.15m`.
2. `q = 0.0` produces `r = 2.0m`.
3. Function is monotonically decreasing across all sampled values of q.
4. High-speed ball (>25 m/s) increases `r` relative to identical `q` at low speed.

---

### FR-03 â€” Ball Displacement

**Statement:** The system shall compute a new ball position and velocity after the touch,
combining the agent's intended touch direction (derived from movement command) with an
angular error term scaled inversely by control quality.

**Inputs:**
- `agent.MovementCommand` â€” desired touch direction intent (unit vector)
- `q` â€” control quality
- `r` â€” touch radius
- `ball.Position` â€” current position
- `ball.Velocity` â€” incoming velocity

**Outputs:**
- `newBallPosition` â€” Vector3, ball position after touch
- `newBallVelocity` â€” Vector3, ball velocity after touch (passed to Ball Physics)

**Angular error model:** Perfect control (`q = 1.0`) produces zero angular deviation from
intended direction. Zero control (`q = 0.0`) produces maximum angular deviation (Â±45Â°).
Error is linearly interpolated between these bounds as a function of `1 - q`.

**Rationale for Â±45Â° maximum:** Beyond 45Â° the ball is no longer travelling in a direction
the agent intended, which is consistent with "the touch has completely failed." Larger maximum
angles were considered but rejected as producing physically implausible outcomes â€” a player
cannot hit a ball 180Â° from intention due to physics of foot-ball contact.

**Acceptance Criteria:**
1. At `q = 1.0`, actual direction is within 2Â° of intended direction.
2. At `q = 0.0`, actual direction is within Â±45Â° of intended direction (statistical
   distribution, not fixed value â€” verified by running 1,000 evaluations with different seeds).
3. Output ball position respects pitch boundary (no negative or out-of-bounds values).
4. Ball velocity magnitude after touch is physically plausible (â‰¤ incoming speed Ã— 0.8,
   accounting for energy loss at contact).

---

### FR-04 â€” Possession Transfer Logic

**Statement:** The system shall determine the possession outcome state for every agent-ball
contact event. The outcome state machine has four states: CONTROLLED, LOOSE_BALL,
DEFLECTION, and INTERCEPTION.

**Inputs:**
- `q` â€” control quality
- `r` â€” touch radius
- `newBallPosition` â€” ball position after touch
- `contactType` â€” from `AgentBallCollisionData.ContactType` (intentional vs. inadvertent)
- `team` â€” agent team identifier

**Output:**
- `PossessionOutcome` â€” enum {CONTROLLED, LOOSE_BALL, DEFLECTION, INTERCEPTION}

**State definitions:**

| State | Trigger Condition | Downstream Effect |
|-------|------------------|-------------------|
| CONTROLLED | `q â‰¥ 0.55` AND `r â‰¤ 0.8m` | Agent enters dribbling state; `DribblingModifier` sent to Agent Movement |
| LOOSE_BALL | `q < 0.55` OR `r > 0.8m` AND `r â‰¤ 2.0m` | Ball is free; no possession assigned; both teams can contest |
| DEFLECTION | Contact was inadvertent (body contact, not intentional control attempt) | Ball Physics handles trajectory; no possession assigned |
| INTERCEPTION | Contact with ball belonging to opposing team; `q â‰¥ 0.35` | Ball trajectory redirected; next frame's collision assigns new possession |

**CONTROLLED threshold rationale:** 0.55 was selected as the midpoint between "acceptable
professional first touch" and "barely making contact." An elite player (Technique 18+) under
no pressure receives a pass and should be CONTROLLED in the vast majority of cases. Threshold
validated against expected attribute distributions in Section 5 scenarios.

**INTERCEPTION threshold rationale:** Interception requires lower quality threshold (0.35 vs
0.55) because a deflection away from the opponent is a successful interception even if not
perfectly controlled. The intercepting agent has not yet controlled the ball â€” that evaluation
occurs in the next frame.

**Acceptance Criteria:**
1. CONTROLLED is produced in â‰¥ 80% of cases when Technique â‰¥ 15 and ball speed â‰¤ 15 m/s
   with no pressure.
2. LOOSE_BALL is produced in â‰¥ 70% of cases when Technique â‰¤ 8 and ball speed â‰¥ 25 m/s.
3. DEFLECTION never assigns possession to either team.
4. INTERCEPTION always sets the intercepting team as possessors in the next frame.
5. All four states are reachable by test scenarios in Section 5.

---

### FR-05 â€” Pressure Evaluation

**Statement:** The system shall compute a pressure scalar `p âˆˆ [0.0, 1.0]` representing the
degrading effect of nearby opposing agents on control quality. Pressure uses inverse-square
falloff from each opponent within a defined pressure radius.

**Inputs:**
- `agent.Position` â€” receiving agent position
- List of `opponent.Position` values within `PRESSURE_RADIUS = 5.0m`
  (obtained via Collision System Â§3.1.4 spatial query)

**Output:**
- `pressureScalar` â€” float [0.0, 1.0] where 1.0 = maximum pressure (control quality
  degraded by maximum amount)

**Falloff model:** Inverse-square from opponent to agent. Contributions from multiple
opponents are summed and clamped to 1.0. This ensures:
- A single opponent at 1.0m exerts significant pressure (~0.75 scalar)
- A single opponent at 5.0m (boundary) exerts minimal pressure (~0.04 scalar)
- Multiple distant opponents stack to meaningful pressure (realistic crowding effect)

**Rationale for 5.0m pressure radius:** Beyond 5.0m, a physically capable professional
footballer can receive a pass without meaningful pressure from a standing start. 5.0m represents
approximately one stride + reaction time (â‰ˆ0.5s Ã— 9 m/s sprint speed). This is consistent
with sports science literature on pressing effectiveness (Gegenpressing models typically cite
5â€“6m as critical pressing distance).

**Rationale for inverse-square falloff:** Linear falloff would understate the effect of very
close opponents and overstate distant ones. Inverse-square matches physical intuition: an
opponent at 1.0m is four times more distracting than one at 2.0m, not twice as distracting.

**Acceptance Criteria:**
1. Single opponent at 1.0m produces pressureScalar â‰¥ 0.70.
2. Single opponent at 5.0m produces pressureScalar â‰¤ 0.05.
3. Two opponents at 2.5m each produce combined scalar â‰¥ 0.50.
4. No opponents within 5.0m produces pressureScalar = 0.0 exactly.
5. Clamping ensures pressureScalar never exceeds 1.0 regardless of opponent count.

---

### FR-06 â€” Body Orientation Detection

**Statement:** The system shall detect when an agent is in a "half-turn" receiving stance
(body oriented approximately 45Â° to the ball approach direction) and apply a bonus of +15%
to control quality, as specified in Master Vol 1 Â§6 (Half-Turn). The bonus shall not exceed
+0.15 regardless of how close to 45Â° the orientation is.

**Inputs:**
- `agent.FacingDirection` â€” agent facing unit vector
- `ball.Velocity` â€” ball approach direction (negated to get approach vector toward agent)

**Output:**
- `orientationBonus` â€” float [0.0, +0.15]

**Half-turn definition:** Agent facing direction is between 30Â° and 60Â° relative to the
ball's incoming direction. This represents a player receiving the ball "over the shoulder"
or "across the body" â€” the canonical half-turn technique used by technically skilled players
to open up the pitch on receipt.

**Angle tolerance:** Â±15Â° around the 45Â° ideal (i.e., 30Â°â€“60Â° range) receives the full +0.15
bonus. Outside this range the bonus is zero (no partial credit). This binary approach is
chosen over a smooth interpolation to keep the implementation simple and the gameplay
reward clear â€” players are either in good position or they are not.

**Rationale for 15% bonus:** Master Vol 1 Â§6 specifies this value. It is meaningful enough
to reward correct technique without being so large that orientation gaming dominates play.
A 15% bonus is sufficient to shift a marginal touch (q â‰ˆ 0.50) into CONTROLLED territory
for a player of average Technique.

**Acceptance Criteria:**
1. Agent at exactly 45Â° to ball approach receives full +0.15 bonus.
2. Agent at 0Â° (facing directly at ball) receives 0.0 bonus.
3. Agent at 90Â° (facing perpendicular) receives 0.0 bonus.
4. Agent at 30Â° and 60Â° (boundaries) receives full +0.15 bonus.
5. Agent at 29Â° and 61Â° receives 0.0 bonus (hard boundary).
6. Bonus is additive to base control quality before normalisation clamping.

---

### FR-07 â€” Event Emission

**Statement:** The system shall emit a `FirstTouchEvent` struct for every agent-ball contact
evaluation. The event shall be published to the event queue for consumption by the Statistics
system, Replay system, and any future UI consumers.

**Output:** `FirstTouchEvent` struct (see Section 4 for full definition), containing:
- `AgentId` â€” which agent made the touch
- `ControlQuality` â€” computed `q` value
- `PossessionOutcome` â€” resulting state
- `TouchRadius` â€” computed `r` value
- `BallPositionBefore` â€” ball position at contact
- `BallPositionAfter` â€” ball position after touch
- `PressureScalar` â€” pressure at time of touch
- `OrientationBonus` â€” whether half-turn bonus was applied
- `FrameNumber` â€” for replay synchronisation

**Rationale:** Event emission is essential for two purposes: (1) Statistics and match data
require every touch be recorded for possession %, touch map, and touch quality analytics;
(2) Replay determinism requires the event log to be sufficient to reconstruct the match
without re-running the simulation.

**Acceptance Criteria:**
1. Every agent-ball contact produces exactly one `FirstTouchEvent`.
2. No events are dropped under normal operating conditions.
3. Event data is sufficient for replay reconstruction without additional state.
4. `FrameNumber` is monotonically increasing across events in a single match.

---

> **Note (v1.1):** The Outline's FR-08 (Determinism) is not tracked as a separate
> functional requirement in this section. Determinism is a cross-cutting invariant
> enforced via FR-01's acceptance criteria ("identical inputs produce bitwise-identical
> output"), the Master Vol 1 §1.3 constraint, and Section 3 §3.1.4. Every FR's
> acceptance criteria implicitly requires determinism. Tracking it as a separate FR
> would create redundant test coverage without additional verification value.

---

## 2.2 System Architecture

### 2.2.1 Architectural Position

First Touch Mechanics is a **discrete event processor** â€” it does not execute every frame.
It executes only when the Collision System reports an `AGENT_BALL` collision event. In a
typical 90-minute match at 60 Hz, First Touch Mechanics executes approximately 200â€“400 times
total (not 324,000 times per match). This architectural decision has significant performance
implications: the system has a generous per-evaluation budget because evaluations are infrequent.

```
Frame Loop Position:
  Step 1: Input Processing
  Step 2: Agent Movement (FR-2 through FR-6)
  Step 3: Ball Physics (gravity, drag, Magnus)
  Step 4: Collision System (broad phase â†’ narrow phase â†’ events)
  Step 5: *** FIRST TOUCH MECHANICS *** â† This specification
           (triggered only on AGENT_BALL collision events)
  Step 6: Ball Physics update (apply new ball state from touch)
  Step 7: Event Queue flush (statistics, replay, UI)
```

**Step ordering rationale:**
- First Touch must execute AFTER Collision System (needs `AgentBallCollisionData`)
- First Touch must execute BEFORE Ball Physics Step 6 (provides new ball state)
- Ball Physics Step 6 consumes the new ball state produced by First Touch
- Event Queue flushes AFTER all systems have processed, ensuring complete events

### 2.2.2 Sub-System Decomposition

The First Touch Mechanics system is decomposed into five sub-systems. Each sub-system maps
to one or more functional requirements and has a corresponding Section 3 technical specification.

```
FirstTouchMechanics
â”œâ”€â”€ ControlQualityCalculator        (FR-01)
â”‚     â”œâ”€â”€ AttributeWeighter         [Â§3.1.1]
â”‚     â”œâ”€â”€ VelocityDifficultyScaler  [Â§3.1.2]
â”‚     â””â”€â”€ QualityNormaliser         [Â§3.1.3]
â”œâ”€â”€ TouchRadiusCalculator           (FR-02)
â”‚     â””â”€â”€ PiecewiseInterpolator     [Â§3.2.1]
â”œâ”€â”€ BallDisplacementResolver        (FR-03)
â”‚     â”œâ”€â”€ DirectionIntentExtractor  [Â§3.3.1]
â”‚     â””â”€â”€ AngularErrorApplier       [Â§3.3.2]
â”œâ”€â”€ PossessionStateMachine          (FR-04)
â”‚     â””â”€â”€ OutcomeClassifier         [Â§3.4.1]
â”œâ”€â”€ PressureEvaluator               (FR-05)
â”‚     â””â”€â”€ InverseSquareSummer       [Â§3.5.1]
â”œâ”€â”€ OrientationDetector             (FR-06)
â”‚     â””â”€â”€ HalfTurnChecker           [Â§3.6.1]
â””â”€â”€ EventEmitter                    (FR-07)
      â””â”€â”€ FirstTouchEventBuilder    [Â§3.7.1]
```

### 2.2.3 Evaluation Pipeline

Within a single First Touch evaluation, sub-systems execute in strict sequential order.
Each sub-system depends on the output of the preceding one.

```
AGENT_BALL collision event received
         â”‚
         â–¼
  [1] Height Guard
       Ball.Position.y > GROUND_CONTROL_HEIGHT (0.5m)?
           YES â†’ Route to Heading Mechanics (Spec #9); EXIT
           NO  â†’ continue
         â”‚
         â–¼
  [2] OrientationDetector
       Input:  agent.FacingDirection, ball.Velocity
       Output: orientationBonus âˆˆ [0.0, +0.15]
         â”‚
         â–¼
  [3] PressureEvaluator
       Input:  agent.Position, spatial query (nearby opponents)
       Output: pressureScalar âˆˆ [0.0, 1.0]
         â”‚
         â–¼
  [4] ControlQualityCalculator
       Input:  agent.Technique, agent.FirstTouch, ball.Speed,
               agent.Speed, pressureScalar, orientationBonus
       Output: q âˆˆ [0.0, 1.0]
         â”‚
         â–¼
  [5] TouchRadiusCalculator
       Input:  q, ball.Speed
       Output: r âˆˆ [0.10m, 2.0m]
         â”‚
         â–¼
  [6] BallDisplacementResolver
       Input:  agent.MovementCommand, q, r, ball.Position, ball.Velocity
       Output: newBallPosition (Vector3), newBallVelocity (Vector3)
         â”‚
         â–¼
  [7] PossessionStateMachine
       Input:  q, r, contactType, team, newBallPosition
       Output: PossessionOutcome enum
         â”‚
         â–¼
  [8] EventEmitter
       Input:  all computed values
       Output: FirstTouchEvent â†’ event queue
         â”‚
         â–¼
  [9] Output Dispatch
       â†’ Ball Physics: (newBallPosition, newBallVelocity)
       â†’ Agent Movement: (DribblingModifier if CONTROLLED)
       EXIT
```

---

## 2.3 Data Flow

### 2.3.1 Inbound Interfaces

Data consumed by First Touch Mechanics from dependent specifications:

| Data | Source Spec | Source Location | Type | Notes |
|------|-------------|-----------------|------|-------|
| `AgentBallCollisionData` | Collision System (#3) | Â§4.2.6 | struct | Primary trigger; contains contact point, agent velocity, contact type |
| `BallState` | Ball Physics (#1) | Â§3.1 | struct | Ball position, velocity, angular velocity at contact |
| `PlayerAttributes.Technique` | Agent Movement (#2) | Â§3.5.6 | int [1â€“20] | Primary control quality input |
| `PlayerAttributes.FirstTouch` | Agent Movement (#2) | Â§3.5.6 | int [1â€“20] | Secondary control quality input |
| `Agent.Position` | Agent Movement (#2) | Â§3.5.1 | Vector3 | Used for pressure radius query |
| `Agent.Velocity` | Agent Movement (#2) | Â§3.5.1 | Vector3 | Agent movement difficulty term |
| `Agent.FacingDirection` | Agent Movement (#2) | Â§3.5.1 | Vector2 | Half-turn orientation check |
| `Agent.MovementCommand` | Agent Movement (#2) | Â§3.5.2 | Vector3 | Intended touch direction |
| `SpatialQuery(radius)` | Collision System (#3) | Â§3.1.4 | API call | Returns nearby opponents for pressure |

**âš  Pending Verification:** `AgentBallCollisionData` (Â§4.2.6) and `SpatialQuery` API (Â§3.1.4)
are marked as pending approval in Collision System Spec #3. Interface contracts in this
specification are written against the Collision System outline. A cross-reference verification
pass is required once Collision System Spec #3 is approved.

### 2.3.2 Outbound Interfaces

Data produced by First Touch Mechanics for downstream consumers:

| Data | Consumer Spec | Consumer Location | Type | Notes |
|------|---------------|------------------|------|-------|
| `newBallState` (position + velocity) | Ball Physics (#1) | Â§3.1 | BallState | Ball Physics applies this as new state post-touch |
| `DribblingModifier` | Agent Movement (#2) | Â§6.1.2 | struct | Activates dribbling locomotion penalties when CONTROLLED |
| `FirstTouchEvent` | Event System (#17) | TBD (Stage 1) | struct | Touch record for statistics, replay, UI |

### 2.3.3 Coordinate System

All spatial values conform to Ball Physics Â§3.1.1 coordinate definitions:

| Axis | Direction | Notes |
|------|-----------|-------|
| X | Along pitch length | Positive = attacking direction (Team A) |
| Y | Vertical (height) | Positive = up. Y = 0 is pitch surface |
| Z | Across pitch width | Positive = right side of pitch (from Team A perspective) |

**GROUND_CONTROL_HEIGHT constant:** `0.5m` in Y axis. Ball centre above this value is routed
to Heading Mechanics (Spec #9). Ball centre at or below 0.5m is eligible for First Touch
evaluation. This constant is defined in Section 4 and referenced by the height guard in Â§2.2.3.

---

## 2.4 Touch Outcome Taxonomy

This section formally defines the four touch outcome states and their transition conditions.
This taxonomy is the authoritative reference; FR-04 (Possession Transfer Logic) implements it.

### 2.4.1 CONTROLLED

**Definition:** The agent has successfully brought the ball under control. The ball is within
immediate playing distance and the agent is the designated possessor.

**Conditions:**
- Control quality `q â‰¥ 0.55`
- Touch radius `r â‰¤ 0.80m`
- Contact type was intentional (agent was attempting to receive the ball)

**Downstream effects:**
- Agent Movement receives `DribblingModifier` (activates dribbling locomotion penalties)
- Ball Physics places ball at `newBallPosition` with `newBallVelocity`
- Possession assigned to receiving agent's team

**Football context:** Represents a clean, controlled reception â€” the agent can immediately
make the next decision (pass, shoot, dribble) without first needing to recover the ball.

### 2.4.2 LOOSE_BALL

**Definition:** Contact was made but the ball has not been controlled. No agent holds possession.
Both teams must contest for the ball.

**Conditions:**
- Control quality `q < 0.55`, OR
- Touch radius `r > 0.80m` (ball bounced too far away)

**Downstream effects:**
- No possession assigned
- Ball Physics handles trajectory from `newBallPosition` / `newBallVelocity`
- Collision System will detect next agent-ball contact and trigger new evaluation

**Football context:** A heavy touch, a mis-control, or a poor reception. The ball is live
and contested â€” common result of rushed touches under pressure.

### 2.4.3 DEFLECTION

**Definition:** The ball struck an agent incidentally (not an intentional control attempt).
The contact was inadvertent â€” the ball deflected off an agent's body without deliberate action.

**Conditions:**
- `AgentBallCollisionData.ContactType == INADVERTENT`

**Downstream effects:**
- No possession assigned
- Ball Physics applies collision response (Collision System handles the physics via normal
  agent-ball collision response; First Touch does not alter trajectory for deflections)
- No `DribblingModifier` emitted

**Football context:** A deflection off a leg or body that the player did not intend â€” ricochets
off a defender, accidental contact, goalkeeper body block without catch attempt.

**Important scope note:** DEFLECTION evaluation is minimal in First Touch Mechanics. The
physical trajectory is handled entirely by Collision System's collision response. First Touch
is invoked only to record the `FirstTouchEvent` and confirm no possession transfer occurs.

### 2.4.4 INTERCEPTION

**Definition:** An agent has made contact with a ball that was intended for an opposing team
agent, successfully diverting it. The intercepting agent has not yet controlled the ball â€”
possession is resolved in the next frame.

**Conditions:**
- Ball was in possession of or directed toward an opposing team agent
- Intercepting agent's control quality `q â‰¥ 0.35` (lower threshold; see FR-04 rationale)
- The intercepting agent's team did not have possession at time of contact

**Downstream effects:**
- Ball trajectory is redirected (newBallPosition / newBallVelocity set toward intercepting
  agent's open space or movement direction)
- No immediate possession assignment â€” next frame's collision will determine CONTROLLED or
  LOOSE_BALL for the intercepting agent
- `FirstTouchEvent` records outcome as INTERCEPTION

**Interception chain rule:** When INTERCEPTION occurs, the intercepting agent does NOT receive
an immediate First Touch evaluation in the same frame. The next frame's Collision System
detects agent-ball contact and triggers a new evaluation. This rule prevents infinite
recursion and maintains strict frame-by-frame determinism (see Critical Issue #4 resolution,
Outline Â§Critical Issues).

**Football context:** A defender stepping in to intercept a pass, or a pressing player
winning the ball before it reaches the intended recipient.

---

## 2.5 Performance Budget

First Touch Mechanics is a discrete event processor and is not in the per-frame hot path.
Its performance characteristics are therefore measured differently from per-frame systems.

### 2.5.1 Budget Context

The overall physics frame budget is 6ms at 60 Hz per Master Vol 4 Â§3.2. First Touch
executes approximately 200â€“400 times per 90-minute match, not every frame. The maximum
per-evaluation budget is generous by design.

| Budget Type | Value | Rationale |
|-------------|-------|-----------|
| Per-evaluation target | < 0.05ms | Comfortable within single frame; no frame impact |
| Per-evaluation ceiling | < 0.20ms | Absolute maximum; still within 1 frame at 60 Hz |
| Frame budget impact | ~0.01ms average | 3â€“4 touches per second peak Ã— 0.03ms each |
| Total match impact | < 20ms | 400 evaluations Ã— 0.05ms = 20ms across 90 min |

**Note:** These are estimates pending profiling. Operation count analysis is provided in
Section 6 (Performance Analysis) for validation. Actual profiling will occur during Stage 0
implementation.

### 2.5.2 Memory Budget

All First Touch data structures are stack-allocated or pooled. No heap allocations occur in
the hot path.

| Structure | Size | Allocation |
|-----------|------|------------|
| `FirstTouchContext` (inputs) | ~128 bytes | Stack |
| `FirstTouchResult` (outputs) | ~80 bytes | Stack |
| `FirstTouchEvent` (emitted) | ~96 bytes | Event queue pool |
| Constants block | ~200 bytes | Static (load once) |

**Total heap allocation per touch:** Zero. All evaluation state is stack-based.
Event queue is pre-allocated pool (capacity defined by Event System Â§17, Stage 1).

---

## 2.6 Failure Modes

Four failure modes are defined for Stage 0. Each has a defined detection condition and
recovery procedure. The overriding principle is: **never corrupt ball or agent state;
prefer a safe default outcome over a partially computed outcome.**

### 2.6.1 FM-01: Control Quality Out of Range

**Trigger:** `q` outside [0.0, 1.0] after computation.

**Possible causes:**
- NaN propagation from attribute values (if an attribute was never initialised)
- Floating-point overflow if ball speed exceeds expected range (e.g., ball speed > 100 m/s
  due to upstream bug in Ball Physics)
- Incorrect formula coefficients producing values > 1.0

**Detection:** Assert `q >= 0.0f && q <= 1.0f && !float.IsNaN(q)` immediately after calculation.

**Recovery:**
1. Log detailed error: agent ID, all input values, computed intermediate values, frame number.
2. Clamp `q` to valid range: `q = Mathf.Clamp(q, 0.0f, 1.0f)`.
3. Continue evaluation with clamped value (do not abort).
4. Increment `FM01_ClampCount` diagnostic counter.
5. If `FM01_ClampCount > 10` in a single match, escalate to ERROR level logging â€”
   repeated clamping indicates a systemic formula error, not an edge case.

**Design rationale for continue-with-clamp:** Aborting the evaluation would leave the ball
in an undefined state (Collision System has already completed; Ball Physics expects new ball
state). A clamped evaluation with a logged warning is safer than a mid-frame abort.

---

### 2.6.2 FM-02: Touch Radius Out of Range

**Trigger:** `r` outside [0.10m, 2.0m] after computation.

**Possible causes:**
- Input `q` was clamped (FM-01) and the corrected value produces an edge-case `r`
- Interpolation table contains an error (implementation bug)
- Ball speed term pushes `r` beyond maximum

**Detection:** Assert `r >= MIN_TOUCH_RADIUS && r <= MAX_TOUCH_RADIUS` after calculation.

**Recovery:**
1. Log error with `q` input value and computed `r`.
2. Clamp: `r = Mathf.Clamp(r, MIN_TOUCH_RADIUS, MAX_TOUCH_RADIUS)`.
3. Continue evaluation.

---

### 2.6.3 FM-03: Invalid Ball Position After Displacement

**Trigger:** `newBallPosition` is outside pitch bounds, contains NaN, or is below the pitch
surface (`y < -0.01m`).

**Possible causes:**
- Angular error calculation produced a vector component overflow
- Ball position at contact was already slightly invalid (upstream bug in Ball Physics)
- Displacement vector calculation error

**Detection:** Check `newBallPosition` bounds against pitch dimensions + 1.0m margin, and
`y >= -0.01m`, and `!float.IsNaN(newBallPosition.x)` after computation.

**Recovery:**
1. Log error with contact position, displacement vector, and final computed position.
2. **Do not use the invalid position.** Use the ball's contact position plus a minimal
   displacement in the agent's movement command direction:
   `newBallPosition = collisionData.ContactPoint + agent.MovementCommand.normalised * MIN_TOUCH_RADIUS`
3. Set possession to LOOSE_BALL (safe default â€” neither team is favoured).
4. Continue.

**Design rationale:** An invalid ball position is the most dangerous failure â€” placing the
ball outside the pitch would break the entire simulation frame. The conservative fallback
(minimal displacement from contact point, LOOSE_BALL) is always safe.

---

### 2.6.4 FM-04: Spatial Query Timeout / No Response

**Trigger:** Collision System's `SpatialQuery` API does not return within expected latency,
or returns a null/empty result in a situation where opponents are known to be present.

**Possible causes:**
- Collision System is in an error state
- Spatial hash was not updated for current frame (Collision System bug)
- API contract mismatch (Stage 0 risk; pending verification noted in Â§2.3.1)

**Detection:** Check for null return from spatial query. If null, log warning and treat as
zero-pressure result.

**Recovery:**
1. Log warning: "SpatialQuery returned null at frame [N], agent [ID]. Using zero pressure."
2. Set `pressureScalar = 0.0` (favours the receiving agent â€” conservative choice).
3. Continue evaluation without pressure term.
4. Increment `FM04_ZeroPressureCount` diagnostic counter.

**Design rationale for zero-pressure fallback:** Underestimating pressure is safer than
overestimating it. A false LOOSE_BALL outcome due to phantom pressure would be more disruptive
to gameplay than a CONTROLLED outcome with zero-pressure assumption.

---

## 2.7 Section Summary

| Subsection | Key Content |
|------------|-------------|
| **2.1 Functional Requirements** | FR-01â€“FR-07 with statements, inputs/outputs, rationale, acceptance criteria |
| **2.2 Architecture** | Frame pipeline position (Step 5 of 9); sub-system decomposition; evaluation pipeline |
| **2.3 Data Flow** | Inbound interfaces (5 sources); outbound interfaces (3 consumers); coordinate system |
| **2.4 Touch Outcome Taxonomy** | CONTROLLED / LOOSE_BALL / DEFLECTION / INTERCEPTION formal definitions |
| **2.5 Performance Budget** | <0.05ms per evaluation; zero heap allocation; total match impact <20ms |
| **2.6 Failure Modes** | FM-01â€“FM-04; detection conditions; recovery procedures; design rationale |

---

## Cross-Reference Verification

| Reference | Target | Verified | Notes |
|-----------|--------|----------|-------|
| Master Vol 1 Â§6.4 | Control quality formula | âœ” | FR-01 inputs match |
| Master Vol 1 Â§6 (Half-Turn) | +15% orientation bonus | âœ” | FR-06, Â§2.4 |
| Master Vol 1 Â§1.3 | Determinism requirement | âœ” | FR-01 acceptance criteria |
| Master Vol 4 Â§3.2 | 6ms physics frame budget | âœ” | Â§2.5.1 budget context |
| Ball Physics #1 Â§3.1 | BallState structure | âœ” | Â§2.3.1 inbound table |
| Ball Physics #1 Â§3.1.1 | Coordinate system | âœ” | Â§2.3.3 reproduces definitions |
| Agent Movement #2 Â§3.5.6 | PlayerAttributes.Technique, FirstTouch | âœ” | Â§2.3.1 inbound table |
| Agent Movement #2 Â§6.1.2 | DribblingModifier | âœ” | Â§2.3.2 outbound table |
| Collision System #3 Â§4.2.6 | AgentBallCollisionData | âš  Verified (approved Feb 19, 2026) | Â§2.3.1 warning note |
| Collision System #3 Â§3.1.4 | SpatialQuery API | âš  Verified (approved Feb 19, 2026) | Â§2.3.1 warning note; FM-04 |
| First Touch Spec Â§1 | Scope boundaries | âœ” | Â§2.4 taxonomy consistent with Â§1.2 |
| Outline Critical Issue #4 | Interception chain rule | âœ” | Â§2.4.4 documents resolution |

---

**End of Section 2**

---

## Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.1 | March 25, 2026 | Claude (AI) / Anton | Comprehensive audit fixes: (1) MOD-01: FR-8 (Determinism) absorption note added — determinism is a cross-cutting invariant enforced via FR-01 acceptance criteria, not a separate FR. (2) Collision System cross-references updated from Pending to Verified (approved Feb 19, 2026). |
| 1.0 | February 16, 2026, 8:15 PM PST | Claude (AI) / Anton | Initial draft. 7 FRs. Full system architecture. Data flow. Failure modes. |

**Page Count:** ~10 pages
**Version:** 1.1
**Next Section:** Section 3 â€” Technical Specifications (Control Quality Formula, Touch Radius, Ball Displacement, Possession State Machine, Pressure Evaluation, Orientation Detection)
