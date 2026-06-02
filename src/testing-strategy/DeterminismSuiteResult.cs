// File:     src/testing-strategy/DeterminismSuiteResult.cs
// Created:  2026-06-02
// Modified: 2026-06-02
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.2 / §4.3.1, Code Standards #20
// Purpose:  Aggregated result for one invocation of DeterminismGate. Carried
//           verbatim by ITestHarness.RunDeterminismTiers per FR-TS-016 (single
//           integration point through which #16 §5 is invoked).

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// Aggregated result for one invocation of <see cref="DeterminismGate.RunTiers"/>.
    /// Carries per-tier outcomes plus the golden-vector run (a precondition of #16 §5.5
    /// per FR-DS-009-GATE). The CI gate consumes <see cref="AllPassed"/> as the
    /// merge-block signal per FR-TS-012. Empty inputs fail closed (<see cref="AllPassed"/>
    /// is false) so a degenerate caller cannot silently green-light the gate.
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

        /// <summary>
        /// True only if both lists are non-empty AND every tier passed AND every
        /// golden-vector corpus passed. Empty input fails closed per AR-1 M-3.
        /// </summary>
        public bool AllPassed { get; }

        /// <summary>
        /// Initialises an aggregated suite result. Both arguments MUST be non-null;
        /// <see cref="ArgumentNullException"/> is thrown otherwise. Empty lists are
        /// permitted but cause <see cref="AllPassed"/> to be false (fail-closed
        /// semantics — an empty suite cannot pass FR-DS-009-GATE).
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="tierResults"/> or <paramref name="goldenVectorResults"/> is null.
        /// </exception>
        public DeterminismSuiteResult(
            IReadOnlyList<DeterminismTierResult> tierResults,
            IReadOnlyList<GoldenVectorResult> goldenVectorResults)
        {
            if (tierResults == null)
            {
                throw new ArgumentNullException(nameof(tierResults));
            }
            if (goldenVectorResults == null)
            {
                throw new ArgumentNullException(nameof(goldenVectorResults));
            }

            // Always copy on construction (AR-4 M-1). The previous AR-3 L-4 fast path
            // ("skip wrap if already ReadOnlyCollection<T>") did NOT achieve defensive
            // copy: ReadOnlyCollection<T> wraps an IList<T>, and a caller retaining the
            // underlying list could mutate it post-construction and have the change
            // appear through TierResults. Always-copy at the suite boundary supersedes
            // the AR-2 L-1 producer-side wrap (now reverted in DeterminismGate.RunTiers
            // since it created a double-copy with this defensive copy).
            DeterminismTierResult[] tierCopy = new DeterminismTierResult[tierResults.Count];
            for (int i = 0; i < tierCopy.Length; i++)
            {
                tierCopy[i] = tierResults[i];
            }
            GoldenVectorResult[] goldenCopy = new GoldenVectorResult[goldenVectorResults.Count];
            for (int i = 0; i < goldenCopy.Length; i++)
            {
                goldenCopy[i] = goldenVectorResults[i];
            }
            TierResults         = new ReadOnlyCollection<DeterminismTierResult>(tierCopy);
            GoldenVectorResults = new ReadOnlyCollection<GoldenVectorResult>(goldenCopy);

            // Fail-closed on empty input: a suite with no tiers or no corpora cannot pass
            // FR-DS-009-GATE even if every present element passed. AR-1 M-3.
            // Iterate over the copied arrays so the pass-loop and the locked AllPassed
            // are computed against the same snapshot the property surface exposes.
            bool allPassed = tierCopy.Length > 0 && goldenCopy.Length > 0;
            for (int i = 0; allPassed && i < tierCopy.Length; i++)
            {
                if (!tierCopy[i].Passed)
                {
                    allPassed = false;
                }
            }
            for (int i = 0; allPassed && i < goldenCopy.Length; i++)
            {
                if (!goldenCopy[i].Passed)
                {
                    allPassed = false;
                }
            }

            AllPassed = allPassed;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-02 | —      | Initial implementation.                                            |
// | 1.1     | 2026-06-02 | —      | AR-1 M-3: empty tier or golden-vector lists now fail closed         |
// |         |            |        | (AllPassed=false); previously empty input silently passed the gate. |
// |         |            |        | AR-1 L-4: constructor null-checks both list arguments with explicit |
// |         |            |        | ArgumentNullException instead of letting .Count NRE.                |
// | 1.2     | 2026-06-02 | —      | AR-3 L-4: constructor re-wraps input lists in ReadOnlyCollection<T> |
// |         |            |        | unless already wrapped, closing the AR-2 L-1 contract leak for     |
// |         |            |        | external direct constructions that bypass DeterminismGate.RunTiers. |
// | 1.3     | 2026-06-02 | —      | AR-4 M-1: drop the AR-3 L-4 fast-path bypass (skip wrap if already |
// |         |            |        | ReadOnlyCollection<T>) — it did not achieve defensive copy because |
// |         |            |        | the wrapped IList<T> could still be mutated through a retained     |
// |         |            |        | caller reference. Constructor now always copies inputs into fresh  |
// |         |            |        | arrays before wrapping. Pass-loop iterates the copied arrays so   |
// |         |            |        | the AllPassed verdict and the property surface are computed       |
// |         |            |        | against the same snapshot. Supersedes the AR-2 L-1 producer-side  |
// |         |            |        | wrap in DeterminismGate.RunTiers (reverted in DeterminismGate v1.4).|
#endregion
