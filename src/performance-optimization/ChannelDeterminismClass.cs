// File:     src/performance-optimization/ChannelDeterminismClass.cs
// Created:  2026-05-30
// Modified: 2026-06-07
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.8.2, Appendix F.0, Code Standards #20
// Purpose:  Determinism-classification enum for a trace channel; one of three F.0 schema enums.

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Determinism classification of a trace channel. Tier A / B channels must be
    /// determinism-clean per FR-PO-058a. Performance Optimization Strategy #18 §3.8.2 / Appendix F.0.
    ///
    /// ORDINAL STABILITY: ordinals here mirror <see cref="TacticalDirector.DeterministicSim.DeterminismTier"/>
    /// (TierA=0 / TierB=1 / TierC=2 per #16 §3.2). If either side gains a new tier, both
    /// MUST be extended atomically at the matching ordinal to preserve the cross-spec
    /// mirror; APPEND-only otherwise.
    /// </summary>
    public enum ChannelDeterminismClass
    {
        /// <summary>Tier A — authoritative, included in digest. Channels must not perturb digest.</summary>
        TierA,
        /// <summary>Tier B — bounded-authoritative; Stage 5+ tolerance path.</summary>
        TierB,
        /// <summary>Tier C — cosmetic / observability only; excluded from digest.</summary>
        TierC
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-30 | —      | Initial implementation in TraceChannel.cs.                          |
// | 1.1     | 2026-06-02 | —      | AR-1 H-1: extracted from TraceChannel.cs (one public type per file).|
// | 1.2     | 2026-06-07 | —      | AR-4 L-1: ordinal-stability paragraph added — mirror of #16        |
// |         |            |        | DeterminismTier ordinals; cross-spec extension must be atomic.     |
#endregion
