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
    /// Ball centre above this height is routed to Heading Mechanics (Spec #10).
    /// Ball centre at or below this height is processed by First Touch.
    ///
    /// Value: 0.50m ≈ approximate chest height for a crouching player.
    /// Represents the boundary between ground control and aerial mechanics.
    ///
    /// CRITICAL: This is ball CENTRE height, not contact point height.
    /// Ball centre at 0.50m means ball surface is at 0.39m (0.50 - 0.11 RADIUS).
    ///
    /// Source: Outline Critical Issue #2 resolution; §3.4.3 height guard.
    /// Cross-reference: Heading Mechanics Spec #10 must use the same constant.
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

