// File:     src/testing-strategy/DeterminismGate.cs
// Created:  2026-06-02
// Modified: 2026-06-02
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.2 / §4.3.1 / §5.7 / FR-TS-011..018,
//           Deterministic Simulation #16 §5 / §5.5 (FR-DS-009-GATE),
//           Code Standards #20
// Purpose:  Single integration point through which #16 §5's regression suite is
//           invoked from Spec #19 (FR-TS-016). Duplicate entry points are forbidden.
//           Aggregates the golden-vector corpus run (FR-DS-009-GATE precondition)
//           with the four §5 tier outcomes into one DeterminismSuiteResult.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// Single integration point through which #16 §5's regression suite is invoked from
    /// Spec #19 per FR-TS-016. Bound to the canonical tier order via
    /// <see cref="DeterminismTierKind"/> (FR-TS-011). Failures block merges per FR-TS-012;
    /// Spec #19 does not soften or override #16's exit criteria.
    ///
    /// Stage 0 surface: <see cref="RunTiers"/> returns a deferred-status suite result
    /// naming the upstream authority. The §5 tier bodies live in
    /// <c>TacticalDirector.DeterministicSim.Tests</c> and are exercised through whatever
    /// test runner D1 pins. Aggregating into one result lets the CI gate read a single
    /// pass/fail signal even before D1 is pinned.
    /// Testing Strategy &amp; Framework #19 §3.2 / §4.3.1 / KD-2.
    /// </summary>
    public static class DeterminismGate
    {
        /// <summary>
        /// [FIXED] Stage 0 deferred-status diagnostic embedded in every tier result
        /// emitted by <see cref="RunTiers"/> until the §7.5 D1 test-runner pin lands.
        /// Names the upstream authority (<c>TacticalDirector.DeterministicSim.Tests</c>)
        /// and the FR-TS-012 merge-block consumption path during the deferral window.
        /// </summary>
        private const string Stage0DeferredDiagnostic =
            "Stage 0: tier bodies live in TacticalDirector.DeterministicSim.Tests; " +
            "this gate becomes authoritative once the §7.5 D1 test-runner pin lands. " +
            "FR-TS-012 merge-block consumes the test-assembly results until then.";

        /// <summary>
        /// [FIXED] Canonical #16 §5 tier order (Unit → Integration → Scenario → Soak)
        /// consumed by <see cref="RunTiers"/> per FR-TS-011. Pre-allocated once and
        /// exposed as <see cref="IReadOnlyList{T}"/> via <see cref="ReadOnlyCollection{T}"/>
        /// so the wrapper denies element-level mutation (AR-2 L-2).
        ///
        /// MAINTAINER NOTE (AR-2 L-5): if <see cref="DeterminismTierKind"/> gains a new
        /// member, this list MUST be extended atomically in the same commit — FR-TS-018
        /// forbids #19 introducing new determinism tier categories without a #16 §5
        /// revision, but a missed entry here would silently exclude the new tier from
        /// <see cref="RunTiers"/> and the gate would report a stale <see cref="DeterminismSuiteResult.AllPassed"/>.
        /// </summary>
        private static readonly IReadOnlyList<DeterminismTierKind> s_canonicalTierOrder =
            new ReadOnlyCollection<DeterminismTierKind>(new[]
            {
                DeterminismTierKind.Unit,
                DeterminismTierKind.Integration,
                DeterminismTierKind.Scenario,
                DeterminismTierKind.Soak,
            });

        /// <summary>
        /// Runs every #16 §5 tier in canonical order plus the golden-vector corpus and
        /// returns the aggregated result. Single entry point per FR-TS-016.
        ///
        /// Stage 0: returns deferred-status tier results. Stage 0+1: drives the pinned
        /// test runner directly.
        /// </summary>
        public static DeterminismSuiteResult RunTiers()
        {
            DeterminismTierResult[] tierResults = new DeterminismTierResult[s_canonicalTierOrder.Count];
            for (int i = 0; i < s_canonicalTierOrder.Count; i++)
            {
                tierResults[i] = new DeterminismTierResult(
                    s_canonicalTierOrder[i],
                    passed: false,
                    testsExecuted: 0,
                    testsFailed: 0,
                    diagnostic: Stage0DeferredDiagnostic);
            }

            IReadOnlyList<GoldenVectorResult> goldenVectorResults =
                GoldenVectorRunner.RunAll();

            // Wrap the local tierResults array via ReadOnlyCollection<T> so the
            // DeterminismSuiteResult.TierResults surface cannot be downcast back to
            // a mutable DeterminismTierResult[] (AR-2 L-1 parallel; GoldenVectorRunner.RunAll
            // already returns a wrapped collection).
            return new DeterminismSuiteResult(
                new ReadOnlyCollection<DeterminismTierResult>(tierResults),
                goldenVectorResults);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-02 | —      | Initial implementation.                                            |
// | 1.1     | 2026-06-02 | —      | AR-1 M-2: Stage0DeferredDiagnostic private const gained XML        |
// |         |            |        | <summary> per FR-CS-061. AR-1 L-6: tier order array promoted from  |
// |         |            |        | per-call local to private static readonly s_canonicalTierOrder.    |
// | 1.2     | 2026-06-02 | —      | AR-2 L-2: s_canonicalTierOrder wrapped in ReadOnlyCollection<T>    |
// |         |            |        | (typed IReadOnlyList<DeterminismTierKind>) so elements are no     |
// |         |            |        | longer reassignable through the field reference. AR-2 L-5:         |
// |         |            |        | maintainer note added next to the order list reminding contribs   |
// |         |            |        | to extend atomically when DeterminismTierKind gains a new member. |
// |         |            |        | AR-2 L-1 parallel: tierResults array wrapped in ReadOnlyCollection|
// |         |            |        | before construction of DeterminismSuiteResult so its surface       |
// |         |            |        | cannot be cast back to DeterminismTierResult[].                    |
#endregion
