# Perception System Specification #7 — Section 6: Performance Analysis

**File:** `Perception_System_Spec_Section_6_v1_1.md`
**Purpose:** Authoritative performance analysis for the Perception System — computational
complexity classification, per-step operation counts derived from the §3 pipeline models,
memory footprint, profiling targets, trig cost evaluation, optimization roadmap, known
limitations, and Fixed64 migration notes. This section is the single authoritative source
for all Perception System performance targets and budgets. Budget figures stated in §2.4.8
are summaries derived from this section; if a conflict exists, this section takes precedence.

**Created:** February 26, 2026, 3:00 PM PST
**Version:** 1.1
**Status:** DRAFT — Awaiting Lead Developer Review
**Specification Number:** 7 of 20 (Stage 0 — Physics Foundation)
**Author:** Claude (AI) with Anton (Lead Developer)
**Prerequisites:** Section 1 v1.1, Section 2 v1.1, Section 3 v1.1, Section 4 v1.1, Section 5 v1.1

**Open Dependency Flags:** None. All hard dependencies confirmed stable in Section 1.

**Version History:**

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | February 26, 2026, 3:00 PM PST | Claude (AI) / Anton | Initial draft |
| 1.1 | February 26, 2026, 5:00 PM PST | Claude (AI) / Anton | Four corrections: (1) PressureScalar op count corrected from 25 to 34 equiv-ops (derived from First Touch §3.5 formula line-by-line: 5 fixed + 9 per-opponent × 3 typical + 2 clamp). (2) Forced refresh worst-case rewritten against §3.8 and §4.6.1 authoritative trigger list — "8 agents" claim removed; maximum bounded at 6 (ball contact in clustered scenario). (3) Step 6 amortised cost calculation explicitly qualified as Anticipation-dependent; single-median shortcut replaced with attribute-range derivation. (4) §2.3.3 persistent state underestimate flagged as KL-4 with authoritative corrected figure. |

---

## Table of Contents

- [Preamble: Role of This Section](#preamble-role-of-this-section)
- [6.1 Computational Complexity](#61-computational-complexity)
  - [6.1.1 Overall Classification](#611-overall-classification)
  - [6.1.2 Per-Step Complexity Breakdown](#612-per-step-complexity-breakdown)
  - [6.1.3 Worst-Case Bounding Analysis](#613-worst-case-bounding-analysis)
  - [6.1.4 Forced Refresh Cost Bounding](#614-forced-refresh-cost-bounding)
- [6.2 Operation Count Analysis](#62-operation-count-analysis)
  - [6.2.1 Methodology](#621-methodology)
  - [6.2.2 Step 1 — CacheAttributes](#622-step-1--cacheattributes)
  - [6.2.3 Step 2 — QueryNearbyEntities and PressureScalar](#623-step-2--querynearbyentities-and-pressurescalar)
  - [6.2.4 Step 3 — ApplyFieldOfView](#624-step-3--applyfieldofview)
  - [6.2.5 Step 4 — ApplyOcclusionFilter](#625-step-4--applyocclusionfilter)
  - [6.2.6 Step 5 — ApplyRecognitionLatency](#626-step-5--applyrecognitionlatency)
  - [6.2.7 Step 6 — ApplyBlindSideAwareness](#627-step-6--applyblindsideawareness)
  - [6.2.8 Step 7 — BuildPerceptionSnapshot](#628-step-7--buildperceptionsnapshot)
  - [6.2.9 Per-Agent Summary](#629-per-agent-summary)
  - [6.2.10 Comparison to Prior Specifications](#6210-comparison-to-prior-specifications)
- [6.3 Performance Budget](#63-performance-budget)
  - [6.3.1 Budget Derivation](#631-budget-derivation)
  - [6.3.2 Per-Function Timing Targets](#632-per-function-timing-targets)
  - [6.3.3 Measurement Protocol](#633-measurement-protocol)
- [6.4 Trig Cost Evaluation](#64-trig-cost-evaluation)
  - [6.4.1 Atan2 Usage and Lookup Table Evaluation](#641-atan2-usage-and-lookup-table-evaluation)
  - [6.4.2 Asin Usage](#642-asin-usage)
  - [6.4.3 Dot-Product Alternative for FoV Test](#643-dot-product-alternative-for-fov-test)
- [6.5 Memory Footprint](#65-memory-footprint)
  - [6.5.1 Per-Heartbeat Stack Usage](#651-per-heartbeat-stack-usage)
  - [6.5.2 Static Persistent State](#652-static-persistent-state)
  - [6.5.3 Snapshot Output Memory](#653-snapshot-output-memory)
  - [6.5.4 Cache-Line Analysis](#654-cache-line-analysis)
- [6.6 Cache Behaviour](#66-cache-behaviour)
- [6.7 Optimization Roadmap](#67-optimization-roadmap)
- [6.8 Anti-Pattern Checklist](#68-anti-pattern-checklist)
- [6.9 Known Limitations](#69-known-limitations)
- [6.10 Fixed64 Migration Notes](#610-fixed64-migration-notes)
- [6.11 Cross-References](#611-cross-references)
- [6.12 Section Summary](#612-section-summary)
- [6.13 Version History](#613-version-history)

---

## Preamble: Role of This Section

This section provides the **single authoritative source** for all Perception System
performance targets, budgets, and analysis.

**Critical distinction from per-frame systems:** Ball Physics, Agent Movement, and Collision
System all analyse per-frame costs because those systems run at 60Hz. The Perception System
runs at **10Hz** — one heartbeat every 100ms. This fundamentally changes the performance
framing:

- The budget window is 100ms, not 16.67ms.
- The Perception System must complete all 22 agents within **2ms** of that 100ms window.
- The remaining 98ms is available to other systems; Perception is **not** on the critical
  path of the 60Hz physics loop.

Despite this generous budget, a thorough analysis is warranted for three reasons:

1. **The 10Hz heartbeat coincides with a physics frame.** On every 6th physics frame (at
   60Hz cadence), the full perception batch executes alongside the standard 60Hz pipeline.
   That frame bears the cost of both pipelines; the 2ms target must be verified affordable
   within the tighter per-frame envelope on those frames.

2. **Forced mid-heartbeat refreshes can trigger at any physics frame.** Ball contact,
   tackle completion, and possession change events trigger perception refreshes for the
   directly involved agents outside the scheduled heartbeat. The worst-case cost of these
   events must be bounded.

3. **The shadow cone occlusion test has an O(n × k) inner loop.** While n = 22 is fixed
   and k (nearby opponents per observer) is typically ≤ 10, any unguarded loop requires
   explicit bounding to prevent future regression.

**Outline authority:** The 2ms / 22 agents / ~90µs per-agent budget figures are
established in Outline v1.1 §6.1. Section 2 §2.4.8 carries these as summary requirements.
This section is the authoritative derivation of those figures, not a revision of them.

---

## 6.1 Computational Complexity

### 6.1.1 Overall Classification

The Perception System executes a **sequential per-agent pipeline**. For each of the 22
agents, 7 steps execute in strict order. The pipeline does not interleave agents; each
agent is fully processed before the next begins. All 22 agents execute within the single
2ms heartbeat allocation.

**Overall complexity: O(n × k)**

Where:
- **n** = number of agents (always 22 at Stage 0; fixed, not a variable)
- **k** = number of nearby opponents visible to each observer (range: 0–21; typical: 5–10
  in open play; up to 21 in clustered set-piece scenarios)

The O(n × k) bound arises entirely from Step 4 (ApplyOcclusionFilter), which tests each
FoV-surviving entity against shadow cones cast by all nearby opponents. All other steps are
O(c) or better, where c is the candidate list size (≤ 22 at Stage 0). No step has a
higher-order term.

### 6.1.2 Per-Step Complexity Breakdown

| Step | Name | Complexity | Inner Loop? | Dominant Cost |
|------|------|-----------|------------|----------------|
| 1 | CacheAttributes | O(1) | None | 4 struct field reads |
| 2 | QueryNearbyEntities + PressureScalar | O(p) | Over pressure-radius opponents (p ≤ 11 typical) | Spatial hash call + inverse-square loop |
| 3 | ApplyFieldOfView | O(c) | Over candidates (c ≤ 22) | `Atan2` per candidate bearing |
| 4 | ApplyOcclusionFilter | O(f × k) | FoV-filtered × opponents | `Asin` per occluder, `Atan2` per entity |
| 5 | ApplyRecognitionLatency | O(f) | Over occlusion-surviving entities | Dictionary read per entity |
| 6 | ApplyBlindSideAwareness | O(b × k) conditional | Blind-side candidates × opponents | Active only during 3-tick check window |
| 7 | BuildPerceptionSnapshot | O(confirmed) | Over confirmed entities | Struct field writes |

**Notation:**
- c = candidate count after spatial hash query (typically all 22 other entities at Stage 0,
  since MAX_PERCEPTION_RANGE = 120m spans the full pitch diagonal)
- f = entities surviving FoV test (typical: 10–15 in open play)
- k = nearby opponents generating shadow cones (typical: 5–10)
- p = opponents within PRESSURE_RADIUS = 3.0m (typical: 0–4 in open play; 0 when no press)
- b = entities in blind-side arc during a shoulder check (typical: 2–6)
- confirmed = entities fully confirmed after latency (typically slightly < f)

### 6.1.3 Worst-Case Bounding Analysis

**Worst case scenario: corner kick or set-piece**

All 22 agents clustered within a 20m radius. Every observer has all 21 others within FoV
range. FoV-surviving entities ≈ 18 (those within the forward arc). All 11 opponents
generate shadow cones.

Per-observer Step 4 worst case: 18 candidates × 11 occluders = **198 shadow cone tests**.
Total across all 22 observers: 22 × 198 = **4,356 shadow cone tests per heartbeat**.

Each test costs approximately 4 operations (depth check 2.5 + angular difference 1 +
comparison 0.5), plus an `Atan2` (15 ops) cached once per entity before the occluder
inner loop. The entity bearing cost is therefore 15 ops per entity amortised across
all k occluder tests. Total worst-case Step 4 computation:

```
Cone construction: 22 observers × 11 occluders × 43 ops = 10,406 ops
Entity bearings:   22 observers × 18 entities × 15 ops  =  5,940 ops
Occlusion tests:   4,356 tests × 4 ops each             = 17,424 ops
Total Step 4:      ~33,770 ops worst case
```

At 5ns per op (managed C# conservative estimate): **~169µs total for all shadow cone
computation** at worst case. This is 8.4% of the 2ms budget. The O(n × k) characteristic
is not a concern at n = 22.

### 6.1.4 Forced Refresh Cost Bounding

A forced mid-heartbeat refresh (§3.8, §4.6.1) re-runs the full 7-step pipeline for
involved agents only. The authoritative trigger list from §4.6.1 defines three qualifying
events and their agent scope:

| Trigger | Agents Refreshed (per §3.8 / §4.6.1) |
|---------|--------------------------------------|
| Ball contact (pass/shot/first touch) | Ball-contacting agent + agents within 5m of ball |
| Tackle completion | Tackler + tackled agent only |
| Possession change (non-tackle) | Losing agent + gaining agent only |

**Tackle / possession change worst case:** 2 agents refreshed.
**Ball contact worst case:** The ball-contacting agent plus all agents within 5m of the
ball. In a corner kick cluster, up to 6 agents could be within 5m simultaneously. This is
the practical maximum given agent body sizes and typical set-piece spacing.

**Worst-case forced refresh cost:** 6 agents × ~90µs per agent (§6.3.1 per-agent budget)
= **~540µs**. This is 3.2% of the 16.67ms physics frame budget. It is safely absorbed
by frame headroom on any frame that is not also a scheduled heartbeat frame.

**On a heartbeat frame (every 6th physics frame):** A simultaneous worst-case forced
refresh (6 agents) plus the full scheduled heartbeat (22 agents) costs:
~2ms (scheduled) + 540µs (forced) = ~2.54ms. This remains well within the 5ms of
headroom available after standard 60Hz physics on a heartbeat frame.

**No rate limiting or deferral mechanism is needed at Stage 0.** This bound is documented
here as the authoritative worst-case analysis. If agent count grows in Stage 1+, this
analysis must be updated.

---

## 6.2 Operation Count Analysis

### 6.2.1 Methodology

Operation counts follow the **equivalent-operation (equiv-op)** methodology established
in Agent Movement Specification #2 §5.1 and applied consistently in First Touch #4 §6
and Pass Mechanics #5 §6:

| Operation | Equiv-Ops | Notes |
|-----------|-----------|-------|
| Float add / subtract / compare / branch | 1.0 | |
| Float multiply | 1.0 | |
| Float divide | 3.0 | |
| `Mathf.Sqrt` | 5.0 | |
| `Mathf.Atan2` | 15.0 | Platform C runtime; x86-64 baseline |
| `Mathf.Asin` | 12.0 | |
| `Mathf.Cos` / `Mathf.Sin` | 12.0 | |
| `Vector2.magnitude` | 6.0 | 1 add + 1 mul + 1 Sqrt |
| `Vector2.normalized` | 9.0 | magnitude + 2 divides |
| Struct field read | 0.5 | |
| Struct field write | 0.5 | |
| Dictionary lookup `Dictionary<(int,int),int>` | 3.0 | Hash + comparison |
| Dictionary write | 4.0 | Hash + comparison + write |
| Conditional branch (taken / not-taken combined) | 0.5 | |

All counts represent the **standard execution path** for typical open-play conditions.
ARM platform differences are documented in §6.9 Known Limitations.

---

### 6.2.2 Step 1 — CacheAttributes

Executed once per agent per heartbeat, before any pipeline step.

| Operation | Equiv-Ops |
|-----------|-----------|
| Read `AgentState.Position` (2 floats) | 1.0 |
| Read `AgentState.FacingDirection` (float) | 0.5 |
| Read `PlayerAttributes.Decisions` (int) | 0.5 |
| Read `PlayerAttributes.Anticipation` (int) | 0.5 |
| Cache 4 values to locals (4 writes) | 2.0 |
| **Step 1 Total** | **4.5** |

---

### 6.2.3 Step 2 — QueryNearbyEntities and PressureScalar

Step 2 encompasses two distinct operations that both occur before the FoV test: the
spatial hash range query (candidate enumeration) and the PressureScalar computation
(required by Step 3A to compute EffectiveFoV). They are grouped here because the
PressureScalar query is a separate spatial hash call at a different radius, not part
of the main candidate enumeration.

**Sub-step 2A — Candidate enumeration (spatial hash query at 120m):**

| Operation | Equiv-Ops |
|-----------|-----------|
| `spatialHash.QueryRadius(pos, 120f)` — hash lookup + buffer reference | 5.0 |
| Candidate count read | 0.5 |
| **Sub-step 2A Total** | **5.5** |

**Sub-step 2B — PressureScalar computation (§3.6, formula from First Touch §3.5):**

This is a separate `spatialHash.QueryRadius(pos, 3.0f, excludeTeamId)` call followed by
the inverse-square loop. The formula is reused verbatim from First Touch §3.5.1–3.5.3.
Op count is derived directly from those sub-sections.

**Fixed cost (always executes, regardless of opponent count):**

| Operation | Equiv-Ops |
|-----------|-----------|
| `spatialHash.QueryRadius(pos, 3.0f, excludeTeamId)` — filtered hash query | 5.0 |
| `rawPressure = 0f` initialisation | 0.0 |
| **Sub-step 2B Fixed Total** | **5.0** |

**Per-opponent cost (repeats p times, where p = opponents within 3.0m):**

| Operation | Equiv-Ops (per opponent) |
|-----------|--------------------------|
| `distance = |Observer.Position - O.Position|` (Vector2.magnitude) | 6.0 |
| `Max(distance, MIN_PRESSURE_DISTANCE)` clamp | 1.5 |
| `rawContrib = (0.3 / dist)²` = divide + multiply | 4.0 |
| `rawPressure += rawContrib` | 1.0 |
| **Per-opponent cost** | **12.5** |

**Aggregation:**

| Operation | Equiv-Ops |
|-----------|-----------|
| `PressureScalar = Clamp(rawPressure / 1.5, 0, 1)` — divide + Clamp01 | 5.0 |
| **Aggregation Total** | **5.0** |

**Sub-step 2B totals by typical pressure context:**

| Context | p (opponents in 3m) | Sub-step 2B Total |
|---------|--------------------|--------------------|
| Open play, no press | 0 | 5.0 + 0 + 5.0 = **10.0** |
| Light press (1 opponent) | 1 | 5.0 + 12.5 + 5.0 = **22.5** |
| Standard press (3 opponents) | 3 | 5.0 + 37.5 + 5.0 = **47.5** |
| Heavy press (5 opponents) | 5 | 5.0 + 62.5 + 5.0 = **72.5** |

**Step 2 Total (typical p = 2 opponents in press):**
5.5 (2A) + 10.0 + 25.0 + 5.0 (2B, p=2) = **~45.5 equiv-ops**

**Step 2 Total (no press, p = 0):** 5.5 + 10.0 = **~15.5 equiv-ops**

This is a significant variable. Agents under active press cost approximately 3× more in
Step 2 than agents in open space. With 22 agents and typically 2–4 under active press at
any moment, the average Step 2 cost across the full heartbeat batch falls between these
bounds. The pressure calculation is not the bottleneck (Step 4 dominates), but it is the
most attribute-sensitive variable in the pipeline.

---

### 6.2.4 Step 3 — ApplyFieldOfView

Computes `EffectiveFoVAngle` once per agent, then tests each candidate entity.

**Sub-step 3A — Compute EffectiveFoVAngle (once per observer, PressureScalar already cached from Step 2):**

| Operation | Equiv-Ops |
|-----------|-----------|
| Read Decisions from cache | 0.5 |
| FoV bonus: `(Decisions - 1) / 19.0f × MAX_FOV_BONUS_ANGLE` | 3.5 (sub, div, mul) |
| Read PressureScalar (cached from Step 2) | 0.5 |
| `Max(0, PressureScalar - PRESSURE_FOV_THRESHOLD)` | 1.5 |
| PressureReduction: `result × PRESSURE_FOV_SCALE (40.0f)` | 1.0 |
| `EffectiveFoV = BASE + bonus - reduction` | 2.0 |
| `Max(MIN_FOV_ANGLE, EffectiveFoV)` clamp | 1.5 |
| Half-angle: `EffectiveFoV / 2.0f` | 1.5 |
| **Sub-step 3A Total** | **12.0** |

**Sub-step 3B — Angular bearing test per candidate entity (×c candidates, typical c = 21):**

| Operation | Equiv-Ops (per entity) |
|-----------|------------------------|
| `delta = entity.Position - observer.Position` (2 subs) | 2.0 |
| `Mathf.Atan2(delta.y, delta.x)` | 15.0 |
| Angular difference from FacingDirection | 2.0 (sub + abs) |
| Wrap to [0°, 180°] | 1.5 |
| Compare against half-angle | 1.0 |
| Conditional discard | 0.5 |
| **Per-entity angular test** | **22.0** |

| Scenario | c (candidates tested) | Sub-step 3B Total |
|----------|----------------------|-------------------|
| Standard (all entities in range) | 21 | 21 × 22 = **462.0** |
| Sparse positioning (some out of range) | 15 | 15 × 22 = **330.0** |

**Step 3 Total (typical c = 21):** 12.0 + 462.0 = **~474 equiv-ops**

---

### 6.2.5 Step 4 — ApplyOcclusionFilter

The dominant pipeline step. For each opponent, compute their shadow cone angular interval
(one `Asin` per occluder). For each FoV-surviving entity, test its bearing against each
shadow cone's angular interval (one `Atan2` per entity, cached before the inner loop).

**Critical implementation note:** Entity bearings must be computed once per entity and
cached in a local array before the shadow cone inner loop. Recomputing `Atan2` per
entity-per-cone inflates this step by up to k × 15 ops per entity — a 10× cost
multiplication for k = 10 occluders. This is the single most impactful correctness
requirement in the entire pipeline. See §6.8 AP-1.

**Sub-step 4A — Shadow cone construction per opponent (×k opponents, typical k = 10):**

| Operation | Equiv-Ops (per occluder) |
|-----------|--------------------------|
| `occluderVec = O.Position - Observer.Position` (2 subs) | 2.0 |
| `occluderDist = occluderVec.magnitude` | 6.0 |
| `occluderBearing = Atan2(occluderVec.y, occluderVec.x)` | 15.0 |
| `Min(AGENT_BODY_RADIUS / Max(dist, clampVal), 1.0f)` | 4.5 |
| `Asin(ratio)` → raw half-angle | 12.0 |
| `Max(MIN_SHADOW_HALF_ANGLE, raw)` floor clamp | 1.5 |
| Store cone interval: bearing ± halfAngle | 2.0 |
| **Per-occluder shadow cone construction** | **43.0** |

| Scenario | k (occluders) | Sub-step 4A Total |
|----------|--------------|-------------------|
| Typical open play | 10 | 10 × 43 = **430** |
| Set-piece worst case | 11 | 11 × 43 = **473** |
| No nearby opponents | 0 | **0** |

**Sub-step 4B — Entity bearing cache + occlusion tests:**

| Operation | Equiv-Ops |
|-----------|-----------|
| Per-entity `Atan2` bearing (×f, cached once, used across all k cones) | f × 15 |
| Per entity-cone depth check: compare entity dist vs. occluder dist | f × k_avg × 2.5 |
| Per entity-cone angle check (only for entities passing depth check) | f × k_avg × 1.5 |
| Conditional break on first occlusion found (early exit) | f × 0.5 |

With early exit (average 50% of candidates are occluded by the first cone tested):

| Scenario | f | k | Est. 4B Total |
|----------|---|---|---------------|
| Typical open play | 15 | 10 | 225 (bearings) + 15×5×4 (tests, 50% exit) = **525** |
| Set-piece worst case | 18 | 11 | 270 + 18×5.5×4 = **666** |
| No occluders | 15 | 0 | 0 |

**Step 4 Total (typical):** 430 (4A) + 525 (4B) = **~955 equiv-ops**
**Step 4 Total (set-piece worst case):** 473 (4A) + 666 (4B) = **~1,139 equiv-ops**

---

### 6.2.6 Step 5 — ApplyRecognitionLatency

Processes each entity surviving FoV + occlusion (f_confirmed, typically 10–14).

| Operation | Equiv-Ops (per entity) |
|-----------|------------------------|
| Dictionary lookup: `_latencyCounters[(observerId, entityId)]` | 3.0 |
| First-visibility check (counter == 0 or key absent) | 1.0 |
| L_rec computation: `L_MAX - round((D-1)/19 × (L_MAX-L_MIN))` | 4.5 (sub, div, mul, round) |
| Half-turn orientation bonus check + apply: `×0.85` if flag set | 2.5 |
| Deterministic noise: `xorshift32(observerId ^ entityId ^ tick) % 2` | 4.0 |
| `L_rec += noiseResult` (additive-only, preserves L_MIN floor per §3.3.4) | 1.0 |
| Counter increment: read + conditional reset + write | 2.0 |
| Threshold check: `counter >= L_rec` | 1.5 |
| Confirmation write (bool flag) | 0.5 |
| **Per-entity latency processing** | **20.0** |

| Scenario | Entities processed | Step 5 Total |
|----------|--------------------|--------------|
| Typical (12 entities) | 12 | 12 × 20 = **240** |
| Dense set-piece (16 entities) | 16 | 16 × 20 = **320** |

---

### 6.2.7 Step 6 — ApplyBlindSideAwareness

This step is **conditional on whether a shoulder check window is active for this agent
at this tick**. The window duration is `SHOULDER_CHECK_DURATION = 3 ticks` (300ms).
The window fires interval depends on `PlayerAttributes.Anticipation`:

```
CheckInterval_ticks = round(Lerp(CHECK_MAX_TICKS, CHECK_MIN_TICKS, (A-1)/19))
= round(Lerp(30, 6, (A-1)/19))

Anticipation = 1:  interval = 30 ticks (3.0s)  → window active 3/30 = 10% of ticks
Anticipation = 10: interval = 18 ticks (1.8s)   → window active 3/18 = 17% of ticks
Anticipation = 20: interval = 6 ticks (0.6s)    → window active 3/6  = 50% of ticks
```

**Step 6 cost therefore varies substantially by Anticipation attribute.** The analysis
below provides costs for both the common case (no active window) and the active window
case, then derives attribute-specific amortised costs.

**Common case (no active window):**

| Operation | Equiv-Ops |
|-----------|-----------|
| Read `_blindSideWindowActive[id]` | 1.0 |
| Branch: not active → check schedule | 0.5 |
| Read `_nextCheckTick[id]` | 1.0 |
| Compare against current tick | 1.0 |
| Conditional: if tick matches, open window (write 2 fields) + schedule next | 6.0 |
| **Step 6 — no window (window-opening tick)** | **9.5** |
| **Step 6 — no window (non-opening tick)** | **3.5** |

The window-opening tick costs ~9.5 ops and occurs once every `CheckInterval_ticks`.
Non-opening ticks cost ~3.5 ops. This is negligible in all cases.

**Active window case (b ≈ 4 blind-side candidates typical):**

| Operation | Equiv-Ops |
|-----------|-----------|
| Window active check + window expiry management | 4.0 |
| Blind-side arc spatial query (partial spatial hash, rear 200° arc) | 8.0 |
| Per blind-side candidate bearing test (same as Step 3B, ×b) | b × 22.0 |
| Per blind-side candidate occlusion test (same as Step 4, simplified single-cone) | b × 25.0 |
| Per blind-side candidate latency processing (same as Step 5) | b × 20.0 |
| Window expiry decrement | 2.0 |
| **Step 6 — active window (b = 4)** | **4 + 8 + 4×67 = ~280 equiv-ops** |

**Amortised Step 6 cost per Anticipation bracket:**

| Anticipation | Window fraction | Amortised cost (per tick) |
|-------------|----------------|--------------------------|
| A = 1 (interval 30) | 10% | 0.10 × 280 + 0.90 × 3.5 = **~31.2** |
| A = 10 (interval 18) | 17% | 0.17 × 280 + 0.83 × 3.5 = **~50.5** |
| A = 20 (interval 6) | 50% | 0.50 × 280 + 0.50 × 3.5 = **~141.8** |

**For the per-agent summary (§6.2.9), a mid-Anticipation value of ~50 equiv-ops is used as
the representative typical-agent cost.** High-Anticipation (A = 20) agents incur up to
4.5× higher Step 6 costs than low-Anticipation agents. This is intentional by design —
elite scanners are more computationally expensive, which correctly reflects the cognitive
work they perform.

---

### 6.2.8 Step 7 — BuildPerceptionSnapshot

Assembles the output `PerceptionSnapshot` struct from all confirmed pipeline outputs.

| Operation | Equiv-Ops |
|-----------|-----------|
| Header writes: ObserverId, FrameNumber | 1.0 |
| Ball fields: BallVisible, BallPerceivedPosition, BallStalenessFrames | 2.5 |
| VisibleTeammates span setup (pointer + count write) | 1.5 |
| VisibleOpponents span setup | 1.5 |
| EffectiveFoVAngle, PressureScalar writes (cached from Steps 2–3) | 1.0 |
| BlindSideWindowActive, BlindSideWindowExpiry, BlindSide span | 2.0 |
| ForcedRefreshThisTick flag write | 0.5 |
| ShoulderCheckAnimData stub populate (4 fields) | 4.0 |
| Per-confirmed-agent PerceivedAgent writes (6 fields × 0.5 each = 3.0 per agent) | 3.0 × confirmed |
| **Step 7 Total (12 confirmed agents)** | **~50.0** |

---

### 6.2.9 Per-Agent Summary

The table below uses typical open-play conditions for each step. Where a step cost varies
by attribute, the representative value is noted.

| Step | Name | Equiv-Ops (typical) | Variable Factor |
|------|------|---------------------|-----------------|
| 1 | CacheAttributes | 4.5 | None |
| 2 | QueryNearbyEntities + PressureScalar | ~45.5 | p = opponents in 3m (0–11) |
| 3 | ApplyFieldOfView | ~474 | c = candidates tested (≤ 22) |
| 4 | ApplyOcclusionFilter | ~955 | **Dominant.** f × k; early-exit rate |
| 5 | ApplyRecognitionLatency | ~240 | entities confirmed |
| 6 | ApplyBlindSideAwareness | ~50 | **Highly attribute-dependent** (A=1: ~31; A=20: ~142) |
| 7 | BuildPerceptionSnapshot | ~50 | confirmed agent count |
| **Total per-agent (typical, A=10)** | | **~1,819 equiv-ops** | |
| **Total per-agent (A=20, set-piece)** | | **~2,600 equiv-ops** | |
| **Total per-agent (A=1, open play, no press)** | | **~1,350 equiv-ops** | |
| **Total all 22 agents (typical mixed squad)** | | **~38,000–42,000 equiv-ops** | |

**Wall-clock estimate at 5ns/op (managed C#, conservative):**
- Typical squad: ~40,000 ops × 5ns = **~200µs for all 22 agents**
- This is **10% of the 2ms budget**.

Even at 10ns/op (worst-case managed C# with cache misses and JIT overhead):
- ~400µs — **20% of budget**. Budget is not at risk for any realistic parameter configuration.

The outline §6.1 estimate of ~23µs per agent (total ~506µs) was a wall-clock intuition
estimate. The authoritative op-count derivation gives a lower wall-clock estimate
(~9–18µs per agent at 5–10ns/op), suggesting the outline was conservatively high. Both
estimates are comfortably within the 90µs per-agent ceiling.

### 6.2.10 Comparison to Prior Specifications

| System | Cadence | Budget | Op Count | Budget Utilisation (est.) |
|--------|---------|--------|----------|--------------------------|
| Ball Physics | 60Hz | 0.5ms | ~190 per frame | ~40% |
| Agent Movement | 60Hz | 3.0ms | ~5,500 total (22 agents) | ~55% |
| Collision System | 60Hz | 1.0ms | ~5,800 total | ~35% |
| **Perception System** | **10Hz** | **2.0ms** | **~40,000 per heartbeat** | **<20% estimated** |

Perception has more operations per execution than any prior system, but its 10Hz cadence
makes it budget-comfortable. Step 3 (FoV bearing tests via `Atan2`) and Step 4 (occlusion)
account for approximately 79% of total per-agent ops. Both are O(c) or O(f × k) inner
loops that are cache-friendly and JIT-friendly.

---

## 6.3 Performance Budget

### 6.3.1 Budget Derivation

**Source authority:** Outline v1.1 §6.1, confirmed in §2.4.8:

- 10Hz heartbeat window: **100ms**
- Perception allocation: **2ms total for all 22 agents**
- Per-agent target: **~90µs**

The 2ms allocation comes from the Master Vol 4 system budget. On every 6th physics frame
(heartbeat frame), the 60Hz pipeline and the perception batch must both complete. The 60Hz
pipeline uses approximately 11ms (Ball Physics 0.5ms + Agent Movement 3ms + Collision 1ms
+ AI 4ms + Events 0.5ms + ~2ms other). The remaining ~5.67ms on heartbeat frames
accommodates perception (2ms) plus rendering headroom.

**Per-agent ceiling:** 2ms / 22 = **90.9µs**.

The op-count analysis in §6.2.9 estimates actual cost at **~9–18µs per agent** at realistic
managed C# timings. This gives a **5× to 10× budget margin** — by far the most
comfortable allocation of any Stage 0 system. Profiling is expected to confirm substantial
underutilisation. If profiling reveals anything above 45µs per agent (50% of ceiling),
the optimisation roadmap in §6.7 should be reviewed.

### 6.3.2 Per-Function Timing Targets

These targets are the acceptance gates for Section 5 §5.13 PERF-001 through PERF-004.

| Profiler Label | Target (mean) | Ceiling (p99) | Hard Fail |
|----------------|--------------|---------------|-----------|
| `Perception.UpdateAll` | < 1.0ms | < 2.0ms | > 5.0ms |
| `Perception.UpdateSingle` | < 45µs | < 90µs | > 0.5ms |
| `FovCalculator.Compute` | < 15µs | < 30µs | > 0.2ms |
| `OcclusionFilter.Apply` | < 20µs | < 40µs | > 0.2ms |
| `RecognitionLatency.Process` | < 5µs | < 10µs | > 0.05ms |
| `Perception.ForcedRefresh` | < 45µs | < 90µs | > 0.5ms |

**Reference hardware:** Intel Core i7-10700K, 32GB RAM, Unity 2022 LTS Editor Play Mode,
Burst Compiler disabled (conservative managed C# baseline). These targets apply to the
unoptimised managed path; Burst-compiled targets are expected to be substantially lower.

### 6.3.3 Measurement Protocol

1. Unity Editor Play Mode with standard Profiler (not Deep Profile — Deep Profile inflates
   managed costs 5–10×; use `ProfilerMarker` named markers instead).
2. Execute 1,000 consecutive heartbeat ticks, all 22 agents in standard open-play
   mid-field formation.
3. Discard first 50 ticks (JIT warm-up — the managed JIT will optimise the hot inner
   loops after initial execution).
4. Record per-heartbeat timing from the `Perception.UpdateAll` marker.
5. Calculate mean, p50, p95, p99 from the 950-tick sample.
6. **Pass criteria align exactly with Section 5 §5.13 PERF-001 through PERF-004:**
   - p95 < 1.5ms; p99 < 2.0ms (hard fail above); zero GC allocation per tick; zero NaN
     in a 54,000-tick (90-minute) continuous run.

---

## 6.4 Trig Cost Evaluation

The perception pipeline makes heavy use of `Atan2` and `Asin`. This section evaluates
whether lookup table alternatives or algebraic substitutions are warranted.

### 6.4.1 Atan2 Usage and Lookup Table Evaluation

`Atan2` appears in three locations:

| Location | Calls per agent per tick | Purpose |
|----------|--------------------------|---------|
| Step 3B: candidate FoV test | c ≈ 21 | Entity bearing relative to observer |
| Step 4A: shadow cone construction | k ≈ 10 | Occluder bearing from observer |
| Step 4B: entity bearing cache | f ≈ 15 | Entity bearing for shadow cone test (cached, not per-cone) |

Total: approximately **46 `Atan2` calls per agent per heartbeat** (21 + 10 + 15).
At 15 equiv-ops each: **690 equiv-ops** dedicated to `Atan2` — approximately **38%** of
the total per-agent count. This is the dominant trig cost.

**Lookup Table (LUT) Alternative Evaluation:**

A LUT would discretise the (dy, dx) input space into angular bins and replace each `Atan2`
call with a table index computation (~3 ops) plus a table read (~2 ops). Potential saving
per call: 15 - 5 = **10 equiv-ops**. Total potential saving across 46 calls: **460 ops
per agent** — approximately 25% reduction in total per-agent ops.

**Verdict: Reject LUT for Stage 0.**

Reasons, in priority order:

1. **Determinism risk (blocking).** The `Atan2` LUT requires choosing a bin resolution
   (e.g., 0.5° per bin = 720 entries). Any entity whose bearing falls precisely at a bin
   boundary will produce different visibility results depending on rounding direction. This
   creates heartbeat-to-heartbeat visibility flicker for borderline-FoV entities —
   a non-determinism source that violates INV-9 (§2.2.3). The current specification relies
   on continuous-valued `Atan2` output for correct shadow cone boundary tests. A LUT is
   not drop-in compatible without a precision analysis that is outside Stage 0 scope.

2. **No budget pressure (decisive).** The estimated 460 equiv-op saving represents
   approximately 2.3µs per agent at 5ns/op, or **50µs total across all 22 agents per
   heartbeat**. The budget has ~1.6ms of headroom. The saving is below profiler measurement
   noise at the system level.

3. **Maintenance cost.** A LUT requires a pre-computed table (720+ entries), initialisation
   logic, and documentation of the precision contract. For a sub-microsecond saving, this
   complexity is not justified.

**Stage 1+ reconsideration:** If agent count grows substantially (≥ 44 in extended squads)
or if Fixed64 migration (§6.10) raises trig costs by 10×, a LUT with documented precision
bounds may become appropriate. This is flagged in §7 (Future Extensions).

### 6.4.2 Asin Usage

`Asin` is called once per occluder in Step 4A for shadow cone half-angle computation.
Typical call count: k ≈ 10 per agent per tick. Cost: 12 equiv-ops × 10 = **120 ops**,
approximately 6.6% of per-agent total. No optimisation is warranted — low frequency,
not a hot path.

### 6.4.3 Dot-Product Alternative for FoV Test

The Step 3B FoV angular test can be implemented two ways:

- **Option A (current spec):** `Atan2(delta.y, delta.x)` → compare to half-angle
- **Option B:** `dot(normalised_delta, normalised_facing) > cos(EffectiveFoV/2)`

Option B replaces the `Atan2` (15 ops) with a normalisation (9 ops) + dot product (3 ops)
+ comparison (1 op) = **13 ops**, saving 2 ops per candidate. With c = 21 candidates,
total saving: 42 ops per agent — negligible (< 0.3% of total).

However, Option B requires `cos(EffectiveFoV/2)` to be pre-computed once per tick
(12 ops). This amortises to < 1 op per candidate for c > 12.

**Verdict: Either option is acceptable.** The implementation team should choose based
on which produces cleaner code. The choice must be documented in `FovCalculator.cs`
comments, because it affects the boundary behaviour documented in Section 5 §5.2
(FOV-002, FOV-003) — `Atan2`-based and dot-product-based comparisons produce slightly
different results at exactly the boundary due to floating-point representation of
`cos(halfAngle)`. The implementer must confirm that boundary tests pass with their
chosen method.

---

## 6.5 Memory Footprint

### 6.5.1 Per-Heartbeat Stack Usage

No heap allocation occurs during a heartbeat. All pipeline-local data is stack-allocated
or read from pre-allocated buffers.

| Data | Type | Size (bytes) | Allocation |
|------|------|-------------|------------|
| Cached observer attributes (4 values) | Float/int locals | 16 | Stack |
| EffectiveFoV, halfAngle, PressureScalar | Float locals | 12 | Stack |
| Per-occluder shadow cone array (bearing × 21 max, halfAngle × 21 max) | float[21] × 2 | 168 max | Stack fixed-size array |
| Per-entity bearing cache (float × 21 max) | float[21] | 84 max | Stack fixed-size array |
| Confirmed entity ID list (int × 21 max) | int[21] | 84 max | Stack fixed-size array |
| **Total per-agent pipeline stack peak** | | **~364 bytes** | Stack |

This is well within the 1MB C# stack. No stack overflow risk at n ≤ 22.

### 6.5.2 Static Persistent State

Persistent state maintained by `PerceptionSystem` across heartbeats. Allocated once at
match initialisation; no per-heartbeat growth.

| Component | Entries | Bytes per entry | Total (bytes) |
|-----------|---------|-----------------|---------------|
| `_latencyCounters Dictionary<(int,int),int>` | 462 max (22×21) | ~12 (key + value + hash node overhead) | ~5,544 |
| `_blindSideLatencyCounters Dictionary<(int,int),int>` | 462 max | ~12 | ~5,544 |
| `_nextCheckTick Dictionary<int,int>` | 22 | ~8 | ~176 |
| `_checkWindowExpiry Dictionary<int,float>` | 22 | ~8 | ~176 |
| `_blindSideWindowActive bool[]` | 22 | 1 | 22 |
| `PerceptionConstants` static fields (~18 constants) | 18 | 4 | 72 |
| **Total persistent state** | | | **~11,534 bytes (~11.3 KB)** |

**Note on §2.3.3 discrepancy:** Section 2 §2.3.3 estimates "approximately 3.7KB" for
persistent state. The authoritative figure here is **~11.3KB** — 3× larger. The
discrepancy is C# `Dictionary` per-entry node overhead (~8 bytes per entry for the hash
table's linked list node), which §2.3.3 did not account for when estimating raw
key+value storage only. This is documented as KL-4 below; a correction to §2.3.3 is
recommended in its next revision. The 11.3KB figure fits comfortably in L1 cache and
is not a budget concern.

Both latency counter dictionaries are initialised with `capacity: 512` at match start
(next power-of-two above 462) to prevent in-play rehashing.

### 6.5.3 Snapshot Output Memory

One `PerceptionSnapshot` is produced per agent per heartbeat. The Decision Tree receives
these by value; the Perception System's backing buffers are overwritten at the next tick.

**`PerceptionSnapshot` header (fields that are not span-backed arrays):**

| Field | Type | Size (bytes) |
|-------|------|-------------|
| `ObserverId` | int | 4 |
| `FrameNumber` | int | 4 |
| `ForcedRefreshThisTick` | bool | 1 (+3 pad) |
| `BallVisible` | bool | 1 (+3 pad) |
| `BallPerceivedPosition` | Vector2 | 8 |
| `BallStalenessFrames` | int | 4 |
| `VisibleTeammates` (span: pointer + length) | ReadOnlySpan<PerceivedAgent> | 16 |
| `VisibleOpponents` (span: pointer + length) | ReadOnlySpan<PerceivedAgent> | 16 |
| `EffectiveFoVAngle` | float | 4 |
| `PressureScalar` | float | 4 |
| `BlindSideWindowActive` | bool | 1 (+3 pad) |
| `BlindSideWindowExpiry` | int | 4 |
| `BlindSidePerceivedAgents` (span) | ReadOnlySpan<PerceivedAgent> | 16 |
| `ShoulderCheckAnimData` stub (4 fields: int, int, float, bool) | struct | 16 |
| **Header total** | | **~109 bytes (~128 with alignment padding)** |

**`PerceivedAgent` sub-struct (per entry in backing arrays):**

| Field | Type | Size (bytes) |
|-------|------|-------------|
| `AgentId` | int | 4 |
| `PerceivedPosition` | Vector2 | 8 |
| `PerceivedVelocity` | Vector2 | 8 |
| `ConfidenceScore` | float | 4 |
| `IsInBlindSide` | bool | 1 (+3 pad) |
| `LatencyCounterAtConfirmation` | int | 4 |
| **PerceivedAgent total** | | **~32 bytes** |

**Total per-agent snapshot memory (typical 12 confirmed agents):**

```
Header:                       ~128 bytes
PerceivedAgents (12 × 32):    ~384 bytes
BlindSide agents (2 × 32):     ~64 bytes
Total per agent:              ~576 bytes
Total all 22 agents:    22 × 576 = ~12.7 KB per heartbeat
```

All 22 agent snapshots fit within a single 16KB L1 data cache. Sequential Decision Tree
reads of all 22 snapshots will benefit from excellent cache locality.

### 6.5.4 Cache-Line Analysis

| Structure | Size | Cache Lines | Notes |
|-----------|------|-------------|-------|
| `PerceptionSnapshot` header | ~128 bytes | 2 × 64-byte lines | Both lines frequently accessed by DT |
| `PerceivedAgent` | ~32 bytes | 0.5 lines | 2 agents per cache line — good density |
| 22-agent snapshot batch | ~12.7 KB | 199 lines | Fits in L1 data cache (typically 32–64 KB) |
| Persistent Dictionary state | ~11.3 KB | 177 lines | L2 resident; pointer-chase on lookup |

---

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
