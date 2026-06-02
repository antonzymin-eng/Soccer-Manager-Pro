// File:     src/testing-strategy/DeterminismSuiteResult.cs
// Created:  2026-06-02
// Modified: 2026-06-02
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.2 / §4.3.1, Code Standards #20
// Purpose:  Aggregated result for one invocation of DeterminismGate. Carried
//           verbatim by ITestHarness.RunDeterminismTiers per FR-TS-016 (single
//           integration point through which #16 §5 is invoked).

using System.Collections.Generic;

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// Aggregated result for one invocation of <see cref="DeterminismGate.RunTiers"/>.
    /// Carries per-tier outcomes plus the golden-vector run (a precondition of #16 §5.5
    /// per FR-DS-009-GATE). The CI gate consumes <see cref="AllPassed"/> as the
    /// merge-block signal per FR-TS-012.
    /// Testing Strategy &amp; Framework #19 §3.2 / §4.3.1 / KD-2.
    /// </summary>
    public sealed class DeterminismSuiteResult
    {
        /// <summary>
        /// Per-tier outcomes in #16 §5 canonical order (Unit → Integration → Scenario → Soak).
        /// FR-TS-011.
        /// </summary>
        public IReadOnlyList<DeterminismTierResult> TierResults { get; }

        /// <summary>
        /// Golden-vector run produced by <see cref="GoldenVectorRunner.RunAll"/> as the
        /// FR-DS-009-GATE precondition (#16 §5.5). One entry per #16 §9.5 #4 (a/b/c).
        /// </summary>
        public IReadOnlyList<GoldenVectorResult> GoldenVectorResults { get; }

        /// <summary>True only if every tier passed and every golden-vector corpus passed.</summary>
        public bool AllPassed { get; }

        /// <summary>
        /// Initialises an aggregated suite result. <see cref="AllPassed"/> is computed
        /// from the union of tier and golden-vector outcomes.
        /// </summary>
        public DeterminismSuiteResult(
            IReadOnlyList<DeterminismTierResult> tierResults,
            IReadOnlyList<GoldenVectorResult> goldenVectorResults)
        {
            TierResults         = tierResults;
            GoldenVectorResults = goldenVectorResults;

            bool allPassed = true;
            for (int i = 0; i < tierResults.Count; i++)
            {
                if (!tierResults[i].Passed)
                {
                    allPassed = false;
                    break;
                }
            }
            if (allPassed)
            {
                for (int i = 0; i < goldenVectorResults.Count; i++)
                {
                    if (!goldenVectorResults[i].Passed)
                    {
                        allPassed = false;
                        break;
                    }
                }
            }

            AllPassed = allPassed;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-02 | —      | Initial implementation. |
#endregion
