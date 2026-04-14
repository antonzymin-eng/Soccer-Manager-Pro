# First Touch Mechanics Specification #4 â€” Section 5: Testing

**Purpose:** Defines all test requirements, acceptance criteria, and validation methodology for
the First Touch Mechanics system (Spec #4). Establishes unit tests, integration tests, and
real-world validation scenarios to verify correctness of the Control Quality formula, Touch
Radius mapping, Ball Displacement model, Possession State Machine, Pressure Evaluation, and
Body Orientation Detection. This section is the authoritative test reference for Stage 0
implementation sign-off.

**Created:** February 18, 2026, 3:00 PM PST
**Updated:** February 22, 2026, 12:00 AM PST
**Version:** 1.3
**Status:** Approved â€” VS-001 radius corrected (v1.2)
**Author:** Claude (AI) with Anton (Lead Developer)
**Specification Number:** 4 of 20 (Stage 0 Physics Foundation)
**Prerequisites:** Section 1 (v1.0), Section 2 (v1.0), Section 3 (v1.1), Section 4 (v1.1)

**Changelog:**
- v1.3 (March 25, 2026): MOD-04 audit fix — subsection numbering normalised.
  §5.10.x subsections renumbered to §5.11.x (under §5.11 Acceptance Criteria Summary);
  §5.11.x subsections renumbered to §5.12.x (under §5.12 Test Execution Plan).
  Section 6 cross-reference updated from §5.11.4 to §5.11.4.
- v1.2 (February 22, 2026): VS-001 hand-calc corrected -- r updated from 0.195m to 0.428m.
  The v1.1 hand-calc omitted the velocity modifier (+0.233m at 14 m/s). The possession
  outcome (CONTROLLED) was correct because r = 0.428m is within CONTROLLED_RADIUS (0.60m).
  Only the stated radius value was wrong. Expected output tolerance updated to match.
  Discovered by Appendix B numerical verification (First_Touch_Spec_Appendices_v1_0.md).
- v1.1 (February 18, 2026): BD category added (Ball Displacement unit tests BD-001 to BD-008).

---

## Table of Contents

- [5.1 Test Strategy](#51-test-strategy)
- [5.2 Unit Tests â€” Control Quality (CQ)](#52-unit-tests--control-quality-cq)
- [5.3 Unit Tests â€” Touch Radius (TR)](#53-unit-tests--touch-radius-tr)
- [5.4 Unit Tests â€” Pressure Evaluation (PR)](#54-unit-tests--pressure-evaluation-pr)
- [5.5 Unit Tests â€” Body Orientation (OR)](#55-unit-tests--body-orientation-or)
- [5.6 Unit Tests â€” Possession State Machine (PO)](#56-unit-tests--possession-state-machine-po)
- [5.7 Unit Tests â€” Edge Cases and Robustness (EC)](#57-unit-tests--edge-cases-and-robustness-ec)
- [5.8 Unit Tests â€” Ball Displacement (BD)](#58-unit-tests--ball-displacement-bd)
- [5.9 Integration Tests (IT)](#59-integration-tests-it)
- [5.10 Real-World Validation Scenarios (VS)](#510-real-world-validation-scenarios-vs)
- [5.11 Acceptance Criteria Summary](#511-acceptance-criteria-summary)
- [5.12 Test Execution Plan](#512-test-execution-plan)
- [5.13 Section Summary](#513-section-summary)

---

## 5.1 Test Strategy

### 5.1.1 Test Pyramid

```
        /\
       /  \     E2E Tests (5%)
      /____\    - Full 90-minute match simulation
     /      \   - Replay determinism verification
    /        \
   / Integr-  \ Integration Tests (25%)
  /  ation     \ - Cross-system boundary scenarios
 /______________\ - First Touch â†” Ball Physics, Agent Movement, Collision
/                \
/   Unit Tests    \ Unit Tests (70%)
/__(31+ tests)____\ - Formula validation, edge cases, boundary conditions
```

**Distribution rationale:**

Unit tests are fast (<10s total) and enable rapid iteration during TDD. Every formula term,
every constant, and every branch condition in Sections 3.1â€“3.6 has a corresponding unit test.
Integration tests verify that correct outputs flow between systems as specified in Section 4's
interface contracts. E2E tests run pre-milestone only; they confirm no NaN propagation,
stable memory, and deterministic replay across a full simulated match.

### 5.1.2 Testing Tools

| Tool | Purpose | Configuration |
|---|---|---|
| **Unity Test Framework (NUnit)** | Test runner | Edit Mode for pure logic; Play Mode for integration |
| **Unity Code Coverage** | Line coverage | Target: > 85% for Sections 3.1â€“3.6 code |
| **Unity Profiler** | Performance validation | p95 < 0.05ms per touch evaluation |
| **Custom DeterministicTestHarness** | Replay determinism | Fixed inputs; bitwise comparison of outputs |

### 5.1.3 Test Data Strategy

**Derived values, not magic numbers.** Every expected output in this section is computed from
the formulas defined in Sections 3.1â€“3.6 using the constants in Section 4. Test comments
include the derivation or reference the step in the formula. Appendix B of this specification
contains full hand-calculation worksheets for all scenarios marked `[HAND-CALC]`.

**Formula reference constants used throughout this section:**

| Constant | Value | Source |
|---|---|---|
| `TECHNIQUE_WEIGHT` | 0.70 | Â§3.1.2 |
| `FIRST_TOUCH_WEIGHT` | 0.30 | Â§3.1.2 |
| `ATTR_MAX` | 20.0 | Â§3.1.2 |
| `VELOCITY_REFERENCE` | 15.0 m/s | Â§3.1.2 |
| `VELOCITY_MAX_FACTOR` | 4.0 | Â§3.1.2 |
| `MOVEMENT_REFERENCE` | 7.0 m/s | Â§3.1.2 |
| `MOVEMENT_PENALTY` | 0.5 | Â§3.1.2 |
| `PRESSURE_WEIGHT` | 0.40 | Â§3.1.2 |
| `PRESSURE_RADIUS` | 3.0 m | Â§3.5.1 |
| `MIN_PRESSURE_DISTANCE` | 0.3 m | Â§3.5.2 |
| `PRESSURE_SATURATION` | 1.5 | Â§3.5.3 |
| `HALF_TURN_ANGLE_MIN` | 30Â° | Â§3.6.2 |
| `HALF_TURN_ANGLE_MAX` | 60Â° | Â§3.6.2 |
| `HALF_TURN_BONUS` | 0.15 | Â§3.6.3 |
| `CONTROLLED_THRESHOLD` | 0.55 q | Â§3.4.2 |
| `LOOSE_BALL_THRESHOLD` | 0.60 m radius | Â§3.4.2 |
| `INTERCEPTION_THRESHOLD` | 1.20 m radius | Â§3.4.2 |
| `INTERCEPTION_RADIUS` | 2.50 m | Â§3.4.2 |
| `DEFLECTION_THRESHOLD` | 1.50 m radius | Â§3.4.2 |
| `DEFLECTION_ALIGNMENT_MIN` | 0.70 | Â§3.4.2 |

### 5.1.4 Tolerance Rationale

All tolerances in this section are derived from the precision requirements of the system,
not chosen arbitrarily.

**Control quality tolerances (Â±0.02 â€“ Â±0.05):** The formula involves floating-point division
and multiplication across 8 steps. Accumulated IEEE 754 rounding is bounded by approximately
Â±0.001 per arithmetic operation. Over 8 steps, Â±0.01 is the practical floor. Tolerances of
Â±0.02â€“Â±0.05 provide a 2â€“5Ã— safety margin over the numerical floor while remaining tight
enough to detect implementation errors. Tight tolerances (Â±0.02) are applied to simpler
sub-formula tests; wider tolerances (Â±0.05) apply to full-formula tests with more terms.

**Touch radius tolerances (Â±0.02 â€“ Â±0.10m):** Piecewise linear interpolation introduces
no additional floating-point error beyond the quality input. The Â±0.05m tolerance at band
midpoints is one-sixth of the band width; this is tight enough to catch wrong-band errors
(which would produce errors of 0.30m+) while accommodating floating-point input variation.
Wider tolerance (Â±0.10m) applies to the velocity modifier tests where two multiplications
are involved.

**Pressure scalar tolerances (Â±0.02 â€“ Â±0.10):** The inverse-square formula is nonlinear;
a 5 cm position error in opponent placement produces approximately Â±0.02 scalar variation
at 1.0m distance. Tolerances are set to 2Ã— this sensitivity.

---

## 5.2 Unit Tests â€” Control Quality (CQ)

These tests exercise `CalculateControlQuality()` from Â§3.1.4. Each test fixes all variables
except those under scrutiny, then verifies the output against a hand-calculated expected value.

---

**Test ID:** CQ-001
**Name:** `ControlQuality_ElitePlayer_SlowBall_NoConditions_ReturnsNearPerfect`
**Purpose:** Verify that an elite agent receiving a slow ball with no modifiers produces
quality approaching 1.0. Validates the happy path and that the formula does not over-cap.
**[HAND-CALC]**

```
Step 1: WeightedAttr = (17 Ã— 0.70) + (18 Ã— 0.30) = 11.90 + 5.40 = 17.30
Step 2: NormAttr = 17.30 / 20.0 = 0.865
Step 3: AttrWithBonus = 0.865 Ã— (1.0 + 0.0) = 0.865   [no half-turn]
Step 4: VelDifficulty = 12.0 / 15.0 = 0.800            [clamped: in range]
Step 5: MoveDifficulty = 1.0 + (0.0 / 7.0) Ã— 0.5 = 1.000
Step 6: RawQuality = 0.865 / (0.800 Ã— 1.000) = 1.081
Step 7: q = 1.081 Ã— (1.0 - 0.0 Ã— 0.40) = 1.081
Step 8: q = Clamp(1.081, 0, 1) = 1.000
```

**Inputs:** Technique=17, FirstTouch=18, ballSpeed=12.0 m/s, agentSpeed=0.0, pressure=0.0,
orientationBonus=0.0
**Expected:** q = 1.00 (clamped)
**Tolerance:** Exact (clamped output)
**Failure interpretation:** If q < 1.00, the formula is under-computing in an unconstrained
elite scenario. Most likely cause: wrong constant for ATTR_MAX or VELOCITY_REFERENCE.

---

**Test ID:** CQ-002
**Name:** `ControlQuality_AveragePlayer_TypicalPass_Walking_LowPressure_ReturnsMidRange`
**Purpose:** Validates the "average professional" baseline scenario. Output should sit in
the middle range where CONTROLLED / LOOSE_BALL boundary decisions are meaningful.
**[HAND-CALC]**

```
Step 1: WeightedAttr = (12 Ã— 0.70) + (11 Ã— 0.30) = 8.40 + 3.30 = 11.70
Step 2: NormAttr = 11.70 / 20.0 = 0.585
Step 3: AttrWithBonus = 0.585 Ã— 1.0 = 0.585            [no half-turn]
Step 4: VelDifficulty = 15.0 / 15.0 = 1.000
Step 5: MoveDifficulty = 1.0 + (2.0 / 7.0) Ã— 0.5 = 1.0 + 0.143 = 1.143
Step 6: RawQuality = 0.585 / (1.000 Ã— 1.143) = 0.512
Step 7: q = 0.512 Ã— (1.0 - 0.15 Ã— 0.40) = 0.512 Ã— 0.940 = 0.481
Step 8: q = Clamp(0.481, 0, 1) = 0.481
```

**Inputs:** Technique=12, FirstTouch=11, ballSpeed=15.0 m/s, agentSpeed=2.0, pressure=0.15,
orientationBonus=0.0
**Expected:** q = 0.481 Â± 0.03
**Tolerance:** Â±0.03
**Failure interpretation:** If q > 0.55, the average player scenario incorrectly yields
CONTROLLED on a difficult pass. Check pressure application and movement difficulty steps.

---

**Test ID:** CQ-003
**Name:** `ControlQuality_PoorPlayer_FastPass_Jogging_MediumPressure_ReturnsLow`
**Purpose:** Validates that a poor technical player in adverse conditions produces very
low quality, triggering LOOSE_BALL or worse. Ensures the formula's denominator compounds
correctly when multiple difficulty terms are elevated.
**[HAND-CALC]**

```
Step 1: WeightedAttr = (7 Ã— 0.70) + (6 Ã— 0.30) = 4.90 + 1.80 = 6.70
Step 2: NormAttr = 6.70 / 20.0 = 0.335
Step 3: AttrWithBonus = 0.335 Ã— 1.0 = 0.335
Step 4: VelDifficulty = 22.0 / 15.0 = 1.467            [clamped: in range]
Step 5: MoveDifficulty = 1.0 + (4.0 / 7.0) Ã— 0.5 = 1.0 + 0.286 = 1.286
Step 6: RawQuality = 0.335 / (1.467 Ã— 1.286) = 0.335 / 1.887 = 0.178
Step 7: q = 0.178 Ã— (1.0 - 0.45 Ã— 0.40) = 0.178 Ã— 0.820 = 0.146
Step 8: q = Clamp(0.146, 0, 1) = 0.146
```

**Inputs:** Technique=7, FirstTouch=6, ballSpeed=22.0 m/s, agentSpeed=4.0, pressure=0.45,
orientationBonus=0.0
**Expected:** q = 0.146 Â± 0.03
**Tolerance:** Â±0.03
**Failure interpretation:** If q > 0.35, the heavy-touch scenario is miscalculated. Most
likely cause: pressure not being applied multiplicatively, or movement difficulty over-applied.

---

**Test ID:** CQ-004
**Name:** `ControlQuality_VelocityDifficulty_CappedAtMaxFactor`
**Purpose:** Verifies that balls faster than VELOCITY_MAX_FACTOR Ã— VELOCITY_REFERENCE
(i.e., faster than 60 m/s) do not produce unbounded difficulty. The cap at 4.0 must hold.
**[HAND-CALC]**

```
Step 4: VelDifficulty = 80.0 / 15.0 = 5.333 â†’ clamped to 4.0 (VELOCITY_MAX_FACTOR)
        (all other steps identical to a 60 m/s ball)
```

**Inputs:** Technique=12, FirstTouch=11, ballSpeed=80.0 m/s, agentSpeed=0.0, pressure=0.0,
orientationBonus=0.0
**Comparison input:** ballSpeed=60.0 m/s (same parameters)
**Expected:** q(80 m/s) == q(60 m/s) â€” output is identical, proving cap activates
**Tolerance:** Exact (both should equal the capped-denominator result)
**Failure interpretation:** If q(80 m/s) < q(60 m/s), the velocity cap is not being applied.

---

**Test ID:** CQ-005
**Name:** `ControlQuality_HalfTurnBonus_IncreasesQuality_By15Percent`
**Purpose:** Isolates the orientation bonus term. Compares identical scenarios with and
without half-turn, verifies multiplicative application of the 0.15 bonus factor.
**[HAND-CALC]**

```
Baseline (no bonus): NormAttr = 0.600 (Tech=12, FT=12)
  AttrWithBonus = 0.600 Ã— 1.0 = 0.600
  With bonus:    AttrWithBonus = 0.600 Ã— 1.15 = 0.690
  Ratio = 0.690 / 0.600 = 1.150 (exactly 15% more)
  This ratio propagates through unchanged to final q since bonus is applied pre-denominator.
```

**Inputs (no bonus):** Technique=12, FirstTouch=12, ballSpeed=15.0, agentSpeed=0.0,
pressure=0.0, orientationBonus=0.0
**Inputs (with bonus):** Same + orientationBonus=0.15
**Expected:** q_halfTurn / q_noHalfTurn â‰ˆ 1.15 (before clamping)
**Tolerance:** Â±0.02 on the ratio
**Failure interpretation:** Ratio < 1.13 or > 1.17 indicates the bonus is not being applied
correctly. Ratio == 1.0 means bonus is ignored entirely.

---

**Test ID:** CQ-006
**Name:** `ControlQuality_MaximumPressure_DegradesByExactly40Percent`
**Purpose:** Verifies the pressure degradation step. At pressureScalar = 1.0 and
PRESSURE_WEIGHT = 0.40, quality must be reduced by exactly 40% of RawQuality.
**[HAND-CALC]**

```
RawQuality (no pressure) = some value R
q_noPressure  = R Ã— (1.0 - 0.0 Ã— 0.40) = R
q_maxPressure = R Ã— (1.0 - 1.0 Ã— 0.40) = R Ã— 0.60
Ratio = 0.60 (60% remaining; 40% degraded)
```

**Inputs (base):** Technique=15, FirstTouch=14, ballSpeed=15.0, agentSpeed=0.0, pressure=0.0,
orientationBonus=0.0
**Inputs (max pressure):** Same + pressure=1.0
**Expected:** q_maxPressure = q_noPressure Ã— 0.60
**Tolerance:** Â±0.001 (this is a pure multiplication; virtually no floating-point error)
**Failure interpretation:** Any deviation beyond Â±0.001 indicates the pressure step formula
has a constant error (e.g., PRESSURE_WEIGHT hard-coded incorrectly).

---

**Test ID:** CQ-007
**Name:** `ControlQuality_ZeroPressure_NoPenaltyApplied`
**Purpose:** Confirms that zero pressure produces no modification to RawQuality. This is
the open-space reception case and is a precondition for several other tests.

**Inputs:** Technique=12, FirstTouch=11, ballSpeed=15.0, agentSpeed=2.0, pressure=0.0,
orientationBonus=0.0
**Expected:** q = RawQuality Ã— 1.0 (pressure term is inert)
**Verification method:** Compute q manually; confirm pressure=0.0 and pressure=0.001 produce
identical results within floating-point precision.
**Tolerance:** Exact at pressure=0.0
**Failure interpretation:** Non-zero result change at pressure=0.0 indicates a constant
offset error in the pressure formula.

---

**Test ID:** CQ-008
**Name:** `ControlQuality_MinimumAttributes_ProducesNonZeroOutput`
**Purpose:** Verifies that the absolute minimum player (Technique=1, FirstTouch=1) never
produces q = 0 from attributes alone (pressure at zero). The formula must not degenerate.
**[HAND-CALC]**

```
Step 1: WeightedAttr = (1 Ã— 0.70) + (1 Ã— 0.30) = 1.0
Step 2: NormAttr = 1.0 / 20.0 = 0.05
Step 3: AttrWithBonus = 0.05
Step 4: VelDifficulty = 15.0 / 15.0 = 1.0
Step 5: MoveDifficulty = 1.0
Step 6: RawQuality = 0.05 / 1.0 = 0.05
Step 7: q = 0.05 Ã— 1.0 = 0.05
Step 8: q = 0.05 (> 0)
```

**Inputs:** Technique=1, FirstTouch=1, ballSpeed=15.0, agentSpeed=0.0, pressure=0.0,
orientationBonus=0.0
**Expected:** q = 0.05 Â± 0.005
**Tolerance:** Â±0.005
**Failure interpretation:** q = 0.0 means the formula collapses to zero for minimum players,
which would make them physically unable to receive any ball â€” not design intent.

---

**Test ID:** CQ-009
**Name:** `ControlQuality_MaximumAttributes_SlowBall_ClampsAtOne`
**Purpose:** Verifies the upper clamp. Maximum attributes with a slow ball must produce
q = 1.0 exactly, confirming the Clamp(q, 0, 1) step works.
**[HAND-CALC]**

```
Step 1: WeightedAttr = (20 Ã— 0.70) + (20 Ã— 0.30) = 20.0
Step 2: NormAttr = 20.0 / 20.0 = 1.0
Step 3: AttrWithBonus = 1.0 Ã— 1.15 = 1.15  [half-turn]
Step 4: VelDifficulty = 5.0 / 15.0 = 0.333 [slow ball]
Step 5: MoveDifficulty = 1.0
Step 6: RawQuality = 1.15 / 0.333 = 3.45
Step 7: q = 3.45 Ã— 1.0 = 3.45
Step 8: q = Clamp(3.45, 0, 1) = 1.0
```

**Inputs:** Technique=20, FirstTouch=20, ballSpeed=5.0, agentSpeed=0.0, pressure=0.0,
orientationBonus=0.15
**Expected:** q = 1.0 (exact, clamped)
**Tolerance:** Exact
**Failure interpretation:** q < 1.0 means upper clamp is broken or formula produces < 1.0
for elite inputs, indicating constants are mis-tuned.

---

**Test ID:** CQ-010
**Name:** `ControlQuality_SprintReception_ReducesQualityVsStationary`
**Purpose:** Verifies that receiving while sprinting (7.0 m/s = MOVEMENT_REFERENCE) produces
a 50% movement difficulty increase compared to stationary reception.
**[HAND-CALC]**

```
Stationary: MoveDifficulty = 1.0 + (0.0 / 7.0) Ã— 0.5 = 1.000
Sprinting:  MoveDifficulty = 1.0 + (7.0 / 7.0) Ã— 0.5 = 1.500
Ratio of q: q_sprint / q_stationary â‰ˆ 1.000 / 1.500 = 0.667 (33% reduction)
```

**Inputs (stationary):** Technique=12, FirstTouch=11, ballSpeed=15.0, agentSpeed=0.0,
pressure=0.0, orientationBonus=0.0
**Inputs (sprinting):** Same + agentSpeed=7.0
**Expected:** q_sprinting â‰ˆ q_stationary Ã— 0.667
**Tolerance:** Â±0.02 on the ratio
**Failure interpretation:** Ratio closer to 1.0 means movement penalty is not being applied.
Ratio < 0.60 suggests movement difficulty is computed incorrectly.

---

**Test ID:** CQ-011
**Name:** `ControlQuality_Determinism_IdenticalInputsProduceBitwiseIdenticalOutput`
**Purpose:** Verifies the determinism contract (Master Vol 1 Â§1.3). The function must be
a pure mathematical function: no internal state, no Time, no Random, no platform variance.

**Method:** Call `CalculateControlQuality()` twice in the same frame with identical inputs.
Also call it in two separate frames (frame 1 and frame 100) with identical inputs.
**Expected:** All four results are bitwise identical (float equality, not tolerance equality).
**Failure interpretation:** Any difference in results is a critical determinism failure.
This is a blocking issue for replay system compatibility.

---

**Test ID:** CQ-012
**Name:** `ControlQuality_AllFourVerificationMatrixScenarios_PassSimultaneously`
**Purpose:** Runs all four canonical "sanity check" scenarios from Â§3.1.3 simultaneously.
Acts as a high-level regression test for the entire formula.

| Scenario | Tech | FT | Ball | Agent | Pressure | HalfTurn | Expected q |
|---|---|---|---|---|---|---|---|
| Elite, slow, no pressure | 20 | 20 | 5.0 | 0.0 | 0.0 | No | â‰¥ 0.85 |
| Average, typical, no pressure | 12 | 11 | 15.0 | 2.0 | 0.0 | No | 0.55â€“0.65 |
| Average, typical, medium pressure | 12 | 11 | 15.0 | 2.0 | 0.5 | No | 0.35â€“0.45 |
| Poor, hard shot, full pressure, sprint | 5 | 5 | 30.0 | 7.0 | 1.0 | No | â‰¤ 0.15 |

**Tolerance:** Per range bounds above.
**Failure interpretation:** Failure on any single scenario indicates which region of the
formula is mis-calibrated. Elite failure suggests attribute normalisation is wrong.
Average failure suggests pressure or movement is wrong. Poor/extreme failure suggests
velocity cap or combined difficulty is wrong.

---

## 5.3 Unit Tests â€” Touch Radius (TR)

These tests exercise `CalculateTouchRadius()` from Â§3.2.4. All expected values are derived
from the piecewise linear formula and velocity modifier defined in Â§3.2.2â€“Â§3.2.3.

---

**Test ID:** TR-001
**Name:** `TouchRadius_PerfectControl_ReturnsMinimumRadius`
**Purpose:** At q = 1.0 (maximum quality), the interpolation in the Perfect band must
produce the band minimum of 0.10m.
**[HAND-CALC]**

```
Band: Perfect. t = (1.0 - 0.85) / 0.15 = 1.0
r = Lerp(0.30, 0.10, 1.0) = 0.10m
VelMod at 12 m/s: excess = max(0, 12 - 15) = 0 â†’ VelMod = 1.0
r_final = 0.10m
```

**Inputs:** q=1.0, ballSpeed=12.0 m/s
**Expected:** r = 0.10m
**Tolerance:** Â±0.005m
**Failure interpretation:** r > 0.15m means the Perfect band upper boundary is wrong.

---

**Test ID:** TR-002
**Name:** `TouchRadius_PerfectGoodBoundary_IsContinuous`
**Purpose:** At q = 0.85 (the Perfect/Good boundary), both the top of the Good band
interpolation and the bottom of the Perfect band interpolation must agree. Verifies
piecewise continuity, which is required for monotonicity.
**[HAND-CALC]**

```
Perfect band at q = 0.85: t = 0 â†’ r = 0.30m
Good band at q = 0.85:    t = (0.85 - 0.60) / 0.25 = 1.0 â†’ r = Lerp(0.60, 0.30, 1.0) = 0.30m
Both = 0.30m âœ“
```

**Inputs (perfect):** q=0.8501, ballSpeed=15.0
**Inputs (good):** q=0.8499, ballSpeed=15.0
**Expected:** |r_perfect - r_good| < 0.01m
**Tolerance:** < 0.01m gap between them
**Failure interpretation:** A discontinuity at the band boundary means the Lerp endpoints
are mismatched, violating monotonicity.

---

**Test ID:** TR-003
**Name:** `TouchRadius_GoodControlMidRange_ReturnsInterpolatedValue`
**Purpose:** Validates the Good band at an interior point (q = 0.72).
**[HAND-CALC]**

```
t = (0.72 - 0.60) / 0.25 = 0.480
r = Lerp(0.60, 0.30, 0.480) = 0.60 - (0.30 Ã— 0.480) = 0.60 - 0.144 = 0.456m
No velocity excess at 15 m/s â†’ r_final = 0.456m
```

**Inputs:** q=0.72, ballSpeed=15.0 m/s
**Expected:** r = 0.456m
**Tolerance:** Â±0.005m

---

**Test ID:** TR-004
**Name:** `TouchRadius_GoodPoorBoundary_IsContinuous`
**Purpose:** Continuity verification at q = 0.60 (Good/Poor boundary).
**[HAND-CALC]**

```
Good band at q = 0.60:  t = 0 â†’ r = 0.60m
Poor band at q = 0.60:  t = (0.60 - 0.35) / 0.25 = 1.0 â†’ r = Lerp(1.20, 0.60, 1.0) = 0.60m
Both = 0.60m âœ“
```

**Inputs (good band):** q=0.6001, ballSpeed=15.0
**Inputs (poor band):** q=0.5999, ballSpeed=15.0
**Expected:** |r_good - r_poor| < 0.01m

---

**Test ID:** TR-005
**Name:** `TouchRadius_PoorBandMidRange_ReturnsInterpolatedValue`
**Purpose:** Validates the Poor band at q = 0.475.
**[HAND-CALC]**

```
t = (0.475 - 0.35) / 0.25 = 0.500
r = Lerp(1.20, 0.60, 0.500) = 0.90m
No velocity excess at 15 m/s â†’ r_final = 0.90m
```

**Inputs:** q=0.475, ballSpeed=15.0 m/s
**Expected:** r = 0.90m
**Tolerance:** Â±0.01m

---

**Test ID:** TR-006
**Name:** `TouchRadius_HeavyTouchBand_ReturnsInterpolatedValue`
**Purpose:** Validates the Heavy band at q = 0.175.
**[HAND-CALC]**

```
t = 0.175 / 0.35 = 0.500
r = Lerp(2.00, 1.20, 0.500) = 1.60m
No velocity excess at 15 m/s â†’ r_final = 1.60m
```

**Inputs:** q=0.175, ballSpeed=15.0 m/s
**Expected:** r = 1.60m
**Tolerance:** Â±0.02m

---

**Test ID:** TR-007
**Name:** `TouchRadius_PoorHeavyBoundary_IsContinuous`
**Purpose:** Continuity verification at q = 0.35 (Poor/Heavy boundary).
**[HAND-CALC]**

```
Poor band at q = 0.35:   t = 0 â†’ r = 1.20m
Heavy band at q = 0.35:  t = 0.35 / 0.35 = 1.0 â†’ r = Lerp(2.00, 1.20, 1.0) = 1.20m
Both = 1.20m âœ“
```

**Inputs (poor):** q=0.3501, ballSpeed=15.0
**Inputs (heavy):** q=0.3499, ballSpeed=15.0
**Expected:** |r_poor - r_heavy| < 0.01m

---

**Test ID:** TR-008
**Name:** `TouchRadius_MonotonicityVerification_HigherQualityAlwaysLowerRadius`
**Purpose:** Proves the MONOTONICITY CONTRACT (Â§3.2.2) holds across the full [0.0, 1.0]
range. This is a sweep test, not a single-point test.

**Method:** Compute r for q = {0.0, 0.05, 0.10, ..., 0.95, 1.0} (21 values) with
ballSpeed=15.0. Verify that each r[i+1] â‰¤ r[i]. Record any violations.
**Expected:** Zero violations â€” strictly non-increasing r across all 21 samples.
**Tolerance:** r[i+1] must not exceed r[i] by any positive amount.
**Failure interpretation:** Any violation means a band boundary is discontinuous or a
Lerp direction is reversed. This is a blocking failure for the radius system.

---

**Test ID:** TR-009
**Name:** `TouchRadius_VelocityModifier_IncreasesRadiusForFastBall`
**Purpose:** Verifies that a ball at 30 m/s produces a larger radius than an identical
quality ball at 15 m/s, and that the modifier formula is applied correctly.
**[HAND-CALC]**

```
At q=0.72, ballSpeed=15 m/s: r_base = 0.456m, VelExcess = 0, VelMod = 1.0, r = 0.456m
At q=0.72, ballSpeed=30 m/s: r_base = 0.456m
  VelExcess = max(0, 30 - 15) = 15
  VelMod = 1.0 + (15 / 15) Ã— 0.25 = 1.25
  r = 0.456 Ã— 1.25 = 0.570m
```

**Inputs A:** q=0.72, ballSpeed=15.0 â†’ Expected r = 0.456m
**Inputs B:** q=0.72, ballSpeed=30.0 â†’ Expected r = 0.570m
**Tolerance:** Â±0.01m each
**Failure interpretation:** If r_A == r_B, velocity modifier is not being applied.

---

**Test ID:** TR-010
**Name:** `TouchRadius_VelocityModifier_HardCappedAtMaxTouchRadius`
**Purpose:** Even with a very high velocity ball, radius must not exceed MAX_TOUCH_RADIUS (2.0m).

**Inputs:** q=0.10, ballSpeed=60.0 m/s (extreme)
**[HAND-CALC]**

```
r_base at q=0.10: t = 0.10 / 0.35 = 0.286 â†’ r = Lerp(2.00, 1.20, 0.286) = 1.771m
VelExcess = 45. VelMod = 1.0 + (45/15) Ã— 0.25 = 1.75
r = 1.771 Ã— 1.75 = 3.099m â†’ clamped to 2.0m
```

**Expected:** r = 2.0m (clamped)
**Tolerance:** Exact (hard cap)

---

## 5.4 Unit Tests â€” Pressure Evaluation (PR)

These tests exercise Â§3.5.1â€“Â§3.5.3. Each opponent is placed at a fixed distance; expected
pressure scalars are derived from the inverse-square falloff formula.

---

**Test ID:** PR-001
**Name:** `Pressure_NoOpponents_ReturnsZeroScalar`
**Purpose:** With no opposing agents within PRESSURE_RADIUS (3.0m), pressure scalar = 0.0.

**Setup:** Receiving agent at (50.0, 34.0, 0). No opponents within 3.0m.
**Expected:** pressureScalar = 0.0 (exact)
**Failure interpretation:** Non-zero output means the spatial query is returning false
positives or a constant offset is being added.

---

**Test ID:** PR-002
**Name:** `Pressure_SingleOpponentAtPressureRadiusEdge_ProducesNearZero`
**Purpose:** At exactly 3.0m (PRESSURE_RADIUS boundary), raw contribution is minimal.
**[HAND-CALC]**

```
distance = 3.0m, MIN_PRESSURE_DISTANCE = 0.3m
rawContrib = (0.3 / 3.0)Â² = 0.01
pressureScalar = Clamp(0.01 / 1.5, 0, 1) = 0.0067
```

**Setup:** One opponent at exactly 3.0m from receiving agent.
**Expected:** pressureScalar â‰ˆ 0.007
**Tolerance:** Â±0.005
**Note:** This test validates that the edge of the pressure radius is not excluded (closed
boundary). An opponent AT 3.0m still contributes, though minimally.

---

**Test ID:** PR-003
**Name:** `Pressure_SingleOpponentAt1Point5m_ProducesMidrangePressure`
**Purpose:** A typical pressing distance. Tests the inverse-square formula at a realistic
in-game distance.
**[HAND-CALC]**

```
rawContrib = (0.3 / 1.5)Â² = 0.04
pressureScalar = Clamp(0.04 / 1.5, 0, 1) = 0.0267
```

**Setup:** One opponent at 1.5m.
**Expected:** pressureScalar â‰ˆ 0.027
**Tolerance:** Â±0.005
**Note:** This is a lower pressure than the outline's PR-003 expected range (0.20â€“0.35).
The discrepancy is because the outline used an approximate formula. This test uses the
exact formula from Â§3.5. See Appendix B for reconciliation.

---

**Test ID:** PR-004
**Name:** `Pressure_SingleOpponentAt0Point5m_ProducesHighPressure`
**Purpose:** A tight-marking scenario. Opponent is inside the MIN_PRESSURE_DISTANCE clamp.
**[HAND-CALC]**

```
distance = 0.5m â†’ clamped distance = max(0.5, 0.3) = 0.5m
rawContrib = (0.3 / 0.5)Â² = 0.36
pressureScalar = Clamp(0.36 / 1.5, 0, 1) = 0.240
```

**Setup:** One opponent at 0.5m.
**Expected:** pressureScalar â‰ˆ 0.240
**Tolerance:** Â±0.02

---

**Test ID:** PR-005
**Name:** `Pressure_OpponentAtMinimumDistance_ClampsCorrectly`
**Purpose:** An opponent within MIN_PRESSURE_DISTANCE (0.3m) or closer must produce the
same result as one at exactly 0.3m (the clamp floor).
**[HAND-CALC]**

```
distance = 0.1m â†’ clamped to 0.3m
rawContrib = (0.3 / 0.3)Â² = 1.0
pressureScalar = Clamp(1.0 / 1.5, 0, 1) = 0.667
```

**Setup A:** One opponent at 0.1m. **Setup B:** One opponent at 0.3m.
**Expected:** pressureScalar_A == pressureScalar_B â‰ˆ 0.667
**Tolerance:** Â±0.01; must be equal between A and B
**Failure interpretation:** If A > B, the distance clamp is not being applied.

---

**Test ID:** PR-006
**Name:** `Pressure_TwoOpponents_StacksCorrectly`
**Purpose:** Verifies additive stacking of contributions from two opponents before
normalisation.
**[HAND-CALC]**

```
Opponent 1 at 1.0m: rawContrib = (0.3 / 1.0)Â² = 0.09
Opponent 2 at 2.0m: rawContrib = (0.3 / 2.0)Â² = 0.0225
rawPressure = 0.09 + 0.0225 = 0.1125
pressureScalar = Clamp(0.1125 / 1.5, 0, 1) = 0.075
```

**Setup:** Two opponents, one at 1.0m and one at 2.0m from receiving agent.
**Expected:** pressureScalar â‰ˆ 0.075
**Tolerance:** Â±0.01
**Failure interpretation:** If result equals single-opponent contribution, stacking is broken.

---

**Test ID:** PR-007
**Name:** `Pressure_SaturatesAtMaximumScalar`
**Purpose:** Extreme pressure (many close opponents) must saturate at 1.0, not exceed it.

**Setup:** Five opponents all at MIN_PRESSURE_DISTANCE (0.3m) from receiving agent.
**[HAND-CALC]**

```
Each contrib = 1.0. rawPressure = 5.0
pressureScalar = Clamp(5.0 / 1.5, 0, 1) = Clamp(3.333, 0, 1) = 1.0
```

**Expected:** pressureScalar = 1.0 (clamped)
**Tolerance:** Exact
**Failure interpretation:** pressureScalar > 1.0 means the final Clamp is missing.

---

**Test ID:** PR-008
**Name:** `Pressure_OpponentsBeyondRadius_NotCounted`
**Purpose:** Opponents outside PRESSURE_RADIUS (3.0m) must not contribute, even slightly.

**Setup:** Receiving agent at (50.0, 34.0, 0). One opponent at exactly 3.01m.
**Expected:** pressureScalar = 0.0 (opponent is outside pressure radius)
**Tolerance:** Exact 0.0
**Note:** This contrasts with PR-002 (opponent at exactly 3.0m contributes). The query
must use a strict less-than-or-equal comparison for the boundary.

---

## 5.5 Unit Tests â€” Body Orientation (OR)

These tests exercise `IsHalfTurnOriented()` and the orientation bonus from Â§3.6.

---

**Test ID:** OR-001
**Name:** `Orientation_AgentFacingAtHalfTurn_45Degrees_ReturnsTrue`
**Purpose:** The archetypal half-turn scenario. Agent facing at 45Â° to the incoming ball
direction places them squarely within the [30Â°, 60Â°] window.

**Setup:** Ball incoming from direction (âˆ’1, 0) (i.e., along negative X axis).
Agent facing direction: (0.707, 0.707, 0) â€” facing at 45Â° to ball approach.
**[HAND-CALC]**

```
incomingDir = (1, 0) [negated ball velocity for "direction ball came from"]
agentFacing = normalise(0.707, 0.707) = (0.707, 0.707)
angle = Atan2(cross, dot) where:
  dot   = 0.707Ã—1 + 0.707Ã—0 = 0.707
  cross = 0.707Ã—0 âˆ’ 0.707Ã—1 = âˆ’0.707   [signed z-component of cross product]
  angle = Atan2(âˆ’0.707, 0.707) = âˆ’45Â°
  absAngle = 45Â°
  45Â° âˆˆ [30Â°, 60Â°] â†’ IsHalfTurn = true
```

**Expected:** IsHalfTurnOriented = true; orientationBonus = 0.15
**Failure interpretation:** false output means Atan2 implementation is incorrect or angle
comparison uses wrong sign convention.

---

**Test ID:** OR-002
**Name:** `Orientation_AgentFacingBallDirectly_ReturnsNotHalfTurn`
**Purpose:** Agent facing directly toward the ball (0Â° angle) is not a half-turn. This
is the standard facing-the-ball reception; no bonus applies.

**Setup:** Ball incoming from (âˆ’1, 0). Agent facing (1, 0, 0) (directly facing source).
**Expected:** absAngle â‰ˆ 0Â°. 0Â° < 30Â° â†’ IsHalfTurnOriented = false; orientationBonus = 0.0

---

**Test ID:** OR-003
**Name:** `Orientation_AgentFacingAwayFromBall_ReturnsNotHalfTurn`
**Purpose:** Agent with back fully to the ball (180Â°) is not in the half-turn window.

**Setup:** Ball incoming from (âˆ’1, 0). Agent facing (âˆ’1, 0, 0) (facing away from ball source).
**Expected:** absAngle â‰ˆ 180Â°. 180Â° > 60Â° â†’ IsHalfTurnOriented = false

---

**Test ID:** OR-004
**Name:** `Orientation_AtLowerWindowBoundary_30Degrees_IsIncluded`
**Purpose:** Tests that the lower boundary (30Â°) is inclusive. Agent at exactly 30Â° must
receive the half-turn bonus.

**Setup:** Ball incoming from (1, 0). Agent facing at exactly 30Â° from ball approach direction.
**Expected:** absAngle = 30.0Â°. 30.0Â° â‰¥ HALF_TURN_ANGLE_MIN (30Â°) â†’ IsHalfTurnOriented = true
**Tolerance:** Test must use a facing vector that produces exactly 30Â° via Atan2.
**Note:** facing = (cos(30Â°), sin(30Â°), 0) = (0.866, 0.500, 0)

---

**Test ID:** OR-005
**Name:** `Orientation_AtUpperWindowBoundary_60Degrees_IsIncluded`
**Purpose:** Tests that the upper boundary (60Â°) is inclusive.

**Setup:** Ball incoming from (1, 0). Agent facing at exactly 60Â°.
**Note:** facing = (cos(60Â°), sin(60Â°), 0) = (0.500, 0.866, 0)
**Expected:** absAngle = 60.0Â° â‰¤ HALF_TURN_ANGLE_MAX (60Â°) â†’ IsHalfTurnOriented = true

---

**Test ID:** OR-006
**Name:** `Orientation_JustOutsideUpperBoundary_61Degrees_ReturnsFalse`
**Purpose:** Ensures the window is bounded above. One degree outside must not qualify.

**Setup:** Agent facing at 61Â° from ball approach direction.
**Expected:** IsHalfTurnOriented = false
**Failure interpretation:** true result means the upper boundary comparison uses wrong
operator (< instead of â‰¤, or > instead of <).

---

**Test ID:** OR-007
**Name:** `Orientation_Symmetrical_BothSidesOfBall_BothQualify`
**Purpose:** The half-turn window is symmetric: an agent at +45Â° and one at âˆ’45Â° from the
ball approach should both qualify. Atan2 sign handling must be correct.

**Setup A:** Agent at +45Â° from ball approach. **Setup B:** Agent at âˆ’45Â°.
**Expected:** Both return IsHalfTurnOriented = true, both return orientationBonus = 0.15
**Failure interpretation:** Only one side qualifying means the absolute angle comparison
is using signed angle (not absAngle), breaking left/right symmetry.

---

## 5.6 Unit Tests â€” Possession State Machine (PO)

These tests exercise the priority-ordered outcome logic from Â§3.4.2.

---

**Test ID:** PO-001
**Name:** `Possession_GoodQuality_NoOpponent_ReturnsControlled`
**Purpose:** The canonical CONTROLLED scenario. q â‰¥ 0.55 and r < 0.60m with no opponent
in range must always yield CONTROLLED.

**Inputs:** q=0.75 (Good band â†’ r â‰ˆ 0.42m < LOOSE_BALL_THRESHOLD 0.60m)
No opponents within INTERCEPTION_RADIUS (2.50m).
**Expected:** outcome = CONTROLLED; dribbling state signalled = true
**Tolerance:** Exact enum value

---

**Test ID:** PO-002
**Name:** `Possession_TouchRadius_AboveLooseBallThreshold_NoOpponent_ReturnsLooseBall`
**Purpose:** Ball escapes to 0.60mâ€“1.20m range with no opponent nearby. Must be LOOSE_BALL,
not DEFLECTION (no momentum alignment check triggers).

**Inputs:** q=0.45 (Poor band â†’ r â‰ˆ 0.90m). No opponents in 2.50m range.
Ensure momentum alignment < DEFLECTION_ALIGNMENT_MIN (0.70).
**Expected:** outcome = LOOSE_BALL
**Note:** Verify that DEFLECTION is not triggered when r < DEFLECTION_THRESHOLD (1.50m).

---

**Test ID:** PO-003
**Name:** `Possession_HeavyTouch_OpponentInRange_ReturnsInterception`
**Purpose:** Priority 1 rule: r â‰¥ INTERCEPTION_THRESHOLD (1.20m) AND opponent within
INTERCEPTION_RADIUS (2.50m) â†’ INTERCEPTION, regardless of momentum alignment.

**Inputs:** q=0.25 â†’ r â‰ˆ 1.43m (> 1.20m). One opponent at 2.0m (< 2.50m).
**Expected:** outcome = INTERCEPTION
**Failure interpretation:** DEFLECTION returned instead means priority ordering is wrong
(DEFLECTION check happening before INTERCEPTION check).

---

**Test ID:** PO-004
**Name:** `Possession_HeavyTouch_HighMomentumAlignment_NoOpponent_ReturnsDeflection`
**Purpose:** DEFLECTION requires: r â‰¥ 1.50m AND momentumAlignment â‰¥ 0.70 AND no opponent
in range. This tests the DEFLECTION path.

**Inputs:** q=0.15 â†’ r â‰ˆ 1.66m (> DEFLECTION_THRESHOLD 1.50m). No opponents within 2.50m.
Ball velocity and new ball velocity aligned within 45Â° (dot product â‰¥ 0.70).
**Expected:** outcome = DEFLECTION
**Failure interpretation:** LOOSE_BALL returned means deflection momentum check is missing.

---

**Test ID:** PO-005
**Name:** `Possession_HeavyTouch_LowMomentumAlignment_NoOpponent_ReturnsLooseBall`
**Purpose:** A heavy touch where the agent significantly redirected the ball (alignment < 0.70)
should be LOOSE_BALL, not DEFLECTION, because the ball is not travelling on its original path.

**Inputs:** q=0.20 â†’ r â‰ˆ 1.54m (> 1.50m). No opponents. Momentum alignment = 0.50 (< 0.70).
**Expected:** outcome = LOOSE_BALL
**Failure interpretation:** DEFLECTION returned means the alignment check is missing or using
wrong constant.

---

**Test ID:** PO-006
**Name:** `Possession_InterceptionTakesPriorityOverDeflection`
**Purpose:** Verifies priority ordering: if both INTERCEPTION and DEFLECTION conditions are
met (r â‰¥ 1.50m, alignment â‰¥ 0.70, opponent in range), INTERCEPTION wins.

**Inputs:** q=0.15 â†’ r â‰ˆ 1.66m. Ball momentum alignment = 0.85 (â‰¥ 0.70). Opponent at 2.0m.
**Expected:** outcome = INTERCEPTION (not DEFLECTION)
**Failure interpretation:** DEFLECTION returned means the priority ordering in Â§3.4.2 is
not respected. This is a documented design requirement from the INTERCEPTION priority note.

---

**Test ID:** PO-007
**Name:** `Possession_OpponentBeyondInterceptionRadius_DoesNotTriggerInterception`
**Purpose:** An opponent just outside INTERCEPTION_RADIUS (2.50m) must not trigger
INTERCEPTION, even if the touch radius qualifies.

**Inputs:** q=0.20 â†’ r â‰ˆ 1.54m. One opponent at 2.51m (just outside 2.50m radius).
**Expected:** outcome = DEFLECTION or LOOSE_BALL (not INTERCEPTION)
**Failure interpretation:** INTERCEPTION returned means the radius check uses wrong comparison
operator (< vs â‰¤) or incorrect constant.

---

**Test ID:** PO-008
**Name:** `Possession_ControlledOutcome_TriggersCorrectDribblingSignal`
**Purpose:** Verifies the dribbling state side-effect of CONTROLLED. The Agent Movement
interface must be called with (agentID, isDribbling=true) when outcome = CONTROLLED.

**Setup:** Mock `IAgentMovementSystem`. Run a CONTROLLED scenario.
**Expected:** `SetDribblingState(agentID, true)` called exactly once.
No call made for LOOSE_BALL / DEFLECTION / INTERCEPTION outcomes.
**Failure interpretation:** Missing call means CONTROLLED outcome is not signalling Agent
Movement, breaking the dribbling locomotion modifier system.

---

