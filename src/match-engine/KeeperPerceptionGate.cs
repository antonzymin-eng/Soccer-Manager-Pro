// File:     src/match-engine/KeeperPerceptionGate.cs
// Created:  2026-09-11
// Modified: 2026-09-11 (W4 review: exclude non-participating bodies from keeper LOS)
// Author:   —
// Spec:     Match-engine wiring backlog W4; Perception System #7 §3.2
// Purpose:  One pure save-availability predicate shared by the DT SAVE gate and keeper-rush exclusion.

using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.PerceptionSystem;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// W4 keeper-perception gate. Keeps the existing save-flight geometry authoritative while adding
    /// keeper-specific all-body line of sight. It deliberately does not consume FilteredView.BallVisible:
    /// BallVisible also contains FoV/range semantics and can be false for an unobstructed shot while a
    /// keeper's generic facing is stale. The only perception condition introduced here is physical LOS.
    /// </summary>
    internal static class KeeperPerceptionGate
    {
        /// <summary>
        /// Returns true only when the existing save-flight predicate is armed and no participating
        /// agent body — from either team — screens the ball from the keeper. Pure and allocation-free.
        /// </summary>
        public static bool SaveAvailable(
            int keeperTeam,
            int keeperAgentId,
            in Vector2 keeperPosition,
            in Vector3 ballPosition,
            in Vector3 ballVelocity,
            bool ballLoose,
            AgentState[] agents,
            bool[] excludedAgents = null)
        {
            if (!GkHeadingIntentSource.SaveArmed(
                    keeperTeam, in ballPosition, in ballVelocity, ballLoose))
            {
                return false;
            }

            Vector2 ballPosition2D = new Vector2(ballPosition.x, ballPosition.y);
            return !OcclusionFilter.IsOccludedByAnyAgent(
                keeperPosition,
                ballPosition2D,
                keeperAgentId,
                agents,
                excludedAgents);
        }
    }
}
