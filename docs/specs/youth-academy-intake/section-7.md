# Youth Academy & Intake #42 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial)
**Version:** 0.1
**Status:** APPROVED

---

## 7.1 T-phase plan

| Phase | Contents | Behaviour |
|---|---|---|
| **T0** | The `TacticalDirector.YouthAcademy` assembly with the pure surface: `AcademyQuality`, `YouthProspect`, `AcademyState`, `AcademyTransforms` (`ApplyCeilingShift` / `ReanchorAge`), `AcademyAnchor.DeriveActionOrdinal`, `AcademyConstants`, plus §5.2/§5.3 unit tests. No RNG registration, no #30 wiring. | Inert — nothing calls it. |
| **T1** | `AcademySaveCodec` + the round-trip / fail-loud tests (§5.5, §5.7). Still not composed into the season frame. | Inert. |
| **T2** | `AcademyIntake` + `AcademyPromotion` wired at #30's academy tick slot; the `youth.intake` stream registered at the first intake; the sub-blob composed into `SeasonSaveCodec` (outer `SEASON_SAVE_FORMAT_VERSION` bump); the #16 `0x2B`/93 promotion; the §4.4 anchor seam. | **First behaviour change** — a managed career gains a cohort. Neutral quality ⇒ an identity over #28 (T-YA-ID-001). |
| **T3** | The deep tier: non-neutral quality wired from #34/#40 at the root, the bio-banded age band, youth contracts, promotion criteria. | Non-neutral by design. |

**T2 is the first non-inert phase** and is where the `SEASON_SAVE_FORMAT_VERSION` bump, the id-authority
contract (§4.6), and the closed-loop scenario (§5.8) all land together — they are not separable.

## 7.2 Deferred by design

- **World-wide academies.** FR-YA-021 limits the minimal tier to the managed club. Extending to every
  club is gated on the **shared `MaxRngStreams` bound** (§7.4 R-1) and on the background-tier world sim,
  exactly as #36 defers its scope to Stage 5.
- **Bio-banding (KD-2b).** The transform exists and is a no-op at minimal; the band itself waits on the
  Master Vol 1 source model being confirmed (§7.4 R-2). Pinning a band now would be inventing numbers.
- **Youth contracts.** `YouthProspect.ContractState` is present and inert (`0 == none`). Depth arrives
  with #31's negotiation pattern, reused the way #34 KD-1 reuses it — pattern and enum, not #31's
  `Offer` struct.
- **Promotion criteria.** Minimal promotion is an explicit manager command. Automatic / AI promotion is
  deep-tier and belongs with the #26-style manager-decision cadence, not on the daily tick.
- **A scouting view of prospects (#32).** #42 publishes the record; #32 will own the knowledge model.
  #42 builds nothing for it (FR-LW-031).

## 7.3 Explicitly NOT planned

- **Any change to #28.** KD-1 and KD-2 exist precisely so the generator, the CA/PA model, and
  `PlayerLifecycle` stay untouched. If a future extension appears to need a #28 edit, that is a signal to
  re-derive the extension as a post-generation transform first.
- **A #42 growth modifier.** F7 forbids it. Coaching reaches growth through #29 → #28; #42's dial is a
  one-time ceiling. Any surface that lets an academy also change the *rate* is a double-count.
- **A #42-owned reputation, morale, or finance field.** Those belong to their owners; #42 consumes
  values, never mirrors them.

## 7.4 Open risks carried forward

- **R-1 (shared, pre-existing — surfaced by #42, not caused by it): `MaxRngStreams` = 64 vs per-club
  streams.** #28 FR-PG-020 registers a `player-progression.regen` stream **per club**; #42 would too if
  academies ran world-wide. A full-world career would exhaust the 64-slot, never-shrinking table. #42
  stays single-club at minimal (FR-YA-021) so it does not make this worse, but **the bound must be
  resolved by #28/#16 before either spec goes world-wide** — via a larger table, or club-indexed
  sub-streams under a single registration. Recorded here because #42's review is where it surfaced.
- **R-2: bio-banding source model unconfirmed** (see §7.2).
- **R-3: two `PlayerId` allocators.** Resolved *as a contract* in §4.6 (the root owns one authority or a
  documented serialized partition), but the contract is only enforceable when the root exists at T2. Until
  then it is a written obligation, not a mechanism.
- **R-4: quality double-counting.** Structurally prevented by F7 today. The risk is a future reader
  "unifying" the ceiling dial with the growth path; §1.2 and F7 state the disjointness so that unification
  reads as the regression it would be.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §7 (T0–T3 plan with T2 named as the first non-inert phase, deferred-by-design items, the explicit not-planned list incl. the no-#28-change rule, and the four carried risks incl. the shared `MaxRngStreams` bound). Status IN REVIEW. |
#endregion
