// ============================================================================
// File:     src/season-save/SeasonFinanceRuntime.cs
// Created:  2026-09-11
// Modified: 2026-10-13
// Author:   —
// Spec:     Club Finances & Economy #40 FR-FN-001/002/003/004/012/013/023/025/027,
//           §3.4/§3.6/§3.7, §4.1-§4.3, §7.1 T2b/T3b2; Season Loop #30 §3.3/§3.5/§4.3;
//           Code Standards #20
// Purpose:  #30-owned composition mechanics for the live #40 state: keyed access, ledger routing,
//           staged daily accounting and season-boundary settlement for SeasonLoop.
// ============================================================================

using System;

using TacticalDirector.ClubFinances;

using ClubFinanceState = TacticalDirector.ClubFinances.ClubFinances;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// Composition-only mechanics for #40 inside #30. The finance assembly owns all arithmetic and
    /// ledger semantics; this helper owns only the mapping between stable season <c>ClubId</c>s and the
    /// canonical <see cref="ClubFinanceEntry"/> array carried by <see cref="SeasonLoop"/>.
    /// </summary>
    internal static class SeasonFinanceRuntime
    {
        /// <summary>
        /// Invokes #40's accounting primitive once per initialized club for the day being completed.
        /// With the gate on, forwards settled calendar/fixture/table facts into #40's amount model.
        /// With the gate off, does not inspect deep-only season inputs and supplies zero amounts.
        /// Returns all clubs in a detached array only after every calculation succeeds. SeasonLoop
        /// publishes that complete result; a refusal preserves live state for retry without a day cursor.
        /// </summary>
        internal static ClubFinanceEntry[] PrepareDailyRevenue(
            ClubFinanceEntry[] entries, bool deepRevenueEnabled, SeasonState season, uint day)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            if (entries.Length == 0)
            {
                throw new InvalidOperationException("Daily finance accounting requires initialized club finances.");
            }

            bool inSeasonDay = false;
            int clubCount = 0;
            if (deepRevenueEnabled)
            {
                if (season == null)
                {
                    throw new ArgumentNullException(nameof(season));
                }

                clubCount = season.ClubIds.Count;
                inSeasonDay = day >= season.Calendar.DayOfRound(0)
                    && day <= season.Calendar.DayOfRound(season.Calendar.RoundCount - 1);
            }

            var accrued = new ClubFinanceEntry[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                ClubFinanceEntry entry = entries[i];
                ClubFinanceState prior = entry.Finances;
                long sponsorship = 0L;
                long matchday = 0L;
                if (deepRevenueEnabled)
                {
                    long homeFixturesCompleted = 0L;
                    for (int fixtureIndex = 0; fixtureIndex < season.FixtureCount; fixtureIndex++)
                    {
                        Fixture fixture = season.FixtureAt(fixtureIndex);
                        if (fixture.HomeClubId != entry.ClubId
                            || season.Calendar.DayOfRound(fixture.RoundIndex) != day)
                        {
                            continue;
                        }

                        // Played is #30's persisted assertion that the result was applied to the table.
                        if (!fixture.Played)
                        {
                            throw new InvalidOperationException("Daily revenue cannot count an unplayed home fixture.");
                        }

                        homeFixturesCompleted = checked(homeFixturesCompleted + 1L);
                    }

                    (sponsorship, matchday) = FinanceStep.DailyRevenue(
                        homeFixturesCompleted, season.PositionOf(entry.ClubId), clubCount, inSeasonDay);
                }

                ClubFinanceState next = FinanceStep.AccrueDailyRevenue(
                    in prior,
                    sponsorshipRevenue: sponsorship,
                    matchdayRevenue: matchday,
                    deepRevenueEnabled);
                accrued[i] = new ClubFinanceEntry(entry.ClubId, in next);
            }

            return accrued;
        }

        /// <summary>
        /// Computes the complete season-boundary finance result without mutating the live array.
        /// <see cref="SeasonLoop.RollToNextSeason"/> calls this at step (b') and installs the returned
        /// array only after the season's fallible commit succeeds, preserving #30's all-or-nothing roll.
        /// </summary>
        internal static ClubFinanceEntry[] PrepareSettlement(
            SeasonState season,
            ClubFinanceEntry[] entries)
        {
            if (season == null)
            {
                throw new ArgumentNullException(nameof(season));
            }

            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            // The low-level generic SeasonLoop constructor may still carry the explicit legacy/unwired
            // empty state for compatibility. Canonical new games never do (League.CreateLoop bootstraps
            // them), and Restore upgrades a persisted empty T1b block before ordinary composition.
            // Settlement is a finance operation, so the remaining generic empty state fails loud here
            // rather than silently skipping prize money and next-season budget projection.
            if (entries.Length == 0)
            {
                throw new InvalidOperationException(
                    "Club finances are not initialized on this SeasonLoop. Canonical new games must use "
                    + "League.CreateLoop and restored T1b saves must enter through SeasonLoop.Restore "
                    + "before a season can settle finances.");
            }

            var settled = new ClubFinanceEntry[entries.Length];
            int clubCount = season.ClubIds.Count;

            // T2b intentionally consumes the identity board input. #45/non-identity BoardModifier
            // production is explicitly deferred to #40 T3 (§7.1); do not invent a second producer here.
            BoardModifier board = BoardModifier.Identity;

            for (int i = 0; i < entries.Length; i++)
            {
                ClubFinanceEntry entry = entries[i];
                ClubFinanceState prior = entry.Finances;
                int position = season.PositionOf(entry.ClubId);
                ClubFinanceState next = FinanceStep.SettleFinances(
                    in prior,
                    position,
                    clubCount,
                    in board);
                settled[i] = new ClubFinanceEntry(entry.ClubId, in next);
            }

            return settled;
        }

        /// <summary>Returns a detached observer value for one club, failing loud when #40 is absent.</summary>
        internal static FinancesViewModel View(ClubFinanceEntry[] entries, int clubId)
        {
            int index = IndexOf(entries, clubId);
            ClubFinanceState finances = entries[index].Finances;
            return FinancesViewModel.From(in finances);
        }

        /// <summary>Routes #31's read-only spending-ceiling query through #40's canonical ledger API.</summary>
        internal static long AvailableTransferBudget(ClubFinanceEntry[] entries, int clubId)
        {
            int index = IndexOf(entries, clubId);
            ClubFinanceState finances = entries[index].Finances;
            return FinanceLedger.AvailableTransferBudget(in finances);
        }

        /// <summary>
        /// Routes one command to #40 and replaces the keyed value only after #40 accepts the complete
        /// transaction. A rejected transaction therefore leaves the live entry byte-for-byte unchanged.
        /// </summary>
        internal static void ApplyTransaction(
            ClubFinanceEntry[] entries,
            int clubId,
            in FinanceTransaction transaction)
        {
            int index = IndexOf(entries, clubId);
            ClubFinanceEntry entry = entries[index];
            ClubFinanceState finances = entry.Finances;
            FinanceLedger.ApplyTransaction(ref finances, in transaction);
            entries[index] = new ClubFinanceEntry(clubId, in finances);
        }

        private static int IndexOf(ClubFinanceEntry[] entries, int clubId)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            if (entries.Length == 0)
            {
                throw new InvalidOperationException(
                    "Club finances are not initialized on this SeasonLoop. Canonical new games must use "
                    + "League.CreateLoop and restored T1b saves must enter through SeasonLoop.Restore "
                    + "before finance commands or queries are valid (F6 / FR-FN-025).");
            }

            int low = 0;
            int high = entries.Length - 1;
            while (low <= high)
            {
                int middle = low + ((high - low) / 2);
                int middleClubId = entries[middle].ClubId;
                if (middleClubId == clubId)
                {
                    return middle;
                }

                if (middleClubId < clubId)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            throw new ArgumentOutOfRangeException(
                nameof(clubId),
                clubId,
                "No ClubFinances entry exists for this ClubId (F6 / FR-FN-025).");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                        |
// | 1.0     | 2026-09-11 | —      | #40 T2b: keyed runtime access plus staged boundary settlement. |
// | 1.1     | 2026-09-11 | —      | Alias finance state type to avoid namespace/type ambiguity.   |
// | 1.2     | 2026-09-11 | —      | First review made empty state fail loud at runtime.           |
// | 1.3     | 2026-09-11 | —      | Claude review: comments align with Restore-only legacy        |
// |         |            |        | migration; BoardModifier.Identity explicitly pinned to T2b.   |
// | 1.4     | 2026-10-08 | —      | T3b1: allocation-free, per-club zero daily accounting invocation. |
// | 1.5     | 2026-10-08 | —      | PR #491 review: publish daily state only after every club succeeds. |
// | 1.6     | 2026-10-13 | —      | T3b2: compose settled day/fixture/table facts through #40 amounts, only when enabled. |
#endregion
