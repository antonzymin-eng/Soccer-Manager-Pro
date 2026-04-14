# Shot Mechanics Specification #6 — Section 4: Architecture & Integration

**File:** `Shot_Mechanics_Spec_Section_4_v1_3.md`
**Purpose:** Defines the complete file/module architecture for the Shot Mechanics system,
including all integration contracts with Ball Physics (#1), Agent Movement (#2),
Collision System (#3), Decision Tree (#8), and the Event System stub. This section is the
authoritative reference for boundary ownership and interface signatures. Implementers
must not cross these boundaries without a formal amendment to this section.

**Created:** February 23, 2026, 11:59 PM PST
**Revised:** February 23, 2026
**Version:** 1.3
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 6 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)
**Supersedes:** Shot_Mechanics_Spec_Section_4_v1_1.md and
               Shot_Mechanics_Spec_Section_4_Amendment_1_v1_0.md
               (both documents are now void — this file is the single authoritative source)

**Prerequisites:**
- Shot Mechanics Section 1 v1.1 (scope, KDs 1–7 locked)
- Shot Mechanics Section 2 v1.0 (data structures, FR-01–FR-11, 13-step pipeline)
- Shot Mechanics Section 3 Part 1 v1.0 (§3.1–§3.3 specified)
- Shot Mechanics Section 3 Part 2 v1.0 (§3.4–§3.10 specified)
- Ball Physics Spec #1 (approved) — `Ball.ApplyKick()` interface stable
- Agent Movement Spec #2 (approved) — `PlayerAttributes` confirmed in §3.5.6
- Collision System Spec #3 (approved) — tackle interrupt flag interface confirmed
- Pass Mechanics Spec #5 (approved) — interface patterns reused directly

**Open Dependency Flags:** None. All hard dependencies confirmed stable.

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
- [4.5 Forward Reference: Goalkeeper Mechanics (#11)](#45-forward-reference-goalkeeper-mechanics-10)
  - [4.5.1 ShotExecutedEvent as the Sole Interface Surface](#451-shotexecutedevent-as-the-sole-interface-surface)
  - [4.5.2 What GK Mechanics Will Receive](#452-what-gk-mechanics-will-receive)
  - [4.5.3 No IGkResponseSystem at Stage 0](#453-no-igkresponsesystem-at-stage-0)
- [4.6 Forward Reference: Decision Tree (#8) as Caller](#46-forward-reference-decision-tree-7-as-caller)
  - [4.6.1 ShotRequest Ownership](#461-shotrequest-ownership)
  - [4.6.2 Caller Contract](#462-caller-contract)
- [4.7 Integration with Event System (#17)](#47-integration-with-event-system-17)
  - [4.7.1 Events Published by Shot Mechanics](#471-events-published-by-shot-mechanics)
  - [4.7.2 Events Not Published by Shot Mechanics](#472-events-not-published-by-shot-mechanics)
  - [4.7.3 Stage 0 Stub Implementation](#473-stage-0-stub-implementation)
- [4.8 Dependency Graph](#48-dependency-graph)
- [4.9 Cross-Specification Validation Checks](#49-cross-specification-validation-checks)
- [4.10 Version History](#410-version-history)

---

## 4.1 File Structure

The Shot Mechanics module occupies its own folder within the simulation scripts directory.
All files are listed with their responsibilities. No other specification may add files to
this folder without a formal amendment to this section.

```
/Scripts/ShotMechanics/
│
│   ShotExecutor.cs
│       Main orchestrator. Owns the shot execution state machine
│       (IDLE → INITIATING → WINDUP → CONTACT → FOLLOW_THROUGH → COMPLETE).
│       Entry point for all external callers. Validates ShotRequest, coordinates
│       sub-system calls, invokes Ball.ApplyKick() at CONTACT state.
│       Publishes ShotExecutedEvent on completion.
│       Transitions to CANCELLED state on tackle interrupt; does not call ApplyKick().
│       Receives IShotVelocityCalculator via constructor injection (Amendment 1B).
│       Default: ShotVelocityCalculator.Instance. Tests inject NaNVelocityStub for EC-008.
│       Constructor: ShotExecutor(IShotVelocityCalculator velocityCalculator = null)
│
│   IShotVelocityCalculator.cs                                      [ADDED — Amendment 1B]
│       Thin interface on ShotVelocityCalculator enabling NaN injection for EC-008
│       (FM-05 NaN recovery path test). Single method: Calculate(...) → float.
│       The only production implementation is ShotVelocityCalculator.
│       The only test stub is NaNVelocityStub (in /Tests/).
│       Must not be used to inject any other calculator — this seam exists solely for EC-008.
│
│   ShotVelocityCalculator.cs
│       Computes scalar kick speed (§3.2) using the Finishing/LongShots sigmoid blend,
│       ContactZone modifier, spin–velocity trade-off, fatigue scalar, and contact quality
│       modifier. Implements IShotVelocityCalculator (Amendment 1B).
│       Singleton pattern: ShotVelocityCalculator.Instance (private constructor).
│       All methods are deterministic given identical inputs. Must remain stateless —
│       no mutable fields may be added without a formal amendment. Must never call System.Random.
│
│   ShotLaunchAngleCalculator.cs
│       Computes launch angle (§3.3) from ContactZone base angle, power lift modifier,
│       spin lift modifier, body lean penalty, and body shape penalty.
│       Pure calculation — no side effects. Static class.
│       Outputs angle in degrees. ShotExecutor encodes into velocity Z component.
│
│   ShotSpinCalculator.cs
│       Computes the spin vector (§3.4) from SpinIntent, ContactZone, and facing direction.
│       Outputs angular velocity Vector3 (rad/s) for Magnus model input in Ball Physics.
│       Pure calculation — no side effects. Static class.
│
│   ShotPlacementResolver.cs
│       Translates PlacementTarget (goal-relative UV) into world-space aim direction (§3.5).
│       Reads goal geometry via GoalGeometryProvider.Get() — NEVER directly from ShotConstants.
│       (Amendment 1A: GoalGeometryProvider is the sole access point for goal geometry.)
│       Applies final error-adjusted target as launch direction input to Ball.ApplyKick().
│       Pure calculation — no side effects. Static class.
│
│   GoalGeometryProvider.cs                                         [ADDED — Amendment 1A]
│       Single access point for goal geometry constants (goal width, height, post positions,
│       goal line Z coordinate). Returns ShotConstants compile-time values in production.
│       In #if UNITY_EDITOR || DEVELOPMENT_BUILD builds: exposes SetTestOverride() and
│       ClearTestOverride() for SP-009 only. Zero production runtime cost.
│       ShotPlacementResolver is the only production caller. No other Shot Mechanics file
│       reads goal geometry directly from ShotConstants.
│       See §4.1.1 for full implementation specification.
│
│   ShotErrorCalculator.cs
│       Computes the deterministic error vector (§3.6) from Finishing, Composure,
│       ContactZone quality, pressure scalar, fatigue scalar, body shape scalar, and
│       power penalty scalar. Applies WeakFootPenalty when ShotRequest.IsWeakFoot = true.
│       Deterministic hash: matchSeed + agentId + frameNumber. Must never call System.Random.
│       Pure calculation — no side effects. Static class.
│
│   BodyMechanicsEvaluator.cs
│       Computes BodyMechanicsScore [0.0–1.0] (§3.7) from locomotion state, run-up angle,
│       plant foot offset, and agent velocity at shot initiation. A score below
│       STUMBLE_SCORE_THRESHOLD with PowerIntent above STUMBLE_POWER_THRESHOLD triggers
│       a STUMBLING signal read by the Agent Movement hysteresis system (§3.9).
│       Pure calculation — no side effects. Static class.
│
│   WeakFootPenaltyApplier.cs
│       Applies the weak foot error multiplier (§3.8) when ShotRequest.IsWeakFoot = true.
│       Uses the same formula as Pass Mechanics §3.7 — direct reuse, no structural changes.
│       Formula: WeakFootMultiplier = 1 + (5 - WeakFootRating) × WEAK_FOOT_ERROR_SCALE.
│       Pure calculation — no side effects. Static class.
│
│   ShotStateMachine.cs
│       Owns IDLE → INITIATING → WINDUP → CONTACT → FOLLOW_THROUGH → COMPLETE / CANCELLED
│       state transition logic (§3.9). Tracks windup duration and follow-through duration.
│       Polls tackle interrupt flag during WINDUP (see §4.4.2). Does not own physics
│       calculations — delegates to sub-system calculators. Called exclusively by ShotExecutor.
│
│   ShotEventEmitter.cs
│       Constructs and publishes ShotExecutedEvent (§3.10) at CONTACT state completion.
│       At Stage 0, publishes to the EventBusStub (no-op). Struct fields defined in §2.4.3.
│       Must not publish ShotExecutedEvent for CANCELLED shots.
│
│   ShotConstants.cs
│       All gameplay-tunable scalar constants. Every constant annotated [GT] (gameplay-tunable)
│       or [EST] (literature-estimated, requires verification) or [VER] (verified).
│       No magic numbers anywhere else in the module. Single source of truth for tuning.
│       Includes goal geometry constants (goal width, height, post positions) — but these
│       must be accessed via GoalGeometryProvider.Get(), not read from ShotConstants directly.
│
│   ShotRequest.cs
│       ShotRequest struct definition. Owned by this specification. Decision Tree (#8)
│       instantiates and populates it before calling ShotExecutor.Execute().
│       This file is the authoritative struct definition. Decision Tree must not
│       define its own version. Any additional fields required by Decision Tree must
│       be added via amendment to this specification.
│
│   ShotResult.cs
│       ShotResult struct definition. Returned by ShotExecutor.Execute() to the caller.
│       Contains: Outcome (enum), ErrorAngleDeg (float), FinalVelocity (Vector3),
│       FinalSpin (Vector3), EstimatedTarget (Vector2), BodyMechanicsScore (float),
│       Frame (int), Diagnostics (ShotDiagnostics — populated in DEVELOPMENT builds only).
│
│   ShotOutcome.cs
│       ShotOutcome enum: Completed, Cancelled, Invalid.
│       ShotDiagnostics struct: intermediate calculation values for testing (DEVELOPMENT only).
│
│   ShotEvents.cs
│       ShotExecutedEvent struct definition (§2.4.3 — authoritative definition here).
│       ShotCancelledEvent struct definition.
│       Stage 0: published to no-op EventBusStub. Stage 1: published to Event System (#17).
│       No ShotType field. Physical vectors carry all information downstream consumers need.
│
│   ShotAnimationData.cs
│       ShotAnimationData stub struct (§2.4.4). Populated by ShotExecutor at CONTACT state.
│       Not consumed by any Stage 0 system. Defined now to reserve the field contract for
│       the Animation System (Stage 1+). Must not be removed during Stage 0 implementation.
│
└───/Tests/
        ShotValidatorTests.cs           — Unit tests for §3.1 validation (PV-*)
        ShotVelocityTests.cs            — Unit tests for §3.2 velocity model (SV-*)
        ShotLaunchAngleTests.cs         — Unit tests for §3.3 launch angle model (LA-*)
        ShotSpinTests.cs                — Unit tests for §3.4 spin calculation (SN-*)
        ShotPlacementTests.cs           — Unit tests for §3.5 placement resolver (SP-*)
        ShotErrorTests.cs               — Unit tests for §3.6 error model (SE-*)
        BodyMechanicsTests.cs           — Unit tests for §3.7 body mechanics (BM-*)
        WeakFootTests.cs                — Unit tests for §3.8 weak foot penalty (WF-*)
        ShotStateMachineTests.cs        — Unit tests for §3.9 state machine (SSM-*)
        ShotEventTests.cs               — Unit tests for §3.10 event emission (IT-*)
        ShotIntegrationTests.cs         — Integration tests (IT-*)
        ShotDeterminismTests.cs         — Replay determinism tests (IT-012)
        NaNVelocityStub.cs              — EC-008 test stub only [ADDED — Amendment 1B]
                                          Returns float.NaN to trigger FM-05 recovery.
                                          Never referenced from production code.
```

**File count:** 15 module files + 13 test files = **28 total**
(v1.1 had 13 + 12 = 25. Amendment 1 added GoalGeometryProvider.cs, IShotVelocityCalculator.cs,
NaNVelocityStub.cs.)

**Ownership rule:** `ShotRequest.cs` is defined in this specification. Decision Tree (#8)
is the caller — it instantiates `ShotRequest` but does not define the struct. If Decision
Tree requires additional fields on `ShotRequest`, that amendment must originate in this
specification, not in Decision Tree's specification.

**Statelessness rule:** All calculator classes (`ShotVelocityCalculator`, `ShotLaunchAngleCalculator`,
`ShotSpinCalculator`, `ShotPlacementResolver`, `ShotErrorCalculator`, `BodyMechanicsEvaluator`,
`WeakFootPenaltyApplier`) must remain **stateless** — no mutable instance or static fields beyond
compile-time constants. Adding mutable state to any calculator without a formal amendment is a
specification violation. This rule is especially critical for `ShotVelocityCalculator`, which
uses the singleton pattern: a stateful singleton would break determinism and thread safety.

---

### 4.1.1 GoalGeometryProvider — Full Implementation Specification

`GoalGeometryProvider.cs` is specified here rather than in §3.5 because it is an
architectural concern (testability seam) rather than a physics calculation.

```csharp
/// <summary>
/// GoalGeometryProvider.cs
/// Owner: Shot Mechanics Specification #6, §4.1.1
/// Purpose: Single access point for goal geometry constants used by ShotPlacementResolver.
///          In production builds, returns compile-time constants from ShotConstants.
///          In editor/development builds, supports a test-only override for SP-009.
///          ShotPlacementResolver must call GoalGeometryProvider.Get() for ALL goal
///          geometry — never read ShotConstants goal fields directly.
/// Created: February 23, 2026
/// </summary>
public static class GoalGeometryProvider
{
    // Test override — null in production builds.
    // Set only by test fixtures. Must never be set from production code paths.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static GoalGeometry? _testOverride = null;
#endif

    /// <summary>
    /// Returns goal geometry constants. In production: compile-time values from ShotConstants.
    /// In editor/development builds: returns test override if one has been set.
    /// </summary>
    public static GoalGeometry Get()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_testOverride.HasValue)
            return _testOverride.Value;
#endif
        return new GoalGeometry
        {
            GoalWidth   = ShotConstants.GOAL_WIDTH,
            GoalHeight  = ShotConstants.GOAL_HEIGHT,
            GoalLineZ   = ShotConstants.GOAL_LINE_Z,
            LeftPostX   = ShotConstants.GOAL_LEFT_POST_X,
            RightPostX  = ShotConstants.GOAL_RIGHT_POST_X,
            CrossbarY   = ShotConstants.GOAL_CROSSBAR_Y
        };
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// TEST USE ONLY. Sets a temporary goal geometry override for SP-009.
    /// Must be paired with ClearTestOverride() in [TearDown].
    /// If the test body throws before TearDown, the static field will remain dirty
    /// for the remainder of the test session. Use try/finally inside SP-009 as a
    /// second defence in addition to [TearDown].
    /// Must not be called from any production code path.
    /// </summary>
    public static void SetTestOverride(GoalGeometry overrides)
    {
        _testOverride = overrides;
    }

    /// <summary>
    /// TEST USE ONLY. Clears the test override. Call in [TearDown] of every test
    /// that calls SetTestOverride(). See try/finally note in SetTestOverride().
    /// </summary>
    public static void ClearTestOverride()
    {
        _testOverride = null;
    }
#endif
}

/// <summary>
/// Value type carrying all goal geometry parameters for a single evaluation.
/// Passed by value — no heap allocation. All fields populated by GoalGeometryProvider.Get().
/// </summary>
public struct GoalGeometry
{
    public float GoalWidth;     // metres — full internal width, left post to right post
    public float GoalHeight;    // metres — ground to underside of crossbar
    public float GoalLineZ;     // world-space Z coordinate of the goal line (front face of net)
    public float LeftPostX;     // world-space X of the left post (from attacker's perspective)
    public float RightPostX;    // world-space X of the right post
    public float CrossbarY;     // world-space Y of the crossbar underside
}
```

---

### 4.1.2 IShotVelocityCalculator — Full Implementation Specification

```csharp
/// <summary>
/// IShotVelocityCalculator.cs
/// Owner: Shot Mechanics Specification #6, §4.1.2
/// Purpose: Thin interface on ShotVelocityCalculator to enable NaN injection for
///          EC-008 (FM-05 recovery path test). The only production implementation
///          is ShotVelocityCalculator. The only test stub is NaNVelocityStub.
///          This interface must not be used for any other purpose. Do not add
///          further interfaces to other Shot Mechanics calculators without a formal
///          amendment specifying the test case that requires them.
/// Created: February 23, 2026
/// </summary>
public interface IShotVelocityCalculator
{
    /// <summary>
    /// Computes scalar kick speed (m/s) from §3.2 formula.
    /// Production implementation (ShotVelocityCalculator) never returns NaN or Infinity.
    /// NaNVelocityStub returns float.NaN intentionally to trigger FM-05 in ShotExecutor.
    /// </summary>
    float Calculate(
        float       powerIntent,
        float       distanceToGoal,
        float       finishing,
        float       longShots,
        float       kickPower,
        ContactZone contactZone,
        float       spinIntent,
        float       fatigue,
        float       contactQualityModifier
    );
}
```

**ShotVelocityCalculator singleton pattern (amendment to v1.1 static class):**

```csharp
// ShotVelocityCalculator.cs — partial specification
// Implements IShotVelocityCalculator for EC-008 injection seam.
// Singleton: ShotVelocityCalculator.Instance is the production instance.
// Private constructor: prevents instantiation outside this class.
// STATELESSNESS REQUIREMENT: no mutable fields. Ever. See §4.1 statelessness rule.

public class ShotVelocityCalculator : IShotVelocityCalculator
{
    public static readonly ShotVelocityCalculator Instance = new ShotVelocityCalculator();
    private ShotVelocityCalculator() { }

    public float Calculate(/* ... §3.2 formula ... */) { /* ... */ }
}
```

**ShotExecutor constructor injection:**

```csharp
// ShotExecutor.cs — constructor signature (amendment to v1.1)
// Production callers pass no argument; default resolves to ShotVelocityCalculator.Instance.
// EC-008 test passes new NaNVelocityStub().

public class ShotExecutor
{
    private readonly IShotVelocityCalculator _velocityCalculator;

    public ShotExecutor(IShotVelocityCalculator velocityCalculator = null)
    {
        _velocityCalculator = velocityCalculator ?? ShotVelocityCalculator.Instance;
    }
    // ... all other ShotExecutor behaviour unchanged from v1.1 ...
}
```

**NaNVelocityStub — test stub (in /Tests/, compiled in editor/development builds only):**

```csharp
// NaNVelocityStub.cs — TEST USE ONLY. /Tests/ folder only.
// Returns float.NaN to trigger ShotExecutor FM-05 recovery path.
// Used exclusively by EC-008. Must not be referenced from production code.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
public class NaNVelocityStub : IShotVelocityCalculator
{
    public float Calculate(float powerIntent, float distanceToGoal,
        float finishing, float longShots, float kickPower,
        ContactZone contactZone, float spinIntent,
        float fatigue, float contactQualityModifier)
    {
        return float.NaN;  // Intentional — triggers FM-05 in ShotExecutor
    }
}
#endif
```

---

## 4.2 Integration with Ball Physics (#1)

Ball Physics is the primary downstream dependency. Shot Mechanics calls exactly one
Ball Physics interface — `Ball.ApplyKick()` — and reads `BallState` once at the start
of each shot execution for a possession confirmation check. No other Ball Physics
interfaces are touched.

### 4.2.1 ApplyKick() Interface Contract

The authoritative `ApplyKick()` signature is defined in Ball Physics §3.1.11.2:

```csharp
// Defined in: Ball Physics Specification #1, §3.1.11.2
// Owner: Ball Physics
// Caller: Shot Mechanics (ShotExecutor.cs, CONTACT state only)
// Called: Exactly once per shot execution. Never called during WINDUP, FOLLOW_THROUGH,
//         or CANCELLED state.

void Ball.ApplyKick(
    ref BallState ball,              // Ball state, modified in-place by Ball Physics
    Vector3 velocity,                // Full 3D velocity vector (m/s). Elevation encoded in Z.
    Vector3 spin,                    // Angular velocity vector (rad/s). Magnus model input.
    int agentId,                     // ID of the kicking agent. Used for possession handoff.
    float matchTime,                 // Match time at kick (seconds from kickoff). For event records.
    BallEventLogger logger = null    // Ball event logger. Null accepted for unit tests ONLY.
                                     // In production builds, ShotExecutor MUST supply the
                                     // production BallEventLogger instance. Passing null in
                                     // production silently disables kick event logging.
)
```

**Constraints on the caller (Shot Mechanics):**

1. `ApplyKick()` must be called **exactly once** per shot execution, at `CONTACT` state.
   Calling more than once for the same shot is a specification violation.
2. Calling with `velocity == Vector3.zero` is **forbidden**. If a shot is cancelled,
   use `ShotOutcome.Cancelled` and do not call `ApplyKick()`.
3. `velocity.magnitude` must be within `[V_MIN, V_MAX]` — clamped by
   `ShotVelocityCalculator` before the call (§3.2.10). The clamped value must be logged
   if clamping was applied, for calibration purposes.
4. `spin.magnitude` must not exceed `BallPhysicsConstants.Limits.MAX_SPIN` (80.0 rad/s).
   Shot Mechanics `SPIN_ABSOLUTE_MAX` (§3.4.10) is set to 80.0 rad/s to enforce this
   at source. Ball Physics will silently clamp any spin exceeding 80.0 rad/s — passing
   values above this limit produces no error but results in unexpected Magnus behaviour.
5. `agentId` must match the agent whose possession was confirmed at `INITIATING` state
   (§4.2.2). A possession re-check immediately before `ApplyKick()` provides defence
   against race conditions introduced by tackle resolution running in the same tick.
6. `logger` must be the production `BallEventLogger` instance in all non-test call sites.
   `ShotExecutor` receives the logger at construction and passes it through. Null is
   only acceptable in unit tests that intentionally bypass event logging.

**No `ShotType` or `KickType` parameter.** The physical intent is fully encoded in
the velocity magnitude, launch direction, and spin vector. Ball Physics does not need
intent labels to simulate correct aerodynamics. Named shot labels (driven, finesse, chip)
are Decision Tree vocabulary and carry zero physics weight here. Any future reference in
this specification or any downstream specification to a shot type enum on `ApplyKick()`
is an error and must be corrected. (KD-3, Section 1.4; ERR-005 resolution, Pass Mechanics.)

**Velocity encoding convention:**

```
velocity = Vector3(
    horizontal_speed × sin(yaw_angle),    // X component — world horizontal
    horizontal_speed × cos(yaw_angle),    // Y component — world horizontal
    full_speed × sin(launch_angle_rad)    // Z component — world vertical (up positive)
)
```

Where `yaw_angle` is the aim direction azimuth and `launch_angle_rad` is the output
of `ShotLaunchAngleCalculator` converted to radians. The speed components are derived
from the scalar output of `ShotVelocityCalculator` (§3.2) such that
`velocity.magnitude == V_KICK` as clamped to `[V_MIN, V_MAX]`.

### 4.2.2 BallState Read Contract

Shot Mechanics reads `BallState` once, at `INITIATING` state, to confirm the executing
agent has possession before proceeding to `WINDUP`.

```csharp
// Read location: ShotExecutor.cs, ValidateRequest() method, INITIATING state
// Purpose: Confirm possession before committing to WINDUP

BallState ball = BallSystem.GetCurrentState();

// Possession check — ERR-008 Option B resolution:
// BallState has NO PossessingAgentId field. Possession is agent-side state.
// The correct check queries the agent system's possession record directly:
bool hasPossession = AgentSystem.GetPossessor() == request.AgentId;
if (!hasPossession)
{
    // Reject. Return ShotOutcome.Invalid. Do not proceed to WINDUP.
    // Log: "ShotRequest: agent {id} does not have possession."
}
// NOTE: The agent system possession API (GetPossessor / GetPossessingAgentId) will be
// confirmed and locked when the Possession System (Spec TBD, Stage 0 stub) is specified.
// Until then, use the equivalent Option B pattern established in Pass Mechanics §4.2.2.
```

Additionally, at `INITIATING` state, Shot Mechanics reads `BallState.Position.z` (ball
height) to apply to the STUMBLING threshold evaluation in `BodyMechanicsEvaluator`. A
shot attempted at ball height above the VOLLEY_HEIGHT_THRESHOLD floor is evaluated
with a different body mechanics baseline than a ground-ball shot. No other `BallState`
fields are read.

### 4.2.3 Possession Loss on ApplyKick()

Possession loss on `ApplyKick()` follows the same contract resolved for Pass Mechanics
(ERR-008): Ball Physics owns the possession transition inside `ApplyKick()`. Shot
Mechanics does not write to any possession field directly. No additional action is
required by Shot Mechanics after the call.

If ERR-008 resolution is superseded by a future amendment, this section requires a
corresponding update.

### 4.2.4 Failure Modes at This Boundary

| ID | Scenario | Detection | Action |
|----|----------|-----------|--------|
| FM-01 | Agent does not have possession at `ValidateRequest()` | Possession check (§4.2.2) | Reject `ShotRequest`. Return `ShotOutcome.Invalid`. Log warning with `agentId` and `frame`. Do not proceed to WINDUP. |
| FM-02 | `ApplyKick()` produces NaN or Infinity in `BallState` | Post-call `float.IsNaN()` / `float.IsInfinity()` check on `ball.Velocity` | Log critical error. Flag for QA investigation. Indicates upstream physics parameter error. |
| FM-03 | Possession race condition: agent loses possession between `INITIATING` and `CONTACT` | Possession re-check immediately before `ApplyKick()` at CONTACT | Log error. Transition to CANCELLED. This indicates a Collision System tackle interrupt handling failure. |
| FM-04 | Velocity magnitude below `V_MIN` after full calculation | Post-calculation magnitude check | Apply `V_MIN` floor. Log warning. This should not occur if ShotVelocityCalculator is correct. |
| FM-05 | NaN or Infinity from `IShotVelocityCalculator.Calculate()` | `float.IsNaN()` / `float.IsInfinity()` check on returned value | Do NOT call `ApplyKick()`. Return `ShotOutcome.Invalid`. Log critical error. Tested by EC-008 via NaNVelocityStub injection. |

---

## 4.3 Integration with Agent Movement (#2)

Shot Mechanics is a **read-only** consumer of Agent Movement data. It reads agent
attributes and state at shot initiation. It never writes to Agent Movement state —
possession loss is handled by Ball Physics (§4.2.3), and the STUMBLING signal is
handled by the Agent Movement hysteresis system without a direct write call from
Shot Mechanics.

### 4.3.1 PlayerAttributes Read Contract

Attributes are read once at `INITIATING` state, before WINDUP begins. They are captured
at this point and held constant for the duration of the execution pipeline, ensuring
determinism. A mid-shot attribute change — not possible in Stage 0, possible in Stage 3+ —
cannot corrupt the in-flight calculation.

```csharp
// Read location: ShotExecutor.cs, INITIATING state (before WINDUP)
// Source: Agent Movement Specification #2, §3.5.6
// Owner of these fields: Agent Movement Specification #2

PlayerAttributes attrs = AgentSystem.GetAttributes(request.AgentId);

// Fields consumed by Shot Mechanics (all required):
int   Finishing;         // [1–20]  Primary accuracy attribute for shots ≤ LONG_SHOT_THRESHOLD.
                         //          Sigmoid blend with LongShots (§3.2.3).
int   LongShots;         // [1–20]  Primary accuracy attribute for shots above threshold.
                         //          Sigmoid blend with Finishing (§3.2.3).
int   Composure;         // [1–20]  Pressure resistance scalar in error model (§3.6.5).
int   KickPower;         // [1–20]  Velocity scaling attribute (§3.2.4).
int   WeakFootRating;    // [1–5]   Weak foot error multiplier (§3.8).
float Fatigue;           // [0–1]   Fatigue scalar. Applied to velocity (§3.2.7) and error (§3.6.6).
float FormModifier;      // [GT]    Set to 1.0f at Stage 0. Reserved for Form System (Stage 2+).
float InjuryLevel;       // [0–1]   Read but not applied at Stage 0. Zero-injury assumption holds.
                         //          Reserved hook: Stage 2 will apply as velocity/BMS degradation.
```

**Note on `Finishing` vs `LongShots` sigmoid blend (KD-1 / §3.2.3):** The effective
velocity attribute `A_EFF` blends `Finishing` and `LongShots` as a continuous function
of distance. At `LONG_SHOT_THRESHOLD` [GT], the blend weight is 0.5 (equal contribution).
This is the primary distinction from Pass Mechanics, which uses a single primary
attribute without distance-dependent blending.

**Composure vs Technique (Pass Mechanics parallel):** Pass Mechanics used `Technique`
as a secondary accuracy attribute. Shot Mechanics uses `Composure` in that role —
specifically, `Composure` governs the pressure scalar (how much opponent proximity
degrades accuracy) rather than a general technique baseline. This distinction is
intentional and documented in §3.6.5.

### 4.3.2 AgentState Read Contract

Agent state is read at the same time as attributes — once at `INITIATING` state,
captured and held constant for the execution pipeline.

```csharp
// Read location: ShotExecutor.cs, INITIATING state
// Source: Agent Movement Specification #2, §3.5.3
// Owner of these fields: Agent Movement Specification #2

Agent agent = AgentSystem.GetAgent(request.AgentId);

// Fields consumed by Shot Mechanics (all required):
Vector3              Position;        // World position of executing agent at shot initiation.
                                      //  Used: distance-to-goal calculation (§3.2.4),
                                      //        pressure query origin (§4.4.1),
                                      //        goal geometry resolution (§3.5.4).
Vector3              Velocity;        // Agent velocity at shot initiation.
                                      //  Used: run-up angle calculation in BodyMechanicsEvaluator (§3.7.3).
                                      //  Body lean angle at Stage 0 is DERIVED from Velocity magnitude
                                      //  using the empirical lean formula in §3.3.6 — no separate field
                                      //  required. A dedicated BodyLeanAngle field is reserved in AgentState
                                      //  for Stage 1 animation integration but must not be assumed present.
Vector2              FacingDirection; // XY-plane facing unit vector.
                                      //  Used: body lean penalty (§3.3.6), spin direction basis (§3.4.3).
AgentMovementState   CurrentState;    // Authoritative movement state enum (Agent Movement §3.1.2).
                                      //  Shot rejected (ShotOutcome.Invalid) if CurrentState is not in
                                      //  the approved set: IDLE, WALKING, JOGGING, SPRINTING, DECELERATING.
                                      //  GROUNDED and STUMBLING are rejected — agent cannot shoot while
                                      //  incapacitated. No other states are valid shot initiation states.
                                      //  Field name: agent.CurrentState (NOT MovementState — that name
                                      //  belongs to AnimationDataContract, a different struct).
```

**Plant foot offset** — `BodyMechanicsEvaluator` requires the lateral distance from
the ball to the plant foot at contact. At Stage 0, this is derived from `AgentState.Position`
relative to `BallState.Position` (both read at `INITIATING`). A direct plant foot
position field is reserved in `AgentState` for Stage 1 animation integration but is
not required for Stage 0 calculation.

### 4.3.3 Write Prohibition

Shot Mechanics writes **nothing** to Agent Movement state. The following responsibilities
are explicitly confirmed as **not** belonging to Shot Mechanics:

- Setting agent locomotion state to `STUMBLING` — Shot Mechanics sets `ShotResult.StumbleTriggered`
  and publishes `ShotExecutedEvent.StumbleTriggered = true`. **Agent Movement subscribes to
  `ShotExecutedEvent` via the Event System and handles the `STUMBLING` state transition
  internally when `StumbleTriggered = true`.** Shot Mechanics does not call any Agent Movement
  write method directly. This is Mechanism C (event subscription) — it preserves the write
  prohibition and uses the existing event infrastructure without creating a direct call
  dependency from Shot Mechanics into Agent Movement.
  > Note: Agent Movement Spec #2 must document its subscription to `ShotExecutedEvent` when
  > Section 4 of that spec is reviewed. The subscription is currently unspecified in Agent
  > Movement — this is a cross-spec action item flagged for that spec's next revision.
- Setting agent locomotion state to `FOLLOW_THROUGH` — Animation System concern (Stage 1).
  Shot Mechanics populates `ShotAnimationData` as a passive data struct; it does not
  command animation.
- Removing possession from the executing agent — Ball Physics responsibility inside
  `ApplyKick()` (§4.2.3).
- Modifying `Fatigue` — read-only. Fatigue changes are governed by Agent Movement's
  own energy depletion model, which may increment fatigue in response to a shot (Stage 2+).

**STUMBLING signal contract (Mechanism C — event subscription):**

```csharp
// ShotResult (authoritative definition: §2.4.2):
bool StumbleTriggered;   // True if BodyMechanicsScore < STUMBLE_SCORE_THRESHOLD [GT]
                         // AND request.PowerIntent > STUMBLE_POWER_THRESHOLD [GT].
                         // Set by BodyMechanicsEvaluator (§3.7); stored in ShotResult.
                         // Also mirrored in ShotExecutedEvent.StumbleTriggered (§2.4.3).

// ShotExecutedEvent (§2.4.3) — published at CONTACT state completion:
bool StumbleTriggered;   // Agent Movement subscribes to ShotExecutedEvent and reads this
                         // field. When true, Agent Movement transitions the shooting agent
                         // to AgentMovementState.STUMBLING. Shot Mechanics does not call
                         // any write method on Agent Movement directly.

// Agent Movement subscription (to be documented in Agent Movement Spec #2 §4):
// EventBus.Subscribe<ShotExecutedEvent>(OnShotExecuted);
// void OnShotExecuted(ShotExecutedEvent evt) {
//     if (evt.StumbleTriggered)
//         AgentSystem.TransitionToStumbling(evt.ShootingAgentId);
// }
```

**Why Mechanism C over alternatives:**
- Mechanism A (direct call from ShotStateMachine to AgentSystem.SignalStumble) violates
  the write prohibition in §4.3.3 and creates a direct runtime dependency.
- Mechanism B (Decision Tree reads ShotResult and calls Agent Movement) adds a
  cross-system dependency to the Decision Tree that is not part of its defined contract.
- Mechanism C (event subscription) keeps all write authority within Agent Movement,
  uses the existing event infrastructure, and requires no new interface types.

---

