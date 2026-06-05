// File:     src/collision-system/ContactTypeClassifier.cs
// Created:  2026-05-25
// Modified: 2026-06-05  [v1.1]
// Author:   —
// Spec:     Collision System #3 §3.3.6, FR-06, Code Standards #20
// Purpose:  Velocity-heuristic contact classification for foul detection groundwork.

using UnityEngine;

namespace TacticalDirector.CollisionSystem
{
    /// <summary>
    /// Classifies contact type from velocity geometry. Static and pure.
    /// Stage 0: uses velocity heuristics; Stage 1+: adds facing direction.
    /// Collision System #3 §3.3.6.
    /// </summary>
    public static class ContactTypeClassifier
    {
        /// <summary>
        /// Classifies contact type (shoulder, behind, side) from agent velocities.
        /// </summary>
        /// <param name="instigator">Agent with higher approach velocity.</param>
        /// <param name="victim">Agent who received contact.</param>
        /// <param name="normal">Collision normal (instigator → victim direction).</param>
        public static ContactType Classify(
            in AgentPhysicalProperties instigator,
            in AgentPhysicalProperties victim,
            Vector2 normal)
        {
            float minSpeed = ContactClassificationConstants.MinSpeedForClassification;
            var vi = new Vector2(instigator.Velocity.x, instigator.Velocity.y);
            float speedI = vi.magnitude;

            if (speedI < minSpeed)
            {
                return ContactType.SIDE_IMPACT;
            }

            Vector2 approachDir = vi / speedI;

            var vv = new Vector2(victim.Velocity.x, victim.Velocity.y);
            float speedV = vv.magnitude;

            if (speedV <= minSpeed)
            {
                return ContactType.SIDE_IMPACT;
            }

            // victimDir computed once and shared by both predicates; the behind-test
            // applies a stricter speed gate (MinVictimSpeedBehind > MinSpeedForClassification).
            Vector2 victimDir = vv / speedV;

            float facingDot = Vector2.Dot(approachDir, victimDir);
            if (facingDot > ContactClassificationConstants.ShoulderDotThreshold)
            {
                return ContactType.SHOULDER_TO_SHOULDER;
            }

            if (speedV > ContactClassificationConstants.MinVictimSpeedBehind)
            {
                float behindDot = Vector2.Dot(-normal, victimDir);
                if (behindDot > ContactClassificationConstants.BehindDotThreshold)
                {
                    return ContactType.FROM_BEHIND;
                }
            }

            return ContactType.SIDE_IMPACT;
        }

        /// <summary>
        /// Determines which agent is the instigator (higher approach velocity toward the other).
        /// Ties broken by lower agent index.
        /// </summary>
        /// <param name="a1">First agent.</param>
        /// <param name="a2">Second agent.</param>
        /// <param name="normal">Collision normal pointing from a1 toward a2.</param>
        /// <param name="instigatorIndex">0 = a1 is instigator; 1 = a2 is instigator.</param>
        /// <param name="victimIndex">Opposite of instigatorIndex.</param>
        public static void DetermineInstigatorAndVictim(
            in AgentPhysicalProperties a1,
            in AgentPhysicalProperties a2,
            Vector2 normal,
            out int instigatorIndex,
            out int victimIndex)
        {
            var v1 = new Vector2(a1.Velocity.x, a1.Velocity.y);
            var v2 = new Vector2(a2.Velocity.x, a2.Velocity.y);

            float approach1 = Vector2.Dot(v1,  normal);
            float approach2 = Vector2.Dot(v2, -normal);

            if (approach1 >= approach2)
            {
                instigatorIndex = 0;
                victimIndex = 1;
            }
            else
            {
                instigatorIndex = 1;
                victimIndex = 0;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                          |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                                 |
// | 1.1     | 2026-06-05 | —      | AR-4 L-1. Classify() hoists victimDir = vv / speedV out of both inner branches |
// |         |            |        | (was computed twice with identical inputs). Early-return on speedV <= minSpeed |
// |         |            |        | for symmetry with the speedI check above; behind-branch keeps its stricter     |
// |         |            |        | MinVictimSpeedBehind gate.                                                     |
#endregion
