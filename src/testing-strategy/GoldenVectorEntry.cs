// File:     src/testing-strategy/GoldenVectorEntry.cs
// Created:  2026-06-02
// Modified: 2026-06-02
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.8 / Appendix F,
//           Deterministic Simulation #16 §9.5 acceptance criterion #4,
//           Code Standards #20
// Purpose:  Catalogue entry describing one golden-vector corpus. Stage 0 ships
//           the catalogue (kind + source path + citation); Stage 0+1 the runner
//           parses the linked markdown table and executes the KATs.

using System;

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// One row in the <see cref="GoldenVectorRunner"/> catalogue. Identifies a corpus
    /// pinned by #16 §9.5 acceptance criterion #4 (a/b/c) and the on-disk source
    /// that the Stage 0+1 runner parses.
    /// Testing Strategy &amp; Framework #19 Appendix F glossary "Golden trace" /
    /// Deterministic Simulation #16 §9.5.
    /// Note: <c>default(GoldenVectorEntry)</c> bypasses the constructor's invariant
    /// checks (C# struct default-value semantics). All call sites that originate
    /// entries (e.g. <see cref="GoldenVectorRunner.Catalogue"/>) MUST use the public
    /// constructor; consumers SHOULD treat default-valued entries as malformed.
    /// </summary>
    public readonly struct GoldenVectorEntry
    {
        /// <summary>Kind discriminator — selects the reference algorithm to execute.</summary>
        public GoldenVectorKind Kind { get; }

        /// <summary>Human-readable corpus name (matches the markdown title).</summary>
        public string Name { get; }

        /// <summary>Repo-relative path to the markdown corpus file under <c>docs/specs/deterministic-sim/golden-vectors/</c>.</summary>
        public string SourcePath { get; }

        /// <summary>Authoritative citation in #16 for the algorithm under test.</summary>
        public string Citation { get; }

        /// <summary>
        /// Initialises one catalogue entry. Parameter order matches property declaration
        /// order (Kind / Name / SourcePath / Citation). All string fields MUST be non-null
        /// and non-empty per AR-2 M-1 (parallel to <see cref="GoldenVectorResult"/> +
        /// <see cref="DeterminismTierResult"/> invariant guards).
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="name"/>, <paramref name="sourcePath"/>, or
        /// <paramref name="citation"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="name"/>, <paramref name="sourcePath"/>, or
        /// <paramref name="citation"/> is empty.
        /// </exception>
        public GoldenVectorEntry(
            GoldenVectorKind kind,
            string name,
            string sourcePath,
            string citation)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (name.Length == 0)
            {
                throw new ArgumentException("must be non-empty", nameof(name));
            }
            if (sourcePath == null)
            {
                throw new ArgumentNullException(nameof(sourcePath));
            }
            if (sourcePath.Length == 0)
            {
                throw new ArgumentException("must be non-empty", nameof(sourcePath));
            }
            if (citation == null)
            {
                throw new ArgumentNullException(nameof(citation));
            }
            if (citation.Length == 0)
            {
                throw new ArgumentException("must be non-empty", nameof(citation));
            }

            Kind       = kind;
            Name       = name;
            SourcePath = sourcePath;
            Citation   = citation;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-02 | —      | Initial implementation.                                            |
// | 1.1     | 2026-06-02 | —      | AR-1 L-1: property declaration order reordered to                  |
// |         |            |        | Kind / Name / SourcePath / Citation, matching constructor params.  |
// | 1.2     | 2026-06-02 | —      | AR-2 M-1: constructor enforces non-empty name / sourcePath /       |
// |         |            |        | citation (parallel to GoldenVectorResult / DeterminismTierResult   |
// |         |            |        | invariant guards). AR-2 L-6: XML doc notes default-value bypass.   |
// | 1.3     | 2026-06-02 | —      | AR-3 L-1: null-vs-empty checks split — ArgumentNullException for  |
// |         |            |        | null, ArgumentException for empty (idiomatic .NET BCL convention; |
// |         |            |        | parallels the AR-2 L-3 range-vs-relation split).                   |
#endregion
