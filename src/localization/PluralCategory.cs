// File:     src/localization/PluralCategory.cs
// Created:  2026-10-07
// Modified: 2026-10-07
// Author:   —
// Specs:    Localization & Accessibility #49 KD-3, FR-LC-009
// Purpose:  Bounded authored plural categories; locale rules are supplied by content.

namespace TacticalDirector.Localization
{
    /// <summary>Plural vocabulary supported by the L2 template model.</summary>
    public enum PluralCategory
    {
        /// <summary>The default plural category.</summary>
        Other = 0,
        /// <summary>One-category form.</summary>
        One = 1,
        /// <summary>Few-category form.</summary>
        Few = 2,
        /// <summary>Many-category form.</summary>
        Many = 3
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | 1.0     | 2026-10-07 | —      | Initial L2 plural vocabulary. |
#endregion
