// ============================================================================
// File:     src/club-finances/ClubFinanceEntry.cs
// Created:  2026-09-04
// Modified: 2026-09-11 (#40 T2a — clarify pure bootstrap transform ownership)
// Author:   —
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #40 FR-FN-002/025/027, §4.1-§4.3, §7.1 T2a
// Purpose:  Couples one stable ClubId to its #40-owned finance value for canonical save framing and
//           provides the pure T2a bootstrap transform over canonical #27 squad identities.
// ============================================================================

using System;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.ClubFinances
{
    /// <summary>One persisted per-club finance record, keyed by stable <see cref="ClubId"/>.</summary>
    public readonly struct ClubFinanceEntry
    {
        /// <summary>Stable club identity from #27.</summary>
        public readonly int ClubId;

        /// <summary>#40-owned financial state for <see cref="ClubId"/>.</summary>
        public readonly ClubFinances Finances;

        /// <summary>Creates a ClubId-keyed finance record.</summary>
        public ClubFinanceEntry(int clubId, in ClubFinances finances)
        {
            ClubId = clubId;
            Finances = finances;
        }

        /// <summary>
        /// Creates the initial #40 record set from the canonical #27 squad identities. The returned
        /// records are sorted by <see cref="ClubId"/> so they are already in the canonical order used
        /// by <see cref="ClubFinancesSaveCodec"/> and the #30 composition seam.
        /// </summary>
        /// <param name="squads">The league/game-bootstrap squads whose <see cref="Squad.ClubId"/> values
        /// define the stable club universe.</param>
        /// <returns>Exactly one initialized finance entry per supplied squad.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="squads"/> is null.</exception>
        /// <exception cref="ArgumentException">The set is empty, contains a null squad, or contains the
        /// same <see cref="Squad.ClubId"/> more than once.</exception>
        public static ClubFinanceEntry[] CreateInitialForSquads(Squad[] squads)
        {
            if (squads == null)
            {
                throw new ArgumentNullException(nameof(squads));
            }

            if (squads.Length == 0)
            {
                throw new ArgumentException(
                    "Finance bootstrap requires at least one canonical club squad.",
                    nameof(squads));
            }

            var entries = new ClubFinanceEntry[squads.Length];
            for (int i = 0; i < squads.Length; i++)
            {
                Squad squad = squads[i];
                if (squad == null)
                {
                    throw new ArgumentException($"squads[{i}] is null.", nameof(squads));
                }

                ClubFinances finances = ClubFinances.CreateInitial(
                    ClubFinancesConstants.StartingClubBalance);
                entries[i] = new ClubFinanceEntry(squad.ClubId, in finances);
            }

            Array.Sort(entries, (left, right) => left.ClubId.CompareTo(right.ClubId));
            for (int i = 1; i < entries.Length; i++)
            {
                if (entries[i - 1].ClubId == entries[i].ClubId)
                {
                    throw new ArgumentException(
                        $"Finance bootstrap received duplicate ClubId {entries[i].ClubId}; exactly one "
                        + "entry per club is required (FR-FN-025).",
                        nameof(squads));
                }
            }

            return entries;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-04 | —      | Initial #40 T1a persisted entry value. |
// | 1.1     | 2026-09-06 | —      | Header author attribution corrected to automated-agent placeholder. |
// | 1.3     | 2026-09-08 | —      | Corrected the version-history table to the required parseable pipe-row format. |
// | 1.4     | 2026-09-11 | —      | T2a: bootstrap canonical initial entries from #27 Squad.ClubId values. |
// | 1.5     | 2026-09-11 | —      | Review follow-up: ownership wording aligned with §7.3; #40 provides a pure transform, #30 owns lifecycle invocation. |
#endregion
