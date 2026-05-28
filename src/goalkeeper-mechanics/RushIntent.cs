// File:     src/goalkeeper-mechanics/RushIntent.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §2.2.5, §3.7, KD-15, KD-17, Code Standards #20
// Purpose:  Locked intent for a GK rush or sweep. rushTarget is locked at commit and
//           NOT re-read during the rush (intent-staleness policy per KD-15).

using UnityEngine;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Locked rush intent committed by the Decision Tree at the 10 Hz tactical tick.
    /// RushTarget is frozen at commit time and not re-read during the rush per KD-15
    /// (mirrors Heading #10 KD-17 intent-staleness policy).
    /// Goalkeeper Mechanics #11 §2.2.5 / §3.7 / KD-15.
    /// </summary>
    public struct RushIntent
    {
        /// <summary>World-space point the GK is committed to reach. Locked at commit; never updated mid-rush (KD-15). §3.7.2.</summary>
        public Vector3 RushTarget;

        /// <summary>
        /// Commitment level [0, 1]. Values above RUSH_COMMIT_THRESHOLD enable the rush per §3.1.
        /// Higher values indicate greater Decision Tree confidence in the rush decision.
        /// </summary>
        public float CommitmentLevel;

        /// <summary>
        /// 10 Hz tactical tick index at which the intent was locked.
        /// Used for telemetry and intent-age tracking. KD-17.
        /// </summary>
        public int AttemptCommittedTick;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
