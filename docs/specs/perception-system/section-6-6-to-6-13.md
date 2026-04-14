## 6.6 Cache Behaviour

**Data warm at heartbeat time (likely L1/L2 resident):**
- `AgentState` array: updated by Agent Movement 6 frames earlier; will be L2-warm
- `BallState`: updated every frame by Ball Physics; will be L1-warm
- `PerceptionConstants`: read-only block of ~72 bytes; L1-warm after first heartbeat

**Data that may be cold (cache miss risk):**
- `_latencyCounters` Dictionary (Step 5): 5.5KB hash table accessed as a pointer-chased
  linked structure. Worst case: 3 L2 misses per dictionary lookup × 12 entities = 36 L2
  misses per agent. At 5ns per L2 miss: ~180ns per agent from latency counter reads alone.
  This is absorbed by budget headroom. P2-B optimisation (§6.7) replaces Dictionary with
  a flat array, eliminating this risk.
- Spatial hash (Step 2): The Collision System's spatial hash backing store may not be
  L1-warm when Perception calls `QueryRadius`. Estimated 2–4 L2 misses per call (~20ns
  overhead). Acceptable.

**Dominant cache behaviour:** The `Atan2` and `Asin` calls in Steps 3–4 are purely
computational (no memory access beyond their float inputs). They execute in FPU registers.
Cache pressure is therefore concentrated in Steps 2 and 5 (spatial hash and dictionary),
not in the trig-heavy Steps 3–4. This is a favourable characteristic.

---

## 6.7 Optimization Roadmap

Optimisations are classified by implementation priority. P0 applies only if profiling
reveals a budget violation; P1–P3 are conditional or deferred.

### 6.7.1 P0 — Required Only If Profiling Shows Budget Violation

**P0-A: Profile per-step to identify bottleneck.**
Use sub-markers: `FovCalculator.Compute`, `OcclusionFilter.Apply`, `RecognitionLatency.Process`.
The dominant step is expected to be Step 4 (occlusion). Verify before applying any fix.

**P0-B: Verify entity bearing caching in Step 4B.**
Confirm that `Atan2` for each FoV-surviving entity is computed **once**, before the shadow
cone inner loop, and the result stored in a local array. Recomputing it inside the loop
inflates Step 4 by up to 10× for k = 10 occluders. See §6.8 AP-1. This is the single
highest-impact correctness check in the implementation.

**P0-C: Verify zero GC allocation.**
Run Profiler with GC Alloc column visible. Any non-zero allocation is a defect. Common
sources: LINQ in hot path, `params` array boxing, `(int, int)` tuple key boxing as
`object` in Dictionary. The tuple key must implement `IEquatable<(int,int)>` without
boxing.

**P0-D: Cap candidate iteration to actual returned count.**
The spatial hash returns results in a pre-sized buffer. Confirm iteration stops at the
count returned by the query, not the buffer capacity, to avoid processing empty slots.

### 6.7.2 P1 — Recommended During Implementation

**P1-A: Initialise both latency counter dictionaries with `capacity: 512`.**
Prevents rehashing during play. The dictionary default capacity will trigger at least two
rehashes before reaching 462 entries. Each rehash allocates a new backing array and copies
all entries — a GC spike that violates PERF-003.

**P1-B: Pre-compute `cos(EffectiveFoV/2)` for dot-product FoV test.**
If the implementation team chooses Option B for the FoV test (§6.4.3), this pre-computation
must be done once per agent per tick, not per candidate. Cost is one `Mathf.Cos` call (12 ops)
per agent, amortised across all c candidates.

**P1-C: Cache occluder distances in shadow cone struct.**
Step 4A computes `occluderDist = occluderVec.magnitude` (6 ops). Step 4B uses occluder
distance for the depth-ordering check. Store the distance in the shadow cone struct rather
than recomputing it during Step 4B. Saves 6 ops × f entities × k occluders = up to
1,386 ops per agent at set-piece worst case.

### 6.7.3 P2 — Conditional, Apply If Profiling Confirms Benefit

**P2-A: Replace `_latencyCounters` Dictionary with flat array.**
Substitute `Dictionary<(int,int),int>` with `int[22, 21]` (462-element 2D array, ~1.8KB).
Index as `array[observerId, targetId < observerId ? targetId : targetId - 1]`. Eliminates
all Dictionary overhead in Step 5: replaces 3.0 equiv-op lookups with 0.5 equiv-op array
reads. Saves ~30 ops per agent per tick (12 entities × 2.5 ops saved). Also eliminates
the L2 miss risk from pointer-chased Dictionary nodes, which is the more significant gain.
The flat array is also smaller than the Dictionary (~1.8KB vs ~5.5KB), improving L1 fit.

**P2-B: Early rejection of rear-facing occluders in Step 4A.**
Before computing the full shadow cone for occluder O, check if O is directly behind the
observer (angle to O relative to FacingDirection > 90°). An occluder behind the observer
cannot block anything within the forward FoV arc. In typical play, ~30–40% of opponents
are in the rear hemisphere. This rejection saves the full 43-op cone construction for
those occluders. Estimated saving: ~140 ops per agent per tick (0.35 × 10 occluders × 40
ops saved, allowing for the rejection check overhead of ~3 ops).

### 6.7.4 P3 — Post-Stage 0 (Deferred)

**P3-A: Unity Burst Compiler annotation.**
Steps 3–4 (FoV + occlusion inner loops) are pure float arithmetic with no managed object
accesses — ideal candidates for Burst auto-vectorisation. Expected 2–4× speedup for these
steps. Not needed for Stage 0 budget. Defer until agent count grows or Fixed64 raises costs.

**P3-B: Angular-sector partitioning of occluder loop.**
Divide the FoV arc into sectors (e.g., 8 × 22.5° sectors). For each entity, only test
shadow cones from the same angular sector ± 1. This reduces the Step 4B inner loop from
k to ~k/4 on average — an O(n×k) → O(n×k/S) reduction. Not needed at n = 22; document
as the growth path if Stage 1+ adds sideline agents or extended squads.

---

## 6.8 Anti-Pattern Checklist

| # | Anti-Pattern | Consequence | Detection |
|---|--------------|-------------|-----------|
| AP-1 | `Atan2` called per-entity **inside** the shadow cone loop (Step 4B) | ~10× Step 4 cost inflation | `OcclusionFilter.Apply` > 50µs on profiler |
| AP-2 | LINQ (`Where`, `Select`, `.ToList()`) in any pipeline hot path | GC allocation per heartbeat | Profiler GC Alloc > 0 per tick |
| AP-3 | Dictionary initialised with default capacity | Rehash GC spike during first match | Profiler: GC alloc burst in first 50 ticks |
| AP-4 | `Vector2.magnitude` when only squared distance is needed | Unnecessary `Sqrt` | Code review |
| AP-5 | `Debug.Log` outside `#if UNITY_EDITOR` guard | String alloc in Release build | Profiler: GC alloc in Release build |
| AP-6 | `new PerceivedAgent(...)` inside heartbeat loop | GC alloc per entity per tick | Profiler GC Alloc > 0 |
| AP-7 | Decision Tree caching `ReadOnlySpan<PerceivedAgent>` across ticks | Dangling span; data corruption (silent) | Unit test: snapshot batch overwrite between ticks |
| AP-8 | `(int, int)` tuple key boxing as `object` in Dictionary | GC alloc per lookup | Profiler + code review: ensure `ValueTuple<int,int>` not boxed |

---

## 6.9 Known Limitations

### KL-1: ARM Platform Trig Performance (Unverified)

All op-count estimates and timing targets are derived for x86-64 (Intel/AMD) platforms.
Unity's `Mathf.Atan2` and `Mathf.Asin` delegate to platform C runtime trig:

- Apple Silicon M-series: typically comparable to x86-64; hardware-accelerated NEON paths
- Android ARM64: potentially 2–3× slower for `Atan2` in software float paths
- WebGL: JavaScript `Math.atan2` approximately 5× slower than native on some browsers

If Android ARM64 `Atan2` is 3× more expensive (~45 equiv-ops vs ~15), the ~46 calls per
agent per tick add approximately 1,380 additional ops — raising the per-agent total from
~1,819 to ~3,200 ops. At 5ns/op: ~16µs per agent. Still well within the 90µs ceiling.

**Action:** Profile on each target platform during Stage 0 implementation. If ARM64 trig
proves more than 5× slower, the P1-B dot-product FoV test (§6.7.2) becomes a P0 requirement
for ARM targets, potentially reducing `Atan2` calls from 46 to 25 per agent per tick.

### KL-2: Forced Refresh Rate Under Extreme Concurrent Events (Observation)

The forced refresh mechanism (§3.8, §4.6.1) has no explicit rate limit at Stage 0. The
authoritative trigger list caps the practical maximum at approximately 6 agents per event
(ball contact cluster in set-piece). Two simultaneous qualifying events (e.g., a tackle
happening the same frame as a pass reception elsewhere) could refresh up to 8 agents.

At ~90µs per agent, 8 simultaneous forced refreshes cost ~720µs — approximately 4.3% of
a 16.67ms physics frame. This is within budget. No rate limiting is warranted at Stage 0.

This is documented here as an observation, not a defect. If Stage 1+ introduces multiple
simultaneous ball events (multi-ball, extended squad), this bound must be reassessed.

### KL-3: Step 2 PressureScalar Cost Underestimated in Outline

The Outline §6.1 pipeline cost table estimated `QueryNearbyEntities` at ~5µs and did not
separately account for the `PressureScalar` sub-step. The authoritative Step 2 cost from
§6.2.3 is ~15.5–72.5 equiv-ops depending on pressure context, representing ~0.1–0.4µs
per agent at 5ns/op. The outline estimate remains valid as a combined Step 1+2 total; this
section supersedes it for individual step granularity.

**No action required.** Budget impact is negligible. The outline was correct to present a
combined estimate at the outline stage of development.

### KL-4: §2.3.3 Persistent State Size Underestimate

Section §2.3.3 estimates "approximately 3.7KB" for `PerceptionSystem` persistent state.
The authoritative figure from §6.5.2 is **~11.3KB** — approximately 3× larger. The
discrepancy is C# `Dictionary<(int,int),int>` per-entry node overhead (~8 bytes), which
§2.3.3 omitted by counting raw key+value storage only.

The 11.3KB figure is not a budget concern — it fits in L1 cache. However, §2.3.3 should
be corrected in its next revision to reference §6.5.2 as the authoritative figure.
Implementers sizing system memory from §2.3.3 alone will underestimate persistent state
allocation by 7.6KB.

**Action:** Flag §2.3.3 for correction in the next Section 2 revision (non-blocking for
Section 6 approval).

### KL-5: Step 6 Cost Is Agent-Attribute-Dependent

The per-agent summary in §6.2.9 uses ~50 equiv-ops for Step 6 (ApplyBlindSideAwareness)
as a representative typical value (Anticipation = 10 agent). The actual cost ranges from
~31 equiv-ops (A = 1, window fires rarely) to ~142 equiv-ops (A = 20, window fires 50%
of ticks).

A squad of 11 elite (A = 20) agents would add approximately 22 × (142 − 50) = 2,024 equiv-ops
per heartbeat over the mid-Anticipation estimate — approximately 10µs at 5ns/op. This
remains within budget. However, it means the `Perception.UpdateAll` timing will vary
measurably by squad Anticipation profile in integration testing. PERF-001 should be
profiled with both a low-Anticipation and a high-Anticipation squad configuration to
establish the full performance envelope.

---

## 6.10 Fixed64 Migration Notes

The Perception System uses floating-point arithmetic throughout. When Fixed64 Math Library
(Specification #9, not yet written) is implemented, a migration will be required for
cross-platform deterministic replay fidelity.

### 6.10.1 Affected Operations

| Step | Float Operations | Fixed64 Impact | Risk |
|------|-----------------|----------------|------|
| 2B | PressureScalar (add, div, clamp) | Direct substitution | Low |
| 3A | FoV formula (add, mul, div) | Direct substitution | Low |
| 3B | `Atan2` per candidate | Requires Fixed64 `Atan2` from Spec #9 | **High** |
| 4A | `Asin` per occluder, `Atan2`, `magnitude` | Requires Fixed64 trig + sqrt | **High** |
| 4B | Angular comparisons | Direct substitution | Low |
| 5 | L_rec formula (int arithmetic dominates) | Minimal — int ops unchanged | Low |
| 6 | Shoulder check intervals (int arithmetic) | Minimal | Low |

### 6.10.2 Cost Impact Estimate

Fixed64 transcendentals typically cost 8–15× more than hardware float on x86-64 (software
implementation vs. FPU instruction). At 10× transcendental cost:

```
Atan2 cost:  46 calls × 15 ops × 10× = 6,900 ops  (vs. 690 current)
Asin cost:   10 calls × 12 ops × 10× = 1,200 ops  (vs. 120 current)
Net increase per agent: ~7,290 additional ops
New per-agent total:    ~1,819 + 7,290 = ~9,109 ops
Wall-clock at 5ns/op:   ~46µs per agent
Total 22 agents:        ~1,012µs ≈ ~1ms
```

Post-migration estimated cost: **~1ms for 22 agents**, within the 2ms budget with ~50%
headroom. This is a far more favourable migration profile than Ball Physics (which
consumes its full budget post-migration) because the 10Hz cadence provides intrinsic
headroom.

### 6.10.3 Migration Strategy

Follow the `PerceptionMath` wrapper pattern (established in Ball Physics §7):

1. At implementation, wrap all `Mathf` calls in a `PerceptionMath` static class:
   `PerceptionMath.Atan2(y, x)` → delegates to `Mathf.Atan2(y, x)` at Stage 0.
2. When Spec #9 (Fixed64 Library) is written, swap `PerceptionMath` internals to use
   Fixed64 equivalents. Zero call-site changes required.
3. Run Section 5 §5.10 determinism test suite (DET-001 through DET-004) post-migration.
   Cross-platform byte-identical snapshots are the acceptance criterion.

No `PerceptionMath` interface is defined here — that is Spec #9's responsibility to
establish as part of the Fixed64 Library API. This section documents the wrapper pattern
as a requirement on the implementing developer.

---

## 6.11 Cross-References

| Topic | Authoritative Section | Notes |
|-------|-----------------------|-------|
| Performance requirements (FR-2.4.8) | §2.4.8 | Summary figures; this section is authoritative |
| Performance test gates | §5.13 PERF-001 to PERF-004 | Acceptance criteria for §6.3.2 targets |
| Profiler label names | §6.3.2 (this section) | Must match implementation exactly |
| Struct field definitions | §2.3.1, §2.3.2 (Section 2); §3.7 (Section 3) | Source of §6.5.3 memory figures |
| Persistent state (§2.3.3 underestimate) | §6.5.2 (this section) | KL-4: §2.3.3 should be corrected |
| Pressure scalar formula ops | First Touch §3.5.1–3.5.3 | Source of Step 2B derivation |
| Shadow cone geometry ops | §3.2 (Section 3) | Source of Step 4A derivation |
| Forced refresh trigger list | §3.8 + §4.6.1 | Source of §6.1.4 worst-case bound |
| Atan2 FoV alternative | §6.4.3 (this section) | Dot-product boundary note for test FOV-002/003 |
| Fixed64 wrapper pattern | Ball Physics §7 | Precedent for PerceptionMath wrapper |
| Frame budget allocation | Master Vol 4 | System-level allocation context |
| Zero-allocation policy | Development Best Practices | P0 rationale for AP-2, AP-6 |
| Heartbeat cadence | §2.1.2 (Section 2) | Source of 10Hz budget framing |

---

## 6.12 Section Summary

| Subsection | Key Finding |
|------------|-------------|
| **6.1 Complexity** | O(n × k); worst-case 4,356 shadow cone tests total — ~169µs at 5ns/op, well within budget |
| **6.2 Op Counts** | ~1,819 equiv-ops typical per agent; Step 4 (occlusion) + Step 3 (FoV) = ~79% of total |
| **6.2.3 PressureScalar** | 15.5–72.5 ops depending on press context (0–5 opponents in 3m); not the bottleneck |
| **6.2.7 Step 6** | Anticipation-dependent: 31 ops (A=1) to 142 ops (A=20) per agent per tick |
| **6.3 Budget** | 90µs ceiling; estimated actual 9–18µs (5–10× headroom) |
| **6.4 Trig** | **LUT rejected.** No budget pressure; determinism risk at boundaries; savings < 50µs total. Dot-product FoV alternative is P1, not P0. |
| **6.5 Memory** | ~11.3KB persistent state (KL-4: §2.3.3 underestimates); ~12.7KB snapshots per heartbeat; zero GC |
| **6.6 Cache** | Dictionary Step 5 is the only L2 miss risk; absorbed by budget headroom; P2-A flat-array eliminates it |
| **6.7 Optimisations** | P0 (bearing cache verify, zero-GC); P1–P2 (capacity init, flat array, rear-occluder reject); P3 (Burst, sector partition) |
| **6.9 Limitations** | KL-1 (ARM trig), KL-4 (§2.3.3 underestimate), KL-5 (Step 6 Anticipation variance) require attention |
| **6.10 Fixed64** | ~1ms post-migration (50% budget remaining); 10Hz cadence provides intrinsic headroom vs. 60Hz systems |

---

## 6.13 Version History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | February 26, 2026, 3:00 PM PST | Claude (AI) / Anton | Initial draft |
| 1.1 | February 26, 2026, 5:00 PM PST | Claude (AI) / Anton | Four corrections: (1) PressureScalar op count corrected from 25 to 34 equiv-ops per First Touch §3.5 formula — now 47.5 ops at p=3. (2) Forced refresh worst-case rewritten against §3.8/§4.6.1 authoritative triggers; invented "8 agent" claim removed; max bounded at 6 (ball contact cluster), 2 for tackle/possession. (3) Step 6 amortised cost expanded to show full Anticipation-attribute range (A=1: ~31 ops; A=20: ~142 ops) rather than single median; KL-5 added. (4) §2.3.3 underestimate ($~3.7KB actual ~11.3KB) formalised as KL-4 with correction recommendation. |

---

*End of Section 6 — Perception System Specification #7*
*Tactical Director — Specification #7 of 20 | Stage 0: Physics Foundation*

*Next: Section 7 — Future Extensions*
