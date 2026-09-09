// File:     src/localization/ILocalizer.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   —
// Spec:     Localization & Accessibility #49 §2.1/§2.2, FR-LC-001/003/004/012, Code Standards #20
// Purpose:  Single producer-agnostic surface-string seam for static and procedural localized text.

namespace TacticalDirector.Localization
{
    /// <summary>
    /// The single localization seam through which user-facing static and procedural strings are rendered.
    /// </summary>
    public interface ILocalizer
    {
        /// <summary>
        /// Resolves one stable static localization key for the current display locale with base fallback.
        /// </summary>
        string Resolve(LocalizationKey key);

        /// <summary>
        /// Renders one already-decided procedural request without advancing simulation state or drawing RNG.
        /// </summary>
        string Render(in LocalizedTextRequest request);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                  |
// | 1.0     | 2026-09-04 | —      | Initial Resolve/Render seam contract.   |
#endregion
