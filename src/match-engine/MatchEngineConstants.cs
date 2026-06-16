// File:     src/match-engine/MatchEngineConstants.cs
// Created:  2026-06-16
// Modified: 2026-06-16
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §2.3, Code Standards #20
// Purpose:  Constant catalogue for the match-engine composition root. Stage 0 Phase A holds the
//           roster sizing, the coordinate convention (Ball Physics #1 §1.2 corner-origin, Z-up),
//           and the Phase-A snapshot payload format version. Real formation slots and pitch
//           geometry are sourced from PositioningAIConstants when the AI phase is wired (Phase D).

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Fixed constants for the match-engine composition root.
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

        /// <summary>[FIXED] Pitch width (touchline-to-touchline, Y axis), metres. Ball Physics #1 §1.2.</summary>
        public const float PITCH_WIDTH_M = 68f;

        /// <summary>[FIXED] Kickoff ball X position (centre spot, pitch length midpoint), metres. Ball Physics #1 §1.2.</summary>
        public const float KICKOFF_BALL_X_M = 52.5f;

        /// <summary>[FIXED] Kickoff ball Y position (centre spot, pitch width midpoint), metres. Ball Physics #1 §1.2.</summary>
        public const float KICKOFF_BALL_Y_M = 34f;

        /// <summary>[FIXED] Resting ball-centre height above ground, metres. Ball Physics #1 §1.2 (ball radius).</summary>
        public const float BALL_REST_HEIGHT_M = 0.11f;

        /// <summary>[FIXED] Placeholder home-team line X (own half), metres. Phase A kickoff scaffold only — replaced by formation slots in Phase D.</summary>
        public const float HOME_LINE_X_M = 26.25f;

        /// <summary>[FIXED] Placeholder away-team line X (own half), metres. Phase A kickoff scaffold only — replaced by formation slots in Phase D.</summary>
        public const float AWAY_LINE_X_M = 78.75f;

        /// <summary>[FIXED] Home-team facing heading (toward away goal, +X), degrees.</summary>
        public const float HOME_FACING_DEG = 0f;

        /// <summary>[FIXED] Away-team facing heading (toward home goal, −X), degrees.</summary>
        public const float AWAY_FACING_DEG = 180f;

        /// <summary>[FIXED] Phase-A world-state payload format version. Independent of the
        /// deterministic-sim SNAPSHOT_SCHEMA_VERSION, which is pinned for the full field set in
        /// Phase B (design note §2.6). Bump when the Phase-A field order changes.</summary>
        public const byte PHASE_A_PAYLOAD_FORMAT_VERSION = 1;

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-16 | —      | Initial implementation (Phase A skeleton). |
#endregion
