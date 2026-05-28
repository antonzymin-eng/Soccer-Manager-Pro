// File:     src/goalkeeper-mechanics/GoalkeeperAgentAttributes.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.2–§3.8, Agent Movement #2 §3.5.6, Code Standards #20
// Purpose:  Snapshot of the GK agent attributes and fatigue consumed by all §3 sub-systems.
//           Read once per tactical tick; held constant for the 60 Hz resolution phase.

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Goalkeeper agent attribute snapshot consumed by §3.2–§3.8 formulas.
    /// Attributes are in [1, 20]; Fatigue is in [0, 1] (0 = rested, 1 = fatigued per CLAUDE.md).
    /// Normalised accessors expose [0, 1] values using ATTR_MAX = 20.
    /// Agent Movement #2 §3.5.6. Goalkeeper Mechanics #11 §3.2–§3.8.
    /// </summary>
    public struct GoalkeeperAgentAttributes
    {
        /// <summary>Reflexes attribute [1–20]. Governs reaction-pipeline latency reduction (§3.2). Agent Movement #2 §3.5.6.</summary>
        public float Reflexes;

        /// <summary>Handling attribute [1–20]. Governs handling-quality scalar and reach radius (§3.3.4 / §3.5). Agent Movement #2 §3.5.6.</summary>
        public float Handling;

        /// <summary>Composure attribute [1–20]. Reserved for Stage 1+ decision-tree calm-under-pressure extension. Agent Movement #2 §3.5.6.</summary>
        public float Composure;

        /// <summary>Strength attribute [1–20]. Governs dive launch impulse, peak hand Z, reach, and duel score (§3.3 / §3.5 / §3.6). Agent Movement #2 §3.5.6.</summary>
        public float Strength;

        /// <summary>Aerial attribute [1–20]. Governs dive launch impulse, peak hand Z, reach, and cross-claim duel score (§3.3 / §3.6). Agent Movement #2 §3.5.6.</summary>
        public float Aerial;

        /// <summary>Balance attribute [1–20]. Governs cross-claim duel score (§3.6). Agent Movement #2 §3.5.6.</summary>
        public float Balance;

        /// <summary>OneVsOne attribute [1–20]. Governs reaction reduction and handling bonus in 1v1 state (§3.2 / §3.5 / KD-20). Agent Movement #2 §3.5.6.</summary>
        public float OneVsOne;

        /// <summary>Pace attribute [1–20]. Governs rush launch speed (§3.7). Agent Movement #2 §3.5.6.</summary>
        public float Pace;

        /// <summary>Throwing attribute [1–20]. Governs throw distribution accuracy (§3.8). Agent Movement #2 §3.5.6.</summary>
        public float Throwing;

        /// <summary>Kicking attribute [1–20]. Governs kick distribution accuracy (§3.8). Agent Movement #2 §3.5.6.</summary>
        public float Kicking;

        /// <summary>
        /// Fatigue scalar [0, 1]. 0 = fully rested, 1 = fully fatigued.
        /// CLAUDE.md fatigue convention: 0 = rested, 1 = fatigued (FR-GK-011 / KD-8).
        /// Applied as a penalty in §3.3 / §3.5 / §3.7.
        /// </summary>
        public float Fatigue;

        /// <summary>
        /// Team identifier (0 or 1). Team 0 defends goal at X = 0; team 1 defends goal at X = PitchLengthM.
        /// Ball Physics #1 §1.2 coordinate system.
        /// </summary>
        public int TeamId;

        // ── Normalised accessors [0, 1] ──────────────────────────────────────────────
        // Clamp then divide by ATTR_MAX (20). Clamp is defensive; Decision Tree should
        // never supply out-of-range values.

        /// <summary>Reflexes normalised to [0, 1]. §3.2.</summary>
        public float ReflexesNorm => Clamp01Norm(Reflexes);

        /// <summary>Handling normalised to [0, 1]. §3.3.4 / §3.5.</summary>
        public float HandlingNorm => Clamp01Norm(Handling);

        /// <summary>Composure normalised to [0, 1]. Reserved for Stage 1+.</summary>
        public float ComposureNorm => Clamp01Norm(Composure);

        /// <summary>Strength normalised to [0, 1]. §3.3 / §3.5 / §3.6.</summary>
        public float StrengthNorm => Clamp01Norm(Strength);

        /// <summary>Aerial normalised to [0, 1]. §3.3 / §3.6.</summary>
        public float AerialNorm => Clamp01Norm(Aerial);

        /// <summary>Balance normalised to [0, 1]. §3.6.</summary>
        public float BalanceNorm => Clamp01Norm(Balance);

        /// <summary>OneVsOne normalised to [0, 1]. §3.2 / §3.5 / KD-20.</summary>
        public float OneVsOneNorm => Clamp01Norm(OneVsOne);

        /// <summary>Pace normalised to [0, 1]. §3.7.</summary>
        public float PaceNorm => Clamp01Norm(Pace);

        /// <summary>Throwing normalised to [0, 1]. §3.8.</summary>
        public float ThrowingNorm => Clamp01Norm(Throwing);

        /// <summary>Kicking normalised to [0, 1]. §3.8.</summary>
        public float KickingNorm => Clamp01Norm(Kicking);

        // ── Private helper ───────────────────────────────────────────────────────────

        private static float Clamp01Norm(float attribute)
        {
            float clamped = attribute < GoalkeeperConstants.ATTR_MIN ? GoalkeeperConstants.ATTR_MIN
                          : attribute > GoalkeeperConstants.ATTR_MAX ? GoalkeeperConstants.ATTR_MAX
                          : attribute;
            return clamped / GoalkeeperConstants.ATTR_MAX;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
