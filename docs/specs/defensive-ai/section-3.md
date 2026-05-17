# Defensive AI Specification #14 — Section 3: Core Formulas and Algorithms

**Created:** May 17, 2026
**Last Updated:** May 17, 2026 (v0.1 — Initial draft from `outline-detailed.md` v1.0)
**Version:** 0.1
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.0

---

This section publishes the per-tick computation pipeline for Defensive AI #14.
Every formula carries units, valid input ranges, and at least one worked example
(FR-DA-034 / CLAUDE.md "When Writing or Editing Specs").

The per-tick pseudocode is in §3.13; §3.1–§3.12 define each algorithm step.

**Spec-level stage binding:** This spec is authored at Stage 0; runtime activation
is Stage 1. All algorithms in §3 are normative specification text. No runtime code
is produced at Stage 0.

---

## 3.1 Phase Gating (binding to #12 §3.0 per KD-19)

### 3.1.1 Purpose

At the top of every 10 Hz tick, #14 reads the per-team phase enum produced
by Positioning AI #12 §3.0. If the team holds possession, defensive
assignment is irrelevant and #14 emits an all-ZONAL no-override directive
immediately, releasing the remaining computation budget. Only `OUT_OF_POSSESSION`
or `TRANSITION` phase causes the full assignment pipeline to execute.

This gate is normative per FR-DA-013 and KD-19.

### 3.1.2 Algorithm

```
// Inputs
phase = PositioningAI.GetPhase(team)   // #12 §3.0 accessor (Stage 1+ binding)

// Gate check
if phase == Phase.IN_POSSESSION:
    EmitAllZonal(team, assignments, ref directive)
    // MarkDirective: all MarkAssignment.mode = ZONAL
    //                targetPosition = each agent's #12 formationSlot
    //                offsideTrapActive = false
    //                emergencyFlag = false
    return

// Proceed to §3.2 — team is OUT_OF_POSSESSION or TRANSITION
```

`EmitAllZonal` sets every agent's `MarkAssignment.mode = ZONAL` with
`targetPosition = agent.formationSlot` from the #12 baseline. No overrides
are applied. The directive is published immediately (§3.13 Step 2).

### 3.1.3 Valid Input Ranges

- `phase`: enum `{IN_POSSESSION, OUT_OF_POSSESSION, TRANSITION_TO_ATTACK, TRANSITION_TO_DEFEND}` per #12 §3.0.2.
- Any unrecognised phase value falls back to `OUT_OF_POSSESSION` (conservative — activates
  the defensive assignment pipeline rather than suppressing it). This mirrors the
  FR-PA-047 pattern in #12 §2.4 F6.

### 3.1.4 Worked Example

Team defending x = 0 goal. #12 reports phase = `IN_POSSESSION` for this team.
Result: `EmitAllZonal` fires. All 11 agents receive `MarkAssignment.mode = ZONAL`.
Directive `offsideTrapActive = false`, `emergencyFlag = false`. Function returns.
Total computation: O(1) plus one O(N) memset for ZONAL fill. No further §3.2–§3.12
steps execute for this team this tick.

---

## 3.2 HOLD_SHAPE Pool Filtering (binding to #13 KD-4)

### 3.2.1 Purpose

The HOLD_SHAPE pool is the exclusive input set for #14's mark assignment
algorithm. Its membership is derived each tick from #13's `PressAssignment`
role partition (FR-PR-014). #14 MUST NOT assign marks to agents with an active
press role (FR-DA-010), and MUST NOT assign marks to the GK (FR-DA-009).

### 3.2.2 Algorithm

```
// Inputs
//   perception: PerceptionSnapshot (#7 §3.7)
//   pressDir:   PressDirective from #13 this tick

holdShapePool = new List<AgentId>(capacity: 11)

for each agent in perception.teamAgents(team) sorted by EntityId ascending:
    // Exclusion rule 1: GK (FR-DA-009, KD-7)
    if agent.role == GK:
        continue

    // Exclusion rule 2: assigned to PRIMARY_PRESS or COVER_SHADOW by #13 (FR-DA-010, KD-4)
    pressRole = pressDir.GetRole(agent.entityId)   // #13 accessor
    if pressRole == Role.PRIMARY_PRESS OR pressRole == Role.COVER_SHADOW:
        continue

    holdShapePool.Add(agent.entityId)

// Pool is already EntityId-ascending due to the sort above (FR-DA-003)
```

**GK exclusion (FR-DA-009):** The GK is excluded unconditionally, even if the
GK's x-position is further forward than outfield defenders (e.g., during an
attacking corner). #11 Goalkeeper Mechanics owns all GK positioning decisions.

**Press-role exclusion (FR-DA-010):** Agents with `PRIMARY_PRESS` or
`COVER_SHADOW` roles this tick are managed entirely by #13; their target
positions are already set. Including them in #14's pool would create conflicting
instructions for the same agent.

**EntityId sort (FR-DA-003):** The iteration is EntityId-ascending per #16 §3.2.5
to ensure deterministic evaluation order across all machines and replay sessions.

### 3.2.3 Valid Input Ranges

- Team agent count: 1–11 (11 in normal play; may be fewer after red cards).
- Minimum pool size: 0 (edge case — all outfield players in press roles; F4 fallback
  emits all-ZONAL in this event per §3.10 post-loop check).
- Maximum pool size: 10 (11 agents minus GK).

### 3.2.4 Worked Example

11-agent team: GK (1 agent, excluded by FR-DA-009) + 1 PRIMARY_PRESS agent
(excluded by FR-DA-010) + 2 COVER_SHADOW agents (excluded by FR-DA-010) =
**7 agents in HOLD_SHAPE pool**.

Sorted EntityId list (ascending): `[102, 105, 107, 109, 111, 113, 117]`.
These 7 entity IDs are the input to §3.3.

---

## 3.3 Mark-Mode Assignment Algorithm

### 3.3.1 Purpose

For each agent in the HOLD_SHAPE pool, compute the appropriate `MarkAssignment`:
one of `ZONAL`, `MAN_MARK`, or `INTERCEPT_RUNNER`. Emergency and GK-coverage
overrides (`COVER_GK_ZONE`) are applied in §3.8 and §3.9 before this loop runs
(§3.13 Steps 4–4a set those overrides first; §3.3 skips already-overridden agents).

### 3.3.2 Mode Priority

When an opponent qualifies as a candidate for more than one mode:

```
INTERCEPT_RUNNER  >  MAN_MARK  >  ZONAL
```

Higher-urgency modes are preferred. A running attacker threatening the own half
takes priority over static man-marking.

### 3.3.3 Algorithm

```
for each agent in holdShapePool (EntityId-ascending):
    // Skip agents already assigned by §3.8 (last-man) or §3.9 (GK cover)
    if assignments[agent].overriddenThisTick:
        continue

    // Step 1: Hysteresis check (§3.11)
    if hysteresis[agent].dwellCounter > 0:
        hysteresis[agent].dwellCounter -= 1
        // Retain current assignment; do not re-evaluate
        continue

    // Step 2: Collect MAN_MARK candidates
    //   Opponents within MAN_MARK_CANDIDATE_RADIUS_M [GT] AND visible per #7
    manMarkCandidates = []
    for each opponent in perception.opponents:
        d = distance(agent.position, opponent.position)
        if d <= MAN_MARK_CANDIDATE_RADIUS_M AND perception.isVisible(opponent.entityId):
            manMarkCandidates.Add(opponent)

    // Step 3: Collect INTERCEPT_RUNNER candidates
    //   Opponents with velocity magnitude > RUNNER_VELOCITY_THRESHOLD_M_S [GT]
    //   AND velocity direction points toward own half (positive component along
    //   the own-goal direction)
    interceptCandidates = []
    for each opponent in perception.opponents:
        speed = magnitude(opponent.velocity)
        if speed > RUNNER_VELOCITY_THRESHOLD_M_S:
            // Normalise: for team defending x=0, own-goal direction = -X
            //            for team defending x=105, own-goal direction = +X
            // Use dot product of velocity with own-goal unit vector
            ownGoalDir = (team.defendsX0) ? Vector2(-1, 0) : Vector2(+1, 0)
            dot = dot(normalize(opponent.velocity.xy), ownGoalDir)
            if dot > 0:
                // Opponent running toward own half
                interceptCandidates.Add(opponent)

    // Step 4: Score candidates and resolve best assignment
    bestMode = ZONAL
    bestTarget = null
    bestScore = -1.0f

    // INTERCEPT_RUNNER candidates (higher priority)
    for each opp in interceptCandidates:
        s = ThreatScore(opp)   // §3.5
        if s > bestScore:
            bestScore = s
            bestMode = INTERCEPT_RUNNER
            bestTarget = opp.entityId

    // MAN_MARK candidates (lower priority; override only if no INTERCEPT_RUNNER found)
    if bestMode == ZONAL:
        for each opp in manMarkCandidates:
            s = ThreatScore(opp)   // §3.5
            if s > bestScore:
                bestScore = s
                bestMode = MAN_MARK
                bestTarget = opp.entityId

    // Step 5: Tie-breaking within same mode
    //   If two candidates share the highest threat score (to float tolerance),
    //   prefer the one with lower displacement cost from this agent (§3.4).
    //   Terminal tie-break: lower EntityId (FR-DA-014).

    // Step 6: If no candidate found
    if bestMode == ZONAL:
        assignments[agent] = MarkAssignment {
            mode = ZONAL,
            targetEntityId = null,
            targetPosition = shape.GetFormationSlot(agent.entityId)  // #12 baseline
        }
        continue

    // Step 7: Apply hysteresis gate (§3.11) before committing
    ApplyHysteresisGate(agent, MarkAssignment { mode = bestMode, targetEntityId = bestTarget },
                         hysteresis)
```

### 3.3.4 Anti-Chaos Post-Pass

After all agent assignments are computed in this loop, §3.10 anti-chaos invariants
are applied before publication (FR-DA-024). The mode-selection loop above does not
self-enforce §3.10 — the enforcement pass is a separate post-processing step.

---

## 3.4 Displacement Cost Function

### 3.4.1 Formula

```
cost(agent, targetPos) = (agent.position.x − targetPos.x)²
                       + (agent.position.y − targetPos.y)²
```

**Units:** m² (squared metres).

**Purpose:** Used for tie-breaking when two mark candidates produce equal threat
scores (§3.5). Lower cost means the agent is already closer to the required
position and can cover it with less displacement. This minimises shape disruption.

**Terminal tie-break:** If two candidates produce equal cost (within floating-point
comparison precision), the candidate with lower EntityId is preferred (FR-DA-014,
FR-DA-033).

### 3.4.2 Valid Input Ranges

- `agent.position`: `[0, 105] × [0, 68]` (pitch bounds, X × Y in metres).
- `targetPos`: same bounds.
- Output: `[0.0, 105² + 68²]` = `[0.0, 15649.0]` m² (diagonal worst case).
- The cost function is never negative; it is zero only when agent is already
  at the target position.

### 3.4.3 Worked Example

- Agent at position (30.0, 25.0) m.
- Target position (40.0, 30.0) m.
- `cost = (30.0 − 40.0)² + (25.0 − 30.0)²`
- `cost = (−10.0)² + (−5.0)²`
- `cost = 100.0 + 25.0 = 125.0 m²`

Second candidate at (35.0, 28.0):
- `cost = (30.0 − 35.0)² + (25.0 − 28.0)² = 25.0 + 9.0 = 34.0 m²`

Lower cost (34.0 < 125.0) → second candidate preferred when threat scores are equal.

---

## 3.5 Threat Score

### 3.5.1 Formula

```
threat(opponent) = perceivedGoalProximity(opponent)
                 × opponentReceivingAttribute(opponent)
```

**Output range:** `[0.0, 1.0]` (product of two normalised scalars).

**Units:** dimensionless.

### 3.5.2 Component Definitions

**`perceivedGoalProximity(opp)`**

```
// For team defending x=0: ownGoalCenter = (0.0, 34.0)
// For team defending x=105: ownGoalCenter = (105.0, 34.0)

ownGoalCenter = (team.defendsX0) ? Vector2(0.0f, 34.0f) : Vector2(105.0f, 34.0f)

// Goal-proximity along x-axis only (y-component ignored; full Euclidean
// distance is a Stage 2+ refinement — see §7.1)
xDist = |opp.position.x − ownGoalCenter.x|

perceivedGoalProximity = 1.0f − (xDist / PITCH_LENGTH_M)
perceivedGoalProximity = Clamp(perceivedGoalProximity, 0.0f, 1.0f)
```

`PITCH_LENGTH_M = 105.0 m` `[CROSS: #1 §1.2]`

*Why x-axis only:* Using only the longitudinal distance prevents penalising wide
attackers who are laterally offset from goal but nonetheless dangerous. A winger
at (20.0, 5.0) is as threatening goal-proximity-wise as a central striker at
(20.0, 34.0). Full 2D Euclidean proximity is a Stage 2+ enhancement (§7.1).

**`opponentReceivingAttribute(opp)`**

```
// #7 perception snapshot returns the perceived FirstTouch attribute
// as an integer in [1, 20] per Master Volumes / Perception System #7 §8.6.
// Normalise to [0.0, 1.0].

perceivedFirstTouch = perception.GetPerceivedAttribute(opp.entityId,
                          AttributeId.FIRST_TOUCH)   // #7 §3.7–§3.10

opponentReceivingAttribute = (perceivedFirstTouch − 1) / 19.0f
// Range derivation: minimum [1-1]/19 = 0.0; maximum [20-1]/19 = 1.0
```

**Attribute range:** integer `[1, 20]` per Master Volumes and #7 §8.6.
**Normalisation:** `(attr − 1) / 19.0` maps `[1, 20] → [0.0, 1.0]` linearly.

*Cite-not-redefine #7 (KD-1, FR-DA-017):* All attribute reads go through
the #7 perception snapshot (`perception.GetPerceivedAttribute`). #14 does not
access raw player attribute tables directly. The perception system applies
its own uncertainty and visibility filters per #7 §3.7–§3.10 before returning
the value.

### 3.5.3 Valid Input Ranges

- `opp.position.x`: `[0.0, 105.0]` m.
- `opp.position.y`: `[0.0, 68.0]` m.
- `perceivedFirstTouch` attribute: integer `[1, 20]`.
- Output `threat`: `[0.0, 1.0]`.

Edge case — attribute = 1 (minimum): `opponentReceivingAttribute = 0.0`.
Even a very well-positioned opponent with terrible first touch scores 0.0
threat from the attribute factor.

Edge case — opponent at ownGoalCenter (xDist = 0): `perceivedGoalProximity = 1.0`.
This is the maximum threat position; any opponent quality > 1 produces non-zero
threat.

### 3.5.4 Worked Example

Team defending x = 0. Opponent at position (22.0, 34.0) m. Perceived FirstTouch = 16.

**Step 1 — perceivedGoalProximity:**
```
ownGoalCenter = (0.0, 34.0)
xDist = |22.0 − 0.0| = 22.0 m
perceivedGoalProximity = 1.0 − (22.0 / 105.0)
                       = 1.0 − 0.2095
                       = 0.7905
Clamped: 0.7905 (within [0, 1])
```

**Step 2 — opponentReceivingAttribute:**
```
opponentReceivingAttribute = (16 − 1) / 19.0 = 15 / 19.0 = 0.7895
```

**Step 3 — threat:**
```
threat = 0.7905 × 0.7895 = 0.624  (to 3 d.p.)
```

---

## 3.6 Tackle Intent Evaluation (KD-6)

### 3.6.1 Purpose

For each HOLD_SHAPE agent whose assigned opponent is within `TACKLE_ELIGIBLE_RADIUS_M`,
#14 produces a `TackleIntentRequest` carrying the recommended mode:
`COMMIT` (lunge), `JOCKEY` (shadow without committing), or `HOLD` (maintain
shape). This is a *per-tick, per-agent* intent declaration — #8 Decision Tree
consumes it and translates the intent into an `AgentAction` dispatched to
#3 Collision System (KD-6 boundary).

Intent is per-tick only; there is no carry-forward. Agents not within
`TACKLE_ELIGIBLE_RADIUS_M` do not receive a `TackleIntentRequest` this tick.

### 3.6.2 Algorithm

```
// For each agent a in holdShapePool (EntityId-ascending):
assignedOppId = assignments[a].targetEntityId
if assignedOppId == null:
    continue   // ZONAL — no specific opponent; no tackle intent

opp = perception.GetAgent(assignedOppId)
dist = distance(a.position, opp.position)

if dist > TACKLE_ELIGIBLE_RADIUS_M:
    continue   // Out of tackle range; no TackleIntentRequest this tick

// Compute approach angle
agentToOpp = opp.position - a.position
agentVel   = a.velocity

if magnitude(agentVel) < 0.001f:
    // Agent nearly stationary: worst-case approach angle
    approachAngle = π/2.0f   // 1.5708 rad (~90°)
else:
    approachAngle = acos(Clamp(
        dot(normalize(agentToOpp), normalize(agentVel.xy)),
        -1.0f, 1.0f))

// Compute coverage depth: teammates between agent and own goal
coverageDepth = 0
for each teammate t in holdShapePool (excluding a):
    // "Between agent and own goal" means t.x < a.x (for team defending x=0)
    // Team-agnostic: use distToOwnGoal(t) < distToOwnGoal(a)
    if |t.position.y − a.position.y| < COVERAGE_DEPTH_CORRIDOR_M
       AND distToOwnGoal(t) < distToOwnGoal(a):
        coverageDepth += 1

// Mode selection
if coverageDepth >= TACKLE_COMMIT_COVERAGE_FLOOR:
    mode = COMMIT        // adequate cover behind; risk of committing acceptable
elif approachAngle < TACKLE_JOCKEY_ANGLE_RAD:
    mode = JOCKEY        // favourable angle but insufficient cover; shadow without committing
else:
    mode = HOLD          // poor angle AND insufficient cover; hold shape

// Emit TackleIntentRequest
tackleRequests[tackleSite++] = TackleIntentRequest {
    agentId       = a.entityId,
    targetId      = assignedOppId,
    mode          = mode,
    approachAngle = approachAngle,
    coverageDepth = (byte)coverageDepth
}
```

**`distToOwnGoal` normalisation (KD-12):**
```
distToOwnGoal(agent) = |agent.position.x − ownGoalLine.x|
// ownGoalLine.x = 0.0 for team defending x=0
// ownGoalLine.x = 105.0 for team defending x=105
// This scalar is team-agnostic and shared across §3.6, §3.8, §3.9.
```

**Last-man special case (binding §3.8):** When `IsLastManThreat` is active and
`a == lastManAgent`, the tackle mode selection is overridden: `coverageDepth`
is effectively 0 for the last-man evaluation, forcing the decision to either
`JOCKEY` or `HOLD` — never `COMMIT` when truly last man with no cover. This
prevents the "last man sent off" scenario. The override is applied in §3.8 at
Step 3, before the §3.6 loop runs for that agent.

### 3.6.3 Valid Input Ranges

- `dist`: `[0.0, TACKLE_ELIGIBLE_RADIUS_M]` m (eligibility enforced above).
- `approachAngle`: `[0.0, π]` rad.
- `coverageDepth`: integer `[0, 10]` (at most 10 outfield teammates possible).
- `TACKLE_ELIGIBLE_RADIUS_M = 3.0 m` `[GT]`.
- `TACKLE_COMMIT_COVERAGE_FLOOR = 1` `[GT]` (at least one teammate behind).
- `TACKLE_JOCKEY_ANGLE_RAD = 0.35 rad (~20°)` `[GT]`.
- `COVERAGE_DEPTH_CORRIDOR_M = 5.0 m` `[GT]` (half-width of y-corridor).

### 3.6.4 Worked Example

Agent A at (18.0, 34.0) m, velocity (1.8, 0.0) m/s.
Assigned opponent O at (20.5, 34.0) m, velocity irrelevant for this calc.

**Eligibility check:**
```
dist = |20.5 − 18.0| = 2.5 m < TACKLE_ELIGIBLE_RADIUS_M (3.0) → eligible
```

**Approach angle:**
```
agentToOpp = (20.5 − 18.0, 34.0 − 34.0) = (2.5, 0.0)
normalize(agentToOpp) = (1.0, 0.0)

agentVel = (1.8, 0.0), magnitude = 1.8 > 0.001
normalize(agentVel) = (1.0, 0.0)

dot = dot((1.0,0.0), (1.0,0.0)) = 1.0
approachAngle = acos(1.0) = 0.0 rad
```

**Coverage depth (team defending x = 0):**
Suppose teammates T1 at (12.0, 32.0) and T2 at (15.0, 37.0).
- T1: |32.0 − 34.0| = 2.0 < 5.0 (corridor ✓); distToOwnGoal(T1) = 12.0 < 18.0 (✓) → counted.
- T2: |37.0 − 34.0| = 3.0 < 5.0 (corridor ✓); distToOwnGoal(T2) = 15.0 < 18.0 (✓) → counted.
- `coverageDepth = 2`.

**Mode selection:**
```
coverageDepth (2) >= TACKLE_COMMIT_COVERAGE_FLOOR (1) → mode = COMMIT
```

Emitted: `TackleIntentRequest { agentId = A, targetId = O, mode = COMMIT,
           approachAngle = 0.0 rad, coverageDepth = 2 }`.

---

## 3.7 Offside Trap Algorithm (KD-9)

### 3.7.1 Purpose

#14 owns the decision to step up the defensive line (offside trap execution).
#14 does NOT adjudicate whether any attacker is offside — that is a future
referee / rules spec concern (FR-DA-020, KD-9). #14 merely places defenders
at the target depth; the rules system reads agent positions independently.

### 3.7.2 Trigger Conditions

All four conditions must hold simultaneously for `OFFSIDE_TRAP_DWELL_TICKS [GT]`
consecutive ticks. If any condition fails on any tick, `offsideState.stepUpDwellCounter`
resets to 0.

| # | Condition | Rationale |
|---|-----------|-----------|
| 1 | `ball.velocity.magnitude < OFFSIDE_BALL_SPEED_THRESHOLD_M_S` | Ball is slow or stopped. A fast-moving ball (e.g., through-pass in progress) makes stepping the line very dangerous. |
| 2 | `ball.position.x > HALF_LINE_X` (for team defending x=0) | Ball is in opponent half. Trapping from own half creates excessive exposure. |
| 3 | `max(defLine.x) − min(defLine.x) < LINE_COHERENCE_THRESHOLD_M` | DEFENSE-line agents are x-compact enough to step as a unit. A broken line creates uneven exposure. |
| 4 | No `PRIMARY_PRESS` role active for this team this tick | Stepping the line behind an active press creates dangerous space between the pressing and defensive lines. |

`HALF_LINE_X = 52.5 m` `[CROSS: #1 §1.2]`

**Team-agnostic form for condition 2:** `distToOwnGoal(ball) > HALF_LINE_X`.
For team defending x = 105: `ball.position.x < HALF_LINE_X`.

### 3.7.3 Dwell Counter Update

```
// Evaluate all four conditions this tick
cond1 = ball.velocity.magnitude < OFFSIDE_BALL_SPEED_THRESHOLD_M_S
cond2 = distToOwnGoal(ball) > HALF_LINE_X          // ball in opponent half
cond3 = (max(defLine.x) − min(defLine.x)) < LINE_COHERENCE_THRESHOLD_M
cond4 = (pressDir.primaryPressAgent == null)        // no active PRIMARY_PRESS

if cond1 AND cond2 AND cond3 AND cond4:
    offsideState.stepUpDwellCounter += 1
else:
    offsideState.stepUpDwellCounter = 0   // any failure resets

// Decrement cooldown if active
if offsideState.cooldownTicksRemaining > 0:
    offsideState.cooldownTicksRemaining -= 1
```

### 3.7.4 Execution

Fires when `stepUpDwellCounter >= OFFSIDE_TRAP_DWELL_TICKS`
AND `cooldownTicksRemaining == 0`:

```
// Compute step target depth
offsideStepDepth = currentLineDepth + OFFSIDE_STEP_SIZE_M
offsideStepDepth = max(offsideStepDepth, shape.DefensiveLineDepth)   // never step below #12 target
offsideStepDepth = min(offsideStepDepth, OFFSIDE_MAX_DEPTH_M)        // safety ceiling

// For team defending x=0: x increases = forward into opponent half
// For team defending x=105: step is mirrored (decrease x)
// currentLineDepth is always expressed as distToOwnGoal; convert back:
// targetX = (team.defendsX0) ? offsideStepDepth : (105.0 − offsideStepDepth)

// Issue ZONAL assignment to all DEFENSE-line agents simultaneously (FR-DA-019)
for each agent a with LineMembership == DEFENSE in holdShapePool:
    assignments[a] = MarkAssignment {
        mode           = ZONAL,
        targetPosition = Vector2(targetX, shape.GetFormationSlot(a).y),
        targetEntityId = null
    }

// Update directive and state
directive.offsideTrapActive   = true
directive.stepUpTargetDepth   = offsideStepDepth

offsideState.cooldownTicksRemaining = OFFSIDE_RESET_COOLDOWN_TICKS
offsideState.stepUpDwellCounter     = 0
```

**Simultaneous step (FR-DA-019):** All DEFENSE-line agents receive the same
`targetPosition.x = targetX` on the same tick. Staggered stepping (some agents
advancing while others wait) is out of scope for Stage 1 and deferred to §7.2.

### 3.7.5 Constant Catalogue Forward References

| Constant | Tag | Proposed Value | Purpose |
|----------|-----|----------------|---------|
| `OFFSIDE_BALL_SPEED_THRESHOLD_M_S` | `[GT]` | 4.0 m/s | Ball speed ceiling for trigger condition 1 |
| `OFFSIDE_TRAP_DWELL_TICKS` | `[GT]` | 3 ticks | Consecutive tick count before trap fires |
| `OFFSIDE_STEP_SIZE_M` | `[GT]` | 3.0 m | Forward advancement per trap execution |
| `OFFSIDE_MAX_DEPTH_M` | `[GT]` | 45.0 m | Maximum distToOwnGoal the line may advance |
| `OFFSIDE_RESET_COOLDOWN_TICKS` | `[GT]` | 10 ticks | Cooldown after trap fires |
| `LINE_COHERENCE_THRESHOLD_M` | `[GT]` | 8.0 m | Max x-spread of DEFENSE-line for eligibility |
| `HALF_LINE_X` | `[CROSS: #1 §1.2]` | 52.5 m | Midfield line x-coordinate |

All values are finalised in §6.1 with derivations in `appendices.md` Appendix A.

### 3.7.6 Valid Input Ranges

- `ball.velocity.magnitude`: `[0.0, ∞)` m/s; trigger fires only below threshold.
- `currentLineDepth`: `[0.0, 52.5]` m (distToOwnGoal of DEFENSE line; cannot exceed half-line).
- `offsideStepDepth`: `(currentLineDepth, OFFSIDE_MAX_DEPTH_M]` m.

### 3.7.7 Worked Example — Trap Fires

Situation: team defending x = 0. Current DEFENSE-line distToOwnGoal = 38.0 m.
`shape.DefensiveLineDepth = 40.0 m`. `OFFSIDE_MAX_DEPTH_M = 45.0 m`.

**Tick T:** ball speed = 1.2 m/s < 4.0 (cond 1 ✓); ball.x = 60.0 > 52.5 (cond 2 ✓);
DEFENSE-line x-spread = 2.1 m < 8.0 (cond 3 ✓); no PRIMARY_PRESS active (cond 4 ✓).
`stepUpDwellCounter = 1`.

**Tick T+1:** same conditions hold. `stepUpDwellCounter = 2`.

**Tick T+2:** same conditions hold. `stepUpDwellCounter = 3 = OFFSIDE_TRAP_DWELL_TICKS`.
Cooldown = 0 → trap fires.

**Execution:**
```
offsideStepDepth = 38.0 + 3.0 = 41.0
offsideStepDepth = max(41.0, 40.0) = 41.0  (above DefensiveLineDepth)
offsideStepDepth = min(41.0, 45.0) = 41.0  (below safety ceiling)
targetX = 41.0  (for team defending x=0, distToOwnGoal = x-coordinate)
```

All four DEFENSE-line agents receive `MarkAssignment { mode = ZONAL, targetPosition.x = 41.0 }`.
Their y-components retain individual formationSlot y values from #12.
`cooldownTicksRemaining = 10`. `stepUpDwellCounter = 0`.

**Tick T+3:** cooldown = 9. Even if all conditions hold, trap cannot re-fire until
cooldown reaches 0 (tick T+12 or later).

### 3.7.8 Worked Example — Trap Blocked (ball too fast)

**Tick T:** ball speed = 6.5 m/s > 4.0 (cond 1 FAILS). `stepUpDwellCounter = 0`.

Even if conditions 2–4 hold, the dwell counter cannot increment. Trap does not fire.

---

## 3.8 Last-Man Predicate (KD-12)

### 3.8.1 Definitions

```
// Normalised distance scalar (team-agnostic)
distToOwnGoal(a) = |a.position.x − ownGoalLine.x|
// ownGoalLine.x = 0.0 for team defending x=0
// ownGoalLine.x = 105.0 for team defending x=105

// Last-man candidate: the HOLD_SHAPE-pool agent closest to own goal
IsLastManCandidate(a) :=
    a ∈ holdShapePool            // GK already excluded (FR-DA-009)
    AND distToOwnGoal(a) ==
        min{ distToOwnGoal(b) : b ∈ holdShapePool }
    // Tie-break: lowest EntityId wins (FR-DA-033)

// Last-man threat: ball is ahead of (closer to own goal than) last man + buffer
IsLastManThreat(ballPos, lastManAgent) :=
    distToOwnGoal(ballPos) < distToOwnGoal(lastManAgent) + LAST_MAN_BALL_BUFFER_M
    // ball is ahead of last man (closer to own goal than lastMan + buffer)
    AND distToOwnGoal(ballPos) > LAST_MAN_OWN_HALF_MIN_X
    // ball is not trivially in own third (prevents constant emergency triggering)
```

**GK exclusion note:** Even if the GK ventures forward (e.g., attacking corner),
GK is excluded from `holdShapePool` entirely (FR-DA-009). The GK is never the
last-man candidate per this algorithm. #11 owns GK positioning decisions.

**Performance:** `min` over ≤ 10 HOLD_SHAPE outfield agents. O(N) per tick, N ≤ 10.

### 3.8.2 When `IsLastManThreat` Fires

```
lastMan = ComputeLastManCandidate(holdShapePool)

if IsLastManThreat(snapshot.ballPosition, lastMan):
    // Step 1: Set emergency flag
    directive.emergencyFlag = true

    // Step 2: Identify highest-threat advancing attacker
    advancingAttacker = argmax over perception.opponents:
        perceivedGoalProximity(opp)   // §3.5 component; using proximity only,
                                      // not the full threat product, because urgency
                                      // is positional — any attacker in that zone is dangerous
        // Tie-break: lowest EntityId (determinism)

    // Step 3: Override lastMan's assignment
    assignments[lastMan] = MarkAssignment {
        mode           = INTERCEPT_RUNNER,
        targetEntityId = advancingAttacker.entityId,
        targetPosition = null
    }
    assignments[lastMan].overriddenThisTick = true
    ResetHysteresis(hysteresis[lastMan])   // Emergency takes effect immediately

    // Step 4 (tackle intent override): last man MUST NOT commit when isolated
    // The §3.6 loop for lastMan will check this flag and force JOCKEY or HOLD.
    // See §3.6.2 "Last-man special case".
```

**Emergency override takes effect BEFORE the regular §3.3 loop** (§3.13 Step 4
precedes Step 5). The regular loop skips agents with `overriddenThisTick = true`.

### 3.8.3 Constant Catalogue Forward References

| Constant | Tag | Proposed Value | Purpose |
|----------|-----|----------------|---------|
| `LAST_MAN_BALL_BUFFER_M` | `[GT]` | 5.0 m | Ball-ahead buffer before emergency fires |
| `LAST_MAN_OWN_HALF_MIN_X` | `[GT]` | 5.0 m | Min distToOwnGoal for ball to trigger emergency |

Values finalised in §6.1.

### 3.8.4 Valid Input Ranges

- `distToOwnGoal(ballPos)`: `[0.0, 105.0]` m.
- `distToOwnGoal(lastManAgent)`: `[0.0, 105.0]` m.
- The predicate is well-defined for all in-pitch ball positions.

### 3.8.5 Worked Example

Team defending x = 0. `ownGoalLine.x = 0.0`.

HOLD_SHAPE pool distToOwnGoal values:
- Agent 102: 18.0 m
- Agent 105: 22.0 m
- Agent 107: 25.0 m (others further)

`lastMan = Agent 102` (minimum = 18.0 m).

Ball position: (12.0, 34.0) m. `distToOwnGoal(ball) = 12.0 m`.

**Predicate check:**
```
12.0 < 18.0 + LAST_MAN_BALL_BUFFER_M (5.0) = 23.0 → true (cond 1)
12.0 > LAST_MAN_OWN_HALF_MIN_X (5.0) → true (cond 2)
IsLastManThreat = true
```

Agent 102 receives emergency override: `INTERCEPT_RUNNER` targeting the
opponent with highest `perceivedGoalProximity` in the perception snapshot.
`directive.emergencyFlag = true`.

**Non-triggering case:** Ball at (30.0, 34.0) m. `distToOwnGoal(ball) = 30.0 m`.
```
30.0 < 18.0 + 5.0 = 23.0 → false (cond 1 fails)
IsLastManThreat = false
```
No emergency. Normal §3.3 loop proceeds for Agent 102.

---

## 3.9 Emergency COVER_GK_ZONE Override (KD-7)

### 3.9.1 Trigger Conditions

Both conditions must hold simultaneously:

1. `IsLastManThreat` is active this tick (`directive.emergencyFlag == true`).
2. GK's perceived position is outside the expected zone:
   `distToOwnGoal(gk.position) > GK_EXPECTED_ZONE_MAX_X`

`GK_EXPECTED_ZONE_MAX_X = 15.0 m` `[GT]` (maximum distance from own goal-line
at which the GK is considered "in position"). Value expressed as distToOwnGoal
scalar — team-agnostic per KD-12.

### 3.9.2 Algorithm

```
gkPos = perception.GetGKPosition(team)   // #7 snapshot read

if directive.emergencyFlag AND distToOwnGoal(gkPos) > GK_EXPECTED_ZONE_MAX_X:
    // GK is out of expected zone; find the best cover agent

    // Abandoned zone center: midpoint of expected GK zone
    // For team defending x=0: zone = [0, GK_EXPECTED_ZONE_MAX_X]; center.x = 7.5
    // For team defending x=105: zone = [105-15, 105]; center.x = 97.5
    abandonedZoneCenter = Vector2(
        (team.defendsX0) ? (GK_EXPECTED_ZONE_MAX_X / 2.0f)
                         : (105.0f − GK_EXPECTED_ZONE_MAX_X / 2.0f),
        PITCH_WIDTH_M / 2.0f   // y-center = 34.0
    )

    // Find agent with minimum displacement cost to abandonedZoneCenter
    // Exclude lastManAgent (already overridden by §3.8)
    coverAgent = argmin over holdShapePool (excluding lastManAgent):
        cost(agent.position, abandonedZoneCenter)   // §3.4 formula

    // Override coverAgent's assignment
    assignments[coverAgent] = MarkAssignment {
        mode           = COVER_GK_ZONE,
        targetPosition = abandonedZoneCenter,
        targetEntityId = null
    }
    assignments[coverAgent].overriddenThisTick = true
    ResetHysteresis(hysteresis[coverAgent])

    // Track duration (prevent persistent override if GK genuinely absent)
    offsideState.coverGkZoneActiveTicks += 1
    if offsideState.coverGkZoneActiveTicks >= COVER_GK_ZONE_MAX_TICKS:
        // Force release: revert to ZONAL (agent cannot hold zone indefinitely)
        assignments[coverAgent] = MarkAssignment {
            mode = ZONAL,
            targetPosition = shape.GetFormationSlot(coverAgent.entityId)
        }
        offsideState.coverGkZoneActiveTicks = 0
else:
    // Override not active or GK returned to zone; reset counter
    offsideState.coverGkZoneActiveTicks = 0
```

`PITCH_WIDTH_M = 68.0 m` `[CROSS: #1 §1.2]`

### 3.9.3 Release Conditions

The COVER_GK_ZONE override is released when either:
- `distToOwnGoal(gk.position) <= GK_EXPECTED_ZONE_MAX_X` (GK returned to zone), OR
- The override has been active for `COVER_GK_ZONE_MAX_TICKS` consecutive ticks (safety
  release — prevents permanent occupation of the zone when the GK is genuinely absent,
  e.g., injury scenario).

After release, the coverAgent reverts to `ZONAL` with its #12 formationSlot as target.

### 3.9.4 Constant Catalogue Forward References

| Constant | Tag | Proposed Value | Purpose |
|----------|-----|----------------|---------|
| `GK_EXPECTED_ZONE_MAX_X` | `[GT]` | 15.0 m | Max distToOwnGoal for GK "in position" |
| `COVER_GK_ZONE_MAX_TICKS` | `[GT]` | 20 ticks | Safety release after 2.0 s of override |

Values finalised in §6.1.

### 3.9.5 Valid Input Ranges

- `distToOwnGoal(gkPos)`: `[0.0, 105.0]` m.
- `abandonedZoneCenter.x`: `[0.0, 52.5]` m (own half; will not exceed half-line).
- `abandonedZoneCenter.y`: `34.0 m` (pitch centre, fixed per formula).
- `coverGkZoneActiveTicks`: `[0, COVER_GK_ZONE_MAX_TICKS]`.

### 3.9.6 Worked Example

`IsLastManThreat` active. GK at position (40.0, 34.0) m, defending x = 0.
`distToOwnGoal(gkPos) = 40.0 m > GK_EXPECTED_ZONE_MAX_X (15.0 m)` → condition 2 holds.

```
abandonedZoneCenter = (15.0 / 2.0, 68.0 / 2.0) = (7.5, 34.0)
```

HOLD_SHAPE pool agents (excluding lastManAgent, e.g., Agent 102):
- Agent 105 at (22.0, 34.0): `cost((22.0,34.0), (7.5,34.0)) = (22-7.5)² + 0² = 210.25`
- Agent 107 at (25.0, 38.0): `cost((25.0,38.0), (7.5,34.0)) = (17.5)² + (4.0)² = 306.25 + 16.0 = 322.25`

Agent 105 has lower cost → `coverAgent = Agent 105`.
Assignment: `MarkAssignment { mode = COVER_GK_ZONE, targetPosition = (7.5, 34.0) }`.

---

## 3.10 Anti-Chaos Invariant Enforcement (KD-17)

### 3.10.1 Purpose

After the full assignment pass (§3.3–§3.9), before publication, three
measurable invariants are enforced. This prevents the assignment algorithm
from producing collectively incoherent states (e.g., most defenders
pulled into man-mark assignments, leaving the backline empty).

Applied BEFORE directive publication (FR-DA-024).

### 3.10.2 The Three Invariants

| # | Invariant | Enforcement |
|---|-----------|-------------|
| 1 | `count(DEFENSE-line agents in ZONAL) >= MIN_BACKLINE_AGENTS` | Demote the most-recently-assigned non-ZONAL DEFENSE-line agent |
| 2 | `count(MAN_MARK assignments) <= MAX_MAN_MARK_ASSIGNMENTS` | Demote the MAN_MARK assignment with lowest threat score |
| 3 | `displacement(agent, targetPos) <= MAX_MARK_DISPLACEMENT_M` for all non-ZONAL | Demote the violating assignment |

`MIN_BACKLINE_AGENTS = 3` `[GT]`
`MAX_MAN_MARK_ASSIGNMENTS = 4` `[GT]`
`MAX_MARK_DISPLACEMENT_M = 20.0 m` `[GT]`

### 3.10.3 Algorithm

```
for pass in 1..3:   // Maximum 3 passes (FR-DA-028)

    // --- Invariant 1: Min backline ---
    defenseLineTotal    = count(a in holdShapePool where lineMembership[a] == DEFENSE)
    defenseLineInZonal  = count(a in holdShapePool where lineMembership[a] == DEFENSE
                                                    AND assignments[a].mode == ZONAL)
    if defenseLineInZonal < MIN_BACKLINE_AGENTS AND defenseLineTotal > MIN_BACKLINE_AGENTS:
        // Find the non-ZONAL DEFENSE-line assignment with lowest threat score (most demotion-safe)
        demoteCandidate = argmin over {a : lineMembership[a] == DEFENSE
                                          AND assignments[a].mode != ZONAL}:
            ThreatScore(perception.GetAgent(assignments[a].targetEntityId))
        assignments[demoteCandidate] = MarkAssignment {
            mode = ZONAL,
            targetPosition = shape.GetFormationSlot(demoteCandidate)
        }
        continue   // Re-check from pass start

    // --- Invariant 2: Max man-mark ---
    manMarkCount = count(a in holdShapePool where assignments[a].mode == MAN_MARK)
    if manMarkCount > MAX_MAN_MARK_ASSIGNMENTS:
        demoteCandidate = argmin over {a : assignments[a].mode == MAN_MARK}:
            ThreatScore(perception.GetAgent(assignments[a].targetEntityId))
        assignments[demoteCandidate] = MarkAssignment {
            mode = ZONAL,
            targetPosition = shape.GetFormationSlot(demoteCandidate)
        }
        continue

    // --- Invariant 3: Max displacement ---
    violation = null
    for each a in holdShapePool:
        if assignments[a].mode != ZONAL AND assignments[a].targetPosition != null:
            displacement = distance(assignments[a].targetPosition,
                                    shape.GetFormationSlot(a))
            if displacement > MAX_MARK_DISPLACEMENT_M:
                violation = a
                break   // Demote first violation found; re-check
    if violation != null:
        assignments[violation] = MarkAssignment {
            mode = ZONAL,
            targetPosition = shape.GetFormationSlot(violation)
        }
        continue

    // All three invariants satisfied: break early
    break

// Post-loop check (FR-DA-032)
// Re-evaluate all three invariants one final time
allSatisfied = (invariant1 AND invariant2 AND invariant3)
if NOT allSatisfied:
    // Hard fallback: all-ZONAL for this tick
    EmitAllZonal(team, assignments, directive)
    log.Warn("DEFENSIVE_AI_INVARIANT_FALLBACK", team, tick)
    return false
return true
```

**Note on invariant 1 demote selection:** "Most demotion-safe" is the DEFENSE-line
non-ZONAL assignment whose target opponent has the lowest threat score — demoting
the least dangerous assignment first preserves the most defensive value.

### 3.10.4 Pass Count Rationale

Three passes are sufficient because each pass demotes at most one assignment and
each demotion can only improve (not worsen) invariants 2 and 3. In extreme cases
(e.g., all 10 HOLD_SHAPE agents MAN_MARK simultaneously), the hard fallback at
the post-loop check catches the residual violation.

### 3.10.5 Worked Example — Invariant 2 Violation

`MAX_MAN_MARK_ASSIGNMENTS = 4`. After §3.3, 5 agents have `MAN_MARK` mode.

**Pass 1 — Invariant 1:** 3 DEFENSE-line agents in ZONAL. Invariant satisfied.

**Pass 1 — Invariant 2:** `manMarkCount = 5 > 4`. Violation.
Compute threat scores for the 5 MAN_MARK assignments:
- Agent 105 → threat 0.62
- Agent 107 → threat 0.55
- Agent 109 → threat 0.71
- Agent 111 → threat 0.48 (lowest)
- Agent 113 → threat 0.59

Demote Agent 111's assignment to ZONAL. `continue`.

**Pass 2 — Invariant 1:** Still satisfied.
**Pass 2 — Invariant 2:** `manMarkCount = 4 = MAX_MAN_MARK_ASSIGNMENTS`. Satisfied.
**Pass 2 — Invariant 3:** All displacements checked; all ≤ 20.0 m. Satisfied.
Break.

Result: 4 MAN_MARK assignments published. Agent 111 reverts to ZONAL
(#12 baseline slot).

---

## 3.11 Assignment Hysteresis (KD-11, binding to #2 §3.1)

### 3.11.1 Purpose

Prevent assignment thrash when two candidates swap relative threat-score or cost
ranking on adjacent ticks (e.g., two opponents crossing paths). The dwell-time
hysteresis pattern is reused from Agent Movement #2 §3.1. #14 parameterises
the #2 pattern — it does not define a new algorithm.

### 3.11.2 State

Per-agent `MarkHysteresisState`:

```
MarkHysteresisState {
    dwellCounter: int,        // ticks to retain current assignment without re-evaluation
    candidateMode: MarkMode,  // candidate being held for dwell
    candidateTargetId: EntityId?,
    holdTicks: int            // consecutive ticks the new candidate has been preferred
}
```

`MarkHysteresisState` is authoritative simulation state (digested per #16 §6.2 / KD-10).

### 3.11.3 Algorithm

```
// On tick T, evaluating agent a (called from §3.3 after hysteresis pre-check):

// --- Pre-check (executed in §3.3 Step 1 before candidate evaluation) ---
if assignments[a].mode != ZONAL AND hysteresis[a].dwellCounter > 0:
    hysteresis[a].dwellCounter -= 1
    // Retain current assignment; skip candidate evaluation
    return

// --- Post-evaluation hysteresis gate (called after §3.3 selects bestCandidate) ---
ApplyHysteresisGate(agent a, MarkAssignment newCandidate, ref MarkHysteresisState h):
    if newCandidate.mode == assignments[a].mode
       AND newCandidate.targetEntityId == assignments[a].targetEntityId:
        // Same assignment — no transition needed; reset candidate tracker
        h.holdTicks = 0
        h.candidateMode = newCandidate.mode
        h.candidateTargetId = newCandidate.targetEntityId
        return  // Keep current assignment

    // New candidate differs from current assignment
    if h.candidateMode == newCandidate.mode
       AND h.candidateTargetId == newCandidate.targetEntityId:
        // Candidate is stable this tick
        h.holdTicks += 1
        if h.holdTicks >= MARK_DWELL_TICKS:
            // Candidate has been consistently preferred; commit transition
            assignments[a] = newCandidate
            h.dwellCounter = MARK_DWELL_TICKS    // lock in for dwell period
            h.holdTicks    = 0
            h.candidateMode     = MarkMode.ZONAL
            h.candidateTargetId = null
    else:
        // New candidate: reset dwell accumulator
        h.candidateMode     = newCandidate.mode
        h.candidateTargetId = newCandidate.targetEntityId
        h.holdTicks         = 1
```

**Emergency overrides bypass hysteresis** (`ResetHysteresis` in §3.8 and §3.9).
Emergency assignments take effect immediately with no dwell requirement.

### 3.11.4 Constant Catalogue Forward Reference

| Constant | Tag | Proposed Value | Purpose |
|----------|-----|----------------|---------|
| `MARK_DWELL_TICKS` | `[GT]` | 4 ticks | Consecutive ticks a new candidate must be preferred before transition commits |

Value finalised in §6.1. At 10 Hz, 4 ticks = 400 ms of consistent preference
before a mark target changes. This is long enough to suppress typical positional
crossings but short enough to respond to genuine assignment changes.

### 3.11.5 Valid Input Ranges

- `dwellCounter`: `[0, MARK_DWELL_TICKS]`.
- `holdTicks`: `[0, MARK_DWELL_TICKS]`.
- Emergency resets: `dwellCounter = 0`, `holdTicks = 0`.

### 3.11.6 Worked Example — Thrash Prevention

Two opponents A (EntityId 201) and B (EntityId 202) cross paths over ticks T…T+5.
Agent 105 evaluates them:

| Tick | Best Candidate (§3.3) | h.candidateTargetId | h.holdTicks | Assignment Published |
|------|----------------------|---------------------|-------------|----------------------|
| T-1  | 201 (current)        | 201                 | 0           | MAN_MARK → 201 (locked, dwellCounter=4) |
| T    | 201 (dwellCounter>0) | —                   | —           | MAN_MARK → 201 (retained by §3.3 pre-check) |
| T+1  | 201 (dwellCounter>0) | —                   | —           | MAN_MARK → 201 (retained) |
| T+2  | 202 (candidates swapped) | 202           | 1           | MAN_MARK → 201 (holdTicks < 4; no commit) |
| T+3  | 201 (swapped back)   | 201                 | 1           | MAN_MARK → 201 (new candidate 201 = current; commit) |
| T+4  | 202                  | 202                 | 1           | MAN_MARK → 201 (holdTicks = 1) |
| T+5  | 202                  | 202                 | 2           | MAN_MARK → 201 (holdTicks = 2) |

Agent 105 stays on opponent 201 until 202 has been consistently preferred for
4 consecutive ticks. Positional noise on ticks T+2–T+3 does not cause a flip.

---

## 3.12 Constants Catalogue Reference

All constants used in §3 are defined in §6.1 with: value, unit, tag, and Appendix A
derivation entry. This subsection provides the forward-reference index only; values
are not duplicated here.

**Tags used in §3:**

| Tag | Meaning | Rule |
|-----|---------|------|
| `[GT]` | Gameplay-Tuned | Designer sets value; lives in `DefensiveAIConstants.cs` |
| `[CROSS]` | Cross-spec constant | Defined in named upstream spec; consumed read-only |
| `[CROSS-PENDING]` | Cross-spec constant pending upstream allocation | Promoted to `[CROSS]` atomically with upstream APPROVED |

**Constants index by subsection:**

| Constant | Tag | §3 First Use |
|----------|-----|--------------|
| `MAN_MARK_CANDIDATE_RADIUS_M` | `[GT]` | §3.3.3 |
| `RUNNER_VELOCITY_THRESHOLD_M_S` | `[GT]` | §3.3.3 |
| `TACKLE_ELIGIBLE_RADIUS_M` | `[GT]` | §3.6.2 |
| `TACKLE_COMMIT_COVERAGE_FLOOR` | `[GT]` | §3.6.2 |
| `TACKLE_JOCKEY_ANGLE_RAD` | `[GT]` | §3.6.2 |
| `COVERAGE_DEPTH_CORRIDOR_M` | `[GT]` | §3.6.2 |
| `OFFSIDE_BALL_SPEED_THRESHOLD_M_S` | `[GT]` | §3.7.2 |
| `OFFSIDE_TRAP_DWELL_TICKS` | `[GT]` | §3.7.2 |
| `OFFSIDE_STEP_SIZE_M` | `[GT]` | §3.7.4 |
| `OFFSIDE_MAX_DEPTH_M` | `[GT]` | §3.7.4 |
| `OFFSIDE_RESET_COOLDOWN_TICKS` | `[GT]` | §3.7.4 |
| `LINE_COHERENCE_THRESHOLD_M` | `[GT]` | §3.7.2 |
| `LAST_MAN_BALL_BUFFER_M` | `[GT]` | §3.8.1 |
| `LAST_MAN_OWN_HALF_MIN_X` | `[GT]` | §3.8.1 |
| `GK_EXPECTED_ZONE_MAX_X` | `[GT]` | §3.9.1 |
| `COVER_GK_ZONE_MAX_TICKS` | `[GT]` | §3.9.2 |
| `MIN_BACKLINE_AGENTS` | `[GT]` | §3.10.2 |
| `MAX_MAN_MARK_ASSIGNMENTS` | `[GT]` | §3.10.2 |
| `MAX_MARK_DISPLACEMENT_M` | `[GT]` | §3.10.2 |
| `MARK_DWELL_TICKS` | `[GT]` | §3.11.4 |
| `PITCH_LENGTH_M` | `[CROSS: #1 §1.2]` | §3.5.2 |
| `PITCH_WIDTH_M` | `[CROSS: #1 §1.2]` | §3.9.2 |
| `HALF_LINE_X` | `[CROSS: #1 §1.2]` | §3.7.2 |
| `DOMAIN_TAG_DEFENSIVE_AI` | `[CROSS-PENDING]` `0x1A` | §3.13 |

---

## 3.13 Per-Tick Main Loop Pseudocode

```csharp
// DefensiveAITick.Execute(
//     TeamId team,
//     PerceptionSnapshot snapshot,           // #7 §3.7 — read-only
//     BaselineDefensiveShapeView shape,      // #12 §4.5.2 — read-only
//     PressDirective pressDir,               // #13 §4.5 — read-only
//     ref MarkDirective directive,           // OUTPUT — per-team
//     Span<MarkAssignment> assignments,      // OUTPUT — per-agent (length == snapshot.teamAgentCount)
//     Span<TackleIntentRequest> tackleRequests,  // OUTPUT — per eligible agent
//     ref int tackleCount,                   // OUTPUT — number of valid TackleIntentRequests
//     ref OffsideLineState offsideState,     // AUTHORITATIVE STATE
//     Span<MarkHysteresisState> hysteresis   // AUTHORITATIVE STATE (per-agent)
// )

// Step 1: Read inputs (snapshot already read at tick start by orchestrator)
phase       = PositioningAI.GetPhase(team)         // #12 §3.0 (Stage 1+)
defLineDepth = shape.DefensiveLineDepth            // #12 §2.2 field (Stage 1+)

// Step 2: Phase gate (§3.1)
if phase == Phase.IN_POSSESSION:
    EmitAllZonal(team, assignments, ref directive)
    return

// Step 3: Build HOLD_SHAPE pool (§3.2)
holdShapePool = BuildHoldShapePool(snapshot, pressDir)  // excludes GK + press roles

// Step 4: Last-man predicate (§3.8) — FIRST; highest priority
lastMan = ComputeLastManCandidate(holdShapePool, snapshot)
if IsLastManThreat(snapshot.ballPosition, lastMan, team):
    directive.emergencyFlag = true
    advAttacker = FindHighestProximityAttacker(snapshot, team)
    assignments[lastMan.poolIndex] = MarkAssignment {
        mode           = MarkMode.INTERCEPT_RUNNER,
        targetEntityId = advAttacker.entityId,
        targetPosition = null
    }
    assignments[lastMan.poolIndex].overriddenThisTick = true
    ResetHysteresis(ref hysteresis[lastMan.poolIndex])

    // Step 4a: GK out-of-position check (§3.9)
    gkPos = snapshot.GetGKPosition(team)
    if DistToOwnGoal(gkPos, team) > GK_EXPECTED_ZONE_MAX_X:
        coverAgent = FindMinCostCoverAgent(holdShapePool, lastMan.poolIndex, snapshot, team)
        if offsideState.coverGkZoneActiveTicks < COVER_GK_ZONE_MAX_TICKS:
            assignments[coverAgent.poolIndex] = MarkAssignment {
                mode           = MarkMode.COVER_GK_ZONE,
                targetPosition = ComputeAbandonedZoneCenter(team),
                targetEntityId = null
            }
            assignments[coverAgent.poolIndex].overriddenThisTick = true
            ResetHysteresis(ref hysteresis[coverAgent.poolIndex])
            offsideState.coverGkZoneActiveTicks += 1
        else:
            // Safety release: revert to ZONAL
            assignments[coverAgent.poolIndex] = MarkAssignment {
                mode = MarkMode.ZONAL,
                targetPosition = shape.GetFormationSlot(coverAgent.entityId)
            }
            offsideState.coverGkZoneActiveTicks = 0
else:
    // Emergency not active; reset cover-GK counter
    directive.emergencyFlag = false
    offsideState.coverGkZoneActiveTicks = 0

// Step 5: Regular assignment loop (§3.3) — EntityId-ascending
for i in 0..holdShapePool.count:
    agent = holdShapePool[i]
    if assignments[i].overriddenThisTick:
        continue  // Already set by Steps 4/4a

    // Hysteresis pre-check
    if assignments[i].mode != MarkMode.ZONAL AND hysteresis[i].dwellCounter > 0:
        hysteresis[i].dwellCounter -= 1
        continue  // Retain current assignment

    // Evaluate candidates (§3.3 Steps 2–4)
    candidates   = EvaluateCandidates(agent, snapshot, team)   // §3.3 Steps 2–3
    bestCandidate = SelectBestCandidate(candidates, agent)      // §3.5 threat + §3.4 cost
    ApplyHysteresisGate(i, bestCandidate, ref hysteresis[i],    // §3.11
                        ref assignments[i], shape)

// Step 6: Tackle intent evaluation (§3.6)
tackleCount = 0
for i in 0..holdShapePool.count:
    agent = holdShapePool[i]
    if assignments[i].targetEntityId == null:
        continue   // ZONAL — no specific opponent
    opp = snapshot.GetAgent(assignments[i].targetEntityId)
    if opp == null:
        continue
    d = Distance(agent.position, opp.position)
    if d <= TACKLE_ELIGIBLE_RADIUS_M:
        tackleRequests[tackleCount++] = EvaluateTackleIntent(
            agent, opp, i, holdShapePool, snapshot, team,
            isLastMan: (i == lastMan.poolIndex AND directive.emergencyFlag))

// Step 7: Offside trap check (§3.7)
UpdateOffsideTrapDwell(snapshot, holdShapePool, pressDir, team, ref offsideState)
if offsideState.stepUpDwellCounter >= OFFSIDE_TRAP_DWELL_TICKS
        AND offsideState.cooldownTicksRemaining == 0:
    ExecuteOffsideTrap(holdShapePool, shape, ref directive, assignments,
                       ref offsideState, team)

// Step 8: Anti-chaos enforcement (§3.10)
enforced = EnforceAntiChaosInvariants(holdShapePool, shape, snapshot,
                                      assignments, team)
if NOT enforced:
    EmitAllZonal(team, assignments, ref directive)   // F4 hard fallback (FR-DA-032)
    return

// Step 9: Publish
directive.team = team
PublishMarkDirective(ref directive)
PublishMarkAssignments(assignments, holdShapePool.count)
// tackleRequests published via ref; orchestrator reads tackleCount entries
```

**Determinism note (KD-10):** All iterations are EntityId-ascending (FR-DA-003).
Any RNG tie-breaking uses `DeterministicRngService.NextInt(DOMAIN_TAG_DEFENSIVE_AI)`
where `DOMAIN_TAG_DEFENSIVE_AI = 0x1A [CROSS-PENDING]` pending ERR-014-004
resolution in #16 §3.4.

---

## 3.14 Version History

| Version | Date | Author | Summary |
|---------|------|--------|---------|
| 0.1 | May 17, 2026 | AI agent | Initial draft from `outline-detailed.md` v1.0. All 13 algorithm subsections populated. §3.1 phase gate with `EmitAllZonal` pseudocode. §3.2 HOLD_SHAPE pool filter with GK and press-role exclusion. §3.3 mark-mode assignment with INTERCEPT_RUNNER > MAN_MARK > ZONAL priority. §3.4 displacement cost formula (units m², worked example, valid ranges). §3.5 threat score with perceivedGoalProximity x-axis-only formula and attribute normalisation `(attr-1)/19`; Q3 resolved (attribute range [1–20] → [0,1]). §3.6 tackle intent (COMMIT/JOCKEY/HOLD) with approach-angle and coverage-depth logic; worked example. §3.7 offside trap with four trigger conditions, dwell counter, step-depth formula, two worked examples (fires / blocked). §3.8 last-man predicate with `distToOwnGoal` team-agnostic normalisation; KD-12 formal definition; two worked examples. §3.9 COVER_GK_ZONE override; Q4 resolved (GK zone expressed as distToOwnGoal scalar, team-agnostic). §3.10 anti-chaos three-invariant pass loop (max 3 passes; F4 hard fallback); worked example. §3.11 assignment hysteresis binding to #2 §3.1; dwell-counter + holdTicks state; thrash-prevention worked example. §3.12 constants index with tags. §3.13 per-tick main-loop pseudocode (9 steps). §3.14 this version history. KD-5 Option B resolved via `TacticalContext.MarkDirective?` nullable field mechanism (aligned with #13 Option B per ERR-013-001; #8 §2.2.6 amendment to be filed as ERR-014-001). |
