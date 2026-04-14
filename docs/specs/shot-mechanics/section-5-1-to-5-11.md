# Shot Mechanics Specification #6 — Section 5: Testing

**File:** `Shot_Mechanics_Spec_Section_5_v1_3.md`
**Purpose:** Defines all test requirements, acceptance criteria, and validation methodology
for the Shot Mechanics system (Spec #6). Establishes unit tests, integration tests, and
real-world validation scenarios to verify correctness of: Parameter Validation (§3.1),
Velocity Calculation (§3.2), Launch Angle Derivation (§3.3), Spin Vector Calculation (§3.4),
Placement Resolution (§3.5), Error Model (§3.6), Body Mechanics Evaluation (§3.7), Weak
Foot Penalty (§3.8), Shot State Machine (§3.9), and Event Publishing (§3.10). This section
is the authoritative test reference for Stage 0 implementation sign-off.

**Created:** February 23, 2026, 11:59 PM PST
**Revised:** February 23, 2026
**Version:** 1.3
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 6 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)
**Prerequisites:** Section 1 v1.1, Section 2 v1.0, Section 3 (§3.1–§3.10), Section 4 v1.3

**Open Dependency Flags:** None. All hard dependencies confirmed stable in Section 1.
All attribute names (Finishing, LongShots, KickPower, Composure, Technique,
WeakFootRating) confirmed in Agent Movement Spec #2 §3.5.6 v1.3.

---

## Table of Contents

- [5.1 Test Strategy](#51-test-strategy)
- [5.2 Unit Tests — Parameter Validation (PV-)](#52-unit-tests--parameter-validation-pv-)
- [5.3 Unit Tests — Velocity Model (SV-)](#53-unit-tests--velocity-model-sv-)
- [5.4 Unit Tests — Launch Angle Derivation (LA-)](#54-unit-tests--launch-angle-derivation-la-)
- [5.5 Unit Tests — Spin Vector Calculation (SN-)](#55-unit-tests--spin-vector-calculation-sn-)
- [5.6 Unit Tests — Placement Resolution (SP-)](#56-unit-tests--placement-resolution-sp-)
- [5.7 Unit Tests — Error Model (SE-)](#57-unit-tests--error-model-se-)
- [5.8 Unit Tests — Body Mechanics (BM-)](#58-unit-tests--body-mechanics-bm-)
- [5.9 Unit Tests — Weak Foot Penalty (WF-)](#59-unit-tests--weak-foot-penalty-wf-)
- [5.10 Unit Tests — State Machine (SSM-)](#510-unit-tests--state-machine-ssm-)
- [5.11 Unit Tests — Edge Cases and Robustness (EC-)](#511-unit-tests--edge-cases-and-robustness-ec-)
- [5.12 Integration Tests (IT-)](#512-integration-tests-it-)
- [5.13 Real-World Validation Scenarios (VS-)](#513-real-world-validation-scenarios-vs-)
- [5.14 Acceptance Criteria Summary](#514-acceptance-criteria-summary)
- [5.15 Test Execution Plan](#515-test-execution-plan)
- [5.16 Section Summary](#516-section-summary)
- [5.17 Version History](#517-version-history)

---

## 5.1 Test Strategy

### 5.1.1 Test Pyramid

```
          /\
         /  \     E2E Tests (5%)
        /____\    - Full 90-minute match simulation
       /      \   - Replay determinism verification; shot count plausibility
      /        \
     / Integr-  \ Integration Tests (20%)
    /  ation     \ - Cross-system boundary scenarios
   /______________\ - Shot Mechanics ↔ Ball Physics, Agent Movement, Collision, Event System
  /                \
 /   Unit Tests     \ Unit Tests (75%)
/__(90+ tests)______\ - Formula validation, edge cases, boundary conditions
```

**Distribution rationale:**

Unit tests are fast (< 15s total) and enable rapid iteration during TDD. Every formula
term, every constant, and every branch condition in §3.1–§3.10 has a corresponding unit
test. Integration tests verify that correct outputs flow between systems as defined in
the Section 4 interface contracts. E2E tests run pre-milestone only; they confirm no NaN
propagation, stable memory, and deterministic replay across a full simulated match.

Shot Mechanics unit tests are more demanding than Pass Mechanics because the consequence
of error is higher: a mistakenly high velocity (> 35 m/s) could cause Ball Physics
divergence. Boundary verification is therefore treated as first-class test concern, not
an afterthought.

### 5.1.2 Determinism Requirement

**All tests produce identical results across runs without exception.**

No test uses `System.Random`, `UnityEngine.Random`, or any other non-deterministic source.
Tests use fixed attribute values, fixed positions, fixed facing vectors, and fixed frame
numbers. The deterministic hash `matchSeed + agentId + frameNumber` must produce the same
error direction for identical inputs on any platform.

Determinism is the single most important correctness property for Shot Mechanics. A
non-deterministic shot system makes replay verification impossible and corrupts goal
statistics. IT-012 (determinism regression) is the CRITICAL blocking test — if it fails,
halt all other work immediately.

### 5.1.3 Test Categories

Tests are categorised by prefix:

| Prefix | Category | §3 Owner | Count |
|--------|----------|----------|-------|
| `PV-`  | Parameter Validation | §3.1 | 8 |
| `SV-`  | Velocity Model | §3.2 | 12 |
| `LA-`  | Launch Angle Derivation | §3.3 | 8 |
| `SN-`  | Spin Vector Calculation | §3.4 | 8 |
| `SP-`  | Placement Resolution | §3.5 | 10 |
| `SE-`  | Error Model | §3.6 | 10 |
| `BM-`  | Body Mechanics Evaluation | §3.7 | 8 |
| `WF-`  | Weak Foot Penalty | §3.8 | 6 |
| `SSM-` | Shot State Machine | §3.9 | 8 |
| `EC-`  | Edge Cases / Robustness | §3.1–§3.10 | 8 |
| `IT-`  | Integration Tests | All | 12 |
| `VS-`  | Validation Scenarios | All | 6 |
| **Total** | | | **104** |

### 5.1.4 Test Tooling

| Tool | Purpose |
|------|---------|
| Unity Test Framework (NUnit) | Test runner — Edit Mode for pure logic, Play Mode for integration |
| Unity Profiler | Performance validation against §5.1.5 budgets |
| Unity Code Coverage | Line coverage tracking — target > 85% for §3.x code |
| Custom `DeterministicTestHarness` | Fixed-seed frame counter for deterministic evaluation |
| `ShotTestFixtures.cs` | Shared test data — canonical agent profiles and expected values |
| `MutableAgentPhysicsStateMock` | Required for IT-002. Mock supporting mid-execution `AgentPhysicalProperties` mutation to verify property freeze at INITIATING. Must not be used in unit tests. |
| `IShotVelocityCalculator` + `NaNVelocityStub` | Required for EC-008. Injectable interface and NaN-returning stub. Seam confirmed in `ShotExecutor` constructor — see §4 Amendment 1B. `NaNVelocityStub` lives in `/Tests/` only. |
| `GoalGeometryProvider.SetTestOverride()` | Required for SP-009. Test-only goal geometry override — see §4 Amendment 1A. Available in `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` builds only. Pair every call with `ClearTestOverride()` in `[TearDown]`. |

### 5.1.5 Performance Budget (Reference)

Tests that include timing assertions use the following budgets derived from Section 6:

| Metric | Target |
|--------|--------|
| `ExecuteShot()` mean | < 0.017ms per call |
| `ExecuteShot()` p99 | < 0.05ms per call |
| Full unit test suite | < 15 seconds total |
| Full integration suite | < 60 seconds total |
| Full suite including VS | < 5 minutes total |

### 5.1.6 Test Fixture Philosophy

All expected numeric outputs are pre-calculated from §3.2–§3.6 governing formulas and
documented in `ShotTestFixtures.cs` with inline derivation comments. No magic numbers
appear in test assertions. Every expected value must trace to a formula and a constant
in `ShotConstants.cs`. If a constant is tuned during playtesting, the affected test
fixture values must be updated and the delta documented in `ShotConstants.cs` comments.

---

## 5.2 Unit Tests — Parameter Validation (PV-)

**Owner:** §3.1 — ShotRequest Validation (ShotValidator.cs)
**Target count:** 8 tests
**Purpose:** Verify that every malformed `ShotRequest` is rejected before any physics
calculation begins. Validation failures must leave `BallState` unchanged and return
`ShotResult{ Outcome = ShotOutcome.Invalid }`.

| ID | Test Name | Inputs | Expected Result | Tolerance |
|----|-----------|--------|-----------------|-----------|
| PV-001 | Valid request passes all checks | All fields in range, agent has possession | `ShotResult.Outcome == ShotOutcome.Completed` | Exact |
| PV-002 | `PowerIntent` above upper bound rejected | `PowerIntent = 1.01` | `ShotResult.Outcome == ShotOutcome.Invalid`; no `Ball.ApplyKick()` call | Exact |
| PV-003 | `PowerIntent` below lower bound rejected | `PowerIntent = -0.01` | `ShotResult.Outcome == ShotOutcome.Invalid`; no `Ball.ApplyKick()` call | Exact |
| PV-004 | `SpinIntent` out of range rejected | `SpinIntent = 1.5` | `ShotResult.Outcome == ShotOutcome.Invalid`; no `Ball.ApplyKick()` call | Exact |
| PV-005 | `PlacementTarget.u` out of range rejected | `PlacementTarget = (1.1, 0.5)` | `ShotResult.Outcome == ShotOutcome.Invalid`; no `Ball.ApplyKick()` call | Exact |
| PV-006 | `PlacementTarget.v` out of range rejected | `PlacementTarget = (0.5, -0.1)` | `ShotResult.Outcome == ShotOutcome.Invalid`; no `Ball.ApplyKick()` call | Exact |
| PV-007 | Invalid `ContactZone` enum value rejected | `ContactZone = (ContactZone)99` | `ShotResult.Outcome == ShotOutcome.Invalid`; no `Ball.ApplyKick()` call | Exact |
| PV-008 | Agent without possession rejected | Valid `ShotRequest`; `AgentSystem.GetPossessor()` returns a different agent ID (ERR-008 Option B — no `PossessingAgentId` on `BallState`) | `ShotResult.Outcome == ShotOutcome.Invalid`; no `Ball.ApplyKick()` call | Exact |

**Notes:**

PV-001 is the baseline sanity test. If this fails, no other test result is trustworthy.
Run PV-001 first in any diagnostic session.

PV-007 guards against Unity serialisation bugs. C# enums do not enforce bounds at
runtime; a corrupted `ShotRequest` from a serialised match state could pass a nominal
`ContactZone` field containing an undefined integer value. The validator must check
enum membership explicitly rather than relying on C# type safety alone.

PV-008 tests the possession guard. The Decision Tree is responsible for not submitting
shots for non-possessing agents, but Shot Mechanics must also defend against this in
case of Decision Tree bugs. Critically, this validation must not call `Ball.ApplyKick()`.
Use a mock/spy to confirm zero calls.

---

## 5.3 Unit Tests — Velocity Model (SV-)

**Owner:** §3.2 — Velocity Calculation (ShotVelocityCalculator.cs)
**Target count:** 12 tests
**Purpose:** Verify the Finishing/LongShots sigmoid blend, ContactZone modifiers,
spin–velocity trade-off, fatigue modifier, and ContactQualityModifier. All expected
values pre-calculated from §3.2 formulas and stored in `ShotTestFixtures.cs`.

All expected velocity values are calculated from §3.2 formulas and documented in
`ShotTestFixtures.cs` with inline derivation comments. Magic numbers in test assertions
are forbidden.

| ID | Test Name | Inputs | Expected Result | Tolerance |
|----|-----------|--------|-----------------|-----------|
| SV-001 | Close range: Finishing dominates sigmoid blend | `DistanceToGoal = 6m`, `Finishing=18`, `LongShots=8`, `KickPower=14`, `PowerIntent=0.8`, `Fatigue=0` | Velocity closer to Finishing-only prediction than LongShots-only prediction | Strict: \|V - V_Finishing\| < \|V - V_LongShots\| |
| SV-002 | Long range: LongShots dominates sigmoid blend | `DistanceToGoal = 28m`, `Finishing=8`, `LongShots=18`, `KickPower=14`, `PowerIntent=0.8`, `Fatigue=0` | Velocity closer to LongShots-only prediction than Finishing-only prediction | Strict: \|V - V_LongShots\| < \|V - V_Finishing\| |
| SV-003 | Maximum power, elite attributes, Centre contact | `PowerIntent=1.0`, `Finishing=20`, `LongShots=20`, `KickPower=20`, `ContactZone=Centre`, `Fatigue=0`, `BodyMechanicsScore=1.0` | `V ∈ [28, 35] m/s` | ± 0.5 m/s |
| SV-004 | Minimum power, minimum attributes, Centre contact | `PowerIntent=0.1`, `Finishing=1`, `LongShots=1`, `KickPower=1`, `ContactZone=Centre`, `Fatigue=0` | `V ∈ [V_ABSOLUTE_MIN, 12] m/s` and `V >= V_ABSOLUTE_MIN (8.0)` | ± 0.1 m/s |
| SV-005 | BelowCentre contact reduces velocity vs Centre | `ContactZone=BelowCentre` vs `ContactZone=Centre`, identical other inputs | `V_BelowCentre < V_Centre` | Strict inequality |
| SV-006 | OffCentre contact reduces velocity vs Centre | `ContactZone=OffCentre` vs `ContactZone=Centre`, identical other inputs | `V_OffCentre < V_Centre` | Strict inequality |
| SV-007 | High SpinIntent reduces velocity | `SpinIntent=1.0` vs `SpinIntent=0.0`, `ContactZone=OffCentre`, identical other inputs | `V_SpinMax < V_SpinZero` | Strict inequality |
| SV-008 | SpinIntent=0.0 applies no spin–velocity trade-off | `SpinIntent=0.0`, any ContactZone | Spin trade-off multiplier == 1.0 (no reduction from this term) | Exact |
| SV-009 | Fatigue=1.0 reduces velocity vs Fatigue=0.0 | `Fatigue=1.0` vs `Fatigue=0.0`, identical other inputs | `V_Fatigued < V_Fresh` | Strict inequality |
| SV-010 | Fatigue modifier is monotonically decreasing | `Fatigue ∈ {0.0, 0.25, 0.5, 0.75, 1.0}`, fixed others | `V` strictly decreases at each step | Strict monotone |
| SV-011 | Velocity is clamped at V_ABSOLUTE_MAX (35 m/s) | `PowerIntent=1.0`, all attributes=20, `ContactZone=Centre`, `Fatigue=0`, `BodyMechanicsScore=1.0` | `V ≤ 35.0 m/s` | Exact ≤ |
| SV-012 | Velocity calculation is deterministic | Same inputs × 3 calls | All 3 results bitwise-identical | Exact |

**Notes:**

SV-001 and SV-002 verify the core architectural decision of OI-001 (sigmoid blend).
A regression to a hard threshold would cause a discontinuous jump in velocity at the
threshold distance — these two tests bracket the blend and would catch that failure.

SV-003 and SV-011 together verify the V_ABSOLUTE_MAX clamp: SV-003 confirms that elite
inputs produce realistically high velocities, SV-011 confirms they never exceed the
Ball Physics safety ceiling. Neither test alone is sufficient.

SV-012 (determinism) must be run first in any diagnostic session involving the velocity
model. Non-deterministic velocity would corrupt all validation scenarios.

---

## 5.4 Unit Tests — Launch Angle Derivation (LA-)

**Owner:** §3.3 — Launch Angle Derivation (ShotLaunchAngleCalculator.cs)
**Target count:** 8 tests
**Purpose:** Verify BaseAngle by ContactZone, power lift modifier, spin lift modifier,
body lean penalty, and body shape penalty. All expected angles derived from §3.3
formulas. Output unit is degrees.

| ID | Test Name | Inputs | Expected Result | Tolerance |
|----|-----------|--------|-----------------|-----------|
| LA-001 | Centre contact, high power produces low launch angle | `ContactZone=Centre`, `PowerIntent=0.9`, `SpinIntent=0.0`, `BodyLean=0°` | `LaunchAngle ∈ [2°, 8°]` | ± 0.5° |
| LA-002 | BelowCentre contact produces higher launch angle than Centre | `ContactZone=BelowCentre` vs `ContactZone=Centre`, same PowerIntent and SpinIntent | `Angle_BelowCentre > Angle_Centre` | Strict inequality |
| LA-003 | OffCentre contact produces higher launch angle than Centre | `ContactZone=OffCentre` vs `ContactZone=Centre`, same `PowerIntent=0.6` and `SpinIntent=0.5` | `Angle_OffCentre > Angle_Centre` | Strict inequality |
| LA-004 | High SpinIntent with BelowCentre contact elevates launch angle | `ContactZone=BelowCentre`, `SpinIntent=1.0` vs `SpinIntent=0.0` | `Angle_SpinHigh > Angle_SpinZero` | Strict inequality |
| LA-005 | High PowerIntent with Centre contact suppresses lift | `ContactZone=Centre`, `PowerIntent=1.0` vs `PowerIntent=0.3` | `Angle_PowerHigh < Angle_PowerLow` | Strict inequality |
| LA-006 | Body lean penalty increases launch angle | `BodyLean > 0°` vs `BodyLean = 0°`, identical other inputs | `Angle_Leaning > Angle_Upright` (leaning back skies the ball) | Strict inequality |
| LA-007 | Launch angle is clamped within valid range | `BodyLean = MAX_LEAN`, `SpinIntent=1.0`, `ContactZone=BelowCentre` (worst-case stack) | `LaunchAngle ≤ MAX_LAUNCH_ANGLE` constant | Exact ≤ |
| LA-008 | Derived angle is encoded into final velocity vector Z component | Valid `ShotRequest`; capture final `FinalVelocity` from `ShotResult` | `asin(FinalVelocity.y / |FinalVelocity|) ≈ LaunchAngle` | ± 0.5° |

**Notes:**

LA-003 was revised in v1.1 from a three-way ordering assertion (`Centre < OffCentre < BelowCentre`)
to a weaker two-point assertion (`OffCentre > Centre`). The three-way ordering assumed that
OffCentre BaseAngle sits strictly between the other two zones — but §3.3 does not guarantee
this ordering, and a valid tuning decision could place OffCentre BaseAngle closer to Centre
without constituting a bug. The weakened assertion captures the essential physical intent
(OffCentre produces more loft than a pure Centre contact) without over-specifying the
constant relationships before tuning is complete.

LA-006 is a critical physical correctness test. Backward body lean (leaning away from
the ball) reduces foot contact precision and elevates the ball. This is the primary
physical cause of shots going over the bar. If the sign of the body lean penalty is
accidentally inverted, shots would be suppressed by poor posture rather than elevated —
a nonsensical output. LA-006 must be run after any §3.3 refactor.

LA-008 is the bridge test between angle calculation and the `Ball.ApplyKick()` call. It
verifies that `LaunchAngleDeg` is not computed and then silently discarded before
velocity vector construction. This is a copy-error class of bug — plausible but not
caught by formula correctness tests alone.

---

## 5.5 Unit Tests — Spin Vector Calculation (SN-)

**Owner:** §3.4 — Spin Vector Calculation (ShotSpinCalculator.cs)
**Target count:** 8 tests
**Purpose:** Verify topspin, sidespin, and backspin component signs and magnitudes for
each ContactZone and SpinIntent combination. Spin is `Vector3` in rad/s. Sign conventions
follow §3.4: topspin = positive Y-axis rotation (forward roll); backspin = negative Y;
sidespin = ±Z-axis depending on OffCentre orientation.

| ID | Test Name | Inputs | Expected Result | Tolerance |
|----|-----------|--------|-----------------|-----------|
| SN-001 | Centre contact, SpinIntent=0 produces topspin-dominant spin | `ContactZone=Centre`, `SpinIntent=0.0`, `Technique=10` | `Spin.y > 0` (topspin); `\|Spin.y\| > \|Spin.z\|` | Exact sign + dominance |
| SN-002 | BelowCentre contact produces backspin (negative Y rotation) | `ContactZone=BelowCentre`, `SpinIntent=0.6`, `Technique=10` | `Spin.y < 0` | Exact sign |
| SN-003 | OffCentre contact produces sidespin (non-zero Z rotation) | `ContactZone=OffCentre`, `SpinIntent=0.7`, `Technique=10` | `\|Spin.z\| > 0` | Exact non-zero |
| SN-004 | Higher Technique increases sidespin magnitude | `Technique=18` vs `Technique=5`, `ContactZone=OffCentre`, `SpinIntent=0.8` | `\|Spin.z\|_18 > \|Spin.z\|_5` | Strict inequality |
| SN-005 | SpinIntent=0.0 produces minimum spin (not zero) | `SpinIntent=0.0`, all ContactZone values | `\|Spin\| > 0` for all; no zero vector | Strict non-zero |
| SN-006 | Spin magnitude within Ball Physics Magnus calibration bounds | `ContactZone=OffCentre`, `SpinIntent=1.0`, `Technique=20` | `\|Spin\| ≤ MAX_SPIN_MAGNUS_BOUND` (from §3.4 / Ball Physics cross-check) | Exact ≤ |
| SN-007 | SpinIntent=1.0 produces maximum spin, constrained by Magnus bound | `SpinIntent=1.0` vs `SpinIntent=0.5`, same ContactZone and Technique | `\|Spin\|_SpinIntentMax > \|Spin\|_SpinIntentHalf` and both ≤ `MAX_SPIN_MAGNUS_BOUND` | Strict inequality + bound |
| SN-008 | Spin vector calculation is deterministic | Same inputs × 3 calls | All 3 results bitwise-identical | Exact |

**Notes:**

SN-005 is a correctness guard: zero spin silently disables the Magnus effect in Ball
Physics, producing physically implausible flat trajectories for chip and curl shots.
Even minimum SpinIntent should contribute meaningful, non-zero angular momentum.

SN-006 and SN-007 together implement the cross-specification Magnus calibration check.
A spin value that exceeds Ball Physics's force calibration range would produce implausible
deflections. The constant `MAX_SPIN_MAGNUS_BOUND` is defined jointly with Ball Physics
§3.1 and must not be changed in isolation.

---

## 5.6 Unit Tests — Placement Resolution (SP-)

**Owner:** §3.5 — Placement Resolution (ShotPlacementResolver.cs)
**Target count:** 10 tests
**Purpose:** Verify that goal-relative placement coordinates `(u, v)` map correctly to
world-space aim directions. Tests cover all corner targets, centre-goal target,
goal geometry constants, and aim direction normalisation.

| ID | Test Name | Inputs | Expected Result | Tolerance |
|----|-----------|--------|-----------------|-----------|
| SP-001 | Centre target (0.5, 0.5) aims at goal centre | `PlacementTarget = (0.5, 0.5)`, known goal geometry | `AimDirection` points at world-space goal centre ± goal width / 40 | ± 0.02m at goal line |
| SP-002 | Left post target (0.0, 0.5) aims near left post | `PlacementTarget = (0.0, 0.5)` | `AimDirection` world-space x ≈ left_post_x | ± `PLACEMENT_TOLERANCE` m |
| SP-003 | Right post target (1.0, 0.5) aims near right post | `PlacementTarget = (1.0, 0.5)` | `AimDirection` world-space x ≈ right_post_x | ± `PLACEMENT_TOLERANCE` m |
| SP-004 | Bottom-left corner target (0.0, 0.0) | `PlacementTarget = (0.0, 0.0)` | Aim direction points at bottom-left of goal mouth | ± `PLACEMENT_TOLERANCE` m |
| SP-005 | Top-right corner target (1.0, 1.0) | `PlacementTarget = (1.0, 1.0)` | Aim direction points at top-right of goal mouth | ± `PLACEMENT_TOLERANCE` m |
| SP-006 | Aim direction is a unit vector | Any valid `PlacementTarget` | `\|AimDirection\| ≈ 1.0` | ± 0.0001 |
| SP-007 | u-axis monotonicity: increasing u moves aim rightward | `u ∈ {0.0, 0.25, 0.5, 0.75, 1.0}`, fixed v=0.5 | World-space x of aim direction strictly increases | Strict monotone |
| SP-008 | v-axis monotonicity: increasing v moves aim upward | `v ∈ {0.0, 0.25, 0.5, 0.75, 1.0}`, fixed u=0.5 | World-space y of aim direction strictly increases | Strict monotone |
| SP-009 | Placement resolution uses goal geometry constants from ShotConstants | `PlacementTarget=(0.5, 0.5)`, override `GOAL_WIDTH` to known test value | Aim direction matches expected position under test constant | ± 0.001m |
| SP-010 | Placement resolution is deterministic | Same `PlacementTarget` × 3 calls, same agent position and goal geometry | All 3 results bitwise-identical | Exact |

**Notes:**

SP-007 and SP-008 (monotonicity) are regression guards for goal-space-to-world-space
coordinate conversion. Axis inversion or origin mismatch — the most common geometry
bugs — would violate monotonicity without necessarily producing an obvious failure in
any single point test.

SP-009 verifies that `PlacementResolver` reads goal geometry from `ShotConstants` rather
than hardcoding it. This is essential for supporting non-standard goal dimensions in
future Stadium Editor features (Stage 3+) without modifying resolver logic.

**SP-009 Implementation:** Resolved by Section 4 Amendment 1A. `GoalGeometryProvider.cs`
is added to the module providing a `SetTestOverride()` / `ClearTestOverride()` method
pair, available only in `#if UNITY_EDITOR || DEVELOPMENT_BUILD` builds. Zero production
runtime cost. `ShotPlacementResolver` must call `GoalGeometryProvider.Get()` for all
goal geometry — never `ShotConstants` directly. See Amendment 1A for full implementation
and the reference SP-009 test body.

---

## 5.7 Unit Tests — Error Model (SE-)

**Owner:** §3.6 — Deterministic Error Model (ShotErrorCalculator.cs)
**Target count:** 10 tests
**Purpose:** Verify each error scalar in isolation (Finishing attribute, power penalty,
pressure scalar, fatigue scalar, body shape scalar), the deterministic hash direction,
and the composite MAX_ERROR clamp. Each test isolates one modifier by holding all others
at their neutral value (1.0 multiplier).

| ID | Test Name | Inputs | Expected Result | Tolerance |
|----|-----------|--------|-----------------|-----------|
| SE-001 | Elite finishing, no pressure, fresh, ideal body shape | `Finishing=20`, `PowerIntent=0.5`, `Pressure=0.0`, `Fatigue=0.0`, `BodyMechanicsScore=1.0`, `IsWeakFoot=false` | `ErrorAngle ≤ ELITE_SHOT_ERROR` (constant from §3.6) | ± 0.01° |
| SE-002 | Minimum finishing, maximum pressure, full fatigue, poor body shape | `Finishing=1`, `PowerIntent=1.0`, `Pressure=1.0`, `Fatigue=1.0`, `BodyMechanicsScore=0.0`, `IsWeakFoot=true`, `WeakFootRating=1` | `ErrorAngle == MAX_ERROR` (clamped) | ± 0.05° |
| SE-003 | Power penalty: PowerIntent=1.0 produces higher error than PowerIntent=0.4 | `PowerIntent=1.0` vs `PowerIntent=0.4`, neutral modifiers | `ErrorAngle_MaxPower > ErrorAngle_MedPower` | Strict inequality |
| SE-004 | Power penalty is monotonically increasing with PowerIntent | `PowerIntent ∈ {0.1, 0.3, 0.5, 0.7, 0.9, 1.0}`, neutral other inputs | Error strictly increases at each step | Strict monotone |
| SE-005 | Pressure scalar is monotonically increasing with pressure | `Pressure ∈ {0.0, 0.25, 0.5, 0.75, 1.0}`, neutral other inputs | Error strictly increases at each step | Strict monotone |
| SE-006 | Fatigue scalar is monotonically increasing with fatigue | `Fatigue ∈ {0.0, 0.25, 0.5, 0.75, 1.0}`, neutral other inputs | Error strictly increases at each step | Strict monotone |
| SE-007 | Finishing attribute modifier is monotonically decreasing | `Finishing ∈ {1, 5, 10, 15, 20}`, neutral other inputs | Error strictly decreases at each step | Strict monotone |
| SE-008 | BodyMechanicsScore modifier: low score increases error | `BodyMechanicsScore=0.2` vs `BodyMechanicsScore=0.9`, neutral other inputs | `ErrorAngle_PoorMechanics > ErrorAngle_GoodMechanics` | Strict inequality |
| SE-009 | Error direction is deterministic: same seed yields same direction | `matchSeed=12345`, `agentId=7`, `frameNumber=3600` × 3 calls | All 3 error direction vectors bitwise-identical | Exact |
| SE-010 | Error angle is never exactly zero | `Finishing=20`, all inputs at most favourable value | `ErrorAngle > 0` | Strict positive |

**Notes:**

SE-010 enforces the design principle established in Section 1 KD-1: perfect shots are
not possible. An `ErrorAngle` of exactly zero would mean the ball always reaches its
exact intended target point regardless of distance — physically implausible and
gameplay-exploitable. The error floor must be a positive constant even for elite
attributes.

SE-009 is the determinism test for the hash function. It must be run after any change
to `DeterministicHash()` or its seed composition. The specific seed values (12345, 7,
3600) are canonical test constants and must not be changed without updating
`ShotTestFixtures.cs`.

---

## 5.8 Unit Tests — Body Mechanics (BM-)

**Owner:** §3.7 — Body Mechanics Evaluation (BodyMechanicsEvaluator.cs)
**Target count:** 8 tests
**Purpose:** Verify run-up angle score, plant foot offset score, agent velocity at contact
score, body lean contribution, and the composite `BodyMechanicsScore` ∈ [0.0, 1.0].
Also verifies the stumble trigger condition.

| ID | Test Name | Inputs | Expected Result | Tolerance |
|----|-----------|--------|-----------------|-----------|
| BM-001 | Perfect run-up (0° offset): maximum RunUpScore | `RunUpAngle = 0°` (agent facing perfectly toward target) | `RunUpScore == 1.0` (or within tolerance of maximum) | ± 0.01 |
| BM-002 | 90° run-up angle reduces RunUpScore significantly | `RunUpAngle = 90°` | `RunUpScore < 0.5` | Strict < |
| BM-003 | Ideal plant foot offset: maximum PlantFootScore | `PlantFootOffset = IDEAL_PLANT_OFFSET` (within optimal zone) | `PlantFootScore ≈ 1.0` | ± 0.05 |
| BM-004 | Excessive plant foot offset reduces PlantFootScore | `PlantFootOffset > MAX_EFFECTIVE_PLANT_OFFSET` | `PlantFootScore < 0.5` | Strict < |
| BM-005 | Moderate approach velocity improves BodyMechanicsScore | `AgentVelocity = OPTIMAL_SHOT_APPROACH_SPEED` vs `AgentVelocity = 0.0` (stationary) | `Score_Moving > Score_Stationary` | Strict inequality |
| BM-006 | BodyMechanicsScore is always in [0.0, 1.0] | Sweep of extreme input combinations (worst-case stack) | `0.0 ≤ BodyMechanicsScore ≤ 1.0` | Exact bounds |
| BM-007 | Stumble triggered when BodyMechanicsScore below threshold AND PowerIntent above threshold | `BodyMechanicsScore < STUMBLE_SCORE_THRESHOLD`, `PowerIntent > STUMBLE_POWER_THRESHOLD` | `StumbleTriggered == true`; `ShotExecutedEvent.StumbleTriggered == true` (Agent Movement subscribes to this event — Mechanism C, §4.3.3) | Exact |
| BM-008 | Stumble NOT triggered when PowerIntent is low despite poor body mechanics | `BodyMechanicsScore < STUMBLE_SCORE_THRESHOLD`, `PowerIntent < STUMBLE_POWER_THRESHOLD` | `StumbleTriggered == false` | Exact |

**Notes:**

BM-007 and BM-008 together define the AND gate for stumble triggering. Both conditions
must hold simultaneously. BM-008 is the inverse case: poor body mechanics alone does
not cause a stumble if the shot is not being attempted with high effort. This distinction
prevents low-power tap-in shots from triggering exaggerated stumble animations.

BM-005 acknowledges the physical reality that a stationary or slow-moving agent cannot
generate the same contact quality as an agent running onto the ball. Stationary shots are
valid (penalty kicks model this) but produce a naturally lower BodyMechanicsScore.

---

## 5.9 Unit Tests — Weak Foot Penalty (WF-)

**Owner:** §3.8 — Weak Foot Penalty (WeakFootPenaltyApplier.cs)
**Target count:** 6 tests
**Purpose:** Verify the weak foot error cone multiplier and velocity reduction formula
across all five WeakFootRating levels. Formula is direct reuse from Pass Mechanics §3.7;
these tests confirm the reuse is correct and the Shot Mechanics pipeline correctly
threads the multiplier into both error and velocity paths.

| ID | Test Name | Inputs | Expected Result | Tolerance |
|----|-----------|--------|-----------------|-----------|
| WF-001 | IsWeakFoot=false: no penalty applied | `IsWeakFoot=false`, any attributes | `WeakFootMultiplier == 1.0`; velocity unchanged from strong-foot baseline | ± 0.001 |
| WF-002 | WeakFootRating=5 (ambidextrous): minimal penalty | `IsWeakFoot=true`, `WeakFootRating=5` | Error multiplier ≈ 1.0 + `(5-5) × WEAK_FOOT_ERROR_SCALE` = 1.0 (no penalty) | ± 0.001 |
| WF-003 | WeakFootRating=1 (weakest weak foot): maximum penalty | `IsWeakFoot=true`, `WeakFootRating=1` | Error multiplier == 1 + 4 × `WEAK_FOOT_ERROR_SCALE`; velocity reduced by `WEAK_FOOT_VEL_SCALE_MIN` | ± 0.01 |
| WF-004 | Penalty increases monotonically as WeakFootRating decreases | `WeakFootRating ∈ {5, 4, 3, 2, 1}`, `IsWeakFoot=true` | Error multiplier strictly increases at each step | Strict monotone |
| WF-005 | Velocity reduction is also applied when IsWeakFoot=true | `IsWeakFoot=true`, `WeakFootRating=2`, baseline velocity known | `V_WeakFoot < V_StrongFoot` | Strict inequality |
| WF-006 | WeakFoot flag is read from ShotRequest, not independently derived | `ShotRequest.IsWeakFoot=true`, correct dominant foot configured | `IsWeakFoot=true` in pipeline (Shot Mechanics does not override caller-supplied value) | Code review + exact |

**Notes:**

WF-002 is a design contract test: WeakFootRating=5 means the player is effectively
ambidextrous. The formula `1 + (5 - Rating) × SCALE` evaluates to exactly 1.0 — the
weak foot multiplier must be neutral (no penalty) at this rating.

WF-006 is a design contract test, not a formula test. It verifies that Shot Mechanics
reads `ShotRequest.IsWeakFoot` as authoritative and does not independently determine
which foot the agent is using. This should be verified by code review and static analysis
as well as by runtime assertion. Flag as "manual review required" in the test plan.

---

## 5.10 Unit Tests — State Machine (SSM-)

**Owner:** §3.9 — Shot State Machine (ShotStateMachine.cs)
**Target count:** 8 tests
**Purpose:** Verify all state transitions including the cancel path, stumble path,
and double-submission rejection. The state machine is the orchestrator of the full
pipeline; its correctness is a prerequisite for integration test validity.

States: `IDLE → INITIATING → WINDUP → CONTACT → FOLLOW_THROUGH → COMPLETE`
Cancel path: `WINDUP → CANCELLED → IDLE`

| ID | Test Name | Inputs / Setup | Expected Result | Tolerance |
|----|-----------|----------------|-----------------|-----------|
| SSM-001 | Valid ShotRequest transitions IDLE → INITIATING → WINDUP | Valid `ShotRequest` submitted in IDLE | State == WINDUP after validation pass | Exact |
| SSM-002 | Invalid ShotRequest leaves state at IDLE | `ShotRequest` with `PowerIntent=1.5` | State remains IDLE; `ShotResult.Outcome == Invalid` | Exact |
| SSM-003 | WINDUP timer completes: WINDUP → CONTACT | Advance frame counter past WindupDuration | State == CONTACT; `Ball.ApplyKick()` called exactly once | Exact |
| SSM-004 | CONTACT → FOLLOW_THROUGH → COMPLETE → IDLE (automatic) | Valid request reaches CONTACT | All four transitions occur without external trigger; final state == IDLE | Exact |
| SSM-005 | Tackle interrupt during WINDUP → CANCELLED | Collision System tackle interrupt flag set during WINDUP | State → CANCELLED; `ShotCancelledEvent` published; `Ball.ApplyKick()` NOT called | Exact |
| SSM-006 | Tackle interrupt during CONTACT: shot completes (not cancelled) | Collision System tackle interrupt flag set during CONTACT | State → FOLLOW_THROUGH normally; `Ball.ApplyKick()` called; tackle interrupt ignored | Exact |
| SSM-007 | Double submission during non-IDLE state rejected | Second `ShotRequest` submitted while state == WINDUP | Second request returns `Invalid`; first shot execution unaffected; state remains WINDUP | Exact |
| SSM-008 | Ball.ApplyKick() called exactly once per completed shot | Valid shot executed to COMPLETE | `Ball.ApplyKick()` spy count == 1 | Exact count |

**Notes:**

SSM-008 is critical safety-level. Calling `Ball.ApplyKick()` twice in a single shot
execution would apply double impulse, launching the ball at up to 70 m/s — well above
the V_ABSOLUTE_MAX clamp, potentially causing Ball Physics divergence. This must be
verified with a mock/spy on `Ball.ApplyKick()` that counts invocations.

SSM-006 defines the tackle interrupt boundary: once CONTACT has begun, the kick is
committed. A tackle that lands simultaneously with foot-to-ball contact does not
retroactively cancel the shot — the ball was already kicked. This is the same policy
as Pass Mechanics §3.8.

SSM-005 tests both state transition AND event publication in a single scenario. A
`ShotCancelledEvent` without the state transitioning to IDLE is a partial failure;
both must be verified.

---

## 5.11 Unit Tests — Edge Cases and Robustness (EC-)

**Owner:** §3.1–§3.10 (cross-cutting)
**Target count:** 8 tests
**Purpose:** Verify system stability under degenerate inputs, attribute boundary
extremes, and numerical edge conditions. None of these inputs should produce NaN,
Infinity, divide-by-zero, or an unhandled exception. The system must return a valid
(if degraded) `ShotResult` or a clean `Invalid` outcome in every case.

| ID | Test Name | Inputs | Expected Result | Tolerance |
|----|-----------|--------|-----------------|-----------|
| EC-001 | All attributes at minimum (1): valid result produced | All `PlayerAttributes = 1`, valid `ShotRequest` ranges | Valid `ShotResult` with V ≥ V_ABSOLUTE_MIN; no exception; no NaN | Exact |
| EC-002 | All attributes at maximum (20): valid result produced | All `PlayerAttributes = 20`, valid `ShotRequest` ranges | Valid `ShotResult` with V ≤ V_ABSOLUTE_MAX; no exception; no NaN | Exact |
| EC-003 | PowerIntent exactly 0.0: valid result, V_ABSOLUTE_MIN floor applied | `PowerIntent = 0.0` | `V ≥ V_ABSOLUTE_MIN (8.0 m/s)`; log warning issued; no exception | Exact floor |
| EC-004 | PowerIntent exactly 1.0 (boundary): valid result | `PowerIntent = 1.0` | Valid `ShotResult`; no exception; velocity within [8.0, 35.0] | Exact |
| EC-005 | SpinIntent exactly 1.0 with Centre ContactZone: valid result | `SpinIntent = 1.0`, `ContactZone = Centre` | Valid `ShotResult`; heavy topspin; velocity reduced by spin trade-off; no exception | Exact |
| EC-006 | DistanceToGoal = 0.0: clamped to 1.0m minimum; log warning | `DistanceToGoal = 0.0` | `DistanceToGoal` clamped to 1.0m for sigmoid calculation; log warning issued; no exception | Exact |
| EC-007 | ShotRequest submitted while state machine in WINDUP: clean rejection | Valid second request during WINDUP | Returns `ShotOutcome.Invalid`; first shot execution unaffected; no exception | Exact |
| EC-008 | NaN in computed velocity: recovery clamp applied | Mock VelocityCalculator returns NaN | FM-05 recovery: velocity clamped to safe fallback; critical error logged; no crash | Exact recovery |

**Notes:**

EC-008 is a defensive test for the NaN guard described in §2.7 (FM-05). In production,
NaN should never appear in the velocity pipeline — it would indicate a formula
implementation bug. The test verifies that if it does occur, the system recovers
gracefully rather than propagating NaN to Ball Physics where it would cause simulation
divergence. The "safe fallback" is specified in §3.2 FM-05 recovery path.

**EC-008 Implementation:** Resolved by Section 4 Amendment 1B. `IShotVelocityCalculator`
interface added; `ShotVelocityCalculator` refactored to singleton implementing it;
`ShotExecutor` receives it via constructor injection (default: `ShotVelocityCalculator.Instance`
— zero change to production behaviour). `NaNVelocityStub.cs` added to `/Tests/` folder
for exclusive use by this test. See Amendment 1B for full implementation and the
reference EC-008 test body. `IShotVelocityCalculator` is the **only** injectable
interface added to Shot Mechanics calculators at Stage 0.

EC-003 and EC-006 both test degenerate inputs that the Decision Tree should prevent but
which can appear from serialised/replayed match data. Log warnings are required (not
just silently corrected) so that debugging sessions can identify malformed Decision Tree
output.

---

