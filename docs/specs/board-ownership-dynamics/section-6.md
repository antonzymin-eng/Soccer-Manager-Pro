# Board & Ownership Dynamics #45 — Section 6: Performance

**Created:** July 25, 2026
**Last Updated:** July 25, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** APPROVED

---

## 6.1 Loop classification

#45 runs **exclusively on the world tick** (`WorldClock`, one day = one `worldTick`). It has **no hot
path**: nothing in #45 executes on the 10 Hz tactical loop or the 60 Hz physics loop, and no #45 type is
reachable from `MatchEngine.RunTick` (FR-BD-001, asserted structurally by T-BD-BOUND-001).

The per-tick budget disciplines of #18 therefore do not bind #45's daily step. What does bind it is the
season-advance loop: a career skipping months advances thousands of world days in a burst, so per-day
cost is multiplied by days-elapsed, not by frames.

## 6.2 Cost profile

| Operation | Cadence | Work |
|---|---|---|
| `AdvanceBoardDay` | once per modelled club per world day | 4 range checks, 2 comparisons, ~6 integer ops, 1 store write |
| `TryProjectBoardModifier` | once per club per **season boundary** | 1 lookup, 1 multiply-divide, 1 clamp |
| `DeriveJobSecurityBand` | on read | ≤ 3 comparisons; **allocates nothing**, stores nothing |
| `Encode`/`Decode` | once per save/load | O(modelled clubs) |
| `AdvanceTakeovers` *(deep)* | once per modelled club per world day | + 1 keyed draw |

**Allocation: zero** on every per-day path. The state is value types in a per-`ClubId` store; the daily
step mutates in place through a `ref` and allocates nothing. `BoardViewModel` allocates only when #38
asks for it, off the tick.

At the minimal tier one club is modelled, so a full 365-day season costs on the order of a few thousand
integer operations in total — below measurement noise. The scaling term that matters is **clubs
modelled**, not days: at a hypothetical world-wide deep tier with ~20 clubs per division the daily step
is still ~20 × a handful of integer ops.

## 6.3 Budget ceilings

| Budget | Value | Tag |
|---|---|---|
| `BD_BUDGET_ADVANCE_US` — one club's daily advance | 5 µs | `[GT]` |
| `BD_BUDGET_SEASON_PROJECTION_US` — one club's boundary projection | 5 µs | `[GT]` |

**These are ceilings, not measurements.** No certified number exists for #45 and none is invented here:
a certified figure must come from the pinned Windows 11 / Unity 6000.4.9f1 / DX11 / Mono host per
`certification-platform.md`, and #45 has no implementation to measure. The ceilings are set generously
so that a first measurement either passes comfortably or reveals something genuinely wrong with the
implementation — the `CertifiedPerfBaseline` PENDING posture, applied to a spec that has not been built.

## 6.4 Memory

Per modelled club: `BoardConfidence` (8 bytes), `OwnershipProfile` (16), `TakeoverState` (8) — 32 bytes,
plus store overhead. A world-wide deep tier across, say, 500 clubs is ~16 KB resident. The save sub-blob
is the same order (Appendix B), which is why #45 does not need — and deliberately does not have — the
`SAVE_SIZE_BUDGET` compression machinery #22 carries.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-25 | — | Initial §6 (world-tick-only classification with no hot path, per-operation cost profile with the days-vs-clubs scaling note, `[GT]` budget ceilings with an explicit no-certified-number caveat, memory footprint). Status IN REVIEW. |
#endregion
