// File:     src/testing-strategy/GoldenVectorRunner.cs
// Created:  2026-06-02
// Modified: 2026-06-02
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.8 / Appendix F,
//           Deterministic Simulation #16 §9.5 acceptance criterion #4 / §5.5 (FR-DS-009-GATE),
//           Code Standards #20
// Purpose:  Catalogues the three golden-vector corpora pinned by #16 §9.5 #4 (a/b/c)
//           and provides the per-entry runner surface consumed by the CI determinism gate.
//           Stage 0: catalogue-only (Run() returns a deferred result naming the upstream
//           authority — KAT execution lives in DeterministicSimTests under the
//           test-runner pin D1). Stage 0+1: parses the markdown corpus and invokes
//           DeterministicRngService / CanonicalSerializer directly.

using System.Collections.Generic;

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// Single integration point for the three golden-vector corpora pinned by
    /// Deterministic Simulation #16 §9.5 acceptance criterion #4. Every corpus
    /// MUST pass for FR-DS-009-GATE (Stage 0 certification gate, #16 §5.5).
    ///
    /// Stage 0 surface: <see cref="Catalogue"/> returns the entry list; <see cref="Run"/>
    /// returns a deferred-status <see cref="GoldenVectorResult"/> naming the upstream
    /// authority. The authoritative KAT execution lives in the
    /// <c>DeterministicSimTests</c> assembly under the §7.5 D1 test-runner pin.
    /// At Stage 0+1 this runner parses the source markdown corpus and invokes
    /// <c>DeterministicRngService</c> / <c>CanonicalSerializer</c> directly so the
    /// CI gate has a single entry point that does not depend on a test runner.
    /// Testing Strategy &amp; Framework #19 §3.8 / Deterministic Simulation #16 §9.5.
    /// </summary>
    public static class GoldenVectorRunner
    {
        private const string GoldenVectorRootRelPath =
            "docs/specs/deterministic-sim/golden-vectors/";

        private const string Stage0DeferredDiagnostic =
            "Stage 0: KAT execution lives in TacticalDirector.DeterministicSim.Tests; " +
            "this runner becomes authoritative once the §7.5 D1 test-runner pin lands. " +
            "FR-DS-009-GATE consumes the test-assembly results until then.";

        /// <summary>
        /// Returns the catalogue of golden-vector corpora pinned by #16 §9.5 #4 (a/b/c).
        /// The list is materialised on every call (CI tooling, not a hot path);
        /// allocate-and-return is acceptable here per Spec #19 §1.0 (no game-loop code).
        /// </summary>
        public static IReadOnlyList<GoldenVectorEntry> Catalogue()
        {
            return new GoldenVectorEntry[]
            {
                new GoldenVectorEntry(
                    GoldenVectorKind.HkdfSha256Kat,
                    "HKDF-SHA256 Known-Answer Test Vectors",
                    GoldenVectorRootRelPath + "hkdf-sha256-kat.md",
                    "Deterministic Simulation #16 §3.4 RNG_KDF; RFC 5869 Appendix A.1–A.3"),

                new GoldenVectorEntry(
                    GoldenVectorKind.SipHash24Kat,
                    "SipHash-2-4-64 Known-Answer Test Vectors",
                    GoldenVectorRootRelPath + "siphash-2-4-kat.md",
                    "Deterministic Simulation #16 §3.4 RNG_STREAM_HASH; Aumasson & Bernstein 2012 App. A"),

                new GoldenVectorEntry(
                    GoldenVectorKind.CanonicalSerializeCorpus,
                    "SerializeCanonical Reference Corpus",
                    GoldenVectorRootRelPath + "serialize-canonical-corpus.md",
                    "Deterministic Simulation #16 §3.2.4.1 primitive encoding table"),
            };
        }

        /// <summary>
        /// Runs one catalogue entry. Stage 0 returns a deferred-status result naming the
        /// upstream authority — the KAT bodies live in <c>DeterministicSimTests</c> and
        /// are invoked through whatever test runner D1 pins. Returning a deferred result
        /// (rather than throwing) lets the CI gate aggregate all three corpora into a
        /// single FR-DS-009-GATE report without conditional logic.
        /// </summary>
        /// <param name="entry">Catalogue entry to run.</param>
        public static GoldenVectorResult Run(in GoldenVectorEntry entry)
        {
            return new GoldenVectorResult(
                entry,
                passed: false,
                vectorsExecuted: 0,
                vectorsFailed: 0,
                diagnostic: Stage0DeferredDiagnostic);
        }

        /// <summary>
        /// Runs every catalogue entry and returns the aggregated results. Used by the
        /// CI determinism gate to drive FR-DS-009-GATE: any non-passing result blocks
        /// the gate per #16 §5.5.
        /// </summary>
        public static IReadOnlyList<GoldenVectorResult> RunAll()
        {
            IReadOnlyList<GoldenVectorEntry> entries = Catalogue();
            GoldenVectorResult[] results = new GoldenVectorResult[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                results[i] = Run(entries[i]);
            }
            return results;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-02 | —      | Initial implementation. |
#endregion
