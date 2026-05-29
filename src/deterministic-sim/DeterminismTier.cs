// File:     src/deterministic-sim/DeterminismTier.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Deterministic Simulation #16 §3.3, §3.4, Code Standards #20
// Purpose:  Tier classification for authoritative fields. Determines digest inclusion rules and
//           comparator policy (bitwise equality vs. epsilon vs. exclusion).

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Determinism tier for an authoritative field.
    /// Used by DivergenceDetector to select the correct comparator.
    /// Deterministic Simulation #16 §3.3.
    /// </summary>
    public enum DeterminismTier : byte
    {
        /// <summary>
        /// Tier A — Authoritative Hard.
        /// Bitwise exact equality is required. NaN/Inf triggers ERR_DS_TIERA_NONFINITE.
        /// Restricted to serial-path fields at Stage 0; parallel-touched floats must be Tier B.
        /// §3.3 / FR-DS-013.
        /// </summary>
        TierA = 0,

        /// <summary>
        /// Tier B — Bounded-Authoritative.
        /// Epsilon tolerance is permitted; an approved tolerance row in the tolerance matrix is mandatory
        /// (absence triggers ERR_DS_TIERB_TOLERANCE_MISSING). Non-canonical NaN triggers ERR_DS_TIERB_NONFINITE.
        /// §3.3 / FR-DS-011.
        /// </summary>
        TierB = 1,

        /// <summary>
        /// Tier C — Non-Authoritative.
        /// VFX, UI, audio presentation state. Excluded from all determinism digest scopes.
        /// §3.3.
        /// </summary>
        TierC = 2,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
