## 3.5 Execution Dispatch

### 3.5.1 Dispatch Overview

`DispatchAction()` (Step 6 of the pipeline, §2.1.2) receives the `AgentAction` selected
by `SelectAction()` (§3.3) and routes it to the appropriate execution system. This is a
**pure routing function** — it performs no utility evaluation, no attribute reads, and no
physics computation. Its responsibility is:

1. Construct the execution request struct appropriate to `ActionType`.
2. Populate all required fields from `DecisionContext` and `AgentAction.Payload`.
3. Call the entry point of the appropriate execution system.
4. Return without waiting for execution completion.

All execution systems are synchronous at Stage 0. `DispatchAction()` returns as soon as
the execution system's entry-point method returns. For Pass Mechanics and Shot Mechanics,
this means returning after `WINDUP` state entry (not after ball contact). For Agent
Movement, this means returning after the `MovementCommand` is accepted by the movement
controller.

**Ownership boundary:** Once `DispatchAction()` returns, all responsibility for the
action's physical outcome belongs to the receiving execution system. The Decision Tree
does not monitor ball trajectory, does not receive pass completion callbacks, and does not
modify execution state. This is KD-5 (§1.4).

**AR-5 risk (outline):** Incorrect field population is the highest-risk failure mode in
this section. A `PassRequest` with a bad `IntendedDistance` produces wrong velocity; a
`ShotRequest` with an out-of-range `PlacementTarget` triggers Shot Mechanics validation
failure (VR-05/VR-06). Every field below has an explicit source and constraint. Unit
tests in §5.5 verify each field for both valid and boundary inputs.

---

### 3.5.2 PassRequest Population (PASS)

**Execution system:** Pass Mechanics Spec #5, `PassExecutor.Execute(PassRequest)`  
**Entry point:** `Pass_Mechanics_Spec_Section_4_v1_0 §4.1`  
**PassRequest struct definition:** Pass Mechanics Spec #5, §2.4.1 (authoritative)

```csharp
// ============================================================
// PassRequest population — Decision Tree §3.5.2
// Source struct: Pass Mechanics §2.4.1
// Called by: ActionDispatcher.cs, DispatchAction(), PASS branch
// ============================================================

PassRequest request = new PassRequest
{
    // ── Field: AgentID ─────────────────────────────────────────────────────
    // Type: int
    // Constraint: Must be valid agent ID [0–21]
    // Source: DecisionContext.AgentState.AgentId (read-only; set by orchestrator)
    // Validation performed by: Pass Mechanics §4.1 (possession check)
    AgentID = context.AgentState.AgentId,

    // ── Field: PassType ────────────────────────────────────────────────────
    // Type: PassType enum {Ground, Driven, Lofted, ThroughBall, Cross, Chip}
    // Source: AgentAction.Payload.PassType
    //   Populated by GenerateOptions() §3.1 during option generation.
    //   Selection logic per §3.1.2: pass type is determined from IntendedDistance
    //   and PerceivedTeammate geometry at option-generation time, not here.
    // Constraint: Must be a valid PassType enum member.
    PassType = context.SelectedAction.Payload.PassType,

    // ── Field: CrossSubType ────────────────────────────────────────────────
    // Type: CrossSubType enum {Flat, Whipped, High}; default = Flat
    // Source: AgentAction.Payload.CrossSubType
    //   Set by GenerateOptions() only when PassType == Cross.
    //   For all non-Cross pass types: CrossSubType is ignored by Pass Mechanics.
    //   Set to default value (Flat) for non-Cross types — never left uninitialised.
    // Cross-spec note: Pass Mechanics §3.1 warns that a non-null CrossSubType
    //   on a non-Cross PassType does not cause failure (ignored), but this spec
    //   sets it to Flat defensively to avoid confusion in diagnostics.
    CrossSubType = context.SelectedAction.Payload.CrossSubType,  // default Flat

    // ── Field: TargetAgentID ───────────────────────────────────────────────
    // Type: int; -1 means space-targeted
    // Source: AgentAction.Payload.TargetAgentId
    //   For player-targeted passes: the perceived teammate's AgentId, as read
    //   from PerceptionSnapshot.VisibleTeammates[n].AgentId.
    //   For through-balls into space: -1 (GenerateOptions §3.1.3 sets this).
    // Constraint: If ≥ 0, must be a teammate agent [0–21] currently in the
    //   simulation. Decision Tree cannot verify possession state at dispatch;
    //   Pass Mechanics §4.1 validates possession ownership.
    TargetAgentID = context.SelectedAction.Payload.TargetAgentId,

    // ── Field: TargetPosition ──────────────────────────────────────────────
    // Type: Vector3 (world space)
    // Source: AgentAction.Payload.TargetPosition
    //   Player-targeted: ignored by Pass Mechanics (TargetAgentID takes precedence).
    //     Set to Vector3.zero defensively.
    //   Space-targeted (TargetAgentID == -1): the projected through-ball destination
    //     computed in GenerateOptions §3.1.3 from receiver velocity extrapolation.
    //     Must be a valid pitch-space coordinate.
    // Constraint: Only meaningful when TargetAgentID == -1.
    TargetPosition = context.SelectedAction.Payload.TargetPosition,

    // ── Field: IntendedDistance ────────────────────────────────────────────
    // Type: float (metres); must be > 0
    // Source: Euclidean distance between AgentState.Position and TargetPosition
    //   (or TargetAgent.Position if player-targeted), computed at option-generation
    //   time and stored in AgentAction.Payload.IntendedDistance.
    //   NOT recomputed here — agent or target may have moved between option
    //   generation and dispatch, but recomputation is not permitted (determinism).
    // Constraint: > 0. Pass Mechanics §3.2 will log FM-07 and reject if ≤ 0.
    //   GenerateOptions() guarantees this is populated with a valid distance.
    IntendedDistance = context.SelectedAction.Payload.IntendedDistance,

    // ── Field: Urgency ─────────────────────────────────────────────────────
    // Type: float [0.0–1.0]
    // Source: AgentAction.Payload.Urgency
    //   Derived in GenerateOptions §3.1 from PressureScalar P (§3.2).
    //   High pressure → high urgency. Formula:
    //     Urgency = clamp(P × URGENCY_PRESSURE_SCALE, 0.0f, 1.0f)
    //     URGENCY_PRESSURE_SCALE = 1.0f [GT] (stored in UtilityWeights.cs)
    //   This creates a direct mapping: P=0.0 → Urgency=0.0, P=1.0 → Urgency=1.0.
    //   Urgency feeds Pass Mechanics error model §3.5 and windup duration §3.8.
    //   Designers may tune URGENCY_PRESSURE_SCALE to control how aggressively
    //   pressure translates to rushed passing.
    Urgency = context.SelectedAction.Payload.Urgency,

    // ── Field: IsWeakFoot ──────────────────────────────────────────────────
    // Type: bool
    // Source: Computed in GenerateOptions §3.1 per weak-foot determination rule:
    //   IsWeakFoot = (RequiredFoot(targetDirection) != AgentAttributes.PreferredFoot)
    //   Stored in AgentAction.Payload.IsWeakFoot.
    // Note [ERR-007-TRACKED]: PreferredFoot determination requires WeakFootRating
    //   from PlayerAttributes (AM-002-001). Temporary proxy: if WeakFootRating not
    //   available, IsWeakFoot = false (conservative — avoids phantom penalties).
    // Cross-spec note: Pass Mechanics §3.7 applies the weak foot error multiplier;
    //   the Decision Tree determines the flag, not the magnitude.
    IsWeakFoot = context.SelectedAction.Payload.IsWeakFoot,

    // ── Field: FrameNumber ─────────────────────────────────────────────────
    // Type: int; must be > 0
    // Source: DecisionContext.CurrentFrame (simulation frame counter; set by
    //   orchestrator each heartbeat).
    // Purpose: Required by Pass Mechanics §3.5 deterministic error hash:
    //   hashInput = AgentId × 73856093 XOR FrameNumber × 19349663 XOR PassTypeIndex × 83492791
    //   (Pass Mechanics Appendix B §App-B-3.5). Same-frame, same-agent, same-type
    //   → identical error angle. Cross-tick determinism requires this field.
    FrameNumber = context.CurrentFrame
};
```

**Post-population call:**

```csharp
PassResult result = PassExecutor.Execute(request);
// Decision Tree does not inspect result. PassExecutor handles all failure modes.
// PassAttemptEvent or PassCancelledEvent published by Pass Mechanics §4.6.
```

**Payload fields required on `AgentAction` for PASS dispatch:**

| Field | Type | Source | Constraint |
|---|---|---|---|
| `Payload.PassType` | `PassType` | §3.1 option gen | Valid enum member |
| `Payload.CrossSubType` | `CrossSubType` | §3.1 (Cross only) | Valid enum; default Flat |
| `Payload.TargetAgentId` | `int` | §3.1 option gen | ≥ 0 or -1 (space pass) |
| `Payload.TargetPosition` | `Vector3` | §3.1 option gen | Valid if TargetAgentId == -1 |
| `Payload.IntendedDistance` | `float` | §3.1 option gen | > 0 |
| `Payload.Urgency` | `float` | §3.1 (from P) | [0.0, 1.0] |
| `Payload.IsWeakFoot` | `bool` | §3.1 option gen | — |

---

### 3.5.3 ShotRequest Population (SHOOT)

**Execution system:** Shot Mechanics Spec #6, `ShotExecutor.Execute(ShotRequest)`  
**Entry point:** Shot Mechanics Spec #6, §4.1  
**ShotRequest struct definition:** Shot Mechanics Spec #6, §2.4.1 (authoritative)

```csharp
// ============================================================
// ShotRequest population — Decision Tree §3.5.3
// Source struct: Shot Mechanics §2.4.1
// Called by: ActionDispatcher.cs, DispatchAction(), SHOOT branch
// ============================================================

ShotRequest request = new ShotRequest
{
    // ── Field: AgentId ─────────────────────────────────────────────────────
    // Type: int; must be > 0 (Shot Mechanics VR-01)
    // Source: DecisionContext.AgentState.AgentId
    // Validation: Shot Mechanics §3.1 VR-01 confirms agent has possession.
    AgentId = context.AgentState.AgentId,

    // ── Field: PowerIntent ─────────────────────────────────────────────────
    // Type: float [0.0, 1.0] (Shot Mechanics VR-02)
    // Source: AgentAction.Payload.PowerIntent
    //   Derived in GenerateOptions §3.1 from the SHOOT utility score.
    //   Mapping: PowerIntent = clamp(GoalOpeningScore × A_Finishing_norm, 0.1f, 1.0f)
    //   High GoalOpeningScore + high Finishing → high power intent (commit to shot).
    //   Low GoalOpeningScore → agent reduces power to increase placement precision.
    //   Minimum 0.1f: prevents zero-power shots from being generated.
    //   The power/accuracy trade-off curve is owned by Shot Mechanics §3.2.
    PowerIntent = context.SelectedAction.Payload.PowerIntent,

    // ── Field: ContactZone ─────────────────────────────────────────────────
    // Type: ContactZone enum {Centre, BelowCentre, OffCentre} (VR-04)
    // Source: AgentAction.Payload.ContactZone
    //   Determined in GenerateOptions §3.1 by shot intent classification:
    //     Centre     → driven low shot (default; most common)
    //     BelowCentre → chip/loft (when GoalkeeperId detected in line of shot
    //                   and GoalOpeningScore is low overhead)
    //     OffCentre  → curl (when agent's LongShots > CURL_LONGSHOTS_THRESHOLD
    //                   and PlacementTarget is near a post)
    //   The threshold constants are defined in §3.1 and live in UtilityWeights.cs.
    //   Shot Mechanics KD-1 explicitly states: DT selects physical parameters;
    //   Shot Mechanics does NOT interpret a shot type label. This field IS the
    //   type indicator — it encodes intent as physics input, not as enum intent.
    ContactZone = context.SelectedAction.Payload.ContactZone,

    // ── Field: SpinIntent ──────────────────────────────────────────────────
    // Type: float [0.0, 1.0] (VR-03)
    // Source: AgentAction.Payload.SpinIntent
    //   Set in GenerateOptions §3.1 based on ContactZone selection:
    //     Centre     → SpinIntent = 0.0f (minimal deliberate spin; natural topspin)
    //     BelowCentre → SpinIntent = 0.6f [GT] (deliberate backspin / float)
    //     OffCentre  → SpinIntent = 0.8f [GT] (deliberate sidespin / curl)
    //   These are starting values for option generation; the Composure noise
    //   model (§3.3) does not modify SpinIntent — it only modifies EffectiveUtility.
    //   SpinIntent variations therefore reflect only the agent's deliberate intent,
    //   not execution noise (execution noise lives in Shot Mechanics §3.3).
    SpinIntent = context.SelectedAction.Payload.SpinIntent,

    // ── Field: PlacementTarget ─────────────────────────────────────────────
    // Type: Vector2; components each [0.0, 1.0] (VR-05, VR-06)
    //   u = [0.0, 1.0]: left post → right post
    //   v = [0.0, 1.0]: ground → crossbar
    // Source: AgentAction.Payload.PlacementTarget
    //   Derived in GenerateOptions §3.1 using goal geometry:
    //     1. Identify visible goal mouth extent from PerceptionSnapshot.
    //     2. Identify highest-value unguarded zone (fewest goalkeeper/defender
    //        positions in the visible path).
    //     3. Select corner of highest-value zone:
    //        PlacementTarget = unguardedCorner + PLACEMENT_CORNER_OFFSET [GT]
    //        PLACEMENT_CORNER_OFFSET nudges target inward from posts/bar by 0.1
    //        to avoid near-post/crossbar clipping in error scenarios.
    //   Error is applied by Shot Mechanics §3.3 in placement-target space —
    //   the Decision Tree provides intent; Shot Mechanics provides execution
    //   imperfection.
    // Constraint: Both components must be in [0.0, 1.0]. Clamped in §3.1 before
    //   being stored in payload. Shot Mechanics VR-05/VR-06 will reject out-of-range.
    PlacementTarget = context.SelectedAction.Payload.PlacementTarget,

    // ── Field: IsWeakFoot ──────────────────────────────────────────────────
    // Type: bool
    // Source: Same determination as PASS (§3.5.2 IsWeakFoot note above).
    //   Required shooting direction relative to agent facing and preferred foot.
    // Note [ERR-007-TRACKED]: same proxy applies — false if WeakFootRating unavailable.
    // Shot Mechanics §3.3 applies the weak-foot accuracy penalty.
    IsWeakFoot = context.SelectedAction.Payload.IsWeakFoot,

    // ── Field: DistanceToGoal ──────────────────────────────────────────────
    // Type: float; must be > 0 (VR-07)
    // Source: Computed in GenerateOptions §3.1 as Euclidean distance from
    //   AgentState.Position to the nearest goal-post midpoint.
    //   Stored in AgentAction.Payload.DistanceToGoal.
    //   NOT recomputed at dispatch (same determinism rationale as IntendedDistance).
    // Purpose: Shot Mechanics §3.2 uses this for Finishing/LongShots sigmoid blend.
    DistanceToGoal = context.SelectedAction.Payload.DistanceToGoal,

    // ── Field: FrameNumber ─────────────────────────────────────────────────
    // Type: int; must be > 0 (VR-08)
    // Source: DecisionContext.CurrentFrame
    // Purpose: Shot Mechanics §3.3 deterministic error seed:
    //   matchSeed + agentId + frameNumber → deterministic error vector (OI-004).
    //   Consistent with Pass Mechanics pattern (§3.5.2) and §3.3 SplitMix64 hash.
    FrameNumber = context.CurrentFrame
};
```

**Post-population call:**

```csharp
ShotResult result = ShotExecutor.Execute(request);
// Decision Tree does not inspect result. Shot Mechanics handles all failure modes.
// ShotAttemptEvent published by Shot Mechanics §4.6.
```

**Payload fields required on `AgentAction` for SHOOT dispatch:**

| Field | Type | Source | Constraint |
|---|---|---|---|
| `Payload.PowerIntent` | `float` | §3.1 option gen | [0.0, 1.0] min 0.1 |
| `Payload.ContactZone` | `ContactZone` | §3.1 option gen | Valid enum |
| `Payload.SpinIntent` | `float` | §3.1 option gen | [0.0, 1.0] |
| `Payload.PlacementTarget` | `Vector2` | §3.1 option gen | Both components [0.0, 1.0] |
| `Payload.IsWeakFoot` | `bool` | §3.1 option gen | — |
| `Payload.DistanceToGoal` | `float` | §3.1 option gen | > 0 |

---

### 3.5.4 MovementCommand Construction — DRIBBLE

**Execution system:** Agent Movement Spec #2, movement controller  
**Interface:** `MovementCommand` struct submitted to movement system per Agent Movement §3.5.4 (v1.4)

DRIBBLE means the agent has possession and is moving with the ball at feet. The agent's
goal is to advance toward a dribble target position while maintaining possession.

```csharp
// ── DRIBBLE dispatch ──────────────────────────────────────────────────────
// Agent has possession and chooses to advance with the ball.
// Target: AgentAction.Payload.DribbleTarget — a position computed in
//   GenerateOptions §3.1 as the optimal space to advance into:
//   - Selected from visible open lanes in PerceptionSnapshot
//   - Limited to DRIBBLE_MAX_TARGET_DISTANCE [GT] = 8.0m from agent position
//   - Direction biased toward goal if in attacking third, otherwise
//     toward formation slot to maintain positional discipline

MovementCommand dribbleCmd = new MovementCommand
{
    TargetPosition = context.SelectedAction.Payload.DribbleTarget,
    DesiredState   = AgentMovementState.JOGGING,        // Ball-at-feet pace
    DecelerationMode = DecelerationMode.CONTROLLED,
    FacingMode     = FacingMode.AUTO_ALIGN,             // Face direction of travel
    FacingTarget   = Vector3.zero,                      // Unused with AUTO_ALIGN
    OverrideSafetyConstraints = false,                  // NEVER override in gameplay
    DebugLabel     = "Dribble"                          // Interned string; no GC alloc
};

MovementController.SubmitCommand(context.AgentState.AgentId, dribbleCmd);
```

**`JOGGING` pace rationale:** Dribbling is intentionally constrained to `JOGGING` at
Stage 0. An agent holding the ball at sprint speed while maintaining precise control is
a Stage 1 concern (dribbling speed modifier). Forcing `JOGGING` prevents the pathological
case of an agent sprinting away from all opponents indefinitely — at `JOGGING` pace,
opponents can close. This is a deliberate Stage 0 gameplay constraint, documented in
§1.6 known limitations.

**Note on dribble speed:** Agent Movement §3.5.4 states: if the requested `DesiredState`
is not achievable, movement system uses the highest achievable state. A JOGGING request
from a STUMBLING agent will be reduced to WALKING. The Decision Tree does not compensate
for this — it always requests JOGGING and trusts Agent Movement's feasibility logic.

---

### 3.5.5 MovementCommand Construction — HOLD

HOLD means the agent has possession but chooses not to advance. The agent stops moving
(or decelerates to idle) while maintaining possession. Facing is locked toward the ball's
last known position to preserve situational awareness.

```csharp
// ── HOLD dispatch ─────────────────────────────────────────────────────────
// Agent has possession and chooses to hold in place.
// Agent decelerates to IDLE. Facing locked toward ball position.
// Note: HOLD is always generated by GenerateOptions §3.1. It is the
//   no-move fallback. Its low BaseUtility (0.25 [GT]) ensures it wins
//   only when all other scored candidates are lower.

MovementCommand holdCmd = new MovementCommand
{
    TargetPosition = context.AgentState.Position,       // Stay in place
    DesiredState   = AgentMovementState.IDLE,
    DecelerationMode = DecelerationMode.CONTROLLED,
    FacingMode     = FacingMode.TARGET_LOCK,            // Watch ball while holding
    FacingTarget   = context.MatchContext.BallPosition, // Ball position from MatchContext
    OverrideSafetyConstraints = false,
    DebugLabel     = "Hold"
};

MovementController.SubmitCommand(context.AgentState.AgentId, holdCmd);
```

**`BallPosition` source:** `MatchContext.BallPosition` is a public game-state value
(§2.2.5). The Decision Tree is permitted to read ball position from `MatchContext` — it
is not required to route through `PerceptionSnapshot` for publicly available match state.
This is consistent with §1.3.1 ("game-state information publicly available to all agents
may be accessed via `MatchContext`").

---

### 3.5.6 MovementCommand Construction — MOVE_TO_POSITION

MOVE_TO_POSITION instructs the agent to return to or advance toward its formation slot.
The formation slot is provided by `TacticalContext.FormationSlot` (§2.2.6), adjusted for
`DefensiveLineDepth` (§3.4.5).

```csharp
// ── MOVE_TO_POSITION dispatch ─────────────────────────────────────────────
// Agent moves toward formation slot. Pace is determined by urgency:
//   - Distance > MOVE_SPRINT_THRESHOLD [GT] (15m): SPRINTING — urgently out of shape
//   - Distance > MOVE_JOG_THRESHOLD [GT] (6m): JOGGING — moderate repositioning
//   - Distance ≤ MOVE_JOG_THRESHOLD: WALKING — minor adjustment
// This tiered pace avoids agents sprinting across the pitch for small corrections.

float distToSlot = Vector3.Distance(
    context.AgentState.Position,
    new Vector3(                                         // Vector2 slot → world Vector3 (Z=0)
        context.TacticalContext.FormationSlot.x,
        0f,
        context.TacticalContext.FormationSlot.y
    )
);

AgentMovementState pace = distToSlot > MOVE_SPRINT_THRESHOLD
    ? AgentMovementState.SPRINTING
    : distToSlot > MOVE_JOG_THRESHOLD
        ? AgentMovementState.JOGGING
        : AgentMovementState.WALKING;

MovementCommand moveCmd = new MovementCommand
{
    TargetPosition = new Vector3(                          // Vector2 → world Vector3 (Z=0)
        context.TacticalContext.FormationSlot.x, 0f,
        context.TacticalContext.FormationSlot.y),
    DesiredState   = pace,
    DecelerationMode = DecelerationMode.CONTROLLED,
    FacingMode     = FacingMode.AUTO_ALIGN,
    FacingTarget   = Vector3.zero,
    OverrideSafetyConstraints = false,
    DebugLabel     = "MoveToPosition"
};

MovementController.SubmitCommand(context.AgentState.AgentId, moveCmd);
```

**Threshold constants** — listed in §3.4.7 constant table; defined in `UtilityWeights.cs`:

| Constant | Value | Tuning range | Constraint |
|---|---|---|---|
| `MOVE_SPRINT_THRESHOLD` | 15.0m | [10.0m, 25.0m] | Must be > `MOVE_JOG_THRESHOLD` |
| `MOVE_JOG_THRESHOLD` | 6.0m | [3.0m, 12.0m] | Must be < `MOVE_SPRINT_THRESHOLD` |

**`FormationSlot` Vector2→Vector3 conversion:** `TacticalContext.FormationSlot` is `Vector2`
(pitch XZ-plane position). Conversion is `new Vector3(slot.x, 0f, slot.y)` — Y=0 for
ground-plane movement. There is no `.ToVector3()` helper on Unity's `Vector2`; the
explicit constructor is required. This is consistent with Agent Movement's 2D-primary,
3D-secondary coordinate convention (Agent Movement §3.5.3 `FacingDirection` is XY-plane).
**Note on axis convention:** Agent Movement uses XY for the pitch plane; Ball Physics
uses XZ. This section uses XZ convention (Y=0) consistent with Ball Physics Spec #1
§1.2 which is the authoritative coordinate system source. Confirm against Agent Movement
§3.5.3 before implementation.

---

### 3.5.7 MovementCommand Construction — PRESS

PRESS instructs the agent to close down an opponent who has or is receiving the ball.
The press target is an `AgentId` from the agent's visible opponents list.

```csharp
// ── PRESS dispatch ────────────────────────────────────────────────────────
// Agent closes down a visible opponent. Sprint pace; emergency braking.
// PressTargetId: set in GenerateOptions §3.1 as the opponent with possession
//   (or nearest opponent to ball) within PRESS_TRIGGER_DISTANCE (§3.2).
// The pressing agent runs to the opponent's last perceived position
//   (from PerceptionSnapshot.VisibleOpponents[n].Position).
// It does NOT extrapolate opponent movement — it chases the last known position.
// This is intentional: pressing should be reactionary, not perfectly predictive.

Vector3 pressTargetPos = context.SelectedAction.Payload.PressTargetPosition;
// Note: PressTargetPosition is the perceived position of the target agent
//   at option-generation time. Stored in AgentAction.Payload to preserve
//   determinism — opponent may have moved since option generation.

MovementCommand pressCmd = new MovementCommand
{
    TargetPosition = pressTargetPos,
    DesiredState   = AgentMovementState.SPRINTING,      // Full press = sprint
    DecelerationMode = DecelerationMode.EMERGENCY,      // May need to stop fast
    FacingMode     = FacingMode.TARGET_LOCK,            // Keep eyes on ball carrier
    FacingTarget   = pressTargetPos,                    // Face the opponent
    OverrideSafetyConstraints = false,
    DebugLabel     = "Press"
};

MovementController.SubmitCommand(context.AgentState.AgentId, pressCmd);
```

**`EMERGENCY` deceleration rationale:** A pressing agent may need to stop suddenly —
e.g., the opponent passes before the pressing agent arrives. `EMERGENCY` braking gives
the movement system licence to accept stumble risk in exchange for fast stopping. This is
intentional: a committed press that leads to a stumble if the ball is played away is
realistic football behaviour.

**Payload field required:**

| Field | Type | Source | Constraint |
|---|---|---|---|
| `Payload.PressTargetPosition` | `Vector3` | §3.1 option gen | Must be within PRESS_TRIGGER_DISTANCE of agent |

---

### 3.5.8 MovementCommand Construction — INTERCEPT

INTERCEPT instructs the agent to move to an intercept point on the ball's projected
trajectory, aiming to reach that point before the ball does.

```csharp
// ── INTERCEPT dispatch ────────────────────────────────────────────────────
// Agent moves to a ball trajectory intercept point.
// InterceptPoint: computed in GenerateOptions §3.1 via:
//   1. Project ball trajectory from BallState.Position + BallState.Velocity × t
//   2. Compute agent travel time to candidate intercept points: t_agent = dist / AgentMaxSpeed
//   3. Select intercept point where t_ball ≈ t_agent (feasibility window)
//   4. If no feasible intercept point exists, INTERCEPT option is not generated.
//      (Feasibility is a GenerateOptions gate — by the time we reach dispatch,
//       the intercept is deemed feasible at option-generation time.)
// Stored in AgentAction.Payload.InterceptPoint (world space Vector3).

MovementCommand interceptCmd = new MovementCommand
{
    TargetPosition = context.SelectedAction.Payload.InterceptPoint,
    DesiredState   = AgentMovementState.SPRINTING,      // Always sprint to intercept
    DecelerationMode = DecelerationMode.CONTROLLED,     // Controlled stop at point
    FacingMode     = FacingMode.TARGET_LOCK,            // Watch ball during run
    FacingTarget   = context.MatchContext.BallPosition,
    OverrideSafetyConstraints = false,
    DebugLabel     = "Intercept"
};

MovementController.SubmitCommand(context.AgentState.AgentId, interceptCmd);
```

**`CONTROLLED` deceleration rationale for INTERCEPT:** Unlike PRESS (which uses
`EMERGENCY`), the agent intercepting a ball needs to control their stop at the intercept
point to make a first touch. `CONTROLLED` deceleration reduces stumble risk and gives
First Touch Mechanics (#4) a better quality of contact when the collision occurs.

**Payload field required:**

| Field | Type | Source | Constraint |
|---|---|---|---|
| `Payload.InterceptPoint` | `Vector3` | §3.1 option gen | Feasible intercept point |

---

### 3.5.9 Dispatch Failure Modes

| ID | Scenario | Detection | Resolution |
|---|---|---|---|
| FM-DT-09 | `AgentAction.ActionType` does not match any dispatch branch | Exhaustive switch default | Log error with `AgentId` and `ActionType`. Produce HOLD command to keep agent safe. This should not occur if §3.1 option generation is correct. |
| FM-DT-10 | `Payload.IntendedDistance ≤ 0` for PASS dispatch | Pre-dispatch assertion | Log error. Replace with `Vector3.Distance(agent, target)` as fallback. |
| FM-DT-11 | `Payload.DistanceToGoal ≤ 0` for SHOOT dispatch | Pre-dispatch assertion | Log error. Compute from `AgentState.Position` to goal centre. |
| FM-DT-12 | `Payload.PlacementTarget` component out of [0.0, 1.0] for SHOOT | Pre-dispatch clamp | Clamp to [0.0, 1.0]. Log warning. Do not reject — clamped value is valid for Shot Mechanics. |
| FM-DT-13 | PASS or SHOOT selected but agent no longer has possession at dispatch | Detected by Pass/Shot Mechanics VR at entry | Pass/Shot Mechanics rejects request (FM-01 / VR-01). Decision Tree receives no success confirmation. Agent remains in EXECUTING state until next heartbeat tick re-evaluates. |

**FM-DT-13 note:** This failure is not preventable by the Decision Tree. Possession
can be lost between option generation (Step 3) and dispatch (Step 6) if a tackle event
arrives mid-pipeline. The correct resolution is Pass/Shot Mechanics rejection at their
entry point, followed by DT re-evaluation on the next heartbeat. The DT must not
attempt possession re-validation at dispatch — that would duplicate Pass/Shot Mechanics
responsibility.

---

### 3.5.10 Cross-Specification Validation Checks

The following cross-specification checks must be completed before §3.5 can be approved.
Each identifies a field, interface, or constant defined in another specification that
§3.5 depends on.

| Check ID | What to Verify | Source Spec | §3.5 Dependency | Status |
|---|---|---|---|---|
| XC-3.5-01 | `PassRequest.AgentID` is `int`, not `AgentId` struct (field name casing) | Pass Mechanics §2.4.1 | §3.5.2 `AgentID =` | ✅ Verified: `public int AgentID` |
| XC-3.5-02 | `PassRequest.FrameNumber` is `int` (not `uint` or `long`) | Pass Mechanics §2.4.1 | §3.5.2 `FrameNumber =` | ✅ Verified: `public int FrameNumber` |
| XC-3.5-03 | `ShotRequest.AgentId` is `int` (casing: lowercase 'd') | Shot Mechanics §2.4.1 | §3.5.3 `AgentId =` | ✅ Verified: `public int AgentId` — NOTE: casing inconsistency between PassRequest (`AgentID`) and ShotRequest (`AgentId`). Log as ERR-011 (documentation inconsistency — non-blocking). |
| XC-3.5-04 | `ShotRequest.PlacementTarget` is `Vector2` (not `Vector3`) | Shot Mechanics §2.4.1 | §3.5.3 `PlacementTarget =` | ✅ Verified: `public Vector2 PlacementTarget` |
| XC-3.5-05 | `MovementCommand.DesiredState` uses `AgentMovementState` enum from Agent Movement §3.1 | Agent Movement §3.1 v1.2 | §3.5.4–3.5.8 | ✅ Verified: `public AgentMovementState DesiredState` |
| XC-3.5-06 | `MovementCommand.FacingMode` has exactly 2 values: `AUTO_ALIGN` and `TARGET_LOCK` (v1.4 alignment) | Agent Movement §3.5.4 v1.4 | §3.5.4–3.5.8 | ✅ Verified: §3.5.4 Issue #3 fix aligned FacingMode to two values |
| XC-3.5-07 | `PassRequest` has no `HeightHint` field (outline §3.5.2 listed it; §2.4.1 does not define it) | Pass Mechanics §2.4.1 | §3.5.2 | ✅ Verified: No `HeightHint` field in Pass Mechanics §2.4.1. Outline §3.5.2 was preliminary and is superseded by this section. |
| XC-3.5-08 | `PassRequest.TargetType` field exists (outline referenced it; §2.4.1 uses TargetAgentID=-1 pattern instead) | Pass Mechanics §2.4.1 | §3.5.2 target encoding | ✅ Verified: Pass Mechanics §2.4.1 uses `TargetAgentID == -1` for space-targeted passes. There is no `TargetType` enum field. Outline §3.5.1 routing table reference to `TargetType` is superseded by this section. |
| XC-3.5-09 | `AgentMovementState.JOGGING` exists (not RUNNING or JOG) | Agent Movement §3.1 v1.2 | §3.5.4 DRIBBLE dispatch | ✅ Verified: `JOGGING` is defined at line 82 of §3.1 v1.2 |
| XC-3.5-10 | `MovementController.SubmitCommand(int agentId, MovementCommand cmd)` is the dispatch interface | Agent Movement §3.5.4 / §4 | §3.5.4–3.5.8 | 🔒 **TBD-INTERFACE-001** — Contract is locked at the signature level (`(int agentId, MovementCommand cmd) → void`, synchronous, idempotent within a tick). The method **name** `SubmitCommand` is provisional pending Agent Movement §4 finalisation. Implementers MUST NOT diverge from the locked signature; only the identifier may be renamed via a single-symbol substitution at integration time. Tracked in Open Issues as a non-blocking integration item. |

**ERR-011 (new — §3.5 discovery):** `PassRequest.AgentID` uses uppercase `ID` while
`ShotRequest.AgentId` uses mixed-case `Id`. These are different struct definitions in
different specifications. This is a naming inconsistency that will cause confusion at
implementation. Logged here for Spec Error Log addition. Non-blocking — both are
syntactically valid; the inconsistency is cosmetic but should be harmonised before
implementation begins.

---

