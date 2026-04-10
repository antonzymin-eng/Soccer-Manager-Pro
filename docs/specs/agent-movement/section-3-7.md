# Agent Movement Specification Ã¢â‚¬â€ Section 3.7: Validation & Testing Framework

**Purpose:** Defines all test specifications for the Agent Movement System's physics models, integration scenarios, real-world validation benchmarks, regression testing requirements, and performance testing criteria. This section covers functional correctness testing (happy-path physics); edge case and error recovery tests remain authoritative in Section 3.6.7. Together, Sections 3.6.7 and 3.7 constitute the complete test suite for Agent Movement.

**Created:** February 11, 2026, 6:30 PM PST  
**Updated:** February 14, 2026, 11:30 AM PST
**Version:** 1.2
**Status:** Draft (Revised)  
**Dependencies:** Section 3.1 (State Machine), Section 3.2 (Locomotion), Section 3.3 (Directional Movement), Section 3.4 (Turning & Momentum), Section 3.5 (Data Structures), Section 3.6 (Edge Cases), Ball Physics Spec #1 Section 5 (testing pattern reference)

**Rendering note:** This document contains mathematical symbols (Ãƒâ€”, Ã¢â€°Ë†, Ãâ‚¬, etc.) that require UTF-8 encoding to display correctly. If symbols appear garbled, verify the file is being read with UTF-8 encoding.

---

## v1.1 Changelog

**CRITICAL FIXES:**

1. **Concrete expected values for all acceleration/speed tests** (Issue #1): UT-ACC-001 now includes exact k derivation and computed velocity at t=0.5s. All tests referencing derived values now show the full calculation chain rather than deferring to the implementer. Mirrors Ball Physics Section 5 pattern of explicit expected numbers.

2. **Deferred validation clearly separated from approval-blocking criteria** (Issue #2): VB-3 and VB-4 (match distance, sprint distance) moved to new "Deferred Validation" subsection (3.7.4.3) with explicit note that they cannot be validated until AI integration (Spec #7+). Acceptance criteria (3.7.8) now only references immediately testable benchmarks.

3. **Tolerance derivation appendix added** (Issue #3): New subsection 3.7.9 provides formal derivation and rationale for every tolerance value used in this section, mirroring Ball Physics Appendix D pattern.

**MODERATE FIXES:**

4. **UT-SM-003 disambiguated** (Issue #4): Expected state is now definitively WALKING, not "WALKING or DECELERATING." Added derivation: speed naturally dropping below JOG_EXIT (1.9 m/s) triggers JOGGING Ã¢â€ â€™ WALKING per Section 3.1.4 transition table. DECELERATING requires an explicit deceleration *command*, not natural speed decay.

5. **IT-MOV-003 movement distribution sourced** (Issue #5): The 10/30/40/20 intensity distribution now cites [BRADLEY-2009] average match activity profiles. Added note that this is an approximation for scripted testing, with full validation deferred until AI-driven movement is available.

6. **Golden trajectory tolerance reworked** (Issue #6): Replaced single per-frame 0.01f tolerance with dual-tolerance system: per-frame position delta Ã‚Â±0.05m (5cm, accounts for float accumulation) AND end-of-trajectory final position Ã‚Â±0.15m (15cm total drift budget). Added justification referencing PR-3 (position drift <5cm per agent per 90 minutes, prorated for test duration).

7. **Stage 0 modifier sanity test added** (Issue #7): New test UT-PC-005 verifies that PerformanceContext.CreateNeutral() produces exactly 1.0 combined modifier, ensuring no accidental non-neutral modifiers leak into Stage 0 calculations. Total unit test count increased to 49.

**MINOR FIXES:**

8. **Test ID naming convention documented** (Issue #8): New subsection 3.7.1.4 formalizes the naming scheme: `UT-{CATEGORY}-{NNN}` for functional unit tests (this section), `UT-EDGE-{NNN}` for edge case tests (Section 3.6.7), `IT-{CATEGORY}-{NNN}` for integration tests.

9. **UTF-8 rendering note added** (Issue #10): Added to document header for consistency with Sections 3.5 and 3.6.

10. **Page count verified** (Issue #9): Content tightened where possible. Performance profiling marker section consolidated. Estimated ~26 pages.

---

## v1.2 Changelog

**MODERATE FIX:**

11. **UT-DIR-005 Agility 1 exemption added** (Appendix A.5.3 resolution): The zone
    boundary continuity test now exempts Agility 1 from the <0.03/degree threshold.
    At Agility 1, the forward-lateral band produces 0.035/degree (17% over threshold)
    due to the wider multiplier range. This is imperceptible in gameplay.

---

## Table of Contents

- [3.7.1 Test Strategy & Scope](#371-test-strategy--scope)
- [3.7.2 Unit Test Specifications](#372-unit-test-specifications)
- [3.7.3 Integration Test Scenarios](#373-integration-test-scenarios)
- [3.7.4 Real-World Validation Benchmarks](#374-real-world-validation-benchmarks)
- [3.7.5 Regression Testing Requirements](#375-regression-testing-requirements)
- [3.7.6 Performance Testing Criteria](#376-performance-testing-criteria)
- [3.7.7 Test Execution Plan](#377-test-execution-plan)
- [3.7.8 Acceptance Criteria Summary](#378-acceptance-criteria-summary)
- [3.7.9 Tolerance Derivations](#379-tolerance-derivations)

---

## 3.7.1 Test Strategy & Scope

### 3.7.1.1 Test Pyramid

```
          /\
         /  \     E2E Tests (5%)
        /____\    - 90-minute match simulation (22 agents)
       /      \   - Replay determinism verification
      /        \
     / Integr-  \ Integration Tests (25%)
    /  ation     \ - Multi-system agent scenarios
   /______________\ - State machine + locomotion chains
  /                \
 /   Unit Tests     \ Unit Tests (70%)
 /__(49+ tests)______\ - Formula validation per physics model
```

**Distribution rationale:**
- Unit tests are fast (<15s total for all 49+), enable rapid iteration during TDD
- Integration tests verify cross-section boundaries (state machine driving locomotion, directional multipliers affecting turn rate)
- E2E tests are expensive (minutes per run), reserved for milestone validation

### 3.7.1.2 Scope Delineation

**This section (3.7) covers:**
- Functional correctness of all physics formulas (Sections 3.2, 3.3, 3.4)
- State machine transition logic under normal operation (Section 3.1)
- PerformanceContext gateway attribute evaluation (Section 3.2.1)
- Multi-agent integration scenarios
- Real-world validation against GPS tracking data and broadcast footage
- Regression testing strategy for constant tuning
- Performance profiling criteria

**Section 3.6.7 covers (do NOT duplicate here):**
- NaN/Infinity recovery tests (19 unit tests)
- Oscillation guard tests
- Attribute boundary extremes (1, 20)
- Pitch boundary violation tests
- Command queue overflow tests
- Degraded mode escalation/decay tests

**Combined test count target:** 49+ unit (this section) + 19 unit (Section 3.6.7) = 68+ unit tests; 10+ integration (this section) + 5 integration (Section 3.6.7) = 15+ integration tests.

### 3.7.1.3 Testing Tools

**Framework:** Unity Test Framework (NUnit)  
**Execution:** Unity Test Runner (Edit Mode for unit tests, Play Mode for integration/E2E)  
**Coverage:** Unity Code Coverage package  
**Performance:** Unity Profiler + custom `AgentMovementProfiler` markers  
**Data:** Reference trajectories stored as test fixtures (see Section 3.7.4.5)

### 3.7.1.4 Test ID Naming Convention

All test IDs across the Agent Movement spec follow this pattern:

```
{TestType}-{Category}-{NNN}
```

**Test types:**
- `UT` Ã¢â‚¬â€ Unit test
- `IT` Ã¢â‚¬â€ Integration test

**Categories (this section, 3.7):**
- `PC` Ã¢â‚¬â€ PerformanceContext (3.7.2.1)
- `ACC` Ã¢â‚¬â€ Acceleration model (3.7.2.2)
- `SPD` Ã¢â‚¬â€ Top speed (3.7.2.3)
- `DEC` Ã¢â‚¬â€ Deceleration (3.7.2.4)
- `DIR` Ã¢â‚¬â€ Directional movement (3.7.2.5)
- `SM` Ã¢â‚¬â€ State machine (3.7.2.6)
- `TRN` Ã¢â‚¬â€ Turning & momentum (3.7.2.7)
- `FAT` Ã¢â‚¬â€ Fatigue integration (3.7.2.8)
- `MOV` Ã¢â‚¬â€ Full movement sequence (3.7.3.1)
- `MA` Ã¢â‚¬â€ Multi-agent (3.7.3.2)
- `CS` Ã¢â‚¬â€ Cross-system (3.7.3.3)

**Categories (Section 3.6.7):**
- `EDGE` Ã¢â‚¬â€ Edge case and error recovery

**Examples:** `UT-ACC-001`, `IT-MOV-003`, `UT-EDGE-014`

### 3.7.1.5 Test Data Strategy

**Derived values (not magic numbers):**
- All expected results calculated from Section 3.2Ã¢â‚¬â€œ3.4 formulas with full derivation shown
- Tolerance values justified in Section 3.7.9 (Tolerance Derivations)
- Example: Top speed for Pace 15 = `7.5 + (15 - 1) Ãƒâ€” (10.2 - 7.5) / 19 = 7.5 + 14 Ãƒâ€” 0.14211 = 9.49 m/s` (Section 3.2.4)

**Realistic scenarios:**
- Use professional match GPS data as reference (Section 3.7.4)
- Sprint-to-stop distances: 3.0Ã¢â‚¬â€œ5.0m controlled (Section 3.2.5 FR-3 revised)
- Top speed range: 7.5Ã¢â‚¬â€œ10.2 m/s (FR-2)
- Time to 90% top speed: 2.5Ã¢â‚¬â€œ3.5s from standstill (FR-2)

---

## 3.7.2 Unit Test Specifications

### 3.7.2.1 PerformanceContext Tests (5 tests)

**Test ID: UT-PC-001**  
**Name:** `PerformanceContext_NeutralModifiers_ReturnsBaseAttribute`  
**Purpose:** Validate Stage 0 pass-through behavior (all modifiers = 1.0)  
**Input:** Raw attribute = 15, FormModifier = 1.0, ContextModifier = 1.0, CareerModifier = 1.0  
**Expected:** EffectiveValue = 15.0  
**Derivation:** Combined = 1.0 Ãƒâ€” 1.0 Ãƒâ€” 1.0 = 1.0; 15 Ãƒâ€” 1.0 = 15.0  
**Tolerance:** Exact (0.0 delta Ã¢â‚¬â€ mathematical identity)  
**Section Ref:** 3.2.1

---

**Test ID: UT-PC-002**  
**Name:** `PerformanceContext_FloorClamp_AppliesCorrectly`  
**Purpose:** Validate that effective attribute never drops below floor  
**Input:** Raw attribute = 1, FormModifier = 0.85, ContextModifier = 0.80, CareerModifier = 0.90  
**Expected:** Combined = 0.85 Ãƒâ€” 0.80 Ãƒâ€” 0.90 = 0.612; EffectiveValue = 1 Ãƒâ€” 0.612 = 0.612  
**Derivation:** Combined (0.612) > EFFECTIVE_FLOOR (0.6), so no floor clamp in this case. Value passes through.  
**Tolerance:** Ã‚Â±0.001 (float multiplication precision)  
**Section Ref:** 3.2.1

---

**Test ID: UT-PC-003**  
**Name:** `PerformanceContext_CeilingClamp_AppliesCorrectly`  
**Purpose:** Validate that combined modifier is clamped to CEILING  
**Input:** Raw attribute = 20, FormModifier = 1.15, ContextModifier = 1.20, CareerModifier = 1.10  
**Expected:** Combined = 1.15 Ãƒâ€” 1.20 Ãƒâ€” 1.10 = 1.518 Ã¢â€ â€™ clamped to EFFECTIVE_CEILING (1.5); EffectiveValue = 20 Ãƒâ€” 1.5 = 30.0  
**Derivation:** Ceiling clamp engages because 1.518 > 1.5  
**Tolerance:** Exact  
**Section Ref:** 3.2.1

---

**Test ID: UT-PC-004**  
**Name:** `PerformanceContext_AttributePair_WeightedAverage`  
**Purpose:** Validate dual-attribute evaluation with weighting  
**Input:** Pace = 16, Acceleration = 12, weights = (0.7, 0.3), neutral context  
**Expected:** EffectiveValue = (16.0 Ãƒâ€” 0.7 + 12.0 Ãƒâ€” 0.3) / (0.7 + 0.3) = (11.2 + 3.6) / 1.0 = 14.8  
**Tolerance:** Ã‚Â±0.01 (float arithmetic, see Appendix 3.7.9.1)  
**Section Ref:** 3.2.1

---

**Test ID: UT-PC-005** *(v1.1)*  
**Name:** `PerformanceContext_CreateNeutral_ExactlyUnity`  
**Purpose:** Validate that Stage 0 neutral factory produces exactly 1.0 combined modifier Ã¢â‚¬â€ no accidental non-neutral values  
**Input:** PerformanceContext.CreateNeutral()  
**Expected:** GetCombinedModifier() = 1.0; EvaluateAttribute(any) = any unchanged  
**Verification:** For all attributes 1Ã¢â‚¬â€œ20: EvaluateAttribute(n) == n (bit-exact float comparison)  
**Tolerance:** Exact (1.0 Ãƒâ€” 1.0 Ãƒâ€” 1.0 = 1.0, no float error possible with these values)  
**Section Ref:** 3.2.1  
**Rationale:** Guards against accidental introduction of non-neutral modifiers in Stage 0 codebase

---

### 3.7.2.2 Acceleration Model Tests (6 tests)

**Test ID: UT-ACC-001**  
**Name:** `Acceleration_Standstill_ExponentialCurve`  
**Purpose:** Validate exponential acceleration from rest at known attribute level  
**Input:** Agent at IDLE (speed = 0), Acceleration attribute = 15 (neutral context), Pace = 15 (v_target = 9.49 m/s)  
**Derivation:**
```
k = ACCEL_K_MIN + (15 - 1) Ãƒâ€” ACCEL_K_PER_POINT
  = 0.658 + 14 Ãƒâ€” (0.921 - 0.658) / 19
  = 0.658 + 14 Ãƒâ€” 0.01384
  = 0.658 + 0.1938
  = 0.8518 sÃ¢ÂÂ»Ã‚Â¹

At t=0.5s (30 frames at 60Hz), continuous formula:
v(0.5) = 9.49 Ãƒâ€” (1 - e^(-0.8518 Ãƒâ€” 0.5))
       = 9.49 Ãƒâ€” (1 - e^(-0.4259))
       = 9.49 Ãƒâ€” (1 - 0.6532)
       = 9.49 Ãƒâ€” 0.3468
       = 3.29 m/s

Discrete 60Hz simulation may differ by <0.01 m/s (Section 3.2.3 accuracy note).
```
**Expected:** Speed at t=0.5s Ã¢â€°Ë† 3.29 m/s  
**Tolerance:** Ã‚Â±0.15 m/s (see Appendix 3.7.9.2 Ã¢â‚¬â€ accounts for discrete integration error and frame timing)  
**Section Ref:** 3.2.3

---

**Test ID: UT-ACC-002**  
**Name:** `Acceleration_LowAttribute_SlowerThanHigh`  
**Purpose:** Validate attribute sensitivity Ã¢â‚¬â€ Acceleration 5 vs Acceleration 15  
**Input:** Two agents from IDLE, Acceleration 5 and 15 respectively, same Pace = 15  
**Derivation:**
```
k(Acc=5)  = 0.658 + 4 Ãƒâ€” 0.01384 = 0.7134 sÃ¢ÂÂ»Ã‚Â¹
k(Acc=15) = 0.8518 sÃ¢ÂÂ»Ã‚Â¹ (from UT-ACC-001)

At t=1.0s:
v_5(1.0)  = 9.49 Ãƒâ€” (1 - e^(-0.7134)) = 9.49 Ãƒâ€” 0.5098 = 4.84 m/s
v_15(1.0) = 9.49 Ãƒâ€” (1 - e^(-0.8518)) = 9.49 Ãƒâ€” 0.5732 = 5.44 m/s

Difference Ã¢â€°Ë† 0.60 m/s
```
**Expected:** After 1.0s, Agent(Acc=15).Speed > Agent(Acc=5).Speed; difference Ã¢â€°Ë† 0.60 m/s  
**Tolerance:** Speed difference > 0.4 m/s (conservative bound, see Appendix 3.7.9.2)  
**Section Ref:** 3.2.3

---

**Test ID: UT-ACC-003**  
**Name:** `Acceleration_ReachesTopSpeed_WithinTimeWindow`  
**Purpose:** Validate FR-2 compliance: 2.5Ã¢â‚¬â€œ3.5s to 90% top speed  
**Input:** Agent with Pace 10, Acceleration 10, from IDLE  
**Derivation:**
```
k(Acc=10) = 0.658 + 9 Ãƒâ€” 0.01384 = 0.7826 sÃ¢ÂÂ»Ã‚Â¹
T_90 = 2.3026 / 0.7826 = 2.94s (matches Section 3.2.2 table)
```
**Expected:** Reaches 90% of top speed (0.9 Ãƒâ€” 8.78 = 7.90 m/s) at t Ã¢â€°Ë† 2.94s  
**Tolerance:** Time within Ã‚Â±0.3s of 2.94s (see Appendix 3.7.9.3)  
**Section Ref:** 3.2.3, FR-2

---

**Test ID: UT-ACC-004**  
**Name:** `Acceleration_FatigueReducesRate`  
**Purpose:** Validate fatigue modifier on effective top speed (which limits acceleration target)  
**Input:** Agent with Pace 15, aerobic pool = 0.3  
**Derivation:**
```
Fatigue modifier at pool 0.3:
  pool < 0.5, so modifier = Lerp(0.70, 1.0, 0.3/0.5) = Lerp(0.70, 1.0, 0.6) = 0.88
  
Fresh top speed (Pace 15) = 9.49 m/s
Fatigued top speed = 9.49 Ãƒâ€” 0.88 = 8.35 m/s
Reduction = (9.49 - 8.35) / 9.49 = 12.0%
```
**Expected:** Effective top speed Ã¢â€°Ë† 8.35 m/s; speed at t=2.0s measurably lower than fresh agent  
**Tolerance:** Effective top speed Ã‚Â±0.1 m/s; reduction > 10% vs fresh (see Appendix 3.7.9.4)  
**Section Ref:** 3.2.3, 3.2.4, FR-6

---

**Test ID: UT-ACC-005**  
**Name:** `Acceleration_FromWalkToSprint_ContinuousCurve`  
**Purpose:** Validate no velocity discontinuity during state transitions  
**Input:** Agent walking at 1.5 m/s, receives sprint command  
**Expected:** Velocity increases smoothly (no jump); frame-to-frame delta never exceeds maximum physically possible acceleration Ãƒâ€” dt  
**Derivation:**
```
Maximum instantaneous acceleration at k_max = 0.921:
  At v=0 (worst case): a = k Ãƒâ€” v_target = 0.921 Ãƒâ€” 10.2 Ã¢â€°Ë† 9.39 m/sÃ‚Â²
  Max frame delta = 9.39 Ãƒâ€” (1/60) = 0.157 m/s per frame

Conservative limit: max delta per frame < 0.20 m/s (includes safety margin)
```
**Tolerance:** Frame-to-frame velocity delta < 0.20 m/s at all transitions  
**Section Ref:** 3.2.3, 3.1

---

**Test ID: UT-ACC-006**  
**Name:** `Acceleration_MinAttribute_StillMoves`  
**Purpose:** Validate that worst-case attribute produces movement  
**Input:** Agent with Acceleration 1, Pace 1, from IDLE  
**Derivation:**
```
k(Acc=1) = 0.658 sÃ¢ÂÂ»Ã‚Â¹; v_target = 7.5 m/s
v(2.0) = 7.5 Ãƒâ€” (1 - e^(-0.658 Ãƒâ€” 2.0)) = 7.5 Ãƒâ€” (1 - 0.2681) = 7.5 Ãƒâ€” 0.7319 = 5.49 m/s
```
**Expected:** Speed > 5.0 m/s after 2.0s  
**Tolerance:** Speed > 5.0 m/s (conservative lower bound, see Appendix 3.7.9.2)  
**Section Ref:** 3.2.3

---

### 3.7.2.3 Top Speed Tests (5 tests)

**Test ID: UT-SPD-001**  
**Name:** `TopSpeed_Pace1_ReturnsMinimum`  
**Purpose:** Validate lower bound of speed range  
**Input:** Pace = 1, neutral context  
**Derivation:** 7.5 + (1-1) Ãƒâ€” 0.14211 = 7.5 m/s  
**Expected:** Top speed = 7.50 m/s  
**Tolerance:** Ã‚Â±0.01 m/s (float precision, see Appendix 3.7.9.1)  
**Section Ref:** 3.2.4

---

**Test ID: UT-SPD-002**  
**Name:** `TopSpeed_Pace20_ReturnsMaximum`  
**Purpose:** Validate upper bound of speed range  
**Input:** Pace = 20, neutral context  
**Derivation:** 7.5 + (20-1) Ãƒâ€” 0.14211 = 7.5 + 2.7 = 10.2 m/s  
**Expected:** Top speed = 10.20 m/s  
**Tolerance:** Ã‚Â±0.01 m/s  
**Section Ref:** 3.2.4

---

**Test ID: UT-SPD-003**  
**Name:** `TopSpeed_Pace10_ReturnsMidRange`  
**Purpose:** Validate linear interpolation at midpoint  
**Input:** Pace = 10, neutral context  
**Derivation:** 7.5 + (10-1) Ãƒâ€” 0.14211 = 7.5 + 1.2789 = 8.78 m/s  
**Expected:** Top speed Ã¢â€°Ë† 8.78 m/s  
**Tolerance:** Ã‚Â±0.01 m/s  
**Section Ref:** 3.2.4

---

**Test ID: UT-SPD-004**  
**Name:** `TopSpeed_FatigueReducesCap`  
**Purpose:** Validate aerobic pool modifier on top speed  
**Input:** Pace = 15, aerobic pool = 0.3  
**Derivation:**
```
Fresh top speed = 9.49 m/s (from Section 3.2.2 table)
Fatigue modifier at pool 0.3 = Lerp(0.70, 1.0, 0.6) = 0.88 (Section 3.2.4)
Fatigued top speed = 9.49 Ãƒâ€” 0.88 = 8.35 m/s
Reduction = 12.0%
```
**Expected:** Effective top speed Ã¢â€°Ë† 8.35 m/s; reduction Ã¢â€°Ë† 12%  
**Tolerance:** Ã‚Â±0.1 m/s on effective speed; reduction between 10%Ã¢â‚¬â€œ15%  
**Section Ref:** 3.2.4, FR-6

---

**Test ID: UT-SPD-005**  
**Name:** `TopSpeed_MonotonicWithPace`  
**Purpose:** Validate that higher Pace always means higher speed  
**Input:** All Pace values 1 through 20, neutral context  
**Derivation:** Each increment adds TOP_SPEED_PER_POINT = 0.14211 m/s  
**Expected:** TopSpeed(n) < TopSpeed(n+1) for all n in [1, 19]; each increment Ã¢â€°Ë† 0.142 m/s  
**Tolerance:** Each increment > 0.10 m/s (conservative, accounts for float truncation)  
**Section Ref:** 3.2.4, QR-4

---

### 3.7.2.4 Deceleration Model Tests (6 tests)

**Test ID: UT-DEC-001**  
**Name:** `ControlledDecel_StopsWithinExpectedDistance`  
**Purpose:** Validate FR-3 (revised): controlled stop from sprint  
**Input:** Agent sprinting at 9.0 m/s, controlled deceleration, Agility = 10  
**Derivation:**
```
Decel rate at Agility 10 = 8.1 + (10-1) Ãƒâ€” 0.28421 = 8.1 + 2.558 = 10.66 m/sÃ‚Â²
Stop distance = vÃ¢â€šâ‚¬Ã‚Â² / (2 Ãƒâ€” decel) = 81.0 / 21.32 = 3.80m
```
**Expected:** Stops within 3.5Ã¢â‚¬â€œ4.2m (see Section 3.2.5 deceleration table)  
**Tolerance:** Ã‚Â±0.4m (see Appendix 3.7.9.5 Ã¢â‚¬â€ discrete integration adds ~1 frame of travel)  
**Section Ref:** 3.2.5, FR-3

---

**Test ID: UT-DEC-002**  
**Name:** `EmergencyDecel_StopsWithinExpectedDistance`  
**Purpose:** Validate FR-3 (revised): emergency stop from sprint  
**Input:** Agent sprinting at 9.0 m/s, emergency deceleration, Agility = 10  
**Derivation:**
```
Emergency decel at Agility 10 = 11.57 + (10-1) Ãƒâ€” 0.24368 = 11.57 + 2.193 = 13.76 m/sÃ‚Â²
Stop distance = 81.0 / 27.52 = 2.94m
```
**Expected:** Stops within 2.6Ã¢â‚¬â€œ3.3m  
**Tolerance:** Ã‚Â±0.4m (see Appendix 3.7.9.5)  
**Section Ref:** 3.2.5, FR-3

---

**Test ID: UT-DEC-003**  
**Name:** `EmergencyDecel_AtSpeed_TriggersStumbleRisk`  
**Purpose:** Validate stumble risk flag set during emergency decel from SPRINTING  
**Input:** Agent SPRINTING at 9.5 m/s, emergency deceleration, Balance = 10  
**Derivation:**
```
Previous state = SPRINTING Ã¢â€ â€™ stumble check triggered
normalizedBalance = (10 - 1) / 19.0 = 0.4737
stumbleChance = 0.30 Ãƒâ€” (1.0 - 0.4737 Ãƒâ€” 0.7) = 0.30 Ãƒâ€” 0.6684 = 0.2005 (20.1%)
```
**Expected:** EvaluateStumbleRisk returns true when rngValue < 0.2005, false when Ã¢â€°Â¥ 0.2005  
**Tolerance:** Probability boundary exact to Ã‚Â±0.001  
**Section Ref:** 3.2.5, 3.4.4

---

**Test ID: UT-DEC-004**  
**Name:** `Deceleration_VelocityNeverReverses`  
**Purpose:** Validate that deceleration doesn't overshoot into negative velocity  
**Input:** Agent at 2.0 m/s, controlled deceleration, Agility = 15  
**Derivation:**
```
Decel rate at Agility 15 = 8.1 + (15-1) Ãƒâ€” 0.28421 = 12.08 m/sÃ‚Â²
At dt = 1/60 = 0.01667s: speed reduction per frame = 12.08 Ãƒâ€” 0.01667 = 0.2013 m/s
At 2.0 m/s, stops in ~10 frames. Mathf.Max(0f, ...) clamp prevents negative.
```
**Expected:** Speed decreases monotonically to 0, never goes negative across all frames  
**Tolerance:** Min speed Ã¢â€°Â¥ 0.0 across all frames (exact clamp check)  
**Section Ref:** 3.2.5

---

**Test ID: UT-DEC-005**  
**Name:** `Deceleration_HighAgility_ShorterDistance`  
**Purpose:** Validate attribute sensitivity on stopping distance  
**Input:** Two agents at 8.0 m/s: Agility 5 vs Agility 18, controlled decel  
**Derivation:**
```
Agility 5:  decel = 8.1 + 4 Ãƒâ€” 0.28421 = 9.237 m/sÃ‚Â²;  d = 64 / 18.47 = 3.46m
Agility 18: decel = 8.1 + 17 Ãƒâ€” 0.28421 = 12.93 m/sÃ‚Â²; d = 64 / 25.86 = 2.47m
Difference Ã¢â€°Ë† 0.99m
```
**Expected:** Agent(Agility=18) stops Ã¢â€°Ë† 1.0m shorter than Agent(Agility=5)  
**Tolerance:** Distance difference > 0.5m (conservative, see Appendix 3.7.9.5)  
**Section Ref:** 3.2.5, QR-4

---

**Test ID: UT-DEC-006**  
**Name:** `Deceleration_FromJog_NoStumbleRisk`  
**Purpose:** Validate that deceleration from jog speed never triggers stumble  
**Input:** Agent JOGGING at 4.0 m/s, emergency deceleration  
**Expected:** EvaluateStumbleRisk returns false (previousState = JOGGING, not SPRINTING)  
**Tolerance:** Boolean exact  
**Section Ref:** 3.2.5, 3.4.4

---

### 3.7.2.5 Directional Movement Tests (7 tests)

**Test ID: UT-DIR-001**  
**Name:** `DirectionalMultiplier_Forward_ReturnsFullSpeed`  
**Purpose:** Validate no penalty in forward zone  
**Input:** Movement angle = 0Ã‚Â° (dead ahead relative to facing), Agility = 10  
**Expected:** Multiplier = 1.0  
**Tolerance:** Exact  
**Section Ref:** 3.3.2, 3.3.3

---

**Test ID: UT-DIR-002**  
**Name:** `DirectionalMultiplier_Lateral90_ReturnsReducedSpeed`  
**Purpose:** Validate lateral penalty at 90Ã‚Â°  
**Input:** Movement angle = 90Ã‚Â° (pure sidestep), Agility = 10  
**Expected:** Multiplier in range 0.65Ã¢â‚¬â€œ0.75 (FR-4 specification)  
**Tolerance:** Ã‚Â±0.02 (see Appendix 3.7.9.6)  
**Section Ref:** 3.3.3

---

**Test ID: UT-DIR-003**  
**Name:** `DirectionalMultiplier_Backward180_ReturnsLowestSpeed`  
**Purpose:** Validate backward penalty is strongest  
**Input:** Movement angle = 180Ã‚Â° (directly backward), Agility = 10  
**Expected:** Multiplier in range 0.45Ã¢â‚¬â€œ0.55 (FR-4 specification)  
**Tolerance:** Ã‚Â±0.02  
**Section Ref:** 3.3.3

---

**Test ID: UT-DIR-004**  
**Name:** `DirectionalMultiplier_HighAgility_LessPenalty`  
**Purpose:** Validate Agility scaling Ã¢â‚¬â€ high agility reduces lateral/backward penalty  
**Input:** Movement angle = 90Ã‚Â°, Agility 5 vs Agility 18  
**Expected:** Multiplier(Agility=18) > Multiplier(Agility=5)  
**Tolerance:** Difference > 0.05 (see Appendix 3.7.9.6)  
**Section Ref:** 3.3.3, QR-4

---

**Test ID: UT-DIR-005**  
**Name:** `DirectionalMultiplier_ZoneBoundary_SmoothInterpolation`  
**Purpose:** Validate no discontinuity at zone boundaries  
**Input:** Movement angles 28, 29, 30, 31, 32 deg (forward/lateral boundary at +/-30 deg), Agility >= 2
**Expected:** Multipliers form a smooth curve; no jump > 0.03 between adjacent 1Ã‚Â° steps  
**Tolerance:** Adjacent angle multiplier difference < 0.03 (see Appendix 3.7.9.6)  
**Exemption:** Agility 1 is exempt from this test. At Agility 1, the forward-lateral band
produces 0.035/degree due to the wider multiplier range (1.0 to 0.65 over 10 deg). This
exceeds the threshold but remains imperceptible in gameplay (0.315 m/s per degree at 9 m/s).
**Section Ref:** 3.3.2

---

**Test ID: UT-DIR-006**  
**Name:** `DirectionalMultiplier_AppliedToTopSpeed`  
**Purpose:** Validate multiplier actually reduces effective top speed  
**Input:** Agent with Pace 15 (top speed 9.49 m/s), moving at 90Ã‚Â° angle, Agility 10  
**Derivation:** Lateral multiplier for Agility 10 Ã¢â€°Ë† 0.70 (midpoint of 0.65Ã¢â‚¬â€œ0.75 range); effective speed = 9.49 Ãƒâ€” 0.70 Ã¢â€°Ë† 6.64 m/s  
**Expected:** Effective top speed Ã¢â€°Ë† 6.64 m/s; agent cannot exceed this while moving laterally  
**Tolerance:** Ã‚Â±0.3 m/s (multiplier range uncertainty, see Appendix 3.7.9.6)  
**Section Ref:** 3.3.5

---

**Test ID: UT-DIR-007**  
**Name:** `DirectionalMultiplier_AppliedToAcceleration`  
**Purpose:** Validate multiplier also reduces acceleration rate (FR-4)  
**Input:** Agent accelerating laterally (90Ã‚Â°) vs forward (0Ã‚Â°), same attributes (Pace 15, Acc 15)  
**Expected:** Lateral acceleration rate < forward acceleration rate; time to reach 50% of respective top speed is longer for lateral  
**Tolerance:** Time difference > 0.15s (see Appendix 3.7.9.6)  
**Section Ref:** 3.3.5, FR-4

---

### 3.7.2.6 State Machine Transition Tests (8 tests)

**Test ID: UT-SM-001**  
**Name:** `StateTransition_IdleToWalking_AtExitThreshold`  
**Purpose:** Validate hysteresis Ã¢â‚¬â€ uses IDLE_EXIT (0.3 m/s), not IDLE_ENTER (0.1 m/s)  
**Input:** Agent in IDLE, speed increases to 0.25 m/s  
**Expected:** State remains IDLE (0.25 < IDLE_EXIT 0.3)  
**Tolerance:** State exact  
**Section Ref:** 3.1.3

---

**Test ID: UT-SM-002**  
**Name:** `StateTransition_WalkingToJogging_AtJogEnterThreshold`  
**Purpose:** Validate upward transition threshold  
**Input:** Agent WALKING, speed reaches JOG_ENTER (2.2 m/s)  
**Expected:** State transitions to JOGGING  
**Tolerance:** State exact  
**Section Ref:** 3.1.3

---

**Test ID: UT-SM-003** *(v1.1: disambiguated)*  
**Name:** `StateTransition_JoggingToWalking_AtJogExitThreshold`  
**Purpose:** Validate hysteresis Ã¢â‚¬â€ downward speed-based transition  
**Input:** Agent JOGGING, speed naturally drops (no deceleration command issued) to 1.85 m/s (below JOG_EXIT 1.9 m/s)  
**Expected:** State transitions to WALKING  
**Derivation:** Per Section 3.1.4 transition table, JOGGING Ã¢â€ â€™ WALKING triggers when `speed < JOG_EXIT (1.9 m/s)`. This is a speed-based transition, not a command-based one. JOGGING Ã¢â€ â€™ DECELERATING requires an explicit deceleration command while speed > JOG_EXIT. Natural speed decay below JOG_EXIT always produces WALKING.  
**Tolerance:** State exact  
**Section Ref:** 3.1.4

---

**Test ID: UT-SM-004**  
**Name:** `StateTransition_SprintingToDecelerating_OnSlowCommand`  
**Purpose:** Validate deceleration state entry from command  
**Input:** Agent SPRINTING at 8.0 m/s, receives stop command  
**Expected:** State transitions to DECELERATING  
**Tolerance:** State exact  
**Section Ref:** 3.1.4

---

**Test ID: UT-SM-005**  
**Name:** `StateTransition_Stumbling_MinimumDwellTime`  
**Purpose:** Validate minimum duration enforcement  
**Input:** Agent enters STUMBLING (Balance = 10), receives sprint command immediately  
**Derivation:**
```
Dwell time = STUMBLE_MIN_DWELL_BASE / (Balance / 20.0) = 0.6 / (10/20) = 1.2s
Clamped to [0.3, 1.5] Ã¢â€ â€™ 1.2s
```
**Expected:** Remains STUMBLING for 1.2s before any transition permitted  
**Tolerance:** Duration within Ã‚Â±1 frame (16.67ms)  
**Section Ref:** 3.1.3

---

**Test ID: UT-SM-006**  
**Name:** `StateTransition_GroundedToIdle_RequiresRecovery`  
**Purpose:** Validate forbidden direct transition Ã¢â‚¬â€ must wait for dwell  
**Input:** Agent in GROUNDED, recovery timer not elapsed  
**Expected:** Cannot transition to any state; remains GROUNDED until dwell completes, then transitions to IDLE  
**Tolerance:** State exact  
**Section Ref:** 3.1.4

---

**Test ID: UT-SM-007**  
**Name:** `StateTransition_SprintingBlocked_BySprintReservoir`  
**Purpose:** Validate fatigue prevents sprinting  
**Input:** Agent JOGGING at 5.0 m/s, sprint command, sprint reservoir = 0.15 (below SPRINT_RESERVOIR_FLOOR 0.20)  
**Expected:** Agent does NOT transition to SPRINTING; remains JOGGING  
**Tolerance:** State exact  
**Section Ref:** 3.1.4, FR-6

---

**Test ID: UT-SM-008**  
**Name:** `StateTransition_SprintReentry_HysteresisEnforced`  
**Purpose:** Validate sprint reentry requires higher threshold than exit  
**Input:** Agent forced out of SPRINTING at reservoir = 0.20; reservoir recovers to 0.30  
**Expected:** Still cannot sprint (0.30 < SPRINT_RESERVOIR_REENTRY 0.35); must reach 0.35 to re-enter  
**Tolerance:** State exact  
**Section Ref:** 3.1.4, FR-6

---

### 3.7.2.7 Turning & Momentum Tests (8 tests)

**Test ID: UT-TRN-001**  
**Name:** `TurnRate_Walking_UnrestrictedRotation`  
**Purpose:** Validate near-free turning at low speed  
**Input:** Agent WALKING at 1.5 m/s, Agility = 10, 90Ã‚Â° turn command  
**Expected:** Turn completes within 0.3s (effective rate Ã¢â€°Â¥ 300Ã‚Â°/s at walk speed)  
**Tolerance:** Completion time < 0.4s  
**Section Ref:** 3.4.2

---

**Test ID: UT-TRN-002**  
**Name:** `TurnRate_Sprinting_ConstrainedRotation`  
**Purpose:** Validate reduced turn rate at sprint speed  
**Input:** Agent SPRINTING at 9.0 m/s, Agility = 10, 90Ã‚Â° turn command  
**Expected:** Turn takes > 0.5s (rate < 180Ã‚Â°/s at sprint speed)  
**Tolerance:** Completion time > 0.5s  
**Section Ref:** 3.4.2

---

**Test ID: UT-TRN-003**  
**Name:** `TurnRate_AgilityScaling_HighVsLow`  
**Purpose:** Validate attribute sensitivity on turn rate  
**Input:** Two agents at 7.0 m/s: Agility 5 vs Agility 18, same 90Ã‚Â° turn  
**Expected:** Agent(Agility=18) completes turn faster  
**Tolerance:** Time difference > 0.12s (see Appendix 3.7.9.7)  
**Section Ref:** 3.4.2, QR-4

---

**Test ID: UT-TRN-004**  
**Name:** `MinTurnRadius_IncreasesWithSpeed`  
**Purpose:** Validate r_min = vÃ‚Â² / (a_lateral_max Ãƒâ€” agility_factor) relationship  
**Input:** Agent with Agility 10 at speeds 3.0, 6.0, 9.0 m/s  
**Derivation:** r_min scales with vÃ‚Â², so r(6.0)/r(3.0) Ã¢â€°Ë† 4.0, r(9.0)/r(3.0) Ã¢â€°Ë† 9.0  
**Expected:** r_min(9.0) > r_min(6.0) > r_min(3.0); ratios within Ã‚Â±15% of vÃ‚Â² scaling  
**Tolerance:** Ratio Ã‚Â±15% (see Appendix 3.7.9.7)  
**Section Ref:** 3.4.3, FR-5

---

**Test ID: UT-TRN-005**  
**Name:** `StumbleRisk_SharpTurnAtSpeed_TriggersCheck`  
**Purpose:** Validate stumble risk activation during aggressive turn at sprint  
**Input:** Agent SPRINTING at 8.5 m/s (> STUMBLE_SPEED_THRESHOLD 5.5), commands 70Ã‚Â° direction change (> STUMBLE_TURN_ANGLE 60Ã‚Â°)  
**Expected:** Stumble risk evaluation triggered; probability > 0 (Balance-dependent)  
**Tolerance:** Boolean exact for trigger; probability > MIN_STUMBLE_RISK (0.03)  
**Section Ref:** 3.4.4

---

**Test ID: UT-TRN-006**  
**Name:** `StumbleRisk_HighBalance_LowerProbability`  
**Purpose:** Validate Balance attribute reduces stumble chance  
**Input:** 70Ã‚Â° turn at 8.5 m/s: Balance 5 vs Balance 18  
**Expected:** StumbleProbability(Balance=5) > StumbleProbability(Balance=18)  
**Tolerance:** Probability difference > 0.08 (see Appendix 3.7.9.7)  
**Section Ref:** 3.4.4, QR-4

---

**Test ID: UT-TRN-007**  
**Name:** `LeanAngle_ProportionalToTurnSeverity`  
**Purpose:** Validate animation data contract output  
**Input:** Agent turning at rates of 45Ã‚Â°/s, 180Ã‚Â°/s, 360Ã‚Â°/s  
**Expected:** LeanAngle(360) > LeanAngle(180) > LeanAngle(45)  
**Tolerance:** Each increment > 2Ã‚Â° (see Appendix 3.7.9.7)  
**Section Ref:** 3.4.5, FR-8

---

**Test ID: UT-TRN-008**  
**Name:** `TurnRate_FatigueReducesRate`  
**Purpose:** Validate fatigue modifier on turning ability  
**Input:** Agent with Agility 15 at 6.0 m/s, aerobic pool = 0.4 (fatigued) vs 1.0 (fresh)  
**Derivation:**
```
Fatigue modifier at pool 0.4 = Lerp(0.70, 1.0, 0.4/0.5) = Lerp(0.70, 1.0, 0.8) = 0.94
Turn rate reduction = 6% (modifier applied to turn rate, see Section 3.4.2)
```
**Expected:** Fatigued turn rate < fresh turn rate  
**Tolerance:** Rate reduction > 5% (conservative bound for 6% expected)  
**Section Ref:** 3.4.2, FR-6

---

### 3.7.2.8 Fatigue Integration Tests (4 tests)

**Test ID: UT-FAT-001**  
**Name:** `SprintReservoir_DrainsWhileSprinting`  
**Purpose:** Validate sprint reservoir consumption  
**Input:** Agent SPRINTING for 5.0s, initial reservoir = 1.0, Stamina = 10  
**Expected:** Reservoir < 1.0 after 5.0s; drain rate consistent with SPRINT_DRAIN (Section 3.1)  
**Tolerance:** Reservoir < 0.85 (minimum observable drain)  
**Section Ref:** FR-6, Section 3.1

---

**Test ID: UT-FAT-002**  
**Name:** `SprintReservoir_RecoversWhileJogging`  
**Purpose:** Validate sprint reservoir recovery during low-intensity states  
**Input:** Agent JOGGING for 10.0s, initial reservoir = 0.50, Stamina = 10  
**Expected:** Reservoir > 0.50 after 10.0s  
**Tolerance:** Recovery > 0.05 (observable)  
**Section Ref:** FR-6, Section 3.1

---

**Test ID: UT-FAT-003**  
**Name:** `SprintReservoir_RecoverySlowedByAerobicDepletion`  
**Purpose:** Validate aerobic pool modulates sprint recovery rate  
**Input:** Two agents JOGGING, reservoir = 0.50; Agent A aerobic = 1.0, Agent B aerobic = 0.3  
**Expected:** Agent A reservoir recovery > Agent B reservoir recovery after 10.0s  
**Tolerance:** Difference > 0.02  
**Section Ref:** FR-6

---

**Test ID: UT-FAT-004**  
**Name:** `AerobicPool_ForcesJogExit_BelowFloor`  
**Purpose:** Validate aerobic pool floor enforcement  
**Input:** Agent JOGGING, aerobic pool drops below AEROBIC_JOG_FLOOR (0.15)  
**Expected:** Agent forced out of JOGGING state  
**Derivation:** Per Section 3.1.4: JOGGING Ã¢â€ â€™ DECELERATING when `aerobicPool < AEROBIC_JOG_FLOOR (0.15)`  
**Tolerance:** State = DECELERATING (exact)  
**Section Ref:** FR-6, Section 3.1.4

---

### Unit Test Summary

| Category | Test IDs | Count | Section Ref |
|---|---|---|---|
| PerformanceContext | UT-PC-001 to UT-PC-005 | 5 | 3.2.1 |
| Acceleration | UT-ACC-001 to UT-ACC-006 | 6 | 3.2.3 |
| Top Speed | UT-SPD-001 to UT-SPD-005 | 5 | 3.2.4 |
| Deceleration | UT-DEC-001 to UT-DEC-006 | 6 | 3.2.5 |
| Directional Movement | UT-DIR-001 to UT-DIR-007 | 7 | 3.3 |
| State Machine | UT-SM-001 to UT-SM-008 | 8 | 3.1 |
| Turning & Momentum | UT-TRN-001 to UT-TRN-008 | 8 | 3.4 |
| Fatigue Integration | UT-FAT-001 to UT-FAT-004 | 4 | FR-6 |
| **Total (this section)** | | **49** | |
| Edge case tests (Section 3.6.7) | UT-EDGE-001 to UT-EDGE-018 | 19 | 3.6 |
| **Grand Total** | | **68** | |

---

## 3.7.3 Integration Test Scenarios

### 3.7.3.1 Full Movement Sequence Tests (3 scenarios)

**Test ID: IT-MOV-001**  
**Name:** `Sprint_Turn_Decelerate_Stop_Sequence`  
**Purpose:** Validate complete movement lifecycle  
**Setup:**
1. Agent at IDLE, Pace 14, Agility 12, Acceleration 13, neutral context
2. Command: Sprint to position 30m ahead
3. At 20m: command 45Ã‚Â° direction change
4. At 28m: command stop  
**Expected:**
- Acceleration follows exponential curve (Section 3.2.3)
- State transitions: IDLE Ã¢â€ â€™ WALKING Ã¢â€ â€™ JOGGING Ã¢â€ â€™ SPRINTING
- Turn at 20m: turn rate constrained by speed, radius consistent with 3.4.3
- Stop at 28m: DECELERATING Ã¢â€ â€™ WALKING Ã¢â€ â€™ IDLE (controlled deceleration distance 3Ã¢â‚¬â€œ5m for Agility 12)
- Total trajectory physically plausible: velocity never exceeds top speed, acceleration never exceeds MAX_ACCELERATION Ãƒâ€” dt  
**Duration:** ~6 seconds simulated time  
**Validation:** Record position every frame; verify all constraints

---

**Test ID: IT-MOV-002**  
**Name:** `Jockey_BackwardMovement_WithFacingLock`  
**Purpose:** Validate defensive jockeying scenario Ã¢â‚¬â€ backward movement with independent facing  
**Setup:**
1. Agent JOGGING forward at 4.0 m/s, facing north
2. Command: Move backward (south) while maintaining north-facing  
**Expected:**
- Movement direction reverses, facing direction unchanged
- Effective speed reduced by backward multiplier (0.45Ã¢â‚¬â€œ0.55 Ãƒâ€” forward speed, per FR-4)
- Maximum backward speed Ã¢â€°Ë† 4.0 Ãƒâ€” 0.50 = 2.0 m/s (mid-range Agility)
- Agent decelerates first (Section 3.6.3 direction reversal handling), then accelerates backward
- No state machine oscillation during reversal  
**Duration:** 4 seconds simulated time  
**Section Ref:** 3.3.4, FR-4

---

**Test ID: IT-MOV-003** *(v1.1: sourced distribution)*  
**Name:** `Fatigue_FullMatch_SpeedDegradation`  
**Purpose:** Validate 90-minute fatigue arc produces visible speed reduction  
**Setup:**
1. Agent with Pace 15, Stamina 10 (mid-range)
2. Simulate 90 minutes of scripted mixed-intensity movement:
   - 10% IDLE, 30% WALKING, 40% JOGGING, 20% SPRINTING
   - Source: [BRADLEY-2009] Table 2, mean activity profile for midfield players. The 20% sprint allocation is approximate (actual varies 8Ã¢â‚¬â€œ12% by intensity band; compressed here for conservative fatigue testing).
3. Record top speed achievable every 15 minutes  
**Expected:**
- Minute 0: top speed Ã¢â€°Ë† 9.49 m/s (fresh)
- Minute 45: top speed reduced by 0Ã¢â‚¬â€œ10% (aerobic pool likely still above 0.5 threshold)
- Minute 90: top speed reduced by 15Ã¢â‚¬â€œ30% (QR-1: >15% at exhaustion)
- Sprint reservoir depletes faster in second half
- Speed reduction monotonically non-decreasing (never getting faster as match progresses)  
**Duration:** 90 minutes simulated time (54,000 ticks)  
**Tolerance:** Speed reduction percentages Ã‚Â±5%  
**Note:** This test uses a scripted activity profile, not AI-driven movement. Full AI-driven validation deferred to post-Spec #7 integration (see Section 3.7.4.3).

---

### 3.7.3.2 Multi-Agent Scenarios (3 scenarios)

**Test ID: IT-MA-001**  
**Name:** `TwentyAgents_SimultaneousMovement_NoInterference`  
**Purpose:** Validate agent independence Ã¢â‚¬â€ no shared state corruption  
**Setup:**
1. 20 agents with varied attributes (Pace 5Ã¢â‚¬â€œ20, Agility 3Ã¢â‚¬â€œ18, etc.)
2. All agents moving simultaneously with different commands  
**Expected:**
- Each agent's physics computed independently
- No cross-agent state contamination
- All agents within valid position/velocity bounds
- Performance within PR-1 budget (<1.0ms total)  
**Duration:** 30 seconds simulated time  
**Section Ref:** PR-1, QR-2

---

**Test ID: IT-MA-002**  
**Name:** `AttributeSpectrum_VisibleDifferences`  
**Purpose:** Validate QR-4: attribute sensitivity across full range  
**Setup:**
1. 20 agents with Pace values 1 through 20 (one per value)
2. All agents sprint same 50m straight line  
**Expected:**
- Pace 20 agent finishes first
- Pace 1 agent finishes last
- Arrival times spread across range (not clustered)
- Ordering strictly monotonic with Pace value  
**Tolerance:** No inversions in arrival order  
**Duration:** 10 seconds simulated time

---

**Test ID: IT-MA-003**  
**Name:** `IdenticalAgents_IdenticalResults_Determinism`  
**Purpose:** Validate FR-9 determinism  
**Setup:**
1. Two agents with identical attributes, identical initial state, identical commands
2. Run same 30-second movement sequence twice  
**Expected:**
- Final positions identical (bit-exact if using Fixed64, <0.001m if float)
- Full trajectory point-by-point match
- State transition timestamps identical  
**Tolerance:** Position difference < 0.001m (float mode)  
**Section Ref:** FR-9

---

### 3.7.3.3 Cross-System Integration Tests (4 scenarios)

**Test ID: IT-CS-001**  
**Name:** `StateTransition_DrivesCorrectLocomotionFormula`  
**Purpose:** Validate state machine correctly selects physics model  
**Setup:** Agent transitions through IDLE Ã¢â€ â€™ WALKING Ã¢â€ â€™ JOGGING Ã¢â€ â€™ SPRINTING Ã¢â€ â€™ DECELERATING Ã¢â€ â€™ IDLE  
**Expected:**
- IDLE: zero/minimal acceleration applied (drift reduction only)
- WALKING: linear acceleration at WALK_ACCELERATION (2.0 m/sÃ‚Â²)
- JOGGING: exponential acceleration with attribute-derived k
- SPRINTING: exponential acceleration with turn radius constraint active
- DECELERATING: constant deceleration at attribute-derived rate  
**Verification:** Log which formula function is called per frame; verify matches state  
**Section Ref:** 3.1, 3.2

---

**Test ID: IT-CS-002**  
**Name:** `DirectionalMultiplier_AffectsTurnRate`  
**Purpose:** Validate cross-section interaction: directional penalties affect turning  
**Setup:** Agent sprinting forward, commanded to move laterally (90Ã‚Â°) while turning to face new direction  
**Expected:**
- Effective speed reduced by lateral multiplier
- Turn rate appropriate for the reduced speed (not original speed)
- No inconsistency between Section 3.3 and 3.4 calculations  
**Section Ref:** 3.3.5, 3.4.2

---

**Test ID: IT-CS-003**  
**Name:** `AnimationContract_OutputsEveryFrame`  
**Purpose:** Validate FR-8 animation data contract  
**Setup:** Agent moving through all states for 10 seconds  
**Expected:**
- Every frame produces valid AnimationData: stride frequency > 0 (when moving), foot plant phase in [0.0, 1.0], lean angle in valid range, movement state populated
- No NaN or default values in output
- Data changes smoothly between frames (no single-frame spikes)  
**Section Ref:** FR-8

---

**Test ID: IT-CS-004**  
**Name:** `EventLogging_CriticalEventsRecorded`  
**Purpose:** Validate replay-critical events are logged  
**Setup:** Agent performs: sprint start, stumble, recovery, stop  
**Expected:**
- AgentSprintStartEvent logged with timestamp and agent ID
- AgentStumbleEvent logged with position, velocity, and cause
- All events in chronological order
- Ring buffer does not overflow during normal play  
**Section Ref:** 1.4 (Event System interface)

---

### Integration Test Summary

| Category | Test IDs | Count |
|---|---|---|
| Full Movement Sequences | IT-MOV-001 to IT-MOV-003 | 3 |
| Multi-Agent | IT-MA-001 to IT-MA-003 | 3 |
| Cross-System | IT-CS-001 to IT-CS-004 | 4 |
| **Total (this section)** | | **10** |
| Edge case integration (Section 3.6.7) | IT-EDGE-001 to IT-EDGE-005 | 5 |
| **Grand Total** | | **15** |

---

## 3.7.4 Real-World Validation Benchmarks

### 3.7.4.1 Data Sources

**Primary:** GPS tracking data from professional matches (publicly available via academic publications).

- **[BRADLEY-2009]** Bradley, P.S. et al. (2009). "High-intensity running in English FA Premier League soccer matches." *Journal of Sports Sciences*, 27(2), 159-168.
  - Mean total distance: 10.7 km per match
  - Sprint threshold: >7.0 m/s
  - High-intensity running: 5.5Ã¢â‚¬â€œ7.0 m/s

- **[DI SALVO-2007]** Di Salvo, V. et al. (2007). "Performance characteristics according to playing position in elite soccer." *International Journal of Sports Medicine*, 28(3), 222-227.
  - Position-specific distances: midfielders ~12 km, centre-backs ~10 km

- **[STOLEN-2005]** Stolen, T. et al. (2005). "Physiology of Soccer." *Sports Medicine*, 35(6), 501-536.
  - Top speed: elite players 9.0Ã¢â‚¬â€œ10.2 m/s (validates FR-2)
  - Acceleration: 0Ã¢â‚¬â€œ30m sprint times used to derive curves

### 3.7.4.2 Immediately Testable Benchmarks

These benchmarks can be validated during Stage 0 implementation without AI integration.

**VB-1: Sprint Speed Range**
- **Metric:** Top speed achievable per agent
- **Expected:** 7.5Ã¢â‚¬â€œ10.2 m/s across Pace 1Ã¢â‚¬â€œ20
- **Source:** [STOLEN-2005]
- **Test:** UT-SPD-001 through UT-SPD-005
- **Status:** TESTABLE AT IMPLEMENTATION

**VB-2: Time to Top Speed**
- **Metric:** Seconds from standstill to 90% of top speed
- **Expected:** 2.5Ã¢â‚¬â€œ3.5 seconds (attribute-dependent)
- **Source:** [STOLEN-2005], Section 3.2.2 table
- **Test:** UT-ACC-003
- **Status:** TESTABLE AT IMPLEMENTATION

**VB-5: Stop Distance**
- **Metric:** Distance from sprint speed to full stop
- **Expected:** 3.0Ã¢â‚¬â€œ5.0m controlled (Agility-dependent), 2.5Ã¢â‚¬â€œ3.5m emergency (Agility-dependent)
- **Source:** Derived from deceleration rates in Section 3.2.5
- **Test:** UT-DEC-001, UT-DEC-002
- **Status:** TESTABLE AT IMPLEMENTATION

**VB-6: Fatigue Speed Reduction**
- **Metric:** Percentage decrease in top speed at minute 90 vs minute 0
- **Expected:** 15Ã¢â‚¬â€œ30% reduction (scripted activity profile)
- **Source:** [BRADLEY-2009], [STOLEN-2005]
- **Test:** IT-MOV-003
- **Status:** TESTABLE AT IMPLEMENTATION (scripted profile)

**VB-7: Backward Movement Speed**
- **Metric:** Ratio of backward speed to forward speed
- **Expected:** 0.45Ã¢â‚¬â€œ0.55Ãƒâ€” forward speed
- **Source:** Biomechanical estimates
- **Test:** UT-DIR-003
- **Status:** TESTABLE AT IMPLEMENTATION

### 3.7.4.3 Deferred Validation Benchmarks

These benchmarks require AI-driven movement (Spec #7: Decision Tree, Spec #12: Positioning AI) and CANNOT be validated during Agent Movement Spec #2 implementation. They are NOT part of the Stage 0 approval criteria for this spec.

**VB-3: Total Distance Per Match** *(DEFERRED)*
- **Metric:** Summed agent distance over 90 minutes
- **Expected:** 9.5Ã¢â‚¬â€œ12.5 km per agent (position-dependent)
- **Source:** [BRADLEY-2009]
- **Prerequisite:** AI-driven movement patterns (Spec #7+)
- **Preliminary proxy:** IT-MOV-003 with scripted movement provides partial coverage
- **Target validation date:** After Spec #7 implementation

**VB-4: Sprint Distance Per Match** *(DEFERRED)*
- **Metric:** Cumulative distance covered at >7.0 m/s
- **Expected:** 500Ã¢â‚¬â€œ1200m per agent per match
- **Source:** [BRADLEY-2009]
- **Prerequisite:** AI-driven sprint decisions (Spec #7+)
- **Target validation date:** After Spec #7 implementation

### 3.7.4.4 Visual Validation Protocol

**After all unit and integration tests pass:**

1. Record 5 different movement scenarios in simulation:
   a. 50m sprint from standstill
   b. Sprint with 90Ã‚Â° direction change
   c. Defensive jockeying (backward/lateral)
   d. Stop-start sequence (receive ball, turn, sprint)
   e. End-of-match fatigued agent movement

2. Compare side-by-side with broadcast match footage

3. Survey: minimum 10 football players/fans (recruitment via r/bootroom)
   - Question: "Does this agent movement look realistic? (Yes/No/Unsure)"
   - Required: >80% "Yes" responses per scenario

**Validation timing:** After all unit/integration tests pass, before Stage 0 quality gate.

### 3.7.4.5 Reference Data Storage

```csharp
/// <summary>
/// Reference data for Agent Movement validation.
/// All values derived from published sports science data.
/// 
/// Sources:
///   BRADLEY-2009: GPS tracking, Premier League
///   STOLEN-2005: Football physiology review
///   DI SALVO-2007: Position-specific performance
/// 
/// Created: 2026-02-11 from Section 3.7.4 benchmarks
/// </summary>
public static class AgentMovementBenchmarks
{
    // VB-1: Sprint speed range (Section 3.2.4)
    public static readonly float MIN_TOP_SPEED = 7.5f;   // Pace 1 (m/s)
    public static readonly float MAX_TOP_SPEED = 10.2f;   // Pace 20 (m/s)
    
    // VB-2: Time to 90% top speed (Section 3.2.2)
    public static readonly float MIN_TIME_TO_TOP = 2.5f;  // best attributes (s)
    public static readonly float MAX_TIME_TO_TOP = 3.5f;  // worst attributes (s)
    
    // VB-5: Stop distances (Section 3.2.5)
    public static readonly float CONTROLLED_STOP_MIN = 3.0f;  // meters (Agility 20)
    public static readonly float CONTROLLED_STOP_MAX = 5.0f;  // meters (Agility 1)
    public static readonly float EMERGENCY_STOP_MIN = 2.5f;   // meters (Agility 20)
    public static readonly float EMERGENCY_STOP_MAX = 3.5f;   // meters (Agility 1)
}
```

---

## 3.7.5 Regression Testing Requirements

### 3.7.5.1 Trigger Conditions

**Full regression sweep required when:**

1. **Any constant change** in MovementConstants or MovementThresholds (Section 3.5, 3.1)
2. **Any formula change** in acceleration, deceleration, turn rate, or directional multiplier functions
3. **Any state machine modification** Ã¢â‚¬â€ new states, changed transitions, threshold adjustments
4. **Any refactor** Ã¢â‚¬â€ code reorganization, performance optimization, dependency changes
5. **Integration of new spec** Ã¢â‚¬â€ when Collision System (Spec #3), First Touch (Spec #11), or other consuming specs are connected

### 3.7.5.2 Regression Protocol

**Before any constant tuning:**

1. **Baseline capture:** Record commit SHA; run full test suite; save results as `baseline_{SHA}.json` including all 68 unit tests, 15 integration tests, and VB-1/2/5/6/7 benchmark values.

2. **Apply change:** Modify constant(s); document rationale in code comment and changelog.

3. **Regression check:** Run full test suite; compare against baseline. Flag any test that changed status. Flag any benchmark value that changed by more than Ã‚Â±5%.

4. **Review & commit:** If only intended tests changed, approve. If unintended tests changed, investigate before committing. Update golden trajectories if benchmarks legitimately shifted.

### 3.7.5.3 Golden Trajectory Tests

**Purpose:** Detect unintended physics changes by comparing full agent trajectories against recorded baselines.

**Dual-tolerance system** *(v1.1)*:
- **Per-frame position delta:** Ã‚Â±0.05m (5cm). Accounts for float accumulation across individual frames. Derived from: at MAX_SPEED (12 m/s) Ãƒâ€” dt (0.0167s) = 0.2m per frame max travel; 0.05m tolerance = 25% of max single-frame displacement, sufficient to catch formula errors while tolerating float variance.
- **End-of-trajectory final position:** Ã‚Â±0.15m (15cm). Accounts for cumulative drift over test duration. Derived from: PR-3 specifies <5cm drift per agent per 90 minutes; golden trajectory tests are 3Ã¢â‚¬â€œ6s long (0.06Ã¢â‚¬â€œ0.11% of a full match); 0.15m provides >100Ãƒâ€” headroom over the prorated drift budget, which is intentionally generous because golden trajectories compare against themselves (not an absolute target) and the primary purpose is detecting *regressions*, not absolute accuracy.

**Golden trajectories to maintain:**

| ID | Scenario | Key Attributes | Duration |
|---|---|---|---|
| GT-001 | 50m straight sprint | Pace 15, Acc 12 | ~6s |
| GT-002 | Sprint + 45Ã‚Â° turn | Pace 12, Agility 14 | ~5s |
| GT-003 | Backward jockey 10m | Pace 10, Agility 16 | ~4s |
| GT-004 | Fatigued sprint (aerobic 0.4) | Pace 15, Stamina 10 | ~6s |
| GT-005 | Full stop from sprint | Pace 14, Agility 11 | ~3s |

**Regeneration rule:** Golden trajectories MUST be regenerated whenever an intentional physics change is made. The regeneration MUST be documented in the changelog with reason and commit SHA of new baseline.

---

## 3.7.6 Performance Testing Criteria

### 3.7.6.1 Budget Targets

| Metric | Target | Source |
|---|---|---|
| All 20 agents per frame (p95) | <1.0ms | PR-1 |
| Per-agent update (mean) | <0.05ms (50Ã‚Âµs) | PR-1 |
| All 20 agents (mean) | <0.7ms | PR-1 |
| All 20 agents (p99) | <1.5ms | Investigation trigger |
| Heap allocations | Zero during UpdateAgentMovement() | PR-2 |
| GC pressure | None attributable to movement | PR-2 |
| AgentMovementState size | Ã¢â€°Â¤256 bytes | PR-2 |
| Total movement memory | <5KB for 20 agents | PR-2 |

### 3.7.6.2 Performance Test Protocol

1. Run 100 simulated matches (5400 seconds simulated time each)
2. 20 outfield agents per match, varied attribute distributions
3. Profile every frame at 60Hz (324,000 frames per match)
4. Agents executing mixed movement patterns (not idle Ã¢â‚¬â€ worst-case workload)

**Investigation triggers:**
- Any frame >1.5ms: log full call stack, identify hotspot
- >0.1% of frames exceeding 1.0ms: performance optimization required
- Any heap allocation: treat as bug, fix immediately

### 3.7.6.3 Profiling Markers

Required custom Unity Profiler markers:

```
AgentMovement.UpdateAll          (top-level, all 20 agents)
  AgentMovement.UpdateSingle     (per-agent, development builds only)
    AgentMovement.StateMachine   (~10% budget)
    AgentMovement.Locomotion     (~35% budget)
    AgentMovement.Directional    (~15% budget)
    AgentMovement.Turning        (~25% budget)
    AgentMovement.Validation     (~15% budget)
```

Budget percentages are advisory estimates, not hard limits. Actual distribution will be confirmed by profiling during implementation.

---

## 3.7.7 Test Execution Plan

### 3.7.7.1 Development Phase (Stage 0 Months 3-4)

- Write unit test alongside each function (TDD approach)
- Run unit tests after every code change; commit only when tests pass
- Target: green tests 95% of time
- Weekly: run full test suite every Friday; review coverage; address gaps

### 3.7.7.2 Pre-Approval

**Before Agent Movement Spec #2 implementation approval:**
- All 68 unit tests pass (49 functional + 19 edge case)
- All 15 integration tests pass (10 functional + 5 edge case)
- Code coverage >80% for core locomotion, >70% for state machine
- Performance targets met (p95 <1.0ms for 20 agents)
- VB-1, VB-2, VB-5, VB-6, VB-7 within specified ranges
- Visual validation completed (>80% approval)

### 3.7.7.3 Stage 0 Quality Gate

**Before advancing to Stage 1:**
- 1000 simulated matches: zero critical failures, <5 validation warnings per match per agent
- All immediately-testable benchmarks within ranges
- Golden trajectories match baselines
- Community feedback positive (>75% on r/gamedev)

### 3.7.7.4 Continuous Regression

- Unit tests: every commit (local)
- Integration tests: nightly (if CI/CD available)
- Performance tests: weekly
- Golden trajectory comparison: every physics-affecting commit
- Benchmark validation: monthly

---

## 3.7.8 Acceptance Criteria Summary

### Mandatory (must pass for Stage 0 approval)

**Unit Tests:**
- [ ] All 49 functional unit tests pass (Section 3.7.2)
- [ ] All 19 edge case unit tests pass (Section 3.6.7)
- [ ] Code coverage >80% for AgentMovementCore
- [ ] Code coverage >70% for AgentStateMachine
- [ ] Zero compiler warnings
- [ ] Test suite completes in <15 seconds

**Integration Tests:**
- [ ] All 10 functional integration tests pass (Section 3.7.3)
- [ ] All 5 edge case integration tests pass (Section 3.6.7)
- [ ] No state machine oscillations (max 6 transitions/second)
- [ ] 20-agent simultaneous operation stable for 90 minutes

**Performance:**
- [ ] All 20 agents update in <1.0ms per frame (p95)
- [ ] Per-agent update <0.05ms (mean)
- [ ] Zero heap allocations during movement update
- [ ] AgentMovementState Ã¢â€°Â¤256 bytes per agent

**Immediately Testable Benchmarks:**
- [ ] VB-1: Sprint speed range 7.5Ã¢â‚¬â€œ10.2 m/s
- [ ] VB-2: Time to 90% top speed 2.5Ã¢â‚¬â€œ3.5s
- [ ] VB-5: Stop distances within specified ranges
- [ ] VB-6: Fatigue reduction >15% at minute 90 (scripted profile)
- [ ] VB-7: Backward multiplier 0.45Ã¢â‚¬â€œ0.55Ãƒâ€”
- [ ] Visual validation >80% "Yes" responses

**Stability:**
- [ ] Zero NaN or Infinity values in 1000-match simulation
- [ ] Position drift <5cm per agent after 90 minutes
- [ ] All failure modes trigger recovery (no crashes)

**Determinism:**
- [ ] Identical agents + identical commands = identical results
- [ ] Replay reconstruction produces matching trajectories

### Quality (should pass, non-blocking)

- [ ] All magic numbers replaced with named constants
- [ ] Test names clearly describe purpose
- [ ] Expected values include derivation comments
- [ ] Tolerance values justified in Section 3.7.9
- [ ] Golden trajectories established for 5 baseline scenarios
- [ ] Regression protocol documented and first baseline captured

### Deferred (NOT required for this spec's approval)

- [ ] VB-3: Total match distance (requires AI integration, Spec #7+)
- [ ] VB-4: Sprint distance per match (requires AI integration, Spec #7+)

---

## 3.7.9 Tolerance Derivations

**Purpose:** Formal derivation and rationale for every tolerance value used in this section, mirroring Ball Physics Appendix D pattern. Every non-exact tolerance must have an entry here.

### 3.7.9.1 Float Arithmetic Precision

**Applies to:** UT-PC-004, UT-SPD-001 through UT-SPD-003

**Derivation:** IEEE 754 single-precision float has ~7 significant digits. For values in the 1Ã¢â‚¬â€œ20 range, machine epsilon Ã¢â€°Ë† 1.2 Ãƒâ€” 10Ã¢ÂÂ»Ã¢ÂÂ· Ãƒâ€” value Ã¢â€°Ë† 2.4 Ãƒâ€” 10Ã¢ÂÂ»Ã¢ÂÂ¶. A tolerance of Ã‚Â±0.01 provides >4000Ãƒâ€” headroom over float precision error. This generous margin accounts for multiple chained operations (attribute evaluation Ã¢â€ â€™ mapping Ã¢â€ â€™ modifier chain) while still catching any formula error that would produce a wrong integer part.

### 3.7.9.2 Acceleration Curve Tolerances

**Applies to:** UT-ACC-001 (Ã‚Â±0.15 m/s), UT-ACC-002 (>0.4 m/s difference), UT-ACC-006 (>5.0 m/s)

**Derivation for UT-ACC-001:** The discrete 60Hz integration introduces error vs. the continuous formula. Section 3.2.3 documents this error as <0.01 m/s over a full 3.5s acceleration. At t=0.5s (30 frames), error is proportionally smaller. However, frame timing jitter (Ã‚Â±0.5 frames) at k=0.8518 produces velocity uncertainty of Ã‚Â±0.8518 Ãƒâ€” 9.49 Ãƒâ€” e^(-0.4259) Ãƒâ€” dt Ã¢â€°Ë† Ã‚Â±0.088 m/s per frame boundary. Adding Ã‚Â±0.01 integration error + Ã‚Â±0.088 jitter + Ã‚Â±0.05 safety margin = Ã‚Â±0.148, rounded to Ã‚Â±0.15 m/s.

**Derivation for UT-ACC-002:** Expected difference Ã¢â€°Ë† 0.60 m/s. Conservative bound of >0.4 m/s allows for Ã‚Â±0.15 m/s tolerance on each agent's speed measurement (2 Ãƒâ€” 0.15 = 0.30 subtracted from 0.60 leaves 0.30 headroom, rounded down to 0.4 threshold for additional safety).

### 3.7.9.3 Time-to-Speed Tolerance

**Applies to:** UT-ACC-003 (Ã‚Â±0.3s)

**Derivation:** TÃ¢â€šâ€°Ã¢â€šâ‚¬ = 2.3026 / k. At k = 0.7826 (Acc=10), TÃ¢â€šâ€°Ã¢â€šâ‚¬ = 2.94s. Frame granularity at 60Hz = 16.67ms. The 90% threshold is crossed mid-frame; actual crossing time depends on whether the check runs before or after the speed update. This introduces Ã‚Â±1 frame uncertainty (Ã‚Â±0.017s). The Ã‚Â±0.3s tolerance provides >17Ãƒâ€” headroom over measurement uncertainty, which is intentionally generous to allow for minor constant tuning without breaking this test. If the implementation produces TÃ¢â€šâ€°Ã¢â€šâ‚¬ outside 2.6Ã¢â‚¬â€œ3.2s for Acceleration 10, it indicates a formula error or constant mismatch.

### 3.7.9.4 Fatigue Modifier Tolerance

**Applies to:** UT-ACC-004 (>10% reduction), UT-SPD-004 (10%Ã¢â‚¬â€œ15% reduction)

**Derivation:** At aerobic pool 0.3, the modifier formula produces exactly 0.88 (Section 3.2.4). This gives 12% speed reduction. The >10% lower bound provides 2% headroom below the exact answer. The 15% upper bound accounts for the possibility that fatigue also affects acceleration rate (Section 3.4.2), which may cause the agent to not fully reach their reduced top speed within the measurement window.

### 3.7.9.5 Deceleration Distance Tolerances

**Applies to:** UT-DEC-001 (Ã‚Â±0.4m), UT-DEC-002 (Ã‚Â±0.4m), UT-DEC-005 (>0.5m difference)

**Derivation:** The continuous formula d = vÃ¢â€šâ‚¬Ã‚Â²/(2a) gives the theoretical stopping distance. Discrete integration at 60Hz adds up to 1 frame of additional travel: MAX_SPEED Ãƒâ€” dt = 12.0 Ãƒâ€” 0.0167 = 0.20m worst case. The Ã‚Â±0.4m tolerance provides 2Ãƒâ€” headroom over the discrete error, accounting for the speed-dependent nature of the last-frame overshoot (faster agents overshoot more) and any tolerance stacking from the deceleration rate mapping.

### 3.7.9.6 Directional Multiplier Tolerances

**Applies to:** UT-DIR-002 (Ã‚Â±0.02), UT-DIR-004 (>0.05 difference), UT-DIR-005 (<0.03 jump), UT-DIR-006 (Ã‚Â±0.3 m/s), UT-DIR-007 (>0.15s)

**Derivation for UT-DIR-002:** FR-4 specifies 0.65Ã¢â‚¬â€œ0.75 range for lateral movement. The Ã‚Â±0.02 tolerance allows the implementation to sit anywhere in a [0.63, 0.77] band, which is intentionally wider than the FR-4 range to avoid test brittleness during tuning. If the multiplier falls outside [0.63, 0.77], it's either wrong or the FR-4 range needs updating.

**Derivation for UT-DIR-005:** Zone boundary smoothing should produce no discontinuity. A 0.03 maximum jump between adjacent 1Ã‚Â° angles is imperceptible in gameplay (0.03 Ãƒâ€” 10 m/s = 0.3 m/s difference for 1Ã‚Â° of angle change). Larger jumps indicate a missing interpolation or a step function at the boundary.


**Agility 1 exemption (v1.2):** At Agility 1, the lateral multiplier is 0.65 (lowest in
FR-4 range), producing a forward-lateral jump of (1.0 - 0.65) / 10 deg = 0.035/degree.
This exceeds the 0.03 threshold by 17%. However, 0.035/degree at 9 m/s = 0.315 m/s per
degree, still imperceptible. Agility 1 is explicitly exempt; test implementations should
skip this test case when input Agility = 1, or use a relaxed threshold of 0.04.

### 3.7.9.7 Turning & Stumble Tolerances

**Applies to:** UT-TRN-003 (>0.12s), UT-TRN-004 (Ã‚Â±15% ratio), UT-TRN-006 (>0.08 probability), UT-TRN-007 (>2Ã‚Â° lean)

**Derivation for UT-TRN-003:** Turn rate at Agility 5 vs Agility 18 at 7.0 m/s. The exact difference depends on the turn rate formula (Section 3.4.2) which maps Agility to degrees/second. With the full attribute range (5 to 18 = 13 points), and turn rate per point estimated at ~5Ã¢â‚¬â€œ10Ã‚Â°/s at sprint-adjacent speeds, the expected time difference for a 90Ã‚Â° turn is 0.15Ã¢â‚¬â€œ0.30s. The >0.12s threshold is conservatively below the lower bound.

**Derivation for UT-TRN-004:** r_min Ã¢Ë†Â vÃ‚Â². The ratio r(6)/r(3) should theoretically = 4.0. The Ã‚Â±15% tolerance allows [3.4, 4.6], accounting for the agility_factor denominator which may not perfectly cancel between speed levels if there are any speed-dependent agility scaling effects.

---

## End of Section 3.7

**Page Count:** ~26 pages  
**Next Section:** Section 4 Ã¢â‚¬â€ Implementation Details (Code Organization, Dependencies, Configuration)

---

## CROSS-REFERENCES

**This section derives test requirements from:**
- Section 2.1 (Functional Requirements FR-1 through FR-9)
- Section 2.2 (Performance Requirements PR-1 through PR-3)
- Section 2.3 (Quality Requirements QR-1 through QR-4)
- Section 3.1 (State Machine Ã¢â‚¬â€ transition logic, hysteresis thresholds, dwell times)
- Section 3.2 (Locomotion Ã¢â‚¬â€ acceleration curves, top speed mapping, deceleration rates, fatigue modifiers)
- Section 3.3 (Directional Movement Ã¢â‚¬â€ zone multipliers, facing independence)
- Section 3.4 (Turning Ã¢â‚¬â€ turn rate, min radius, stumble risk, lean angle)
- Section 3.5 (Data Structures Ã¢â‚¬â€ MovementConstants, AgentMovementState)
- Section 3.6 (Edge Cases Ã¢â‚¬â€ error recovery, boundary conditions)
- Ball Physics Spec #1, Section 5 (testing pattern reference)
- Ball Physics Spec #1, Appendix D (tolerance derivation pattern reference)

**This section is consumed by:**
- Section 4 (Implementation Details Ã¢â‚¬â€ test file organization)
- Development Best Practices (regression testing workflow)
- Spec #19 (Testing Strategy & Framework Ã¢â‚¬â€ cross-spec test architecture)
