// File:     src/season-save/SeasonFinanceCoherence.cs
// Created:  2026-09-10
// Modified: 2026-09-11 (#40 T2b review — legacy empty finance migration is now an explicit
//           Restore-only operation; ordinary composition validation no longer manufactures state.)
// Modified: 2026-09-10
// Author:   —
// Spec:     Club Finances & Economy #40 FR-FN-002/025; Season & Competition Loop #30 Appendix B.1;
//           ERR-030-050/051; Code Standards #20
// Purpose:  One owner for the cross-block club-universe invariant between SeasonState and #40 finance
//           entries, with the T2b compatibility migration kept separate from ordinary validation.

using System;
using System.Collections.ObjectModel;

using TacticalDirector.ClubFinances;

using ClubFinanceState = TacticalDirector.ClubFinances.ClubFinances;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// Validates and canonicalizes #40 finance entries against the season that owns their club universe.
    /// This rule lives in the composition root because neither #40 nor its standalone codec may depend on
    /// <see cref="SeasonState"/>. Ordinary composition and the restore-only compatibility migration are
    /// deliberately separate so a forgotten finance argument cannot be silently repaired as valid state.
    /// </summary>
    internal static class SeasonFinanceCoherence
    {
        /// <summary>
        /// Returns an ascending-ClubId snapshot of <paramref name="financesOrNull"/> after checking it
        /// against <paramref name="season"/>. Null/empty remains the explicit legacy/unwired generic
        /// composition state and is returned as empty; it is NOT initialized here. A supplied non-empty
        /// set must contain exactly one entry for every current-season club and no foreign club
        /// (FR-FN-025 / ERR-030-050).
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
                    "persistent finance entry per current-season club; only the wholly empty legacy " +
                    "generic-composition state is exempt.",
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

        /// <summary>
        /// Restore-only T2b compatibility migration for a v7 save produced while T1b still represented
        /// "runtime producer not wired" as a well-formed empty finance block. A non-empty restored block
        /// is only validated/canonicalized through <see cref="Normalize"/>; an empty restored block is
        /// initialized exactly here, never by ordinary constructor composition.
        /// <para>
        /// The canonical new-game path remains <see cref="ClubFinanceEntry.CreateInitialForSquads"/>
        /// over #27 squads. A persisted empty v7 block contains no squad payload to replay, so the
        /// restored <see cref="SeasonState.ClubIds"/> is the authoritative persisted club universe for
        /// this one migration. Initial values remain #40-owned through
        /// <see cref="ClubFinanceState.CreateInitial"/> and <see cref="ClubFinancesConstants"/>.
        /// </para>
        /// </summary>
        internal static ClubFinanceEntry[] NormalizeLegacyRestore(
            SeasonState season,
            ClubFinanceEntry[] financesOrNull,
            string paramName)
        {
            if (season == null)
            {
                throw new ArgumentNullException(nameof(season));
            }

            if (financesOrNull != null && financesOrNull.Length != 0)
            {
                return Normalize(season, financesOrNull, paramName);
            }

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
// | 1.1     | 2026-09-11 | —      | T2b first cut upgraded every empty composition implicitly.   |
// | 1.2     | 2026-09-11 | —      | Review: ordinary Normalize is validation-only again; legacy  |
// |         |            |        | empty initialization is explicit and Restore-only.           |
#endregion
