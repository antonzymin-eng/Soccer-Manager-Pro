// File:     src/pass-mechanics/PassResult.cs
// Created:  2026-05-26
// Modified: 2026-05-26
// Author:   —
// Spec:     Pass Mechanics #5 §2.4.2, Code Standards #20
// Purpose:  Output struct returned by PassExecutor. Contains full execution record
//           for event payload, analytics, and replay synchronisation.

using UnityEngine;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Full record of a pass execution attempt. Created by PassExecutor, consumed by event
    /// payload assembly and analytics. All value types — zero heap allocation.
    /// Pass Mechanics #5 §2.4.2.
    /// </summary>
    public struct PassResult
    {
        /// <summary>Outcome of the pass execution attempt.</summary>
        public PassOutcome Outcome;

        /// <summary>Velocity vector passed to Ball.ApplyKick(). Zero if Outcome != Completed.</summary>
        public Vector3 FinalVelocity;

        /// <summary>Spin vector passed to Ball.ApplyKick(). Zero if Outcome != Completed.</summary>
        public Vector3 FinalSpin;

        /// <summary>Pre-error aim point. Exposed for analytics and debug.</summary>
        public Vector3 AimPoint;

        /// <summary>
        /// Error angle applied in degrees. Zero if cancelled or invalid.
        /// Exposed for post-match pass accuracy attribution.
        /// </summary>
        public float ErrorAngleDeg;

        /// <summary>
        /// Lead distance: metres ahead of receiver's current position to interception point.
        /// Zero for player-targeted passes and stationary receivers.
        /// </summary>
        public float LeadDistance;

        /// <summary>Pass type that was executed. Copied from PassRequest for event payload convenience.</summary>
        public PassType PassType;

        /// <summary>
        /// Simulation frame on which Ball.ApplyKick() was called.
        /// Used for replay synchronisation. Zero if not Completed.
        /// </summary>
        public int ContactFrame;

        /// <summary>Match time in seconds at CONTACT. Zero if not Completed.</summary>
        public float ContactMatchTime;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                  |
// | 1.0     | 2026-05-26 | —      | Initial implementation. |
#endregion
