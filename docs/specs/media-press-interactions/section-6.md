# Media & Press Interactions #35 — Section 6: Performance

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 6.1 Loop classification

#35 runs on the **world tick** and the **#30 post-round path** only. It has **no hot path**: nothing in
#35 executes on the 10 Hz tactical loop or the 60 Hz physics loop, no #35 type is reachable from
`MatchEngine.RunTick`, and **#35 feeds no digest at all** (FR-ME-001, asserted structurally by
T-ME-BOUND-001).

The per-tick budget disciplines of #18 therefore do not bind #35. Two scaling terms do:

- **The post-round call is per fixture, not per managed fixture.** #30 invokes `TryQueueConference` for
  **every** fixture in a round — 190 calls in a 20-club round — and all but one must do nothing. This is
  why the managed-club gate is step 1 of §3.1 rather than a filter later: the common path is a single
  integer comparison and an early `false`.
- **The drain is per player per day.** `TryTakePendingDelta` is called for every player #30 iterates at
  step 3, and returns `false` on the overwhelming majority. Absence must therefore be cheap and must not
  be exceptional — which is why F8's *"not a failure mode"* classification is a performance property as
  well as a correctness one.

## 6.2 Cost profile

| Operation | Cadence | Work |
|---|---|---|
| `TryQueueConference` — **non-managed fixture** | ~189× per round | **1 integer comparison**, then return |
| `TryQueueConference` — managed fixture | 1× per round | 1 comparison, 1 count check, 3 comparisons for the archetype, 1 catalogue lookup, 1 append |
| `TryTakePendingDelta` — **no pending row** | once per player per day | 1 lookup over a queue bounded by `MEDIA_MAX_PENDING_CONFERENCES`; the common case |
| `TryTakePendingDelta` — hit | rare | + 1 removal |
| `AdvanceMediaDay` | once per world day | 1 cursor comparison, then ≤ `MEDIA_MAX_PENDING_CONFERENCES` `uint` comparisons |
| `TryAnswerQuestion` | once per player command | 1 lookup, 3 comparisons, 1 catalogue lookup, ≤ `MEDIA_MAX_CONSEQUENCE_TARGETS` appends |
| `SelectionValue` | once per rendered item, **display-side** | 4 SplitMix64 rounds — no lookup, no allocation, no state |
| `Encode` / `Decode` | once per save / load | O(pending conferences + pending deltas), both bounded |

**Everything #35 iterates is bounded by a `[GT]` constant**, which is the structural reason it has no
scaling risk: the pending queue is capped at enqueue (F7 drops rather than growing), the option list at
`MEDIA_MAX_OPTIONS`, the consequence list at `MEDIA_MAX_CONSEQUENCE_TARGETS`, and pending deltas are
cleared on delivery and dropped on departure (F9). There is no unbounded collection anywhere in the spec.

**Allocation: zero** on every per-day and per-fixture path. State is value types in a bounded store; the
lifecycle mutates in place through a `ref`. `ConferenceView` allocates only when #46 or #38 asks for it,
off the tick. `SelectionValue` runs display-side, after the deterministic decision, and allocates nothing.

A full 38-round season costs on the order of **7 000 early-return comparisons** for the queue seam, plus
365 expiry sweeps over a queue that is usually empty — below measurement noise by orders of magnitude.

## 6.3 Budget ceilings

| Budget | Value | Tag |
|---|---|---|
| `MEDIA_BUDGET_QUEUE_US` — one `TryQueueConference` call | 5 µs | `[GT]` |
| `MEDIA_BUDGET_EXPIRY_US` — one day's expiry sweep | 10 µs | `[GT]` |
| `MEDIA_BUDGET_DRAIN_US` — one `TryTakePendingDelta` call | 2 µs | `[GT]` |

**These are ceilings, not measurements.** No certified number exists for #35 and none is invented here: a
certified figure must come from the pinned Windows 11 / Unity 6000.4.9f1 / DX11 / Mono host per
`certification-platform.md`, and #35 has no implementation to measure. The ceilings are set generously so
that a first measurement either passes comfortably or reveals something genuinely wrong — the
`CertifiedPerfBaseline` PENDING posture applied to a spec that has not been built.

**`MEDIA_BUDGET_DRAIN_US` is the one that could bite**, and it is the one to measure first: it is
multiplied by every player #30 iterates, every day, for the whole career. A linear scan over the pending
queue is fine at `MEDIA_MAX_PENDING_CONFERENCES = 8`; it would not be at 800, which is one more reason the
cap is `[GT]`-bounded rather than open.

## 6.4 Memory

Per pending conference: ids and days (~28 bytes) plus a bounded `MediaIntent[]` of at most
`MEDIA_MAX_OPTIONS` `int`s. Per pending delta: 13 bytes. With both collections capped, #35's resident
footprint is **under a kilobyte** at the minimal tier and the save sub-blob is the same order (Appendix B).

**Nothing in #35 grows with career length**, and that is a property the design maintains deliberately
rather than one that happens to hold: deltas are cleared on delivery, dropped on departure (F9), and never
written when zero (FR-ME-033). Remove any one of those three and the APPEND-only blob grows without bound
across a decade-long career — which is why each is a MUST rather than an optimisation.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §6 (world-tick + post-round classification with no hot path, the two scaling terms — per-fixture queue calls and per-player drain calls — the cost profile, `[GT]` budget ceilings with an explicit no-certified-number caveat and a note on which one to measure first, memory footprint and the three MUSTs that keep it bounded). Status IN REVIEW. |
#endregion
