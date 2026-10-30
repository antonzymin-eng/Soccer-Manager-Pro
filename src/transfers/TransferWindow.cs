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
        /// §3.5 as corrected by ERR-031-011 (superseding ERR-031-008's roll-day-length derivation): the
        /// single minimal summer window, derived from the season's OWN calendar so it does not depend on
        /// when the client rolls. <c>Open</c> = <paramref name="seasonBeginWorldDay"/>, the world day #30
        /// begins the season (the new-game day at genesis, the roll day at each boundary) — nothing can
        /// transact before the season exists. <c>Close</c> = <paramref name="openingFixtureWorldDay"/> +
        /// <see cref="TransfersConstants.SummerWindowInSeasonDays"/> − 1, inclusive, so the window stays
        /// open for exactly that many days from the opening fixture day on, whatever the roll day.
        /// Example (days): a season begun on day 267 whose opening fixture is on day 322 gives, with the
        /// default 38, the window [267, 322 + 38 − 1] = [267, 359]. At genesis (day 0, opening fixture
        /// day 7) it is [0, 44].
        /// </summary>
        /// <param name="seasonBeginWorldDay">World day #30 begins the season (world days, ≥ 0).</param>
        /// <param name="openingFixtureWorldDay">World day of the season's round 0 (world days, ≥ begin).</param>
        /// <exception cref="ArgumentOutOfRangeException">The opening fixture day precedes the season's
        /// begin day, or the close would run past <c>uint.MaxValue</c>.</exception>
        public static TransferWindow SummerWindowFor(uint seasonBeginWorldDay, uint openingFixtureWorldDay)
        {
            if (openingFixtureWorldDay < seasonBeginWorldDay)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(openingFixtureWorldDay), openingFixtureWorldDay,
                    "The season's opening fixture day cannot precede the day the season begins.");
            }

            ulong close = (ulong)openingFixtureWorldDay + (ulong)TransfersConstants.SummerWindowInSeasonDays - 1UL;
            if (close > uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(openingFixtureWorldDay), openingFixtureWorldDay, "The summer window would run past the last world day.");
            }

            return new TransferWindow(seasonBeginWorldDay, (uint)close);
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
// | 1.2     | 2026-10-20 | —      | ERR-031-011 (AR round 2 M6): SummerWindowFrom replaced by SummerWindowFor(seasonBeginWorldDay, openingFixtureWorldDay) — [begin, opening + SummerWindowInSeasonDays − 1]; the close no longer depends on roll timing. |
#endregion
