// File:     src/testing-strategy/PerfGateReport.cs
// Created:  2026-06-02
// Modified: 2026-06-02
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.9.2 (pointer to Spec #18),
//           Performance Optimization Strategy #18 §3.5.2 / §3.5.6 / FR-PO-031,
//           Code Standards #20
// Purpose:  Result returned by PerfGateRunner.Run(). Wraps Spec #18's RegressionResult
//           with the scenario / loop / spec-id context the CI gate needs to format
//           a merge-block message (FR-PO-036).

using System;

using TacticalDirector.PerformanceOptimization;

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// CI-friendly wrapper around a Spec #18 <see cref="RegressionResult"/>. Carries
    /// the spec ID, loop tag, and scenario name needed by the CI gate to format a
    /// per-spec merge-block diagnostic (FR-PO-036).
    /// Testing Strategy &amp; Framework #19 §3.9.2 / Performance Optimization #18 §3.5.2.
    /// </summary>
    public sealed class PerfGateReport
    {
        /// <summary>Spec ID whose §6 budget row produced the baseline (e.g. 1 for Ball Physics).</summary>
        public int SpecId { get; }

        /// <summary>
        /// Loop tag string from <see cref="PerformanceOptimizationConstants"/>
        /// (<c>LOOP_TAG_TACTICAL_10HZ</c> or <c>LOOP_TAG_PHYSICS_60HZ</c>) per #18 §3.2.2.
        /// </summary>
        public string LoopTag { get; }

        /// <summary>Scenario manifest ID measured (mirrors <c>SessionManifest.ScenarioManifestId</c>).</summary>
        public string ScenarioManifestId { get; }

        /// <summary>Underlying regression-gate evaluation from #18.</summary>
        public RegressionResult Regression { get; }

        /// <summary>True only if both per-PR and absolute-drift gates passed.</summary>
        public bool AllPassed => Regression.AllPassed;

        /// <summary>
        /// Initialises a report. <paramref name="loopTag"/> SHOULD be one of the
        /// #18 <c>LOOP_TAG_*</c> string constants; the runner does not enforce that
        /// invariant so dashboards may render unknown loop tags as <c>UNKNOWN</c>.
        /// Both <paramref name="loopTag"/> and <paramref name="scenarioManifestId"/>
        /// MUST be non-null per AR-3 M-1 (parallel to the AR-1/AR-2 invariant push on
        /// the other result/entry types). The empty string is permitted for both —
        /// <see cref="PerfGateRunner.Run"/> deliberately uses <see cref="string.Empty"/>
        /// as the missing-manifest sentinel.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="loopTag"/> or <paramref name="scenarioManifestId"/> is null.
        /// </exception>
        public PerfGateReport(
            int specId,
            string loopTag,
            string scenarioManifestId,
            RegressionResult regression)
        {
            if (loopTag == null)
            {
                throw new ArgumentNullException(nameof(loopTag));
            }
            if (scenarioManifestId == null)
            {
                throw new ArgumentNullException(nameof(scenarioManifestId));
            }

            SpecId             = specId;
            LoopTag            = loopTag;
            ScenarioManifestId = scenarioManifestId;
            Regression         = regression;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-02 | —      | Initial implementation.                                            |
// | 1.1     | 2026-06-02 | —      | AR-3 M-1: constructor null-checks loopTag + scenarioManifestId    |
// |         |            |        | with explicit ArgumentNullException (parallel to AR-1/AR-2        |
// |         |            |        | invariant push on the other result/entry types). Empty strings    |
// |         |            |        | remain valid — PerfGateRunner.Run uses string.Empty as the         |
// |         |            |        | missing-manifest sentinel.                                         |
#endregion
