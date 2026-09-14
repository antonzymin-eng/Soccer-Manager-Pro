// ============================================================================
// File:     src/localization/GrammaticalGender.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Specs:    Localization & Accessibility #49 FR-LC-009, ERR-049-004
// Purpose:  Locale-neutral typed grammatical selector category.
// ============================================================================

namespace TacticalDirector.Localization
{
    /// <summary>Authored grammatical-gender selector values carried without localized text.</summary>
    public enum GrammaticalGender
    {
        /// <summary>No gender selector is present.</summary>
        Unspecified = 0,
        /// <summary>Masculine authored category.</summary>
        Masculine = 1,
        /// <summary>Feminine authored category.</summary>
        Feminine = 2,
        /// <summary>Neutral authored category.</summary>
        Neutral = 3,
        /// <summary>Producer-provided category outside the preceding three.</summary>
        Other = 4
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-09-11 | —      | Initial L1 typed grammatical selector enum. |
#endregion
