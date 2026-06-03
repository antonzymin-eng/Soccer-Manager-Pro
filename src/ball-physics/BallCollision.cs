// File:     src/ball-physics/BallCollision.cs
// Created:  2026-05-24
// Modified: 2026-06-03
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Goal-post collision, boundary detection, possession evaluation, and kick
//           application. Possession tracking is external to BallState (Option B).

using UnityEngine;

namespace TacticalDirector.BallPhysics
{
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
        /// check at Stage 1+. <see cref="BallStateMachine.IsOutOfBounds"/> applies the
        /// same z gate so the two predicates agree.
        /// Corner-region precedence (Stage 0 simplification): when both the goal line and
        /// a touchline are crossed in the same frame, the touchline check runs first and
        /// classifies the exit as ThrowIn even though geometric reasoning would prefer
        /// the goal-line classification. Trajectory-based corner resolution is a Stage
        /// 1+ deliverable.
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
                return (true, RestartType.ThrowIn);

            // Home goal (x &lt; −r): the Y/Z gates are identical to the away goal because
            // both goal mouths are centred at WIDTH/2 and capped at GOAL_HEIGHT; the X
            // half-space is what distinguishes them and the caller already verified it.
            if (lowEnough && x < -r)
            {
                if (IsBetweenPostsUnderCrossbar(ball.Position))
                    return (true, RestartType.KickOff);
                return (true, lastTouchTeamID == 0 ? RestartType.Corner : RestartType.GoalKick);
            }

            // Away goal (x &gt; LENGTH + r): see comment on home-goal branch above.
            if (lowEnough && x > BallPhysicsConstants.Pitch.LENGTH + r)
            {
                if (IsBetweenPostsUnderCrossbar(ball.Position))
                    return (true, RestartType.KickOff);
                return (true, lastTouchTeamID == 1 ? RestartType.Corner : RestartType.GoalKick);
            }

            return (false, RestartType.None);
        }

        private static bool IsBetweenPostsUnderCrossbar(Vector3 position)
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
        /// Transitions ball to Controlled state. Called by agent system after CheckPossession.
        /// Does NOT record which agent has possession (Option B — agent system owns that).
        /// </summary>
        public static void SetBallControlled(ref BallState ball)
        {
            ball.State           = BallStateType.Controlled;
            ball.Velocity        = Vector3.zero;
            ball.AngularVelocity = Vector3.zero;
        }

        /// <summary>
        /// Applies a kick impulse to the ball, releasing it from Controlled.
        /// POSSESSION MODEL (Option B): transitions BallState out of Controlled as the signal
        /// the agent system polls to clear its possession record.
        /// Returns <see cref="KickResult.RejectedNonFiniteVelocity"/> without mutating ball
        /// state when the caller supplies NaN/Infinity velocity — caller MUST inspect the
        /// return value and either retry with a sanitized vector or abort the kick.
        /// </summary>
        public static KickResult ApplyKick(
            ref BallState ball,
            Vector3 velocity,
            Vector3 spin,
            int agentId,
            float matchTime,
            BallEventLogger logger = null)
        {
            if (!IsFiniteVector(velocity))
            {
                Debug.LogError(
                    $"[BallPhysics] ApplyKick: Invalid velocity {velocity} from agent {agentId}. Kick rejected.");
                return KickResult.RejectedNonFiniteVelocity;
            }

            if (!IsFiniteVector(spin))
            {
                Debug.LogWarning(
                    $"[BallPhysics] ApplyKick: Invalid spin {spin} from agent {agentId}. Spin zeroed.");
                spin = Vector3.zero;
            }

            if (velocity.magnitude > BallPhysicsConstants.Limits.MaxVelocity)
            {
                velocity = velocity.normalized * BallPhysicsConstants.Limits.MaxVelocity;
                Debug.LogWarning(
                    $"[BallPhysics] ApplyKick: Velocity clamped to {BallPhysicsConstants.Limits.MaxVelocity} m/s.");
            }

            if (spin.magnitude > BallPhysicsConstants.Limits.MaxSpin)
                spin = spin.normalized * BallPhysicsConstants.Limits.MaxSpin;

            ball.Velocity        = velocity;
            ball.AngularVelocity = spin;

            float horizontalSpeed = new Vector2(velocity.x, velocity.y).magnitude;

            if (velocity.z > 0f)
                ball.State = BallStateType.Airborne;
            else if (horizontalSpeed > BallPhysicsConstants.State.MinVelocity)
                ball.State = BallStateType.Rolling;
            else
                ball.State = BallStateType.Stationary;

            ball.LastValidPosition = ball.Position;
            ball.LastValidVelocity = ball.Velocity;

            logger?.LogKick(ball, agentId, ball.State, matchTime);

            return KickResult.Applied;
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
// | 1.2     | 2026-06-02 | —      | AR-1 fixes. H-2: file header path corrected. M-1: using order      |
// |         |            |        | System → UnityEngine. M-2: _coefficients → s_coefficients. M-3:    |
// |         |            |        | dead isHomeGoal parameter removed. M-4: enum members PascalCase.   |
// |         |            |        | M-5: ApplyKick → KickResult. L-2: corner-precedence doc. L-4:      |
// |         |            |        | BodyPartCoefficients.Get throws on unknown enum values.            |
// | 1.3     | 2026-06-03 | —      | AR-2 fixes. L-1: IsInHomeGoal / IsInAwayGoal wrappers folded —    |
// |         |            |        | CheckBoundaries now calls IsBetweenPostsUnderCrossbar directly     |
// |         |            |        | with inline home-goal / away-goal comments; the two zero-info      |
// |         |            |        | wrappers are gone. L-2: BodyPart, RestartType, KickResult, and     |
// |         |            |        | BodyPartCoefficients extracted to their own files per src/CLAUDE.md|
// |         |            |        | FILE NAMING. Unused System.Collections.Generic using removed and   |
// |         |            |        | the UnityEngine. prefix dropped from Debug / Vector2 (already in   |
// |         |            |        | scope via the single UnityEngine using).                           |
#endregion
