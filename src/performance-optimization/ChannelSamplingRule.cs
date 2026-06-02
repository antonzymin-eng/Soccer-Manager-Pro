// File:     src/performance-optimization/ChannelSamplingRule.cs
// Created:  2026-05-30
// Modified: 2026-06-02
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.8.2, Appendix F.0, Code Standards #20
// Purpose:  Sampling-rule enum for a trace channel; one of three F.0 schema enums.

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Sampling rule for a trace channel. Controls when records are emitted per FR-PO-056.
    /// Performance Optimization Strategy #18 §3.8.2 / Appendix F.0.
    /// </summary>
    public enum ChannelSamplingRule
    {
        /// <summary>Emit a record on every simulation tick.</summary>
        EveryTick,
        /// <summary>Emit every N ticks; requires <c>SampleN &gt; 0</c> in the channel descriptor.</summary>
        PerNTicks,
        /// <summary>Emit only when triggered by a discrete game event.</summary>
        EventDriven
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-30 | —      | Initial implementation in TraceChannel.cs.                          |
// | 1.1     | 2026-06-02 | —      | AR-1 H-1: extracted from TraceChannel.cs (one public type per file).|
#endregion
