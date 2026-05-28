// File:     src/heading-mechanics/ContactQualityLabel.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Heading Mechanics #10 §2.2, §3.4, KD-2, FR-HE-020, Code Standards #20
// Purpose:  Telemetry label for contact quality bucket; never consumed by physics formulas.

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Telemetry label for the contact-quality timing bucket.
    /// Emitted in HeaderExecutedEvent.ContactQualityLabel and the heading.contact.quality.label counter.
    /// NEVER consumed by §3.5–§3.7 physics formulas (KD-2 / FR-HE-002).
    /// Centred bucket is OnTime — NOT Perfect (FR-HE-020 / pass-1 L-1).
    /// Heading Mechanics #10 §2.2 / §3.4.
    /// </summary>
    public enum ContactQualityLabel
    {
        /// <summary>timingOffsetMs &lt; -EARLY_LABEL_THRESHOLD_MS. §3.4.</summary>
        Early,

        /// <summary>-EARLY_LABEL_THRESHOLD_MS ≤ timingOffsetMs ≤ +LATE_LABEL_THRESHOLD_MS. §3.4.</summary>
        OnTime,

        /// <summary>timingOffsetMs &gt; +LATE_LABEL_THRESHOLD_MS. §3.4.</summary>
        Late
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
