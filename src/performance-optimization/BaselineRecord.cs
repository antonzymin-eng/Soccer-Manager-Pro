// File:     src/performance-optimization/BaselineRecord.cs
// Created:  2026-06-01
// Modified: 2026-06-07
// Author:   —
// Spec:     Performance Optimization Strategy #18 §4.3.2, Appendix A, Code Standards #20
// Purpose:  Immutable baseline record: session manifest + captured metrics per Appendix A.
//           On-disk encoding conforms to Deterministic Simulation #16 §3.2.4.1 (KD-11).
//           Produced by IPerfHarness.FinalizeSession(); stored under
//           docs/specs/performance-optimization/baselines/ at Stage 0.

using System;
using System.Collections.Generic;

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Immutable baseline record: §3.3.2 session manifest plus captured metrics.
    /// Produced by <see cref="IPerfHarness.FinalizeSession"/>; serialised per
    /// Deterministic Simulation #16 §3.2.4.1 (KD-11).
    /// Stage 0 JSON projection lives under
    /// <c>docs/specs/performance-optimization/baselines/&lt;spec&gt;/</c>.
    /// Performance Optimization Strategy #18 §4.3.2 / Appendix A.
    /// </summary>
    public sealed class BaselineRecord
    {
        /// <summary>§3.3.2 session manifest. All fields required.</summary>
        public SessionManifest Manifest { get; }

        /// <summary>Which simulation loop this record was captured against (KD-8).</summary>
        public LoopTag Loop { get; }

        /// <summary>Median per-tick / per-frame budget consumption in milliseconds.</summary>
        public float P50Ms { get; }

        /// <summary>99th-percentile per-tick / per-frame budget consumption in milliseconds.</summary>
        public float P99Ms { get; }

        /// <summary>
        /// Per-method managed allocation totals in bytes per tick.
        /// Every hot-path entry MUST report 0; non-zero values trigger alloc-gate failure.
        /// The MUST is enforced by <c>tools/budget-auditor.py</c> at the §5.3 schema-
        /// conformance step — this struct intentionally carries the raw dictionary so the
        /// auditor can attribute the diff to the offending entry name. Tooling-side
        /// managed dictionary; not on the game hot path.
        /// </summary>
        public IReadOnlyDictionary<string, ulong> PerMethodAllocBytes { get; }

        /// <summary>
        /// Pass/fail verdict against the §3.5.2 threshold at time of capture.
        /// Advisory at capture; authoritative at CI gate-evaluation time.
        /// </summary>
        public BaselinePassFail PassFail { get; }

        /// <summary>
        /// The FR-PO-### or per-spec §6 threshold cited in this comparison
        /// (e.g., <c>FR-PO-031</c>, <c>shot-mechanics/section-4.md §4.5</c>).
        /// </summary>
        public string ThresholdCited { get; }

        /// <summary>
        /// Initialises a baseline record from a completed profiling session.
        /// Enforces reference-non-null invariants on <paramref name="manifest"/>,
        /// <paramref name="perMethodAllocBytes"/>, and <paramref name="thresholdCited"/>,
        /// plus finite-and-non-negative invariants on <paramref name="p50Ms"/> and
        /// <paramref name="p99Ms"/>. Degenerate records cannot enter the corpus.
        /// </summary>
        public BaselineRecord(
            SessionManifest manifest,
            LoopTag loop,
            float p50Ms,
            float p99Ms,
            IReadOnlyDictionary<string, ulong> perMethodAllocBytes,
            BaselinePassFail passFailAtCapture,
            string thresholdCited)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }

            if (perMethodAllocBytes == null)
            {
                throw new ArgumentNullException(nameof(perMethodAllocBytes));
            }

            if (thresholdCited == null)
            {
                throw new ArgumentNullException(nameof(thresholdCited));
            }

            if (thresholdCited.Length == 0)
            {
                throw new ArgumentException(
                    "thresholdCited must name an FR-PO-### identifier or per-spec §6 anchor.",
                    nameof(thresholdCited));
            }

            if (!float.IsFinite(p50Ms) || p50Ms < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(p50Ms), p50Ms,
                    "p50Ms must be finite and non-negative.");
            }

            if (!float.IsFinite(p99Ms) || p99Ms < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(p99Ms), p99Ms,
                    "p99Ms must be finite and non-negative.");
            }

            Manifest             = manifest;
            Loop                 = loop;
            P50Ms                = p50Ms;
            P99Ms                = p99Ms;
            PerMethodAllocBytes  = perMethodAllocBytes;
            PassFail             = passFailAtCapture;
            ThresholdCited       = thresholdCited;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-01 | —      | Initial implementation.                                            |
// | 1.1     | 2026-06-07 | —      | AR-3 M-2: constructor enforces non-null manifest /                  |
// |         |            |        | perMethodAllocBytes / thresholdCited (non-empty) and finite,        |
// |         |            |        | non-negative p50Ms / p99Ms. Closes upstream NPE hazards in          |
// |         |            |        | RegressionGate.Evaluate / BaselineReproducibilityAuditor.Validate.  |
#endregion
