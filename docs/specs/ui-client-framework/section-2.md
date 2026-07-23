# UI / Client Framework #38 — Section 2: Functional Requirements, Data Structures, Failure Modes

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 (2M+2L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 2.1 Functional requirements (FR-UI-001..023)

### Layer contract (KD-1)
- **FR-UI-001** — No sim/loop/analytics assembly may reference the UI assembly (the asmdef reference
  direction; the `match-viewer` no-reverse-reference lock).
- **FR-UI-002** — A view model MUST be an immutable read-only projection: no live-buffer reference or
  mutable engine handle may escape into it.
- **FR-UI-003** — Sim mutation MUST route ONLY through an existing **public** sim/loop command seam. The
  framework MUST provide no mutation path of its own.
- **FR-UI-004** — The UI MUST compute no game state, analytics, or localized text; it composes the
  outputs of the owning specs (KD-5).
- **FR-UI-005** — A **pure-observation** surface (match view, playback) MUST hold no engine reference (the
  `LiveMatchServer` precedent). A **command** surface MAY reference the seam owner, but MUST be able to
  invoke only public, sim-validated seams (the assembly boundary hides internals).

### View-model contract
- **FR-UI-006** — `IViewModelSource<T>.Project()` MUST return an immutable `T` derived purely from
  observation surfaces; it MUST NOT mutate any sim/loop state.
- **FR-UI-007** — A view-model type `T` MUST be a value type or an immutable snapshot (cloned arrays /
  read-only collections — the `MatchReplay` precedent), never a wrapper over a live buffer.
- **FR-UI-008** — Projection MUST fail loud (throw) on malformed observation input: an agent index
  outside `[0, SQUAD_SIZE)` (F1) or a non-finite sampled value (the `match-viewer` NaN-gate).

### Navigation shell
- **FR-UI-009** — `NavigationShell` MUST be a deterministic screen-registration + transition state
  machine (push/pop/replace), fully testable without Unity (no UGUI dependency in the state machine).
- **FR-UI-010** — A screen MUST be registered as `{ view-model source, command dispatcher }`; the shell
  MUST NOT hard-code any screen.
- **FR-UI-011** — A transition to an unregistered screen id MUST fail loud (F2), never silently no-op;
  likewise a `Pop` below the root screen MUST fail loud (the shell never empties its stack).

### Command dispatch
- **FR-UI-012** — `ICommandDispatcher` MUST map a typed `ManagerIntent` to an existing public seam; it
  MUST carry no seam of its own (KD-1).
- **FR-UI-013** — The match-tactics dispatcher MUST map intent only to `SetTeamTactic` / `SetPlayerTactic`
  / `SubstitutePlayer` (the verified public seams); no other mutation.
- **FR-UI-014** — A dispatcher MUST NOT expose or invoke any non-public sim mutation (enforced by the
  assembly boundary). An intent with no mapped public seam MUST fail loud (F3), never invent a seam.

### Match view (KD-3)
- **FR-UI-015** — The match view MUST refresh by reading the latest published immutable frame
  (`LiveMatchStreamer.TryGetLatestFrame`); it MUST NOT call `RunTick` or otherwise advance the sim.
- **FR-UI-016** — Refresh MUST occur at render cadence, decoupled from the sim tick; each rendered frame
  MUST be a complete consistent snapshot (no torn/partial read of a live buffer).
- **FR-UI-017** — Observing a match through the UI MUST be byte-identical to an unobserved same-seed run
  (observer-neutrality; the `MatchViewerTests` digest-lock).

### Composition & scope
- **FR-UI-018** — A screen MUST bind independent read-only inputs (a sim/loop VM + #37 analytics + later
  #48/#49), each produced by its owning spec; the UI owns no domain logic (KD-5).
- **FR-UI-019** — The framework MUST NOT author a screen-specific view model for unbuilt data; screen
  specs are Wave-7, gated on their data spec (KD-2).
- **FR-UI-020** — A screen needing a missing command seam MUST file it against the owning spec; the UI
  MUST add no mutation path (KD-4).
- **FR-UI-021** — The UGUI rendering binding MUST be Unity-host-gated; this spec pins the pure substrate +
  layer contract, not the rendering (§7.2).
- **FR-UI-022** — #38 MUST register no RNG stream / domain tag / `SubsystemOrdinal`, and MUST hold no
  persistent sim state / bump no save format (the `match-viewer` presentation class).
- **FR-UI-023** — During a **live streamed match** (the engine owned by the streamer's tick thread), a
  command MUST be **marshaled onto the sim thread** (enqueued to the streamer, applied between ticks),
  never called cross-thread (F6) — the public seams are not documented cross-thread-safe. In a
  single-threaded context (pre-kickoff config, turn-based advance with no running streamer) the dispatcher
  MAY call the seam directly (§3.3).

## 2.2 Data structures

```
// The read-only projection contract (KD-1). T is an immutable value type.
interface IViewModelSource<T> where T : struct {
    T Project();   // pure; reads observation surfaces; no sim mutation
}

// A typed manager intent (the ONLY thing a dispatcher accepts).
readonly struct ManagerIntent {
    IntentKind Kind;          // SetTeamTactic | SetPlayerTactic | Substitute | AdvanceRound | ...
    // typed payload per kind (teamId+TeamTactic, agentId+PlayerTactic, sub slots, ...)
}

// Routes an intent to an EXISTING public seam (KD-1) — carries no seam of its own.
interface ICommandDispatcher {
    void Dispatch(in ManagerIntent intent);   // throws on an unmapped intent (F3)
}

// The pure navigation state machine (no UGUI dependency).
readonly struct ScreenId { int Value; }
struct ScreenRegistration { ScreenId Id; /* view-model source + dispatcher handles */ }
class NavigationShell {
    void Register(in ScreenRegistration reg);
    void Push(ScreenId id);      // throws on unregistered id (F2)
    void Pop();
    void Replace(ScreenId id);
    ScreenId Current { get; }
}

// The one concrete match-view projection (over LiveMatchStreamer / the observation surface).
struct MatchViewModelSource : IViewModelSource<MatchFrameView> {
    MatchFrameView Project();    // reads TryGetLatestFrame; never RunTick (FR-UI-015)
}
readonly struct MatchFrameView { /* immutable ball + agents + score snapshot */ }
```

## 2.3 Failure modes

| # | Condition | Handling | FR |
|---|---|---|---|
| **F1** | Projection reads an agent index outside `[0, SQUAD_SIZE)` or a non-finite value | **Throw** (fail loud) | FR-UI-008 |
| **F2** | `Push`/`Replace` to an unregistered `ScreenId` | **Throw** (never silent no-op) | FR-UI-011 |
| **F3** | `Dispatch` of a `ManagerIntent` with no mapped public seam | **Throw** (never invent a seam / silently drop) | FR-UI-014 |
| **F4** | A view model is constructed holding a live buffer / mutable engine reference | **Forbidden by the contract** (value type / cloned snapshot); a returned VM is immutable so a mutation attempt is a compile error | FR-UI-002/007 |
| **F5** | Match view rendered before the streamer has published a frame (`TryGetLatestFrame` returns false) | Render the last-known / empty frame (documented) — **not** a crash and **not** a `RunTick` to force one | FR-UI-015 |
| **F6** | A command dispatched **cross-thread** directly against the engine during a live streamed match (bypassing the streamer's enqueue) | **Forbidden** — the live-match dispatcher MUST marshal via `streamer.EnqueueIntent` (applied on the tick thread); direct cross-thread `route()` races the tick | FR-UI-023 |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial FR set (FR-UI-001..023), data structures, failure modes F1–F6. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+2L; M-1 match-view reads streamer frame, M-2 cross-thread command marshaling FR-UI-023/F6, L-1 dispatcher split, L-2 Pop-below-root) → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
