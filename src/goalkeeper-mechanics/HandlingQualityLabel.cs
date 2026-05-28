// File:     src/goalkeeper-mechanics/HandlingQualityLabel.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.5, KD-1, KD-2, Code Standards #20
// Purpose:  Telemetry-only label for the outcome of a handling-quality scalar band-to-action
//           mapping. Physics NEVER branches on this label (KD-2).

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Telemetry label for handling-quality band-to-action outcome.
    /// Assigned for telemetry channel emission only; physics formulas never branch on this value (KD-2).
    /// Goalkeeper Mechanics #11 §3.5.
    /// </summary>
    public enum HandlingQualityLabel
    {
        /// <summary>handlingQualityScalar ≥ CATCH_THRESHOLD. Ball possession secured. §3.5.</summary>
        Caught,

        /// <summary>handlingQualityScalar in [PARRY_THRESHOLD, CATCH_THRESHOLD). Controlled rebound. §3.5.</summary>
        Parried,

        /// <summary>handlingQualityScalar in [DEFLECT_THRESHOLD, PARRY_THRESHOLD). Redirected but not controlled. §3.5.</summary>
        Deflected,

        /// <summary>handlingQualityScalar in [MIN_HANDLING_QUALITY, DEFLECT_THRESHOLD). Loose ball. §3.5.</summary>
        Spilled,

        /// <summary>handlingQualityScalar below MIN_HANDLING_QUALITY. No effective contact. §3.5.</summary>
        Missed
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
