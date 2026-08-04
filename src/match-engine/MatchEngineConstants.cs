// File:     src/match-engine/MatchEngineConstants.cs
// Created:  2026-06-16
// Modified: 2026-07-27  (B3: CARD_KIND_YELLOW / CARD_KIND_RED)
// Modified: 2026-07-11 (#26 manager-AI wiring — SNAPSHOT_SCHEMA_VERSION 12 → 13, v13 ManagerState doc)
// Modified: 2026-07-11 (engine substrate — match-length/halves model + SNAPSHOT_SCHEMA_VERSION 13 → 14)
// Modified: 2026-07-14 (match-flow completion — restart/foul-card/offside/substitution/half-full-time constants; SNAPSHOT_SCHEMA_VERSION 14 → 15)
// Modified: 2026-07-17 (#27 T1 AR-4, doc-only — STAGE0_NEUTRAL_* stale ERR-007 TODOs retired: production-unconsumed since T1, retained as the KD-P7 neutral-equivalence references)
// Modified: 2026-07-27 (P1 richer observation frame: NO_RESTART_TEAM sentinel)
// Modified (prior): 2026-07-18 (#27 T3 — NO_ROSTER_CLUB_ID sentinel + SNAPSHOT_SCHEMA_VERSION 15 → 16, v16 per-team roster reference)
// Modified: 2026-07-22 (GK #11 / Heading #10 engine integration Phase 1 — +6 [GT] Stage-0 save/header trigger constants; no schema change)
// Modified: 2026-07-26 (§5.Z Phase H — [FIXED] FIRST_HALF_KICKOFF_TEAM + [DERIVED] SECOND_HALF_KICKOFF_TEAM + [GT] LooseBallPickupRadiusM; no schema change)
// Modified: 2026-07-26 (§5.Z.12: HomeLineXM/AwayLineXM collapsed to OutfieldKickoffLineXM; HOME_FACING_DEG/AWAY_FACING_DEG deleted — facing now mirrors)
// Modified: 2026-07-26 (§5.Z.10: + [CROSS] GkKickoffDepthM mirroring PositioningAIConstants.GK_DEPTH_M — the keeper's goal-line spawn depth)
// Modified: 2026-07-26 (§5.Z.9 foul/discipline balance pass: + [GT] FoulCallProbability; Yellow 0.35 -> 0.16, Red 0.05 -> 0.011, FoulCooldownTicks 60 -> 180; no schema change. See docs/tracking/foul-discipline-balance-design.md)
// Modified: 2026-08-04 (wiring backlog W1 keeper rush trigger: + [FIXED] GK_RUSH_SOLVE_EPSILON + 4 [GT] GkRush* trigger constants; no schema change. See docs/tracking/gk-rush-trigger-design.md)
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §2.3, Code Standards #20
// Purpose:  Constant catalogue for the match-engine composition root. Stage 0 Phase A holds the
//           roster sizing, the coordinate convention (Ball Physics #1 §1.2 corner-origin, Z-up),
//           and the Phase-A snapshot payload format version. Real formation slots are sourced from
//           PositioningAIConstants when the AI phase is wired (Phase D); the Phase-A kickoff line
//           positions are scaffold values derived from pitch geometry only.

using System;

using TacticalDirector.PositioningAI;
using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Constants for the match-engine composition root.
    /// Coordinate convention is the project-wide corner-origin, Z-up system (CLAUDE.md /
    /// Ball Physics #1 §1.2): X goal-to-goal [0,105], Y touchline-to-touchline [0,68], Z up.
    /// </summary>
    public static class MatchEngineConstants
    {
        #region Fixed

        /// <summary>[FIXED] Total players on the pitch (11 v 11). Match Engine design note §2.3.</summary>
        public const int SQUAD_SIZE = 22;

        /// <summary>[FIXED] Degenerate-coefficient guard for the W1 rush-intercept quadratic — the
        /// magnitude below which a coefficient is treated as zero rather than divided by. Numerical
        /// guard, not a tunable: it selects which algebraic branch is well-conditioned.
        /// gk-rush-trigger-design.md §2.2.</summary>
        public const float GK_RUSH_SOLVE_EPSILON = 1e-6f;

        /// <summary>[FIXED] Number of teams in a match.</summary>
        public const int TEAM_COUNT = 2;

        /// <summary>[FIXED] Players per team (one goalkeeper + ten outfield).</summary>
        public const int PLAYERS_PER_TEAM = 11;

        /// <summary>[FIXED] Pitch length (goal-to-goal, X axis), metres. Ball Physics #1 §1.2.</summary>
        public const float PITCH_LENGTH_M = 105f;

        /// <summary>[FIXED] Pitch width (touchline-to-touchline, Y axis), metres. Ball Physics #1 §1.2.</summary>
        public const float PITCH_WIDTH_M = 68f;

        /// <summary>[FIXED] Resting ball-centre height above ground (ball radius), metres. Ball Physics #1 §1.2.</summary>
        public const float BALL_REST_HEIGHT_M = 0.11f;


        /// <summary>[FIXED] Possessing-agent sentinel for "ball is loose" (no agent has possession).
        /// Mirrors the Decision Tree #8 MatchContext.PossessingAgentId convention (−1 = loose);
        /// the C4 step folds host possession into MatchContext.</summary>
        public const int NO_POSSESSION = -1;

        /// <summary>[FIXED] Per-team roster-reference sentinel for "no <c>Squad</c> configured" (the
        /// default / all-neutral path). A real roster's <c>Squad.ClubId</c> is non-negative (the
        /// <c>PlayerId = clubId * CLUB_SQUAD_SIZE + localIndex</c> formula assumes it), so this −1
        /// sentinel does not collide in practice. #27 T3 (squad-roster-reference-design.md, KD-T3-1);
        /// mirrors the −1 sentinel convention (<see cref="NO_POSSESSION"/>).</summary>
        public const int NO_ROSTER_CLUB_ID = -1;

        /// <summary>[FIXED] Awarded-team sentinel for "no restart was applied this tick", reported by
        /// <c>MatchEngine.RestartAwardedTeam</c> whenever <c>RestartAppliedThisTick</c> is
        /// <c>RestartCue.None</c>. Presentation-only observation state (interactive Unity client
        /// §5-P1 KD-P1-3); mirrors the −1 sentinel convention (<see cref="NO_POSSESSION"/>).</summary>
        public const int NO_RESTART_TEAM = -1;

        /// <summary>[FIXED] <c>CardIssuedEvent.CardKind</c> value for a caution. The wire encoding of
        /// the card severity a foul draws; named here so an observer (Match Analytics #37) reads the
        /// same source the producer writes from rather than carrying a private 0/1 literal.</summary>
        public const byte CARD_KIND_YELLOW = 0;

        /// <summary>[FIXED] <c>CardIssuedEvent.CardKind</c> value for a dismissal (straight red, or a
        /// second yellow promoted by <c>ApplyCardAndCheckSentOff</c>).</summary>
        public const byte CARD_KIND_RED = 1;

        /// <summary>[FIXED] Reason ordinal written into the Phase E PossessionChangedEvent (#17 ordinal
        /// 0x04) payload. Stage 0 has no possession-change reason taxonomy (a kick release, a first-touch
        /// gain, and an interception all surface only as a holder change), so the host emits a single
        /// UNSPECIFIED reason. Stage 1+ may introduce a real reason enum; this sentinel reserves 0.</summary>
        public const byte POSSESSION_CHANGE_REASON_UNSPECIFIED = 0;

        /// <summary>[FIXED] Perception broad-phase grid insert radius (metres), Phase D D1. The host
        /// point-inserts agents into the perception grid each AI tick; the MaxPerceptionRange (120 m)
        /// query window spans the whole pitch, so the body radius does not affect candidacy — a point
        /// insert (0 m, center cell only) is sufficient and deterministic. Not a tunable.</summary>
        public const float PERCEPTION_GRID_POINT_INSERT_RADIUS = 0f;

        /// <summary>[FIXED] Match-engine world-state snapshot schema version (design note §2.6 /
        /// step B3). Versions the field set and serialization order of the world state written into
        /// the <c>SnapshotPayload</c> body by <see cref="MatchEngine.SerializeWorldState"/>; bump on
        /// ANY backward-incompatible change to that field set or order (parallel to the
        /// <c>PhaseId</c> schema-bump rule). Written as the first u32 of the payload so the body is
        /// self-describing when decoded in isolation.
        ///
        /// DISTINCT from <c>DeterministicSimConstants.SNAPSHOT_SCHEMA_VERSION</c>: that constant
        /// versions the #16 <c>SnapshotHeader</c> / codec framing that WRAPS this payload, whereas
        /// this one versions only the match-engine world-state body INSIDE it. The two evolve
        /// independently — a match-engine field-set change bumps this without touching the certified
        /// #16 header schema.
        ///
        /// v1 (Phase B / B3) was the first full §2.6 field set (ball position/velocity/spin/state +
        /// LastValid* checkpoints; per-agent full <c>AgentState</c> including the B0
        /// <c>OscillationGuard</c> state, LastValid* checkpoints, team/goalkeeper flags, the two
        /// collision-feedback inputs, and the held <c>MovementCommand</c>); it superseded the B2-era
        /// kinematic-subset PHASE_A_PAYLOAD_FORMAT_VERSION.
        ///
        /// v2 (Phase C / C5) adds the per-agent Pass/Shot executor in-flight state (the C0
        /// <c>PassExecutorState</c> / <c>ShotExecutorState</c> capture, ×22 each — cross-tick once an
        /// AI dispatcher initiates a pass/shot) and the authoritative <c>MatchContext</c> (which folds
        /// in the host's possessing-agent id; written each Resolve, read by the next AI tick).
        ///
        /// v3 (Phase D / D4) adds the per-agent DecisionTree state machine (the D0
        /// <c>DecisionTreeState</c> capture, ×22 — the <c>DtState</c> ordinal + last <c>AgentAction</c> +
        /// the §3.7.2 dispatched-action flag): a PASS/SHOOT decision is taken on one 10 Hz heartbeat and
        /// EXECUTING persists across the intervening 60 Hz ticks, so this is cross-tick simulation state
        /// that a save/restore must reconstruct. The perception internal state (RecognitionLatency /
        /// ShoulderCheck / ball-prev) and the per-team Positioning/Pressing/Defensive/Attacking hysteresis
        /// remain EXCLUDED at v3 — they have no get/restore seam yet; same-seed in-process determinism
        /// still holds (both runs evolve identically), only save/restore replay is affected. Their seams
        /// + serialization are a follow-up snapshot extension (they will bump this again).
        ///
        /// v4 (Phase D / D4 follow-up) adds the per-team Positioning AI (#12) <c>HysteresisState</c> (the
        /// CaptureState seam, ×TEAM_COUNT — team phase + dwell + per-agent line/lane membership), the first
        /// of the mechanics-AI hysteresis seams.
        ///
        /// v5 (Phase D / D4 follow-up) adds the per-team Pressing AI (#13) <c>PressingTickState</c> (the
        /// CaptureState seam, ×TEAM_COUNT — trigger debounce counters, disengage/cooldown dwell, per-agent
        /// role hysteresis + accumulated press fatigue).
        ///
        /// v6 (Phase D / D4 follow-up) adds the per-team Defensive AI (#14) <c>DefensiveTickState</c> (the
        /// CaptureState seam, ×TEAM_COUNT — per-team offside-line state + per-agent mark hysteresis + last
        /// committed mark assignment).
        ///
        /// v7 (Phase D / D4 follow-up) adds the per-team Attacking AI (#15) <c>AttackingTickState</c> (the
        /// CaptureState seam, ×TEAM_COUNT — per-team transition-hold state + frozen in-possession directive +
        /// per-agent role hysteresis).
        ///
        /// v8 (Phase D / D4 follow-up) adds the Perception (#7) <c>PerceptionTickState</c> (single shared
        /// instance — the recognition-latency tracker pair arrays, the shoulder-check scheduler per-agent +
        /// per-pair arrays, and the per-agent ball-perception carry-over). With v8 every cross-tick gameplay
        /// surface is serialized; no cross-tick state remains excluded (only boot-deterministic constants and
        /// tick-derivable observation counters).
        ///
        /// v9 (#21 / ERR-021-002) adds the per-team Tactical Instructions (#21) manager tactic — both the
        /// active <c>TeamTactic</c> (read by the AI phase) and the pending one (a <c>SetTeamTactic</c> staged
        /// but not yet committed at a stride boundary), ×TEAM_COUNT each, in Appendix B field order. Until v9
        /// the tactic was excluded, so a tactic changed MID-match did not survive save/restore; with v9 a
        /// mid-match change is restore-deterministic.
        ///
        /// v10 (#21 §3.3) adds the per-agent Tactical Instructions (#21) <c>PlayerTactic</c> (role + duty +
        /// individual instructions) — both the active tactic (read by the AI phase) and the pending one (a
        /// <c>SetPlayerTactic</c> staged but not yet committed at a stride boundary), ×SQUAD_SIZE each, in
        /// Appendix B field order. A per-agent tactic changed MID-match is now restore-deterministic. The team
        /// <c>Tempo</c> carried in the Decision Tree <c>TacticalContext</c> still needs NO field — it is
        /// re-assembled each AI tick from the serialized team tactic plus the boot identity.
        ///
        /// v11 (2026-07-07, cheap-item addition) appends <c>TeamTactic.MarkingOrientation</c> to the
        /// per-team WriteTeamTactic field list (§3.4, #14 MAN_MARK candidate radius). Appended after
        /// TimeWasting so no prior field's byte offset moves.
        ///
        /// v12 (2026-07-11, specs #23/#24/#25 wiring — one bump covers all three, landed together)
        /// appends, after the v10 per-agent tactic block and in spec order: (a) #23 per-agent
        /// <c>MarkingDwellState</c> (DwellTicks i32 + LastMarkerId i32, ×SQUAD_SIZE, #23 Appendix B);
        /// (b) #24 per-team <c>BuildUpZoneState</c> (CommittedZone u8 + SuppressTicksRemaining i32,
        /// ×TEAM_COUNT, #24 Appendix B) + the engine-level FM-BU-03 settled-possession-team tracker
        /// (i32 — the "settledTeam" the team-level-regain arming diffs against); (c) #25 per-team
        /// rotation state in #25 Appendix B order (per-agent SlotIndex binding i32 ×11, per-agent
        /// LastComposedTarget f32×2 ×11, per-pair TriggerDwellTicks i32 + Rotated bool +
        /// HoldTicksRemaining i32 in table-row order). WriteTeamTactic additionally appends the three
        /// #21 back-prop dials (DismarkIntensity / BuildUpStructure / RotationFreedom, i32 each) after
        /// MarkingOrientation in the pinned #21 Appendix B order.
        ///
        /// v13 (2026-07-11, #26 manager-AI wiring / FR-TP-012) appends the per-team
        /// <c>ManagerState</c> in the #26 Appendix C pinned field order (Mode u8, ProfileOrdinal u8,
        /// CurrentPresetOrdinal u8, HoldIntervalsRemaining i32, LastDecisionTick i32, ×TEAM_COUNT).
        /// The hold countdown and last-decision tick drive future decision points, so a save between
        /// two decision points resumes byte-identically (T-TP-DET-003). The default Human zero-init
        /// block is byte-stable across same-seed runs.
        ///
        /// v14 (2026-07-11, engine substrate — goal detection + score state) appends the per-team
        /// goal count (i32 ×TEAM_COUNT) and the last-holder tracker (i32 — the last agent roster
        /// index that HELD settled possession; the GoalAwardedEvent scorer credit and the
        /// CheckBoundaries lastTouchTeamID source). Cross-tick state: the score drives the #26
        /// manager-AI goalDiff input and the restart-side classification, so a save mid-match
        /// resumes with the correct score.
        ///
        /// v15 (2026-07-14, match-flow completion — docs/tracking/match-flow-completion-design.md)
        /// appends: per-agent yellow-card count (u8 ×SQUAD_SIZE) and sent-off flag (bool ×SQUAD_SIZE);
        /// the global foul-detection cooldown counter (i32); per-agent active bench slot (i32
        /// ×SQUAD_SIZE, −1 = original starter); per-team substitutions-used count (i32 ×TEAM_COUNT);
        /// and the half-time / full-time transition flags (bool, bool). All cross-tick, digest-load-
        /// bearing — a mid-match card, substitution, or half/full-time transition now feeds the
        /// digest chain, matching every prior field-set addition in this history.
        ///
        /// v16 (2026-07-18, #27 T3 — squad-roster-reference-design.md) appends the per-team roster
        /// reference (i32 ×TEAM_COUNT — the loaded <c>Squad.ClubId</c>, or
        /// <see cref="NO_ROSTER_CLUB_ID"/> = −1 when no squad was configured). Boot-constant identity
        /// (the same lifecycle class as the already-serialized <c>_teamIds</c>/<c>_isGoalkeeper</c>):
        /// a save now records WHICH squad each team loaded, so a future restore path can re-project the
        /// per-slot attribute records (excluded from the snapshot by the boot-deterministic proof) —
        /// keyed by the v15 <c>_activeBenchSlot</c> for substitution bench-swaps. A match configured
        /// with a real ClubId is deliberately digest-distinguishable from an unconfigured one (KD-T3-2 —
        /// the reference is identity, not attributes; this supersedes the T1 KD-P7 all-default byte-
        /// identity lock, which was a T1-only property). Behavioural neutrality is unchanged: an
        /// all-CreateDefault squad still moves agents identically — the ONLY digest difference is this
        /// reference field.
        ///
        /// v17 (2026-07-20, snapshot-deserialize-design.md KD-8) appends the <c>match-flow.card-severity</c>
        /// <c>DeterministicRngService</c> stream cursor — its <c>RngCursor</c> and
        /// <c>ActionOrdinal</c> (u64 each), the two mutable fields the reservation-atomic card-severity draw
        /// leaves at rest. This stream is the match engine's ONLY mutable RNG stream (collision self-seeds
        /// from <c>matchSeed ^ frameNumber</c> and pass/shot error is hash-based on the tick, both pure
        /// functions of the tick reconstructible with no stored state), so it is the whole of the RNG
        /// cross-tick surface. It advances on every card-severity draw (one per issued card), so before v17 a
        /// save taken AFTER any booking would restore a fresh engine at cursor 0 and the next card draw would
        /// diverge from the saved run — the round-trip determinism contract (KD-5) silently failed for any
        /// match with a card. v17 closes it (restore via <c>DeterministicRngService.RestoreStream</c>, the
        /// <c>WorldStore</c> world.text-cursor precedent). This corrects the stale v8 "no cross-tick gameplay
        /// state is excluded" claim in <see cref="MatchEngine.SerializeWorldState"/> — that note predates the
        /// v15 card-severity stream; a new <c>DeterministicRngService</c> draw site is cross-tick state and
        /// must land in the snapshot in the same change that adds it.
        ///
        /// v18 (2026-07-23, gk-heading-engine-integration-design.md Phase 2) appends the GK (#11) / Heading
        /// (#10) cross-tick state, making a flag-on engine (<c>EnableGkHeading</c>) snapshot-safe (KD-11): the
        /// two subsystem RNG-stream cursors (<c>heading.mechanics</c> / <c>goalkeeper.mechanics</c>, the
        /// card-severity precedent), the two §4 trigger latches (<c>_saveCommittedForGk</c> /
        /// <c>_headerCommittedThisEpisode</c> — engine-level cross-tick state gating trigger re-commits), and
        /// both orchestrators' in-flight arrays via their <c>CaptureState</c>/<c>RestoreState</c> seams. Written
        /// UNCONDITIONALLY (both streams register at boot regardless of the flag; while off the latch/orchestrator
        /// arrays sit at boot-init values, so a flag-off engine round-trips this block as a deterministic no-op —
        /// the version bump is what moves the digest, and the contract is comparative round-trip determinism, not
        /// an absolute golden). The Phase-1 durable-capture fail-loud guard is removed with this change.</summary>
        /// <para>v19 (§5.Z.13 contact-rate fix) appends the collision system's contact-onset pair set —
        /// four u64 words of <c>CollisionPairBitfield</c>. That set is the collision system's ONLY
        /// cross-tick state, and it became cross-tick with the fix: a <c>CollisionEvent</c> is now
        /// emitted when a contact BEGINS rather than on every tick the overlap persists, so the
        /// previous tick's pair set is the gate input. Omitting it would make a restore mid-contact
        /// re-emit an onset the uninterrupted run had already spent.</para>
        public const uint SNAPSHOT_SCHEMA_VERSION = 19;

        /// <summary>[FIXED] On-disk match save-file framing version (match-save-file-design.md KD-1).
        /// The FIRST u32 of a <c>MatchSaveManager</c> save blob; a load with a mismatched value fails
        /// loud (no cross-version migration at Stage 0 — the same posture as the two snapshot schema
        /// versions and <c>WorldStateSerializer</c>'s version gate).
        ///
        /// This is a THIRD version, distinct from both snapshot schema versions it frames:
        /// <see cref="SNAPSHOT_SCHEMA_VERSION"/> versions the world-state BODY inside the payload;
        /// <c>DeterministicSimConstants.SNAPSHOT_SCHEMA_VERSION</c> versions the #16 <c>SnapshotHeader</c>
        /// FRAMING that wraps the payload; this one versions the FILE frame (boot-header + header +
        /// payload) that packs a whole save. Bump it on any change to the on-disk layout in
        /// <c>MatchSaveCodec</c> (a new boot-header field, a header/fingerprint field, or a reorder) —
        /// the two inner schema versions ride inside the blob and are re-checked by
        /// <see cref="MatchEngine.RestoreFromSnapshot"/> itself, so this one need only track the OUTER
        /// file frame.</summary>
        public const uint MATCH_SAVE_FORMAT_VERSION = 1;

        /// <summary>[FIXED] Regulation match length, minutes (Laws of the Game — two 45-minute
        /// halves). Stage 0 models no stoppage time and no extra time; the engine's match-length
        /// model is exactly this many minutes of 60 Hz ticks (see <see cref="MATCH_TICKS_TOTAL"/>).
        /// Mirrors <c>TestingStrategyConstants.MATCH_LENGTH_MINUTES</c> (an infrastructure assembly
        /// game code cannot reference — both derive independently from the Laws of the Game).</summary>
        public const int MATCH_LENGTH_MINUTES = 90;

        /// <summary>[FIXED] Seconds a goalkeeper may control the ball before releasing it (Laws of the
        /// Game, Law 12). A rule of the sport, not a tunable — see <see cref="GkMaxHoldTicks"/>.</summary>
        public const float GK_MAX_HOLD_SECONDS = 6f;

        /// <summary>[FIXED] Six-yard-box depth from the goal line (Laws of the Game §1 — the goal
        /// area), metres. Used to place the Stage-0 goal-kick restart position (design note §5).</summary>
        public const float GOAL_AREA_DEPTH_M = 5.5f;

        /// <summary>[FIXED] Bench size per team (match-day substitutes). Design note §6.</summary>
        public const int SUBSTITUTES_PER_TEAM = 7;

        /// <summary>[FIXED] Maximum substitutions permitted per team per match (current IFAB
        /// allowance). Design note §6.</summary>
        public const int MAX_SUBSTITUTIONS_PER_TEAM = 5;

        /// <summary>
        /// [FIXED] Team id awarded the FIRST-half kickoff. Stage 0 has no coin toss (that draw would need
        /// its own registered RNG stream and buys nothing yet), so the home side kicks off — a fixed
        /// convention, not a tunable. Match Engine design note §5.Z (Phase H).
        /// </summary>
        public const int FIRST_HALF_KICKOFF_TEAM = 0;

        /// <summary>
        /// [DERIVED] Team id awarded the SECOND-half kickoff = the side that did not kick off the first
        /// half (Laws of the Game, Law 8). Derived so the two can never drift to the same team.
        /// Source constants: MatchEngineConstants.FIRST_HALF_KICKOFF_TEAM, MatchEngineConstants.TEAM_COUNT.
        /// </summary>
        public const int SECOND_HALF_KICKOFF_TEAM = (FIRST_HALF_KICKOFF_TEAM + 1) % TEAM_COUNT;

        // | 1.27    | 2026-07-26 | —      | §5.Z.9 foul & discipline balance pass. + [GT]                   |
// |         |            |        | FoulCallProbability = 0.015 (the referee-judgement term the     |
// |         |            |        | model lacked); YellowCardProbability 0.35 -> 0.16 and           |
// |         |            |        | RedCardProbability 0.05 -> 0.011 (real-football ratios per      |
// |         |            |        | ~22 fouls); FoulCooldownTicks 60 -> 180 (a restart takes        |
// |         |            |        | several seconds; rate-neutral at the new call probability).     |
// |         |            |        | FoulImpactForceThresholdN stays 1200 but is re-documented as    |
// |         |            |        | the "hard enough to consider" gate, NOT the rate knob — the     |
// |         |            |        | measured force distribution is bounded at ~2362 N, so the       |
// |         |            |        | threshold is a cliff (480 fouls at 1200 N, 90 at 2000, 0 at     |
// |         |            |        | 3000) and cannot carry a rate at all.                          |
// | 1.28    | 2026-07-26 | —      | §5.Z.10: + [CROSS] GkKickoffDepthM, mirroring                    |
// |         |            |        | PositioningAIConstants.GK_DEPTH_M (the resting depth #12's own  |
// |         |            |        | ComputeGkSlot yields for a centre-spot ball), so the kickoff    |
// |         |            |        | keeper spawn and the positioning model agree by construction    |
// |         |            |        | instead of drifting apart.                                     |
// | 1.29    | 2026-07-26 | —      | §5.Z.12 per-side pairs removed. HomeLineXM + AwayLineXM ->      |
// |         |            |        | one OutfieldKickoffLineXM stated in the own-half frame (the     |
// |         |            |        | away line is it mirrored); HOME_FACING_DEG + AWAY_FACING_DEG    |
// |         |            |        | deleted outright, since facing is now MirrorVelocityIfAway of   |
// |         |            |        | +X and needs no degrees. Each deleted pair stated one fact      |
// |         |            |        | twice, which is the drift surface behind ERR-008-002,           |
// |         |            |        | ERR-013-009/010 and the §5.Z.10 keeper spawn.                   |
#endregion

        #region Derived

        /// <summary>
        /// [DERIVED] Kickoff ball X (centre spot) = PITCH_LENGTH_M / 2, metres.
        /// Source constants: MatchEngineConstants.PITCH_LENGTH_M.
        /// </summary>
        public static readonly float KickoffBallXM = PITCH_LENGTH_M / 2f;

        /// <summary>
        /// [DERIVED] Kickoff ball Y (centre spot) = PITCH_WIDTH_M / 2, metres.
        /// Source constants: MatchEngineConstants.PITCH_WIDTH_M.
        /// </summary>
        public static readonly float KickoffBallYM = PITCH_WIDTH_M / 2f;

        /// <summary>
        /// [DERIVED] Outfield kickoff line X = PITCH_LENGTH_M / 4, metres, expressed in the acting team's
        /// OWN-HALF frame — the away side's line is this value mirrored, not a second constant.
        /// Source constants: MatchEngineConstants.PITCH_LENGTH_M.
        ///
        /// <para>Phase-A scaffolding: outfield agents are moved onto real formation slots by the AI phase
        /// on the first stride tick, so this is a transient starting spread rather than a shape. It is
        /// NOT the keeper's placement — see <see cref="GkKickoffDepthM"/> for why that distinction is
        /// load-bearing (a keeper never moves at Stage 0).</para>
        ///
        /// <para>Replaced the former `HomeLineXM` / `AwayLineXM` pair, which stated the same distance
        /// twice — once per side — and so had two places that had to agree. One own-half value mirrored
        /// through <c>MirrorPitchIfAway</c> has one.</para>
        /// </summary>
        public static readonly float OutfieldKickoffLineXM = PITCH_LENGTH_M / 4f;

        /// <summary>
        /// [DERIVED] Highest EntityId in the match = SQUAD_SIZE − 1 (roster indices 0..SQUAD_SIZE−1
        /// are the agent EntityIds the mechanics-AI ticks key by). Sizes the Positioning AI (#12)
        /// per-team EntityId→slot lookups (Phase D D2). Source constants: MatchEngineConstants.SQUAD_SIZE.
        /// </summary>
        public static readonly int MaxEntityId = SQUAD_SIZE - 1;

        /// <summary>
        /// [DERIVED] Total 60 Hz ticks in a regulation match = MATCH_LENGTH_MINUTES × 60 s ×
        /// PHYSICS_TICK_HZ = 90 × 60 × 60 = 324 000. This is the engine match-length model the #26
        /// adaptation ladder's <c>t01 = clamp01(ticksRemaining / MATCH_TICKS_TOTAL)</c> divides by
        /// (#26 §3.4 FM-TP-04 — the constant #26 §3.5 carried as <c>[CROSS-PENDING]</c>, engine-owned
        /// and now allocated here; the consuming ladder takes it as an explicit parameter because
        /// the tactical-instructions assembly sits below this one in the reference graph).
        /// Stage 0 scope: <c>ticksRemaining</c> (the #26 adaptation-ladder input) clamps at 0 for
        /// ticks beyond this constant; the engine ITSELF freezes gameplay at this tick per
        /// <c>MatchEngine.CheckMatchFlowTransitions</c> (design note §7, landed 2026-07-14) — a host
        /// that keeps calling RunTick past full time gets a frozen, still-serializable match.
        /// Source constants: MatchEngineConstants.MATCH_LENGTH_MINUTES,
        /// DeterministicSimConstants.PHYSICS_TICK_HZ (Deterministic Simulation #16 §3.1.2).
        /// </summary>
        public const long MATCH_TICKS_TOTAL =
            MATCH_LENGTH_MINUTES * 60L * TacticalDirector.DeterministicSim.DeterministicSimConstants.PHYSICS_TICK_HZ;

        /// <summary>
        /// [DERIVED] The half-time boundary tick = MATCH_TICKS_TOTAL / 2 = 162 000 (the first tick
        /// of the second half). The #26 decision gate fires its half-time decision at the first stride
        /// evaluation at or after this tick (see <see cref="ManagerDecisionGate"/>). The engine ALSO
        /// marks the transition here (design note §7, landed 2026-07-14) —
        /// <c>MatchEngine.CheckMatchFlowTransitions</c> resets the ball to the centre spot, clears
        /// possession, and publishes <c>MatchPhaseChangedEvent</c>, exactly once, the first tick at or
        /// after this boundary. <b>NOT a full ends-swap</b> (AR-4): agent positions and the fixed
        /// <c>team 0 attacks +X</c> convention are left untouched — that convention is hardcoded across
        /// goal detection, <c>OffsideEvaluator</c>, and every Mechanics-AI <c>MirrorPitchIfAway</c> call,
        /// so repositioning agents without also flipping it everywhere would break second-half goal/
        /// offside classification. The true ends-swap is a documented Stage-1+ deferral.
        /// Source constants: MatchEngineConstants.MATCH_TICKS_TOTAL.
        /// </summary>
        public const long HALF_TIME_BOUNDARY_TICK = MATCH_TICKS_TOTAL / 2;

        #endregion

        #region GT

        /// <summary>
        /// [GT] Stage-0 neutral mid-scale player attribute [1–20] — the pre-#27-T1 seed the pass/shot
        /// executor adapters synthesised while Agent Movement #2 carried no passing/finishing/
        /// technique fields (ERR-007). DECLARED-BUT-UNCONSUMED in production since #27 T1 (the
        /// attribute split landed — every seeding site now projects from the canonical player record
        /// via PlayerAttributeProjection, whose neutral projection equals this value); retained as
        /// the pre-T1 seed REFERENCE the KD-P7 neutral-equivalence locks assert against
        /// (PlayerAttributeProjectionTests) — the byte-identity contract anchor, not dead weight.
        /// </summary>
        public static readonly float STAGE0_NEUTRAL_ATTRIBUTE = 10f;

        /// <summary>
        /// [GT] Stage-0 neutral weak-foot rating [1–5] — the pre-#27-T1 pass/shot adapter seed.
        /// DECLARED-BUT-UNCONSUMED in production since #27 T1 (see STAGE0_NEUTRAL_ATTRIBUTE);
        /// retained as the KD-P7 neutral-equivalence reference value.
        /// </summary>
        public static readonly int STAGE0_NEUTRAL_WEAK_FOOT = 3;

        /// <summary>
        /// [GT] Stage-0 formation archetype assigned to BOTH teams (Phase D D2). The Positioning AI
        /// (#12) formation table is authored attack-toward-+X; the host maps the away team into that
        /// canonical frame, so a single shared archetype positions both teams correctly. Replaced by a
        /// per-team tactical selection when the [GT] config loader lands (Stage 1).
        /// </summary>
        public static readonly FormationFamily STAGE0_FORMATION =
            (FormationFamily)Enum.Parse(typeof(FormationFamily), Config.GetString("match-engine", "STAGE0_FORMATION", "F442"));

        /// <summary>
        /// [GT] Stage-0 team tactical-intensity input [0,1] supplied to Positioning AI (#12)
        /// ContextModifierInputs (Phase D D2). Mid-scale placeholder until per-archetype tactical
        /// instructions wire in (Stage 1, #21 / FR-PA-018 / FR-PA-032).
        /// </summary>
        public static readonly float STAGE0_TACTICAL_INTENSITY = Config.GetFloat("match-engine", "STAGE0_TACTICAL_INTENSITY", 0.5f);

        /// <summary>
        /// [GT] Capacity of the per-team <c>PassEventRing</c> feeding the Pressing AI (#13) BackwardPass
        /// trigger (Phase D D2b). Stage 0 publishes no pass events into the ring (no carrier exists yet),
        /// so the trigger never fires; the small ring is allocated once at boot for the wiring path.
        /// </summary>
        public static readonly int STAGE0_PASS_EVENT_RING_CAPACITY = 16; // TODO: feed real pass events (Stage 1)

        /// <summary>
        /// [GT] Stage-0 default defensive-line depth [0.0 = deepest, 1.0 = highest] supplied to the
        /// Defensive AI (#14) snapshot (Phase D D2b). Mirrors the Decision Tree #8 Stage0Default value;
        /// passed straight through to MarkDirective.OffensiveLineDepth and back into the decision context.
        /// </summary>
        public static readonly float STAGE0_DEFENSIVE_LINE_DEPTH = Config.GetFloat("match-engine", "STAGE0_DEFENSIVE_LINE_DEPTH", 0.5f);

        /// <summary>
        /// [GT] Stage-0 neutral normalised attribute [0,1] — the pre-#27-T1 seed for the Attacking #15
        /// Pace / Dribbling snapshot fields (§2.3 "declared for Stage 1+ use"). DECLARED-BUT-UNCONSUMED
        /// in production since #27 T1 (the fields now carry canonical Pace/Dribbling ÷ ATTRIBUTE_MAX
        /// per projection-design KD-P3, whose neutral projection equals this 0.5); retained as the
        /// KD-P7 neutral-equivalence reference value.
        /// </summary>
        public static readonly float STAGE0_NEUTRAL_NORMALIZED = 0.5f;

        /// <summary>
        /// [GT] Host reach (m) within which a loose, approaching ground ball triggers a first-touch
        /// attempt by the nearest eligible agent (Phase D D3). This is the host-side trigger gate — the
        /// decision to ATTEMPT a touch — distinct from the First Touch (#4) §3.2 OUTPUT displacement
        /// radius. A ball outside this reach has not yet "arrived" at the agent; one inside it, closing
        /// on the agent, is a receive. Mid-scale placeholder pending the Stage-1 config loader.
        /// </summary>
        public static readonly float FIRST_TOUCH_ACCEPTANCE_RADIUS_M = Config.GetFloat("match-engine", "FIRST_TOUCH_ACCEPTANCE_RADIUS_M", 1.0f);

        /// <summary>
        /// [GT] Minimum ball speed (m/s) for a first-touch trigger (Phase D D3). Below this the ball is
        /// treated as at-rest — a resting loose ball is not an incoming receive, so an idle agent next to
        /// it does not auto-control it. (The closing-direction gate already excludes a zero-velocity ball,
        /// since its velocity·to-agent dot is 0; this threshold makes the intent explicit and tunable.)
        /// Mid-scale placeholder pending the Stage-1 config loader.
        /// </summary>
        public static readonly float FIRST_TOUCH_MIN_BALL_SPEED_M_S = Config.GetFloat("match-engine", "FIRST_TOUCH_MIN_BALL_SPEED_M_S", 0.5f);

        /// <summary>
        /// [GT] Host reach (m) within which an agent claims a loose ball that has come to REST — the
        /// <see cref="MatchEngine"/> loose-ball pickup (design note §5.Z Phase H, KD-H3). Deliberately a
        /// separate constant from <see cref="FIRST_TOUCH_ACCEPTANCE_RADIUS_M"/>: that gate is the reach at
        /// which an INCOMING ball counts as arriving (a First Touch #4 event, whose control-quality model
        /// is a function of incoming velocity), whereas this is the reach at which a player standing over
        /// a still ball simply has it. The two mechanics are disjoint by construction — pickup requires a
        /// ball BELOW <see cref="FIRST_TOUCH_MIN_BALL_SPEED_M_S"/>, first touch requires it at or above —
        /// so no ball can satisfy both, and they are tunable independently. Mid-scale placeholder pending
        /// the Stage-1 config loader.
        /// </summary>
        public static readonly float LooseBallPickupRadiusM = Config.GetFloat("match-engine", "LooseBallPickupRadiusM", 1.0f);

        /// <summary>
        /// [DERIVED] Ticks a goalkeeper may hold the ball before the engine forces a release — the Laws of
        /// the Game (Law 12) six seconds, expressed at the 60 Hz physics rate. Derived rather than tuned
        /// because it is a Law, not a balance lever.
        /// Source constants: MatchEngineConstants.GK_MAX_HOLD_SECONDS,
        /// DeterministicSimConstants.PHYSICS_TICK_HZ.
        /// See <c>MatchEngine.EnforceGoalkeeperReleaseRule</c> and design note §5.Z.15.
        /// </summary>
        public const int GkMaxHoldTicks =
            (int)(GK_MAX_HOLD_SECONDS
                  * TacticalDirector.DeterministicSim.DeterministicSimConstants.PHYSICS_TICK_HZ);

        /// <summary>
        /// [GT] Ticks after a forced goalkeeper release during which THAT keeper may not be selected as the
        /// loose-ball collector. Without it the keeper re-collects the ball it has just put down on the very
        /// next tick and the stall re-arms; with it the ball falls to the nearest outfielder, which is the
        /// shape of a throw-out. Long enough for a covering defender to close, short enough that the ball is
        /// not abandoned. Config key [match-engine] GkReleaseCooldownTicks.
        /// </summary>
        public static readonly int GkReleaseCooldownTicks =
            Config.GetInt("match-engine", "GkReleaseCooldownTicks", 120);

        /// <summary>
        /// [CROSS] Distance (m) from its own goal line at which a goalkeeper is spawned at kickoff,
        /// centred on the goal mouth. Authoritative source:
        /// <c>PositioningAIConstants.GK_DEPTH_M</c> (Positioning AI #12 §3.4 — the resting depth its
        /// <c>ComputeGkSlot</c> produces for a ball at the centre spot), mirrored here read-only so the
        /// boot placement and the positioning model agree instead of drifting.
        ///
        /// Load-bearing well past kickoff: the Physics phase skips goalkeepers at Stage 0 (GK locomotion
        /// is Goalkeeper Mechanics #11), so a keeper stands where boot puts it for the entire match.
        /// </summary>
        public static readonly float GkKickoffDepthM =
            TacticalDirector.PositioningAI.PositioningAIConstants.GK_DEPTH_M;

        /// <summary>
        /// [GT] Minimum <c>ContactForceData.ForceMagnitude</c> (N) for a FROM_BEHIND agent-agent
        /// collision to qualify as a CANDIDATE foul — the "hard enough for the referee to consider it"
        /// gate (design note §3; balance pass `foul-discipline-balance-design.md` §4.1). Sits at the p99
        /// of the measured cross-team from-behind force distribution, deliberately in the meaningful
        /// part of the band rather than on its last few samples: the distribution is bounded at ~2400 N
        /// (a collision impulse over <c>ContactDurationS</c> cannot exceed it), so a threshold chosen up
        /// on the tail would read as calibrated while actually being noise. The foul RATE is carried by
        /// <see cref="FoulCallProbability"/>, not by this value.
        /// Config key [match-engine] FoulImpactForceThresholdN.
        /// </summary>
        public static readonly float FoulImpactForceThresholdN = Config.GetFloat("match-engine", "FoulImpactForceThresholdN", 1200f);

        /// <summary>
        /// [GT] Probability that a candidate contact AT the force threshold is actually whistled — the
        /// referee-judgement term (`foul-discipline-balance-design.md` KD-F1). The applied probability
        /// scales with force, <c>p(F) = min(1, FoulCallProbability × F / FoulImpactForceThresholdN)</c>,
        /// so a harder challenge is likelier to be given while a hard contact is never automatically a
        /// foul.
        ///
        /// Calibrated (not guessed) against a ~22-fouls-per-90-minutes target, measured end-to-end on
        /// real composed play rather than predicted.
        ///
        /// RE-CALIBRATED 2026-07-27 (§5.Z.13), 0.015 → 0.030, because the denominator moved: the
        /// collision system now emits one <c>CollisionEvent</c> per CONTACT rather than one per tick of
        /// a sustained overlap, which took cross-team from-behind contacts from 58/s to 0.5/s. The
        /// previous value was explicitly documented as "only meaningful against" that stream, and left
        /// unchanged it gave ~0.4 fouls per 90 minutes. Re-measured with <c>FoulRateDiagnosticTests</c>
        /// exactly as that note instructed. Note the live-vs-sweep correction now runs the OTHER way
        /// from §5.Z.9's: the sweep replays a stream produced at the near-zero shipped rate, so
        /// restoring fouls adds restarts, which stops play and LOWERS the contact count — the live rate
        /// lands at or a little under the sweep's prediction rather than above it.
        /// Config key [match-engine] FoulCallProbability.
        /// </summary>
        public static readonly float FoulCallProbability = Config.GetFloat("match-engine", "FoulCallProbability", 0.030f);

        /// <summary>
        /// [GT] Probability band width [0,1) for a straight red card on a WHISTLED foul (design note §3).
        /// Read from the rescaled remainder of the single <c>match-flow.card-severity</c> draw that also
        /// decided the call (KD-F2): <c>[0, Red)</c> = straight red, <c>[Red, Red+Yellow)</c> = yellow,
        /// else no card. Set from the real-football ratio ~0.25 reds per ~22 fouls (KD-F5).
        /// Config key [match-engine] RedCardProbability.
        /// </summary>
        public static readonly float RedCardProbability = Config.GetFloat("match-engine", "RedCardProbability", 0.011f);

        /// <summary>
        /// [GT] Probability band width [0,1) for a yellow card on a whistled foul (design note §3),
        /// immediately after the <see cref="RedCardProbability"/> band. Set from the real-football ratio
        /// ~3.5 bookings per ~22 fouls (KD-F5). Config key [match-engine] YellowCardProbability.
        /// </summary>
        public static readonly float YellowCardProbability = Config.GetFloat("match-engine", "YellowCardProbability", 0.16f);

        /// <summary>
        /// [GT] Ticks a WHISTLED foul suppresses further foul detection (design note §3) — a global
        /// debounce so one sustained tangle cannot be given twice. 180 ticks = 3 s at 60 Hz, which is
        /// about how long the restart itself takes with the players still gathered; the previous 1 s was
        /// thin. A waved-on candidate arms nothing (KD-F3), so this never suppresses a genuine foul that
        /// follows a no-call. Config key [match-engine] FoulCooldownTicks.
        /// </summary>
        public static readonly int FoulCooldownTicks = Config.GetInt("match-engine", "FoulCooldownTicks", 180);

        // ── GK (#11) / Heading (#10) Stage-0 trigger heuristics ──────────────────────────
        // gk-heading-engine-integration-design.md §4. Conservative world-state gates that fire the
        // save / header intents (seeded from the projections) when no Decision-Tree producer exists —
        // the MatchFlowCollisionConsumer heuristic-foul precedent. Illustrative pending a balance pass
        // (the #21 G2 precedent); the contract under review is the wiring, not the tuned magnitude.

        /// <summary>[GT] Head-contact range (m): the nearest active outfield agent within this radius of
        /// an airborne ball may commit a header (design §4.2). Config key [match-engine] HeaderTriggerRangeM.</summary>
        public static readonly float HeaderTriggerRangeM = Config.GetFloat("match-engine", "HeaderTriggerRangeM", 1.5f);

        /// <summary>[GT] Minimum ball height (m) above ground for the header trigger to consider the ball
        /// airborne (design §4.2). Config key [match-engine] HeaderTriggerMinBallHeightM.</summary>
        public static readonly float HeaderTriggerMinBallHeightM = Config.GetFloat("match-engine", "HeaderTriggerMinBallHeightM", 0.5f);

        /// <summary>[GT] PowerIntent [0,1] the Stage-0 header trigger commits (design §4.2).
        /// Config key [match-engine] HeaderTriggerPowerIntent.</summary>
        public static readonly float HeaderTriggerPowerIntent = Config.GetFloat("match-engine", "HeaderTriggerPowerIntent", 0.7f);

        /// <summary>[GT] Distance (m) from the defended goal line within which a loose on-target ball
        /// arms the keeper's save trigger (design §4.1). Config key [match-engine] GkSaveTriggerRangeM.</summary>
        public static readonly float GkSaveTriggerRangeM = Config.GetFloat("match-engine", "GkSaveTriggerRangeM", 16.5f);

        /// <summary>[GT] Minimum ball speed (m/s) toward the defended goal for the save trigger to fire
        /// (design §4.1). Config key [match-engine] GkSaveTriggerMinBallSpeedMps.</summary>
        public static readonly float GkSaveTriggerMinBallSpeedMps = Config.GetFloat("match-engine", "GkSaveTriggerMinBallSpeedMps", 3.0f);

        /// <summary>[GT] ClutchFirmness [0,1] the Stage-0 save trigger commits (design §4.1).
        /// Config key [match-engine] SaveTriggerClutchFirmness.</summary>
        public static readonly float SaveTriggerClutchFirmness = Config.GetFloat("match-engine", "SaveTriggerClutchFirmness", 0.8f);

        // ── Wiring backlog W1: the keeper rush trigger ────────────────────────────────────
        // gk-rush-trigger-design.md §4. All are new dials for a surface that had no production caller
        // at all (`CommitRushIntent`), so none of them is a KD-W1 retune — there was no prior value to
        // freeze. Every default is a first plausible number, NOT a fitted one: they are the input to the
        // single calibration pass the wiring backlog books after the board is clear, not its output. Do
        // not cite any of them as measured.
        //
        // NOTE: how far the keeper comes out is NOT here. That is §3.7.0's attribute-driven
        // `GoalkeeperRushDispatch.ComputeRushCommitDistanceM`, in #11's own catalogue, because the
        // decision belongs to the keeper rather than to the engine's trigger geometry (ERR-011-010).

        /// <summary>[GT] How far in FRONT of the ball (m, along the goal-to-goal axis) a team-mate must
        /// be to count as goal-side cover. A defender level with the carrier — or chasing him from
        /// behind — is not cover: he narrows no shooting angle, so the keeper still comes out.
        /// Config key [match-engine] GkRushCoverGoalSideMarginM.</summary>
        public static readonly float GkRushCoverGoalSideMarginM = Config.GetFloat("match-engine", "GkRushCoverGoalSideMarginM", 2.0f);

        /// <summary>[GT] Half-width (m) of the corridor around the ball → goal-centre line inside which a
        /// goal-side team-mate counts as cover. A full-back stranded on the far touchline is goal-side of
        /// a central ball and blocks nothing. Config key [match-engine] GkRushCoverCorridorHalfWidthM.</summary>
        public static readonly float GkRushCoverCorridorHalfWidthM = Config.GetFloat("match-engine", "GkRushCoverCorridorHalfWidthM", 6.0f);

        /// <summary>[GT] Longest run (s, at the keeper's own rush speed) he will commit to — the single
        /// time budget applied to both trigger branches. For a loose ball it is exactly the intercept
        /// cap, since the solve places the meeting point at rushSpeed × t; for an opponent carrying the
        /// ball it is the same budget expressed as a distance, there being no race to solve. It also
        /// bounds the straight-line ball extrapolation the solve relies on, the role
        /// <c>DivePredictionHorizonS</c> plays for the §3.3.4 dive prediction.
        /// Config key [match-engine] GkRushMaxInterceptS.</summary>
        public static readonly float GkRushMaxInterceptS = Config.GetFloat("match-engine", "GkRushMaxInterceptS", 2.0f);

        /// <summary>[GT] Ball height (m) above which a rush is refused — that ball is a cross to be
        /// claimed (wiring backlog W3, not wired), not one to be swept.
        /// Config key [match-engine] GkRushMaxBallHeightM.</summary>
        public static readonly float GkRushMaxBallHeightM = Config.GetFloat("match-engine", "GkRushMaxBallHeightM", 2.5f);

        /// <summary>[GT] CommitmentLevel [0,1] the Stage-0 rush trigger writes into the RushIntent. MUST
        /// exceed <c>GoalkeeperConstants.RushCommitThreshold</c> (0.60) or #11's Set/Anticipate → Rushing
        /// rows ignore the intent entirely. Config key [match-engine] GkRushCommitment.</summary>
        public static readonly float GkRushCommitment = Config.GetFloat("match-engine", "GkRushCommitment", 0.85f);

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-16 | —      | Initial implementation (Phase A skeleton). |
// | 1.1     | 2026-06-16 | —      | AR-1 L-1: retagged kickoff/line constants. KICKOFF_BALL_X/Y + |
// |         |            |        | HOME/AWAY_LINE_X are now [DERIVED] (PascalCase, formula from   |
// |         |            |        | pitch dims) instead of [FIXED] placeholders; PITCH_LENGTH_M    |
// |         |            |        | added as the derivation source. Facing headings kept [FIXED]  |
// |         |            |        | (fixed kickoff orientation, not tunable).                     |
// | 1.2     | 2026-06-16 | —      | Phase B step B2: PHASE_A_PAYLOAD_FORMAT_VERSION bumped 1 → 2   |
// |         |            |        | — interim payload now sourced from real BallState/AgentState   |
// |         |            |        | and agent facing serialized as a 2-component direction.        |
// | 1.3     | 2026-06-16 | —      | Phase B step B3: PHASE_A_PAYLOAD_FORMAT_VERSION (byte) replaced |
// |         |            |        | with SNAPSHOT_SCHEMA_VERSION (uint = 1) — the design-note §2.6  |
// |         |            |        | schema pin for the full world-state field set now serialized by |
// |         |            |        | SerializeWorldState. Doc distinguishes it from the #16          |
// |         |            |        | SnapshotHeader SNAPSHOT_SCHEMA_VERSION (header framing vs body).|
// | 1.4     | 2026-06-19 | —      | Phase C C1/C1a: NO_POSSESSION sentinel ([FIXED] −1, mirrors     |
// |         |            |        | MatchContext.PossessingAgentId) for the host possession field;  |
// |         |            |        | STAGE0_NEUTRAL_ATTRIBUTE / STAGE0_NEUTRAL_WEAK_FOOT ([GT]) feed |
// |         |            |        | the pass/shot executor query adapters until the ERR-007         |
// |         |            |        | attribute split wires real attributes in (Phase D). New GT      |
// |         |            |        | region added after Derived.                                    |
// | 1.5     | 2026-06-22 | —      | Phase C C5: SNAPSHOT_SCHEMA_VERSION bumped 1 → 2 — the world-    |
// |         |            |        | state body now also serializes the per-agent Pass/Shot executor |
// |         |            |        | in-flight state (C0 capture) + the authoritative MatchContext   |
// |         |            |        | (folds in the possessing-agent id). Doc records the v1/v2 split. |
// | 1.6     | 2026-06-22 | —      | Phase D D1: PERCEPTION_GRID_POINT_INSERT_RADIUS ([FIXED] 0 m)   |
// |         |            |        | added to the Fixed region — the host point-inserts agents into  |
// |         |            |        | the perception broad-phase grid each AI tick. SNAPSHOT_SCHEMA_  |
// |         |            |        | VERSION unchanged (DT/perception cross-tick serialization is    |
// |         |            |        | the D4 step).                                                   |
// | 1.7     | 2026-06-22 | —      | Phase D D2: MaxEntityId ([DERIVED] SQUAD_SIZE−1) sizes the      |
// |         |            |        | Positioning AI (#12) per-team EntityId→slot lookups;            |
// |         |            |        | STAGE0_FORMATION ([GT] F442) + STAGE0_TACTICAL_INTENSITY ([GT]  |
// |         |            |        | 0.5) feed the per-team formation tick. SNAPSHOT_SCHEMA_VERSION  |
// |         |            |        | unchanged (positioning hysteresis serialization is the D4 step).|
// | 1.8     | 2026-06-22 | —      | Phase D D3 (first-touch): FIRST_TOUCH_ACCEPTANCE_RADIUS_M ([GT] |
// |         |            |        | 1.0 m, host trigger reach) + FIRST_TOUCH_MIN_BALL_SPEED_M_S     |
// |         |            |        | ([GT] 0.5 m/s) gate the Resolve-phase first-touch trigger.      |
// |         |            |        | SNAPSHOT_SCHEMA_VERSION unchanged — FirstTouchSystem is         |
// |         |            |        | stateless; it writes only _ball + _possessingAgentId (already   |
// |         |            |        | serialized, the latter via MatchContext.PossessingAgentId).     |
// | 1.9     | 2026-06-26 | —      | Phase D D2b (Pressing #13 / Defensive #14 / Attacking #15):     |
// |         |            |        | STAGE0_PASS_EVENT_RING_CAPACITY ([GT] 16) sizes the per-team    |
// |         |            |        | PassEventRing; STAGE0_DEFENSIVE_LINE_DEPTH ([GT] 0.5) feeds the |
// |         |            |        | Defensive snapshot (→ MarkDirective.OffensiveLineDepth carrier);|
// |         |            |        | STAGE0_NEUTRAL_NORMALIZED ([GT] 0.5) for the unconsumed [0,1]   |
// |         |            |        | Attacking pace/dribbling fields. SNAPSHOT_SCHEMA_VERSION         |
// |         |            |        | unchanged (mechanics hysteresis serialization is the D4 step).  |
// | 1.10    | 2026-06-27 | —      | Phase D D4: SNAPSHOT_SCHEMA_VERSION 2 → 3 — the per-agent       |
// |         |            |        | DecisionTree state machine (D0 DecisionTreeState capture, ×22)  |
// |         |            |        | is now serialized into the world-state body. v3 doc paragraph   |
// |         |            |        | added; perception + per-team mechanics hysteresis remain        |
// |         |            |        | excluded (no get/restore seam yet — follow-up extension).       |
// | 1.11    | 2026-06-27 | —      | Phase D D4 (cont.): SNAPSHOT_SCHEMA_VERSION 3 → 4 — the per-    |
// |         |            |        | team Positioning AI (#12) HysteresisState is now serialized.    |
// |         |            |        | v4 doc paragraph added; perception + Pressing/Defensive/        |
// |         |            |        | Attacking hysteresis still excluded (no seam yet).             |
// | 1.12    | 2026-06-27 | —      | Phase D D4 (cont.): SNAPSHOT_SCHEMA_VERSION 4 → 5 — the per-    |
// |         |            |        | team Pressing AI (#13) PressingTickState is now serialized.     |
// |         |            |        | v5 doc paragraph added; perception + Defensive/Attacking        |
// |         |            |        | hysteresis still excluded (no seam yet).                       |
// | 1.13    | 2026-06-27 | —      | Phase D D4 (cont.): SNAPSHOT_SCHEMA_VERSION 5 → 7 — Defensive   |
// |         |            |        | AI (#14, v6) DefensiveTickState + Attacking AI (#15, v7)        |
// |         |            |        | AttackingTickState now serialized. v6/v7 doc paragraphs added;  |
// |         |            |        | perception internal state is the only remaining exclusion.     |
// | 1.14    | 2026-06-27 | —      | Phase D D4 (final): SNAPSHOT_SCHEMA_VERSION 7 → 8 — Perception  |
// |         |            |        | (#7, v8) PerceptionTickState now serialized. v8 doc paragraph   |
// |         |            |        | added; cross-tick coverage complete (no gameplay state left     |
// |         |            |        | excluded — only boot-deterministic constants + observation).   |
// | 1.15    | 2026-06-27 | —      | Phase E: POSSESSION_CHANGE_REASON_UNSPECIFIED ([FIXED] byte 0)  |
// |         |            |        | added to the Fixed region — the Stage-0 reason ordinal written  |
// |         |            |        | into the possession-changed event (#17 0x04) payload (no reason |
// |         |            |        | taxonomy yet). SNAPSHOT_SCHEMA_VERSION unchanged (world-state    |
// |         |            |        | body untouched; only the serialized ledger carries the event).  |
// | 1.16    | 2026-06-29 | —      | #21 / ERR-021-002: SNAPSHOT_SCHEMA_VERSION 8 → 9 — the per-team  |
// |         |            |        | active + pending TeamTactic is now serialized into the world-    |
// |         |            |        | state body (Appendix B order), so a mid-match tactic change is   |
// |         |            |        | restore-deterministic. v9 doc paragraph added.                  |
// | 1.17    | 2026-06-30 | —      | #21 §3.3: SNAPSHOT_SCHEMA_VERSION 9 → 10 — the per-agent active  |
// |         |            |        | + pending PlayerTactic (×SQUAD_SIZE) is now serialized, so a     |
// |         |            |        | mid-match per-agent tactic change is restore-deterministic. v10  |
// |         |            |        | doc paragraph added.                                            |
// | 1.18    | 2026-07-11 | —      | #23/#24/#25 wiring: SNAPSHOT_SCHEMA_VERSION 11 → 12 — per-agent  |
// |         |            |        | marking dwell (#23), per-team build-up zone state + settled-     |
// |         |            |        | possession-team tracker (#24), per-team rotation binding/cache/  |
// |         |            |        | pair state (#25), and the three #21 back-prop dials appended to  |
// |         |            |        | WriteTeamTactic. v12 doc paragraph added. (The v10 → 11 bump of  |
// |         |            |        | 2026-07-07 predates this row — its doc paragraph was added       |
// |         |            |        | without a history row here; recorded now for completeness.)     |
// | 1.19    | 2026-07-11 | —      | #26 manager-AI wiring: SNAPSHOT_SCHEMA_VERSION 12 → 13 — the     |
// |         |            |        | per-team ManagerState (Appendix C order) is now serialized, so   |
// |         |            |        | mid-match manager decisions are restore-deterministic (FR-TP-012).|
// |         |            |        | v13 doc paragraph added.                                        |
// | 1.20    | 2026-07-11 | —      | Engine substrate (match-length/halves model + score state, the   |
// |         |            |        | #26 §9.3 upstream deliverables): [FIXED] MATCH_LENGTH_MINUTES +  |
// |         |            |        | [DERIVED] MATCH_TICKS_TOTAL (= 324 000; the #26 §3.5             |
// |         |            |        | [CROSS-PENDING] allocation — ALL_CAPS kept per the spec's own    |
// |         |            |        | token, the AI_PHASE_STRIDE precedent) + [DERIVED]                |
// |         |            |        | HALF_TIME_BOUNDARY_TICK (= 162 000; the FR-TP-019 Stage-0 halves |
// |         |            |        | model — boundary only, no break/end-swap/match-end).             |
// |         |            |        | SNAPSHOT_SCHEMA_VERSION 13 → 14 — per-team goal counts + the     |
// |         |            |        | last-holder tracker serialized (v14 doc paragraph).              |
// | 1.21    | 2026-07-14 | —      | Match-flow completion: GOAL_AREA_DEPTH_M / SUBSTITUTES_PER_TEAM /|
// |         |            |        | MAX_SUBSTITUTIONS_PER_TEAM [FIXED] + FoulImpactForceThresholdN / |
// |         |            |        | RedCardProbability / YellowCardProbability / FoulCooldownTicks   |
// |         |            |        | [GT] added; MATCH_TICKS_TOTAL / HALF_TIME_BOUNDARY_TICK docs     |
// |         |            |        | updated (half-time ends-swap + full-time freeze now implemented, |
// |         |            |        | see MatchEngine.CheckMatchFlowTransitions). SNAPSHOT_SCHEMA_     |
// |         |            |        | VERSION 14 → 15 (v15 doc paragraph — discipline/substitution/    |
// |         |            |        | match-flow-clock cross-tick fields).                             |
// | 1.22    | 2026-07-17 | —      | #27 T1 repeat-AR (doc-only): the three STAGE0_NEUTRAL_* docs     |
// |         |            |        | still said "until the ERR-007 attribute split lands" — it landed |
// |         |            |        | (#27 T1); stale TODOs retired, constants re-documented as        |
// |         |            |        | production-unconsumed pre-T1 seed REFERENCES retained for the    |
// |         |            |        | KD-P7 neutral-equivalence locks. Values unchanged.               |
// | 1.23    | 2026-07-18 | —      | #27 T3 (squad-roster-reference-design.md): [FIXED]              |
// |         |            |        | NO_ROSTER_CLUB_ID (−1 sentinel, KD-T3-1) added; SNAPSHOT_SCHEMA_ |
// |         |            |        | VERSION 15 → 16 (v16 doc paragraph — per-team roster reference,  |
// |         |            |        | the loaded Squad.ClubId, a boot-constant identity field so a save|
// |         |            |        | records which squad each team loaded; KD-T3-2 configured ≠       |
// |         |            |        | unconfigured by design).                                        |
// | 1.24    | 2026-07-20 | —      | Snapshot-deserialize (snapshot-deserialize-design.md) Phase 1   |
// |         |            |        | KD-8 writer half: SNAPSHOT_SCHEMA_VERSION 16 → 17 — the         |
// |         |            |        | match-flow.card-severity RngStreamState cursor (RngCursor +      |
// |         |            |        | ActionOrdinal, u64 each) is now serialized; it is the engine's   |
// |         |            |        | only mutable RNG stream and was the one cross-tick surface the   |
// |         |            |        | writer omitted, so a save after any booking now round-trips      |
// |         |            |        | deterministically. v17 doc paragraph added; the stale v8         |
// |         |            |        | "no cross-tick state excluded" note corrected.                  |
// | 1.25    | 2026-07-22 | —      | GK #11 / Heading #10 engine integration (Phase 1): +6 [GT]      |
// |         |            |        | Stage-0 trigger constants (HeaderTriggerRangeM /                |
// |         |            |        | HeaderTriggerMinBallHeightM / HeaderTriggerPowerIntent /        |
// |         |            |        | GkSaveTriggerRangeM / GkSaveTriggerMinBallSpeedMps /            |
// |         |            |        | SaveTriggerClutchFirmness). No SNAPSHOT_SCHEMA_VERSION change.  |
// | 1.26    | 2026-07-26 | —      | §5.Z Phase H possession bootstrap: + [FIXED]                    |
// |         |            |        |   FIRST_HALF_KICKOFF_TEAM, + [DERIVED]                          |
// |         |            |        |   SECOND_HALF_KICKOFF_TEAM (derived so the two halves cannot    |
// |         |            |        |   drift to the same side, Law 8), + [GT]                         |
// |         |            |        |   LooseBallPickupRadiusM (the KD-H3 pickup reach, deliberately   |
// |         |            |        |   separate from FIRST_TOUCH_ACCEPTANCE_RADIUS_M). No             |
// |         |            |        |   SNAPSHOT_SCHEMA_VERSION change.                               |
// | 1.27    | 2026-07-27 | —      | P1 richer observation frame: [FIXED] NO_RESTART_TEAM (−1)       |
// |         |            |        | sentinel for MatchEngine.RestartAwardedTeam when no restart     |
// |         |            |        | was applied this tick. Mirrors the NO_POSSESSION /              |
// |         |            |        | NO_ROSTER_CLUB_ID sentinel convention. No                       |
// |         |            |        | SNAPSHOT_SCHEMA_VERSION change.                                 |
// | 1.28    | 2026-08-04 | —      | Wiring backlog W1 (the keeper rush trigger): + [FIXED]          |
// |         |            |        | GK_RUSH_SOLVE_EPSILON (the intercept quadratic's branch guard)  |
// |         |            |        | and 5 [GT] — GkRushMaxInterceptS / GkRushMaxBallHeightM /       |
// |         |            |        | GkRushCommitment / GkRushCoverGoalSideMarginM /                 |
// |         |            |        | GkRushCoverCorridorHalfWidthM. How far the keeper comes out is  |
// |         |            |        | deliberately NOT here — that is #11 §3.7.0's attribute-driven   |
// |         |            |        | ComputeRushCommitDistanceM (ERR-011-010). New dials for a       |
// |         |            |        | surface that had NO production caller, so not a KD-W1 retune;   |
// |         |            |        | all four are un-calibrated and are the calibration pass's       |
// |         |            |        | input. No SNAPSHOT_SCHEMA_VERSION change (#11's own already-    |
// |         |            |        | serialized _rushIntentActive is the latch).                     |
#endregion
