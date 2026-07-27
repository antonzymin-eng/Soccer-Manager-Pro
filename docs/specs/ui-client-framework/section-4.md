# UI / Client Framework #38 — Section 4: Architecture

**Created:** July 22, 2026
**Last Updated:** July 27, 2026 (v0.3 — back-prop landed atomically with the ten-spec approval wave; see the version-history row)
**Last Updated (prior):** July 22, 2026 (v0.2 — section-file PASS-1 (2M+2L) → AR-2 convergence; APPROVED)
**Version:** 0.3
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

### 4.4.1 The client-local settings store — #38 owns it (ERR-038-004, at #51's approval)

**#38 owns exactly one client-local settings store**: its file location, the registration of contributed
**schema fragments**, and the **failure policy**. Contributors supply fragments and own no file:
**#49** (locale + a11y options, FR-LC-018), **#38** itself (UI preferences/layout), **#48**
(presentation toggles), **#51** (per-bus volume/mute), **#39** (achievement progress + Cloud sync state).

**This was filed because five specs named this store and none owned it** — each was one implementation
decision away from writing its own file, and #48's text already claims *"audio levels"* while #51 also
describes them, so two approved specs believed they described the same state. #38 is the natural owner: it
is the client framework, it already holds UI preferences, and it is the only candidate every contributor
already composes with.

**The failure policy is reset-to-defaults-and-continue, and it is deliberately the opposite of #50's.**
An unreadable or partially-invalid fragment resets **only the invalid fields** and launch proceeds
silently. #50 refuses a save it cannot classify because a career is irreplaceable; a volume slider is not,
and applying save-grade refusal to preferences would let a corrupt settings byte **block launch**. This
store is therefore **outside #50's migration scope** entirely: there is no format version and nothing to
migrate, because an unreadable fragment is already *defined* as "use the defaults".

**It remains outside the determinism save** (FR-UI-022 above is unchanged): no sim assembly reads it, it
reaches no digest, and it bumps no save format.

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
| 0.3 | 2026-07-27 | — | **ERR-038-004** (at #51's approval): new **§4.4.1 — #38 owns the one client-local settings store** (location, fragment registration, failure policy), with #49/#38/#48/#51/#39 contributing fragments. Filed because **five specs named this store and none owned it**, and two approved specs both described the audio-levels state. The policy is **reset-to-defaults-and-continue**, deliberately the inverse of #50's refusal — a corrupt preference byte must not block launch — which also places the store outside #50's migration scope. FR-UI-022 is unchanged: still outside the determinism save. |
#endregion
