# Pass Mechanics Specification #5 — Section 5: Testing

**File:** `Pass_Mechanics_Spec_Section_5_v1_1.md`
**Purpose:** Defines all test requirements, acceptance criteria, and validation methodology
for the Pass Mechanics system (Spec #5). Establishes unit tests, integration tests, and
real-world validation scenarios to verify correctness of: Pass Type Classification (§3.1),
Pass Velocity Model (§3.2), Launch Angle Derivation (§3.3), Spin Vector Calculation (§3.4),
Error Model (§3.5), Target Resolution (§3.6), Weak Foot Penalty (§3.7), Execution State
Machine (§3.8), and Event Publishing (§3.9). This section is the authoritative test reference
for Stage 0 implementation sign-off.

**Created:** February 20, 2026, 11:30 PM PST
**Revised:** February 21, 2026
**Version:** 1.1
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 5 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)
**Prerequisites:** Section 1 (v1.0), Section 2 (v1.0), Section 3 (§3.1–§3.9), Section 4 (v1.0)

**Open Dependency Flags:**
- `[ERR-007-PENDING]` — `KickPower`, `WeakFootRating`, `Crossing` absent from
  `PlayerAttributes`. Tests PV-*, WF-* marked `[ERR-007-BLOCKED]` cannot be finalised
  until attribute names are confirmed. Placeholder attribute names used throughout.
- `[ERR-008-PENDING]` — `PossessingAgentId` design unresolved. IT-004 (possession
  transfer on ApplyKick) marked `[ERR-008-BLOCKED]` until resolved.

---

## Table of Contents

- [5.1 Test Strategy](#51-test-strategy)
- [5.2 Unit Tests — Pass Type Classification (PT-)](#52-unit-tests--pass-type-classification-pt-)
- [5.3 Unit Tests — Pass Velocity Model (PV-)](#53-unit-tests--pass-velocity-model-pv-)
- [5.4 Unit Tests — Launch Angle Derivation (LA-)](#54-unit-tests--launch-angle-derivation-la-)
- [5.5 Unit Tests — Spin Vector Calculation (SV-)](#55-unit-tests--spin-vector-calculation-sv-)
- [5.6 Unit Tests — Pass Error Model (PE-)](#56-unit-tests--pass-error-model-pe-)
- [5.7 Unit Tests — Target Resolution (TR-)](#57-unit-tests--target-resolution-tr-)
- [5.8 Unit Tests — Weak Foot Penalty (WF-)](#58-unit-tests--weak-foot-penalty-wf-)
- [5.9 Unit Tests — Execution State Machine (PSM-)](#59-unit-tests--execution-state-machine-psm-)
- [5.10 Unit Tests — Edge Cases and Robustness (EC-)](#510-unit-tests--edge-cases-and-robustness-ec-)
- [5.11 Integration Tests (IT-)](#511-integration-tests-it-)
- [5.12 Real-World Validation Scenarios (VS-)](#512-real-world-validation-scenarios-vs-)
- [5.13 Acceptance Criteria Summary](#513-acceptance-criteria-summary)
- [5.14 Test Execution Plan](#514-test-execution-plan)
- [5.15 Section Summary](#515-section-summary)

---

## 5.1 Test Strategy

### 5.1.1 Test Pyramid

```
        /\
       /  \     E2E Tests (5%)
      /____\    - Full 90-minute match simulation
     /      \   - Replay determinism verification
    /        \
   / Integr-  \ Integration Tests (20%)
  /  ation     \ - Cross-system boundary scenarios
 /______________\ - Pass Mechanics ↔ Ball Physics, Agent Movement, Collision, Event System
/                \
/   Unit Tests    \ Unit Tests (75%)
/__(82+ tests)____\ - Formula validation, edge cases, boundary conditions
```

**Distribution rationale:**

Unit tests are fast (< 15s total) and enable rapid iteration during TDD. Every formula
term, every constant, and every branch condition in §3.1–§3.9 has a corresponding unit
test. Integration tests verify that correct outputs flow between systems as defined in
the Section 4 interface contracts. E2E tests run pre-milestone only; they confirm no NaN
propagation, stable memory, and deterministic replay across a full simulated match.

### 5.1.2 Determinism Requirement

**All tests produce identical results across runs without exception.**

No test uses `System.Random`, `UnityEngine.Random`, or any other non-deterministic source.
Tests use fixed attribute values, fixed positions, fixed facing vectors, and fixed frame
numbers. Any test that cannot be made deterministic with fixed inputs represents a design
flaw in the system under test — escalate as a specification bug before proceeding.

Determinism is the single most important correctness property for Pass Mechanics. A
non-deterministic pass system makes replay verification impossible and is therefore a
critical project failure, not a minor defect.

### 5.1.3 Test Categories

Tests are categorised by prefix:

| Prefix | Category | §3 Owner | Count |
|--------|----------|----------|-------|
| `PT-`  | Pass Type Classification | §3.1 | 8 |
| `PV-`  | Pass Velocity Model | §3.2 | 12 |
| `LA-`  | Launch Angle Derivation | §3.3 | 8 |
| `SV-`  | Spin Vector Calculation | §3.4 | 8 |
| `PE-`  | Pass Error Model | §3.5 | 10 |
| `TR-`  | Target Resolution | §3.6 | 16 |
| `WF-`  | Weak Foot Penalty | §3.7 | 6 |
| `PSM-` | Execution State Machine | §3.8 | 6 |
| `EC-`  | Edge Cases / Robustness | §3.1–§3.9 | 8 |
| `IT-`  | Integration Tests | All | 12 |
| `VS-`  | Validation Scenarios | All | 6 |
| **Total** | | | **100** |

### 5.1.4 Test Tooling

| Tool | Purpose |
|------|---------|
| Unity Test Framework (NUnit) | Test runner — Edit Mode for pure logic, Play Mode for integration |
| Unity Profiler | Performance validation against §5.1.5 budgets |
| Unity Code Coverage | Line coverage tracking — target > 85% for §3.x code |
| Custom `DeterministicTestHarness` | Fixed-seed frame counter for deterministic evaluation |
| `PassTestFixtures.cs` | Shared test data — canonical agent profiles and expected values |

### 5.1.5 Performance Budget (Reference)

Tests that include timing assertions use the following budgets derived from Section 6:

| Metric | Target |
|--------|--------|
| `EvaluatePass()` p95 | < 0.05ms per call |
| `EvaluatePass()` p99 | < 0.10ms per call |
| Peak-frame (3 simultaneous) | < 0.25ms combined |
| Full unit test suite | < 15 seconds total |
| Full integration suite | < 60 seconds total |
| Full suite including VS | < 5 minutes total |

---

## 5.2 Unit Tests — Pass Type Classification (PT-)

**Owner:** §3.1 — Pass Type Taxonomy and Physical Profiles
**Target count:** 8 tests
**Purpose:** Verify that each `PassType` enum value maps to a valid, correctly-populated
`PhysicalProfile`, that profile lookup is deterministic, that invalid inputs are rejected,
and that cross sub-typing produces distinct profiles.

| ID | Test Name | Inputs | Expected Result | Tolerance |
|----|-----------|--------|-----------------|-----------|
| PT-001 | Ground pass profile not null | `PassType.Ground` | `PhysicalProfile != null` | Exact |
| PT-002 | Driven pass profile not null | `PassType.Driven` | `PhysicalProfile != null` | Exact |
| PT-003 | All 7 pass types yield non-null profile | All 7 `PassType` values | All non-null | Exact |
| PT-004 | All 7 pass types yield non-zero V_MAX | All 7 `PassType` values | `V_MAX > 0` for all | Exact |
| PT-005 | Profile lookup is deterministic | `PassType.Lofted` × 3 calls | All 3 results identical | Exact |
| PT-006 | Cross Flat vs Whipped produce different profiles | `CrossSubType.Flat`, `CrossSubType.Whipped` | `V_MAX_Flat ≠ V_MAX_Whipped` OR `LaunchAngle_Flat ≠ LaunchAngle_Whipped` | Exact |
| PT-007 | Cross Flat default when CrossSubType absent | `PassType.Cross`, no `CrossSubType` | Profile equals `CrossSubType.Flat` profile | Exact |
| PT-008 | Invalid PassType enum value produces FM-01 | `(PassType)99` | `PassResult.Outcome == FM-01` | Exact |

**Notes:**

PT-006 is the critical cross sub-typing regression test. If a refactor removes cross
sub-type differentiation, this test catches it immediately. It tests either velocity or
angle being different — not both — to avoid over-specifying the profiles before §3.1
constants are tuning-finalised.

PT-008 verifies defensive coding. Unity's `enum` provides no bounds enforcement at
runtime; an unchecked cast from an integer is possible in serialised data.

---

## 5.3 Unit Tests — Pass Velocity Model (PV-)

**Owner:** §3.2 — Pass Velocity Model
**Target count:** 12 tests
**Dependency:** `[ERR-007-PENDING]` — tests using `KickPower` attribute use placeholder
name `PlayerAttributes.KickPower`. Update to confirmed attribute name before finalisation.

All expected velocity values are calculated from §3.2 formulas and documented in
`PassTestFixtures.cs` with inline derivation comments. Magic numbers in test assertions
are forbidden.

| ID | Test Name | Inputs | Expected Result | Tolerance |
|----|-----------|--------|-----------------|-----------|
| PV-001 | Ground pass, mid-range attributes, mid-range distance | `PassType.Ground`, `KickPower=10`, `Distance=20m`, `Fatigue=0` | `V ∈ [10, 16] m/s` | ± 0.01 m/s |
| PV-002 | Driven pass, high attributes, long distance | `PassType.Driven`, `KickPower=18`, `Distance=35m`, `Fatigue=0` | `V ∈ [20, 26] m/s` | ± 0.01 m/s |
| PV-003 | Lofted pass, mid-range attributes, long distance | `PassType.Lofted`, `KickPower=10`, `Distance=40m`, `Fatigue=0` | `V ∈ [14, 20] m/s` | ± 0.01 m/s |
| PV-004 | Ground pass, minimum power, long distance | `PassType.Ground`, `KickPower=1`, `Distance=30m`, `Fatigue=0` | `V ∈ [V_OFFSET_Ground, V_OFFSET_Ground + 1.5]` (low but above floor) | ± 0.05 m/s |
| PV-005 | Chip pass, maximum power, maximum distance | `PassType.Chip`, `KickPower=20`, `Distance=D_MAX_Chip`, `Fatigue=0` | `V == V_MAX_Chip` (clamped) | ± 0.001 m/s |
| PV-006 `[ERR-007-BLOCKED]` | Fatigue=1.0 reduces velocity | `PassType.Driven`, `KickPower=15`, `Fatigue=1.0` | `V < V_at_Fatigue_0` | Strict inequality |
| PV-007 | Fatigue=0.0 applies no reduction | `PassType.Driven`, `KickPower=15`, `Fatigue=0.0` | `V == V_no_fatigue` | ± 0.001 m/s |
| PV-008 | Fatigue modifier is monotonically decreasing | `Fatigue ∈ {0.0, 0.25, 0.5, 0.75, 1.0}`, fixed others | `V` decreases at each step | Strict monotone |
| PV-009 | All pass types produce V within their profile bounds | All 7 types, mid-range inputs | `V_MIN ≤ V ≤ V_MAX` for each | Exact (no tolerance) |
| PV-010 | Ground pass, high power, maximum distance | `PassType.Ground`, `KickPower=20`, `Distance=D_MAX_Ground`, `Fatigue=0` | `V == V_MAX_Ground` (clamped) | ± 0.001 m/s |
| PV-011 | Velocity is deterministic | Same inputs × 3 calls | All 3 results identical | Exact |
| PV-012 | Zero distance input produces FM-07 | `Distance=0m` | `PassResult.Outcome == FM-07` | Exact |

**Notes:**

PV-004 updated in v1.1: the corrected §3.2 formula (AM-003-001, V_OFFSET addition) means
KickPower=1 at Distance=30m produces V ≈ V_OFFSET_Ground + small increment, not V_MIN.
The test now verifies the practical minimum rather than the absolute clamping floor.

PV-005 and PV-010 updated in v1.1: V_MAX clamp is only reached when KickPower=20 AND
Distance=D_MAX. At short distance with max KickPower, V_base does not reach V_MAX.
Both tests corrected to use D_MAX as the distance input.

PV-008 (monotone fatigue) is a regression guard for formula refactors. If the fatigue
modifier is accidentally inverted, this test catches it before any integration testing.

PV-012 tests the degenerate zero-distance case. While Decision Tree should not produce
this, Pass Mechanics must reject it gracefully rather than producing NaN velocity.

---

## 5.4 Unit Tests — Launch Angle Derivation (LA-)

**Owner:** §3.3 — Launch Angle Derivation
**Target count:** 8 tests
**Notes:** Expected values calculated from §3.3 projectile equations. All angles in
degrees; conversion to radians happens internally before `ApplyKick()`.

| ID | Test Name | Inputs | Expected Result | Tolerance |
|----|-----------|--------|-----------------|-----------|
| LA-001 | Ground pass angle within profile bounds | `PassType.Ground`, `Distance=20m` | `Angle ∈ [2°, 5°]` | ± 0.1° |
| LA-002 | Lofted pass angle within profile bounds | `PassType.Lofted`, `Distance=30m` | `Angle ∈ [25°, 55°]` | ± 0.5° |
| LA-003 | Chip pass angle within profile bounds | `PassType.Chip`, `Distance=12m` | `Angle ∈ [40°, 70°]` | ± 0.5° |
| LA-004 | Cross Flat angle within profile bounds | `PassType.Cross`, `CrossSubType.Flat` | `Angle ∈ [8°, 15°]` | ± 0.5° |
| LA-005 | Cross High angle within profile bounds | `PassType.Cross`, `CrossSubType.High` | `Angle ∈ [25°, 40°]` | ± 0.5° |
| LA-006 | Lofted angle decreases with distance (short vs long) | `PassType.Lofted`, `Distance=15m` vs `Distance=45m` | Angle at 15m > Angle at 45m | Strict inequality |
| LA-007 | Angle derivation is deterministic | Same inputs × 3 calls | All 3 results identical | Exact |
| LA-008 | Computed angle passed correctly to ApplyKick spin | `PassType.Lofted`, verify velocity Y component | `V_y = V × sin(Angle)` within tolerance | ± 0.01 m/s |

**Notes:**

LA-006 corrected in v1.1: the projectile mechanics formula (θ = atan(4H/D) with fixed apex
height H) produces HIGHER angles at SHORTER distances — to reach the same apex over a
shorter horizontal distance requires a steeper launch. The original v1.0 expectation was
physically inverted. Appendix B.2.4 (OI-App-B-04) identified and resolved this.

At D=15m: θ = atan(4×6.0/15) = atan(1.6) ≈ 58.0°
At D=45m: θ = atan(4×6.0/45) = atan(0.533) ≈ 28.1°
58.0° > 28.1° ✓ — Angle at 15m is greater than at 45m, confirming the corrected test.

LA-008 is the bridge test between angle derivation and the `ApplyKick()` call. It
verifies that the derived angle is actually used to decompose velocity, not computed and
then discarded by a copy error.

---

## 5.5 Unit Tests — Spin Vector Calculation (SV-)

**Owner:** §3.4 — Spin Vector Calculation
**Target count:** 8 tests
**Notes:** Spin is `Vector3` in rad/s. Sign conventions follow §3.4: topspin = positive
Y-axis rotation (forward roll), backspin = negative Y-axis, sidespin = Z-axis rotation.
All expected magnitudes calculated from §3.4 formulas.

| ID | Test Name | Inputs | Expected Result | Tolerance |
|----|-----------|--------|-----------------|-----------|
| SV-001 | Ground pass produces topspin (positive Y rotation) | `PassType.Ground` | `Spin.y > 0` | Exact sign |
| SV-002 | Chip pass produces backspin (negative Y rotation) | `PassType.Chip` | `Spin.y < 0` | Exact sign |
| SV-003 | Cross produces sidespin (non-zero Z rotation) | `PassType.Cross` | `Spin.z ≠ 0` | Exact non-zero |
| SV-004 | High Technique increases spin magnitude | `Technique=18` vs `Technique=5`, same pass type | `|Spin|_18 > |Spin|_5` | Strict inequality |
| SV-005 | Spin magnitude within Cross Magnus validation bounds | `PassType.Cross`, `Technique=10` | `|Spin| ≤ MAX_CROSS_SPIN` (from §3.4 Magnus check) | Exact ≤ |
| SV-006 | Spin magnitude within Chip Magnus validation bounds | `PassType.Chip`, `Technique=10` | `|Spin| ≤ MAX_CHIP_SPIN` | Exact ≤ |
| SV-007 | Spin vector calculation is deterministic | Same inputs × 3 calls | All 3 results identical | Exact |
| SV-008 | Spin=Vector3.zero is not produced for any non-zero technique | All 7 pass types, `Technique=10` | `|Spin| > 0` for all types | Strict non-zero |

**Notes:**

SV-005 and SV-006 implement the Magnus cross-spec calibration check identified in the
Outline critique (§CRITIQUE point 5). A spin value that exceeds Ball Physics Magnus force
calibration bounds would produce implausible curves. These tests act as the numerical
check between Pass Mechanics and Ball Physics §3.1 constants.

SV-008 is a regression guard: zero spin silently disables Magnus effect in Ball Physics,
producing flat trajectories for all passes. This must not occur in normal operation.

---

## 5.6 Unit Tests — Pass Error Model (PE-)

**Owner:** §3.5 — Deterministic Error Model
**Target count:** 10 tests
**Notes:** Error is an angular deviation in degrees. Expected values calculated from §3.5
formula. The formula is:

```
ErrorAngle = BASE_ERROR(PassType)
           × PassingModifier(Passing)
           × PressureModifier(PressureScalar)
           × FatigueModifier(Fatigue)
           × OrientationModifier(BodyAngle)
           × UrgencyModifier(Urgency)
           × WeakFootModifier(IsWeakFoot, WeakFootRating)
```

Each PE test isolates one modifier, holding all others at their neutral value (1.0
multiplier), to test the modifier in isolation.

| ID | Test Name | Inputs | Expected Result | Tolerance |
|----|-----------|--------|-----------------|-----------|
| PE-001 | Elite passer, no pressure, no fatigue, aligned body | `Passing=20`, `Pressure=0`, `Fatigue=0`, `BodyAngle=0°`, `Urgency=0`, `IsWeakFoot=false` | `ErrorAngle ≤ ELITE_ERROR(PassType)` where `ELITE_ERROR = BASE_ERROR × PASSING_ERROR_MIN` | ± 0.01° |
| PE-002 | Minimum passer, max pressure, full fatigue, misaligned | `Passing=1`, `Pressure=1.0`, `Fatigue=1.0`, `BodyAngle=90°`, `Urgency=HIGH`, `IsWeakFoot=true`, `WeakFootRating=1` | `ErrorAngle == MAX_ERROR(PassType)` (clamped) | ± 0.05° |
| PE-003 | Pressure modifier is monotonically increasing | `Pressure ∈ {0, 0.25, 0.5, 0.75, 1.0}`, fixed others at neutral | `ErrorAngle` strictly increases at each step | Strict monotone |
| PE-004 | Fatigue modifier is monotonically increasing | `Fatigue ∈ {0, 0.25, 0.5, 0.75, 1.0}`, fixed others at neutral | `ErrorAngle` strictly increases at each step | Strict monotone |
| PE-005 | Passing modifier is monotonically decreasing | `Passing ∈ {1, 5, 10, 15, 20}`, fixed others at neutral | `ErrorAngle` strictly decreases at each step | Strict monotone |
| PE-006 | Body orientation at 45° produces intermediate penalty | `BodyAngle=45°` vs `BodyAngle=0°` and `BodyAngle=90°` | `Error_0° < Error_45° < Error_90°` | Strict order |
| PE-007 | Urgency HIGH increases error vs Urgency LOW | `Urgency=HIGH` vs `Urgency=LOW`, fixed others | `Error_HIGH > Error_LOW` | Strict inequality |
| PE-008 | Weak foot false — no weak foot modifier applied | `IsWeakFoot=false` | `ErrorAngle` identical to weak foot term = 1.0 (neutral) | ± 0.001° |
| PE-009 | Error calculation is deterministic | Same inputs × 3 calls | All 3 results identical | Exact |
| PE-010 | Error angle is never exactly zero | `Passing=20`, all other inputs at best possible value | `ErrorAngle > 0` | Strict positive |

**Notes:**

PE-010 enforces KD-1 from Section 1: perfect passes may emerge from extreme attribute
values but the error floor is never zero. A literal zero angle means the ball always
reaches its exact intended destination regardless of target distance, which is physically
implausible and gameplay-exploitable.

PE-002 verifies the MAX_ERROR clamp. Without a clamp, the multiplicative formula can
produce angles large enough to redirect the ball backward, which would be nonsensical.

---

## 5.7 Unit Tests — Target Resolution (TR-)

**Owner:** §3.6 — Target Resolution (Player-Targeted and Space-Targeted)
**Target count:** 16 tests (8 player-targeted, 8 space-targeted)

### 5.7.1 Player-Targeted Tests (TR-001 – TR-008)

| ID | Test Name | Inputs | Expected Result | Tolerance |
|----|-----------|--------|-----------------|-----------|
| TR-001 | Player-targeted: correct target position extracted | `TargetType=Agent`, valid `TargetAgentId` | Returned position == agent's current `AgentState.Position` | ± 0.001m |
| TR-002 | Player-targeted: null agent ID produces FM-02 | `TargetType=Agent`, `TargetAgentId=null` | `PassResult.Outcome == FM-02` | Exact |
| TR-003 | Player-targeted: unknown agent ID produces FM-02 | `TargetType=Agent`, `TargetAgentId=INVALID_ID` | `PassResult.Outcome == FM-02` | Exact |
| TR-004 | Player-targeted: intent direction is agent-to-target vector | Valid agent and target, known positions | `IntendedDirection == Normalise(target - passer)` | ± 0.0001 (unit vector) |
| TR-005 | Player-targeted through ball: lead offset applied | `PassType.ThroughBall_Ground`, valid moving receiver | `TargetPosition ≠ receiver.Position` (lead applied) | Strict inequality |
| TR-006 | Through ball lead = velocity × WindupDuration (linear projection) | Receiver moving at 5 m/s, known WindupDuration | `Lead ≈ 5 × WindupDuration` | ± 5% |
| TR-007 | Through ball, stationary receiver: no lead offset applied | Receiver `Velocity == Vector3.zero` | `TargetPosition == receiver.Position` | ± 0.001m |
| TR-008 | Player-targeted: target on same team or opposing team both valid | Friendly and opposing `TargetAgentId` | Both resolve without FM-02 (Pass Mechanics is target-agnostic) | Exact |

### 5.7.2 Space-Targeted Tests (TR-009 – TR-016)

| ID | Test Name | Inputs | Expected Result | Tolerance |
|----|-----------|--------|-----------------|-----------|
| TR-009 | Space-targeted: uses `TargetPosition` directly | `TargetType=Space`, valid `TargetPosition` | Returned position == `TargetPosition` | ± 0.001m |
| TR-010 | Space-targeted: null `TargetPosition` produces FM-03 | `TargetType=Space`, `TargetPosition=null` | `PassResult.Outcome == FM-03` | Exact |
| TR-011 | Space-targeted: out-of-pitch position produces FM-03 | `TargetPosition` outside pitch bounds | `PassResult.Outcome == FM-03` | Exact |
| TR-012 | Space-targeted: intent direction is passer-to-space vector | Known passer position and space target | `IntendedDirection == Normalise(space - passer)` | ± 0.0001 |
| TR-013 | Space-targeted: no through-ball lead applied | `PassType.ThroughBall_Ground`, `TargetType=Space` | `TargetPosition` unchanged (lead is for agent targets only) | Exact |
| TR-014 | Space-targeted: position on pitch boundary is valid | `TargetPosition` on touchline exactly | No FM-03 produced | Exact |
| TR-015 | Unknown `TargetType` enum value produces FM-03 | `(TargetType)99` | `PassResult.Outcome == FM-03` | Exact |
| TR-016 | Target resolution is deterministic | Same inputs × 3 calls | All 3 results identical | Exact |

**Notes:**

TR-008 is important: Pass Mechanics is deliberately target-agnostic. Whether passing to
a teammate or an opponent (e.g., a deliberate back-pass to goalkeeper) is a Decision Tree
concern. Pass Mechanics resolves position regardless. This boundary must not erode.

TR-013 clarifies a potentially ambiguous case: through-ball lead is only meaningful when
targeting a moving agent. When targeting a space coordinate directly, no adjustment is
made — the caller has already accounted for movement by specifying the lead position.

---

## 5.8 Unit Tests — Weak Foot Penalty (WF-)

**Owner:** §3.7 — Weak Foot Penalty Model
**Target count:** 6 tests
**Dependency:** `[ERR-007-PENDING]` — all WF tests use `WeakFootRating`. Update
attribute name when confirmed.

| ID | Test Name | Inputs | Expected Result | Tolerance |
|----|-----------|--------|-----------------|-----------|
| WF-001 `[ERR-007-BLOCKED]` | WeakFoot=false applies no modifier | `IsWeakFoot=false`, any `WeakFootRating` | `WeakFootModifier == 1.0` (neutral) | Exact |
| WF-002 `[ERR-007-BLOCKED]` | WeakFoot=true, Rating=1 produces maximum penalty | `IsWeakFoot=true`, `WeakFootRating=1` | `WeakFootModifier == MAX_WF_MULTIPLIER` | ± 0.001 |
| WF-003 `[ERR-007-BLOCKED]` | WeakFoot=true, Rating=5 produces no penalty | `IsWeakFoot=true`, `WeakFootRating=5` | `WeakFootModifier == 1.0` (ambidextrous — fully neutral) | ± 0.001 |
| WF-004 `[ERR-007-BLOCKED]` | Weak foot modifier is monotonically decreasing with rating | `WeakFootRating ∈ {1, 2, 3, 4, 5}`, `IsWeakFoot=true` | Modifier decreases at each step | Strict monotone |
| WF-005 `[ERR-007-BLOCKED]` | Weak foot penalty visible in final ErrorAngle | `IsWeakFoot=true` vs `IsWeakFoot=false`, identical other inputs | `ErrorAngle_WeakFoot > ErrorAngle_StrongFoot` | Strict inequality |
| WF-006 | IsWeakFoot set by caller — PM does not compute it | `PassRequest.IsWeakFoot` field read directly | No attribute inspection occurs inside `PassExecutor.cs` | Code inspection / static analysis |

**Notes:**

WF-003 and WF-004 corrected in v1.1: the WeakFootRating attribute uses a [1, 5] scale
(not [1, 20] as used by most other attributes). WF-003 previously tested Rating=20 which
is out-of-range; corrected to Rating=5 (ambidextrous) which is the zero-penalty boundary.
WF-004 step values corrected to {1, 2, 3, 4, 5} to test all five meaningful levels.

WF-006 is a design contract test, not a formula test. It verifies KD-5 from Section 1:
Pass Mechanics reads `IsWeakFoot` from the `PassRequest` and does not independently
determine which foot the agent is using. This should be verified by code review /
static analysis rather than runtime assertion. Flag as "manual review" in the test plan.

---

## 5.9 Unit Tests — Execution State Machine (PSM-)

**Owner:** §3.8 — Execution State Machine
**Target count:** 6 tests
**States:** IDLE → INITIATING → WINDUP → CONTACT → FOLLOW_THROUGH → IDLE

| ID | Test Name | Inputs / Setup | Expected Result | Tolerance |
|----|-----------|----------------|-----------------|-----------|
| PSM-001 | Valid PassRequest transitions IDLE → INITIATING → WINDUP | Valid `PassRequest` received in IDLE | State == WINDUP after validation | Exact |
| PSM-002 | Invalid PassRequest rejected — state remains IDLE | `PassRequest` with `PassType=(PassType)99` | State remains IDLE; `PassResult.Outcome == FM-01` | Exact |
| PSM-003 | Windup timer completion transitions WINDUP → CONTACT | Advance frame count past `WindupDuration` | State == CONTACT; `Ball.ApplyKick()` call issued | Exact |
| PSM-004 | Tackle interrupt during WINDUP → IDLE + PassCancelledEvent | Collision System tackle interrupt signal in WINDUP | State == IDLE; `PassCancelledEvent` published | Exact |
| PSM-005 | Urgency HIGH reduces WindupDuration | `Urgency=HIGH` vs `Urgency=LOW` | `WindupDuration_HIGH < WindupDuration_LOW` | Strict inequality |
| PSM-006 | CONTACT issues exactly one ApplyKick call | Valid pass to CONTACT | `Ball.ApplyKick()` called exactly once | Exact count |

**Notes:**

PSM-006 is critical. Calling `Ball.ApplyKick()` twice in one pass execution would apply
double the kick impulse, launching the ball at implausible speed. The state machine must
enforce single-call semantics. Use a mock/spy on `Ball.ApplyKick()` to count invocations.

PSM-004 tests KD-8 (Section 1): tackle interrupt produces `PassCancelledEvent`. The test
verifies both the state machine transition and the event publication in a single scenario.

PSM-005 verifies that urgency has a mechanical consequence beyond error modulation. A
high-urgency pass both increases error (PE-007) and reduces windup time, making it
distinctly different from a low-urgency pass in all measurable ways.

---

## 5.10 Unit Tests — Edge Cases and Robustness (EC-)

**Owner:** §3.1–§3.9 (cross-cutting)
**Target count:** 8 tests
**Purpose:** Verify system stability under degenerate inputs, attribute boundary extremes,
and numerical edge conditions. None of these inputs should produce NaN, Infinity, divide-by-
zero, or an unhandled exception.

| ID | Test Name | Inputs | Expected Result | Tolerance |
|----|-----------|--------|-----------------|-----------|
| EC-001 | Attribute minimum boundary: all attributes = 1 | All `PlayerAttributes` = 1 | Valid `PassResult` with clamped-floor values; no exception | Exact |
| EC-002 | Attribute maximum boundary: all attributes = 20 | All `PlayerAttributes` = 20 | Valid `PassResult` with clamped-ceiling values; no exception | Exact |
| EC-003 | Zero velocity does not produce NaN spin | `KickPower=1`, `Distance=1m`, resulting V at V_MIN | `Spin` is valid Vector3, no NaN components | Exact |
| EC-004 | Passer at exact target position (Distance ≈ 0) | `Passer.Position == Target.Position` | FM-07 returned; no divide-by-zero | Exact |
| EC-005 | Fatigue exactly 1.0 (fully exhausted) | `Fatigue=1.0` | Valid `PassResult`; V == V_MIN (clamped); no exception | Exact |
| EC-006 | Pressure exactly 1.0 (maximum press) | `PressureScalar=1.0` | Valid `PassResult`; `ErrorAngle == MAX_ERROR` (clamped); no exception | Exact |
| EC-007 | PassRequest issued while state machine not IDLE | Second `PassRequest` while in WINDUP | FM-04 returned; state remains WINDUP; first pass unaffected | Exact |
| EC-008 | BodyAngle = 180° (completely facing away from target) | `BodyAngle=180°` | `OrientationModifier == MAX_ORIENTATION_PENALTY`; no exception | ± 0.001 |

**Notes:**

EC-007 addresses a race condition that could occur if Decision Tree issues a second pass
request before the first completes. Pass Mechanics must reject it cleanly, not attempt
to queue it or overwrite the current execution state.

EC-004 duplicates the zero-distance check from PV-012 but approaches it from the target
resolution layer rather than the velocity calculation layer, ensuring both the target
resolver and the velocity model handle the case defensively.

---

## 5.11 Integration Tests (IT-)

**Owner:** All — cross-system boundary verification
**Target count:** 12 tests
**Purpose:** Verify that correct outputs flow across the interfaces defined in Section 4.
Integration tests require simulated or mocked instances of Ball Physics, Agent Movement,
Collision System, and Event System.

| ID | Test Name | Systems Involved | Scenario | Expected Result |
|----|-----------|-----------------|----------|-----------------|
| IT-001 | Ground pass: ApplyKick called with non-zero velocity | Pass Mechanics + Ball Physics | Valid Ground pass to agent target | `Ball.ApplyKick(v, s, k)` called; `v.magnitude > 0`; `v.y` near zero |
| IT-002 | Lofted pass: ApplyKick velocity Y component positive | Pass Mechanics + Ball Physics | Valid Lofted pass, 40m | `v.y > 0` (ball leaves ground); `v.magnitude ∈ profile range` |
| IT-003 | Chip pass: ApplyKick velocity Y component large | Pass Mechanics + Ball Physics | Valid Chip pass, 10m | `v.y / v.magnitude > sin(40°)` (steep angle) |
| IT-004 `[ERR-008-BLOCKED]` | ApplyKick clears passer possession | Pass Mechanics + Ball Physics (PossessingAgentId) | Valid pass, check BallState after ApplyKick | `BallState.PossessingAgentId != PasserId` after call |
| IT-005 | Agent attributes read correctly from AgentState | Pass Mechanics + Agent Movement | Valid pass; verify attributes used match AgentState | Logged attribute values == values in mock `AgentState` |
| IT-006 | Fatigue from AgentState propagates to error calculation | Pass Mechanics + Agent Movement | Mock `AgentState.Fatigue=0.9`; compare to Fatigue=0 | `ErrorAngle_0.9 > ErrorAngle_0` |
| IT-007 | Tackle interrupt signal from Collision cancels pass | Pass Mechanics + Collision System | Initiate pass; fire tackle interrupt during WINDUP | State returns to IDLE; `PassCancelledEvent` published |
| IT-008 | PassAttemptEvent published on CONTACT | Pass Mechanics + Event System | Advance pass to CONTACT | `PassAttemptEvent` in event queue with correct payload |
| IT-009 | PassCompletedEvent published on FOLLOW_THROUGH completion | Pass Mechanics + Event System | Advance pass to FOLLOW_THROUGH completion | `PassCompletedEvent` in event queue with correct payload |
| IT-010 | PassCancelledEvent published on tackle interrupt | Pass Mechanics + Event System | Tackle interrupt in WINDUP | `PassCancelledEvent` in queue; `PassAttemptEvent` NOT in queue |
| IT-011 | Full pass cycle: IDLE → COMPLETE with correct events | Pass Mechanics + all systems | Full uninterrupted pass cycle | Events published in correct order; final state == IDLE |
| IT-012 | Replay determinism: two identical passes produce identical results | Pass Mechanics (all) | Execute same pass twice from reset state | All output values bit-identical |

**Notes:**

IT-012 is the determinism regression test. It must be run nightly as a scheduled CI job
(see §5.14.2). Any failure here is a CRITICAL halt — do not proceed until root cause is
resolved. The test resets all state between runs using a documented reset procedure in
`DeterministicTestHarness.cs`.

IT-011 is the full cycle smoke test. It does not replace individual tests but confirms
that the system produces the right events in the right order under normal conditions —
the "happy path." Failure here typically indicates an event publishing error in §3.9
rather than a formula error.

---

## 5.12 Real-World Validation Scenarios (VS-)

**Owner:** All
**Target count:** 6 scenarios
**Purpose:** Verify that Pass Mechanics produces physically plausible, football-realistic
outputs for canonical real-world archetypes. Each scenario is independently calculated by
hand from §3.x formulas before implementation, recorded as a fixture, and compared against
live system output. These are ground-truth regression tests, not exploratory tests.

All expected values in `PassTestFixtures.cs` are documented with full derivation comments
tracing each value back to the §3.x formula and the input attributes.

### VS-001 — Elite Short Ground Pass (Tiki-Taka Style)

**Archetype:** Elite technical passer, 8m ground pass, no pressure, rested.

| Parameter | Value |
|-----------|-------|
| PassType | Ground |
| Passing | 19 |
| KickPower | 14 `[ERR-007]` |
| Technique | 17 |
| Distance | 8m |
| Pressure | 0.05 |
| Fatigue | 0.1 |
| BodyAngle | 10° |
| Urgency | LOW |
| IsWeakFoot | false |

**Expected outputs (pre-calculated, documented in fixtures):**

| Output | Expected | Tolerance |
|--------|----------|-----------|
| Velocity | ≈ 11.5 m/s | ± 0.5 m/s |
| Launch Angle | ≈ 1.5° | ± 0.5° |
| Error Angle | ≤ 0.8° | Exact ≤ |
| Spin type | Topspin dominant | Sign check |

**Football realism check:** A 0.8° error on an 8m pass produces ≈ 0.11m lateral miss —
ball reaches the target's feet with a minor correction required. Appropriate for an elite
passer under minimal pressure.

---

### VS-002 — Pressured Long Driven Pass

**Archetype:** Average midfielder, 35m driven pass, heavy press, fatigued (80th minute).

| Parameter | Value |
|-----------|-------|
| PassType | Driven |
| Passing | 12 |
| KickPower | 14 `[ERR-007]` |
| Technique | 11 |
| Distance | 35m |
| Pressure | 0.8 |
| Fatigue | 0.75 |
| BodyAngle | 25° |
| Urgency | HIGH |
| IsWeakFoot | false |

**Expected outputs:**

| Output | Expected | Tolerance |
|--------|----------|-----------|
| Velocity | ≈ 22.0 m/s | ± 1.0 m/s |
| Launch Angle | ≈ 4.0° | ± 1.0° |
| Error Angle | ≈ 4.5° | ± 1.0° |
| Spin type | Topspin dominant | Sign check |

**Football realism check:** A 4.5° error on a 35m pass produces ≈ 2.75m lateral miss.
The receiver will need to move significantly or the pass may reach an opponent. Appropriate
for a pressured, fatigued passer playing quickly.

---

### VS-003 — Chip Pass Over Defensive Line

**Archetype:** Creative forward, 18m chip over a high defensive line, moderate pressure.

| Parameter | Value |
|-----------|-------|
| PassType | Chip |
| Passing | 15 |
| KickPower | 12 `[ERR-007]` |
| Technique | 16 |
| Distance | 18m |
| Pressure | 0.4 |
| Fatigue | 0.2 |
| BodyAngle | 15° |
| Urgency | MEDIUM |
| IsWeakFoot | false |

**Expected outputs:**

| Output | Expected | Tolerance |
|--------|----------|-----------|
| Velocity | ≈ 10.5 m/s | ± 0.5 m/s |
| Launch Angle | ≈ 55° | ± 5° |
| Error Angle | ≈ 2.0° | ± 0.5° |
| Spin type | Backspin dominant | Sign check |

**Football realism check:** A 55° launch angle produces a steeply lofted ball that drops
sharply — characteristic of a dinked chip. 2.0° error on 18m ≈ 0.63m lateral miss.
Appropriate for a technically good player under moderate pressure.

---

### VS-004 — Weak Foot Cross from Byline

**Archetype:** Attack-minded fullback, cross from deep on weak foot, moderate fatigue.

| Parameter | Value |
|-----------|-------|
| PassType | Cross |
| CrossSubType | Whipped |
| Passing | 13 |
| KickPower | 13 `[ERR-007]` |
| Technique | 12 |
| Crossing | 11 `[ERR-007]` |
| Distance | 28m |
| Pressure | 0.3 |
| Fatigue | 0.4 |
| BodyAngle | 20° |
| Urgency | MEDIUM |
| IsWeakFoot | true |
| WeakFootRating | 6 `[ERR-007]` |

**Expected outputs:**

| Output | Expected | Tolerance |
|--------|----------|-----------|
| Velocity | ≈ 19.5 m/s | ± 1.0 m/s |
| Launch Angle | ≈ 18° | ± 3° |
| Error Angle | ≈ 5.5° | ± 1.0° |
| Spin type | Sidespin dominant | Sign check |

**Football realism check:** 5.5° error on a 28m cross ≈ 2.7m lateral miss at the target
zone. For a cross, this is a realistic outcome — the target area is wide enough that a
striker can still attack the delivery, but the cross is far from precise. The weak foot
penalty (WeakFootRating=6) adds approximately 1.2° above the strong foot baseline.

---

### VS-005 — Elite Through Ball (Vision Play)

**Archetype:** Playmaker using through ball to split defensive line, receiver moving at speed.
Uses `Technique` as Vision proxy per KD-7.

| Parameter | Value |
|-----------|-------|
| PassType | ThroughBall_Ground |
| Passing | 18 |
| KickPower | 15 `[ERR-007]` |
| Technique | 19 (Vision proxy) |
| Distance | 22m (to projected position) |
| Receiver velocity | 6 m/s |
| WindupDuration | 0.4s |
| Pressure | 0.2 |
| Fatigue | 0.15 |
| BodyAngle | 8° |
| Urgency | LOW |
| IsWeakFoot | false |

**Expected outputs:**

| Output | Expected | Tolerance |
|--------|----------|-----------|
| Lead offset | ≈ 2.4m (6 × 0.4) | ± 0.1m |
| Velocity | ≈ 14.5 m/s | ± 0.5 m/s |
| Error Angle | ≤ 0.9° | Exact ≤ |
| Spin type | Topspin dominant | Sign check |

**Football realism check:** Lead of 2.4m means the ball is played to where the receiver
will be 0.4s later — a well-timed through ball. Error ≤ 0.9° on a 22m pass ≈ ≤ 0.35m
lateral miss. For an elite player with minimal pressure, this is a highly accurate pass
that splits the defence cleanly.

---

### VS-006 — Minimum Competence Pass (Youth-Level Archetype)

**Archetype:** Low-attribute player, short ground pass, no pressure. Tests that even minimum
attribute combinations produce valid, football-realistic outputs rather than degenerate values.

| Parameter | Value |
|-----------|-------|
| PassType | Ground |
| Passing | 3 |
| KickPower | 4 `[ERR-007]` |
| Technique | 3 |
| Distance | 12m |
| Pressure | 0.1 |
| Fatigue | 0.05 |
| BodyAngle | 5° |
| Urgency | LOW |
| IsWeakFoot | false |

**Expected outputs:**

| Output | Expected | Tolerance |
|--------|----------|-----------|
| Velocity | ≥ V_MIN_Ground (clamped) | Exact ≥ |
| Launch Angle | ≈ 1.5° | ± 1.0° |
| Error Angle | ≈ 3.5° | ± 1.0° |
| PassResult.Outcome | PassOutcome.Attempted | Exact |

**Football realism check:** 3.5° on 12m ≈ 0.73m lateral miss. For a poor passer, this
is realistic — the pass arrives in the receiver's vicinity but requires adjustment. The
clamp ensures velocity is never below V_MIN; a minimum-power player is simply limited,
not broken.

---

