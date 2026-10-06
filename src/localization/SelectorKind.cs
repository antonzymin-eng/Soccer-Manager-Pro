// ============================================================================
// File:     src/localization/SelectorKind.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Specs:    Localization & Accessibility #49 §1 KD-3, FR-LC-009
// Purpose:  The one bounded selector a template variant may declare.
// ============================================================================

namespace TacticalDirector.Localization
{
    /// <summary>Which bounded grammatical selector a template variant declares, if any.</summary>
    public enum SelectorKind
    {
        /// <summary>Plain variant: no selector, a single text.</summary>
        None = 0,

        /// <summary>Forms keyed by <see cref="PluralCategory"/>, chosen from a cardinal operand.</summary>
        Plural = 1,

        /// <summary>Forms keyed by <see cref="GrammaticalGender"/>, chosen from a gender operand.</summary>
        Gender = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-10-06 | —      | Initial L2 selector kind (localization-l2-plan v0.5 §5.1, Q2). |
#endregion
