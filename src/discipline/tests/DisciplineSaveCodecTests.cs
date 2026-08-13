// File:     src/discipline/tests/DisciplineSaveCodecTests.cs
// Created:  2026-08-13
// Modified: 2026-08-13
// Author:   —
// Spec:     Discipline & Suspensions #44 §4.4 + Appendix B (the sub-blob layout) / FR-DC-014/015/016;
//           F3; ERR-044-001 (the magic-before-version correction); §5 T-DC-SAV-001, T-DC-DET-001;
//           Code Standards #20
// Purpose:  Unit tests for DisciplineSaveCodec — round-trip field identity (empty and populated
//           multi-entry, multi-competition states), the pinned magic-then-version-then-count byte
//           layout, proof that the magic actually refuses a foreign sub-blob, and every F3 fail-loud
//           gate (magic, version, every truncation length, trailing bytes, non-ascending keys,
//           duplicate keys, negative values, an all-zero row) plus two-run determinism.

using System;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.Discipline.Tests
{
    /// <summary>Tests for <see cref="DisciplineSaveCodec"/>.</summary>
    [TestFixture]
    internal sealed class DisciplineSaveCodecTests
    {
        private static DisciplineEntry Row(int playerId, int competitionId, int yellows, int ban) =>
            new DisciplineEntry(playerId, competitionId, yellows, ban);

        private static DisciplineState PopulatedState()
        {
            // Multi-entry, multi-competition, in scrambled insertion order — DisciplineState.Upsert
            // (via DisciplineState.FromEntries below where noted) is what canonicalizes it.
            return DisciplineState.FromEntries(new[]
            {
                Row(5, 0, 3, 0),
                Row(5, 1, 0, 2),
                Row(30, 0, 1, 1),
                Row(60, 0, 0, 4),
            });
        }

        // ── Round-trip ─────────────────────────────────────────────────────────────

        [Test]
        public void RoundTrip_EmptyState_IsWellFormedAndComesBackEmpty()
        {
            var state = new DisciplineState();

            byte[] blob = DisciplineSaveCodec.Encode(state);
            DisciplineState decoded = DisciplineSaveCodec.Decode(blob);

            Assert.AreEqual(0, decoded.Count);
        }

        [Test]
        public void RoundTrip_PopulatedMultiEntryMultiCompetitionState_IsFieldIdentical()
        {
            DisciplineState state = PopulatedState();

            DisciplineState decoded = DisciplineSaveCodec.Decode(DisciplineSaveCodec.Encode(state));

            Assert.AreEqual(state.Count, decoded.Count);
            for (int i = 0; i < state.Count; i++)
            {
                DisciplineEntry expected = state.EntryAt(i);
                DisciplineEntry actual = decoded.EntryAt(i);
                Assert.AreEqual(expected.PlayerId, actual.PlayerId, $"entry {i} PlayerId");
                Assert.AreEqual(expected.CompetitionId, actual.CompetitionId, $"entry {i} CompetitionId");
                Assert.AreEqual(expected.Yellows, actual.Yellows, $"entry {i} Yellows");
                Assert.AreEqual(expected.BanMatchesRemaining, actual.BanMatchesRemaining, $"entry {i} BanMatchesRemaining");
            }
        }

        // ── Byte layout, pinned explicitly ────────────────────────────────────────

        [Test]
        public void Encode_ByteLayout_MagicThenVersionThenLength()
        {
            DisciplineState state = PopulatedState();
            byte[] blob = DisciplineSaveCodec.Encode(state);

            int o = 0;
            uint magic = CanonicalSerializer.ReadU32(blob, ref o);
            uint version = CanonicalSerializer.ReadU32(blob, ref o);

            Assert.AreEqual(DisciplineConstants.DISCIPLINE_SAVE_MAGIC, magic, "bytes [0,4) must be the magic");
            Assert.AreEqual(DisciplineConstants.DISCIPLINE_SAVE_FORMAT_VERSION, version, "bytes [4,8) must be the version");
            Assert.AreEqual(12 + 16 * state.Count, blob.Length,
                "header (magic+version+count = 12 bytes) + 16 bytes per entry (4 i32 fields), nothing else");
        }

        [Test]
        public void Encode_EmptyState_IsExactlyTheTwelveByteHeader()
        {
            byte[] blob = DisciplineSaveCodec.Encode(new DisciplineState());
            Assert.AreEqual(12, blob.Length);
        }

        // ── The magic actually does its job ───────────────────────────────────────

        [Test]
        public void Decode_ForeignBlobWithADifferentMagic_Throws()
        {
            // discipline-tests.gen.csproj references only discipline, event-system, player-database,
            // deterministic-sim and project-constants — NOT injuries-medical or training-system — so a
            // real MedicalSaveCodec/TrainingSaveCodec block is unreachable from this assembly. This
            // hand-builds a blob with a DIFFERENT magic but an otherwise well-formed
            // version|entryCount|entries… shape (the exact byte layout #44's own header warns every
            // sub-blob under the season frame shares), to prove the magic check is what refuses it —
            // not a version mismatch, not a bounds failure.
            byte[] blob = new byte[12];
            int o = 0;
            CanonicalSerializer.WriteU32(blob, ref o, 0x54524149u);   // an arbitrary foreign magic ("TRAI"-ish, NOT DISC)
            CanonicalSerializer.WriteU32(blob, ref o, DisciplineConstants.DISCIPLINE_SAVE_FORMAT_VERSION);
            CanonicalSerializer.WriteU32(blob, ref o, 0u);   // entryCount 0 — otherwise well-formed

            Assert.AreNotEqual(DisciplineConstants.DISCIPLINE_SAVE_MAGIC, 0x54524149u,
                "the foreign magic chosen for this test must actually differ from DISC's");
            Assert.Throws<InvalidOperationException>(() => DisciplineSaveCodec.Decode(blob));
        }

        // ── F3 refusals ────────────────────────────────────────────────────────────

        [Test]
        public void Decode_WrongMagic_Throws()
        {
            byte[] blob = DisciplineSaveCodec.Encode(PopulatedState());
            int o = 0;
            CanonicalSerializer.WriteU32(blob, ref o, DisciplineConstants.DISCIPLINE_SAVE_MAGIC + 1);

            Assert.Throws<InvalidOperationException>(() => DisciplineSaveCodec.Decode(blob));
        }

        [Test]
        public void Decode_WrongVersion_Throws()
        {
            byte[] blob = DisciplineSaveCodec.Encode(PopulatedState());
            int o = 4;   // past the magic
            CanonicalSerializer.WriteU32(blob, ref o, DisciplineConstants.DISCIPLINE_SAVE_FORMAT_VERSION + 1);

            Assert.Throws<InvalidOperationException>(() => DisciplineSaveCodec.Decode(blob));
        }

        [Test]
        public void Decode_EveryTruncationLength_Throws()
        {
            byte[] full = DisciplineSaveCodec.Encode(PopulatedState());
            Assert.Greater(full.Length, 1, "precondition: there must be something to truncate");

            for (int len = 1; len < full.Length; len++)
            {
                var truncated = new byte[len];
                Array.Copy(full, truncated, len);
                Assert.Throws<InvalidOperationException>(
                    () => DisciplineSaveCodec.Decode(truncated),
                    $"a {len}-byte prefix of a {full.Length}-byte blob must be refused as truncated");
            }
        }

        [Test]
        public void Decode_TrailingBytes_Throws()
        {
            byte[] blob = DisciplineSaveCodec.Encode(PopulatedState());
            var padded = new byte[blob.Length + 1];
            Array.Copy(blob, padded, blob.Length);

            Assert.Throws<InvalidOperationException>(() => DisciplineSaveCodec.Decode(padded));
        }

        [Test]
        public void Decode_NonAscendingKeys_Throws()
        {
            byte[] blob = DisciplineSaveCodec.Encode(DisciplineState.FromEntries(new[]
            {
                Row(1, 0, 1, 0),
                Row(2, 0, 1, 0),
            }));
            // First entry starts right after the 12-byte header: playerId is the first i32.
            int o = 12;
            CanonicalSerializer.WriteI32(blob, ref o, 5);   // 5 then 2 — no longer ascending

            Assert.Throws<InvalidOperationException>(() => DisciplineSaveCodec.Decode(blob));
        }

        [Test]
        public void Decode_DuplicateKeys_Throws()
        {
            byte[] blob = DisciplineSaveCodec.Encode(DisciplineState.FromEntries(new[]
            {
                Row(1, 0, 1, 0),
                Row(2, 0, 1, 0),
            }));
            int o = 12;   // first entry's playerId
            CanonicalSerializer.WriteI32(blob, ref o, 2);   // now (2,0) then (2,0) — a duplicate key

            Assert.Throws<InvalidOperationException>(() => DisciplineSaveCodec.Decode(blob));
        }

        [Test]
        public void Decode_NegativeYellows_Throws()
        {
            byte[] blob = DisciplineSaveCodec.Encode(DisciplineState.FromEntries(new[] { Row(1, 0, 1, 0) }));
            int o = 12 + 4 + 4;   // playerId, competitionId, then yellows
            CanonicalSerializer.WriteI32(blob, ref o, -1);

            Assert.Throws<InvalidOperationException>(() => DisciplineSaveCodec.Decode(blob));
        }

        [Test]
        public void Decode_NegativeBanMatchesRemaining_Throws()
        {
            byte[] blob = DisciplineSaveCodec.Encode(DisciplineState.FromEntries(new[] { Row(1, 0, 0, 1) }));
            int o = 12 + 4 + 4 + 4;   // playerId, competitionId, yellows, then banMatchesRemaining
            CanonicalSerializer.WriteI32(blob, ref o, -1);

            Assert.Throws<InvalidOperationException>(() => DisciplineSaveCodec.Decode(blob));
        }

        [Test]
        public void Decode_AllZeroRow_Throws()
        {
            byte[] blob = DisciplineSaveCodec.Encode(DisciplineState.FromEntries(new[] { Row(1, 0, 1, 0) }));
            int o = 12 + 4 + 4;   // yellows field
            CanonicalSerializer.WriteI32(blob, ref o, 0);   // yellows -> 0; banMatchesRemaining already 0 -> (0,0)

            Assert.Throws<InvalidOperationException>(() => DisciplineSaveCodec.Decode(blob),
                "FR-DC-017: a stored (0,0) row is corrupt — an absent row is the ONE representation of clean.");
        }

        [Test]
        public void Encode_NullState_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => DisciplineSaveCodec.Encode(null));
        }

        [Test]
        public void Decode_NullBlob_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => DisciplineSaveCodec.Decode(null));
        }

        // ── Determinism ────────────────────────────────────────────────────────────

        [Test]
        public void Encode_TwoEqualStates_ProduceIdenticalBytes()
        {
            DisciplineState a = DisciplineState.FromEntries(new[] { Row(1, 0, 2, 0), Row(9, 0, 0, 3) });
            DisciplineState b = DisciplineState.FromEntries(new[] { Row(1, 0, 2, 0), Row(9, 0, 0, 3) });

            CollectionAssert.AreEqual(DisciplineSaveCodec.Encode(a), DisciplineSaveCodec.Encode(b));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-13 | —      | Initial suite (#44 T-phase tests): round-trip field identity      |
// |         |            |        | (empty + populated multi-entry, multi-competition), the pinned    |
// |         |            |        | magic/version/length byte layout, a hand-built foreign-magic      |
// |         |            |        | blob proving the magic gate does its job (the sibling codecs are  |
// |         |            |        | unreachable from this asmdef), every F3 fail-loud gate incl. an   |
// |         |            |        | exhaustive truncation-length loop, and two-run determinism.       |
#endregion
