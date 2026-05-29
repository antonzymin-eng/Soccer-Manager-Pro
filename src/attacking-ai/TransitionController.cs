// File:     src/attacking-ai/TransitionController.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Attacking AI #15 §3.9, FR-AT-009, Code Standards #20
// Purpose:  SET-then-DECREMENT transition hold. On first possession-loss tick the counter is
//           SET to TRANSITION_HOLD_TICKS, then immediately DECREMENTED and the frozen last
//           directive is emitted. Subsequent ticks decrement and emit; when 0 emit empty.

using TacticalDirector.PositioningAI;

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Possession-loss transition controller. Pure static.
    /// SET-then-DECREMENT ordering (§3.9 step-order rationale): on tick T of possession loss,
    /// counter is set to TRANSITION_HOLD_TICKS, then decremented to TRANSITION_HOLD_TICKS−1
    /// and frozen directive emitted. Gives exactly TRANSITION_HOLD_TICKS frozen ticks total.
    /// The COUNTER_ATTACK profile sets TRANSITION_HOLD_TICKS = 0 → instant empty directive.
    /// Attacking AI #15 §3.9 (FR-AT-009).
    /// </summary>
    internal static class TransitionController
    {
        /// <summary>
        /// Evaluates the transition-hold countdown for non-IN_POSSESSION phases.
        /// Mutates <paramref name="state"/> (set and/or decrement). Returns either the frozen
        /// last-IN_POSSESSION directive or an empty directive.
        /// </summary>
        /// <param name="prevPhase">Phase from the previous tick.</param>
        /// <param name="currentPhase">Phase this tick (not IN_POSSESSION).</param>
        /// <param name="styleProfile">Active profile (provides TransitionHoldTicks).</param>
        /// <param name="lastInPossDirective">Last directive produced while IN_POSSESSION.</param>
        /// <param name="state">Per-team mutable countdown state; mutated here.</param>
        public static AttackDirective Evaluate(
            Phase                prevPhase,
            Phase                currentPhase,
            StyleProfile         styleProfile,
            AttackDirective      lastInPossDirective,
            ref TransitionHoldState state)
        {
            // Step 1: SET counter on the first tick of possession loss (§3.9 step 1).
            // Must occur BEFORE DECREMENT so the full hold window is preserved.
            if (prevPhase == Phase.InPoss && currentPhase != Phase.InPoss)
                state.TransitionHoldTick = styleProfile.TransitionHoldTicks;

            // Step 2: Decrement and emit (§3.9 step 2).
            if (state.TransitionHoldTick > 0)
            {
                state.TransitionHoldTick--;
                return lastInPossDirective; // frozen — no new runs triggered
            }

            return AttackDirective.Empty;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
