// ============================================================================
// File:     src/transfers/TransferWindow.cs
// Created:  2026-09-12
// Modified: 2026-10-20
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

        /// <summary>
        /// §3.5 as corrected by ERR-031-008: the single minimal summer window, opening on the world day #30
        /// begins the season (the new-game day at genesis, the roll day at each boundary) and lasting
        /// exactly <see cref="TransfersConstants.SummerWindowLengthDays"/> days INCLUSIVE —
        /// <c>[start, start + length − 1]</c>. Example: a season begun on day 280 with the default 45-day
        /// window is open on days 280..324.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The window would run past <c>uint.MaxValue</c>.</exception>
        public static TransferWindow SummerWindowFrom(uint seasonStartWorldDay)
        {
            ulong close = (ulong)seasonStartWorldDay + (ulong)TransfersConstants.SummerWindowLengthDays - 1UL;
            if (close > uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(seasonStartWorldDay), seasonStartWorldDay, "The summer window would run past the last world day.");
            }

            return new TransferWindow(seasonStartWorldDay, (uint)close);
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
// | 1.1     | 2026-10-20 | —      | #31 T2 (ERR-031-008): SummerWindowFrom(seasonStartWorldDay) — [start, start + SummerWindowLengthDays − 1], start = the day #30 begins the season. |
#endregion
