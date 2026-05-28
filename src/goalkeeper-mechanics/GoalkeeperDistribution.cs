// File:     src/goalkeeper-mechanics/GoalkeeperDistribution.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.8, KD-6, KD-16, Code Standards #20
// Purpose:  Release-point geometry, windup duration, accuracy coefficient, and F-05/F-09
//           target validation for GK distribution. All methods are pure static.

using UnityEngine;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// GK distribution geometry and validation formulas per §3.8 / KD-16.
    /// KD-1: DeliveryKind is a kinematic-profile lookup label — no physics formula gates on it.
    /// All methods are deterministic and side-effect-free.
    /// Goalkeeper Mechanics #11 §3.8.
    /// </summary>
    public static class GoalkeeperDistribution
    {
        /// <summary>
        /// Computes the world-space ball release point for this distribution.
        /// Formula: gkPosition + Vector3(0, 0, releaseHeight), where releaseHeight depends on DeliveryKind.
        /// §3.8.1. Goalkeeper Mechanics #11 §3.8.
        /// </summary>
        /// <param name="gkPosition">Current GK world-space position. §3.8.1.</param>
        /// <param name="deliveryKind">Distribution type (height lookup — KD-1). §3.8.1.</param>
        /// <returns>World-space ball release point.</returns>
        public static Vector3 ComputeReleasePoint(Vector3 gkPosition, DeliveryKind deliveryKind)
        {
            float releaseHeight = LookupReleaseHeight(deliveryKind);
            return new Vector3(gkPosition.x, gkPosition.y, gkPosition.z + releaseHeight);
        }

        /// <summary>
        /// Returns the windup duration (ms) for the given delivery kind.
        /// §3.8.1. Goalkeeper Mechanics #11 §3.8.
        /// </summary>
        /// <param name="deliveryKind">Distribution type. §3.8.1.</param>
        /// <returns>Windup duration (ms).</returns>
        public static float ComputeWindupMs(DeliveryKind deliveryKind)
        {
            switch (deliveryKind)
            {
                case DeliveryKind.Throw: return GoalkeeperConstants.ThrowWindupMs;
                case DeliveryKind.Roll:  return GoalkeeperConstants.RollWindupMs;
                case DeliveryKind.Kick:  return GoalkeeperConstants.KickWindupMs;
                default:                 return GoalkeeperConstants.KickWindupMs;
            }
        }

        /// <summary>
        /// Computes the distribution accuracy coefficient = accuracyBase × attributeNorm.
        /// Roll always returns 1.0 (no attribute modifier per §3.8.1).
        /// §3.8.1. Goalkeeper Mechanics #11 §3.8.
        /// </summary>
        /// <param name="deliveryKind">Distribution type. §3.8.1.</param>
        /// <param name="attrs">GK agent attributes (Throwing_norm / Kicking_norm). §3.8.1.</param>
        /// <returns>Accuracy coefficient [0, 1]. Multiplied with powerIntent in §3.8.1.</returns>
        public static float ComputeAccuracyCoeff(DeliveryKind deliveryKind, GoalkeeperAgentAttributes attrs)
        {
            switch (deliveryKind)
            {
                case DeliveryKind.Throw: return GoalkeeperConstants.ThrowAccuracyCoeff * attrs.ThrowingNorm;
                case DeliveryKind.Roll:  return 1.0f;
                case DeliveryKind.Kick:  return GoalkeeperConstants.KickAccuracyCoeff  * attrs.KickingNorm;
                default:                 return 1.0f;
            }
        }

        /// <summary>
        /// Validates the DistributeIntent and applies F-05 / F-09 corrections.
        /// F-05: if TargetReceiverId is not in the agent roster, nullify it and keep last-known position.
        /// F-09: clamp TargetPoint to pitch bounds.
        /// Mutates the intent in-place. §3.8.2. Goalkeeper Mechanics #11 §3.8.
        /// </summary>
        /// <param name="intent">DistributeIntent to validate and possibly correct. §3.8.2.</param>
        /// <param name="agentRosterContains">True if TargetReceiverId is currently in the agent roster. §3.8.2.</param>
        /// <param name="receiverMissingWarningEmitted">Output: true if F-05 condition fired and telemetry warning should be emitted. §3.8.2.</param>
        /// <param name="targetPointClamped">Output: true if F-09 condition fired and TargetPoint was clamped. §3.8.2.</param>
        public static void ValidateTarget(
            ref DistributeIntent intent,
            bool agentRosterContains,
            out bool receiverMissingWarningEmitted,
            out bool targetPointClamped)
        {
            receiverMissingWarningEmitted = false;
            targetPointClamped            = false;

            // F-05: receiver substituted between commit and release
            if (intent.TargetReceiverId.HasValue && !agentRosterContains)
            {
                intent.TargetReceiverId       = null;
                receiverMissingWarningEmitted = true;
                // TargetPoint already holds last-known receiver position per §3.8.2
            }

            // F-09: TargetPoint outside pitch bounds → clamp
            float pitchLength = GoalkeeperConstants.PitchLengthM;
            float pitchWidth  = GoalkeeperConstants.PitchWidthM;

            float clampedX = Clamp(intent.TargetPoint.x, 0.0f, pitchLength);
            float clampedY = Clamp(intent.TargetPoint.y, 0.0f, pitchWidth);

            if (!Approx(clampedX, intent.TargetPoint.x) || !Approx(clampedY, intent.TargetPoint.y))
            {
                intent.TargetPoint  = new Vector3(clampedX, clampedY, intent.TargetPoint.z);
                targetPointClamped  = true;
            }
        }

        // ── Private helpers ──────────────────────────────────────────────────────────

        private static float LookupReleaseHeight(DeliveryKind deliveryKind)
        {
            switch (deliveryKind)
            {
                case DeliveryKind.Throw: return GoalkeeperConstants.ThrowReleaseHeightM;
                case DeliveryKind.Roll:  return GoalkeeperConstants.RollReleaseHeightM;
                case DeliveryKind.Kick:  return GoalkeeperConstants.KickReleaseHeightM;
                default:                 return GoalkeeperConstants.KickReleaseHeightM;
            }
        }

        private static float Clamp(float v, float min, float max)
        {
            return v < min ? min : v > max ? max : v;
        }

        private static bool Approx(float a, float b)
        {
            // Sub-millimetre position equality check; not a domain constant.
            return a == b;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
