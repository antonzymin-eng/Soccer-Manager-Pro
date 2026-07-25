// File:     src/ui-framework/LiveMatchStreamerFrameSource.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     UI / Client Framework #38 §3.4 (FR-UI-005/015),
//           docs/tracking/ui-framework-t0-implementation-plan.md KD-U7, Code Standards #20
// Purpose:  Adapts the real LiveMatchStreamer to the framework's ILiveFrameSource read seam — the
//           production wiring of the match view onto a running match.

using System;

using TacticalDirector.MatchViewer;

namespace TacticalDirector.UiFramework
{
    /// <summary>
    /// Adapts <see cref="LiveMatchStreamer"/> to <see cref="ILiveFrameSource"/>. A pure pass-through:
    /// the streamer already publishes each frame under lock as a complete snapshot, so there is nothing
    /// to add and deliberately nothing to cache here — the caching F5 needs lives one layer up in
    /// <see cref="MatchViewModelSource"/>, where it belongs to the view rather than to the transport.
    /// <para>
    /// Note what this adapter does NOT expose: the streamer's <c>Start</c>/<c>Stop</c>/pause/speed
    /// surface stops here. A match view holding this sees exactly one capability — read the latest
    /// frame — which is the pure-observation contract (FR-UI-005) made structural.
    /// </para>
    /// </summary>
    public sealed class LiveMatchStreamerFrameSource : ILiveFrameSource
    {
        private readonly LiveMatchStreamer _streamer;

        /// <summary>Wraps <paramref name="streamer"/>.</summary>
        /// <param name="streamer">The streamer publishing frames. Must not be null.</param>
        public LiveMatchStreamerFrameSource(LiveMatchStreamer streamer)
        {
            _streamer = streamer ?? throw new ArgumentNullException(nameof(streamer));
        }

        /// <inheritdoc/>
        public bool TryGetLatestFrame(out LiveMatchFrame frame) => _streamer.TryGetLatestFrame(out frame);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-25 | —      | Initial creation (#38 T0): split out of ILiveFrameSource.cs at |
// |         |            |        | code AR pass-2 (the ScenarioIndexEntry file-naming precedent). |
#endregion
