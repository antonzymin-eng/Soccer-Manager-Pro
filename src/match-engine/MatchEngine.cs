// File:     src/match-engine/MatchEngine.cs
// Created:  2026-06-16
// Modified: 2026-06-19 (Phase C C1/C1a/C2/C3 — Resolve-phase wiring: collision + executors + adapters)
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §2–§5, Code Standards #20
// Purpose:  Composition root that owns match world state and drives the deterministic-sim
//           TickOrchestrator 7-phase pipeline. The Physics phase (B2) drives Ball Physics (#1) +
//           Agent Movement (#2); the Resolve phase (Phase C) drives Collision (#3) + the per-agent
//           Pass (#5) / Shot (#6) executor lifecycles via host world-state adapters. The AI phase
//           remains an EventBus-lifecycle-only stub (design note §5 Phase D).

using System;

using Unity.Profiling;
using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.CollisionSystem;
using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;
using TacticalDirector.PassMechanics;
using TacticalDirector.ShotMechanics;

// The collision orchestrator type name (CollisionSystem) collides with its own namespace leaf
// (TacticalDirector.CollisionSystem); alias it to a distinct name so the type is unambiguous here.
using CollisionSubsystem = TacticalDirector.CollisionSystem.CollisionSystem;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Stage 0 match-engine composition root (determinism spine + Physics-phase wiring as of B2,
    /// full-field-set snapshot serialization as of B3).
    /// Owns the world state, boots the deterministic infrastructure, and exposes the seven
    /// phase methods as <see cref="System.Action"/> method-group callbacks handed to
    /// <see cref="TickOrchestrator"/> (constructor injection per FR-CS-051–054; method-group
    /// conversion allocates once at construction so the hot path stays zero-allocation).
    ///
    /// The phase callbacks drive the EventBus tick lifecycle (design note §2.4): the
    /// orchestrator does not touch the EventBus, so the engine opens the tick in the Input
    /// phase, enters every phase (the AI phase unconditionally, at the end of Intent, so the
    /// EventBus phase stream is invariant across stride/non-stride ticks), drains at Events,
    /// and serializes the ledger + world state at Snapshot. The Physics phase (step B2) drives
    /// the real Ball Physics (#1) and Agent Movement (#2) seams; the AI and Resolve phases remain
    /// lifecycle-only stubs until Phases C–F.
    /// </summary>
    public sealed class MatchEngine
    {
        // ── Deterministic infrastructure ──────────────────────────────────────────────

        private readonly DeterministicRngService _rng;
        private readonly ulong                   _matchSeed;   // raw seed; UpdateCollisions self-seeds from it (C2)
        private readonly MatchClock              _clock;
        private readonly SnapshotCodec           _codec;
        private readonly EnvironmentFingerprint  _fingerprint;
        private readonly TickOrchestrator        _orchestrator;

        // ── Physics subsystems (design note §3) ───────────────────────────────────────
        // AgentMovementSystem is stateless except its pinned physics Hz, so one shared instance
        // serves all 22 agents. BallPhysicsCore is a static class (no instance needed).

        private readonly AgentMovementSystem _movement;

        // ── Resolve subsystems (design note §3 / Phase C C1) ──────────────────────────
        // Per-agent executors are 22-element INSTANCE arrays — each holds its own in-flight
        // state machine (the C0 CaptureState surface), so a shared evaluator cannot serve them
        // (resolves §6 item 5: per-agent instance, not shared). The three query interfaces each
        // executor injects are stateless over world state, so ONE adapter per family (Pass / Shot)
        // backs all 22 instances (the adapter methods take agentId). DecisionTree stays Phase D.

        private readonly CollisionSubsystem            _collisionSystem;
        private readonly ICollisionEventConsumer       _eventConsumer;   // null-object drain; real consumers Phase E
        private readonly PassExecutor[]                _passExecutors;   // [SQUAD_SIZE]
        private readonly ShotExecutor[]                _shotExecutors;   // [SQUAD_SIZE]
        private readonly bool[]                        _stumbleScratch;  // UpdateCollisions stumbleOut sink (discarded — not a Stage-0 movement input, B4)

        // Authoritative ball possession: agent index [0–21], or NO_POSSESSION (−1) when loose.
        // Read by the executor adapters (IsBallPossessedBy); cleared on ApplyKick. The C4 step folds
        // this into MatchContext.PossessingAgentId; Stage 0 has no production possession producer
        // (kickoff is loose), so a TestOnly_ seam scripts it for the executor-lifecycle tests.
        private int _possessingAgentId;

        // ── World state (design note §2.3) ────────────────────────────────────────────
        // Real BallState + AgentState[] driven by the production physics seams (step B2). Step B3
        // serializes the full §2.6 field set field-by-field through CanonicalSerializer (incl. the
        // embedded OscillationGuard via its B0 get/restore seam) under the pinned
        // SNAPSHOT_SCHEMA_VERSION, so all cross-tick state — not just kinematics — feeds the digest.

        private BallState _ball;

        private readonly AgentState[]         _agents;       // [SQUAD_SIZE]
        private readonly PlayerAttributes[]   _attrs;        // per-agent attribute snapshot (default)
        private readonly PerformanceContext[] _perfs;        // per-agent form/context modifiers (neutral)
        private readonly MovementCommand[]    _commands;     // per-agent held command (AI owns it at Phase D)
        private readonly int[]                _teamIds;
        private readonly bool[]               _isGoalkeeper;

        // Collision-feedback buffers (design note §3 one-tick-lag contract): the real two-input
        // movement seam {isCollisionKnockdown, collisionForce}. Written by the Resolve phase
        // (Phase C); consumed by movement here. Boot-seeded standing-at-rest (false / 0); cross-tick
        // state, serialized into the snapshot at B3.
        private readonly bool[]  _isCollisionKnockdown;      // [SQUAD_SIZE]
        private readonly float[] _collisionForces;           // [SQUAD_SIZE]

        // ── Phase A observation state (no gameplay effect) ────────────────────────────

        private bool  _aiPhaseRanThisTick;
        private ulong _aiPhaseRunCount;

        // ── Profiler markers ──────────────────────────────────────────────────────────

        private static readonly ProfilerMarker s_runTickMarker = new ProfilerMarker("MatchEngine.RunTick");

        // ── Boot (design note §4) ─────────────────────────────────────────────────────

        /// <summary>
        /// Boots the match engine for a single match. Seeds deterministic RNG with
        /// <paramref name="matchSeed"/>, allocates all world-state buffers, initialises the
        /// kickoff world state, and constructs the <see cref="TickOrchestrator"/> with the
        /// seven phase callbacks. All allocations happen here; <see cref="RunTick"/> is
        /// zero-allocation on the hot path.
        /// </summary>
        /// <param name="matchSeed">Deterministic match seed (design note §4 step 1).</param>
        public MatchEngine(ulong matchSeed)
        {
            // §4 step 1 — deterministic RNG. Phase A registers no draw sites (no subsystem
            // draws until Phase C+); the seed plumbing is established here for later phases.
            _rng = new DeterministicRngService(matchSeed);
            // Retained raw: CollisionSystem.UpdateCollisions self-seeds its own DeterministicRNG from
            // matchSeed ^ frameNumber (design note C2 NOTE — Phase C registers no host RNG draw sites).
            _matchSeed = matchSeed;

            // §4 step 5 — clock, codec, environment fingerprint.
            _clock       = new MatchClock(0UL);
            _codec       = new SnapshotCodec();
            _fingerprint = EnvironmentFingerprint.CreateStage0Dev();

            // §4 step 3 — physics subsystems. AgentMovementSystem is pinned to the 60 Hz physics
            // tick (deterministic; never wall-clock-derived).
            _movement = new AgentMovementSystem(DeterministicSimConstants.PHYSICS_TICK_HZ);

            // World-state + per-agent input buffers (pre-allocated once; hot path mutates by ref).
            _agents               = new AgentState[MatchEngineConstants.SQUAD_SIZE];
            _attrs                = new PlayerAttributes[MatchEngineConstants.SQUAD_SIZE];
            _perfs                = new PerformanceContext[MatchEngineConstants.SQUAD_SIZE];
            _commands             = new MovementCommand[MatchEngineConstants.SQUAD_SIZE];
            _teamIds              = new int[MatchEngineConstants.SQUAD_SIZE];
            _isGoalkeeper         = new bool[MatchEngineConstants.SQUAD_SIZE];
            _isCollisionKnockdown = new bool[MatchEngineConstants.SQUAD_SIZE];   // default false (standing at rest)
            _collisionForces      = new float[MatchEngineConstants.SQUAD_SIZE];  // default 0    (standing at rest)

            // §4 step 4 — initialise kickoff world state (deterministic; no RNG).
            InitializeKickoffState();

            // §4 step 3 (cont.) — Resolve subsystems (Phase C C1). Kickoff ball is loose.
            _possessingAgentId = MatchEngineConstants.NO_POSSESSION;
            _collisionSystem   = new CollisionSubsystem(MatchEngineConstants.SQUAD_SIZE);
            _eventConsumer     = new NullCollisionEventConsumer();
            _stumbleScratch    = new bool[MatchEngineConstants.SQUAD_SIZE];

            // One adapter per executor family backs all 22 per-agent instances (C1a). Constructed once
            // here; the executors hold them for the match lifetime (no per-frame allocation).
            var passAdapter = new PassWorldAdapter(this);
            var shotAdapter = new ShotWorldAdapter(this);
            _passExecutors = new PassExecutor[MatchEngineConstants.SQUAD_SIZE];
            _shotExecutors = new ShotExecutor[MatchEngineConstants.SQUAD_SIZE];
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                _passExecutors[i] = new PassExecutor(passAdapter, passAdapter, passAdapter);
                _shotExecutors[i] = new ShotExecutor(shotAdapter, shotAdapter, shotAdapter);
            }

            // §4 step 6 — construct the orchestrator with the seven method-group callbacks.
            // Method-group conversion allocates the delegates once here (no per-frame closures).
            _orchestrator = new TickOrchestrator(
                _clock,
                _codec,
                _fingerprint,
                RunInputPhase,
                RunIntentPhase,
                RunAiPhase,
                RunPhysicsPhase,
                RunResolvePhase,
                RunEventsPhase,
                RunSnapshotPhase);
        }

        /// <summary>
        /// Seeds the world state to a deterministic kickoff layout. Positions are a Phase-A
        /// scaffold (two lines, evenly spaced across the pitch width) — they are replaced by
        /// real formation slots from PositioningAIConstants when the AI phase wires in (Phase D).
        /// </summary>
        private void InitializeKickoffState()
        {
            // Stationary ball at the centre spot (a kick would set it in motion; none at Stage 0).
            _ball = BallState.CreateAtPosition(new Vector3(
                MatchEngineConstants.KickoffBallXM,
                MatchEngineConstants.KickoffBallYM,
                MatchEngineConstants.BALL_REST_HEIGHT_M));

            for (int team = 0; team < MatchEngineConstants.TEAM_COUNT; team++)
            {
                for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
                {
                    int i = team * MatchEngineConstants.PLAYERS_PER_TEAM + k;

                    _teamIds[i]      = team;
                    _isGoalkeeper[i] = k == 0;

                    float lineX = team == 0
                        ? MatchEngineConstants.HomeLineXM
                        : MatchEngineConstants.AwayLineXM;
                    // Even lateral spread across the pitch width: k+1 of PLAYERS_PER_TEAM+1 gaps.
                    float spreadY = MatchEngineConstants.PITCH_WIDTH_M
                                  * (k + 1) / (MatchEngineConstants.PLAYERS_PER_TEAM + 1);
                    float headingDeg = team == 0
                        ? MatchEngineConstants.HOME_FACING_DEG
                        : MatchEngineConstants.AWAY_FACING_DEG;

                    _agents[i] = AgentState.CreateAtPosition(
                        new Vector2(lineX, spreadY), FacingFromHeading(headingDeg));
                    _attrs[i]  = PlayerAttributes.CreateDefault();
                    _perfs[i]  = PerformanceContext.CreateNeutral();

                    // Boot-time command: hold formation position. The AI phase (Phase D) replaces
                    // this on the first stride tick (tick 6); until then every agent holds (§3).
                    _commands[i] = MovementCommand.Stop(_agents[i].Position);
                }
            }
        }

        /// <summary>
        /// Converts a kickoff heading in degrees (project convention: +X = toward the away goal,
        /// so 0° faces the away goal and 180° faces the home goal) into a unit facing direction.
        /// Stage 0 kickoff headings are axis-aligned, so they map to exact unit vectors — this keeps
        /// floating-point fuzz (e.g. <c>Mathf.Sin(180°)</c> ≈ 8.7e-8) out of the deterministic
        /// snapshot. Non-cardinal headings (none at Stage 0) fall back to trig. Boot-only — not on
        /// the hot path.
        /// </summary>
        private static Vector2 FacingFromHeading(float degrees)
        {
            if (degrees == 0f)
            {
                return new Vector2(1f, 0f);
            }
            if (degrees == 180f)
            {
                return new Vector2(-1f, 0f);
            }

            float rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        // ── Public API ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Advances the simulation by one 60 Hz tick through the canonical phase pipeline.
        /// Zero heap allocation on the hot path.
        /// </summary>
        public void RunTick()
        {
            using var _ = s_runTickMarker.Auto();
            _orchestrator.RunTick();
        }

        /// <summary>Current 60 Hz physics tick (0 before the first <see cref="RunTick"/>).</summary>
        public ulong CurrentTick => _clock.CurrentTick;

        /// <summary>
        /// True if the AI phase body executed during the most recent <see cref="RunTick"/>.
        /// The orchestrator runs the AI phase only on stride ticks (tick % AI_PHASE_STRIDE == 0);
        /// this flag is reset at the start of each tick's Input phase, so after <see cref="RunTick"/>
        /// it reports that tick's AI cadence.
        /// </summary>
        public bool DidAiPhaseRunLastTick => _aiPhaseRanThisTick;

        /// <summary>Total number of AI-phase executions since boot (one per stride tick).</summary>
        public ulong AiPhaseRunCount => _aiPhaseRunCount;

        /// <summary>
        /// The match-seeded deterministic RNG owned by the composition root. Phase A registers
        /// no draw sites; later phases inject this into subsystems (collision foul/stumble,
        /// pass/shot error, perception latency, GK, heading — design note §4 step 1). Exposed as
        /// an internal seam for those phases and for seed-plumbing assertions in tests.
        /// </summary>
        internal DeterministicRngService Rng => _rng;

        /// <summary>
        /// Test-only seam: overwrites the ball height before a tick so a determinism test can prove
        /// world state actually contributes to the snapshot digest (a perturbed value MUST change
        /// the digest). The ball stays Stationary, so the physics phase leaves it untouched. Not
        /// called by production code; gameplay mutates the ball via the Physics phase.
        /// </summary>
        internal void TestOnly_SetBallHeight(float z)
        {
            _ball.Position = new Vector3(_ball.Position.x, _ball.Position.y, z);
        }

        /// <summary>
        /// Test-only seam: overwrites the entire ball state (e.g. an Airborne ball for a drop-and-
        /// settle test that exercises the real Ball Physics seam). Not called by production code.
        /// </summary>
        internal void TestOnly_SetBall(in BallState state)
        {
            _ball = state;
        }

        /// <summary>Test-only: a copy of the current ball state (read after <see cref="RunTick"/>
        /// to assert the physics seam mutated it).</summary>
        internal BallState TestOnly_BallSnapshot => _ball;

        /// <summary>
        /// Test-only seam: overwrites an agent's held movement command. The AI phase owns this at
        /// Phase D; B2 tests inject a WalkTo to exercise the movement seam. Not called by production.
        /// </summary>
        internal void TestOnly_SetCommand(int index, in MovementCommand command)
        {
            _commands[index] = command;
        }

        /// <summary>Test-only: a copy of an agent's state (read after <see cref="RunTick"/> to
        /// assert movement, or its absence for skipped goalkeepers).</summary>
        internal AgentState TestOnly_AgentSnapshot(int index) => _agents[index];

        /// <summary>
        /// Test-only seam: overwrites an agent's full state so a B3 test can prove the full §2.6
        /// field set (e.g. velocity, OscillationGuard ring-buffer state) feeds the snapshot digest —
        /// a perturbation of any serialized field MUST change the digest. Not called by production.
        /// </summary>
        internal void TestOnly_SetAgent(int index, in AgentState state)
        {
            _agents[index] = state;
        }

        /// <summary>Test-only: whether the agent at the given roster index is a goalkeeper
        /// (UpdateAllAgents skips goalkeepers at Stage 0).</summary>
        internal bool TestOnly_IsGoalkeeper(int index) => _isGoalkeeper[index];

        /// <summary>
        /// Test-only seam: sets authoritative possession to an agent (or NO_POSSESSION). The production
        /// possession producer lands at C4/Phase D; Phase C scripts it so the executor adapters'
        /// IsBallPossessedBy gate passes for a scripted pass/shot. Not called by production.
        /// </summary>
        internal void TestOnly_SetPossession(int agentId)
        {
            _possessingAgentId = agentId;
        }

        /// <summary>Test-only: the current authoritative possessing agent index (NO_POSSESSION = loose).</summary>
        internal int TestOnly_PossessingAgentId => _possessingAgentId;

        /// <summary>
        /// Test-only seam: scripts a pass on the given agent's executor (the Phase D AI dispatcher is the
        /// production trigger — design note C3). The executor advances on subsequent Resolve phases. Not
        /// called by production.
        /// </summary>
        internal PassResult TestOnly_InitiatePass(int agentId, in PassRequest request)
        {
            return _passExecutors[agentId].Execute(in request);
        }

        /// <summary>Test-only seam: scripts a shot on the given agent's executor (see TestOnly_InitiatePass).</summary>
        internal ShotResult TestOnly_InitiateShot(int agentId, in ShotRequest request)
        {
            return _shotExecutors[agentId].Execute(in request);
        }

        /// <summary>Test-only: whether the agent's pass executor is idle (no pass in flight).</summary>
        internal bool TestOnly_PassExecutorIdle(int agentId) => _passExecutors[agentId].IsIdle;

        /// <summary>Test-only: whether the agent's shot executor is idle (no shot in flight).</summary>
        internal bool TestOnly_ShotExecutorIdle(int agentId) => _shotExecutors[agentId].IsIdle;

        /// <summary>
        /// Returns a fresh 32-byte copy of the current snapshot digest (the chained
        /// CurrentSnapshotDigest after the most recent <see cref="RunTick"/>). Diagnostic /
        /// test accessor — allocates a copy and is not called on the hot path.
        /// </summary>
        public byte[] CurrentSnapshotDigest
        {
            get
            {
                byte[] copy = new byte[DeterministicSimConstants.SHA256_BYTES];
                Array.Copy(
                    _orchestrator.CurrentHeader.CurrentSnapshotDigest, 0,
                    copy, 0,
                    DeterministicSimConstants.SHA256_BYTES);
                return copy;
            }
        }

        // ── Phase callbacks (design note §2.4 / §3) ───────────────────────────────────
        // Each callback drives the EventBus phase lifecycle. The Physics phase (B2) invokes the
        // ball + agent-movement seams; the Input / Intent / AI / Resolve phases remain lifecycle-
        // only stubs (gameplay wires in at Phases C–F).

        /// <summary>Phase 0 — Input. Opens the EventBus tick and enters the Input phase.</summary>
        private void RunInputPhase()
        {
            // Reset per-tick observation state (the AI phase may or may not run this tick).
            _aiPhaseRanThisTick = false;

            // MatchClock.Advance() has already run inside RunTick, so CurrentTick is the tick
            // being processed (design note §2.4).
            EventBus.BeginTick((uint)_clock.CurrentTick);
            EventBus.BeginPhase(PhaseId.Input);
        }

        /// <summary>Phase 1 — Intent. Enters the Intent phase, then unconditionally enters the
        /// AI phase so the EventBus phase stream is invariant on non-stride ticks (§2.4).</summary>
        private void RunIntentPhase()
        {
            EventBus.BeginPhase(PhaseId.Intent);

            // AI phase entry is unconditional: the orchestrator skips _runAI on non-stride ticks,
            // so BeginPhase(AI) is issued here (end of Intent) rather than inside RunAiPhase.
            EventBus.BeginPhase(PhaseId.AI);
        }

        /// <summary>Phase 2 — AI. Stride-gated by the orchestrator (runs only when
        /// tick % AI_PHASE_STRIDE == 0). Does NOT call BeginPhase (handled by RunIntentPhase).</summary>
        private void RunAiPhase()
        {
            // Phase A: no perception / decision / mechanics-AI calls yet (Phase D).
            _aiPhaseRanThisTick = true;
            _aiPhaseRunCount++;
        }

        /// <summary>Phase 3 — Physics. Integrates the ball (#1) and the 22 agents (#2) one 60 Hz
        /// step. Consumes the previous tick's collision-feedback buffers per the §3 one-tick-lag
        /// contract (those buffers are written by the Resolve phase, which is still a stub at B2).</summary>
        private void RunPhysicsPhase()
        {
            EventBus.BeginPhase(PhaseId.Physics);

            // Fixed 60 Hz timestep in SECONDS (design note §3 / step B1); never wall-clock.
            float dt = DeterministicSimConstants.FrameSeconds;

            // Ball: a null logger drops matchTime (the logger is its sole consumer — design note B2),
            // so no allocation and no non-load-bearing time enters the digest. No wind at Stage 0.
            BallPhysicsCore.UpdateBallPhysics(
                ref _ball, dt, SurfaceType.GrassDry, Vector3.zero, logger: null, matchTime: 0f);

            // Agents: the batch seam skips goalkeepers (Stage 0 — GK locomotion is Spec #11).
            // currentTime is the seconds-domain match clock (step B1), as OscillationGuard compares
            // elapsed transition times against WindowSeconds.
            _movement.UpdateAllAgents(
                _agents, _attrs, _perfs, _commands, _isGoalkeeper,
                _isCollisionKnockdown, _collisionForces, dt, _clock.CurrentMatchTimeSeconds);
        }

        /// <summary>Phase 4 — Resolve. Runs collision (×22) then advances the in-flight pass/shot
        /// executor lifecycles (Phase C C2/C3). Intra-Resolve order is fixed and digest-load-bearing:
        /// collision → executor Update → possession (possession update lands in C4). Collision writes
        /// THIS tick's feedback buffers (consumed by movement next tick — the §3 one-tick-lag contract);
        /// the executors advance any pass/shot scripted via the TestOnly_ seam (production trigger is the
        /// Phase D AI dispatcher), kicking the ball at CONTACT through the executor adapters.</summary>
        private void RunResolvePhase()
        {
            EventBus.BeginPhase(PhaseId.Resolve);

            int   frameNumber = (int)_clock.CurrentTick;          // narrows safely at Stage 0 (~414 days @ 60 Hz)
            float matchTime   = _clock.CurrentMatchTimeSeconds;

            // C2 — collision first. Reuses _attrs (PlayerAttributes[]); writes _isCollisionKnockdown /
            // _collisionForces (consumed by movement at tick N+1). stumbleOut is discarded (B4 — not a
            // Stage-0 movement input). Self-seeds its own RNG from _matchSeed ^ frameNumber internally.
            _collisionSystem.UpdateCollisions(
                _agents, _attrs, _teamIds, _isGoalkeeper,
                knockdownOut:      _isCollisionKnockdown,
                knockdownForceOut: _collisionForces,
                stumbleOut:        _stumbleScratch,
                ball:              ref _ball,
                matchSeed:         _matchSeed,
                frameNumber:       frameNumber,
                matchTime:         matchTime,
                eventConsumer:     _eventConsumer);

            // C3 — advance any in-flight executors. Idle executors no-op; only a pass/shot started via
            // the TestOnly_ seam (or, from Phase D, the AI dispatcher) is mid-lifecycle here.
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                _passExecutors[i].Update(matchTime, frameNumber, ref _ball);
                _shotExecutors[i].Update(matchTime, frameNumber, ref _ball);
            }
        }

        /// <summary>Phase 5 — Events. Enters the Events phase and drains the tick's ledger.</summary>
        private void RunEventsPhase()
        {
            EventBus.BeginPhase(PhaseId.Events);
            EventBus.DrainTick();
        }

        /// <summary>Phase 6 — Snapshot. Serializes world state + the event ledger into the
        /// digest-load-bearing payload, then closes the EventBus tick boundary (§2.4 / §2.6).</summary>
        private void RunSnapshotPhase(SnapshotPayload payload)
        {
            EventBus.BeginPhase(PhaseId.Snapshot);

            // The orchestrator has already Reset() the payload, so BytesWritten is 0 here.
            SerializeWorldState(payload);

            // Append the canonical event-ledger bytes after the world state — they are part of
            // the snapshot preimage and therefore digest-load-bearing. Phase A publishes no
            // events, so this writes the empty-ledger header (domain tag + zero count) — a
            // constant byte string. NOTE: the EventBus ledger is process-static; Phase A keeps
            // two same-seed runs deterministic only because nothing is published or subscribed
            // (the ledger is always empty here). When real publishes land (Phase E), cross-run
            // ledger state becomes load-bearing and this assumption must be revisited.
            int free = payload.PayloadBytes.Length - payload.BytesWritten;
            int written = EventBus.SerializeLedger(
                new Span<byte>(payload.PayloadBytes, payload.BytesWritten, free));
            payload.BytesWritten += written;

            EventBus.OnTickBoundary();
        }

        /// <summary>
        /// Writes the full world state into the snapshot payload in a fixed canonical order, sourced
        /// from the real BallState / AgentState structs (design note §2.6, step B3). Order is
        /// digest-load-bearing and versioned by <see cref="MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION"/>
        /// — bump that constant on any field-set or ordering change.
        ///
        /// The field set captures all state that survives across ticks, not just kinematics: the ball
        /// velocity / spin / state-machine state and its LastValid* NaN-recovery checkpoints; per agent
        /// the full <see cref="AgentState"/> field-for-field (incl. the embedded
        /// <see cref="OscillationGuard"/> ring-buffer state via the B0 get/restore seam); and the
        /// per-agent ancillary world state that is not part of AgentState but persists cross-tick —
        /// team id, goalkeeper flag, the two collision-feedback inputs (one-tick-lag contract, §3),
        /// and the held <see cref="MovementCommand"/>. Each is read-before-written on a later tick, so
        /// omitting any would diverge save/restore replay. Zero allocation: the OscillationGuard seam
        /// returns a value type.
        /// </summary>
        private void SerializeWorldState(SnapshotPayload payload)
        {
            byte[] buf = payload.PayloadBytes;
            int o = payload.BytesWritten;

            // EXCLUSION PROOF (design note §2.6 "proof must be recorded per field"): _attrs and
            // _perfs are NOT serialized. At Stage 0 both are boot-deterministic constants —
            // PlayerAttributes.CreateDefault() / PerformanceContext.CreateNeutral(), passed to
            // UpdateAllAgents by `in` (read-only, never mutated mid-sim) — so a save/restore
            // reconstructs them identically at boot and their omission cannot diverge replay. The
            // Phase-A observation counters (_aiPhaseRanThisTick/_aiPhaseRunCount) are likewise
            // excluded — instrumentation derivable from the tick number, not gameplay state.
            // PHASE-D FLAG: when the AI phase begins writing per-agent form/fatigue context into
            // _perfs, _perfs becomes cross-tick state and MUST be serialized here (bump
            // SNAPSHOT_SCHEMA_VERSION at that point).
            CanonicalSerializer.WriteU32(buf, ref o, MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION);
            // Tick is also carried in the header; included here so the payload is self-describing
            // when decoded in isolation (replay/save tooling reads the payload directly).
            CanonicalSerializer.WriteU64(buf, ref o, _clock.CurrentTick);

            WriteBallState(buf, ref o, in _ball);

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                WriteAgentState(buf, ref o, in _agents[i]);

                // Ancillary per-agent world state (not carried inside AgentState) — all cross-tick.
                CanonicalSerializer.WriteI32 (buf, ref o, _teamIds[i]);
                CanonicalSerializer.WriteBool(buf, ref o, _isGoalkeeper[i]);
                CanonicalSerializer.WriteBool(buf, ref o, _isCollisionKnockdown[i]);
                CanonicalSerializer.WriteF32 (buf, ref o, _collisionForces[i]);
                WriteMovementCommand(buf, ref o, in _commands[i]);
            }

            payload.BytesWritten = o;
        }

        /// <summary>Serializes the full <see cref="BallState"/> field set in canonical order.
        /// Enum state is written as i32 (ordinal); ordinal stability is the enum's own contract.</summary>
        private static void WriteBallState(byte[] buf, ref int o, in BallState ball)
        {
            CanonicalSerializer.WriteF32(buf, ref o, ball.Position.x);
            CanonicalSerializer.WriteF32(buf, ref o, ball.Position.y);
            CanonicalSerializer.WriteF32(buf, ref o, ball.Position.z);
            CanonicalSerializer.WriteF32(buf, ref o, ball.Velocity.x);
            CanonicalSerializer.WriteF32(buf, ref o, ball.Velocity.y);
            CanonicalSerializer.WriteF32(buf, ref o, ball.Velocity.z);
            CanonicalSerializer.WriteF32(buf, ref o, ball.AngularVelocity.x);
            CanonicalSerializer.WriteF32(buf, ref o, ball.AngularVelocity.y);
            CanonicalSerializer.WriteF32(buf, ref o, ball.AngularVelocity.z);
            CanonicalSerializer.WriteI32(buf, ref o, (int)ball.State);
            CanonicalSerializer.WriteF32(buf, ref o, ball.LastValidPosition.x);
            CanonicalSerializer.WriteF32(buf, ref o, ball.LastValidPosition.y);
            CanonicalSerializer.WriteF32(buf, ref o, ball.LastValidPosition.z);
            CanonicalSerializer.WriteF32(buf, ref o, ball.LastValidVelocity.x);
            CanonicalSerializer.WriteF32(buf, ref o, ball.LastValidVelocity.y);
            CanonicalSerializer.WriteF32(buf, ref o, ball.LastValidVelocity.z);
        }

        /// <summary>Serializes the full <see cref="AgentState"/> field set in canonical order,
        /// including the embedded <see cref="OscillationGuard"/> ring-buffer state via its B0
        /// <see cref="OscillationGuard.GetState"/> accessor. Enum fields are written as i32.</summary>
        private static void WriteAgentState(byte[] buf, ref int o, in AgentState a)
        {
            // Kinematic
            CanonicalSerializer.WriteF32(buf, ref o, a.Position.x);
            CanonicalSerializer.WriteF32(buf, ref o, a.Position.y);
            CanonicalSerializer.WriteF32(buf, ref o, a.Velocity.x);
            CanonicalSerializer.WriteF32(buf, ref o, a.Velocity.y);
            CanonicalSerializer.WriteF32(buf, ref o, a.FacingDirection.x);
            CanonicalSerializer.WriteF32(buf, ref o, a.FacingDirection.y);

            // State machine
            CanonicalSerializer.WriteI32(buf, ref o, (int)a.CurrentState);
            CanonicalSerializer.WriteI32(buf, ref o, (int)a.PreviousState);
            CanonicalSerializer.WriteF32(buf, ref o, a.TimeInState);
            CanonicalSerializer.WriteI32(buf, ref o, (int)a.GroundedReason);
            CanonicalSerializer.WriteF32(buf, ref o, a.CollisionForce);

            // Turning
            CanonicalSerializer.WriteF32(buf, ref o, a.LeanAngle);
            CanonicalSerializer.WriteF32(buf, ref o, a.CurrentTurnRate);

            // Dual-energy fatigue
            CanonicalSerializer.WriteF32(buf, ref o, a.AerobicPool);
            CanonicalSerializer.WriteF32(buf, ref o, a.SprintReservoir);

            // Safety / recovery checkpoints
            CanonicalSerializer.WriteF32(buf, ref o, a.LastValidPosition.x);
            CanonicalSerializer.WriteF32(buf, ref o, a.LastValidPosition.y);
            CanonicalSerializer.WriteF32(buf, ref o, a.LastValidVelocity.x);
            CanonicalSerializer.WriteF32(buf, ref o, a.LastValidVelocity.y);
            CanonicalSerializer.WriteF32(buf, ref o, a.LastValidFacing.x);
            CanonicalSerializer.WriteF32(buf, ref o, a.LastValidFacing.y);
            CanonicalSerializer.WriteF32(buf, ref o, a.Speed);

            // Oscillation guard — private ring-buffer state via the B0 get/restore seam.
            OscillationGuardState g = a.OscillationGuard.GetState();
            CanonicalSerializer.WriteF32 (buf, ref o, g.T0);
            CanonicalSerializer.WriteF32 (buf, ref o, g.T1);
            CanonicalSerializer.WriteF32 (buf, ref o, g.T2);
            CanonicalSerializer.WriteF32 (buf, ref o, g.T3);
            CanonicalSerializer.WriteF32 (buf, ref o, g.T4);
            CanonicalSerializer.WriteF32 (buf, ref o, g.T5);
            CanonicalSerializer.WriteF32 (buf, ref o, g.T6);
            CanonicalSerializer.WriteF32 (buf, ref o, g.T7);
            CanonicalSerializer.WriteI32 (buf, ref o, g.WriteIndex);
            CanonicalSerializer.WriteBool(buf, ref o, g.IsLocked);
            CanonicalSerializer.WriteF32 (buf, ref o, g.LockUntilTime);
        }

        /// <summary>Serializes the held <see cref="MovementCommand"/> field set in canonical order.
        /// Produced only on stride ticks but consumed every tick (§2.6), so it is cross-tick state.</summary>
        private static void WriteMovementCommand(byte[] buf, ref int o, in MovementCommand c)
        {
            CanonicalSerializer.WriteF32 (buf, ref o, c.TargetPosition.x);
            CanonicalSerializer.WriteF32 (buf, ref o, c.TargetPosition.y);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)c.DesiredState);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)c.DecelerationMode);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)c.FacingMode);
            CanonicalSerializer.WriteF32 (buf, ref o, c.FacingTarget.x);
            CanonicalSerializer.WriteF32 (buf, ref o, c.FacingTarget.y);
            CanonicalSerializer.WriteBool(buf, ref o, c.OverrideSafetyConstraints);
        }

        // ── Executor world-state mappers (Phase C C1a) ────────────────────────────────
        // Translate the host's AgentState / PlayerAttributes into the per-spec query DTOs the
        // executors consume. Attribute fields Agent Movement does not yet carry (passing / finishing /
        // technique / weak-foot — ERR-007) are Stage-0 neutral placeholders; fatigue is derived from
        // the agent's AerobicPool (0 = spent → 1 fatigued) so it is real, not a placeholder.

        private PassAgentAttributes BuildPassAttributes(int i)
        {
            return new PassAgentAttributes
            {
                Passing        = MatchEngineConstants.STAGE0_NEUTRAL_ATTRIBUTE,
                Technique      = MatchEngineConstants.STAGE0_NEUTRAL_ATTRIBUTE,
                KickPower      = MatchEngineConstants.STAGE0_NEUTRAL_ATTRIBUTE,
                WeakFootRating = MatchEngineConstants.STAGE0_NEUTRAL_WEAK_FOOT,
                Crossing       = MatchEngineConstants.STAGE0_NEUTRAL_ATTRIBUTE,
                Fatigue        = 1f - _agents[i].AerobicPool
            };
        }

        private PassAgentState BuildPassState(int i)
        {
            return new PassAgentState
            {
                Position        = _agents[i].Position,
                Velocity        = _agents[i].Velocity,
                FacingDirection = _agents[i].FacingDirection
            };
        }

        private ShotAgentAttributes BuildShotAttributes(int i)
        {
            int neutral = Mathf.RoundToInt(MatchEngineConstants.STAGE0_NEUTRAL_ATTRIBUTE);
            return new ShotAgentAttributes
            {
                Finishing      = neutral,
                LongShots      = neutral,
                Composure      = neutral,
                KickPower      = neutral,
                Technique      = neutral,
                WeakFootRating = MatchEngineConstants.STAGE0_NEUTRAL_WEAK_FOOT,
                Fatigue        = 1f - _agents[i].AerobicPool
            };
        }

        private ShotAgentState BuildShotState(int i)
        {
            return new ShotAgentState
            {
                Position        = new Vector3(_agents[i].Position.x, _agents[i].Position.y, 0f),
                Velocity        = new Vector3(_agents[i].Velocity.x, _agents[i].Velocity.y, 0f),
                FacingDirection = _agents[i].FacingDirection,
                CurrentState    = _agents[i].CurrentState
            };
        }

        /// <summary>
        /// Releases possession from <paramref name="agentId"/> when it kicks the ball (Option B: the ball
        /// leaves Controlled at ApplyKick). Authoritative possession transitions are finalized at C4; this
        /// keeps the executor adapters' IsBallPossessedBy honest so a re-entrant CONTACT cannot re-kick.
        /// </summary>
        private void ReleasePossessionOnKick(int agentId)
        {
            if (_possessingAgentId == agentId)
            {
                _possessingAgentId = MatchEngineConstants.NO_POSSESSION;
            }
        }

        // ── Executor adapters (Phase C C1a) ───────────────────────────────────────────
        // Two adapter classes implement all six executor query interfaces (IPass/IShot × Ball/Agent/
        // Collision) over the host world state. Private nested sealed classes so they can read the
        // enclosing engine's private state through the injected back-reference. Collision queries are
        // Stage-0 deterministic stubs (no tackle flags / pressure model until Phase D/E).

        private sealed class PassWorldAdapter : IPassBallSystem, IPassAgentQuery, IPassCollisionQuery
        {
            private readonly MatchEngine _engine;

            public PassWorldAdapter(MatchEngine engine)
            {
                _engine = engine;
            }

            public bool IsBallPossessedBy(int agentId) => _engine._possessingAgentId == agentId;

            public void ApplyKick(ref BallState ball, Vector3 velocity, Vector3 spin, int agentId, float matchTime)
            {
                BallCollision.ApplyKick(ref ball, velocity, spin, agentId, matchTime, logger: null);
                _engine.ReleasePossessionOnKick(agentId);
            }

            public PassAgentAttributes GetAttributes(int agentId) => _engine.BuildPassAttributes(agentId);

            public PassAgentState GetState(int agentId) => _engine.BuildPassState(agentId);

            // Stage 0: tackle flags arrive with the collision-event consumers (Phase E); pressure model
            // wires in with the AI phase (Phase D). Both return deterministic no-pressure defaults.
            public bool GetAndClearTackleFlag(int agentId) => false;

            public float ComputePressureScalar(Vector2 passerPosition, int passerTeamId) => 0f;
        }

        private sealed class ShotWorldAdapter : IShotBallSystem, IShotAgentQuery, IShotCollisionQuery
        {
            private readonly MatchEngine _engine;

            public ShotWorldAdapter(MatchEngine engine)
            {
                _engine = engine;
            }

            public bool IsBallPossessedBy(int agentId) => _engine._possessingAgentId == agentId;

            public void ApplyKick(ref BallState ball, Vector3 velocity, Vector3 spin, int agentId, float matchTime)
            {
                BallCollision.ApplyKick(ref ball, velocity, spin, agentId, matchTime, logger: null);
                _engine.ReleasePossessionOnKick(agentId);
            }

            public ShotAgentAttributes GetAttributes(int agentId) => _engine.BuildShotAttributes(agentId);

            public ShotAgentState GetState(int agentId) => _engine.BuildShotState(agentId);

            public bool GetAndClearTackleFlag(int agentId) => false;

            public float ComputePressureScalar(Vector3 shooterPosition, int shooterTeamId) => 0f;
        }

        /// <summary>Null-object collision-event consumer (Phase C C1): drains every collision event.
        /// Real cross-subsystem consumers subscribe at Phase E.</summary>
        private sealed class NullCollisionEventConsumer : ICollisionEventConsumer
        {
            public void OnCollisionEvent(in CollisionEvent evt) { }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-16 | —      | Initial implementation — Phase A skeleton & determinism spine: |
// |         |            |        | new composition root, world-state fields, boot, 7 method-group |
// |         |            |        | phase callbacks wired into TickOrchestrator with EventBus      |
// |         |            |        | tick-lifecycle-only stubs and digest-load-bearing snapshot     |
// |         |            |        | serialization. No gameplay subsystems invoked (design note §5).|
// | 1.1     | 2026-06-16 | —      | AR-1: L-1 kickoff/line constant references updated to the      |
// |         |            |        | retagged [DERIVED] names; M-1 TestOnly_SetBallHeight seam added |
// |         |            |        | so a test can prove world state feeds the digest; L-2 static-  |
// |         |            |        | EventBus determinism assumption documented at SerializeLedger; |
// |         |            |        | L-3 payload-tick-vs-header redundancy noted as intentional.    |
// | 1.2     | 2026-06-16 | —      | Phase B step B2 — Physics-phase wiring. World state migrated   |
// |         |            |        | from the Phase-A kinematic float arrays to real BallState +    |
// |         |            |        | AgentState[] plus per-agent input buffers (attrs/perfs/        |
// |         |            |        | commands) and the two collision-feedback buffers. RunPhysics-  |
// |         |            |        | Phase now calls BallPhysicsCore.UpdateBallPhysics (null logger,|
// |         |            |        | GrassDry, no wind) and AgentMovementSystem.UpdateAllAgents     |
// |         |            |        | (skips GKs) with dt = FrameSeconds and the seconds-domain      |
// |         |            |        | clock. Boot seeds Stop commands, default attrs, neutral perfs. |
// |         |            |        | Serialization sources the kinematic subset (position + facing) |
// |         |            |        | from the structs; full field set + schema pin land at B3. New  |
// |         |            |        | test seams: TestOnly_SetBall / BallSnapshot / SetCommand /     |
// |         |            |        | AgentSnapshot / IsGoalkeeper. asmdef gains BallPhysics +       |
// |         |            |        | AgentMovement references.                                      |
// | 1.2.1   | 2026-06-16 | —      | B2 self-review L-1: FacingFromHeading maps the axis-aligned    |
// |         |            |        | kickoff headings (0° / 180°) to exact unit vectors instead of  |
// |         |            |        | Mathf.Cos/Sin, keeping sin(180°)≈8.7e-8 fuzz out of the        |
// |         |            |        | deterministic snapshot; non-cardinal headings still use trig.  |
// | 1.3     | 2026-06-16 | —      | Phase B step B3 — full canonical field-set serialization +    |
// |         |            |        | schema pin. SerializeWorldState now writes the full §2.6 field |
// |         |            |        | set field-by-field (BallState position/velocity/spin/state +   |
// |         |            |        | LastValid*; per-agent full AgentState incl. the OscillationGuard|
// |         |            |        | ring-buffer state via the B0 GetState seam; team/GK flags; the |
// |         |            |        | two collision-feedback inputs; the held MovementCommand) under |
// |         |            |        | MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION (u32), replacing  |
// |         |            |        | the B2 kinematic-subset + PHASE_A_PAYLOAD_FORMAT_VERSION (u8). |
// |         |            |        | New WriteBallState/WriteAgentState/WriteMovementCommand        |
// |         |            |        | helpers (zero-alloc — guard seam returns a value type). New    |
// |         |            |        | TestOnly_SetAgent seam so a test can prove the expanded field  |
// |         |            |        | set feeds the digest.                                          |
// | 1.3.1   | 2026-06-16 | —      | B3 self-AR (0H+1M+2L). M-1: recorded the §2.6 exclusion proof  |
// |         |            |        | for _attrs/_perfs (boot-deterministic constants, passed `in`,  |
// |         |            |        | never mutated mid-sim) + the Phase-A observation counters, with |
// |         |            |        | a PHASE-D flag that _perfs MUST be serialized once the AI phase |
// |         |            |        | writes it. L-1: file-header Modified annotation refreshed B2 →  |
// |         |            |        | B3. (L-2: Modified field added to the new test file header.)    |
// | 1.4     | 2026-06-19 | —      | Phase C C1/C1a/C2/C3 — Resolve-phase wiring. C1: retain        |
// |         |            |        | _matchSeed; construct CollisionSystem(22), a null-object        |
// |         |            |        | ICollisionEventConsumer, the per-agent PassExecutor[22] /       |
// |         |            |        | ShotExecutor[22] instance arrays (resolves §6 item 5 — per-     |
// |         |            |        | agent instance, shared adapter), and _possessingAgentId         |
// |         |            |        | (NO_POSSESSION at kickoff). C1a: PassWorldAdapter /             |
// |         |            |        | ShotWorldAdapter nested classes implement all six executor      |
// |         |            |        | query interfaces over world state (BuildPass*/BuildShot*        |
// |         |            |        | mappers; ERR-007 neutral attribute proxies; fatigue from        |
// |         |            |        | AerobicPool; Stage-0 no-tackle / zero-pressure collision        |
// |         |            |        | stubs). C2: RunResolvePhase calls UpdateCollisions (reuses      |
// |         |            |        | _attrs; stumbleOut discarded; writes the one-tick-lag feedback  |
// |         |            |        | buffers). C3: advances all 22 pass + 22 shot executors via      |
// |         |            |        | Update each Resolve tick; TestOnly_ seams script Execute +      |
// |         |            |        | possession (Phase D AI dispatcher is the production trigger).   |
// |         |            |        | No CONTACT publish reached at Stage 0 (executors idle in        |
// |         |            |        | production / determinism tests; registry boot + possession-flip |
// |         |            |        | completion test land at C4). Snapshot field set unchanged       |
// |         |            |        | (executor/MatchContext state serialized at C5). asmdef gains    |
// |         |            |        | CollisionSystem + PassMechanics + ShotMechanics references.     |
#endregion
