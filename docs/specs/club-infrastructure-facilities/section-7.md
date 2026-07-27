# Club Infrastructure & Facilities #53 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 27, 2026
**Last Updated:** July 27, 2026 (v0.1 — initial section-file set)
**Version:** 0.1
**Status:** IN REVIEW

---

## 7.1 T-phase plan

| Phase | Content | Behaviour |
|---|---|---|
| **T0** | The assembly + `FacilityType` + `ClubFacilities` + the constants catalogue + `FacilityStore` (with its insertion guard) + the pure projections (§3.4/§3.5) and their tests. Nothing wired into #30 or the root. | **Inert** — no caller exists |
| **T1** | `FacilitySaveCodec` + the round-trip / fail-loud / ordinal-stability suite. Still not composed into the season save. | **Inert** |
| **T2** | **First non-inert phase.** Wire `AdvanceFacilityDay` at #30's slot (ERR-030-020); compose the sub-blob into `SeasonSaveCodec` (bumps `SEASON_SAVE_FORMAT_VERSION`); the root begins assembling #53's terms into `AcademyQuality` / `MedicalModifier` / #29's training input. | **Live**, and **identity-preserving**: every club is at baseline, so every dial is exactly its consumer's identity and no consumer output changes |
| **T3** | The feature. `CanStartUpgrade` / `StartUpgrade` wired to the command layer with #40's debit between them; the `[GT]` balance pass over the per-level constants; #40's matchday accrual reads `StadiumCapacity`. | **Named activation** — the first phase where a facility level can differ from baseline, and therefore the first where behaviour changes |

**T2 is where #53 becomes real, and it is deliberately still behaviourally neutral.** The producer is in
place and the four waiting consumers are bound to it, but every level is at baseline, so nothing moves.
That split exists so the save-format work and the balance work fail independently rather than together —
and it is the whole argument for landing the minimal tier now: it puts the **producer** in place before
any consumer needs a non-neutral value, which is precisely what §1.1's four specs are missing today.

**T3 is the first phase that can be got wrong in a way a player notices**, which is why the balance pass
and the command wiring are together in it rather than split: a per-level constant is only meaningful once
a level can change.

## 7.2 Deep-tier extensions (designed for, not built)

- **Maintenance and decay** — a facility that degrades without upkeep, and an upkeep cost posted through
  #40. Fits the existing shape: decay is a level decrement on the same day advance; upkeep is a #40
  transaction the command layer schedules. No new #53 surface.
- **Multiple concurrent builds** — lift the one-build-per-club simplification (KD-3). The record becomes
  a small fixed-capacity list, and completion order within a day must then be **pinned** — which is
  exactly the question the Stage-3 simplification exists to avoid answering prematurely.
- **Capacity-expansion economics** — attendance, ticket revenue, and the payback period on a stadium
  build. All of it is **#40's**; #53's contribution stays the capacity integer (§3.5).
- **A `ScoutingInfrastructure` member** — an APPEND-only addition, deliberately **not** declared in
  advance, landing if and when #32 declares a dial for it (KD-2). Zero cost when it comes; a phantom
  today.
- **Facility effects on reputation and player attraction** — consumed by #54 and #31 as read-only
  projections of levels #53 already holds. No #53 logic change; a new projection method at most.
- **Seed-varied genesis** — big clubs starting with better grounds (KD-2). Attractive, and explicitly a
  **promotion decision** rather than a tweak, because it enrols #53 in `WORLD_GENERATION_VERSION` (#50
  KD-2) and would break T-IN-DET-004 by design.
- **Multiple grounds per club** — training centres in more than one country, a second stadium. This is a
  keying change (`(ClubId, FacilityType)` → `(ClubId, SiteId, FacilityType)`) and therefore a
  `FACILITY_SAVE_FORMAT_VERSION` bump; recorded so it is understood as structural rather than additive.

## 7.3 Explicitly not planned

- **Money, in any form.** #53 will never hold a price, a budget, or a balance (FR-IN-005). A future spec
  wanting facility-linked economics reads levels and does its own arithmetic in #40's ledger.
- **An AI that decides to build.** #53 applies validated commands (FR-IN-028). Club-AI spending
  behaviour, if it is ever wanted, belongs with whatever spec models club AI — not here, where it would
  quietly become a second decision-maker alongside the manager.
- **A "total infrastructure score".** A single blended number over all four facilities is the obvious
  convenience and is refused: it would couple the four independent dials (breaking T-IN-U-006) and
  re-open the double-counting surface KD-4 closes. Consumers read the term they need.
- **Facility state reaching the match engine.** No #53 value reaches the 10 Hz/60 Hz loops. A "home
  advantage from a big stadium" effect, if ever wanted, is a value routed by the root into an existing
  match-engine input — never a #53 reference into the engine.
- **A stadium as a place.** Geometry, crowd, and atmosphere are #48's (§1.2).

## 7.4 Risks carried

- **R-1 — the mis-attribution is the whole reason to act now.** Four approved specs point at a producer
  that does not exist (§1.1). Each will otherwise reach its Stage-3 tier, find the dial still neutral, and
  either improvise a local facility notion or defer again — and two specs improvising the same model is
  the parallel-surface trap this project has hit three times (`TacticTranslation`, `PlayerAttributes`,
  `POSITION_COUNT`). The cost of #53 is one leaf assembly; the cost of the alternative is a model in two
  places with no single owner.
- **R-2 — #53 is a twenty-sixth format version** (KD-5), feeding #50's registry-bookkeeping risk.
  Accepted as the cost of the ownership model, and cheap here because the block is small, integer-only,
  and purely additive (it changes no existing spec's representation, unlike #45's `JobSecurity` bump).
- **R-3 — scope creep toward "club operations".** Infrastructure attracts stadium-expansion economics,
  ticket pricing, and naming rights, all of which are #40's or #45's. §1.2 and §7.3 hold the line at
  *levels + upgrade lifecycle + projections*; the line should be re-checked at each review, because every
  one of those features will feel natural to add here.
- **R-4 — the draw-free commitment is load-bearing for the determinism block** (KD-6). The roadmap §6
  block is exactly full at `0x20`–`0x2D` / 82–95, with `0x2E`–`0x2F` / 96–97 held back as slack for a
  currently-read-only spec that later discovers it needs a draw. A deep-tier "build overrun" feature
  would consume it, so FR-IN-031 requires that to be an explicit promotion decision rather than an
  implementation choice.
- **R-5 — Stage-3 placement means the minimal tier may sit unused for a while.** Acceptable because it is
  a provable no-op (§1.7), but the spec should not grow speculative depth in the meantime — §7.2 is a
  list of things *not* built, and it should stay that way until a consumer needs one.
- **R-6 — the producer-after-consumer inversion.** #53 lands after #42, #29 and #41, inverting the
  roadmap's producer-before-consumer rule. It is safe **only** because each of those consumers was built
  to the value-input pattern with an explicit neutral identity, so they function today with no producer
  at all. Recorded rather than silently relied upon: the same inversion would be **unsafe** for any
  consumer lacking a neutral default, and a future gap-fill candidate must check that property before
  assuming #53's precedent applies.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-27 | — | Initial §7 (T0–T3 with T2 as the first non-inert phase and its deliberate behavioural neutrality, deep-tier extensions with the structural-vs-additive distinction called out for multiple grounds and seed-varied genesis, the not-planned list incl. the refused "total infrastructure score", risks R-1..R-6 with the producer-after-consumer inversion recorded as R-6). Status IN REVIEW. |
#endregion
