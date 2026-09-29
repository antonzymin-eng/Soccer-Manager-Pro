// ============================================================================
// File:     src/club-finances/BoardModifier.cs
// Created:  2026-09-04
// Modified: 2026-09-08
// Author:   —
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #40 KD-4, FR-FN-018 (board identity routing seam)
// Purpose:  Declares the board budget multiplier consumed by settlement without creating a #45 interface.
// ============================================================================

namespace TacticalDirector.ClubFinances
{
    /// <summary>Per-mille board multiplier; <see cref="Identity"/> is the Stage-2 neutral value.</summary>
    public readonly struct BoardModifier
    {
        /// <summary>Budget multiplier in per-mille units; non-positive values are invalid at the settlement seam.</summary>
        public readonly int BudgetMultiplierMillPermille;

        /// <summary>Creates a board modifier with the supplied per-mille multiplier.</summary>
        public BoardModifier(int budgetMultiplierMillPermille)
        {
            BudgetMultiplierMillPermille = budgetMultiplierMillPermille;
        }

        /// <summary>Explicit ×1.0 identity; unlike <c>default(BoardModifier)</c>, this is valid.</summary>
        public static BoardModifier Identity =>
            new BoardModifier(ClubFinancesConstants.BOARD_MODIFIER_IDENTITY_PERMILLE);
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-04 | —      | Initial #40 T0 board routing seam. |
// | 1.1     | 2026-09-06 | —      | Header author attribution corrected to automated-agent placeholder. |
// | 1.2     | 2026-09-07 | —      | Documented the F4 positive-multiplier runtime contract. |
// | 1.3     | 2026-09-08 | —      | Corrected the version-history table to the required parseable pipe-row format. |
#endregion
