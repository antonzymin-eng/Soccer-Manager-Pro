## SECTION-BY-SECTION OUTLINE

---

### SECTION 1: PURPOSE AND SCOPE (~4 pages)

#### 1.1 Document Purpose
- Decision Tree is the agent cognition layer — translates perception into action
- Sole consumer of `PerceptionSnapshot`
- Operates at 10Hz tactical heartbeat, second in evaluation order after Perception
- Produces `AgentAction` structs dispatched to execution systems

#### 1.2 What This Specification Covers
- `AgentAction` struct definition and type taxonomy
- Option generation: how candidate actions are enumerated from snapshot
- Option scoring: how candidates are ranked by attribute-weighted utility
- Option selection: final action selection (highest utility, with composure-based noise)
- Execution dispatch: routing selected action to correct execution system
- Intake mechanism for `PerceptionSnapshot` (defines the interface Perception deferred)
- `MatchContext` struct (publicly visible game state)
- Data structures: `ActionOption`, `DecisionContext`, `AgentAction`
- State machine: IDLE → EVALUATING → EXECUTING → INTERRUPTED states
- Edge cases: no viable option, simultaneous equal-utility options, mid-execution interrupt
- Event publication: `DecisionMadeEvent` stub

#### 1.3 What Is OUT of Scope

- Action execution physics (owned by Pass Mechanics, Shot Mechanics, Agent Movement)
- Tactical instruction derivation (Formation System, Stage 1+)
- Goalkeeper decisions (Goalkeeper Mechanics #11)
- Set piece AI: corners, free kicks, throw-ins, kick-offs (Stage 2+)
- **Heading decisions — no aerial ball decisions at Stage 0.** Heading Mechanics (#10) will integrate with DT at Stage 1. Multiple adjacent specs reference "Decision Tree" as the aerial decision caller; those interfaces are NOT written here.
- Multi-agent coordination protocols (Positioning AI #12, Pressing AI #13)
- Any direct world state reads outside `PerceptionSnapshot` and `MatchContext`

**⚠ Adjacent-spec expectation note:** Pass Mechanics (#5), Shot Mechanics (#6), First Touch (#4), and Heading Mechanics (#10) all reference "Decision Tree" as their caller. At Stage 0, DT only calls Pass Mechanics and Shot Mechanics directly. First Touch is triggered by the Collision System, not DT. Heading Mechanics is entirely out of scope. Implementers reading those specs must not expect DT to call systems it does not yet interface with.

#### 1.4 Key Design Decisions (Preliminary — to be locked before §3 drafting)

| # | Decision | Rationale |
|---|----------|-----------|
| KD-1 | Perception-first evaluation order: Perception runs at heartbeat tick N, DT runs at tick N using that tick's snapshots. DT never reads stale N-1 data. | Prevents temporal inconsistency; resolves KR-5 from Perception §7. |
| KD-2 | Utility-based option scoring, not scripted priority ladders | Allows emergent behaviour from parameter tuning; consistent with FM killer ambition |
| KD-3 | Composure attribute introduces bounded noise into final selection | High composure → always selects max utility. Low composure → probabilistic deviation. Deterministic from seed + agentId + tick. |
| KD-4 | Decision Tree consumes PerceptionSnapshot by value, never by reference | Mirrors Perception §4.5.2 delivery mechanism; prevents cross-heartbeat contamination |
| KD-5 | AgentAction is a value struct dispatched in the same heartbeat it is selected | No queuing across heartbeats except for multi-frame execution (pass windup, etc.) — execution system owns that state |
| KD-6 | TacticalContext injected as a read-only struct; DT does not own or modify it | Tactical instructions are external constraints, not DT output |
| KD-7 | Stage 0 action set is fixed at 7 types; no extensibility hooks for new types at this stage | Prevents scope creep; Stage 1 extensions listed in §7 |

#### 1.5 Dependencies
- Hard: PerceptionSnapshot (#7), PassRequest (#5), ShotRequest (#6), AgentState (#2), PlayerAttributes (#2), BallState (#1)
- Soft: Event System (#17), Formation System (Stage 1), Fixed64 (#9)

#### 1.6 Stage 0 Deliverables vs. Deferrals

**Stage 0 Deliverables:**
- 7-action decision pipeline (PASS, SHOOT, DRIBBLE, HOLD, MOVE_TO_POSITION, PRESS, INTERCEPT)
- Utility-based scoring with attribute weighting
- Composure noise model
- `PerceptionSnapshot` intake interface (resolves Perception System §4.5.3 deferral)
- `PassRequest` and `ShotRequest` population logic
- `MatchContext` struct and accessor
- `TacticalContext` stub with hardcoded Stage 0 defaults
- Full test suite (~90–110 tests)

**Stage 0 Known Limitations (explicitly documented — not bugs):**

| Limitation | Description | Resolution Stage |
|------------|-------------|-----------------|
| **No team tactical differentiation** | `TacticalContext` is hardcoded to MEDIUM pressing, MIXED passing, MEDIUM defensive line for all teams. Both teams use identical default parameters. Tactical instructions from the manager have no effect at Stage 0. | Stage 1 — Formation System wires real team instructions. |
| **No aerial decisions** | Agents never decide to head the ball. Any ball above 0.5m is outside the DT's decision scope entirely at Stage 0. Agents with the ball above 0.5m have no DT action until the ball returns to ground. | Stage 1 — Heading Mechanics #10 integration. |
| **No set pieces** | Corners, free kicks, throw-ins, and goal kicks produce HOLD or MOVE_TO_POSITION only. No specialist set piece behaviour. | Stage 2 |
| **No goalkeeper decisions** | Goalkeeper is treated as a standard outfield agent for positioning purposes. No saves, distribution, or GK-specific actions. | Stage 1 — Goalkeeper Mechanics #11 integration. |
| **Utility weights are initial estimates** | All [GT] gameplay-tuned constants in §3.2 are starting values requiring post-implementation tuning. They are not expected to produce optimal gameplay on first build. | Ongoing — tuning phase after Stage 0 implementation. |
| **Vision attribute affects only pass lane assessment** | At Stage 0, Vision is only used in PASS option scoring. Broader Vision effects (e.g., off-ball awareness of dangerous runs) are deferred. | Stage 1 |

### SECTION 2: SYSTEM OVERVIEW AND FUNCTIONAL REQUIREMENTS (~5 pages)

#### 2.1 Decision Pipeline (Single Heartbeat)

```
Step 1: ReceiveSnapshot()         — intake PerceptionSnapshot for this agent
Step 2: AssembleDecisionContext() — combine snapshot + MatchContext + TacticalContext + AgentState
Step 3: GenerateOptions()         — enumerate all valid AgentAction candidates
Step 4: ScoreOptions()            — compute utility score for each candidate
Step 5: SelectAction()            — composure-weighted selection from scored list
Step 6: DispatchAction()          — route to execution system; publish DecisionMadeEvent stub
```

#### 2.2 Data Structures (Preliminary)

```csharp
// Typed action struct — produced by DT, consumed by execution systems
struct AgentAction
{
    int         AgentId;
    ActionType  Type;               // PASS, SHOOT, DRIBBLE, HOLD, MOVE_TO_POSITION, PRESS, INTERCEPT
    int         TargetAgentId;      // For PASS, PRESS, INTERCEPT (−1 if not applicable)
    Vector2     TargetPosition;     // For MOVE_TO_POSITION, DRIBBLE
    PassRequest PassParams;         // Populated only when Type == PASS
    ShotRequest ShotParams;         // Populated only when Type == SHOOT
    float       UtilityScore;       // Score at selection time (for debug/replay)
    int         HeartbeatTick;      // Tick this action was selected
}

// Candidate option during evaluation
struct ActionOption
{
    ActionType  Type;
    float       BaseUtility;        // Raw score before composure noise
    // ... additional context fields TBD in Section 3
}

// Publicly visible game state (not world state)
struct MatchContext
{
    int         HomeScore;
    int         AwayScore;
    float       MatchTimeSeconds;
    PossessionState Possession;     // HOME_TEAM, AWAY_TEAM, CONTESTED
    MatchPhase  Phase;              // OPEN_PLAY, SET_PIECE_HOME, SET_PIECE_AWAY, KICK_OFF
    FieldZone   BallZone;           // Defensive, Midfield, Attacking (coarse zone; derived from BallState.Position)
}
```

#### 2.3 Functional Requirements (FR list — preliminary)

- FR-01: DT SHALL process exactly one PerceptionSnapshot per agent per heartbeat tick
- FR-02: DT SHALL produce exactly one AgentAction per agent per heartbeat
- FR-03: DT SHALL NOT read world state outside PerceptionSnapshot and MatchContext
- FR-04: DT output SHALL be deterministic given identical inputs (seed + agentId + tick)
- FR-05: DT SHALL NOT produce actions that require capabilities not available to the agent (e.g., SHOOT when agent has no ball)
- FR-06: Option scoring SHALL be influenced by PlayerAttributes in a measurable, tunable way
- FR-07: AgentAction dispatch SHALL occur within the same 10Hz heartbeat tick
- FR-08: DT SHALL handle "no viable option" case — fallback to HOLD without exception
- FR-09: Composure attribute [1–20] SHALL produce measurably different selection distributions
- FR-10: DT SHALL publish `DecisionMadeEvent` stub for each action selected
- FR-11: DT SHALL accept mid-execution interrupt from Collision System (tackle, challenge)
- FR-12: DT performance SHALL complete all 22 agents within 4ms budget per heartbeat

#### 2.4 Failure Modes

- No visible teammates (PASS unavailable; options reduced)
- No ball visible (ball lost; default to MOVE_TO_POSITION)
- Agent not in possession but snapshot shows no ball visible anywhere
- PerceptionSnapshot null / malformed
- Simultaneous equal-utility options (deterministic tiebreak required)
- Interrupt during execution (incomplete pass/shot)

---

### SECTION 3: TECHNICAL SPECIFICATIONS (~12 pages)

#### 3.1 Option Generation

**3.1.1 Ball Possession Check**
- Agent has ball: PASS, SHOOT, DRIBBLE, HOLD candidates generated
- Agent does not have ball: MOVE_TO_POSITION, PRESS, INTERCEPT candidates generated
- Possession state sourced from PerceptionSnapshot (not world state)

**3.1.2 PASS Candidate Generation**
- For each VisibleTeammate in PerceptionSnapshot:
  - Check passing lane viability: geometric calculation against VisibleOpponents positions
  - Check distance-to-pass-type compatibility: derive PassType from distance/angle
  - Generate `PassOption` with estimated success probability
- Pass lane score: probability that pass reaches teammate given opponent positions
- Risk score: how many opponents can intercept the passing lane
- Attributes: Vision (lane quality assessment), Passing (accuracy baseline), Decisions (option breadth — lower Decisions → fewer candidates considered)

**3.1.3 SHOOT Candidate Generation**
- Check: agent has ball AND goal position known AND within shooting range
- Goal visibility: derived from PerceptionSnapshot (is goal in FoV and unoccluded?)
- Shot zone classification: determines PowerIntent range
- Attributes: Shooting (baseline accuracy), Composure (pressure sensitivity), Long Shots (range extension)

**3.1.4 DRIBBLE Candidate Generation**
- Check: agent has ball AND open space exists in some direction
- Space evaluation: vector analysis of VisibleOpponents positions relative to agent
- Viable dribble directions: directions where no opponent within threat radius
- Attributes: Dribbling, Agility, Pace (directional viability weights)

**3.1.5 HOLD Candidate Generation**
- Always available when agent has ball
- Base utility is low — chosen only when all other options score lower
- Attributes: Composure (higher → more likely to hold under pressure without panicking)

**3.1.6 MOVE_TO_POSITION Candidate Generation**
- Target position sourced from TacticalContext (formation slot)
- Modified by MatchContext.Phase and MatchContext.Possession
- Attributes: Positioning, Work Rate (how aggressively to fulfil positional duty)

**3.1.7 PRESS Candidate Generation**
- Check: opponent within pressing trigger distance (attribute-modulated threshold)
- Pressing target: closest visible opponent with ball OR highest-threat opponent without
- Viability: distance, agent's current stamina, TacticalContext pressing instruction
- Attributes: Aggression, Work Rate, Stamina, Pressing (TacticalContext)

**3.1.8 INTERCEPT Candidate Generation**
- Check: ball trajectory passes within intercept window of agent's movement capability
- Ball trajectory derived from BallState (via MatchContext or PerceptionSnapshot ball fields)
- Time-to-intercept vs. agent sprint speed: geometric viability check
- Attributes: Anticipation (window size), Pace (time-to-intercept calculation)

---

#### 3.2 Option Scoring — Utility Model

**Design principle:** Utility is a continuous [0, 1] normalised score. Higher = more
desirable. No magic constants without documentation. All weights are [GT] gameplay-tuned
and explicitly labelled.

**3.2.1 Utility Formula Structure**

```
U(option) = BaseUtility(option) × AttributeMultiplier × ContextModifier × RiskPenalty
```

Where:
- `BaseUtility`: intrinsic value of action type given current game state
- `AttributeMultiplier`: agent's relevant attribute as a [0.5, 1.5] range multiplier
- `ContextModifier`: match phase, zone, tactical instruction alignment
- `RiskPenalty`: probability-weighted downside of the action failing

**3.2.2 Per-Action Utility Derivations (Preliminary)**

These are the starting mathematical forms for Section 3 to derive fully with numerical verification. All constants marked [GT] are initial estimates — explicit gameplay-tuning is expected post-implementation.

**Notation used across all formulas:**
- `A_x` = attribute x normalised to [0, 1]: `A_x = (x_raw − 1) / 19`
- `P` = pressure scalar from PerceptionSnapshot (reuses Perception System §3.6 formula)
- `Z` = zone modifier: Z_def = 0.8, Z_mid = 1.0, Z_att = 1.2 [GT]
- All final U(option) values clamped to [0.01, 1.0] — floor prevents total suppression

---

**PASS Utility:**

```
U_PASS(target_i) = U_base_PASS
                 × PassLaneScore(target_i)
                 × A_Vision^0.3 [GT]            ← Vision governs lane quality reading
                 × A_Passing^0.4 [GT]           ← Passing governs accuracy confidence
                 × (1 − RiskPenalty_PASS)
                 × TacticalModifier_PASS

PassLaneScore(target_i) = clamp(1 − (interceptors_in_lane / 3.0), 0.1, 1.0) [GT denominator]
RiskPenalty_PASS        = P × (1 − A_Passing) × 0.3 [GT]
TacticalModifier_PASS   = 1.0 + (PassStyle_direct_bonus) [Stage 0: 0.0; Stage 1: from TacticalContext]
U_base_PASS             = 0.6 [GT] — pass is the default positive action
```

Multiple PASS candidates generated (one per VisibleTeammate); highest-scoring PassOption → best target.

---

**SHOOT Utility:**

```
U_SHOOT = U_base_SHOOT(zone)
        × A_Shooting^0.5 [GT]
        × A_Composure^0.3 [GT]               ← composure → pressure resistance at goal
        × GoalOpeningScore
        × (1 − RiskPenalty_SHOOT)

U_base_SHOOT(zone) = 0.9 if FieldZone == ATTACKING [GT]
                   = 0.5 if FieldZone == MIDFIELD and A_LongShots > 0.6 [GT]
                   = 0.05 otherwise (very low — discourages shots from own half)

GoalOpeningScore    = visible_goal_width_fraction × (1 − opponent_density_in_shot_lane)
                    — derived from PerceivedAgent positions relative to goal geometry
RiskPenalty_SHOOT   = (1 − GoalOpeningScore) × P × 0.4 [GT]
```

---

**DRIBBLE Utility:**

```
U_DRIBBLE = U_base_DRIBBLE
          × A_Dribbling^0.4 [GT]
          × A_Agility^0.3 [GT]
          × SpaceScore
          × (1 − RiskPenalty_DRIBBLE)

SpaceScore           = clamp(nearest_opponent_distance / DRIBBLE_THREAT_RADIUS, 0.0, 1.0)
                     — DRIBBLE_THREAT_RADIUS = 2.0m [GT]
U_base_DRIBBLE       = 0.45 [GT]
RiskPenalty_DRIBBLE  = P × (1 − A_Dribbling) × 0.35 [GT]
```

---

**HOLD Utility:**

```
U_HOLD = U_base_HOLD
       × A_Composure^0.5 [GT]               ← high composure → comfortable holding
       × (1 − P × 0.6) [GT]                 ← high pressure → hold becomes worse option

U_base_HOLD = 0.25 [GT] — deliberately low; holds only win when all options are worse
```

HOLD is always generated; its low base ensures it is selected only when all other candidates score below it.

---

**MOVE_TO_POSITION Utility:**

```
U_MOVE = U_base_MOVE
       × (1 − A_Positioning × 0.4) [GT]     ← well-positioned agent → low urgency to move
       × DistanceModifier
       × PhaseModifier

DistanceModifier     = clamp(distance_to_formation_slot / MAX_POSITION_URGENCY_DISTANCE, 0.1, 1.0)
                     — MAX_POSITION_URGENCY_DISTANCE = 15.0m [GT]
                     — agent far from slot → higher urgency to move
PhaseModifier        = 0.7 if Possession == OWN_TEAM [GT]  (can delay repositioning while in possession)
                     = 1.2 if Possession == OPPONENT [GT]  (urgent to get back into shape)
U_base_MOVE          = 0.4 [GT]
```

---

**PRESS Utility:**

```
U_PRESS = U_base_PRESS
        × A_Aggression^0.3 [GT]
        × A_WorkRate^0.3 [GT]
        × A_Stamina^0.2 [GT]
        × ProximityScore
        × TacticalPressingModifier

ProximityScore           = clamp(1 − (distance_to_press_target / PRESS_TRIGGER_DISTANCE), 0.0, 1.0)
                         — PRESS_TRIGGER_DISTANCE = 8.0m [GT]
TacticalPressingModifier = 1.4 if PressingMode == HIGH [GT]
                         = 1.0 if PressingMode == MEDIUM
                         = 0.6 if PressingMode == LOW
U_base_PRESS             = 0.5 [GT]
```

---

**INTERCEPT Utility:**

```
U_INTERCEPT = U_base_INTERCEPT
            × A_Anticipation^0.5 [GT]
            × InterceptFeasibilityScore
            × (1 − P × 0.2) [GT]            ← agent under pressure is less effective interceptor

InterceptFeasibilityScore = clamp(1 − (time_to_intercept / MAX_INTERCEPT_TIME), 0.0, 1.0)
                          — time_to_intercept = distance_to_intercept_point / AgentMaxSpeed
                          — MAX_INTERCEPT_TIME = 1.5s [GT]
U_base_INTERCEPT          = 0.55 [GT]
```

---

**Zone bonus summary table:**

| Action | Def Zone Modifier | Mid Zone Modifier | Att Zone Modifier |
|--------|-----------------|-------------------|-------------------|
| PASS | 1.0 | 1.0 | 0.9 [GT] |
| SHOOT | 0.1 [GT] | 0.5 [GT] | 1.0 |
| DRIBBLE | 0.7 [GT] | 1.0 | 1.1 [GT] |
| HOLD | 1.2 [GT] | 1.0 | 0.8 [GT] |
| MOVE_TO_POSITION | 1.0 | 1.0 | 1.0 |
| PRESS | 0.8 [GT] | 1.0 | 1.2 [GT] |
| INTERCEPT | 1.1 [GT] | 1.0 | 0.9 [GT] |

**Note:** These are preliminary values. Section 3 must derive them with worked numerical examples at attribute extremes (1 and 20), verify no single action dominates at all inputs, and confirm the composure noise in §3.3 does not destabilise the rankings at extreme pressure. All formula exponents and GT constants must appear in a dedicated `UtilityWeights.cs` constant file — never inline.

**3.2.3 Attribute-to-Utility Mapping Table**
- Full table: each attribute [1–20] → normalised multiplier value
- Academic grounding where possible [EST]; otherwise [GT] with explicit note
- ~55–65% gameplay-tuned constants expected (consistent with project pattern)

**3.2.4 Gameplay-Tuned Constant Management**

~65–70% of Decision Tree constants will be [GT]. This is higher than prior specs and is expected — the utility model is inherently a design layer, not a physics layer. The following governance rules apply:

- **All [GT] constants live exclusively in `UtilityWeights.cs`.** No inline magic numbers in formula code. This is a hard rule enforced in the §9 approval checklist.
- **Every [GT] constant must have a comment stating:** (1) what it controls, (2) the expected effect of increasing/decreasing it, (3) the range within which it is safe to tune without breaking other formulas.
- **Tuning is a post-implementation phase, not a spec-writing task.** Section 3 establishes initial values with worked examples showing they produce plausible output. The expectation is not that these values are final — it is that they produce non-pathological behaviour on first build.
- **Tuning must not change formula structure** — only constant values. A change that requires restructuring a formula is a specification amendment, not a tune.
- **Academic sources for attribute→performance relationships [EST]:**
  - Vision → pass lane assessment: EPV (Expected Possession Value) literature; Fernandez et al. (2021)
  - Composure → decision quality under pressure: Beilock (2010) — shared with Perception System
  - Anticipation → intercept window: Müller et al. (2009) interceptive timing research
  - All other attribute exponents: [GT] initially — flag for literature review in §8.2

---

#### 3.3 Final Action Selection — Composure Model

**3.3.1 Deterministic Noise Injection**
- Without composure: select max utility action always
- With composure: inject bounded noise into utility scores before selection
  ```
  Noise = f(Composure, seed, agentId, heartbeatTick)
  EffectiveUtility = BaseUtility + Noise × (1 − Composure_normalised)
  ```
- High Composure (18–20): Noise magnitude ≈ 0 → always selects optimal
- Low Composure (1–3): Noise magnitude large → may select suboptimal under pressure
- Noise is deterministic from hash: `noise_seed = matchSeed ^ (agentId << 16) ^ heartbeatTick`

**3.3.2 Tiebreak Rule**
- If two options have equal EffectiveUtility after noise: select by ActionType enum ordinal (deterministic)
- Log tiebreak in `DecisionMadeEvent` for debug

**3.3.3 No Viable Option Fallback**
- If GenerateOptions() returns zero candidates: produce HOLD action with utility = 0.0
- Log as FM-DT-08 failure mode; should not occur in normal play

---

#### 3.4 Tactical Context Integration

**3.4.1 TacticalContext Struct (Preliminary)**

```csharp
struct TacticalContext
{
    PressingMode    Pressing;           // HIGH, MEDIUM, LOW
    PassingStyle    PassStyle;          // DIRECT, MIXED, SHORT
    DefensiveLine   DefLine;            // HIGH, MEDIUM, LOW
    float           CreativeFreedom;    // [0, 1] normalised; high = looser adherence to position
    // Stage 1+: full 15 team instruction set
}
```

**3.4.2 Instruction-to-Utility Modifications**
- PressingMode.HIGH: PRESS utility multiplier increased; HOLD utility decreased
- PassingStyle.DIRECT: long-range PASS utility increased; HOLD utility decreased
- DefensiveLine.HIGH: MOVE_TO_POSITION target zone shifted upfield
- CreativeFreedom: high → DRIBBLE utility boosted, PASS utility slightly reduced

**3.4.3 Out-of-Possession Behaviour**
- When Possession == AWAY_TEAM: PRESS and INTERCEPT candidates prioritised
- When Possession == HOME_TEAM and agent is home team: PASS/DRIBBLE/SHOOT prioritised
- Formation slot distance influences MOVE_TO_POSITION urgency

---

#### 3.5 Execution Dispatch

**3.5.1 Action Routing Table**
| ActionType | Execution System | Method Called |
|------------|-----------------|---------------|
| PASS | Pass Mechanics #5 | `PassExecutor.Execute(PassRequest)` |
| SHOOT | Shot Mechanics #6 | `ShotExecutor.Execute(ShotRequest)` |
| DRIBBLE | Agent Movement #2 | `MovementController.SetDribbleTarget(Vector2)` |
| HOLD | Agent Movement #2 | `MovementController.HoldPosition()` |
| MOVE_TO_POSITION | Agent Movement #2 | `MovementController.MoveTo(Vector2, priority)` |
| PRESS | Agent Movement #2 | `MovementController.MoveToPress(targetAgentId)` |
| INTERCEPT | Agent Movement #2 | `MovementController.MoveToIntercept(Vector2, float)` |

**3.5.2 PassRequest Population**
- DT selects PassType from distance/angle analysis (§3.1.2)
- DT sets TargetAgentId, PowerHint (normalised intent)
- Pass Mechanics executes; DT does not compute velocity or spin

**3.5.3 ShotRequest Population**
- DT sets PowerIntent [0, 1], ContactZone (enum from Shot Mechanics §3.5), PlacementTarget (Vector2 in goal plane), SpinIntent [−1, 1]
- DT does not name the shot type — it selects physical parameters only (consistent with Shot Mechanics KD-1)

**3.5.4 Interrupt Handling**
- Collision System may publish `TackleContactEvent` during execution windup
- DT receives interrupt signal; cancels current AgentAction; re-evaluates on next tick
- Partial execution state (e.g., incomplete pass windup) is owned by execution system, not DT

---

#### 3.6 PerceptionSnapshot Intake Interface

**3.6.1 Interface Definition**
- This section defines the interface Perception System §4.5.3 explicitly deferred
- **Delivery mechanism (resolved — see OQ-1 resolution below):** Direct method call from the simulation loop orchestrator. The orchestrator calls `DecisionTree.ReceiveSnapshot(PerceptionSnapshot snapshot)` for each agent in ascending `AgentId` order, immediately after the Perception System batch completes for that heartbeat. No event bus, no shared buffer.
- Delivery: by value (per Perception §4.5.2 constraint — DT receives a copy, not a reference)
- Timing: all 22 agents' snapshots delivered before any DT evaluation begins (deterministic ordering)
- Interface owned by this specification; Perception System §4.5.3 deferred definition here

```csharp
// Defined in: DecisionTree.cs (owned by Decision Tree Specification #8)
// Called by: SimulationLoopOrchestrator — once per agent per heartbeat, after Perception batch
// NOT a Unity event or message bus subscription — direct synchronous call

public void ReceiveSnapshot(PerceptionSnapshot snapshot);
// snapshot is passed by value; DT may not retain reference to caller's copy
// Forced refresh snapshot uses the same method signature; DT re-evaluates that agent only
```

**3.6.2 Forced Refresh Handling**
- Perception §4.5.2 warns DT must accept snapshots outside normal heartbeat cadence
- DT must not assume snapshot arrival only on tick boundaries
- Forced refresh for an agent triggers immediate re-evaluation for that agent only

---

#### 3.7 State Machine

**States:**
- IDLE: no action in progress (entry state, and state after action completion)
- EVALUATING: heartbeat pipeline running (Steps 1–5)
- EXECUTING: action dispatched, awaiting completion or interrupt
- INTERRUPTED: execution cancelled by external event; return to IDLE

**Transitions:**
- IDLE → EVALUATING: heartbeat tick received
- EVALUATING → EXECUTING: action dispatched successfully
- EVALUATING → IDLE: no viable option (HOLD produced; no execution system engaged)
- EXECUTING → IDLE: execution system confirms action complete
- EXECUTING → INTERRUPTED: tackle contact event or forced re-evaluate signal
- INTERRUPTED → IDLE: one frame; return to IDLE for next heartbeat

---

#### 3.8 Edge Cases

- Agent has ball but no VisibleTeammates and not in shooting range → DRIBBLE or HOLD
- Agent in penalty box with ball and goal visible → SHOOT utility extremely high regardless of snapshot quality
- All opponents out of visible range (agent isolated) → MOVE_TO_POSITION dominates
- PerceptionSnapshot.BallVisible = false → agent cannot make possession-dependent decisions
- Perception forced refresh mid-heartbeat → decision already dispatched; no mid-execution override
- Two agents both select PRESS on same opponent → handled by Agent Movement collision avoidance; not DT's responsibility
- Agent Composure=1 under extreme pressure → may select obviously suboptimal action; this is correct behaviour, not a bug

---

### SECTION 4: ARCHITECTURE AND INTEGRATION (~5 pages)

#### 4.1 File Structure (Preliminary — ~18–22 files)
- `DecisionTree.cs` — main orchestrator; heartbeat entry point
- `OptionGenerator.cs` — Step 3 generation for all 7 action types
- `UtilityScorer.cs` — Step 4 scoring formulas
- `ActionSelector.cs` — Step 5 composure model and selection
- `ActionDispatcher.cs` — Step 6 routing to execution systems
- `SnapshotReceiver.cs` — intake interface implementation
- `MatchContextProvider.cs` — public game state accessor
- `TacticalContextReader.cs` — team instruction accessor (stub at Stage 0)
- `DecisionMadeEvent.cs` — event stub
- `AgentAction.cs` — output struct
- `ActionOption.cs` — intermediate scoring struct
- `DecisionContext.cs` — assembled input struct for Steps 3–5
- `UtilityWeights.cs` — all [GT] constants with explicit labelling
- `PassRequestBuilder.cs` — constructs PassRequest from option data
- `ShotRequestBuilder.cs` — constructs ShotRequest from option data
- Test files: `OptionGeneratorTests.cs`, `UtilityScorerTests.cs`, `ActionSelectorTests.cs`, `DecisionTreeIntegrationTests.cs`

#### 4.2 Interface Contracts with Upstream Systems
- Perception System #7: intake interface (defined here; deferred in Perception §4.5.3)
- Agent Movement #2: read-only `AgentState` and `PlayerAttributes`; write via movement commands
- Ball Physics #1: read-only `BallState` via `MatchContext`

#### 4.3 Interface Contracts with Downstream Systems
- Pass Mechanics #5: `PassRequest` struct (already defined in Pass Mechanics §4; DT must conform)
- Shot Mechanics #6: `ShotRequest` struct (already defined in Shot Mechanics §3.1; DT must conform)
- Agent Movement #2: movement command structs (define here; Agent Movement §6 hooks expected)

#### 4.4 Dependency on Fixed64 (#9)
- Stage 0: float arithmetic throughout
- Fixed64 migration path documented in §7 (same pattern as all prior specs)

---

### SECTION 5: TESTING (~6 pages)

#### 5.1 Test Strategy
- Same philosophy as prior specs: all expected values derived from formulas in Section 3
- Determinism requirement: identical seed + agentId + tick → identical AgentAction
- Edit Mode unit tests (no Unity runtime); Play Mode integration tests

#### 5.2 Unit Tests — Option Generation
- For each of 7 action types: availability condition tests (attribute 1 and 20), edge case (no candidates), candidate count vs. Decisions attribute
- Target: ~30–40 unit tests

#### 5.3 Unit Tests — Utility Scoring
- Each formula component in isolation: BaseUtility, AttributeMultiplier, ContextModifier, RiskPenalty
- Zone bonuses: verify SHOOT utility increase in attacking third
- Tactical context: verify PRESS utility increase under HIGH pressing instruction
- Target: ~25–35 unit tests

#### 5.4 Unit Tests — Composure Model
- Composure 1 vs. Composure 20: distribution difference measurable over 1000 seeds
- Determinism: identical inputs → identical output
- Tiebreak: equal utility → deterministic selection
- Target: ~10–15 unit tests

#### 5.5 Unit Tests — PassRequest and ShotRequest Population
- Verify PassRequest fields populated correctly from option data
- Verify ShotRequest fields populated correctly (PowerIntent in [0,1], ContactZone valid, etc.)
- Target: ~10–15 unit tests

#### 5.6 Integration Tests
- Full 22-agent heartbeat: all agents produce valid actions in <4ms
- Possession chain: 3-pass sequence produces correct Pass→FirstTouch→Pass chain
- Shooting scenario: agent in shooting position with clear goal → selects SHOOT
- Pressing scenario: opponent has ball in own half under HIGH press → agents select PRESS
- No-ball scenario: all agents without ball select MOVE_TO_POSITION or PRESS
- Interrupt handling: tackle contact during pass windup → DT returns to IDLE
- Target: ~15–20 integration tests

#### 5.7 Balance Tests
- Decisions 1 vs. Decisions 20: measurable difference in option count considered
- Composure 1 vs. Composure 20: measurable difference in selection optimality over 100 ticks
- Vision 1 vs. Vision 20: measurable difference in pass utility assessment quality

#### 5.8 Performance Tests
- 22-agent batch within 4ms heartbeat budget
- Per-agent budget: ~180µs (generous; DT is compute-light vs. Perception)

**Estimated total test count:** ~90–110 tests (6–7× minimum)

---

### SECTION 6: PERFORMANCE ANALYSIS (~3 pages)

#### 6.1 Operation Count Analysis
- Step 3 (GenerateOptions): O(k) where k = VisibleTeammates.Length + VisibleOpponents.Length ≤ 21
- Step 4 (ScoreOptions): O(m) where m = candidate count ≤ 7 × k (bounded)
- Step 5 (SelectAction): O(m log m) sort + O(1) selection
- Total: bounded O(k) per agent per heartbeat; highly cache-friendly value-type operations

#### 6.2 Performance Budget
- 10Hz heartbeat = 100ms window
- DT allocation: **4ms for all 22 agents** (per Master Vol 4 system budget)
- Per-agent target: ~180µs
- Expectation: DT is significantly under budget (pure logic, no trig, no spatial queries)

#### 6.3 Memory Profile
- No per-heartbeat heap allocations
- `ActionOption[]` pool: 22 × max candidates (fixed capacity at match start)
- All structs are value types; no reference type intermediate state

---

### SECTION 7: FUTURE EXTENSIONS (~3 pages)

#### 7.1 Stage 1 Extensions
- **7.1.1 Team Instruction Modifiers (Full 15-instruction set)**: requires Formation System
- **7.1.2 Individual Player Instructions**: per-position instructions (overlap, stay back, etc.)
- **7.1.3 Body Part Selection for Pass**: DT populates `PassRequest.BodyPart` (hook already in Pass Mechanics §7.5)
- **7.1.4 Dribbling Intent Signal**: DT sets `DribblingIntent` field (hook in First Touch §7.5)
- **7.1.5 Context-sensitive shoulder check urgency**: DT provides `ContextProvider` to Perception (Perception §7.1.5)

#### 7.2 Stage 2 Extensions
- **7.2.1 Set piece AI**: corner, free kick, throw-in decision making
- **7.2.2 Heading decision integration**: aerial ball decisions routing to Heading Mechanics #10
- **7.2.3 Feint/skill move selection**: requires Skill Move System

#### 7.3 Stage 3+ Extensions
- Multi-agent coordination: coordinated press, overlapping runs (Pressing AI #13, Attacking AI #15)
- Psychology model: pressure/confidence affecting decision quality (Psychology System Stage 4)
- Form modifier: recent match results affecting Composure multiplier (Form System Stage 3)

#### 7.4 Permanently Excluded
- **Scripted behaviour sequences**: no hard-coded if/else play patterns — all behaviour must emerge from utility model
- **Omniscient world state access**: DT may never read raw world state — only `PerceptionSnapshot` + `MatchContext`
- **Tactical reasoning inside execution systems**: Pass/Shot/Movement execute; DT reasons — boundary is absolute

---

### SECTION 8: REFERENCES (~2 pages)

#### 8.1 Internal Project References
- Ball Physics Spec #1, Agent Movement Spec #2, Collision System Spec #3, First Touch Spec #4, Pass Mechanics Spec #5, Shot Mechanics Spec #6, Perception System Spec #7
- Master Vol 2 (Human Systems): AI decision architecture design intent
- Master Vol 4 (Tech Implementation): system performance budgets

#### 8.2 Academic Sources (Preliminary — DOI verification required before §8 is finalised)
- Utility theory in game AI: Russell & Norvig, *Artificial Intelligence: A Modern Approach*
- Sports decision-making under pressure: Beilock (2010) — shared with Perception System
- Agent-based simulation in football: academic literature on IMPECT, EPV (Expected Possession Value)
- Decision fatigue and cognitive load: literature on attribute-to-performance mappings

#### 8.3 Expected Constant Classification

| Category | Count (est.) | Classification | Notes |
|----------|-------------|----------------|-------|
| Utility base values (U_base per action) | 7 | [GT] | Starting values derived from design intent; expect 2–3 tuning cycles |
| Attribute exponents (A_x^n) | ~20 | [GT] | Exponent values; academic literature may provide bounds but not exact values |
| Distance/radius thresholds | ~8 | [GT] | PRESS_TRIGGER_DISTANCE, DRIBBLE_THREAT_RADIUS, etc. |
| Zone modifiers | ~21 (7 actions × 3 zones) | [GT] | Design intent values; validate against observable play patterns |
| Risk penalty weights | ~7 | [GT] | One per action; sensitivity-test for extreme pressure values |
| Composure noise bounds | 2 | [GT] | Upper and lower noise clamping values; see §3.3.1 |
| Tactical context multipliers | ~12 | [GT] | Per-instruction per-action modifiers |
| Attribute multiplier from academic sources | ~4 | [EST] | Vision (EPV), Composure/Anticipation (sports science) |
| Performance budget allocations | 2 | Internal [GT] | From Master Vol 4 |

**Expected total: ~80 constants. ~75 [GT] (~94%), ~4 [EST], ~1 performance budget.**

This is the highest [GT] ratio in any specification to date, consistent with the nature of AI decision weight tuning. It is transparent, not hidden. All constants are in `UtilityWeights.cs` and all are labelled. The tuning phase is explicitly planned as post-implementation work.

**Tuning roadmap (to be documented in §7):**
- Phase 1 (after Stage 0 build): base utility values — verify no single action dominates in any typical game state
- Phase 2: attribute exponents — verify high-attribute agents produce observably better decisions
- Phase 3: zone and tactical modifiers — verify team instructions produce observable tactical differences
- Phase 4: composure noise bounds — verify low-Composure agents make believable errors, not random chaos

---

### SECTION 9: APPROVAL CHECKLIST

Standard template — drafted last, after all sections complete.

---

## OPEN QUESTIONS (Must Be Resolved Before Section 2 Is Drafted)

| # | Question | Impact | Options |
|---|----------|--------|---------|
| OQ-1 | ~~How does DT receive PerceptionSnapshot? Direct method call, event bus, or shared buffer?~~ | **RESOLVED** | **Direct method call from simulation loop orchestrator.** Rationale: (1) Testability — direct call lets unit tests inject snapshots without event infrastructure. (2) Execution order — orchestrator controls Perception-then-DT order explicitly; event bus introduces ordering ambiguity. (3) Consistency — matches Pass Mechanics and Shot Mechanics synchronous call pattern. (4) Simplicity — no subscription lifecycle to manage; no risk of missed or duplicated delivery. `ReceiveSnapshot(PerceptionSnapshot)` defined in §3.6.1. |
| OQ-2 | Does DT persist action state across heartbeats for multi-frame actions (e.g., pass windup)? | If yes: DT state machine has EXECUTING state with cross-tick memory. If no: execution system owns all multi-frame state. | **Recommendation: No. Execution systems own their own state (consistent with architecture of Pass Mechanics and Shot Mechanics). DT treats a new heartbeat as always fresh.** |
| OQ-3 | How does DT know a forced-refresh snapshot arrived mid-heartbeat? | Perception §4.5.2 warns DT must handle this | Perception publishes `PerceptionRefreshEvent`; DT listens. Define in §3.6.2. |
| OQ-4 | At Stage 0, are TacticalContext instructions hardcoded, defaulted, or derived from a stub system? | Affects §3.4 complexity | **Recommendation: Defaulted to sensible midfield constants at Stage 0 (MEDIUM pressing, MIXED passing, etc.). Stage 1 wires Formation System.** |
| OQ-5 | Does the DT evaluate all 22 agents sequentially or in any order? | Affects determinism guarantee if any shared mutable state exists | **Recommendation: Sequential, ascending AgentId (agent 0 through 21). No shared mutable state; each evaluation is independent. Consistent with Perception §4.5.2 delivery order.** |

---

