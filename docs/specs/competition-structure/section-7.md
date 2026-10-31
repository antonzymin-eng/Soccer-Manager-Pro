# Competition Structure #43 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 24, 2026
**Last Updated:** October 30, 2026 (v0.3 — §7.4 records implementation-time findings under roadmap C6; prior: July 24, 2026 (v0.2 — cross-set AR pass 3; prior v0.1 initial))
**Version:** 0.3
**Status:** APPROVED

---

## 7.1 T-phase implementation plan (post-APPROVED)

- **T0** — `TacticalDirector.Competition` assembly: `CompetitionFormat` / `Competition` /
  `CompetitionSet` + the instance-0 binding + `CompetitionConstants`. Behaviour-neutral by
  construction (KD-8 — the registry is inert on the season path).
- **T1** — `CompetitionSaveCodec` (`COMPETITION_SAVE_FORMAT_VERSION` = 1) + composition into #30's
  season save (outer `SEASON_SAVE_FORMAT_VERSION` bump coordinated here — exact version TBD,
  §4.4). Fail-loud gates (F3/F4) incl. canonical-order + coherence decode.
- **T2** — a **second round-robin division** (another instance over #30's pure machinery) +
  division membership + `ApplyPromotionRelegation` at (a') — landing the **#30 (a') execution
  hook** (the first half of the soft-reserved ERR-030-008: the transform's membership output
  applied to `SeasonState.ClubIds` via #30's command API before roll step (c); its own reviewed
  #30 change).
- **T3** — knockout cups: `BracketState`, `DrawRound` keyed draws (the **first draw site —
  promotes `_RESERVED_0x2C_` → `DOMAIN_TAG_COMPETITION = 0x2C` / `SubsystemOrdinals.Competition =
  94`**, spec-text-first, ERR-016 pattern), the merged fixture-day view + the **deep
  multi-competition fixture-day driver** (the second half of ERR-030-008), and the
  `GroupThenKnockout` format (group-assignment draws, `GroupAssign` purpose).

## 7.2 Deferred (recorded, not built)

- **Seeded draws / country protection.** Real cup draws use seeding pots and same-country
  avoidance; the Stage-5 extension adds pot structure over the same keyed mechanism (more
  purposes, APPEND-only).
- **Two-legged ties / away goals / replays / modelled extra time.** Knockout pairings resolve as
  single matches at the deep entry, a level scoreline decided by the FR-CP-026 keyed tie-break;
  multi-leg aggregation, replays, and a modelled extra-time/shootout (replacing the abstract
  tie-break draw as a reviewed change) are format extensions over `BracketState` (append fields
  behind the version gate).
- **Byes / preliminary rounds.** FR-CP-027 refuses non-power-of-two knockout entrant/group counts
  at config validation; bye structure (odd entry lists, staggered entry rounds — the real-cup
  shape) is the extension that lifts that gate, over the same keyed-draw mechanism.
- **Continental qualification.** League finish → continental entry is a season-boundary rule
  reading final standings (the (a') neighbourhood); deferred with the continental instance.
- **Fixture-congestion rescheduling.** The merged view slots deterministically; postponement /
  rescheduling (weather, pile-ups) is a Stage-5 calendar extension.
- **Per-competition prize money (#40) / suspension scoping (#44) / tournament overlay (#36) /
  bracket screens (#38).** Deferred consumers of #43's surfaces; no interface built (FR-LW-031).

## 7.3 Seam contracts recorded for downstream authors

- **#30 (season loop):** the (a') insertion point (FR-SN-031) is #43's transform site — before
  #40's (b'); the T-phase ERR-030-008 coordinations are (i) the (a') execution hook (T2) and
  (ii) the deep fixture-day driver (T3). #43 never references #30's assembly; #30's pure types
  (`FixtureScheduler`/`LeagueTable`) are consumed per instance.
- **#40 (finances):** reads post-promotion standings at (b') — already recorded from #40's side;
  per-competition prize money is #40's deep extension reading #43's per-competition finishes.
- **#44 (discipline, future):** scopes suspensions by the `CompetitionId` #43 carries on
  fixtures/results (FR-CP-020).
- **#36 (national teams, future):** overlays the calendar/competition model; the merged
  fixture-day view is the congestion surface it must respect.
- **#38 (UI, future):** renders competition/bracket view models (read-only value copies,
  FR-CP-022); MUST NOT mutate #43 state directly.

## 7.4 Findings recorded against implemented code (roadmap C6, October 30, 2026)

Recorded, not applied. This spec has no `src/` assembly, so under roadmap C6 these findings are discharged
in the landing named in each bullet, in the same commit as the code they govern. Found by the October 30,
2026 wide check of assembly-less specs against implemented code on `main`. The normative text above is
unchanged.

- **#44 has landed** (August 13, 2026, `src/discipline/`). It carries `DisciplineConstants.LeagueCompetitionKey
  = 0` as a `[CROSS]` copy of this spec's `LEAGUE_COMPETITION_ID`, kept as a literal because no #43 symbol
  exists. §7.2, §7.3 and §8 still treat #44 as a future consumer. At T0, replace that literal with a
  compiler-checked reference, placing the constant where `discipline` can reference it without a cycle, and
  land the ERR-044 Appendix A back-prop that file's history records as owed.

- **Save-block integration (#30 Appendix B rules added after this spec's approval).** From the phase that
  composes this block into the season frame (**T1**):
  (a) a guard against overwriting a populated destination block (ERR-030-036), keyed **per family**: on
  emptiness only if a destination that once carried this state can never legitimately present as empty
  (the #28 / #40 case); otherwise on an explicit wiring fact passed through **every** public write entry point
  (the #44 case, ERR-030-038 / ERR-030-039). Provisional reading: the minimal singleton collection persists nothing beyond instance
  0's binding, so an empty block is the ordinary state and a wiring fact is likely required. Confirm the classification against Appendix B at landing.
  (b) A `SeasonLoop.Restore` resume parameter as soon as the frame can carry populated state (ERR-030-050).
  (c) Where the block is club-keyed, the equivalent of #40's current-season club-universe coherence rule
  (ERR-030-050).
  (d) Composition in `client-app`'s `CareerSession` (`StartNew` / `Resume`, landed October 18, 2026) as well as
  in `SeasonLoop` — a second composition root this plan predates.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §7 (T-phase plan T0–T3, deferred extensions, downstream seam contracts), promoted from design supplement v0.3. Status IN REVIEW. |
| 0.2 | 2026-07-24 | — | Cross-set AR pass 3 (M-1/M-2 follow-through): the multi-leg deferral row re-anchored on the FR-CP-026 tie-break baseline; new **byes / preliminary rounds** deferral row (the extension lifting the FR-CP-027 power-of-two gate). |
| 0.3 | 2026-10-30 | — | §7.4: C6 findings recorded from the wide check against implemented code (#44 literal mirror, save-block integration). No normative text changed. |
#endregion
