// File:     src/client-app/ClientMatchReport.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Spec:     P5b lifecycle/identity plan §§4–7, S0 binding contracts §2, Code Standards #20
// Purpose:  Frozen ended frame, analytics publication and stable command evidence.

using System.Collections.Generic;
using System.Collections.ObjectModel;

using TacticalDirector.MatchClientCore;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp
{
    /// <summary>Immutable report context created once after the real Stop/ServiceOnce end barrier.</summary>
    public readonly struct ClientMatchReport
    {
        /// <summary>True only after the full-time barrier completed.</summary>
        public readonly bool IsAvailable;
        /// <summary>Authoritative score/time/end snapshot.</summary>
        public readonly MatchFrameView FinalFrame;
        /// <summary>Independent statistics and incomplete/cutoff state.</summary>
        public readonly MatchAnalyticsPublication Analytics;
        /// <summary>Immutable applied command evidence.</summary>
        public readonly IReadOnlyList<TickStampedCommand> Applied;
        /// <summary>Immutable refused command evidence.</summary>
        public readonly IReadOnlyList<TickStampedCommand> Refused;
        internal ClientMatchReport(in MatchFrameView frame, in MatchAnalyticsPublication analytics,
            IReadOnlyList<TickStampedCommand> applied, IReadOnlyList<TickStampedCommand> refused)
        {
            IsAvailable = true;
            FinalFrame = frame;
            Analytics = analytics;
            Applied = Copy(applied);
            Refused = Copy(refused);
        }
        private static ReadOnlyCollection<TickStampedCommand> Copy(IReadOnlyList<TickStampedCommand> source)
        {
            var values = new TickStampedCommand[source.Count];
            for (int i = 0; i < values.Length; i++) values[i] = source[i];
            return new ReadOnlyCollection<TickStampedCommand>(values);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-06 | —      | Consumed P5b lifecycle/identity implementation. |
#endregion
