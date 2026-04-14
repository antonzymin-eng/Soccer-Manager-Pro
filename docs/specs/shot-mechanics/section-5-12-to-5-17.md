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
