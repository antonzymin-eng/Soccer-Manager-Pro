# Club Finances & Economy #40 — Section 6: Performance & Cadence

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.1 — initial authoring)
**Version:** 0.1
**Status:** APPROVED

---

## 6.1 Cadence

`SettleFinances` runs **once per club, once per season**, invoked by #30's `RollToNextSeason()` at the new
KD-6 step (b') — **not** the world tick (`WorldClock`), and **not** the 60 Hz physics or 10 Hz tactical match
loops. This is an even coarser cadence than #28/#29/#41's per-world-day steps: #40's minimal tier has no
per-day component at all (KD-1). `ApplyTransaction` runs on demand, whenever #31/#34 commit a deal — bounded
by transfer-window/contract activity, not any tick loop. Both are therefore in the off-pitch band and exempt
from the 60 Hz zero-allocation / `ProfilerMarker` hot-path rules (the #22/#27/#28/#29/#41 off-pitch
precedent).

## 6.2 Per-season cost

Per club per season boundary: one `SettleFinances` call — a fixed number of integer operations
(`PrizeMoneyForPosition`'s interpolation, two budget-ceiling projections, a carry-forward add). **No
allocation**, no draw, no reservation loop, no per-day rollover (the minimal tier has nothing to roll over
within a season). The step is O(1) per club; the full season-boundary finance pass is O(`clubCount`), once
per season. Serialization of the finance block is linear in club count, once per save.

## 6.3 `ApplyTransaction` cost

`ApplyTransaction` is a fixed number of integer comparisons/additions per call — no allocation, no RNG, no
loop. Its call frequency is bounded by deal/contract activity (#31/#34), which is orders of magnitude below
even the world-tick cadence, let alone the match loops.

## 6.4 Budget

Off-pitch, at most once per simulated season for `SettleFinances` and on-demand for `ApplyTransaction` —
orders of magnitude below any per-tick or even per-world-day budget. No perf gate is required at Stage 0/1;
the FR-PO-052 per-tick gate is a match-loop concern and does not apply. The (deferred) deep-tier per-day
revenue accrual, if it lands, would add an O(`clubCount`) daily step — still off-pitch and cheap, comparable
to #41's per-day cost analysis.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial performance analysis: cadence, per-season cost, `ApplyTransaction` cost, budget. Status IN REVIEW. |
#endregion
