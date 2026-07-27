# Club Infrastructure & Facilities #53 — Section 6: Performance

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 6.1 Loop classification

#53 runs **exclusively on the world tick** (`WorldClock`, one day = one `worldTick`). It has **no hot
path**: nothing in #53 executes on the 10 Hz tactical loop or the 60 Hz physics loop, and no #53 type is
reachable from `MatchEngine.RunTick` (FR-IN-001, asserted structurally by T-IN-BOUND-001).

The per-tick budget disciplines of #18 therefore do not bind #53's daily step. What *does* bind it is the
season-advance loop: a career skipping months advances thousands of world days in a burst, so per-day
cost is multiplied by **days elapsed × clubs modelled**, not by frames.

## 6.2 Cost profile

| Operation | Cadence | Work |
|---|---|---|
| `AdvanceFacilityDay` | once per modelled club per world day | 1 lookup, 1 sentinel comparison; **for the overwhelming majority of days it exits on the first branch** — a club is idle or mid-build far more often than it completes |
| `CanStartUpgrade` | once per upgrade command | 1 lookup, 1 range check, 3 comparisons |
| `StartUpgrade` | once per accepted command | the predicate again (FR-IN-013), 1 multiply, 1 add, 3 field writes |
| `ProjectAcademyQuality` / `ProjectMedicalModifier` / `ProjectTrainingTerm` | once per club per consumer per day | 1 lookup, 1 subtract, 1 multiply, 1 clamp |
| `StadiumCapacity` | on read (deferred consumer) | 1 lookup, 1 subtract, 1 multiply, 1 add |
| `Encode` / `Decode` | once per save / load | O(modelled clubs) |

**The completion branch is the rare one, and that is the point of KD-3.** A remaining-days counter would
do work *every* day for *every* club with a build in progress; a stored completion day does a single
`uint` comparison and returns. The design decision that makes completion restore-safe also makes it the
cheapest possible daily step.

**Allocation: zero** on every per-day path. State is a value type in a per-`ClubId` store; the advance
mutates in place through a `ref`; the projections return small `readonly struct`s by value.
`FacilityViewModel` allocates only when #38 asks for it, off the tick.

At the minimal tier one club is modelled, so a full 365-day season costs on the order of a **thousand
integer operations in total** — below measurement noise. The scaling term that matters is **clubs
modelled**: at a hypothetical world-wide deep tier of ~500 clubs the daily step is still ~500 lookups and
comparisons, and a decade-long career at that scale is a few tens of millions of integer operations
spread across ~3 650 days.

## 6.3 Budget ceilings

| Budget | Value | Tag |
|---|---|---|
| `FACILITY_BUDGET_ADVANCE_US` — one club's daily advance | 5 µs | `[GT]` |
| `FACILITY_BUDGET_PROJECTION_US` — one club's single projection | 2 µs | `[GT]` |

**These are ceilings, not measurements.** No certified number exists for #53 and none is invented here: a
certified figure must come from the pinned Windows 11 / Unity 6000.4.9f1 / DX11 / Mono host per
`certification-platform.md`, and #53 has no implementation to measure. The ceilings are set generously so
that a first measurement either passes comfortably or reveals something genuinely wrong with the
implementation — the `CertifiedPerfBaseline` PENDING posture, applied to a spec that has not been built.

## 6.4 Memory

Per modelled club: four `int` levels (16 bytes), `InProgressFacility` + `TargetLevel` (8),
`CompletionWorldDay` (4) — **28 bytes** plus store and array overhead. A world-wide deep tier across 500
clubs is well under 100 KB resident, and the save sub-blob is the same order (Appendix B).

That is why #53 does not need — and deliberately does not have — the `SAVE_SIZE_BUDGET` compression
machinery #22 carries, nor any of the cold-store / rehydrate apparatus. Recorded explicitly so a later
reviewer does not read its absence as an omission.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §6 (world-tick-only classification with no hot path, per-operation cost profile with the KD-3 rare-completion-branch note, `[GT]` budget ceilings with an explicit no-certified-number caveat, memory footprint and why no compression apparatus is needed). Status IN REVIEW. |
#endregion
