# UI / Client Framework #38 — Appendices

**Created:** July 22, 2026
**Last Updated:** November 5, 2026 (v0.7 — ERR-038-006 / M1: distinguish live capability refusal, owner diagnosis mapping and interim client wage policy.)
**Last Updated (prior):** November 5, 2026 (v0.6 — T4 / ERR-038-006: repair amendment version collision; explain dependency choice and P7b lifecycle scope.)
**Last Updated (prior):** November 5, 2026 (v0.5 — ERR-038-006 / ERR-031-017: authoritative live worker diagnosis; cached UI check is a hint.)
**Last Updated (prior):** November 5, 2026 (v0.4 — ERR-038-006: Appendix C.2 lists payload-carrying career commands (SubmitTransfer → SubmitTransferBid))
**Version:** 0.7
**Status:** APPROVED

---

## Appendix A — Constant catalogue (`UiFrameworkConstants`)

`[GT]` magnitudes are presentation-feel values (illustrative; tunable without a contract change). The UI
has no sim constants.

| Constant | Tag | Value | Meaning |
|---|---|---|---|
| `MATCH_VIEW_REFRESH_HZ` | `[GT]` | 60 | render-cadence poll rate of the match view (a client feel value, decoupled from the sim tick — KD-3) |
| `NON_MATCH_REFRESH_MODE` | `[GT]` | on-change | non-match screens re-project on a state change, not on a clock |

No `[CROSS]`/`[FIXED]`/`[EST]` constants — the framework consumes observation surfaces and public seams,
not numeric catalogues. `SQUAD_SIZE` (the projection bounds gate, F1) is an existing `[CROSS]` mirror of
`MatchEngineConstants`, not re-declared here.

## Appendix B — Observation surface → view-model projection table

The concrete match-view projection (`MatchViewModelSource`, §3.4) reads **only the streamer's published
`LiveMatchFrame`** — it holds no `MatchEngine` reference (FR-UI-005; reading the engine directly from the
render thread would race the streamer's tick thread). The streamer, on its own thread, is what reads the
engine's observation surface to build each frame; the source reads the frame:

| Source (read-only) | Into `MatchFrameView` | Gate |
|---|---|---|
| `LiveMatchStreamer.TryGetLatestFrame(out frame)` | the whole immutable frame | F5 (false ⇒ empty/last-known) |
| `frame` ball + per-agent position/state | ball + agents | F1 bounds / F2 NaN |
| `frame` per-agent team / GK flag | team / GK flag | F1 bounds |
| `frame` scoreline / end flag | score / end | — |

The engine surfaces the *streamer* reads to populate a frame (`BallView`/`AgentView(i)`/`AgentTeamId(i)`/
`AgentIsGoalkeeper(i)`/`HomeScore`/`AwayScore`/`MatchEnded`, verified public in `MatchEngine.cs`) are
`LiveMatchStreamer`'s concern, not the view-model source's. Other view models (a #30 season VM, a #37
analytics VM) are projected by **their** owning spec's source, not #38 (KD-5).

## Appendix C — Command-intent → public-seam routing table (§3.3)

Each intent kind maps to exactly one **existing public** seam; an unmapped kind throws (F3). The routing
is split across dispatchers by owning surface — the **match-tactics** dispatcher owns the first three
(live-match intents, marshaled per FR-UI-023); a **season** dispatcher owns `AdvanceRound`, `AdvanceDay` and
`StartNextSeason` (single-threaded turn-based seams, called directly; ERR-038-005):

| `ManagerIntent.Kind` | Dispatcher | Public seam (verified) | Payload |
|---|---|---|---|
| `SetTeamTactic` | match-tactics | `MatchEngine.SetTeamTactic(teamId, in tactic)` | teamId + `TeamTactic` |
| `SetPlayerTactic` | match-tactics | `MatchEngine.SetPlayerTactic(agentId, in tactic)` | agentId + `PlayerTactic` |
| `Substitute` | match-tactics | `MatchEngine.SubstitutePlayer(teamId, outSlot, benchIndex, reason)` | slots + reason |
| `AdvanceRound` | season | #30 `AdvanceAndPlayNextRound(squads)` (forward seam); for a loop that owns its provider, the no-argument `AdvanceAndPlayNextRound()` (ERR-028-010) | (squad provider) / none |
| `AdvanceDay` | season | #30 `SeasonLoop.AdvanceDays(1)` (ERR-038-005) | none |
| `StartNextSeason` | season | #30 `SeasonLoop.RollToNextSeason()` (ERR-038-005) | none |
| any management action whose owning-spec seam is not built | — | **throws** (F3) | filed against the owning spec (KD-4) |

### Appendix C.2 — Career commands that carry a payload (ERR-038-006)

A **career command** is a mutation of the running simulation/loop. `ManagerIntent` cannot carry offer
terms. The framework intentionally keeps no Transfers dependency; adding an Offer payload and IntentKind
was rejected because it couples reusable framework intents to feature-owned terms. This is a dependency
choice, not a prohibition in FR-UI-001. A payload-carrying career command is a **composition-root
coordinator operation** onto one named public owner seam (FR-UI-010 amendment). It adds no mutation path (FR-UI-003); a seam that does not
exist is filed against the owner (FR-UI-020). Intents remain the route for payload-free and match commands.

| Coordinator operation (`src/client-app`) | Session call | Public owner seam (verified) | Typed result | Typed client refusals |
|---|---|---|---|---|
| `ClientCareerCoordinator.SubmitTransfer(in Offer)` | `CareerSession.SubmitTransfer` | #30 `SeasonLoop.SubmitTransferBid(in Offer)` → #31 FR-TX-025 UI seam | #31 `TransferSubmissionOutcome` (`Accepted`, `Rejected`, `CounterOffered`, `InsufficientBudget`, `SquadFull`, `SquadDepleted`), published as `TransferResult` | `TransferOfferIssue` (`TransfersUnavailable`, `WindowClosed`, `UnknownPlayer`, `WrongDirection`, `WrongCounterparty`, `FeeNegative`, `WageOutOfRange`, `LengthTooShort`), live worker capability absence yields `TransfersUnavailable`; #31 `TransferBidIssue` maps the owner-diagnosed categories; the client upper wage ceiling is an interim policy after diagnosis, all before SubmitTransferBid |

A refusal is a value published by the worker without calling the mutation seam. On the serialized
worker, live session/loop capability is checked first: absent #31 state or #28 roster authority yields
`TransfersUnavailable` without Diagnose or Submit. Valid current-format optional-empty #31 saves can
resume in this state; the retained market projection cannot decide capability. For a wired career,
the authoritative pre-check is #31 `Diagnose` through `SeasonLoop.DiagnoseTransferBid` /
`CareerSession.DiagnoseTransfer`, run against live state; owner issues are mapped before the client
upper wage ceiling is applied, then submission may call the mutation seam. `TransferOfferCheck`
is only a non-authoritative hint over cached snapshot facts plus the client's wage-bound policy; a stale
market can disagree with it. F4/F6 malformed/stale offers are reported as typed refusals by the worker,
while invariant and integration defects remain fail-loud. No blanket promise depends on cached UI state.

C.2's mutation table is not an inventory of every coordinator method. P7b also has these legitimate
lifecycle/persistence/navigation operations, which do not mutate a running simulation through a new path:

| Coordinator operation | Responsibility and existing public route |
|---|---|
| `SaveTo` | Persistence only: `CareerSession.SaveTo` → the save-slot store; no loop mutation. |
| `LoadCareer` / `ContinueCareer` | Restore a session through `LoadCareerSource` → `CareerSession.Resume` / `ContinueLatest`; publish and navigate after successful Home projection. |
| `OpenNewCareer` | Navigation plus `NewCareerSetup.Prepare` preview generation; no running-career mutation. |
| `SelectClub` | Idle UI-owned preview selection through `NewCareerSetup.Select`; no simulation read/write. |
| `StartCareer` | Lifecycle creation through `NewCareerSetup.Start` → `CareerSession.StartNew`; creates and autosaves a new loop, then publishes/navigates. |

These operations are explicitly outside the running-loop command definition, not loopholes in the
public-owner-seam rule. Failed entry projection releases the held session and preserves the autosave.
The season progress mutations remain intents in Appendix C's first table.

## Appendix D — Worked navigation transition (§3.5)

`MainMenu`=0, `MatchView`=1, `Tactics`=2 registered; `stack=[0]`:

| Step | Action | Result | Note |
|---|---|---|---|
| 1 | `Push(1)` | `[0,1]`, Current=1 | enter match view (pure-observation surface) |
| 2 | `Push(2)` | `[0,1,2]`, Current=2 | open tactics over the match |
| 3 | dispatch `SetTeamTactic` | sim mutates via the public seam only | the one mutation path (KD-1) |
| 4 | `Pop()` | `[0,1]`, Current=1 | back to the match |
| 5 | `Push(99)` (unregistered) | **throw** (F2) | fail-loud navigation |

The sequence exercises no Unity and no sim mutation except the single public-seam call — the pure
substrate the framework pins.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial appendices: constant catalogue, observation→VM projection table, intent→seam routing table, worked navigation transition. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+2L; M-1 match-view reads streamer frame, M-2 cross-thread command marshaling FR-UI-023/F6, L-1 dispatcher split, L-2 Pop-below-root) → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
| 0.3 | 2026-10-18 | — | ERR-038-005: the season dispatcher's rows extended to `AdvanceDay → AdvanceDays(1)` and `StartNextSeason → RollToNextSeason()`, and `AdvanceRound` notes the no-argument overload. Additive; no existing row changed meaning. Landed with the code (`IntentKind` 4–6, `IntentRouting`, `client-app/SeasonCommandDispatcher`). |
| 0.4 | 2026-11-05 | — | ERR-038-006: payload-carrying career commands are sanctioned as composition-root (client-app) coordinator operations onto a named public owner seam; FR-UI-003 and FR-UI-020 unchanged; intents stay the route for payload-free and match commands. First instance: `SubmitTransfer` → `SeasonLoop.SubmitTransferBid` (#31 FR-TX-025). Additive; approval status unchanged. |
| 0.5 | 2026-11-05 | — | ERR-038-006 / ERR-031-017: authoritative live worker diagnosis; cached UI check is a hint. |
| 0.6 | 2026-11-05 | — | T4 / ERR-038-006: repair amendment version collision; explain dependency choice and P7b lifecycle scope. |
| 0.7 | 2026-11-05 | — | ERR-038-006 / M1: distinguish live capability refusal, owner diagnosis mapping and interim client wage policy. |
#endregion
