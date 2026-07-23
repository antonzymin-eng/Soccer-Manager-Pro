# GK/Heading Closed-Loop Scenario — Design Supplement

> **Status:** DESIGN SUPPLEMENT (pre-implementation; not a numbered spec — same governance class as
> `match-engine-design.md` / `gk-heading-engine-integration-design.md`).
> **Created:** 2026-07-23
> **Governs:** a new `#19 ScenarioRunner` closed-loop scenario for the opt-in Goalkeeper (#11) /
> Heading (#10) engine wiring landed in Phase 1 (July 22) + Phase 2 (July 23).
> **Owner sign-off:** pending.

---

## 0. Why (motivation + scope)

The GK/Heading wiring (`docs/tracking/gk-heading-engine-integration-design.md`) landed in two phases:

- **Phase 1 (v1.44, July 22):** `EnableGkHeading()` opt-in; flag-on drives both orchestrators + the
  §4 world-state triggers that commit a `SaveIntent` / `HeaderIntent` seeded from the
  `ToGoalkeeper` / `ToHeading` projections. Locked by `MatchEngineGkHeadingTests` (unit/integration).
- **Phase 2 (v1.46, July 23):** flag-on cross-tick state serialized at `SNAPSHOT_SCHEMA_VERSION` 18,
  so a flag-on engine is snapshot-safe. Round-trip determinism locked in
  `MatchEngineSnapshotRestoreTests`.

Both phases are verified at the **unit + direct-integration** level. What does **not** yet exist is a
**Simulation-layer closed-loop scenario** on the `#19 ScenarioRunner` — the composition-level lock the
project uses for every other orchestrator (`MatchEngineCapstoneScenarios`,
`MatchEngineAwayTeamScenarios`, the per-spec `*Scenarios` corpora). The GK/Heading design's own
"Phase 2 deferred / still deferred" list names **"the closed-loop `#19 ScenarioRunner` scenario"** as
outstanding. This supplement designs exactly that one item.

**In scope:** one scenario file + one test file registering flag-on GK/Heading scenario(s) on the
ScenarioRunner, asserting the composition runs end-to-end deterministically (flag-on forward digest
determinism through the harness), that the projections are a live consumer **through the natural
`RunTick` phase pipeline** (a trigger fires and commits inside `RunAiPhase`, not via a directly-called
drive seam), and that a flag-on save→restore→continue is byte-identical through the harness.

**Net-new value over the existing unit suite** (`MatchEngineGkHeadingTests` + `MatchEngineSnapshot*`):
the scenario's delta is the **Simulation-layer composition** the unit suite does not exercise —
(a) trigger firing driven through `RunTick`'s phase order (AI-phase commit, not a `TestOnly_Drive*`
call); (b) flag-on **restore-through-the-`ScenarioRunner`-harness**; (c) the structural invariants
(cadence / on-pitch / finite) held with the flag on across a multi-second composed run. The forward
flag-on determinism digest match overlaps the existing `FlagOn_TwoRuns_AreForwardDeterministic` unit
test and is kept only as the harness-level echo (the capstone/away-team precedent), not the point.

**Explicitly out of scope** (each is its own deferred item in the GK/Heading design):
- Flipping the default flag ON + the flag-on digest rebaseline.
- The `CollisionConsumer` AGENT_BALL duel fan-out (contested headers).
- A DT-driven producer superseding the Stage-0 heuristic triggers.
- Any production `MatchEngine.cs` behaviour change (the scenario reads existing public + `TestOnly_`
  seams only — the `MatchEngineCapstone`/`AwayTeam` precedent: **no production change**).

---

## 1. Key decisions (KD)

- **KD-1 — Test-only, zero production change.** The scenario observes through the existing public
  surface (`RunTick`, `CurrentSnapshotDigest`, `CurrentTick`, `AiPhaseRunCount`, `AgentView`,
  `AgentTeamId`, `AgentIsGoalkeeper`) + existing `internal` `TestOnly_*` seams
  (`EnableGkHeading` is public; `TestOnly_GkHeadingEnabled`, `TestOnly_LastCommittedSaveAttrs`,
  `TestOnly_LastCommittedHeaderAttrs`, `TestOnly_ForceBallLoose`, `TestOnly_DriveGkHeadingTactical`).
  No new production seam. If a needed observation is missing, prefer the public surface; add a
  `TestOnly_` seam only as a last resort and flag it in the detailed plan.

- **KD-2 — Owning specs `{2, 10, 11, 16, 19}` (+ the match-engine composition).** The asserted
  behaviour is load-bearing on Agent Movement (#2, the on-pitch/clamp invariant — the `AwayTeam`
  precedent lists #2 for exactly this reason), Heading (#10), Goalkeeper (#11), Deterministic
  Simulation (#16, 7-phase tick + digest + restore), and Testing Strategy (#19, the harness). Tier
  **B** (Simulation layer), path under `SCENARIO_PATH_CROSS_SPEC_PREFIX` — the `AwayTeam` precedent
  for a match-engine-composition scenario spanning ≥2 owning specs. (If the final predicate set drops
  the on-pitch bound, drop #2 to match — the owning-spec set must equal the actually-asserted set.)

- **KD-3 — Trigger firing is driven through the natural `RunTick` pipeline; only the *stimulus* is
  scripted.** The scenario does NOT call `TestOnly_DriveGkHeadingTactical` directly (that would test
  the seam, not the composition, and merely re-run `MatchEngineGkHeadingTests`). Instead it scripts
  only the **world stimulus** — a shot-on-goal / loose airborne ball via `TestOnly_ForceBallLoose`
  (you cannot test a save without a ball travelling at the goal) — immediately before a `RunTick`
  whose AI phase is a stride tick, and lets `RunTick`'s own `RunAiPhase → DriveGkHeadingTactical`
  read the forced ball and commit. Phase order guarantees this: AI (phase 2) runs before Physics
  (phase 3), so the forced ball is consumed by the tactical drive before physics moves it. The
  predicate then asserts `TestOnly_LastCommitted*Attrs.HasValue` — the projection reached the
  orchestrator **through the phase pipeline**. The stimulus is deterministic (fixed tick, fixed ball
  state), so the trigger scenario is itself two-run reproducible.

- **KD-4 — Determinism is the primary lock; both directions; both runs are pure free-play.** The
  determinism and restore runs carry **no `TestOnly_*` mutation** — they are pure flag-on `RunTick`
  loops, so they lock the *natural* pipeline (not a scripted-perturbed chain) and avoid any
  inject-replay-across-the-save-boundary coupling. (a) **Forward:** two independent flag-on engines,
  same seed → byte-identical per-tick digest chain (re-locks `EventBus.ResetForNewMatch` across two
  in-process matches, the capstone precedent). (b) **Restore:** save@N → restore → tick to N+K == an
  uninterrupted flag-on free-play run to N+K, byte-for-byte — driven **through the harness** so the
  ScenarioRunner exercises the flag-on snapshot path end to end. (The unit suite proves restore in
  isolation; the composed harness run is the new coverage.) The trigger stimulus (KD-3) lives in its
  own scenario/predicate so it never contaminates these runs.

- **KD-5 — Robust structural predicates.** Beyond determinism: tick-count exact; `ai-stride-cadence`
  = `NumTicks / AI_PHASE_STRIDE` (locks the 10 Hz/60 Hz separation with the flag on); ball + all agents
  finite and on-pitch every tick (flag-on physics must not destabilize the world). These stay true
  regardless of whether a trigger happens to fire on a given tick.

- **KD-6 — Flag-off contrast lock, against the *same* stimulus.** The flag-off lock is only meaningful
  under the identical trigger stimulus that makes flag-on commit — otherwise it is vacuous (a free-play
  flag-on run might also not commit). So the contrast applies the **same** `TestOnly_ForceBallLoose`
  stimulus + `RunTick` sequence as KD-3 under both flags: flag-on commits, flag-off commits **nothing**
  (the byte-identical-to-pre-wiring contract — the flag-off engine ignores the ball stimulus entirely).
  (Detailed plan decides one-scenario-two-runs vs. two registered scenarios; cross-spec arity ≥2 applies
  per registered path.)

---

## 2. Shape of the deliverable

Mirror the `MatchEngineAwayTeamScenarios` / `...Tests` pair exactly:

- `src/match-engine/tests/MatchEngineGkHeadingScenarios.cs` — `internal static` scenario builder:
  `BuildIndex()` → a `ScenarioIndex` with the registered `ClosedLoopScenario`(s); the scenario body
  boots a real `MatchEngine`, `EnableGkHeading()`, ticks a multi-second run with scripted trigger
  injection(s), and records the KD-4/KD-5/KD-6 envelope predicates.
- `src/match-engine/tests/MatchEngineGkHeadingScenarioTests.cs` — runs the scenario through
  `ScenarioRunner.Run(path, seed)` and asserts `Passed`; plus a direct two-run digest-chain equality
  test and a direct save/restore-continuity test (the capstone/away-team test-file pattern).

No asmdef change expected — `match-engine-tests.asmdef` already references TestingStrategy,
DeterministicSim, PlayerDatabase, HeadingMechanics, GoalkeeperMechanics.

---

## 3. Risks / open questions (resolved in the detailed plan)

- **R1 — Stimulus timing through the pipeline (KD-3).** The detailed plan must (a) confirm
  `TestOnly_ForceBallLoose` is callable between `RunTick` calls (it is `internal`, used mid-run by the
  unit suite), and (b) place the force at a tick such that the *next* `RunTick`'s `RunAiPhase` is a
  stride tick (`DriveGkHeadingTactical` runs only on stride ticks), so the forced ball is read by the
  tactical drive before `RunPhysicsPhase` moves it. Determinism/restore runs carry **no** stimulus
  (KD-4), so no inject-replay-across-restore question arises.
- **R2 — Save/restore through the harness:** prefer the in-memory blob API
  `MatchSaveManager.Encode(engine) → byte[]` / `Restore(blob) → MatchEngine` (verified present) to keep
  the scenario disk-free (harness scenarios are hermetic); `CaptureDurable*` + `RestoreFromSnapshot`
  is the lower-level fallback. Decide in the detailed plan.
- **R3 — One scenario or two registered paths?** Determinism/restore (free-play) vs. trigger+flag-off
  (stimulus) are cleanly separable (KD-4 vs KD-3/KD-6). Decide one-file-two-scenarios vs. one scenario
  with multiple runs; cross-spec arity (≥2 owning specs) applies to each registered path.
- **R4 — Envelope predicate count > 0** (FR-TS-030: zero predicates ⇒ Failed) and every predicate is
  observable through the chosen seams without a new production surface (KD-1).

---

## 4. Acceptance

- The scenario(s) register on the ScenarioRunner and `ScenarioRunner.Run(...) == Passed`.
- Flag-on forward determinism, a live trigger commit, flag-on save/restore continuity, structural
  invariants, and the flag-off no-commit contrast are all locked.
- Full dotnet gate green; no production `MatchEngine.cs` change; no `SNAPSHOT_SCHEMA_VERSION` change.

---

## 5. Detailed implementation plan

### 5.1 Files (mirror the `MatchEngineAwayTeam*` pair)

- `src/match-engine/tests/MatchEngineGkHeadingScenarios.cs` — `internal static class
  MatchEngineGkHeadingScenarios`, namespace `TacticalDirector.MatchEngine`. `BuildIndex()` returns a
  `ScenarioIndex` with **one** registered `ClosedLoopScenario` (R3 decided: one scenario, multiple runs
  in its body — the away-team single-scenario pattern; cross-spec arity ≥2 satisfied by 5 owning specs).
- `src/match-engine/tests/MatchEngineGkHeadingScenarioTests.cs` — `[TestFixture] public sealed class
  MatchEngineGkHeadingScenarioTests`: (1) runs the scenario through `ScenarioRunner.Run(path, seed)` →
  `Assert.AreEqual(ScenarioStatus.Passed, …)`; (2) direct two-run flag-on determinism digest test;
  (3) direct flag-on restore-continuity test; (4) direct save-trigger + header-trigger commit + flag-off
  no-commit tests (clear per-predicate failure messages, the capstone/away-team convention).

No asmdef change — `match-engine-tests.asmdef` already references TestingStrategy, DeterministicSim,
PlayerDatabase, HeadingMechanics, GoalkeeperMechanics, AgentMovement.

### 5.2 Registration constants

```
Path  = SCENARIO_PATH_CROSS_SPEC_PREFIX + "gk-heading-flag-on-composition"
Seed  = 0x11ED9A5EC0DEBA5EUL         // distinct from the away-team/capstone seeds
Tier  = TestTier.TierB               // Simulation layer
Owning specs = { 2, 10, 11, 16, 19 } // #2 for the on-pitch bound (KD-2)
NumTicks = 300                       // 5 s @ 60 Hz; 300 / AI_PHASE_STRIDE = 50 strides
RestoreSaveTick = 180 ; RestoreContinueTicks = 120   // 180 + 120 = 300
```

### 5.3 Scenario body — the runs (each builds a fresh engine; only the trigger/flag-off runs mutate)

- **`RunFreePlayFlagOn(seed, ticks)` → `byte[][]` digest chain + structural observations.** Pure:
  `new MatchEngine(seed)` → `EnableGkHeading()` → loop `RunTick()`, record `CurrentSnapshotDigest`,
  and per tick track (a) ball finite + on-pitch, (b) every agent finite + on-pitch (the away-team
  `AgentView` bound, ± `AgentBufferM` + `BoundsEpsilonM`). **No `TestOnly_*` mutation** (KD-4).
- **`FireTriggerThroughPipeline(seed, kind)` → bool committed.** The KD-3 natural-pipeline driver, and
  the whole reason this is a *composition* lock rather than a re-run of the unit seam test:
  ```
  var e = new MatchEngine(seed); e.EnableGkHeading();
  for (int i = 0; i < AI_PHASE_STRIDE; i++) {         // ≤6 ticks ⇒ exactly one stride tick
      ForceStimulus(e, kind);                          // re-forced each tick so physics drift can't
      e.RunTick();                                     //   move the loose ball out of trigger range
      if (Committed(e, kind)) return true;             //   before the stride tick's AI phase reads it
  }
  return false;
  ```
  The commit happens **inside `RunTick`'s `RunAiPhase → DriveGkHeadingTactical`** (verified at
  `MatchEngine.cs:2181`) — the phase pipeline, not a directly-called drive. Phase order (AI=2 before
  Physics=3) guarantees the forced ball is read before physics moves it.
  - Save stimulus: `ForceBallLoose((5, 34, 0.11), (-10, 0, 0))` (team-0 keeper defends x=0; the unit
    test's proven geometry). `Committed` = `TestOnly_LastCommittedSaveAttrs.HasValue`.
  - Header stimulus: `ForceBallLoose((agentPos.x, agentPos.y, 1.0), zero)` for the first outfield
    agent, recomputed at that agent's **current** position each tick. `Committed` =
    `TestOnly_LastCommittedHeaderAttrs.HasValue`.
- **`FlagOffIgnoresStimulus(seed, kind)` → bool committed.** Identical loop, `MatchEngine` **without**
  `EnableGkHeading()`. Must return **false** — `DriveGkHeadingTactical` is a no-op while the flag is
  off, so the same stimulus commits nothing (KD-6 non-vacuous contrast: same stimulus, opposite flag).
- **`RestoreContinuityMatches(seed, N, K)` → bool.** Reference = `RunFreePlayFlagOn(seed, N+K)` digest
  chain. Split run: build a fresh flag-on engine, tick to N, `blob = MatchSaveManager.Encode(engine)`,
  `restored = MatchSaveManager.Restore(blob)` (in-memory, disk-free — R2; Phase-2 restore reproduces
  the flag-on mode), tick `restored` for K more, record its digests. Assert the split run's
  digests[N+1 .. N+K] equal the reference's byte-for-byte.

### 5.4 Envelope predicates (all in the scenario body; count > 0, FR-TS-030)

| id | assertion |
|----|-----------|
| `tick-count` | `RunFreePlayFlagOn` advanced exactly `NumTicks` (`CurrentTick == NumTicks`) |
| `ai-stride-cadence` | `AiPhaseRunCount == NumTicks / AI_PHASE_STRIDE` (10 Hz/60 Hz separation, flag on) |
| `flagon-ball-on-pitch` | ball finite + within pitch±buffer every tick |
| `flagon-agents-on-pitch` | every agent finite + within pitch±buffer every tick (#2) |
| `flagon-two-run-determinism` | two `RunFreePlayFlagOn(seed)` chains byte-identical (harness echo) |
| `save-trigger-commits-via-pipeline` | `FireTriggerThroughPipeline(seed, Save) == true` |
| `header-trigger-commits-via-pipeline` | `FireTriggerThroughPipeline(seed, Header) == true` |
| `flagoff-save-stimulus-no-commit` | `FlagOffIgnoresStimulus(seed, Save) == false` |
| `flagoff-header-stimulus-no-commit` | `FlagOffIgnoresStimulus(seed, Header) == false` |
| `flagon-restore-continuity` | `RestoreContinuityMatches(seed, 180, 120) == true` |

Each predicate carries a diagnostic detail string (first-bad tick / agent) on failure, InvariantCulture
(the away-team convention). Every value is read through existing public / `internal TestOnly_*` seams —
**no new production surface** (KD-1). The `context.RunSeed` is the KD-7-seeded run seed; each helper is
handed that seed so the whole scenario is reproducible and its two-run predicates compare like with like.

### 5.5 Test file assertion (runner-only, the away-team precedent)

One test — `sim_crossspec_gk_heading_flag_on_composition` — runs the scenario through
`ScenarioRunner.Run(CompositionPath, CompositionSeed)` and asserts `ScenarioStatus.Passed`, mirroring
`MatchEngineAwayTeamTests` exactly (§2). Legibility on failure is covered by the ScenarioRunner: a
failed predicate returns via `result.Diagnostics` naming the exact predicate id + its detail string
(first-bad tick / diverge tick), so a separate direct-mirror test per predicate adds no failure-signal
the diagnostics don't already carry. (An earlier draft of this §5.5 listed five direct mirror tests;
that contradicted §2's "mirror the away-team pair exactly" — reconciled to runner-only, the code's
choice, at implementation.)

### 5.6 Non-goals / invariants held

- No production `MatchEngine.cs` edit; no `SNAPSHOT_SCHEMA_VERSION` change; no asmdef change.
- The scenario adds Simulation-layer composition coverage; the unit suite (`MatchEngineGkHeadingTests`,
  `MatchEngineSnapshotRestoreTests`) is unchanged and remains the per-function authority.
- Determinism/restore runs never touch a `TestOnly_*` mutation seam; only the trigger/flag-off runs do,
  and they are isolated engines.

### 5.7 Edge cases the plan pins

- **Stride alignment** (R1): the ≤`AI_PHASE_STRIDE`-iteration re-forcing loop is offset-independent — it
  does not assume which residue of `CurrentTick` is a stride tick; it simply keeps the stimulus fresh
  until one AI phase runs. If the loop exits without a commit, the predicate fails loud (not a silent
  pass) — surfacing a real regression if the trigger geometry ever stops firing.
- **Latch semantics**: `TestOnly_LastCommitted*Attrs` is a latched observation; `HasValue` stays true
  after the first commit, so the loop's early-return is correct and re-forcing cannot un-commit it.
- **Restore boundary**: the save at tick N is taken on a pure free-play engine (no pending stimulus), so
  the inject-vs-save ordering hazard the AR-1 flagged cannot arise (KD-4 keeps stimulus out of this run).

#region VersionHistory
<!--
| Version | Date       | Author | Notes                                            |
| 0.1     | 2026-07-23 | —      | Initial high-level outline (pre-adversarial-review). |
| 0.2     | 2026-07-23 | —      | AR-1 (0H+3M+2L): separated determinism/restore (free-play, no mutation) from the trigger stimulus; trigger now fires through the natural RunTick AI phase (composition, not the TestOnly_Drive seam); flag-off contrast bound to the same stimulus (was vacuous); owning specs +#2 for the on-pitch bound; net-new-vs-unit-suite delta stated. AR-2 sweep clean — CONVERGENCE. |
| 0.3     | 2026-07-23 | —      | §5 detailed implementation plan added: files, registration constants, run helpers (free-play / trigger-through-pipeline / flag-off / restore-continuity), the 10-predicate envelope table, test-file assertions, and the pinned edge cases (offset-independent stride loop, latch semantics, restore boundary). |
| 0.4     | 2026-07-23 | —      | Plan AR-1 (0H+0M+3L) — CONVERGENCE. Core mechanism verified against source: DriveGkHeadingTactical (RunAiPhase:2181) reads live _ball/_possessingAgentId, and nothing between AI-phase entry and the drive mutates them, so a forced ball fires the trigger through the pipeline. Lows folded into the implementation: trigger loop uses 2×AI_PHASE_STRIDE for margin; restore compares reference[N+j] vs split[j]; header red-card edge accepted (fails loud). |
| 1.0     | 2026-07-23 | —      | IMPLEMENTED + code AR-1 (0H+0M+3L) — CONVERGENCE. New tests/MatchEngineGkHeadingScenarios.cs + MatchEngineGkHeadingScenarioTests.cs. Full dotnet gate (SDK 8.0.129 via apt): PASSED, 0 failures (305 match-engine tests; whole tree green). Code AR verified against source: the commit-attr writers are reachable only via RunTick's AI phase (non-tautological trigger predicates), the flag-off contrast is non-vacuous (same stimulus commits flag-on), flagon-restore-continuity genuinely locks Phase-2 v18 flag-on snapshot completeness, and no state leaks across the sequential engine boots. Lows: §5.5 reconciled to the away-team runner-only precedent (this row); reference-chain recompute in RestoreContinuityMatches (correct, minor) + FirstOutfieldAgent -1 fail-loud (can't happen at kickoff) left as-is. |
-->
#endregion
