// File:     src/attacking-ai/AttackingPoolBuilder.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Attacking AI #15 §3.2, FR-AT-006, FR-AT-007, FR-AT-025, Code Standards #20
// Purpose:  Constructs the per-tick attacking pool by filtering the snapshot to own-team,
//           active, non-GK, non-ball-carrier agents and sorting by EntityId-ascending.
//           Returns -1 on F2 (sentinel slot detected); 0 on empty pool; N on success.

using TacticalDirector.PressingAI;

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Builds the per-tick attacking pool from the perception snapshot. Pure static.
    /// Excludes GK (FR-AT-006 / KD-7), ball carrier (FR-AT-007 / KD-3), inactive agents,
    /// and agents on the wrong team. Sorts EntityId-ascending per #16 §3.2.5.
    /// Returns −1 on F2 failure (SENTINEL_NO_SLOT detected); ≥ 0 otherwise.
    /// Attacking AI #15 §3.2.
    /// </summary>
    internal static class AttackingPoolBuilder
    {
        /// <summary>
        /// Fills <paramref name="pool"/> from <paramref name="snapshot"/> and returns the
        /// pool count. Returns −1 when any agent has a SENTINEL_NO_SLOT baseline slot (F2).
        /// </summary>
        /// <param name="snapshot">Current tick input.</param>
        /// <param name="pool">Pre-allocated scratch buffer; capacity ≥ SQUAD_SIZE.</param>
        /// <returns>Pool count (0–10), or −1 for F2 sentinel failure.</returns>
        public static int Build(AttackingSnapshot snapshot, AttackPoolEntry[] pool)
        {
            int count = 0;
            int attackingTeamId  = snapshot.AttackingTeamId;
            int ballCarrierId    = snapshot.BallCarrierEntityId;

            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly AttackingAgentSnapshot a = ref snapshot.Agents[i];

                if (a.TeamId != attackingTeamId) continue;
                if (!a.IsActive)                 continue;
                if (a.IsGoalkeeper)              continue;  // KD-7 / FR-AT-006
                if (a.EntityId == ballCarrierId) continue;  // KD-3 / FR-AT-007

                // F2: SENTINEL_NO_SLOT — baseline slot is (−∞, −∞). §2.4 / FR-AT-025.
                if (PositioningAIView.IsSentinelSlot(a.BaselineSlot))
                    return -1;

                pool[count] = new AttackPoolEntry
                {
                    EntityId     = a.EntityId,
                    Position     = a.Position,
                    LateralPct   = a.BaselineSlot.y / AttackingAIConstants.PITCH_WIDTH_M,
                    Line         = a.Line,
                    AssignedRole = AttackRole.HoldWidth,
                    HasRunParams = false,
                };
                count++;
            }

            // Sort ascending by EntityId for determinism (#16 §3.2.5 / FR-AT-003).
            // Insertion sort: pool ≤ 10 agents, so O(N²) is constant-bounded; zero allocation.
            for (int i = 1; i < count; i++)
            {
                AttackPoolEntry key = pool[i];
                int j = i - 1;
                while (j >= 0 && pool[j].EntityId > key.EntityId)
                {
                    pool[j + 1] = pool[j];
                    j--;
                }
                pool[j + 1] = key;
            }

            return count;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
