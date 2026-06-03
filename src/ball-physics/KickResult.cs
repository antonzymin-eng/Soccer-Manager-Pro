// File:     src/ball-physics/KickResult.cs
// Created:  2026-06-03
// Modified: 2026-06-03 (AR-5 fix pass)
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Outcome enum returned by BallCollision.ApplyKick so callers can detect
//           non-finite-velocity rejection without scraping Debug.LogError output.

namespace TacticalDirector.BallPhysics
{
    /// <summary>
    /// Outcome of <see cref="BallCollision.ApplyKick"/>.
    /// ORDINAL STABILITY: future additions MUST be appended at the end of the enum.
    /// KickResult is currently a method-return-only value with no embedding in
    /// <c>BallEvent</c> or <c>BallState</c>, so the serialisation hazard is weaker
    /// than the sibling enums (<see cref="BallEventType"/>, <see cref="BallStateType"/>,
    /// <see cref="SurfaceType"/>, <see cref="RestartType"/>) — but Stage 1+ kick-
    /// outcome logs will inherit the same insertion-shifts-ordinals hazard, so the
    /// APPEND-only rule applies pre-emptively.
    /// </summary>
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
// | 1.1     | 2026-06-03 | —      | AR-5 fixes. M-1: file header Modified field added (FR-CS-056).    |
// |         |            |        | L-1: KickResult XML doc gains an ORDINAL STABILITY paragraph       |
// |         |            |        | parallel to AR-3 L-2 / AR-4 L-1 on the four sibling public enums.  |
#endregion
