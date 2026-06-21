// File:     src/tactical-instructions/GkDistributionPolicy.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.4, §3.4, Code Standards #20
// Purpose:  Default goalkeeper distribution policy. Sets the default fields of #11
//           DistributeIntent (DeliveryKind, target, power) at T2 (FR-TI-022).

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Goalkeeper distribution policy (FR-TI-022). 6-valued; <see cref="SlowDown"/> (index 0) is the
    /// <see cref="TeamTactic.Balanced"/> identity. The consumer (#11) maps each value to a default
    /// (DeliveryKind, target, power) triple for <c>DistributeIntent</c> at T2 (§3.4).
    /// ORDINAL STABILITY: byte-backed, APPEND-only (FR-TI-007). Append at the end only.
    /// </summary>
    public enum GkDistributionPolicy : byte
    {
        /// <summary>Slow the game down; default distribution (identity). §3.4.</summary>
        SlowDown = 0,

        /// <summary>Distribute quickly to restart attacks at pace. §3.4.</summary>
        Quick = 1,

        /// <summary>Short kick to a nearby outfield option. §3.4.</summary>
        ShortKick = 2,

        /// <summary>Long kick up the pitch. §3.4.</summary>
        LongKick = 3,

        /// <summary>Roll the ball out to a defender. §3.4.</summary>
        RollOut = 4,

        /// <summary>Throw out to a wide option. §3.4.</summary>
        ThrowOut = 5
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).   |
#endregion
