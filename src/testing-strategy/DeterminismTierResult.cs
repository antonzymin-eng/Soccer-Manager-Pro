// File:     src/testing-strategy/DeterminismTierResult.cs
// Created:  2026-06-02
// Modified: 2026-06-02
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.2 / §5.7, Code Standards #20
// Purpose:  Per-tier outcome produced by DeterminismGate. Failures in any tier
//           block merges per KD-2 / FR-TS-012; Spec #19 does not soften #16's
//           exit criteria.

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// Outcome of running one tier of #16 §5's regression suite. Carries pass/fail,
    /// test counts, and a diagnostic string used by the CI gate. Aggregated into
    /// <see cref="DeterminismSuiteResult"/>.
    /// Testing Strategy &amp; Framework #19 §3.2 / KD-2 / FR-TS-012.
    /// </summary>
    public readonly struct DeterminismTierResult
    {
        /// <summary>Which #16 §5 tier produced this result.</summary>
        public DeterminismTierKind Tier { get; }

        /// <summary>True if every test in the tier passed; false blocks the merge per FR-TS-012.</summary>
        public bool Passed { get; }

        /// <summary>Total number of tests executed in the tier.</summary>
        public int TestsExecuted { get; }

        /// <summary>Number of failing tests in the tier.</summary>
        public int TestsFailed { get; }

        /// <summary>
        /// Human-readable diagnostic. For passing tiers, "OK (N tests)". For failing tiers,
        /// names the first failure. For Stage 0 deferred tiers, the deferral reason and the
        /// gating decision (e.g. D1 test runner pin).
        /// </summary>
        public string Diagnostic { get; }

        /// <summary>
        /// Initialises a tier result. <paramref name="diagnostic"/> MUST be non-empty.
        /// </summary>
        public DeterminismTierResult(
            DeterminismTierKind tier,
            bool passed,
            int testsExecuted,
            int testsFailed,
            string diagnostic)
        {
            Tier          = tier;
            Passed        = passed;
            TestsExecuted = testsExecuted;
            TestsFailed   = testsFailed;
            Diagnostic    = diagnostic;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-02 | —      | Initial implementation. |
#endregion
