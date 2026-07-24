# Youth Academy & Intake #42 — Section 6: Performance

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** IN REVIEW

---

## 6.1 Loop classification

#42 runs **exclusively on the world tick** (one call per calendar day per club with an academy), never on
the 10 Hz tactical or 60 Hz physics loops. Per #18 KD-8 loop tagging, every #42 entry point is
`LOOP_TAG_TACTICAL_10HZ`-exempt and `LOOP_TAG_PHYSICS_60HZ`-exempt: **no #42 code is on a hot path**, so
the FR-CS-066 zero-allocation-per-frame budget does not bind, and #42 declares **no**
`[HotPathAllocExempt]` entries.

This matches the #40 / #41 / #31 / #34 off-pitch posture. It is the reason `AcademyState.Cohort` may be a
plain array reallocated at intake, and why the transforms are written for clarity rather than for
zero-alloc.

## 6.2 Cost profile

| Path | Frequency | Cost |
|---|---|---|
| `AdvanceAcademyDay` — trigger miss | once per club per day | Two integer comparisons and a return. This is the overwhelmingly common case (364 of every 365 days at the default period) and is the only #42 cost most days. |
| `AdvanceAcademyDay` — trigger hit | once per club per intake period | One stream anchor + `ACADEMY_INTAKE_COHORT_SIZE` × (`PROGRESSION_REGEN_FIELDS` draws + two O(1) transforms) + one cohort array write. |
| `Promote` | manager-initiated, rare | One linear scan of the cohort (bounded by `ACADEMY_COHORT_SIZE_MAX`) + one array removal. |
| `Encode` / `Decode` | once per save / load | Linear in the cohort size. |

**The dominant term is the intake**, and it is dominated in turn by #28's draw budget, which #42 does not
influence (FR-YA-002 — #42 adds no draw). A cohort of `n` prospects costs exactly
`n × PROGRESSION_REGEN_FIELDS` draws, the same as `n` regens.

## 6.3 Budget

| Subroutine | Loop | Budget | Note |
|---|---|---|---|
| `AdvanceAcademyDay` (miss) | world tick | **≤ 0.001 ms** | Two comparisons; effectively free. |
| `AdvanceAcademyDay` (hit) | world tick | **≤ 2.0 ms** | An intake is a once-a-season event on a non-interactive tick; the budget is generous because a day-advance already runs #28/#29/#33/#41/#31/#34 steps and the user-visible unit is the whole advance, not this step. |
| `Promote` | command | **≤ 0.5 ms** | User-initiated, single-frame. |
| `Encode` / `Decode` | save/load | **≤ 1.0 ms** | Bounded by cohort size; the season save already writes four larger blobs. |

These are **[GT] budget ceilings, not measurements** — no certified number exists or can exist for #42
until the T-phase produces real code to run on the pinned host (`certification-platform.md`). Per #18
FR-PO-052 the authoritative figures come from a certified capture; nothing here is presented as one.

## 6.4 Memory

`AcademyState` per club is a small header plus the cohort. A prospect is one `PlayerRecord`
(#27, 31 attributes) + one `PlayerLifecycle` (#28, six fields) + three academy fields, so a cohort of
`ACADEMY_INTAKE_COHORT_SIZE` is on the order of a single squad's worth of records — negligible beside
the world store. With FR-YA-021 (managed club only at minimal) there is exactly **one** `AcademyState`
at Stage 3.

The save sub-blob is linear in the same quantity and, being one of several season sub-blobs, is not the
`SAVE_SIZE_BUDGET` driver.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §6 (loop classification + hot-path exemption rationale, cost profile, [GT] budget ceilings with the explicit no-certified-number caveat, memory). Status IN REVIEW. |
#endregion
