// File:     src/match-client-core/MatchControlAvailability.cs
// Created:  2026-08-07
// Modified: 2026-08-07
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P5, §6.2,
//           §6.3, §12 rule 1), Code Standards #20
// Purpose:  Which match-view controls are live at this instant, and why the locked ones are locked.
//           Resolves §5-P5's "the UI gates tactical input at full time so a click does not silently
//           no-op" into a value the binding reads, so enable/disable is decided in gate-compiled code.

using TacticalDirector.MatchViewer;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// The match view's control-enablement decision (§5-P5), resolved from the latest published
    /// frame.
    ///
    /// <para><b>What this is for.</b> §5-P5 requires the UI to gate tactical input at full time "so a
    /// click does not silently no-op". That is a decision — which controls are live, in which match
    /// state — and §12 rule 1 keeps decisions out of the <c>MonoBehaviour</c>. The binding calls
    /// <see cref="From"/> with what <c>TryGetLatestFrame</c> returned and assigns
    /// <c>interactable</c> from the flags.</para>
    ///
    /// <para><b>THIS IS NOT A SAFETY GUARANTEE, AND MUST NOT BE TREATED AS ONE (§6.2).</b> The
    /// authority on whether a finished match can be mutated is the <i>sim side</i>, which reads the
    /// engine's live <c>_matchEnded</c>. This gate reads the latest published frame, which trails the
    /// sim by at least one frame, so a command can pass it in the window between the sim setting
    /// <c>_matchEnded</c> and the UI observing it. Such a straggler is harmless — it is dropped by the
    /// sim-side guard when next drained, or never drained at all, because the auto-paused sim does not
    /// tick. What this gate buys is honesty in the interface: a control that cannot do anything is
    /// shown as unavailable instead of accepting a click and discarding it. <b>Do not delete a
    /// sim-side guard on the grounds that the UI checks this</b> — that inverts the two, and the
    /// best-effort half is the one that would be left holding the invariant.</para>
    ///
    /// <para><b>Saving stays available at full time, deliberately (§6.3).</b> A completed match is
    /// exactly when a viewer wants to save, and the streamer's <c>ServiceOnce()</c> seam exists so a
    /// capture can be serviced without advancing a tick. Locking save alongside the tactical controls
    /// would make a finished match unsaveable, which §6.3 calls out by name.</para>
    /// </summary>
    public readonly struct MatchControlAvailability
    {
        /// <summary>
        /// True when team/player tactic changes may be offered. False before the first frame and at
        /// full time.
        /// </summary>
        public readonly bool TacticalInputEnabled;

        /// <summary>
        /// True when substitutions may be offered. Tracks <see cref="TacticalInputEnabled"/> today —
        /// both are match-mutating manager intent gated by the same two states — but is a separate
        /// flag because the two diverge the moment a rule does (a substitution window, a used-subs
        /// limit), and a UI binding to one field for two controls could not express that.
        /// </summary>
        public readonly bool SubstitutionEnabled;

        /// <summary>
        /// True when the playback speed ladder and pause/resume may be offered. False before the
        /// first frame (nothing is being paced yet) and at full time (the sim auto-pauses and does
        /// not tick, so a speed change would be inert).
        /// </summary>
        public readonly bool PlaybackControlsEnabled;

        /// <summary>
        /// True when a save may be requested. True in every state, including full time (§6.3) — the
        /// field exists so the binding reads enablement uniformly from this type rather than
        /// hard-coding "save is always on", which is itself a decision and would silently become
        /// wrong the first time a state disallows it.
        /// </summary>
        public readonly bool SaveEnabled;

        /// <summary>
        /// Why the locked controls are locked, or <see cref="MatchControlLockReason.None"/> when
        /// nothing is locked.
        /// </summary>
        public readonly MatchControlLockReason LockReason;

        private MatchControlAvailability(
            bool tacticalInputEnabled,
            bool substitutionEnabled,
            bool playbackControlsEnabled,
            bool saveEnabled,
            MatchControlLockReason lockReason)
        {
            TacticalInputEnabled    = tacticalInputEnabled;
            SubstitutionEnabled     = substitutionEnabled;
            PlaybackControlsEnabled = playbackControlsEnabled;
            SaveEnabled             = saveEnabled;
            LockReason              = lockReason;
        }

        /// <summary>
        /// The availability before any frame has been published — everything match-facing locked,
        /// saving still offered.
        /// </summary>
        public static MatchControlAvailability AwaitingFirstFrame =>
            new MatchControlAvailability(
                tacticalInputEnabled:    false,
                substitutionEnabled:     false,
                playbackControlsEnabled: false,
                saveEnabled:             true,
                lockReason:              MatchControlLockReason.AwaitingFirstFrame);

        /// <summary>The availability during a running match — everything offered.</summary>
        public static MatchControlAvailability Live =>
            new MatchControlAvailability(
                tacticalInputEnabled:    true,
                substitutionEnabled:     true,
                playbackControlsEnabled: true,
                saveEnabled:             true,
                lockReason:              MatchControlLockReason.None);

        /// <summary>
        /// The availability at full time — match-mutating and pacing controls locked, saving still
        /// offered (§6.3).
        /// </summary>
        public static MatchControlAvailability FullTime =>
            new MatchControlAvailability(
                tacticalInputEnabled:    false,
                substitutionEnabled:     false,
                playbackControlsEnabled: false,
                saveEnabled:             true,
                lockReason:              MatchControlLockReason.MatchEnded);

        /// <summary>
        /// Resolves availability from what <c>LiveMatchStreamer.TryGetLatestFrame</c> returned.
        /// </summary>
        /// <param name="hasFrame">The <c>TryGetLatestFrame</c> return value.</param>
        /// <param name="frame">The frame it produced. Ignored entirely when
        /// <paramref name="hasFrame"/> is false, because the out-parameter of a failed
        /// <c>TryGetLatestFrame</c> is <c>default</c> — and <c>default(LiveMatchFrame).MatchEnded</c>
        /// is false, so reading it unconditionally would report a match that has not started as
        /// running.</param>
        public static MatchControlAvailability From(bool hasFrame, in LiveMatchFrame frame)
        {
            if (!hasFrame)
            {
                return AwaitingFirstFrame;
            }

            return frame.MatchEnded ? FullTime : Live;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-07 | —      | Initial creation (§5-P5 host-free half): the three control-     |
// |         |            |        | enablement states with their lock reason, resolved from the     |
// |         |            |        | latest frame. Save stays enabled at full time per §6.3, and     |
// |         |            |        | the type documents at length that it is §6.2's best-effort      |
// |         |            |        | early-out rather than the sim-side guarantee.                   |
#endregion
