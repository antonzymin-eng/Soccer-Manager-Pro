// File:     src/match-client-core/LiveFrameLatch.cs
// Created:  2026-08-15
// Modified: 2026-08-15
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4b, §12
//           rule 1), Code Standards #20
// Purpose:  Decides which of the two most recently captured LiveMatchFrames is "previous" and which
//           is "current" for FrameInterpolator, and when the interpolation clock resets. Extracted
//           (AR pass M-6) from MatchClientBehaviour.AdvanceFrame, which ran this exact state machine
//           inline in a file the CI gate cannot compile — the state machine now lives where the gate
//           compiles and tests it (§12 rule 1: nothing that decides may live in a MonoBehaviour).

using System;

using TacticalDirector.MatchViewer;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// Latches the previous/current frame pair a renderer interpolates between, and the wall-clock
    /// moment the current one arrived.
    ///
    /// <para><b>The three cases <see cref="TryAccept"/> resolves.</b> The FIRST frame ever offered
    /// seeds both <see cref="Previous"/> and <see cref="Current"/> to itself, so the very first
    /// display frame interpolates between a frame and itself rather than reading an unset
    /// <c>default(LiveMatchFrame)</c>. A frame carrying a NEW tick (including a multi-tick jump —
    /// nothing here assumes ticks arrive one at a time) shifts <see cref="Current"/> into
    /// <see cref="Previous"/> and becomes the new <see cref="Current"/>. A frame carrying the SAME
    /// tick as <see cref="Current"/> — the streamer publishes its latest frame far more often than it
    /// advances a tick — is a no-op: accepting it again would reset the arrival clock without a new
    /// tick to interpolate towards, making the render loop think a fresh interval just started.</para>
    ///
    /// <para>Not thread-safe and not reentrant — exactly one caller (the render loop) is expected to
    /// drive it, the same single-writer assumption <see cref="TickStampedCommandReplay"/> makes of
    /// its own cursor.</para>
    /// </summary>
    public sealed class LiveFrameLatch
    {
        private bool _hasFrame;
        private LiveMatchFrame _previous;
        private LiveMatchFrame _current;
        private float _currentArrivalSeconds;

        /// <summary>True once at least one frame has ever been accepted.</summary>
        public bool HasFrame => _hasFrame;

        /// <summary>
        /// The older of the two latched frames — equal to <see cref="Current"/> until a second tick
        /// has been accepted. Reading this before <see cref="HasFrame"/> is true returns
        /// <c>default(LiveMatchFrame)</c>.
        /// </summary>
        public LiveMatchFrame Previous => _previous;

        /// <summary>
        /// The newer of the two latched frames — the one a render frame interpolates towards. Reading
        /// this before <see cref="HasFrame"/> is true returns <c>default(LiveMatchFrame)</c>.
        /// </summary>
        public LiveMatchFrame Current => _current;

        /// <summary>
        /// Offers <paramref name="frame"/> to the latch.
        /// </summary>
        /// <param name="frame">The newest frame read off the streamer this render frame.</param>
        /// <param name="nowSeconds">
        /// Wall-clock seconds (e.g. Unity's <c>Time.time</c>) at which <paramref name="frame"/> was
        /// observed — recorded as <see cref="Current"/>'s arrival time when accepted.
        /// </param>
        /// <returns>
        /// True when <paramref name="frame"/> changed <see cref="Current"/> (the first frame ever, or
        /// a new tick); false when <paramref name="frame"/> repeats the already-latched tick, in
        /// which case nothing about the latch's state changes.
        /// </returns>
        public bool TryAccept(in LiveMatchFrame frame, float nowSeconds)
        {
            if (!_hasFrame)
            {
                _previous = frame;
                _current  = frame;
                _hasFrame = true;
            }
            else if (frame.Tick != _current.Tick)
            {
                _previous = _current;
                _current  = frame;
            }
            else
            {
                return false;
            }

            _currentArrivalSeconds = nowSeconds;
            return true;
        }

        /// <summary>
        /// Wall-clock seconds elapsed since <see cref="Current"/> arrived, given the caller's own
        /// clock reading <paramref name="nowSeconds"/> — the interval
        /// <c>FrameInterpolator.ComputeAlpha</c> blends across.
        /// </summary>
        public float SecondsSinceCurrent(float nowSeconds) => nowSeconds - _currentArrivalSeconds;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-15 | —      | Initial creation (AR pass M-6 over the P4b binding): extracts   |
// |         |            |        | the previous/current frame latch MatchClientBehaviour.          |
// |         |            |        | AdvanceFrame ran inline into a gate-compiled, gate-tested type.  |
// |         |            |        | Covers the three cases the inline version handled: first frame  |
// |         |            |        | ever, a new tick (including a multi-tick jump), and the same    |
// |         |            |        | tick repeating.                                                  |
#endregion
