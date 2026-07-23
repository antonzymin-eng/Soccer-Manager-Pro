# UI / Client Framework #38 — Section 7: Forward Extensions

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 (2M+2L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 7.1 The Wave-7 screen-spec roadmap (KD-2)

The screens are separate specs, each **gated on its data spec existing** (a screen against unbuilt data
is the phantom-consumer trap). Each registers into the `NavigationShell` and binds `IViewModelSource<T>`
+ `ICommandDispatcher` — **no framework change per screen** (this slice is the identity they extend).

| Screen (later spec) | Binds (view model from) | Dispatches to (owning-spec seam) | Gated on |
|---|---|---|---|
| Tactics screen (S1) | #21 tactic types / engine observation | `SetTeamTactic`/`SetPlayerTactic` (exist) | — (buildable after this framework) |
| Post-match report | #37 `MatchAnalyticsResult` | (read-only) | #37 (APPROVED) |
| Season/league screen | #30 table + calendar VM | `AdvanceAndPlayNextRound` | #30 (APPROVED) |
| Squad screen | #27 roster VM | selection seam | #27 (APPROVED) + a selection seam |
| Transfer screen | #31 transfer VM | transfer-action seam | **#31 (not built)** — the seam is #31's to add (KD-4) |
| Training / scouting | #29 / #32 VMs | their action seams | **#29 / #32 (not built)** |

The framework does **not** author any of these VMs or dispatchers now (KD-2). A screen whose owning-spec
seam is missing (transfer/training/scouting) waits for that spec to add the seam (FR-UI-020 / KD-4).

## 7.2 The UGUI rendering binding (Unity-host-gated)

The pure substrate (projection / navigation / dispatch / match-view cadence) is pinned and testable now.
The **UGUI rendering** (canvases, prefabs, layout, input) binds the same view models + dispatchers when a
Unity host exists (the standing OPEN ISSUE). No contract changes when it lands: the rendering is a *view*
over the already-defined substrate, exactly as `match-viewer`'s live HTML surface was a view over the
observation surface. The interactive match view is then promoted from the web viewer
(`LiveMatchServer`) into the in-client UGUI surface, still reading the same `LiveMatchStreamer` frames.

## 7.3 Composition of #37 / #48 / #49 (KD-5)

A screen composes independent read-only inputs, each owned elsewhere:
- **#37 analytics** — a `MatchAnalyticsResult` VM the post-match screen binds (the UI renders, computes
  no stat).
- **#48 presentation depth** — commentary/animation/audio assets a screen binds when #48 lands (no UI
  logic).
- **#49 localization** — localized strings the UI renders through #49's seam; the UI holds no string
  catalogue (that is #49). The view-model contract is the composition seam; adding an input never
  changes the framework.

## 7.4 Generalization seams

- `IViewModelSource<T>` / `ICommandDispatcher` are generic, so a new screen or data source plugs in
  without a framework change.
- `ManagerIntent` is an extensible typed intent — a new intent kind maps to a new **existing** public
  seam (never a new UI mutation path, KD-1/KD-4).
- The refresh-cadence `[GT]` is a presentation-feel value (illustrative), tunable without a contract
  change.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial forward extensions: the Wave-7 screen-spec roadmap (data-spec gating), the UGUI-host binding, the #37/#48/#49 composition seam, generalization seams. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+2L; M-1 match-view reads streamer frame, M-2 cross-thread command marshaling FR-UI-023/F6, L-1 dispatcher split, L-2 Pop-below-root) → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
