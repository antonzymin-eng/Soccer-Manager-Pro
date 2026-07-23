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

#region VersionHistory
<!--
| Version | Date       | Author | Notes                                            |
| 0.1     | 2026-07-23 | —      | Initial high-level outline (pre-adversarial-review). |
| 0.2     | 2026-07-23 | —      | AR-1 (0H+3M+2L): separated determinism/restore (free-play, no mutation) from the trigger stimulus; trigger now fires through the natural RunTick AI phase (composition, not the TestOnly_Drive seam); flag-off contrast bound to the same stimulus (was vacuous); owning specs +#2 for the on-pitch bound; net-new-vs-unit-suite delta stated. AR-2 sweep clean — CONVERGENCE. |
-->
#endregion
