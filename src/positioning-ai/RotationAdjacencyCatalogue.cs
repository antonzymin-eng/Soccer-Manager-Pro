// File:     src/positioning-ai/RotationAdjacencyCatalogue.cs
// Created:  2026-07-10
// Modified: 2026-07-10
// Author:   —
// Spec:     Positional Rotations #25 Appendix A ([GT] data), FR-RO-002/003/009/016, Code Standards #20
// Purpose:  The static per-FormationFamily rotation adjacency tables. Row order is commit
//           priority (FR-RO-009): flank exchanges rank above central ones.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Rotation adjacency catalogue (#25 Appendix A; [GT] data). Eligible pairs come exclusively
    /// from these tables (FR-RO-002); a pair absent here never rotates. Slot indices refer to the
    /// family's <c>PositioningAIConstants.Family*</c> table order. Each table is invariant-tested
    /// (FR-RO-016): GK-free, index-valid, distinct rows, ≤ ROTATION_MAX_PAIRS_PER_FAMILY. Rows are
    /// APPEND-only within a family — row order is both commit priority (FR-RO-009) and the
    /// FR-RO-013 serialization order for per-pair state.
    /// Pair values pinned per Appendix A v0.3 (F1 hand-audits recorded there); magnitudeless data,
    /// but membership itself is [GT] pending the balance pass.
    /// </summary>
    public static class RotationAdjacencyCatalogue
    {
        // Family442 slots: GK0, LB1, CB1 2, CB2 3, RB4, LM5, CM1 6, CM2 7, RM8, ST1 9, ST2 10.
        private static readonly RotationPair[] s_family442 = new RotationPair[]
        {
            new RotationPair(1, 5),   // row 0 — LB ↔ LM   (flank underlap/overlap exchange)
            new RotationPair(4, 8),   // row 1 — RB ↔ RM   (flank underlap/overlap exchange)
            new RotationPair(6, 7),   // row 2 — LCM ↔ RCM (pivot box rotation)
            new RotationPair(5, 9),   // row 3 — LM ↔ LST  (inside-forward drift exchange)
            new RotationPair(8, 10),  // row 4 — RM ↔ RST  (inside-forward drift exchange)
        };

        // Family433 slots: GK0, LB1, CB1 2, CB2 3, RB4, DM5, CM1 6, CM2 7, LW8, ST9, RW10.
        // The single pivot (DM, slot 5) is deliberately excluded from every pair (Appendix A.2 —
        // it anchors the rest-defence shape and has no like-for-like partner in this family).
        private static readonly RotationPair[] s_family433 = new RotationPair[]
        {
            new RotationPair(1, 8),   // row 0 — LB ↔ LW   (flank underlap/overlap exchange)
            new RotationPair(4, 10),  // row 1 — RB ↔ RW   (flank underlap/overlap exchange)
            new RotationPair(6, 7),   // row 2 — CM1 ↔ CM2 (interior-eight box rotation)
            new RotationPair(8, 9),   // row 3 — LW ↔ ST   (wide-forward / false-nine drift exchange)
            new RotationPair(9, 10),  // row 4 — RW ↔ ST   (wide-forward / false-nine drift exchange)
        };

        // Family4231 slots: GK0, LB1, CB1 2, CB2 3, RB4, DM1 5, DM2 6, LAM7, CAM8, RAM9, ST10.
        private static readonly RotationPair[] s_family4231 = new RotationPair[]
        {
            new RotationPair(1, 7),   // row 0 — LB ↔ LAM  (flank underlap/overlap exchange)
            new RotationPair(4, 9),   // row 1 — RB ↔ RAM  (flank underlap/overlap exchange)
            new RotationPair(5, 6),   // row 2 — DM1 ↔ DM2 (double-pivot box rotation)
            new RotationPair(8, 10),  // row 3 — CAM ↔ ST  (ten–nine vertical exchange)
            new RotationPair(7, 8),   // row 4 — LAM ↔ CAM (attacking-band interchange)
            new RotationPair(8, 9),   // row 5 — RAM ↔ CAM (attacking-band interchange)
        };

        private static readonly ReadOnlyCollection<RotationPair> s_family442Ro =
            new ReadOnlyCollection<RotationPair>(s_family442);
        private static readonly ReadOnlyCollection<RotationPair> s_family433Ro =
            new ReadOnlyCollection<RotationPair>(s_family433);
        private static readonly ReadOnlyCollection<RotationPair> s_family4231Ro =
            new ReadOnlyCollection<RotationPair>(s_family4231);

        /// <summary>
        /// The family's adjacency table in commit-priority row order (FR-RO-009). Read-only wrapper
        /// — callers cannot cast back to the mutable array (the #18/#19 AR-1 L-3 pattern).
        /// </summary>
        /// <param name="family">The formation family whose table to fetch.</param>
        public static IReadOnlyList<RotationPair> GetPairs(FormationFamily family)
        {
            return family switch
            {
                FormationFamily.F433 => s_family433Ro,
                FormationFamily.F4231 => s_family4231Ro,
                _ => s_family442Ro,
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                   |
// | 1.0     | 2026-07-10 | —      | Initial implementation (#25 T0 scaffolding): all three Appendix A v0.3 tables. |
#endregion
