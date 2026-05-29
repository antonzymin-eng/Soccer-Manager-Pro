// File:     src/defensive-ai/OffsideLineState.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Defensive AI #14 §2.2.5, §3.7, Code Standards #20
// Purpose:  Per-team offside-trap state maintained across ticks. Tracks the step-up
//           trigger dwell counter, post-trap cooldown, and GK-cover active tick count.

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Per-team mutable state for the offside trap and GK-cover override (§3.7 / §3.9).
    /// Maintained across ticks; contributes to the per-tick determinism digest
    /// (FR-DA-004 / #16 §6.2 / KD-10). Defensive AI #14 §2.2.5.
    ///
    /// <c>CurrentLineDepth</c> mirrors Positioning AI #12 <c>DefensiveLineDepth</c> each tick;
    /// #14 never computes this value (FR-DA-012).
    /// </summary>
    public struct OffsideLineState
    {
        /// <summary>
        /// Current effective defensive line depth (distToOwnGoal, m).
        /// Updated each tick from Positioning AI #12 DefensiveLineDepth read-only.
        /// </summary>
        public float CurrentLineDepth;

        /// <summary>
        /// Consecutive ticks all four offside-trap trigger conditions have been satisfied.
        /// Increments while all conditions hold; resets to 0 on any failure (§3.7.3).
        /// Trap fires when this reaches OffsideTrapDwellTicks.
        /// </summary>
        public int StepUpDwellCounter;

        /// <summary>
        /// Post-trap cooldown ticks remaining. While &gt; 0 the trap cannot re-fire.
        /// Set to OffsideResetCooldownTicks when a trap executes; decrements each tick (§3.7.3).
        /// </summary>
        public int CooldownTicksRemaining;

        /// <summary>
        /// Consecutive ticks the COVER_GK_ZONE emergency override has been active (§3.9.2).
        /// Resets to 0 when GK returns to zone or when CoverGkZoneMaxTicks is reached.
        /// </summary>
        public int CoverGkZoneActiveTicks;

        /// <summary>Returns a zeroed initial state.</summary>
        public static OffsideLineState Default()
        {
            return new OffsideLineState
            {
                CurrentLineDepth      = 0f,
                StepUpDwellCounter    = 0,
                CooldownTicksRemaining = 0,
                CoverGkZoneActiveTicks = 0,
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
