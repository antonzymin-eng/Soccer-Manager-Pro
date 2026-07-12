// File:     src/match-engine/MatchEngine.cs
// Created:  2026-06-16
// Modified: 2026-06-28 (Pressing #13 wiring AR — AttackingDirection inversion fix)
// Modified: 2026-06-29 (#21 T2 Pressing AI (#13) Phase-D writer — route TeamTactic.LineOfEngagement → PressingSnapshot)
// Modified: 2026-06-29 (#21 T2 Defensive (#14) + Attacking (#15) Phase-D writers — route OffsideTrap / FocusPlay → snapshots)
// Modified: 2026-06-29 (#21 T2 Positioning (#12) Phase-D writer — route TeamTactic.Width / DefensiveWidth → ContextModifierInputs; all three writers now closed)
// Modified: 2026-06-29 (#21 §3.3 team-Tempo routing + ERR-021-002: SNAPSHOT_SCHEMA_VERSION 8 → 9, per-team active+pending TeamTactic serialized)
// Modified: 2026-06-30 (#21 §3.3 per-agent PlayerTactic config surface (SetPlayerTactic) + §3.4 DefensiveLine depth recompute; SNAPSHOT_SCHEMA_VERSION 9 → 10)
// Modified: 2026-07-07 (Cheap-item additions: #14 MarkingOrientation routing (SNAPSHOT_SCHEMA_VERSION 10 → 11) + #12 rest-defense coverage routed into TacticalContext)
// Modified: 2026-07-11 (#23/#24/#25 wiring: Phase-D writers + dismark per-agent pass + build-up regain consumer + rotation serialization; SNAPSHOT_SCHEMA_VERSION 11 → 12)
// Modified: 2026-07-11 (#26 manager-AI wiring: ConfigureManager + stride decision gate + ManagerState serialization; SNAPSHOT_SCHEMA_VERSION 12 → 13)
// Modified: 2026-07-11 (engine substrate: Resolve-phase goal detection + score state + GoalAwardedEvent + centre-spot restart; #26 live goalDiff/clock inputs + half-time trigger; SNAPSHOT_SCHEMA_VERSION 13 → 14)
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
using TacticalDirector.TacticalInstructions;

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
        private readonly ICollisionEventConsumer       _eventConsumer;   // null-object drain; real collision/foul consumers deferred (no Stage-0 card/foul model)
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

        // Phase E — the possession holder as of the END of the PREVIOUS Resolve, used to detect a
        // possession transition once per tick (after this tick's possession settles). On a change the
        // host publishes a Tier A PossessionChangedEvent (digest-load-bearing ledger). Seeded at boot to
        // the kickoff value so the first real transition (not the boot state) is the first event.
        private int _prevPossessingAgentId;

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

        // ── Tactical Instructions (#21 T2 runtime activation) ─────────────────────────
        // Per-team manager tactic (the §3.1/§3.2 input layer). _pending is what SetTeamTactic writes;
        // _active is what the AI phase reads. FR-TI-027: a mid-match change takes effect only at a
        // tactical-stride boundary, so _pending → _active is copied at the top of RunAiPhase (which runs
        // only on stride ticks) — never mid-tick. Both default to TeamTactic.Balanced, which reproduces
        // Stage0Default exactly (Mentality.Balanced ⇒ risk ×1.0, Pressing.Medium → MEDIUM, Passing.Mixed
        // → MIXED; FR-TI-031), so a match left at the default is byte-identical to pre-#21 behaviour.
        // BOTH arrays are serialized into the world-state snapshot at SNAPSHOT_SCHEMA_VERSION v9
        // (ERR-021-002 resolved): the active tactic (read by the AI phase) and the pending tactic (staged
        // by SetTeamTactic, committed at the next stride) are cross-tick state, so a tactic changed
        // MID-match now survives save/restore — a mid-match change is restore-deterministic.
        private readonly TeamTactic[] _activeTeamTactics;   // [TEAM_COUNT]
        private readonly TeamTactic[] _pendingTeamTactics;  // [TEAM_COUNT]

        // Per-agent manager tactic (#21 §3.3 — the per-agent role/duty/individual-instruction layer).
        // Same active/pending stride-commit contract as the team tactic (FR-TI-027): _pending is what
        // SetPlayerTactic writes, _active is what RunMechanicsAI folds into each agent's TacticalContext.
        // Both default to the identity PlayerTactic.Default(PlayerRole.Default) (every §3.3 product factor
        // ×1.0; FR-TI-031), so a match left at the default is byte-identical to pre-#21. Both arrays are
        // serialized into the snapshot (SNAPSHOT_SCHEMA_VERSION v10), so a per-agent tactic changed
        // MID-match is restore-deterministic (the same reasoning as ERR-021-002 for the team tactic).
        private readonly PlayerTactic[] _activePlayerTactics;   // [SQUAD_SIZE]
        private readonly PlayerTactic[] _pendingPlayerTactics;  // [SQUAD_SIZE]

        // ── Manager AI #26 per-team state (FR-TP-012, §2.2.4) ─────────────────────────
        // Zero-init = ManagerMode.Human = inert (KD-4): no decision-gate fire, no adaptation, no
        // engine calls — a default match is byte-identical to pre-#26. ConfigureManager opts a
        // team into AI mode; ManagerAdaptation.ApplyKickoff seeds the kickoff selection; the
        // stride-boundary gate in RunAiPhase fires interval decisions (FR-TP-006/018).
        // Serialized at v13 in Appendix C order, so mid-match manager state (hold countdown,
        // last-decision tick, current preset) is restore-deterministic.
        private readonly ManagerState[] _managerStates;  // [TEAM_COUNT]

        // ── Score state (engine substrate — the #26 §9.3 upstream deliverable) ────────
        // Per-team goal counts, incremented by the Resolve-phase goal check (CheckGoalAndRestart)
        // when the ball fully crosses a goal line between the posts under the crossbar
        // (BallCollision.CheckBoundaries ⇒ RestartType.KickOff; the Stage-0 z < Diameter gate is
        // that predicate's own documented simplification). Read by the #26 manager-AI decision
        // point as goalDiff (own − opponent). Serialized at v14 (cross-tick, digest-load-bearing).
        private readonly int[] _goals;  // [TEAM_COUNT]

        // The last agent roster index that HELD settled possession (never reset to NO_POSSESSION
        // once an agent has held the ball). At goal time the ball is loose (the scoring kick
        // released possession at CONTACT), so _possessingAgentId is −1 — this tracker supplies the
        // GoalAwardedEvent Scorer credit and CheckBoundaries' lastTouchTeamID. Stage-0 credit
        // approximation: the last HOLDER, not the last TOUCH (a deflection en route is not
        // tracked); an own-goal deflection therefore credits the deflecting holder if they ever
        // held the ball — the scoring TEAM is classified by geometry (which goal), never by this
        // field. Serialized at v14. −1 until any agent first holds possession.
        private int _lastHolderAgentId;

        // ── Dismarking #23 per-agent state (FR-DM-014) ────────────────────────────────
        // Persistent per-agent marking dwell, updated in the per-agent perception pass each AI
        // stride (FR-DM-003 — AFTER the mechanics AI, so the positioning stage consumes the
        // previous stride's FilteredView-derived pressure per the §3.2 PASS-1 M-1 contract).
        // Serialized at v12 (#23 Appendix B). The pressure/marker carriers handed to #12 are NOT
        // stored across ticks — they are recomputed each stride from the (stale) FilteredView +
        // this dwell state, so the dwell is the only new cross-tick surface.
        private readonly MarkingDwellState[] _markingDwell;         // [SQUAD_SIZE]
        private readonly Vector2[]           _dismarkOppPosScratch; // [SQUAD_SIZE] perceived-opponent scratch
        private readonly int[]               _dismarkOppIdScratch;  // [SQUAD_SIZE]

        // ── Build-Up Structures #24 per-team state (FR-BU-011) ────────────────────────
        // Committed hysteresis zone + post-regain suppression countdown, advanced once per team
        // per AI stride in RunMechanicsAI (classify → gate-read → decrement, #24 §3.1/§3.3);
        // armed by the possession-changed consumer on a TEAM-LEVEL regain (FM-BU-03, PASS-1 M-1).
        // Serialized at v12 (#24 Appendix B).
        private readonly BuildUpZoneState[] _buildUpStates;  // [TEAM_COUNT]

        // FM-BU-03 "settledTeam": the team of the current settled possessor (−1 = never settled).
        // A loose ball does NOT change it; only an opponent → this-team transition arms the
        // suppression window. Cross-tick state, serialized at v12.
        private int _settledPossessionTeam;

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
        // Last ContextModifierInputs handed to each team's PositioningAITick.Tick this AI tick. Persisted
        // only so a test can read back the #21 Phase-D Width / DefensiveWidth routing (the modifier struct
        // is otherwise a transient per-tick input, not part of the serialized world state).
        private readonly ContextModifierInputs[]         _posModifiers;  // [TEAM_COUNT]

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
            _possessingAgentId     = MatchEngineConstants.NO_POSSESSION;
            _prevPossessingAgentId = MatchEngineConstants.NO_POSSESSION; // Phase E — no transition at boot
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

            // #23 — per-agent marking dwell (zero dwell / NoMarker) + the perceived-opponent
            // extraction scratch the marker search reads (zero alloc on the hot path). Allocated
            // BEFORE the positioning loop below: FillPositioningSnapshot reads them.
            _markingDwell         = new MarkingDwellState[MatchEngineConstants.SQUAD_SIZE];
            _dismarkOppPosScratch = new Vector2[MatchEngineConstants.SQUAD_SIZE];
            _dismarkOppIdScratch  = new int[MatchEngineConstants.SQUAD_SIZE];
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                _markingDwell[i] = MarkingDwellState.Unmarked;
            }

            // #24 — per-team build-up state. The committed zone is boot-seeded from the actual
            // kickoff ball X (team-relative) per §2.2.2; suppression starts closed. The settled-
            // possession tracker starts "never settled" (kickoff ball is loose), so the FIRST
            // possession is not a regain and arms nothing (FM-BU-03: opponent → this team only).
            _buildUpStates = new BuildUpZoneState[MatchEngineConstants.TEAM_COUNT];
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                _buildUpStates[t].CommittedZone =
                    BuildUpZoneClassifier.RawZone(MirrorPitchIfAway(t, _ball.Position).x);
            }
            _settledPossessionTeam = -1;

            // #21 T2: both teams start at the Balanced identity tactic (FR-TI-031) — behaviour-neutral
            // until a caller invokes SetTeamTactic before kickoff. _active is seeded directly (not via the
            // stride swap) so the very first AI stride already reads a valid tactic.
            _activeTeamTactics  = new TeamTactic[MatchEngineConstants.TEAM_COUNT];
            _pendingTeamTactics = new TeamTactic[MatchEngineConstants.TEAM_COUNT];
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                _activeTeamTactics[t]  = TeamTactic.Balanced;
                _pendingTeamTactics[t] = TeamTactic.Balanced;
            }

            // #21 §3.3: every agent starts at the identity per-agent tactic (FR-TI-031) — behaviour-neutral
            // until a caller invokes SetPlayerTactic before kickoff. _active is seeded directly so the very
            // first AI stride already reads a valid per-agent tactic.
            _activePlayerTactics  = new PlayerTactic[MatchEngineConstants.SQUAD_SIZE];
            _pendingPlayerTactics = new PlayerTactic[MatchEngineConstants.SQUAD_SIZE];
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                _activePlayerTactics[i]  = PlayerTactic.Default(PlayerRole.Default);
                _pendingPlayerTactics[i] = PlayerTactic.Default(PlayerRole.Default);
            }

            // #26 KD-4: both teams start in ManagerMode.Human — the CLR zero-init of ManagerState IS
            // the inert identity (no gate fire, no adaptation), so a default match is byte-identical
            // to pre-#26. ConfigureManager opts a team into AI mode.
            _managerStates = new ManagerState[MatchEngineConstants.TEAM_COUNT];

            // Engine score state (v14): 0–0 at kickoff; no agent has held possession yet.
            _goals             = new int[MatchEngineConstants.TEAM_COUNT];
            _lastHolderAgentId = MatchEngineConstants.NO_POSSESSION;

            InitializeAiSnapshots();

            // §4 step 3 (cont.) — mechanics AI (Phase D D2). One Positioning AI (#12) instance + reused
            // perception snapshot per team; seed each from the kickoff formation so a valid slot exists
            // before the first AI read (the per-tick Tick() refreshes them — RunPositioningAI).
            _positioning  = new PositioningAITick[MatchEngineConstants.TEAM_COUNT];
            _posSnapshots = new PositioningPerceptionSnapshot[MatchEngineConstants.TEAM_COUNT];
            _posModifiers = new ContextModifierInputs[MatchEngineConstants.TEAM_COUNT];
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

            // §4 step 2 (Phase E) — reset the process-static EventBus for THIS match before booting the
            // registrars and subscribing consumers. The bus is a spec-mandated static singleton (#17
            // §3.2.1 KD-4/KD-8): without this, a second MatchEngine in the same process (and, critically,
            // the two same-seed runs the determinism tests build back-to-back) would hit
            // ERR_EVT_REGISTRATION_PHASE when it tries to Subscribe after the first match's first
            // DrainTick set BootPhaseComplete, and would leak subscribers toward MaxHandlersPerEventType.
            // ResetForNewMatch clears the subscriber tables + reopens the boot phase but leaves the
            // EventRegistry row schema intact, so the idempotent registrar Initialize() calls below stay
            // correct. (Match-engine design note Risk #4 / #16 ReplayEngine step 6.)
            EventBus.ResetForNewMatch();

            // Boot the EventBus registry for the wired producers (Pass #5 / Shot #6) so a pass/shot
            // reaching CONTACT can publish (C4 — without this, ExecuteContact throws
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

            // Phase E — subscribe the real cross-subsystem consumer: possession-changed → AI. Tier A
            // subscription MUST happen during the boot phase (#17 FR-EVT-020/021 — Subscribe throws
            // ERR_EVT_REGISTRATION_PHASE after the first DrainTick), which is why this is here in Boot and
            // not lazily. The handler is a method group (no per-frame closure). PossessionChangedEvent
            // (ordinal 0x04) is a seeded EventRegistry row, so EnsureInitialized() inside Subscribe has
            // already populated its ordinal cache by now. The returned token is discarded — the bus is
            // reset per match (ResetForNewMatch above), so there is no per-subscription teardown to do.
            EventBus.Subscribe<PossessionChangedEvent>(OnPossessionChanged);

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

        /// <summary>
        /// Sets a team's manager tactic (#21 §3.1/§3.2 — T2 runtime activation). The change is staged
        /// as <em>pending</em> and committed at the next tactical-stride boundary (FR-TI-027), so it never
        /// takes effect mid-tick. <paramref name="teamId"/> is 0 (home) or 1 (away).
        /// The active and pending tactics are serialized into the snapshot (SNAPSHOT_SCHEMA_VERSION v9,
        /// ERR-021-002), so a change made MID-match is restore-deterministic. The default is
        /// <see cref="TeamTactic.Balanced"/> (behaviour-neutral).
        /// </summary>
        public void SetTeamTactic(int teamId, in TeamTactic tactic)
        {
            if (teamId < 0 || teamId >= MatchEngineConstants.TEAM_COUNT)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(teamId), teamId, "teamId must be 0 (home) or 1 (away).");
            }
            _pendingTeamTactics[teamId] = tactic;
        }

        /// <summary>
        /// Sets an agent's per-agent tactic (#21 §3.3 — behavioural role + duty + individual instructions).
        /// Like <see cref="SetTeamTactic"/> the change is staged as <em>pending</em> and committed at the next
        /// tactical-stride boundary (FR-TI-027). <paramref name="agentId"/> is a roster index in
        /// <c>[0, SQUAD_SIZE)</c>. The per-agent tactic is serialized into the snapshot
        /// (SNAPSHOT_SCHEMA_VERSION v10), so a mid-match change is restore-deterministic. The default is the
        /// identity <see cref="PlayerTactic.Default(PlayerRole)"/> (behaviour-neutral; FR-TI-031).
        /// </summary>
        public void SetPlayerTactic(int agentId, in PlayerTactic tactic)
        {
            if (agentId < 0 || agentId >= MatchEngineConstants.SQUAD_SIZE)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(agentId), agentId, "agentId must be a roster index in [0, SQUAD_SIZE).");
            }
            _pendingPlayerTactics[agentId] = tactic;
        }

        /// <summary>
        /// Configures a team's manager AI (#26 FR-TP-007 / KD-4). <see cref="ManagerMode.Human"/>
        /// (the default) resets the team's manager state to the inert identity — no selection, no
        /// adaptation, no engine calls. <see cref="ManagerMode.AI"/> opts the team in: the given
        /// Appendix A.2 archetype backs its <see cref="ManagerProfile"/>, the current preset seeds
        /// to the Balanced catalogue midpoint until the kickoff boot path
        /// (<see cref="ManagerAdaptation.ApplyKickoff"/>) selects one, and
        /// <c>LastDecisionTick = −1</c> marks the kickoff decision as not yet fired. Intended
        /// pre-kickoff; a mid-match call is deterministic (the state is serialized at v13) but the
        /// kickoff selection path only runs pre-kickoff (KD-1).
        /// </summary>
        /// <param name="teamId">0 (home) or 1 (away).</param>
        /// <param name="mode">The manager mode.</param>
        /// <param name="profileOrdinal">Appendix A.2 archetype ordinal (AI mode; ignored for Human).</param>
        public void ConfigureManager(int teamId, ManagerMode mode, byte profileOrdinal = 0)
        {
            if (teamId < 0 || teamId >= MatchEngineConstants.TEAM_COUNT)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(teamId), teamId, "teamId must be 0 (home) or 1 (away).");
            }
            if (mode != ManagerMode.Human && mode != ManagerMode.AI)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(mode), mode, "Undefined ManagerMode ordinal (#26 FR-TP-013).");
            }
            if (mode == ManagerMode.Human)
            {
                _managerStates[teamId] = default;  // the inert zero-init identity (KD-4)
                return;
            }
            if (profileOrdinal >= TacticalPresetsConstants.MANAGER_ARCHETYPE_COUNT)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(profileOrdinal), profileOrdinal,
                    "Archetype ordinal beyond the A.2 catalogue (#26 F2).");
            }
            _managerStates[teamId] = new ManagerState
            {
                Mode = ManagerMode.AI,
                ProfileOrdinal = profileOrdinal,
                CurrentPresetOrdinal = TacticPresetLibrary.BalancedOrdinal,
                HoldIntervalsRemaining = 0,
                LastDecisionTick = -1,
            };
        }

        /// <summary>Copy of a team's #26 manager state (read by <see cref="ManagerAdaptation.ApplyKickoff"/>).</summary>
        internal ManagerState GetManagerState(int teamId)
        {
            return _managerStates[teamId];
        }

        /// <summary>
        /// Seeds a team's kickoff selection from the boot path (#26 FR-TP-004/010): stamps the
        /// selected preset ordinal and <c>LastDecisionTick = 0</c> (the kickoff decision is
        /// consumed, so the tick-0 in-engine gate does not double-fire). Called only by
        /// <see cref="ManagerAdaptation.ApplyKickoff"/>; an out-of-range ordinal fails loud (F2).
        /// </summary>
        internal void SeedManagerKickoff(int teamId, byte presetOrdinal)
        {
            if (presetOrdinal >= TacticPresetLibrary.Count)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(presetOrdinal), presetOrdinal,
                    "Preset ordinal beyond the A.1 catalogue (#26 F2).");
            }
            _managerStates[teamId].CurrentPresetOrdinal = presetOrdinal;
            _managerStates[teamId].LastDecisionTick = 0;
            _managerStates[teamId].HoldIntervalsRemaining = 0;
        }

        /// <summary>Test-only seam (#26 §4.3): a team's manager state (mode, ordinals, hold, last tick).</summary>
        internal ManagerState TestOnly_ManagerState(int teamId)
        {
            return _managerStates[teamId];
        }

        /// <summary>Test-only: a team's goal count (the v14 engine score state).</summary>
        internal int TestOnly_Goals(int teamId) => _goals[teamId];

        /// <summary>Test-only seam: scripts the score directly (the production writer is the
        /// Resolve-phase goal check). Lets the manager-AI live-input tests exercise a non-level
        /// score without simulating the ~minutes of play a real goal needs.</summary>
        internal void TestOnly_SetGoals(int homeGoals, int awayGoals)
        {
            _goals[0] = homeGoals;
            _goals[1] = awayGoals;
        }

        /// <summary>Test-only: the last settled possession holder (v14; −1 = no agent has held yet).</summary>
        internal int TestOnly_LastHolderAgentId => _lastHolderAgentId;

        /// <summary>Test-only seam: runs the manager decision points exactly as RunAiPhase does —
        /// same gate, same live goalDiff/clock inputs — at an arbitrary <paramref name="decisionTick"/>,
        /// so late-match ladder behaviour is testable without running ~270 000 real ticks. A staged
        /// tactic still commits only at the next real stride boundary (FR-TI-027).</summary>
        internal void TestOnly_RunManagerDecisionPoints(int decisionTick)
        {
            RunManagerDecisionPoints(decisionTick);
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

        // ── Public observation surface (presentation layer / match viewer) ─────────────
        // Read-only world-state COPIES for presentation consumers (the match viewer records
        // these between ticks). Value-type copies only — no reference into the live buffers
        // escapes, and nothing here can mutate world state or perturb determinism.

        /// <summary>A copy of the current ball state (corner-origin frame per Ball Physics #1 §1.2).</summary>
        public BallState BallView => _ball;

        /// <summary>A copy of agent <paramref name="index"/>'s movement state (roster index in [0, SQUAD_SIZE)).</summary>
        public AgentState AgentView(int index)
        {
            GuardRosterIndex(index);
            return _agents[index];
        }

        /// <summary>Team id (0 = home, 1 = away) of roster <paramref name="index"/>.</summary>
        public int AgentTeamId(int index)
        {
            GuardRosterIndex(index);
            return _teamIds[index];
        }

        /// <summary>True when roster <paramref name="index"/> is a goalkeeper.</summary>
        public bool AgentIsGoalkeeper(int index)
        {
            GuardRosterIndex(index);
            return _isGoalkeeper[index];
        }

        /// <summary>Public-surface roster-index guard (parallel to <see cref="SetPlayerTactic"/>).</summary>
        private static void GuardRosterIndex(int index)
        {
            if (index < 0 || index >= MatchEngineConstants.SQUAD_SIZE)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(index), index, "index must be a roster index in [0, SQUAD_SIZE).");
            }
        }

        /// <summary>Possessing agent's roster index, or NO_POSSESSION (−1) when the ball is loose.</summary>
        public int PossessingAgentId => _possessingAgentId;

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

        /// <summary>Test-only: the agent's DecisionTree state-machine state — lets the Phase E events test
        /// prove the possession-changed consumer interrupted the new holder (EXECUTING → INTERRUPTED).</summary>
        internal DtState TestOnly_DtState(int agentId) => _decisionTrees[agentId].State;

        /// <summary>Test-only: restores an agent's DecisionTree cross-tick state (D0 seam) so a test can
        /// prove the D4 per-agent DecisionTreeState is in the snapshot digest preimage.</summary>
        internal void TestOnly_SetDecisionTreeState(int agentId, in DecisionTreeState state) =>
            _decisionTrees[agentId].RestoreState(state);

        /// <summary>Test-only: the live per-team Positioning AI (#12) hysteresis (D4 CaptureState seam),
        /// so a test can perturb it and prove the positioning hysteresis is in the snapshot digest preimage.</summary>
        internal HysteresisState TestOnly_PositioningState(int teamId) => _positioning[teamId].CaptureState();

        /// <summary>Test-only: the live per-team Pressing AI (#13) cross-tick state (D4 CaptureState seam),
        /// so a test can perturb it and prove the pressing hysteresis is in the snapshot digest preimage.</summary>
        internal PressingTickState TestOnly_PressingState(int teamId) => _pressing[teamId].CaptureState();

        /// <summary>Test-only: the live per-team Defensive AI (#14) cross-tick state (D4 CaptureState seam).</summary>
        internal DefensiveTickState TestOnly_DefensiveState(int teamId) => _defensive[teamId].CaptureState();

        /// <summary>Test-only: the live per-team Attacking AI (#15) cross-tick state (D4 CaptureState seam).</summary>
        internal AttackingTickState TestOnly_AttackingState(int teamId) => _attacking[teamId].CaptureState();

        /// <summary>Test-only: the live Perception (#7) cross-tick state (D4 CaptureState seam; single shared instance).</summary>
        internal PerceptionTickState TestOnly_PerceptionState() => _perception.CaptureState();

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

        /// <summary>Test-only: the #21 routed tactic carriers (Mentality / Pressing / Passing) folded into
        /// the agent's TacticalContext at the last AI tick — lets the runtime-activation test prove
        /// SetTeamTactic reaches the DecisionTree input and the Balanced default is behaviour-neutral.</summary>
        internal Mentality TestOnly_Mentality(int agentId) => _tacticalContexts[agentId].Mentality;
        internal PressingMode TestOnly_Pressing(int agentId) => _tacticalContexts[agentId].Pressing;
        internal PassingStyle TestOnly_Passing(int agentId) => _tacticalContexts[agentId].Passing;

        /// <summary>Test-only: the #21 per-agent tactic (role / duty / instructions) folded into the agent's
        /// TacticalContext at the last AI tick — lets the per-agent config test prove SetPlayerTactic reaches
        /// the DecisionTree input and the identity default is behaviour-neutral.</summary>
        internal PlayerTactic TestOnly_PlayerTactic(int agentId) => _tacticalContexts[agentId].PlayerTactic;

        /// <summary>Test-only: the #21 line of engagement routed into team <paramref name="teamId"/>'s
        /// Pressing AI (#13) snapshot at the last AI tick — lets the Phase-D writer test prove
        /// SetTeamTactic reaches the press input and the Balanced default (Standard) is behaviour-neutral.</summary>
        internal LineOfEngagement TestOnly_PressLineOfEngagement(int teamId) => _pressSnapshots[teamId].LineOfEngagement;

        /// <summary>Test-only: the #21 OffsideTrap toggle routed into team <paramref name="teamId"/>'s
        /// Defensive AI (#14) snapshot at the last AI tick — lets the Phase-D writer test prove
        /// SetTeamTactic reaches the defensive input and the Balanced default (false) is the identity.</summary>
        internal bool TestOnly_OffsideTrapRequested(int teamId) => _defSnapshots[teamId].OffsideTrapRequested;

        /// <summary>Test-only: the #21 MarkingOrientation dial routed into team <paramref name="teamId"/>'s
        /// Defensive AI (#14) snapshot at the last AI tick — lets the Phase-D writer test prove
        /// SetTeamTactic reaches the defensive input and the Balanced default is the identity.</summary>
        internal TacticalDirector.TacticalInstructions.MarkingOrientation TestOnly_MarkingOrientation(int teamId) =>
            _defSnapshots[teamId].MarkingOrientation;

        /// <summary>Test-only: the cheap-item Positioning AI (#12) rest-defense coverage result routed
        /// into team <paramref name="teamId"/>'s agents' TacticalContext at the last AI tick.</summary>
        internal bool TestOnly_RestDefenseSufficient(int teamId) => _positioning[teamId].GetRestDefenseSufficient();

        /// <summary>Test-only: the #21 FocusPlay routed into team <paramref name="teamId"/>'s Attacking
        /// AI (#15) snapshot at the last AI tick — lets the Phase-D writer test prove SetTeamTactic
        /// reaches the attacking input and the Balanced default (Mixed) is the identity.</summary>
        internal TacticalDirector.TacticalInstructions.FocusPlay TestOnly_FocusPlay(int teamId) => _attackSnapshots[teamId].FocusPlay;

        /// <summary>Test-only: the #21 Width / DefensiveWidth routed into team <paramref name="teamId"/>'s
        /// Positioning AI (#12) ContextModifierInputs at the last AI tick — lets the Phase-D writer test
        /// prove SetTeamTactic reaches the positioning input and the Balanced default (Standard) is the
        /// identity. (The modifier struct is a transient per-tick input captured for the seam.)</summary>
        internal TacticalDirector.TacticalInstructions.TacticWidth TestOnly_PositioningWidth(int teamId) => _posModifiers[teamId].Width;
        internal TacticalDirector.TacticalInstructions.TacticDefWidth TestOnly_PositioningDefWidth(int teamId) => _posModifiers[teamId].DefensiveWidth;

        /// <summary>#23 routing seam: the DismarkIntensity routed into this agent's TacticalContext (FR-DM-015).</summary>
        internal DismarkIntensity TestOnly_DismarkIntensity(int agentId) => _tacticalContexts[agentId].DismarkIntensity;

        /// <summary>#23 routing seam: the DismarkIntensity routed into this team's #12 snapshot (FR-DM-015).</summary>
        internal DismarkIntensity TestOnly_PositioningDismarkIntensity(int teamId) => _posSnapshots[teamId].DismarkIntensity;

        /// <summary>#23 state seam: this agent's marking-dwell state (FR-DM-014).</summary>
        internal MarkingDwellState TestOnly_MarkingDwell(int agentId) => _markingDwell[agentId];

        /// <summary>#24 routing seam: the BuildUpStructure routed into this team's #12 snapshot (FR-BU-012).</summary>
        internal BuildUpStructure TestOnly_BuildUpStructure(int teamId) => _posSnapshots[teamId].BuildUpStructure;

        /// <summary>#24 state seam: this team's committed build-up zone (FM-BU-01).</summary>
        internal BuildUpZone TestOnly_BuildUpCommittedZone(int teamId) => _buildUpStates[teamId].CommittedZone;

        /// <summary>#24 state seam: this team's post-regain suppression countdown (FM-BU-03).</summary>
        internal int TestOnly_BuildUpSuppressTicks(int teamId) => _buildUpStates[teamId].SuppressTicksRemaining;

        /// <summary>#25 routing seam: the RotationFreedom routed into this team's #12 snapshot (FR-RO-014).</summary>
        internal RotationFreedom TestOnly_RotationFreedom(int teamId) => _posSnapshots[teamId].RotationFreedom;

        /// <summary>#25 state seam: the bound slot index for this team's roster index (FR-RO-014).</summary>
        internal int TestOnly_SlotBinding(int teamId, int rosterIndex) =>
            _positioning[teamId].CaptureRotationState().GetSlotOfAgent(rosterIndex);

        /// <summary>#25 state seam: the per-pair rotation state for this team's adjacency-table row.</summary>
        internal RotationPairState TestOnly_RotationPairState(int teamId, int row) =>
            _positioning[teamId].CaptureRotationState().GetPairState(row);

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
        /// <summary>
        /// Evaluates the #26 manager decision gate for both teams and runs any due decision point
        /// with the LIVE engine inputs (§3.4 FM-TP-04): <c>goalDiff</c> = own goals − opponent goals
        /// from the v14 score state, <c>ticksRemaining</c> = <c>MATCH_TICKS_TOTAL − decisionTick</c>
        /// clamped at 0 (the clock does not stop at full time at Stage 0 — a decision point past the
        /// notional final whistle sees t01 = 0, maximum urgency/protect weight), and the engine
        /// match-length constant. Production caller: RunAiPhase's stride branch (F5 — plus the
        /// signature-preserving TestOnly wrapper, which exists so late-match ladder arithmetic is
        /// testable without ~270 000 real ticks).
        /// </summary>
        private void RunManagerDecisionPoints(int decisionTick)
        {
            long ticksRemaining = MatchEngineConstants.MATCH_TICKS_TOTAL - decisionTick;
            if (ticksRemaining < 0)
            {
                ticksRemaining = 0;
            }

            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                if (ManagerDecisionGate.DecisionDue(decisionTick, in _managerStates[t]))
                {
                    // TEAM_COUNT == 2, so the opponent index is 1 − t.
                    int goalDiff = _goals[t] - _goals[1 - t];
                    ManagerAdaptation.RunDecisionPoint(
                        this, t, ref _managerStates[t], decisionTick,
                        goalDiff, ticksRemaining, MatchEngineConstants.MATCH_TICKS_TOTAL);
                }
            }
        }

        private void RunAiPhase()
        {
            _aiPhaseRanThisTick = true;
            _aiPhaseRunCount++;

            // #26 FR-TP-006/018: the manager decision gate — evaluated ONLY here inside the stride
            // branch (off-stride firing impossible by construction, F5) and BEFORE the FR-TI-027
            // pending→active commit below, so a decision fired at tick N stages via SetTeamTactic
            // and commits at this same stride boundary. Human mode (the default) never fires (KD-4).
            // LIVE INPUTS (the §3.4 PASS-1 M-1 gates, closed 2026-07-11 by the engine substrate):
            // goalDiff reads the Resolve-phase goal producer's score state (v14), and the clock pair
            // is MATCH_TICKS_TOTAL / ticksRemaining from the engine match-length model — the ladder
            // and the half-time trigger are fully live.
            RunManagerDecisionPoints((int)_clock.CurrentTick);

            // #21 FR-TI-027: commit any pending tactic change at this tactical-stride boundary.
            // RunAiPhase runs only on stride ticks, so copying pending → active here is exactly the
            // "swap on IsAiStrideTick" contract — a SetTeamTactic call during the intervening 60 Hz
            // physics frames cannot take effect until the next stride. Cheap struct copy (TEAM_COUNT=2),
            // zero allocation; idempotent when unchanged.
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                _activeTeamTactics[t] = _pendingTeamTactics[t];
            }
            // #21 §3.3 FR-TI-027: the per-agent tactic commits at the same stride boundary.
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                _activePlayerTactics[i] = _pendingPlayerTactics[i];
            }

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

                // #23 §3.2 (FR-DM-003): the per-agent marking-dwell update runs HERE, in the
                // per-agent perception pass where FilteredView was just rebuilt, in ascending agent
                // index. The #12 offset stage consumed the PREVIOUS stride's value earlier this
                // stride (FillPositioningSnapshot); the §3.4 passer-side penalty below consumes the
                // same-pass fresh view. Runs regardless of the DismarkIntensity dial — the dwell
                // state machine models attention, the dial gates only its consumers — so a mid-match
                // dial flip starts from warm dwell. Deterministic: pure function of the view + the
                // committed team phase.
                {
                    int oppCount = ExtractPerceivedOpponents(in view);
                    bool markerExists = MarkingPressureEvaluator.TryFindNearestMarker(
                        _agents[i].Position,
                        new ReadOnlySpan<Vector2>(_dismarkOppPosScratch, 0, oppCount),
                        new ReadOnlySpan<int>(_dismarkOppIdScratch, 0, oppCount),
                        out int markerId, out _, out _);
                    _markingDwell[i] = MarkingPressureEvaluator.UpdateDwell(
                        in _markingDwell[i], _positioning[_teamIds[i]].GetPhase(), markerExists, markerId);
                }

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
        /// feed PressRole to Defensive (PressDirective has no Stage-0 TacticalContext carrier). The DT-facing
        /// Pressing / Passing / Mentality carriers come from the #21 active team tactic (default Balanced =
        /// the prior Stage0Default values), overlaid below — see RunAiPhase for the FR-TI-027 stride swap.
        /// </summary>
        private void RunMechanicsAI(int tacticalTick)
        {
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                // #24 §3.1/§3.3 per-team pre-pass, BEFORE the positioning tick (the classifier
                // "runs once per team per heartbeat, before the overlay stage"): classify the
                // committed zone from team-relative ball X (FM-BU-01 hysteresis), then let
                // FillPositioningSnapshot read this heartbeat's gate values (zone + pre-decrement
                // suppression flag); the suppression countdown decrements AFTER the fill so the
                // gate reads the current heartbeat's value (check-then-decrement — the §3.3 worked
                // example: armed 30 at heartbeat 100 ⇒ suppressed through 129, active from 130).
                _buildUpStates[t].CommittedZone = BuildUpZoneClassifier.Classify(
                    _buildUpStates[t].CommittedZone, MirrorPitchIfAway(t, _ball.Position).x);

                // Positioning (#12) — formation slots + the Line/Phase inputs the rest of the chain reads.
                // #21 T2 Phase-D writer (FR-TI-016): route the active team tactic's Width / DefensiveWidth
                // into the modifier inputs (#12 ContextModifier translates them to the lateral-compactness
                // scalar). Default Balanced ⇒ Standard / Standard ⇒ scalar 1.00 ⇒ byte-identical to pre-#21
                // (the 5-arg ctor with both Standard equals the 3-arg identity-seeding ctor). This is the
                // #12 analogue of the #13 FillPressingSnapshot single-writer.
                FillPositioningSnapshot(t, tacticalTick);
                ContextModifierInputs modifiers = new ContextModifierInputs(
                    scoreDiff:         0,
                    teamMeanFatigue:   ComputeTeamMeanFatigue(t),
                    tacticalIntensity: MatchEngineConstants.STAGE0_TACTICAL_INTENSITY,
                    width:             _activeTeamTactics[t].Width,
                    defensiveWidth:    _activeTeamTactics[t].DefensiveWidth);
                _posModifiers[t] = modifiers;
                _positioning[t].Tick(_posSnapshots[t], modifiers);

                // #24 §3.3: per-heartbeat suppression decrement (after the gate consumed this
                // heartbeat's value above).
                _buildUpStates[t] = BuildUpZoneClassifier.TickSuppression(in _buildUpStates[t]);

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
                    // Mechanics-AI carriers + the #21 team tactic (T2 runtime activation).
                    // OffensiveLineDepth is frame-invariant ([0,1] depth), so no inverse map needed.
                    TacticalContext ctx = TacticalContext.Stage0Default(worldSlot);

                    // #21 §3.1/§3.2: route this team's active tactic into the DecisionTree input. Mentality
                    // drives the UtilityScorer risk multiplier; Pressing/Passing translate to the #8 enums
                    // (TacticTranslation, rank-mapped so the opposite enum orderings do not invert). For the
                    // default Balanced tactic these resolve to MEDIUM/MIXED/×1.0 — identical to Stage0Default,
                    // so the overlay is behaviour-neutral until a non-Balanced tactic is set (FR-TI-031).
                    TeamTactic tactic = _activeTeamTactics[t];
                    ctx.Mentality = tactic.Mentality;
                    // #21 §3.3: team tempo drives the per-option forward-vs-retain factor in the
                    // UtilityScorer §3.3 product. Balanced ⇒ Tempo.Standard ⇒ all factors ×1.0
                    // (behaviour-neutral). The per-agent PlayerTactic (role / duty / individual instructions)
                    // is routed from the active per-agent config — the default identity tactic resolves to
                    // ×1.0 on every factor (FR-TI-031), so a default match stays byte-identical.
                    ctx.Tempo        = tactic.Tempo;
                    ctx.PlayerTactic = _activePlayerTactics[i];
                    // #23 FR-DM-015: route the team's DismarkIntensity into the DecisionTree input
                    // (drives the §3.4 marked-pass-target penalty). Default Off ⇒ ×1.0 identity.
                    ctx.DismarkIntensity = tactic.DismarkIntensity;
                    // Fully qualified: TacticTranslation now exists in BOTH DecisionTree (#8) and
                    // PressingAI (#13), and the match-engine references both, so the bare name is
                    // ambiguous (CS0104). These two are the #8 enum maps specifically.
                    ctx.Pressing  = TacticalDirector.DecisionTree.TacticTranslation.ToPressingMode(tactic.Pressing);
                    ctx.Passing   = TacticalDirector.DecisionTree.TacticTranslation.ToPassingStyle(tactic.Passing);

                    // #21 §3.4: DefensiveLineDepth is the #14 MarkDirective output — #12/#14 remain the depth
                    // authority. The §3.4 recompute Clamp01(TeamTactic.DefensiveLine + MentalityLineBias) is
                    // now applied at the #14 INPUT (FillDefensiveSnapshot.DefensiveLineDepth), so the manager
                    // dial + mentality bias flows into #14 and its output reaches #8 here — a single
                    // authoritative depth source (no parallel surface). Balanced ⇒ 0.5 + 0.0 = 0.5, the prior
                    // STAGE0_DEFENSIVE_LINE_DEPTH, so a default match is unchanged (FR-TI-031).
                    ctx.DefensiveLineDepth = mark.OffensiveLineDepth;
                    ctx.HasMarkDirective   = !teamHasPossession;
                    ctx.HasAttackIntent    = HasActiveAttackIntent(_attacking[t].GetIntent(i));
                    // Cheap-item addition (new §3.2/§7.7): Positioning AI #12's rest-defense coverage
                    // check, computed once per team per stride, routed to every agent's context.
                    ctx.RestDefenseSufficient = _positioning[t].GetRestDefenseSufficient();
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

            // #23/#24/#25 Phase-D writers (FR-DM-015 / FR-BU-012 / FR-RO-014): this fill is the sole
            // populator of the #12 snapshot's routing dials. Default Balanced ⇒ Off / None / Off —
            // the exact identities, so a default match's composed slots are unchanged. The #24 zone
            // + suppression carriers were advanced by the RunMechanicsAI pre-pass (boot fill reads
            // the seeded zone + a closed window).
            TeamTactic activeTactic  = _activeTeamTactics[team];
            snap.DismarkIntensity    = activeTactic.DismarkIntensity;
            snap.BuildUpStructure    = activeTactic.BuildUpStructure;
            snap.BuildUpCommittedZone = _buildUpStates[team].CommittedZone;
            snap.BuildUpSuppressed   = _buildUpStates[team].SuppressTicksRemaining > 0;
            snap.RotationFreedom     = activeTactic.RotationFreedom;

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

                // #23 §3.2/§4.4: the per-agent dismark carriers — the nearest qualifying marker +
                // the UNGATED proximity × dwell pressure — computed from this agent's FilteredView.
                // Positioning runs BEFORE the per-agent perception pass in the stride order, so the
                // view content here is the PREVIOUS stride's (the deliberate one-stride staleness of
                // the PASS-1 M-1 contract; empty at boot/heartbeat 0 ⇒ no marker, conservative).
                // The FR-DM-006 phase gate is applied by the SlotComposer stage with this tick's
                // committed phase, hence the InPoss argument here (bypass — pressure ungated).
                // Skipped entirely at Off (§6.3 default-cheap): the carriers stay zero and the
                // composer stage is gated off anyway.
                if (activeTactic.DismarkIntensity == DismarkIntensity.Off || isGk)
                {
                    snap.HasMarker[k]       = false;
                    snap.MarkingPressure[k] = 0f;
                    snap.MarkerPosition[k]  = Vector2.zero;
                }
                else
                {
                    FilteredView view = _perception.GetFilteredView(i);
                    int oppCount = ExtractPerceivedOpponents(in view);
                    bool markerExists = MarkingPressureEvaluator.TryFindNearestMarker(
                        _agents[i].Position,
                        new ReadOnlySpan<Vector2>(_dismarkOppPosScratch, 0, oppCount),
                        new ReadOnlySpan<int>(_dismarkOppIdScratch, 0, oppCount),
                        out _, out Vector2 markerPos, out float markerDist);

                    snap.HasMarker[k]       = markerExists;
                    snap.MarkingPressure[k] = MarkingPressureEvaluator.ComputePressure(
                        TacticalDirector.PositioningAI.Phase.InPoss, markerExists, markerDist,
                        _markingDwell[i].DwellTicks);
                    // Marker position mapped into the same canonical frame as agent positions —
                    // it is the agent's PERCEIVED marker (FR-DM-001/004), never ground truth.
                    snap.MarkerPosition[k]  = markerExists
                        ? MirrorPitchIfAway(team, markerPos)
                        : Vector2.zero;
                }
            }
            snap.ActiveOutfieldCount = activeOutfield;
        }

        /// <summary>
        /// Copies the visible-opponent perceived positions/ids of one agent's <see cref="FilteredView"/>
        /// into the pre-allocated dismark scratch buffers (#23 §4.4 — the sanctioned extraction seam
        /// that keeps <c>MarkingPressureEvaluator</c>'s primitive-span signature auditable: the only
        /// opponent-data source reaching it is the agent's own FilteredView). Returns the entry count.
        /// </summary>
        private int ExtractPerceivedOpponents(in FilteredView view)
        {
            int n = 0;
            for (int j = 0; j < view.VisibleOpponentsCount; j++)
            {
                _dismarkOppPosScratch[n] = view.VisibleOpponents[j].PerceivedPosition;
                _dismarkOppIdScratch[n]  = view.VisibleOpponents[j].AgentId;
                n++;
            }
            return n;
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
            // The snapshot is built in the PRESSING team's canonical attack-+X frame, so the
            // pressing team's own attacking direction is the constant +X. PressingSnapshot's
            // contract (AR-3 H / ERR-013-009/010) is that AttackingDirection is the PRESSING
            // team's; the consumers (TriggerEvaluator.EvaluateBackwardPass, CoverShadowSelector
            // threat progression) NEGATE it to recover the ball-carrier's forward. Feeding the
            // ball-carrier's direction here (−X when the opponent holds the ball — i.e. exactly
            // when pressing is active) would double-invert those two, firing BackwardPass on
            // forward passes and rewarding retreating receivers.
            snap.AttackingDirection  = new Vector2(1f, 0f);
            snap.PossessionTeamId    = owner >= 0 ? _teamIds[owner] : MatchEngineConstants.NO_POSSESSION;
            snap.PressingTeamId      = team;

            // #21 §3.4 / FR-TI-017 (T2 Phase-D writer): route this team's active tactic line of
            // engagement into the Pressing AI (#13) input. PrimaryPressSelector scales its trigger
            // radius by TacticTranslation.PressTriggerRadiusScalar(LineOfEngagement). Default Balanced
            // ⇒ Standard ⇒ ×1.0, byte-identical to pre-#21 (the #13 analogue of the #8 RunMechanicsAI
            // single-writer). The snapshot is per-tick assembled, so this overwrites the ctor seed.
            snap.LineOfEngagement    = _activeTeamTactics[team].LineOfEngagement;

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
                    // Cheap-item addition (new §7.12): cover-shadow curve attributes, sourced from
                    // the same _dtAttrs the Decision Tree already reads (Stage 0: neutral defaults
                    // for every agent; Stage 1+ real rosters will differentiate this).
                    DefensivePositioningAttribute = _dtAttrs[i].Positioning,
                    PhysicalEffortAttribute       = (_dtAttrs[i].WorkRate + _dtAttrs[i].Pace + _dtAttrs[i].Stamina) / 3f,
                    MentalSharpnessAttribute      = (_dtAttrs[i].Decisions + _dtAttrs[i].Anticipation) / 2f,
                };
            }
        }

        /// <summary>
        /// Fills team <paramref name="team"/>'s reused <see cref="DefensiveSnapshot"/> (Phase D D2b). The
        /// per-agent <c>PressRole</c> is read back from this team's Pressing AI (#13) output, completing the
        /// Positioning→Pressing→Defensive chain. All 22 agents are carried in the canonical attack-+X frame;
        /// the team phase is the Positioning AI phase and the line depth is the #21 §3.4 recompute
        /// <c>Clamp01(TeamTactic.DefensiveLine + MentalityLineBias[mentality])</c> (echoed into
        /// <see cref="MarkDirective.OffensiveLineDepth"/>; Balanced ⇒ 0.5). The team's goalkeeper anchors the COVER_GK_ZONE
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
            // #21 §3.4 (resolves PASS-1 M-2): the authoritative defensive-line depth is the manager input
            // dial + the per-mentality additive bias, re-Clamp01'd — TeamTactic.DefensiveLine is INPUT ONLY,
            // never a parallel depth value. This is the single source #12/#14 (here) and #8 (via the #14
            // MarkDirective output) read. Default Balanced ⇒ Clamp01(0.5 + 0.0) = 0.5 = the prior
            // STAGE0_DEFENSIVE_LINE_DEPTH, so a default match is byte-identical (FR-TI-031). The resolved
            // depth is recomputed every tick from the serialized dial + mentality, so it is never an
            // independently-restorable second surface (no divergence-on-restore; §3.4 serialization note).
            TeamTactic depthTactic = _activeTeamTactics[team];
            snap.DefensiveLineDepth      = Mathf.Clamp01(
                depthTactic.DefensiveLine
                + TacticalDirector.DecisionTree.TacticTranslation.MentalityLineBias(depthTactic.Mentality));
            snap.AgentCount              = MatchEngineConstants.SQUAD_SIZE;
            snap.HasActivePrimaryPress   = _pressing[team].LastDirective.IsActive;

            // #21 §3.4 / FR-TI-022 (T2 Phase-D writer): route this team's active tactic OffsideTrap
            // toggle into the Defensive AI (#14) input. Fully qualified because TacticTranslation now
            // exists in five referenced assemblies (#8/#12/#13/#14/#15) — CS0104 at the composition
            // root (the #13 v1.17 lesson). Default Balanced ⇒ false (the routing identity, FR-TI-031);
            // per KD-9 this is a REQUEST, not a guarantee — OffsideTrapController's §3.7.2 autonomous
            // cascade is unchanged at Stage 0 and does not yet read this flag (gating today's arming
            // behind a default-false toggle would not be behaviour-neutral; active consumption lands
            // with the §3.7.2 additive-request design at activation). The snapshot is per-tick
            // assembled, so this overwrites the class-field default each tick.
            snap.OffsideTrapRequested    =
                TacticalDirector.DefensiveAI.TacticTranslation.OffsideTrapRequested(
                    _activeTeamTactics[team].OffsideTrap);

            // Cheap-item addition (2026-07-07): routes the team's MarkingOrientation dial into the
            // #14 MAN_MARK candidate radius (MarkAssigner scales DefensiveAIConstants.ManMarkCandidateRadiusM
            // by TacticTranslation.MarkRadiusScalar(MarkingOrientation)). Balanced ⇒ ×1.0, byte-identical
            // to pre-addition (FR-TI-031).
            snap.MarkingOrientation      = _activeTeamTactics[team].MarkingOrientation;

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

            // #21 §3.3 / FR-TI-021 (T2 Phase-D writer): route this team's active tactic FocusPlay into
            // the Attacking AI (#15) input. The snapshot field is the #21 enum; the translation to a
            // preferred Flank? (TacticTranslation.PreferredFlank) is the consumer's job. Default
            // Balanced ⇒ FocusPlay.Mixed (no lateral preference = the routing identity, FR-TI-031), so
            // a default match is byte-identical to pre-#21. The OverloadDetector flank-preference
            // consumption is deferred to the §5.6 / G2 balance pass; this writer connects the seam. The
            // snapshot is per-tick assembled, so this overwrites the auto-property zero-value each tick.
            snap.FocusPlay           = _activeTeamTactics[team].FocusPlay;

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

            // Engine substrate — goal check. Runs AFTER the executors (the ball's crossing position came
            // from this tick's Physics phase, possibly adjusted by collision) and BEFORE first touch, so a
            // ball that has fully crossed the goal line cannot be "received" by an agent standing in the
            // out-of-bounds buffer. On a goal the ball is restarted at the centre spot, so D3/C4 below see
            // the restarted state. Non-goal exits (throw-in/corner/goal-kick classifications) are ignored —
            // Stage 0 has no restart model for them, preserving pre-substrate behaviour exactly.
            CheckGoalAndRestart();

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

            // Engine substrate — record the last settled HOLDER (v14). Updated after C4 so it tracks the
            // same settled value MatchContext folds in; only ever overwritten by a real holder, so at goal
            // time (ball loose) it still names the agent whose kick scored (the GoalAwardedEvent credit).
            if (_possessingAgentId >= 0)
            {
                _lastHolderAgentId = _possessingAgentId;
            }

            // Phase E — possession now SETTLED for this tick; publish a Tier A PossessionChangedEvent if
            // the holder changed since the previous tick. Diffing the settled value once here (not at each
            // mutation site) collapses an intra-tick kick-release-then-first-touch-regain to its NET change
            // — a transient mid-Resolve flicker that ends on the same holder emits nothing. Publishing in
            // Resolve (phase 4) enqueues the event before the Events phase (5) drains it the same tick.
            PublishPossessionChangeIfChanged();
        }

        /// <summary>
        /// Engine-substrate goal check (Resolve phase; the #26 §9.3 upstream goal-detection
        /// deliverable, first named by #26 §7.2). Classifies the ball's settled position through
        /// <see cref="BallCollision.CheckBoundaries"/> — a <see cref="RestartType.KickOff"/> return
        /// means the ball fully crossed a goal line between the posts under the crossbar (the z-gate
        /// and corner-precedence simplifications are that predicate's own documented Stage-0 scope).
        /// The scoring TEAM is classified by geometry alone (which half-space the ball exited —
        /// home attacks +X toward the away goal at x = PITCH_LENGTH_M, so an exit there scores for
        /// team 0; an exit at x &lt; 0 scores for team 1): an own goal therefore credits the correct
        /// team regardless of who touched last. On a goal: the scoring team's count increments, a
        /// Tier A <see cref="GoalAwardedEvent"/> (ordinal 0x07, registry producer phase = Resolve)
        /// is published into the digest-load-bearing ledger (Scorer = the last settled holder, −1 if
        /// none yet; Assister = −1 — no assist tracking at Stage 0), and the ball restarts at the
        /// centre spot, stationary at rest height — the minimal Stage-0 restart (agents keep their
        /// positions; no kickoff re-setup, no half-end swap; an executor mid-windup elsewhere
        /// proceeds against the restarted ball and self-cancels via its own possession recheck if it
        /// lost the ball). Non-goal exits return without touching any state — Stage 0 has no
        /// throw-in/corner/goal-kick restart model and the ball remains out of play exactly as
        /// before this check existed. Deterministic and allocation-free.
        /// </summary>
        private void CheckGoalAndRestart()
        {
            int lastTouchTeam = _lastHolderAgentId >= 0 ? _teamIds[_lastHolderAgentId] : 0;
            (bool isOut, RestartType restart) = BallCollision.CheckBoundaries(_ball, lastTouchTeam);
            if (!isOut || restart != RestartType.KickOff)
            {
                return;
            }

            // Which goal: the exit half-space. CheckBoundaries only returns KickOff for x < −r or
            // x > LENGTH + r, so a mid-pitch compare cleanly separates the two.
            int scoringTeam = _ball.Position.x > MatchEngineConstants.PITCH_LENGTH_M * 0.5f ? 0 : 1;
            _goals[scoringTeam]++;

            var evt = new GoalAwardedEvent(
                scorer:       _lastHolderAgentId,
                assister:     -1,
                scoringTeam:  (byte)scoringTeam,
                ballPosition: _ball.Position);
            EventBus.Publish(in evt);

            // Centre-spot restart: same construction as the kickoff boot state. The restarted ball
            // is definitionally loose — in the (kick-scored) common case possession was already
            // released at CONTACT and this is a no-op; in the degenerate possessed-into-the-goal
            // case it prevents a stale holder claiming a ball now 50 m away (the Phase E publisher
            // below emits the holder → loose transition).
            _ball = BallState.CreateAtPosition(new Vector3(
                MatchEngineConstants.KickoffBallXM,
                MatchEngineConstants.KickoffBallYM,
                MatchEngineConstants.BALL_REST_HEIGHT_M));
            _possessingAgentId = MatchEngineConstants.NO_POSSESSION;
        }

        /// <summary>
        /// Phase E producer. Compares the settled possession holder against the previous tick's holder and,
        /// on a change, publishes a Tier A <see cref="PossessionChangedEvent"/> (ordinal 0x04) into the
        /// digest-load-bearing ledger, then records the new holder. Deterministic and allocation-free (the
        /// event is a struct passed by <c>in</c>). The <c>Reason</c> is the Stage-0 UNSPECIFIED sentinel
        /// (no reason taxonomy yet — see <see cref="MatchEngineConstants.POSSESSION_CHANGE_REASON_UNSPECIFIED"/>).
        /// </summary>
        private void PublishPossessionChangeIfChanged()
        {
            if (_possessingAgentId == _prevPossessingAgentId)
                return;

            var evt = new PossessionChangedEvent(
                _prevPossessingAgentId,
                _possessingAgentId,
                MatchEngineConstants.POSSESSION_CHANGE_REASON_UNSPECIFIED);
            EventBus.Publish(in evt);

            _prevPossessingAgentId = _possessingAgentId;
        }

        /// <summary>
        /// Phase E consumer (possession-changed → AI). Subscribed once at boot (#17 boot-phase Subscribe);
        /// invoked from <see cref="EventBus.DrainTick"/> in the Events phase. Forces the NEW holder's
        /// DecisionTree to re-plan on its next AI stride: <see cref="DecisionTreeAI.NotifyInterrupt"/>
        /// clears an in-flight EXECUTING hold (EXECUTING → INTERRUPTED, DispatchedActionType reset), and
        /// INTERRUPTED transitions to EVALUATING on the next valid snapshot (#8 §3.7.2/§3.7.3). It is a safe
        /// no-op when the new holder is not mid-PASS/SHOOT (OnInterrupt only transitions from EXECUTING).
        /// The PREVIOUS holder is not interrupted here — losing the ball mid-pass already self-cancels via
        /// the executor's own possession recheck (Pass #5 FM-08), so a second interrupt would be redundant.
        /// A loose-ball transition (NewHolder = NO_POSSESSION) has no DecisionTree to interrupt. Pure and
        /// allocation-free; the effect (DecisionTree state) is captured in the same tick's snapshot digest.
        /// </summary>
        private void OnPossessionChanged(in PossessionChangedEvent evt)
        {
            int newHolder = evt.NewHolder;
            if (newHolder >= 0 && newHolder < MatchEngineConstants.SQUAD_SIZE)
            {
                _decisionTrees[newHolder].NotifyInterrupt();

                // #24 §3.3 (FM-BU-03, PASS-1 M-1): TEAM-LEVEL regain detection. The raw event fires
                // on teammate receptions too (PreviousHolder/NewHolder are agent ids), so the window
                // arms only when the settled possessing TEAM transitions opponent → this team; an
                // intra-team possessor change never re-arms. A loose-ball transition (NewHolder < 0)
                // does not change settledTeam, and the first-ever settle (settledTeam −1 at kickoff)
                // is not a regain. The regaining team's OWN TransitionWon decides the arming
                // (CounterAttack/CounterPress ⇒ REGAIN_SUPPRESS_TICKS; HoldShape/Regroup ⇒ none) —
                // default Balanced carries HoldShape, so a default match never opens a window.
                int newTeam = _teamIds[newHolder];
                if (newTeam != _settledPossessionTeam)
                {
                    if (_settledPossessionTeam >= 0)
                    {
                        _buildUpStates[newTeam] = BuildUpZoneClassifier.ArmOnTeamRegain(
                            in _buildUpStates[newTeam], _activeTeamTactics[newTeam].TransitionWon);
                    }
                    _settledPossessionTeam = newTeam;
                }
            }
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
            // the snapshot preimage and therefore digest-load-bearing. Phase E publishes a Tier A
            // PossessionChangedEvent (ordinal 0x04) into this ledger on each possession transition, so on
            // a no-transition tick this writes the empty-ledger header (domain tag + zero count) and on a
            // transition tick it writes that header plus the one event record. NOTE: the EventBus ledger
            // is process-static — two same-seed runs stay deterministic because each match resets the bus
            // at boot (EventBus.ResetForNewMatch) and replays the identical possession transitions, so the
            // ledger byte stream (and thus the digest) is reproduced exactly. (Phase A relied on nothing
            // being published; Phase E makes the published ledger load-bearing — locked by the
            // two-same-seed ledger-digest test in MatchEngineEventsTests.)
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
            // CROSS-TICK COVERAGE COMPLETE (D4, v8): every cross-tick gameplay surface is now serialized.
            // The four mechanics-AI hysteresis surfaces — Positioning (#12, v4), Pressing (#13, v5),
            // Defensive (#14, v6), Attacking (#15, v7) — and the Perception (#7, v8) internal state
            // (RecognitionLatencyTracker / ShoulderCheckScheduler / ball-prev arrays) are all serialized
            // below via their CaptureState seams, alongside the per-agent DecisionTreeState (D4) and the
            // C0/B0 executor + OscillationGuard state. v9 adds the per-team #21 manager tactic (active +
            // pending), closing ERR-021-002 — a mid-match tactic change is now restore-deterministic. The
            // ONLY remaining un-serialized fields are the boot-deterministic constants (_attrs/_perfs,
            // proven above) and the tick-derivable observation counters — no cross-tick gameplay state is
            // excluded. The per-agent PlayerTactic is now its own config surface (SetPlayerTactic) and is
            // serialized (active + pending, ×SQUAD_SIZE) at v10 below. The team Tempo carried in
            // TacticalContext (#21 §3.3) still needs no separate field — it is re-assembled each AI tick in
            // RunMechanicsAI from the serialized team tactic.
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

            // D4 — per-team Pressing AI (#13) cross-tick state via the CaptureState seam (role hysteresis,
            // trigger debounce, disengage/cooldown dwell, accumulated press fatigue).
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                WritePressingTickState(buf, ref o, _pressing[t].CaptureState());
            }

            // D4 — per-team Defensive AI (#14) cross-tick state (per-entity mark hysteresis + last
            // assignment + per-team offside-line state).
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                WriteDefensiveTickState(buf, ref o, _defensive[t].CaptureState());
            }

            // D4 — per-team Attacking AI (#15) cross-tick state (per-agent role hysteresis + transition-
            // hold state + frozen in-possession directive).
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                WriteAttackingTickState(buf, ref o, _attacking[t].CaptureState());
            }

            // D4 — Perception (#7) cross-tick state (single shared instance over all 22 agents): the
            // recognition-latency tracker, shoulder-check scheduler, and per-agent ball-perception
            // carry-over. The last AI-internal cross-tick surface; with this the snapshot covers every
            // cross-tick subsystem and there is no remaining excluded gameplay state.
            WritePerceptionTickState(buf, ref o, _perception.CaptureState());

            // v9 (ERR-021-002 resolved) — the per-team manager tactic. Both the active tactic (what the AI
            // phase reads) and the pending tactic (a SetTeamTactic staged but not yet committed at a stride
            // boundary) are cross-tick state: a tactic changed MID-match now survives save/restore, so a
            // mid-match change is restore-deterministic. Default Balanced is still byte-stable across two
            // same-seed runs (both serialize the identical Balanced block every tick).
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                WriteTeamTactic(buf, ref o, in _activeTeamTactics[t]);
                WriteTeamTactic(buf, ref o, in _pendingTeamTactics[t]);
            }

            // v10 (#21 §3.3) — the per-agent PlayerTactic (role + duty + individual instructions). Both the
            // active tactic (read by RunMechanicsAI) and the pending one (a SetPlayerTactic staged but not yet
            // committed at a stride boundary) are cross-tick state, so a per-agent tactic changed MID-match is
            // restore-deterministic — the same reasoning as the v9 team tactic. Default identity is byte-stable
            // across two same-seed runs (both serialize the identical identity block every tick).
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                WritePlayerTactic(buf, ref o, in _activePlayerTactics[i]);
                WritePlayerTactic(buf, ref o, in _pendingPlayerTactics[i]);
            }

            // v12 (a) — #23 per-agent marking-dwell state (FR-DM-014; #23 Appendix B order). The
            // dwell is the ONLY new #23 cross-tick surface: the pressure/marker carriers the #12
            // stage consumes are recomputed each stride from this dwell + the (already-serialized,
            // v8) perception state, so they need no field of their own.
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                CanonicalSerializer.WriteI32(buf, ref o, _markingDwell[i].DwellTicks);
                CanonicalSerializer.WriteI32(buf, ref o, _markingDwell[i].LastMarkerId);
            }

            // v12 (b) — #24 per-team build-up state (FR-BU-011; #24 Appendix B order) + the
            // engine-level FM-BU-03 settled-possession-team tracker the regain arming diffs against.
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                CanonicalSerializer.WriteU8 (buf, ref o, (byte)_buildUpStates[t].CommittedZone);
                CanonicalSerializer.WriteI32(buf, ref o, _buildUpStates[t].SuppressTicksRemaining);
            }
            CanonicalSerializer.WriteI32(buf, ref o, _settledPossessionTeam);

            // v12 (c) — #25 per-team rotation state (FR-RO-013; #25 Appendix B order: the binding
            // permutation, then the LastComposedTarget cache — restore loads it VERBATIM, a re-seed
            // would break byte-identity (PASS-1 H-1) — then the per-pair state in table-row order).
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                RotationController rot = _positioning[t].CaptureRotationState();
                for (int k = 0; k < rot.SquadSize; k++)
                {
                    CanonicalSerializer.WriteI32(buf, ref o, rot.GetSlotOfAgent(k));
                }
                for (int k = 0; k < rot.SquadSize; k++)
                {
                    Vector2 target = rot.GetLastComposedTarget(k);
                    CanonicalSerializer.WriteF32(buf, ref o, target.x);
                    CanonicalSerializer.WriteF32(buf, ref o, target.y);
                }
                for (int r = 0; r < rot.PairCount; r++)
                {
                    RotationPairState pair = rot.GetPairState(r);
                    CanonicalSerializer.WriteI32 (buf, ref o, pair.TriggerDwellTicks);
                    CanonicalSerializer.WriteBool(buf, ref o, pair.Rotated);
                    CanonicalSerializer.WriteI32 (buf, ref o, pair.HoldTicksRemaining);
                }
            }

            // v13 — #26 per-team manager-AI state (FR-TP-012; Appendix C pinned field order:
            // Mode u8, ProfileOrdinal u8, CurrentPresetOrdinal u8, HoldIntervalsRemaining i32,
            // LastDecisionTick i32). Cross-tick state: the hold countdown and last-decision tick
            // drive future decisions, so a save between two decision points resumes byte-identically
            // (T-TP-DET-003). Default Human zero-init is byte-stable across same-seed runs.
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                CanonicalSerializer.WriteU8 (buf, ref o, (byte)_managerStates[t].Mode);
                CanonicalSerializer.WriteU8 (buf, ref o, _managerStates[t].ProfileOrdinal);
                CanonicalSerializer.WriteU8 (buf, ref o, _managerStates[t].CurrentPresetOrdinal);
                CanonicalSerializer.WriteI32(buf, ref o, _managerStates[t].HoldIntervalsRemaining);
                CanonicalSerializer.WriteI32(buf, ref o, _managerStates[t].LastDecisionTick);
            }

            // v14 — engine score state (goal detection substrate). Cross-tick and digest-load-
            // bearing: the score drives the #26 manager-AI goalDiff input and the goal-side
            // classification, and the last-holder tracker feeds the GoalAwardedEvent scorer credit.
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                CanonicalSerializer.WriteI32(buf, ref o, _goals[t]);
            }
            CanonicalSerializer.WriteI32(buf, ref o, _lastHolderAgentId);

            payload.BytesWritten = o;
        }

        /// <summary>Serializes a <see cref="PlayerTactic"/> in canonical (Appendix B) field order: the
        /// behavioural <c>Role</c> and <c>Duty</c> as i32 ordinals, then the embedded
        /// <see cref="PlayerInstructions"/> (six <see cref="InstrBias"/> ordinals as i32, the TightMarking
        /// bool, the man-mark target id as i32, and the set-piece-duty flags as i32). Ordinal stability is
        /// each enum's own APPEND-only contract.</summary>
        private static void WritePlayerTactic(byte[] buf, ref int o, in PlayerTactic t)
        {
            CanonicalSerializer.WriteI32(buf, ref o, (int)t.Role);
            CanonicalSerializer.WriteI32(buf, ref o, (int)t.Duty);

            PlayerInstructions ins = t.Instructions;
            CanonicalSerializer.WriteI32 (buf, ref o, (int)ins.RiskyPasses);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)ins.ShootTendency);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)ins.DribbleTendency);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)ins.CrossTendency);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)ins.PositioningFreedom);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)ins.CloseDown);
            CanonicalSerializer.WriteBool(buf, ref o, ins.TightMarking);
            CanonicalSerializer.WriteI32 (buf, ref o, ins.MarkTargetEntityId);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)ins.SetPieceRoles);
        }

        /// <summary>Serializes a <see cref="TeamTactic"/> in canonical (Appendix B) field order. Enum
        /// fields are written as i32 ordinals (ordinal stability is each enum's own APPEND-only contract);
        /// the manager-input <c>DefensiveLine</c> dial as f32 and <c>TimeWasting</c> as u8.</summary>
        private static void WriteTeamTactic(byte[] buf, ref int o, in TeamTactic t)
        {
            CanonicalSerializer.WriteI32(buf, ref o, (int)t.Mentality);
            CanonicalSerializer.WriteI32(buf, ref o, (int)t.Formation);
            CanonicalSerializer.WriteI32(buf, ref o, (int)t.Tempo);
            CanonicalSerializer.WriteI32(buf, ref o, (int)t.Width);
            CanonicalSerializer.WriteI32(buf, ref o, (int)t.Passing);
            CanonicalSerializer.WriteI32(buf, ref o, (int)t.Pressing);
            CanonicalSerializer.WriteI32(buf, ref o, (int)t.LineOfEngagement);
            CanonicalSerializer.WriteF32(buf, ref o, t.DefensiveLine);
            CanonicalSerializer.WriteI32(buf, ref o, (int)t.DefensiveWidth);
            CanonicalSerializer.WriteI32(buf, ref o, (int)t.TransitionWon);
            CanonicalSerializer.WriteI32(buf, ref o, (int)t.TransitionLost);
            CanonicalSerializer.WriteBool(buf, ref o, t.OffsideTrap);
            CanonicalSerializer.WriteI32(buf, ref o, (int)t.TriggerPressMask);
            CanonicalSerializer.WriteI32(buf, ref o, (int)t.FocusPlay);
            CanonicalSerializer.WriteI32(buf, ref o, (int)t.GkDistribution);
            CanonicalSerializer.WriteU8 (buf, ref o, t.TimeWasting);
            CanonicalSerializer.WriteI32(buf, ref o, (int)t.MarkingOrientation);
            // v12: the three #21 back-prop dials in the pinned Appendix B approval order
            // (#23 → #24 → #25), appended after MarkingOrientation so no prior offset moves.
            CanonicalSerializer.WriteI32(buf, ref o, (int)t.DismarkIntensity);
            CanonicalSerializer.WriteI32(buf, ref o, (int)t.BuildUpStructure);
            CanonicalSerializer.WriteI32(buf, ref o, (int)t.RotationFreedom);
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

        /// <summary>Serializes one team's Pressing AI (#13) <see cref="PressingTickState"/> (D4) in canonical
        /// order — the eight trigger debounce counters, the disengage + cooldown dwell, then each agent's
        /// role-hysteresis (last/pending role + dwell) and accumulated press fatigue. Enum fields are written
        /// as i32 ordinals; the per-agent count is fixed by the EntityId-space capacity
        /// (<c>state.Roles.Capacity</c> == <c>state.PressFatigue.Length</c>), stable for the match.</summary>
        private static void WritePressingTickState(byte[] buf, ref int o, in PressingTickState s)
        {
            CanonicalSerializer.WriteI32(buf, ref o, s.Trigger.BadTouchDwell);
            CanonicalSerializer.WriteI32(buf, ref o, s.Trigger.BadTouchRelease);
            CanonicalSerializer.WriteI32(buf, ref o, s.Trigger.BackwardPassDwell);
            CanonicalSerializer.WriteI32(buf, ref o, s.Trigger.BackwardPassRelease);
            CanonicalSerializer.WriteI32(buf, ref o, s.Trigger.SidelineTrapDwell);
            CanonicalSerializer.WriteI32(buf, ref o, s.Trigger.SidelineTrapRelease);
            CanonicalSerializer.WriteI32(buf, ref o, s.Trigger.WeakReceiverDwell);
            CanonicalSerializer.WriteI32(buf, ref o, s.Trigger.WeakReceiverRelease);

            CanonicalSerializer.WriteI32(buf, ref o, s.DisengageDwell);
            CanonicalSerializer.WriteI32(buf, ref o, s.CooldownTicks);

            RoleHysteresisState roles = s.Roles;
            float[] fatigue = s.PressFatigue;
            for (int i = 0; i < roles.Capacity; i++)
            {
                CanonicalSerializer.WriteI32(buf, ref o, (int)roles.LastRole[i]);
                CanonicalSerializer.WriteI32(buf, ref o, (int)roles.PendingRole[i]);
                CanonicalSerializer.WriteI32(buf, ref o, roles.RoleDwell[i]);
                CanonicalSerializer.WriteF32(buf, ref o, fatigue[i]);
            }
        }

        /// <summary>Serializes one team's Defensive AI (#14) <see cref="DefensiveTickState"/> (D4) in
        /// canonical order — the per-team offside-line state, then per agent the mark-hysteresis block and
        /// the last committed mark assignment. Enum fields are written as i32 ordinals; the per-agent count
        /// is the EntityId-space capacity (<c>state.Hysteresis.Length</c> == <c>state.PrevAssignments.Length</c>).</summary>
        private static void WriteDefensiveTickState(byte[] buf, ref int o, in DefensiveTickState s)
        {
            CanonicalSerializer.WriteF32(buf, ref o, s.Offside.CurrentLineDepth);
            CanonicalSerializer.WriteI32(buf, ref o, s.Offside.StepUpDwellCounter);
            CanonicalSerializer.WriteI32(buf, ref o, s.Offside.CooldownTicksRemaining);
            CanonicalSerializer.WriteI32(buf, ref o, s.Offside.CoverGkZoneActiveTicks);

            MarkHysteresisState[] hyst = s.Hysteresis;
            MarkAssignment[] prev = s.PrevAssignments;
            for (int i = 0; i < hyst.Length; i++)
            {
                CanonicalSerializer.WriteI32(buf, ref o, hyst[i].DwellCounter);
                CanonicalSerializer.WriteI32(buf, ref o, (int)hyst[i].CandidateMode);
                CanonicalSerializer.WriteI32(buf, ref o, hyst[i].CandidateTargetEntityId);
                CanonicalSerializer.WriteI32(buf, ref o, hyst[i].HoldTicks);

                CanonicalSerializer.WriteI32 (buf, ref o, prev[i].AgentEntityId);
                CanonicalSerializer.WriteI32 (buf, ref o, (int)prev[i].Mode);
                CanonicalSerializer.WriteI32 (buf, ref o, prev[i].TargetEntityId);
                CanonicalSerializer.WriteF32 (buf, ref o, prev[i].TargetPosition.x);
                CanonicalSerializer.WriteF32 (buf, ref o, prev[i].TargetPosition.y);
                CanonicalSerializer.WriteI32 (buf, ref o, prev[i].ValidThroughTick);
                CanonicalSerializer.WriteBool(buf, ref o, prev[i].OverriddenThisTick);
                CanonicalSerializer.WriteBool(buf, ref o, prev[i].IsManuallyAssigned);
            }
        }

        /// <summary>Serializes one team's Attacking AI (#15) <see cref="AttackingTickState"/> (D4) in
        /// canonical order — the per-team transition-hold state, the frozen in-possession directive, then
        /// per agent the role-hysteresis block. Enum fields are written as i32 ordinals; the per-agent count
        /// is the EntityId-space capacity (<c>state.Hysteresis.Length</c>).</summary>
        private static void WriteAttackingTickState(byte[] buf, ref int o, in AttackingTickState s)
        {
            CanonicalSerializer.WriteI32(buf, ref o, s.Transition.TransitionHoldTick);
            CanonicalSerializer.WriteI32(buf, ref o, (int)s.Transition.PrevPhase);

            CanonicalSerializer.WriteI32 (buf, ref o, s.LastInPossDirective.TeamId);
            CanonicalSerializer.WriteBool(buf, ref o, s.LastInPossDirective.OverloadActive);
            CanonicalSerializer.WriteI32 (buf, ref o, (int)s.LastInPossDirective.OverloadFlank);
            CanonicalSerializer.WriteI32 (buf, ref o, s.LastInPossDirective.TransitionHoldTick);

            AttackHysteresisState[] hyst = s.Hysteresis;
            for (int i = 0; i < hyst.Length; i++)
            {
                CanonicalSerializer.WriteI32(buf, ref o, (int)hyst[i].CurrentRole);
                CanonicalSerializer.WriteI32(buf, ref o, hyst[i].DwellCounter);
                CanonicalSerializer.WriteI32(buf, ref o, (int)hyst[i].CandidateRole);
                CanonicalSerializer.WriteI32(buf, ref o, hyst[i].CandidateDwell);
            }
        }

        /// <summary>Serializes the Perception (#7) <see cref="PerceptionTickState"/> (D4) in canonical
        /// order — the recognition-latency pair arrays, then the shoulder-check per-agent + per-pair arrays,
        /// then the per-agent ball-perception carry-over. The pair-array length (MaxAgents²) and per-agent
        /// length (MaxAgents) are fixed for the match. There is one shared perception instance (not per team).</summary>
        private static void WritePerceptionTickState(byte[] buf, ref int o, in PerceptionTickState s)
        {
            RecognitionLatencyState lat = s.Latency;
            int pairCap = lat.PairCapacity;
            for (int i = 0; i < pairCap; i++)
            {
                CanonicalSerializer.WriteI32 (buf, ref o, lat.LatencyCounters[i]);
                CanonicalSerializer.WriteBool(buf, ref o, lat.Confirmed[i]);
                CanonicalSerializer.WriteI32 (buf, ref o, lat.ExpiryCounters[i]);
            }

            ShoulderCheckState sc = s.ShoulderCheck;
            int agentCap = sc.AgentCapacity;
            for (int i = 0; i < agentCap; i++)
            {
                CanonicalSerializer.WriteI32 (buf, ref o, sc.NextCheckFrame[i]);
                CanonicalSerializer.WriteI32 (buf, ref o, sc.WindowExpiryFrame[i]);
                CanonicalSerializer.WriteBool(buf, ref o, sc.WindowActive[i]);
                CanonicalSerializer.WriteI32 (buf, ref o, sc.AnimData[i].AgentId);
                CanonicalSerializer.WriteI32 (buf, ref o, sc.AnimData[i].FireFrame);
                CanonicalSerializer.WriteF32 (buf, ref o, sc.AnimData[i].CheckDirection);
                CanonicalSerializer.WriteBool(buf, ref o, sc.AnimData[i].AnyEntityConfirmed);
            }

            int scPairCap = sc.PairCapacity;
            for (int i = 0; i < scPairCap; i++)
            {
                CanonicalSerializer.WriteI32 (buf, ref o, sc.BlindSideLatency[i]);
                CanonicalSerializer.WriteBool(buf, ref o, sc.BlindSideConfirmed[i]);
            }

            int agentCount = s.AgentCount;
            for (int i = 0; i < agentCount; i++)
            {
                CanonicalSerializer.WriteBool(buf, ref o, s.BallVisiblePrev[i]);
                CanonicalSerializer.WriteF32 (buf, ref o, s.BallPerceivedPositionPrev[i].x);
                CanonicalSerializer.WriteF32 (buf, ref o, s.BallPerceivedPositionPrev[i].y);
                CanonicalSerializer.WriteI32 (buf, ref o, s.BallStalenessFramesPrev[i]);
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
// | 1.12    | 2026-06-27 | —      | Phase D D4 (cont.) — per-team Pressing AI (#13) cross-tick      |
// |         |            |        | state serialized via its new CaptureState seam (WritePressing-  |
// |         |            |        | TickState, ×TEAM_COUNT: trigger debounce + disengage/cooldown   |
// |         |            |        | dwell + per-agent role hysteresis + press fatigue); SNAPSHOT_   |
// |         |            |        | SCHEMA_VERSION 4 → 5. Pressing dropped from the exclusion list; |
// |         |            |        | perception + Defensive/Attacking still excluded (no seam yet).  |
// |         |            |        | New TestOnly_PressingState seam + PressingState_FeedsSnapshot-  |
// |         |            |        | Digest probe; test asmdef gains TacticalDirector.PressingAI.    |
// | 1.13    | 2026-06-27 | —      | Phase D D4 (cont.) — per-team Defensive AI (#14) + Attacking AI |
// |         |            |        | (#15) cross-tick state serialized via new CaptureState seams    |
// |         |            |        | (WriteDefensiveTickState: offside + per-agent mark hysteresis + |
// |         |            |        | last assignment; WriteAttackingTickState: transition-hold +     |
// |         |            |        | frozen directive + per-agent role hysteresis; each ×TEAM_COUNT);|
// |         |            |        | SNAPSHOT_SCHEMA_VERSION 5 → 7. Exclusion list down to perception|
// |         |            |        | only. New TestOnly_DefensiveState/_AttackingState seams + two   |
// |         |            |        | digest probes; test asmdef gains DefensiveAI + AttackingAI.     |
// | 1.14    | 2026-06-27 | —      | Phase D D4 (final cross-tick surface) — Perception (#7) state   |
// |         |            |        | serialized via its new CaptureState seam (WritePerceptionTick-  |
// |         |            |        | State: recognition-latency pair arrays + shoulder-check per-    |
// |         |            |        | agent/per-pair arrays + ball-perception carry-over; one shared  |
// |         |            |        | instance); SNAPSHOT_SCHEMA_VERSION 7 → 8. CROSS-TICK COVERAGE   |
// |         |            |        | COMPLETE — no cross-tick gameplay state remains excluded. New   |
// |         |            |        | TestOnly_PerceptionState seam + PerceptionState_FeedsSnapshot-  |
// |         |            |        | Digest probe; test asmdef gains PerceptionSystem.              |
// | 1.15    | 2026-06-27 | —      | Phase E — events-phase consumers. PRODUCER: RunResolvePhase now |
// |         |            |        | calls PublishPossessionChangeIfChanged after UpdateMatchContext |
// |         |            |        | — diffs the settled holder against _prevPossessingAgentId (new  |
// |         |            |        | field) and on a change publishes a Tier A PossessionChangedEvent|
// |         |            |        | (#17 ordinal 0x04) into the digest-load-bearing ledger (net     |
// |         |            |        | change per tick; an intra-tick flicker that ends on the same    |
// |         |            |        | holder emits nothing). CONSUMER: Boot subscribes               |
// |         |            |        | OnPossessionChanged (Tier A Subscribe MUST be in the boot phase,|
// |         |            |        | FR-EVT-020) which NotifyInterrupt()s the new holder's Decision- |
// |         |            |        | Tree so it re-plans next AI stride (EXECUTING→INTERRUPTED→      |
// |         |            |        | EVALUATING; safe no-op otherwise). Boot first calls the new     |
// |         |            |        | EventBus.ResetForNewMatch() so the process-static bus can re-   |
// |         |            |        | Subscribe per match without ERR_EVT_REGISTRATION_PHASE / handler|
// |         |            |        | leakage across the determinism tests' two engines (Risk #4 /   |
// |         |            |        | #16 ReplayEngine step 6). New POSSESSION_CHANGE_REASON_UNSPEC-  |
// |         |            |        | IFIED constant; TestOnly_DtState seam. Snapshot world-state body|
// |         |            |        | UNCHANGED (no schema bump) — the LEDGER digest now carries the  |
// |         |            |        | event. Collision/foul real consumers stay deferred (no Stage-0  |
// |         |            |        | card/foul model). New MatchEngineEventsTests fixture.           |
// | 1.16    | 2026-06-28 | —      | #21 T2 runtime activation — the Phase-D single-writer now routes|
// |         |            |        | a live per-team TeamTactic into the DecisionTree input. New     |
// |         |            |        | _active/_pendingTeamTactics[TEAM_COUNT] (default TeamTactic.    |
// |         |            |        | Balanced); public SetTeamTactic(teamId, tactic) stages pending; |
// |         |            |        | RunAiPhase commits pending→active at the stride boundary (FR-TI-|
// |         |            |        | 027). RunMechanicsAI overlays ctx.Mentality (drives the #8      |
// |         |            |        | UtilityScorer risk mult) + ctx.Pressing/Passing via the now-    |
// |         |            |        | public TacticTranslation (rank-mapped, non-inverting). Balanced |
// |         |            |        | resolves to MEDIUM/MIXED/×1.0 = Stage0Default, so a default     |
// |         |            |        | match is byte-identical to pre-#21 (TacticalContext is a per-   |
// |         |            |        | tick input, NOT serialized → no schema bump). DefensiveLineDepth|
// |         |            |        | stays the #14 output; the §3.4 mentality-line recompute is      |
// |         |            |        | deferred (ERR-021-002). Mid-match changes not yet restore-      |
// |         |            |        | deterministic (tactic not in snapshot — ERR-021-002). New       |
// |         |            |        | TestOnly_Mentality/Pressing/Passing seams; asmdef gains the     |
// |         |            |        | TacticalInstructions ref. New MatchEngineTacticTests fixture.   |
// | 1.17    | 2026-06-28 | —      | Pressing (#13) wiring AR — H. FillPressingSnapshot fed the ball-|
// |         |            |        | carrier's attack direction (CanonicalAttackDir → −X when the    |
// |         |            |        | opponent holds the ball) into PressingSnapshot.AttackingDirec-  |
// |         |            |        | tion, but that field's contract (AR-3 H / ERR-013-009/010) is   |
// |         |            |        | the PRESSING team's own direction, which the consumers NEGATE.  |
// |         |            |        | During active pressing this double-inverted the BackwardPass    |
// |         |            |        | trigger and the CoverShadow threat-progression term. Snapshot   |
// |         |            |        | is in the pressing team's canonical attack-+X frame, so the     |
// |         |            |        | field is the constant +X; dead CanonicalAttackDir helper removed|
// | 1.17    | 2026-06-28 | —      | Build fix (CS0104): the #21 T2 Pressing AI (#13) seam added a   |
// |         |            |        | second public TacticTranslation (in PressingAI), and the match- |
// |         |            |        | engine references both PressingAI and DecisionTree, so the two  |
// |         |            |        | bare TacticTranslation.ToPressingMode/ToPassingStyle calls in   |
// |         |            |        | RunMechanicsAI became ambiguous. Fully qualified them to        |
// |         |            |        | TacticalDirector.DecisionTree.TacticTranslation. No behaviour   |
// |         |            |        | change.                                                         |
// | 1.18    | 2026-06-29 | —      | #21 T2 Pressing AI (#13) Phase-D writer — the #13 analogue of   |
// |         |            |        | the v1.16 #8 single-writer. FillPressingSnapshot now routes the |
// |         |            |        | pressing team's active TeamTactic.LineOfEngagement into         |
// |         |            |        | PressingSnapshot.LineOfEngagement (overwriting the ctor seed),  |
// |         |            |        | which PrimaryPressSelector scales the trigger radius by via     |
// |         |            |        | PressingAI.TacticTranslation.PressTriggerRadiusScalar. Default  |
// |         |            |        | Balanced ⇒ Standard ⇒ ×1.0 = byte-identical to pre-#21. New     |
// |         |            |        | TestOnly_PressLineOfEngagement seam. No schema bump (Pressing-  |
// |         |            |        | Snapshot is a per-tick input). New MatchEngineTacticTests case. |
// | 1.19    | 2026-06-29 | —      | #21 T2 Defensive (#14) + Attacking (#15) Phase-D writers — the  |
// |         |            |        | #14/#15 analogues of the v1.18 #13 writer. FillDefensiveSnapshot|
// |         |            |        | routes the active TeamTactic.OffsideTrap → DefensiveSnapshot.   |
// |         |            |        | OffsideTrapRequested via fully-qualified DefensiveAI.Tactic-    |
// |         |            |        | Translation (CS0104 — five TacticTranslation types now in scope)|
// |         |            |        | FillAttackingSnapshot routes the active TeamTactic.FocusPlay →  |
// |         |            |        | AttackingSnapshot.FocusPlay (enum passthrough; consumer trans-  |
// |         |            |        | lates to Flank?). Default Balanced ⇒ false / Mixed = the routing|
// |         |            |        | identities (FR-TI-022/021), byte-identical to pre-#21. Active   |
// |         |            |        | consumption stays deferred: #14 OffsideTrapController per KD-9  |
// |         |            |        | (gating autonomous arming behind a default-false toggle is not  |
// |         |            |        | neutral); #15 OverloadDetector flank-pref per §5.6/G2 balance   |
// |         |            |        | pass. No schema bump (both are per-tick inputs). New TestOnly_  |
// |         |            |        | OffsideTrapRequested / TestOnly_FocusPlay seams; new test cases.|
// | 1.20    | 2026-06-29 | —      | #21 T2 Positioning (#12) Phase-D writer — the last of the three |
// |         |            |        | Mechanics writers. RunMechanicsAI now builds ContextModifier-   |
// |         |            |        | Inputs via the 5-arg ctor, routing the active TeamTactic.Width /|
// |         |            |        | DefensiveWidth (ContextModifier translates them to the lateral- |
// |         |            |        | compactness scalar). Default Balanced ⇒ Standard / Standard ⇒   |
// |         |            |        | scalar 1.00 = byte-identical to pre-#21 (5-arg both-Standard ≡  |
// |         |            |        | 3-arg identity ctor). Per-team _posModifiers captured for the   |
// |         |            |        | TestOnly_PositioningWidth / _PositioningDefWidth seams. No      |
// |         |            |        | schema bump (the modifier struct is a per-tick input). New test |
// |         |            |        | cases. All three Mechanics Phase-D writers now closed.          |
// | 1.21    | 2026-06-29 | —      | #21 §3.3: RunMechanicsAI routes the active team Tempo into the  |
// |         |            |        | TacticalContext (per-option §3.3 utility product in UtilityScor-|
// |         |            |        | er); per-agent PlayerTactic stays the Stage0Default identity.   |
// |         |            |        | Balanced ⇒ Tempo.Standard ⇒ ×1.0 (behaviour-neutral).          |
// | 1.22    | 2026-06-29 | —      | ERR-021-002 resolved: SNAPSHOT_SCHEMA_VERSION 8 → 9 — the per-  |
// |         |            |        | team active + pending TeamTactic now serialized via WriteTeam-  |
// |         |            |        | Tactic (Appendix B order). A mid-match tactic change is now     |
// |         |            |        | restore-deterministic; SetTeamTactic / _activeTeamTactics docs  |
// |         |            |        | + the cross-tick-coverage proof updated. New TeamTactic_Feeds-  |
// |         |            |        | SnapshotDigest probe.                                           |
// | 1.23    | 2026-06-30 | —      | #21 §3.3 per-agent PlayerTactic config surface + §3.4 Defensive-|
// |         |            |        | Line depth recompute. (1) New _active/_pendingPlayerTactics[    |
// |         |            |        | SQUAD_SIZE] (default identity); public SetPlayerTactic(agentId, |
// |         |            |        | tactic) stages pending, committed at the stride boundary (FR-TI-|
// |         |            |        | 027); RunMechanicsAI routes the active per-agent tactic into    |
// |         |            |        | ctx.PlayerTactic (identity ⇒ ×1.0, byte-identical default). New |
// |         |            |        | PlayerTacticConfig / PlayerTacticConfigApplier in-code source.  |
// |         |            |        | Serialized active+pending ×SQUAD_SIZE via WritePlayerTactic;    |
// |         |            |        | SNAPSHOT_SCHEMA_VERSION 9 → 10 (mid-match per-agent change is   |
// |         |            |        | restore-deterministic). New TestOnly_PlayerTactic seam. (2) §3.4|
// |         |            |        | FillDefensiveSnapshot.DefensiveLineDepth = Clamp01(TeamTactic.  |
// |         |            |        | DefensiveLine + MentalityLineBias[mentality]) — the manager dial|
// |         |            |        | + bias is the single depth source; #14 output still reaches #8. |
// |         |            |        | Balanced ⇒ 0.5 = STAGE0_DEFENSIVE_LINE_DEPTH (behaviour-neutral)|
// | 1.24    | 2026-07-02 | —      | Public observation surface for the presentation layer (match   |
// |         |            |        | viewer): BallView / AgentView(i) / AgentTeamId(i) /             |
// |         |            |        | AgentIsGoalkeeper(i) / PossessingAgentId — read-only value-type |
// |         |            |        | COPIES of world state (no live-buffer reference escapes; no     |
// |         |            |        | mutation path; determinism unaffected). Consumed by the new     |
// |         |            |        | src/match-viewer/ MatchReplayRecorder. No behaviour change.     |
// | 1.25    | 2026-07-02 | —      | AR-1 M-2 (match-viewer review): the three indexed observation   |
// |         |            |        | accessors gain the public-surface roster-index guard            |
// |         |            |        | (ArgumentOutOfRangeException, parallel to SetPlayerTactic)      |
// |         |            |        | instead of a bare IndexOutOfRangeException from the array.      |
/// | 1.26    | 2026-07-07 | —      | Cheap-item additions (tactical-theory cross-reference follow-up): |
// |         |            |        | (a) #14 MarkingOrientation appended to WriteTeamTactic + routed |
// |         |            |        | into FillDefensiveSnapshot (SNAPSHOT_SCHEMA_VERSION 10 → 11);   |
// |         |            |        | (b) Positioning AI #12 rest-defense coverage (GetRestDefense-   |
// |         |            |        | Sufficient) routed into every agent's TacticalContext each      |
// |         |            |        | stride. New TestOnly_MarkingOrientation / _RestDefenseSufficient|
// |         |            |        | seams. Balanced/default ⇒ identity, byte-identical to           |
// |         |            |        | pre-addition.                                                   |
// | 1.27    | 2026-07-07 | —      | Reverted after user review: the half-spaces AgentLane routing   |
// |         |            |        | (ctx.AgentLane = _positioning[t].GetLane(i)) and the             |
// |         |            |        | TestOnly_AgentLane seam are REMOVED — half-spaces are an        |
// |         |            |        | exploitable spatial gap requiring tactical/player instructions, |
// |         |            |        | not a flat passing bonus. No SNAPSHOT_SCHEMA_VERSION change     |
// |         |            |        | (AgentLane was never serialized).                               |
// | 1.28    | 2026-07-11 | —      | Specs #23/#24/#25 wiring (SNAPSHOT_SCHEMA_VERSION 11 → 12):     |
// |         |            |        | (a) #23 — FillPositioningSnapshot routes DismarkIntensity + the |
// |         |            |        | per-agent pressure/marker carriers (previous stride's Filtered- |
// |         |            |        | View + dwell, the §3.2 M-1 one-stride contract); the per-agent  |
// |         |            |        | perception pass updates _markingDwell (FR-DM-003);              |
// |         |            |        | ctx.DismarkIntensity routed for the #8 §3.4 penalty; dwell      |
// |         |            |        | serialized (Appendix B). (b) #24 — per-team zone classify +     |
// |         |            |        | check-then-decrement suppression in RunMechanicsAI; team-level  |
// |         |            |        | regain arming in OnPossessionChanged (settledTeam diff, FM-BU-  |
// |         |            |        | 03); zone state + settledTeam serialized. (c) #25 —             |
// |         |            |        | RotationFreedom routed; binding/cache/pair state serialized via |
// |         |            |        | CaptureRotationState. WriteTeamTactic appends the three dials   |
// |         |            |        | in pinned #21 Appendix B order. New TestOnly seams:             |
// |         |            |        | _DismarkIntensity/_PositioningDismarkIntensity/_MarkingDwell/   |
// |         |            |        | _BuildUpStructure/_BuildUpCommittedZone/_BuildUpSuppressTicks/  |
// |         |            |        | _RotationFreedom/_SlotBinding/_RotationPairState. Default       |
// |         |            |        | Balanced ⇒ Off/None/Off = identities (behaviour-neutral).       |
// | 1.29    | 2026-07-11 | —      | #26 manager-AI wiring (SNAPSHOT_SCHEMA_VERSION 12 → 13): new    |
// |         |            |        | per-team _managerStates (zero-init Human = inert, KD-4); public |
// |         |            |        | ConfigureManager(teamId, mode, profileOrdinal) (F2-gated);      |
// |         |            |        | internal GetManagerState / SeedManagerKickoff (the ApplyKickoff |
// |         |            |        | boot seam — LastDecisionTick = 0 consumes the kickoff decision) |
// |         |            |        | + TestOnly_ManagerState (§4.3). RunAiPhase evaluates the        |
// |         |            |        | ManagerDecisionGate per team BEFORE the FR-TI-027 pending→      |
// |         |            |        | active commit (FR-TP-018; off-stride firing impossible, F5) and |
// |         |            |        | on fire runs ManagerAdaptation.RunDecisionPoint with goalDiff=0 |
// |         |            |        | (engine-TRUE — no goal producer exists; the ladder terms are   |
// |         |            |        | identically 0, so the clock placeholders cannot influence       |
// |         |            |        | behaviour until goal detection + MATCH_TICKS_TOTAL land, §3.4   |
// |         |            |        | PASS-1 M-1). v13 serializes ManagerState per team in Appendix C |
// |         |            |        | order. Default Human/Human is byte-identical to pre-#26.        |
// | 1.30    | 2026-07-11 | —      | Engine substrate (the #26 §9.3 upstream deliverables): NEW      |
// |         |            |        | Resolve-phase CheckGoalAndRestart between the executor advance   |
// |         |            |        | and first touch — BallCollision.CheckBoundaries ⇒ KickOff means |
// |         |            |        | a goal (side classified by exit half-space geometry, so own      |
// |         |            |        | goals credit the right TEAM); increments _goals[scoringTeam],   |
// |         |            |        | publishes the first-ever Tier A GoalAwardedEvent (0x07, scorer = |
// |         |            |        | last settled holder, assister −1), restarts the ball at the      |
// |         |            |        | centre spot (minimal Stage-0 restart — agents keep positions, no |
// |         |            |        | end-swap; non-goal exits untouched, no throw-in/corner model).   |
// |         |            |        | NEW _lastHolderAgentId tracker (updated post-C4). v14 serializes |
// |         |            |        | _goals ×TEAM_COUNT + the tracker. #26 activation: the manager    |
// |         |            |        | block extracted to RunManagerDecisionPoints, now passing LIVE    |
// |         |            |        | goalDiff (v14 score) + ticksRemaining/MATCH_TICKS_TOTAL (the     |
// |         |            |        | match-length model) — closes the §3.4 PASS-1 M-1 gates; the      |
// |         |            |        | half-time trigger activates in ManagerDecisionGate v1.1. New     |
// |         |            |        | seams: TestOnly_Goals/SetGoals/LastHolderAgentId +               |
// |         |            |        | TestOnly_RunManagerDecisionPoints (late-match ladder testable    |
// |         |            |        | without ~270k ticks).                                            |
#endregion
