// File:     src/match-client-core/PlaybackSpeedLadder.cs
// Created:  2026-08-07
// Modified: 2026-08-07
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P5, §12
//           rule 1), master plan §3.4 playback-speed set, Code Standards #20
// Purpose:  The ordered playback-speed rungs the P5 speed control offers, and what a faster/slower
//           click means at each end. The catalogue holds the four multipliers; this holds their order
//           and their stepping semantics, so the binding maps a button to an index and decides nothing.

using System;
using System.Globalization;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// The playback-speed ladder the match view presents (§5-P5): the master plan's four multipliers
    /// in ascending order, plus what a "faster" or "slower" click does at the ends.
    ///
    /// <para><b>Why this exists next to the catalogue rather than inside it.</b>
    /// <see cref="MatchClientConstants"/> holds four independent <c>[GT]</c> dials; it does not say
    /// they form a ladder, which one a match opens at, or what happens when the user clicks "faster"
    /// at 10×. Those are decisions, and §12 rule 1 puts decisions in a gate-compiled assembly rather
    /// than in the <c>MonoBehaviour</c> wiring up the buttons. A binding asks for an index and
    /// assigns what comes back.</para>
    ///
    /// <para><b>Pause is not a rung.</b> The master plan's set is {Pause, 1×, 3×, 5×, 10×}, but pause
    /// is a streamer state (<c>LiveMatchStreamer.Pause</c>/<c>Resume</c>), not a multiplier — there is
    /// no multiplier that means "stopped", and 0× is outside the streamer's legal range. So the ladder
    /// carries the four multipliers only, and pause is a separate control. Resuming returns to
    /// whichever rung was selected; pausing does not reset it.</para>
    ///
    /// <para><b>Stepping clamps, it does not wrap.</b> A "faster" click at the top stays at the top.
    /// Wrapping 10× → 1× would silently drop a viewer from the fastest speed to the slowest on a
    /// button they pressed to speed up, which reads as a fault rather than a limit.</para>
    /// </summary>
    public static class PlaybackSpeedLadder
    {
        // Ascending, slowest first. The order is the decision: index 0 is real time, and "faster"
        // means a higher index, so a binding never has to know which direction the array runs.
        private static readonly float[] s_rungs =
        {
            MatchClientConstants.Speed1x,
            MatchClientConstants.Speed3x,
            MatchClientConstants.Speed5x,
            MatchClientConstants.Speed10x,
        };

        /// <summary>How many speed rungs the control offers.</summary>
        public static int RungCount => s_rungs.Length;

        /// <summary>
        /// The rung a match opens at — real time. Named rather than written as <c>0</c> at each call
        /// site, so "a match starts at 1×" is one decision in one place.
        /// </summary>
        public static int RealTimeIndex => 0;

        /// <summary>The slowest rung's index.</summary>
        public static int SlowestIndex => 0;

        /// <summary>The fastest rung's index.</summary>
        public static int FastestIndex => s_rungs.Length - 1;

        /// <summary>
        /// The multiplier at <paramref name="index"/>, for
        /// <c>LiveMatchStreamer.SetSpeedMultiplier</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The index is not a rung. Fail-loud rather
        /// than clamping: an out-of-range index is a binding bug, and clamping it would run the match
        /// at a speed nobody selected.</exception>
        public static float MultiplierAt(int index)
        {
            RequireRung(index);
            return s_rungs[index];
        }

        /// <summary>
        /// The next rung up, or <paramref name="index"/> itself when already at the top.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The index is not a rung.</exception>
        public static int StepFaster(int index)
        {
            RequireRung(index);
            return index == FastestIndex ? index : index + 1;
        }

        /// <summary>
        /// The next rung down, or <paramref name="index"/> itself when already at the bottom.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The index is not a rung.</exception>
        public static int StepSlower(int index)
        {
            RequireRung(index);
            return index == SlowestIndex ? index : index - 1;
        }

        /// <summary>
        /// True when <paramref name="index"/> names a rung. For a binding validating a restored or
        /// user-supplied selection before using it.
        /// </summary>
        public static bool IsRung(int index) => index >= 0 && index < s_rungs.Length;

        /// <summary>
        /// Finds the rung carrying <paramref name="multiplier"/>.
        ///
        /// <para>Matching is exact, and that is deliberate: the only multipliers that should ever be
        /// looked up here are ones this ladder handed out, so an approximate match would hide a
        /// caller that invented a speed of its own rather than stepping the ladder.</para>
        /// </summary>
        public static bool TryIndexOf(float multiplier, out int index)
        {
            for (int i = 0; i < s_rungs.Length; i++)
            {
                if (s_rungs[i] == multiplier)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        /// <summary>
        /// The rungs, slowest first. Returns a copy — a caller cannot reorder or retune the ladder
        /// through the array it is handed.
        /// </summary>
        public static float[] SnapshotRungs() => (float[])s_rungs.Clone();

        private static void RequireRung(int index)
        {
            if (!IsRung(index))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    index,
                    "Playback speed rung index must be in [0, " +
                    (s_rungs.Length - 1).ToString(CultureInfo.InvariantCulture) + "].");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-07 | —      | Initial creation (§5-P5 host-free half): the four catalogue     |
// |         |            |        | multipliers as an ordered ladder, the opening rung named, and   |
// |         |            |        | faster/slower clamping at the ends rather than wrapping. The    |
// |         |            |        | catalogue holds the dials; the order and the stepping are       |
// |         |            |        | decisions, so §12 rule 1 keeps them out of the binding.         |
#endregion
