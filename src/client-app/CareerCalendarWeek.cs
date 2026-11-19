// File:     src/client-app/CareerCalendarWeek.cs
// Created:  2026-11-19
// Modified: 2026-11-19
// Author:   —
// Spec:     Interactive Unity client §5-P7, Season & Competition Loop #30 §2.2, Code Standards #20
// Purpose:  Bound calendar browsing to the career's current season using its world days.

using System;

using TacticalDirector.SeasonSave;

namespace TacticalDirector.ClientApp
{
    /// <summary>A seven-day page on the in-game calendar.</summary>
    public readonly struct CareerCalendarWeek
    {
        /// <summary>[FIXED] World days in a calendar page.</summary>
        public const int DAYS_PER_WEEK = 7;

        /// <summary>The first world day shown.</summary>
        public readonly uint FirstDay;

        /// <summary>The last world day shown.</summary>
        public readonly uint LastDay;

        /// <summary>The start of the previous page, or this page at the lower bound.</summary>
        public readonly uint PreviousWeek;

        /// <summary>The start of the next page, or this page at the upper bound.</summary>
        public readonly uint NextWeek;

        private CareerCalendarWeek(uint firstDay, uint lastDay, uint previousWeek, uint nextWeek)
        {
            FirstDay = firstDay;
            LastDay = lastDay;
            PreviousWeek = previousWeek;
            NextWeek = nextWeek;
        }

        /// <summary>Finds the page containing the requested day, within the current season and current week.
        /// Week starts are aligned to multiples of seven in world days.</summary>
        /// <exception cref="ArgumentException"><paramref name="home"/> has no active career.</exception>
        public static CareerCalendarWeek At(in CareerHomeViewModel home, uint requestedDay)
        {
            if (!home.HasCareer)
            {
                throw new ArgumentException("A calendar week requires an active career.", nameof(home));
            }

            SeasonCalendar calendar = home.Calendar;
            uint firstRoundDay = calendar.DayOfRound(0);
            uint lastRoundDay = calendar.DayOfRound(calendar.RoundCount - 1);
            uint earliest = StartOfWeek(Math.Min(home.WorldDay, firstRoundDay));
            uint latest = StartOfWeek(Math.Max(home.WorldDay, lastRoundDay));
            uint first = Math.Max(earliest, Math.Min(latest, StartOfWeek(requestedDay)));
            uint last = (uint)Math.Min(uint.MaxValue, (ulong)first + DAYS_PER_WEEK - 1);
            uint previous = first > earliest ? first - DAYS_PER_WEEK : first;
            uint next = first < latest ? first + DAYS_PER_WEEK : first;
            return new CareerCalendarWeek(first, last, previous, next);
        }

        private static uint StartOfWeek(uint day) => day - day % DAYS_PER_WEEK;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-19 | —      | Calendar week paging on the world-day timeline. |
#endregion
