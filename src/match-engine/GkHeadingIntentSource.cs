// File:     src/match-engine/GkHeadingIntentSource.cs
// Created:  2026-07-22
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
#endregion
