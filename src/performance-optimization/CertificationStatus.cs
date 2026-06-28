// File:     src/performance-optimization/CertificationStatus.cs
// Created:  2026-06-28
// Modified: 2026-06-28
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.4.4 / FR-PO-031 / FR-PO-052,
//           certification-platform.md (Stage 0 host pin), Code Standards #20
// Purpose:  Certification state of a perf baseline corpus entry: Pending until a run on the
//           pinned certification platform (certification-platform.md) supplies the measured
//           metrics; Certified once it has. A Pending entry MUST NOT carry a measured number —
//           the Linux compile/test gate is explicitly NON-certifying, so a number sourced there
//           would be a fabricated certification.

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Certification state of a <see cref="CertifiedPerfBaseline"/> corpus entry.
    /// A baseline is authoritative for the FR-PO-031 regression gate only when it was captured
    /// on the pinned certification platform (certification-platform.md). Until then the entry is
    /// <see cref="Pending"/> and carries no measured metric (no fabricated certification).
    /// Performance Optimization Strategy #18 §3.4.4.
    ///
    /// ORDINAL STABILITY: append new members at the end ONLY — a Stage 0+1 corpus reader may
    /// hash or index this state, and inserting in the middle shifts ordinals on existing entries.
    /// </summary>
    public enum CertificationStatus
    {
        /// <summary>
        /// No certification run on the pinned platform has produced metrics yet. The entry records
        /// only the intent (scenario, loop, platform pin, cited threshold); its metrics are NaN and
        /// it cannot build a corpus <see cref="BaselineRecord"/>.
        /// </summary>
        Pending,

        /// <summary>
        /// Captured on the pinned certification platform with finite, positive metrics and a
        /// complete <see cref="SessionManifest"/>. Authoritative for FR-PO-031 comparison.
        /// </summary>
        Certified
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-28 | —      | Initial implementation. Pending / Certified states for the perf    |
// |         |            |        | baseline corpus (FR-PO-052 certified baseline machinery).          |
#endregion
