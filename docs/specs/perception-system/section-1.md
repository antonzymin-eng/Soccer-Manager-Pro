# Perception System Specification #7 — Section 1: Purpose & Scope

**File:** `Perception_System_Spec_Section_1_v1_1.md`
**Purpose:** Defines the complete scope boundary for Perception System Specification #7 —
what this system owns, what it explicitly does not own, all key architectural decisions,
relationships to adjacent systems, and dependency contracts required before Section 3
can be drafted.

**Created:** February 24, 2026, 12:00 PM PST
**Version:** 1.2
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 7 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)

**Dependencies confirmed stable:**
- Ball Physics Specification #1 (approved) — `BallState` struct: position, velocity, spin
- Agent Movement Specification #2 (approved) — `AgentState`, `PlayerAttributes.Decisions`,
  `PlayerAttributes.Anticipation`
- Collision System Specification #3 (approved) — spatial hash query interface
- First Touch Mechanics Specification #4 (approved) — half-turn orientation bonus (§3.6,
  cross-referenced as 15% L_rec reduction; established in that spec, consumed here read-only)
- Perception System Outline v1.1 (approved — all OQ-1 through OQ-5 resolved)

**Open Dependency Flags:** None.

---

## Table of Contents

- [1.1 Document Purpose](#11-document-purpose)
- [1.2 What This Specification Covers](#12-what-this-specification-covers)
- [1.3 What Is Out of Scope](#13-what-is-out-of-scope)
  - [1.3.1 Responsibilities Owned by Other Specifications](#131-responsibilities-owned-by-other-specifications)
  - [1.3.2 Features Deferred to Stage 1+](#132-features-deferred-to-stage-1)
  - [1.3.3 Permanent Exclusions](#133-permanent-exclusions)
- [1.4 Key Design Decisions](#14-key-design-decisions)
- [1.5 Relationship to Adjacent Systems](#15-relationship-to-adjacent-systems)
  - [1.5.1 System Context](#151-system-context)
  - [1.5.2 What Perception Is Not Responsible For](#152-what-perception-is-not-responsible-for)
- [1.6 Dependencies and Integration Contracts](#16-dependencies-and-integration-contracts)
  - [1.6.1 Hard Dependencies — Must Be Stable Before Section 3](#161-hard-dependencies--must-be-stable-before-section-3)
  - [1.6.2 Soft Dependencies — Forward References to Unwritten Specifications](#162-soft-dependencies--forward-references-to-unwritten-specifications)
- [1.7 Version History](#17-version-history)

---

## 1.1 Document Purpose

This section defines the scope boundaries for Perception System Specification #7. It
serves as the authoritative reference for:

1. **What this specification owns** — Computation of `FilteredView` and `PerceptionDiagnostics`: the filtered,
   latency-aware, attribute-modulated view of the world state delivered to the Decision
   Tree each heartbeat.
2. **What other specifications own** — Decision-making logic, ball physics, agent
   locomotion, and spatial partitioning infrastructure.
3. **Key design decisions** — Seven architectural decisions, locked before technical
   drafting begins, with full rationale.
4. **Implementation timeline** — Stage 0 deliverables versus deferred and permanently
   excluded features.
5. **Dependencies** — Required interfaces from upstream specifications and forward
   contracts with downstream specifications not yet written.

Perception is the bridge between the simulation's omniscient world state and the
**limited, delayed, error-prone awareness** that must drive realistic agent behaviour.
The world state knows where every agent and the ball are at all times. No individual
agent should. A defender cannot react to a runner he cannot see. A midfielder under
pressure cannot calmly survey the full pitch. An elite playmaker perceives more, faster,
and more accurately than a low-quality squad player — and this difference must be
numerically grounded, tunable, and verifiably correct.

The central design problem is therefore not spatial: it is **epistemic**. The question
this system answers for every agent, 10 times per second, is: *what does this agent
believe the world looks like right now?* That belief is the only input the Decision Tree
will ever receive. If the perception model is wrong, every downstream decision is built
on a false foundation regardless of how sophisticated the Decision Tree becomes.

This specification covers only the interval from raw world state to completed
`FilteredView` and `PerceptionDiagnostics`. Everything before (simulation physics, world state maintenance)
and everything after (decision logic, action selection, tactical evaluation) is owned
by adjacent systems with defined boundary interfaces.

**On perception labels and named states:** Terms such as "aware," "tracking," and "blind"
are useful colloquial descriptions of agent cognition. They are not enums or states
in this system. Perception is parametric and continuous. An agent does not toggle
between `AWARE` and `UNAWARE` states — their snapshot population varies as a function of
facing direction, field of view, occlusion geometry, recognition latency, and attribute
values. All variation is numerical, not classified. No `PerceptionState` enum exists.

---

## 1.2 What This Specification Covers

Perception System Specification #7 governs the computation of a `FilteredView` and
`PerceptionDiagnostics` struct pair for each of the 22 active agents, executed once per
10Hz tactical heartbeat. The `FilteredView` represents the agent's current knowledge of
the world and is the sole input to the Decision Tree. `PerceptionDiagnostics` carries
filter metadata (FoV angle, pressure scalar, shoulder check state) and is not delivered
to the Decision Tree.

**Specifically, this specification covers:**

**`FilteredView` and `PerceptionDiagnostics` struct definitions.** All fields, types, and frame stamps are
defined here. These structs are owned exclusively by this specification; the Decision Tree
consumes `FilteredView` but may not modify its definition. Any amendment to either struct
goes through this specification's version control.

**Field of view (FoV) model.** Each agent perceives the world within a forward-facing
angular cone. The base cone half-angle is attribute-modified by `PlayerAttributes.Decisions`
[1–20]. Under pressure, the effective cone narrows (pressure degrades breadth, not depth —
KD-7). The FoV model is fully derived with worked examples in Section 3.

**Occlusion model.** Opponents project shadow cones from the observer's perspective.
Any candidate entity falling within an opponent's shadow cone is marked occluded and
excluded from the snapshot. Shadow cone geometry is computed as an angular interval, not
a full raycast. Teammate occlusion is deferred to Stage 1 (resolved: OQ-1). The model
is O(n × k) in practice where k is the number of nearby opponents.

**Recognition latency (L_rec).** When an entity first becomes visible to an agent, it
is not immediately added to the snapshot. A deterministic latency period must elapse
before the entity is confirmed. `L_rec` is a function of the `Decisions` attribute and
a deterministic hash of (agentId, targetId, frameNumber). Once confirmed, the entity
remains in the snapshot until it becomes invisible for a continuous expiry window.
The ball is exempt from `L_rec` — ball recognition is immediate upon visibility
(resolved: OQ-2).

**Blind-side awareness.** Agents have a rear arc — the portion of 360° not covered by
the forward FoV cone — in which they receive no perception by default. The exact arc
width is derived in Section 3 from the base FoV half-angle and is not stated here
to avoid locking a value before Section 3 derivation is complete. The Anticipation
attribute [1–20] governs the frequency of autonomous shoulder checks that temporarily
expose this rear arc. The angular width of the blind-side arc is fixed; what varies
is scan frequency (resolved: KD-5).

**Shoulder check mechanic.** When a shoulder check fires, the agent gains a 3-heartbeat
(~0.3s) awareness window covering the rear arc. Checks are triggered autonomously by the
Perception System — the Decision Tree does not request them (resolved: OQ-3). Interval
between checks is attribute-scaled by `Anticipation`; agents in possession check more
frequently (possession halving of interval documented in Section 3).

**Scanning frequency model.** The full perception pipeline executes each heartbeat for
all agents. "Scanning frequency" here specifically refers to the shoulder check interval
— how often a new rear-arc observation is generated. This is distinct from the FoV
model, which is continuously evaluated.

**Pressure scalar effect on perception.** Under pressure (derived from opponent proximity
and closing speed, identical in structure to the pressure scalar in First Touch Mechanics
#4 §3.3), the agent's effective FoV angle narrows. The agent sees fewer entities but
perceives those entities at full fidelity. This models attentional narrowing under
stress (Beilock, 2010).

**Half-turn orientation bonus.** When an agent is in a half-turned stance — established
in First Touch Mechanics #4 — recognition latency for entities in the rear/peripheral
arc is reduced by 15%. This is a cross-specification constant, already locked in
First Touch #4. This spec consumes it read-only; the constant is not re-derived here.

**Performance contract.** All 22 agent perception pipelines must complete within the
10Hz heartbeat budget (100ms available, allocation specified in Section 6). Spatial hash
queries, shadow cone tests, and latency bookkeeping must each meet per-operation budgets
defined in Section 6.

---

## 1.3 What Is Out of Scope

### 1.3.1 Responsibilities Owned by Other Specifications

**Decision Tree Specification #8** owns all decision-making logic. `FilteredView`
is delivered to the Decision Tree; what the Decision Tree does with it — which entities
to prioritise, what tactical options to evaluate, what action to select — is entirely
outside this specification's remit. The Perception System does not evaluate tactical
context or weight snapshot entities by importance.

**Ball Physics Specification #1** owns ball trajectory, bounce, spin decay, and all
aerodynamic modelling. Perception reads `BallState.Position` to determine visibility;
it reads nothing else from ball state and modifies nothing.

**Agent Movement Specification #2** owns all locomotion, attribute definitions, and
physical state management. Perception reads `AgentState` and `PlayerAttributes`; it
modifies neither. The facing direction that governs FoV is owned by Agent Movement
and consumed here read-only.

**Collision System Specification #3** owns the spatial hash implementation. Perception
calls `spatialHash.QueryRadius()` as a read-only consumer. It does not maintain its own
spatial index.

**Goalkeeper Mechanics Specification #11** (not yet written) will own any goalkeeper-
specific perception behaviour — for example, expanded awareness of shots within the
penalty area, or specialised ball-flight tracking. At Stage 0, the goalkeeper is treated
as a normal agent by the Perception System.

### 1.3.2 Features Deferred to Stage 1+

The following are acknowledged design areas that will be addressed in future stages.
Their deferral is intentional and does not represent an oversight. Each is documented
in Section 7 with the intended upgrade path.

- **Teammate occlusion.** At Stage 0, only opponents project shadow cones. Teammate
  bodies do not occlude perception. The shadow cone architecture supports teammate
  occlusion as an additive extension (resolved: OQ-1).
- **Weather and fog effects on vision distance.** `MAX_PERCEPTION_RANGE = 120f` [GT]
  is a fixed constant at Stage 0 (resolved: OQ-5). Stage 2 environmental systems will
  reduce this value dynamically.
- **Crowd noise and communication.** Agents do not communicate within the Perception
  System at Stage 0. Verbal cues ("man on") are deferred to a future Communication spec.
- **Referee awareness.** Referee position is not tracked in agent perception at Stage 0.
- **Detailed peripheral vision degradation model.** FoV degradation with angle-from-
  centre is not modelled at Stage 0 — entities are either inside the cone or outside it.
  A continuous clarity fall-off model is deferred to Stage 1+.
- **Context-sensitive shoulder check frequency.** At Stage 0, possession is the only
  context modifier (halving the check interval). Full context-sensitivity — different
  check rates in different tactical situations (pressing, tracking runner, set piece) —
  is a Stage 1 upgrade (resolved: OQ-3, flagged in Section 7).

### 1.3.3 Permanent Exclusions

- **Named perception states or awareness enums.** No `PerceptionState`, `AwarenessLevel`,
  or classified awareness enum will ever exist in this system. Perception is parametric.
- **Rendering or visual debug cone display.** Visualisation of perception cones is a
  Stage 1+ editor/debug tool. This specification produces no rendering data.
- **Per-entity perceptual fidelity variation.** All perceived entities are perceived at
  full fidelity. Degraded fidelity of individual entity data (e.g. uncertain velocity
  readings) is permanently excluded from Stage 0; the snapshot contains verified state
  or nothing.

---

## 1.4 Key Design Decisions

The following decisions are locked. They are established before Section 3 drafting
to prevent architectural drift. Each corresponds to a resolved outline question or
integration issue.

---

**KD-1: Perception runs at 10Hz, not 60Hz.**

The tactical heartbeat is the correct cadence for perception. Perception is a cognitive
process — scanning, recognising, updating a mental model — not a continuous physical
event like collision detection. Running perception at 60Hz would add O(22n) computation
per physics frame with no realistic benefit; human reaction times operate at 100–300ms
granularity, which the 10Hz heartbeat models correctly.

Physics-layer events may trigger a forced mid-heartbeat perception refresh for the
directly involved agents only. This is edge-triggered, not polled. All other agents
continue on the 10Hz schedule. The complete enumeration of qualifying trigger events
(e.g. ball contact, tackle completion, possession change) is defined in Section 3;
only two illustrative examples are given here to establish the concept.

---

**KD-2: `FilteredView` and `PerceptionDiagnostics` are value structs, not classes.**

Consistent with `PassRequest`, `ShotRequest`, and `BallState` throughout the Stage 0
specifications. Stack allocation eliminates heap pressure. `FilteredView` is copied into
the Decision Tree intake each heartbeat; no shared reference exists. `PerceptionDiagnostics`
is written to a pre-allocated debug buffer and never delivered to the Decision Tree.
This enforces the read-only contract: the Decision Tree cannot modify a `FilteredView`
in a way that affects the Perception System's internal state.

---

**KD-3: Occlusion uses shadow cone approximation, not full raycasting.**

Raycasting against agent body geometry is unnecessary precision for Stage 0 and
computationally unjustifiable at n=22 agents running at 10Hz. Each opponent projects
a shadow cone defined by their body half-width and the observer-to-occluder distance.
Any candidate entity whose bearing from the observer falls within this angular interval
is marked occluded.

This approximation is conservative — at very short range the cone slightly over-occludes
relative to a true raycast — but this is an acceptable error direction (producing slight
over-caution in agents, never false confidence). The architecture supports raycast
upgrade in Stage 1+ without restructuring.

---

**KD-4: Recognition latency is deterministic, not random.**

`L_rec` is generated from a deterministic hash of (agentId, targetId, frameNumber)
seeded from `matchSeed`. There is no call to `System.Random` anywhere in the Perception
System. This is a non-negotiable project-wide requirement: all simulation outcomes must
be deterministically reproducible from match seed for replay and multiplayer consistency.

The hash produces values that *appear* varied and unpredictable to an observer but are
entirely reproducible given the same inputs. This is identical in principle to the error
generation model in Pass Mechanics §3.5 and Shot Mechanics §3.4.

---

**KD-5: The blind-side arc geometry is fixed; Anticipation governs scan frequency only.**

The blind-side arc is defined as the portion of 360° not covered by the forward FoV cone
(i.e., two times the FoV half-angle subtracted from 360°). The exact arc width is derived
in Section 3 from the base FoV half-angle; it is not stated here to prevent a premature
constant from drifting against the Section 3 derivation. High-Anticipation agents do not
gain a smaller blind-side arc — they perform shoulder checks more frequently, reducing
the average time a rear-arc entity goes undetected.

This decision prevents a class of edge cases where high-Anticipation agents approach
360° awareness, which would be biomechanically implausible and tactically unrealistic.
Attribute variation affects *process* (how often you look) not *anatomy* (how wide your
eyes are). The mathematical relationship between Anticipation [1–20] and check interval
is fully derived in Section 3.

---

**KD-6: The ball is always immediately recognised when visible; no L_rec applies.**

`L_rec` models cognitive agent-recognition delay — the time a human needs to identify
and categorise a newly-seen object as a specific person. The ball is an inanimate
object with no identification requirement. Applying `L_rec` to ball visibility would
misuse the model and produce the unrealistic outcome of agents not reacting to a ball
rolling directly towards them.

Ball staleness — the condition where the ball has been out of perception for long enough
that the agent's belief about ball position degrades — handles the "lost sight" case
separately. Ball staleness is defined in Section 3. The absence of `L_rec` for the ball
is a deliberate design decision and must not be treated as an oversight.

*(Resolved: OQ-2)*

---

**KD-7: Pressure degrades perception breadth, not depth.**

Under pressure — defined by the same pressure scalar model used in First Touch Mechanics
#4 §3.3, consuming opponent proximity and closing speed — the agent's effective FoV
half-angle narrows proportionally. The agent perceives fewer entities. Entities within
the narrowed cone are perceived at full fidelity: their position, velocity, and
recognition status are not degraded.

This models the well-documented phenomenon of attentional narrowing under acute stress
(Beilock, 2010; Janelle, 2002): stressed players miss peripheral information but are
not impaired in processing what they do see. The distinction matters for Decision Tree
quality — a pressured agent should make worse decisions because they have less
information, not because their available information is noisy.

Minimum effective FoV under maximum pressure is bounded by `MIN_FOV_ANGLE` [GT] to
prevent degenerate near-zero cone states. Full derivation in Section 3.

---

## 1.5 Relationship to Adjacent Systems

### 1.5.1 System Context

Perception System occupies the boundary between the simulation's physics layer and
its cognitive layer. All upstream dependencies are approved. The sole downstream
consumer (Decision Tree #8) is not yet written; no interface is defined here.

```
Ball Physics (#1)          Agent Movement (#2)         Collision System (#3)
   BallState.Position    AgentState, PlayerAttributes   spatialHash.QueryRadius()
         │                        │                              │
         └────────────────────────┼──────────────────────────────┘
                                  │
                                  ▼
                     ┌─────────────────────────┐
                     │  Perception System (#7)  │
                     │  10Hz tactical heartbeat │
                     │  22 agents per tick      │
                     └─────────────────────────┘
                                  │
                    FilteredView + PerceptionDiagnostics (value structs)
                    one pair per agent per heartbeat
                                  │
                                  ▼
                         Decision Tree (#8)
                         [not yet specified]


First Touch (#4) ─── cross-reference ──► half-turn orientation bonus
                                         (15% L_rec reduction — established in #4,
                                          consumed here read-only, not re-derived)

Perception (#7) ─── publishes (conditional) ──► PerceptionRefreshEvent
                                                        │
                                                        └──► Event System (#17) [when written]
```

### 1.5.2 What Perception Is Not Responsible For

Once `FilteredView` is delivered to the Decision Tree, Perception's responsibility
ends for that heartbeat. It does not monitor what decisions are made from the snapshot,
evaluate decision quality, or provide feedback on whether entities were correctly prioritised.

Perception does not make decisions. It produces a filtered view of the world; what the
Decision Tree does with that view is entirely outside this system's scope.

Perception does not drive agent movement. A shoulder check is an internal cognitive
event that updates the snapshot. It is not a physical head-turn animation at Stage 0.
The animation data contract for shoulder checks is defined as a stub in Section 3 and
will be consumed by the Animation System in Stage 1+.

Perception does not own the spatial hash. It is a read-only consumer. If the spatial
hash is unavailable or returns an error, the Perception System degrades gracefully
(defined in Section 3) but does not attempt to rebuild or maintain the index.

---

## 1.6 Dependencies and Integration Contracts

### 1.6.1 Hard Dependencies — Must Be Stable Before Section 3

All hard dependencies are confirmed stable. No blocking items exist.

| Dependency | Specification | Status | Fields Required |
|---|---|---|---|
| `BallState.Position` | Ball Physics #1 | ✅ Approved | `Position: Vector3` — is ball visible to this agent? |
| `AgentState.Position` | Agent Movement #2 | ✅ Approved | `Position: Vector3` — observer and target positions |
| `AgentState.FacingDirection` | Agent Movement #2 | ✅ Approved | `FacingDirection: Vector3` — FoV cone orientation |
| `AgentState.Velocity` | Agent Movement #2 | ✅ Approved | `Velocity: Vector3` — snapshot field; staleness heuristic |
| `PlayerAttributes.Decisions` | Agent Movement #2 §3.5.6 | ✅ Approved | `int [1–20]` — FoV modifier, L_rec modifier |
| `PlayerAttributes.Anticipation` | Agent Movement #2 §3.5.6 | ✅ Approved | `int [1–20]` — shoulder check frequency |
| Spatial hash query | Collision System #3 | ✅ Approved | `QueryRadius(position, radius) → List<EntityRef>` |
| Half-turn orientation flag | First Touch #4 | ✅ Approved | Cross-reference constant (15% L_rec reduction); no new interface |
| Pressure scalar pattern | First Touch #4 §3.3 | ✅ Approved | Pattern reused directly; no new interface required |
| `matchSeed` | Simulation root | ✅ Established | Deterministic hash seed for L_rec; pattern identical to Pass/Shot Mechanics |

**Attribute read contract:** `PlayerAttributes.Decisions` and `PlayerAttributes.Anticipation`
are read once per heartbeat at pipeline entry and cached for the duration of that agent's
pipeline execution. A mid-heartbeat attribute change — not possible at Stage 0 — cannot
corrupt the in-flight snapshot computation.

### 1.6.2 Soft Dependencies — Forward References to Unwritten Specifications

| Consumer Specification | What Perception Produces | Interface Status |
|---|---|---|
| Decision Tree Spec #8 | `FilteredView` struct — Section 3 defines all fields the Decision Tree must consume | Caller contract; Decision Tree implements its own intake |
| Event System Spec #17 | `PerceptionRefreshEvent` stub — published on forced mid-heartbeat refresh | Event struct only; no `IPerceptionEventConsumer` interface written here |
| Animation System (Stage 1+) | `ShoulderCheckAnimData` stub — populated but unconsumed at Stage 0 | Stub struct defined in Section 3 |
| Goalkeeper Mechanics Spec #11 | Goalkeeper treated as standard agent at Stage 0; spec #11 will extend or override | No interface defined until #11 is written |
| Fixed64 Math Library Spec #9 | Trigonometric functions in FoV angle and shadow cone calculations | Float arithmetic at Stage 0; migration to Fixed64 documented in Section 7 |

---

## 1.7 Version History

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0 | February 24, 2026, 12:00 PM PST | Claude (AI) / Anton | Initial draft. All OQ-1 through OQ-5 from Outline v1.1 reflected. All 7 KDs locked. |
| 1.2 | April 22, 2026 | Claude (AI) / Anton | Struct rename following §3 v1.3 architectural rework: `PerceptionSnapshot` replaced throughout by `FilteredView` (consumer output, 9 fields) and `PerceptionDiagnostics` (filter metadata, 7 fields). KD-2 rationale updated to reflect two-struct architecture. §1.2 `PerceptionSnapshot` struct description updated to list both structs. §1.5.1 diagram updated. §1.6.2 soft dependency table updated. Section Summary updated. |

---

## Section 1 Summary

Perception System Specification #7 governs computation of `FilteredView` and
`PerceptionDiagnostics` — the filtered, latency-aware, attribute-modulated world view
delivered to the Decision Tree each 10Hz heartbeat. It owns the FoV model, shadow-cone
occlusion, recognition latency, blind-side awareness, shoulder check mechanic, pressure
scalar effect on perception breadth, and the `FilteredView` / `PerceptionDiagnostics`
struct definitions.

It does not own decision-making logic, ball physics, agent locomotion, spatial hash
infrastructure, or goalkeeper-specific perception. Named perception states do not exist
in this system; all variation is parametric and continuous.

Seven key design decisions are locked. All hard dependencies are confirmed stable from
approved upstream specifications. No blocking items exist.

**Next:** Section 2 — System Overview and Functional Requirements.

---

*End of Section 1 — Perception System Specification #7*
*Tactical Director — Specification #7 of 20 | Stage 0: Physics Foundation*