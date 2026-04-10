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

## 5.7 Unit Tests â€” Edge Cases and Robustness (EC)

These tests verify that the system degrades gracefully under invalid or extreme inputs,
consistent with the validation rules in Â§4.8.

---

**Test ID:** EC-001
**Name:** `EdgeCase_NaNBallVelocity_DoesNotPropagateNaN`
**Purpose:** If ball velocity contains NaN (upstream data corruption), the system must
recover rather than propagating NaN through the simulation.

**Inputs:** ballSpeed = float.NaN (all other inputs valid)
**Expected:** Function returns a valid q âˆˆ [0.0, 1.0]. No NaN in output.
**Recovery method:** NaN ballSpeed should be clamped/replaced with VELOCITY_REFERENCE
fallback before formula application. See Â§2.6 FM-02.
**Failure interpretation:** NaN output causes the entire simulation to destabilize within
a few frames.

---

**Test ID:** EC-002
**Name:** `EdgeCase_ZeroBallVelocity_UsesMinimumVelocityFloor`
**Purpose:** A stationary ball (v = 0) must not cause division by zero in velocity
difficulty calculation (VelDifficulty = 0 / 15 = 0 â†’ denominator = 0).

**Inputs:** ballSpeed = 0.0 m/s
**Expected:** VelDifficulty clamped to 0.1 (VELOCITY_MIN_CLAMP from Â§3.1.4).
q = finite value âˆˆ [0.0, 1.0].
**Failure interpretation:** Division by zero exception or Infinity output.

---

**Test ID:** EC-003
**Name:** `EdgeCase_AttributesBelowMinimum_ClampedTo1`
**Purpose:** Attributes must be validated before formula entry. Technique = âˆ’5 should
behave identically to Technique = 1.

**Inputs:** Technique=âˆ’5, FirstTouch=âˆ’3
**Expected:** q equivalent to Technique=1, FirstTouch=1
**Note:** Clamping is documented in Â§4.8.1 validation asserts. In debug builds this
should also log an assertion failure.

---

**Test ID:** EC-004
**Name:** `EdgeCase_AttributesAboveMaximum_ClampedTo20`
**Purpose:** Attributes above 20 must not produce super-human results.

**Inputs:** Technique=25, FirstTouch=30
**Expected:** q equivalent to Technique=20, FirstTouch=20
**Failure interpretation:** If q > 1.0 (before final clamp), super-maximum attributes are
producing phantom quality â€” indicates the clamp is happening after normalization, not before.

---

**Test ID:** EC-005
**Name:** `EdgeCase_AgentAtPitchBoundary_BallPositionClamped`
**Purpose:** A touch at the pitch edge must produce a newBallPosition that remains within
pitch bounds after displacement.

**Setup:** Agent at position (0.5, 34.0, 0) (near sideline). Touch direction toward
X < 0 (off pitch). q=0.50 â†’ r â‰ˆ 0.90m.
**Expected:** newBallPosition.x â‰¥ 0.0 (clamped to pitch boundary)
**Failure interpretation:** Negative X position means boundary clamping in Â§3.3.4 is
not being applied.

---

**Test ID:** EC-006
**Name:** `EdgeCase_BallHeightAboveGroundControlHeight_ShouldNotEvaluateFirstTouch`
**Purpose:** The height guard at Â§2.2 must prevent First Touch evaluation for aerial balls.
This tests that the guard is active.

**Inputs:** BallHeight = 0.6m (above GROUND_CONTROL_HEIGHT = 0.5m)
**Expected:** EvaluateFirstTouch() throws an error, returns an error code, or is not called
(depending on implementation: the guard may be in the calling Collision System code).
The Â§4.8.1 validation assert must fire in debug builds.
**Note:** The *correct* handling is routing to Heading Mechanics. This test confirms the
guard triggers, not that heading mechanics runs (which is out of scope).

---

**Test ID:** EC-007
**Name:** `EdgeCase_SimultaneousContact_OnlyPrimaryContactEvaluated`
**Purpose:** If two agents contact the ball in the same frame (Collision System's primary
contact resolution), only one FirstTouchContext should enter EvaluateFirstTouch().
Tests the boundary condition at the Collision System interface.

**Setup:** Configure mock Collision System to return two simultaneous AgentBallCollisionData
structs in the same frame.
**Expected:** EvaluateFirstTouch() is called exactly once (primary contact only).
**Note:** This test exercises the contract defined in Collision System Spec Â§3.x
(primary contact determination). First Touch does not make this decision itself.

---

**Test ID:** EC-008
**Name:** `EdgeCase_InterceptionChain_DoesNotRecurseInSameFrame`
**Purpose:** When outcome = INTERCEPTION, the intercepting agent must not evaluate their
own first touch in the same frame (Critical Issue #4 resolution, Â§3.4.5).

**Setup:** Frame N: receive INTERCEPTION outcome. Confirm no second EvaluateFirstTouch()
call occurs for the intercepting agent in frame N.
**Expected:** EvaluateFirstTouch() for intercepting agent called zero times in frame N,
exactly once in frame N+1.
**Failure interpretation:** Same-frame recursion would cause infinite regression or incorrect
double-evaluation of the ball state.

---

## 5.8 Unit Tests â€” Ball Displacement (BD)

These tests exercise `CalculateBallDisplacement()` and the related velocity calculation from
Â§3.3. The ball displacement model has three interdependent components: the angular direction
blend (Â§3.3.2), the displacement distance (Â§3.3.3), and the new ball velocity (Â§3.3.5).
Each is tested independently before the compound behaviour is verified.

All vector inputs are in the XY plane (Z = 0) consistent with the coordinate system
defined in Ball Physics Â§3.1.1.

---

**Test ID:** BD-001
**Name:** `BallDisplacement_PerfectQuality_ActualDirectionMatchesIntended`
**Purpose:** At q = 1.0, the angular error model (Â§3.3.2) must produce zero deviation â€”
the ball goes exactly where the agent intended. This validates that the blend formula
`blended = IntendedDir Ã— 1.0 + IncomingDir Ã— 0.0` produces ActualDir = IntendedDir.
**[HAND-CALC]**

```
q = 1.0
ErrorWeight = 1.0 - q = 0.0
blended = IntendedDir Ã— 1.0 + IncomingDir Ã— 0.0 = IntendedDir
ActualDir = Normalise(IntendedDir) = IntendedDir  [already unit vector]
Angle between ActualDir and IntendedDir = 0Â°
```

**Inputs:**
- IntendedDir = (1.0, 0.0) [agent wants ball to go right]
- IncomingDir = (âˆ’0.707, 0.707) [ball coming from lower-left]
- q = 1.0

**Expected:** ActualDir = (1.0, 0.0) Â± 2Â°
**Tolerance:** Angular deviation < 2Â° (floating-point precision margin only)
**Failure interpretation:** Non-zero deviation at q=1.0 means the blend formula applies
a residual error term when it should be zero. Check ErrorWeight computation.

---

**Test ID:** BD-002
**Name:** `BallDisplacement_ZeroQuality_ActualDirectionMatchesIncoming`
**Purpose:** At q = 0.0, the agent has no control. The ball must follow the incoming
direction (deflected along its original path), not the intended direction. This validates
the opposite extreme of the blend formula.
**[HAND-CALC]**

```
q = 0.0
ErrorWeight = 1.0 - 0.0 = 1.0
blended = IntendedDir Ã— 0.0 + IncomingDir Ã— 1.0 = IncomingDir
ActualDir = Normalise(IncomingDir) = IncomingDir
```

**Inputs:**
- IntendedDir = (1.0, 0.0) [agent wants ball right]
- IncomingDir = (0.0, 1.0) [ball came from below, deflects upward]
- q = 0.0

**Expected:** ActualDir â‰ˆ (0.0, 1.0) (matches IncomingDir)
**Tolerance:** Angular deviation from IncomingDir < 2Â°
**Failure interpretation:** ActualDir closer to IntendedDir than IncomingDir means the
blend weights are inverted (ErrorWeight applied to IntendedDir instead of IncomingDir).
This would produce the physically wrong behaviour: failed touches go where intended.

---

**Test ID:** BD-003
**Name:** `BallDisplacement_MidQuality_ActualDirectionIsBlendedBetweenBoth`
**Purpose:** At q = 0.5, the actual direction must be a normalised blend of IntendedDir
and IncomingDir with equal weights. The result should lie between the two directions.
**[HAND-CALC]**

```
q = 0.5, ErrorWeight = 0.5
IntendedDir = (1.0, 0.0)
IncomingDir = (0.0, 1.0)

blended = (1.0, 0.0) Ã— 0.5 + (0.0, 1.0) Ã— 0.5 = (0.5, 0.5)
magnitude = sqrt(0.5Â² + 0.5Â²) = sqrt(0.5) = 0.707
ActualDir = (0.5/0.707, 0.5/0.707) = (0.707, 0.707)
Angle from IntendedDir (1,0): arccos(0.707) = 45Â°
Angle from IncomingDir (0,1): arccos(0.707) = 45Â°
ActualDir is exactly between both â†’ 45Â° from each âœ“
```

**Inputs:**
- IntendedDir = (1.0, 0.0)
- IncomingDir = (0.0, 1.0)
- q = 0.5

**Expected:** ActualDir = (0.707, 0.707) Â± 2Â°
**Tolerance:** Â±0.01 on each component
**Failure interpretation:** ActualDir not equidistant between IntendedDir and IncomingDir
means the weighting is asymmetric. This produces skill-weighted bias at mid-quality.

---

**Test ID:** BD-004
**Name:** `BallDisplacement_NearZeroBlendMagnitude_FallsBackToIncomingDir`
**Purpose:** When IntendedDir and IncomingDir are nearly opposite (anti-parallel), the
linear blend produces a near-zero vector. Normalising a zero vector is undefined. The
Â§3.3.2 safety guard must detect this and fall back to IncomingDir.
**[HAND-CALC]**

```
q = 0.5, ErrorWeight = 0.5
IntendedDir = (1.0, 0.0)
IncomingDir = (âˆ’1.0, 0.0)  [anti-parallel: agent trying to play ball back the way it came]

blended = (1.0, 0.0) Ã— 0.5 + (âˆ’1.0, 0.0) Ã— 0.5 = (0.0, 0.0)
magnitude = 0.0 â†’ below BLEND_MIN_MAGNITUDE (0.001)
FALLBACK: ActualDir = IncomingDir = (âˆ’1.0, 0.0)
```

**Inputs:**
- IntendedDir = (1.0, 0.0)
- IncomingDir = (âˆ’1.0, 0.0) [exactly anti-parallel]
- q = 0.5

**Expected:** ActualDir = (âˆ’1.0, 0.0) [fallback to IncomingDir]
**Tolerance:** Angular deviation from (âˆ’1.0, 0.0) < 5Â°
**Failure interpretation:** Any of the following indicates the fallback guard is absent or
broken: (1) NaN in ActualDir â€” divide-by-zero; (2) ActualDir = (0, 0) â€” zero vector not
caught; (3) ActualDir = IntendedDir â€” guard falls back to wrong direction.

**Note:** This is the highest-priority test in the BD category. The blend fallback guard
(Â§3.3.2) was added specifically because of this edge case. Its absence causes
a simulation-stopping NaN cascade whenever an agent at qâ‰ˆ0.5 attempts a back-heel touch.

---

**Test ID:** BD-005
**Name:** `BallDisplacement_NewBallVelocitySpeed_CappedAtTouchMaxBallSpeed`
**Purpose:** Verifies the velocity cap in Â§3.3.5. A touch cannot accelerate the ball
beyond TOUCH_MAX_BALL_SPEED (12.0 m/s), regardless of agent speed or ball momentum.
This prevents first touches from launching the ball at kick-like speeds.
**[HAND-CALC]**

```
Scenario: fast agent, high incoming ball, perfect control
AgentContrib = ActualDir Ã— min(agentSpeed, DRIBBLE_MAX_SPEED)
             = ActualDir Ã— min(7.0, 5.5) = ActualDir Ã— 5.5   [capped to dribble max]

BallRetained = ball.Velocity Ã— (1.0 - q) Ã— MOMENTUM_RETENTION
             = ball.Velocity Ã— 0.0 Ã— 0.5 = 0     [q=1.0 â†’ no retention]

newBallVelocity = (ActualDir Ã— 5.5 Ã— 1.0) + 0
               = ActualDir Ã— 5.5   [magnitude = 5.5 < 12.0 â†’ no cap needed]

For cap test, use q=0.3 (heavy touch retains momentum):
AgentContrib magnitude = ActualDir Ã— 5.5 Ã— 0.3 = 1.65 m/s
BallRetained = 20.0 m/s Ã— 0.7 Ã— 0.5 = 7.0 m/s [same direction as incoming]
If directions align: newSpeed = 1.65 + 7.0 = 8.65 m/s  [under cap, need worse case]

Use q=0.3, incoming ball 30 m/s, agent 7 m/s, aligned directions:
AgentContrib = 5.5 Ã— 0.3 = 1.65 m/s
BallRetained = 30.0 Ã— 0.7 Ã— 0.5 = 10.5 m/s
newSpeed = 1.65 + 10.5 = 12.15 m/s â†’ exceeds TOUCH_MAX_BALL_SPEED (12.0)
â†’ newBallVelocity clamped: magnitude = 12.0 m/s âœ“
```

**Inputs:**
- q = 0.30 (heavy touch, retains momentum)
- agentSpeed = 7.0 m/s (sprinting)
- ballSpeed = 30.0 m/s (hard incoming)
- ActualDir = IncomingDir (aligned for maximum combined speed)

**Expected:** newBallVelocity.magnitude = 12.0 m/s (clamped to TOUCH_MAX_BALL_SPEED)
**Tolerance:** Â±0.01 m/s
**Failure interpretation:** magnitude > 12.0 means the speed cap in Â§3.3.5 is not applied.
This allows first-touch deflections to produce kick-speed balls, violating physical realism.

---

**Test ID:** BD-006
**Name:** `BallDisplacement_NewBallVelocityZ_AlwaysZeroOnGroundTouch`
**Purpose:** Â§3.3.5 explicitly sets newBallVelocity.Z = 0.0 for ground touches (Stage 0
simplification). Verifies that vertical velocity from the incoming ball is discarded.

**Inputs:**
- Ball incoming with velocity (10.0, 5.0, 2.0) [Z = 2.0 m/s upward component]
- q = 0.8 (good touch)
- Ground touch (BallHeight â‰¤ GROUND_CONTROL_HEIGHT)

**Expected:** newBallVelocity.Z = 0.0 (exact)
**Tolerance:** Exact 0.0
**Failure interpretation:** Non-zero Z means the vertical component is being retained,
which would cause the ball to bounce upward after a ground touch â€” physically wrong
for a Stage 0 "flatten the ball" reception model.

**Note:** This test documents the Stage 0 simplification noted in Â§3.3.5. Stage 1 will
introduce loft modelling for chest and thigh contacts. Any test that passes here will
require revision when loft is implemented.

---

**Test ID:** BD-007
**Name:** `BallDisplacement_PitchBoundaryEnforcement_XAxisClamped`
**Purpose:** newBallPosition must not exceed the pitch X boundary (0 to PITCH_LENGTH 105.0m).
Tests the boundary clamping from Â§3.3.4 on the X axis specifically.

**Setup:**
- Agent at position (1.0, 34.0, 0) â€” near goal line
- q = 0.40 â†’ r â‰ˆ 1.10m (Poor band)
- IntendedDir = (âˆ’1.0, 0.0) [touching ball toward own goal line / off pitch]
- DisplacementDist = r = 1.10m

**[HAND-CALC]**

```
newBallPosition.x = agent.x + ActualDir.x Ã— r = 1.0 + (âˆ’1.0 Ã— 1.10) = âˆ’0.10m
â†’ clamped to 0.0m (PITCH_LENGTH lower bound)
newBallPosition.y = 34.0 + 0 = 34.0m  [unchanged]
newBallPosition.z = Ball.RADIUS = 0.11m
```

**Expected:** newBallPosition.x = 0.0m (clamped); Y and Z unchanged
**Tolerance:** Â±0.005m on X
**Failure interpretation:** Negative X position is off the pitch, which would place the
ball in an invalid game state and cause Collision System spatial hash to behave incorrectly.

---

**Test ID:** BD-008  
**Name:** `BallDisplacement_PitchBoundaryEnforcement_YAxisClamped`
**Purpose:** Same boundary enforcement on the Y axis (0 to PITCH_WIDTH 68.0m). Tests
the sideline boundary specifically â€” a common scenario for wing play.

**Setup:**
- Agent at position (52.5, 67.5, 0) â€” near sideline
- q = 0.45 â†’ r â‰ˆ 0.90m
- IntendedDir = (0.0, 1.0) [touching ball toward sideline / off pitch]

**[HAND-CALC]**

```
newBallPosition.y = 67.5 + (1.0 Ã— 0.90) = 68.40m â†’ clamped to 68.0m (PITCH_WIDTH upper bound)
newBallPosition.x = 52.5 + 0 = 52.5m  [unchanged]
newBallPosition.z = 0.11m
```

**Expected:** newBallPosition.y = 68.0m (clamped)
**Tolerance:** Â±0.005m on Y

---

## 5.9 Integration Tests (IT)

Integration tests verify that First Touch interacts correctly with its three primary
consumers: Ball Physics (Â§4.5.1), Agent Movement (Â§4.5.3), and the Collision System (Â§4.5.2).
These tests require a minimal game context with at least two agents and a ball.

---

**Test ID:** IT-001
**Name:** `Integration_CollisionToFirstTouchToBallPhysics_CompletePipeline`
**Purpose:** The full Step-5-of-9 pipeline (Â§2.2). Collision System detects contact â†’
First Touch evaluates â†’ Ball Physics receives updated BallState. No steps are mocked.

**Setup:** One agent moving at 2.0 m/s toward a ball. Ball moving at 12.0 m/s toward agent.
All systems active (Collision, First Touch, Ball Physics).
**Expected:**
1. Collision System fires `AgentBallCollisionData` with correct contact point.
2. FirstTouchContext is built and populated from live game state.
3. EvaluateFirstTouch() runs and produces FirstTouchResult with no NaN fields.
4. Ball Physics receives `newBallState` and applies it as authoritative ball position/velocity.
5. Ball is no longer at its pre-touch position one frame after contact.
**Failure interpretation:** Ball remains at original position = Ball Physics did not receive
or apply the new state.

---

**Test ID:** IT-002
**Name:** `Integration_ControlledOutcome_AgentMovementReceivesDribblingModifier`
**Purpose:** When outcome = CONTROLLED, Agent Movement must receive the dribbling modifier
and apply speed/turn-rate penalties to the possessing agent.

**Setup:** Stage CONTROLLED outcome (elite agent, slow ball, no pressure).
Agent Movement system is live (not mocked).
**Expected:**
1. Agent enters dribbling locomotion state (Â§Agent Movement Â§6.1.2).
2. Agent's effective max speed is reduced per dribbling modifier.
3. Modifier persists across subsequent frames until possession is lost.
**Failure interpretation:** No speed reduction means the dribbling signal was not sent or
Agent Movement is not processing it.

---

**Test ID:** IT-003
**Name:** `Integration_PossessionChange_UpdatesTeamPossessionState`
**Purpose:** When outcome changes possession (CONTROLLED, INTERCEPTION), the match state's
team possession flag must update correctly.

**Setup A:** Team A agent receives ball cleanly â†’ CONTROLLED.
**Setup B:** Team A agent's heavy touch is intercepted by Team B agent (next frame).
**Expected A:** Match state shows Team A in possession.
**Expected B:** Match state shows Team B in possession after interception resolves.
**Failure interpretation:** Possession flag unchanged = First Touch is not writing to
match state, or match state is not reading from First Touch output.

---

**Test ID:** IT-004
**Name:** `Integration_EventEmission_FirstTouchEventPublishedOnEachContact`
**Purpose:** Every call to EvaluateFirstTouch() that produces a result must emit a
FirstTouchEvent (Â§3.8.3) to the Event System.

**Setup:** Three separate contacts in sequence (CONTROLLED, LOOSE_BALL, DEFLECTION).
Event System subscriber records received events.
**Expected:** Exactly three FirstTouchEvents received, one per contact, with correct
ControlQuality, TouchRadius, Outcome, and AgentID fields.
**Failure interpretation:** Missing events break the statistics and replay systems.

---

**Test ID:** IT-005
**Name:** `Integration_ReplayDeterminism_IdenticalMatchStateProducesIdenticalResults`
**Purpose:** The determinism requirement (Master Vol 1 Â§1.3). Same match state recorded
at frame N must produce identical First Touch output when replayed.

**Method:**
1. Record a 10-second match segment. Log all FirstTouchContext inputs and FirstTouchResult
   outputs at every contact.
2. Replay the same segment from identical initial conditions.
3. Compare logged inputs and outputs frame-by-frame.
**Expected:** Bitwise-identical outputs for all contacts in both runs.
**Failure interpretation:** Any divergence is a critical determinism failure. Root cause
must be identified before implementation proceeds. Common causes: floating-point
non-determinism, uninitialized memory, frame-order dependency.

---

**Test ID:** IT-006
**Name:** `Integration_GoalkeeperFootControl_ProcessedNormally`
**Purpose:** IsGoalkeeper = true in AgentBallCollisionData (Â§4.3.1) must not alter
First Touch evaluation for a foot contact. The flag is informational in Stage 0.

**Setup:** Goalkeeper agent (IsGoalkeeper=true) receives a back pass at 14 m/s.
Goalkeeper Technique=12, FirstTouch=10 (typical goalkeeper attributes).
**Expected:** EvaluateFirstTouch() produces the same result as an outfield player with
identical attributes. IsGoalkeeper flag does not modify q, r, or outcome.
**Failure interpretation:** Different result means the IsGoalkeeper flag is incorrectly
used to modify the formula, violating the Â§1.3 scope boundary.

---

**Test ID:** IT-007
**Name:** `Integration_PressureQueryFromSpatialHash_ReturnsCorrectOpponents`
**Purpose:** The pressure spatial query (Â§3.5.1) uses the Collision System's spatial hash
(Collision System Â§3.1.4). Verify the correct opponents are returned in a live scenario.

**Setup:** Receiving agent at (50, 34, 0). Place exactly two Team B agents within
pressure radius: one at (51.5, 34, 0) (1.5m away) and one at (50, 36.5, 0) (2.5m away).
Place one Team B agent outside radius at (53.2, 34, 0) (3.2m away).
**Expected:** Spatial query returns exactly two opponents. pressureScalar matches the
two-opponent hand calculation.
**Failure interpretation:** Three opponents returned means the radius filter is wrong.
One or zero returned means the spatial hash is not registering one of the in-range agents.

---

**Test ID:** IT-008
**Name:** `Integration_ConsecutiveTouches_OneTwoPass_BothEvaluateCorrectly`
**Purpose:** A one-two pass scenario: Agent A receives from Agent B (Touch 1), immediately
passes back to Agent B who touches it again (Touch 2). Tests that consecutive evaluations
in the same match context are independent and produce correct results.

**Setup:**
- Frame N: Agent A receives pass at 18 m/s. Should produce CONTROLLED or LOOSE_BALL.
- Frame N+2: Agent B receives the return pass at 14 m/s. Should produce CONTROLLED.
**Expected:** Both evaluations complete independently. Agent B's evaluation in frame N+2
uses Agent B's attributes, not Agent A's (no context bleed between evaluations).
**Failure interpretation:** Any shared state between evaluations is a critical data isolation
failure.

---

## 5.10 Real-World Validation Scenarios (VS)

These scenarios ground the First Touch system in recognisable football events. They serve
as sanity checks that the mathematical model produces intuitive results. All expected
values are hand-calculated and documented in Appendix B.

Each scenario maps to a player archetype and situation observable in professional football.
The scenarios do not test specific real players â€” they test attribute profiles that represent
real-world archetypes.

---

### VS-001: Elite Creative Midfielder, Standard Pass in Space
**Football context:** An elite technical midfielder (Ã–zil / De Bruyne archetype) receiving
a pass in a half-turn position with no immediate pressure.

**Setup:**
- Technique: 18, FirstTouch: 19
- Ball speed: 14 m/s (crisp pass)
- Agent speed: 3.0 m/s (jogging to meet ball)
- Half-turn oriented: Yes (orientationBonus = 0.15)
- No opponents within pressure radius (pressureScalar = 0.0)

**[HAND-CALC]** (Appendix B, VS-001):
```
Step 1: WeightedAttr = (18 Ã— 0.70) + (19 Ã— 0.30) = 12.60 + 5.70 = 18.30
Step 2: NormAttr = 18.30 / 20.0 = 0.915
Step 3: AttrWithBonus = 0.915 Ã— 1.15 = 1.052
Step 4: VelDifficulty = 14.0 / 15.0 = 0.933
Step 5: MoveDifficulty = 1.0 + (3.0 / 7.0) Ã— 0.5 = 1.0 + 0.214 = 1.214
Step 6: RawQuality = 1.052 / (0.933 Ã— 1.214) = 1.052 / 1.132 = 0.929
Step 7: q = 0.929 Ã— 1.0 = 0.929
Step 8: q = Clamp(0.929, 0, 1) = 0.929
Touch radius (Perfect band): t = (0.929 - 0.85) / 0.15 = 0.527 â†’ r_base = Lerp(0.30, 0.10, 0.527) = 0.195m
Velocity modifier: (14.0 / 15.0) x 0.25 = 0.933 x 0.25 = 0.233m
r = r_base + modifier = 0.195 + 0.233 = 0.428m
Outcome: r = 0.428m <= 0.60m â†’ CONTROLLED
```

**Expected outputs:**
- q â‰ˆ 0.929 (Â±0.03) â†’ Perfect band
- r â‰ˆ 0.428m (±0.02m)
- outcome = CONTROLLED
- dribbling state activated: true

**Real-world validation:** An elite playmaker's first touch in this scenario should produce
a ball landing within approximately 0.2m, ready for immediate play. This matches observable
professional ball control.

---

### VS-002: Target Striker, Long Ball Reception Under Pressure
**Football context:** A physical centre-forward (Haaland / Giroud archetype) receiving a
60-metre diagonal ball under defensive pressure.

**Setup:**
- Technique: 12, FirstTouch: 11
- Ball speed: 22 m/s (long diagonal ball, decelerating to this speed at arrival)
- Agent speed: 0.5 m/s (bracing, nearly stationary)
- Half-turn oriented: No
- Two defenders at 1.5m and 2.0m (pressureScalar calculated below)

**[HAND-CALC] Pressure first:**
```
Defender 1 at 1.5m: rawContrib = (0.3 / 1.5)Â² = 0.04
Defender 2 at 2.0m: rawContrib = (0.3 / 2.0)Â² = 0.0225
rawPressure = 0.0625
pressureScalar = Clamp(0.0625 / 1.5, 0, 1) = 0.042
```

**[HAND-CALC] Control quality:**
```
Step 1: WeightedAttr = (12 Ã— 0.70) + (11 Ã— 0.30) = 8.40 + 3.30 = 11.70
Step 2: NormAttr = 11.70 / 20.0 = 0.585
Step 3: AttrWithBonus = 0.585 Ã— 1.0 = 0.585
Step 4: VelDifficulty = 22.0 / 15.0 = 1.467
Step 5: MoveDifficulty = 1.0 + (0.5 / 7.0) Ã— 0.5 = 1.0 + 0.036 = 1.036
Step 6: RawQuality = 0.585 / (1.467 Ã— 1.036) = 0.585 / 1.520 = 0.385
Step 7: q = 0.385 Ã— (1.0 - 0.042 Ã— 0.40) = 0.385 Ã— 0.983 = 0.379
Step 8: q = 0.379
Touch radius (Poor band): t = (0.379 - 0.35) / 0.25 = 0.116 â†’ r = Lerp(1.20, 0.60, 0.116) = 1.130m
Velocity modifier at 22 m/s: VelExcess = 7, VelMod = 1 + (7/15) Ã— 0.25 = 1.117
r_adjusted = 1.130 Ã— 1.117 = 1.262m â†’ below 2.0m cap
Outcome: r = 1.262m â‰¥ 1.20m â†’ check opponents.
Two defenders, nearest at 1.5m < INTERCEPTION_RADIUS 2.50m â†’ INTERCEPTION
```

**Expected outputs:**
- q â‰ˆ 0.379 (Â±0.03) â†’ bottom of Poor band
- r â‰ˆ 1.262m (Â±0.05m) â†’ crosses INTERCEPTION_THRESHOLD
- outcome = INTERCEPTION (defenders exploit the heavy touch)

**Real-world validation:** A target striker's first touch on a fast long ball under tight
marking is frequently contested. The INTERCEPTION outcome here represents a defender
winning the second ball â€” a common scenario in physical football.

**Note on pressure values:** The two defenders at 1.5m and 2.0m in this scenario produce
lower pressure (0.042) than the outline's qualitative description implied ("medium pressure").
This is correct per the Â§3.5 formula. "Medium pressure" in colloquial terms corresponds to
opponents at tighter distances than 1.5m. Outline approximations were illustrative, not
precise. This hand-calculation takes precedence.

---

### VS-003: Defensive Midfielder, Receiving Under a High Press
**Football context:** A defensive midfielder (Busquets / Rice archetype) trying to receive
in a high-press scenario with a striker immediately closing.

**Setup:**
- Technique: 14, FirstTouch: 13
- Ball speed: 17 m/s (firm pass from goalkeeper)
- Agent speed: 1.5 m/s (standing and scanning)
- Half-turn oriented: Yes (pre-scanning, orientationBonus = 0.15)
- One pressing striker at 0.8m (very close)

**[HAND-CALC] Pressure:**
```
distance = 0.8m (> MIN_PRESSURE_DISTANCE 0.3m â†’ no clamp needed)
rawContrib = (0.3 / 0.8)Â² = 0.141
pressureScalar = Clamp(0.141 / 1.5, 0, 1) = 0.094
```

**[HAND-CALC] Control quality:**
```
Step 1: WeightedAttr = (14 Ã— 0.70) + (13 Ã— 0.30) = 9.80 + 3.90 = 13.70
Step 2: NormAttr = 13.70 / 20.0 = 0.685
Step 3: AttrWithBonus = 0.685 Ã— 1.15 = 0.788
Step 4: VelDifficulty = 17.0 / 15.0 = 1.133
Step 5: MoveDifficulty = 1.0 + (1.5 / 7.0) Ã— 0.5 = 1.0 + 0.107 = 1.107
Step 6: RawQuality = 0.788 / (1.133 Ã— 1.107) = 0.788 / 1.254 = 0.628
Step 7: q = 0.628 Ã— (1.0 - 0.094 Ã— 0.40) = 0.628 Ã— 0.962 = 0.604
Step 8: q = 0.604
Touch radius (Good band): t = (0.604 - 0.60) / 0.25 = 0.016 â†’ r = Lerp(0.60, 0.30, 0.016) = 0.595m
Velocity modifier: VelExcess = 2.0, VelMod = 1 + (2/15) Ã— 0.25 = 1.033
r_adjusted = 0.595 Ã— 1.033 = 0.615m â†’ above LOOSE_BALL_THRESHOLD (0.60m)
Outcome: r = 0.615m â‰¥ 0.60m. r < 1.20m (below INTERCEPTION_THRESHOLD).
â†’ Check opponent: striker at 0.8m < 2.50m? Yes.
BUT: r = 0.615m < INTERCEPTION_THRESHOLD (1.20m) â†’ INTERCEPTION priority not met.
â†’ LOOSE_BALL
```

**Expected outputs:**
- q â‰ˆ 0.604 (Â±0.03) â†’ just inside Good band
- r â‰ˆ 0.615m (Â±0.03m) â†’ just above LOOSE_BALL_THRESHOLD
- outcome = LOOSE_BALL (ball requires a second touch; striker can contest)

**Real-world validation:** A technically competent player in a half-turn receiving a firm
pass under a close press should retain the ball roughly 60% of the time but frequently
require a second touch. The LOOSE_BALL outcome represents a contested situation â€” the
midfielder has not cleanly controlled it but has not lost it outright. This is realistic.

**Design insight:** The half-turn bonus (15%) is what keeps this scenario from being an
INTERCEPTION. Without the half-turn, q would drop to approximately 0.526, pushing into
the Poor band with r â‰ˆ 0.80m and potentially triggering INTERCEPTION. The mechanical
incentive to adopt a half-turn stance is therefore demonstrated by this scenario.

---

## 5.11 Acceptance Criteria Summary

For implementation to be approved for Stage 0 sign-off, ALL of the following gates must pass.

### 5.11.1 Unit Test Gate

| Category | Required Count | Minimum Pass Rate |
|---|---|---|
| Control Quality (CQ) | 12 tests | 100% |
| Touch Radius (TR) | 10 tests | 100% |
| Pressure Evaluation (PR) | 8 tests | 100% |
| Body Orientation (OR) | 7 tests | 100% |
| Possession State Machine (PO) | 8 tests | 100% |
| Edge Cases (EC) | 8 tests | 100% |
| Ball Displacement (BD) | 8 tests | 100% |
| **Total unit tests** | **61 tests** | **100%** |

**No partial pass accepted.** Any failing unit test is a blocking issue. The minimum test
count from the outline was 25; this specification provides 61 â€” more than 2.4Ã— the minimum,
consistent with this project's pattern of exceeding minimums on critical systems.

### 5.11.2 Integration Test Gate

| Category | Required Count | Minimum Pass Rate |
|---|---|---|
| Integration tests (IT) | 8 tests | 100% |

### 5.11.3 Validation Scenario Gate

All three validation scenarios (VS-001, VS-002, VS-003) must produce outputs within the
specified tolerances.

### 5.11.4 Performance Gate

| Metric | Target | Hard Limit |
|---|---|---|
| p95 evaluation latency | < 0.05ms | < 0.10ms |
| p99 evaluation latency | < 0.10ms | < 0.20ms |
| Heap allocations per touch | 0 | 0 |
| Total match impact (300 touches) | < 15ms | < 30ms |

### 5.11.5 Coverage Gate

| Scope | Target |
|---|---|
| Section 3.1 (Control Quality) | â‰¥ 90% line coverage |
| Section 3.2 (Touch Radius) | â‰¥ 90% line coverage |
| Section 3.3 (Ball Displacement) | â‰¥ 85% line coverage |
| Section 3.4 (Possession) | â‰¥ 90% line coverage |
| Section 3.5 (Pressure) | â‰¥ 90% line coverage |
| Section 3.6 (Orientation) | â‰¥ 90% line coverage |
| Overall First Touch System | â‰¥ 85% line coverage |

### 5.11.6 Determinism Gate

The IT-005 determinism test must pass with zero divergences over a 10-second match
segment. This is a binary pass/fail gate with no tolerance.

---

## 5.12 Test Execution Plan

### 5.12.1 Test Ordering

Tests must run in the following order to ensure dependency satisfaction and efficient
failure diagnosis. Unit tests run first; failures here do not block integration tests
from being attempted (diagnosis value), but unit test failures are blocking for sign-off.

```
Order 1: EC tests (edge cases / robustness)    â€” validates input sanitisation first
Order 2: CQ tests (control quality formula)   â€” validates primary computation
Order 3: TR tests (touch radius)               â€” depends on correct q output
Order 4: BD tests (ball displacement)         â€” depends on correct q and r outputs
Order 5: PR tests (pressure evaluation)        â€” isolated sub-system
Order 6: OR tests (orientation detection)      â€” isolated sub-system
Order 7: PO tests (possession state machine)   â€” depends on q and r
Order 8: IT tests (integration)                â€” requires all units passing
Order 9: VS tests (validation scenarios)       â€” requires integration passing
```

### 5.12.2 Execution Cadence

| Cadence | Tests | Trigger |
|---|---|---|
| On every code change | CQ, TR, PR, OR, PO, EC (all unit tests) | Git pre-commit hook |
| On pull request | All unit + all IT tests | CI pipeline |
| On milestone | All tests including VS | Manual milestone gate |
| Nightly | IT-005 determinism + performance profiling | Scheduled CI job |

**Target total execution time:**
- Unit tests only: < 10 seconds
- Unit + integration: < 60 seconds
- Full suite including VS: < 5 minutes

### 5.12.3 Failure Response Protocol

**Level 1 â€” Formula error (CQ, TR, PR, OR):** Halt further development. Fix before
any integration work. These failures contaminate all downstream tests.

**Level 2 â€” State machine error (PO):** Fix before integration testing. Priority order
errors (PO-006) are particularly critical as they affect gameplay-observable behaviour.

**Level 3 â€” Integration failure (IT):** May proceed with other non-dependent IT tests
for diagnosis. Must resolve before milestone gate.

**Level 4 â€” Validation scenario deviation (VS):** If outputs are within 2Ã— the tolerance,
investigate formula calibration. If outputs are within 10Ã— tolerance, investigate whether
the player archetype attributes need revision. If outputs exceed 10Ã— tolerance, treat as
Level 1 (formula error).

**Level 5 â€” Determinism failure (IT-005):** This is a CRITICAL failure. Halt all
development. Root-cause analysis required before any code proceeds. Possible causes:
floating-point operations with non-deterministic ordering, unintentional state sharing
between evaluations, platform-specific math library differences.

### 5.12.4 Regression Test Strategy

All 61 unit tests serve as the regression suite. When any formula constant is tuned
(e.g., PRESSURE_WEIGHT, MOVEMENT_PENALTY, BLEND_MIN_MAGNITUDE, TOUCH_MAX_BALL_SPEED),
the following tests will likely need expected value recalculation: CQ-002, CQ-003,
CQ-006, CQ-010, CQ-012, BD-003, BD-005, VS-001, VS-002, VS-003.

These tests should be reviewed and updated alongside any constant change. The hand
calculations in Appendix B must be re-run to verify new expected values before updating
the tests.

---

## 5.13 Section Summary

| Subsection | Test Count | Key Content |
|---|---|---|
| **5.2 Control Quality (CQ)** | 12 | Formula validation, bonus isolation, clamps, determinism |
| **5.3 Touch Radius (TR)** | 10 | Band interpolation, continuity at boundaries, monotonicity, velocity modifier |
| **5.4 Pressure (PR)** | 8 | Inverse-square formula, stacking, saturation, boundary conditions |
| **5.5 Orientation (OR)** | 7 | Window bounds, symmetry, Atan2 sign convention |
| **5.6 Possession (PO)** | 8 | All four outcomes, priority ordering, dribbling signal |
| **5.7 Edge Cases (EC)** | 8 | NaN, zero velocity, attribute bounds, pitch boundary, simultaneity |
| **5.8 Ball Displacement (BD)** | 8 | Blend formula, zero-quality/perfect-quality extremes, near-zero fallback, velocity cap, Z zeroing, pitch boundary X and Y |
| **5.9 Integration (IT)** | 8 | Full pipeline, determinism, cross-system data flow |
| **5.10 Validation (VS)** | 3 | Real-world archetype scenarios with complete hand calculations |
| **Total** | **72 tests** | Exceeds outline minimum (25 unit + 8 integration) by 2.6Ã— |

---

## Cross-Reference Verification

| Reference | Target | Verified | Notes |
|---|---|---|---|
| Ball Physics #1 Â§3.1.1 | Coordinate system (XY pitch, Z up) | âœ“ | IT-001, BD-001â€“BD-008 use XY plane inputs |
| Ball Physics #1 Â§3.1.2 | Ball.RADIUS = 0.11m | âœ“ | BD-007, BD-008 verify Z = 0.11m in newBallPosition |
| Agent Movement #2 Â§3.5.6 | Technique, FirstTouch [1â€“20] | âœ“ | All CQ tests use [1â€“20] attribute range |
| Agent Movement #2 Â§6.1.2 | DribblingModifier | âœ“ | IT-002, PO-008 verify dribbling signal |
| Collision System #3 Â§4.2.6 | AgentBallCollisionData | âš  Pending | IT-001, IT-007 depend on this interface |
| Master Vol 1 Â§1.3 | Determinism requirement | âœ“ | CQ-011, IT-005 explicitly test determinism |
| Master Vol 1 Â§6 | Touch radius thresholds | âœ“ | All TR tests use Master Vol 1 band values |
| First Touch #4 Â§3.1 | Control quality formula | âœ“ | All CQ derivations reference Â§3.1 steps |
| First Touch #4 Â§3.2 | Touch radius bands | âœ“ | All TR derivations reference Â§3.2.2 |
| First Touch #4 Â§3.3 | Ball displacement model | âœ“ | BD-001â€“BD-008 cover Â§3.3.2â€“Â§3.3.5 |
| First Touch #4 Â§3.4 | Possession state machine | âœ“ | All PO tests reference Â§3.4.2 priority order |
| First Touch #4 Â§3.5 | Pressure formula | âœ“ | All PR derivations reference Â§3.5.2â€“Â§3.5.3 |
| First Touch #4 Â§3.6 | Orientation window | âœ“ | All OR tests reference Â§3.6.2 constants |
| First Touch Outline Â§5.1 | Min 25 unit tests | âœ“ | 61 unit tests provided (2.44Ã— minimum) |
| First Touch Outline Â§5.2 | Min 8 integration tests | âœ“ | 8 integration tests provided |

---

**End of Section 5**

**Page Count:** ~28 pages
**Version:** 1.0
**Next Section:** Section 6 â€” Performance Analysis
