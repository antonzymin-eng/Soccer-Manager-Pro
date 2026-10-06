// ============================================================================
// File:     src/localization/CitationClause.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Specs:    Localization & Accessibility #49 §3.2, §3.4, FR-LC-010
// Purpose:  One opaque citation clause row of a catalogue.
// ============================================================================

using System;

namespace TacticalDirector.Localization
{
    /// <summary>
    /// One clause row. The text is a complete authored sentence appended as <c>" " + text</c>; like
    /// living-world's clause table it is not expanded.
    /// </summary>
    public readonly struct CitationClause
    {
        /// <summary>Creates a clause row.</summary>
        public CitationClause(CitationClauseKey key, string text)
        {
            if (!key.IsValid)
            {
                throw new ArgumentException("A clause row requires a valid key.", nameof(key));
            }

            Key = key;
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }

        /// <summary>Producer-scoped clause identity.</summary>
        public CitationClauseKey Key { get; }

        /// <summary>Authored clause text.</summary>
        public string Text { get; }

        /// <summary>True for a row built through the constructor.</summary>
        public bool IsValid => Key.IsValid && Text != null;
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-10-06 | —      | Initial L2 clause row (localization-l2-plan v0.5 §5.1). |
#endregion
