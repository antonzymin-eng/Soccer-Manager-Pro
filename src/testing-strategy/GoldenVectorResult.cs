// File:     src/testing-strategy/GoldenVectorResult.cs
// Created:  2026-06-02
// Modified: 2026-06-02
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.8,
//           Deterministic Simulation #16 §9.5 / §5.5 (FR-DS-009-GATE),
//           Code Standards #20
// Purpose:  Result returned by GoldenVectorRunner.Run for one catalogue entry.
//           A single failed entry blocks FR-DS-009-GATE per #16 §5.5.

using System;

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// Outcome of running one <see cref="GoldenVectorEntry"/>. Carries pass/fail,
    /// the number of vectors exercised, and a diagnostic string used by the CI
    /// gate (FR-DS-009-GATE).
    /// Testing Strategy &amp; Framework #19 §3.8 / Deterministic Simulation #16 §5.5.
    /// </summary>
    public readonly struct GoldenVectorResult
    {
        /// <summary>Catalogue entry that produced this result.</summary>
        public GoldenVectorEntry Entry { get; }

        /// <summary>True if every vector in the corpus produced its expected output bit-for-bit.</summary>
        public bool Passed { get; }

        /// <summary>Total number of vectors in the corpus exercised by this run.</summary>
        public int VectorsExecuted { get; }

        /// <summary>Number of vectors whose output did not match the expected value.</summary>
        public int VectorsFailed { get; }

        /// <summary>
        /// Human-readable diagnostic. For passing runs, "OK (N vectors)". For failing runs,
        /// names the first mismatch (corpus ID, expected vs. actual hex). For Stage 0 deferred
        /// runs, the deferral reason and the gating decision (e.g. D1 test runner pin).
        /// </summary>
        public string Diagnostic { get; }

        /// <summary>
        /// Initialises a golden-vector result. Enforces per AR-1 L-2/L-3:
        /// <paramref name="diagnostic"/> MUST be non-empty, counts MUST satisfy
        /// <c>0 ≤ vectorsFailed ≤ vectorsExecuted</c>, and
        /// <c>passed == (vectorsExecuted &gt; 0 &amp;&amp; vectorsFailed == 0)</c> — a corpus
        /// can only pass when at least one vector executed and none failed. The
        /// <c>(passed=false, executed=0, failed=0)</c> shape is reserved for the Stage 0
        /// deferred-status path emitted by <see cref="GoldenVectorRunner.Run"/>.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="diagnostic"/> is null/empty, when counts are
        /// negative or inconsistent, or when the pass invariant is violated.
        /// </exception>
        public GoldenVectorResult(
            GoldenVectorEntry entry,
            bool passed,
            int vectorsExecuted,
            int vectorsFailed,
            string diagnostic)
        {
            if (string.IsNullOrEmpty(diagnostic))
            {
                throw new ArgumentException("diagnostic must be non-empty", nameof(diagnostic));
            }
            if (vectorsExecuted < 0)
            {
                throw new ArgumentException("must be non-negative", nameof(vectorsExecuted));
            }
            if (vectorsFailed < 0 || vectorsFailed > vectorsExecuted)
            {
                throw new ArgumentException(
                    "must be non-negative and ≤ vectorsExecuted",
                    nameof(vectorsFailed));
            }
            bool expectedPassed = vectorsExecuted > 0 && vectorsFailed == 0;
            if (passed != expectedPassed)
            {
                throw new ArgumentException(
                    "passed must equal (vectorsExecuted > 0 && vectorsFailed == 0)",
                    nameof(passed));
            }

            Entry           = entry;
            Passed          = passed;
            VectorsExecuted = vectorsExecuted;
            VectorsFailed   = vectorsFailed;
            Diagnostic      = diagnostic;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-02 | —      | Initial implementation.                                            |
// | 1.1     | 2026-06-02 | —      | AR-1 L-2: constructor enforces non-empty diagnostic; AR-1 L-3:     |
// |         |            |        | enforces passed == (vectorsFailed == 0) and 0 ≤ vectorsFailed ≤   |
// |         |            |        | vectorsExecuted so malformed callers can no longer publish        |
// |         |            |        | inconsistent records.                                              |
#endregion
