# Staff & Backroom #34 — Outline

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.1 — initial, promoted from design supplement v0.4)
**Version:** 0.1
**Status:** APPROVED

---

## Purpose

The **backroom**: coaches, scouts, and physios modelled as **attributed entities** with roles/skills and
(deep) hiring — that **modulate** the systems they support (#29 training, #41 injuries/medical, #33
mentoring, #31 valuation) — advanced on the world tick, constrained by #40's club budgets, and persisted
alongside the season/career save. #34 is a **Stage-3 system**; its pulled-forward floor is an **identity
scaffold** — a real neutral-baseline staff roster whose quality projections are exactly the identity each
consumer already defaults to — so #34 lands **behaviour-neutral**, with real attributes / hiring / wages as
the deep tier on **one code path**.

## Section map

| Section | Content |
|---------|---------|
| 1 | Introduction, scope, out-of-scope seams, dependencies, key decisions (KD-1..KD-8) |
| 2 | Functional requirements (FR-ST-001..024), data structures, failure modes (F1..F6) |
| 3 | Core algorithms: staff-quality projections, the neutral baseline + seeding, hiring (deep), the #40 wage boundary (deep) |
| 4 | Architecture, assembly/file layout, the projection seam, save composition |
| 5 | Test plan (behaviour-neutral identity + projection + save/determinism + fail-loud) |
| 6 | Performance analysis and budgets |
| 7 | Future extensions and T-phase plan (T0–T3) |
| 8 | References and cross-spec cross-references (XC-034-*) |
| 9 | Approval checklist |
| Appendices | Constant catalogue, save-block layout, worked projection example |

## Governing decisions (see §1)

- **KD-1** — hiring reuses #31's `NegotiationOutcome` enum + the validate-all-first atomic-commit **pattern**,
  but a thin staff-specific `StaffOffer`/`EvaluateStaffOffer` (the negotiated quantity is a **wage**, not a
  fee — #31's `EvaluateOffer` tests `Fee`); all hiring is deep-tier, year-round (no window).
- **KD-2** — `StaffRecord`/`StaffAttributes` are a fresh #34-owned data layer (a distinct staff-skill
  vocabulary, not #27's `PlayerAttributes`) organised as per-club **role slots**; keyed by a stable,
  serialized, monotonic `StaffId`.
- **KD-3** — staff-quality projections return each consumer's **own pre-existing identity type**
  (`MedicalModifier`/`CoachingModifier`/`staffMult`/`MentoringPlan`); neutral baseline ⇒ each type's exact
  `Identity`; #34 invents no new multiplier convention and is the sole staff path into each consumer.
- **KD-4** — draw-free scaffold (`0x26`/88 stays reserved); one `STAFF_SAVE_FORMAT_VERSION` season-save
  sub-blob (no `WORLD_STORE_FORMAT_VERSION` bump); deep candidate-pool draws are keyed, no cursor.
- **KD-5** — the neutral baseline is a **real** neutral house-staff entity projecting explicit `Identity`,
  never an absence sentinel; `default(StaffRecord)`/`default(StaffAttributes)` all-zero fails loud (F4).
- **KD-6** — staff wages are deep-tier; the scaffold posts no `StaffWage` (FR-FN-015 preserved verbatim); the
  deep affordability gate reads #40's running `WageBillAggregate` directly — **no #34 wage counter**.
- **KD-7** — staff keep a **stable `StaffId`** + a mutable `EmployerClubId`, so a hire never re-keys; **no
  #30 roster-commit, no cross-system migration hook** (a simpler divergence from #31's KD-7).
- **KD-8** — behaviour-neutral identity scaffold (neutral staff ⇒ Identity ⇒ byte-identical season); the one
  approval-time back-prop is the reserve-ahead #30 staff tick-order null seam (ERR-030-006).

## Back-props

- **At approval:** one — the #30 staff tick-order null-seam slot (ERR-030-006). `0x26`/88 stays reserved
  (draw-free scaffold); #41/#29/#40/#33/#31/#27/#16 unchanged.
- **At T-phase (deferred):** the #30 outer `SEASON_SAVE_FORMAT_VERSION` bump; the #29 `CoachingModifier`
  field-shape + consumption (ERR-029-002); the #40 FR-FN-015 relax + `WageBudget` gate (shared ERR-040 with
  #31); the #16 `0x26`/88 promotion at the first candidate-pool draw (ERR-016).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial outline, promoted from design supplement v0.4 (AR-converged). Status IN REVIEW. |
#endregion
