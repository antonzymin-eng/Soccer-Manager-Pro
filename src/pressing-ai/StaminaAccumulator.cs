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
        /// Accumulates this tick's press-role fatigue costs into the caller's persistent
        /// per-EntityId fatigue ledger (AR-2 M-1). Writing to a persistent ledger — rather
        /// than to the per-tick snapshot, which is rebuilt and discarded — is what makes
        /// §3.7 stamina costs accumulate across ticks toward the §3.3/§3.7 ceiling.
        /// </summary>
        /// <param name="assignments">Committed role assignments for this tick.</param>
        /// <param name="assignmentCount">Number of valid entries in <paramref name="assignments"/>.</param>
        /// <param name="pressFatigueLedger">Persistent ledger indexed by EntityId; mutated in place, clamped to [0, 1].</param>
        /// <param name="ledgerCapacity">Length of <paramref name="pressFatigueLedger"/>; EntityIds outside [0, capacity) are skipped.</param>
        public static void ApplyAll(
            PressAssignment[] assignments,
            int assignmentCount,
            float[] pressFatigueLedger,
            int ledgerCapacity)
        {
            for (int i = 0; i < assignmentCount; i++)
            {
                PressRole role     = assignments[i].Role;
                int       entityId = assignments[i].EntityId;

                if (role == PressRole.HoldShape)
                    continue;
                if (entityId < 0 || entityId >= ledgerCapacity)
                    continue;

                Apply(role, ref pressFatigueLedger[entityId]);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-06-15 | —      | AR-2 M-1: ApplyAll now accumulates into a persistent per-EntityId fatigue ledger instead of the throwaway snapshot, so press fatigue persists across ticks. |
#endregion
