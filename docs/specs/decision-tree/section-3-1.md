# Decision Tree Specification #8 — Section 3.1: Option Generation

**File:** `Decision_Tree_Spec_Section_3_1_v1_1.md`
**Purpose:** Defines the complete option generation stage of the Decision Tree pipeline
(Step 3 of 6). For each agent at each 10Hz heartbeat, this section governs: how possession
state is determined from `PerceptionSnapshot`, what candidate actions are eligible given that
state, and what precondition checks gate each of the 7 Stage 0 action types. This section
is the gating authority — if an option is not generated here, it cannot be scored or
selected downstream. All attribute references are cross-referenced to `PlayerAttributes`
(Agent Movement Spec #2 §3.5.6 v1.3); attributes listed as TBD are formally declared
as DT requirements pending Spec #20 master attribute registry.

**Created:** March 01, 2026, 3:30 PM PST
**Updated:** March 01, 2026, 5:00 PM PST
**Version:** 1.1
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 8 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)

**Prerequisite Sections (must be stable before this section is finalised):**
- Section 1 v1.1 (approved) — KD-1 through KD-7 locked
- Section 2 v1.1 (approved) — pipeline, data structures, FR list confirmed
- Perception System Spec #7 §3.7 (approved) — `PerceptionSnapshot` struct definition

**Upstream Cross-References (read-only):**
- `PerceptionSnapshot` struct: Perception System #7 §3.7.1
- `PerceivedAgent` sub-struct: Perception System #7 §3.7.2
- `PlayerAttributes`: Agent Movement #2 §3.5.6 v1.3
- `BallState.PossessingAgentId`: Ball Physics #1 (via `MatchContext` — see §3.1.1)
- `PassRequest` caller contract: Pass Mechanics #5 §4 (read-only; populated in §3.5.2)
- `ShotRequest` caller contract: Shot Mechanics #6 §3.1 (read-only; populated in §3.5.3)
- Pressure scalar formula authority: First Touch #4 §3.5 (reused verbatim in Perception #7 §3.6)

---

## ⚠ ATTRIBUTE DEPENDENCY FLAGS

Three attributes consumed by this section are listed as **TBD** in `PlayerAttributes`
(Agent Movement #2 §3.5.6 v1.3 — note at bottom of struct: "Additional attributes TBD
pending other spec requirements"). This section formally declares them as Decision Tree
requirements. They must be added to `PlayerAttributes` before implementation begins.

| Attribute | Used In | Required Type | Spec #20 Action |
|-----------|---------|---------------|-----------------|
| `Decisions` | §3.1.2, §3.1.3 | `int` 1–20 | Add to PlayerAttributes; DT consumer |
| `Anticipation` | §3.1.8 | `int` 1–20 | Already referenced by Perception #7 §3.0 — confirm presence |
| `WorkRate` | §3.1.7 | `int` 1–20 | Add to PlayerAttributes; DT consumer |
| `Positioning` | §3.1.6 | `int` 1–20 | Add to PlayerAttributes; DT consumer |

**Note:** `Decisions` and `Anticipation` are already referenced in Perception System #7 §3.0
(Step 1: CacheAttributes). If absent from `PlayerAttributes`, Perception System is also
affected. This dependency was not flagged in Perception §7 — it is a cross-spec gap. Log
as ERR-011 (low severity; non-blocking on spec drafting, blocking on implementation).

---

## Table of Contents

- [3.1.0 Option Generation in the Pipeline](#310-option-generation-in-the-pipeline)
- [3.1.1 Possession State Determination](#311-possession-state-determination)
  - [3.1.1.1 Possession Source Authority](#3111-possession-source-authority)
  - [3.1.1.2 Possession Classification Rules](#3112-possession-classification-rules)
  - [3.1.1.3 Possession Uncertainty Handling](#3113-possession-uncertainty-handling)
- [3.1.2 Action Set Branching](#312-action-set-branching)
- [3.1.3 PASS Candidate Generation](#313-pass-candidate-generation)
  - [3.1.3.1 Eligibility Gate](#3131-eligibility-gate)
  - [3.1.3.2 Candidate Enumeration](#3132-candidate-enumeration)
  - [3.1.3.3 Pass Lane Viability Check](#3133-pass-lane-viability-check)
  - [3.1.3.4 Pass Type Derivation](#3134-pass-type-derivation)
  - [3.1.3.5 PassOption Construction](#3135-passoption-construction)
  - [3.1.3.6 Decisions Attribute Candidate Cap](#3136-decisions-attribute-candidate-cap)
- [3.1.4 SHOOT Candidate Generation](#314-shoot-candidate-generation)
  - [3.1.4.1 Eligibility Gate](#3141-eligibility-gate)
  - [3.1.4.2 Shooting Range Classification](#3142-shooting-range-classification)
  - [3.1.4.3 Goal Visibility Assessment](#3143-goal-visibility-assessment)
- [3.1.5 DRIBBLE Candidate Generation](#315-dribble-candidate-generation)
  - [3.1.5.1 Eligibility Gate](#3151-eligibility-gate)
  - [3.1.5.2 Space Vector Analysis](#3152-space-vector-analysis)
  - [3.1.5.3 DribbleOption Construction](#3153-dribbleoption-construction)
- [3.1.6 HOLD Candidate Generation](#316-hold-candidate-generation)
- [3.1.7 MOVE_TO_POSITION Candidate Generation](#317-move_to_position-candidate-generation)
  - [3.1.7.1 Eligibility Gate](#3171-eligibility-gate)
  - [3.1.7.2 Formation Slot Target Derivation](#3172-formation-slot-target-derivation)
  - [3.1.7.3 MoveOption Construction](#3173-moveoption-construction)
- [3.1.8 PRESS Candidate Generation](#318-press-candidate-generation)
  - [3.1.8.1 Eligibility Gate](#3181-eligibility-gate)
  - [3.1.8.2 Press Target Selection](#3182-press-target-selection)
  - [3.1.8.3 PressOption Construction](#3183-pressoptionc-construction)
- [3.1.9 INTERCEPT Candidate Generation](#319-intercept-candidate-generation)
  - [3.1.9.1 Eligibility Gate](#3191-eligibility-gate)
  - [3.1.9.2 Intercept Point Geometry](#3192-intercept-point-geometry)
  - [3.1.9.3 Time-to-Intercept Feasibility Check](#3193-time-to-intercept-feasibility-check)
  - [3.1.9.4 InterceptOption Construction](#3194-interceptoption-construction)
- [3.1.10 Option Set Invariants](#3110-option-set-invariants)
- [3.1.11 Worked Example — Full Option Generation Pass](#3111-worked-example--full-option-generation-pass)
- [3.1.12 Version History](#3112-version-history)

---

## 3.1.0 Option Generation in the Pipeline

Option generation is **Step 3** of the 6-step Decision Tree pipeline. It runs immediately
after `AssembleDecisionContext()` (Step 2) and before `ScoreOptions()` (Step 3). Its sole
responsibility is to produce a list of `ActionOption` structs representing every action
type the agent is currently eligible to attempt.

```
Step 2: AssembleDecisionContext()  → DecisionContext assembled (snapshot + attributes + context)
        ↓
Step 3: GenerateOptions()          ← THIS SECTION (§3.1)
        ↓
Step 4: ScoreOptions()             → §3.2 (utility scoring applied to option list)
```

**GenerateOptions()** is deterministic, side-effect-free, and reads exclusively from the
`DecisionContext` struct. It does not modify agent state, request world state, or call
execution systems. It returns a fixed-size pre-allocated array of `ActionOption` structs
(capacity = 7 action types + up to 10 PASS candidates = 17 maximum slots).

**Contract:**
- Input: `DecisionContext` (assembled in Step 2 from snapshot + MatchContext + TacticalContext + AgentState)
- Output: `ActionOption[]` — ordered list of valid candidates (may be empty; see §3.1.10)
- Duration: must complete within 0.09ms per agent (part of the 4ms total budget; see Section 6)
- Side effects: none

---

## 3.1.1 Possession State Determination

### 3.1.1.1 Possession Source Authority

The `PerceptionSnapshot` struct (Perception System #7 §3.7.1) does **not** contain a
`HasBall` boolean or `PossessingAgentId` field. This is an intentional design consequence
of the epistemic model: the Perception System reports only what the agent perceives, not
authoritative game state. Possession is not a perceptual datum — it is a game state datum.

Possession state is therefore sourced from `MatchContext`, which carries the authoritative
`BallState.PossessingAgentId` field (Ball Physics Spec #1). `MatchContext` is accessible
to the DT as a read-only input (KD-6); it is not derived from the perception snapshot.

```
Authoritative possession data flow:
  BallState.PossessingAgentId  (Ball Physics #1 — world state)
       ↓
  MatchContext.PossessingAgentId  (copied at simulation orchestrator level each heartbeat)
       ↓
  DecisionContext.AgentHasBall  (computed in AssembleDecisionContext(), Step 2)
       ↓
  GenerateOptions()  ← reads DecisionContext.AgentHasBall (this section)
```

**Critical constraint:** The DT uses `MatchContext.PossessingAgentId`, not any field from
`PerceptionSnapshot`. This is the only permitted exception to the "DT reads only from
PerceptionSnapshot" rule (FR-03). `MatchContext` is explicitly carved out in KD-6 and the
scope definition (Section 1.2) as publicly available game state, not world state.

### 3.1.1.2 Possession Classification Rules

`AssembleDecisionContext()` (Step 2) sets `DecisionContext.AgentHasBall` as follows:

```
AgentHasBall = (MatchContext.PossessingAgentId == this.AgentId)
```

This is a binary flag. There is no partial possession state at Stage 0.

`MatchContext.PossessedByTeam` is set by the orchestrator:

```
PossessedByTeam = OWN_TEAM   if PossessingAgentId is on this agent's team
                = OPPONENT   if PossessingAgentId is on the opposing team
                = CONTESTED  if PossessingAgentId == -1 (ball loose)
```

Both `AgentHasBall` and `PossessedByTeam` are used in option generation (§3.1.2).

### 3.1.1.3 Possession Uncertainty Handling

A situation arises where `AgentHasBall = true` but `PerceptionSnapshot.BallVisible = false`.
This is physically implausible — an agent cannot possess a ball it cannot see — but may
occur in edge cases at Stage 0 (e.g., ball briefly clipping into agent geometry). Handling:

- If `AgentHasBall = true` AND `BallVisible = false`:
  - Log `FM-DT-09` warning (unexpected state; not a hard failure)
  - Treat as `AgentHasBall = true` (world state is authoritative)
  - Allow possession-branch option generation to proceed
  - Scoring in §3.2 will naturally penalise options that depend on accurate ball position

A situation where `AgentHasBall = false` AND the agent clearly has the ball visually (e.g.,
after a tackle that Ball Physics has not yet registered) is handled by the simulation
orchestrator's tick ordering — this specification does not solve it. The DT always trusts
`MatchContext.PossessingAgentId` as ground truth.

---

## 3.1.2 Action Set Branching

`GenerateOptions()` first evaluates `DecisionContext.AgentHasBall` to determine which branch
of the action tree to enter. The two branches are mutually exclusive at Stage 0.

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    OPTION GENERATION BRANCHING                          │
├──────────────────────────────┬──────────────────────────────────────────┤
│ AgentHasBall = TRUE          │ AgentHasBall = FALSE                     │
│ (Possession branch)          │ (Off-ball branch)                        │
├──────────────────────────────┼──────────────────────────────────────────┤
│ §3.1.3  PASS     (0..N)      │ §3.1.7  MOVE_TO_POSITION (always 1)     │
│ §3.1.4  SHOOT    (0 or 1)    │ §3.1.8  PRESS            (0 or 1)       │
│ §3.1.5  DRIBBLE  (0 or 1)    │ §3.1.9  INTERCEPT        (0 or 1)       │
│ §3.1.6  HOLD     (always 1)  │                                          │
└──────────────────────────────┴──────────────────────────────────────────┘
```

**Branch invariant:** HOLD is always generated in the possession branch (§3.1.6).
MOVE_TO_POSITION is always generated in the off-ball branch (§3.1.7). This guarantees
the no-viable-option fallback is never needed in practice; at minimum one option always
exists. See §3.1.10 (Option Set Invariants).

**Stage 0 constraint:** An agent cannot generate options from both branches simultaneously.
The goalkeeper exception is out of scope (Goalkeeper Mechanics #11, Stage 1+).

---

## 3.1.3 PASS Candidate Generation

### 3.1.3.1 Eligibility Gate

PASS candidates are generated if and only if ALL of the following are true:

```
Gate condition:
  (1)  DecisionContext.AgentHasBall == true
  (2)  PerceptionSnapshot.VisibleTeammates.Length > 0
         — at least one teammate is confirmed visible
  (3)  MatchContext.Phase == OPEN_PLAY
         — no passing during set pieces at Stage 0
              (Stage 2 exception: set piece passing will be added in §7 extensions)
```

If Gate (2) fails — no visible teammates — PASS produces zero candidates. The agent
falls back to DRIBBLE, HOLD, or SHOOT depending on those sections' own gates.

### 3.1.3.2 Candidate Enumeration

For each `PerceivedAgent T` in `PerceptionSnapshot.VisibleTeammates`:

1. Compute `PassLaneScore(T)` (§3.1.3.3)
2. Derive `PassType` from distance/angle (§3.1.3.4)
3. If `PassLaneScore(T)` ≥ `MIN_PASS_LANE_SCORE` [GT] = 0.05, construct a `PassOption` (§3.1.3.5)
4. Otherwise: skip this teammate (lane is too congested to warrant scoring)

Candidates are generated in the order teammates appear in `VisibleTeammates`. Order
has no scoring significance (ScoreOptions() ranks all candidates). The Decisions
attribute cap (§3.1.3.6) may limit how many candidates are generated.

**Maximum candidates:** min(VisibleTeammates.Length, `DecisionsAttributeCap`) — see §3.1.3.6.
Absolute maximum is 10 (10 outfield teammates visible simultaneously).

### 3.1.3.3 Pass Lane Viability Check

A pass lane is viable if a straight-line ball path from the agent to teammate `T` is not
excessively obstructed by visible opponents.

**Geometric lane test:**

For each `PerceivedAgent O` in `PerceptionSnapshot.VisibleOpponents`:

```
// Vector from agent to target teammate T
lane_vec = T.PerceivedPosition − AgentPosition

// Perpendicular distance from opponent O to the pass lane line
// Uses scalar projection to find closest point on lane segment
t_proj        = Dot(O.PerceivedPosition − AgentPosition, lane_vec) / Dot(lane_vec, lane_vec)
t_proj_clamped = Clamp(t_proj, 0.0, 1.0)          // Only segment [0,1] is relevant
closest_point  = AgentPosition + t_proj_clamped × lane_vec
perp_distance  = |O.PerceivedPosition − closest_point|

// An opponent is an interceptor if within the lane width threshold
is_interceptor(O) := perp_distance < PASS_LANE_WIDTH_HALF   // [GT] = 0.8m
                     AND t_proj_clamped > 0.05               // Not behind the passer
                     AND t_proj_clamped < 0.95               // Not past the target
```

**PASS_LANE_WIDTH_HALF = 0.8m [GT]:** Half-width of the lane corridor. An opponent
centred in the lane at 0.8m from the line is close enough to credibly intercept.
Range: (0.0, 2.0]. Increasing this makes the DT more conservative about attempting
passes through traffic. Decreasing it makes the DT attempt passes through tighter gaps.

**Lane score formula:**

```
interceptor_count = count of VisibleOpponents where is_interceptor(O) == true

PassLaneScore(T) = Clamp(1.0 − (interceptor_count / PASS_LANE_DIVISOR), 0.0, 1.0)

PASS_LANE_DIVISOR = 3.0 [GT]
```

**PASS_LANE_DIVISOR = 3.0 [GT]:** Three interceptors in the lane → `PassLaneScore = 0.0`
(blocked). Two interceptors → 0.33. One interceptor → 0.67. Clear lane → 1.0.

**Verification (attribute extremes):**

| Interceptors in Lane | PassLaneScore |
|---------------------|---------------|
| 0 | 1.00 (clear lane — full score) |
| 1 | 0.67 (one player in path) |
| 2 | 0.33 (congested; risky) |
| 3+ | 0.00 (blocked) |

**Lane floor:** If `PassLaneScore(T)` < `MIN_PASS_LANE_SCORE = 0.05`, this teammate is
skipped entirely (no `PassOption` generated). The floor prevents generating candidates
so poor that scoring produces degenerate results. A lane score of 0.05 means the agent
considers an essentially blocked pass — this is intentionally prohibited.

### 3.1.3.4 Pass Type Derivation

`PassType` is derived geometrically from distance and angle to the target teammate.
This is a generation-time classification only — the DT selects *which* pass type
parameters to populate the `PassRequest` with. Physics execution is owned by Pass
Mechanics #5.

```
// Distance and angle from agent to teammate T
pass_distance = |T.PerceivedPosition − AgentPosition|
pass_angle    = angle of T.PerceivedPosition relative to AgentFacingDirection

PassType derived as:
  if pass_distance ≤ SHORT_PASS_MAX_DISTANCE:         SHORT_GROUND
  if pass_distance ≤ MEDIUM_PASS_MAX_DISTANCE:        THROUGH_BALL or GROUND (angle-dependent)
  if pass_distance >  MEDIUM_PASS_MAX_DISTANCE:       LONG_BALL
  if pass_angle    >  CROSS_ANGLE_THRESHOLD
    AND AgentPosition.x in WIDE_ZONE:                 CROSS (any sub-type)
```

| Constant | Value | Tag |
|----------|-------|-----|
| `SHORT_PASS_MAX_DISTANCE` | 15.0m | [GT] |
| `MEDIUM_PASS_MAX_DISTANCE` | 30.0m | [GT] |
| `CROSS_ANGLE_THRESHOLD` | 60° from forward arc | [GT] |

**Stage 0 limitation:** THROUGH_BALL vs GROUND discrimination within medium range is
determined by whether the target has forward velocity toward goal (`T.PerceivedVelocity`
dot goal direction > THROUGH_BALL_VELOCITY_THRESHOLD = 1.0 m/s [GT]). This is a
simplification; precise THROUGH_BALL geometry is a Stage 1 enhancement.

All `PassType` enum values are defined in Pass Mechanics Spec #5 §3.x. The DT does
not define or extend this enum.

### 3.1.3.5 PassOption Construction

A pass to a teammate behind the agent has a clear lane (no interceptors) and therefore
a high `PassLaneScore`. Without correction, backward passes would be scored equally to
forward passes with the same lane clearance — overvaluing sideways and backward options
relative to goal-progression intent. A directional modifier is applied at generation
time to embed goal-direction awareness into the candidate before scoring.

**Goal-direction modifier:**

```
// Vector from teammate T to opponent goal centre (goal progression potential)
goal_direction = Normalise(MatchContext.OpponentGoalCentre − AgentPosition)
pass_direction = Normalise(T.PerceivedPosition − AgentPosition)

// Cosine similarity: +1.0 = pass goes directly toward goal
//                    0.0 = pass is lateral (90°)
//                   -1.0 = pass goes directly backward
GoalDirectionCosine = Dot(pass_direction, goal_direction)    // [−1.0, +1.0]

// Map to a [GOAL_DIR_PENALTY, 1.0] modifier
// Backward passes (cosine = -1): modifier = GOAL_DIR_MIN_MODIFIER
// Lateral passes  (cosine =  0): modifier = midpoint
// Forward passes  (cosine = +1): modifier = 1.0

GOAL_DIR_MIN_MODIFIER = 0.5 [GT]
GoalDirectionModifier = GOAL_DIR_MIN_MODIFIER + ((GoalDirectionCosine + 1.0) / 2.0) × (1.0 − GOAL_DIR_MIN_MODIFIER)
                      = 0.5 + ((GoalDirectionCosine + 1.0) / 2.0) × 0.5

Modifier at key angles:
  Direct forward  (cosine =  1.0): GoalDirectionModifier = 1.00
  Diagonal fwd    (cosine =  0.71): GoalDirectionModifier = 0.93
  Lateral         (cosine =  0.0):  GoalDirectionModifier = 0.75
  Diagonal back   (cosine = −0.71): GoalDirectionModifier = 0.57
  Direct backward (cosine = −1.0):  GoalDirectionModifier = 0.50
```

**GOAL_DIR_MIN_MODIFIER = 0.5 [GT]:** The minimum modifier applied to a pure backward
pass. A backward pass is not forbidden — it is penalised. A clear backward pass to an
unmarked sweeper (PassLaneScore = 1.0) yields an adjusted lane score of 0.50, competing
against a congested forward option (PassLaneScore = 0.33) scoring 0.31 adjusted. The
backward pass still wins when the alternative is heavily congested. Range: (0.0, 1.0).
Setting to 0.0 would never generate backward pass candidates. Setting to 1.0 removes
the directional penalty entirely (original v1.0 behaviour).

**Adjusted PassLaneScore:**

```csharp
AdjustedPassLaneScore = PassLaneScore(T) × GoalDirectionModifier

// Floor still applies to adjusted score (not pre-adjustment)
if AdjustedPassLaneScore < MIN_PASS_LANE_SCORE:
    skip this teammate
```

```csharp
// Constructed for each teammate T that passes the adjusted lane score floor
PassOption passOption = new PassOption
{
    Type                  = ActionType.PASS,
    TargetAgentId         = T.AgentId,
    TargetPosition        = T.PerceivedPosition,
    PassLaneScore         = PassLaneScore(T),               // raw, pre-modifier [0.0, 1.0]
    AdjustedPassLaneScore = AdjustedPassLaneScore,          // modifier applied [0.05, 1.0]
    GoalDirectionCosine   = GoalDirectionCosine,            // stored for §3.2 scoring
    DerivedPassType       = PassType,                       // from §3.1.3.4
    BaseUtility           = 0.0f                            // set to 0; ScoreOptions() (§3.2) populates this
};
```

Both `PassLaneScore` (raw) and `AdjustedPassLaneScore` are stored. The scoring system
in §3.2.2 uses `AdjustedPassLaneScore` as the lane quality input. `GoalDirectionCosine`
is also forwarded to §3.2 where the PASS utility formula may apply additional
directional weighting (e.g., tactical instructions to play direct may amplify the forward
bonus; instructions to retain possession may suppress it).

### 3.1.3.6 Decisions Attribute Candidate Cap

An agent with a low `Decisions` attribute does not evaluate every possible passing option.
The attribute represents cognitive breadth under time pressure. High `Decisions` agents
assess more options; low `Decisions` agents consider fewer.

```
max_pass_candidates = Floor(2.0 + (A_Decisions × 8.0))

Where:
  A_Decisions = (Decisions_raw − 1) / 19     // normalise to [0, 1]

Candidate cap at attribute extremes:
  Decisions = 1  (A = 0.00): cap = Floor(2.0) = 2   candidates
  Decisions = 10 (A = 0.47): cap = Floor(5.8) = 5   candidates
  Decisions = 20 (A = 1.00): cap = Floor(10.0) = 10 candidates
```

If `VisibleTeammates.Length` < `max_pass_candidates`, all visible teammates are
evaluated (cap is non-binding).

If `VisibleTeammates.Length` ≥ `max_pass_candidates`, teammates are evaluated in
**proximity order** (closest first), and generation stops when the cap is reached.
This is not a scoring decision — it is a cognitive scope limit. A world-class
Decision-maker considers all options; a low-Decision player misses distant options.

**Gameplay intent:** A Decisions=1 player passes to one of the two nearest teammates,
potentially missing a completely unmarked player further away. This is the observable
tactical error that low-Decisions should produce.

---

## 3.1.4 SHOOT Candidate Generation

### 3.1.4.1 Eligibility Gate

SHOOT produces exactly zero or one candidate. Gate conditions:

```
Gate condition:
  (1)  DecisionContext.AgentHasBall == true
  (2)  PerceptionSnapshot.BallVisible == true
         — must currently see the ball (prevents phantom shots)
  (3)  pass_distance_to_goal ≤ ShootingRange(AgentAttributes)    [§3.1.4.2]
  (4)  goal_visibility_score > MIN_GOAL_VISIBILITY                [§3.1.4.3]
         — MIN_GOAL_VISIBILITY = 0.05 [GT]
  (5)  MatchContext.Phase == OPEN_PLAY
         — no shooting on set pieces at Stage 0
```

If all five conditions are met, exactly one `ShootOption` is generated.

### 3.1.4.2 Shooting Range Classification

The maximum shooting range is not fixed — it scales with `LongShots` attribute.

```
// Goal position is known from MatchContext (fixed pitch geometry)
goal_position     = MatchContext.OpponentGoalCentre   // Vector2 — centre of goal line
agent_position    = AgentPosition                     // from DecisionContext
distance_to_goal  = |goal_position − agent_position|

// LongShots attribute extends shooting range
A_LongShots = (LongShots_raw − 1) / 19              // normalise to [0, 1]

ShootingRange(attrs) = BASE_SHOOT_RANGE + (A_LongShots × LONGSHOT_RANGE_BONUS)

BASE_SHOOT_RANGE   = 20.0m [GT]      ← minimum range for all agents (Finishing gate only)
LONGSHOT_RANGE_BONUS = 15.0m [GT]   ← bonus range at LongShots = 20

Shooting range at attribute extremes:
  LongShots = 1  (A = 0.00): range = 20.0m (only close-range shots)
  LongShots = 10 (A = 0.47): range = 27.1m
  LongShots = 20 (A = 1.00): range = 35.0m (long-range attempts feasible)
```

**Field zone cross-check:** A shooting range of 35.0m from goal places the agent
approximately at the halfway line on a standard 105m pitch. Shots from further
than 35m are excluded entirely at Stage 0. This is consistent with the outline's
zone modifier table (SHOOT midfield modifier = 0.5; attacking modifier = 1.0).

### 3.1.4.3 Goal Visibility Assessment

Goal visibility is a proxy for how open the shooting lane is. It uses the same geometric
lane check as §3.1.3.3 but targets the goal rather than a teammate.

```
// Goal is modelled as a line segment: left_post to right_post
// Standard goal width: 7.32m → each post is 3.66m from centre
goal_left  = MatchContext.OpponentGoalCentre + Vector2(−3.66, 0)
goal_right = MatchContext.OpponentGoalCentre + Vector2(+3.66, 0)

// Compute angular width of unobstructed goal from agent's position
total_goal_arc    = AngularSpan(goal_left, goal_right, AgentPosition)    // degrees
blocked_goal_arc  = 0.0

// For each visible opponent between agent and goal line:
foreach O in VisibleOpponents where IsInShotPath(O):
    // Compute angular width that opponent O occludes of the goal
    O_blocking_angle = AngularOcclusionOf(O, goal_left, goal_right, AgentPosition)
    blocked_goal_arc += O_blocking_angle

unblocked_goal_arc = Max(total_goal_arc − blocked_goal_arc, 0.0)
GoalVisibilityScore = unblocked_goal_arc / total_goal_arc    // [0.0, 1.0]
```

`IsInShotPath(O)` is true if opponent O is between the agent and the goal (along the
axis of the shot, not the pass lane model). Identical in concept to §3.1.3.3 but
the target is the goal plane rather than a teammate position.

`GoalVisibilityScore` is stored in the `ShootOption` and consumed by §3.2.2 (SHOOT
utility formula: `GoalOpeningScore` field).

---

## 3.1.5 DRIBBLE Candidate Generation

### 3.1.5.1 Eligibility Gate

DRIBBLE produces zero or one candidate.

```
Gate condition:
  (1)  DecisionContext.AgentHasBall == true
  (2)  PerceptionSnapshot.BallVisible == true
  (3)  SpaceScore > MIN_DRIBBLE_SPACE             [§3.1.5.2]
         — MIN_DRIBBLE_SPACE = 0.10 [GT]
  (4)  AgentMovementState != GROUNDED             ← agent must be upright
  (5)  MatchContext.Phase == OPEN_PLAY
```

If Gate (3) fails — no space in any direction — DRIBBLE is not generated. The agent
must HOLD, PASS, or SHOOT.

### 3.1.5.2 Space Vector Analysis

Space availability is evaluated by scanning opponent proximity in 8 discretised
directional sectors at 45° intervals, covering the full 360° around the agent.

```
// Agent's forward direction (facing)
forward = AgentFacingDirection   // normalised Vector2

// Evaluate 8 directions at 45° intervals: full 360° coverage
candidate_directions = {
    forward,                    //   0° — straight ahead
    Rotate(forward, +45°),      //  45° — forward-right diagonal
    Rotate(forward, −45°),      // −45° — forward-left diagonal
    Rotate(forward, +90°),      //  90° — right
    Rotate(forward, −90°),      // −90° — left
    Rotate(forward, +135°),     // 135° — backward-right diagonal
    Rotate(forward, −135°),     // 135° — backward-left diagonal
    Rotate(forward, +180°)      // 180° — straight back
}

// For each direction, score is based on the nearest opponent in that directional sector.
// A sector is the 45°-wide arc centred on the direction vector.
// An opponent belongs to the sector whose centre vector is closest to the
// bearing from agent to opponent.
DRIBBLE_THREAT_RADIUS = 2.0m [GT]

foreach dir in candidate_directions:
    // Collect opponents whose bearing from agent falls within ±22.5° of dir
    sector_opponents = VisibleOpponents where |angle_between(dir, O.PerceivedPosition − AgentPosition)| < 22.5°
    
    if sector_opponents is empty:
        space_in_dir(dir) = 1.0    // no threat in this sector
    else:
        nearest_dist = MIN(|O.PerceivedPosition − AgentPosition|) for O in sector_opponents
        space_in_dir(dir) = Clamp(nearest_dist / DRIBBLE_THREAT_RADIUS, 0.0, 1.0)

// Best dribble direction: highest space score, forward-arc bias on tie
best_direction  = argmax(space_in_dir), tie broken by lowest sector index (forward-first)
SpaceScore      = space_in_dir(best_direction)    // [0.0, 1.0]
```

**DRIBBLE_THREAT_RADIUS = 2.0m [GT]:** An opponent within 2m of the agent's intended
dribble direction is a credible threat to the dribble. Range: (0.5m, 5.0m).
Increasing this makes agents more reluctant to dribble in traffic. Decreasing it
makes agents dribble more aggressively past nearby opponents.

**8-sector coverage and blind angle analysis:**

With 8 sectors at 45° spacing, each sector covers ±22.5°. An opponent at exactly 22.5°
from a sector boundary (i.e., 22.5° off a sector centre) is the worst-case scenario for
misclassification — it falls on the boundary between two adjacent sectors. In practice,
the opponent is correctly assigned to one sector and its threat is detected. There is no
blind angle: 8 sectors × 45° = 360° with no gaps.

The residual error is not missed detection but **within-sector resolution loss**: two
opponents at 10° and 40° within the same sector both contribute to `nearest_dist`. Since
only the nearest is used, an opponent at 40° offset within the sector would make the
agent perceive that direction as more threatened than a pure geometric check would show.
This is a conservative bias — it causes the agent to prefer cleaner directions, which is
the correct behaviour for dribble safety. The error magnitude is bounded:

```
Maximum within-sector angular error: ±22.5°
At DRIBBLE_THREAT_RADIUS = 2.0m, maximum lateral misattribution:
  2.0 × sin(22.5°) = 2.0 × 0.383 = 0.77m

This means an opponent 0.77m outside the true dribble path can suppress
the space score in that sector. This is an acceptable over-conservatism
given the 2.0m threat radius — opponents within 0.77m of the path are
genuinely relevant to dribble viability.
```

**Comparison with v1.0 5-sector model:** The 5-sector model had genuine gaps — a
defender pressing from 67.5° off forward (midway between the 45° and 90° sectors)
was assigned to the nearest sector but with up to ±22.5° angular error from a 45°
sector boundary, vs. ±22.5° maximum in the 8-sector model. The 8-sector model
eliminates the structural gap between sectors and reduces the worst-case angular
error from ±67.5° (5-sector midpoints) to ±22.5°. No backward-sector penalty is
applied to `SpaceScore` at generation time; the scoring stage (§3.2.2) applies
directional-to-goal modifiers to the DRIBBLE utility.

### 3.1.5.3 DribbleOption Construction

```csharp
DribbleOption dribbleOption = new DribbleOption
{
    Type           = ActionType.DRIBBLE,
    TargetPosition = AgentPosition + (best_direction × 5.0f), // 5m look-ahead [GT]
    SpaceScore     = SpaceScore,        // [0.10, 1.0] (floored by gate)
    BestDirection  = best_direction,    // used by Agent Movement execution
    BaseUtility    = 0.0f              // populated by ScoreOptions()
};
```

The 5.0m look-ahead target is a directional indicator, not a committed endpoint. Agent
Movement controls actual dribble path; the DT only specifies direction intent. This is
consistent with the DT's scope: it does not compute locomotion trajectories (Section 1.3).

---

## 3.1.6 HOLD Candidate Generation

HOLD is **always generated** when `AgentHasBall = true`. No gate conditions.

```csharp
HoldOption holdOption = new HoldOption
{
    Type        = ActionType.HOLD,
    BaseUtility = 0.0f   // populated by ScoreOptions(); base value = 0.25 [GT] per §3.2
};
```

HOLD has a deliberately low base utility (0.25 [GT] — see §3.2.2). It wins only when
all other options score below it. Its role is to guarantee the possession branch always
has at least one candidate, preventing the zero-candidate fallback (FR-08).

**Gameplay purpose:** HOLD = agent shields the ball, waits, or plays for time. Correct
behaviour under maximum pressure with no open passing lanes and out of shooting range.
An agent who always holds under pressure is not malfunctioning — it is correct within
the system constraints.

---

## 3.1.7 MOVE_TO_POSITION Candidate Generation

### 3.1.7.1 Eligibility Gate

MOVE_TO_POSITION is **always generated** when `AgentHasBall = false`. No gate conditions.

```
Gate condition:
  (1)  DecisionContext.AgentHasBall == false
```

MOVE_TO_POSITION mirrors HOLD's role in the off-ball branch: it guarantees the off-ball
candidate set is never empty. An agent without the ball always has somewhere to move.

### 3.1.7.2 Formation Slot Target Derivation

At Stage 0, `TacticalContext` carries a hardcoded formation slot per agent (KD known
limitation — see Section 1.6.2). The formation slot position is a Vector2 on the pitch
representing the agent's positional anchor.

```
// Formation slot target position (Stage 0: from TacticalContext hardcoded defaults)
formation_slot    = TacticalContext.GetFormationSlot(DecisionContext.AgentId)
                    // Returns Vector2 (pitch coordinates)

distance_to_slot  = |formation_slot − AgentPosition|
```

**Stage 0 limitation:** Both teams use identical positional roles. The formation slot is
a fixed anchor point, not a dynamic positioning instruction. Stage 1 wires the Formation
System (Spec #14) to provide live formation slot positions that adjust with tactical
instructions and ball position.

### 3.1.7.3 MoveOption Construction

```csharp
MoveOption moveOption = new MoveOption
{
    Type             = ActionType.MOVE_TO_POSITION,
    TargetPosition   = formation_slot,
    DistanceToSlot   = distance_to_slot,   // used in §3.2.2 utility formula
    BaseUtility      = 0.0f               // populated by ScoreOptions()
};
```

---

## 3.1.8 PRESS Candidate Generation

### 3.1.8.1 Eligibility Gate

PRESS produces zero or one candidate.

```
Gate condition:
  (1)  DecisionContext.AgentHasBall == false
  (2)  PerceptionSnapshot.VisibleOpponents.Length > 0
  (3)  A valid press target exists within PRESS_TRIGGER_DISTANCE     [§3.1.8.2]
  (4)  DecisionContext.StaminaAvailable == true
         — agent must have stamina to press (see §3.1.8.1 note below)
  (5)  MatchContext.Phase == OPEN_PLAY
```

**Stamina gate (Gate 4) — Stage 0 simplification:** At Stage 0, `StaminaAvailable` is
a binary threshold derived from `AgentState.AerobicStaminaPool`. If the pool is below
`PRESS_STAMINA_MINIMUM = 0.20 [GT]`, PRESS is not generated. The agent defaults to
MOVE_TO_POSITION. This prevents agents from pressing indefinitely, but is cruder than
the fatigue-weighted stamina model planned for Stage 1 (Fatigue System #13).

### 3.1.8.2 Press Target Selection

The press target is the highest-priority opponent to press given the snapshot data.
Priority order (evaluated in sequence; first match is the target):

```
Priority 1: VisibleOpponent O where O.AgentId == MatchContext.PossessingAgentId
              AND |O.PerceivedPosition − AgentPosition| ≤ PRESS_TRIGGER_DISTANCE
              ← press the ball-carrier directly if within range

Priority 2: VisibleOpponent O nearest to AgentPosition
              where |O.PerceivedPosition − AgentPosition| ≤ PRESS_TRIGGER_DISTANCE
              ← press the nearest opponent if ball-carrier is out of range

Priority 3: No valid target → PRESS not generated (Gate 3 fails)
```

```
PRESS_TRIGGER_DISTANCE = 8.0m [GT]
```

**PRESS_TRIGGER_DISTANCE = 8.0m [GT]:** Maximum distance at which pressing is
considered viable. Beyond 8m, agents do not initiate pressing — they move to
position instead. Range: (3.0m, 20.0m). Increasing this creates a higher-pressure
team shape. Decreasing it produces a more conservative, positional defensive style.

**Sensitivity analysis — degenerate case boundaries:**

This constant has the highest tuning impact of any single constant in §3.1 because
it gates whether PRESS is generated at all. Small changes produce large observable
differences in team defensive shape. The following boundaries must be validated
in Section 5 (Testing) before Stage 0 sign-off:

| Scenario | Expected behaviour | Degenerate if violated |
|----------|--------------------|------------------------|
| `PRESS_TRIGGER_DISTANCE` = 3.0m | Only closest-range pressing; team falls deep | Agents never press → effectively zero defensive pressure; all out-of-possession agents default to MOVE_TO_POSITION |
| `PRESS_TRIGGER_DISTANCE` = 8.0m (default) | Midblock pressing; agents press opponents within ~2 body lengths of a 60Hz physics body | Baseline — needs BAL-PRESS-01 verification (see §5) |
| `PRESS_TRIGGER_DISTANCE` = 20.0m | Half-pitch press triggers; nearly all opponents in range at all times | Every agent generates PRESS on every tick → PRESS utility must be consistently beaten by MOVE_TO_POSITION for off-ball agents not in position to press, or agents stampede forward |

**Required balance tests (Section 5):**

- `BAL-PRESS-01`: At default `PRESS_TRIGGER_DISTANCE = 8.0m` with `TacticalContext.Pressing = MEDIUM`, average fraction of off-ball agents generating PRESS each heartbeat should be 20–40%. If > 60%: reduce distance. If < 10%: increase distance or re-examine `ProximityScore` floor.
- `BAL-PRESS-02`: At `Pressing = HIGH`, fraction should be 40–70%. At `Pressing = LOW`, fraction should be 5–20%.
- `BAL-PRESS-03`: No agent should generate PRESS on > 8 consecutive heartbeats (800ms) without the ball changing possession — sustained pressing without resolution indicates the PRESS utility is never beaten by MOVE_TO_POSITION, which is a scoring system failure, not a generation failure.

These tests are documented here as generation-level requirements. Section 5 will implement them as integration tests verifying cross-system behaviour.

### 3.1.8.3 PressOption Construction

```csharp
PressOption pressOption = new PressOption
{
    Type           = ActionType.PRESS,
    TargetAgentId  = press_target.AgentId,
    TargetPosition = press_target.PerceivedPosition,
    ProximityScore = Clamp(1.0f − (distance_to_target / PRESS_TRIGGER_DISTANCE), 0.0f, 1.0f),
    BaseUtility    = 0.0f   // populated by ScoreOptions()
};
```

`ProximityScore` at extremes:
- Agent is 0m from press target: 1.0 (fully committed)
- Agent is 8m from press target: 0.0 (at the edge of the trigger zone)
- This value feeds directly into the PRESS utility formula in §3.2.2.

---

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
