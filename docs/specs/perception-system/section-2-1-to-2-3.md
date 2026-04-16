# Perception System Specification #7 — Section 2: System Overview & Functional Requirements

**File:** `Perception_System_Spec_Section_2_v1_1.md`
**Purpose:** Defines the complete perception pipeline architecture, the authoritative
`PerceptionSnapshot` and `PerceivedAgent` struct definitions, all functional requirements
governing system behaviour, and the data model that Section 3 must implement. This section
is the bridge between the scope boundary established in Section 1 and the mathematical
models specified in Section 3. All requirements here are testable and binding.

**Created:** February 24, 2026, 3:00 PM PST
**Version:** 1.1
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 7 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)

**Upstream authority:**
- Perception System Outline v1.1 (all OQ-1 through OQ-5 resolved; KD-1 through KD-7 locked)
- Perception System Specification #7 Section 1 v1.1 (scope and decisions established)
- Agent Movement Specification #2 (Approved) — `AgentState`, `PlayerAttributes.Decisions`,
  `PlayerAttributes.Anticipation` confirmed fields
- Ball Physics Specification #1 (Approved) — `BallState.Position` confirmed
- Collision System Specification #3 (Approved) — spatial hash query interface confirmed
- First Touch Mechanics Specification #4 (Approved) — `PressureScalar` formula and
  half-turn orientation bonus (§3.6, §3.3.2) confirmed; consumed read-only here

**Open Dependency Flags:** None. All upstream structs confirmed before this section was drafted.

---

## Table of Contents

- [2.1 System Architecture Overview](#21-system-architecture-overview)
  - [2.1.1 Position in the Simulation Stack](#211-position-in-the-simulation-stack)
  - [2.1.2 Execution Cadence](#212-execution-cadence)
  - [2.1.3 The Epistemic Problem](#213-the-epistemic-problem)
- [2.2 Perception Pipeline](#22-perception-pipeline)
  - [2.2.1 Pipeline Overview](#221-pipeline-overview)
  - [2.2.2 Pipeline Step Descriptions](#222-pipeline-step-descriptions)
  - [2.2.3 Pipeline Invariants](#223-pipeline-invariants)
  - [2.2.4 Forced Mid-Heartbeat Refresh](#224-forced-mid-heartbeat-refresh)
- [2.3 Data Structures — Authoritative Definitions](#23-data-structures--authoritative-definitions)
  - [2.3.1 `PerceptionSnapshot` Struct](#231-perceptionsnapshot-struct)
  - [2.3.2 `PerceivedAgent` Struct](#232-perceivedagent-struct)
  - [2.3.3 `PerceptionSystem` Internal State](#233-perceptionsystem-internal-state)
  - [2.3.4 Struct Ownership and Amendment Authority](#234-struct-ownership-and-amendment-authority)
- [2.4 Functional Requirements](#24-functional-requirements)
  - [2.4.1 Core Pipeline Requirements](#241-core-pipeline-requirements)
  - [2.4.2 Field of View Requirements](#242-field-of-view-requirements)
  - [2.4.3 Occlusion Requirements](#243-occlusion-requirements)
  - [2.4.4 Recognition Latency Requirements](#244-recognition-latency-requirements)
  - [2.4.5 Blind-Side and Shoulder Check Requirements](#245-blind-side-and-shoulder-check-requirements)
  - [2.4.6 Ball Perception Requirements](#246-ball-perception-requirements)
  - [2.4.7 Determinism Requirements](#247-determinism-requirements)
  - [2.4.8 Performance Requirements](#248-performance-requirements)
- [2.5 Attribute Influence Summary](#25-attribute-influence-summary)
- [2.6 Constants Declared in This Section](#26-constants-declared-in-this-section)
- [2.7 Version History](#27-version-history)

---

## 2.1 System Architecture Overview

### 2.1.1 Position in the Simulation Stack

The Perception System occupies the cognitive layer between raw simulation truth and agent
decision-making. It is the only system that translates omniscient world state into the
individual, bounded, fallible awareness that each agent actually acts upon.

```
┌─────────────────────────────────────────┐
│          WORLD STATE (OMNISCIENT)        │  ← Physics engine; all positions, all velocities
│   BallState · AgentState[22] · Match     │    No agent has direct read access at runtime
└────────────────────┬────────────────────┘
                     │ Read-only at heartbeat
                     ▼
┌─────────────────────────────────────────┐
│         PERCEPTION SYSTEM (#7)           │  ← THIS SPECIFICATION
│   FoV · Occlusion · L_rec · Shoulder    │    Transforms world truth into per-agent belief
│   check · Pressure degradation          │
└────────────────────┬────────────────────┘
                     │ PerceptionSnapshot (value struct, copied by value)
                     ▼
┌─────────────────────────────────────────┐
│         DECISION TREE (#8)               │  ← Sole consumer; not yet specified
│   Reads snapshot only; no world access   │    Defines its own intake contract
└─────────────────────────────────────────┘
```

No downstream system — the Decision Tree or any AI module — may query world state
directly. The `PerceptionSnapshot` is the sole source of world knowledge for agent
decision-making. This is the architectural guarantee that prevents omniscient agents.

### 2.1.2 Execution Cadence

The Perception System runs at **10Hz** — once per tactical heartbeat tick, every 100ms
of match time. This is not a performance compromise; it is the correct model for human
cognition. A football player does not re-evaluate their complete awareness of the pitch
60 times per second. They scan, hold a mental model, update it, and act on it at a
cadence driven by attention and opportunity — which the 10Hz heartbeat represents.

The physics simulation continues at 60Hz between heartbeats. Perception is not
re-evaluated at 60Hz. The snapshot from the most recent heartbeat is the valid input to
the Decision Tree until the next heartbeat produces a new one.

**Exception — forced mid-heartbeat refresh:** Specific physics-layer events (ball
contact, tackle completion, possession change) trigger an immediate out-of-schedule
perception refresh for the directly involved agents only. All other agents continue on
the 10Hz schedule. See §2.2.4 for the full enumeration.

### 2.1.3 The Epistemic Problem

The Perception System solves a specific, bounded problem: *what does agent A believe
the world looks like at heartbeat tick T?*

This problem has three components that the system must address simultaneously:

**Spatial filtering** — An agent cannot perceive what is behind them, beyond their
maximum range, or hidden behind other agents. The FoV model and occlusion model (§2.2,
Steps 2–3) handle this.

**Temporal filtering** — An agent cannot immediately identify a new stimulus. Cognitive
recognition takes time. A defender who glimpses a movement must register it before
reacting. The recognition latency model (§2.2, Step 4) handles this.

**Attentional filtering** — An agent under pressure cannot maintain broad awareness.
Stress narrows focus. The pressure scalar effect on FoV (KD-7, §2.4.2) handles this.

The Perception System is complete when all three filters are applied and a valid
`PerceptionSnapshot` is assembled. Section 3 provides the mathematical detail for
each filter. This section specifies what the filters must achieve (requirements) and
what data structures they must produce (structs).

---

## 2.2 Perception Pipeline

### 2.2.1 Pipeline Overview

The perception pipeline is a single linear pass executed once per heartbeat per agent.
No iteration, no feedback loops, no look-ahead. Input is world state; output is
`PerceptionSnapshot`. The pipeline is designed to be executed independently for all
22 agents per heartbeat within the 2ms budget (§2.4.8).

```
INPUT: WorldState (BallState, AgentState[22])
       + AgentState[observerId] (this agent's position, facing, attributes)

  ┌──────────────────────────────────────────────────────────────┐
  │  Step 1: QueryNearbyEntities()                               │
  │  Spatial hash radius query — all agents + ball within        │
  │  MAX_PERCEPTION_RANGE. Eliminates distant entities cheaply   │
  │  before any angular math is performed.                       │
  └──────────────────────────┬───────────────────────────────────┘
                             │ CandidateList (agents + ball)
                             ▼
  ┌──────────────────────────────────────────────────────────────┐
  │  Step 2: ApplyFieldOfView()                                  │
  │  Angular test: discard entities outside forward facing cone. │
  │  Cone angle is attribute-modified and pressure-degraded.     │
  │  Blind-side arc (200° rear) discards entities by default.    │
  └──────────────────────────┬───────────────────────────────────┘
                             │ FoV-filtered list
                             ▼
  ┌──────────────────────────────────────────────────────────────┐
  │  Step 3: ApplyOcclusionFilter()                              │
  │  Shadow cone test: for each candidate, check if any opponent │
  │  projects a shadow cone that blocks line-of-sight. Discard   │
  │  entities inside any shadow cone. Teammates do not generate  │
  │  shadow cones at Stage 0 (OQ-1).                            │
  └──────────────────────────┬───────────────────────────────────┘
                             │ Occlusion-filtered list
                             ▼
  ┌──────────────────────────────────────────────────────────────┐
  │  Step 4: ApplyRecognitionLatency()                           │
  │  For each remaining entity: check latency counter. If newly  │
  │  visible and counter < L_rec, entity is not yet confirmed.   │
  │  Increment counter. Only confirmed entities enter snapshot.  │
  │  Ball is exempt from L_rec — always immediately confirmed.   │
  └──────────────────────────┬───────────────────────────────────┘
                             │ Confirmed entity list
                             ▼
  ┌──────────────────────────────────────────────────────────────┐
  │  Step 5: ApplyBlindSideAwareness()                           │
  │  If shoulder check window is active: apply rear-arc entities │
  │  with their own L_rec cycle (not free; they must confirm).   │
  │  Check window open/close timing managed here.                │
  │  Next check scheduled if current tick reaches next interval. │
  └──────────────────────────┬───────────────────────────────────┘
                             │ Final visible entity list
                             ▼
  ┌──────────────────────────────────────────────────────────────┐
  │  Step 6: BuildPerceptionSnapshot()                           │
  │  Assemble output struct. Populate all fields. Stamp with     │
  │  FrameNumber. Copy by value to Decision Tree intake.         │
  └──────────────────────────┴───────────────────────────────────┘

OUTPUT: PerceptionSnapshot (value struct)
```

### 2.2.2 Pipeline Step Descriptions

**Step 1 — QueryNearbyEntities:**
Calls `spatialHash.QueryRadius(observerPosition, MAX_PERCEPTION_RANGE)` from the
Collision System (#3) spatial hash. Returns a candidate list of all agents and the ball
within the maximum possible perception range. This is a cheap coarse filter — it
eliminates entities on the far side of the pitch before any trigonometric operations
are performed. `MAX_PERCEPTION_RANGE = 120f` [GT] is effectively the full pitch diagonal;
at Stage 0 on a standard pitch, this filter will rarely eliminate any entity. Its value
is architectural: it preserves the spatial hash optimisation path and provides the hook
for Stage 2 weather/fog effects (OQ-5 resolution).

**Step 2 — ApplyFieldOfView:**
Performs an angular test for each candidate. The forward-facing cone is centred on
`AgentState.FacingDirection`. The effective cone angle is computed from the base angle
modified by the agent's `Decisions` attribute and degraded by the current `PressureScalar`
(detailed in §3.1). Any entity whose bearing from the observer exceeds half the effective
cone angle is discarded. The 200° blind-side arc — the rear complement of the forward cone
— discards all entities behind the agent by default. Step 5 overrides this for confirmed
shoulder check windows.

**Step 3 — ApplyOcclusionFilter:**
Tests each FoV-passing entity against shadow cones projected by all opponents currently
in the candidate list. An opponent at position P projects a shadow cone away from the
observer; any entity falling within that angular interval and beyond P is marked occluded
and removed. The ball is subject to the same occlusion test using identical geometry. Full
details of shadow cone geometry are in §3.2.

**Step 4 — ApplyRecognitionLatency:**
Maintains a per-observer, per-target latency counter in `_latencyCounters`. For each
entity passing occlusion: if this is the entity's first appearance in the visible set,
the counter is initialised to 0. Each heartbeat the entity remains visible, the counter
increments. The entity enters `VisibleTeammates` or `VisibleOpponents` only when the
counter has reached `L_rec` for this observer-target pair. If the entity leaves the
visible set at any point (leaves FoV, becomes occluded, or exits range), the counter
resets to 0; the full latency cycle restarts on re-appearance. The ball is exempt from
this mechanism and enters the snapshot immediately upon becoming visible.

**Step 5 — ApplyBlindSideAwareness:**
If `BlindSideWindowActive` is true for this agent (a shoulder check is in progress),
rear-arc entities are re-admitted to the pipeline from the discarded set of Step 2 and
processed through Steps 3 and 4 with their own latency counters. They are not
immediately confirmed — they still require `L_rec` ticks before entering the snapshot.
This step also manages the shoulder check scheduler: if the current heartbeat tick
reaches the agent's next scheduled check tick, a new window is opened
(`BlindSideWindowActive = true`, expiry set to current tick + `SHOULDER_CHECK_DURATION`).
Scheduling is deterministic with controlled jitter (§3.4).

**Step 6 — BuildPerceptionSnapshot:**
Assembles the output struct from all confirmed entities. Populates all fields per the
struct definition in §2.3.1. Stamps `FrameNumber` with the current heartbeat tick.
Updates `BallStalenessFrames` — if the ball was not confirmed visible this tick, the
counter increments from its previous value; if visible, it resets to 0. The completed
struct is passed by value; no reference to internal pipeline state is exposed.

### 2.2.3 Pipeline Invariants

The following invariants hold for every pipeline execution without exception. Violation
of any invariant is a bug, not an edge case to be handled gracefully:

| # | Invariant |
|---|-----------|
| INV-1 | The pipeline produces exactly one `PerceptionSnapshot` per agent per heartbeat tick |
| INV-2 | No entity in `VisibleTeammates` or `VisibleOpponents` was ever inside an active shadow cone at any step |
| INV-3 | No entity enters the snapshot before its latency counter has reached `L_rec` |
| INV-4 | The ball never appears in `VisibleTeammates` or `VisibleOpponents`; it has dedicated fields |
| INV-5 | An observer never appears in their own `VisibleTeammates` array |
| INV-6 | An entity with `RecognitionLatencyRemaining > 0` does not appear in the confirmed entity arrays |
| INV-7 | `EffectiveFoVAngle` is always ≥ `MIN_FOV_ANGLE` and ≤ `BASE_FOV_ANGLE + MAX_FOV_BONUS_ANGLE` |
| INV-8 | `BallStalenessFrames` is 0 if and only if `BallVisible` is true in the same snapshot |
| INV-9 | Identical inputs to every pipeline step produce identical outputs (determinism) |
| INV-10 | No heap allocation occurs during a standard heartbeat pipeline execution |

### 2.2.4 Forced Mid-Heartbeat Refresh

Certain physics-layer events warrant an immediate out-of-schedule perception refresh
for directly involved agents. These are edge-triggered (not polled) and generate a
`PerceptionRefreshEvent` that the Perception System subscribes to. The event system
stub is consistent with the pattern established in Ball Physics and Pass Mechanics.

Qualifying trigger events:

| Event | Agents Refreshed | Rationale |
|-------|-----------------|-----------|
| Ball contact (pass, shot, header) | Ball sender and receiver | Immediate awareness of possession change |
| Tackle event completion | Tackler and tackled agent | Critical state change; stale snapshot would cause decision error |
| Possession loss | Agent who lost possession | Agent must immediately know possession has changed |

**Non-qualifying events** (do not trigger refresh):
- Agent movement and acceleration changes (handled at 60Hz physics; perception catches up at next heartbeat)
- Referee whistle / set piece initiation (deferred; no referee system at Stage 0)
- Distant events involving no directly involved agents

The forced refresh executes the full pipeline for only the affected agents. All other
agents continue their 10Hz schedule unaffected. A forced refresh resets the affected
agents' `FrameNumber` to the current physics tick, not the current heartbeat tick — the
Decision Tree must handle receiving a snapshot outside the normal 10Hz cadence.

---

## 2.3 Data Structures — Authoritative Definitions

The structs defined in this section are the implementation authority for all downstream
specifications that consume or reference `PerceptionSnapshot`. Changes to these
definitions require an amendment to this section and a corresponding version increment.
No field may be added, renamed, or removed by an implementing specification.

### 2.3.1 `PerceptionSnapshot` Struct

This is the complete output of the Perception System for one agent at one heartbeat.
It is a value struct (KD-2). It is passed by value to the Decision Tree. No reference
to internal Perception System state is exposed through this struct.

**Design note — agent result arrays:** Embedding variable-count entity arrays directly
in a C# value struct presents two problems: (1) C# `fixed` arrays in structs require
`unsafe` context and only support primitive types — `PerceivedAgent` is not primitive;
(2) allocating per-heartbeat arrays violates the zero-allocation policy (INV-10). The
resolution, consistent with the Collision System (#3) query result buffer pattern, is to
maintain **pre-allocated, per-agent result arrays** inside `PerceptionSystem` at match
initialisation. `PerceptionSnapshot` holds integer counts and read-only span accessors
into those buffers. The underlying arrays are never reallocated at runtime. The Decision
Tree reads results via the spans, which are valid for the duration of the heartbeat tick.
The buffers are overwritten at the next heartbeat — the Decision Tree must not cache
the spans across ticks.

```csharp
/// <summary>
/// The complete perception state for one agent at one heartbeat.
/// Produced by PerceptionSystem.ComputeSnapshot(observerId, worldState).
/// Passed by value to the Decision Tree — scalar fields are copied by value.
///
/// AGENT RESULT ARRAYS: VisibleTeammates and VisibleOpponents are ReadOnlySpan<PerceivedAgent>
/// views into pre-allocated PerceptionSystem buffers. They are valid for the current
/// heartbeat tick only. Decision Tree must not cache or store these spans across ticks.
/// See §2.3.3 for buffer ownership and lifetime details.
///
/// OWNERSHIP: Perception System Specification #7 owns this struct definition.
/// No downstream specification may alter this struct without an amendment
/// to Perception_System_Spec_Section_2.
///
/// STAGE 0 NOTE: PerceivedPosition and PerceivedVelocity are exact (no error model).
/// ConfidenceScore is binary [0 or 1]. Both are marked for Stage 1+ upgrade.
/// </summary>
struct PerceptionSnapshot
{
    // ─── Identity & Frame Stamp ──────────────────────────────────────────────

    /// <summary>
    /// AgentId of the agent this snapshot represents the perception of.
    /// Range: 0–21 (matches AgentState.AgentID from Agent Movement #2).
    /// </summary>
    int ObserverId;

    /// <summary>
    /// The heartbeat tick this snapshot was computed for.
    /// Matches the 10Hz tactical heartbeat counter (not the 60Hz physics frame counter).
    /// For forced mid-heartbeat refreshes, this field holds the physics frame number
    /// at the time of refresh. Decision Tree must tolerate non-standard frame stamps.
    /// </summary>
    int FrameNumber;

    // ─── Ball Perception ─────────────────────────────────────────────────────

    /// <summary>
    /// Whether the ball is currently directly visible to this agent.
    /// True = ball is within FoV, not occluded, within MAX_PERCEPTION_RANGE.
    /// False = ball is outside FoV, occluded by opponent, or out of range.
    /// See §2.2, Steps 2–3 for visibility determination.
    /// Ball is never subject to L_rec — it is immediately confirmed when visible (OQ-2).
    /// </summary>
    bool BallVisible;

    /// <summary>
    /// The agent's current perceived ball position.
    /// If BallVisible is true: exact match to BallState.Position (Stage 0; no error model).
    /// If BallVisible is false: last confirmed ball position before it left visibility.
    ///   This is a STALE value. Decision Tree must use BallStalenessFrames to weight
    ///   confidence in this field. Do not treat a stale position as current truth.
    /// Units: metres. Coordinate system: matches AgentState.Position (pitch-centred).
    /// </summary>
    Vector2 BallPerceivedPosition;

    /// <summary>
    /// Number of heartbeat ticks since the ball was last directly visible to this agent.
    /// 0 = ball is currently visible (BallVisible is true — these are always consistent).
    /// >0 = ball has not been visible for this many ticks; BallPerceivedPosition is stale.
    /// Decision Tree should treat ball position confidence as inversely proportional to
    /// this value. No maximum cap — the ball may be unseen for many ticks in edge cases.
    /// INVARIANT: BallStalenessFrames == 0 iff BallVisible == true (INV-8).
    /// </summary>
    int BallStalenessFrames;

    // ─── Agent Perception ────────────────────────────────────────────────────

    /// <summary>
    /// Read-only view of confirmed visible teammates at this heartbeat (excluding observer).
    /// An agent is confirmed after their recognition latency counter has elapsed (§3.3).
    ///
    /// LIFETIME: Valid for the current heartbeat tick only. The underlying buffer is
    /// owned by PerceptionSystem and overwritten at the next heartbeat. Decision Tree
    /// must not store or cache this span across ticks — copy required data immediately.
    ///
    /// CAPACITY: Backed by a pre-allocated PerceivedAgent[10] buffer per observer.
    ///   Allocated once at match initialisation; never reallocated at runtime (INV-10).
    ///   Slice length = VisibleTeammateCount (0–10); unused capacity is not exposed.
    /// </summary>
    ReadOnlySpan<PerceivedAgent> VisibleTeammates;

    /// <summary>
    /// Number of valid entries in VisibleTeammates. Range: 0–10.
    /// Reflects the slice length of the VisibleTeammates span.
    /// Provided as a convenience scalar field; use either this or VisibleTeammates.Length.
    /// </summary>
    int VisibleTeammateCount;

    /// <summary>
    /// Read-only view of confirmed visible opponents at this heartbeat.
    /// Occlusion is applied — opponent shadow cones may hide other opponents.
    ///
    /// LIFETIME: Valid for the current heartbeat tick only. Same constraints as
    /// VisibleTeammates — do not cache across ticks.
    ///
    /// CAPACITY: Backed by a pre-allocated PerceivedAgent[11] buffer per observer.
    ///   Allocated once at match initialisation; never reallocated at runtime (INV-10).
    ///   Slice length = VisibleOpponentCount (0–11); unused capacity is not exposed.
    /// </summary>
    ReadOnlySpan<PerceivedAgent> VisibleOpponents;

    /// <summary>
    /// Number of valid entries in VisibleOpponents. Range: 0–11.
    /// Reflects the slice length of the VisibleOpponents span.
    /// </summary>
    int VisibleOpponentCount;

    // ─── Perception Quality Metadata ─────────────────────────────────────────

    /// <summary>
    /// The effective field of view angle used for this snapshot, in degrees.
    /// = BASE_FOV_ANGLE + Decisions modifier − PressureScalar reduction.
    /// Clamped to [MIN_FOV_ANGLE, BASE_FOV_ANGLE + MAX_FOV_BONUS_ANGLE].
    /// Exposed for Decision Tree use: a narrow EffectiveFoVAngle indicates the agent is
    /// under significant pressure. Decision Tree may use this to qualify option confidence.
    /// Units: degrees. Value is always positive.
    /// </summary>
    float EffectiveFoVAngle;

    // ─── Blind-Side / Shoulder Check State ──────────────────────────────────

    /// <summary>
    /// Whether a shoulder check awareness window is currently active for this agent.
    /// True = rear-arc entities are being processed through their own L_rec cycle;
    ///   rear-arc entities that have confirmed will appear in VisibleTeammates/Opponents.
    /// False = standard blind-side exclusion is active; no rear-arc entities are perceived.
    /// Managed autonomously by Perception System (OQ-3). Decision Tree does not set this.
    /// </summary>
    bool BlindSideWindowActive;

    /// <summary>
    /// Match time (seconds) at which the current shoulder check window expires.
    /// Valid only when BlindSideWindowActive is true.
    /// When match time exceeds this value, BlindSideWindowActive becomes false
    /// and rear-arc processing stops at the next heartbeat.
    /// Units: match time in seconds (same reference as AgentState timestamps).
    /// Undefined (0f) when BlindSideWindowActive is false.
    /// </summary>
    float BlindSideWindowExpiry;
}
```

**`ReadOnlySpan` compatibility note:** `ReadOnlySpan<T>` is a ref struct and cannot be
stored on the heap or in a class field. This enforces correct usage — the Decision Tree
cannot inadvertently cache a snapshot containing a dangling span. If the Decision Tree
architecture (Spec #8) requires storing snapshot data across ticks for any agent, it must
copy the required `PerceivedAgent` values into its own managed storage. This is by design.

### 2.3.2 `PerceivedAgent` Struct

Each entry in `VisibleTeammates` and `VisibleOpponents` is a `PerceivedAgent`. This
struct contains everything the observing agent knows about a perceived entity. At Stage 0,
position and velocity are exact copies of world state. Stage 1 will introduce error models.

```csharp
/// <summary>
/// The observing agent's perception of a single other agent at one heartbeat.
/// Contains what the observer knows — not ground truth — about this agent.
///
/// STAGE 0: PerceivedPosition and PerceivedVelocity are identical to world state.
/// ConfidenceScore is binary: 1.0f = confirmed, 0.0f = pending (pending never appears
/// in confirmed arrays — this field is reserved for Stage 1 continuous confidence model).
///
/// Stage 1 upgrade paths:
/// - PerceivedPosition: introduce small error scaled by Decisions attribute
/// - PerceivedVelocity: introduce estimation error for rapidly changing velocity
/// - ConfidenceScore: continuous [0,1] decay with distance and occlusion proximity
/// </summary>
struct PerceivedAgent
{
    // ─── Identity ────────────────────────────────────────────────────────────

    /// <summary>
    /// AgentId of the perceived agent.
    /// Matches AgentState.AgentID from Agent Movement Specification #2.
    /// Range: 0–21. Never equals the ObserverId from the containing snapshot.
    /// </summary>
    int AgentId;

    // ─── Perceived State ─────────────────────────────────────────────────────

    /// <summary>
    /// The observer's perceived position of this agent, in metres.
    /// Stage 0: exact match to AgentState.Position (2D projection, Y-forward convention).
    /// Stage 1+: will differ from true position by an attribute-scaled error term.
    /// Decision Tree should treat this as the observer's belief, not verified ground truth.
    /// </summary>
    Vector2 PerceivedPosition;

    /// <summary>
    /// The observer's perceived velocity of this agent, in m/s.
    /// Stage 0: exact match to AgentState.Velocity (2D projection).
    /// Stage 1+: will incorporate estimation error for rapidly changing velocity.
    /// Used by Decision Tree for option evaluation (passing lane prediction, pressing target).
    /// </summary>
    Vector2 PerceivedVelocity;

    // ─── Confidence & Latency Metadata ───────────────────────────────────────

    /// <summary>
    /// Confidence in the perceived data for this agent. Range: [0.0, 1.0].
    /// Stage 0: always 1.0f for any entry in the confirmed arrays (binary model).
    ///   Entries with ConfidenceScore < 1.0 never appear in the snapshot at Stage 0.
    /// Stage 1+: continuous decay model; Decision Tree will receive entries with
    ///   intermediate scores representing degrading confidence in stale data.
    /// Reserved at Stage 0 for forward compatibility — do not remove this field.
    /// </summary>
    float ConfidenceScore;

    /// <summary>
    /// Heartbeat ticks remaining until this agent would be fully confirmed.
    /// Stage 0: always 0 for entries in confirmed arrays (they are already confirmed).
    ///   Non-zero values are internal pipeline state; they never appear in the output struct.
    /// Stage 1+: may expose partially-confirmed agents with reduced ConfidenceScore.
    /// Reserved at Stage 0. Decision Tree must not depend on this being non-zero.
    /// </summary>
    int RecognitionLatencyRemaining;
}
```

**Debug occlusion data:** The `WasOccludedThisTick` flag considered during drafting has
been deliberately excluded from `PerceivedAgent`. Debug information must not inflate
shipped value structs. Occlusion diagnostic data is instead written to a parallel
`PerceivedAgentDebug` struct populated only in `#if UNITY_EDITOR` builds:

```csharp
// EDITOR-ONLY — never compiled into release builds.
// Populated by PerceptionSystem in parallel with the main pipeline when UNITY_EDITOR is defined.
#if UNITY_EDITOR
struct PerceivedAgentDebug
{
    int   AgentId;
    bool  WasOccludedThisTick;    // True if discarded by shadow cone test at Step 3
    int   LatencyCounterAtTick;   // Raw counter value when snapshot was built
    float ShadowConeAngle;        // Angle of occluding cone if WasOccludedThisTick = true
}
#endif
```

This keeps the shipped struct minimal and the debug path non-intrusive.

### 2.3.3 `PerceptionSystem` Internal State

The `PerceptionSystem` class maintains persistent state across heartbeats. This state
is not exposed through the `PerceptionSnapshot` and is not accessible to any downstream
system. It is documented here to define the full memory footprint and clarify which
data the pipeline reads as ephemeral input versus retains across ticks.

```csharp
// Internal to PerceptionSystem — not exposed externally.
// Detailed implementation in Section 3 and Section 4.

// Recognition latency counters.
// Key: (observerId, targetId). Value: ticks this target has been visible but unconfirmed.
// Capacity: 22 × 21 = 462 entries maximum. Fixed capacity; no runtime growth.
// Reset behaviour: counter set to 0 when target leaves visibility set.
Dictionary<(int, int), int> _latencyCounters;

// Per-agent shoulder check schedule.
// Key: observerId. Value: heartbeat tick at which next check is triggered.
// Capacity: 22 entries (one per agent).
// Updated in Step 5 when a window expires or match starts.
Dictionary<int, int> _nextCheckTick;

// Per-agent shoulder check window expiry.
// Key: observerId. Value: match time (seconds) at which the current window closes.
// Set to 0f when no window is active. Checked in Step 5.
Dictionary<int, float> _checkWindowExpiry;
```

Memory footprint: approximately 3.7KB per match instance (462 int pairs + 44 float/int
values). This is a fixed allocation made at match initialisation; no per-heartbeat heap
allocation occurs (INV-10).

### 2.3.4 Struct Ownership and Amendment Authority

`PerceptionSnapshot` and `PerceivedAgent` are owned exclusively by Perception System
Specification #7. The following rules govern amendment:

- **Decision Tree Specification #8** is the primary consumer. If the Decision Tree
  requires additional fields, it must raise an amendment request against this specification.
  The Decision Tree may not add fields unilaterally.
- **No interface struct** (`IPerceptionConsumer`, etc.) is defined in this specification.
  The Decision Tree defines its own intake pattern. This follows the project interface
  principle: write the interface only when both sides are fully specified.
- **Goalkeeper Mechanics Specification #11** (not yet written) may require extended
  perception fields. If so, an amendment or a `GoalkeeperPerceptionSnapshot` subtype
  (extending this struct) should be requested before that specification's Section 3 is
  drafted. At Stage 0, the goalkeeper uses the standard struct without extension.
- Any amendment to these structs requires a version increment to this section and a
  cross-specification impact assessment before ratification.

---

