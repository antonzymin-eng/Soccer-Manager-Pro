# UI / Client Framework #38 — Appendices

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 (2M+2L) → AR-2 convergence; APPROVED)
**Version:** 0.2
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
(live-match intents, marshaled per FR-UI-023); a **season** dispatcher owns `AdvanceRound` (a
single-threaded turn-based seam, called directly):

| `ManagerIntent.Kind` | Dispatcher | Public seam (verified) | Payload |
|---|---|---|---|
| `SetTeamTactic` | match-tactics | `MatchEngine.SetTeamTactic(teamId, in tactic)` | teamId + `TeamTactic` |
| `SetPlayerTactic` | match-tactics | `MatchEngine.SetPlayerTactic(agentId, in tactic)` | agentId + `PlayerTactic` |
| `Substitute` | match-tactics | `MatchEngine.SubstitutePlayer(teamId, outSlot, benchIndex, reason)` | slots + reason |
| `AdvanceRound` | season | #30 `AdvanceAndPlayNextRound(squads)` (forward seam) | (squad provider) |
| any management action whose owning-spec seam is not built | — | **throws** (F3) | filed against the owning spec (KD-4) |

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
#endregion
