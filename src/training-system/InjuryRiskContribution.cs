// File:     src/training-system/InjuryRiskContribution.cs
// Created:  2026-07-30
// Modified: 2026-07-31
// Author:   —
// Spec:     Training System #29 §2.2 / §3.4; Code Standards #20
// Purpose:  Read-only training input that the future medical system consumes.

namespace TacticalDirector.TrainingSystem
{
    /// <summary>Read-only training contribution to the injury model owned by Injuries &amp; Medical #41.</summary>
    public readonly struct InjuryRiskContribution
    {
        /// <summary>Initializes the bounded risk score.</summary>
        public InjuryRiskContribution(int riskScore)
        {
            RiskScore = riskScore;
        }

        /// <summary>Bounded training-derived risk score.</summary>
        public int RiskScore { get; }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-30 | —      | Initial Stage-2 core.   |
// | 1.1     | 2026-07-31 | —      | AR-1 L-4: `Author:` header line added (FR-CS-056). |
#endregion