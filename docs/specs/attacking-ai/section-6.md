# Attacking AI Specification #15 — Section 6: Performance Analysis and Budgets

**Created:** May 17, 2026
**Last Updated:** May 17, 2026 (v0.1 — initial draft from `outline-detailed.md` v1.1)
**Version:** 0.1
**Status:** DRAFT
**Source:** `outline-detailed.md` v1.1 (May 17, 2026)

---

## 6.1 Constant Catalogue

All constants live in `AttackingAIConstants.cs` per KD-14 / FR-AT-030.
No constants in any other `src/AttackingAI/` file. Constants below are
tagged and have proposed values; full derivations are in Appendix A.

`ATTACK_DWELL_TICKS` is promoted from `[EST]` to `[GT]` here — see
Appendix A §A.1 for the derivation.

### 6.1.1 Phase Gating and Pool Construction

| Constant | Tag | Value | Purpose |
|---|---|---|---|
| `PITCH_LENGTH_M` | `[CROSS: #1 §1.2]` | 105.0 m | X-axis pitch length; corner-origin |
| `PITCH_WIDTH_M` | `[CROSS: #1 §1.2]` | 68.0 m | Y-axis pitch width; corner-origin |
| `HALF_LINE_X` | `[CROSS: #1 §1.2]` | 52.5 m | Midfield line x-coordinate |

### 6.1.2 Role Assignment

| Constant | Tag | Value | Purpose |
|---|---|---|---|
| `MAX_RUNNERS` | `[GT]` | 2 | Anti-chaos: max simultaneous RUNNER roles (POSSESSION profile baseline; overridden per profile — see §6.1.5) |
| `MIN_WEAK_SIDE_AGENT_THRESHOLD` | `[GT]` | 4 | Minimum pool size required before a WEAK_SIDE agent is assigned (below this, pool is too small to dedicate an agent to the weak side) |

### 6.1.3 Run Parameter Generation

| Constant | Tag | Value | Purpose |
|---|---|---|---|
| `BASE_RUN_DEPTH_M` | `[GT]` | 15.0 m | Base run target depth before profile multiplier; output clamped to [5.0, 40.0] |
| `LATERAL_SCALE` | `[GT]` | 0.8 | Scale factor converting `formationSlot.lateralPct` deviation to lateral run offset; < 1 so runs stay slightly narrower than agent's baseline lane position |
| `BASE_RUN_TRIGGER_DELAY_TICKS` | `[GT]` | 3 | Base run trigger delay in 10 Hz ticks before profile multiplier; minimum 1 tick enforced by `max(1, round(...))` |
| `FINAL_THIRD_X_M` | `[DERIVED]` | 70.0 m | Final-third threshold for the team attacking x=105; formula: `PITCH_LENGTH_M × 2/3 = 105 × 2/3`; see Appendix A §A.2 |

### 6.1.4 Support, Width-Holding, and Weak-Side

| Constant | Tag | Value | Purpose |
|---|---|---|---|
| `SUPPORT_RADIUS_M` | `[GT]` | 12.0 m | Base radius within which agents are SUPPORT_BALL candidates (before profile multiplier); units: m |
| `MIN_WIDTH_HOLDERS` | `[GT]` | 2 | Minimum agents holding near-touchline width per tick (HOLD_WIDTH + WEAK_SIDE counted on near side) |
| `TOUCHLINE_HOLD_DIST_M` | `[GT]` | 4.0 m | Distance from the nearest touchline for a HOLD_WIDTH target position (NOT an absolute Y value; formula in §3.6 derives absolute Y from this distance and ball side) |
| `WEAK_SIDE_FAR_Y_M` | `[GT]` | 8.0 m | Distance from the weak-side touchline for the WEAK_SIDE agent target; formula in §3.7 |
| `WEAK_SIDE_DEPTH_OFFSET_M` | `[GT]` | 5.0 m | X-offset toward the opponent goal for the WEAK_SIDE agent target position |

### 6.1.5 Team-Style Profile Multipliers

Profile multipliers are applied as constant scale factors in §3.4 (run depth,
timing) and §3.5 (support radius). The algorithm is identical across profiles;
only the values of these constants differ (KD-8 / KD-12 — no enum branching).

At Stage 0, all teams use the POSSESSION-profile constant values as the
default. Stage 1 wires real team-style selection.

| Constant | Tag | POSSESSION | DIRECT | COUNTER_ATTACK | Purpose |
|---|---|---|---|---|---|
| `DEPTH_MULT_POSSESSION` / `_DIRECT` / `_COUNTER` | `[GT]` | 0.8 | 1.4 | 1.6 | `depthOffset_m` scale factor |
| `TIMING_MULT_POSSESSION` / `_DIRECT` / `_COUNTER` | `[GT]` | 1.2 | 0.7 | 0.5 | `runTriggerTick` delay scale factor |
| `SUPPORT_MULT_POSSESSION` / `_DIRECT` / `_COUNTER` | `[GT]` | 1.3 | 0.8 | 0.5 | `SUPPORT_RADIUS_M` scale factor |
| `MAX_RUNNERS_POSSESSION` / `_DIRECT` / `_COUNTER` | `[GT]` | 1 | 3 | 4 | Per-profile MAX_RUNNERS override |
| `TRANSITION_HOLD_TICKS` | `[GT]` | 5 (POSSESSION/DIRECT) | — | 0 (COUNTER_ATTACK) | Ticks to hold attack directive after possession loss (COUNTER: instant recovery) |

### 6.1.6 Overload Detection

| Constant | Tag | Value | Purpose |
|---|---|---|---|
| `OVERLOAD_COUNT` | `[GT]` | 3 | Minimum agents in flank corridor (excluding WEAK_SIDE) to declare overload |
| `OVERLOAD_ZONE_WIDTH_M` | `[GT]` | 20.0 m | Y-half-width of overload detection corridor: `|agentY − ballY| ≤ OVERLOAD_ZONE_WIDTH_M` |

### 6.1.7 Anti-Chaos Invariants

| Constant | Tag | Value | Purpose |
|---|---|---|---|
| `MIN_SUPPORT_AGENTS` | `[GT]` | 1 | Anti-chaos: minimum combined SUPPORT_BALL + HOLD_WIDTH agents per tick |
| `OWN_HALF_RUN_BLOCK_M` | `[GT]` | 5.0 m | Anti-chaos: run target must not be more than this distance past the half-line into own half; see §3.11 |
| `MAX_INVARIANT_PASSES` | `[GT]` | 3 | Maximum demotion-loop iterations before emitting all-default directive (FR-AT-026) |

### 6.1.8 Hysteresis and Transition

| Constant | Tag | Value | Purpose |
|---|---|---|---|
| `ATTACK_DWELL_TICKS` | `[GT]` | 3 | Dwell ticks before a role/target transition fires; derived in Appendix A §A.1 (promoted from `[EST]`) |

### 6.1.9 Domain Tag

| Constant | Tag | Value | Purpose |
|---|---|---|---|
| `DOMAIN_TAG_ATTACKING_AI` | `[CROSS-PENDING]` | `0x1B` | RNG domain tag for stochastic tie-breaks in `DeterministicRngService`; pending ERR-015-001 allocation in #16 §3.4 |

### 6.1.10 Test Acceptance Criteria Constants

| Constant | Tag | Value | Purpose |
|---|---|---|---|
| `DANGER_ZONE_MAX_DIST_M` | `[GT]` | 20.0 m | Max distance to goal center for dangerous-zone surrogate metric (§5.7) |
| `DANGER_ZONE_CORRIDOR_HW_M` | `[GT]` | 10.16 m | Half-width of dangerous zone (§5.7); derived from penalty-area half-width; see Appendix A §A.3 |
| `DIRECT_RUN_COUNT_DELTA` | `[GT]` | 15 | Min additional RUNNER assignments per 90-min match in DIRECT vs. POSSESSION profile (§5.8) |
| `COUNTER_MAX_HOLD_TICKS` | `[GT]` | 0 | Max mean `transitionHoldTick` for COUNTER_ATTACK per possession-loss event (§5.8) |

### 6.1.11 Constant Count Summary

| Category | Count | Tags |
|---|---|---|
| `[CROSS: #1 §1.2]` | 3 | `PITCH_LENGTH_M`, `PITCH_WIDTH_M`, `HALF_LINE_X` |
| `[DERIVED]` | 1 | `FINAL_THIRD_X_M` |
| `[CROSS-PENDING]` | 1 | `DOMAIN_TAG_ATTACKING_AI` |
| `[GT]` | 33 | All remaining constants |
| `[EST]` | 0 | None (ATTACK_DWELL_TICKS promoted to `[GT]` in this draft) |
| **Total** | **38** | — |

---

## 6.2 Hot Path Enumeration (binding to #18 KD-10)

Per-tick computational steps and their complexity, for N = pool size
(N ≤ 10 off-ball agents).

| Step | Module | Complexity | Operations |
|---|---|---|---|
| Pool construction | `AttackingPoolBuilder` | O(N) | N comparisons (GK + carrier filter) |
| Hysteresis check | `AttackHysteresis` | O(N) | N counter reads + comparisons |
| Role assignment | `RoleAssigner` | O(N) | N agents × O(1) role priority evaluation |
| Run parameter generation | `RoleAssigner` | O(R) ≤ O(N) | R ≤ MAX_RUNNERS × 8 float ops (trig: cos/sin evaluated once per tick, not per agent) |
| Support radius scan | `SupportHeuristic` | O(N) | N Euclidean distance computations (2 subtracts + 1 multiply + 1 compare) |
| Width-holding | `WidthHolder` | O(N log N) | 1 sort by |Y − touchline|; N ≤ 10 so constant-bounded |
| Weak-side | `WeakSideController` | O(N) | N Y-deviation comparisons |
| Overload detection | `OverloadDetector` | O(N) | N Y-distance comparisons |
| Anti-chaos enforcement | `InvariantEnforcer` | O(N × MAX_INVARIANT_PASSES) = O(3N) | ≤ 30 comparisons + ≤ 30 role reassignments in worst case |
| Digest write | (orchestrator) | O(N) | N `AttackIntent` structs copied to digest buffer |

**Total worst-case per tick (N=10):** ≈ 120 floating-point operations +
30 comparison operations + 1 sort (≤ 40 comparisons for N=10). Bounded
by O(N) with constant factors ≤ 40 per operation type.

This is substantially cheaper than Defensive AI #14 (which includes
opponent threat scoring, a displacement-cost matrix O(N²), and tackle
intent evaluation) and comparable to Pressing AI #13.

`cos(teamAttackAngle)` and `sin(teamAttackAngle)` are evaluated **once
per tick** (not once per RUNNER agent) and cached for the tick duration,
since `teamAttackAngle` is a match-half constant. This avoids N trig calls
per tick.

---

## 6.3 Per-Tick Budget (binding to #18 KD-9)

**Target:** ≤ 0.10 ms per 10 Hz tick.

**Reference host:** Ryzen 7 5800X @ 4.5 GHz, single thread, Mono
backend, Unity 2022.3 LTS — the same host used by Pressing AI #13 and
Defensive AI #14 per-tick budget anchors. This allows direct comparison
across the Phase C tactical-AI chain.

**Caveat:** The certification-platform host pin (`certification-platform.md`
Stage-0 row) overrides this estimate once the lead developer populates the
concrete platform configuration. Until then, the Ryzen 7 5800X reference
is a provisional anchor only. The 0.10 ms budget is not activated as a
hard gate until the reference host is pinned (same caveat as #18 FR-PO-052
and #14 §6.3).

**Rationale:** The worst-case complexity analysis in §6.2 gives ≈ 150
operations per tick. At 4.5 GHz with a throughput of ≈ 1 operation/ns
(conservative for scalar float arithmetic), worst-case execution ≈ 150 ns
≈ 0.00015 ms — three orders of magnitude under budget. Memory access is
the likely bottleneck; all working data (N=10 agents × struct sizes ≈ 2 KB)
fits in L1 cache, so cache misses are unlikely.

---

## 6.4 Per-Frame Budget

**N/A — no per-frame work.**

`AttackingAI` produces outputs only on the 10 Hz tactical loop (KD-2).
No 60 Hz per-frame computation. Per-frame budget = 0 µs.

---

## 6.5 Memory Footprint (Stage 1 estimate)

Approximate memory per team per match (worst-case N=10 off-ball agents):

| Data | Size (estimate) | Notes |
|---|---|---|
| `AttackDirective` | 16 bytes | team EntityId (4B) + overloadActive (1B) + overloadFlank (1B) + transitionHoldTick (4B) + padding (6B) |
| `AttackIntent[10]` | 10 × 28 = 280 bytes | agent EntityId (4B) + role (4B) + RunParameters? (16B: 12B struct + 1B null flag + 3B padding) + validThroughTick (4B) |
| `RunParameters[10]` | 10 × 12 = 120 bytes | Included in `AttackIntent[]` above |
| `AttackHysteresisState[10]` | 10 × 16 = 160 bytes | currentRole (4B) + dwellCounter (4B) + candidateRole (4B) + candidateDwell (4B) |
| `TransitionHoldState` | 8 bytes | transitionHoldTick (4B) + prevPhase (4B) |
| **Total (per team)** | **≈ 464 bytes ≈ 0.5 KB** | — |

Two teams = ≈ 1 KB. Substantially under the #18 §3.7 hot-path allocation
budget. All structs are stack-allocated; no heap allocation in the hot
path (FR-AT-030 / #18 §3.7 zero-alloc rule).

---

## 6.6 Version History

| Version | Date | Author | Summary |
|---|---|---|---|
| 0.1 | May 17, 2026 | AI agent (claude-sonnet-4-6 / draft-attacking-ai-spec) | Initial draft from `outline-detailed.md` v1.1. §6.1–§6.6 authored. 38 constants catalogued (33 `[GT]` + 3 `[CROSS]` + 1 `[DERIVED]` + 1 `[CROSS-PENDING]`); `ATTACK_DWELL_TICKS` promoted `[EST]` → `[GT]` with Appendix A derivation. Hot-path analysis and budget anchor documented. |
| 0.2 | May 18, 2026 | AI agent (claude-sonnet-4-6) | §6.5 memory-footprint corrections: `AttackHysteresisState` corrected to 16 bytes/struct (was 8; now includes all 4 fields: currentRole, dwellCounter, candidateRole, candidateDwell); `AttackIntent` corrected to 28 bytes/struct (was 24; now includes agent EntityId 4B + RunParameters? as Nullable<T>=16B); `TransitionHoldState` corrected to 8 bytes (was 12; transitionHoldTick 4B + prevPhase 4B, no padding needed); total per-team ≈ 464 bytes. |
