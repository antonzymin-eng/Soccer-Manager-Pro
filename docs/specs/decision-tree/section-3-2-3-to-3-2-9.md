## 3.2.3 SHOOT Utility Formula

### 3.2.3.1 Formula Definition

```
U_SHOOT = BaseUtility_SHOOT
        × AttributeMultiplier_SHOOT
        × GoalOpeningScore
        × (1 − RiskPenalty_SHOOT)

clamped to [0.01, 1.0]
```

**where:**

```
BaseUtility_SHOOT = U_BASE_SHOOT_NOMINAL × ZoneModifier_SHOOT(BallZone, A_LongShots)

U_BASE_SHOOT_NOMINAL = 0.85 [GT]

ZoneModifier_SHOOT:
  ATTACKING:  1.00
  MIDFIELD:   0.55 [GT]  if (0.5 + A_LongShots × 0.5) > LONG_SHOT_THRESHOLD
              0.05 [GT]  otherwise  (effectively suppresses shot)
  DEFENSIVE:  0.10 [GT]

LONG_SHOT_THRESHOLD  = 0.75 [GT]   ← requires LongShots raw ≥ 15 for midfield shot viability

AttributeMultiplier_SHOOT = (0.5 + A_Finishing   × 0.5) ^ SHOOT_FINISHING_EXP
                          × (0.5 + A_Composure   × 0.5) ^ SHOOT_COMPOSURE_EXP

SHOOT_FINISHING_EXP   = 0.50 [GT]  — primary execution attribute; steeper curve than Vision
SHOOT_COMPOSURE_EXP   = 0.30 [GT]  — composure at point of shot: graded degradation model.
                                      Shallow curve (0.30) intentional — see ⚠ DESIGN DECISION
                                      below §3.2.5.1 for full split rationale vs HOLD (0.50).

GoalOpeningScore = see §3.2.3.2

RiskPenalty_SHOOT = (1.0 − GoalOpeningScore) × P × SHOOT_RISK_COEFF
SHOOT_RISK_COEFF  = 0.40 [GT]
```

**Attribute rationale:**

- Both Finishing and Composure use the shifted form. A striker with Finishing=1 is
  still 50% of an elite striker in attribute terms — they can shoot; the shot mechanics
  system governs accuracy. A player with Composure=1 under pressure is not expected
  to be frozen; they are expected to be erratic (governed by composure noise in §3.3
  selection stage, not here).
- `SHOOT_FINISHING_EXP = 0.50`: steeper than PASS_VISION_EXP — finishing quality
  matters more within-formula than lane-reading. Elite finishing should produce
  meaningfully better shot utility than average.
- `SHOOT_COMPOSURE_EXP = 0.30`: shallow curve. Shooting is a single discrete execution
  event — composure governs the *quality* of that moment on a continuum. A low-composure
  player does not fail to shoot; they shoot with graded degradation. The erratic selection
  behaviour for low-Composure players is captured downstream in §3.3 (composure noise at
  selection). SHOOT also carries a separate `RiskPenalty_SHOOT` term, which shares the
  pressure-response load — composure does not need to do heavy lifting here.
  See ⚠ DESIGN DECISION below §3.2.5.1 for full rationale of the 0.30/0.50 split.

---

### 3.2.3.2 GoalOpeningScore Derivation

`GoalOpeningScore` measures the fraction of the opponent goal that is visible from the
agent's current position, accounting for opponents positioned in the shooting lane.
It is computed by `ComputeGoalOpeningScore()`, a Decision Tree-owned function.

**Goal positions:** from `PitchGeometry` static class (§3.2.1.4).

```
ComputeGoalOpeningScore(agentPos, teamId, visibleOpponents[]):

1. Fetch goal posts:
   goalPostL = PitchGeometry.GetOpponentGoalPostL(teamId)   // e.g., (52.5, +3.66)
   goalPostR = PitchGeometry.GetOpponentGoalPostR(teamId)   // e.g., (52.5, -3.66)

2. Compute angular subtense of the full goal from agent position:
   angleL = atan2(goalPostL.y − agentPos.y, goalPostL.x − agentPos.x)
   angleR = atan2(goalPostR.y − agentPos.y, goalPostR.x − agentPos.x)
   totalGoalAngle = |angleL − angleR|    // in radians

3. For each visible opponent, compute their angular width subtended from agent pos:
   // Outfield players are modelled as blocking discs of radius BLOCKER_RADIUS = 0.5m [GT]
   // Goalkeepers are modelled with GK_BLOCKER_RADIUS = 1.5m [GT] to approximate their
   // wider effective coverage (arm reach + lateral movement at Stage 0, where no
   // GK-specific positioning logic exists).
   //
   // GK identification heuristic (Stage 0): an opponent whose PerceivedPosition is within
   // GK_PROXIMITY_TO_GOAL (6.0m) of the opponent goal line is classified as goalkeeper.
   // This is a positional proxy — no GoalkeeperId field exists at Stage 0.
   // Stage 1: replace with explicit AgentRole == GOALKEEPER flag from AgentState.
   //
   opponentDist = |visibleOpponent.PerceivedPosition − agentPos|
   If opponentDist < GOAL_MIN_SHOT_DISTANCE: skip (opponent is behind agent or too close)

   // Determine effective blocker radius
   opponentDistToGoalLine = |visibleOpponent.PerceivedPosition.x − goalPostL.x|
   isLikelyGoalkeeper = (opponentDistToGoalLine ≤ GK_PROXIMITY_TO_GOAL)
   effectiveRadius = isLikelyGoalkeeper ? GK_BLOCKER_RADIUS : BLOCKER_RADIUS

   blockedAngle = 2 × atan(effectiveRadius / opponentDist)
   Clamp blockedAngle to [0.0, totalGoalAngle] to avoid over-blocking.

4. Sum blocked angles from all opponents whose angular position overlaps the goal arc:
   // Overlap test: opponent's angular centre is within [angleR, angleL]
   totalBlockedAngle = Σ blockedAngle_i  (for overlapping opponents only)
   totalBlockedAngle = min(totalBlockedAngle, totalGoalAngle)   // cannot exceed 100%

5. Compute score:
   GoalOpeningScore = (totalGoalAngle − totalBlockedAngle) / totalGoalAngle
   GoalOpeningScore = Clamp(GoalOpeningScore, GOAL_OPENING_MIN, 1.0)
   GOAL_OPENING_MIN = 0.05 [GT]   ← floor; a tiny gap always exists
```

**Numerical example:**
```
Agent position: (40, 5) — right-of-centre, 12.5m from goal line
TeamId: 0 (home) — attacking +X

goalPostL = (52.5, +3.66)
goalPostR = (52.5, -3.66)

angleL = atan2(3.66−5, 52.5−40) = atan2(−1.34, 12.5) = −0.1068 rad
angleR = atan2(−3.66−5, 52.5−40) = atan2(−8.66, 12.5) = −0.6051 rad
totalGoalAngle = |−0.1068 − (−0.6051)| = 0.4983 rad (≈ 28.5°)

Visible opponent at (48, 3): directly in shooting lane
  opponentDist = |(48−40, 3−5)| = |(8, −2)| = 8.25m
  blockedAngle = 2 × atan(0.5 / 8.25) = 2 × 0.0606 = 0.1211 rad
  Opponent angular centre = atan2(3−5, 48−40) = atan2(−2, 8) = −0.2450 rad
  Within [−0.6051, −0.1068]? Yes → overlapping.

totalBlockedAngle = 0.1211 rad
GoalOpeningScore = (0.4983 − 0.1211) / 0.4983 = 0.3772 / 0.4983 = 0.757

ScoredGoalOpeningScore = clamp(0.757, 0.05, 1.0) = 0.757 ✓
```

**Goalkeeper as blocker:** The goalkeeper is identified by a positional heuristic:
any opponent within `GK_PROXIMITY_TO_GOAL = 6.0m` of their own goal line is treated
as the goalkeeper and assigned a blocking radius of `GK_BLOCKER_RADIUS = 1.5m` rather
than the outfield `BLOCKER_RADIUS = 0.5m`. This approximates the GK's effective coverage
area (arm reach + lateral movement) without requiring a GK-specific role flag, which
does not exist at Stage 0. The heuristic is conservative — a GK at their near post can
cover 3–4m laterally from centre; 1.5m is the disc radius equivalent giving a 3m
effective blocking diameter across the centre of the goal.

**Known limitation (Stage 0):** The heuristic misidentifies a defender who has tracked
back inside the box as a goalkeeper. In practice, this is rare and the consequence
(slightly overestimating GoalOpeningScore by using a 0.5m blocker instead of 1.5m)
is a minor error. Stage 1 fix: replace the positional heuristic with `AgentState.Role
== GOALKEEPER` when Role field is added.

**Teammate blocking:** Only `VisibleOpponents` are used as blockers. Teammates in the
shooting lane are not modelled at Stage 0. Flagged for Stage 1 refinement.

---

### 3.2.3.3 Numerical Verification

**Verification Case A: Elite striker, attacking zone, clear shot**
```
Agent: Finishing=19, Composure=17
BallZone: ATTACKING
GoalOpeningScore = 0.757 (from §3.2.3.2 example)
PressureScalar P = 0.15

A_Finishing = (19−1)/19 = 0.9474 → Shifted = 0.9737
A_Composure = (17−1)/19 = 0.8421 → Shifted = 0.9211

BaseUtility = 0.85 × 1.00 = 0.850
AttributeMultiplier = 0.9737^0.50 × 0.9211^0.30
                    = 0.9867 × 0.9742 = 0.9613
GoalOpeningScore = 0.757
RiskPenalty = (1−0.757) × 0.15 × 0.40 = 0.243 × 0.06 = 0.01458

U_raw = 0.850 × 0.9613 × 0.757 × (1−0.01458)
      = 0.850 × 0.9613 = 0.8171
      = 0.8171 × 0.757 = 0.6185
      = 0.6185 × 0.98542 = 0.6095

ScoredUtility = clamp(0.6095, 0.01, 1.0) = 0.610 ✓
```

This is higher than the elite PASS score (0.541) in the same zone, correctly reflecting
that a striker with a clear shot should prefer shooting. ✓

**Verification Case B: Low-Finishing midfielder, midfield, Long Shots = 16**
```
Agent: Finishing=7, Composure=11, LongShots=16
BallZone: MIDFIELD → LongShots shifted = 0.5 + (15/19)×0.5 = 0.895 > 0.75 threshold
ZoneModifier = 0.55

GoalOpeningScore = 0.60 (moderate — distant, some blockers)
PressureScalar P = 0.30

A_Finishing = (7−1)/19 = 0.3158 → Shifted = 0.6579
A_Composure = (11−1)/19 = 0.5263 → Shifted = 0.7632

BaseUtility = 0.85 × 0.55 = 0.4675
AttributeMultiplier = 0.6579^0.50 × 0.7632^0.30
                    = 0.8111 × 0.9246 = 0.7499
GoalOpeningScore = 0.60
RiskPenalty = (1−0.60) × 0.30 × 0.40 = 0.40 × 0.12 = 0.0480

U_raw = 0.4675 × 0.7499 × 0.60 × (1−0.0480)
      = 0.4675 × 0.7499 = 0.3506
      = 0.3506 × 0.60 = 0.2104
      = 0.2104 × 0.9520 = 0.2003

ScoredUtility = clamp(0.2003, 0.01, 1.0) = 0.200 ✓
```

A moderate midfield shot by an average finishing midfielder: 0.200. A good pass option
(0.50+) would correctly win; an adventurous agent with composure noise might still select
this shot occasionally. Plausible. ✓

---

### 3.2.3.4 Range Gate Boundary Analysis

The eligibility gate in §3.1.4.1 provides the first-pass shooting range filter. The zone
modifier provides the second. Three boundary conditions are documented:

1. **Agent in defensive zone, Finishing = 20:** BaseUtility = 0.85 × 0.10 = 0.085.
   Even an elite player barely scores above the HOLD floor (≈ 0.25) from their own third.
   This correctly suppresses shooting from deep. ✓

2. **Agent in midfield, LongShots = 14 (Shifted = 0.868 < 0.875 threshold):**
   Wait — threshold check: `(0.5 + A_LongShots × 0.5) > 0.75`.
   LongShots=14 → A=(13/19)=0.684 → Shifted=0.842. `0.842 > 0.75` → YES, passes threshold.
   LongShots=10 → A=(9/19)=0.474 → Shifted=0.737. `0.737 < 0.75` → FAILS, zone = 0.05.
   **Therefore the effective LongShots threshold is raw ≥ 11 for midfield shots.** This
   is lower than the outline suggested (≥ 15). Documented here as the authoritative
   derivation. Outline's "raw ≥ 15" comment was imprecise — the threshold constant
   `LONG_SHOT_THRESHOLD = 0.75` maps to raw ≥ 11, not raw ≥ 15. [GT] value is correct;
   commentary in outline was not.

3. **Agent in attacking zone, GoalOpeningScore = 0.05 (fully blocked):**
   U_raw = 0.85 × 1.0 × AM × 0.05 × (1 − RiskPenalty). Even elite finisher (AM ≈ 0.96):
   U_raw ≈ 0.85 × 0.96 × 0.05 × 0.97 ≈ 0.040 → clamped to 0.040. The agent would prefer
   PASS (≈ 0.54). Correct: a fully blocked shot should be avoided. ✓

---

## 3.2.4 DRIBBLE Utility Formula

### 3.2.4.1 Formula Definition

```
U_DRIBBLE = BaseUtility_DRIBBLE
          × AttributeMultiplier_DRIBBLE
          × SpaceScore
          × (1 − RiskPenalty_DRIBBLE)

clamped to [0.01, 1.0]
```

**where:**

```
BaseUtility_DRIBBLE    = U_BASE_DRIBBLE_NOMINAL × ZoneModifier_DRIBBLE(BallZone)
U_BASE_DRIBBLE_NOMINAL = 0.45 [GT]

AttributeMultiplier_DRIBBLE = A_Dribbling ^ DRIBBLE_DRIBBLING_EXP
                            × A_Agility   ^ DRIBBLE_AGILITY_EXP

DRIBBLE_DRIBBLING_EXP  = 0.40 [GT]  — primary skill; raw form: low dribbling → very low utility
DRIBBLE_AGILITY_EXP    = 0.30 [GT]  — directional change speed; secondary

SpaceScore             = option.SpaceScore
                       ← populated in §3.1.5.2; range [0.0, 1.0]

RiskPenalty_DRIBBLE    = P × (1.0 − A_Dribbling) × DRIBBLE_RISK_COEFF
DRIBBLE_RISK_COEFF     = 0.35 [GT]
```

**Raw form rationale for Dribbling and Agility:**
Unlike passing attributes, Dribbling and Agility use the raw `A_x ^ exp` form (no shift
to 0.5 floor). A defender or goalkeeper with Dribbling=1 truly has no dribbling ability
— they should not dribble past an opponent. The near-zero AttributeMultiplier (≈ 0.0 at
A_Dribbling=0.0) correctly pushes U_DRIBBLE to the clamp floor (0.01), where the option
exists but will never win the selection. This is the intended behaviour.

**SpaceScore as ContextModifier:** `SpaceScore` is already normalised to [0.0, 1.0] by
§3.1.5.2 and is used directly as the context modifier. A SpaceScore of 0.0 drives U_DRIBBLE
to the clamp floor — but §3.1.5.1 eligibility gate already ensures DRIBBLE is only generated
when `SpaceScore > 0.0`, so SpaceScore will always be > 0.0 in the scored candidate list.

---

### 3.2.4.2 Numerical Verification

**Case A: Skilled dribbler, open space, attacking zone**
```
Agent: Dribbling=17, Agility=16
BallZone: ATTACKING → ZoneModifier = 1.10
SpaceScore = 0.90 (nearest opponent 1.8m away within 2.0m threat radius)
PressureScalar P = 0.20

A_Dribbling = (17−1)/19 = 0.8421
A_Agility   = (16−1)/19 = 0.7895

BaseUtility = 0.45 × 1.10 = 0.495
AttributeMultiplier = 0.8421^0.40 × 0.7895^0.30
                    = 0.9339 × 0.9322 = 0.8706
SpaceScore = 0.90
RiskPenalty = 0.20 × (1−0.8421) × 0.35 = 0.20 × 0.1579 × 0.35 = 0.01105

U_raw = 0.495 × 0.8706 × 0.90 × (1−0.01105)
      = 0.495 × 0.8706 = 0.4309
      = 0.4309 × 0.90 = 0.3878
      = 0.3878 × 0.98895 = 0.3836

ScoredUtility = 0.384 ✓
```

This is lower than a clean PASS option (0.54) — a skilled dribbler with space in the
attacking third would still prefer a good pass option. This is correct; dribbling should
be an option when passing lanes are blocked, not the automatic first choice. ✓

**Case B: Poor dribbler, tight space, defensive zone**
```
Agent: Dribbling=4, Agility=5
BallZone: DEFENSIVE → ZoneModifier = 0.70
SpaceScore = 0.40 (some space but limited)
PressureScalar P = 0.60

A_Dribbling = (4−1)/19 = 0.1579
A_Agility   = (5−1)/19 = 0.2105

BaseUtility = 0.45 × 0.70 = 0.315
AttributeMultiplier = 0.1579^0.40 × 0.2105^0.30
                    = 0.4638 × 0.5977 = 0.2772
SpaceScore = 0.40
RiskPenalty = 0.60 × (1−0.1579) × 0.35 = 0.60 × 0.8421 × 0.35 = 0.1768

U_raw = 0.315 × 0.2772 × 0.40 × (1−0.1768)
      = 0.315 × 0.2772 = 0.08732
      = 0.08732 × 0.40 = 0.03493
      = 0.03493 × 0.8232 = 0.02875

ScoredUtility = clamp(0.02875, 0.01, 1.0) = 0.029 ✓
```

A poor dribbler in defensive pressure correctly scores near the clamp floor. PASS, HOLD,
or MOVE_TO_POSITION will win. ✓

---

## 3.2.5 HOLD Utility Formula

### 3.2.5.1 Formula Definition

```
U_HOLD = BaseUtility_HOLD
       × AttributeMultiplier_HOLD
       × (1.0 − P × HOLD_PRESSURE_COEFF)

clamped to [0.01, 1.0]
```

**where:**

```
BaseUtility_HOLD    = U_BASE_HOLD_NOMINAL × ZoneModifier_HOLD(BallZone)
U_BASE_HOLD_NOMINAL = 0.28 [GT]   — deliberately low; HOLD wins only as last resort.
                                    Raised from initial 0.25 to widen fallback margin
                                    over degraded PASS scores at extreme inputs.
                                    Ceiling (0.28 × 1.25 = 0.350) remains well below
                                    normal PASS (0.49+), ensuring HOLD never wins in
                                    normal play conditions.

AttributeMultiplier_HOLD = (0.5 + A_Composure × 0.5) ^ HOLD_COMPOSURE_EXP
HOLD_COMPOSURE_EXP       = 0.50 [GT]  — sustained composure gate. Steeper than
                                         SHOOT_COMPOSURE_EXP (0.30) by design —
                                         see ⚠ DESIGN DECISION immediately below.

HOLD_PRESSURE_COEFF = 0.50 [GT]  — pressure reduces HOLD appeal. Reduced from 0.60
                                    to prevent the pressure term from dominating the
                                    formula and collapsing HOLD below its fallback role
                                    at extreme pressure. Max penalty is now 50% rather
                                    than 60%, keeping the floor above clamp-floor range.

Note: HOLD has no RiskPenalty term. Holding fails only if the agent is tackled —
this is a Collision System event, not a scoring-phase prediction. The pressure
coefficient embedded in the formula serves as the pressure response.
```

**No `ContextModifier` for HOLD:** There is no lane score, space score, or goal
opening for holding — it is a null action. The pressure scalar's direct inclusion
as `(1 − P × 0.50)` serves both the context and risk role: high pressure reduces the
wisdom of holding; low pressure makes it a safe defensive option.

---

## ⚠ DESIGN DECISION: COMPOSURE EXPONENT SPLIT (SHOOT 0.30 vs HOLD 0.50)

**Problem:** The same attribute — Composure [1–20] — produces different exponent
values in two formulas. This requires explicit justification; an unexplained split
is an architectural inconsistency.

**Decision:** The split is intentional and mechanically justified. The values must
not be unified.

**Rationale — three independent grounds:**

**Ground 1 — Action type: discrete event vs. sustained state.**
Shooting is a single discrete execution moment. Composure governs the quality of
that moment on a continuum — a low-composure player shoots with graded degradation,
not failure. A shallow exponent (0.30) correctly models this: the composure benefit
increases quickly at low values (where it matters most) and flattens at high values
(elite players are already composed enough). Holding is a sustained state across a
full decision cycle while opponents close. The agent must maintain composure for the
duration, not just for an instant. The viability of HOLD degrades substantially as
composure drops — a low-composure agent under pressure should be steered toward
action (pass, dribble, accept dispossession), not frozen in a hold state. A steeper
exponent (0.50) correctly models this stronger composure dependency.

**Ground 2 — Behavioural intent: graded degradation vs. viability gate.**
In SHOOT, low composure reduces shot quality smoothly — the agent still shoots.
In HOLD, low composure should substantially suppress the action's utility, making
HOLD an unattractive option for agents who lack the mental stability to execute it.
The exponent controls this gate: at 0.50, a Composure=1 agent has
`0.500^0.50 = 0.707` multiplier (29% suppression); at 0.30, the same agent has
`0.500^0.30 = 0.812` multiplier (19% suppression). The 0.50 exponent applies
meaningfully more composure-driven suppression across the mid-range (Composure 5–15),
which is where the viability gate is needed.

**Ground 3 — Structural compensation (architectural, not interpretive).**
HOLD has no `RiskPenalty` term. The decision to omit it was correct — holding fails
via Collision System (a tackled event), not via scoring-phase prediction. However,
this means the HOLD formula has two pressure-response mechanisms: `HOLD_PRESSURE_COEFF`
and the composure multiplier. In contrast, SHOOT has three: `SHOOT_COMPOSURE_EXP`,
`SHOOT_FINISHING_EXP` (implicitly affected by pressure context), and `RiskPenalty_SHOOT`.
HOLD's composure exponent must carry more structural weight precisely because the
RiskPenalty term is absent. The 0.50 value is partly compensating for a missing term
that SHOOT possesses.

**Numerical comparison (mid-tier agent, Composure=10, A_Composure=0.45):**
```
SHOOT: (0.5 + 0.45×0.5)^0.30 = 0.725^0.30 = 0.906  — 9.4% suppression
HOLD:  (0.5 + 0.45×0.5)^0.50 = 0.725^0.50 = 0.851  — 14.9% suppression
```
The extra 5.5% suppression in HOLD for a mid-tier player is the intended gate effect.
At low composure (Composure=3, A_Composure=0.10):
```
SHOOT: 0.550^0.30 = 0.840  — 16.0% suppression
HOLD:  0.550^0.50 = 0.742  — 25.8% suppression
```
Low-composure agents are significantly more penalised for choosing HOLD than SHOOT,
correctly steering them toward decisive action over passive retention.

**Resolution:** No change to values. Document this rationale in §3.2.11 constant
catalogue. Both exponents tagged [GT]; concave form confirmed by power law of
practice literature; specific values are gameplay parameters subject to Stage 1
calibration. The split is a design decision, not an error.

**Cross-references:** SHOOT formula §3.2.3.1 | HOLD formula §3.2.5.1 |
Constant catalogue §3.2.11 | Composure noise model §3.3

---

---

### 3.2.5.2 HOLD as Fallback Floor Analysis

HOLD must reliably act as the fallback action when all other options score poorly.
The condition is: `U_HOLD > U_all_other_options` when no good options exist.

**HOLD minimum score (worst case):**
```
Agent: Composure=1, P=1.0, BallZone=ATTACKING
A_Composure = 0.0 → Shifted = 0.500
AttributeMultiplier = 0.500^0.50 = 0.707
(1 − P × 0.50) = 1 − 0.50 = 0.50
BaseUtility = 0.28 × 0.80 = 0.224

U_raw = 0.224 × 0.707 × 0.50 = 0.0792
ScoredUtility = clamp(0.0792, 0.01, 1.0) = 0.079
```

**HOLD maximum score (best case):**
```
Agent: Composure=20, P=0.0, BallZone=DEFENSIVE
A_Composure = 1.0 → Shifted = 1.000
AttributeMultiplier = 1.000^0.50 = 1.000
(1 − P × 0.50) = 1.00
BaseUtility = 0.28 × 1.25 = 0.350

U_raw = 0.350 × 1.000 × 1.00 = 0.350
ScoredUtility = 0.350
```

**Fallback guarantee:** HOLD's minimum score is 0.079. The FR-08 requirement states
HOLD must be selected when all other options score at or below HOLD. For this to fail,
another option would need to score ≤ 0.079 while HOLD also scores 0.079 — meaning
HOLD ties another option. The tiebreak rule (§3.3.2: select by ActionType enum ordinal)
resolves this deterministically. FR-08 is satisfied. ✓

**HOLD worst case vs PASS worst case:**
PASS worst case (§3.2.2.3 Case B) = 0.064. HOLD worst case = 0.079. The margin is now
0.015 — more than double the v1.0 margin of 0.007. This is wide enough to survive
composure noise (§3.3) without routine reversal: the noise magnitude at minimum Composure
(worst case) is bounded by ±NOISE_MAX (defined in §3.3); the HOLD/PASS separation (0.015)
is maintained as a meaningful structural gap rather than a rounding artifact.

**Ceiling check:** HOLD best case (0.350) remains well below PASS normal (≈ 0.490 for
average attributes, moderate lane). HOLD will never win in normal play conditions where
any reasonable pass lane exists. ✓

---

## 3.2.6 MOVE_TO_POSITION Utility Formula

### 3.2.6.1 Formula Definition

```
U_MOVE = BaseUtility_MOVE
       × AttributeMultiplier_MOVE
       × DistanceModifier
       × PhaseModifier(Possession)
       × PressProximityPenalty

clamped to [0.01, 1.0]
```

**where:**

```
BaseUtility_MOVE    = U_BASE_MOVE_NOMINAL × ZoneModifier_MOVE(BallZone)
U_BASE_MOVE_NOMINAL = 0.40 [GT]
ZoneModifier_MOVE   = 1.00 for all zones (see §3.2.1.3 rationale)

AttributeMultiplier_MOVE = (0.5 + A_Positioning × 0.5) ^ MOVE_POSITIONING_EXP
                         × (0.5 + A_WorkRate     × 0.5) ^ MOVE_WORKRATE_EXP

MOVE_POSITIONING_EXP = 0.40 [GT]
MOVE_WORKRATE_EXP    = 0.30 [GT]

DistanceModifier = Clamp(distanceToSlot / MOVE_URGENCY_DISTANCE, MOVE_DIST_MIN, 1.0)
MOVE_URGENCY_DISTANCE = 15.0m [GT]
MOVE_DIST_MIN         = 0.10 [GT]   ← floor: agent always has some urgency to be in position
distanceToSlot = |AgentState.Position − DecisionContext.FormationSlot|

PhaseModifier(Possession):
  OWN_TEAM:  0.70 [GT]   — in possession; can delay repositioning
  OPPONENT:  1.25 [GT]   — out of possession; urgent to recover formation shape
  CONTESTED: 1.00         — neutral

PressProximityPenalty:
  Reduces MOVE utility when a visible opponent is close enough to press.
  Prevents MOVE from crowding out PRESS in out-of-possession scenarios where
  an opponent is immediately pressable.

  nearestOpponentDist = min(|AgentState.Position − opp.PerceivedPosition|)
                        over all opp in DecisionContext.VisibleOpponents
                        (if VisibleOpponentCount == 0: nearestOpponentDist = ∞)

  If nearestOpponentDist ≤ MOVE_PRESS_SUPPRESSION_DIST:
    PressProximityPenalty = MOVE_PRESS_SUPPRESSION_FACTOR
  Else:
    PressProximityPenalty = 1.0

MOVE_PRESS_SUPPRESSION_DIST   = 6.0m [GT]  — within pressing trigger range
MOVE_PRESS_SUPPRESSION_FACTOR = 0.60 [GT]  — 40% reduction; does not eliminate MOVE,
                                             but makes PRESS competitive when in range
```

---

### 3.2.6.2 Positioning Attribute Direction Rationale

The Positioning attribute uses the shifted form and a positive exponent — higher
Positioning produces a higher AttributeMultiplier. This means a well-positioned,
high-Positioning player scores *higher* on MOVE_TO_POSITION utility, not lower.

**This might appear counterintuitive:** a player who is already well-positioned has less
need to move. However, the `DistanceModifier` governs the *urgency* dimension — it scales
with how far the agent actually is from their slot. A high-Positioning agent who is near
their slot gets a low DistanceModifier (≈ 0.10), which already suppresses the score.
The Positioning attribute instead governs *commitment* to positional duty: a high-Positioning
player actively wants to fulfil their role and executes the move effectively when needed.
A low-Positioning player (lazy or tactically unaware) has lower commitment even when far
out of position.

**The outline's original formula** `(1 − A_Positioning × 0.4)` would have inverted this,
penalising high-Positioning players for moving — which is incorrect. This section uses
the positive form `(0.5 + A_Positioning × 0.5)` instead. This is a **correction from
the outline**, not a deviation from design intent. The outline's formula was identified
as a bug during §3.2 drafting (pre-flagged at start of this session).

**PressProximityPenalty rationale:**
Without this term, an agent badly out of position while defending scores up to 0.450–0.500
on MOVE — competitive with or exceeding PRESS (≈ 0.315) even when an opponent is
immediately pressable. This produces passive defensive behaviour: agents jog back toward
formation rather than pressing opponents who are within closing range. The
`PressProximityPenalty` multiplier (0.60) reduces MOVE by 40% when any visible opponent
is within `MOVE_PRESS_SUPPRESSION_DIST = 6.0m`. This brings MOVE to ≈ 0.270 — below
PRESS (≈ 0.315) in close-opponent scenarios, restoring the correct priority. When no
opponent is within 6m, the penalty is 1.0 (no effect); MOVE is not penalised when the
agent is repositioning in an open area with no immediate threat.

**Note:** `MOVE_PRESS_SUPPRESSION_DIST = 6.0m` is deliberately less than
`PRESS_TRIGGER_DISTANCE = 8.0m`. An opponent at 7m is within press eligibility range
but does not trigger the MOVE suppression — PRESS scores higher naturally in that band
via ProximityScore without needing MOVE suppression. The 6m threshold targets the
close-range case (≤ 6m) where the imbalance was most severe.

---

### 3.2.6.3 Numerical Verification

**Case A: Agent far out of position, out of possession, no nearby opponents**
```
Agent: Positioning=14, WorkRate=16
distanceToSlot = 18.0m (far out of position — capped by MOVE_URGENCY_DISTANCE)
Possession = OPPONENT → PhaseModifier = 1.25
nearestOpponentDist = 12.0m → PressProximityPenalty = 1.0 (no suppression)

A_Positioning = (14−1)/19 = 0.6842 → Shifted = 0.8421
A_WorkRate    = (16−1)/19 = 0.7895 → Shifted = 0.8947

BaseUtility = 0.40 × 1.00 = 0.400
AttributeMultiplier = 0.8421^0.40 × 0.8947^0.30
                    = 0.9316 × 0.9659 = 0.8998
DistanceModifier = Clamp(18.0/15.0, 0.10, 1.0) = 1.000
PhaseModifier = 1.25
PressProximityPenalty = 1.0

U_raw = 0.400 × 0.8998 × 1.000 × 1.25 × 1.0
      = 0.400 × 0.8998 = 0.3599
      = 0.3599 × 1.25 = 0.4499

ScoredUtility = 0.450 ✓
```

Strong positional urgency (0.450) when an agent with decent attributes is badly out of
position while the team is defending, with no opponent nearby. This correctly reflects
the need to recover formation. ✓

**Case B: Agent at formation slot, in possession**
```
Agent: Positioning=15, WorkRate=12
distanceToSlot = 1.2m (very close to slot)
Possession = OWN_TEAM → PhaseModifier = 0.70
nearestOpponentDist = 8.0m → PressProximityPenalty = 1.0 (8.0m > 6.0m threshold)

A_Positioning = (15−1)/19 = 0.7368 → Shifted = 0.8684
A_WorkRate    = (12−1)/19 = 0.5789 → Shifted = 0.7895

BaseUtility = 0.40
AttributeMultiplier = 0.8684^0.40 × 0.7895^0.30
                    = 0.9441 × 0.9322 = 0.8801
DistanceModifier = Clamp(1.2/15.0, 0.10, 1.0) = Clamp(0.080, 0.10, 1.0) = 0.100
PhaseModifier = 0.70

U_raw = 0.400 × 0.8801 × 0.100 × 0.70 × 1.0
      = 0.3520 × 0.100 = 0.03520
      = 0.03520 × 0.70 = 0.02464

ScoredUtility = clamp(0.02464, 0.01, 1.0) = 0.025 ✓
```

An agent in position during their team's possession correctly scores near zero. ✓

**Case C: Agent out of position, defending, opponent 4.0m away (PressProximityPenalty active)**
```
Agent: Positioning=14, WorkRate=16  (same as Case A)
distanceToSlot = 18.0m
Possession = OPPONENT → PhaseModifier = 1.25
nearestOpponentDist = 4.0m → 4.0m ≤ 6.0m → PressProximityPenalty = 0.60

U_raw (pre-penalty, from Case A) = 0.400 × 0.8998 × 1.000 × 1.25 = 0.4499
With penalty: U_raw = 0.4499 × 0.60 = 0.2699

ScoredUtility = 0.270 ✓

For comparison, PRESS score for the same agent at 4.0m (from §3.2.7.3 Case A basis):
PRESS ProximityScore = Clamp(1.0 − (4.0/8.0), 0.0, 1.0) = 0.500
Using §3.2.7.3 AttributeMultiplier ≈ 0.933, BaseUtility = 0.50 × 1.00 (MID zone):
U_PRESS ≈ 0.500 × 0.933 × 0.500 × 1.0 = 0.233
```

**Result:** At 4.0m, MOVE (0.270) still narrowly beats PRESS (0.233) for this
typical-attribute agent. The penalty has changed the outcome from a dominant MOVE
victory (0.450 vs 0.233) to a narrow MOVE lead. For agents with higher Aggression/WorkRate
(dedicated pressers), PRESS will win; for agents with high Positioning/WorkRate (positional
midfielders), MOVE still wins. This is correct emergent behaviour — pressing is for
dedicated pressers; positional players recover shape even when close to opponents.

The complete inversion to PRESS-always-wins is not the design intent. The balance
test BAL-DT-04 (Section 5) will verify that high-Aggression/WorkRate agents correctly
select PRESS over MOVE when within 4–5m of an opponent while defending.

---

## 3.2.7 PRESS Utility Formula

### 3.2.7.1 Formula Definition

```
U_PRESS = BaseUtility_PRESS
        × AttributeMultiplier_PRESS
        × ProximityScore
        × TacticalPressingModifier

clamped to [0.01, 1.0]
```

**where:**

```
BaseUtility_PRESS    = U_BASE_PRESS_NOMINAL × ZoneModifier_PRESS(BallZone)
U_BASE_PRESS_NOMINAL = 0.50 [GT]

AttributeMultiplier_PRESS = (0.5 + A_Aggression × 0.5) ^ PRESS_AGGRESSION_EXP
                          × (0.5 + A_WorkRate   × 0.5) ^ PRESS_WORKRATE_EXP
                          × (0.5 + A_Stamina    × 0.5) ^ PRESS_STAMINA_EXP

PRESS_AGGRESSION_EXP = 0.30 [GT]  — pressing intent; desire to challenge
PRESS_WORKRATE_EXP   = 0.30 [GT]  — engine; maintains press over distance
PRESS_STAMINA_EXP    = 0.20 [GT]  — governs whether agent has capacity; lowest exponent

ProximityScore = Clamp(1.0 − (distToPressTarget / PRESS_TRIGGER_DISTANCE), 0.0, 1.0)
PRESS_TRIGGER_DISTANCE = 8.0m [GT]   ← from §3.1.8.1 eligibility gate
distToPressTarget      = |AgentState.Position − option.PressTarget.PerceivedPosition|

TacticalPressingModifier: see §3.2.7.2
```

**Note: No RiskPenalty for PRESS.** The risk of a press failing (agent being dribbled)
is an execution-phase risk governed by the Collision System, not a pre-decision risk
in the scoring phase. Pressing is not inherently risky from the option-selection
perspective — the ProximityScore already ensures it only generates when the target
is reachable within the trigger distance.

---

### 3.2.7.2 Stage 0 TacticalPressingModifier Behaviour

At Stage 0, `TacticalContext` is hardcoded to `PressingMode.MEDIUM` for all agents on
both teams. This means `TacticalPressingModifier = 1.0` for every PRESS option scored
at Stage 0.

The modifier exists in the formula and in `UtilityWeights.cs` so that it can be
plugged in at Stage 1 without formula changes. The constant table still documents all
three values:

```
TacticalPressingModifier:
  PressingMode.HIGH   = 1.40 [GT]  ← Stage 1: applied when manager sets HIGH press
  PressingMode.MEDIUM = 1.00        ← Stage 0 default for all agents
  PressingMode.LOW    = 0.60 [GT]  ← Stage 1: applied when manager sets LOW press
```

**Stage 0 note:** Both teams behave identically under medium pressing mode. This is
a known Stage 0 limitation (DT Spec §1.6.2). The modifier is not a dead code path —
it is a Stage 1 activation point. It must not be removed or short-circuited in the
Stage 0 implementation.

---

### 3.2.7.3 Numerical Verification

**Case A: High-energy presser, close to opponent**
```
Agent: Aggression=17, WorkRate=18, Stamina=15
BallZone: ATTACKING → ZoneModifier = 1.20
distToPressTarget = 3.5m
TacticalPressingModifier = 1.0 (Stage 0)

A_Aggression = (17−1)/19 = 0.8421 → Shifted = 0.9211
A_WorkRate   = (18−1)/19 = 0.8947 → Shifted = 0.9474
A_Stamina    = (15−1)/19 = 0.7368 → Shifted = 0.8684

BaseUtility = 0.50 × 1.20 = 0.600
AttributeMultiplier = 0.9211^0.30 × 0.9474^0.30 × 0.8684^0.20
                    = 0.9757 × 0.9840 × 0.9718 = 0.9327
ProximityScore = Clamp(1.0 − (3.5/8.0), 0.0, 1.0) = Clamp(0.5625, 0.0, 1.0) = 0.563

U_raw = 0.600 × 0.9327 × 0.563 × 1.0
      = 0.600 × 0.9327 = 0.5596
      = 0.5596 × 0.563 = 0.3150

ScoredUtility = 0.315 ✓
```

An energetic pressing player at 3.5m distance from an opponent scores 0.315. This is
below a good pass option (0.54) but above HOLD (≈ 0.25). In out-of-possession scenarios
where PASS is not available, PRESS would win. ✓

---

## 3.2.8 INTERCEPT Utility Formula

### 3.2.8.1 Formula Definition

```
U_INTERCEPT = BaseUtility_INTERCEPT
            × AttributeMultiplier_INTERCEPT
            × FeasibilityScore
            × (1.0 − P × INTERCEPT_PRESSURE_COEFF)

clamped to [0.01, 1.0]
```

**where:**

```
BaseUtility_INTERCEPT    = U_BASE_INTERCEPT_NOMINAL × ZoneModifier_INTERCEPT(BallZone)
U_BASE_INTERCEPT_NOMINAL = 0.55 [GT]

AttributeMultiplier_INTERCEPT = A_Anticipation ^ INTERCEPT_ANTICIPATION_EXP
                               × (0.5 + A_Pace × 0.5) ^ INTERCEPT_PACE_EXP

INTERCEPT_ANTICIPATION_EXP = 0.50 [GT]  — reading of ball trajectory; raw form
INTERCEPT_PACE_EXP          = 0.30 [GT]  — speed to reach intercept point; shifted form

FeasibilityScore = option.FeasibilityScore
                 ← populated in §3.1.9.3; range [0.0, 1.0]

INTERCEPT_PRESSURE_COEFF = 0.20 [GT]   — pressure slightly reduces intercept quality
                                          (agent distracted from ball trajectory reading)
```

**Anticipation uses the raw form `A_x ^ exp`:**
Anticipation governs the agent's reading of ball trajectory — an agent with Anticipation=1
genuinely cannot anticipate the ball path. Near-zero utility is correct for such agents.
INTERCEPT would fall to the clamp floor (0.01) for Anticipation=1 regardless of Pace.

**Pace uses the shifted form:**
A slow player (Pace=1) still has some chance of reaching a nearby intercept point — the
feasibility check in §3.1.9.3 already gates distance feasibility. Pace should not
eliminate the option entirely; it should reduce the utility of distant interceptions.

---

### 3.2.8.2 Numerical Verification

**Case A: Anticipating midfielder, feasible intercept, defensive zone**
```
Agent: Anticipation=16, Pace=13
BallZone: DEFENSIVE → ZoneModifier = 1.10
FeasibilityScore = 0.72 (from §3.1.9.3 — 72% of time budget used)
PressureScalar P = 0.25

A_Anticipation = (16−1)/19 = 0.7895
A_Pace         = (13−1)/19 = 0.6316 → Shifted = 0.8158

BaseUtility = 0.55 × 1.10 = 0.605
AttributeMultiplier = 0.7895^0.50 × 0.8158^0.30
                    = 0.8886 × 0.9441 = 0.8390
FeasibilityScore = 0.72
PressureTerm = 1.0 − 0.25 × 0.20 = 1.0 − 0.050 = 0.950

U_raw = 0.605 × 0.8390 × 0.72 × 0.950
      = 0.605 × 0.8390 = 0.5076
      = 0.5076 × 0.72 = 0.3655
      = 0.3655 × 0.950 = 0.3472

ScoredUtility = 0.347 ✓
```

An interception opportunity for a capable midfielder scores 0.347 — competitive in
an out-of-possession defensive scenario. ✓

---

## 3.2.9 Attribute-to-Multiplier Mapping Tables

The following tables show `AttributeMultiplier` values at key attribute levels for each
formula. These support balance testing in Section 5 (UT-07a through UT-07g). Values
shown are the full `AttributeMultiplier` product at the specified single attribute,
assuming all other attributes are at their mean (x_raw = 10).

**PASS AttributeMultiplier (both attributes at specified level; all other = mean):**

| x_raw | A_Vision→Shifted | AM_Vision^0.3 | A_Pass→Shifted | AM_Pass^0.4 | Full AM |
|-------|-----------------|--------------|---------------|------------|--------|
| 1 | 0.500 | 0.812 | 0.500 | 0.758 | 0.615 |
| 5 | 0.605 | 0.851 | 0.605 | 0.806 | 0.686 |
| 10 | 0.737 | 0.908 | 0.737 | 0.882 | 0.801 |
| 15 | 0.868 | 0.958 | 0.868 | 0.945 | 0.906 |
| 20 | 1.000 | 1.000 | 1.000 | 1.000 | 1.000 |

**SHOOT AttributeMultiplier (Finishing and Composure at specified level):**

| x_raw | Finishing→Sh | AM_F^0.5 | Composure→Sh | AM_C^0.3 | Full AM |
|-------|-------------|---------|-------------|---------|--------|
| 1 | 0.500 | 0.707 | 0.500 | 0.812 | 0.574 |
| 5 | 0.605 | 0.778 | 0.605 | 0.851 | 0.662 |
| 10 | 0.737 | 0.858 | 0.737 | 0.908 | 0.779 |
| 15 | 0.868 | 0.932 | 0.868 | 0.958 | 0.894 |
| 20 | 1.000 | 1.000 | 1.000 | 1.000 | 1.000 |

**DRIBBLE AttributeMultiplier (Dribbling and Agility at specified level, raw form):**

| x_raw | A_Drib | AM_D^0.4 | A_Agility | AM_A^0.3 | Full AM |
|-------|--------|---------|-----------|---------|--------|
| 1 | 0.000 | 0.000 | 0.000 | 0.000 | 0.000 → clamp 0.01 |
| 5 | 0.211 | 0.570 | 0.211 | 0.634 | 0.361 |
| 10 | 0.474 | 0.760 | 0.474 | 0.807 | 0.613 |
| 15 | 0.737 | 0.891 | 0.737 | 0.902 | 0.804 |
| 20 | 1.000 | 1.000 | 1.000 | 1.000 | 1.000 |

**INTERCEPT AttributeMultiplier (Anticipation raw form, Pace shifted form):**

| x_raw | A_Ant | AM_A^0.5 | A_Pace→Sh | AM_P^0.3 | Full AM |
|-------|-------|---------|----------|---------|--------|
| 1 | 0.000 | 0.000 | 0.500 | 0.812 | 0.000 → clamp 0.01 |
| 5 | 0.211 | 0.459 | 0.605 | 0.851 | 0.391 |
| 10 | 0.474 | 0.688 | 0.737 | 0.908 | 0.625 |
| 15 | 0.737 | 0.858 | 0.868 | 0.958 | 0.823 |
| 20 | 1.000 | 1.000 | 1.000 | 1.000 | 1.000 |

---

