# Pass Mechanics Specification #5 — Section 4: Architecture & Integration

**File:** `Pass_Mechanics_Spec_Section_4_v1_0.md`
**Purpose:** Defines the complete file/module architecture for the Pass Mechanics system
and specifies all integration contracts with dependent specifications — Ball Physics (#1),
Agent Movement (#2), Collision System (#3), First Touch (#4), and Event System (#17).
This section is the authoritative reference for boundary ownership and interface signatures.
Implementers must not cross these boundaries without a formal amendment.

**Created:** February 20, 2026, 9:00 PM PST
**Version:** 1.0
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 5 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)
**Prerequisites:** Section 1 (v1.0), Section 2 (v1.0), Section 3.1 (v1.1)

**Open Dependency Flags:**
- `[ERR-007-PENDING]` — `KickPower`, `WeakFootRating`, `Crossing` absent from
  `PlayerAttributes`; amendment AM-002-001 drafted, pending approval. Affects §4.3.
- `[ERR-008-PENDING]` — `PossessingAgentId` design (Option A vs B) unresolved in
  `BallState`. Affects §4.2 possession-loss contract. Both options documented below.

---

## Table of Contents

- [4.1 File Structure](#41-file-structure)
- [4.2 Integration with Ball Physics (#1)](#42-integration-with-ball-physics-1)
  - [4.2.1 ApplyKick() Interface Contract](#421-applykick-interface-contract)
  - [4.2.2 BallState Read Contract](#422-ballstate-read-contract)
  - [4.2.3 Possession Loss on ApplyKick()](#423-possession-loss-on-applykick)
  - [4.2.4 Failure Modes at This Boundary](#424-failure-modes-at-this-boundary)
- [4.3 Integration with Agent Movement (#2)](#43-integration-with-agent-movement-2)
  - [4.3.1 PlayerAttributes Read Contract](#431-playerattributes-read-contract)
  - [4.3.2 AgentState Read Contract](#432-agentstate-read-contract)
  - [4.3.3 Write Prohibition](#433-write-prohibition)
- [4.4 Integration with Collision System (#3)](#44-integration-with-collision-system-3)
  - [4.4.1 Pressure Query Contract](#441-pressure-query-contract)
  - [4.4.2 Tackle Interrupt Contract](#442-tackle-interrupt-contract)
  - [4.4.3 Interface Form Decision](#443-interface-form-decision)
- [4.5 Integration with First Touch (#4)](#45-integration-with-first-touch-4)
  - [4.5.1 Implicit Connection via Ball State](#451-implicit-connection-via-ball-state)
  - [4.5.2 Cross-Spec Event Chain](#452-cross-spec-event-chain)
- [4.6 Integration with Event System (#17)](#46-integration-with-event-system-17)
  - [4.6.1 Events Published by Pass Mechanics](#461-events-published-by-pass-mechanics)
  - [4.6.2 Events Not Published by Pass Mechanics](#462-events-not-published-by-pass-mechanics)
  - [4.6.3 Stage 0 Stub Implementation](#463-stage-0-stub-implementation)
- [4.7 Dependency Graph](#47-dependency-graph)
- [4.8 Cross-Specification Validation Checks](#48-cross-specification-validation-checks)
- [4.9 Version History](#49-version-history)

---

## 4.1 File Structure

The Pass Mechanics module occupies its own folder within the simulation scripts directory.
All files are listed with their responsibilities. No other specification may add files to
this folder without a formal amendment to this section.

```
/Scripts/PassMechanics/
│
│   PassExecutor.cs
│       Main orchestrator. Owns the pass execution state machine
│       (IDLE → INITIATING → WINDUP → CONTACT → FOLLOW_THROUGH → COMPLETE).
│       Entry point for all external callers. Validates PassRequest, coordinates
│       sub-system calls, invokes Ball.ApplyKick() at CONTACT state.
│       Publishes PassAttemptEvent and PassCancelledEvent.
│
│   PassVelocityCalculator.cs
│       Computes scalar kick speed (§3.2), launch angle (§3.3), and spin vector (§3.4).
│       Reads PhysicalProfile from PassTypeProfiles. Pure calculation — no side effects.
│       Static class. All methods are deterministic given identical inputs.
│
│   PassErrorCalculator.cs
│       Computes the deterministic error vector (§3.5) from agent attributes,
│       pressure, fatigue, orientation, and urgency. Applies weak foot multiplier
│       when PassRequest.IsWeakFoot = true. Pure calculation — no side effects.
│       Static class. Must never call System.Random.
│
│   PassTargetResolver.cs
│       Resolves player-targeted passes (§3.6) and space-targeted passes (§3.6).
│       Performs linear receiver projection for through ball lead distance (§3.6).
│       Applies error vector to produce final aim direction.
│       Static class. All methods are deterministic.
│
│   PassTypeProfiles.cs
│       Read-only physical profile constants per pass type. Keyed on PassType enum.
│       Each profile defines: VelocityMin, VelocityMax, LaunchAngleMin,
│       LaunchAngleMax, DominantSpinType, SpinMagnitudeRange.
│       All values are [GAMEPLAY-TUNABLE]. Loaded once at match initialisation.
│       Source of truth for cross sub-type discrimination (Flat/Whipped/High crosses).
│
│   PassConstants.cs
│       All gameplay-tunable scalar constants. Every constant is annotated
│       [GAMEPLAY-TUNABLE] or [PHYSICS-DERIVED] and includes its derivation source.
│       No magic numbers elsewhere in the module.
│
│   PassRequest.cs
│       PassRequest struct definition. Owned by this specification. Decision Tree (#8)
│       instantiates and populates it before calling PassExecutor.Execute().
│       This file is the authoritative struct definition — Decision Tree must not
│       define its own version.
│
│   PassResult.cs
│       PassResult struct definition. Returned by PassExecutor.Execute() to the caller.
│       Contains: Outcome (enum), ErrorAngleDeg (float), FinalVelocity (Vector3),
│       FinalSpin (Vector3), TargetPosition (Vector3), Frame (int).
│
│   PassType.cs
│       PassType enum (7 values), CrossSubType enum (3 values),
│       PassOutcome enum (Valid, Cancelled, Invalid).
│
│   PassEvents.cs
│       PassAttemptEvent and PassCancelledEvent struct definitions.
│       Stage 0: published to no-op stub. Stage 1: published to Event System (#17).
│
└───/Tests/
        PassVelocityTests.cs        — Unit tests for §3.2, §3.3, §3.4 (PV-*, LA-*, SV-*)
        PassErrorTests.cs           — Unit tests for §3.5 error model (PE-*)
        PassTargetTests.cs          — Unit tests for §3.6 target resolution (TR-*)
        PassWeakFootTests.cs        — Unit tests for §3.7 weak foot model (WF-*)
        PassStateMachineTests.cs    — Unit tests for §3.8 state machine (PSM-*)
        PassIntegrationTests.cs     — Integration tests §5.3 (IT-*)
```

**Ownership rule:** `PassRequest.cs` is defined here. Decision Tree (#8) is the caller —
it instantiates `PassRequest` but does not define the struct. If Decision Tree requires
additional fields on `PassRequest`, that amendment must originate in this specification,
not in Decision Tree's specification.

---

## 4.2 Integration with Ball Physics (#1)

Ball Physics is the primary downstream dependency. Pass Mechanics calls exactly one
Ball Physics interface — `Ball.ApplyKick()` — and reads `BallState` once at the start
of each pass execution for a possession confirmation check. No other Ball Physics
interfaces are touched.

### 4.2.1 ApplyKick() Interface Contract

The authoritative `ApplyKick()` signature is defined in Ball Physics §3.1.11.2
(Amendment AM-001-001, pending ERR-008 resolution):

```csharp
// Defined in: Ball Physics Specification #1, §3.1.11.2
// Owner: Ball Physics
// Caller: Pass Mechanics (PassExecutor.cs, CONTACT state only)
// Called: Exactly once per pass execution. Never called during WINDUP, FOLLOW_THROUGH,
//         or any other state.

void Ball.ApplyKick(
    ref BallState ball,         // Ball state, modified in-place by Ball Physics
    Vector3 velocity,           // Full 3D velocity vector (m/s). Elevation encoded in Z.
    Vector3 spin,               // Angular velocity vector (rad/s). Magnus input.
    int agentId,                // ID of the kicking agent. Used for possession handoff.
    float matchTime             // Match time at kick (seconds from kickoff). For event records.
)
```

**Constraints on the caller (Pass Mechanics):**

1. `ApplyKick()` must be called **exactly once** per pass execution, at `CONTACT` state.
   Calling more than once for the same pass is a specification violation.
2. Calling with `velocity == Vector3.zero` is **forbidden**. If a pass is cancelled,
   use `PassOutcome.Cancelled` and do not call `ApplyKick()`.
3. `velocity.magnitude` must be within the physical profile's `[VMin, VMax]` range
   (clamped by `PassVelocityCalculator` before the call — see §3.2).
4. `spin` values must have been validated against Ball Physics Magnus constants
   (cross-spec validation check XC-4.2-01 below).
5. `agentId` must match the agent whose possession was confirmed in step 1 of the
   execution pipeline (§2.2.3).

**No `KickType` parameter.** The pass type is fully encoded in the velocity and spin
vectors. Ball Physics does not need intent labels to simulate correct aerodynamics.
This is KD-3 (Section 1.3) and the resolution of ERR-005 (Spec Error Log v1.0).
If any future section of this specification or any other specification references a
`KickType` parameter on `ApplyKick()`, that reference is in error and must be corrected.

### 4.2.2 BallState Read Contract

Pass Mechanics reads `BallState` once, at `INITIATING` state, to confirm the executing
agent has possession before proceeding to `WINDUP`.

```csharp
// Read location: PassExecutor.cs, ValidateRequest() method, INITIATING state
// Purpose: Confirm possession before committing to WINDUP

BallState ball = BallSystem.GetCurrentState();

// Possession check logic depends on ERR-008 resolution (see §4.2.3):
// Option A: ball.PossessingAgentId == request.AgentId
// Option B: PossessionTracker.GetPossessor() == request.AgentId
```

No other `BallState` fields are read by Pass Mechanics. Ball trajectory, angular
velocity, and flight state are irrelevant to pass initiation and must not be accessed.

### 4.2.3 Possession Loss on ApplyKick()

The mechanism by which the executing agent loses possession when `ApplyKick()` is called
depends on the unresolved `ERR-008` design decision. Both options are documented here.
Section 4 cannot be finalised until ERR-008 is resolved.

**Option A — Possession field on BallState:**

```
BallState.PossessingAgentId is set to -1 (no possessor) by Ball Physics inside
ApplyKick(), after applying the velocity and spin vectors. Pass Mechanics does not
write to BallState.PossessingAgentId directly. Ball Physics owns that transition.
```

Implication for Pass Mechanics: no additional action required after `ApplyKick()`.
Possession release is a side effect of the call.

**Option B — External possession tracking:**

```
A PossessionTracker singleton (or equivalent) is notified by Ball Physics after
ApplyKick() to release the executing agent's possession. Pass Mechanics does not
call PossessionTracker directly. Ball Physics is responsible for the notification.
```

Implication for Pass Mechanics: same as Option A — no additional action required.
The ownership of the notification stays inside Ball Physics in both options.

**`[ERR-008-PENDING]`** — Until this decision is made, PassExecutor.cs must be
written with a clear comment at the possession-confirmation step indicating that the
check logic must be updated once ERR-008 is resolved. The check must not be hardcoded
to either option.

### 4.2.4 Failure Modes at This Boundary

| ID | Scenario | Detection | Action |
|----|----------|-----------|--------|
| FM-01 | Agent does not have possession when `ValidateRequest()` runs | Possession check (§4.2.2) | Reject `PassRequest`. Return `PassOutcome.Invalid`. Log warning with `agentId` and `frame`. Do not proceed to WINDUP. |
| FM-02 | `ApplyKick()` call produces NaN or Infinity in `BallState` | Post-call `float.IsNaN()` / `float.IsInfinity()` check on resulting `ball.Velocity` | Log error. Cancel pass. Flag for QA investigation. This indicates a physics parameter error upstream. |
| FM-08 | `ApplyKick()` called when agent no longer has possession (race condition) | Possession re-check immediately before `ApplyKick()` call at CONTACT | Log error. Cancel pass. This should not be possible if tackle interrupt handling (§4.4.2) is correct. |

---

## 4.3 Integration with Agent Movement (#2)

Pass Mechanics is a **read-only** consumer of Agent Movement data. It reads agent
attributes and state at pass initiation. It never writes to Agent Movement state —
possession loss is handled by Ball Physics (§4.2.3), not by a direct write to agent
locomotion state.

### 4.3.1 PlayerAttributes Read Contract

Attributes are read once at `INITIATING` state, before WINDUP begins. They are captured
at this point and held constant for the duration of the execution pipeline. This ensures
determinism — a mid-pass attribute change (impossible in Stage 0, possible in Stage 3+)
cannot corrupt the in-flight calculation.

```csharp
// Read location: PassExecutor.cs, INITIATING state (before WINDUP)
// Source: Agent Movement Specification #2, §3.5.6
// Owner of these fields: Agent Movement Specification #2

PlayerAttributes attrs = AgentSystem.GetAttributes(request.AgentId);

// Fields consumed by Pass Mechanics (all required):
int  Passing;           // [1–20]  Primary accuracy attribute. §3.5 error model.
int  Technique;         // [1–20]  Secondary accuracy; proxy for Vision (KD-7). §3.5, §3.6.
int  KickPower;         // [1–20]  Primary velocity attribute. §3.2.          [ERR-007-PENDING]
int  WeakFootRating;    // [1–5]   Weak foot penalty magnitude. §3.7.          [ERR-007-PENDING]
int  Crossing;          // [1–20]  Cross accuracy attribute. §3.5 (cross type). [ERR-007-PENDING]
float Fatigue;          // [0–1]   Fatigue scalar. Applied to velocity and error. §3.2, §3.5.
```

**`[ERR-007-PENDING]`** — `KickPower`, `WeakFootRating`, and `Crossing` are not yet
defined in `PlayerAttributes` (Agent Movement §3.5.6). Amendment AM-002-001 adds them.
Section 3 and this section use these field names as specified in AM-002-001. If
AM-002-001 is revised with different field names, this section requires a corresponding
amendment.

**Fallback behaviour pending ERR-007 resolution:** If AM-002-001 is not approved before
implementation begins, the following proxies apply as temporary measure only — they
must not be shipped to any milestone build:

| Missing Attribute | Temporary Proxy | Source |
|---|---|---|
| `KickPower` | `(Passing + Technique) / 2` | Reasonable correlation; not physically grounded |
| `WeakFootRating` | `3` (mid-scale fixed value) | Conservative default — assumes average weak foot |
| `Crossing` | `Passing` | Acceptable proxy; cross accuracy correlates with passing |

These proxies must be marked `[TEMPORARY-PROXY-ERR-007]` in code and flagged in the
Section 9 Approval Checklist as an open item.

### 4.3.2 AgentState Read Contract

Agent state is read at the same time as attributes — once at `INITIATING` state,
captured and held constant for the execution pipeline.

```csharp
// Read location: PassExecutor.cs, INITIATING state
// Source: Agent Movement Specification #2, §3.5.3
// Owner of these fields: Agent Movement Specification #2

AgentState state = AgentSystem.GetState(request.AgentId);

// Fields consumed by Pass Mechanics (all required):
Vector3 Position;           // World position of executing agent at pass initiation.
                            // Used: target distance calculation (§3.2), pressure query origin (§4.4.1).
Vector3 Velocity;           // Agent velocity at pass initiation.
                            // Used: orientation bonus computation (§3.5).
Vector2 FacingDirection;    // XY-plane facing unit vector.
                            // Used: body orientation penalty (§3.5).
                            // Note: 2D — Z component not used in Stage 0.
MovementState MovementState; // STANDING, RUNNING, etc.
                            // Not currently used in Stage 0 calculations.
                            // Reserved as Stage 2 extension hook (§7.2).
```

**Receiver state (for through ball lead calculation):**

When `PassRequest.TargetType == TargetType.Agent` and `PassRequest.PassType == PassType.ThroughBall`,
Pass Mechanics additionally reads the receiver's state:

```csharp
AgentState receiverState = AgentSystem.GetState(request.TargetAgentId);
Vector3 ReceiverPosition; // Current receiver position.
Vector3 ReceiverVelocity; // Current receiver velocity. Used for linear projection (KD-4).
```

No other agent's state is read. The pressure query (§4.4.1) uses the Collision System
spatial hash — Pass Mechanics does not iterate over all agents itself.

### 4.3.3 Write Prohibition

Pass Mechanics writes **nothing** to Agent Movement state. The following responsibilities
are explicitly confirmed as **not** belonging to Pass Mechanics:

- Setting agent locomotion state to `FOLLOW_THROUGH` — this is an Animation System
  concern (Stage 1). Pass Mechanics populates `PassAnimationData` as a passive data
  struct; it does not command animation.
- Removing possession from the executing agent — this is Ball Physics' responsibility
  inside `ApplyKick()` (§4.2.3).
- Modifying `Fatigue` — read-only. Fatigue changes are governed by Agent Movement's
  own energy depletion model.

---

## 4.4 Integration with Collision System (#3)

Pass Mechanics interacts with the Collision System for two distinct purposes: (1) a
one-shot pressure query at `CONTACT` state to compute the pressure scalar for the error
model, and (2) a tackle interrupt signal that can cancel a pass during `WINDUP` state.
These are separate interfaces with different timing and ownership.

### 4.4.1 Pressure Query Contract

The pressure query is a **read-only** call to the Collision System's spatial hash.
It is called once, at `CONTACT` state, immediately before the error vector is computed.

```csharp
// Read location: PassExecutor.cs, CONTACT state, before PassErrorCalculator.Compute()
// Source: Collision System Specification #3, §3.1.4 (SpatialHash.QueryRadius)
// Owner: Collision System
// Pass Mechanics does not implement spatial hashing — it queries the existing hash.

List<AgentId> nearbyOpponents = SpatialHash.QueryRadius(
    center:  state.Position,
    radius:  PassConstants.PRESSURE_RADIUS_MAX,     // [GAMEPLAY-TUNABLE] — see §3.5
    filter:  QueryFilter.OpponentsOnly              // Same-team agents excluded
);

// Result fed to PassErrorCalculator as opponent count or nearest distance scalar.
// See §3.5 for pressure scalar derivation from this list.
```

**Timing constraint:** The query runs at `CONTACT` state, not at `INITIATING`. This
captures pressure at the moment of ball contact, not at the moment the agent decided
to pass. The distinction matters: a player who steps away from pressure during `WINDUP`
passes with less error than if pressure were sampled at decision time. This is the
physically correct model.

**`PRESSURE_RADIUS_MAX`** is defined in `PassConstants.cs` as a `[GAMEPLAY-TUNABLE]`
constant. Its value must be cross-validated against the Collision System's spatial
hash cell size — if `PRESSURE_RADIUS_MAX` exceeds the cell size, the query may miss
opponents in adjacent cells. Cross-spec validation check XC-4.4-01 covers this.

### 4.4.2 Tackle Interrupt Contract

A tackle interrupt is a signal from the Collision System to Pass Mechanics indicating
that a physical tackle contact has been made on the executing agent during `WINDUP`
state. If the interrupt is received, the pass is cancelled.

**Interface form — resolved decision:**

The tackle interrupt uses a **polling flag**, not a callback. At the start of each
simulation tick during `WINDUP` state, `PassExecutor` polls:

```csharp
// Polled location: PassExecutor.cs, start of each simulation tick during WINDUP
// Source: Collision System Specification #3
// Note: Collision System sets this flag; Pass Mechanics reads and clears it.

bool tackled = CollisionSystem.GetAndClearTackleFlag(agentId: request.AgentId);
if (tackled)
{
    // Transition WINDUP → IDLE
    // Set result.Outcome = PassOutcome.Cancelled
    // Publish PassCancelledEvent (see §4.6.1)
    // Do NOT call Ball.ApplyKick()
    return;
}
```

**Rationale for polling over callback:** A callback from the Collision System into
Pass Mechanics would invert the dependency direction — the Collision System would need
to know about Pass Mechanics to invoke it. A flag on the Collision System's agent
record keeps the dependency one-directional: Pass Mechanics depends on Collision System,
not the reverse. This is consistent with the consumer-defines-interface principle
established in this project.

**Ownership of the flag:** The Collision System owns and sets the flag. Pass Mechanics
reads and clears it. The flag is per-agent, not global. The Collision System's
specification must document this flag in its §4 (Data Structures) as a required field.

> ⚠ **Cross-spec action required:** Collision System Specification #3 must add a
> `TackleContactFlag` (or equivalent) to its agent record data structure and document
> the set/clear protocol. This is flagged as XC-4.4-02 below.

**Out-of-sequence tackle (FM-06):** If a tackle interrupt arrives after `CONTACT` state
(i.e., `ApplyKick()` has already been called), the flag is cleared and the interrupt
is ignored. A CONTACT-state pass is irreversible. The tackle is logged for analytics.
See FM-06 (Section 2.6).

### 4.4.3 Interface Form Decision

The polling-flag decision (§4.4.2) is recorded here as an architectural commitment.
Changing to a callback model requires a formal amendment to both this specification
and Collision System Specification #3. The primary reason for preferring polling is
dependency direction hygiene — callbacks create bidirectional dependencies that become
difficult to reason about as the specification count grows toward 20.

---

## 4.5 Integration with First Touch (#4)

Pass Mechanics has **no direct interface with First Touch**. There is no function call
from Pass Mechanics into First Touch and no function call from First Touch into Pass
Mechanics. The connection is implicit, mediated entirely through the ball state.

### 4.5.1 Implicit Connection via Ball State

The full data flow at the pass/reception boundary is:

```
Pass Mechanics
    └── calls Ball.ApplyKick(velocity, spin, agentId, matchTime)
            │
            ▼
    Ball Physics
        └── updates BallState (new velocity, trajectory, possession cleared)
        └── begins ball flight simulation
            │
            ▼
    [ball in flight — n frames]
            │
            ▼
    Collision System
        └── detects agent-ball contact (receiving agent)
        └── calls Ball.OnCollision(AgentBallCollisionData)
            │
            ▼
    First Touch (#4)
        └── evaluates control quality, resolves touch outcome
        └── publishes FirstTouchEvent
        └── publishes PassCompletedEvent (or PassInterceptedEvent via Ball Physics)
```

Pass Mechanics initiates this chain and exits. It has no knowledge of what follows.
The chain is observable through the event system (§4.6) and through `BallState`
changes, but Pass Mechanics does not listen for either after publishing
`PassAttemptEvent`.

### 4.5.2 Cross-Spec Event Chain

The cross-specification event chain for a complete pass is:

| Frame | Event | Publisher | Consumer |
|-------|-------|-----------|----------|
| N (CONTACT) | `PassAttemptEvent` | Pass Mechanics | Statistics Engine (Stage 1), Replay System (Stage 1) |
| N+k (reception) | `FirstTouchEvent` | First Touch (#4) | Statistics, Replay, UI |
| N+k (reception) | `PassCompletedEvent` | First Touch (#4) | Statistics, Decision Tree |
| N+j (intercept, if any) | `PassInterceptedEvent` | Ball Physics (#1) | Statistics, Decision Tree |
| N (cancel) | `PassCancelledEvent` | Pass Mechanics | Statistics, Decision Tree |

Pass Mechanics is responsible only for the events in rows 1 and 5. It must not
attempt to publish `PassCompletedEvent` or `PassInterceptedEvent` — those are
downstream responsibilities confirmed by KD-8 (Section 1.3).

---

## 4.6 Integration with Event System (#17)

The Event System is not written at Stage 0. Pass Mechanics publishes events to a
**no-op stub** in Stage 0. The stub accepts calls with the correct struct signatures
and discards them. This allows the event publishing code to be written and tested in
Stage 0 without requiring Event System (#17) to exist.

### 4.6.1 Events Published by Pass Mechanics

Pass Mechanics publishes exactly two events. Both are defined in `PassEvents.cs`.

---

**`PassAttemptEvent`**

Published at: `CONTACT` state, immediately after `Ball.ApplyKick()` returns.

```csharp
// Defined in: PassEvents.cs
// Published by: PassExecutor.cs
// Consumed by: Statistics Engine (Stage 1), Replay System (Stage 1)

public struct PassAttemptEvent
{
    public int     AgentId;          // ID of the executing agent
    public int     TeamId;           // Team of the executing agent
    public PassType PassType;        // Ground, Driven, Lofted, ThroughBall, Cross, Chip
    public CrossSubType CrossSubType; // Flat / Whipped / High (only relevant for Cross type)
    public Vector3 AimPosition;      // Final intended target after error applied
    public Vector3 ActualVelocity;   // Velocity vector passed to ApplyKick()
    public float   ErrorAngleDeg;    // Total angular error magnitude in degrees
    public bool    IsWeakFoot;       // Whether weak foot penalty was applied
    public float   PressureScalar;   // Pressure scalar at moment of contact [0, 1]
    public int     Frame;            // Simulation frame number at CONTACT
    public float   MatchTime;        // Match time in seconds at CONTACT
}
```

---

**`PassCancelledEvent`**

Published at: `WINDUP → IDLE` transition (tackle interrupt only).

```csharp
// Defined in: PassEvents.cs
// Published by: PassExecutor.cs
// Consumed by: Statistics Engine (Stage 1), Decision Tree (#8)

public struct PassCancelledEvent
{
    public int         AgentId;         // ID of the agent whose pass was cancelled
    public int         TeamId;          // Team of the executing agent
    public CancelReason CancelReason;   // TACKLE_INTERRUPT (only valid reason in Stage 0)
    public PassType    PassType;        // Pass type that was in progress
    public int         Frame;           // Simulation frame at cancellation
    public float       MatchTime;       // Match time in seconds at cancellation
}

public enum CancelReason
{
    TACKLE_INTERRUPT,     // Collision System set tackle flag during WINDUP
    // Additional reasons reserved for Stage 2+ (e.g., STUMBLE_INTERRUPT)
}
```

**Invalid request rejection does not produce `PassCancelledEvent`.** An invalid
`PassRequest` (FM-01, FM-07) is rejected silently with a logged error. It represents
a programming error, not a game event. See KD-8 (Section 1.3) and OQ-5 resolution
(Outline v1.0).

### 4.6.2 Events Not Published by Pass Mechanics

The following events are related to the pass lifecycle but are explicitly **not**
published by Pass Mechanics. Their publishers are documented for traceability.

| Event | Actual Publisher | Trigger |
|-------|-----------------|---------|
| `PassCompletedEvent` | First Touch (#4) | Receiving agent achieves CONTROLLED or LOOSE_BALL touch outcome |
| `PassInterceptedEvent` | Ball Physics (#1) | Possession changes to an opponent during ball flight |
| `PassOutOfPlayEvent` | Out-of-Play System (TBD, Stage 0) | Ball crosses touchline or goal line |

Pass Mechanics must not listen for these events. If Pass Mechanics appears to need
information from `PassCompletedEvent`, that is a sign of scope creep — the information
belongs to the statistics engine, not to Pass Mechanics.

### 4.6.3 Stage 0 Stub Implementation

The Stage 0 Event Bus stub is a no-op receiver that validates struct type compliance
without dispatching events to real consumers.

```csharp
// Stage 0 stub — replace with real Event System (#17) at Stage 1

public static class EventBusStub
{
    // Accepts all events. Performs no dispatch. Validates that struct is non-default.
    // In DEVELOPMENT builds: logs event type and frame to console.
    // In RELEASE builds: compiles to a no-op.

    public static void Publish<T>(T evt) where T : struct
    {
    #if DEVELOPMENT_BUILD
        Debug.Log($"[EventBus STUB] {typeof(T).Name} @ frame {GetFrame(evt)}");
    #endif
        // No dispatch. Event is discarded.
    }
}
```

This stub must be replaced — not removed — at Stage 1. The replacement must accept
the same struct signatures without modification. If Event System (#17) requires a
different publish signature, this specification requires a corresponding amendment.

---

## 4.7 Dependency Graph

The complete dependency graph for Pass Mechanics, showing direction of dependency
(arrows point from dependent to dependency):

```
                      ┌─────────────────────┐
                      │   Decision Tree (#8) │  ← (not yet written)
                      │     [CALLER]         │
                      └──────────┬──────────┘
                                 │ PassRequest
                                 ▼
                      ┌─────────────────────┐
              ┌───────│   PASS MECHANICS (#5)│────────┐
              │       │    [THIS SYSTEM]     │        │
              │       └──────────┬──────────┘        │
              │                  │                    │
              │ reads            │ calls              │ publishes
              │                  │ ApplyKick()        │
              ▼                  ▼                    ▼
  ┌──────────────────┐  ┌───────────────────┐  ┌─────────────────┐
  │  Agent Movement  │  │   Ball Physics    │  │  Event System   │
  │      (#2)        │  │      (#1)         │  │  (#17) [STUB]   │
  │  [READ ONLY]     │  │  [WRITE TARGET]   │  │  [NO-OP]        │
  └──────────────────┘  └────────┬──────────┘  └─────────────────┘
                                 │ BallState
              ┌──────────────────┘
              │ reads (pressure)
              │ receives (tackle flag)
              ▼
  ┌──────────────────┐
  │ Collision System │
  │      (#3)        │
  │ [READ + POLL]    │
  └──────────────────┘
              │
              │ implicit, via BallState changes
              ▼
  ┌──────────────────┐
  │   First Touch    │
  │      (#4)        │
  │  [NO DIRECT CALL]│
  └──────────────────┘
```

**Key observations from this graph:**

1. Pass Mechanics has **no circular dependencies** with any Stage 0 specification.
2. The Decision Tree (#8) is the only caller. No other system calls `PassExecutor.Execute()`.
3. First Touch has no knowledge of Pass Mechanics — only of the ball state it receives.
4. The Event System relationship is one-directional. Pass Mechanics publishes; it never
   subscribes to events from any system.

---

## 4.8 Cross-Specification Validation Checks

Before Section 4 can be approved, the following cross-specification checks must be
completed and signed off. Each check identifies a value or interface in another
specification that must be verified for consistency with this section.

| Check ID | What to Verify | Source Location | Target Location | Blocking? |
|----------|----------------|-----------------|-----------------|-----------|
| XC-4.2-01 | `ApplyKick()` signature matches §4.2.1 exactly — no `KickType` parameter | Ball Physics §3.1.11.2 (AM-001-001) | Pass Mechanics §4.2.1 | ✅ Blocking |
| XC-4.2-02 | `BallState.PossessingAgentId` field exists (Option A) OR `PossessionTracker` interface is defined (Option B) | Ball Physics §3.1 or standalone tracker | Pass Mechanics §4.2.3 | ⚠ Blocking (ERR-008) |
| XC-4.3-01 | `PlayerAttributes.KickPower`, `WeakFootRating`, `Crossing` exist with correct types and ranges | Agent Movement §3.5.6 (AM-002-001) | Pass Mechanics §4.3.1 | ⚠ Blocking (ERR-007) |
| XC-4.3-02 | `AgentState.FacingDirection` is `Vector2` (not `Vector3`) — confirm 2D in Stage 0 | Agent Movement §3.5.3 | Pass Mechanics §4.3.2 | ✅ Blocking |
| XC-4.4-01 | `SpatialHash.QueryRadius()` call signature matches §4.4.1 (center, radius, filter) | Collision System §3.1.4 | Pass Mechanics §4.4.1 | ✅ Blocking |
| XC-4.4-02 | Collision System agent record includes a per-agent `TackleContactFlag` readable by Pass Mechanics | Collision System §4 (Data Structures) | Pass Mechanics §4.4.2 | ✅ Blocking — **requires Collision System amendment** |
| XC-4.4-03 | `PRESSURE_RADIUS_MAX` constant in `PassConstants.cs` does not exceed Collision System spatial hash cell size | Collision System §3.1.3 (cell size) | Pass Mechanics `PassConstants.cs` | ✅ Blocking |
| XC-4.6-01 | `PassAttemptEvent` and `PassCancelledEvent` struct fields are compatible with Event System (#17) stub signature | Event Bus Stub (Stage 0) | Pass Mechanics §4.6.3 | Non-blocking (stub accepts any struct) |

**Status key:** ✅ = must be resolved before Section 4 approval. ⚠ = must be resolved
before Section 3 finalisation (already flagged in Section 1 and 2 dependency tables).

---

## 4.9 Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | February 20, 2026, 9:00 PM PST | Claude (AI) / Anton | Initial draft. All five integration contracts defined. Polling-flag decision for tackle interrupt formalised. ERR-007 and ERR-008 flags carried forward. Cross-spec validation table complete (8 checks). |
| 1.1 | March 25, 2026 | Claude (AI) / Anton | Post-audit fixes: Decision Tree #7→#8 (C-03, 5 instances); Prerequisites §3.1 version updated v1.0→v1.1 (Mod-03). |

---

*End of Section 4 — Pass Mechanics Specification #5*

*Next: Section 5 — Testing (test philosophy, unit tests, integration tests, validation tests)*
