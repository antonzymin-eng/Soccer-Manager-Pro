# Staff & Backroom #34 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 23, 2026
**Last Updated:** October 30, 2026 (v0.2 — §7.4 records implementation-time findings under roadmap C6; prior: July 23, 2026 (v0.1 — initial))
**Version:** 0.2
**Status:** APPROVED

---

## 7.1 T-phase implementation plan (post-APPROVED)

- **T0** — `TacticalDirector.Staff` assembly: value types (`StaffAttributes`, `StaffRole`, `StaffRecord`,
  `StaffState`), the pure `StaffProjections` (`ToMedicalModifier`/`ToCoachingModifier`/`ToStaffMult`/
  `ToMentoringOverride`/`ToScoutQuality`, each neutral ⇒ `Identity`), `NeutralHouseStaff` + `SeedInitialStaff`,
  `StaffConstants`. Behaviour-neutral by construction (KD-8 — neutral projections; no draw; no wage). The
  composition root threads the two **live** projections (`MedicalModifier` → #41, `CoachingModifier` → #29).
- **T1** — `StaffSaveCodec` (`STAFF_SAVE_FORMAT_VERSION` = 1) + composition into #30's season save (the
  `SeasonSaveCodec` sub-blob; #30's outer `SEASON_SAVE_FORMAT_VERSION` bump coordinated here — exact version
  TBD, §4.4). Fail-loud gates (F3); the **genesis-only-vs-load** lifecycle wired at the composition root.
- **T2** — Wire the world-tick step at #30's **new staff slot** (ERR-030-006, declared at approval — §8);
  expose the read-only staff/role-slot accessors later consumers need. **No RNG stream registered
  (draw-free); no #30 roster-commit (staff never re-key).**
- **T3** — Deep tier (each defaulting to its scaffold identity via `deepStaffEnabled`): **real
  attribute-derived non-identity projections**; the **candidate pool** (the first draw site — promotes
  `DOMAIN_TAG_STAFF = 0x26` / `SubsystemOrdinals.Staff = 88`, spec-text-first, ERR-016, keyed on `(clubId,
  worldDay, purpose)`); **hiring** via the #31 negotiation pattern (`StaffOffer`/`EvaluateStaffOffer`/
  `HireStaff`, year-round); the **`StaffWage` producer** (the `{Debit/Credit, StaffWage, …}` posts + the
  `WageBillAggregate + wage ≤ WageBudget` gate, landing with the **shared ERR-040** #40 back-prop relaxing
  FR-FN-015); the **#29 `CoachingModifier` field shape + consumption** (ERR-029-002); **#33 judgement**
  feeding scout/coach quality; and **staff aging/retirement** at the season boundary.

## 7.2 Deferred (recorded, not built)

- **Staff hiring + candidate pool.** The scaffold has neutral-baseline staff only; hiring, the stochastic
  candidate pool, and `StaffWage` posts are deep (KD-1/KD-4/KD-6). The tick-order slot is declared now
  (reserve-ahead) but empty until this lands.
- **Non-identity projections.** The scaffold projects `Identity`; attribute-derived modulation is deep.
- **The #29 `CoachingModifier` field shape.** #29 reserved only the type name (`default` identity); the
  per-mille fields + #29's consumption are a deep #29 back-prop (ERR-029-002), landing when #34 first produces
  a non-identity coaching modifier.
- **The #33 judgement / mentoring-override production.** #34 produces the `MentoringPlan` override #33 already
  reads (default `MentoringPlan.None`); a non-identity override + any morale-modulating staff judgement is
  deep. #33 §7.3 already names #34 the producer.
- **The #32 scout-quality consumer.** #34 publishes `ToScoutQuality`; #32 (which does not exist yet) consumes
  it. No #32 interface is built (FR-LW-031).
- **The #42 academy hook.** Academy coaching → intake quality is a deferred #42 consumer of the staff-quality
  projection.
- **Staff aging / retirement / contract renewal.** Staff carry an `Age` and (deep) a `Contract`; aging,
  retirement, and renewal flows are deep-tier.
- **All-clubs staff modelling.** The scaffold tracks the managed club only (FR-ST-011); AI clubs get staff
  with autonomous AI at the deep tier.
- **Indexed candidate search.** The deep candidate pool is a bounded scan; an index is a deep performance
  extension.

## 7.3 Seam contracts recorded for downstream authors

- **#41 (Injuries & Medical):** #34 produces the `MedicalModifier` #41 reads (`AdvanceMedicalDay` /
  `ComputeInjuryRisk`, default `Identity`); #34 MUST NOT add a second occurrence-risk / recovery-speed path
  (#41 §7 KD-5). Consume-ready at approval.
- **#29 (Training System):** #34 produces the `CoachingModifier` #29 reads (`AdvanceTrainingDay` /
  `ComputeTrainingInput`, default `Identity`); #34 MUST NOT add a second training-effectiveness path (#29 §7
  KD-3). The per-mille field shape + consumption is a deep back-prop (ERR-029-002).
- **#40 (Club Finances):** #34 posts staff wages via `ApplyTransaction` (`StaffWage`, deep) and reads
  `WageBudget`/`WageBillAggregate` read-only; it MUST NOT write `ClubFinances` or hold a parallel wage total
  (FR-FN-015). At the scaffold #34 posts **nothing**, so FR-FN-015 is preserved verbatim and **no #40
  back-prop is needed at approval**; the deep `StaffWage` producer + the `WageBudget` gate land with the
  shared ERR-040.
- **#33 (Personalities):** the deep tier reads #33 **read-only** (`MoraleOf`, `PersonalityProfile`) and
  produces the `MentoringPlan` override #33 already reads (default `None`, FR-HS-022); #34 never writes #33
  state.
- **#31 (Transfers):** #34 reuses #31's `NegotiationOutcome` enum (deep) and produces #31's `staffMult`
  (default `TRANSFERS_STAFF_MULT_IDENTITY = 1000`, FR-TX-011); #34 builds no transfer surface.
- **#30 (season loop):** owns the world-tick slot timing + the season-save composition; #34 declares a **new
  staff tick-order null-seam slot** (ERR-030-006) and needs **no roster-commit** (KD-7). #31 MUST NOT
  reference #30.
- **#32 (scouting, future) / #42 (academy, future):** consume `ToScoutQuality` / the coaching projection with
  their own inputs; #34 builds no interface for them (FR-LW-031).
- **#38 (UI, future):** drives the staff command APIs (`HireStaff` etc.); MUST NOT mutate #34 state directly
  (the `SetTeamTactic` command discipline).

## 7.4 Findings recorded against implemented code (roadmap C6, October 30, 2026)

Recorded, not applied. This spec has no `src/` assembly, so under roadmap C6 these findings are discharged
in the landing named in each bullet, in the same commit as the code they govern. Found by the October 30,
2026 wide check of assembly-less specs against implemented code on `main`. The normative text above is
unchanged.

- **Per-club modifier routing.** #30's career layer passes one modifier to every club:
  `PlayerCareerStates.AdvanceTrainingDay` and `GatherTrainingInputs` take one `CoachingModifier`, and
  `AdvanceMedicalDay` one `MedicalModifier` (`src/season-save/PlayerCareerStates.cs`). The identity-only scaffold
  (T0–T2) is unaffected. Before T3's first non-identity projection (or #53's per-club terms) reaches those calls, `season-save` needs a per-club
  routing change (and #30 text, if it pins the signature); otherwise one club's effect would apply to all.

- **Projection names disagree.** #53 §4 composes `staff.ProjectMedicalModifier(clubId)` and
  `staff.ProjectAcademyTerm(clubId)`; #34 §3 defines `ToMedicalModifier(in StaffRecord physio)` and no
  academy-term projection. Reconcile the root adapter with #34's actual surface when the first of #34 / #53
  lands.

- **Save-block integration (#30 Appendix B rules added after this spec's approval).** From the phase that
  composes this block into the season frame (**T1**):
  (a) a guard against overwriting a populated destination block (ERR-030-036), keyed **per family**: on
  emptiness only if a destination that once carried this state can never legitimately present as empty
  (the #28 / #40 case); otherwise on an explicit wiring fact passed through **every** public write entry point
  (the #44 case, ERR-030-038 / ERR-030-039). Provisional reading: once `SeedInitialStaff` has run the managed club always holds
  neutral house staff, which suggests the emptiness key; confirm that no #54 manager change can leave the
  store legitimately empty. Confirm the classification against Appendix B at landing.
  (b) A `SeasonLoop.Restore` resume parameter as soon as the frame can carry populated state (ERR-030-050).
  (c) Where the block is club-keyed, the equivalent of #40's current-season club-universe coherence rule
  (ERR-030-050).
  (d) Composition in `client-app`'s `CareerSession` (`StartNew` / `Resume`, landed October 18, 2026) as well as
  in `SeasonLoop` — a second composition root this plan predates.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-23 | — | Initial §7 (T-phase plan T0–T3 + deferred extensions + downstream seam contracts), promoted from design supplement v0.4. Status IN REVIEW. |
| 0.2 | 2026-10-30 | — | §7.4: C6 findings recorded from the wide check against implemented code (per-club modifier routing, projection names, save-block integration). No normative text changed. |
#endregion
