// File:     src/shot-mechanics/BodyMechanicsResult.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Shot Mechanics #6 §3.7, Code Standards #20
// Purpose:  Output struct from BodyMechanicsEvaluator containing composite score,
//           contact quality modifier, and stumble trigger flag. §3.7.

namespace TacticalDirector.ShotMechanics
{
    /// <summary>Result of body mechanics evaluation. §3.7.</summary>
    public struct BodyMechanicsResult
    {
        /// <summary>Composite body mechanics score [0.0, 1.0]. §3.7.7.</summary>
        public float Score;

        /// <summary>Contact quality modifier [CQM_MIN, CQM_MAX] derived from Score. §3.7.8.</summary>
        public float ContactQualityModifier;

        /// <summary>True if BodyMechanicsScore &lt; StumbleThreshold AND PowerIntent &gt; StumblePowerThreshold. §3.7.9.</summary>
        public bool StumbleTriggered;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                          |
// | 1.0     | 2026-05-28 | —      | Extracted from BodyMechanicsEvaluator.cs (H-1). |
#endregion
