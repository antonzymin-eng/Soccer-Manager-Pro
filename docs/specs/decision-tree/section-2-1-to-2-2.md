# Decision Tree Specification #8 — Section 2: System Overview and Functional Requirements

**File:** `Decision_Tree_Spec_Section_2_v1_1.md`  
**Purpose:** Establishes the complete system overview for Decision Tree Specification #8:
the single-heartbeat decision pipeline, all data structures owned by this specification,
the full set of functional requirements with acceptance criteria, and the failure mode
catalogue. This section is the authoritative reference for what the Decision Tree does,
how data flows through it, and what constitutes correct behaviour at the boundary of
each requirement.

**Created:** March 01, 2026, 12:00 PM PST  
**Version:** 1.1  
**Status:** DRAFT — Awaiting Lead Developer Review  
**Specification Number:** 8 of 20 (Stage 0 — Physics Foundation)  
**Author:** Claude (AI) with Anton (Lead Developer)

**Prerequisite:** Section 1 approved (v1.1). All hard dependencies confirmed stable.  
**Upstream sections:** Ball Physics #1, Agent Movement #2, Collision System #3, Pass
Mechanics #5, Shot Mechanics #6, Perception System #7 — all approved.

---

## Table of Contents

- [2.1 Decision Pipeline — Single Heartbeat](#21-decision-pipeline--single-heartbeat)
  - [2.1.1 Pipeline Overview](#211-pipeline-overview)
  - [2.1.2 Step-by-Step Description](#212-step-by-step-description)
  - [2.1.3 Pipeline Timing and Evaluation Order](#213-pipeline-timing-and-evaluation-order)
  - [2.1.4 Pipeline Invariants](#214-pipeline-invariants)
- [2.2 Data Structures](#22-data-structures)
  - [2.2.1 ActionType Enumeration](#221-actiontype-enumeration)
  - [2.2.2 AgentAction Struct](#222-agentaction-struct)
  - [2.2.3 ActionOption Struct](#223-actionoption-struct)
  - [2.2.4 DecisionContext Struct](#224-decisioncontext-struct)
  - [2.2.5 MatchContext Struct](#225-matchcontext-struct)
  - [2.2.6 TacticalContext Struct (Stage 0 Stub)](#226-tacticalcontext-struct-stage-0-stub)
  - [2.2.7 DecisionMadeEvent Struct (Stage 0 Stub)](#227-decisionmadeevent-struct-stage-0-stub)
  - [2.2.8 Data Structure Ownership Summary](#228-data-structure-ownership-summary)
- [2.3 Functional Requirements](#23-functional-requirements)
  - [2.3.1 Pipeline Integrity Requirements (FR-01 – FR-04)](#231-pipeline-integrity-requirements-fr-01--fr-04)
  - [2.3.2 Action Generation Requirements (FR-05 – FR-06)](#232-action-generation-requirements-fr-05--fr-06)
  - [2.3.3 Scoring and Selection Requirements (FR-07 – FR-09)](#233-scoring-and-selection-requirements-fr-07--fr-09)
  - [2.3.4 Dispatch and Event Requirements (FR-10 – FR-11)](#234-dispatch-and-event-requirements-fr-10--fr-11)
  - [2.3.5 Performance Requirement (FR-12)](#235-performance-requirement-fr-12)
  - [2.3.6 Functional Requirements Summary Table](#236-functional-requirements-summary-table)
- [2.4 Failure Modes](#24-failure-modes)
  - [2.4.1 Failure Mode Catalogue](#241-failure-mode-catalogue)
  - [2.4.2 Failure Mode Severity Classification](#242-failure-mode-severity-classification)
- [2.5 Version History](#25-version-history)

---

## 2.1 Decision Pipeline — Single Heartbeat

### 2.1.1 Pipeline Overview

The Decision Tree processes one agent per invocation. For each of the 22 active field
agents, the simulation orchestrator calls `ReceiveSnapshot()` in ascending `AgentId`
order (0 through 21), passing the current heartbeat's `PerceptionSnapshot` for that
agent. The DT must complete all six pipeline steps and produce a dispatched `AgentAction`
before the orchestrator advances to the next agent.

The six pipeline steps are:

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                    DECISION TREE — SINGLE HEARTBEAT PIPELINE                    │
│                    (runs 22 times per heartbeat tick, per agent)                │
├──────┬──────────────────────────┬──────────────────────────────────────────────┤
│ Step │ Method                   │ Function                                     │
├──────┼──────────────────────────┼──────────────────────────────────────────────┤
│  1   │ ReceiveSnapshot()        │ Intake PerceptionSnapshot for this agent     │
│  2   │ AssembleDecisionContext() │ Combine snapshot + MatchContext +            │
│      │                          │ TacticalContext + AgentState into one struct  │
│  3   │ GenerateOptions()        │ Enumerate all valid AgentAction candidates   │
│  4   │ ScoreOptions()           │ Compute utility score for each candidate     │
│  5   │ SelectAction()           │ Composure-modulated selection from scored    │
│      │                          │ list; deterministic tiebreak if tied         │
│  6   │ DispatchAction()         │ Route selected action to execution system;   │
│      │                          │ publish DecisionMadeEvent stub               │
└──────┴──────────────────────────┴──────────────────────────────────────────────┘
```

### 2.1.2 Step-by-Step Description

**Step 1 — ReceiveSnapshot()**

The orchestrator delivers the current tick's `PerceptionSnapshot` to this agent's
Decision Tree instance via direct method call. The snapshot is received **by value**
(struct copy), not by reference. This guarantees:

- The DT works on data that cannot be mutated by another agent's concurrent or
  subsequent evaluation during the same tick.
- The `ReadOnlySpan<PerceivedAgent>` lifetime concern (AR-3, Outline) is eliminated:
  no span is stored; all needed data is copied into `DecisionContext` in Step 2 before
  the snapshot goes out of scope.

The DT must not cache the snapshot across heartbeat ticks. The snapshot is valid only
for the duration of the current pipeline execution. Any field from the snapshot that
is needed beyond Step 2 must be copied into `DecisionContext`. Visible agent lists
are copied into pre-allocated backing arrays in `DecisionContext` — element counts
are tracked in `VisibleTeammateCount` / `VisibleOpponentCount` (§2.2.4). No heap
allocation occurs in this step.

If the snapshot is null or in an unrecognised state (see §2.4.1 FM-04), the pipeline
must not proceed. See §2.4 for fallback behaviour.

---

**Step 2 — AssembleDecisionContext()**

The DT assembles a `DecisionContext` struct by combining four read-only inputs:

| Input | Source | Access Pattern |
|---|---|---|
| `PerceptionSnapshot` | Passed in Step 1 | Copy needed fields into `DecisionContext` |
| `MatchContext` | Injected at DT construction; updated each tick by orchestrator | Read-only reference |
| `TacticalContext` | Injected at DT construction; Stage 0 hardcoded defaults | Read-only reference |
| `AgentState` (self) | Injected at DT construction; updated each tick by orchestrator | Read-only reference |

`PlayerAttributes` are accessed via `AgentState` and must be read exactly once, here,
and cached in `DecisionContext` for all downstream steps. A mid-heartbeat attribute
change is not possible at Stage 0. Attribute reads must not be repeated in Steps 3–5.

`MatchContext.BallZone` is pre-computed by the orchestrator before the DT pipeline
runs. The DT reads this field from `MatchContext`; it does not read `BallState` directly.
This is the only approved mechanism for zone-aware utility modification (see §1.7.1 and
AR-2 omniscience prohibition).

---

**Step 3 — GenerateOptions()**

The DT enumerates all `ActionOption` candidates available to this agent given the current
`DecisionContext`. Option generation is gated by possession state:

**If agent has ball (sourced from `PerceptionSnapshot.HasPossession`):**

| Candidate | Generation Condition |
|---|---|
| PASS | ≥1 `VisibleTeammate` in snapshot; one `PassOption` generated per teammate |
| SHOOT | Goal position known; agent within shooting range; ball height ≤ 0.5m |
| DRIBBLE | ≥1 viable dribble direction exists (open space ahead) |
| HOLD | Always generated when agent has ball |

**If agent does not have ball:**

| Candidate | Generation Condition |
|---|---|
| MOVE_TO_POSITION | Always generated; target from `TacticalContext.FormationSlot` |
| PRESS | ≥1 `VisibleOpponent` within pressing trigger distance |
| INTERCEPT | Ball trajectory passes within intercept window of agent's movement capability |

An agent cannot generate candidates from both groups. Ball possession is binary and
sourced exclusively from `PerceptionSnapshot`. See §3.1 (Section 3) for full generation
logic.

The `Decisions` attribute (normalised from `PlayerAttributes.Decisions`) limits the
number of PASS candidates evaluated. An agent with low `Decisions` considers fewer
passing options — this is a meaningful attribute-to-behaviour mapping, not a
performance shortcut. Full derivation in Section 3.

---

**Step 4 — ScoreOptions()**

Each `ActionOption` receives a utility score computed by the formula:

```
U(option) = BaseUtility(option) × AttributeMultiplier × ContextModifier × RiskPenalty
```

Where:
- `BaseUtility` — intrinsic value of the action type given the current game state
- `AttributeMultiplier` — agent's relevant attributes as a [0.5, 1.5] range multiplier
- `ContextModifier` — zone modifier and tactical instruction alignment (Stage 0: zone only)
- `RiskPenalty` — probability-weighted downside of the action failing given current pressure

All scores are clamped to [0.01, 1.0]. The floor of 0.01 prevents complete suppression of
any candidate; no action is completely invisible to the scoring system. The HOLD candidate
receives a deliberately low `BaseUtility = 0.25` [GT] to ensure it wins selection only
when all other candidates score lower.

Full per-action utility derivations, attribute exponent values, and worked numerical
examples at attribute extremes are in Section 3.

---

**Step 5 — SelectAction()**

The DT selects a final `AgentAction` from the scored `ActionOption` list using the
Composure noise model:

1. A bounded deterministic noise term is added to each candidate's `BaseUtility`:

   ```
   EffectiveUtility(option) = U(option) + Noise × (1 − A_Composure)
   ```

   Where `Noise` is deterministically derived from:
   ```
   noise_seed = matchSeed XOR (agentId << 16) XOR heartbeatTick
   ```

2. The candidate with the highest `EffectiveUtility` is selected.

3. If two candidates have equal `EffectiveUtility` after noise injection, the tiebreak
   rule applies: select the candidate with the lower `ActionType` enum ordinal value
   (deterministic; see §2.2.1). This tie is logged in `DecisionMadeEvent.TiebreakerApplied`.

High `Composure` (18–20) → noise magnitude ≈ 0 → agent reliably selects maximum utility
action. Low `Composure` (1–3) → noise magnitude large → agent may select suboptimal action
under pressure. This models poor decision-making under stress, not random behaviour.
Noise is clamped to a maximum of ±0.20 utility [GT] to prevent pathological selection
(AR-4, Outline). Full derivation in Section 3.

---

**Step 6 — DispatchAction()**

The selected `AgentAction` is routed to the appropriate execution system:

| ActionType | Dispatched To | Mechanism |
|---|---|---|
| PASS | Pass Mechanics #5 | Synchronous call; `AgentAction.PassParams` populated as `PassRequest` |
| SHOOT | Shot Mechanics #6 | Synchronous call; `AgentAction.ShotParams` populated as `ShotRequest` |
| DRIBBLE | Agent Movement #2 | Synchronous call; `AgentAction.TargetPosition` set |
| HOLD | Agent Movement #2 | Synchronous call; minimal params (hold in place) |
| MOVE_TO_POSITION | Agent Movement #2 | Synchronous call; `AgentAction.TargetPosition` set |
| PRESS | Agent Movement #2 | Synchronous call; `AgentAction.TargetAgentId` set |
| INTERCEPT | Agent Movement #2 | Synchronous call; `AgentAction.TargetPosition` set to intercept point |

After dispatch, `DecisionMadeEvent` is published (stub at Stage 0). The pipeline for
this agent is complete. The orchestrator advances to the next agent.

---

### 2.1.3 Pipeline Timing and Evaluation Order

The Decision Tree operates at the **10Hz tactical heartbeat** — one full evaluation of
all 22 agents per 100ms game tick. This is not the physics tick (which runs at 60Hz
in Unity). The DT is not called on physics frames; it is called by the simulation
orchestrator on the tactical heartbeat schedule only.

**Heartbeat evaluation order (enforced by orchestrator):**

```
Tick N begins
  → Phase 1: Perception System generates PerceptionSnapshot for all 22 agents
  → Phase 2: Decision Tree evaluates all 22 agents (ascending AgentId, 0 → 21)
  → Phase 3: Execution systems process dispatched AgentActions
Tick N ends
```

The DT never evaluates before Perception has completed Phase 1 for the current tick.
This strict ordering is enforced at the orchestrator level, not by convention. DT
instances must not cache or depend on Phase 1 completing in any partial order. This
resolves KR-5 from Perception System §7.7 and is established as KD-1 in §1.4.

**No shared mutable state between agent evaluations.** Agent 5's evaluation cannot read
or modify state that Agent 6's evaluation depends on. Evaluation is fully independent per
agent, enabling the determinism guarantee in FR-04.

---

### 2.1.4 Pipeline Invariants

The following invariants must hold for every pipeline execution. They are tested in
Section 5 (testing framework). Any violation is a defect, not an edge case.

| ID | Invariant |
|---|---|
| INV-01 | Exactly one `AgentAction` is produced per pipeline execution per agent per tick |
| INV-02 | The produced `AgentAction.Type` is always a member of the Stage 0 `ActionType` enum |
| INV-03 | `AgentAction.HeartbeatTick` always equals the orchestrator's current tick number |
| INV-04 | An agent without ball possession never produces PASS, SHOOT, DRIBBLE, or HOLD |
| INV-05 | An agent with ball possession never produces PRESS, INTERCEPT, or MOVE_TO_POSITION |
| INV-06 | HOLD is always the fallback: if GenerateOptions() produces no viable candidates beyond HOLD, HOLD is selected with zero exceptions raised |
| INV-07 | The pipeline never reads from the WorldState namespace (enforced by static analysis) |
| INV-08 | The pipeline completes for all 22 agents within the 4ms performance budget |

---

## 2.2 Data Structures

This section defines all data structures owned by Decision Tree Specification #8.
Structures consumed from upstream specifications are referenced by their source.

### 2.2.1 ActionType Enumeration

The Stage 0 action set is fixed at seven types. The enum ordinal values are significant:
they serve as deterministic tiebreakers in Step 5 (lower ordinal wins ties). The ordering
reflects a rough hierarchy from constructive-with-ball to defensive-without-ball, but the
ordering is explicit and locked — it must not be changed without updating the tiebreak
documentation in §2.1.2 and the composure model in Section 3.

```csharp
/// <summary>
/// The complete set of actions available to a field agent at Stage 0.
/// Ordinal values are significant — used as deterministic tiebreakers in SelectAction().
/// Do NOT reorder without updating §2.1.2 and Section 3 composure model documentation.
/// Stage 1 will extend this enum with HEADER and SET_PIECE subtypes.
/// </summary>
public enum ActionType
{
    PASS              = 0,   // Has ball; dispatch to Pass Mechanics #5
    SHOOT             = 1,   // Has ball; dispatch to Shot Mechanics #6
    DRIBBLE           = 2,   // Has ball; dispatch to Agent Movement #2
    HOLD              = 3,   // Has ball; fallback; dispatch to Agent Movement #2
    MOVE_TO_POSITION  = 4,   // No ball; tactical repositioning; dispatch to Agent Movement #2
    PRESS             = 5,   // No ball; pressing trigger; dispatch to Agent Movement #2
    INTERCEPT         = 6,   // No ball; intercept trajectory; dispatch to Agent Movement #2
}
```

---

### 2.2.2 AgentAction Struct

`AgentAction` is the output of the Decision Tree pipeline. It is a value struct produced
once per agent per heartbeat and dispatched to the appropriate execution system in Step 6.
It is not persisted across ticks by the DT; execution systems own any multi-frame state
arising from the dispatched action (KD-5, §1.4).

```csharp
/// <summary>
/// The typed action instruction produced by the Decision Tree for one agent on one
/// heartbeat tick. Dispatched synchronously to the appropriate execution system in
/// pipeline Step 6. Owned by: Decision Tree Specification #8.
///
/// Lifetime: Created in SelectAction(); dispatched in DispatchAction(); not retained.
/// Cross-tick state: NOT the DT's responsibility. Execution systems own windup states.
/// </summary>
public readonly struct AgentAction
{
    /// <summary>
    /// The agent this action was produced for.
    /// Must match the AgentId from the PerceptionSnapshot that initiated this pipeline run.
    /// </summary>
    public readonly int AgentId;

    /// <summary>
    /// The selected action type. Determines which execution system receives this action
    /// and which optional fields are populated.
    /// </summary>
    public readonly ActionType Type;

    /// <summary>
    /// Target agent for PASS, PRESS, and INTERCEPT actions.
    /// -1 when not applicable (SHOOT, DRIBBLE, HOLD, MOVE_TO_POSITION).
    /// For PASS: the selected teammate's AgentId.
    /// For PRESS: the selected opponent's AgentId.
    /// For INTERCEPT: the ball's assigned AgentId is not used; -1; TargetPosition is used.
    /// </summary>
    public readonly int TargetAgentId;

    /// <summary>
    /// Target world position for MOVE_TO_POSITION, DRIBBLE, and INTERCEPT actions.
    /// Vector2.zero when not applicable (PASS, SHOOT, HOLD, PRESS).
    /// For MOVE_TO_POSITION: TacticalContext.FormationSlot for this agent.
    /// For DRIBBLE: best open-space vector, 1.0–3.0m ahead of agent.
    /// For INTERCEPT: projected ball intercept point in world space.
    /// </summary>
    public readonly Vector2 TargetPosition;

    /// <summary>
    /// Populated only when Type == PASS. Contains the full PassRequest contract
    /// defined in Pass Mechanics Specification #5 §4.
    /// Default (uninitialised) when Type != PASS.
    /// </summary>
    public readonly PassRequest PassParams;

    /// <summary>
    /// Populated only when Type == SHOOT. Contains the full ShotRequest contract
    /// defined in Shot Mechanics Specification #6 §3.1.
    /// Default (uninitialised) when Type != SHOOT.
    /// </summary>
    public readonly ShotRequest ShotParams;

    /// <summary>
    /// The EffectiveUtility score at the moment of selection, after Composure noise
    /// injection. Retained for debug output and replay system. Not used post-dispatch.
    /// </summary>
    public readonly float UtilityScore;

    /// <summary>
    /// The simulation heartbeat tick on which this action was selected.
    /// Must equal the orchestrator's current tick. Used by replay system and test assertions.
    /// </summary>
    public readonly int HeartbeatTick;

    // NOTE: TiebreakerApplied is intentionally NOT a field on AgentAction.
    // It is internal selection metadata with no meaning for execution systems.
    // It is published exclusively via DecisionMadeEvent (§2.2.7) for replay and debug.
    // Execution systems (Pass Mechanics, Shot Mechanics, Agent Movement) must not
    // receive or act on selection provenance — they receive only the action itself.
}
```

---

### 2.2.3 ActionOption Struct

`ActionOption` is an intermediate struct used during option generation (Step 3) and
scoring (Step 4). It is not dispatched; it is consumed within the pipeline and discarded
after `SelectAction()` in Step 5. It must not be exposed outside the `DecisionTree`
class.

```csharp
/// <summary>
/// Intermediate candidate action used during GenerateOptions() and ScoreOptions().
/// Not dispatched. Discarded after SelectAction(). Internal to DecisionTree.cs.
/// One ActionOption exists per generated candidate action.
/// For PASS: one ActionOption per viable VisibleTeammate (up to Decisions-limited cap).
/// </summary>
internal struct ActionOption
{
    /// <summary>The action type this candidate represents.</summary>
    public ActionType Type;

    /// <summary>
    /// Target agent ID for pass/press/intercept candidates. -1 if not applicable.
    /// Copied into AgentAction.TargetAgentId if this option is selected.
    /// </summary>
    public int TargetAgentId;

    /// <summary>
    /// Target position for move/dribble/intercept candidates. Vector2.zero if not applicable.
    /// </summary>
    public Vector2 TargetPosition;

    /// <summary>
    /// Raw utility score computed in ScoreOptions() before Composure noise injection.
    /// Range: [0.01, 1.0] (clamped). Used in SelectAction() as the base for EffectiveUtility.
    /// </summary>
    public float BaseUtility;

    /// <summary>
    /// Final utility score after Composure noise injection. Computed in SelectAction().
    /// Range: [0.01, 1.0 + max_noise_cap]. Used for final ranking only.
    /// </summary>
    public float EffectiveUtility;

    /// <summary>
    /// Intermediate PassRequest parameters, populated during PASS option generation.
    /// Transferred to AgentAction.PassParams if this option is selected.
    /// Default-initialised for non-PASS candidates.
    /// </summary>
    public PassRequest PassParams;

    /// <summary>
    /// Intermediate ShotRequest parameters, populated during SHOOT option generation.
    /// Transferred to AgentAction.ShotParams if this option is selected.
    /// Default-initialised for non-SHOOT candidates.
    /// </summary>
    public ShotRequest ShotParams;
}
```

---

### 2.2.4 DecisionContext Struct

`DecisionContext` is assembled in Step 2 and carries all information the DT requires
for Steps 3–5. It is the single point of data access for option generation and scoring.
No step after Step 2 may read from the original `PerceptionSnapshot`, `MatchContext`,
`TacticalContext`, or `AgentState` directly — all data is routed through this struct.

This design enforces the no-omniscience constraint (AR-2, Outline) and prevents any
accidental world state reads in scoring logic.

```csharp
/// <summary>
/// Assembled in AssembleDecisionContext() (Step 2). Carries all information required
/// for GenerateOptions(), ScoreOptions(), and SelectAction(). Steps 3–5 must not
/// read from original source structs; they must read exclusively from this struct.
///
/// This is the single enforcement boundary for the no-omniscience constraint:
/// if a field is not in DecisionContext, the DT cannot use it. Period.
/// </summary>
internal struct DecisionContext
{
    // ── Agent identity ────────────────────────────────────────────────────────
    public int     AgentId;
    public int     HeartbeatTick;
    public bool    IsHomeTeam;

    // ── Possession state (from PerceptionSnapshot) ────────────────────────────
    /// <summary>True if this agent is currently in possession of the ball.</summary>
    public bool    HasPossession;

    // ── Visible agents (copied from PerceptionSnapshot) ───────────────────────
    /// <summary>
    /// All teammates visible to this agent this tick.
    /// Copied by value from PerceptionSnapshot.VisibleTeammates (span is not retained).
    /// Logical length ≤ PerceptionSystem.MAX_VISIBLE_TEAMMATES.
    ///
    /// ⚠ IMPLEMENTATION CONSTRAINT — NO PER-TICK HEAP ALLOCATION:
    /// At 22 agents × 10Hz, a naive PerceivedAgent[] allocation would produce 220
    /// heap allocations per second, creating continuous GC pressure incompatible with
    /// Unity's real-time budget. This field MUST be backed by a pre-allocated pool.
    ///
    /// Required implementation pattern (Section 6 performance analysis must verify):
    ///   - Option A (preferred): Per-agent pre-allocated fixed-size NativeArray
    ///     (Unity.Collections). Size = MAX_VISIBLE_TEAMMATES. Populated in Step 2;
    ///     length tracked via VisibleTeammateCount. Zero-allocation per tick.
    ///   - Option B: Object pool with ArrayPool&lt;PerceivedAgent&gt;.Shared.Rent/Return.
    ///     Acceptable only if Section 6 confirms GC pressure is within budget.
    ///
    /// The interface here uses int VisibleTeammateCount + fixed-size backing storage.
    /// Section 6 must validate the chosen pattern against the 4ms DT budget (FR-12).
    /// </summary>
    public PerceivedAgent[] VisibleTeammates;   // Backing store — pre-allocated, not newed per tick

    /// <summary>
    /// Valid element count in VisibleTeammates. Range: [0, MAX_VISIBLE_TEAMMATES].
    /// Steps 3–5 must iterate VisibleTeammates[0..VisibleTeammateCount-1] only.
    /// </summary>
    public int VisibleTeammateCount;

    /// <summary>
    /// All opponents visible to this agent this tick.
    /// Same pooling constraint as VisibleTeammates — must NOT be newed per tick.
    /// </summary>
    public PerceivedAgent[] VisibleOpponents;   // Backing store — pre-allocated, not newed per tick

    /// <summary>Valid element count in VisibleOpponents.</summary>
    public int VisibleOpponentCount;

    /// <summary>
    /// True if the ball is within this agent's visible FoV this tick.
    /// False means agent has no perception of ball position; MOVE_TO_POSITION is generated.
    /// </summary>
    public bool    BallVisible;

    /// <summary>
    /// Ball position in world space as perceived by this agent. Valid only if BallVisible.
    /// Sourced from PerceptionSnapshot — NOT from BallState (omniscience prohibition).
    /// </summary>
    public Vector2 PerceivedBallPosition;

    /// <summary>
    /// Ball velocity as perceived by this agent. Used for INTERCEPT trajectory estimation.
    /// </summary>
    public Vector2 PerceivedBallVelocity;

    /// <summary>
    /// Perceived ball height above ground (metres). SHOOT is only generated if ≤ 0.5m.
    /// </summary>
    public float   PerceivedBallHeight;

    // ── Pressure scalar (from PerceptionSnapshot) ─────────────────────────────
    /// <summary>
    /// Pressure scalar [0.0, 1.0] for this agent this tick.
    /// Computed by Perception System §3.6. High = heavily pressed.
    /// Used in all RiskPenalty calculations in Section 3.
    /// </summary>
    public float   PressureScalar;

    // ── Agent attributes (from AgentState.PlayerAttributes; read once in Step 2) ─
    /// <summary>Normalised [0,1]: A_x = (raw − 1) / 19. Read once; cached here.</summary>
    public float   A_Passing;
    public float   A_Shooting;
    public float   A_Dribbling;
    public float   A_Decisions;
    public float   A_Vision;
    public float   A_Composure;
    public float   A_Anticipation;
    public float   A_Aggression;
    public float   A_WorkRate;
    public float   A_Stamina;
    public float   A_Positioning;
    public float   A_Agility;
    public float   A_Pace;
    public float   A_LongShots;
    public float   A_Crossing;

    // ── Match context (from MatchContext; read-only) ───────────────────────────
    public int            HomeScore;
    public int            AwayScore;
    public float          MatchTimeSeconds;
    public PossessionState Possession;   // HOME_TEAM, AWAY_TEAM, CONTESTED
    public MatchPhase     Phase;         // OPEN_PLAY, SET_PIECE_*, KICK_OFF
    public FieldZone      BallZone;      // DEFENSIVE, MIDFIELD, ATTACKING (pre-computed by orchestrator)

    // ── Tactical context (from TacticalContext; Stage 0 hardcoded defaults) ───
    public Vector2       FormationSlot;       // This agent's tactical position target
    public PressingMode  PressingInstruction; // HIGH, MEDIUM, LOW — Stage 0: always MEDIUM
    public PassingStyle  PassingInstruction;  // DIRECT, MIXED, SHORT — Stage 0: always MIXED
    public float         DefensiveLineDepth;  // Normalised [0,1] — Stage 0: 0.5 (MEDIUM)
}
```

---

### 2.2.5 MatchContext Struct

`MatchContext` carries publicly visible game state that all agents may observe without
violating the no-omniscience constraint. It is not world state — it is the equivalent
of the scoreboard and clock that any observer in the ground can see. It is owned by
the Decision Tree specification; the orchestrator populates it each tick.

```csharp
/// <summary>
/// Publicly visible game state. The equivalent of what appears on the scoreboard:
/// score, time, possession indicator, match phase, and ball zone.
/// This is NOT world state — it is permissible for the DT to read.
/// Owned by: Decision Tree Specification #8.
/// Populated by: simulation orchestrator, once per heartbeat tick, before DT runs.
/// Consumed by: Decision Tree (read-only). Accessible from DecisionContext only.
/// </summary>
public readonly struct MatchContext
{
    public readonly int            HomeScore;
    public readonly int            AwayScore;

    /// <summary>Elapsed match time in seconds. [0, 5400] for a 90-minute match.</summary>
    public readonly float          MatchTimeSeconds;

    /// <summary>
    /// Current possession state. Derived by orchestrator from BallState.
    /// HOME_TEAM: home team agent has possession.
    /// AWAY_TEAM: away team agent has possession.
    /// CONTESTED: no agent has possession (ball in air, 50/50 challenge, etc.)
    /// </summary>
    public readonly PossessionState Possession;

    /// <summary>Current match phase. Used to suppress set piece actions at Stage 0.</summary>
    public readonly MatchPhase      Phase;

    /// <summary>
    /// Coarse field zone of the ball: DEFENSIVE, MIDFIELD, or ATTACKING.
    /// Derived from BallState.Position by the orchestrator before the DT pipeline runs.
    /// The DT reads this field only — it does not read BallState directly.
    ///
    /// Zone thresholds (from own goal line, standard 105m pitch): [GT]
    ///   DEFENSIVE:  0m – 35m   (own third)
    ///   MIDFIELD:   35m – 65m  (middle third)
    ///   ATTACKING:  65m – 105m (final third)
    ///
    /// [GT] These values are gameplay-tuned initial estimates. They are proportional
    /// to pitch length (1/3 thirds) and consistent with conventional football zone
    /// conventions. They are NOT sourced from Perception System §3.4, which does not
    /// define coarse FieldZone thresholds.
    ///
    /// XC-NOTE: The §9 Approval Checklist must verify that Perception System §3.4
    /// contains no conflicting zone threshold definitions. If Perception §3.4 defines
    /// zone thresholds in a future revision, these values must be unified and owned
    /// by a single specification (Match Configuration, Stage 1). This is flagged as
    /// a potential consistency risk, not a current conflict.
    /// </summary>
    public readonly FieldZone       BallZone;
}
```

**Supporting enumerations (owned by this specification):**

```csharp
public enum PossessionState { HOME_TEAM, AWAY_TEAM, CONTESTED }
public enum MatchPhase      { OPEN_PLAY, SET_PIECE_HOME, SET_PIECE_AWAY, KICK_OFF }
public enum FieldZone       { DEFENSIVE, MIDFIELD, ATTACKING }
public enum PressingMode    { HIGH, MEDIUM, LOW }
public enum PassingStyle    { DIRECT, MIXED, SHORT }
```

---

### 2.2.6 TacticalContext Struct (Stage 0 Stub)

`TacticalContext` carries tactical instructions injected by the Formation System (Stage 1).
At Stage 0, all fields are hardcoded to conservative defaults. The struct is fully defined
here to prevent field additions at Stage 1 from becoming breaking changes; Stage 1 wires
the Formation System to populate real values. The field set is frozen at Stage 0.

```csharp
/// <summary>
/// Tactical instruction set for one team. Injected into each agent's DecisionContext
/// in Step 2. Stage 0: all fields hardcoded to defaults below.
/// Stage 1: Formation System populates this struct with real team instructions.
///
/// IMPORTANT: The field set is frozen at Stage 0. Stage 1 may only change VALUES,
/// not add or remove fields. Any field addition requires a specification amendment.
/// </summary>
public struct TacticalContext
{
    /// <summary>
    /// This agent's tactical position target in world space. Used by MOVE_TO_POSITION.
    /// Stage 0: derived from a fixed 4-4-2 flat formation at kick-off positions.
    /// Stage 1: live formation slot from Formation System.
    /// </summary>
    public Vector2      FormationSlot;

    /// <summary>Stage 0 default: MEDIUM.</summary>
    public PressingMode  PressingInstruction;

    /// <summary>Stage 0 default: MIXED.</summary>
    public PassingStyle  PassingInstruction;

    /// <summary>
    /// Defensive line depth. Normalised [0.0, 1.0].
    /// 0.0 = deep defensive block. 1.0 = very high line.
    /// Stage 0 default: 0.5 (MEDIUM). Used by MOVE_TO_POSITION and PRESS scoring.
    /// </summary>
    public float         DefensiveLineDepth;

    /// <summary>
    /// Stage 0 factory method. Returns a TacticalContext with hardcoded defaults
    /// for all fields. Called by orchestrator at match initialisation.
    /// </summary>
    public static TacticalContext Stage0Default(Vector2 formationSlot) => new TacticalContext
    {
        FormationSlot        = formationSlot,
        PressingInstruction  = PressingMode.MEDIUM,
        PassingInstruction   = PassingStyle.MIXED,
        DefensiveLineDepth   = 0.5f,
    };
}
```

---

### 2.2.7 DecisionMadeEvent Struct (Stage 0 Stub)

`DecisionMadeEvent` is published at the end of Step 6 for each dispatched action. At
Stage 0 it is a stub: the Event System (#17) is not yet specified, so this struct is
defined and published but not consumed by any downstream system. It is essential for the
replay system and statistics pipeline at Stage 2+; defining it now prevents field
additions from becoming breaking changes.

```csharp
/// <summary>
/// Published by the Decision Tree for each AgentAction dispatched. Stage 0: stub.
/// No subscriber consumes this event at Stage 0. Event System #17 (Stage 2) will wire
/// subscribers for replay, statistics, and tactical analytics.
///
/// Field set is frozen at Stage 0. Stage 2 may only add subscribers, not new fields,
/// without a specification amendment.
/// </summary>
public readonly struct DecisionMadeEvent
{
    public readonly int        AgentId;
    public readonly ActionType SelectedAction;
    public readonly float      UtilityScore;       // EffectiveUtility of selected action
    public readonly int        CandidateCount;     // Total candidates generated in Step 3
    public readonly int        HeartbeatTick;
    public readonly bool       TiebreakerApplied;  // True if equal-utility tiebreak was used
    public readonly bool       FallbackToHold;     // True if HOLD was selected as fallback (no viable candidates)
}
```

---

### 2.2.8 Data Structure Ownership Summary

| Struct / Enum | Owned By | Consumed By | Notes |
|---|---|---|---|
| `ActionType` | DT Spec #8 | DT, Pass Mechanics, Shot Mechanics, Agent Movement | Stage 0: 7 types. Stage 1: HEADER added. |
| `AgentAction` | DT Spec #8 | Pass Mechanics, Shot Mechanics, Agent Movement | Dispatched synchronously; not retained by DT. |
| `ActionOption` | DT Spec #8 | Internal to DT only | Discarded after SelectAction(). Not exposed. |
| `DecisionContext` | DT Spec #8 | Internal to DT only | Assembled per pipeline run; discarded after DispatchAction(). |
| `MatchContext` | DT Spec #8 | DT (read-only) | Populated by orchestrator each tick. |
| `TacticalContext` | DT Spec #8 | DT (read-only), Formation System (Stage 1 writer) | Stage 0: hardcoded. Stage 1: Formation System populates. |
| `DecisionMadeEvent` | DT Spec #8 | Event System #17 (Stage 2) | Stage 0: published but not consumed. |
| `PossessionState` | DT Spec #8 | DT, orchestrator | Enum; 3 values. |
| `MatchPhase` | DT Spec #8 | DT | Enum; 4 values at Stage 0. |
| `FieldZone` | DT Spec #8 | DT | Enum; 3 values. Consistent with Perception System §3.4 zone thresholds. |
| `PressingMode` | DT Spec #8 | DT, TacticalContext | Enum; 3 values. Stage 1: Formation System sets. |
| `PassingStyle` | DT Spec #8 | DT, TacticalContext | Enum; 3 values. Stage 1: Formation System sets. |
| `PerceptionSnapshot` | Perception System #7 | DT (read-only, by value) | Defined in Perception §4. DT defines intake method only. |
| `PassRequest` | Pass Mechanics #5 | DT (populates), Pass Mechanics (consumes) | Defined in Pass Mechanics §4. DT populates in Step 3/5. |
| `ShotRequest` | Shot Mechanics #6 | DT (populates), Shot Mechanics (consumes) | Defined in Shot Mechanics §3.1. DT populates in Step 3/5. |

---

