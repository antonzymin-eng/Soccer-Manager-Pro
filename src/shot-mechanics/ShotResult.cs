// File:     src/shot-mechanics/ShotResult.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §2.4.2, Code Standards #20
// Purpose:  Output struct returned by ShotExecutor. Zero heap allocation (all value types).
//           ContactFrame sentinel -1 for non-Completed outcomes.

using UnityEngine;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Result of a shot execution cycle. Returned by ShotExecutor to callers and
    /// used to populate ShotExecutedEvent. Shot Mechanics #6 §2.4.2.
    /// </summary>
    public struct ShotResult
    {
        /// <summary>Execution outcome: Completed, Cancelled, Invalid, or Initiated.</summary>
        public ShotOutcome Outcome;

        /// <summary>
        /// Final velocity vector passed to Ball.ApplyKick() (world space, m/s).
        /// Vector3.zero if Outcome != Completed. §4.2.1 velocity encoding convention.
        /// </summary>
        public Vector3 FinalVelocity;

        /// <summary>Final spin vector passed to Ball.ApplyKick() (rad/s). Zero if Outcome != Completed.</summary>
        public Vector3 FinalSpin;

        /// <summary>World-space aim direction BEFORE error application. Exposed for debug and analytics.</summary>
        public Vector3 IntendedDirection;

        /// <summary>World-space aim direction AFTER error application (actual kick direction, unit vector).</summary>
        public Vector3 FinalDirection;

        /// <summary>
        /// Error offset applied in goal-relative (u, v) space. Zero if Outcome != Completed.
        /// Positive v = skied above target; negative u = pulled left of target. §3.6.
        /// </summary>
        public Vector2 ErrorOffset;

        /// <summary>Body mechanics score [0.0, 1.0] computed during this shot. §3.7. Zero if Outcome != Completed.</summary>
        public float BodyMechanicsScore;

        /// <summary>Power penalty scalar applied [≥ 1.0]. Exposed for post-shot analysis. §3.6 (FR-03).</summary>
        public float PowerPenaltyApplied;

        /// <summary>Kick speed (m/s) before direction encoding [V_FLOOR, V_CEILING]. §3.2.</summary>
        public float KickSpeed;

        /// <summary>Launch angle (degrees) used. Exposed for trajectory debugging. §3.3.</summary>
        public float LaunchAngleDeg;

        /// <summary>True if stumble was triggered. Informs Agent Movement via ShotExecutedEvent. §3.7, §4.3.3.</summary>
        public bool StumbleTriggered;

        /// <summary>
        /// Frame on which Ball.ApplyKick() was called. Replay synchronisation.
        /// Sentinel: -1 if Outcome != Completed.
        /// </summary>
        public int ContactFrame;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-27 | —      | Initial implementation. |
#endregion
