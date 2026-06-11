// File:     src/first-touch/BallDisplacementProcessor.cs
// Created:  2026-05-25
// Modified: 2026-06-10
// Author:   —
// Spec:     First Touch Mechanics #4 §3.3, Code Standards #20
// Purpose:  Computes new ball position and velocity after a touch using direction blend and momentum retention.

using UnityEngine;

namespace TacticalDirector.FirstTouch
{
    /// <summary>
    /// Computes new ball world position and velocity for one first-touch event.
    /// Implements direction blend (§3.3.2), pitch clamped position (§3.3.4),
    /// and momentum-retained velocity (§3.3.5). First Touch Mechanics #4 §3.3.
    /// </summary>
    internal static class BallDisplacementProcessor
    {
        /// <summary>
        /// Computes new ball position and velocity after applying the touch.
        /// First Touch Mechanics #4 §3.3.
        /// </summary>
        /// <param name="ctx">Per-touch input context.</param>
        /// <param name="q">Control quality scalar [0,1].</param>
        /// <param name="r">Displacement radius (m) from TouchRadiusCalculator.</param>
        internal static (Vector3 newBallPos, Vector3 newBallVel) Compute(
            in FirstTouchContext ctx,
            float q,
            float r)
        {
            // §3.3.2 — Direction blend: blend intended direction with incoming ball direction.
            // High q → touches go where intended; low q → ball continues along its original
            // travel path (the §3.3.2 "error attractor": a poorly executed touch deflects the
            // ball FURTHER ALONG its original path). ERR-004-003: IncomingDir is the ball's
            // TRAVEL direction (+velocity) — the pre-AR-7 negation displaced a heavy touch
            // back toward the passer while §3.3.5 BallRetained kept the momentum forward.
            // (Contrast OrientationDetector, where negating velocity IS correct: it compares
            // facing against the approach direction.)
            Vector2 ballVelXY = new Vector2(ctx.BallVelocity.x, ctx.BallVelocity.y);
            Vector2 agentFacingXY = new Vector2(ctx.AgentFacing.x, ctx.AgentFacing.y);
            Vector2 incomingDir = ballVelXY.sqrMagnitude > FirstTouchConstants.BLEND_MIN_MAGNITUDE_SQ
                ? ballVelXY.normalized
                : (agentFacingXY.sqrMagnitude > FirstTouchConstants.BLEND_MIN_MAGNITUDE_SQ
                    ? agentFacingXY.normalized
                    : Vector2.right);

            Vector2 intendedDirXY = new Vector2(ctx.IntendedTouchDirection.x, ctx.IntendedTouchDirection.y);
            Vector2 intendedDir = intendedDirXY.sqrMagnitude > FirstTouchConstants.BLEND_MIN_MAGNITUDE_SQ
                ? intendedDirXY.normalized
                : incomingDir;

            Vector2 blendedDir2D = Vector2.Lerp(incomingDir, intendedDir, q);

            // §3.3.2 SAFETY fallback (AR-7 M-2): when intended and incoming are nearly opposite
            // (e.g. playing the ball back the way it came at q ≈ 0.5) the blend degenerates to
            // near-zero. Spec mandates fallback to IncomingDir — ball follows its original path,
            // the correct heavy-touch behaviour. incomingDir is unit-length by construction.
            blendedDir2D = blendedDir2D.sqrMagnitude < FirstTouchConstants.BLEND_MIN_MAGNITUDE_SQ
                ? incomingDir
                : blendedDir2D.normalized;

            // §3.3.4 — New ball position: agent position + displacement radius in blended direction.
            // Z is set to BallRadius so the ball rests on the ground.
            Vector2 newPos2D = new Vector2(ctx.AgentPosition.x, ctx.AgentPosition.y)
                             + blendedDir2D * r;

            // Clamp XY to pitch bounds. §3.3.4: [0, PITCH_LENGTH] × [0, PITCH_WIDTH].
            float clampedX = Mathf.Clamp(newPos2D.x, 0.0f, FirstTouchConstants.PitchLength);
            float clampedY = Mathf.Clamp(newPos2D.y, 0.0f, FirstTouchConstants.PitchWidth);

            Vector3 newBallPos = new Vector3(clampedX, clampedY, FirstTouchConstants.BallRadius);

            // §3.3.5 — New ball velocity: agent contribution + retained ball momentum.
            // AgentContrib = ActualDir × min(agentSpeed, DRIBBLE_MAX_SPEED); scaled by q.
            // BallRetained = ball.Velocity × (1 - q) × MOMENTUM_RETENTION; uses original direction.
            float agentSpeed = ctx.AgentVelocity.magnitude;
            float agentContribSpeed = Mathf.Min(agentSpeed, FirstTouchConstants.DribbleMaxSpeed);

            Vector3 ballRetained = ctx.BallVelocity * (1.0f - q) * FirstTouchConstants.MomentumRetentionContact;

            Vector3 newBallVel = new Vector3(
                blendedDir2D.x * agentContribSpeed * q + ballRetained.x,
                blendedDir2D.y * agentContribSpeed * q + ballRetained.y,
                0.0f);

            // Hard cap on touch output speed.
            float newSpeed = newBallVel.magnitude;
            if (newSpeed > FirstTouchConstants.TouchMaxBallSpeed)
            {
                newBallVel = (newBallVel / newSpeed) * FirstTouchConstants.TouchMaxBallSpeed;
            }

            return (newBallPos, newBallVel);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                                       |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                                                              |
// | 1.1     | 2026-05-26 | —      | H-4 fix: IncomingDir negated (approach direction per §3.3.2); H-5 fix: velocity formula adds AgentContrib. |
// | 1.2     | 2026-05-26 | —      | Adversarial review pass 2: replaced PitchHalfLength*2.0f and PitchHalfWidth*2.0f with PitchLength/PitchWidth constants (no magic literals); removed duplicate §3.3.5 comment reference. |
// | 1.3     | 2026-06-06 | —      | AR-5 M-2 follow-on: BlendMinMagnitude → BLEND_MIN_MAGNITUDE (ALL_CAPS [FIXED] rename in FirstTouchConstants). |
// | 1.4     | 2026-06-06 | —      | AR-6 L-1: blendThreshSq local dropped; three sqrMagnitude predicates now read BLEND_MIN_MAGNITUDE_SQ directly from FirstTouchConstants. |
// | 1.5     | 2026-06-10 | —      | AR-7 H-1 (ERR-004-003): IncomingDir un-negated — now the ball's TRAVEL direction (+velocity), matching §3.3.2 intent prose and §3.3.5 momentum retention; the v1.1 "H-4 fix" negation displaced heavy touches back toward the passer against their own retained momentum. Spec §3.3.2 pseudocode patched in the same commit. AR-7 M-2: degenerate-blend fallback now IncomingDir per §3.3.2 (was agentFacing/Vector2.right deviation); incomingDir made unconditionally unit-length (agentFacing → Vector2.right chain hoisted into its construction). |
#endregion
