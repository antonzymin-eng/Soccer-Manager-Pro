## 5.11 Integration Tests

Integration tests run in Unity Play Mode with real `AgentState`, `BallState`, and
spatial hash instances. All 22-agent scenarios use scripted (not AI-driven) movement.
Each test verifies a cross-specification data boundary or a multi-agent scenario.

**Target: ≥ 12 tests.** Group mapping per §4 boundary contracts.

---

### 5.11.1 AgentState → Perception (Agent Movement Spec #2 boundary)

**IT-AM-001 — FoV orientation follows AgentState.FacingDirection**

Agent faces East (1,0). Target at North of agent (0,1). Angular offset = 90°.
EffectiveFoV = 160° (half-angle = 80°). 90° > 80° → not visible.

Expected: IsVisible = false. FR ref: FR-2.4.2-01

---

**IT-AM-002 — FoV recomputed fresh each heartbeat from current FacingDirection**

Tick 1: Agent faces East. Tick 2: Agent faces North. Target due North.

Expected: Tick 1: not visible (90° offset). Tick 2: visible (0° offset).
FacingDirection consumed from AgentState each tick, not cached across ticks.

FR ref: FR-2.4.1-02

---

**IT-AM-003 — Decisions attribute read correctly from PlayerAttributes**

AgentState.PlayerAttributes.Decisions = 20. Verify EffectiveFoV = 170°.

Expected: EffectiveFoV = 170°. Read path AgentState → PlayerAttributes → Decisions
confirmed as integer [1–20].

FR ref: FR-2.4.2-03

---

**IT-AM-004 — Anticipation attribute read correctly for shoulder check interval**

AgentState.PlayerAttributes.Anticipation = 20. Verify interval = 6 ticks.

Expected: Check fires at tick 6, 12, 18... Interval = CHECK_MIN_TICKS.

FR ref: FR-2.4.5-03

---

### 5.11.2 BallState → Perception (Ball Physics Spec #1 boundary)

**IT-BP-001 — BallState.Position 3D → 2D projection**

BallState.Position = (30, 2.5, 20) (x, height, z). Observer at (0,0) 2D.

Expected: BallPerceivedPosition = (30, 20). Height component (Y in 3D) dropped.
Angular offset and range computed from projected 2D coordinates.

Tolerance: ±0.001m. FR ref: FR-2.4.6-01

---

**IT-BP-002 — Ball contact forced refresh reaches correct agents only**

Ball contact event fires at tick 15 (mid-heartbeat). Involved agents: #3, #7.

Expected: Agents #3 and #7 receive new snapshot at tick 15 (`IsForceRefreshed=true`).
Agents #1, #2, #4–#6, #8–#22 unchanged until next standard tick 20.

FR ref: FR-2.4.1-04

---

### 5.11.3 Spatial Hash → Perception (Collision System Spec #3 boundary)

**IT-CS-001 — QueryRadius returns correct candidate count (self excluded)**

22 agents placed. 10 agents within MAX_PERCEPTION_RANGE=120m of observer.

Expected: Candidate list = exactly 10 (self filtered if returned by hash).

FR ref: FR-2.4.1-01

---

**IT-CS-002 — Pressure query uses distinct 3.0m radius call**

3 opponents within 3.0m. 5 opponents at 4–10m.

Expected: PressureScalar computed from 3 close opponents only. Distant 5 contribute 0.
Two QueryRadius calls: 120m (candidates) and 3.0m (pressure) — both confirmed in test.

FR ref: FR-2.4.2-04

---

**IT-CS-003 — Perception never writes to spatial hash (read-only contract)**

1,000 heartbeats. Monitor spatial hash for any insert/remove/update call originating
from PerceptionSystem.

Expected: Zero mutations. FR ref: FR-2.4.1-03

---

### 5.11.4 Multi-Agent and Long-Duration Scenarios

**IT-FULL-001 — Elite midfielder vs novice defender — exact measurable differences**

Setup: Agent A (Decisions=18, Anticipation=18). Agent B (Decisions=2, Anticipation=2).
Same position, same facing (East), same pressure (PressureScalar=0). Same world state.

Derivations:
- Agent A FoV: 160 + 10×(17/19) = 160 + 8.95 = 168.95°. Half-angle = 84.47°.
- Agent B FoV: 160 + 10×(1/19) = 160 + 0.53 = 160.53°. Half-angle = 80.26°.
- Agent A L_rec: 5 − 4×(17/19) = 5 − 3.58 = 1.42 → floor = 1 tick (vs Agent B: 4 ticks).
- Agent A check interval: 30 − 24×(17/19) = 30 − 21.47 = 8.53 → floor = 8 ticks.
- Agent B check interval: 30 − 24×(1/19) = 30 − 1.26 = 28.74 → floor = 28 ticks.

Expected: All four computed values match derivations above (±0.01° for angles, exact
for tick values). Agent A has measurably wider FoV, faster recognition, and more
frequent shoulder checks.

FR ref: FR-2.4.2-03, FR-2.4.4-01, FR-2.4.5-03

---

**IT-FULL-002 — Awareness builds over first several ticks (match cold-start)**

22 agents at kickoff positions. All L_rec counters = 0. No prior awareness.

Expected: At tick 0: each agent perceives 0 confirmed visible agents (all in latency
accumulation). By tick 5: agents with Decisions≥17 (L_rec=1) have confirmed nearby
entities. By tick 10 (L_MAX): even Decisions=1 agents have confirmed all visible entities.
VisibleAgents count grows monotonically in first ~5 ticks.

FR ref: FR-2.4.4-01

---

**IT-FULL-003 — Agent behind wall of opponents — correctly perception-isolated**

Observer at (0,0). 4 opponents in a lateral line at x=5, y= −4 to +4 (2m spacing),
forming a wall. Target at (10,0).

Expected: Target at (10,0) occluded by one of the wall opponents. IsVisible = false.
Observer VisibleOpponents does NOT include the target.

FR ref: FR-2.4.3-02

---

**IT-FULL-004 — 22-agent full heartbeat — all snapshots produced, zero nulls**

22 agents at valid positions. Run one complete 10Hz heartbeat.

Expected: 22 PerceptionSnapshots produced. None null. Each ObserverId matches
the corresponding agent. No exceptions. Completes within 2ms budget.

FR ref: FR-2.4.1-01, FR-2.4.8-01

---

**IT-FULL-005 — L_rec tracking does not leak memory over extended simulation**

9,000 heartbeats (90 simulated minutes at 10Hz). Scripted movement. Monitor
`_latencyCounters` dictionary size.

Expected: Dictionary size ≤ 22 × 21 = 462 entries (max pairwise tracking). Does not
grow unboundedly. Entries for agents that leave visibility are expired correctly.
Zero GC allocation per tick confirmed.

FR ref: FR-2.4.7-04, FR-2.4.8-01

---

**IT-FULL-006 — Determinism: 100 heartbeats, identical inputs → byte-identical snapshots**

Two PerceptionSystem instances, seed=42. Same scripted 22-agent movement.

Expected: All snapshot fields bit-identical at every tick for all 22 agents.

Tolerance: Bit-exact. FR ref: FR-2.4.7-01

---

## 5.12 Balance Tests

Balance tests validate that attribute scaling produces measurably different but not
exploitably dominant outcomes. These are scenario tests, not formula-verification
tests.

---

**BAL-001 — Decisions=1 vs Decisions=20: measurable option count difference**

Setup: Two identical agents at the same position and facing, surrounded by 8 targets
at various angles and distances. Run 100 heartbeats.

Measurement: Count mean `VisibleOpponents.Length` per snapshot over 100 ticks for
each agent.

Expected: Agent (D=20) consistently perceives more entities per snapshot than Agent
(D=1) due to wider FoV and lower L_rec. Ratio should be measurable (>10% difference
in mean visible count) and stable across the 100-tick window.

This test cannot produce exact expected values until AI-driven movement is available.
At Stage 0 it is a smoke test confirming directional correctness, not a precision
assertion.

---

**BAL-002 — Anticipation=1 vs Anticipation=20: blind-side detection frequency ratio matches formula**

Setup: A runner is placed behind two agents (one A=1, one A=20) and moves laterally
across the blind arc. Observe over 300 heartbeat ticks (30 seconds).

Derivation: Agent (A=20) fires check every 6 ticks → ~50 checks in 300 ticks.
Agent (A=1) fires check every 30 ticks → ~10 checks in 300 ticks. Ratio = ~5:1.

Expected: Observed blind-side detection frequency ratio falls within 4:1 to 6:1.
Confirms formula produces intended attribute scaling.

---

**BAL-003 — No perception system behaviour creates dominant exploitable patterns**

Qualitative review (not automated). After BAL-001 and BAL-002:

- Confirm D=20 agent does not perceive entities that are genuinely occluded (no
  occlusion bypass at high Decisions).
- Confirm A=20 agent cannot permanently see behind itself (only during 3-tick windows).
- Confirm pressure scalar cannot reduce EffectiveFoV below MIN_FOV_ANGLE floor
  (confirmed algebraically in FOV-007; verify no code path bypasses the clamp).

---

## 5.13 Performance Tests

**Authoritative budget (Outline §6.1):** 10Hz heartbeat = 100ms window.
Perception allocation = **2ms for all 22 agents**. Per-agent budget = **~90µs**.

These tests are classified as integration tests and run in Unity Play Mode with
profiling enabled.

---

**PERF-001 — 22-agent heartbeat batch within 2ms budget**

Setup: 22 agents. Full mutual visibility (maximum occlusion computation). 1,000
consecutive ticks. Measured via `PerceptionSystem.ProfilerMarker`.

Expected: Mean < 1ms. p95 < 2ms. p99 < 2.5ms (10% headroom above hard limit).
Hard fail: any tick > 5ms.

Reference hardware: Intel Core i7-10700K, 32GB RAM, Unity 2022 LTS Editor.

---

**PERF-002 — Worst-case occlusion load**

Setup: All 22 agents opponents to each other (cross-team assignment). Maximum
shadow cone tests = 22 × 21 = 462 per tick.

Expected: Still within 2ms budget. Occlusion is O(n×k) — verify k=21 case does not
violate budget even without spatial hash culling.

---

**PERF-003 — Zero GC allocation per heartbeat tick**

Setup: 22 agents, 1,000 ticks. Unity Profiler GC Alloc column monitored.

Expected: 0 bytes GC allocation per tick. All buffers pre-allocated. Any GC
allocation is a hard failure. `ReadOnlySpan<PerceivedAgent>` views used correctly
per §2.3.1 v1.1 fix.

---

**PERF-004 — 90-minute simulation — zero NaN propagation**

54,000 heartbeats (90 sim-minutes × 10Hz). All 22 agents scripted.

Expected: Zero `float.IsNaN()` or `float.IsInfinity()` in any PerceptionSnapshot
field across all ticks for all agents. Any NaN is a hard failure.

---

## 5.14 Functional Requirement Coverage Matrix

Every SHALL-level requirement from §2.4 must map to at least one test. This matrix
is the sign-off gating document for Section 5 approval.

| Subsystem | Unit Tests | Integration Tests | Balance/Perf | FR Coverage |
|-----------|-----------|-------------------|--------------|-------------|
| FoV Model (§2.4.2) | FOV-001 to FOV-008 | IT-AM-001 to IT-AM-003, IT-FULL-001, IT-FULL-002 | BAL-001 | FR-2.4.2-01 to -06 ✅ |
| Occlusion (§2.4.3) | OCC-001 to OCC-013 | IT-CS-001, IT-FULL-003 | — | FR-2.4.3-01 to -07 ✅ |
| Recognition Latency (§2.4.4) | LR-001 to LR-009 | IT-FULL-001, IT-FULL-002 | — | FR-2.4.4-01 to -06 ✅ |
| Blind-Side / Shoulder Check (§2.4.5) | SC-001 to SC-008 | IT-FULL-001 | BAL-002, BAL-003 | FR-2.4.5-01 to -07 ✅ |
| Ball Perception (§2.4.6) | BP-001 to BP-006, LR-009 | IT-BP-001, IT-BP-002 | — | FR-2.4.6-01 to -05 ✅ |
| Pressure Scalar (§2.4.2/§3.6) | PS-001 to PS-005 | IT-CS-002, IT-FULL-001 | BAL-003 | FR-2.4.2-04/-05 ✅ |
| Forced Refresh (§2.4.1) | FR-001 to FR-010 | IT-BP-002 | — | FR-2.4.1-04/-05 ✅ |
| Snapshot Assembly (§3.7) | SNAP-001 to SNAP-010 | IT-FULL-004 | — | FR-2.4.1-01/-02 ✅ |
| Determinism (§2.4.7) | DET-001 to DET-004 | IT-FULL-006 | — | FR-2.4.7-01 to -04 ✅ |
| Performance (§2.4.8) | — | PERF-001 to PERF-004 | — | FR-2.4.8-01 ✅ |
| Memory / No GC (§2.4.8) | — | PERF-003, IT-FULL-005 | — | FR-2.4.8-01 ✅ |

---

## 5.15 Tolerance Derivations

All tolerance values are derived, not asserted arbitrarily. Mirrors Ball Physics
Appendix D and Agent Movement §3.7.9.

| Test Group | Tolerance | Derivation |
|-----------|-----------|-----------|
| FoV angle (general) | ±0.01° | float32 at 160° ≈ ε ≈ 0.002°. 0.01° = 5× float epsilon. Sub-pixel; gameplay-irrelevant. |
| FoV boundary (FOV-002/003) | ±0.001° | Boundary tests require tighter tolerance to confirm branch correctness. Implementer must document comparison method (dot product vs atan2). |
| Position (ball/agent) | ±0.001m | float32 at 100m: ε ≈ 10⁻⁴m. 0.001m = 10× float epsilon. Sub-centimetre; zero gameplay impact. |
| PressureScalar | ±0.001 | float32 arithmetic on pressure formula: ε < 0.0001. 0.001 = 10× headroom. |
| Tick counts (L_rec, intervals) | Exact integer | L_rec and shoulder check intervals are integer tick values. No floating-point. Any deviation is a logic error, not precision loss. |
| GC allocation | Exact zero | Zero-allocation policy §2.4.8. Any allocation is a hard defect, not a tolerance question. |

---

## 5.16 Deferred Validation

The following cannot be validated at Stage 0. They are non-blocking for Section 5
approval and are registered here to prevent future confusion.

| Item | What Cannot Be Tested | When |
|------|----------------------|------|
| DV-1: Decision Tree consumption | Cannot verify DT uses PerceptionSnapshot fields correctly | When Spec #8 is implemented |
| DV-2: Teammate occlusion | No teammate shadow cone logic exists | Stage 1, per OQ-1 |
| DV-3: Cross-platform Fixed64 determinism | Fixed64 Math Library Spec #9 not written | Stage 5+ migration |
| DV-4: Weather range reduction | Environmental system not specified | Stage 2 |
| DV-5: AI-driven movement validation | Decision Tree not yet specified | Stage 1 integration |

---

## 5.17 Section Summary

| Group | Test IDs | Count |
|-------|---------|-------|
| FoV Unit Tests | FOV-001 to FOV-008 | 8 |
| Occlusion Unit Tests | OCC-001 to OCC-013 | 13 |
| Recognition Latency Unit Tests | LR-001 to LR-009 | 9 |
| Blind-Side / Shoulder Check Unit Tests | SC-001 to SC-008 | 8 |
| Ball Perception Unit Tests | BP-001 to BP-006 | 6 |
| Pressure Scalar Unit Tests | PS-001 to PS-005 | 5 |
| Forced Refresh Unit Tests | FR-001 to FR-010 | 10 |
| Snapshot Assembly Unit Tests | SNAP-001 to SNAP-010 | 10 |
| Determinism Unit Tests | DET-001 to DET-004 | 4 |
| **Unit Tests Total** | | **73** |
| Integration Tests | IT-AM-001 to IT-FULL-006 | 12 |
| Balance Tests | BAL-001 to BAL-003 | 3 |
| Performance Tests | PERF-001 to PERF-004 | 4 |
| **Grand Total** | | **92** |

Minimum requirements per outline §5.1/5.2: ≥ 40 unit + ≥ 12 integration.
Delivered: 73 unit (1.8× minimum) + 12 integration (meets target) + 7 supplementary.

---

*End of Section 5 — Perception System Specification #7*
*Tactical Director — Specification #7 of 20 | Stage 0: Physics Foundation*
