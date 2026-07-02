// File:     src/match-viewer/MatchViewerConstants.cs
// Created:  2026-07-02
// Modified: 2026-07-02
// Author:   —
// Spec:     Match viewer (presentation tooling; not a numbered spec — see docs/tracking/match-engine-design.md
//           for the engine it observes), Code Standards #20 (constant catalogue; no magic numbers)
// Purpose:  Constant catalogue for the minimal match viewer: recording defaults plus the HTML replay
//           exporter's pitch-marking geometry and canvas presentation values.

namespace TacticalDirector.MatchViewer
{
    /// <summary>
    /// Constant catalogue for the match viewer. Presentation-only — nothing here feeds the
    /// simulation, the snapshot digest, or any determinism surface. Pitch DIMENSIONS come from
    /// <c>MatchEngineConstants.PITCH_LENGTH_M/PITCH_WIDTH_M</c> (consumed at the call sites);
    /// the marking geometry below is IFAB Laws of the Game standard distances.
    /// </summary>
    public static class MatchViewerConstants
    {
        #region Fixed — IFAB pitch-marking geometry (metres)

        /// <summary>[FIXED] Centre-circle radius, metres (IFAB Law 1).</summary>
        public const float CentreCircleRadiusM = 9.15f;

        /// <summary>[FIXED] Penalty-area depth from the goal line, metres (16.5 m; IFAB Law 1).</summary>
        public const float PenaltyAreaDepthM = 16.5f;

        /// <summary>[FIXED] Penalty-area width, metres (40.32 m; IFAB Law 1).</summary>
        public const float PenaltyAreaWidthM = 40.32f;

        /// <summary>[FIXED] Goal-area depth from the goal line, metres (5.5 m; IFAB Law 1).</summary>
        public const float GoalAreaDepthM = 5.5f;

        /// <summary>[FIXED] Goal-area width, metres (18.32 m; IFAB Law 1).</summary>
        public const float GoalAreaWidthM = 18.32f;

        /// <summary>[FIXED] Penalty-spot distance from the goal line, metres (IFAB Law 1).</summary>
        public const float PenaltySpotDistanceM = 11f;

        /// <summary>[FIXED] Goal mouth width, metres (7.32 m; IFAB Law 1).</summary>
        public const float GoalWidthM = 7.32f;

        #endregion

        #region GT — canvas presentation (pixels / display tuning)

        /// <summary>[GT] Canvas scale, pixels per metre.</summary>
        public const float PixelsPerMetre = 9f;

        /// <summary>[GT] Canvas margin around the pitch, pixels.</summary>
        public const float CanvasMarginPx = 24f;

        /// <summary>[GT] Outfield-agent marker radius, pixels.</summary>
        public const float AgentRadiusPx = 7f;

        /// <summary>[GT] Ball marker radius at ground level, pixels (grows with ball height).</summary>
        public const float BallRadiusPx = 4f;

        /// <summary>[GT] Extra ball-marker radius per metre of ball height, pixels (cheap 2D height cue).</summary>
        public const float BallRadiusPerMetreHeightPx = 2f;

        #endregion

        #region GT — recording defaults

        /// <summary>
        /// [GT] Default recorder sample stride, physics ticks per captured frame. 2 ⇒ 30 samples/s
        /// at the 60 Hz physics tick — smooth playback at half the file size of per-tick capture.
        /// </summary>
        public const int DefaultSampleStride = 2;

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial creation: IFAB marking geometry [FIXED] + canvas/      |
// |         |            |        | recording presentation values [GT] for the minimal viewer.    |
#endregion
