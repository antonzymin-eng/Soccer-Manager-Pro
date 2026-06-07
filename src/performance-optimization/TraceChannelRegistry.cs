// File:     src/performance-optimization/TraceChannelRegistry.cs
// Created:  2026-05-30
// Modified: 2026-06-07
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.8.2, Appendix F.0, Code Standards #20
// Purpose:  Stage 0 anchor rows for the Appendix F.0 channel registry.

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Stage 0 anchor rows for the Appendix F.0 channel registry.
    /// The three <c>perf.*</c> channels are owned by Spec #18 itself and serve as
    /// upstream data sources for the F.1 … F.5 dashboards.
    /// Per-subsystem rows (e.g., <c>ai.*</c>, <c>physics.*</c>) are Stage 1 deliverables
    /// populated by each spec's <c>src/&lt;spec&gt;/</c> subsystem when it instruments itself.
    /// Performance Optimization Strategy #18 §3.8.2 / Appendix F.0.
    /// </summary>
    public static class TraceChannelRegistry
    {
        /// <summary>
        /// Per-spec per-tick budget consumption roll-up. Feeds F.1 dashboard.
        /// Tier C, every-tick, not inside tick pipeline. F.0 anchor row.
        /// </summary>
        public static readonly TraceChannelDescriptor PerfBudget = new TraceChannelDescriptor(
            channelName:         "perf.budget",
            owningSubsystem:     "#18",
            defaultVerbosity:    ChannelVerbosity.Standard,
            samplingRule:        ChannelSamplingRule.EveryTick,
            sampleN:             0,
            determinismClass:    ChannelDeterminismClass.TierC,
            insideTickPipeline:  false,
            signOffLogRef:       null,
            recordFormatVersion: "per #16 §3.2.4.1 active version",
            ownerContact:        "Spec #18 author",
            createdDate:         "2026-05-14");

        /// <summary>
        /// Per-method managed-allocation totals. Feeds F.2 and F.4 dashboards.
        /// Tier C, per-1-tick sampling, not inside tick pipeline. F.0 anchor row.
        /// </summary>
        public static readonly TraceChannelDescriptor PerfAlloc = new TraceChannelDescriptor(
            channelName:         "perf.alloc",
            owningSubsystem:     "#18",
            defaultVerbosity:    ChannelVerbosity.Debug,
            samplingRule:        ChannelSamplingRule.PerNTicks,
            sampleN:             1,
            determinismClass:    ChannelDeterminismClass.TierC,
            insideTickPipeline:  false,
            signOffLogRef:       null,
            recordFormatVersion: "per #16 §3.2.4.1 active version",
            ownerContact:        "Spec #18 author",
            createdDate:         "2026-05-14");

        /// <summary>
        /// Full per-draw-call trace. Will move inside the tick pipeline at Stage 1 when the
        /// first emission point is added; doing so requires #16-owner sign-off per FR-PO-058a.
        /// Until that sign-off lands and a real ERR-018-NNN row is filed, the anchor row is
        /// declared with <c>InsideTickPipeline = false</c> so the AR-2 L-2 invariant does not
        /// silently accept the "ERR-018-NNN" placeholder as a valid sign-off reference.
        /// Tier C, every-tick, exhaustive verbosity. F.0 anchor row.
        /// </summary>
        public static readonly TraceChannelDescriptor PerfTrace = new TraceChannelDescriptor(
            channelName:         "perf.trace",
            owningSubsystem:     "#18",
            defaultVerbosity:    ChannelVerbosity.Exhaustive,
            samplingRule:        ChannelSamplingRule.EveryTick,
            sampleN:             0,
            determinismClass:    ChannelDeterminismClass.TierC,
            insideTickPipeline:  false,
            signOffLogRef:       null,
            recordFormatVersion: "per #16 §3.2.4.1 active version",
            ownerContact:        "Spec #18 author",
            createdDate:         "2026-05-14");
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-30 | —      | Initial implementation in TraceChannel.cs.                          |
// | 1.1     | 2026-06-02 | —      | AR-1 H-1: extracted from TraceChannel.cs (one public type per file).|
// | 1.2     | 2026-06-07 | —      | AR-3 M-3: PerfTrace InsideTickPipeline flipped true→false and       |
// |         |            |        | SignOffLogRef set to null until the real #16-owner sign-off and    |
// |         |            |        | a real ERR-018-NNN row exist. The prior literal placeholder         |
// |         |            |        | satisfied the AR-2 L-2 invariant cosmetically while bypassing      |
// |         |            |        | FR-PO-058a governance.                                              |
#endregion
