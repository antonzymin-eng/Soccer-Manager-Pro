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
**Updated:** August 6, 2026, later same day (v1.5 — ERR-008-021 AR-1: the P3 exemption narrowed from the whole `GK_PROXIMITY_TO_GOAL` band to the SINGLE GK candidate (goal-line-nearest in band) — the band-wide form left every near-goal defender unweighted exactly where shots are blocked (H-1); the P5 exactness claim corrected to midpoint-and-null-view-only (M-2). Prior update below.)
**Updated (prior):** August 6, 2026 (v1.4 — ERR-008-021: §3.1.4.3's deferred shot-lane follow-up lands — outfield blocker occlusion is weighted by §3.1.3.3's Vision-read Anticipation/Pace `perceived_ability`; the goalkeeper's arc stays geometric per doctrine P3. Prior update below.)
**Updated (prior):** August 4, 2026 (v1.3 — ERR-008-020: §3.1.3.3 rewritten to the continuous, attribute-weighted lane-threat model; §3.1.4.3 gains the shot-lane deferral scope note)
**Version:** 1.5
**Status:** ✅ APPROVED — Lead developer signed off April 27, 2026 (draft-level quality gate; see §9 approval checklist). v1.1.1 (May 15, 2026): ERR-012-002 stale spec ref correction (§3.1.7.2 "Spec #14" → "Positioning AI, Spec #12"). v1.1.2 (May 17, 2026): ERR-013-004 stale spec name correction (§3.1.8.1 "Fatigue System #13" → "Pressing AI #13"). Both are single-token non-behavioral patches; no formula, contract, or pipeline change. Approval status preserved.
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
excessively threatened by visible opponents.

> **ERR-008-020 (August 4, 2026 — football-judgment proxy review §6.4, the doctrine's
> template fix).** The original form of this section counted every opponent inside a
> single 0.8 m corridor as exactly one "interceptor" and every opponent outside it as
> nothing — a 2 cm positional cliff (doctrine P1), and blind to who the defender is: a
> slow, poor-anticipation defender blocked a lane exactly as hard as an elite one
> (doctrine P2). Replaced by the continuous, attribute-weighted threat model below.
> The worked verification table of the original ("1 interceptor → 0.67") survives as
> the neutral row of the new table — an ability-neutral defender at lane centre still
> costs exactly one threat unit (doctrine P5: today's balance is the pivot).

**Per-opponent threat weight:**

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

// Endpoint exclusion (unchanged): opponents effectively behind the passer or
// past the target do not threaten the lane
if t_proj_clamped ≤ PASS_LANE_ENDPOINT_MARGIN                 // [GT] = 0.05
   OR t_proj_clamped ≥ 1.0 − PASS_LANE_ENDPOINT_MARGIN:  weight(O) = 0

// Positional falloff: full threat inside the core corridor, linear fade to zero
// at the outer edge (doctrine P1 — no cliff)
falloff(O) = Clamp((PASS_LANE_FALLOFF_END − perp_distance)
                   / (PASS_LANE_FALLOFF_END − PASS_LANE_CORE_HALF_WIDTH), 0.0, 1.0)

// True interception ability from the defender's own attributes (units: none;
// inputs raw [1,20], A_X = (raw − 1)/19 ∈ [0,1])
ability_mean01  = 0.5 × (A_Anticipation(O) + A_Pace(O))
true_ability(O) = INTERCEPTOR_ABILITY_MIN
                  + (INTERCEPTOR_ABILITY_MAX − INTERCEPTOR_ABILITY_MIN) × ability_mean01

// The passer's Vision as discrimination fidelity (doctrine P2): a low-Vision
// passer reads every defender as near-average — he never invents information,
// he fails to resolve it
fidelity            = LANE_VISION_FIDELITY_FLOOR
                      + (1.0 − LANE_VISION_FIDELITY_FLOOR) × A_Vision(passer)
perceived_ability(O) = 1.0 + fidelity × (true_ability(O) − 1.0)

weight(O) = falloff(O) × perceived_ability(O)
```

**Constants:**

| Constant | Value | Tag | Meaning |
|---|---|---|---|
| `PASS_LANE_CORE_HALF_WIDTH` | 0.4 m | [GT] | Full-threat corridor half-width. Range (0.0, `PASS_LANE_FALLOFF_END`). |
| `PASS_LANE_FALLOFF_END` | 1.2 m | [GT] | Perpendicular distance at which positional threat reaches zero. Range (`PASS_LANE_CORE_HALF_WIDTH`, 3.0]. |
| `INTERCEPTOR_ABILITY_MIN` | 0.6 | [GT] | Ability scalar at Anticipation/Pace raw 1/1. Range (0.0, 1.0]. |
| `INTERCEPTOR_ABILITY_MAX` | 1.4 | [GT] | Ability scalar at raw 20/20. Range [1.0, 2.5). Midpoint of MIN..MAX MUST equal 1.0 so the league-average defender is weight-neutral. |
| `LANE_VISION_FIDELITY_FLOOR` | 0.2 | [GT] | Fraction of ability deviation a Vision-1 passer still resolves. Range [0.0, 1.0). |
| `PASS_LANE_ENDPOINT_MARGIN` | 0.05 | [GT] | Unchanged from the original model. |

**Calibration note (doctrine P5):** the falloff ramp is deliberately centred on the
original 0.8 m cliff — core 0.4 m + 0.8 m linear fade integrates to the same total
threat over a uniformly distributed defender position as the old binary corridor, so
the fix does not systematically loosen or tighten passing; it redistributes threat
from a step to a slope. These are first-guess `[GT]`s: full calibration waits for a
complete-engine pass per KD-W1 (`match-engine-wiring-backlog.md`).

**Lane score formula:**

```
lane_threat = Σ weight(O) over VisibleOpponents

PassLaneScore(T) = Clamp(1.0 − (lane_threat / PASS_LANE_DIVISOR), 0.0, 1.0)

PASS_LANE_DIVISOR = 3.0 [GT]
```

**PASS_LANE_DIVISOR = 3.0 [GT]:** Summed lane threat of 3.0 → `PassLaneScore = 0.0`
(blocked). The neutral-defender rows below reproduce the original integer table.

**Worked example.** Passer (Vision raw 20 ⇒ `A_Vision` = 1.0, `fidelity` = 1.0) at
(52, 34) passing to (62, 34); defender at (57, 34.2) ⇒ `t_proj` = 0.5, `perp` = 0.2 m
⇒ `falloff` = 1.0 (inside core). Defender Anticipation/Pace raw 20/20 ⇒
`ability_mean01` = 1.0 ⇒ `true_ability` = 1.4 ⇒ `weight` = 1.4 ⇒
`PassLaneScore` = 1 − 1.4/3 = **0.533**. The identical geometry with a raw-1/1
defender gives `weight` = 0.6 ⇒ score **0.80**. The same two defenders read by a
Vision-1 passer (`fidelity` = 0.2): weights 1.08 / 0.92 ⇒ scores 0.64 / 0.693 — the
low-Vision passer barely tells them apart, which is the pre-fix behaviour.

**Verification:**

| Scenario (defender at lane centre unless noted) | Weight | PassLaneScore |
|---|---|---|
| Clear lane | 0.0 | 1.00 |
| 1 ability-neutral defender (the P5 pivot row — old "1 interceptor") | 1.00 | 0.67 |
| 2 ability-neutral defenders | 2.00 | 0.33 |
| 3+ ability-neutral defenders | ≥ 3.00 | 0.00 |
| 1 elite defender (20/20), Vision-20 passer | 1.40 | 0.53 |
| 1 poor defender (1/1), Vision-20 passer | 0.60 | 0.80 |
| 1 neutral defender at perp 0.79 m / 0.81 m (old cliff edge) | 0.51 / 0.49 | 0.83 / 0.84 |

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
         — MIN_GOAL_VISIBILITY = 0.12 [GT]  (retuned 0.05 → 0.12, July 27, 2026, shot-outcome
           design KD-7: at 0.05 it equalled the §3.2.3.2 step-5 GOAL_OPENING_MIN floor, so the
           gate could only fire on the degenerate zero-arc return and a fully walled-off shot
           was generated, scored and taken)
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
goal_left  = MatchContext.OpponentGoalCentre + Vector2(0, −3.66)
goal_right = MatchContext.OpponentGoalCentre + Vector2(0, +3.66)
// ERR-008-011 (June 11, 2026 audit): v1.1 offset the posts along X — the goal line
// runs along Y at fixed X (Ball Physics #1 §1.2 corner-origin; §3.2.1.4 PitchGeometry
// has the correct Y ± 3.66 form, which the implementation uses).

// Compute angular width of unobstructed goal from agent's position
total_goal_arc    = AngularSpan(goal_left, goal_right, AgentPosition)    // degrees
blocked_goal_arc  = 0.0

// ERR-008-021 (AR-1 H-1 form): exactly ONE opponent — the goal-line-nearest
// visible opponent within GK_PROXIMITY_TO_GOAL (§3.2.3.2 step 3 heuristic;
// first in snapshot order on an exact tie; independent of IsInShotPath) —
// is the GK candidate and is exempt from the ability weighting below.
gk_candidate = argmin over VisibleOpponents O with |O.x − goal_line_x| ≤ GK_PROXIMITY_TO_GOAL
               of |O.x − goal_line_x|          // −1 if no opponent is in the band

// For each visible opponent between agent and goal line:
foreach O in VisibleOpponents where IsInShotPath(O):
    // Compute angular width that opponent O occludes of the goal
    O_blocking_angle = AngularOcclusionOf(O, goal_left, goal_right, AgentPosition)
    // ERR-008-021: a blocker's occlusion scales by the shooter's perceived read
    // of his blocking ability — §3.1.3.3's perceived_ability(O) (Anticipation/
    // Pace mapped to INTERCEPTOR_ABILITY_MIN..MAX, read through the shooter's
    // Vision fidelity). Only the single GK CANDIDATE's arc stays geometric —
    // see §3.2.3.2 step 3a.
    if O is not gk_candidate:
        O_blocking_angle ×= perceived_ability(O)
    blocked_goal_arc += O_blocking_angle

unblocked_goal_arc = Max(total_goal_arc − blocked_goal_arc, 0.0)
GoalVisibilityScore = unblocked_goal_arc / total_goal_arc    // [0.0, 1.0]
```

`IsInShotPath(O)` is true if opponent O is between the agent and the goal (along the
axis of the shot, not the pass lane model). Identical in concept to §3.1.3.3 but
the target is the goal plane rather than a teammate position.

> **ERR-008-021 (August 6, 2026 — the follow-up deferred at ERR-008-020's landing,
> football-judgment proxy review §6.4).** The occlusion model above was already
> continuous in position (no P1 cliff), but attribute-blind: a slow,
> poor-anticipation defender walled off the goal exactly as hard as an elite one
> standing in the identical spot (pattern (a)). Each blocker's occluded arc now
> scales by the same `perceived_ability(O)` scalar as the pass lane — his
> Anticipation/Pace blocking ability read through the *shooter's* Vision as
> discrimination fidelity (doctrine P2). No new constants: the §3.1.3.3 `[GT]`s
> (`INTERCEPTOR_ABILITY_MIN/MAX`, `LANE_VISION_FIDELITY_FLOOR`) are reused
> verbatim, keeping one calibration lever per KD-W1. Exactly ONE opponent — the
> **single GK candidate** (goal-line-nearest within `GK_PROXIMITY_TO_GOAL`;
> AR-1 H-1 corrected the original band-wide exemption, under which every
> defender within 6 m of the goal line escaped the weighting precisely where
> shots are blocked) — is deliberately NOT weighted: the *keeper's*
> shot-stopping quality is priced once, at the #11 save resolution — weighting
> his occlusion here would double-count it (doctrine P3), and
> `GK_BLOCKER_RADIUS` is already an abstraction of coverage rather than a body.
> A second defender inside the band has no save resolution pricing him and IS
> weighted (his radius still follows the band heuristic — the recorded Stage-0
> limitation in §3.2.3.2). The multiplier is exactly 1.0 for an
> attribute-view-less blocker or one at the ability midpoint (Anticipation+Pace
> `mean01` = 0.5, e.g. raw 10/11); the all-default raw-10/10 profile reads
> ≈ 0.979, so today's arcs are the pivot approximately, exact at the midpoint
> and under a null view (doctrine P5). Formula authority and step numbering:
> §3.2.3.2 step 3a.

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
applied to `SpaceScore` at generation time; the scoring stage applies the
directional-to-goal modifier to the DRIBBLE utility — `DirectionQuality_DRIBBLE`
in §3.2.4.1.

> **ERR-008-018 (August 3, 2026).** The cross-reference above previously pointed at
> §3.2.2, which is the **PASS** utility formula; §3.2.4 is DRIBBLE's. The promised
> modifier was therefore never given a home, and §3.2.4.1's formula shipped without
> it. Because `SpaceScore` is direction-blind by construction and `best_direction`
> is chosen by `argmax(space)`, a dribble away from goal scored exactly as well as
> the same dribble at goal — and in the final third, where the free space is behind
> the carrier, that is the direction the argmax picks. Measured over six full
> matches, DRIBBLE was the modal carrier action in the attacking third (40% of
> heartbeat decisions) with a mean cosine to the opponent goal of **−0.30**: the
> average final-third dribble pointed away from the goal. `DirectionQuality_DRIBBLE`
> is added to §3.2.4.1 in the same commit.

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
System (Positioning AI, Spec #12) to provide live formation slot positions that adjust with tactical
instructions and ball position.

**Stage 1+ RUNNER override (ERR-015-002):** When `TacticalContext.AttackIntent` is
non-null and the agent's `AttackIntent.role == RUNNER`, the `MOVE_TO_POSITION` target is
the `runTargetPosition` derived from `AttackIntent.runParameters` (§3.4 of Attacking AI
#15) instead of the formation slot. The formation slot remains the default for all other
roles (SUPPORT_BALL / HOLD_WIDTH / WEAK_SIDE). This override is a Stage 1 deliverable;
at Stage 0 `TacticalContext.AttackIntent` is always null.

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
the fatigue-weighted stamina model planned for Stage 1 (Pressing AI #13).

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

## 3.1.13 SAVE generation (ERR-008-013 back-prop anchor)

The off-ball branch generates one additional candidate, `ActionType.SAVE = 7` (the DT-emitted
goalkeeper save the #11 `SaveIntent` doc anticipates the DT committing). It is gated on the new
`TacticalContext.SaveAvailable` fact — set only for the threatened keeper, only under the match
engine's opt-in `EnableGkHeading` flag (from `GkHeadingIntentSource.SaveArmed` geometry). When
`SaveAvailable`, the off-ball branch emits **SAVE alone** (MOVE/PRESS/INTERCEPT suppressed), so the
keeper's save is selected robustly rather than competing on utility (a must-happen, geometry-gated
action must not depend on out-scoring INTERCEPT, which can reach the utility ceiling under an
aggressive tactic). Flag-off / non-keeper ⇒ `SaveAvailable` false ⇒ this section is inert and the
off-ball branch is byte-identical to §3.1.7–§3.1.9. Owned by ERR-008-013 + the code
(`OptionGenerator.GenerateSaveCandidate`); scoring is §3.2, dispatch §3.5.

---

## 3.1.14 Loose-ball collect (ERR-008-014 back-prop anchor)

Before this correction the tree had **no action at all that fetches a loose ball lying at rest**: §3.1.7
MOVE_TO_POSITION targets the formation slot, §3.1.8 PRESS requires an opponent target, and §3.1.9.1 rejects
any ball below `INTERCEPT_MIN_BALL_SPEED`. Composed in the match engine, play therefore stopped for good the
first time a pass ran out of momentum in space — measured, with the nearest agent 13.75 m away (beyond
§3.1.9.3's `MAX_INTERCEPT_TIME` reach of roughly ten metres) and all 22 agents settling onto their formation
slots around a ball none of them could decide to fetch.

One change, purely additive:

1. **§3.1.9.1's minimum-ball-speed gate is UNCHANGED.** It keeps rejecting every slow ball, possessed or
   loose, and that is correct: no slow ball should reach §3.1.9.2's look-ahead geometry, where at v ≈ 0
   every projected point collapses onto the ball's own position and the `MAX_INTERCEPT_TIME` cap makes a
   ball beyond roughly ten metres un-chaseable by anyone. Loosening the gate to "intercept-eligible while
   LOOSE" was considered and **rejected**: it would make every off-ball agent eligible to chase a resting
   ball, which is exactly the converge-and-dither behaviour the single designated collector in item 2
   exists to prevent. One consequence is accepted rather than covered — a loose ball between the host's
   pickup gate (`FIRST_TOUCH_MIN_BALL_SPEED_M_S`) and `INTERCEPT_MIN_BALL_SPEED` is claimable by nobody
   for the fraction of a second it takes to decelerate below the lower gate. It is transient and
   self-healing, since drag only ever carries the ball DOWN through that band.
2. **The loose case routes to a dedicated collect**, gated on the new `TacticalContext.LooseBallCollector`
   fact and emitted as the **SOLE** off-ball option — the §3.1.13 SAVE pattern, for the reason ERR-008-013's
   AR-4 established: a must-happen action cannot depend on out-scoring a competitor under composure noise.
   It does not: the collect scores ~0.35 against MOVE_TO_POSITION's ~0.21 on neutral attributes, a gap of
   0.14 inside the ±0.15 noise band, and the designated collector measurably flip-flopped and never arrived.
   The collect skips §3.1.9.2's look-ahead geometry (at v ≈ 0 every projected point is the ball's own
   position) and carries feasibility 1.0, since for a stationary ball being the designated player IS the
   feasibility.

`LooseBallCollector` is set by the match engine, not derived inside the tree: it is a team-level role
assignment from team state (the Pressing AI #13 primary-presser precedent) and — load-bearing — only the
host knows which agents are **sent off**. A perception-derived "no teammate I can see is closer" rule
deadlocked on a frozen red-carded agent that eleven teammates deferred to. No collector designated ⇒ the
fact is false for every agent ⇒ this section is inert and the off-ball branch is byte-identical to
§3.1.7–§3.1.9. Owned by ERR-008-014 + the code (`OptionGenerator.GenerateLooseBallCollectCandidate`);
scoring is §3.2 (unchanged — it is an INTERCEPT), dispatch §3.5.

---

## 3.1.12 Version History

| Version | Date | Author | Changes |
|---|---|---|---|
| 1.0 | March 01, 2026 | Claude (AI) / Anton | Initial draft. §3.1.0–§3.1.8 option generation for all 7 action types. |
| 1.1 | May 18, 2026 | Claude (AI) / Anton | Non-behavioral patch. ERR-015-002 §3.1.7 update: Stage 1+ RUNNER override note added to §3.1.7.2 — when AttackIntent.role == RUNNER, MOVE_TO_POSITION target is runTargetPosition from #15 §3.4 instead of formation slot. Stage 0 behavior unchanged (AttackIntent null). Approval status preserved. |
| 1.1 | March 01, 2026 | Claude (AI) / Anton | Self-critique corrections. |
| 1.1.1 | May 15, 2026 | Claude (AI) / Anton | Non-behavioral patch per ERR-012-002: §3.1.7.2 "Formation System (Spec #14)" → "Formation System (Positioning AI, Spec #12)". Single-token correction. Approval status preserved. |
| 1.1.2 | May 17, 2026 | Claude (AI) / Anton | Non-behavioral patch per ERR-013-004: §3.1.8.1 "Fatigue System #13" → "Pressing AI #13". Single-token correction (current Spec #13 is Pressing AI; Fatigue System is a separate Stage-1 spec with no allocated number). Approval status preserved. |
| 1.2 | August 4, 2026 | — | ERR-008-018 back-prop (close-chance-creation pass, §5.Z.24): §3.1.5.2's closing delegation pointed the DRIBBLE directional-to-goal modifier at **§3.2.2, the PASS formula**, so the promised term was never given a home and §3.2.4.1 shipped without it. Cross-reference corrected to §3.2.4.1 and the measured consequence recorded inline (final-third dribbles: 40% of carrier decisions, mean cosine to goal −0.30 over six full matches). Generation-stage behaviour is UNCHANGED — `best_direction` is still the free-space argmax; only the delegation target is corrected. |
| 1.3 | August 4, 2026 | — | ERR-008-020 (football-judgment proxy review §6.4 — the doctrine's template fix; spec + code, same commit). §3.1.3.3 rewritten: the binary 0.8 m `is_interceptor` corridor (a 2 cm positional cliff, blind to defender identity) becomes a continuous per-opponent threat weight — linear positional falloff (core 0.4 m [GT], zero at 1.2 m [GT]; ramp centred on the old cliff so integrated threat is preserved) × the defender's Anticipation/Pace ability (0.6–1.4 [GT], average ⇒ exactly 1.0) read through the passer's Vision fidelity (floor 0.2 [GT] — doctrine P2, low Vision degrades to the attribute-blind read). `PASS_LANE_WIDTH_HALF` removed; lane floor, endpoint margin, and `PASS_LANE_DIVISOR` unchanged. §3.1.4.3 gains the scope note deferring the shot lane to a follow-up. Consumers: `UtilityWeights.cs` v1.7, `OptionGenerator.cs` v1.6, `DecisionContext(.Assembler).cs`, `DecisionTree.cs` v1.6, `MatchEngine.cs` v1.61 (the attribute-view wiring). |
| 1.4 | August 6, 2026 | — | ERR-008-021 (the shot-lane follow-up deferred at ERR-008-020's landing; spec + code, same commit). §3.1.4.3: each OUTFIELD blocker's occluded arc is scaled by §3.1.3.3's `perceived_ability(O)` (Anticipation/Pace → `INTERCEPTOR_ABILITY_MIN..MAX`, read through the shooter's Vision fidelity — doctrine P2); the goalkeeper's arc stays purely geometric (doctrine P3 — keeper quality is priced once, at the #11 save). No new constants (KD-W1: one calibration lever); neutral/null-view ability = 1.0 ⇒ today's arcs exactly (doctrine P5). Formula authority: §3.2.3.2 step 3a. Consumers: `OptionGenerator.cs` v1.7, `OptionGeneratorTests.cs` v1.7. |
| 1.5 | August 6, 2026 | — | ERR-008-021 AR-1 (same-day adversarial review over the landing). **H-1:** the v1.4 exemption keyed on the 6 m GK band, so EVERY near-goal defender escaped the weighting — inert exactly where shots are blocked; now a `gk_candidate` pre-pass (goal-line-nearest in band, snapshot-order tie-break, independent of `IsInShotPath`) exempts exactly one opponent, and every other blocker is weighted (radius stays per-band — the recorded §3.2.3.2 Stage-0 limitation). **M-2:** "exactly 1.0 for a league-average blocker" corrected — exact only at ability midpoint (`mean01` = 0.5, raw 10/11) or under a null view; the all-default 10/10 profile reads ≈ 0.979. Consumers: `OptionGenerator.cs` v1.8, `OptionGeneratorTests.cs` v1.8 (incl. the in-band-defender-is-weighted lock). |

