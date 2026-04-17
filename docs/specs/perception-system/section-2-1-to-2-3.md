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
└────────────────────┬────────────────────┘
                     │ FilteredView (value struct — consumer output, per heartbeat)
                     ▼
┌─────────────────────────────────────────┐
│         DECISION TREE (#8)               │  ← Sole consumer; not yet specified
│   Reads FilteredView only; no world      │    Defines its own intake contract
│   access                                 │
└─────────────────────────────────────────┘
```

No downstream system — the Decision Tree or any AI module — may query world state
directly. `FilteredView` is the sole source of world knowledge for agent
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
`FilteredView` and `PerceptionDiagnostics` are assembled. Section 3 provides the
mathematical detail for each filter. This section specifies what the filters must
achieve (requirements) and what data structures they must produce (structs).

---

## 2.2 Perception Pipeline

### 2.2.1 Pipeline Overview

The perception pipeline is a single linear pass executed once per heartbeat per agent.
No iteration, no feedback loops, no look-ahead. Input is world state; output is
`FilteredView` + `PerceptionDiagnostics`. The pipeline is designed to be executed independently for all
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
  │  Step 6: BuildFilteredView()                                 │
  │  Assemble FilteredView and PerceptionDiagnostics. Populate   │
  │  all fields. Stamp with FrameNumber. Copy FilteredView by    │
  │  value to Decision Tree intake.                              │
  └──────────────────────────┴───────────────────────────────────┘

OUTPUT: FilteredView (consumer output — delivered to Decision Tree)
        PerceptionDiagnostics (filter metadata — NOT delivered to Decision Tree)
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

**Step 6 — BuildFilteredView:**
Assembles `FilteredView` and `PerceptionDiagnostics` from all confirmed entities.
Populates all fields per the struct definitions in §2.3.1 and §2.3.2. Stamps
`FrameNumber` with the current heartbeat tick. Updates `BallStalenessFrames` — if the
ball was not confirmed visible this tick, the counter increments from its previous
value; if visible, it resets to 0. `FilteredView` is passed by value to the Decision
Tree. `PerceptionDiagnostics` is retained internally for debug/telemetry consumers.
No reference to internal pipeline state is exposed through either struct.

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
| INV-6 | An entity whose recognition latency counter has not yet reached `L_rec` does not appear in the confirmed entity arrays |
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
specifications that consume or reference `FilteredView` or `PerceptionDiagnostics`.
Changes to these definitions require an amendment to this section and a corresponding
version increment. No field may be added, renamed, or removed by an implementing
specification.

### 2.3.1 `FilteredView` Struct

`FilteredView` is the consumer-facing output of the Perception System for one agent at
one heartbeat. It contains only what the Decision Tree needs to know about the world —
no filter internals, no diagnostic metadata. It is a value struct (KD-2), passed by
value to the Decision Tree each heartbeat.

**Design note — direct arrays vs. ReadOnlySpan:** The v1.1 design used `ReadOnlySpan`
views into pre-allocated `PerceptionSystem` buffers, with redundant count scalar fields.
v1.2 replaces this with direct `PerceivedAgent[]` array fields. The count scalars are
removed — `.Length` provides the count. Pre-allocation at match initialisation is still
used; the arrays are never reallocated at runtime (INV-10).

```csharp
/// <summary>
/// FilteredView — the agent's filtered knowledge of the world at one heartbeat.
///
/// Produced once per 10Hz heartbeat per agent. Passed by value to the Decision Tree.
/// Contains ONLY confirmed, filtered, latency-resolved world-state data.
/// Does NOT contain filter metadata (FoV angles, pressure scalars, animation stubs) —
/// those are in PerceptionDiagnostics (§2.3.2), which is never delivered to the DT.
///
/// OWNERSHIP: Perception System Specification #7 owns this struct definition.
/// CONSUMERS: Decision Tree Specification #8 (sole consumer at Stage 0).
/// MODIFICATION: Any field change requires a version bump of this specification.
/// </summary>
struct FilteredView
{
    // ── Identity and Timing ──────────────────────────────────────────────────────

    int     ObserverId;
    // AgentId of the agent this view describes.
    // Set to the observing agent's AgentState.AgentId. Range: 0–21.

    int     FrameNumber;
    // Heartbeat tick (10Hz counter) at which this view was built.
    // Used by Decision Tree to detect stale views (should never be > 1 tick old).

    bool    ForcedRefreshThisTick;
    // True if this view was produced by a mid-heartbeat forced refresh (§2.2.4).
    // False for all standard 10Hz heartbeat views.

    // ── Ball State ───────────────────────────────────────────────────────────────

    bool    BallVisible;
    // True if ball passed all three visibility tests (range, FoV, occlusion) this tick.
    // Ball is never subject to L_rec — it is immediately confirmed when visible (OQ-2).

    Vector2 BallPerceivedPosition;
    // Last confirmed ball position. If BallVisible=true: equals ground truth this tick.
    // If BallVisible=false: retains last confirmed position (may be stale).
    // Never (0,0) after first ball sighting — retains last known value indefinitely.
    // INVARIANT: BallStalenessFrames == 0 iff BallVisible == true (INV-8).

    int     BallStalenessFrames;
    // Number of consecutive heartbeats during which BallVisible was false.
    // 0 = ball currently visible (ground truth). Higher values = older position estimate.
    // Decision Tree should weight ball confidence inversely with this value.

    // ── Visible Agents (Forward Arc) ─────────────────────────────────────────────

    PerceivedAgent[] VisibleTeammates;
    // All teammate agents that are: (a) within EffectiveFoV, (b) not occluded,
    // (c) confirmed (latency counter satisfied).
    // Array length is 0..10 (max 10 outfield teammates + GK at Stage 0).
    // Pre-allocated at match initialisation; never reallocated at runtime (INV-10).

    PerceivedAgent[] VisibleOpponents;
    // All opponent agents that are: (a) within EffectiveFoV, (b) not occluded,
    // (c) confirmed (latency counter satisfied).
    // Array length is 0..11 (max 11 opponents).
    // Pre-allocated at match initialisation; never reallocated at runtime (INV-10).

    // ── Blind-Side Agents (Shoulder Check) ───────────────────────────────────────

    PerceivedAgent[] BlindSidePerceivedAgents;
    // Agents confirmed during an active shoulder check window (§3.4.3).
    // Empty when no shoulder check window is active, or when window is active
    // but no agents have been confirmed yet (L_rec not yet elapsed).
    // Subject to same L_rec requirements as forward-arc agents.
}
```

**Field count: 9.** This struct contains pure world-state data — what the agent knows.
Filter metadata (how the filter operated) is in `PerceptionDiagnostics` (§2.3.2).

### 2.3.2 `PerceptionDiagnostics` Struct

`PerceptionDiagnostics` contains information about *how* the perception filter operated,
not what the agent knows about the world. It is produced alongside `FilteredView` each
heartbeat but is **never delivered to the Decision Tree**. It is available for debug
tooling, telemetry, balance analysis, and the Animation System (Stage 1+).

```csharp
/// <summary>
/// PerceptionDiagnostics — filter metadata for one agent at one heartbeat.
///
/// Contains information about HOW the perception filter operated, NOT what the
/// agent knows about the world. This struct is NEVER delivered to the Decision Tree.
///
/// CONSUMERS: Debug tooling, telemetry, balance analysis, Animation System (Stage 1+).
/// OWNERSHIP: Perception System Specification #7. Same amendment authority as FilteredView.
/// MODIFICATION: Adding fields to diagnostics is lower-impact than FilteredView because
///   no decision-making system consumes this struct.
/// </summary>
struct PerceptionDiagnostics
{
    // ── Correlation Keys ─────────────────────────────────────────────────────────

    int     ObserverId;
    // Matches FilteredView.ObserverId for the same heartbeat.

    int     FrameNumber;
    // Matches FilteredView.FrameNumber for the same heartbeat.

    // ── Filter State ─────────────────────────────────────────────────────────────

    float   EffectiveFoVAngle;
    // Actual FoV angle in degrees after Decisions modifier and pressure narrowing.
    // Range: [MIN_FOV_ANGLE, BASE_FOV_ANGLE + MAX_FOV_BONUS_ANGLE] = [120°, 170°].
    // Useful for debug visualisation of perception cones and balance analysis.

    float   PressureScalar;
    // Pressure scalar at the time of filter execution.
    // Range: [0.0, 1.0]. Computed from opponent proximity (§3.6).
    // Useful for balance telemetry and verifying pressure model behaviour.

    // ── Shoulder Check State ─────────────────────────────────────────────────────

    bool    BlindSideWindowActive;
    // True during an active shoulder check window (§3.4.3).

    float   BlindSideWindowExpiry;
    // Match time (seconds) at which the current shoulder check window expires.
    // Meaningless if BlindSideWindowActive = false.

    // ── Animation Stub ───────────────────────────────────────────────────────────

    ShoulderCheckAnimData ShoulderCheckAnim;
    // Stage 1+ stub. Populated at Stage 0; no consumer until Animation System written.
    // See §3.4.4 for struct definition.
}
```

**Field count: 7.** None of these fields are consumed by the Decision Tree.

### 2.3.3 `PerceivedAgent` Struct

Each entry in `FilteredView.VisibleTeammates`, `FilteredView.VisibleOpponents`, and
`FilteredView.BlindSidePerceivedAgents` is a `PerceivedAgent`. This struct contains
everything the observing agent knows about a perceived entity. At Stage 0, position and
velocity are exact copies of world state. Stage 1 will introduce error models.

```csharp
/// <summary>
/// PerceivedAgent — data for one visible agent in a FilteredView.
///
/// All fields reflect agent state at the time of view construction.
/// At Stage 0: position and velocity are ground truth for confirmed entities.
/// At Stage 1+: error modelling will introduce uncertainty into these values.
///
/// This struct contains ONLY consumer-relevant data. Diagnostic fields
/// (latency counters, occlusion state) live in the editor-only
/// PerceivedAgentDebug struct (§2.3.4).
/// </summary>
struct PerceivedAgent
{
    int     AgentId;
    // AgentState.AgentId of the perceived agent. Range: 0–21.
    // Never equals the ObserverId from the containing FilteredView.

    Vector2 PerceivedPosition;
    // Ground-truth position at time of view construction (Stage 0).
    // Stage 1+: will include perception error based on distance and Decisions attribute.
    // Decision Tree should treat this as the observer's belief, not verified ground truth.

    Vector2 PerceivedVelocity;
    // Ground-truth velocity at time of view construction (Stage 0).
    // Stage 1+: will include direction estimation error for far agents.
    // Used by Decision Tree for option evaluation (passing lane prediction, pressing target).

    float   ConfidenceScore;
    // [0.0, 1.0]. At Stage 0: binary — 1.0 for confirmed, not present if unconfirmed.
    // Stage 1: continuous, degrading with distance and staleness.
    // Reserved at Stage 0 for forward compatibility — do not remove this field.
}
```

**Field count: 4.** `RecognitionLatencyRemaining` (present in v1.1) is removed — it was
always 0 in confirmed arrays (the only time an entry appears in `FilteredView`). It has
been moved to the editor-only `PerceivedAgentDebug` struct alongside occlusion diagnostics:

```csharp
// EDITOR-ONLY — never compiled into release builds.
// Populated by PerceptionSystem in parallel with the main pipeline when UNITY_EDITOR is defined.
#if UNITY_EDITOR
struct PerceivedAgentDebug
{
    int   AgentId;
    bool  WasOccludedThisTick;       // True if discarded by shadow cone test at Step 3
    int   LatencyCounterAtConfirm;   // Raw counter value when entity confirmed
    float ShadowConeAngle;           // Angle of occluding cone if WasOccludedThisTick = true
}
#endif
```

This keeps the shipped struct minimal and the debug path non-intrusive.

### 2.3.4 `PerceptionSystem` Internal State

The `PerceptionSystem` class maintains persistent state across heartbeats. This state
is not exposed through `FilteredView` or `PerceptionDiagnostics` and is not accessible
to any downstream system. It is documented here to define the full memory footprint and clarify which
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
  Decision Tree requires additional fields, it must raise an amendment request against
  this specification. The Decision Tree may not add fields unilaterally.
- **`PerceptionDiagnostics` amendments** carry lower risk than `FilteredView` amendments
  because no decision-making system consumes diagnostics. However, a version increment is
  still required.
- **No interface struct** (`IPerceptionConsumer`, etc.) is defined in this specification.
  The Decision Tree defines its own intake pattern. This follows the project interface
  principle: write the interface only when both sides are fully specified.
- **Goalkeeper Mechanics Specification #11** (not yet written) may require extended
  perception fields. If so, an amendment or a `GoalkeeperFilteredView` subtype
  (extending `FilteredView`) should be requested before that specification's Section 3 is
  drafted. At Stage 0, the goalkeeper uses the standard struct without extension.
- Any amendment to these structs requires a version increment to this section and a
  cross-specification impact assessment before ratification.

---

