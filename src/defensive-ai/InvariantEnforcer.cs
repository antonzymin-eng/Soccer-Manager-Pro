// File:     src/defensive-ai/InvariantEnforcer.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Defensive AI #14 §3.10, §4.3, FR-DA-024–028, Code Standards #20
// Purpose:  Pure static module: enforces the three anti-chaos invariants (§3.10 / KD-17)
//           after the full assignment pass, before directive publication.

using UnityEngine;

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Enforces the three anti-chaos invariants after all assignments are computed (§3.10 / KD-17).
    /// Applied BEFORE directive publication (FR-DA-024). Up to 3 demotion passes (FR-DA-028).
    ///
    /// Invariant 1: DEFENSE-line agents in ZONAL &gt;= MIN_BACKLINE_AGENTS (FR-DA-025).
    /// Invariant 2: MAN_MARK assignments &lt;= MAX_MAN_MARK_ASSIGNMENTS (FR-DA-026).
    /// Invariant 3: non-ZONAL displacement from baseline &lt;= MAX_MARK_DISPLACEMENT_M (FR-DA-027).
    ///
    /// Returns false on F4 (hard fallback: emit all-ZONAL per FR-DA-032).
    /// Pure static; zero allocation. Defensive AI #14 §3.10 / §4.3.
    /// </summary>
    public static class InvariantEnforcer
    {
        private const int MaxPasses = 3;

        /// <summary>
        /// Enforces all three invariants in place, demoting violating assignments to ZONAL.
        /// </summary>
        /// <param name="snapshot">Current tick snapshot.</param>
        /// <param name="poolBuffer">EntityId-ascending HOLD_SHAPE pool.</param>
        /// <param name="poolCount">Number of valid pool entries.</param>
        /// <param name="currentTick">Tick index for any produced ZONAL assignments.</param>
        /// <param name="assignments">Assignment buffer (modified in place).</param>
        /// <returns>True when all invariants are satisfied; false when F4 hard fallback is required.</returns>
        public static bool Enforce(
            DefensiveSnapshot snapshot,
            int[]             poolBuffer,
            int               poolCount,
            int               currentTick,
            MarkAssignment[]  assignments)
        {
            for (int pass = 0; pass < MaxPasses; pass++)
            {
                // --- Invariant 1: minimum DEFENSE-line agents in ZONAL ---
                if (!EnforceMinBackline(snapshot, poolBuffer, poolCount, currentTick, assignments))
                    break; // Eligible pool exhausted — let post-loop check handle residual.

                // --- Invariant 2: maximum MAN_MARK assignments ---
                if (!EnforceMaxManMark(snapshot, poolBuffer, poolCount, currentTick, assignments))
                    break;

                // --- Invariant 3: maximum displacement ---
                if (!EnforceMaxDisplacement(snapshot, poolBuffer, poolCount, currentTick, assignments))
                    break;

                // All three satisfied: clean.
                return true;
            }

            // Post-loop final check.
            return AreAllSatisfied(snapshot, poolBuffer, poolCount, assignments);
        }

        // ── Invariant 1: MinBacklineAgents ───────────────────────────────────

        /// <summary>Returns false when no eligible demotion candidate exists (all non-ZONAL are emergency).</summary>
        private static bool EnforceMinBackline(
            DefensiveSnapshot snapshot,
            int[]             poolBuffer,
            int               poolCount,
            int               currentTick,
            MarkAssignment[]  assignments)
        {
            int defensiveTotal = 0;
            int defensiveZonal = 0;

            for (int p = 0; p < poolCount; p++)
            {
                int aIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, poolBuffer[p]);
                if (aIdx < 0) continue;
                if (snapshot.Agents[aIdx].Line != TacticalDirector.PositioningAI.LineId.Defense) continue;
                defensiveTotal++;
                if (assignments[p].Mode == MarkMode.Zonal) defensiveZonal++;
            }

            // Check condition: guard also requires enough total DEFENSE-line agents.
            if (defensiveZonal >= DefensiveAIConstants.MinBacklineAgents
             || defensiveTotal < DefensiveAIConstants.MinBacklineAgents)
                return true; // Satisfied or unsatisfiable (too few defense agents overall).

            // Find lowest-threat non-ZONAL DEFENSE-line assignment that is not an emergency override
            // and has a valid target (not COVER_GK_ZONE with no targetEntityId).
            int   bestPoolIdx = -1;
            float lowestThreat = float.MaxValue;

            for (int p = 0; p < poolCount; p++)
            {
                if (assignments[p].Mode == MarkMode.Zonal) continue;
                if (assignments[p].OverriddenThisTick) continue;
                if (assignments[p].TargetEntityId < 0) continue; // COVER_GK_ZONE excluded.

                int aIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, poolBuffer[p]);
                if (aIdx < 0) continue;
                if (snapshot.Agents[aIdx].Line != TacticalDirector.PositioningAI.LineId.Defense) continue;

                // Find threat score of the targeted opponent.
                int oIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, assignments[p].TargetEntityId);
                float threat = 0f;
                if (oIdx >= 0)
                {
                    ref readonly DefensiveAgentSnapshot opp = ref snapshot.Agents[oIdx];
                    threat = MarkAssigner.ThreatScore(opp.Position, opp.PerceivedFirstTouch,
                                                      snapshot.DefensiveTeamId);
                }

                if (threat < lowestThreat || (threat == lowestThreat && (bestPoolIdx < 0 || poolBuffer[p] < poolBuffer[bestPoolIdx])))
                {
                    lowestThreat = threat;
                    bestPoolIdx  = p;
                }
            }

            if (bestPoolIdx < 0)
                return false; // All non-ZONAL DEFENSE agents are emergency overrides; cannot demote.

            DemoteToZonal(snapshot, poolBuffer[bestPoolIdx], currentTick, ref assignments[bestPoolIdx]);
            return true;
        }

        // ── Invariant 2: MaxManMarkAssignments ───────────────────────────────

        private static bool EnforceMaxManMark(
            DefensiveSnapshot snapshot,
            int[]             poolBuffer,
            int               poolCount,
            int               currentTick,
            MarkAssignment[]  assignments)
        {
            int manMarkCount = 0;
            for (int p = 0; p < poolCount; p++)
                if (assignments[p].Mode == MarkMode.ManMark) manMarkCount++;

            if (manMarkCount <= DefensiveAIConstants.MaxManMarkAssignments)
                return true; // Satisfied.

            // Demote lowest-threat MAN_MARK assignment.
            int   bestPoolIdx  = -1;
            float lowestThreat = float.MaxValue;

            for (int p = 0; p < poolCount; p++)
            {
                if (assignments[p].Mode != MarkMode.ManMark) continue;

                int oIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, assignments[p].TargetEntityId);
                float threat = 0f;
                if (oIdx >= 0)
                {
                    ref readonly DefensiveAgentSnapshot opp = ref snapshot.Agents[oIdx];
                    threat = MarkAssigner.ThreatScore(opp.Position, opp.PerceivedFirstTouch,
                                                      snapshot.DefensiveTeamId);
                }

                if (threat < lowestThreat || (threat == lowestThreat && (bestPoolIdx < 0 || poolBuffer[p] < poolBuffer[bestPoolIdx])))
                {
                    lowestThreat = threat;
                    bestPoolIdx  = p;
                }
            }

            if (bestPoolIdx >= 0)
                DemoteToZonal(snapshot, poolBuffer[bestPoolIdx], currentTick, ref assignments[bestPoolIdx]);

            return true;
        }

        // ── Invariant 3: MaxMarkDisplacementM ────────────────────────────────

        private static bool EnforceMaxDisplacement(
            DefensiveSnapshot snapshot,
            int[]             poolBuffer,
            int               poolCount,
            int               currentTick,
            MarkAssignment[]  assignments)
        {
            for (int p = 0; p < poolCount; p++)
            {
                if (assignments[p].Mode == MarkMode.Zonal) continue;
                if (assignments[p].OverriddenThisTick) continue;

                int aIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, poolBuffer[p]);
                if (aIdx < 0) continue;

                Vector2 baseline  = snapshot.Agents[aIdx].BaselineSlot;
                Vector2 target    = assignments[p].TargetPosition;
                float   dx        = target.x - baseline.x;
                float   dy        = target.y - baseline.y;
                float   dist      = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > DefensiveAIConstants.MaxMarkDisplacementM)
                {
                    DemoteToZonal(snapshot, poolBuffer[p], currentTick, ref assignments[p]);
                    return true; // Demoted first violation; re-check next pass.
                }
            }

            return true; // No violation found.
        }

        // ── Post-loop verification ────────────────────────────────────────────

        private static bool AreAllSatisfied(
            DefensiveSnapshot snapshot,
            int[]             poolBuffer,
            int               poolCount,
            MarkAssignment[]  assignments)
        {
            int defensiveTotal = 0;
            int defensiveZonal = 0;
            int manMarkCount   = 0;

            for (int p = 0; p < poolCount; p++)
            {
                int aIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, poolBuffer[p]);
                if (aIdx >= 0 && snapshot.Agents[aIdx].Line == TacticalDirector.PositioningAI.LineId.Defense)
                {
                    defensiveTotal++;
                    if (assignments[p].Mode == MarkMode.Zonal) defensiveZonal++;
                }
                if (assignments[p].Mode == MarkMode.ManMark) manMarkCount++;

                if (assignments[p].Mode != MarkMode.Zonal && !assignments[p].OverriddenThisTick)
                {
                    if (aIdx >= 0)
                    {
                        Vector2 baseline = snapshot.Agents[aIdx].BaselineSlot;
                        Vector2 target   = assignments[p].TargetPosition;
                        float   dx       = target.x - baseline.x;
                        float   dy       = target.y - baseline.y;
                        if (Mathf.Sqrt(dx * dx + dy * dy) > DefensiveAIConstants.MaxMarkDisplacementM)
                            return false;
                    }
                }
            }

            bool inv1 = defensiveZonal >= DefensiveAIConstants.MinBacklineAgents
                     || defensiveTotal < DefensiveAIConstants.MinBacklineAgents;
            bool inv2 = manMarkCount   <= DefensiveAIConstants.MaxManMarkAssignments;
            return inv1 && inv2;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static void DemoteToZonal(
            DefensiveSnapshot snapshot,
            int               entityId,
            int               currentTick,
            ref MarkAssignment assignment)
        {
            int aIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, entityId);
            Vector2 slot = aIdx >= 0 ? snapshot.Agents[aIdx].BaselineSlot : Vector2.zero;
            assignment = MarkAssignment.MakeZonal(entityId, slot, currentTick);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
