// File:     src/season-save/SeasonFinanceCoherence.cs
// Created:  2026-09-10
// Modified: 2026-09-10
// Author:   —
// Spec:     Club Finances & Economy #40 FR-FN-002/025; Season & Competition Loop #30 Appendix B.1;
//           ERR-030-050; Code Standards #20
// Purpose:  One owner for the cross-block club-universe invariant between SeasonState and #40 finance
//           entries. Empty remains the explicit pre-T2 state; once non-empty, the finance set must be
//           exactly the current season's club set.

using System;
using System.Collections.ObjectModel;

using TacticalDirector.ClubFinances;

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
        /// against <paramref name="season"/>. Null and empty both mean the explicit pre-T2 state at the
        /// loop-composition boundary. A non-empty set must contain exactly one entry for every current
        /// season club and no foreign club (FR-FN-025 / ERR-030-050).
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
                return Array.Empty<ClubFinanceEntry>();
            }

            var copy = (ClubFinanceEntry[])financesOrNull.Clone();
            Array.Sort(copy, (x, y) => x.ClubId.CompareTo(y.ClubId));

            ReadOnlyCollection<int> seasonClubs = season.ClubIds;
            if (copy.Length != seasonClubs.Count)
            {
                throw new ArgumentException(
                    $"The finance set carries {copy.Length} club(s) but the season carries " +
                    $"{seasonClubs.Count}. Once #40 state exists, FR-FN-025 requires exactly one " +
                    "persistent finance entry per current-season club; only the wholly empty pre-T2 " +
                    "state is exempt.",
                    paramName);
            }

            for (int i = 0; i < copy.Length; i++)
            {
                if (copy[i].ClubId != seasonClubs[i])
                {
                    throw new ArgumentException(
                        $"The finance set and season disagree at ascending position {i}: finance " +
                        $"ClubId {copy[i].ClubId}, season ClubId {seasonClubs[i]}. A non-empty finance " +
                        "set must exactly match SeasonState.ClubIds (FR-FN-025 / ERR-030-050).",
                        paramName);
                }
            }

            return copy;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                        |
// | 1.0     | 2026-09-10 | —      | ERR-030-050: shared current-season finance club-set gate;    |
// |         |            |        | empty remains legal pre-T2, non-empty must match exactly.    |
#endregion
