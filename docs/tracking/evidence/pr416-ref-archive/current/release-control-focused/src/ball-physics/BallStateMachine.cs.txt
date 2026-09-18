// File:     src/ball-physics/BallStateMachine.cs
// Created:  2026-05-24
// Modified: 2026-07-27 (shot-outcome pass)
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Pure state-transition logic for the ball state machine.
//           No physics calculations; only reads BallState fields.

using UnityEngine;

namespace TacticalDirector.BallPhysics
{
    /// <summary>
    /// State transition logic for the ball state machine.
    /// Pure state logic — no physics calculations.
    /// </summary>
    public static class BallStateMachine
    {
        /// <summary>
        /// Updates ball state based on current physics conditions.
        /// Uses hysteresis to prevent rapid oscillation at boundaries.
        /// </summary>
        public static BallStateType UpdateBallState(BallState ball)
        {
            switch (ball.State)
            {
                case BallStateType.Stationary:
                    // Transitions handled externally by kick/touch events.
                    return BallStateType.Stationary;

                case BallStateType.Rolling:
                    if (ball.Velocity.magnitude < BallPhysicsConstants.State.MinVelocity)
                        return BallStateType.Stationary;
                    if (ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
                        return BallStateType.Airborne;
                    if (IsOutOfBounds(ball.Position))
                        return BallStateType.OutOfPlay;
                    return BallStateType.Rolling;

                case BallStateType.Airborne:
                    if (ball.Position.z <= BallPhysicsConstants.State.AirborneExitThreshold &&
                        ball.Velocity.z < 0f)
                        return BallStateType.Bouncing;
                    if (IsOutOfBounds(ball.Position))
                        return BallStateType.OutOfPlay;
                    return BallStateType.Airborne;

                case BallStateType.Bouncing:
                    if (Mathf.Abs(ball.Velocity.z) < BallPhysicsConstants.State.BounceVelocityThreshold)
                        return BallStateType.Rolling;
                    return BallStateType.Airborne;

                case BallStateType.Controlled:
                    // Transitions handled externally by the agent system.
                    return BallStateType.Controlled;

                case BallStateType.OutOfPlay:
                    // Transitions handled externally by the restart system.
                    return BallStateType.OutOfPlay;

                default:
                    return BallStateType.Stationary;
            }
        }

        /// <summary>
        /// Returns true if the ball has entirely crossed a pitch boundary line — on the ground or
        /// in the air (Law 9; ERR-001-004 removed the former z &lt; Ball.Diameter gate, under which
        /// an airborne crossing was neither out of play here nor classified by
        /// BallCollision.CheckBoundaries). Mirrors CheckBoundaries so the state machine and the
        /// restart classifier agree on what "out" means; the goal/over-bar split is
        /// CheckBoundaries' alone.
        /// </summary>
        public static bool IsOutOfBounds(Vector3 position)
        {
            float r = BallPhysicsConstants.Ball.RADIUS;
            return position.x < -r
                || position.x > BallPhysicsConstants.Pitch.LENGTH + r
                || position.y < -r
                || position.y > BallPhysicsConstants.Pitch.WIDTH + r;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-24 | —      | Initial implementation.                                            |
// | 1.1     | 2026-05-24 | —      | Fix pass: namespace → TacticalDirector.BallPhysics; ALL_CAPS       |
// |         |            |        | constant refs → PascalCase; file header per FR-CS-056/057.         |
// | 1.2     | 2026-06-02 | —      | AR-1 fixes. H-2: file header path corrected to src/ball-physics/.  |
// |         |            |        | H-3: IsOutOfBounds now applies the same z &lt; Ball.Diameter gate as |
// |         |            |        | BallCollision.CheckBoundaries — a high-flying ball is no longer    |
// |         |            |        | silently transitioned to OutOfPlay while CheckBoundaries returns   |
// |         |            |        | (false, None); the two definitions of "out" now agree. M-4:        |
// |         |            |        | BallStateType members renamed to PascalCase.                       |
// | 1.3     | 2026-07-27 | —      | ERR-001-004 (shot-outcome design KD-5): IsOutOfBounds drops the   |
// |         |            |        | z < Diameter gate in the same commit as CheckBoundaries — the two |
// |         |            |        | predicates are pinned to agree, and an airborne crossing is out.  |
#endregion
