// ============================================================================
// File:     src/transfers/TransferWindow.cs
// Created:  2026-09-12
// Modified: 2026-09-12
// Author:   —
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #31 §2.3, §3.5, FR-TX-019/020 (owned transfer window)
// Purpose:  Declares #31's inclusive world-day transfer window and deterministic open predicate.
// ============================================================================

using System;

namespace TacticalDirector.Transfers
{
    /// <summary>#31-owned inclusive transfer window over world-day ordinals.</summary>
    public readonly struct TransferWindow
    {
        /// <summary>First world day on which transfer commands are legal.</summary>
        public readonly uint OpenWorldDay;

        /// <summary>Last world day on which transfer commands are legal.</summary>
        public readonly uint CloseWorldDay;

        /// <summary>Creates an inclusive transfer window.</summary>
        public TransferWindow(uint openWorldDay, uint closeWorldDay)
        {
            if (closeWorldDay < openWorldDay)
            {
                throw new ArgumentOutOfRangeException(nameof(closeWorldDay), closeWorldDay, "CloseWorldDay must be at or after OpenWorldDay (F4).");
            }

            OpenWorldDay = openWorldDay;
            CloseWorldDay = closeWorldDay;
        }

        /// <summary>Returns whether <paramref name="worldDay"/> is inside this inclusive window.</summary>
        public bool IsWindowOpen(uint worldDay)
        {
            return worldDay >= OpenWorldDay && worldDay <= CloseWorldDay;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 inclusive transfer-window value. |
#endregion
