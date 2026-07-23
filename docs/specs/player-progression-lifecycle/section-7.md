# Player Progression & Lifecycle #28 — Section 7: Future Extensions & Implementation Plan

**Created:** July 23, 2026
**Last Updated:** July 23, 2026 (v0.2 — section-file PASS-1 (0H+2M) → AR-2 (3M cross-fix) → AR-3 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 7.1 T-phase implementation plan

Behaviour is neutral at every step through T2 (the deep curve is off; the training seam is neutral),
so a season with the engine wired but the dial off ages players by the literal §4.3 step — the KD-8
identity.

- **T0 (value types + the §4.3 identity, behaviour-defining but self-contained).** `PlayerLifecycle`
  / `TrainingInput` / `RetirementResult` / `RegenResult` / `LifecycleViewModel`; `GrowthProjection`
  (§3.1, `curveEnabled` off = the §4.3 step); `AbilityModel` (`ComputeCA` + band classify + weighted
  spend); `RegenGenerator` (§3.3, reusing #27's draw pattern); `PlayerProgressionConstants`. New
  assembly `TacticalDirector.PlayerProgression`. Not yet driven by anything (unit-tested standalone —
  the §5 determinism/identity/regen locks). Unlike a typical behaviour-neutral T0 this *defines* the
  aging behaviour, but it is inert until T2 wires the driver, so it lands with its own reviewed change.
- **T1 (persistence).** `ProgressionSaveCodec` + `ProgressionEngine.Snapshot()/Restore()`
  (`PROGRESSION_SAVE_FORMAT_VERSION` = 1); the §5.7 fail-loud + round-trip locks. The season-save
  composition is a **#30 change** (its root frames the blob + bumps `SEASON_SAVE_FORMAT_VERSION`) —
  co-sequenced, not landed here.
- **T2 (wire the driver).** `ProgressionEngine.AdvanceDay` / `RunSeasonBoundary` wired at #30's
  reserved day-advance seam + season-boundary "advance ages via #28" slot (#30 KD-2 / KD-6). **This
  requires #30 implemented first** (the Wave-1 → Wave-2 ordering guarantees it — #28 T2 must not land
  before #30's seam code exists, or it would wire against a phantom seam). The `progression.regen` RNG
  stream registers here (the first draw site); the #16 `0x20`/82 promotion back-prop lands with it.
  The optional `#19` capstone (T-PG-SIM-001) lands here.
- **T3 (deep tier + #29).** The `curveEnabled` deep CA/PA growth-decline curve (per-attribute, age ×
  position × CA→PA-gap keyed); the #29 training-input consumption (a non-neutral `TrainingInput` from
  #29's producer). The `[GT]` balance pass pins the illustrative magnitudes (the #21 §9.2 precedent).

## 7.2 Stage-3+ deferrals (documented, not built)

- **The deep CA/PA curve** (per-attribute age/position/gap-keyed growth-decline) — the `curveEnabled`
  path; Stage-3. Adds no new draw site if kept deterministic; a stochastic **growth-jitter** dial
  would add an APPEND-only draw on the `progression.*` stream (documented, §4.3).
- **Probabilistic retirement** past a soft threshold (vs. the Stage-2 hard `RETIREMENT_AGE`) — adds an
  APPEND-only draw site; Stage-3.
- **Nation/second-nationality / DOB modeling** for regens beyond the Stage-2 club/nation read — folds
  into #36 (national teams) / #47 (new-game setup) when those land.
- **The #42 youth-academy structure** (facilities → intake quality) modulates regen quality via the
  shared generation machinery — #42's concern; #28 provides the machinery, #42 the quality dial.
- **Valuation** (age/PA → price) is #31; #28 exposes age/PA via `LifecycleViewModel`, #31 consumes it.

## 7.3 The #29 seam contract (recorded for #29's author)

`GrowthProjection`'s daily step takes `in TrainingInput` and is the **sole** attribute-mutation path
(KD-2). #29 becomes the producer that supplies a non-neutral `TrainingInput` per player per day;
training growth is thereby an **input to** #28's single curve, never a parallel mutation (which would
double-count and break the one-code-path invariant). #29 MUST NOT add a second attribute writer — the
`GrowthProjection`-is-sole-writer invariant is a #28 contract #29 consumes, cross-checked at #29's
section-file stage.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial future-extensions + T0–T3 implementation plan; Stage-3 deferrals; the #29 seam contract for #29's author. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (0H+2M: M-1 age-model muddle → one BirthWorldDay-derived representation; M-2 per-club regen stream) → AR-2 (3M cross-fix regressions) → AR-3 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
