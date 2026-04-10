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

## 5.12 Integration Tests (IT-)

**Owner:** All — cross-system boundary verification
**Target count:** 12 tests
**Purpose:** Verify that correct outputs flow across the interfaces defined in Section 4.
Integration tests require simulated or mocked instances of Ball Physics, Agent Movement,
Collision System, and Event System stubs.

### IT-001 — Ball Physics Interface: ApplyKick Called with Valid Arguments

**Scope:** ShotExecutor → Ball.ApplyKick()
**Setup:** Valid `ShotRequest`, controlled `PlayerAttributes`, mock `Ball` with call recorder.

**Action:** Execute full shot pipeline to CONTACT state.

**Verify:**
- `Ball.ApplyKick()` called exactly once
- `velocity` argument: `|velocity| ∈ [8.0, 35.0] m/s`
- `spin` argument: `|spin| ≤ MAX_SPIN_MAGNUS_BOUND`
- `agentId` argument: matches `ShotRequest.AgentId`
- `matchTime` argument: matches `ShotRequest.MatchTime`
- `logger` argument: non-null (production `BallEventLogger` instance passed — not null)

**Failure:** Any argument outside these bounds indicates a pipeline assembly bug,
not a formula bug. Formula correctness is verified by unit tests; this test
catches incorrect argument wiring.

---

### IT-002 — Agent Movement Interface: AgentPhysicalProperties Frozen at INITIATING

**Scope:** ShotExecutor ↔ AgentPhysicalProperties (Agent Movement §3.5.4)
**Setup:** Controllable `AgentPhysicalState` mock that allows mid-execution changes.

**Action:** Submit `ShotRequest`. While in WINDUP state, mutate the mock's
`AgentPhysicalProperties.RunUpAngle` to a different value.

**Verify:**
- `BodyMechanicsScore` computed during CONTACT matches the value captured at INITIATING
- `BodyMechanicsScore` does NOT reflect the mid-execution mutation

**Failure:** A non-deterministic body mechanics score that changes based on mid-shot
agent state would cause replay divergence for shots taken during rapid direction changes.

---

### IT-003 — Collision System Interface: Tackle Interrupt During WINDUP Cancels Shot

**Scope:** ShotStateMachine ↔ Collision System tackle interrupt flag
**Setup:** Mock Collision System implementing `GetAndClearTackleFlag(agentId)`. Mock
returns `true` once when the flag is set, then `false` on subsequent calls (atomic
read-and-clear semantics matching the approved API).

**Action:** Execute shot to WINDUP. Set mock's internal flag to `true`. Advance one frame.

**Verify:**
- State == CANCELLED (or IDLE if transition completes in same frame)
- `Ball.ApplyKick()` NOT called (zero invocations)
- `ShotCancelledEvent` published with correct `AgentId` and `CancelFrame`

---

### IT-004 — Collision System Interface: Tackle Interrupt During CONTACT Does Not Cancel

**Scope:** ShotStateMachine ↔ Collision System tackle interrupt flag
**Setup:** Mock Collision System implementing `GetAndClearTackleFlag(agentId)`. Mock's
internal flag is set to `true` before CONTACT entry — simulating a tackle that registered
during the final WINDUP frame but is polled only at CONTACT.

**Action:** Execute shot through CONTACT state with tackle flag already active.

**Verify:**
- `Ball.ApplyKick()` called exactly once (shot completes)
- `ShotExecutedEvent` published (not `ShotCancelledEvent`)
- State progresses to FOLLOW_THROUGH normally

---

### IT-005 — Event System Interface: ShotExecutedEvent Published with Complete Payload

**Scope:** ShotEventEmitter → EventBusStub
**Setup:** Valid shot, EventBus spy that captures published events.

**Action:** Execute full shot pipeline to COMPLETE.

**Verify:**
- `ShotExecutedEvent` published exactly once
- `ShootingAgentId` matches `ShotRequest.AgentId`
- `KickVelocity.magnitude ∈ [8.0, 35.0] m/s`
- `\|KickSpin\| ≤ MAX_SPIN_MAGNUS_BOUND`
- `IntendedTarget ∈ [0,1]²` (goal-relative coordinates in range)
- `BodyMechanicsScore ∈ [0.0, 1.0]`
- `StumbleTriggered` matches output of BodyMechanicsEvaluator from same execution

**Failure mode:** Missing fields or out-of-range values indicate the event struct is
being populated before the pipeline completes, capturing intermediate rather than
final values.

---

### IT-006 — Event System Interface: ShotCancelledEvent Published on Tackle Interrupt, Not ShotExecutedEvent

**Scope:** ShotEventEmitter → EventBusStub
**Setup:** Mock Collision System set to trigger tackle interrupt during WINDUP; EventBus spy.

**Action:** Execute shot; trigger tackle interrupt during WINDUP.

**Verify:**
- `ShotCancelledEvent` published exactly once
- `ShotExecutedEvent` NOT published (zero `ShotExecutedEvent` publications)
- `ShotCancelledEvent.Reason == ShotCancelReason.TackleInterrupt`

---

### IT-007 — End-to-End: Top-Corner Placement Produces Correct Aim Direction

**Scope:** Full pipeline: ShotRequest → PlacementResolver → ErrorCalculator → Ball.ApplyKick()
**Setup:** Known agent position, goal geometry confirmed; `PlacementTarget = (0.9, 0.9)` (top-right);
error minimised (`Finishing=20`, `Composure=20`, `PowerIntent=0.5`, `Pressure=0.0`, `Fatigue=0.0`,
`BodyMechanicsScore=0.95`).

**Action:** Execute full pipeline.

**Verify:**
- `ShotExecutedEvent.FinalDirection` points toward top-right region of goal mouth
- Expected world-space target ≈ (goal_right_x - 0.3m, crossbar_y - 0.2m, goal_line_z)
- Actual aim direction when extended to goal line falls within ± 0.5m of expected

**Failure:** Placement axis inversion or goal geometry error.

---

### IT-008 — End-to-End: Chip Shot Profile Produces Elevated Launch Angle

**Scope:** Full pipeline: ShotRequest with BelowCentre contact and high SpinIntent
**Setup:** `ContactZone=BelowCentre`, `SpinIntent=0.8`, `PowerIntent=0.4`, mid-range attributes.

**Action:** Execute full pipeline.

**Verify:**
- `LaunchAngleDeg` from `ShotResult` ≥ 20° (chip trajectory)
- `FinalVelocity.y > 0` (ball initially ascending)
- Spin vector has negative Y component (backspin confirmed from SN-002)

---

### IT-009 — End-to-End: Driven Shot Profile Produces Low Launch Angle and High Speed

**Scope:** Full pipeline: ShotRequest with Centre contact and high PowerIntent
**Setup:** `ContactZone=Centre`, `PowerIntent=0.9`, `SpinIntent=0.0`, `Finishing=16`, `KickPower=16`.

**Action:** Execute full pipeline.

**Verify:**
- `LaunchAngleDeg` from `ShotResult` ≤ 8°
- `KickSpeed` from `ShotResult` ≥ 22 m/s
- Spin vector has positive Y component (topspin confirmed from SN-001)

---

### IT-010 — End-to-End: Weak Foot Shot Produces Higher Error and Lower Speed Than Strong Foot Equivalent

**Scope:** Full pipeline: identical requests differing only in `IsWeakFoot`
**Setup:** Same `ShotRequest` twice; `IsWeakFoot=true` then `IsWeakFoot=false`. `WeakFootRating=2`.
Use fixed hash seed to control error direction; compare magnitudes.

**Action:** Execute both variants; record `ShotResult`.

**Verify:**
- `ErrorOffset` magnitude larger for weak foot variant
- `KickSpeed` lower for weak foot variant
- All other `ShotResult` fields monotonically worsened (not improved) by weak foot flag

---

### IT-011 — ShotAnimationData Stub: Populated on Completion, Not Published

**Scope:** ShotEventEmitter → ShotAnimationData stub
**Setup:** Valid shot; EventBus spy configured to detect any `ShotAnimationData` publication.

**Action:** Execute full pipeline.

**Verify:**
- `ShotResult.AnimationData` is populated (not default struct)
- `ShotResult.AnimationData.ContactZone` matches `ShotRequest.ContactZone`
- `ShotResult.AnimationData.IsWeakFoot` matches `ShotRequest.IsWeakFoot`
- `ShotAnimationData` is NOT published to EventBus (Stage 0 stub behaviour)

---

### IT-012 — CRITICAL: Determinism Regression Test

**Scope:** Full pipeline × 3 executions with identical inputs on same platform
**Setup:** Canonical test fixture from `ShotTestFixtures.cs` (committed, immutable).
`matchSeed=12345`, `agentId=7`, `frameNumber=3600`, mid-range attributes, `ContactZone=OffCentre`,
`SpinIntent=0.6`, `PlacementTarget=(0.3, 0.7)`.

**Action:** Execute full shot pipeline three times with identical inputs. No state
shared between executions. Capture full `ShotResult` struct each time.

**Verify:**
- All three `ShotResult` structs are bitwise-identical
- `FinalVelocity`, `FinalSpin`, `ErrorOffset`, `BodyMechanicsScore`, `LaunchAngleDeg`,
  `KickSpeed` — all fields identical

**Failure policy:** IT-012 failure is a CRITICAL HALT. Do not proceed with any further
development until root cause is identified. Determinism failure causes replay
divergence across all 90-minute simulations and invalidates the entire match record.

**Diagnostic hints:** Floating-point operations with non-deterministic ordering (e.g.,
parallel summation); unintentional mutable state on static calculator classes;
platform-specific `Mathf` vs `System.Math` rounding differences.

---

## 5.13 Real-World Validation Scenarios (VS-)

**Purpose:** Verify that the complete pipeline produces football-realistic outputs for
canonical player archetypes. These scenarios serve as the ground-truth regression suite
for all future formula and constant tuning. Each scenario must be reviewable by a
football-knowledgeable person and produce an output that makes intuitive sense.

All expected outputs are **pre-Appendix-B estimates** `[EST]` derived from
`ShotTestFixtures.cs` pre-calculations and StatsBomb velocity benchmarks. Numeric
ranges are subject to revision after Appendix B numerical verification against the Ball
Physics drag model — the same methodology used in Pass Mechanics §5.12. However, the
football realism narrative check is non-negotiable and is not subject to formula
revision: the described outcome must remain physically and tactically plausible regardless
of constant tuning.

---

### VS-001 — Elite Striker, Close Range, Driven Shot to Bottom Corner

**Archetype:** 25-yard-box centre-forward, first-time driven shot, strong foot, good run.

| Parameter | Value |
|-----------|-------|
| `Finishing` | 19 |
| `LongShots` | 13 |
| `KickPower` | 17 |
| `Composure` | 18 |
| `Technique` | 15 |
| `WeakFootRating` | 4 |
| `DistanceToGoal` | 14m |
| `ContactZone` | Centre |
| `PowerIntent` | 0.85 |
| `SpinIntent` | 0.1 |
| `PlacementTarget` | (0.1, 0.1) (bottom-left corner) |
| `IsWeakFoot` | false |
| `Pressure` | 0.25 |
| `Fatigue` | 0.2 |
| `BodyMechanicsScore` | 0.85 |

**Expected outputs:**

| Output | Expected | Tolerance |
|--------|----------|-----------|
| `KickSpeed` | 24–30 m/s | ± 2 m/s |
| `LaunchAngleDeg` | 2–7° | ± 1° |
| `ErrorAngle` | 1.5–3.0° | ± 0.5° |
| Spin type | Topspin dominant | Sign check (Spin.y > 0) |
| `StumbleTriggered` | false | Exact |

**Football realism check:** 14m driven shot at 24–30 m/s travels to the goal line in
~0.5–0.6s. Error of 1.5–3.0° at 14m ≈ 0.37–0.73m lateral miss. Bottom-left corner
target with this error window: shot likely on target but not guaranteed to beat a
well-positioned goalkeeper. Plausible for a good but not trivial scoring chance.

---

### VS-002 — Long-Range Specialist, 28m Power Shot

**Archetype:** Attacking midfielder, long-range attempt from outside the box, strong foot.

| Parameter | Value |
|-----------|-------|
| `Finishing` | 12 |
| `LongShots` | 18 |
| `KickPower` | 16 |
| `Composure` | 14 |
| `Technique` | 13 |
| `WeakFootRating` | 3 |
| `DistanceToGoal` | 28m |
| `ContactZone` | Centre |
| `PowerIntent` | 0.95 |
| `SpinIntent` | 0.05 |
| `PlacementTarget` | (0.75, 0.35) (right side, mid-height) |
| `IsWeakFoot` | false |
| `Pressure` | 0.35 |
| `Fatigue` | 0.3 |
| `BodyMechanicsScore` | 0.75 |

**Expected outputs:**

| Output | Expected | Tolerance |
|--------|----------|-----------|
| `KickSpeed` | 26–33 m/s | ± 2 m/s |
| `LaunchAngleDeg` | 3–9° | ± 1° |
| `ErrorAngle` | 2.5–4.5° | ± 0.7° |
| Sigmoid blend | LongShots dominant (28m > threshold) | Assert: closer to LongShots prediction |
| `StumbleTriggered` | false | Exact |

**Football realism check:** At 28m the LongShots attribute dominates (OI-001 resolution).
Power penalty from `PowerIntent=0.95` adds ~1.5–2.0° to error. Result: a powerful but
imprecise shot — a realistic long-range speculative effort that a goalkeeper should hold
comfortably unless struck exceptionally cleanly.

---

### VS-003 — Penalty-Area Chip Over Advancing Goalkeeper

**Archetype:** Forward one-on-one with advancing GK at 8m, chip attempt, moderate composure.

| Parameter | Value |
|-----------|-------|
| `Finishing` | 14 |
| `LongShots` | 8 |
| `KickPower` | 13 |
| `Composure` | 13 |
| `Technique` | 14 |
| `WeakFootRating` | 3 |
| `DistanceToGoal` | 8m |
| `ContactZone` | BelowCentre |
| `PowerIntent` | 0.45 |
| `SpinIntent` | 0.75 |
| `PlacementTarget` | (0.5, 0.85) (centre, just under crossbar) |
| `IsWeakFoot` | false |
| `Pressure` | 0.5 (GK closing distance) |
| `Fatigue` | 0.25 |
| `BodyMechanicsScore` | 0.6 |

**Expected outputs:**

| Output | Expected | Tolerance |
|--------|----------|-----------|
| `KickSpeed` | 10–16 m/s | ± 1.5 m/s |
| `LaunchAngleDeg` | 22–38° | ± 3° |
| `ErrorAngle` | 2.5–5.0° | ± 0.8° |
| Spin type | Backspin dominant | Sign check (Spin.y < 0) |
| `StumbleTriggered` | false | Exact |

**Football realism check:** BelowCentre contact at SpinIntent=0.75 produces characteristic
chip arc. 22–38° launch at 10–16 m/s clears an advancing GK at typical leap height.
Error of 2.5–5.0° at 8m ≈ 0.35–0.70m miss — chip is technically demanding, a high error
range is expected. BodyMechanicsScore=0.6 reflects the awkward contact angle under
GK pressure. Plausible but risky.

---

### VS-004 — Weak Foot Curling Shot to Far Post

**Archetype:** Right-footed winger shooting with left foot, cutting inside, curl to far post.

| Parameter | Value |
|-----------|-------|
| `Finishing` | 15 |
| `LongShots` | 10 |
| `KickPower` | 14 |
| `Composure` | 12 |
| `Technique` | 16 |
| `WeakFootRating` | 2 |
| `DistanceToGoal` | 18m |
| `ContactZone` | OffCentre |
| `PowerIntent` | 0.65 |
| `SpinIntent` | 0.8 |
| `PlacementTarget` | (0.85, 0.4) (far post, mid-height) |
| `IsWeakFoot` | true |
| `Pressure` | 0.3 |
| `Fatigue` | 0.35 |
| `BodyMechanicsScore` | 0.7 |

**Expected outputs:**

| Output | Expected | Tolerance |
|--------|----------|-----------|
| `KickSpeed` | 14–20 m/s | ± 1.5 m/s |
| `LaunchAngleDeg` | 6–15° | ± 2° |
| `ErrorAngle` | 4.0–7.0° | ± 1.0° |
| Spin type | Sidespin dominant | Sign check (\|Spin.z\| > \|Spin.y\|) |
| `StumbleTriggered` | false | Exact |

**Football realism check:** `WeakFootRating=2` applies significant error penalty.
Error of 4.0–7.0° at 18m ≈ 1.26–2.20m lateral miss — a curl to the far post that
requires significant margin. Sidespin from OffCentre contact produces visible curl
in flight. The high error range is realistic: weak-foot shots to the far post are
high-risk, low-accuracy attempts. A miss to the side or over is the expected modal outcome.

---

### VS-005 — Set Piece Specialist, Free Kick Simulation (OffCentre, High Spin)

**Archetype:** Dedicated free kick taker, ideal run-up, no direct pressure, maximum technique.

| Parameter | Value |
|-----------|-------|
| `Finishing` | 16 |
| `LongShots` | 17 |
| `KickPower` | 15 |
| `Composure` | 18 |
| `Technique` | 19 |
| `WeakFootRating` | 4 |
| `DistanceToGoal` | 22m |
| `ContactZone` | OffCentre |
| `PowerIntent` | 0.70 |
| `SpinIntent` | 0.9 |
| `PlacementTarget` | (0.15, 0.9) (top-left corner) |
| `IsWeakFoot` | false |
| `Pressure` | 0.0 (no direct press — static free kick situation) |
| `Fatigue` | 0.1 |
| `BodyMechanicsScore` | 0.92 (ideal run-up) |

**Expected outputs:**

| Output | Expected | Tolerance |
|--------|----------|-----------|
| `KickSpeed` | 18–26 m/s | ± 2 m/s |
| `LaunchAngleDeg` | 8–18° | ± 2° |
| `ErrorAngle` | 0.8–2.5° | ± 0.5° |
| Spin type | Sidespin dominant | Sign check |
| `StumbleTriggered` | false | Exact |

**Football realism check:** Technique=19 with zero pressure produces a low error angle.
0.8–2.5° at 22m ≈ 0.31–0.96m lateral miss. A top-left corner target with this error
window means the shot is likely on frame — a genuinely dangerous free kick that requires
a save. SpinIntent=0.9 with OffCentre produces significant sidespin: the ball curves,
making the trajectory less predictable for the GK. Plausible for an elite set piece taker.

---

### VS-006 — Minimum Competence Shot (Youth-Level Archetype)

**Archetype:** Low-attribute player, point-blank range, all attributes minimum, basic shot.

| Parameter | Value |
|-----------|-------|
| `Finishing` | 2 |
| `LongShots` | 2 |
| `KickPower` | 3 |
| `Composure` | 3 |
| `Technique` | 3 |
| `WeakFootRating` | 3 |
| `DistanceToGoal` | 7m |
| `ContactZone` | Centre |
| `PowerIntent` | 0.5 |
| `SpinIntent` | 0.0 |
| `PlacementTarget` | (0.5, 0.5) (centre goal) |
| `IsWeakFoot` | false |
| `Pressure` | 0.1 |
| `Fatigue` | 0.05 |
| `BodyMechanicsScore` | 0.5 |

**Expected outputs:**

| Output | Expected | Tolerance |
|--------|----------|-----------|
| `KickSpeed` | ≥ V_ABSOLUTE_MIN (8.0 m/s) | Exact ≥ |
| `LaunchAngleDeg` | 2–12° | ± 3° |
| `ErrorAngle` | 4.0–8.0° | ± 1.5° |
| `ShotResult.Outcome` | `ShotOutcome.Completed` | Exact |
| No NaN or Infinity in any `ShotResult` field | All fields | Exact |

**Football realism check:** At 7m with 4.0–8.0° error ≈ 0.49–0.98m lateral miss at
the goal line. A minimum-attribute player has a reasonable chance of scoring from 7m
even with poor technique — which is correct. The system must never produce a velocity
below V_ABSOLUTE_MIN; even the weakest shot is still a proper kick, not a dribble. The
critical verification here is the absence of NaN, the output clamp, and a valid Completed
outcome — this test validates that degenerate attribute values do not break the pipeline.

---

## 5.14 Acceptance Criteria Summary

All criteria below must be satisfied before Section 5 (and the full specification) can
be marked as passing the quality gate. No partial passes. A single failing critical
criterion blocks specification approval.

### 5.14.1 Unit Test Acceptance

| Criterion | Required | Blocking |
|-----------|----------|---------|
| All PV- tests pass | 8/8 | Yes |
| All SV- tests pass | 12/12 | Yes |
| All LA- tests pass | 8/8 | Yes |
| All SN- tests pass | 8/8 | Yes |
| All SP- tests pass | 10/10 | Yes |
| All SE- tests pass | 10/10 | Yes |
| All BM- tests pass | 8/8 | Yes |
| All WF- tests pass | 6/6 | Yes |
| All SSM- tests pass | 8/8 | Yes |
| All EC- tests pass | 8/8 | Yes |

### 5.14.2 Integration Test Acceptance

| Criterion | Required | Blocking |
|-----------|----------|---------|
| IT-001 through IT-011 pass | 11/11 | Yes |
| IT-012 determinism test passes | 1/1 | Yes — **CRITICAL HALT if fails** |

### 5.14.3 Validation Scenario Acceptance

| Criterion | Required | Blocking |
|-----------|----------|---------|
| All VS- expected outputs within tolerance | 6/6 | Yes |
| No VS- output produces NaN or Infinity in any field | 6/6 | Yes |
| Football realism judgement: Lead Developer sign-off | Pass | Yes |

### 5.14.4 Coverage and Performance Acceptance

| Criterion | Target | Blocking |
|-----------|--------|---------|
| Line coverage on §3.x code | > 85% | Yes |
| `ExecuteShot()` p99 ≤ 0.05ms | Profiler result | Yes |
| Full unit suite runtime | < 15 seconds | No (advisory) |

---

## 5.15 Test Execution Plan

### 5.15.1 Test Ordering

Tests must execute in the following order to enable efficient failure diagnosis. A
failure in an earlier phase does not block execution of later tests for diagnostic
purposes, but it is blocking for sign-off. Fix failures in order.

```
Phase 1: EC- (edge cases / robustness)         — validates defensive coding first
Phase 2: PV- (parameter validation)             — validates pipeline entry gate
Phase 3: SV- (velocity model)                   — primary output calculation
Phase 4: LA- (launch angle)                     — depends on correct ContactZone handling
Phase 5: SN- (spin vector)                      — depends on correct SpinIntent handling
Phase 6: SP- (placement resolution)             — isolated sub-system
Phase 7: SE- (error model)                      — isolated sub-system
Phase 8: BM- (body mechanics)                   — isolated sub-system
Phase 9: WF- (weak foot)                        — isolated sub-system
Phase 10: SSM- (state machine)                  — depends on all §3.x functions
Phase 11: IT- (integration)                     — all unit tests must pass first
Phase 12: VS- (validation scenarios)            — full pipeline must pass first
```

### 5.15.2 Execution Cadence

| Cadence | Tests Run | Trigger |
|---------|-----------|---------|
| On every code change | All unit tests (EC- through SSM-) | Developer — local, pre-commit |
| On pull request | All unit + all IT- tests | CI pipeline |
| On milestone | Full suite including VS- | Manual milestone gate |
| Nightly | IT-012 determinism + performance profiling | Scheduled CI job |
| After any constant tuning | Full unit suite | Manual — document baseline SHA |

### 5.15.3 Failure Response Protocol

**Level 1 — Formula error (SV-, LA-, SN-, SE-):** HALT. Fix before any integration work.
These failures contaminate all downstream tests and invalidate validation scenarios.
In particular: SV- failures invalidate IT-001, IT-007, IT-008, IT-009, and all VS- outputs.

**Level 2 — Validation or placement error (PV-, SP-):** HALT. These are gateway
functions. PV- errors mean invalid requests reach the physics pipeline. SP- errors
mean shots are aimed at incorrect goal regions.

**Level 3 — State machine error (SSM-):** Fix before integration testing.
SSM-008 (double ApplyKick) is Level 1 severity regardless of category.

**Level 4 — Integration failure (IT-):** May run non-dependent IT- tests for diagnosis.
Must resolve before milestone gate. IT-012 determinism failure escalates immediately to
Level 1 CRITICAL HALT.

**Level 5 — Validation scenario deviation (VS-):**
- Within 2× tolerance: investigate formula calibration; likely a constant tuning issue.
- Within 10× tolerance: investigate player archetype attribute values in test fixtures.
- Exceeds 10× tolerance: treat as Level 1 formula error; escalate immediately.

### 5.15.4 Regression Strategy

All 86 unit tests constitute the regression suite. Before any constant or formula tuning:

1. Record baseline metrics (commit SHA, test results, VS- outputs).
2. Make tuning changes.
3. Run full unit suite — confirm no regressions.
4. Re-run VS- scenarios — document output delta.
5. Record new baseline with tuning rationale in `ShotConstants.cs` comments.

Never tune a constant without running the full suite afterward. Constants are not
isolated; changing `BASE_ERROR` affects SE-001, SE-002, SE-010, VS-001, and VS-006
simultaneously.

---

## 5.16 Section Summary

Section 5 defines **104 tests** across 12 categories covering the full Shot Mechanics
system:

- **86 unit tests** verify individual formula correctness, boundary conditions, and edge
  case handling in isolation. Every formula term, every constant, and every branch
  condition in §3.1–§3.10 has at least one corresponding unit test.
- **12 integration tests** verify correct cross-system data flow across all five interface
  contracts defined in Section 4, including the CRITICAL IT-012 determinism regression test.
- **6 validation scenarios** verify that the complete pipeline produces football-realistic
  outputs for canonical player archetypes, serving as the ground-truth regression suite
  for all future formula and constant tuning.

**No open blockers.** All attribute names and interface contracts are confirmed stable.
All 104 tests are executable upon implementation completion of §3.1–§3.10.

---

## 5.17 Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | February 23, 2026, 11:59 PM PST | Claude (AI) / Anton | Initial draft. 104 tests across 12 categories. No open blockers. All attribute names confirmed from Agent Movement §3.5.6 v1.3. IT-012 designated CRITICAL HALT. Six validation scenarios with football realism narrative checks. |
| 1.1 | February 23, 2026 | Claude (AI) / Anton | Five fixes from self-critique: (1) LA-003 weakened from three-way ordering to two-point assertion. (2) SP-009 flagged with ⚠ DI seam requirement. (3) VS- intro updated with `[EST]` caveat. (4) IT-002 mutable mock documented in tooling. (5) EC-008 flagged with ⚠ injectable seam requirement. |
| 1.2 | February 23, 2026 | Claude (AI) / Anton | Residual risks resolved. SP-009 ⚠ cleared: GoalGeometryProvider seam confirmed via §4 Amendment 1A — test now implementable with zero production cost. EC-008 ⚠ cleared: IShotVelocityCalculator + ShotExecutor constructor injection confirmed via §4 Amendment 1B — NaNVelocityStub pattern documented. IT-002 mutable mock entry updated (removed "only" qualifier). Tooling table updated to reflect all three seams as confirmed. No test logic changes. |
| 1.3 | February 23, 2026 | Claude (AI) / Anton | Section 4 v1.3 audit corrections propagated: (1) Prerequisites header updated to Section 4 v1.3. (2) PV-008 possession check wording updated — no `BallState.PossessingAgentId`; mock setup now uses `AgentSystem.GetPossessor()` returning wrong agent (ERR-008 Option B). (3) BM-007 stumble outcome updated — "emitted to Agent Movement hysteresis" replaced with `ShotExecutedEvent.StumbleTriggered == true` reference (Mechanism C, §4.3.3). (4) IT-001 verify block: `logger` argument added — must be non-null in production test setup. (5) IT-003 and IT-004 mock descriptions updated — `TackleInterruptActive` flag name replaced with `GetAndClearTackleFlag(agentId)` atomic mock pattern. No test count changes; 104 tests unchanged. |
| 1.4 | March 6, 2026 | Claude (AI) / Anton | Comprehensive audit corrections: (1) §5.15.4 and §5.16 unit test count corrected 90→86 to match §5.1.3 authoritative table. (2) Decision Tree #7→#8, Goalkeeper Mechanics #10→#11. |

---

*End of Section 5 — Shot Mechanics Specification #6*

*Tactical Director — Specification #6 of 20 | Stage 0: Physics Foundation*

*Next: Section 6 — Performance Analysis*
