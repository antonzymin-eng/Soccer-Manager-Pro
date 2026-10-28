// File:     src/client-app/CareerSlotSummary.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (D-P7-2), Code Standards #20
// Purpose:  One row of the Load Career picker: the slot, its state, and the career facts recorded in its
//           sidecar when it was written — enough to choose a save without decoding it.

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// A save slot as the picker shows it (§5-P7). The career facts are meaningful only when
    /// <see cref="State"/> is <see cref="CareerSlotState.Ready"/> or <see cref="CareerSlotState.Incompatible"/>
    /// (both have a readable sidecar); for <see cref="CareerSlotState.Empty"/> and
    /// <see cref="CareerSlotState.Damaged"/> they are zero / empty.
    /// </summary>
    public readonly struct CareerSlotSummary
    {
        /// <summary>The slot this row describes.</summary>
        public readonly CareerSaveSlot Slot;

        /// <summary>The slot's state.</summary>
        public readonly CareerSlotState State;

        /// <summary>
        /// The store-wide write sequence at which this slot was last written (monotonic across every slot
        /// of one store). The highest <see cref="CareerSlotState.Ready"/> sequence is what Continue loads.
        /// </summary>
        public readonly long Sequence;

        /// <summary>The season-save format version the save was written under.</summary>
        public readonly uint SeasonSaveFormatVersion;

        /// <summary>The career's world seed.</summary>
        public readonly ulong WorldSeed;

        /// <summary>The managed club's id.</summary>
        public readonly int ManagedClubId;

        /// <summary>The managed club's display name at the time of writing.</summary>
        public readonly string ManagedClubName;

        /// <summary>#30's season counter (0 = the career's first season).</summary>
        public readonly int SeasonNumber;

        /// <summary>The world day the save was taken on.</summary>
        public readonly uint WorldDay;

        /// <summary>Rounds played in that season when the save was taken.</summary>
        public readonly int RoundsPlayed;

        /// <summary>Rounds in that season.</summary>
        public readonly int RoundCount;

        /// <summary>The managed club's league position (1-based) when the save was taken.</summary>
        public readonly int LeaguePosition;

        /// <summary>Constructs a summary row.</summary>
        public CareerSlotSummary(
            CareerSaveSlot slot,
            CareerSlotState state,
            long sequence,
            uint seasonSaveFormatVersion,
            ulong worldSeed,
            int managedClubId,
            string managedClubName,
            int seasonNumber,
            uint worldDay,
            int roundsPlayed,
            int roundCount,
            int leaguePosition)
        {
            Slot = slot;
            State = state;
            Sequence = sequence;
            SeasonSaveFormatVersion = seasonSaveFormatVersion;
            WorldSeed = worldSeed;
            ManagedClubId = managedClubId;
            ManagedClubName = managedClubName ?? string.Empty;
            SeasonNumber = seasonNumber;
            WorldDay = worldDay;
            RoundsPlayed = roundsPlayed;
            RoundCount = roundCount;
            LeaguePosition = leaguePosition;
        }

        /// <summary>A row for a slot with no readable sidecar.</summary>
        public static CareerSlotSummary Without(CareerSaveSlot slot, CareerSlotState state) =>
            new CareerSlotSummary(slot, state, 0, 0, 0, 0, string.Empty, 0, 0, 0, 0, 0);

        /// <summary>True when the slot can be loaded.</summary>
        public bool IsLoadable => State == CareerSlotState.Ready;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7): the picker row.                      |
#endregion
