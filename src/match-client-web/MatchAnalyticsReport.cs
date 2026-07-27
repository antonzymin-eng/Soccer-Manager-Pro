// File:     src/match-client-web/MatchAnalyticsReport.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Path-to-playable roadmap §7 (B6), Match Analytics #37 §4.4, Code Standards #20
// Purpose:  Pairs a statistics result with the tick count it was derived from, so a caller cannot
//           read the two out of step across the sim thread.

using TacticalDirector.MatchAnalytics;

namespace TacticalDirector.MatchClientWeb
{
    /// <summary>
    /// A statistics snapshot and the tick it describes, taken together.
    ///
    /// <para>Exists because the two are read from a shared accumulator that a sim thread is writing:
    /// fetched separately, the tick can advance between them and a screen shows shares computed at
    /// tick N labelled with tick N+1. Small, but the kind of incoherence that surfaces later as a
    /// confusing report about percentages that do not add up.</para>
    /// </summary>
    public readonly struct MatchAnalyticsReport
    {
        /// <summary>The statistics.</summary>
        public readonly MatchAnalyticsResult Result;

        /// <summary>Ticks observed when <see cref="Result"/> was built — its denominator.</summary>
        public readonly long ObservedTicks;

        /// <summary>Pairs a result with its tick count.</summary>
        public MatchAnalyticsReport(in MatchAnalyticsResult result, long observedTicks)
        {
            Result        = result;
            ObservedTicks = observedTicks;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (B6 self-review): the coherent report pair.   |
#endregion
