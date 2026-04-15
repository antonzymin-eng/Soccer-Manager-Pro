# Perception System Specification #7 — Section 2: System Overview & Functional Requirements

**File:** `Perception_System_Spec_Section_2_v1_2.md`
**Purpose:** Defines the complete perception pipeline architecture, the authoritative
`FilteredView`, `PerceptionDiagnostics`, and `PerceivedAgent` struct definitions, all
functional requirements governing system behaviour, and the data model that Section 3
must implement. The core architectural principle is **separation of filter output from
filter metadata**: `FilteredView` contains only what the Decision Tree needs to know
about the world; `PerceptionDiagnostics` contains how the filter operated, available
for debug tooling and future systems. This section is the bridge between the scope
boundary established in Section 1 and the mathematical models specified in Section 3.
All requirements here are testable and binding.

**Created:** February 24, 2026, 3:00 PM PST
**Version:** 1.2
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
  - [2.3.1 `FilteredView` Struct](#231-filteredview-struct)
  - [2.3.2 `PerceptionDiagnostics` Struct](#232-perceptiondiagnostics-struct)
  - [2.3.3 `PerceivedAgent` Struct](#233-perceivedagent-struct)
  - [2.3.4 `PerceptionSystem` Internal State](#234-perceptionsystem-internal-state)
  - [2.3.5 Struct Ownership and Amendment Authority](#235-struct-ownership-and-amendment-authority)
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
└──────────┬──────────────────┬───────────┘
           │                  │
           │ FilteredView     │ PerceptionDiagnostics
           │ (value struct)   │ (filter metadata — debug/telemetry only)
           ▼                  ▼
┌─────────────────────┐  ┌───────────────────────────┐
│  DECISION TREE (#8) │  │ Debug Tooling / Telemetry │
│  Sole consumer of   │  │ Animation System (Stage1+)│
│  FilteredView; no   │  │ Not delivered to DT       │
│  world state access │  │                           │
└─────────────────────┘  └───────────────────────────┘
```

No downstream system — the Decision Tree or any AI module — may query world state
directly. The `FilteredView` is the sole source of world knowledge for agent
decision-making. This is the architectural guarantee that prevents omniscient agents.
`PerceptionDiagnostics` is a separate struct containing filter metadata (effective FoV
angle, pressure scalar, shoulder check animation stubs) — it is NOT delivered to the
Decision Tree and is available only for debug tooling, telemetry, and future systems
(e.g., Animation System at Stage 1+).

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
`FilteredView` is assembled. Section 3 provides the mathematical detail for
each filter. This section specifies what the filters must achieve (requirements) and
what data structures they must produce (structs).

---

## 2.2 Perception Pipeline

### 2.2.1 Pipeline Overview

The perception pipeline is a single linear pass executed once per heartbeat per agent.
No iteration, no feedback loops, no look-ahead. Input is world state; output is
`FilteredView` (and its companion `PerceptionDiagnostics`). The pipeline is designed to be executed independently for all
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
  │  Step 6: BuildFilteredView() + BuildDiagnostics()            │
  │  Assemble FilteredView (consumer output) and                 │
  │  PerceptionDiagnostics (filter metadata). Stamp with         │
  │  FrameNumber. FilteredView copied by value to Decision Tree. │
  └──────────────────────────┴───────────────────────────────────┘

OUTPUT: FilteredView (value struct — delivered to Decision Tree)
      + PerceptionDiagnostics (value struct — debug/telemetry only)
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

**Step 6 — BuildFilteredView + BuildDiagnostics:**
Assembles two output structs from all confirmed entities and filter state:
- **`FilteredView`** — the consumer output. Contains only what the Decision Tree needs:
  identity, ball state, visible agents, blind-side agents, and forced-refresh flag.
  Populated per the struct definition in §2.3.1.
- **`PerceptionDiagnostics`** — filter metadata. Contains how the filter operated:
  effective FoV angle, pressure scalar, shoulder check window state, animation stub.
  Populated per the struct definition in §2.3.2. NOT delivered to the Decision Tree.

Stamps `FrameNumber` with the current heartbeat tick on both structs.
Updates `BallStalenessFrames` — if the ball was not confirmed visible this tick, the
counter increments from its previous value; if visible, it resets to 0. The completed
`FilteredView` is passed by value to the Decision Tree; no reference to internal pipeline
state is exposed.

### 2.2.3 Pipeline Invariants

The following invariants hold for every pipeline execution without exception. Violation
of any invariant is a bug, not an edge case to be handled gracefully:

| # | Invariant |
|---|-----------|
| INV-1 | The pipeline produces exactly one `FilteredView` and one `PerceptionDiagnostics` per agent per heartbeat tick |
| INV-2 | No entity in `VisibleTeammates` or `VisibleOpponents` was ever inside an active shadow cone at any step |
| INV-3 | No entity enters the snapshot before its latency counter has reached `L_rec` |
| INV-4 | The ball never appears in `VisibleTeammates` or `VisibleOpponents`; it has dedicated fields |
| INV-5 | An observer never appears in their own `VisibleTeammates` array |
| INV-6 | An entity with `RecognitionLatencyRemaining > 0` does not appear in the confirmed entity arrays |
| INV-7 | `EffectiveFoVAngle` is always ≥ `MIN_FOV_ANGLE` and ≤ `BASE_FOV_ANGLE + MAX_FOV_BONUS_ANGLE` |
| INV-8 | `BallStalenessFrames` is 0 if and only if `BallVisible` is true in the same `FilteredView` |
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
specifications that consume or reference perception output. Changes to these definitions
require an amendment to this section and a corresponding version increment. No field may
be added, renamed, or removed by an implementing specification.

**Architectural principle — separation of filter output from filter metadata:**

The Perception System produces two distinct outputs each heartbeat:

1. **`FilteredView`** — the filtered world state. Contains *only* what the Decision Tree
   needs to make decisions: who the agent can see, where the ball is, and what the agent
   learned from shoulder checks. This is a pure data struct. It contains no information
   about how the filter operated — no FoV angles, no pressure scalars, no animation stubs.
   The Decision Tree receives `FilteredView` and nothing else.

2. **`PerceptionDiagnostics`** — filter metadata. Contains how the filter operated: the
   effective FoV angle used, the pressure scalar at the time of filtering, shoulder check
   window state, and the animation stub for Stage 1+. This struct is **NOT** delivered to
   the Decision Tree. It is available for debug tooling, telemetry, and future systems
   (Animation System at Stage 1+).

This separation enforces a clean boundary: the Decision Tree acts on *what the agent
knows about the world*, not on how the perception filter works internally. Adding new
consumers (Goalkeeper AI, Event System, Animation) does not require them to understand
filter internals — they receive `FilteredView` and get pure data. Changing filter
internals (e.g., reworking occlusion, adding weather effects) does not change the
`FilteredView` shape — only what passes through the filter.

### 2.3.1 `FilteredView` Struct

This is the consumer-facing output of the Perception System for one agent at one
heartbeat. It is a value struct (KD-2). It is passed by value to the Decision Tree.
No reference to internal Perception System state is exposed through this struct.

**Design note — agent result arrays:** The `PerceptionSystem` maintains **pre-allocated,
per-agent result arrays** at match initialisation, consistent with the Collision System
(#3) query result buffer pattern. `FilteredView` holds array references into those
buffers. The underlying arrays are never reallocated at runtime. The Decision Tree reads
results via the arrays, which are valid for the duration of the heartbeat tick. The
buffers are overwritten at the next heartbeat — the Decision Tree must not cache array
references across ticks.

```csharp
/// <summary>
/// FilteredView — the agent's filtered knowledge of the world at one heartbeat.
///
/// Produced by PerceptionSystem.ComputeFilteredView(observerId, worldState).
/// Passed by value to the Decision Tree. This struct contains ONLY what the
/// Decision Tree needs to make decisions — pure world-state data after filtering.
/// It does NOT contain filter metadata (FoV angles, pressure scalars, animation
/// stubs). Those live in PerceptionDiagnostics, which is not delivered to the DT.
///
/// OWNERSHIP: Perception System Specification #7 owns this struct definition.
/// No downstream specification may alter this struct without an amendment
/// to Perception_System_Spec_Section_2.
///
/// STAGE 0 NOTE: PerceivedPosition and PerceivedVelocity in PerceivedAgent entries
/// are exact (no error model). ConfidenceScore is binary [0 or 1]. Both are marked
/// for Stage 1+ upgrade.
/// </summary>
struct FilteredView
{
    // ─── Identity & Timing ──────────────────────────────────────────────────

    /// <summary>
    /// AgentId of the agent this view represents the perception of.
    /// Range: 0–21 (matches AgentState.AgentID from Agent Movement #2).
    /// </summary>
    int ObserverId;

    /// <summary>
    /// The heartbeat tick this view was computed for.
    /// Matches the 10Hz tactical heartbeat counter (not the 60Hz physics frame counter).
    /// For forced mid-heartbeat refreshes, this field holds the physics frame number
    /// at the time of refresh. Decision Tree must tolerate non-standard frame stamps.
    /// </summary>
    int FrameNumber;

    /// <summary>
    /// True if this view was produced by a forced mid-heartbeat refresh (§3.8).
    /// False for all standard 10Hz heartbeat views.
    /// Decision Tree must handle receiving a view at any simulation frame, not only
    /// on heartbeat ticks, when this flag is true.
    /// </summary>
    bool ForcedRefreshThisTick;

    // ─── Ball State ─────────────────────────────────────────────────────────

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

    // ─── Visible Agents (Forward Arc) ───────────────────────────────────────

    /// <summary>
    /// Confirmed visible teammates at this heartbeat (excluding observer).
    /// An agent is confirmed after their recognition latency counter has elapsed (§3.3).
    ///
    /// LIFETIME: Valid for the current heartbeat tick only. The underlying buffer is
    /// owned by PerceptionSystem and overwritten at the next heartbeat. Decision Tree
    /// must not store or cache this array across ticks — copy required data immediately.
    ///
    /// CAPACITY: Backed by a pre-allocated PerceivedAgent[10] buffer per observer.
    ///   Allocated once at match initialisation; never reallocated at runtime (INV-10).
    /// </summary>
    PerceivedAgent[] VisibleTeammates;

    /// <summary>
    /// Confirmed visible opponents at this heartbeat.
    /// Occlusion is applied — opponent shadow cones may hide other opponents.
    ///
    /// LIFETIME: Same constraints as VisibleTeammates — do not cache across ticks.
    ///
    /// CAPACITY: Backed by a pre-allocated PerceivedAgent[11] buffer per observer.
    ///   Allocated once at match initialisation; never reallocated at runtime (INV-10).
    /// </summary>
    PerceivedAgent[] VisibleOpponents;

    // ─── Blind-Side Agents (Shoulder Check) ─────────────────────────────────

    /// <summary>
    /// Agents confirmed during an active shoulder check window (§3.4.3).
    /// Empty when no shoulder check window is active or when window is active but
    /// no agents have been confirmed yet (L_rec not yet elapsed).
    /// Subject to same L_rec requirements as forward-arc agents.
    ///
    /// LIFETIME: Same constraints as VisibleTeammates — do not cache across ticks.
    ///
    /// CAPACITY: Backed by a pre-allocated PerceivedAgent[21] buffer per observer.
    /// </summary>
    PerceivedAgent[] BlindSidePerceivedAgents;
}
```

**Field count: 9.** This is deliberately minimal. The Decision Tree receives pure data
about what the agent knows — not how the filter works. Fields that describe filter
internals (EffectiveFoVAngle, PressureScalar, BlindSideWindowActive, BlindSideWindowExpiry,
ShoulderCheckAnimData) live in `PerceptionDiagnostics` (§2.3.2).

### 2.3.2 `PerceptionDiagnostics` Struct

Filter metadata produced alongside `FilteredView` each heartbeat. This struct is NOT
delivered to the Decision Tree. It exists for debug tooling, telemetry, balance analysis,
and future systems (Animation System at Stage 1+).

```csharp
/// <summary>
/// PerceptionDiagnostics — filter metadata for one agent at one heartbeat.
///
/// Contains information about how the perception filter operated, NOT what the
/// agent knows about the world. This struct is never delivered to the Decision Tree.
///
/// CONSUMERS: Debug tooling, telemetry, balance analysis tools, Animation System (Stage 1+).
/// OWNERSHIP: Perception System Specification #7. Same amendment authority as FilteredView.
/// </summary>
struct PerceptionDiagnostics
{
    // ─── Correlation Keys ───────────────────────────────────────────────────

    /// <summary>
    /// AgentId — matches FilteredView.ObserverId for the same heartbeat.
    /// Used to correlate diagnostics with the corresponding FilteredView.
    /// </summary>
    int ObserverId;

    /// <summary>
    /// Heartbeat tick — matches FilteredView.FrameNumber for the same heartbeat.
    /// </summary>
    int FrameNumber;

    // ─── Filter State ───────────────────────────────────────────────────────

    /// <summary>
    /// The effective field of view angle used for this heartbeat, in degrees.
    /// = BASE_FOV_ANGLE + Decisions modifier − PressureScalar reduction.
    /// Clamped to [MIN_FOV_ANGLE, BASE_FOV_ANGLE + MAX_FOV_BONUS_ANGLE].
    /// Useful for debug visualisation of perception cones and balance analysis.
    /// Units: degrees. Value is always positive.
    /// </summary>
    float EffectiveFoVAngle;

    /// <summary>
    /// Pressure scalar at the time of filter execution.
    /// Range: [0.0, 1.0]. Computed from opponent proximity (§3.6).
    /// Useful for balance telemetry and verifying pressure model behaviour.
    /// </summary>
    float PressureScalar;

    // ─── Shoulder Check State ───────────────────────────────────────────────

    /// <summary>
    /// Whether a shoulder check awareness window was active during this heartbeat.
    /// True = rear-arc entities were processed through Steps 3 and 4.
    /// False = standard blind-side exclusion was active.
    /// </summary>
    bool BlindSideWindowActive;

    /// <summary>
    /// Match time (seconds) at which the current shoulder check window expires.
    /// Valid only when BlindSideWindowActive is true.
    /// Units: match time in seconds (same reference as AgentState timestamps).
    /// Undefined (0f) when BlindSideWindowActive is false.
    /// </summary>
    float BlindSideWindowExpiry;

    // ─── Animation Stub ─────────────────────────────────────────────────────

    /// <summary>
    /// Stage 1+ stub for Animation System. Populated at Stage 0; no consumer until
    /// Animation System is written. See §3.4.4 for struct definition.
    /// </summary>
    ShoulderCheckAnimData ShoulderCheckAnim;
}
```

**Field count: 7.** These fields describe how the perception filter operated, not what the
agent knows about the world. None are consumed by the Decision Tree at any stage.

### 2.3.3 `PerceivedAgent` Struct

Each entry in `VisibleTeammates`, `VisibleOpponents`, and `BlindSidePerceivedAgents` is
a `PerceivedAgent`. This struct contains everything the observing agent knows about a
perceived entity. At Stage 0, position and velocity are exact copies of world state.
Stage 1 will introduce error models.

```csharp
/// <summary>
/// The observing agent's perception of a single other agent at one heartbeat.
/// Contains what the observer knows — not ground truth — about this agent.
///
/// This struct contains ONLY consumer-relevant data. Diagnostic fields
/// (latency counters, occlusion state) live in PerceivedAgentDebug (editor-only).
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
    /// <summary>
    /// AgentId of the perceived agent.
    /// Matches AgentState.AgentID from Agent Movement Specification #2.
    /// Range: 0–21. Never equals the ObserverId from the containing FilteredView.
    /// </summary>
    int AgentId;

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

    /// <summary>
    /// Confidence in the perceived data for this agent. Range: [0.0, 1.0].
    /// Stage 0: always 1.0f for any entry in the confirmed arrays (binary model).
    ///   Entries with ConfidenceScore < 1.0 never appear in the FilteredView at Stage 0.
    /// Stage 1+: continuous decay model; Decision Tree will receive entries with
    ///   intermediate scores representing degrading confidence in stale data.
    /// Reserved at Stage 0 for forward compatibility — do not remove this field.
    /// </summary>
    float ConfidenceScore;
}
```

**Field count: 4.** The `RecognitionLatencyRemaining` field from v1.1 has been removed —
it was always 0 in confirmed arrays at Stage 0 and is internal pipeline state. Occlusion
debug data and latency diagnostics live in the editor-only `PerceivedAgentDebug` struct:

```csharp
// EDITOR-ONLY — never compiled into release builds.
// Populated by PerceptionSystem in parallel with the main pipeline when UNITY_EDITOR is defined.
#if UNITY_EDITOR
struct PerceivedAgentDebug
{
    int   AgentId;
    bool  WasOccludedThisTick;        // True if discarded by shadow cone test at Step 3
    int   LatencyCounterAtTick;       // Raw counter value when view was built
    int   RecognitionLatencyRemaining; // Ticks remaining until confirmation (0 if confirmed)
    float ShadowConeAngle;            // Angle of occluding cone if WasOccludedThisTick = true
    bool  IsInBlindSide;              // True if confirmed via shoulder check, not forward arc
}
#endif
```

This keeps the shipped struct minimal (4 fields per perceived agent) and the debug path
non-intrusive. Diagnostic fields that were previously in the shipped struct
(`RecognitionLatencyRemaining`, `IsInBlindSide`) are now editor-only.

### 2.3.4 `PerceptionSystem` Internal State

The `PerceptionSystem` class maintains persistent state across heartbeats. This state
is not exposed through `FilteredView` or `PerceptionDiagnostics` and is not accessible to any downstream
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

### 2.3.5 Struct Ownership and Amendment Authority

`FilteredView`, `PerceptionDiagnostics`, and `PerceivedAgent` are owned exclusively by
Perception System Specification #7. The following rules govern amendment:

- **Decision Tree Specification #8** is the primary consumer of `FilteredView`. If the
  Decision Tree requires additional fields in `FilteredView`, it must raise an amendment
  request against this specification. The Decision Tree may not add fields unilaterally.
  The Decision Tree does NOT receive `PerceptionDiagnostics` under any circumstance.
- **No interface struct** (`IPerceptionConsumer`, etc.) is defined in this specification.
  The Decision Tree defines its own intake pattern. This follows the project interface
  principle: write the interface only when both sides are fully specified.
- **Goalkeeper Mechanics Specification #11** (not yet written) may require extended
  perception fields. If so, an amendment or a `GoalkeeperFilteredView` subtype
  (extending this struct) should be requested before that specification's Section 3 is
  drafted. At Stage 0, the goalkeeper uses the standard struct without extension.
- **Diagnostic consumers** (Animation System, debug tooling, telemetry) consume
  `PerceptionDiagnostics`. Adding fields to diagnostics is lower-impact than adding
  fields to `FilteredView` because no decision-making system depends on diagnostics.
- Any amendment to `FilteredView` or `PerceivedAgent` requires a version increment to
  this section and a cross-specification impact assessment before ratification.
  Amendments to `PerceptionDiagnostics` require a version increment but no cross-spec
  impact assessment (no decision-making consumer is affected).

---

## 2.4 Functional Requirements

Requirements are stated using SHALL (mandatory), SHOULD (recommended), and MAY
(permitted). All SHALL requirements are testable and define the complete behavioural
contract for Section 3 implementations. Requirements are grouped by pipeline step
for traceability.

### 2.4.1 Core Pipeline Requirements

| ID | Requirement | Priority | Tests |
|----|-------------|----------|-------|
| FR-1 | Each agent SHALL produce exactly one `FilteredView` and one `PerceptionDiagnostics` per 10Hz heartbeat tick under normal operation | Mandatory | UT-CORE-001 |
| FR-2 | The pipeline SHALL execute Steps 1–6 in order; no step may be skipped or reordered | Mandatory | UT-CORE-002 |
| FR-3 | A forced mid-heartbeat refresh SHALL execute the full pipeline for affected agents only; unaffected agents SHALL NOT be refreshed | Mandatory | UT-CORE-003 |
| FR-4 | The pipeline SHALL produce valid output when all 22 agents are processed in a single heartbeat | Mandatory | IT-CORE-001 |
| FR-5 | The `FilteredView` and `PerceptionDiagnostics` outputs SHALL be value structs; no heap allocation SHALL occur during standard pipeline execution | Mandatory | UT-CORE-004 |
| FR-6 | Agent attributes and state SHALL be read once, at the start of the heartbeat pipeline, and cached for its duration | Mandatory | UT-CORE-005 |

### 2.4.2 Field of View Requirements

| ID | Requirement | Priority | Tests |
|----|-------------|----------|-------|
| FR-7 | Agents beyond `MAX_PERCEPTION_RANGE` SHALL be excluded from the candidate list before any FoV test is performed | Mandatory | UT-FOV-001 |
| FR-8 | An entity whose bearing from the observer exceeds `EffectiveFoVAngle / 2` SHALL be excluded from the FoV-passing set | Mandatory | UT-FOV-002 |
| FR-9 | The effective FoV angle SHALL be computed from `BASE_FOV_ANGLE` modified by the agent's `Decisions` attribute | Mandatory | UT-FOV-003 |
| FR-10 | The effective FoV angle SHALL be reduced when `PressureScalar` exceeds `PRESSURE_FOV_THRESHOLD` (KD-7) | Mandatory | UT-FOV-004 |
| FR-11 | `EffectiveFoVAngle` SHALL be clamped to a minimum of `MIN_FOV_ANGLE` regardless of pressure | Mandatory | UT-FOV-005 |
| FR-12 | `EffectiveFoVAngle` SHALL be clamped to a maximum of `BASE_FOV_ANGLE + MAX_FOV_BONUS_ANGLE` regardless of Decisions value | Mandatory | UT-FOV-006 |
| FR-13 | A Decisions=20 agent SHALL have a measurably larger `EffectiveFoVAngle` than a Decisions=1 agent at the same PressureScalar | Mandatory | UT-FOV-007 |
| FR-14 | An agent with `PressureScalar = 0` SHALL have an `EffectiveFoVAngle` equal to `BASE_FOV_ANGLE + Decisions modifier` | Mandatory | UT-FOV-008 |

### 2.4.3 Occlusion Requirements

| ID | Requirement | Priority | Tests |
|----|-------------|----------|-------|
| FR-15 | An entity falling within the shadow cone projected by any opponent SHALL be excluded from the occlusion-passing set | Mandatory | UT-OCC-001 |
| FR-16 | An entity closer to the observer than the occluding opponent SHALL NOT be occluded by that opponent's shadow cone | Mandatory | UT-OCC-002 |
| FR-17 | Teammates SHALL NOT generate shadow cones at Stage 0 (OQ-1 resolution) | Mandatory | UT-OCC-003 |
| FR-18 | An observer SHALL NOT be occluded by their own position in self-reference edge cases | Mandatory | UT-OCC-004 |
| FR-19 | The ball SHALL be subject to occlusion using the same shadow cone geometry as agents (OQ-2 resolution) | Mandatory | UT-OCC-005 |
| FR-20 | A target entity occluded by multiple overlapping shadow cones SHALL still be treated as a single occlusion, not compound-penalised | Mandatory | UT-OCC-006 |

### 2.4.4 Recognition Latency Requirements

| ID | Requirement | Priority | Tests |
|----|-------------|----------|-------|
| FR-21 | A newly visible agent SHALL NOT enter `VisibleTeammates` or `VisibleOpponents` until their recognition latency counter has reached `L_rec` ticks | Mandatory | UT-LREC-001 |
| FR-22 | A Decisions=1 agent's `L_rec` SHALL equal `L_MAX` heartbeat ticks | Mandatory | UT-LREC-002 |
| FR-23 | A Decisions=20 agent's `L_rec` SHALL equal `L_MIN` heartbeat ticks | Mandatory | UT-LREC-003 |
| FR-24 | When an agent leaves the visible set (any reason), their latency counter SHALL reset to 0; the full `L_rec` cycle SHALL restart on re-appearance | Mandatory | UT-LREC-004 |
| FR-25 | When the half-turn orientation bonus is active (from First Touch §3.3.2), `L_rec` SHALL be reduced by 15% | Mandatory | UT-LREC-005 |
| FR-26 | The ball SHALL have `L_rec = 0`; it SHALL enter the snapshot immediately upon becoming visible (OQ-2 resolution) | Mandatory | UT-LREC-006 |
| FR-27 | Recognition latency SHALL be deterministic: identical (observerId, targetId, frameNumber, Decisions) inputs SHALL produce identical `L_rec` outputs | Mandatory | UT-LREC-007 |
| FR-28 | Deterministic noise applied to `L_rec` SHALL NOT use `System.Random` or any non-deterministic random number generator | Mandatory | UT-LREC-008 |

### 2.4.5 Blind-Side and Shoulder Check Requirements

| ID | Requirement | Priority | Tests |
|----|-------------|----------|-------|
| FR-29 | Entities in the 200° rear arc SHALL be excluded from the snapshot by default (KD-5) | Mandatory | UT-BLIND-001 |
| FR-30 | When `BlindSideWindowActive` is true, rear-arc entities SHALL be processed through Steps 3 and 4 with their own recognition latency cycle | Mandatory | UT-BLIND-002 |
| FR-31 | Rear-arc entities admitted during a shoulder check window SHALL NOT be immediately confirmed; they SHALL require `L_rec` ticks to confirm | Mandatory | UT-BLIND-003 |
| FR-32 | The shoulder check interval SHALL be attribute-scaled by `Anticipation` [1–20]: Anticipation=1 produces the longest interval; Anticipation=20 produces the shortest | Mandatory | UT-BLIND-004 |
| FR-33 | The shoulder check window SHALL expire after `SHOULDER_CHECK_DURATION` heartbeat ticks (KD-6) | Mandatory | UT-BLIND-005 |
| FR-34 | The Perception System SHALL trigger shoulder checks autonomously; the Decision Tree SHALL NOT be responsible for requesting checks (OQ-3 resolution) | Mandatory | UT-BLIND-006 |
| FR-35 | When an agent is in ball-possession state, the shoulder check interval SHALL double (§3.4.4) | Mandatory | UT-BLIND-007 |
| FR-36 | Shoulder check scheduling jitter SHALL be deterministic: identical (agentId, frameNumber) SHALL produce identical jitter values | Mandatory | UT-BLIND-008 |

### 2.4.6 Ball Perception Requirements

| ID | Requirement | Priority | Tests |
|----|-------------|----------|-------|
| FR-37 | `BallVisible` SHALL be true if and only if the ball is within `MAX_PERCEPTION_RANGE`, within `EffectiveFoVAngle`, and not occluded | Mandatory | UT-BALL-001 |
| FR-38 | `BallPerceivedPosition` SHALL equal `BallState.Position` when `BallVisible` is true | Mandatory | UT-BALL-002 |
| FR-39 | `BallPerceivedPosition` SHALL retain the last confirmed position when `BallVisible` is false (stale position) | Mandatory | UT-BALL-003 |
| FR-40 | `BallStalenessFrames` SHALL increment by 1 each heartbeat the ball is not directly visible | Mandatory | UT-BALL-004 |
| FR-41 | `BallStalenessFrames` SHALL reset to 0 each heartbeat the ball is directly visible | Mandatory | UT-BALL-005 |
| FR-42 | `BallStalenessFrames` SHALL be 0 if and only if `BallVisible` is true in the same snapshot (INV-8) | Mandatory | UT-BALL-006 |

### 2.4.7 Determinism Requirements

| ID | Requirement | Priority | Tests |
|----|-------------|----------|-------|
| FR-43 | Given identical world state and identical agent attributes at tick T, all 22 agents' `FilteredView` outputs SHALL be identical across independent runs | Mandatory | IT-DET-001 |
| FR-44 | The `_latencyCounters` dictionary state at tick T SHALL be identical across independent runs given identical inputs from tick 0 | Mandatory | IT-DET-002 |
| FR-45 | No non-deterministic API SHALL be called anywhere in the perception pipeline (no `System.Random`, `DateTime.Now`, `UnityEngine.Random`) | Mandatory | UT-DET-001 |
| FR-46 | The forced mid-heartbeat refresh mechanism SHALL be deterministic given identical trigger event sequences | Mandatory | UT-DET-002 |

### 2.4.8 Performance Requirements

| ID | Requirement | Priority | Tests |
|----|-------------|----------|-------|
| FR-47 | Total perception processing for all 22 agents SHALL complete within **2ms** per 10Hz heartbeat on target hardware | Mandatory | PT-001 |
| FR-48 | Per-agent perception pipeline SHALL complete within **90μs** on target hardware | Mandatory | PT-002 |
| FR-49 | No per-heartbeat heap allocation SHALL occur in the standard (non-forced-refresh) pipeline path | Mandatory | PT-003 |
| FR-50 | `_latencyCounters`, `_nextCheckTick`, and `_checkWindowExpiry` SHALL have fixed capacity allocated at match initialisation; capacity SHALL NOT grow at runtime | Mandatory | PT-004 |

---

## 2.5 Attribute Influence Summary

This table provides a consolidated reference for how `PlayerAttributes.Decisions` and
`PlayerAttributes.Anticipation` influence the perception pipeline. All formulas are
derived in Section 3. This table is informational — Section 3 is the mathematical authority.

| Attribute | Range | Pipeline Step | Influence | Direction |
|-----------|-------|---------------|-----------|-----------|
| `Decisions` | [1–20] | Step 2: FoV | Expands effective FoV angle | Higher → wider FoV |
| `Decisions` | [1–20] | Step 4: L_rec | Reduces recognition latency | Higher → faster recognition |
| `Anticipation` | [1–20] | Step 5: Shoulder check | Reduces check interval | Higher → more frequent checks |
| `PressureScalar` | [0–1] | Step 2: FoV | Narrows effective FoV angle | Higher → narrower FoV |
| Half-turn orientation (cross-spec: First Touch §3.3.2) | bool | Step 4: L_rec | Reduces L_rec by 15% | Active → faster recognition |
| Ball-possession state | bool | Step 5: Shoulder check | Doubles check interval | Possessed → less frequent checks |

**Notable absences (intentional):**
- `Anticipation` does NOT expand the geometric extent of the blind-side arc (KD-5). The
  200° rear arc is a fixed geometric constant, not an attribute-scaled value. What
  Anticipation scales is the *frequency* of autonomous checks, not the size of the arc.
- `Decisions` does NOT affect shoulder check frequency. Shoulder check behaviour is
  exclusively an Anticipation function.
- No attribute reduces the blind-side arc below 200° at Stage 0. Rear-arc sensitivity is
  a binary outcome of shoulder check timing, not a continuous awareness gradient.

---

## 2.6 Constants Declared in This Section

The following constants are introduced in this section. All values are provisional pending
Section 3 mathematical derivation and citation audit. Constants marked [GT] are
gameplay-tuned within literature-informed ranges; [LIT] are directly literature-derived.

| Constant | Value | Type | Source |
|----------|-------|------|--------|
| `MAX_PERCEPTION_RANGE` | 120f | float, metres | [GT] — full pitch diagonal; OQ-5 resolution |
| `BASE_FOV_ANGLE` | 160f | float, degrees | [LIT-INFORMED] — human peripheral vision ≈ 180°; 160° accounts for focus degradation at periphery |
| `MAX_FOV_BONUS_ANGLE` | 10f | float, degrees | [GT] — small range intentional; see §3.1.2 |
| `MIN_FOV_ANGLE` | 120f | float, degrees | [GT] — floor for pressure-narrowed FoV; prevents total tunnel vision |
| `PRESSURE_FOV_THRESHOLD` | 0.5f | float, [0–1] | [GT] — pressure onset at mid-scale; see §3.1.3 |
| `PRESSURE_FOV_SCALE` | 40f | float, degrees per unit | [GT] — governs rate of FoV narrowing above threshold; at PressureScalar=1.0 and threshold=0.5, reduction = (1.0−0.5)×40 = 20°, yielding EffectiveFoV floor contact at MIN_FOV_ANGLE=120° from a base ~160°. Section 3 will validate or revise. |
| `OCCLUSION_CONE_HALF_ANGLE` | 8f | float, degrees | [GT] — approximates agent body half-width at mid-range; see §3.2.1 |
| `L_MAX` | 5 | int, heartbeat ticks | [GT] — 500ms; within Franks (1985) bracket |
| `L_MIN` | 1 | int, heartbeat ticks | [GT] — 100ms minimum recognition; see §3.3.2 |
| `CHECK_MAX_TICKS` | 30 | int, heartbeat ticks | [GT] — 3.0s interval for Anticipation=1; within Franks (1985) bracket |
| `CHECK_MIN_TICKS` | 6 | int, heartbeat ticks | [GT] — 0.6s interval for Anticipation=20; Master Vol 1 elite rate |
| `SHOULDER_CHECK_DURATION` | 3 | int, heartbeat ticks | [GT] — 300ms window; Master Vol 1 "~0.3 seconds" |

All constants will be subject to citation audit in Section 8. Constants marked [GT]
will be explicitly distinguished from physics-derived values in that audit, consistent
with the project-wide constants methodology established in Pass Mechanics and Shot
Mechanics specifications.

---

## 2.7 Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | February 24, 2026, 3:00 PM PST | Claude (AI) / Anton | Initial draft. Pipeline, structs, 50 functional requirements, constants table. |
| 1.1 | February 24, 2026, 4:00 PM PST | Claude (AI) / Anton | Three issue resolutions: (1) Fixed-size embedded arrays replaced with `ReadOnlySpan<PerceivedAgent>` views into pre-allocated `PerceptionSystem` buffers — eliminates `unsafe` context requirement and preserves zero-allocation policy. (2) `PRESSURE_FOV_SCALE` constant added to §2.6 with provisional value and derivation note. (3) `WasOccludedThisTick` removed from shipped `PerceivedAgent` struct; replaced with `#if UNITY_EDITOR` companion `PerceivedAgentDebug` struct. |
| 1.2 | April 11, 2026 | Claude (AI) / Anton | **Architectural rework — separation of filter output from filter metadata.** (1) `PerceptionSnapshot` replaced by two structs: `FilteredView` (9 fields — pure consumer output delivered to Decision Tree) and `PerceptionDiagnostics` (7 fields — filter metadata for debug/telemetry/future systems, NOT delivered to DT). (2) `PerceivedAgent` reduced from 5 fields to 4: `RecognitionLatencyRemaining` removed (always 0 in confirmed arrays; moved to editor-only `PerceivedAgentDebug`). (3) `ReadOnlySpan` + count field pattern replaced with direct `PerceivedAgent[]` arrays — count fields were redundant with `.Length` and existed only to support the Span pattern. (4) `ForcedRefreshThisTick` added to `FilteredView` (resolves XC-4.5-01 from Section 4). (5) `BlindSidePerceivedAgents` added as separate array in `FilteredView` — blind-side agents are a distinct category of knowledge from forward-arc agents. (6) `EffectiveFoVAngle`, `PressureScalar`, `BlindSideWindowActive`, `BlindSideWindowExpiry`, and `ShoulderCheckAnimData` moved to `PerceptionDiagnostics` — these describe how the filter works, not what the agent knows. (7) `BlindSideWindowExpiry` type confirmed as `float` (match time in seconds), resolving the int/float type conflict between former Section 3 and Section 4 definitions. |

---

## Section 2 Summary

Section 2 establishes three things required before Section 3 can be drafted:

1. **Architecture** — The six-step perception pipeline is defined with invariants,
   forced refresh behaviour, and the system's position between world state and Decision Tree.
   The pipeline produces two distinct outputs: `FilteredView` (consumer data) and
   `PerceptionDiagnostics` (filter metadata).

2. **Data structures** — `FilteredView` (9 fields), `PerceptionDiagnostics` (7 fields),
   and `PerceivedAgent` (4 fields) are defined as implementation authority. The core
   architectural principle is **separation of filter output from filter metadata**: the
   Decision Tree receives only `FilteredView` — pure data about what the agent knows.
   Filter internals (FoV angles, pressure scalars, animation stubs) live in
   `PerceptionDiagnostics` and are never delivered to decision-making systems. All fields
   are documented with Stage 0 behaviour and Stage 1 upgrade paths. Ownership and
   amendment rules are established.

3. **Functional requirements** — 50 SHALL-level requirements cover every pipeline step,
   all output structs, all attribute influences, determinism, and performance contracts.
   Each requirement is individually testable and maps to Section 5 test cases.

No mathematical detail appears in this section — that is Section 3's domain. Section 2
specifies *what* the system must achieve; Section 3 specifies *how*.

**Next:** Section 3 — Core Models (FoV, Occlusion, Recognition Latency, Blind-Side,
Ball Perception, Pressure Scalar).

---

*End of Section 2 — Perception System Specification #7*
*Tactical Director — Specification #7 of 20 | Stage 0: Physics Foundation*
