# Injuries & Medical #41 — Section 6: Performance & Cadence

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.1 — initial authoring)
**Version:** 0.1
**Status:** APPROVED

---

## 6.1 Cadence

Injury advancement runs on the **world tick** (one day = one `worldTick`), invoked by #30's `AdvanceDay`
loop at the new KD-6 slot — **not** the 60 Hz physics or 10 Hz tactical match loops. It is therefore in the
off-pitch band and is exempt from the 60 Hz zero-allocation / `ProfilerMarker` hot-path rules (the
#22/#27/#28/#29 off-pitch precedent).

## 6.2 Per-day cost

Per club per day: one pass over the roster, each player a constant number of operations —
`AssembleRiskScore` (a weighted sum + a table lookup), the recovery countdown (one clamped subtract), the
idempotency compare, and — only for a player healthy at call entry — exactly **one** keyed RNG draw plus a
deterministic bucketing compare (`ClassifySeverityFromDraw`, no additional draw). **No allocation in the
step**, no rollover loop (the step is per-day and self-contained), no per-day catch-up loop (#30's loop
advances one day at a time, so a gap fails loud rather than batch-replaying, §2.3 F7). The step is O(1) per
player. Serialization is linear in roster size, once per save.

## 6.3 The single draw's cost

The `injuries.occurrence` draw is a single keyed hash evaluation (`DeriveActionOrdinal` + one
`DeterministicRngService` draw) — no reservation loop, no per-evaluation branching cost beyond the
`wasAvailableAtEntry` gate. This is strictly cheaper than the free-running-cursor pattern (no `Reserve`/
`CloseReservation` bracket needed, since the draw is keyed rather than reserved against an advancing
cursor).

## 6.4 Budget

Off-pitch, once per simulated day — orders of magnitude below any per-tick budget. No perf gate is required
at Stage 0/1; the FR-PO-052 per-tick gate is a match-loop concern and does not apply.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial performance analysis: cadence, per-day cost, single-draw cost, budget. Status IN REVIEW. |
#endregion
