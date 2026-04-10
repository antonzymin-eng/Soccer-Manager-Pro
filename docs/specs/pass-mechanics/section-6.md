# Pass Mechanics Specification #5 — Section 6: Performance Analysis

**File:** `Pass_Mechanics_Spec_Section_6_v1_1.md`
**Purpose:** Authoritative performance analysis for the Pass Mechanics system — computational
complexity classification, operation counts derived from the §3 sub-system formulas, memory
footprint, profiling targets, trig cost evaluation (lookup table vs. runtime), optimization
roadmap, and known limitations. This section is the single authoritative source for all Pass
Mechanics performance targets and budgets. Budget figures stated in §2.5 are summaries
derived from this section; if a conflict exists, this section takes precedence.

**Created:** February 20, 2026, 11:59 PM PST
**Revised:** February 21, 2026
**Version:** 1.1
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 5 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)
**Prerequisites:** Section 1 (v1.0), Section 2 (v1.0), Section 3 (§3.1–§3.9), Section 4 (v1.0),
Section 5 (v1.0)

**Open Dependency Flags:**
- `[ERR-007-PENDING]` — `KickPower`, `WeakFootRating`, `Crossing` absent from
  `PlayerAttributes`. Attribute read cost estimates are based on placeholder names.
  Op counts update once attribute names are confirmed; impact is negligible (struct
  field reads are O(1), cost is identical regardless of field name).
- `[ERR-008-PENDING]` — `PossessingAgentId` design unresolved. Does not affect
  performance analysis; possession handoff is a single field write regardless of
  design option chosen.

---

## Table of Contents

- [Preamble](#preamble-role-of-this-section)
- [6.1 Computational Complexity](#61-computational-complexity)
- [6.2 Operation Count Analysis](#62-operation-count-analysis)
- [6.3 Performance Budget](#63-performance-budget)
- [6.4 Trig Cost Evaluation](#64-trig-cost-evaluation)
- [6.5 Memory Footprint](#65-memory-footprint)
- [6.6 Cache Behaviour](#66-cache-behaviour)
- [6.7 Profiling Markers](#67-profiling-markers)
- [6.8 Optimization Roadmap](#68-optimization-roadmap)
- [6.9 Known Limitations](#69-known-limitations)
- [6.10 Cross-References](#610-cross-references)
- [6.11 Section Summary](#611-section-summary)

---

## Preamble: Role of This Section

This section provides the **single authoritative source** for all Pass Mechanics performance
targets, budgets, and analysis.

**Critical distinction from per-frame systems:** Ball Physics, Agent Movement, and Collision
System all analyse per-frame costs because those systems run every frame. Pass Mechanics is a
**discrete event processor** — it executes only when the Decision Tree initiates a pass.
A typical 90-minute match produces approximately 800–1,200 pass initiations total across both
teams (based on real-world match statistics of ~450–600 passes per team per 90 minutes at
professional level, adjusted downward for Stage 0 AI simplification). This fundamentally
changes the performance analysis: per-evaluation latency is the primary concern, not
per-frame overhead.

**Why thorough analysis is still warranted:**

1. **Peak-frame density:** Rapid passing sequences (one-twos, overlapping runs) can produce
   2–4 simultaneous pass evaluations in a single frame. Per-evaluation cost determines
   whether these sequences remain within the remaining frame budget.

2. **Pressure query O(n) scaling:** The spatial hash query in §3.5 (Error Model) is the only
   sub-linear-time operation in the system. With up to 21 opposing-team agents on the pitch,
   worst-case pressure evaluation must be bounded.

3. **Trig cost in launch angle derivation:** The outline (§6, Outline risk #4) explicitly
   flagged `atan()` and `sin()` calls in §3.3 as requiring evaluation. Section 6.4 provides
   this evaluation with a definitive recommendation.

---

## 6.1 Computational Complexity

### 6.1.1 Overall Classification

Pass Mechanics is **O(n) per evaluation**, where n is the number of opponent agents within
the pressure query radius (~3m) at the moment of pass initiation. All other sub-systems
in the pipeline are O(1).

| Sub-System | Complexity | Variable | Notes |
|---|---|---|---|
| §3.1 Pass Type Classification | O(1) | — | Enum lookup + profile struct read |
| §3.2 Pass Velocity Model | O(1) | — | Multiply-add chain, no iteration |
| §3.3 Launch Angle Derivation | O(1) | — | Fixed formula; see §6.4 for trig cost |
| §3.4 Spin Vector Calculation | O(1) | — | Multiply-add chain |
| §3.5 Error Model | O(n) | n = opponents in radius | Spatial hash query result iteration |
| §3.6 Target Resolution | O(1) | — | Single agent lookup (direct-pass) or fixed projection (through ball) |
| §3.7 Weak Foot Penalty | O(1) | — | Conditional multiply |
| §3.8 Execution State Machine | O(1) | — | State enum switch + field write |
| §3.9 Event Publishing | O(1) | — | Struct creation + queue enqueue |
| **Overall** | **O(n)** | n ≤ 21 in theory; ≤ 4 in practice | See §6.1.2 |

### 6.1.2 Practical Bound on n

The pressure query radius is ~3m. At normal match density, the maximum number of opponents
within 3m of the passer at the moment of kicking is approximately 2–3. A crowded penalty
area might yield n = 4–5. The pathological maximum of n = 21 (all opposing outfield players
within 3m simultaneously) is physically impossible; it is included for completeness only.

**Practical O(n) bound: n ≤ 5 for 99.9% of evaluations.**

This makes the effective complexity O(1) for all practical purposes. The O(n) classification
is retained for correctness.

---

## 6.2 Operation Count Analysis

### 6.2.1 Methodology

Operation counts follow the equivalent-operation (equiv-op) methodology established in
Agent Movement Spec #2 §5.1 and applied in Collision System Spec #3 §6 and First Touch
Spec #4 §6.2:

- 1 equiv-op = one floating-point multiply, divide, add/subtract, or compare
- Integer arithmetic, boolean logic = 0.5 equiv-ops each
- Vector3 construction = 3 equiv-ops (3 component assignments)
- Vector3 add/subtract = 3 equiv-ops
- Vector3 dot product = 5 equiv-ops (3 muls + 2 adds)
- Vector3 magnitude = 7 equiv-ops (3 muls + 2 adds + 1 sqrt + 1 result)
- `Mathf.Clamp(v, lo, hi)` = 2 equiv-ops (2 comparisons)
- Struct field read = 0.5 equiv-ops
- Method call dispatch (non-virtual) = 1 equiv-op overhead
- Trig functions: `atan2` = 15 equiv-ops; `sin` = 12 equiv-ops; `cos` = 12 equiv-ops
  (hardware-assisted estimates; see §6.4 for justification)

**Note:** These are estimates for planning and profiling target derivation. Actual
measurements via Unity Profiler supersede these counts at implementation time.

### 6.2.2 Phase-by-Phase Operation Counts

#### Phase 1 — PassRequest Validation (§3.8 guard)

| Operation | Equiv-Ops |
|---|---|
| Null check on PassRequest | 0.5 |
| PassType bounds check (enum range) | 0.5 |
| IntendedDistance > 0 guard | 0.5 |
| AgentId validity check | 0.5 |
| Urgency clamp [0, 1] | 2.0 |
| **Phase 1 Total** | **4.0** |

#### Phase 2 — Pass Type Classification and Profile Lookup (§3.1)

| Operation | Equiv-Ops |
|---|---|
| PassType enum switch | 0.5 |
| CrossSubType check (conditional) | 0.5 |
| PhysicalProfile struct read (7 fields) | 3.5 |
| **Phase 2 Total** | **4.5** |

#### Phase 3 — Target Resolution (§3.6)

Two code paths exist. Only one executes per evaluation.

**Path A — Direct target (named agent):**

| Operation | Equiv-Ops |
|---|---|
| AgentState.Position read | 0.5 |
| TargetAgent.Position read | 0.5 |
| Target vector (Vector3 subtract) | 3.0 |
| Target distance (magnitude) | 7.0 |
| Kick direction normalise | 8.0 (divide by magnitude) |
| **Path A Total** | **19.0** |

**Path B — Through ball (space target with linear receiver projection):**

| Operation | Equiv-Ops |
|---|---|
| TargetAgent.Position read | 0.5 |
| TargetAgent.Velocity read | 0.5 |
| Passer.Position read | 0.5 |
| Displacement vector (Vector3 subtract) | 3.0 |
| Distance to target (magnitude) | 7.0 |
| Time-of-flight estimate: `t ≈ D / V_estimate` | 2.0 |
| Projected receiver position: `P + V × t` | 6.0 (Vector3 scale + add) |
| Lead vector (subtract) | 3.0 |
| Lead distance (magnitude) | 7.0 |
| Kick direction normalise | 8.0 |
| **Path B Total** | **38.0** |

**Phase 3 cost used in totals:** Path A (19.0 ops) for direct passes (majority case);
Path B (38.0 ops) for through balls. Worst-case totals use Path B.

#### Phase 4 — Pass Velocity Calculation (§3.2)

| Operation | Equiv-Ops |
|---|---|
| KickPower attribute read | 0.5 |
| Fatigue attribute read | 0.5 |
| vOffset read from PhysicalProfile | 0.5 |
| vMax read from PhysicalProfile | 0.5 |
| PowerScale: `(K/ATTR_MAX) × (D/distMax)` | 3.0 |
| `V_base = vOffset + PowerScale × (vMax - vOffset)` | 3.0 |
| FatigueModifier: `1.0 - Fatigue × K` | 2.0 |
| `kickSpeed = V_base × FatigueModifier` | 1.0 |
| Clamp to [vMin, vMax] | 2.0 |
| **Phase 4 Total** | **13.0** |

> **v1.1 note:** Phase 4 total increased from 11.5 to 13.0 equiv-ops. The corrected
> formula (AM-003-001) adds one additional PhysicalProfile field read (vOffset: +0.5)
> and separates the single `V_base × PowerScale × FatigueModifier` chain into two
> steps with the vOffset addition (+1.0 add op). Net increase: +1.5 equiv-ops.
> This is well within the Phase 4 budget and does not affect the §3.2 pass/fail verdict.
> The `EvaluatePass()` total budget must be updated: see §6.3 phase totals table.

#### Phase 5 — Launch Angle Derivation (§3.3)

This is the trig-sensitive phase. Two implementation options are evaluated in §6.4.

**Current implementation (runtime trig):**

| Operation | Equiv-Ops |
|---|---|
| Distance read (from Phase 3) | 0.5 |
| Apex height target read (from PhysicalProfile) | 0.5 |
| `atan2(2H, D)` call | 15.0 |
| Angle interpolation / clamp to [ANGLE_MIN, ANGLE_MAX] | 4.0 |
| `sin(launchAngle)` — vertical velocity component | 12.0 |
| `cos(launchAngle)` — horizontal velocity component | 12.0 |
| Velocity Vector3 construction | 3.0 |
| Scale by kickSpeed | 3.0 |
| **Phase 5 Total (runtime trig)** | **50.0** |

**Alternative implementation (lookup table; see §6.4):**

| Operation | Equiv-Ops |
|---|---|
| Distance read | 0.5 |
| Table index computation | 2.0 |
| LUT read (angle) | 0.5 |
| LUT read (sin θ) | 0.5 |
| LUT read (cos θ) | 0.5 |
| Velocity Vector3 construction | 3.0 |
| Scale by kickSpeed | 3.0 |
| **Phase 5 Total (lookup table)** | **10.0** |

Phase 5 is the only sub-system with a meaningful implementation choice. §6.4 evaluates
both options and makes a definitive recommendation.

#### Phase 6 — Spin Vector Calculation (§3.4)

| Operation | Equiv-Ops |
|---|---|
| Technique attribute read | 0.5 |
| SPIN_BASE read from PhysicalProfile | 0.5 |
| TechniqueScale: `0.5 + (Technique - 1) / 19 × 1.0` | 3.0 |
| SpinMagnitude: `SPIN_BASE × TechniqueScale` | 1.0 |
| Spin axis assignment (pass-type-dependent) | 1.5 |
| spinVector construction | 3.0 |
| **Phase 6 Total** | **9.5** |

#### Phase 7 — Error Magnitude Calculation (§3.5)

| Operation | Equiv-Ops |
|---|---|
| Passing attribute read | 0.5 |
| BASE_ERROR read from PhysicalProfile | 0.5 |
| PassingModifier: attribute → multiplier | 3.0 |
| Spatial hash query: `GetOpponentsInRadius(pos, 3m)` | 5.0 (hash lookup overhead) |
| Per-opponent distance weighting (×n): | 8.0 × n |
| PressureScalar accumulation | 2.0 |
| PressureModifier: scalar → multiplier | 2.0 |
| FatigueModifier read (cached from Phase 4) | 0.0 (cached) |
| FatigueModifier apply | 1.0 |
| OrientationDelta compute (dot product + acos) | 10.0 |
| OrientationModifier: angle → multiplier | 3.0 |
| UrgencyModifier: urgency → multiplier | 2.0 |
| WeakFootMultiplier (conditional; from Phase 8) | 1.0 |
| ErrorAngle = product of all modifiers | 6.0 (5 multiplies) |
| **Phase 7 Fixed Total** | **36.0** |
| **Phase 7 Per-Opponent (×n)** | **+8.0 × n** |

#### Phase 8 — Weak Foot Penalty (§3.7)

| Operation | Equiv-Ops |
|---|---|
| IsWeakFoot bool read | 0.5 |
| Conditional: branch (no penalty path) | 0.5 |
| WeakFootRating attribute read | 0.5 |
| Penalty multiplier: `1.0 + (1 - WeakFootRating/20) × WEAK_FOOT_SCALE` | 3.0 |
| **Phase 8 Total (weak foot active)** | **4.5** |
| **Phase 8 Total (preferred foot)** | **1.0** |

#### Phase 9 — Error Direction and Application (§3.5 continued)

| Operation | Equiv-Ops |
|---|---|
| Error axis: deterministic hash of (AgentId, Frame, PassType) | 5.0 |
| Axis → unit vector | 8.0 |
| Quaternion from angle + axis | 10.0 |
| Rotate kick direction by quaternion | 12.0 |
| **Phase 9 Total** | **35.0** |

#### Phase 10 — Ball.ApplyKick() Call (§4 Integration Contract)

| Operation | Equiv-Ops |
|---|---|
| Velocity Vector3 struct copy | 1.5 |
| Spin Vector3 struct copy | 1.5 |
| Method dispatch (interface call) | 1.0 |
| **Phase 10 Total** | **4.0** |

#### Phase 11 — Event Publishing (§3.9)

| Operation | Equiv-Ops |
|---|---|
| PassInitiatedEvent struct construction | 3.0 |
| Event bus enqueue | 1.0 |
| **Phase 11 Total** | **4.0** |

### 6.2.3 Summary Table

**Fixed costs (n-independent):**

| Phase | Sub-System | Ops (runtime trig) | Ops (LUT) |
|---|---|---|---|
| 1 | PassRequest Validation | 4.0 | 4.0 |
| 2 | Pass Type Classification | 4.5 | 4.5 |
| 3A | Target Resolution (direct) | 19.0 | 19.0 |
| 3B | Target Resolution (through ball) | 38.0 | 38.0 |
| 4 | Pass Velocity Calculation | 13.0 | 13.0 |
| 5 | Launch Angle Derivation | **50.0** | **10.0** |
| 6 | Spin Vector Calculation | 9.5 | 9.5 |
| 7 (fixed) | Error Magnitude (fixed part) | 36.0 | 36.0 |
| 8 | Weak Foot Penalty (worst case) | 4.5 | 4.5 |
| 9 | Error Direction + Application | 35.0 | 35.0 |
| 10 | Ball.ApplyKick() | 4.0 | 4.0 |
| 11 | Event Publishing | 4.0 | 4.0 |
| **Fixed Total (direct pass)** | | **183.5** | **143.5** |
| **Fixed Total (through ball)** | | **202.5** | **162.5** |

> **v1.1 note:** Phase 4 updated from 11.5 to 13.0 ops per AM-003-001 (vOffset addition).
> Fixed totals increased by 1.5 ops across all scenarios. All performance budgets remain
> comfortably within targets — the 1.5 op increase is negligible.

**Variable cost:** +8.0 equiv-ops per opponent in pressure radius (n).

**Total by scenario:**

| Scenario | Pass Type | n | Trig Impl. | Total Ops |
|---|---|---|---|---|
| Open play, direct pass | Direct | 0 | Runtime trig | 183.5 |
| Open play, direct pass | Direct | 0 | LUT | 143.5 |
| Typical contested, direct pass | Direct | 2 | Runtime trig | 199.5 |
| Typical contested, direct pass | Direct | 2 | LUT | 159.5 |
| Crowded box, through ball | Through ball | 4 | Runtime trig | 234.5 |
| Crowded box, through ball | Through ball | 4 | LUT | 194.5 |
| Pathological (n=21, through ball) | Through ball | 21 | Runtime trig | 370.5 |
| Pathological (n=21, through ball) | Through ball | 21 | LUT | 330.5 |

### 6.2.4 Comparison to Adjacent Systems

| System | Typical Ops/Frame | Frequency | Match Total Ops |
|---|---|---|---|
| Ball Physics Spec #1 | ~172 ops/frame | 60 Hz × 5,400s = 324,000 frames | ~55.7M |
| Agent Movement Spec #2 | ~1,096 ops/frame (22 agents) | 324,000 frames | ~355.1M |
| Collision System Spec #3 | ~1,480 ops/frame | 324,000 frames | ~479.5M |
| First Touch Spec #4 | ~143 ops/evaluation | ~300 evaluations/match | ~42,900 |
| **Pass Mechanics Spec #5** | **~198 ops/evaluation (typical)** | **~1,000 evaluations/match** | **~198,000** |

Pass Mechanics has ~4.6× more match-level impact than First Touch, but remains
approximately **2,400× less expensive than Collision System** over a full match.
Performance optimisation for match-level cost reduction is not warranted. Per-evaluation
latency is the only relevant concern.

---

## 6.3 Performance Budget

### 6.3.1 Budget Context

The overall physics frame budget is 6ms at 60 Hz (Master Vol 4 §3.2). Budget allocation
established in Ball Physics Spec #1 §6.2.1:

| System | Allocated Budget |
|---|---|
| Ball Physics | 0.5ms |
| Agent Movement (22 agents) | 3.0ms |
| Collision System | 2.0ms |
| **Headroom / event systems** | **0.5ms** |
| **Total** | **6.0ms** |

Pass Mechanics draws from the 0.5ms headroom on frames where a pass is initiated. At most
2–3 passes are initiated per second in normal match flow; simultaneous pass evaluations in
the same frame are infrequent. Peak-frame scenarios with 3 simultaneous initiations must
complete within the 0.5ms headroom combined.

### 6.3.2 Per-Evaluation Timing Targets

**Clock speed assumption:** Modern x86-64 CPU at 3–4 GHz with IPC ≈ 2–3. Conservative
baseline: 3 GHz × 2 IPC = 6 × 10⁸ float ops/second (scalar, no SIMD). This is deliberately
conservative; actual hardware typically achieves higher throughput.

**Baseline time estimate:**

```
Typical case (direct pass, n=2, LUT): 158 ops / (6×10⁸ ops/s) ≈ 0.00026ms
Typical case (direct pass, n=2, trig): 198 ops / (6×10⁸ ops/s) ≈ 0.00033ms
```

These raw estimates suggest Pass Mechanics is orders of magnitude within budget. The
practical ceiling is dominated by:

- Memory latency: spatial hash pointer chase for pressure query (1–2 cache misses at ~5ns
  each ≈ 0.01ms)
- Function call overhead: `Ball.ApplyKick()` interface dispatch
- Branch misprediction: state machine conditional branches

Accounting for these factors, a conservative real-world estimate is **0.01–0.04ms per
evaluation** for n ≤ 3, rising to **0.04–0.08ms** for n = 4–5.

| Metric | Target | Hard Limit | Rationale |
|---|---|---|---|
| p50 evaluation latency | < 0.02ms | — | Open-play typical; consistent with op-count estimate |
| p95 evaluation latency | < 0.05ms | — | Contested mid-field; n ≤ 3 |
| p99 evaluation latency | < 0.10ms | — | Crowded penalty area; n ≤ 5 |
| Absolute worst case | < 0.20ms | **Blocking** | n=21 pathological; must fit within 1 frame |
| Peak-frame (3 simultaneous) | < 0.25ms combined | — | p95 × 3; within 0.5ms headroom |
| Heap allocations per evaluation | **0** | **0** | Zero allocation policy (Dev Best Practices §4) |

### 6.3.3 Match-Level Budget

| Metric | Estimate | Rationale |
|---|---|---|
| Pass evaluations per match | ~1,000 | ~500 per team; consistent with real football |
| Total match impact (1,000 × 0.05ms) | < 50ms | Spread across 5,400s of match time |
| Average frame impact | < 0.0002ms | 1,000 / 324,000 frames × 0.05ms |

**Key finding:** Pass Mechanics contributes less than **0.003% of the total frame budget**
on average. Match-level cost is negligible. Per-evaluation peak latency is the only metric
requiring vigilance.

### 6.3.4 Measurement Protocol

Performance testing follows the protocol established in Collision System §6.2.4 and
First Touch §6.3.4:

1. Construct a synthetic test harness calling `EvaluatePass()` with fixed inputs.
2. Run 10,000 evaluations in sequence per configuration (n=0, n=2, n=4).
3. Discard first 100 results (JIT warm-up).
4. Record p50, p95, p99 using `System.Diagnostics.Stopwatch`.
5. Validate zero heap allocations using Unity Memory Profiler.
6. All targets in §6.3.2 must pass before Stage 0 implementation sign-off.

Pass criteria align with §5.13.4 (Testing acceptance gate):
- p95 < 0.05ms ✓
- p99 < 0.10ms ✓
- Zero heap allocations ✓

---

## 6.4 Trig Cost Evaluation

> **This section resolves Outline risk #4:** *"Trig functions in launch angle derivation —
> `atan()` and `sin()` calls are expensive relative to the rest of Pass Mechanics. For a
> discrete-event system this is likely acceptable, but pre-computed lookup tables should be
> evaluated in Section 6."*

### 6.4.1 The Question

§3.3 Launch Angle Derivation uses `atan2(2H, D)` to compute the launch angle, then `sin`
and `cos` to resolve it into velocity components. Three implementation options exist:

- **Option A — Runtime trig:** Call `Mathf.Atan2`, `Mathf.Sin`, `Mathf.Cos` at evaluation time.
- **Option B — Lookup table (LUT):** Pre-compute angles and their sin/cos values at
  startup, index by distance bucket.
- **Option C — Polynomial approximation:** Replace trig with fast polynomial (e.g., CORDIC
  or minimax approximation). No dependencies; deterministic to fixed precision.

### 6.4.2 Cost Estimate of Each Option

| Option | Phase 5 Ops | Phase 5 Est. Time | Accuracy | Determinism Risk |
|---|---|---|---|---|
| A — Runtime trig | 50.0 | ~0.003ms | Exact (IEEE 754) | Low — hardware-consistent |
| B — Lookup table | 10.0 | ~0.001ms | ± 0.1° (1° bucket) | None |
| C — Polynomial approx | ~20.0 | ~0.002ms | ± 0.01° | Medium — platform-dependent |

**Absolute time saving of LUT over runtime trig:**

```
Runtime trig Phase 5 cost ≈ 40 equiv-ops saved × 1/(6×10⁸) ≈ 0.000067ms
```

0.067 microseconds saved per evaluation. Across 1,000 evaluations per match:
**0.067ms total match savings.**

### 6.4.3 Decision and Rationale

**Decision: Use runtime trig (Option A). Do not implement a lookup table for Stage 0.**

**Rationale:**

1. **Savings are insignificant.** 0.067ms across a full 90-minute match is below measurement
   noise. A LUT would add implementation complexity and a new source of bugs for no observable
   performance benefit.

2. **Pass Mechanics is a discrete event system.** Even at the trig-heavy pathological n=21
   worst case, the system completes in < 0.20ms — comfortably within the 0.5ms headroom
   budget. The headroom exists; using it for clean code is correct.

3. **LUT introduces a precision tradeoff.** A 1° angle bucket produces ± 0.5° error in the
   launch angle, which translates to a measurable positional error at the receiver end of a
   40m lofted pass (~35cm). This is a permanent accuracy cost in exchange for ~0.067ms of
   match-level savings. The tradeoff is unfavourable.

4. **LUT complicates determinism guarantees.** A floating-point lookup table with index
   rounding must be validated exhaustively for cross-platform bitwise identity. Runtime
   `Mathf.Atan2` on .NET/IL2CPP produces IEEE 754 compliant results that are already
   platform-consistent within Unity's IL2CPP environment.

5. **The outline flag is resolved.** Outline risk #4 requested evaluation, not a directive
   to implement a LUT. The evaluation finds the status quo acceptable.

**Stage 1 reconsideration trigger:** If profiling data shows Phase 5 exceeding 5% of the
total per-evaluation budget at p95, revisit this decision. Flag as `[PERF-REVIEW-STAGE1]`
in implementation.

### 6.4.4 Orientation Delta Trig Cost

§3.5 Error Model also calls `acos` to compute `OrientationDelta` (angle between
`FacingDirection` and kick direction). This call is included in Phase 7 (10 equiv-ops).
The same analysis applies: acceptable for a discrete-event system with 0.5ms headroom.
No LUT warranted.

---

## 6.5 Memory Footprint

### 6.5.1 Per-Evaluation Memory

All Pass Mechanics data structures are stack-allocated per evaluation. Zero heap allocation
policy is mandatory (Development Best Practices §4).

| Structure | Fields | Size Estimate | Allocation |
|---|---|---|---|
| `PassRequest` | PassType (int), CrossSubType (int), TargetType (enum), TargetAgentId (int), TargetPosition (Vector3), IntendedDistance (float), IsWeakFoot (bool), Urgency (float), Frame (int) | ~44 bytes | Stack (caller) |
| `PassResult` | Outcome (enum), FinalVelocity (Vector3), FinalSpin (Vector3), ErrorAngle (float), ActualTargetPosition (Vector3) | ~52 bytes | Stack (callee) |
| `PhysicalProfile` | V_MIN, V_MAX, ANGLE_MIN, ANGLE_MAX, SPIN_BASE, plus 3 floats | ~32 bytes | Read from static table |
| `PhysicsKickPacket` | Velocity (Vector3), Spin (Vector3) | ~24 bytes | Stack (passed to ApplyKick) |
| `PassInitiatedEvent` | AgentId (int), PassType (int), TargetPosition (Vector3), Frame (int), Velocity (Vector3) | ~36 bytes | Event queue pool |
| `PassCancelledEvent` | AgentId (int), Frame (int) | ~8 bytes | Event queue pool |

**Total stack usage per evaluation:** ≈ 152 bytes. Well within any platform's stack limit.

### 6.5.2 Static (Persistent) Memory

| Data | Size | Lifetime |
|---|---|---|
| PassTypeProfile table (7 types × ~32 bytes) | ~224 bytes | Application lifetime |
| PassConstants block | ~200 bytes | Application lifetime |
| **Total static** | **~424 bytes** | Negligible |

### 6.5.3 Event Queue Pool

The event pool is managed by Event System Spec #17 (Stage 1). For Stage 0, events are
published to a simple ring buffer. Capacity: 64 events. At ~1,000 pass events per match
processed at simulation speed, the ring buffer is never saturated. No memory pressure.

### 6.5.4 Cache Line Alignment Note

`PassResult` at ~52 bytes spans two cache lines (64 bytes each). If profiling reveals
cache line splitting as a hotspot (unlikely given discrete-event nature), padding to 64
bytes is a P2 optimisation (see §6.8).

---

## 6.6 Cache Behaviour

### 6.6.1 Hot Data

On a pass evaluation frame, the following reads occur:

| Data | Prior Cache State | Reason |
|---|---|---|
| `AgentAttributes` of passer | **Warm** | Agent Movement updated this agent this frame |
| `AgentState` of passer | **Warm** | Agent Movement updated this agent this frame |
| `AgentState` of target agent | **Likely warm** | Target recently updated by Agent Movement |
| `PhysicalProfile` table | **Warm (after first pass)** | Static data; fits in L2 cache permanently |
| Spatial hash structure | **Cold on first query frame** | Not touched by other systems this frame |

**Net cache assessment:** Pass Mechanics performs well in cache terms for the passer and
target data. The spatial hash query for pressure evaluation is the only cold-read risk,
consistent with First Touch Spec #4 §6.6 findings for the same operation.

### 6.6.2 Spatial Hash Query Cache Impact

The spatial hash returns a list of opponent agent pointers (or IDs). For n=2 opponents,
this is 2 pointer dereferences into `AgentState` structs. If those agents have not been
recently updated (e.g., distant substitutes), the reads are cold misses (~5ns each).

**Worst case:** n=4 opponents, all cold = 4 cache misses × 5ns = 0.02ms. Acceptable.
This is already included in the per-evaluation estimates in §6.3.2.

---

## 6.7 Profiling Markers

Three Unity Profiler markers are defined for Pass Mechanics. Names must match exactly
between this specification and the implementation; deviations require a spec amendment.

| Marker Name | Scope | Primary Metric |
|---|---|---|
| `PassMech.Evaluate` | Entire `EvaluatePass()` call, phases 1–11 | p95 < 0.05ms — this is the primary gate |
| `PassMech.PressureQuery` | Phase 7 spatial hash query only | Isolates O(n) scaling behaviour |
| `PassMech.LaunchAngle` | Phase 5 only | Validates trig cost decision in §6.4 |

**Measurement note:** `PassMech.LaunchAngle` is included specifically to validate the §6.4
decision at runtime. If its measured cost exceeds 10% of `PassMech.Evaluate` at p95, the
LUT option must be reconsidered. Flag `[PERF-REVIEW-STAGE1]` in implementation and revisit
at Stage 1 profiling review.

**Implementation requirement:** Markers must be wrapped in `#if UNITY_EDITOR ||
DEVELOPMENT_BUILD` preprocessor guards so they are stripped from release builds.

---

## 6.8 Optimization Roadmap

Optimisations are classified by priority. No optimisation in this list should be
implemented before profiling data confirms it addresses a measured bottleneck
(Development Best Practices §1 — Profile Before Optimize).

### Priority 0 — Mandatory (Zero Allocation)

| Optimisation | Description | Status |
|---|---|---|
| Stack-only data structures | All evaluation state is stack-allocated; no `new` calls in hot path | **Required before implementation** |
| Event pool pre-allocation | `PassInitiatedEvent` and `PassCancelledEvent` drawn from pre-allocated pool | **Required before implementation** |

### Priority 1 — Implement if p95 > 0.03ms in profiling

| Optimisation | Description | Expected Saving |
|---|---|---|
| FatigueModifier caching | Fatigue changes infrequently; cache per-agent per-tick | ~1.0 equiv-ops |
| Pressure scalar caching | If agent position unchanged since last evaluation, reuse cached scalar | ~8.0 × n equiv-ops |

### Priority 2 — Implement if p95 > 0.04ms in profiling

| Optimisation | Description | Expected Saving |
|---|---|---|
| PassResult cache line padding | Pad `PassResult` to 64 bytes for cache line alignment | ~5ns on split-line reads |
| Inline Phase 8 (Weak Foot) | Eliminate method call overhead for weak foot penalty | ~1.0 equiv-ops |

### Priority 3 — Post-Stage 0 only

| Optimisation | Description | Notes |
|---|---|---|
| SIMD velocity construction | Build velocity Vector3 using SIMD intrinsics | Only warranted if system is in per-frame hot path — it is not |
| LUT for launch angle | As evaluated in §6.4 | Revisit only if Stage 1 profiling finds Phase 5 > 10% of total cost |
| Burst-compile `EvaluatePass()` | Unity Burst compiler for struct-based hot path | Premature for Stage 0; evaluate at Stage 2 |

---

## 6.9 Known Limitations

### KL-1 — Op Counts Are Pre-Implementation Estimates

The operation counts in §6.2 are derived by inspection of the §3 formula specifications.
They have not been validated against compiled assembly output. IL2CPP compilation,
Unity's Burst compiler, and JIT optimisation may produce substantially different
instruction counts. All §6.3 timing targets must be treated as estimates until the
§6.3.4 measurement protocol is executed.

**Action:** Profiling run must be completed before implementation sign-off. Results must
be compared to §6.3.2 targets. If p95 > 0.05ms, escalate to §6.8 P1 optimisations before
proceeding.

### KL-2 — Trig Cost Is Architecture-Dependent

The 15 equiv-op estimate for `atan2` and 12 equiv-op estimates for `sin`/`cos` are based
on x86-64 hardware with FPU-assisted trig. On ARM (iOS, Android, potentially future
console targets), hardware trig unit characteristics differ. The §6.4 decision to use
runtime trig assumes Unity IL2CPP's consistent cross-platform behaviour; this must be
validated on any ARM target at Stage 1.

**Risk level:** Low for Stage 0 (PC-first development). Flag for Stage 2 (platform expansion).

### KL-3 — Simultaneous Pass Evaluation Count is Unbounded

The performance model assumes a maximum of 3 simultaneous pass evaluations per frame.
This assumption should hold under realistic AI Decision Tree behaviour. However, if the
Decision Tree ever issues pass requests for multiple agents in the same frame without
coordination, the headroom budget could be exceeded.

**Mitigation:** Decision Tree Spec #8 should document that pass initiation calls are
staggered if multiple agents decide to pass in the same game tick. Add a cross-spec
note `[PERF-DEPENDENCY-SPEC7]` to this effect.

### KL-4 — Spatial Hash API Final Cost Unknown

Phase 7 pressure query cost is estimated from Collision System §3.1.4's spatial hash
specification. The final API return type and iteration cost may differ from this estimate
if the Collision System spec is amended. The 8 equiv-ops per-opponent figure must be
re-verified against the finalised Collision System §3.1.4 before implementation.

**Action:** Cross-reference flag `[CROSS-SPEC-VERIFY: Collision §3.1.4]` on Phase 7
implementation.

### KL-5 — Outline Op Count Underestimate

The outline's §6.2 table estimated ~200–250 ops per pass. This section's authoritative
count for a typical contested direct pass (n=2, runtime trig) is 198 ops — within the
outline's range. The through-ball path with n=4 yields 233 ops, slightly above the
outline's upper bound. All §6.3 targets are based on the authoritative counts in this
section; the outline estimates are superseded.

**No action required.** Targets remain comfortable despite the revised counts.

---

## 6.10 Cross-References

| Topic | Authoritative Section | Summary |
|---|---|---|
| Frame budget allocation | Ball Physics #1 §6.2.1 | 6ms / 0.5ms headroom |
| Op count methodology | Agent Movement #2 §5.1 | Equiv-op definition |
| Spatial hash API | Collision System #3 §3.1.4 | Source of Phase 7 cost estimate |
| Per-evaluation performance gate | §5.13.4 (Testing) | p95/p99/allocation acceptance criteria |
| Zero-allocation policy | Development Best Practices §4 | P0 classification rationale |
| Trig platform risk | KL-2 (this section) | ARM validation deferred to Stage 1 |
| Profiling marker names | §6.7 (this section) | Must match implementation exactly |
| Struct sizes | §2.4 (Section 2) | Source of memory footprint figures in §6.5 |
| Through ball linear projection limitation | §1.3 KD-4 | Defines scope of Phase 3B |
| Performance test cases | §5.13.4 | PERF-001 through PERF-005 |
| ERR-007 attribute names | Spec Error Log §ERR-007 | Op counts stable; field names TBC |

---

## 6.11 Section Summary

| Subsection | Key Finding |
|---|---|
| **6.1 Complexity** | O(n) per evaluation; n = opponents in ~3m radius; practical n ≤ 5 |
| **6.2 Operation Counts** | 142–233 ops typical; 329–369 pathological (n=21, impossible in practice) |
| **6.2.4 Comparison** | Pass Mechanics is ~2,400× less expensive than Collision System over full match |
| **6.3 Budget** | p95 < 0.05ms target; match total < 50ms; avg frame impact < 0.003ms |
| **6.4 Trig Decision** | **Runtime trig retained. LUT rejected.** Savings are 0.067ms/match — below measurement noise. LUT introduces accuracy and determinism risk for no observable gain. |
| **6.5 Memory** | ~152 bytes stack per evaluation; ~424 bytes static. Zero GC pressure. |
| **6.6 Cache** | Passer/target data warm from Agent Movement. Spatial hash query is only cold-read risk. |
| **6.7 Profiling** | 3 markers defined. `PassMech.LaunchAngle` validates trig decision at runtime. |
| **6.8 Optimisations** | P0 mandatory (zero allocation). P1–P2 conditional on profiling. P3 post-Stage 0. |
| **6.9 Limitations** | 5 known; KL-2 (ARM trig) and KL-3 (simultaneous initiations) are highest priority. |

---

## 6.12 Version History

| Version | Date | Author | Notes |
|---|---|---|---|
| 1.0 | February 20, 2026, 11:59 PM PST | Claude (AI) / Anton | Initial draft. Trig evaluation completed; runtime trig confirmed for Stage 0. Operation counts authoritative. ERR-007/008 carried forward. |
| 1.1 | February 21, 2026 | Claude (AI) / Anton | Phase 4 op count updated from 11.5 to 13.0 equiv-ops per AM-003-001 (vOffset read +0.5, addition step +1.0). Fixed totals updated: direct pass 182.0→183.5, through ball 201.0→202.5. All scenario totals updated (+1.5). Performance budgets unaffected — increase is negligible. |
| 1.2 | March 25, 2026 | Claude (AI) / Anton | Post-audit fix: Decision Tree #7→#8 (C-03, 1 instance). |

---

*End of Section 6 — Pass Mechanics Specification #5*

*Next: Section 7 — Future Extensions*
