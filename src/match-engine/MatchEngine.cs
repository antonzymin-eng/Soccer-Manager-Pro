// File:     src/match-engine/MatchEngine.cs
// Created:  2026-06-16
// Modified: 2026-06-16 (Phase B step B2 — physics-phase wiring)
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §2–§5, Code Standards #20
// Purpose:  Composition root that owns match world state and drives the deterministic-sim
//           TickOrchestrator 7-phase pipeline. Phase B step B2 wires the Physics phase to the
//           real Ball Physics (#1) and Agent Movement (#2) seams; the AI and Resolve phases
//           remain EventBus-lifecycle-only stubs (design note §5 Phases C–F).

using System;

using Unity.Profiling;
using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Stage 0 match-engine composition root (determinism spine + Physics-phase wiring as of B2).
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
        private readonly MatchClock              _clock;
        private readonly SnapshotCodec           _codec;
        private readonly EnvironmentFingerprint  _fingerprint;
        private readonly TickOrchestrator        _orchestrator;

        // ── Physics subsystems (design note §3) ───────────────────────────────────────
        // AgentMovementSystem is stateless except its pinned physics Hz, so one shared instance
        // serves all 22 agents. BallPhysicsCore is a static class (no instance needed).

        private readonly AgentMovementSystem _movement;

        // ── World state (design note §2.3) ────────────────────────────────────────────
        // Real BallState + AgentState[] driven by the production physics seams (step B2). The full
        // §2.6 field-by-field canonical serialization + SNAPSHOT_SCHEMA_VERSION pin lands in step B3;
        // B2 serializes the kinematic subset (position + facing) sourced from these structs so
        // movement is reflected in the digest chain.

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
        /// Boot-only — not on the hot path.
        /// </summary>
        private static Vector2 FacingFromHeading(float degrees)
        {
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

        /// <summary>Test-only: whether the agent at the given roster index is a goalkeeper
        /// (UpdateAllAgents skips goalkeepers at Stage 0).</summary>
        internal bool TestOnly_IsGoalkeeper(int index) => _isGoalkeeper[index];

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

        /// <summary>Phase 4 — Resolve. Enters the Resolve phase (no collision/executors yet).</summary>
        private void RunResolvePhase()
        {
            EventBus.BeginPhase(PhaseId.Resolve);
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
        /// Writes the interim world state into the snapshot payload in a fixed canonical order,
        /// sourced from the real BallState / AgentState structs. Order is digest-load-bearing and
        /// versioned by PHASE_A_PAYLOAD_FORMAT_VERSION. Step B2 serializes the kinematic subset
        /// (position + facing) so movement is reflected in the digest; the full §2.6 field set
        /// (velocity, state-machine state, LastValid* checkpoints, OscillationGuard) plus the
        /// SNAPSHOT_SCHEMA_VERSION pin land in step B3.
        /// </summary>
        private void SerializeWorldState(SnapshotPayload payload)
        {
            byte[] buf = payload.PayloadBytes;
            int o = payload.BytesWritten;

            CanonicalSerializer.WriteU8(buf, ref o, MatchEngineConstants.PHASE_A_PAYLOAD_FORMAT_VERSION);
            // Tick is also carried in the header; included here so the payload is self-describing
            // when decoded in isolation (replay/save tooling reads the payload directly).
            CanonicalSerializer.WriteU64(buf, ref o, _clock.CurrentTick);

            CanonicalSerializer.WriteF32(buf, ref o, _ball.Position.x);
            CanonicalSerializer.WriteF32(buf, ref o, _ball.Position.y);
            CanonicalSerializer.WriteF32(buf, ref o, _ball.Position.z);

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                CanonicalSerializer.WriteF32 (buf, ref o, _agents[i].Position.x);
                CanonicalSerializer.WriteF32 (buf, ref o, _agents[i].Position.y);
                CanonicalSerializer.WriteF32 (buf, ref o, _agents[i].FacingDirection.x);
                CanonicalSerializer.WriteF32 (buf, ref o, _agents[i].FacingDirection.y);
                CanonicalSerializer.WriteI32 (buf, ref o, _teamIds[i]);
                CanonicalSerializer.WriteBool(buf, ref o, _isGoalkeeper[i]);
            }

            payload.BytesWritten = o;
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
#endregion
