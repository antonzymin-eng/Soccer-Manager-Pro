# UI / Client Framework #38 — Section 1: Introduction, Scope, Dependencies, Key Decisions

**Created:** July 22, 2026
**Last Updated:** July 22, 2026 (v0.2 — section-file PASS-1 (2M+2L) → AR-2 convergence; APPROVED)
**Version:** 0.2
**Status:** APPROVED
**Source:** `docs/tracking/ui-client-framework-design.md` v0.2

---

## 1.1 Introduction

The UI / Client Framework (#38) is the player-facing client's foundation. It is the **presentation
layer**: it reads observation surfaces and view models, and it issues manager intent through the sim's
existing public command seams. It computes no game state. The rule that shapes the spec is settled in
§1.4 before any requirement: **the UI never owns domain logic and never mutates the sim except through an
existing public, sim-validated seam** — the `match-viewer` layer contract.

This spec is the **framework slice** (KD-2). It defines the contract every screen obeys; the screens
themselves are Wave-7 specs. It is authored as a **forward design** (nothing built yet — the #21–#37
posture), and its UGUI rendering binding is gated on Unity host access (the standing "full in-Unity
rendering blocked on Unity host access" OPEN ISSUE); this slice pins the **layer contract + the pure
testable substrate**, not the rendering.

## 1.2 Cadence and layer

The UI observes at **render cadence**, decoupled from the sim's 60 Hz physics / 10 Hz AI loops (KD-3). It
never advances the sim (`RunTick` is the streamer's, not the UI's). It is presentation infrastructure
(Code Standards #20 §3.5.2 layer taxonomy), **not** zero-allocation game-loop code.

## 1.3 Scope

**In scope (Wave 1 — the framework):**
- The **view-model contract** — the immutable read-only projection shape every screen binds (KD-1).
- The **navigation shell** — a pure screen-registration + transition state machine (testable without
  Unity).
- The **command-dispatch discipline** — mutation only through existing public sim/loop seams (KD-1/KD-4).
- The **interactive match view** — the one concrete surface with real backing, over `LiveMatchStreamer`
  (KD-3).

**Out of scope — deferred to a named later spec:**
- The **tactics screen** + all **management screens** (squad/transfer/training/scouting) — Wave-7 screen
  specs, each **gated on its data spec existing** (KD-2; §7.1). This slice authors **no** screen-specific
  view model for unbuilt data.
- Commentary/animation/audio depth (#48); localization/a11y (#49); the on-disk save/migration contract
  (#30/#50). The UI composes them; it owns none of their logic (KD-5).
- The **UGUI rendering binding** (prefabs/canvases/layout) — Unity-host-gated (§7.2).

## 1.4 The layer reality (verified against source)

- **Layer taxonomy** is authoritative in Code Standards #20 §3.5.2: the presentation layer reads sim;
  **sim never references presentation.** Verified: no asmdef references `TacticalDirector.MatchViewer`
  (the no-reverse-reference lock #38 inherits).
- **The command-dispatch discipline is already demonstrated:** `LiveMatchServer` exposes a playback-only
  `GET /control` (pause/resume/speed) and **never holds a `MatchEngine` reference — only a
  `LiveMatchStreamer`** — so "reachable by the presentation surface" and "can mutate the match" are
  disjoint by construction (src/CLAUDE.md v2.18).
- **The real command seams** (verified public in `MatchEngine.cs`): `SetTeamTactic`, `SetPlayerTactic`,
  `SubstitutePlayer`, `ConfigureSquads`. The loop's mutation seam (#30 `AdvanceAndPlayNextRound`) is the
  same class — owned by its spec, never re-implemented UI-side (KD-4).
- **The observation surface** (verified public): `BallView`, `AgentView(i)`, `AgentTeamId(i)`,
  `AgentIsGoalkeeper(i)`, `PossessingAgentId`, `HomeScore`, `AwayScore`, `MatchEnded`, `CurrentTick`;
  plus `LiveMatchStreamer.TryGetLatestFrame(out LiveMatchFrame)` + the `Start`/`Stop`/`Pause`/`Resume`/
  `SetSpeedMultiplier` playback surface (lock-protected latest-frame handoff).

## 1.5 Dependencies

| Direction | Spec / surface | Nature |
|---|---|---|
| Upstream (needs) | `MatchEngine` observation + command seams; `match-viewer` (`LiveMatchStreamer`) | read observation / dispatch to public seams |
| Upstream (composes, as they land) | #30 season loop (calendar/table/day-advance seam), #37 analytics view models, #21 tactic types; later #48/#49 | read-only inputs a screen binds (KD-5) |
| Downstream (consumers) | **none** — top of the dependency graph; no sim assembly may reference it (KD-1) |

## 1.6 Key decisions

**KD-1 — The layer contract (load-bearing).** Two invariants, both `match-viewer`-precedented:
- **No reverse reference:** no sim/loop/analytics assembly references the UI assembly (asmdef direction).
- **Reads are projections; writes go only through existing public seams.** A view model is an immutable
  read-only projection (value types — the `ReplayFrame`/#37 `MatchStatline` class; no engine reference
  escapes). Sim mutation routes ONLY through an existing **public** command seam (§1.4); the framework
  provides **no** mutation path, and — because the UI assembly sees only the sim's public surface
  (internals are invisible across the assembly boundary) — it *cannot* poke internals. **Two surface
  classes, not to be conflated:** a **pure-observation surface** (the match view, playback) holds **no**
  engine reference (the `LiveMatchServer` precedent — correct because playback must not mutate); a
  **command surface** (a tactics screen) legitimately references the seam owner to call `SetTeamTactic`
  — that is allowed; the guarantee is it can call **only** public, sim-validated seams. A "convenience"
  UI-side seam bypassing validation is forbidden (§9 anti-pattern).

**KD-2 — Framework now, screens deferred.** Author the framework identity first; screen specs (Wave 7)
extend it, each gated on its data spec. No screen VM is authored for unbuilt data (the phantom-consumer
discipline that kept #30 producer-only and #37 within the ledger).

**KD-3 — Refresh cadence: latest published frame, decoupled from the sim tick.** The match view refreshes
at render cadence by reading the **latest published immutable frame** (`LiveMatchStreamer.TryGetLatestFrame`
— the streamer advances the sim on its own thread; the UI never calls `RunTick`). Tearing/stale reads are
avoided because each `LiveMatchFrame` is a complete consistent snapshot handed off under lock. The UI has
no determinism obligation, but **observer-neutrality is load-bearing** (the `MatchViewerTests` digest-lock).
The **write** side is decoupled symmetrically (FR-UI-023): a live-match command is **marshaled onto the
streamer's tick thread** (enqueued, applied between ticks), never called cross-thread against the engine —
the read and write paths both cross the sim/render boundary only through the streamer's lock-owned thread.

**KD-4 — Missing command seams belong to the owning spec.** Management intent dispatches through the
loop/data spec's public seam. A screen needing a seam that does not exist **files it against the owning
spec** (e.g. #31 adds the transfer-action API), never adds it UI-side — which is why screen specs are
gated on their data specs (KD-2).

**KD-5 — Composition: bind independent inputs; own no domain logic.** A screen composes multiple
read-only inputs — a sim/loop VM + #37 analytics + (later) #48 assets + #49 strings — each produced by
its owning spec, each bound independently. The view-model contract is the composition seam; the UI
computes no game state, xG, or localized text itself.

## 1.7 Boundary matrix

| Concern | Owner | #38's relationship |
|---|---|---|
| Game state / analytics / localized text | sim / #37 / #49 | **reads** projections (computes none) |
| Sim mutation (tactics, subs, day-advance) | the owning sim/loop spec's public seam | **dispatches** to it (adds no seam) |
| The match observation surface | `MatchEngine` / `LiveMatchStreamer` | **reads** the latest immutable frame |
| Screen layouts / bindings for management data | Wave-7 screen specs | **out of scope** (gated on data specs) |
| UGUI rendering (canvases/prefabs) | Unity-host-gated impl | **deferred** (§7.2) |

#region VersionHistory
| Version | Date | Author | Notes |
|---|---|---|---|
| 0.1 | 2026-07-22 | — | Initial section from the converged supplement. Scope/deps/KD-1..5/boundary matrix, grounded in the verified layer taxonomy + `match-viewer`/`LiveMatchServer`/command seams. Status IN REVIEW. |
| 0.2 | 2026-07-22 | — | Section-file PASS-1 (0H+2M+2L; M-1 match-view reads streamer frame, M-2 cross-thread command marshaling FR-UI-023/F6, L-1 dispatcher split, L-2 Pop-below-root) → AR-2 convergence; APPROVED. See section-9 §9.3.1. |
#endregion
