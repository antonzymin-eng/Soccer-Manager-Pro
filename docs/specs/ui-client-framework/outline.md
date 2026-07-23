# UI / Client Framework Specification #38 (framework slice) — Outline

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 (2M+2L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED
**Source:** `docs/tracking/ui-client-framework-design.md` v0.2 (July 22, 2026), AR-1 (2M) → AR-2 converged

---

## Purpose

Defines the player-facing client's **framework** — the layer contract every screen obeys: a **view-model
contract** (immutable read-only projections of sim/loop/analytics state), a **navigation shell** (a pure
screen-registration + transition state machine), and a **command-dispatch discipline** (sim mutation
ONLY through existing public command seams). It pins the one concrete presentation surface with real
backing today — the **interactive match view** over the existing `LiveMatchStreamer` observation path.
It is top of the dependency graph: **no sim assembly may reference it** (the `match-viewer` lock).
Authored **framework-slice-only** (KD-2): the tactics + management **screens** are deferred to Wave-7
screen specs, each gated on its data spec — this slice is the **identity** they extend. This is a
**forward design** (the #21–#37 pre-code posture); the UGUI rendering binding is Unity-host-gated.

## Section map

| Section | Content |
|---|---|
| 1 | Introduction, scope (the framework slice; the screen/UGUI deferrals), dependencies, key decisions (KD-1..KD-5), boundary matrix (UI vs. sim vs. owning specs) |
| 2 | Functional requirements (FR-UI-001..023), data structures (`IViewModelSource<T>`/`ICommandDispatcher`/`ManagerIntent`/`NavigationShell`/`MatchViewModelSource`), failure modes F1–F6 |
| 3 | The projection model; the navigation state machine; command-dispatch routing; the match-view refresh cadence. Contracts + worked transitions |
| 4 | Architecture: the `TacticalDirector.UiFramework` assembly, the reference direction (generic substrate references nothing sim-side; concrete surfaces reference only built assemblies), the UGUI-binding deferral, no RNG/tag/ordinal |
| 5 | Test plan (no-reverse-reference asmdef lock / observer-neutrality digest-lock / dispatch-routes-only-through-public-seams / navigation state machine / fail-loud) + FR traceability |
| 6 | Performance: render-cadence observation (decoupled from the sim tick); zero persistent sim state |
| 7 | Forward extensions: the Wave-7 screen-spec roadmap (each gated on its data spec); the UGUI-host binding; the composition seam for #37/#48/#49 |
| 8 | References (the layer taxonomy #20 §3.5.2; the `match-viewer`/`LiveMatchServer` precedents) |
| 9 | Approval checklist + R-01..R-05 lead-developer gates |
| Appendices | Constant catalogue (`UiFrameworkConstants`); the observation-surface → view-model projection table; the command-intent → public-seam routing table; a worked navigation transition |

## Key decisions (detailed in §1)

- **KD-1** The layer contract: reads are immutable projections; sim mutation goes ONLY through existing
  public command seams (the UI adds none; the assembly boundary hides sim internals). Pure-observation
  surfaces hold no engine reference (the `LiveMatchServer` precedent); command surfaces hold the seam
  owner but can call only public seams.
- **KD-2** Framework now, screens deferred to Wave-7 (gated on their data specs); no screen VM for
  unbuilt data.
- **KD-3** The match view refreshes by reading the latest published immutable frame, decoupled from the
  sim tick (never calls `RunTick`); observer-neutrality is load-bearing.
- **KD-4** A missing command seam belongs to the owning spec, never the UI.
- **KD-5** Composition: a screen binds independent read-only inputs; the UI owns no domain logic.

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial outline authored from the converged design supplement. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+2L; M-1 match-view reads streamer frame, M-2 cross-thread command marshaling FR-UI-023/F6, L-1 dispatcher split, L-2 Pop-below-root) → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
