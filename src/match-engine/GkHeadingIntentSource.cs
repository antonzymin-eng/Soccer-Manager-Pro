// File:     src/match-engine/GkHeadingIntentSource.cs
// Created:  2026-07-22
// Modified: 2026-08-04 (wiring backlog W1: + RushArmed / TrySolveRushIntercept — the keeper rush trigger geometry. See docs/tracking/gk-rush-trigger-design.md)
// Author:   —
// Spec:     GK/Heading engine-integration design supplement
//           (docs/tracking/gk-heading-engine-integration-design.md) §4; Code Standards #20
// Purpose:  Pure Stage-0 world-state trigger predicates for the GK (#11) / Heading (#10) wiring —
//           the "when does a save/header happen" heuristic, extracted out of MatchEngine so it is a
//           pure, unit-testable function (the MatchFlowCollisionConsumer heuristic-foul precedent).
//           The engine owns the per-episode latch, the ToGoalkeeper/ToHeading projection, and the
//           orchestrator commit; this class owns only the geometry decision.

using UnityEngine;

using TacticalDirector.AgentMovement;

namespace TacticalDirector.MatchEngine
{
    /// <summary>Pure Stage-0 GK/Heading trigger heuristics (§4). Stateless — every method is a pure
    /// function of the world snapshot it is handed, so it is unit-testable without a booted engine, and
    /// the engine's commit sites read only as producers of an intent decision (KD-8 live consumer).</summary>
    internal static class GkHeadingIntentSource
    {
        /// <summary>§4.1: true when a loose ball is driving fast enough toward the goal team
        /// <paramref name="keeperTeam"/> defends, within save range of the goal line. Team 0 defends
        /// x = 0; team 1 defends x = PITCH_LENGTH_M. Pure geometry — the caller owns the possession /
        /// per-episode latch.</summary>
        public static bool SaveArmed(int keeperTeam, in Vector3 ballPosition, in Vector3 ballVelocity, bool ballLoose)
        {
            if (!ballLoose)
            {
                return false;
            }

            float goalX = keeperTeam == 0 ? 0f : MatchEngineConstants.PITCH_LENGTH_M;
            float distToGoalLine = Mathf.Abs(ballPosition.x - goalX);
            float towardGoal = (goalX - ballPosition.x) * ballVelocity.x;   // > 0 ⇒ moving toward the goal line
            float speed = new Vector2(ballVelocity.x, ballVelocity.y).magnitude;

            return distToGoalLine <= MatchEngineConstants.GkSaveTriggerRangeM
                   && towardGoal > 0f
                   && speed >= MatchEngineConstants.GkSaveTriggerMinBallSpeedMps;
        }

        /// <summary>
        /// §4.4 (gk-rush-trigger-design.md): true when this keeper should leave his line, and the
        /// world-space point he should be committed to. Pure geometry — the caller owns the latch, the
        /// projection, the save-priority exclusion and the commit.
        ///
        /// <para>The football judgement is <b>condition 3</b>: is anyone already between the ball and
        /// the goal? A keeper comes out to <b>reduce the shooting angle</b>, and the only thing that
        /// makes that unnecessary is a team-mate who is <i>goal-side</i> — already in the shot's path.
        /// A defender chasing the carrier down, or trying to muscle him off the ball, is emphatically
        /// <b>not</b> a reason to stay: the carrier still has a clear sight of goal, and closing the
        /// angle is the keeper's job whether or not someone is snapping at the man's heels.</para>
        ///
        /// <para>For a loose ball the target is the solved meeting point, not the ball's current
        /// position. The target is LOCKED at commit (KD-15 / FR-GK-018), so aiming at where the ball is
        /// now sends the keeper to where it was a second ago. The intercept solve doubles as the range
        /// guard: a ball moving away faster than the keeper runs has no positive root, so a clearance
        /// cannot drag him out of his goal.</para>
        /// </summary>
        /// <param name="keeperTeam">Keeper index == team id (KD-1). Team 0 defends x = 0.</param>
        /// <param name="gkPosition">Keeper's current world position.</param>
        /// <param name="ballPosition">Current ball position.</param>
        /// <param name="ballVelocity">Current ball velocity.</param>
        /// <param name="ballLoose">True when no agent possesses the ball.</param>
        /// <param name="ballHeldByKeeperTeam">True when the possessor is on the keeper's own team.</param>
        /// <param name="hasGoalSideCover">True when a team-mate of the keeper is already between the ball
        /// and the goal, inside the shot corridor — see <see cref="HasGoalSideCover"/>.</param>
        /// <param name="rushCommitDistanceM">How far from his own goal THIS keeper comes out, from
        /// <c>GoalkeeperRushDispatch.ComputeRushCommitDistanceM</c> (§3.7.0). The trigger's "when" is the
        /// keeper's own attributes; this predicate only applies it geometrically.</param>
        /// <param name="rushSpeedMps">The keeper's rush speed from <c>ComputeRushLaunchMps</c> — the solve
        /// must race at the speed the keeper will actually run at, not a nominal one.</param>
        /// <param name="rushTarget">The point to lock into the <c>RushIntent</c>. Written on every path;
        /// meaningful only when the method returns true.</param>
        public static bool RushArmed(
            int keeperTeam,
            in Vector3 gkPosition,
            in Vector3 ballPosition,
            in Vector3 ballVelocity,
            bool ballLoose,
            bool ballHeldByKeeperTeam,
            bool hasGoalSideCover,
            float rushCommitDistanceM,
            float rushSpeedMps,
            out Vector3 rushTarget)
        {
            rushTarget = new Vector3(ballPosition.x, ballPosition.y, 0f);

            // 1. Our own player has it. Nothing to come for.
            if (!ballLoose && ballHeldByKeeperTeam)
            {
                return false;
            }

            // 2. A ball above claim height is a cross to be caught (backlog W3 — the contested
            //    multi-agent claim is not wired), not a ball to be swept. Running at it commits the
            //    keeper to a duel the engine cannot yet resolve.
            if (ballPosition.z > MatchEngineConstants.GkRushMaxBallHeightM)
            {
                return false;
            }

            // 3. Somebody is already in the shot's path. A goal-side team-mate is narrowing the angle
            //    the keeper would be coming out to narrow, and two bodies converging on the same line
            //    is how a keeper gets rounded. A CHASING defender is not this: the carrier still has a
            //    clear sight of goal, and the keeper comes anyway.
            if (hasGoalSideCover)
            {
                return false;
            }

            // 4. Loose ball: it is a race, and the solve decides whether it is winnable.
            if (ballLoose)
            {
                if (!TrySolveRushIntercept(
                        in gkPosition, in ballPosition, in ballVelocity, rushSpeedMps,
                        out _, out Vector3 meetingPoint))
                {
                    return false;
                }
                rushTarget = meetingPoint;
            }

            // 5. One rule for how long a run the keeper will commit to, applied to BOTH branches. For
            //    the loose ball this is exactly the intercept cap — the solve puts the meeting point at
            //    rushSpeed × t by construction, so distance ≤ rushSpeed × cap ⟺ t ≤ cap — and for a
            //    carrier it is the same budget expressed the only way it can be, since there is no race
            //    to solve. Without it the possessed branch would let a keeper on his line commit to a
            //    22 m sprint with no time bound at all.
            float runDx = rushTarget.x - gkPosition.x;
            float runDy = rushTarget.y - gkPosition.y;
            float runBudgetM = rushSpeedMps * MatchEngineConstants.GkRushMaxInterceptS;
            if (runDx * runDx + runDy * runDy > runBudgetM * runBudgetM)
            {
                return false;
            }

            // 6. THE trigger condition: is the ball inside the distance from his own goal at which THIS
            //    keeper comes out? That distance is §3.7.0's attribute-driven answer — an aggressive,
            //    composed sweeper-keeper commits from the edge of the area, a timid or spent one barely
            //    leaves his line. Applied to the TARGET rather than the ball, so a race solved to a
            //    meeting point out by the halfway line is refused even when the ball is currently close.
            float goalX = keeperTeam == 0 ? 0f : MatchEngineConstants.PITCH_LENGTH_M;
            return Mathf.Abs(rushTarget.x - goalX) <= rushCommitDistanceM;
        }

        /// <summary>
        /// §4.4: true when a team-mate of keeper <paramref name="keeperTeam"/> is <b>goal-side</b> of the
        /// ball and inside the shot corridor — already standing in the path the keeper would be coming
        /// out to block.
        ///
        /// <para>Two conditions, and the first is the one that matters. <b>Goal-side</b> means nearer the
        /// defended goal line than the ball is, by <c>GkRushCoverGoalSideMarginM</c>. A defender level
        /// with the carrier, or chasing him from behind, is therefore NOT cover — which is the point: a
        /// recovering defender does not narrow the shooting angle, so the keeper still comes. Only a body
        /// that is genuinely in front of the ball does.</para>
        ///
        /// <para><b>Inside the corridor</b> means within <c>GkRushCoverCorridorHalfWidthM</c> of the
        /// segment from the ball to the centre of the defended goal — the line the shot would take. A
        /// full-back stranded on the far touchline is goal-side of a ball in the centre and blocks
        /// nothing; measuring perpendicular distance to the shot line rather than a plain Y band keeps
        /// that honest for a ball out wide, where the two differ most.</para>
        ///
        /// <para>Goalkeepers and sent-off agents are excluded — the keeper is not his own cover, and a
        /// frozen red-carded agent blocks nothing (the participation rule every scan in this engine
        /// carries).</para>
        /// </summary>
        public static bool HasGoalSideCover(
            int keeperTeam,
            in Vector3 ballPosition,
            AgentState[] agents, int[] teamIds, bool[] isGoalkeeper, bool[] isSentOff, int count)
        {
            float goalX = keeperTeam == 0 ? 0f : MatchEngineConstants.PITCH_LENGTH_M;
            float goalY = MatchEngineConstants.PITCH_WIDTH_M * 0.5f;

            float ballToGoalM = Mathf.Abs(ballPosition.x - goalX);

            // The shot line: ball → goal centre. Degenerate when the ball is on the goal line itself,
            // in which case nobody can be goal-side of it by a margin anyway.
            float lineDx = goalX - ballPosition.x;
            float lineDy = goalY - ballPosition.y;
            float lineLenSq = lineDx * lineDx + lineDy * lineDy;
            if (lineLenSq < MatchEngineConstants.GK_RUSH_SOLVE_EPSILON)
            {
                return false;
            }
            float lineLen = Mathf.Sqrt(lineLenSq);

            float corridor = MatchEngineConstants.GkRushCoverCorridorHalfWidthM;

            for (int i = 0; i < count; i++)
            {
                if (teamIds[i] != keeperTeam || isGoalkeeper[i] || isSentOff[i])
                {
                    continue;
                }

                Vector2 p = agents[i].Position;

                // Goal-side by a margin — strictly in FRONT of the ball, not level with it.
                if (Mathf.Abs(p.x - goalX) > ballToGoalM - MatchEngineConstants.GkRushCoverGoalSideMarginM)
                {
                    continue;
                }

                // Perpendicular distance to the ball → goal-centre line (the 2-D cross product over the
                // segment length). The goal-side test above already bounds the along-line position, so
                // the infinite-line distance is the right measure here.
                float relX = p.x - ballPosition.x;
                float relY = p.y - ballPosition.y;
                float perpM = Mathf.Abs(relX * lineDy - relY * lineDx) / lineLen;

                if (perpM <= corridor)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Solves the keeper-vs-ball intercept in the XY plane: the earliest time t ≥ 0 at which a
        /// keeper running from <paramref name="gkPosition"/> at <paramref name="rushSpeedMps"/> can
        /// reach the ball's straight-line extrapolation. Returns false when no such time exists (the
        /// ball outruns the keeper), which is the guard that keeps a keeper from chasing a clearance.
        ///
        /// <para>|b + v·t − g|² = (s·t)² expands to (v·v − s²)t² + 2(b−g)·v·t + (b−g)·(b−g) = 0. With
        /// s &gt; |v| the quadratic coefficient is negative and the constant term non-negative, so the
        /// roots straddle zero and exactly one is positive — a faster keeper always has a solution.
        /// Straight-line extrapolation only: no drag, no bounce. The
        /// <see cref="MatchEngineConstants.GkRushMaxInterceptS"/> cap at the call site is what keeps
        /// that approximation inside the regime where it is credible, the same bound
        /// <c>DivePredictionHorizonS</c> puts on the §3.3.4 dive prediction.</para>
        /// </summary>
        public static bool TrySolveRushIntercept(
            in Vector3 gkPosition,
            in Vector3 ballPosition,
            in Vector3 ballVelocity,
            float rushSpeedMps,
            out float interceptS,
            out Vector3 meetingPoint)
        {
            interceptS = 0f;
            meetingPoint = new Vector3(ballPosition.x, ballPosition.y, 0f);

            if (rushSpeedMps <= 0f)
            {
                return false;
            }

            float rx = ballPosition.x - gkPosition.x;
            float ry = ballPosition.y - gkPosition.y;
            float vx = ballVelocity.x;
            float vy = ballVelocity.y;

            float a = vx * vx + vy * vy - rushSpeedMps * rushSpeedMps;
            float b = 2f * (rx * vx + ry * vy);
            float c = rx * rx + ry * ry;

            float t;
            if (Mathf.Abs(a) < MatchEngineConstants.GK_RUSH_SOLVE_EPSILON)
            {
                // Ball speed equals rush speed: the quadratic degenerates to b·t + c = 0.
                if (Mathf.Abs(b) < MatchEngineConstants.GK_RUSH_SOLVE_EPSILON)
                {
                    return false;
                }
                t = -c / b;
            }
            else
            {
                float disc = b * b - 4f * a * c;
                if (disc < 0f)
                {
                    return false;
                }
                float root = Mathf.Sqrt(disc);
                float t1 = (-b - root) / (2f * a);
                float t2 = (-b + root) / (2f * a);

                // Earliest non-negative root. Both may be negative (ball receding, keeper slower),
                // which is the no-solution case.
                float lo = Mathf.Min(t1, t2);
                float hi = Mathf.Max(t1, t2);
                t = lo >= 0f ? lo : hi;
            }

            if (t < 0f)
            {
                return false;
            }

            interceptS = t;
            meetingPoint = new Vector3(ballPosition.x + vx * t, ballPosition.y + vy * t, 0f);
            return true;
        }

        /// <summary>§4.2: the single nearest active outfield agent within head range of a loose airborne
        /// ball, or −1 when none qualifies. Deterministic tie-break: the LATER index wins (the <c>&lt;=</c>
        /// compare), matching the engine's original scan. Pure — the caller owns the per-episode latch and
        /// the airborne/loose gate result passed in as <paramref name="airborneLoose"/>.</summary>
        public static int NearestHeaderCandidate(
            in Vector3 ballPosition, bool airborneLoose,
            AgentState[] agents, bool[] isGoalkeeper, bool[] isSentOff, int count)
        {
            if (!airborneLoose)
            {
                return -1;
            }

            int nearest = -1;
            float nearestSq = MatchEngineConstants.HeaderTriggerRangeM * MatchEngineConstants.HeaderTriggerRangeM;
            for (int i = 0; i < count; i++)
            {
                if (isGoalkeeper[i] || isSentOff[i])
                {
                    continue;
                }
                Vector2 ap = agents[i].Position;
                float dx = ap.x - ballPosition.x;
                float dy = ap.y - ballPosition.y;
                float dSq = dx * dx + dy * dy;
                if (dSq <= nearestSq)
                {
                    nearestSq = dSq;
                    nearest = i;
                }
            }
            return nearest;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-07-22 | —      | Initial — pure §4 save/header trigger heuristics extracted from  |
// |         |            |        | MatchEngine.TryCommitSaveIntents / TryCommitHeaderIntents so the |
// |         |            |        | "when" decision is unit-testable (cleaner-architecture pass).    |
// | 1.1     | 2026-08-04 | —      | Wiring backlog W1: + RushArmed (§4.4) and TrySolveRushIntercept. |
// |         |            |        | The trigger GoalkeeperMechanics.CommitRushIntent never had. The  |
// |         |            |        | football judgement is one condition — no team-mate is nearer the |
// |         |            |        | ball than the keeper — which covers the through-ball and the     |
// |         |            |        | unattended runner alike and fails safe. The intercept solve      |
// |         |            |        | exists because KD-15 locks the target at commit, so aiming at    |
// |         |            |        | the ball's CURRENT position sends the keeper to where it was;    |
// |         |            |        | it also self-guards, since a ball outrunning the keeper has no   |
// |         |            |        | positive root and a clearance therefore cannot pull him out.     |
#endregion
