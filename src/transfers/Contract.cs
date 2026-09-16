// ============================================================================
// File:     src/transfers/Contract.cs
// Created:  2026-09-12
// Modified: 2026-09-12
// Author:   —
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #31 §2.3, §3.3 (minimal contract state)
// Purpose:  Declares the integer-only wage-and-length contract record owned by #31.
// ============================================================================

using System;

namespace TacticalDirector.Transfers
{
    /// <summary>Minimal durable player contract; deep-tier clauses append later without replacing this identity.</summary>
    public struct Contract
    {
        /// <summary>Current club-scoped player identifier.</summary>
        public int PlayerId;

        /// <summary>Non-negative wage recorded for the contract; not posted to #40 at minimal tier.</summary>
        public long WagePerPeriod;

        /// <summary>Strictly positive remaining contract length in seasons.</summary>
        public int LengthSeasons;

        /// <summary>Creates a validated minimal contract.</summary>
        public Contract(int playerId, long wagePerPeriod, int lengthSeasons)
        {
            if (playerId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerId), playerId, "PlayerId must be non-negative (F6).");
            }

            if (wagePerPeriod < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(wagePerPeriod), wagePerPeriod, "WagePerPeriod must be non-negative (F6).");
            }

            if (lengthSeasons <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lengthSeasons), lengthSeasons, "LengthSeasons must be positive (F7).");
            }

            PlayerId = playerId;
            WagePerPeriod = wagePerPeriod;
            LengthSeasons = lengthSeasons;
        }

        internal static void Validate(in Contract contract)
        {
            if (contract.PlayerId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(contract), contract.PlayerId, "Contract PlayerId must be non-negative (F6).");
            }

            if (contract.WagePerPeriod < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(contract), contract.WagePerPeriod, "Contract WagePerPeriod must be non-negative (F6).");
            }

            if (contract.LengthSeasons <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(contract), contract.LengthSeasons, "Contract LengthSeasons must be positive (F7).");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 minimal contract value. |
#endregion
