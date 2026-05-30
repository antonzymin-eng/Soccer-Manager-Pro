// File:     src/performance-optimization/TraceChannel.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.8.2, Appendix F.0, Code Standards #20
// Purpose:  Channel registry schema types and the Stage 0 anchor channel definitions.
//           The three perf.* anchor channels declared here match Appendix F.0 exactly;
//           per-subsystem channel rows (ai.*, physics.*, etc.) are Stage 1 deliverables.

namespace TacticalDirector.PerformanceOptimization
{
    // ── F.0 Schema enums ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Verbosity tier for a trace channel. Controls emission volume under FR-PO-055.
    /// Performance Optimization Strategy #18 §3.8.2 / Appendix F.0.
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

    /// <summary>
    /// Determinism classification of a trace channel. Tier A / B channels must be
    /// determinism-clean per FR-PO-058a. Performance Optimization Strategy #18 §3.8.2 / Appendix F.0.
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

    // ── F.0 Channel descriptor ────────────────────────────────────────────────────────

    /// <summary>
    /// Schema-conforming descriptor for a single trace channel.
    /// One instance per channel in the registry; populated by channel owners at Stage 1.
    /// Performance Optimization Strategy #18 Appendix F.0.
    /// </summary>
    public sealed class TraceChannelDescriptor
    {
        /// <summary>Globally unique dotted lower-snake name (e.g., <c>perf.budget</c>). Required. F.0.</summary>
        public string ChannelName { get; }

        /// <summary>Spec ID owning this channel (e.g., <c>#18</c>, <c>#8</c>). Required. F.0.</summary>
        public string OwningSubsystem { get; }

        /// <summary>Default verbosity tier. Required. F.0 / FR-PO-055.</summary>
        public ChannelVerbosity DefaultVerbosity { get; }

        /// <summary>Sampling rule for emission. Required. F.0 / FR-PO-056.</summary>
        public ChannelSamplingRule SamplingRule { get; }

        /// <summary>
        /// N for <see cref="ChannelSamplingRule.PerNTicks"/>; must be &gt; 0 when
        /// SamplingRule = PerNTicks. Zero otherwise. F.0.
        /// </summary>
        public uint SampleN { get; }

        /// <summary>Determinism classification. Required. F.0.</summary>
        public ChannelDeterminismClass DeterminismClass { get; }

        /// <summary>
        /// True if at least one emission point sits inside the canonical tick pipeline
        /// (#16 §3.1); requires sign-off from the #16 owner per FR-PO-058a. F.0.
        /// </summary>
        public bool InsideTickPipeline { get; }

        /// <summary>
        /// Row ID in spec-error-log.md recording #16-owner sign-off.
        /// Required when <see cref="InsideTickPipeline"/> is true. F.0.
        /// </summary>
        public string SignOffLogRef { get; }

        /// <summary>
        /// Canonical record-format version active when this row was created.
        /// Pinned to the #16 §3.2.4.1 version at creation date. F.0.
        /// </summary>
        public string RecordFormatVersion { get; }

        /// <summary>
        /// Spec-author or subsystem-owner identifier. Required. F.0.
        /// </summary>
        public string OwnerContact { get; }

        /// <summary>
        /// RFC 3339 date of registry-row creation. Required. F.0.
        /// </summary>
        public string CreatedDate { get; }

        public TraceChannelDescriptor(
            string channelName,
            string owningSubsystem,
            ChannelVerbosity defaultVerbosity,
            ChannelSamplingRule samplingRule,
            uint sampleN,
            ChannelDeterminismClass determinismClass,
            bool insideTickPipeline,
            string signOffLogRef,
            string recordFormatVersion,
            string ownerContact,
            string createdDate)
        {
            ChannelName         = channelName;
            OwningSubsystem     = owningSubsystem;
            DefaultVerbosity    = defaultVerbosity;
            SamplingRule        = samplingRule;
            SampleN             = sampleN;
            DeterminismClass    = determinismClass;
            InsideTickPipeline  = insideTickPipeline;
            SignOffLogRef       = signOffLogRef;
            RecordFormatVersion = recordFormatVersion;
            OwnerContact        = ownerContact;
            CreatedDate         = createdDate;
        }
    }

    // ── Stage 0 anchor channel registry ──────────────────────────────────────────────

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
        // ── perf.budget (F.1 upstream) ────────────────────────────────────────────────

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

        // ── perf.alloc (F.2 / F.4 upstream) ──────────────────────────────────────────

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

        // ── perf.trace (deep-dive; inside tick pipeline) ──────────────────────────────

        /// <summary>
        /// Full per-draw-call trace. Inside tick pipeline — requires #16-owner sign-off
        /// before activation (FR-PO-058a / ERR-018-NNN filed at first emission point).
        /// Tier C, every-tick, exhaustive verbosity. F.0 anchor row.
        /// </summary>
        public static readonly TraceChannelDescriptor PerfTrace = new TraceChannelDescriptor(
            channelName:         "perf.trace",
            owningSubsystem:     "#18",
            defaultVerbosity:    ChannelVerbosity.Exhaustive,
            samplingRule:        ChannelSamplingRule.EveryTick,
            sampleN:             0,
            determinismClass:    ChannelDeterminismClass.TierC,
            insideTickPipeline:  true,
            signOffLogRef:       "ERR-018-NNN (filed when first tick-pipeline emission point is added)",
            recordFormatVersion: "per #16 §3.2.4.1 active version",
            ownerContact:        "Spec #18 author",
            createdDate:         "2026-05-14");
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                    |
// | 1.0     | 2026-05-30 | —      | Initial implementation. F.0 schema enums + descriptor class + Stage 0  |
// |         |            |        | perf.budget / perf.alloc / perf.trace anchor rows. §3.8.2 / Appendix  |
// |         |            |        | F.0. Per-subsystem rows are Stage 1 deliverables.                      |
#endregion
