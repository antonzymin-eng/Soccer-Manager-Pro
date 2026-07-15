# Interactive Match View — Design Note

> **Created:** 2026-07-15
> **Status:** Design — adversarial review in progress (see Version History)
> **Governs:** `src/match-viewer/LiveMatchFrame.cs`, `LiveMatchStreamer.cs`, `LiveMatchServer.cs`
> (new); `MatchEngine.cs` (3-property observation-surface extension); `MatchViewerConstants.cs`
> (new `[GT]` region). Presentation tooling, like the existing `match-viewer` assembly — not a
> numbered spec.

## 1. Problem

The only match-viewing surface today (`docs/tracking/match-engine-design.md` §5 Phase F +
src/CLAUDE.md v1.85+) is `MatchReplayRecorder` + `HtmlReplayExporter`: it ticks a whole match to
completion first, then exports a static, self-contained HTML file the user opens **after** the
match is over. There is no way to watch a match while it runs.

**Goal:** a live-updating viewer — the user starts a match and watches it progress in a browser
in (approximately) real time, not a post-hoc export. Full in-Unity rendering is out of reach in
this environment (no Unity host — see root `CLAUDE.md` OPEN ISSUES "Unity engine version bump":
recertification and the interactive client both wait on host access). The deliverable here is the
"at minimum a live-updating viewer" floor the task explicitly allows: a background thread paces a
real `MatchEngine` at wall-clock speed, and a minimal local HTTP server streams the current frame
to a browser page that polls and redraws.

## 2. Scope

**In scope:**
- Real-time-paced tick loop over an existing `MatchEngine` (observer only — never mutates engine
  state beyond calling `RunTick()`, exactly like `MatchReplayRecorder`).
- A thread-safe "latest frame" handoff (ball, agent positions, possession, score, tick, match-ended).
- A minimal loopback-only HTTP server (hand-rolled over `TcpListener` — no new package
  dependencies, compiles under both Unity Mono and the `tools/dotnet-ci` netstandard2.1 shim)
  serving: an HTML/JS viewer page, a polled JSON frame endpoint, and a playback-control endpoint
  (pause / resume / speed).
- Three new trivial read-only properties on `MatchEngine` (`HomeScore` / `AwayScore` /
  `MatchEnded`) — the observation-surface pattern `BallView`/`AgentView`/`PossessingAgentId`
  already established (v1.24), extended to cover state a live HUD needs that the replay viewer
  never did (a saved replay's HTML already knows the final score is irrelevant to render live).

**Out of scope (explicit non-goals):**
- Real Unity/in-engine rendering — blocked on Unity host access (existing OPEN ISSUE); this is a
  browser-based stand-in, same class of deliverable as the existing HTML replay exporter.
- Any gameplay-mutating control surface. The HTTP server is playback-control only (pause / resume /
  speed) — it must never become a channel for tactical changes, substitutions, etc. That stays a
  separate, authenticated, Stage-1+ UI concern; conflating it with a bare local dev server would be
  a real security/architecture mistake, not just scope creep.
- Scrubbing / rewind (this is a *live* view — there is nothing to scrub to yet).
- Multi-match / multi-tenant serving, authentication, remote (non-loopback) access.
- Card/substitution/sent-off visual cues (no public accessor for `_isSentOff` etc. exists; adding
  one is a follow-up, not required for a first live view — see §7).
- A UI entry point that starts a "real match" for an end user — Stage 0 has no MonoBehaviour /
  PlayerLoop host (src/CLAUDE.md "WHAT IS NOT HERE YET") and no season/career flow reaches this
  code yet. This design delivers the mechanism (a `MatchEngine` someone already booted can be
  observed live); wiring "click Play Match" to it is Stage 1+ UI work, same boundary the existing
  replay exporter already sits behind.

## 3. Architecture

```
 caller (test / future UI)
    │  constructs + configures MatchEngine (as today)
    ▼
 LiveMatchStreamer(engine)            — owns the engine; the ONLY thing that ever calls
    │  Start() spawns a background        engine.RunTick(). Paces ticks at wall-clock speed
    │  Thread; TickOnce() is the          (Stopwatch-based, drift-corrected). Auto-pauses once
    │  pure per-tick capture step,         MatchEnded flips true. Exposes a lock-protected
    │  callable directly by tests.         TryGetLatestFrame + Pause/Resume/SetSpeed.
    ▼
 LiveMatchFrame (immutable snapshot: tick, ball, agent positions, possession, score, matchEnded)
    ▲
    │  reads only through the streamer's synchronized surface — NEVER touches MatchEngine directly
 LiveMatchServer(streamer, port)      — TcpListener on 127.0.0.1 only. GET / (HTML+JS page),
                                          GET /frame (JSON snapshot), GET /control?... (pause/
                                          resume/speed). One short-lived thread per connection,
                                          no keep-alive, fail-loud on bind, silent-and-continue on
                                          a single malformed request.
    ▲
    │  polls every ~50 ms (20 Hz) via fetch(), redraws <canvas>
 browser tab
```

`LiveMatchServer` never references `MatchEngine` — only `LiveMatchStreamer`. This is the load-bearing
invariant that keeps "the HTTP server" and "the thing that can mutate the match" disjoint: even a
future control-endpoint bug can, at worst, corrupt playback pacing, never game state.

## 4. `MatchEngine` observation-surface extension

Three trivial read-only properties, same section as the existing v1.24 surface:

```csharp
public int HomeScore => _goals[0];
public int AwayScore => _goals[1];
public bool MatchEnded => _matchEnded;
```

No behaviour change, no new state, no `SNAPSHOT_SCHEMA_VERSION` bump (both backing fields are
already serialized). Bump `MatchEngine.cs` to v1.32.

## 5. `LiveMatchFrame`

A new small immutable type (not a reuse of `ReplayFrame` — that type is already
adversarially-reviewed and consumed by the frozen post-hoc exporter/replay pipeline; extending it
with score/matchEnded fields it doesn't need would be gratuitous churn on a settled surface).
Fields: `Tick` (ulong), `BallPosition` (Vector3), `PossessingAgentId` (int), `AgentPositions`
(Vector2[SQUAD_SIZE], fresh array per frame — same hand-over convention as `ReplayFrame`: the
streamer never retains or mutates a captured array after construction), `HomeScore`/`AwayScore`
(int), `MatchEnded` (bool).

## 6. `LiveMatchStreamer`

- Constructor takes an already-constructed/configured `MatchEngine` (pre-kickoff, same convention
  as `MatchReplayRecorder`'s pre-configured-engine overload) + optional ticks-per-second (default
  `DeterministicSimConstants.PHYSICS_TICK_HZ` = 60).
- `TickOnce()`: `engine.RunTick()` then captures a `LiveMatchFrame` and swaps it into a
  lock-protected field. Pure, synchronous, directly callable by tests — no threading, no timing.
  If the captured frame's `MatchEnded` is true, sets an internal `_autoPaused` flag.
- `Start()` spawns one background `Thread` (`IsBackground = true`) running a drift-corrected pacing
  loop: track `startWallClock` via `Stopwatch`, target wall time for tick *n* is
  `n × (1000 / (ticksPerSecond × speedMultiplier))` ms; sleep `max(0, target − elapsed)` each
  iteration (never accumulates drift from per-iteration `Thread.Sleep` rounding). Skips ticking
  (short poll sleep instead) while paused or auto-paused. `Start()` is a no-op if already started;
  throws `InvalidOperationException` on a second `Start()` after `Stop()` (a streamer is single-use,
  matching `MatchEngine`'s own single-match-per-instance convention).
- `Stop()` signals the loop to exit and `Join()`s the thread; idempotent (no-op if never started or
  already stopped).
- `TryGetLatestFrame(out LiveMatchFrame frame)`: `false` before the first `TickOnce()` runs.
- `Pause()` / `Resume()` / `SetSpeedMultiplier(float)` (clamped to
  `[MatchViewerConstants.MinLiveSpeedMultiplier, MaxLiveSpeedMultiplier]`, NaN/Infinity rejected —
  the project's `!(x > 0f)` NaN-gate pattern) — all under the same lock as the frame swap.
- `IsPaused` (true if user-paused OR auto-paused at full time) — the server's `/control` reads this
  to render pause/resume button state correctly to a client that connects after full time.

**Observer-neutrality**: `TickOnce()` calls exactly `engine.RunTick()` plus read-only observation
getters — identical to `MatchReplayRecorder.CaptureFrame`'s existing, already-tested contract. A
streamed match's final `CurrentSnapshotDigest` is therefore digest-identical to an unobserved run
with the same seed (locked by a test mirroring `MatchViewerTests`' existing observer-neutrality
lock).

## 7. `LiveMatchServer`

- Constructor: `LiveMatchServer(LiveMatchStreamer streamer, int port = MatchViewerConstants.LiveServerDefaultPort)`.
  `port = 0` binds an OS-chosen ephemeral port (used by tests); `Port` property exposes the bound
  port after `Start()`.
- Binds `new TcpListener(IPAddress.Loopback, port)` — **loopback only**, never `IPAddress.Any`; this
  is a local spectator tool, not a service, and binding wide would silently expose match state (and
  the control endpoint) to the LAN.
- `Start()`: `listener.Start()` (fail-loud on bind failure — a caller passing an in-use port needs
  to know), spawns an accept-loop thread. Each accepted `TcpClient` gets its own short-lived thread:
  read one HTTP request line (bounded read — refuse anything over a small max-header-bytes cap
  rather than block forever on a slow/hostile client), route, write one HTTP/1.1 response with an
  exact `Content-Length` and `Connection: close`, close the socket. No headers are parsed beyond the
  request line (no request body is ever consumed by any of the three routes, so none is needed).
- Routes:
  - `GET /` → the HTML/JS viewer page (canvas pitch rendering reusing `HtmlReplayExporter`'s
    marking-geometry constants; polls `/frame` every `MatchViewerConstants.LivePollIntervalMs`
    (default 50 ms) via `fetch()`, redraws, updates score/clock HUD text, and posts to `/control`
    on pause/resume/speed button clicks).
  - `GET /frame` → JSON snapshot of the latest `LiveMatchFrame` (+ the static roster metadata —
    team id / goalkeeper flag per index — included in every response for simplicity; it is a
    handful of ints/bools, not worth a separate endpoint). Hand-rolled writer, `InvariantCulture`,
    fail-loud (`InvalidOperationException`) if any coordinate is non-finite — mirrors
    `HtmlReplayExporter`'s existing NaN-gate; on-pitch invariants elsewhere in the engine mean this
    should never fire, but NaN/Infinity are not valid JSON tokens so silently emitting them would
    hand the browser a document it cannot even parse.
  - `GET /control?action=pause|resume` and `GET /control?action=speed&value=<float>` → mutate the
    streamer, respond with the small current-state JSON `/frame`'s header uses. Unknown `action`,
    unparsable `value`, or an out-of-range speed → `400 Bad Request` (not a crash).
  - Anything else → `404 Not Found`.
- Per-connection handling is wrapped in try/catch — one malformed request (a health-checker, a
  browser prefetch, a truncated connection) must not take down the accept loop or crash the process;
  this is presentation/networking code, not the 60 Hz game loop, so `try`/`catch` here does not
  conflict with FR-CS-069 (that rule targets per-frame *simulation* inner loops).
- `Stop()`: `listener.Stop()` (which unblocks the accept thread's blocking `AcceptTcpClient` with a
  `SocketException` the accept loop catches and treats as the shutdown signal, not an error to log),
  joins the accept thread; idempotent.

**Wire format is presentation-only** — like `HtmlReplayExporter`'s output, the JSON schema is NOT a
determinism-pinned wire format; it may change freely as the viewer UI evolves.

## 8. Constants (`MatchViewerConstants.cs` new `[GT]` region)

| Constant | Default | Purpose |
|---|---|---|
| `LiveServerDefaultPort` | 8787 | Default loopback port |
| `LivePollIntervalMs` | 50 | Browser `/frame` poll cadence |
| `MinLiveSpeedMultiplier` | 0.25 | Slowest allowed playback pace |
| `MaxLiveSpeedMultiplier` | 8.0 | Fastest allowed playback pace |
| `MaxHttpRequestLineBytes` | 8192 | Abandon a connection whose request line exceeds this (abuse/hang guard) |

## 9. Determinism & thread-safety analysis

1. **Single writer to the engine.** Only the streamer's background thread ever calls
   `engine.RunTick()` or any engine method beyond the read-only observation getters. The server's
   connection threads only ever call `LiveMatchStreamer` methods (`TryGetLatestFrame`,
   `Pause`/`Resume`/`SetSpeedMultiplier`), never the engine. This is enforced by *construction*
   (`LiveMatchServer` holds no `MatchEngine` reference at all), not just by convention.
2. **Frame handoff race.** The captured `LiveMatchFrame` is immutable after construction and is
   swapped into the shared field under the same `lock` that readers use — a reader that acquires
   the lock always sees either the previous complete frame or the new complete frame, never a
   partially-written one.
3. **Control-state race.** `Pause`/`Resume`/`SetSpeedMultiplier` mutate under the same lock as the
   pacing loop reads them each iteration — last-writer-wins is an accepted outcome for a
   single-operator local dev tool (two browser tabs fighting over the pause button is a UX
   footgun, not a correctness bug).
4. **Shutdown races.** `TcpListener.Stop()` while `AcceptTcpClient()` is blocked throws
   `SocketException` in the accept thread by design (documented BCL behaviour) — the accept loop
   must special-case that as the clean-shutdown signal, not surface it as an error.
5. **No effect on match determinism.** The streamer's pacing (`Thread.Sleep`) affects only
   wall-clock cadence, never tick content — identical reasoning to `MatchReplayRecorder`'s existing
   observer-neutrality guarantee, extended with a digest-equality test.

## 10. Test plan

- `LiveMatchStreamerTests.cs`: `TryGetLatestFrame` false before first `TickOnce`; deterministic
  N-tick sequence via direct `TickOnce()` calls (no wall-clock dependency) matches an unobserved
  N-tick run's digest; `MatchEnded` frame auto-pauses; speed-multiplier NaN/Infinity/out-of-range
  rejected; `Start()` twice is a no-op / `Start()` after `Stop()` throws; minimal threaded
  start/stop lifecycle smoke test (tolerant of wall-clock — asserts no exception and no deadlock,
  not an exact tick count).
- `LiveMatchServerTests.cs`: bind on port 0, real `TcpClient` round-trips for `GET /` (200, html),
  `GET /frame` (200, json, roster length matches SQUAD_SIZE, tick present), `GET /control?action=pause`
  then `/frame` reflects paused state, `GET /control?action=speed&value=2` then rejects
  `value=-1`/`value=nan` with 400, unknown path → 404, oversized request line → connection closed
  without crashing the server (next request on a fresh connection still succeeds), `Stop()` then a
  fresh connection attempt is refused.
- `MatchEngine` property tests: `HomeScore`/`AwayScore` match `TestOnly_Goals` after a goal
  (extend `MatchEngineGoalTests.cs`); `MatchEnded` matches `TestOnly_MatchEnded` at full time
  (extend `MatchEngineMatchFlowTests.cs`).

Full `dotnet test` is not runnable in this environment (no SDK reachable — see attempted `apt-get
install dotnet-sdk-8.0`/`10.0`, both 404 against the mirror). Verified instead by the project's
established fallback: exhaustive manual review of every touched file (not just the diff) in place
of the gate, documented in the code-review pass below.

## Version History

| Version | Date | Notes |
|---|---|---|
| 0.1 | 2026-07-15 | Initial draft. |
| 0.2 | 2026-07-15 | AR-1 (self-review, 0H+0M+4L, all resolved): (1) initial draft left the control endpoint's blast radius unstated — added the explicit non-goal in §2 that it must never become a gameplay-mutation channel, and the §3 "disjoint by construction" invariant (`LiveMatchServer` holds no `MatchEngine` reference). (2) initial draft did not address pacing drift — added the Stopwatch/target-wall-time drift-correction scheme to §6 (naive per-tick `Thread.Sleep` would accumulate seconds of drift over a 90-minute / 324,000-tick match). (3) initial draft didn't say what happens at full time — added the auto-pause behaviour to §6/§9 (otherwise the streamer spins forever after `_matchEnded`, burning a thread for no visible progress). (4) initial draft's HTTP handling didn't bound the request-line read — added `MaxHttpRequestLineBytes` (§8) and the abuse/hang guard note (§7) since a hand-rolled line reader with no cap is a trivial local DoS via a client that never sends CRLF. |
| 0.3 | 2026-07-15 | AR-2 (self-review, 0H+0M+2L, all resolved) — CONVERGENCE. L-1: §7 didn't say why `try`/`catch` per connection is acceptable given the project's FR-CS-069 ban on `try`/`catch` in per-frame inner loops; added the explicit scoping note (this is networking/presentation code, not the 60 Hz simulation loop the rule targets). L-2: §5 didn't explain why a new `LiveMatchFrame` type was introduced instead of extending the existing `ReplayFrame` — added the rationale (avoid churn on an already-reviewed, frozen replay surface). Full re-read found nothing further: the loopback-only bind, the disjoint-by-construction server/engine separation, the drift-correction scheme, the auto-pause-at-full-time behaviour, and the fail-loud/silent-continue split between bind errors and per-request errors all still hold under a fresh read. Proceeding to implementation. |
| 0.4 | 2026-07-15 | **Code adversarial review (post-implementation), two passes, converged.** Pass 1 (2H+2M+1L, all fixed): H-1 — both `LiveMatchStreamer.Start()` and `LiveMatchServer.Start()` originally flipped their running-state flag to true *inside* their lifecycle lock but only created/assigned the background thread *after* releasing it; a `Stop()` racing into that window would capture a still-null thread reference and call `.Join()` on it (NRE in `LiveMatchStreamer`; a silent no-op via `?.Join()` in `LiveMatchServer` that left a freshly-spawned accept thread orphaned against an already-stopped listener). Fixed identically in both: thread creation, the state flip, and `.Start()` now all happen inside one lock acquisition. H-2 — two missing `using TacticalDirector.MatchEngine;` directives (`LiveMatchStreamer.cs`, `LiveMatchServer.cs`) referencing `MatchEngine.MatchEngineConstants`/`MatchEngine.MatchEngine` without the namespace import — would not have compiled; fixed by adding the directive (`LiveMatchStreamer.cs`) and, in `LiveMatchServer.cs`, by instead exposing `PitchLengthM`/`PitchWidthM` on `LiveMatchStreamer` so the server's own "never references MatchEngine" invariant (§3) stays literally true rather than importing the namespace just to read two constants. M-1 — `LiveMatchServer`'s constructor tried to default a port parameter to `MatchViewerConstants.LiveServerDefaultPort`, a config-resolved `static readonly` value, which cannot be a C# compile-time default parameter (the exact constraint `MatchReplayRecorder`'s own AR-2 M-3 fix already hit in this assembly); split into an explicit no-port overload, matching the established precedent. M-2 — the planned auto-pause test needed to force a real `MatchEngine` into `MatchEnded = true`, which is reachable only via ~324,000 real ticks or the engine's own `TestOnly_CheckMatchFlowTransitions` seam — `internal` and granted only to `TacticalDirector.MatchEngine.Tests`, not this assembly's test project; rather than widen a different module's `InternalsVisibleTo` for one test, `LiveMatchStreamer.TickOnce()` was split into `TickOnce()` (ticks + captures) and internal `ApplyCapturedFrame(LiveMatchFrame)` (stores + applies the auto-pause rule), so the auto-pause *decision* is unit-testable against a hand-built frame. L-1 — `LiveMatchServer.AcceptLoop`'s catch narrowed to `SocketException`/`ObjectDisposedException`; widened to catch `Exception` generically, since `TcpListener.Stop()` can also surface `InvalidOperationException` depending on exact timing and an unhandled exception on a background thread terminates the process — every exception from `AcceptTcpClient` in this specific loop means "shut down," never a per-connection condition (those stay isolated inside `HandleConnection`'s own broad catch). Pass 2 (0H+0M+0L) — CONVERGENCE: re-verified the JSON field names emitted by `AppendFrameFields`/`BuildFrameJson` against every field the embedded JS reads (`hasFrame`/`paused`/`speed`/`roster[].team`/`roster[].gk`/`tick`/`possession`/`homeScore`/`awayScore`/`matchEnded`/`ball`/`agents`) — exact match; re-verified the embedded verbatim JS string contains no raw `"` (would have terminated the C# string early) and that all HTML tags open/close in balance; re-verified `SetSpeedMultiplier`'s NaN/Infinity rejection is reachable through the HTTP layer (`float.TryParse` under `NumberStyles.Float` + `CultureInfo.InvariantCulture` accepts the literal tokens `"NaN"`/`"Infinity"`, both then rejected by the `!(x > 0f)` NaN-gate / `IsInfinity` checks); re-verified no deadlock path between `LiveMatchStreamer`'s and `LiveMatchServer`'s two independent locks (never nested, never acquired in reverse order); re-verified `LiveMatchServer` genuinely never references `TacticalDirector.MatchEngine` types (grep-confirmed — only `LiveMatchStreamer` does). Nothing further found. |
