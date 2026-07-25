// File:     src/season-save/BoardObjective.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     Season & Competition Loop #30 §2.2, Appendix B row 11, FR-SN-014, KD-6;
//           Code Standards #20
// Purpose:  The board's literal Stage-0 season objective — "finish at or above position P".

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The board's season objective (#30 §2.2 / FR-SN-014). The Stage-0 surface is deliberately the
    /// single literal form "finish at or above position P" (§1.6) — authored as the identity a richer
    /// objective model (#45 Board &amp; Ownership) later extends, not as a throwaway.
    /// </summary>
    public readonly struct BoardObjective
    {
        /// <summary>The 1-based league position the club must finish at or above to pass.</summary>
        public readonly int TargetPositionOrBetter;

        /// <summary>
        /// Constructs an objective.
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="targetPositionOrBetter"/>
        /// is not positive (positions are 1-based).</exception>
        public BoardObjective(int targetPositionOrBetter)
        {
            if (targetPositionOrBetter <= 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(targetPositionOrBetter), targetPositionOrBetter,
                    "League positions are 1-based, so the target must be positive.");
            }

            TargetPositionOrBetter = targetPositionOrBetter;
        }

        /// <summary>
        /// Whether <paramref name="position"/> satisfies this objective (a lower position number is
        /// better). A pure predicate — evaluating it never mutates the objective (FR-SN-015).
        /// </summary>
        public bool IsMetBy(int position) => position <= TargetPositionOrBetter;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial implementation (#30 T0).                                   |
#endregion
