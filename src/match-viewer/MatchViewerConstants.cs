// File:     src/match-viewer/MatchViewerConstants.cs
// Created:  2026-07-02
// Modified: 2026-07-24
// Author:   —
// Spec:     Match viewer (presentation tooling; not a numbered spec — see docs/tracking/match-engine-design.md
//           for the engine it observes) + interactive Unity client (docs/tracking/interactive-unity-client-design.md
//           §5-P0), Code Standards #20 (constant catalogue; no magic numbers)
// Purpose:  Constant catalogue for the minimal match viewer: recording defaults plus the HTML replay
//           exporter's pitch-marking geometry and canvas presentation values.

using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

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
        public static readonly float PixelsPerMetre = Config.GetFloat("match-viewer", "PixelsPerMetre", 9f);

        /// <summary>[GT] Canvas margin around the pitch, pixels.</summary>
        public static readonly float CanvasMarginPx = Config.GetFloat("match-viewer", "CanvasMarginPx", 24f);

        /// <summary>[GT] Outfield-agent marker radius, pixels.</summary>
        public static readonly float AgentRadiusPx = Config.GetFloat("match-viewer", "AgentRadiusPx", 7f);

        /// <summary>[GT] Ball marker radius at ground level, pixels (grows with ball height).</summary>
        public static readonly float BallRadiusPx = Config.GetFloat("match-viewer", "BallRadiusPx", 4f);

        /// <summary>[GT] Extra ball-marker radius per metre of ball height, pixels (cheap 2D height cue).</summary>
        public static readonly float BallRadiusPerMetreHeightPx = Config.GetFloat("match-viewer", "BallRadiusPerMetreHeightPx", 2f);

        #endregion

        #region GT — recording defaults

        /// <summary>
        /// [GT] Default recorder sample stride, physics ticks per captured frame. 2 ⇒ 30 samples/s
        /// at the 60 Hz physics tick — smooth playback at half the file size of per-tick capture.
        /// A config-supplied non-positive value is rejected fail-loud by the recorder's stride
        /// guard on the default-stride overloads (this catalogue does not re-validate).
        /// </summary>
        public static readonly int DefaultSampleStride = Config.GetInt("match-viewer", "DefaultSampleStride", 2);

        #endregion

        #region GT — live server / streamer (interactive match view)

        /// <summary>[GT] Default loopback TCP port for <c>LiveMatchServer</c>. A caller may pass 0 to bind an OS-chosen ephemeral port instead (used by tests).</summary>
        public static readonly int LiveServerDefaultPort = Config.GetInt("match-viewer", "LiveServerDefaultPort", 8787);

        /// <summary>[GT] Browser <c>/frame</c> poll cadence, milliseconds. 50 ms ⇒ 20 Hz — cheap over loopback and smooth to the eye; independent of the 60 Hz sim tick rate (a presentation sampling choice, not a physics constant).</summary>
        public static readonly int LivePollIntervalMs = Config.GetInt("match-viewer", "LivePollIntervalMs", 50);

        /// <summary>[GT] Slowest allowed <c>LiveMatchStreamer</c> playback-speed multiplier.</summary>
        public static readonly float MinLiveSpeedMultiplier = Config.GetFloat("match-viewer", "MinLiveSpeedMultiplier", 0.25f);

        /// <summary>
        /// [GT] Fastest allowed <c>LiveMatchStreamer</c> playback-speed multiplier. Default 10 so the
        /// interactive Unity client's master-plan speed set {Pause, 1, 3, 5, 10} is deliverable — the
        /// streamer clamps <c>SetSpeedMultiplier</c> to this cap, so a lower cap silently ran 10× as
        /// the cap value (interactive-unity-client-design.md §2 item 2 / §5-P0).
        /// </summary>
        public static readonly float MaxLiveSpeedMultiplier = Config.GetFloat("match-viewer", "MaxLiveSpeedMultiplier", 10f);

        /// <summary>[GT] Maximum bytes <c>LiveMatchServer</c> will read while looking for the end of an HTTP request line before abandoning the connection (abuse/hang guard against a client that never sends CRLF).</summary>
        public static readonly int MaxHttpRequestLineBytes = Config.GetInt("match-viewer", "MaxHttpRequestLineBytes", 8192);

        /// <summary>[GT] Milliseconds <c>LiveMatchStreamer</c>'s pacing loop sleeps between checks while paused (user-paused or auto-paused at full time), instead of spinning.</summary>
        public static readonly int LivePausedPollIntervalMs = Config.GetInt("match-viewer", "LivePausedPollIntervalMs", 100);

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial creation: IFAB marking geometry [FIXED] + canvas/      |
// |         |            |        | recording presentation values [GT] for the minimal viewer.    |
// | 1.1     | 2026-07-02 | —      | AR-2 M-3: [GT] values migrated onto GameplayConfig             |
// |         |            |        | (Config.GetFloat/GetInt, "match-viewer" section) — this       |
// |         |            |        | catalogue was authored plain-const the day after the June-30  |
// |         |            |        | 17-catalogue migration. [FIXED] IFAB rows stay const.         |
// | 1.2     | 2026-07-02 | —      | AR-4 L (doc): DefaultSampleStride notes that a config-        |
// |         |            |        | supplied non-positive value is rejected by the recorder's     |
// |         |            |        | stride guard (catalogue does not re-validate).                |
// | 1.3     | 2026-07-15 | —      | Interactive match view: new [GT] region for LiveMatchServer /  |
// |         |            |        | LiveMatchStreamer — default port, browser poll cadence, min/  |
// |         |            |        | max speed multiplier, max HTTP request-line bytes.             |
// | 1.4     | 2026-07-24 | —      | Interactive Unity client §5-P0: MaxLiveSpeedMultiplier default |
// |         |            |        | 8 → 10 so the master-plan speed set {Pause,1,3,5,10} is         |
// |         |            |        | deliverable (the streamer clamps to this cap).                 |
#endregion
