# UI / Client Framework (#38, framework slice) — Design Supplement

> **Created:** July 22, 2026 · **Last Updated:** July 22, 2026 (v0.2 — AR-1 2M fix + AR-2 convergence)
> **Status:** DESIGN SUPPLEMENT (pre-promotion — no section files, no `SPEC_INDEX.md` row).
> Same governance class as `match-analytics-statistics-design.md` / `match-engine-design.md`.
> **Candidate spec:** #38 (**framework slice only**) · **Wave:** 1 (framework) / 7 (screens) · **Tier:** Stage 1 → 2
> **FR prefix:** `FR-UI`
> **Determinism:** read-only / presentation — **no RNG stream, no domain tag, no `SubsystemOrdinal`**
> (the `match-viewer` class).
> **Source plan:** `docs/tracking/spec-plans/spec-38-ui-client-framework-screens.md` v0.1

---

## 0. One-paragraph intent

The player-facing client's **framework** — the layer contract every screen obeys: a **view-model
contract** (immutable read-only projections of sim/loop/analytics state), a **navigation shell**, and a
**command-dispatch discipline** that mutates the sim ONLY through existing public command seams. It also
pins the one concrete presentation surface that already has real backing today — the **interactive match
view** over the existing `LiveMatchStreamer` observation path. It is top of the dependency graph: **no
sim assembly may reference it** (the `match-viewer` lock). The **screens themselves** (tactics,
management) are deferred to Wave-7 screen specs, each gated on its data spec — this slice is the
**identity** they extend, not a per-screen rewrite.

## 1. The scope decision, settled first (KD-2 — the split)

The plan's biggest structural fork (§9): authoring one monolithic UI spec would couple the framework to
screens and thrash every time a data spec (#28/#29/#31/#32) lands. **This supplement scopes the
FRAMEWORK slice only:**

**In scope (Wave 1, framework):**
- The **view-model contract** — what a view model *is* (an immutable read-only projection), how a screen
  binds one, how it stays one-directional (KD-1). This is the composition seam every screen + #37/#48/#49
  input plugs into (KD-5).
- The **navigation shell** — a screen-registration + navigation state machine (pure, testable without
  Unity): a screen = `{ view-model source, command dispatcher }`; navigation is deterministic screen
  transitions.
- The **command-dispatch discipline** — mutation routes ONLY through existing public sim/loop command
  seams; the presentation surface and the mutation surface are **structurally disjoint** (KD-1, the
  `LiveMatchServer` precedent).
- The **interactive match view** — the one concrete surface with real backing: binding the existing
  `LiveMatchStreamer` frame/playback surface into the client (KD-3).

**Out of scope — deferred, each to a named later spec:**
- The **tactics screen** and all **management screens** (squad/transfer/training/scouting) — Wave-7
  screen specs, each **gated on its data spec existing** (building a transfer screen before #31 lands is
  the phantom-consumer trap, §9). This slice authors **no screen-specific view model** for unbuilt data
  (KD-2).
- Commentary/animation/audio depth (#48); localization/a11y routing (#49); the on-disk save/migration
  contract (#30/#50). The UI owns none of their logic; it composes them (KD-5).
- **UGUI rendering binding** (prefabs, canvases, layout) — Unity-host-gated (the standing "full in-Unity
  rendering blocked on Unity host access" OPEN ISSUE). This slice pins the **layer contract + the pure
  testable substrate** (view-model projection, navigation state machine, dispatch discipline); the UGUI
  binding lands when a Unity host exists, exactly as `match-viewer`'s live surface did at the
  "at-minimum a live-updating viewer" floor.

## 2. Grounding (verified against source)

- **Layer taxonomy** — authoritative in Code Standards #20 §3.5.2 (src/CLAUDE.md §"Assembly Layer
  Taxonomy"): the presentation layer reads sim; **sim never references presentation**. Verified: no
  asmdef references `TacticalDirector.MatchViewer` (the no-reverse-reference lock).
- **The command-dispatch precedent is already built** — `LiveMatchServer` exposes a `GET /control`
  playback endpoint (pause/resume/speed) and **never holds a `MatchEngine` reference, only a
  `LiveMatchStreamer`**, so "can mutate the match" and "reachable by the presentation surface" are
  **disjoint by construction** (src/CLAUDE.md v2.18). #38's dispatch discipline generalizes exactly this.
- **The real command seams** the UI dispatches through (verified public in `MatchEngine.cs`):
  `SetTeamTactic`, `SetPlayerTactic`, `SubstitutePlayer`, `ConfigureSquads`. The loop's mutation seams
  (#30 `AdvanceAndPlayNextRound`) and future data-spec action APIs are the same class — owned by their
  spec, never re-implemented UI-side (KD-4).
- **The observation surface** the view models project from (verified public): `BallView`, `AgentView(i)`,
  `AgentTeamId(i)`, `AgentIsGoalkeeper(i)`, `PossessingAgentId`, `HomeScore`, `AwayScore`, `MatchEnded`,
  `CurrentTick`; plus `LiveMatchStreamer`'s `TryGetLatestFrame(out LiveMatchFrame)` + `Start`/`Stop`/
  `Pause`/`Resume`/`SetSpeedMultiplier` playback surface (lock-protected latest-frame handoff).

## 3. Key design decisions

### KD-1 — The layer contract (load-bearing): read observation + view models, mutate only via existing seams

The UI is the presentation layer. Two invariants, both `match-viewer`-precedented:
- **No reverse reference:** no sim/loop/analytics assembly may reference the UI assembly (enforced by the
  asmdef reference direction — the UI references *them*).
- **Reads are projections; writes go only through existing public seams.** A view model is a **read-only
  projection** (immutable value types, the `match-viewer` `ReplayFrame` / #37 `MatchStatline` class — no
  engine reference escapes). Any sim mutation routes ONLY through an existing **public** command seam
  (§2); the framework provides **no** mutation path of its own, and — because the UI assembly sees only
  the sim's public surface (internals are not visible across the assembly boundary) — it *cannot* poke
  sim internals even if a screen wanted to. A "convenience" UI-side seam that bypasses sim validation is
  the §9 anti-pattern and is forbidden.

  Two surface classes fall out of this, and they must not be conflated: a **pure-observation surface**
  (the match view, playback controls) holds **no** engine reference at all — the `LiveMatchServer`
  precedent (it holds a `LiveMatchStreamer`, never a `MatchEngine`, so "reachable by the presentation
  surface" and "can mutate the match" are disjoint by construction), which is correct precisely because
  playback has no business mutating the sim. A **command surface** (a tactics screen) legitimately *does*
  reference the seam owner so it can call `SetTeamTactic` — that is not a violation; the guarantee is
  that it can call **only** public, sim-validated seams, never a bespoke internal mutation. The
  disjoint-surface structure is the rule for pure-observation surfaces, not a claim that every screen is
  engine-free.

### KD-2 — Framework now, screens deferred (the split, §1)

Author the framework identity first; screen specs (Wave 7) extend it, each gated on its data spec. The
framework defines the contract **shape** + the one concrete match-view VM; it invents **no** screen VM
for unbuilt data (the phantom-consumer discipline that kept #30 producer-only and #37 within the ledger).

### KD-3 — Refresh cadence: poll the latest published frame, decoupled from the sim tick

The match view refreshes at **render cadence** by reading the **latest published immutable frame**
(`LiveMatchStreamer.TryGetLatestFrame` — the streamer advances the sim on its own thread; the UI never
calls `RunTick`). Tearing/stale-read is avoided because each `LiveMatchFrame` is a **complete consistent
snapshot** handed off under lock (the existing design), not a live buffer. The UI has no determinism
obligation, but **observer-neutrality is load-bearing** (reading must not change the match — the
`MatchViewerTests` digest-lock). Non-match screens refresh at the world-tick / on-change cadence (a
season screen re-projects after `AdvanceAndPlayNextRound`), never inside a match loop.

### KD-4 — Missing command seams belong to the owning spec, never the UI

Management intent (day-advance, transfer action, training focus, squad selection) dispatches through the
**loop/data spec's** public seam. If a screen needs a seam that does not exist, it is **filed against the
owning spec** (e.g. #31 adds the transfer-action API), never added UI-side — the UI adds no mutation
path (KD-1). This is why screen specs are gated on their data specs (KD-2): the seam must exist first.

### KD-5 — Composition: screens bind independent read-only inputs; the UI owns no domain logic

A screen composes multiple read-only inputs — a sim/loop view model + #37 analytics view models + (later)
#48 presentation assets + #49 localized strings — each produced by its owning spec, each bound
independently. The view-model contract (KD-1) is the composition seam; the UI computes **no** game state,
xG, or localized text itself (it renders what the owning specs hand it). This keeps every domain concern
in its owning spec and the UI a pure presenter.

## 4. Primary surfaces (proposed)

- New presentation-layer assembly **`TacticalDirector.UiFramework`** (`src/ui-framework/`). Its generic
  substrate (`IViewModelSource<T>`, `ICommandDispatcher`, `NavigationShell`) references **nothing
  sim-side** — it is parameterized over `T`. Its **concrete** surfaces reference only assemblies that
  **already exist**: `MatchViewModelSource` references `MatchEngine` + `MatchViewer` (both built). A
  reference to `MatchAnalytics` (#37) or the #30 season-save is added **only when that spec is built and
  a concrete VM projects it** — never speculatively (the FR-LW-031 phantom-dependency discipline; a
  screen against unbuilt data is Wave-7, KD-2). **Referenced by no sim assembly.** (The UGUI screen
  assemblies are separate, Wave-7.)
- **`IViewModelSource<T>`** — the read-only-projection contract: `T Project()` returning an immutable
  value type from observation surfaces (pure, no mutation).
- **`ICommandDispatcher`** — routes a typed manager-intent to an existing public sim/loop seam; carries
  **no** seam of its own (KD-1). A `MatchTacticsDispatcher` maps intent → `SetTeamTactic`/`SetPlayerTactic`.
- **`NavigationShell`** — the pure screen-registration + transition state machine (testable without Unity).
- **`MatchViewModelSource`** — the concrete match-view projection over `LiveMatchStreamer`/the observation
  surface (KD-3).
- **`UiFrameworkConstants`** — refresh-cadence `[GT]` (illustrative; a UI feel value, not a sim constant).

## 5. Reserved identifiers

- **Candidate #38** — matches the roadmap / `spec-plans/spec-38-…`. `SPEC_INDEX.md` row added at
  promotion (the #30/#37 precedent), scoped **"framework slice"** so the Wave-7 screen specs are clearly
  distinct rows/specs later.
- **FR prefix `FR-UI`** — verify unclaimed by grep over `docs/specs/**` before promotion.
- **No determinism identifiers** — #38 registers no domain tag / ordinal / RNG (KD, the presentation
  class); nothing to reserve in #16 §3.4, no `_RESERVED_` placeholder (the #37 posture).

## 6. Implementation plan (post-approval, forward design)

**Promotion pipeline:** author the 11-file section set at `IN REVIEW` (§1 scope/split/KD-1..5/boundary
matrix, §2 FR-UI-*/the view-model + dispatcher + navigation contracts/failure modes, §3 the projection
model + navigation state machine + dispatch routing + match-view cadence, §4 architecture/assembly/
reference-direction/UGUI-binding deferral, §5 test plan — the no-reverse-reference lock + observer-
neutrality + dispatch-routes-only-through-existing-seams + navigation state machine, §6 perf, §7 the
Wave-7 screen-spec roadmap + UGUI-host deferral, §8 refs, §9 checklist, appendices) → PASS-1 → AR-2 →
R-01..R-05 → APPROVED.

**T-phase code sequence (post-APPROVED):**
- **T0** — the pure substrate: `IViewModelSource<T>`, `ICommandDispatcher`, `NavigationShell` state
  machine, `UiFrameworkConstants` + unit locks (no Unity). Behaviour-neutral (sim untouched).
- **T1** — `MatchViewModelSource` over `LiveMatchStreamer` + the observer-neutrality digest-lock and the
  no-reverse-reference asmdef lock + a dispatch-routing test (mutation only via `SetTeamTactic` etc.).
- **T2 (Unity-host-gated)** — the UGUI rendering binding (canvases/prefabs) when a Unity host exists;
  the interactive match view promoted from the web viewer into the client.

**Deliberately NOT built:** any screen (Wave-7, gated on data specs, KD-2); any UI-side mutation seam
(KD-1); the UGUI rendering (Unity-host-gated); localization/analytics/presentation logic (owned by
#49/#37/#48, KD-5).

## Version History
| Version | Date | Change |
|---|---|---|
| v0.1 | July 22, 2026 | Initial supplement. KD-2 split settled: framework slice only (view-model contract + navigation shell + command-dispatch discipline + the concrete match-view surface), screens deferred to Wave-7 gated on their data specs. KD-1 layer contract, KD-3 latest-frame refresh decoupled from the sim tick, KD-4 missing seams belong to the owning spec, KD-5 composition seam. Grounded in the verified `match-viewer`/`LiveMatchStreamer`/command-seam surfaces; UGUI rendering deferred (Unity-host-gated). No determinism identifiers (the `match-viewer` class). |
| v0.2 | July 22, 2026 | **AR-1 (2M, fixed):** **M-1** — §4 listed the framework assembly referencing `MatchAnalytics` (#37) + the #30 season-save, both unbuilt forward designs — the FR-LW-031 phantom-dependency the spec itself preaches; rewrote §4 so the generic substrate references nothing sim-side (it is parameterized over `T`), concrete surfaces reference only built assemblies (`MatchEngine`/`MatchViewer`), and a #37/#30 reference is added only when that spec is built and a concrete VM projects it. **M-2** — KD-1's "structurally disjoint mutation surface" over-generalized the `LiveMatchServer` (pure-observation, engine-free) precedent to *all* screens, but a command screen legitimately references the seam owner to call `SetTeamTactic`; restated the invariant precisely (reads are immutable projections; writes go only through public sim-validated seams — the assembly boundary already hides internals) and split the two surface classes (pure-observation = engine-free; command = holds the seam owner but can call only public seams). **AR-2 (0H+0M; L-only ⇒ CONVERGENCE):** re-read end-to-end — the fixes align §4 with the phantom-dependency discipline and KD-1 with the command-vs-observation distinction; the #30 `AdvanceAndPlayNextRound` / #37 references are forward seams of APPROVED specs (consistent with the supplement's forward-reference posture). Cycle closes per the #21–#37 L-only-round convention. |
