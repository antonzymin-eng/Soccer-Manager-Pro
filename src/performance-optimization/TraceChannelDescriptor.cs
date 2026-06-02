// File:     src/performance-optimization/TraceChannelDescriptor.cs
// Created:  2026-05-30
// Modified: 2026-06-02
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.8.2, Appendix F.0, Code Standards #20
// Purpose:  Schema-conforming descriptor for a single trace channel.

using System;

namespace TacticalDirector.PerformanceOptimization
{
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
            if (samplingRule == ChannelSamplingRule.PerNTicks && sampleN == 0u)
            {
                throw new ArgumentException(
                    "SampleN must be > 0 when SamplingRule = PerNTicks (Appendix F.0).",
                    nameof(sampleN));
            }

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
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-30 | —      | Initial implementation in TraceChannel.cs.                          |
// | 1.1     | 2026-06-02 | —      | AR-1 H-1: extracted from TraceChannel.cs (one public type per file).|
// |         |            |        | AR-1 L-1: constructor enforces SamplingRule/SampleN invariant.      |
#endregion
