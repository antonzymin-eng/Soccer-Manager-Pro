// File:     src/match-engine/MatchEngineConstants.cs
// Created:  2026-06-16
// Modified: 2026-06-16
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §2.3, Code Standards #20
// Purpose:  Constant catalogue for the match-engine composition root. Stage 0 Phase A holds the
//           roster sizing, the coordinate convention (Ball Physics #1 §1.2 corner-origin, Z-up),
//           and the Phase-A snapshot payload format version. Real formation slots are sourced from
//           PositioningAIConstants when the AI phase is wired (Phase D); the Phase-A kickoff line
//           positions are scaffold values derived from pitch geometry only.

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
        /// in the host's possessing-agent id; written each Resolve, read by the next AI tick).</summary>
        public const uint SNAPSHOT_SCHEMA_VERSION = 2;

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
#endregion
