# Path to Playable — Implementation Roadmap

> **Created:** July 25, 2026
> **Status:** ROADMAP (meta-planning — the same governance class as
> `management-layer-spec-roadmap.md`). This document designs no system. It sequences **already-approved
> specifications and already-converged design supplements** into the shortest defensible path from
> today's state to a build a person can sit down and play. It changes no `SPEC_INDEX.md` row and
> opens no new numbered spec.
> **Purpose:** The companion to `management-layer-spec-roadmap.md`. That document answers *"which
> specs to author, in what order."* This one answers *"which code to land, in what order, to reach a
> playable build"* — and records the hard constraints that ordering has to respect.
> **Scope boundary:** implementation sequencing only. Where a work item needs governance that does
> not exist yet, this document names the gap and the **minimum** governance that closes it (§6) —
> it does not pre-empt that authoring.

---

## 0. The finding that motivates this document

The project has run a specification-first strategy to a measurable position:

| Layer | Specified | Implemented |
|---|---|---|
| Match engine + tactical layer (#1–#26) | ✅ 41 approved specs | ✅ ~110k lines, 321 engine tests, deterministic save/restore/season-save |
| Squad data #27 / progression #28 | ✅ | #27 built; **#28 T0 only** (draw-free core, unwired, behaviour-neutral) |
| **#29, #30, #31, #32, #33, #34, #37, #38, #40, #41, #43, #44, #49** | ✅ **APPROVED** | **no assembly exists** |

`src/` contains no `season-competition-loop`, no `training-system`, no `transfers`, no `finances`,
no `analytics`, no `discipline`, no UI screens. #30's own §9 checklist records it plainly:
*"Layer built — **NOT STARTED**."*

Thirteen approved specs (~13,000 spec lines) sit at zero implementation. At the spec→code ratios
this project actually realises (#22: 1,338→6,636; #21: 1,187→2,878; #12: 3,416→6,607 — call it
2–5×), that is **30–60k lines of unwritten code**. Meanwhile the question a playable build answers —
*is this game any good* — has never once been asked.

This roadmap is the shortest path to asking it.

---

## 1. What "playable" means — the milestone ladder

"Playable" is not one thing. Pinning it into a ladder is what makes the sequencing decidable, because
each rung has a different blocker profile.

### PM-1 — **Playable Match**
> Launch the client, choose two teams, set a formation and team instructions, kick off, watch the
> match render in real time, make a tactical change and a substitution while it runs, and read a
> post-match report with the score and match statistics.

**Exit criteria (testable):**
- A `MatchSetup` is producible from UI input (not hardcoded).
- Live tactical change + substitution apply through the P2 tick-stamped command channel.
- Post-match screen renders a `MatchAnalyticsResult` (#37).
- ~~The P6 closed-loop scenario passes: same `MatchSetup` + same command log ⇒ digest-identical runs.~~
  ✅ **MET 2026-08-03** — `match-client-command-log-replay` (plus a command-free control run that must
  diverge, so the criterion cannot be satisfied by a channel that does nothing) and
  `match-client-save-restore-replay`, both on the #19 `ScenarioRunner`, host-free and gate-checked.
  Note this is the *determinism* criterion only: the three above it still need UI.

### PM-2 — **Playable Season**
> Start a new game against a generated league, see a fixture list and a league table, advance
> day-by-day to the next fixture, play (or quick-resolve) each round, watch the table update, serve
> card suspensions, finish a 38-round season, and save/resume mid-season.

**Exit criteria:**
- A season completes end-to-end from the client with the managed fixture engine-simulated.
- Save mid-season → quit → resume is byte-identical to the uninterrupted run (the #30 T1 contract).
- League table tie-breaks, board objectives, and suspensions are live.

### PM-3 — **Playable Career**
> Roll into a second season with squads that changed: players aged and progressed, some retired,
> regens arrived, a training focus was set, injuries happened, a transfer window ran.

**Exit criteria:**
- `RollToNextSeason` is restartable and multi-season continuous (#30 T3).
- #28 progression is wired to the world tick; minimal #29/#40/#41/#31 land as identity-extended tiers.

**This roadmap targets PM-2 as the primary objective.** PM-1 is a prerequisite rung, PM-3 is the
first depth increment. PM-2 is where the game becomes answerable as a game.

---

## 2. The floor that already exists

Non-trivial, and it changes the shape of the remaining work. Recorded so nothing here gets rebuilt:

| Capability | Where | State |
|---|---|---|
| Full 90-minute match simulation | `src/match-engine/` (19,587 lines) | ✅ 321 tests; match flow complete (restarts, fouls/cards, offside, subs, half/full time) |
| Determinism + save/restore | `deterministic-sim`, `MatchSaveManager`, `SeasonSaveManager` | ✅ certified on the pinned host; round-trip byte-identity locked |
| Season **file** root | `src/season-save/` | ✅ world + optional in-progress match, one file, version-gated sub-blobs |
| Roster generation | `player-database/RosterGenerator.Generate(…, count)` and `Generate(…, PlayerPosition[])` | ✅ deterministic, 25-player club squads; the supplied-position overload (FR-SQ-012a, ERR-027-002) guarantees a fieldable squad |
| Lineup selection, attribute projection | `LineupSelector`, `PlayerAttributeProjection` | ✅ |
| Watchable match (web) | `match-viewer/` — HTML replay + `LiveMatchServer` live viewer | ✅ observer-neutral, digest-locked |
| **Manager command channel** | `src/match-client-core/` — `ManagerCommandQueue`, `MatchClientDriver`, tick-stamped log, `MatchSession` | ✅ **P0 + P2 landed 2026-07-24**, host-free, CI-gated |
| **Input-determinism lock (the PM-1 exit criterion)** | `src/match-client-core/` — `MatchSession.TickOnce/CaptureSave/RestoreFrom`, `TickStampedCommandReplay`, the two §5-P6 closed-loop scenarios | ✅ **head-less half of P6 landed 2026-08-03**, host-free, CI-gated. Same `MatchSetup` + same log ⇒ digest-identical; save@N → restore → replay == uninterrupted; and a command-free control run must diverge |
| Unity render skin | `src/match-client-unity/` | ⛔ asmdef only — P4–P5 + the on-host half of P6, host-gated |

**The single most under-appreciated asset is `RosterGenerator`.** It means a playable league needs no
database editor and no authored data — see C3. *(Realised July 25, 2026 as `LeagueBootstrap` — A3.)*

---

## 3. Two tracks, run in parallel

The work splits cleanly on one line: **does it need a Unity host?**

- **Track S — Simulation & loop (host-free).** #30, #37, #44, the league bootstrap. Every item
  compiles and tests under the `tools/dotnet-ci` Linux shim gate on every push. **No external
  blocker.** This track alone reaches *PM-2-sim* — a season fully playable head-lessly.
- **Track C — Client & presentation.** Unity client P1/P3 and #38 T0 are host-free; **P4–P6 are
  host-gated** and cannot be verified without access to the pinned Windows 11 / Unity 6000.4.9f1 host.

**They must run in parallel, and Track S must not wait on Track C.** The single worst outcome
available is blocking the season loop — which has no blocker — behind a Unity host that does.

---

## 4. Hard constraints discovered while sequencing

These are quantified findings, not cautions. Each one changes the plan.

### C1 — Full-fidelity season simulation is infeasible. The round-resolution model is critical path.
From the certified baseline (`kickoff-multi-second.cert.md`: **p50 = 0.4768 ms/tick**) and
`MATCH_TICKS_TOTAL = 324,000`:

- One full match ≳ **154 s** (~2.6 min) of compute.
- A 20-club league season = 38 rounds × 10 fixtures = **380 matches ≳ 16.3 hours**.

> These are **lower bounds**, not point estimates: they multiply the *median* per-tick cost by the tick
> count, but total wall-clock tracks the *mean*, and with p99 = 2.5669 ms the distribution is
> right-tailed. The true figures are higher, which only strengthens the conclusion below.

So "simulate every fixture with the real engine" is not a slow option — it is not an option. #30's
KD-9 `AdvanceAndPlayNextRound` already specifies the answer (managed fixture through a real
`MatchEngine`, the rest through a **round-resolution model**), but the sizing makes the requirement
sharp: for a round to resolve in under a second, the quick-sim budget is **≲ 10 ms/match** — a
~15,000× gap. That is a *statistical resolution model*, not a fast-forwarded engine, and it is on
the critical path to PM-2, not an optimisation after it.

### C1a — The quick-sim needs a calibration corpus, and the corpus costs compute.
If the round-resolution model's score distribution does not agree with the engine's, the league table
will feel wrong in a way no unit test catches. Calibrating it requires a corpus of **engine-simulated**
matches across varied squad strengths — at ~2.6 min each, ~200 matches ≈ **9 hours** of compute.
This is parallelisable and run-once. **Budget it explicitly (A4a) rather than discovering it during
A4.**

### C2 — Unity host access is an external blocker on P4–P6, and there is a real fallback.
The `match-client-unity` README and `interactive-unity-client-design.md` both record it: no Unity host
exists in the build/CI environment, so the render skin is verified only at a cert run. **But the
browser surface already works** — `LiveMatchServer` streams live frames to a canvas page today, in
CI, with no host. Extending that surface into the season UI reaches PM-1 and PM-2 **without a Unity
host at all**, and the #38 framework contract (view models + dispatchers) is renderer-agnostic by
design, so the UGUI skin later binds the same substrate. See the B6 decision point.

### C3 — No roster world exists — and `RosterGenerator` closes it without #47.
Nothing today produces a league of clubs. The roadmap assigns this to **#47 (New-Game Setup & DB
Editor, Wave 7, unauthored)**. But #47's *editor* is not what playability needs — playability needs
*a league to exist*. `RosterGenerator.Generate(rng, streamIndex, clubId, count)` already produces
deterministic 25-player club squads. A thin `LeagueBootstrap` over it (A3) yields a 20-club league
from the world seed in a few hundred lines, and **defers #47 entirely** past PM-3. Authoring a
database editor before anyone has played a season is the wrong order.

### C4 — Save-migration debt (#50) activates the moment saves matter.
#50 is parked in Wave 8 on the reasoning that pre-ship format bumps need no migration path. That
reasoning holds exactly until a save exists that someone would be upset to lose. PM-2 creates that
save. **Decide explicitly at the PM-2 exit:** either declare saves breakable through PM-3 (fine, and
cheap — say so in the build), or pull #50 forward. Do not let it be decided by accident.

### C5 — Every unimplemented spec carries defect latency; budget for it.
This project's own record: **ERR-024-001** (#24's Appendix A row keys matched no slot in any family
table — the whole overlay catalogue was a structural no-op, found at T0 *after* PASS-1 review had
checked lane geometry); **ERR-017-002** (#17 specified `Publish`/`Subscribe` overloads distinguished
only by generic constraint — illegal C#; six files implemented it verbatim and the production event
assembly had never compiled); the June-12 first full-tree compile finding *eight* never-compiled
surfaces. Expect **1–3 ERR-class findings per T-phase landing** against a spec that has never been
compiled against. That is not a reason to delay — it is the reason to *start*, and a reason to keep
the per-landing adversarial-review discipline on **code**.

---

## 5. The roadmap

Sequenced by dependency, annotated with owning governance and gate. "Landing" = one T-phase with
tests, adversarial review to convergence, and a green full gate — the project's established unit
(#28 T0 was one landing).

### Phase A — Season spine (Track S, host-free, **no external blockers**)

| # | Work item | Governance | Gate | Depends on |
|---|---|---|---|---|
| **A1** ✅ | **#30 T0 — LANDED July 25, 2026** (97 season-save tests, full gate green; surfaced **ERR-030-010**, see §9 risk row C5).  `SeasonState`, `Fixture`, `FixtureScheduler` (pure `Generate`), `LeagueTableRow`/`LeagueTable` (`ApplyResult` + tie-break `OrderedView`), `SeasonCalendar`, `BoardObjective`/`BoardState`, `MatchResult`, `SeasonViewModel`. New `TacticalDirector.SeasonLoop` assembly. Behaviour-neutral by construction (no orchestrator touched). | #30 §7.1 T0 | shim gate + determinism tests | — |
| **A2** ✅ | **#30 T1 — LANDED July 25, 2026** (135 season-save tests, full gate green; surfaced **ERR-030-011**, see §9 risk row C5). `SeasonStateCodec` (§3.6 / Appendix B sub-blob); `SeasonSaveCodec`/`SeasonSaveBlobs`/`SeasonSaveContents`/`SeasonSaveManager` gain the season block; `SEASON_SAVE_FORMAT_VERSION` **1 → 2**, `SEASON_STATE_FORMAT_VERSION` = 1 first used. World and match blobs byte-untouched (FR-SN-020). | #30 §7.1 T1 | round-trip + fail-loud gates | A1 |
| **A3** ✅ | **League bootstrap — LANDED July 25, 2026** (season-save 141 → 168 tests, player-database 42 → 46, full gate green). `LeagueBootstrap.Generate(worldSeed, clubCount)` → `League`: N clubs (default 20) × 25 **position-coherent** players, deterministic from one world seed via three domain-separated derivations; `Club`/`ClubNameCatalogue` identity; a per-club strength ramp so the table is not 20 identical teams (its *sufficiency* is A4a Step 0's first check); `League` **is** the `ISquadProvider`; `CreateSeason` hands #30 a startable `SeasonState`. Generation is persistence-equivalent — rosters are regenerated from the world seed, not saved — so it is pinned by a golden vector (KD-10), and `WorldStore.WorldSeed` is now readable so a saved career can rebuild its provider. **The #47-minimal substitute (C3), not the editor.** | `league-bootstrap-design.md` v1.1 (§6 item 1) | determinism + round-trip | A1 |
| **A4** ✅ | **#30 T2 — LANDED July 26, 2026** (season-save 179 → 240 tests (237 passed + the 3 env-gated calibration/diagnostic drivers skipped), incl. the `season-multi-fixture` capstone, full gate green; surfaced **ERR-030-012**, **ERR-030-013** and — via A4a's Step 0 — **ERR-030-014**, see §9). `SeasonLoop` composition root; `AdvanceToNextFixtureDay` (KD-2 fixed tick order, only the world tick live); `AdvanceAndPlayNextRound(ISquadProvider)` (KD-9 — managed fixture through a real `MatchEngine`, the rest via the round-resolution model); the #16 §3.4 back-prop (`DOMAIN_TAG_SEASON_LOOP = 0x22` / `SubsystemOrdinals.SeasonLoop = 84`); the `#19 ScenarioRunner` `season-multi-fixture` capstone. | #30 §7.1 T2 | capstone scenario + two-run determinism | A2, A3 (**not** A4a — see below) |
| **A4a** ⬥ **UNBLOCKED July 26, 2026 — now the next item** | **Round-resolution calibration corpus.** A4b landed, so a match now plays and Step 0 can be re-run (~33 min). **It may still refuse:** Phase H makes matches *play*, not necessarily *discriminate by squad strength*, which is exactly the question Step 0 exists to ask — if the extremes remain indistinguishable the answer is to raise `LeagueStrengthSpread` (risk row 3), not to fit three parameters to noise. Compute is not the blocker (~98 s/match ⇒ ~1.4 h across four processes, inside C1a's 9 h budget). Harness, fitter and re-run recipe are committed. ORIGINAL BLOCKED ENTRY: KD-8's Step 0 pilot ran and **refused to proceed**: all 20 engine matches finished **0–0** at a measured `dSquad` of **±6**. Characterisation found the ball's velocity identically zero for the whole match and no agent ever possessing it — `InitializeKickoffState` leaves the ball at rest, `RunFirstTouch` will only grant a touch on a *moving* ball, production possession comes only from that path, and only a possessing agent can kick. **A production match has always been a 90-minute 0–0 deadlock.** The blocker is therefore not A4a's compute (measured at ~98 s/match ⇒ ~1.4 h across four processes, well inside C1a's 9 h budget) but the engine's inability to produce a corpus with variance in it. Step 0 did exactly its job — it cost 33 minutes and saved a five-hour fit against a table of zeros. **The harness, the fitter and the re-run recipe are committed**; A4a resumes the moment A4b lands. | `league-bootstrap-design.md` KD-7/KD-8; evidence + root cause in `round-resolution-corpus.md` | Step 0 gate (re-runnable now) | **A4b** ✅ |
| **A4b** ✅ **LANDED July 26, 2026** | **Make the match playable — the possession bootstrap. DONE.** It needed **five** seams, not the single kickoff grant sketched below, and four of the five were found by *running* the composed engine, each invisible until the previous fix let play run further: the restart taker award (`ApplyRestart` now takes an `awardedTeam`; taker = nearest non-sent-off agent of that team), the loose-ball pickup for a ball that comes to REST, the DecisionTree loose-ball **collect** (ERR-008-014 — the tree had no action at all that fetches a stationary loose ball), the PASS/SHOOT completion sweep (ERR-008-015 — `NotifyActionComplete` had zero production callers, freezing every agent that passed or shot), and the interrupt deferral that stops a re-plan dispatching into a busy executor. Measured over 6 seeds × 9 min: ball 16.2–17.2 m/s (was 0.00), possession 10.5–20.9% of ticks (was 0%), 262–298 turnovers (was 0), both penalty areas reached, goals scored. Acceptance = the new `match-engine-play-develops` scenario; **every predicate fails on the pre-fix engine**, and `play-still-alive-at-final-tick` caught two stalls that let play run 8–9 minutes before dying. C5 held for the fifth consecutive landing. Full gate green; no `SNAPSHOT_SCHEMA_VERSION` change. **Unblocks A4a and PM-1.** Recorded not fixed: the foul heuristic's ~7 red cards per 9 min (now the top balance item), the interleaved-engine EventBus divergence. ORIGINAL SKETCH: Award possession to a designated agent at kickoff and at every restart, so the Decision Tree has a carrier to act for; from there PASS/SHOOT dispatch, first touch, offside, fouls and goal detection all already exist. Deliberately not folded into A4: it is a behaviour change to the most safety-critical assembly in the tree, it activates a large amount of code that has never run in composition (**C5 at its strongest — budget for several findings**), and it moves every engine digest, so the schema preimage probes and the certified perf baseline need review. **Blocks A4a and PM-1; does not block PM-2-sim.** | `match-engine-design.md` **§5.Z** (Phase H, KD-H1..KD-H5) | ✅ `match-engine-play-develops` — the composed scenario that asserts the ball is **kicked**, possession is held and contested, play is still alive at the final tick, and play reaches both boxes | A4 |
| **A5** ✅ **LANDED July 27, 2026** | **#30 T3 — the season-boundary roll. DONE, and with it Phase A's simulation spine.** `SeasonLoop.RollToNextSeason()` is the KD-6 restartable transform: finalize the table → evaluate the board (job security gained flat when the objective is met, lost **per league position short** when it is not) → (a') #43 and (b') #40 insertion points, declared and empty → derive the next seed through its own domain constant → regenerate the schedule → rebuild the calendar → (d) #28 age advance, empty → reset the table. Pure in the prior `SeasonState`: no clock read, no draw, so a save either side of the boundary restores to the same continuation and two careers from one seed agree on **both** seasons' tables. Everything is computed and validated before any write, and the throwing commit runs before the non-throwing one, so a refused roll leaves the season untouched. **C5's prediction held for the sixth consecutive landing — ERR-030-015: §3.5's pseudocode never rebuilt the calendar**, and a season rolled from it is *permanently unplayable* (the cursor stays at `RoundCount`, so both advance methods throw for the rest of the career). Caught only because the acceptance test plays a **second** season to completion; 9 of the suite's 18 predicates fail against the spec-as-written. Season-save 240 → 261 tests, full gate green. **An adversarial review over the landing then found 1H+3M+2L, all fixed** — headline: `AdvanceDays` bounded the clock only in-season, so walking the close season past the next season's opening day reached a career that could be neither played nor rolled and that saved and reloaded cleanly. A follow-up L moved the calendar shift onto `SeasonCalendar` itself, so one derivation now serves both the roll and that new bound (season-save 261 → 263). | #30 §7.1 T3 | ✅ restartability across a real save file + two-career determinism over two seasons + a rolled season played to completion | A4 |

**Phase A exit — `PM-2-sim`:** a full 38-round season simulates head-lessly, saves and resumes
byte-identically, and rolls into a second season. **No UI yet, no Unity, no external blocker.**

> **REACHED July 27, 2026.** A5's landing closes the last Phase-A item: a full 20-club / 38-round /
> 380-fixture season simulates head-lessly in milliseconds (A4), saves and resumes byte-identically
> mid-sequence (A2), and now **rolls into a second season and plays that one too** (A5) — two careers from
> one seed agreeing on both seasons' final tables. Every clause of the exit criterion is asserted by test.
>
> **What that does and does not claim.** PM-2-sim is a statement about the *loop*, not about the quality of
> what the loop simulates. Two things sit on top of it, both tracked and neither blocking it: the
> round-resolution quick-sim's three parameters are still **provisional, not fitted** (A4a, whose corpus
> cannot be trusted while the engine's goal rate runs several times football's — see
> `match-engine-design.md` §5.Z.15 and **§5.Z.17**), and the managed fixture, though it now genuinely
> plays (A4b), is not yet worth watching for the same reason. A season is *playable and correct*; making
> it *convincing* is the next question.
>
> **§5.Z.17 (July 27, 2026) narrowed that question sharply, and it is worth reading before picking up
> A4a.** §5.Z.15 named the goalkeeper's save as the next lever on the goal rate. Measured, the keepers
> were making **no saves at all** — zero hand contacts over three full matches — for three independent
> reasons, all now fixed. **The goal rate did not move: 15.3 → 15.3** per match against football's ~2.7.
> So the named lever was real, is now spent, and is worth **nothing measurable** on the scoreline. **The residual is the shot side, and
> it is structural:** shots essentially cannot miss the goal (aim is hardcoded 0.732 m inside the post
> and the vertical component of the aim is never read), there is **no crossbar** (every boundary test is
> gated on the ball being below 0.22 m), and there are **no blocked shots**
> (`BallCollisionHandler.OnAgentCollision` is an empty `TODO` that production calls). In football ~30% of
> shots are blocked and ~30% miss; here both are approximately zero. **A4a's blocker is now specific: the
> shot-outcome distribution, not the goalkeeper.**

### Phase B — Playable match client (Track C)

| # | Work item | Governance | Host? |
|---|---|---|---|
| **B1** | ~~**Unity client P1** — richer observation frame~~ **LANDED July 27, 2026.** Per-agent booking/sent-off/substitute cues, per-team substitutions used, the derived `MatchPeriod`, and the last restart (cue + team + tick), carried through `LiveMatchFrame` → `MatchFrameView`. No `SNAPSHOT_SCHEMA_VERSION` change: the engine's restart cue is a **within-tick** field (KD-P1-3) and the cross-tick latch lives in `LiveMatchStreamer`, so nothing new reaches the snapshot. Observer-neutrality re-locked over the whole new surface. | `interactive-unity-client-design.md` §5-P1 | host-free |
| **B2** | ~~**#37 T0**~~ **LANDED July 27, 2026.** New host-free `TacticalDirector.MatchAnalytics` assembly: the four value types (all immutable, all copy-not-wrap, all gated at construction), `MatchAnalyticsConstants`, and the pure `XgLocationModel` + T-AN-XG-* locks (the three §3.3 worked examples pinned, plus the shape properties a Stage-2 refit must preserve). §4.6 CS0104 grep: clean. KD-4 reverse-reference invariant now scanned mechanically. Surfaced **ERR-037-001** (§4.1's reference list omits the Ball Physics reference Appendix A's `[CROSS]` goal-width tag requires). 24 tests. | #37 §7.1 T0 | host-free |
| **B3** | ~~**#37 T1** — the read-only per-tick ledger tap + `MatchAnalyticsAggregator`~~ **LANDED July 27, 2026.** The KD-7 tap is a `TickLedgerSnapshot` the engine fills in the Snapshot phase, after `SerializeLedger` and before the bus resets the tick — **the only moment the records exist and the tick is identified**. It copies rather than indexing the ring, so "current-tick scoped" is structural, and it reuses `SerializeLedger`'s own canonical-order walk (extracted to `EventLedger.BuildCanonicalOrder`), so the digest bytes and the observer see one derivation. `MatchAnalyticsAggregator` implements §3.1–§3.4 with the §3.2 routing table keyed on `EventRegistry.GetOrdinal<T>()` (promoted public) rather than a local ordinal table — no parallel surface to drift. 30 new tests. Surfaced **ERR-037-002**. | #37 §7.1 T1 | host-free |
| **B4** | ~~**Unity client P3** — frame interpolation, follow-ball camera, live-stats accumulator~~ **LANDED July 27, 2026** (two of three; see below). `FrameInterpolator` — speed-aware alpha (an interpolator handed the unscaled tick rate falls further behind the sim every frame at 3×) and blending that **snaps rather than smooths across a discontinuity**: a restart teleports the ball and a substitution swaps who occupies a roster slot, and blending either draws a glide where the truth is a jump. `FollowBallCamera` — dead zone, `1 − e^(−rate·dt)` smoothing (proven frame-rate-independent by step subdivision, not asserted), and a pitch clamp that **centres** when the view is wider than the pitch instead of oscillating between two impossible bounds. 23 new tests. **The third item is deliberately not built:** #37's aggregator (B3) *is* the live-stats accumulator, and a second one in `match-client-core` would be the parallel-surface trap — recorded here rather than silently dropped. | §5-P3 | host-free |
| **B5** | ~~**#38 T0** — framework only~~ **LANDED July 25, 2026** (ahead of B1/B2; the table had not been updated). `NavigationShell`, `IViewModelSource<T>`, `ICommandDispatcher`, `ManagerIntent`, `MatchFrameView` + `MatchViewModelSource`, `MatchTacticsDispatcher`; 39 tests incl. the observer-neutrality digest lock and a mechanical reverse-reference scan. Filed **ERR-038-001/-002/-003**. | #38 (APPROVED) | host-free |
| **B6** | ~~**Renderer — option (b), extend the browser surface**~~ **LANDED July 27, 2026**, governed by the new `browser-match-client-design.md`. **Not** an extension of `LiveMatchServer`, and that is the design: that server's playback-only invariant is load-bearing (the streamer holds the engine, the server holds no engine reference — *disjoint by construction*), and it is what ERR-038-001 and the interactive-client AR-1 H-2 both turn on. So the mutating surface is a new host-free assembly `src/match-client-web/` **above** `match-client-core`, and `match-viewer` keeps its invariant. Three routes, three privileges, asserted against the command queue rather than by inspection: reads cannot change a match; `/playback` changes *when* ticks happen, never what is in them, so it never enters the replay log; `/intent` is the only mutating route and goes through `ManagerCommandQueue`, landing on a tick boundary and in the tick-stamped record. Router and transport are separate types, so every routing decision is a pure function under test and the socket code decides nothing. 34 tests. | `browser-match-client-design.md` (§6 item 2) | host-free |

**Phase B exit — `PM-1`: REACHED July 27, 2026.** A person can open a browser on the running client
and watch a real match with a live pitch, clock, score, period and restart captions; change a team's
mentality / pressing / passing and see it queued and applied on a tick boundary; substitute; pause,
resume and run at 1–10×; and read live statistics that keep serving after full time. Every clause is
asserted by test.

> **What PM-1 does and does not claim.** It is a statement about the *client*, not about the match it
> shows. The two open realism items are unchanged and neither blocks it: the engine's goal rate runs
> ~4.7× football's, and its home/away scoring asymmetry is ~50× football's home advantage
> (`match-engine-design.md` §5.Z.11/§5.Z.15). A match is now watchable; whether it is *convincing* is
> the next question, and it is a match-engine question rather than a client one.
>
> **Three PM-1 surfaces are deliberately thin, and each is recorded rather than quietly dropped:**
> team selection is `MatchSetup` in code (a new-game screen is C4, and #47's editor is deferred past
> PM-3 by C3); `SetPlayerTactic` returns **501** rather than assembling a per-agent tactic from
> defaults the manager never chose; and the post-match report is the live statistics panel continuing
> to serve after full time, not a dedicated screen — that belongs with C3's navigation work.

**Note on #37's ceiling:** §7.2 records that shots, shots-on-target, xG-over-shots, pass-completion,
tackles and saves all wait on match-engine producers that do not exist (`ShotAttemptedEvent`,
`PassCompletedEvent`, `TackleEvent`, a digest-committed `SaveAttemptedEvent`). PM-1's post-match
report therefore ships with possession, territory, score and heatmaps — **not** a full statline.
Adding the producers is a separate match-engine change with its own review (#37 §7.1 T2). Do not let
it expand Phase B.

### Phase C — Season playable end-to-end

| # | Work item | Governance |
|---|---|---|
| **C1** | **#44 T0/T1** — `TacticalDirector.Discipline` assembly (`DisciplineState`, `DisciplineRules`, `Availability`, constants) + `DisciplineSaveCodec` (`DISCIPLINE_SAVE_FORMAT_VERSION` = 1) composed into #30's season save. Inert until wired. | #44 §7.1 |
| **C2** | **#44 T2** — live wiring: the tap-fed `CardLedgerFold` (shares B3's #37-class per-tick tap — one tap, two consumers); the **ERR-030-009 filter** at #30's resolve→filter→configure seam; `OnClubFixturePlayed` serving on both resolution paths. | #44 §7.1 T2 |
| **C3** | **Season + squad screens** — league table, fixture list, calendar, squad/selection, advance-round. Binds #30's `SeasonViewModel` + the availability view; dispatches `AdvanceAndPlayNextRound`. | ⚠️ **needs governance** — §6 |
| **C4** | **New-game flow** — league bootstrap → season start → save slot, wired through the client. | with C3 |

**Phase C exit — `PM-2`. This is the objective.** A person can start a new game, play a season, and
save it.

### Phase D — Career depth (first increment past the objective)

| # | Work item | Governance |
|---|---|---|
| **D1** | **#28 T1–T3** — wire the landed progression T0 into #30's day-advance slot 1 + season boundary; promote the `player-progression.regen` production stream (`0x20`/82, KD-B). | #28 §7 |
| **D2** | **#29 minimal** — the training seam at #30's slot 2; conditioning cursor + the `ComputeTrainingInput` feed into #28's growth. | #29 §7 |
| **D3** | **#41 minimal** — injury occurrence/recovery on the world tick (`injuries.occurrence`, `0x2A`/92). | #41 §7 |
| **D4** | **#40 minimal** — budget-from-league-finish at #30's (b') boundary point. | #40 §7 |
| **D5** | **#31 minimal** — transfer window, offer/response, contracts; the roster re-key + #44 ban migration hygiene. | #31 §7 |
| **D6** | **#50 decision** — see C4. Either declare saves breakable, or author it. | §6 |

---

## 6. Governance gaps — the *minimum* that closes them

**Zero new numbered specs are required to reach PM-2.** Everything in Phases A–C is either an
APPROVED spec's own §7 T-phase plan or a converged design supplement. Three items need governance
that does not exist, and in each case a **design note is sufficient** — the precedent is
`lineup-selection-design.md`, `match-save-file-design.md` and `interactive-unity-client-design.md`,
all of which govern shipped code with no numbered spec:

1. ✅ **A3 League bootstrap + A4a calibration corpus** → `docs/tracking/league-bootstrap-design.md`
   (v1.1, AR-1..AR-3 converged, AR-4 over the shipped code). Resolves all five required questions:
   club count/identity/naming (KD-2/KD-3), strength distribution (KD-5), world-seed derivation
   (KD-4), the round-resolution model's shape (KD-7), and the calibration methodology (KD-8).
   Explicitly *not* #47 — it authors no editor and defines no new data format (it consumes #27's).
   **A3 is landed; A4a's run is still outstanding.**
2. **C3 Season/squad screens** → extend `interactive-unity-client-design.md` with a P7 (management
   screens) rather than authoring the Wave-7 #38 screens spec. #38 §7.1 already gates each screen on
   its data spec; #30 and #27 are APPROVED, so the gate is satisfied and the screens add no framework
   change (§7.1: *"no framework change per screen"*).
3. **D6 #50 Save Migration** → a decision first, a spec only if the decision goes that way.
4. **A4b make-the-match-playable** *(new, July 26, 2026)* → extend `docs/tracking/match-engine-design.md`
   with a possession-bootstrap phase, rather than opening a numbered spec. The match engine has never been
   a numbered spec — that design note already governs Phases A–G of it, including behaviour changes of this
   class — and the change consumes only surfaces that exist (`_possessingAgentId`, the restart primitive,
   the Decision Tree's carrier branch). The note must decide: which agent receives possession at kickoff and
   at each restart type, whether the grant is a possession assignment or a small imparted velocity, and what
   happens to the engine's digest baselines.

**Deliberately deferred past PM-3:** #47 (editor), #42, #45, #35, #36, #46, #48, #51, #52, #43,
#32, #34, #33, #49 content. None is on the path to a playable season.

---

## 7. The B6 decision point — renderer

This is the one genuine fork, and it should be taken consciously.

| | **(a) Unity P4–P6** | **(b) Extend the browser client** |
|---|---|---|
| Blocker | Needs pinned-host access; unverifiable in CI | None — `LiveMatchServer` runs in CI today |
| Cost | 3 landings + a cert run | ~2 landings |
| Fidelity | The real target platform; sprites, camera, UGUI | Canvas; adequate for tactics/table/report |
| Risk | Work sits unverified until host access | Throwaway risk *if* the UGUI skin later replaces it |
| Throwaway exposure | — | **Low** — #38's view models + dispatchers are renderer-agnostic; only the render leaf is re-done |

**DECIDED July 25, 2026 — (b) first, (a) after.** The #38 framework contract exists precisely so the
renderer is a leaf. Taking (b) reaches PM-1 and PM-2 with **no external blocker at all**, closes the
"is this fun" loop months earlier, and the UGUI skin then binds an already-proven substrate — which
is exactly what §7.2 of #38 says will happen anyway (*"the rendering is a view over the already-defined
substrate, exactly as `match-viewer`'s live HTML surface was"*). Take (a) when host access is
available, not as a gate on playing the game.

---

## 8. Dependency graph

```
                       [existing: match-engine · season-save · player-database · match-client-core]
                                                    │
   TRACK S (host-free, unblocked)                    │                    TRACK C
   ───────────────────────────────                   │            ──────────────────────────
   A1 #30 T0 ─┬─► A2 #30 T1 ──┐                     │            B1 P1 frame ──┐
              │               │                      │            B2 #37 T0 ─┬─►B3 #37 T1
              └─► A3 bootstrap─┤                     │            B4 P3 view ─┤
                    └─► A4a ───┤                     │            B5 #38 T0 ──┘
                              ▼                      │                    │
                        A4 #30 T2 ──► A5 #30 T3      │                    ▼
                              │                      │            B6 ⬥ renderer decision
                              │                      │                    │
                              └──────────► PM-2-sim  │                    ▼
                                              │      │                  PM-1
                                              └──────┴────────┬───────────┘
                                                              ▼
                                            C1/C2 #44 ──► C3 screens ──► C4 new-game flow
                                                              │
                                                              ▼
                                                           **PM-2**
                                                              │
                                                              ▼
                                              D1 #28 · D2 #29 · D3 #41 · D4 #40 · D5 #31 ──► PM-3
```

**Critical path to PM-2:** A1 → A2 → A4 → A5 → C1 → C2 → C3 → C4, with A3 feeding A4 and Track C
converging at C3. Roughly **13–16 landings to PM-2** on the critical path, plus 5–6 on Track C
running in parallel.

**Amended July 26, 2026.** A4a is no longer a *predecessor* of A4 — A4 landed with the model's shape
pinned and its numbers provisional, which is the right factoring: the loop does not depend on the fit, only
on the fit being honest about itself. A4a instead sits behind the new **A4b** (make the match playable),
which is now the critical path to **PM-1** and to any calibrated table:

```
   A4 #30 T2 ✅ ──► A4b ⬥ playable match ──► A4a calibration ──► (recalibrated quick-sim)
        │                   │
        └──► A5 #30 T3 ──► PM-2-sim        └──► PM-1 (Phase B's exit becomes reachable)
```

---

## 9. Risks

| Risk | Severity | Mitigation |
|---|---|---|
| Round-resolution model diverges from engine results; league tables feel wrong | **High** | A4a calibration corpus, budgeted as its own item; lock the fit by test |
| **A production match cannot develop play at all — ball velocity identically zero, possession never granted, every match 0–0 (ERR-030-014)** | **RESOLVED July 26, 2026 (A4b)** | Found by A4a's KD-8 Step 0 pilot, which refused to proceed and thereby saved a ~5 h fit against a table of zeros. Root cause diagnosed to a closed loop between the at-rest kickoff, `RunFirstTouch`'s moving-ball gate, and the possession-gated kick. Fixed by **A4b** the same day — five seams, four found by running the engine (see A4b). PM-1 and A4a unblocked. Evidence: `round-resolution-corpus.md`; fix: `match-engine-design.md` §5.Z |
| The league strength spread is too small to move engine results, so the table is noise regardless of the quick-sim | **High (now TESTABLE — A4b landed)** | `league-bootstrap-design.md` KD-8 **Step 0**: a ~20-match pilot at the ramp extremes runs BEFORE the 9 h corpus; if the extremes are indistinguishable, raise `LeagueStrengthSpread` first rather than fit three parameters to noise |
| A generation change silently invalidates every save (rosters are regenerated from the world seed, not persisted) | **High** | Pinned golden vector (`LeagueBootstrapGoldenVectorTests`, KD-10), verified non-vacuous; a generation change must re-pin in the same commit and is treated as save-breaking |
| Spec-defect latency — T0s surface ERR-class findings against never-compiled specs (C5) | **High (certain)** | Expect 1–3 per landing; keep code-side adversarial review; file ERRs against spec text as the project already does |
| Unity host access never materialises | Medium | B6 option (b) removes it from the critical path entirely |
| Phase B expands to chase the #37 §7.2 deferred producers | Medium | PM-1 ships possession/territory/score/heatmaps; producers are a separate match-engine change |
| Save-migration debt accrues silently once PM-2 saves exist (C4) | Medium | Force the decision at the PM-2 exit gate |
| #30 T2's fixed tick order gets re-pinned by later specs | **Low** | Already mitigated — ERR-030-002/004/006/007/008/009 pre-declared the null seams for #41/#31/#34/#32/#43/#44 |
| A3 bootstrap drifts into #47's data-model territory | Low | Design note explicitly consumes #27's format and defines none |

---

## 10. What this roadmap deliberately does not do

- **It does not finish the specification set.** #35, #36, #39, #42, #45, #46, #47, #48, #50, #51, #52
  and the #38-screens / #49-content slices remain unauthored, on purpose. The roadmap's own text
  already defers two of them (#39: *"do not front-load it"*; #52: *"deliberately deferred"*).
- **It does not deepen any minimal tier.** Every Phase-D item is the identity the deep tier later
  modulates — the discipline every management spec was authored under.
- **It does not touch the match engine's determinism, save, or event architecture.** Those are the
  cross-cutting invariants that genuinely required whole-system foresight, and they are already
  specified, implemented and test-locked. That is precisely why the remaining work can be sequenced
  by playability rather than by dependency fear.

---

## Version History

| Version | Date | Change |
| v0.9 | July 27, 2026 | **The shot-outcome distribution pass landed** (`shot-outcome-distribution-design.md` / `match-engine-design.md` §5.Z.18) — the specific blocker the v0.8 note named on A4a. Shots can miss (a true `tan(err)×distance` error cone + the vertical placement/error half made live), the goal has a crossbar (Law 9/10 airborne adjudication), shots are blocked (the agent-ball deflection is live), the shot pressure query is wired, and the goal-visibility gate can fire. Measured: **goals/match 15.3 → 12.3**, goals/shot 0.24–0.29 → 0.14–0.25, deflections 0 → 560–612/match. **A4a stays gated but the gate has moved:** the remaining goal-rate mass is shot VOLUME (59–70/match, ~2.5× football — DT selection / possession churn), shot SPEED (means 7–10 m/s vs ~25 — #6 `VFloor`/`VCeiling` × #8 `PowerIntent` shaping), and the keeper's catch/parry conversion. Those are the named next levers; each is a `[GT]`/selection balance pass with the measurement instruments now in place. |
|---------|------|--------|
| v0.10 | August 3, 2026 | **PM-1's determinism exit criterion is MET — the head-less half of the Unity client's P6 landed** (`interactive-unity-client-design.md` v0.10). Two closed-loop scenarios on the #19 `ScenarioRunner`, host-free and gate-checked every push: same `MatchSetup` + same tick-stamped log ⇒ digest-identical, and save@N → restore → replay the post-N log == the uninterrupted run. Ordering follows §12's argument — `match-client-unity` is shim-excluded, so P4/P5 are invisible to `tools/dotnet-ci` while this is not; the render skin now arrives against an existing determinism lock rather than ahead of one. **The finding worth carrying:** the phase description assumes three verbs `MatchSession` did not have — it could not be advanced head-lessly, saved, or restored — so P6 was three production additions (`TickOnce`, `CaptureSave` on the `ServiceOnce` seam, `RestoreFrom`) plus `TickStampedCommandReplay`, before any scenario could be written. **And the predicate that actually carries the criterion is the control run:** both scenarios pass on a command channel that does nothing, so a third command-free run must DIVERGE in a bounded window — the ERR-030-014 lesson applied to this layer. **PM-1's other three exit criteria are unchanged and still need UI** (a `MatchSetup` from UI input, live changes applied through the channel *from a screen*, a post-match `MatchAnalyticsResult` render); those are P4/P5 on the pinned host. Track-C inventory gains a row. |
| v0.9 | July 27, 2026 | **Phase B is complete and `PM-1` is REACHED.** B3 (#37 T1), B4 (Unity client P3) and B6 (the browser client) landed; the table's B5 row was corrected — #38 T0 had landed on July 25, ahead of B1/B2. **B3's tap is the design point:** it is filled in the Snapshot phase, after `SerializeLedger` and before the bus resets the tick — the only moment the records exist AND are identified with a tick — and it reuses `SerializeLedger`'s own canonical-order walk, so the digest bytes and the observer cannot drift apart. Surfaced **ERR-037-002** (§3.4 states the territorial split as two strict inequalities and then requires it to be total; both fail at exactly `x == L/2`, which a kickoff sits on for many consecutive ticks). **B4 built two of its three items and refused the third:** #37's aggregator already IS the live-stats accumulator, and a second one in `match-client-core` would be the parallel-surface trap. **B6's finding is that the obvious implementation was the wrong one:** extending `LiveMatchServer` would have handed the spectator surface a mutation channel, which is precisely what ERR-038-001 and the interactive-client AR-1 H-2 rejected; the mutating surface is a new assembly above `match-client-core` instead. It also needed a genuinely new seam — #37's every-tick contract cannot ride the pre-tick hook, which is set-once and also fires from `ServiceOnce()` where no tick advances — so `LiveMatchStreamer` gains a read-only post-tick observer that **disarms and latches** on failure rather than killing the sim thread. Governed by the new `browser-match-client-design.md`. Full gate green throughout; match-analytics 24 → 54, match-client-core 22 → 45, new match-client-web 34. **Next: Phase C (#44 discipline, then the season and new-game screens) — the objective, PM-2.** |
| v0.8 | July 27, 2026 | **A5 LANDED — Phase A is complete and `PM-2-sim` is REACHED.** `SeasonLoop.RollToNextSeason()`: the KD-6 restartable boundary transform, pure in the prior `SeasonState`, with the (a') #43 / (b') #40 / (d) #28 insertion points declared and empty. Two careers from one seed now agree on both seasons' tables, and a save taken at the boundary restores to the same continuation. C5 held for the sixth consecutive landing — **ERR-030-015**: §3.5's pseudocode never rebuilt the calendar, so a season rolled from it was permanently unplayable; caught only by an acceptance test that plays a *second* season to completion. Season-save 240 → 263 tests, full gate green (including the adversarial-review pass over the landing: 1H+3M+2L, all fixed). **Next on this track: A4a is still gated — not on compute, but on the engine's goal rate (~4.7× football's), because a corpus fitted now would calibrate the quick-sim to reproduce that.** |
| v0.7 | July 26, 2026 | **A4b LANDED — the match is playable; ERR-030-014 closed.** Five seams, four of them found by running the composed engine one after another (each invisible until the previous fix let play run further): the restart taker award, the loose-ball pickup, the DecisionTree loose-ball collect (ERR-008-014), the PASS/SHOOT completion sweep (ERR-008-015), and the interrupt deferral. Locked by the new `match-engine-play-develops` acceptance scenario, whose every predicate fails on the pre-fix engine. **A4a and PM-1 unblocked**; A4a is now the next item (re-run KD-8 Step 0, ~33 min — it may still refuse, since playing and discriminating by squad strength are different questions). New top balance item: the foul heuristic issues ~7 red cards per 9 minutes. See `match-engine-design.md` §5.Z. |
| v0.6 | July 26, 2026 | **A4 (#30 T2) LANDED** — the `SeasonLoop` composition root, the KD-2 fixed day-advance order (only the world tick live), whole-round resolution routed by a new `RoundResolutionMode` dial, the keyed `RoundResolutionModel` quick-sim, the `DOMAIN_TAG_SEASON_LOOP = 0x22` back-prop at its first draw site, and the `season-multi-fixture` capstone. Season-save 179 → 240 tests (237 passed + 3 env-gated drivers skipped), full gate green. C5's prediction held for the fourth consecutive landing, and this time it produced the most consequential finding on the track: **ERR-030-012** (§4.5's registered cursor stream contradicts §3.4.1's keyed-draw requirement — realized as a keyed derivation; `SubsystemOrdinals.SeasonLoop` deliberately NOT allocated, since an ordinal with no stream is the FR-LW-031 phantom), **ERR-030-013** (§4.6's "records the `MatchResult` in `SeasonState`" is unimplementable — §2.2/Appendix B have no outcome collection), and **ERR-030-014** — found by executing A4a's KD-8 Step 0 pilot: **every engine match finishes 0–0 because a production match never puts the ball in motion at all**. New item **A4b** (kickoff/restart possession grant) now precedes A4a on the critical path; A4a is blocked upstream of itself, with its harness, fitter and re-run recipe committed. |
| v0.5 | July 25, 2026 | **A3 (league bootstrap) LANDED** — `LeagueBootstrap` / `League` / `Club` / `ClubNameCatalogue` / `LeagueBootstrapConstants` in `TacticalDirector.SeasonSave` (+ a `PlayerDatabase` asmdef reference), and an additive supplied-position `RosterGenerator.Generate` overload. Governed by the new `league-bootstrap-design.md` (AR-1 1H+2M+2L → AR-2 1M+1L → AR-3 CONVERGENCE → AR-4 over the code 2M+3L). Season-save 141 → 168 tests, player-database 42 → 46, full gate green. C5's prediction held in a new form: the H finding was found *at design time* rather than after landing — uniform position draws make ~3% of bootstrapped squads unable to field a back four, which `LineupSelector` refuses fail-loud, so a 20-club league would have failed to start *by seed*. §6 item 1 is closed; **A4a's methodology is designed, its ~9 h run is not done.** |
| v0.4 | July 25, 2026 | **A2 (#30 T1) LANDED** — the season save/restore path: `SeasonStateCodec` over the Appendix B layout, the frame gaining a third sub-blob, `SEASON_SAVE_FORMAT_VERSION` 1 → 2, and `Save`/`Load` gaining the season (FR-SN-021). Season-save tests 112 → 135, full gate green. C5's prediction held for the second consecutive landing: implementation surfaced **ERR-030-011** (§3.6's `EncodeSeason` pseudocode omitted `ManagedClubId`, which Appendix B row 3a requires; Appendix B row 11's `f32/u8` job security pinned to the integer per-mille the code carries), filed and patched same commit. A code self-review also closed an encode/decode asymmetry in the T0 `SeasonState` constructor (an empty schedule with an unset calendar was constructible but not decodable). |
| v0.3 | July 25, 2026 | Adversarial-review corrections: engine test count 306 → 321 (both occurrences); C1's per-match / per-season figures relabelled **lower bounds** — they multiply the certified *median* per-tick cost by the tick count, but wall-clock tracks the *mean* and p99 = 2.5669 ms, so the true cost is higher (the infeasibility conclusion only strengthens). |
| v0.2 | July 25, 2026 | **B6 renderer decision taken: option (b)** — extend the existing browser client; Unity P4–P6 follows when host access exists. **A1 (#30 T0) LANDED** — the season value types, fixture scheduler, league table, calendar, board, match-outcome payload, season state and view model, with 77 new tests (season-save 20 → 97) and the full gate green. C5's prediction held on the first landing: implementation surfaced **ERR-030-010** (§3.1's parity venue rule vs the §3.7 / Appendix C worked tables), filed and patched same commit. |
| v0.1 | July 25, 2026 | Initial roadmap: PM-1/PM-2/PM-3 milestone ladder with testable exit criteria; existing-floor inventory; Track S / Track C split; five quantified constraints (C1 season-sim infeasibility at ~16.3 h/season and the ≲10 ms quick-sim budget; C1a the ~9 h calibration corpus; C2 the host block and the browser fallback; C3 `RosterGenerator` deferring #47; C4 save-migration debt; C5 spec-defect latency); Phase A–D work breakdown anchored to each spec's own §7 T-phase plan; the §6 finding that zero new numbered specs are required to reach PM-2; the B6 renderer decision point with a recommendation; dependency graph; risk register. |
