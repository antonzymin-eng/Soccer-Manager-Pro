// File:     src/training-system/InjuryRiskContribution.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Training System #29 §2.2 / §3.4 KD-5 (FR-TR-017); Code Standards #20
// Purpose:  The read-only per-player injury-risk scalar #41 consumes. #29 computes only this input —
//           occurrence, severity and recovery are #41's, and #29 owns no injury model.

namespace TacticalDirector.TrainingSystem
{
    /// <summary>
    /// #29's injury-risk output (KD-5 / FR-TR-017): one clamped integer on the
    /// <c>[0, TrainingSystemConstants.InjuryRiskMax]</c> scale, produced by
    /// <see cref="TrainingStep.ComputeInjuryRisk"/> from the training-fatigue accumulator and the
    /// conditioning shortfall, mitigated by the player's own robustness attributes.
    /// <para>
    /// It is a <b>value #41 pulls</b>, not an interface #29 declares against #41 (FR-LW-031). The
    /// boundary is one-directional in both specs: #41 MUST read this read-only and MUST NOT reach into
    /// <see cref="TrainingState"/>; #29 MUST NOT grow an injury model of its own (#29 §7.3 / #41 §7.3).
    /// </para>
    /// </summary>
    public readonly struct InjuryRiskContribution
    {
        /// <summary>The clamped risk scalar on the <c>[0, TrainingSystemConstants.InjuryRiskMax]</c> scale (§3.4).</summary>
        public readonly int RiskScore;

        /// <summary>Wraps an already-clamped risk scalar. Callers use <see cref="TrainingStep.ComputeInjuryRisk"/>; this exists so a test or a #41 fixture can pin an exact input.</summary>
        /// <param name="riskScore">The risk scalar; the caller is responsible for the clamp.</param>
        public InjuryRiskContribution(int riskScore)
        {
            RiskScore = riskScore;
        }

        /// <summary>The zero contribution — a perfectly conditioned, unfatigued player. <c>default</c> is legal here: the field is a magnitude, and zero is its true identity.</summary>
        public static InjuryRiskContribution None => default;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                  |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#29 T0, KD-5). |
#endregion
