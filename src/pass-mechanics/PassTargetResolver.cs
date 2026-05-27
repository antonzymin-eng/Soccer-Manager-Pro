// File:     src/pass-mechanics/PassTargetResolver.cs
// Created:  2026-05-26
// Modified: 2026-05-26
// Author:   —
// Spec:     Pass Mechanics #5 §3.6, Code Standards #20
// Purpose:  Pure static resolver for aim points, through-ball lead projection,
//           pitch boundary clamping, and error-vector application. All methods
//           deterministic. Coordinate system: Z-up (X=pitch length, Y=pitch width).

using UnityEngine;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Resolves pass aim points and applies deterministic error to the kick direction.
    /// Pure static — no state, no side effects. Pass Mechanics #5 §3.6.
    /// </summary>
    internal static class PassTargetResolver
    {
        // ── §3.6.3 / §3.6.4 — Aim Point Resolution ──────────────────────────────────

        /// <summary>
        /// Resolves the pre-error aim point for a player-targeted pass.
        /// Aim point is the target agent's current XY position. §3.6.3.
        /// </summary>
        /// <param name="receiverPosition">Target agent's XY position (metres).</param>
        /// <returns>Aim point as a Vector3 with Z = 0 (ground level).</returns>
        public static Vector3 ResolvePlayerTargetedAimPoint(Vector2 receiverPosition)
        {
            return new Vector3(receiverPosition.x, receiverPosition.y, 0f);
        }

        /// <summary>
        /// Resolves the pre-error aim point for a space-targeted pass from an explicit
        /// target position. §3.6.4 Path A.
        /// </summary>
        /// <param name="targetPosition">Explicit space target from PassRequest.</param>
        /// <returns>Aim point (may have Z > 0 if caller wants aerial target).</returns>
        public static Vector3 ResolveSpaceTargetedAimPoint(Vector3 targetPosition)
        {
            return targetPosition;
        }

        // ── §3.6.5 — Through-Ball Lead Distance ────────────────────────────────────────

        /// <summary>
        /// Computes the through-ball aim point using linear receiver projection.
        /// Stage 0 simplification — ignores receiver acceleration, ball deceleration,
        /// and non-linear receiver paths (KD-4, §7.1 Stage 1 upgrade point).
        /// §3.6.5.
        /// </summary>
        /// <param name="passerPosition">XY position of the passing agent.</param>
        /// <param name="receiverPosition">XY position of the target agent now.</param>
        /// <param name="receiverVelocity">XY velocity of the target agent (m/s).</param>
        /// <param name="kickSpeed">Kick speed from §3.2 (m/s) for time-of-flight estimate.</param>
        /// <param name="leadDistance">Output: distance projected ahead of receiver (metres).</param>
        /// <returns>Projected aim point as Vector3 with Z = 0.</returns>
        public static Vector3 ComputeThroughBallAimPoint(
            Vector2 passerPosition,
            Vector2 receiverPosition,
            Vector2 receiverVelocity,
            float kickSpeed,
            out float leadDistance)
        {
            if (receiverVelocity.magnitude < PassMechanicsConstants.VThresholdStationary)
            {
                leadDistance = 0f;
                return new Vector3(receiverPosition.x, receiverPosition.y, 0f);
            }

            float dInitial = Vector2.Distance(passerPosition, receiverPosition);
            float tFlight  = dInitial / Mathf.Max(kickSpeed, 0.001f);

            Vector2 projected = receiverPosition + receiverVelocity * tFlight;
            leadDistance      = (projected - receiverPosition).magnitude;

            return new Vector3(projected.x, projected.y, 0f);
        }

        // ── §3.6.6 — Pitch Boundary Clamping ────────────────────────────────────────

        /// <summary>
        /// Clamps the aim point to pitch bounds. Z-up: X = pitch length [0, 105m],
        /// Y = pitch width [0, 68m]. Z (height) is not clamped. §3.6.6.
        /// </summary>
        public static Vector3 ClampToPitchBounds(Vector3 aimPoint)
        {
            Vector3 original = aimPoint;
            aimPoint.x = Mathf.Clamp(aimPoint.x, 0f, PassMechanicsConstants.PitchLength);
            aimPoint.y = Mathf.Clamp(aimPoint.y, 0f, PassMechanicsConstants.PitchWidth);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (!Mathf.Approximately(aimPoint.x, original.x) || !Mathf.Approximately(aimPoint.y, original.y))
                Debug.LogWarning($"[TargetResolver] Aim point clamped to pitch bounds: original={original}, clamped={aimPoint}");
#endif

            return aimPoint;
        }

        // ── §3.6.7 — Error Application ───────────────────────────────────────────────

        /// <summary>
        /// Applies the deterministic error rotation to the kick direction.
        /// Rotates kickDirection by errorAngleDeg × sin(errorDirectionRad) in the
        /// horizontal plane (XY) around the Z axis (vertical in Z-up coordinate system).
        /// The spec note in §3.6.7 permits implementation variation; this form is
        /// deterministic and produces the correct mean angular deviation. §3.6.7.
        /// </summary>
        /// <param name="kickDirection">Normalised horizontal direction (Z must be 0).</param>
        /// <param name="errorAngleDeg">Error magnitude in degrees from §3.5.</param>
        /// <param name="errorDirectionRad">Hash-determined error direction [0, 2π) from §3.5.7.</param>
        /// <returns>Normalised kick direction after error applied.</returns>
        public static Vector3 ApplyErrorToDirection(
            Vector3 kickDirection,
            float errorAngleDeg,
            float errorDirectionRad)
        {
            // Rotate around Z axis (vertical in Z-up) by errorAngleDeg * sin(errorDirectionRad)
            // degrees. sin maps [0,2π) to [-1,+1], distributing error direction uniformly.
            float rotDeg = Mathf.Sin(errorDirectionRad) * errorAngleDeg;
            Quaternion errorRotation = Quaternion.Euler(0f, 0f, rotDeg);
            return (errorRotation * kickDirection).normalized;
        }

        // ── Derived Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Computes the horizontal kick direction (Z=0) from passer to aim point.
        /// </summary>
        public static Vector3 ComputeKickDirection(Vector2 passerPosition, Vector3 aimPoint)
        {
            float dx = aimPoint.x - passerPosition.x;
            float dy = aimPoint.y - passerPosition.y;
            Vector2 dir2d = new Vector2(dx, dy);

            if (dir2d.sqrMagnitude < 0.0001f)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogWarning("[TargetResolver] Passer and aim point are nearly coincident. Using +X as fallback.");
#endif
                return Vector3.right;
            }

            dir2d.Normalize();
            return new Vector3(dir2d.x, dir2d.y, 0f);
        }

        /// <summary>
        /// Computes the body angle (degrees) between FacingDirection and kick direction.
        /// Clamped to [0°, 90°] as required by OrientationModifier (§3.5.4).
        /// </summary>
        public static float ComputeBodyAngle(Vector2 facingDirection, Vector3 kickDirection)
        {
            Vector2 kickDir2d = new Vector2(kickDirection.x, kickDirection.y);

            if (facingDirection.sqrMagnitude < 0.0001f || kickDir2d.sqrMagnitude < 0.0001f)
                return 0f;

            float dot = Vector2.Dot(facingDirection.normalized, kickDir2d.normalized);
            dot = Mathf.Clamp(dot, -1f, 1f);
            float angleDeg = Mathf.Acos(dot) * Mathf.Rad2Deg;
            return Mathf.Clamp(angleDeg, 0f, 90f);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-26 | —      | Initial implementation.                                            |
// | 1.1     | 2026-05-26 | —      | L6: ClampToPitchBounds log level Debug.Log → Debug.LogWarning.    |
// | 1.2     | 2026-05-27 | —      | AR-1 M-1: class changed public → internal (implementation detail). |
// |         |            |        | AR-1 M-2: diagnostic Debug.LogWarning calls guarded by             |
// |         |            |        |     #if DEVELOPMENT_BUILD || UNITY_EDITOR (FR-CS-027-034).         |
#endregion
