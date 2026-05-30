// File:     src/deterministic-sim/DivergenceClass.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Deterministic Simulation #16 §3.3, §3.4 (FR-DS-007), Code Standards #20
// Purpose:  Divergence classification taxonomy returned by DivergenceDetector.

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Divergence classification for desynchronisation events.
    /// DivergenceDetector maps field-tier differences to this taxonomy.
    /// Deterministic Simulation #16 §3.3 / FR-DS-007.
    /// </summary>
    public enum DivergenceClass : byte
    {
        /// <summary>No divergence detected between the two traces.</summary>
        None = 0,

        /// <summary>
        /// Hard desync: a Tier A field differs between traces.
        /// Requires session abort or full state resync before continuing.
        /// §3.3 / FR-DS-007.
        /// </summary>
        HardDesync = 1,

        /// <summary>
        /// Soft drift: a Tier B field exceeds its approved epsilon tolerance.
        /// Warning-level; does not require immediate abort but must be logged and investigated.
        /// §3.3 / FR-DS-007.
        /// </summary>
        SoftDrift = 2,

        /// <summary>
        /// Cosmetic divergence: a Tier C field differs.
        /// No action required; Tier C is non-authoritative.
        /// §3.3 / FR-DS-007.
        /// </summary>
        Cosmetic = 3,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
