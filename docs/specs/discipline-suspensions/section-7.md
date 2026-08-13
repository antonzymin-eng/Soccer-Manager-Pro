# Discipline & Suspensions #44 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 24, 2026
**Last Updated:** August 13, 2026 (v0.3 — ERR-044-003, C1/C2 landing back-prop: the ban-serving
deferral bullet flagged as a now-LIVE owner decision — #30 §2.3 F9 makes a suspended player
reinstatable in extremis rather than an absolute bar — with the deferral queue recorded as the
designed alternative)
**Last Updated (prior):** July 24, 2026 (v0.2 — cross-set AR; prior v0.1 — initial)
**Version:** 0.3
**Status:** APPROVED

---

## 7.1 T-phase implementation plan (post-APPROVED)

- **T0** — `TacticalDirector.Discipline` assembly: `DisciplineState`, `DisciplineRules`
  (thresholds/serving), `Availability` (`IsAvailable`/`FilterAvailable`), `DisciplineConstants`.
  Inert until wired (nothing calls it — behaviour-preserving by construction).
- **T1** — `DisciplineSaveCodec` (`DISCIPLINE_SAVE_FORMAT_VERSION` = 1) + composition into #30's
  season save (outer bump coordinated — exact version TBD, §4.4). Fail-loud gates (F3).
- **T2** — the live wiring: the tap-fed `CardLedgerFold` around engine-resolved fixtures (the
  #37-class read); the **ERR-030-009 filter** at the resolve→configure seam; `OnClubFixturePlayed`
  serving on both resolution paths; the **re-key migration / retirement drop** hygiene on the
  FR-TX-022 hook / #28 lifecycle coordination (as those land); the `Incoming`-id semantics
  verified against the live engine (KD-2's absorbed assumption re-checked).
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
- **Ban-serving deferral under squad shortfall.** *(ERR-044-003, August 13, 2026 — this is now a
  LIVE decision, not the hypothetical it was written as.)* §2.3's F5 fail-loud below the 18-player
  floor was **withdrawn**: #30 §2.3 F9 / §3.4 (ERR-030-029, approved after this section) settles a
  depleted squad by back-filling the least-injured (now least-suspended too) players back in until
  the engine's own selector can field the formation, never refusing until even the whole squad
  cannot. That means a suspended player **is** reinstatable in extremis today — suspension is a
  stricter reinstatement tier than injury (pressed back only after every injured player), but not an
  absolute bar, which the Laws of the Game do not allow. This deferral queue (excess bans postpone
  serving until the squad can field 18, refusing the fixture rather than fielding a banned player) is
  the designed alternative if the owner would rather refuse than reinstate — recorded here, still
  unbuilt, so the eventual answer is a designed extension, not an emergency patch.
- **Appeals / suspension psychology (#33)** — out of scope entirely at Stage 2.
- **Suspension screens (#38) / news items (#46)** — deferred consumers of the availability view
  and ban events (FR-LW-031).

## 7.3 Seam contracts recorded for downstream authors

- **#30:** the ERR-030-009 resolve→*filter*→configure null seam is #44's insertion point; serving
  is reported per played fixture on both resolution paths; the sub-blob rides `SeasonSaveCodec`.
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
#endregion
