// File:     src/match-engine/RestartResolver.cs
// Created:  2026-07-14
// Modified: 2026-07-16 (AR-7 L-1 — lastTouchTeam param doc aligned to what the caller actually passes: the last settled holder, not the last physical toucher)
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-flow-completion-design.md) §5, Code Standards #20
// Purpose:  Pure restart-position / awarded-team resolution for the three non-goal RestartType
//           values BallCollision.CheckBoundaries already classifies (ThrowIn/Corner/GoalKick).
//           KickOff (a goal) is handled separately by MatchEngine.CheckRestartAndApply's existing
//           centre-spot path — this helper is never called with RestartType.KickOff or None.

using System;

using UnityEngine;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Resolves the ball placement and awarded team for a throw-in, corner, or goal kick. Pure and
    /// allocation-free — mirrors the <see cref="BallCollision"/> static-helper style.
    /// </summary>
    public static class RestartResolver
    {
        /// <summary>
        /// Resolves <paramref name="type"/> (must be ThrowIn, Corner, or GoalKick) against the ball's
        /// exit position and the team that touched it last. For all three types the awarded team is
        /// uniformly "whoever did NOT touch it last" — verified against
        /// <see cref="BallCollision.CheckBoundaries"/>'s actual branches (design note §5 AR-1): a
        /// Corner and a GoalKick both reduce to that rule, identical to a ThrowIn. Only the ball
        /// placement differs by type.
        /// </summary>
        /// <param name="type">The non-goal restart type (ThrowIn/Corner/GoalKick).</param>
        /// <param name="ballPosition">The ball's position at the moment it left the field of play.</param>
        /// <param name="lastTouchTeam">Team id (0/1) charged with the last touch. AR-7 L-1 — this is
        /// the CALLER's best available approximation, and the production caller
        /// (<c>MatchEngine.CheckRestartAndApply</c>) passes the last settled possession HOLDER, not
        /// the last physical toucher (deflections and uncontrolled touches never update that
        /// tracker; before any possession has settled, team 0 is assumed) — a documented Stage-0
        /// approximation recorded at the call seam.</param>
        /// <returns>The restart position (pitch-plane, m) and the awarded team id (0/1).</returns>
        public static (Vector2 position, int awardedTeam) Resolve(
            RestartType type, Vector2 ballPosition, int lastTouchTeam)
        {
            int awardedTeam = 1 - lastTouchTeam;

            Vector2 position;
            switch (type)
            {
                case RestartType.ThrowIn:
                    position = ResolveThrowIn(ballPosition);
                    break;
                case RestartType.Corner:
                    position = ResolveCorner(ballPosition);
                    break;
                case RestartType.GoalKick:
                    position = ResolveGoalKick(ballPosition);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(type), type,
                        "RestartResolver.Resolve handles only ThrowIn/Corner/GoalKick — KickOff " +
                        "(a goal) and None are the caller's responsibility (design note §5).");
            }

            return (position, awardedTeam);
        }

        /// <summary>Throw-in: the exit point, X clamped onto the pitch, Y snapped to the nearer touchline.</summary>
        private static Vector2 ResolveThrowIn(Vector2 ballPosition)
        {
            float x = Mathf.Clamp(ballPosition.x, 0f, MatchEngineConstants.PITCH_LENGTH_M);
            float y = ballPosition.y < MatchEngineConstants.PITCH_WIDTH_M * 0.5f
                ? 0f
                : MatchEngineConstants.PITCH_WIDTH_M;
            return new Vector2(x, y);
        }

        /// <summary>Corner: the corner point nearest the ball's Y, inset by the ball radius so the
        /// restart position stays strictly in bounds.</summary>
        private static Vector2 ResolveCorner(Vector2 ballPosition)
        {
            float r = BallPhysicsConstants.Ball.RADIUS;
            float x = ballPosition.x < MatchEngineConstants.PITCH_LENGTH_M * 0.5f
                ? r
                : MatchEngineConstants.PITCH_LENGTH_M - r;
            float y = ballPosition.y < MatchEngineConstants.PITCH_WIDTH_M * 0.5f
                ? r
                : MatchEngineConstants.PITCH_WIDTH_M - r;
            return new Vector2(x, y);
        }

        /// <summary>Goal kick: the centre of the six-yard box on the exited goal line.</summary>
        private static Vector2 ResolveGoalKick(Vector2 ballPosition)
        {
            float x = ballPosition.x < MatchEngineConstants.PITCH_LENGTH_M * 0.5f
                ? MatchEngineConstants.GOAL_AREA_DEPTH_M
                : MatchEngineConstants.PITCH_LENGTH_M - MatchEngineConstants.GOAL_AREA_DEPTH_M;
            float y = MatchEngineConstants.PITCH_WIDTH_M * 0.5f;
            return new Vector2(x, y);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-14 | —      | Initial implementation.                                        |
// | 1.1     | 2026-07-16 | —      | AR-7 L-1 (doc-only): the lastTouchTeam param doc claimed "the  |
// |         |            |        | agent who touched the ball last" while the production caller   |
// |         |            |        | passes the last settled HOLDER (deflections never update it;   |
// |         |            |        | −1 ⇒ team 0) — doc now states the approximation explicitly.    |
#endregion
