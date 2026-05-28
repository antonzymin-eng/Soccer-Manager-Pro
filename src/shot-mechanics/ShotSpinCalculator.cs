// File:     src/shot-mechanics/ShotSpinCalculator.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §3.4, Code Standards #20
// Purpose:  Computes world-space spin vector (rad/s) from ContactZone, SpinIntent,
//           PowerIntent, Technique attribute, and facing direction. Pure static calculation.

using UnityEngine;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Computes spin vector (rad/s) per §3.4. Pure static calculation — no side effects.
    /// Outputs angular velocity Vector3 for Magnus model input in Ball Physics.
    /// Shot Mechanics #6 §3.4.
    /// </summary>
    public static class ShotSpinCalculator
    {
        /// <summary>
        /// Computes world-space spin vector (rad/s). Magnitude clamped to SpinAbsoluteMax.
        /// §3.4: topspin (forward rotation), backspin (backward), sidespin (lateral curl).
        /// Shot Mechanics #6 §3.4.
        /// </summary>
        /// <param name="contactZone">Determines spin base values per zone.</param>
        /// <param name="spinIntent">Deliberate spin application [0, 1].</param>
        /// <param name="powerIntent">Shot power [0, 1]; high power amplifies topspin.</param>
        /// <param name="technique">Technique attribute [1–20]; scales sidespin. §3.4.5.</param>
        /// <param name="facingDirectionXY">Agent's facing direction unit vector in XY plane.</param>
        public static Vector3 Compute(
            ContactZone contactZone,
            float       spinIntent,
            float       powerIntent,
            int         technique,
            Vector2     facingDirectionXY)
        {
            // §3.4.2 — Topspin: dominant at Centre, reduced by high spin intent
            float topspinBase = GetTopspinBase(contactZone);
            float topspinMag  = topspinBase * (1.0f - spinIntent) * powerIntent;

            // §3.4.3 — Backspin: dominant at BelowCentre, driven by spin intent
            float backspinBase = GetBackspinBase(contactZone);
            float backspinMag  = backspinBase * spinIntent;

            // §3.4.4 — Sidespin: dominant at OffCentre, modulated by Technique
            float techScale  = ComputeTechniqueScale(technique);
            float sidespinBase = GetSidespinBase(contactZone);
            float sidespinMag  = sidespinBase * spinIntent * techScale;

            // §3.4 — Assemble spin vector in agent-local frame, then rotate to world space.
            // Topspin = forward rotation axis (right of facing direction in XY, upward component).
            // Backspin = opposite of topspin.
            // Sidespin = upward axis (Z), sign from right-of-facing.
            // At Stage 0: simplified world-space rotation using facing direction.
            Vector3 forward = new Vector3(facingDirectionXY.x, facingDirectionXY.y, 0.0f);
            Vector3 right   = new Vector3(facingDirectionXY.y, -facingDirectionXY.x, 0.0f); // perp in XY
            Vector3 up      = Vector3.up;

            // Net topspin component: rotation axis = right × forward cross, pointing right
            // Net backspin: inverse of topspin
            float netForwardSpin = topspinMag - backspinMag;
            Vector3 spinVector   = right * netForwardSpin + up * sidespinMag;

            // §3.4.10 — Clamp spin magnitude to SpinAbsoluteMax (80 rad/s, matches Ball Physics MAX_SPIN)
            float magnitude = spinVector.magnitude;
            if (magnitude > ShotMechanicsConstants.SpinAbsoluteMax)
                spinVector = spinVector * (ShotMechanicsConstants.SpinAbsoluteMax / magnitude);

            return spinVector;
        }

        private static float GetTopspinBase(ContactZone zone)
        {
            switch (zone)
            {
                case ContactZone.Centre:      return ShotMechanicsConstants.TopspinBaseCentre;
                case ContactZone.BelowCentre: return ShotMechanicsConstants.TopspinBaseBelowCentre;
                case ContactZone.OffCentre:   return ShotMechanicsConstants.TopspinBaseOffCentre;
                default:
                    Debug.LogError($"[ShotMechanics] ShotSpinCalculator.GetTopspinBase: unknown zone={zone}.");
                    return ShotMechanicsConstants.TopspinBaseCentre;
            }
        }

        private static float GetBackspinBase(ContactZone zone)
        {
            switch (zone)
            {
                case ContactZone.Centre:      return ShotMechanicsConstants.BackspinBaseCentre;
                case ContactZone.BelowCentre: return ShotMechanicsConstants.BackspinBaseBelowCentre;
                case ContactZone.OffCentre:   return ShotMechanicsConstants.BackspinBaseOffCentre;
                default:
                    Debug.LogError($"[ShotMechanics] ShotSpinCalculator.GetBackspinBase: unknown zone={zone}.");
                    return ShotMechanicsConstants.BackspinBaseCentre;
            }
        }

        private static float GetSidespinBase(ContactZone zone)
        {
            switch (zone)
            {
                case ContactZone.Centre:      return ShotMechanicsConstants.SidespinBaseCentre;
                case ContactZone.BelowCentre: return ShotMechanicsConstants.SidespinBaseBelowCentre;
                case ContactZone.OffCentre:   return ShotMechanicsConstants.SidespinBaseOffCentre;
                default:
                    Debug.LogError($"[ShotMechanics] ShotSpinCalculator.GetSidespinBase: unknown zone={zone}.");
                    return ShotMechanicsConstants.SidespinBaseCentre;
            }
        }

        /// <summary>
        /// §3.4.5 — TechniqueScale: linearly interpolates between TechniqueSpinBase and TechniqueSpinMax.
        /// At Technique=1: TechniqueSpinBase (0.6). At Technique=20: TechniqueSpinMax (1.0).
        /// </summary>
        private static float ComputeTechniqueScale(int technique)
        {
            float t = (Mathf.Clamp(technique, (int)ShotMechanicsConstants.ATTR_MIN,
                                   (int)ShotMechanicsConstants.ATTR_MAX) - ShotMechanicsConstants.ATTR_MIN)
                      / (ShotMechanicsConstants.ATTR_MAX - ShotMechanicsConstants.ATTR_MIN);
            return ShotMechanicsConstants.TechniqueSpinBase
                   + t * (ShotMechanicsConstants.TechniqueSpinMax - ShotMechanicsConstants.TechniqueSpinBase);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-05-27 | —      | Initial implementation.                                        |
// | 1.1     | 2026-05-28 | —      | M-4: Bare literal 1 replaced with ATTR_MIN in TechniqueScale.  |
#endregion
