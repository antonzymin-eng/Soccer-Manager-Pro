// File: src/positioning-ai/AnchorCalculator.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec: #12 Positioning AI §3.1, §3.2, §3.3.3
// Purpose: Computes formation anchor positions and ball-relative slot offsets, including GK dedicated formula.

using UnityEngine;

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Computes:
    ///   (a) Formation anchor: pitchSize × formationOffset[role] — §3.1.
    ///   (b) Ball-relative offset: piecewise-linear basis × pullFactor × range — §3.2.
    ///   (c) GK dedicated slot — §3.3.3.
    /// All methods are pure static functions; no instance state.
    /// </summary>
    public static class AnchorCalculator
    {
        // ── (a) Formation anchor ──────────────────────────────────────────────

        /// <summary>
        /// Computes the formation anchor for a single outfield slot.
        /// anchor.x = PITCH_LENGTH_M × formationOffset.LongPct
        /// anchor.y = PITCH_WIDTH_M  × formationOffset.LateralPct
        /// Worked example §3.1.2: 4-3-3 LW (longPct=0.743, lateralPct=0.100) → (78.015, 6.800).
        /// </summary>
        public static Vector2 ComputeAnchor(in FormationSlotRecord slot)
        {
            return new Vector2(
                PositioningAIConstants.PITCH_LENGTH_M * slot.LongPct,
                PositioningAIConstants.PITCH_WIDTH_M  * slot.LateralPct);
        }

        // ── (b) Ball-relative offset ──────────────────────────────────────────

        /// <summary>
        /// Computes the signed ball-relative offset for a single outfield agent.
        /// Piecewise-linear basis ∈ [-1,+1] maps ball position relative to pitch centre.
        /// offset.x = pullFactor.x × basisX × OFFSET_RANGE_X_M
        /// offset.y = pullFactor.y × basisY × OFFSET_RANGE_Y_M
        ///
        /// Worked example §3.2.2: ball=(20,34), AM, OutOfPoss, pullFactor=(0.60,0.10)
        ///   basisX = (20−52.5)/52.5 = −0.619
        ///   offset.x = 0.60×(−0.619)×12.0 = −4.46 m  (AM retreats when ball is deep)
        ///   offset.y = 0.10×0.0×8.0 = 0.0 m
        /// </summary>
        public static Vector2 ComputeBallRelativeOffset(Vector3 ballWorld, RoleId role, Phase phase)
        {
            Vector2 pull = PositioningAIConstants.GetPullFactor(role, phase);
            float basisX = ComputeBasis(ballWorld.x, PositioningAIConstants.PITCH_HALF_LENGTH_M);
            float basisY = ComputeBasis(ballWorld.y, PositioningAIConstants.PITCH_HALF_WIDTH_M);

            return new Vector2(
                pull.x * basisX * PositioningAIConstants.OFFSET_RANGE_X_M,
                pull.y * basisY * PositioningAIConstants.OFFSET_RANGE_Y_M);
        }

        // ── (c) GK dedicated slot ─────────────────────────────────────────────

        /// <summary>
        /// Computes the goalkeeper formation slot via the dedicated §3.3.3 formula.
        /// gkSlot.x = GK_DEPTH_M + GK_ADVANCE_FACTOR × basisX(ball.x_clamped)
        /// gkSlot.y = PITCH_WIDTH_M/2 + GK_LATERAL_FACTOR × basisY(ball.y)
        ///
        /// ball.x is clamped to own half [0, PITCH_HALF_LENGTH_M] before computing basisX
        /// to prevent GK advancing past halfway when ball is in opponent half.
        /// </summary>
        public static Vector2 ComputeGkSlot(Vector3 ballWorld)
        {
            float ballXClamped = Mathf.Clamp(ballWorld.x, 0f, PositioningAIConstants.PITCH_HALF_LENGTH_M);
            float basisX = ComputeBasis(ballXClamped, PositioningAIConstants.PITCH_HALF_LENGTH_M);
            float basisY = ComputeBasis(ballWorld.y,  PositioningAIConstants.PITCH_HALF_WIDTH_M);

            return new Vector2(
                PositioningAIConstants.GK_DEPTH_M  + PositioningAIConstants.GK_ADVANCE_FACTOR * basisX,
                PositioningAIConstants.PITCH_HALF_WIDTH_M + PositioningAIConstants.GK_LATERAL_FACTOR * basisY);
        }

        // ── Shared helper ─────────────────────────────────────────────────────

        /// <summary>
        /// Maps a world position to a signed basis value ∈ [-1,+1].
        /// basis = (worldPos − halfRange) / halfRange
        /// centre → 0, minimum → -1, maximum → +1.
        /// </summary>
        private static float ComputeBasis(float worldPos, float halfRange)
        {
            return (worldPos - halfRange) / halfRange;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
