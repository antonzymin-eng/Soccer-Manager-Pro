// File:     src/client-app/CareerProgress.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (D-P7-3 day-granular progress),
//           Season & Competition Loop #30 §3.3 (KD-4 cursor invariant), §3.4 (FR-SN-012),
//           §3.5 (boundary roll, ERR-030-053), UI / Client Framework #38 Appendix C, Code Standards #20
// Purpose:  Derives the single legal progress action from #30's state, and maps it to and from the
//           season intent the season dispatcher routes. Pure reads; nothing here mutates the loop.

using System;

using TacticalDirector.SeasonSave;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// The Early Access progress rule (§5-P7 D-P7-3, owner decision October 18, 2026: "advance one day
    /// at a time; the match appears when its day arrives"), expressed against #30's own guards so the
    /// client can never offer an action the loop would refuse:
    /// <list type="number">
    /// <item>Mid-season, clock before the pending round's fixture day → <see cref="CareerProgressAction.AdvanceDay"/>.
    /// <c>AdvanceDays(1)</c> can never overshoot: the clock is strictly before the target.</item>
    /// <item>Mid-season, clock ON the fixture day → <see cref="CareerProgressAction.PlayMatchday"/>.
    /// <c>AdvanceDays(1)</c> would pass the target and #30 refuses it (KD-4); the round is the only way on.</item>
    /// <item>Season complete, final fixture day not yet completed by a day advance →
    /// <see cref="CareerProgressAction.AdvanceDay"/>. <c>RollToNextSeason</c> refuses until that day has
    /// been ticked (ERR-030-053), and the step is inside the close-season bound.</item>
    /// <item>Season complete and the final fixture day completed → <see cref="CareerProgressAction.StartNextSeason"/>.</item>
    /// </list>
    /// </summary>
    public static class CareerProgress
    {
        /// <summary>The one progress action legal for <paramref name="loop"/> now. Never <see cref="CareerProgressAction.None"/>.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="loop"/> is null.</exception>
        public static CareerProgressAction NextAction(SeasonLoop loop)
        {
            if (loop == null)
            {
                throw new ArgumentNullException(nameof(loop));
            }

            SeasonCalendar calendar = loop.State.Calendar;
            uint today = loop.CurrentWorldDay;

            if (!calendar.IsSeasonComplete)
            {
                return today < calendar.NextFixtureDay()
                    ? CareerProgressAction.AdvanceDay
                    : CareerProgressAction.PlayMatchday;
            }

            uint finalFixtureDay = calendar.DayOfRound(calendar.RoundCount - 1);
            return today > finalFixtureDay
                ? CareerProgressAction.StartNextSeason
                : CareerProgressAction.AdvanceDay;
        }

        /// <summary>Days from today until the pending round's fixture day; 0 on matchday, -1 once the season is complete.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="loop"/> is null.</exception>
        public static int DaysUntilNextMatchday(SeasonLoop loop)
        {
            if (loop == null)
            {
                throw new ArgumentNullException(nameof(loop));
            }

            SeasonCalendar calendar = loop.State.Calendar;
            if (calendar.IsSeasonComplete)
            {
                return -1;
            }

            uint target = calendar.NextFixtureDay();
            uint today = loop.CurrentWorldDay;
            return today >= target ? 0 : (int)(target - today);
        }

        /// <summary>The season intent the season dispatcher routes for <paramref name="action"/>.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="action"/> is <see cref="CareerProgressAction.None"/> or unallocated.</exception>
        public static ManagerIntent IntentFor(CareerProgressAction action)
        {
            switch (action)
            {
                case CareerProgressAction.AdvanceDay:
                    return ManagerIntent.AdvanceDay();
                case CareerProgressAction.PlayMatchday:
                    return ManagerIntent.AdvanceRound();
                case CareerProgressAction.StartNextSeason:
                    return ManagerIntent.StartNextSeason();
                default:
                    throw new ArgumentOutOfRangeException(nameof(action), action, "Not a legal progress action.");
            }
        }

        /// <summary>The progress action a season intent kind asks for; <see cref="CareerProgressAction.None"/> for any non-season kind.</summary>
        public static CareerProgressAction ActionFor(IntentKind kind)
        {
            switch (kind)
            {
                case IntentKind.AdvanceDay:
                    return CareerProgressAction.AdvanceDay;
                case IntentKind.AdvanceRound:
                    return CareerProgressAction.PlayMatchday;
                case IntentKind.StartNextSeason:
                    return CareerProgressAction.StartNextSeason;
                default:
                    return CareerProgressAction.None;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7): the day-granular progress rule over  |
// |         |            |        | #30's guards (KD-4, ERR-030-053) + intent mapping both ways.   |
#endregion
