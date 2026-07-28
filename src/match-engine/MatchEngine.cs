// File:     src/match-engine/MatchEngine.cs
// Created:  2026-06-16
// Modified: 2026-07-27  (shot-outcome pass: live shot pressure query — ComputeOpponentPressureScalar)
// Modified: 2026-07-27  (B3: the #37 KD-7 read-only per-tick ledger tap)
// Modified: 2026-06-29 (#21 T2 Pressing AI (#13) Phase-D writer — route TeamTactic.LineOfEngagement → PressingSnapshot)
// Modified: 2026-06-29 (#21 T2 Defensive (#14) + Attacking (#15) Phase-D writers — route OffsideTrap / FocusPlay → snapshots)
// Modified: 2026-06-29 (#21 T2 Positioning (#12) Phase-D writer — route TeamTactic.Width / DefensiveWidth → ContextModifierInputs; all three writers now closed)
// Modified: 2026-06-29 (#21 §3.3 team-Tempo routing + ERR-021-002: SNAPSHOT_SCHEMA_VERSION 8 → 9, per-team active+pending TeamTactic serialized)
// Modified: 2026-06-30 (#21 §3.3 per-agent PlayerTactic config surface (SetPlayerTactic) + §3.4 DefensiveLine depth recompute; SNAPSHOT_SCHEMA_VERSION 9 → 10)
// Modified: 2026-07-07 (Cheap-item additions: #14 MarkingOrientation routing (SNAPSHOT_SCHEMA_VERSION 10 → 11) + #12 rest-defense coverage routed into TacticalContext)
// Modified: 2026-07-11 (#23/#24/#25 wiring: Phase-D writers + dismark per-agent pass + build-up regain consumer + rotation serialization; SNAPSHOT_SCHEMA_VERSION 11 → 12)
// Modified: 2026-07-11 (#26 manager-AI wiring: ConfigureManager + stride decision gate + ManagerState serialization; SNAPSHOT_SCHEMA_VERSION 12 → 13)
// Modified: 2026-07-11 (engine substrate: Resolve-phase goal detection + score state + GoalAwardedEvent + centre-spot restart; #26 live goalDiff/clock inputs + half-time trigger; SNAPSHOT_SCHEMA_VERSION 13 → 14)
// Modified: 2026-07-14 (match-flow completion: throw-in/corner/goal-kick restarts, fouls/cards, offside, substitutions, half-time ends-swap, full-time freeze; SNAPSHOT_SCHEMA_VERSION 14 → 15 — see docs/tracking/match-flow-completion-design.md)
// Modified: 2026-07-15 (interactive match view: observation-surface extension — HomeScore/AwayScore/MatchEnded; no schema change; see docs/tracking/interactive-match-view-design.md)
// Modified: 2026-07-17 (#27 T1/T2: attribute seeding sourced from canonical player records via PlayerAttributeProjection + ConfigureSquads; default path byte-identical, no schema change — see docs/tracking/player-attribute-projection-design.md)
// Modified: 2026-07-18 (#27 T3: per-team roster reference (_rosterClubId) serialized at SNAPSHOT_SCHEMA_VERSION 15 → 16; a configured squad is digest-distinguishable from unconfigured by design — see docs/tracking/squad-roster-reference-design.md)
// Modified: 2026-07-19 (#27 lineup selection Plan-3: ConfigureSquads assigns starters/bench via LineupSelector (position + rating), GK flags from the selection (KD-L4); no schema change — see docs/tracking/lineup-selection-design.md)
// Modified: 2026-07-17 (#27 T1 AR-4, doc-only — three stale "Stage-0 neutral placeholder" comments aligned to the T1 canonical-projection sourcing)
// Modified: 2026-07-16 (match-flow AR-7 fix pass: substitution yellow-card reset (M-1) + post-full-time SubstitutePlayer refusal (L-2) + last-holder-vs-last-toucher approximation documented at the restart seam (L-1))
// Modified: 2026-07-16 (AR-8 M-1, later same day: sent-off agents excluded from first-touch reception — the one participation surface missing the exclusion; a red-carded agent could receive the ball and deadlock possession)
// Modified: 2026-07-17 (AR-9 M-1: foul candidates involving a sent-off participant discarded at ApplyFoulIfCaptured — a frozen red-carded agent could repeatedly win free kicks and draw cards against opponents running into them)
// Modified: 2026-07-17 (AR-10, doc-only: _lastHolderAgentId writer comment aligned to the last-settled-holder approximation — a deflection-chain goal credits the last settled holder, not necessarily the kicker; CONVERGENCE round)
// Modified: 2026-07-20 (snapshot-deserialize Phase 1 KD-8 writer half: match-flow.card-severity RngStreamState cursor serialized at SNAPSHOT_SCHEMA_VERSION 16 → 17 — the engine's only mutable RNG stream; a save after a booking now round-trips deterministically. See docs/tracking/snapshot-deserialize-design.md)
// Modified: 2026-07-20 (snapshot-deserialize Phase 1 READER: DeserializeWorldState + Read* helpers (symmetric mirror, restore-seam reconstruction, version-gate + ledger-boundary trailing guard) + static RestoreFromSnapshot factory (fingerprint gate + boot + digest-chain/clock restore + KD-3 distinct-squad fail-loud). No schema change. See docs/tracking/snapshot-deserialize-design.md §5 Phase 1.)
// Modified: 2026-07-20 (snapshot-deserialize Phase 2: distinct-squad re-projection (#27 T3 / KD-3) — new ISquadProvider seam threaded into RestoreFromSnapshot; ReprojectDistinctSquads re-derives each configured team's per-slot attribute records (base lineup via LineupSelector + substitutions replayed from the serialized _activeBenchSlot), fail-loud on absent/unresolvable/mismatched roster. No schema change. See docs/tracking/snapshot-deserialize-design.md §5 Phase 2.)
// Modified: 2026-07-21 (snapshot-deserialize Phase 3 on-disk fold: public MatchSeed property (the boot seed a save persists) + the durable-capture seams promoted TestOnly_ → production internal (CaptureDurableHeader/Payload) for MatchSaveManager. No schema change. See docs/tracking/match-save-file-design.md)
// Modified: 2026-07-21 (§4.8.2 runtime MXCSR float-mode gate wired into boot + RestoreFromSnapshot step 0 via MxcsrValidator; native shim in deterministic-sim/native/mxcsr_query.c. No-op where the shim is absent (Linux CI); enforces on the pinned cert host. No schema change.)
// Modified: 2026-07-22 (GK #11 / Heading #10 engine integration, Phase 1 — opt-in: construct + drive both orchestrators + 4 stateless adapters + 2 RNG streams; EnableGkHeading() gates the 10 Hz/60 Hz drive + §4 save/header triggers seeded from PlayerAttributeProjection.ToGoalkeeper/ToHeading; durable-capture seams fail loud when on. Default engine byte-identical (no schema change). See docs/tracking/gk-heading-engine-integration-design.md)
// Modified: 2026-07-22 (GK/Heading cleaner-architecture pass — behaviour-identical: the four nested ball/RNG adapters collapsed into ONE GkHeadingWorldAdapter (both ball systems share ApplyKick; the two RNG services disambiguate by arity); the §4 trigger geometry extracted to the pure, unit-testable GkHeadingIntentSource (TryCommitSaveIntents/HeaderIntents keep only latch + projection + commit). No schema change.)
// Modified: 2026-07-23 (GK/Heading engine-integration Phase 2 — SNAPSHOT_SCHEMA_VERSION 17 → 18: serialize the
//           GK (#11) / Heading (#10) cross-tick state (opt-in flag + 2 subsystem RNG cursors + 2 §4 trigger
//           latches + both orchestrators' in-flight arrays via CaptureState/RestoreState seams), making a
//           flag-on engine snapshot-safe. The Phase-1 durable-capture fail-loud guard is removed.)
// Modified: 2026-07-23 (DT-emitted goalkeeper SAVE (ERR-008-013) + AR follow-up TestOnly_SaveCommittedForGk latch seam)
// Modified: 2026-07-27 (P1 richer observation frame — interactive-unity-client-design.md §5-P1: the
//           per-agent discipline/substitution accessors, the derived CurrentPeriod, and the within-tick
//           restart cue; ApplyRestart gains a RestartCue (KD-P1-4). No schema change.)
// Modified (prior): 2026-07-26 (§5.Z Phase H possession bootstrap — ERR-030-014: ApplyRestart(position, awardedTeam) + SelectRestartTaker (KD-H1), the boot kickoff award, RunLooseBallPickup (KD-H3), SelectLooseBallCollector (KD-H5), the Resolve PASS/SHOOT completion sweep (KD-H4 / ERR-008-015), and interrupt deferral while an executor is in flight. No schema change. See docs/tracking/match-engine-design.md §5.Z)
// Modified: 2026-07-26 (§5.Z.12: boot placement collapsed to ONE own-half template mirrored for the away side — the HomeLineXM/AwayLineXM and HOME_/AWAY_FACING_DEG pairs are gone, along with FacingFromHeading. Away lateral spread mirrors, so digests move; behaviour is transient (the AI reslots outfielders at tick 6).)
// Modified: 2026-07-26 (§5.Z.10 kickoff keeper placement: a keeper spawns on the goal line it DEFENDS, centred on the mouth, instead of on the outfield kickoff line — Stage-0 Physics skips GK locomotion, so boot placement stood for the whole match and both goals were unguarded. See docs/tracking/match-engine-design.md §5.Z.10)
// Modified: 2026-07-26 (§5.Z.9 foul/discipline balance pass: referee-call probability partitioned out of the single card-severity draw (KD-F1/KD-F2), no-call arms no cooldown (KD-F3), strongest-wins candidate capture (KD-F4), + the TestOnly collision-observer measurement seam. No schema change. See docs/tracking/foul-discipline-balance-design.md)
// Modified: 2026-07-27 (§5.Z.17 goalkeeper save pipeline: NotifyKeeperOfShot opens #11's §3.2 reaction window on the shot CONTACT frame (ERR-011-004, stamped in MILLISECONDS); ClearSaveIntent called on the save-episode disarm so the engine and #11 latches cannot disagree; TestOnly_GoalkeeperState observation seam. No schema change. See docs/tracking/goalkeeper-save-pipeline-design.md)
// Modified: 2026-07-27 (P1 richer observation frame + AR-1 L-3: discipline / period / restart accessors for a HUD, ApplyRestart declares its RestartCue, and the unmapped-RestartType arm warns under a gated diagnostic instead of reporting "no restart" in silence. Within-tick fields only — no SNAPSHOT_SCHEMA_VERSION change. See docs/tracking/interactive-unity-client-design.md §5-P1)
// Modified: 2026-07-28 (shot-speed pass (KD-6): _prevTickBallPosition capture + swept goal-frame call in RunPhysicsPhase, crossing-point CheckBoundaries overload in CheckRestartAndApply, TestOnly_WoodworkStrikes. Within-tick + diagnostic state only — no SNAPSHOT_SCHEMA_VERSION change. See docs/tracking/shot-speed-woodwork-design.md)
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
using TacticalDirector.GoalkeeperMechanics;
using TacticalDirector.HeadingMechanics;
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

        // ── The #37 KD-7 read-only per-tick ledger tap ────────────────────────────────
        // Filled in the Snapshot phase (after SerializeLedger, before the bus resets the tick), and
        // read only through the public accessors below. NOT serialized and never read by the sim:
        // it is an observation buffer in the same class as BallView/AgentView, so an observed match
        // is byte-identical to an unobserved one (#37 FR-AN-017).
        private readonly TickLedgerSnapshot _tickLedger = new TickLedgerSnapshot();

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
        private readonly ICollisionEventConsumer       _eventConsumer;   // MatchFlowCollisionConsumer (design note §3) — captures at most one foul candidate per tick
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

        // The #26 preset catalogue the manager AI resolves ordinals against (WS-1). The default is
        // the in-code catalogue (byte-identical to the pre-refactor static path); a disk-loaded
        // catalogue would be injected here. Read-only after boot; NOT serialized (a boot-constant
        // reference, the _teamIds/_isGoalkeeper class — the ordinal it produces IS serialized via
        // ManagerState.CurrentPresetOrdinal, and restore resolves it against this catalogue).
        private readonly TacticalDirector.TacticalInstructions.ITacticPresetCatalogue _presetCatalogue;

        // ── Score state (engine substrate — the #26 §9.3 upstream deliverable) ────────
        // Per-team goal counts, incremented by the Resolve-phase goal check (CheckRestartAndApply)
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

        // ── Shot-speed / woodwork pass (docs/tracking/shot-speed-woodwork-design.md) ──
        // The ball's position at the top of THIS tick's Physics phase, before integration —
        // WITHIN-TICK state (written every Physics phase, consumed by the swept goal-frame test
        // and the Resolve-phase crossing-point adjudication the same tick, never read across
        // ticks; the RestartAppliedThisTick class). NOT serialized — no schema impact (KD-6).
        private Vector3 _prevTickBallPosition;

        // Cumulative post/crossbar strikes this match — DIAGNOSTIC OBSERVATION ONLY (the
        // AiPhaseRunCount class): not serialized, not digest-load-bearing, not restored. Read by
        // the shot-outcome diagnostic; a restored match restarts the count at zero by design.
        private int _woodworkStrikes;

        // ── Match-flow completion (docs/tracking/match-flow-completion-design.md) ─────
        // Discipline: per-agent yellow-card count + sent-off flag, plus a global foul-detection
        // cooldown (design note §3). Serialized at v15 (cross-tick, digest-load-bearing).
        private readonly byte[] _yellowCards;   // [SQUAD_SIZE]
        private readonly bool[] _isSentOff;     // [SQUAD_SIZE]
        private int _foulCooldownRemaining;

        // Single-candidate foul capture (design note §3) — MatchFlowCollisionConsumer.OnCollisionEvent
        // writes these during CollisionSystem.UpdateCollisions; ApplyFoulIfCaptured reads + resets
        // them immediately after (the sole reset — see the RunResolvePhase comment at the
        // UpdateCollisions call site). Not persisted cross-tick in any meaningful sense (always
        // false entering a tick's collision step), so NOT serialized.
        // §5.Z.15 six-second rule (Laws of the Game, Law 12). Making the keeper a live, mobile agent let
        // it WIN possession — and nothing in the engine could make it give the ball up, because #11's
        // distribution is not engine-driven and the Decision Tree has no keeper-distribution action.
        // Measured: in one of four full matches a keeper held the ball for 33.5% of the second half,
        // stalling the match. Cross-tick, so serialized at v19 alongside the collision contact set.
        private int _gkHoldTicks;                 // ticks the current keeper has held the ball
        private int _gkReleaseCooldownRemaining;  // ticks the just-released keeper may not re-collect
        private int _gkReleasedAgentId;           // which keeper that is (NO_POSSESSION when idle)

        private bool  _foulCandidateFound;
        private int   _foulCandidateOffender;
        private int   _foulCandidateVictim;
        // Contact force (N) of the captured candidate — the input to the referee-call probability
        // (foul-discipline-balance-design.md KD-F1). Same lifecycle as the three fields above, so
        // likewise NOT serialized.
        private float _foulCandidateForceN;

        // Balance-measurement seam (design note §5.Z.9). An optional observer the
        // MatchFlowCollisionConsumer forwards EVERY collision event to, BEFORE any of its gates —
        // the only way to measure the force distribution the FoulImpactForceThresholdN gate sits on,
        // since the consumer is a private nested class and the collision system takes exactly one
        // consumer. Null in production and in every test that does not set it, so the cost is one
        // null check per collision event and the behaviour is unchanged. Not serialized: it is an
        // observation hook, not world state.
        private ICollisionEventConsumer _collisionObserver;

        // RNG stream for card-severity draws (design note §3), registered at Boot on the injected
        // DeterministicRngService — the first host-owned draw site in match-engine (Phase A/C register
        // none; collision self-seeds and pass/shot error is hash-based). entityId -1 = the world-scoped
        // (non-entity) stream, matching the InteractionTextGenerator (#22) convention.
        private readonly int _cardSeverityStreamIndex;

        // GK (#11) / Heading (#10) engine integration (gk-heading-engine-integration-design.md, Phase 1).
        // Both orchestrators are CONSTRUCTED at boot (cheap array allocation; does not touch _ball or any
        // serialized world state, so the default engine stays byte-identical) but are only DRIVEN + their
        // §4 triggers fired when the opt-in _gkHeadingEnabled flag is set (KD-11 — default off). Their two
        // RNG streams are registered at boot in a fixed order (stable indices), the card-severity
        // precedent (KD-1); they are inert until a draw fires (only under the flag).
        // NOTE: the orchestrator class names collide with their own namespace names, so they are
        // fully qualified here and at construction (CS0118) — the same namespace-vs-type hazard the
        // projection design flagged. The interfaces / intent / attribute types are uniquely named.
        private readonly TacticalDirector.HeadingMechanics.HeadingMechanics       _heading;
        private readonly TacticalDirector.GoalkeeperMechanics.GoalkeeperMechanics _goalkeeper;
        private readonly int _headingStreamIndex;
        private readonly int _goalkeeperStreamIndex;
        private readonly int[] _gkAgentIds;      // [MaxGkAgents] — agentId of each keeper (keeper index → agentId)
        private bool _gkHeadingEnabled;          // KD-11 opt-in flag; false = byte-identical default engine
        // §4 trigger latches: at most one save per ball episode per keeper, one header per airborne episode
        // per agent. Cleared when the ball leaves the triggering condition. These are engine-level cross-tick
        // state that GATES whether a save/header re-commits, so they are serialized at v18 (Phase 2) alongside
        // the two orchestrators' in-flight state — without them a restore would re-fire a trigger the
        // uninterrupted run suppressed and diverge (see the v18 block in SerializeWorldState).
        private readonly bool[] _saveCommittedForGk;         // [MaxGkAgents]
        private readonly bool[] _headerCommittedThisEpisode; // [SQUAD_SIZE]
        // TestOnly observation: the attributes the engine last handed to CommitSaveIntent / CommitIntent —
        // the projection-reached-orchestrator proof (§7). Recorded at the engine-side commit site.
        private GoalkeeperAgentAttributes _lastCommittedSaveAttrs;
        private bool _lastSaveAttrsValid;
        private HeadingAgentAttributes _lastCommittedHeaderAttrs;
        private bool _lastHeaderAttrsValid;

        // Substitutions (design note §6): per-agent active bench slot (-1 = original starter) + per-team
        // substitutions-used count are cross-tick, serialized at v15. The bench roster itself
        // (attributes/perf/GK-flag per team per bench slot) is a boot-deterministic Stage-0 in-code
        // config — same B3 exclusion proof as _attrs/_perfs — so it is NOT serialized.
        private readonly int[] _activeBenchSlot;    // [SQUAD_SIZE], -1 = original starter
        private readonly int[] _substitutionsUsed;  // [TEAM_COUNT]

        // #27 T3 (squad-roster-reference-design.md): per-team roster reference — the ClubId of the
        // Squad ConfigureSquads loaded for the team, or NO_ROSTER_CLUB_ID (-1) when no squad was
        // configured. Boot-constant identity (the same lifecycle as _teamIds — set at boot, never
        // mutated mid-match), serialized at v16 so a save records WHICH squad each team loaded; a
        // future restore path re-projects the per-slot attribute records (excluded by the
        // boot-deterministic proof) from the referenced roster, keyed by the serialized
        // _activeBenchSlot for substitution bench-swaps (KD-T3-3 — the re-projection itself is future
        // work; no snapshot-deserialize path exists yet, so building the consumer now would be a
        // phantom). A real ClubId is deliberately digest-distinguishable from the sentinel (KD-T3-2).
        private readonly int[] _rosterClubId;       // [TEAM_COUNT]

        // Pending substitution-event queue (design note §6, AR-5 finding): SubstitutePlayer is a
        // public API a caller may invoke BETWEEN ticks, when EventBus.CurrentPhase is the post-
        // OnTickBoundary 0xFF sentinel — publishing a SubstitutionEvent immediately there would throw
        // (EventBus.cs AR-8 M-2 stale-Publish guard). The state effect (attrs/perf/GK-flag swap) is
        // applied immediately in SubstitutePlayer; only the notification event is queued here and
        // flushed at the top of the next RunResolvePhase, where CurrentPhase == Resolve (the
        // registered producer phase for SubstitutionEvent). Capacity = every team's max subs, so it
        // can never overflow. Not cross-tick in any persisted sense (drained same tick it is filled,
        // whenever that tick next runs) — NOT serialized.
        private readonly int[]  _pendingSubOutgoing;  // [MAX_SUBSTITUTIONS_PER_TEAM * TEAM_COUNT]
        private readonly int[]  _pendingSubIncoming;
        private readonly byte[] _pendingSubTeam;
        private readonly byte[] _pendingSubReason;
        private int _pendingSubCount;
        private readonly PlayerAttributes[][]   _benchAttrs;        // [TEAM_COUNT][SUBSTITUTES_PER_TEAM]
        private readonly PerformanceContext[][] _benchPerfs;        // [TEAM_COUNT][SUBSTITUTES_PER_TEAM]
        private readonly bool[][]               _benchIsGoalkeeper; // [TEAM_COUNT][SUBSTITUTES_PER_TEAM]
        // #27 T1: canonical bench records (the _canonicalAttrs sibling; _benchAttrs is its #2
        // projection). Substitution copies the canonical record onto the outgoing slot and
        // re-projects every per-slot surface — see SubstitutePlayer. Fully qualified per KD-P6.
        private readonly TacticalDirector.PlayerDatabase.PlayerAttributes[][] _benchCanonicalAttrs; // [TEAM_COUNT][SUBSTITUTES_PER_TEAM]

        // Match-flow clock (design note §7): half-time ends-swap and full-time gameplay freeze, each
        // fired exactly once (guarded by these flags). Serialized at v15 (cross-tick).
        private bool _secondHalfStarted;
        private bool _matchEnded;

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
        // #27 T1 (projection design KD-P2/KD-P6): the canonical per-slot player record every per-spec
        // attribute surface projects from (PlayerAttributeProjection). Defaults to CreateDefault()
        // (all-neutral — projects byte-identically to the pre-T1 seeds, KD-P7); ConfigureSquads
        // overwrites it pre-kickoff from a real Squad. Fully qualified — the bare type name collides
        // with AgentMovement.PlayerAttributes (CS0104, KD-P6). Boot-deterministic on the default
        // path, NOT serialized (same B3 exclusion class as _attrs; distinct-squad restore is the T3
        // roster-reference deliverable — KD-P10, see the exclusion proof in SerializeWorldState).
        private readonly TacticalDirector.PlayerDatabase.PlayerAttributes[] _canonicalAttrs; // [SQUAD_SIZE]
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

        // P1 (interactive-unity-client-design.md §5-P1, KD-P1-3) — the restart applied during the
        // CURRENT tick, for the presentation layer's HUD. Reset in RunInputPhase alongside
        // _aiPhaseRanThisTick and written by ApplyRestart, so this is within-tick state, NOT
        // cross-tick state: there is nothing here for the snapshot to carry, and the
        // SerializeWorldState exclusion proof needs no new class. The cross-tick memory a HUD wants
        // ("hold the banner for ~2 s") is latched by LiveMatchStreamer, which has no determinism
        // obligations. No gameplay path reads either field.
        private RestartCue _restartAppliedThisTick;
        private int        _restartAwardedTeamThisTick;

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

            // §4.8.2 runtime float-mode gate — read the live MXCSR on the sim thread at boot and reject a
            // host whose DAZ/FTZ/rounding bits diverge from the Stage-0 pin (defense-in-depth over the
            // certified fingerprint). A no-op where the native shim is absent (Linux CI / dev / no plugin);
            // enforces only where it loads (the pinned cert host). See native/mxcsr_query.c + MxcsrValidator.
            MxcsrValidator.ValidateStage0FloatMode();

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

            // #27 T1 — canonical player records default to all-neutral; every attribute surface below
            // is a projection of this array (allocated before InitializeKickoffState, its first reader).
            _canonicalAttrs = new TacticalDirector.PlayerDatabase.PlayerAttributes[MatchEngineConstants.SQUAD_SIZE];
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                _canonicalAttrs[i] = TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault();
            }

            // §4 step 4 — initialise kickoff world state (deterministic; no RNG).
            InitializeKickoffState();

            // §4 step 3 (cont.) — Resolve subsystems (Phase C C1). Kickoff ball is loose.
            _possessingAgentId     = MatchEngineConstants.NO_POSSESSION;
            _prevPossessingAgentId = MatchEngineConstants.NO_POSSESSION; // Phase E — no transition at boot
            _collisionSystem   = new CollisionSubsystem(MatchEngineConstants.SQUAD_SIZE);
            _eventConsumer     = new MatchFlowCollisionConsumer(this);
            _stumbleScratch    = new bool[MatchEngineConstants.SQUAD_SIZE];

            // Match-flow completion (design note §3) — the first host-owned RNG draw site. Registered
            // once here; the entityId -1 sentinel matches the InteractionTextGenerator (#22) world-
            // scoped-stream convention (this is a match-wide, not per-agent, draw).
            _cardSeverityStreamIndex = _rng.RegisterStream(
                "match-flow.card-severity", SubsystemOrdinals.EventSystem, entityId: -1, streamVersion: 1);

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

            // WS-1: the default in-code preset catalogue (byte-identical to the pre-refactor static
            // path — it wraps the unchanged TacticPresetLibrary).
            _presetCatalogue = new TacticalDirector.TacticalInstructions.InCodeTacticPresetCatalogue();

            // Engine score state (v14): 0–0 at kickoff; no agent has held possession yet.
            _goals             = new int[MatchEngineConstants.TEAM_COUNT];
            _lastHolderAgentId = MatchEngineConstants.NO_POSSESSION;

            // Match-flow completion (design note §2/§3/§6/§7): discipline, substitutions, match-flow
            // clock. Every array starts at its behaviour-neutral identity (no cards, no subs used, no
            // transition fired) — a match that never calls SubstitutePlayer and never triggers a foul
            // is unaffected.
            _yellowCards            = new byte[MatchEngineConstants.SQUAD_SIZE];
            _isSentOff              = new bool[MatchEngineConstants.SQUAD_SIZE];
            _foulCooldownRemaining  = 0;
            _activeBenchSlot        = new int[MatchEngineConstants.SQUAD_SIZE];
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                _activeBenchSlot[i] = -1;
            }
            _substitutionsUsed = new int[MatchEngineConstants.TEAM_COUNT];
            // #27 T3: no squad configured yet — both teams reference the sentinel until ConfigureSquads.
            _rosterClubId = new int[MatchEngineConstants.TEAM_COUNT];
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                _rosterClubId[t] = MatchEngineConstants.NO_ROSTER_CLUB_ID;
            }
            int maxPendingSubs = MatchEngineConstants.MAX_SUBSTITUTIONS_PER_TEAM * MatchEngineConstants.TEAM_COUNT;
            _pendingSubOutgoing = new int[maxPendingSubs];
            _pendingSubIncoming = new int[maxPendingSubs];
            _pendingSubTeam     = new byte[maxPendingSubs];
            _pendingSubReason   = new byte[maxPendingSubs];
            _pendingSubCount    = 0;

            // Bench roster (design note §6): a Stage-0 in-code source, mirroring the TeamTacticConfig
            // precedent — the on-disk loader is a Stage 1+ parser swap. Every slot defaults to the
            // neutral identity attributes/perf/GK-flag; a real per-competition bench is a config-loader
            // follow-up, not a Stage-0 requirement.
            _benchAttrs          = new PlayerAttributes[MatchEngineConstants.TEAM_COUNT][];
            _benchPerfs          = new PerformanceContext[MatchEngineConstants.TEAM_COUNT][];
            _benchIsGoalkeeper   = new bool[MatchEngineConstants.TEAM_COUNT][];
            _benchCanonicalAttrs = new TacticalDirector.PlayerDatabase.PlayerAttributes[MatchEngineConstants.TEAM_COUNT][];
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                _benchAttrs[t]          = new PlayerAttributes[MatchEngineConstants.SUBSTITUTES_PER_TEAM];
                _benchPerfs[t]          = new PerformanceContext[MatchEngineConstants.SUBSTITUTES_PER_TEAM];
                _benchIsGoalkeeper[t]   = new bool[MatchEngineConstants.SUBSTITUTES_PER_TEAM];
                _benchCanonicalAttrs[t] = new TacticalDirector.PlayerDatabase.PlayerAttributes[MatchEngineConstants.SUBSTITUTES_PER_TEAM];
                for (int b = 0; b < MatchEngineConstants.SUBSTITUTES_PER_TEAM; b++)
                {
                    // #27 T1: bench attrs are the #2 projection of the canonical bench record
                    // (all-neutral by default — projects to exactly the pre-T1 CreateDefault()).
                    _benchCanonicalAttrs[t][b] = TacticalDirector.PlayerDatabase.PlayerAttributes.CreateDefault();
                    _benchAttrs[t][b] = PlayerAttributeProjection.ToAgentMovement(in _benchCanonicalAttrs[t][b]);
                    _benchPerfs[t][b] = PerformanceContext.CreateNeutral();
                    _benchIsGoalkeeper[t][b] = false;
                }
            }

            _secondHalfStarted = false;
            _matchEnded        = false;

            // P1 KD-P1-3 — the pre-first-tick value of the restart cue. RunInputPhase re-establishes
            // this every tick, but a caller may observe a booted engine before the first RunTick, and
            // the awarded-team field's zero default would otherwise read as "team 0" while the cue
            // says None. Set here so "no restart" is a single coherent answer from boot onward.
            _restartAppliedThisTick     = RestartCue.None;
            _restartAwardedTeamThisTick = MatchEngineConstants.NO_RESTART_TEAM;

            // GK (#11) / Heading (#10) engine integration (gk-heading-engine-integration-design.md §3.1,
            // Phase 1). Construct both orchestrators + their stateless ball/RNG adapters, and register the
            // two subsystem RNG streams (fixed order → stable indices; the card-severity precedent, KD-1).
            // Constructed unconditionally — this only allocates arrays, touching no serialized world state,
            // so the default (flag-off) engine stays byte-identical. Both are DRIVEN and their §4 triggers
            // fired only under _gkHeadingEnabled (KD-11), which starts false.
            var gkHeadingWorld = new GkHeadingWorldAdapter(this);   // one adapter, all four boundary interfaces
            _headingStreamIndex = _rng.RegisterStream(
                "heading.mechanics", SubsystemOrdinals.HeadingMechanics, entityId: -1, streamVersion: 1);
            _goalkeeperStreamIndex = _rng.RegisterStream(
                "goalkeeper.mechanics", SubsystemOrdinals.GoalkeeperMechanics, entityId: -1, streamVersion: 1);
            _heading    = new TacticalDirector.HeadingMechanics.HeadingMechanics(gkHeadingWorld, gkHeadingWorld);
            _goalkeeper = new TacticalDirector.GoalkeeperMechanics.GoalkeeperMechanics(gkHeadingWorld, gkHeadingWorld);

            // Keeper roster: GoalkeeperConstants.MaxGkAgents == TEAM_COUNT == 2, so keeper index == team id
            // (keeper t is team t's goalkeeper). _teamIds / _isGoalkeeper are boot-populated above.
            //
            // That equality is load-bearing and is NOT structurally guaranteed: MaxGkAgents is a `[GT]`
            // read off GameplayConfig while TEAM_COUNT is a `[FIXED]` const, so a config file alone can
            // break the identity. Everything keyed on it would then be silently wrong rather than loud —
            // `NotifyKeeperOfShot` routes by `1 - shooterTeam`, `HostSaveDispatch` maps agent → keeper
            // slot, and #11's own `TacticalTick` derives which goal a keeper defends from `gkIndex`
            // (ERR-011-002). A keeper would defend the wrong end of the pitch, exactly the defect that
            // fix removed. Gate it at boot instead: one config typo fails here, at the composition root
            // that depends on it, instead of surfacing as inexplicable goalkeeping.
            if (GoalkeeperConstants.MaxGkAgents != MatchEngineConstants.TEAM_COUNT)
            {
                throw new InvalidOperationException(
                    "GoalkeeperConstants.MaxGkAgents (" + GoalkeeperConstants.MaxGkAgents.ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    + ") must equal MatchEngineConstants.TEAM_COUNT ("
                    + MatchEngineConstants.TEAM_COUNT.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    + "): the match engine keys keeper index directly on team id (#11 KD-1).");
            }

            _gkAgentIds = new int[GoalkeeperConstants.MaxGkAgents];
            RefreshGkAgentIds();   // refreshed each drive too (ConfigureSquads / substitutions move the GK slot)
            _saveCommittedForGk         = new bool[GoalkeeperConstants.MaxGkAgents];
            _headerCommittedThisEpisode = new bool[MatchEngineConstants.SQUAD_SIZE];
            // §5.Z.15 — DEFAULT ON. Phase 2 serialized the GK/Heading cross-tick state at v18, which is
            // what made a flag-on engine snapshot-safe, and recorded "flip the default to on, take the
            // digest rebaseline" as the remaining work. This is that flip: a keeper that never attempts
            // a save is a large part of a goal rate ~10x football's, and leaving Goalkeeper Mechanics
            // #11 built, wired, tested and switched OFF meant every match was played without one.
            // No absolute rebaseline is needed — every determinism test in this tree is comparative
            // (two same-seed runs), and EnableGkHeading() remains for symmetry with DisableGkHeading().
            _gkHeadingEnabled = true;

            // §5.Z.15 six-second rule — idle at boot (no keeper holds the ball at kickoff).
            _gkHoldTicks                = 0;
            _gkReleaseCooldownRemaining = 0;
            _gkReleasedAgentId          = MatchEngineConstants.NO_POSSESSION;

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
            // ERR-008-013: the GK save sink. One instance backs all 22 trees; only ever called for the
            // flag-on threatened keeper (SAVE is generated only under TacticalContext.SaveAvailable).
            var saveDispatch = new HostSaveDispatch(this);
            _decisionTrees = new DecisionTreeAI[MatchEngineConstants.SQUAD_SIZE];
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                _decisionTrees[i] = new DecisionTreeAI(
                    i, movementController, matchSeed, _passExecutors[i], _shotExecutors[i], saveDispatch);
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

            // §5.Z.15 — Heading (#10) / Goalkeeper (#11) producers. Both orchestrators publish through
            // their own EventBusStub the moment they resolve an attempt (HeaderExecutedEvent 0x12 Tier B
            // / HeaderAttemptFailedEvent 0x13 Tier C / SaveAttemptedEvent 0x14 / BallClaimedEvent 0x15 /
            // DistributionExecutedEvent 0x16 / GoalkeeperRushEvent 0x17), and an unregistered ordinal
            // throws. Missing until now purely because the flag defaulted OFF, so the publish path had
            // never run inside the engine — flipping the default surfaced it on the first mistimed
            // header (ERR_EVT_UNREGISTERED_ORDINAL out of HeadingMechanics.EmitFailedAttempt). Both
            // carry the same idempotent s_registered guard as the three above.
            TacticalDirector.HeadingMechanics.EventBusRegistrar.Initialize();
            TacticalDirector.GoalkeeperMechanics.EventBusRegistrar.Initialize();

            // Phase E — subscribe the real cross-subsystem consumer: possession-changed → AI. Tier A
            // subscription MUST happen during the boot phase (#17 FR-EVT-020/021 — Subscribe throws
            // ERR_EVT_REGISTRATION_PHASE after the first DrainTick), which is why this is here in Boot and
            // not lazily. The handler is a method group (no per-frame closure). PossessionChangedEvent
            // (ordinal 0x04) is a seeded EventRegistry row, so EnsureInitialized() inside Subscribe has
            // already populated its ordinal cache by now. The returned token is discarded — the bus is
            // reset per match (ResetForNewMatch above), so there is no per-subscription teardown to do.
            EventBus.Subscribe<PossessionChangedEvent>(OnPossessionChanged);

            // §5.Z Phase H — award the opening kickoff to the HOME team (team 0 kicks off the first half;
            // CheckMatchFlowTransitions hands the second-half kickoff to the other side). This is the one
            // production possession grant that does not presuppose the ball already moving, and without it
            // the whole match deadlocks 0–0 (ERR-030-014); see ApplyRestart for the full rationale. Placed
            // here, at the end of Boot, because SelectRestartTaker reads _teamIds / _agents (seeded by
            // InitializeKickoffState) AND _isSentOff (allocated with the discipline state further up) — all
            // three are live by this point. _prevPossessingAgentId is deliberately left at NO_POSSESSION so
            // the first Resolve publishes the loose → taker PossessionChangedEvent, exactly as a mid-match
            // restart does.
            _possessingAgentId = SelectRestartTaker(
                new Vector2(MatchEngineConstants.KickoffBallXM, MatchEngineConstants.KickoffBallYM),
                MatchEngineConstants.FIRST_HALF_KICKOFF_TEAM);

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
        /// Restore factory (snapshot-deserialize design note KD-4): produces a ready-to-tick
        /// <see cref="MatchEngine"/> from a saved snapshot — the (header, payload) pair
        /// <see cref="SerializeWorldState"/> writes. A static factory (rather than a Load() on a running
        /// instance) is chosen because boot does load-bearing wiring that must happen exactly once, before
        /// any state is applied, and a "half-booted, half-restored" instance is not a valid state to expose.
        ///
        /// Step 0 (KD-6 / §4.8.2) — validate the runtime float mode AND the header's
        /// <see cref="EnvironmentFingerprint"/> BEFORE any state is touched, so a rejected restore mutates
        /// nothing. The float-mode half reads the live MXCSR via <see cref="MxcsrValidator"/> (rejecting a
        /// host whose DAZ/FTZ/rounding bits diverge from the Stage-0 pin; a no-op where the native shim is
        /// absent, e.g. the Linux CI gate). The fingerprint half runs only when the header carries a
        /// fingerprint (O3: the deterministic-sim <c>SaveManager</c> writes <c>Fingerprint = null</c>, so a
        /// null fingerprint is skipped-with-intent; the on-disk <c>MatchSaveManager</c> serializes a real
        /// one). The live fingerprint is the recorded/dev tuple
        /// (<see cref="EnvironmentFingerprint.CreateStage0Dev"/>) — at Stage 0 a self-consistency
        /// (schema/tuple) check. The MXCSR query enforces the (already-certified) pin as defense-in-depth;
        /// it is not the proof the bits are exact (the determinism-KAT run is).
        ///
        /// Step 1 — construct a fresh engine through the normal boot path (which also runs step 2:
        /// <see cref="EventBus.ResetForNewMatch"/>, so the process-static bus is clean for the restored
        /// match). Step 3 — <see cref="DeserializeWorldState"/> overwrites the boot-seeded cross-tick state
        /// with the saved state (and restores the clock + card-severity RNG cursor). Step 3b (#27 T3 / KD-3,
        /// Phase 2) — <see cref="ReprojectDistinctSquads"/> re-derives the per-slot attribute records for any
        /// team that loaded a distinct squad (the payload carries only the roster IDENTITY, not the attribute
        /// VALUES) from <paramref name="squads"/>, failing loud if a referenced roster is unresolvable; a
        /// neutral / unconfigured-squad match skips this and needs no provider. Step 4 — restore the digest
        /// chain from the header so the NEXT tick's digest equals what an uninterrupted run would produce
        /// (KD-5, the round-trip determinism contract).
        /// </summary>
        /// <param name="header">The saved snapshot header (fingerprint + digest chain + tick + versions).</param>
        /// <param name="payload">The saved snapshot payload (the cross-tick world-state bytes).</param>
        /// <param name="matchSeed">The boot match seed the payload does not carry (KD-7 O1 — the caller
        /// persists it alongside the payload; an on-disk boot-header is revisited at the save-file root).</param>
        /// <param name="squads">Resolver for the club rosters a distinct-squad match loaded (#27 T3 / KD-3).
        /// Required (non-null, resolving every referenced <c>ClubId</c>) when the snapshot was taken after
        /// <see cref="ConfigureSquads"/>; ignored for a neutral / unconfigured-squad match. Must return the
        /// SAME rosters the saved match loaded — see <see cref="ISquadProvider"/>.</param>
        public static MatchEngine RestoreFromSnapshot(
            in SnapshotHeader header, SnapshotPayload payload, ulong matchSeed, ISquadProvider squads = null)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            // Step 0 (KD-6 / §4.8.2) — runtime float-mode gate, before any state is touched. Read the live
            // MXCSR on the sim thread and reject a host whose DAZ/FTZ/rounding bits diverge from the Stage-0
            // pin, so a restore never resumes a certified snapshot under a divergent float mode. A no-op
            // where the native shim is absent; enforces only where it loads. See MxcsrValidator.
            MxcsrValidator.ValidateStage0FloatMode();

            // Step 0 (KD-6 / O3) — fingerprint gate, before any state is touched.
            if (header.Fingerprint != null)
            {
                EnvironmentFingerprint live = EnvironmentFingerprint.CreateStage0Dev();
                ushort code = header.Fingerprint.ValidateAgainst(live);
                if (code != 0)
                {
                    throw new InvalidOperationException(
                        $"Snapshot environment fingerprint mismatch (code 0x{code:X4}) — refusing to restore " +
                        "a snapshot captured under a different float/runtime environment (KD-6 / #16 §4.8.2).");
                }
            }

            // Step 1 (+2) — fresh boot (allocations, EventBus.ResetForNewMatch, boot-constant seeding).
            MatchEngine engine = new MatchEngine(matchSeed);

            // Step 3 — overwrite the boot-seeded cross-tick state with the saved state.
            engine.DeserializeWorldState(payload);

            // KD-3 (#27 T3, Phase 2): a match booted through ConfigureSquads with a DISTINCT squad carries a
            // non-sentinel roster reference (v16) whose per-slot attribute records (the boot-constant
            // exclusion) must be re-projected from the actual Squad — the payload carries only the roster
            // IDENTITY (its ClubId), not the attribute VALUES. ReprojectDistinctSquads re-derives them from
            // the caller-supplied ISquadProvider, keyed by the serialized _activeBenchSlot for substitutions,
            // and fails loud (rather than silently falling back to CreateDefault() and diverging on the very
            // next tick, R4) if the provider is absent or cannot resolve a referenced ClubId. The neutral
            // path (every _rosterClubId == NO_ROSTER_CLUB_ID — every match that never calls ConfigureSquads)
            // needs no provider and returns immediately.
            engine.ReprojectDistinctSquads(squads);

            // Step 4 (KD-5) — continue the digest chain from the saved link so the next tick's digest matches
            // an uninterrupted run. The clock was restored to the saved tick inside DeserializeWorldState.
            engine._codec.CommitLoadedDigest(header);

            if (engine._clock.CurrentTick != header.Tick)
            {
                throw new InvalidOperationException(
                    $"Restored clock tick {engine._clock.CurrentTick} != header tick {header.Tick} " +
                    "— payload/header tick disagreement.");
            }

            return engine;
        }

        /// <summary>
        /// Snapshot-deserialize Phase 2 (#27 T3 / KD-3): re-derives every distinct-squad team's per-slot
        /// attribute records from the roster its serialized <c>_rosterClubId</c> (v16) names. The payload
        /// carries only the roster IDENTITY (each team's <c>ClubId</c>), not the attribute VALUES (the
        /// boot-constant exclusion — <c>_canonicalAttrs</c> / <c>_attrs</c> / <c>_dtAttrs</c> /
        /// <c>_perceptionAttrs</c> / bench attrs are NOT serialized), so a distinct-squad match cannot be
        /// restored faithfully without its rosters back. Both teams' resolved squads are resolved,
        /// ClubId-checked, size-checked, lineup-selected, and record-validated BEFORE any is applied (the
        /// <see cref="ConfigureSquads"/> validate-both-before-write discipline); then the base lineup is
        /// re-projected and the substitution swaps the serialized <c>_activeBenchSlot</c> records are
        /// replayed. The neutral / unconfigured-squad path (both <c>_rosterClubId == NO_ROSTER_CLUB_ID</c>)
        /// returns immediately and needs no provider. Fails loud on an absent provider, an unresolvable
        /// ClubId, or a provider that returns a mismatched roster — a distinct-squad match must not silently
        /// fall back to <c>CreateDefault()</c> and diverge from the saved run (R4). Determinism rests on the
        /// provider returning the SAME roster the saved match loaded: <see cref="LineupSelector.Select"/> and
        /// <see cref="PlayerAttributeProjection"/> are pure, so an identical roster reproduces the exact
        /// per-slot records the save held.
        /// </summary>
        /// <param name="squads">The caller-supplied ClubId -> Squad resolver, or <c>null</c> for a neutral
        /// restore (fails loud if any team is distinct-squad).</param>
        private void ReprojectDistinctSquads(ISquadProvider squads)
        {
            bool anyDistinct = false;
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                if (_rosterClubId[t] != MatchEngineConstants.NO_ROSTER_CLUB_ID)
                {
                    anyDistinct = true;
                }
            }
            if (!anyDistinct)
            {
                return;  // Phase-1 neutral path: no ConfigureSquads was called, nothing to re-project.
            }

            // Resolve + validate + select for EVERY distinct team BEFORE applying any — a failure leaves the
            // engine un-re-projected (and the throwing factory discards it), mirroring ConfigureSquads'
            // validate-both-before-write rule so there is no half-re-projected intermediate.
            var resolved = new TacticalDirector.PlayerDatabase.Squad[MatchEngineConstants.TEAM_COUNT];
            var plans    = new LineupPlan[MatchEngineConstants.TEAM_COUNT];
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                if (_rosterClubId[t] == MatchEngineConstants.NO_ROSTER_CLUB_ID)
                {
                    continue;
                }
                if (squads == null)
                {
                    throw new System.NotSupportedException(
                        $"RestoreFromSnapshot: the snapshot references a distinct squad for team {t} "
                        + $"(ClubId {_rosterClubId[t]}) but no ISquadProvider was supplied — its per-slot "
                        + "attribute records cannot be re-projected (#27 T3 / KD-3). Pass the rosters the "
                        + "saved match loaded.");
                }
                TacticalDirector.PlayerDatabase.Squad squad = squads.ResolveByClubId(_rosterClubId[t]);
                if (squad == null)
                {
                    throw new System.NotSupportedException(
                        $"RestoreFromSnapshot: the ISquadProvider returned no Squad for team {t}'s ClubId "
                        + $"{_rosterClubId[t]} — a distinct-squad match cannot be faithfully restored without "
                        + "its roster (#27 T3 / KD-3).");
                }
                if (squad.ClubId != _rosterClubId[t])
                {
                    throw new System.InvalidOperationException(
                        $"RestoreFromSnapshot: the ISquadProvider returned a Squad with ClubId {squad.ClubId} "
                        + $"for the requested ClubId {_rosterClubId[t]} (team {t}) — resolver contract "
                        + "violation.");
                }
                ValidateSquadSize(t, squad);
                LineupPlan plan = LineupSelector.Select(squad, MatchEngineConstants.STAGE0_FORMATION);
                ValidateSelectedRecords(t, squad, in plan);
                resolved[t] = squad;
                plans[t]    = plan;
            }

            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                if (_rosterClubId[t] == MatchEngineConstants.NO_ROSTER_CLUB_ID)
                {
                    continue;
                }
                ReprojectBaseLineup(t, resolved[t], in plans[t]);
                ReprojectSubstitutions(t);
            }
        }

        /// <summary>
        /// Re-projects one team's base-lineup attribute arrays from its resolved squad + selected lineup
        /// (the attribute half of <see cref="ApplySquad"/>). Deliberately does NOT write the ON-PITCH
        /// goalkeeper flags (<c>_isGoalkeeper</c>) — those are serialized (a v-restored value that already
        /// reflects any substitution, so re-writing from the plan would clobber a substituted slot's restored
        /// bench-GK flag with the starter's). It DOES re-project the BENCH goalkeeper flags
        /// (<c>_benchIsGoalkeeper</c>): unlike the on-pitch array those are a boot-constant NOT serialized,
        /// so a fresh boot leaves them all-<c>false</c>, and a substitution made AFTER the restore must be
        /// able to bring a bench goalkeeper on with the correct flag. Used only by
        /// <see cref="ReprojectDistinctSquads"/> at restore.
        /// </summary>
        private void ReprojectBaseLineup(
            int teamId, TacticalDirector.PlayerDatabase.Squad squad, in LineupPlan plan)
        {
            for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
            {
                int i     = teamId * MatchEngineConstants.PLAYERS_PER_TEAM + k;
                int local = plan.StarterLocalIndices[k];
                _canonicalAttrs[i]  = squad.GetPlayer(local).Attributes;
                _attrs[i]           = PlayerAttributeProjection.ToAgentMovement(in _canonicalAttrs[i]);
                _dtAttrs[i]         = PlayerAttributeProjection.ToDecisionTree(in _canonicalAttrs[i], teamId);
                _perceptionAttrs[i] = PlayerAttributeProjection.ToPerception(
                    in _canonicalAttrs[i], teamId, _perceptionAttrs[i].IsHalfTurned);
            }
            for (int b = 0; b < MatchEngineConstants.SUBSTITUTES_PER_TEAM; b++)
            {
                int local = plan.BenchLocalIndices[b];
                _benchCanonicalAttrs[teamId][b] = squad.GetPlayer(local).Attributes;
                _benchAttrs[teamId][b] =
                    PlayerAttributeProjection.ToAgentMovement(in _benchCanonicalAttrs[teamId][b]);
                _benchIsGoalkeeper[teamId][b] = plan.BenchIsGoalkeeper[b];
            }
        }

        /// <summary>
        /// Replays the attribute half of every substitution the serialized <c>_activeBenchSlot</c> records
        /// for team <paramref name="teamId"/>: after <see cref="ReprojectBaseLineup"/> re-seeds the base
        /// lineup, a slot that was substituted must again hold the bench player's re-projected attributes,
        /// not the starter's (the attribute half of <see cref="SubstitutePlayer"/>). The serialized-state
        /// half of a substitution (<c>_isGoalkeeper</c>, <c>_yellowCards</c>, <c>_activeBenchSlot</c>,
        /// <c>_substitutionsUsed</c>) is already restored from the payload, and <c>_perfs</c> is the
        /// boot-neutral constant on both sides (it would become serialized, not re-projected, if it ever
        /// went non-neutral — the exclusion-proof PHASE-D note), so only the boot-constant attribute arrays
        /// are re-derived here. Used only by <see cref="ReprojectDistinctSquads"/> at restore.
        /// </summary>
        private void ReprojectSubstitutions(int teamId)
        {
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (_teamIds[i] != teamId)
                {
                    continue;
                }
                int benchIndex = _activeBenchSlot[i];
                if (benchIndex == -1)
                {
                    continue;
                }
                _attrs[i]           = _benchAttrs[teamId][benchIndex];
                _canonicalAttrs[i]  = _benchCanonicalAttrs[teamId][benchIndex];
                _dtAttrs[i]         = PlayerAttributeProjection.ToDecisionTree(in _canonicalAttrs[i], teamId);
                _perceptionAttrs[i] = PlayerAttributeProjection.ToPerception(
                    in _canonicalAttrs[i], teamId, _perceptionAttrs[i].IsHalfTurned);
            }
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

                    // ONE own-half template, mirrored for the away side (§5.Z.12). Every position and
                    // facing below is expressed in the acting team's own-half frame and passed through
                    // the existing mirror helpers, so there is no Home/Away constant pair to keep in
                    // agreement. That pairing is the shape behind three defects in this engine's history
                    // — ERR-008-002 (away zone modifiers inverted), ERR-013-009/010 (AttackingDirection
                    // inverted) and the §5.Z.10 keeper spawn — and a mirror has one place to be wrong
                    // where a pair has two.
                    //
                    // The keeper stands on the goal line it DEFENDS, centred on the goal mouth, not on
                    // the outfield line with everyone else. Load-bearing far beyond kickoff: the Physics
                    // phase skips goalkeepers at Stage 0 (#11 owns GK locomotion), so wherever boot puts
                    // a keeper is where it stands for the WHOLE match. Under the old shared-line
                    // placement the keeper took the k = 0 lateral slot — 26 m upfield of its own goal and
                    // 28 m off-centre — leaving BOTH goals unguarded for ninety minutes (§5.Z.10).
                    //
                    // Outfielders spread evenly across the width (k+1 of PLAYERS_PER_TEAM+1 gaps) on a
                    // quarter-length line. Transient: the AI phase moves them onto real formation slots
                    // on the first stride tick.
                    Vector2 ownHalfSpawn = _isGoalkeeper[i]
                        ? new Vector2(
                            MatchEngineConstants.GkKickoffDepthM,
                            MatchEngineConstants.PITCH_WIDTH_M * 0.5f)
                        : new Vector2(
                            MatchEngineConstants.OutfieldKickoffLineXM,
                            MatchEngineConstants.PITCH_WIDTH_M
                                * (k + 1) / (MatchEngineConstants.PLAYERS_PER_TEAM + 1));

                    // Facing is a free vector, so it mirrors by negation rather than about the pitch
                    // centre. Every team faces the goal it attacks: +X in its own frame. This also
                    // removes the trig the former degrees-based helper needed — a pure negation of exact
                    // unit components cannot introduce the floating-point fuzz (Mathf.Sin(180°) ≈ 8.7e-8)
                    // that helper existed to special-case away from the deterministic snapshot.
                    _agents[i] = AgentState.CreateAtPosition(
                        MirrorPitchIfAway(team, ownHalfSpawn),
                        MirrorVelocityIfAway(team, new Vector2(1f, 0f)));
                    // #27 T1: the #2 locomotion attrs are a projection of the canonical record
                    // (all-neutral at boot ⇒ byte-identical to the pre-T1 CreateDefault() seed).
                    _attrs[i]  = PlayerAttributeProjection.ToAgentMovement(in _canonicalAttrs[i]);
                    _perfs[i]  = PerformanceContext.CreateNeutral();

                    // Boot-time command: hold formation position. The AI phase (Phase D) replaces
                    // this on the first stride tick (tick 6); until then every agent holds (§3).
                    _commands[i] = MovementCommand.Stop(_agents[i].Position);
                }
            }
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
            GuardTeamId(teamId);
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
        /// Substitutes a bench player onto the pitch in place of an on-pitch agent (design note §6).
        /// The outgoing slot's attributes/performance-context/goalkeeper-flag are overwritten from the
        /// Stage-0 in-code bench config (<paramref name="teamId"/>'s slot <paramref name="benchIndex"/>);
        /// position/velocity are left untouched (no re-entry ceremony, matching every other restart's
        /// minimalism). The outgoing agent's DecisionTree is interrupted so it re-plans fresh next AI
        /// stride (reuses the existing possession-change seam). Publishes a Tier A
        /// <see cref="SubstitutionEvent"/> with a synthetic incoming-player id
        /// (<c>SQUAD_SIZE + teamId * SUBSTITUTES_PER_TEAM + benchIndex</c>) distinct from any on-pitch
        /// slot index. Fails loud (boot/manager-decision-time call, not hot-path) on: a call after
        /// full time (AR-7 L-2 — the state effects would apply to a frozen match while the queued
        /// <see cref="SubstitutionEvent"/> could never flush, RunResolvePhase returning before
        /// <see cref="PublishPendingSubstitutions"/> once <see cref="_matchEnded"/> is set); an
        /// out-of-range team/slot/bench index; a slot not belonging to <paramref name="teamId"/>; a
        /// sent-off or already-substituted slot; a bench index already used this match for the team;
        /// or the team's <see cref="MatchEngineConstants.MAX_SUBSTITUTIONS_PER_TEAM"/> cap already
        /// reached. The outgoing slot's yellow-card count resets to 0 (AR-7 M-1 — discipline attaches
        /// to the PLAYER, and the slot now holds a different player; without the reset a substitute
        /// replacing a booked player would be sent off on their own first yellow).
        /// </summary>
        public void SubstitutePlayer(int teamId, int outSlotIndex, int benchIndex, SubstitutionReason reason)
        {
            if (_matchEnded)
            {
                throw new System.InvalidOperationException(
                    "SubstitutePlayer: the match has ended (full time) — no further substitutions.");
            }
            GuardTeamId(teamId);
            if (outSlotIndex < 0 || outSlotIndex >= MatchEngineConstants.SQUAD_SIZE)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(outSlotIndex), outSlotIndex, "outSlotIndex must be a roster index in [0, SQUAD_SIZE).");
            }
            if (_teamIds[outSlotIndex] != teamId)
            {
                throw new System.ArgumentException(
                    "SubstitutePlayer: outSlotIndex does not belong to teamId.", nameof(outSlotIndex));
            }
            if (_isSentOff[outSlotIndex])
            {
                throw new System.InvalidOperationException("SubstitutePlayer: outSlotIndex has been sent off.");
            }
            if (_activeBenchSlot[outSlotIndex] != -1)
            {
                throw new System.InvalidOperationException("SubstitutePlayer: outSlotIndex has already been substituted.");
            }
            if (benchIndex < 0 || benchIndex >= MatchEngineConstants.SUBSTITUTES_PER_TEAM)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(benchIndex), benchIndex, "benchIndex must be in [0, SUBSTITUTES_PER_TEAM).");
            }
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (_teamIds[i] == teamId && _activeBenchSlot[i] == benchIndex)
                {
                    throw new System.InvalidOperationException("SubstitutePlayer: benchIndex has already been used this match.");
                }
            }
            if (_substitutionsUsed[teamId] >= MatchEngineConstants.MAX_SUBSTITUTIONS_PER_TEAM)
            {
                throw new System.InvalidOperationException("SubstitutePlayer: teamId has used all permitted substitutions.");
            }

            _attrs[outSlotIndex]        = _benchAttrs[teamId][benchIndex];
            _perfs[outSlotIndex]        = _benchPerfs[teamId][benchIndex];
            _isGoalkeeper[outSlotIndex] = _benchIsGoalkeeper[teamId][benchIndex];
            // #27 T1: the slot now holds a different PLAYER — copy the canonical bench record and
            // re-project the boot-seeded per-slot AI surfaces (#8 / #7), which are otherwise only
            // written at InitializeAiSnapshots (pre-T1 this was a no-op: every record was neutral;
            // the root-CLAUDE.md v2.20 substitution-attrs hazard's on-pitch half). The per-call
            // surfaces (Pass/Shot builders, snapshot fills, FirstTouchContext) read _canonicalAttrs
            // live and need no re-seed. TeamId is slot identity (unchanged); IsHalfTurned is runtime
            // stance, preserved. Restore scope: _canonicalAttrs is not serialized — a distinct-squad
            // substitution's bench record is reconstructible from the v16 roster reference (#27 T3)
            // + the serialized _activeBenchSlot, once a snapshot-deserialize path exists (KD-P10/KD-T3-3).
            _canonicalAttrs[outSlotIndex] = _benchCanonicalAttrs[teamId][benchIndex];
            _dtAttrs[outSlotIndex] = PlayerAttributeProjection.ToDecisionTree(
                in _canonicalAttrs[outSlotIndex], teamId);
            _perceptionAttrs[outSlotIndex] = PlayerAttributeProjection.ToPerception(
                in _canonicalAttrs[outSlotIndex], teamId, _perceptionAttrs[outSlotIndex].IsHalfTurned);
            // AR-7 M-1: discipline attaches to the player, not the slot — the incoming substitute
            // starts on zero yellows (the outgoing player's booking leaves the pitch with them;
            // Stage 0 has no persistent player identity to carry it to, and the slot's serialized
            // v15 count now describes the player actually occupying it). _isSentOff needs no
            // parallel reset: a sent-off slot refuses substitution above (red card ≠ substitution).
            _yellowCards[outSlotIndex] = 0;
            _decisionTrees[outSlotIndex].NotifyInterrupt();
            _activeBenchSlot[outSlotIndex] = benchIndex;
            _substitutionsUsed[teamId]++;

            // AR-5: queue the notification rather than publishing immediately — this method may be
            // called between ticks, when EventBus.CurrentPhase is not a valid producer phase. Flushed
            // at the top of the next RunResolvePhase. Capacity can never overflow (bounded by every
            // team's MAX_SUBSTITUTIONS_PER_TEAM, enforced above).
            int incomingId = MatchEngineConstants.SQUAD_SIZE
                + teamId * MatchEngineConstants.SUBSTITUTES_PER_TEAM + benchIndex;
            _pendingSubOutgoing[_pendingSubCount] = outSlotIndex;
            _pendingSubIncoming[_pendingSubCount] = incomingId;
            _pendingSubTeam[_pendingSubCount]     = (byte)teamId;
            _pendingSubReason[_pendingSubCount]   = (byte)reason;
            _pendingSubCount++;
        }

        /// <summary>Publishes every queued <see cref="SubstitutionEvent"/> from <see cref="SubstitutePlayer"/>
        /// calls made since the last flush (design note §6, AR-5), then clears the queue. Called at the
        /// top of <see cref="RunResolvePhase"/>, where <c>EventBus.CurrentPhase == Resolve</c> — the
        /// registered producer phase for <see cref="SubstitutionEvent"/>.</summary>
        private void PublishPendingSubstitutions()
        {
            for (int i = 0; i < _pendingSubCount; i++)
            {
                var evt = new SubstitutionEvent(
                    _pendingSubOutgoing[i], _pendingSubIncoming[i], _pendingSubTeam[i], _pendingSubReason[i]);
                EventBus.Publish(in evt);
            }
            _pendingSubCount = 0;
        }

        /// <summary>
        /// Sources both teams' player attributes from real club squads (#27 T1 — the
        /// player-attribute projection design doc), assigning each to its formation slot by
        /// <b>proper lineup selection</b> (Plan-3, <c>docs/tracking/lineup-selection-design.md</c>):
        /// <see cref="LineupSelector"/> picks the eleven starters by coarse position + rating (KD-L2)
        /// and fills the seven bench slots best-remaining, replacing the earlier roster-order trust
        /// mapping. A starter slot with no eligible player for its required position fails loud
        /// (KD-L3); the per-slot goalkeeper flags flow from the selection, not the boot
        /// <c>k == 0</c> seed (KD-L4). Squad players beyond the consumed lineup are unused (a full
        /// 25-player club roster is accepted; only the selected 18 are validated + applied). A squad
        /// pre-ordered coherently (a goalkeeper, then each line best-rated first) reproduces the old
        /// roster-order mapping (KD-L5); selection is otherwise not roster-order — that is the point
        /// of "proper" selection.
        /// Overwrites the canonical per-slot records and re-projects every boot-seeded attribute
        /// surface (#2 <c>_attrs</c>, #8 <c>_dtAttrs</c>, #7 <c>_perceptionAttrs</c>, bench attrs);
        /// the per-call surfaces (Pass/Shot builders, Mechanics-AI snapshot fills,
        /// FirstTouchContext) read the canonical records live. Deliberately NOT behaviour-neutral
        /// for a non-neutral squad — that is the point of T1; an all-<c>CreateDefault</c> squad
        /// (or never calling this) is byte-identical to pre-T1 (KD-P7, digest-locked). Fails loud
        /// (boot-time call, not hot-path): null squads; a squad smaller than
        /// <c>PLAYERS_PER_TEAM + SUBSTITUTES_PER_TEAM</c>; any consumed player's attribute outside
        /// <c>[1,20]</c> (WeakFootRating <c>[1,5]</c>) — the FR-TP-014-style gate at the consuming
        /// seam, since <see cref="TacticalDirector.PlayerDatabase.Squad"/> accepts hand-built
        /// records; or a call after the first tick (a mid-match attribute swap is neither
        /// stride-aligned nor restore-coherent). RESTORE SCOPE (KD-P10 / #27 T3): the canonical
        /// attribute records are NOT serialized (re-derivable from the roster), but since T3 the
        /// per-team roster REFERENCE (each squad's <c>ClubId</c>) IS serialized (v16), so a save now
        /// records which squad each team loaded. Full distinct-squad restore still needs a
        /// snapshot-deserialize path to re-project the records from the referenced roster (keyed by
        /// the serialized <c>_activeBenchSlot</c> for substitutions) — none exists in the engine yet
        /// (KD-T3-3), so this remains a fidelity deliverable, not a silent divergence vector.
        /// </summary>
        /// <param name="homeSquad">Club squad for team 0 (home).</param>
        /// <param name="awaySquad">Club squad for team 1 (away).</param>
        public void ConfigureSquads(
            TacticalDirector.PlayerDatabase.Squad homeSquad,
            TacticalDirector.PlayerDatabase.Squad awaySquad)
        {
            if (homeSquad == null)
            {
                throw new System.ArgumentNullException(nameof(homeSquad));
            }
            if (awaySquad == null)
            {
                throw new System.ArgumentNullException(nameof(awaySquad));
            }
            if (_clock.CurrentTick != 0UL)
            {
                throw new System.InvalidOperationException(
                    "ConfigureSquads: pre-kickoff only — the match has already ticked (#27 T1).");
            }
            // Every fail-loud step for BOTH squads runs BEFORE any state is written, so a refused call
            // leaves the engine untouched — validating inside the per-team apply would let an invalid
            // away squad refuse only after the home squad had already landed (a half-applied
            // configuration; self-review AR-1 M-1 of the T1 landing, preserved here).
            // (1) Size gate first (a clear message before selection); (2) lineup selection, which fails
            // loud on a starter slot with no eligible player (KD-L3); (3) bounds gate on the SELECTED
            // consumed records (not a fixed prefix — the plan chooses which 18 are consumed).
            ValidateSquadSize(0, homeSquad);
            ValidateSquadSize(1, awaySquad);
            LineupPlan homePlan = LineupSelector.Select(homeSquad, MatchEngineConstants.STAGE0_FORMATION);
            LineupPlan awayPlan = LineupSelector.Select(awaySquad, MatchEngineConstants.STAGE0_FORMATION);
            ValidateSelectedRecords(0, homeSquad, in homePlan);
            ValidateSelectedRecords(1, awaySquad, in awayPlan);
            ApplySquad(0, homeSquad, in homePlan);
            ApplySquad(1, awaySquad, in awayPlan);
            // #27 T3: record which roster each team loaded (the identity half of restore fidelity),
            // set only after both squads validated-and-applied so a refused call leaves the reference
            // at the sentinel (validate-before-write, matching the AR-1 M-1 both-squads rule above).
            _rosterClubId[0] = homeSquad.ClubId;
            _rosterClubId[1] = awaySquad.ClubId;
        }

        /// <summary>Fail-loud gate on one squad's size — enough players for the starters + bench (see <see cref="ConfigureSquads"/>).</summary>
        private static void ValidateSquadSize(int teamId, TacticalDirector.PlayerDatabase.Squad squad)
        {
            int needed = MatchEngineConstants.PLAYERS_PER_TEAM + MatchEngineConstants.SUBSTITUTES_PER_TEAM;
            if (squad.Count < needed)
            {
                throw new System.ArgumentException(
                    $"ConfigureSquads: team {teamId} squad has {squad.Count} players; "
                    + $"need at least {needed} (starters + bench).");
            }
        }

        /// <summary>Fail-loud bounds gate on every SELECTED consumed record — the plan chooses which
        /// 18 of the roster are consumed, so validation follows the selection, not a fixed prefix
        /// (see <see cref="ConfigureSquads"/>).</summary>
        private static void ValidateSelectedRecords(
            int teamId, TacticalDirector.PlayerDatabase.Squad squad, in LineupPlan plan)
        {
            for (int k = 0; k < plan.StarterLocalIndices.Length; k++)
            {
                int local = plan.StarterLocalIndices[k];
                ValidateCanonicalRecord(teamId, local, squad.GetPlayer(local).Attributes);
            }
            for (int b = 0; b < plan.BenchLocalIndices.Length; b++)
            {
                int local = plan.BenchLocalIndices[b];
                ValidateCanonicalRecord(teamId, local, squad.GetPlayer(local).Attributes);
            }
        }

        /// <summary>Applies one validated squad to one team's on-pitch + bench slots through the
        /// selected lineup (see <see cref="ConfigureSquads"/>). The per-slot goalkeeper flags come
        /// from the selection (KD-L4), replacing the boot <c>k == 0</c> seed.</summary>
        private void ApplySquad(
            int teamId, TacticalDirector.PlayerDatabase.Squad squad, in LineupPlan plan)
        {
            for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
            {
                int i     = teamId * MatchEngineConstants.PLAYERS_PER_TEAM + k;
                int local = plan.StarterLocalIndices[k];
                _canonicalAttrs[i] = squad.GetPlayer(local).Attributes;
                _attrs[i]          = PlayerAttributeProjection.ToAgentMovement(in _canonicalAttrs[i]);
                _dtAttrs[i]        = PlayerAttributeProjection.ToDecisionTree(in _canonicalAttrs[i], teamId);
                _perceptionAttrs[i] = PlayerAttributeProjection.ToPerception(
                    in _canonicalAttrs[i], teamId, _perceptionAttrs[i].IsHalfTurned);
                _isGoalkeeper[i]   = plan.StarterIsGoalkeeper[k];
            }
            for (int b = 0; b < MatchEngineConstants.SUBSTITUTES_PER_TEAM; b++)
            {
                int local = plan.BenchLocalIndices[b];
                _benchCanonicalAttrs[teamId][b] = squad.GetPlayer(local).Attributes;
                _benchAttrs[teamId][b] =
                    PlayerAttributeProjection.ToAgentMovement(in _benchCanonicalAttrs[teamId][b]);
                _benchIsGoalkeeper[teamId][b] = plan.BenchIsGoalkeeper[b];
            }
        }

        /// <summary>
        /// Fail-loud bounds gate on a consumed canonical record: all 31 fields in [1,20],
        /// WeakFootRating in [1,5] (boot-time call — the ToArray() allocation is off the hot path).
        /// </summary>
        private static void ValidateCanonicalRecord(
            int teamId, int localIndex, TacticalDirector.PlayerDatabase.PlayerAttributes attributes)
        {
            int[] values = attributes.ToArray();
            for (int f = 0; f < values.Length; f++)
            {
                if (values[f] < TacticalDirector.PlayerDatabase.PlayerDatabaseConstants.ATTRIBUTE_MIN
                    || values[f] > TacticalDirector.PlayerDatabase.PlayerDatabaseConstants.ATTRIBUTE_MAX)
                {
                    throw new System.ArgumentException(
                        $"ConfigureSquads: team {teamId} squad player {localIndex} attribute ordinal {f} "
                        + $"= {values[f]} is outside [1,20].");
                }
            }
            if (attributes.WeakFootRating < TacticalDirector.PlayerDatabase.PlayerDatabaseConstants.WEAK_FOOT_MIN
                || attributes.WeakFootRating > TacticalDirector.PlayerDatabase.PlayerDatabaseConstants.WEAK_FOOT_MAX)
            {
                throw new System.ArgumentException(
                    $"ConfigureSquads: team {teamId} squad player {localIndex} WeakFootRating "
                    + $"= {attributes.WeakFootRating} is outside [1,5].");
            }
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
            GuardTeamId(teamId);
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
                CurrentPresetOrdinal = _presetCatalogue.BalancedOrdinal,
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
            if (presetOrdinal >= _presetCatalogue.Count)
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

        /// <summary>Test-only seam (snapshot-deserialize KD-8, v17): advances the match-flow.card-severity
        /// RNG stream cursor without issuing a card, so a test can prove the serialized RngStreamState cursor
        /// (RngCursor + ActionOrdinal) is in the snapshot digest preimage. Reads the boot-registered stream,
        /// overwrites only the two mutable cursor fields, and restores it via
        /// <see cref="DeterministicRngService.RestoreStream"/> — the values the writer serializes.</summary>
        internal void TestOnly_SetCardSeverityStreamCursor(ulong rngCursor, ulong actionOrdinal)
        {
            RngStreamState s = _rng.GetStreamState(_cardSeverityStreamIndex);
            s.RngCursor     = rngCursor;
            s.ActionOrdinal = actionOrdinal;
            _rng.RestoreStream(_cardSeverityStreamIndex, in s);
        }

        /// <summary>Test-only seam (gk-heading-engine-integration Phase 2, v18): advances the
        /// goalkeeper.mechanics RNG stream cursor without drawing, so a test can prove the serialized v18
        /// GK/Heading block reaches the snapshot digest preimage (the analogue of
        /// <see cref="TestOnly_SetCardSeverityStreamCursor"/>).</summary>
        internal void TestOnly_SetGoalkeeperStreamCursor(ulong rngCursor, ulong actionOrdinal)
        {
            RngStreamState s = _rng.GetStreamState(_goalkeeperStreamIndex);
            s.RngCursor     = rngCursor;
            s.ActionOrdinal = actionOrdinal;
            _rng.RestoreStream(_goalkeeperStreamIndex, in s);
        }

        /// <summary>Test-only: the last settled possession holder (v14; −1 = no agent has held yet).</summary>
        internal int TestOnly_LastHolderAgentId => _lastHolderAgentId;

        /// <summary>Test-only: an agent's yellow-card count (design note §3; v15).</summary>
        internal byte TestOnly_YellowCards(int agentId) => _yellowCards[agentId];

        /// <summary>Test-only: an agent's sent-off flag (design note §3; v15).</summary>
        internal bool TestOnly_IsSentOff(int agentId) => _isSentOff[agentId];

        /// <summary>Test-only: the global foul-detection cooldown remaining (design note §3; v15).</summary>
        internal int TestOnly_FoulCooldownRemaining => _foulCooldownRemaining;

        /// <summary>Test-only: an agent's active bench slot, −1 = original starter (design note §6; v15).</summary>
        internal int TestOnly_ActiveBenchSlot(int agentId) => _activeBenchSlot[agentId];

        /// <summary>Test-only: a team's bench-slot goalkeeper flag (re-projected at restore per Phase 2 /
        /// KD-3; a boot-constant NOT serialized, so a substitution after restore relies on it being
        /// re-derived).</summary>
        internal bool TestOnly_BenchIsGoalkeeper(int teamId, int benchIndex) => _benchIsGoalkeeper[teamId][benchIndex];

        /// <summary>Test-only: a team's substitutions-used count (design note §6; v15).</summary>
        internal int TestOnly_SubstitutionsUsed(int teamId) => _substitutionsUsed[teamId];

        /// <summary>Test-only: a team's roster reference — the configured <c>Squad.ClubId</c>, or
        /// <see cref="MatchEngineConstants.NO_ROSTER_CLUB_ID"/> if no squad was configured (#27 T3; v16).</summary>
        internal int TestOnly_RosterClubId(int teamId) => _rosterClubId[teamId];

        /// <summary>Test-only: the half-time-fired flag (design note §7; v15).</summary>
        internal bool TestOnly_SecondHalfStarted => _secondHalfStarted;

        /// <summary>Test-only: the full-time-fired (gameplay-freeze) flag (design note §7; v15).</summary>
        internal bool TestOnly_MatchEnded => _matchEnded;

        /// <summary>Test-only seam: runs the match-flow clock check exactly as RunInputPhase does, at
        /// an arbitrary <paramref name="tick"/> (mirrors the <c>TestOnly_RunManagerDecisionPoints</c>
        /// explicit-tick pattern), so half-time/full-time behaviour is testable without running the
        /// real ~162 000 / ~324 000 ticks.</summary>
        internal void TestOnly_CheckMatchFlowTransitions(long tick) => CheckMatchFlowTransitions(tick);

        /// <summary>Test-only seam: sets an agent's team id directly (offside/foul fixture construction;
        /// pairs with the existing <c>TestOnly_SetAgent</c>, which sets position/facing/velocity via a
        /// full <see cref="AgentState"/> — e.g. <c>AgentState.CreateAtPosition</c>).</summary>
        internal void TestOnly_SetTeamId(int agentId, int teamId) => _teamIds[agentId] = teamId;

        /// <summary>Test-only seam: sets an agent's sent-off flag directly, bypassing the RNG-driven
        /// card path (fixture construction for the offside/mechanics-AI exclusion tests).</summary>
        internal void TestOnly_SetIsSentOff(int agentId, bool isSentOff) => _isSentOff[agentId] = isSentOff;

        /// <summary>Test-only seam: configures a bench slot's attributes directly (substitution fixture
        /// construction — the production source is the Stage-0 in-code default per design note §6).</summary>
        internal void TestOnly_SetBenchSlot(int teamId, int benchIndex, bool isGoalkeeper)
        {
            _benchIsGoalkeeper[teamId][benchIndex] = isGoalkeeper;
        }

        /// <summary>Test-only seam: runs the offside check exactly as the <c>RunFirstTouch</c> Controlled
        /// case does, for an arbitrary <paramref name="toucher"/>, without needing to engineer a full
        /// first-touch reception through ball/agent physics. Returns true iff a violation was called
        /// (and applied — ball/possession already stomped by the time this returns).</summary>
        internal bool TestOnly_EvaluateAndApplyOffside(int toucher) => EvaluateAndApplyOffside(toucher);

        /// <summary>Test-only seam: attaches an observer that receives every agent-agent / agent-ball
        /// collision event the <see cref="MatchFlowCollisionConsumer"/> sees, before any of its foul
        /// gates. Exists for the §5.Z.9 foul-rate balance measurement — the force distribution the
        /// <c>FoulImpactForceThresholdN</c> gate sits on cannot otherwise be observed, because the
        /// collision system accepts exactly one consumer and that consumer is private to this class.
        /// Pass null to detach. Purely observational: an observer cannot influence the match.</summary>
        internal void TestOnly_SetCollisionObserver(ICollisionEventConsumer observer) =>
            _collisionObserver = observer;

        /// <summary>
        /// Test-only: a contact force (N) at which the referee-call probability saturates at 1, so a
        /// candidate carrying it is whistled with certainty whatever the `[GT]` values are retuned to.
        /// Deliberately far above the ~2400 N a real collision can produce — this is a test constant,
        /// not a physical one, and it exists so that a retune of <c>FoulCallProbability</c> cannot
        /// silently turn "this test injects a foul" into "this test injects a coin flip".
        /// </summary>
        internal const float CertainFoulForceN = 1e9f;

        /// <summary>Test-only seam: injects a foul candidate exactly as <see cref="MatchFlowCollisionConsumer"/>
        /// would, bypassing the need to engineer a real FROM_BEHIND high-force agent-agent collision.
        /// Call before <see cref="RunTick"/> — the value survives into that tick's <c>RunResolvePhase</c>
        /// (which resets it only via <c>ApplyFoulIfCaptured</c> consuming it, never pre-emptively) and is
        /// applied in the normal Resolve-phase place (design note §3 test plan).
        ///
        /// <paramref name="forceN"/> defaults to <see cref="CertainFoulForceN"/>, so an injected candidate
        /// IS a foul unless a test deliberately asks otherwise — which is what every caller predating the
        /// referee-call probability (`foul-discipline-balance-design.md` KD-F1) means by injecting one.</summary>
        internal void TestOnly_InjectFoulCandidate(int offender, int victim, float forceN = CertainFoulForceN)
        {
            _foulCandidateFound    = true;
            _foulCandidateOffender = offender;
            _foulCandidateVictim   = victim;
            _foulCandidateForceN   = forceN;
        }

        /// <summary>Test-only: the pure referee-call probability, for locking the KD-F1 shape directly.</summary>
        internal static float TestOnly_FoulCallProbability(float forceN) => ComputeFoulCallProbability(forceN);

        /// <summary>Test-only: the live foul-candidate consumer, so the KD-F4 strongest-wins capture rule
        /// can be driven with synthetic collision events instead of engineered physics.</summary>
        internal ICollisionEventConsumer TestOnly_FoulCandidateConsumer => _eventConsumer;

        /// <summary>Test-only: the captured candidate's contact force (N); 0 when none is captured.</summary>
        internal float TestOnly_FoulCandidateForceN => _foulCandidateFound ? _foulCandidateForceN : 0f;

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

        /// <summary>The boot match seed (the RNG seed this engine was constructed with). The world-state
        /// payload does not carry it (it is a boot constant, not cross-tick state — snapshot-deserialize
        /// KD-7), so an on-disk save persists it alongside the payload and feeds it back to
        /// <see cref="RestoreFromSnapshot"/>. Consumed by <c>MatchSaveManager</c>.</summary>
        public ulong MatchSeed => _matchSeed;

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
        /// Number of Tier A/B event records the engine drained on the tick most recently completed —
        /// the KD-7 read-only ledger tap Match Analytics #37 §4.3 consumes.
        ///
        /// <para><b>Scoped to the last completed tick.</b> Each <see cref="RunTick"/> overwrites the
        /// capture, so a client that wants every record must read the tap once per tick (#37 §3.5's
        /// lossless-consumption contract, which <c>MatchAnalyticsAggregator.ObserveTick</c> enforces
        /// on its own side by refusing a non-consecutive tick).</para>
        ///
        /// <para>Before the first tick this is 0.</para>
        /// </summary>
        public int TickLedgerCount => _tickLedger.Count;

        /// <summary>
        /// Appendix A event-type ordinal of tap record <paramref name="index"/>, in the canonical
        /// FM-017-002 order the snapshot digest also used. Compare against
        /// <c>EventRegistry.GetOrdinal&lt;T&gt;()</c> to decide which record type to read.
        /// </summary>
        public byte TickLedgerOrdinal(int index) => _tickLedger.OrdinalAt(index);

        /// <summary>
        /// Reads tap record <paramref name="index"/> back as a value copy of <typeparamref name="T"/>.
        /// Branch on <see cref="TickLedgerOrdinal"/> first; a type wider than the captured record
        /// fails loud rather than reading past it.
        /// </summary>
        public T TickLedgerRecord<T>(int index) where T : struct => _tickLedger.ReadAt<T>(index);

        /// <summary>Home team's (team 0) current goal count.</summary>
        public int HomeScore => _goals[0];

        /// <summary>Away team's (team 1) current goal count.</summary>
        public int AwayScore => _goals[1];

        /// <summary>True once full time has fired (see <see cref="CheckMatchFlowTransitions"/>); AI/Physics/Resolve are frozen but the tick/snapshot loop keeps advancing.</summary>
        public bool MatchEnded => _matchEnded;

        // ── P1 richer observation frame (interactive-unity-client-design.md §5-P1) ─────
        // Read-only value copies in the same shape as AgentTeamId / AgentIsGoalkeeper above
        // (KD-P1-1): the engine exposes scalars, and the presentation layer does the aggregating.
        // Nothing below adds state to the engine except the two within-tick restart fields
        // (KD-P1-3), so there is no SNAPSHOT_SCHEMA_VERSION change on this surface.

        /// <summary>Yellow cards currently held by roster <paramref name="index"/> (0 or 1 — a second
        /// yellow is promoted to a sending-off, and a substitution resets the slot's count).</summary>
        public int AgentYellowCards(int index)
        {
            GuardRosterIndex(index);
            return _yellowCards[index];
        }

        /// <summary>True when roster <paramref name="index"/> has been sent off. A sent-off agent stays a
        /// physical body (collision/perception) but is excluded from every participation surface.</summary>
        public bool AgentIsSentOff(int index)
        {
            GuardRosterIndex(index);
            return _isSentOff[index];
        }

        /// <summary>The bench slot currently occupying pitch slot <paramref name="index"/>, or −1 when the
        /// original starter is still on. A non-negative value is what a View draws a "substituted on"
        /// marker from.</summary>
        public int AgentBenchSlot(int index)
        {
            GuardRosterIndex(index);
            return _activeBenchSlot[index];
        }

        /// <summary>Substitutions <paramref name="teamId"/> has used, in
        /// [0, <c>MAX_SUBSTITUTIONS_PER_TEAM</c>].</summary>
        public int SubstitutionsUsed(int teamId)
        {
            GuardTeamId(teamId);
            return _substitutionsUsed[teamId];
        }

        /// <summary>
        /// Which period of the match the clock is in (KD-P1-2). DERIVED per call from the two
        /// transition flags <c>CheckMatchFlowTransitions</c> already owns and already serializes — no
        /// new state, nothing added to the snapshot.
        /// <para>It reports which transitions have <b>fired</b> rather than re-deriving them from
        /// <see cref="CurrentTick"/> against <c>HALF_TIME_BOUNDARY_TICK</c>. That keeps the boundary rule
        /// in exactly one place (a second copy of it is the parallel-surface trap), and it means the
        /// reported period can never disagree with what the engine actually did — including after a
        /// restore, since both flags round-trip through the payload.</para>
        /// <para>Note there is no <c>HalfTime</c> value: the Stage-0 halves model has no interval
        /// (FR-TP-019) — the ball is reset at the boundary and play continues on the next tick.</para>
        /// </summary>
        public MatchPeriod CurrentPeriod
        {
            get
            {
                if (_matchEnded)        { return MatchPeriod.FullTime; }
                if (_secondHalfStarted) { return MatchPeriod.SecondHalf; }
                return MatchPeriod.FirstHalf;
            }
        }

        /// <summary>
        /// The restart applied during the tick just run, or <see cref="RestartCue.None"/> (KD-P1-3).
        /// <para><b>This is a WITHIN-TICK value</b>, cleared at the top of the next tick's Input phase —
        /// the same lifecycle as <see cref="DidAiPhaseRunLastTick"/>. It is deliberately not latched
        /// here: cross-tick memory of the last restart is the presentation layer's job
        /// (<c>LiveMatchStreamer</c>), which keeps this off the snapshot entirely. A consumer that
        /// samples less often than every tick will miss restarts, which is exactly why the streamer
        /// latches on every tick and serves the latched value.</para>
        /// <para>The boot kickoff is applied before the first tick and so is never reported here.
        /// Tick 0 at 0–0 is unambiguously kickoff without a cue.</para>
        /// </summary>
        public RestartCue RestartAppliedThisTick => _restartAppliedThisTick;

        /// <summary>Team (0/1) awarded the restart reported by <see cref="RestartAppliedThisTick"/>, or
        /// <see cref="MatchEngineConstants.NO_RESTART_TEAM"/> (−1) when that is
        /// <see cref="RestartCue.None"/>.</summary>
        public int RestartAwardedTeam => _restartAwardedTeamThisTick;

        /// <summary>Public-surface team-id guard (parallel to <see cref="GuardRosterIndex"/>). Shared by
        /// <see cref="SetTeamTactic"/>, <see cref="SubstitutePlayer"/>, <see cref="ConfigureManager"/> and
        /// <see cref="SubstitutionsUsed"/> — extracted when P1 would otherwise have added a fourth
        /// verbatim copy of the same three lines. Message text is unchanged from those copies.</summary>
        private static void GuardTeamId(int teamId)
        {
            if (teamId < 0 || teamId >= MatchEngineConstants.TEAM_COUNT)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(teamId), teamId, "teamId must be 0 (home) or 1 (away).");
            }
        }

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

        /// <summary>Test-only: cumulative post/crossbar strikes this match (shot-speed design
        /// KD-6 — diagnostic observation, not serialized, zero after a restore by design).</summary>
        internal int TestOnly_WoodworkStrikes => _woodworkStrikes;

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

        /// <summary>Test-only seam: a slot's canonical #27 player record (T1 wiring assertions).</summary>
        internal TacticalDirector.PlayerDatabase.PlayerAttributes TestOnly_CanonicalAttributes(int index) =>
            _canonicalAttrs[index];

        /// <summary>Test-only seam: a slot's projected #2 locomotion attributes (T1 wiring assertions).</summary>
        internal PlayerAttributes TestOnly_MovementAttributes(int index) => _attrs[index];

        /// <summary>Test-only seam: a slot's projected #8 DecisionTree attributes (T1 wiring assertions).</summary>
        internal DtAgentAttributes TestOnly_DtAttributes(int index) => _dtAttrs[index];

        /// <summary>Test-only seam: a slot's projected #7 Perception attributes (T1 wiring assertions).</summary>
        internal PerceptionAgentAttributes TestOnly_PerceptionAttributes(int index) => _perceptionAttrs[index];

        /// <summary>Test-only seam: a slot's live #5 pass-attribute projection (BuildPassAttributes).</summary>
        internal PassAgentAttributes TestOnly_PassAttributes(int index) => BuildPassAttributes(index);

        /// <summary>Test-only seam: a slot's live #6 shot-attribute projection (BuildShotAttributes).</summary>
        internal ShotAgentAttributes TestOnly_ShotAttributes(int index) => BuildShotAttributes(index);

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

        /// <summary>Test-only (§5.Z.15): ticks the current goalkeeper has held the ball. The stall this
        /// rule closes was exactly this counter running unbounded, so it is the value worth asserting —
        /// observing possession alone cannot tell "the rule fired" from "an opponent took it".</summary>
        internal int TestOnly_GkHoldTicks => _gkHoldTicks;

        /// <summary>Test-only (§5.Z.15): remaining ticks the just-released keeper may not re-collect.</summary>
        internal int TestOnly_GkReleaseCooldownRemaining => _gkReleaseCooldownRemaining;

        /// <summary>
        /// Test-only (§5.Z.15): runs the six-second rule once, exactly as the Resolve phase does. The rule
        /// is a STALL BACKSTOP — measured, healthy play has a keeper distribute after ~54 ticks, well
        /// inside Law 12's 360 — so a composed run never reaches the release branch and cannot lock it.
        /// This seam drives that branch directly rather than leaving it untested, which is the
        /// never-compiled-surface trap this project has hit repeatedly.
        /// </summary>
        internal void TestOnly_RunGoalkeeperReleaseRule() => EnforceGoalkeeperReleaseRule();

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

        /// <summary>Test-only: the action type the agent's DecisionTree last selected. Lets a composed
        /// scenario assert what the AI is actually DOING with the ball, not merely that it decided.</summary>
        internal ActionType TestOnly_DtLastActionType(int agentId) => _decisionTrees[agentId].LastAction.Type;

        /// <summary>Test-only: whether the agent's routed TacticalContext designates it this team's
        /// loose-ball collector (§5.Z Phase H KD-H5 / ERR-008-014).</summary>
        internal bool TestOnly_LooseBallCollector(int agentId) =>
            _tacticalContexts[agentId].LooseBallCollector;

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

        /// <summary>Returns a DURABLE deep copy of the current snapshot header (the orchestrator's live
        /// header is reused each tick, so a save must snapshot it). Pairs with
        /// <see cref="CaptureDurablePayload"/> to form the (header, payload) save artifact that
        /// <see cref="RestoreFromSnapshot"/> consumes and that <c>MatchSaveManager</c> writes to disk.
        /// Not on the hot path (a save is a host action, not per-tick), so the copy allocation is fine.</summary>
        internal SnapshotHeader CaptureDurableHeader()
        {
            SnapshotHeader live = _orchestrator.CurrentHeader;
            SnapshotHeader copy = new SnapshotHeader
            {
                SchemaVersion = live.SchemaVersion,
                DigestVersion = live.DigestVersion,
                Tick          = live.Tick,
                Fingerprint   = live.Fingerprint,
                Cursor        = live.Cursor,
            };
            Array.Copy(live.PrevSnapshotDigest,    0, copy.PrevSnapshotDigest,    0, DeterministicSimConstants.SHA256_BYTES);
            Array.Copy(live.CurrentSnapshotDigest, 0, copy.CurrentSnapshotDigest, 0, DeterministicSimConstants.SHA256_BYTES);
            return copy;
        }

        /// <summary>Returns a DURABLE deep copy of the current snapshot payload (the orchestrator's live
        /// payload is reused each tick). Pairs with <see cref="CaptureDurableHeader"/>. Not on the hot
        /// path (a save is a host action, not per-tick), so the copy allocation is fine.</summary>
        internal SnapshotPayload CaptureDurablePayload()
        {
            SnapshotPayload live = _orchestrator.CurrentPayload;
            SnapshotPayload copy = new SnapshotPayload();
            Array.Copy(live.PayloadBytes, 0, copy.PayloadBytes, 0, live.BytesWritten);
            copy.BytesWritten = live.BytesWritten;
            return copy;
        }

        // ── GK (#11) / Heading (#10) engine integration — public + test surface (design §1.2a / §7) ──

        /// <summary>Opts this engine into the GK (#11) / Heading (#10) wiring
        /// (gk-heading-engine-integration-design.md, Phase 1 / KD-11). While OFF (the default) the engine
        /// is byte-identical to a pre-wiring engine — the orchestrators are constructed but never driven,
        /// their RNG streams never drawn, no save/header intent committed. Turning it ON drives both
        /// orchestrators and fires the §4 Stage-0 triggers seeded from the projections. Since Phase 2 the
        /// GK/Heading cross-tick state is serialized at SNAPSHOT_SCHEMA_VERSION 18, so a flag-on engine is
        /// both deterministic FORWARD and snapshot-safe (save/restore round-trips deterministically).
        /// Intended to be set once before ticking (a host activation, not a per-tick toggle).</summary>
        public void EnableGkHeading()
        {
            _gkHeadingEnabled = true;
        }

        /// <summary>
        /// Turns Goalkeeper Mechanics (#11) / Heading Mechanics (#10) OFF for this engine. Since §5.Z.15
        /// the default is ON, so this exists for tests that need the pre-integration behaviour and for a
        /// host that wants it — the inverse of <see cref="EnableGkHeading"/>, and like it, intended to be
        /// called once before ticking rather than toggled per tick.
        /// </summary>
        public void DisableGkHeading()
        {
            _gkHeadingEnabled = false;
        }

        /// <summary>Test-only (§7 projection proof): the <see cref="GoalkeeperAgentAttributes"/> the engine
        /// last handed to <c>CommitSaveIntent</c> (the live consumer of
        /// <see cref="PlayerAttributeProjection.ToGoalkeeper"/>), or <c>null</c> if no save has been
        /// committed since boot.</summary>
        internal GoalkeeperAgentAttributes? TestOnly_LastCommittedSaveAttrs =>
            _lastSaveAttrsValid ? _lastCommittedSaveAttrs : (GoalkeeperAgentAttributes?)null;

        /// <summary>Test-only (ERR-008-013): the per-episode save commit latch for a team's keeper
        /// (<c>_saveCommittedForGk</c>, serialized at v18). True once <see cref="HostSaveDispatch.CommitSave"/>
        /// has committed this episode; cleared in <c>RunMechanicsAI</c> once the ball is no longer save-armed,
        /// so a fresh shot re-arms and re-commits. Lets a test observe the arm → commit → clear → re-commit
        /// episode cycle.</summary>
        internal bool TestOnly_SaveCommittedForGk(int teamId) => _saveCommittedForGk[teamId];

        /// <summary>Test-only (§7 projection proof): the <see cref="HeadingAgentAttributes"/> the engine
        /// last handed to <c>CommitIntent</c> (the live consumer of
        /// <see cref="PlayerAttributeProjection.ToHeading"/>), or <c>null</c> if no header has been
        /// committed since boot.</summary>
        internal HeadingAgentAttributes? TestOnly_LastCommittedHeaderAttrs =>
            _lastHeaderAttrsValid ? _lastCommittedHeaderAttrs : (HeadingAgentAttributes?)null;

        /// <summary>Test-only: whether the GK/Heading opt-in wiring is enabled on this engine.</summary>
        internal bool TestOnly_GkHeadingEnabled => _gkHeadingEnabled;

        /// <summary>Test-only: the goalkeeper orchestrator's cross-tick state, for the §5.Z.17 save-pipeline
        /// diagnostic. Reads through the SAME public <c>CaptureState</c> seam the v19 snapshot writer uses,
        /// so the instrument observes exactly the state the digest serializes — an instrument reading a
        /// parallel surface could disagree with what the engine actually persists.</summary>
        internal TacticalDirector.GoalkeeperMechanics.GoalkeeperTickState TestOnly_GoalkeeperState =>
            _goalkeeper.CaptureState();

        /// <summary>Test-only (§7): force the ball into a loose, given position/velocity so a §4 trigger's
        /// world-state gate can be exercised deterministically without a full match developing the geometry
        /// naturally. Clears possession (loose ball).</summary>
        internal void TestOnly_ForceBallLoose(Vector3 position, Vector3 velocity)
        {
            _ball.Position = position;
            _ball.Velocity = velocity;
            _possessingAgentId = MatchEngineConstants.NO_POSSESSION;
        }

        /// <summary>Test-only (§7): run the GK/Heading 10 Hz tactical drive (baselines + state machine +
        /// §4 triggers) directly, bypassing the stride gate, so a forced ball geometry can be turned into a
        /// committed intent in one deterministic step. No-op unless the wiring is enabled.</summary>
        internal void TestOnly_DriveGkHeadingTactical() => DriveGkHeadingTactical();

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

            // P1 KD-P1-3: same lifecycle — a restart is reported only for the tick it was applied on.
            _restartAppliedThisTick     = RestartCue.None;
            _restartAwardedTeamThisTick = MatchEngineConstants.NO_RESTART_TEAM;

            // MatchClock.Advance() has already run inside RunTick, so CurrentTick is the tick
            // being processed (design note §2.4).
            EventBus.BeginTick((uint)_clock.CurrentTick);
            EventBus.BeginPhase(PhaseId.Input);

            // Match-flow completion (design note §7): half-time ends-swap / full-time freeze, checked
            // every tick (not stride-gated) so the transition fires on the exact boundary tick
            // regardless of AI stride alignment.
            CheckMatchFlowTransitions((long)_clock.CurrentTick);
        }

        /// <summary>
        /// Match-flow clock transitions (design note §7): checked every tick (not stride-gated) so a
        /// transition fires on the exact boundary tick. Half-time (once, guarded by
        /// <see cref="_secondHalfStarted"/>): resets the ball to the centre spot, clears possession,
        /// and publishes <see cref="MatchPhaseChangedEvent"/>(SecondHalf) — a real, visible transition
        /// marker. AR-4 (design note v0.4): does NOT reposition agents or flip the attack-direction
        /// convention — <c>team 0 attacks +X</c> is hardcoded across goal detection, offside, and every
        /// Mechanics-AI frame mapping, so a full ends-swap is a documented Stage-1+ deferral, not
        /// attempted here. Full-time (once, guarded by <see cref="_matchEnded"/>): publishes
        /// <see cref="MatchPhaseChangedEvent"/>(FullTime) — the freeze itself is enforced by the
        /// per-phase <see cref="_matchEnded"/> guards in RunPhysicsPhase/RunResolvePhase/RunAiPhase.
        /// Neither transition pauses real time (the sim has no wall-clock) — both are instantaneous
        /// tick-boundary events, consistent with every other restart in this note.
        /// </summary>
        private void CheckMatchFlowTransitions(long tick)
        {
            if (!_secondHalfStarted && tick >= MatchEngineConstants.HALF_TIME_BOUNDARY_TICK)
            {
                _secondHalfStarted = true;
                // §5.Z Phase H: the second-half kickoff is taken by the team that did NOT kick off the
                // first half (Law 8), i.e. the away team — the boot kickoff is awarded to team 0.
                ApplyRestart(
                    new Vector2(MatchEngineConstants.KickoffBallXM, MatchEngineConstants.KickoffBallYM),
                    awardedTeam: MatchEngineConstants.SECOND_HALF_KICKOFF_TEAM,
                    cue: RestartCue.KickOff);

                var evt = new MatchPhaseChangedEvent(newPhase: 0, homeScore: _goals[0], awayScore: _goals[1]);
                EventBus.Publish(in evt);
            }

            // Deliberately NOT an else-if: a real per-tick increment can never cross both boundaries
            // in the same call (HALF_TIME_BOUNDARY_TICK != MATCH_TICKS_TOTAL), but a test-only direct
            // jump straight to MATCH_TICKS_TOTAL on a fresh engine must still fire both transitions.
            if (!_matchEnded && tick >= MatchEngineConstants.MATCH_TICKS_TOTAL)
            {
                _matchEnded = true;

                var evt = new MatchPhaseChangedEvent(newPhase: 1, homeScore: _goals[0], awayScore: _goals[1]);
                EventBus.Publish(in evt);
            }
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
                        goalDiff, ticksRemaining, MatchEngineConstants.MATCH_TICKS_TOTAL, _presetCatalogue);
                }
            }
        }

        private void RunAiPhase()
        {
            _aiPhaseRanThisTick = true;
            _aiPhaseRunCount++;

            // Match-flow completion (design note §7): freeze all gameplay decisions after full time.
            // Placed after the observation counters (so stride-cadence tests are unaffected) and
            // before any tactic commit / mechanics AI / decision dispatch.
            if (_matchEnded)
            {
                return;
            }

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

            // GK (#11) / Heading (#10) 10 Hz tactical drive (design §3.4): advance the GK state machine and
            // fire the §4 save/header triggers (committed at the tactical tick, KD-17). No-op unless the
            // opt-in flag is set (KD-11). Placed after the mechanics AI so the orchestrators read the same
            // tick's settled positions.
            DriveGkHeadingTactical();

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

                // Match-flow completion (design note §3): a sent-off agent is never dispatched a new
                // action (RunPhysicsPhase separately forces them to a stop each tick). Perception/dwell
                // above still runs unconditionally — harmless, and keeps this the only orchestration-
                // level skip needed.
                if (_isSentOff[i])
                {
                    continue;
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

                // #27 T1: both AI attribute snapshots are projections of the canonical record
                // (all-neutral at boot ⇒ byte-identical to the pre-T1 CreateDefault seeds).
                // TeamId is match-scoped runtime identity; IsHalfTurned is runtime body stance
                // (false at boot, exactly the pre-T1 CreateDefault value) — KD-P4.
                _perceptionAttrs[i] = PlayerAttributeProjection.ToPerception(
                    in _canonicalAttrs[i], teamId, isHalfTurned: false);

                _dtAttrs[i]          = PlayerAttributeProjection.ToDecisionTree(in _canonicalAttrs[i], teamId);
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

                // §5.Z Phase H (ERR-008-014): designate this team's loose-ball collector for the stride —
                // exactly one agent, or none. See SelectLooseBallCollector for why the host owns this.
                int collector = SelectLooseBallCollector(t);

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

                    // #11/#10 (ERR-008-013): the DT-emitted-SAVE gate. Under the opt-in flag, the
                    // threatened keeper's context carries SaveAvailable = true, and the DecisionTree
                    // emits SAVE as its sole off-ball option this stride. Geometry from the proven pure
                    // GkHeadingIntentSource (the former heuristic's own gate). The per-episode commit
                    // latch is cleared here when the ball is no longer armed (the former
                    // TryCommitSaveIntents cleared it the same way), so a new shot re-arms and re-commits.
                    // Flag-off / non-keeper leaves SaveAvailable at the Stage0Default false ⇒ the off-ball
                    // branch is byte-identical to pre-integration.
                    if (_gkHeadingEnabled && _isGoalkeeper[i] && !_isSentOff[i])
                    {
                        bool loose = _possessingAgentId == MatchEngineConstants.NO_POSSESSION;
                        bool armed = GkHeadingIntentSource.SaveArmed(
                            t, in _ball.Position, in _ball.Velocity, loose);
                        ctx.SaveAvailable = armed;
                        if (!armed)
                        {
                            // One owner of "the episode is over". This latch and #11's own
                            // _saveIntentActive used to have DIFFERENT lifetimes — #11 cleared only when
                            // a dive resolved, this one clears as soon as the geometry lapses — so a
                            // threat that armed, committed and then cleared before the keeper dived left
                            // #11 armed indefinitely and fired at the next Anticipate: a dive at nothing.
                            // Disarming both here keeps them from disagreeing (ClearSaveIntent is a no-op
                            // while a dive is already in flight, so a live attempt still runs to its own
                            // resolution).
                            _saveCommittedForGk[t] = false;
                            _goalkeeper.ClearSaveIntent(t);
                        }
                    }

                    // §5.Z Phase H (ERR-008-014): the loose-ball collect gate. Exactly one agent per team
                    // carries it, so the DecisionTree emits the collect as its sole off-ball option this
                    // stride. No loose resting ball ⇒ collector is NO_POSSESSION ⇒ every context keeps the
                    // Stage0Default false, and the off-ball branch is byte-identical to pre-Phase-H.
                    ctx.LooseBallCollector = i == collector;

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
                    isActive:     !_isSentOff[i],       // match-flow completion: red-carded agents excluded
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
                    // #27 T1 (projection design §3.5a / KD-P9): canonical FirstTouchAbility, every
                    // agent (no GK gate — KD-P5). Neutral record ⇒ 10 = the pre-T1 STAGE0 seed.
                    FirstTouchAttribute = PlayerAttributeProjection.FirstTouchAbility(in _canonicalAttrs[i]),
                    LastTouchQuality    = 1f,   // perfect touch ⇒ no Stage-0 BadTouch trigger
                    PostTouchBallSpeed  = 0f,
                    IsGoalkeeper        = _isGoalkeeper[i],
                    HasBall             = i == owner,
                    IsActive            = !_isSentOff[i], // match-flow completion: red-carded agents excluded
                    BaselineSlot        = isOwn ? _positioning[team].GetFormationSlot(i)
                                                : MirrorPitchIfAway(team, _agents[i].Position),
                    Line                = isOwn ? _positioning[team].GetLine(i) : LineId.Midfield,
                    // Cheap-item addition (new §7.12): cover-shadow curve attributes, sourced from
                    // the same _dtAttrs the Decision Tree already reads — since #27 T1 these are
                    // canonical-record projections (real values under a configured squad; the
                    // no-squad default stays all-neutral), so they flow transitively with no
                    // separate projection row (projection design §1 "derived consumers").
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
                    IsActive            = !_isSentOff[i], // match-flow completion: red-carded agents excluded
                    IsGoalkeeper        = _isGoalkeeper[i],
                    HasBall             = i == owner,
                    BaselineSlot        = isOwn ? _positioning[team].GetFormationSlot(i)
                                                : MirrorPitchIfAway(team, _agents[i].Position),
                    Line                = isOwn ? _positioning[team].GetLine(i) : LineId.Midfield,
                    PressRole           = _pressing[team].GetAssignment(i).Role,
                    // #27 T1 (projection design §3.5a): canonical FirstTouchAbility (see the
                    // Pressing fill note). Stage-0 approximation: the true attribute stands in
                    // for a perceived estimate, exactly as the neutral placeholder did.
                    PerceivedFirstTouch = PlayerAttributeProjection.FirstTouchAbility(in _canonicalAttrs[i]),
                };
            }
        }

        /// <summary>
        /// Fills team <paramref name="team"/>'s reused <see cref="AttackingSnapshot"/> (Phase D D2b). All 22
        /// agents are carried in the acting team's canonical attack-+X frame, so the team attack angle is 0.
        /// Stamina is the live fatigue (1 − AerobicPool); pace / dribbling are the canonical record's
        /// Pace/Dribbling normalised ÷ ATTRIBUTE_MAX since #27 T1 (projection design §3.8 / KD-P3 —
        /// neutral record ⇒ 0.5, the pre-T1 placeholder; still not consumed by the Stage-0 RUNNER
        /// algorithm, §2.3).
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
                    isActive:     !_isSentOff[i], // match-flow completion: red-carded agents excluded
                    // #27 T1 (projection design §3.8 / KD-P3): the one pre-normalized target —
                    // canonical Pace/Dribbling ÷ ATTRIBUTE_MAX, so the neutral record ⇒ 0.5 =
                    // the pre-T1 STAGE0_NEUTRAL_NORMALIZED seed.
                    pace:         PlayerAttributeProjection.ToNormalized(_canonicalAttrs[i].Pace),
                    stamina:      1f - _agents[i].AerobicPool,
                    dribbling:    PlayerAttributeProjection.ToNormalized(_canonicalAttrs[i].Dribbling));
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
        // ── GK (#11) / Heading (#10) drive + Stage-0 triggers (design §3.3/§3.4/§4, Phase 1) ──
        // All of the below runs ONLY under _gkHeadingEnabled (KD-11). The two RNG adapters draw from the
        // single per-subsystem stream via these helpers, converting the ulong draw to the [0,1) / Gaussian
        // shapes the stubs use (KD-3). Fail-loud on a reservation-order violation (the card-severity posture).

        private float DrawStreamFloat01(int streamIndex)
        {
            if (_rng.Reserve(streamIndex, 1) != 0)
            {
                throw new InvalidOperationException(
                    "MatchEngine GK/Heading RNG: a reservation is already open (draw-site misuse).");
            }
            if (_rng.DrawReserved(streamIndex, 0, out ulong draw) != 0)
            {
                _rng.CloseReservation(streamIndex);
                throw new InvalidOperationException(
                    "MatchEngine GK/Heading RNG: draw failed — corrupt reservation state (internal invariant).");
            }
            _rng.CloseReservation(streamIndex);
            // Top 24 bits → [0, 1); the HeadingRngServiceStub conversion (24-bit mantissa window).
            return (float)((draw >> 40) * (1.0 / (1UL << 24)));
        }

        private float DrawStreamGaussian(int streamIndex)
        {
            // Box-Muller (the stub's method): two uniforms → one standard-normal sample.
            float u1 = DrawStreamFloat01(streamIndex);
            float u2 = DrawStreamFloat01(streamIndex);
            if (u1 < HeadingMechanicsConstants.RNG_GUARD_EPSILON)
            {
                u1 = HeadingMechanicsConstants.RNG_GUARD_EPSILON;
            }
            return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
        }

        /// <summary>
        /// ERR-011-004: routes a just-struck shot to the keeper it is struck AT, opening #11's §3.2
        /// reaction window. Called once per agent per Resolve tick, immediately after that agent's shot
        /// executor advanced, so a shot whose CONTACT ran this frame is seen this frame.
        /// </summary>
        /// <param name="shooterId">Agent whose shot executor just advanced.</param>
        /// <param name="frameNumber">Current 60 Hz physics frame.</param>
        /// <param name="matchTimeMs">Current match time (ms) from kickoff.</param>
        private void NotifyKeeperOfShot(int shooterId, int frameNumber, float matchTimeMs)
        {
            ShotResult r = _shotExecutors[shooterId].LastResult;
            if (r.Outcome != ShotOutcome.Completed || r.ContactFrame != frameNumber)
            {
                return;
            }

            // The keeper under threat is the one defending the goal the shooter attacks — i.e. the
            // OTHER team's. Derived from the shooter's team rather than from ball direction, so a
            // miscued shot still starts the right keeper's clock (and never the shooter's own).
            int shooterTeam = _teamIds[shooterId];
            if (shooterTeam < 0 || shooterTeam >= GoalkeeperConstants.MaxGkAgents)
            {
                return;
            }

            int defendingTeam = 1 - shooterTeam;

            // Keeper index == team id (KD-1, MaxGkAgents == TEAM_COUNT == 2), the same mapping
            // HostSaveDispatch uses. A team with no keeper on the pitch (sent off) has none to notify.
            if (_gkAgentIds[defendingTeam] < 0)
            {
                return;
            }

            GoalkeeperAgentAttributes attrs = PlayerAttributeProjection.ToGoalkeeper(
                in _canonicalAttrs[_gkAgentIds[defendingTeam]], defendingTeam, fatigue: 0f);

            _goalkeeper.OnShotExecutedEvent(
                defendingTeam, matchTimeMs, _ball.Velocity.magnitude, attrs);
        }

        /// <summary>Refills <see cref="_gkAgentIds"/> (keeper index == team id → that team's GK agent index,
        /// −1 if none) from the current <c>_isGoalkeeper</c>/<c>_teamIds</c>. Called at boot and at the top of
        /// each drive so ConfigureSquads (which reassigns <c>_isGoalkeeper</c>) and GK substitutions are
        /// tracked. Cheap (SQUAD_SIZE loop) and only runs under the flag after boot.</summary>
        private void RefreshGkAgentIds()
        {
            for (int k = 0; k < _gkAgentIds.Length; k++)
            {
                _gkAgentIds[k] = -1;
            }
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (_isGoalkeeper[i])
                {
                    int t = _teamIds[i];
                    if (t >= 0 && t < _gkAgentIds.Length)
                    {
                        _gkAgentIds[t] = i;
                    }
                }
            }
        }

        /// <summary>GK/Heading 10 Hz tactical drive (design §3.4): anchor keeper baselines, advance the GK
        /// state machine, then fire the §4.2 header trigger (committed at the tactical tick per KD-17). The
        /// SAVE decision is NO LONGER fired here — it is a DT-emitted action (ERR-008-013): RunMechanicsAI
        /// sets <see cref="TacticalContext.SaveAvailable"/> for the threatened keeper and the DecisionTree
        /// emits SAVE → <see cref="HostSaveDispatch"/> (which maps agent→team directly, independent of
        /// <c>_gkAgentIds</c>). Called from <see cref="RunAiPhase"/> on the stride, after RunMechanicsAI;
        /// no-op unless <c>_gkHeadingEnabled</c>.</summary>
        private void DriveGkHeadingTactical()
        {
            if (!_gkHeadingEnabled)
            {
                return;
            }
            RefreshGkAgentIds();
            for (int k = 0; k < _gkAgentIds.Length; k++)
            {
                int agentId = _gkAgentIds[k];
                if (agentId >= 0 && !_isSentOff[agentId])
                {
                    _goalkeeper.UpdateBaselineSlot(k, _agents[agentId].Position);
                }
            }
            _goalkeeper.TacticalTick((int)_clock.CurrentTick, _agents, _ball, _gkAgentIds);
            TryCommitHeaderIntents();
        }

        /// <summary>GK/Heading 60 Hz physics drive (design §3.4): advance both orchestrators against the
        /// current ball. Runs in <see cref="RunPhysicsPhase"/> (before the Resolve-phase goal check), so a
        /// committed save/header can deflect the ball before goal detection. No-op unless the flag is on.</summary>
        private void DriveGkHeadingPhysics()
        {
            if (!_gkHeadingEnabled)
            {
                return;
            }
            RefreshGkAgentIds();
            int   frameNumber = (int)_clock.CurrentTick;
            float matchTimeS  = _clock.CurrentMatchTimeSeconds;
            float matchTimeMs = _clock.CurrentMatchTimeMs;
            _heading.Update(_agents, _ball, frameNumber, matchTimeS);
            _goalkeeper.Update(frameNumber, matchTimeMs, _agents, _ball, _gkAgentIds);
        }

        /// <summary>§4.1 save decision — REMOVED (ERR-008-013). The keeper's save is now a DT-emitted
        /// <c>ActionType.SAVE</c>: <see cref="RunMechanicsAI"/> sets <see cref="TacticalContext.SaveAvailable"/>
        /// (from <see cref="GkHeadingIntentSource.SaveArmed"/>) for the threatened keeper under the flag, the
        /// DecisionTree emits SAVE as its sole off-ball option, and <see cref="HostSaveDispatch"/> applies the
        /// per-episode latch + <see cref="PlayerAttributeProjection.ToGoalkeeper"/> projection + commit.
        /// RunMechanicsAI clears the latch when the ball is no longer armed. The former heuristic is gone.</summary>

        /// <summary>§4.2 header trigger: the single nearest active outfield agent within head range of a
        /// loose airborne ball commits a header seeded from <see cref="PlayerAttributeProjection.ToHeading"/>
        /// (the projection's live consumer). At most one per airborne episode per agent (KD-7). Records the
        /// committed attrs for the <c>TestOnly_</c> projection proof.</summary>
        private void TryCommitHeaderIntents()
        {
            Vector3 bp = _ball.Position;
            bool airborneLoose = bp.z >= MatchEngineConstants.HeaderTriggerMinBallHeightM
                                 && _possessingAgentId == MatchEngineConstants.NO_POSSESSION;

            // §4.2 candidate selection (pure): nearest active outfielder to a loose airborne ball, or −1.
            int nearest = GkHeadingIntentSource.NearestHeaderCandidate(
                in bp, airborneLoose, _agents, _isGoalkeeper, _isSentOff, MatchEngineConstants.SQUAD_SIZE);

            // Clear the per-agent episode latch for every agent that is not the current nearest candidate.
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (i != nearest)
                {
                    _headerCommittedThisEpisode[i] = false;
                }
            }
            if (nearest < 0 || _headerCommittedThisEpisode[nearest])
            {
                return;
            }

            int t = _teamIds[nearest];
            // Opponent goal: team 0 attacks +X (goal at PITCH_LENGTH_M), team 1 attacks −X (goal at 0).
            float oppGoalX = t == 0 ? MatchEngineConstants.PITCH_LENGTH_M : 0f;
            var intent = new HeaderIntent
            {
                PowerIntent          = MatchEngineConstants.HeaderTriggerPowerIntent,
                ContactPointIntent   = Vector2.zero,
                TargetIntent         = new Vector3(oppGoalX, MatchEngineConstants.PITCH_WIDTH_M / 2f, 0f),
                AttemptCommittedTick = (int)_clock.CurrentTacticalTick,
                SetPieceContext      = SetPieceContext.OpenPlay,
            };
            HeadingAgentAttributes attrs =
                PlayerAttributeProjection.ToHeading(in _canonicalAttrs[nearest], t, fatigue: 0f);
            _heading.CommitIntent(nearest, intent, attrs, _ball, (int)_clock.CurrentTick);
            _lastCommittedHeaderAttrs = attrs;
            _lastHeaderAttrsValid = true;
            _headerCommittedThisEpisode[nearest] = true;
        }

        private void RunPhysicsPhase()
        {
            EventBus.BeginPhase(PhaseId.Physics);
            if (_matchEnded)
            {
                return;
            }

            // Fixed 60 Hz timestep in SECONDS (design note §3 / step B1); never wall-clock.
            float dt = DeterministicSimConstants.FrameSeconds;

            // ERR-001-005 / shot-speed design KD-6 — capture the ball's pre-integration position.
            // WITHIN-TICK state (the RestartAppliedThisTick class): written here at the top of every
            // Physics phase, consumed by the swept frame test below and the Resolve-phase
            // crossing-point adjudication the same tick, never read across ticks — so it is NOT
            // serialized and needs no exclusion-proof entry beyond this note.
            _prevTickBallPosition = _ball.Position;

            // Ball: a null logger drops matchTime (the logger is its sole consumer — design note B2),
            // so no allocation and no non-load-bearing time enters the digest. No wind at Stage 0.
            BallPhysicsCore.UpdateBallPhysics(
                ref _ball, dt, SurfaceType.GrassDry, Vector3.zero, logger: null, matchTime: 0f);

            // ERR-001-005 / KD-4 — the goal frame is physical: a ball whose movement segment meets
            // a post or the crossbar rebounds (restitution + spin retention) instead of flying
            // through a 0.12 m cylinder the discrete per-tick test tunnels past at shot speeds.
            // Runs immediately after the ball integrates so the Resolve-phase boundary check sees
            // the post-rebound, in-play position. The counter is diagnostic observation only
            // (the AiPhaseRunCount class — not serialized, not digest-load-bearing).
            if (BallCollision.ApplySweptGoalFrameCollision(ref _ball, _prevTickBallPosition))
            {
                _woodworkStrikes++;
            }

            // Match-flow completion (design note §3): a sent-off agent is frozen — forced to a stop
            // command every tick, overriding whatever the last AI dispatch left held (they will never
            // receive a new one, per the RunAiPhase skip above). Decelerates to rest and stays there.
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (_isSentOff[i])
                {
                    _commands[i] = MovementCommand.Stop(_agents[i].Position);
                }
            }

            // Agents: the batch seam skips goalkeepers (Stage 0 — GK locomotion is Spec #11).
            // currentTime is the seconds-domain match clock (step B1), as OscillationGuard compares
            // elapsed transition times against WindowSeconds.
            _movement.UpdateAllAgents(
                _agents, _attrs, _perfs, _commands, _isGoalkeeper,
                _isCollisionKnockdown, _collisionForces, dt, _clock.CurrentMatchTimeSeconds);

            // GK LOCOMOTION (§5.Z.15). The batch seam above skips goalkeepers, and until now nothing
            // moved them: boot placement WAS the keeper's position for the whole ninety minutes
            // (§5.Z.10 found both goals unguarded for exactly this reason and fixed only the spawn).
            // A keeper that cannot close down, narrow an angle or come for a cross is a large part of
            // a goal rate ~10x football's.
            //
            // The command they need already exists: the Decision Tree runs for keepers like anyone
            // else and dispatches MOVE_TO_POSITION at the #12-composed GK slot, which
            // AnchorCalculator.ComputeGkSlot makes a function of ball position. Only the integration
            // was missing, so this drives each keeper through the SAME per-agent Update the batch
            // seam calls — no new locomotion model, and #2's documented batch contract is untouched.
            // Spec #11 keeps what is genuinely its own: the dive, the save and the claim.
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (!_isGoalkeeper[i])
                {
                    continue;
                }

                _movement.Update(
                    ref _agents[i], in _attrs[i], in _perfs[i], in _commands[i],
                    dt, _clock.CurrentMatchTimeSeconds,
                    _isCollisionKnockdown[i], _collisionForces[i]);
            }

            // GK (#11) / Heading (#10) 60 Hz drive (design §3.4). After the ball + agents are integrated so
            // the orchestrators see the current world, and — since this is the Physics phase — strictly
            // before the Resolve-phase goal check (a committed save/header can deflect the ball first).
            // No-op unless _gkHeadingEnabled (KD-11 — the default engine is byte-identical).
            DriveGkHeadingPhysics();
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
            if (_matchEnded)
            {
                return;
            }

            // Match-flow completion (design note §6, AR-5): flush any SubstitutePlayer calls made
            // since the last tick — CurrentPhase is now Resolve, the registered producer phase.
            PublishPendingSubstitutions();

            int   frameNumber = (int)_clock.CurrentTick;          // narrows safely at Stage 0 (~414 days @ 60 Hz)
            float matchTime   = _clock.CurrentMatchTimeSeconds;

            // Match-flow completion (design note §3): the global foul-detection cooldown decrements
            // once per tick, before the collision step that would otherwise re-arm a foul this tick.
            if (_foulCooldownRemaining > 0)
            {
                _foulCooldownRemaining--;
            }

            // C2 — collision first. Reuses _attrs (PlayerAttributes[]); writes _isCollisionKnockdown /
            // _collisionForces (consumed by movement at tick N+1). stumbleOut is discarded (B4 — not a
            // Stage-0 movement input). Self-seeds its own RNG from _matchSeed ^ frameNumber internally.
            // NOTE: UpdateCollisions processes ALL 22 agents incl. goalkeepers, whereas Physics-phase
            // UpdateAllAgents skips GKs (Stage 0 — GK locomotion is #11). A GK can therefore be
            // displaced by a collision that movement never re-integrates; benign at Stage 0 (kickoff
            // spread admits no GK collisions) and inherent to the two seams, recorded here for Phase D.
            // _foulCandidateFound needs no reset here: ApplyFoulIfCaptured (called every tick, right
            // below) always clears it when true, so it is already false entering UpdateCollisions —
            // an invariant a TestOnly-injected candidate can rely on too (design note §3 test plan).
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

            // Match-flow completion (design note §3): apply the (at most one) foul candidate the
            // consumer just captured — RNG-drawn severity, card issuance, sent-off, and a free kick.
            ApplyFoulIfCaptured();

            // C3 — advance any in-flight executors. Idle executors no-op; only a pass/shot started via
            // the TestOnly_ seam (or, from Phase D, the AI dispatcher) is mid-lifecycle here.
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                _passExecutors[i].Update(matchTime, frameNumber, ref _ball);
                _shotExecutors[i].Update(matchTime, frameNumber, ref _ball);

                // ERR-011-004 — tell the defending keeper a shot has been struck.
                // GoalkeeperMechanics.OnShotExecutedEvent is the §3.2 reaction-pipeline entry point, and
                // it had ZERO production callers: _shotDetectedTickMs stayed 0, so the per-frame update
                // that writes ReactionWindowAchieved (gated on _shotDetectedTickMs > 0) never ran, and
                // reactionWindowAchieved was permanently 0. Since §3.5.1 blends
                //     quality = alpha*rawHandling + (1 - alpha)*reactionWindowAchieved,  alpha = 0.70,
                // that capped quality at 0.70*rawHandling — measured ceiling 0.630 for a PERFECT keeper
                // (Handling 20, zero noise, exact contact point) against CatchThreshold 0.78. A catch was
                // ARITHMETICALLY unreachable regardless of positioning, reach or dive accuracy.
                //
                // The contact frame is the trigger, not the windup: §3.2.1 dates perception from the ball
                // being struck. LastResult is only Completed once ApplyKick has run, and ContactFrame
                // pins the frame it ran on, so the equality fires exactly once per shot.
                //
                // MILLISECONDS, deliberately not the `matchTime` seconds the executors take. #11's whole
                // reaction pipeline is ms (`_shotDetectedTickMs` is compared against the
                // `_clock.CurrentMatchTimeMs` that DriveGkHeadingPhysics feeds `_goalkeeper.Update`), so
                // stamping it in seconds here would leave `elapsed` ~1000x too large, drive the §3.2.3
                // late branch to a clamped 0, and reproduce EXACTLY the permanently-zero
                // reactionWindowAchieved this fix exists to remove — a defect that looks fixed and is not.
                if (_gkHeadingEnabled)
                {
                    NotifyKeeperOfShot(i, frameNumber, _clock.CurrentMatchTimeMs);
                }
            }

            // §5.Z Phase H (KD-H4 / ERR-008-015) — close the Decision Tree's PASS/SHOOT lifecycle.
            // §3.7.2 parks a tree in EXECUTING after a PASS or SHOOT dispatch and re-evaluates only on
            // NotifyActionComplete / NotifyInterrupt / a forced refresh — but NOTHING in production ever
            // called NotifyActionComplete, and the possession-changed consumer interrupts only the NEW
            // holder, never the passer. So an agent that passed (or whose PassExecutor.Execute was
            // REJECTED, which the dispatcher deliberately does not inspect per §3.5.2) was frozen in
            // EXECUTING for the remainder of the match: no further decisions, no further movement
            // commands, and — if it still held the ball — no way to release it. Composed, that stalled
            // the whole match a few minutes after kickoff even with possession bootstrapped.
            //
            // The composition root owns both the trees and the executors, so it is the only layer that can
            // observe the executor lifecycle ending. One rule covers completion AND rejection: a tree
            // waiting on an executor that is not running has nothing left to wait for. Checked after the
            // advance above so a windup that finished THIS tick is seen the same tick; a dispatch made
            // earlier in this tick's AI phase left its executor non-Idle synchronously (Execute enters
            // WINDUP), so a live pass is never cut short.
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (_decisionTrees[i].IsAwaitingExecutorCompletion
                    && _passExecutors[i].IsIdle
                    && _shotExecutors[i].IsIdle)
                {
                    _decisionTrees[i].NotifyActionComplete();
                }
            }

            // Engine substrate — restart check (goal / throw-in / corner / goal-kick). Runs AFTER the
            // executors (the ball's crossing position came from this tick's Physics phase, possibly
            // adjusted by collision) and BEFORE first touch, so a ball that has fully left the field
            // cannot be "received" by an agent standing in the out-of-bounds buffer. Every restart
            // places the ball and clears possession, so D3/C4 below see the restarted state.
            CheckRestartAndApply();

            // D3 — first touch. A loose, approaching, ground-level ball arriving within reach of an agent
            // is received here (a CONTROLLED touch gains possession; an INTERCEPTION flips it to the
            // opponent; a LOOSE_BALL / DEFLECTION redirects the ball but leaves it loose). Runs AFTER the
            // executors so the same-tick kick that releases possession is visible (the ball is loose), and
            // BEFORE C4 so MatchContext reflects any possession gained by the touch.
            RunFirstTouch();

            // §5.Z Phase H (KD-H3) — loose-ball pickup. A ball that has come to REST while loose is
            // otherwise unreachable: RunFirstTouch gate 3 refuses it (correctly — a still ball is not an
            // incoming receive), so without this the possession loop dies the first time a pass runs out
            // of momentum with nobody arriving, and the match falls straight back into the ERR-030-014
            // deadlock a few minutes after kickoff. Runs AFTER RunFirstTouch so a genuine reception always
            // wins; the two are disjoint by their speed gates in any case.
            RunLooseBallPickup();

            // §5.Z.15 — Law 12's six-second rule. Runs AFTER the two possession-granting paths so a
            // keeper that claims the ball this tick starts its count from this tick.
            EnforceGoalkeeperReleaseRule();

            // C4 — author MatchContext last, so it reflects this tick's settled possession (a CONTACT
            // kick above released possession, or a D3 first touch) and ball kinematics. Read by the next
            // AI tick (Phase D).
            UpdateMatchContext();

            // Engine substrate — record the last settled HOLDER (v14). Updated after C4 so it tracks the
            // same settled value MatchContext folds in; only ever overwritten by a real holder. In the
            // common case a goal follows the holder's own kick, so the GoalAwardedEvent credit names the
            // scorer — but this is the same last-settled-holder APPROXIMATION documented at the
            // RestartResolver seam (AR-7 L-1): deflections never update the tracker, so a goal reached
            // through an uncontrolled deflection chain credits the last settled holder, who may not have
            // kicked the scoring ball (and may even have been sent off since — every card path clears
            // possession via ApplyRestart without touching this tracker). Scoring-TEAM classification is
            // pure geometry and unaffected (AR-9/AR-4 doc alignment).
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
        /// lost the ball). Non-goal exits (design note §5, landed 2026-07-14) route through
        /// <see cref="RestartResolver"/> + <see cref="ApplyRestart"/> instead of being ignored — a
        /// throw-in/corner/goal-kick now places the ball and clears possession exactly like a goal
        /// does, and publishes a Tier A <see cref="RestartAwardedEvent"/> (ordinal 0x19). Deterministic
        /// and allocation-free.
        /// </summary>
        private void CheckRestartAndApply()
        {
            // AR-7 L-1 — documented Stage-0 approximation at this seam: "last touch" is derived from
            // the last settled HOLDER (_lastHolderAgentId), not the last physical toucher — a
            // deflection or uncontrolled touch never updates the tracker, so a ball deflected out
            // off a defender is still classified against the last possession (affecting the
            // throw-in/corner/goal-kick award direction and the corner-vs-goal-kick split inside
            // CheckBoundaries). Before any possession has ever settled the tracker is −1 and team 0
            // is assumed. A true last-toucher tracker is a Stage-1+ refinement (it needs a
            // physical-contact event this engine does not yet consume).
            int lastTouchTeam = _lastHolderAgentId >= 0 ? _teamIds[_lastHolderAgentId] : 0;
            // ERR-001-005 / shot-speed design KD-5: the segment overload adjudicates a goal-line
            // crossing at the interpolated crossing point rather than the detected position (up to
            // ~0.42 m past the plane at shot speeds — the band around the crossbar).
            (bool isOut, RestartType restart) =
                BallCollision.CheckBoundaries(_ball, _prevTickBallPosition, lastTouchTeam);
            if (!isOut || restart == RestartType.None)
            {
                return;
            }

            if (restart != RestartType.KickOff)
            {
                // Design note §5: throw-in / corner / goal-kick.
                Vector2 ballXY = new Vector2(_ball.Position.x, _ball.Position.y);
                (Vector2 position, int awardedTeam) = RestartResolver.Resolve(restart, ballXY, lastTouchTeam);
                ApplyRestart(position, awardedTeam, ToRestartCue(restart));

                var restartEvt = new RestartAwardedEvent(
                    restartKind: (byte)restart,
                    awardedTeam: (byte)awardedTeam,
                    location:    new Vector3(position.x, position.y, MatchEngineConstants.BALL_REST_HEIGHT_M));
                EventBus.Publish(in restartEvt);
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

            // Centre-spot restart: same construction as the kickoff boot state. Any stale holder is
            // dropped (a possessed-into-the-goal ball must not stay claimed by an agent now 50 m away;
            // the Phase E publisher below emits the transition), and §5.Z Phase H awards the kickoff to
            // the CONCEDING team per Law 8 — the side that did not score restarts.
            ApplyRestart(
                new Vector2(MatchEngineConstants.KickoffBallXM, MatchEngineConstants.KickoffBallYM),
                awardedTeam: 1 - scoringTeam,
                cue: RestartCue.KickOff);
        }

        /// <summary>
        /// P1 KD-P1-5 — maps the Ball Physics boundary-exit classification onto the presentation
        /// <see cref="RestartCue"/>. Total over the four non-<c>None</c> members; <c>None</c> is
        /// unreachable here (the caller returns early on it) and maps to <see cref="RestartCue.None"/>
        /// rather than throwing, since this is observation state and must never be able to abort a tick.
        /// </summary>
        private static RestartCue ToRestartCue(RestartType restart)
        {
            switch (restart)
            {
                case RestartType.KickOff:  return RestartCue.KickOff;
                case RestartType.ThrowIn:  return RestartCue.ThrowIn;
                case RestartType.GoalKick: return RestartCue.GoalKick;
                case RestartType.Corner:   return RestartCue.Corner;
                default:
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    // Observation code must never abort a tick, so this falls through rather than
                    // throwing — but a RestartType member added later would otherwise be reported to a
                    // View as "no restart" in total silence (AR-1 L-3).
                    UnityEngine.Debug.LogWarning(
                        "[MatchEngine] ToRestartCue: unmapped RestartType " + restart + " reported as RestartCue.None.");
#endif
                    return RestartCue.None;
            }
        }

        /// <summary>
        /// Shared restart primitive (design note §5, extended by §5.Z Phase H): places the ball at
        /// <paramref name="position"/> at rest height, stationary, and AWARDS possession to a taker
        /// from <paramref name="awardedTeam"/>. Used by the kickoff, the goal restart, the throw-in/
        /// corner/goal-kick restarts, an offside violation, and a foul's awarded free kick — the same
        /// "stomp, don't undo" minimalism throughout (no ceremony; agents keep their positions).
        ///
        /// <para><b>Why the award exists (ERR-030-014 / §5.Z).</b> Before Phase H this primitive only
        /// CLEARED possession, and production had no other way to grant it: <see cref="RunFirstTouch"/>
        /// gate 3 refuses a touch unless the ball is already moving, and the ball is set in motion only
        /// by a pass/shot executor, which requires a possessor. No motion ⇒ no reception ⇒ no possession
        /// ⇒ no kick ⇒ no motion — every production match was a 90-minute 0–0 deadlock. Awarding the
        /// restart is the one grant that does not presuppose prior motion, so it breaks the loop at its
        /// only entry point; everything downstream (PASS/SHOOT dispatch, the executors, first touch,
        /// offside, fouls, goal detection) already existed.</para>
        ///
        /// <para><b>KD-H2 — assignment, not imparted velocity.</b> The restart grants possession and
        /// leaves the ball at rest, so <c>ApplyKick</c> stays the SOLE producer of ball motion (a second
        /// motion source would have to be serialized, digest-reasoned about, and kept coherent with the
        /// executors' possession recheck). The taker's own AI decides what to do with it on the next
        /// tactical stride.</para>
        ///
        /// <para><b>Stage-0 approximation.</b> The taker is not walked to the ball — consistent with the
        /// agents-keep-positions minimalism this primitive already documented — so a taker may be some
        /// metres from the restart spot when they play it. A real restart ceremony (walk-to-ball, wall
        /// set-up, the taker's two-touch restriction) is Stage 1+.</para>
        ///
        /// <para><b>P1 KD-P1-4 — every restart declares its kind.</b> <paramref name="cue"/> is recorded
        /// for the current tick only (<see cref="RestartAppliedThisTick"/>), so the presentation layer can
        /// caption the restart. It has no gameplay effect whatsoever: nothing in the engine reads it back,
        /// and the field is cleared at the top of the next tick. Requiring it as a parameter rather than
        /// inferring it here is the same discipline KD-H1 applied to <paramref name="awardedTeam"/> — an
        /// untyped restart reaching this primitive would be reported to a View as whatever the previous
        /// restart was.</para>
        /// </summary>
        /// <param name="position">Restart position for the ball (pitch plane, m).</param>
        /// <param name="awardedTeam">Team id (0/1) awarded the restart; its nearest eligible agent takes it.</param>
        /// <param name="cue">What kind of restart this is. Observation only — never read by gameplay.</param>
        private void ApplyRestart(Vector2 position, int awardedTeam, RestartCue cue)
        {
            _ball = BallState.CreateAtPosition(new Vector3(
                position.x, position.y, MatchEngineConstants.BALL_REST_HEIGHT_M));
            _possessingAgentId = SelectRestartTaker(position, awardedTeam);

            // P1 KD-P1-3 — within-tick observation state; see the field declaration.
            _restartAppliedThisTick     = cue;
            _restartAwardedTeamThisTick = awardedTeam;
        }

        /// <summary>
        /// §5.Z Phase H (KD-H5 / ERR-008-014) — designates the ONE agent of <paramref name="teamId"/> who
        /// should go and collect a loose ball that has come to rest, or
        /// <see cref="MatchEngineConstants.NO_POSSESSION"/> when there is nothing to collect.
        ///
        /// <para>Conditions: the ball is loose, at ground level, and effectively stationary (below
        /// <see cref="MatchEngineConstants.FIRST_TOUCH_MIN_BALL_SPEED_M_S"/> — a MOVING loose ball is the
        /// ordinary Decision Tree §3.1.9 intercept, which needs no designation). The collector is then the
        /// team's nearest agent that is not sent off, ties to the lower roster index.</para>
        ///
        /// <para><b>Why the HOST designates.</b> Two reasons, one architectural and one load-bearing.
        /// Architecturally this is a team-level role assignment made from team state — the same class as
        /// Pressing AI (#13) picking one primary presser from the whole team snapshot — not a per-agent
        /// perception judgement. Load-bearing: only the host knows who is SENT OFF. An in-tree rule of the
        /// form "defer to the nearest teammate I can see" defers to red-carded players, who are never
        /// dispatched an action and therefore never move — measured, that re-created the deadlock outright,
        /// with the ball lying 4 m from a frozen sent-off agent while eleven teammates waited for it.</para>
        ///
        /// <para>Both teams designate independently, so a resting ball is contested by the nearest player
        /// from each side. Deterministic and allocation-free.</para>
        /// </summary>
        /// <summary>
        /// Laws of the Game, Law 12: a goalkeeper may not control the ball with the hands for more than
        /// six seconds. §5.Z.15 made the keeper a live agent that can WIN possession, and nothing in the
        /// engine could make it give the ball back up — #11's distribution is not engine-driven and the
        /// Decision Tree has no keeper-distribution action — so a keeper that claimed the ball held it
        /// for the rest of the match (measured: 33.5% of one second half, in one of four full matches).
        ///
        /// The release is deliberately the smallest thing that is both correct and football-shaped: clear
        /// possession, leaving the ball at rest at the keeper's feet, which is exactly the state
        /// <see cref="RunLooseBallPickup"/> already handles. A short cooldown bars that same keeper from
        /// being the collector, so the ball goes to an outfielder instead of looping straight back — the
        /// shape of a throw-out or a goal kick. No new physics and no invented distribution model: when
        /// #11's distribution is engine-driven it replaces this method's body, not its trigger.
        /// </summary>
        private void EnforceGoalkeeperReleaseRule()
        {
            if (_gkReleaseCooldownRemaining > 0)
            {
                _gkReleaseCooldownRemaining--;
                if (_gkReleaseCooldownRemaining == 0)
                {
                    _gkReleasedAgentId = MatchEngineConstants.NO_POSSESSION;
                }
            }

            int holder = _possessingAgentId;
            if (holder == MatchEngineConstants.NO_POSSESSION || !_isGoalkeeper[holder])
            {
                _gkHoldTicks = 0;
                return;
            }

            _gkHoldTicks++;
            if (_gkHoldTicks < MatchEngineConstants.GkMaxHoldTicks)
            {
                return;
            }

            _possessingAgentId          = MatchEngineConstants.NO_POSSESSION;
            _gkHoldTicks                = 0;
            _gkReleasedAgentId          = holder;
            _gkReleaseCooldownRemaining = MatchEngineConstants.GkReleaseCooldownTicks;
        }

        /// <summary>This team's goalkeeper, or <c>NO_POSSESSION</c> if it has none on the pitch.</summary>
        private int FirstGoalkeeperOnTeam(int teamId)
        {
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (_teamIds[i] == teamId && _isGoalkeeper[i])
                {
                    return i;
                }
            }
            return MatchEngineConstants.NO_POSSESSION;
        }

        private int SelectLooseBallCollector(int teamId)
        {
            // No _matchEnded term: RunAiPhase — the only path here — already returns before RunMechanicsAI
            // once the match has ended, exactly as RunResolvePhase guards RunLooseBallPickup. Keeping a
            // second, unreachable copy of that gate here would leave a reader guessing which one is real.
            if (_possessingAgentId != MatchEngineConstants.NO_POSSESSION)
            {
                return MatchEngineConstants.NO_POSSESSION;
            }

            float ballHeight = _ball.Position.z - FirstTouchConstants.BallRadius;
            if (ballHeight > FirstTouchConstants.GroundControlHeight)
            {
                return MatchEngineConstants.NO_POSSESSION;
            }

            Vector2 ballPosXY = new Vector2(_ball.Position.x, _ball.Position.y);
            Vector2 ballVelXY = new Vector2(_ball.Velocity.x, _ball.Velocity.y);
            float minSpeed = MatchEngineConstants.FIRST_TOUCH_MIN_BALL_SPEED_M_S;
            if (ballVelXY.sqrMagnitude >= minSpeed * minSpeed)
            {
                return MatchEngineConstants.NO_POSSESSION; // moving: the ordinary intercept path owns it
            }

            // §5.Z.15/16: the keeper is never the designated loose-ball collector. The collector is
            // "this team's nearest agent to the ball", and for a ball sitting in a team's own six-yard
            // box that is always its keeper — so the keeper collected, was released by the six-second
            // rule, was nearest again, and collected again. Measured, that loop held the ball for a
            // THIRD of one second half and the six-second cap alone barely dented it (33.5% → 33.4%),
            // because the defect is re-acquisition, not hold duration.
            //
            // A keeper claiming a ball that ARRIVES is untouched — that is First Touch #4 and #11's
            // save, and it is what a keeper is for. What is removed is the keeper being sent to fetch a
            // ball that has come to rest, which is not a thing keepers do and is the whole stall.
            int gkRestricted = FirstGoalkeeperOnTeam(teamId);

            // The selection is the identical predicate the restart taker uses — "this team's nearest agent
            // to a point, excluding anyone sent off, ties to the lower roster index" — so it is expressed
            // ONCE, in SelectRestartTaker. Two copies of a participation scan is how the _isSentOff
            // exclusion came to be added one surface at a time over four review rounds (AR-8 M-1 first
            // touch, AR-9 M-1 the foul interpretation); the next participation rule must not have to find
            // every clone.
            return SelectRestartTaker(ballPosXY, teamId, gkRestricted);
        }

        /// <summary>
        /// KD-H1 — picks the restart taker: the eligible agent of <paramref name="awardedTeam"/> nearest
        /// <paramref name="position"/>. Eligible = not sent off (a sent-off agent is not a participant —
        /// the same exclusion every other participation surface carries; see the <see cref="RunFirstTouch"/>
        /// receiver scan). Ties resolve to the lower roster index, matching the project's other proximity
        /// tie-breaks (DT §3.1.3.6, the first-touch receiver scan). Goalkeepers are deliberately NOT
        /// excluded: nearest-to-the-spot naturally gives the keeper a goal kick and an outfielder a corner,
        /// so one rule covers every restart type without a per-type table.
        ///
        /// Returns <see cref="MatchEngineConstants.NO_POSSESSION"/> if the awarded team has no eligible
        /// agent at all (every player sent off — a real match would be abandoned; here the ball simply
        /// stays loose rather than the engine throwing). Deterministic and allocation-free.
        ///
        /// <para>This is the project's single expression of "a team's nearest eligible agent to a point":
        /// <see cref="SelectLooseBallCollector"/> is its second caller (the collector is the same
        /// selection, made against the ball's resting position after that method's own gates). Any future
        /// participation rule — an injured agent, a taker serving a restart restriction — belongs HERE, in
        /// one place, and reaches both call sites.</para>
        /// </summary>
        /// <param name="excludeAgentId">Optional agent barred from selection (default: none). Used by the
        /// §5.Z.15 six-second release so the keeper that has just put the ball down cannot immediately
        /// collect it again. A restart never passes this — every agent is eligible to take one.</param>
        private int SelectRestartTaker(
            Vector2 position, int awardedTeam, int excludeAgentId = MatchEngineConstants.NO_POSSESSION)
        {
            int   taker  = MatchEngineConstants.NO_POSSESSION;
            float bestSq = float.MaxValue;

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (_teamIds[i] != awardedTeam || _isSentOff[i] || i == excludeAgentId)
                {
                    continue;
                }

                float distSq = (_agents[i].Position - position).sqrMagnitude;
                if (taker == MatchEngineConstants.NO_POSSESSION || distSq < bestSq)
                {
                    bestSq = distSq;
                    taker  = i;
                }
            }

            return taker;
        }

        /// <summary>
        /// Applies the (at most one) foul candidate <see cref="MatchFlowCollisionConsumer"/> captured
        /// this tick (design note §3): publishes <see cref="FoulCommittedEvent"/>, draws card severity
        /// from the <c>match-flow.card-severity</c> RNG stream, issues a card if the draw qualifies
        /// (second yellow promotes to red), sends the offender off on any red, re-arms the global
        /// cooldown, and awards a free kick to the victim's team at the foul location. No-op if no
        /// candidate was captured this tick, or if either participant is already sent off (AR-9
        /// M-1 — a sent-off agent cannot commit or win a foul; see the inline comment).
        /// </summary>
        private void ApplyFoulIfCaptured()
        {
            if (!_foulCandidateFound)
            {
                return;
            }
            _foulCandidateFound = false;

            int   offender = _foulCandidateOffender;
            int   victim   = _foulCandidateVictim;
            float forceN   = _foulCandidateForceN;

            // AR-9 M-1: a sent-off agent is not a participant in play — contact with (or by) one
            // cannot produce a foul, a card, or a restart. The physical collision itself still
            // resolves in the collision system (momentum exchange, fall/stumble — the documented
            // physical-presence minimalism); only the match-flow FOUL interpretation is discarded.
            // Pre-fix, a frozen red-carded agent standing in the path of play repeatedly "won"
            // free kicks (ApplyRestart teleported the ball to their feet) and drew cards against
            // opponents who ran into their back — for the rest of the match. Gated HERE (the
            // application site) rather than in MatchFlowCollisionConsumer: capture and application
            // happen in the same tick and cards are issued only here, so the timing is equivalent;
            // a single gate avoids the sibling-drift class (PM AR-7 M-1), and this site also covers
            // the TestOnly_InjectFoulCandidate seam. Cost: a discarded candidate still consumed the
            // tick's single capture slot, shadowing a same-tick genuine foul — negligible (at most
            // one 60 Hz tick's delay for an already-rare event), and doubly so under the KD-F4
            // strongest-wins rule, since a sent-off agent is held at rest by the forced stop and the
            // FROM_BEHIND classifier needs BOTH participants moving, so it rarely raises a candidate
            // at all. No cooldown is armed and no FoulCommittedEvent is published: a non-foul must
            // not suppress or announce anything.
            if (_isSentOff[offender] || _isSentOff[victim])
            {
                return;
            }

            // Referee judgement (foul-discipline-balance-design.md KD-F1/KD-F2). Reaching here means the
            // contact was hard, from behind, cross-team, and both participants are on the pitch — a
            // CANDIDATE, not yet a foul. Whether the whistle goes is a probability scaled by how hard the
            // contact was. The draw happens BEFORE any observable effect, so a wave-on leaves no trace:
            // no event, no card, no restart, and (KD-F3) no cooldown, since suppressing detection after a
            // no-call would silently swallow the genuine foul two ticks later.
            if (_rng.Reserve(_cardSeverityStreamIndex, 1) != 0)
            {
                throw new InvalidOperationException(
                    "MatchEngine.ApplyFoulIfCaptured: a card-severity reservation is already open (draw-site misuse).");
            }
            if (_rng.DrawReserved(_cardSeverityStreamIndex, 0, out ulong draw) != 0)
            {
                // Unreachable under the API contract (Reserve(1) above set DeclaredBudget = 1, so index
                // 0 is always in range); closing keeps the stream usable for the next caller, matching
                // the InteractionTextGenerator (#22) precedent for this exact defensive branch.
                _rng.CloseReservation(_cardSeverityStreamIndex);
                throw new InvalidOperationException(
                    "MatchEngine.ApplyFoulIfCaptured: card-severity draw failed — corrupt reservation state (internal invariant).");
            }
            _rng.CloseReservation(_cardSeverityStreamIndex);

            float u = (draw % 1_000_000UL) / 1_000_000f;

            float callProbability = ComputeFoulCallProbability(forceN);
            if (u >= callProbability)
            {
                return; // Waved on.
            }

            // KD-F2: the same draw selects the card. Conditional on a call, u is uniform on
            // [0, callProbability), so v = u / callProbability is uniform on [0,1) — the input
            // DetermineCardKind's bands are defined against. One draw, two decisions, no second stream
            // and no SNAPSHOT_SCHEMA_VERSION bump. callProbability > u >= 0 here, so it cannot be zero.
            float v = u / callProbability;

            Vector2 victimPos = _agents[victim].Position;
            Vector3 location  = new Vector3(victimPos.x, victimPos.y, 0f);

            var foulEvt = new FoulCommittedEvent(offender, victim, location, foulKind: (byte)ContactType.FROM_BEHIND);
            EventBus.Publish(in foulEvt);

            byte? drawnKind = DetermineCardKind(v);

            if (drawnKind.HasValue)
            {
                byte cardKind = ApplyCardAndCheckSentOff(offender, drawnKind.Value);
                var cardEvt = new CardIssuedEvent(offender, cardKind, foulOrdinal: 0xFFFF);
                EventBus.Publish(in cardEvt);
            }

            _foulCooldownRemaining = MatchEngineConstants.FoulCooldownTicks;

            // Free kick to the victim's team at the foul location (design note §3); §5.Z Phase H awards
            // the taker to that team.
            ApplyRestart(victimPos, awardedTeam: _teamIds[victim], cue: RestartCue.FreeKick);
        }

        /// <summary>
        /// Pure referee-call probability for a candidate contact of <paramref name="forceN"/> newtons
        /// (`foul-discipline-balance-design.md` KD-F1): <c>min(1, FoulCallProbability × F / threshold)</c>.
        /// At the threshold it equals <c>FoulCallProbability</c> and it rises linearly with force, so a
        /// harder challenge is likelier to be given while a hard contact is never automatically a foul —
        /// which the balance measurement showed it must not be, since the engine produces roughly
        /// seventeen qualifying cross-team from-behind contacts per second.
        ///
        /// Separated from the draw so the shape is directly testable, mirroring
        /// <see cref="DetermineCardKind"/>. Non-finite input maps to 0 (never call): the force comes from
        /// the collision system, which sanitises, so this is a fail-closed guard rather than a live path —
        /// and failing closed here means a missed foul, not a phantom one.
        /// </summary>
        private static float ComputeFoulCallProbability(float forceN)
        {
            if (!(forceN > 0f) || float.IsInfinity(forceN))
            {
                return 0f;
            }

            float scaled = MatchEngineConstants.FoulCallProbability
                           * (forceN / MatchEngineConstants.FoulImpactForceThresholdN);
            return scaled > 1f ? 1f : scaled;
        }

        /// <summary>
        /// Pure card-severity band lookup (design note §3), separated from the RNG draw itself so the
        /// boundary conditions are directly testable with an explicit <paramref name="u"/> (mirrors the
        /// project's pure-formula-over-hash-derived-input convention, e.g. <c>OffsideEvaluator.IsOffside</c>).
        /// <c>[0, RedCardProbability)</c> = straight red (1); <c>[RedCardProbability,
        /// RedCardProbability + YellowCardProbability)</c> = yellow (0); else null (no card).
        /// </summary>
        private static byte? DetermineCardKind(float u)
        {
            if (u < MatchEngineConstants.RedCardProbability)
            {
                return MatchEngineConstants.CARD_KIND_RED;
            }
            if (u < MatchEngineConstants.RedCardProbability + MatchEngineConstants.YellowCardProbability)
            {
                return MatchEngineConstants.CARD_KIND_YELLOW;
            }
            return null;
        }

        /// <summary>Test-only seam: the pure card-severity band lookup, directly testable at exact
        /// boundary values without needing to know a real RNG draw output (design note §3 test plan).</summary>
        internal static byte? TestOnly_DetermineCardKind(float u) => DetermineCardKind(u);

        /// <summary>
        /// Applies a drawn card kind (0=yellow, 1=red) to <paramref name="offender"/>: a first yellow
        /// just increments the count; a SECOND yellow promotes to card kind 2 (SecondYellow) and sends
        /// the agent off; a straight red sends the agent off immediately. Returns the ACTUAL card kind
        /// issued (0/1/2) for the published <see cref="CardIssuedEvent"/>. Separated from the RNG draw
        /// so the promotion logic is directly testable (design note §3 test plan).
        /// </summary>
        private byte ApplyCardAndCheckSentOff(int offender, byte drawnKind)
        {
            if (drawnKind == 0)
            {
                _yellowCards[offender]++;
                if (_yellowCards[offender] >= 2)
                {
                    _isSentOff[offender] = true;
                    return 2; // SecondYellow
                }
                return 0;
            }

            _isSentOff[offender] = true; // straight red
            return 1;
        }

        /// <summary>Test-only seam: the card-kind-to-effect resolution, directly testable without a real
        /// RNG draw (design note §3 test plan).</summary>
        internal byte TestOnly_ApplyCardAndCheckSentOff(int agentId, byte drawnKind) =>
            ApplyCardAndCheckSentOff(agentId, drawnKind);

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
                // §5.Z Phase H (KD-H4): do NOT interrupt a holder whose own pass/shot executor is still
                // in flight. Re-planning would hand the tree straight back to the pipeline, which can
                // re-select PASS/SHOOT and dispatch into an executor that is mid-lifecycle — the executor
                // correctly refuses ("Execute() called while shot in progress"), but the decision is
                // wasted and it logs an error every time. Once play actually developed, that fired
                // repeatedly: an agent shoots, the loose ball rebounds back to it within the same
                // FollowThrough, and possession-changed re-plans it into its own busy executor.
                // Deferring costs nothing — the executor's completion is observed in the very next
                // Resolve by the completion sweep in RunResolvePhase, which releases the tree to IDLE so
                // it re-plans on the following stride.
                bool executorInFlight = !_passExecutors[newHolder].IsIdle || !_shotExecutors[newHolder].IsIdle;
                if (!executorInFlight)
                {
                    _decisionTrees[newHolder].NotifyInterrupt();
                }

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
            // Incidental fix (match-flow completion, design note §7): _goals has existed since the
            // v14 engine-substrate goal detection landed, but this method still hardcoded 0-0. Reading
            // the real score here is a one-line correction of that pre-existing latent bug, not new
            // scope — no other change in this method.
            _matchContext.HomeScore        = _goals[0];
            _matchContext.AwayScore        = _goals[1];
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
                // AR-8 M-1: a sent-off agent no longer participates and must not receive the ball.
                // This was the ONE participation surface without the exclusion (AI dispatch, all four
                // Mechanics-AI snapshot fills, the physics forced-stop, and the offside line all have
                // it) — a ball rolling past the frozen agent handed them possession, which they could
                // never release (no AI dispatch ⇒ no kick), deadlocking play until half/full time.
                // They remain a PHYSICAL presence (collision, perception, pressure) — that is the
                // documented agents-keep-positions minimalism, distinct from participating in play.
                if (_isSentOff[i])
                {
                    continue;
                }
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
                {
                    // Design note §4 — Stage-0 offside (reception-time approximation). At this point
                    // _lastHolderAgentId still names the PREVIOUS tick's holder (the production
                    // writer runs later in RunResolvePhase, after UpdateMatchContext), so this is
                    // exactly "a genuine same-team pass reception, not an interception and not the
                    // same agent re-touching a loose dribble".
                    int newHolder = result.PossessingAgentID;
                    bool isPassReception = _lastHolderAgentId >= 0
                        && _teamIds[_lastHolderAgentId] == _teamIds[newHolder]
                        && _lastHolderAgentId != newHolder;

                    if (isPassReception && EvaluateAndApplyOffside(newHolder))
                    {
                        // Violation: the assignment below is skipped (not undone) and ApplyRestart
                        // already stomped ball/possession state (design note §4 point 3).
                        break;
                    }

                    _possessingAgentId = newHolder;
                    break;
                }
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
        /// Loose-ball pickup (design note §5.Z Phase H, KD-H3): the eligible agent standing nearest a
        /// loose ball that has come to REST claims it.
        ///
        /// <para>This is NOT a First Touch (#4) event and deliberately does not run that model: #4 scores
        /// the control of an INCOMING ball, and its control quality is a function of incoming velocity, so
        /// applying it at v ≈ 0 would be using the spec outside its domain. A player standing over a still
        /// ball simply has it. Keeping the two paths separate also leaves <see cref="RunFirstTouch"/> — and
        /// every #4 contract test — untouched.</para>
        ///
        /// <para>Gates, in order: the ball is loose; it is at ground level (a resting airborne ball is not
        /// a thing, but the height gate keeps this symmetric with first touch and correct if a future
        /// mechanic parks the ball mid-air); its planar speed is BELOW
        /// <see cref="MatchEngineConstants.FIRST_TOUCH_MIN_BALL_SPEED_M_S"/> — the exact complement of
        /// first-touch gate 3, so the two mechanics can never both fire on one ball; and some eligible
        /// (not sent off) agent is within <see cref="MatchEngineConstants.LooseBallPickupRadiusM"/>.
        /// The nearest such agent claims it, ties to the lower roster index — the same proximity
        /// tie-break as the first-touch receiver scan and <see cref="SelectRestartTaker"/>.</para>
        ///
        /// <para>No RNG, no allocation: a pure function of world state. There is deliberately no contest
        /// model — two opponents equidistant over a still ball resolve by roster index rather than by a
        /// duel. A real 50-50 (strength/aggression, possibly a foul) belongs with the Collision System
        /// #3 duel fan-out, which is Stage 1+.</para>
        /// </summary>
        private void RunLooseBallPickup()
        {
            if (_possessingAgentId != MatchEngineConstants.NO_POSSESSION)
            {
                return;
            }

            float ballHeight = _ball.Position.z - FirstTouchConstants.BallRadius;
            if (ballHeight > FirstTouchConstants.GroundControlHeight)
            {
                return;
            }

            Vector2 ballPosXY = new Vector2(_ball.Position.x, _ball.Position.y);
            Vector2 ballVelXY = new Vector2(_ball.Velocity.x, _ball.Velocity.y);
            float minSpeed = MatchEngineConstants.FIRST_TOUCH_MIN_BALL_SPEED_M_S;
            if (ballVelXY.sqrMagnitude >= minSpeed * minSpeed)
            {
                return; // still in motion — first touch owns this ball, not pickup.
            }

            float radiusSq = MatchEngineConstants.LooseBallPickupRadiusM
                           * MatchEngineConstants.LooseBallPickupRadiusM;
            int   claimer  = MatchEngineConstants.NO_POSSESSION;
            float bestSq   = radiusSq;

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (_isSentOff[i])
                {
                    continue;
                }

                float distSq = (_agents[i].Position - ballPosXY).sqrMagnitude;
                if (distSq > bestSq)
                {
                    continue;
                }
                // Inclusive boundary via the first-candidate clause, matching the first-touch scan.
                if (claimer == MatchEngineConstants.NO_POSSESSION || distSq < bestSq)
                {
                    bestSq  = distSq;
                    claimer = i;
                }
            }

            if (claimer != MatchEngineConstants.NO_POSSESSION)
            {
                _possessingAgentId = claimer;
            }
        }

        /// <summary>
        /// Stage-0 offside evaluation (design note §4) for a same-team pass reception by
        /// <paramref name="toucher"/>. Computes the OPPONENT team's offside line from live agent
        /// positions (<see cref="OffsideEvaluator.ComputeOffsideLineX"/>) and checks the toucher
        /// against it. On a violation: publishes a Tier A <see cref="OffsideCalledEvent"/> and awards
        /// an indirect free kick to the defending team at the toucher's position via
        /// <see cref="ApplyRestart"/> (no explicit "undo" of the touch — see the design note §4 point 3).
        /// Returns true iff a violation was called (the caller skips the possession assignment).
        /// </summary>
        private bool EvaluateAndApplyOffside(int toucher)
        {
            int toucherTeam    = _teamIds[toucher];
            int defendingTeam  = 1 - toucherTeam;
            float toucherX     = _agents[toucher].Position.x;

            float lineX = OffsideEvaluator.ComputeOffsideLineX(
                new ReadOnlySpan<AgentState>(_agents), new ReadOnlySpan<int>(_teamIds),
                new ReadOnlySpan<bool>(_isSentOff), defendingTeam, MatchEngineConstants.SQUAD_SIZE);

            if (!OffsideEvaluator.IsOffside(toucherX, toucherTeam, lineX))
            {
                return false;
            }

            Vector2 toucherPos = _agents[toucher].Position;
            var evt = new OffsideCalledEvent(
                offendingAgentId: toucher,
                team:             (byte)toucherTeam,
                location:         new Vector3(toucherPos.x, toucherPos.y, 0f));
            EventBus.Publish(in evt);

            // Indirect free kick to the DEFENDING team (§5.Z Phase H awards it a taker).
            ApplyRestart(toucherPos, awardedTeam: defendingTeam, cue: RestartCue.FreeKick);
            return true;
        }

        /// <summary>
        /// Assembles the <see cref="FirstTouchContext"/> for the receiving agent (Phase D D3). Player
        /// touch attributes (Technique / FirstTouchAbility) are canonical-record projections since
        /// #27 T1 (projection design §3.5a — the former Stage-0 neutral placeholders were the
        /// projection of the default record), the same sourcing the pass/shot
        /// adapters use. Pressure / nearest-opponent data come from a <c>PressureEvaluator</c> pass over
        /// the opposing team (filling <see cref="_opponentScratch"/>, zero alloc), and
        /// <see cref="OrientationDetector.IsHalfTurnOriented"/> supplies the half-turn flag against the
        /// incoming ball direction. The intended touch direction defaults to the agent's facing (no
        /// movement-target carrier at Stage 0; HasMovementTarget = false).
        /// </summary>
        /// <summary>
        /// The #4 §3.5 opponent-pressure scalar at an arbitrary world-space position for the given
        /// team — the same evaluator + scratch-buffer pass <see cref="BuildFirstTouchContext"/>
        /// runs for receptions, exposed for the shot adapter's §4.4.1 pressure query (shot-outcome
        /// design KD-4). Both callers run inside the single-threaded Resolve phase (executors at
        /// C3, first touch at C4), so the shared <see cref="_opponentScratch"/> cannot alias.
        /// </summary>
        private float ComputeOpponentPressureScalar(Vector2 positionXY, int teamId)
        {
            int opponentTeam = MatchEngineConstants.TEAM_COUNT - 1 - teamId; // 0 ↔ 1

            for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
            {
                int oi = opponentTeam * MatchEngineConstants.PLAYERS_PER_TEAM + k;
                _opponentScratch[k] = _agents[oi].Position;
            }

            PressureResult pressure = TacticalDirector.FirstTouch.PressureEvaluator.Evaluate(
                positionXY,
                new ReadOnlySpan<Vector2>(_opponentScratch, 0, MatchEngineConstants.PLAYERS_PER_TEAM));

            return pressure.PressureScalar;
        }

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

            Vector3 facing3 = new Vector3(facing.x, facing.y, 0f);

            // #27 T1 (projection design §3.5a): canonical FirstTouchAbility + Technique, raw int
            // copies (the canonical record is already int, so the pre-T1 RoundToInt of the neutral
            // float seed reduces to the same value — implementation-time inventory addition for
            // Technique recorded in the projection design doc's version history). Neutral ⇒ 10 each.
            return new FirstTouchContext
            {
                AgentID                   = i,
                TeamID                    = teamId,
                Technique                 = _canonicalAttrs[i].Technique,
                FirstTouchAttribute       = PlayerAttributeProjection.FirstTouchAbility(in _canonicalAttrs[i]),
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

            // #37 KD-7: copy out this tick's Tier A/B records for observers, in the same FM-017-002
            // canonical order the bytes above were written in (one derivation, two readers). This is
            // the only moment the records exist and the tick is identified: OnTickBoundary below
            // resets the queue, and the next tick overwrites the ring. Read-only — the capture
            // consumes nothing and touches neither the payload nor the digest.
            EventBus.CaptureTickLedger(_tickLedger);

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
            // _perfs are NOT serialized. Both are boot-deterministic on the default path — since
            // #27 T1, _attrs (and _dtAttrs/_perceptionAttrs/bench attrs) are projections of the
            // _canonicalAttrs records, which default to CreateDefault() and are only overwritten by
            // the pre-kickoff ConfigureSquads (never mutated mid-sim except the substitution bench
            // copy, itself a pure function of the boot-configured bench records + the serialized
            // _activeBenchSlot) — so a default-path save/restore reconstructs them identically at
            // boot and their omission cannot diverge replay. DISTINCT-SQUAD SCOPE (projection
            // design KD-P10 / #27 T3): the attribute VALUES stay excluded (re-projectable from the
            // roster), but since T3 the per-team roster REFERENCE (each Squad's ClubId) IS serialized
            // below (v16) — the identity half of restore fidelity, so a save records which squad each
            // team loaded. Full distinct-squad restore still needs a snapshot-deserialize path to
            // re-project the records from the referenced roster (keyed by _activeBenchSlot for
            // substitution bench-swaps); none exists in the engine yet (KD-T3-3), so building the
            // re-projection consumer now would be a phantom. The reference is captured; the restore
            // that would consume it is future work, unblocked on the data side. The
            // Phase-A observation counters (_aiPhaseRanThisTick/_aiPhaseRunCount) are likewise
            // excluded — instrumentation derivable from the tick number, not gameplay state.
            // PHASE-D FLAG: the AI phase still does NOT write per-agent form/fatigue context into
            // _perfs (it stays the boot-neutral constant) — when it begins to, _perfs becomes
            // cross-tick state and MUST be serialized here (bump SNAPSHOT_SCHEMA_VERSION at that point).
            //
            // CROSS-TICK COVERAGE (D4 v8, completed by v17): every cross-tick gameplay surface is serialized.
            // NOTE (snapshot-deserialize-design.md KD-8): the v8-era "no cross-tick state is excluded" claim
            // below became STALE at v15, when match-flow completion added the match-flow.card-severity
            // DeterministicRngService stream — a mutable RNG cursor that IS cross-tick state and was NOT then
            // serialized. v17 (the last block of this method) closes that gap; the claim is true as written
            // only from v17 onward. The lesson: a new DeterministicRngService draw site is cross-tick state
            // and must land in the snapshot in the same change that adds the draw.
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

            // v15 (match-flow completion) — discipline (per-agent yellow-card count + sent-off flag +
            // the global foul cooldown), substitutions (per-agent active bench slot + per-team
            // substitutions-used count), and the match-flow clock (half-time / full-time fired flags).
            // All cross-tick and digest-load-bearing: a mid-match card, substitution, or half/full-time
            // transition now feeds the digest chain.
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                CanonicalSerializer.WriteU8 (buf, ref o, _yellowCards[i]);
                CanonicalSerializer.WriteBool(buf, ref o, _isSentOff[i]);
            }
            CanonicalSerializer.WriteI32(buf, ref o, _foulCooldownRemaining);
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                CanonicalSerializer.WriteI32(buf, ref o, _activeBenchSlot[i]);
            }
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                CanonicalSerializer.WriteI32(buf, ref o, _substitutionsUsed[t]);
            }
            CanonicalSerializer.WriteBool(buf, ref o, _secondHalfStarted);
            CanonicalSerializer.WriteBool(buf, ref o, _matchEnded);

            // v16 (#27 T3 — squad-roster-reference-design.md) — the per-team roster reference: the
            // ClubId of the Squad ConfigureSquads loaded, or NO_ROSTER_CLUB_ID (−1) when unconfigured.
            // Boot-constant identity (the same class as _teamIds/_isGoalkeeper above), captured so a
            // save records WHICH squad each team loaded — the identity half of distinct-squad restore
            // fidelity (the attribute VALUES stay excluded above, re-projectable from the roster). A
            // real ClubId is deliberately digest-distinguishable from the sentinel (KD-T3-2): a
            // configured all-neutral squad still moves agents identically to an unconfigured match, so
            // this field is the sole digest difference — the reference does its job even when the
            // behaviour is neutral.
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                CanonicalSerializer.WriteI32(buf, ref o, _rosterClubId[t]);
            }

            // v17 (snapshot-deserialize-design.md KD-8) — the match-flow.card-severity RNG stream cursor.
            // This DeterministicRngService stream is the match engine's ONLY mutable RNG stream (collision
            // self-seeds from matchSeed ^ frameNumber; pass/shot error is hash-based on the tick — both pure
            // functions of the tick, reconstructible with no stored state), so it is the whole of the RNG
            // cross-tick surface. Its cursor advances on every card-severity draw (one per issued card), so
            // WITHOUT this field a restore would re-register the stream at ActionOrdinal 0 and the next card
            // draw after any prior booking would diverge from the saved run — the round-trip determinism
            // contract (KD-5) silently failed for any match with a card. Only the two mutable fields at rest
            // are serialized: RngCursor and ActionOrdinal. The draw is reservation-atomic
            // (Reserve…CloseReservation inside ApplyFoulIfCaptured, no yield) and snapshots are taken in the
            // Snapshot phase after Resolve, so the reservation is always closed at snapshot time — the other
            // RngStreamState fields (StreamKey/SiteId/SubsystemOrdinal/EntityId/StreamVersion +
            // BudgetRemaining/DeclaredBudget/DrawIndex) are boot-reconstructed by the RegisterStream call and
            // need not be stored (the WorldStore world.text-cursor precedent). This is the RNG half of the
            // "CROSS-TICK COVERAGE" claim above: that claim (written at v8) predated the v15 card-severity
            // stream and is only made true here — a new DeterministicRngService draw site is cross-tick state.
            ref readonly RngStreamState cardStream = ref _rng.GetStreamState(_cardSeverityStreamIndex);
            CanonicalSerializer.WriteU64(buf, ref o, cardStream.RngCursor);
            CanonicalSerializer.WriteU64(buf, ref o, cardStream.ActionOrdinal);

            // v18 (gk-heading-engine-integration-design.md Phase 2) — the GK (#11) / Heading (#10) cross-tick
            // state, so a flag-on engine (EnableGkHeading) is snapshot-safe (KD-11 / §6). Written
            // UNCONDITIONALLY: both RNG streams are registered at boot regardless of the flag, and the
            // latch/orchestrator arrays sit at their boot-init values while off — so a flag-off engine
            // round-trips this block as a deterministic no-op (the schema-version bump 17 → 18 is what moves
            // the digest; there is no absolute-golden rebaseline, only the comparative two-run/round-trip
            // contract). (1) The two subsystem RNG-stream cursors (RngCursor + ActionOrdinal, the card-severity
            // precedent — draws are reservation-atomic in DrawStreamFloat01 with no yield, and snapshots are
            // taken in the Snapshot phase after Resolve, so the reservation is always closed at snapshot time;
            // the other RngStreamState fields are boot-reconstructed by RegisterStream). (2) The two §4
            // trigger latches (_saveCommittedForGk / _headerCommittedThisEpisode) — engine-level cross-tick
            // state that gates whether a save/header re-commits; WITHOUT these a restore would re-fire a
            // trigger the uninterrupted run suppressed, diverging the orchestrator state. (3) Both orchestrators'
            // in-flight arrays via their CaptureState seams. (_gkAgentIds is rebuilt each drive by
            // RefreshGkAgentIds, so it is reconstructed not serialized; the _lastCommitted*Attrs TestOnly
            // observation fields are write-only and never read by the drive, so excluded.) The opt-in flag
            // itself is cross-tick state that gates the drive, so it is serialized FIRST — a restore reproduces
            // the engine's mode (a flag-on save restores into a flag-on engine and continues deterministically;
            // without this the fresh restored engine would boot flag-off and stop driving the orchestrators).
            CanonicalSerializer.WriteBool(buf, ref o, _gkHeadingEnabled);
            ref readonly RngStreamState headStream = ref _rng.GetStreamState(_headingStreamIndex);
            CanonicalSerializer.WriteU64(buf, ref o, headStream.RngCursor);
            CanonicalSerializer.WriteU64(buf, ref o, headStream.ActionOrdinal);
            ref readonly RngStreamState gkStream = ref _rng.GetStreamState(_goalkeeperStreamIndex);
            CanonicalSerializer.WriteU64(buf, ref o, gkStream.RngCursor);
            CanonicalSerializer.WriteU64(buf, ref o, gkStream.ActionOrdinal);

            for (int k = 0; k < GoalkeeperConstants.MaxGkAgents; k++)
            {
                CanonicalSerializer.WriteBool(buf, ref o, _saveCommittedForGk[k]);
            }
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                CanonicalSerializer.WriteBool(buf, ref o, _headerCommittedThisEpisode[i]);
            }

            WriteGoalkeeperState(buf, ref o, _goalkeeper.CaptureState());
            WriteHeadingState(buf, ref o, _heading.CaptureState());

            // v19 — the collision system's contact-onset pair set (§5.Z.13). This is the ONLY
            // cross-tick state that subsystem holds: without it, a restore mid-contact would treat
            // every still-open contact as new and re-emit an onset event the uninterrupted run had
            // already spent, diverging the foul stream and the digest chain.
            CollisionContactState contacts = _collisionSystem.CaptureContactState();
            CanonicalSerializer.WriteU64(buf, ref o, contacts.Word0);
            CanonicalSerializer.WriteU64(buf, ref o, contacts.Word1);
            CanonicalSerializer.WriteU64(buf, ref o, contacts.Word2);
            CanonicalSerializer.WriteU64(buf, ref o, contacts.Word3);

            // v19 — the §5.Z.15 six-second-rule state. Cross-tick and outcome-bearing: restoring with a
            // zeroed hold count would hand a keeper a fresh six seconds on every load.
            CanonicalSerializer.WriteI32(buf, ref o, _gkHoldTicks);
            CanonicalSerializer.WriteI32(buf, ref o, _gkReleaseCooldownRemaining);
            CanonicalSerializer.WriteI32(buf, ref o, _gkReleasedAgentId);

            payload.BytesWritten = o;
        }

        /// <summary>
        /// The symmetric reader for <see cref="SerializeWorldState"/> (snapshot-deserialize design note
        /// KD-1): reads the exact field set the writer wrote, in the same canonical order, through the
        /// <see cref="CanonicalSerializer"/> read primitives, and reconstructs the engine's full cross-tick
        /// world state. This method is the line-for-line mirror of the writer and is kept adjacent to it so a
        /// future field addition is edited in both places in one diff (design note R1).
        ///
        /// The FIRST field read is the schema version — if it does not equal the build's
        /// <see cref="MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION"/> the reader throws (fail-loud; there is
        /// no cross-version migration at Stage 0, exactly as <see cref="SnapshotCodec.ValidateHeader"/> and
        /// the living-world <c>WorldStateSerializer</c> version gate). AFTER the full payload is read, the
        /// reader asserts the cursor consumed exactly <see cref="SnapshotPayload.BytesWritten"/> bytes — the
        /// trailing-byte / short-read guard that turns a writer/reader field drift within one schema version
        /// into a fail-loud rather than a silent partial restore (design note R1).
        ///
        /// Ownership boundary (KD-2): subsystem-internal state is reconstructed through each subsystem's
        /// <c>RestoreState</c> counterpart (executors / DecisionTree / OscillationGuard / the four
        /// Mechanics-AI hysteresis surfaces / Perception / RotationController), never by reaching inside
        /// their private fields. Engine-owned arrays/scalars are assigned directly (they are this engine's
        /// own state). The card-severity RNG stream cursor and the clock are restored here (they are
        /// cross-tick state carried in the payload); the digest-chain continuity is restored by the
        /// <see cref="RestoreFromSnapshot"/> factory (it lives in the header, not the payload).
        /// </summary>
        private void DeserializeWorldState(SnapshotPayload payload)
        {
            byte[] buf = payload.PayloadBytes;
            int o = 0;

            uint schema = CanonicalSerializer.ReadU32(buf, ref o);
            if (schema != MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION)
            {
                throw new InvalidOperationException(
                    $"Snapshot schema version {schema} != build version " +
                    $"{MatchEngineConstants.SNAPSHOT_SCHEMA_VERSION} — no cross-version migration at Stage 0 (KD-1).");
            }

            // Tick (the payload's self-describing copy) — restore the clock to the saved tick so the next
            // RunTick advances to tick+1, continuing the run exactly where the save was taken.
            ulong tick = CanonicalSerializer.ReadU64(buf, ref o);
            _clock.RestoreFromSnapshot(tick);

            ReadBallState(buf, ref o, ref _ball);

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                _agents[i] = ReadAgentState(buf, ref o);

                _teamIds[i]              = CanonicalSerializer.ReadI32 (buf, ref o);
                _isGoalkeeper[i]         = CanonicalSerializer.ReadBool(buf, ref o);
                _isCollisionKnockdown[i] = CanonicalSerializer.ReadBool(buf, ref o);
                _collisionForces[i]      = CanonicalSerializer.ReadF32 (buf, ref o);
                _commands[i]             = ReadMovementCommand(buf, ref o);

                PassExecutorState passState = ReadPassExecutorState(buf, ref o);
                _passExecutors[i].RestoreState(in passState);
                ShotExecutorState shotState = ReadShotExecutorState(buf, ref o);
                _shotExecutors[i].RestoreState(in shotState);

                DecisionTreeState dtState = ReadDecisionTreeState(buf, ref o);
                _decisionTrees[i].RestoreState(in dtState);
            }

            ReadMatchContext(buf, ref o, ref _matchContext);

            // Reconstruct the excluded _possessingAgentId / _prevPossessingAgentId from the restored
            // MatchContext (the writer's exclusion proof: _possessingAgentId is "captured under a different
            // field" — MatchContext.PossessingAgentId, authored each Resolve at C4). Both are read at the
            // start of the next tick — _possessingAgentId by the Resolve executors' IsBallPossessedBy and
            // RunFirstTouch, _prevPossessingAgentId by the possession-change producer — so leaving them at the
            // boot NO_POSSESSION would diverge the round-trip for any match that has developed possession (a
            // first-touch control). At snapshot time (Snapshot phase, after Resolve authors the context and
            // the possession producer runs) the invariant
            // _prevPossessingAgentId == _possessingAgentId == MatchContext.PossessingAgentId holds, so both
            // restore from the one serialized field.
            _possessingAgentId     = _matchContext.PossessingAgentId;
            _prevPossessingAgentId = _matchContext.PossessingAgentId;

            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                ReadPositioningHysteresis(buf, ref o, _positioning[t]);
            }
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                ReadPressingTickState(buf, ref o, _pressing[t]);
            }
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                ReadDefensiveTickState(buf, ref o, _defensive[t]);
            }
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                ReadAttackingTickState(buf, ref o, _attacking[t]);
            }
            ReadPerceptionTickState(buf, ref o);

            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                _activeTeamTactics[t]  = ReadTeamTactic(buf, ref o);
                _pendingTeamTactics[t] = ReadTeamTactic(buf, ref o);
            }

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                _activePlayerTactics[i]  = ReadPlayerTactic(buf, ref o);
                _pendingPlayerTactics[i] = ReadPlayerTactic(buf, ref o);
            }

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                _markingDwell[i].DwellTicks   = CanonicalSerializer.ReadI32(buf, ref o);
                _markingDwell[i].LastMarkerId = CanonicalSerializer.ReadI32(buf, ref o);
            }

            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                _buildUpStates[t].CommittedZone          = (BuildUpZone)CanonicalSerializer.ReadU8(buf, ref o);
                _buildUpStates[t].SuppressTicksRemaining = CanonicalSerializer.ReadI32(buf, ref o);
            }
            _settledPossessionTeam = CanonicalSerializer.ReadI32(buf, ref o);

            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                ReadRotationState(buf, ref o, _positioning[t]);
            }

            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                _managerStates[t].Mode                   = (ManagerMode)CanonicalSerializer.ReadU8(buf, ref o);
                _managerStates[t].ProfileOrdinal         = CanonicalSerializer.ReadU8 (buf, ref o);
                _managerStates[t].CurrentPresetOrdinal   = CanonicalSerializer.ReadU8 (buf, ref o);
                _managerStates[t].HoldIntervalsRemaining = CanonicalSerializer.ReadI32(buf, ref o);
                _managerStates[t].LastDecisionTick       = CanonicalSerializer.ReadI32(buf, ref o);
            }

            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                _goals[t] = CanonicalSerializer.ReadI32(buf, ref o);
            }
            _lastHolderAgentId = CanonicalSerializer.ReadI32(buf, ref o);

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                _yellowCards[i] = CanonicalSerializer.ReadU8 (buf, ref o);
                _isSentOff[i]   = CanonicalSerializer.ReadBool(buf, ref o);
            }
            _foulCooldownRemaining = CanonicalSerializer.ReadI32(buf, ref o);
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                _activeBenchSlot[i] = CanonicalSerializer.ReadI32(buf, ref o);
            }
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                _substitutionsUsed[t] = CanonicalSerializer.ReadI32(buf, ref o);
            }
            _secondHalfStarted = CanonicalSerializer.ReadBool(buf, ref o);
            _matchEnded        = CanonicalSerializer.ReadBool(buf, ref o);

            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                _rosterClubId[t] = CanonicalSerializer.ReadI32(buf, ref o);
            }

            // v17 — restore the match-flow.card-severity RNG stream cursor (RngCursor + ActionOrdinal, the
            // two mutable fields the reservation-atomic draw leaves at rest). Read the boot-registered stream
            // (its StreamKey / SiteId / SubsystemOrdinal / etc. were reconstructed by the RegisterStream call
            // at boot), overwrite only the two cursor fields, and restore it — the mirror of the writer and of
            // the TestOnly_SetCardSeverityStreamCursor seam.
            ulong cardCursor  = CanonicalSerializer.ReadU64(buf, ref o);
            ulong cardOrdinal = CanonicalSerializer.ReadU64(buf, ref o);
            RngStreamState cardStreamRestore = _rng.GetStreamState(_cardSeverityStreamIndex);
            cardStreamRestore.RngCursor     = cardCursor;
            cardStreamRestore.ActionOrdinal = cardOrdinal;
            _rng.RestoreStream(_cardSeverityStreamIndex, in cardStreamRestore);

            // v18 — restore the GK/Heading cross-tick state (the symmetric mirror of the writer's v18 block).
            // The heading + goalkeeper RNG cursors first (overwrite only the two cursor fields on the
            // boot-registered stream, the card-severity restore pattern), then the two §4 trigger latches, then
            // both orchestrators' in-flight arrays through their RestoreState seams. Written unconditionally by
            // the writer, so read unconditionally here — a flag-off save round-trips this as a no-op. The opt-in
            // flag is restored FIRST (mirror of the writer), so the restored engine resumes in the saved mode.
            _gkHeadingEnabled = CanonicalSerializer.ReadBool(buf, ref o);
            ulong headCursor  = CanonicalSerializer.ReadU64(buf, ref o);
            ulong headOrdinal = CanonicalSerializer.ReadU64(buf, ref o);
            RngStreamState headStreamRestore = _rng.GetStreamState(_headingStreamIndex);
            headStreamRestore.RngCursor     = headCursor;
            headStreamRestore.ActionOrdinal = headOrdinal;
            _rng.RestoreStream(_headingStreamIndex, in headStreamRestore);

            ulong gkCursor  = CanonicalSerializer.ReadU64(buf, ref o);
            ulong gkOrdinal = CanonicalSerializer.ReadU64(buf, ref o);
            RngStreamState gkStreamRestore = _rng.GetStreamState(_goalkeeperStreamIndex);
            gkStreamRestore.RngCursor     = gkCursor;
            gkStreamRestore.ActionOrdinal = gkOrdinal;
            _rng.RestoreStream(_goalkeeperStreamIndex, in gkStreamRestore);

            for (int k = 0; k < GoalkeeperConstants.MaxGkAgents; k++)
            {
                _saveCommittedForGk[k] = CanonicalSerializer.ReadBool(buf, ref o);
            }
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                _headerCommittedThisEpisode[i] = CanonicalSerializer.ReadBool(buf, ref o);
            }

            ReadGoalkeeperState(buf, ref o, _goalkeeper);
            ReadHeadingState(buf, ref o, _heading);

            // v19 — collision contact-onset pair set (mirror of the writer's trailing block).
            ulong contactW0 = CanonicalSerializer.ReadU64(buf, ref o);
            ulong contactW1 = CanonicalSerializer.ReadU64(buf, ref o);
            ulong contactW2 = CanonicalSerializer.ReadU64(buf, ref o);
            ulong contactW3 = CanonicalSerializer.ReadU64(buf, ref o);
            _collisionSystem.RestoreContactState(
                new CollisionContactState(contactW0, contactW1, contactW2, contactW3));

            // v19 — §5.Z.15 six-second-rule state (mirror of the writer).
            _gkHoldTicks                = CanonicalSerializer.ReadI32(buf, ref o);
            _gkReleaseCooldownRemaining = CanonicalSerializer.ReadI32(buf, ref o);
            _gkReleasedAgentId          = CanonicalSerializer.ReadI32(buf, ref o);

            // Trailing region: the event ledger. RunSnapshotPhase appends the canonical event-ledger bytes
            // (EventBus.SerializeLedger — a 1-byte domain tag + u32 count, then any Tier A records) AFTER the
            // world state, and they are part of the digest preimage. The reader does NOT restore the ledger —
            // after restore the engine replays forward, producing its own per-tick ledger, and the saved
            // tick's ledger is already baked into the digest the factory inherits via the header (KD-5) — but
            // it MUST account for those bytes. The world-state read must end exactly at the ledger boundary:
            // the next byte is the ledger domain tag, so a world-state read that drifted by even one byte
            // would not land on it (R1 drift check, stronger than a bare length compare on the boundary).
            byte ledgerTag = CanonicalSerializer.ReadU8(buf, ref o);
            if (ledgerTag != EventSystemConstants.DomainTagEventLedger)
            {
                throw new InvalidOperationException(
                    $"World-state read did not end at the event-ledger boundary (found 0x{ledgerTag:X2}, " +
                    $"expected the ledger domain tag 0x{EventSystemConstants.DomainTagEventLedger:X2}) — " +
                    "writer/reader field drift (R1). The reader must mirror SerializeWorldState exactly.");
            }
            uint ledgerCount = CanonicalSerializer.ReadU32(buf, ref o);
            // The empty ledger (no possession transition this tick — the Stage-0 common case) is exactly the
            // 5-byte header, so its byte account is exact. A non-empty ledger's Tier A records are not parsed
            // here (that needs the event registry, and the records are not restored); the G3 round-trip digest
            // test covers their content. Either way the reader must not have overrun the payload.
            if (ledgerCount == 0u && o != payload.BytesWritten)
            {
                throw new InvalidOperationException(
                    $"Empty-ledger payload has unexpected trailing bytes: consumed {o}, payload holds " +
                    $"{payload.BytesWritten} (R1).");
            }
            if (o > payload.BytesWritten)
            {
                throw new InvalidOperationException(
                    $"DeserializeWorldState overran the payload (consumed {o}, payload holds {payload.BytesWritten}).");
            }
        }

        /// <summary>Reads a <see cref="BallState"/> in the <see cref="WriteBallState"/> field order.</summary>
        private static void ReadBallState(byte[] buf, ref int o, ref BallState ball)
        {
            ball.Position        = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
            ball.Velocity        = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
            ball.AngularVelocity = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
            ball.State           = (BallStateType)CanonicalSerializer.ReadI32(buf, ref o);
            ball.LastValidPosition = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
            ball.LastValidVelocity = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
        }

        /// <summary>Reads an <see cref="AgentState"/> in the <see cref="WriteAgentState"/> field order,
        /// restoring the embedded <see cref="OscillationGuard"/> via its B0 <c>RestoreState</c> seam.</summary>
        private static AgentState ReadAgentState(byte[] buf, ref int o)
        {
            AgentState a = default;

            a.Position        = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
            a.Velocity        = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
            a.FacingDirection = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));

            a.CurrentState    = (AgentMovementState)CanonicalSerializer.ReadI32(buf, ref o);
            a.PreviousState   = (AgentMovementState)CanonicalSerializer.ReadI32(buf, ref o);
            a.TimeInState     = CanonicalSerializer.ReadF32(buf, ref o);
            a.GroundedReason  = (GroundedReason)CanonicalSerializer.ReadI32(buf, ref o);
            a.CollisionForce  = CanonicalSerializer.ReadF32(buf, ref o);

            a.LeanAngle       = CanonicalSerializer.ReadF32(buf, ref o);
            a.CurrentTurnRate = CanonicalSerializer.ReadF32(buf, ref o);

            a.AerobicPool     = CanonicalSerializer.ReadF32(buf, ref o);
            a.SprintReservoir = CanonicalSerializer.ReadF32(buf, ref o);

            a.LastValidPosition = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
            a.LastValidVelocity = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
            a.LastValidFacing   = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
            a.Speed             = CanonicalSerializer.ReadF32(buf, ref o);

            float t0 = CanonicalSerializer.ReadF32(buf, ref o);
            float t1 = CanonicalSerializer.ReadF32(buf, ref o);
            float t2 = CanonicalSerializer.ReadF32(buf, ref o);
            float t3 = CanonicalSerializer.ReadF32(buf, ref o);
            float t4 = CanonicalSerializer.ReadF32(buf, ref o);
            float t5 = CanonicalSerializer.ReadF32(buf, ref o);
            float t6 = CanonicalSerializer.ReadF32(buf, ref o);
            float t7 = CanonicalSerializer.ReadF32(buf, ref o);
            int writeIndex    = CanonicalSerializer.ReadI32 (buf, ref o);
            bool isLocked     = CanonicalSerializer.ReadBool(buf, ref o);
            float lockUntil   = CanonicalSerializer.ReadF32 (buf, ref o);
            var guardState = new OscillationGuardState(t0, t1, t2, t3, t4, t5, t6, t7, writeIndex, isLocked, lockUntil);
            a.OscillationGuard.RestoreState(in guardState);

            return a;
        }

        /// <summary>Reads a <see cref="MovementCommand"/> in the <see cref="WriteMovementCommand"/> field
        /// order, reconstructing it verbatim via the snapshot-restore factory (it is a readonly struct).</summary>
        private static MovementCommand ReadMovementCommand(byte[] buf, ref int o)
        {
            Vector2 target = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
            AgentMovementState desiredState = (AgentMovementState)CanonicalSerializer.ReadI32(buf, ref o);
            DecelerationMode decel = (DecelerationMode)CanonicalSerializer.ReadI32(buf, ref o);
            FacingMode facing = (FacingMode)CanonicalSerializer.ReadI32(buf, ref o);
            Vector2 facingTarget = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
            bool overrideSafety = CanonicalSerializer.ReadBool(buf, ref o);
            return MovementCommand.ReconstructFromSnapshot(target, desiredState, decel, facing, facingTarget, overrideSafety);
        }

        /// <summary>Reads a <see cref="PassExecutorState"/> in the <see cref="WritePassExecutorState"/>
        /// field order (the internal PhysicalProfile is recomputed by RestoreState, not serialized).</summary>
        private static PassExecutorState ReadPassExecutorState(byte[] buf, ref int o)
        {
            int state = CanonicalSerializer.ReadI32(buf, ref o);

            PassRequest req = new PassRequest
            {
                AgentId          = CanonicalSerializer.ReadI32(buf, ref o),
                PassType         = (PassType)CanonicalSerializer.ReadI32(buf, ref o),
                CrossSubType     = (CrossSubType)CanonicalSerializer.ReadI32(buf, ref o),
                TargetAgentId    = CanonicalSerializer.ReadI32(buf, ref o),
                TargetPosition   = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o)),
                IntendedDistance = CanonicalSerializer.ReadF32(buf, ref o),
                Urgency          = CanonicalSerializer.ReadF32(buf, ref o),
                IsWeakFoot       = CanonicalSerializer.ReadBool(buf, ref o),
                TeamId           = CanonicalSerializer.ReadI32(buf, ref o),
                FrameNumber      = CanonicalSerializer.ReadI32(buf, ref o),
            };

            CrossSubType effectiveSubType = (CrossSubType)CanonicalSerializer.ReadI32(buf, ref o);
            float kickSpeed      = CanonicalSerializer.ReadF32(buf, ref o);
            float launchAngleDeg = CanonicalSerializer.ReadF32(buf, ref o);
            Vector3 spinVector       = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
            Vector3 baseKickDir      = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
            Vector3 aimPoint         = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
            float leadDistance   = CanonicalSerializer.ReadF32(buf, ref o);
            float cachedPassing  = CanonicalSerializer.ReadF32(buf, ref o);
            float cachedFatigue  = CanonicalSerializer.ReadF32(buf, ref o);
            float cachedBodyAng  = CanonicalSerializer.ReadF32(buf, ref o);
            bool cachedIsWeak    = CanonicalSerializer.ReadBool(buf, ref o);
            int cachedWeakRating = CanonicalSerializer.ReadI32(buf, ref o);
            int windupRemaining  = CanonicalSerializer.ReadI32(buf, ref o);
            int followRemaining  = CanonicalSerializer.ReadI32(buf, ref o);

            PassResult lastResult = new PassResult
            {
                Outcome          = (PassOutcome)CanonicalSerializer.ReadI32(buf, ref o),
                FinalVelocity    = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o)),
                FinalSpin        = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o)),
                AimPoint         = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o)),
                ErrorAngleDeg    = CanonicalSerializer.ReadF32(buf, ref o),
                LeadDistance     = CanonicalSerializer.ReadF32(buf, ref o),
                PassType         = (PassType)CanonicalSerializer.ReadI32(buf, ref o),
                ContactFrame     = CanonicalSerializer.ReadI32(buf, ref o),
                ContactMatchTime = CanonicalSerializer.ReadF32(buf, ref o),
            };

            return new PassExecutorState(
                state, in req, effectiveSubType, kickSpeed, launchAngleDeg, spinVector, baseKickDir, aimPoint,
                leadDistance, cachedPassing, cachedFatigue, cachedBodyAng, cachedIsWeak, cachedWeakRating,
                windupRemaining, followRemaining, in lastResult);
        }

        /// <summary>Reads a <see cref="ShotExecutorState"/> in the <see cref="WriteShotExecutorState"/> field order.</summary>
        private static ShotExecutorState ReadShotExecutorState(byte[] buf, ref int o)
        {
            int state = CanonicalSerializer.ReadI32(buf, ref o);

            ShotRequest req = new ShotRequest
            {
                AgentId        = CanonicalSerializer.ReadI32(buf, ref o),
                PowerIntent    = CanonicalSerializer.ReadF32(buf, ref o),
                ContactZone    = (ContactZone)CanonicalSerializer.ReadI32(buf, ref o),
                SpinIntent     = CanonicalSerializer.ReadF32(buf, ref o),
                PlacementTarget = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o)),
                IsWeakFoot     = CanonicalSerializer.ReadBool(buf, ref o),
                DistanceToGoal = CanonicalSerializer.ReadF32(buf, ref o),
                TeamId         = CanonicalSerializer.ReadI32(buf, ref o),
                FrameNumber    = CanonicalSerializer.ReadI32(buf, ref o),
            };

            float kickSpeed      = CanonicalSerializer.ReadF32(buf, ref o);
            float launchAngleDeg = CanonicalSerializer.ReadF32(buf, ref o);
            Vector3 spinVector    = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
            Vector3 intendedAim   = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));

            BodyMechanicsResult bodyMech = new BodyMechanicsResult
            {
                Score                  = CanonicalSerializer.ReadF32(buf, ref o),
                ContactQualityModifier = CanonicalSerializer.ReadF32(buf, ref o),
                StumbleTriggered       = CanonicalSerializer.ReadBool(buf, ref o),
            };

            float weakFootErrMult = CanonicalSerializer.ReadF32(buf, ref o);
            int windupFrames      = CanonicalSerializer.ReadI32(buf, ref o);
            Vector3 cachedAgentPos = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
            float cachedFinishing = CanonicalSerializer.ReadF32(buf, ref o);
            float cachedLongShots = CanonicalSerializer.ReadF32(buf, ref o);
            float cachedComposure = CanonicalSerializer.ReadF32(buf, ref o);
            float cachedFatigue   = CanonicalSerializer.ReadF32(buf, ref o);
            int windupRemaining   = CanonicalSerializer.ReadI32(buf, ref o);
            int followRemaining   = CanonicalSerializer.ReadI32(buf, ref o);

            ShotResult lastResult = new ShotResult
            {
                Outcome            = (ShotOutcome)CanonicalSerializer.ReadI32(buf, ref o),
                FinalVelocity      = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o)),
                FinalSpin          = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o)),
                IntendedDirection  = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o)),
                FinalDirection     = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o)),
                ErrorOffset        = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o)),
                BodyMechanicsScore = CanonicalSerializer.ReadF32(buf, ref o),
                PowerPenaltyApplied = CanonicalSerializer.ReadF32(buf, ref o),
                KickSpeed          = CanonicalSerializer.ReadF32(buf, ref o),
                LaunchAngleDeg     = CanonicalSerializer.ReadF32(buf, ref o),
                StumbleTriggered   = CanonicalSerializer.ReadBool(buf, ref o),
                ContactFrame       = CanonicalSerializer.ReadI32(buf, ref o),
            };

            return new ShotExecutorState(
                state, in req, kickSpeed, launchAngleDeg, spinVector, intendedAim, in bodyMech,
                weakFootErrMult, windupFrames, cachedAgentPos, cachedFinishing, cachedLongShots,
                cachedComposure, cachedFatigue, windupRemaining, followRemaining, in lastResult);
        }

        /// <summary>Reads a <see cref="DecisionTreeState"/> in the <see cref="WriteDecisionTreeState"/> field order.</summary>
        private static DecisionTreeState ReadDecisionTreeState(byte[] buf, ref int o)
        {
            int state = CanonicalSerializer.ReadI32(buf, ref o);
            bool hasDispatched = CanonicalSerializer.ReadBool(buf, ref o);

            int agentId = CanonicalSerializer.ReadI32(buf, ref o);
            ActionType type = (ActionType)CanonicalSerializer.ReadI32(buf, ref o);
            int targetAgentId = CanonicalSerializer.ReadI32(buf, ref o);
            Vector2 targetPosition = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));

            PassRequest passParams = new PassRequest
            {
                AgentId          = CanonicalSerializer.ReadI32(buf, ref o),
                PassType         = (PassType)CanonicalSerializer.ReadI32(buf, ref o),
                CrossSubType     = (CrossSubType)CanonicalSerializer.ReadI32(buf, ref o),
                TargetAgentId    = CanonicalSerializer.ReadI32(buf, ref o),
                TargetPosition   = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o)),
                IntendedDistance = CanonicalSerializer.ReadF32(buf, ref o),
                Urgency          = CanonicalSerializer.ReadF32(buf, ref o),
                IsWeakFoot       = CanonicalSerializer.ReadBool(buf, ref o),
                TeamId           = CanonicalSerializer.ReadI32(buf, ref o),
                FrameNumber      = CanonicalSerializer.ReadI32(buf, ref o),
            };

            ShotRequest shotParams = new ShotRequest
            {
                AgentId        = CanonicalSerializer.ReadI32(buf, ref o),
                PowerIntent    = CanonicalSerializer.ReadF32(buf, ref o),
                ContactZone    = (ContactZone)CanonicalSerializer.ReadI32(buf, ref o),
                SpinIntent     = CanonicalSerializer.ReadF32(buf, ref o),
                PlacementTarget = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o)),
                IsWeakFoot     = CanonicalSerializer.ReadBool(buf, ref o),
                DistanceToGoal = CanonicalSerializer.ReadF32(buf, ref o),
                TeamId         = CanonicalSerializer.ReadI32(buf, ref o),
                FrameNumber    = CanonicalSerializer.ReadI32(buf, ref o),
            };

            float utilityScore = CanonicalSerializer.ReadF32(buf, ref o);
            int heartbeatTick = CanonicalSerializer.ReadI32(buf, ref o);

            AgentAction action = new AgentAction(
                agentId, type, targetAgentId, targetPosition, passParams, shotParams, utilityScore, heartbeatTick);
            return new DecisionTreeState(state, in action, hasDispatched);
        }

        /// <summary>Reads a <see cref="MatchContext"/> in the <see cref="WriteMatchContext"/> field order.</summary>
        private static void ReadMatchContext(byte[] buf, ref int o, ref MatchContext m)
        {
            m.HomeScore         = CanonicalSerializer.ReadI32(buf, ref o);
            m.AwayScore         = CanonicalSerializer.ReadI32(buf, ref o);
            m.MatchTimeSeconds  = CanonicalSerializer.ReadF32(buf, ref o);
            m.Possession        = (PossessionState)CanonicalSerializer.ReadI32(buf, ref o);
            m.PossessingAgentId = CanonicalSerializer.ReadI32(buf, ref o);
            m.Phase             = (MatchPhase)CanonicalSerializer.ReadI32(buf, ref o);
            m.BallPosition      = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
            m.BallVelocity      = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
            m.BallZone          = (FieldZone)CanonicalSerializer.ReadI32(buf, ref o);
        }

        /// <summary>Reads one team's Positioning AI (#12) hysteresis in the
        /// <see cref="WritePositioningHysteresis"/> field order and restores it through
        /// <see cref="PositioningAITick.RestoreState"/>.</summary>
        private static void ReadPositioningHysteresis(byte[] buf, ref int o, PositioningAITick tick)
        {
            HysteresisState live = tick.CaptureState();
            HysteresisState src  = new HysteresisState(live.SquadSize);

            src.CurrentPhase    = (Phase)CanonicalSerializer.ReadI32(buf, ref o);
            src.CandidatePhase  = (Phase)CanonicalSerializer.ReadI32(buf, ref o);
            src.PhaseDwellCount = CanonicalSerializer.ReadI32(buf, ref o);

            AgentHysteresisState[] agents = src.Agents;
            for (int i = 0; i < agents.Length; i++)
            {
                agents[i].CurrentLine    = (LineId)CanonicalSerializer.ReadI32(buf, ref o);
                agents[i].CandidateLine  = (LineId)CanonicalSerializer.ReadI32(buf, ref o);
                agents[i].LineDwellCount = CanonicalSerializer.ReadI32(buf, ref o);
                agents[i].CurrentLane    = (LaneId)CanonicalSerializer.ReadI32(buf, ref o);
                agents[i].CandidateLane  = (LaneId)CanonicalSerializer.ReadI32(buf, ref o);
                agents[i].LaneDwellCount = CanonicalSerializer.ReadI32(buf, ref o);
            }

            tick.RestoreState(src);
        }

        /// <summary>Reads one team's #25 rotation state in the writer's block order and restores it through
        /// the <see cref="RotationController"/>'s validating restore seams (RestoreBinding / RestorePairState /
        /// RestoreLastComposedTarget).</summary>
        private static void ReadRotationState(byte[] buf, ref int o, PositioningAITick tick)
        {
            RotationController rot = tick.CaptureRotationState();

            int squadSize = rot.SquadSize;
            int[] binding = new int[squadSize];
            for (int k = 0; k < squadSize; k++)
            {
                binding[k] = CanonicalSerializer.ReadI32(buf, ref o);
            }
            rot.RestoreBinding(binding);

            for (int k = 0; k < squadSize; k++)
            {
                Vector2 target = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
                rot.RestoreLastComposedTarget(k, target);
            }

            int pairCount = rot.PairCount;
            for (int r = 0; r < pairCount; r++)
            {
                RotationPairState pair = default;
                pair.TriggerDwellTicks  = CanonicalSerializer.ReadI32 (buf, ref o);
                pair.Rotated            = CanonicalSerializer.ReadBool(buf, ref o);
                pair.HoldTicksRemaining = CanonicalSerializer.ReadI32 (buf, ref o);
                rot.RestorePairState(r, in pair);
            }
        }

        /// <summary>Reads one team's Pressing AI (#13) state in the <see cref="WritePressingTickState"/>
        /// field order and restores it through <see cref="PressingAITick.RestoreState"/>.</summary>
        private static void ReadPressingTickState(byte[] buf, ref int o, PressingAITick tick)
        {
            PressingTickState live = tick.CaptureState();
            int cap = live.Roles.Capacity;

            PressTrigger trigger = default;
            trigger.BadTouchDwell       = CanonicalSerializer.ReadI32(buf, ref o);
            trigger.BadTouchRelease     = CanonicalSerializer.ReadI32(buf, ref o);
            trigger.BackwardPassDwell   = CanonicalSerializer.ReadI32(buf, ref o);
            trigger.BackwardPassRelease = CanonicalSerializer.ReadI32(buf, ref o);
            trigger.SidelineTrapDwell   = CanonicalSerializer.ReadI32(buf, ref o);
            trigger.SidelineTrapRelease = CanonicalSerializer.ReadI32(buf, ref o);
            trigger.WeakReceiverDwell   = CanonicalSerializer.ReadI32(buf, ref o);
            trigger.WeakReceiverRelease = CanonicalSerializer.ReadI32(buf, ref o);

            int disengageDwell = CanonicalSerializer.ReadI32(buf, ref o);
            int cooldownTicks  = CanonicalSerializer.ReadI32(buf, ref o);

            RoleHysteresisState roles = new RoleHysteresisState(cap);
            float[] fatigue = new float[cap];
            for (int i = 0; i < cap; i++)
            {
                roles.LastRole[i]    = (PressRole)CanonicalSerializer.ReadI32(buf, ref o);
                roles.PendingRole[i] = (PressRole)CanonicalSerializer.ReadI32(buf, ref o);
                roles.RoleDwell[i]   = CanonicalSerializer.ReadI32(buf, ref o);
                fatigue[i]           = CanonicalSerializer.ReadF32(buf, ref o);
            }

            tick.RestoreState(new PressingTickState(roles, in trigger, disengageDwell, cooldownTicks, fatigue));
        }

        /// <summary>Reads one team's Defensive AI (#14) state in the <see cref="WriteDefensiveTickState"/>
        /// field order and restores it through <see cref="DefensiveAITick.RestoreState"/>.</summary>
        private static void ReadDefensiveTickState(byte[] buf, ref int o, DefensiveAITick tick)
        {
            DefensiveTickState live = tick.CaptureState();
            int cap = live.Hysteresis.Length;

            OffsideLineState offside = default;
            offside.CurrentLineDepth       = CanonicalSerializer.ReadF32(buf, ref o);
            offside.StepUpDwellCounter     = CanonicalSerializer.ReadI32(buf, ref o);
            offside.CooldownTicksRemaining = CanonicalSerializer.ReadI32(buf, ref o);
            offside.CoverGkZoneActiveTicks = CanonicalSerializer.ReadI32(buf, ref o);

            MarkHysteresisState[] hyst = new MarkHysteresisState[cap];
            MarkAssignment[] prev = new MarkAssignment[cap];
            for (int i = 0; i < cap; i++)
            {
                hyst[i].DwellCounter            = CanonicalSerializer.ReadI32(buf, ref o);
                hyst[i].CandidateMode           = (MarkMode)CanonicalSerializer.ReadI32(buf, ref o);
                hyst[i].CandidateTargetEntityId = CanonicalSerializer.ReadI32(buf, ref o);
                hyst[i].HoldTicks               = CanonicalSerializer.ReadI32(buf, ref o);

                prev[i].AgentEntityId      = CanonicalSerializer.ReadI32 (buf, ref o);
                prev[i].Mode               = (MarkMode)CanonicalSerializer.ReadI32(buf, ref o);
                prev[i].TargetEntityId     = CanonicalSerializer.ReadI32 (buf, ref o);
                prev[i].TargetPosition     = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
                prev[i].ValidThroughTick   = CanonicalSerializer.ReadI32 (buf, ref o);
                prev[i].OverriddenThisTick = CanonicalSerializer.ReadBool(buf, ref o);
                prev[i].IsManuallyAssigned = CanonicalSerializer.ReadBool(buf, ref o);
            }

            tick.RestoreState(new DefensiveTickState(hyst, prev, in offside));
        }

        /// <summary>Reads one team's Attacking AI (#15) state in the <see cref="WriteAttackingTickState"/>
        /// field order and restores it through <see cref="AttackingAITick.RestoreState"/>.</summary>
        private static void ReadAttackingTickState(byte[] buf, ref int o, AttackingAITick tick)
        {
            AttackingTickState live = tick.CaptureState();
            int cap = live.Hysteresis.Length;

            TransitionHoldState transition = default;
            transition.TransitionHoldTick = CanonicalSerializer.ReadI32(buf, ref o);
            transition.PrevPhase          = (Phase)CanonicalSerializer.ReadI32(buf, ref o);

            int dirTeamId       = CanonicalSerializer.ReadI32 (buf, ref o);
            bool dirOverload    = CanonicalSerializer.ReadBool(buf, ref o);
            Flank dirFlank      = (Flank)CanonicalSerializer.ReadI32(buf, ref o);
            int dirHoldTick     = CanonicalSerializer.ReadI32 (buf, ref o);
            AttackDirective directive = new AttackDirective(dirTeamId, dirOverload, dirFlank, dirHoldTick);

            AttackHysteresisState[] hyst = new AttackHysteresisState[cap];
            for (int i = 0; i < cap; i++)
            {
                hyst[i].CurrentRole   = (AttackRole)CanonicalSerializer.ReadI32(buf, ref o);
                hyst[i].DwellCounter  = CanonicalSerializer.ReadI32(buf, ref o);
                hyst[i].CandidateRole = (AttackRole)CanonicalSerializer.ReadI32(buf, ref o);
                hyst[i].CandidateDwell = CanonicalSerializer.ReadI32(buf, ref o);
            }

            tick.RestoreState(new AttackingTickState(hyst, in transition, in directive));
        }

        /// <summary>Reads the Perception (#7) state in the <see cref="WritePerceptionTickState"/> field
        /// order and restores it through <see cref="PerceptionSubsystem.RestoreState"/>.</summary>
        private void ReadPerceptionTickState(byte[] buf, ref int o)
        {
            PerceptionTickState live = _perception.CaptureState();

            int pairCap = live.Latency.PairCapacity;
            int[] latCounters = new int[pairCap];
            bool[] latConfirmed = new bool[pairCap];
            int[] latExpiry = new int[pairCap];
            for (int i = 0; i < pairCap; i++)
            {
                latCounters[i]  = CanonicalSerializer.ReadI32 (buf, ref o);
                latConfirmed[i] = CanonicalSerializer.ReadBool(buf, ref o);
                latExpiry[i]    = CanonicalSerializer.ReadI32 (buf, ref o);
            }

            int agentCap = live.ShoulderCheck.AgentCapacity;
            int[] nextCheck = new int[agentCap];
            int[] windowExpiry = new int[agentCap];
            bool[] windowActive = new bool[agentCap];
            ShoulderCheckAnimData[] anim = new ShoulderCheckAnimData[agentCap];
            for (int i = 0; i < agentCap; i++)
            {
                nextCheck[i]    = CanonicalSerializer.ReadI32 (buf, ref o);
                windowExpiry[i] = CanonicalSerializer.ReadI32 (buf, ref o);
                windowActive[i] = CanonicalSerializer.ReadBool(buf, ref o);
                anim[i].AgentId            = CanonicalSerializer.ReadI32 (buf, ref o);
                anim[i].FireFrame          = CanonicalSerializer.ReadI32 (buf, ref o);
                anim[i].CheckDirection     = CanonicalSerializer.ReadF32 (buf, ref o);
                anim[i].AnyEntityConfirmed = CanonicalSerializer.ReadBool(buf, ref o);
            }

            int scPairCap = live.ShoulderCheck.PairCapacity;
            int[] blindLatency = new int[scPairCap];
            bool[] blindConfirmed = new bool[scPairCap];
            for (int i = 0; i < scPairCap; i++)
            {
                blindLatency[i]   = CanonicalSerializer.ReadI32 (buf, ref o);
                blindConfirmed[i] = CanonicalSerializer.ReadBool(buf, ref o);
            }

            int agentCount = live.AgentCount;
            bool[] ballVisible = new bool[agentCount];
            Vector2[] ballPos = new Vector2[agentCount];
            int[] ballStale = new int[agentCount];
            for (int i = 0; i < agentCount; i++)
            {
                ballVisible[i] = CanonicalSerializer.ReadBool(buf, ref o);
                ballPos[i]     = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
                ballStale[i]   = CanonicalSerializer.ReadI32(buf, ref o);
            }

            RecognitionLatencyState latency = new RecognitionLatencyState(latCounters, latConfirmed, latExpiry);
            ShoulderCheckState shoulderCheck = new ShoulderCheckState(nextCheck, windowExpiry, windowActive, anim, blindLatency, blindConfirmed);
            _perception.RestoreState(new PerceptionTickState(in latency, in shoulderCheck, ballVisible, ballPos, ballStale));
        }

        /// <summary>Reads a <see cref="TeamTactic"/> in the <see cref="WriteTeamTactic"/> (Appendix B) field order.</summary>
        private static TeamTactic ReadTeamTactic(byte[] buf, ref int o)
        {
            Mentality mentality           = (Mentality)CanonicalSerializer.ReadI32(buf, ref o);
            TacticFormation formation     = (TacticFormation)CanonicalSerializer.ReadI32(buf, ref o);
            Tempo tempo                   = (Tempo)CanonicalSerializer.ReadI32(buf, ref o);
            TacticWidth width             = (TacticWidth)CanonicalSerializer.ReadI32(buf, ref o);
            TacticPassing passing         = (TacticPassing)CanonicalSerializer.ReadI32(buf, ref o);
            TacticPressing pressing       = (TacticPressing)CanonicalSerializer.ReadI32(buf, ref o);
            LineOfEngagement line         = (LineOfEngagement)CanonicalSerializer.ReadI32(buf, ref o);
            float defensiveLine           = CanonicalSerializer.ReadF32(buf, ref o);
            TacticDefWidth defWidth       = (TacticDefWidth)CanonicalSerializer.ReadI32(buf, ref o);
            TransitionPlan transitionWon  = (TransitionPlan)CanonicalSerializer.ReadI32(buf, ref o);
            TransitionPlan transitionLost = (TransitionPlan)CanonicalSerializer.ReadI32(buf, ref o);
            bool offsideTrap              = CanonicalSerializer.ReadBool(buf, ref o);
            TacticTriggerMask triggerMask = (TacticTriggerMask)CanonicalSerializer.ReadI32(buf, ref o);
            FocusPlay focusPlay           = (FocusPlay)CanonicalSerializer.ReadI32(buf, ref o);
            GkDistributionPolicy gkDist   = (GkDistributionPolicy)CanonicalSerializer.ReadI32(buf, ref o);
            byte timeWasting              = CanonicalSerializer.ReadU8(buf, ref o);
            MarkingOrientation marking    = (MarkingOrientation)CanonicalSerializer.ReadI32(buf, ref o);
            DismarkIntensity dismark      = (DismarkIntensity)CanonicalSerializer.ReadI32(buf, ref o);
            BuildUpStructure buildUp      = (BuildUpStructure)CanonicalSerializer.ReadI32(buf, ref o);
            RotationFreedom rotation      = (RotationFreedom)CanonicalSerializer.ReadI32(buf, ref o);

            return new TeamTactic(
                mentality, formation, tempo, width, passing, pressing, line, defensiveLine, defWidth,
                transitionWon, transitionLost, offsideTrap, triggerMask, focusPlay, gkDist, timeWasting,
                marking, dismark, buildUp, rotation);
        }

        /// <summary>Reads a <see cref="PlayerTactic"/> in the <see cref="WritePlayerTactic"/> (Appendix B) field order.</summary>
        private static PlayerTactic ReadPlayerTactic(byte[] buf, ref int o)
        {
            PlayerRole role = (PlayerRole)CanonicalSerializer.ReadI32(buf, ref o);
            Duty duty       = (Duty)CanonicalSerializer.ReadI32(buf, ref o);

            InstrBias riskyPasses       = (InstrBias)CanonicalSerializer.ReadI32(buf, ref o);
            InstrBias shootTendency     = (InstrBias)CanonicalSerializer.ReadI32(buf, ref o);
            InstrBias dribbleTendency   = (InstrBias)CanonicalSerializer.ReadI32(buf, ref o);
            InstrBias crossTendency     = (InstrBias)CanonicalSerializer.ReadI32(buf, ref o);
            InstrBias positioningFreedom = (InstrBias)CanonicalSerializer.ReadI32(buf, ref o);
            InstrBias closeDown         = (InstrBias)CanonicalSerializer.ReadI32(buf, ref o);
            bool tightMarking           = CanonicalSerializer.ReadBool(buf, ref o);
            int markTargetEntityId      = CanonicalSerializer.ReadI32(buf, ref o);
            SetPieceDutyFlags setPieces = (SetPieceDutyFlags)CanonicalSerializer.ReadI32(buf, ref o);

            PlayerInstructions instructions = new PlayerInstructions(
                riskyPasses, shootTendency, dribbleTendency, crossTendency, positioningFreedom, closeDown,
                tightMarking, markTargetEntityId, setPieces);
            return new PlayerTactic(role, duty, instructions);
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

        // ── GK (#11) / Heading (#10) engine-integration Phase 2 snapshot helpers (v18) ──
        // MatchEngine owns 100% of the byte layout (the WritePressingTickState / ReadPressingTickState
        // Option-B convention); the orchestrators expose only typed CaptureState/RestoreState carriers.
        // Enum fields are written as u8 ordinals (all fit in a byte — the manager/build-up precedent); Vector2/
        // Vector3 component-by-component as f32; the two nullables (SaveIntent.DeflectionTarget,
        // DistributeIntent.TargetReceiverId) as a present-flag bool + value-iff-present. The per-GK / per-agent
        // count is the fixed orchestrator capacity (MaxGkAgents / MaxAgents), stable for the match.

        /// <summary>Serializes the GK (#11) orchestrator's per-GK cross-tick state
        /// (<see cref="GoalkeeperTickState"/>) in the field order the reader mirrors.</summary>
        private static void WriteGoalkeeperState(byte[] buf, ref int o, in GoalkeeperTickState s)
        {
            for (int i = 0; i < GoalkeeperConstants.MaxGkAgents; i++)
            {
                CanonicalSerializer.WriteU8(buf, ref o, (byte)s.States[i]);

                GoalkeeperAgentAttributes a = s.Attrs[i];
                CanonicalSerializer.WriteF32(buf, ref o, a.Reflexes);
                CanonicalSerializer.WriteF32(buf, ref o, a.Handling);
                CanonicalSerializer.WriteF32(buf, ref o, a.Composure);
                CanonicalSerializer.WriteF32(buf, ref o, a.Strength);
                CanonicalSerializer.WriteF32(buf, ref o, a.Aerial);
                CanonicalSerializer.WriteF32(buf, ref o, a.Balance);
                CanonicalSerializer.WriteF32(buf, ref o, a.OneVsOne);
                CanonicalSerializer.WriteF32(buf, ref o, a.Pace);
                CanonicalSerializer.WriteF32(buf, ref o, a.Throwing);
                CanonicalSerializer.WriteF32(buf, ref o, a.Kicking);
                CanonicalSerializer.WriteF32(buf, ref o, a.Fatigue);
                CanonicalSerializer.WriteI32(buf, ref o, a.TeamId);

                GkContactState cs = s.ContactStates[i];
                CanonicalSerializer.WriteI32(buf, ref o, cs.PredictedContactFrame);
                CanonicalSerializer.WriteI32(buf, ref o, cs.ActualContactFrame);
                CanonicalSerializer.WriteF32(buf, ref o, cs.ReactionWindowAchieved);
                CanonicalSerializer.WriteF32(buf, ref o, cs.HandlingQualityScalar);
                CanonicalSerializer.WriteF32(buf, ref o, cs.ContactPointError.x);
                CanonicalSerializer.WriteF32(buf, ref o, cs.ContactPointError.y);
                CanonicalSerializer.WriteU8 (buf, ref o, (byte)cs.HandChoice);
                CanonicalSerializer.WriteF32(buf, ref o, cs.ClutchFirmness);

                SaveIntent si = s.SaveIntents[i];
                CanonicalSerializer.WriteU8 (buf, ref o, (byte)si.TargetHand);
                CanonicalSerializer.WriteF32(buf, ref o, si.ClutchFirmness);
                bool hasDefl = si.DeflectionTarget.HasValue;
                CanonicalSerializer.WriteBool(buf, ref o, hasDefl);
                if (hasDefl)
                {
                    Vector3 d = si.DeflectionTarget.Value;
                    CanonicalSerializer.WriteF32(buf, ref o, d.x);
                    CanonicalSerializer.WriteF32(buf, ref o, d.y);
                    CanonicalSerializer.WriteF32(buf, ref o, d.z);
                }
                CanonicalSerializer.WriteI32(buf, ref o, si.AttemptCommittedTick);
                CanonicalSerializer.WriteBool(buf, ref o, s.SaveIntentActive[i]);

                RushIntent ri = s.RushIntents[i];
                CanonicalSerializer.WriteF32(buf, ref o, ri.RushTarget.x);
                CanonicalSerializer.WriteF32(buf, ref o, ri.RushTarget.y);
                CanonicalSerializer.WriteF32(buf, ref o, ri.RushTarget.z);
                CanonicalSerializer.WriteF32(buf, ref o, ri.CommitmentLevel);
                CanonicalSerializer.WriteI32(buf, ref o, ri.AttemptCommittedTick);
                CanonicalSerializer.WriteBool(buf, ref o, s.RushIntentActive[i]);

                DistributeIntent di = s.DistributeIntents[i];
                CanonicalSerializer.WriteU8 (buf, ref o, (byte)di.DeliveryKind);
                bool hasRcv = di.TargetReceiverId.HasValue;
                CanonicalSerializer.WriteBool(buf, ref o, hasRcv);
                if (hasRcv)
                {
                    CanonicalSerializer.WriteI32(buf, ref o, di.TargetReceiverId.Value);
                }
                CanonicalSerializer.WriteF32(buf, ref o, di.TargetPoint.x);
                CanonicalSerializer.WriteF32(buf, ref o, di.TargetPoint.y);
                CanonicalSerializer.WriteF32(buf, ref o, di.TargetPoint.z);
                CanonicalSerializer.WriteF32(buf, ref o, di.PowerIntent);
                CanonicalSerializer.WriteF32(buf, ref o, di.SpinIntent.x);
                CanonicalSerializer.WriteF32(buf, ref o, di.SpinIntent.y);
                CanonicalSerializer.WriteF32(buf, ref o, di.SpinIntent.z);
                CanonicalSerializer.WriteBool(buf, ref o, s.DistributeIntentActive[i]);

                GoalkeeperPositioningContract pc = s.PositioningContracts[i];
                CanonicalSerializer.WriteF32(buf, ref o, pc.GkBaselineSlot.x);
                CanonicalSerializer.WriteF32(buf, ref o, pc.GkBaselineSlot.y);

                CanonicalSerializer.WriteI32(buf, ref o, s.DiveLaunchFrames[i]);
                CanonicalSerializer.WriteI32(buf, ref o, s.DiveDurationFrames[i]);
                CanonicalSerializer.WriteF32(buf, ref o, s.DivePeakHandZ[i]);
                CanonicalSerializer.WriteF32(buf, ref o, s.DiveDirectionLateral[i]);
                CanonicalSerializer.WriteF32(buf, ref o, s.RushLaunchMps[i]);
                CanonicalSerializer.WriteI32(buf, ref o, s.RushInitialAttackerId[i]);
                CanonicalSerializer.WriteF32(buf, ref o, s.ShotDetectedTickMs[i]);
                CanonicalSerializer.WriteF32(buf, ref o, s.RequiredReactionMs[i]);
                CanonicalSerializer.WriteBool(buf, ref o, s.ShotEventPending[i]);
                CanonicalSerializer.WriteI32(buf, ref o, s.ClaimTick[i]);
                CanonicalSerializer.WriteI32(buf, ref o, s.ReleaseTickEarliest[i]);
                CanonicalSerializer.WriteI32(buf, ref o, s.RecoveryCooldownEndTick[i]);
            }
        }

        /// <summary>Reads the GK (#11) orchestrator's per-GK cross-tick state in the
        /// <see cref="WriteGoalkeeperState"/> field order and restores it through
        /// <see cref="TacticalDirector.GoalkeeperMechanics.GoalkeeperMechanics.RestoreState"/>. Fresh arrays
        /// are allocated to the captured capacity and handed to RestoreState, which copies them into the live
        /// containers (the KD-2 reconstruct-through-the-seam contract; the ReadPressingTickState pattern).</summary>
        private static void ReadGoalkeeperState(
            byte[] buf, ref int o, TacticalDirector.GoalkeeperMechanics.GoalkeeperMechanics tick)
        {
            GoalkeeperTickState live = tick.CaptureState();
            int cap = live.States.Length;

            var states                  = new GoalkeeperState[cap];
            var attrs                   = new GoalkeeperAgentAttributes[cap];
            var contactStates           = new GkContactState[cap];
            var saveIntents             = new SaveIntent[cap];
            var saveIntentActive        = new bool[cap];
            var rushIntents             = new RushIntent[cap];
            var rushIntentActive        = new bool[cap];
            var distributeIntents       = new DistributeIntent[cap];
            var distributeIntentActive  = new bool[cap];
            var positioningContracts    = new GoalkeeperPositioningContract[cap];
            var diveLaunchFrames        = new int[cap];
            var diveDurationFrames      = new int[cap];
            var divePeakHandZ           = new float[cap];
            var diveDirectionLateral    = new float[cap];
            var rushLaunchMps           = new float[cap];
            var rushInitialAttackerId   = new int[cap];
            var shotDetectedTickMs      = new float[cap];
            var requiredReactionMs      = new float[cap];
            var shotEventPending        = new bool[cap];
            var claimTick               = new int[cap];
            var releaseTickEarliest     = new int[cap];
            var recoveryCooldownEndTick = new int[cap];

            for (int i = 0; i < cap; i++)
            {
                states[i] = (GoalkeeperState)CanonicalSerializer.ReadU8(buf, ref o);

                GoalkeeperAgentAttributes a = default;
                a.Reflexes  = CanonicalSerializer.ReadF32(buf, ref o);
                a.Handling  = CanonicalSerializer.ReadF32(buf, ref o);
                a.Composure = CanonicalSerializer.ReadF32(buf, ref o);
                a.Strength  = CanonicalSerializer.ReadF32(buf, ref o);
                a.Aerial    = CanonicalSerializer.ReadF32(buf, ref o);
                a.Balance   = CanonicalSerializer.ReadF32(buf, ref o);
                a.OneVsOne  = CanonicalSerializer.ReadF32(buf, ref o);
                a.Pace      = CanonicalSerializer.ReadF32(buf, ref o);
                a.Throwing  = CanonicalSerializer.ReadF32(buf, ref o);
                a.Kicking   = CanonicalSerializer.ReadF32(buf, ref o);
                a.Fatigue   = CanonicalSerializer.ReadF32(buf, ref o);
                a.TeamId    = CanonicalSerializer.ReadI32(buf, ref o);
                attrs[i] = a;

                GkContactState cs = default;
                cs.PredictedContactFrame  = CanonicalSerializer.ReadI32(buf, ref o);
                cs.ActualContactFrame     = CanonicalSerializer.ReadI32(buf, ref o);
                cs.ReactionWindowAchieved = CanonicalSerializer.ReadF32(buf, ref o);
                cs.HandlingQualityScalar  = CanonicalSerializer.ReadF32(buf, ref o);
                cs.ContactPointError      = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
                cs.HandChoice             = (HandEnum)CanonicalSerializer.ReadU8(buf, ref o);
                cs.ClutchFirmness         = CanonicalSerializer.ReadF32(buf, ref o);
                contactStates[i] = cs;

                SaveIntent si = default;
                si.TargetHand     = (HandEnum)CanonicalSerializer.ReadU8(buf, ref o);
                si.ClutchFirmness = CanonicalSerializer.ReadF32(buf, ref o);
                bool hasDefl = CanonicalSerializer.ReadBool(buf, ref o);
                si.DeflectionTarget = hasDefl
                    ? new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o))
                    : (Vector3?)null;
                si.AttemptCommittedTick = CanonicalSerializer.ReadI32(buf, ref o);
                saveIntents[i] = si;
                saveIntentActive[i] = CanonicalSerializer.ReadBool(buf, ref o);

                RushIntent ri = default;
                ri.RushTarget           = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
                ri.CommitmentLevel      = CanonicalSerializer.ReadF32(buf, ref o);
                ri.AttemptCommittedTick = CanonicalSerializer.ReadI32(buf, ref o);
                rushIntents[i] = ri;
                rushIntentActive[i] = CanonicalSerializer.ReadBool(buf, ref o);

                DistributeIntent di = default;
                di.DeliveryKind = (DeliveryKind)CanonicalSerializer.ReadU8(buf, ref o);
                bool hasRcv = CanonicalSerializer.ReadBool(buf, ref o);
                di.TargetReceiverId = hasRcv ? CanonicalSerializer.ReadI32(buf, ref o) : (int?)null;
                di.TargetPoint  = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
                di.PowerIntent  = CanonicalSerializer.ReadF32(buf, ref o);
                di.SpinIntent   = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
                distributeIntents[i] = di;
                distributeIntentActive[i] = CanonicalSerializer.ReadBool(buf, ref o);

                GoalkeeperPositioningContract pc = default;
                pc.GkBaselineSlot = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
                positioningContracts[i] = pc;

                diveLaunchFrames[i]        = CanonicalSerializer.ReadI32(buf, ref o);
                diveDurationFrames[i]      = CanonicalSerializer.ReadI32(buf, ref o);
                divePeakHandZ[i]           = CanonicalSerializer.ReadF32(buf, ref o);
                diveDirectionLateral[i]    = CanonicalSerializer.ReadF32(buf, ref o);
                rushLaunchMps[i]           = CanonicalSerializer.ReadF32(buf, ref o);
                rushInitialAttackerId[i]   = CanonicalSerializer.ReadI32(buf, ref o);
                shotDetectedTickMs[i]      = CanonicalSerializer.ReadF32(buf, ref o);
                requiredReactionMs[i]      = CanonicalSerializer.ReadF32(buf, ref o);
                shotEventPending[i]        = CanonicalSerializer.ReadBool(buf, ref o);
                claimTick[i]               = CanonicalSerializer.ReadI32(buf, ref o);
                releaseTickEarliest[i]     = CanonicalSerializer.ReadI32(buf, ref o);
                recoveryCooldownEndTick[i] = CanonicalSerializer.ReadI32(buf, ref o);
            }

            tick.RestoreState(new GoalkeeperTickState(
                states: states,
                attrs: attrs,
                contactStates: contactStates,
                saveIntents: saveIntents,
                saveIntentActive: saveIntentActive,
                rushIntents: rushIntents,
                rushIntentActive: rushIntentActive,
                distributeIntents: distributeIntents,
                distributeIntentActive: distributeIntentActive,
                positioningContracts: positioningContracts,
                diveLaunchFrames: diveLaunchFrames,
                diveDurationFrames: diveDurationFrames,
                divePeakHandZ: divePeakHandZ,
                diveDirectionLateral: diveDirectionLateral,
                rushLaunchMps: rushLaunchMps,
                rushInitialAttackerId: rushInitialAttackerId,
                shotDetectedTickMs: shotDetectedTickMs,
                requiredReactionMs: requiredReactionMs,
                shotEventPending: shotEventPending,
                claimTick: claimTick,
                releaseTickEarliest: releaseTickEarliest,
                recoveryCooldownEndTick: recoveryCooldownEndTick));
        }

        /// <summary>Serializes the Heading (#10) orchestrator's per-agent cross-tick state
        /// (<see cref="HeadingTickState"/>) in the field order the reader mirrors.</summary>
        private static void WriteHeadingState(byte[] buf, ref int o, in HeadingTickState s)
        {
            for (int i = 0; i < HeadingMechanicsConstants.MaxAgents; i++)
            {
                HeaderIntent hi = s.Intents[i];
                CanonicalSerializer.WriteF32(buf, ref o, hi.PowerIntent);
                CanonicalSerializer.WriteF32(buf, ref o, hi.ContactPointIntent.x);
                CanonicalSerializer.WriteF32(buf, ref o, hi.ContactPointIntent.y);
                CanonicalSerializer.WriteF32(buf, ref o, hi.TargetIntent.x);
                CanonicalSerializer.WriteF32(buf, ref o, hi.TargetIntent.y);
                CanonicalSerializer.WriteF32(buf, ref o, hi.TargetIntent.z);
                CanonicalSerializer.WriteI32(buf, ref o, hi.AttemptCommittedTick);
                CanonicalSerializer.WriteU8 (buf, ref o, (byte)hi.SetPieceContext);

                HeaderContactState hc = s.ContactStates[i];
                CanonicalSerializer.WriteI32(buf, ref o, hc.JumpStartFrame);
                CanonicalSerializer.WriteI32(buf, ref o, hc.PredictedContactFrame);
                CanonicalSerializer.WriteI32(buf, ref o, hc.IdealContactFrame);
                CanonicalSerializer.WriteI32(buf, ref o, hc.ActualContactFrame);
                CanonicalSerializer.WriteF32(buf, ref o, hc.TimingOffsetMs);
                CanonicalSerializer.WriteF32(buf, ref o, hc.ContactPointError.x);
                CanonicalSerializer.WriteF32(buf, ref o, hc.ContactPointError.y);
                CanonicalSerializer.WriteF32(buf, ref o, hc.ContactQualityScalar);
                CanonicalSerializer.WriteF32(buf, ref o, hc.DisturbanceFactor);
                CanonicalSerializer.WriteF32(buf, ref o, hc.JumpReachM);
                CanonicalSerializer.WriteF32(buf, ref o, hc.PrevFrameFacingDirection.x);
                CanonicalSerializer.WriteF32(buf, ref o, hc.PrevFrameFacingDirection.y);

                CanonicalSerializer.WriteBool(buf, ref o, s.IntentActive[i]);
                CanonicalSerializer.WriteI32 (buf, ref o, s.BallSnapshotFrames[i]);

                HeadingAgentAttributes ha = s.AgentAttrs[i];
                CanonicalSerializer.WriteI32(buf, ref o, ha.Heading);
                CanonicalSerializer.WriteI32(buf, ref o, ha.Strength);
                CanonicalSerializer.WriteI32(buf, ref o, ha.Balance);
                CanonicalSerializer.WriteF32(buf, ref o, ha.Fatigue);
                CanonicalSerializer.WriteI32(buf, ref o, ha.TeamId);
            }
        }

        /// <summary>Reads the Heading (#10) orchestrator's per-agent cross-tick state in the
        /// <see cref="WriteHeadingState"/> field order and restores it through
        /// <see cref="TacticalDirector.HeadingMechanics.HeadingMechanics.RestoreState"/>.</summary>
        private static void ReadHeadingState(
            byte[] buf, ref int o, TacticalDirector.HeadingMechanics.HeadingMechanics tick)
        {
            HeadingTickState live = tick.CaptureState();
            int cap = live.Intents.Length;

            var intents            = new HeaderIntent[cap];
            var contactStates      = new HeaderContactState[cap];
            var intentActive       = new bool[cap];
            var ballSnapshotFrames = new int[cap];
            var agentAttrs         = new HeadingAgentAttributes[cap];

            for (int i = 0; i < cap; i++)
            {
                HeaderIntent hi = default;
                hi.PowerIntent        = CanonicalSerializer.ReadF32(buf, ref o);
                hi.ContactPointIntent = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
                hi.TargetIntent       = new Vector3(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
                hi.AttemptCommittedTick = CanonicalSerializer.ReadI32(buf, ref o);
                hi.SetPieceContext    = (SetPieceContext)CanonicalSerializer.ReadU8(buf, ref o);
                intents[i] = hi;

                HeaderContactState hc = default;
                hc.JumpStartFrame           = CanonicalSerializer.ReadI32(buf, ref o);
                hc.PredictedContactFrame    = CanonicalSerializer.ReadI32(buf, ref o);
                hc.IdealContactFrame        = CanonicalSerializer.ReadI32(buf, ref o);
                hc.ActualContactFrame       = CanonicalSerializer.ReadI32(buf, ref o);
                hc.TimingOffsetMs           = CanonicalSerializer.ReadF32(buf, ref o);
                hc.ContactPointError        = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
                hc.ContactQualityScalar     = CanonicalSerializer.ReadF32(buf, ref o);
                hc.DisturbanceFactor        = CanonicalSerializer.ReadF32(buf, ref o);
                hc.JumpReachM               = CanonicalSerializer.ReadF32(buf, ref o);
                hc.PrevFrameFacingDirection = new Vector2(CanonicalSerializer.ReadF32(buf, ref o), CanonicalSerializer.ReadF32(buf, ref o));
                contactStates[i] = hc;

                intentActive[i]       = CanonicalSerializer.ReadBool(buf, ref o);
                ballSnapshotFrames[i] = CanonicalSerializer.ReadI32(buf, ref o);

                HeadingAgentAttributes ha = default;
                ha.Heading  = CanonicalSerializer.ReadI32(buf, ref o);
                ha.Strength = CanonicalSerializer.ReadI32(buf, ref o);
                ha.Balance  = CanonicalSerializer.ReadI32(buf, ref o);
                ha.Fatigue  = CanonicalSerializer.ReadF32(buf, ref o);
                ha.TeamId   = CanonicalSerializer.ReadI32(buf, ref o);
                agentAttrs[i] = ha;
            }

            tick.RestoreState(new HeadingTickState(intents, contactStates, intentActive, ballSnapshotFrames, agentAttrs));
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
        // Translate the host's AgentState + canonical player record into the per-spec query DTOs the
        // executors consume. Since #27 T1, the attribute halves are PlayerAttributeProjection reads
        // of _canonicalAttrs (the former Stage-0 neutral placeholders are the projection of the
        // default record — the ERR-007 proxies now compute from real attributes); fatigue is derived
        // from the agent's AerobicPool (0 = spent → 1 fatigued) so it is live runtime state (KD-P4).

        private PassAgentAttributes BuildPassAttributes(int i)
        {
            // #27 T1/T2 (projection design §3.4): canonical projection; KickPower derived
            // (Passing+Technique)×0.5 per KD-P1 — the [TEMPORARY-PROXY-ERR-007] formula computed
            // from real varied attributes. Neutral record ⇒ every field = the pre-T1 seed.
            return PlayerAttributeProjection.ToPass(in _canonicalAttrs[i], 1f - _agents[i].AerobicPool);
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
            // #27 T1/T2 (projection design §3.5): canonical projection; KickPower derived
            // RoundToInt((Finishing+LongShots)×0.5) per KD-P1/§4 L-1. Neutral ⇒ pre-T1 seeds.
            return PlayerAttributeProjection.ToShot(in _canonicalAttrs[i], 1f - _agents[i].AerobicPool);
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

        /// <remarks>
        /// TEAM-RELATIVITY (§5.Z.14 — ERR-006-001). Shot Mechanics #6 aims at ONE goal: its
        /// <c>GoalGeometryProvider.Get()</c> returns <c>GoalLineX = PitchLength</c> unconditionally and
        /// says so — "Assumes the attacking team is shooting toward X = PitchLength (right goal). Stage 1+
        /// will supply attack direction from match context." <c>ShotPlacementResolver</c> is written to
        /// match, down to <c>Mathf.Max(baseAimDirection.x, epsilon)</c>. Nothing ever supplied that
        /// direction, so BOTH teams shot at x = 105 — team 1 shot at the goal it defends. Measured over
        /// four full matches: team 0 scored 21, team 1 scored 0, on symmetric possession, passes and
        /// attacking-third time (§5.Z.14). It is the ERR-008-002 / ERR-013-009 defect class again.
        ///
        /// The fix is this adapter, not #6: the boundary between the engine's world frame and #6's
        /// canonical attack-+X frame is exactly here, and the engine already owns the mirror pair the
        /// rest of the composition root uses. Per §5.Z.12 — "a pair has two places that must agree; a
        /// mirror has one" — the away team's world state is mirrored INTO the canonical frame on the way
        /// in and the resulting kick is mirrored back OUT, leaving every APPROVED #6 formula, constant
        /// and test untouched. The mirror is a 180-degree rotation about Z, so the same negate-x-y rule
        /// is correct for both velocity and spin (a proper rotation transforms a pseudovector exactly as
        /// it transforms a vector).
        /// </remarks>
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
                // Canonical attack-+X frame → world frame. Both are free vectors, so both negate.
                int team = _engine._teamIds[agentId];
                BallCollision.ApplyKick(
                    ref ball,
                    MirrorVelocityIfAway(team, velocity),
                    MirrorVelocityIfAway(team, spin),
                    agentId, matchTime, logger: null);
                _engine.ReleasePossessionOnKick(agentId);
            }

            public ShotAgentAttributes GetAttributes(int agentId) => _engine.BuildShotAttributes(agentId);

            /// <summary>
            /// The shooter's state in #6's canonical attack-+X frame. Position is an affine point,
            /// velocity and facing are free vectors, so each takes its own mirror.
            /// </summary>
            public ShotAgentState GetState(int agentId)
            {
                ShotAgentState s = _engine.BuildShotState(agentId);
                int team = _engine._teamIds[agentId];
                if (team == 0)
                {
                    return s;
                }

                s.Position        = MirrorPitchIfAway(team, s.Position);
                s.Velocity        = MirrorVelocityIfAway(team, s.Velocity);
                s.FacingDirection = MirrorVelocityIfAway(team, s.FacingDirection);
                return s;
            }

            public bool GetAndClearTackleFlag(int agentId) => false;

            /// <summary>
            /// The §4.4.1 pressure query, live (shot-outcome design KD-4 — this adapter's former
            /// hardcoded 0f was the largest inert multiplier in the §3.6 error model). Reuses the
            /// SAME first-touch PressureEvaluator pass the composition root already runs (Phase D
            /// D3 InternalsVisibleTo) — the anti-parallel-surface choice. The shooter position
            /// arrives in #6's canonical attack-+X frame (§5.Z.14), so it is mirrored back to
            /// world space for the away team before evaluating (the mirror is involutive).
            /// </summary>
            public float ComputePressureScalar(Vector3 shooterPosition, int shooterTeamId)
            {
                Vector3 world = MirrorPitchIfAway(shooterTeamId, shooterPosition);
                return _engine.ComputeOpponentPressureScalar(
                    new Vector2(world.x, world.y), shooterTeamId);
            }
        }

        // ── GK (#11) / Heading (#10) boundary adapter (gk-heading-engine-integration-design.md §3.2/§3.3) ──
        // ONE stateless bridge implements all four boundary interfaces — the two ball systems share an
        // identical ApplyKick signature, and the two RNG services disambiguate by arity (NextFloat(int) →
        // heading, NextFloat(int, uint) → goalkeeper), so a single instance is injected into both
        // orchestrators' ctors. All ball mutation flows through BallCollision.ApplyKick(ref _engine._ball,
        // …) — the one seam whose NaN gate + possession bookkeeping already apply. The RNG methods draw from
        // the single per-subsystem stream registered at boot and accept-and-ignore drawSiteId/domainTag for
        // stream selection (KD-3 — the Stage-0 posture of HeadingRngServiceStub; the #16 §4.5 per-draw-site
        // registry is Stage-1 work).
        private sealed class GkHeadingWorldAdapter
            : IHeadingBallSystem, IGoalkeeperBallSystem, IHeadingRngService, IGoalkeeperRngService
        {
            private readonly MatchEngine _engine;

            public GkHeadingWorldAdapter(MatchEngine engine)
            {
                _engine = engine;
            }

            // Ball systems (#10 IHeadingBallSystem + #11 IGoalkeeperBallSystem — shared ApplyKick signature).
            public BallState GetBallState(float matchTime) => _engine._ball;

            public void ApplyKick(Vector3 velocity, Vector3 spin, int agentId, float matchTime)
            {
                BallCollision.ApplyKick(ref _engine._ball, velocity, spin, agentId, matchTime, logger: null);
            }

            public void SetPossessor(int agentId) => _engine._possessingAgentId = agentId;

            public int GetBallPossessorId() => _engine._possessingAgentId;

            // Heading RNG (#10 IHeadingRngService — arity 1 → heading stream).
            public float NextFloat(int drawSiteId) => _engine.DrawStreamFloat01(_engine._headingStreamIndex);

            public float NextGaussian(int drawSiteId) => _engine.DrawStreamGaussian(_engine._headingStreamIndex);

            // Goalkeeper RNG (#11 IGoalkeeperRngService — arity 2 → goalkeeper stream).
            public float NextFloat(int drawSiteId, uint domainTag) =>
                _engine.DrawStreamFloat01(_engine._goalkeeperStreamIndex);

            public float NextGaussian(int drawSiteId, uint domainTag) =>
                _engine.DrawStreamGaussian(_engine._goalkeeperStreamIndex);
        }

        /// <summary>
        /// Collision-event consumer (design note §3): captures AT MOST ONE foul candidate per Resolve
        /// tick into scalar fields on the host — no buffer, since only the first qualifying collision
        /// is ever acted on (cards are rare). Qualification: AGENT_AGENT, ContactType.FROM_BEHIND,
        /// ForceMagnitude ≥ FoulImpactForceThresholdN, opposite teams, and the host's foul cooldown is
        /// closed. Sent-off participation is deliberately NOT checked here — that gate lives at the
        /// application site (<see cref="MatchEngine.ApplyFoulIfCaptured"/>, AR-9 M-1), which also
        /// covers the test-injection seam. <see cref="MatchEngine.ApplyFoulIfCaptured"/> reads +
        /// resets this state immediately after <c>UpdateCollisions</c> returns.
        /// </summary>
        private sealed class MatchFlowCollisionConsumer : ICollisionEventConsumer
        {
            private readonly MatchEngine _engine;

            public MatchFlowCollisionConsumer(MatchEngine engine)
            {
                _engine = engine;
            }

            public void OnCollisionEvent(in CollisionEvent evt)
            {
                // Balance-measurement seam (§5.Z.9): forwarded BEFORE every gate, so an observer sees
                // the whole population the gates select from — including the events suppressed by the
                // cooldown, which is what makes the offline threshold sweep exact. Null in production.
                _engine._collisionObserver?.OnCollisionEvent(in evt);

                if (_engine._foulCooldownRemaining > 0)
                {
                    return;
                }
                if (evt.Type != CollisionType.AGENT_AGENT)
                {
                    return;
                }

                ContactForceData foul = evt.FoulData;
                if (foul.Type != ContactType.FROM_BEHIND)
                {
                    return;
                }
                if (foul.ForceMagnitude < MatchEngineConstants.FoulImpactForceThresholdN)
                {
                    return;
                }
                if (_engine._teamIds[foul.InstigatorAgentID] == _engine._teamIds[foul.VictimAgentID])
                {
                    return;
                }

                // KD-F4: keep the STRONGEST candidate this tick, not the first. Force now drives the
                // referee-call probability, so first-wins would let a marginal 1201 N contact shadow a
                // 2300 N challenge in the same tick and systematically under-call the hardest fouls.
                // Strictly-greater keeps the earlier of two equal contacts, so detection order (itself
                // deterministic) still decides ties. Still at most one candidate per tick.
                if (_engine._foulCandidateFound
                    && foul.ForceMagnitude <= _engine._foulCandidateForceN)
                {
                    return;
                }

                _engine._foulCandidateFound    = true;
                _engine._foulCandidateOffender = foul.InstigatorAgentID;
                _engine._foulCandidateVictim   = foul.VictimAgentID;
                _engine._foulCandidateForceN   = foul.ForceMagnitude;
            }
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
        /// GK save dispatch sink (ERR-008-013): the <see cref="TacticalDirector.DecisionTree.IDtSaveDispatch"/>
        /// the DecisionTree calls when a keeper selects SAVE. Maps the keeper's agent id to its GK slot
        /// (slot index == team id at Stage 0, <see cref="GoalkeeperConstants.MaxGkAgents"/> == TEAM_COUNT),
        /// applies the per-episode commit latch (<c>_saveCommittedForGk</c>, serialized at v18 — a SAVE
        /// re-selected each stride commits once), projects <see cref="PlayerAttributeProjection.ToGoalkeeper"/>,
        /// and commits the same Stage-0 <see cref="SaveIntent"/> the former heuristic
        /// <c>TryCommitSaveIntents</c> built. One instance backs all 22 DecisionTrees (routes by agentId).
        /// Only ever called for the flag-on threatened keeper (SAVE is generated solely under
        /// <see cref="TacticalContext.SaveAvailable"/>, set only under <see cref="EnableGkHeading"/>).
        /// </summary>
        private sealed class HostSaveDispatch : TacticalDirector.DecisionTree.IDtSaveDispatch
        {
            private readonly MatchEngine _engine;

            public HostSaveDispatch(MatchEngine engine)
            {
                _engine = engine;
            }

            public void CommitSave(int agentId)
            {
                // Defensive: SAVE is only generated for a non-sent-off keeper (SaveAvailable gate), but
                // the sink must not trust its caller — mirror the participation gates the heuristic had.
                if (agentId < 0 || agentId >= MatchEngineConstants.SQUAD_SIZE
                    || !_engine._isGoalkeeper[agentId] || _engine._isSentOff[agentId])
                {
                    return;
                }

                int teamId = _engine._teamIds[agentId];   // GK slot index == team id (KD-1, MaxGkAgents==2)
                if (teamId < 0 || teamId >= GoalkeeperConstants.MaxGkAgents)
                {
                    return;
                }

                // Per-episode latch (KD-6): commit once until the episode clears (RunMechanicsAI clears
                // the latch when the ball is no longer armed).
                if (_engine._saveCommittedForGk[teamId])
                {
                    return;
                }

                GoalkeeperAgentAttributes attrs =
                    PlayerAttributeProjection.ToGoalkeeper(in _engine._canonicalAttrs[agentId], teamId, fatigue: 0f);
                var intent = new SaveIntent
                {
                    TargetHand           = HandEnum.Either,
                    ClutchFirmness       = MatchEngineConstants.SaveTriggerClutchFirmness,
                    DeflectionTarget     = null,
                    AttemptCommittedTick = (int)_engine._clock.CurrentTacticalTick,
                };
                _engine._goalkeeper.CommitSaveIntent(teamId, intent, attrs);
                _engine._lastCommittedSaveAttrs = attrs;
                _engine._lastSaveAttrsValid = true;
                _engine._saveCommittedForGk[teamId] = true;
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
// | 1.31    | 2026-07-14 | —      | Match-flow completion (docs/tracking/match-flow-completion-      |
// |         |            |        | design.md): CheckGoalAndRestart renamed/extended to              |
// |         |            |        | CheckRestartAndApply — throw-ins/corners/goal-kicks now route     |
// |         |            |        | through new RestartResolver + a shared ApplyRestart primitive,   |
// |         |            |        | publishing RestartAwardedEvent (0x19). Fouls/cards: renamed       |
// |         |            |        | NullCollisionEventConsumer → MatchFlowCollisionConsumer (captures |
// |         |            |        | one FROM_BEHIND candidate/tick); new ApplyFoulIfCaptured draws    |
// |         |            |        | severity from the new match-flow.card-severity RNG stream,        |
// |         |            |        | publishes FoulCommittedEvent/CardIssuedEvent, sends off on red /  |
// |         |            |        | second yellow, awards a free kick. Offside: new                  |
// |         |            |        | EvaluateAndApplyOffside hooked into RunFirstTouch's Controlled    |
// |         |            |        | case (reception-time approximation via new OffsideEvaluator),    |
// |         |            |        | publishes OffsideCalledEvent (0x18). Sent-off agents excluded via |
// |         |            |        | the FOUR existing IsActive/isActive snapshot fields (Positioning/ |
// |         |            |        | Pressing/Defensive/Attacking — previously hardcoded true, now     |
// |         |            |        | !_isSentOff[i]) + frozen in RunPhysicsPhase + skipped in          |
// |         |            |        | RunAiPhase's per-agent dispatch loop. Substitutions: new public   |
// |         |            |        | SubstitutePlayer (Stage-0 in-code bench roster per design note    |
// |         |            |        | §6); publishes SubstitutionEvent (0x08, now wired — previously    |
// |         |            |        | registered with zero producers) via a small pending-event queue   |
// |         |            |        | flushed at the top of RunResolvePhase (AR-5 — the public API may  |
// |         |            |        | be called between ticks, when EventBus.CurrentPhase is not a      |
// |         |            |        | valid producer phase). Half-time/full-time: new                  |
// |         |            |        | CheckMatchFlowTransitions, called every tick from RunInputPhase;  |
// |         |            |        | half-time resets the ball + publishes MatchPhaseChangedEvent      |
// |         |            |        | (0x1A) — NOT a full ends-swap (AR-4 — team 0 attacks +X is        |
// |         |            |        | hardcoded across goal/offside/MirrorPitchIfAway; repositioning    |
// |         |            |        | agents without flipping that convention everywhere would break    |
// |         |            |        | second-half goal/offside detection); full-time sets _matchEnded,  |
// |         |            |        | which RunPhysicsPhase/RunResolvePhase/RunAiPhase check to freeze  |
// |         |            |        | gameplay while the EventBus phase lifecycle keeps running.        |
// |         |            |        | Incidental fix: UpdateMatchContext's hardcoded 0-0 score replaced |
// |         |            |        | with the real _goals[] (existed since v14, never wired here).     |
// |         |            |        | SNAPSHOT_SCHEMA_VERSION 14 → 15 (discipline + substitution +      |
// |         |            |        | match-flow-clock fields). New files: RestartResolver.cs,          |
// |         |            |        | OffsideEvaluator.cs, SubstitutionReason.cs. 15 new TestOnly_       |
// |         |            |        | seams. Full dotnet gate not runnable in this environment (network |
// |         |            |        | policy blocks the SDK download) — every file hand-verified for    |
// |         |            |        | brace/paren balance and member-name accuracy against actual       |
// |         |            |        | source; CI verification pending push.                            |
// | 1.32    | 2026-07-15 | —      | Interactive match view (docs/tracking/interactive-match-view-    |
// |         |            |        | design.md): observation-surface extension — HomeScore/AwayScore/ |
// |         |            |        | MatchEnded read-only properties, same section as the v1.24       |
// |         |            |        | BallView/AgentView surface (trivial field reads; no new state,   |
// |         |            |        | no SNAPSHOT_SCHEMA_VERSION change). Consumed by the new           |
// |         |            |        | src/match-viewer/LiveMatchStreamer + LiveMatchServer (real-time-  |
// |         |            |        | paced tick loop + a loopback-only HTTP server for a live browser |
// |         |            |        | viewer, replacing "watch after the match ends" with "watch the   |
// |         |            |        | match happen"). Full dotnet gate not runnable in this            |
// |         |            |        | environment (no SDK reachable) — verified by exhaustive manual   |
// |         |            |        | review in place of dotnet test.                                  |
// | 1.33    | 2026-07-16 | —      | Match-flow AR-7 fix pass (fresh-eyes adversarial review of the   |
// |         |            |        | July-14/15 landings). M-1: SubstitutePlayer resets                |
// |         |            |        | _yellowCards[outSlotIndex] — discipline attaches to the player,  |
// |         |            |        | not the slot; without the reset a substitute replacing a booked  |
// |         |            |        | player was sent off on their own first yellow via the            |
// |         |            |        | second-yellow promotion (nothing tested or documented the        |
// |         |            |        | inheritance). No schema change — v15 already serializes the      |
// |         |            |        | count; only its value at substitution time changes. L-2:         |
// |         |            |        | SubstitutePlayer refuses a post-full-time call — it previously   |
// |         |            |        | mutated state and queued a SubstitutionEvent that could never    |
// |         |            |        | flush (RunResolvePhase returns before                            |
// |         |            |        | PublishPendingSubstitutions once _matchEnded is set), silently   |
// |         |            |        | losing the event while the swap applied to a frozen match. L-1   |
// |         |            |        | (doc): CheckRestartAndApply's lastTouchTeam documented as the    |
// |         |            |        | last settled HOLDER, not the last physical toucher (deflections  |
// |         |            |        | never update the tracker; −1 ⇒ team 0 assumed) — RestartResolver |
// |         |            |        | 's param doc claimed "touched last", a contract drift against    |
// |         |            |        | what the caller actually passes (its doc patched in the same     |
// |         |            |        | commit, v1.1).                                                   |
// | 1.34    | 2026-07-16 | —      | AR-8 fix pass (repeat review, later same day). M-1: sent-off     |
// |         |            |        | agents excluded from RunFirstTouch's receiver scan — every       |
// |         |            |        | other participation surface had the exclusion (AI dispatch, the  |
// |         |            |        | four Mechanics-AI snapshot IsActive fills, the physics forced-   |
// |         |            |        | stop, the offside line) but the gate-4 loop did not, so a ball   |
// |         |            |        | rolling past a frozen red-carded agent handed them possession    |
// |         |            |        | they could never release (no AI dispatch ⇒ no kick),             |
// |         |            |        | deadlocking play until the next half/full-time ball reset.       |
// |         |            |        | Physical presence (collision/perception/pressure sources) is     |
// |         |            |        | deliberately unchanged — agents-keep-positions minimalism.       |
// | 1.35    | 2026-07-17 | —      | AR-9 fix pass (third repeat review). M-1: ApplyFoulIfCaptured    |
// |         |            |        | discards a foul candidate when EITHER participant is sent off —  |
// |         |            |        | the foul/card/restart interpretation is a participation surface, |
// |         |            |        | and sent-off agents remain collision bodies (physics forced-stop |
// |         |            |        | decelerates them; they then stand frozen in the path of play),   |
// |         |            |        | so pre-fix a red-carded agent repeatedly "won" free kicks        |
// |         |            |        | (ApplyRestart teleported the ball to their feet) and drew cards  |
// |         |            |        | against opponents who ran into their back, for the rest of the   |
// |         |            |        | match. Gated at the application site (timing-equivalent to the   |
// |         |            |        | capture site; covers the TestOnly injection seam; single gate    |
// |         |            |        | avoids sibling drift). No event, no cooldown, no restart on a    |
// |         |            |        | discarded candidate. Physical collision response unchanged.      |
// | 1.36    | 2026-07-17 | —      | AR-10 sweep (fourth repeat review): 0H+0M+1L, doc-only —         |
// |         |            |        | CONVERGENCE. L: the _lastHolderAgentId writer comment claimed    |
// |         |            |        | the GoalAwardedEvent credit "names the agent whose kick scored"; |
// |         |            |        | deflections never update the tracker (the approximation the      |
// |         |            |        | RestartResolver seam documents, AR-7 L-1), so a deflection-chain |
// |         |            |        | goal credits the last SETTLED holder — possibly not the kicker,  |
// |         |            |        | possibly sent off since. Comment aligned; no code change. Full   |
// |         |            |        | participation matrix re-walked clean: dispatch skip / 4 snapshot |
// |         |            |        | fills / forced-stop / offside line / receiver scan (AR-8) / foul |
// |         |            |        | interpretation (AR-9) / in-flight executors (card ⇒ ApplyRestart |
// |         |            |        | clears possession BEFORE the executor advance, and the adapters' |
// |         |            |        | IsBallPossessedBy reads the live value, so FM-08/FM-05 cancel at |
// |         |            |        | CONTACT) / substitution refusal / half+full-time one-shots.      |
// | 1.37    | 2026-07-17 | —      | #27 T1/T2 (projection design v0.3): every attribute-seeding      |
// |         |            |        | surface now projects from new canonical per-slot player records  |
// |         |            |        | (_canonicalAttrs/_benchCanonicalAttrs, default CreateDefault) via|
// |         |            |        | PlayerAttributeProjection — _attrs/_dtAttrs/_perceptionAttrs/    |
// |         |            |        | bench seeds, Build{Pass,Shot}Attributes (KickPower derived, KD-P1|
// |         |            |        | — the ERR-007 proxies now compute from real attributes), the 3   |
// |         |            |        | FirstTouchAbility sites (#13/#14/#4 — KD-P9) + FirstTouchContext |
// |         |            |        | .Technique, Attacking pace/dribbling ÷ATTRIBUTE_MAX (KD-P3). New |
// |         |            |        | public ConfigureSquads (pre-kickoff, roster-order lineup, fail-  |
// |         |            |        | loud bounds gate); SubstitutePlayer copies the canonical bench   |
// |         |            |        | record + re-projects _dtAttrs/_perceptionAttrs (the v2.20 hazard'|
// |         |            |        | s on-pitch half). Default path byte-identical (KD-P7, digest-    |
// |         |            |        | locked); distinct-squad restore deferred to T3 (KD-P10, exclusion|
// |         |            |        | proof updated). No schema change. +6 TestOnly attribute seams.   |
// | 1.38    | 2026-07-17 | —      | #27 T1 repeat-AR (AR-4, doc-only): three comments the T1 code    |
// |         |            |        | edits outdated — the FillPressingSnapshot CoverShadowCurve note  |
// |         |            |        | ("Stage 0: neutral defaults"), the FillAttackingSnapshot summary |
// |         |            |        | ("neutral normalised placeholder"), and the BuildFirstTouch-     |
// |         |            |        | Context summary ("neutral placeholders — ERR-007") — aligned to  |
// |         |            |        | the canonical-projection sourcing. No code change.               |
// | 1.39    | 2026-07-18 | —      | #27 T3 (squad-roster-reference-design.md): per-team roster       |
// |         |            |        | reference (_rosterClubId[TEAM_COUNT], the loaded Squad.ClubId or |
// |         |            |        | NO_ROSTER_CLUB_ID) — boot-constant identity set by ConfigureSquads|
// |         |            |        | after validate-and-apply, serialized at SNAPSHOT_SCHEMA_VERSION  |
// |         |            |        | 15 → 16. A save now records which squad each team loaded (the    |
// |         |            |        | identity half of distinct-squad restore fidelity; the attribute |
// |         |            |        | VALUES stay excluded, re-projectable from the roster). KD-T3-2:  |
// |         |            |        | a configured squad is digest-distinguishable from unconfigured   |
// |         |            |        | by design (the reference is identity, not attributes) —          |
// |         |            |        | supersedes the T1 KD-P7 all-default byte-identity lock; behaviour|
// |         |            |        | stays neutral (a config-default squad moves agents identically,  |
// |         |            |        | the roster field is the sole digest difference). KD-T3-3: the    |
// |         |            |        | restore re-projection is future (no snapshot-deserialize path    |
// |         |            |        | exists). +TestOnly_RosterClubId seam; exclusion-proof + Configure|
// |         |            |        | Squads/substitution restore-scope docs updated.                 |
// | 1.40    | 2026-07-20 | —      | Snapshot-deserialize (snapshot-deserialize-design.md) Phase 1   |
// |         |            |        | KD-8 writer half: the match-flow.card-severity RngStreamState   |
// |         |            |        | cursor (RngCursor + ActionOrdinal) is serialized at SNAPSHOT_    |
// |         |            |        | SCHEMA_VERSION 16 → 17 — the engine's only mutable RNG stream    |
// |         |            |        | and the one cross-tick surface the writer omitted, so a save     |
// |         |            |        | after any booking now round-trips deterministically (the KD-5   |
// |         |            |        | contract, previously silently broken for any carded match). The |
// |         |            |        | stale v8 "no cross-tick state excluded" note corrected. Restore  |
// |         |            |        | via DeterministicRngService.RestoreStream, plus the reader /     |
// |         |            |        | RestoreFromSnapshot factory / G3 round-trip test, are the        |
// |         |            |        | remaining Phase-1 slices.                                        |
// | 1.41    | 2026-07-20 | —      | Snapshot-deserialize Phase 1 reader LANDED (KD-1/KD-2/KD-4/KD-5):|
// |         |            |        | DeserializeWorldState (the symmetric mirror of SerializeWorld-   |
// |         |            |        | State + per-block Read* helpers, reconstructing subsystem state  |
// |         |            |        | through each RestoreState seam; version-gate + event-ledger-     |
// |         |            |        | boundary trailing guard, R1); the static RestoreFromSnapshot     |
// |         |            |        | factory (fingerprint gate step 0 → boot+EventBus reset →         |
// |         |            |        | deserialize → KD-3 distinct-squad fail-loud → digest-chain       |
// |         |            |        | CommitLoadedDigest + clock restore); _possessingAgentId /        |
// |         |            |        | _prevPossessingAgentId reconstructed from the restored Match-    |
// |         |            |        | Context; TestOnly_CaptureDurableHeader/Payload seams. No schema  |
// |         |            |        | change (reader over the v17 writer). Full dotnet gate: PASSED.   |
// | 1.42    | 2026-07-20 | —      | Snapshot-deserialize Phase 2: distinct-squad re-projection (#27  |
// |         |            |        | T3 / KD-3). New ISquadProvider seam (ISquadProvider.cs); Restore-|
// |         |            |        | FromSnapshot gains an optional squads param; ReprojectDistinct-  |
// |         |            |        | Squads replaces the Phase-1 fail-loud — for each team with a     |
// |         |            |        | non-sentinel _rosterClubId it resolves the roster (ClubId-check +|
// |         |            |        | size/record validation, both teams before any apply), re-runs    |
// |         |            |        | LineupSelector + PlayerAttributeProjection for the base lineup   |
// |         |            |        | (ReprojectBaseLineup, attribute arrays only — GK flags stay the  |
// |         |            |        | restored serialized value), then replays the substitutions the  |
// |         |            |        | serialized _activeBenchSlot records (ReprojectSubstitutions).    |
// |         |            |        | Fail-loud on absent/unresolvable/mismatched roster (R4). Neutral |
// |         |            |        | path unchanged. No schema change. Full dotnet gate: PASSED.      |
// | 1.43    | 2026-07-21 | —      | Snapshot-deserialize Phase 3 on-disk fold (match-save-file-      |
// |         |            |        | design.md): public MatchSeed property (the boot seed the save    |
// |         |            |        | persists — the payload does not carry it, KD-2/KD-7) and the two |
// |         |            |        | durable-capture seams promoted TestOnly_CaptureDurableHeader/    |
// |         |            |        | Payload → production internal CaptureDurableHeader/Payload (they |
// |         |            |        | now have a production consumer, MatchSaveManager; the restore    |
// |         |            |        | tests are repointed to the production names). New src/match-     |
// |         |            |        | engine files MatchSaveContents/MatchSaveCodec/MatchSaveManager   |
// |         |            |        | wire SerializeWorldState + RestoreFromSnapshot to an on-disk     |
// |         |            |        | save file (boot-header + header + payload, atomic write). No     |
// |         |            |        | schema change. Full dotnet gate: PASSED (279 match-engine tests).|
// | 1.44    | 2026-07-22 | —      | GK #11 / Heading #10 engine integration, Phase 1 (opt-in). Boot |
// |         |            |        | constructs both sealed orchestrators + 4 stateless ball/RNG     |
// |         |            |        | adapters (HeadingBallWorldAdapter / GoalkeeperBallWorldAdapter /|
// |         |            |        | HeadingRngWorldAdapter / GoalkeeperRngWorldAdapter) and         |
// |         |            |        | registers heading.mechanics + goalkeeper.mechanics RNG streams. |
// |         |            |        | EnableGkHeading() opts in: DriveGkHeadingTactical (10 Hz, in    |
// |         |            |        | RunAiPhase) + DriveGkHeadingPhysics (60 Hz, in RunPhysicsPhase  |
// |         |            |        | before the Resolve goal check) drive both, and the §4 save/     |
// |         |            |        | header Stage-0 triggers commit intents seeded from ToGoalkeeper |
// |         |            |        | / ToHeading (the projections' live consumer). RefreshGkAgentIds |
// |         |            |        | tracks the GK slot across ConfigureSquads/subs. CaptureDurable- |
// |         |            |        | Header/Payload fail loud (NotSupportedException) when the flag  |
// |         |            |        | is on (Phase-1 not snapshot-safe; §6). Flag off = byte-         |
// |         |            |        | identical default (no SNAPSHOT_SCHEMA_VERSION change). Full     |
// |         |            |        | dotnet gate: PASSED (290 match-engine tests; whole tree green). |
// | 1.45    | 2026-07-22 | —      | GK/Heading cleaner-architecture pass (behaviour-identical). The |
// |         |            |        | four nested adapters collapsed into ONE GkHeadingWorldAdapter   |
// |         |            |        | implementing all four boundary interfaces (both ball systems    |
// |         |            |        | share ApplyKick; the two RNG services disambiguate by arity).   |
// |         |            |        | The §4 trigger geometry extracted to the pure static            |
// |         |            |        | GkHeadingIntentSource (SaveArmed / NearestHeaderCandidate);     |
// |         |            |        | TryCommitSaveIntents/HeaderIntents keep only the latch +        |
// |         |            |        | projection + orchestrator commit. New GkHeadingIntentSource-    |
// |         |            |        | Tests (10). No schema change. Full dotnet gate: PASSED (300     |
// |         |            |        | match-engine tests; whole tree green).                          |
// | 1.46    | 2026-07-23 | —      | GK/Heading engine-integration Phase 2 — SNAPSHOT_SCHEMA_VERSION |
// |         |            |        | 17 → 18: serialize the GK (#11) / Heading (#10) cross-tick      |
// |         |            |        | state so a flag-on engine (EnableGkHeading) is snapshot-safe.   |
// |         |            |        | The v18 block (written unconditionally, after the v17 card      |
// |         |            |        | cursor) = the opt-in flag + the two subsystem RNG cursors       |
// |         |            |        | (heading/goalkeeper .mechanics) + the two §4 trigger latches    |
// |         |            |        | (_saveCommittedForGk / _headerCommittedThisEpisode, engine-     |
// |         |            |        | level cross-tick state gating trigger re-commits) + both        |
// |         |            |        | orchestrators' in-flight arrays via new GoalkeeperTickState /   |
// |         |            |        | HeadingTickState CaptureState/RestoreState seams (MatchEngine   |
// |         |            |        | owns the byte layout: WriteGoalkeeperState/ReadGoalkeeperState  |
// |         |            |        | + WriteHeadingState/ReadHeadingState). RestoreFromSnapshot      |
// |         |            |        | reproduces the flag, so a flag-on save restores into a flag-on  |
// |         |            |        | engine. The Phase-1 RequireGkHeadingSnapshotSafe fail-loud      |
// |         |            |        | guard + its two call sites removed. New round-trip + schema     |
// |         |            |        | probe tests; default flag stays OFF (flip is a follow-up).      |
// | 1.47    | 2026-07-23 | —      | DT-emitted goalkeeper SAVE (ERR-008-013). The save decision    |
// |         |            |        | moves from the heuristic TryCommitSaveIntents (removed) into    |
// |         |            |        | the DecisionTree as ActionType.SAVE. New HostSaveDispatch sink  |
// |         |            |        | (IDtSaveDispatch): maps agent→GK slot, applies the v18 latch,   |
// |         |            |        | projects ToGoalkeeper, commits the same Stage-0 SaveIntent.     |
// |         |            |        | RunMechanicsAI sets TacticalContext.SaveAvailable for the       |
// |         |            |        | threatened keeper under EnableGkHeading (from                   |
// |         |            |        | GkHeadingIntentSource.SaveArmed) + clears the latch when no     |
// |         |            |        | longer armed; DriveGkHeadingTactical keeps only the header      |
// |         |            |        | trigger. No SNAPSHOT_SCHEMA_VERSION change; flag-off byte-      |
// |         |            |        | identical. New SaveDecision_SurvivesAdversarialTactic lock.     |
// | 1.48    | 2026-07-23 | —      | AR follow-up: + internal TestOnly_SaveCommittedForGk(teamId)    |
// |         |            |        | seam over the _saveCommittedForGk per-episode latch, so a test |
// |         |            |        | can observe the arm → commit → clear → re-commit episode cycle  |
// |         |            |        | (the latch clear is the sole re-commit guard for the continuous|
// |         |            |        | SAVE action). Test-only read; no behaviour change.             |
// | 1.49    | 2026-07-24 | —      | WS-1 (#26 KD-6 on-disk preset format): the manager AI now       |
// |         |            |        | resolves preset ordinals against an injected                   |
// |         |            |        | ITacticPresetCatalogue (_presetCatalogue, defaulting to the    |
// |         |            |        | in-code catalogue) instead of reading TacticPresetLibrary by   |
// |         |            |        | static reference — the ConfigureManager/SeedManagerKickoff     |
// |         |            |        | BalancedOrdinal/Count reads + the RunDecisionPoint call now go |
// |         |            |        | through it. Boot-constant reference, NOT serialized; default   |
// |         |            |        | path byte-identical, no SNAPSHOT_SCHEMA_VERSION change.        |
// | 1.50    | 2026-07-26 | —      | §5.Z Phase H possession bootstrap (ERR-030-014): ApplyRestart  |
// |         |            |        | takes an awardedTeam and awards a taker via the new            |
// |         |            |        | SelectRestartTaker (KD-H1 — every call site declares a team);  |
// |         |            |        | Boot awards the opening kickoff; RunLooseBallPickup claims a   |
// |         |            |        | ball that has come to REST (KD-H3, the exact speed-gate        |
// |         |            |        | complement of RunFirstTouch); SelectLooseBallCollector         |
// |         |            |        | designates one collector per team for the ERR-008-014 DT       |
// |         |            |        | collect (KD-H5); a Resolve-phase sweep closes the DecisionTree |
// |         |            |        | PASS/SHOOT lifecycle (KD-H4 / ERR-008-015, which had zero      |
// |         |            |        | production callers); and OnPossessionChanged defers the        |
// |         |            |        | interrupt while the new holder's own executor is in flight.    |
// |         |            |        | No SNAPSHOT_SCHEMA_VERSION change.                             |
// | 1.49    | 2026-07-26 | —      | §5.Z.9 foul & discipline balance pass. ApplyFoulIfCaptured now  |
// |         |            |        | computes ComputeFoulCallProbability(F) = min(1, callP x F /     |
// |         |            |        | threshold) and PARTITIONS the single existing card-severity     |
// |         |            |        | draw: u >= p waves on (no event, card, restart or cooldown —    |
// |         |            |        | KD-F3, since arming it would swallow the genuine foul two ticks |
// |         |            |        | later), u < p whistles and takes the severity from v = u / p.   |
// |         |            |        | No new RNG stream, no SNAPSHOT_SCHEMA_VERSION change.           |
// |         |            |        | MatchFlowCollisionConsumer keeps the STRONGEST contact of a     |
// |         |            |        | tick rather than the first (KD-F4 — force now decides the call, |
// |         |            |        | so first-wins would under-call the hardest fouls); new          |
// |         |            |        | _foulCandidateForceN joins the always-reset-within-the-tick     |
// |         |            |        | candidate fields (not serialized). + TestOnly_SetCollisionObs-  |
// |         |            |        | erver (the measurement seam), TestOnly_FoulCallProbability,     |
// |         |            |        | TestOnly_FoulCandidateConsumer/ForceN, and an optional forceN   |
// |         |            |        | on TestOnly_InjectFoulCandidate defaulting to certainty so      |
// |         |            |        | every pre-existing injection test keeps its meaning. Measured   |
// |         |            |        | 480 -> 21 fouls, 147 -> 3.0 yellows, 75 -> 1.0 reds per 90 min. |
// | 1.50    | 2026-07-26 | —      | §5.Z.10 kickoff keeper placement. InitializeKickoffState put     |
// |         |            |        | every agent of a team on one x-line spread across the width by   |
// |         |            |        | roster index, so the keeper (index 0) took the first lateral     |
// |         |            |        | slot: 26 m upfield of the goal it defends and 28 m off-centre.   |
// |         |            |        | Stage-0 Physics SKIPS goalkeepers (#11 owns GK locomotion), so   |
// |         |            |        | that was the keeper's position for the whole ninety minutes —    |
// |         |            |        | both goals stood unguarded in every match the engine has ever    |
// |         |            |        | played, and the KD-8 Step 0 pilot measured 15-39 goals a match.  |
// |         |            |        | A keeper now spawns at (GkKickoffDepthM, WIDTH/2) on the line it |
// |         |            |        | defends, mirrored for the away side through MirrorPitchIfAway.   |
// |         |            |        | Outfield placement untouched.                                   |
// | 1.51    | 2026-07-26 | —      | §5.Z.12 de-duplicated the per-side boot placement. Kickoff       |
// |         |            |        | position and facing are now written ONCE in the acting team's    |
// |         |            |        | own-half frame and passed through MirrorPitchIfAway /            |
// |         |            |        | MirrorVelocityIfAway, so the HomeLineXM/AwayLineXM and           |
// |         |            |        | HOME_FACING_DEG/AWAY_FACING_DEG pairs are deleted and            |
// |         |            |        | FacingFromHeading (now unused) with them. A Home/Away pair is    |
// |         |            |        | two places that must agree; a mirror is one — the shape behind   |
// |         |            |        | ERR-008-002, ERR-013-009/010 and the §5.Z.10 keeper spawn.       |
// |         |            |        | The x line is byte-identical (105/4 mirrors exactly to 105*3/4); |
// |         |            |        | the away lateral spread now mirrors too (y -> WIDTH - y), so     |
// |         |            |        | boot differs and every digest moves. Behaviourally transient:    |
// |         |            |        | the AI reslots outfielders at the first stride and the keeper is |
// |         |            |        | placed explicitly. Removing the trig also strengthens the        |
// |         |            |        | determinism property FacingFromHeading special-cased for.        |
// | 1.49    | 2026-07-27 | —      | P1 richer observation frame (interactive-unity-client-design    |
// |         |            |        | §5-P1). Public: AgentYellowCards / AgentIsSentOff /             |
// |         |            |        | AgentBenchSlot / SubstitutionsUsed (KD-P1-1, the AgentTeamId    |
// |         |            |        | value-copy shape), CurrentPeriod (KD-P1-2 — derived from the    |
// |         |            |        | already-serialized _matchEnded / _secondHalfStarted, so the     |
// |         |            |        | HALF_TIME_BOUNDARY_TICK rule keeps exactly one reader), and     |
// |         |            |        | RestartAppliedThisTick / RestartAwardedTeam. The latter two     |
// |         |            |        | are WITHIN-TICK fields reset in RunInputPhase beside            |
// |         |            |        | _aiPhaseRanThisTick (KD-P1-3), so they are not cross-tick       |
// |         |            |        | state, the SerializeWorldState exclusion proof needs no new     |
// |         |            |        | class, and there is NO SNAPSHOT_SCHEMA_VERSION change; the      |
// |         |            |        | cross-tick latch a HUD needs lives in LiveMatchStreamer.        |
// |         |            |        | ApplyRestart gains a RestartCue so every restart site declares  |
// |         |            |        | its kind (KD-P1-4, the KD-H1 discipline for the awarded team);  |
// |         |            |        | ToRestartCue maps Ball Physics' ordinal-stable RestartType      |
// |         |            |        | rather than widening it (KD-P1-5). Also: the three verbatim     |
// |         |            |        | inline teamId guards in SetTeamTactic / SubstitutePlayer /      |
// |         |            |        | ConfigureManager collapsed into GuardTeamId (message text       |
// |         |            |        | unchanged) rather than adding a fourth copy.                    |
// | 1.50 | 2026-07-27 | — | **§5.Z.17 goalkeeper save pipeline.** `NotifyKeeperOfShot` opens #11's §3.2|
// |      |            |   | reaction window on the shot CONTACT frame (ERR-011-004) — the method had ZERO|
// |      |            |   | callers anywhere, so reactionWindowAchieved was pinned at 0 and a catch was|
// |      |            |   | arithmetically impossible. Stamped in MILLISECONDS (`_clock.CurrentMatchTimeMs`),|
// |      |            |   | not the seconds the executors take: AR-2 caught the first landing passing seconds|
// |      |            |   | into a pipeline that compares ms, which reproduced the permanently-zero window|
// |      |            |   | while looking fixed. `ClearSaveIntent` called on the save-episode disarm so the|
// |      |            |   | engine latch and #11's own cannot disagree. `TestOnly_GoalkeeperState` reads|
// |      |            |   | through the public CaptureState the v19 writer uses. No schema/RNG/draw-order|
// |      |            |   | change; flag-off byte-identical.                                        |
// | 1.50    | 2026-07-27 | —      | P1 AR-1 L-3: ToRestartCue's default arm emits a gated           |
// |         |            |        | LogWarning. It still falls through to RestartCue.None —         |
// |         |            |        | observation code must never abort a tick — but a RestartType    |
// |         |            |        | member added later would otherwise be reported to a View as     |
// |         |            |        | "no restart" in total silence, and this mapper is exactly the   |
// |         |            |        | place that silence would begin.                                 |
// | 1.52    | 2026-07-27 | —      | Shot-outcome pass (design KD-4): ShotWorldAdapter.ComputePressureScalar |
// |         |            |        | live (was the Stage-0 `0f` stub) via new ComputeOpponentPressureScalar  |
// |         |            |        | — the same first-touch PressureEvaluator + _opponentScratch pass        |
// |         |            |        | BuildFirstTouchContext runs (both callers single-threaded Resolve, no   |
// |         |            |        | aliasing), with the §5.Z.14 canonical-frame un-mirror for the away      |
// |         |            |        | shooter. No schema/RNG/draw-order change.                               |
// | 1.53    | 2026-07-28 | —      | Shot-speed pass (design KD-6): _prevTickBallPosition (within-tick) captured |
// |         |            |        | before ball integration; ApplySweptGoalFrameCollision after it (the goal    |
// |         |            |        | frame is physical — ERR-001-005); CheckRestartAndApply adjudicates at the   |
// |         |            |        | interpolated crossing (KD-5); TestOnly_WoodworkStrikes diagnostic counter.  |
// |         |            |        | No schema change.                                                           |
#endregion
