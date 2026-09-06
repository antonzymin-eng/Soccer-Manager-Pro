// ============================================================================
// File:     src/club-finances/tests/ClubFinancesSaveCodecTests.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   Codex / Anton
// Specs:    Spec #20 §3.6.2, §3.9.4 (style/docs; general-test allocation carve-out)
//           Spec #40 FR-FN-020/021/022, §4.4 (finance persistence)
// Purpose:  Locks canonical finance save framing, round-trip identity, and fail-loud corruption gates.
// ============================================================================
// §3.9.4 general-unit-test — allocation rules relaxed in test body

using System;

using NUnit.Framework;

namespace TacticalDirector.ClubFinances.Tests
{
    /// <summary>Acceptance coverage for #40's independently framed T1a finance sub-blob.</summary>
    [TestFixture]
    public sealed class ClubFinancesSaveCodecTests
    {
        /// <summary>Proves all six finance fields and signed balances round-trip field-identically.</summary>
        [Test]
        public void RoundTrip_PreservesEveryFinanceField()
        {
            ClubFinanceEntry[] input =
            {
                Entry(7, -500L, 10L, 20L, 30L, 40L, -50L),
                Entry(2, 900L, 100L, 200L, 300L, 400L, 500L)
            };

            byte[] encoded = ClubFinancesSaveCodec.Encode(input);
            ClubFinanceEntry[] decoded = ClubFinancesSaveCodec.Decode(encoded);

            Assert.That(decoded.Length, Is.EqualTo(2));
            AssertEntry(decoded[0], 2, 900L, 100L, 200L, 300L, 400L, 500L);
            AssertEntry(decoded[1], 7, -500L, 10L, 20L, 30L, 40L, -50L);
        }

        /// <summary>Proves input permutation does not change canonical bytes and Encode does not reorder the caller.</summary>
        [Test]
        public void Encode_IsCanonicalAndDoesNotMutateInputOrder()
        {
            ClubFinanceEntry a = Entry(9, 1L, 2L, 3L, 4L, 5L, 6L);
            ClubFinanceEntry b = Entry(3, 7L, 8L, 9L, 10L, 11L, 12L);
            ClubFinanceEntry[] first = { a, b };
            ClubFinanceEntry[] second = { b, a };

            byte[] firstBytes = ClubFinancesSaveCodec.Encode(first);
            byte[] secondBytes = ClubFinancesSaveCodec.Encode(second);

            CollectionAssert.AreEqual(firstBytes, secondBytes);
            Assert.That(first[0].ClubId, Is.EqualTo(9));
            Assert.That(first[1].ClubId, Is.EqualTo(3));
        }

        /// <summary>Locks the self-identifying format header before the version word.</summary>
        [Test]
        public void Encode_WritesMagicThenVersion()
        {
            byte[] encoded = ClubFinancesSaveCodec.Encode(Array.Empty<ClubFinanceEntry>());

            uint magic = ReadU32(encoded, 0);
            uint version = ReadU32(encoded, 4);
            uint count = ReadU32(encoded, 8);

            Assert.That(magic, Is.EqualTo(ClubFinancesConstants.FINANCE_SAVE_MAGIC));
            Assert.That(version, Is.EqualTo(ClubFinancesConstants.FINANCE_SAVE_FORMAT_VERSION));
            Assert.That(count, Is.Zero);
        }

        /// <summary>Proves sibling-format/confused input cannot be accepted merely because its version is 1.</summary>
        [Test]
        public void Decode_RejectsWrongMagicBeforePayloadInterpretation()
        {
            byte[] blob = ClubFinancesSaveCodec.Encode(Array.Empty<ClubFinanceEntry>());
            blob[0] ^= 0x01;

            Assert.Throws<ArgumentException>(() => ClubFinancesSaveCodec.Decode(blob));
        }

        /// <summary>Locks the independent format-version gate.</summary>
        [Test]
        public void Decode_RejectsUnknownVersion()
        {
            byte[] blob = ClubFinancesSaveCodec.Encode(Array.Empty<ClubFinanceEntry>());
            blob[4] = 2;

            Assert.Throws<ArgumentException>(() => ClubFinancesSaveCodec.Decode(blob));
        }

        /// <summary>Locks overflow-safe length-prefix bounds.</summary>
        [Test]
        public void Decode_RejectsImpossibleRecordCount()
        {
            byte[] blob = ClubFinancesSaveCodec.Encode(Array.Empty<ClubFinanceEntry>());
            blob[8] = 0xFF;
            blob[9] = 0xFF;
            blob[10] = 0xFF;
            blob[11] = 0x7F;

            Assert.Throws<InvalidOperationException>(() => ClubFinancesSaveCodec.Decode(blob));
        }

        /// <summary>Locks exact-consumption framing.</summary>
        [Test]
        public void Decode_RejectsTrailingBytes()
        {
            byte[] canonical = ClubFinancesSaveCodec.Encode(Array.Empty<ClubFinanceEntry>());
            byte[] withTail = new byte[canonical.Length + 1];
            Array.Copy(canonical, withTail, canonical.Length);

            Assert.Throws<InvalidOperationException>(() => ClubFinancesSaveCodec.Decode(withTail));
        }

        /// <summary>Locks canonical key uniqueness on encode.</summary>
        [Test]
        public void Encode_RejectsDuplicateClubIds()
        {
            ClubFinanceEntry[] entries =
            {
                Entry(4, 1L, 0L, 0L, 0L, 0L, 0L),
                Entry(4, 2L, 0L, 0L, 0L, 0L, 0L)
            };

            Assert.Throws<ArgumentException>(() => ClubFinancesSaveCodec.Encode(entries));
        }

        /// <summary>Locks F1 coherence at the persistence boundary.</summary>
        [Test]
        public void Encode_RejectsNegativeBudgetOrWageLiability()
        {
            ClubFinanceEntry[] entries =
            {
                Entry(1, 0L, -1L, 0L, 0L, 0L, 0L)
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => ClubFinancesSaveCodec.Encode(entries));
        }

        private static ClubFinanceEntry Entry(
            int clubId,
            long balance,
            long transferBudget,
            long wageBudget,
            long wageBill,
            long seasonRevenue,
            long ffpBalance)
        {
            ClubFinances finances = new ClubFinances
            {
                Balance = balance,
                TransferBudget = transferBudget,
                WageBudget = wageBudget,
                WageBillAggregate = wageBill,
                SeasonRevenueAccrued = seasonRevenue,
                FfpBalanceWindow = ffpBalance
            };
            return new ClubFinanceEntry(clubId, in finances);
        }

        private static void AssertEntry(
            ClubFinanceEntry entry,
            int clubId,
            long balance,
            long transferBudget,
            long wageBudget,
            long wageBill,
            long seasonRevenue,
            long ffpBalance)
        {
            Assert.That(entry.ClubId, Is.EqualTo(clubId));
            Assert.That(entry.Finances.Balance, Is.EqualTo(balance));
            Assert.That(entry.Finances.TransferBudget, Is.EqualTo(transferBudget));
            Assert.That(entry.Finances.WageBudget, Is.EqualTo(wageBudget));
            Assert.That(entry.Finances.WageBillAggregate, Is.EqualTo(wageBill));
            Assert.That(entry.Finances.SeasonRevenueAccrued, Is.EqualTo(seasonRevenue));
            Assert.That(entry.Finances.FfpBalanceWindow, Is.EqualTo(ffpBalance));
        }

        private static uint ReadU32(byte[] bytes, int offset)
        {
            return (uint)bytes[offset]
                | ((uint)bytes[offset + 1] << 8)
                | ((uint)bytes[offset + 2] << 16)
                | ((uint)bytes[offset + 3] << 24);
        }
    }
}

#region VersionHistory
// Version | Date       | Author        | Change
// --------|------------|---------------|----------------------------------------------
// 1.0     | 2026-09-04 | Codex / Anton | Initial #40 T1a save-codec acceptance coverage.
#endregion
