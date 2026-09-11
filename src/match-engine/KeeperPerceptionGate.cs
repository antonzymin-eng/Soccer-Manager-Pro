// File:     src/match-engine/KeeperPerceptionGate.cs
// Created:  2026-09-11
// Modified: 2026-09-11 (W4 review: SAVE emission uses live LOS; rush veto remains raw SaveArmed)
// Author:   —
// Spec:     Match-engine wiring backlog W4; Perception System #7 §3.2
// Purpose:  Pure DT-SAVE availability gate: existing save-flight geometry plus keeper physical LOS.

using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.CollisionSystem;
using TacticalDirector.PerceptionSystem;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// W4 keeper-perception gate for DT SAVE emission. Keeps the existing save-flight geometry
    /// authoritative while adding keeper-specific all-body line of sight from the current-frame world
    /// state. It deliberately does not consume FilteredView.BallVisible: BallVisible also contains
    /// FoV/range semantics and can be false for an unobstructed shot while a keeper's generic facing is
    /// stale. The rush-priority veto must remain on raw GkHeadingIntentSource.SaveArmed geometry so an
    /// unsighted keeper never becomes eligible to rush merely because LOS is blocked.
    /// </summary>
    internal static class KeeperPerceptionGate
    {
        /// <summary>
        /// Returns true only when the existing save-flight predicate is armed and no participating
        /// agent body — from either team — physically screens a body-height ball from the keeper.
        /// Balls above CollisionPhysicsConstants.AgentReachHeight bypass the inherited 2D body-shadow
        /// model rather than being falsely blocked by an infinite-height cylinder. Pure and allocation-free.
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

            // W4 intentionally uses the collision system's Stage-0 reach-height boundary as the
            // available body-height authority. Above it, a ground player's 2D shadow cannot honestly
            // represent a physical screen, so LOS does not disarm an otherwise valid save threat.
            if (ballPosition.z > CollisionPhysicsConstants.AgentReachHeight)
            {
                return true;
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

#region VersionHistory
// | Version | Date       | Author | Notes                                                                     |
// | 1.0     | 2026-09-11 | —      | W4: DT SAVE geometry + live physical LOS; high-ball body-height bypass.   |
#endregion
