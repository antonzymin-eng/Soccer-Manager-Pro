// File:     src/attacking-ai/TransitionHoldState.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Attacking AI #15 §2.2.5, §3.9, FR-AT-009, Code Standards #20
// Purpose:  Per-team countdown state after a possession-loss event. Carries prevPhase so that
//           TransitionController can detect the first tick of a possession-loss transition.

using TacticalDirector.PositioningAI;

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Per-team transition-hold state. Mutable authoritative simulation state
    /// (FR-AT-004 / #16 §3.2). Attacking AI #15 §2.2.5 / §3.9.
    ///
    /// <para><see cref="TransitionHoldTick"/> is set to TRANSITION_HOLD_TICKS on the first
    /// possession-loss tick, decremented each subsequent tick, and reset to 0 when
    /// IN_POSSESSION resumes. The COUNTER_ATTACK profile sets TRANSITION_HOLD_TICKS = 0,
    /// so the set-then-decrement logic immediately emits an empty directive.</para>
    /// </summary>
    public struct TransitionHoldState
    {
        /// <summary>
        /// Countdown ticks remaining; SET on possession loss, DECREMENTED each tick,
        /// RESET to 0 when IN_POSSESSION resumes. Value 0 = no hold active.
        /// </summary>
        public int   TransitionHoldTick;

        /// <summary>
        /// Team phase from the previous tick. Written at the end of every tick
        /// and read at the start of the next to detect possession-loss transitions.
        /// </summary>
        public Phase PrevPhase;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
