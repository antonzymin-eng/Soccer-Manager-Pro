// File:     src/localization/LocalizationConstants.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   —
// Spec:     Localization & Accessibility #49 Appendix A, FR-LC-011/017, Code Standards #20
// Purpose:  Fixed localization identities owned by the producer-agnostic presentation layer.

namespace TacticalDirector.Localization
{
    /// <summary>
    /// Fixed localization identities. This assembly owns no gameplay-tuned values.
    /// </summary>
    public static class LocalizationConstants
    {
        /// <summary>
        /// [FIXED] Identity locale whose content is the fallback/correctness anchor (#49 Appendix A).
        /// </summary>
        public const string BASE_LOCALE = "en";
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                  |
// | 1.0     | 2026-09-04 | —      | Initial T0 base-locale identity.       |
#endregion
