// File:     src/client-app/CareerStandingsPage.cs
// Created:  2026-11-20
// Modified: 2026-11-20
// Author:   —
// Spec:     Interactive Unity client §5-P7, UX detailed plan §7.5 Scores and standings, Code Standards #20
// Purpose:  Page the league standings so a keyboard user can bring every row on screen at 200% text scale.

using System;

namespace TacticalDirector.ClientApp
{
    /// <summary>A fixed-size page of the league standings, by table position (zero-based row index).</summary>
    public readonly struct CareerStandingsPage
    {
        /// <summary>
        /// [FIXED] Standings rows per page, matching <see cref="CareerFixturePage.FIXTURES_PER_PAGE"/>. The
        /// page sits above its paging controls, which are the last Tab stops on Career Home, so focusing a
        /// control must bring the page's heading, column headers and rows on screen together at 200% text
        /// scale in the smallest supported layout (pinned-host run, November 20, 2026). A layout value,
        /// not a balance dial.
        /// </summary>
        public const int ROWS_PER_PAGE = 8;

        /// <summary>The zero-based index of the first standings row shown.</summary>
        public readonly int First;

        /// <summary>The number of rows shown; zero only when the table is empty.</summary>
        public readonly int Count;

        /// <summary>The number of clubs in the table.</summary>
        public readonly int Total;

        /// <summary>The first index of the previous (higher) page, or <see cref="First"/> on the first page.</summary>
        public readonly int PreviousFirst;

        /// <summary>The first index of the next (lower) page, or <see cref="First"/> on the last page.</summary>
        public readonly int NextFirst;

        private CareerStandingsPage(int first, int count, int total, int previousFirst, int nextFirst)
        {
            First = first;
            Count = count;
            Total = total;
            PreviousFirst = previousFirst;
            NextFirst = nextFirst;
        }

        /// <summary>True when a higher page exists.</summary>
        public bool HasPrevious => PreviousFirst != First;

        /// <summary>True when a lower page exists.</summary>
        public bool HasNext => NextFirst != First;

        /// <summary>The page holding the managed club's row, or the first page when it is not in the table.</summary>
        /// <exception cref="ArgumentException"><paramref name="home"/> has no active career.</exception>
        public static CareerStandingsPage Current(in CareerHomeViewModel home)
        {
            RequireCareer(in home);
            int managed = 0;
            for (int i = 0; i < home.Table.Count; i++)
            {
                if (home.Table[i].ClubId == home.ManagedClubId)
                {
                    managed = i;
                    break;
                }
            }

            return At(in home, managed);
        }

        /// <summary>The page containing the requested row index, clamped to the table.
        /// Pages start at multiples of <see cref="ROWS_PER_PAGE"/>.</summary>
        /// <exception cref="ArgumentException"><paramref name="home"/> has no active career.</exception>
        public static CareerStandingsPage At(in CareerHomeViewModel home, int requestedIndex)
        {
            RequireCareer(in home);
            int total = home.Table.Count;
            if (total == 0)
            {
                return new CareerStandingsPage(0, 0, 0, 0, 0);
            }

            int lastFirst = StartOfPage(total - 1);
            int first = Math.Max(0, Math.Min(lastFirst, StartOfPage(Math.Max(0, requestedIndex))));
            int count = Math.Min(ROWS_PER_PAGE, total - first);
            int previous = first > 0 ? first - ROWS_PER_PAGE : first;
            int next = first < lastFirst ? first + ROWS_PER_PAGE : first;
            return new CareerStandingsPage(first, count, total, previous, next);
        }

        private static int StartOfPage(int index) => index - index % ROWS_PER_PAGE;

        private static void RequireCareer(in CareerHomeViewModel home)
        {
            if (!home.HasCareer)
            {
                throw new ArgumentException("A standings page requires an active career.", nameof(home));
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-20 | —      | Standings paging for keyboard reach at 200% text scale. |
#endregion
