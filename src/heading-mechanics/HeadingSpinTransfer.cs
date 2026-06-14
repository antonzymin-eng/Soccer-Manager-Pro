// File:     src/heading-mechanics/HeadingSpinTransfer.cs
// Created:  2026-05-28
// Modified: 2026-06-14
// Author:   —
// Spec:     Heading Mechanics #10 §3.6, KD-16, FR-HE-015, FR-HE-032, Code Standards #20
// Purpose:  headAngularVelocity derivation (FR-HE-032) and outgoing spin assembly (FM-010-004).

using UnityEngine;

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Head angular-velocity derivation from agent.facing finite-difference and outgoing spin assembly.
    /// FM-010-004: outgoingSpin = SPIN_TRANSFER_COEFF · headAngularVelocity + incomingSpin · spinPreservationFactor.
    /// KD-16: spin is Heading-owned; Ball Physics #1 receives the final spin via Ball.ApplyKick.
    /// FR-HE-032: headAngularVelocity derived locally — no AM #2 amendment required at Stage 0.
    /// Heading Mechanics #10 §3.6.
    /// </summary>
    public static class HeadingSpinTransfer
    {
        /// <summary>
        /// Computes the outgoing ball spin vector (rad/s).
        /// FM-010-004: outgoingSpin = SPIN_TRANSFER_COEFF · headAngularVelocity + incomingSpin · spinPreservationFactor.
        /// spinPreservationFactor = SPIN_PRESERVATION_BASE × (1 − contactPointAxialOffset / SPIN_TRANSFER_REVERSAL_THRESHOLD).
        /// When factor is negative the incoming spin is sign-reversed proportionally (v0.2 H-1; no double-reversal term).
        /// Heading Mechanics #10 §3.6.
        /// </summary>
        /// <param name="incomingSpin">Ball angular velocity at the contact frame (rad/s).</param>
        /// <param name="contactPointActual_headLocal">Actual contact point in head-local 2-D coordinates (m). x = facing-forward axis.</param>
        /// <param name="currentFacingDir">Agent facing direction at the contact frame (unit vector, XY plane).</param>
        /// <param name="prevFacingDir">Agent facing direction at the previous 60 Hz frame (unit vector, XY plane).</param>
        /// <returns>Outgoing spin vector (rad/s) in world space.</returns>
        public static Vector3 ComputeOutgoingSpin(
            Vector3 incomingSpin,
            Vector2 contactPointActual_headLocal,
            Vector2 currentFacingDir,
            Vector2 prevFacingDir)
        {
            Vector3 headAngularVelocity = DeriveHeadAngularVelocity(currentFacingDir, prevFacingDir);

            // NaN/Inf gate (AR-3 L-1): a non-finite incoming spin would propagate through the
            // preservation term into Ball.ApplyKick; treat it as zero (Step-0 sanitise pattern).
            if (!IsFinite(incomingSpin))
            {
                incomingSpin = Vector3.zero;
            }

            float contactPointAxialOffset = Mathf.Abs(contactPointActual_headLocal.x);

            float spinPreservationFactor = HeadingMechanicsConstants.SpinPreservationBase
                * (1.0f - contactPointAxialOffset
                        / HeadingMechanicsConstants.SpinTransferReversalThreshold);

            return HeadingMechanicsConstants.SpinTransferCoeff * headAngularVelocity
                 + incomingSpin * spinPreservationFactor;
        }

        /// <summary>
        /// Derives head angular velocity (rad/s) as a world-space Vector3 from agent.facing finite-difference.
        /// Stage 0 approximation: yaw-rate only (rotation around Z axis). FR-HE-032 / §3.6.
        /// At Stage 1+, retires to AM #2 skeletal API read when available (§7.9).
        /// </summary>
        /// <param name="currentFacingDir">Facing direction this frame (unit XY vector).</param>
        /// <param name="prevFacingDir">Facing direction previous frame (unit XY vector).</param>
        public static Vector3 DeriveHeadAngularVelocity(Vector2 currentFacingDir, Vector2 prevFacingDir)
        {
            if (currentFacingDir.sqrMagnitude < HeadingMechanicsConstants.DEGENERACY_EPSILON_SQ ||
                prevFacingDir.sqrMagnitude < HeadingMechanicsConstants.DEGENERACY_EPSILON_SQ)
            {
                return Vector3.zero;
            }

            float angleNow  = Mathf.Atan2(currentFacingDir.y, currentFacingDir.x);
            float anglePrev = Mathf.Atan2(prevFacingDir.y,    prevFacingDir.x);

            // Wrap delta to [-π, π] to avoid discontinuity at ±π boundary.
            float delta = angleNow - anglePrev;
            if (delta >  Mathf.PI) delta -= 2.0f * Mathf.PI;
            if (delta < -Mathf.PI) delta += 2.0f * Mathf.PI;

            float yawRateRadPerS = delta / HeadingMechanicsConstants.FrameS;

            return new Vector3(0.0f, 0.0f, yawRateRadPerS);
        }

        private static bool IsFinite(Vector3 v)
        {
            return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                              |
// | 1.1     | 2026-05-28 | —      | AR-1: 1e-6f literals → DEGENERACY_EPSILON_SQ constant.                          |
// | 1.2     | 2026-05-28 | —      | AR-2 M-2: FrameMs/1000.0f → FrameS.                                             |
// | 1.3     | 2026-06-14 | —      | AR-3 L-1: ComputeOutgoingSpin zeroes a non-finite incomingSpin before the        |
// |         |            |        | preservation term (Step-0 sanitise); new IsFinite helper.                        |
#endregion
