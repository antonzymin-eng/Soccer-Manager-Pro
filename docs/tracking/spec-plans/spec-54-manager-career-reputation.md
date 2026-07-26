# Spec #54 — Manager Career, Reputation & Job Market — High-Level Plan

> **Created:** July 26, 2026
> **Status:** PLAN (pre-design-supplement — no section files, no `SPEC_INDEX.md` row). Candidate spec number **#54** (proposed in `../management-layer-spec-roadmap.md` v0.6, not reserved). **A converged design supplement already exists** — `../manager-career-reputation-design.md` v0.4 — because the gap was found while authoring Wave 8; this plan completes the record, and the supplement is authoritative where they differ.
> **Master-plan home:** §5 Stage 5 *"Manager career mode (job offers, reputation)"* · **Tier:** S2 min → S5 deep · **Wave:** 6 (after #45) · **FR prefix (proposed):** FR-MC (grep-verified unclaimed)
> **Determinism:** minimal tier **draw-free**; `_RESERVED_0x2E_` / ordinal 96 **reserved, not promoted** until the S3 job-market draw exists.
> **Purpose:** Own the manager tenure lifecycle — the termination rule `FR-BD-012` attributes to #30 but #30 does not contain — plus the career record, reputation, the job market, and the unemployed state that makes them representable.

## 1. Scope
The **manager entity** and **tenure** (appointment → employment → termination); the **career record** and the **reputation** projected from it; the **job market** (vacancies, interest, offers); and the **unemployed** state. **Out of scope:** board confidence (#45 — read-only input), the objective and its season evaluation (#30), the day/season loop (#30 invokes a #54 step), club finances/facilities/squads (#40/#53/#27, read as root-supplied values), player psychology (#33), and #26's in-match `ManagerProfile`/`ManagerMode`, which is a different "manager" entirely.

## 2. Staging (minimal-first → deep)
Minimal = the entity, record and tenure state with one appointment and no vacancies; termination is representable and the save survives it, but with no vacancy source the player cannot yet continue into a new job. S3 = vacancies from club state, interest and offers, moving clubs mid-career (where the `0x2E` draw likely becomes real). S5 = rival managers as entities via #22's phase-5 `BackgroundTierSim`, manager personality (#33), international appointments (#36).

## 3. Dependencies
- **Upstream (needs):** #45 board confidence (Wave 5) and #30's objective outcome + a tick-order/boundary slot; #40/#53/#27 club values for vacancy attractiveness (values, not references).
- **Downstream (consumers):** #31 negotiation may read reputation as a value input; #22's phase-5 replaces the vacancy source at S5. Nothing references #54.

## 4. Persistent state & save impact
An APPEND-only `ManagerCareer` block (tenures with club, dates, end reason, finishes, trophies) in a `CAREER_SAVE_FORMAT_VERSION`-gated opaque sub-blob. **Reputation is deliberately absent from the shape** — it is a projection. Separately, `ManagedClubId` must become an **explicit optional** so unemployment is representable, carrying a `SEASON_STATE_FORMAT_VERSION` bump best combined with `ERR-030-009`'s queued one.

## 5. Determinism
Minimal is draw-free (tenure evaluation is a rule; reputation is arithmetic over a record). World-tick only. Reputation-as-projection cannot desynchronise across a restore — the failure `ERR-030-009` documents. From S3, one **subsystem-wide** stream with keyed action ordinals if promoted, never one per club or vacancy (the shared `MaxRngStreams = 64` bound).

## 6. Key design decisions (resolved in the supplement)
- **KD-1** #54 owns tenure end to end; #45 keeps confidence, #30 gains a seam. Splitting rule from aftermath is what orphaned `FR-BD-012`.
- **KD-2** Reputation is a projection over an APPEND-only record, never a stored scalar.
- **KD-3** Vacancies are a property of **clubs**; rival managers are not invented (#22 phase-5 is their producer).
- **KD-4** *Continue-unemployed* over *end-the-career*; and an **appointment** must initialise board confidence to the **factory** value, since `default(BoardConfidence)` reads as `Critical` — with the insertion performed by the command layer, so #54 acquires no write into #45's store.
- **KD-5** An explicit optional, not a `-1` sentinel, so the compiler enumerates every read site.

## 7. Primary surfaces (proposed)
`ManagerCareer` + `CurrentTenure`; `EvaluateTenure`; `ReputationOf` (projection); `VacancyView`; `Appoint` / `Terminate`; `CareerSaveCodec`. All proposed. **Naming hazard:** #26 already ships `ManagerProfile`/`ManagerMode` — a T0 grep must confirm no collision (the `TacticTranslation` / `PlayerAttributes` precedent).

## 8. Test focus
The unemployed state (a career survives mid-season termination; the season advances with no managed club, every fixture through the round-resolution model; the save round-trips byte-identically) — the case the current codec cannot construct, so it also proves the back-prop landed. Reputation cannot diverge (no stored field exists to compare, plus a structural check that none appears). Tenure evaluation is pure and leaves #45 unchanged. Appointment yields a factory confidence, never `default`. Append-only history; single-appointment identity.

## 9. Open questions / risks
- `FR-BD-012` will keep pointing at #30 until re-pointed, so the first implementer of #45's confidence will look for a sacking rule, not find one, and put one somewhere convenient.
- The `ManagedClubId` format bump lands on a block that already has one queued; combining them is the recommendation, and failing to costs players two refusal boundaries.
- Scope creep toward "career mode as a whole" (manager attributes, badges, media relationships).
- Inventing rival managers at S3 would build the consumer #22's phase-5 is meant to produce.

## Version History
| Version | Date | Change |
|---------|------|--------|
| v0.1 | July 26, 2026 | Initial plan, written alongside the converged supplement v0.4 (which is authoritative). |
