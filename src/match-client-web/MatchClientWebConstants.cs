// File:     src/match-client-web/MatchClientWebConstants.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Path-to-playable roadmap §7 (B6 renderer decision — option (b)), interactive Unity
//           client design note §5-P3/§5-P5, Code Standards #20
// Purpose:  Constant catalogue for the browser match client. Presentation and transport only —
//           nothing here reaches the simulation or the snapshot digest.

using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

namespace TacticalDirector.MatchClientWeb
{
    /// <summary>
    /// Catalogue for the PM-1 browser client (roadmap B6).
    ///
    /// <para>Deliberately its own port and its own catalogue rather than sharing
    /// <c>MatchViewerConstants</c>: the spectator viewer and this client are different surfaces with
    /// different privileges — the viewer cannot influence a match and this one can — and running
    /// both at once on one port would be an accident waiting to happen.</para>
    /// </summary>
    public static class MatchClientWebConstants
    {
        #region Fixed

        /// <summary>[FIXED] Maximum bytes read while looking for the end of an HTTP request line —
        /// the hang guard against a client that never sends a terminator. Mirrors the spectator
        /// viewer's guard; a request line longer than this is refused, not buffered.</summary>
        public const int MAX_HTTP_REQUEST_LINE_BYTES = 8_192;

        #endregion

        #region GT

        /// <summary>[GT] Loopback port the client binds by default. Config key [match-client-web] DefaultPort.</summary>
        public static readonly int DefaultPort = Config.GetInt("match-client-web", "DefaultPort", 8181);

        /// <summary>[GT] Browser poll interval (ms) for the frame endpoint. Config key
        /// [match-client-web] FramePollIntervalMs. Deliberately much coarser than the ~16 ms tick:
        /// every poll is its own connection on a <c>Connection: close</c> transport, and ~10 Hz reads
        /// as continuous motion for a spectator view. It is NOT sized for interpolation — the page
        /// draws the latest polled frame directly today (<c>browser-match-client-design.md</c> §2);
        /// wiring <c>FrameInterpolator</c> would want a finer cadence than this.</summary>
        public static readonly int FramePollIntervalMs =
            Config.GetInt("match-client-web", "FramePollIntervalMs", 100);

        /// <summary>[GT] Browser poll interval (ms) for the statistics endpoint. Config key
        /// [match-client-web] ReportPollIntervalMs. Slower than the frame poll: statistics are read,
        /// not watched, and each request takes the analytics lock the sim thread also uses.</summary>
        public static readonly int ReportPollIntervalMs =
            Config.GetInt("match-client-web", "ReportPollIntervalMs", 1_000);

        /// <summary>[GT] Pixels per pitch metre in the rendered canvas. Config key
        /// [match-client-web] PixelsPerMetre.</summary>
        public static readonly float PixelsPerMetre =
            Config.GetFloat("match-client-web", "PixelsPerMetre", 8f);

        /// <summary>[GT] Canvas margin (px) around the pitch rectangle. Config key
        /// [match-client-web] CanvasMarginPx.</summary>
        public static readonly int CanvasMarginPx = Config.GetInt("match-client-web", "CanvasMarginPx", 24);

        /// <summary>[GT] How long (ticks) a restart caption stays on screen. Config key
        /// [match-client-web] RestartCaptionTicks. The frame carries the restart and the tick it
        /// happened on (P1 KD-P1-3); this is how the View turns that into a fading caption.</summary>
        public static readonly int RestartCaptionTicks =
            Config.GetInt("match-client-web", "RestartCaptionTicks", 180);

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (B6): port, poll cadences, canvas scale,      |
// |         |            |        | restart-caption window and the request-line hang guard.        |
// | 1.1     | 2026-07-27 | —      | AR-1 L: region order corrected to the mandated most-immutable- |
// |         |            |        | first (Fixed before GT), and FramePollIntervalMs' doc fixed —  |
// |         |            |        | it claimed 100 ms was "well below the ~16 ms tick" and sized   |
// |         |            |        | for an interpolation the page does not do.                     |
#endregion
