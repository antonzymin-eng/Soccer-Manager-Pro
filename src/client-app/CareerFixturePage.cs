// File:     src/client-app/CareerFixturePage.cs
// Created:  2026-11-19
// Modified: 2026-11-19
// Author:   —
// Spec:     Interactive Unity client §5-P7, UX detailed plan §Schedule candidate, Code Standards #20
// Purpose:  Page the managed club's fixture list so a keyboard user can reach every row at 200% text scale.

using System;

namespace TacticalDirector.ClientApp
{
    /// <summary>A fixed-size page of the managed club's fixture list, by fixture index.</summary>
    public readonly struct CareerFixturePage
    {
        /// <summary>
        /// [FIXED] Fixtures per page. A page and its heading fit between the paging controls and the next
        /// focusable control in one 1280×720 viewport at 200% text scale, so moving focus past the list
        /// always brings every row on screen (pinned-host run, November 19, 2026). A layout value, not a
        /// balance dial.
        /// </summary>
        public const int FIXTURES_PER_PAGE = 8;

        /// <summary>The zero-based index of the first fixture shown.</summary>
        public readonly int First;

        /// <summary>The number of fixtures shown; zero only when the club has no fixtures.</summary>
        public readonly int Count;

        /// <summary>The number of fixtures in the club's season.</summary>
        public readonly int Total;

        /// <summary>The first index of the previous page, or <see cref="First"/> on the first page.</summary>
        public readonly int PreviousFirst;

        /// <summary>The first index of the next page, or <see cref="First"/> on the last page.</summary>
        public readonly int NextFirst;

        private CareerFixturePage(int first, int count, int total, int previousFirst, int nextFirst)
        {
            First = first;
            Count = count;
            Total = total;
            PreviousFirst = previousFirst;
            NextFirst = nextFirst;
        }

        /// <summary>True when an earlier page exists.</summary>
        public bool HasPrevious => PreviousFirst != First;

        /// <summary>True when a later page exists.</summary>
        public bool HasNext => NextFirst != First;

        /// <summary>The page holding the club's next unplayed fixture, or the last page once all are played.</summary>
        /// <exception cref="ArgumentException"><paramref name="home"/> has no active career.</exception>
        public static CareerFixturePage Current(in CareerHomeViewModel home)
        {
            RequireCareer(in home);
            int next = 0;
            while (next < home.Fixtures.Count && home.Fixtures[next].IsPlayed)
            {
                next++;
            }

            return At(in home, Math.Min(next, Math.Max(0, home.Fixtures.Count - 1)));
        }

        /// <summary>The page containing the requested fixture index, clamped to the season's fixtures.
        /// Pages start at multiples of <see cref="FIXTURES_PER_PAGE"/>.</summary>
        /// <exception cref="ArgumentException"><paramref name="home"/> has no active career.</exception>
        public static CareerFixturePage At(in CareerHomeViewModel home, int requestedIndex)
        {
            RequireCareer(in home);
            int total = home.Fixtures.Count;
            if (total == 0)
            {
                return new CareerFixturePage(0, 0, 0, 0, 0);
            }

            int lastFirst = StartOfPage(total - 1);
            int first = Math.Max(0, Math.Min(lastFirst, StartOfPage(Math.Max(0, requestedIndex))));
            int count = Math.Min(FIXTURES_PER_PAGE, total - first);
            int previous = first > 0 ? first - FIXTURES_PER_PAGE : first;
            int next = first < lastFirst ? first + FIXTURES_PER_PAGE : first;
            return new CareerFixturePage(first, count, total, previous, next);
        }

        private static int StartOfPage(int index) => index - index % FIXTURES_PER_PAGE;

        private static void RequireCareer(in CareerHomeViewModel home)
        {
            if (!home.HasCareer)
            {
                throw new ArgumentException("A fixture page requires an active career.", nameof(home));
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-19 | —      | Fixture-list paging for keyboard reach at 200% text scale. |
#endregion
