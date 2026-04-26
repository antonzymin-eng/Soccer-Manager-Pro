## 3.6 Body Orientation Detection

Body orientation detection determines whether the receiving agent is in the "half-turn"
stance, which confers a +15% effective Technique bonus to control quality (Master Vol 1 Â§6,
Â§2.1). The half-turn positions the agent to receive the ball while already facing play,
reducing recognition latency and improving control.

### 3.6.1 Half-Turn Definition

A player is in the half-turn stance when their body is oriented approximately 45Â° relative
to the incoming ball's approach vector. This allows them to see both the ball and the field
of play simultaneously â€” the canonical "good receiving technique" of professional football.

**Acceptable window:** 45Â° Â± 15Â° = 30Â° to 60Â° from the incoming ball direction.

### 3.6.2 Detection Geometry

```
// Incoming ball approach direction (unit vector, XY plane)
// This is where the ball is coming FROM â€” the direction the ball is travelling in reverse
ApproachDir   = Normalise(Vector2(-ball.Velocity.x, -ball.Velocity.y))

// Agent facing direction (unit vector, XY plane, from Agent Movement Â§3.5.1)
FacingDir     = agent.FacingDirection    // Already normalised

// Angle between agent facing and ball approach
// Dot product of unit vectors = cos(angle)
cosAngle      = Dot(FacingDir, ApproachDir)
angleDeg      = Degrees(Acos(Clamp(cosAngle, -1.0f, 1.0f)))
// Clamp prevents NaN from floating-point precision errors in Acos

// Half-turn window check
isHalfTurn    = (angleDeg >= HALF_TURN_MIN_ANGLE && angleDeg <= HALF_TURN_MAX_ANGLE)
// HALF_TURN_MIN_ANGLE = 30.0Â°
// HALF_TURN_MAX_ANGLE = 60.0Â°
```

### 3.6.3 Bonus Application

```csharp
// orientationBonus is the value passed into CalculateControlQuality (Â§3.1)
float orientationBonus = isHalfTurn ? HALF_TURN_BONUS : 0.0f;
// HALF_TURN_BONUS = 0.15 (15% of normalised attribute)
// Applied as: AttrWithBonus = NormAttr Ã— (1.0 + 0.15) = NormAttr Ã— 1.15
```

### 3.6.4 Edge Cases

| Case | Handling |
|---|---|
| Ball.Velocity â‰ˆ zero (ball nearly stationary) | ApproachDir defaults to agent's inverse-facing direction; bonus unlikely |
| FacingDir is zero vector | IsHalfTurn = false; no bonus; log warning |
| Ball is in AERIAL state (height > 0.5m) | Height guard (Â§2.2) prevents reaching this sub-system |
| Agent facing exactly away from ball (180Â°) | angleDeg = 180Â°; outside window; no bonus |

### 3.6.5 Rationale for Deterministic Angle Computation

The dot-product approach is deterministic on all target platforms when compiled with
consistent floating-point settings (IEEE 754 deterministic mode). `Acos` is monotonically
decreasing for input âˆˆ [-1, 1], so the clamp ensures the result is always defined. No
lookup tables, approximations, or random elements are introduced.

| Constant | Value | Rationale |
|---|---|---|
| `HALF_TURN_MIN_ANGLE` | 30.0Â° | Below this = facing the ball (not half-turn) |
| `HALF_TURN_MAX_ANGLE` | 60.0Â° | Above this = too side-on (poor contact angle) |
| `HALF_TURN_BONUS` | 0.15 | +15% from Master Vol 1 Â§6; also reduces recognition latency (Â§2.1) |

---

## 3.7 Event Emission

> **DEFERRAL NOTE (Section 4 v1.1, ERR-004):** The `IFirstTouchEventQueue` interface
> and `EVENT_QUEUE_CAPACITY` constant were **removed** in Section 4 v1.1. Event emission
> is deferred until the Event System (Spec #17) is designed. Stage 0 implementation
> constructs the `FirstTouchEvent` struct (for diagnostic/logging purposes) but does NOT
> enqueue it. The queue dispatch code in §3.7.2 is **reference architecture** for Stage 1,
> not a Stage 0 implementation requirement. See Section 4 v1.1 changelog for rationale.

First Touch constructs a `FirstTouchEvent` after every evaluation, regardless of outcome.
At Stage 0, this event is available for diagnostic logging only. At Stage 1, it will feed
the Event System (Spec #17) for statistics, replay, and tactical AI learning.
Event construction is the last operation before returning the result â€” it must not throw or
block, and must not affect game state.

### 3.7.1 Event Population

```csharp
/// <summary>
/// Populated after all sub-systems complete. Emitted to event queue.
/// All fields are populated even if outcome is INTERCEPTION or DEFLECTION.
///
/// Consumer: Event System (Spec #17), Stage 1.
/// Stage 0: Event is constructed and queued; Event System reads it when ready.
/// </summary>
FirstTouchEvent evt = new FirstTouchEvent
{
    // â”€â”€ Timing â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    FrameNumber              = currentFrameNumber,
    MatchTime                = matchTimeSeconds,        // Seconds elapsed

    // â”€â”€ Agent â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    AgentID                  = agent.AgentID,
    TeamID                   = agent.TeamID,

    // â”€â”€ Touch details â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    IncomingBallVelocity     = ball.Speed,              // m/s at contact
    ControlQuality           = q,                       // Â§3.1 output
    TouchRadius              = r,                       // Â§3.2 output
    Outcome                  = outcome,                  // Â§3.4 output

    // â”€â”€ Positions â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    TouchPosition            = agent.Position,          // Where touch occurred
    BallEndPosition          = newBallPosition,         // Â§3.3 output

    // â”€â”€ Context â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    PressureScalar           = pressureScalar,          // Â§3.5 output
    WasHalfTurn              = isHalfTurn,              // Â§3.6 output
    WasThunderbolt           = (ball.Speed >= THUNDERBOLT_THRESHOLD),
    // THUNDERBOLT_THRESHOLD = 28.0 m/s; statistical tag for hard shots received

    // â”€â”€ Derived stats â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    ResultedInPossessionChange = (outcome == TouchResult.CONTROLLED
                                  && previousPossessingTeamID != agent.TeamID),
    NewPossessingTeamID      = (outcome == TouchResult.CONTROLLED)
                                  ? agent.TeamID
                                  : previousPossessingTeamID
};
```

### 3.7.2 Queue Dispatch (Stage 1 — Reference Architecture)

> **Stage 0 status:** This subsection describes the **planned** Stage 1 queue dispatch.
> At Stage 0, the event struct is constructed but not enqueued. See Section 4 v1.1 ERR-004.

```csharp
/// <summary>
/// [STAGE 1] Enqueue the event for consumption by Event System.
/// Stage 0: Event is constructed for diagnostics only; queue does not exist.
/// Stage 1: Event System (Spec #17) provides the queue; this code activates.
/// The queue is a pre-allocated ring buffer to avoid heap allocation.
/// If the queue is full, the oldest event is overwritten and a warning logged.
/// </summary>
// eventQueue.Enqueue(evt);  // DEFERRED to Stage 1 (ERR-004)
// Ring buffer capacity: 64 events (matches ~2 seconds of dense play at 60Hz)
// Pre-allocated at startup; zero allocations in hot path
```

### 3.7.3 Stage 0 vs Stage 1 Distinction

In Stage 0, the event queue is written but not consumed (Event System is a Stage 1
deliverable). The queue serves as a buffer and development diagnostic. First Touch
should not gate any game-state logic on Event System availability â€” emit and forget.

| Constant | Value | Rationale |
|---|---|---|
| `THUNDERBOLT_THRESHOLD` | 28.0 m/s | Shots â‰¥ 28 m/s are statistically hard shots; useful for analytics |
| Event queue capacity | 64 slots | 64 Ã— ~96 bytes = ~6 KB; fits in L1 cache; covers dense play bursts |

---

## 3.8 Complete Evaluation Pipeline

The following shows the full call sequence for a single first-touch evaluation, tying all
sub-systems together.

```csharp
/// <summary>
/// Complete first touch evaluation pipeline.
/// Called by Collision System when AGENT_BALL contact detected (Collision System Â§3.4).
/// Returns FirstTouchResult which Collision System dispatches to Ball Physics
/// and Agent Movement.
///
/// FRAME CONTRACT: Called at most once per agent-ball contact per frame.
/// Multiple contacts in the same frame from the same agent are discarded
/// (Collision System handles deduplication).
///
/// HEIGHT CONTRACT: Only called when ball.Position.z â‰¤ GROUND_CONTROL_HEIGHT (0.5m).
/// Caller (Collision System) enforces this check before calling.
///
/// GOALKEEPER CONTRACT: ctx.CollisionData.IsGoalkeeper is received from
/// AgentBallCollisionData (Collision System Â§4.2.6) but is NOT used in any
/// Stage 0 calculation. Goalkeeper foot control (e.g. receiving a back pass)
/// uses identical First Touch calculations to any outfield agent.
/// Goalkeeper-specific behaviour (catching, parrying, diving) is owned by
/// Goalkeeper Mechanics Spec #11 and does not reach this system.
/// </summary>
public FirstTouchResult EvaluateFirstTouch(FirstTouchContext ctx)
{
    // â”€â”€ [1] Sub-system: Orientation Detection (Â§3.6) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    bool  isHalfTurn       = OrientationDetector.Detect(ctx.Agent, ctx.Ball);
    float orientationBonus = isHalfTurn ? FirstTouchConstants.HALF_TURN_BONUS : 0.0f;

    // â”€â”€ [2] Sub-system: Pressure Evaluation (Â§3.5) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    float pressureScalar   = PressureEvaluator.Evaluate(
                                 ctx.Agent,
                                 ctx.SpatialHash,
                                 ctx.FrameNumber);

    // â”€â”€ [3] Sub-system: Control Quality (Â§3.1) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    float q = ControlQualityCalculator.Calculate(
                  ctx.Agent.Attributes.Technique,
                  ctx.Agent.Attributes.FirstTouch,
                  ctx.Ball.Speed,
                  ctx.Agent.Speed,
                  pressureScalar,
                  orientationBonus);

    // â”€â”€ [4] Sub-system: Touch Radius (Â§3.2) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    float r = TouchRadiusCalculator.Calculate(q, ctx.Ball.Speed);

    // â”€â”€ [5] Sub-system: Ball Displacement (Â§3.3) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    (Vector3 newBallPos, Vector3 newBallVel) = BallDisplacementResolver.Resolve(
                                                   ctx.Agent,
                                                   ctx.Ball,
                                                   q,
                                                   r);

    // â”€â”€ [6] Sub-system: Possession State Machine (Â§3.4) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    TouchResult outcome = PossessionStateMachine.Determine(
                              q, r,
                              newBallPos,
                              ctx.Ball.Velocity,
                              newBallVel,
                              ctx.Agent,
                              ctx.SpatialHash);

    // â”€â”€ [7] Sub-system: Event Emission (Â§3.7) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    EventEmitter.Emit(ctx, q, r, newBallPos, pressureScalar, isHalfTurn, outcome);

    // â”€â”€ Output â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    return new FirstTouchResult
    {
        Outcome         = outcome,
        NewBallPosition = newBallPos,
        NewBallVelocity = newBallVel,
        ControlQuality  = q,
        TouchRadius     = r,
        IsDribbling     = (outcome == TouchResult.CONTROLLED)
    };
}
```

---

## 3.9 Empirical Tuning Notes

The following constants are reasonable initial estimates but are **not derived from sports
science literature or validated playtest data**. They are marked `[TUNING REQUIRED]` and
must be revisited during the first playtest cycle. The expected tuning signal for each is
documented to guide that process.

| Constant | Current Value | Tuning Range | Signal to Watch |
|---|---|---|---|
| `MOMENTUM_RETENTION` | 0.5 | 0.3 â€“ 0.7 | Heavy touches feel too "sticky" (lower) or too "slippery" (raise) |
| `DRIBBLE_MAX_SPEED` | 5.5 m/s | 4.5 â€“ 6.5 m/s | Ball at feet lags behind sprinting agent (raise) or races ahead (lower) |
| `TOUCH_MAX_BALL_SPEED` | 12.0 m/s | 8.0 â€“ 15.0 m/s | First-time layoffs feel too fast (lower) or too weak (raise) |
| `PRESSURE_SATURATION` | 1.5 | 1.0 â€“ 2.5 | Single close opponent dominates too much (raise) or too little (lower) |
| `PRESSURE_RADIUS` | 3.0 m | 2.0 â€“ 4.0 m | Opponents feel threatening too early (lower) or too late (raise) |
| `VELOCITY_RADIUS_FACTOR` | 0.25 | 0.15 â€“ 0.40 | Hard passes too easy to control (raise) or too punishing (lower) |
| `MOVEMENT_PENALTY` | 0.5 | 0.3 â€“ 0.7 | Sprinting reception too easy (raise) or unrealistically hard (lower) |
| `INTERCEPTION_RADIUS` | 2.50 m | 1.5 â€“ 3.5 m | Interceptions triggering too frequently (lower) or too rarely (raise) |

**Tuning process:** Constants should be adjusted one at a time, with the verification matrix
(Â§3.1.3) re-run after each change to confirm boundary cases remain within expected ranges.
Constants that interact (e.g. `PRESSURE_WEIGHT` in Â§3.1 and `PRESSURE_SATURATION` in Â§3.5)
should be tuned together as a pair.

---

---

## 3.10 Section Summary

| Sub-system | Section | Output | Key Constants |
|---|---|---|---|
| Control Quality Model | Â§3.1 | `q âˆˆ [0.0, 1.0]` | TECHNIQUE_WEIGHT=0.70, VELOCITY_REFERENCE=15 m/s |
| Touch Radius | Â§3.2 | `r âˆˆ [0.10m, 2.0m]` | 4 bands; VELOCITY_RADIUS_FACTOR=0.25 |
| Ball Displacement | Â§3.3 | `newBallPos, newBallVel` | DRIBBLE_MAX_SPEED=5.5 m/s, MOMENTUM_RETENTION=0.5 |
| Possession State Machine | Â§3.4 | `TouchResult enum` | INTERCEPTION_THRESHOLD=1.20m, INTERCEPTION_RADIUS=2.50m |
| Pressure Evaluation | Â§3.5 | `pressureScalar âˆˆ [0.0, 1.0]` | PRESSURE_RADIUS=3.0m, PRESSURE_SATURATION=1.5 |
| Orientation Detection | Â§3.6 | `isHalfTurn bool, orientationBonus` | HALF_TURN_WINDOW=30Â°â€“60Â°, BONUS=+0.15 |
| Event Emission | §3.7 | `FirstTouchEvent` constructed (queue deferred to Stage 1 per §4 v1.1 ERR-004) | THUNDERBOLT=28 m/s |

---

---

## Cross-Reference Verification

| Reference | Target | Verified | Notes |
|---|---|---|---|
| Master Vol 1 Â§6.4 | Control quality formula structure | âœ“ | Â§3.1.1 expands base formula |
| Master Vol 1 Â§6 (half-turn) | +15% orientation bonus | âœ“ | Â§3.6.3 HALF_TURN_BONUS = 0.15 |
| Master Vol 1 Â§6 (touch radii) | 0.3m / 0.6m / 1.2m / 2.0m thresholds | âœ“ | Â§3.2.1 band definitions |
| Master Vol 1 Â§1.3 | Determinism requirement | âœ“ | Â§3.1.4, Â§3.6.5 â€” no randomness |
| Master Vol 4 Â§3.2 | 6ms physics frame budget | âœ“ | Â§3 operations are O(n) where n â‰¤ 4 |
| Ball Physics #1 Â§3.1.2 | Ball.RADIUS = 0.11m | âœ“ | Â§3.3.4 newBallPosition.z |
| Ball Physics #1 Â§3.1.1 | Coordinate system | âœ“ | XY=pitch plane, Z=up throughout |
| Agent Movement #2 Â§3.5.6 | PlayerAttributes.Technique, FirstTouch [1-20] | âœ“ | Â§3.1.1 ATTR_MAX = 20.0 |
| Agent Movement #2 §3.2 | Pace 20 top speed 10.2 m/s (MOVEMENT_REFERENCE uses [GT] 7.0 m/s) | ✓ | §3.1.2 MOVEMENT_REFERENCE; 7.0 is gameplay-tuned, not derived from §3.5.2 |
| Agent Movement #2 Â§6.1.2 | DribblingModifier activation | âœ“ | Â§3.4.4 |
| Collision System #3 §4.2.6 | AgentBallCollisionData | ✓ | Collision System approved |
| Collision System #3 §3.1.4 | SpatialQuery API | ✓ | Collision System approved; FM-04 fallback retained |
| First Touch Spec Â§2.4 | INTERCEPTION next-frame rule | âœ“ | Â§3.4.5 documents chain resolution |
| First Touch Outline Issue #2 | 0.5m aerial threshold | âœ“ | Â§3.4.3 height guard reference |
| First Touch Outline Issue #3 | Primary contact only | âœ“ | Â§3.8 frame contract note |
| First Touch Outline Issue #4 | Interception next-frame | âœ“ | Â§3.4.5 |

---

**End of Section 3**

**Page Count:** ~20 pages
**Version:** 1.1
**Next Section:** Section 4 â€” Data Structures (FirstTouchContext, FirstTouchResult, FirstTouchEvent, constants class)
