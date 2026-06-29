// File:     src/attacking-ai/OverloadDetector.cs
// Created:  2026-05-29
// Modified: 2026-06-29
// Author:   —
// Spec:     Attacking AI #15 §3.8, FR-AT-016, FR-AT-036, Code Standards #20; Tactical Instructions #21 §3.3 (FR-TI-021)
// Purpose:  Counts non-WEAK_SIDE agents in the Y-corridor around the ball. Sets overloadActive
//           and overloadFlank on the directive. WEAK_SIDE excluded: far-side agents must not
//           inflate the near-side count.

using UnityEngine;

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Overload-zone detector. Pure static.
    /// Counts non-WEAK_SIDE pool agents where |agentY − ballY| ≤ OVERLOAD_ZONE_WIDTH_M.
    /// Fires when count ≥ OVERLOAD_COUNT. Attacking AI #15 §3.8 (FR-AT-016 / FR-AT-036).
    /// </summary>
    internal static class OverloadDetector
    {
        /// <summary>
        /// Evaluates the overload condition and returns whether an overload is active.
        /// </summary>
        /// <param name="ballPosition">Ball position (X, Y) in metres.</param>
        /// <param name="pool">Attacking pool entries (roles already assigned).</param>
        /// <param name="poolCount">Number of valid entries.</param>
        /// <param name="overloadFlank">
        /// Flank where overload is occurring. Defined only when return value is true.
        /// </param>
        /// <returns>True when the overload condition is met (FR-AT-016).</returns>
        public static bool Evaluate(
            Vector2           ballPosition,
            AttackPoolEntry[] pool,
            int               poolCount,
            out Flank         overloadFlank)
            => Evaluate(ballPosition, pool, poolCount, preferredFlank: null, out overloadFlank);

        /// <summary>
        /// Overload evaluation with the #21 FocusPlay flank preference (FR-TI-021). When
        /// <paramref name="preferredFlank"/> matches the flank the ball is on, the trigger count is
        /// lowered by <see cref="AttackingAIConstants.OverloadFocusCountBias"/> so the team declares
        /// an overload more readily on the manager's chosen flank. A bias, not a gate: a null
        /// preference (Mixed / ThroughMiddle), or a ball on the non-preferred flank, leaves the
        /// threshold unchanged — so a default tactic is behaviour-neutral (FR-TI-031).
        /// </summary>
        /// <param name="preferredFlank">Manager-preferred attacking flank, or null for none.</param>
        public static bool Evaluate(
            Vector2           ballPosition,
            AttackPoolEntry[] pool,
            int               poolCount,
            Flank?            preferredFlank,
            out Flank         overloadFlank)
        {
            int count = 0;
            for (int i = 0; i < poolCount; i++)
            {
                ref AttackPoolEntry e = ref pool[i];
                if (e.AssignedRole == AttackRole.WeakSide) continue; // excluded per §3.8
                float dy = System.Math.Abs(e.Position.y - ballPosition.y);
                if (dy <= AttackingAIConstants.OverloadZoneWidthM) count++;
            }

            Flank ballFlank = (ballPosition.y >= AttackingAIConstants.PITCH_WIDTH_M * 0.5f)
                ? Flank.Right
                : Flank.Left;

            // FocusPlay bias: lower the threshold only when the manager prefers the ball-side flank.
            int requiredCount = AttackingAIConstants.OverloadCount;
            if (preferredFlank.HasValue && preferredFlank.Value == ballFlank)
                requiredCount -= AttackingAIConstants.OverloadFocusCountBias;

            if (count >= requiredCount)
            {
                overloadFlank = ballFlank;
                return true;
            }

            overloadFlank = default;
            return false;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-06-29 | —      | #21 FR-TI-021: + 5-arg Evaluate overload taking a FocusPlay-preferred Flank?; when it |
// |         |            |        |   matches the ball-side flank the trigger count drops by OverloadFocusCountBias. Null  |
// |         |            |        |   preference ⇒ unchanged (4-arg overload delegates with null) ⇒ behaviour-neutral.     |
#endregion
