// ============================================================================
// File:     src/localization/ILocalizer.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Specs:    Localization & Accessibility #49 §2.1-§2.2, FR-LC-001/003/004/012
// Purpose:  Producer-agnostic surface-string seam for static and procedural text.
// ============================================================================

namespace TacticalDirector.Localization
{
    /// <summary>Single display-side localization seam for static and procedural strings.</summary>
    public interface ILocalizer
    {
        /// <summary>Resolves one stable static localization key for the current display locale.</summary>
        string Resolve(LocalizationKey key);

        /// <summary>Renders one already-decided locale-neutral procedural request.</summary>
        string Render(in LocalizedTextRequest request);
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|--------|
// | 1.0     | 2026-09-11 | —      | Initial approved L1 Resolve/Render seam. |
#endregion
