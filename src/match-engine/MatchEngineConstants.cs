// File:     src/match-engine/MatchEngineConstants.cs
// Created:  2026-06-16
// Modified: 2026-06-27 (Phase E — POSSESSION_CHANGE_REASON_UNSPECIFIED for the possession-changed event)
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

        /// <summary>[FIXED] Home-team kickoff heading: toward the away goal (+X), degrees.
        /// A fixed kickoff orientation, not a tunable.</summary>
        public const float HOME_FACING_DEG = 0f;

        /// <summary>[FIXED] Away-team kickoff heading: toward the home goal (−X), degrees.
        /// A fixed kickoff orientation, not a tunable.</summary>
        public const float AWAY_FACING_DEG = 180f;

        /// <summary>[FIXED] Possessing-agent sentinel for "ball is loose" (no agent has possession).
        /// Mirrors the Decision Tree #8 MatchContext.PossessingAgentId convention (−1 = loose);
        /// the C4 step folds host possession into MatchContext.</summary>
        public const int NO_POSSESSION = -1;

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
        /// re-assembled each AI tick from the serialized team tactic plus the boot identity.</summary>
        public const uint SNAPSHOT_SCHEMA_VERSION = 10;

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
        /// [DERIVED] Phase-A scaffold home-team line X = PITCH_LENGTH_M / 4 (own half), metres.
        /// Placeholder only — replaced by formation slots in Phase D.
        /// Source constants: MatchEngineConstants.PITCH_LENGTH_M.
        /// </summary>
        public static readonly float HomeLineXM = PITCH_LENGTH_M / 4f;

        /// <summary>
        /// [DERIVED] Phase-A scaffold away-team line X = PITCH_LENGTH_M * 3 / 4 (own half), metres.
        /// Placeholder only — replaced by formation slots in Phase D.
        /// Source constants: MatchEngineConstants.PITCH_LENGTH_M.
        /// </summary>
        public static readonly float AwayLineXM = PITCH_LENGTH_M * 3f / 4f;

        /// <summary>
        /// [DERIVED] Highest EntityId in the match = SQUAD_SIZE − 1 (roster indices 0..SQUAD_SIZE−1
        /// are the agent EntityIds the mechanics-AI ticks key by). Sizes the Positioning AI (#12)
        /// per-team EntityId→slot lookups (Phase D D2). Source constants: MatchEngineConstants.SQUAD_SIZE.
        /// </summary>
        public static readonly int MaxEntityId = SQUAD_SIZE - 1;

        #endregion

        #region GT

        /// <summary>
        /// [GT] Stage-0 neutral mid-scale player attribute [1–20] supplied to the pass/shot executor
        /// adapters (Phase C C1a). Agent Movement #2 PlayerAttributes carries no passing/finishing/
        /// technique fields yet (ERR-007 attribute split), so the executor query adapters synthesise a
        /// neutral value until the AI phase wires real attributes in (Phase D).
        /// </summary>
        public static readonly float STAGE0_NEUTRAL_ATTRIBUTE = 10f; // TODO: replace when ERR-007 attribute split lands (Phase D)

        /// <summary>
        /// [GT] Stage-0 neutral weak-foot rating [1–5] supplied to the pass/shot executor adapters
        /// (Phase C C1a). Mid-scale placeholder until the ERR-007 attribute split (Phase D).
        /// </summary>
        public static readonly int STAGE0_NEUTRAL_WEAK_FOOT = 3; // TODO: replace when ERR-007 attribute split lands (Phase D)

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
        /// [GT] Stage-0 neutral normalised attribute [0,1] for the Mechanics-AI snapshot fields the Stage-0
        /// algorithms do not consume (Attacking #15 Pace / Dribbling — §2.3 "declared for Stage 1+ use").
        /// Replaced by real normalised attributes once the ERR-007 attribute split lands.
        /// </summary>
        public static readonly float STAGE0_NEUTRAL_NORMALIZED = 0.5f; // TODO: replace when ERR-007 attribute split lands (Phase D+)

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
#endregion
