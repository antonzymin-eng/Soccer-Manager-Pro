## 4.4 Integration with Collision System (#3)

Shot Mechanics interacts with the Collision System for two distinct purposes: (1) a
one-shot pressure query at `CONTACT` state to compute the pressure scalar for the error
model, and (2) a tackle interrupt signal that can cancel a shot during `WINDUP` state.
These are separate interfaces with different timing and ownership. Both patterns are
direct reuse from Pass Mechanics §4.4 — no new interface patterns are introduced.

### 4.4.1 Pressure Query Contract

The pressure query is a **read-only** call to the Collision System's spatial hash.
It is called once, at `CONTACT` state, immediately before the error vector is computed.

```csharp
// Read location: ShotExecutor.cs, CONTACT state, before ShotErrorCalculator.Compute()
// Source: Collision System Specification #3, §3.1.4 (SpatialHash.QueryRadius)
// Owner: Collision System
// Shot Mechanics does not implement spatial hashing — it queries the existing hash.

List<AgentId> queriedEntities = SpatialHash.QueryRadius(
    center:  agent.Position,
    radius:  ShotConstants.PRESSURE_RADIUS_MAX,     // [GT] — see §3.6.5
    filter:  QueryFilter.OpponentsOnly              // Same-team agents excluded
);

// ⚠ INTERIM WORKAROUND (pending Collision System §3.1 amendment — see Spec Error Log):
// SpatialHashGrid.Query() ignores the radius parameter — it always returns the fixed
// 3×3 cell neighbourhood (~±1.5m), regardless of the radius argument passed.
// Until the Collision System is amended to honour radius, the caller must filter:
List<AgentId> nearbyOpponents = queriedEntities
    .Where(id => Vector3.Distance(agent.Position, AgentSystem.GetAgent(id).Position)
                 <= ShotConstants.PRESSURE_RADIUS_MAX)
    .ToList();

// Result passed to ShotErrorCalculator as input to pressure scalar computation.
// See §3.6.5 for pressure scalar derivation from nearest opponent distance.
```

**When the Collision System is amended** (dynamic neighbourhood size based on radius),
remove the `.Where()` filter above — `QueryRadius` will return the correct set directly.

**Timing constraint:** The query runs at `CONTACT` state, not at `INITIATING`. This
captures pressure at the moment of ball contact. A shooter who steps away from pressure
during `WINDUP` benefits from reduced pressure error — this is the physically correct model.

**Collision System Query() defect:** `PRESSURE_RADIUS_MAX` (3.0m) exceeds the spatial hash
cell size (1.0m). The `Query()` implementation ignores its radius argument and always queries
a fixed 3×3 neighbourhood (±1.5m). The interim workaround above (caller-side distance filter)
corrects this until the Collision System spec is amended. Tracked in Spec Error Log as
ERR-011. XC-4.4-01 is marked RESOLVED WITH WORKAROUND.

### 4.4.2 Tackle Interrupt Contract

A tackle interrupt cancels a shot during `WINDUP` if the Collision System signals that
a physical tackle contact has been made on the executing agent. The interface form is
identical to Pass Mechanics §4.4.2 — polling flag, not callback.

```csharp
// Polled location: ShotExecutor.cs, start of each simulation tick during WINDUP
// Source: Collision System Specification #3
// Note: Collision System sets this flag; this single call atomically reads and clears it.
// API aligned with Pass Mechanics §4.4.2 (approved reference implementation).

bool tackleInterrupt = CollisionSystem.GetAndClearTackleFlag(agentId: request.AgentId);

if (tackleInterrupt)
{
    // Transition to CANCELLED state. Ball.ApplyKick() is NOT called.
    // ShotResult.Outcome = ShotOutcome.Cancelled.
    // ShotCancelledEvent published with CancelReason = TackleInterrupt.
    // Ball remains at shooter's feet in ROLLING state — Collision System owns tackle resolution.
}
```

**CANCELLED state behaviour:** The ball remains at the shooter's feet in `ROLLING` state.
Collision System owns the tackle resolution outcome (which agent gains possession, if any).
Shot Mechanics makes no possession decision on cancellation.

### 4.4.3 Interface Form Decision

**Polling flag retained from Pass Mechanics.** The callback alternative was considered
and rejected for the same reasons as in Pass Mechanics §4.4.3: callback registration
requires an interface definition before both the Collision System callback mechanism
and Shot Mechanics are fully specified. The polling flag adds approximately 1 ns per
tick (single boolean read) and eliminates cross-specification coupling at Stage 0.
Migration to callback is documented as a Stage 1 option in Section 7.

---

## 4.5 Forward Reference: Goalkeeper Mechanics (#11)

Goalkeeper Mechanics (#11) is the primary downstream consumer of Shot Mechanics output.
It is not yet specified. Per the project interface principle, no `IGkResponseSystem`
interface will be defined until both sides are specified.

### 4.5.1 ShotExecutedEvent as the Sole Interface Surface

The only interface surface between Shot Mechanics and Goalkeeper Mechanics is the
`ShotExecutedEvent` struct (§2.4.3). Goalkeeper Mechanics will subscribe to this event
when Spec #11 is written. Shot Mechanics publishes the event and terminates — it has no
knowledge of what Goalkeeper Mechanics does with it.

This is the complete Stage 0 interface contract between these two specifications.
**No additional surface area is required**, and any attempt to add a direct method call
from Shot Mechanics to a goalkeeper system constitutes a specification violation.

### 4.5.2 What GK Mechanics Will Receive

```csharp
// Authoritative struct definition: ShotEvents.cs
// Published by: ShotEventEmitter.cs at CONTACT state completion
// Consumed by: Goalkeeper Mechanics (#11) — not yet written

struct ShotExecutedEvent
{
    int     ShootingAgentId;      // Identifies shooter for GK target tracking
    Vector3 KickVelocity;         // Full velocity vector — direction + magnitude combined.
                                  //  GK Mechanics derives shot speed and trajectory from this alone.
                                  //  No separate speed field needed.
    Vector3 KickSpin;             // Spin vector — encodes curl, dip, and loft effects implicitly.
                                  //  GK Mechanics uses this to anticipate ball curve during flight.
    float   PowerIntent;          // [0.0–1.0] — proxy for shot power classification.
                                  //  Not used by Ball Physics; reserved for GK decision weighting.
    float   SpinIntent;           // [0.0–1.0] — proxy for spin classification.
                                  //  Reserved for GK anticipation model.
    Vector2 EstimatedTarget;      // Goal-relative (u,v) INTENDED target — pre-error.
                                  //  GK Mechanics can use this to understand shooter intent
                                  //  if GK has appropriate perception attributes.
                                  //  Distinct from actual ball trajectory (KickVelocity determines that).
    float   BodyMechanicsScore;   // [0.0–1.0] — output of BodyMechanicsEvaluator (§3.7).
                                  //  GK Mechanics uses this to weight save difficulty.
                                  //  Low BMS → poorly struck ball → GK advantage.
    float   MatchTime;            // Seconds from kickoff. For event records and Statistics Engine.
    bool    StumbleTriggered;     // True if shooter stumbled. GK: irregular trajectory possible.
    int     ContactFrame;         // Simulation frame of Ball.ApplyKick(). Replay synchronisation.
}
```

**No `ShotType` field.** The physical vectors carry all information GK Mechanics requires
to model save difficulty. A shot type enum would be redundant and would introduce coupling
to Decision Tree vocabulary that does not belong in the physics layer. (KD-3, §1.4.)

### 4.5.3 No IGkResponseSystem at Stage 0

No `IGkResponseSystem` interface is defined in this specification. When Goalkeeper
Mechanics Spec #11 is drafted, it will define its own subscription to `ShotExecutedEvent`
via the Event System. Shot Mechanics requires no amendment for that to occur.

If a reviewer or implementer adds a GK system reference to any Shot Mechanics file
before Spec #11 is approved, that is a specification violation and must be reverted.

---

## 4.6 Forward Reference: Decision Tree (#8) as Caller

Decision Tree (#8) is the sole caller of `ShotExecutor.Execute()`. It is not yet
specified. The interface surface from Shot Mechanics' perspective is fully defined
by `ShotRequest.cs`.

### 4.6.1 ShotRequest Ownership

`ShotRequest` is defined and owned by Shot Mechanics Specification #6. Decision Tree
instantiates and populates it, but it must not define its own copy of the struct. The
authoritative definition is `ShotRequest.cs` in this module.

```csharp
// Authoritative struct definition: ShotRequest.cs
// Owner: Shot Mechanics Specification #6
// Instantiated by: Decision Tree (#8)

struct ShotRequest
{
    int         AgentId;          // ID of the shooting agent
    float       PowerIntent;      // [0.0–1.0] — normalised power intent
    ContactZone ContactZone;      // Enum: Centre | BelowCentre | OffCentre (§2.4.1, §3.1.3 V-005)
                                  //  NOT a Vector2. Authoritative type is the ContactZone enum
                                  //  defined in §2.4.1. Three discrete values only.
    float       SpinIntent;       // [0.0–1.0] — normalised spin intent
    Vector2     PlacementTarget;  // Goal-relative (u,v) target [0.0–1.0 per axis]
    bool        IsWeakFoot;       // True if shooting with non-dominant foot
    float       DistanceToGoal;   // Distance to goal (metres) at request submission; > 0
    int         FrameNumber;      // Simulation frame — deterministic hash seed component
}
```

**No `ShotType` field on `ShotRequest`.** Decision Tree encodes shot intent exclusively
through the four physical parameters above. Named shot types (driven, finesse, chip)
are Decision Tree vocabulary and are fully represented by parameter combinations.
Adding a `ShotType` field to `ShotRequest` would constitute a specification violation.
(KD-3, §1.4; OI-006 resolution, Outline v1.2.)

### 4.6.2 Caller Contract

Decision Tree (#8) must satisfy the following constraints before calling
`ShotExecutor.Execute()`:

1. The executing agent must have ball possession at the time of the call. Shot Mechanics
   will re-verify this at `INITIATING` state, but the Decision Tree is responsible for
   not submitting shot requests for agents without possession.
2. `ShotRequest` must not be submitted while the agent is already in a non-IDLE shot
   execution state. Double-submission will be rejected with `ShotOutcome.Invalid` and
   logged. (§3.1.3, V-010.)
3. `PowerIntent`, `SpinIntent`, and `PlacementTarget` components must be in [0.0, 1.0].
   Out-of-range values are rejected at validation (§3.1.3, V-002 through V-005).
4. `ContactZone` must be a valid `ContactZone` enum member (`Centre`, `BelowCentre`, or
   `OffCentre`). Any other value is rejected at validation (§3.1.3, V-005). `ContactZone`
   is **not** a Vector2 — it is a three-value enum defined in §2.4.1.

Decision Tree has no access to `ShotContext` (internal computation state). If Decision
Tree requires post-execution information, it reads from `ShotResult` returned by
`ShotExecutor.Execute()` and from `ShotExecutedEvent` via Event System subscription.

**Note on ShotExecutor constructor:** Decision Tree instantiates `ShotExecutor` (or
receives it via a service locator). When doing so, Decision Tree passes no argument
to the constructor — the production default (`ShotVelocityCalculator.Instance`) is
correct. Decision Tree must not pass any velocity calculator argument; that seam
is for test use only.

---

## 4.7 Integration with Event System (#17)

Event System (#17) is not yet specified. At Stage 0, Shot Mechanics uses a no-op stub
that accepts the same event struct signatures to ensure seamless migration at Stage 1.

### 4.7.1 Events Published by Shot Mechanics

Shot Mechanics publishes exactly two event types:

| Event | Published When | Subscriber (Stage 0) | Subscriber (Stage 1+) |
|---|---|---|---|
| `ShotExecutedEvent` | `CONTACT` state completes, `Ball.ApplyKick()` has been called | EventBusStub (no-op) | Goalkeeper Mechanics (#11), Statistics Engine, Animation System |
| `ShotCancelledEvent` | `WINDUP` tackle interrupt fires | EventBusStub (no-op) | Decision Tree (#8), Animation System |

No other events are published by Shot Mechanics. In particular:

- No `ShotAttemptEvent` (pre-contact intent event) is published at Stage 0. Reserved
  as Stage 1 extension for animation and commentary systems.
- `ShotCancelledEvent` is published on WINDUP interrupt only. It is NOT published
  when a `ShotRequest` is rejected as `Invalid` — that is a programming error,
  not an observable game event.

### 4.7.2 Events Not Published by Shot Mechanics

The following events may appear in adjacent systems but are **not** published by Shot Mechanics:

| Event | Owner |
|---|---|
| Goal detection event | Match Referee system |
| Goalkeeper save event | Goalkeeper Mechanics (#11) |
| Ball out-of-bounds event | Match Referee system |
| Agent state STUMBLING transition | Agent Movement hysteresis system (reads `ShotResult.StumbleTriggered`) |

### 4.7.3 Stage 0 Stub Implementation

The Stage 0 event bus stub is identical to the Pass Mechanics stub (§4.6.3 of Pass
Mechanics Spec #5). No new stub implementation is required. `ShotEventEmitter.cs`
calls the same `EventBusStub.Publish<T>()` method:

```csharp
// Location: ShotEventEmitter.cs
// Same stub as PassMechanics — no structural change required

public static class EventBusStub
{
    // Stage 0: no-op stub. Accepts event struct; discards it.
    // Stage 1: this class is replaced with the Event System (#17) dispatcher.
    // Replacement must accept the same struct signatures without modification.
    // If Event System #17 requires a different publish signature, this specification
    // requires a corresponding amendment.

    public static void Publish<T>(T evt) where T : struct
    {
    #if DEVELOPMENT_BUILD
        Debug.Log($"[EventBusStub] {typeof(T).Name} @ frame {Time.frameCount}");
    #endif
        // No dispatch. Event is discarded at Stage 0.
    }
}
```

**Migration note:** When Event System #17 is specified and implemented, the stub is
replaced — not modified piecemeal. `ShotEventEmitter.cs` must not be modified during
stub replacement. If the Event System requires a different call site pattern, that
pattern change is documented in Event System Spec #17 and cross-referenced here via
amendment.

---

## 4.8 Dependency Graph

The complete dependency graph for Shot Mechanics, showing direction of dependency
(arrows point from dependent to dependency):

```
                      ┌────────────────────────┐
                      │   Decision Tree (#8)    │  ← (not yet written)
                      │         [CALLER]        │
                      └───────────┬────────────┘
                                  │ ShotRequest
                                  ▼
                      ┌────────────────────────┐
              ┌───────│  SHOT MECHANICS (#6)   │────────────┐
              │       │     [THIS SYSTEM]      │            │
              │       └───────────┬────────────┘            │
              │                   │                          │
              │ reads             │ calls                    │ publishes
              │                   │ ApplyKick()              │
              ▼                   ▼                          ▼
  ┌──────────────────┐  ┌──────────────────────┐  ┌─────────────────────┐
  │  Agent Movement  │  │    Ball Physics       │  │   Event System      │
  │      (#2)        │  │        (#1)           │  │  (#17) [STUB]       │
  │  [READ ONLY]     │  │  [WRITE TARGET]       │  │  [NO-OP]            │
  └──────────────────┘  └──────────┬────────────┘  └─────────────────────┘
                                   │ BallState
              ┌────────────────────┘
              │ reads (pressure query)
              │ receives (tackle interrupt flag)
              ▼
  ┌──────────────────┐
  │ Collision System │
  │      (#3)        │
  │ [READ + POLL]    │
  └──────────────────┘

  ┌──────────────────┐
  │ Goalkeeper Mech. │  ← (not yet written — Spec #11)
  │      (#10)       │  Subscribes to ShotExecutedEvent via Event System.
  │  [EVENT CONSUMER]│  No direct call from Shot Mechanics to GK system.
  └──────────────────┘
```

**Key observations:**

1. Shot Mechanics has **no circular dependencies** with any Stage 0 specification.
2. Decision Tree (#8) is the only caller. No other system calls `ShotExecutor.Execute()`.
3. Goalkeeper Mechanics has no knowledge of Shot Mechanics' internal systems — only of
   `ShotExecutedEvent`. The coupling is entirely mediated by the event struct.
4. The Event System relationship is one-directional. Shot Mechanics publishes; it never
   subscribes to events from any system.
5. First Touch (#4) has no direct relationship to Shot Mechanics. If a shot is saved
   and the ball is played back into play, First Touch activates independently based on
   ball state — not via any Shot Mechanics interface.

---

## 4.9 Cross-Specification Validation Checks

Before Section 4 can be approved, the following cross-specification checks must be
completed and signed off. These checks verify that interface contracts defined in this
section are consistent with the specifications they reference.

| ID | Check | Source Spec | This Spec Reference | Status |
|----|-------|-------------|---------------------|--------|
| XC-4.2-01 | `Ball.ApplyKick()` signature in Ball Physics §3.1.11.2 matches the contract in §4.2.1 (parameter names, types, order, no KickType) | Ball Physics #1, §3.1.11.2 | §4.2.1 | ✅ RESOLVED v1.3 — 6th parameter `BallEventLogger logger = null` added; production logger constraint documented |
| XC-4.2-02 | `V_MAX` ceiling and `SPIN_ABSOLUTE_MAX` do not exceed Ball Physics limits (`MAX_VELOCITY` = 50.0 m/s, `MAX_SPIN` = 80.0 rad/s) | Ball Physics #1, §3.1.12 | §4.2.1, §3.2.10, §3.4.10 | ✅ RESOLVED v1.3 — velocity OK (35.0 < 50.0); `SPIN_ABSOLUTE_MAX` corrected 150→80 rad/s in §3.4.10 |
| XC-4.2-03 | `BallState` possession model follows ERR-008 Option B (no `PossessingAgentId` field; possession is agent-side state) | Ball Physics #1, §3.1.11.2 | §4.2.2 | ✅ RESOLVED v1.3 — stale Option A code comment replaced with correct Option B implementation |
| XC-4.3-01 | `PlayerAttributes` in Agent Movement §3.5.6 contains all six fields consumed by Shot Mechanics: `Finishing`, `LongShots`, `Composure`, `KickPower`, `WeakFootRating`, `Fatigue` | Agent Movement #2, §3.5.6 | §4.3.1 | ✅ CONFIRMED v1.3 — all 6 fields present in Agent Movement v1.3 (ERR-007 resolved) |
| XC-4.3-02 | `Agent.CurrentState` field (`AgentMovementState`) is the correct type and name; approved shot initiation states documented | Agent Movement #2, §3.5.3 | §4.3.2 | ✅ RESOLVED v1.3 — `MovementState` corrected to `agent.CurrentState` (`AgentMovementState`); approved state set documented |
| XC-4.3-03 | Stumble signal mechanism specified: Agent Movement subscribes to `ShotExecutedEvent.StumbleTriggered` (Mechanism C) | Agent Movement #2 §4 (pending), Shot Mechanics §4.3.3 | §4.3.3 | ✅ RESOLVED v1.3 — Mechanism C documented; cross-spec action item added to Agent Movement Spec #2 |
| XC-4.4-01 | `PRESSURE_RADIUS_MAX` (3.0m) vs spatial hash cell size (1.0m); `Query()` radius bug workaround applied | Collision System #3, §3.1.4 | §4.4.1 | ✅ RESOLVED WITH WORKAROUND v1.3 — caller-side distance filter applied; Collision System §3.1 amendment tracked as ERR-011 |
| XC-4.4-02 | Collision System tackle interrupt API uses `GetAndClearTackleFlag(agentId)` — single atomic call, aligned with Pass Mechanics §4.4.2 | Collision System #3 | §4.4.2 | ✅ RESOLVED v1.3 — two-call pattern replaced with `GetAndClearTackleFlag()` |
| XC-4.5-01 | `ShotExecutedEvent` struct fields sufficient for Goalkeeper Mechanics save difficulty model | Goalkeeper Mechanics #11 | §4.5.2 | ☐ Deferred to Spec #10 |
| XC-4.7-01 | `EventBusStub.Publish<T>()` signature identical in Shot Mechanics and Pass Mechanics | Pass Mechanics #5, §4.6.3 | §4.7.3 | ✅ CONFIRMED v1.3 — signatures identical |

**All non-deferred checks are now RESOLVED or CONFIRMED.** XC-4.5-01 is pre-blocking for
Goalkeeper Mechanics Spec #11 and does not block Shot Mechanics approval.

**Section 4 approval status: READY FOR LEAD DEVELOPER SIGN-OFF.**

---

## 4.10 Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | February 23, 2026, 11:59 PM PST | Claude (AI) / Anton | Initial draft. All integration contracts specified. File structure defined (13 files + 12 test files). Five integration boundaries documented. Dependency graph complete. 10 cross-spec validation checks defined. |
| 1.1 | February 23, 2026 | Claude (AI) / Anton | Four critique issues resolved: (1) `BodyLeanAngle` removed from `AgentState` read contract — now derived from `Velocity` at Stage 0; (2) `StumbleTriggered` field name corrected throughout; (3) `ContactZone` type corrected from `Vector2` to `ContactZone` enum in ShotRequest; (4) `ShotRequest` struct fields aligned with §2.4.1 authoritative definition. |
| 1.2 | February 23, 2026 | Claude (AI) / Anton | Amendment 1 merged. Two testing infrastructure gaps resolved: (1A) `GoalGeometryProvider.cs` added — test-only override seam for SP-009; (1B) `IShotVelocityCalculator` interface + `ShotVelocityCalculator` singleton + `NaNVelocityStub` added for EC-008. File count: 28 total. Statelessness rule; `ShotCancelledEvent` added to event table; `XC-4.3-02` field count corrected. Amendment 1 document now void. |
| 1.3 | February 23, 2026 | Claude (AI) / Anton | Cross-spec audit resolutions. 7 defects closed: (A1) `ApplyKick()` 6th parameter `BallEventLogger logger = null` added to §4.2.1; production logger constraint documented. (A2) `SPIN_ABSOLUTE_MAX` corrected 150→80 rad/s in §3.4.10 to match Ball Physics `MAX_SPIN`; marked `[VER]`. (A3) §4.2.2 possession check comment corrected to ERR-008 Option B — no `PossessingAgentId` on `BallState`; uses `AgentSystem.GetPossessor()`. (A4) §4.3.2 `MovementState` → `agent.CurrentState` (`AgentMovementState`); approved shot initiation state set documented (IDLE, WALKING, JOGGING, SPRINTING, DECELERATING; GROUNDED and STUMBLING rejected). (A5) Tackle interrupt API corrected: two-call pattern replaced with `CollisionSystem.GetAndClearTackleFlag(agentId)` — aligned with Pass Mechanics §4.4.2. (A6) Pressure query §4.4.1: caller-side distance filter workaround applied pending Collision System §3.1 amendment (tracked ERR-011). (A7) Stumble mechanism §4.3.3: Mechanism C adopted — Agent Movement subscribes to `ShotExecutedEvent.StumbleTriggered`; write prohibition preserved; cross-spec action item flagged for Agent Movement Spec #2. All 9 non-deferred XC checks resolved or confirmed. |
| 1.4 | March 6, 2026 | Claude (AI) / Anton | Comprehensive audit corrections: (1) Version header corrected 1.2→1.3 to match filename. (2) Duplicate Purpose text removed. (3) Decision Tree #7→#8, Goalkeeper Mechanics #10→#11. (4) ERR-009→ERR-011 per Spec Error Log v1.4 renumbering. |

---

## Section 4 Summary

| Sub-section | Key Content |
|-------------|-------------|
| **4.1 File Structure** | 15 module files + 13 test files; GoalGeometryProvider and IShotVelocityCalculator added by Amendment 1; statelessness rule; ownership rules |
| **4.1.1 GoalGeometryProvider** | Full implementation — test-override seam for SP-009; #if guarded; try/finally warning documented |
| **4.1.2 IShotVelocityCalculator** | Full implementation — singleton pattern; ShotExecutor constructor injection; NaNVelocityStub specification |
| **4.2 Ball Physics** | ApplyKick() contract; no KickType/ShotType; velocity encoding convention; 5 failure modes including FM-05 NaN guard |
| **4.3 Agent Movement** | 6 attributes read; 4 state fields read; STUMBLING flag contract; write prohibition |
| **4.4 Collision System** | Pressure query at CONTACT; polling tackle interrupt flag; CANCELLED behaviour |
| **4.5 Goalkeeper Mechanics** | ShotExecutedEvent only; no IGkResponseSystem; field justifications |
| **4.6 Decision Tree** | ShotRequest ownership; no ShotType field; caller constraints; constructor note |
| **4.7 Event System** | ShotExecutedEvent + ShotCancelledEvent; no-op stub; migration contract |
| **4.8 Dependency Graph** | No circular dependencies; GK coupling mediated by event only |
| **4.9 Validation Checks** | 10 checks; 9 pending pre-approval; 1 deferred to Spec #11 |

---

*End of Section 4 — Shot Mechanics Specification #6*
*Tactical Director — Specification #6 of 20 | Stage 0: Physics Foundation*
*Next: Section 5 — Testing*
