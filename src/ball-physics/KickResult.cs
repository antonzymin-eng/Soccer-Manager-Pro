// File:     src/ball-physics/KickResult.cs
// Created:  2026-06-03
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Outcome enum returned by BallCollision.ApplyKick so callers can detect
//           non-finite-velocity rejection without scraping Debug.LogError output.

namespace TacticalDirector.BallPhysics
{
    /// <summary>Outcome of <see cref="BallCollision.ApplyKick"/>.</summary>
    public enum KickResult
    {
        /// <summary>Kick applied successfully.</summary>
        Applied,

        /// <summary>
        /// Velocity contained NaN or Infinity; kick was rejected and ball state unchanged.
        /// Callers MUST retry with a sanitized velocity or abort the kick — do not
        /// assume the ball transitioned out of Controlled.
        /// </summary>
        RejectedNonFiniteVelocity
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-03 | —      | Extracted from BallCollision.cs as part of AR-2 L-2 file split    |
// |         |            |        | (one public type per file, src/CLAUDE.md FILE NAMING). Originally  |
// |         |            |        | introduced by AR-1 M-5 (ApplyKick void → KickResult contract).     |
#endregion
