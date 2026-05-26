// File:     src/first-touch/BallDisplacementProcessor.cs
// Created:  2026-05-25
// Modified: 2026-05-25
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
            // High q → touches go where intended; low q → ball continues along original path.
            // IncomingDir is where the ball came FROM (negated velocity). §3.3.2.
            Vector2 ballVelXY = new Vector2(ctx.BallVelocity.x, ctx.BallVelocity.y);
            float blendThreshSq = FirstTouchConstants.BlendMinMagnitude * FirstTouchConstants.BlendMinMagnitude;
            Vector2 agentFacingXY = new Vector2(ctx.AgentFacing.x, ctx.AgentFacing.y);
            Vector2 incomingDir = ballVelXY.sqrMagnitude > blendThreshSq
                ? new Vector2(-ballVelXY.x, -ballVelXY.y).normalized
                : agentFacingXY;

            Vector2 intendedDirXY = new Vector2(ctx.IntendedTouchDirection.x, ctx.IntendedTouchDirection.y);
            Vector2 intendedDir = intendedDirXY.sqrMagnitude > blendThreshSq
                ? intendedDirXY.normalized
                : incomingDir;

            Vector2 blendedDir2D = Vector2.Lerp(incomingDir, intendedDir, q);

            // Fallback: if blended direction is near-zero, use agent facing.
            if (blendedDir2D.sqrMagnitude < blendThreshSq)
            {
                blendedDir2D = agentFacingXY.sqrMagnitude > blendThreshSq
                    ? agentFacingXY.normalized
                    : Vector2.right;
            }
            else
            {
                blendedDir2D = blendedDir2D.normalized;
            }

            // §3.3.4 — New ball position: agent position + displacement radius in blended direction.
            // Z is set to BallRadius so the ball rests on the ground.
            Vector2 newPos2D = new Vector2(ctx.AgentPosition.x, ctx.AgentPosition.y)
                             + blendedDir2D * r;

            // Clamp XY to pitch bounds.
            float clampedX = Mathf.Clamp(
                newPos2D.x,
                0.0f,
                FirstTouchConstants.PitchHalfLength * 2.0f);
            float clampedY = Mathf.Clamp(
                newPos2D.y,
                0.0f,
                FirstTouchConstants.PitchHalfWidth * 2.0f);

            Vector3 newBallPos = new Vector3(clampedX, clampedY, FirstTouchConstants.BallRadius);

            // §3.3.5 — New ball velocity: agent contribution + retained ball momentum. §3.3.5.
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
#endregion
