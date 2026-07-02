# File Manifest (Post-Migration Baseline)

**Created:** April 30, 2026  
**Last Updated:** July 2, 2026 (**Minimal match viewer — first presentation-layer surface.** New assembly `src/match-viewer/` (`TacticalDirector.MatchViewer`; tooling, not a numbered spec): `match-viewer.asmdef`, `MatchViewerConstants.cs`, `ReplayFrame.cs`, `MatchReplay.cs`, `MatchReplayRecorder.cs`, `HtmlReplayExporter.cs` + `tests/match-viewer-tests.asmdef`, `tests/MatchViewerTests.cs` — see the new `src/match-viewer/` section. Modified: `src/match-engine/MatchEngine.cs` v1.24 (public read-only observation surface: `BallView`/`AgentView(i)`/`AgentTeamId(i)`/`AgentIsGoalkeeper(i)`/`PossessingAgentId` — value-type copies, no behaviour change; observer-neutrality digest-locked in `MatchViewerTests`). dotnet gate runs in CI on push.)  
**Last Updated (prior):** June 30, 2026 (**Tactical Instructions #21 — Stage-1 per-agent on-disk tactic-file loader.** New: `src/match-engine/PlayerTacticFileLoader.cs` (`Parse(text) → PlayerTacticConfig` over the `[agent N]` `key = value` text grammar — the per-agent sibling of `TeamTacticFileLoader`; omitted key/section ⇒ identity ⇒ behaviour-neutral; fail-loud on every malformation) + `src/match-engine/tests/PlayerTacticFileLoaderTests.cs`. The team + per-agent in-code config sources, boot appliers, and text loaders now all exist; only the pinned `[GT]` disk-encoding swap (FR-CS-019, Stage-1, outside #21) is outstanding. No production-behaviour change at the default. dotnet gate runs in CI on push. Prior June 30, 2026 (**Tactical Instructions #21 — Stage-1 leftovers (per-agent tactic config + G2 balance pass + §3.4 depth recompute).** New: `src/match-engine/PlayerTacticConfig.cs` + `src/match-engine/PlayerTacticConfigApplier.cs` (in-code per-agent `PlayerTactic` config source + boot applier; mirrors `TeamTacticConfig`; `Identity` = behaviour-neutral), `src/match-engine/tests/PlayerTacticConfigTests.cs`, `src/tactical-instructions/Tests/BalancePassInvariantsTests.cs` (pins identity-row exactness + strict monotonicity + `RoleWeightModifiers` ∈ [0.5,2.0] shapes). Modified: `src/match-engine/MatchEngine.cs` v1.23 (per-agent `_active`/`_pendingPlayerTactics[SQUAD_SIZE]` + public `SetPlayerTactic` + stride-commit + `ctx.PlayerTactic` route + `WritePlayerTactic` serialization + `TestOnly_PlayerTactic`; §3.4 `FillDefensiveSnapshot.DefensiveLineDepth = Clamp01(DefensiveLine + MentalityLineBias)`), `src/match-engine/MatchEngineConstants.cs` v1.17 (`SNAPSHOT_SCHEMA_VERSION` 9 → 10 + v10 doc), `src/match-engine/tests/MatchEngineTacticTests.cs` v1.4 + `MatchEngineSnapshotSchemaTests.cs` v1.7 (pin 9→10 + `PlayerTactic_FeedsSnapshotDigest`), `src/tactical-instructions/TacticalInstructionsConstants.cs` v1.1 (G2 magnitudes pinned — values unchanged; framing illustrative → pinned), `src/decision-tree/TacticTranslation.cs` + `UtilityScorer.cs` + `TacticalContext.cs` (doc reframes), `docs/specs/tactical-instructions/section-3.md` + `section-5.md` (G2 balance pass DONE). No production-behaviour change at the default (byte-identical). dotnet gate runs in CI on push.) Prior June 28, 2026 (**FR-PO-052 certified perf baseline corpus machinery.** New: `src/performance-optimization/CertificationStatus.cs` (Pending/Certified) + `CertifiedPerfBaseline.cs` (a certification-tagged baseline corpus entry — Pending carries NO metric and refuses to build a record because the Linux gate is NON-certifying; Certified validates a complete manifest + finite positive metrics and projects to a corpus BaselineRecord; platform-pin tokens Stage0CertPlatformPin/LinuxNonCertPlatformPin), `src/match-engine/tests/CertifiedPerfBaselineTests.cs` (PENDING lock + certified projection through PerfGateRunner + fail-closed invariants), and the first corpus artifact `docs/specs/performance-optimization/baselines/match-engine/kickoff-multi-second.cert.md` (PENDING_CERT_RUN — runbook to promote on the pinned platform). Modified: `src/match-engine/tests/MatchEngineCapstoneTests.cs` v1.1 (non-cert anchor pin → the named LinuxNonCertPlatformPin constant; behaviour-neutral). No production behaviour change; the authoritative per-tick budget still requires a run on the pinned Windows/Unity tuple. Prior June 28, 2026: Match Engine design note **Phase F — capstone closed-loop scenario; Match Engine integration (Phases A–F) complete.** New: `src/match-engine/tests/MatchEngineCapstoneScenarios.cs` (`match-engine-kickoff-multi-second` on the #19 ScenarioRunner — owning specs {1,2,3,4,5,6,7,8,12,13,14,15,16,17,19}, Tier B; boots a real MatchEngine, ticks it 600× = 10 s @ 60 Hz, records gameplay-invariant predicates [tick-count; ai-stride-cadence = NumTicks/AI_PHASE_STRIDE = 100; ball + agents finite and on-pitch every tick; chained snapshot digest advances] + a two-run same-seed determinism digest match) and `MatchEngineCapstoneTests.cs` (runs the scenario through ScenarioRunner.Run → Passed; direct two-run digest-chain equality test; FR-PO-052 per-tick perf-gate activation via PerfGateRunner.Run against a generous Stage-0 anchor BaselineRecord — NON-certifying Linux gate, authoritative budget stays on the pinned Windows/Unity tuple). Modified: `src/match-engine/tests/match-engine-tests.asmdef` (+`TacticalDirector.TestingStrategy` + `TacticalDirector.PerformanceOptimization`). No production `MatchEngine.cs` change — the scenario reads world state through existing internal `TestOnly_*` seams + public `CurrentTick`/`AiPhaseRunCount`/`CurrentSnapshotDigest`. `docs/tracking/match-engine-design.md` v1.0 (Phase F implemented; status line + §5 Phase F + Version History updated). CI gate (`bash tools/dotnet-ci/run-gate.sh`) verifies compile + suite on push — dotnet is unavailable in the authoring environment. Prior June 27, 2026 (Match Engine design note Phase E — events-phase consumers. New: `src/match-engine/tests/MatchEngineEventsTests.cs` (publish-on-change interrupts only the new holder; no-change publishes nothing; two same-seed runs with a possession transition produce byte-identical ledger-backed digest chains + lock the per-match reset seam; transition-vs-baseline effect; Tier A boot-phase Subscribe guard). Modified: `src/event-system/EventBus.cs` v2.1 (new public `ResetForNewMatch()` — clears the Tier A/B subscriber Dispatchers table + Tier C channel and reopens the boot phase, leaving the EventRegistry row schema intact; closes match-engine Risk #4 — the process-static bus could not be re-subscribed by a second match), `src/match-engine/MatchEngine.cs` v1.15 (RunResolvePhase calls `PublishPossessionChangeIfChanged` after C4 — diffs the settled holder vs the new `_prevPossessingAgentId` and publishes a Tier A PossessionChangedEvent #17 0x04 on a net change; Boot calls `EventBus.ResetForNewMatch()` then `EventBus.Subscribe<PossessionChangedEvent>(OnPossessionChanged)` which NotifyInterrupt()s the new holder's DecisionTree; new `TestOnly_DtState` seam), `src/match-engine/MatchEngineConstants.cs` v1.15 (`POSSESSION_CHANGE_REASON_UNSPECIFIED` [FIXED] byte 0), `src/match-engine/tests/match-engine-tests.asmdef` (+`TacticalDirector.EventSystem`). No `SNAPSHOT_SCHEMA_VERSION` bump — world-state body unchanged; only the serialized ledger digest now carries the event. `docs/tracking/match-engine-design.md` v0.9.13 (Phase E implemented; Risk #4 RESOLVED; §4 boot-step-2 + §5 Phase E + status line updated). CI gate (`bash tools/dotnet-ci/run-gate.sh`) verifies compile + suite on push — dotnet is unavailable in the authoring environment. Phase F pending.) Prior June 22, 2026, later same day (Match Engine design note Phase D step D3 — first-touch wiring. Modified: `src/match-engine/MatchEngine.cs` v1.8 (RunResolvePhase calls RunFirstTouch after the C3 executor Update and before the C4 UpdateMatchContext — a loose, ground-level, moving ball arriving within FIRST_TOUCH_ACCEPTANCE_RADIUS_M of the nearest APPROACHING agent triggers BuildFirstTouchContext via the real internal PressureEvaluator + OrientationDetector seams, EvaluateFirstTouch/ApplyTouchResult through the new FirstTouchWorldAdapter, and the outcome maps onto possession; CONTROLLED → toucher, INTERCEPTION → interceptor (AGENT_ID_NONE at Stage 0 → loose), LOOSE_BALL/DEFLECTION → loose), `src/match-engine/MatchEngineConstants.cs` v1.8 (FIRST_TOUCH_ACCEPTANCE_RADIUS_M + FIRST_TOUCH_MIN_BALL_SPEED_M_S [GT]; SNAPSHOT_SCHEMA_VERSION unchanged — FirstTouchSystem is stateless, writing only _ball + _possessingAgentId), `src/match-engine/match-engine.asmdef` (+FirstTouch), `src/first-touch/AssemblyInfo.cs` v1.1 (+InternalsVisibleTo("TacticalDirector.MatchEngine") so the host runs the internal PressureEvaluator / OrientationDetector context-assembly seams). New: `src/match-engine/tests/MatchEngineFirstTouchTests.cs` (CONTROLLED receive → possession, home + away frame-agnostic; receding / high / possessed not touched; same-seed digest determinism). `docs/tracking/match-engine-design.md` v0.9.6 (D3 implemented). CI gate (`bash tools/dotnet-ci/run-gate.sh`) verifies compile + suite on push — dotnet is unavailable in the authoring environment. D2b + D4–D5 + E–F pending.) Prior June 22, 2026, later same day (Match Engine design note Phase D step D2a — mechanics-AI wiring (Positioning AI #12). Modified: `src/match-engine/MatchEngine.cs` v1.7 (RunAiPhase runs RunPositioningAI before the perception/DT loop — one PositioningAITick + reused PositioningPerceptionSnapshot per team seeded at boot from STAGE0_FORMATION, filled from world state + ticked, GetFormationSlot folded into each agent's TacticalContext; away team mapped through the canonical attack-+X frame via the self-inverse 180° MirrorPitchIfAway — ERR-008-002 guard; new helpers RunPositioningAI/FillPositioningSnapshot/ComputeTeamMeanFatigue/MirrorPitchIfAway + TestOnly_FormationSlot), `src/match-engine/MatchEngineConstants.cs` v1.7 (MaxEntityId [DERIVED] + STAGE0_FORMATION/STAGE0_TACTICAL_INTENSITY [GT]; SNAPSHOT_SCHEMA_VERSION unchanged — positioning hysteresis serialization is the D4 step), `src/match-engine/match-engine.asmdef` (+PositioningAI). New: `src/match-engine/tests/MatchEngineMechanicsTests.cs` (formation slots feed the decision context; away-team mirror; same-seed slot determinism). `docs/tracking/match-engine-design.md` v0.9.5 (D2a implemented; §5 split into D2a/D2b; Pressing #13 / Defensive #14 / Attacking #15 remain as D2b). CI gate (`bash tools/dotnet-ci/run-gate.sh`) verifies compile + suite on push — dotnet is unavailable in the authoring environment. D2b + D3–D5 + E–F pending.) Prior June 22, 2026, later same day (Match Engine design note Phase D step D1 — AI-phase wiring (perception → decision → movement). Modified: `src/match-engine/MatchEngine.cs` v1.6 (RunAiPhase drives a host-owned perception SpatialHashGrid + PerceptionSystem.OnHeartbeat ×22 → DecisionTree.ReceiveSnapshot ×22; new HostMovementController adapter writes MovementCommands into _commands; InitializeAiSnapshots assembles the Stage-0 static §2.5 AI input snapshots; DecisionTree EventBusRegistrar booted; PerceptionSubsystem/DecisionTreeAI aliases), `src/match-engine/MatchEngineConstants.cs` v1.6 (PERCEPTION_GRID_POINT_INSERT_RADIUS [FIXED]; SNAPSHOT_SCHEMA_VERSION unchanged — DT/perception cross-tick state serialization is D4), `src/match-engine/match-engine.asmdef` (+PerceptionSystem), `src/match-engine/tests/MatchEnginePhysicsTests.cs` v1.1 (OutfieldAgent_MovesTowardTarget... replaced by AiPhase_DrivesChain_GoalkeepersSkipped — AI now owns _commands; +DeterministicSim using). `docs/tracking/match-engine-design.md` v0.9.5 (D1 implemented; §5 D1 + §6.5 perception/DT cross-tick-state D4 follow-up). No files added or removed. CI gate (`bash tools/dotnet-ci/run-gate.sh`) verifies compile + suite on push — dotnet is unavailable in the authoring environment. D2–D5 + E–F pending.) Prior June 19, 2026, later same day (Match Engine design note Phase C steps C1/C1a/C2/C3 — Resolve-phase wiring. Modified: `src/match-engine/MatchEngine.cs` v1.4 (collision + per-agent executor lifecycle in RunResolvePhase; PassWorldAdapter/ShotWorldAdapter nested adapters; BuildPass*/BuildShot* world-state mappers; possession field + TestOnly_ seams), `src/match-engine/MatchEngineConstants.cs` v1.4 (NO_POSSESSION + STAGE0_NEUTRAL_* GT region), `src/match-engine/match-engine.asmdef` (+CollisionSystem/PassMechanics/ShotMechanics), `src/match-engine/tests/match-engine-tests.asmdef` (+PassMechanics/ShotMechanics). New: `src/match-engine/tests/MatchEngineResolveTests.cs` (collision separation in Resolve, same-seed determinism with a live collision, scripted pass/shot initiation through the adapters). `docs/tracking/match-engine-design.md` v0.9.2 (C1–C3 marked implemented; C4 absorbs the registry boot + possession-flip test). C4–C6 pending.) Prior June 19, 2026 (Match Engine design note Phase C step C0 — executor snapshot get/restore seams. New source files: `src/pass-mechanics/PassExecutorState.cs` (PassExecutor cross-tick state DTO; PhysicalProfile excluded — recomputed on restore) + `src/shot-mechanics/ShotExecutorState.cs` (ShotExecutor cross-tick state DTO). Modified: `src/pass-mechanics/PassExecutor.cs` v1.14 + `src/shot-mechanics/ShotExecutor.cs` v1.9 (CaptureState/RestoreState seams, parallel to the B0 OscillationGuard seam). New test files: `src/pass-mechanics/Tests/PassExecutorStateTests.cs` + `src/shot-mechanics/Tests/ShotExecutorStateTests.cs` (CanonicalSerializer round-trip + Capture/Restore identity locks). Test asmdefs `pass-mechanics-tests` / `shot-mechanics-tests` gain the `TacticalDirector.DeterministicSim` reference (CanonicalSerializer). `docs/tracking/match-engine-design.md` v0.9.1 (C0 marked implemented). C1–C6 pending.) Prior June 11, 2026, later same day (Decision Tree #8 comprehensive audit (AR-2): 3H+11M+9L. New files: `docs/specs/decision-tree/audit-report.md` (canonical audit deliverable), `src/decision-tree/Tests/DecisionContextAssemblerTests.cs` (H-2/M-1 locks). Modified: 18 decision-tree source files + 4 test files (see audit-report.md Files-changed list), `src/agent-movement/MovementCommand.cs` v1.4 (PressSprint + SprintWhileWatching factories), 5 asmdefs (DeterministicSim reference: decision-tree, pass-mechanics, perception-system, heading-mechanics, goalkeeper-mechanics), 7 decision-tree spec section files (ERR-008-002..011 patches), spec-error-log.md v1.26.) Prior June 11, 2026 (Pass Mechanics #5 AR-9 fix pass (1H+3M+5L) + AR-10 sweep (2L, same commit). No files added or removed — H-1 repaired the never-compiling `src/pass-mechanics/Tests/PassMechanicsTests.cs` (namespace closed before the v1.1 IT- fixture; stray `}` at EOF) and added the PassExecutorGuardTests fixture (PX-001..004) inside it; source fixes in PassExecutor.cs v1.12, PassErrorCalculator.cs v1.8, PassTargetResolver.cs v1.8, PhysicalProfile.cs v1.2, PassAgentAttributes.cs v1.1, PassAgentState.cs v1.1, PassOutcome.cs v1.3, tests v1.2.) Prior June 10, 2026, still later same day (Scenario-corpus expansion onto the #19 ScenarioRunner. New Spec #1 per-spec corpus: `src/ball-physics/tests/BallPhysicsScenarios.cs` (drop-and-rebound — AR-7 H-1 / ERR-001-001 lock; fast-descent-grounds-out — AR-7 H-2 hover-deadlock lock; envelope windows derived from a numerical mirror of the fixed model) + `src/ball-physics/tests/BallPhysicsScenarioTests.cs` (sim_<scenario> Simulation-layer tests); `ball-physics-tests.asmdef` gains the testing-strategy reference. First cross-spec corpus (KD-8, paths under `tests/scenarios/cross-spec/`): `src/testing-strategy/Tests/CrossSpecScenarios.cs` (lofted-pass-kick-bounce-roll — real PassExecutor (#5) kicks a real BallState through the real BallPhysicsCore loop (#1) via the IPassBallSystem seam, with #17 boot wiring + tick lifecycle around the CONTACT publish; owning specs {1, 5}) + `CrossSpecScenarioTests.cs`; `testing-strategy-tests.asmdef` gains ball-physics / pass-mechanics / event-system references. Manifest reconciliation: the three June-7 ball-physics test files (EnumOrdinalStabilityTests, BodyPartCoefficientsTests, SurfacePropertiesTests) were recorded only in this header note and never added to the Spec #1 per-file table — rows added now. Prior June 10, 2026, still later same day (Spec #19 ScenarioRunner AR-2 sweep: 0H+0M+2L, both resolved — ScenarioEnvelope.cs v1.2 (NaN in_range bounds throw as authoring error; min>max exception message InvariantCulture); ScenarioRunnerTests.cs v1.2 (18→19 tests). Prior June 10, 2026, later same day (Spec #19 ScenarioRunner AR-1 fix pass: 0H+4M+6L, all resolved. New file: `src/testing-strategy/ScenarioIndexEntry.cs` (extracted from ScenarioIndex.cs per FILE NAMING precedent + AR-1 M-1 manifest-coherence guard). Modified: ScenarioRunner.cs v1.1 (M-2 fixture_refs refusal, M-4 path↔name + cross-spec arity, L-6 format-version-first), ScenarioIndex.cs v1.1 (M-4 duplicate-name rejection), ScenarioEnvelope.cs v1.1 + ClosedLoopScenario.cs v1.1 (M-3 CR/LF sanitization + exception_stack line), ScenarioManifest.cs v1.1 (L-1 ReadOnlyCollection wrappers), ScenarioResult.cs / IScenario.cs / ScenarioContext.cs v1.1 (doc), TestingStrategyConstants.cs v1.4 (SCENARIO_PATH_CROSS_SPEC_PREFIX), ScenarioRunnerTests.cs v1.1 (12→18 tests), AgentMovementScenarios.cs v1.1 (L-2 InvariantCulture details, L-3 exact position equality for T-AM-115). Prior June 10, 2026 (Stage 0 closed-loop scenario harness landed — Spec #19 §3.3.3 ScenarioRunner pulled forward from the Stage 0+1 schedule after the third consecutive spec (Ball Physics AR-7, Agent Movement AR-12/AR-13) where H/M-class closed-loop defects were encoded by pure-function unit suites rather than caught by them. New `src/testing-strategy/` files (9 .cs): ScenarioStatus, ScenarioResult, ScenarioManifest, ScenarioEnvelope, ScenarioContext, IScenario, ClosedLoopScenario, ScenarioIndex, ScenarioRunner; new `src/testing-strategy/Tests/` (testing-strategy-tests.asmdef + ScenarioRunnerTests.cs, 12 contract tests); TestingStrategyConstants.cs v1.3 adds SCENARIO_MANIFEST_FORMAT_VERSION. First fixture corpus: T-AM-110..115 migrated out of AgentMovementTests.cs (v2.3) into AgentMovementScenarios.cs (bodies + A.1 manifests) + AgentMovementScenarioTests.cs (`sim_<scenario>` Simulation-layer tests); agent-movement-tests.asmdef gains the testing-strategy reference; docs/specs/agent-movement/test-plan.md v0.4. A `src/testing-strategy/` per-file section was added to this manifest (the June 7 scaffold had been recorded only in this header note). Prior June 9, 2026 (Ball Physics #1 AR-7/AR-8 fix pass: BallGroundInteraction.cs v1.3.1, BallPhysicsCore.cs v1.4, BallPhysicsConstants.cs v1.8, tests/BallPhysicsCoreTests.cs v1.5, tests/BallIntegrationTests.cs v1.4 (new Airborne_FastDescent_Bounces_NoHoverDeadlock test); docs/specs/ball-physics/section-3-1-8-to-3-1-14.md row 2.8; spec-error-log.md v1.23 (ERR-001-001..003). No files added or removed. Prior June 8, 2026 (Event System #17 boot-wiring smoke test landed: new `src/event-system/tests/EventBusWiringSmokeTests.cs` v0.4 — SMOKE-EVT-WIRING-001 drives boot → publish-one-per-spec → DrainTick → SerializeLedger and asserts SHA-256 digest stability across the 6 currently-wired EventBusRegistrar.Initialize() call sites (Pass / Shot / Perception / Decision / Heading / Goalkeeper); Agent Movement (#2) is a `[CROSS-PENDING]` slot pending the AM-side registrar; golden digest pinned via Assert.Inconclusive until that lands. AR-1 (1H+1M+4L) + AR-2 (0H+1M+4L) + AR-3 (0H+0M+2L cycle-stop) adversarial review cycles complete. `event-system-tests.asmdef` extended with 6 production spec references for the smoke test (Editor-only, no production layering impact — test assemblies are infrastructure per src/CLAUDE.md). Prior June 7, 2026 (AR-hardening sweep complete + test scaffolding landed. New source files: `src/agent-movement/AssemblyInfo.cs` (InternalsVisibleTo for tooling-override factory access). New test files: `src/ball-physics/tests/EnumOrdinalStabilityTests.cs` (AR-6 L-3 — locks int ordinals for all 6 public enums), `src/ball-physics/tests/BodyPartCoefficientsTests.cs` (AR-4 L-2 — throw-on-unknown + catalogue round-trip), `src/ball-physics/tests/SurfacePropertiesTests.cs` (AR-4 L-2 — throw-on-unknown across all 4 Get* methods), `src/agent-movement/Tests/AgentMovementTests.cs` v2.0 (T-AM-001..018, T-AM-030..033, T-AM-040..043 — 18 NUnit tests across 4 fixtures), `src/agent-movement/Tests/AgentMovementUnitTests.cs` (T-AM-007..107 — 59 NUnit tests across 7 fixtures). New tracking doc: `docs/specs/agent-movement/test-plan.md` v0.2 (T-AM-NNN catalogue). File splits: Ball Physics — `BallCollision.cs` split into `BodyPart.cs` + `RestartType.cs` + `KickResult.cs` + `BodyPartCoefficients.cs` + `BallCollision.cs` per FILE NAMING (AR-2 L-2). Performance Optimization — `TraceChannel.cs` split into `ChannelVerbosity.cs` + `ChannelSamplingRule.cs` + `ChannelDeterminismClass.cs` + `TraceChannelDescriptor.cs` + `TraceChannelRegistry.cs` (AR-1 H-1). New Testing Strategy assembly: `src/testing-strategy/` with 14 files (TestingStrategyConstants, TestTier, TestLayer, GoldenVectorKind/Entry/Result/Runner, DeterminismTierKind/Result, DeterminismSuiteResult, DeterminismGate, PerfGateReport, PerfGateRunner, testing-strategy.asmdef). Prior May 31: AR-4 fixes in event-system.))
**Last Updated:** June 22, 2026, later same day (Match Engine design note Phase C steps C4/C5/C6 — Resolve-phase completion. Modified: `src/match-engine/MatchEngine.cs` v1.5 (MatchContext authored each Resolve via UpdateMatchContext; Pass/Shot EventBusRegistrar boot; C5 SerializeWorldState adds per-agent executor C0 capture + MatchContext; WritePassExecutorState/WriteShotExecutorState/WriteMatchContext helpers; TestOnly_MatchContext seam), `src/match-engine/MatchEngineConstants.cs` v1.5 (SNAPSHOT_SCHEMA_VERSION 1 → 2 + v1/v2 doc), `src/match-engine/match-engine.asmdef` + `src/match-engine/tests/match-engine-tests.asmdef` (+DecisionTree), `src/match-engine/tests/MatchEngineSnapshotSchemaTests.cs` (pin 1 → 2). New: `src/match-engine/tests/MatchEngineMatchContextTests.cs` (ball-zone authoring, possession derivation, scripted pass reaches CONTACT + releases possession, same-seed determinism with a live CONTACT publish, C5 digest-preimage probes). `docs/tracking/match-engine-design.md` v0.9.4 (C4–C6 implemented, Phase C complete). Match-engine manifest section reconciled — missing MatchEngineResolveTests.cs row added; header/refs/MatchEngine.cs row refreshed for C0–C6. Phase D D1 unblocked.) Prior June 22, 2026 (Match Engine design note Phase D step D0 — DecisionTree snapshot get/restore seam, the gating sub-step. New source file: `src/decision-tree/DecisionTreeState.cs` (cross-tick state-machine DTO: DtState ordinal + last AgentAction + _hasDispatchedAction; _matchSeed/_optionBuffer excluded per §2.6). Modified: `src/decision-tree/DecisionTree.cs` v1.2 (CaptureState/RestoreState seams, parallel to the Pass/Shot executor C0 seams), `src/decision-tree/Tests/decision-tree-tests.asmdef` (+`TacticalDirector.DeterministicSim` for CanonicalSerializer). New test file: `src/decision-tree/Tests/DecisionTreeStateTests.cs` (CanonicalSerializer round-trip + Capture/Restore identity + fresh-IDLE default + reflection field-count guard). `docs/tracking/match-engine-design.md` v0.9.3 (D0 marked implemented; §5 Phase D expanded into ordered sub-steps D0–D5). Decision Tree section count 36 → 38 files. C4–C6 + D1–D5 + E–F pending.) Prior June 20, 2026 (Tactical Instructions #21 APPROVED — new spec folder `docs/specs/tactical-instructions/` (11 section files: outline + section-1..8 + section-9-approval-checklist + appendices, at v0.3/v0.4) + `adversarial-review-section-files-v1.md` (PASS-1) + `adversarial-review-section-files-v2.md` (PASS-2). **No `src/` source files** — the `src/tactical-instructions/` assembly (T0 scaffolding) is not yet authored, so no per-file source rows are added here; this manifest tracks `src/` inventory. `docs/tracking/tactical-instruction-layer-design.md` marked superseded by the spec. Prior June 11, 2026, later same day (Decision Tree #8 comprehensive audit (AR-2): 3H+11M+9L. New files: `docs/specs/decision-tree/audit-report.md` (canonical audit deliverable), `src/decision-tree/Tests/DecisionContextAssemblerTests.cs` (H-2/M-1 locks). Modified: 18 decision-tree source files + 4 test files (see audit-report.md Files-changed list), `src/agent-movement/MovementCommand.cs` v1.4 (PressSprint + SprintWhileWatching factories), 5 asmdefs (DeterministicSim reference: decision-tree, pass-mechanics, perception-system, heading-mechanics, goalkeeper-mechanics), 7 decision-tree spec section files (ERR-008-002..011 patches), spec-error-log.md v1.26.) Prior June 11, 2026 (Pass Mechanics #5 AR-9 fix pass (1H+3M+5L) + AR-10 sweep (2L, same commit). No files added or removed — H-1 repaired the never-compiling `src/pass-mechanics/Tests/PassMechanicsTests.cs` (namespace closed before the v1.1 IT- fixture; stray `}` at EOF) and added the PassExecutorGuardTests fixture (PX-001..004) inside it; source fixes in PassExecutor.cs v1.12, PassErrorCalculator.cs v1.8, PassTargetResolver.cs v1.8, PhysicalProfile.cs v1.2, PassAgentAttributes.cs v1.1, PassAgentState.cs v1.1, PassOutcome.cs v1.3, tests v1.2.) Prior June 10, 2026, still later same day (Scenario-corpus expansion onto the #19 ScenarioRunner. New Spec #1 per-spec corpus: `src/ball-physics/tests/BallPhysicsScenarios.cs` (drop-and-rebound — AR-7 H-1 / ERR-001-001 lock; fast-descent-grounds-out — AR-7 H-2 hover-deadlock lock; envelope windows derived from a numerical mirror of the fixed model) + `src/ball-physics/tests/BallPhysicsScenarioTests.cs` (sim_<scenario> Simulation-layer tests); `ball-physics-tests.asmdef` gains the testing-strategy reference. First cross-spec corpus (KD-8, paths under `tests/scenarios/cross-spec/`): `src/testing-strategy/Tests/CrossSpecScenarios.cs` (lofted-pass-kick-bounce-roll — real PassExecutor (#5) kicks a real BallState through the real BallPhysicsCore loop (#1) via the IPassBallSystem seam, with #17 boot wiring + tick lifecycle around the CONTACT publish; owning specs {1, 5}) + `CrossSpecScenarioTests.cs`; `testing-strategy-tests.asmdef` gains ball-physics / pass-mechanics / event-system references. Manifest reconciliation: the three June-7 ball-physics test files (EnumOrdinalStabilityTests, BodyPartCoefficientsTests, SurfacePropertiesTests) were recorded only in this header note and never added to the Spec #1 per-file table — rows added now. Prior June 10, 2026, still later same day (Spec #19 ScenarioRunner AR-2 sweep: 0H+0M+2L, both resolved — ScenarioEnvelope.cs v1.2 (NaN in_range bounds throw as authoring error; min>max exception message InvariantCulture); ScenarioRunnerTests.cs v1.2 (18→19 tests). Prior June 10, 2026, later same day (Spec #19 ScenarioRunner AR-1 fix pass: 0H+4M+6L, all resolved. New file: `src/testing-strategy/ScenarioIndexEntry.cs` (extracted from ScenarioIndex.cs per FILE NAMING precedent + AR-1 M-1 manifest-coherence guard). Modified: ScenarioRunner.cs v1.1 (M-2 fixture_refs refusal, M-4 path↔name + cross-spec arity, L-6 format-version-first), ScenarioIndex.cs v1.1 (M-4 duplicate-name rejection), ScenarioEnvelope.cs v1.1 + ClosedLoopScenario.cs v1.1 (M-3 CR/LF sanitization + exception_stack line), ScenarioManifest.cs v1.1 (L-1 ReadOnlyCollection wrappers), ScenarioResult.cs / IScenario.cs / ScenarioContext.cs v1.1 (doc), TestingStrategyConstants.cs v1.4 (SCENARIO_PATH_CROSS_SPEC_PREFIX), ScenarioRunnerTests.cs v1.1 (12→18 tests), AgentMovementScenarios.cs v1.1 (L-2 InvariantCulture details, L-3 exact position equality for T-AM-115). Prior June 10, 2026 (Stage 0 closed-loop scenario harness landed — Spec #19 §3.3.3 ScenarioRunner pulled forward from the Stage 0+1 schedule after the third consecutive spec (Ball Physics AR-7, Agent Movement AR-12/AR-13) where H/M-class closed-loop defects were encoded by pure-function unit suites rather than caught by them. New `src/testing-strategy/` files (9 .cs): ScenarioStatus, ScenarioResult, ScenarioManifest, ScenarioEnvelope, ScenarioContext, IScenario, ClosedLoopScenario, ScenarioIndex, ScenarioRunner; new `src/testing-strategy/Tests/` (testing-strategy-tests.asmdef + ScenarioRunnerTests.cs, 12 contract tests); TestingStrategyConstants.cs v1.3 adds SCENARIO_MANIFEST_FORMAT_VERSION. First fixture corpus: T-AM-110..115 migrated out of AgentMovementTests.cs (v2.3) into AgentMovementScenarios.cs (bodies + A.1 manifests) + AgentMovementScenarioTests.cs (`sim_<scenario>` Simulation-layer tests); agent-movement-tests.asmdef gains the testing-strategy reference; docs/specs/agent-movement/test-plan.md v0.4. A `src/testing-strategy/` per-file section was added to this manifest (the June 7 scaffold had been recorded only in this header note). Prior June 9, 2026 (Ball Physics #1 AR-7/AR-8 fix pass: BallGroundInteraction.cs v1.3.1, BallPhysicsCore.cs v1.4, BallPhysicsConstants.cs v1.8, tests/BallPhysicsCoreTests.cs v1.5, tests/BallIntegrationTests.cs v1.4 (new Airborne_FastDescent_Bounces_NoHoverDeadlock test); docs/specs/ball-physics/section-3-1-8-to-3-1-14.md row 2.8; spec-error-log.md v1.23 (ERR-001-001..003). No files added or removed. Prior June 8, 2026 (Event System #17 boot-wiring smoke test landed: new `src/event-system/tests/EventBusWiringSmokeTests.cs` v0.4 — SMOKE-EVT-WIRING-001 drives boot → publish-one-per-spec → DrainTick → SerializeLedger and asserts SHA-256 digest stability across the 6 currently-wired EventBusRegistrar.Initialize() call sites (Pass / Shot / Perception / Decision / Heading / Goalkeeper); Agent Movement (#2) is a `[CROSS-PENDING]` slot pending the AM-side registrar; golden digest pinned via Assert.Inconclusive until that lands. AR-1 (1H+1M+4L) + AR-2 (0H+1M+4L) + AR-3 (0H+0M+2L cycle-stop) adversarial review cycles complete. `event-system-tests.asmdef` extended with 6 production spec references for the smoke test (Editor-only, no production layering impact — test assemblies are infrastructure per src/CLAUDE.md). Prior June 7, 2026 (AR-hardening sweep complete + test scaffolding landed. New source files: `src/agent-movement/AssemblyInfo.cs` (InternalsVisibleTo for tooling-override factory access). New test files: `src/ball-physics/tests/EnumOrdinalStabilityTests.cs` (AR-6 L-3 — locks int ordinals for all 6 public enums), `src/ball-physics/tests/BodyPartCoefficientsTests.cs` (AR-4 L-2 — throw-on-unknown + catalogue round-trip), `src/ball-physics/tests/SurfacePropertiesTests.cs` (AR-4 L-2 — throw-on-unknown across all 4 Get* methods), `src/agent-movement/Tests/AgentMovementTests.cs` v2.0 (T-AM-001..018, T-AM-030..033, T-AM-040..043 — 18 NUnit tests across 4 fixtures), `src/agent-movement/Tests/AgentMovementUnitTests.cs` (T-AM-007..107 — 59 NUnit tests across 7 fixtures). New tracking doc: `docs/specs/agent-movement/test-plan.md` v0.2 (T-AM-NNN catalogue). File splits: Ball Physics — `BallCollision.cs` split into `BodyPart.cs` + `RestartType.cs` + `KickResult.cs` + `BodyPartCoefficients.cs` + `BallCollision.cs` per FILE NAMING (AR-2 L-2). Performance Optimization — `TraceChannel.cs` split into `ChannelVerbosity.cs` + `ChannelSamplingRule.cs` + `ChannelDeterminismClass.cs` + `TraceChannelDescriptor.cs` + `TraceChannelRegistry.cs` (AR-1 H-1). New Testing Strategy assembly: `src/testing-strategy/` with 14 files (TestingStrategyConstants, TestTier, TestLayer, GoldenVectorKind/Entry/Result/Runner, DeterminismTierKind/Result, DeterminismSuiteResult, DeterminismGate, PerfGateReport, PerfGateRunner, testing-strategy.asmdef). Prior May 31: AR-4 fixes in event-system.))
**Purpose:** Canonical inventory aligned with the current folder-based spec layout in `docs/specs/`.

---

## Scope and Authority

This manifest supersedes the legacy flat-file inventory that referenced historical filenames such as `*_v1_0.md`.

- **Canonical spec status source:** `docs/specs/SPEC_INDEX.md`
- **Canonical schedule source:** `docs/tracking/PROGRESS.md`
- **Canonical cross-spec issue source:** `docs/tracking/spec-error-log.md`

Use this file to track the **current folder structure**, not legacy per-version filenames.

---

## Source Files

| File | Purpose |
|------|---------|
| `src/CLAUDE.md` | Coding guide: C# naming, constant catalogues, Unity project structure, build/test commands. Created May 19, 2026 when coding began. At v1.23 as of May 30, 2026. |

### Spec #1 — Ball Physics (`src/ball-physics/`)

> Relocated May 30, 2026 from `src/Core/Physics/Ball/` to spec-canonical `src/ball-physics/` via `git mv`; history preserved. Two asmdef files added.

| File | Purpose |
|------|---------|
| `src/ball-physics/ball-physics.asmdef` | Assembly definition (references TacticalDirector.DeterministicSim; autoReferenced true) |
| `src/ball-physics/BallPhysicsConstants.cs` | `[FIXED]` / `[GT]` / `[DERIVED]` / `[CROSS]` constant catalogue for Ball Physics |
| `src/ball-physics/BallState.cs` | Mutable value-type game state for the ball (position, velocity, spin, ground contact) |
| `src/ball-physics/BallPhysicsCore.cs` | Core physics calculations: gravity, drag, Magnus effect |
| `src/ball-physics/BallStateMachine.cs` | State machine: CONTROLLED ↔ AIRBORNE ↔ ROLLING transitions |
| `src/ball-physics/BallGroundInteraction.cs` | Ground friction and rolling dynamics |
| `src/ball-physics/BallCollision.cs` | Ball-specific collision response (detection geometry lives in `collision-system/`) |
| `src/ball-physics/BallEventLogger.cs` | Event/logging infrastructure for ball physics events |
| `src/ball-physics/SurfaceProperties.cs` | Surface-specific physics parameters (grass, artificial turf, etc.) |
| `src/ball-physics/tests/ball-physics-tests.asmdef` | Test assembly definition (EditMode; references ball-physics.asmdef + testing-strategy.asmdef; autoReferenced false) |
| `src/ball-physics/tests/BallPhysicsCoreTests.cs` | Unit tests for core physics calculations |
| `src/ball-physics/tests/BallIntegrationTests.cs` | Integration tests for full ball physics pipeline |
| `src/ball-physics/tests/BallStateMachineTests.cs` | Unit tests for ball state machine transitions |
| `src/ball-physics/tests/BodyPartCoefficientsTests.cs` | AR-4 L-2 — throw-on-unknown + catalogue round-trip for the per-body-part coefficients |
| `src/ball-physics/tests/SurfacePropertiesTests.cs` | AR-4 L-2 — throw-on-unknown + catalogue round-trip across all 4 SurfaceProperties Get* methods |
| `src/ball-physics/tests/EnumOrdinalStabilityTests.cs` | AR-6 L-3 — locks int ordinals for all 6 public ball-physics enums |
| `src/ball-physics/tests/BallPhysicsScenarios.cs` | Per-spec closed-loop scenario corpus (#19 §3.3.1): drop-and-rebound (AR-7 H-1 / ERR-001-001 lock) + fast-descent-grounds-out (AR-7 H-2 hover-deadlock lock); bodies + Appendix A.1 manifests, envelope windows from a numerical mirror of the fixed model |
| `src/ball-physics/tests/BallPhysicsScenarioTests.cs` | sim_<scenario> Simulation-layer tests running the Spec #1 corpus through the #19 ScenarioRunner |

### Spec #2 — Agent Movement (`src/agent-movement/`)

| File | Purpose |
|------|---------|
| `src/agent-movement/AgentMovementConstants.cs` | All movement constants: `MovementThresholds` / `FatigueRates` / `LocomotionConstants` / `DirectionalConstants` / `TurnConstants` / `OscillationGuardConstants` / `SafetyConstants` / `PlayerAttributeConstants` (8 nested classes) |
| `src/agent-movement/AgentMovementState.cs` | Enum: `AgentMovementState` (7 locomotion states) |
| `src/agent-movement/GroundedReason.cs` | Enum: `GroundedReason` (NONE / COLLISION / SLIDING_TACKLE / DIVING_HEADER) |
| `src/agent-movement/FacingMode.cs` | Enum: `FacingMode` (AUTO_ALIGN / TARGET_LOCK) |
| `src/agent-movement/DecelerationMode.cs` | Enum: `DecelerationMode` (CONTROLLED / EMERGENCY) |
| `src/agent-movement/AgentState.cs` | Mutable value-type game state for an agent (ref-mutated, not readonly — pending C# version pin) |
| `src/agent-movement/PlayerAttributes.cs` | Player skill ratings (1–20 scale) used by movement formulas |
| `src/agent-movement/PerformanceContext.cs` | Performance modifier gateway (fatigue, injury, surface) |
| `src/agent-movement/MovementCommand.cs` | Input command structure from Decision Tree / tactical layer |
| `src/agent-movement/AgentMovementSystem.cs` | 12-step 60 Hz pipeline orchestrator |
| `src/agent-movement/AgentStateMachine.cs` | Pure state evaluator (no side effects) |
| `src/agent-movement/OscillationGuard.cs` | Ring-buffer anti-oscillation guard; v1.5 adds the GetState/RestoreState serialization seam (Match Engine Phase B step B0) |
| `src/agent-movement/OscillationGuardState.cs` | Plain-data snapshot DTO of OscillationGuard's ring-buffer state (Phase B step B0 serialization seam; parallel to DeterministicSim RngStreamState) |
| `src/agent-movement/AgentLocomotion.cs` | Acceleration / deceleration formulas |
| `src/agent-movement/AgentTurning.cs` | Turn rate / lean angle / stumble probability |
| `src/agent-movement/AgentDirectionalMovement.cs` | Directional multipliers / facing update |
| `src/agent-movement/AgentSafetySystem.cs` | NaN detection / speed clamp / pitch boundary enforcement |
| `src/agent-movement/agent-movement.asmdef` | Assembly definition (no references; autoReferenced true) |
| `src/agent-movement/AssemblyInfo.cs` | `[InternalsVisibleTo("TacticalDirector.AgentMovement.Tests")]` (added June 4, 2026 for T-AM-030..032 seam) |
| `src/agent-movement/Tests/AgentMovementTests.cs` | v2.3 — Regression-anchored integration roster T-AM-001..018 / 030..033 / 040..043 (closed-loop fixture T-AM-110..115 migrated to the #19 scenario harness June 10, 2026) |
| `src/agent-movement/Tests/AgentMovementUnitTests.cs` | Pure-function unit coverage T-AM-007..009 / 019..023 / 034..039 / 044..047 / 050..052 / 070..109 (T-AM-108..109 added in AR-12 fix pass, June 9, 2026) |
| `src/agent-movement/Tests/AgentMovementScenarios.cs` | Per-spec closed-loop scenario corpus T-AM-110..115 (#19 §3.3.1): scenario bodies + Appendix A.1 manifests; first fixture corpus on the #19 ScenarioRunner (June 10, 2026) |
| `src/agent-movement/Tests/AgentMovementScenarioTests.cs` | Simulation-layer executable tests (`sim_<scenario>` per #19 §3.1.4) running the T-AM-110..115 corpus through ScenarioRunner.Run |
| `src/agent-movement/Tests/OscillationGuardSerializationTests.cs` | B0-001..004 — OscillationGuard GetState/RestoreState CanonicalSerializer round-trip + behavioural-equivalence locks (Match Engine Phase B step B0) |
| `src/agent-movement/Tests/agent-movement-tests.asmdef` | Test assembly definition (EditMode; references agent-movement.asmdef + testing-strategy.asmdef + deterministic-sim.asmdef for the B0 CanonicalSerializer round-trip) |

### Spec #3 — Collision System (`src/collision-system/`)

| File | Purpose |
|------|---------|
| `src/collision-system/CollisionSystemConstants.cs` | All constant catalogue: `[FIXED]` / `[GT]` / `[DERIVED]` / `[CROSS]` constants for the collision system |
| `src/collision-system/CollisionDetection.cs` | Broad-phase and narrow-phase collision detection logic |
| `src/collision-system/SpatialHashGrid.cs` | Spatial hash grid for broad-phase agent and ball proximity queries |
| `src/collision-system/CollisionManifold.cs` | Contact manifold: penetration depth, contact normal, contact point |
| `src/collision-system/CollisionEvent.cs` | Struct event published to the event bus on confirmed collision |
| `src/collision-system/CollisionResponse.cs` | Impulse-based collision response calculations |
| `src/collision-system/CollisionSystem.cs` | Main orchestrator: 60 Hz pipeline for all agent–agent and agent–ball collisions |
| `src/collision-system/CollisionPairBitfield.cs` | Bitfield tracking already-processed pairs within a tick (prevents double-processing) |
| `src/collision-system/AgentAgentCollisionResult.cs` | Result struct for an agent–agent collision resolution |
| `src/collision-system/AgentBallCollisionData.cs` | Data struct describing an agent–ball contact |
| `src/collision-system/AgentPhysicalProperties.cs` | Physical properties (mass, radius, restitution) per agent |
| `src/collision-system/BallCollisionHandler.cs` | Ball-specific collision response (delegates geometry to `ball-physics/BallCollision.cs`) |
| `src/collision-system/ContactForceData.cs` | Contact force magnitude and direction for logging/events |
| `src/collision-system/ContactType.cs` | Enum: contact classification (SLIDE, SHOULDER, BLOCK, FOUL) |
| `src/collision-system/ContactTypeClassifier.cs` | Classifies a collision manifold into a `ContactType` |
| `src/collision-system/CollisionType.cs` | Enum: collision kind (AGENT_AGENT, AGENT_BALL, AGENT_POST, AGENT_BOUNDARY) |
| `src/collision-system/DeterministicRNG.cs` | Thin wrapper around SplitMix64 for foul-roll and stumble-roll RNG draws |
| `src/collision-system/ICollisionEventConsumer.cs` | Interface for systems that consume collision events (Spec #3 §3.4.2 consumer pattern) |
| `src/collision-system/collision-system.asmdef` | Assembly definition for the collision-system assembly |
| `src/collision-system/tests/collision-system-tests.asmdef` | Test assembly definition (EditMode; references collision-system.asmdef) |

### Spec #4 — First Touch Mechanics (`src/first-touch/`)

| File | Purpose |
|------|---------|
| `src/first-touch/FirstTouchConstants.cs` | All constant catalogue for First Touch |
| `src/first-touch/FirstTouchContext.cs` | Input context struct: incoming ball state, agent state, env conditions |
| `src/first-touch/FirstTouchResult.cs` | Final output struct: displaced ball state + possession outcome |
| `src/first-touch/TouchResult.cs` | Intermediate result of touch quality evaluation |
| `src/first-touch/FirstTouchSystem.cs` | Main orchestrator: entry point for a first-touch resolution |
| `src/first-touch/BallDisplacementProcessor.cs` | Computes post-touch ball displacement vector from error and control quality |
| `src/first-touch/ControlQualityCalculator.cs` | Calculates control quality score (0–1) from player attributes and context |
| `src/first-touch/OrientationDetector.cs` | Detects player body orientation relative to the incoming ball direction |
| `src/first-touch/PossessionStateMachine.cs` | Manages possession state transitions (LOOSE → CONTROLLED → POSSESSED) |
| `src/first-touch/PressureEvaluator.cs` | Evaluates nearby-defender pressure scalar from agent positions |
| `src/first-touch/PressureResult.cs` | Pressure evaluation output struct |
| `src/first-touch/TouchRadiusCalculator.cs` | Computes the acceptance radius for a first-touch attempt |
| `src/first-touch/IAgentMovementSystem.cs` | Interface boundary to Agent Movement (#2) (read-only query surface) |
| `src/first-touch/IBallPhysicsSystem.cs` | Interface boundary to Ball Physics (#1) (read-only query surface) |
| `src/first-touch/IFirstTouchSystem.cs` | Public interface for consumers of the First Touch system |
| `src/first-touch/first-touch.asmdef` | Assembly definition (references BallPhysics, AgentMovement, CollisionSystem) |
| `src/first-touch/Tests/first-touch-tests.asmdef` | EditMode test assembly definition |
| `src/first-touch/Tests/FirstTouchTests.cs` | CQ/TR/PR/OR/PO/EC/BD/VS + invariant suite + IT-001..008 stubs (v1.2 AR-7 re-derivation) |
| `src/first-touch/AssemblyInfo.cs` | [InternalsVisibleTo("TacticalDirector.FirstTouch.Tests")] for the scenario corpus |
| `src/first-touch/Tests/FirstTouchScenarios.cs` | Closed-loop scenario corpus on the #19 ScenarioRunner: heavy-touch-runs-on (ERR-004-003 lock) + interception-chain-anchors-at-displaced-ball (ERR-004-004 / §3.4.5 lock) |
| `src/first-touch/Tests/FirstTouchScenarioTests.cs` | sim_<scenario> Simulation-layer tests running the corpus through ScenarioRunner |

### Spec #5 — Pass Mechanics (`src/pass-mechanics/`)

| File | Purpose |
|------|---------|
| `src/pass-mechanics/PassMechanicsConstants.cs` | All constant catalogue: physical profiles, error model, timing constants |
| `src/pass-mechanics/PassRequest.cs` | Input struct: passer, target agent/position, requested pass type |
| `src/pass-mechanics/PassResult.cs` | Output struct: actual ball velocity applied + outcome classification |
| `src/pass-mechanics/PassAttemptEvent.cs` | Tier A struct event (IEventA; ordinal 0x0C; 12-byte header): published when a pass is initiated |
| `src/pass-mechanics/PassCancelledEvent.cs` | Tier A struct event (IEventA; ordinal 0x0D; 12-byte header): published when a pass is cancelled |
| `src/pass-mechanics/CancelReason.cs` | Enum: TackleInterrupt — reason a pass was cancelled |
| `src/pass-mechanics/PassExecutor.cs` | Main orchestrator: executes the full pass pipeline |
| `src/pass-mechanics/PassVelocityCalculator.cs` | Calculates launch velocity from physical profile and player attributes |
| `src/pass-mechanics/PassErrorCalculator.cs` | Error / accuracy model: direction and speed deviation |
| `src/pass-mechanics/PassTargetResolver.cs` | Resolves intended target position from agent reference |
| `src/pass-mechanics/PassTypeProfiles.cs` | Factory: returns the `PhysicalProfile` for a given `PassType` |
| `src/pass-mechanics/PhysicalProfile.cs` | Struct: physical parameters (speed range, spin, launch angle) per pass type |
| `src/pass-mechanics/PassExecutorState.cs` | Plain-data snapshot DTO of PassExecutor's cross-tick state-machine + in-flight fields (Match Engine Phase C C0 seam; PhysicalProfile recomputed on restore) |
| `src/pass-mechanics/PassType.cs` | Enum: GROUND, DRIVEN, LOB, CHIP, CROSS |
| `src/pass-mechanics/CrossSubType.cs` | Enum: cross sub-type (LOW, DRIVEN, FLOATED, CUTBACK) |
| `src/pass-mechanics/SpinType.cs` | Enum: spin type (TOPSPIN, BACKSPIN, SIDESPIN, NONE) |
| `src/pass-mechanics/PassOutcome.cs` | Enum/struct: outcome classification (ACCURATE, MISPLACED, INTERCEPTED, OUT) |
| `src/pass-mechanics/PassAgentAttributes.cs` | Agent skill attributes consumed by the pass system (passing, vision, weak-foot) |
| `src/pass-mechanics/PassAgentState.cs` | Agent state consumed by the pass system (fatigue, stamina, body orientation) |
| `src/pass-mechanics/IPassAgentQuery.cs` | Interface to query agent attributes and state |
| `src/pass-mechanics/IPassBallSystem.cs` | Interface to Ball Physics (#1) for applying kick velocity |
| `src/pass-mechanics/IPassCollisionQuery.cs` | Interface to Collision System (#3) for interception queries |
| `src/pass-mechanics/EventBusStub.cs` | Wired to EventBus.Publish; 3-tier generic IEventA/B/C overloads |
| `src/pass-mechanics/EventBusRegistrar.cs` | Boot-time RegisterExternalRow<T>() for PassAttemptEvent (0x0C) + PassCancelledEvent (0x0D) |

### Spec #6 — Shot Mechanics (`src/shot-mechanics/`)

| File | Purpose |
|------|---------|
| `src/shot-mechanics/ShotMechanicsConstants.cs` | All GT/Fixed/Cross constants (velocity, angle, spin, error, body mechanics, weak-foot, timing) |
| `src/shot-mechanics/ContactZone.cs` | Enum: Centre / BelowCentre / OffCentre — where on the ball the foot contacts |
| `src/shot-mechanics/ShotOutcome.cs` | Enum: Completed / Cancelled / Invalid / Initiated |
| `src/shot-mechanics/ShotCancelReason.cs` | Enum: TackleInterrupt — reason a shot was cancelled |
| `src/shot-mechanics/ShotRequest.cs` | Input struct from Decision Tree (#8) to ShotExecutor |
| `src/shot-mechanics/ShotResult.cs` | Output struct returned by ShotExecutor (velocity, spin, error offset, BMS, outcome) |
| `src/shot-mechanics/ShotExecutorState.cs` | Plain-data snapshot DTO of ShotExecutor's cross-tick state-machine + in-flight fields (Match Engine Phase C C0 seam) |
| `src/shot-mechanics/ShotAgentAttributes.cs` | Agent attribute snapshot (Finishing, LongShots, Composure, KickPower, Technique, WeakFootRating, Fatigue) |
| `src/shot-mechanics/ShotAgentState.cs` | Agent physical state snapshot (Position, Velocity, FacingDirection, CurrentState) |
| `src/shot-mechanics/ShotExecutedEvent.cs` | Tier A struct event (IEventA; ordinal 0x01; 12-byte header): published at CONTACT completion after Ball.ApplyKick() |
| `src/shot-mechanics/ShotCancelledEvent.cs` | Tier A struct event (IEventA; ordinal 0x0E; 12-byte header): published when a tackle interrupt fires during WINDUP |
| `src/shot-mechanics/ShotAnimationData.cs` | Tier C struct event (IEventC; ordinal 0x0F): animation data stub for Animation System (unconsumed at Stage 0) |
| `src/shot-mechanics/BodyMechanicsResult.cs` | Output struct from BodyMechanicsEvaluator (Score, CQM, StumbleTriggered) |
| `src/shot-mechanics/GoalGeometry.cs` | Value struct: goal width, height, goal-line X, post Y coords, crossbar Z |
| `src/shot-mechanics/IShotVelocityCalculator.cs` | Interface enabling EC-008 NaN injection seam only (ShotVelocityCalculator + NaNVelocityStub) |
| `src/shot-mechanics/IShotBallSystem.cs` | Interface: IsBallPossessedBy() + ApplyKick() to Ball Physics |
| `src/shot-mechanics/IShotAgentQuery.cs` | Interface: GetAttributes() + GetState() to Agent Movement |
| `src/shot-mechanics/IShotCollisionQuery.cs` | Interface: tackle flag poll + pressure scalar to Collision System |
| `src/shot-mechanics/GoalGeometryProvider.cs` | Static access point for goal geometry; test override seam for SP-009 |
| `src/shot-mechanics/ShotVelocityCalculator.cs` | §3.2 velocity formula; stateless singleton; implements IShotVelocityCalculator |
| `src/shot-mechanics/ShotLaunchAngleCalculator.cs` | §3.3 launch angle formula (base angle + power/spin lift + body lean + body shape); pure static |
| `src/shot-mechanics/ShotSpinCalculator.cs` | §3.4 spin vector assembly (topspin / backspin / sidespin); pure static |
| `src/shot-mechanics/ShotPlacementResolver.cs` | §3.5 goal-relative placement → world-space aim direction; also applies error offset |
| `src/shot-mechanics/BodyMechanicsEvaluator.cs` | §3.7 body mechanics score (run-up angle, plant foot, velocity, body lean); pure static |
| `src/shot-mechanics/WeakFootPenaltyApplier.cs` | §3.8 weak-foot error multiplier and velocity multiplier; pure static |
| `src/shot-mechanics/ShotErrorCalculator.cs` | §3.6 deterministic angular error (magnitude, direction hash, offset); pure static |
| `src/shot-mechanics/ShotEventEmitter.cs` | Publishes ShotExecutedEvent, ShotCancelledEvent, ShotAnimationData via EventBusStub |
| `src/shot-mechanics/EventBusStub.cs` | Wired to EventBus.Publish; 3-tier generic IEventA/B/C overloads |
| `src/shot-mechanics/EventBusRegistrar.cs` | Boot-time RegisterExternalRow<T>() for ShotExecutedEvent (0x01) + ShotCancelledEvent (0x0E) + ShotAnimationData (0x0F) |
| `src/shot-mechanics/ShotExecutor.cs` | Sealed orchestrator: 5-state machine (Idle→Windup→Contact→FollowThrough→Complete) |
| `src/shot-mechanics/Tests/NaNVelocityStub.cs` | #if UNITY_EDITOR\|\|DEVELOPMENT_BUILD; returns float.NaN for EC-008 FM-05 recovery test |

### Spec #10 — Heading Mechanics (`src/heading-mechanics/`)

| File | Purpose |
|------|---------|
| `src/heading-mechanics/heading-mechanics.asmdef` | Assembly definition (references agent-movement, ball-physics, collision-system, event-system; added event-system ref May 30, 2026) |
| `src/heading-mechanics/HeadingMechanicsConstants.cs` | All GT/Fixed/Cross/Derived constants (§3.1); region order Fixed→Derived→Cross→GT |
| `src/heading-mechanics/ContactQualityLabel.cs` | Enum: Early / OnTime / Late — telemetry only; KD-2 |
| `src/heading-mechanics/MistimedDirection.cs` | Enum: None / Early / Late — eligibility output |
| `src/heading-mechanics/FailureCause.cs` | Enum: MistimedEarly / MistimedLate / PositionedPoorly / DisturbedInDuel |
| `src/heading-mechanics/SetPieceContext.cs` | Enum: OpenPlay / Corner / FreeKick — telemetry only |
| `src/heading-mechanics/HeadingAgentAttributes.cs` | Struct: Heading/Strength/Balance [1-20], Fatigue [0,1], TeamId |
| `src/heading-mechanics/HeaderIntent.cs` | Struct: PowerIntent/ContactPointIntent/TargetIntent/AttemptCommittedTick/SetPieceContext (locked at commit; KD-17) |
| `src/heading-mechanics/HeaderContactState.cs` | Struct: per-attempt mutable state (JumpStartFrame, quality, disturbance, etc.) |
| `src/heading-mechanics/EligibilityResult.cs` | Struct: IsEligible, PredictedContactFrame, IdealContactFrame, MistimedDirection |
| `src/heading-mechanics/HeaderExecutedEvent.cs` | Tier B struct event (IEventB; ordinal 0x12; 12-byte header): published on successful contact |
| `src/heading-mechanics/HeaderAttemptFailedEvent.cs` | Tier C struct event (IEventC; ordinal 0x13): published on failure; no ball-state modification |
| `src/heading-mechanics/ContestedDuelContext.cs` | Struct: DuelId, ParticipantCount, WinnerAgentId, BufferStartIndex |
| `src/heading-mechanics/IHeadingBallSystem.cs` | Interface: GetBallState + ApplyKick |
| `src/heading-mechanics/IHeadingRngService.cs` | Interface: NextFloat + NextGaussian |
| `src/heading-mechanics/HeadingRngServiceStub.cs` | Stage 0 SplitMix64 stub; replace at Stage 1 with #16 wiring |
| `src/heading-mechanics/EventBusStub.cs` | Wired to EventBus.Publish; 3-tier generic IEventA/B/C overloads |
| `src/heading-mechanics/EventBusRegistrar.cs` | Boot-time RegisterExternalRow<T>() for HeaderExecutedEvent (0x12 Tier B) + HeaderAttemptFailedEvent (0x13 Tier C) |
| `src/heading-mechanics/HeadingEligibility.cs` | Pure eligibility predicate (§3.2); no side effects |
| `src/heading-mechanics/HeadingJumpKinematics.cs` | FM-010-001 JumpReach + Stage 0 synthetic parabolic Z (KD-18) |
| `src/heading-mechanics/HeadingContactQuality.cs` | FM-010-002 contact-quality scalar (asymmetric timing + point error) |
| `src/heading-mechanics/HeadingPowerAngle.cs` | FM-010-003 outgoing speed + reflection geometry + own-goal flag (§3.8) |
| `src/heading-mechanics/HeadingSpinTransfer.cs` | FM-010-004 head angular-velocity derivation + outgoing spin (§3.6) |
| `src/heading-mechanics/HeadingDuelResolution.cs` | FM-010-005 duel scoring; ICollisionEventConsumer; pre-allocated buffers |
| `src/heading-mechanics/HeadingTelemetry.cs` | Stage 0 stub; emits §2.4 heading.* trace-pipeline channels at Stage 0+1 |
| `src/heading-mechanics/HeadingMechanics.cs` | 60 Hz orchestrator; two-pass per-frame loop (§4.6) |

### Spec #11 — Goalkeeper Mechanics (`src/goalkeeper-mechanics/`)

| File | Purpose |
|------|---------|
| `src/goalkeeper-mechanics/goalkeeper-mechanics.asmdef` | Assembly definition (references agent-movement, ball-physics, collision-system, event-system; added event-system ref May 30, 2026) |
| `src/goalkeeper-mechanics/GoalkeeperConstants.cs` | All GT/Fixed/Cross/Derived constants (§3.4); region order Fixed→Derived→Cross→GT; ~79 constants; 4 draw-site IDs |
| `src/goalkeeper-mechanics/GoalkeeperState.cs` | Enum: Resting/Set/Anticipate/Diving/Airborne/HandsOnBall/Recovering/Distributing/Rushing/OneOnOne/Smothered |
| `src/goalkeeper-mechanics/HandlingQualityLabel.cs` | Enum: Caught/Parried/Deflected/Spilled/Missed — telemetry only (KD-2) |
| `src/goalkeeper-mechanics/ReactionLabel.cs` | Enum: Reflexive/Standard/Sluggish — telemetry only (KD-2) |
| `src/goalkeeper-mechanics/FailureCause.cs` | Enum: MissedContact/MistimedDive/WrongDirection/OutOfReach/DisturbedInDuel |
| `src/goalkeeper-mechanics/ClaimType.cs` | Enum: Cross/Aerial/OneOnOne/ShotCatch — telemetry only |
| `src/goalkeeper-mechanics/RushPhase.cs` | Enum: Launched/InFlight/Reached/Aborted |
| `src/goalkeeper-mechanics/AbortReason.cs` | Enum: BallIntercepted/BallCleared/AttackerBeatGK |
| `src/goalkeeper-mechanics/BodyPartEnum.cs` | Enum: Hand/Head/Body/Foot — collision routing (KD-14) |
| `src/goalkeeper-mechanics/HandEnum.cs` | Enum: Left/Right/Either — anatomy lookup KD-1 carve-out (not physics input) |
| `src/goalkeeper-mechanics/DeliveryKind.cs` | Enum: Throw/Roll/Kick — kinematic profile lookup KD-1 carve-out (not physics input) |
| `src/goalkeeper-mechanics/SaveIntent.cs` | Struct: TargetHand/ClutchFirmness/DeflectionTarget/AttemptCommittedTick (from Decision Tree #8) |
| `src/goalkeeper-mechanics/ClaimIntent.cs` | Struct: TargetContactPoint/ClutchFirmness/AttemptCommittedTick |
| `src/goalkeeper-mechanics/DistributeIntent.cs` | Struct: DeliveryKind/TargetReceiverId/TargetPoint/PowerIntent/SpinIntent |
| `src/goalkeeper-mechanics/RushIntent.cs` | Struct: RushTarget/CommitmentLevel/AttemptCommittedTick |
| `src/goalkeeper-mechanics/GkContactState.cs` | Struct: per-attempt mutable state (PredictedContactFrame, ReactionWindowAchieved, HandlingQualityScalar, etc.) |
| `src/goalkeeper-mechanics/CrossClaimDuelContext.cs` | Struct: DuelId, ParticipantCount, WinnerAgentId, ContactBodyPart, BufferStartIndex |
| `src/goalkeeper-mechanics/GoalkeeperAgentAttributes.cs` | Struct: all GK attributes [1-20] + Fatigue [0,1] + normalised accessors |
| `src/goalkeeper-mechanics/GoalkeeperPositioningContract.cs` | Struct: KD-13 consumer contract — holds gkBaselineSlot + reactive-radius bounds logic |
| `src/goalkeeper-mechanics/SaveAttemptedEvent.cs` | Tier A struct event (IEventA; ordinal 0x14; 12-byte header): published on every save attempt; includes telemetry labels |
| `src/goalkeeper-mechanics/BallClaimedEvent.cs` | Tier A struct event (IEventA; ordinal 0x15; 12-byte header): published on Caught save; includes releaseTickEarliest (6-second rule) |
| `src/goalkeeper-mechanics/DistributionExecutedEvent.cs` | Tier A struct event (IEventA; ordinal 0x16; 12-byte header): published when distribution passIntent is emitted to Pass Mechanics #5. v1.2: AR-2 fix — int? TargetReceiverId → int (sentinel -1); nullable padding bytes are non-deterministic in null case. |
| `src/goalkeeper-mechanics/GoalkeeperRushEvent.cs` | Tier C struct event (IEventC; ordinal 0x17): published on rush launch, update, and abort |
| `src/goalkeeper-mechanics/IGoalkeeperBallSystem.cs` | Interface: GetBallState + ApplyKick + SetPossessor |
| `src/goalkeeper-mechanics/IGoalkeeperRngService.cs` | Interface: NextFloat + NextGaussian (4 registered draw sites) |
| `src/goalkeeper-mechanics/GoalkeeperStateMachine.cs` | Pure state evaluator: EvaluateTacticalTransition + EvaluatePhysicsTransition; no side effects |
| `src/goalkeeper-mechanics/GoalkeeperReactionPipeline.cs` | §3.2 formulas: ComputeShotDetectedTickMs / ComputeRequiredReactionMs / ComputeReactionWindowAchieved / ComputeReactionLabel; pure static |
| `src/goalkeeper-mechanics/GoalkeeperDiveKinematics.cs` | §3.3 Stage 0 synthetic dive trajectory: launch impulse, timing jitter, parabolic Z, reach envelope; pure static |
| `src/goalkeeper-mechanics/GoalkeeperHandlingQuality.cs` | §3.5 handling-quality scalar + band-to-action velocity helpers (parry/deflect/spill); pure static |
| `src/goalkeeper-mechanics/GoalkeeperCrossClaimDuel.cs` | §3.6 body-part determination + duel-score arithmetic + tiebreak; implements ICollisionEventConsumer |
| `src/goalkeeper-mechanics/GoalkeeperRushDispatch.cs` | §3.7 rush launch impulse + per-frame update; pure static |
| `src/goalkeeper-mechanics/GoalkeeperDistribution.cs` | §3.8 release-point geometry, windup duration, accuracy coefficient, F-05/F-09 target validation; pure static |
| `src/goalkeeper-mechanics/GoalkeeperTelemetry.cs` | Stage 0 stub; emits §2.4 gk.* trace-pipeline channels at Stage 0+1 (12 channels) |
| `src/goalkeeper-mechanics/EventBusStub.cs` | Wired to EventBus.Publish; 3-tier generic IEventA/B/C overloads |
| `src/goalkeeper-mechanics/EventBusRegistrar.cs` | Boot-time RegisterExternalRow<T>() for SaveAttemptedEvent (0x14) + BallClaimedEvent (0x15) + DistributionExecutedEvent (0x16) + GoalkeeperRushEvent (0x17) |
| `src/goalkeeper-mechanics/GoalkeeperMechanics.cs` | Main 10 Hz + 60 Hz orchestrator: state machine, dive kinematics, handling quality, cross-claim duels, rush, distribution; constructor-injected |

### Perception System (#7) — 14 files

| File | Role |
|------|------|
| `src/perception-system/perception-system.asmdef` | Assembly definition (AI layer; references AgentMovement, BallPhysics, CollisionSystem, FirstTouch, EventSystem; added event-system ref May 30, 2026) |
| `src/perception-system/PerceptionConstants.cs` | All GT/Fixed/Derived/Cross constants (§3.10): 18 spec constants + system-sizing constants |
| `src/perception-system/PerceptionAgentAttributes.cs` | Struct: Decisions, Anticipation, TeamId, IsHalfTurned snapshot (§4.2.2) |
| `src/perception-system/FilteredView.cs` | FilteredView, PerceptionDiagnostics, PerceivedAgent, ShoulderCheckAnimData, OcclusionDebugRecord, PerceivedAgentDebug struct definitions (§3.7) |
| `src/perception-system/PerceptionEvents.cs` | Tier C struct event PerceptionRefreshEvent (IEventC; ordinal 0x10) + RefreshTrigger enum (§4.6.3) |
| `src/perception-system/EventBusStub.cs` | Wired to EventBus.Publish; 3-tier generic IEventA/B/C overloads |
| `src/perception-system/EventBusRegistrar.cs` | Boot-time RegisterExternalRow<T>() for PerceptionRefreshEvent (0x10) |
| `src/perception-system/FovCalculator.cs` | FoV formula (§3.1) + angular candidacy test + blind-side and peripheral arc predicates; static, no side effects |
| `src/perception-system/OcclusionFilter.cs` | Shadow cone geometry (§3.2.3) + opponent occlusion test; Stage 0: opponents only (OQ-1); static, no side effects |
| `src/perception-system/PressureEvaluator.cs` | PressureScalar formula (§3.6); reused verbatim from First Touch #4 §3.5; static, no side effects |
| `src/perception-system/BallPerceptionEvaluator.cs` | Ball range/FoV/occlusion tests + BallStalenessFrames tracking (§3.5); no L_rec (OQ-2); static, no side effects. v1.2: AR-2 L-1 removed unused prevBallVisible parameter. |
| `src/perception-system/RecognitionLatencyTracker.cs` | Per-(observer,target) latency counters, L_rec formula, half-turn peripheral bonus, Wang/Jenkins deterministic hash (§3.3); INV-10 pre-allocated int[22×22] arrays. v1.2: AR-2 L-3 DeterministicHash literals cast to unchecked int for true 32-bit wrapping. |
| `src/perception-system/ShoulderCheckScheduler.cs` | Autonomous shoulder check scheduling, window management, blind-side entity L_rec (§3.4); INV-10 pre-allocated arrays |
| `src/perception-system/ViewBuilder.cs` | Pure field-assembly step: sets scalar/count fields on pre-allocated FilteredView + PerceptionDiagnostics without overwriting PerceivedAgent[] references (§3.7); static, no computation |
| `src/perception-system/RecognitionLatencyState.cs` | Readonly struct: D4 snapshot view over the recognition-latency tracker pair arrays (latency/confirmed/expiry); returned by RecognitionLatencyTracker.CaptureState |
| `src/perception-system/ShoulderCheckState.cs` | Readonly struct: D4 snapshot view over the shoulder-check scheduler per-agent arrays (next-check/window-expiry/active/anim) + per-pair blind-side arrays; returned by ShoulderCheckScheduler.CaptureState |
| `src/perception-system/PerceptionTickState.cs` | Readonly struct: D4 snapshot bundle (RecognitionLatencyState + ShoulderCheckState + per-agent ball-perception carry-over); returned by PerceptionSystem.CaptureState for the Match Engine snapshot layer |
| `src/perception-system/PerceptionSystem.cs` | 10Hz orchestrator; 7-step pipeline for all 22 agents; forced-refresh handler; zero heap allocation on hot path (§3.0–§3.8, §4.1, §4.6). v1.2: AR-2 L-1/L-2 — removed prevBallVisible argument; added length guards to HandleForcedRefresh; added agentHasPossession length guard. |

### Decision Tree (#8) — 38 files

| File | Description |
|------|-------------|
| `src/decision-tree/decision-tree.asmdef` | Assembly definition (AI layer; references agent-movement, perception-system, pass-mechanics, shot-mechanics, heading-mechanics, goalkeeper-mechanics, collision-system, event-system, deterministic-sim, tactical-instructions; June 11, 2026 audit added the deterministic-sim ref EventBusRegistrar requires — asmdef refs are not transitive; June 28, 2026 added tactical-instructions for the #21 T2 seam) |
| `src/decision-tree/AssemblyInfo.cs` | [assembly: InternalsVisibleTo("TacticalDirector.DecisionTree.Tests")] |
| `src/decision-tree/DecisionTree.cs` | Public sealed class: 6-step pipeline orchestrator + state machine (§3.6, §3.7, §4.1) |
| `src/decision-tree/DecisionTreeStateMachine.cs` | Pure state evaluator: IDLE/EVALUATING/EXECUTING/INTERRUPTED transitions (§3.7.2) |
| `src/decision-tree/SnapshotValidator.cs` | Step 1: validates FilteredView — phase gate, agent identity, ball state (§3.1.1) |
| `src/decision-tree/DecisionContextAssembler.cs` | Step 2: assembles DecisionContext from all pipeline inputs (§2.2.4, §3.1.1) |
| `src/decision-tree/OptionGenerator.cs` | Step 3: generates all eligible ActionOption candidates (§3.1) |
| `src/decision-tree/UtilityScorer.cs` | Step 4: scores ActionOptions with §3.2 formulas; #21 §3.2 Mentality risk mult + §3.3 per-agent PlayerTactic product applied per option before the clamp (identity ⇒ ×1.0) |
| `src/decision-tree/TacticalModifierResolver.cs` | Step 4 helper: resolves tactical multipliers per action type (§3.4) |
| `src/decision-tree/TacticTranslation.cs` | #21 T2 consumer seam: TacticPressing/TacticPassing → #8 enums (rank-mapped, F5 clamp) + Mentality risk/line resolvers + §3.3 PlayerTacticActionMultiplier (per-agent role/duty/instr × team tempo product; identity ⇒ ×1.0) (#21 §3.1/§3.2/§3.3; pure, translate-once) |
| `src/decision-tree/ActionSelector.cs` | Step 5: composure noise injection + highest-EffectiveUtility winner (§3.3) |
| `src/decision-tree/ActionDispatcher.cs` | Step 6: routes selected action to movement controller or physics executor (§3.5) |
| `src/decision-tree/DecisionContext.cs` | Internal struct: all assembled pipeline inputs for one agent-tick (§2.2.4) |
| `src/decision-tree/ActionOption.cs` | Internal struct: one scored candidate (§3.1.0) |
| `src/decision-tree/AgentAction.cs` | Public readonly struct: pipeline output (type, target, params, utility) (§2.2.3) |
| `src/decision-tree/DecisionTreeState.cs` | Public readonly struct: Match Engine Phase D D0 snapshot DTO — cross-tick state machine (DtState ordinal + last AgentAction + _hasDispatchedAction); _matchSeed/_optionBuffer excluded (§2.6) |
| `src/decision-tree/DecisionMadeEvent.cs` | Tier C struct event (IEventC; ordinal 0x11): published after each decision (§2.2.7) |
| `src/decision-tree/DtAgentAttributes.cs` | Struct: all DT-consumed player attributes [1–20] + CreateDefault factory (§3.1) |
| `src/decision-tree/MatchContext.cs` | Struct: authoritative match state per heartbeat (§2.2.5) |
| `src/decision-tree/TacticalContext.cs` | Struct: pressing mode, passing style, formation slots, #21 Mentality + Tempo + per-agent PlayerTactic routing fields (Stage0Default seeds Balanced/Standard/identity); Stage0Default factory (§2.2.6) |
| `src/decision-tree/DecisionTreeConstants.cs` | Constants: capacity limits / timing budgets / pipeline invariants (§4.2, §3.7) |
| `src/decision-tree/UtilityWeights.cs` | Constants: all 58+ utility scoring constants (§3.2.11) |
| `src/decision-tree/ComposureWeights.cs` | Constants: NOISE_MAX / COMPOSURE_SUPPRESSION / TIEBREAK_EPSILON (§3.3.3–3.3.5) |
| `src/decision-tree/TacticalWeights.cs` | Constants: tactical multipliers for all action types (§3.4) |
| `src/decision-tree/PitchGeometry.cs` | Static helpers: field zone classification, goal post positions, centre (§3.1.1) |
| `src/decision-tree/IDtMovementController.cs` | Public interface: dispatch boundary to Agent Movement #2 (§3.5) |
| `src/decision-tree/EventBusStub.cs` | Wired to EventBus.Publish (internal; single-sig for DecisionMadeEvent) |
| `src/decision-tree/EventBusRegistrar.cs` | Boot-time RegisterExternalRow<T>() for DecisionMadeEvent (0x11) |
| `src/decision-tree/ActionType.cs` | Enum: PASS/SHOOT/DRIBBLE/HOLD/MOVE_TO_POSITION/PRESS/INTERCEPT |
| `src/decision-tree/DtState.cs` | Enum: IDLE/EVALUATING/EXECUTING/INTERRUPTED (§3.7.1) |
| `src/decision-tree/FieldZone.cs` | Enum: DEFENSIVE/MIDFIELD/ATTACKING |
| `src/decision-tree/MatchPhase.cs` | Enum: OPEN_PLAY/SET_PIECE_HOME/SET_PIECE_AWAY/KICK_OFF |
| `src/decision-tree/PassingStyle.cs` | Enum: DIRECT/MIXED/SHORT |
| `src/decision-tree/PressingMode.cs` | Enum: HIGH/MEDIUM/LOW |
| `src/decision-tree/PossessionState.cs` | Enum: HOME_TEAM/AWAY_TEAM/CONTESTED |
| `src/decision-tree/Tests/decision-tree-tests.asmdef` | Test assembly (EditMode; references decision-tree.asmdef) |
| `src/decision-tree/Tests/OptionGeneratorTests.cs` | UT-01..07: OptionGenerator generation gates and candidate logic |
| `src/decision-tree/Tests/UtilityScorerTests.cs` | UT-08..09: UtilityScorer per-action-type utility formulas |
| `src/decision-tree/Tests/ActionSelectorTests.cs` | UT-10..15: ActionSelector composure noise + winner selection |
| `src/decision-tree/Tests/DispatcherTests.cs` | UT-16..23: ActionDispatcher movement routing |
| `src/decision-tree/Tests/DecisionTreeIntegrationTests.cs` | UT-24..35: full pipeline state machine + output (UT-33..35 are June 11 audit H-3 locks) |
| `src/decision-tree/Tests/DecisionContextAssemblerTests.cs` | June 11, 2026 audit locks: H-2 team-relative BallZone + M-1 OpponentHasBall derivation |
| `src/decision-tree/Tests/DecisionTreeStateTests.cs` | Match Engine Phase D D0 locks: DecisionTreeState CanonicalSerializer round-trip + Capture/Restore identity + fresh-IDLE default + reflection field-count guard (asmdef gains DeterministicSim ref) |
| `src/decision-tree/Tests/TacticTranslationTests.cs` | #21 T2 seam locks: enum-translation validity + non-inversion + F5 clamp; Mentality Balanced identity (FR-TI-031) + Stage0Default no-op + monotone risk/line shape (asmdef gains TacticalInstructions ref) |

### Positioning AI (#12) — 24 files

| File | Description |
|------|-------------|
| `src/positioning-ai/positioning-ai.asmdef` | Assembly definition (Mechanics layer; references tactical-instructions for the #21 T2 width seam) |
| `src/positioning-ai/PositioningAIConstants.cs` | Single constant catalogue (FR-PA-011/KD-17): pitch/spacing/hysteresis/GK/phase constants + 3 formation tables + pull-factor 13×4 table + lane edges |
| `src/positioning-ai/Phase.cs` | Enum: InPoss/OutOfPoss/TransToAtk/TransToDef (byte) |
| `src/positioning-ai/LineId.cs` | Enum: Defense/Midfield/Attack (byte) |
| `src/positioning-ai/LaneId.cs` | Enum: LW/LH/C/RH/RW — five 13.6 m bins (byte) |
| `src/positioning-ai/RoleId.cs` | Enum: 13 roles GK..ST — row index in 13×4 pull-factor table (byte) |
| `src/positioning-ai/FormationFamily.cs` | Enum: F442/F433/F4231 (byte) |
| `src/positioning-ai/FormationSlotRecord.cs` | Readonly struct: LongPct/LateralPct/Role/DefaultLine/DefaultLane/IsGoalkeeper |
| `src/positioning-ai/ContextModifierInputs.cs` | Readonly struct: ScoreDiff/TeamMeanFatigue/TacticalIntensity; #21 T2 Width/DefensiveWidth routing fields (3-arg ctor seeds Standard = ×1.00 identity; 5-arg ctor for the Phase-D writer) |
| `src/positioning-ai/AgentPositioningData.cs` | Readonly struct: EntityId/SlotIndex/Position/IsActive/Role/IsGoalkeeper |
| `src/positioning-ai/AgentHysteresisState.cs` | Struct: CurrentLine/CandidateLine/LineDwellCount/CurrentLane/CandidateLane/LaneDwellCount |
| `src/positioning-ai/HysteresisState.cs` | Sealed class: team phase state + AgentHysteresisState[] Agents; SeedFromFormation() |
| `src/positioning-ai/PositioningPerceptionSnapshot.cs` | Sealed class: pre-allocated tick input (TickIndex/BallPosition/BallVxFiltered/Agents[]) |
| `src/positioning-ai/PhaseClassifier.cs` | Pure static: ClassifyAndCommit() PHASE_HYSTERESIS_TICKS dwell; indeterminate → lastCommitted |
| `src/positioning-ai/AnchorCalculator.cs` | Pure static: ComputeAnchor/ComputeBallRelativeOffset/ComputeGkSlot (own-half ball.x clamp) |
| `src/positioning-ai/ContextModifier.cs` | Pure static: ApplyToAll() — lateral + vertical compactness scaling relative to centroid (§3.5); #21 T2 — lateralScale ×= phase-selected width scalar via TacticTranslation (in-poss Width / OOP DefensiveWidth; Standard ⇒ ×1.00 exact) |
| `src/positioning-ai/SpacingResolver.cs` | Pure static: EnforceHardSpacing() cost-based displacement up to SPACING_MAX_PASSES (§3.6) |
| `src/positioning-ai/ShapeAnalyzer.cs` | Pure static: ResolveAllLines() insertion-sort + LINE_DWELL_TICKS; ResolveAllLanes() LANE_DWELL_TICKS; called AFTER spacing+clamp (AR-S1-03) |
| `src/positioning-ai/SlotComposer.cs` | Pure static: Compose() 7-step pipeline (anchor→offset→modifiers→spacing→clamp→lines→lanes) |
| `src/positioning-ai/PositioningAITick.cs` | Sealed class: 10 Hz orchestrator; zero-alloc hot path; F1 stale detection; GetFormationSlot/GetLine/GetLane/GetPhase |
| `src/positioning-ai/TacticTranslation.cs` | #21 T2 consumer seam: TacticWidth/TacticDefWidth → lateral-compactness scalar (direct ordinal lookup over WidthScalar/DefWidthScalar, §3.1 F5 clamp; Standard ⇒ ×1.00); pure, translate-once (FR-TI-025) |
| `src/positioning-ai/Tests/positioning-ai-tests.asmdef` | Test assembly (EditMode; references positioning-ai.asmdef + tactical-instructions) |
| `src/positioning-ai/Tests/PositioningAITests.cs` | T-U-001..021 (unit) + T-D-001..002 (determinism) + T-I-001..004 (integration) + T-P-001 (perf) + T-T-001 (tactical) |
| `src/positioning-ai/Tests/TacticTranslationTests.cs` | #21 T2 seam locks: TacticWidth/TacticDefWidth → compactness scalar validity + Standard identity (FR-TI-031) + ContextModifierInputs Standard-seed neutrality + monotone shape + F5 clamp |
| `src/pressing-ai/pressing-ai.asmdef` | Assembly definition (Mechanics layer; references positioning-ai, pass-mechanics, tactical-instructions) |
| `src/pressing-ai/PressingAIConstants.cs` | Single constant catalogue: trigger distances/durations, cover-shadow geometry, stamina costs, pitch constants (GT/Fixed/Derived/Cross regions) |
| `src/pressing-ai/AssemblyInfo.cs` | `[InternalsVisibleTo("TacticalDirector.PressingAI.Tests")]` — created June 12, 2026 (dotnet CI gate; test suite was uncompilable without it) |
| `src/pressing-ai/TriggerFlags.cs` | [Flags] enum: None / BadTouch / BackwardPass / SidelineTrap / WeakReceiver (byte) |
| `src/pressing-ai/PressRole.cs` | Enum: HoldShape / PrimaryPress / CoverShadow (byte) |
| `src/pressing-ai/CoverShadow.cs` | Struct: DefenderId, ReceiverId, TargetPosition |
| `src/pressing-ai/PressDirective.cs` | Struct: per-tick output (PrimaryPresserId, PrimaryTargetPosition, Shadow0, Shadow1, CoverShadowCount, ActiveTriggers); static Inactive; IsActive property |
| `src/pressing-ai/PressAssignment.cs` | Struct: per-agent output (EntityId, Role, TargetPosition) |
| `src/pressing-ai/PressTrigger.cs` | Struct: 8 dwell/release counters (4 dwell + 4 release; zero allocation, no arrays) |
| `src/pressing-ai/RoleHysteresisState.cs` | Sealed class: LastRole[], PendingRole[], RoleDwell[] arrays keyed by EntityId (AR-2 M-2/M-3); Reset() |
| `src/pressing-ai/PressingTickState.cs` | Readonly struct: D4 snapshot view bundling the cross-tick state (RoleHysteresisState, PressTrigger, disengage/cooldown dwell, press-fatigue array); returned by PressingAITick.CaptureState for the Match Engine snapshot layer |
| `src/pressing-ai/PressingAgentSnapshot.cs` | Struct: per-agent tick input (EntityId, TeamId, Position, BaselineSlot, Fatigue, FirstTouchAttribute, Line, IsGoalkeeper, HasBall, IsActive) |
| `src/pressing-ai/PressingSnapshot.cs` | Sealed class: tick input container (TickIndex, BallPosition, BallVelocity, BallCarrierEntityId, AttackingDirection, PossessionTeamId, PressingTeamId, Agents[22]); #21 T2 `LineOfEngagement` routing field (ctor-seeded `Standard` = identity — zero-value default is VeryLow) |
| `src/pressing-ai/PassEventRing.cs` | Sealed class: ring buffer for BackwardPass trigger (Push, TryGetLatest, Clear) |
| `src/pressing-ai/PositioningAIView.cs` | Readonly struct: facade over PositioningAITick (GetFormationSlot, GetLine, GetPhase, IsSentinelSlot) |
| `src/pressing-ai/TriggerEvaluator.cs` | Pure static: Evaluate() debounce pipeline for 4 triggers + ComputeGeometricPressure helper |
| `src/pressing-ai/PrimaryPressSelector.cs` | Pure static: Select() best presser by cost; ComputeInterceptionPoint(); GetCarrierPosition() helper; #21 T2 — eligibility radius scaled by TacticTranslation.PressTriggerRadiusScalar(snapshot.LineOfEngagement) (Standard ⇒ ×1.0) |
| `src/pressing-ai/TacticTranslation.cs` | #21 T2 consumer seam: LineOfEngagement → #13 press-trigger-radius scalar (direct ordinal lookup over LineOfEngagementScalar, §3.1 F5 clamp; Standard ⇒ ×1.0); pure, translate-once (FR-TI-025) |
| `src/pressing-ai/CoverShadowSelector.cs` | Pure static: Select() up to 2 cover shadows; threat score + greedy defender assignment |
| `src/pressing-ai/RoleHysteresis.cs` | Pure static: Commit() dwell guard; ForceAllHoldShape() |
| `src/pressing-ai/StaminaAccumulator.cs` | Pure static: Apply() per-role fatigue cost; ApplyAll() batch apply |
| `src/pressing-ai/DisengageResolver.cs` | Pure static: Evaluate() disengage conditions (zone exit + timeout); IsInCooldown() |
| `src/pressing-ai/InvariantEnforcer.cs` | Pure static: Enforce() three anti-chaos invariants (MaxPressersBallThird, MinBacklineAgents, MaxPressDisplacementM) |
| `src/pressing-ai/PitchOrientation.cs` | Pure static: AttackRelativeX() — attack-direction-relative pitch X for §3.8 zone / §3.9 own-third checks (AR-2 H-1) |
| `src/pressing-ai/PressingAITick.cs` | Sealed class: 10 Hz orchestrator; 8-step pipeline; pre-allocated buffers; zero-alloc hot path; persistent press-fatigue ledger (AR-2 M-1) |
| `src/pressing-ai/Tests/TacticTranslationTests.cs` | #21 T2 seam locks: LineOfEngagement → press-trigger-radius scalar validity + Standard identity (FR-TI-031) + PressingSnapshot ctor-seed behaviour-neutrality + monotone shape + F5 clamp (tests asmdef gains TacticalInstructions ref) |

### `src/defensive-ai/` — Spec #14 (20 files: 19 .cs + 1 asmdef)

| File | Role |
|------|------|
| `src/defensive-ai/defensive-ai.asmdef` | Assembly definition (Mechanics layer; references positioning-ai, pressing-ai, tactical-instructions) |
| `src/defensive-ai/DefensiveAIConstants.cs` | Single constant catalogue: 22 [GT] + 4 [CROSS] constants (assignment, hysteresis, offside-trap, tackle, anti-chaos, GK-zone bounds) |
| `src/defensive-ai/MarkMode.cs` | Enum: Zonal / ManMark / InterceptRunner / CoverGkZone (byte; FR-DA-011) |
| `src/defensive-ai/TackleMode.cs` | Enum: Hold / Jockey / Commit (byte) |
| `src/defensive-ai/MarkDirective.cs` | Struct: team-level tick output (TeamId, OffensiveLineDepth, OffsideTrapActive, StepUpTargetDepth, EmergencyFlag); Inactive() factory |
| `src/defensive-ai/MarkAssignment.cs` | Struct: per-agent assignment (Mode, TargetEntityId, TargetPosition, ValidThroughTick, OverriddenThisTick, IsManuallyAssigned); MakeZonal() factory |
| `src/defensive-ai/TackleIntentRequest.cs` | Struct: per-agent tackle intent (AgentEntityId, Mode, TargetEntityId, ApproachAngle, CoverageDepth) |
| `src/defensive-ai/MarkHysteresisState.cs` | Struct: per-agent dwell-lock state (DwellCounter, CandidateMode, CandidateTargetEntityId, HoldTicks); Default() factory |
| `src/defensive-ai/DefensiveTickState.cs` | Readonly struct: D4 snapshot view bundling cross-tick state (per-entity MarkHysteresisState[], per-entity last MarkAssignment[], OffsideLineState); returned by DefensiveAITick.CaptureState for the Match Engine snapshot layer |
| `src/defensive-ai/OffsideLineState.cs` | Struct: per-team offside state (CurrentLineDepth, StepUpDwellCounter, CooldownTicksRemaining, CoverGkZoneActiveTicks); Default() factory |
| `src/defensive-ai/DefensiveAgentSnapshot.cs` | Struct: per-agent tick input (EntityId, TeamId, Position, Velocity, IsActive, IsGoalkeeper, HasBall, BaselineSlot, Line, PressRole, PerceivedFirstTouch) |
| `src/defensive-ai/DefensiveSnapshot.cs` | Sealed class: tick input container (TickIndex, DefensiveTeamId, BallPosition, BallVelocity, TeamPhase, DefensiveLineDepth, GkEntityId, GkPosition, Agents[22], HasActivePrimaryPress); #21 T2 OffsideTrapRequested routing field (false identity; arming-gate consumption deferred per KD-9) |
| `src/defensive-ai/HoldShapePoolFilter.cs` | Pure static: BuildPool() filters GK + PrimaryPress/CoverShadow; SnapshotIndexOf(); IndexOf() |
| `src/defensive-ai/LastManDetector.cs` | Pure static: Evaluate() last-man predicate (§3.8) + COVER_GK_ZONE trigger (§3.9); DefendsX0/DistToOwnGoal/DisplacementCost/ComputeAbandonedZoneCenter helpers; LastManResult struct |
| `src/defensive-ai/MarkHysteresis.cs` | Pure static: PreCheck() dwell-lock gate; ApplyGate() transition accumulator; Reset() for emergency overrides |
| `src/defensive-ai/MarkAssigner.cs` | Pure static: Assign() regular assignment loop (§3.3); ThreatScore() (§3.5); SelectBestCandidate(); IsBetter() tie-break comparator |
| `src/defensive-ai/TackleIntentEvaluator.cs` | Pure static: Evaluate() tackle intent (§3.6); ComputeCoverageDepth(); SelectMode() |
| `src/defensive-ai/OffsideTrapController.cs` | Pure static: Update() dwell counter + fire trigger (§3.7); ExecuteStepUp(); ComputeDefenseLineSpread(). #21 FR-TI-022/KD-9 (v1.2): consumes OffsideTrapRequested as an additive request — requested ⇒ reduced OffsideTrapRequestedDwellTicks; false ⇒ baseline (neutral) |
| `src/defensive-ai/InvariantEnforcer.cs` | Pure static: Enforce() 3 anti-chaos invariants (§3.10); 3-pass demotion loop; AreAllSatisfied() post-loop check; F4 hard-fallback detection |
| `src/defensive-ai/DefensiveAITick.cs` | Sealed class: 10 Hz orchestrator; 9-step §3.13 pipeline; pre-allocated buffers; GetMarkDirective/GetAssignment/GetTackleIntentRequests public API |
| `src/defensive-ai/TacticTranslation.cs` | #21 T2 consumer seam: OffsideTrap → #14 trap-request bool passthrough (false identity; KD-9 request-not-guarantee); pure, translate-once (FR-TI-025) |
| `src/defensive-ai/Tests/TacticTranslationTests.cs` | #21 T2 seam locks: OffsideTrap passthrough + DefensiveSnapshot false-seed identity (FR-TI-031); tests asmdef gains tactical-instructions ref |

### `src/attacking-ai/` — Spec #15 (26 files: 24 .cs + 1 asmdef + 1 test)

| File | Description |
|------|-------------|
| `src/attacking-ai/attacking-ai.asmdef` | Assembly definition (Mechanics layer; references positioning-ai, pressing-ai, tactical-instructions) |
| `src/attacking-ai/AttackingAIConstants.cs` | Single constant catalogue: GT/Derived/Cross constants (run-params bounds, support radius, width, weak-side, overload, invariants, hysteresis, test criteria, angle epsilon) |
| `src/attacking-ai/AttackRole.cs` | Enum: HoldWidth / SupportBall / Runner / WeakSide (byte; FR-AT-012) |
| `src/attacking-ai/Flank.cs` | Enum: Left / Right — overload lateral discriminator (§3.8) |
| `src/attacking-ai/RunParameters.cs` | Readonly struct: DepthOffsetM / LateralOffsetM / RunTriggerTick — exactly 3 fields (FR-AT-011) |
| `src/attacking-ai/AttackHysteresisState.cs` | Struct: per-agent dwell state (CurrentRole, DwellCounter, CandidateRole, CandidateDwell) |
| `src/attacking-ai/AttackingTickState.cs` | Readonly struct: D4 snapshot view bundling cross-tick state (per-agent AttackHysteresisState[], TransitionHoldState, frozen in-possession AttackDirective); returned by AttackingAITick.CaptureState for the Match Engine snapshot layer |
| `src/attacking-ai/TransitionHoldState.cs` | Struct: per-team possession-loss countdown + PrevPhase |
| `src/attacking-ai/AttackDirective.cs` | Readonly struct: team-level tick output (TeamId, OverloadActive, OverloadFlank, TransitionHoldTick); static Empty |
| `src/attacking-ai/AttackIntent.cs` | Readonly struct: per-agent tick output (AgentEntityId, Role, RunParameters?, ValidThroughTick) |
| `src/attacking-ai/StyleProfile.cs` | Readonly struct: 5 profile multipliers + static factories Possession/Direct/Counter |
| `src/attacking-ai/AttackIntentSnapshot.cs` | Readonly struct: read-only zero-copy view over tick output (Directive, Intents[], IntentCount, TickIndex) |
| `src/attacking-ai/AttackingAgentSnapshot.cs` | Readonly struct: per-agent tick input (EntityId, TeamId, Position, BaselineSlot, Line, IsGoalkeeper, HasBall, IsActive, Pace, Stamina, Dribbling) |
| `src/attacking-ai/AttackingSnapshot.cs` | Sealed class: pre-allocated tick input container (TickIndex, AttackingTeamId, BallPosition, BallCarrierEntityId, BallCarrierPosition, TeamAttackAngle, Agents[22]); #21 T2 FocusPlay routing field (Mixed zero-value identity; OverloadDetector consumption deferred) |
| `src/attacking-ai/AttackPoolEntry.cs` | Internal struct: per-agent scratch entry during pipeline (EntityId, Position, LateralPct, Line, AssignedRole, HasRunParams, run-param fields, RunTargetPosition, TargetPosition) |
| `src/attacking-ai/AttackingPoolBuilder.cs` | Pure static: Build() filters snapshot→pool, EntityId-ascending insertion sort; −1 on F2 sentinel |
| `src/attacking-ai/AttackHysteresis.cs` | Pure static: IsStable() / Update() (with CandidateDwell reset on current-role re-preference) / Reset() — increment-based dwell |
| `src/attacking-ai/SupportHeuristic.cs` | Pure static: IsWithinSupportRadius() / ComputeEffectiveRadius() — floor = MinEffectiveRadiusM |
| `src/attacking-ai/RoleAssigner.cs` | Pure static: Assign() two-pass (pass 1 counts stable, pass 2 evaluates non-stable); GenerateRunParams() §3.4 with Mathf.RoundToInt |
| `src/attacking-ai/WidthHolder.cs` | Pure static: Enforce() near-touchline width-holding; skips near-side HoldWidth+WeakSide in promotion loop |
| `src/attacking-ai/WeakSideController.cs` | Pure static: EnsureWeakSide() post-check; selects max-|Y-ballY| non-RUNNER agent |
| `src/attacking-ai/OverloadDetector.cs` | Pure static: Evaluate() counts non-WEAK_SIDE agents in Y-corridor; fires at ≥OverloadCount. #21 FR-TI-021 (v1.1): 5-arg Evaluate overload (4-arg delegates null) — a FocusPlay-preferred ball-side flank lowers the trigger count by OverloadFocusCountBias (bias, not gate; null ⇒ unchanged) |
| `src/attacking-ai/TransitionController.cs` | Pure static: Evaluate() SET-then-DECREMENT transition hold; COUNTER (0 ticks) → instant empty |
| `src/attacking-ai/InvariantEnforcer.cs` | Pure static: Apply() 3 anti-chaos invariants (max runners, min support, no own-half runs); ApplyFallback() all-HoldWidth |
| `src/attacking-ai/AttackingAITick.cs` | Sealed class: 10 Hz orchestrator; §3.13 pipeline; pre-allocated zero-alloc buffers; LastDirective/GetIntent/GetSnapshot public API |
| `src/attacking-ai/TacticTranslation.cs` | #21 T2 consumer seam: FocusPlay → preferred Flank? (Mixed/ThroughMiddle → null identity; FR-TI-021); pure, translate-once (FR-TI-025) |
| `src/attacking-ai/Tests/TacticTranslationTests.cs` | #21 T2 seam locks: FocusPlay → preferred-flank mapping + AttackingSnapshot Mixed-zero-value identity (FR-TI-031); tests asmdef gains tactical-instructions ref |

### `src/deterministic-sim/` — Spec #16 (27 files: 25 .cs + 2 asmdef)

> Cross-cutting foundation assembly; all gameplay layers reference it; it references no other gameplay assembly.
> AR-1 (4H+4M) + AR-2 (1L) + AR-3 (1L) adversarial review cycles complete (AR-3 clean). Implementation date: May 29, 2026.
> Golden-vector pass June 12, 2026: full §9.5 #4(a)/(b)/(c) KAT suites landed (3 new test fixtures); HKDF upgraded to full RFC 5869 multi-block Expand; WriteF64TierB added; AssemblyInfo.cs InternalsVisibleTo added (the test assembly's internal HkdfSha256/SipHash24_64 calls had never been compilable without it); DeterministicSimTests.cs stray namespace closure fixed (save/load fixture was stranded in the global namespace).

| File | Purpose |
|------|---------|
| `src/deterministic-sim/deterministic-sim.asmdef` | Assembly definition (no references — cross-cutting foundation) |
| `src/deterministic-sim/DeterministicSimConstants.cs` | All [FIXED]/[DERIVED]/[GT] constants: tick rates, error codes (0x1601–0x160D), domain tags (0x10–0x1D), field widths, RNG params, digest/schema versions, END_OF_SNAPSHOT_PHASE_ORDINAL=6, FrameMs / FrameSeconds (B1 per-tick dt) |
| `src/deterministic-sim/PhaseId.cs` | Enum: Input=0 / Intent=1 / AI=2 / Physics=3 / Resolve=4 / Events=5 / Snapshot=6 (byte; AR-1 H-4: AI_NoOp removed; Events=5 added) |
| `src/deterministic-sim/DeterminismTier.cs` | Enum: TierA=0 / TierB=1 / TierC=2 (byte) |
| `src/deterministic-sim/DivergenceClass.cs` | Enum: None / HardDesync / SoftDrift / Cosmetic (byte) |
| `src/deterministic-sim/SubsystemOrdinals.cs` | Compile-time const ints for deterministic intra-phase ordering: BallPhysics=0..GoalkeeperMechanics=7 (Physics 0–19), PositioningAI=20..AttackingAI=23 (Mechanics 20–39), PerceptionSystem=40, DecisionTree=41 (AI 40–59), EventSystem=60 |
| `src/deterministic-sim/ReplayCursor.cs` | Readonly struct: Tick (ulong), PhaseOrdinal (byte), IsAtEndOfSnapshot property, EndOfSnapshot(tick) factory — step-7 boundary assertion in ReplayEngine |
| `src/deterministic-sim/DespawnEntry.cs` | Readonly struct: EntityId (int), FinalActionOrdinal (ulong), FinalRngCursor (ulong), DespawnTick (ulong) — Tier A tombstone written by Resolve phase |
| `src/deterministic-sim/DespawnLog.cs` | Pre-allocated tombstone list: Append / ContainsEntity / GetEntry / Clear; capacity = MaxDespawnEntries (512) |
| `src/deterministic-sim/EnvironmentFingerprint.cs` | Sealed class: 6 readonly fields (WorkerCount, SchedulerPolicy, ReductionTopology, SimdFeatureLevel, FloatModelHash, UnicodeNormalizationVersion); Lock(); ValidateAgainst() → ERR_DS_REPLAY_ENV_MISMATCH; CreateStage0Dev() factory |
| `src/deterministic-sim/RngStreamState.cs` | Mutable struct: StreamKey/RngCursor/ActionOrdinal (ulong), BudgetRemaining/DeclaredBudget/DrawIndex (int), SiteId (string), StreamVersion (ushort), SubsystemOrdinal (int), EntityId (int); ClearReservation() |
| `src/deterministic-sim/MatchClock.cs` | Sealed class: CurrentTick / CurrentTacticalTick (÷AI_PHASE_STRIDE) / CurrentMatchTimeMs (×FrameMs) / CurrentMatchTimeSeconds (×FrameSeconds; B1 seconds-clock) / IsAiStrideTick; Advance(); RestoreFromSnapshot(tick) for replay step 5 — no System.DateTime (FR-CS-042) |
| `src/deterministic-sim/DeterministicRngService.cs` | Sealed class: HKDF-SHA256 key derivation at construction; SipHash-2-4-64 per-draw hash; RegisterStream / Reserve / DrawReserved / CloseReservation / Skip / RestoreStream; zero-alloc hot path (stackalloc Span<byte>[21]; AR-1 H-3) |
| `src/deterministic-sim/CanonicalSerializer.cs` | Static class: §3.2.4.1 Write/Read for bool, u8/i8, u16/i16, u32/i32, u64/i64, f32 (−0.0→+0.0), f32TierB (NaN→0x7FC00000), f64, f64TierB (NaN→0x7FF8000000000000; corpus F-09), strings, bytes, optional tags; FloatUintUnion explicit-layout struct (AR-1 H-1/H-2: eliminates BitConverter.GetBytes heap alloc) |
| `src/deterministic-sim/SnapshotHeader.cs` | Sealed class: SchemaVersion (u32) / DigestVersion (u16) / Tick (u64) / PrevSnapshotDigest[32] / CurrentSnapshotDigest[32] / Fingerprint / Cursor; Initialize(tick, prevDigest, fingerprint) |
| `src/deterministic-sim/SnapshotPayload.cs` | Sealed class: pre-allocated PayloadBytes[MaxSnapshotBytes] / BytesWritten; Reset() |
| `src/deterministic-sim/SnapshotCodec.cs` | Sealed class: Encode() — SHA-256 over payload bytes, digest chain advance; ValidateHeader() → ERR_DS_SCHEMA_INCOMPATIBLE; ValidatePrevDigest() → ERR_DS_DIGEST_CHAIN_BREAK; CommitLoadedDigest() for replay load |
| `src/deterministic-sim/ReplayEngine.cs` | Sealed class: PrepareReplay() executes §4.2.2 steps 1–7; step 6 (RNG restoration) is Stage 0 stub comment (AR-3 L-1: empty loop replaced); step 8 (ReapplyInputsFromT+1) delegated to TickOrchestrator |
| `src/deterministic-sim/SaveManager.cs` | Sealed class: CommitAtomic() implements §4.6.1.1 five-step atomic save (temp write → fsync → rename-with-overwrite → dir fsync); File.Move(overwrite:true) (AR-1 M-2: IOException fix) |
| `src/deterministic-sim/TickOrchestrator.cs` | Sealed class: RunTick() 7-phase 60 Hz pipeline (Input→Intent→AI/AI_NoOp→Physics→Resolve→Events→Snapshot); AI stride-gated on IsAiStrideTick; System.Action phase callbacks; 9 ProfilerMarkers; zero-alloc hot path |
| `src/deterministic-sim/DivergenceDetector.cs` | Static class: CompareDigests / CompareTierAFloat / CompareTierBFloat (AR-1 M-3: one-canonical-NaN case returns SoftDrift) / CompareTierAInt / CompareTierAUlong / Worst(DivergenceClass, DivergenceClass) |
| `src/deterministic-sim/AssemblyInfo.cs` | Assembly-level attributes: InternalsVisibleTo("TacticalDirector.DeterministicSim.Tests") so the KAT suites can drive internal HkdfSha256/HkdfExtract/HkdfExpand/SipHash24_64 (first-touch/pass-mechanics precedent) |
| `src/deterministic-sim/tests/deterministic-sim-tests.asmdef` | Test assembly definition (EditMode; references deterministic-sim.asmdef) |
| `src/deterministic-sim/tests/DeterministicSimTests.cs` | HKDF RFC 5869 Appendix A.1 KAT; SipHash-2-4-64 ref vectors 0–7; canonical serialization (bool, u32/u64 LE, −0.0, PHYSICS_DT bits); T-DS-ORDER-001 clock sequence; T-DS-RNG-002 branch cursor parity; T-DS-SNAP-003 u64 round-trip; T-DS-FAULT-009..014 (budget mismatch, Tier A NaN, Tier B non-canonical NaN, digest chain break, env mismatch, replay boundary); AI stride; DespawnLog; v1.2 fixed the v1.1 stray namespace closure that stranded the save/load fixture in the global namespace |
| `src/deterministic-sim/tests/HkdfSha256KatTests.cs` | Full HKDF-SHA256 KAT suite (§9.5 #4(a)): RFC 5869 A.1–A.3 PRK + full OKM byte-exact (L=42/82 locks multi-block Expand) + pinned project Test Case 4 (RNG_KDF invocation pattern → (k0,k1)) per hkdf-sha256-kat.md v1.2 |
| `src/deterministic-sim/tests/SipHash24KatTests.cs` | Full SipHash-2-4-64 KAT suite (§9.5 #4(b)): all 64 Aumasson & Bernstein 2012 Appendix A vectors + pinned project RNG_STREAM_HASH 21-byte draw-preimage case per siphash-2-4-kat.md v1.2 |
| `src/deterministic-sim/tests/SerializeCanonicalCorpusTests.cs` | Full canonical-serialization corpus suite (§9.5 #4(c)): all 41 serialize-canonical-corpus.md entries (P/F/S/B/O/E/A/ST/D incl. chained SnapshotDigest D-07), encoded bytes + SHA-256 asserted per entry |

### `src/event-system/` — Spec #17 (21 files: 19 .cs + 2 asmdef)

> Cross-cutting foundation assembly; autoReferenced true so Assembly-CSharp assemblies get it automatically; spec assemblies with their own .asmdef need explicit references.
> AR-1 (3H+3M+2L) + AR-2 (1L) + AR-3 clean adversarial review cycles complete. Implementation date: May 30, 2026.

| File | Purpose |
|------|---------|
| `src/event-system/event-system.asmdef` | Assembly definition (references TacticalDirector.DeterministicSim; autoReferenced true) |
| `src/event-system/EventSystemConstants.cs` | All [GT]/[CROSS] constants: queue/dispatch/handler/slot capacities + error codes (0x1701–0x1706) + DomainTagEventLedger (0x15). v1.1: AR-2 fix — MaxEventSlotBytes 128→160; ErrEvtUnregisteredOrdinal (0x1706) added. |
| `src/event-system/IEventA.cs` | Marker interface: Tier A (authoritative, ring-buffered, digest-included) |
| `src/event-system/IEventB.cs` | Marker interface: Tier B (bounded-authoritative; Stage 5+ tolerance path) |
| `src/event-system/IEventC.cs` | Marker interface: Tier C (cosmetic; immediate CosmeticChannel dispatch; excluded from digest) |
| `src/event-system/EventHandler.cs` | Delegate: `void EventHandler<T>(in T evt) where T : struct` |
| `src/event-system/SubscriptionToken.cs` | Readonly struct: EventTypeOrdinal + SubscriberIndex; zero allocation (FR-EVT-073) |
| `src/event-system/EventRegistry.cs` | Appendix A registry: 11 seeded rows (0x01–0x0B) + placeholder rows 0x0C–0x17 (updated by owning spec's EventBusRegistrar.Initialize()); RegisterRow<T> / RegisterRowRaw / RegisterExternalRow<T>; EventOrdinalCache<T> O(1) static-field lookup. v1.3: AR-3 fix — IsRegistered now requires StructSize > 0 (placeholder RegisterRowRaw rows return false until Initialize() sets struct size). |
| `src/event-system/EventLedger.cs` | Ring buffer + typed BFS dispatch; EventSlotMeta (FM-017-002 sort key); EventTypeDispatchBase / EventTypeDispatcher<T>; DrainTick; InsertionSort; SerializeLedger; Subscribe. v1.3: AR-4 H-1: EventTypeOrdinal removed from CompareKey (not in FM-017-002); AR-4 H-2: SerializeLedger now sorts by FM-017-002 key (was insertion order). |
| `src/event-system/CosmeticChannel.cs` | Tier C immediate dispatch: per-ordinal pub-count table; ≥ maxPerTick drop predicate; stackalloc span dispatch (zero-alloc FR-EVT-048). v1.5: AR-3 fix — structSize <= 0 guard promoted from silent return to throw; added upper-bound guard structSize > MaxEventSlotBytes preventing ArgumentOutOfRangeException crash on oversized Tier C structs. |
| `src/event-system/EventBus.cs` | Public static API: BeginTick / BeginPhase / DrainTick / SerializeLedger / OnTickBoundary; Publish / Subscribe overloads per tier. v1.3: AR-2 fix — unconditional if/throw guard in PublishAuthoritative; Subscribe<IEventA/B> guard. v1.4: AR-4 M-1: upper-bound structSize > MaxEventSlotBytes guard added to PublishAuthoritative Tier A/B path; AR-4 L-1: structSize<=0 fallback promoted to throw. |
| `src/event-system/EventTierCache.cs` | internal static generic: cached tier-marker flags (IsTierA/B/C/IsValid) backing EventBus single-method dispatch (ERR-017-002); type-init reflection only |
| `src/event-system/PossessionChangedEvent.cs` | Tier A 0x04: PreviousHolder / NewHolder / Reason |
| `src/event-system/FoulCommittedEvent.cs` | Tier A 0x05: Offender / Victim / Location (Vector3) / FoulKind |
| `src/event-system/CardIssuedEvent.cs` | Tier A 0x06: Recipient / CardKind / FoulOrdinal (byte; 0xFF = procedural) |
| `src/event-system/GoalAwardedEvent.cs` | Tier A 0x07: Scorer / Assister / ScoringTeam / BallPosition (Vector3) |
| `src/event-system/SubstitutionEvent.cs` | Tier A 0x08: Outgoing / Incoming / Team / SubstitutionReason |
| `src/event-system/TickHeartbeatEvent.cs` | Tier C 0x09: empty payload (CLR min size 1 byte); MaxPerTick=1 |
| `src/event-system/VfxImpactCue.cs` | Tier C 0x0A: ImpactPoint (Vector3) / ImpactKind / Intensity; MaxPerTick=64 |
| `src/event-system/UiNotificationCue.cs` | Tier C 0x0B: NotificationKind / SubjectEntity; MaxPerTick=32 |
| `src/event-system/tests/event-system-tests.asmdef` | Test assembly (EditMode; autoReferenced false; references TacticalDirector.EventSystem + TacticalDirector.DeterministicSim + 6 production spec assemblies for the boot-wiring smoke test: PassMechanics / ShotMechanics / PerceptionSystem / DecisionTree / HeadingMechanics / GoalkeeperMechanics — added 2026-06-08 with EventBusWiringSmokeTests.cs landing). |
| `src/event-system/tests/EventBusWiringSmokeTests.cs` | SMOKE-EVT-WIRING-001 boot-wiring smoke test (added 2026-06-08). Drives boot → publish-one-per-spec → DrainTick → SerializeLedger and asserts SHA-256 digest stability across runs. Catches regressions in ordinal allocation, version byte, producer-phase wiring, subsystem-ordinal embedding, struct-size registration, and canonical byte layout. Covers 6 currently-wired registrars (Pass / Shot / Perception / Decision / Heading / Goalkeeper); Agent Movement (#2) is a `[CROSS-PENDING]` slot. Golden digest pinned via Assert.Inconclusive (surfaces unfilled pin to CI as yellow) until the AM-side EventBusRegistrar lands. AR-1 (1H+1M+4L) + AR-2 (0H+1M+4L) + AR-3 (0H+0M+2L cycle-stop) adversarial review cycles complete. |

### `src/performance-optimization/` — Spec #18 (17 files: 16 .cs + 1 asmdef)

> Infrastructure-only assembly (Spec #18). autoReferenced false; references TacticalDirector.DeterministicSim (for EnvironmentFingerprint #16 §4.8). Game-layer assemblies (Physics / Mechanics / AI) MUST NOT import this assembly at runtime. Scaffold added May 30, 2026; promoted to real implementation June 1, 2026.

| File | Purpose |
|------|---------|
| `src/performance-optimization/performance-optimization.asmdef` | Assembly definition; references TacticalDirector.DeterministicSim; autoReferenced false |
| `src/performance-optimization/HotPathAllocExemptAttribute.cs` | Governance attribute (§3.7.5): `[HotPathAllocExempt(justification)]` with optional `SignOffRef`; `AttributeTargets.Method\|Class\|Struct`; `Inherited=false; AllowMultiple=false` |
| `src/performance-optimization/ChannelVerbosity.cs` | F.0 schema enum: Minimal / Standard / Debug / Exhaustive (FR-PO-055). Extracted from TraceChannel.cs in AR-1 H-1 |
| `src/performance-optimization/ChannelSamplingRule.cs` | F.0 schema enum: EveryTick / PerNTicks / EventDriven (FR-PO-056). Extracted from TraceChannel.cs in AR-1 H-1 |
| `src/performance-optimization/ChannelDeterminismClass.cs` | F.0 schema enum: TierA / TierB / TierC (FR-PO-058a). Extracted from TraceChannel.cs in AR-1 H-1 |
| `src/performance-optimization/TraceChannelDescriptor.cs` | v1.2 — F.0 sealed descriptor (11 fields); constructor enforces both structural F.0 invariants (SamplingRule=PerNTicks ⇒ SampleN>0 per AR-1 L-1; InsideTickPipeline ⇒ SignOffLogRef non-empty per AR-2 L-2) and carries XML <summary> per AR-2 M-2. Extracted from TraceChannel.cs in AR-1 H-1 |
| `src/performance-optimization/TraceChannelRegistry.cs` | Stage 0 anchor rows: PerfBudget / PerfAlloc / PerfTrace (perf.* channels owned by Spec #18). Extracted from TraceChannel.cs in AR-1 H-1 |
| `src/performance-optimization/PerformanceOptimizationConstants.cs` | v1.2 — Fixed: HOT_PATH_ALLOC_BUDGET_BYTES=0 / LOOP_TAG_TACTICAL_10HZ / LOOP_TAG_PHYSICS_60HZ; GT: PerPrRegressionFraction=0.05 / AbsoluteDriftFraction=0.10 / BaselineSampleCount=100 / MaxFlakeRate=0.01 / HeadroomMultiplierMin=1.2 / HeadroomMultiplierMax=1.5 / PromotionToleranceFraction=0.20 / ReproducibilityToleranceFraction=0.20; EST: SamplerDefaultHz=1000 / StatisticalSignificanceN=30 / FirstTickWarmupCount=0 |
| `src/performance-optimization/LoopTag.cs` | enum: TacticalTenHz / PhysicsSixtyHz — loop discriminator per KD-8 / §3.2.2; on-disk string keys are LOOP_TAG_* constants |
| `src/performance-optimization/BaselinePassFail.cs` | enum: Pass / Fail / Advisory — capture-time pass/fail verdict per Appendix A (advisory at capture; authoritative at CI gate time) |
| `src/performance-optimization/HardwareCounterSnapshot.cs` | readonly struct: CpuModel / CoreCount / ThermalState — §3.3.2 session manifest hardware field |
| `src/performance-optimization/SessionManifest.cs` | v1.2 — sealed class: all §3.3.2 required fields (GitSha / Seed / EnvironmentFingerprint #16 §4.8 / PlatformPin / ScenarioManifestId / SessionStartUtc / SessionEndUtc / HardwareCounters / HarnessVersion); `IsComplete()` validator used by §3.4.4 baseline validator (AR-2 L-1: validates HardwareCounters.CpuModel/ThermalState; PR #129 Codex P2: also requires HardwareCounters.CoreCount > 0 — completes the §3.3.2 hardware-snapshot triple) |
| `src/performance-optimization/BaselineRecord.cs` | sealed class: SessionManifest + Loop + P50Ms + P99Ms + PerMethodAllocBytes + PassFail + ThresholdCited — immutable baseline record per Appendix A / §4.3.2 |
| `src/performance-optimization/BudgetRollupEntry.cs` | readonly struct: SpecId / SubroutineName / Loop / BudgetMs / AllocBudgetBytes / Citation — one row in the §3.1.3 Appendix C roll-up table; produced by IBudgetSource |
| `src/performance-optimization/HotPathEntry.cs` | readonly struct: SpecId / MethodName / Loop / BudgetMs / HasAllocExemption — one entry in the §3.7.2 hot-path union set (materialised into tools/hot-path-union.json at Stage 0+1) |
| `src/performance-optimization/IPerfHarness.cs` | interface: BeginSession(manifest) / RecordTickSample(declared, actualMs, allocBytes) / FinalizeSession() → BaselineRecord; both producer (§3.3 harness) and consumer (#19 ScenarioRunner) specified per §4.3.1 / §4.4 |
| `src/performance-optimization/IBudgetSource.cs` | v1.1 — interface: SpecId property + GetEntries() → IReadOnlyList<BudgetRollupEntry> (AR-1 L-3 read-only return type); both producer (per-spec §6 extractor) and consumer (budget-auditor.py) specified per §4.4 |
| `src/performance-optimization/RegressionResult.cs` | readonly struct: PerPrPassed / AbsoluteDriftPassed / DeltaFraction / MilestoneDriftFraction / AllPassed — output of RegressionGate.Evaluate (FR-PO-031) |
| `src/performance-optimization/RegressionGate.cs` | v1.2 — static class: PassesPerPrCheck(baselineMs, currentMs) / PassesAbsoluteDriftCheck(milestoneMs, currentMs) / Evaluate(baseline, current, milestoneMs) → RegressionResult; implements FR-PO-031 §3.5.2 + §3.5.6. AR-1 M-1 Evaluate reuses helpers; AR-1 M-2 degenerate baseline (≤0/NaN) fails closed; AR-2 M-1 PassesAbsoluteDriftCheck NaN milestone returns true (skip-drift signal) aligning helper with Evaluate; non-NaN ≤0 milestone still fails closed |
| `src/performance-optimization/ReproducibilityResult.cs` | readonly struct: IsReproducible / OriginalP50Ms / RecapturedP50Ms / AbsDeltaFraction / ScenarioMatched / SeedMatched — output of BaselineReproducibilityAuditor.Validate (FR-PO-067) |
| `src/performance-optimization/BaselineReproducibilityAuditor.cs` | v1.1 — static class (AR-1 L-2 sealed→static): Validate(original, recaptured) → ReproducibilityResult; implements §3.4.4 / §5.4 / FR-PO-067 reproducibility check; AR-1 M-3 degenerate origP50 (≤0/NaN) fails closed; Stage 0 carve-out per §3.4.4 (MUST activates at Stage 0+1) |
| `src/performance-optimization/CertificationStatus.cs` | v1.0 — enum Pending/Certified (append-only ordinal stability; Pending=0 = safe default) for a perf baseline corpus entry's certification state (FR-PO-052 certified baseline machinery) |
| `src/performance-optimization/CertifiedPerfBaseline.cs` | v1.0 — certification-tagged perf baseline corpus entry: Pending(scenario, loop, platformPin, threshold) carries NO metric (NaN) and refuses TryBuildBaselineRecord (the Linux gate is NON-certifying — no fabricated number); Certified(manifest, loop, p50, p99, threshold) validates a complete manifest + finite positive metrics (p99≥p50, fail-closed) and projects to a corpus BaselineRecord. Platform-pin tokens Stage0CertPlatformPin (certification-platform.md v1.2 tuple) + LinuxNonCertPlatformPin |

### `src/testing-strategy/` — Spec #19 (28 files: 26 .cs + 2 asmdef)

> Infrastructure-only assembly (Spec #19). autoReferenced false; references TacticalDirector.DeterministicSim + TacticalDirector.PerformanceOptimization. Game-layer assemblies MUST NOT import this assembly at runtime. Scaffold added June 2, 2026; Stage 0 ScenarioRunner (closed-loop scenario harness, §3.3.3) added June 10, 2026.

| File | Purpose |
|------|---------|
| `src/testing-strategy/testing-strategy.asmdef` | Assembly definition; references TacticalDirector.DeterministicSim + TacticalDirector.PerformanceOptimization; autoReferenced false |
| `src/testing-strategy/TestingStrategyConstants.cs` | v1.4 — §3.10 governance constant catalogue (pyramid bounds, coverage thresholds, flake windows, pre-commit budget) + SCENARIO_MANIFEST_FORMAT_VERSION (FR-TS-070) + SCENARIO_PATH_CROSS_SPEC_PREFIX (§3.3.5 layout, AR-1 M-4) |
| `src/testing-strategy/TestLayer.cs` | enum: five-layer test taxonomy per §3.1.1 (Unit / Integration / Simulation / Determinism / EndToEndSoak) |
| `src/testing-strategy/TestTier.cs` | enum: Tier A/B/C classification mirror of #16 §1.1.1 (KD-1) |
| `src/testing-strategy/GoldenVectorKind.cs` | enum: discriminates the three golden-vector corpora pinned by #16 §9.5 #4 (a/b/c) |
| `src/testing-strategy/GoldenVectorEntry.cs` | Catalogue entry for one golden-vector corpus (kind + source path + citation) |
| `src/testing-strategy/GoldenVectorResult.cs` | Per-entry result from GoldenVectorRunner; one failed entry blocks FR-DS-009-GATE |
| `src/testing-strategy/GoldenVectorRunner.cs` | Catalogues the #16 §9.5 #4 corpora; per-entry runner surface for the CI determinism gate |
| `src/testing-strategy/DeterminismTierKind.cs` | enum: canonical tier order of #16 §5's regression suite (FR-TS-011 / FR-TS-018) |
| `src/testing-strategy/DeterminismTierResult.cs` | Per-tier outcome from DeterminismGate; failures block merges (KD-2 / FR-TS-012) |
| `src/testing-strategy/DeterminismSuiteResult.cs` | Aggregated DeterminismGate result; carried verbatim by ITestHarness.RunDeterminismTiers (FR-TS-016) |
| `src/testing-strategy/DeterminismGate.cs` | Single integration point invoking #16 §5's regression suite from Spec #19 (FR-TS-016) |
| `src/testing-strategy/PerfGateReport.cs` | PerfGateRunner result: wraps #18 RegressionResult with spec / loop / scenario context |
| `src/testing-strategy/PerfGateRunner.cs` | v1.2 — CI-side wrapper around #18 RegressionGate; rejects mismatched baseline pairs per FR-PO-031 (PR #132 Codex P2) |
| `src/testing-strategy/ScenarioStatus.cs` | enum: Passed / Failed / Quarantined per §3.3.3 (Quarantined is Stage 0+1 flake-layer only) |
| `src/testing-strategy/ScenarioResult.cs` | §3.3.3 result value: status + machine-readable diagnostics + durationMs + #16 §4.8 fingerprint |
| `src/testing-strategy/ScenarioManifest.cs` | In-memory Appendix A.1 manifest entry (name / owning_spec_ids / seed / tier / fixture_refs / format_version) + load-time field validation; Stage 0 in-code authoring (on-disk encoding pinned at Stage 0+1, D1) |
| `src/testing-strategy/ScenarioEnvelope.cs` | Executable expected_outcome_envelope: bodies record bounded predicate outcomes (CheckTrue / CheckEquals / CheckInRange); zero predicates ⇒ Failed (FR-TS-030); NaN values fail in_range, NaN bounds throw (AR-2 L-1) |
| `src/testing-strategy/ScenarioContext.cs` | Per-invocation body input: manifest + verbatim run seed + KD-7-seeded DeterministicRngService + envelope; declares ScenarioBody delegate |
| `src/testing-strategy/IScenario.cs` | §4.4.1 interface: single method ScenarioResult Run(ulong seed); both sides specified in #19 |
| `src/testing-strategy/ClosedLoopScenario.cs` | Standard IScenario for closed-loop scenarios: fresh RNG + context per run (hermetic, FR-TS-023), body drives a real subsystem loop, envelope evaluated (implicit pass forbidden), exceptions → Failed with diagnostic |
| `src/testing-strategy/ScenarioIndex.cs` | v1.1 — immutable in-memory root manifest; duplicate paths AND duplicate names rejected (AR-1 M-4: A.1 name uniqueness); the runner refuses unindexed scenarios (§3.3.6 / FR-TS-028) |
| `src/testing-strategy/ScenarioIndexEntry.cs` | One index row (path + manifest + scenario); extracted from ScenarioIndex.cs (AR-1 L-4); rejects a ClosedLoopScenario registered under a different manifest instance than it executes (AR-1 M-1) |
| `src/testing-strategy/ScenarioRunner.cs` | v1.1 — §3.3.3 single entry point Run(manifestPath, seed): index resolution + load-time validation (FR-TS-070 format version first, then A.1 fields, §3.3.5 path↔name coherence, cross-spec ≥2 owning-spec arity, non-empty fixture_refs refusal per §3.3.4 — AR-1 M-2/M-4/L-6) → delegates to IScenario.Run; Stage 0 index injected in code, Stage 0+1 adds the index.<ext> file loader (D1) |
| `src/testing-strategy/Tests/testing-strategy-tests.asmdef` | Test assembly definition (EditMode; references testing-strategy + deterministic-sim + ball-physics + pass-mechanics + event-system — the cross-spec corpus drives real #1/#5/#17 surfaces) |
| `src/testing-strategy/Tests/CrossSpecScenarios.cs` | Cross-spec closed-loop scenario corpus (KD-8; paths under SCENARIO_PATH_CROSS_SPEC_PREFIX, ≥ 2 owning specs per A.1): lofted-pass-kick-bounce-roll chains PassExecutor (#5) into BallPhysicsCore (#1) through the IPassBallSystem seam with #17 boot wiring + tick lifecycle around the CONTACT publish; owning specs {1, 5} |
| `src/testing-strategy/Tests/CrossSpecScenarioTests.cs` | sim_<scenario> Simulation-layer tests running the cross-spec corpus through ScenarioRunner |
| `src/testing-strategy/Tests/ScenarioRunnerTests.cs` | v1.2 — 19 ScenarioRunner contract tests: index refusal, format-version rejection, kebab-case validation, implicit-pass rejection, failure diagnostics, NaN in_range, exception + stack capture, seed plumbing (KD-7), per-invocation hermeticity, AR-1 locks (manifest coherence, fixture-refs refusal, newline flattening, duplicate-name / path↔name / cross-spec arity) |

### `src/tactical-instructions/` — Tactical Instructions input layer (Spec #21 T0, June 21, 2026)

> T0 scaffolding (the first landable slice of #21 §7.2): the bottom-of-graph data assembly — 16 enums + 3 aggregate structs + identity factories + 1 constant catalogue + 2 test files. Behaviour-neutral (KD-10): no consumer is wired and the identity factories reproduce today's no-instruction baseline. References only `TacticalDirector.ProjectConstants` per FR-TI-002, but that assembly does not exist yet and T0 consumes nothing from it, so the asmdef `references` array is empty until project-constants lands. Seams into #8/#11–#15 + the consumer-side `TacticTranslation` maps are T2–T3 (gated on match-engine Phase C/D + the `[GT]` config-loader).

| File | Purpose |
|---|---|
| `src/tactical-instructions/tactical-instructions.asmdef` | Assembly `TacticalDirector.TacticalInstructions`; empty `references` (project-constants not yet created); autoReferenced true |
| `src/tactical-instructions/Mentality.cs` | enum (byte, 7): VeryDefensive…VeryAttacking; indexes MentalityRiskMult/LineBias (§3.2) |
| `src/tactical-instructions/Tempo.cs` | enum (byte, 5): VerySlow…VeryFast; Standard (2) identity; NEW #8 branch (§3.3) |
| `src/tactical-instructions/TacticWidth.cs` | enum (byte, 5): VeryNarrow…VeryWide; Standard (2) identity; → #12 compactness |
| `src/tactical-instructions/TacticDefWidth.cs` | enum (byte, 3): Narrow/Standard/Wide; → #12 OOP compactness |
| `src/tactical-instructions/LineOfEngagement.cs` | enum (byte, 5): VeryLow…VeryHigh; → #13 trigger distances |
| `src/tactical-instructions/TransitionPlan.cs` | enum (byte, 4): CounterAttack/HoldShape/CounterPress/Regroup; overrides only the transition dimension (§3.2) |
| `src/tactical-instructions/GkDistributionPolicy.cs` | enum (byte, 6): SlowDown…ThrowOut; → #11 DistributeIntent defaults |
| `src/tactical-instructions/FocusPlay.cs` | enum (byte, 4): Mixed/LeftFlank/RightFlank/ThroughMiddle; NEW #8/#15 branch |
| `src/tactical-instructions/TacticPassing.cs` | enum (byte, 3): Short/Mixed/Direct; translated → #8 PassingStyle |
| `src/tactical-instructions/TacticPressing.cs` | enum (byte, 3): Low/Medium/High; translated → #8 PressingMode |
| `src/tactical-instructions/TacticTriggerMask.cs` | [Flags] enum (byte): None/BadTouch/BackwardPass/SidelineTrap/WeakReceiver; translated → #13 TriggerFlags |
| `src/tactical-instructions/TacticFormation.cs` | enum (byte, 3): F442/F433/F4231 (ordinals match #12 FormationFamily); translated → #12 |
| `src/tactical-instructions/Duty.cs` | enum (byte, 3): Defend/Support/Attack; indexes DutyForeOffsetM/DutyAggressionBias |
| `src/tactical-instructions/PlayerRole.cs` | enum (byte, 6): Default/Poacher/DeepLyingPlaymaker/BallWinningMid/InsideForward/TargetMan; behavioural role (KD-3, ≠ RoleId); indexes RoleWeightModifiers |
| `src/tactical-instructions/InstrBias.cs` | enum (byte, 3): Less/Default/More; indexes InstrBiasMult |
| `src/tactical-instructions/SetPieceDutyFlags.cs` | [Flags] enum (byte): None/FreeKickTaker/CornerTaker/PenaltyTaker (Stage 1+) |
| `src/tactical-instructions/TeamTactic.cs` | readonly struct (16 fields, canonical Appendix B order) + `Balanced` identity factory (reproduces Stage0Default; FR-TI-031) |
| `src/tactical-instructions/PlayerInstructions.cs` | readonly struct (per-agent individual instructions) + `Default` identity factory |
| `src/tactical-instructions/PlayerTactic.cs` | readonly struct (Role + Duty + Instructions) + `Default(role)` identity factory |
| `src/tactical-instructions/TacticalInstructionsConstants.cs` | single catalogue (Appendix A): Fixed (cardinalities + MARK_TARGET_NONE) / Derived (identity-row properties — expression-bodied to dodge static-init order) / GT (all [GT] tables, illustrative pending T2 balance pass) |
| `src/tactical-instructions/Tests/tactical-instructions-tests.asmdef` | Test assembly (EditMode; references the production assembly) |
| `src/tactical-instructions/Tests/EnumOrdinalStabilityTests.cs` | Locks all 16 enums' ordinals / bit-positions + byte-backing + 8-flag ceiling (FR-TI-007) |
| `src/tactical-instructions/Tests/FactoryIdentityTests.cs` | Locks the identity factories + catalogue identity rows + RoleWeightModifiers [0.5,2.0] (T-TI-U-029) + table dimensions (FR-TI-031) |

### `tools/` — Stage 0 perf-gate tooling (Spec #18 Appendix E / FR-PO-070)

> Added June 1, 2026. All tools are Stage 0 deliverables per Appendix E. Stage 0+1 upgrades the harness from manual Stopwatch to automated benchmark per §3.3.5.

| File | Purpose |
|------|---------|
| `tools/run-perf-local.sh` | Stage 0 local pre-commit perf-gate runbook (Appendix E / FR-PO-070): runs budget-auditor.py schema + loop-tag passes, then invokes perf-harness/run.sh for anchor baselines; reviewer pastes output into PR description (FR-PO-071) |
| `tools/budget-auditor.py` | §5.3 schema-conformance auditor + §5.5 loop-tag auditor (FR-PO-070): walks every approved spec §6 against Appendix B template; reports missing sections and untagged ms values as ERR-018-NNN candidates; `--mode schema\|loop-tag\|all` |
| `tools/select-seed.py` | KD-6 deterministic seed selector: Stage 0 returns fixed dev seed; Stage 0+1 derives seed from git SHA + scenario ID via SHA-256 (8-byte truncation) |
| `tools/perf-harness/run.sh` | Stage 0 synthetic harness runner: parses scenario manifest, captures Stage 0 Stopwatch stub metrics, writes JSON baseline record under `docs/specs/performance-optimization/baselines/` per Appendix A §A.3 |
| `tools/perf-harness/scenarios/anchor-baseline.manifest.json` | Stage 0 anchor scenario manifest: PERF-ANCHOR-S0-001; validates JSON projection schema end-to-end; no gameplay code exercised |
| `docs/specs/performance-optimization/baselines/.gitkeep` | Stage 0 baseline storage root (Appendix A §A.3): JSON records land at baselines/\<spec-N\>/\<scenario\>-\<seed\>-\<sha8\>.json; migrates to tests/data/baselines/ at first src/ commit (FR-PO-074) |

---

### `tools/dotnet-ci/` — Non-certifying Linux compile/test gate (June 12, 2026)

| File | Purpose |
|---|---|
| `tools/dotnet-ci/README.md` | Gate rationale, first-run findings (8 never-compiled surfaces), shim fidelity rules, NOT-a-certification caveat |
| `tools/dotnet-ci/generate_projects.py` | asmdef → `*.gen.csproj` + `TacticalDirector.gen.sln` generator (generated files gitignored; asmdefs remain single source of truth; production netstandard2.1 / tests net8.0; LangVersion 9.0; AssemblyName = asmdef name; DEVELOPMENT_BUILD defined) |
| `tools/dotnet-ci/run-gate.sh` | Gate runner: generate → restore → build (errors block) → `dotnet test` excluding quarantine (any failure blocks) → report-only quarantined run |
| `tools/dotnet-ci/known-failures.txt` | Machine-readable quarantine ledger (30 entries from the first-ever suite execution; shrinking-only; mirrored in `docs/tracking/dotnet-ci-quarantine.md`) |
| `tools/dotnet-ci/LogAssertVerifyAssemblyInfo.cs` | Linked into every generated test project; applies the assembly-level LogAssert reset/verify action |
| `tools/dotnet-ci/UnityShim/UnityShim.csproj` | Shim assembly project (netstandard2.1) |
| `tools/dotnet-ci/UnityShim/Vector2.cs` | UnityEngine.Vector2 shim — Unity-exact approximate `==`, exact `Equals`, 1e-5 Normalize threshold |
| `tools/dotnet-ci/UnityShim/Vector3.cs` | UnityEngine.Vector3 shim — same semantics notes as Vector2 |
| `tools/dotnet-ci/UnityShim/Mathf.cs` | UnityEngine.Mathf shim — Unity NaN-propagation semantics (NaN-gate pattern depends on them), round-half-to-even RoundToInt |
| `tools/dotnet-ci/UnityShim/Debug.cs` | UnityEngine.Debug + LogType + ShimLog event spine (LogAssert observation seam) |
| `tools/dotnet-ci/UnityShim/Profiling.cs` | No-op UnityEngine.Profiling.Profiler + Unity.Profiling.ProfilerMarker |
| `tools/dotnet-ci/UnityShim.TestTools/UnityShim.TestTools.csproj` | TestTools shim project (separate so production assemblies gain no NUnit reference) |
| `tools/dotnet-ci/UnityShim.TestTools/LogAssert.cs` | UnityEngine.TestTools.LogAssert with UTF parity (unmet expectation / unexpected failing log fails the test) |
| `tools/dotnet-ci/UnityShim.TestTools/LogAssertVerifyAttribute.cs` | Assembly-level NUnit ITestAction applying the log contract per test |

---

### `src/match-engine/` — Match Engine composition root (Phase A + B June 16, 2026; **Phase C complete** C0–C3 June 19 / C4–C6 June 22, 2026; **Phase D steps D0–D1 + D2a + D3** June 22, 2026)

> Infrastructure/composition assembly — NOT a member of any gameplay layer; NOT covered by a formal spec (governance anchor: `docs/tracking/match-engine-design.md`). Drives the deterministic-sim `TickOrchestrator` 7-phase pipeline. References `TacticalDirector.DeterministicSim` + `TacticalDirector.EventSystem` + `BallPhysics` + `AgentMovement` (B2) + `CollisionSystem` + `FirstTouch` (D3) + `PassMechanics` + `ShotMechanics` (C1) + `DecisionTree` (C4, for `MatchContext` / `PitchGeometry`) + `PositioningAI` (D2a, for the per-team formation tick); remaining game-layer references (Pressing #13 / Defensive #14 / Attacking #15 at D2b) land with Phases D–F. autoReferenced true. Game-layer assemblies MUST NOT reference match-engine back. (The D0 DecisionTree snapshot seam lives in `src/decision-tree/`; the match-engine consumes it at the Phase D snapshot extension. First touch grants the host `InternalsVisibleTo` so it can run the internal `PressureEvaluator` / `OrientationDetector` context-assembly seams — D3.)

| File | Purpose |
|------|---------|
| `src/match-engine/match-engine.asmdef` | Assembly definition; references DeterministicSim + EventSystem + BallPhysics + AgentMovement + CollisionSystem + FirstTouch (D3) + PassMechanics + ShotMechanics + PerceptionSystem + DecisionTree + PositioningAI (D2a) |
| `src/match-engine/AssemblyInfo.cs` | InternalsVisibleTo("TacticalDirector.MatchEngine.Tests") |
| `src/match-engine/MatchEngineConstants.cs` | [FIXED]/[DERIVED]/[GT] catalogue: SQUAD_SIZE / TEAM_COUNT / PLAYERS_PER_TEAM, kickoff coordinate constants (Ball Physics #1 §1.2 corner-origin), NO_POSSESSION sentinel, STAGE0_NEUTRAL_* executor-adapter proxies, PERCEPTION_GRID_POINT_INSERT_RADIUS (D1 broad-phase point insert), MaxEntityId + STAGE0_FORMATION + STAGE0_TACTICAL_INTENSITY (D2a Positioning AI inputs), FIRST_TOUCH_ACCEPTANCE_RADIUS_M + FIRST_TOUCH_MIN_BALL_SPEED_M_S (D3 first-touch trigger gates), SNAPSHOT_SCHEMA_VERSION (u32 = 2 as of C5; world-state field-set pin — distinct from the #16 SnapshotHeader schema version) |
| `src/match-engine/MatchEngine.cs` | Sealed composition root: boot (seed → DeterministicRngService, clock/codec/fingerprint, AgentMovementSystem, CollisionSystem + per-agent PassExecutor[22]/ShotExecutor[22] + adapters, Pass/Shot EventBusRegistrar boot, real BallState + AgentState[] kickoff world state + buffers + MatchContext), 7 method-group phase callbacks driving the EventBus lifecycle + digest-load-bearing snapshot serialization. B2: Physics drives BallPhysicsCore + AgentMovementSystem.UpdateAllAgents (skips GKs). B3: full §2.6 AgentState/Ball field set incl. OscillationGuard. C2/C3: Resolve drives CollisionSystem.UpdateCollisions + the 22 pass + 22 shot executor lifecycles via the PassWorldAdapter/ShotWorldAdapter. C4: UpdateMatchContext authors MatchContext (possession state, home-perspective BallZone) at the end of Resolve. C5: SerializeWorldState adds the per-agent C0 executor capture + MatchContext (schema v2). D1: RunAiPhase drives a host-owned perception SpatialHashGrid + PerceptionSystem.OnHeartbeat ×22 → 22 per-agent DecisionTree.ReceiveSnapshot, dispatching MovementCommands into _commands (HostMovementController) / PASS-SHOOT into the executors; Stage-0 static AI input snapshots assembled at boot (InitializeAiSnapshots); DecisionTree EventBusRegistrar booted (DecisionMadeEvent Tier C, excluded from digest). D2a: RunAiPhase runs RunPositioningAI before the DT loop — one PositioningAITick + reused PositioningPerceptionSnapshot per team (seeded at boot from STAGE0_FORMATION), filled from world state and ticked, with GetFormationSlot folded back into each agent's TacticalContext (the DT MOVE_TO_POSITION / HOLD anchor); the away team is mapped through the canonical attack-+X frame and back via the self-inverse 180° MirrorPitchIfAway (ERR-008-002 guard). D3: RunResolvePhase calls RunFirstTouch after the executor Update (C3) and before UpdateMatchContext (C4) — a loose, ground-level, moving ball arriving within FIRST_TOUCH_ACCEPTANCE_RADIUS_M of the nearest APPROACHING agent triggers BuildFirstTouchContext (real PressureEvaluator pass over the opposing team via _opponentScratch + OrientationDetector half-turn flag; ERR-007 neutral touch attributes) → FirstTouchSystem.EvaluateFirstTouch/ApplyTouchResult through the FirstTouchWorldAdapter (IBallPhysicsSystem → _ball; IAgentMovementSystem → Stage-0 dribbling no-op); the outcome maps onto possession (CONTROLLED → toucher, INTERCEPTION → interceptor id (AGENT_ID_NONE at Stage 0 → loose), LOOSE_BALL/DEFLECTION → loose). Snapshot schema unchanged (FirstTouchSystem stateless). #21 T2 runtime activation: per-team `_active`/`_pendingTeamTactics` (default `TeamTactic.Balanced`); public `SetTeamTactic(teamId, in TeamTactic)` stages pending; RunAiPhase commits pending→active at the stride boundary (FR-TI-027); RunMechanicsAI overlays the active tactic's Mentality (→ #8 UtilityScorer risk mult) + translated Pressing/Passing (TacticTranslation) into each TacticalContext. Balanced = MEDIUM/MIXED/×1.0 = Stage0Default (behaviour-neutral; tactic arrays NOT serialized → no schema bump; mid-match change not yet restore-deterministic, ERR-021-002). TestOnly_Mentality/Pressing/Passing seams added. #13 Phase-D writer (v1.18): FillPressingSnapshot routes the pressing team's active TeamTactic.LineOfEngagement → PressingSnapshot.LineOfEngagement (overwriting the ctor Standard seed; PrimaryPressSelector scales its trigger radius by PressingAI.TacticTranslation; Balanced ⇒ Standard ⇒ ×1.0 byte-identical). TestOnly_PressLineOfEngagement seam added. #14/#15 Phase-D writers (v1.19): FillDefensiveSnapshot routes the active TeamTactic.OffsideTrap → DefensiveSnapshot.OffsideTrapRequested via fully-qualified DefensiveAI.TacticTranslation (CS0104 — five TacticTranslation types in scope); FillAttackingSnapshot routes the active TeamTactic.FocusPlay → AttackingSnapshot.FocusPlay (enum passthrough). Balanced ⇒ false / Mixed = routing identities (byte-identical); active consumption deferred (#14 KD-9, #15 §5.6/G2). TestOnly_OffsideTrapRequested / TestOnly_FocusPlay seams added. #12 Phase-D writer (v1.20, last of the three Mechanics writers): RunMechanicsAI builds ContextModifierInputs via the 5-arg ctor, routing the active TeamTactic.Width / DefensiveWidth (ContextModifier translates to the lateral-compactness scalar). Balanced ⇒ Standard / Standard ⇒ ×1.00 byte-identical (5-arg both-Standard ≡ 3-arg identity ctor). The modifier struct is a per-tick input captured per-team in _posModifiers only for the TestOnly_PositioningWidth / TestOnly_PositioningDefWidth seams; no schema bump. #21 §3.3 (v1.21): RunMechanicsAI routes the active team Tempo into TacticalContext (per-option §3.3 UtilityScorer product); per-agent PlayerTactic stays the Stage0Default identity. ERR-021-002 resolved (v1.22): SerializeWorldState writes both the active+pending per-team TeamTactic via WriteTeamTactic (Appendix B order); SNAPSHOT_SCHEMA_VERSION 8 → 9 — a mid-match tactic change is now restore-deterministic. Public observation surface (v1.24): BallView / AgentView(i) / AgentTeamId(i) / AgentIsGoalkeeper(i) / PossessingAgentId — read-only value-type COPIES for the presentation layer (`src/match-viewer/` recorder); no live-buffer reference escapes, no behaviour change. |
| `src/match-engine/TeamTacticConfig.cs` | #21 T2 manager-tactic config source: immutable per-team TeamTactic (index = teamId 0 home / 1 away); `Default` = Balanced for every team (FR-TI-031 behaviour-neutral); ForTeam(teamId) with bounds guard. Authored in code (Default/ctor) or from the on-disk Stage-0 text format via TeamTacticFileLoader.Parse (the parser swap, #19 ScenarioIndex precedent); the Stage-1 [GT] loader (FR-CS-019) may replace the grammar leaving Apply untouched |
| `src/match-engine/TeamTacticConfigApplier.cs` | #21 T2 boot applier: static Apply(engine, config) stages every team's tactic into MatchEngine.SetTeamTactic once per team before kickoff (committed at the first AI-stride boundary, FR-TI-027); null-guards both args; applying TeamTacticConfig.Default is behaviour-neutral. The boot-time seam TeamTacticFileLoader feeds (parses a file → TeamTacticConfig → Apply unchanged) |
| `src/match-engine/TeamTacticFileLoader.cs` | #21 on-disk tactic-file loader: Parse(text) → TeamTacticConfig over a line-oriented case-insensitive `key = value` grammar under [home]/[away] headers + `#` comments; omitted key inherits the Balanced identity (empty/null ⇒ Default ⇒ behaviour-neutral); unknown key/section, unparsable value, duplicate key, out-of-range TimeWasting all throw FormatException (fail loud). Stage-0 human-authoring text format (NOT a determinism-pinned wire format — only the resulting TeamTactic values enter the digest via v9); the parser swap TeamTacticConfig/Applier were authored to receive |
| `src/match-engine/PlayerTacticFileLoader.cs` | #21 §3.3 per-agent on-disk tactic-file loader (sibling of TeamTacticFileLoader): Parse(text) → PlayerTacticConfig over a line-oriented case-insensitive `key = value` grammar under [agent N] headers (N = roster index 0..SQUAD_SIZE−1) + `#` comments; every PlayerTactic/PlayerInstructions field has a key (role/duty/riskyPasses/shootTendency/dribbleTendency/crossTendency/positioningFreedom/closeDown/tightMarking/markTarget/setPieceRoles); omitted key/section inherits the PlayerTactic.Default(PlayerRole.Default) identity (empty/null ⇒ PlayerTacticConfig.Identity ⇒ behaviour-neutral); unknown key/section, out-of-range or non-numeric agent index, unparsable value, duplicate key, duplicate section all throw FormatException (fail loud). Stage-0 human-authoring text format (only the resulting PlayerTactic values enter the v10 digest); the parser swap PlayerTacticConfig/Applier were authored to receive |
| `src/match-engine/tests/match-engine-tests.asmdef` | Test assembly definition (EditMode; references match-engine + deterministic-sim + event-system + ball-physics + agent-movement + pass-mechanics + shot-mechanics + decision-tree + positioning/pressing/defensive/attacking AI + perception-system + testing-strategy + performance-optimization (Phase F) + tactical-instructions (#21 T2)) |
| `src/match-engine/tests/MatchEngineDeterminismTests.cs` | Phase A capstone: two same-seed runs → byte-identical snapshot digest chains; chain non-degenerate + advances; AI phase fires only on AI_PHASE_STRIDE ticks; first processed tick is 1 / first AI tick is stride |
| `src/match-engine/tests/MatchEnginePhysicsTests.cs` | Phase B step B2 + Phase D D1: dropped-ball integration through the real loop; same-seed determinism with live ball + agent + AI dynamics; AiPhase_DrivesChain_GoalkeepersSkipped (D1 — the AI chain runs ×22/stride over a 2 s run without throwing and both goalkeepers stay byte-exact; supersedes the B2 injected-WalkTo test now that the AI owns _commands) |
| `src/match-engine/tests/MatchEngineSnapshotSchemaTests.cs` | Phase B step B3 + Phase D D4 + #21: SNAPSHOT_SCHEMA_VERSION pin (9 — v9 adds per-team TeamTactic); OscillationGuard-state + ball-spin + DT/positioning/pressing/defensive/attacking/perception + TeamTactic_FeedsSnapshotDigest preimage probes; locked-guard same-seed determinism |
| `src/match-engine/tests/TeamTacticFileLoaderTests.cs` | #21 loader tests: round-trips the text grammar onto TeamTactic fields, empty/comment-only/null ⇒ Balanced identity (behaviour-neutral), fail-loud cases (unknown key/section, bad enum, duplicate key, key-before-section, out-of-range TimeWasting), parsed config fed through Apply and routed per team |
| `src/match-engine/tests/PlayerTacticFileLoaderTests.cs` | #21 §3.3 per-agent loader tests: round-trips the [agent N] grammar onto PlayerTactic/PlayerInstructions fields (omitted key/section ⇒ identity), empty/comment-only/null ⇒ PlayerTacticConfig.Identity (digest-chain behaviour-neutral when applied), fail-loud cases (key-before-section, unknown/out-of-range/non-numeric section, unknown key, bad enum, bad markTarget, duplicate key/section, no-`=` line), parsed config fed through Apply and routed per agent |
| `src/match-engine/tests/MatchEngineResolveTests.cs` | Phase C C1/C1a/C2/C3: collision separates an overlapping pair in Resolve; same-seed determinism with a live collision; scripted pass/shot initiates through the executor adapters and advances one tick (below CONTACT) |
| `src/match-engine/tests/MatchEngineMatchContextTests.cs` | Phase C C4/C5: home-perspective ball-zone authoring; loose=CONTESTED + possessing-agent-team derivation; scripted ground pass reaches CONTACT, releases possession, kicks the ball; same-seed determinism with a live CONTACT publish; C5 digest-preimage probes for MatchContext + executor state |
| `src/match-engine/tests/MatchEngineMechanicsTests.cs` | Phase D D2a (Positioning AI #12): formation slots feed the decision context (home defender deep / striker advanced, on-pitch); away-team slots mirror the home team (exact GK pitch-mirror — ERR-008-002 guard); same-seed determinism of the slot output |
| `src/match-engine/tests/MatchEngineFirstTouchTests.cs` | Phase D D3 (first touch): a loose, ground-level, approaching ball is received → CONTROLLED gains possession (home + away, proving first-touch is frame-agnostic); receding / above-control-height / already-possessed balls are not touched; a scripted receive is byte-identical across two same-seed runs |
| `src/match-engine/tests/MatchEngineCapstoneScenarios.cs` | Phase F capstone scenario corpus (#19 ScenarioRunner): `match-engine-kickoff-multi-second` (owning specs {1,2,3,4,5,6,7,8,12,13,14,15,16,17,19}, Tier B) boots a real MatchEngine and ticks it 600× (10 s @ 60 Hz); records gameplay-invariant predicates (tick-count; ai-stride-cadence = NumTicks/AI_PHASE_STRIDE = 100; ball + agents finite and on-pitch every tick; chained snapshot digest advances) + a two-run same-seed determinism digest match. Reads world state via the existing internal TestOnly_* seams + public CurrentTick/AiPhaseRunCount/CurrentSnapshotDigest (no production change) |
| `src/match-engine/tests/MatchEngineCapstoneTests.cs` | Phase F capstone tests: runs the kickoff scenario through ScenarioRunner.Run → Passed; a direct two-run same-seed digest-chain equality test (re-locks EventBus.ResetForNewMatch across two in-process matches); FR-PO-052 per-tick perf-gate activation — a real per-tick measurement flows through PerfGateRunner.Run (#18 RegressionGate) against a generous Stage-0 anchor BaselineRecord (loop PhysicsSixtyHz; NON-certifying Linux gate) |
| `src/match-engine/tests/MatchEngineTacticTests.cs` | #21 T2 runtime-activation: SetTeamTactic routes a live per-team TeamTactic into each agent's TacticalContext at the AI-stride boundary (per-team translation Pressing.High→HIGH / Passing.Direct→DIRECT etc.); FR-TI-027 pending takes effect only at the stride; default/explicit Balanced is behaviour-neutral (digest chain identical to the untouched run); same non-Balanced tactic is deterministic across two runs; invalid teamId throws; #13 Phase-D writer — LineOfEngagement routes per team into the Pressing AI snapshot (VeryHigh/VeryLow) and the Balanced default routes Standard (v1.1); #14/#15 Phase-D writers — OffsideTrap routes per team into the Defensive AI snapshot + FocusPlay (LeftFlank/RightFlank) into the Attacking AI snapshot, with false/Mixed identity defaults (v1.2); #12 Phase-D writer — Width/DefWidth (VeryWide/Wide vs VeryNarrow/Narrow) route per team into the Positioning modifiers and the Balanced default routes Standard/Standard (v1.3) |
| `src/match-engine/tests/MatchEngineAwayTeamScenarios.cs` | Decision Tree #8 audit deferred away-team closed-loop scenario on the #19 ScenarioRunner (`away-team-tactic-mirror`, Tier B, owning specs {2,8,16,19,21}, cross-spec path): boots a real MatchEngine, sets home=defending / away=attacking, ticks 300× (5 s), and locks that every away agent carries the away (attacking) routed tactic, every home agent the home (defending) one, the partitions distinct (composition-level inverse of the ERR-008-002 home/away root cause), away agents in bounds, two-run determinism digest match |
| `src/match-engine/tests/MatchEngineAwayTeamTests.cs` | Runs the away-team tactic-mirror scenario through ScenarioRunner.Run → Passed (DT #8 deferred away-team closed-loop follow-up, enabled by #21 runtime activation) |
| `src/match-engine/tests/CertifiedPerfBaselineTests.cs` | v1.0 — locks the FR-PO-052 certified perf baseline for the kickoff scenario: Stage-0 corpus entry is PENDING (no metric, refuses to build a record — no fabricated certification); certified projection builds a complete BaselineRecord that self-compares through PerfGateRunner (0% → pass); fail-closed invariants (degenerate metrics, incomplete manifest, empty args); platform-pin tokens match the documented tuple |
| `src/match-engine/tests/TeamTacticConfigTests.cs` | #21 T2 TeamTacticConfig + applier tests: Default Balanced-for-every-team, ForTeam per-team mapping + bounds throw, applier null-guards, Apply routes each team's tactic through SetTeamTactic at the stride boundary (Attacking/Defending translated per team), and applying the Default config is behaviour-neutral (digest chain identical to the unconfigured run) |

### `src/project-constants/` — Project Constants & `[GT]` config loader (FR-CS-019, June 30, 2026)

> Infrastructure assembly at the bottom of the reference graph (read-only by all; `references: []`, `autoReferenced`). The documented home for the `[GT]` config-loading mechanism and (when one exists) multi-consumer `[CROSS]` constants. The mechanism landed June 30, 2026; the per-catalogue migration of the 520 existing `[GT]` literals landed the same day (509/520 — see `GameplayConfigHolder.cs` + the 17 migrated `<Spec>Constants.cs` catalogues listed in their own per-spec sections; 11 array-table constants in `tactical-instructions` + 4 untagged catalogues are explicit carve-outs, see `src/CLAUDE.md` "Migration status").

| File | Purpose |
|---|---|
| `src/project-constants/project-constants.asmdef` | Assembly definition `TacticalDirector.ProjectConstants`; `references: []`; `autoReferenced` |
| `src/project-constants/GameplayConfig.cs` | FR-CS-019 immutable boot-time `[GT]` key/value store keyed `[section] key`; `GetFloat/GetInt/GetBool/GetString(section, key, fallback)` — absent ⇒ fallback (behaviour-neutral), present-but-malformed ⇒ `FormatException`; immutable + constructor-injected (not a static singleton); boot-time only |
| `src/project-constants/GameplayConfigFileLoader.cs` | FR-CS-019 `Parse(text) → GameplayConfig` over `[section]` `key = value` + `#` comments; `null`/empty ⇒ `Empty`; key-before-section / empty key / duplicate `section.key` / malformed header / no-`=` line all throw `FormatException`; parser-swap seam (grammar not determinism-pinned) |
| `src/project-constants/GameplayConfigHolder.cs` | Resolves the boot-sequencing design point: single `Bind(config)` call point a composition root uses before any `[GT]` catalogue is referenced; `Config` resolves to `GameplayConfig.Empty` until bound (behaviour-neutral default); first `Config` read locks the binding, so a late `Bind` throws `InvalidOperationException` instead of silently losing the override |
| `src/project-constants/AssemblyInfo.cs` | `[InternalsVisibleTo("TacticalDirector.ProjectConstants.Tests")]` for `GameplayConfigHolder.ResetForTests` |
| `src/project-constants/tests/project-constants-tests.asmdef` | Test assembly definition (EditMode; references project-constants) |
| `src/project-constants/tests/GameplayConfigTests.cs` | Getter / fallback / fail-loud / case-insensitive / ctor-guard locks |
| `src/project-constants/tests/GameplayConfigFileLoaderTests.cs` | Grammar round-trip + comments/blanks + empty→Empty + every fail-loud case |
| `src/project-constants/tests/GameplayConfigHolderTests.cs` | Empty-default / Bind-before-lock takes effect / Bind(null) throws / Bind-after-lock throws / ResetForTests clears the lock |

### `src/living-world/` — Living World System #22 T0 scaffolding + season/world-loop slices 1–2 (June 21 / July 2, 2026; spec APPROVED June 22, 2026)

> Self-contained: no references (the spec's vol-2/vol-3 human-systems + project-constants upstreams do not exist in `src/` yet; engine-free, `noEngineReferences`). **Season/world-loop slice 1 landed July 2, 2026** — the KD-10 "persistent world store + season-calendar loop" prerequisite: `WorldClock` / `WorldLoop` / `MemoryStore` / `ColdStore` (§4.2/§4.3). **Slice 2 landed July 2, 2026 (same day)** — `ArcEngine` (§3.4 spawn/pin/lifecycle/expiry; trigger evaluation + its `world.arcs` RNG draws stay the documented KD-10 seam, FR-LW-020/031) + `ActiveSetMembership` (§3.5 entry/LRU-demotion/promotion, FR-LW-023/025), wired into WorldLoop phases 4/6. Remaining services (arc *trigger evaluators*, InteractionTextGenerator, BackgroundTierSim) land as their KD-10 upstreams (vol-2/vol-3, match-outcome events, world RNG sub-streams) are wired.

| File | Purpose |
|---|---|
| `src/living-world/living-world.asmdef` | Assembly definition `TacticalDirector.LivingWorld`; no references; `noEngineReferences` (off-pitch layer touches no physics) |
| `src/living-world/AssemblyInfo.cs` | `InternalsVisibleTo("TacticalDirector.LivingWorld.Tests")` |
| `src/living-world/RelationshipLayer.cs` | `enum : byte {PlayerEdge,Affinity,Trust}` — ordinals = ActiveLayers bit positions (FR-LW-028) |
| `src/living-world/EventKind.cs` | `enum : byte` — open roster (vol-2 §7), APPEND-only seed members |
| `src/living-world/ArcKind.cs` | `enum : byte` — ordinal order = non-entity arc evaluation order (FR-LW-017) |
| `src/living-world/InteractionIntent.cs` | `enum : byte` — named to avoid existing Intent/AttackIntent/DistributeIntent collisions |
| `src/living-world/MemoryEpisode.cs` | `readonly struct` — episodeId/Kind/Salience/WorldTick/ManagerChoiceId + WithDecayedSalience |
| `src/living-world/SpawnCause.cs` | `readonly struct` provenance (KD-8) with nested Input |
| `src/living-world/Arc.cs` | `struct` arc state machine + PinnedEpisode refs + IsExpired liveness |
| `src/living-world/RelationshipEdge.cs` | `struct` — ActiveLayers mask, read-only PlayerEdge mirror, owned Affinity/Trust, Memory[], NextEpisodeId; IsLayerActive |
| `src/living-world/ColdSummary.cs` | `struct` — departed-contact compression incl. NextEpisodeId high-water mark (FR-LW-009) |
| `src/living-world/LivingWorldConstants.cs` | Appendix A catalogue — [GT] (illustrative, pending §7 G2 balance pass) + CLIQUE_THRESHOLD [CROSS vol-2 §2.1] |
| `src/living-world/LivingWorldMath.cs` | Pure deterministic helpers: §3.1 ApplyEvent/ApplyDecay/Clamp01 + FR-LW-021 CompareEvictability tiebreak |
| `src/living-world/WorldClock.cs` | Season-calendar clock (KD-4/FR-LW-019): one worldTick = one calendar day; Advance/RestoreFromSnapshot; distinct from MatchClock, never advanced by the match loops |
| `src/living-world/WorldLoop.cs` | §4.2 per-tick orchestrator: clock advance + phase-3 salience decay + phase-4 ArcEngine expiry sweep + phase-6 membership cap enforcement (null-injectable seams); phases 1/2/5 documented seams (producers not yet built; no phantom interfaces per FR-LW-031) |
| `src/living-world/MemoryStore.cs` | Live deep-tier store: edges sorted on the canonical (FromId,ToId) key (FR-LW-021); §3.2 evict-before-append (lowest-salience unpinned pre-existing episode; all-pinned ⇒ transient growth, shrink on unpin); FR-LW-018 **reference-counted** pins (AR-1 M-1); RemoveEdge refuses pinned edges (AR-1 M-2); §3.1 owned-layer ApplyEvent (PlayerEdge refused, FR-LW-004); InsertEdge F6 gate |
| `src/living-world/ColdStore.cs` | Cold tier sorted by EntityId + §3.5 Compress/Rehydrate transforms; Residue-A v1 schema recorded (NetRelationship = mean of active owned layers); episodeId resumes from NextEpisodeId (FR-LW-009); TryPeek = non-destructive verify-before-take companion to TryTake (slice-2 AR-1 M-2) |
| `src/living-world/ArcEngine.cs` | §3.4 emergent-arc lifecycle: SpawnArc (steps 1–3, atomic FR-LW-018 pinning with F1 rollback; **AR-1 M-1** spawn-time pin-array snapshot so post-spawn caller mutation cannot desync resolve; **AR-1 L-1** spawnTick+lifetime uint-overflow gate), AdvanceState, ResolveArc/unpin, §6.2 per-tick expiry sweep in deterministic spawn order; trigger evaluation + `world.arcs` RNG sub-stream documented as the KD-10 seam (no draw site ⇒ no stream registered, FR-LW-020/031) |
| `src/living-world/ActiveSetMembership.cs` | §3.5 active-set membership (FR-LW-023/025): entry on first interaction; cold-store promotion honouring the verify-live-edge-first TryTake ordering with the **AR-1 M-2** mask check against a TryPeek BEFORE the destructive take; deterministic LRU demotion at the external cap (max episode worldTick, ties → lowest EntityId, arc-pinned edges skipped); own-club at-club exemption + Depart path (pinned departure defers as external) |
| `src/living-world/Tests/living-world-tests.asmdef` | Test assembly definition (EditMode; references living-world) |
| `src/living-world/Tests/LivingWorldTests.cs` | T0 units: enum ordinals (T-LW-U-001..004); §3.1 worked examples (0.56, ~0.016, no-overshoot, no-op); eviction tiebreak; ActiveLayers masking; episodeId-resume |
| `src/living-world/Tests/SeasonWorldLoopTests.cs` | Slice-1 suite (28 tests): clock calendar semantics + T-LW-DET-006; memory T-LW-U-011..018 (monotonic ids, eviction + tiebreak + pin exemption + transient growth, decay, F1 guard, PlayerEdge/F6 refusal, NaN-gates); T-LW-DET-002 canonical order; LOD T-LW-I-011..014 (top-N retention, F5 retained-fields round-trip / T-LW-FAIL-005, episodeId resume, duplicate demote/promote fail-loud); loop phase order + T-LW-DET-007 additive identity; two-run field-identity determinism; AR-1 regression locks (AR-1: ref-counted pins, pinned-edge removal refusal, mask conflict, F6 insert gate, cold-summary coherence; AR-2: out-of-roster layer/mask-bit refusal, episodeId + salience coherence at both seams) |
| `src/living-world/Tests/ArcMembershipTests.cs` | Slice-2 suite (22 tests incl. AR-1 locks — M-1 post-spawn pin-array mutation cannot desync resolve, M-2 promotion mask conflict fails loud without stranding the summary, L-1 overflow refusal): ArcEngine spawn/pin + provenance, F1 pin rollback, validations, resolve/unpin, shared-pin refcount integration, §6.2 expiry boundary, state advance; membership entry/repeat/class-flip + mask-conflict fail-loud, LRU cap demotion to cold, worldTick-tie → lowest EntityId, arc-pinned skip, own-club exemption, Depart→cold + FR-LW-009 re-entry resume, pinned-Depart deferral as external, non-own-club Depart refusal; WorldLoop phase-4/6 wiring; two-run field-identity determinism |

### `src/match-viewer/` — Minimal match viewer (July 2, 2026; presentation tooling — not a numbered spec)

> First presentation-layer surface: records a `MatchEngine` run through its public observation surface (v1.24) and exports a self-contained HTML canvas replay (ball + 22 agents, play/pause/scrub/speed). Pure observer — recording is digest-identical to an unobserved run (locked by test). The HTML output is NOT a determinism-pinned wire format (rounded display coordinates only; same contract class as the Stage-0 tactic text grammar). The UI layer proper remains Stage 1+ (`src/CLAUDE.md` layer taxonomy).

| File | Purpose |
|---|---|
| `src/match-viewer/match-viewer.asmdef` | Assembly definition `TacticalDirector.MatchViewer`; references MatchEngine + DeterministicSim + BallPhysics + AgentMovement + ProjectConstants |
| `src/match-viewer/MatchViewerConstants.cs` | Catalogue: IFAB pitch-marking geometry `[FIXED]` (const) + canvas/recording presentation `[GT]` (config-resolved via `GameplayConfig`, `"match-viewer"` section; presentation-only — nothing feeds the sim or digest) |
| `src/match-viewer/ReplayFrame.cs` | One sampled frame: tick / ball position / possessing agent / agent positions (value copies, never aliasing live buffers) |
| `src/match-viewer/MatchReplay.cs` | Immutable frame sequence + roster (teamIds, GK flags) / pitch / cadence metadata; ReadOnlyCollection + cloned arrays; fail-loud ctor (frame coherence, strictly-increasing ticks, non-empty, metadata NaN-gates) + roster-index guards |
| `src/match-viewer/MatchReplayRecorder.cs` | Ticks an engine, sampling between ticks; seed + pre-configured-engine overloads; fail-loud guards; kickoff frame + final tick always captured |
| `src/match-viewer/HtmlReplayExporter.cs` | Self-contained HTML canvas replay: pitch markings, home/away/GK/possession/ball-height cues, play/pause/scrub/speed + space toggle; InvariantCulture; fail-loud non-finite gate |
| `src/match-viewer/tests/match-viewer-tests.asmdef` | Test assembly definition (EditMode; references match-viewer + match-engine + deterministic-sim + ball-physics + agent-movement) |
| `src/match-viewer/tests/MatchViewerTests.cs` | Frame cadence; on-pitch finiteness; bitwise two-run determinism; observer-neutrality digest lock; fail-loud guards; exporter structure/no-NaN locks |

## Tracking Documents

| File | Purpose |
|------|---------|
| `docs/tracking/PROGRESS.md` | Stage progress, milestones, and current status notes |
| `docs/tracking/spec-error-log.md` | Cross-spec error tracking (`ERR-*`) |
| `docs/tracking/spec-error-log-err012-addendum.md` | ERR-012 addendum details |
| `docs/tracking/fix-manifest-pass-mechanics.md` | Pass Mechanics audit/fix closure tracking |
| `docs/tracking/certification-platform.md` | Stage 0 host platform version pin (required before first Spec #16 certification run) |
| `docs/tracking/file-manifest.md` | This manifest |
| `docs/tracking/stress-test-strategy.md` | Tier A/B/C spec stress-test probe strategy (v1.0, May 18, 2026) |
| `docs/tracking/stress-reports/INDEX.md` | Index of all stress-test run reports |
| `docs/tracking/stress-reports/2026-05-18-tier-a-run-1.md` | Tier A Run 1 report (May 18, 2026) — 3 FAIL, 2 WARN; all 3 FAILs resolved before Run 2 |
| `docs/tracking/stress-reports/2026-05-18-tier-a-run-2.md` | Tier A Run 2 report (May 18, 2026) — 2 FAIL, 1 WARN (×147); FAIL-4/FAIL-5 fixed in this commit; OBS-1 closed |
| `docs/tracking/stress-reports/2026-05-18-tier-a-run-3.md` | Tier A Run 3 report (May 18, 2026) — 1 FAIL (FAIL-6: `[EST]` body-text in #12 §3 + §6.1) fixed in this pass; zero open FAILs after run |
| `docs/tracking/stress-reports/2026-05-18-tier-a-run-4.md` | Tier A Run 4 report (May 18, 2026) — 1 FAIL (FAIL-7: `ATTACK_DWELL_TICKS [EST]` in #15 §1.4) + FIND-12 (headers) + FIND-13 (checklist evidence); all fixed; A-16 triage inaugurated (10/147) |
| `docs/tracking/stress-reports/2026-05-19-tier-a-run-5.md` | Tier A Run 5 report (May 19, 2026) — A-16 triage full corpus sweep; 167/167 entries, all XC- confirmed, 0 open; 0 new FAILs |
| `tools/spec-stress/reports/a16-triage.json` | A-16 normative-constraint-audit triage state — 167 entries (all XC- confirmed), 0 open; COMPLETE |

---


*(June 12, 2026: `docs/tracking/dotnet-ci-quarantine.md` added — human-readable quarantine ledger for the dotnet CI gate; machine mirror at `tools/dotnet-ci/known-failures.txt`.)*

## Planning Documents

| File |
|------|
| `docs/planning/master-development-plan.md` |
| `docs/planning/master-vol-1-physics-core.md` |
| `docs/planning/master-vol-2-human-systems.md` |
| `docs/planning/master-vol-3-club-operations.md` |
| `docs/planning/master-vol-4-tech-implementation.md` |
| `docs/planning/development-best-practices.md` |

---

## Current Specification Folders

All 20 spec folders now exist in `docs/specs/`. Status reflects authoritative classification in `SPEC_INDEX.md`. Folders marked NOT STARTED contain header-only scaffolding (outline + section-1…9 + appendices skeletons with no body content).

| # | Folder | Status |
|---|--------|--------|
| 1 | `docs/specs/ball-physics/` | APPROVED |
| 2 | `docs/specs/agent-movement/` | APPROVED |
| 3 | `docs/specs/collision-system/` | APPROVED |
| 4 | `docs/specs/first-touch/` | APPROVED |
| 5 | `docs/specs/pass-mechanics/` | APPROVED (re-approved May 6, 2026) |
| 6 | `docs/specs/shot-mechanics/` | APPROVED |
| 7 | `docs/specs/perception-system/` | APPROVED |
| 8 | `docs/specs/decision-tree/` | APPROVED (draft-level) |
| 9 | `docs/specs/fixed64-math/` | APPROVED (May 15, 2026) — §9 v1.0 lead-developer sign-off; §9.2 engine + gameplay owner sign-offs granted; §9.7 reciprocal `XC-016-NNN` resolved via #16 §8.3.2 documented comparator-glossary deferral (CLAUDE.md "Interface Design Principle"). §9.8 implementation-time deliverables (golden-vector corpus, CI bench, harness digest, owning-team ledger) remain post-APPROVED follow-ups. Implementation deferred to Stage 5 per §8.1 v0.2. |
| 10 | `docs/specs/heading-mechanics/` | APPROVED May 16, 2026 (v0.3; section files 1–9 + appendices + outline + outline-PASS-1 + section-files-PASS-1-adversarial-review; ERR-010-001 RESOLVED) |
| 11 | `docs/specs/goalkeeper-mechanics/` | APPROVED (May 18, 2026) — section files v0.2; PASS-1 adversarial review (11 findings: 3 H / 5 M / 3 L) resolved same day; ERR-011-001 CLOSED (`DOMAIN_TAG_GOALKEEPER = 0x1D [CROSS: #16 §3.4]` in #16 §3.4 v1.0.5); GK constants `GK_DEPTH_M` / `GK_ADVANCE_FACTOR` / `GK_LATERAL_FACTOR` promoted `[EST]` → `[GT]`; lead-developer R-01..R-05 signed. 44 FRs; ~79 constants; 4 RNG draw sites. |
| 12 | `docs/specs/positioning-ai/` | APPROVED (May 18, 2026) — section files v0.3 (FAIL-4 fix pass); PASS-1 adversarial review (21 findings: 7 H / 9 M / 5 L) filed and resolved in v0.2 (May 16); ERR-012-001 CLOSED (`DOMAIN_TAG_POSITIONING_AI = 0x17 [CROSS: #16 §3.4]` in #16 §3.4 v1.0.5); Appendix A.1–A.8 derivations confirmed; GK constants promoted `[EST]` → `[GT]`; lead-developer R-01..R-05 signed. 47 active FRs; 18 `[GT]` + 7 other constants; no `[EST]` remain. |
| 13 | `docs/specs/pressing-ai/` | APPROVED (May 17, 2026) — v0.3. All gate items resolved: ERR-013-001 Option B (`TacticalContext.PressDirective?` nullable field added to DT #8 §2.2.6); ERR-013-004 (`"Fatigue System #13"` → `"Pressing AI #13"` in DT #8 §3.1.8.1); ERR-013-005 (`DOMAIN_TAG_PRESSING_AI = 0x19 [CROSS]` in #16 §3.4 v1.0.3); ERR-013-007 / ERR-013-008 (`GetPhase` / `GetLine` Stage 1 accessor declarations in #12 §4.5.1); Appendix A derivations for TRIGGER_DWELL_TICKS / TRIGGER_RELEASE_TICKS / ROLE_DWELL_TICKS / INTERCEPT_LOOKAHEAD_TICKS all `[EST]` → `[GT]`; §4.4.3/§4.4.4/§4.5.2/§4.6 updated (Option B mechanism); §1.6 boundary table `[CROSS-PENDING]` → `[CROSS]`; T-C-/T-X- test-prefix table added to #19 §3.1.4; lead-developer R-01..R-05 signed 2026-05-17. OI-002 (Stage 1 channel-registry rows) open non-blocking per §9.6. |
| 14 | `docs/specs/defensive-ai/` | APPROVED (May 18, 2026) — section files v0.4 (FAIL-4 fix pass); PASS-1 adversarial review (17 findings: 6 H / 7 M / 4 L) filed and all resolved in v0.2 fix pass same day; ERR-014-001 CLOSED (`TacticalContext.MarkDirective?` nullable field added to #8 §2.2.6 v1.1.3); ERR-014-004 CLOSED (`DOMAIN_TAG_DEFENSIVE_AI = 0x1A [CROSS: #16 §3.4]` in #16 §3.4 v1.0.5); lead-developer R-01..R-05 signed. 37 FRs; 26 constants (22 `[GT]` + 4 `[CROSS]`); ≥85 tests; ≤0.12 ms per-tick budget. ERR-014-002/003 Stage 1 non-blocking open. |
| 15 | `docs/specs/attacking-ai/` | APPROVED (May 18, 2026) — section files v0.2 (section-1.md through section-9-approval-checklist.md + appendices.md); PASS-1 adversarial review (7 findings: 1 H / 5 M / 1 L) filed and all resolved in v0.2 fix pass; lead-developer R-01..R-05 signed; ERR-015-001 CLOSED (DOMAIN_TAG_ATTACKING_AI = 0x1B allocated in #16 §3.4 v1.0.4); ERR-015-002 CLOSED (AttackIntent[]? added to #8 §2.2.6 v1.1.2; §3.1.7 RUNNER override note added); ERR-015-005 CLOSED ("Attacking AI (#15)" added to #8 §1.3.2 v1.1.2); ERR-015-003/004 Stage 1 non-blocking open. 36 FRs; 38 constants (33 [GT] + 4 [CROSS] + 1 [DERIVED]); 85 tests. |
| 16 | `docs/specs/deterministic-sim/` | APPROVED (May 14, 2026, later same day) — Tier 2 Final Approval. §9 v1.7. All §9.4.2 gates cleared: §9.5 #4(a)/(b)/(c) spec-level sub-conditions SATISFIED (golden-vector files `hkdf-sha256-kat.md` v1.1, `siphash-2-4-kat.md` v1.1, `serialize-canonical-corpus.md` v1.0); §8.3.1 cross-spec re-audit COMPLETE (§8 v1.2, all four upstream rows promoted to `complete`); §9.3 sign-offs (lead-developer Tier 2, QA-automation, platform-certification) granted, platform-certification with explicit Stage-0 host-platform-pin caveat. ERR-017-001 closed atomically via `DOMAIN_TAG_EVENT_LEDGER = 0x15` allocation in §3.4 v1.0.1 (no `DETERMINISM_DIGEST_VERSION` bump). |
| 17 | `docs/specs/event-system/` | APPROVED (May 13, 2026) — 10 section files + appendices; section-files PASS 1 + PASS 2 adversarial review applied; lead-developer sign-off complete |
| 18 | `docs/specs/performance-optimization/` | APPROVED (May 15, 2026, later same day still) — §9 v1.0 lead-developer sign-off. v1.0 `[TBD-NORMATIVE]` sweep landed across §1 / §2 / §3 / §4 / §5 / §6 / §8 / appendices (60 citation-qualifier instances resolved against #16 APPROVED text + #19 v1.0.1 patch-revised text). §9.4.1 blocker list all RESOLVED. KD-2 sequencing satisfied. FR count 82 (FR-PO-019 split into 019 MAY + 019a MUST; FR-PO-058a emission constraints). KD-3 inverted: #18 owns trace pipeline; #16 retains §3.2.4.1 / §3.1 / §5. |
| 19 | `docs/specs/testing-strategy/` | APPROVED (May 15, 2026, later same day) — §9 v1.0 lead-developer sign-off; v1.0.1 `[TBD-NORMATIVE]` sweep landed across §1 / §3 / §4 / §5 / §6 / §8 / appendices against #16's APPROVED text and #18's IN REVIEW v0.3 surface (51 citation-qualifier instances resolved); KD-2 sequencing satisfied. Clears #18's last KD-2 gate. |
| 20 | `docs/specs/code-standards/` | APPROVED (May 11, 2026) — 10 section files + appendices; adversarial review pass-1 applied; lead-developer R-01..R-05 sign-off complete |

**Notes:**
- Attacking AI (#15) files (May 17–18, 2026): `outline.md` (high-level v1.0), `outline-detailed.md` (v1.1), `adversarial-review-outline-detailed-v1.md`, `section-1.md` through `section-9-approval-checklist.md` + `appendices.md` (all at v0.2). `DOMAIN_TAG_ATTACKING_AI = 0x1B [CROSS: #16 §3.4]` (ERR-015-001 CLOSED May 18, 2026). Lead-developer R-01..R-05 signed May 18, 2026. Status: APPROVED.
- Deterministic Simulation (#16) files: `outline.md`, `section-1.md` through `section-9-approval-checklist.md`, `appendices.md`, `critique-log.md` (consolidated review history; supersedes the former `adversarial-review.md` and `third-pass-fix-log.md`, both removed May 3, 2026).
- Fixed64 Math (#9) files include `adversarial-review.md` and `adversarial-critique-pass-2.md` alongside the standard section files.
- Code Standards (#20) outline-tier files: `outline.md` (high-level v1.0), `outline-mid.md` (mid-level v1.3), `outline-detailed.md` (detailed v1.3). Section files authored from the detailed outline May 7–8, 2026; adversarial review pass-1 applied May 11, 2026; lead-developer R-01..R-05 sign-off completed May 11, 2026. Current set: `section-1.md` v1.0.1, `section-2.md` v1.0.1, `section-3.md` v1.0.1, `section-4.md` v1.0, `section-5.md` v1.0.1, `section-6.md` v1.0.1, `section-7.md` v1.0.1, `section-8.md` v1.0, `section-9-approval-checklist.md` v1.1 (APPROVED), `appendices.md` v1.1. SPEC_INDEX.md line 40 reflects APPROVED status.
- Testing Strategy (#19) files: `outline.md` (high-level v1.0 + first adversarial review), `outline-detailed.md` (v1.1 — second adversarial review applied), section-1 through section-9-approval-checklist, and `appendices.md`. Initial section-file draft authored May 12, 2026 from `outline-detailed.md` v1.1; v0.2 self-critique sweep applied same day (3 H / 6 M / 8 L findings, all resolved); status `IN REVIEW`. v0.2 corrected #16 section-number citations against current `deterministic-sim/` text (§7 → §5 regression suite, §1.3.1 → §1.1.1 tier classification, §5 → §3.2.4.1 canonical schema, deleted §8 "trace channels" — no such section). Per KD-2 sequencing in the spec, advancement to `APPROVED` is gated on (a) Spec #16 reaching Tier 2 `APPROVED`, (b) Spec #18 having at least an outline-level draft, and (c) all `TBD-NORMATIVE` tags resolved.
- Performance Optimization Strategy (#18) files: `outline.md` (high-level v1.0 with embedded adversarial review of May 6, 2026), `outline-detailed.md` (v1.1 — May 13, 2026), `section-1.md` through `section-9-approval-checklist.md` + `appendices.md`, `pass-2-adversarial-review.md`. Section files authored May 13, 2026 (v0.1) from `outline-detailed.md` v1.1; PASS-1 adversarial review (4 H / 6 M / 13 L findings, 23 total) resolved in v0.2 (May 14, 2026). PASS-2 adversarial review (2 H / 5 M / 8 L findings, 15 total) filed May 14, 2026 against v0.2; resolved in v0.3 fix pass same day. ERR-018-002..018 all resolved. PASS-2 H-1 / H-2 / M-1 traced to PR #59 + PR #60 parallel-branch merge collision on the v0.2 fix pass. Status `IN REVIEW`; lead-developer sign-off pending. KD-3 inverted: #18 owns trace pipeline; #16 retains §3.2.4.1/§5/§3.1.2. `TBD-NORMATIVE` tags throughout for #16 (IN PROGRESS) and #19 (IN REVIEW) citations. FR count is 82 (FR-PO-001 … 080 + FR-PO-019a + FR-PO-058a — FR-PO-019a split from FR-PO-019 per ERR-018-017).
- Event System (#17) files: `outline.md` (high-level v1.0), `outline-detailed.md` (v1.1), `section-files-critique-pass-1.md`, `section-files-critique-pass-2.md`, `section-1.md` through `section-8.md`, `section-9-approval-checklist.md`, and `appendices.md`. All section files authored May 13, 2026 from `outline-detailed.md` v1.1. Section-files PASS 1 adversarial critique (3 H / 5 M / 12 L findings) resolved in v0.2; section-files PASS 2 adversarial critique (2 H / 6 M / 7 L findings) resolved in v0.3. All section files at v0.3; lead-developer sign-off granted May 13, 2026. ERR-017-001 FULLY RESOLVED — #16-side May 14, 2026 (`DOMAIN_TAG_EVENT_LEDGER = 0x15` allocated in #16 §3.4 v1.0.1); #17-side May 15, 2026 (§1.0.1 patch revision across §1 / §2 / §3 / §7 / §8 / §9 / appendices — `[CROSS-PENDING]` → `[CROSS]` promotion, literal value `0x15` inlined).

---

## Naming Convention (Current)

- Spec files are stored inside per-spec folders.
- Filenames do **not** carry version suffixes.
- Git history is the version record.

Examples:
- `section-1.md`
- `section-3-1-to-3-2.md`
- `section-9-approval-checklist.md`
- `appendix-a.md`
- `audit-report.md`

---

## Maintenance Rule

Update this manifest when:
- a new spec folder is added,
- a tracking/planning file is added, removed, or renamed,
- project-level documentation paths change.
