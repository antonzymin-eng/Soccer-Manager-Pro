# UI / Client Framework #38 — Section 5: Test Plan

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 (2M+2L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED

---

## 5.1 Test groups

### T-UI-LAYER — Layer contract (FR-UI-001..005)
- **T-UI-LAYER-001** — **No reverse reference:** assert no sim/loop/analytics asmdef references
  `TacticalDirector.UiFramework` (the `match-viewer` no-reverse-reference lock; a build/asmdef audit).
- **T-UI-LAYER-002** — A returned view model exposes no mutable engine handle / live buffer (reflection
  or type-shape assert: `T` is a value type / immutable snapshot, FR-UI-002/007).

### T-UI-NEU — Observer neutrality (FR-UI-017)
- **T-UI-NEU-001** — A match observed through `MatchViewModelSource` (repeated `Project()` at render
  cadence) produces the **same match digest** as an unobserved same-seed run (the `MatchViewerTests`
  digest-lock — projecting perturbs nothing; the source never calls `RunTick`).

### T-UI-DISPATCH — Command dispatch (FR-UI-012..014)
- **T-UI-DISPATCH-001** — A `SetTeamTactic` intent routes to `MatchEngine.SetTeamTactic` and mutates
  exactly that team's tactic (via a `TestOnly_` read-back); `SetPlayerTactic` / `Substitute` likewise.
- **T-UI-DISPATCH-002** — An intent with no mapped public seam ⇒ **throw** (F3), never a silent drop or
  an invented seam.
- **T-UI-DISPATCH-003** — Dispatch mutates **only** through the public seams (no test can observe a sim
  internal changed by the dispatcher — the assembly-boundary guarantee).
- **T-UI-DISPATCH-004** — **Marshaling (FR-UI-023):** the live-match dispatcher `Dispatch(intent)` calls
  `streamer.EnqueueIntent` (a spy streamer records the enqueue) and does **not** call the engine seam on
  the dispatching thread; the enqueued intent's `route()` is applied on the streamer's tick thread
  between ticks (F6). The single-threaded dispatcher calls `route()` directly (no streamer present).

### T-UI-NAV — Navigation state machine (FR-UI-009..011)
- **T-UI-NAV-001** — `Register`/`Push`/`Pop`/`Replace`/`Current` follow the §3.2 stack semantics
  (the §3.5 worked transition reproduced exactly).
- **T-UI-NAV-002** — `Push`/`Replace` to an unregistered id ⇒ **throw** (F2); `Pop` below the root ⇒
  **throw**.
- **T-UI-NAV-003** — The shell has **no** UGUI dependency (compiles + runs in the pure test assembly with
  no Unity reference — the "testable without a Unity host" contract).

### T-UI-MATCHVIEW — Match view cadence (FR-UI-015/016)
- **T-UI-MATCHVIEW-001** — `MatchViewModelSource.Project()` reads `TryGetLatestFrame` and never advances
  the sim (a spy streamer records zero `RunTick`/`TickOnce` calls from the source).
- **T-UI-MATCHVIEW-002** — Rendered before any frame is published (`TryGetLatestFrame` false) ⇒ empty /
  last-known, **no throw**, **no** forced tick (F5).

### T-UI-FAIL — Fail-loud (FR-UI-008)
- **T-UI-FAIL-001** — A projection reading an agent index outside `[0, SQUAD_SIZE)` ⇒ **throw** (F1).
- **T-UI-FAIL-002** — A non-finite observed value entering a VM ⇒ **throw** (F1, NaN-gate).

## 5.2 FR traceability

| FR | Test(s) |
|---|---|
| FR-UI-001 no reverse ref | T-UI-LAYER-001 |
| FR-UI-002/007 immutable VM | T-UI-LAYER-002, F4 (compile-time) |
| FR-UI-003/005/012..014 dispatch discipline | T-UI-DISPATCH-001/002/003 |
| FR-UI-006/008 projection | T-UI-FAIL-001/002 |
| FR-UI-009..011 navigation | T-UI-NAV-001/002/003 |
| FR-UI-015/016 match-view cadence | T-UI-MATCHVIEW-001/002 |
| FR-UI-017 observer neutrality | T-UI-NEU-001 |
| FR-UI-022 no RNG/tag/persistent state | §4.4 (no allocation to assert) + T-UI-LAYER-001 |

## 5.3 Deliberately untested (out of scope)

- No UGUI rendering tests (Unity-host-gated, §4.3/§7.2).
- No screen-specific tests (Wave-7 screen specs, gated on their data specs, KD-2).
- No save round-trip (no persistent sim state, FR-UI-022).

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial test plan (T-UI-LAYER/NEU/DISPATCH/NAV/MATCHVIEW/FAIL) + FR traceability. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+2L; M-1 match-view reads streamer frame, M-2 cross-thread command marshaling FR-UI-023/F6, L-1 dispatcher split, L-2 Pop-below-root) → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
