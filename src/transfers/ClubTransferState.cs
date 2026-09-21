// ============================================================================
// File:     src/transfers/ClubTransferState.cs
// Created:  2026-09-12
// Modified: 2026-09-12
// Author:   —
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #31 §2.3, §3.3 (season-scoped transfer state)
// Purpose:  Declares #31's per-managed-club window state and static-ceiling spend accumulator.
// ============================================================================

using System;

namespace TacticalDirector.Transfers
{
    /// <summary>Season-scoped transfer state for the managed club.</summary>
    public struct ClubTransferState
    {
        /// <summary>Accepted buy fees accumulated against #40's static season transfer ceiling.</summary>
        public long CommittedSpendThisWindow;

        /// <summary>Currently active #31-owned transfer window.</summary>
        public TransferWindow ActiveWindow;

        /// <summary>Creates validated season-scoped transfer state.</summary>
        public ClubTransferState(long committedSpendThisWindow, in TransferWindow activeWindow)
        {
            if (committedSpendThisWindow < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(committedSpendThisWindow),
                    committedSpendThisWindow,
                    "CommittedSpendThisWindow must be non-negative (F1).");
            }

            CommittedSpendThisWindow = committedSpendThisWindow;
            ActiveWindow = activeWindow;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 per-club transfer state. |
#endregion
