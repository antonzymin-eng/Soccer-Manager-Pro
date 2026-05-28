// File:     src/goalkeeper-mechanics/DistributeIntent.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §2.2.6, §3.8, KD-16, KD-17, Code Standards #20
// Purpose:  Locked intent for GK ball distribution. Committed at the 10 Hz tactical tick;
//           mutated only by F-05/F-09 validation in GoalkeeperDistribution.ValidateTarget.

using UnityEngine;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Locked distribution intent from the Decision Tree. Committed at the 10 Hz tactical tick.
    /// The F-05 / F-09 validation pass (GoalkeeperDistribution.ValidateTarget) may nullify
    /// TargetReceiverId or clamp TargetPoint when the roster check fails at release time.
    /// Goalkeeper Mechanics #11 §2.2.6 / §3.8 / KD-16 / KD-17.
    /// </summary>
    public struct DistributeIntent
    {
        /// <summary>Distribution method (throw / roll / kick). Kinematic-profile lookup only (KD-1). §3.8.</summary>
        public DeliveryKind DeliveryKind;

        /// <summary>
        /// Optional receiver agent ID. Null when the GK targets a field zone rather than a specific player.
        /// Nullified by F-05 validation if receiver has been substituted. §3.8.2.
        /// </summary>
        public int? TargetReceiverId;

        /// <summary>World-space target point for the distribution. Clamped to pitch bounds by F-09 validation. §3.8.2.</summary>
        public Vector3 TargetPoint;

        /// <summary>
        /// Power intent [0, 1]. Scaled by accuracyCoeff (delivery-kind × attribute) in §3.8.1.
        /// 0 = minimum safe release; 1 = maximum power for this delivery kind.
        /// </summary>
        public float PowerIntent;

        /// <summary>Requested spin vector (world-space angular velocity) on the released ball. §3.8.</summary>
        public Vector3 SpinIntent;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
