// ============================================================================
// File:     src/club-finances/ClubFinancesConstants.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   Codex / Anton
// Specs:    Spec #20 §3.2.3, §3.6.2 (constant catalogue, GT loading, style/docs)
//           Spec #40 Appendix A, §3.1, §4.4 (Club Finances constants/save framing)
// Purpose:  Declares the integer-only fixed and tunable constants used by the minimal finance model.
//           The reserved RNG namespace remains code-free until #40 T3's first real draw.
// ============================================================================

using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

namespace TacticalDirector.ClubFinances
{
    /// <summary>Constant catalogue for Club Finances &amp; Economy #40.</summary>
    public static class ClubFinancesConstants
    {
        #region Fixed

        /// <summary>[FIXED] Shared per-mille denominator for board and budget-share arithmetic. Spec #40 Appendix A.</summary>
        public const int PERMILLE_DENOM = 1000;

        /// <summary>[FIXED] Identity value for <see cref="BoardModifier.BudgetMultiplierMillPermille"/>. Spec #40 Appendix A.</summary>
        public const int BOARD_MODIFIER_IDENTITY_PERMILLE = 1000;

        /// <summary>[FIXED] Self-identifying #40 save-block magic (<c>"FNCE"</c>). Spec #40 §4.4.</summary>
        public const uint FINANCE_SAVE_MAGIC = 0x464E4345;

        /// <summary>[FIXED] #40 finance sub-blob format version. Spec #40 FR-FN-020 / §4.4.</summary>
        public const uint FINANCE_SAVE_FORMAT_VERSION = 1;

        /// <summary>[FIXED] Finance block framing bytes: magic + version + record count. Spec #40 §4.4.</summary>
        public const int FINANCE_SAVE_HEADER_BYTES = 12;

        /// <summary>[FIXED] One persisted finance record: i32 ClubId + six i64 finance fields. Spec #40 §4.4.</summary>
        public const int FINANCE_SAVE_RECORD_BYTES = 52;

        #endregion

        #region GT

        /// <summary>[GT] New-club starting cash balance. Config key [club-finances] StartingClubBalance. Spec #40 Appendix A.</summary>
        public static readonly long StartingClubBalance = Config.GetInt("club-finances", "StartingClubBalance", 500_000);

        /// <summary>[GT] Prize money for position 1. Config key [club-finances] PrizeMoneyWinner. Spec #40 Appendix A.</summary>
        public static readonly long PrizeMoneyWinner = Config.GetInt("club-finances", "PrizeMoneyWinner", 2_000_000);

        /// <summary>[GT] Prize money for last place. Config key [club-finances] PrizeMoneyLastPlace. Spec #40 Appendix A.</summary>
        public static readonly long PrizeMoneyLastPlace = Config.GetInt("club-finances", "PrizeMoneyLastPlace", 200_000);

        /// <summary>[GT] Flat transfer-budget allocation before prize-money share. Config key [club-finances] BaseTransferBudget. Spec #40 Appendix A.</summary>
        public static readonly long BaseTransferBudget = Config.GetInt("club-finances", "BaseTransferBudget", 100_000);

        /// <summary>[GT] Per-mille prize-money share folded into the transfer budget. Config key [club-finances] TransferBudgetPrizeSharePermille. Spec #40 Appendix A.</summary>
        public static readonly int TransferBudgetPrizeSharePermille = Config.GetInt("club-finances", "TransferBudgetPrizeSharePermille", 400);

        /// <summary>[GT] Flat wage-budget allocation before prize-money share. Config key [club-finances] BaseWageBudget. Spec #40 Appendix A.</summary>
        public static readonly long BaseWageBudget = Config.GetInt("club-finances", "BaseWageBudget", 50_000);

        /// <summary>[GT] Per-mille prize-money share folded into the wage budget. Config key [club-finances] WageBudgetPrizeSharePermille. Spec #40 Appendix A.</summary>
        public static readonly int WageBudgetPrizeSharePermille = Config.GetInt("club-finances", "WageBudgetPrizeSharePermille", 150);

        /// <summary>[GT] Sanity ceiling for projected transfer and wage budgets. Config key [club-finances] ClubFinancesBudgetCeilingMax. Spec #40 Appendix A.</summary>
        public static readonly long ClubFinancesBudgetCeilingMax = Config.GetInt("club-finances", "ClubFinancesBudgetCeilingMax", 50_000_000);

        #endregion
    }
}

#region VersionHistory
// Version | Date       | Author        | Change
// --------|------------|---------------|----------------------------------------------
// 1.0     | 2026-09-04 | Codex / Anton | Initial #40 T0 constants catalogue.
// 1.1     | 2026-09-04 | Codex / Anton | T1a: add save magic/version and framing widths.
#endregion
