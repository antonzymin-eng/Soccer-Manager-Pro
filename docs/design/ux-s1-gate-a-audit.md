# System XI — S1 Gate-A Capability Audit (PM-2 season loop)

**Created:** October 18, 2026\
**Last Updated:** October 18, 2026\
**Version:** 0.1\
**Status:** GATE-A AUDIT — RECORDED; Gates B–I not started\
**Purpose:** classify every S1 capability `ux-detailed-plan.md` §7.3 lists against the tree of the day,
so S1 design neither assumes #30 is missing nor assumes that because #30 exists the UI exists.\
**Execution authority:** [`ux-detailed-plan.md`](ux-detailed-plan.md) §7 · **Architecture:**
[`ux-experience-architecture.md`](ux-experience-architecture.md) §2–§9 · **Client contract:**
[`interactive-unity-client-design.md`](../tracking/interactive-unity-client-design.md) §5-P7

## 1. Audited boundary

Audited on branch `feat/c3-c4-career-shell` (cut from `origin/main` `c95e27ba`) **including** the §5-P7
host-free landing of October 18, 2026. Classification vocabulary is `ux-detailed-plan.md` §F1:
`LIVE` (production behaviour exists), `DESIGNABLE` (owning read/action exists, presentation does not),
`FUTURE-BLOCKED` (needs an absent owning-system or client change), qualified by `HOST-UNVERIFIED` where
code exists but has not run on the pinned Unity host. Every row names the symbol it was verified
against, not a folder name (the §8.1 false-positive lesson).

## 2. §7.3 classification

| # | Capability | Classification | Evidence |
|---|---|---|---|
| 1 | League table read model | `LIVE` (client projection) / binding `FUTURE-BLOCKED` | `SeasonState.TableOrdered()` → `CareerHomeViewModel.Table` + `TableClubNames` (`CareerHomeSource`) |
| 2 | Fixture / calendar read model | `LIVE` (next fixture) / full fixture-list screen `DESIGNABLE` | `SeasonState.Fixtures`, `SeasonCalendar.DayOfRound` → `CareerHomeViewModel.NextOpponent*`, `NextFixtureDay`; no list projection yet |
| 3 | Next-fixture / progression state | `LIVE` | `CareerProgress.NextAction` / `DaysUntilNextMatchday`, locked against #30's own guards (`CareerProgressTests`) |
| 4 | Advance command seam | `LIVE` | #30 `AdvanceDays(1)`, `AdvanceAndPlayNextRound()`, `RollToNextSeason()` |
| 5 | UI dispatcher / intent for advance | `LIVE` | `IntentKind.AdvanceDay/AdvanceRound/StartNextSeason` (ERR-038-005), `SeasonCommandDispatcher` |
| 6 | Generated new-game bootstrap | `LIVE` | `LeagueBootstrap.Generate` → `CareerSession.StartNew` (full #28/#29/#41/#44/#40 composition) |
| 7 | New-game configuration required for EA | `LIVE` — decided: club only (D-P7-1) | `NewCareerSetup` / `NewCareerViewModel` |
| 8 | Save capture / load / resume domain support | `LIVE` | `SeasonSaveManager.Save/Load` via `CareerSaveSlots`; resume ≡ uninterrupted (`ResumedCareer_ContinuesExactlyAsTheUninterruptedOne`) |
| 9 | Player-facing save browser / continue selection | `LIVE` (projection) / binding `FUTURE-BLOCKED` | `LoadCareerSource` / `LoadCareerViewModel` (`CanContinue`, `LatestSlot`, `LastError`) |
| 10 | Career Home/Season `ScreenId` and navigation | `LIVE` | `ClientScreens.CareerHome/NewCareer/LoadCareer`; eight guarded moves in `ClientScreenFlow` |
| 11 | Post-match return target | `FUTURE-BLOCKED` (D-P7-6) | No #30 seam plays the managed fixture interactively; no `PostMatchReport → CareerHome` edge by design |
| 12 | Settings / a11y persistence | `FUTURE-BLOCKED` | #49 runtime/client surface absent; #38 §4.4.1 settings store specified (ERR-038-004), not built |
| 13 | #37 season/statistic inputs used by the surface | `DESIGNABLE` | Career Home uses #30 table/result data only; no #37 season aggregate is admitted |

**Binding.** Every `LIVE` projection above is host-free and gate-compiled; the UGUI binding (P7b) is
absent, so every player-facing presentation is `FUTURE-BLOCKED` on P7b and will be `HOST-UNVERIFIED`
until it runs on the pinned host.

## 3. §7.4 product decisions — resolved October 18, 2026

| §7.4 item | Owner decision | Recorded |
|---|---|---|
| exact EA new-game options | pick a club in a generated league; default objective; no seed entry | §5-P7 D-P7-1 |
| autosave / manual-save promise | autosave (creation, every round, every roll) + 3 manual slots | D-P7-2 |
| incompatible-save promise | breakable through PM-3; incompatible slots listed, never loaded | D-P7-5 = D-10 |
| Continue auto-select vs picker | Continue loads the latest loadable slot directly; Load opens a picker | D-P7-2 |
| advance semantic | one day at a time; the match appears on its day | D-P7-3 |
| mandatory blockers before advance | none; auto-lineup; blocked only while an operation runs | D-P7-4 |

## 4. Consequences for Gates B–I

- **Career Home modules admissible now** (`LIVE` data): what changed (last managed result / roll
  outcome), attention (injured count, objective on/off track, autosave failure), what is next (next
  opponent, venue, day, days until), progress (single action + blocked reason), league table.
- **Not admissible as live**: post-match return to Career Home, a "prepare match" entry from Career
  Home, settings/a11y, suspension attention, fixture-list and calendar screens — label as future in
  any Gate B–C flow.
- **Usability task impact (§7.6):** "reach preparation" and "complete a round through the match/report
  loop" cannot be run against the career path until the D-P7-6 seam lands; until then a round completes
  on Career Home. Gate G planning must either wait for that seam or record the substitution.

## Version History

| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-10-18 | Initial S1 Gate-A audit against the §5-P7 landing: 13 capabilities classified with symbol evidence; §7.4 product decisions recorded as resolved by the owner; Gate B–I consequences. |
