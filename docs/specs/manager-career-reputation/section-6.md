# Manager Career, Reputation & Job Market #54 — Section 6: Performance

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 6.1 Loop classification

#54 runs on the **world tick** and at the **season boundary** only. It has **no hot path**: nothing
executes on the 10 Hz tactical or 60 Hz physics loops, no #54 type is reachable from
`MatchEngine.RunTick`, and #54 feeds no digest (FR-MC-002, asserted structurally by T-MC-BOUND-002).

Its cadences are the lowest of any spec in the wave:

- **`EvaluateTenure`** — once per evaluation slot. Whether that is per world day or per season boundary
  is #30's to pin (ERR-030-021); either way it is a handful of comparisons.
- **`Appoint` / `Terminate`** — a few times per **career**, not per season.
- **`ReputationOf`** — on read. This is the one that matters, and §6.2 explains why it still does not.

## 6.2 Cost profile

| Operation | Cadence | Work |
|---|---|---|
| `EvaluateTenure` | once per evaluation slot | 2 range checks, 1 subtraction, ≤ 3 comparisons |
| `Terminate` | a few times per career | 3 field writes |
| `Appoint` | a few times per career | 1 append + 1 index write |
| `ReputationOf` | on read (UI, and any future consumer) | O(tenures × (seasons + trophies)) integer ops |
| `Attractiveness` | per vacancy shown | 4 multiply-divides + 1 clamp |
| `Encode` / `Decode` | once per save / load | O(tenures × seasons), bounded by `MC_MAX_TENURES` |

**`ReputationOf` is recomputed on every read, and that is the KD-2 trade stated as a number.** A career
bounded by `MC_MAX_TENURES` tenures, each with at most a couple of dozen seasons and trophies, is a few
hundred integer operations — **well under a microsecond**, at a cadence measured in UI refreshes. The
alternative — caching it — buys nothing measurable and costs the second-truth failure `ERR-030-009`
documents.

**This is worth stating plainly because performance is the argument a cache will be introduced under.**
The recomputation is not a tolerated inefficiency to be optimised later; it is the design, and §5.2's
structural assertion is what keeps it.

**Allocation: zero** on the evaluation path (comparisons over a `ref`), on `Terminate` (field writes), and
on `ReputationOf` (an integer fold over existing arrays). `Appoint` allocates one array slot when the
tenure list grows. `VacancyView` and the career view model allocate only when #38 asks, off the tick.

**Nothing in #54 scales with world size** — not with clubs, not with players, not with fixtures. It scales
with **career length**, and career length is bounded by `MC_MAX_TENURES` (§6.4).

## 6.3 Budget ceilings

| Budget | Value | Tag |
|---|---|---|
| `MC_BUDGET_EVALUATE_US` — one `EvaluateTenure` | 2 µs | `[GT]` |
| `MC_BUDGET_REPUTATION_US` — one `ReputationOf` over a full career | 50 µs | `[GT]` |
| `MC_BUDGET_ATTRACTIVENESS_US` — one vacancy projection | 2 µs | `[GT]` |

**These are ceilings, not measurements.** No certified number exists for #54 and none is invented here: a
certified figure must come from the pinned Windows 11 / Unity 6000.4.9f1 / DX11 / Mono host per
`certification-platform.md`, and #54 has no implementation to measure. The ceilings are generous so a
first measurement either passes comfortably or reveals something genuinely wrong — the
`CertifiedPerfBaseline` PENDING posture applied to a spec that has not been built.

**`MC_BUDGET_REPUTATION_US` is set at 50 µs deliberately**, an order of magnitude above the expected cost.
It is the number a future contributor will cite when proposing a cache, so it is set where a **real
regression** — a projection that has grown unboundedly, or acquired a non-linear term — trips it, rather
than where ordinary recomputation sits.

## 6.4 Memory

Per tenure: two `uint` days, an `int` club id, a `byte` reason, two `int` counters, plus the `Finishes`
and `Trophies` arrays — on the order of **30 bytes plus 4 bytes per season served and per trophy**. A
twenty-season career across five clubs is **well under a kilobyte**, and the save sub-blob is the same
order (Appendix B).

**#54 is the only spec in the wave whose state grows with career length rather than world size**, and that
is bounded rather than unbounded: `MC_MAX_TENURES` caps the history, and `MC_MAX_SEASONS_PER_TENURE` caps
each entry's arrays. Both are `[GT]`, both are enforced at the write seam, and both exist so a
thirty-year career cannot grow the block without limit.

**Reputation contributes zero bytes** (FR-MC-013) — it is a function, not a field. That is KD-2's payoff
stated as a number, and it is the same shape as #36's *"zero bytes for every unpinned player"*: deriving
rather than storing is cheaper on **both** axes, not a trade.

#54 therefore needs none of the `SAVE_SIZE_BUDGET` compression machinery #22 carries — recorded so its
absence is not read as an omission.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §6 (world-tick + boundary classification with no hot path, cost profile with the `ReputationOf` recomputation costed explicitly — since performance is the argument a cache would be introduced under — `[GT]` ceilings with the reputation budget deliberately set an order of magnitude high so it trips on a real regression rather than on ordinary recomputation, and memory with the two career-length bounds and reputation's zero-byte footprint). Status IN REVIEW. |
#endregion
