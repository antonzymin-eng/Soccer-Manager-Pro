# Youth Academy & Intake #42 — Outline

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial, promoted from design supplement v0.3)
**Version:** 0.1
**Status:** APPROVED

---

## Purpose

The **club academy pipeline**: a periodic, deterministic **intake** of youth prospects, the **academy
roster** they live on, and **promotion** into the senior squad — built as a **consumer of #28's
generation machinery**, never a fork of it. Academy structure (facilities + youth coaching) modulates a
prospect's **ceiling** at intake, not the generator and not the daily growth rate. #42 is a **Stage-3
system**; its minimal tier is an **identity over #28** — a neutral-quality cohort is byte-identical to
what `RegenGenerator` alone would produce — so real quality, bio-banding, and youth contracts are the
deep tier on **one code path**.

## Section map

| Section | Content |
|---------|---------|
| 1 | Introduction, scope, out-of-scope seams, dependencies, key decisions (KD-1..KD-8) |
| 2 | Functional requirements (FR-YA-001..028), data structures, failure modes (F1..F7) |
| 3 | Core algorithms: the intake trigger + stream anchor, cohort generation, the two post-generation transforms, promotion. Worked examples |
| 4 | Architecture, assembly/file layout, the `AcademyQuality` input seam, save composition, interface contracts |
| 5 | Test plan (identity + position-independence + save/determinism + fail-loud) |
| 6 | Performance analysis and budgets |
| 7 | Future extensions and T-phase plan (T0–T3) |
| 8 | References and cross-spec cross-references (XC-042-*) |
| 9 | Approval checklist |
| Appendices | Constant catalogue, save-block layout, worked cohort example |

## Governing decisions (see §1)

- **KD-1** — #42 **calls `RegenGenerator.GenerateRegen` unmodified** from its own registered stream (the
  generator is `static`, pure, and takes `streamIndex` as a parameter). No fork, no #28 edit, no shared
  cursor with `player-progression.regen`.
- **KD-2** — the academy quality dial shifts **`PotentialAbility`**, never `CurrentAbility` (a derived
  cache of `AbilityModel.ComputeCA`) and never the attributes (which would be a second path into #28's
  weighted spend/drain model). The clamp floor reproduces `RegenGenerator`'s own `paFloor` verbatim, so a
  shifted prospect still satisfies the generator's "room to grow" postcondition. **KD-2b** — age
  re-anchoring / bio-banding is deep-tier and neutral at minimal.
- **KD-3** — academy structure arrives as an `AcademyQuality` **value input** the composition root
  assembles; #42 references neither #34 nor #40 (FR-LW-031). `default(AcademyQuality)` **is** `Neutral`.
- **KD-4** — the intake is a one-shot latched on **`LastIntakeWorldDay`** with an
  `ACADEMY_INTAKE_PERIOD_DAYS` dial — **#30 exposes no season-year field**, so keying on one would invent
  #30 state. Genesis is an explicit sentinel (world day `0` is a legal day).
- **KD-5** — promotion **emits a `PromotionResult`**; #42 never writes a `Squad` (the FR-PG-012
  discipline). `PlayerId` is stable across promotion — no re-key.
- **KD-6** — one opaque, independently version-gated **season-save sub-blob**
  (`ACADEMY_SAVE_FORMAT_VERSION` [FIXED] = 1) composed into `SeasonSaveCodec`; **no
  `WORLD_STORE_FORMAT_VERSION` bump** (the #41/#33/#34 precedent).
- **KD-7** — **anchor-then-free-run**: one `youth.intake` stream per club, re-anchored per intake from
  `(clubId, intakeWorldDay, purpose)`, so each cohort is position-independent and **no RNG cursor is
  serialized**.
- **KD-8** — behaviour-neutral: a neutral-quality academy reproduces #28's generator exactly, and a
  career with the academy seam null is byte-identical to pre-#42. Exactly **one** approval-time back-prop.

## Back-props

- **At approval:** one — the #30 academy tick-order null-seam slot (**ERR-030-007**).
- **At T-phase (deferred):** the #16 `0x2B`/93 promotion at the first draw; the #30 outer
  `SEASON_SAVE_FORMAT_VERSION` bump; **conditionally** a #16 `SeekStream` seam if §4 declines to
  re-purpose `RestoreStream` for the KD-7 anchor.
- **Never:** #28. KD-1 and KD-2 exist to keep #28 schema-untouched.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial outline, promoted from design supplement v0.3 (AR-converged). Status IN REVIEW. |
#endregion
