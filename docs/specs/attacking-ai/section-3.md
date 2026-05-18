# Attacking AI Specification #15 — Section 3: Core Formulas and Algorithms

**Created:** May 17, 2026
**Last Updated:** May 17, 2026 (v0.1 — initial draft from `outline-detailed.md` v1.1)
**Version:** 0.1
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.1

---

This section publishes the per-tick computation pipeline for Attacking AI #15.
Every formula carries units, valid input ranges, and at least one worked
example (FR-AT-028 / CLAUDE.md "When Writing or Editing Specs").

The per-tick pseudocode is in §3.13; §3.1–§3.12 define each step in detail.
The constants catalogue forward reference is in §3.14; the full catalogue
with derivations appears in §6.1 and Appendix A.

---

## 3.1 Phase Gating

**Purpose:** Determine whether the attacking-AI pipeline runs this tick.
§3.1 is a **pure gate** — it dispatches to §3.9 for non-IN_POSSESSION states
and does not duplicate transition logic.

**Binding:** #12 Positioning AI phase enum per KD-5 / KD-6. Accessor:
`PositioningAI.GetPhase(TeamId)` (Stage 1 declaration per #12 §4.5.1).

**Algorithm:**

```
phase = PositioningAI.GetPhase(thisTeam)   // read-only; fails safe to
                                            // OUT_OF_POSSESSION on error

if phase == OUT_OF_POSSESSION:
    emit emptyDirective
    return

if phase == TRANSITION or (prevPhase == IN_POSSESSION and phase != IN_POSSESSION):
    directive = TransitionController(prevPhase, phase, state)   // §3.9
    return directive

// phase == IN_POSSESSION
state.transitionHoldTick = 0    // reset countdown; arrival in IN_POSSESSION
                                 // cancels any residual TRANSITION holdback
proceed to §3.2
```

**Failure mode F4 (FR-AT-027):** If the #12 phase read fails or returns an
unrecognized value, treat as OUT_OF_POSSESSION and emit empty directive. This
is the safe fallback — no spurious attacking output when phase is unknown.

**Note on prevPhase:** `prevPhase` is the phase value recorded at the end of
the previous tick by `AttackHysteresisState` (§3.12 / §4.3). It is compared
here to detect the first tick of a phase transition. The comparison is
unambiguous because TransitionController (§3.9) sets the counter on the very
first transition tick, before decrementing.

---

## 3.2 Attacking Pool Construction

**Purpose:** Identify the set of agents eligible for attacking-role assignment
this tick.

**Binding:** KD-3 (ball-carrier exclusion) and KD-7 (GK exclusion).

**Algorithm:**

```
attackingPool = []
for each agent in thisTeam.allAgents:
    if agent.role == PlayerRole.Goalkeeper:
        continue    // GK excluded unconditionally (KD-7)
    if agent.entityId == ballCarrier.entityId:
        continue    // ball carrier excluded unconditionally (KD-3)
    attackingPool.append(agent)

// sort ascending by EntityId for determinism (#16 §3.2.5)
attackingPool.sortBy(a => a.entityId)
```

**Pool size:** 0 to 10 agents.
- Minimum pool: 0 (if all non-GK agents are ball-carrier — degenerate; emit
  empty directive).
- Typical pool: 9 agents (11-agent roster, 1 GK, 1 ball carrier).
- Maximum pool: 10 agents (GK included in roster count but excluded from pool;
  11-player team with 1 GK and 1 ball carrier = 9; edge case of squad
  rotation or simulated red card may reduce the pool further).

**Worked example:** 11-agent team (EntityIds 1–11). EntityId 3 = GK.
EntityId 7 = ball carrier. Pool = {1, 2, 4, 5, 6, 8, 9, 10, 11} — 9 agents,
sorted ascending.

**GK identification:** by `agent.role == PlayerRole.Goalkeeper`, NOT by field
position or entityId heuristic. This is the authoritative identifier per KD-7
and #11 Goalkeeper Mechanics §1.4.

---

## 3.3 Role Assignment Algorithm

**Purpose:** Assign each attacking pool agent one of four attacking roles:
RUNNER, SUPPORT_BALL, HOLD_WIDTH, or WEAK_SIDE.

**Iteration order:** EntityId-ascending (determinism per #16 §3.2.5 / FR-AT-003).

**Important field distinction:** RUNNER eligibility is controlled by
`formationSlot.lineMembership` (DEFENSE / MIDFIELD / ATTACK — the forward/
backward axis). Width-holding uses `laneAssignment` (5-bin lateral enum:
LEFT_WIDE / LEFT / CENTRE / RIGHT / RIGHT_WIDE). These are independent #12
fields; confusing them is a known hazard (see CLAUDE.md).

**Algorithm (per agent):**

```
for each agent in attackingPool (EntityId-ascending):

    // Step 1: Hysteresis check (§3.12)
    if hysteresis.isStable(agent.entityId, ATTACK_DWELL_TICKS):
        retain current role; continue

    // Step 2: Candidate role evaluation (priority order)
    candidateRole = HOLD_WIDTH   // default

    // Priority a: RUNNER
    if (agent.formationSlot.lineMembership == MIDFIELD
        or agent.formationSlot.lineMembership == ATTACK)
        and (runnerCount < MAX_RUNNERS):
        candidateRole = RUNNER
        runnerCount += 1

    // Priority b: SUPPORT_BALL (only if not RUNNER)
    else if distanceToBallCarrier(agent) <= SUPPORT_RADIUS_M * styleProfile.supportMult:
        candidateRole = SUPPORT_BALL

    // Priority c: WEAK_SIDE (only if not RUNNER or SUPPORT_BALL)
    // MIN_WEAK_SIDE_AGENT_THRESHOLD is a pool-size gate (FR-AT-015): only
    // assign WEAK_SIDE when the pool is large enough, and at most one agent
    // gets the WEAK_SIDE role per tick (weakSideCount == 0 guard).
    else if attackingPool.count >= MIN_WEAK_SIDE_AGENT_THRESHOLD
             and weakSideCount == 0
             and inWeakSideCorridor(agent.position.y, ball.y):
        candidateRole = WEAK_SIDE
        weakSideCount += 1

    // Priority d: HOLD_WIDTH (default — already set)

    agent.assignedRole = candidateRole

    // Step 3: If RUNNER, generate RunParameters (§3.4)
    if candidateRole == RUNNER:
        agent.runParameters = GenerateRunParameters(agent, ballCarrier, teamAttackAngle,
                                                    styleProfile, currentTick)
```

**Role catalog (FR-AT-012):** Exactly four values — RUNNER, SUPPORT_BALL,
HOLD_WIDTH, WEAK_SIDE. No other values exist.

**Worked example (9 agents, DIRECT style profile):**
- Agents 1, 2 (lineMembership ATTACK): assigned RUNNER (count 1, then 2).
  MAX_RUNNERS_DIRECT = 3 — still capacity.
- Agent 4 (lineMembership MIDFIELD): assigned RUNNER (count 3). Cap reached.
- Agent 5 (lineMembership MIDFIELD, distance to ball = 10.0m,
  SUPPORT_RADIUS_M × 0.8 (DIRECT supportMult) = 9.6m): 10.0 > 9.6 →
  not SUPPORT_BALL. Not weak-side. Assigned HOLD_WIDTH.
- Agent 6 (lineMembership MIDFIELD, distance to ball = 8.0m): 8.0 ≤ 9.6 →
  SUPPORT_BALL.
- Agents 8, 9, 10, 11 (lineMembership DEFENSE): Not RUNNER (cap met).
  Checked for SUPPORT_BALL by distance; WEAK_SIDE by Y-corridor; else HOLD_WIDTH.

---

## 3.4 Run Parameter Generation

**Purpose:** For each agent assigned RUNNER, compute the three `RunParameters`
fields that fully describe the run without any PatternType enum (KD-8 /
FR-AT-010 / FR-AT-011).

**Coordinate-frame definition:**

`teamAttackAngle` is the team's attack direction in pitch-frame (a match-half
constant, NOT the ball-carrier's velocity vector — using velocity would produce
degenerate runs from a stationary carrier):

- `0.0 rad` — team attacking the x=105 goal (positive-X direction)
- `π rad` — team attacking the x=0 goal (negative-X direction)

`depthOffset_m` is the forward component in the `teamAttackAngle` direction.
`lateralOffset_m` is the perpendicular component (positive = toward the
y=68 touchline when attacking x=105; positive = toward the y=0 touchline
when attacking x=0, due to π rotation). This convention preserves
"positive = right side from the attacking team's perspective" regardless
of which goal the team attacks.

**Formula:**

```
// Step 1: Compute raw offsets

depthOffset_m = Clamp(BASE_RUN_DEPTH_M [GT] * styleProfile.depthMult,
                      5.0, 40.0)
// Units: m. Clamp bounds: [5.0, 40.0] m.

centeredPct   = formationSlot.lateralPct - 0.5
// lateralPct ∈ [0, 1] from #12 §2.2 FormationSlot.
// centeredPct ∈ [-0.5, +0.5].
// Positive centeredPct = toward y=68 in pitch-frame.

lateralOffset_m = Clamp(centeredPct * PITCH_WIDTH_M [CROSS: #1 §1.2]
                         * LATERAL_SCALE [GT],
                        -34.0, 34.0)
// Units: m. Clamp bounds: [-34.0, 34.0] m (half pitch width).

runTriggerTick = currentTick
                 + max(1, round(BASE_RUN_TRIGGER_DELAY_TICKS [GT]
                                * styleProfile.timingMult))
// Units: ticks. Always >= currentTick + 1 (minimum delay of 1 tick).

// Step 2: Compute run target in pitch-frame

depthVec    = Vector2(cos(teamAttackAngle), sin(teamAttackAngle))
              * depthOffset_m
lateralVec  = Vector2(-sin(teamAttackAngle), cos(teamAttackAngle))
              * lateralOffset_m
// depthVec is in the attack direction.
// lateralVec is perpendicular to the attack direction (90° CCW rotation).

runTargetPosition = ballCarrier.position + depthVec + lateralVec

// Step 3: Clamp to pitch boundary

runTargetPosition.x = Clamp(runTargetPosition.x, 0.0, PITCH_LENGTH_M [CROSS: #1 §1.2])
runTargetPosition.y = Clamp(runTargetPosition.y, 0.0, PITCH_WIDTH_M  [CROSS: #1 §1.2])
```

**Valid input ranges:**

| Field | Range | Enforcement |
|---|---|---|
| `depthOffset_m` | [5.0, 40.0] m | Clamp applied in Step 1 |
| `lateralOffset_m` | [-34.0, 34.0] m | Clamp applied in Step 1 |
| `lateralPct` | [0.0, 1.0] | Source: #12 §2.2; trusted as-is |
| `teamAttackAngle` | {0.0, π} rad | Match-half constant |
| `runTriggerTick` | [currentTick + 1, ∞) | `max(1, ...)` enforces minimum |
| `runTargetPosition.x` | [0.0, 105.0] m | Clamp applied in Step 3 |
| `runTargetPosition.y` | [0.0, 68.0] m | Clamp applied in Step 3 |

**Worked example (DIRECT profile, team attacking x=105):**

Given:
- Ball carrier at position (70, 34) m.
- `teamAttackAngle = 0.0 rad`.
- Agent's `formationSlot.lateralPct = 0.75` (right-side channel).
- `BASE_RUN_DEPTH_M = 15.0 m` [GT], `depthMult` (DIRECT) `= 1.4`.
- `LATERAL_SCALE = 0.8` [GT].
- `BASE_RUN_TRIGGER_DELAY_TICKS = 3` [GT], `timingMult` (DIRECT) `= 0.7`.
- `currentTick = 100`.

Step 1:
- `depthOffset_m = Clamp(15.0 × 1.4, 5.0, 40.0) = Clamp(21.0, …) = 21.0 m`
- `centeredPct = 0.75 − 0.5 = 0.25`
- `lateralOffset_m = Clamp(0.25 × 68.0 × 0.8, −34.0, 34.0) = Clamp(13.6, …) = 13.6 m`
- `runTriggerTick = 100 + max(1, round(3 × 0.7)) = 100 + max(1, 2) = 102`

Step 2:
- `depthVec = Vector2(cos 0, sin 0) × 21.0 = Vector2(1, 0) × 21.0 = (21.0, 0.0)`
- `lateralVec = Vector2(−sin 0, cos 0) × 13.6 = Vector2(0, 1) × 13.6 = (0.0, 13.6)`
- `runTargetPosition = (70, 34) + (21, 0) + (0, 13.6) = (91.0, 47.6)`

Step 3:
- `Clamp(91.0, 0, 105) = 91.0 m` — inside pitch.
- `Clamp(47.6, 0, 68) = 47.6 m` — inside pitch.

Result: `RunParameters { depthOffset_m = 21.0, lateralOffset_m = 13.6,
runTriggerTick = 102 }`. `runTargetPosition = (91.0, 47.6)` — final third,
right channel → "overlap" geometry in gameplay vocabulary (Appendix F).

**Note on pattern vocabulary:** The label "overlap" is gameplay shorthand
documented in Appendix F only. No `RunType.OVERLAP` enum is used anywhere in
the algorithm (KD-8 / FR-AT-010).

**Counter-example (team attacking x=0, π rotation):**

Given:
- `teamAttackAngle = π rad`.
- Ball carrier at (35, 34) m. `lateralPct = 0.25` (left-of-centre).
- Same constants as above.

Step 2:
- `depthVec = Vector2(cos π, sin π) × 21.0 = Vector2(−1, 0) × 21.0 = (−21.0, 0.0)`
- `lateralVec = Vector2(−sin π, cos π) × 13.6`
  = `Vector2(0, −1) × 13.6`... wait: `lateralOffset_m` for `lateralPct = 0.25`:
  `centeredPct = 0.25 − 0.5 = −0.25`,
  `lateralOffset_m = Clamp(−0.25 × 68 × 0.8, −34, 34) = −13.6 m`.
  `lateralVec = Vector2(0, −1) × (−13.6) = (0.0, 13.6)`.
- `runTargetPosition = (35, 34) + (−21, 0) + (0, 13.6) = (14.0, 47.6)`.
- Clamp: `(14.0, 47.6)` — inside pitch. This agent runs into the left final
  third (x=14 is near the x=0 goal). Geometry is consistent with an "overlap"
  from the left-of-centre lane toward the y=68 side, which is the right side
  from the team's attacking perspective (π-rotated). Convention is preserved.

---

## 3.5 Support Radius Heuristic

**Purpose:** Determine which pool agents are within supporting distance of the
ball carrier and thus eligible for the SUPPORT_BALL role.

**Binding:** #7 Perception System §3.7 for agent positions (FR-AT-013).

**Formula:**

```
distanceToBallCarrier(agent) = Euclidean2D(agent.position, ballCarrier.position)
    = sqrt((agent.position.x - ballCarrier.position.x)^2
           + (agent.position.y - ballCarrier.position.y)^2)
// Units: m. Range: [0, ~148) m (diagonal of 105×68 pitch).

effectiveSupportRadius = SUPPORT_RADIUS_M [GT] * styleProfile.supportMult
// Units: m. Minimum effective radius floor: 5.0 m.
// The floor ensures the effective radius never collapses to near zero when
// an aggressive supportMult is used, so there is always at least a 5m
// zone around the ball carrier within which agents qualify as SUPPORT_BALL.
// Agents closer than 5m to the carrier are included (they are clearly
// in support range), not excluded.

isWithinSupportRadius = distanceToBallCarrier <= max(5.0, effectiveSupportRadius)
```

The `max(5.0, ...)` guard ensures the effective radius never collapses below
5.0 m regardless of the `supportMult` value. This prevents degenerate cases
where no agent can ever qualify as SUPPORT_BALL.

The heuristic is evaluated inside the role-assignment loop (§3.3) only for
agents not already assigned RUNNER. It is NOT a pre-pass over all agents;
the priority ordering in §3.3 means RUNNER eligibility is checked first.

**Worked example (POSSESSION profile):**

`SUPPORT_RADIUS_M = 12.0 m` [GT], `supportMult` (POSSESSION) `= 1.3`.
`effectiveSupportRadius = 12.0 × 1.3 = 15.6 m`.
Agent at (62, 34), ball carrier at (70, 38):
`distance = sqrt((62−70)² + (34−38)²) = sqrt(64 + 16) = sqrt(80) ≈ 8.94 m`.
`8.94 ≤ 15.6` → within support radius → SUPPORT_BALL candidate.

**Worked example (COUNTER_ATTACK profile):**

`SUPPORT_RADIUS_M = 12.0 m`, `supportMult` (COUNTER) `= 0.5`.
`effectiveSupportRadius = max(5.0, 12.0 × 0.5) = max(5.0, 6.0) = 6.0 m`.
Same agent at distance 8.94 m: `8.94 > 6.0` → NOT within support radius.
Counter-attacking teams keep their support agents back and favour runners over
close support. This emerges from the multiplier alone; no branching in code.

---

## 3.6 Width-Holding Protocol

**Purpose:** Ensure at least `MIN_WIDTH_HOLDERS` agents hold a wide position
on the touchline nearest to the ball, stretching the defensive shape and
maintaining attacking width.

**Binding:** FR-AT-014. Runs after role assignment (§3.3), not concurrently.

**Step 1 — Count near-touchline agents:**

```
nearSideCount = count of agents in attackingPool
                where (agent.assignedRole == HOLD_WIDTH
                       or agent.assignedRole == WEAK_SIDE)
                and agentIsOnNearSide(agent.position.y, ball.y)
// "Near side" = same touchline half as the ball.
// agentIsOnNearSide: ball.y >= PITCH_WIDTH_M/2 → near side is y ≥ 34;
//                   ball.y <  PITCH_WIDTH_M/2 → near side is y <  34.
// (Precise near-side definition is spatial proximity, not a strict half-split;
//  the condition used is: if ball.y >= 34, nearSide means agent.y >= 34.)
```

**Step 2 — Promote if deficient:**

```
if nearSideCount < MIN_WIDTH_HOLDERS [GT]:
    // Promote the agent closest to the near touchline (ascending |Y-deviation|)
    // among HOLD_WIDTH and non-RUNNER agents. EntityId tie-break.
    candidates = [a for a in attackingPool
                  if a.assignedRole != RUNNER]
    candidates.sortBy(a => abs(a.position.y - nearTouchlineY),
                      thenBy => a.entityId)
    if len(candidates) > 0:
        candidates[0].assignedRole = HOLD_WIDTH
        // Repeat until MIN_WIDTH_HOLDERS met or no candidates remain.
```

**HOLD_WIDTH target position formula:**

```
// TOUCHLINE_HOLD_DIST_M [GT] = distance from the touchline (NOT absolute Y).
// "Near touchline" = the touchline on the same side as the ball.

if ball.y >= PITCH_WIDTH_M / 2:
    nearTouchlineY = PITCH_WIDTH_M - TOUCHLINE_HOLD_DIST_M   // ball on y=68 side
else:
    nearTouchlineY = TOUCHLINE_HOLD_DIST_M                   // ball on y=0 side

targetPosition.x = ballCarrier.position.x   // tracks ball depth
targetPosition.y = nearTouchlineY
```

Units: m. The derivation is identical regardless of which goal the team
attacks — the formula depends only on `ball.y`, not `teamAttackAngle`.

**Worked example:**

`ball.y = 50 m` (y=68 side). `TOUCHLINE_HOLD_DIST_M = 4.0 m` [GT].
`nearTouchlineY = 68 − 4.0 = 64.0 m`.
Ball carrier at x = 75 m.
HOLD_WIDTH target = (75, 64) m — four metres from the y=68 touchline,
level with the ball carrier.

**Second worked example (ball on y=0 side):**

`ball.y = 18 m` (y=0 side). `TOUCHLINE_HOLD_DIST_M = 4.0 m`.
`nearTouchlineY = 4.0 m`.
Ball carrier at x = 60 m.
HOLD_WIDTH target = (60, 4) m — four metres from the y=0 touchline.

---

## 3.7 Weak-Side Positioning

**Purpose:** Assign one agent to hold the far side of the pitch opposite to
the ball, maintaining attacking width across the whole pitch and leaving space
for diagonal switches.

**Binding:** FR-AT-015. Formal weak-side definition from KD-16.

**Formal definition:**

"Weak side" is the half of the Y-axis opposite to the ball's current Y position.
Asymmetric thresholds (y < 30 / y > 38) prevent positional flicker at midfield Y.

```
if ball.y > PITCH_WIDTH_M / 2:        // ball on y=68 side
    weakSideTarget.y = WEAK_SIDE_FAR_Y_M [GT]               // near y=0 touchline
else:                                  // ball on y=0 side
    weakSideTarget.y = PITCH_WIDTH_M - WEAK_SIDE_FAR_Y_M [GT]  // near y=68 touchline

weakSideTarget.x = ballCarrier.position.x + WEAK_SIDE_DEPTH_OFFSET_M [GT]
// Units: m. Clamp to pitch: applied if needed.
```

**Agent selection:**

```
// O(N) scan; N <= 9.
bestAgent = null
maxYDeviation = -1.0
for each agent in attackingPool:
    if agent.assignedRole == RUNNER:
        continue    // do not pull runners off their run
    yDeviation = abs(agent.position.y - ball.y)
    if yDeviation > maxYDeviation
       or (yDeviation == maxYDeviation and agent.entityId < bestAgent.entityId):
        bestAgent = agent
        maxYDeviation = yDeviation

if bestAgent != null:
    bestAgent.assignedRole = WEAK_SIDE
    bestAgent.targetPosition = weakSideTarget
```

EntityId is the canonical tie-break (ascending) per #16 §3.2.5.

**Worked example:**

`ball.y = 50 m`. `WEAK_SIDE_FAR_Y_M = 8.0 m` [GT].
→ `weakSideTarget.y = 8.0 m` (near y=0 touchline).
`WEAK_SIDE_DEPTH_OFFSET_M = 5.0 m` [GT], ball carrier at x=70 m.
→ `weakSideTarget.x = 70 + 5.0 = 75.0 m`.
Weak-side target: (75, 8) m.

Pool agents (non-RUNNER) at Y positions: 45, 52, 60, 42, 38.
Y-deviations from ball.y=50: |45−50|=5, |52−50|=2, |60−50|=10, |42−50|=8, |38−50|=12.
Greatest deviation: agent at y=38, deviation=12. That agent → WEAK_SIDE.

**Minimum agent threshold (FR-AT-015):** If pool size < `MIN_WEAK_SIDE_AGENT_THRESHOLD` [GT],
the WEAK_SIDE assignment is skipped entirely. With a very small pool, forcing one
agent to the far side would leave too few agents near the ball.

---

## 3.8 Overload Detection

**Purpose:** Detect when the team has concentrated enough agents on the
ball-side flank to constitute an overload — a situation where the attacking
team has numerical superiority in a lateral zone, creating a goal-scoring
opportunity.

**Binding:** FR-AT-016 / FR-AT-036.

**Formula:**

```
// sameFlank(agentY, ballY, zoneWidth) := |agentY - ballY| <= zoneWidth

nearSideAgents = [a for a in attackingPool
                  if abs(a.position.y - ball.y) <= OVERLOAD_ZONE_WIDTH_M [GT]
                  and a.assignedRole != WEAK_SIDE]

if len(nearSideAgents) >= OVERLOAD_COUNT [GT]:
    AttackDirective.overloadActive = true
    AttackDirective.overloadFlank  = (ball.y > PITCH_WIDTH_M / 2) ? RIGHT : LEFT
else:
    AttackDirective.overloadActive = false
    // overloadFlank is undefined when overloadActive is false;
    // consumers must not read overloadFlank unless overloadActive is true.
```

**Units:** `OVERLOAD_ZONE_WIDTH_M` in metres. `OVERLOAD_COUNT` is a count
(dimensionless). `overloadFlank` is a positional indicator (LEFT / RIGHT /
NONE), not a movement-pattern enum — it identifies which side of the pitch
the overload is on (analogous to `MarkAssignment.mode` in #14; acceptable
per KD-8 scope clarification).

**WEAK_SIDE exclusion:** WEAK_SIDE agents are excluded from the overload count.
A weak-side agent's contribution to the far-side width would be incorrectly
counted as near-side pressure if not excluded.

**Worked example:**

`ball.y = 50 m`. `OVERLOAD_ZONE_WIDTH_M = 20.0 m` [GT]. `OVERLOAD_COUNT = 3` [GT].

Pool agents and their Y positions (after role assignment):
- Agent A: y=45, role=RUNNER. |45−50|=5 ≤ 20. Not WEAK_SIDE. → counted.
- Agent B: y=52, role=SUPPORT_BALL. |52−50|=2 ≤ 20. Not WEAK_SIDE. → counted.
- Agent C: y=60, role=HOLD_WIDTH. |60−50|=10 ≤ 20. Not WEAK_SIDE. → counted.
- Agent D: y=8, role=WEAK_SIDE. |8−50|=42 > 20. Also excluded as WEAK_SIDE. → not counted.
- Agent E: y=80... not valid (clamped to 68). If y=67: |67−50|=17 ≤ 20. → counted (4th).

nearSideAgents count = 3 (A, B, C) ≥ `OVERLOAD_COUNT = 3`. `ball.y = 50 > 34`.
→ `overloadActive = true`, `overloadFlank = RIGHT`.

---

## 3.9 TransitionController

**Purpose:** On possession loss, freeze the attacking directive for
`TRANSITION_HOLD_TICKS` ticks before emitting an empty directive. Prevents
jarring agent teleportation when possession flips.

**Binding:** KD-6. Dispatched from §3.1 when `phase != IN_POSSESSION`.
**Authoritative location:** This subsection owns all transition-counter logic.
§3.1 is a pure gate that dispatches here; §3.1 does not set, decrement, or
read the transition counter except to reset it to 0 on return to IN_POSSESSION.

**Algorithm:**

```
function TransitionController(prevPhase, currentPhase, state):

    // Step 1: Detect phase change and SET counter.
    // SET must occur BEFORE DECREMENT so the full hold window is preserved.
    if prevPhase == IN_POSSESSION and currentPhase != IN_POSSESSION:
        state.transitionHoldTick = TRANSITION_HOLD_TICKS [GT]

    // Step 2: Decrement and emit.
    if state.transitionHoldTick > 0:
        state.transitionHoldTick -= 1
        return frozenLastDirective      // emit the last IN_POSSESSION directive
                                        // frozen: no new runs triggered
    else:
        return emptyDirective           // countdown expired; emit empty

    // Step 3: (Handled in §3.1 gate, not here)
    // On return to IN_POSSESSION: §3.1 sets state.transitionHoldTick = 0.
```

**Profile values for `TRANSITION_HOLD_TICKS` [GT]:**
- POSSESSION profile: 5 ticks (500 ms). Agents hold attacking shape briefly.
- DIRECT profile: 5 ticks (500 ms). Same hold window.
- COUNTER_ATTACK profile: 0 ticks. Instant recovery — counter-attacking
  teams immediately abandon the last attacking directive and adopt defensive
  shape. No freeze window is appropriate when the game plan is rapid transition.

**Step-order rationale:** The SET (Step 1) must precede the DECREMENT (Step 2).
If a phase transition occurs on tick T:
- Step 1 sets `transitionHoldTick = 5`.
- Step 2 decrements to 4 and emits frozen directive.
- On tick T+1 (still TRANSITION): prevPhase is already not IN_POSSESSION, so
  Step 1 does not re-set. Step 2 decrements to 3. Continues until 0.
- On tick T+5 (if still TRANSITION): `transitionHoldTick` reaches 0 → emit empty.

This gives exactly `TRANSITION_HOLD_TICKS` frozen ticks before the empty
directive appears, which is the correct behaviour.

**Stage 1+ coupling (boundary hint only; not implemented at Stage 0):**
If #14 Defensive AI emits `MarkDirective.emergencyFlag = true` (per KD-6 / #14 §7.4),
#15 may override `transitionHoldTick = 0` immediately to accelerate defensive
shape recovery. The coupling is a Stage 1+ boundary hint; no interface is
authored at Stage 0 (Interface Design Principle — #14 is IN REVIEW).

---

## 3.10 Team-Style Profile Application

**Purpose:** Modulate run depth, run timing, and support radius without
algorithm branching — profile constants are loaded at match initialisation
and consumed as multipliers (KD-8 / KD-12 / FR-AT-017).

**Binding:** All multipliers are `[GT]` constants in the single constant
catalogue (`AttackingAIConstants.cs`). The algorithm code is identical across
all profiles; only the constant values differ.

**Profile-multiplier catalogue:**

| Multiplier Constant | POSSESSION | DIRECT | COUNTER_ATTACK | Tag |
|---|---|---|---|---|
| `DEPTH_MULT_POSSESSION` / `DEPTH_MULT_DIRECT` / `DEPTH_MULT_COUNTER` | 0.8 | 1.4 | 1.6 | `[GT]` |
| `TIMING_MULT_POSSESSION` / `TIMING_MULT_DIRECT` / `TIMING_MULT_COUNTER` | 1.2 | 0.7 | 0.5 | `[GT]` |
| `SUPPORT_MULT_POSSESSION` / `SUPPORT_MULT_DIRECT` / `SUPPORT_MULT_COUNTER` | 1.3 | 0.8 | 0.5 | `[GT]` |
| `MAX_RUNNERS_POSSESSION` / `MAX_RUNNERS_DIRECT` / `MAX_RUNNERS_COUNTER` | 1 | 3 | 4 | `[GT]` |
| `TRANSITION_HOLD_TICKS_POSSESSION` / `TRANSITION_HOLD_TICKS_DIRECT` / `TRANSITION_HOLD_TICKS_COUNTER` | 5 | 5 | 0 | `[GT]` |

These 15 constants (5 multiplier families × 3 profiles) live in the
`AttackingAIConstants.cs` catalogue.

**Application sites:**

| Formula location | Multiplier consumed | Effect |
|---|---|---|
| §3.4 `depthOffset_m` | `depthMult` | Deeper runs (COUNTER) or shallower (POSSESSION) |
| §3.4 `runTriggerTick` | `timingMult` | Earlier triggers (COUNTER) or later (POSSESSION) |
| §3.5 effective support radius | `supportMult` | Wider support net (POSSESSION) or tighter (COUNTER) |
| §3.3 RUNNER cap | `MAX_RUNNERS` | More simultaneous runners (COUNTER) or fewer (POSSESSION) |
| §3.9 transition hold | `TRANSITION_HOLD_TICKS` | Instant recovery (COUNTER) or brief freeze (others) |

**No enum branching:** The profile is selected at match initialisation by
loading the appropriate cluster of constant values into the `styleProfile`
struct. Inside the algorithm, `styleProfile.depthMult` is just a `float` —
there is no `switch(profile)` or `if (profile == COUNTER_ATTACK)` anywhere
in the algorithm code.

**Stage 0 default:** At Stage 0, all teams use POSSESSION profile constants
(the conservative default). Stage 1 wires real team-style selection via the
team-instruction infrastructure.

---

## 3.11 Anti-Chaos Invariant Enforcement

**Purpose:** Enforce three tactical safety invariants after role assignment
(§3.3) and before directive publication (FR-AT-021 / KD-13). These invariants
prevent algorithmically degenerate configurations — too many runners leaving no
support, runners assigned to own-half positions, etc.

**Timing:** POST-role-assignment, PRE-publication.

**Invariant 1 — Maximum runners (FR-AT-018):**

```
// Invariant: count of RUNNER roles <= MAX_RUNNERS
runnerCount = count(agent for agent in attackingPool if agent.role == RUNNER)
while runnerCount > MAX_RUNNERS:
    // Demote the runner with the smallest depthOffset_m (shallowest run;
    // least threatening to lose). EntityId tie-break (ascending).
    demotee = min(runners, key=(r.runParameters.depthOffset_m, r.entityId))
    demotee.assignedRole = SUPPORT_BALL
    demotee.runParameters = null
    runnerCount -= 1
```

**Invariant 2 — Minimum support agents (FR-AT-019):**

```
// Invariant: count of (SUPPORT_BALL + HOLD_WIDTH) >= MIN_SUPPORT_AGENTS
supportCount = count(agent for agent in attackingPool
                     if agent.role == SUPPORT_BALL or agent.role == HOLD_WIDTH)
while supportCount < MIN_SUPPORT_AGENTS:
    // Demote the runner with the smallest depthOffset_m (shallowest).
    // EntityId tie-break.
    if no runners remain: break    // cannot demote further; skip to fallback
    demotee = min(runners, key=(r.runParameters.depthOffset_m, r.entityId))
    demotee.assignedRole = SUPPORT_BALL
    demotee.runParameters = null
    supportCount += 1
```

**Invariant 3 — Own-half runner block (FR-AT-020):**

```
// Invariant: no RUNNER runTargetPosition.x is in own half beyond OWN_HALF_RUN_BLOCK_M
// "Own half" = x < HALF_LINE_X for team attacking x=105;
//              x > HALF_LINE_X for team attacking x=0.
for each runner in attackingPool where runner.role == RUNNER:
    distIntoOwnHalf = ownHalfDepth(runner.runParameters.runTargetPosition.x,
                                   teamAttackAngle, HALF_LINE_X [CROSS: #1 §1.2])
    // ownHalfDepth returns positive value if in own half, negative if in opp half.
    if distIntoOwnHalf > OWN_HALF_RUN_BLOCK_M [GT]:
        runner.assignedRole = HOLD_WIDTH
        runner.runParameters = null
```

**Fallback (FR-AT-026):**

```
// Applied if any invariant is still violated after MAX_INVARIANT_PASSES [GT]
// iterations of the full invariant check:
passCount = 0
while any_invariant_violated() and passCount < MAX_INVARIANT_PASSES:
    apply_invariants()
    passCount += 1
if any_invariant_violated():
    // Emit all-default directive for this tick:
    for each agent in attackingPool:
        if agent.role == RUNNER: agent.role = HOLD_WIDTH
        if agent.role == WEAK_SIDE and pool is large enough: retain
    // All agents are HOLD_WIDTH or SUPPORT_BALL; no runners.
    // This guarantees the published directive always passes invariants.
```

**Worked example (MAX_RUNNERS invariant):**

DIRECT profile, `MAX_RUNNERS_DIRECT = 3`. §3.3 assigned 4 runners (agents
1, 2, 4, 5; depthOffsets 21.0, 18.5, 25.0, 15.0). Invariant 1 fires:
- `runnerCount = 4 > 3`.
- Demotee: min depthOffset_m = 15.0 (agent 5). Demoted to SUPPORT_BALL.
- Re-check: `runnerCount = 3 ≤ 3`. Invariant satisfied.

---

## 3.12 Assignment Hysteresis

**Purpose:** Prevent role-thrash when an agent oscillates at a boundary
condition (e.g., alternately inside/outside `SUPPORT_RADIUS_M`).

**Binding:** FR-AT-022 / FR-AT-023. Binding to #2 Agent Movement §3.1
assignment-stability pattern.

**Algorithm:**

```
struct HysteresisEntry:
    currentRole     : AttackingRole    // the role this agent is currently locked into
    dwellCounter    : int              // ticks the current role has been stably preferred;
                                       // isStable() fires when dwellCounter >= ATTACK_DWELL_TICKS
    candidateRole   : AttackingRole    // new role being evaluated for transition
    candidateDwell  : int              // consecutive ticks candidateRole has been preferred;
                                       // transition commits when candidateDwell >= ATTACK_DWELL_TICKS

// Note: prevPhase (the team's possession phase from the previous tick) is stored in
// TransitionHoldState (per-team state), not here (per-agent state).

function isStable(entityId, dwellTicks):
    entry = hysteresisState[entityId]
    return entry.dwellCounter >= dwellTicks

function update(entityId, candidateRole):
    entry = hysteresisState[entityId]
    if candidateRole == entry.currentRole:
        entry.dwellCounter = min(entry.dwellCounter + 1, dwellTicks + 1)
        // Cap to avoid overflow; +1 to distinguish "just met" from "long stable".
    else:
        // New candidate preferred for the first time (or switching back):
        if entry.candidateRole != candidateRole:
            entry.candidateRole = candidateRole
            entry.candidateDwell = 1
        else:
            entry.candidateDwell += 1
        if entry.candidateDwell >= dwellTicks:
            // Transition: promote candidate to current role
            entry.currentRole   = candidateRole
            entry.dwellCounter  = 0
            entry.candidateDwell = 0
```

`ATTACK_DWELL_TICKS = 3` `[GT]` — a role/target transition fires only after
the new candidate role has been the preferred assignment for 3 consecutive ticks.
At 10 Hz this is a 300 ms stability window; sufficient to smooth out
boundary oscillations without introducing perceptible lag. Derivation in
Appendix A §A.1 (promoted from `[EST]` to `[GT]` at section-file draft time).

**State persistence:** `hysteresisState` (one `HysteresisEntry` per pool agent)
is authoritative simulation state per #16 §3.2 and is included in the per-tick
determinism digest.

---

## 3.13 Per-Tick Main Loop Pseudocode

**Purpose:** Canonical ordered execution of the full attacking-AI pipeline.
Each step references the subsection that defines it in detail.

```
// ============================================================
// AttackingAI Per-Tick Main Loop — 10 Hz
// Spec #15, §3.13
// ============================================================

function AttackingAITick(teamId, perceptionSnapshot, positioningAIView,
                         styleProfile, teamAttackAngle, currentTick,
                         ref hysteresisState, ref transitionHoldState):

    // Step 1: Read inputs at tick start.
    //   - Perception snapshot (#7 §3.7): agent positions, ball position,
    //     ball carrier EntityId.
    //   - #12 formationSlot[], lineMembership[], lateralPct[], laneAssignment[],
    //     phase enum.
    //   - teamAttackAngle: match-half constant (0.0 or π rad).
    phase    = PositioningAI.GetPhase(teamId)                    // §3.1
    prevPhase = transitionHoldState.prevPhase                   // per-team; retained from last tick

    // Step 2: Phase gate (§3.1 — pure gate; dispatches to §3.9 for non-IN_POSSESSION).
    if phase == OUT_OF_POSSESSION:
        transitionHoldState.prevPhase = phase
        return emptyDirective

    if phase == TRANSITION or (prevPhase == IN_POSSESSION and phase != IN_POSSESSION):
        directive = TransitionController(prevPhase, phase, ref transitionHoldState) // §3.9
        transitionHoldState.prevPhase = phase
        return directive

    // phase == IN_POSSESSION.
    transitionHoldState.transitionHoldTick = 0      // reset countdown (§3.1)

    // Step 3: Build attacking pool (§3.2).
    //   All agents on thisTeam, excluding GK (PlayerRole.Goalkeeper) and ball carrier.
    //   Sorted EntityId-ascending.
    attackingPool = BuildAttackingPool(perceptionSnapshot, teamId)

    if attackingPool.count == 0:
        hysteresisState.prevPhase = phase
        return emptyDirective                       // degenerate case

    // Step 4: Role assignment (§3.3).
    //   For each agent in pool (EntityId-ascending):
    //     - Hysteresis check (§3.12): retain if dwell valid.
    //     - Else: evaluate RUNNER / SUPPORT_BALL / WEAK_SIDE / HOLD_WIDTH priority.
    //     - If RUNNER: generate RunParameters using teamAttackAngle + lateralPct (§3.4).
    runnerCount    = 0
    weakSideCount  = 0
    for each agent in attackingPool:
        if hysteresisState.isStable(agent.entityId, ATTACK_DWELL_TICKS):
            // retain; do not re-evaluate role
            continue
        role = AssignRole(agent, perceptionSnapshot, positioningAIView,
                          styleProfile, runnerCount, weakSideCount)  // §3.3
        if role == RUNNER:
            agent.runParameters = GenerateRunParameters(agent, ballCarrier,
                                                        teamAttackAngle,
                                                        styleProfile, currentTick)  // §3.4
            runnerCount += 1
        if role == WEAK_SIDE:
            weakSideCount += 1
        agent.assignedRole = role
        hysteresisState.update(agent.entityId, role)

    // Step 5: Validate support radius for SUPPORT_BALL candidates (§3.5).
    //   (Implicit in Step 4; §3.5 defines the distance threshold used in §3.3.)

    // Step 6: Enforce width-holding (§3.6).
    //   Promote agent(s) to HOLD_WIDTH if MIN_WIDTH_HOLDERS not met.
    EnforceWidthHolding(attackingPool, perceptionSnapshot, styleProfile)  // §3.6

    // Step 7: Assign WEAK_SIDE agent (§3.7).
    //   (Merged into Step 4 role assignment; §3.7 defines agent-selection logic.)
    //   Post-check: ensure exactly one WEAK_SIDE agent if pool size qualifies.
    EnsureWeakSideAgent(attackingPool, perceptionSnapshot)  // §3.7

    // Step 8: Compute overload flag (§3.8).
    overloadResult = ComputeOverload(attackingPool, perceptionSnapshot)  // §3.8

    // Step 9: Anti-chaos invariant enforcement (§3.11).
    //   Demote / fallback if any invariant violated.
    InvariantEnforcer.Apply(attackingPool, styleProfile, teamAttackAngle)  // §3.11

    // Step 10: Publish AttackDirective + per-agent AttackIntent.
    directive = AttackDirective {
        teamId         = teamId,
        overloadActive = overloadResult.active,
        overloadFlank  = overloadResult.flank,
        transitionHoldTick = transitionHoldState.transitionHoldTick
    }
    for each agent in attackingPool:
        intents[agent.entityId] = AttackIntent {
            role           = agent.assignedRole,
            runParameters  = (agent.assignedRole == RUNNER)
                             ? agent.runParameters : null,
            validThroughTick = currentTick + 1
        }

    transitionHoldState.prevPhase = phase
    return (directive, intents)
```

---

## 3.14 Constants Catalogue Forward Reference

The full constant catalogue with derivations, proposed values, and `[GT]` /
`[EST]` / `[FIXED]` / `[DERIVED]` / `[CROSS]` / `[CROSS-PENDING]` tags
appears in **§6.1**. Appendix A provides derivation evidence for all `[EST]`
tags promoted to `[GT]` at sign-off.

A summary of all constants consumed by §3.1–§3.13 is provided below for
in-section reference. Values are as proposed in `outline-detailed.md` v1.1;
authoritative values are in §6.1.

| Constant | Tag | Proposed Value | Used In |
|---|---|---|---|
| `SUPPORT_RADIUS_M` | `[GT]` | 12.0 m | §3.3, §3.5 |
| `MIN_WIDTH_HOLDERS` | `[GT]` | 2 | §3.6 |
| `MIN_WEAK_SIDE_AGENT_THRESHOLD` | `[GT]` | 4 | §3.3, §3.7 |
| `OVERLOAD_COUNT` | `[GT]` | 3 | §3.8 |
| `OVERLOAD_ZONE_WIDTH_M` | `[GT]` | 20.0 m | §3.8 |
| `TOUCHLINE_HOLD_DIST_M` | `[GT]` | 4.0 m | §3.6 |
| `WEAK_SIDE_FAR_Y_M` | `[GT]` | 8.0 m | §3.7 |
| `WEAK_SIDE_DEPTH_OFFSET_M` | `[GT]` | 5.0 m | §3.7 |
| `MAX_RUNNERS_POSSESSION` | `[GT]` | 1 | §3.3, §3.11 |
| `MAX_RUNNERS_DIRECT` | `[GT]` | 3 | §3.3, §3.11 |
| `MAX_RUNNERS_COUNTER` | `[GT]` | 4 | §3.3, §3.11 |
| `MIN_SUPPORT_AGENTS` | `[GT]` | 1 | §3.11 |
| `OWN_HALF_RUN_BLOCK_M` | `[GT]` | 5.0 m | §3.11 |
| `MAX_INVARIANT_PASSES` | `[GT]` | 3 | §3.11 |
| `ATTACK_DWELL_TICKS` | `[GT]` | 3 ticks | §3.12 |
| `TRANSITION_HOLD_TICKS_POSSESSION` | `[GT]` | 5 ticks | §3.9 |
| `TRANSITION_HOLD_TICKS_DIRECT` | `[GT]` | 5 ticks | §3.9 |
| `TRANSITION_HOLD_TICKS_COUNTER` | `[GT]` | 0 ticks | §3.9 |
| `BASE_RUN_DEPTH_M` | `[GT]` | 15.0 m | §3.4 |
| `LATERAL_SCALE` | `[GT]` | 0.8 | §3.4 |
| `BASE_RUN_TRIGGER_DELAY_TICKS` | `[GT]` | 3 ticks | §3.4 |
| `DEPTH_MULT_POSSESSION` | `[GT]` | 0.8 | §3.4, §3.10 |
| `DEPTH_MULT_DIRECT` | `[GT]` | 1.4 | §3.4, §3.10 |
| `DEPTH_MULT_COUNTER` | `[GT]` | 1.6 | §3.4, §3.10 |
| `TIMING_MULT_POSSESSION` | `[GT]` | 1.2 | §3.4, §3.10 |
| `TIMING_MULT_DIRECT` | `[GT]` | 0.7 | §3.4, §3.10 |
| `TIMING_MULT_COUNTER` | `[GT]` | 0.5 | §3.4, §3.10 |
| `SUPPORT_MULT_POSSESSION` | `[GT]` | 1.3 | §3.5, §3.10 |
| `SUPPORT_MULT_DIRECT` | `[GT]` | 0.8 | §3.5, §3.10 |
| `SUPPORT_MULT_COUNTER` | `[GT]` | 0.5 | §3.5, §3.10 |
| `DANGER_ZONE_MAX_DIST_M` | `[GT]` | 20.0 m | §5 (test plan) |
| `DANGER_ZONE_CORRIDOR_HW_M` | `[GT]` | 10.16 m | §5 (test plan); derivation Appendix A |
| `FINAL_THIRD_X_M` | `[DERIVED]` | 70.0 m (= `PITCH_LENGTH_M × 2/3`) | §3.4 (worked examples) |
| `PITCH_LENGTH_M` | `[CROSS: #1 §1.2]` | 105.0 m | §3.4, §3.6, §3.8, §3.11 |
| `PITCH_WIDTH_M` | `[CROSS: #1 §1.2]` | 68.0 m | §3.4, §3.6, §3.7, §3.8 |
| `HALF_LINE_X` | `[CROSS: #1 §1.2]` | 52.5 m | §3.11 |
| `DIRECT_RUN_COUNT_DELTA` | `[GT]` | 15 | §5.8 (tactical-identity acceptance) |
| `DOMAIN_TAG_ATTACKING_AI` | `[CROSS: #16 §3.4]` | `0x1B` | §3.13, §4.6 |

**Full derivations and confidence-interval evidence:** Appendix A.

---

## 3.15 Version History

| Version | Date | Author | Changes |
|---|---|---|---|
| 0.1 | May 17, 2026 | Lead developer | Initial draft from `outline-detailed.md` v1.1. All subsections §3.1–§3.14 authored. All formulas include units, valid input ranges, and worked examples per FR-AT-028 / CLAUDE.md. |
| 0.3 | May 18, 2026 | AI agent (claude-sonnet-4-6) | ERR-015-006 fix: promoted `[CROSS-PENDING]` in §3.14 constant reference table (`DOMAIN_TAG_ATTACKING_AI` row) to `[CROSS: #16 §3.4]`. Resolves A-03 FAIL from stress-test Tier A run 1. |
| 0.2 | May 18, 2026 | AI agent (claude-sonnet-4-6) | Adversarial-review fixes: (1) §3.3 WEAK_SIDE condition corrected — was `weakSideCount < MIN_WEAK_SIDE_AGENT_THRESHOLD` (allows up to 4 WEAK_SIDE agents), now `attackingPool.count >= MIN_WEAK_SIDE_AGENT_THRESHOLD and weakSideCount == 0` (pool-size gate, one agent); (2) §3.8 removed `overloadFlank = NONE` (NONE not defined); (3) §3.12 HysteresisEntry struct now includes `candidateRole` and `candidateDwell` fields that the algorithm uses; prevPhase moved to TransitionHoldState (per-team, not per-agent); `[EST]` → `[GT]` for ATTACK_DWELL_TICKS; (4) §3.13 `hysteresisState.prevPhase` → `transitionHoldState.prevPhase` throughout; (5) §3.14 ATTACK_DWELL_TICKS tag corrected `[EST]` → `[GT]`; (6) §3.5 max(5.0) floor description clarified. |
