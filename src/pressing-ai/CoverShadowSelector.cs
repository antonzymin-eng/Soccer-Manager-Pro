// File:     src/pressing-ai/CoverShadowSelector.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Pressing AI #13 §3.4–§3.5, Code Standards #20
// Purpose:  Pure static class: scores candidate receivers by threat, assigns own-team
//           defenders greedily to shadow lanes up to MaxCoverShadows.

using System;

using UnityEngine;

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Selects up to <see cref="PressingAIConstants.MaxCoverShadows"/> cover-shadow
    /// assignments per tick. Receivers are ranked by threat score (§3.4); defenders
    /// are assigned greedily by minimum cover-cost (§3.5).
    /// Pressing AI #13 §3.4–§3.5.
    /// </summary>
    public static class CoverShadowSelector
    {
        // ── Scratch constants ────────────────────────────────────────────────
        // MaxCoverShadows == 2 is a compile-time constant, so local stack arrays
        // sized to SQUAD_SIZE are sufficient for the candidate loop.
        private const int MaxCandidates = PressingAIConstants.SQUAD_SIZE;

        /// <summary>
        /// Selects cover-shadow assignments and writes them into <paramref name="out0"/>
        /// and <paramref name="out1"/>. Returns the number of valid assignments [0, 2].
        /// </summary>
        /// <param name="snapshot">Current tick snapshot.</param>
        /// <param name="primaryPresserId">EntityId of the already-selected primary presser (-1 = none).</param>
        /// <param name="out0">First shadow assignment (valid when return value ≥ 1).</param>
        /// <param name="out1">Second shadow assignment (valid when return value == 2).</param>
        /// <returns>Number of cover-shadow assignments produced [0, MaxCoverShadows].</returns>
        public static int Select(
            PressingSnapshot snapshot,
            int primaryPresserId,
            out CoverShadow out0,
            out CoverShadow out1)
        {
            out0 = default;
            out1 = default;

            int carrierId      = snapshot.BallCarrierEntityId;
            int pressingTeamId = snapshot.PressingTeamId;

            // Need a carrier to have candidates.
            if (carrierId < 0)
                return 0;

            // Locate carrier position.
            Vector2 carrierPos = Vector2.zero;
            bool carrierFound  = false;
            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot a = ref snapshot.Agents[i];
                if (a.EntityId == carrierId)
                {
                    if (!a.IsActive)
                        break;
                    carrierPos   = a.Position;
                    carrierFound = true;
                    break;
                }
            }

            if (!carrierFound)
                return 0;

            float radiusSq    = PressingAIConstants.CoverShadowCandidateRadiusM
                              * PressingAIConstants.CoverShadowCandidateRadiusM;
            float halfPitchX  = PressingAIConstants.PITCH_HALF_LENGTH_M;

            // ── Rank receivers ───────────────────────────────────────────────
            // Stack-allocated scratch: receiver entity IDs and their threat scores.
            // MaxCandidates == 22, well within safe stackalloc budget.
            Span<int>   receiverIds    = stackalloc int[MaxCandidates];
            Span<float> threatScores   = stackalloc float[MaxCandidates];
            int         receiverCount  = 0;

            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot r = ref snapshot.Agents[i];

                // Opponents only, not carrier, not GK, not inactive.
                if (r.TeamId == pressingTeamId)
                    continue;
                if (!r.IsActive)
                    continue;
                if (r.EntityId == carrierId || r.IsGoalkeeper)
                    continue;

                // Within candidate radius of ball-carrier.
                float dx = r.Position.x - carrierPos.x;
                float dy = r.Position.y - carrierPos.y;
                if (dx * dx + dy * dy > radiusSq)
                    continue;

                float score = ComputeThreatScore(
                    r.Position, r.FirstTouchAttribute,
                    carrierPos, snapshot, pressingTeamId, halfPitchX, radiusSq);

                // Insertion-sort descending by score (small list — at most 21 entries).
                int insertAt = receiverCount;
                while (insertAt > 0 && threatScores[insertAt - 1] < score)
                    insertAt--;

                // Shift right.
                for (int k = receiverCount; k > insertAt; k--)
                {
                    receiverIds[k]  = receiverIds[k - 1];
                    threatScores[k] = threatScores[k - 1];
                }

                receiverIds[insertAt]  = r.EntityId;
                threatScores[insertAt] = score;
                receiverCount++;
            }

            if (receiverCount == 0)
                return 0;

            // ── Track assigned defenders ────────────────────────────────────
            // Two explicit slots (MaxCoverShadows == 2).
            int assignedDef0 = -1;
            int assignedDef1 = -1;
            int assigned     = 0;

            for (int ri = 0; ri < receiverCount && assigned < PressingAIConstants.MaxCoverShadows; ri++)
            {
                int receiverId = receiverIds[ri];

                // Compute shadow-lane position.
                Vector2 receiverPos = GetAgentPosition(snapshot, receiverId);
                Vector2 shadowLane  = Vector2.Lerp(carrierPos, receiverPos,
                    PressingAIConstants.CoverShadowLaneFraction);

                // Find best eligible defender (min coverCost, EntityId ascending tie-break).
                int   bestDefId   = -1;
                float bestCoverSq = float.MaxValue;

                for (int i = 0; i < snapshot.Agents.Length; i++)
                {
                    ref readonly PressingAgentSnapshot d = ref snapshot.Agents[i];

                    // Own-team, not GK, not primary presser, not inactive.
                    if (d.TeamId != pressingTeamId)
                        continue;
                    if (!d.IsActive)
                        continue;
                    if (d.IsGoalkeeper)
                        continue;
                    if (d.EntityId == primaryPresserId)
                        continue;

                    // Not already assigned.
                    if (d.EntityId == assignedDef0 || d.EntityId == assignedDef1)
                        continue;

                    // Fatigue eligibility.
                    if (d.Fatigue >= PressingAIConstants.PressFatigueCeiling)
                        continue;

                    float ddx    = d.Position.x - shadowLane.x;
                    float ddy    = d.Position.y - shadowLane.y;
                    float coverSq = ddx * ddx + ddy * ddy;

                    if (bestDefId < 0
                        || coverSq < bestCoverSq - PressingAIConstants.SpacingEpsilonM2
                        || (coverSq - bestCoverSq < PressingAIConstants.SpacingEpsilonM2
                            && d.EntityId < bestDefId))
                    {
                        bestDefId   = d.EntityId;
                        bestCoverSq = coverSq;
                    }
                }

                if (bestDefId < 0)
                    continue;

                CoverShadow shadow = new CoverShadow(bestDefId, receiverId, shadowLane);

                if (assigned == 0)
                {
                    out0         = shadow;
                    assignedDef0 = bestDefId;
                }
                else
                {
                    out1         = shadow;
                    assignedDef1 = bestDefId;
                }

                assigned++;
            }

            return assigned;
        }

        // ── Private helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Computes the threat score for receiver <paramref name="rPos"/> per §3.4.
        /// threatScore = progressionGain * ThreatProgressionW
        ///             + (1 − geometricPressure) * ThreatOpenW
        ///             + (firstTouch / 20) * ThreatSkillW
        /// </summary>
        private static float ComputeThreatScore(
            Vector2 rPos,
            float firstTouchAttribute,
            Vector2 carrierPos,
            PressingSnapshot snapshot,
            int pressingTeamId,
            float halfPitchX,
            float radiusSq)
        {
            // Progression gain: how much closer to the POSSESSING team's goal the receiver is
            // vs the carrier. AR-3 H (ERR-013-010): a receiver is more threatening the further
            // it is advanced along the ball-carrier's attack, which is the opposite of the
            // pressing team's. snapshot.AttackingDirection is the pressing team's (its XML doc),
            // so negate it to measure progression in the possessing team's frame — otherwise the
            // score rewards receivers retreating toward their own goal (home/away inversion).
            Vector2 toReceiver = rPos - carrierPos;
            float dot = toReceiver.x * -snapshot.AttackingDirection.x
                      + toReceiver.y * -snapshot.AttackingDirection.y;
            float progressionGain = dot / halfPitchX;
            if (progressionGain < 0f) progressionGain = 0f;
            if (progressionGain > 1f) progressionGain = 1f;

            // Geometric pressure on the receiver.
            float pressure = TriggerEvaluator.ComputeGeometricPressure(
                rPos, snapshot, pressingTeamId, radiusSq);

            // Skill term.
            float skillTerm = firstTouchAttribute / PressingAIConstants.ATTRIBUTE_SCALE_MAX;

            return progressionGain * PressingAIConstants.ThreatProgressionW
                 + (1f - pressure) * PressingAIConstants.ThreatOpenW
                 + skillTerm       * PressingAIConstants.ThreatSkillW;
        }

        /// <summary>Returns the (X, Y) position of the agent with the given EntityId.</summary>
        private static Vector2 GetAgentPosition(PressingSnapshot snapshot, int entityId)
        {
            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot a = ref snapshot.Agents[i];
                if (a.EntityId == entityId)
                    return a.Position;
            }

            return Vector2.zero;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-05-29 | —      | AR-1 H-1: added IsActive guards in carrier search, receiver loop, and defender loop. |
// | 1.2     | 2026-06-12 | —      | Build fix (dotnet CI gate): missing 'using System;' - Span<T> (stackalloc scratch,   |
// |         |            |        | lines ~77-78) is System namespace, so the file was CS0246 under Unity and the Linux  |
// |         |            |        | gate alike. Using added per FR-CS-006 ordering; no functional change.                |
// | 1.3     | 2026-06-15 | —      | AR-3 H (ERR-013-010): threat progressionGain measured in the possessing team's frame |
// |         |            |        | (negated AttackingDirection) so advanced receivers — not retreating ones — rank highest.|
#endregion
