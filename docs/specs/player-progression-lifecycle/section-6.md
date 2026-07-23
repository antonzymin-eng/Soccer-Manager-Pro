# Player Progression & Lifecycle #28 — Section 6: Performance

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — section-file PASS-1 (0H+2M) → AR-2 (3M cross-fix) → AR-3 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 6.1 Cadence — off-pitch, not the 60 Hz path

Lifecycle runs on the **world tick** (`WorldClock`, one day = one `worldTick`; FR-PG-001), invoked
by #30's day-advance loop once per advanced calendar day — **not** the 10 Hz tactical or 60 Hz
physics loops. So the zero-allocation / `ProfilerMarker` hot-path rules (Code Standards #20 §game-loop)
**do not apply** — the same carve-out `WorldStore` / `RosterGenerator` / `SquadFileLoader` /
`Squad` take (off-pitch, club-setup / season-cadence code). `ProgressionEngine` may allocate at
setup/boundary time (it manages a roster-sized collection).

## 6.2 Work per advanced day

`AdvanceDay` is O(roster size) integer arithmetic per advanced day: per player, one age derivation
(a single integer division, gap-independent — §3.1.1), one cursor accrual, and at most a few
`POINT_COST` spend iterations (a whole attribute-point per `POINT_COST` accrued — bounded by the daily
point rate, ≪ 1 attribute-point/day at Stage-2). No per-day allocation is required (the roster
collection is pre-owned). A simulated season advances at most `DAYS_PER_SEASON` days × roster size —
trivial at Stage-2 scale (one division per player, `N` clubs × 25).

`RunSeasonBoundary` is O(retirees) — a bounded fraction of the roster per season — each retiree
costing one `PROGRESSION_REGEN_FIELDS` reservation. The lifecycle blob is **bounded by the roster
size** (FR-PG-019, 1:1 vacancy fill), so save size is stable across seasons — the master-plan
save-size concern (§9) is structurally addressed, not merely tuned.

## 6.3 Budget

No per-tick perf gate applies (off the FR-PO-052 60 Hz path). The relevant budget is the
day-advance loop's total, owned by #30 (the integration choke point); #28's contribution is
O(roster) integer work per day, bounded and allocation-free on the steady-state path. A `#19`
capstone (T-PG-SIM-001) can record a wall-clock envelope once wired, but it is non-gating (the
determinism locks are the load-bearing tests, per §5).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial performance section: world-tick cadence carve-out, O(roster) per-day work, bounded save size, no 60 Hz gate. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (0H+2M: M-1 age-model muddle → one BirthWorldDay-derived representation; M-2 per-club regen stream) → AR-2 (3M cross-fix regressions) → AR-3 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
