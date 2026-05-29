// File:     src/defensive-ai/HoldShapePoolFilter.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Defensive AI #14 §3.2, §4.3, Code Standards #20
// Purpose:  Pure static module: filters the HOLD_SHAPE pool each tick by excluding
//           the goalkeeper and agents with active press roles from Pressing AI #13.

using TacticalDirector.PressingAI;

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Builds the HOLD_SHAPE pool for one tick (§3.2).
    /// Excludes the GK (FR-DA-009 / KD-7) and agents with PRIMARY_PRESS or COVER_SHADOW
    /// press roles (FR-DA-010 / KD-4). Output is EntityId-ascending (FR-DA-003 / #16 §3.2.5).
    /// Defensive AI #14 §3.2 / §4.3.
    ///
    /// Zero heap allocation — writes into a caller-supplied buffer.
    /// </summary>
    public static class HoldShapePoolFilter
    {
        /// <summary>
        /// Builds the HOLD_SHAPE pool from the snapshot.
        /// </summary>
        /// <param name="snapshot">Current tick snapshot (read-only).</param>
        /// <param name="poolBuffer">Caller-supplied pre-allocated buffer. Length must be &gt;= 11.</param>
        /// <param name="poolCount">Number of EntityIds written into <paramref name="poolBuffer"/>.</param>
        public static void BuildPool(
            DefensiveSnapshot snapshot,
            int[]             poolBuffer,
            out int           poolCount)
        {
            poolCount = 0;
            int defensiveTeamId = snapshot.DefensiveTeamId;

            for (int i = 0; i < snapshot.AgentCount; i++)
            {
                ref readonly DefensiveAgentSnapshot a = ref snapshot.Agents[i];

                if (a.TeamId != defensiveTeamId)
                    continue;

                // Exclude inactive (substituted / red-carded) agents.
                if (!a.IsActive)
                    continue;

                // Exclusion rule 1: GK (FR-DA-009 / KD-7).
                if (a.IsGoalkeeper)
                    continue;

                // Exclusion rule 2: agents with active press roles from #13 (FR-DA-010 / KD-4).
                if (a.PressRole == PressRole.PrimaryPress || a.PressRole == PressRole.CoverShadow)
                    continue;

                // Failure mode F3 recovery: the orchestrator sets PressRole = HoldShape for all
                // agents when #13 directive is absent (FR-DA-031). No additional handling needed here.

                poolBuffer[poolCount++] = a.EntityId;
            }

            // Pool is EntityId-ascending because the orchestrator populates Agents in
            // EntityId-ascending order per #16 §3.2.5 (FR-DA-003). No sort required.
        }

        /// <summary>
        /// Returns the pool index of the given EntityId, or -1 if not found.
        /// </summary>
        public static int IndexOf(int[] poolBuffer, int poolCount, int entityId)
        {
            for (int i = 0; i < poolCount; i++)
            {
                if (poolBuffer[i] == entityId)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Returns the snapshot index for the given EntityId, or -1 if not found.
        /// </summary>
        public static int SnapshotIndexOf(DefensiveSnapshot snapshot, int entityId)
        {
            for (int i = 0; i < snapshot.AgentCount; i++)
            {
                if (snapshot.Agents[i].EntityId == entityId)
                    return i;
            }

            return -1;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
