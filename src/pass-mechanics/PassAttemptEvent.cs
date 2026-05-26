// File:     src/pass-mechanics/PassAttemptEvent.cs
// Created:  2026-05-26
// Modified: 2026-05-26
// Author:   —
// Spec:     Pass Mechanics #5 §3.9.2, Code Standards #20
// Purpose:  PassAttemptEvent struct: published at CONTACT immediately after
//           Ball.ApplyKick() succeeds. ~100 bytes, all value types.

using UnityEngine;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Published at CONTACT state immediately after Ball.ApplyKick() succeeds.
    /// ~100 bytes, all value types — no heap allocation. Pass Mechanics #5 §3.9.2.
    /// Consumed by Statistics Engine (Stage 1) and Replay System (Stage 1).
    /// </summary>
    public struct PassAttemptEvent
    {
        /// <summary>ID of the passing agent.</summary>
        public int AgentId;

        /// <summary>Team of the passing agent.</summary>
        public int TeamId;

        /// <summary>Pass type executed.</summary>
        public PassType PassType;

        /// <summary>Cross sub-type (Flat if non-cross).</summary>
        public CrossSubType CrossSubType;

        /// <summary>Pre-error aim point (world space).</summary>
        public Vector3 TargetPosition;

        /// <summary>Final velocity vector passed to ApplyKick().</summary>
        public Vector3 FinalVelocity;

        /// <summary>Final spin vector passed to ApplyKick().</summary>
        public Vector3 FinalSpin;

        /// <summary>Error magnitude applied in degrees.</summary>
        public float ErrorAngleDeg;

        /// <summary>Scalar launch speed in m/s.</summary>
        public float KickSpeed;

        /// <summary>Through-ball lead distance in metres (0 for player-targeted).</summary>
        public float LeadDistance;

        /// <summary>True if the non-preferred foot was used.</summary>
        public bool IsWeakFoot;

        /// <summary>Target agent ID; -1 for space-targeted passes.</summary>
        public int TargetAgentId;

        /// <summary>Simulation frame at CONTACT.</summary>
        public int Frame;

        /// <summary>Match time in seconds at CONTACT.</summary>
        public float MatchTime;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-05-26 | —      | Extracted from PassEvents.cs per one-type-per-file rule (H4).   |
#endregion
