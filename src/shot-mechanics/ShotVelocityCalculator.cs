// File:     src/shot-mechanics/ShotVelocityCalculator.cs
// Created:  2026-05-27
// Modified: 2026-05-28
// Author:   —
// Spec:     Shot Mechanics #6 §3.2, §4.1.2, Code Standards #20
// Purpose:  Computes scalar kick speed from the §3.2 formula: Finishing/LongShots sigmoid
//           blend, KickPower ceiling scaling, ContactZone modifier, spin–velocity trade-off,
//           fatigue scalar, and contact quality modifier. Implements IShotVelocityCalculator
//           for EC-008 NaN injection seam. Singleton; stateless. §4.1.2.

using UnityEngine;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Computes scalar kick speed (m/s) per §3.2 formula.
    /// Stateless singleton — no mutable fields. Implements IShotVelocityCalculator for EC-008.
    /// Shot Mechanics #6 §3.2, §4.1.2.
    /// </summary>
    public sealed class ShotVelocityCalculator : IShotVelocityCalculator
    {
        /// <summary>Production singleton instance. EC-008 tests inject NaNVelocityStub instead.</summary>
        public static readonly ShotVelocityCalculator Instance = new ShotVelocityCalculator();

        private ShotVelocityCalculator() { }

        /// <summary>
        /// Computes scalar kick speed (m/s). §3.2.
        /// Clamped to [VAbsoluteMin, VAbsoluteMax]. Logs if clamping was applied.
        /// </summary>
        public float Calculate(
            float       powerIntent,
            float       distanceToGoal,
            float       finishing,
            float       longShots,
            int         kickPower,
            ContactZone contactZone,
            float       spinIntent,
            float       fatigue,
            float       contactQualityModifier)
        {
            // §3.2.3 — Finishing/LongShots sigmoid blend by distance
            float w = 1.0f / (1.0f + Mathf.Exp(-(distanceToGoal - ShotMechanicsConstants.DMid)
                                                  / ShotMechanicsConstants.DScale));
            float effectiveAttr = (1.0f - w) * finishing + w * longShots;
            float attrFraction  = Mathf.Clamp(effectiveAttr, ShotMechanicsConstants.ATTR_MIN,
                                               ShotMechanicsConstants.ATTR_MAX)
                                  / ShotMechanicsConstants.ATTR_MAX;

            // §3.2.4 — KickPower scales the effective velocity ceiling
            float kickPowerFraction = Mathf.Clamp(kickPower, (int)ShotMechanicsConstants.ATTR_MIN,
                                                   (int)ShotMechanicsConstants.ATTR_MAX)
                                      / ShotMechanicsConstants.ATTR_MAX;
            float effectiveCeiling  = ShotMechanicsConstants.VFloor
                                      + kickPowerFraction * (ShotMechanicsConstants.VCeiling
                                                             - ShotMechanicsConstants.VFloor);

            // §3.2 base velocity
            float vBase = ShotMechanicsConstants.VFloor
                          + attrFraction * (effectiveCeiling - ShotMechanicsConstants.VFloor)
                          * powerIntent;

            // §3.2.5 — ContactZone modifier
            float vZone = vBase * ShotMechanicsConstants.GetContactZoneModifier(contactZone);

            // §3.2.6 — Spin–velocity trade-off
            float vSpin = vZone * (1.0f - ShotMechanicsConstants.SpinVelocityTradeOff * spinIntent);

            // §3.2.7 — Fatigue modifier
            float vFatigue = vSpin * (1.0f - ShotMechanicsConstants.FatiguePowerReduction * fatigue);

            // §3.2.8 — Contact quality modifier (from BodyMechanicsEvaluator)
            float vFinal = vFatigue * contactQualityModifier;

            // §3.2.10 — Clamp to absolute range
            float clamped = Mathf.Clamp(vFinal,
                                        ShotMechanicsConstants.VAbsoluteMin,
                                        ShotMechanicsConstants.VAbsoluteMax);

            if (!Mathf.Approximately(clamped, vFinal))
                Debug.LogWarning($"[ShotMechanics] §3.2.10: kick speed clamped {vFinal:F2} → {clamped:F2} m/s.");

            return clamped;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                    |
// | 1.0     | 2026-05-27 | —      | Initial implementation.                                                  |
// | 1.1     | 2026-05-28 | —      | H-3: Updated constant references to PascalCase names (DMid/DScale/VFloor |
// |         |            |        |   /VCeiling/VAbsoluteMin/VAbsoluteMax). M-5: bare 1 → ATTR_MIN.          |
#endregion
