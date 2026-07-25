// File:     src/ui-framework/ILiveFrameSource.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     UI / Client Framework #38 §3.4 (FR-UI-005/015/016),
//           docs/tracking/ui-framework-t0-implementation-plan.md KD-U7, Code Standards #20
// Purpose:  The one-method read seam the match view projects from. Its whole surface is "hand me the
//           latest published frame", which is what makes the pure-observation contract structural
//           rather than promised: nothing reachable through this seam can advance or mutate a match.
//           The production adapter over LiveMatchStreamer lives in LiveMatchStreamerFrameSource.cs.

using TacticalDirector.MatchViewer;

namespace TacticalDirector.UiFramework
{
    /// <summary>
    /// Supplies the latest complete match frame published by whoever is running the match.
    /// <para>
    /// This exists so the match view depends on a *capability* (read the latest frame) rather than on
    /// <c>LiveMatchStreamer</c> the class. Two things fall out, both load-bearing:
    /// </para>
    /// <list type="bullet">
    /// <item><description>FR-UI-005 becomes structural — a consumer holding only this seam has no engine
    /// to advance, so "the match view never ticks the sim" is enforced by the type system rather than by
    /// review discipline.</description></item>
    /// <item><description>The match view is testable without threads. The real streamer publishes only
    /// from its paced background thread and its manual-step seams are <c>internal</c> to
    /// <c>match-viewer</c>; a stub implementation of this seam gives deterministic tests without
    /// wall-clock pacing and without prising open another assembly.</description></item>
    /// </list>
    /// <para>
    /// Both sides of this seam are built (the <see cref="LiveMatchStreamerFrameSource"/> adapter and the
    /// <see cref="MatchViewModelSource"/> consumer), so it is not the phantom-interface class
    /// ERR-001/ERR-004 forbid.
    /// </para>
    /// </summary>
    public interface ILiveFrameSource
    {
        /// <summary>
        /// Returns the latest complete published frame, or false when none has been published yet.
        /// Never blocks on, waits for, or forces production of a frame (F5).
        /// </summary>
        bool TryGetLatestFrame(out LiveMatchFrame frame);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-25 | —      | Initial creation (#38 T0, plan AR-1 M-2): the frame read seam, |
// |         |            |        | so the match view is thread-free testable and cannot reach an  |
// |         |            |        | engine. Code AR pass-2 Low: the streamer adapter moved to its  |
// |         |            |        | own file (the ScenarioIndexEntry file-naming precedent).       |
#endregion
