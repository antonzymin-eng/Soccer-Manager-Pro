// File:     src/Core/Physics/Ball/BallCollision.cs
// Created:  2026-05-24
// Modified: 2026-05-24
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Goal-post collision, boundary detection, possession evaluation, and kick
//           application. Possession tracking is external to BallState (Option B).

using UnityEngine;
using System.Collections.Generic;

namespace TacticalDirector.BallPhysics
{
    /// <summary>Body parts used for deflection coefficient lookup.</summary>
    public enum BodyPart { Foot, Shin, Thigh, Torso, Head, Arm }

    /// <summary>Restart types awarded after the ball leaves the field of play.</summary>
    public enum RestartType { NONE, THROW_IN, GOAL_KICK, CORNER, KICKOFF }

    /// <summary>
    /// Speed and spin retention coefficients per body part for deflection calculations.
    /// </summary>
    public static class BodyPartCoefficients
    {
        private static readonly Dictionary<BodyPart, (float speedRetention, float spinRetention)> _coefficients
            = new Dictionary<BodyPart, (float, float)>
        {
            { BodyPart.Foot,  (0.75f, 0.30f) },
            { BodyPart.Shin,  (0.65f, 0.20f) },
            { BodyPart.Thigh, (0.60f, 0.40f) },
            { BodyPart.Torso, (0.55f, 0.50f) },
            { BodyPart.Head,  (0.70f, 0.10f) },
            { BodyPart.Arm,   (0.50f, 0.30f) }
        };

        /// <summary>Returns (speedRetention, spinRetention) for the given body part.</summary>
        public static (float speedRetention, float spinRetention) Get(BodyPart part)
        {
            return _coefficients.TryGetValue(part, out var coef) ? coef : (0.60f, 0.30f);
        }
    }

    /// <summary>
    /// Collision detection, boundary checks, possession evaluation, and kick application.
    /// Possession tracking is EXTERNAL to BallState (Option B design — see §3.1.11).
    /// </summary>
    public static class BallCollision
    {
        /// <summary>
        /// Handles ball collision with a goal post or crossbar.
        /// </summary>
        public static void ApplyGoalPostCollision(
            ref BallState ball,
            Vector3 contactPoint,
            Vector3 postCenter,
            BallEventLogger logger,
            float matchTime)
        {
            Vector3 normal = (contactPoint - postCenter).normalized;

            float   vn       = Vector3.Dot(ball.Velocity, normal);
            Vector3 vt       = ball.Velocity - vn * normal;
            float   vn_after = -BallPhysicsConstants.GoalPost.CoefficientOfRestitution * vn;

            ball.Velocity        = vt + vn_after * normal;
            ball.AngularVelocity *= BallPhysicsConstants.GoalPost.SpinRetention;

            logger?.LogGoalPostHit(ball, contactPoint, matchTime);
        }

        /// <summary>
        /// Checks if the ball has left the field of play.
        /// Ball must entirely cross the line. Stage 0: only detects ground-level exits
        /// (z &lt; Ball.Diameter). Goals scored at height require a dedicated goal-volume
        /// check at Stage 1+.
        /// </summary>
        public static (bool isOut, RestartType restart) CheckBoundaries(
            BallState ball,
            int lastTouchTeamID)
        {
            float x = ball.Position.x;
            float y = ball.Position.y;
            float z = ball.Position.z;
            float r = BallPhysicsConstants.Ball.RADIUS;

            bool lowEnough = z < BallPhysicsConstants.Ball.Diameter;

            if (lowEnough && (y < -r || y > BallPhysicsConstants.Pitch.WIDTH + r))
                return (true, RestartType.THROW_IN);

            if (lowEnough && x < -r)
            {
                if (IsInGoal(ball.Position, isHomeGoal: true))
                    return (true, RestartType.KICKOFF);
                return (true, lastTouchTeamID == 0 ? RestartType.CORNER : RestartType.GOAL_KICK);
            }

            if (lowEnough && x > BallPhysicsConstants.Pitch.LENGTH + r)
            {
                if (IsInGoal(ball.Position, isHomeGoal: false))
                    return (true, RestartType.KICKOFF);
                return (true, lastTouchTeamID == 1 ? RestartType.CORNER : RestartType.GOAL_KICK);
            }

            return (false, RestartType.NONE);
        }

        private static bool IsInGoal(Vector3 position, bool isHomeGoal)
        {
            float halfGoalWidth = BallPhysicsConstants.Pitch.GOAL_WIDTH / 2f;
            float centerY       = BallPhysicsConstants.Pitch.WIDTH / 2f;

            bool withinPosts   = position.y > centerY - halfGoalWidth
                              && position.y < centerY + halfGoalWidth;
            bool underCrossbar = position.z < BallPhysicsConstants.Pitch.GOAL_HEIGHT;

            return withinPosts && underCrossbar;
        }

        /// <summary>
        /// Returns true if an agent physically can take possession this frame.
        /// Does NOT modify BallState — pure predicate.
        /// Caller must: record possession in agent system, call SetBallControlled(), drive position.
        /// </summary>
        public static bool CheckPossession(
            BallState ball,
            Vector3 agentPosition,
            Vector3 agentVelocity)
        {
            // XY-plane distance only; height handled by ControlHeight check.
            float distance = Vector3.Distance(
                new Vector3(ball.Position.x, ball.Position.y, 0f),
                new Vector3(agentPosition.x, agentPosition.y, 0f));

            if (distance > BallPhysicsConstants.Possession.ControlRadius)
                return false;

            if ((ball.Velocity - agentVelocity).magnitude > BallPhysicsConstants.Possession.ControlVelocity)
                return false;

            if (ball.Position.z > BallPhysicsConstants.Possession.ControlHeight)
                return false;

            return true;
        }

        /// <summary>
        /// Transitions ball to CONTROLLED state. Called by agent system after CheckPossession.
        /// Does NOT record which agent has possession (Option B — agent system owns that).
        /// </summary>
        public static void SetBallControlled(ref BallState ball)
        {
            ball.State           = BallStateType.CONTROLLED;
            ball.Velocity        = Vector3.zero;
            ball.AngularVelocity = Vector3.zero;
        }

        /// <summary>
        /// Applies a kick impulse to the ball, releasing it from CONTROLLED.
        /// POSSESSION MODEL (Option B): transitions BallState out of CONTROLLED as the signal
        /// the agent system polls to clear its possession record.
        /// </summary>
        public static void ApplyKick(
            ref BallState ball,
            Vector3 velocity,
            Vector3 spin,
            int agentId,
            float matchTime,
            BallEventLogger logger = null)
        {
            if (!IsFiniteVector(velocity))
            {
                UnityEngine.Debug.LogError(
                    $"[BallPhysics] ApplyKick: Invalid velocity {velocity} from agent {agentId}. Kick rejected.");
                return;
            }

            if (!IsFiniteVector(spin))
            {
                UnityEngine.Debug.LogWarning(
                    $"[BallPhysics] ApplyKick: Invalid spin {spin} from agent {agentId}. Spin zeroed.");
                spin = Vector3.zero;
            }

            if (velocity.magnitude > BallPhysicsConstants.Limits.MaxVelocity)
            {
                velocity = velocity.normalized * BallPhysicsConstants.Limits.MaxVelocity;
                UnityEngine.Debug.LogWarning(
                    $"[BallPhysics] ApplyKick: Velocity clamped to {BallPhysicsConstants.Limits.MaxVelocity} m/s.");
            }

            if (spin.magnitude > BallPhysicsConstants.Limits.MaxSpin)
                spin = spin.normalized * BallPhysicsConstants.Limits.MaxSpin;

            ball.Velocity        = velocity;
            ball.AngularVelocity = spin;

            float horizontalSpeed = new UnityEngine.Vector2(velocity.x, velocity.y).magnitude;

            if (velocity.z > 0f)
                ball.State = BallStateType.AIRBORNE;
            else if (horizontalSpeed > BallPhysicsConstants.State.MinVelocity)
                ball.State = BallStateType.ROLLING;
            else
                ball.State = BallStateType.STATIONARY;

            ball.LastValidPosition = ball.Position;
            ball.LastValidVelocity = ball.Velocity;

            logger?.LogKick(
                ball, agentId,
                $"ApplyKick|v={velocity.magnitude:F1}m/s|s={spin.magnitude:F1}rad/s|→{ball.State}",
                matchTime);
        }

        private static bool IsFiniteVector(Vector3 v)
        {
            return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-24 | —      | Initial implementation.                                            |
// | 1.1     | 2026-05-24 | —      | Fix pass: namespace → TacticalDirector.BallPhysics; ALL_CAPS       |
// |         |            |        | constant refs → PascalCase; Stage 0 lowEnough limitation          |
// |         |            |        | documented in CheckBoundaries XML doc; file header per FR-CS-056.  |
#endregion
