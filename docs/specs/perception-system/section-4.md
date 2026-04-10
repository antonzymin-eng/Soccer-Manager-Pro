# Perception System Specification #7 — Section 4: Architecture & Integration

**File:** `Perception_System_Spec_Section_4_v1_1.md`  
**Purpose:** Defines the complete file/module architecture for the Perception System and
specifies all integration contracts with upstream dependencies — Agent Movement (#2),
Ball Physics (#1), Collision System (#3) — and the downstream output contract with
Decision Tree (#8). Defines the forced refresh event model and the Event System stub
relationship. This section is the authoritative reference for boundary ownership.
Implementers must not cross these boundaries without a formal amendment to this section.

**Created:** February 25, 2026, 12:00 PM PST  
**Revised:** February 26, 2026  
**Version:** 1.1  
**Status:** DRAFT — Awaiting Lead Developer Review  
**Specification Number:** 7 of 20 (Stage 0 — Physics Foundation)  
**Author:** Claude (AI) with Anton (Lead Developer)

**Prerequisite Sections:**
- Section 1 v1.1 (scope, KD-1 through KD-7 locked)
- Section 2 v1.1 (pipeline, functional requirements, struct definitions)
- Section 3 v1.1 (all models specified: FoV, occlusion, L_rec, shoulder check, ball
  perception, pressure scalar, `PerceptionSnapshot` struct definition, forced refresh,
  worked examples, constants table)

**Open Dependency Flags:** None. All hard dependencies confirmed stable from approved
upstream specifications.

---

## Table of Contents

- [4.1 File Structure](#41-file-structure)
- [4.2 Integration with Agent Movement (#2)](#42-integration-with-agent-movement-2)
  - [4.2.1 AgentState Read Contract](#421-agentstate-read-contract)
  - [4.2.2 PlayerAttributes Read Contract](#422-playerattributes-read-contract)
  - [4.2.3 Write Prohibition](#423-write-prohibition)
  - [4.2.4 Failure Modes at This Boundary](#424-failure-modes-at-this-boundary)
- [4.3 Integration with Ball Physics (#1)](#43-integration-with-ball-physics-1)
  - [4.3.1 BallState Read Contract](#431-ballstate-read-contract)
  - [4.3.2 Write Prohibition](#432-write-prohibition)
  - [4.3.3 Failure Modes at This Boundary](#433-failure-modes-at-this-boundary)
- [4.4 Integration with Collision System (#3)](#44-integration-with-collision-system-3)
  - [4.4.1 Spatial Hash Query Contract](#441-spatial-hash-query-contract)
  - [4.4.2 Pressure Query Contract](#442-pressure-query-contract)
  - [4.4.3 Write Prohibition](#443-write-prohibition)
  - [4.4.4 Failure Modes at This Boundary](#444-failure-modes-at-this-boundary)
- [4.5 Output Contract: Decision Tree (#8)](#45-output-contract-decision-tree-8)
  - [4.5.1 PerceptionSnapshot Ownership](#451-perceptionsnapshot-ownership)
  - [4.5.2 Delivery Mechanism and Cadence](#452-delivery-mechanism-and-cadence)
  - [4.5.3 No Interface Defined Here](#453-no-interface-defined-here)
  - [4.5.4 Amendment Authority](#454-amendment-authority)
- [4.6 Forced Refresh Events](#46-forced-refresh-events)
  - [4.6.1 Qualifying Trigger Events](#461-qualifying-trigger-events)
  - [4.6.2 Forced Refresh Scope](#462-forced-refresh-scope)
  - [4.6.3 PerceptionRefreshEvent Stub](#463-perceptionrefreshevent-stub)
  - [4.6.4 Events Not Published by This System](#464-events-not-published-by-this-system)
  - [4.6.5 Stage 0 Stub Implementation](#465-stage-0-stub-implementation)
- [4.7 Dependency Graph](#47-dependency-graph)
- [4.8 Cross-Specification Validation Checks](#48-cross-specification-validation-checks)
- [4.9 Version History](#49-version-history)

---

## 4.1 File Structure

The Perception System module occupies its own folder within the simulation scripts
directory. All files are listed with their responsibilities. No other specification
may add files to this folder without a formal amendment to this section.

```
/Scripts/PerceptionSystem/
│
│   PerceptionSystem.cs
│       Main orchestrator. Owns the per-agent, per-heartbeat perception pipeline
│       (Steps 1–6 per §3.0). Entry point for the simulation heartbeat scheduler.
│       Maintains internal state: _latencyCounters, _shoulderCheckCounters,
│       _blindSideWindowExpiry (per agent). Schedules autonomous shoulder checks.
│       Handles forced mid-heartbeat refresh for qualifying events (§4.6).
│       Publishes PerceptionRefreshEvent stub on forced refresh.
│       Produces one PerceptionSnapshot per agent per heartbeat.
│
│   FovCalculator.cs
│       Computes effective FoV angle per agent per heartbeat (§3.1).
│       Applies Decisions attribute modifier and PressureScalar degradation.
│       Performs angular candidacy test for each entity in the candidate list.
│       Static class. All methods are deterministic. No side effects.
│
│   OcclusionFilter.cs
│       Computes shadow cone geometry for each opponent occluder (§3.2).
│       Determines if candidate entity T falls within any occluder's shadow cone.
│       Applies depth-ordering check (dot product) before angular check.
│       Opponents only at Stage 0 — no teammate shadow cones.
│       Static class. All methods are deterministic. No side effects.
│
│   RecognitionLatencyTracker.cs
│       Manages _latencyCounters dictionary (§3.3.5).
│       Computes L_rec from Decisions attribute and deterministic hash.
│       Applies half-turn orientation bonus (§3.3.3, First Touch §3.3.2).
│       Increments counters for visible-but-unconfirmed entities.
│       Resets counters on entity exit or forced refresh.
│       Contains DeterministicHash() — project-standard xorshift-based hash.
│       Not static — owns mutable counter state across heartbeats.
│
│   ShoulderCheckScheduler.cs
│       Manages autonomous shoulder check scheduling per agent (§3.4.2).
│       Computes CheckInterval_ticks from Anticipation attribute.
│       Applies deterministic jitter to prevent synchronised mass-checks.
│       Doubles interval when agent is in possession state.
│       Manages window open/close and BlindSideWindowExpiry timestamps.
│       Populates ShoulderCheckAnimData stub (§3.4.5) for Stage 1 animation.
│       Not static — owns mutable scheduler state across heartbeats.
│
│   BallPerceptionEvaluator.cs
│       Evaluates ball visibility: range check, FoV test, occlusion test (§3.5).
│       Updates BallVisible and BallPerceivedPosition in the snapshot.
│       Increments BallStalenessFrames when ball is not directly visible.
│       Resets BallStalenessFrames on re-entry into visibility.
│       Static class. All methods are deterministic. No side effects.
│
│   PressureEvaluator.cs
│       Computes PressureScalar for an agent using formula from First Touch §3.5
│       (reused verbatim — no new derivation). §3.6.
│       Single use: result passed to FovCalculator. Not used for L_rec or
│       shoulder check timing.
│       Static class. All methods are deterministic. No side effects.
│
│   SnapshotBuilder.cs
│       Assembles the final PerceptionSnapshot value struct from the outputs
│       of all prior pipeline steps (§3.7).
│       Populates all 12 fields. Guarantees no null references in the struct.
│       Handles empty-snapshot case (no visible entities) without error.
│       Static class. Pure assembly — no computation.
│
│   PerceptionConstants.cs
│       All tunable constants for the Perception System.
│       Source of truth. No other Perception file may use inline numeric literals.
│       Every constant is tagged [GT], [DERIVED], or [CROSS] per project standard.
│       Cross-spec constants are referenced by pointer, not duplicated.
│       Frozen at Stage 0 approval — changes require spec revision and regression.
│
│   PerceptionSnapshot.cs
│       PerceptionSnapshot struct definition (§3.7.1). OWNED BY THIS SPECIFICATION.
│       PerceivedAgent sub-struct definition (§3.7.2).
│       PerceptionDebugRecord struct definition (§3.7.3).
│       ShoulderCheckAnimData stub definition (§3.7.4).
│       Decision Tree consumes these structs; it does not define them.
│
│   PerceptionEvents.cs
│       PerceptionRefreshEvent struct definition.
│       Stage 0: published to no-op Event Bus stub.
│       Stage 1: published to Event System (#17).
│
└───/Tests/
        FovCalculatorTests.cs           — Unit tests §5.1 FOV-* (8 tests)
        OcclusionFilterTests.cs         — Unit tests §5.1 OCC-* (8 tests)
        RecognitionLatencyTests.cs      — Unit tests §5.1 LAT-* (8 tests)
        ShoulderCheckTests.cs           — Unit tests §5.1 BSA-* (8 tests)
        BallPerceptionTests.cs          — Unit tests §5.1 BAL-* (4 tests)
        SnapshotBuilderTests.cs         — Unit tests §5.1 SNAP-* (4 tests)
        PerceptionIntegrationTests.cs   — Integration tests §5.2 (12 tests)
        PerceptionBalanceTests.cs       — Balance tests §5.3 (3 scenarios)
```

**Ownership rule:** `PerceptionSnapshot.cs` is defined here. Decision Tree (#8) is the
consumer — it reads `PerceptionSnapshot` but does not define the struct. If Decision
Tree requires additional fields, that amendment must originate in this specification, not
in Decision Tree's specification.

**Ownership rule:** `PerceptionEvents.cs` is defined here. If Event System (#17) requires
a different publish signature at Stage 1, this specification requires a corresponding
amendment before the Event System consumes these structs.

---

## 4.2 Integration with Agent Movement (#2)

Agent Movement is the primary upstream dependency. Perception reads from `AgentState`
and `PlayerAttributes` once per heartbeat per observed agent. No Agent Movement
interfaces are called. Perception is a read-only consumer of Agent Movement data.

### 4.2.1 AgentState Read Contract

The authoritative `AgentState` struct is defined in Agent Movement Specification #2
§3.5.3. Perception reads the following fields:

```csharp
// Defined in: Agent Movement Specification #2, §3.5.3
// Owner: Agent Movement
// Consumer: Perception System (PerceptionSystem.cs, FovCalculator.cs, OcclusionFilter.cs,
//           BallPerceptionEvaluator.cs, PressureEvaluator.cs)
// Access: Read-only. No field is ever written by Perception.

struct AgentState
{
    // Fields consumed by Perception System:
    int     AgentId;            // Identity key for latency counter dictionary
    Vector2 Position;           // Observer position: spatial hash centre; occluder position
    Vector2 FacingDirection;    // Normalised. Used for FoV cone axis (§3.1.1)
    int     TeamId;             // Used to discriminate teammate vs opponent shadow cones (§3.2.2)

    // Fields NOT consumed by Perception System (listed for completeness):
    Vector2 Velocity;           // Not used — ball velocity not included in snapshot at Stage 0
    // ... additional fields per Agent Movement §3.5.3 (omitted here)
}
```

**Read cadence:** All consumed fields are read once at the start of the per-agent
heartbeat pipeline (Step 0: attribute caching, per §3.0). They are cached for the
duration of the pipeline. Any `AgentState` change that occurs mid-heartbeat is not
reflected until the next heartbeat tick. This is deliberate (KD-1 rationale: perception
is a cognitive snapshot, not a continuous physical update).

**AgentState source:** The Perception System receives `AgentState[22]` — the complete
array of all agent states — from the simulation heartbeat scheduler. The scheduler owns
the canonical array and passes it by reference. Perception reads from this reference;
it does not maintain its own copy of agent state.

**Possession state — external tracker (Option B):** `AgentState` does not contain a
`HasPossession` field. Per Ball Physics Specification #1 §3.1.11 (Option B resolution,
ERR-008), possession is tracked externally by the agent system — it is not stored in
`BallState` or `AgentState`. The `ShoulderCheckScheduler` therefore reads possession
state from an external possession tracker owned by the simulation scheduler, not from
`AgentState`. At Stage 0, `ShoulderCheckScheduler` accepts a `bool hasPossession`
parameter passed in by the heartbeat scheduler at pipeline start, sourced from
whichever system holds the external possession record. The formal interface for that
possession tracker is not defined here — it will be specified when the agent system's
possession model is fully documented. This is a known integration gap; cross-spec
validation check XC-4.2-02 tracks its resolution.

### 4.2.2 PlayerAttributes Read Contract

The authoritative `PlayerAttributes` struct is defined in Agent Movement Specification
#2 §3.5.6. Perception reads the following attributes:

```csharp
// Defined in: Agent Movement Specification #2, §3.5.6
// Owner: Agent Movement
// Consumer: Perception System (FovCalculator.cs, RecognitionLatencyTracker.cs,
//           ShoulderCheckScheduler.cs)
// Access: Read-only. Cached once at heartbeat start per §3.0.

struct PlayerAttributes
{
    // Attributes consumed by Perception System:
    int Decisions;      // [1–20]. Governs FoV modifier (§3.1.2) and L_rec (§3.3.2)
    int Anticipation;   // [1–20]. Governs shoulder check interval (§3.4.2)

    // Attributes NOT consumed by Perception System (listed for traceability):
    // Technique, FirstTouch, Pace, Passing, Shooting, Positioning, etc.
    // ... per Agent Movement §3.5.6 (omitted here)
}
```

**Attribute read timing:** Attributes are read once at pipeline Step 0 (heartbeat start)
and cached in local variables for the pipeline duration. This eliminates the possibility
of mid-pipeline attribute drift caused by a concurrent attribute update (e.g., fatigue
decrement). Attributes are never re-read mid-pipeline.

**Attribute validation:** Perception does not validate attribute ranges. Agent Movement
is responsible for ensuring `Decisions` and `Anticipation` are in [1–20] at all times.
If an out-of-range value is received, Perception applies the formula unchanged — the
result may be clamped by the formula's own output bounds (e.g., `EffectiveFoV` clamp
to `MIN_FOV_ANGLE`). A `Debug.LogWarning` is emitted in development builds only.

### 4.2.3 Write Prohibition

The Perception System **must never write to any Agent Movement struct or field.**
Specifically:
- `AgentState.FacingDirection` — read only; shoulder checks do not alter the facing vector
- `PlayerAttributes` — read only; Perception does not modify attributes
- No new fields may be added to `AgentState` or `PlayerAttributes` by this specification
  without a formal Agent Movement amendment

**Rationale:** Writing to AgentState from the Perception System would create a circular
dependency. Agent Movement must remain the sole authority on agent physical state.

**Note on possession:** Although `ShoulderCheckScheduler` reads `hasPossession` (§4.2.1),
this value comes from an external possession tracker — not from `AgentState`. Perception
does not write possession state anywhere.

### 4.2.4 Failure Modes at This Boundary

| Failure Mode | ID | Detection | Behaviour |
|---|---|---|---|
| `AgentState` array is null | FM-AM-01 | Assert at heartbeat start | Log error; skip heartbeat; output empty snapshots |
| `AgentState` array length ≠ 22 | FM-AM-02 | Assert at heartbeat start | Log error; skip heartbeat |
| `FacingDirection` is zero vector | FM-AM-03 | Magnitude check in FovCalculator | Log warning; treat as (1,0) default facing |
| `Decisions` or `Anticipation` outside [1–20] | FM-AM-04 | Range check (development only) | Log warning; clamp to [1,20] before formula |
| `AgentId` not found in latency counter dictionary | FM-AM-05 | Dictionary ContainsKey check | Initialise counter to 0; treat as new agent |

---

## 4.3 Integration with Ball Physics (#1)

Ball Physics is a read-only upstream dependency. Perception reads `BallState.Position`
once per heartbeat to determine ball visibility. No Ball Physics interfaces are called.
Perception never modifies ball state.

### 4.3.1 BallState Read Contract

The authoritative `BallState` struct is defined in Ball Physics Specification #1
§3.1.1. Perception reads the following field:

```csharp
// Defined in: Ball Physics Specification #1, §3.1.1
// Owner: Ball Physics
// Consumer: Perception System (BallPerceptionEvaluator.cs)
// Access: Read-only. BallState is never modified by Perception.

struct BallState
{
    // Field consumed by Perception System:
    Vector3 Position;   // Ball world position (x,y,z). Perception uses x,y only (2D pitch).
                        // Used for: range check, FoV angle test, occlusion test (§3.5.1)

    // Fields NOT consumed by Perception System at Stage 0:
    Vector3 Velocity;           // Not used — ball velocity not included in snapshot at Stage 0
    Vector3 AngularVelocity;    // Not used — spin not perceived by agents at Stage 0
    // ... additional fields per Ball Physics §3.1.1 (omitted here)
}
```

**2D projection:** `BallState.Position` is a `Vector3`. Perception uses only the `x`
and `y` components, projecting to the 2D pitch plane. The `z` (height) component is
ignored for all distance and angle calculations. This is consistent with the 2D
coordinate system used throughout all Stage 0 specifications.

**Read cadence:** `BallState` is read once per heartbeat, after `AgentState` caching
(Step 0). The same ball position is used for all 22 agents' ball perception calculations
in that heartbeat. Ball position is not re-read mid-heartbeat.

**Ball velocity not perceived:** At Stage 0, agents do not perceive ball velocity or
spin. The `BallState.Velocity` field is not consumed. The Decision Tree receives
`BallVisible` and `BallPerceivedPosition` only. Ball velocity perception is a Stage 1
upgrade (flagged in Section 7 of this specification).

### 4.3.2 Write Prohibition

The Perception System **must never write to any Ball Physics struct or field.**

This is an absolute prohibition with no exceptions. Ball Physics is a physics-layer
system; Perception is a cognitive-layer system. Cognitive-layer systems consume
physics-layer output; they do not modify it.

### 4.3.3 Failure Modes at This Boundary

| Failure Mode | ID | Detection | Behaviour |
|---|---|---|---|
| `BallState` reference is null | FM-BP-01 | Assert at heartbeat start | Log error; set `BallVisible=false`, `BallStalenessFrames++` for all agents this tick |
| `BallState.Position` is NaN or Infinity | FM-BP-02 | IsNaN / IsInfinity check in BallPerceptionEvaluator | Log error; treat ball as invisible this tick |

---

## 4.4 Integration with Collision System (#3)

The Collision System provides two services to Perception: spatial proximity queries
(for candidate entity enumeration) and opponent position data (for pressure scalar
calculation). Perception is a read-only consumer of both services.

### 4.4.1 Spatial Hash Query Contract

The authoritative spatial hash query interface is defined in Collision System
Specification #3 §3.1.4. Perception calls this interface once per agent per heartbeat
in pipeline Step 1.

```csharp
// Defined in: Collision System Specification #3, §3.1.4
// Owner: Collision System
// Caller: Perception System (PerceptionSystem.cs, Step 1 — QueryNearbyEntities)
// Called: Once per agent per heartbeat

// Query all agents within radius of centre position.
// Returns a list of AgentState references for agents within the radius,
// excluding the querying agent itself.
List<AgentState> spatialHash.QueryRadius(
    Vector2 centre,     // Observer's position
    float   radius      // MAX_PERCEPTION_RANGE = 120f [GT] (§3.10, PerceptionConstants.cs)
)

// Query all agents within radius, filtered to a specific team.
// Used by PressureEvaluator to find nearby opponents only.
List<AgentState> spatialHash.QueryRadius(
    Vector2 centre,
    float   radius,
    int     excludeTeamId   // Exclude this team — returns opponents only
)
```

**Constraints on the caller (Perception System):**
- `radius` passed to Step 1 query must equal `MAX_PERCEPTION_RANGE` — never a larger value
- `radius` passed to pressure query must equal `PRESSURE_RADIUS` (from First Touch §3.5, 
  value = 3.0m) — never a larger value
- The returned list must not be modified — read-only access to the reference
- The returned list is valid for the duration of the current heartbeat pipeline only;
  it must not be cached across heartbeats

**Why the spatial hash is used for pressure:** The pressure scalar requires all
opponents within `PRESSURE_RADIUS` = 3.0m of the observer. Using the same spatial hash
infrastructure is consistent with the project pattern established in First Touch (#4)
and Pass Mechanics (#5). Perception does not implement its own proximity search.

### 4.4.2 Pressure Query Contract

Perception calls `spatialHash.QueryRadius(observerPosition, PRESSURE_RADIUS, excludeTeamId)`
in `PressureEvaluator.cs` during Step 2 (before FoV test) to gather the opponent list
needed for pressure scalar computation. The returned opponent positions are passed
directly into the pressure formula from First Touch §3.5.

The pressure query is a separate call from the Step 1 candidate query. It uses a
smaller radius (`PRESSURE_RADIUS` = 3.0m vs `MAX_PERCEPTION_RANGE` = 120m) and
returns opponents only. These are two distinct responsibilities:

| Query | Step | Radius | Filter | Purpose |
|---|---|---|---|---|
| Step 1: candidate enumeration | 1 | 120m | None | FoV + occlusion candidates |
| Step 2: pressure evaluation | 2 | 3.0m | Opponents only | PressureScalar for FoV narrowing |

Both queries use the same `spatialHash` instance. The two calls occur sequentially
within the same heartbeat pipeline; no concurrency concern exists at Stage 0.

### 4.4.3 Write Prohibition

The Perception System **must never write to the Collision System spatial hash.**
Specifically:
- Perception must not call any spatial hash mutation method (insert, remove, update)
- Perception must not modify any `AgentState` reference returned by a query
- The spatial hash is a shared infrastructure resource; its integrity must be maintained

### 4.4.4 Failure Modes at This Boundary

| Failure Mode | ID | Detection | Behaviour |
|---|---|---|---|
| `spatialHash` reference is null | FM-CS-01 | Assert at system initialisation | Log error; abort match start |
| `QueryRadius()` returns null | FM-CS-02 | Null check post-call | Log warning; treat as empty candidate list; snapshot contains no visible agents |
| `QueryRadius()` returns agent with `AgentId` matching observer | FM-CS-03 | Filter by AgentId in PerceptionSystem | Skip self-entry silently; self is never a perception candidate |
| Pressure query radius exceeds spatial hash cell size | FM-CS-04 | Validated at startup against `PRESSURE_RADIUS` constant | If violated: log error and continue (pressure formula handles sparse results correctly) |

---

## 4.5 Output Contract: Decision Tree (#8)

Decision Tree Specification #8 has not yet been written. Per the project interface
principle: no `IDecisionTreeConsumer` or `IPerceptionPublisher` interface is defined
in this specification. The Decision Tree owns its own intake mechanism.

### 4.5.1 PerceptionSnapshot Ownership

`PerceptionSnapshot` is a value struct defined in `PerceptionSnapshot.cs` (§3.7.1),
which is owned exclusively by this specification. The canonical struct definition is:

```csharp
// Defined in: Perception System Specification #7, §3.7.1
// Owned by: Perception System Specification #7
// Consumer: Decision Tree Specification #8 (sole consumer at Stage 0)
// Modification authority: This specification only

public struct PerceptionSnapshot
{
    public int      ObserverId;                 // Agent this snapshot belongs to
    public int      HeartbeatTick;              // Heartbeat tick this snapshot is valid for
    public bool     BallVisible;                // Is ball currently in perception?
    public Vector2  BallPerceivedPosition;      // Last confirmed ball position (stale if invisible)
    public int      BallStalenessFrames;        // Ticks since ball last directly observed
    public PerceivedAgent[] VisibleTeammates;   // Confirmed visible teammates (L_rec elapsed)
    public PerceivedAgent[] VisibleOpponents;   // Confirmed visible opponents (L_rec elapsed)
    public float    EffectiveFoVAngle;          // Actual FoV angle after pressure degradation (degrees)
    public float    PressureScalar;             // [0–1]; stored for DT diagnostic use
    public bool     BlindSideWindowActive;      // True if shoulder check window open
    public float    BlindSideWindowExpiry;      // Match time (seconds) when window closes
    public bool     ForcedRefreshThisTick;      // True if this snapshot was triggered mid-heartbeat
}
```

This struct definition is the sole canonical version. The Decision Tree must not define
its own version, alias, or wrapper of this struct. Any field addition, removal, or type
change requires a version bump of this specification and a corresponding update to the
Decision Tree specification's intake code.

> **⚠ Amendment Required — Section 3 §3.7.1:** The `ForcedRefreshThisTick` field
> (final field above) is introduced in this section and is **not present** in the
> Section 3 §3.7.1 struct definition (Section 3 v1.1). This is an intentional
> cross-section inconsistency flagged here for resolution. Before Section 4 can be
> approved, Section 3 must be amended to add `ForcedRefreshThisTick` to the
> `PerceptionSnapshot` struct definition with the following annotation:
> `public bool ForcedRefreshThisTick; // True if this snapshot was triggered by a
> forced mid-heartbeat refresh (§3.8). False on normal heartbeat cadence.`
> This amendment does not affect any Section 3 formulas or models — it is a purely
> additive struct field. Section 3 version bump to v1.2 is required. This is tracked
> as cross-spec validation check XC-4.5-01.

### 4.5.2 Delivery Mechanism and Cadence

`PerceptionSnapshot` is passed **by value** to the Decision Tree each heartbeat.
No shared reference exists between the Perception System and the Decision Tree.
Once the struct is handed off, the Decision Tree's copy is independent. The
Perception System may immediately overwrite its working state for the next heartbeat.

**Delivery cadence:** One `PerceptionSnapshot` per agent per 10Hz heartbeat tick. All
22 agents' snapshots are assembled before any snapshot is delivered to any Decision
Tree. The ordering of delivery is deterministic: agent 0 through agent 21 in ascending
`AgentId` order.

**Forced refresh delivery:** If a qualifying event triggers a forced mid-heartbeat
refresh (§4.6), the affected agents receive a new snapshot immediately. This snapshot
is delivered outside the normal heartbeat cadence. The Decision Tree must be capable
of receiving a snapshot at any simulation frame, not only on heartbeat ticks. This
constraint must be documented in Decision Tree Specification #8 when written.

### 4.5.3 No Interface Defined Here

The mechanism by which the Decision Tree receives `PerceptionSnapshot` — callback,
event subscription, direct method call, shared buffer — is not defined in this
specification. That is the Decision Tree's architectural concern. This specification
defines only what is produced and when. Decision Tree Specification #8 defines how
it is consumed.

This follows the project interface principle: interfaces are written when both sides
are specified. As of the writing of this section, Decision Tree (#8) is not yet
specified. Therefore, no `IPerceptionConsumer`, `IHeartbeatReceiver`, or equivalent
interface is defined here.

### 4.5.4 Amendment Authority

Any change to `PerceptionSnapshot` or `PerceivedAgent` field definitions requires:
1. A formal amendment to this specification (version bump, changelog entry)
2. A corresponding notation in Decision Tree Specification #8's dependency table
3. A cross-specification validation check added to §4.8 of this section

The Decision Tree may not unilaterally modify the `PerceptionSnapshot` struct without
initiating an amendment through this specification. This is an ownership rule, not
a courtesy — `PerceptionSnapshot.cs` is not within the Decision Tree's source folder.

---

## 4.6 Forced Refresh Events

Certain physics-layer events warrant an immediate perception update for directly
involved agents, outside the normal 10Hz heartbeat schedule. This is the edge-triggered
refresh mechanism defined in KD-1 (Section 1.4) and specified in Section 3.8.

### 4.6.1 Qualifying Trigger Events

The following events qualify for forced mid-heartbeat refresh. This list is
authoritative and closed at Stage 0. Additional triggers require a formal amendment.

| Event | Source System | Agents Refreshed | Rationale |
|---|---|---|---|
| Ball contact (pass, shot, clearance) | Ball Physics (#1) via possession change | Kicking agent + any agent whose snapshot contains the ball | Ball state changes instantly; stale ball position would cause immediate mis-decision |
| Tackle completion | Collision System (#3) | Tackler + tackled agent | Both agents' awareness of each other changes discontinuously at tackle resolution |
| Possession change (non-contact, e.g., ball rolls to new agent) | Ball Physics (#1) | New possessing agent | New possessor needs immediate awareness of ball; existing snapshot is stale |

**Edge-triggered, not polled.** Qualifying events emit a signal to the Perception
System. The Perception System does not poll physics state on every frame looking for
changes. The event signal is the trigger; the Perception System runs a partial pipeline
(Steps 1–6) for affected agents only on receipt.

**Stage 0 coupling note — known architectural shortcut:** At Stage 0, qualifying events
reach the Perception System via a **direct method call** on the `PerceptionSystem`
instance. This means Ball Physics and the Collision System hold a reference to
`PerceptionSystem`, which is an upward dependency — a lower-layer system referencing
a higher-layer system. This is an acknowledged coupling that exists only because Event
System (#17) is not yet implemented. It does not reflect the target architecture. At
Stage 1, this is replaced by Event System subscription: Ball Physics and Collision
System publish to the event bus; Perception System subscribes. No direct cross-layer
reference will exist at Stage 1. The `PerceptionRefreshEvent` struct defined in §4.6.3
is already in the correct form for that transition.

**All other agents unaffected.** A ball contact event triggers refresh for involved
agents only. The remaining 20 agents continue on the normal 10Hz schedule. There is
no global perception flush on any qualifying event.

### 4.6.2 Forced Refresh Scope

A forced refresh runs the full perception pipeline (Steps 1–6) for the affected agent.
It is not a partial update — the full snapshot is regenerated.

**Recognition latency on forced refresh:** On forced refresh, `L_rec` is set to 0
for the directly triggering entity only (§3.8.2). For all other entities in the new
snapshot, the existing latency counters continue from their current values. This prevents
a forced refresh from wiping accumulated recognition progress for unrelated entities.

For example: a tackle completion triggers refresh for the tackler. The tackler's
recognition counter for the ball is reset to 0 (ball position is now fresh). Their
recognition counter for a nearby runner who was already at L_rec tick 3 out of 5
remains at 3 — the tackle does not restart awareness of unrelated entities.

### 4.6.3 PerceptionRefreshEvent Stub

A stub event struct is defined for the forced refresh signal. This stub serves two
purposes at Stage 0: (1) it documents the data contract for when Event System (#17)
is written, and (2) it allows the physics-layer trigger to call a typed method rather
than a raw callback.

```csharp
// Defined in: PerceptionEvents.cs
// Published by: Physics layer (Ball Physics, Collision System) on qualifying events
// Consumed by: PerceptionSystem.cs (forced refresh handler)
// Stage 0: Received via direct method call on PerceptionSystem instance.
// Stage 1: Consumed via Event System (#17) subscription.

public struct PerceptionRefreshEvent
{
    public RefreshTrigger   Trigger;            // Enum: BALL_CONTACT, TACKLE_COMPLETION,
                                                //       POSSESSION_CHANGE
    public int              PrimaryAgentId;     // First agent to refresh (always populated)
    public int              SecondaryAgentId;   // Second agent to refresh (-1 if none)
    public int              TriggeringEntityId; // Entity whose L_rec resets to 0 (-1 if none)
    public int              SimulationFrame;    // Physics frame at which event occurred
    public float            MatchTime;          // Match time in seconds
}

public enum RefreshTrigger
{
    BALL_CONTACT,           // Pass, shot, clearance, header contact
    TACKLE_COMPLETION,      // Collision System tackle resolution complete
    POSSESSION_CHANGE       // Ball possession changes without direct contact
}
```

**`AGENT_ID_NONE` sentinel:** `SecondaryAgentId = -1` when only one agent requires
refresh. `TriggeringEntityId = -1` when no specific entity warrants L_rec reset
(e.g., possession change from rolling ball — no single entity caused the event).

### 4.6.4 Events Not Published by This System

The following events are related to perception but are explicitly **not** published
by the Perception System. Their publishers are documented for traceability.

| Event | Actual Publisher | Trigger |
|---|---|---|
| `ShoulderCheckAnimEvent` | Animation System (Stage 1+) | Stub defined in `PerceptionSnapshot.ShoulderCheckAnimData`; not published at Stage 0 |
| `AgentPerceivedEvent` | Not defined at Stage 0 | Would fire when an agent enters another's awareness; deferred indefinitely |
| Heartbeat tick trigger | Simulation Scheduler (not yet specified) | Heartbeat scheduler owns the 10Hz tick; it calls PerceptionSystem.OnHeartbeat(), not vice versa |

The Perception System publishes exactly one event type at Stage 0: `PerceptionRefreshEvent`
on forced mid-heartbeat refresh. It does not publish on normal heartbeat completion.

### 4.6.5 Stage 0 Stub Implementation

The Stage 0 Event Bus stub is a no-op receiver consistent with the pattern established
in Pass Mechanics §4.6.3 and Shot Mechanics §4.7.3.

```csharp
// Stage 0 stub — replace with real Event System (#17) at Stage 1
// Pattern: identical to PassEvents and ShotEvents stubs

public static class EventBusStub
{
    // Accepts all events. Performs no dispatch. Validates struct is non-default.
    // In DEVELOPMENT builds: logs event type to console.
    // In RELEASE builds: compiles to no-op.
    //
    // NOTE: No reflection used. Frame logging is omitted deliberately — reflection
    // is fragile, inconsistent with the project's determinism philosophy, and
    // not needed for a no-op stub. If frame logging is required in development,
    // add a typed overload for each event struct rather than using GetField().

    public static void Publish<T>(T evt) where T : struct
    {
    #if DEVELOPMENT_BUILD
        Debug.Log($"[EventBus STUB] {typeof(T).Name} published");
    #endif
        // No dispatch. Event is discarded.
    }
}
```

This stub must be replaced — not removed — at Stage 1. If Event System (#17) requires
a different publish signature, this specification requires a corresponding amendment
before the replacement is merged.

---

## 4.7 Dependency Graph

The complete dependency graph for the Perception System, showing direction of dependency
(arrows point from dependent to dependency):

```
                    ┌──────────────────────────────────┐
                    │      Decision Tree (#8)           │
                    │      [CONSUMER — not yet written] │
                    └──────────────────┬───────────────┘
                                       │ PerceptionSnapshot (by value, per heartbeat)
                                       │
                    ┌──────────────────▼───────────────┐
                    │     PERCEPTION SYSTEM (#7)        │
                    │     [THIS SYSTEM]                 │
                    └──┬──────────┬──────────┬──────────┘
                       │          │          │
                   reads       reads      reads +
                   AgentState  BallState  spatialHash.QueryRadius()
                       │          │          │
          ┌────────────▼──┐  ┌────▼──────┐  ┌▼──────────────────┐
          │ Agent Movement│  │Ball Physics│  │ Collision System  │
          │    (#2)       │  │   (#1)     │  │      (#3)         │
          │ [READ ONLY]   │  │[READ ONLY] │  │ [READ ONLY]       │
          └───────────────┘  └───────────┘  └───────────────────┘
                                                       │
                                   receives PerceptionRefreshEvent
                                   (edge-triggered, forced refresh)
                                                       │
                    ┌──────────────────────────────────▼
                    │     Event System (#17) [STUB]     │
                    │     [NO-OP at Stage 0]             │
                    └───────────────────────────────────┘
```

**Key observations from this graph:**

1. The Perception System has **no circular dependencies** with any Stage 0 specification.
2. Perception is a pure consumer of three upstream systems; it writes to none of them.
3. Decision Tree (#8) is the sole consumer. No other Stage 0 system reads `PerceptionSnapshot`.
4. Forced refresh signals travel from physics layer to Perception (Ball Physics, Collision
   System → Perception). This is a downward signal in the cognitive stack, not upward.
5. The Event System relationship is one-directional. Perception publishes; it never
   subscribes to any event from any Stage 0 system.

---

## 4.8 Cross-Specification Validation Checks

Before Section 4 can be approved, the following cross-specification checks must be
completed and signed off. Each check identifies a value or interface in another
specification that must be verified for consistency with this section.

| Check ID | What to Verify | Source Location | Target Location | Blocking? |
|---|---|---|---|---|
| XC-4.2-01 | `AgentState.FacingDirection` is `Vector2` (not `Vector3`) — 2D confirmed for Stage 0 | Agent Movement §3.5.3 | Perception §4.2.1, FovCalculator.cs | ✅ Blocking |
| XC-4.2-02 | Possession state source confirmed: `AgentState` does NOT contain `HasPossession` (Option B, ERR-008). Confirm that simulation scheduler supplies `bool hasPossession` to `ShoulderCheckScheduler` at pipeline start, and that the external possession tracker's interface is documented when fully specified | Ball Physics §3.1.11 (Option B) | Perception §4.2.1, ShoulderCheckScheduler.cs | ⚠ Non-blocking at Stage 0 (bool parameter accepted); blocking before possession system is formally specified |
| XC-4.2-03 | `PlayerAttributes.Decisions` type is `int` [1–20] — not float | Agent Movement §3.5.6 | Perception §4.2.2, RecognitionLatencyTracker.cs | ✅ Blocking |
| XC-4.2-04 | `PlayerAttributes.Anticipation` type is `int` [1–20] — not float | Agent Movement §3.5.6 | Perception §4.2.2, ShoulderCheckScheduler.cs | ✅ Blocking |
| XC-4.3-01 | `BallState.Position` is `Vector3` (confirmed 3D in Ball Physics — 2D projection required) | Ball Physics §3.1.1 | Perception §4.3.1, BallPerceptionEvaluator.cs | ✅ Blocking |
| XC-4.4-01 | `spatialHash.QueryRadius(Vector2, float)` call signature matches §4.4.1 exactly | Collision System §3.1.4 | Perception §4.4.1, PerceptionSystem.cs Step 1 | ✅ Blocking |
| XC-4.4-02 | `spatialHash.QueryRadius(Vector2, float, int)` overload exists for team-filtered queries | Collision System §3.1.4 | Perception §4.4.2, PressureEvaluator.cs | ✅ Blocking — may require Collision System amendment if overload not present |
| XC-4.4-03 | `PRESSURE_RADIUS` value in `PerceptionConstants.cs` matches First Touch §3.5 authoritative value (3.0m) | First Touch §3.5.1 | Perception `PerceptionConstants.cs` [CROSS] tag | ✅ Blocking |
| XC-4.5-01 | `PerceptionSnapshot` struct field count matches between §3.7.1 (Section 3) and §4.5.1 (this section). **Currently mismatched:** §4.5.1 introduces `ForcedRefreshThisTick` (12th field) which is absent from Section 3 §3.7.1. Section 3 must be amended to v1.2 before Section 4 approval. | Perception §3.7.1 (v1.1) | Perception §4.5.1 | ✅ Blocking — Section 3 amendment required |
| XC-4.6-01 | Ball Physics emits a detectable signal on possession change compatible with `PerceptionRefreshEvent.POSSESSION_CHANGE` trigger | Ball Physics §3.1.11 (possession model) | Perception §4.6.1, §4.6.3 | ⚠ Non-blocking at Stage 0 (stub accepts direct call); blocking before Stage 1 Event System integration |
| XC-4.6-02 | Collision System emits a detectable signal on tackle completion compatible with `PerceptionRefreshEvent.TACKLE_COMPLETION` trigger | Collision System §4 (event model) | Perception §4.6.1, §4.6.3 | ⚠ Non-blocking at Stage 0; blocking before Stage 1 |

**Status key:** ✅ = must be resolved before Section 4 approval. ⚠ = must be resolved
before Stage 1 Event System integration; non-blocking for Stage 0 stub operation.

**Checks XC-4.4-02 note:** If the Collision System's `QueryRadius()` does not have a
team-filter overload, `PressureEvaluator.cs` must implement client-side team filtering
on the unfiltered result. This is a fallback, not a preference. The team-filter overload
is the preferred approach for consistency with the pressure model pattern established
in First Touch (#4) and Pass Mechanics (#5). A Collision System amendment should be
requested if the overload does not exist.

---

## 4.9 Version History

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0 | February 25, 2026, 12:00 PM PST | Claude (AI) / Anton | Initial draft. All four integration contracts defined. File structure (9 source files, 8 test files). Forced refresh event model formalised. PerceptionRefreshEvent stub defined. Dependency graph complete. Cross-spec validation table (11 checks). |
| 1.1 | February 26, 2026 | Claude (AI) / Anton | Four fixes: (1) Removed `AgentState.HasPossession` — field does not exist; replaced with Option B external possession tracker pattern (XC-4.2-02 updated). (2) Removed reflection-based `GetFrame<T>()` from EventBusStub — replaced with simple type-name log to preserve determinism philosophy. (3) Added explicit Stage 0 coupling note to §4.6.1 documenting that direct-call cross-layer reference is a known shortcut pending Event System (#17). (4) Added amendment warning to §4.5.1 flagging that `ForcedRefreshThisTick` field is absent from Section 3 §3.7.1 — Section 3 v1.2 amendment required before approval; XC-4.5-01 updated to blocking. |

---

*End of Section 4 — Perception System Specification #7*  
*Tactical Director — Specification #7 of 20 | Stage 0: Physics Foundation*
