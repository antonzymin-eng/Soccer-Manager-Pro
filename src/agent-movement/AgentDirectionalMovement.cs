// File:     src/agent-movement/AgentDirectionalMovement.cs
// Created:  2026-05-22
// Modified: 2026-06-03
// Author:   —
// Spec:     Agent Movement #2 §3.3, Code Standards #20
// Purpose:  Directional speed multipliers, facing direction updates, and velocity-direction rotation.

using UnityEngine;

namespace TacticalDirector.AgentMovement
{
    /// <summary>
    /// Computes directional penalties based on the angle between movement direction
    /// and facing direction. Static, no side effects. Agent Movement #2 §3.3.
    /// </summary>
    public static class AgentDirectionalMovement
    {
        /// <summary>
        /// Lateral speed multiplier at a given Agility attribute level.
        /// Formula: LateralMultMin + (agility - 1) × PER_POINT. Agent Movement #2 §3.3.2.
        /// </summary>
        public static float LateralMultiplier(float effectiveAgility)
        {
            float t = Mathf.Clamp(effectiveAgility - PlayerAttributeConstants.AttributeMin, 0.0f, PlayerAttributeConstants.AttributeRangeSpan);
            return DirectionalConstants.LateralMultMin
                 + t * DirectionalConstants.LateralMultPerAgilityPoint;
        }

        /// <summary>
        /// Backward speed multiplier at a given Agility attribute level.
        /// Formula: BackwardMultMin + (agility - 1) × PER_POINT. Agent Movement #2 §3.3.2.
        /// </summary>
        public static float BackwardMultiplier(float effectiveAgility)
        {
            float t = Mathf.Clamp(effectiveAgility - PlayerAttributeConstants.AttributeMin, 0.0f, PlayerAttributeConstants.AttributeRangeSpan);
            return DirectionalConstants.BackwardMultMin
                 + t * DirectionalConstants.BackwardMultPerAgilityPoint;
        }

        /// <summary>
        /// Blended directional speed multiplier for a movement angle vs facing angle.
        /// angleDeg: unsigned angle [0, 180] between movement direction and facing direction.
        /// Agent Movement #2 §3.3.3.
        /// </summary>
        public static float CalculateDirectionalMultiplier(float angleDeg, float effectiveAgility)
        {
            float lat = LateralMultiplier(effectiveAgility);
            float bwd = BackwardMultiplier(effectiveAgility);

            float hys      = DirectionalConstants.ZONE_HYSTERESIS;
            float fwdMax   = DirectionalConstants.FORWARD_ZONE_MAX;
            float latStart = DirectionalConstants.LATERAL_ZONE_START;
            float latEnd   = DirectionalConstants.LATERAL_ZONE_END;
            float bwdStart = DirectionalConstants.BACKWARD_ZONE_START;

            // Hysteresis extends the pure-forward zone and compresses the backward-blend entry,
            // reducing frame-to-frame multiplier flicker near boundary angles.
            float fwdBlendStart = fwdMax + hys;   // 33°: pure forward up to here
            float bwdBlendEnd   = bwdStart - hys; // 87°: pure backward begins here

            if (angleDeg <= fwdBlendStart)
            {
                return 1.0f;
            }

            if (angleDeg <= latStart)
            {
                float width = latStart - fwdBlendStart;
                float t = width > 0.0f ? (angleDeg - fwdBlendStart) / width : 1.0f;
                return Mathf.Lerp(1.0f, lat, t);
            }

            if (angleDeg <= latEnd)
            {
                return lat;
            }

            if (angleDeg <= bwdBlendEnd)
            {
                float width = bwdBlendEnd - latEnd;
                float t = width > 0.0f ? (angleDeg - latEnd) / width : 1.0f;
                return Mathf.Lerp(lat, bwd, t);
            }

            return bwd;
        }

        /// <summary>
        /// Applies the directional multiplier to the acceleration k value (square-root scaling).
        /// k_directional = k_base × sqrt(mult). Agent Movement #2 §3.3.5.
        /// </summary>
        public static float ApplyDirectionalToAccelK(float kBase, float mult)
        {
            return kBase * Mathf.Sqrt(Mathf.Max(mult, 0.0f));
        }

        /// <summary>
        /// Unsigned angle (degrees) between a movement direction and a facing direction.
        /// Both vectors must be in the XY plane (Z ignored). Agent Movement #2 §3.3.3.
        /// </summary>
        public static float MovementAngleDeg(Vector2 movementDir, Vector2 facingDir)
        {
            if (movementDir.sqrMagnitude < SafetyConstants.VELOCITY_SQR_MAGNITUDE_EPSILON || facingDir.sqrMagnitude < SafetyConstants.VELOCITY_SQR_MAGNITUDE_EPSILON)
            {
                return 0.0f;
            }

            float dot = Vector2.Dot(movementDir.normalized, facingDir.normalized);
            dot = Mathf.Clamp(dot, -1.0f, 1.0f);
            return Mathf.Acos(dot) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Rotates the facing direction toward the target direction by at most maxTurnDeg degrees.
        /// Uses signed angle to pick the shortest rotation path. Agent Movement #2 §3.3.4.
        /// signedAngleApplied: the signed rotation actually applied this call (degrees). Caller
        /// uses |value|/dt as the achieved turn rate and the sign for left/right lean direction.
        /// </summary>
        public static Vector2 RotateFacingToward(
            Vector2 currentFacing, Vector2 targetFacing, float maxTurnDeg, out float signedAngleApplied)
        {
            if (targetFacing.sqrMagnitude < SafetyConstants.VELOCITY_SQR_MAGNITUDE_EPSILON)
            {
                signedAngleApplied = 0.0f;
                return currentFacing;
            }

            Vector2 target = targetFacing.normalized;
            float signedAngle = Vector2.SignedAngle(currentFacing, target);
            float clampedAngle = Mathf.Clamp(signedAngle, -maxTurnDeg, maxTurnDeg);
            signedAngleApplied = clampedAngle;

            float rad = clampedAngle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            return new Vector2(
                currentFacing.x * cos - currentFacing.y * sin,
                currentFacing.x * sin + currentFacing.y * cos).normalized;
        }

        /// <summary>
        /// Rotates velocity direction toward targetDirection at the same rate cap as facing, then
        /// rescales to newSpeed. Momentum-respecting: prevents instantaneous velocity-direction
        /// changes that the prior implementation produced by assigning targetDir × newSpeed each
        /// frame. Agent Movement #2 §3.3.4, §4.4.1 step 8.
        ///
        /// From rest (|currentVelocity|² &lt; epsilon): jump-starts in targetDirection × newSpeed.
        /// Zero/degenerate target: returns currentDirection × newSpeed (maintains momentum).
        /// Caller is responsible for passing maxTurnDeg = 0 when steering is non-voluntary
        /// (STUMBLING / GROUNDED / IDLE-stop) — this function performs no state-machine carve-out.
        /// </summary>
        public static Vector2 RotateVelocityToward(
            Vector2 currentVelocity, Vector2 targetDirection, float newSpeed, float maxTurnDeg)
        {
            if (newSpeed <= MovementThresholds.MIN_VELOCITY_MAGNITUDE)
            {
                return Vector2.zero;
            }

            bool hasCurrentDir = currentVelocity.sqrMagnitude >= SafetyConstants.VELOCITY_SQR_MAGNITUDE_EPSILON;
            bool hasTargetDir = targetDirection.sqrMagnitude >= SafetyConstants.VELOCITY_SQR_MAGNITUDE_EPSILON;

            if (!hasCurrentDir)
            {
                Vector2 startDir = hasTargetDir ? targetDirection.normalized : Vector2.up;
                return startDir * newSpeed;
            }

            Vector2 currentDir = currentVelocity.normalized;

            if (!hasTargetDir || maxTurnDeg <= 0.0f)
            {
                return currentDir * newSpeed;
            }

            Vector2 targetDir = targetDirection.normalized;
            float signedAngle = Vector2.SignedAngle(currentDir, targetDir);
            float clampedAngle = Mathf.Clamp(signedAngle, -maxTurnDeg, maxTurnDeg);

            float rad = clampedAngle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            Vector2 newDir = new Vector2(
                currentDir.x * cos - currentDir.y * sin,
                currentDir.x * sin + currentDir.y * cos).normalized;

            return newDir * newSpeed;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                              |
// | 1.0     | 2026-05-22 | —      | Initial implementation.                                                            |
// | 1.1     | 2026-05-25 | —      | Pass-1: H-2 namespace; L-1 PascalCase refs.                                        |
// | 1.2     | 2026-05-25 | —      | Pass-3: 19.0f/1.0f attribute literals → PlayerAttributeConstants.AttributeRangeSpan / AttributeMin. |
// | 1.3     | 2026-05-25 | —      | Pass-4 fix: L-1 1e-6f literals → SafetyConstants.VELOCITY_SQR_MAGNITUDE_EPSILON [FIXED].       |
// | 1.4     | 2026-05-26 | —      | AR-2 fix: M-1 ZONE_HYSTERESIS consumed in CalculateDirectionalMultiplier; extends              |
// |         |            |        | pure-forward zone by 3° and compresses backward-blend entry by 3°, reducing boundary          |
// |         |            |        | flicker. Degenerate-blend guards (width > 0 check) prevent division by zero if                 |
// |         |            |        | zone constants are adjusted.                                                                   |
// | 1.5     | 2026-06-03 | —      | AR-4 fix: H-2/H-3 RotateVelocityToward added (rate-limited velocity-direction rotation,        |
// |         |            |        | analogous to RotateFacingToward). Closes the momentum-instability defect in System Step 8.    |
// |         |            |        | H-4/L-1 RotateFacingToward gained out signedAngleApplied so caller computes achieved turn     |
// |         |            |        | rate and signed lean direction.                                                                |
#endregion
