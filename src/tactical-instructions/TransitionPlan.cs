// File:     src/tactical-instructions/TransitionPlan.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.1, §3.2, §3.4, Code Standards #20
// Purpose:  Transition intent on winning or losing the ball. Overrides ONLY the
//           transition dimension of the Mentality-selected StyleProfile
//           (TransitionHoldTicks) and the #13 counter-press gate (FR-TI-020).

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Transition plan (FR-TI-020). One enum carries both the TransitionWon and TransitionLost
    /// fields of <see cref="TeamTactic"/>. Composes with <see cref="Mentality"/>: it overrides only
    /// <c>StyleProfile.TransitionHoldTicks</c> and the #13 counter-press gate, never the profile's
    /// other multipliers (§3.2). The <see cref="TeamTactic.Balanced"/> identity uses
    /// <see cref="HoldShape"/> (won) and <see cref="Regroup"/> (lost).
    /// ORDINAL STABILITY: byte-backed, APPEND-only (FR-TI-007). Append at the end only.
    /// </summary>
    public enum TransitionPlan : byte
    {
        /// <summary>On winning the ball: break forward immediately. §3.4.</summary>
        CounterAttack = 0,

        /// <summary>On winning the ball: retain shape and keep possession (won-side identity). §3.4.</summary>
        HoldShape = 1,

        /// <summary>On losing the ball: counter-press to win it back at once. §3.4.</summary>
        CounterPress = 2,

        /// <summary>On losing the ball: drop and regroup into shape (lost-side identity). §3.4.</summary>
        Regroup = 3
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).   |
#endregion
