# Event System Specification #17 — Section 6: Performance Analysis & Budgets

**Created:** May 13, 2026
**Last Updated:** May 13, 2026
**Version:** 0.1 (initial section-file draft from `outline-detailed.md` v1.1)
**Status:** DRAFT

> **Slot reconciliation.** This section IS the CLAUDE.md 9-section
> template's "Performance Analysis" slot. `outline.md` v1.0 placed
> error handling here, which violated the template; PASS 1 finding
> 2 mandates this section be performance + budgets, with error
> handling living in §3.6 / §3.10 instead. Heading order follows
> `outline-detailed.md` v1.1 §"SECTION 6" and supersedes the v0.0
> stub.

---

## 6.1 Complexity Analysis

| Operation | Complexity | Notes |
|-----------|------------|-------|
| `Publish<T>` (Tier A/B) | `O(1)` amortised | Single ring-buffer slot write + `intraPhaseDrawIndex` counter increment. |
| `Publish<T>` (Tier C) | `O(h)` where `h` = subscriber count for that ordinal | Immediate-dispatch over pre-allocated `EventHandler<T>[]`. Indexed `for` loop; no enumerator. |
| `DrainTick` | `O(n log n)` where `n` = events-per-tick | The intra-tick sort (§3.2.4 / FM-017-002) dominates. `n` is bounded by `EVENT_QUEUE_CAPACITY` = 1024. |
| `SerializeLedger` | `O(n)` | Linear walk over the sorted ring buffer; `SerializeCanonical` per record. |
| `OnTickBoundary` | `O(1)` | Zero out `count` + reset 256-row Tier C counter table + reset per-phase `intraPhaseDrawIndex`. |
| `RegisterStartupSubscribers` | `O(s)` where `s` = total Tier A/B subscriber count | One-time boot cost; off the hot path. |

## 6.2 Allocation Budget (KD-8)

| Operation | Allocation budget | Mechanism |
|-----------|-------------------|-----------|
| `Publish<T>` | 0 bytes | `in T evt` parameter; ring-buffer slot is a struct field; counter increment is on a value type. |
| `DrainTick` | 0 bytes | Sort uses `stackalloc` scratch buffer sized to `EVENT_QUEUE_CAPACITY`; dispatch walks `EventHandler<T>[]` via indexed loop. |
| `SerializeLedger` | 0 bytes | Writes to caller-provided `Span<byte>`. `SerializeCanonical` is an `in`-ref API per #16 §3.2.4.1 `TBD-NORMATIVE`. |
| `OnTickBoundary` | 0 bytes | In-place resets. |
| `Subscribe<T>` (boot) | `O(handler-count)` bytes, one-time | Allocates the `EventHandler<T>[]` array at boot; off hot path. |
| `Subscribe<T>` (Tier C runtime) | Bounded one-time per subscriber | Slot in pre-allocated Tier C subscriber overflow array; sizing TBD with first measurements (D-§4.3.2). |

All 0-byte budgets are asserted by §5.3 unit tests using an
allocation tracker (FR-EVT-048, FR-EVT-049, FR-EVT-050).

**Registration-time delegate allocation (acknowledged).** Both
`EventBus.Subscribe<T>` (boot Tier A/B) and
`CosmeticChannel.Subscribe<T>` (runtime Tier C) accept an
`EventHandler<T>` delegate argument. In C#, the method-group →
delegate conversion allocates a `Delegate` instance per
registration. Recent compilers cache the conversion for static
method-group targets, but the caching guarantee depends on the
pinned compiler / runtime (D1 in §7.5; Spec #18). At boot
registration this is off the hot path and one-time. At Tier C
runtime registration the cost is bounded by §3.5.3 / FR-EVT-022
("UI and VFX systems use this surface"): runtime Tier C
`Subscribe` is expected to happen during loading screens / scene
transitions, never inside the simulation tick. A §5.3 unit test
asserts that `Subscribe<T>` invoked inside the tick is rejected
or, if instrumentation-only, that its allocation is bounded.

## 6.3 Worst-Case Publish-Rate Analysis

The constants in §3.10 (`EVENT_QUEUE_CAPACITY`,
`COSMETIC_PER_TICK_PUBLICATION_BUDGET`) are sized from this
analysis.

### 6.3.1 Tier A worst case at 60 Hz

**Per physics tick (event-driven):**
- `BallContactEvent` — ≤ 2 (ball, two players colliding in worst
  case)
- `BallCrossedLineEvent` — ≤ 1 (rare; only when crossing happens)
- Other Resolve-cadence Tier A (`ShotExecutedEvent`,
  `PossessionChangedEvent`, `GoalAwardedEvent`,
  `FoulCommittedEvent`, `CardIssuedEvent`, `SubstitutionEvent`) —
  ≤ 6 aggregate, with margin ×4 for unforeseen.
- Aggregate per-tick physics-cadence ceiling: ≤ 16.

**Per tactical tick (10 Hz; one in six ticks):**
- AI events — assumed **≤ 1 AI event per agent per stride tick**
  (invariant stated explicitly here; per-event-type aggregation
  lives in subscribers per §3.3.4 anti-pattern). 11 players ×
  2 sides = 22 first-order AI events per stride tick. Margin ×2
  → 44 worst case.
- Aggregate per-tactical-tick ceiling: ≤ 48 amortised over six
  ticks ≈ ≤ 8 / tick equivalent.

**Aggregate per-tick first-order Tier A ceiling: ≤ 64.**

**Provisional pending #13–#15 seeding.** The AI-cadence
contribution to this ceiling depends on event types
(`PressTriggeredEvent`, `MarkAssignedEvent`, `RunCalledEvent`)
that §3.3.1 marks as `future — populated at #13/#14/#15
IN REVIEW`. Until those specs land their Appendix A rows, the
≤ 64 first-order ceiling is **provisional**. The ×2 margin
(→ 1024 final capacity) absorbs the expected first three AI
event types; the §6.3.4 re-tuning trigger explicitly fires at
each of #13 / #14 / #15 reaching `IN REVIEW`, with the §3.10
constant re-evaluated against the registry as-seeded.

### 6.3.2 BFS dispatch-depth fanout

Second-order publish from inside a Tier A handler is permitted
(§3.2.5) and is bounded by **two** invariants together:

- `MAX_EVENT_DISPATCH_DEPTH = 8` (depth cap).
- Per-handler out-degree = 1 (FR-EVT-046a / FR-EVT-046b; a single
  Tier A/B handler invocation may publish at most one secondary
  Tier A/B event).

With both caps in force, BFS occupancy is **additive across
levels**, not multiplicative:

```
WorstCaseRingBufferOccupancy
    = first-order ceiling × MAX_EVENT_DISPATCH_DEPTH
    = 64 × 8
    = 512                    # each level adds at most 64 events
                             # because out-degree-per-handler = 1
```

Without the out-degree cap, the worst case would be
`64 × Σ_{i=0..7} k^i` for arbitrary `k`, which is unbounded in
practical terms (e.g., `k = 2 → 64 × 255 = 16,320`). The
FR-EVT-046a out-degree cap is the load-bearing invariant that
keeps `EVENT_QUEUE_CAPACITY` finite.

Doubled for unforeseen second-order amplification (e.g., a future
relaxation of FR-EVT-046a to allow a small out-degree, or
miscounting at the dispatcher):

```
EVENT_QUEUE_CAPACITY = 512 × 2 = 1024  [GT]
```

Headroom is therefore **×2 over the dispatch-depth-bounded
worst case under FR-EVT-046a**, not ×16 over the first-order
ceiling alone (resolves PASS 2 finding 11; resolves PASS 3
finding H1).

### 6.3.3 Tier C worst case at 60 Hz

Peak VFX:

- VFX cues per physics tick — ≤ 32 (boot/contact/sliding heavy).
- UI notification cues — ≤ 16.
- Aggregate ≤ 256 / tick under stress.

`COSMETIC_PER_TICK_PUBLICATION_BUDGET = 4096` `[GT]` is a sanity
ceiling (×16 over 256) on the **sum** of per-ordinal `maxPerTick`
rows in Appendix A. It is NOT a queue capacity — Tier C has no
delivery queue (§3.5.3).

### 6.3.4 Re-tuning trigger

All **runtime-tunable** `[GT]` numbers (see §3.10 note —
`EVENT_QUEUE_CAPACITY`, `COSMETIC_PER_TICK_PUBLICATION_BUDGET`,
`MAX_EVENT_DISPATCH_DEPTH`) are revisited at the following
triggers:

- **Stage 0+1 first measurements** — against the microbenchmark
  suite (D1; parallel to Spec #20 §5.3 numeric re-tuning).
- **Each of #13 / #14 / #15 reaching `IN REVIEW`** — the §6.3.1
  first-order ceiling is recomputed against the as-seeded
  Appendix A registry (resolves L2 from the section-files PASS 1
  critique). If the recomputed ceiling exceeds `EVENT_QUEUE_CAPACITY /
  (MAX_EVENT_DISPATCH_DEPTH × 2)`, the constant is bumped in a
  patch revision.
- **Each new Tier A registry row appended by a future spec** — same
  recompute discipline, run by the spec author at registry-row
  authoring time.

Design-fixed `[GT]` constants (per §3.10 note) are **NOT** subject
to re-tuning. Updates land in a new minor revision of this spec
with a §6.3 version-history entry.

## 6.4 Frame-Budget Contribution (binds to #16 §6 / Spec #18 §4)

Spec #16 §6 budget table currently allocates "Resolve + Events =
18% `TBD-NORMATIVE` per KD-2" of the 60 Hz frame budget
(16.67 ms / frame).

Spec #17 declares its share of that combined `Resolve + Events`
slice:

| Operation | Per-frame budget | Notes |
|-----------|------------------|-------|
| `DrainTick` | ≤ 0.3 ms | Sort + dispatch; bound = `O(n log n)` with `n ≤ 1024`. |
| `SerializeLedger` | ≤ 0.2 ms | Linear walk + canonical encoding. |
| `OnTickBoundary` | ≤ 0.01 ms | In-place resets. |
| **Combined event-system total** | **≤ 0.5 ms / frame** | ≈ 3% of 16.67 ms frame budget. |

The Resolve phase's own share of the 18% combined allocation is
owned by its parent specs (Ball Physics #1, Collision #3,
Pass Mechanics #5, etc.), not #17. The 3% combined event-system
total is well within the `TBD-NORMATIVE` 18% combined slice.

**Performance regression gate thresholds OWNED BY Spec #18 §4 /
§7** (KD-3 parallel of Spec #19 KD-3). Spec #17 declares its
budget; Spec #18 enforces the gate. Spec #18 is currently
`NOT STARTED`, so the gate enforcement is a Stage 0+1+
deliverable.

## 6.5 Instrumentation Budget (KD-11; binds to #16 §8.2)

| Output | Budget | Notes |
|--------|--------|-------|
| Per Tier A publish | ≤ 16 bytes of trace-channel output | One entry per publish; FR-EVT-061. |
| Per Tier C publish | Aggregated per `eventTypeOrdinal` per tick | One trace entry per (tick, ordinal) regardless of count; FR-EVT-062. |
| Per-match instrumentation footprint | ≤ 2 MB uncompressed at peak event rate | FR-EVT-064 (SHOULD); §5.3 soak test verifies. |

- **Trace channel names** declared here (see table below).
- **Trace channel format** cited from #16 §8 `TBD-NORMATIVE`; Spec
  #17 does NOT republish #16's budget numbers (KD-11).

### 6.5.1 Trace channel registry

| Channel name | Tier | Producer | Verbosity default | Purpose |
|--------------|------|----------|-------------------|---------|
| `event-system.tier-a.publish` | A | `EventBus.Publish<IEventA>` | INFO | Per-publish header (`ordinal | tick | subsystemOrdinal | drawIdx`). |
| `event-system.tier-a.digest` | A | `DrainTick` end | DEBUG | Per-tick FM-017-001 byte count summary. |
| `event-system.tier-b.publish` | B | `EventBus.Publish<IEventB>` | INFO | Same as Tier A; activated at Stage 5+. |
| `event-system.tier-c.publish` | C | `CosmeticChannel` | DEBUG | Aggregated `(ordinal, count)` per tick. |
| `event-system.tier-c.drop` | C | `CosmeticChannel` drop predicate | INFO | Per-tick drop count per ordinal (FR-EVT-045). |
| `event-system.overflow` | — | `Publish<T>` raises `ERR_EVT_QUEUE_OVERFLOW` | ERROR | Pre-fail snapshot of ring-buffer state for crash dump. |
| `event-system.tier-mismatch` | — | `Subscribe<T>` rejects (tier-marker mismatch) | ERROR | Subscriber type, attempted tier, registered tier. |
| `event-system.registration-phase` | — | `Subscribe<T>` rejects (Tier A/B registration after boot) | ERROR | Subscriber type, attempted at-boot/post-boot state, current pipeline phase. |

Verbosity defaults are documented at D6 (§7.5 deferred-decision
tracker) and are pinned at Stage 0+1.

## 6.6 Profiling Plan

Stage 1 deliverables:

- **Per-publish microbenchmark suite.** BenchmarkDotNet or
  equivalent — tool pin owned by Spec #18 (D1 in §7.5; Spec #18 is
  `NOT STARTED`).
- **Full-match profile** with allocation tracker asserting zero
  allocations after warm-up. Runs against the `g1-phase-digest-60s`
  fixture (§5.3.2) plus a 90-min variant.
- **Second-order dispatch profile.** Validates that BFS depth in
  realistic play stays well below the `MAX_EVENT_DISPATCH_DEPTH = 8`
  ceiling. If sustained depth exceeds 4, the `[GT]` constant is
  revisited.
- **Tier C drop-predicate rate measurements.** Feeds per-ordinal
  `maxPerTick` re-tuning in Appendix A.

## 6.7 Version History

| Version | Date         | Author      | Notes                                                                 |
|---------|--------------|-------------|-----------------------------------------------------------------------|
| 0.1     | May 13, 2026 | Claude Code | Initial section-file draft from `outline-detailed.md` v1.1. Complexity, allocation budget, worst-case publish-rate derivation (`64 × 8 × 2 = 1024`), frame-budget contribution (≤ 0.5 ms / frame ≈ 3%), instrumentation channel registry published. Section heading order superseded the v0.0 stub. |
| 0.2     | May 13, 2026 | Claude Code | PASS 1 critique resolution. §6.2 added registration-time delegate-allocation acknowledgement (M7). §6.3.1 stated "≤ 1 AI event per agent per stride tick" invariant (L1) and marked AI-cadence ceiling provisional pending #13–#15 (L2). §6.3.2 BFS derivation rewritten to show additivity is load-bearing under FR-EVT-046a out-degree cap (H1). §6.3.4 re-tuning trigger expanded with explicit per-spec hooks (L2). §6.5.1 added `event-system.registration-phase` channel and renamed `producer` column to `subsystemOrdinal` (L3 / M4). |
