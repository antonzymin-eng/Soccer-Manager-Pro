# Interactive Unity Client — High-Level Implementation Plan

> **Created:** 2026-07-23
> **Status:** DESIGN SUPPLEMENT (pre-promotion, pre-code) — high-level plan only; not yet
> adversarially reviewed, no section files, no numbered spec. Same governance class as
> `interactive-match-view-design.md` and `match-engine-design.md`.
> **Governs (when implemented):** a new `src/match-client-unity/` presentation assembly + the Unity
> project scene/prefab/host scaffolding; a new deterministic manager-command channel on
> `MatchEngine`. Presentation tooling on top of the match-engine composition root — like
> `match-viewer`, it observes the engine and (via §6) drives it only through pre-existing
> stride-committed public APIs.
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
inside the engine, and (b) accepts **manager input** — tactical changes, substitutions, speed
control — fed back into the running simulation. (a) is a richer View over the existing ViewModel;
(b) is the genuinely new architectural surface, because it is the first thing that mutates a live
match from the outside, and it must do so **without breaking determinism**.

## 2. Goal & definition of done

**Goal:** the master plan's Stage-1 Match View deliverable — "watch a simulated match" natively in
Unity, plus live tactical input — reachable from a minimal Main-Menu → Tactics → Match flow.

**Done when:**
1. A Unity scene boots a real `MatchEngine`, renders it live at 60 FPS (pitch + agents + ball +
   ball-height/possession/team cues + follow-ball camera), and shows a score/clock HUD.
2. Speed controls (Pause / 1× / 3× / 5× / 10×) work — the master plan's exact set.
3. The manager can change team/player tactics and make substitutions **mid-match**, and those
   changes take effect deterministically (same inputs + same seed ⇒ byte-identical match, and the
   match remains save/restore-clean through the existing snapshot path).
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
2. **Presentation never mutates the engine except through vetted, stride-committed APIs.** The
   existing public mutators — `SetTeamTactic`, `SetPlayerTactic`, `SubstitutePlayer`,
   `ConfigureManager`, `ConfigureSquads`, `EnableGkHeading` — already stage-then-commit at the AI
   stride boundary (FR-TI-027) or pre-kickoff. Input rides those; the client invents **no** new
   raw poke into engine state.
3. **Render/sim thread separation via the Presentation Proxy** (vol-4 §7): the sim owns the tick
   loop; the View reads a **double-buffered / lock-guarded latest frame**; the two never block each
   other. `LiveMatchStreamer` is already exactly this ViewModel — the Unity View reuses it rather
   than adding a second engine driver.
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
   this client *does* carry gameplay mutations. That channel must be in-process (Unity UI → engine),
   single-operator, and — if it is ever exposed over a socket — authenticated and separate from the
   playback stream. Conflating mutation with the bare loopback viewer server is forbidden (§6).

## 4. Architecture

MVVM, matching vol-4 §7. The key insight: **the Model and ViewModel already exist** — the Unity
client is a new **View** plus a new **input path back into the Model**.

```
 Model (Sim thread)                 ViewModel (thread-safe handoff)          View (Unity render thread)
 ┌───────────────────┐              ┌──────────────────────────────┐         ┌──────────────────────────┐
 │  MatchEngine       │   RunTick   │  LiveMatchStreamer            │  frame  │  MatchClientBehaviour     │
 │  (composition root │◀───────────▶│  - paces ticks (wall-clock)   │────────▶│  (MonoBehaviour host)     │
 │   of all 20 specs  │  observe    │  - lock-guarded latest frame  │         │  - pitch/agent/ball render│
 │   + #21..#27)      │             │  - pause/resume/speed         │         │  - follow-ball camera     │
 │                    │             │                               │         │  - UGUI HUD + tactics UI  │
 │  public mutators   │◀────────────┤  ManagerCommandQueue (NEW §6) │◀────────┤  input events             │
 │  (stride-committed)│  drain+apply │  - enqueue on UI thread       │         │                          │
 └───────────────────┘  at stride   │  - drained on sim thread      │         └──────────────────────────┘
                                     └──────────────────────────────┘
```

- **View is replaceable.** The browser viewer and the Unity client become two Views over one
  `LiveMatchStreamer`. Nothing engine-facing is Unity-specific; the Unity assembly is a skin.
- **Frame path (read):** unchanged and already observer-neutral — the Unity View calls
  `TryGetLatestFrame` (extended in §5-P1 to carry the extra cues Unity can show that the browser
  floor omitted: sent-off / booking / substitution state).
- **Command path (write):** the one new engine-facing surface. A `ManagerCommandQueue` accepts
  typed commands on the UI thread and the **streamer drains them on the sim thread at the
  tick/stride boundary**, applying each via the existing public mutator. This is what keeps input
  deterministic (§6).

## 5. Phased implementation plan

Each phase is independently shippable and independently testable. Phases P0–P3 are **host-free**
(compile + logic-test under the shim); P4–P6 are the **Unity-host skin** (verified at a cert run).
This ordering front-loads everything the host block does *not* prevent.

### P0 — Foundations & scaffolding (host-free)
- New `src/match-client-unity/` assembly (`TacticalDirector.MatchClientUnity`), asmdef referencing
  `match-engine` + `match-viewer` + `deterministic-sim` (+ `tactical-instructions`/`player-database`
  for command payload types). Tests asmdef alongside.
- `MatchClientConstants.cs` — the master plan's speed set (`{Pause, 1, 3, 5, 10}`), camera tuning,
  render-cue sizes ([GT], migrated onto `GameplayConfig` per the June-30 catalogue convention).
- A `MatchSession` façade: constructs/configures a `MatchEngine` + `LiveMatchStreamer` from a
  `MatchSetup` value (home/away squads, tactics, seed) — the single place that owns match lifecycle,
  so the Unity host and any test drive it identically. This is the "click Play Match" seam the
  browser design left to Stage-1 UI.

### P1 — Richer observation frame (host-free)
- Extend the live frame with the cues a native View can show that the browser floor skipped:
  per-agent booking/sent-off state, active-substitution markers, current restart/phase. Requires a
  small read-only extension to the `MatchEngine` observation surface (same pattern as v1.24/v1.32 —
  read-only value copies, **no** `SNAPSHOT_SCHEMA_VERSION` change, observer-neutrality re-locked).
- Do this before rendering so the render layer targets the final frame shape once.

### P2 — Deterministic manager-command channel (host-free) — **the core new work**
- `ManagerCommandQueue` + a closed set of typed commands (`SetTeamTactic`, `SetPlayerTactic`,
  `Substitute`, `SetManagerMode`, playback speed/pause). See §6 for the determinism design.
- Drain-and-apply hook inside `LiveMatchStreamer.TickOnce()` at the stride boundary, ahead of the
  AI phase, routing each to the existing public mutator.
- **Command journaling** into the match record so save/restore/replay stays byte-exact (§6.3).
- This phase is the one that carries real determinism risk and gets the heaviest test + adversarial
  focus — it is fully exercisable head-less (no rendering needed to prove input determinism).

### P3 — Client-side view state & stats (host-free)
- Frame interpolation math (render at 60 FPS between 10 Hz AI strides / 60 Hz physics — pure
  functions, unit-tested without Unity), follow-ball camera target math, and a minimal live-stats
  accumulator (possession %, shots, score) fed off the observation surface. All pure/testable.

### P4 — Unity render skin (host, cert-verified)
- `MatchClientBehaviour : MonoBehaviour` — the PlayerLoop host the project currently lacks
  (src/CLAUDE.md "WHAT IS NOT HERE YET"): owns `MatchSession`, reads `TryGetLatestFrame` each
  `Update`, drives rendering. Master plan Month 1-2: pitch (markings from the existing IFAB
  `[FIXED]` geometry catalogue), agent sprites (team color + jersey + has-ball/pressing/sent-off
  indicator), ball (sprite + shadow for height), follow-ball camera with smoothing/zoom.
- 2D-first (sprites, orthographic camera) per the master plan; 3D is a later polish pass.

### P5 — Unity UGUI shell (host, cert-verified)
- The master plan's four screens: **Main Menu** (New Demo Match), **Tactics Setup** (formation /
  team / player instructions — writing a `MatchSetup`), **Match View** (canvas + collapsible stats
  panel + the speed controls + in-match tactical-adjustment buttons wired to the P2 command queue),
  **Post-Match Report** (score + stats).
- The in-match tactical buttons are the first UI producers of P2 commands — the input half of "done".

### P6 — Integration, cert & closed-loop scenario
- A `#19 ScenarioRunner` cross-spec scenario: boot via `MatchSession`, inject a scripted command
  sequence through the queue, assert (a) two same-seed+same-command runs are digest-identical, and
  (b) save@N → restore → tick-to-N+K with the same commands == uninterrupted run. This locks input
  determinism at the composition level, head-less.
- On the pinned host: open the scene, run a live match, capture the FR-PO-052-class render-loop perf
  number, confirm 60 FPS, sign off the rendering half.

## 6. Deterministic manager-command channel (design detail)

This is the one part of the plan with genuine architectural risk, so it is specified further even
at this stage.

**6.1 Constraints.** Manager input arrives on the **UI thread at an arbitrary wall-clock moment**,
but the engine only accepts changes **at a tick/stride boundary** and must remain replay-exact. A
naive "call `SetTeamTactic` directly from the button handler" would (a) race the sim thread and
(b) apply at a nondeterministic tick, so two identical play-throughs would diverge.

**6.2 Mechanism.** Commands are **enqueued** (thread-safe) on the UI thread and **drained on the sim
thread** inside the streamer's `TickOnce`, at a fixed point relative to the tick — so a command
enqueued during rendering of tick *N* is deterministically applied at the top of tick *N+1*'s
stride. Each command carries only data that maps onto an existing public mutator; there is no path
to poke engine internals. The apply order within a drained batch is FIFO and stable.

**6.3 Replay/save integrity.** For a saved-and-restored match to re-tick identically, the commands
must be part of the record, keyed by the tick they were applied at. Two options, to be settled at
promotion (leaning toward the second):
- (a) **Command journal** alongside the snapshot: `(appliedTick, command)` list, replayed on
  restore. Minimal engine change; the snapshot already captures the *result* of a committed tactic
  (v9/v10 serialize active+pending `TeamTactic`/`PlayerTactic`), so a mid-match change is *already*
  restore-deterministic from a snapshot taken after it — the journal only matters for **replay from
  an earlier point**, which is exactly the rewind/replay use case.
- (b) Confirm the existing v9/v10 serialization already covers **live save/restore** (it does — the
  applied tactic is in the snapshot), and scope the journal as a **replay-only** artifact owned by
  a future replay/rewind feature, keeping P2 to "apply deterministically at the boundary" without a
  schema change. **This is the likely Stage-1 scope**: no `SNAPSHOT_SCHEMA_VERSION` bump, journal
  deferred with the rewind feature it serves.

**6.4 Boundary with the browser viewer.** The browser `LiveMatchServer` stays playback-only,
permanently. This command channel lives **in-process** (Unity UI → queue → sim thread). If a future
task wants remote input, that is a separate authenticated endpoint — never folded into the loopback
frame stream (§3-7).

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
host-free P0–P3 code so it remains shim-testable; the `MonoBehaviour` itself is verified only at a
cert run. This is also where the profiling-marker convention (src/CLAUDE.md §ProfilerMarker) first
gets a real render/Update loop to instrument.

## 9. Testing strategy

- **Host-free (shim, every push):** command-queue determinism (enqueue/drain ordering, apply-at-
  boundary), observer-neutrality re-lock for the extended frame, interpolation/camera/stat math unit
  tests, `MatchSession` lifecycle, and the P6 `ScenarioRunner` cross-spec scenario (same-seed+same-
  commands digest equality + save/restore-with-commands round-trip). **This proves the hard part —
  input determinism — without a Unity host.**
- **Host (cert run):** scene boot, 60 FPS render, live tactical input through the UI, post-match
  report; FR-PO-052-class render-loop perf capture on the pinned tuple.
- Follows the project's established split: logic is gated in CI; the rendering skin is human/cert
  verified, exactly as `certification-platform.md` already partitions determinism vs. host concerns.

## 10. Risks, dependencies & open questions

| # | Item | Disposition |
|---|---|---|
| R1 | **Unity host availability** for P4–P6. | Mitigated: the July-19 recert opened the pinned host; P0–P3 need no host, so work proceeds regardless. |
| R2 | **Input determinism** (the core risk). | Contained in P2/§6, fully head-less-testable; heaviest adversarial focus. |
| R3 | **Snapshot schema churn.** | Avoided in likely scope (§6.3-b): live save/restore already covered by v9/v10; journal deferred with rewind. Confirm at promotion. |
| R4 | **Scope creep into stats/career.** | Hard-fenced in §11; this client is Match-View-only. |
| R5 | **UGUI vs UI Toolkit.** | Master plan pins UGUI for Stage 1; revisit only if it becomes a real constraint. |
| Q1 | Is a command journal in-scope now, or deferred with rewind? | Recommend **deferred** (§6.3-b) — settle at promotion. |
| Q2 | 2D-first vs. 3D. | Recommend **2D-first** per master plan Month 1-2; 3D is a later polish spec. |
| D1 | Depends on: match-engine observation surface (exists), public mutation API (exists), `LiveMatchStreamer` (exists), pinned Unity host (exists). No upstream blockers for P0–P3. | — |

## 11. What this plan does NOT do

- No heatmaps / xG / PPDA / advanced-stats overlays (master plan §3.3 — separate Stage-1 stats
  spec).
- No season/career/transfer flow — the client boots a single demo match via `MatchSession`; wiring
  it into a career loop is separate Stage-1+ work.
- No audio, no art/animation polish, no 3D (2D-first).
- No networking / multiplayer / remote input (Stage 5+; §6.4).
- No new numbered spec **yet** — this is a design supplement; promotion to a numbered UI spec (the
  first member of the Stage-1 UI layer) follows the #21/#22/#27 precedent if/when the owner elects.

## 12. Recommended next step

Promote this note through one adversarial-review cycle to convergence (per project convention),
then land **P0–P2 host-free** first — the foundation + the deterministic command channel — since
that is the highest-risk, fully-testable core and needs no Unity host. The rendering skin (P4–P6)
follows once the input/determinism core is locked and a cert-host slot is scheduled.

## Version History

| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-07-23 | Initial high-level implementation plan. Scopes the interactive Unity client (live rendering + input) as the next presentation surface above the `match-viewer` HTML-replay / live-browser floor. MVVM over the existing observer-neutral `LiveMatchStreamer` ViewModel; new engine-facing work confined to a deterministic, stride-committed, replay-safe manager-command channel (§6). Phased P0–P6 with P0–P3 host-free (shim-testable) and P4–P6 the cert-verified Unity skin. Not yet adversarially reviewed. |
