# System XI — S1 Gate-A Capability Audit (PM-2 season loop)

**Created:** October 18, 2026\
**Last Updated:** November 3, 2026\
**Version:** 0.2\
**Status:** ORIGINAL GATE-A AUDIT RECORDED; INTERACTIVE DELTA PREPARED, NOT ADOPTED; Gates B–I not started\
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

## Remote-main correction — November 3

The local delta below was based on stale main `32ff375f`. Live remote main `1d3cebf3` and its
D-P7-6 amendment v0.6 prohibit mid-match saves and replace unfinished-matchday resume with abandonment.
Any resume-specific task or persistence requirement below is withdrawn. Subsequent user decisions are consolidated in
[delegated completion v0.5](../tracking/d-p7-6-delegated-completion-proposal.md): plan Play or
selected-coach delegation before kickoff, irreversible selected-coach remainder simulation, normal
finish/save/exit, and no user entry to a delegated match. No active-match save; Home saving before/after
the match on the same calendar day remains. Do not use the remote abandon/restart choice where these
new user directions supersede it.
The independent P7b binding work and deferred recruitment remain. The delta is still unadopted.

## 5. November 3 delta preparation — historical, superseded where noted

The October 18 rows above are retained as historical audit evidence. Baseline inspected for this
preparation: `32ff375f7b2b74ff5e2c95e4c19aedb1d737efce`. Roadmap D-11 now requires interactive managed
fixtures and unfinished-matchday resume. The [D-P7-6 draft](../tracking/d-p7-6-interactive-career-contract.md)
is PROPOSED. Per §5-P7, adopt this delta only after contract approval; then verify the landing's actual
symbols and host evidence before advancing any capability label. A contract approval alone does not
make its implementation `LIVE`.

| Original rows | Current evidence / change to re-check | Current disposition |
|---|---|---|
| 1–2 | `CareerHomeSource`, `SeasonState.Fixtures`, `SeasonCalendar.DayOfRound` still own table/next-fixture data; no full fixture-list/calendar screen admitted | Existing data retained; presentation still absent |
| 3–5 | `CareerProgress.NextAction` and `SeasonCommandDispatcher` still resolve matchday synchronously | Current behavior `LIVE`; interactive begin/finish continuation `FUTURE-BLOCKED` on D-P7-6 |
| 6–7 | `NewCareerSetup.Prepare/Select/Start` and `CareerSession.StartNew` remain club-only bootstrap | Existing host-free source retained; UGUI absent |
| 8 | `SeasonSaveCodec` has optional match bytes; `SeasonLoop.ActiveMatch` cannot safely be captured concurrently; `CardLedgerFold` pending cards are not serialized | Between-round save supported; unfinished-matchday resume `FUTURE-BLOCKED`, not implied by the match flag |
| 9 | `LoadCareerSource.TryContinue/TryLoad` and `CareerSaveSlots` retain loadable/incompatible/damaged handling; career resume refuses a match block | Existing picker source retained; pending-match route and restore validation absent |
| 10 | Seven screen ids/eight career moves; seven-root `ClientShellWiringValidator`; cached worker-backed `ClientCareerCoordinator`; optional seven-screen `ClientMatchCoordinator` composition. Linux ClientApp 218 passed / 3 diagnostic skips, documented host-crash fixture excluded; pinned Unity not run | P7b host-free composition plus opt-in UGUI candidate now exist; default scene remains four roots until host setup. All career runtime/copy/layout acceptance is unverified |
| 11 | No Career Home → Tactics or Report → Career Home move exists; new named provenance-aware moves required | `FUTURE-BLOCKED` until approved seam implemented; no PM-1 return reuse |
| 12 | No settings/a11y entry is admitted by the current career shell | Still `FUTURE-BLOCKED`; S1-T8 must remain in prototype coverage |
| 13 | No new #37 season statistic source was introduced | No additional statistic admitted |

### Task/validation consequences

- Preserve all eight S1 tasks. Replace the old §4 suggestion to record a substituted Home-only round:
  **that substitution cannot satisfy the D-11 interactive S1-T5 task at Gate G**.
- Recruitment is **deferred by the user November 3**; retain [the prepared brief](ux-s1-recruitment.md)
  for later. Neither participant is confirmed. This does not block the current contract/P7b work.
  Owner availability and independence/distinctness attestations remain due before Gate F.
- Participants run at **Gate G after Gate F**. Gate F may use a host-free prototype, accurately labelled
  where behavior is simulated/future; a missing prescribed task fails readiness rather than shrinking
  the denominator. Gate G validates the design; Gate J verifies actual Unity implementation.
- Prototype the interactive match/report/return and unfinished-matchday save/resume outcomes, including
  disabled, failure/retry and cancellation paths after the D-P7-6 contract is settled.
- Retain the S1-specific two-independent-participant rule. S0 owner image approval does not waive it.

### Adoption checklist (all still open)

- [ ] Explicit D-P7-6 contract approval recorded against a reviewed revision.
- [ ] Verify new domain/adapter/navigation/persistence symbols; classify implemented vs future separately.
- [ ] Replace historical rows with adopted current evidence and pin the reviewed source identity.
- [ ] Reconcile Gates B–I task flow/state matrix; identify prototype behavior vs runtime behavior.
- [ ] Record actual P7b compile, pseudo-locale/scale/focus and host evidence without carrying S0 results forward.

## Version History

| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-10-18 | Initial S1 Gate-A audit against the §5-P7 landing: 13 capabilities classified with symbol evidence; §7.4 product decisions recorded as resolved by the owner; Gate B–I consequences. |
| 0.2 | 2026-11-03 | Preserve the October 18 audit; prepare, but do not adopt, the D-11 interactive delta, source evidence, task consequences and adoption checklist. |
