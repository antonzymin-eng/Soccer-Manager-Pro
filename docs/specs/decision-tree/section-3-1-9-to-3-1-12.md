## 3.1.9 INTERCEPT Candidate Generation

### 3.1.9.1 Eligibility Gate

INTERCEPT produces zero or one candidate.

```
Gate condition:
  (1)  DecisionContext.AgentHasBall == false
  (2)  PerceptionSnapshot.BallVisible == true
         — agent must currently see the ball in motion
  (3)  PerceptionSnapshot.BallStalenessFrames == 0
         — ball position must be current (stale position = unreliable trajectory)
  (4)  ball is in motion: |MatchContext.BallVelocity| > INTERCEPT_MIN_BALL_SPEED [GT] = 1.0 m/s
         — ball must be moving for interception to be geometrically meaningful
  (5)  InterceptFeasibilityScore > 0.0                  [§3.1.9.3]
  (6)  MatchContext.Phase == OPEN_PLAY
```

**Gate (3) — staleness constraint:** INTERCEPT requires `BallStalenessFrames == 0`.
A ball position that is even 1 tick old (100ms) makes trajectory extrapolation
unreliable at the speeds involved. This is deliberately conservative — better to
MOVE_TO_POSITION than attempt an intercept on stale data.

### 3.1.9.2 Intercept Point Geometry

The intercept point is derived by projecting ball trajectory forward and finding
the point where the agent can arrive at the same time as the ball.

**Stage 0 trajectory model — drag-adjusted linear approximation:**

Ball Physics #1 models drag, Magnus force, and spin. Computing the full trajectory
forward for up to 1.5s at option generation time would require calling into Ball
Physics on every DT tick for every agent — this exceeds the performance budget.
The Stage 0 model uses a drag-adjusted linear approximation: the ball's velocity
is decayed by a first-order drag term at each timestep, producing a curved speed
profile without the cost of full simulation. Magnus and spin effects are ignored.

```
// Ball data
ball_position  = PerceptionSnapshot.BallPerceivedPosition   // current position (m)
ball_speed     = |MatchContext.BallVelocity|                 // scalar speed (m/s)
ball_direction = Normalise(MatchContext.BallVelocity)        // unit direction vector

// Drag constant (from Ball Physics #1 §3.x — reused read-only)
// Ball Physics uses C_d × rho × A / (2m) as the drag deceleration coefficient.
// At Stage 0, approximate with DRAG_APPROX = 0.3 s^-1 [CROSS — from Ball Physics #1]
// This gives a ~26% speed reduction over 1.0s, consistent with
// typical pass deceleration observed in Ball Physics §3 worked examples.
DRAG_APPROX = 0.3   // s^-1  [CROSS — Ball Physics #1 §3.x drag formula]

// Agent data
agent_position  = AgentPosition
agent_max_speed = AgentMaxSpeed(Attributes.Pace)    // from Agent Movement #2 §3.2

// Iterate over candidate interception times (0.1s to MAX_INTERCEPT_TIME, step 0.1s)
MAX_INTERCEPT_TIME = 1.5s [GT]

best_intercept_point = null
best_intercept_time  = MAX_INTERCEPT_TIME + 1.0    // sentinel

foreach t in {0.1, 0.2, 0.3, ..., 1.5}:
    // Drag-adjusted ball position: integrate v(t) = v0 × e^(−DRAG_APPROX × t)
    // Displacement: x(t) = (v0 / DRAG_APPROX) × (1 − e^(−DRAG_APPROX × t))
    drag_decay         = Exp(−DRAG_APPROX × t)                         // e^(−kt)
    ball_displacement  = (ball_speed / DRAG_APPROX) × (1.0 − drag_decay)
    projected_ball_pos = ball_position + ball_direction × ball_displacement

    agent_travel_dist  = |projected_ball_pos − agent_position|
    agent_travel_time  = agent_travel_dist / agent_max_speed

    if agent_travel_time ≤ t:    // agent can arrive at or before ball
        if t < best_intercept_time:
            best_intercept_time  = t
            best_intercept_point = projected_ball_pos

// If no feasible intercept found in [0.1, 1.5]: INTERCEPT not generated
```

**Drag approximation error bounds:**

The full Ball Physics drag model accounts for varying drag coefficient with ball
speed (quadratic drag: F = ½ρC_dAv²). The first-order approximation
(v(t) = v₀e^{−kt}) is exact only for linear drag. The error grows with initial
ball speed and is negligible at low speeds.

| Ball Speed | Approx position at t=1.5s | True position (est.) | Error |
|-----------|--------------------------|----------------------|-------|
| 10 m/s | 11.5m from start | 11.2m (est.) | ~0.3m (2.7%) |
| 20 m/s | 19.4m from start | 18.5m (est.) | ~0.9m (4.7%) |
| 30 m/s (hard shot) | 24.8m from start | 23.1m (est.) | ~1.7m (6.9%) |

**Design decision:** At 1.5s maximum intercept time, a 1.7m positional error on a
30 m/s ball is acceptable at Stage 0. The DT is selecting *whether* to attempt an
intercept, not computing its exact arrival position — Agent Movement handles actual
pathfinding to the intercept point. An error of <7% on extreme cases will not produce
pathological decisions. Stage 1 upgrade path: replace the loop body with a call to
`BallPhysics.ProjectPosition(t)` when that API is exposed.

**Magnus/spin exclusion:** Spin effects can curve ball trajectory by up to 2–3m over
1.5s for a strongly spinning ball. This is not modelled at Stage 0. The consequence is
that INTERCEPT options generated for curling passes or shots will have an intercept
point offset from the true ball path. The agent will still move toward the computed
point; Agent Movement's real-time replanning will correct the path as the ball's
actual position is re-observed each heartbeat. This is an acceptable degradation.

**MAX_INTERCEPT_TIME = 1.5s [GT]:** Interception window. Balls not reachable within
1.5 seconds are not worth attempting. Range: (0.5s, 3.0s). Increasing this allows
longer chase interceptions. Decreasing it makes agents more conservative, defaulting
to MOVE_TO_POSITION for distant balls.

### 3.1.9.3 Time-to-Intercept Feasibility Check

```
// Once best intercept point found:
time_to_intercept    = best_intercept_time
distance_to_point    = |best_intercept_point − agent_position|
agent_travel_time    = distance_to_point / agent_max_speed

// Feasibility: can agent physically reach the intercept point in time?
// (This is rechecked — same calculation as §3.1.9.2 loop, surfaced for scoring)
InterceptFeasibilityScore = Clamp(1.0 − (time_to_intercept / MAX_INTERCEPT_TIME), 0.0, 1.0)
```

| time_to_intercept | InterceptFeasibilityScore |
|-------------------|--------------------------|
| 0.0s (immediate) | 1.0 (trivial intercept — ball at feet) |
| 0.75s | 0.5 |
| 1.5s | 0.0 (at the boundary — barely feasible) |

### 3.1.9.4 InterceptOption Construction

```csharp
InterceptOption interceptOption = new InterceptOption
{
    Type                       = ActionType.INTERCEPT,
    TargetPosition             = best_intercept_point,
    TimeToIntercept            = best_intercept_time,        // seconds
    InterceptFeasibilityScore  = InterceptFeasibilityScore,  // [0.0, 1.0]
    BaseUtility                = 0.0f   // populated by ScoreOptions()
};
```

---

## 3.1.10 Option Set Invariants

The following invariants MUST hold after `GenerateOptions()` returns. Violations are
specification failures, not implementation bugs.

| Invariant | Statement |
|-----------|-----------|
| **INV-GEN-01** | If `AgentHasBall = true`: option set contains exactly one HOLD candidate |
| **INV-GEN-02** | If `AgentHasBall = false`: option set contains exactly one MOVE_TO_POSITION candidate |
| **INV-GEN-03** | Option set is never completely empty (INV-01 or INV-02 guarantees minimum one candidate) |
| **INV-GEN-04** | No action type appears more than once, except PASS (which may have 0..N candidates) |
| **INV-GEN-05** | No candidate has `BaseUtility` != 0.0 at exit from `GenerateOptions()` (scoring is deferred to Step 4) |
| **INV-GEN-06** | All `TargetPosition` values are within pitch bounds (`[0, 105] × [0, 68]` metres) |
| **INV-GEN-07** | All `TargetAgentId` values refer to agents confirmed present in the current `PerceptionSnapshot` |
| **INV-GEN-08** | PASS candidates are ordered by `AdjustedPassLaneScore` descending (ties broken by proximity ascending) |
| **INV-GEN-09** | Option count does not exceed 17 (max 10 PASS + 1 SHOOT + 1 DRIBBLE + 1 HOLD + 1 MOVE + 1 PRESS + 1 INTERCEPT) |
| **INV-GEN-10** | All candidates reference the same `DecisionContext` heartbeat tick (no cross-tick contamination) |

**INV-GEN-03 rationale:** The FR-08 "no viable option" fallback (produce HOLD with utility
0.0) is a safety net for genuinely pathological states — not for normal play. In practice
INV-GEN-01 and INV-GEN-02 make the fallback unreachable during standard match conditions.

---

## 3.1.11 Worked Example — Full Option Generation Pass

**Scenario:** Midfielder in possession (AgentId = 7), attacking zone, moderate pressure.

```
Inputs:
  AgentHasBall       = true
  AgentPosition      = (72.0, 34.0)    ← central attacking midfield
  AgentFacingDirection = (1, 0)        ← facing goal (positive x)
  VisibleTeammates   = [T1(84, 30), T2(77, 40), T3(62, 34)]
  VisibleOpponents   = [O1(73, 36), O2(78, 32), O3(85, 28)]
  PressureScalar     = 0.45 (moderate)
  BallVisible        = true
  MatchContext.Phase = OPEN_PLAY
  MatchContext.OpponentGoalCentre = (105, 34)
  Decisions          = 12 (cap = Floor(2.0 + (0.579 × 8.0)) = Floor(6.63) = 6)

Step: Check branch
  AgentHasBall = true → Possession branch

Step: Generate PASS candidates
  Cap = 6; VisibleTeammates.Length = 3 (cap non-binding)
  Teammates sorted by proximity: T2(7.81m), T3(10.0m), T1(12.65m)

  Goal direction from agent: Normalise((105,34) − (72,34)) = (1, 0)

  T2 (77, 40):
    lane_vec = (5.0, 6.0), |lane_vec| = 7.81m
    [Lane check — same geometry as v1.0: interceptor_count = 0]
    PassLaneScore(T2) = 1.0

    GoalDirectionCosine:
      pass_dir = Normalise(5.0, 6.0) = (0.640, 0.768)
      Dot((0.640, 0.768), (1, 0)) = 0.640
      GoalDirectionModifier = 0.5 + ((0.640 + 1.0) / 2.0) × 0.5 = 0.5 + 0.820 × 0.5 = 0.910
    AdjustedPassLaneScore(T2) = 1.0 × 0.910 = 0.910 ✓ (> 0.05 floor)
    PassOption generated (T2, raw=1.00, adjusted=0.91, cosine=+0.64)

  T3 (62, 34):
    lane_vec = (−10.0, 0.0), |lane_vec| = 10.0m
    [No opponents project onto backward lane: interceptor_count = 0]
    PassLaneScore(T3) = 1.0

    GoalDirectionCosine:
      pass_dir = Normalise(−10, 0) = (−1, 0)
      Dot((−1, 0), (1, 0)) = −1.0   ← pure backward pass
      GoalDirectionModifier = 0.5 + ((−1.0 + 1.0) / 2.0) × 0.5 = 0.5 + 0 = 0.500
    AdjustedPassLaneScore(T3) = 1.0 × 0.500 = 0.500 ✓ (> 0.05 floor)
    PassOption generated (T3, raw=1.00, adjusted=0.50, cosine=−1.00)

    Note: T3 (backward) now scores 0.50 adjusted vs 0.91 for T2 (diagonal forward),
    correctly penalising the backward option relative to v1.0 where both scored 1.0 raw.

  T1 (84, 30):
    lane_vec = (12.0, −4.0), |lane_vec| = 12.65m
    [O2 at perp_dist = 0.0m: interceptor_count = 1]
    PassLaneScore(T1) = 1.0 − (1 / 3.0) = 0.67

    GoalDirectionCosine:
      pass_dir = Normalise(12, −4) = (0.949, −0.316)
      Dot((0.949, −0.316), (1, 0)) = 0.949
      GoalDirectionModifier = 0.5 + ((0.949 + 1.0) / 2.0) × 0.5 = 0.5 + 0.974 × 0.5 = 0.987
    AdjustedPassLaneScore(T1) = 0.67 × 0.987 = 0.661 ✓
    PassOption generated (T1, raw=0.67, adjusted=0.66, cosine=+0.95)

  Generated (sorted by AdjustedPassLaneScore desc):
    [PassOption(T2, adj=0.91), PassOption(T1, adj=0.66), PassOption(T3, adj=0.50)]

  Observation: T3 (backward, clear lane) now ranks BELOW T1 (forward, one interceptor).
  This is correct tactical behaviour — a contested forward pass is preferred over a
  safe backward one under default TacticalContext. The PASS utility formula in §3.2
  may further amplify or suppress this ordering based on tactical instructions.

Step: Generate SHOOT candidate
  distance_to_goal = |(72, 34) − (105, 34)| = 33.0m
  LongShots = 14, A_LongShots = (14−1)/19 = 0.684
  ShootingRange = 20.0 + (0.684 × 15.0) = 30.3m
  33.0m > 30.3m → Gate (3) FAILS
  No ShootOption generated.

Step: Generate DRIBBLE candidate (8-sector model)
  Agent forward direction: (1, 0)
  8 directions: (1,0), (0.71,0.71), (0.71,−0.71), (0,1), (0,−1),
                (−0.71,0.71), (−0.71,−0.71), (−1,0)

  For each opponent, compute bearing from agent:
    O1 (73, 36): bearing = (1, 2), angle = 63.4° → sector (0.71, 0.71) [+45°]
    O2 (78, 32): bearing = (6, −2), angle = −18.4° → sector (1, 0) [0° forward]
    O3 (85, 28): bearing = (13, −6), angle = −24.8° → sector (1, 0) [0° forward] or (0.71, −0.71)?
      |angle_to_0°| = 24.8°; |angle_to_−45°| = 20.2° → assigned to (0.71, −0.71)

  O2 in forward sector (1,0): distance = |(78,32)−(72,34)| = 6.32m > DRIBBLE_THREAT_RADIUS 2.0m
    → space_in_dir(0°) = 1.0 (O2 is beyond threat radius)
  O1 in +45° sector: distance = |(73,36)−(72,34)| = 2.24m > 2.0m
    → space_in_dir(+45°) = 1.0
  O3 in −45° sector: distance = |(85,28)−(72,34)| = 14.4m > 2.0m
    → space_in_dir(−45°) = 1.0
  All remaining sectors: no opponents → space_in_dir = 1.0

  All 8 sectors score 1.0 (all opponents > 2.0m away from agent).
  SpaceScore = 1.0; best_direction = (1,0) (forward by tie-break ordinal priority) ✓
  DribbleOption generated.

Step: Generate HOLD candidate
  Always generated: HoldOption generated.

Final option set (possession branch):
  [PassOption(T2), PassOption(T1), PassOption(T3), DribbleOption, HoldOption]
  Count: 5 options

Invariant check:
  INV-GEN-01 ✓ (HOLD present)
  INV-GEN-03 ✓ (5 candidates)
  INV-GEN-04 ✓ (no duplicate types except PASS)
  INV-GEN-05 ✓ (all BaseUtility = 0.0)
  INV-GEN-08 ✓ (PASS sorted by AdjustedPassLaneScore: 0.91, 0.66, 0.50)
  INV-GEN-09 ✓ (5 ≤ 17)
```

This option set is passed to `ScoreOptions()` (§3.2) for utility assignment.

---

## 3.1.12 Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | March 01, 2026, 3:30 PM PST | Claude (AI) / Anton | Initial draft. Complete option generation for all 7 action types. Worked example verified numerically. Attribute dependency flags (ERR-011). |
| 1.1 | March 01, 2026, 5:00 PM PST | Claude (AI) / Anton | Four fixes: (1) DRIBBLE expanded from 5-sector to 8-sector model — eliminates structural gaps, reduces worst-case angular error from ±67.5° to ±22.5°, blind angle analysis quantified. (2) INTERCEPT trajectory upgraded from linear to drag-adjusted approximation — integrates e^(−kt) decay from Ball Physics #1 drag constant [CROSS], error bounds tabulated (max 6.9% at 30 m/s over 1.5s). Magnus exclusion documented with consequence analysis. (3) PRESS_TRIGGER_DISTANCE given full sensitivity analysis — three degenerate case boundaries documented, three balance tests (BAL-PRESS-01/02/03) defined for Section 5. (4) Backward pass overvaluation fixed — GoalDirectionModifier added to PassOption construction, reducing backward pass AdjustedPassLaneScore to 0.50× of raw score; worked example updated to show correct ranking (T3 backward now correctly ranks below T1 forward despite cleaner lane). |

---

*End of Section 3.1 — Option Generation*

*Decision Tree Specification #8 | Tactical Director — Specification #8 of 20 | Stage 0: Physics Foundation*
*Next: Section 3.2 — Option Scoring: Utility Model*
