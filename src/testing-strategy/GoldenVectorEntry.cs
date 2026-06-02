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

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// One row in the <see cref="GoldenVectorRunner"/> catalogue. Identifies a corpus
    /// pinned by #16 §9.5 acceptance criterion #4 (a/b/c) and the on-disk source
    /// that the Stage 0+1 runner parses.
    /// Testing Strategy &amp; Framework #19 Appendix F glossary "Golden trace" /
    /// Deterministic Simulation #16 §9.5.
    /// </summary>
    public readonly struct GoldenVectorEntry
    {
        /// <summary>Kind discriminator — selects the reference algorithm to execute.</summary>
        public GoldenVectorKind Kind { get; }

        /// <summary>Repo-relative path to the markdown corpus file under <c>docs/specs/deterministic-sim/golden-vectors/</c>.</summary>
        public string SourcePath { get; }

        /// <summary>Authoritative citation in #16 for the algorithm under test.</summary>
        public string Citation { get; }

        /// <summary>Human-readable corpus name (matches the markdown title).</summary>
        public string Name { get; }

        /// <summary>
        /// Initialises one catalogue entry. All fields are required and copied verbatim.
        /// </summary>
        public GoldenVectorEntry(
            GoldenVectorKind kind,
            string name,
            string sourcePath,
            string citation)
        {
            Kind       = kind;
            Name       = name;
            SourcePath = sourcePath;
            Citation   = citation;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-02 | —      | Initial implementation. |
#endregion
