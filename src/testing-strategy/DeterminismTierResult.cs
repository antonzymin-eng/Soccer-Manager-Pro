// File:     src/testing-strategy/DeterminismTierResult.cs
// Created:  2026-06-02
// Modified: 2026-06-02
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.2 / §5.7, Code Standards #20
// Purpose:  Per-tier outcome produced by DeterminismGate. Failures in any tier
//           block merges per KD-2 / FR-TS-012; Spec #19 does not soften #16's
//           exit criteria.

using System;

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// Outcome of running one tier of #16 §5's regression suite. Carries pass/fail,
    /// test counts, and a diagnostic string used by the CI gate. Aggregated into
    /// <see cref="DeterminismSuiteResult"/>.
    /// Testing Strategy &amp; Framework #19 §3.2 / KD-2 / FR-TS-012.
    /// Note: <c>default(DeterminismTierResult)</c> bypasses the constructor's invariant
    /// checks (C# struct default-value semantics — <see cref="Diagnostic"/> will be null).
    /// All call sites that originate results (<see cref="DeterminismGate.RunTiers"/>)
    /// MUST use the public constructor; consumers SHOULD treat default-valued results
    /// as malformed. AR-2 L-6.
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
        /// Initialises a tier result. Enforces per AR-1 L-2/L-3:
        /// <paramref name="diagnostic"/> MUST be non-empty, counts MUST satisfy
        /// <c>0 ≤ testsFailed ≤ testsExecuted</c>, and
        /// <c>passed == (testsExecuted &gt; 0 &amp;&amp; testsFailed == 0)</c> — a tier
        /// can only pass when at least one test executed and none failed. The
        /// <c>(passed=false, executed=0, failed=0)</c> shape is reserved for the
        /// Stage 0 deferred-status path emitted by <see cref="DeterminismGate.RunTiers"/>.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="diagnostic"/> is null/empty or when the pass
        /// invariant is violated.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="testsExecuted"/> or <paramref name="testsFailed"/>
        /// is negative, or when <paramref name="testsFailed"/> exceeds
        /// <paramref name="testsExecuted"/>.
        /// </exception>
        public DeterminismTierResult(
            DeterminismTierKind tier,
            bool passed,
            int testsExecuted,
            int testsFailed,
            string diagnostic)
        {
            if (string.IsNullOrEmpty(diagnostic))
            {
                throw new ArgumentException("diagnostic must be non-empty", nameof(diagnostic));
            }
            if (testsExecuted < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(testsExecuted),
                    testsExecuted,
                    "must be non-negative");
            }
            if (testsFailed < 0 || testsFailed > testsExecuted)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(testsFailed),
                    testsFailed,
                    "must be non-negative and ≤ testsExecuted");
            }
            bool expectedPassed = testsExecuted > 0 && testsFailed == 0;
            if (passed != expectedPassed)
            {
                throw new ArgumentException(
                    "passed must equal (testsExecuted > 0 && testsFailed == 0)",
                    nameof(passed));
            }

            Tier          = tier;
            Passed        = passed;
            TestsExecuted = testsExecuted;
            TestsFailed   = testsFailed;
            Diagnostic    = diagnostic;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-02 | —      | Initial implementation.                                            |
// | 1.1     | 2026-06-02 | —      | AR-1 L-2/L-3 parallel: constructor enforces non-empty diagnostic,  |
// |         |            |        | 0 ≤ testsFailed ≤ testsExecuted, and passed == (testsFailed == 0). |
// | 1.2     | 2026-06-02 | —      | AR-2 L-3: range violations (negative counts, testsFailed >        |
// |         |            |        | testsExecuted) now throw ArgumentOutOfRangeException; the pass    |
// |         |            |        | invariant and the null/empty diagnostic check stay ArgumentExc-   |
// |         |            |        | eption (not range issues). AR-2 L-6: XML doc notes default-value  |
// |         |            |        | bypass.                                                            |
#endregion
