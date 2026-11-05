// File:     src/client-app/TransferBudgetRule.cs
// Created:  2026-11-05
// Modified: 2026-11-05
// Author:   —
// Spec:     Transfers #31 (F1 budget check, TransferCommands), docs/tracking/interactive-unity-client-design.md §5-P8,
//           Code Standards #20
// Purpose:  The single client copy of #31's transfer-budget headroom rule, shared by the two view models.

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// Mirrors #31's F1 check (<c>TransferCommands</c>): a buy fits while its fee is at most the season ceiling
    /// minus the fees already committed in the window. Both the Transfers and Club Finances view models call
    /// this so the two screens cannot show different headroom (L2).
    /// </summary>
    internal static class TransferBudgetRule
    {
        /// <summary>Ceiling minus committed spend, in the same money units as both inputs.</summary>
        internal static long Remaining(long ceiling, long committedSpend) => ceiling - committedSpend;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-05 | —      | Initial creation (L2): one headroom helper for both view models. |
#endregion
