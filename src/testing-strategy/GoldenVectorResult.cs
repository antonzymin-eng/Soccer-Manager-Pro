// File:     src/testing-strategy/GoldenVectorResult.cs
// Created:  2026-06-02
// Modified: 2026-06-02
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.8,
//           Deterministic Simulation #16 §9.5 / §5.5 (FR-DS-009-GATE),
//           Code Standards #20
// Purpose:  Result returned by GoldenVectorRunner.Run for one catalogue entry.
//           A single failed entry blocks FR-DS-009-GATE per #16 §5.5.

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
        /// Initialises a golden-vector result. <paramref name="diagnostic"/> MUST be non-empty.
        /// </summary>
        public GoldenVectorResult(
            GoldenVectorEntry entry,
            bool passed,
            int vectorsExecuted,
            int vectorsFailed,
            string diagnostic)
        {
            Entry           = entry;
            Passed          = passed;
            VectorsExecuted = vectorsExecuted;
            VectorsFailed   = vectorsFailed;
            Diagnostic      = diagnostic;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-02 | —      | Initial implementation. |
#endregion
