# UI / Client Framework #38 — Section 3: Contracts & Algorithms

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 (2M+2L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

The framework's "algorithms" are the three pure contracts (projection, navigation, dispatch) plus the
match-view cadence. All are testable without Unity (KD-2 / §5).

## 3.1 The projection model (view-model contract)

An `IViewModelSource<T>` is a pure read: `Project()` reads observation surfaces and returns an immutable
`T`. Rules (FR-UI-002/006/007/008):
- **Pure:** no sim/loop mutation, no draw, no wall-clock — the same observed state yields the same `T`.
- **Immutable output:** `T` is a value type or an immutable snapshot (cloned arrays / read-only
  collections, the `MatchReplay` precedent); no live buffer or mutable engine handle escapes into `T`.
- **Fail-loud inputs:** every agent index is bounds-gated (F1) and every sampled value NaN-gated before
  it enters `T` (the `match-viewer` guard).

The projection is the **composition seam** (KD-5): a screen holds several `IViewModelSource<…>` (a
sim/loop source + a #37 analytics source + later #48/#49), binds each independently, and owns none of
their logic. The framework provides the *contract* and the *one concrete* match source (§3.4); other
sources are their owning spec's (a #37 `MatchAnalyticsResult` is projected by #37, not #38).

## 3.2 The navigation state machine

`NavigationShell` is a deterministic stack machine over registered `ScreenId`s (FR-UI-009/010/011):

```
Register(reg):   registry[reg.Id] = reg            # explicit; no hard-coded screen
Push(id):        require id in registry (else throw, F2); stack.push(id)
Pop():           require stack.count > 1 (else throw); stack.pop()
Replace(id):     require id in registry (else throw, F2); stack.top = id
Current:         stack.top
```

Pure and UGUI-free — a screen is `{ view-model source, command dispatcher }`, so the shell knows only
ids and handles, never a concrete Unity view. This is what makes the navigation contract testable
without a Unity host, and what lets Wave-7 screen specs register into it without changing the framework.

## 3.3 Command-dispatch routing

`ICommandDispatcher.Dispatch(intent)` maps a typed `ManagerIntent` to an **existing public seam**
(FR-UI-012/013/014). The match-tactics dispatcher's routing:

```
route(intent):
    switch intent.Kind:
        SetTeamTactic:    engine.SetTeamTactic(intent.teamId, intent.teamTactic)     # public seam
        SetPlayerTactic:  engine.SetPlayerTactic(intent.agentId, intent.playerTactic) # public seam
        Substitute:       engine.SubstitutePlayer(intent.teamId, intent.out, intent.bench, intent.reason)
        default:          throw  # F3 — no invented seam, no silent drop
```

The dispatcher can call **only** the public seams above (the assembly boundary hides sim internals);
an unmapped intent throws (F3) — never invented UI-side (KD-4).

**Threading (FR-UI-023 / KD-3): a live-match command MUST be marshaled onto the sim thread, not called
cross-thread.** During a live streamed match the `MatchEngine` is owned by the streamer's tick thread
(the only caller of `RunTick`), and the public seams are **not** documented cross-thread-safe (e.g.
`SetTeamTactic` stages a pending tactic the tick thread reads at the stride boundary). So the live-match
dispatcher **enqueues** the intent to the streamer, which applies `route(intent)` on its **own tick
thread between ticks** — the same thread that owns the engine, so no read/write race:

```
Dispatch(intent):    # live-match dispatcher
    streamer.EnqueueIntent(intent)     # applied by the streamer's thread before the next tick; route() runs there
```

`EnqueueIntent` is a small presentation-side addition to `LiveMatchStreamer` (it already owns the tick
thread; §4). In a **single-threaded** context (pre-kickoff configuration, or a turn-based advance where
no streamer is running), the dispatcher calls `route(intent)` directly — there is no other thread to race
(F6 forbids only the cross-thread *direct* call during a live match).

## 3.4 The match-view refresh cadence (KD-3)

The match view is a pure-observation surface (FR-UI-005 — holds no engine, only the streamer):

```
// at RENDER cadence (decoupled from the sim tick):
if streamer.TryGetLatestFrame(out frame):   # latest complete immutable snapshot
    render(frame)                            # ball + agents + score
else:
    render(lastKnown or empty)               # F5 — never RunTick to force a frame
```

The streamer advances the sim on its own thread and publishes each `LiveMatchFrame` under lock as a
complete snapshot; the UI reads the latest and never tears (no partial live-buffer read) and never
mutates (observer-neutrality, FR-UI-017 — the `MatchViewerTests` digest-lock). Non-match screens refresh
on-change (e.g. a season screen re-projects after the #30 `AdvanceAndPlayNextRound` seam returns), never
inside a match loop.

## 3.5 Worked navigation transition

`MainMenu` (id 0), `MatchView` (id 1), `Tactics` (id 2) registered. Start `stack=[0]`:
- `Push(1)` → `stack=[0,1]`, `Current=1` (enter match view).
- `Push(2)` → `stack=[0,1,2]`, `Current=2` (open tactics over the match).
- a tactics change dispatches `SetTeamTactic` (§3.3) — the sim mutates via its public seam only.
- `Pop()` → `stack=[0,1]`, `Current=1` (back to the match).
- `Push(99)` where 99 is unregistered → **throw** (F2).

The whole sequence exercises no Unity and no sim mutation except the one public-seam call — the pure
substrate the framework pins (KD-2).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial contracts: projection model, navigation state machine, dispatch routing, match-view cadence + worked transition. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+2L; M-1 match-view reads streamer frame, M-2 cross-thread command marshaling FR-UI-023/F6, L-1 dispatcher split, L-2 Pop-below-root) → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
