// ============================================================================
// File:     src/transfers/tests/TransfersT1Tests.cs
// Created:  2026-10-18
// Modified: 2026-10-19
// Author:   —
// Specs:    Spec #31 §5.5 T-TX-DET-001, §5.6 T-TX-FAIL-001, Appendix B (as corrected by ERR-031-003)
// Purpose:  Locks the T1 transfers sub-blob: a pinned golden byte vector, field-identical round-trip of
//           non-default mid-window state, and every F3/F6/F7 decode refusal.
// ============================================================================

using System;

using NUnit.Framework;

using TacticalDirector.ClubFinances;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.Transfers.Tests
{
    [TestFixture]
    public sealed class TransfersT1Tests
    {
        private const int MANAGED_CLUB_ID = 4;
        private const int FIRST_MANAGED_PLAYER_ID = MANAGED_CLUB_ID * PlayerDatabaseConstants.CLUB_SQUAD_SIZE;

        // ── Golden vector + layout ────────────────────────────────────────────────────

        [Test]
        public void Encode_PinsTheAppendixBLayout_ByteForByte()
        {
            TransfersState state = TransfersState.Restore(
                MANAGED_CLUB_ID,
                1234,
                new TransferWindow(100, 145),
                new[] { new Contract(FIRST_MANAGED_PLAYER_ID + 1, 1000, 3) });

            byte[] expected =
            {
                0x52, 0x45, 0x46, 0x58,                         // magic "XFER" (ERR-031-003), before the version
                0x01, 0x00, 0x00, 0x00,                         // TRANSFERS_SAVE_FORMAT_VERSION
                0x01, 0x00, 0x00, 0x00,                         // clubCount
                0x04, 0x00, 0x00, 0x00,                         // ClubId 4
                0xD2, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // CommittedSpendThisWindow 1234
                0x64, 0x00, 0x00, 0x00,                         // OpenWorldDay 100
                0x91, 0x00, 0x00, 0x00,                         // CloseWorldDay 145
                0x01, 0x00, 0x00, 0x00,                         // contractCount
                0x65, 0x00, 0x00, 0x00,                         // PlayerId 101
                0xE8, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // WagePerPeriod 1000
                0x03, 0x00, 0x00, 0x00                          // LengthSeasons 3
            };

            Assert.That(TransfersSaveCodec.Encode(state), Is.EqualTo(expected));
        }

        [Test]
        public void Encode_Unwired_WritesAWellFormedZeroClubBlock_ThatDecodesToNull()
        {
            byte[] blob = TransfersSaveCodec.Encode(null);

            Assert.That(blob, Is.EqualTo(new byte[]
            {
                0x52, 0x45, 0x46, 0x58, 0x01, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            }));
            Assert.That(TransfersSaveCodec.Decode(blob), Is.Null);
        }

        [Test]
        public void Encode_AWiredStateWithNoContracts_StillWritesItsClubRecord()
        {
            // clubCount is a wiring fact (ERR-030-038's lesson): a career whose contracts have all expired is
            // still wired, and must not encode as the unwired zero-club block.
            var state = new TransfersState(MANAGED_CLUB_ID, new TransferWindow(10, 20));

            TransfersState decoded = TransfersSaveCodec.Decode(TransfersSaveCodec.Encode(state));

            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded.ManagedClubId, Is.EqualTo(MANAGED_CLUB_ID));
            Assert.That(decoded.ContractCount, Is.EqualTo(0));
        }

        // ── T-TX-DET-001: field-identical mid-window round-trip ──────────────────────

        [Test]
        public void RoundTrip_NonDefaultMidWindowState_IsFieldIdentical()
        {
            TransfersState state = TransfersState.Restore(
                MANAGED_CLUB_ID,
                long.MaxValue - 7,
                new TransferWindow(365, 410),
                new[]
                {
                    new Contract(FIRST_MANAGED_PLAYER_ID + 24, 0, 1),
                    new Contract(FIRST_MANAGED_PLAYER_ID, long.MaxValue, int.MaxValue),
                    new Contract(FIRST_MANAGED_PLAYER_ID + 9, 5_250, 2)
                });

            TransfersState decoded = TransfersSaveCodec.Decode(TransfersSaveCodec.Encode(state));

            Assert.That(decoded.ManagedClubId, Is.EqualTo(state.ManagedClubId));
            Assert.That(decoded.CommittedSpendThisWindow, Is.EqualTo(state.CommittedSpendThisWindow));
            Assert.That(decoded.ActiveWindow.OpenWorldDay, Is.EqualTo(365u));
            Assert.That(decoded.ActiveWindow.CloseWorldDay, Is.EqualTo(410u));
            Contract[] before = state.ContractsSnapshot();
            Contract[] after = decoded.ContractsSnapshot();
            Assert.That(after.Length, Is.EqualTo(3));
            for (int i = 0; i < before.Length; i++)
            {
                Assert.That(after[i].PlayerId, Is.EqualTo(before[i].PlayerId));
                Assert.That(after[i].WagePerPeriod, Is.EqualTo(before[i].WagePerPeriod));
                Assert.That(after[i].LengthSeasons, Is.EqualTo(before[i].LengthSeasons));
            }

            Assert.That(TransfersSaveCodec.Encode(decoded), Is.EqualTo(TransfersSaveCodec.Encode(state)));
        }

        [Test]
        public void Clone_IsIndependentOfItsSource()
        {
            TransfersState state = TransfersState.Restore(
                MANAGED_CLUB_ID, 50, new TransferWindow(1, 2), new[] { new Contract(FIRST_MANAGED_PLAYER_ID, 1, 1) });

            TransfersState clone = state.Clone();
            state.RemoveContract(FIRST_MANAGED_PLAYER_ID);

            Assert.That(clone.ContractCount, Is.EqualTo(1));
            Assert.That(clone.CommittedSpendThisWindow, Is.EqualTo(50));
        }

        // ── T-TX-FAIL-001 (F3) + value gates ─────────────────────────────────────────

        [Test]
        public void Decode_ASiblingSubBlob_IsRefusedAtTheMagic()
        {
            // The ERR-028-004 lesson: an empty finance block is the same 12-byte shape as a header and
            // version 1 — only the magic tells them apart.
            byte[] finance = ClubFinancesSaveCodec.Encode(Array.Empty<ClubFinanceEntry>());

            var e = Assert.Throws<InvalidOperationException>(() => TransfersSaveCodec.Decode(finance));
            StringAssert.Contains("magic", e.Message);
        }

        [Test]
        public void Decode_AnotherGeneration_IsRefused()
        {
            byte[] blob = TransfersSaveCodec.Encode(null);
            blob[4] = 2;

            var e = Assert.Throws<InvalidOperationException>(() => TransfersSaveCodec.Decode(blob));
            StringAssert.Contains("format version 2", e.Message);
        }

        [Test]
        public void Decode_TrailingByte_IsRefused()
        {
            byte[] good = TransfersSaveCodec.Encode(new TransfersState(MANAGED_CLUB_ID, new TransferWindow(1, 2)));
            byte[] padded = new byte[good.Length + 1];
            Array.Copy(good, padded, good.Length);

            var e = Assert.Throws<InvalidOperationException>(() => TransfersSaveCodec.Decode(padded));
            StringAssert.Contains("trailing", e.Message);
        }

        [Test]
        public void Decode_EveryTruncation_IsRefusedLoud()
        {
            byte[] good = TransfersSaveCodec.Encode(TransfersState.Restore(
                MANAGED_CLUB_ID, 9, new TransferWindow(3, 4), new[] { new Contract(FIRST_MANAGED_PLAYER_ID, 7, 2) }));

            for (int cut = 0; cut < good.Length; cut++)
            {
                byte[] truncated = new byte[cut];
                Array.Copy(good, truncated, cut);
                Assert.Throws<InvalidOperationException>(
                    () => TransfersSaveCodec.Decode(truncated), "truncated at " + cut + " byte(s)");
            }
        }

        [Test]
        public void Decode_MoreThanTheOneManagedClub_IsRefused()
        {
            byte[] blob = BlockWithHeader(clubCount: 2, extraBytes: 2 * TransfersConstants.TRANSFERS_SAVE_CLUB_RECORD_BYTES + 4);

            var e = Assert.Throws<InvalidOperationException>(() => TransfersSaveCodec.Decode(blob));
            StringAssert.Contains("managed club", e.Message);
        }

        [Test]
        public void Decode_ContractsWithoutAClub_AreRefused()
        {
            byte[] good = TransfersSaveCodec.Encode(null);
            byte[] blob = new byte[good.Length + TransfersConstants.TRANSFERS_SAVE_CONTRACT_RECORD_BYTES];
            Array.Copy(good, blob, good.Length);
            blob[12] = 1;                                   // contractCount = 1 after a zero clubCount
            blob[16] = 1;                                   // PlayerId 1
            blob[28] = 1;                                   // LengthSeasons 1

            var e = Assert.Throws<InvalidOperationException>(() => TransfersSaveCodec.Decode(blob));
            StringAssert.Contains("no managed club", e.Message);
        }

        [Test]
        public void Decode_ContractsOutOfOrder_AreRefused()
        {
            byte[] blob = TransfersSaveCodec.Encode(TransfersState.Restore(
                MANAGED_CLUB_ID, 0, new TransferWindow(1, 2),
                new[] { new Contract(FIRST_MANAGED_PLAYER_ID, 1, 1), new Contract(FIRST_MANAGED_PLAYER_ID + 1, 1, 1) }));
            int firstContract = TransfersConstants.TRANSFERS_SAVE_HEADER_BYTES + TransfersConstants.TRANSFERS_SAVE_CLUB_RECORD_BYTES + 4;
            int secondContract = firstContract + TransfersConstants.TRANSFERS_SAVE_CONTRACT_RECORD_BYTES;
            blob[secondContract] = blob[firstContract];   // duplicate the first id

            var e = Assert.Throws<InvalidOperationException>(() => TransfersSaveCodec.Decode(blob));
            StringAssert.Contains("ascending", e.Message);
        }

        [TestCase(0, 0, "LengthSeasons")]   // F7: default(Contract)'s zero length
        [TestCase(-1, 1, "WagePerPeriod")]  // F6: negative wage
        public void Decode_AMalformedContract_IsRefused(long wage, int length, string field)
        {
            byte[] blob = TransfersSaveCodec.Encode(TransfersState.Restore(
                MANAGED_CLUB_ID, 0, new TransferWindow(1, 2), new[] { new Contract(FIRST_MANAGED_PLAYER_ID, 5, 5) }));
            int contract = TransfersConstants.TRANSFERS_SAVE_HEADER_BYTES + TransfersConstants.TRANSFERS_SAVE_CLUB_RECORD_BYTES + 4;
            WriteI64(blob, contract + 4, wage);
            WriteI32(blob, contract + 12, length);

            var e = Assert.Throws<InvalidOperationException>(() => TransfersSaveCodec.Decode(blob));
            StringAssert.Contains(field, e.Message);
        }

        [Test]
        public void Decode_AWindowThatClosesBeforeItOpens_IsRefused()
        {
            byte[] blob = TransfersSaveCodec.Encode(new TransfersState(MANAGED_CLUB_ID, new TransferWindow(10, 20)));
            WriteI32(blob, TransfersConstants.TRANSFERS_SAVE_HEADER_BYTES + 16, 9);   // CloseWorldDay 9 < Open 10

            var e = Assert.Throws<InvalidOperationException>(() => TransfersSaveCodec.Decode(blob));
            StringAssert.Contains("CloseWorldDay", e.Message);
        }

        [Test]
        public void Decode_NegativeCommittedSpend_IsRefused()
        {
            byte[] blob = TransfersSaveCodec.Encode(new TransfersState(MANAGED_CLUB_ID, new TransferWindow(10, 20)));
            WriteI64(blob, TransfersConstants.TRANSFERS_SAVE_HEADER_BYTES + 4, -1);

            var e = Assert.Throws<InvalidOperationException>(() => TransfersSaveCodec.Decode(blob));
            StringAssert.Contains("CommittedSpendThisWindow", e.Message);
        }

        [Test]
        public void Decode_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => TransfersSaveCodec.Decode(null));
        }

        // ── helpers ──────────────────────────────────────────────────────────────────

        private static byte[] BlockWithHeader(uint clubCount, int extraBytes)
        {
            byte[] blob = new byte[TransfersConstants.TRANSFERS_SAVE_HEADER_BYTES + extraBytes];
            WriteI32(blob, 0, unchecked((int)TransfersConstants.TRANSFERS_SAVE_MAGIC));
            WriteI32(blob, 4, (int)TransfersConstants.TRANSFERS_SAVE_FORMAT_VERSION);
            WriteI32(blob, 8, (int)clubCount);
            return blob;
        }

        private static void WriteI32(byte[] blob, int offset, int value)
        {
            for (int i = 0; i < 4; i++)
            {
                blob[offset + i] = (byte)(value >> (8 * i));
            }
        }

        private static void WriteI64(byte[] blob, int offset, long value)
        {
            for (int i = 0; i < 8; i++)
            {
                blob[offset + i] = (byte)(value >> (8 * i));
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-10-18 | —      | #31 T1: golden vector, round-trip, and F3/F6/F7 decode-refusal locks. |
// | 1.1     | 2026-10-19 | —      | ERR-031-005: the codec-level managed-membership refusal is deleted — an id no longer encodes its club; membership is the season root's roster check (SeasonSaveTransfersTests). |
#endregion
