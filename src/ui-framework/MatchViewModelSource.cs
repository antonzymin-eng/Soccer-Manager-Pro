// File:     src/ui-framework/MatchViewModelSource.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     UI / Client Framework #38 §3.4 (FR-UI-005/006/015/016/017, failure mode F5),
//           docs/tracking/ui-framework-t0-implementation-plan.md KD-U5/KD-U7, Code Standards #20
// Purpose:  The one concrete view-model source the framework ships: projects the latest published match
//           frame into an immutable MatchFrameView at render cadence. Holds no engine, so it cannot
//           advance the sim — observing a match through it is byte-identical to not observing it.

using System;

using TacticalDirector.MatchViewer;

namespace TacticalDirector.UiFramework
{
    /// <summary>
    /// Projects the live match into an immutable <see cref="MatchFrameView"/> (#38 §3.4).
    /// <para>
    /// PURE OBSERVATION (FR-UI-005/015): the source holds an <see cref="ILiveFrameSource"/> and nothing
    /// else. There is no engine handle here to advance even by accident — the observer-neutrality
    /// property (FR-UI-017) follows from the shape of the type, not from care at the call site.
    /// </para>
    /// <para>
    /// F5 — BEFORE THE FIRST FRAME: <see cref="Project"/> returns the last view it produced, or
    /// <see cref="MatchFrameView.Empty"/> if it has never produced one. It never throws and never forces
    /// a tick to manufacture a frame. That last-known cache makes <c>Project()</c> stateful across
    /// calls, which is worth naming against FR-UI-006's "pure": the purity that matters is *no sim
    /// mutation* and *same observed state ⇒ same view*, both of which hold. The cache is client-local
    /// presentation state — it is not sim state, is not serialized, and touches no digest (FR-UI-022).
    /// </para>
    /// <para>
    /// RENDER-THREAD ONLY: not thread-safe, and does not need to be — one renderer reads it. The frame
    /// handoff underneath it (the streamer's lock-guarded publish) is what makes cross-thread *frames*
    /// safe; this cache is single-consumer.
    /// </para>
    /// </summary>
    public sealed class MatchViewModelSource : IViewModelSource<MatchFrameView>
    {
        private readonly ILiveFrameSource _frames;
        private MatchFrameView _lastKnown;

        /// <summary>Projects from an arbitrary frame source.</summary>
        /// <param name="frames">The frame source. Must not be null.</param>
        public MatchViewModelSource(ILiveFrameSource frames)
        {
            _frames    = frames ?? throw new ArgumentNullException(nameof(frames));
            _lastKnown = MatchFrameView.Empty;
        }

        /// <summary>Projects from a live streamer (the ordinary composition).</summary>
        /// <param name="streamer">The streamer publishing frames. Must not be null.</param>
        public MatchViewModelSource(LiveMatchStreamer streamer)
            : this(new LiveMatchStreamerFrameSource(streamer))
        {
        }

        /// <summary>
        /// Reads the latest published frame and projects it. Returns the last-known (or empty) view when
        /// nothing has been published yet (F5). Never advances the sim.
        /// </summary>
        public MatchFrameView Project()
        {
            if (_frames.TryGetLatestFrame(out LiveMatchFrame frame))
            {
                // A malformed frame throws out of the MatchFrameView constructor (F1) and deliberately
                // does NOT poison the cache: _lastKnown is only assigned once construction succeeded.
                _lastKnown = new MatchFrameView(in frame);
            }
            return _lastKnown;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-25 | —      | Initial creation (#38 T0): frame-seam projection with the F5   |
// |         |            |        | last-known cache; holds no engine reference.                   |
#endregion
