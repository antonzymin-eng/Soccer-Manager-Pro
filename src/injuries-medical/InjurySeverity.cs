// File:     src/injuries-medical/InjurySeverity.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Injuries & Medical #41 §2.2 / §3.2 (KD-4), F4; Code Standards #20
// Purpose:  The Stage-2 fixed severity tiers. None = healthy and is the zero value, so a fresh
//           InjuryState is available by construction.

namespace TacticalDirector.InjuriesMedical
{
    /// <summary>
    /// The Stage-2 fixed severity tiers (KD-4). Each non-<see cref="None"/> tier maps to a fixed
    /// recovery-days constant (<see cref="InjuriesMedicalConstants.RecoveryDaysFor"/>).
    /// <para>
    /// <see cref="None"/> is ordinal 0 and means <b>healthy</b> — so <c>default(InjurySeverity)</c> is
    /// the available state, and <see cref="MedicalStep.IsAvailable"/> is exactly
    /// <c>Severity == None</c>.
    /// </para>
    /// <para>
    /// Ordinals are APPEND-only: they are written into the persisted medical sub-blob as a byte, so
    /// renumbering would silently re-tier every saved injury. A value outside the defined set reaching
    /// a consuming seam fails loud rather than being clamped (F4).
    /// </para>
    /// </summary>
    public enum InjurySeverity : byte
    {
        /// <summary>Healthy — available for selection. The default and the recovered state.</summary>
        None = 0,

        /// <summary>A minor knock — the shortest fixed recovery tier.</summary>
        Minor = 1,

        /// <summary>A moderate injury — the middle fixed recovery tier.</summary>
        Moderate = 2,

        /// <summary>A serious injury — the longest fixed recovery tier.</summary>
        Serious = 3,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                  |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#41 T0, §2.2). |
#endregion
