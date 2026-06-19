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
        /// v1 is the first full §2.6 field set (ball position/velocity/spin/state + LastValid*
        /// checkpoints; per-agent full <c>AgentState</c> including the B0 <c>OscillationGuard</c>
        /// state, LastValid* checkpoints, team/goalkeeper flags, the two collision-feedback inputs,
        /// and the held <c>MovementCommand</c>). It supersedes the B2-era kinematic-subset
        /// PHASE_A_PAYLOAD_FORMAT_VERSION.</summary>
        public const uint SNAPSHOT_SCHEMA_VERSION = 1;

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
#endregion
