// File:     src/shot-mechanics/ShotLaunchAngleCalculator.cs
// Created:  2026-05-27
// Modified: 2026-05-28
// Author:   —
// Spec:     Shot Mechanics #6 §3.3, Code Standards #20
// Purpose:  Computes vertical launch angle (degrees) from ContactZone base angle,
//           power lift modifier, spin lift modifier, body lean penalty, and body
//           shape penalty. Pure static calculation; no side effects.

using UnityEngine;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Computes launch angle (degrees above horizontal) per §3.3 formula.
    /// Pure static calculation — no side effects, no mutable state.
    /// Shot Mechanics #6 §3.3.
    /// </summary>
    public static class ShotLaunchAngleCalculator
    {
        /// <summary>
        /// Computes launch angle (degrees). Clamped to [LaunchAngleMin, LaunchAngleMax].
        /// §3.3 formula: BaseAngle + PowerLift + SpinLift + BodyLeanPenalty + BodyShapePenalty.
        /// Shot Mechanics #6 §3.3.
        /// </summary>
        /// <param name="contactZone">Ball contact zone; determines BaseAngle.</param>
        /// <param name="powerIntent">Shot power intent [0, 1]; low power increases angle.</param>
        /// <param name="spinIntent">Spin intent [0, 1]; high spin increases angle.</param>
        /// <param name="bodyLeanAngleDeg">Forward body lean angle (degrees); adds unintended loft.</param>
        /// <param name="bodyMechanicsScore">Body mechanics score [0, 1]; low score increases angle penalty.</param>
        public static float Compute(
            ContactZone contactZone,
            float       powerIntent,
            float       spinIntent,
            float       bodyLeanAngleDeg,
            float       bodyMechanicsScore)
        {
            float angle = ShotMechanicsConstants.GetBaseAngle(contactZone);

            // §3.3.4 — Power lift: lower power = more loft (foot under ball)
            angle += ShotMechanicsConstants.PowerLiftScale * (1.0f - powerIntent);

            // §3.3.5 — Spin lift: higher spin intent lifts trajectory
            angle += ShotMechanicsConstants.SpinLiftScale * spinIntent;

            // §3.3.6 — Body lean transfers into upward angle (forward lean → more loft)
            angle += bodyLeanAngleDeg * ShotMechanicsConstants.BodyLeanTransferCoefficient;

            // §3.3.7 — Body shape penalty: poor mechanics adds unintended loft
            angle += ShotMechanicsConstants.BodyShapeMaxPenalty * (1.0f - bodyMechanicsScore);

            return Mathf.Clamp(angle,
                               ShotMechanicsConstants.LaunchAngleMin,
                               ShotMechanicsConstants.LaunchAngleMax);
        }

        /// <summary>
        /// Derives body lean angle (degrees) from agent velocity magnitude at Stage 0.
        /// At Stage 0 a dedicated BodyLeanAngle field is not available on AgentState;
        /// this empirical approximation stands in until Stage 1 animation integration.
        /// §3.3.6, §4.3.2.
        /// </summary>
        public static float DeriveBodyLeanAngle(float agentSpeedMs)
        {
            return Mathf.Clamp(agentSpeedMs / ShotMechanicsConstants.BodyLeanMaxSpeed
                               * ShotMechanicsConstants.BodyLeanMaxDeg,
                               0.0f,
                               ShotMechanicsConstants.BodyLeanMaxDeg);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                          |
// | 1.0     | 2026-05-27 | —      | Initial implementation.                                                        |
// | 1.1     | 2026-05-28 | —      | M-2: DeriveBodyLeanAngle local magic constants promoted to ShotMechanicsConstants. |
#endregion
