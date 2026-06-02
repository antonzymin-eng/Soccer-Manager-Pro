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
using System.Collections.ObjectModel;

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
        /// <summary>
        /// [FIXED] Repo-relative root of the #16 §9.5 #4 golden-vector corpora.
        /// Catalogue entries concatenate corpus file names onto this prefix.
        /// </summary>
        private const string GoldenVectorRootRelPath =
            "docs/specs/deterministic-sim/golden-vectors/";

        /// <summary>
        /// [FIXED] Stage 0 deferred-status diagnostic emitted by <see cref="Run"/>
        /// until the §7.5 D1 test-runner pin lands. Names the upstream authority
        /// (<c>TacticalDirector.DeterministicSim.Tests</c>) and explains the
        /// FR-DS-009-GATE consumption path during the deferral window.
        /// </summary>
        private const string Stage0DeferredDiagnostic =
            "Stage 0: KAT execution lives in TacticalDirector.DeterministicSim.Tests; " +
            "this runner becomes authoritative once the §7.5 D1 test-runner pin lands. " +
            "FR-DS-009-GATE consumes the test-assembly results until then.";

        /// <summary>
        /// Returns the catalogue of golden-vector corpora pinned by #16 §9.5 #4 (a/b/c).
        /// The list is materialised on every call (CI tooling, not a hot path);
        /// allocate-and-return is acceptable here per Spec #19 §1.0 (no game-loop code).
        /// Wrapped via <see cref="ReadOnlyCollection{T}"/> so callers cannot cast back to
        /// a mutable <c>GoldenVectorEntry[]</c> (AR-2 L-1).
        /// </summary>
        public static IReadOnlyList<GoldenVectorEntry> Catalogue()
        {
            GoldenVectorEntry[] entries = new GoldenVectorEntry[]
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
            return new ReadOnlyCollection<GoldenVectorEntry>(entries);
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
        /// the gate per #16 §5.5. Wrapped via <see cref="ReadOnlyCollection{T}"/> so
        /// callers cannot cast back to a mutable <c>GoldenVectorResult[]</c> (AR-2 L-1).
        /// </summary>
        public static IReadOnlyList<GoldenVectorResult> RunAll()
        {
            IReadOnlyList<GoldenVectorEntry> entries = Catalogue();
            GoldenVectorResult[] results = new GoldenVectorResult[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                results[i] = Run(entries[i]);
            }
            return new ReadOnlyCollection<GoldenVectorResult>(results);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-02 | —      | Initial implementation.                                            |
// | 1.1     | 2026-06-02 | —      | AR-1 M-1: GoldenVectorRootRelPath + Stage0DeferredDiagnostic       |
// |         |            |        | private consts gained XML <summary> docs per FR-CS-061             |
// |         |            |        | (every constant, any access level, requires summary).             |
// | 1.2     | 2026-06-02 | —      | AR-2 L-1: Catalogue() + RunAll() wrap their backing arrays in     |
// |         |            |        | ReadOnlyCollection<T> so callers cannot cast IReadOnlyList<T>     |
// |         |            |        | back to a mutable T[] and corrupt the read-only contract.         |
#endregion
