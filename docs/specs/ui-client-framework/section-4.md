# UI / Client Framework #38 — Section 4: Architecture

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 (2M+2L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 4.1 Assembly placement & reference direction

New presentation-layer assembly **`TacticalDirector.UiFramework`** (`src/ui-framework/`).

**Reference direction (the load-bearing KD-1 invariant):**
- The **generic substrate** (`IViewModelSource<T>`, `ICommandDispatcher`, `ManagerIntent`,
  `NavigationShell`, `ScreenId`) references **nothing sim-side** — it is parameterized over `T` and over
  intent/dispatcher handles.
- The **concrete** surfaces reference only assemblies that **already exist**: `MatchViewModelSource` +
  the match-tactics dispatcher reference `MatchEngine` + `MatchViewer` (both built). A reference to
  `MatchAnalytics` (#37), the #30 season-save, or any data spec is added **only when that spec is built
  and a concrete source/dispatcher projects it** — never speculatively (the FR-LW-031 phantom-dependency
  rule; a screen against unbuilt data is Wave-7, KD-2).
- **Referenced by no sim/loop/analytics assembly** (FR-UI-001 — enforced by the absence of the reverse
  reference, exactly as `match-viewer` is unreferenced by sim; §5 asserts it).

The UGUI screen assemblies (Wave-7) are **separate** assemblies that reference this framework; they are
not part of this spec.

**One presentation-side addition to `LiveMatchStreamer` (FR-UI-023):** a public `EnqueueIntent` +
between-ticks apply, so a live-match command runs `route(intent)` on the thread that owns the engine (the
tick thread), not cross-thread. `LiveMatchStreamer` already owns that thread and already has the
`TickOnce`/`ApplyCapturedFrame` between-ticks seam; the enqueue is the write-side analogue of the
existing read-side `TryGetLatestFrame` handoff. It stays in the presentation layer (the streamer is
`match-viewer`), so the sim gains no new surface — the engine's public seams are unchanged; only the
*thread they are called from* is fixed.

## 4.2 File layout (proposed)

```
src/ui-framework/
├── ui-framework.asmdef
├── IViewModelSource.cs        // KD-1 projection contract (generic)
├── ICommandDispatcher.cs      // KD-1 dispatch contract (generic)
├── ManagerIntent.cs           // typed intent
├── NavigationShell.cs         // pure screen state machine
├── ScreenId.cs                // + ScreenRegistration
├── MatchViewModelSource.cs    // concrete match-view projection (over LiveMatchStreamer)
├── MatchFrameView.cs          // immutable match snapshot VM
├── MatchTacticsDispatcher.cs  // intent -> SetTeamTactic/SetPlayerTactic/SubstitutePlayer
├── UiFrameworkConstants.cs    // [GT] refresh-cadence (presentation feel, not a sim constant)
└── Tests/
    ├── ui-framework-tests.asmdef
    ├── NavigationShellTests.cs
    ├── CommandDispatchTests.cs
    └── MatchViewObserverNeutralityTests.cs
```

## 4.3 The UGUI-binding deferral (Unity-host-gated)

This spec pins the **layer contract + the pure substrate** (projection / navigation / dispatch / the
match-view cadence) — all testable without Unity. The **UGUI rendering binding** (canvases, prefabs,
layout, input wiring) is gated on Unity host access (the standing "full in-Unity rendering blocked on
Unity host access" OPEN ISSUE; the same gate `match-viewer`'s live surface hit at its "at-minimum a
live-updating viewer" floor). The rendering binds the same view models + dispatchers this spec defines;
no contract changes when it lands (§7.2). Deferring it is not a gap — it is the honest Stage-binding, and
nothing in the pinned substrate depends on it.

## 4.4 Determinism & naming (KD / FR-UI-022)

#38 registers **no** RNG stream, **no** `DOMAIN_TAG_*`, **no** `SubsystemOrdinal`, holds no persistent
sim state, and bumps no save format — it draws nothing, advances nothing, and persists nothing to the
determinism save (UI preferences/layout are client-local settings outside it). It appears nowhere in the
#16 §3.4 catalogue (the `match-viewer` class); there is nothing to reserve and no `_RESERVED_`
placeholder is warranted (the #37 posture).

## 4.5 CS0104 hazard (name collision)

`MatchFrameView` / `ManagerIntent` / `NavigationShell` are new names; a grep of `docs/specs/**` +
`src/**` at T0 MUST confirm no existing type shares them before the assembly is wired (the
`TacticTranslation` / `PlayerAttributes` CS0104 precedent). If a future spec brings a same-named type
into a shared scope, fully-qualify from line one (the KD-P6 discipline).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial architecture: assembly placement + the generic-substrate-references-nothing / concrete-references-only-built reference direction, the UGUI-binding deferral, no-RNG/tag/ordinal, CS0104 note. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+2L; M-1 match-view reads streamer frame, M-2 cross-thread command marshaling FR-UI-023/F6, L-1 dispatcher split, L-2 Pop-below-root) → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
