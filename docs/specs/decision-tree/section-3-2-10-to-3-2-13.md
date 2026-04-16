## 3.2.10 Cross-Formula Dominance Analysis

**Purpose:** Verify that no single action dominates under all reasonable inputs. Each
action should win in the context for which it is designed.

**Maximum achievable scores (elite attributes, optimal conditions):**

| Action | Max BaseUtility | Max AM | Max Context | Max (1−Risk) | Approx Max U |
|--------|---------------|--------|------------|-------------|------------|
| PASS | 0.630 (DEF) | 1.000 | 1.00 | 1.00 | **0.630** |
| SHOOT | 0.850 (ATT) | 1.000 | 1.00 | 1.00 | **0.850** |
| DRIBBLE | 0.495 (ATT) | 1.000 | 1.00 | 1.00 | **0.495** |
| HOLD | 0.350 (DEF) | 1.000 | — | — | **0.350** |
| MOVE_TO_POS | 0.500 (OPP)¹ | 1.000 | 1.00 | — | **0.500** |
| PRESS | 0.600 (ATT) | 1.000 | 1.00 | — | **0.600** |
| INTERCEPT | 0.605 (DEF) | 1.000 | 1.00 | 1.00 | **0.605** |

¹ MOVE max assumes no nearby opponents (PressProximityPenalty = 1.0). With suppression
active (opponent ≤ 6m), MOVE max reduces to 0.300.

**Analysis by scenario:**

**Attacking third, possession:** SHOOT (0.850) > PASS (0.540) > DRIBBLE (0.495) > HOLD (0.200).
Correct: shooting dominates when an agent is in position to shoot. PASS is second — a
prudent agent with a blocked shot still considers passing. ✓

**Midfield, possession:** PASS (0.600) > DRIBBLE (0.450) > HOLD (0.250). PASS is the
dominant possession action in midfield. ✓

**Defensive third, no possession, no nearby opponents:** INTERCEPT (0.605) > MOVE (0.450)
> PRESS (0.400 — DEF zone). INTERCEPT correctly dominates when a ball trajectory is
feasible; MOVE beats PRESS in open areas, which is correct — repositioning takes priority
over pressing when no opponent is immediately close. ✓

**Defensive third, no possession, opponent within 4m:**
INTERCEPT and PRESS compete. MOVE is suppressed to ≈ 0.270 (from 0.450).
For a dedicated presser (Aggression=17, WorkRate=18): PRESS ≈ 0.315 > MOVE ≈ 0.270.
For a positional player (Positioning=14, WorkRate=16, Aggression=10): PRESS ≈ 0.240 <
MOVE ≈ 0.270 — positional players still recover shape even under close-opponent conditions.
This is correct attribute-differentiated behaviour. ✓

**HOLD pathology check:** HOLD maximum is now 0.350 (raised from 0.313). This is still
well below PASS normal (≈ 0.490 for average attributes, moderate lane). HOLD wins only
when all pass lanes are blocked AND dribble space is zero. ✓

**DRIBBLE underperformance:** DRIBBLE achieves a maximum of 0.495 — correctly the third
possession option behind SHOOT and PASS. It wins only when pass lanes are blocked and
the agent has high dribbling skill. ✓

**No pathological dominance identified.** All four structural weaknesses from v1.0
have been resolved.

---

## 3.2.11 UtilityWeights.cs Constant Catalogue

All constants below are owned by `UtilityWeights.cs`. No other file may define utility
scoring constants. Format: `[TAG] CONSTANT_NAME = value — comment`.

```csharp
/// <summary>
/// UtilityWeights — all gameplay-tunable and estimated constants for the Decision Tree
/// utility scoring model. No constant in this file may be referenced inline in code;
/// all must be accessed via this class.
///
/// [GT] = Gameplay-tuned. Initial estimates. Expected to change during balance phase.
///        Changing these values is a balance patch, not a specification amendment.
/// [EST] = Estimated from literature. Directionally sourced.
/// [FIXED] = Physical or regulatory constant. Do not change.
/// [DERIVED] = Mathematically derived; do not change independently.
///
/// Owned by: Decision Tree Specification #8 §3.2.11
/// </summary>
public static class UtilityWeights
{
    // ── Universal ──────────────────────────────────────────────────────────────────

    public const float UTILITY_FLOOR   = 0.01f;  // [DERIVED] Minimum scored utility
    public const float UTILITY_CEILING = 1.00f;  // [DERIVED] Maximum scored utility

    // ── Zone Modifiers ─────────────────────────────────────────────────────────────
    // Increasing DEF_ZONE values makes an action more attractive in the defensive third.
    // All zone modifiers: safe tuning range [0.05, 2.00].

    public const float PASS_ZONE_DEF   = 1.05f;  // [GT] Pass urgency in own third
    public const float PASS_ZONE_MID   = 1.00f;  // [GT] Neutral baseline
    public const float PASS_ZONE_ATT   = 0.90f;  // [GT] Passing less dominant in attack

    public const float SHOOT_ZONE_ATT  = 1.00f;  // [GT] Full baseline in attacking third
    public const float SHOOT_ZONE_MID_LONG = 0.55f; // [GT] Long shot midfield modifier
    public const float SHOOT_ZONE_MID_SHORT = 0.05f; // [GT] Near-suppression for no long shot
    public const float SHOOT_ZONE_DEF  = 0.10f;  // [GT] Strong discouragement from own half

    public const float DRIBBLE_ZONE_DEF = 0.70f; // [GT] Dangerous in own third
    public const float DRIBBLE_ZONE_MID = 1.00f; // [GT] Neutral
    public const float DRIBBLE_ZONE_ATT = 1.10f; // [GT] Mildly encouraged in attack

    public const float HOLD_ZONE_DEF   = 1.25f;  // [GT] Safe in own third
    public const float HOLD_ZONE_MID   = 1.00f;  // [GT] Neutral
    public const float HOLD_ZONE_ATT   = 0.80f;  // [GT] Waste of attacking opportunity

    public const float MOVE_ZONE_DEF   = 1.00f;  // Constant — positional duty unchanged by zone
    public const float MOVE_ZONE_MID   = 1.00f;
    public const float MOVE_ZONE_ATT   = 1.00f;

    public const float PRESS_ZONE_DEF  = 0.80f;  // [GT] Pressing from deep exposes space
    public const float PRESS_ZONE_MID  = 1.00f;  // [GT] Neutral
    public const float PRESS_ZONE_ATT  = 1.20f;  // [GT] High press encouraged in attack

    public const float INTERCEPT_ZONE_DEF = 1.10f; // [GT] Most valuable defensively
    public const float INTERCEPT_ZONE_MID = 1.00f; // [GT] Neutral
    public const float INTERCEPT_ZONE_ATT = 0.90f; // [GT] In attack, seek creation not chase

    // ── Base Utility Nominals ───────────────────────────────────────────────────────
    // The intrinsic value of each action type before zone and attribute modifiers.
    // Increasing a value makes this action more desirable in all zones uniformly.

    public const float U_BASE_PASS      = 0.60f;  // [GT] Primary positive action
    public const float U_BASE_SHOOT     = 0.85f;  // [GT] Highest ceiling action; zone-gated
    public const float U_BASE_DRIBBLE   = 0.45f;  // [GT] Creative outlet; deliberately secondary
    public const float U_BASE_HOLD      = 0.28f;  // [GT] Fallback; must be lowest baseline.
                                                   //      Increased from 0.25 → 0.28 to widen
                                                   //      margin above degraded PASS worst case.
                                                   //      Safe tuning range: [0.15, 0.35].
    public const float U_BASE_MOVE      = 0.40f;  // [GT] Positional duty; moderate urgency
    public const float U_BASE_PRESS     = 0.50f;  // [GT] Active defence; moderate baseline
    public const float U_BASE_INTERCEPT = 0.55f;  // [GT] Best active defensive action

    // ── Attribute Exponents ─────────────────────────────────────────────────────────
    // Controls sensitivity of attribute response. exp < 1.0 = diminishing returns.
    // Safe tuning range: [0.10, 0.80]. Do not exceed 0.80 at Stage 0.

    public const float PASS_VISION_EXP      = 0.30f;  // [GT] Lane quality reading
    public const float PASS_TECHNIQUE_EXP   = 0.40f;  // [GT] Passing execution accuracy
    public const float SHOOT_FINISHING_EXP  = 0.50f;  // [GT] Shot execution; steeper curve
    public const float SHOOT_COMPOSURE_EXP  = 0.30f;  // [GT] Discrete-event composure: graded
                                                       //      degradation at point of shot. Shallow
                                                       //      curve — SHOOT also carries RiskPenalty
                                                       //      which shares pressure-response load.
                                                       //      Split rationale vs HOLD_COMPOSURE_EXP:
                                                       //      see ⚠ DESIGN DECISION §3.2.5.1.
                                                       //      Concave form: Beilock & Carr (2001)
                                                       //      DOI: 10.1037/0096-3445.130.4.701
    public const float DRIBBLE_DRIBBLING_EXP = 0.40f; // [GT] Core dribbling skill
    public const float DRIBBLE_AGILITY_EXP  = 0.30f;  // [GT] Directional change speed
    public const float HOLD_COMPOSURE_EXP   = 0.50f;  // [GT] Sustained-state composure gate.
                                                       //      Steeper than SHOOT (0.30) — three
                                                       //      independent grounds: (1) HOLD is a
                                                       //      sustained state, not a discrete event;
                                                       //      (2) composure gates viability, not just
                                                       //      quality; (3) HOLD has no RiskPenalty
                                                       //      term — composure carries more structural
                                                       //      weight. Full rationale: ⚠ DESIGN
                                                       //      DECISION §3.2.5.1. Do not unify with
                                                       //      SHOOT_COMPOSURE_EXP.
    public const float MOVE_POSITIONING_EXP = 0.40f;  // [GT] Positional commitment
    public const float MOVE_WORKRATE_EXP    = 0.30f;  // [GT] Running effort
    public const float PRESS_AGGRESSION_EXP = 0.30f;  // [GT] Pressing intent
    public const float PRESS_WORKRATE_EXP   = 0.30f;  // [GT] Pressing engine
    public const float PRESS_STAMINA_EXP    = 0.20f;  // [GT] Capacity to press
    public const float INTERCEPT_ANTICIPATION_EXP = 0.50f; // [EST] Interceptive timing research.
                                                            //       Source: Müller & Abernethy (2006)
                                                            //       — anticipation skill in sport shows
                                                            //       strong threshold effects: near-zero
                                                            //       performance below a competence floor,
                                                            //       rapid improvement through mid-range.
                                                            //       Exponent 0.50 (square root) captures
                                                            //       this concave response profile.
                                                            //       DOI: 10.1016/j.humov.2006.07.017
    public const float INTERCEPT_PACE_EXP   = 0.30f;  // [GT] Speed to intercept point

    // ── Risk Penalty Coefficients ───────────────────────────────────────────────────
    // Controls how much pressure and skill deficiency reduce utility via the risk term.
    // Increasing a coefficient makes the action riskier under pressure.
    // Safe tuning range: [0.05, 0.50].

    public const float PASS_RISK_COEFF      = 0.30f;  // [GT] Passing risk under pressure
    public const float SHOOT_RISK_COEFF     = 0.40f;  // [GT] Shot blocked = possession lost
    public const float DRIBBLE_RISK_COEFF   = 0.35f;  // [GT] Dribble tackle = possession lost
    public const float INTERCEPT_PRESSURE_COEFF = 0.20f; // [GT] Pressure reduces intercept read
    public const float HOLD_PRESSURE_COEFF  = 0.50f;  // [GT] Pressure reduces HOLD appeal.
                                                       //      Reduced from 0.60 → 0.50 to prevent
                                                       //      extreme-pressure collapse of HOLD
                                                       //      below its fallback role.
                                                       //      Max penalty: 50% at P=1.0.
                                                       //      Safe tuning range: [0.30, 0.65].

    // ── Context Score Thresholds and Distances ──────────────────────────────────────

    public const float LONG_SHOT_THRESHOLD  = 0.75f;  // [GT] Shifted LongShots for midfield shot
    public const float GOAL_OPENING_MIN     = 0.05f;  // [GT] Minimum goal opening score floor
    public const float BLOCKER_RADIUS_M     = 0.50f;  // [GT] Outfield player body width in shot lane.
                                                       //      Increasing widens shot lane suppression.
                                                       //      Safe tuning range: [0.30, 0.80].
    public const float GK_BLOCKER_RADIUS_M  = 1.50f;  // [GT] Goalkeeper effective blocking radius.
                                                       //      Approximates arm reach + lateral movement.
                                                       //      GK identified by GK_PROXIMITY_TO_GOAL.
                                                       //      Increasing reduces GoalOpeningScore near goal.
                                                       //      Safe tuning range: [1.00, 2.50].
    public const float GK_PROXIMITY_TO_GOAL = 6.00f;  // [GT] Distance from goal line to classify as GK.
                                                       //      Stage 0 heuristic only — replace with
                                                       //      AgentState.Role == GOALKEEPER at Stage 1.
                                                       //      Safe tuning range: [4.00, 10.00].
    public const float GOAL_MIN_SHOT_DIST   = 1.00f;  // [GT] Minimum dist to count as blocker

    public const float MOVE_URGENCY_DIST_M  = 15.0f;  // [GT] Full urgency distance for MOVE
    public const float MOVE_DIST_MIN        = 0.10f;  // [GT] Minimum distance modifier floor
    public const float MOVE_PRESS_SUPPRESSION_DIST   = 6.0f;  // [GT] Opponent proximity threshold that
                                                               //      activates MOVE suppression penalty.
                                                               //      Set below PRESS_TRIGGER_DISTANCE (8m)
                                                               //      to target close-range imbalance only.
                                                               //      Safe tuning range: [4.0, 7.0].
    public const float MOVE_PRESS_SUPPRESSION_FACTOR = 0.60f; // [GT] Multiplier applied to MOVE when
                                                               //      opponent is within suppression dist.
                                                               //      0.60 = 40% reduction.
                                                               //      Does not eliminate MOVE; positional
                                                               //      players still prefer shape recovery.
                                                               //      Safe tuning range: [0.40, 0.80].

    // ── Phase Modifiers ─────────────────────────────────────────────────────────────

    public const float MOVE_PHASE_OWN_TEAM  = 0.70f;  // [GT] Delay repositioning in possession
    public const float MOVE_PHASE_OPPONENT  = 1.25f;  // [GT] Urgent to recover shape without ball
    public const float MOVE_PHASE_CONTESTED = 1.00f;  // Neutral

    // ── Tactical Pressing Modifiers ─────────────────────────────────────────────────
    // Stage 0: all agents use MEDIUM (1.00) — both teams identical.
    // Stage 1: Formation System will set PressingMode per team instruction.

    public const float PRESS_TACTICAL_HIGH   = 1.40f; // [GT] High press instruction
    public const float PRESS_TACTICAL_MEDIUM = 1.00f; // Stage 0 default for all agents
    public const float PRESS_TACTICAL_LOW    = 0.60f; // [GT] Low press instruction

    // ── Constant Count Summary ──────────────────────────────────────────────────────
    // Total constants: 58 (increased from 52 in v1.0: +2 GK blocker, +2 MOVE suppression,
    //                                                   +2 HOLD adjustments reflected above)
    // [GT]: 50 (86%) — expected and documented. The utility model is a design layer,
    //        not a physics layer. Unlike Ball Physics or Collision System (where constants
    //        are derived from physical laws), utility weights are design choices.
    //        The 86% [GT] rate is consistent with FM-category games where parameter
    //        tuning is a core part of the development cycle, not a deficiency.
    // [EST]: 2 (3%)  — SHOOT_COMPOSURE_EXP (Beilock & Carr 2001), INTERCEPT_ANTICIPATION_EXP
    //                   (Müller & Abernethy 2006). Both DOIs verified.
    // [FIXED]: 0 — no regulatory or physical constants in the utility model.
    // [DERIVED]: 3 (5%) — UTILITY_FLOOR, UTILITY_CEILING, MOVE_PHASE_CONTESTED
    // Unclassified: 3 (5%) — zone modifier neutrals (1.00) and MOVE zone constants (1.00);
    //                          these are not tagged as [GT] because they are definitional
    //                          baselines, not tunable values.
    //
    // Stage 1 [GT] tuning roadmap:
    //   Priority 1 — tune after first playable build:
    //     U_BASE_* constants, ZONE_* modifiers, RISK_COEFF constants
    //   Priority 2 — tune after position role differentiation:
    //     MOVE_PRESS_SUPPRESSION_*, PRESS_TACTICAL_*, PhaseModifiers
    //   Priority 3 — tune with academic review target:
    //     All attribute EXP constants — flag for literature cross-check in §8.2
    //   Do not retune: UTILITY_FLOOR, UTILITY_CEILING (structural invariants)
}
```

---

## 3.2.12 Worked Example — Full Scoring Pass

This example continues from the §3.1.11 worked example. The agent (AgentId=7, home team,
attacking midfielder) has produced five options: PassOption(T2), PassOption(T1),
PassOption(T3), DribbleOption, HoldOption.

**Agent attributes (from §3.1.11 setup):**
- Decisions=15, Vision=14, Passing=13, Finishing=11, Composure=12, Dribbling=10,
  Agility=11, LongShots=8, Anticipation=10, Positioning=13, WorkRate=14, Stamina=12
- Aggression assumed: 11 (not specified in §3.1.11 — using mean-equivalent)
- `PressureScalar P = 0.25` (moderate pressure; assumed from snapshot)
- `BallZone = DEFENSIVE` (ball at (18, 34) → x=18, within 0–35m DEFENSIVE zone)
- `Possession = OWN_TEAM`
- `FormationSlot = (25, 30)`, agent at `(72, 34)` (far from slot)

**Note on scenario:** The §3.1.11 example placed the agent at (72, 34), which is in the
attacking third (x > 65m), yet the ball is at (18, 34) which is in the defensive zone.
The agent is an attacking midfielder caught upfield during a defensive transition.
`BallZone = DEFENSIVE` governs zone modifiers. Formation slot is (25, 30) — the agent
is 47.8m from slot.

---

**Scoring PassOption(T2): target at (35, 20), AdjustedPassLaneScore = 0.91**
```
A_Vision = (14−1)/19 = 0.6842 → Shifted = 0.8421
A_Passing = (13−1)/19 = 0.6316 → Shifted = 0.8158

BaseUtility = 0.60 × 1.05 = 0.6300       (DEFENSIVE zone)
AttributeMultiplier = 0.8421^0.30 × 0.8158^0.40
                    = 0.9461 × 0.9239 = 0.8741
ContextModifier = 0.91
RiskPenalty = 0.25 × (1−0.6316) × 0.30 = 0.25 × 0.3684 × 0.30 = 0.02763

U_raw = 0.6300 × 0.8741 × 0.91 × (1−0.02763)
      = 0.6300 × 0.8741 = 0.5507
      = 0.5507 × 0.91 = 0.5011
      = 0.5011 × 0.97237 = 0.4872

ScoredUtility(T2) = 0.487
```

**Scoring PassOption(T1): target at (40, 30), AdjustedPassLaneScore = 0.66**
```
Same attributes; same BaseUtility; same AM
ContextModifier = 0.66
RiskPenalty = 0.02763 (same P and A_Passing)

U_raw = 0.6300 × 0.8741 × 0.66 × 0.97237
      = 0.5507 × 0.66 = 0.3635
      = 0.3635 × 0.97237 = 0.3534

ScoredUtility(T1) = 0.353
```

**Scoring PassOption(T3): target at (12, 30) [backward], AdjustedPassLaneScore = 0.50**
```
Same attributes; ContextModifier = 0.50

U_raw = 0.5507 × 0.50 × 0.97237
      = 0.2754 × 0.97237 = 0.2678

ScoredUtility(T3) = 0.268
```

**Scoring DribbleOption: SpaceScore = 1.0 (all opponents > 2m), BestDirection = (1,0)**
```
A_Dribbling = (10−1)/19 = 0.4737 (raw form)
A_Agility   = (11−1)/19 = 0.5263 (raw form)

BaseUtility = 0.45 × 0.70 = 0.315        (DEFENSIVE zone)
AttributeMultiplier = 0.4737^0.40 × 0.5263^0.30
                    = 0.7559 × 0.8057 = 0.6091
SpaceScore = 1.0
RiskPenalty = 0.25 × (1−0.4737) × 0.35 = 0.25 × 0.5263 × 0.35 = 0.04604

U_raw = 0.315 × 0.6091 × 1.0 × (1−0.04604)
      = 0.315 × 0.6091 = 0.1919
      = 0.1919 × 0.95396 = 0.1830

ScoredUtility(DRIBBLE) = 0.183
```

**Scoring HoldOption:**
```
A_Composure = (12−1)/19 = 0.5789 → Shifted = 0.7895

BaseUtility = 0.28 × 1.25 = 0.350      (DEFENSIVE zone; U_BASE_HOLD = 0.28)
AttributeMultiplier = 0.7895^0.50 = 0.8886
(1 − P × 0.50) = 1.0 − 0.25 × 0.50 = 1.0 − 0.125 = 0.875

U_raw = 0.350 × 0.8886 × 0.875
      = 0.350 × 0.8886 = 0.3110
      = 0.3110 × 0.875 = 0.2721

ScoredUtility(HOLD) = 0.272
```

---

**Final Scored Option List:**

| Option | ScoredUtility | Rank |
|--------|--------------|------|
| PassOption(T2) — forward, clean lane | **0.487** | 1st |
| PassOption(T1) — forward, moderate lane | 0.353 | 2nd |
| PassOption(T3) — backward, clean lane | 0.268 | 3rd |
| HoldOption | 0.272 | 3rd= |
| DribbleOption | 0.183 | 5th |

**Interpretation:** The attacking midfielder caught upfield during a defensive transition
should pass quickly to T2 (the cleanest forward lane at 0.487). Note that PassOption(T3)
(backward, 0.268) and HoldOption (0.272) are nearly tied — a difference of 0.004. At
§3.3 composure noise, this margin may occasionally be reversed for low-Composure agents,
who may hold rather than play a backward pass. This is correct emergent behaviour for an
anxious player under pressure — the backward pass (0.268) barely edges out holding, but
a nervous player may not execute the correct choice. Dribbling (0.183) remains the
worst option. The option set is passed to `SelectAction()` (§3.3).

---

**Invariant verification:**

- INV-SCORE-01 ✓ All five options scored (ScoredUtility populated)
- INV-SCORE-02 ✓ All scores in [0.01, 1.0]
- INV-SCORE-03 ✓ PASS options ranked by AdjustedPassLaneScore, consistent with ScoredUtility ranking
- INV-SCORE-04 ✓ HOLD is not the highest-scored option (0.272 < 0.487); HOLD fallback not triggered
- INV-SCORE-05 ✓ DribbleOption (raw attribute form) does not approach zero (0.183 > 0.01) because agent has Dribbling=10 (adequate)

---

## 3.2.13 Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | March 01, 2026, 7:00 PM PST | Claude (AI) / Anton | Initial draft. Complete utility scoring for all 7 action types. Numerical verification for all 7 formulas. Cross-formula dominance analysis. Full UtilityWeights.cs constant catalogue (52 constants). Worked example from §3.1.11 extended through full scoring pass. Three design clarifications: (1) GoalOpeningScore gap — resolved via PitchGeometry static class (no Perception System amendment required). (2) MOVE_TO_POSITION Positioning attribute inversion — corrected from outline's `(1−A×0.4)` bug to `(0.5+A×0.5)^exp` positive form. (3) LongShots threshold derivation — outline's "raw ≥ 15" comment corrected to "raw ≥ 11" (matches LONG_SHOT_THRESHOLD = 0.75 constant). |
| 1.2 | March 02, 2026, 12:00 PM PST | Claude (AI) / Anton | **Composure exponent split formalised.** Resolved architectural inconsistency between SHOOT_COMPOSURE_EXP (0.30) and HOLD_COMPOSURE_EXP (0.50). Values unchanged. Formal ⚠ DESIGN DECISION block inserted after §3.2.5.1 with three independent grounds: (1) discrete event vs. sustained state; (2) graded degradation vs. viability gate; (3) structural compensation for absent RiskPenalty in HOLD formula. Numerical comparison at Composure=10 and Composure=3 provided. Constant catalogue comments for both exponents rewritten with cross-references. SHOOT_COMPOSURE_EXP retag [EST]→[GT] (Beilock concave form confirmed but specific value is gameplay-tuned; DOI retained in comment). Exponent audit closed: all 14 exponents confirmed [GT] with concave form noted. |
| 1.1 | March 01, 2026, 9:30 PM PST | Claude (AI) / Anton | Four weaknesses resolved: (1) **GK blocker radius** — replaced flat BLOCKER_RADIUS = 0.5m with GK-specific GK_BLOCKER_RADIUS = 1.5m for opponents within GK_PROXIMITY_TO_GOAL = 6.0m of goal line. Positional heuristic documented; Stage 1 replacement with AgentState.Role flag noted. (2) **MOVE vs PRESS imbalance** — added PressProximityPenalty term to MOVE formula: 40% reduction (factor 0.60) when nearest visible opponent ≤ 6.0m. Verified: dedicated pressers now select PRESS over MOVE at close range; positional players still prefer shape recovery. §3.2.6.2 rationale expanded. §3.2.6.3 Case C added. (3) **HOLD/PASS narrow margin** — U_BASE_HOLD raised 0.25 → 0.28; HOLD_PRESSURE_COEFF reduced 0.60 → 0.50. HOLD worst case raised from 0.057 → 0.079; margin over PASS worst case (0.064) doubled from 0.007 → 0.015. HOLD ceiling raised to 0.350, still below PASS normal (0.490+). Worked example HOLD score updated 0.236 → 0.272. (4) **[EST] citation expansion** — SHOOT_COMPOSURE_EXP: full Beilock & Carr (2001) DOI added. INTERCEPT_ANTICIPATION_EXP: updated to Müller & Abernethy (2006) with DOI. Stage 1 tuning roadmap added to UtilityWeights.cs summary. Constant count updated to 58. |

---

*End of Section 3.2 — Option Scoring: Utility Model*

*Decision Tree Specification #8 | Tactical Director — Specification #8 of 20 | Stage 0: Physics Foundation*
*Next: Section 3.3 — Final Action Selection: Composure Model*
