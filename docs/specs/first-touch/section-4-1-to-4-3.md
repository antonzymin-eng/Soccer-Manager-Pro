# First Touch Mechanics Specification #4 — Section 4: Data Structures

**Purpose:** Defines all data structures, enumerations, constants, and interface contracts for
the First Touch Mechanics system. This section is the authoritative source for struct field
definitions, constant values, and API boundaries. Implementation must match these definitions
exactly — any deviation requires a spec revision, not a code workaround.

**Created:** February 17, 2026, 12:00 PM PST
**Revised:** February 21, 2026, 10:00 PM PST
**Version:** 1.2
**Status:** Approved
**Author:** Claude (AI) with Anton (Lead Developer)
**Specification Number:** 4 of 20 (Stage 0 Physics Foundation)

---

## Changelog

### v1.2 — March 05, 2026

**M-05 fix (Moderate):** Updated `FirstTouchContext` struct size comment from `~128 bytes`
to `~88 bytes` per Section 6 §6.4.1 authoritative field-by-field count. The §2.5.2 estimate
of 128 bytes was a conservative overestimate. Section 9's internal consistency audit noted
this discrepancy as "RESOLVED" but the Section 4 comment was never updated. No struct
fields, no logic, and no constants changed.

**Date:** March 25, 2026, 11:59 PM PST

### v1.1 — February 21, 2026

**ERR-001 fix (Major):** Replaced `IBallPhysicsCallback` (§4.5.2) four-method interface with
a single `SetBallState(Vector3 position, Vector3 velocity)` call. The four-method design
encoded First Touch's internal `TouchResult` taxonomy into Ball Physics, creating coupling
between two independent systems. Ball Physics does not need to know *why* the state changed —
only what the new position and velocity are.

Changes applied:
- §4.1: Removed `FirstTouchEventEmitter.cs` from file list (see ERR-004 note).
- §4.5.1: Updated `ApplyTouchResult()` internal call list — removed `IBallPhysicsCallback`;
  added direct `IBallPhysicsSystem.SetBallState()` call.
- §4.5.2: `IBallPhysicsCallback` interface removed entirely. Replaced with `IBallPhysicsSystem`
  single-method contract.
- §4.6: Updated flow diagram — Apply block now shows `IBallPhysicsSystem.SetBallState()`.
- §4.9: Summary table updated.

**ERR-004 fix (Major):** Removed `IPossessionManager` (§4.5.4) and `IFirstTouchEventQueue`
(§4.5.5). Both interfaces defined contracts against unspecified systems (Possession Manager,
Event System). Interfacing against unspecified systems imports assumptions that may be
incompatible once those systems are designed.

Changes applied:
- §4.4: Removed `EVENT_QUEUE_CAPACITY` constant (no longer needed without event queue).
- §4.5.1: Updated `ApplyTouchResult()` internal call list — removed both removed interfaces;
  added note that possession state is written directly to `BallState` and event emission
  is deferred.
- §4.5.4: `IPossessionManager` removed. Replaced with direct `BallState.State` write
  documentation (CONTROLLED outcome sets `BallStateType.CONTROLLED`; all others set
  `BallStateType.AIRBORNE` or `BallStateType.ROLLING`).
- §4.5.5: `IFirstTouchEventQueue` removed. Replaced with deferred stub comment — event
  emission reconnected when Event System (Spec #17) is designed.
- §4.6: Flow diagram updated — possession write shown as direct `BallState` mutation;
  event queue line replaced with deferred stub note.
- §4.7.3: Allocation table updated — removed `FirstTouchEvent` pool row.
- §4.9: Summary table updated.

---

## Table of Contents

- [4.1 File Organization](#41-file-organization)
- [4.2 Enumerations](#42-enumerations)
- [4.3 Core Structs](#43-core-structs)
- [4.4 Constants Class](#44-constants-class)
- [4.5 Interface Contracts](#45-interface-contracts)
- [4.6 Data Flow Diagram](#46-data-flow-diagram)
- [4.7 Memory Layout](#47-memory-layout)
- [4.8 Validation Rules](#48-validation-rules)
- [4.9 Section Summary](#49-section-summary)

---

## 4.1 File Organization

The First Touch Mechanics system maps to the following file structure in the Unity project.
Each file owns a clearly bounded responsibility. This mapping is normative — files must not
be merged or split without a spec revision.

```
Assets/
└── Scripts/
    └── Physics/
        └── FirstTouch/
            ├── FirstTouchSystem.cs              // §4.5.1  Main entry point; coordinates evaluation pipeline
            ├── ControlQualityCalculator.cs      // §3.1    Implements governing formula; pure static methods
            ├── TouchRadiusCalculator.cs         // §3.2    Maps control quality to displacement radius
            ├── BallDisplacementProcessor.cs     // §3.3    Computes newBallPosition and newBallVelocity
            ├── PressureEvaluator.cs             // §3.5    Aggregates opponent proximity into scalar
            ├── OrientationDetector.cs           // §3.6    Half-turn detection and bonus computation
            ├── PossessionStateMachine.cs        // §3.4    Determines TouchResult from quality + radius
            ├── FirstTouchConstants.cs           // §4.4    All tunable constants; single source of truth
            ├── FirstTouchDataStructures.cs      // §4.3    All struct definitions (Context, Result)
            ├── FirstTouchEnumerations.cs        // §4.2    TouchResult enum
            ├── FirstTouchInterfaces.cs          // §4.5    All interface definitions
            └── Tests/
                ├── ControlQualityTests.cs       // Unit tests: §3.1 formula
                ├── TouchRadiusTests.cs          // Unit tests: §3.2 radius bands
                ├── BallDisplacementTests.cs     // Unit tests: §3.3 displacement
                ├── PossessionTests.cs           // Unit tests: §3.4 state machine
                ├── PressureTests.cs             // Unit tests: §3.5 pressure evaluation
                ├── OrientationTests.cs          // Unit tests: §3.6 orientation detection
                └── IntegrationTests.cs          // End-to-end scenarios from §5
```

**v1.1 change:** `FirstTouchEventEmitter.cs` removed. ERR-004 eliminated the
`IFirstTouchEventQueue` interface that this file would have implemented. Event emission
is deferred pending Event System Spec #17. The file will be restored when that spec
is designed and the interface contract is known.

**Rationale for file separation:**
- Each file can be unit-tested in isolation
- Constants are centralized — no magic numbers in logic files
- Interfaces are isolated so mock implementations work without referencing production code
- Test files mirror logic files for navigability

---

## 4.2 Enumerations

### 4.2.1 TouchResult

`TouchResult` represents the possession outcome of a first touch evaluation. It is the
primary output of the possession state machine (§3.4) and the key discriminator used by
downstream systems (Ball Physics, Agent Movement) to determine their response.

**Authority:** This definition supersedes any informal usage in other sections of this spec.
If another section's description conflicts with these definitions, the conflict must be
resolved with a spec revision — never by code convention.

```csharp
/// <summary>
/// Enumeration of all possible possession outcomes from a first touch evaluation.
///
/// These four states are mutually exclusive. Every first touch evaluation
/// must resolve to exactly one state. The evaluation priority order (in
/// PossessionStateMachine) ensures this invariant is maintained.
///
/// See §3.4 for the full state transition diagram and priority ordering.
/// See §2.4 for the football-context description of each state.
/// </summary>
public enum TouchResult
{
    /// <summary>
    /// The agent has successfully brought the ball under full control.
    /// The ball is within immediate playing distance (r ≤ 0.60m).
    ///
    /// Triggers:
    ///   - Agent Movement: DribblingModifier activated (§3.4.4)
    ///   - Ball Physics: SetBallState() called with NewBallPosition, NewBallVelocity
    ///   - BallState.State: set to BallStateType.CONTROLLED
    ///
    /// Precondition: q ≥ 0.55 AND r ≤ 0.60m (§3.4.2)
    /// </summary>
    CONTROLLED = 0,

    /// <summary>
    /// The ball was contacted but not controlled. No agent holds possession.
    /// Ball remains live; both teams contest.
    ///
    /// Triggers:
    ///   - Ball Physics: SetBallState() called; ball free in world
    ///   - BallState.State: set to BallStateType.ROLLING
    ///   - Collision System: next contact triggers new evaluation
    ///
    /// Precondition: r ≥ 0.60m AND r < 1.20m, OR q < 0.55 with recoverable radius
    /// </summary>
    LOOSE_BALL = 1,

    /// <summary>
    /// The ball was deflected away without deliberate control.
    /// Ball momentum is substantially preserved along the incoming direction.
    ///
    /// Triggers:
    ///   - Ball Physics: SetBallState() called (momentum partially retained)
    ///   - BallState.State: set to BallStateType.AIRBORNE or ROLLING depending on trajectory
    ///   - No DribblingModifier emitted
    ///
    /// Note: DEFLECTION is only reached when r ≥ 1.50m AND momentumAlignment ≥ 0.70
    /// AND no opponent is in interception range. If an opponent is in range, outcome
    /// is INTERCEPTION, not DEFLECTION. This is intentional (§3.4.2 priority note).
    ///
    /// Precondition: r ≥ 1.50m AND Dot(newBallVel.xy, ball.Vel.xy) ≥ 0.70
    ///               AND no opponent within INTERCEPTION_RADIUS
    /// </summary>
    DEFLECTION = 2,

    /// <summary>
    /// A heavy touch has allowed a nearby opponent to claim the ball.
    /// The intercepting agent evaluates their own first touch in the NEXT frame.
    ///
    /// Triggers:
    ///   - Ball Physics: SetBallState() called; ball velocity directed toward intercepting opponent
    ///   - BallState.State: set to BallStateType.ROLLING or AIRBORNE
    ///   - Collision System: next frame contact triggers interceptor's evaluation
    ///
    /// Critical: No CONTROLLED state is set in the frame INTERCEPTION is resolved.
    /// The next-frame rule (§3.4.5) prevents recursive evaluation. See §2.4.4.
    ///
    /// Precondition: r ≥ 1.20m AND SpatialQuery finds opponent within INTERCEPTION_RADIUS
    /// </summary>
    INTERCEPTION = 3
}
```

### 4.2.2 Enumeration Value Ordering

The integer values 0–3 are stable and intentional. They may be used for serialisation
(replay files, statistics persistence). Do not reorder or reassign values in future
revisions — add new values at the end only, and document the reason in a changelog entry.

| Value | Name | Typical Frequency |
|-------|------|------------------|
| 0 | CONTROLLED | ~55% of touches (elite players at moderate velocity) |
| 1 | LOOSE_BALL | ~25% of touches |
| 2 | DEFLECTION | ~10% of touches |
| 3 | INTERCEPTION | ~10% of touches |

**Note:** Frequency estimates are design targets, not measured data. Verify against test
scenario outcomes in Section 5 once implemented.

---

## 4.3 Core Structs

All structs in this section are value types (`struct` in C#). They are stack-allocated per
evaluation and never heap-allocated in the hot path. No struct in this section may contain
a reference type field — doing so would force heap allocation and violate the memory budget
(§4.7).

### 4.3.1 FirstTouchContext

`FirstTouchContext` is the complete input package for a first touch evaluation. It is
assembled by the Collision System and passed to `IFirstTouchSystem.EvaluateFirstTouch()`.
All data needed for the evaluation must be present in this struct — the First Touch system
must not reach back into game state at evaluation time.

```csharp
/// <summary>
/// Complete input context for a first touch evaluation.
///
/// Assembled by the Collision System immediately after agent-ball contact
/// is detected. All fields must be populated before calling EvaluateFirstTouch().
///
/// Lifecycle: Created per-evaluation; stack-allocated; not persisted.
/// Size: ~88 bytes (see §6.4.1 for authoritative field-by-field breakdown;
///   §2.5.2 originally estimated 128 bytes — corrected per audit).
///
/// IMPORTANT: This struct must not contain reference types. If a field seems
/// to require a reference (e.g., an array of opponent positions), use a fixed-
/// size alternative or move the data into a separate query result struct that
/// is populated before building FirstTouchContext.
/// </summary>
public struct FirstTouchContext
{
    // ================================================================
    // AGENT IDENTIFICATION
    // ================================================================

    /// <summary>
    /// Unique identifier for the agent receiving the ball.
    /// Range: [0, MAX_AGENTS_IN_MATCH). Must be valid at time of evaluation.
    /// Source: AgentBallCollisionData.AgentID (Collision System §4.2.6)
    /// </summary>
    public int AgentID;

    /// <summary>
    /// Team identifier for the receiving agent.
    /// Values: 0 = home team, 1 = away team.
    /// Source: AgentBallCollisionData.TeamID (Collision System §4.2.6)
    /// </summary>
    public int TeamID;

    // ================================================================
    // AGENT ATTRIBUTES (from PlayerAttributes, Agent Movement §3.5.6)
    // ================================================================

    /// <summary>
    /// Agent's Technique attribute. Range: [1, 20]. Integer.
    /// Weighted at TECHNIQUE_WEIGHT = 0.70 in control quality formula.
    /// Source: PlayerAttributes.Technique (Agent Movement Spec §3.5.6)
    /// </summary>
    public int Technique;

    /// <summary>
    /// Agent's First Touch attribute. Range: [1, 20]. Integer.
    /// Weighted at FIRST_TOUCH_WEIGHT = 0.30 in control quality formula.
    /// Source: PlayerAttributes.FirstTouch (Agent Movement Spec §3.5.6)
    /// </summary>
    public int FirstTouchAttribute;

    // ================================================================
    // AGENT PHYSICAL STATE (world-space, XY = pitch plane, Z = up)
    // ================================================================

    /// <summary>
    /// Agent's world-space position at the moment of contact.
    /// Z component: agent's feet height (typically 0.0 for grounded agents).
    /// Coordinate system: Ball Physics §3.1.1.
    /// Source: AgentPhysicalState.Position (Agent Movement Spec §3.5.4)
    /// </summary>
    public Vector3 AgentPosition;

    /// <summary>
    /// Agent's world-space velocity at the moment of contact. Metres per second.
    /// Used to compute MovementDifficulty in control quality formula (§3.1).
    /// Source: AgentPhysicalState.Velocity (Agent Movement Spec §3.5.4)
    /// </summary>
    public Vector3 AgentVelocity;

    /// <summary>
    /// Agent's facing direction. Unit vector in XY plane (Z = 0).
    /// Used for half-turn orientation detection (§3.6) and default touch direction.
    /// Source: AgentPhysicalState.FacingDirection (Agent Movement Spec §3.5.4)
    /// </summary>
    public Vector3 AgentFacing;

    // ================================================================
    // MOVEMENT INTENT (from Decision Tree / Movement Command)
    // ================================================================

    /// <summary>
    /// The agent's intended touch direction, derived from their movement command.
    /// Unit vector in XY plane (Z = 0).
    ///
    /// If the agent has a movement target, this vector points from AgentPosition
    /// toward that target. If no target (stationary), defaults to AgentFacing.
    ///
    /// This is NOT the ball's incoming direction — it is where the agent WANTS
    /// the ball to go after the touch.
    ///
    /// Source: Derived from MovementCommand.TargetPosition (Agent Movement Spec §3.3)
    /// </summary>
    public Vector3 IntendedTouchDirection;

    /// <summary>
    /// True if the agent has an active movement target at time of contact.
    /// When false, IntendedTouchDirection defaults to AgentFacing.
    /// Source: MovementCommand.HasTargetPosition (Agent Movement Spec §3.3)
    /// </summary>
    public bool HasMovementTarget;

    // ================================================================
    // BALL STATE AT CONTACT MOMENT
    // ================================================================

    /// <summary>
    /// Ball's world-space position at the moment of contact (ball centre).
    /// Z = 0.11m for ground contact (Ball.RADIUS, Ball Physics §3.1.2).
    /// Source: BallState.Position (Ball Physics Spec §3.1)
    /// </summary>
    public Vector3 BallPosition;

    /// <summary>
    /// Ball's world-space velocity at the moment of contact. Metres per second.
    /// Magnitude = incoming ball speed used in velocity difficulty calculation (§3.1).
    /// Source: BallState.Velocity (Ball Physics Spec §3.1)
    /// </summary>
    public Vector3 BallVelocity;

    /// <summary>
    /// Ball's height above pitch surface (ball centre Z minus Ball.RADIUS).
    /// Cached separately to avoid repeated computation.
    /// Range: [0.0, ∞). Typically 0.0 for ground balls.
    ///
    /// Height guard: if BallHeight > GROUND_CONTROL_HEIGHT (0.50m),
    /// First Touch system must reject this evaluation (route to Heading Mechanics).
    /// Enforced in §3.4.3.
    /// </summary>
    public float BallHeight;

    /// <summary>
    /// True if the ball was in AIRBORNE state (Ball Physics §3.1.3) at contact.
    /// Distinct from BallHeight > 0 — a ball can be slightly airborne near ground level.
    /// Used to apply additional difficulty modifier in §3.1 for balls in flight.
    /// Source: BallState.CurrentState == BallStateType.AIRBORNE
    /// </summary>
    public bool BallIsAirborne;

    // ================================================================
    // PRE-COMPUTED PRESSURE CONTEXT
    // Note: Pressure is pre-computed by PressureEvaluator before building
    // FirstTouchContext. This avoids reference-type fields (opponent lists)
    // inside the struct. See §3.5 for computation details.
    // ================================================================

    /// <summary>
    /// Pre-computed pressure scalar from PressureEvaluator (§3.5).
    /// Range: [0.0, 1.0]. 0.0 = no pressure; 1.0 = maximum pressure.
    /// Applied in control quality formula at Step 7 (§3.1.1).
    /// </summary>
    public float PressureScalar;

    /// <summary>
    /// True if at least one opponent is within PRESSURE_RADIUS (3.0m) at contact time.
    /// Shortcut flag to avoid re-querying spatial data for possession logic (§3.4).
    /// </summary>
    public bool HasNearbyOpponent;

    /// <summary>
    /// Distance to the nearest opponent in metres. Positive infinity if no opponent nearby.
    /// Used in possession state machine to determine if INTERCEPTION is reachable (§3.4.2).
    /// </summary>
    public float NearestOpponentDistance;

    // ================================================================
    // ORIENTATION CONTEXT (pre-computed by OrientationDetector)
    // ================================================================

    /// <summary>
    /// True if the agent's facing direction is between 30° and 60° from the
    /// ball's incoming direction (the half-turn window, §3.6.2).
    ///
    /// When true, orientationBonus = HALF_TURN_BONUS = +0.15 is applied
    /// as a multiplicative boost on the normalised attribute score (§3.1.1 Step 3).
    ///
    /// Pre-computed before building this struct to keep EvaluateFirstTouch() pure.
    /// Source: OrientationDetector.IsHalfTurnOriented() (§3.6)
    /// </summary>
    public bool IsHalfTurnOriented;

    // ================================================================
    // ROLE FLAGS
    // ================================================================

    /// <summary>
    /// True if the receiving agent is a goalkeeper.
    /// Informational flag; does not alter First Touch calculation in Stage 0.
    /// Included to satisfy Critical Issue #1 (Outline §Critical Issues):
    /// goalkeeper foot control uses this system normally; catching/diving is
    /// out of scope (Goalkeeper Mechanics Spec #10).
    ///
    /// Stage 1+: May be used to apply goalkeeper-specific attribute overrides.
    /// Source: AgentBallCollisionData.IsGoalkeeper (Collision System §4.2.6)
    /// </summary>
    public bool IsGoalkeeper;
}
```

### 4.3.2 FirstTouchResult

`FirstTouchResult` is the complete output of a first touch evaluation. It contains everything
downstream systems need: the new ball state, the possession outcome, and any flags required
to update game state. It is returned by `IFirstTouchSystem.EvaluateFirstTouch()`.

```csharp
/// <summary>
/// Complete output of a first touch evaluation.
///
/// Produced by the evaluation pipeline and consumed by:
///   - Ball Physics: IBallPhysicsSystem.SetBallState() called with NewBallPosition
///     and NewBallVelocity (§4.5.2)
///   - Agent Movement: activates dribbling state if TriggeredDribblingState
///   - BallState.State: written directly by ApplyTouchResult() based on PossessionOutcome
///   - Event System (Stage 1+): deferred — FirstTouchEvent stub pending Spec #17
///
/// Lifecycle: Produced per-evaluation; stack-allocated; not persisted.
/// Size: ~56 bytes (see §4.7.2 for field-by-field breakdown).
/// </summary>
public struct FirstTouchResult
{
    // ================================================================
    // QUALITY METRICS (diagnostic and downstream input)
    // ================================================================

    /// <summary>
    /// Final control quality value after all modifiers. Range: [0.0, 1.0].
    /// This is 'q' from the governing formula (§3.1.1).
    ///
    /// Interpretation:
    ///   q ≥ 0.85 : Perfect touch — ball within 0.30m
    ///   q ≥ 0.60 : Good touch   — ball within 0.60m
    ///   q ≥ 0.35 : Poor touch   — ball within 1.20m
    ///   q < 0.35 : Heavy touch  — ball up to 2.00m away
    ///
    /// Used by tests, replay, and statistics. Not used directly by Ball Physics
    /// (which consumes NewBallPosition/NewBallVelocity).
    /// </summary>
    public float ControlQuality;

    /// <summary>
    /// Touch radius 'r' in metres. Range: [0.10, 2.00].
    /// Distance from AgentPosition to NewBallPosition.
    ///
    /// This is the distance the ball travels from the agent after the touch.
    /// A radius of 0.10m is a perfectly cushioned touch that drops at the feet;
    /// 2.0m is the maximum escape distance for a heavy touch.
    ///
    /// See §3.2 for the piecewise interpolation that maps ControlQuality → TouchRadius.
    /// </summary>
    public float TouchRadius;

    // ================================================================
    // BALL OUTCOME (authoritative new ball state)
    // ================================================================

    /// <summary>
    /// New ball position after the touch, in world space (ball centre).
    /// Computed in §3.3. Ball Physics must place the ball at this position
    /// via IBallPhysicsSystem.SetBallState() (§4.5.2).
    ///
    /// Invariants:
    ///   - Must be within pitch boundaries
    ///   - Z must be ≥ Ball.RADIUS (0.11m) — ball cannot go below surface
    ///   - |NewBallPosition - AgentPosition| must equal TouchRadius (within float epsilon)
    /// </summary>
    public Vector3 NewBallPosition;

    /// <summary>
    /// New ball velocity after the touch, in metres per second.
    /// Computed in §3.3.5. Ball Physics applies this as the new ball velocity
    /// via IBallPhysicsSystem.SetBallState() (§4.5.2).
    ///
    /// Invariants:
    ///   - Magnitude must be ≤ incoming ball speed × MOMENTUM_RETENTION_MAX (0.80)
    ///     Rationale: energy is lost at contact; the ball cannot gain speed.
    ///   - For CONTROLLED outcome: magnitude ≤ DRIBBLE_MAX_SPEED (5.5 m/s)
    ///   - For DEFLECTION outcome: magnitude reflects retained momentum (§3.3.6)
    /// </summary>
    public Vector3 NewBallVelocity;

    // ================================================================
    // POSSESSION OUTCOME
    // ================================================================

    /// <summary>
    /// The possession outcome of this touch.
    /// See TouchResult enum (§4.2.1) for full state definitions.
    ///
    /// Used by ApplyTouchResult() to determine:
    ///   - Which BallStateType to write to BallState.State
    ///   - Whether to activate DribblingModifier via IAgentMovementSystem
    ///
    /// All four states are possible; see §3.4 for priority ordering.
    /// </summary>
    public TouchResult PossessionOutcome;

    /// <summary>
    /// AgentID of the agent who now possesses the ball.
    /// Value: valid AgentID if PossessionOutcome == CONTROLLED.
    /// Value: -1 (AGENT_ID_NONE) for LOOSE_BALL, DEFLECTION, INTERCEPTION.
    ///
    /// INTERCEPTION: The intercepting agent does not possess the ball in this
    /// frame. Their possession is resolved in Frame N+1 (§3.4.5).
    /// </summary>
    public int PossessingAgentID;

    /// <summary>
    /// AgentID of the opponent who will evaluate interception in the next frame.
    /// Value: valid AgentID only when PossessionOutcome == INTERCEPTION.
    /// Value: -1 (AGENT_ID_NONE) for all other outcomes.
    ///
    /// The Collision System uses this ID to prioritise the next frame's
    /// agent-ball contact resolution for the intercepting agent.
    /// </summary>
    public int InterceptingAgentID;

    // ================================================================
    // STATE CHANGE FLAGS
    // ================================================================

    /// <summary>
    /// True if Agent Movement must activate the DribblingModifier for PossessingAgentID.
    /// Only true when PossessionOutcome == CONTROLLED.
    ///
    /// When true: call IAgentMovementSystem.SetDribblingState(PossessingAgentID, true).
    /// See Agent Movement Spec §6.1.2 for DribblingModifier definition.
    /// </summary>
    public bool TriggeredDribblingState;

    // ================================================================
    // DIAGNOSTIC FIELDS (for logging and testing; not used in game logic)
    // ================================================================

    /// <summary>
    /// Magnitude of ball.Velocity at the moment of contact, in m/s.
    /// Stored for event logging and replay. Not used in downstream game logic.
    /// Matches ball.Speed from BallState at time of evaluation.
    /// </summary>
    public float IncomingBallSpeed;

    /// <summary>
    /// The effective attribute score used in control quality calculation.
    /// This is the weighted, pressure-adjusted, orientation-adjusted value
    /// before division by velocity/movement difficulty. Range: [0.0, 20.0].
    ///
    /// Stored for debugging and designer tuning. Allows inspection of
    /// "how much did pressure reduce the effective attribute?"
    /// Formula: EffectiveAttribute = WeightedAttr × OrientationMult × PressureMult
    /// See §3.1.1, Steps 1–3.
    /// </summary>
    public float EffectiveAttribute;
}
```

### 4.3.3 Sentinel Values

Sentinel values for "not applicable" fields in `FirstTouchResult`:

```csharp
// Defined in FirstTouchConstants.cs (§4.4)
// Used when a field does not apply to the current outcome
public const int AGENT_ID_NONE = -1;
```

---

