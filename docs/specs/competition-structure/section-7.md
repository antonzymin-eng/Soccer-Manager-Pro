# Competition Structure #43 — Section 7: Future Extensions & T-Phase Plan

**Created:** July 24, 2026
**Last Updated:** July 24, 2026 (v0.1 — initial)
**Version:** 0.1
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
- **Two-legged ties / away goals / replays.** Knockout pairings resolve as single matches at the
  deep entry; multi-leg aggregation is a format extension over `BracketState` (append fields
  behind the version gate).
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

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-24 | — | Initial §7 (T-phase plan T0–T3, deferred extensions, downstream seam contracts), promoted from design supplement v0.3. Status IN REVIEW. |
#endregion
