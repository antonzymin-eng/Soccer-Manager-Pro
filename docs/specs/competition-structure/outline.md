# Competition Structure #43 — Outline

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.2 — cross-set AR pass 3; prior v0.1 initial)
**Version:** 0.2
**Status:** APPROVED

---

## Purpose

**The competition set as a first-class collection**: cups, continental competitions, and
promotion/relegation over the season loop #30 ships — multiple concurrent competitions, knockout
brackets with **deterministic keyed draws**, and a **season-boundary promotion/relegation
transform** at the insertion point #30 pre-declared (FR-SN-031 (a')). The Stage-2 minimal tier is
the **singleton-collection identity** (the #30 league as a binding row — byte-identical season,
draw-free); the deep tier populates the collection, reusing #30's instance-ready
`FixtureScheduler`/`LeagueTable` machinery per instance on **one code path**.

## Section map

| Section | Content |
|---------|---------|
| 1 | Introduction, scope, out-of-scope seams, dependencies, key decisions (KD-1..KD-8) |
| 2 | Functional requirements (FR-CP-001..027), data structures, failure modes (F1..F6) |
| 3 | Core algorithms: registry/formats, keyed draws, bracket lifecycle, promotion/relegation, merged calendar |
| 4 | Architecture, assembly/file layout, the instance-0 binding, save composition |
| 5 | Test plan (identity + draw determinism + bracket coherence + transform + save/fail-loud) |
| 6 | Performance analysis and budgets |
| 7 | Future extensions and T-phase plan (T0–T3) |
| 8 | References and cross-spec cross-references (XC-043-*) |
| 9 | Approval checklist |
| Appendices | Constant catalogue, save-block layout, worked draw + promotion examples |

## Governing decisions (see §1)

- **KD-1** — a league IS a competition instance (`CompetitionFormat.RoundRobin`); **instance 0 is a
  binding row** (an id/tag — "the league lives in #30"; no stored #30 object; instance-0 reads via
  the composition root against #30's read surface).
- **KD-2** — knockout/group draws are **position-independent keyed draws** (`competition.draws`,
  `entityId = competitionId`, fixed-radix ordinals) — a revision of the plan's serialized-cursor
  proposal; nothing RNG-related serializes; the minimal tier makes zero draws.
- **KD-3** — brackets are **persisted, not regenerated** (rounds' entrant lists + winners;
  fail-loud coherence gates); a restore never re-rolls a draw.
- **KD-4** — promotion/relegation is a pure membership-only transform at the pre-declared (a'),
  before #40's (b'); `ClubId`s never re-key; the code-side (a') hook is a T-phase #30 coordination
  (soft-reserved ERR-030-008).
- **KD-5** — concurrent scheduling via a #43-owned **merged fixture-day view** (deterministic
  congestion-free slotting); #30's `SeasonCalendar` untouched; queried only when >1 competition.
- **KD-6** — one `COMPETITION_SAVE_FORMAT_VERSION` season-save sub-blob; instance 0 never
  duplicated; no `WORLD_STORE_FORMAT_VERSION` bump.
- **KD-7** — **canonical entrant ordering** (ascending `ClubId`) at every draw-feeding surface;
  the drawn permutation is keyed Fisher–Yates over that base.
- **KD-8** — behaviour-neutral identity: the singleton collection ⇒ byte-identical season; no
  stream; `_RESERVED_0x2C_`/94 stays reserved.

## Back-props

- **At approval:** one — the #16 §3.4 A-04 placeholder sweep (**ERR-043-001**:
  `_RESERVED_0x2B_` #42 / `_RESERVED_0x2C_` #43 / `_RESERVED_0x2D_` #45, completing the roadmap §6
  block). **No #30 back-prop at approval** — the (a') point + FR-SN-031 were pre-declared.
- **At T-phase (deferred):** the #30 outer `SEASON_SAVE_FORMAT_VERSION` bump (T1); the #30
  code-side coordinations (soft-reserved ERR-030-008 — the (a') execution hook at T2, the deep
  fixture-day driver at T3); the #16 `DOMAIN_TAG_COMPETITION = 0x2C` promotion at the first
  knockout draw (T3).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial outline, promoted from design supplement v0.3 (AR-converged). Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Cross-set AR pass 3: FR range → 027 (FR-CP-026 keyed knockout tie-break; FR-CP-027 power-of-two config gate). |
#endregion
