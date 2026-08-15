# Discipline & Suspensions #44 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 24, 2026
**Last Updated:** August 15, 2026 (v0.5 — ERR-044-003 stage 1, owner decision: the ban-serving deferral
bullet resolved — the deferral queue was NOT chosen; the chosen answer is the exempt-the-appearance fix
(now LANDED, FR-DC-011 / `OnClubFixturePlayed`) plus two further staged tiers, both blocked — #42 Youth
has no `src/` assembly, and generated cover needs the packed `PlayerId` id space widened (#27
FR-SQ-010 / ERR-027-004))
**Last Updated (prior):** August 13, 2026, later same day (v0.4 — M11 + L6, adversarial review over the C1/C2
landing: §7.1's T2 bullet marked LANDED (except the migrate/drop hygiene, which did not land with
it); new §7.2 bullet records FR-DC-013's re-key/drop delivery has zero production call site and the
id-reuse hazard that lands with it; §7.3's #30 seam contract corrected from "null seam" to LIVE)
**Last Updated (prior):** August 13, 2026 (v0.3 — ERR-044-003, C1/C2 landing back-prop: the ban-serving
deferral bullet flagged as a now-LIVE owner decision — #30 §2.3 F9 makes a suspended player
reinstatable in extremis rather than an absolute bar — with the deferral queue recorded as the
designed alternative)
**Last Updated (prior):** July 24, 2026 (v0.2 — cross-set AR; prior v0.1 — initial)
**Version:** 0.5
**Status:** APPROVED

---

## 7.1 T-phase implementation plan (post-APPROVED)

- **T0** — `TacticalDirector.Discipline` assembly: `DisciplineState`, `DisciplineRules`
  (thresholds/serving), `Availability` (`IsAvailable`/`FilterAvailable`), `DisciplineConstants`.
  Inert until wired (nothing calls it — behaviour-preserving by construction).
- **T1** — `DisciplineSaveCodec` (`DISCIPLINE_SAVE_FORMAT_VERSION` = 1) + composition into #30's
  season save (outer bump coordinated — exact version TBD, §4.4). Fail-loud gates (F3).
- **T2** — **LANDED August 13, 2026 (roadmap C1/C2), except the migrate/drop hygiene below.** the live
  wiring: the tap-fed `CardLedgerFold` around engine-resolved fixtures (the
  #37-class read); the **ERR-030-009 filter** at the resolve→configure seam; `OnClubFixturePlayed`
  serving on both resolution paths; the `Incoming`-id semantics verified against the live engine
  (KD-2's absorbed assumption re-checked; ERR-044-001 corrected what the verification found). **The
  re-key migration / retirement drop** hygiene on the FR-TX-022 hook / #28 lifecycle coordination did
  **not** land with the rest of T2 — `DisciplineRules.MigratePlayerId`/`DropPlayer` exist and are
  unit-tested but have zero production callers; see the §7.2 bullet below.
- **T3** — deep: **#43 competition partitions** (per-`CompetitionId` tallies + per-competition
  serving — a partition activation over the FR-DC-012 key); the **#30-owned quick-sim card
  synthesis** coordination (keyed draws on #30's `0x22` stream, evening the minimal coverage
  asymmetry — never a #44 stream); varying ban lengths by offence class.

## 7.2 Deferred (recorded, not built)

- **Quick-sim card synthesis** — #30-owned (its stream, its model); #44 folds whatever summary
  #30's model emits, unchanged.
- **Competition-scoped accumulation / cup-vs-league carry rules** — the #43 partition (FR-DC-012
  pre-shapes it); carry rules between competitions are a partition-policy table, deep.
- **Offence classes / varying ban lengths** — richer `CardIssuedEvent` interpretation (e.g.
  violent conduct vs two bookings) requires engine-side offence data that does not exist; deferred
  until the engine emits it.
- **Ban-serving under squad shortfall — RESOLVED, staged.** *(ERR-044-003, August 13, 2026, stage 1
  August 15, 2026.)* §2.3's F5 fail-loud below the 18-player floor was **withdrawn**: #30 §2.3 F9 /
  §3.4 (ERR-030-029, approved after this section) settles a depleted squad by back-filling the
  least-injured (now least-suspended too) players back in until the engine's own selector can field
  the formation, never refusing until even the whole squad cannot. That means a suspended player **is**
  reinstatable in extremis — suspension is a stricter reinstatement tier than injury (pressed back
  only after every injured player), but not an absolute bar, which the Laws of the Game do not allow.
  **The deferral queue this bullet used to record as the alternative (excess bans postpone serving
  until the squad can field 18, refusing the fixture rather than fielding a banned player) was NOT
  chosen.** The owner's decision (August 15, 2026) is a three-tier staging instead, of which the first
  tier is now **LANDED**: (1) an extremis appearance no longer serves the ban it was fielded through —
  `OnClubFixturePlayed` now takes the club's fielded eleven and exempts anyone in it (FR-DC-011,
  §3.3), so the reinstatement stays possible but is no longer free; (2) **youth call-ups** ahead of
  any suspended player — **blocked: #42 Youth has no `src/` assembly**; (3) **generated low-attribute
  cover** ahead of that, after which a banned man never reaches the pitch at all and the suspended
  tier becomes unreachable rather than merely costly — **blocked: the packed `PlayerId = clubId ×
  CLUB_SQUAD_SIZE + local` id space needs widening** (fully packed at 25; a 26th player for club N
  collides with club N+1's first — #27 FR-SQ-010 as amended by ERR-027-004). Tiers 2 and 3 are
  recorded here, unbuilt, as the eventual answer once their blockers clear.
- **FR-DC-013's re-key/drop delivery has no call site (M11, recorded at the adversarial review over
  the C1/C2 landing).** `DisciplineRules.MigratePlayerId` and `DropPlayer` are built and unit-tested
  but referenced by nothing outside `src/discipline/` — the T-phase plan above named the FR-TX-022
  roster-move hook as the delivery point, and #29/#41's own T2 roster-sync landed at exactly that
  point in `SeasonLoop.RollToNextSeason` (`PlayerCareerStates.CommitRosterSync`) without also
  wiring #44's re-key/drop, and #44 has no membership of its own to reconcile independently. Inert
  today, because #28's boundary regen (retiree removal + 1:1 replacement) is itself deferred — but
  the consequence when it lands is not an orphan discipline row: player ids are `clubId *
  CLUB_SQUAD_SIZE + localIndex` (#27), so a regen filling a retiree's vacated slot **inherits the
  identical id**, and with it — silently — the retiree's outstanding ban and yellow tally. Recorded
  at `SeasonLoop.RollToNextSeason`'s roster-sync call site in `src/season-save/SeasonLoop.cs` so
  #28's boundary landing cannot miss it.
- **Appeals / suspension psychology (#33)** — out of scope entirely at Stage 2.
- **Suspension screens (#38) / news items (#46)** — deferred consumers of the availability view
  and ban events (FR-LW-031).

## 7.3 Seam contracts recorded for downstream authors

- **#30:** the ERR-030-009 resolve→*filter*→configure seam is #44's insertion point — LIVE since T2
  (C1/C2, August 13, 2026), not the null seam it was at approval; serving is reported per played
  fixture on both resolution paths; the sub-blob rides `SeasonSaveCodec`.
- **#37:** one per-tick tap feeds both consumers when both are built (a composition-root
  concern); neither references the other.
- **#43:** partitions activate over the `(PlayerId, CompetitionId)` key; #43's `CompetitionId` on
  fixtures/results is the scoping input.
- **#31/#28:** the roster re-key/retirement events deliver the migrate/drop hygiene — bans follow
  the player (the recorded contrast with #32's drop rule).
- **#38 (future):** renders availability/suspension view models (read-only value copies); MUST
  NOT mutate `DisciplineState` directly.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §7 (T-phase plan T0–T3, deferred extensions, downstream seam contracts), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Cross-set AR (L): §7.2 gains the **ban-serving deferral under squad shortfall** row — the F5 <18 fail-loud is coherent today (the engine's own gate, verified) but the pile-up is reachable in principle; the deferral queue is the recorded deep mitigation. |
| 0.3 | 2026-08-13 | — | **ERR-044-003** (C1/C2 landing back-prop): the deferral bullet corrected — §2.3's F5 fail-loud it was written against no longer exists (withdrawn in favour of #30 §2.3 F9's back-fill), so the squad-shortfall question is no longer hypothetical: a suspended player is reinstatable in extremis today, and the deferral queue is recorded as the alternative if the owner prefers refusing the fixture instead. |
| 0.4 | 2026-08-13 | — | **M11 + L6** (adversarial review over the C1/C2 landing): §7.1's T2 bullet marked LANDED, with the migrate/drop hygiene split out as the one T2 item that did NOT land; new §7.2 bullet records FR-DC-013's re-key/drop delivery has zero production call site today and the id-reuse hazard a #28 boundary regen would hit; §7.3's #30 seam-contract bullet corrected from "null seam" (stale since T2) to LIVE. |
| 0.5 | 2026-08-15 | — | **ERR-044-003 stage 1**, owner decision: the ban-serving-under-squad-shortfall bullet resolved from "now a live decision" to RESOLVED — the deferral queue was NOT chosen; the chosen answer is a three-tier staging, of which tier 1 (exempt the extremis appearance from serving, FR-DC-011) is LANDED, and tiers 2 (youth call-ups) and 3 (generated cover) are recorded unbuilt with their blockers (#42 has no `src/` assembly; the packed `PlayerId` id space needs widening, #27 FR-SQ-010 / ERR-027-004). |
#endregion
