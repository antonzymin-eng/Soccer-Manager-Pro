// File:     src/client-app/ClubFinancesViewModel.cs
// Created:  2026-11-05
// Modified: 2026-11-05 (H1: contracted-wage saturation flag)
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P8, docs/design/ux-s2-transfers-finances.md
//           §2 (R1, R2, R9, R12) / §3.3, Club Finances #40 FR-FN-026, Code Standards #20
// Purpose:  The Club Finances screen's immutable, read-only view model.

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// The managed club's finances for the read-only Club Finances screen (§5-P8). A <c>default</c> value has
    /// <see cref="HasCareer"/> false.
    /// </summary>
    public readonly struct ClubFinancesViewModel
    {
        /// <summary>True when a career is active.</summary>
        public readonly bool HasCareer;

        /// <summary>The managed club's display name.</summary>
        public readonly string ClubName;

        /// <summary>#40 cash balance (may be negative).</summary>
        public readonly long Balance;

        /// <summary>#40 season transfer ceiling.</summary>
        public readonly long TransferBudget;

        /// <summary>Fees committed in the current #31 window (0 when #31 is not wired).</summary>
        public readonly long CommittedTransferSpend;

        /// <summary>#40 season wage ceiling.</summary>
        public readonly long WageBudget;

        /// <summary>
        /// #40's wage bill, passed through unchanged. It has no producer yet (#40 KD-5), so it reads 0 even
        /// with contracts in force; the screen labels it "not tracked yet" (Gate A R12).
        /// </summary>
        public readonly long WageBillAggregate;

        /// <summary>
        /// The sum of the managed club's #31 contract wages per period, saturated at <see cref="long.MaxValue"/>
        /// (see <see cref="IsContractedWagesSaturated"/>): #31 accepts any non-negative wage, so the exact sum
        /// need not fit a long.
        /// </summary>
        public readonly long ContractedWages;

        /// <summary>
        /// True when the exact contract-wage sum exceeds <see cref="long.MaxValue"/>, so
        /// <see cref="ContractedWages"/> is a lower bound rather than the total.
        /// </summary>
        public readonly bool IsContractedWagesSaturated;

        /// <summary>The managed club's #31 contract count.</summary>
        public readonly int ContractCount;

        /// <summary>Creates a populated view model.</summary>
        public ClubFinancesViewModel(
            string clubName, long balance, long transferBudget, long committedTransferSpend, long wageBudget,
            long wageBillAggregate, long contractedWages, int contractCount, bool isContractedWagesSaturated = false)
        {
            HasCareer = true;
            ClubName = clubName;
            Balance = balance;
            TransferBudget = transferBudget;
            CommittedTransferSpend = committedTransferSpend;
            WageBudget = wageBudget;
            WageBillAggregate = wageBillAggregate;
            ContractedWages = contractedWages;
            ContractCount = contractCount;
            IsContractedWagesSaturated = isContractedWagesSaturated;
        }

        /// <summary>Transfer budget still available this window.</summary>
        public long RemainingTransferBudget => TransferBudget - CommittedTransferSpend;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-05 | —      | Initial creation (§5-P8): read-only Club Finances view model. |
// | 1.1     | 2026-11-05 | —      | H1: IsContractedWagesSaturated; ContractedWages documented as saturating at long.MaxValue. |
#endregion
