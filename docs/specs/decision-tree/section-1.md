# Decision Tree Specification #8 — Section 1: Purpose & Scope

**File:** `Decision_Tree_Spec_Section_1_v1_1.md`  
**Purpose:** Defines the complete scope boundary for Decision Tree Specification #8 —
what this system owns, what it explicitly does not own, all key architectural decisions
locked before technical drafting begins, relationships to adjacent systems, and
dependency contracts. This is the authoritative scope reference for the entire
specification.

**Created:** February 27, 2026, 12:00 PM PST  
**Version:** 1.1  
**Status:** DRAFT — Awaiting Lead Developer Review  
**Specification Number:** 8 of 20 (Stage 0 — Physics Foundation)  
**Author:** Claude (AI) with Anton (Lead Developer)

**Dependencies confirmed stable:**
- Ball Physics Specification #1 (approved) — `BallState` struct: position, velocity, spin
- Agent Movement Specification #2 (approved) — `AgentState`, `PlayerAttributes` (all
  Stage 0 attributes including `Decisions`, `Vision`, `Composure`, `Anticipation`)
- Collision System Specification #3 (approved) — spatial hash query interface; tackle
  interrupt signal
- First Touch Mechanics Specification #4 (approved) — ball possession state contract;
  no direct call from DT at Stage 0
- Pass Mechanics Specification #5 (approved) — `PassRequest` caller contract; DT
  populates and dispatches
- Shot Mechanics Specification #6 (approved) — `ShotRequest` caller contract; DT
  populates and dispatches
- Perception System Specification #7 (approved) — `PerceptionSnapshot` struct; delivery
  mechanism (direct method call from simulation orchestrator, resolved OQ-1)
- Decision Tree Outline v1.1 (approved — all OQ-1 through OQ-5 resolved)

**Open Dependency Flags:** None. All hard dependencies are confirmed stable.

---

## Table of Contents

- [1.1 Document Purpose](#11-document-purpose)
- [1.2 What This Specification Covers](#12-what-this-specification-covers)
- [1.3 What Is Out of Scope](#13-what-is-out-of-scope)
  - [1.3.1 Responsibilities Owned by Other Specifications](#131-responsibilities-owned-by-other-specifications)
  - [1.3.2 Features Deferred to Stage 1+](#132-features-deferred-to-stage-1)
  - [1.3.3 Adjacent-Spec Expectation Warning](#133-adjacent-spec-expectation-warning)
- [1.4 Key Design Decisions](#14-key-design-decisions)
- [1.5 Stage 0 Action Set](#15-stage-0-action-set)
- [1.6 Stage 0 Deliverables and Known Limitations](#16-stage-0-deliverables-and-known-limitations)
  - [1.6.1 Stage 0 Deliverables](#161-stage-0-deliverables)
  - [1.6.2 Stage 0 Known Limitations](#162-stage-0-known-limitations)
- [1.7 Dependencies and Integration Contracts](#17-dependencies-and-integration-contracts)
  - [1.7.1 Hard Dependencies](#171-hard-dependencies--must-be-stable-before-section-3)
  - [1.7.2 Soft Dependencies](#172-soft-dependencies--forward-references-to-unwritten-specifications)
- [1.8 Version History](#18-version-history)

---

## 1.1 Document Purpose

This section defines the scope boundaries for Decision Tree Specification #8. It serves
as the authoritative reference for:

1. **What this specification owns** — The complete agent cognition pipeline: receiving
   a `PerceptionSnapshot`, generating candidate actions, scoring them via
   attribute-weighted utility functions, selecting the final action through a
   composure-modulated process, and dispatching the result to the appropriate execution
   system.

2. **What other specifications own** — The physics of action execution (Pass Mechanics,
   Shot Mechanics, Agent Movement), the content of the perceptual world view (Perception
   System), ball collision response (Collision System), and all tactical formation logic
   (Formation System, Stage 1+).

3. **Key design decisions** — Seven architectural decisions, locked before Section 3
   drafting begins, with full rationale and cross-references to the risks they resolve.

4. **Implementation timeline** — Stage 0 deliverables, explicitly named Stage 0
   limitations, and the stages at which those limitations are resolved.

5. **Dependencies** — Required interfaces from all upstream specifications and forward
   contracts with downstream specifications not yet written.

The Decision Tree is the agent brain. It receives the output of the Perception System
— a snapshot of what this agent currently believes the world looks like — and must
produce a single typed action to be dispatched to an execution system. It does this
10 times per second for every one of the 22 active field agents.

The central design problem is **epistemic and evaluative**. Given a filtered, imperfect
view of the match state, what is the best action this agent can take? The answer must
depend on the agent's attributes — a technically gifted playmaker with high Vision
should make better passing decisions than a low-quality midfielder. A player with high
Composure under pressure should make better decisions than a nervous one in the same
situation. A striker inside the box should prioritise shooting over dribbling. These
outcomes must *emerge* from the evaluation model — not from hand-coded priority tables.

The utility-based scoring model is the architectural answer to this problem. Every
candidate action receives a score. Scores are derived from perceptual inputs,
attribute values, and contextual factors. The agent selects the highest-scoring
action, modulated by Composure-based noise. The model is fully transparent,
parameter-driven, and tunable without structural change.

**On action classification:** Terms such as "pressing," "holding," and "dribbling"
are common match terminology. They are not emergent or inferred states in this system
— they are explicit `ActionType` enum values selected by a formal scoring process.
An agent does not "decide to press" through accumulated game state. It evaluates a
PRESS candidate action against all others, and the score determines the outcome.
The clarity of the action taxonomy is a deliberate design choice that maximises
auditability and testability.

---

## 1.2 What This Specification Covers

Decision Tree Specification #8 governs the agent cognition pipeline: the process
by which each agent converts its `PerceptionSnapshot` into an `AgentAction`, executed
once per 10Hz tactical heartbeat for each of the 22 active field agents.

**Specifically, this specification covers:**

**`AgentAction` struct definition and type taxonomy.** The struct produced by this
system and consumed by execution systems is defined here. Seven action types at
Stage 0. All fields, types, and frame stamps are owned by this specification.

**`PerceptionSnapshot` intake interface.** Perception System Specification #7
(§4.5.3) explicitly deferred definition of the delivery mechanism to this
specification. Section 3 of this specification defines `ReceiveSnapshot()` — the
method by which the simulation orchestrator pushes snapshots to each agent's
Decision Tree instance at the appropriate point in the heartbeat evaluation order.

**`MatchContext` struct definition.** Publicly visible game state (score, match time,
possession indicator, match phase, and ball zone) that any agent may access, distinct
from world state. `BallZone` (Defensive / Midfield / Attacking) is pre-computed by the
simulation orchestrator from `BallState.Position` each heartbeat before DT evaluation
begins. The DT reads this pre-computed value — it does not derive zone from `BallState`
directly, which would constitute an unauthorised world state read. This struct is defined
here and is the only game-state read authorised outside of `PerceptionSnapshot`.

**`TacticalContext` stub.** At Stage 0, tactical instructions are hardcoded defaults
(MEDIUM pressing, MIXED passing, MEDIUM defensive line). The struct is fully defined
with all fields that Stage 1 will populate; the values are fixed for Stage 0. This
prevents Stage 0 behaviour regression when Stage 1 wires real formation instructions.

**Option generation.** Candidate actions are enumerated from the snapshot state.
Possession determines the candidate set: an agent with the ball evaluates PASS,
SHOOT, DRIBBLE, and HOLD; an agent without the ball evaluates MOVE_TO_POSITION,
PRESS, and INTERCEPT. Generation is rule-based and does not involve scoring.

**Utility scoring model.** Each candidate action receives a score derived from
perceptual context, `PlayerAttributes`, `TacticalContext`, and `MatchContext`.
Section 3 defines the full scoring formula for all seven action types, including
attribute exponents, zone modifiers, and risk penalty weights.

**Composure noise model.** A bounded, deterministic noise term is applied to the
final utility score before selection. Higher Composure produces smaller noise bounds.
The noise source is a deterministic hash of `matchSeed + agentId + heartbeatTick`
— not `System.Random`. This is the sole source of per-agent score variance and is
fully reproducible.

**`AgentAction` dispatch.** The highest-scoring action after composure modulation
is dispatched to the appropriate execution system in the same heartbeat it is
selected. Dispatch routing (Pass Mechanics, Shot Mechanics, Agent Movement) is
defined in Section 4.

**State machine.** Decision Tree states IDLE, EVALUATING, EXECUTING, and INTERRUPTED
are defined with full transition semantics. Mid-execution interrupt handling (tackle
during pass windup) is covered in Section 3.

**Event publication stub.** `DecisionMadeEvent` is published for each selected
action. The struct is defined here; consumption by the Event System (Spec #17) is
a soft dependency.

**Edge case handling.** No viable action, simultaneous equal-utility candidates
(deterministic tiebreak), ball lost from snapshot, malformed snapshot, and interrupt
during execution are all addressed in Section 3.

**`PassRequest` and `ShotRequest` population logic.** When DT selects PASS or SHOOT,
it must correctly populate the request structs defined in Pass Mechanics §4 and Shot
Mechanics §3.1. Section 3 defines all field population logic. Incorrect population is
a silent failure mode (AR-5, outline); the mapping is fully specified and tested.

**Full test suite.** Approximately 90–110 tests covering unit, integration, and
balance validation. Target exceeds minimum project requirement. Tests are in Section 5.

---

## 1.3 What Is Out of Scope

### 1.3.1 Responsibilities Owned by Other Specifications

**Action execution physics** — what happens to the ball or agent after an action
is dispatched is entirely outside this system. Pass Mechanics owns pass execution.
Shot Mechanics owns shot execution. Agent Movement owns locomotion. Once `AgentAction`
is dispatched, this system's responsibility for that action ends.

**`PerceptionSnapshot` computation** — how the snapshot is constructed, filtered,
and delivered is owned by Perception System Specification #7. The Decision Tree
treats the snapshot as a read-only input and has no visibility into how it was built.

**Spatial hash maintenance** — the Collision System owns the spatial index. The
Decision Tree may query it (for range checks during option generation) but may not
write to it or depend on its internal implementation.

**Ball possession determination** — whether an agent is considered to possess the
ball is determined by the Collision System / First Touch integration boundary, not
by this specification. The Decision Tree reads possession state from the snapshot;
it does not compute it.

**Formation and tactical instruction derivation** — `TacticalContext` at Stage 0
is a stub with hardcoded defaults. Formation System (Stage 1+) owns the logic that
populates real team instructions. This specification defines the struct; it does not
own the values.

**Goalkeeper-specific decision logic** — at Stage 0, the goalkeeper is treated as
a standard outfield agent with limited action options. Goalkeeper Mechanics (#11)
will replace this with a full goalkeeper decision model at Stage 1.

### 1.3.2 Features Deferred to Stage 1+

**Team tactical differentiation** — `TacticalContext` is identical for both teams
at Stage 0. Stage 1 wires the Formation System and produces team-specific instructions.

**Aerial decisions** — no agent decides to head the ball at Stage 0. Any ball above
0.5m is outside this system's decision scope. Heading Mechanics (#10) integration
is a Stage 1 deliverable.

**Set piece AI** — corners, free kicks, throw-ins, and goal kicks produce HOLD or
MOVE_TO_POSITION only at Stage 0. Specialist set piece AI is Stage 2+.

**Goalkeeper decision model** — Goalkeeper Mechanics (#11) integration deferred to
Stage 1. Goalkeeper at Stage 0 is an outfield agent with degraded action options.

**Multi-agent coordination** — Positioning AI (#12), Pressing AI (#13), and
coordinated pressing triggers are Stage 1+. Individual agents press independently
at Stage 0 based on proximity and utility score.

**Vision attribute broader effects** — at Stage 0, `Vision` affects pass lane
assessment only. Broader Vision effects (anticipation of dangerous runs, off-ball
awareness) are Stage 1+.

**Fixed64 migration** — floating-point arithmetic is used throughout at Stage 0.
Migration hooks are documented in Section 7. Fixed64 Spec #9 resolves this.

### 1.3.3 Adjacent-Spec Expectation Warning

Pass Mechanics (#5), Shot Mechanics (#6), First Touch (#4), and Heading Mechanics
(#10) all reference "Decision Tree" as their action caller. This is accurate at
the architectural level but requires clarification about Stage 0 scope:

- **Pass Mechanics (#5):** DT calls this. Interface fully active at Stage 0.
- **Shot Mechanics (#6):** DT calls this. Interface fully active at Stage 0.
- **First Touch (#4):** DT does **not** call this directly. First Touch is triggered
  by the Collision System when a ball contacts an agent. DT has no direct interface
  with First Touch at Stage 0.
- **Heading Mechanics (#10):** DT does **not** call this at Stage 0. Out of scope
  entirely. The interface will be defined when #10 is written (Stage 1).

Implementers reading adjacent specifications and expecting a direct DT → First Touch
or DT → Heading call will not find one in this specification. This is correct.

---

## 1.4 Key Design Decisions

These decisions are locked before Section 3 drafting begins. No decision here may
be reopened without a formal version increment and Lead Developer approval. Each
decision includes the risk or question it resolves.

---

**KD-1 — Perception-first evaluation order: Perception runs at heartbeat tick N,
Decision Tree runs at tick N using that tick's snapshot.**

The simulation orchestrator executes Perception System for all 22 agents, completing
all snapshot computations, before any Decision Tree evaluation begins. DT never reads
a stale N−1 snapshot. This ordering is enforced at the orchestrator level — it is
not a documentation convention.

*Resolves:* Perception System §7.7 KR-5 (critical risk: evaluation order ambiguity).
*Consequence:* Any orchestrator implementation that interleaves Perception and DT
evaluation is a defect, not a performance optimisation.

---

**KD-2 — Utility-based option scoring, not scripted priority ladders.**

Candidate actions are scored by a parametric utility function. The highest score
wins. There are no hard-coded "if attacking third, always shoot" rules. Zone modifiers
and attribute weights produce this behaviour from the utility model. This makes
behaviour auditable, tunable without structural change, and extensible to new action
types in Stage 1+ without touching the evaluation architecture.

*Resolves:* Outline §3.2 design intent — "Football Manager killer" ambition requires
emergent, tunable behaviour, not brittle scripted trees.

---

**KD-3 — Composure attribute introduces bounded, deterministic noise into final
action selection.**

After scoring, a noise term bounded by `[−NOISE_MAX, +NOISE_MAX]` is added to each
candidate's utility. The bound scales inversely with `Composure` [1–20]: maximum
Composure produces near-zero noise; minimum Composure produces maximum noise.
The noise value is derived from `hash(matchSeed, agentId, heartbeatTick, actionType)`
— not `System.Random`. This guarantees that identical match seeds produce identical
decision sequences, which is the determinism requirement.

*Resolves:* Determinism requirement (Master Vol 1 §1.3). Creates meaningful
skill differentiation between Composure tiers without arbitrary random behaviour.

---

**KD-4 — Decision Tree consumes `PerceptionSnapshot` by value, not by reference.**

The snapshot is a value struct. The orchestrator passes a copy to each agent's DT
instance. The DT cannot modify the snapshot, cannot cache a reference for use in
a subsequent tick, and is guaranteed to be working with snapshot data that cannot
be mutated by another agent's evaluation in the same tick.

*Resolves:* Outline AR-3 (critical: `ReadOnlySpan<PerceivedAgent>` cannot be stored
across ticks). Value copy eliminates span lifetime concerns entirely.

---

**KD-5 — Execution systems own all multi-frame action state. DT treats each
heartbeat as fresh.**

When a PASS is in windup (a multi-frame action in Pass Mechanics), the Decision
Tree does not track or manage that state. Pass Mechanics owns the windup state
machine. At the next heartbeat, DT evaluates fresh options without consulting any
cross-tick memory. If an interrupt arrives (tackle), it comes from the Collision
System to the executing system (Pass Mechanics) first; the DT is notified via
the INTERRUPTED state transition.

*Resolves:* Outline OQ-2 (does DT persist state across heartbeats? No.).
Consistent with Pass Mechanics and Shot Mechanics architecture.

---

**KD-6 — `TacticalContext` is injected as a read-only struct. DT does not own or
modify it.**

Tactical instructions are external constraints, not DT output. At Stage 0, the
`TacticalContext` struct is hardcoded to MEDIUM pressing, MIXED passing, MEDIUM
defensive line for all agents on both teams. Stage 1 wires the Formation System to
populate this struct with real team-specific instructions. The struct fields are
fully defined at Stage 0 to prevent field additions from becoming breaking changes
at Stage 1.

*Resolves:* Outline OQ-4 (how are TacticalContext instructions handled at Stage 0?).

---

**KD-7 — Decision Tree evaluation is sequential across all 22 agents in ascending
AgentId order. No shared mutable state exists between agent evaluations.**

Agents are evaluated 0 through 21. Each evaluation is fully independent — no agent's
decision reads or modifies any state that another agent's in-progress evaluation
depends on. This, combined with the deterministic hash in KD-3, guarantees that
the full 22-agent evaluation sequence is reproducible for any given match seed
and heartbeat tick number.

*Resolves:* Outline OQ-5 (sequential or parallel evaluation? Sequential, ascending
AgentId.). Prevents subtle non-determinism from parallel evaluation of shared state.

---

## 1.5 Stage 0 Action Set

At Stage 0, the Decision Tree selects from exactly seven action types. This set
is fixed; no extensibility hooks for new types are provided at this stage (KD-7 of
outline §1.4).

| Action Type | Execution System | Eligibility Condition |
|-------------|-----------------|----------------------|
| `PASS` | Pass Mechanics #5 | Agent has ball; ≥1 teammate visible in snapshot; pass viable |
| `SHOOT` | Shot Mechanics #6 | Agent has ball; ball within shooting range; goal visible in snapshot |
| `DRIBBLE` | Agent Movement #2 | Agent has ball; open space ahead detectable in snapshot |
| `HOLD` | Agent Movement #2 | Agent has ball; no safe option scores above threshold; also fallback for FR-08 |
| `MOVE_TO_POSITION` | Agent Movement #2 | Agent does not have ball; tactical positioning |
| `PRESS` | Agent Movement #2 | Agent does not have ball; opponent with ball within `PRESS_TRIGGER_DISTANCE` (defined in §3.1) |
| `INTERCEPT` | Agent Movement #2 | Agent does not have ball; ball trajectory passes within interception window |

**Fallback guarantee (FR-08):** If no candidate action scores above the minimum
viable threshold, or if option generation produces an empty candidate set, the
Decision Tree must produce HOLD (if agent has ball) or MOVE_TO_POSITION (if agent
does not have ball) without throwing an exception. This is the only hard-coded
behaviour in the pipeline and exists solely to prevent null-action states.

**Heading:** No HEADER action type exists at Stage 0. Any ball above 0.5m height
is outside this system's action scope. Agents whose only viable action would be
heading fall through to HOLD (with ball) or MOVE_TO_POSITION (without).

---

## 1.6 Stage 0 Deliverables and Known Limitations

### 1.6.1 Stage 0 Deliverables

- 7-action decision pipeline (PASS, SHOOT, DRIBBLE, HOLD, MOVE_TO_POSITION, PRESS,
  INTERCEPT) with full utility scoring
- `AgentAction` struct (complete field definition, owned by this specification)
- `MatchContext` struct (publicly visible game state; defined here)
- `TacticalContext` stub with all Stage 1-ready fields, hardcoded Stage 0 defaults
- `ReceiveSnapshot(PerceptionSnapshot)` intake method — resolves Perception #7 §4.5.3
  deferral; this is the interface that Perception explicitly did not write
- `PassRequest` and `ShotRequest` population logic (all fields; verified in Section 5)
- Composure noise model — deterministic, attribute-scaled, mathematically derived
- Decision Tree state machine: IDLE → EVALUATING → EXECUTING → INTERRUPTED
- `DecisionMadeEvent` stub struct
- Full test suite targeting 90–110 tests (unit, integration, balance, performance)
- Static analysis constraint: `DecisionTree.cs` may not import `WorldState` namespace
  (enforcement of KD-2 omniscience prohibition — AR-2 of outline)

### 1.6.2 Stage 0 Known Limitations

The following are explicitly acknowledged limitations, not defects. Each has a
documented resolution stage.

| Limitation | Description | Resolution Stage |
|------------|-------------|-----------------|
| **No tactical differentiation between teams** | `TacticalContext` defaults are identical for all agents on both teams. Manager tactical instructions have no effect on DT behaviour at Stage 0. Teams play with identical default parameters. | Stage 1 — Formation System wires real team instructions into `TacticalContext`. |
| **No aerial ball decisions** | Agents never decide to head the ball. Any ball above 0.5m is outside DT scope. An agent who could only head the ball falls through to HOLD or MOVE_TO_POSITION. | Stage 1 — Heading Mechanics #10 integrates with DT. |
| **No set piece differentiation** | Corners, free kicks, throw-ins, and goal kicks produce HOLD or MOVE_TO_POSITION only. No specialist positioning or set piece execution logic. | Stage 2+. |
| **Goalkeeper treated as outfield agent** | No GK-specific actions (saves, distribution, command of area). Goalkeeper receives the standard 7-action pipeline with degraded scoring for actions inappropriate to the position. | Stage 1 — Goalkeeper Mechanics #11 provides a DT extension or override. |
| **Utility constants require post-implementation tuning** | All [GT] constants in Section 3 are principled starting values, not validated gameplay constants. Initial build will not produce optimal match behaviour. A four-phase tuning plan is defined in Section 3. | Ongoing — tuning begins after Stage 0 implementation is complete. |
| **Vision attribute affects pass assessment only** | `PlayerAttributes.Vision` is used only in PASS option scoring (pass lane assessment and through ball lead). Broader Vision effects — anticipation of dangerous opponent runs, off-ball awareness of smart movement — are deferred. | Stage 1. |
| **No coordinated pressing** | PRESS actions are evaluated independently per agent. No agent coordinates its press with teammates' press intentions. A 4-man press is not choreographed — it emerges only if four agents individually score PRESS highly. | Stage 1 — Pressing AI #13 introduces coordinated press triggers. |

---

## 1.7 Dependencies and Integration Contracts

### 1.7.1 Hard Dependencies — Must Be Stable Before Section 3

All hard dependencies are confirmed stable. No blocking items exist.

| Dependency | Specification | Status | Fields / Contract |
|---|---|---|---|
| `BallState.Position`, `BallState.Velocity` | Ball Physics #1 | ✅ Approved | Read via `PerceptionSnapshot.PerceivedBall`; not read from world state directly |
| `AgentState.Position`, `AgentState.Velocity`, `AgentState.FacingDirection` | Agent Movement #2 | ✅ Approved | Read via `PerceptionSnapshot.PerceivedAgents`; not read from world state directly |
| `PlayerAttributes.Decisions`, `.Composure`, `.Vision`, `.Anticipation`, `.Passing`, `.Technique`, `.Shooting`, `.KickPower`, `.WeakFootRating`, `.Crossing` | Agent Movement #2 §3.5.6 | ✅ Approved | Read directly (own agent attributes only); confirmed present per ERR-007 resolution. `.Crossing` required for PASS cross-type scoring in §3.1. |
| Tackle interrupt signal | Collision System #3 | ✅ Approved | Interrupt notification triggers EXECUTING → INTERRUPTED state transition |
| Spatial hash query | Collision System #3 | ✅ Approved | `QueryRadius(position, radius)` used during option generation for PRESS trigger distance check |
| `PerceptionSnapshot` struct | Perception System #7 §4 | ✅ Approved | All fields consumed; struct owned by Perception #7; DT defines intake method only |
| `PassRequest` caller contract | Pass Mechanics #5 §4 | ✅ Approved | DT must populate: `PassType`, `CrossSubType`, `TargetType`, `TargetAgentId`, `TargetPosition`, `IntendedDistance`, `IsWeakFoot`, `UrgencyLevel`, `Frame` |
| `ShotRequest` caller contract | Shot Mechanics #6 §3.1 | ✅ Approved | DT must populate: `PowerIntent`, `ContactZone`, `PlacementTarget`, `SpinIntent`, `IsWeakFoot` |
| `matchSeed` | Simulation root | ✅ Established | Deterministic hash seed for Composure noise; identical pattern to Pass Mechanics and Shot Mechanics |

**Attribute read contract:** All `PlayerAttributes` fields for the evaluating agent
are read once per heartbeat at pipeline entry (Step 2: `AssembleDecisionContext()`)
and cached for the duration of that agent's evaluation. A mid-heartbeat attribute
change is not possible at Stage 0 and cannot corrupt the in-flight evaluation.

**World state prohibition:** `DecisionTree.cs` and all classes in the Decision Tree
namespace may not import or reference the `WorldState` namespace. This is enforced
by a static analysis rule and documented in Section 4. Violation is a defect
regardless of whether the accessed state is also available in the snapshot.

### 1.7.2 Soft Dependencies — Forward References to Unwritten Specifications

| Consumer / Provider | What DT Produces / Receives | Interface Status |
|---|---|---|
| Event System #17 | `DecisionMadeEvent` stub — published per action selected | Struct defined here; Event System not yet specified |
| Formation System (Stage 1) | Real `TacticalContext` values — will replace Stage 0 hardcoded defaults | Struct fields defined here; value population deferred to Stage 1 |
| Heading Mechanics #10 (Stage 1) | HEADER action type and dispatch interface | Not defined at Stage 0; stub placeholder in §7 |
| Goalkeeper Mechanics #11 (Stage 1) | GK-specific action types and DT override/extension mechanism | Not defined at Stage 0 |
| Fixed64 Math Library #9 | Migration of float arithmetic in utility scoring | Float arithmetic used at Stage 0; migration documented in Section 7 |
| Pressing AI #13 (Stage 1) | Coordinated press state — DT will consult before scoring PRESS | Not defined at Stage 0; PRESS scored independently per agent |

---

## 1.8 Version History

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0 | February 27, 2026, 12:00 PM PST | Claude (AI) / Anton | Initial draft. All OQ-1 through OQ-5 from Outline v1.1 reflected. All 7 KDs locked. Stage 0 action set and known limitations formalised. BLK-001 noted as ERR-010 in error log. |
| 1.1 | February 27, 2026 | Claude (AI) / Anton | Three corrections from self-critique: (1) `MatchContext.BallZone` ambiguity resolved — clarified as pre-computed by orchestrator; DT does not read `BallState` directly. (2) `PRESS_TRIGGER_DISTANCE` forward reference in §1.5 action table linked to §3.1. (3) `PlayerAttributes.Crossing` added to §1.7.1 attribute dependency list with note that it is required for cross-type PASS scoring in §3.1. |

---

## Section 1 Summary

Decision Tree Specification #8 governs the complete agent cognition pipeline: the
conversion of `PerceptionSnapshot` into `AgentAction` via utility-based option
scoring, Composure-modulated selection, and execution dispatch. It runs at the
10Hz tactical heartbeat, second in evaluation order after Perception, for all
22 active field agents.

This specification defines `ReceiveSnapshot()` — resolving the interface that
Perception System §4.5.3 explicitly deferred. It owns `AgentAction`, `MatchContext`,
`TacticalContext` (stub), and `DecisionMadeEvent`. It does not own action execution
physics, snapshot computation, spatial hash maintenance, or formation logic.

Seven key design decisions are locked. All hard dependencies are confirmed stable
from seven approved upstream specifications. No blocking items exist.

**Next:** Section 2 — System Overview and Functional Requirements.

---

*End of Section 1 — Decision Tree Specification #8*  
*Tactical Director — Specification #8 of 20 | Stage 0: Physics Foundation*