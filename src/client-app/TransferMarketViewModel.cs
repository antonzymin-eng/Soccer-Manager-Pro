// File:     src/client-app/TransferMarketViewModel.cs
// Created:  2026-11-05
// Modified: 2026-11-05 (L2: shared budget headroom helper)
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P8 (D-P8-1),
//           docs/design/ux-s2-transfers-finances.md §2 (R1–R8) / §3.1–§3.2, Transfers #31 FR-TX-025,
//           Code Standards #20
// Purpose:  The Transfers screen's immutable view model: window, budget, squad capacity, default terms,
//           and one row per league player with its estimated value.

using System.Collections.ObjectModel;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.ClientApp
{
    /// <summary>One league player as the Transfers screen lists him (§5-P8).</summary>
    public readonly struct TransferPlayerRow
    {
        /// <summary>The player's stable id (#31 keeps it across transfers, ERR-031-005).</summary>
        public readonly int PlayerId;

        /// <summary>The club that currently holds his registration.</summary>
        public readonly int ClubId;

        /// <summary>That club's display name.</summary>
        public readonly string ClubName;

        /// <summary>"First Last" display name.</summary>
        public readonly string Name;

        /// <summary>The player's position.</summary>
        public readonly PlayerPosition Position;

        /// <summary>Age in whole years.</summary>
        public readonly int Age;

        /// <summary>#28 current ability.</summary>
        public readonly int CurrentAbility;

        /// <summary>False while #41 rules him unavailable (an injury travels with a transfer).</summary>
        public readonly bool IsAvailable;

        /// <summary>
        /// #31's base value (<c>PlayerValuation.ValuePlayer</c>), shown as "estimated value" (D-P8-1). The
        /// club's acceptance price also applies its positional need, so it can differ.
        /// </summary>
        public readonly long EstimatedValue;

        /// <summary>True when the managed club holds him (a sale candidate).</summary>
        public readonly bool IsManaged;

        /// <summary>True when he holds a managed #31 contract (managed players may be un-contracted, ERR-031-009).</summary>
        public readonly bool HasContract;

        /// <summary>Contract wage per period; 0 without a contract.</summary>
        public readonly long ContractWagePerPeriod;

        /// <summary>Contract seasons remaining; 0 without a contract.</summary>
        public readonly int ContractSeasons;

        /// <summary>Creates one row.</summary>
        public TransferPlayerRow(
            int playerId, int clubId, string clubName, string name, PlayerPosition position, int age,
            int currentAbility, bool isAvailable, long estimatedValue, bool isManaged, bool hasContract,
            long contractWagePerPeriod, int contractSeasons)
        {
            PlayerId = playerId;
            ClubId = clubId;
            ClubName = clubName;
            Name = name;
            Position = position;
            Age = age;
            CurrentAbility = currentAbility;
            IsAvailable = isAvailable;
            EstimatedValue = estimatedValue;
            IsManaged = isManaged;
            HasContract = hasContract;
            ContractWagePerPeriod = contractWagePerPeriod;
            ContractSeasons = contractSeasons;
        }
    }

    /// <summary>One league club, for the sale's buyer picker and the list's club filter.</summary>
    public readonly struct TransferClubRow
    {
        /// <summary>The club id.</summary>
        public readonly int ClubId;

        /// <summary>The club's display name.</summary>
        public readonly string Name;

        /// <summary>True for the managed club (never a counterparty).</summary>
        public readonly bool IsManaged;

        /// <summary>
        /// Players the club holds (#28 roster count; 0 when the career carries no #31 state). A sale needs the
        /// buyer below <see cref="TransferMarketViewModel.SquadCapacity"/>: #31's F5 check is against the
        /// destination, so a full buyer returns SquadFull.
        /// </summary>
        public readonly int SquadCount;

        /// <summary>Creates one club row.</summary>
        public TransferClubRow(int clubId, string name, bool isManaged, int squadCount)
        {
            ClubId = clubId;
            Name = name;
            IsManaged = isManaged;
            SquadCount = squadCount;
        }
    }

    /// <summary>
    /// The Transfers screen's view model (§5-P8). A <c>default</c> value has <see cref="HasCareer"/> false.
    /// <see cref="IsTransfersWired"/> is false for a career with no #31 state; every transfer field is then
    /// zero and the lists are empty.
    /// </summary>
    public readonly struct TransferMarketViewModel
    {
        /// <summary>True when a career is active.</summary>
        public readonly bool HasCareer;

        /// <summary>True when the career carries #31 state (always, for a career started or resumed by the client).</summary>
        public readonly bool IsTransfersWired;

        /// <summary>The managed club's id.</summary>
        public readonly int ManagedClubId;

        /// <summary>The current world day.</summary>
        public readonly uint WorldDay;

        /// <summary>True when offers may be made today.</summary>
        public readonly bool IsWindowOpen;

        /// <summary>The active window's first day (inclusive).</summary>
        public readonly uint WindowOpenDay;

        /// <summary>The active window's last day (inclusive).</summary>
        public readonly uint WindowCloseDay;

        /// <summary>The season's #40 transfer ceiling.</summary>
        public readonly long TransferCeiling;

        /// <summary>Fees already committed in this window.</summary>
        public readonly long CommittedSpend;

        /// <summary>What a buy may still cost: ceiling minus committed spend, the check #31 makes (<see cref="TransferBudgetRule"/>).</summary>
        public readonly long RemainingBudget;

        /// <summary>Players the managed club holds.</summary>
        public readonly int ManagedSquadCount;

        /// <summary>The squad capacity a buy needs room under (<c>CLUB_SQUAD_SIZE</c>).</summary>
        public readonly int SquadCapacity;

        /// <summary>The default offered wage per period (#31 <c>DefaultWagePerPeriod</c>).</summary>
        public readonly long DefaultWagePerPeriod;

        /// <summary>The default offered contract length in seasons (#31 <c>DefaultContractSeasons</c>).</summary>
        public readonly int DefaultContractSeasons;

        /// <summary>Every league player, clubs in ascending id order and each squad in its roster order.</summary>
        public readonly ReadOnlyCollection<TransferPlayerRow> Players;

        /// <summary>Every league club in ascending id order.</summary>
        public readonly ReadOnlyCollection<TransferClubRow> Clubs;

        /// <summary>Creates a populated view model.</summary>
        public TransferMarketViewModel(
            bool isTransfersWired, int managedClubId, uint worldDay, bool isWindowOpen, uint windowOpenDay,
            uint windowCloseDay, long transferCeiling, long committedSpend, int managedSquadCount,
            int squadCapacity, long defaultWagePerPeriod, int defaultContractSeasons,
            ReadOnlyCollection<TransferPlayerRow> players, ReadOnlyCollection<TransferClubRow> clubs)
        {
            HasCareer = true;
            IsTransfersWired = isTransfersWired;
            ManagedClubId = managedClubId;
            WorldDay = worldDay;
            IsWindowOpen = isWindowOpen;
            WindowOpenDay = windowOpenDay;
            WindowCloseDay = windowCloseDay;
            TransferCeiling = transferCeiling;
            CommittedSpend = committedSpend;
            RemainingBudget = TransferBudgetRule.Remaining(transferCeiling, committedSpend);
            ManagedSquadCount = managedSquadCount;
            SquadCapacity = squadCapacity;
            DefaultWagePerPeriod = defaultWagePerPeriod;
            DefaultContractSeasons = defaultContractSeasons;
            Players = players;
            Clubs = clubs;
        }

        /// <summary>
        /// True when a buy could fit the managed squad. A sale is limited by the buyer's squad instead
        /// (<see cref="TransferClubRow.SquadCount"/>).
        /// </summary>
        public bool HasSquadRoom => HasCareer && ManagedSquadCount < SquadCapacity;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-05 | —      | Initial creation (§5-P8): Transfers view model, player and club rows. |
// | 1.1     | 2026-11-05 | —      | H2: TransferClubRow.SquadCount so a sale can show a full buyer; HasSquadRoom doc no longer says a sale is never squad-limited. |
// | 1.2     | 2026-11-05 | —      | L2: RemainingBudget through the shared TransferBudgetRule helper. |
#endregion
