# UI / Client Framework #38 — Section 8: References

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 (2M+2L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 8.1 In-project sources (authoritative)

- **Code Standards #20 §3.5.2** — the authoritative assembly layer taxonomy (presentation reads sim; sim
  never references presentation) the KD-1 no-reverse-reference invariant enforces.
- **`src/match-viewer/`** — the presentation-layer precedent: `LiveMatchStreamer` (paces a real
  `MatchEngine` on its own thread; the only caller of `RunTick`; lock-protected latest-frame handoff) +
  `LiveMatchServer` (playback-only `/control`; **never holds a `MatchEngine` reference, only a
  `LiveMatchStreamer`** — the disjoint-surface precedent for KD-1's pure-observation class) +
  `MatchReplay`/`ReplayFrame` (immutable snapshot VMs — the read-only-projection precedent) +
  `MatchViewerTests` (the observer-neutrality digest-lock #38 reuses).
- **`MatchEngine` public seams** — `SetTeamTactic`/`SetPlayerTactic`/`SubstitutePlayer`/`ConfigureSquads`
  (the command seams the dispatcher routes to) + the observation surface (`BallView`/`AgentView`/
  `AgentTeamId`/`PossessingAgentId`/`HomeScore`/`AwayScore`/`MatchEnded`/`CurrentTick`).
- **Season & Competition Loop #30** — the `AdvanceAndPlayNextRound` day-advance seam a season screen
  dispatches to (APPROVED forward design).
- **Match Analytics #37** — the `MatchAnalyticsResult` view models the post-match screen binds (APPROVED
  forward design).

## 8.2 Domain conventions (background)

- **Unity UGUI** — the master-plan §3.4 client UI toolkit; the rendering binding target (§7.2). This spec
  pins the layer contract + pure substrate, which are UGUI-agnostic; no UGUI API is a normative
  dependency of the substrate.
- **The MVVM / read-only-projection pattern** — the standard presentation-layer separation (a view binds
  an immutable view model; commands flow one way to the model) — a design-pattern convention, not a
  citable formula. #38 pins the one-directional contract, not a framework library.

> No external library or vendor UI framework is a normative dependency: the substrate is plain C# +
> the in-project observation/command seams; the UGUI binding (§7.2) is the only Unity-coupled part and is
> Unity-host-gated.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial references: the #20 §3.5.2 layer taxonomy + the `match-viewer`/`LiveMatchServer` precedents + the command/observation seams + the #30/#37 forward view-model sources. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+2L; M-1 match-view reads streamer frame, M-2 cross-thread command marshaling FR-UI-023/F6, L-1 dispatcher split, L-2 Pop-below-root) → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
