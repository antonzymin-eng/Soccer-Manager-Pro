// File:     src/goalkeeper-mechanics/ClaimIntent.cs
// Created:  2026-05-28
// Modified: 2026-09-22 (W3 / ERR-011-012: live claim episode policy + locked lateral reach direction)
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §2.2.2, KD-17, Code Standards #20
// Purpose:  Locked intent for a GK cross / aerial ball-claim attempt. Committed at the
//           10 Hz tactical tick and held immutable for 60 Hz physics resolution (KD-17).

using UnityEngine;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Locked claim intent committed by the Stage-0 composition producer at the 10 Hz tactical tick.
    /// Fields are immutable once committed; the 60 Hz physics loop reads but never writes them (KD-17).
    /// ERR-011-012: the lateral reach side is locked at commit so a moving keeper cannot flip hands/sides
    /// mid-attempt while the bounded reach episode is in flight.
    /// Goalkeeper Mechanics #11 §2.2.2.
    /// </summary>
    public struct ClaimIntent
    {
        /// <summary>World-space target contact point where the GK intends to intercept the ball. §2.2.2.</summary>
        public Vector3 TargetContactPoint;

        /// <summary>
        /// Clutch firmness [0, 1]. Higher values reduce rebound scatter if duel is contested.
        /// 0 = minimal grip; 1 = maximum committed grip. §3.5 / CLUTCH_FIRMNESS_K_RETAIN.
        /// </summary>
        public float ClutchFirmness;

        /// <summary>
        /// Locked lateral reach direction in the world Y axis (-1, 0, +1), derived once from
        /// targetContactPoint.y - keeperPosition.y at commit. This is reach kinematics, not a collider. §3.6.1.
        /// </summary>
        public float ReachDirectionLateral;

        /// <summary>
        /// 10 Hz tactical tick index at which the intent was locked.
        /// Used by §3.2 reaction-pipeline to compute reactionOffsetMs (KD-17).
        /// </summary>
        public int AttemptCommittedTick;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
// | 1.1     | 2026-09-22 | —      | W3 / ERR-011-012: producer wording corrected; locked ReachDirectionLateral added for the bounded live claim episode. |
#endregion
