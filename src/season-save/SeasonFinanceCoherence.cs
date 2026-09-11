// File:     src/season-save/SeasonFinanceCoherence.cs
// Created:  2026-09-10
// Modified: 2026-09-11 (#40 T2b review — an empty pre-T2 finance block is upgraded at composition
//           into one initial entry per authoritative SeasonState ClubId, so no post-T2 loop can
//           silently skip settlement while old v7 saves remain resumable.)
// Modified: 2026-09-10
// Author:   —
// Spec:     Club Finances & Economy #40 FR-FN-002/025; Season & Competition Loop #30 Appendix B.1;
//           ERR-030-050/051; Code Standards #20
// Purpose:  One owner for the cross-block club-universe invariant between SeasonState and #40 finance
//           entries, including the T2b compatibility upgrade from the formerly legal empty state.

using System;
using System.Collections.ObjectModel;

using TacticalDirector.ClubFinances;

using ClubFinanceState = TacticalDirector.ClubFinances.ClubFinances;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// Validates and canonicalizes #40 finance entries against the season that owns their club universe.
    /// This rule lives in the composition root because neither #40 nor its standalone codec may depend on
    /// <see cref="SeasonState"/>. It is shared by loop composition and the save/load boundary so the
    /// predicates cannot drift.
    /// </summary>
    internal static class SeasonFinanceCoherence
    {
        /// <summary>
        /// Returns an ascending-ClubId snapshot of <paramref name="financesOrNull"/> after checking it
        /// against <paramref name="season"/>. At T2b, the formerly legal null/empty pre-T2 state is a
        /// compatibility input, not a runtime state: it is upgraded deterministically to one initial
        /// #40 entry per authoritative <see cref="SeasonState.ClubIds"/> value. A supplied non-empty set
        /// must contain exactly one entry for every current-season club and no foreign club
        /// (FR-FN-025 / ERR-030-050/-051).
        /// </summary>
        internal static ClubFinanceEntry[] Normalize(
            SeasonState season,
            ClubFinanceEntry[] financesOrNull,
            string paramName)
        {
            if (season == null)
            {
                throw new ArgumentNullException(nameof(season));
            }

            if (financesOrNull == null || financesOrNull.Length == 0)
            {
                return CreateInitialForSeasonClubs(season);
            }

            var copy = (ClubFinanceEntry[])financesOrNull.Clone();
            Array.Sort(copy, (x, y) => x.ClubId.CompareTo(y.ClubId));

            ReadOnlyCollection<int> seasonClubs = season.ClubIds;
            if (copy.Length != seasonClubs.Count)
            {
                throw new ArgumentException(
                    $"The finance set carries {copy.Length} club(s) but the season carries " +
                    $"{seasonClubs.Count}. FR-FN-025 requires exactly one persistent finance entry " +
                    "per current-season club.",
                    paramName);
            }

            for (int i = 0; i < copy.Length; i++)
            {
                if (copy[i].ClubId != seasonClubs[i])
                {
                    throw new ArgumentException(
                        $"The finance set and season disagree at ascending position {i}: finance " +
                        $"ClubId {copy[i].ClubId}, season ClubId {seasonClubs[i]}. The finance set " +
                        "must exactly match SeasonState.ClubIds (FR-FN-025 / ERR-030-050).",
                        paramName);
                }
            }

            return copy;
        }

        /// <summary>
        /// T2b compatibility migration for loops/saves composed while T1b still represented "not yet
        /// wired" as an empty finance block. The canonical new-game path still invokes
        /// <see cref="ClubFinanceEntry.CreateInitialForSquads"/> over #27 squads. This fallback exists
        /// only because an already-persisted empty v7 block has no squad payload to replay; its season
        /// ClubIds are the authoritative persisted club universe, while all initial values remain owned
        /// by #40 through <see cref="ClubFinanceState.CreateInitial"/> and its constants.
        /// </summary>
        private static ClubFinanceEntry[] CreateInitialForSeasonClubs(SeasonState season)
        {
            ReadOnlyCollection<int> clubIds = season.ClubIds;
            var entries = new ClubFinanceEntry[clubIds.Count];
            for (int i = 0; i < clubIds.Count; i++)
            {
                ClubFinanceState finances = ClubFinanceState.CreateInitial(
                    ClubFinancesConstants.StartingClubBalance);
                entries[i] = new ClubFinanceEntry(clubIds[i], in finances);
            }

            return entries;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                        |
// | 1.0     | 2026-09-10 | —      | ERR-030-050: shared current-season finance club-set gate;    |
// |         |            |        | empty remained legal while T2 producer wiring was absent.    |
// | 1.1     | 2026-09-11 | —      | T2b review: empty is now a compatibility input only; it is   |
// |         |            |        | upgraded to one initial entry per SeasonState ClubId.        |
#endregion
