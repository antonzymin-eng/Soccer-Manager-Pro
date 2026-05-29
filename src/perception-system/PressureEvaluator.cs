// File:     src/perception-system/PressureEvaluator.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Perception System #7 §3.6, First Touch Mechanics #4 §3.5, Code Standards #20
// Purpose:  Computes PressureScalar for an agent using the formula from First Touch §3.5
//           (reused verbatim — no new derivation). Single use: result passed to FovCalculator.
//           Not used for L_rec or shoulder check timing (§3.6.3).
//           Static class. All methods are deterministic. No side effects.

using UnityEngine;

using TacticalDirector.AgentMovement;

namespace TacticalDirector.PerceptionSystem
{
    /// <summary>
    /// Computes the pressure scalar for FoV narrowing. Formula reused verbatim from
    /// First Touch Mechanics #4 §3.5 — no new derivation. Single use in this system.
    /// PressureScalar ∈ [0, 1]. Perception System #7 §3.6.
    /// </summary>
    public static class PressureEvaluator
    {
        /// <summary>
        /// Computes pressure scalar for the observer from nearby opponents.
        /// Formula (First Touch #4 §3.5.2–§3.5.3):
        ///   rawPressure = Σ (MIN_PRESSURE_DISTANCE / max(dist, MIN_PRESSURE_DISTANCE))²
        ///   PressureScalar = clamp(rawPressure / PRESSURE_SATURATION, 0, 1)
        /// Perception System #7 §3.6.2.
        /// </summary>
        /// <param name="observerPos">Observer's 2D world position.</param>
        /// <param name="observerTeamId">Observer's team ID. Only opponents contribute to pressure.</param>
        /// <param name="agentStates">Full agent state array indexed 0–21.</param>
        /// <param name="agentAttrs">Full agent attributes array indexed 0–21.</param>
        /// <param name="candidateIds">Entity IDs within PRESSURE_RADIUS from the spatial hash query.</param>
        /// <param name="candidateCount">Number of valid entries in candidateIds.</param>
        public static float ComputePressureScalar(
            Vector2 observerPos,
            int observerTeamId,
            AgentState[] agentStates,
            PerceptionAgentAttributes[] agentAttrs,
            int[] candidateIds,
            int candidateCount)
        {
            float rawPressure = 0.0f;

            for (int i = 0; i < candidateCount; i++)
            {
                int id = candidateIds[i];
                if (id < 0)
                {
                    continue; // skip ball entity
                }

                if (agentAttrs[id].TeamId == observerTeamId)
                {
                    continue; // only opponents contribute
                }

                float dist = (agentStates[id].Position - observerPos).magnitude;
                float safeDist = Mathf.Max(dist, PerceptionConstants.MinPressureDistance);
                float contribution = PerceptionConstants.MinPressureDistance / safeDist;
                rawPressure += contribution * contribution;
            }

            return Mathf.Clamp(rawPressure / PerceptionConstants.PressureSaturation, 0.0f, 1.0f);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
