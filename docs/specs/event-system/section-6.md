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
- AI events (one per player) — ≤ 22 (11 per side; one of each
  type). Margin ×2 → 44 worst case.
- Aggregate per-tactical-tick ceiling: ≤ 48 amortised over six
  ticks ≈ ≤ 8 / tick equivalent.

**Aggregate per-tick first-order Tier A ceiling: ≤ 64.**

### 6.3.2 BFS dispatch-depth fanout

Second-order publish from inside a Tier A handler is permitted
(§3.2.5; bounded by `MAX_EVENT_DISPATCH_DEPTH = 8`). Worst-case
BFS fanout under the depth cap:

```
WorstCaseRingBufferOccupancy
    = first-order ceiling × MAX_EVENT_DISPATCH_DEPTH
    = 64 × 8
    = 512
```

Doubled for unforeseen second-order amplification:

```
EVENT_QUEUE_CAPACITY = 512 × 2 = 1024  [GT]
```

Headroom is therefore **×2 over the dispatch-depth-bounded
worst case**, not ×16 over the first-order ceiling alone (resolves
PASS 2 finding 11).

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

All `[GT]` numbers are revisited at Stage 0+1 against first real
measurements (parallel to Spec #20 §5.3 numeric re-tuning). The
microbenchmark suite (D1) provides the measurements; updates are
made in a new minor revision of this spec with §6.3 version-
history entry.

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
| `event-system.tier-a.publish` | A | `EventBus.Publish<IEventA>` | INFO | Per-publish header (`ordinal | tick | producer | drawIdx`). |
| `event-system.tier-a.digest` | A | `DrainTick` end | DEBUG | Per-tick FM-017-001 byte count summary. |
| `event-system.tier-b.publish` | B | `EventBus.Publish<IEventB>` | INFO | Same as Tier A; activated at Stage 5+. |
| `event-system.tier-c.publish` | C | `CosmeticChannel` | DEBUG | Aggregated `(ordinal, count)` per tick. |
| `event-system.tier-c.drop` | C | `CosmeticChannel` drop predicate | INFO | Per-tick drop count per ordinal (FR-EVT-045). |
| `event-system.overflow` | — | `Publish<T>` raises `ERR_EVT_QUEUE_OVERFLOW` | ERROR | Pre-fail snapshot of ring-buffer state for crash dump. |
| `event-system.tier-mismatch` | — | `Subscribe<T>` rejects | ERROR | Subscriber type, attempted phase, current phase. |

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
