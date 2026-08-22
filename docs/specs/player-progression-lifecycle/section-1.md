# Player Progression & Lifecycle #28 — Section 1: Introduction, Scope, Dependencies, Key Decisions

**Created:** July 23, 2026
**Last Updated:** August 22, 2026 (v0.4 — **ERR-028-020**: §1.3's staging split and KD-8 revised. The age-continuity of the growth rate moves from the Stage-3 promise to the Stage-2 shipped tier (§3.1.3), because the tier as described was a cliff rather than a simplification of the curve §1.3 promised; KD-8's byte-for-byte identity moves onto the new ramp dial's OFF position. What stays Stage-3 is the `(PA − CA)` magnitude modulation and per-attribute weighting. Prior entry below.)
**Last Updated (prior):** July 27, 2026 (v0.3 — back-prop landed atomically with the ten-spec approval wave; see the version-history row)
**Last Updated (prior):** July 23, 2026 (v0.2 — section-file PASS-1 (0H+2M) → AR-2 (3M cross-fix) → AR-3 convergence; APPROVED)
**Version:** 0.4
**Status:** APPROVED

---

## 1.1 Introduction

This spec makes a **multi-season career** true. Today a played season ages no one, retires no one,
and produces no regens: #27 `PlayerRecord.Age` is a generation-time constant, `PlayerAttributes`
never change, and #30's day-advance loop reserves an *empty* seam for progression (#30 KD-2 / KD-6).
This spec fills that seam with player lifecycle on the **world tick** (`WorldClock`, one day = one
`worldTick`; never the 10 Hz/60 Hz match loops): aging, attribute growth/decline via a CA/PA model,
retirement, and regen/newgen production.

It is authored as a **Stage-1-forward pull** (the #21/#22/#27/#30 precedent): the master development
plan (§4.3/§5) places aging + youth at Stage 2–3, but #30 already reserves the seam, so #28 fills it
now, as the **identity** the deep tier modulates rather than a throwaway.

## 1.2 Scope

**In scope:** the per-day progression step #30 invokes (aging + decline + growth), the CA/PA model
over #27's `PlayerAttributes`, retirement (world-tick evaluation; roster removal deferred to the
season boundary), regen/newgen production reusing #27's `RosterGenerator`-class draw machinery, the
shared growth-input seam #29 (training) writes, and the #28-owned career-state persistence block.

**Out of scope** (each its own spec): the youth-academy *structure* (facilities → intake quality) is
**#42**, and the **facility level it consumes is #53's** (ERR-028-002, at #53's approval — recorded so a
reader does not attribute the facility model to #40, whose scope excludes it; #28's own out-of-scope
position is unchanged either way); training-driven growth *input* is **#29** (a shared seam #28 defines, not a duplicate
mutation — KD-2); valuations/price are **#31**; the day-advance loop and season-save composition
that *drive* #28 are **#30** (#28 exposes the step; #30 invokes it — never the reverse). Where #28
must reference a spec that does not exist yet (#29's producer), it does so through a **method input
defaulted to neutral** — never an invented interface (FR-LW-031: no phantom interfaces).

## 1.3 Staging — one code path, a config dial

Stage-2 minimal is the master plan §4.3 rule as a **deterministic per-day projection**: age advances
one year per `DAYS_PER_YEAR` world-days; a player over `DECLINE_AGE` loses ≈1 attribute-point/year,
under `GROWTH_AGE` gains ≈1/year, and at `RETIREMENT_AGE` (36) retires. Stage-3 deep replaces the
flat step with per-attribute CA/PA growth-decline curves keyed to age, position, and (via #29)
training. **One code path, a config dial (KD-8):** with `curveEnabled` off **and
`AGE_BAND_RAMP_HALF_WIDTH_YEARS = 0`** the daily growth function reproduces the literal §4.3 step
exactly (digest-locked); with it on, the deep curve modulates it.

**Revised at ERR-028-020 (August 22, 2026).** The paragraph above described the Stage-2 tier as a
three-way band step with hard edges at `GROWTH_AGE`, `DECLINE_AGE` and `RETIREMENT_AGE`, and then
promised that "per-attribute CA/PA growth-decline curves keyed to age" were the Stage-3 tier's. The
football-judgment proxy review found the two halves in tension: the age-keyed curve was named as a
Stage-3 deliverable while §3 contained no age-continuous formula at all, so the shipped tier was not a
simplification of a curve — it was a cliff, and one whose 1-day discontinuity is visible in a career.
**The age-continuity of the rate is now Stage 2** (§3.1.3's centred ramps, and §3.4's per-player
retirement day under ERR-028-021), and what remains Stage-3 is what always distinguished the deep tier:
the `(PA − CA)` magnitude modulation and per-attribute weighting (§3.2). The KD-8 identity is not lost —
it moves onto the ramp dial's OFF position, which reproduces the retired step byte-for-byte.
The `[GT]` magnitudes are illustrative pending a Stage-2/3 balance pass (the #21 §9.2 / #30 precedent
— the spec's contract is the shapes, not the tuned numbers).

## 1.4 Dependencies

- **Upstream (needs):** #27 (`PlayerRecord` / `PlayerAttributes` / `RosterGenerator` / `Squad` /
  `PlayerDatabaseConstants` + the roster world), Deterministic Simulation #16 (the `progression.*`
  RNG sub-stream + `CanonicalSerializer`), #30 (the day-advance loop + season-boundary roll that
  **invoke** #28's step, and the season save that **composes** #28's blob).
- **Downstream (consumers):** #29 training (writes the growth-input seam), #42 youth academy (shares
  the regen machinery), #31 valuations (age/PA feed price), #38 UI (a read-only lifecycle view model).

**Reference direction (load-bearing):** #30 depends on #28 (calls its step); #28 does **not** depend
on #30. #28 references #27's public `player-database` surface + #16 only. The season-save composition
lives at #30's root (the only assembly above both), so #28 never references the season assembly and
#30 never reaches into #28 internals (FR-LW-003-class hygiene; the #26-gate-invoked-by-the-engine
precedent).

## 1.5 Key decisions

- **KD-1 (byte-exact fractional-daily projection — no float, no double-count; ONE ability model).**
  The `PlayerAttributes` `[1,20]` values are the **single source of truth**; a per-player
  **`GrowthCursor` (integer fixed-point points pool)** is the **only** accumulator; `CurrentAbility`
  (CA) is a **derived** weighted summary (recomputed, never a second accumulator); `PotentialAbility`
  (PA) is the ceiling. Age is **derived** from a single serialized `BirthWorldDay` anchor (no discrete
  rollover step — attribute change is the cursor alone), so everything is integer and a mid-year
  save→restore is byte-exact with nothing to double-count. Full algorithm in §3.1.
- **KD-2 (the #29 shared growth-input seam — training is an input, one code path).** `GrowthProjection`
  is the sole attribute-mutation path; #29 supplies a per-player `TrainingInput` the daily step
  **reads** (a method parameter defaulted to `Neutral`), never a parallel mutation. No interface
  against the absent #29 producer (FR-LW-031; the #21 routing-field-seeded-to-identity precedent).
- **KD-3 (regens — day-deterministic, fresh `PlayerId`, #27's roster world).** `RegenGenerator`
  produces one `PlayerRecord` per vacancy from the `progression.regen` stream (reusing #27's
  fixed-budget draw pattern), with a **fresh monotonic `PlayerId`** (never a retiree's — the block
  keys on `PlayerId`); club/nation are read from #27's roster world; #28 emits a `RegenResult` the
  roster owner applies (§3.3).
- **KD-4 (career-state ownership — a #28-owned block, not a #27 record change).** #28 owns a block
  keyed by `PlayerId` holding the **complete career-state `PlayerRecord` set** (identity + evolving
  attributes) **plus** the lifecycle overlay, serialized under `PROGRESSION_SAVE_FORMAT_VERSION`.
  #27's canonical struct gains **no** CA/PA fields (§4.1). Rationale: #27's on-disk roster is
  Stage-1+ deferred and #30 KD-5 rejects regeneration-on-load, so the career-state roster must be
  *serialized* — and #28 is its single over-time writer.
- **KD-5 (retirement → roster-removal — season-boundary, never mid-fixture).** Retirement is
  evaluated on the world tick (hard at `RETIREMENT_AGE`) and only **flagged**; roster removal + regen
  replacement land at the **season boundary** (#30's KD-6 roll). A flagged player stays selectable
  until the season ends (§3.4).
- **KD-6 (the season-boundary step).** `RunSeasonBoundary` does **not** re-bank growth (banked daily,
  KD-1); it applies the deferred retirements + produces regens, restartable (§3.4).
- **KD-7 (single-writer + observation-surface).** `ProgressionEngine` is the sole writer of lifecycle
  state; a read-only `LifecycleViewModel` is observer-neutral (the `MatchEngine.BallView` posture);
  #30/tests mutate only through the public step API.
- **KD-8 (behaviour-neutral minimal identity).** `curveEnabled` off **and
  `AGE_BAND_RAMP_HALF_WIDTH_YEARS = 0`** reproduces the literal §4.3 step byte-for-byte; a two-run
  multi-season projection from one seed is byte-identical. *(Revised at ERR-028-020 — the identity is
  now the ramp dial's off position rather than the shipped behaviour; §5 exercises it rather than
  asserting it. §1.3 records why.)*

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial section from the converged supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-23 | — | Section-file PASS-1 (0H+2M: M-1 age-model muddle → one BirthWorldDay-derived representation; M-2 per-club regen stream) → AR-2 (3M cross-fix regressions) → AR-3 convergence; APPROVED. See section-9 §9.3.1. |
| 0.3 | 2026-07-27 | — | **ERR-028-002** (at #53's approval): the out-of-scope row records that the facility level #42 consumes is **#53's**, so a reader does not attribute the facility model to #40 (whose scope excludes it). #28's own out-of-scope position is unchanged. |
| 0.4 | 2026-08-22 | — | **ERR-028-020** (football-judgment proxy review, batch 1 — spec + code, same commit). §1.3 named "per-attribute CA/PA growth-decline curves keyed to age" as the Stage-3 tier while §3 contained no age-continuous formula anywhere, so what shipped as "the flat step" was a hard discontinuity at an exact integer age rather than a coarse approximation of a curve — pattern (d) as well as (b). The age-continuity of the rate is now Stage 2 (§3.1.3's centred ramps; §3.4's per-player retirement day under ERR-028-021); Stage 3 keeps the `(PA − CA)` magnitude modulation and per-attribute weighting, which is what always distinguished it. **KD-8** revised in the same way: the byte-for-byte identity is the ramp dial's OFF position, exercised by §5 rather than asserted. No other key decision moved. |
#endregion
