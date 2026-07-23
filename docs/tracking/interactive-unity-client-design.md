# Interactive Unity Client — High-Level Implementation Plan

> **Created:** 2026-07-23
> **Status:** DESIGN SUPPLEMENT (pre-promotion, pre-code) — high-level plan only; adversarial
> review CONVERGED (AR-1 3H+2M+1L → AR-2 0H+0M+3L, see Version History), no section files, no
> numbered spec. Same governance class as `interactive-match-view-design.md` and
> `match-engine-design.md`.
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
2. Speed controls (Pause / 1× / 3× / 5× / 10×) work — the master plan's exact set.
3. The manager can change team/player tactics and make substitutions **mid-match**; each change is
   applied at a deterministic tick and recorded in a **tick-stamped command log**, so a match is
   byte-identically reproducible **from that log + the seed** — *not* from human intent, since the
   wall-clock timing of a live click is not itself reproducible (§6.1) — and the match remains
   save/restore-clean through the existing snapshot path.
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
   this client *does* carry gameplay mutations. That channel must be in-process (Unity UI → engine),
   single-operator, and — if it is ever exposed over a socket — authenticated and separate from the
   playback stream. Conflating mutation with the bare loopback viewer server is forbidden (§6).

## 4. Architecture

MVVM, matching vol-4 §7. The key insight: **the Model and ViewModel already exist** — the Unity
client is a new **View** plus a new **input path back into the Model**.

```
 Model (sim thread)        ViewModel: LiveMatchStreamer          View + driver (Unity)
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
- **Command path (write):** the one new engine-facing surface, owned by the Unity-client `MatchClientDriver`
  — **not** the shared streamer. The driver's `ManagerCommandQueue` accepts typed **game** commands on the
  UI thread; the driver's drain callback (installed as the streamer's pre-tick hook) applies them on the sim
  thread at the tick boundary via the live mutators, and records each in the tick-stamped log. Playback
  pause/speed are **not** commands — they stay on the streamer's own playback surface (§6.4).

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
- `ManagerCommandQueue` + a **closed set of typed game commands** — `SetTeamTactic`,
  `SetPlayerTactic`, `Substitute` — each mapping onto exactly one **live, stride-safe** engine
  mutator. Playback pause/speed are **not** in this queue (presentation-only; they stay on the
  streamer's existing playback surface, §6.4). Pre-kickoff/boot mutators (`ConfigureSquads`,
  `ConfigureManager`, `EnableGkHeading`) are **not** here either — they belong to P0 `MatchSetup`
  (§3-2). No `SetManagerMode` live command: flipping Human⇄AI mid-match has no verified engine path.
- A **`MatchClientDriver`** (Unity-client-owned) owns the queue and a drain callback. The callback is
  installed as `LiveMatchStreamer`'s **optional pre-tick hook**, invoked on the sim thread at the top
  of each tick ahead of the AI phase; the shared streamer itself contains **no** mutation logic and
  the browser viewer supplies no hook, so its playback-only invariant is preserved by construction
  (§4). See §6 for the determinism design.
- Each drained command is written to a **tick-stamped command log** keyed by its applied tick — the
  mechanism that makes a live match replay-reproducible (§6.3). **This log is a P2 deliverable, not a
  deferral**; only the broader on-disk replay/rewind *feature* that consumes it stays deferred (§11).
- Highest determinism risk, heaviest test + adversarial focus, and **fully exercisable head-less**: a
  test enqueues commands and drives `TickOnce()` directly, proving command apply-tick and drain
  ordering without a Unity host. (The live wall-clock path is reproducible only *via* the log, not
  from human intent — §6.1.)

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
- A `#19 ScenarioRunner` cross-spec scenario: boot via `MatchSession`, inject a scripted
  tick-stamped command sequence through the queue, assert (a) two runs with the same seed + same
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
byte-identical given *the same seed + the same log*. This is why the log is a P2 deliverable, not an
optional add-on — without it there is nothing to replay from, and the P6 acceptance test (§5-P6)
would have no defined "the same commands." A naive "call `SetTeamTactic` from the button handler"
fails on both axes: it races the sim thread **and** records no applied tick.

**6.2 Mechanism.** Commands are **enqueued** (thread-safe) on the UI thread and **drained on the sim
thread** by the `MatchClientDriver`'s pre-tick hook (§4), at a fixed point at the top of the tick
ahead of the AI phase — a command enqueued during rendering of tick *N* is applied at the top of
tick *N+1*. Each command carries only data that maps onto a **live** engine mutator (§3-2); there is
no path to poke engine internals or to invoke a pre-kickoff/boot mutator. Apply order within a
drained batch is FIFO and stable. Every applied command is appended to the tick-stamped log with the
tick it was applied at.

**6.3 Save / restore integrity.** Two distinct guarantees, both required by §2's done-criteria:
- **Live save → restore (from the current point):** covered by the **existing** snapshot
  serialization — a committed tactic (v9/v10 `TeamTactic`/`PlayerTactic`), a substitution
  (`_activeBenchSlot` + counts + on-pitch attributes), and manager/GK-heading state (v13/v18) are all
  already in the snapshot, so restoring from a snapshot taken *after* a change re-ticks identically
  **with no `SNAPSHOT_SCHEMA_VERSION` bump**. **Invariant (new):** a durable capture may be taken
  **only at a tick boundary with the command queue fully drained** — a command enqueued-but-not-drained
  at capture time would be silently lost on restore. The driver enforces this by draining before it
  permits a capture.
- **Replay from an earlier point (rewind):** requires the tick-stamped log (post-snapshot commands
  are not in an earlier snapshot). P2 *produces* the log; the on-disk persistence + scrub-back
  *feature* that consumes it is deferred (§11) — but the in-memory log itself is not deferred.
- **`EnableGkHeading` interaction:** `CaptureDurableHeader`/`CaptureDurablePayload` throw
  `NotSupportedException` while Gk-heading is on (`MatchEngine.cs:911,920`). Since `EnableGkHeading`
  is a P0 setup-only toggle (§3-2), the client either leaves it off for a saveable match or **disables
  the in-match save affordance when it is on** — the plan does not silently produce an unsaveable match.

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
host-free P0–P3 code so it remains shim-testable; the `MonoBehaviour` itself is verified only at a
cert run. This is also where the profiling-marker convention (src/CLAUDE.md §ProfilerMarker) first
gets a real render/Update loop to instrument.

## 9. Testing strategy

- **Host-free (shim, every push):** command-queue determinism (enqueue/drain ordering, apply-tick,
  FIFO batch), the drained-empty-queue-before-capture invariant (§6.3), observer-neutrality re-lock
  for the extended frame, interpolation/camera/stat math unit tests, `MatchSession` lifecycle, and
  the P6 `ScenarioRunner` cross-spec scenario (same-seed + **same tick-stamped log** ⇒ digest
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

Promote this note through one adversarial-review cycle to convergence (per project convention),
then land **P0–P2 host-free** first — the foundation + the deterministic command channel — since
that is the highest-risk, fully-testable core and needs no Unity host. The rendering skin (P4–P6)
follows once the input/determinism core is locked and a cert-host slot is scheduled.

## Version History

| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-07-23 | Initial high-level implementation plan. Scopes the interactive Unity client (live rendering + input) as the next presentation surface above the `match-viewer` HTML-replay / live-browser floor. MVVM over the existing observer-neutral `LiveMatchStreamer` ViewModel; new engine-facing work confined to a deterministic, stride-committed manager-command channel (§6). Phased P0–P6 with P0–P3 host-free (shim-testable) and P4–P6 the cert-verified Unity skin. Not yet adversarially reviewed. |
| 0.2 | 2026-07-23 | **AR-1 (self-review, 3H+2M+1L, all resolved).** H-1: §5-P2 lumped playback pause/speed into the `ManagerCommandQueue` while §6.2 defined every queue command as an engine mutator — pause/speed are presentation-only and must never touch the digest (the browser viewer's core invariant); removed them from the queue, they stay on the streamer's playback surface (§4/§5-P2/§6.4). H-2: §5-P2 put the command drain *inside the shared* `LiveMatchStreamer.TickOnce()`, which would have given the browser viewer's streamer a live mutation path too, regressing its playback-only / disjoint-by-construction invariant; the drain now lives in a Unity-client-owned `MatchClientDriver` installed as an **optional pre-tick hook** — the shared streamer keeps zero mutation logic and the browser viewer supplies no hook (§4 diagram + prose, §5-P2). H-3: §2/§9 claimed live-input determinism while §6.3 leaned toward *deferring* the command journal, and the P6 acceptance test depended on the deferred mechanism — an internal contradiction; resolved by defining reproducibility against a **tick-stamped command log** that is a P2 deliverable (§6.1), settling Q1, and rewording §2 item-3 / §9 / R2–R3 to match (a live match is reproducible from the log, not from human intent). M-1: §6.3 never stated the drained-empty-queue-before-capture invariant (an enqueued-but-undrained command would be lost on restore) and ignored that `CaptureDurable*` throws when `EnableGkHeading` is on — both now addressed in §6.3. M-2: §3-2/§5-P2 mixed live-safe mutators with pre-kickoff/boot-only ones (`ConfigureSquads` throws once ticked, `MatchEngine.cs:1301`); split into a setup-phase set (P0 `MatchSetup`) and a live set (`SetTeamTactic`/`SetPlayerTactic`/`SubstitutePlayer` only), `SetManagerMode` dropped from the live queue. L-1: `LiveMatchStreamer` mischaracterized as "double-buffered" — corrected to "lock-guarded latest-frame handoff (same guarantee as the vol-4 double-buffer)" (§3-3). |
| 0.3 | 2026-07-23 | **AR-2 (self-review, 0H+0M+3L, all resolved) — CONVERGENCE.** Full re-read of the whole document; the three High + two Medium from AR-1 verified resolved with no regressions. L-1: §1 still listed "speed control" among manager input "fed back into the running simulation," which the tightened §6.4 (pause/speed never touch the sim) now contradicted — reworded to separate presentation-only speed/pause. L-2: the §4 ASCII diagram was misaligned/hard to follow after the driver was added — redrawn cleanly (streamer's pre-tick hook → driver's drain callback → engine mutators on the sim thread; frame → View; input → queue), and a note added that the hook receives the mutation surface as a parameter so the driver keeps no off-thread-callable engine reference (closes a latent thread-safety footgun). L-3: §5-P6 said "same-command runs" while §9 said "same tick-stamped log" — aligned P6 to "same tick-stamped command sequence." No new High or Medium found; the channel-placement, playback/mutation separation, log-based reproducibility definition, drained-empty-before-capture invariant, and lifecycle-split all hold under a fresh hostile read. Converged. |
