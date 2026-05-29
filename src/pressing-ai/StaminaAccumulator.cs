// File:     src/pressing-ai/StaminaAccumulator.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Pressing AI #13 §3.7, Code Standards #20
// Purpose:  Pure static class: applies per-tick fatigue cost to agents in active
//           press roles, clamping fatigue to [0, 1].

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Applies stamina costs for press-role participation per §3.7.
    /// PrimaryPress costs <see cref="PressingAIConstants.StaminaCostPrimaryPerTick"/>;
    /// CoverShadow costs <see cref="PressingAIConstants.StaminaCostShadowPerTick"/>;
    /// HoldShape agents are unchanged.
    /// Pressing AI #13 §3.7.
    /// </summary>
    public static class StaminaAccumulator
    {
        /// <summary>
        /// Applies fatigue cost for the given role to <paramref name="currentFatigue"/>.
        /// </summary>
        /// <param name="role">Committed press role for this tick.</param>
        /// <param name="currentFatigue">Current fatigue scalar [0, 1]; updated in place.</param>
        public static void Apply(PressRole role, ref float currentFatigue)
        {
            float cost;
            switch (role)
            {
                case PressRole.PrimaryPress:
                    cost = PressingAIConstants.StaminaCostPrimaryPerTick;
                    break;
                case PressRole.CoverShadow:
                    cost = PressingAIConstants.StaminaCostShadowPerTick;
                    break;
                default:
                    return;
            }

            float updated = currentFatigue + cost;
            currentFatigue = updated > 1f ? 1f : updated;
        }

        /// <summary>
        /// Applies fatigue costs across all agents in <paramref name="snapshot"/> using
        /// the committed roles from <paramref name="assignments"/>.
        /// Writes updated fatigue values back into <paramref name="snapshot"/>.
        /// </summary>
        /// <param name="snapshot">Tick snapshot; agent Fatigue fields mutated in place.</param>
        /// <param name="assignments">Committed role assignments for this tick.</param>
        /// <param name="assignmentCount">Number of valid entries in <paramref name="assignments"/>.</param>
        public static void ApplyAll(
            PressingSnapshot snapshot,
            PressAssignment[] assignments,
            int assignmentCount)
        {
            for (int i = 0; i < assignmentCount; i++)
            {
                PressRole role     = assignments[i].Role;
                int       entityId = assignments[i].EntityId;

                if (role == PressRole.HoldShape)
                    continue;

                // Locate agent in snapshot and apply cost.
                for (int j = 0; j < snapshot.Agents.Length; j++)
                {
                    if (snapshot.Agents[j].EntityId == entityId)
                    {
                        Apply(role, ref snapshot.Agents[j].Fatigue);
                        break;
                    }
                }
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
