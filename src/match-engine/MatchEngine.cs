// File:     src/match-engine/MatchEngine.cs
// Created:  2026-06-16
// Modified: 2026-06-27 (Phase D D4 — DecisionTree + Positioning hysteresis snapshot; schema 2→4)
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §2–§5, Code Standards #20
// Purpose:  Composition root that owns match world state and drives the deterministic-sim
//           TickOrchestrator 7-phase pipeline. The Physics phase (B2) drives Ball Physics (#1) +
//           Agent Movement (#2); the Resolve phase (Phase C) drives Collision (#3) + the per-agent
//           Pass (#5) / Shot (#6) executor lifecycles via host world-state adapters. The AI phase
//           (Phase D D1) drives Perception (#7) + the per-agent DecisionTree (#8) on the 10 Hz
//           stride tick, emitting movement commands / pass-shot dispatches.

using System;

using Unity.Profiling;
using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.AttackingAI;
using TacticalDirector.BallPhysics;
using TacticalDirector.CollisionSystem;
using TacticalDirector.DecisionTree;
using TacticalDirector.DefensiveAI;
using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;
using TacticalDirector.FirstTouch;
using TacticalDirector.PassMechanics;
using TacticalDirector.PerceptionSystem;
using TacticalDirector.PositioningAI;
using TacticalDirector.PressingAI;
using TacticalDirector.ShotMechanics;

// The collision orchestrator type name (CollisionSystem) collides with its own namespace leaf
// (TacticalDirector.CollisionSystem); alias it to a distinct name so the type is unambiguous here.
using CollisionSubsystem = TacticalDirector.CollisionSystem.CollisionSystem;

// PerceptionSystem and DecisionTree each name a TYPE identical to their namespace leaf
// (TacticalDirector.PerceptionSystem.PerceptionSystem / TacticalDirector.DecisionTree.DecisionTree);
// alias both so the bare names are unambiguous here (parallel to CollisionSubsystem).
using PerceptionSubsystem = TacticalDirector.PerceptionSystem.PerceptionSystem;
using DecisionTreeAI      = TacticalDirector.DecisionTree.DecisionTree;

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
    /// the real Ball Physics (#1) and Agent Movement (#2) seams; the Resolve phase (Phase C) drives
    /// Collision (#3) + the Pass (#5) / Shot (#6) executor lifecycles; the AI phase (Phase D D1)
    /// drives Perception (#7) + the per-agent DecisionTree (#8) on the 10 Hz stride tick.
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

        // First touch (#4, Phase D D3). One stateless FirstTouchSystem instance + one adapter backing
        // both its IBallPhysicsSystem (writes _ball) and IAgentMovementSystem (Stage-0 dribbling no-op)
        // boundaries. Triggered each Resolve when a loose, approaching, ground-level ball reaches the
        // nearest eligible agent (RunFirstTouch). _opponentScratch is the pre-allocated buffer the
        // PressureEvaluator pass reads (one team's positions; zero alloc on the hot path). The system
        // holds no cross-tick state — it writes only _ball (already serialized) and _possessingAgentId
        // (serialized via MatchContext.PossessingAgentId), so the snapshot schema is unchanged at D3.
        private readonly FirstTouchSystem _firstTouch;
        private readonly Vector2[]        _opponentScratch;  // [PLAYERS_PER_TEAM]

        // Authoritative ball possession: agent index [0–21], or NO_POSSESSION (−1) when loose.
        // Read by the executor adapters (IsBallPossessedBy); cleared on ApplyKick. Folded into
        // MatchContext.PossessingAgentId each Resolve (C4); Stage 0 has no production possession
        // producer (kickoff is loose), so a TestOnly_ seam scripts it for the lifecycle tests.
        private int _possessingAgentId;

        // Authoritative match state (Decision Tree #8 §2.2.5) authored by the host each Resolve tick
        // (C4) and read by the next AI tick (Phase D). Folds in possession, ball kinematics, and the
        // home-perspective ball zone (the team-relative zone is derived downstream by the
        // DecisionContextAssembler — authoring it per-team here would reintroduce ERR-008-002).
        // Serialized into the snapshot at C5 (cross-tick state).
        private MatchContext _matchContext;

        // ── AI subsystems (design note §3 / Phase D D1) ───────────────────────────────
        // Perception (#7) + per-agent DecisionTree (#8) drive the AI phase on the 10 Hz stride
        // tick: perception → decision → movement command. Perception owns its OWN broad-phase grid
        // (host-populated each AI tick from agent positions) — distinct from the CollisionSystem's
        // internal grid. The DecisionTrees are 22 per-agent INSTANCES (each holds a cross-tick state
        // machine; the D0 CaptureState seam) sharing one movement controller + this agent's Pass/Shot
        // executor. NOTE: perception's internal RecognitionLatencyTracker / ShoulderCheckScheduler /
        // ball-prev arrays AND the DecisionTree state machine are cross-tick state that is NOT yet
        // serialized — same-seed-in-process determinism holds (both runs evolve identically), but
        // save/restore replay needs get/restore seams + serialization (deferred to D4; design note §6.5).
        private readonly SpatialHashGrid     _perceptionGrid;
        private readonly PerceptionSubsystem _perception;
        private readonly DecisionTreeAI[]    _decisionTrees;     // [SQUAD_SIZE]

        // Per-agent AI input snapshots (§2.5). Stage-0 static (neutral attributes + Stage0Default
        // tactics), assembled once at boot; _hasPossession is the only per-tick-refreshed input.
        private readonly PerceptionAgentAttributes[] _perceptionAttrs;   // [SQUAD_SIZE]
        private readonly DtAgentAttributes[]         _dtAttrs;           // [SQUAD_SIZE]
        private readonly TacticalContext[]           _tacticalContexts;  // [SQUAD_SIZE]
        private readonly bool[]                       _hasPossession;     // [SQUAD_SIZE]

        // ── Mechanics AI (design note §3 / Phase D D2) ────────────────────────────────
        // Positioning AI (#12) drives per-team formation slots fed into each agent's TacticalContext —
        // the DecisionTree MOVE_TO_POSITION / HOLD anchor (§3.1.7), so agents settle into formation
        // shape instead of holding their kickoff scaffold line (the documented D2 off-ball-motion
        // payoff). One PositioningAITick INSTANCE per team (each owns its own §3 hysteresis), with a
        // reused PositioningPerceptionSnapshot filled from world state each AI tick. The #12 formation
        // table is authored attack-toward-+X (single perspective), so the away team's world state is
        // mapped into that canonical frame (180° pitch rotation) before the tick and the resulting slot
        // mapped back — the ERR-008-002 home/away-asymmetry guard applied at the mechanics layer.
        // NOTE (D4 follow-up): the per-team PositioningAITick hysteresis is cross-tick state NOT yet
        // serialized (same class as the D1 perception / DecisionTree internal state) — same-seed
        // in-process determinism holds; save/restore replay needs a get/restore seam (fold into D4).
        private readonly PositioningAITick[]             _positioning;   // [TEAM_COUNT]
        private readonly PositioningPerceptionSnapshot[] _posSnapshots;  // [TEAM_COUNT]

        // Pressing (#13) → Defensive (#14) → Attacking (#15) chain (Phase D D2b). One INSTANCE + reused
        // input snapshot per team, ticked AFTER Positioning each AI tick (Pressing's per-agent PressRole
        // feeds the Defensive snapshot; both read the Positioning slots via the PositioningAIView facade).
        // Each snapshot carries all 22 agents mapped into the acting team's canonical attack-toward-+X
        // frame (MirrorPitchIfAway) and discriminated by TeamId, mirroring the D2a guard. Stage-0 carriers
        // into the decision context: Defensive MarkDirective.OffensiveLineDepth → TacticalContext.Defensive-
        // LineDepth + HasMarkDirective; Attacking run intent → HasAttackIntent. Pressing's PressDirective has
        // no Stage-0 TacticalContext carrier (PressingMode is a static team tactic) — it runs only to feed
        // PressRole to Defensive. NOTE (D4 follow-up): each tick's internal hysteresis is cross-tick state
        // NOT yet serialized (same class as the D1/D2a state) — fold the get/restore seams into D4.
        private readonly PressingAITick[]    _pressing;       // [TEAM_COUNT]
        private readonly PressingSnapshot[]  _pressSnapshots; // [TEAM_COUNT]
        private readonly PassEventRing[]     _passRings;      // [TEAM_COUNT]
        private readonly DefensiveAITick[]   _defensive;      // [TEAM_COUNT]
        private readonly DefensiveSnapshot[] _defSnapshots;   // [TEAM_COUNT]
        private readonly AttackingAITick[]   _attacking;      // [TEAM_COUNT]
        private readonly AttackingSnapshot[] _attackSnapshots;// [TEAM_COUNT]

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

            // §4 step 3 (cont.) — first touch (Phase D D3). One adapter backs both first-touch boundaries
            // (IBallPhysicsSystem writes _ball; IAgentMovementSystem is a Stage-0 dribbling no-op). The
            // opponent-position scratch buffer feeds the per-touch PressureEvaluator pass (one team).
            var firstTouchAdapter = new FirstTouchWorldAdapter(this);
            _firstTouch      = new FirstTouchSystem(firstTouchAdapter, firstTouchAdapter);
            _opponentScratch = new Vector2[MatchEngineConstants.PLAYERS_PER_TEAM];

            // §4 step 3 (cont.) — AI subsystems (Phase D D1). Perception gets its own broad-phase grid
            // (host-populated each AI tick). The per-agent AI input buffers are allocated once and the
            // Stage-0 static snapshots assembled now (needs the kickoff positions + team ids above).
            _perceptionGrid   = new SpatialHashGrid();
            _perception       = new PerceptionSubsystem(_perceptionGrid);
            _perceptionAttrs  = new PerceptionAgentAttributes[MatchEngineConstants.SQUAD_SIZE];
            _dtAttrs          = new DtAgentAttributes[MatchEngineConstants.SQUAD_SIZE];
            _tacticalContexts = new TacticalContext[MatchEngineConstants.SQUAD_SIZE];
            _hasPossession    = new bool[MatchEngineConstants.SQUAD_SIZE];
            InitializeAiSnapshots();

            // §4 step 3 (cont.) — mechanics AI (Phase D D2). One Positioning AI (#12) instance + reused
            // perception snapshot per team; seed each from the kickoff formation so a valid slot exists
            // before the first AI read (the per-tick Tick() refreshes them — RunPositioningAI).
            _positioning  = new PositioningAITick[MatchEngineConstants.TEAM_COUNT];
            _posSnapshots = new PositioningPerceptionSnapshot[MatchEngineConstants.TEAM_COUNT];
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                _positioning[t]  = new PositioningAITick(
                    MatchEngineConstants.STAGE0_FORMATION, MatchEngineConstants.MaxEntityId);
                _posSnapshots[t] = new PositioningPerceptionSnapshot(MatchEngineConstants.PLAYERS_PER_TEAM);
                FillPositioningSnapshot(t, tickIndex: 0);
                _positioning[t].SeedFromFormation(_posSnapshots[t]);
            }

            // §4 step 3 (cont.) — Pressing/Defensive/Attacking chain (Phase D D2b). One INSTANCE + reused
            // 22-agent snapshot per team. Pressing + Attacking take the PositioningAIView facade over this
            // team's Positioning instance; Attacking takes a Stage-0 balanced StyleProfile. Snapshots are
            // filled from world state each AI tick (RunMechanicsAI).
            _pressing        = new PressingAITick[MatchEngineConstants.TEAM_COUNT];
            _pressSnapshots  = new PressingSnapshot[MatchEngineConstants.TEAM_COUNT];
            _passRings       = new PassEventRing[MatchEngineConstants.TEAM_COUNT];
            _defensive       = new DefensiveAITick[MatchEngineConstants.TEAM_COUNT];
            _defSnapshots    = new DefensiveSnapshot[MatchEngineConstants.TEAM_COUNT];
            _attacking       = new AttackingAITick[MatchEngineConstants.TEAM_COUNT];
            _attackSnapshots = new AttackingSnapshot[MatchEngineConstants.TEAM_COUNT];
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                var posView = new PositioningAIView(_positioning[t]);
                _passRings[t]      = new PassEventRing(MatchEngineConstants.STAGE0_PASS_EVENT_RING_CAPACITY);
                _pressing[t]       = new PressingAITick(posView, _passRings[t], MatchEngineConstants.MaxEntityId);
                _pressSnapshots[t] = new PressingSnapshot();
                _defensive[t]      = new DefensiveAITick(MatchEngineConstants.MaxEntityId);
                _defSnapshots[t]   = new DefensiveSnapshot();
                _attacking[t]      = new AttackingAITick(posView, StyleProfile.Possession, MatchEngineConstants.MaxEntityId);
                _attackSnapshots[t] = new AttackingSnapshot();
            }

            // One movement controller forwards every DT-selected movement command into the held
            // _commands buffer (consumed by the Physics phase next, on the same tick). One instance
            // backs all 22 DecisionTrees. Each DecisionTree is constructed with its agent id, this
            // agent's Pass/Shot executor (the dispatch target for PASS/SHOOT), and the match seed.
            var movementController = new HostMovementController(this);
            _decisionTrees = new DecisionTreeAI[MatchEngineConstants.SQUAD_SIZE];
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                _decisionTrees[i] = new DecisionTreeAI(
                    i, movementController, matchSeed, _passExecutors[i], _shotExecutors[i]);
            }

            // §4 step 2 — boot the EventBus registry for the wired producers (Pass #5 / Shot #6) so a
            // pass/shot reaching CONTACT can publish (C4 — without this, ExecuteContact throws
            // ERR_EVT_UNREGISTERED_ORDINAL). EventRegistry.EnsureInitialized() is internal to the
            // event-system assembly, so the host boots via the public, idempotent
            // EventBusRegistrar.Initialize() sites (both carry an s_registered guard, so repeated boot
            // across multiple MatchEngine constructions in one process is a no-op). RegisterExternalRow
            // forces EventRegistry's seeded-row cctor, so no explicit EnsureInitialized is needed.
            // Fully qualified — both spec namespaces expose an EventBusRegistrar.
            TacticalDirector.PassMechanics.EventBusRegistrar.Initialize();
            TacticalDirector.ShotMechanics.EventBusRegistrar.Initialize();

            // Phase D D1 — the DecisionTree publishes DecisionMadeEvent (Tier C, 0x11) every evaluation,
            // and Tier C publish throws for an unregistered ordinal, so boot the DT registrar too. It is
            // idempotent (s_registered guard — audit AR-2 M-11), safe across multiple constructions in
            // one process (the determinism tests build two engines). DecisionMadeEvent is immediate-
            // dispatch (CosmeticChannel) and excluded from the ledger, so it never enters the digest.
            // Perception publishes PerceptionRefreshEvent only on HandleForcedRefresh (not OnHeartbeat),
            // which the host does not call, so no perception registrar boot is required.
            TacticalDirector.DecisionTree.EventBusRegistrar.Initialize();

            // §4 step 4 (cont.) — author the kickoff MatchContext from the seeded world state so it is
            // valid before the first AI read; the Resolve phase re-authors it every tick (C4).
            UpdateMatchContext();

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

        /// <summary>Test-only: a copy of the authoritative MatchContext authored at the last Resolve
        /// (C4). Read after <see cref="RunTick"/> to assert possession / ball-zone authoring.</summary>
        internal MatchContext TestOnly_MatchContext => _matchContext;

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

        /// <summary>Test-only: whether the agent's DecisionTree has dispatched at least one action
        /// (proves the AI pipeline ran and produced a decision rather than aborting at validation).</summary>
        internal bool TestOnly_DtHasDispatched(int agentId) => _decisionTrees[agentId].HasDispatchedAction;

        /// <summary>Test-only: restores an agent's DecisionTree cross-tick state (D0 seam) so a test can
        /// prove the D4 per-agent DecisionTreeState is in the snapshot digest preimage.</summary>
        internal void TestOnly_SetDecisionTreeState(int agentId, in DecisionTreeState state) =>
            _decisionTrees[agentId].RestoreState(state);

        /// <summary>Test-only: the live per-team Positioning AI (#12) hysteresis (D4 CaptureState seam),
        /// so a test can perturb it and prove the positioning hysteresis is in the snapshot digest preimage.</summary>
        internal HysteresisState TestOnly_PositioningState(int teamId) => _positioning[teamId].CaptureState();

        /// <summary>Test-only: the world-space formation slot the mechanics AI (Positioning #12, D2) fed
        /// into the agent's TacticalContext at the last AI tick. Read after <see cref="RunTick"/> to assert
        /// the formation slots feed the decision context and that away-team slots mirror home-team slots.</summary>
        internal Vector2 TestOnly_FormationSlot(int agentId) => _tacticalContexts[agentId].GetFormationSlot(agentId);

        /// <summary>Test-only: the DefensiveLineDepth carrier the Defensive AI (#14, D2b) fed into the
        /// agent's TacticalContext at the last AI tick (MarkDirective.OffensiveLineDepth).</summary>
        internal float TestOnly_DefensiveLineDepth(int agentId) => _tacticalContexts[agentId].DefensiveLineDepth;

        /// <summary>Test-only: the HasMarkDirective carrier (Defensive AI #14, D2b) at the last AI tick.</summary>
        internal bool TestOnly_HasMarkDirective(int agentId) => _tacticalContexts[agentId].HasMarkDirective;

        /// <summary>Test-only: the HasAttackIntent carrier (Attacking AI #15, D2b) at the last AI tick.</summary>
        internal bool TestOnly_HasAttackIntent(int agentId) => _tacticalContexts[agentId].HasAttackIntent;

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
        // Each callback drives the EventBus phase lifecycle. Physics (B2) drives ball + agent-movement;
        // AI (D1) drives perception + decision tree; Resolve (Phase C) drives collision + executors +
        // MatchContext. The Input / Intent phases remain lifecycle-only (controller / set-piece intent
        // wire in at Phases E–F).

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

        /// <summary>Phase 2 — AI (Phase D D1). Stride-gated by the orchestrator (runs only when
        /// tick % AI_PHASE_STRIDE == 0). Does NOT call BeginPhase (handled by RunIntentPhase, so the
        /// EventBus phase stream is invariant across stride/non-stride ticks). Drives the 10 Hz AI
        /// chain: rebuild the perception broad-phase grid + refresh per-tick inputs (§2.5), run the
        /// mechanics AI (Positioning #12 → formation slots into _tacticalContexts, D2), then
        /// PerceptionSystem.OnHeartbeat (×22), then DecisionTree.ReceiveSnapshot (×22). Each DecisionTree
        /// dispatches a MovementCommand into _commands (via the host movement controller, consumed by the
        /// Physics phase that runs next this tick) or a PASS/SHOOT into this agent's executor (advanced in
        /// Resolve). Reads C4's _matchContext. DecisionMadeEvent (Tier C) publishes here in the AI phase.</summary>
        private void RunAiPhase()
        {
            _aiPhaseRanThisTick = true;
            _aiPhaseRunCount++;

            // §2.5 per-tick assembly. Possession is the only per-tick-varying AI input at Stage 0
            // (attributes + tactics are static defaults assembled at boot). Rebuild the broad-phase grid
            // from current agent positions (perception queries it; the host owns population).
            PopulatePerceptionGrid();
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                _hasPossession[i] = i == _possessingAgentId;
            }

            // The AI heartbeat index is the 10 Hz tactical tick (CurrentTick / AI_PHASE_STRIDE). RunAiPhase
            // runs only on stride ticks, so the integer division is exact (no truncation of a partial tick).
            int heartbeat = (int)_clock.CurrentTacticalTick;

            // §2.5 mechanics AI (Phase D D2): refresh the per-team formation slots + tactical carriers into
            // _tacticalContexts BEFORE the DecisionTree reads them below, so each agent's MOVE_TO_POSITION /
            // HOLD anchor is this tick's Positioning AI (#12) slot and its DefensiveLineDepth / Mark / Attack
            // carriers are this tick's Defensive (#14) / Attacking (#15) output rather than the boot scaffold.
            RunMechanicsAI(heartbeat);

            _perception.OnHeartbeat(heartbeat, _agents, _ball, _perceptionAttrs, _hasPossession);

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                // The pressure scalar is computed during the heartbeat and exposed on the per-agent
                // PerceptionDiagnostics (§3.6 / §3.7.2) — it is NOT a FilteredView field. Reuse it rather
                // than re-running PressureEvaluator (same formula + inputs).
                FilteredView view = _perception.GetFilteredView(i);
                float pressureScalar = _perception.GetDiagnostics(i).PressureScalar;
                _decisionTrees[i].ReceiveSnapshot(
                    view, _matchContext, _tacticalContexts[i], _dtAttrs[i],
                    _agents[i], pressureScalar);
            }
        }

        /// <summary>
        /// Rebuilds the perception broad-phase grid from current agent positions (Phase D D1 §2.5).
        /// Clear + point-insert all 22 agents each AI tick. The ball is NOT inserted — ball perception
        /// (#7 §3.5) targets the ball directly via BallState and uses the grid only to find agent
        /// occluders, so the ball is never a candidate. Point insert (radius 0) is sufficient: the
        /// MaxPerceptionRange (120 m) query window spans the whole pitch, so body radius does not affect
        /// candidacy. Zero allocation (grid buffers are pre-allocated).
        /// </summary>
        private void PopulatePerceptionGrid()
        {
            _perceptionGrid.Clear();
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                Vector2 p = _agents[i].Position;
                _perceptionGrid.Insert(
                    i, new Vector3(p.x, p.y, 0f),
                    MatchEngineConstants.PERCEPTION_GRID_POINT_INSERT_RADIUS);
            }
        }

        /// <summary>
        /// Assembles the Stage-0 static per-agent AI input snapshots once at boot (Phase D D1 §2.5).
        /// Perception attributes use neutral cognition with the agent's real TeamId (it discriminates
        /// teammate vs opponent shadow cones). DT attributes are CreateDefault(teamId). The tactical
        /// context is Stage0Default with the agent's kickoff position as its formation slot. This is the
        /// boot value used until the first AI stride tick; from then on RunPositioningAI (D2) overwrites
        /// the formation slot with the live Positioning AI #12 slot each tick. _hasPossession defaults false.
        /// </summary>
        private void InitializeAiSnapshots()
        {
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                int teamId = _teamIds[i];

                _perceptionAttrs[i]        = PerceptionAgentAttributes.CreateDefault();
                _perceptionAttrs[i].TeamId = teamId;

                _dtAttrs[i]          = DtAgentAttributes.CreateDefault(teamId);
                _tacticalContexts[i] = TacticalContext.Stage0Default(_agents[i].Position);
                _hasPossession[i]    = false;
            }
        }

        /// <summary>
        /// Mechanics AI (Phase D D2): runs the per-team Positioning (#12) → Pressing (#13) → Defensive (#14)
        /// → Attacking (#15) chain and folds each agent's formation slot + tactical carriers into its
        /// <see cref="TacticalContext"/> for the DecisionTree to read. Per team it fills each subsystem's
        /// snapshot from current world state, ticks in dependency order (Pressing's per-agent PressRole feeds
        /// the Defensive snapshot), then writes back: <c>GetFormationSlot(entityId)</c> → the MOVE_TO_POSITION
        /// / HOLD anchor; Defensive <c>MarkDirective.OffensiveLineDepth</c> → <c>DefensiveLineDepth</c> +
        /// <c>HasMarkDirective</c> (ERR-014-001; raised only for the team WITHOUT the ball — the Stage-1
        /// <c>MarkDirective?</c> = null shape for attackers); Attacking run intent → <c>HasAttackIntent</c> (ERR-015-002).
        /// The away team's world state is mapped into the canonical attack-toward-+X frame for every snapshot
        /// and the formation slot mapped back to world space (180° pitch rotation, <see cref="MirrorPitchIfAway"/>),
        /// so the single-perspective #12 / #13 / #14 / #15 authoring positions both teams correctly (the
        /// ERR-008-002 home/away-asymmetry guard at the mechanics layer). Deterministic (no RNG). Pressing's
        /// PressDirective has no Stage-0 carrier (PressingMode is a static team tactic) so it runs only to
        /// feed PressRole to Defensive; the Stage-0 default Pressing / Passing tactics stay the Stage0Default.
        /// </summary>
        private void RunMechanicsAI(int tacticalTick)
        {
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                // Positioning (#12) — formation slots + the Line/Phase inputs the rest of the chain reads.
                FillPositioningSnapshot(t, tacticalTick);
                ContextModifierInputs modifiers = new ContextModifierInputs(
                    scoreDiff:         0,
                    teamMeanFatigue:   ComputeTeamMeanFatigue(t),
                    tacticalIntensity: MatchEngineConstants.STAGE0_TACTICAL_INTENSITY);
                _positioning[t].Tick(_posSnapshots[t], modifiers);

                // Pressing (#13) — per-agent PressRole consumed by the Defensive snapshot below.
                FillPressingSnapshot(t, tacticalTick);
                _pressing[t].Tick(_pressSnapshots[t]);

                // Defensive (#14) — team-level MarkDirective; OffensiveLineDepth is the DecisionContext carrier.
                FillDefensiveSnapshot(t, tacticalTick);
                _defensive[t].Tick(_defSnapshots[t]);
                MarkDirective mark = _defensive[t].GetMarkDirective();

                // Attacking (#15) — per-agent AttackIntent; a committed run is the HasAttackIntent carrier.
                FillAttackingSnapshot(t, tacticalTick);
                _attacking[t].Tick(_attackSnapshots[t]);

                // A Defensive MarkDirective applies only to the team WITHOUT the ball (when this team has
                // possession its agents attack and carry no mark — the Stage-1 MarkDirective? = null shape).
                int owner = _possessingAgentId;
                bool teamHasPossession = owner >= 0 && _teamIds[owner] == t;

                for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
                {
                    int i = t * MatchEngineConstants.PLAYERS_PER_TEAM + k;

                    Vector2 canonicalSlot = _positioning[t].GetFormationSlot(i);
                    // A sentinel slot (inactive agent — none at Stage 0) would corrupt under the 180°
                    // map (PITCH − (−∞) = +∞); fall back to the agent's own position in that case.
                    Vector2 worldSlot = PositioningAITick.IsSentinelSlot(canonicalSlot)
                        ? _agents[i].Position
                        : MirrorPitchIfAway(t, canonicalSlot);

                    // Rebuild the Stage-0 TacticalContext around the live formation slot, then overlay the
                    // Mechanics-AI carriers. Pressing / Passing tactics stay the Stage0Default (no Stage-0
                    // carrier). OffensiveLineDepth is frame-invariant ([0,1] depth), so no inverse map needed.
                    TacticalContext ctx = TacticalContext.Stage0Default(worldSlot);
                    ctx.DefensiveLineDepth = mark.OffensiveLineDepth;
                    ctx.HasMarkDirective   = !teamHasPossession;
                    ctx.HasAttackIntent    = HasActiveAttackIntent(_attacking[t].GetIntent(i));
                    _tacticalContexts[i]   = ctx;
                }
            }
        }

        /// <summary>
        /// True when the Attacking AI (#15) produced a committed off-ball run for this agent (a non-null
        /// <see cref="RunParameters"/>). Stage-0 boolean stand-in for the ERR-015-002 <c>AttackIntent[]?</c>
        /// carrier; a HoldWidth/SupportBall/WeakSide intent without a run is not flagged.
        /// </summary>
        private static bool HasActiveAttackIntent(in AttackIntent intent)
        {
            return intent.RunParameters.HasValue;
        }

        /// <summary>
        /// Fills team <paramref name="team"/>'s reused <see cref="PositioningPerceptionSnapshot"/> from
        /// current world state. Agents are written in roster order (k = 0..PLAYERS_PER_TEAM−1), which is
        /// EntityId-ascending (EntityId = roster index = team·PLAYERS_PER_TEAM + k), as #12 requires.
        /// Positions, the ball, and the longitudinal ball velocity are mapped into the canonical
        /// attack-toward-+X frame (identity for the home team, 180° pitch rotation for the away team).
        /// </summary>
        private void FillPositioningSnapshot(int team, int tickIndex)
        {
            PositioningPerceptionSnapshot snap = _posSnapshots[team];
            FormationSlotRecord[] formation =
                PositioningAIConstants.GetFormationSlots(MatchEngineConstants.STAGE0_FORMATION);

            snap.TickIndex      = tickIndex;
            snap.BallPosition   = MirrorPitchIfAway(team, _ball.Position);
            snap.BallVxFiltered = team == 0 ? _ball.Velocity.x : -_ball.Velocity.x;

            int owner = _possessingAgentId;
            snap.PossessionOwnerEntityId  = owner;
            snap.PossessionOwnerIsOwnTeam = owner >= 0 && _teamIds[owner] == team;

            int activeOutfield = 0;
            for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
            {
                int i = team * MatchEngineConstants.PLAYERS_PER_TEAM + k;
                bool isGk = _isGoalkeeper[i];

                snap.Agents[k] = new AgentPositioningData(
                    entityId:     i,
                    slotIndex:    k,
                    position:     MirrorPitchIfAway(team, _agents[i].Position),
                    isActive:     true,                 // Stage 0: no substitutions / red cards yet
                    role:         formation[k].Role,
                    isGoalkeeper: isGk);

                if (!isGk) activeOutfield++;
            }
            snap.ActiveOutfieldCount = activeOutfield;
        }

        /// <summary>
        /// Fills team <paramref name="team"/>'s reused <see cref="PressingSnapshot"/> (Phase D D2b). Carries
        /// all 22 agents discriminated by <c>TeamId</c>, mapped into the acting team's canonical
        /// attack-toward-+X frame (<see cref="MirrorPitchIfAway"/> for positions, the 180° direction rotation
        /// for velocities/facing). Own-team agents take their Positioning AI (#12) slot + line; opponents take
        /// a position placeholder + neutral line (consumed only for own-team hold-shape geometry). Touch
        /// quality is the perfect-touch identity so the Stage-0 BadTouch trigger never fires.
        /// </summary>
        private void FillPressingSnapshot(int team, int tickIndex)
        {
            PressingSnapshot snap = _pressSnapshots[team];
            int owner = _possessingAgentId;

            snap.TickIndex           = tickIndex;
            snap.BallPosition        = MirrorPitchIfAway(team, _ball.Position);
            snap.BallVelocity        = MirrorVelocityIfAway(team, _ball.Velocity);
            snap.BallCarrierEntityId = owner;
            snap.AttackingDirection  = CanonicalAttackDir(team);
            snap.PossessionTeamId    = owner >= 0 ? _teamIds[owner] : MatchEngineConstants.NO_POSSESSION;
            snap.PressingTeamId      = team;

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                bool isOwn = _teamIds[i] == team;
                snap.Agents[i] = new PressingAgentSnapshot
                {
                    EntityId            = i,
                    TeamId              = _teamIds[i],
                    Position            = MirrorPitchIfAway(team, _agents[i].Position),
                    Velocity            = MirrorVelocityIfAway(team, _agents[i].Velocity),
                    Facing              = MirrorVelocityIfAway(team, _agents[i].FacingDirection),
                    Fatigue             = 1f - _agents[i].AerobicPool,
                    FirstTouchAttribute = MatchEngineConstants.STAGE0_NEUTRAL_ATTRIBUTE,
                    LastTouchQuality    = 1f,   // perfect touch ⇒ no Stage-0 BadTouch trigger
                    PostTouchBallSpeed  = 0f,
                    IsGoalkeeper        = _isGoalkeeper[i],
                    HasBall             = i == owner,
                    IsActive            = true,
                    BaselineSlot        = isOwn ? _positioning[team].GetFormationSlot(i)
                                                : MirrorPitchIfAway(team, _agents[i].Position),
                    Line                = isOwn ? _positioning[team].GetLine(i) : LineId.Midfield,
                };
            }
        }

        /// <summary>
        /// Fills team <paramref name="team"/>'s reused <see cref="DefensiveSnapshot"/> (Phase D D2b). The
        /// per-agent <c>PressRole</c> is read back from this team's Pressing AI (#13) output, completing the
        /// Positioning→Pressing→Defensive chain. All 22 agents are carried in the canonical attack-+X frame;
        /// the team phase is the Positioning AI phase and the line depth is the Stage-0 default (echoed into
        /// <see cref="MarkDirective.OffensiveLineDepth"/>). The team's goalkeeper anchors the COVER_GK_ZONE
        /// last-man check (§3.9).
        /// </summary>
        private void FillDefensiveSnapshot(int team, int tickIndex)
        {
            DefensiveSnapshot snap = _defSnapshots[team];
            int owner = _possessingAgentId;
            Vector2 ballXY = new Vector2(_ball.Position.x, _ball.Position.y);
            Vector2 ballVelXY = new Vector2(_ball.Velocity.x, _ball.Velocity.y);

            snap.TickIndex               = tickIndex;
            snap.DefensiveTeamId         = team;
            snap.BallPosition            = MirrorPitchIfAway(team, ballXY);
            snap.BallVelocity            = MirrorVelocityIfAway(team, ballVelXY);
            snap.PossessionOwnerEntityId = owner;
            snap.TeamPhase               = _positioning[team].GetPhase();
            snap.DefensiveLineDepth      = MatchEngineConstants.STAGE0_DEFENSIVE_LINE_DEPTH;
            snap.AgentCount              = MatchEngineConstants.SQUAD_SIZE;
            snap.HasActivePrimaryPress   = _pressing[team].LastDirective.IsActive;

            int gkEntity = MatchEngineConstants.NO_POSSESSION;
            Vector2 gkPos = Vector2.zero;
            for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
            {
                int g = team * MatchEngineConstants.PLAYERS_PER_TEAM + k;
                if (_isGoalkeeper[g])
                {
                    gkEntity = g;
                    gkPos    = MirrorPitchIfAway(team, _agents[g].Position);
                    break;
                }
            }
            snap.GkEntityId = gkEntity;
            snap.GkPosition = gkPos;

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                bool isOwn = _teamIds[i] == team;
                snap.Agents[i] = new DefensiveAgentSnapshot
                {
                    EntityId            = i,
                    TeamId              = _teamIds[i],
                    Position            = MirrorPitchIfAway(team, _agents[i].Position),
                    Velocity            = MirrorVelocityIfAway(team, _agents[i].Velocity),
                    IsActive            = true,
                    IsGoalkeeper        = _isGoalkeeper[i],
                    HasBall             = i == owner,
                    BaselineSlot        = isOwn ? _positioning[team].GetFormationSlot(i)
                                                : MirrorPitchIfAway(team, _agents[i].Position),
                    Line                = isOwn ? _positioning[team].GetLine(i) : LineId.Midfield,
                    PressRole           = _pressing[team].GetAssignment(i).Role,
                    PerceivedFirstTouch = MatchEngineConstants.STAGE0_NEUTRAL_ATTRIBUTE,
                };
            }
        }

        /// <summary>
        /// Fills team <paramref name="team"/>'s reused <see cref="AttackingSnapshot"/> (Phase D D2b). All 22
        /// agents are carried in the acting team's canonical attack-+X frame, so the team attack angle is 0.
        /// Stamina is the live fatigue (1 − AerobicPool); pace / dribbling are the Stage-0 neutral normalised
        /// placeholder (§2.3 — not consumed by the Stage-0 RUNNER algorithm).
        /// </summary>
        private void FillAttackingSnapshot(int team, int tickIndex)
        {
            AttackingSnapshot snap = _attackSnapshots[team];
            int owner = _possessingAgentId;
            Vector2 ballXY = new Vector2(_ball.Position.x, _ball.Position.y);

            snap.TickIndex           = tickIndex;
            snap.AttackingTeamId     = team;
            snap.BallPosition        = MirrorPitchIfAway(team, ballXY);
            snap.BallCarrierEntityId = owner;
            snap.BallCarrierPosition = owner >= 0
                ? MirrorPitchIfAway(team, _agents[owner].Position)
                : MirrorPitchIfAway(team, ballXY);
            snap.TeamAttackAngle     = 0f;   // acting team attacks +X in its canonical frame

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                bool isOwn = _teamIds[i] == team;
                snap.Agents[i] = new AttackingAgentSnapshot(
                    entityId:     i,
                    teamId:       _teamIds[i],
                    position:     MirrorPitchIfAway(team, _agents[i].Position),
                    baselineSlot: isOwn ? _positioning[team].GetFormationSlot(i)
                                        : MirrorPitchIfAway(team, _agents[i].Position),
                    line:         isOwn ? _positioning[team].GetLine(i) : LineId.Midfield,
                    isGoalkeeper: _isGoalkeeper[i],
                    hasBall:      i == owner,
                    isActive:     true,
                    pace:         MatchEngineConstants.STAGE0_NEUTRAL_NORMALIZED,
                    stamina:      1f - _agents[i].AerobicPool,
                    dribbling:    MatchEngineConstants.STAGE0_NEUTRAL_NORMALIZED);
            }
        }

        /// <summary>
        /// Canonical attack direction (acting team frame) of whichever team currently holds the ball: +X when
        /// the acting team (or no one) holds it, −X when the opponent holds it. Unit vector; never zero.
        /// </summary>
        private Vector2 CanonicalAttackDir(int team)
        {
            int owner = _possessingAgentId;
            if (owner >= 0 && _teamIds[owner] != team) return new Vector2(-1f, 0f);
            return new Vector2(1f, 0f);
        }

        /// <summary>Mean fatigue [0,1] across team <paramref name="team"/> (0 fully rested, 1 fully
        /// fatigued, per the project convention), derived from each agent's AerobicPool reservoir as
        /// fatigue = 1 − pool (a full pool means the agent is rested).</summary>
        private float ComputeTeamMeanFatigue(int team)
        {
            float sum = 0f;
            for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
            {
                int i = team * MatchEngineConstants.PLAYERS_PER_TEAM + k;
                sum += 1f - _agents[i].AerobicPool;
            }
            return sum / MatchEngineConstants.PLAYERS_PER_TEAM;
        }

        /// <summary>
        /// Maps a world-space position into / out of the canonical attack-toward-+X frame used by the
        /// Positioning AI (#12) formation table: identity for the home team (team 0, which attacks +X),
        /// a 180° pitch rotation (x → LENGTH−x, y → WIDTH−y) for the away team (team 1, which attacks −X).
        /// The rotation is its own inverse, so the same call maps world→canonical when filling the
        /// snapshot and canonical→world when reading the computed slot back.
        /// </summary>
        private static Vector2 MirrorPitchIfAway(int team, Vector2 p)
        {
            if (team == 0) return p;
            return new Vector2(
                MatchEngineConstants.PITCH_LENGTH_M - p.x,
                MatchEngineConstants.PITCH_WIDTH_M  - p.y);
        }

        /// <summary>Vector3 overload of <see cref="MirrorPitchIfAway(int, Vector2)"/> preserving Z (height,
        /// frame-invariant).</summary>
        private static Vector3 MirrorPitchIfAway(int team, Vector3 p)
        {
            if (team == 0) return p;
            return new Vector3(
                MatchEngineConstants.PITCH_LENGTH_M - p.x,
                MatchEngineConstants.PITCH_WIDTH_M  - p.y,
                p.z);
        }

        /// <summary>
        /// Maps a world-space velocity/direction into / out of the canonical attack-+X frame. Unlike a
        /// position (an affine point — <see cref="MirrorPitchIfAway"/>), a velocity is a free vector, so the
        /// away-team 180° rotation negates both planar components (no PITCH offset). Self-inverse.
        /// </summary>
        private static Vector2 MirrorVelocityIfAway(int team, Vector2 v)
        {
            return team == 0 ? v : new Vector2(-v.x, -v.y);
        }

        /// <summary>Vector3 overload of <see cref="MirrorVelocityIfAway(int, Vector2)"/> preserving Z
        /// (height velocity, frame-invariant).</summary>
        private static Vector3 MirrorVelocityIfAway(int team, Vector3 v)
        {
            return team == 0 ? v : new Vector3(-v.x, -v.y, v.z);
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

        /// <summary>Phase 4 — Resolve. Runs collision (×22), advances the in-flight pass/shot executor
        /// lifecycles (C2/C3), runs first touch on a loose arriving ball (D3), then authors the
        /// authoritative <see cref="MatchContext"/> from the settled world state (C4). Intra-Resolve
        /// order is fixed and digest-load-bearing: collision → executor Update → first touch →
        /// possession/MatchContext. Collision writes THIS tick's feedback buffers (consumed by movement
        /// next tick — the §3 one-tick-lag contract); the executors advance any pass/shot scripted via the
        /// TestOnly_ seam (production trigger is the Phase D AI dispatcher), kicking the ball at CONTACT
        /// through the executor adapters and releasing possession; first touch (D3) receives a loose
        /// approaching ball and may re-establish possession. MatchContext is authored last so it reflects
        /// post-kick / post-touch possession; it is read by the next AI tick (Phase D).</summary>
        private void RunResolvePhase()
        {
            EventBus.BeginPhase(PhaseId.Resolve);

            int   frameNumber = (int)_clock.CurrentTick;          // narrows safely at Stage 0 (~414 days @ 60 Hz)
            float matchTime   = _clock.CurrentMatchTimeSeconds;

            // C2 — collision first. Reuses _attrs (PlayerAttributes[]); writes _isCollisionKnockdown /
            // _collisionForces (consumed by movement at tick N+1). stumbleOut is discarded (B4 — not a
            // Stage-0 movement input). Self-seeds its own RNG from _matchSeed ^ frameNumber internally.
            // NOTE: UpdateCollisions processes ALL 22 agents incl. goalkeepers, whereas Physics-phase
            // UpdateAllAgents skips GKs (Stage 0 — GK locomotion is #11). A GK can therefore be
            // displaced by a collision that movement never re-integrates; benign at Stage 0 (kickoff
            // spread admits no GK collisions) and inherent to the two seams, recorded here for Phase D.
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

            // D3 — first touch. A loose, approaching, ground-level ball arriving within reach of an agent
            // is received here (a CONTROLLED touch gains possession; an INTERCEPTION flips it to the
            // opponent; a LOOSE_BALL / DEFLECTION redirects the ball but leaves it loose). Runs AFTER the
            // executors so the same-tick kick that releases possession is visible (the ball is loose), and
            // BEFORE C4 so MatchContext reflects any possession gained by the touch.
            RunFirstTouch();

            // C4 — author MatchContext last, so it reflects this tick's settled possession (a CONTACT
            // kick above released possession, or a D3 first touch) and ball kinematics. Read by the next
            // AI tick (Phase D).
            UpdateMatchContext();
        }

        /// <summary>
        /// Authors the authoritative <see cref="MatchContext"/> from the current world state (C4).
        /// Called at the end of Resolve (after possession settles) and once at boot. Stage 0 has no
        /// scoring or match-flow producer, so score is 0 and the phase is a fixed OPEN_PLAY (the running
        /// tick loop is open play; Phase D / match-flow logic drives real phase transitions). The ball
        /// zone is authored from
        /// the HOME-team perspective ONLY — the DecisionContextAssembler derives the team-relative zone
        /// downstream (ERR-008-002 regression guard); re-deriving it per-team here would invert away-team
        /// zone modifiers.
        /// </summary>
        private void UpdateMatchContext()
        {
            _matchContext.HomeScore        = 0;
            _matchContext.AwayScore        = 0;
            _matchContext.MatchTimeSeconds = _clock.CurrentMatchTimeSeconds;

            _matchContext.PossessingAgentId = _possessingAgentId;
            // A valid possessing index 0 ≤ i < SQUAD_SIZE resolves to its team; NO_POSSESSION — or any
            // out-of-range value, a defensive guard against a future Phase-D possession producer
            // writing a stale index into the digest path — is CONTESTED (the project sanitize-to-safe
            // pattern, parallel to the NaN gates; the bounds check cannot throw on the _teamIds access).
            bool possessed = _possessingAgentId >= 0 && _possessingAgentId < MatchEngineConstants.SQUAD_SIZE;
            _matchContext.Possession = !possessed
                ? PossessionState.CONTESTED
                : (_teamIds[_possessingAgentId] == 0 ? PossessionState.HOME_TEAM : PossessionState.AWAY_TEAM);

            // Stage 0 has no kickoff ceremony or set-piece state machine — the running tick loop IS
            // open play, so author OPEN_PLAY. (Phase D / match-flow drives real KICK_OFF→OPEN_PLAY and
            // set-piece transitions.) NOTE: this MUST be OPEN_PLAY, not KICK_OFF — the OptionGenerator
            // returns zero options for any non-OPEN_PLAY phase (§3.1), so KICK_OFF would silently make
            // the entire Phase D AI a no-op (every agent falls back to HOLD).
            _matchContext.Phase = MatchPhase.OPEN_PLAY;

            _matchContext.BallPosition = new Vector2(_ball.Position.x, _ball.Position.y);
            _matchContext.BallVelocity = _ball.Velocity;
            _matchContext.BallZone     = PitchGeometry.ComputeFieldZone(_ball.Position.x); // home-perspective only
        }

        /// <summary>
        /// First touch (Phase D D3). When a loose, ground-level ball is moving and arrives within
        /// <see cref="MatchEngineConstants.FIRST_TOUCH_ACCEPTANCE_RADIUS_M"/> of an approaching agent,
        /// the host assembles a <see cref="FirstTouchContext"/> (incl. a <c>PressureEvaluator</c> pass for
        /// PressureScalar / NearestOpponent* and an <c>OrientationDetector</c> pass for IsHalfTurnOriented),
        /// runs <see cref="FirstTouchSystem.EvaluateFirstTouch"/> + <see cref="FirstTouchSystem.ApplyTouchResult"/>,
        /// and maps the outcome onto authoritative possession: CONTROLLED → the toucher, INTERCEPTION →
        /// the intercepting opponent (AGENT_ID_NONE at Stage 0 — the §3.4.2 interceptor id is a spec gap,
        /// ERR-004-002 — so possession is released to loose), LOOSE_BALL / DEFLECTION → stays loose.
        ///
        /// Eligibility gates (all required): the ball is loose (a possessed ball is already controlled);
        /// the ball centre is at or below ground-control height (a higher ball is a Heading #10 event, not
        /// Stage 0); the ball is moving above the min-speed gate; and the agent is APPROACHED by the ball
        /// (ball velocity · agent-from-ball &gt; 0). The closing-direction gate is what excludes the agent
        /// the ball just departed after a kick — its dot is negative — so a kicker never re-touches the
        /// ball it just played. The nearest such agent is the toucher. Deterministic (no RNG); first-touch
        /// is a pure function of world state + public/internal First Touch formulas.
        /// </summary>
        private void RunFirstTouch()
        {
            // Gate 1 — only a loose ball can be received; a possessed ball is already under control.
            if (_possessingAgentId != MatchEngineConstants.NO_POSSESSION)
            {
                return;
            }

            // Gate 2 — ground control only. Ball centre height above the surface = z − RADIUS; above the
            // GroundControlHeight threshold the ball is a Heading Mechanics (#10) event (not Stage 0).
            float ballHeight = _ball.Position.z - FirstTouchConstants.BallRadius;
            if (ballHeight > FirstTouchConstants.GroundControlHeight)
            {
                return;
            }

            // Gate 3 — the ball must be in motion (a resting loose ball is not an incoming receive).
            Vector2 ballPosXY = new Vector2(_ball.Position.x, _ball.Position.y);
            Vector2 ballVelXY = new Vector2(_ball.Velocity.x, _ball.Velocity.y);
            float minSpeed = MatchEngineConstants.FIRST_TOUCH_MIN_BALL_SPEED_M_S;
            if (ballVelXY.sqrMagnitude < minSpeed * minSpeed)
            {
                return;
            }

            // Gate 4 — nearest APPROACHING agent within the acceptance reach. "Approaching" = the ball is
            // closing on the agent (velocity · (agentPos − ballPos) > 0); this excludes the just-kicked
            // owner (the ball recedes from it). Squared-distance compare; bestSq shrinks only on a
            // STRICTLY closer candidate, so an exact-distance tie keeps the lower roster index (snapshot
            // order, matching the project's other proximity tie-breaks — DT §3.1.3.6). The acceptance
            // boundary is inclusive (distSq == acceptanceSq is in reach) via the first-candidate clause.
            float acceptanceSq = MatchEngineConstants.FIRST_TOUCH_ACCEPTANCE_RADIUS_M
                               * MatchEngineConstants.FIRST_TOUCH_ACCEPTANCE_RADIUS_M;
            int   toucher = MatchEngineConstants.NO_POSSESSION;
            float bestSq  = acceptanceSq;
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                Vector2 toAgent = _agents[i].Position - ballPosXY;
                float distSq = toAgent.sqrMagnitude;
                if (distSq > bestSq)
                {
                    continue; // outside reach, or not closer than the current best
                }
                if (Vector2.Dot(ballVelXY, toAgent) <= 0f)
                {
                    continue; // ball receding from this agent — not a receive
                }
                if (toucher == MatchEngineConstants.NO_POSSESSION || distSq < bestSq)
                {
                    bestSq  = distSq;
                    toucher = i;
                }
            }
            if (toucher == MatchEngineConstants.NO_POSSESSION)
            {
                return;
            }

            // Assemble the per-touch context, evaluate, and apply. ApplyTouchResult writes the displaced
            // ball state via the adapter; the host owns the possession transition from the outcome.
            FirstTouchContext context = BuildFirstTouchContext(toucher);
            FirstTouchResult  result  = _firstTouch.EvaluateFirstTouch(context);
            _firstTouch.ApplyTouchResult(result, context);

            switch (result.PossessionOutcome)
            {
                case TouchResult.Controlled:
                    _possessingAgentId = result.PossessingAgentID;
                    break;
                case TouchResult.Interception:
                {
                    // The intercepting opponent gains possession. At Stage 0 the interceptor id is
                    // unresolved (ERR-004-002 spec gap — FirstTouchContext does not expose it), so
                    // InterceptingAgentID is AGENT_ID_NONE. Map any unresolved / out-of-range id to
                    // NO_POSSESSION explicitly rather than trusting the AGENT_ID_NONE == NO_POSSESSION
                    // cross-assembly sentinel coincidence: the ball is loose, redirected toward the
                    // opponent (§3.4.5), to be re-received on a later tick. A Stage-1 in-range
                    // interceptor id is taken as-is.
                    int interceptor = result.InterceptingAgentID;
                    _possessingAgentId = interceptor >= 0 && interceptor < MatchEngineConstants.SQUAD_SIZE
                        ? interceptor
                        : MatchEngineConstants.NO_POSSESSION;
                    break;
                }
                default:
                    // LOOSE_BALL / DEFLECTION — ball redirected but uncontrolled; possession stays loose.
                    _possessingAgentId = MatchEngineConstants.NO_POSSESSION;
                    break;
            }
        }

        /// <summary>
        /// Assembles the <see cref="FirstTouchContext"/> for the receiving agent (Phase D D3). Player
        /// touch attributes (Technique / FirstTouch) are Stage-0 neutral placeholders — Agent Movement #2
        /// PlayerAttributes carries no such fields yet (ERR-007), the same synthesis the pass/shot
        /// adapters use. Pressure / nearest-opponent data come from a <c>PressureEvaluator</c> pass over
        /// the opposing team (filling <see cref="_opponentScratch"/>, zero alloc), and
        /// <see cref="OrientationDetector.IsHalfTurnOriented"/> supplies the half-turn flag against the
        /// incoming ball direction. The intended touch direction defaults to the agent's facing (no
        /// movement-target carrier at Stage 0; HasMovementTarget = false).
        /// </summary>
        private FirstTouchContext BuildFirstTouchContext(int i)
        {
            int teamId       = _teamIds[i];
            int opponentTeam = MatchEngineConstants.TEAM_COUNT - 1 - teamId; // 0 ↔ 1

            // Fill the opponent-position scratch buffer (the whole opposing team, GK included).
            for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
            {
                int oi = opponentTeam * MatchEngineConstants.PLAYERS_PER_TEAM + k;
                _opponentScratch[k] = _agents[oi].Position;
            }

            Vector2 agentPosXY = _agents[i].Position;
            // Fully qualified: TacticalDirector.PerceptionSystem also exposes a public PressureEvaluator
            // (the same §3.5 formula), so the bare name is ambiguous (CS0104) under both usings — the
            // first-touch producer is the one whose PressureResult this context consumes. (Parallel to the
            // fully-qualified EventBusRegistrar.Initialize() calls — both spec namespaces expose that type.)
            PressureResult pressure = TacticalDirector.FirstTouch.PressureEvaluator.Evaluate(
                agentPosXY,
                new ReadOnlySpan<Vector2>(_opponentScratch, 0, MatchEngineConstants.PLAYERS_PER_TEAM));

            // Normalise: the FirstTouchContext contract treats AgentFacing / IntendedTouchDirection as
            // unit vectors, and OrientationDetector's angle math assumes a unit facing (it clamps the dot
            // before Acos, so a non-unit facing only skews the half-turn angle). Unity's Vector2.normalized
            // returns zero for a degenerate facing, which routes through the §3.6 / §3.3.2 zero-input
            // fallbacks — at Stage 0 facings are non-degenerate (boot ±X, maintained by movement).
            Vector2 facing = _agents[i].FacingDirection.normalized;
            Vector2 ballVelXY = new Vector2(_ball.Velocity.x, _ball.Velocity.y);
            bool isHalfTurn = OrientationDetector.IsHalfTurnOriented(facing, ballVelXY);

            int neutralAttr = Mathf.RoundToInt(MatchEngineConstants.STAGE0_NEUTRAL_ATTRIBUTE);
            Vector3 facing3 = new Vector3(facing.x, facing.y, 0f);

            return new FirstTouchContext
            {
                AgentID                   = i,
                TeamID                    = teamId,
                Technique                 = neutralAttr,
                FirstTouchAttribute       = neutralAttr,
                AgentPosition             = new Vector3(agentPosXY.x, agentPosXY.y, 0f),
                AgentVelocity             = new Vector3(_agents[i].Velocity.x, _agents[i].Velocity.y, 0f),
                AgentFacing               = facing3,
                IntendedTouchDirection    = facing3,
                HasMovementTarget         = false,
                BallPosition              = _ball.Position,
                BallVelocity              = _ball.Velocity,
                BallHeight                = _ball.Position.z - FirstTouchConstants.BallRadius,
                BallIsAirborne            = _ball.State == BallStateType.Airborne,
                PressureScalar            = pressure.PressureScalar,
                HasNearbyOpponent         = pressure.HasNearbyOpponent,
                NearestOpponentDistance   = pressure.NearestOpponentDistance,
                NearestOpponentPositionXY = pressure.NearestOpponentPositionXY,
                IsHalfTurnOriented        = isHalfTurn,
                IsGoalkeeper              = _isGoalkeeper[i]
            };
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
            // PHASE-D FLAG: the AI phase still does NOT write per-agent form/fatigue context into
            // _perfs (it stays the boot-neutral constant) — when it begins to, _perfs becomes
            // cross-tick state and MUST be serialized here (bump SNAPSHOT_SCHEMA_VERSION at that point).
            //
            // EXCLUSION PROOF — perception internal state + remaining mechanics-AI hysteresis (D4): the
            // perception RecognitionLatencyTracker / ShoulderCheckScheduler / ball-prev arrays and the
            // per-team Pressing/Defensive/Attacking hysteresis ARE cross-tick state but are NOT serialized
            // at v4 — none expose a get/restore seam yet. Same-seed in-process determinism is unaffected
            // (both runs init at boot and evolve identically); only save/restore replay needs them, so
            // they are deferred to a follow-up snapshot extension (which will add the seams + serialization
            // and bump SNAPSHOT_SCHEMA_VERSION again). The per-team Positioning (#12) hysteresis IS now
            // serialized below (v4) via its CaptureState seam.
            //
            // EXCLUSION PROOF — _possessingAgentId (Phase C C1): cross-tick state, but it is NOT
            // serialized directly because C4 folds it into MatchContext.PossessingAgentId (authored
            // each Resolve from this exact field, equal at snapshot time), and the MatchContext IS
            // serialized below — so the value is captured, just under a different field. The per-agent
            // Pass/Shot executor in-flight state (C0 CaptureState) is now serialized in the loop below
            // (C5) — at Stage 0 the executors are idle in production, but once the Phase D AI dispatcher
            // initiates passes/shots their WINDUP/CONTACT state is cross-tick and digest-relevant.
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

                // C5 — per-agent Pass/Shot executor in-flight state via the C0 capture seam (value
                // types, zero heap alloc). Idle executors capture a constant default block at Stage 0;
                // a Phase-D dispatched pass/shot capture is the cross-tick WINDUP/CONTACT state.
                PassExecutorState passState = _passExecutors[i].CaptureState();
                WritePassExecutorState(buf, ref o, in passState);
                ShotExecutorState shotState = _shotExecutors[i].CaptureState();
                WriteShotExecutorState(buf, ref o, in shotState);

                // D4 — per-agent DecisionTree state machine via the D0 capture seam. A PASS/SHOOT
                // decision holds EXECUTING across the 60 Hz ticks between heartbeats, so this is
                // cross-tick simulation state; at Stage 0 a resting DT captures the IDLE default block.
                DecisionTreeState dtState = _decisionTrees[i].CaptureState();
                WriteDecisionTreeState(buf, ref o, in dtState);
            }

            // C5 — authoritative MatchContext (folds in the possessing-agent id). Authored each Resolve;
            // read by the next AI tick. Written after the per-agent block so the field order is pinned.
            WriteMatchContext(buf, ref o, in _matchContext);

            // D4 — per-team Positioning AI (#12) hysteresis via the CaptureState seam. Cross-tick state
            // (phase dwell + per-agent line/lane membership) that drives formation shape across AI ticks.
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                WritePositioningHysteresis(buf, ref o, _positioning[t].CaptureState());
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

        /// <summary>Serializes a <see cref="PassExecutorState"/> (C0 capture) in canonical order — the
        /// state-machine ordinal, the held <see cref="PassRequest"/>, the INITIATING-frozen in-flight
        /// fields, and the committed <see cref="PassResult"/>. Mirrors the C0 round-trip field order in
        /// PassExecutorStateTests (the lock that this body must stay in sync with). The internal
        /// PhysicalProfile is excluded — it is recomputed on restore (§2.6).</summary>
        private static void WritePassExecutorState(byte[] buf, ref int o, in PassExecutorState s)
        {
            CanonicalSerializer.WriteI32 (buf, ref o, s.State);

            CanonicalSerializer.WriteI32 (buf, ref o, s.Request.AgentId);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)s.Request.PassType);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)s.Request.CrossSubType);
            CanonicalSerializer.WriteI32 (buf, ref o, s.Request.TargetAgentId);
            CanonicalSerializer.WriteF32 (buf, ref o, s.Request.TargetPosition.x);
            CanonicalSerializer.WriteF32 (buf, ref o, s.Request.TargetPosition.y);
            CanonicalSerializer.WriteF32 (buf, ref o, s.Request.TargetPosition.z);
            CanonicalSerializer.WriteF32 (buf, ref o, s.Request.IntendedDistance);
            CanonicalSerializer.WriteF32 (buf, ref o, s.Request.Urgency);
            CanonicalSerializer.WriteBool(buf, ref o, s.Request.IsWeakFoot);
            CanonicalSerializer.WriteI32 (buf, ref o, s.Request.TeamId);
            CanonicalSerializer.WriteI32 (buf, ref o, s.Request.FrameNumber);

            CanonicalSerializer.WriteI32 (buf, ref o, (int)s.EffectiveSubType);
            CanonicalSerializer.WriteF32 (buf, ref o, s.KickSpeed);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LaunchAngleDeg);
            CanonicalSerializer.WriteF32 (buf, ref o, s.SpinVector.x);
            CanonicalSerializer.WriteF32 (buf, ref o, s.SpinVector.y);
            CanonicalSerializer.WriteF32 (buf, ref o, s.SpinVector.z);
            CanonicalSerializer.WriteF32 (buf, ref o, s.BaseKickDirection.x);
            CanonicalSerializer.WriteF32 (buf, ref o, s.BaseKickDirection.y);
            CanonicalSerializer.WriteF32 (buf, ref o, s.BaseKickDirection.z);
            CanonicalSerializer.WriteF32 (buf, ref o, s.AimPoint.x);
            CanonicalSerializer.WriteF32 (buf, ref o, s.AimPoint.y);
            CanonicalSerializer.WriteF32 (buf, ref o, s.AimPoint.z);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LeadDistance);
            CanonicalSerializer.WriteF32 (buf, ref o, s.CachedPassing);
            CanonicalSerializer.WriteF32 (buf, ref o, s.CachedFatigue);
            CanonicalSerializer.WriteF32 (buf, ref o, s.CachedBodyAngleDeg);
            CanonicalSerializer.WriteBool(buf, ref o, s.CachedIsWeakFoot);
            CanonicalSerializer.WriteI32 (buf, ref o, s.CachedWeakFootRating);
            CanonicalSerializer.WriteI32 (buf, ref o, s.WindupFramesRemaining);
            CanonicalSerializer.WriteI32 (buf, ref o, s.FollowThroughFramesRemaining);

            CanonicalSerializer.WriteI32 (buf, ref o, (int)s.LastResult.Outcome);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.FinalVelocity.x);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.FinalVelocity.y);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.FinalVelocity.z);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.FinalSpin.x);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.FinalSpin.y);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.FinalSpin.z);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.AimPoint.x);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.AimPoint.y);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.AimPoint.z);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.ErrorAngleDeg);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.LeadDistance);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)s.LastResult.PassType);
            CanonicalSerializer.WriteI32 (buf, ref o, s.LastResult.ContactFrame);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.ContactMatchTime);
        }

        /// <summary>Serializes a <see cref="ShotExecutorState"/> (C0 capture) in canonical order, mirroring
        /// the C0 round-trip field order in ShotExecutorStateTests. Shot carries its full in-flight field
        /// set (no recompute-on-restore exclusion, unlike Pass).</summary>
        private static void WriteShotExecutorState(byte[] buf, ref int o, in ShotExecutorState s)
        {
            CanonicalSerializer.WriteI32 (buf, ref o, s.State);

            CanonicalSerializer.WriteI32 (buf, ref o, s.Request.AgentId);
            CanonicalSerializer.WriteF32 (buf, ref o, s.Request.PowerIntent);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)s.Request.ContactZone);
            CanonicalSerializer.WriteF32 (buf, ref o, s.Request.SpinIntent);
            CanonicalSerializer.WriteF32 (buf, ref o, s.Request.PlacementTarget.x);
            CanonicalSerializer.WriteF32 (buf, ref o, s.Request.PlacementTarget.y);
            CanonicalSerializer.WriteBool(buf, ref o, s.Request.IsWeakFoot);
            CanonicalSerializer.WriteF32 (buf, ref o, s.Request.DistanceToGoal);
            CanonicalSerializer.WriteI32 (buf, ref o, s.Request.TeamId);
            CanonicalSerializer.WriteI32 (buf, ref o, s.Request.FrameNumber);

            CanonicalSerializer.WriteF32 (buf, ref o, s.KickSpeed);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LaunchAngleDeg);
            CanonicalSerializer.WriteF32 (buf, ref o, s.SpinVector.x);
            CanonicalSerializer.WriteF32 (buf, ref o, s.SpinVector.y);
            CanonicalSerializer.WriteF32 (buf, ref o, s.SpinVector.z);
            CanonicalSerializer.WriteF32 (buf, ref o, s.IntendedAimDirection.x);
            CanonicalSerializer.WriteF32 (buf, ref o, s.IntendedAimDirection.y);
            CanonicalSerializer.WriteF32 (buf, ref o, s.IntendedAimDirection.z);

            CanonicalSerializer.WriteF32 (buf, ref o, s.BodyMechanics.Score);
            CanonicalSerializer.WriteF32 (buf, ref o, s.BodyMechanics.ContactQualityModifier);
            CanonicalSerializer.WriteBool(buf, ref o, s.BodyMechanics.StumbleTriggered);

            CanonicalSerializer.WriteF32 (buf, ref o, s.WeakFootErrorMultiplier);
            CanonicalSerializer.WriteI32 (buf, ref o, s.WindupFrames);
            CanonicalSerializer.WriteF32 (buf, ref o, s.CachedAgentPosition.x);
            CanonicalSerializer.WriteF32 (buf, ref o, s.CachedAgentPosition.y);
            CanonicalSerializer.WriteF32 (buf, ref o, s.CachedAgentPosition.z);
            CanonicalSerializer.WriteF32 (buf, ref o, s.CachedFinishing);
            CanonicalSerializer.WriteF32 (buf, ref o, s.CachedLongShots);
            CanonicalSerializer.WriteF32 (buf, ref o, s.CachedComposure);
            CanonicalSerializer.WriteF32 (buf, ref o, s.CachedFatigue);
            CanonicalSerializer.WriteI32 (buf, ref o, s.WindupFramesRemaining);
            CanonicalSerializer.WriteI32 (buf, ref o, s.FollowThroughFramesRemaining);

            CanonicalSerializer.WriteI32 (buf, ref o, (int)s.LastResult.Outcome);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.FinalVelocity.x);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.FinalVelocity.y);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.FinalVelocity.z);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.FinalSpin.x);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.FinalSpin.y);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.FinalSpin.z);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.IntendedDirection.x);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.IntendedDirection.y);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.IntendedDirection.z);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.FinalDirection.x);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.FinalDirection.y);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.FinalDirection.z);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.ErrorOffset.x);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.ErrorOffset.y);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.BodyMechanicsScore);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.PowerPenaltyApplied);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.KickSpeed);
            CanonicalSerializer.WriteF32 (buf, ref o, s.LastResult.LaunchAngleDeg);
            CanonicalSerializer.WriteBool(buf, ref o, s.LastResult.StumbleTriggered);
            CanonicalSerializer.WriteI32 (buf, ref o, s.LastResult.ContactFrame);
        }

        /// <summary>Serializes a <see cref="DecisionTreeState"/> (D0 capture) in canonical order — the
        /// state-machine ordinal, the dispatched-action flag, and the last <see cref="AgentAction"/>
        /// (incl. its embedded Pass/Shot request blocks). Mirrors the D0 round-trip field order in
        /// DecisionTreeStateTests (the lock this body must stay in sync with). The DecisionTree's
        /// _matchSeed and per-tick _optionBuffer are excluded — boot-deterministic / scratch (§2.6).</summary>
        private static void WriteDecisionTreeState(byte[] buf, ref int o, in DecisionTreeState s)
        {
            CanonicalSerializer.WriteI32 (buf, ref o, s.State);
            CanonicalSerializer.WriteBool(buf, ref o, s.HasDispatchedAction);

            AgentAction a = s.LastAction;
            CanonicalSerializer.WriteI32 (buf, ref o, a.AgentId);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)a.Type);
            CanonicalSerializer.WriteI32 (buf, ref o, a.TargetAgentId);
            CanonicalSerializer.WriteF32 (buf, ref o, a.TargetPosition.x);
            CanonicalSerializer.WriteF32 (buf, ref o, a.TargetPosition.y);

            CanonicalSerializer.WriteI32 (buf, ref o, a.PassParams.AgentId);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)a.PassParams.PassType);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)a.PassParams.CrossSubType);
            CanonicalSerializer.WriteI32 (buf, ref o, a.PassParams.TargetAgentId);
            CanonicalSerializer.WriteF32 (buf, ref o, a.PassParams.TargetPosition.x);
            CanonicalSerializer.WriteF32 (buf, ref o, a.PassParams.TargetPosition.y);
            CanonicalSerializer.WriteF32 (buf, ref o, a.PassParams.TargetPosition.z);
            CanonicalSerializer.WriteF32 (buf, ref o, a.PassParams.IntendedDistance);
            CanonicalSerializer.WriteF32 (buf, ref o, a.PassParams.Urgency);
            CanonicalSerializer.WriteBool(buf, ref o, a.PassParams.IsWeakFoot);
            CanonicalSerializer.WriteI32 (buf, ref o, a.PassParams.TeamId);
            CanonicalSerializer.WriteI32 (buf, ref o, a.PassParams.FrameNumber);

            CanonicalSerializer.WriteI32 (buf, ref o, a.ShotParams.AgentId);
            CanonicalSerializer.WriteF32 (buf, ref o, a.ShotParams.PowerIntent);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)a.ShotParams.ContactZone);
            CanonicalSerializer.WriteF32 (buf, ref o, a.ShotParams.SpinIntent);
            CanonicalSerializer.WriteF32 (buf, ref o, a.ShotParams.PlacementTarget.x);
            CanonicalSerializer.WriteF32 (buf, ref o, a.ShotParams.PlacementTarget.y);
            CanonicalSerializer.WriteBool(buf, ref o, a.ShotParams.IsWeakFoot);
            CanonicalSerializer.WriteF32 (buf, ref o, a.ShotParams.DistanceToGoal);
            CanonicalSerializer.WriteI32 (buf, ref o, a.ShotParams.TeamId);
            CanonicalSerializer.WriteI32 (buf, ref o, a.ShotParams.FrameNumber);

            CanonicalSerializer.WriteF32 (buf, ref o, a.UtilityScore);
            CanonicalSerializer.WriteI32 (buf, ref o, a.HeartbeatTick);
        }

        /// <summary>Serializes the authoritative <see cref="MatchContext"/> in canonical order (C5).
        /// Enum fields (Possession / Phase / BallZone) are written as i32 ordinals.</summary>
        private static void WriteMatchContext(byte[] buf, ref int o, in MatchContext m)
        {
            CanonicalSerializer.WriteI32 (buf, ref o, m.HomeScore);
            CanonicalSerializer.WriteI32 (buf, ref o, m.AwayScore);
            CanonicalSerializer.WriteF32 (buf, ref o, m.MatchTimeSeconds);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)m.Possession);
            CanonicalSerializer.WriteI32 (buf, ref o, m.PossessingAgentId);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)m.Phase);
            CanonicalSerializer.WriteF32 (buf, ref o, m.BallPosition.x);
            CanonicalSerializer.WriteF32 (buf, ref o, m.BallPosition.y);
            CanonicalSerializer.WriteF32 (buf, ref o, m.BallVelocity.x);
            CanonicalSerializer.WriteF32 (buf, ref o, m.BallVelocity.y);
            CanonicalSerializer.WriteF32 (buf, ref o, m.BallVelocity.z);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)m.BallZone);
        }

        /// <summary>Serializes one team's Positioning AI (#12) <see cref="HysteresisState"/> (D4) in
        /// canonical order — the team phase + dwell, then each agent's line/lane membership + dwell.
        /// Enum fields are written as i32 ordinals; the per-agent count is fixed by the seeded squad size
        /// (<c>state.Agents.Length</c>), equal across teams and stable for the match.</summary>
        private static void WritePositioningHysteresis(byte[] buf, ref int o, HysteresisState state)
        {
            CanonicalSerializer.WriteI32(buf, ref o, (int)state.CurrentPhase);
            CanonicalSerializer.WriteI32(buf, ref o, (int)state.CandidatePhase);
            CanonicalSerializer.WriteI32(buf, ref o, state.PhaseDwellCount);

            AgentHysteresisState[] agents = state.Agents;
            for (int i = 0; i < agents.Length; i++)
            {
                CanonicalSerializer.WriteI32(buf, ref o, (int)agents[i].CurrentLine);
                CanonicalSerializer.WriteI32(buf, ref o, (int)agents[i].CandidateLine);
                CanonicalSerializer.WriteI32(buf, ref o, agents[i].LineDwellCount);
                CanonicalSerializer.WriteI32(buf, ref o, (int)agents[i].CurrentLane);
                CanonicalSerializer.WriteI32(buf, ref o, (int)agents[i].CandidateLane);
                CanonicalSerializer.WriteI32(buf, ref o, agents[i].LaneDwellCount);
            }
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

        /// <summary>
        /// Movement-controller adapter (Phase D D1): the DecisionTree dispatch boundary
        /// (<see cref="IDtMovementController"/>, XC-3.5-10). Writes each DT-selected movement command
        /// into the host's held <c>_commands</c> buffer, which the Physics phase consumes the same tick.
        /// One instance backs all 22 DecisionTrees (it routes by agentId). Goalkeeper commands are written
        /// but the Physics phase skips goalkeepers at Stage 0, so they have no locomotion effect.
        /// </summary>
        private sealed class HostMovementController : IDtMovementController
        {
            private readonly MatchEngine _engine;

            public HostMovementController(MatchEngine engine)
            {
                _engine = engine;
            }

            public void SubmitCommand(int agentId, MovementCommand command)
            {
                _engine._commands[agentId] = command;
            }
        }

        /// <summary>
        /// First-touch world adapter (Phase D D3): implements both First Touch (#4) write boundaries over
        /// the host world state. <see cref="IBallPhysicsSystem.SetBallState"/> writes the displaced ball
        /// position + velocity straight into <c>_ball</c> (the logical BallState enum is left unchanged —
        /// the §4.5.4 BallState-write API gap; at Stage 0 possession is tracked by the host's
        /// <c>_possessingAgentId</c>, not the ball's state machine). <see cref="IAgentMovementSystem.SetDribblingState"/>
        /// is a Stage-0 no-op: Agent Movement #2 AgentState carries no dribbling locomotion modifier yet,
        /// so there is no field to write (the carry/dribble mechanic is a later-stage concern); the host
        /// records the controlled outcome via possession in RunFirstTouch instead. One instance backs both
        /// boundaries (it routes through the injected engine back-reference).
        /// </summary>
        private sealed class FirstTouchWorldAdapter : IBallPhysicsSystem, IAgentMovementSystem
        {
            private readonly MatchEngine _engine;

            public FirstTouchWorldAdapter(MatchEngine engine)
            {
                _engine = engine;
            }

            public void SetBallState(Vector3 newPosition, Vector3 newVelocity)
            {
                _engine._ball.Position = newPosition;
                _engine._ball.Velocity = newVelocity;
            }

            public void SetDribblingState(int agentID, bool isDribbling)
            {
                // Stage-0 no-op — see the class summary (no dribbling modifier on AgentState yet).
            }
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
// | 1.4.1   | 2026-06-19 | —      | C1–C3 AR-1 (doc-only): M-1 — SerializeWorldState gains the §2.6 |
// |         |            |        | exclusion proof for _possessingAgentId (constant NO_POSSESSION  |
// |         |            |        | at Stage 0; C4 serializes it via MatchContext) + the executor   |
// |         |            |        | in-flight-state exclusion note (C5). L-1 — RunResolvePhase notes |
// |         |            |        | the GK collision-active / movement-inactive asymmetry (benign   |
// |         |            |        | at Stage 0, recorded for Phase D). L-2 (DeterministicRNG is a    |
// |         |            |        | struct — no per-frame alloc) and L-3 (ApplySeparation runs       |
// |         |            |        | before the vRel<=0 early return — static-overlap separation     |
// |         |            |        | holds) verified non-issues. No behaviour change.                |
// | 1.5     | 2026-06-22 | —      | Phase C C4/C5/C6 — Resolve-phase completion. C4: new            |
// |         |            |        | MatchContext _matchContext authored each Resolve (after         |
// |         |            |        | possession settles) + at boot via UpdateMatchContext — folds    |
// |         |            |        | _possessingAgentId into PossessingAgentId, derives Possession,  |
// |         |            |        | ball kinematics, home-perspective BallZone (ERR-008-002 guard:  |
// |         |            |        | team-relative zone derived downstream). Boot now boots the      |
// |         |            |        | Pass/Shot EventBusRegistrars (idempotent) so a scripted pass    |
// |         |            |        | can reach CONTACT + publish. C5: SerializeWorldState adds the   |
// |         |            |        | per-agent Pass/Shot executor C0 capture (×22 each) +            |
// |         |            |        | MatchContext; SNAPSHOT_SCHEMA_VERSION 1 → 2; _possessingAgentId |
// |         |            |        | captured via MatchContext (exclusion proof updated). New        |
// |         |            |        | WritePassExecutorState / WriteShotExecutorState /               |
// |         |            |        | WriteMatchContext helpers (mirror the C0 round-trip order);     |
// |         |            |        | TestOnly_MatchContext accessor. asmdef gains the DecisionTree   |
// |         |            |        | reference (MatchContext / PitchGeometry).                       |
// | 1.5.1   | 2026-06-22 | —      | C4/C5 AR (M-1): UpdateMatchContext authors MatchPhase.OPEN_PLAY |
// |         |            |        | (not KICK_OFF) — OptionGenerator returns zero options for any   |
// |         |            |        | non-OPEN_PLAY phase (§3.1), so KICK_OFF would silently no-op    |
// |         |            |        | the entire Phase D AI (all agents HOLD). Stage 0 has no kickoff |
// |         |            |        | ceremony, so the running tick loop is open play. Doc-aligned.   |
// | 1.6     | 2026-06-22 | —      | Phase D D1 — AI-phase wiring (perception → decision → movement).|
// |         |            |        | New AI subsystems: a perception-owned SpatialHashGrid, a        |
// |         |            |        | PerceptionSystem, and 22 per-agent DecisionTree instances       |
// |         |            |        | (sharing one HostMovementController adapter + this agent's      |
// |         |            |        | Pass/Shot executor). RunAiPhase now (on stride ticks) rebuilds  |
// |         |            |        | the perception grid, refreshes _hasPossession, runs             |
// |         |            |        | PerceptionSystem.OnHeartbeat (×22) then DecisionTree.Receive-   |
// |         |            |        | Snapshot (×22); the DT writes movement commands into _commands  |
// |         |            |        | (consumed by Physics this tick) / dispatches PASS/SHOOT into    |
// |         |            |        | the executors (advanced in Resolve). Boot assembles the §2.5    |
// |         |            |        | Stage-0 static AI input snapshots (InitializeAiSnapshots) and   |
// |         |            |        | boots the DecisionTree EventBusRegistrar (DecisionMadeEvent is  |
// |         |            |        | Tier C — excluded from the digest). New PERCEPTION_GRID_POINT_  |
// |         |            |        | INSERT_RADIUS constant; asmdef gains PerceptionSystem. Snapshot |
// |         |            |        | schema UNCHANGED (DT/perception cross-tick state serialization  |
// |         |            |        | is D4). Aliases: PerceptionSubsystem / DecisionTreeAI.          |
// | 1.6.1   | 2026-06-22 | —      | Phase D D1 AR (L-1): TestOnly_DtHasDispatched accessor over the |
// |         |            |        | per-agent DecisionTree.HasDispatchedAction, so the D1 test can  |
// |         |            |        | assert the AI pipeline produced a decision (not a silent abort).|
// | 1.6.2   | 2026-06-22 | —      | Phase D D1 CI fix: pressure scalar sourced from               |
// |         |            |        | PerceptionSystem.GetDiagnostics(i).PressureScalar — it lives on |
// |         |            |        | PerceptionDiagnostics, NOT FilteredView (CS1061 build break the |
// |         |            |        | Linux gate caught; the AR grep had matched the diagnostics      |
// |         |            |        | struct in the shared FilteredView.cs file).                     |
// | 1.7     | 2026-06-22 | —      | Phase D D2 — mechanics-AI wiring (Positioning AI #12). One      |
// |         |            |        | PositioningAITick INSTANCE + reused PositioningPerceptionSnap-  |
// |         |            |        | shot per team, seeded at boot. RunAiPhase now runs RunPositi-   |
// |         |            |        | oningAI before the DT loop: it fills each team's snapshot from  |
// |         |            |        | world state, ticks #12, and folds GetFormationSlot back into    |
// |         |            |        | each agent's TacticalContext (the DT MOVE_TO_POSITION / HOLD    |
// |         |            |        | anchor) so agents settle into formation shape instead of the    |
// |         |            |        | kickoff scaffold line. The away team's world state is mapped    |
// |         |            |        | into the canonical attack-+X frame and the slot mapped back     |
// |         |            |        | (180° pitch rotation via MirrorPitchIfAway) — the ERR-008-002   |
// |         |            |        | home/away guard at the mechanics layer. New helpers RunPositi-  |
// |         |            |        | oningAI / FillPositioningSnapshot / ComputeTeamMeanFatigue /    |
// |         |            |        | MirrorPitchIfAway + TestOnly_FormationSlot accessor. asmdef     |
// |         |            |        | gains PositioningAI. Snapshot schema UNCHANGED (positioning     |
// |         |            |        | hysteresis serialization is the D4 step). Pressing #13 /        |
// |         |            |        | Defensive #14 / Attacking #15 tick wiring remains for D2.       |
// | 1.8     | 2026-06-22 | —      | Phase D D3 — first-touch wiring. New stateless FirstTouchSystem |
// |         |            |        | + one FirstTouchWorldAdapter backing both write boundaries      |
// |         |            |        | (IBallPhysicsSystem → _ball; IAgentMovementSystem → Stage-0     |
// |         |            |        | dribbling no-op). RunResolvePhase calls RunFirstTouch after the |
// |         |            |        | executor Update (C3) and before MatchContext (C4): a loose,     |
// |         |            |        | ground-level, moving ball arriving within FIRST_TOUCH_ACCEPT-   |
// |         |            |        | ANCE_RADIUS_M of an APPROACHING agent (ball-closing dot gate —  |
// |         |            |        | excludes the just-kicked owner) triggers BuildFirstTouchContext |
// |         |            |        | (PressureEvaluator pass over the opposing team via the pre-     |
// |         |            |        | allocated _opponentScratch + OrientationDetector half-turn      |
// |         |            |        | flag; ERR-007 neutral touch attributes) → EvaluateFirstTouch +  |
// |         |            |        | ApplyTouchResult. Outcome maps onto possession: CONTROLLED →    |
// |         |            |        | toucher, INTERCEPTION → interceptor id (AGENT_ID_NONE at Stage  |
// |         |            |        | 0 per ERR-004-002 → loose), LOOSE_BALL / DEFLECTION → loose.    |
// |         |            |        | first-touch InternalsVisibleTo grants the host the internal     |
// |         |            |        | PressureEvaluator / OrientationDetector seams. asmdef gains     |
// |         |            |        | FirstTouch; new FIRST_TOUCH_ACCEPTANCE_RADIUS_M / FIRST_TOUCH_  |
// |         |            |        | MIN_BALL_SPEED_M_S constants. Snapshot schema UNCHANGED         |
// |         |            |        | (FirstTouchSystem stateless; writes only _ball + possession,    |
// |         |            |        | both already serialized). D2b (#13/#14/#15) + D4/D5 pending.    |
// | 1.8.1   | 2026-06-22 | —      | D3 AR (3L). L-1: INTERCEPTION possession maps an unresolved /   |
// |         |            |        | out-of-range InterceptingAgentID to NO_POSSESSION explicitly    |
// |         |            |        | (was trusting the AGENT_ID_NONE == NO_POSSESSION cross-assembly |
// |         |            |        | sentinel coincidence). L-2: the nearest-toucher loop shrinks    |
// |         |            |        | bestSq only on a STRICTLY closer candidate, so an exact-distance |
// |         |            |        | tie keeps the lower roster index (was last-wins); boundary stays |
// |         |            |        | inclusive via the first-candidate clause. L-3: BuildFirstTouch- |
// |         |            |        | Context normalises FacingDirection before the OrientationDetect-|
// |         |            |        | or call + context (the contract is a unit vector; Acos angle    |
// |         |            |        | assumes unit facing). No new alloc; outcomes unchanged for unit |
// |         |            |        | facings (the only Stage-0 case).                                |
// | 1.8.2   | 2026-06-22 | —      | D3 CI fix: fully-qualify TacticalDirector.FirstTouch.Pressure-  |
// |         |            |        | Evaluator in BuildFirstTouchContext — PerceptionSystem also     |
// |         |            |        | exposes a PUBLIC PressureEvaluator (same §3.5 formula), so the  |
// |         |            |        | bare name was ambiguous under both usings (CS0104 — caught by   |
// |         |            |        | the Linux gate; the pass-1 review wrongly assumed perception's  |
// |         |            |        | was internal). Parallel to the fully-qualified EventBusRegistrar|
// |         |            |        | calls. No behaviour change.                                     |
// | 1.9     | 2026-06-26 | —      | Phase D D2b — Pressing #13 / Defensive #14 / Attacking #15      |
// |         |            |        | wiring. RunPositioningAI → RunMechanicsAI: per team it now ticks|
// |         |            |        | the Positioning→Pressing→Defensive→Attacking chain in           |
// |         |            |        | dependency order (Pressing's per-agent PressRole feeds the      |
// |         |            |        | Defensive snapshot) then folds the carriers into each agent's   |
// |         |            |        | TacticalContext: MarkDirective.OffensiveLineDepth →             |
// |         |            |        | DefensiveLineDepth + HasMarkDirective (ERR-014-001); a committed|
// |         |            |        | Attacking run → HasAttackIntent (ERR-015-002). One INSTANCE +   |
// |         |            |        | reused 22-agent snapshot per team; each snapshot carries all 22 |
// |         |            |        | agents in the acting team's canonical attack-+X frame           |
// |         |            |        | (MirrorPitchIfAway positions, MirrorVelocityIfAway velocities/  |
// |         |            |        | facing) discriminated by TeamId — the ERR-008-002 guard. New    |
// |         |            |        | helpers FillPressing/Defensive/AttackingSnapshot, CanonicalAt-  |
// |         |            |        | tackDir, MirrorVelocityIfAway, HasActiveAttackIntent. New       |
// |         |            |        | constants STAGE0_PASS_EVENT_RING_CAPACITY / STAGE0_DEFENSIVE_   |
// |         |            |        | LINE_DEPTH / STAGE0_NEUTRAL_NORMALIZED. asmdef gains PressingAI |
// |         |            |        | / DefensiveAI / AttackingAI. Snapshot schema UNCHANGED (the     |
// |         |            |        | per-team tick hysteresis is cross-tick state deferred to D4).   |
// | 1.9.1   | 2026-06-26 | —      | D2b AR (2L). L-1: HasMarkDirective now gated on possession —    |
// |         |            |        | raised only for the team WITHOUT the ball (the Stage-1          |
// |         |            |        | MarkDirective? = null shape for attackers) instead of           |
// |         |            |        | unconditionally true; inert today (stub unread by the DT) but   |
// |         |            |        | no longer locks a future-wrong contract. L-2: new               |
// |         |            |        | AwayTeamCarriers_MirrorHomeTeam test asserts the three carriers |
// |         |            |        | are slot-symmetric home↔away (the D2b analogue of the D2a       |
// |         |            |        | GK-pitch-mirror lock). No behaviour change to consumed output.  |
// | 1.10    | 2026-06-27 | —      | Phase D D4 — snapshot extension + schema bump. SerializeWorld-  |
// |         |            |        | State now writes the per-agent DecisionTree state machine (D0   |
// |         |            |        | CaptureState, ×22) via new WriteDecisionTreeState (mirrors the  |
// |         |            |        | DecisionTreeStateTests round-trip order); SNAPSHOT_SCHEMA_      |
// |         |            |        | VERSION 2 → 3. Exclusion proofs recorded for _perfs (still      |
// |         |            |        | boot-neutral — PHASE-D flag not yet fired) and the perception   |
// |         |            |        | internal state + per-team Positioning/Pressing/Defensive/      |
// |         |            |        | Attacking hysteresis (no get/restore seam yet — deferred to a   |
// |         |            |        | follow-up extension; same-seed determinism unaffected). New     |
// |         |            |        | TestOnly_SetDecisionTreeState seam + DtState_FeedsSnapshot-     |
// |         |            |        | Digest probe. D5 (design-note reconciliation) pending.         |
// | 1.11    | 2026-06-27 | —      | Phase D D4 (cont.) — per-team Positioning AI (#12) hysteresis   |
// |         |            |        | serialized via its new CaptureState seam (WritePositioning-     |
// |         |            |        | Hysteresis, ×TEAM_COUNT); SNAPSHOT_SCHEMA_VERSION 3 → 4.        |
// |         |            |        | Exclusion proof narrowed: Positioning no longer excluded;       |
// |         |            |        | perception + Pressing/Defensive/Attacking hysteresis still      |
// |         |            |        | excluded (no seam yet). New TestOnly_PositioningState seam +    |
// |         |            |        | PositioningHysteresis_FeedsSnapshotDigest probe; test asmdef    |
// |         |            |        | gains TacticalDirector.PositioningAI. D5 + E–F pending.         |
#endregion
