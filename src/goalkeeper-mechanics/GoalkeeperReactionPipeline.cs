// File:     src/goalkeeper-mechanics/GoalkeeperReactionPipeline.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.2, KD-2, KD-18, Code Standards #20
// Purpose:  Pure-static implementation of the §3.2 shot-reaction pipeline formulas.
//           Computes shotDetectedTickMs, requiredReactionMs, reactionWindowAchieved, and
//           the KD-2 telemetry-only ReactionLabel. All methods are side-effect-free.

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Pure-static shot-reaction pipeline per §3.2.
    /// All methods are deterministic and side-effect-free.
    /// Goalkeeper Mechanics #11 §3.2.
    /// </summary>
    public static class GoalkeeperReactionPipeline
    {
        /// <summary>
        /// Computes the match time (ms) at which the GK detects the shot.
        /// Formula: shotDetectedTickMs = shotExecutedMatchTimeMs + PERCEPTION_BASE_LATENCY_MS
        ///          × perceptionLatencyScale(gk).
        /// perceptionLatencyScale = 1 - PERCEPTION_REFLEXES_SCALE × Reflexes_norm.
        /// §3.2.1. Goalkeeper Mechanics #11 §3.2.
        /// </summary>
        /// <param name="shotExecutedMatchTimeMs">Match time (ms) of the ShotExecutedEvent. §3.2.1.</param>
        /// <param name="attrs">GK agent attributes; Reflexes_norm governs latency scale. §3.2.1.</param>
        /// <returns>shotDetectedTickMs: time (ms) at which the GK detects the shot.</returns>
        public static float ComputeShotDetectedTickMs(float shotExecutedMatchTimeMs, GoalkeeperAgentAttributes attrs)
        {
            float perceptionLatencyScale = 1.0f
                - GoalkeeperConstants.PerceptionReflexesScale * attrs.ReflexesNorm;

            return shotExecutedMatchTimeMs
                + GoalkeeperConstants.PerceptionBaseLatencyMs * perceptionLatencyScale;
        }

        /// <summary>
        /// Computes the required reaction time (ms) for this GK to process the shot.
        /// Formula: REACTION_BASE_MS - REACTION_REFLEXES_COEFF × Reflexes_norm
        ///          + REACTION_BALL_SPEED_COEFF × max(0, ballSpeedMps - REACTION_BALL_SPEED_REF_MPS)
        ///          - (state == OneOnOne ? ONE_VS_ONE_REACTION_COEFF × OneVsOne_norm : 0).
        /// The OneVsOne term subtracts from required time (faster 1v1 reads per KD-20).
        /// §3.2.2. Goalkeeper Mechanics #11 §3.2.
        /// </summary>
        /// <param name="attrs">GK agent attributes. §3.2.2.</param>
        /// <param name="ballSpeedMps">Ball speed (m/s) at shot execution. §3.2.2.</param>
        /// <param name="state">Current GK state; OneVsOne term only applies in OneOnOne state (KD-20). §3.2.2.</param>
        /// <returns>requiredReactionMs: GK-specific minimum reaction time (ms).</returns>
        public static float ComputeRequiredReactionMs(
            GoalkeeperAgentAttributes attrs,
            float ballSpeedMps,
            GoalkeeperState state)
        {
            float required = GoalkeeperConstants.ReactionBaseMs
                           - GoalkeeperConstants.ReactionReflexesCoeff * attrs.ReflexesNorm
                           + GoalkeeperConstants.ReactionBallSpeedCoeff
                             * Mathf_Max(0.0f, ballSpeedMps - GoalkeeperConstants.ReactionBallSpeedRefMps);

            if (state == GoalkeeperState.OneOnOne)
            {
                required -= GoalkeeperConstants.OneVsOneReactionCoeff * attrs.OneVsOneNorm;
            }

            return required;
        }

        /// <summary>
        /// Computes the normalised reaction-window achievement [0, 1] from elapsed and required reaction times.
        /// Formula: reactionOffsetMs = elapsedSinceShotMs - requiredReactionMs.
        ///   if reactionOffsetMs &lt;= 0: reactionWindowAchieved = 1 - clamp01(-offset / REACTION_EARLY_TOLERANCE_MS)
        ///   else:                       reactionWindowAchieved = 1 - clamp01( offset / REACTION_LATE_TOLERANCE_MS)
        /// §3.2.3. Goalkeeper Mechanics #11 §3.2 / KD-18.
        /// </summary>
        /// <param name="elapsedSinceShotMs">Time elapsed (ms) since shotDetectedTickMs. §3.2.3.</param>
        /// <param name="requiredReactionMs">Required reaction time (ms) from ComputeRequiredReactionMs. §3.2.3.</param>
        /// <returns>reactionWindowAchieved in [0, 1]. 1.0 = exactly on time; lower = early or late.</returns>
        public static float ComputeReactionWindowAchieved(float elapsedSinceShotMs, float requiredReactionMs)
        {
            float reactionOffsetMs = elapsedSinceShotMs - requiredReactionMs;

            if (reactionOffsetMs <= 0.0f)
            {
                // Early commit — penalised by misdirection risk
                return 1.0f - Clamp01(-reactionOffsetMs / GoalkeeperConstants.ReactionEarlyToleranceMs);
            }
            else
            {
                // Late commit — penalised by reach loss
                return 1.0f - Clamp01(reactionOffsetMs / GoalkeeperConstants.ReactionLateToleranceMs);
            }
        }

        /// <summary>
        /// Assigns the KD-2 telemetry-only ReactionLabel from reactionWindowAchieved.
        /// NEVER consumed by physics formulas (KD-2). §3.2.4.
        /// Goalkeeper Mechanics #11 §3.2.4.
        /// </summary>
        /// <param name="reactionWindowAchieved">Normalised reaction achievement [0, 1] from ComputeReactionWindowAchieved.</param>
        /// <returns>ReactionLabel telemetry bucket. For telemetry only (KD-2).</returns>
        public static ReactionLabel ComputeReactionLabel(float reactionWindowAchieved)
        {
            if (reactionWindowAchieved > GoalkeeperConstants.ReflexiveLabelThreshold)
            {
                return ReactionLabel.Reflexive;
            }

            if (reactionWindowAchieved < GoalkeeperConstants.SluggishLabelThreshold)
            {
                return ReactionLabel.Sluggish;
            }

            return ReactionLabel.Standard;
        }

        // ── Private helpers ──────────────────────────────────────────────────────────

        private static float Clamp01(float v)
        {
            return v < 0.0f ? 0.0f : v > 1.0f ? 1.0f : v;
        }

        private static float Mathf_Max(float a, float b)
        {
            return a > b ? a : b;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
