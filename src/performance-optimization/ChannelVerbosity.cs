// File:     src/performance-optimization/ChannelVerbosity.cs
// Created:  2026-05-30
// Modified: 2026-06-07
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.8.2, Appendix F.0, Code Standards #20
// Purpose:  Verbosity tier enum for a trace channel; one of three F.0 schema enums.

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Verbosity tier for a trace channel. Controls emission volume under FR-PO-055.
    /// Performance Optimization Strategy #18 §3.8.2 / Appendix F.0.
    ///
    /// ORDINAL STABILITY: this enum is part of the Appendix F.0 channel-registry schema
    /// and is embedded in <see cref="TraceChannelDescriptor"/> rows that will be persisted
    /// (Stage 0+1) for cross-spec dashboard consumers. APPEND new members at the end ONLY.
    /// </summary>
    public enum ChannelVerbosity
    {
        /// <summary>Lowest emission rate; summary-level data only.</summary>
        Minimal,
        /// <summary>Default production level; per-spec §6 budget totals.</summary>
        Standard,
        /// <summary>Per-method breakdown; enabled during profiling sessions.</summary>
        Debug,
        /// <summary>Full per-draw-call trace; enabled for deep-dive investigations.</summary>
        Exhaustive
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-30 | —      | Initial implementation in TraceChannel.cs.                          |
// | 1.1     | 2026-06-02 | —      | AR-1 H-1: extracted from TraceChannel.cs (one public type per file).|
// | 1.2     | 2026-06-07 | —      | AR-4 L-1: ordinal-stability paragraph added — F.0 schema enum.     |
#endregion
