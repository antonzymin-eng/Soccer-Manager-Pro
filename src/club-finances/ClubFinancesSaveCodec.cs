// ============================================================================
// File:     src/club-finances/ClubFinancesSaveCodec.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   Codex / Anton
// Specs:    Spec #16 §3.2.4.1 (CanonicalSerializer / save framing helpers)
//           Spec #20 §3.6.2 (style & docs governance)
//           Spec #40 FR-FN-020/021/022, §4.4 (finance persistence)
// Purpose:  Encodes/decodes #40's independently version-gated, canonical ClubId-keyed finance sub-blob.
// ============================================================================

using System;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.ClubFinances
{
    /// <summary>
    /// Canonical codec for the #40 finance block. The leading magic identifies this format before the
    /// version is considered; records are written in strictly ascending ClubId order.
    /// </summary>
    public static class ClubFinancesSaveCodec
    {
        /// <summary>Encodes a canonical finance block without mutating the caller's record array.</summary>
        /// <param name="entries">ClubId-keyed finance values; input order is irrelevant.</param>
        /// <returns>A self-identifying, independently versioned canonical byte block.</returns>
        public static byte[] Encode(ClubFinanceEntry[] entries)
        {
            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            int[] clubIds = new int[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                clubIds[i] = entries[i].ClubId;
                ClubFinances finances = entries[i].Finances;
                ClubFinances.ValidateCoherence(in finances);
            }

            int[] order = SaveBlobFramingHelpers.CanonicalOrder(
                clubIds,
                "finance",
                "club id",
                "FR-FN-021");

            int length = checked(
                ClubFinancesConstants.FINANCE_SAVE_HEADER_BYTES
                + checked(entries.Length * ClubFinancesConstants.FINANCE_SAVE_RECORD_BYTES));
            byte[] blob = new byte[length];
            int o = 0;

            CanonicalSerializer.WriteU32(blob, ref o, ClubFinancesConstants.FINANCE_SAVE_MAGIC);
            CanonicalSerializer.WriteU32(blob, ref o, ClubFinancesConstants.FINANCE_SAVE_FORMAT_VERSION);
            CanonicalSerializer.WriteU32(blob, ref o, (uint)entries.Length);

            for (int i = 0; i < order.Length; i++)
            {
                ClubFinanceEntry entry = entries[order[i]];
                CanonicalSerializer.WriteI32(blob, ref o, entry.ClubId);
                WriteFinances(blob, ref o, in entry.Finances);
            }

            return blob;
        }

        /// <summary>Decodes and validates one complete canonical finance block.</summary>
        /// <param name="blob">The exact finance sub-blob; trailing bytes are invalid.</param>
        /// <returns>ClubId-keyed records in canonical ascending ClubId order.</returns>
        public static ClubFinanceEntry[] Decode(byte[] blob)
        {
            if (blob == null)
            {
                throw new ArgumentNullException(nameof(blob));
            }

            int o = 0;
            SaveBlobFramingHelpers.Require(o, 8, blob.Length, "Finance save", "magic/version");

            uint magic = CanonicalSerializer.ReadU32(blob, ref o);
            if (magic != ClubFinancesConstants.FINANCE_SAVE_MAGIC)
            {
                throw new ArgumentException(
                    "Finance save magic mismatch: got 0x" + magic.ToString("X8") +
                    ", expected 0x" + ClubFinancesConstants.FINANCE_SAVE_MAGIC.ToString("X8") + ".",
                    nameof(blob));
            }

            uint version = CanonicalSerializer.ReadU32(blob, ref o);
            if (version != ClubFinancesConstants.FINANCE_SAVE_FORMAT_VERSION)
            {
                throw new ArgumentException(
                    "Finance save format version " + version + " is unsupported; expected " +
                    ClubFinancesConstants.FINANCE_SAVE_FORMAT_VERSION + ".",
                    nameof(blob));
            }

            int count = SaveBlobFramingHelpers.ReadCount(
                blob,
                ref o,
                blob.Length,
                ClubFinancesConstants.FINANCE_SAVE_RECORD_BYTES,
                "Finance save",
                "club");

            ClubFinanceEntry[] entries = new ClubFinanceEntry[count];
            long previousClubId = (long)int.MinValue - 1L;

            for (int i = 0; i < count; i++)
            {
                SaveBlobFramingHelpers.Require(
                    o,
                    ClubFinancesConstants.FINANCE_SAVE_RECORD_BYTES,
                    blob.Length,
                    "Finance save",
                    "club record");
                int clubId = CanonicalSerializer.ReadI32(blob, ref o);
                SaveBlobFramingHelpers.RequireAscending(clubId, ref previousClubId, "Finance save", "club id", i);

                ClubFinances finances = ReadFinances(blob, ref o);
                ClubFinances.ValidateCoherence(in finances);
                entries[i] = new ClubFinanceEntry(clubId, in finances);
            }

            if (o != blob.Length)
            {
                throw new InvalidOperationException(
                    "Finance save has " + (blob.Length - o) + " trailing byte(s) after the canonical block.");
            }

            return entries;
        }

        private static void WriteFinances(byte[] blob, ref int o, in ClubFinances finances)
        {
            ClubFinances.ValidateCoherence(in finances);
            CanonicalSerializer.WriteI64(blob, ref o, finances.Balance);
            CanonicalSerializer.WriteI64(blob, ref o, finances.TransferBudget);
            CanonicalSerializer.WriteI64(blob, ref o, finances.WageBudget);
            CanonicalSerializer.WriteI64(blob, ref o, finances.WageBillAggregate);
            CanonicalSerializer.WriteI64(blob, ref o, finances.SeasonRevenueAccrued);
            CanonicalSerializer.WriteI64(blob, ref o, finances.FfpBalanceWindow);
        }

        private static ClubFinances ReadFinances(byte[] blob, ref int o)
        {
            return new ClubFinances
            {
                Balance = unchecked((long)CanonicalSerializer.ReadU64(blob, ref o)),
                TransferBudget = unchecked((long)CanonicalSerializer.ReadU64(blob, ref o)),
                WageBudget = unchecked((long)CanonicalSerializer.ReadU64(blob, ref o)),
                WageBillAggregate = unchecked((long)CanonicalSerializer.ReadU64(blob, ref o)),
                SeasonRevenueAccrued = unchecked((long)CanonicalSerializer.ReadU64(blob, ref o)),
                FfpBalanceWindow = unchecked((long)CanonicalSerializer.ReadU64(blob, ref o))
            };
        }
    }
}

#region VersionHistory
// Version | Date       | Author        | Change
// --------|------------|---------------|----------------------------------------------
// 1.0     | 2026-09-04 | Codex / Anton | Initial #40 T1a canonical finance sub-blob codec.
// 1.1     | 2026-09-04 | Codex / Anton | Critique: catalogue framing widths; unchecked signed restores.
// 1.2     | 2026-09-04 | Codex / Anton | Critique: remove unsupported ClubId sign restriction.
#endregion
