// File:     src/performance-optimization/BaselinePassFail.cs
// Created:  2026-06-01
// Modified: 2026-06-07
// Author:   —
// Spec:     Performance Optimization Strategy #18 Appendix A, §3.5.2, Code Standards #20
// Purpose:  Pass/fail/advisory verdict stored per captured baseline record.
//           Advisory at capture time; authoritative at gate-evaluation time (FR-PO-036).

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Pass/fail verdict for a captured baseline record against its declared threshold.
    /// Advisory at capture time; authoritative at CI gate-evaluation time.
    /// Performance Optimization Strategy #18 Appendix A / §3.5.2.
    ///
    /// ORDINAL STABILITY: members of this enum are embedded in <see cref="BaselineRecord"/>
    /// and serialised to disk per Appendix A. APPEND new members at the end ONLY — inserting
    /// in the middle shifts ordinals on existing on-disk baselines and breaks both Stage 0+1
    /// baseline-corpus consumers and any digest-aware tooling that hashes the record.
    /// </summary>
    public enum BaselinePassFail
    {
        /// <summary>Captured metric is within the declared threshold.</summary>
        Pass,

        /// <summary>Captured metric exceeds the declared threshold.</summary>
        Fail,

        /// <summary>
        /// Advisory — threshold comparison was inconclusive at capture time
        /// (e.g., baseline corpus not yet statistically stable per §3.4.4).
        /// Authoritative verdict is assigned at CI gate-evaluation time.
        /// </summary>
        Advisory
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-01 | —      | Initial implementation.                                            |
// | 1.1     | 2026-06-07 | —      | AR-4 L-1: ordinal-stability paragraph added — enum is embedded in  |
// |         |            |        | BaselineRecord serialised per Appendix A.                          |
#endregion
