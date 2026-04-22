# Perception System Specification #7 — Section 5: Testing

**File:** `Perception_System_Spec_Section_5_v1_3.md`
**Purpose:** Defines all test cases, acceptance criteria, coverage requirements,
determinism validation, and performance benchmarks for the Perception System.
Every functional requirement in Section 2 maps to at least one test case in this
section. All expected values are derived from Section 3 formulas — no magic numbers.

**Created:** February 26, 2026, 12:00 PM PST
**Version:** 1.4
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 7 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)

**Prerequisite Sections:** Section 1 v1.1, Section 2 v1.2, Section 3 v1.3, Section 4 v1.1

**Version History:**

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | February 26, 2026, 12:00 PM PST | Initial draft |
| 1.1 | February 26, 2026, 12:30 PM PST | Four fixes: (1) Performance targets corrected to match outline §6.1 authority: 2ms total/22 agents, ~90µs per agent — previous v1.0 used wrong p95/p99 values. (2) IT-FULL-001 revised to include exact computed values instead of qualitative comparison. (3) FOV-003 tolerance note added: implementer must document angle comparison method (atan2 vs dot product) as it affects exact boundary behaviour. (4) Balance tests added per outline §5.3 (previously absent). |
| 1.2 | February 26, 2026 | Six fixes: (1) §5.2 formula header corrected — was using (Decisions−1)/19 but Section 3 authority uses Decisions/20; phantom constants PRESSURE_FOV_THRESHOLD=0.5 and PRESSURE_FOV_SCALE=40° removed entirely. (2) FOV-001 derivation corrected to D/20 formula (bonus=5.0° at D=10, not 4.737°). (3) FOV-002/003 boundary corrected — D=1 produces 160.5°/80.25° halfangle, not 160°/80°. (4) FOV-004 expected values corrected — difference is 9.5° (not 10°), D=1 produces 160.5° (not 160°). (5) FOV-006 replaced — phantom threshold test removed; replaced with correct continuous-pressure test at PS=0.5 producing EffectiveFoV=150°. (6) FOV-007 derivation corrected — was using wrong formula producing 140°; correct value is 130.5°. (7) FOV-008 expected blind-side arc corrected to 195.0° (was 195.263°). (8) LR-003 tolerance changed from ±1 tick to exact — floor rounding is deterministic per §3.3.2. |
| 1.4 | April 22, 2026 | Struct name corrections following §3 v1.3 rename of `PerceptionSnapshot` to `FilteredView` + `PerceptionDiagnostics`: (1) Prereq header updated to Section 2 v1.2, Section 3 v1.3. (2) `IsForceRefreshed` renamed `ForcedRefreshThisTick` throughout FR-001–FR-010. (3) PS-005 description corrected — removed stale `PRESSURE_FOV_THRESHOLD=0.5` reference; continuous pressure reduction clarified; struct qualifier corrected to `PerceptionDiagnostics`. (4) FOV-008 assertion updated — `BlindSideArcWidth` is not a direct struct field; rewritten to verify via `PerceptionDiagnostics.EffectiveFoVAngle`. (5) All `PerceptionSnapshot.X` test assertions updated to `FilteredView.X` or `PerceptionDiagnostics.X` depending on which struct owns field X. (6) SNAP-007 rewritten — `IsInBlindSide` removed from `PerceivedAgent`; test now verifies `BlindSidePerceivedAgents` vs `VisibleOpponents` array separation. (7) FR-001 label corrected. (8) SNAP-008 and SNAP-010 §3.7 cross-refs updated from §3.7.1 to §3.7.2. |

---

## Table of Contents

- [5.1 Test Strategy](#51-test-strategy)
- [5.2 Unit Tests — Field of View (§3.1)](#52-unit-tests--field-of-view-31)
- [5.3 Unit Tests — Shadow Cone Occlusion (§3.2)](#53-unit-tests--shadow-cone-occlusion-32)
- [5.4 Unit Tests — Recognition Latency (§3.3)](#54-unit-tests--recognition-latency-33)
- [5.5 Unit Tests — Blind-Side Awareness and Shoulder Check (§3.4)](#55-unit-tests--blind-side-awareness-and-shoulder-check-34)
- [5.6 Unit Tests — Ball Perception (§3.5)](#56-unit-tests--ball-perception-35)
- [5.7 Unit Tests — Pressure Scalar (§3.6)](#57-unit-tests--pressure-scalar-36)
- [5.8 Unit Tests — Forced Refresh (§3.8)](#58-unit-tests--forced-refresh-38)
- [5.9 Unit Tests — Snapshot Assembly (§3.7)](#59-unit-tests--snapshot-assembly-37)
- [5.10 Unit Tests — Determinism (§2.4.7)](#510-unit-tests--determinism-247)
- [5.11 Integration Tests](#511-integration-tests)
- [5.12 Balance Tests](#512-balance-tests)
- [5.13 Performance Tests](#513-performance-tests)
- [5.14 Functional Requirement Coverage Matrix](#514-functional-requirement-coverage-matrix)
- [5.15 Tolerance Derivations](#515-tolerance-derivations)
- [5.16 Deferred Validation](#516-deferred-validation)
- [5.17 Section Summary](#517-section-summary)

---

## 5.1 Test Strategy

### 5.1.1 Philosophy

All expected values are computed from the formulas in Section 3. Every test comment
includes the full derivation chain or a cross-reference to the relevant Section 3
subsection. This mirrors the pattern established in Ball Physics §5, Agent Movement
§3.7, and Collision System §5.

The Perception System is a pure-computation module: given identical inputs, it must
produce bit-identical outputs across seeds and platform runs. This property makes
it particularly amenable to unit testing without Unity runtime dependencies.

### 5.1.2 Test Pyramid

| Layer | % of Suite | Target Count | Scope |
|-------|-----------|--------------|-------|
| Unit Tests | ~70% | ≥ 73 | Individual formulas, edge cases, boundary conditions. Edit Mode — no Unity runtime required. |
| Integration Tests | ~25% | ≥ 12 (actual: 15) | Cross-specification data flow; scripted 22-agent scenarios. Play Mode. |
| Balance Tests | ~5% | 3 scenarios | Attribute-scaling validation; no dominant exploitable patterns. |

**E2E tests** (memory stability, NaN propagation over 90-minute simulations) are
classified as integration tests here per the outline's §5.2 grouping. They run in
Play Mode with scripted movement.

### 5.1.3 Global Acceptance Criteria

All of the following must be satisfied before Section 5 is marked PASS:

- All ≥ 73 unit tests pass with zero failures.
- All 15 integration tests pass.
- All 3 balance test scenarios produce expected attribute-scaling behaviour.
- Determinism check: bit-identical output for identical seed across two independent
  simulation runs (100 heartbeats, §5.10 DET-001).
- Performance: full 22-agent heartbeat batch completes within **2ms** (authoritative
  budget per outline §6.1). Per-agent budget: ~90µs.
- Memory: zero GC allocations per heartbeat tick.
- Coverage: > 90% line coverage for §3.1–§3.8 implementation code.
- Every FR in §2.4 maps to at least one test ID in §5.14.

### 5.1.4 Tooling

| Tool | Purpose | Configuration |
|------|---------|---------------|
| Unity Test Framework | Test runner (NUnit-based) | Edit Mode for unit tests; Play Mode for integration |
| Unity Profiler | Performance validation | Custom `PerceptionSystem.ProfilerMarker` over 1,000 ticks |
| Unity Code Coverage | Line/branch coverage | Target: >90% line, >80% branch for §3 code |
| DeterministicTestHarness | Reproducible RNG + match seed | Shared with Ball Physics and Collision System |

### 5.1.5 Test File Mapping (per §4 architecture)

Per the file layout defined in §4.4.1:

| File | Test Group |
|------|-----------|
| `FovCalculatorTests.cs` | FOV-* |
| `OcclusionFilterTests.cs` | OCC-* |
| `RecognitionLatencyTests.cs` | LR-* |
| `ShoulderCheckTests.cs` | SC-* |
| `BallPerceptionTests.cs` | BP-* |
| `SnapshotBuilderTests.cs` | SNAP-* |
| `PerceptionIntegrationTests.cs` | IT-* |
| `PerceptionBalanceTests.cs` | BAL-* |

---

## 5.2 Unit Tests — Field of View (§3.1)

**Constants (§3.10):** `BASE_FOV_ANGLE` = 160°, `MAX_FOV_BONUS_ANGLE` = 10°,
`MAX_FOV_PRESSURE_REDUCTION` = 30°, `MIN_FOV_ANGLE` = 120°.

**Formula (§3.1.2–3.1.4) — authoritative form from Section 3:**
```
Decisions_bonus = (Decisions / 20.0f) × MAX_FOV_BONUS_ANGLE
PressureReduction = PressureScalar × MAX_FOV_PRESSURE_REDUCTION
EffectiveFoV = Max(BASE_FOV_ANGLE + Decisions_bonus − PressureReduction, MIN_FOV_ANGLE)
EffectiveFoV_HalfAngle = EffectiveFoV / 2.0f
```

**Note:** There is no `PRESSURE_FOV_THRESHOLD` constant and no `PRESSURE_FOV_SCALE`
constant in this specification. Pressure reduction applies continuously from
PressureScalar=0 with no threshold gate. Any prior reference to these constants
in draft material is an error; §3.10 is the authoritative constants list.

---

**FOV-001 — Centre-facing entity is always visible**

Setup: Observer faces 0°. Target at 0° angular offset. Decisions = 10. PressureScalar = 0.

Derivation: bonus = (10/20) × 10° = 5.0°. EffectiveFoV = 160° + 5.0° = 165.0°. Half-angle = 82.5°.
Target at 0° offset < 82.5° → visible.

Expected: `IsVisible = true`. Tolerance: ±0.01° on angle computation.

FR ref: FR-2.4.2-01

---

**FOV-002 — Entity at exact FoV boundary is included (boundary inclusive)**

Setup: Decisions=1, PressureScalar=0. Derivation: bonus = (1/20) × 10° = 0.5°.
EffectiveFoV = 160° + 0.5° = 160.5°. Half-angle = 80.25°. Target at exactly 80.25° offset.

Expected: `IsVisible = true`. Boundary is inclusive per §3.1.2.

Tolerance: ±0.001°. Note: implementer must document whether boundary test uses `≤`
on dot-product cosine or strict angle comparison, as floating-point representations
of cos(80.25°) differ by ~10⁻⁷ depending on computation path. Document in
`FovCalculator.cs` comments.

FR ref: FR-2.4.2-02

---

**FOV-003 — Entity one step outside FoV boundary is excluded**

Setup: Same as FOV-002 (Decisions=1, PressureScalar=0, HalfAngle=80.25°) but target at 80.26° offset.

Expected: `IsVisible = false`.

Tolerance: ±0.001°. Same implementer documentation note as FOV-002 applies.

FR ref: FR-2.4.2-02

---

**FOV-004 — Decisions=1 vs Decisions=20 produce measurable FoV difference**

Derivation:
- Decisions=1:  bonus = (1/20)  × 10° = 0.5°.  EffectiveFoV = 160.5°.
- Decisions=20: bonus = (20/20) × 10° = 10.0°. EffectiveFoV = 170.0°.
- Difference = 9.5°. (Bonus range spans 0.5°–10.0° because the formula uses
  Decisions/20, giving Decisions=1 a non-zero baseline of 0.5°.)

Expected: EffectiveFoV(D=1) = 160.5°, EffectiveFoV(D=20) = 170.0°. Difference = 9.5°.
Both values monotonically confirm Decisions↑ → FoV↑.

Tolerance: ±0.01°. FR ref: FR-2.4.2-03

---

**FOV-005 — PressureScalar = 0 produces no FoV reduction**

Setup: PressureScalar = 0. Decisions = 10.

Expected: PressureReduction term = 0. EffectiveFoV = BASE + bonus only. No narrowing.

Tolerance: Exact (no reduction path executed). FR ref: FR-2.4.2-04

---

**FOV-006 — PressureScalar = 0.5 produces proportional FoV reduction**

Setup: PressureScalar = 0.5. Decisions = 10.

Derivation: bonus = (10/20) × 10° = 5.0°. PressureReduction = 0.5 × 30° = 15.0°.
EffectiveFoV = 160° + 5.0° − 15.0° = 150.0°. HalfAngle = 75.0°.
Floor check: 150° > 120° → no clamp.

Expected: EffectiveFoV = 150.0°. Reduction is continuous and linear — there is no
threshold gate below which pressure has zero effect. At PS=0.5, the agent has already
lost 15° of FoV relative to neutral.

Tolerance: ±0.01°. FR ref: FR-2.4.2-04

---

**FOV-007 — PressureScalar = 1.0 with Decisions = 1 — floor not reached**

This test confirms `MIN_FOV_ANGLE` floor logic exists but is not triggered at
maximum PressureScalar under current constants. This is intentional and must not
be treated as dead code.

Derivation: PressureScalar=1.0, Decisions=1.
Bonus = (1/20) × 10° = 0.5°. EffectiveFoV_BeforePressure = 160.5°.
PressureReduction = 1.0 × 30° = 30°. EffectiveFoV = 160.5° − 30° = 130.5°.
Floor check: 130.5° > MIN_FOV_ANGLE=120° → clamp not triggered.
To reach floor: need 160.5° − (P × 30°) < 120° → P > 1.35 → impossible (capped at 1.0).

Expected: EffectiveFoV = 130.5°. MIN_FOV_ANGLE clamp NOT active.

Tolerance: ±0.01°. FR ref: FR-2.4.2-05

---

**FOV-008 — Blind-side arc is strict complement of EffectiveFoV**

Setup: Decisions = 10, PressureScalar = 0.
Derivation: bonus = (10/20) × 10° = 5.0°. EffectiveFoV = 165.0°.

Expected: `PerceptionDiagnostics.EffectiveFoVAngle = 165.0°`. BlindSideArcWidth is not a
direct struct field — it is derived as 360° − EffectiveFoVAngle = 195.0°. Verify via
`PerceptionDiagnostics` in a test fixture that computes the derived value.

Tolerance: ±0.01°. FR ref: FR-2.4.2-06

---

## 5.3 Unit Tests — Shadow Cone Occlusion (§3.2)

**Constants (§3.10):** `AGENT_BODY_RADIUS` = 0.4m, `MIN_SHADOW_HALF_ANGLE` = 5°.

**Occlusion test (§3.2.1):** Target T is occluded if:
1. `dot(T − O, P − O) > dot(P − O, P − O)` — T is beyond occluder P relative to observer O.
2. Angular deviation of T from shadow cone axis `< arctan(AGENT_BODY_RADIUS / |P − O|)`,
   subject to `MIN_SHADOW_HALF_ANGLE` floor.

---

**OCC-001 — No occluder present — target visible**

Setup: Observer (0,0). Target (10,0). No opponents.

Expected: IsOccluded = false. FR ref: FR-2.4.3-01

---

**OCC-002 — Opponent collinear between observer and target — target occluded**

Setup: Observer (0,0). Occluder (5,0). Target (10,0). Occluder is opponent.

Dot check: dot((10,0),(5,0)) = 50 > dot((5,0),(5,0)) = 25 ✓. Angular deviation = 0°.
Shadow half-angle at 5m: arctan(0.4/5) = 4.57° < MIN_SHADOW_HALF_ANGLE → floor = 5°.
0° < 5° ✓ → occluded.

Expected: IsOccluded = true. FR ref: FR-2.4.3-02

---

**OCC-003 — Target at same distance as occluder — NOT occluded (boundary condition)**

Setup: Observer (0,0). Occluder (5,0). Target (5,1).

Dot check: dot((5,1),(5,0)) = 25. dot((5,0),(5,0)) = 25. 25 > 25 is false.
Target NOT beyond occluder — boundary condition is strict greater-than.

Expected: IsOccluded = false. FR ref: FR-2.4.3-03

---

**OCC-004 — Shadow cone half-angle at 5m — MIN floor active**

Occluder at 5m. arctan(0.4/5) = arctan(0.08) = 4.574°. Floor = 5° applies.

Sub-test A: Target at 4.9° angular deviation from shadow axis → occluded (< 5°).
Sub-test B: Target at 5.1° deviation → not occluded (> 5°).

Tolerance: ±0.01°. FR ref: FR-2.4.3-04

---

**OCC-005 — Shadow cone half-angle at 2m — MIN floor NOT active**

Occluder at 2m. arctan(0.4/2) = arctan(0.2) = 11.310°. 11.31° > MIN = 5° → floor not active.

Sub-test A: Target at 11.0° deviation → occluded.
Sub-test B: Target at 11.5° deviation → not occluded.

Tolerance: ±0.05°. FR ref: FR-2.4.3-04

---

**OCC-006 — Teammate does NOT generate shadow cone (Stage 0)**

Setup: Observer (0,0). Teammate (5,0) — same team. Target (10,0).

Expected: IsOccluded = false. Teammate shadow cones explicitly disabled at Stage 0
per OQ-1. Not an oversight — document in test comment.

FR ref: FR-2.4.3-05

---

**OCC-007 — Multiple occluders — any one causing occlusion is sufficient**

Setup: Occluder A at (3,0), Occluder B at (7,0). Target at (10,0). Both opponents.

Expected: IsOccluded = true. Either occluder independently satisfies the occlusion
condition. Test confirms OR logic (not AND).

FR ref: FR-2.4.3-06

---

**OCC-008 — Observer never appears as their own occluder**

Setup: Self-entry present in spatial hash candidate list (can occur if hash returns
the querying agent).

Expected: Self-entry is filtered before occlusion loop. Observer AgentId never
enters the occluder iteration. No self-occlusion possible.

FR ref: FR-2.4.3-07

---

**OCC-009 — Zero-distance occluder — no divide-by-zero**

Setup: Observer at (0,0). Occluder at (0,0) — coincident position (degenerate case;
can arise if two agents overlap during a physics-step collision resolution edge case).

Expected: No DivideByZeroException or NaN. System handles |P − O| = 0 gracefully.
Target treated as visible (a zero-distance occluder cannot logically block observation).
Recovery: distance clamped to minimum before arctan computation.
Verify: no NaN in any downstream pipeline step after this input.

FR ref: §3.2.1 (robustness requirement)

---

**OCC-010 — Target at exactly MAX_PERCEPTION_RANGE boundary**

Setup: Observer at (0,0). MAX_PERCEPTION_RANGE = 120m.

Sub-test A: Target at (120.000, 0) — exactly on boundary.
Expected: Target included in candidate list (boundary condition is ≤ 120m, not < 120m).
FoV and occlusion tests proceed normally.

Sub-test B: Target at (120.001, 0) — marginally beyond boundary.
Expected: Target excluded before FoV test. Not present in VisibleOpponents or VisibleTeammates.

Tolerance: ±0.001m. FR ref: FR-2.4.1-01, §3.0 Step 2 candidate enumeration

---

**OCC-011 — Two collinear occluders — nearest occluder wins; furthest does not mask nearest**

Setup: Observer O at (0,0). Occluder P1 at (5,0). Occluder P2 at (8,0). Target T at (12,0).

Expected: T is occluded (P1 dot check satisfied). P2 is itself independently potentially
occluded by P1 — verify separately. Confirm the occlusion loop does not skip P1 because
P2 is also present, and does not double-count occlusion. T must be marked occluded
exactly once regardless of how many occluders independently qualify.

Sub-test: Verify P2 is also occluded (dot check: dot((8,0),(5,0))=40 > dot((5,0),(5,0))=25 ✓;
shadow half-angle at 5m = 5° floor; P2 at 0° deviation → occluded by P1).

FR ref: FR-2.4.3-02, FR-2.4.3-06

---

**OCC-012 — Target at exactly FoV boundary angle — deterministic result**

Setup: Observer facing East (1,0). EffectiveFoV = 160° (half-angle = 80.000°).
Target positioned at exactly 80.000° angular offset from facing direction.

Expected: Deterministic result — either consistently visible or consistently not visible
for this exact input. The ≤ / < boundary convention in the FoV angular test must be
documented by the implementer (consistent with FOV-002/FOV-003 implementer note in §5.15).
Re-running the test with identical inputs must produce the identical result 100/100 times.

Note: This test verifies determinism and internal consistency of the boundary convention,
not a specific pass/fail outcome. An implementation using dot-product comparison will
produce a different boundary behaviour from one using atan2. Both are acceptable provided
the convention is documented and applied consistently.

Tolerance: Exact repeat. FR ref: FR-2.4.2-01, §5.15 tolerance table

---

**OCC-013 — High-density scenario: 10 opponents in 3m radius — occlusion loop completes correctly**

Setup: Observer at (0,0). 10 opponents all clustered within 3m radius at varying angles.
5 targets at 15m at various angles.

Expected: Occlusion loop processes all (target × occluder) combinations without exception
or early exit. Maximum pairs: 5 targets × 10 occluders = 50 shadow cone evaluations.
At least 1 target occluded (geometry guarantees at least partial occlusion in a 10-agent
cluster). No ArrayIndexOutOfBoundsException, no NaN in results.
Confirms O(n×k) implementation stable at k=10.

FR ref: FR-2.4.3-06, §6.1.1 (O(n×k) complexity bound)

---

## 5.4 Unit Tests — Recognition Latency (§3.3)

**Constants (§3.10):** `L_MAX` = 5 ticks (500ms), `L_MIN` = 1 tick (100ms),
`CONFIRMATION_EXPIRY_TICKS` = 1 tick.

**Base formula (§3.3.2):**
```
L_rec = L_MAX − (L_MAX − L_MIN) × (Decisions − 1) / 19
      = 5 − 4 × (Decisions − 1) / 19
```
**Half-turn bonus (§3.3.3, cross-spec First Touch §3.3.2):** If target in peripheral
arc (40°–80° from facing): `L_rec_adj = floor(L_rec × 0.85)`, minimum `L_MIN`.

**Noise (§3.3.4):** Additive only: `noise = DeterministicHash(agentId, targetId, frameNumber) % 2`.
Result = L_rec + noise. Never below L_MIN (additive cannot violate floor algebraically).

---

**LR-001 — L_rec at Decisions=1 (maximum latency)**

Derivation: 5 − 4 × (0/19) = 5 ticks (500ms).

Expected: L_rec = 5 ticks. Entity confirmed at tick 6 after first visibility (tick 1).

Tolerance: Exact integer. FR ref: FR-2.4.4-01

---

**LR-002 — L_rec at Decisions=20 (minimum latency)**

Derivation: 5 − 4 × (19/19) = 5 − 4 = 1 tick (100ms).

Expected: L_rec = 1 tick. Entity confirmed on tick 2 after first visibility.

Tolerance: Exact integer. FR ref: FR-2.4.4-01

---

**LR-003 — L_rec at Decisions=10 (mid-range)**

Derivation: 5 − 4 × (9/19) = 5 − 1.8947... = 3.1053... → Mathf.FloorToInt = 3 ticks.

Expected: L_rec = 3 ticks. Floor rounding is the documented convention per §3.3.2.
Any implementation producing L_rec=4 ticks at this input is using a non-floor rounding
convention and must be corrected.

Tolerance: Exact integer (floor rounding is deterministic). FR ref: FR-2.4.4-01

---

**LR-004 — Half-turn bonus reduces L_rec for peripheral arc entity**

Setup: Decisions=10 (L_rec=3). Target at 55° from facing (within 40°–80° peripheral arc).

Derivation: L_rec_adj = floor(3 × 0.85) = floor(2.55) = 2 ticks. Check: 2 ≥ L_MIN=1 ✓.

Expected: L_rec = 2 ticks (vs 3 without bonus). FR ref: FR-2.4.4-02

---

**LR-005 — Half-turn bonus NOT applied outside peripheral arc**

Sub-test A: Target at 20° (inside FoV centre, below 40° inner bound). No bonus.
Sub-test B: Target at 85° (beyond FoV edge — not visible at all).

Expected: No bonus applied in either case. L_rec = base value.

FR ref: FR-2.4.4-03

---

**LR-006 — Additive noise never pushes L_rec below L_MIN**

Setup: Decisions=20 (L_rec=1 = L_MIN). Noise hash resolves to +1.

Expected: L_rec = 2 (= 1 + 1). L_MIN floor NOT violated. Noise is always +0 or +1.

Verify: re-run with all 22 possible (agentId, targetId, frameNumber) hash combinations
that yield noise=1 when base L_rec = L_MIN. None should produce L_rec < 1.

Tolerance: Exact. FR ref: FR-2.4.4-04

---

**LR-007 — 1-tick visibility gap does NOT reset confirmation counter**

Setup: Target visible ticks 1–4, invisible tick 5, visible tick 6. L_rec = 3.
CONFIRMATION_EXPIRY_TICKS = 1.

Expected: Counter NOT reset on 1-tick gap (gap ≤ CONFIRMATION_EXPIRY_TICKS).
Entity confirmed at tick 4 and retained through gap. Boundary noise absorbed.

Tolerance: Exact tick count. FR ref: FR-2.4.4-05

---

**LR-008 — 2-tick gap resets confirmation counter**

Setup: Target visible ticks 1–3, invisible ticks 4–5, visible tick 6. L_rec = 3.

Expected: Counter resets on 2-tick gap (> CONFIRMATION_EXPIRY_TICKS=1). Entity must
re-accumulate 3 ticks from tick 6. Not confirmed until tick 9.

Tolerance: Exact tick count. FR ref: FR-2.4.4-05

---

**LR-009 — Ball is exempt from L_rec (immediate recognition)**

Setup: Ball becomes visible at tick N (was invisible at tick N−1).

Expected: `BallVisible = true` at tick N. `BallStalenessFrames = 0`. No latency
counter checked. Immediate inclusion per OQ-2 resolution.

Tolerance: Exact. FR ref: FR-2.4.6-01

---

## 5.5 Unit Tests — Blind-Side Awareness and Shoulder Check (§3.4)

**Constants (§3.10):** `CHECK_MAX_TICKS` = 30 (Anticipation=1),
`CHECK_MIN_TICKS` = 6 (Anticipation=20), `SHOULDER_CHECK_DURATION` = 3 ticks.

**Interval formula (§3.4.2):**
```
Interval = CHECK_MAX_TICKS − (CHECK_MAX_TICKS − CHECK_MIN_TICKS) × (Anticipation − 1) / 19
         = 30 − 24 × (Anticipation − 1) / 19
```

---

**SC-001 — Blind-side entity not visible without shoulder check**

Setup: Target at 200° offset from facing (inside blind arc). No check active.

Expected: IsVisible = false. Target absent from all `FilteredView` arrays
(`VisibleTeammates`, `VisibleOpponents`, `BlindSidePerceivedAgents`).

FR ref: FR-2.4.5-01

---

**SC-002 — Blind-side entity visible during active shoulder check window**

Setup: Shoulder check fires at tick T. Target at 200° offset. L_rec = 1 (Decisions=20).

Expected: Target visible at tick T+1 (after 1-tick L_rec). Window duration = 3 ticks.
Target NOT visible at tick T+4 (window expired, no re-check).

Tolerance: Exact tick boundary. FR ref: FR-2.4.5-02

---

**SC-003 — Check interval at Anticipation=1 (slowest)**

Derivation: 30 − 24 × (0/19) = 30 ticks.

Expected: First check at tick 30. Next at tick 60. Interval = exactly 30 ticks.

Tolerance: Exact. FR ref: FR-2.4.5-03

---

**SC-004 — Check interval at Anticipation=20 (fastest)**

Derivation: 30 − 24 × (19/19) = 30 − 24 = 6 ticks.

Expected: Check fires every 6 ticks.

Tolerance: Exact. FR ref: FR-2.4.5-03

---

**SC-005 — Possession doubles check interval**

Setup: Anticipation=20 (base interval = 6 ticks). Agent has possession.

Expected: Interval = 12 ticks (= 6 × 2). Checked while possession flag holds.

Tolerance: Exact. FR ref: FR-2.4.5-04

---

**SC-006 — Blind-side entities remain subject to L_rec during check window**

Setup: Target enters blind-side at tick T (check fires). L_rec = 3.

Expected: Target NOT in snapshot until tick T+3. Recognition latency applies even
during the check window. The window only grants geometric visibility — not instant
recognition.

Tolerance: Exact tick count. FR ref: FR-2.4.5-05

---

**SC-007 — Decision Tree has no method to trigger shoulder check**

Design verification (not a runtime test). Audit public interface of `PerceptionSystem`.

Expected: No `TriggerShoulderCheck()` or equivalent public method exists.
Autonomous-only per OQ-3 resolution. Verified by interface review in §4.

FR ref: FR-2.4.5-06

---

**SC-008 — ShoulderCheckAnimData stub populated when check fires**

Setup: Shoulder check fires at tick T.

Expected: `ShoulderCheckAnimData.CheckFiredThisTick = true`.
`ShoulderCheckAnimData.CheckDirection` = unit vector toward blind-side arc midpoint.
Struct populated at Stage 0 even though unconsumed (no Animation System yet).

FR ref: FR-2.4.5-07

---

## 5.6 Unit Tests — Ball Perception (§3.5)

---

**BP-001 — Ball within FoV receives immediate inclusion (no L_rec)**

Setup: Ball at 30° angular offset. Not occluded. EffectiveFoV = 160°.

Expected: `BallVisible = true`. `BallPerceivedPosition` = ground truth 2D position.
`BallStalenessFrames = 0`. Ball bypasses latency counter.

Tolerance: Position ±0.001m. FR ref: FR-2.4.6-01

---

**BP-002 — Ball outside FoV is not visible**

Setup: Ball at 110° angular offset. EffectiveFoV = 160° (half-angle = 80°). 110° > 80°.

Expected: `BallVisible = false`. `BallPerceivedPosition` retains last known.
`BallStalenessFrames` increments.

FR ref: FR-2.4.6-02

---

**BP-003 — Ball occluded by opponent shadow cone**

Setup: Ball beyond opponent who is collinear between observer and ball.

Expected: `BallVisible = false` despite ball being within FoV angle. Occlusion
overrides FoV result. Same occlusion geometry as agent occlusion.

FR ref: FR-2.4.6-03

---

**BP-004 — BallStalenessFrames increments each invisible tick**

Setup: Ball invisible for 4 consecutive ticks.

Expected: BallStalenessFrames = 1, 2, 3, 4 at each successive tick.

Tolerance: Exact integer. FR ref: FR-2.4.6-04

---

**BP-005 — BallStalenessFrames resets on re-visibility**

Setup: Ball invisible 3 ticks (Staleness=3). Ball re-enters visibility at tick 4.

Expected: BallStalenessFrames = 0 at tick 4.

Tolerance: Exact. FR ref: FR-2.4.6-04

---

**BP-006 — Last known ball position retained during occlusion**

Setup: Ball at (30,20) at tick N. Ball occluded ticks N+1 through N+4.

Expected: `BallPerceivedPosition = (30,20)` across all occlusion ticks. Position
not updated to true ball position while invisible.

Tolerance: ±0.001m. FR ref: FR-2.4.6-05

---

## 5.7 Unit Tests — Pressure Scalar (§3.6)

**Formula (First Touch §3.5, consumed read-only):**
```
rawPressure = Σ (MIN_PRESSURE_DISTANCE / distance)²
              for each opponent within PRESSURE_RADIUS
PressureScalar = Clamp(rawPressure / PRESSURE_SATURATION, 0, 1)
```
**Constants (§3.10):** `PRESSURE_RADIUS` = 3.0m [CROSS], `MIN_PRESSURE_DISTANCE` = 0.3m
[CROSS], `PRESSURE_SATURATION` = 1.5 [CROSS].

---

**PS-001 — No opponents within radius — PressureScalar = 0**

Expected: PressureScalar = 0.0. FR ref: FR-2.4.2-04

---

**PS-002 — Single opponent at MIN_PRESSURE_DISTANCE**

Derivation: rawPressure = (0.3/0.3)² = 1.0. PressureScalar = Clamp(1.0/1.5, 0, 1) = 0.667.

Expected: PressureScalar = 0.667. Tolerance: ±0.001. FR ref: FR-2.4.2-04

---

**PS-003 — PressureScalar clamped to 1.0 with 3 close opponents**

Derivation: 3 opponents all at 0.3m. rawPressure = 3.0.
PressureScalar = Clamp(3.0/1.5, 0, 1) = Clamp(2.0, 0, 1) = 1.0.

Expected: PressureScalar = 1.0. Tolerance: Exact. FR ref: FR-2.4.2-04

---

**PS-004 — PressureScalar does NOT affect L_rec**

Setup: PressureScalar = 1.0. Decisions = 10 (L_rec = 3 ticks).

Expected: L_rec = 3 ticks unchanged. Pressure scalar has a single use: FoV narrowing
only. No effect on latency per §3.6.3.

Tolerance: Exact tick. FR ref: FR-2.4.2-04

---

**PS-005 — PressureScalar stored in PerceptionDiagnostics regardless of magnitude**

Setup: PressureScalar = 0.3 (pressure reduction applies continuously — no threshold gate
exists in this spec; at PS=0.3 the FoV narrowing = 0.3 × 30° = 9.0°).

Expected: `PerceptionDiagnostics.PressureScalar = 0.3`. Field populated whenever pressure
is non-zero. Decision Tree may consume `PressureScalar` from `PerceptionDiagnostics` for
diagnostic purposes; the Decision Tree does NOT receive `PerceptionDiagnostics` directly
(it is a debug/telemetry struct), but test rigs may read it to verify correct storage.

Tolerance: ±0.001. FR ref: FR-2.4.2-04

---

## 5.8 Unit Tests — Forced Refresh (§3.8)

---

**FR-001 — Ball contact triggers refresh for involved agents only**

Setup: Agent A strikes ball mid-heartbeat. Agents A and B (recipient) flagged.

Expected: A and B receive new `FilteredView` before next standard heartbeat.
`ForcedRefreshThisTick = true`. Uninvolved agents C–V are NOT refreshed.

Tolerance: Tick-exact. FR ref: FR-2.4.1-04

---

**FR-002 — Tackle triggers refresh for tackler and tackled agent**

Setup: Agent A tackles Agent B mid-heartbeat.

Expected: A and B both receive forced refresh. All other agents unaffected.

Tolerance: Tick-exact. FR ref: FR-2.4.1-04

---

**FR-003 — Forced refresh sets L_rec = 0 for all entities**

Setup: Entity X first becomes visible to refreshed observer at exact moment of
forced refresh.

Expected: L_rec = 0 for X — entity added to snapshot immediately.
Standard latency counter bypassed for this refresh cycle only.

Tolerance: Exact. FR ref: FR-2.4.4-06

---

**FR-004 — Forced refresh does NOT reset standard heartbeat clock**

Setup: Forced refresh fires at tick 5. Standard schedule: ticks 0, 10, 20...

Expected: Next standard heartbeat still fires at tick 10. Forced refresh is an
additive event — it does not advance or reset the 10Hz clock.

Tolerance: Exact. FR ref: FR-2.4.1-05

---

**FR-005 — Possession change triggers refresh for both involved agents**

Setup: Agent A loses possession to Agent B (non-tackle). Possession change event fires
at tick 12.

Expected: Both A and B receive forced refresh at tick 12 (`ForcedRefreshThisTick=true`).
All other agents unaffected. Confirms possession-change trigger distinct from tackle
trigger. Both agents use full pipeline with L_rec=0 override.

Tolerance: Tick-exact. FR ref: FR-2.4.1-04, §3.8.1 trigger table

---

**FR-006 — L_rec=0 override applies ONLY to the forced refresh tick**

Setup: Agent A receives forced refresh at tick 15. Agent A's Decisions=1 (L_rec=5
normally). A new opponent enters A's FoV at tick 15.

Sub-test A: At forced-refresh tick 15 — opponent confirmed immediately (L_rec=0 override active).
Sub-test B: At next standard heartbeat tick 20 — newly visible opponent requires full L_rec=5 accumulation.

Expected: L_rec override does NOT persist beyond the forced refresh event.

Tolerance: Exact tick. FR ref: FR-2.4.4-06

---

**FR-007 — Ball-contact refresh applies to all agents within 5m of ball**

Setup: Ball at position (40,30). Agent A at (40,30) — strikes ball. Agent B at (42,31)
— 2.24m from ball. Agent C at (45,35) — 6.40m from ball.

Expected: Agent A refreshed (ball contactor). Agent B refreshed (within 5m).
Agent C NOT refreshed (6.40m > 5m threshold). Standard heartbeat unaffected for C.

Tolerance: Tick-exact. Distance-exact for threshold boundary.
FR ref: FR-2.4.1-04, §3.8.1 trigger table

---

**FR-008 — Two simultaneous forced-refresh triggers for same agent — snapshot built once only**

Setup: Ball contact event and possession change event both fire at tick 22 for Agent A.
Two simultaneous trigger conditions qualifying Agent A.

Expected: Agent A receives exactly ONE new snapshot at tick 22. PerceptionSystem does
not double-execute the pipeline for the same agent in the same tick. `ForcedRefreshThisTick=true`.
No duplicate snapshot entries, no double-allocation.

Tolerance: Exact (pipeline execution count). FR ref: §3.8.2

---

**FR-009 — PerceptionRefreshEvent stub published on forced refresh**

Setup: Ball contact fires at tick 18 for Agent A.

Expected: `PerceptionRefreshEvent` struct populated with `AgentId=A.AgentId`,
`TriggerFrame=18`, `TriggerEventType="BallContact"`. Stub publication succeeds
without consumer (Event System Spec #17 not yet written). No NullReferenceException
or unhandled exception thrown from stub publication path.

FR ref: §3.8.3 (PerceptionRefreshEvent stub definition)

---

**FR-010 — Forced refresh ForcedRefreshThisTick flag never set on uninvolved agents**

Setup: 22 agents. Ball contact event involves only Agent 1 and agents within 5m.
Run 500 ticks with forced refreshes triggering at various ticks.

Expected: For any agent not qualifying as an involved participant in a given forced
refresh event, `ForcedRefreshThisTick = false` in their `FilteredView` at that tick. Monitor
all 22 agent snapshot streams over 500 ticks. `ForcedRefreshThisTick` is set only when
the agent independently qualifies as a named participant.

Tolerance: Exact boolean across all 500 ticks × 22 agents. FR ref: FR-2.4.1-04

---

## 5.9 Unit Tests — Snapshot Assembly (§3.7)

---

**SNAP-001 — Snapshot contains exactly the confirmed visible entities**

Setup: 22-agent world. 6 confirmed visible. 3 in latency accumulation (not yet confirmed).

Expected: `VisibleTeammates` + `VisibleOpponents` total = 6. The 3 unconfirmed agents
absent. No null entries in arrays.

FR ref: FR-2.4.1-01

---

**SNAP-002 — FrameNumber matches current heartbeat tick**

Setup: Snapshot built at tick 47.

Expected: `FilteredView.FrameNumber = 47`.

FR ref: FR-2.4.1-02

---

**SNAP-003 — BlindSideWindowActive matches shoulder check state**

Sub-test A: Shoulder check active. Expected: `BlindSideWindowActive = true`.
Sub-test B: No check active. Expected: `BlindSideWindowActive = false`.

FR ref: FR-2.4.5-02

---

**SNAP-004 — Empty snapshot is valid struct (no null refs)**

Setup: Agent faces away from all 21 other agents. None visible. No ball visible.

Expected: Valid `FilteredView`. `VisibleTeammates.Length = 0`.
`VisibleOpponents.Length = 0`. `BallVisible = false`.
No NullReferenceException. No default-zero struct corruption.

FR ref: FR-2.4.1-01

---

**SNAP-005 — VisibleTeammates and VisibleOpponents are mutually exclusive**

Setup: Agent A faces 4 teammates and 3 opponents. All 7 confirmed visible.

Expected: `VisibleTeammates.Length = 4`, `VisibleOpponents.Length = 3`.
No agent ID appears in both arrays. AgentId is the unique key; team assignment
is resolved from AgentState.TeamId at snapshot build time.

FR ref: FR-2.4.1-01

---

**SNAP-006 — PerceivedAgent.ConfidenceScore = 1.0 for all confirmed entities at Stage 0**

Setup: 5 confirmed visible opponents at varying distances (1m, 5m, 20m, 60m, 100m).

Expected: All 5 `PerceivedAgent.ConfidenceScore = 1.0`. Stage 0 binary confidence only.
Distance does NOT reduce ConfidenceScore at Stage 0 — continuous confidence degradation
is a Stage 1 feature (§7.1.2). Any value other than 1.0f for a confirmed entity is a
Stage 0 implementation defect.

Tolerance: Exact float (1.0f). FR ref: §3.7.2 (ConfidenceScore Stage 0 definition)

---

**SNAP-007 — BlindSidePerceivedAgents array correctly separates blind-side from forward-arc confirmations**

Setup: Active shoulder check fires at tick T. Agent B confirmed at 200° offset during
the window (blind-side). Agent C confirmed at 30° offset via standard FoV in same tick.

Expected: Agent B appears in `FilteredView.BlindSidePerceivedAgents`. Agent B does NOT
appear in `FilteredView.VisibleOpponents` or `FilteredView.VisibleTeammates`.
Agent C appears in `FilteredView.VisibleOpponents`. Agent C does NOT appear in
`FilteredView.BlindSidePerceivedAgents`.
The two arrays are mutually exclusive — an agent confirmed via blind-side shoulder check
cannot simultaneously appear in the forward-arc arrays.

Note: `IsInBlindSide` was removed from `PerceivedAgent` in §3.7.3 v1.3. The
`BlindSidePerceivedAgents` array is the authoritative mechanism for distinguishing
blind-side confirmations.

FR ref: §3.7.1 (BlindSidePerceivedAgents field definition), §3.4.3

---

**SNAP-008 — BlindSideWindowExpiry matches check fire tick + duration constant**

Setup: Shoulder check fires at tick 40. `SHOULDER_CHECK_DURATION = 3`.

Expected: `PerceptionDiagnostics.BlindSideWindowExpiry = 43` (= 40 + 3).
`BlindSideWindowActive = true` at ticks 40, 41, 42.
`BlindSideWindowActive = false` at tick 43.

Tolerance: Exact integer. FR ref: §3.7.2 (BlindSideWindowExpiry field definition)

---

**SNAP-009 — BlindSidePerceivedAgents is empty and BlindSideWindowActive=false when no check active**

Setup: No shoulder check firing. Three agents visible in forward arc. Two agents in blind arc (not checked).

Expected: `BlindSidePerceivedAgents.Length = 0`. `BlindSideWindowActive = false`.
Agents in the blind arc are NOT added to `VisibleOpponents` or `VisibleTeammates`.
`VisibleOpponents.Length + VisibleTeammates.Length` reflects forward-FoV confirmed entities only.

FR ref: §3.7.1 (BlindSidePerceivedAgents field condition), §3.4.1

---

**SNAP-010 — EffectiveFoVAngle field reflects pressure-narrowed value**

Setup: Decisions=15, PressureScalar=0.4.

Derivation:
```
FoV_bonus = (15/20) × 10° = 7.5°
FoV_before_pressure = 160° + 7.5° = 167.5°
Reduction = 0.4 × 30° = 12°
EffectiveFoV = 167.5° − 12° = 155.5°
```

Expected: `PerceptionDiagnostics.EffectiveFoVAngle = 155.5°`. Field must reflect
the post-pressure value, not the pre-pressure FoV. Implementer must not store
the pre-pressure value in this field.

Tolerance: ±0.01°. FR ref: §3.7.2 (EffectiveFoVAngle in PerceptionDiagnostics)

---

## 5.10 Unit Tests — Determinism (§2.4.7)

---

**DET-001 — Identical seed → bit-identical FilteredView and PerceptionDiagnostics**

Setup: Two independent PerceptionSystem instances, `matchSeed = 12345`. Same 22-agent
initial state. Run 100 heartbeat ticks.

Expected: Every field of every `FilteredView` and every `PerceptionDiagnostics` for
every agent at every tick is bit-identical between instances.

Tolerance: Bit-exact. FR ref: FR-2.4.7-01

---

**DET-002 — Different seed — only hash-derived noise may differ**

Setup: Seeds 12345 and 99999. Same agent state.

Expected: L_rec noise (+0/+1) may differ between seeds. EffectiveFoV, PressureScalar,
and occlusion results are seed-independent (no RNG involvement). FoV and occlusion
results must be identical across seeds.

FR ref: FR-2.4.7-02

---

**DET-003 — Replay: recorded tick state reproduces identical snapshot**

Setup: Record full `AgentState[22]` + `BallState` at tick 50. Replay from this state.

Expected: `FilteredView` at tick 50 is bit-identical to original. Confirms all
inputs fully determine output (no hidden mutable static state).

Tolerance: Bit-exact. FR ref: FR-2.4.7-01

---

**DET-004 — Agent processing order does not affect individual snapshot**

Setup: Process 22 agents in order 0→21 vs order 21→0.

Expected: Each agent's `FilteredView` is bit-identical regardless of processing
order. No shared mutable state is written between agents during the pipeline.

Tolerance: Bit-exact. FR ref: FR-2.4.7-03

---

