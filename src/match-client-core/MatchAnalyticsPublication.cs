// File:     src/match-client-core/MatchAnalyticsPublication.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Spec:     P5b lifecycle/identity plan §§4–7, S0 binding contracts §2, Code Standards #20
// Purpose:  Immutable analytics health and cutoff snapshot; score/end remain frame-owned.

using TacticalDirector.MatchAnalytics;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>Publication over completed observations; a failing partial tick is never published as healthy.</summary>
    public readonly struct MatchAnalyticsPublication
    {
        /// <summary>Most recent trustworthy result, or null if no result was published before a fault.</summary>
        public readonly MatchAnalyticsResult? Result;
        /// <summary>True after the first observation failure.</summary>
        public readonly bool IsIncomplete;
        /// <summary>Most recent attempted engine tick.</summary>
        public readonly ulong AttemptedTick;
        /// <summary>Most recent wholly completed observation.</summary>
        public readonly ulong CompletedTick;
        /// <summary>Number of wholly completed observations.</summary>
        public readonly long ObservedTicks;
        /// <summary>Cutoff of Result, which may predate CompletedTick after a fault.</summary>
        public readonly ulong ResultThroughTick;
        /// <summary>Captures result, health and cutoff atomically through the owning adapter.</summary>
        public MatchAnalyticsPublication(MatchAnalyticsResult? result, bool isIncomplete,
            ulong attemptedTick, ulong completedTick, long observedTicks, ulong resultThroughTick)
        {
            Result = result;
            IsIncomplete = isIncomplete;
            AttemptedTick = attemptedTick;
            CompletedTick = completedTick;
            ObservedTicks = observedTicks;
            ResultThroughTick = resultThroughTick;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-06 | —      | Consumed P5b lifecycle/identity implementation. |
#endregion
