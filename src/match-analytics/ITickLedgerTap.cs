// File:     src/match-analytics/ITickLedgerTap.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Match Analytics & Statistics #37 §2.2 / §4.3 (KD-7), FR-AN-002, Code Standards #20
// Purpose:  The read-only per-tick ledger tap the aggregator consumes — one of exactly two inputs
//           #37 is permitted (FR-AN-002). An interface, not the engine type, so the §3.2 routing
//           table is unit-testable without booting a match.

namespace TacticalDirector.MatchAnalytics
{
    /// <summary>
    /// One tick's Tier A/B event records, in the FM-017-002 canonical order the snapshot digest used.
    ///
    /// <para><b>Why an interface here and not a concrete class.</b> The project's default is a concrete
    /// type (the living-world KD-1 precedent), and it is the right default when only one producer can
    /// ever exist. Here both sides are specified — #37 §3.2 pins the consumer and #37 §4.3 pins the
    /// producer — and the §3.2 routing table is a pure function of the record sequence, so the seam
    /// buys the ability to drive every branch of it from an authored record list rather than by
    /// contriving a real match that fouls, cards, and substitutes on cue. Interfaces against
    /// *unspecified* systems are what ERR-001 / ERR-004 forbid; this is not that.</para>
    ///
    /// <para>Implementations return value copies and expose no engine state (FR-AN-001).</para>
    /// </summary>
    public interface ITickLedgerTap
    {
        /// <summary>Number of records the engine drained on the tick being observed.</summary>
        int RecordCount { get; }

        /// <summary>
        /// Appendix A event-type ordinal of record <paramref name="index"/>. Compare against
        /// <c>EventRegistry.GetOrdinal&lt;T&gt;()</c> to decide which record type to read.
        /// </summary>
        byte RecordOrdinal(int index);

        /// <summary>Reads record <paramref name="index"/> back as a value copy of the given type.</summary>
        T ReadRecord<T>(int index) where T : struct;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (#37 T1): the KD-7 tap seam.                  |
#endregion
