# Training System #29 — Section 6: Performance & Cadence

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.3 — PASS-2 re-review; prior APPROVED)
**Version:** 0.3
**Status:** APPROVED

---

## 6.1 Cadence

Training advances on the **world tick** (one day = one `worldTick`), invoked by #30's `AdvanceDay` loop —
**not** the 60 Hz physics or 10 Hz tactical match loops. It is therefore in the off-pitch band and is
exempt from the 60 Hz zero-allocation / `ProfilerMarker` hot-path rules (the #22/#27/#28 off-pitch
precedent).

## 6.2 Per-day cost

Per club per day: one pass over the roster, each player a constant number of integer operations —
`ComputeTrainingInput` (a table lookup + weighted sum), `AdvanceTrainingDay` (two clamped adds), and the
idempotency compare. **No RNG, no allocation in the step**, no rollover loop (KD-4) — the step is O(1) per
player, a single day-delta with no per-day catch-up loop (#30's loop advances one day at a time, so there
is no gap to batch-replay). Serialization is linear in roster size, once per save.

## 6.3 Budget

Off-pitch, once per simulated day — orders of magnitude below any per-tick budget. No perf gate is required
at Stage 0/1; the FR-PO-052 per-tick gate is a match-loop concern and does not apply.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial performance analysis. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | APPROVED. |
| 0.3 | 2026-07-23 | — | PASS-2: §6.2 "gap-independent" wording softened to "one day at a time; a gap fails loud" (F7). |
#endregion
