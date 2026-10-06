// ============================================================================
// File:     src/localization/PluralCategory.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Specs:    Localization & Accessibility #49 §1 KD-3, FR-LC-009
// Purpose:  Bounded CLDR-style plural categories a selected-locale template may key forms on.
// ============================================================================

namespace TacticalDirector.Localization
{
    /// <summary>
    /// The bounded plural categories of KD-3. <see cref="Other"/> is the zero value and the required
    /// default form of every plural selector.
    /// </summary>
    public enum PluralCategory
    {
        /// <summary>Default category; every plural selector must author this form.</summary>
        Other = 0,

        /// <summary>CLDR "one" category.</summary>
        One = 1,

        /// <summary>CLDR "few" category.</summary>
        Few = 2,

        /// <summary>CLDR "many" category.</summary>
        Many = 3
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-10-06 | —      | Initial L2 plural category vocabulary (localization-l2-plan v0.5 §5.1). |
#endregion
