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

## 4.4 Constants Class

All tunable parameters are consolidated in `FirstTouchConstants`. No magic numbers are
permitted in any other First Touch file. Every constant includes its source or derivation
rationale. Constants are frozen at Stage 0 approval — changes in Stage 1+ require a full
regression test pass (§5 scenarios) before merging.

```csharp
/// <summary>
/// All tunable constants for First Touch Mechanics.
/// Single source of truth. No other First Touch file may use inline numeric literals.
///
/// Modification policy:
///   - Stage 0: Frozen after approval. Changes require spec revision + regression.
///   - Stage 1+: Extended (not altered) unless a documented balance decision is made.
///   - All changes must be recorded in the spec changelog.
///
/// Organisation: Constants are grouped by the sub-system that owns them,
/// matching the §3.x section structure.
/// </summary>
public static class FirstTouchConstants
{
    // ================================================================
    // §3.1 CONTROL QUALITY FORMULA — ATTRIBUTE WEIGHTS
    // ================================================================

    /// <summary>
    /// Fraction of control quality derived from Technique attribute.
    /// Rationale: Technique governs general ball mastery; it is the dominant factor
    /// in ball control in real football. 70/30 split validated against FM-style attribute
    /// systems where Technique is typically the primary dribbling attribute.
    /// Source: Master Vol 1 §6.4 (attribute weighting design decision)
    /// </summary>
    public const float TECHNIQUE_WEIGHT = 0.70f;

    /// <summary>
    /// Fraction of control quality derived from First Touch attribute.
    /// Complement of TECHNIQUE_WEIGHT: 0.70 + 0.30 = 1.00.
    /// Rationale: First Touch is the specialist attribute for receiving the ball;
    /// important but subordinate to general technique.
    /// Source: Master Vol 1 §6.4
    /// </summary>
    public const float FIRST_TOUCH_WEIGHT = 0.30f;

    /// <summary>
    /// Maximum possible value of weighted attribute sum.
    /// Derivation: (20 × 0.70) + (20 × 0.30) = 14.0 + 6.0 = 20.0
    /// Used as denominator in normalisation step (§3.1.1 Step 2).
    /// Both attributes at maximum (20/20) gives NormAttr = 1.0.
    /// </summary>
    public const float ATTR_MAX = 20.0f;

    /// <summary>
    /// Minimum attribute value, used to guard against uninitialized attributes.
    /// If either attribute is 0 (uninitialised), guard clamps to 1 before calculation.
    /// Attributes are defined in range [1, 20] so 0 indicates a data error.
    /// </summary>
    public const int ATTR_MIN_GUARD = 1;

    // ================================================================
    // §3.1 CONTROL QUALITY FORMULA — VELOCITY DIFFICULTY
    // ================================================================

    /// <summary>
    /// Reference ball speed in m/s. Represents a "typical" match pass.
    /// At this speed, velocity difficulty factor = 1.0 (no extra difficulty).
    /// Below this speed, the ball is easier to control; above, harder.
    /// Source: Empirical — typical pass speeds in professional football range 10–20 m/s.
    ///         15 m/s chosen as midpoint representing a standard ground pass.
    /// Validation: §5.2 scenario VS-001 (elite player, 18 m/s ball) should yield q ≈ 0.80.
    /// </summary>
    public const float VELOCITY_REFERENCE = 15.0f; // m/s

    /// <summary>
    /// Maximum velocity difficulty multiplier cap.
    /// Derivation: 60 m/s (physically impossible ball speed) / 15 m/s = 4.0.
    /// In practice, balls above 30 m/s are considered "thunderbolt" (§3.3.7).
    /// This cap prevents division from returning near-zero quality for edge cases.
    /// </summary>
    public const float VELOCITY_MAX_FACTOR = 4.0f;

    /// <summary>
    /// Minimum velocity used in difficulty calculation. Guards against division by near-zero
    /// when receiving a stationary or very slow ball.
    /// A ball rolling at 0.1 m/s poses no velocity challenge; difficulty = 0.1/15 ≈ 0.007.
    /// Clamp ensures VelDifficulty ≥ 0.1 (100× easier than reference speed).
    /// </summary>
    public const float VELOCITY_MIN = 0.5f; // m/s

    // ================================================================
    // §3.1 CONTROL QUALITY FORMULA — MOVEMENT DIFFICULTY
    // ================================================================

    /// <summary>
    /// Top sprint speed used as reference for movement difficulty normalisation.
    /// Must match Agent Movement Spec §3.5.2 top sprint speed: 7.0 m/s.
    ///
    /// CROSS-SPEC DEPENDENCY: If Agent Movement §3.5.2 changes the top sprint speed,
    /// this constant must be updated in sync. A mismatch would cause movement difficulty
    /// to be incorrectly calculated for sprinting agents.
    /// </summary>
    public const float MOVEMENT_REFERENCE = 7.0f; // m/s — matches Agent Movement §3.5.2

    /// <summary>
    /// Penalty factor for receiving while moving. Added to the movement difficulty multiplier.
    /// Formula: MoveDifficulty = 1.0 + (agentSpeed / MOVEMENT_REFERENCE) × MOVEMENT_PENALTY
    /// At full sprint (7 m/s): MoveDifficulty = 1.0 + 1.0 × 0.50 = 1.50
    /// Rationale: A sprinting player is 50% harder to control the ball for vs. standing still.
    ///            This aligns with real football coaching ("set your feet before receiving").
    /// Source: Empirical gameplay calibration. Adjust if sprint reception feels too punishing.
    /// </summary>
    public const float MOVEMENT_PENALTY = 0.50f;

    // ================================================================
    // §3.1 CONTROL QUALITY FORMULA — ORIENTATION BONUS
    // ================================================================

    /// <summary>
    /// Multiplicative bonus applied when the agent is in the half-turn orientation window.
    /// Applied as: AttrWithBonus = NormAttr × (1.0 + HALF_TURN_BONUS)
    /// Value: 15% effective attribute increase for half-turn reception.
    /// Source: Master Vol 1 §6 (half-turn mechanics design intent: +15% orientation bonus)
    ///
    /// CROSS-SPEC DEPENDENCY: §3.6 OrientationDetector must produce IsHalfTurnOriented
    /// using the window defined in HALF_TURN_ANGLE_MIN and HALF_TURN_ANGLE_MAX.
    /// </summary>
    public const float HALF_TURN_BONUS = 0.15f;

    /// <summary>
    /// Minimum angle (degrees) from incoming ball direction for half-turn detection.
    /// The half-turn window is [HALF_TURN_ANGLE_MIN, HALF_TURN_ANGLE_MAX].
    /// An agent facing 30°–60° from the ball's approach vector receives the bonus.
    /// Rationale: Below 30°, the agent is facing the ball directly (no half-turn).
    /// Source: §3.6.2 half-turn window definition.
    /// </summary>
    public const float HALF_TURN_ANGLE_MIN = 30.0f; // degrees

    /// <summary>
    /// Maximum angle (degrees) from incoming ball direction for half-turn detection.
    /// Above 60°, the agent is not meaningfully angled for half-turn benefit.
    /// Source: §3.6.2 half-turn window definition; Master Vol 1 §6.
    /// </summary>
    public const float HALF_TURN_ANGLE_MAX = 60.0f; // degrees

    // ================================================================
    // §3.1 CONTROL QUALITY FORMULA — PRESSURE DEGRADATION
    // ================================================================

    /// <summary>
    /// Maximum degradation of control quality from full pressure.
    /// Formula: q = RawQuality × (1.0 - pressureScalar × PRESSURE_WEIGHT)
    /// At pressureScalar = 1.0: quality reduced by 40%.
    /// Rationale: Even under maximum pressure, a technically excellent player
    ///            retains 60% of their capability. Complete inability (0% quality)
    ///            under pressure would be unrealistic.
    /// Source: Empirical gameplay calibration. Paired with PRESSURE_SATURATION.
    /// </summary>
    public const float PRESSURE_WEIGHT = 0.40f;

    // ================================================================
    // §3.1 CONTROL QUALITY FORMULA — RESULT THRESHOLDS
    // ================================================================

    /// <summary>
    /// Control quality threshold for CONTROLLED possession outcome.
    /// q ≥ CONTROLLED_THRESHOLD AND r ≤ CONTROLLED_RADIUS → CONTROLLED.
    /// Rationale: 0.55 represents a professional-quality reception under typical conditions.
    ///            Elite players (Technique 18+) at moderate speed (15 m/s) with no pressure
    ///            should consistently exceed this threshold.
    /// Source: §2.4.1, FR-04 (§2.3.4).
    /// Validation target: CONTROLLED in ≥ 80% of cases when Technique ≥ 15 and ball ≤ 15 m/s.
    /// See §2.6 FM-02.
    /// </summary>
    public const float CONTROLLED_THRESHOLD = 0.55f;

    /// <summary>
    /// Maximum touch radius (m) for CONTROLLED outcome.
    /// Ball must land within this distance of AgentPosition for CONTROLLED.
    /// Beyond this radius, outcome degrades to LOOSE_BALL even if quality is acceptable.
    /// Value = bottom of Perfect + Good bands combined = 0.60m.
    /// Source: §3.2.1 touch radius band definitions; §3.4.2.
    /// </summary>
    public const float CONTROLLED_RADIUS = 0.60f;

    // ================================================================
    // §3.2 TOUCH RADIUS — BAND THRESHOLDS (m)
    // ================================================================

    /// <summary>
    /// Maximum displacement for Perfect band (q ≥ 0.85).
    /// Ball lands within 0.30m of agent — immediately at feet.
    /// Source: Master Vol 1 §6 touch radius design: 0.3m / 0.6m / 1.2m / 2.0m.
    /// </summary>
    public const float RADIUS_PERFECT = 0.30f; // m

    /// <summary>
    /// Maximum displacement for Good band (q ≥ 0.60, q < 0.85).
    /// Ball lands within 0.60m — one comfortable stride to recover.
    /// Source: Master Vol 1 §6.
    /// </summary>
    public const float RADIUS_GOOD = 0.60f; // m

    /// <summary>
    /// Maximum displacement for Poor band (q ≥ 0.35, q < 0.60).
    /// Ball lands within 1.20m — agent must move to recover.
    /// Source: Master Vol 1 §6.
    /// </summary>
    public const float RADIUS_POOR = 1.20f; // m

    /// <summary>
    /// Maximum displacement for Heavy band (q < 0.35).
    /// Ball escapes up to 2.00m — agent cannot immediately recover.
    /// Source: Master Vol 1 §6.
    /// </summary>
    public const float RADIUS_HEAVY = 2.00f; // m

    /// <summary>
    /// Minimum touch radius (Perfect band lower bound).
    /// Even a perfect touch has some minimal displacement.
    /// Prevents the ball snapping to exactly AgentPosition.
    /// </summary>
    public const float RADIUS_MIN = 0.10f; // m

    /// <summary>
    /// Velocity scaling factor for touch radius.
    /// High-speed balls increase the touch radius beyond the base band value.
    /// Formula from §3.2.3: r += (ball.Speed / VELOCITY_REFERENCE) × VELOCITY_RADIUS_FACTOR
    /// At 30 m/s: adds 0.50m to base radius (30/15 × 0.25).
    /// Source: §3.2.3 empirical calibration.
    /// </summary>
    public const float VELOCITY_RADIUS_FACTOR = 0.25f;

    // ================================================================
    // §3.3 BALL DISPLACEMENT
    // ================================================================

    /// <summary>
    /// Maximum speed of the ball when in CONTROLLED dribbling state. Metres per second.
    /// Ball velocity is clamped to this value when PossessionOutcome == CONTROLLED.
    /// Rationale: A player in close control cannot pass themselves the ball at sprint speed;
    ///            the ball stays close. 5.5 m/s ≈ comfortable dribbling pace.
    /// Source: §3.3.5 controlled touch velocity clamping.
    /// </summary>
    public const float DRIBBLE_MAX_SPEED = 5.5f; // m/s

    /// <summary>
    /// Maximum fraction of incoming ball momentum retained after touch.
    /// Ball velocity magnitude after touch ≤ incoming speed × MOMENTUM_RETENTION_MAX.
    /// Rationale: Energy is always lost at contact (foot compression, friction).
    ///            A ball cannot gain speed through a first touch.
    /// Source: §3.3 displacement; FR-03 acceptance criteria.
    /// </summary>
    public const float MOMENTUM_RETENTION_MAX = 0.80f;

    /// <summary>
    /// Fraction of ball momentum retained in a DEFLECTION outcome.
    /// Ball retains 50% of incoming speed, deflected at contact angle.
    /// Rationale: A deflection (incidental contact) loses significant energy
    ///            but the ball continues moving in roughly its original direction.
    /// Source: §3.3.6 deflection velocity calculation.
    /// </summary>
    public const float MOMENTUM_RETENTION_DEFLECTION = 0.50f;

    /// <summary>
    /// Maximum angular error (degrees) between intended and actual touch direction.
    /// At q = 0.0: angular error is ±MAX_TOUCH_ANGLE_ERROR (±45°).
    /// At q = 1.0: angular error is 0° (perfect direction).
    /// Error interpolates linearly between these bounds as (1 - q) × MAX_TOUCH_ANGLE_ERROR.
    ///
    /// Rationale for 45°: Beyond 45° the ball is not travelling toward the intended target.
    ///                    Physically implausible for a foot-ball contact to produce >45° error.
    /// Source: FR-03 rationale; §3.3.4.
    /// </summary>
    public const float MAX_TOUCH_ANGLE_ERROR = 45.0f; // degrees

    // ================================================================
    // §3.3 BALL DISPLACEMENT — THUNDERBOLT HANDLING
    // ================================================================

    /// <summary>
    /// Ball speed threshold for "thunderbolt" classification. Metres per second.
    /// Balls above this speed receive an additional control quality cap.
    /// Definition: 28 m/s ≈ 100 km/h — a very hard shot or clearance.
    /// Source: §3.3.7 thunderbolt cap; §3.7 event emitter (THUNDERBOLT event flag).
    /// </summary>
    public const float THUNDERBOLT_SPEED = 28.0f; // m/s

    /// <summary>
    /// Maximum control quality cap applied to thunderbolt balls.
    /// Even a perfect player (Technique 20, First Touch 20) cannot achieve q > 0.30
    /// on a thunderbolt. Rationale: 100 km/h balls are physically uncontrollable;
    /// the best realistic outcome is deflection, not clean control.
    /// Source: §3.3.7; worked example in Outline §3.7.3.
    /// </summary>
    public const float THUNDERBOLT_QUALITY_CAP = 0.30f;

    // ================================================================
    // §3.4 POSSESSION STATE MACHINE — THRESHOLDS
    // ================================================================

    /// <summary>
    /// Minimum touch radius (m) for INTERCEPTION to be possible.
    /// Ball must escape at least this far before an opponent can intercept.
    /// At r < INTERCEPTION_THRESHOLD, the receiving agent recovers their own touch.
    /// Value = bottom of Poor band (1.20m); ball has genuinely escaped the agent.
    /// Source: §3.4.2; priority ordering note.
    /// </summary>
    public const float INTERCEPTION_THRESHOLD = 1.20f; // m

    /// <summary>
    /// Maximum distance (m) from new ball position at which an opponent can intercept.
    /// If the nearest opponent is beyond this radius, INTERCEPTION cannot occur.
    /// Value: 2.50m — an opponent must be physically close to exploit a heavy touch.
    /// Rationale: A player 3m+ away cannot reach a ball that has just been touched
    ///            in a single movement; 2.5m is the realistic interception range.
    /// Source: §3.4.2; FR-04.
    /// </summary>
    public const float INTERCEPTION_RADIUS = 2.50f; // m

    /// <summary>
    /// Minimum control quality for an INTERCEPTION to count as deliberate.
    /// The intercepting agent must have at least this quality threshold to be
    /// credited with an interception (vs. a lucky deflection).
    ///
    /// Lower than CONTROLLED_THRESHOLD (0.55) by design: intercepting a pass
    /// with partial control (diverting it away from opponent) is still a successful
    /// interception even without clean possession.
    /// Source: FR-04 INTERCEPTION threshold rationale; §2.4.4.
    /// </summary>
    public const float INTERCEPTION_QUALITY_MIN = 0.35f;

    /// <summary>
    /// Minimum touch radius (m) for DEFLECTION classification.
    /// A ball that escapes this far AND has momentum alignment ≥ DEFLECTION_ALIGNMENT_MIN
    /// (and no opponent in interception range) is classified as DEFLECTION.
    /// Source: §3.4.2.
    /// </summary>
    public const float DEFLECTION_THRESHOLD = 1.50f; // m

    /// <summary>
    /// Minimum cosine of angle between newBallVelocity and incoming ball.Velocity
    /// for DEFLECTION classification.
    /// cos(45°) ≈ 0.707. Ball direction must be within 45° of incoming to be a deflection.
    /// Beyond 45° it's a redirect, classified as LOOSE_BALL.
    /// Source: §3.4.2.
    /// </summary>
    public const float DEFLECTION_ALIGNMENT_MIN = 0.70f;

    // ================================================================
    // §3.5 PRESSURE EVALUATION
    // ================================================================

    /// <summary>
    /// Maximum radius (m) within which opponents contribute to pressure scalar.
    /// Opponents beyond this distance do not affect pressure calculation.
    /// Value: 3.0m — realistic immediate pressure zone in professional football.
    /// Source: Empirical; validated against §5.3 pressure test scenarios.
    /// </summary>
    public const float PRESSURE_RADIUS = 3.0f; // m

    // ================================================================
    // §3.3 BALL DISPLACEMENT — BOUNDARY CLAMPING
    // ================================================================

    /// <summary>
    /// Pitch half-length used for ball displacement boundary clamping.
    /// Must match Ball Physics §3.1.1 pitch dimensions.
    /// Value: 52.5m — half of standard FIFA pitch length (105m).
    /// </summary>
    public const float PITCH_HALF_LENGTH = 52.5f; // m

    /// <summary>
    /// Pitch half-width used for ball displacement boundary clamping.
    /// Must match Ball Physics §3.1.1 pitch dimensions.
    /// Value: 34.0m — half of standard FIFA pitch width (68m).
    /// </summary>
    public const float PITCH_HALF_WIDTH = 34.0f; // m

    // ================================================================
    // §3.4.3 AERIAL HEIGHT GUARD (Critical Issue #2 resolution)
    // ================================================================

    /// <summary>
    /// Maximum ball height (m) for First Touch evaluation eligibility.
    /// Ball centre above this height is routed to Heading Mechanics (Spec #9).
    /// Ball centre at or below this height is processed by First Touch.
    ///
    /// Value: 0.50m ≈ approximate chest height for a crouching player.
    /// Represents the boundary between ground control and aerial mechanics.
    ///
    /// CRITICAL: This is ball CENTRE height, not contact point height.
    /// Ball centre at 0.50m means ball surface is at 0.39m (0.50 - 0.11 RADIUS).
    ///
    /// Source: Outline Critical Issue #2 resolution; §3.4.3 height guard.
    /// Cross-reference: Heading Mechanics Spec #9 must use the same constant.
    /// </summary>
    public const float GROUND_CONTROL_HEIGHT = 0.50f; // m (ball centre)

    // ================================================================
    // SENTINEL / UTILITY
    // ================================================================

    /// <summary>
    /// Sentinel value for "no agent" in result fields (PossessingAgentID, etc.).
    /// Used when an ID field is not applicable for the current outcome.
    /// </summary>
    public const int AGENT_ID_NONE = -1;

    /// <summary>
    /// Floating-point epsilon for equality comparisons in this system.
    /// Standard Mathf.Epsilon (1e-45) is too small for game-scale distances.
    /// This value (1e-4 m = 0.1 mm) is appropriate for metre-scale pitch coordinates.
    /// </summary>
    public const float COMPARISON_EPSILON = 0.0001f;

    // ================================================================
    // REMOVED IN v1.1 (ERR-004)
    // EVENT_QUEUE_CAPACITY = 64 — removed with IFirstTouchEventQueue.
    // Event emission is deferred pending Event System Spec #17.
    // ================================================================
}
```

---

## 4.5 Interface Contracts

Interfaces define the API boundaries between First Touch Mechanics and other systems.
They are the contracts that allow unit testing with mocks and enable future system
replacements without coupling.

**v1.1 interface summary (revised from v1.0):**

| Interface | Direction | Status | Notes |
|-----------|-----------|--------|-------|
| `IFirstTouchSystem` | Consumed by Collision System | Retained (§4.5.1) | Unchanged |
| `IBallPhysicsSystem` | First Touch → Ball Physics | **Replaced** (§4.5.2) | Was `IBallPhysicsCallback` (4 methods); now single `SetBallState()` |
| `IAgentMovementSystem` | First Touch → Agent Movement | Retained (§4.5.3) | Unchanged |
| `IPossessionManager` | ~~First Touch → Possession~~ | **Removed** (ERR-004) | Direct `BallState` write instead |
| `IFirstTouchEventQueue` | ~~First Touch → Event System~~ | **Removed** (ERR-004) | Deferred — Spec #17 not yet designed |

### 4.5.1 IFirstTouchSystem

The main entry point. The Collision System holds a reference to this interface and calls
`EvaluateFirstTouch()` on every detected agent-ball contact.

```csharp
/// <summary>
/// Main entry point for First Touch evaluation.
/// Called by Collision System immediately after agent-ball contact is detected.
///
/// Implementer: FirstTouchSystem.cs
/// Consumer: Collision System (Spec #3)
/// Thread safety: Not required in Stage 0 (single-threaded physics tick).
/// </summary>
public interface IFirstTouchSystem
{
    /// <summary>
    /// Evaluates a first touch and returns the complete result.
    ///
    /// IMPORTANT: This method must NOT modify game state. All state changes
    /// are deferred to ApplyTouchResult(). This separation allows the result
    /// to be inspected or logged before application, and simplifies testing.
    ///
    /// The method must be deterministic: identical inputs must produce
    /// bitwise-identical outputs across all platforms (Master Vol 1 §1.3).
    ///
    /// Performance target: < 0.05ms per call (§2.5, §6).
    /// </summary>
    /// <param name="context">Complete evaluation inputs. All fields must be populated.</param>
    /// <returns>Complete evaluation result. No game state is modified.</returns>
    FirstTouchResult EvaluateFirstTouch(FirstTouchContext context);

    /// <summary>
    /// Applies the result of a touch evaluation to game state.
    /// Called immediately after EvaluateFirstTouch() in normal operation.
    ///
    /// Separation rationale: Allows replay systems to evaluate without applying,
    /// and allows tests to inspect results before side effects occur.
    ///
    /// Calls made internally by this method:
    ///   1. IBallPhysicsSystem.SetBallState() — updates ball position and velocity (§4.5.2)
    ///   2. BallState.State write — sets BallStateType based on PossessionOutcome (§4.5.4)
    ///   3. IAgentMovementSystem.SetDribblingState() — activates dribbling if CONTROLLED (§4.5.3)
    ///   4. [DEFERRED] FirstTouchEvent emission — pending Event System Spec #17 (§4.5.5)
    ///
    /// BallState.State mapping:
    ///   CONTROLLED    → BallStateType.CONTROLLED
    ///   LOOSE_BALL    → BallStateType.ROLLING
    ///   DEFLECTION    → BallStateType.AIRBORNE or ROLLING (determined by NewBallVelocity.z)
    ///   INTERCEPTION  → BallStateType.ROLLING (ball directed toward interceptor)
    /// </summary>
    /// <param name="result">Result from EvaluateFirstTouch() to apply.</param>
    /// <param name="context">Original context (for diagnostic logging).</param>
    void ApplyTouchResult(FirstTouchResult result, FirstTouchContext context);
}
```

### 4.5.2 IBallPhysicsSystem

**v1.1 change (ERR-001):** This interface replaces the v1.0 `IBallPhysicsCallback` which
defined four methods: `OnControlled()`, `OnLooseBall()`, `OnDeflected()`, `OnIntercepted()`.

**Problem with the old design:** All four methods performed the same physical operation —
set ball position and velocity. The method names encoded First Touch's internal `TouchResult`
classification into Ball Physics, forcing Ball Physics to know about `TouchResult` states it
does not need. This is inverted responsibility: the interface was written from First Touch's
perspective ("here's what I'm telling you happened") rather than from Ball Physics'
perspective ("here's what I need from you").

**Correct design:** Ball Physics needs one thing — the new position and velocity. The reason
the state changed is irrelevant to physics simulation. `TouchResult` is First Touch's internal
classification. Ball Physics sets its state accordingly via `BallState.State` (written in
`ApplyTouchResult()` separately — see §4.5.1).

```csharp
/// <summary>
/// Interface for First Touch to write the new ball kinematic state to Ball Physics.
/// Ball Physics implements this; First Touch calls it via dependency injection.
///
/// Implementer: BallPhysicsSystem (Ball Physics Spec #1)
/// Consumer: FirstTouchSystem.ApplyTouchResult()
///
/// Design note (v1.1): This replaces the four-method IBallPhysicsCallback from v1.0.
/// Ball Physics does not need to know the TouchResult — only the resulting position
/// and velocity. The BallStateType (CONTROLLED / ROLLING / AIRBORNE) is written
/// directly to BallState.State in ApplyTouchResult() based on PossessionOutcome.
/// </summary>
public interface IBallPhysicsSystem
{
    /// <summary>
    /// Sets the ball's kinematic state after a first touch evaluation.
    /// Called once per touch, regardless of TouchResult outcome.
    ///
    /// Ball Physics applies the new position and velocity to BallState.
    /// The caller (FirstTouchSystem) has already computed values that satisfy:
    ///   - position within pitch boundaries
    ///   - position.z ≥ Ball.RADIUS (0.11m)
    ///   - velocity.magnitude ≤ incoming speed × MOMENTUM_RETENTION_MAX
    ///
    /// Ball Physics should NOT re-validate these invariants in production.
    /// Debug validation is handled by §4.8 ValidateResult() asserts in First Touch.
    /// </summary>
    /// <param name="newPosition">New ball centre position in world space.</param>
    /// <param name="newVelocity">New ball velocity in metres per second.</param>
    void SetBallState(Vector3 newPosition, Vector3 newVelocity);
}
```

### 4.5.3 IAgentMovementSystem

Called by `ApplyTouchResult()` to activate or deactivate dribbling locomotion modifiers.
**Unchanged from v1.0.**

```csharp
/// <summary>
/// Interface for notifying Agent Movement of dribbling state changes.
/// Agent Movement implements this; First Touch calls it via dependency injection.
///
/// Implementer: AgentMovementSystem (Agent Movement Spec #2)
/// Consumer: FirstTouchSystem.ApplyTouchResult()
/// See: Agent Movement Spec §6.1.2 for DribblingModifier definition.
/// </summary>
public interface IAgentMovementSystem
{
    /// <summary>
    /// Activates or deactivates the DribblingModifier for an agent.
    ///
    /// When isDribbling = true:
    ///   Agent Movement applies DribblingModifier to locomotion calculations.
    ///   Speed, acceleration, and turn rate are penalised (Agent Movement §6.1.2).
    ///   Persists until SetDribblingState(agentID, false) is called.
    ///
    /// When isDribbling = false:
    ///   DribblingModifier is removed. Agent returns to normal locomotion.
    ///   Called when: agent shoots, passes, or ball drifts beyond DRIBBLE_DETACH_RADIUS.
    /// </summary>
    /// <param name="agentID">Agent to update.</param>
    /// <param name="isDribbling">True to activate dribbling penalty; false to remove it.</param>
    void SetDribblingState(int agentID, bool isDribbling);
}
```

### 4.5.4 Possession State — Direct BallState Write

**v1.1 change (ERR-004):** `IPossessionManager` interface removed.

**Problem with `IPossessionManager`:** It defined a contract against a Possession Manager
system that has not been designed. Interfacing against an unspecified system imports
architecture assumptions that may conflict with the final design of that system.
The interface also implied that a separate system owns possession tracking — a design
decision that has not been made.

**Stage 0 approach:** Possession state is encoded directly in `BallState.State`
(Ball Physics §3.1.3). First Touch writes the appropriate `BallStateType` in
`ApplyTouchResult()` based on `PossessionOutcome`:

```
PossessionOutcome     →  BallState.State written by ApplyTouchResult()
──────────────────────────────────────────────────────────────────────
CONTROLLED            →  BallStateType.CONTROLLED
LOOSE_BALL            →  BallStateType.ROLLING
DEFLECTION            →  BallStateType.AIRBORNE  (if NewBallVelocity.z > 0)
                         BallStateType.ROLLING   (if NewBallVelocity.z ≤ 0)
INTERCEPTION          →  BallStateType.ROLLING
```

**Which agent holds possession?** In Stage 0, `BallStateType.CONTROLLED` encodes that
*someone* controls the ball. The specific agent ID is tracked by Agent Movement's internal
state (the agent with `DribblingModifier` active is the possessing agent). No separate
Possession Manager is required for Stage 0 simulation correctness.

**Stage 1+ note:** When a Possession Manager system is designed (as part of the match
statistics or tactical system), the interface contract will be defined at that time with
both sides of the dependency known. `ApplyTouchResult()` will be updated to call the
interface rather than writing directly.

### 4.5.5 Event Emission — Deferred Stub

**v1.1 change (ERR-004):** `IFirstTouchEventQueue` interface removed.

**Problem with `IFirstTouchEventQueue`:** It defined a ring buffer contract against the
Event System (Spec #17), which has not been designed. The interface assumed the Event Bus
dispatches by enqueueing typed structs into per-producer queues — an architecture that may
not match Spec #17's final design.

**Stage 0 approach:** `ApplyTouchResult()` calls a no-op stub method for event emission.
The stub logs diagnostic output in debug builds only:

```csharp
// In FirstTouchSystem.ApplyTouchResult():

// STUB: Event emission deferred pending Event System Spec #17.
// When Spec #17 is designed, replace this with the appropriate interface call.
// Required event data is available in: result (FirstTouchResult) and context (FirstTouchContext).
// Debug builds log the touch outcome to Unity console for development visibility.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
Debug.Log($"[FirstTouch] AgentID={context.AgentID} Outcome={result.PossessionOutcome} " +
          $"q={result.ControlQuality:F3} r={result.TouchRadius:F2}m " +
          $"BallSpeed={result.IncomingBallSpeed:F1}m/s");
#endif
```

The `FirstTouchEvent` struct definition and `FirstTouchEventEmitter.cs` are deferred.
They will be authored when Event System Spec #17 specifies the event dispatch contract.

---

## 4.6 Data Flow Diagram

The following diagram shows how `FirstTouchContext` is built, evaluated, and applied.

```
FRAME N
─────────────────────────────────────────────────────────────────────────────────

Collision System detects agent-ball contact
         │
         ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  BUILD FirstTouchContext                                                     │
│                                                                              │
│  AgentBallCollisionData (Collision System §4.2.6)                           │
│    → AgentID, TeamID, IsGoalkeeper, ContactPoint                            │
│                                                                              │
│  PlayerAttributes (Agent Movement §3.5.6)                                   │
│    → Technique [1-20], FirstTouchAttribute [1-20]                           │
│                                                                              │
│  AgentPhysicalState (Agent Movement §3.5.4)                                 │
│    → AgentPosition, AgentVelocity, AgentFacing                              │
│                                                                              │
│  MovementCommand (Agent Movement §3.3)                                      │
│    → IntendedTouchDirection, HasMovementTarget                              │
│                                                                              │
│  BallState (Ball Physics §3.1)                                              │
│    → BallPosition, BallVelocity, BallHeight, BallIsAirborne                │
│                                                                              │
│  PressureEvaluator.Evaluate() → PressureScalar,                             │
│    HasNearbyOpponent, NearestOpponentDistance                                │
│                                                                              │
│  OrientationDetector.Detect() → IsHalfTurnOriented                          │
└─────────────────────────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  IFirstTouchSystem.EvaluateFirstTouch(context)                              │
│                                                                              │
│  [Height Guard: §3.4.3]                                                     │
│    BallHeight > 0.50m → reject, route to Heading Mechanics                  │
│                                                                              │
│  [ControlQualityCalculator: §3.1]                                           │
│    → q ∈ [0.0, 1.0]                                                         │
│                                                                              │
│  [TouchRadiusCalculator: §3.2]                                              │
│    → r ∈ [0.10m, 2.00m]                                                     │
│                                                                              │
│  [BallDisplacementProcessor: §3.3]                                          │
│    → NewBallPosition, NewBallVelocity                                       │
│                                                                              │
│  [PossessionStateMachine: §3.4]                                             │
│    → PossessionOutcome (CONTROLLED / LOOSE_BALL /                           │
│       DEFLECTION / INTERCEPTION)                                            │
│    → PossessingAgentID, InterceptingAgentID                                 │
│                                                                              │
│  Returns: FirstTouchResult (stack value, no heap)                           │
└─────────────────────────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  IFirstTouchSystem.ApplyTouchResult(result, context)                        │
│                                                                              │
│  1. IBallPhysicsSystem.SetBallState(NewBallPosition, NewBallVelocity)       │
│       → Ball Physics places ball at computed position with computed velocity │
│                                                                              │
│  2. BallState.State = BallStateType.[CONTROLLED|ROLLING|AIRBORNE]           │
│       → Written directly based on PossessionOutcome (§4.5.4)               │
│                                                                              │
│  3. IAgentMovementSystem.SetDribblingState(agentID, isDribbling)            │
│       → DribblingModifier activated (CONTROLLED only); cleared otherwise    │
│                                                                              │
│  4. [DEFERRED] Event emission stub (§4.5.5)                                 │
│       → Debug log in editor/development builds only                         │
│       → Full event emission reconnected when Event System Spec #17 designed │
└─────────────────────────────────────────────────────────────────────────────┘
         │
         ▼  (if INTERCEPTION)

FRAME N+1
─────────────────────────────────────────────────────────────────────────────────

Collision System detects InterceptingAgent contacts ball
         │
         ▼
IFirstTouchSystem.EvaluateFirstTouch(interceptorContext)
    → New evaluation for intercepting agent
    → May resolve as CONTROLLED, LOOSE_BALL, or DEFLECTION
    → INTERCEPTION not re-emitted (§3.4.5 prevents recursion)
```

---

## 4.7 Memory Layout

### 4.7.1 FirstTouchContext Field Sizes

| Field | Type | Bytes | Notes |
|-------|------|-------|-------|
| AgentID | int | 4 | |
| TeamID | int | 4 | |
| Technique | int | 4 | |
| FirstTouchAttribute | int | 4 | |
| AgentPosition | Vector3 | 12 | 3 × float |
| AgentVelocity | Vector3 | 12 | 3 × float |
| AgentFacing | Vector3 | 12 | 3 × float |
| IntendedTouchDirection | Vector3 | 12 | 3 × float |
| HasMovementTarget | bool | 1 (+3 pad) | 4 with alignment |
| BallPosition | Vector3 | 12 | 3 × float |
| BallVelocity | Vector3 | 12 | 3 × float |
| BallHeight | float | 4 | |
| BallIsAirborne | bool | 1 (+3 pad) | 4 with alignment |
| PressureScalar | float | 4 | |
| HasNearbyOpponent | bool | 1 (+3 pad) | 4 with alignment |
| NearestOpponentDistance | float | 4 | |
| IsHalfTurnOriented | bool | 1 (+3 pad) | 4 with alignment |
| IsGoalkeeper | bool | 1 (+3 pad) | 4 with alignment |
| **Total (estimated)** | | **≈ 88 bytes** | Within single L1 cache line |

### 4.7.2 FirstTouchResult Field Sizes

| Field | Type | Bytes | Notes |
|-------|------|-------|-------|
| ControlQuality | float | 4 | |
| TouchRadius | float | 4 | |
| NewBallPosition | Vector3 | 12 | |
| NewBallVelocity | Vector3 | 12 | |
| PossessionOutcome | TouchResult (enum) | 4 | Underlying int |
| PossessingAgentID | int | 4 | |
| InterceptingAgentID | int | 4 | |
| TriggeredDribblingState | bool | 1 (+3 pad) | 4 with alignment |
| IncomingBallSpeed | float | 4 | |
| EffectiveAttribute | float | 4 | |
| **Total (estimated)** | | **≈ 56 bytes** | Well within single L1 cache line |

### 4.7.3 Allocation Policy

| Structure | Allocation | Lifetime | Heap? |
|-----------|-----------|---------|-------|
| FirstTouchContext | Stack | Per evaluation | No |
| FirstTouchResult | Stack | Per evaluation | No |
| FirstTouchConstants | Static | Match lifetime | Static (one-time) |
| IBallPhysicsSystem | Injected ref | Match lifetime | Yes (one-time) |
| IAgentMovementSystem | Injected ref | Match lifetime | Yes (one-time) |

**v1.1 change:** `FirstTouchEvent` pool row removed. Event emission is deferred (§4.5.5).
`IPossessionManager` injected ref row removed. Direct `BallState` write requires no
additional interface reference (§4.5.4).

**Zero heap allocation per touch evaluation.** The interface references injected at system
initialisation are the only heap objects; they are allocated once at match start and retained
for the full 90 minutes. No GC pressure is generated by the evaluation hot path.

---

## 4.8 Validation Rules

These rules must be enforced by the implementation at development-time (asserts) and
optionally at runtime in debug builds. They are not designed for production enforcement —
each assert has a performance cost and the invariants should hold if the system is correct.

### 4.8.1 FirstTouchContext Validation

```csharp
/// <summary>
/// Validates a FirstTouchContext before evaluation.
/// Call in DEBUG builds only; disable in RELEASE for performance.
/// </summary>
public static void ValidateContext(FirstTouchContext ctx)
{
    // Agent identification
    Debug.Assert(ctx.AgentID >= 0,
        $"FirstTouch: Invalid AgentID {ctx.AgentID}");
    Debug.Assert(ctx.TeamID == 0 || ctx.TeamID == 1,
        $"FirstTouch: Invalid TeamID {ctx.TeamID}");

    // Attributes must be in valid range [1, 20]
    Debug.Assert(ctx.Technique >= 1 && ctx.Technique <= 20,
        $"FirstTouch: Technique {ctx.Technique} out of range [1,20]");
    Debug.Assert(ctx.FirstTouchAttribute >= 1 && ctx.FirstTouchAttribute <= 20,
        $"FirstTouch: FirstTouchAttribute {ctx.FirstTouchAttribute} out of range [1,20]");

    // Vectors must not be NaN
    Debug.Assert(!float.IsNaN(ctx.AgentPosition.x) &&
                 !float.IsNaN(ctx.AgentPosition.y) &&
                 !float.IsNaN(ctx.AgentPosition.z),
        "FirstTouch: AgentPosition contains NaN");
    Debug.Assert(!float.IsNaN(ctx.BallVelocity.x) &&
                 !float.IsNaN(ctx.BallVelocity.y) &&
                 !float.IsNaN(ctx.BallVelocity.z),
        "FirstTouch: BallVelocity contains NaN");

    // Facing and intended direction must be unit vectors (within tolerance)
    Debug.Assert(Mathf.Abs(ctx.AgentFacing.magnitude - 1.0f) < 0.01f,
        $"FirstTouch: AgentFacing is not normalised (mag={ctx.AgentFacing.magnitude})");
    Debug.Assert(Mathf.Abs(ctx.IntendedTouchDirection.magnitude - 1.0f) < 0.01f,
        $"FirstTouch: IntendedTouchDirection is not normalised");

    // Pressure scalar must be in range
    Debug.Assert(ctx.PressureScalar >= 0.0f && ctx.PressureScalar <= 1.0f,
        $"FirstTouch: PressureScalar {ctx.PressureScalar} out of range [0,1]");

    // Ball height must be non-negative
    Debug.Assert(ctx.BallHeight >= 0.0f,
        $"FirstTouch: BallHeight {ctx.BallHeight} is negative");

    // Height guard (should have been rejected before context is built for aerial balls)
    Debug.Assert(ctx.BallHeight <= FirstTouchConstants.GROUND_CONTROL_HEIGHT,
        $"FirstTouch: BallHeight {ctx.BallHeight} exceeds GROUND_CONTROL_HEIGHT " +
        $"({FirstTouchConstants.GROUND_CONTROL_HEIGHT}); should have routed to Heading Mechanics");
}
```

### 4.8.2 FirstTouchResult Validation

```csharp
/// <summary>
/// Validates a FirstTouchResult after evaluation.
/// Call in DEBUG builds only immediately after EvaluateFirstTouch().
/// </summary>
public static void ValidateResult(FirstTouchResult result, FirstTouchContext ctx)
{
    // Control quality must be in [0, 1]
    Debug.Assert(result.ControlQuality >= 0.0f && result.ControlQuality <= 1.0f,
        $"FirstTouch: ControlQuality {result.ControlQuality} out of range");

    // Touch radius must be in [RADIUS_MIN, RADIUS_HEAVY]
    Debug.Assert(result.TouchRadius >= FirstTouchConstants.RADIUS_MIN &&
                 result.TouchRadius <= FirstTouchConstants.RADIUS_HEAVY,
        $"FirstTouch: TouchRadius {result.TouchRadius} out of range " +
        $"[{FirstTouchConstants.RADIUS_MIN}, {FirstTouchConstants.RADIUS_HEAVY}]");

    // Ball must not be below surface
    Debug.Assert(result.NewBallPosition.z >= 0.11f,
        $"FirstTouch: NewBallPosition.z {result.NewBallPosition.z} below Ball.RADIUS");

    // Ball speed must not exceed incoming speed × retention max
    float maxOutputSpeed = result.IncomingBallSpeed * FirstTouchConstants.MOMENTUM_RETENTION_MAX;
    Debug.Assert(result.NewBallVelocity.magnitude <= maxOutputSpeed + FirstTouchConstants.COMPARISON_EPSILON,
        $"FirstTouch: NewBallVelocity magnitude {result.NewBallVelocity.magnitude} " +
        $"exceeds max {maxOutputSpeed}");

    // Outcome enum must be a valid value
    Debug.Assert(System.Enum.IsDefined(typeof(TouchResult), result.PossessionOutcome),
        $"FirstTouch: Invalid PossessionOutcome value {(int)result.PossessionOutcome}");

    // Possession agent ID rules
    if (result.PossessionOutcome == TouchResult.CONTROLLED)
    {
        Debug.Assert(result.PossessingAgentID == ctx.AgentID,
            "FirstTouch: CONTROLLED outcome must assign possession to the receiving agent");
        Debug.Assert(result.TriggeredDribblingState,
            "FirstTouch: CONTROLLED outcome must set TriggeredDribblingState = true");
    }
    else
    {
        Debug.Assert(result.PossessingAgentID == FirstTouchConstants.AGENT_ID_NONE,
            $"FirstTouch: Non-CONTROLLED outcome must have PossessingAgentID = AGENT_ID_NONE");
    }

    if (result.PossessionOutcome == TouchResult.INTERCEPTION)
    {
        Debug.Assert(result.InterceptingAgentID != FirstTouchConstants.AGENT_ID_NONE,
            "FirstTouch: INTERCEPTION outcome must have a valid InterceptingAgentID");
    }
    else
    {
        Debug.Assert(result.InterceptingAgentID == FirstTouchConstants.AGENT_ID_NONE,
            "FirstTouch: Non-INTERCEPTION outcome must have InterceptingAgentID = AGENT_ID_NONE");
    }
}
```

---

## 4.9 Section Summary

| Component | Section | Description | v1.1 Change |
|-----------|---------|-------------|-------------|
| File Organization | §4.1 | 11 source files + 7 test files | `FirstTouchEventEmitter.cs` removed (ERR-004) |
| TouchResult enum | §4.2 | 4 states: CONTROLLED, LOOSE_BALL, DEFLECTION, INTERCEPTION | Trigger comments updated for v1.1 interface changes |
| FirstTouchContext | §4.3.1 | 19 fields; ~88 bytes | Size corrected from 128 (v1.2) |
| FirstTouchResult | §4.3.2 | 10 fields; ~56 bytes | XML doc updated to reflect direct BallState write |
| FirstTouchConstants | §4.4 | 30 named constants | `EVENT_QUEUE_CAPACITY` removed (ERR-004) |
| IFirstTouchSystem | §4.5.1 | Main API: Evaluate + Apply | `ApplyTouchResult()` internal call list updated |
| IBallPhysicsSystem | §4.5.2 | Single `SetBallState()` call | **Replaced** `IBallPhysicsCallback` 4-method interface (ERR-001) |
| IAgentMovementSystem | §4.5.3 | `SetDribblingState()` | Unchanged |
| Possession state | §4.5.4 | Direct `BallState.State` write | **Replaced** `IPossessionManager` interface (ERR-004) |
| Event emission | §4.5.5 | Deferred debug-log stub | **Replaced** `IFirstTouchEventQueue` ring buffer (ERR-004) |
| Data Flow | §4.6 | Build → Evaluate → Apply sequence | Apply block updated; INTERCEPTION chain unchanged |
| Memory Layout | §4.7 | Zero heap allocation per touch | Allocation table simplified (2 rows removed) |
| Validation | §4.8 | Debug-only asserts | No change — still 12 context + 8 result checks |

---

## Cross-Reference Verification

| Reference | Target | Verified | Notes |
|-----------|--------|----------|-------|
| Ball Physics #1 §3.1.1 | Coordinate system (XY pitch, Z up) | ✓ | AgentFacing, BallPosition comments |
| Ball Physics #1 §3.1.2 | Ball.RADIUS = 0.11m | ✓ | §4.8.2 Z floor check; §4.3.2 NewBallPosition note |
| Ball Physics #1 §3.1.3 | BallStateType enum (CONTROLLED, ROLLING, AIRBORNE) | ✓ | §4.5.4 possession state mapping; §4.5.1 ApplyTouchResult |
| Agent Movement #2 §3.5.6 | Technique, FirstTouch [1-20] | ✓ | §4.3.1 field ranges |
| Agent Movement #2 §3.5.4 | AgentPhysicalState fields | ✓ | §4.3.1 AgentPosition/Velocity/Facing sources |
| Agent Movement #2 §3.5.2 | Top sprint speed 7.0 m/s | ✓ | MOVEMENT_REFERENCE = 7.0 with cross-spec warning |
| Agent Movement #2 §6.1.2 | DribblingModifier activation | ✓ | §4.5.3 IAgentMovementSystem |
| Collision System #3 §4.2.6 | AgentBallCollisionData fields | ⚠ Pending | Collision System approval pending; fields assumed stable |
| Master Vol 1 §1.3 | Determinism requirement | ✓ | EvaluateFirstTouch() purity documented; §4.5.1 |
| Master Vol 1 §6 | Touch radii 0.3/0.6/1.2/2.0m | ✓ | RADIUS_PERFECT/GOOD/POOR/HEAVY |
| Master Vol 1 §6 | Half-turn +15% bonus | ✓ | HALF_TURN_BONUS = 0.15 |
| First Touch #4 §2.4 | Touch outcome taxonomy | ✓ | TouchResult enum matches §2.4 definitions |
| First Touch #4 §3.4.5 | INTERCEPTION next-frame rule | ✓ | §4.6 data flow diagram; §4.2.1 INTERCEPTION comment |
| First Touch Outline §Critical Issues | All 4 issues resolved | ✓ | Issue #1 IsGoalkeeper; #2 GROUND_CONTROL_HEIGHT; #3 single contact; #4 next-frame |
| Spec Error Log | ERR-001 | ✓ | IBallPhysicsCallback → IBallPhysicsSystem.SetBallState() |
| Spec Error Log | ERR-004 | ✓ | IPossessionManager and IFirstTouchEventQueue removed |

---

**End of Section 4**

**Page Count:** ~26 pages
**Version:** 1.1
**Next Section:** Section 5 — Testing (unit tests, validation scenarios, acceptance criteria)
