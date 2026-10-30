// File:     src/pass-mechanics/GoalkeeperDistributionFeedback.cs
// Created:  2026-09-26
// Author:   —
// Spec:     Pass Mechanics #5 §2.4.4, §3.8.13; Code Standards #20
// Purpose:  Terminal goalkeeper-distribution execution record.

using UnityEngine;

namespace TacticalDirector.PassMechanics
{
    /// <summary>Typed terminal result retained by #5 until the Match Engine consumes it.</summary>
    public struct GoalkeeperDistributionFeedback
    {
        /// <summary>Rejected, Cancelled, or Completed terminal kind.</summary>
        public GoalkeeperDistributionFeedbackKind Kind;
        /// <summary>CONTACT-time valid receiver, or no-agent sentinel.</summary>
        public int EffectiveTargetAgentId;
        /// <summary>CONTACT-time receiver position or deterministic receiverless fallback point.</summary>
        public Vector3 EffectiveTargetPosition;
        /// <summary>Actual release point for Completed outcomes.</summary>
        public Vector3 ReleasePoint;
        /// <summary>Actual applied launch velocity for Completed outcomes.</summary>
        public Vector3 FinalVelocity;
        /// <summary>Deterministic #5 error angle applied at CONTACT.</summary>
        public float ErrorAngleDeg;
        /// <summary>CONTACT frame, or -1 when no kick occurred.</summary>
        public int ContactFrame;
        /// <summary>CONTACT match time, or 0 when no kick occurred.</summary>
        public float ContactMatchTime;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-09-26 | —      | W8 B typed terminal feedback retained until host consumption; serialized in snapshot schema v24. |
#endregion
