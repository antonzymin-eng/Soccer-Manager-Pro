# Interactive Unity Client — High-Level Implementation Plan

> **Created:** 2026-07-23
> **Status:** DESIGN SUPPLEMENT — plan adversarial-review CONVERGED (AR-1 3H+2M+1L → AR-2 0H+0M+3L →
> AR-3 1H+2M+1L → AR-4 clean → AR-5 0H+2M+3L → AR-6 clean → AR-7 1H+2M+1L → AR-8 clean, see Version
> History); **P0 host-free foundations LANDED 2026-07-24** (the two assemblies, the CI-gate exclusion,
> the streamer seams, the speed-cap raise, `MatchClientConstants`, `MatchSession`, and the host-free
> deterministic command channel — `ManagerCommandQueue` / `MatchClientDriver` / tick-stamped log — with
> head-less tests; see §5-P0/§5-P2 and the Version History v0.7 row). Full dotnet gate not runnable in
> this environment (the network policy blocks the .NET SDK download — the same "runs in CI on push"
> constraint the tree records elsewhere); verified by exhaustive manual review + the `generate_projects.py`
> exclusion confirmed to keep `match-client-unity` out of the shim walk while `match-client-core` stays in.
>
> **UPDATE 2026-08-03 — P1 and P3 also LANDED (2026-07-27); this header and §12 had not been synced.**
> Both landed under the `path-to-playable-roadmap.md` Track-C rows (B1 and B4), which recorded them
> correctly while this supplement continued to describe them as deferred. **P1** — commit `d0e8573`:
> `MatchPeriod` (derived, KD-P1-2), `RestartCue` (KD-P1-5), `MatchEngine.CurrentPeriod` /
> `RestartAppliedThisTick`, `ApplyRestart` declaring its cue (KD-P1-4), and `LiveAgentCue` /
> `RestartBanner` in `match-viewer` (KD-P1-6), carried through `LiveMatchFrame` → `MatchFrameView`.
> Within-tick fields only — **no `SNAPSHOT_SCHEMA_VERSION` change**, per KD-P1-3. **P3** — commit
> `dfa506b`: `FrameInterpolator` (speed-aware alpha; snaps rather than smooths across a restart or
> substitution discontinuity) and `FollowBallCamera` (dead zone, frame-rate-independent smoothing
> proven by step subdivision, centring pitch clamp), 23 tests. **P3 landed two of three deliverables
> by design:** the live-stats accumulator was deliberately not built, because #37's
> `MatchAnalyticsAggregator` (roadmap B3) already is one and a second in `match-client-core` would be
> the parallel-surface trap. Recorded, not silently dropped.
>
> **All host-free phases (P0–P3) are therefore complete.** What remains is P4–P6 — the Unity render
> skin, the UGUI shell, and integration/cert — of which P6's head-less closed-loop scenario is
> gate-verifiable and P4/P5 are verifiable only on the pinned host. That host block cleared with the
> July 19, 2026 certification (`certification-platform.md` v1.4 ✅ PINNED).
>
> **UPDATE 2026-08-03, later same day — the HEAD-LESS HALF of P6 LANDED** (v0.10 row). Both §5-P6
> scenarios (`match-client-command-log-replay`, `match-client-save-restore-replay`) run on the #19
> `ScenarioRunner` in `src/match-client-core/tests/`, gate-checked on every push. Getting there needed
> three production additions the phase description assumes but P0–P3 never built: `MatchSession.TickOnce()`
> (the head-less advance, driving the real `LiveMatchStreamer.TickOnce()` seam via a new
> `InternalsVisibleTo`), `MatchSession.CaptureSave()` (the durable capture P0 deferred, riding the
> `ServiceOnce` seam, with §6.3's drained-empty-before-capture invariant now held by ordering), and
> `MatchSession.RestoreFrom()` (a session over a restored engine, re-applying no boot mutator) — plus
> `TickStampedCommandReplay`, the log-replay mechanism §6.1 defines reproducibility against.
> **What remains of P6 is the on-host half only:** scene boot, 60 FPS render, live tactical input
> through the UI, and the FR-PO-052-class render-loop perf capture — all of which need P4/P5 first.
>
> **UPDATE 2026-08-03 — OWNER DECISION: this plan is now the ONLY UI track** (v0.11 row). The roadmap's
> B6 fork is reversed to option (a): the product ships this Unity client, not the web-hosted viewer
> (`path-to-playable-roadmap.md` §7 supersede note). **P4 is unblocked and is the next step** — the whole
> substrate it binds is already gate-compiled and unchanged. Two standing rules follow, both in §12:
> **keep logic out of `MonoBehaviour`s** (the CI gate cannot see `match-client-unity` and never will;
> extending the Unity shim to fake `MonoBehaviour`/`GameObject` is explicitly refused, because a
> lifecycle-free stand-in lets a dead render loop report green — ERR-030-014 one layer up), and
> **budget a cert-host run per landing, not one at the end.** Note also that `PM-1`'s three
> screen-facing exit criteria are open again here: they were demonstrated on a surface that is no longer
> the product, while its determinism criterion is met head-lessly and stays met.
>
> **UPDATE 2026-08-03, later same day — P4 SPLIT into P4a / P4b, and P4a LANDED** (v0.12 row). The
> split makes rule 1 above a phase boundary rather than a discipline: **P4a is every render
> *decision*** — the corner-origin ⇄ centre-origin coordinate adapter, the IFAB marking catalogue as
> shapes, the roster's shirt numbering, and the per-agent / ball draw states — all in gate-compiled
> `match-client-core`; **P4b is the binding**, which is all that is left for the host. It surfaced
> **KD-P4a-1**: the streamer's boot-time roster cache held goalkeeper flags that
> `MatchEngine.SubstitutePlayer` rewrites, so a keeper substitution desynchronised it from the engine.
> The flag now rides `LiveAgentCue` per tick, which also fixes the browser viewer.
> No section files, no numbered spec.
> Same governance class as `interactive-match-view-design.md` and `match-engine-design.md`.
> **Governs (when implemented):** three new presentation assemblies — host-free `src/match-client-core/`
> (the deterministic session / command-channel / view-state logic), Unity-only
> `src/match-client-unity/` (the render/UGUI skin + scene/prefab/host scaffolding), and host-free
> `src/client-app/` (the client composition layer above `ui-framework` — the four screens' `ScreenId`
> catalogue + the `ClientScreenFlow` navigation graph; the §5-P5a layering question, resolved by owner
> decision 2026-08-07 as a new assembly, v0.17); a small exclusion
> in `tools/dotnet-ci/generate_projects.py` so the Unity-only assembly is not shim-compiled (§5-P0); a
> modification to `match-viewer/LiveMatchStreamer.cs` (adds one optional pre-tick hook + a `ServiceOnce()`
> seam for off-tick save servicing, §5-P0/§6.3); a raise of
> `match-viewer`'s `MaxLiveSpeedMultiplier` config cap (currently 8.0) to ≥ 10 so the 10× control is
> deliverable (§2 item 2); and read-only observation accessors on `MatchEngine`. The manager-command channel lives in `match-client-core` and
> drives only pre-existing public mutators — **no new mutator on `MatchEngine`.** Presentation tooling
> on top of the match-engine composition root, like `match-viewer` — it observes the engine and (via
> §6) drives it only through pre-existing stride-committed public APIs.
> **Supersedes nothing.** Extends the presentation floor established by `match-viewer` (HTML replay
> + live browser viewer). This is the "interactive Unity client (live rendering + input)" the
> presentation OPEN ISSUE names as the next surface above that floor.

---

## 1. Problem & current floor

The presentation layer today is **two browser-based surfaces**, both deliberately capped as
"floor" deliverables because there is no Unity host in the build/CI environment:

| Surface | What it is | Governed by |
|---|---|---|
| **HTML replay** | `MatchReplayRecorder` ticks a whole match, `HtmlReplayExporter` emits a self-contained HTML canvas replay (play/pause/scrub/speed) — watched **after** the match. | `match-engine-design.md` §5 Phase F |
| **Live browser viewer** | `LiveMatchStreamer` paces a real `MatchEngine` at wall-clock speed; `LiveMatchServer` (loopback `TcpListener`) streams the latest frame to a polling `<canvas>` page — watched **during** the match. Playback controls only (`/control` = pause/resume/speed); **never** a gameplay-mutation channel. | `interactive-match-view-design.md` |

Both stand on the same load-bearing foundation: the `MatchEngine` **observation surface**
(`BallView` / `AgentView(i)` / `AgentTeamId(i)` / `AgentIsGoalkeeper(i)` / `PossessingAgentId` /
`HomeScore` / `AwayScore` / `MatchEnded` / `CurrentTick` — all read-only value-type copies), whose
**observer-neutrality is digest-locked**: recording/streaming a run is byte-identical to an
unobserved same-seed run.

**What is missing** — and what this plan scopes — is the real target from the master plan (§3.4
Match View / §Month 1-2 2D Rendering, and vol-4 §7 Presentation Proxy): an **in-Unity interactive
client** that (a) renders the live match natively (pitch, 22 agents, ball, camera, HUD) at 60 FPS
inside the engine, and (b) accepts **manager input** — tactical changes and substitutions fed into
the running simulation (plus presentation-only speed/pause, which never touch the simulation, §6.4).
(a) is a richer View over the existing ViewModel; (b) is the genuinely new architectural surface,
because it is the first thing that mutates a live match from the outside, and it must do so
**without breaking determinism**.

## 2. Goal & definition of done

**Goal:** the master plan's Stage-1 Match View deliverable — "watch a simulated match" natively in
Unity, plus live tactical input — reachable from a minimal Main-Menu → Tactics → Match flow.

**Done when:**
1. A Unity scene boots a real `MatchEngine`, renders it live at 60 FPS (pitch + agents + ball +
   ball-height/possession/team cues + follow-ball camera), and shows a score/clock HUD.
2. Speed controls (Pause / 1× / 3× / 5× / 10×) work — the master plan's exact set. (Delivering 10×
   requires raising `match-viewer`'s `MaxLiveSpeedMultiplier` cap, currently 8.0, to ≥ 10 — the
   streamer clamps to that cap, so without the raise 10× is silently 8×; see §Governs / §5-P0.)
3. The manager can change team/player tactics and make substitutions **mid-match**; each change is
   applied at a deterministic tick and recorded in a **tick-stamped command log**, so a match is
   byte-identically reproducible **from that log + the `MatchSetup` (seed + initial squads/tactics/
   manager config) within an uninterrupted session** — *not* from human intent, since the wall-clock
   timing of a live click is not itself reproducible (§6.1).
   Across a save/restore the log is not carried (it is in-memory only, §11), so a resumed match
   reproduces from the restored snapshot + the post-restore log; the match remains save/restore-clean
   through the existing snapshot path either way.
4. A post-match report screen shows final score + basic stats.
5. The build compiles and its editor-mode logic tests pass under the `tools/dotnet-ci` shim; the
   scene/rendering half is verified on the pinned Unity host at a cert run.

**Explicitly still deferred** (see §11): heatmaps/xG/advanced stats overlays, career/season flow,
audio, art polish, networking/multiplayer, and any non-Unity (mobile/console) target.

## 3. Constraints & non-negotiables

These are inherited, not chosen — violating any of them is a defect, not a trade-off.

1. **Determinism is a hard requirement.** Rendering must stay **observer-neutral** (the existing
   digest lock extends to the Unity View). Input must be **capture-replayable**: every manager
   command that mutates the match must enter the snapshot/replay record so a saved match restores
   and re-ticks byte-identically (§6). No `System.Random` / `DateTime.Now` / frame-rate-dependent
   logic ever reaches game state.
2. **Presentation never mutates the engine except through vetted APIs, and the client respects each
   API's lifecycle class** (verified against `MatchEngine.cs`): **pre-kickoff / boot-only** —
   `ConfigureSquads` (throws once the match has ticked, `MatchEngine.cs:1301`), `ConfigureManager`
   (seeds the #26 manager profile / decision cadence), `EnableGkHeading` — are driven once from the
   P0 `MatchSetup`, **never live**; **live, stride-committed** — `SetTeamTactic` / `SetPlayerTactic`
   (stage-then-commit at the AI stride boundary, FR-TI-027) and `SubstitutePlayer` (guarded against
   `_matchEnded`, `MatchEngine.cs:1155`) — are the **only** mutators the live command channel drives.
   The client invents **no** new raw poke into engine state.
3. **Render/sim thread separation via the Presentation Proxy** (vol-4 §7): the sim owns the tick
   loop; the View reads a **lock-guarded latest-frame handoff** (the same thread-safe guarantee as
   the vol-4 double-buffer, implemented with a single lock around the frame swap); the two never
   block each other. `LiveMatchStreamer` is already exactly this ViewModel — the Unity View reuses
   it rather than adding a second engine driver.
4. **Unity host block.** No Unity host exists in this environment (root `CLAUDE.md` OPEN ISSUES).
   The plan is therefore split so that **all engine-facing logic is host-independent** (compiles +
   tests under the netstandard2.1 shim) and **only the thin `MonoBehaviour`/rendering skin needs a
   Unity host** — which now exists for cert runs (the July-19 recertification opened the pinned
   Windows 11 / Unity 6000.4.9f1 / DX11 host).
5. **Layer taxonomy.** The UI layer is Stage 1+ and unspecified (src/CLAUDE.md §3.5.2). This client
   is that layer's first member; it may reference the presentation/observation surface and the
   public mutation API, and **must not** reach into any Physics/Mechanics/AI internals.
6. **Pinned platform.** Unity 6000.4.9f1, DX11, Mono, x64 (`certification-platform.md` v1.4
   ✅ PINNED). UGUI for UI (master plan §3.4), not UI Toolkit, for Stage 1 simplicity.
7. **Command channel is not a dev toy.** Unlike the browser viewer's deliberately inert `/control`,
   this client *does* carry gameplay mutations. That channel must be in-process (Unity UI → driver →
   sim-thread engine), single-operator, and — if it is ever exposed over a socket — authenticated and separate from the
   playback stream. Conflating mutation with the bare loopback viewer server is forbidden (§6).

## 4. Architecture

MVVM, matching vol-4 §7. The key insight: **the Model and ViewModel already exist** — the Unity
client is a new **View** plus a new **input path back into the Model**.

```
 Model (sim thread)        ViewModel: LiveMatchStreamer          View (Unity) · driver (host-free core)
                           (shared, observe-only)
 ┌──────────────┐          ┌────────────────────────────┐        ┌──────────────────────────┐
 │ MatchEngine  │  RunTick  │ - paces ticks (wall clock)  │  frame │ MatchClientBehaviour      │
 │  - live      │◀────────▶│ - lock-guarded latest frame │──────▶│  (MonoBehaviour host)     │
 │    mutators  │  observe  │ - pause / resume / speed    │        │  render + camera + HUD    │
 │              │          │ - OPTIONAL pre-tick hook ───┼──┐     └───────────┬──────────────┘
 └──────┬───────┘          └────────────────────────────┘  │    input events │
        │ mutators invoked                                  │                 ▼
        │ by the hook, on the                               │     ┌──────────────────────────┐
        └─ sim thread, top of tick ◀───────────────────────┴─────│ MatchClientDriver         │
                                       hook calls the driver's    │  - ManagerCommandQueue    │
                                       drain callback             │  - drain callback         │
                                                                  │  - tick-stamped log       │
                                                                  └──────────────────────────┘
```

- **View is replaceable.** The browser viewer and the Unity client are two Views over one
  `LiveMatchStreamer`. Nothing engine-facing is Unity-specific; the Unity assembly is a skin.
- **The shared streamer stays observe-only.** `LiveMatchStreamer` contains **no** mutation logic. It
  gains one **optional** pre-tick hook (a delegate invoked on the sim thread at the top of each tick,
  ahead of the AI phase; the hook receives the engine's **mutation surface as a parameter**, so the
  driver retains no engine reference it could call off the sim thread). The **browser viewer supplies
  no hook**, so its streamer keeps the existing playback-only / disjoint-by-construction invariant
  (`interactive-match-view-design.md` §3/§9.1) — preserved by construction, not convention. The Unity
  client is the only caller that supplies one.
- **Frame path (read):** unchanged and already observer-neutral — the Unity View calls
  `TryGetLatestFrame` (extended in §5-P1 to carry the extra cues Unity can show that the browser
  floor omitted: sent-off / booking / substitution state).
- **Command path (write):** the one new engine-facing surface, owned by the **host-free-core**
  `MatchClientDriver` (in `match-client-core`, **not** the Unity assembly and **not** the shared
  streamer — so the command/determinism logic stays shim-testable, §5-P0). The driver's
  `ManagerCommandQueue` accepts typed **game** commands on the UI thread; the driver's drain callback
  (installed by `MatchSession` as the streamer's pre-tick hook, §5-P0) applies them on the sim thread at
  the tick boundary via the live mutators, and records each in the tick-stamped log. The same
  drain-and-service step is reachable off the tick loop through the streamer's `ServiceOnce()` seam
  (§5-P0/§6.3), so a save requested while the match is paused or finished is serviced on the sim thread
  without advancing a tick. Playback pause/speed are **not** commands — they stay on the streamer's own
  playback surface (§6.4).

## 5. Phased implementation plan

Each phase is independently shippable and independently testable. Phases P0–P3 are **host-free** and
land in `match-client-core` (plus P1's read-only accessors on `match-engine`; compile + logic-test
under the `tools/dotnet-ci` shim every push); P4–P6
are the **Unity-host skin** in `match-client-unity` (verified at a cert run, excluded from the shim
gate — §5-P0). This ordering front-loads everything the host block does *not* prevent.

### P0 — Foundations & scaffolding (host-free)
- **Two assemblies, split on the host-free/host line** (load-bearing — the split is what lets the
  determinism core stay CI-gated while the render skin needs a Unity host):
  - `src/match-client-core/` (`TacticalDirector.MatchClientCore`) — **host-free**; all
    determinism-bearing logic (P0–P3: `MatchSession`, `ManagerCommandQueue`, `MatchClientDriver`, the
    live-frame types, interpolation/camera/stats math). asmdef references `match-engine` +
    `match-viewer` + `deterministic-sim` (+ `tactical-instructions`/`player-database` for command
    payload types). Uses **no** `UnityEngine` rendering type, so the shim compiles and tests it every
    push. Tests asmdef alongside.
  - `src/match-client-unity/` (`TacticalDirector.MatchClientUnity`) — **Unity-only**; the render/UGUI
    skin (P4–P6: `MatchClientBehaviour` + rendering + screens). References `match-client-core`.
- **CI-gate deliverable (P0, not optional):** `tools/dotnet-ci/generate_projects.py` globs *every*
  `*.asmdef` under `src/` (`generate_projects.py:64`) and compiles each against a shim whose entire
  UnityEngine surface is Profiler / Vector2 / Vector3 / Debug / Mathf — **no** `MonoBehaviour` /
  `GameObject` / `Camera` / `SpriteRenderer` — with no per-assembly exclusion. So the Unity-only
  assembly must be **excluded from the generator's walk** (add a skip marker / naming convention it
  honours; a few lines in `generate_projects.py`), leaving the shim gate compiling+testing
  `match-client-core` (and the rest of the tree) unchanged. Without this, `MatchClientBehaviour` — the
  project's first `MonoBehaviour` — turns the whole-tree compile red the moment P4 lands.
- `MatchClientConstants.cs` (in core) — the master plan's speed set (`{Pause, 1, 3, 5, 10}`), camera
  tuning, render-cue sizes ([GT], migrated onto `GameplayConfig` per the June-30 catalogue convention).
- **Speed-cap raise (small `match-viewer` change):** the streamer clamps `SetSpeedMultiplier` to
  `MaxLiveSpeedMultiplier` (config default 8.0, `MatchViewerConstants.cs:90`), so the master-plan 10×
  control (§2) is silently 8× until the cap is raised to ≥ 10. Raise the config default; listed in
  §Governs as a change to the reused `match-viewer` assembly.
- **`LiveMatchStreamer` seams (small `match-viewer` change):** two additions to the reused streamer —
  (i) the **optional pre-tick hook** the driver's drain installs (§4), and (ii) a **`ServiceOnce()`**
  method that performs one sim-thread drain-and-service pass (drain the command queue + honour a pending
  save-capture request) **without advancing a tick**, so a save requested while paused or at full time
  is serviced on the sim thread rather than hanging on a tick boundary that never arrives (§6.3). Both
  reuse the streamer's existing single-writer lock; the browser viewer installs no hook and never calls
  `ServiceOnce()`, so its playback-only / disjoint-by-construction invariant is unchanged.
- A `MatchSession` façade (in core): constructs/configures the **whole live-match composition** from a
  `MatchSetup` value (home/away squads, tactics, manager config, seed) — the `MatchEngine`, the
  `LiveMatchStreamer`, **and the `MatchClientDriver`** — installing the driver's drain as the streamer's
  pre-tick hook, wiring the driver's save-request path through the streamer's `ServiceOnce()` seam (§6.3),
  and exposing the driver's `ManagerCommandQueue` (enqueue-only) to the View. It is the
  single place that owns match lifecycle **and the command-channel wiring**, so the Unity host and any
  head-less test drive the identical composition, input path included. This is the "click Play Match"
  seam the browser design left to Stage-1 UI.

### P1 — Richer observation frame (host-free) — ✅ **LANDED 2026-07-27** (commit `d0e8573`, roadmap B1)
- Extend the live frame with the cues a native View can show that the browser floor skipped:
  per-agent booking/sent-off state, active-substitution markers, current restart/phase. Requires a
  small read-only extension to the `MatchEngine` observation surface (same pattern as v1.24/v1.32 —
  read-only value copies, **no** `SNAPSHOT_SCHEMA_VERSION` change, observer-neutrality re-locked).
- Do this before rendering so the render layer targets the final frame shape once.

#### P1 key decisions

**KD-P1-1 — the engine exposes scalars; the frame does the aggregating.** Every new engine accessor is
a read-only value copy in the established `AgentTeamId(i)` / `AgentIsGoalkeeper(i)` shape. No aggregate
presentation type is declared in `match-engine`. This keeps the safety-critical assembly's public
surface uniform and stops a View's preferred data shape from becoming an engine contract.

**KD-P1-2 — match period is DERIVED, never stored.** `MatchEngine.CurrentPeriod` is a pure function of
the two transition flags `CheckMatchFlowTransitions` already owns — `_matchEnded` and
`_secondHalfStarted` — both of which the payload already serializes. So there is no new state, nothing
added to the snapshot, and no schema change. It lives on the **engine**, not the streamer, because
computing the half-time rule inside a presentation assembly would put a second copy of it in the tree:
the parallel-surface trap (the PM AR-7 M-1 / `POSITION_COUNT` class).
- It reports which transitions have **fired** rather than re-deriving them from `CurrentTick` against
  `HALF_TIME_BOUNDARY_TICK`. That is the stronger form: the boundary constant then has exactly one
  reader in the whole tree, the reported period can never disagree with what the engine actually did,
  and because both flags round-trip through the payload it is correct after a restore for free.
- The enum is `MatchPeriod`, deliberately **not** `MatchPhase` — `TacticalDirector.DecisionTree.MatchPhase`
  already exists with an unrelated meaning (`OPEN_PLAY`…), and both types would be in scope together in
  `match-engine`. This project has hit CS0104 on exactly this collision three times (`TacticTranslation`,
  `PlayerAttributes`, and the §5.Z.9 foul-probability helper).
- Three members only — `FirstHalf` / `SecondHalf` / `FullTime`. The Stage-0 halves model has **no break**:
  `CheckMatchFlowTransitions` resets the ball at the boundary and play continues (FR-TP-019). A
  `HalfTime` member would describe a state the engine cannot be in, and a View would render a pause that
  never happens.

**KD-P1-3 — the restart cue is a WITHIN-TICK engine flag that the streamer latches.** This is the
load-bearing decision. The engine records the restart applied *during the current tick* and clears it at
the top of the next, which is exactly the lifecycle of `_aiPhaseRanThisTick` and the §5.Z.9 foul-candidate
triple. Consequences, all of them the point:
- It is **not cross-tick state**, so the `SerializeWorldState` exclusion proof needs no new class, and
  its current claim — *"no cross-tick gameplay state is excluded"* — stays true as written.
- **No `SNAPSHOT_SCHEMA_VERSION` change**, so no digest rebaseline.
- The cross-tick memory a HUD actually needs ("hold the banner for ~2 s after the whistle") lives in
  `LiveMatchStreamer`, a presentation class with no determinism obligations at all.
- Restore falls out correctly **by construction**: the engine has nothing to restore, and a fresh
  streamer over a restored engine legitimately reports "no restart observed yet".

  The rejected alternative was latching on the engine (`_lastRestartKind`/`_lastRestartTeam`/`_lastRestartTick`).
  It would have introduced the engine's first non-serialized *cross-tick* field, forcing a new exclusion
  class into that proof — for three values no gameplay path ever reads. Serializing them instead was
  rejected for the mirror reason: a schema bump that moves every digest baseline, to persist a HUD banner.

**KD-P1-4 — every restart declares its kind.** `ApplyRestart(position, awardedTeam)` gains a cue
parameter, so each of the six restart sites states *what kind* of restart it is, exactly as §5.Z Phase H
KD-H1 made each state *which team*. Without it a restart reaching the ball-placement primitive
untyped would be reported to the View as whatever the previous restart was — the failure mode KD-H1
already fixed once for the awarded team.

**KD-P1-5 — a separate cue enum, not `RestartType`.** `RestartType` (Ball Physics #1) classifies
**boundary exits** — `None`/`ThrowIn`/`GoalKick`/`Corner`/`KickOff` — and its ordinals carry an explicit
STABILITY paragraph because they are embedded in the `RestartAwardedEvent` (0x19) payload the
digest-load-bearing ledger serializes. Fouls and offside produce **free kicks**, which are not boundary
exits and have no member there. Adding one would widen a digest-bearing domain owned by another spec to
serve a presentation need. So `match-engine` declares its own `RestartCue`
(`None`/`KickOff`/`ThrowIn`/`GoalKick`/`Corner`/`FreeKick`) and maps the physics enum into it at the one
site that has one.

**KD-P1-6 — per-agent cues travel as one array of one struct.** `LiveAgentCue` (yellow cards, sent-off,
bench slot) rides beside the existing positions array rather than adding three parallel arrays to the
frame signature. Future per-agent cues extend the struct instead of re-widening the frame — which is what
"target the final frame shape once" has to mean in practice. `LiveAgentCue` is a **match-viewer** type,
per KD-P1-1.

### P2 — Deterministic manager-command channel (host-free) — **the core new work**
- `ManagerCommandQueue` + a **closed set of typed game commands** — `SetTeamTactic`,
  `SetPlayerTactic`, `Substitute` — each mapping onto exactly one **live, stride-safe** engine
  mutator. Playback pause/speed are **not** in this queue (presentation-only; they stay on the
  streamer's existing playback surface, §6.4). Pre-kickoff/boot mutators (`ConfigureSquads`,
  `ConfigureManager`, `EnableGkHeading`) are **not** here either — they belong to P0 `MatchSetup`
  (§3-2). No `SetManagerMode` live command: flipping Human⇄AI mid-match has no verified engine path.
- A **`MatchClientDriver`** (in host-free `match-client-core`, **not** the Unity assembly — §5-P0)
  owns the queue and a drain callback. The callback is installed as `LiveMatchStreamer`'s **optional
  pre-tick hook**, invoked on the sim thread at the top of each tick ahead of the AI phase; the shared
  streamer itself contains **no** mutation logic and the browser viewer supplies no hook, so its
  playback-only invariant is preserved by construction (§4). See §6 for the determinism design.
- Each drained command is written to a **tick-stamped command log** keyed by its applied tick — the
  mechanism that makes a live match replay-reproducible (§6.3). **This log is a P2 deliverable, not a
  deferral**; only the broader on-disk replay/rewind *feature* that consumes it stays deferred (§11).
- Highest determinism risk, heaviest test + adversarial focus, and **fully exercisable head-less**: a
  test enqueues commands and drives `TickOnce()` directly, proving command apply-tick and drain
  ordering without a Unity host. (The live wall-clock path is reproducible only *via* the log, not
  from human intent — §6.1.)

### P3 — Client-side view state & stats (host-free) — ✅ **LANDED 2026-07-27** (commit `dfa506b`, roadmap B4; two of three by design — see the header UPDATE)
- Frame interpolation math (render at 60 FPS between 10 Hz AI strides / 60 Hz physics — pure
  functions, unit-tested without Unity), follow-ball camera target math, and a minimal live-stats
  accumulator (possession %, shots, score) fed off the observation surface. All pure/testable.

### P4 — Unity render skin

**Split into P4a / P4b on August 3, 2026, as the direct consequence of the §12 status-change rule
"keep logic out of `MonoBehaviour`s".** The rule says every *decision* the render skin makes belongs
in a gate-compiled assembly; the split makes that a phase boundary rather than a discipline. P4a is
every render decision, host-free and gate-tested; P4b is the binding, which cannot be anything else.
Sequencing P4a first means P4b arrives with nothing left to decide — the same argument that put P6's
head-less scenario ahead of P4, applied one level down.

#### P4a — Render model (host-free) — ✅ **LANDED 2026-08-03**
- `PitchViewProjection` — the one documented adapter §7 "Coordinate mapping" requires, mapping the
  engine's **corner-origin** metres (Ball Physics #1 §1.2) to a **centre-origin** view plane at
  1 unit per metre, plus the inverse a pointer click needs and an on-pitch predicate. Centring is
  what makes a home-end position and its away-end mirror differ only in sign, which is what makes
  the mirrored assertions cheap to write.
- `PitchMarkings` / `PitchMarking` / `PitchMarkingKind` — the 12-marking IFAB catalogue as shapes,
  built from the **existing** `MatchViewerConstants` `[FIXED]` values (§7 "one source of truth for
  markings across both Views"), both ends emitted from one loop over a sign so a marking cannot be
  right at one end and wrong at the other. The centre-circle D-arc and the corner arcs are
  deliberately absent: neither has a `[FIXED]` constant and the browser viewer draws neither, so
  adding them would mean inventing geometry here and diverging the two Views.
- `MatchRoster` — the match-**constant** per-slot data (team id, shirt number). The shirt-numbering
  rule lives in `match-viewer`'s `RosterShirtNumbers`, which BOTH Views consume — the P4a landing had
  reimplemented it in C# while leaving the browser viewer's inline `computeJersey` in place, and the
  AR pass collapsed the two (see the AR row below).
- `AgentRenderModel` / `BallRenderModel` / `MatchRenderProjection` — the resolved per-agent and ball
  draw states: **world position** (from the P3 interpolator's buffer, because that is what is
  actually being drawn) with every discrete cue (from the newest captured frame, because cues do not
  interpolate), the possession ring, and the ball's ground shadow. Colour-free by design: a palette
  has no correct answer a test could assert, and `UnityEngine.Color` is not in the shim's surface, so
  the renderer maps `TeamId` and everything upstream of that is here.
- `PitchCameraRig` / `PitchCameraPose` — where the camera goes: height, tilt **from vertical**, and
  the lateral offset that makes the view slightly oblique. A placement is a decision, so it is here
  rather than in the `MonoBehaviour` the gate cannot compile; the pose is two world points rather
  than a rotation, because `Quaternion` is not in the shim and widening it to buy coverage is the
  bargain §12 already refuses.
- Render-cue `[GT]` sizes land with their consumers, as `MatchClientConstants` v1.0 said they would.

#### P4a key decision — the view is tilted, not flat

**KD-P4a-2 — a tilted perspective camera, and no faked height cues (owner call, August 4, 2026).**
P4a first shipped a *flat* top-down view: a 2D plane at 1 unit per metre, with ball height suggested
by lifting the sprite and growing it on a capped ramp. The owner reversed that to an FM-style view —
**from above, tilted back from vertical, slightly off centre** — on the grounds that the ball only
needs to be visible on and above the pitch, not scaled.

The revision **removes more than it adds**, which is why it was taken before P4b rather than after.
With a tilted camera, height is a genuine world axis: the ball is placed at `(x, height, z)` and the
projection conveys altitude by itself. So `BallHeightViewOffsetPerMetre`, `BallHeightScalePerMetre`
and `BallMaxHeightScale` are **deleted**, along with `BallRenderModel.SpritePosition` /
`SpriteRadius` and `MatchRenderProjection.HeightScale` — and with them the AR pass's M-5 finding and
its recorded 10 m saturation limitation, which stop existing rather than needing a retune.

Three things survive the change, each for a stated reason. The **shadow** stays: under any tilt a
lofted ball's screen position separates from the pitch point it is over, and that point is what every
gameplay judgement was made against — it is the one cue perspective cannot supply. `PitchViewProjection`'s
**corner→centre re-origin** stays: it is the ground plane, unchanged. And `FollowBallCamera` stays:
it decides *where* the camera looks, which the tilt does not affect.

**The one real cost is the click inverse.** `ToPitch` was a two-line subtraction; under a tilted
perspective camera, screen position is no longer affine in pitch position, so `TryGroundHit` does a
ray/ground-plane intersection instead. `Camera` is not in the shim and never will be, so the Unity
side supplies the ray (`Camera.ScreenPointToRay`) and the math stays gate-tested on this side.

**Two consequences recorded rather than left implicit.** The engine's Y axis (across the pitch)
becomes the world's **Z**, and its Z (up) becomes the world's **Y** — an axis swap, which is the same
class of trap as the corner-origin one and is locked by its own test. And `FollowBallCamera`'s pitch
clamp describes an axis-aligned rectangle of visible ground, which is exact for a straight-down view
and **approximate** for a tilted one (the real footprint is a trapezoid). The clamp's job is keeping
the target near the pitch, not exact framing, so the approximation is kept deliberately.

> **Amended 2026-08-04 (AR pass 2, AR-P4a2-H1).** The pose is **three** values, not the two this
> decision first specified: `Position`, `LookAt` and `FieldOfViewDegrees`. Height and tilt place the
> camera, but nothing said how much of the pitch it *sees* — so P4b would have picked a field of view
> inside the `MonoBehaviour`, which is a framing decision in the one place the gate cannot compile.
> That is the leak §12 rule 1 and the whole P4a/P4b split exist to close, and it was open in the
> deliverable meant to close it. `CameraVerticalFovDegrees` is a `[GT]` bounded against the tilt
> (`tilt + fov/2 < 90`, or the camera's lowest ray never meets the ground and the visible area is
> unbounded), and `PitchCameraRig.GroundExtentAlongTilt` reports what it buys in metres of visible
> pitch — deliberately asymmetric, because the trapezoid reaches further beyond the aim point than in
> front of it. Aspect ratio stays with Unity: it is a property of the window, not a design choice.

#### P4a key decision

**KD-P4a-1 — the goalkeeper flag is per-frame, not roster metadata.** `LiveMatchStreamer` cached team
ids *and* goalkeeper flags at construction under the comment "roster metadata never changes across a
match". That is true of team ids — a bench player belongs to the team whose bench they sit on — and
**false of goalkeeper flags**: `MatchEngine.SubstitutePlayer` copies the bench player's flag into the
on-pitch slot, so substituting a keeper for an outfield player (or the reverse) moves which slot is
the goalkeeper and the cache silently disagrees with the engine from that tick on. So
`LiveAgentCue` gains `IsGoalkeeper` — the first cue added through the extension mechanism KD-P1-6
created the struct for — sampled every tick beside the cards and bench slot it rides with, and
`MatchRoster` deliberately holds **no** goalkeeper flag so the stale copy cannot be reintroduced. The
streamer's `IsGoalkeeper(int)` accessor is kept and re-documented as boot-time only (a caller needs
roster metadata before the first frame exists), and `LiveMatchServer` now reads the frame cue when
there is a frame — which fixes the same defect in the browser harness, with no JSON or script change.
The alternative, re-reading the engine from the accessor, is exactly the off-sim-thread tear-read the
streamer's single-writer invariant exists to prevent.

#### P4a adversarial-review pass — ✅ **2026-08-04, 1H + 5M + 3L fixed**

Recorded here because one finding changes the **P4a → P4b contract**, not just the code.

**AR-P4a-H1 — a rectangle's corners are normalised, and P4b may rely on it.** `PitchMarkings` builds
each end box from its goal line *inwards*, so the away penalty area and away goal area were emitted
with **descending X** while the home pair ascended. `PitchMarking.Rectangle` now normalises to
`A = min`, `B = max`, so `B − A` is a usable extent for every rectangle. Without that, the binding
would have had to decide whether to normalise — and a binding that took the corners as given draws
exactly two of the five rectangles inverted, at one end only. That is #8 ERR-008-002's home/away
asymmetry class, landing in a `MonoBehaviour` the gate can never compile, inside the type whose whole
purpose is to leave the skin nothing to decide. Lines and goal mouths are deliberately **not**
normalised (a line has direction; a goal mouth is post-to-post). The fixture had been hiding it:
`AssertAreaBox` normalised with `Mathf.Min`/`Max` before asserting, so any corner order passed.

The other four Mediums did not move the phase boundary: the render path gained the non-finite gate its
sibling `MatchFrameView` already had (nothing upstream refuses one — `FrameInterpolator` propagates it
by design); `HasBall` became the stored fact with the ring radius derived from it, so a `[GT]` size can
no longer answer a question about the simulation; `MatchClientConstants` validates at boot instead of
silently repairing a bad cap; two fabricated rationale figures were replaced with checked ones; and the
shirt-numbering rule was collapsed to one implementation shared with the browser viewer.

Re-reviewing the fixes surfaced two more, closed in the same pass: the new non-finite gate ran inside
the write loop, which would have left the destination buffer half this frame and half the last behind
a thrown exception — it now validates in its own pass, keeping `ProjectAgents` all-or-nothing — and
M-4's boot validators were themselves unreachable from any test, so `MatchClientConstantsTests` drives
them directly. Replacing an untestable repair branch with an untestable guard would have moved the
problem rather than fixed it.

#### P4b — Unity binding (host, cert-verified)
- `MatchClientBehaviour : MonoBehaviour` (in `match-client-unity`) — the PlayerLoop host the project
  currently lacks (src/CLAUDE.md "WHAT IS NOT HERE YET"): owns `MatchSession`, reads
  `TryGetLatestFrame` each `Update`, and renders. With P4a landed its whole job is binding —
  instantiate ONE PRIMITIVE PER ENTRY of `PitchMarkings.BuildDrawables()` (**not** `Build()` — the
  P4b AR-1 H-1 finding moved the rectangle-into-four-lines decomposition into `match-client-core`
  itself, so the binding's switch has no `Rectangle` case and synthesises no corner of its own; see
  `PitchMarkings.BuildDrawables`'s own doc) and place each on the ground plane via
  `PitchViewProjection.ToWorld`, at one of four ordered `[GT]` ground-layer heights rather than a
  flat zero (round-2 M12 — coplanar opaque ground layers z-fight in a real renderer: markings lowest,
  then the ball's shadow, then the possession ring, then the agent marker); feed `FollowBallCamera`'s
  target to `PitchCameraRig.ComputePose` and assign the three fields it returns — `Position`,
  `LookAt` (via `transform.LookAt`) and `FieldOfViewDegrees` (`Camera.fieldOfView`); turn a click
  into a ray for `PitchViewProjection.TryGroundHit`; assign `transform.position`/`localScale` from
  the `AgentRenderModel`s and the `BallRenderModel` (unit-radius FLAT ground props scaled on X/Z
  only, the ball the one unit-radius VOLUMETRIC prop scaled uniformly — round-2 M11's prefab-contract
  clause 2); map `TeamId` to a palette. It should contain no branch a test would want to reach.
- **Perspective camera, tilted** (KD-P4a-2) — not the orthographic one an earlier draft assumed.
  Markers and markings stay flat primitives on the ground plane; this is a 2.5D presentation, not a
  3D one, and no art pipeline is implied. Shirt-number labels will need billboarding once markers
  foreshorten.
- **The camera is fully specified by the pose — pick nothing here.** Position, aim and field of view
  all come from `PitchCameraRig`, deliberately: how much pitch is in shot is a framing decision, and
  a framing decision chosen in a `MonoBehaviour` is a decision the gate cannot compile (§12 rule 1).
  `PitchCameraRig.GroundExtentAlongTilt` reports what the configured field of view actually sees, in
  metres of pitch, if a landing needs to argue about the framing. Aspect ratio is the one framing
  input the core does not own, because it is a runtime property of the window rather than a design
  choice.

### P5 — Unity UGUI shell

**Split into P5a / P5b on August 7, 2026, for the reason that split P4.** §12 rule 1 says every
decision the shell makes belongs in a gate-compiled assembly; P4a turned that rule into a phase and
P4b arrived with nothing left to decide. The same argument applies here and had not been applied:
"the UGUI shell" as one host-only phase would have put *when a control is available* and *what the
speed buttons offer* inside `MonoBehaviour`s the gate cannot compile — which is exactly the leak
AR-P4a2-H1 caught in the deliverable built to close that leak.

#### P5a — shell decisions, host-free (gate-verified)

**LANDED August 7, 2026.** Two decisions extracted out of the future binding:

- **`PlaybackSpeedLadder`** (`match-client-core`) — the four `[GT]` multipliers as an *ordered*
  ladder, plus the opening rung and the end behaviour. The catalogue held four independent dials; it
  did not say they form a ladder, which one a match opens at, or what "faster" does at 10×.
  Stepping **clamps rather than wraps**: a faster-click at the top that dropped the viewer to 1×
  reads as a fault, not a limit. Pause is deliberately not a rung — it is a streamer state, and no
  multiplier means "stopped" (0× is outside the streamer's legal range anyway).
- **`MatchControlAvailability`** + **`MatchControlLockReason`** (`match-client-core`) — resolves
  §5-P5's standing requirement that "the UI gates tactical input at full time so a click does not
  silently no-op" into a value the binding reads. Three states (`AwaitingFirstFrame`, `Live`,
  `FullTime`), each carrying *why* it is locked so the shell can explain a disabled control.
  Two decisions inside it are worth naming because both are the kind a later tidy-up would reverse:
  **saving stays enabled at full time** (§6.3 — a finished match is exactly when a viewer wants to
  save, and `ServiceOnce()` exists so the capture needs no tick; locking it alongside the tactical
  controls would make a completed match unsaveable), and **a frameless streamer does not resolve to
  `Live`** — `TryGetLatestFrame`'s out-parameter on a false return is `default(LiveMatchFrame)`,
  whose `MatchEnded` is *false*, so a `From` that read the frame unconditionally would report a match
  that has not started as fully interactive.

**One finding, and it is the §5-P0 cap note turned from prose into an assertion.** That note said
`MatchViewerConstants.MaxLiveSpeedMultiplier` must be ≥ 10 so 10× is not refused — and nothing
enforced it. `SetSpeedMultiplier` fail-louds on an out-of-range multiplier, so a cap configured below
a step would have surfaced as *one speed button throwing mid-match while the other three worked*.
`MatchClientConstants.RequireStreamerAcceptsSpeed` now pairs each speed against the streamer's own
`[Min, Max]` at load, in the shape of the existing `RequireFarRayMeetsGround` cross-dial check, so
the process refuses to start instead. Tests express the bounds relative to the cap rather than as
literals, so a retune keeps them meaningful.

**Deliberately NOT built here, and why (a layering decision the owner should make).** The four
screens' **`ScreenId` catalogue and navigation graph** — Main Menu → Tactics Setup → Match View →
Post-Match Report — has no correct home today. `ScreenId` lives in `ui-framework`, but FR-UI-010 is
explicit that the framework hard-codes no screen, so a client's screen catalogue does not belong
there; and `ui-framework` sits *above* `match-client-core`, so the catalogue cannot live in the core
either. The remaining homes are `match-client-unity` (gate-invisible — wrong by rule 1) or a new
assembly above `ui-framework`. That is the same question §6 item 2 of the roadmap already flags for
C3's management screens, and it is not one to settle inside an implementation pass.

**RESOLVED 2026-08-07 — owner decision: a new gate-compiled assembly above `ui-framework`,**
`src/client-app/` (`TacticalDirector.ClientApp`), landed the same day (v0.17). The `match-engine`
precedent carried the argument: a composition root that wires generic infrastructure into a concrete
product lives *above* what it wires, and FR-UI-010 makes the concrete screen set composition, not
framework. The assembly references only `ui-framework` and holds three types: `ClientAppConstants`
(the four `[FIXED]` screen ids, 1–4 — 0 deliberately never allocated, the `ManagerCommandKind.None`
zero-value-safety convention), `ClientScreens` (the ids as typed values), and `ClientScreenFlow`
(the five-edge navigation graph as guarded moves over a **privately-owned** `NavigationShell`, so
the graph is enforced by encapsulation — an edge not encoded there does not exist, and the P5b
binding forwards button clicks and decides nothing). Two edges are `Replace`, both test-locked via
where a later Pop lands: TacticsSetup → MatchView (a running match must not sit above a stale setup
screen a "back" could return to) and MatchView → PostMatchReport (the match is frozen at full time,
§6.2 — a Pop from the report lands on Main Menu, never a dead match view). Deliberately absent: an
abandon-match edge out of MatchView — §5-P5b specifies no quit control, and an edge without a
specified consumer is the phantom FR-CS-049 refuses. C3's management screens (roadmap §6 item 2)
now have their answer by precedent: they register into this same assembly when P7 opens them.

#### P5b — Unity UGUI binding (host, cert-verified)
- The master plan's four screens: **Main Menu** (New Demo Match), **Tactics Setup** (formation /
  team / player instructions — writing a `MatchSetup`), **Match View** (canvas + collapsible stats
  panel + the speed controls + in-match tactical-adjustment buttons wired to the P2 command queue),
  **Post-Match Report** (score + stats).
- The in-match tactical buttons are the first UI producers of P2 commands — the input half of "done".
- **Read enablement from `MatchControlAvailability`, and the speed buttons from
  `PlaybackSpeedLadder`** — do not re-derive either in a `MonoBehaviour`. In particular, do not
  delete a sim-side `_matchEnded` guard on the grounds that the UI now checks it: per §6.2 the UI
  gate is a best-effort early-out that trails the sim by ≥ 1 frame, and the sim side is the
  authority. Inverting those two leaves the best-effort half holding the invariant.
- **Budget a cert-host run for this landing** (§7 / C2).

### P6 — Integration, cert & closed-loop scenario
- A `#19 ScenarioRunner` cross-spec scenario: boot via `MatchSession`, inject a scripted
  tick-stamped command sequence through the queue, assert (a) two runs with the same `MatchSetup` + same
  tick-stamped sequence are digest-identical, and (b) save@N → restore → tick-to-N+K replaying the
  same post-N tick-stamped commands == uninterrupted run. This locks input determinism at the
  composition level, head-less.
- On the pinned host: open the scene, run a live match, capture the FR-PO-052-class render-loop perf
  number, confirm 60 FPS, sign off the rendering half.

## 6. Deterministic manager-command channel (design detail)

This is the one part of the plan with genuine architectural risk, so it is specified further even
at this stage.

**6.1 Constraints, and what determinism can and cannot mean here.** Manager input arrives on the
**UI thread at an arbitrary wall-clock moment**, but the engine only accepts changes **at a
tick/stride boundary**. Quantizing application to the next boundary (§6.2) is *necessary* but **not
sufficient** for reproducibility: the tick *index* a live click lands on is itself a function of
wall-clock scheduling, pause state, and speed multiplier, so **a live human-driven match is not
byte-reproducible from the human's intent** — you cannot replay "the clicks." Reproducibility is
therefore defined against a **tick-stamped command log** (`(appliedTick, command)`): the match is
byte-identical given *the same `MatchSetup` (which carries the seed and the initial squads / tactics /
manager config / GK-heading flag) + the same log*. This is why the log is a P2 deliverable, not an
optional add-on — without it there is nothing to replay from, and the P6 acceptance test (§5-P6)
would have no defined "the same commands." A naive "call `SetTeamTactic` from the button handler"
fails on both axes: it races the sim thread **and** records no applied tick.

**6.2 Mechanism.** Commands are **enqueued** (thread-safe) on the UI thread and **drained on the sim
thread** by the `MatchClientDriver`'s pre-tick hook (§4), at a fixed point at the top of the tick
ahead of the AI phase — a command enqueued during rendering of tick *N* is applied at the top of
tick *N+1*. Each command carries only data that maps onto a **live** engine mutator (§3-2); there is
no path to poke engine internals or to invoke a pre-kickoff/boot mutator. Apply order within a
drained batch is FIFO and stable. Every applied command is appended to the tick-stamped log with the
tick it was applied at. The hook fires **inside `LiveMatchStreamer.TickOnce()`** (not the background
pacing-loop wrapper around it), so the threaded live loop and a direct head-less `TickOnce()` call
both exercise the drain — that is what makes the P2 determinism test meaningful. Commands enqueued
**after `MatchEnded`** must not mutate the finished match, and the **sim side is the authority** for
that (it reads the engine's live `_matchEnded`, not a lagging frame): `SubstitutePlayer` already guards
`_matchEnded` (`MatchEngine.cs:1155`), and a `SetTeamTactic` / `SetPlayerTactic` staged post-end never
commits — its pending→active commit runs inside the AI phase, which the engine freezes at full time
(the match-flow full-time transition freezes AI/Physics/Resolve). The UI's enqueue-time check (from the
latest frame's `MatchEnded`, which trails the sim by ≥ 1 frame) is therefore a best-effort early-out
only — it **cannot** be the guarantee, because a command can pass it in the window between the sim
setting `_matchEnded` and the UI observing it. Such a straggler is harmless: it is either dropped by the
sim-side guard when next drained, or never drained at all (the auto-paused sim does not tick) — either
way it never mutates the finished match. The UI also gates tactical input at full time (§5-P5) so a
click does not silently no-op. **Design requirement (not assumed):** any live command whose applied
tick would fall at/after `MatchEnded` must be dropped sim-side; where an existing mutator lacks that
guard, P2 adds it at the drain.

**6.3 Save / restore integrity.** Distinct guarantees, all required by §2's done-criteria:
- **Live save → restore (from the current point):** covered by the **existing** snapshot
  serialization — a committed tactic (v9/v10 `TeamTactic`/`PlayerTactic`), a substitution
  (`_activeBenchSlot` + counts + on-pitch attributes), and manager/GK-heading state (v13/v18) are all
  already in the snapshot, so restoring from a snapshot taken *after* a change re-ticks identically
  **with no `SNAPSHOT_SCHEMA_VERSION` bump**. **Invariant (new):** a durable capture must run **on the
  sim thread, with the command queue fully drained** — the engine is single-threaded (only the sim
  thread may touch it, per the `LiveMatchStreamer` single-writer invariant), and a command
  enqueued-but-not-drained at capture time would be silently lost on restore. So a UI-thread save
  request **never calls the engine directly**: it sets a request flag serviced on the sim thread, which
  drains the queue, takes the capture, and hands the resulting bytes back to the UI thread — the same
  disjoint-by-construction rule as the frame read (§4). A naive UI-thread `CaptureDurablePayload` call
  would tear-read mid-tick engine state → corrupt save or crash.
- **Servicing the save when the sim is not ticking (paused / full-time):** the save-request flag must
  be serviceable **without advancing a tick**. The two states where a user most wants to save — paused,
  and the auto-pause at full time (§6.2) — are exactly the states where the pre-tick hook never fires,
  so binding save to "the next tick boundary" would make save-while-paused hang and a completed match
  unsaveable. The streamer therefore exposes a **`ServiceOnce()`** seam (§4/§5-P0): one controlled
  sim-thread pass that drains the command queue and honours a pending capture request **without ticking
  the engine**. The driver invokes it for a save regardless of pause/ended state (the engine is
  quiescent when paused, so a capture there is clean); while running, the pre-tick hook already performs
  the same drain-then-service step each tick, so the two paths share **one** servicing routine (no
  second code path to keep in sync). A completed (`MatchEnded`) match is therefore saveable.
- **Replay from an earlier point (rewind):** requires the tick-stamped log (post-snapshot commands
  are not in an earlier snapshot). P2 *produces* the log; the on-disk persistence + scrub-back
  *feature* that consumes it is deferred (§11) — but the in-memory log itself is not deferred.
- **`EnableGkHeading` interaction:** a GK-heading-on match **is saveable** — no special-casing. Verified
  against `MatchEngine.cs`: `EnableGkHeading` serializes its cross-tick state at
  `SNAPSHOT_SCHEMA_VERSION` 18 (its own contract documents "a flag-on engine is … snapshot-safe"), and
  `CaptureDurableHeader` / `CaptureDurablePayload` do **not** throw on the flag, so the "already in the
  snapshot" guarantee of the first bullet applies unchanged — no `SNAPSHOT_SCHEMA_VERSION` bump, no
  save-affordance gating, and `EnableGkHeading` stays the ordinary P0 setup-only toggle (§3-2). (This
  supersedes an earlier draft that treated GK-heading as save-hostile; that reflected a Phase-1
  fail-loud the project's Phase-2 v18 serialization has since removed — the one live boundary this plan
  had against the engine, re-checked against current source.)

**6.4 Boundary with the browser viewer, and with playback controls.** Two separations, both
load-bearing: (1) the browser `LiveMatchServer` stays playback-only, permanently; this game-command
channel lives **in-process** (Unity UI → driver → sim thread) and is never folded into the loopback
frame stream — remote input, if ever wanted, is a separate authenticated endpoint (§3-7). (2)
**Pause/speed are not game commands.** They are presentation pacing that must never affect tick
content (the browser viewer's core invariant), so they stay on the streamer's existing playback
surface — never the `ManagerCommandQueue`, never the tick-stamped log, never the digest.

## 7. Rendering, camera, HUD (P4/P5 detail)

- **Reuse the geometry that already exists:** `MatchViewerConstants` carries the IFAB pitch-marking
  catalogue the HTML viewer draws; the Unity pitch renders from the same `[FIXED]` values (one
  source of truth for markings across both Views).
- **Coordinate mapping:** engine coordinates are corner-origin (X 0–105 goal-to-goal, Y 0–68
  touchline, Z up) — the View maps to Unity world/screen space in one documented adapter, the only
  place the mapping lives.
- **Interpolation:** render at display rate, interpolating agent/ball positions between the last two
  captured frames (P3) so motion is smooth despite the 10/60 Hz sim cadence — a pure View concern,
  never fed back into the sim.
- **HUD:** score + match clock (reusing the browser viewer's minute-rounding fix), speed indicator,
  collapsible stats panel.

## 8. The Unity host bootstrap (P4)

`MatchClientBehaviour` is the project's first `MonoBehaviour`/PlayerLoop host. It is deliberately
**thin**: lifecycle (`Awake` constructs `MatchSession`, `OnDestroy` stops the streamer), a
per-`Update` frame read + render, and UI-event → command-enqueue. All non-trivial logic stays in the
host-free `match-client-core` (P0–P3) so it remains shim-testable; the Unity-only `match-client-unity`
assembly (excluded from the shim gate, §5-P0) and its `MonoBehaviour` are verified only at a cert run.
This is also where the profiling-marker convention (src/CLAUDE.md §ProfilerMarker) first gets a real
render/Update loop to instrument.

## 9. Testing strategy

- **Host-free (shim, every push):** command-queue determinism (enqueue/drain ordering, apply-tick,
  FIFO batch), the drained-empty-queue-before-capture invariant (§6.3), the `ServiceOnce()`
  save-capture path while **paused and at full time** (§6.3 — a save taken paused/ended round-trips to
  an uninterrupted run), the sim-side post-`MatchEnded` command discard (§6.2 — a command enqueued in
  the end-of-match window is dropped, not stranded), observer-neutrality re-lock for the extended
  frame, interpolation/camera/stat math unit tests, `MatchSession` lifecycle, and
  the P6 `ScenarioRunner` cross-spec scenario (same `MatchSetup` + **same tick-stamped log** ⇒ digest
  equality + save/restore-with-log round-trip). **This proves the reproducible-from-the-log core —
  apply-tick determinism, drain ordering, and log-driven replay — without a Unity host.** (It does
  *not* claim live wall-clock click timing is reproducible; by §6.1 it is not, and reproducibility is
  defined against the log, which is exactly what these tests exercise.)
- **Host (cert run):** scene boot, 60 FPS render, live tactical input through the UI, post-match
  report; FR-PO-052-class render-loop perf capture on the pinned tuple.
- Follows the project's established split: logic is gated in CI; the rendering skin is human/cert
  verified, exactly as `certification-platform.md` already partitions determinism vs. host concerns.

## 10. Risks, dependencies & open questions

| # | Item | Disposition |
|---|---|---|
| R1 | **Unity host availability** for P4–P6. | Mitigated: the July-19 recert opened the pinned host; P0–P3 need no host, so work proceeds regardless. |
| R2 | **Input determinism** (the core risk). | Contained in P2/§6; reproducibility defined against the tick-stamped log (§6.1), fully head-less-testable; heaviest adversarial focus. |
| R3 | **Snapshot schema churn.** | Avoided: live save/restore is covered by the existing v9/v10/v13/v18 serialization **when the command queue is drained empty at capture** (§6.3); the tick-stamped log is an in-memory match-record artifact needing no `SNAPSHOT_SCHEMA_VERSION` bump. Confirm the exclusion set at promotion. |
| R4 | **Scope creep into stats/career.** | Hard-fenced in §11; this client is Match-View-only. |
| R5 | **UGUI vs UI Toolkit.** | Master plan pins UGUI for Stage 1; revisit only if it becomes a real constraint. |
| Q1 | Journal scope. | **Settled:** the in-memory tick-stamped command log is in **P2 scope** (required for §2 item-3 determinism and the P6 test, §6.1); only the on-disk replay/rewind *feature* that consumes it is deferred (§11). |
| Q2 | 2D-first vs. 3D. | Recommend **2D-first** per master plan Month 1-2; 3D is a later polish spec. |
| D1 | Depends on: match-engine observation surface (exists), public mutation API (exists), `LiveMatchStreamer` (exists), pinned Unity host (exists). No upstream blockers for P0–P3. | — |

## 11. What this plan does NOT do

- No heatmaps / xG / PPDA / advanced-stats overlays (master plan §3.3 — separate Stage-1 stats
  spec).
- No season/career/transfer flow — the client boots a single demo match via `MatchSession`; wiring
  it into a career loop is separate Stage-1+ work.
- No audio, no art/animation polish, no 3D (2D-first).
- No on-disk replay/rewind feature — P2 produces the **in-memory** tick-stamped command log; persisting
  it and building scrub-back / rewind on top is a separate Stage-1+ feature (§6.3).
- No networking / multiplayer / remote input (Stage 5+; §6.4).
- No new numbered spec **yet** — this is a design supplement; promotion to a numbered UI spec (the
  first member of the Stage-1 UI layer) follows the #21/#22/#27 precedent if/when the owner elects.

## 12. Recommended next step

~~Promote this note through one adversarial-review cycle to convergence (per project convention),
then land **P0–P2 host-free** first — the foundation + the deterministic command channel — since
that is the highest-risk, fully-testable core and needs no Unity host. The rendering skin (P4–P6)
follows once the input/determinism core is locked and a cert-host slot is scheduled.~~
**DONE** — AR converged at AR-8; P0/P2 landed 2026-07-24, P1/P3 landed 2026-07-27.

~~**Next step (2026-08-03): P6's head-less closed-loop scenario, before P4.** Both preconditions the
original recommendation named are met — the input/determinism core is locked, and the cert-host slot
is no longer hypothetical (the July 19, 2026 certification, `certification-platform.md` v1.4
✅ PINNED, cleared the host block on P4–P6).~~

~~The ordering argument is the shim gate. `match-client-unity` is in
`generate_projects.py`'s `SHIM_EXCLUDED_ASMDEFS`, so **every P4/P5 line is invisible to
`tools/dotnet-ci`** and verifiable only on the pinned host. §5-P6's scenario is the opposite: it is
head-less, gate-verified on every push, and depends on nothing in P4/P5 — it asserts that two runs
with the same `MatchSetup` + tick-stamped sequence are digest-identical, and that
save@N → restore → replay equals an uninterrupted run. Landing it first means the render skin arrives
against an existing determinism lock rather than ahead of one, which is the §9 testing strategy's own
posture and the direct lesson of the capstone that asserted a match ticked while every match was a
0–0 deadlock (ERR-030-014).~~

**DONE — the head-less half of P6 LANDED 2026-08-03** (see the v0.10 Version History row). Both
scenarios are in `src/match-client-core/tests/`, on the #19 `ScenarioRunner`, gate-checked every push.

~~**Next step: P4 on the pinned host**, with P5 and the on-host half of P6 (scene boot, 60 FPS render,
live tactical input through the UI, the FR-PO-052-class render-loop perf capture) after it. Nothing
head-less is now blocking: the render skin arrives against an existing determinism lock, which is
exactly what the ordering argument above was for.~~

**DONE for its host-free half — P4 was split into P4a / P4b, and P4a LANDED 2026-08-03** (see the
v0.12 Version History row and §5-P4). Rule 1 below says every decision the render skin makes belongs
in a gate-compiled assembly; P4a is that rule turned into a phase, so it ran host-free ahead of the
skin exactly as P6's head-less half did. It also surfaced KD-P4a-1 — a stale goalkeeper flag in the
streamer's boot-time roster cache, which had been wrong in the browser viewer since P1 and would
have been inherited wholesale by a Unity roster type.

~~**Next step: P4b on the pinned host**, then P5 and the on-host half of P6 (scene boot, 60 FPS render,
live tactical input through the UI, the FR-PO-052-class render-loop perf capture). P4b now binds a
render model that is already decided and already tested, so what the host verifies is binding, which
is precisely what §12 rule 1 was aiming at.~~

**Amended August 7, 2026 — P5 was split the same way P4 was, and P5a LANDED** (see §5-P5 and the
v0.16 row). The rule-1 argument that produced P4a applies to the shell's own decisions — which
controls are live in which match state, and what the speed buttons offer — and those had not been
extracted. They now live in `match-client-core`, gate-compiled and test-locked.

~~**Next step: P4b on the pinned host**, then P5b and the on-host half of P6. Both remaining phases now
bind surfaces that are already decided and already tested, so what the host verifies is binding —
which is precisely what §12 rule 1 was aiming at. **The one open decision ahead of P5b is a layering
question, not an implementation one:** where the four screens' `ScreenId` catalogue and navigation
graph live, given FR-UI-010 forbids the framework and `ui-framework` sits above `match-client-core`
(§5-P5a, and the same question as roadmap §6 item 2).~~

**The layering decision is RESOLVED and LANDED (2026-08-07, v0.17)** — `src/client-app/`, a new
gate-compiled assembly above `ui-framework`, holds the catalogue and the graph (§5-P5a's resolution
block). **Next step: P4b on the pinned host**, then P5b and the on-host half of P6, with nothing now
ahead of P5b: both remaining phases bind surfaces that are already decided and already tested, so
what the host verifies is binding — which is precisely what §12 rule 1 was aiming at.

### Status change, August 3, 2026 — this plan is now the only UI track

The owner reversed the roadmap's B6 decision: **the product ships this client**, not the web-hosted
viewer (`path-to-playable-roadmap.md` §7 supersede note, v0.11). P4–P6 stop being "the native client we
also want eventually" and become the critical path to a shipping UI. Three consequences for how P4 is
built, in priority order.

**1. The gate cannot see this assembly, and that is permanent — so keep logic out of `MonoBehaviour`s.**
`match-client-unity` is in `SHIM_EXCLUDED_ASMDEFS` and will stay there: the Unity shim covers `Vector2`,
`Vector3`, `Mathf`, `Debug` and `Profiling` — value types and statics that can be reimplemented honestly
— and there is no honest head-less `MonoBehaviour`, `GameObject` or `Camera`. **Do not extend the shim
to buy coverage.** A `MonoBehaviour` stand-in that never receives a lifecycle would let a render loop
that never runs report green, which is ERR-030-014's failure mode transplanted one layer up, and this
repo has already paid for that lesson once at the cost of months of 0–0 matches.

The mitigation is architectural, and P3 already demonstrates it: `FrameInterpolator` and
`FollowBallCamera` are plain C# in `match-client-core`, gate-compiled and test-locked, and they are
where the camera and interpolation *decisions* live. Hold that line for everything P4 adds. Anything
that decides — what to draw, where the camera goes, what a click means, which intent an input maps to,
when a caption shows — belongs in `match-client-core` or `ui-framework`. `MatchClientBehaviour` and its
siblings should assign `transform.position`, instantiate prefabs, and forward input events, and should
contain no branch a test would want to reach. Then the uncovered surface is *binding*, which a cert run
genuinely verifies, rather than *behaviour*, which a cert run verifies only along the paths someone
thought to click.

**2. Budget a cert-host run per landing, not one at the end.** The host block cleared July 19, 2026
(`certification-platform.md` v1.4 ✅ PINNED), so this is a scheduling choice, not an access problem. A
skin first exercised at the end is the never-compiled-surface trap; this project has hit that seven
times, including on a *production* assembly.

**3. `PM-1` must be re-established here.** It was reached on July 27 on the browser surface, and what
that proved — the substrate under a renderer is complete and correct — carries over unchanged, because
the substrate is unchanged. What does not carry over is the milestone itself: three of PM-1's four exit
criteria are statements about a *screen* (a `MatchSetup` from UI input, live tactical changes applied
through the command channel from a screen, a post-match `MatchAnalyticsResult` render). The fourth, the
determinism criterion, is met head-lessly and stays met. Treat the other three as open against this
client.

**What does not change:** no substrate work is needed before P4 starts. #38's view models and
dispatchers, `MatchFrameView`, `MatchViewModelSource`, `MatchTacticsDispatcher`, `NavigationShell`,
`MatchSession`, the command channel and the P6 determinism locks are all in gate-compiled assemblies and
are exactly what a UGUI skin binds — the "renderer is a leaf" property #38's contract was written for,
now being used in the direction it was designed for. And no art pipeline is implied: §5-P4 is 2D-first,
the pitch renders from the IFAB `[FIXED]` geometry already in `MatchViewerConstants`, and agents are
primitives. Sprites are a polish decision, not a prerequisite.

**One thing P4 must not do.** `MatchSession` now exposes two mutually exclusive ways to advance a
match — `Start()` (the streamer's pacing thread) and `TickOnce()` (the head-less advance) — and the
Unity host uses the first. `TickOnce()` throws on a `Start()`ed session, so this is enforced rather
than documented, but `MatchClientBehaviour` should not be tempted to drive the sim from `Update()` via
`TickOnce()`: the pacing loop, not the render loop, owns the tick rate, and driving ticks from
`Update()` would make sim cadence frame-rate-dependent — the §3 constraint-1 violation this whole plan
is built to avoid.

## Version History

| Version | Date | Notes |
| 0.19 | 2026-08-15 | **P4b LANDED — the Unity binding, `MatchClientBehaviour.cs` — and backprop for it into this document (M13 of round 2's Medium/Low pass), since the code landed across three commits without one.** §5-P4b's job-list bullet is corrected: it still described the round-1 contract (rectangles arriving corner-normalised, `B − A` as the extent, everything placed "at zero height") that round 1's own AR-1 H1 finding removed from the binding by moving the rectangle-into-four-lines decomposition into `PitchMarkings.BuildDrawables()`; it now names that method and states the current one-primitive-per-entry contract, plus round 2's M12 ground-layer heights and M11 prefab-contract clause split. **Round 1 AR (H1-H3 + 9M + 5L, commits `97bca12`/`2538147`):** H1 moved the rectangle decomposition into `match-client-core` (above); H2 turned the prefab contract from prose into an enforced check at `InstantiatePrefab` (neutral root, no world-space `LineRenderer`); H3 deleted `PrimitiveDefaultRadiusM` in favour of a unit-radius/unit-length authoring contract. The nine Mediums covered wiring validation before any `Instantiate` call, binding `IsGoalkeeper`/`IsSentOff` as marker tints, the possession-ring radius reading `AgentRenderModel` rather than a second `[GT]`, a once-per-agent `MeshRenderer` resolve via `MaterialPropertyBlock` instead of a per-frame `.material.color` leak, the effective-tick-rate product moving onto `LiveMatchStreamer`, the previous/current frame latch extracted into the new `LiveFrameLatch` (§12 rule 1), `RenderAgents` walking `ProjectAgents`' returned count, and the Active Input Handling project-setting requirement recorded. **Round 2 AR (H4-H6, commit `5c93940`):** H4 was a missing `.meta` (fresh GUID every checkout); H5 made an `Awake` throw terminal rather than leaving the component enabled with null fields (Unity delivers `Start`/`Update` after logging an `Awake` exception); H6 replaced the bare `Shader.PropertyToID("_Color")` literal with the inspector-exposed `_colorPropertyName` plus a per-marker material/property check, since this repo's `GraphicsSettings.asset`/`manifest.json` do not agree on Built-in vs URP. **Round 2 Medium/Low pass (M10-M13/L6-L8, this landing):** M10 gives the goal mouth its own prefab + `[GT] GoalMouthWidthM` instead of sharing the marking line's (`PitchMarkingKind.GoalMouth`'s own doc calls it furniture, not a Law 1 marking). M11 splits prefab-contract clause 2 into 2a (flat ground props — unit radius/length in local XZ, zero extent in local Y, the mesh itself authored flat) and 2b (the ball — the one unit-radius volumetric sphere, scaled uniformly); `GroundScale` is renamed `FlatGroundScale` so the name states which rule a call site follows, and `PlaceLine`'s comment no longer credits `Y = 1` with the flatness. M12 adds four ordered `[GT]` ground-layer heights (markings, ball shadow, possession ring, agent marker — millimetre-scale, strictly ascending) so the four previously-coplanar ground layers do not z-fight; read directly in `MatchClientBehaviour` rather than threaded through the `match-client-core` render models, since the ordering is a pure presentation choice with no simulation meaning and `PitchMarking` carries no height field to extend. L6 short-circuits `BuildMarkings`/`BuildAgentObjects`/`BuildBallObjects`/`BuildScene` on `_wiringRejected` so one bad prefab does not keep instantiating and re-logging for every remaining object. L7 validates `MarkingLineWidthM` (and the new `GoalMouthWidthM`) through `RequireInRange` like their render-cue siblings. L8 adds `RequireTiltOrOffsetNonzero`, a `CameraTiltDegrees`/`CameraLateralOffsetM` pairing check in `RequireFarRayMeetsGround`'s own shape — each dial is individually legal at zero, but both together sit the camera directly above its look-at target, an undefined `Transform.LookAt` rotation. `match-client-core` `MatchClientConstantsTests.cs` 165 tests, all green (gate run this landing). `match-client-unity` stays excluded from the `dotnet-ci` gate by design (§12 rule 1) — reviewed by hand, as every P4b finding above was. |
| 0.18 | 2026-08-07 | **The v0.17 landing is GATE-VERIFIED, superseding that row's "gate not runnable" line — the SDK installs from the Ubuntu apt archive (8.0.129) even though the dot.net installer is proxy-blocked** (owner-surfaced; recorded beside the gate command in `src/CLAUDE.md`). Build 0 errors / 5 pre-existing warnings; **`ClientApp.Tests` 15/15 on first execution**; the FR-UI-001 reverse-reference scan **fired on `client-app` exactly as designed** and the assembly is now in the sanctioned-renderers list (`MatchViewObserverNeutralityTests.cs` v1.1, `UiFramework.Tests` 50/50 re-run); `MatchEngine.Tests` 434 / 2 / 10 in 48 m 8 s, with **both failures verified pre-existing at `origin/main` `9b8a7b4`** by executing exactly those two tests there (2/2 fail, 11 m 4 s — `sim_match_engine_close_chance` meanCosine −0.119 vs −0.10; `sim_match_engine_keeper_contact` deepDiveEarly = 1; filed as a root OPEN ISSUES entry, owned by the realism track). Every other suite green. PR #310 tracks the branch. |
| 0.17 | 2026-08-07 | **The §5-P5a layering question RESOLVED by owner decision, and the assembly LANDED: `src/client-app/` (`TacticalDirector.ClientApp`), the client composition layer above `ui-framework`.** The four screens' `ScreenId` catalogue and navigation graph now have the home v0.16 recorded as missing. The candidates were `match-client-unity` (gate-invisible — wrong by §12 rule 1) or a new assembly; the owner chose the new assembly on the `match-engine` precedent: a composition root that wires generic infrastructure into a concrete product lives above what it wires, and FR-UI-010 makes a concrete screen set composition, not framework. **Three types, referencing only `ui-framework`:** `ClientAppConstants` (four `[FIXED]` screen ids 1–4; 0 deliberately never allocated — the `ManagerCommandKind.None` zero-value-safety convention `NavigationShell`'s un-rooted `Current` guard cites), `ClientScreens` (the ids as typed values), `ClientScreenFlow` (the five-edge graph — OpenTacticsSetup/Push, CancelTacticsSetup/Pop, StartMatch/Replace, ShowPostMatchReport/Replace, ReturnToMainMenu/Pop — as guarded moves over a **privately-owned** `NavigationShell`, so the graph is enforced by encapsulation rather than convention and the P5b binding decides nothing). **The two Replace edges are the design content** and both are locked by where a later Pop lands (a Push in either's place strands a stale screen beneath the stack and the final `ReturnToMainMenu` assertion fails): TacticsSetup → MatchView because a running match must not sit above a setup screen a "back" could return to, MatchView → PostMatchReport because §6.2 freezes the match at full time and a Pop from the report must land on Main Menu, never a dead match view. **Registration-id transposition is refused at construction** (the ERR-029-005 silent-transposition class — a swapped argument pair would register each screen under the other's identity with all gates green). **Deliberately absent, recorded:** an abandon-match edge out of MatchView — §5-P5b specifies no quit control, and an edge without a specified consumer is the FR-CS-049 phantom. 15 tests (3 catalogue + 12 flow). **No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag / draw site / draw-order change — nothing here reaches the sim**; blast radius: no scenario tick window, rate band, corpus fit or perf baseline is perturbed (the assembly is above every sim surface and nothing existing references it). **Full dotnet gate NOT RUNNABLE in this environment** (no .NET SDK; CI on push is the compiler — the v0.16 caveat unchanged). Verified instead by manual review against the compile risks that matter here: type/filename match, namespace = rootNamespace, `in`-parameter signatures against `ScreenRegistration`/`NavigationShell`, using-group order, explicit access modifiers, and `generate_projects.py` regenerated clean (66 csproj, both ClientApp projects present). C3's management screens (roadmap §6 item 2) inherit the answer by precedent: they register into this same assembly when P7 opens them. **An adversarial-review pass over the landing found 0 High, 1 Medium, 1 Low, both fixed; a second full pass came back clean.** The Medium is worth naming because it is this project's recurring wrong-verification-count class (the ERR-008-022 "9 locks / 5 of 8" shape): the landing's first-draft record claimed **13 tests (2+11 / 3+10, inconsistently)** across seven documents while the committed fixtures hold **15 (3 catalogue + 12 flow)** — every count above was corrected against `grep -c "\[Test\]"` before commit, not after CI. The Low was a file-count slip in `file-manifest.md`'s header (9 + 10 `.meta` → 7 + 9). |
| 0.16 | 2026-08-07 | **P5 SPLIT into P5a / P5b, and P5a LANDED — the shell's decisions extracted host-free, exactly as P4a did for the render skin.** The split is §12 rule 1 applied to a phase that had not had it applied: "the UGUI shell" as one host-only phase puts *when a control is available* and *what the speed buttons offer* inside `MonoBehaviour`s the gate cannot compile, which is the leak AR-P4a2-H1 found in the deliverable built to close that leak. Landed in `match-client-core`: **`PlaybackSpeedLadder`** (the four `[GT]` multipliers as an ordered ladder — the catalogue held four independent dials and said nothing about order, opening rung, or end behaviour; stepping **clamps rather than wraps**, since a faster-click at 10× dropping the viewer to 1× reads as a fault rather than a limit; pause stays off the ladder because it is a streamer state and 0× is outside the streamer's legal range) and **`MatchControlAvailability`** + **`MatchControlLockReason`** (§5-P5's standing "the UI gates tactical input at full time so a click does not silently no-op", resolved into three states carrying *why* each is locked). **Two decisions inside the availability type are the kind a later tidy-up reverses, so both are test-locked:** saving stays enabled at full time (§6.3 — a finished match is when a viewer wants to save, and `ServiceOnce()` needs no tick; locking it with the tactical controls makes a completed match unsaveable), and a frameless streamer does **not** resolve to `Live` — `TryGetLatestFrame`'s out-parameter on a false return is `default(LiveMatchFrame)` whose `MatchEnded` is *false*, so a `From` reading the frame unconditionally would report a not-yet-started match as fully interactive. **The one finding is the §5-P0 cap note turned from prose into an assertion:** that note required `MaxLiveSpeedMultiplier ≥ 10` so 10× is not refused, and nothing enforced it — `SetSpeedMultiplier` fail-louds, so a cap below a step would have surfaced as *one speed button throwing mid-match while the other three worked*. `MatchClientConstants.RequireStreamerAcceptsSpeed` now pairs each speed against the streamer's `[Min, Max]` at load, in the shape of the existing `RequireFarRayMeetsGround` cross-dial check. **Deliberately not built, and recorded rather than silently dropped:** the four screens' `ScreenId` catalogue and navigation graph, which has no correct home — FR-UI-010 forbids the framework, and `ui-framework` sits *above* `match-client-core` so the core cannot hold it either; the remaining candidates are `match-client-unity` (gate-invisible, wrong by rule 1) or a new assembly. That is a layering decision for the owner and the same question roadmap §6 item 2 already flags for C3. **No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream, domain tag or draw site, no draw-order change — nothing here reaches the sim.** No ERR filed: the cap gap was a missing enforcement of an existing design note, not a contradiction in it. **Blast radius checked: nothing moved.** No behaviour change reaches the engine, so no scenario tick window, per-90 rate band, A4a corpus fit or FR-PO-052 baseline is perturbed; `match-client-core` gains tests only. **Full dotnet gate NOT RUNNABLE in this environment** — no .NET SDK, and every SDK binary host (`dot.net`, `builds.dotnet.microsoft.com`, `dotnetcli.azureedge.net`) returns 403 at the agent proxy, so CI on push is the only compiler for this landing. Verified instead by manual review against the compile risks that matter here: type-name/filename match, brace balance, CS0104 collision sweep over the newly-imported `TacticalDirector.MatchViewer` namespace (12 public types, none colliding), `MatchViewerConstants` and the three frame value types confirmed public and `default`-constructible, `using`-group order, private-static-field `s_` naming (FR-CS-002), and `generate_projects.py` regenerated clean (64 csproj). `match-client-core` 135 → ~157 expected. |
|---|---|---|
| 0.15 | 2026-08-04 | **P4a adversarial-review pass 2 — 1 High, 4 Medium fixed; run over the tilted-view revision's own output.** **AR-P4a2-H1 (the one that matters):** the camera rig placed the camera but never said how much it *sees*, so P4b would have chosen a field of view inside the `MonoBehaviour` — a framing decision in the one place the CI gate cannot compile. The leak was in the deliverable built to close exactly that leak. `PitchCameraPose` gains `FieldOfViewDegrees`, `MatchClientConstants` gains `CameraVerticalFovDegrees` bounded *against the tilt* (`tilt + fov/2 < 90`, else the lowest ray never meets the ground), and `PitchCameraRig.GroundExtentAlongTilt` gives the framing a number — asymmetric near/far, because the trapezoid reaches further beyond the aim point than in front of it. KD-P4a-2 is amended in place; §5-P4b's job list now says "pick nothing here". **M-1:** §5-P4b instructed *both* cameras in one bullet — the new `PitchCameraRig` placement and, in the same sentence, the deleted orthographic one — while the bullet below it said the orthographic assumption was wrong. `path-to-playable-roadmap.md` B8 carried only the stale half. **M-2:** `PitchMarking`'s class doc still sent the render skin to `ToView` for markings that must lie on the ground plane (following it stands every marking upright in the world XY plane); `ToView`/`ToPitch` themselves had no production caller left after the revision and are deleted, their tests re-anchored onto `ToWorld`/`TryGroundHit`. **M-3:** `CameraLateralOffsetM` was the only camera dial with no validation and lands straight in the camera's world position — now `RequireFinite` (either sign is meaningful, so a range would be wrong). **M-4:** `MatchClientConstants.cs` and `MatchRenderProjection.cs` never got their v1.4/v1.2 version-history rows in the revision, so each file's newest row described content it no longer had while three documents cited versions the files did not claim. **Sweep after the fixes found one more Medium, so this pass is not converged:** `PitchMarkingKind.Rectangle` still documented corner ordering as *not* guaranteed and told consumers to re-normalise — the exact contract AR pass 1's H-1 reversed. `PitchMarking.cs` was fixed then and the enum beside it was not, so two files stated opposite contracts for one field, and the enum is what a renderer switching on `Kind` reads first. Fixed; the guarantee is test-locked by `EveryRectangleArrivesWithItsCornersNormalised`. `match-client-core` 129 → 135. **Full dotnet gate: PASSED, 0 failures** (30 suites). **Pass 3 then ran over the whole P4a surface and surfaced no High and no Medium — the loop is converged.** It found two Lows, both fixed: `PitchCameraPose`'s header and summary still described it as two values, and a test comment credited the wrong assertion with guarding the static-init-order defect (asserting the tilt is non-zero does NOT catch a reorder — by the time a test reads the field, static init has finished and it reads its real value either way; re-evaluating the invariant on the finished values is what catches it). **Full dotnet gate on the converged tree: PASSED, 0 failures** (30 suites; `match-client-core` 135, `match-engine` 368 unchanged). |
| 0.14 | 2026-08-04 | **KD-P4a-2 — the view is TILTED, and the faked height cues are deleted (owner call).** P4a shipped a flat top-down view with ball height suggested by a sprite lift and a capped size ramp; the owner reversed it to an FM-style view from above, tilted back from vertical and slightly off centre, on the grounds that the ball only needs to be visible on and above the pitch. Taken **before P4b** because it changes a P4a contract and is cheap now, expensive later. **The revision removes more than it adds:** height becomes a real world axis, so `BallHeightViewOffsetPerMetre` / `BallHeightScalePerMetre` / `BallMaxHeightScale`, `BallRenderModel.SpritePosition` / `SpriteRadius` and `MatchRenderProjection.HeightScale` are all gone — and with them v0.13's M-5 finding and its recorded 10 m saturation limitation, which stop existing rather than needing a retune. **New:** `PitchCameraRig` / `PitchCameraPose` (height, tilt-from-vertical, lateral offset; a placement is a decision, so it is gate-compiled, and the pose is two world points because `Quaternion` is not in the shim) and `PitchViewProjection.ToWorld` / `ToWorldGround` / `TryGroundHit`. **The one real cost:** screen position is no longer affine in pitch position, so the click inverse becomes a ray/ground-plane intersection — `Camera` is not in the shim, so Unity supplies the ray and the math stays gate-tested. **Survivors, each for a reason:** the shadow (under any tilt a lofted ball separates from the pitch point it is over — the one cue perspective cannot supply), the corner→centre re-origin (it is the ground plane), and `FollowBallCamera` (it decides *where* the camera looks). **Recorded rather than left implicit:** the engine's Y becomes the world's Z and its Z the world's Y — an axis swap, the same trap class as the corner origin, locked by a test (inverting it fails seven) — and `FollowBallCamera`'s pitch clamp is now approximate, describing a rectangle of visible ground where a tilted view sees a trapezoid; kept deliberately, since its job is keeping the target near the pitch, not exact framing. §5-P4b's job list gains the camera placement and the click ray, and its "orthographic" note is corrected to a tilted perspective camera. |
| 0.13 | 2026-08-04 | **P4a adversarial-review pass — 1 High, 5 Medium, 3 Low fixed; the pass then re-run clean.** §5 gains a P4a AR block and §5-P4b now states the one contract change. **AR-P4a-H1 (the phase-boundary one):** `PitchMarkings` builds each end box from its goal line *inwards*, so the away penalty area and away goal area were emitted with **descending X** while the home pair ascended, and `PitchMarking.Rectangle` documented no ordering. A binding taking `B − A` as an extent would have drawn exactly those two inverted — at one end only, in a `MonoBehaviour` the gate can never compile, inside the type whose purpose is to leave the skin nothing to decide. `Rectangle` now normalises (`A` = min, `B` = max) and P4b may rely on it; lines and goal mouths stay unnormalised by design. The fixture had laundered it (`AssertAreaBox` normalised with `Mathf.Min`/`Max` before asserting); it now reads `A`/`B` directly, and un-normalising the factory fails four tests. **M-2:** the render path had no non-finite gate while `MatchFrameView` refuses one fail-loud, and the doc excusing that said upstream already refused them — false, `FrameInterpolator` *propagates* a non-finite position by design. Ground positions are now refused; ball height keeps degrading gracefully, since a bad height still leaves a true ground position. **M-3:** `HasBall` is the stored fact and the ring radius derives from it, so a `[GT]` size cannot answer "who has the ball". **M-4:** boot-time validation replaces a silent cap repair, and the previously documented-only ring > marker invariant is enforced. **M-5:** two `[GT]` rationales carried fabricated figures (an uncapped 20 m ball is 2.8 m across, not "wider than the penalty area"); replaced with checked numbers plus the cap's real 10 m saturation point. **M-6:** the shirt-numbering rule was **duplicated, not moved** — the browser viewer's `computeJersey` was still live while three documents (this one included) said it had moved into `MatchRoster`; new `match-viewer/RosterShirtNumbers.cs` is now the single implementation both Views consume. **Lows:** a tautological test replaced, `FromStreamer`'s happy path covered, the ring/marker invariant enforced. **Full dotnet gate: PASSED, 0 failures** (whole tree green; all 30 suites reported, quarantine empty) — `match-client-core` 103 → 112, `match-viewer` 41 → 48, `ui-framework` 50 (unchanged), `match-engine` 368 passed / 8 skipped (unchanged; no `match-engine` source is touched by this pass), every other suite unchanged. No new compiler warnings — the five the tree reports are pre-existing CS0649s in `decision-tree`. No schema / RNG / domain-tag / draw-site / draw-order / engine-behaviour change. |
| 0.12 | 2026-08-03 | **P4 SPLIT into P4a / P4b, and P4a LANDED — the render model, host-free.** The split is v0.11 rule 1 ("keep logic out of `MonoBehaviour`s") turned from a discipline into a phase boundary: P4a is every render *decision*, gate-compiled and test-locked in `match-client-core`; P4b is the binding, which cannot be anything else. Sequencing them in that order means P4b arrives with nothing left to decide — the same argument that put P6's head-less scenario ahead of P4, one level down. §5-P4 is rewritten into the two sub-phases with a new KD; §12's next step becomes P4b. **New:** `PitchViewProjection` (the §7 "Coordinate mapping" adapter — corner-origin metres ⇄ a centre-origin view plane at 1 unit per metre, plus the inverse a pointer click needs; centring is what makes a home position and its away mirror differ only in sign, which is why the mirror assertions are one line each); `PitchMarking`/`PitchMarkingKind`/`PitchMarkings` (the 12-marking IFAB catalogue as shapes, read from the **existing** `MatchViewerConstants` `[FIXED]` values per §7's one-source-of-truth rule, both ends emitted from one loop over a sign — the D-arc and corner arcs deliberately absent, since neither has a `[FIXED]` constant and adding them would invent geometry and diverge the two Views); `MatchRoster` (match-constant per-slot data, and the shirt-numbering rule moved out of the browser viewer's inline JavaScript); `AgentRenderModel`/`BallRenderModel`/`MatchRenderProjection` (positions from the P3 interpolator's buffer because that is what is being drawn, every discrete cue from the newest frame because cues do not interpolate; possession ring; the ball's shadow / height-lift / capped-scale cues). Colour-free by design — a palette has no correct answer a test could assert, and `UnityEngine.Color` is not in the shim's surface. **KD-P4a-1, the finding:** `LiveMatchStreamer` cached team ids *and* goalkeeper flags at construction under "roster metadata never changes across a match" — true of team ids, **false of goalkeeper flags**, which `MatchEngine.SubstitutePlayer` rewrites, so a keeper substitution silently desynchronised the cache from the engine (and had been drawing the keeper ring on the wrong player in the browser viewer since P1). `LiveAgentCue` gains `IsGoalkeeper` — the first cue added through the KD-P1-6 extension mechanism — sampled per tick; `MatchRoster` holds no goalkeeper flag at all so the stale copy cannot come back; the streamer's accessor is kept, re-documented as boot-time only; `LiveMatchServer` reads the frame cue when a frame exists, fixing the harness with no JSON or script change. Re-reading the engine from the accessor was rejected: that is the off-sim-thread tear-read the single-writer invariant exists to prevent. **No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag / draw site, no draw-order change, no engine-behaviour change** — the cue is sampled from an existing read-only accessor. `MatchClientConstants` v1.2 adds the render-cue `[GT]` sizes v1.0 deferred "to P3/P4 with their consumers". |
| 0.11 | 2026-08-03 | **Owner reversed roadmap B6 — this plan is now the only UI track; the product ships this client, not the web-hosted viewer.** Doc-only; no `.cs` changed, no phase re-scoped, no design decision revisited. §12 gains a status-change block recording the three consequences for how P4 is built. **(1) The permanent one: the gate cannot see `match-client-unity` and never will**, so keep logic out of `MonoBehaviour`s — every decision (what to draw, where the camera goes, what a click means, which intent an input maps to) lives in gate-compiled `match-client-core`/`ui-framework`, and the Unity types assign transforms and forward input with no branch a test would want to reach. P3 already demonstrates the pattern (`FrameInterpolator`, `FollowBallCamera` are host-free and test-locked). **Explicitly refused: extending the Unity shim to fake `MonoBehaviour`/`GameObject`/`Camera` to buy coverage** — the shim reimplements value types and statics honestly, and a lifecycle-free stand-in would let a render loop that never runs report green, which is ERR-030-014's failure mode one layer up. That keeps the uncovered surface *binding* (which a cert run verifies) rather than *behaviour* (which it verifies only where someone thought to click). **(2)** Cert-host runs budgeted per P4/P5 landing rather than once at the end — the host block cleared July 19, 2026, so this is scheduling, not access, and a skin first exercised at the end is the never-compiled-surface trap this repo has hit seven times. **(3) `PM-1` must be re-established on this client:** its determinism criterion is met head-lessly and stays met, but its other three exit criteria are statements about a *screen* and were demonstrated on a surface that is no longer the product. **Nothing blocks P4 starting:** the whole substrate a UGUI skin binds — #38's view models and dispatchers, `MatchFrameView`, `MatchSession`, the command channel, the P6 determinism locks — is already gate-compiled and unchanged by the reversal, which is the "renderer is a leaf" property #38's contract was written for. No art prerequisite either: §5-P4 is 2D-first, the pitch renders from the IFAB `[FIXED]` geometry already in `MatchViewerConstants`, agents are primitives, and sprites are polish. |
| 0.10 | 2026-08-03 | **The head-less half of P6 LANDED — §5-P6's closed-loop scenario, before P4, per the v0.9 §12 recommendation.** §12 rewritten (P6 struck through as DONE; next step is P4 on the pinned host). **What §5-P6 turned out to require first.** The phase is written as "boot via `MatchSession`, inject a scripted tick-stamped command sequence, assert (a) digest equality across two runs on the same setup + sequence and (b) save@N → restore → replay == uninterrupted". Three of those verbs had no composition-level surface: `MatchSession` could not be advanced head-lessly (`LiveMatchStreamer.TickOnce()` is `internal` to `match-viewer`; the only public advance is `Start()`'s pacing thread), could not be saved (P0 explicitly deferred "the durable save-capture body that rides the `ServiceOnce` seam"), and could not be restored (the constructor always boot-configures a fresh engine). So P6 is three production additions plus the scenario. **(1) `MatchSession.TickOnce()`** drives the REAL streamer seam — `match-viewer/AssemblyInfo.cs` v1.1 adds `InternalsVisibleTo("TacticalDirector.MatchClientCore")`, keeping the seam internal to `match-viewer` so nothing widens for the browser viewer. A parallel client-side tick path was rejected: routing through the real seam is what makes the scenario a proof about the *shipping* composition — hook, frame capture and full-time auto-pause all behave as under paced playback. `TickOnce()` throws once `Start()` has been called; the streamer's "never concurrently with the pacing loop" rule had been a comment, and two threads through one engine is a data race, so it is now a guard. **(2) `MatchSession.CaptureSave()`** rides the `ServiceOnce` seam, so it works running, paused and at full time (the AR-7 H-1 shape). §6.3's drained-empty-before-capture invariant is now held **by ordering** — one sim-thread pass under the tick gate drains and applies, then encodes — rather than asserted afterwards. An `Encode` fault is latched and rethrown to the `CaptureSave` caller rather than escaping the pre-tick hook and killing the pacing thread (the AR pass-2 isolation posture, applied to a second escape path). The handshake is `Interlocked`/`Volatile`, NOT a lock held across `ServiceOnce`, which would have created the opposite lock order against the tick gate — a latent deadlock the obvious implementation walks straight into. **(3) `MatchSession.RestoreFrom(blob, squads)`** splits the constructor into a static `BootEngine` + an engine-agnostic wiring ctor, so a restored session re-applies no boot mutator; it deliberately takes no `MatchSetup`, since `ConfigureSquads` throws on a ticked engine and re-staging tactics would overwrite restored state. Plus **`TickStampedCommandReplay`** — the mechanism §6.1's invariant is *defined* against, now written down: enqueue each entry immediately before the tick whose pre-tick `CurrentTick` equals its `AppliedTick`, which is exactly where the original drain read the clock, so the log is a fixed point of its own replay (asserted). Out-of-order logs and entries whose application point has passed are refused fail-loud; skipping either silently yields a run that is not the log's run while reporting success. **The scenarios, and the one predicate that carries them.** `match-client-command-log-replay` and `match-client-save-restore-replay`, owning specs {16,19,21}, under `SCENARIO_PATH_CROSS_SPEC_PREFIX`, in the composition root's own test assembly per the `MatchEngineCapstoneScenarios` precedent. Both would pass on a command channel that did nothing — a run reproducing itself proves nothing about whether the commands are in the loop — so a **third, command-free control run must DIVERGE**, in a bounded window around the first command rather than merely eventually. That is the direct lesson of ERR-030-014, and it is the predicate the phase actually rests on. The script is ten commands over all three live mutators and **both teams**, straddling the save tick; a home-only script would have repeated #8 ERR-008-002 one layer up. The save tick is deliberately command-free, and the scenario *checks* that rather than assuming it: a command at the save tick is inside or outside the snapshot depending on drain order within the capture pass while carrying the same stamp either way, which would make the resume-from-N+1 slice silently wrong. **Recorded as deliberately not written:** a "queue drained at capture" predicate inside the scenario. The replay leaves the queue empty at every tick boundary, so it would be true regardless of capture order — vacuous. §6.3 is locked instead by a unit test that enqueues a command immediately before `CaptureSave` and requires it back applied and logged. **Still open in P6:** the on-host half — scene boot, 60 FPS, live tactical input through the UI, the FR-PO-052-class render-loop perf capture — which needs P4/P5 first. No `SNAPSHOT_SCHEMA_VERSION` change, no new RNG stream / domain tag / draw site, no draw-order change, no engine-behaviour change at all. Gate not runnable in this environment (the network policy blocks the .NET SDK download, same as the P0 landing); verified by exhaustive manual review + a `generate_projects.py` run confirming the new `TestingStrategy` reference resolves. |
| 0.9 | 2026-08-03 | **Retroactive sync — P1 and P3 LANDED 2026-07-27; this supplement was never updated and had continued to describe both as deferred.** No design change and no new work: the landings are five weeks old and were recorded correctly in `path-to-playable-roadmap.md` (Track-C rows **B1** and **B4**) at the time. This row closes the drift between the two documents. **P1 (commit `d0e8573`, roadmap B1):** `MatchPeriod` (derived from the two transition flags, KD-P1-2), `RestartCue` as its own enum rather than widening the digest-bearing `RestartType` (KD-P1-5), `MatchEngine.CurrentPeriod` + `RestartAppliedThisTick`, `ApplyRestart` declaring its cue at all six restart sites (KD-P1-4), and `LiveAgentCue` + `RestartBanner` as `match-viewer` types (KD-P1-1/KD-P1-6), carried through `LiveMatchFrame` → `MatchFrameView`. The restart cue stayed a **within-tick** engine field per KD-P1-3, so the `SerializeWorldState` exclusion proof needed no new class and there was **no `SNAPSHOT_SCHEMA_VERSION` change** — the KD held as designed. **P3 (commit `dfa506b`, roadmap B4):** `FrameInterpolator` — speed-aware alpha (an interpolator handed the unscaled tick rate falls further behind the sim every frame at 3×) and blending that **snaps rather than smooths across a discontinuity**, since a restart teleports the ball and a substitution swaps who occupies a roster slot; `FollowBallCamera` — dead zone, `1 − e^(−rate·dt)` smoothing proven frame-rate-independent by step subdivision rather than asserted, and a pitch clamp that centres when the view is wider than the pitch instead of oscillating between two impossible bounds. 23 tests. **P3 landed two of three deliverables, by design:** the §5-P3 live-stats accumulator was deliberately not built — #37's `MatchAnalyticsAggregator` (roadmap B3) already is one, and a second in `match-client-core` would be the parallel-surface trap (the PM AR-7 M-1 / `POSITION_COUNT` class the plan cites elsewhere). Recorded rather than silently dropped. **Consequence: every host-free phase (P0–P3) is complete**; P4–P6 remain, and §12 is rewritten accordingly — its two stated preconditions are now both met, and it recommends P6's head-less closed-loop scenario ahead of P4 because `match-client-unity` sits in `SHIM_EXCLUDED_ASMDEFS` and P4/P5 are therefore invisible to `tools/dotnet-ci`. Doc-only commit; no `.cs` changed, so no gate run. |
| 0.8 | 2026-07-24 | **AR pass over the P0 landing — 3 Medium fixed, doc Lows fixed (pass 1: 0H+2M+3L; pass 2: 0H+1M+0L; pass 3 clean → CONVERGED).** M (pass 1, `MatchClientDriver`): public `Driver.Log` aliased the live `List<T>` the sim thread appends to — a UI-thread read during a running match raced the appender; now every append + read is `_logLock`-guarded and `Log` returns a point-in-time snapshot copy (safe from any thread). M (pass 1, `ManagerCommandKind`/`ManagerCommand`/`ManagerCommandQueue`): `SetTeamTactic` was ordinal 0, so a `default(ManagerCommand)` silently mapped onto `SetTeamTactic(0, default(TeamTactic))` — staging a malformed (non-Balanced zero-value) tactic; added a `None = 0` sentinel (game kinds → 1/2/3), refused fail-loud at `Enqueue` and `Apply`. M (pass 2, `MatchClientDriver`): a command the engine refuses (bad index, sub over the cap / of an already-used or sent-off slot — all reachable via ordinary repeated UI subs; verified `SetPlayerTactic` / `SubstitutePlayer` fail loud) threw inside the pre-tick hook and killed the background pacing thread, silently freezing the match; the drain now isolates a failing command (dropped, not applied, not in `Log`, recorded in a new `FailedCommands` snapshot) so the batch and sim loop carry on (try/catch permitted on this presentation drain path per the streamer's carve-out). L: `ServiceOnce` doc caveat (intended for the paused/full-time path); `TickStampedCommand.AppliedTick` doc tightened (it is the pre-`RunTick` `CurrentTick` = tick N, the design's "top of tick N+1"). Locked by new tests (default-command reject at enqueue + apply; refused-command isolation with batch-continues + no-escape). No production behaviour change on the default/neutral path. |
| 0.7 | 2026-07-24 | **P0 host-free foundations + the P2 deterministic command channel LANDED** (the §12 "land P0–P2 host-free first" recommendation). New assemblies: host-free `src/match-client-core/` (`TacticalDirector.MatchClientCore` — references match-engine + match-viewer + deterministic-sim + tactical-instructions + player-database + project-constants) and Unity-only `src/match-client-unity/` (asmdef + README only; the P4–P6 render skin). Core files: `MatchClientConstants` (master-plan speed set {1,3,5,10} as [GT] scalars), `ILiveMatchMutations` (the closed live-mutator surface — the three stride-safe mutators + tick/match-ended reads; producer + consumer both specified, so not a phantom) + `MatchEngineMutations` pass-through adapter, `ManagerCommandKind`/`ManagerCommand`/`TickStampedCommand` (value-type command + log entry, one factory+Apply path per live mutator; no playback kind per §6.4), `ManagerCommandQueue` (lock-guarded FIFO; UI-thread Enqueue / sim-thread internal DrainInto), `MatchClientDriver` (the `Service` drain = the pre-tick hook body: FIFO apply on the sim thread, per-batch tick-stamp, sim-side post-`MatchEnded` drop, ReadOnlyCollection log view; holds no engine reference — receives the mutation surface per call, §4), `MatchSetup` (immutable boot config; both-or-neither squads) + `MatchSession` (composition root — builds/wires engine+streamer+driver, installs the drain as the streamer pre-tick hook, exposes read=frames / write=commands / `ServiceOnce`). **`match-viewer` changes (§Governs):** `LiveMatchStreamer` gains the optional set-once pre-tick hook + `ServiceOnce()` (both serialized by a new `_tickGate` so the hook never interleaves with a tick; browser viewer installs no hook — playback-only invariant preserved by construction) and `MaxLiveSpeedMultiplier` default 8 → 10 so 10× is deliverable. **CI-gate deliverable:** `generate_projects.py` gains `SHIM_EXCLUDED_ASMDEFS = {TacticalDirector.MatchClientUnity}` so the Unity-only assembly is not shim-compiled (verified: it is not generated; `match-client-core` + its tests are). Head-less tests: `ManagerCommandQueueTests` (FIFO, thread-safe enqueue, the exactly-three-game-kinds §6.4 lock), `MatchClientDriverTests` (FIFO apply-order, per-batch tick-stamp, post-`MatchEnded` drop, two-runs-same-sequence log determinism), `MatchSessionTests` (neutral build + off-tick `ServiceOnce` drain through a real engine logging at tick 0, GK-heading setup, both-or-neither squad guard). Deferred as planned: P1 richer frame, P3 view-state/camera/interp math, P4–P6 Unity skin, and the durable save-capture body that rides the `ServiceOnce` seam. |
| 0.1 | 2026-07-23 | Initial high-level implementation plan. Scopes the interactive Unity client (live rendering + input) as the next presentation surface above the `match-viewer` HTML-replay / live-browser floor. MVVM over the existing observer-neutral `LiveMatchStreamer` ViewModel; new engine-facing work confined to a deterministic, stride-committed manager-command channel (§6). Phased P0–P6 with P0–P3 host-free (shim-testable) and P4–P6 the cert-verified Unity skin. Not yet adversarially reviewed. |
| 0.2 | 2026-07-23 | **AR-1 (self-review, 3H+2M+1L, all resolved).** H-1: §5-P2 lumped playback pause/speed into the `ManagerCommandQueue` while §6.2 defined every queue command as an engine mutator — pause/speed are presentation-only and must never touch the digest (the browser viewer's core invariant); removed them from the queue, they stay on the streamer's playback surface (§4/§5-P2/§6.4). H-2: §5-P2 put the command drain *inside the shared* `LiveMatchStreamer.TickOnce()`, which would have given the browser viewer's streamer a live mutation path too, regressing its playback-only / disjoint-by-construction invariant; the drain now lives in a Unity-client-owned `MatchClientDriver` installed as an **optional pre-tick hook** — the shared streamer keeps zero mutation logic and the browser viewer supplies no hook (§4 diagram + prose, §5-P2). H-3: §2/§9 claimed live-input determinism while §6.3 leaned toward *deferring* the command journal, and the P6 acceptance test depended on the deferred mechanism — an internal contradiction; resolved by defining reproducibility against a **tick-stamped command log** that is a P2 deliverable (§6.1), settling Q1, and rewording §2 item-3 / §9 / R2–R3 to match (a live match is reproducible from the log, not from human intent). M-1: §6.3 never stated the drained-empty-queue-before-capture invariant (an enqueued-but-undrained command would be lost on restore) and ignored that `CaptureDurable*` throws when `EnableGkHeading` is on — both now addressed in §6.3. M-2: §3-2/§5-P2 mixed live-safe mutators with pre-kickoff/boot-only ones (`ConfigureSquads` throws once ticked, `MatchEngine.cs:1301`); split into a setup-phase set (P0 `MatchSetup`) and a live set (`SetTeamTactic`/`SetPlayerTactic`/`SubstitutePlayer` only), `SetManagerMode` dropped from the live queue. L-1: `LiveMatchStreamer` mischaracterized as "double-buffered" — corrected to "lock-guarded latest-frame handoff (same guarantee as the vol-4 double-buffer)" (§3-3). |
| 0.3 | 2026-07-23 | **AR-2 (self-review, 0H+0M+3L, all resolved) — CONVERGENCE.** Full re-read of the whole document; the three High + two Medium from AR-1 verified resolved with no regressions. L-1: §1 still listed "speed control" among manager input "fed back into the running simulation," which the tightened §6.4 (pause/speed never touch the sim) now contradicted — reworded to separate presentation-only speed/pause. L-2: the §4 ASCII diagram was misaligned/hard to follow after the driver was added — redrawn cleanly (streamer's pre-tick hook → driver's drain callback → engine mutators on the sim thread; frame → View; input → queue), and a note added that the hook receives the mutation surface as a parameter so the driver keeps no off-thread-callable engine reference (closes a latent thread-safety footgun). L-3: §5-P6 said "same-command runs" while §9 said "same tick-stamped log" — aligned P6 to "same tick-stamped command sequence." No new High or Medium found; the channel-placement, playback/mutation separation, log-based reproducibility definition, drained-empty-before-capture invariant, and lifecycle-split all hold under a fresh hostile read. Converged. |
| 0.4 | 2026-07-23 | **AR-3 (self-review, 1H+2M+1L, all resolved).** The first two passes hunted inside §6 (the command channel) and missed the assembly / CI-gate layer entirely. H-1: P0 named a single `src/match-client-unity/` assembly and P4 put the first-ever `MatchClientBehaviour : MonoBehaviour` render skin in it — but `tools/dotnet-ci/generate_projects.py:64` globs every `*.asmdef` under `src/` and compiles it against a shim with no rendering types, with no per-assembly exclusion; so P4 would either redden the whole-tree compile or (if the assembly were excluded) drop the host-free determinism core out of CI. Split into host-free `match-client-core` (P0–P3, shim-gated) + Unity-only `match-client-unity` (P4–P6), and made a `generate_projects.py` exclusion an explicit P0 deliverable (§Governs / §5 header / §5-P0 / §5-P4 / §8 / §4 diagram + command-path prose). M-1: §6.3 asserted "drained before capture" but never marshalled the durable capture onto the sim thread — a UI-thread `CaptureDurablePayload` would tear-read the single-threaded engine; §6.3 now routes save through a sim-thread request flag drained/captured by the pre-tick hook. M-2: §2 item-3's "reproducible from log + seed" was over-broad — the log is in-memory only (§11), so it holds only within an uninterrupted session; qualified item-3 for the save/restore case. L-1: §Governs omitted the `LiveMatchStreamer` pre-tick-hook modification and mislabeled the command channel as "on `MatchEngine`"; corrected. **AR-4 (pass over the AR-3 diff) — CONVERGENCE:** the full re-read caught one regression the AR-3 edit introduced — §5-P2 still called `MatchClientDriver` "(Unity-client-owned)" after §4/§5-P0/§Governs had moved it to host-free `match-client-core`, a contradiction in the "core new work" section; fixed §5-P2 + aligned the §3-7 shorthand ("Unity UI → driver → sim-thread engine"). No other High/Medium; the v0.2 history row's "Unity-client-owned" wording is left verbatim as a frozen record of AR-1's then-state. |
| 0.6 | 2026-07-24 | **AR-7 (self-review, 1H+2M+1L, all resolved) — verified against current `MatchEngine.cs`, not the prior passes' assumptions.** The first six passes never re-checked §6's engine facts against source; two had moved. H-1: save requests + post-end commands were serviced **only** by the pre-tick hook, which runs only while the sim is ticking — but the streamer pauses (playback) and auto-pauses at full time, so save-while-paused would hang and a completed match would be unsaveable (both against §2's save/restore done-criterion), and the structural root is that the hook's availability window (ticking-only) did not cover the states where its queued work accumulates. Added a `ServiceOnce()` streamer seam (one sim-thread drain-and-service pass **without advancing a tick**), invoked by the driver for save regardless of pause/ended state; the running path (pre-tick hook) and the paused path share one servicing routine (§4/§5-P0/§6.3/§Governs); §9 gains the paused/ended save-capture test. M-1: §6.3's `EnableGkHeading` bullet was stale, self-contradictory, and mis-cited — it claimed `CaptureDurableHeader`/`CaptureDurablePayload` "throw `NotSupportedException` while Gk-heading is on (`MatchEngine.cs:911,920`)" while its *own first bullet* listed **v18** as covering GK/heading save; verified against source: 911/920 are the `RestoreFromSnapshot` squad-provider fail-loud (capture is at 1938/1957 and does **not** throw), and `EnableGkHeading` documents v18 snapshot-safety since Phase 2. Rewrote the bullet — a flag-on match is saveable, no affordance gating, citation dropped. M-2: §6.2's "reject post-`MatchEnded` at enqueue" read a lagging frame, so a command could slip through in the end-of-match window and strand on the auto-paused sim; moved the authority to the sim side (drain + each mutator's live `_matchEnded` guard), with the UI check demoted to best-effort. L-1: §6.1 reproducibility invariant "seed + log" omitted the initial configuration — tightened to "same `MatchSetup` (seed + squads/tactics/manager config/GK-heading flag) + same log" here, §2 item-3, §5-P6, §9. **AR-8 (pass over the AR-7 diff) — CONVERGENCE:** full re-read; the `ServiceOnce()` seam is consistently referenced across §Governs/§4/§5-P0/§6.2/§6.3, the shared-servicing-routine claim removes the second-code-path hazard the fix could have introduced, the GK-heading correction no longer contradicts the v18 first bullet, and the sim-side post-end authority is consistent with the `ServiceOnce()`/drain discard. No new High/Medium. Converged. |
| 0.5 | 2026-07-23 | **AR-5 (self-review, 0H+2M+3L, all resolved).** The prior "converged" passes never checked the reused streamer's speed clamp against the stated speed set. M-1: done-item 2 requires a 10× control, but `LiveMatchStreamer.SetSpeedMultiplier` clamps to `MaxLiveSpeedMultiplier` (config default 8.0, `MatchViewerConstants.cs:90`) — 10× silently ran at 8×; added the config-cap raise to ≥ 10 as an explicit `match-viewer` change in §Governs / §2 item 2 / §5-P0. M-2: `MatchSession` was billed as the single composition root ("Unity host and any test drive it identically") but the `MatchClientDriver` + pre-tick-hook wiring — the determinism-bearing input path — was left ownerless; put the driver in the façade's charter (§5-P0), so the head-less P2 test drives the exact composition the host ships (§4 command-path updated to "installed by `MatchSession`"). L-1: §5 header now notes P1's accessors land in `match-engine`, not `match-client-core`. L-2: §6.2 now rejects commands enqueued after `MatchEnded` at enqueue (the auto-paused sim thread never drains them). L-3: §6.2 now pins the hook firing **inside `TickOnce()`** (not the pacing-loop wrapper), so the head-less P2 test genuinely exercises the drain. **AR-6 (pass over the AR-5 diff) — CONVERGENCE:** full re-read; the five fixes are internally consistent (the §2 ↔ §Governs ↔ §5-P0 speed-cap cross-refs align; §4 "installed by `MatchSession`" matches the new §5-P0 façade charter; the `MatchSetup` "manager config" addition also closes the prior field-list gap; the §6.2 additions do not conflict with §6.3). No new High/Medium. Converged. |
