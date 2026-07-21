// File:     src/match-engine/tests/MatchSaveManagerTests.cs
// Created:  2026-07-21
// Author:   —
// Spec:     On-disk match save file (docs/tracking/match-save-file-design.md) §5 acceptance;
//           Match Engine design note §5 Phase G-Phase 3; Code Standards #20
// Purpose:  Acceptance tests for the on-disk match save format — the disk analogue of the in-memory
//           G3 round-trip determinism (MatchSaveManager.Save -> Load reproduces an uninterrupted
//           run's digest chain, for the neutral path, a booking-before-save match, and a distinct-
//           squad match via ISquadProvider) plus the MatchSaveCodec fail-loud guards and the
//           MatchSaveManager fail-loud paths.

using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// On-disk save/load tests for <see cref="MatchSaveManager"/> + <see cref="MatchSaveCodec"/>. The
    /// central property is disk round-trip determinism (<see cref="AssertDiskRoundTripDeterministic"/>):
    /// a match Saved to a file and Loaded back continues the digest chain byte-for-byte — the same
    /// correctness contract as the in-memory reader, now through the real file frame + atomic I/O.
    /// </summary>
    [TestFixture]
    public sealed class MatchSaveManagerTests
    {
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;

        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "td-matchsave-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch (Exception) { }
        }

        private string TempPath(string name) => Path.Combine(_tempDir, name);

        // ── Disk round-trip determinism (G4) ──────────────────────────────────────

        /// <summary>
        /// Boots engine A with <paramref name="setup"/>, ticks it to N, Saves it to a file, then:
        /// (a) continues A for K ticks to build the reference digest chain, and (b) Loads a fresh engine
        /// C from the file and ticks it K times, asserting C's digest at every tick equals A's. C comes
        /// solely from the file — A is kept running only to produce the reference chain (design note G4).
        /// </summary>
        private void AssertDiskRoundTripDeterministic(
            Action<MatchEngine> setup, int n, int k, ISquadProvider squads = null)
        {
            MatchEngine a = new MatchEngine(MatchSeed);
            setup?.Invoke(a);
            for (int i = 0; i < n; i++) a.RunTick();
            Assert.AreEqual((ulong)n, a.CurrentTick);

            string path = TempPath("match.save");
            MatchSaveManager.Save(a, path);
            Assert.IsTrue(File.Exists(path), "Save must produce the destination file atomically.");

            var reference = new List<byte[]>(k);
            for (int i = 0; i < k; i++)
            {
                a.RunTick();
                reference.Add(a.CurrentSnapshotDigest);
            }

            MatchEngine c = MatchSaveManager.Load(path, squads);
            Assert.AreEqual((ulong)n, c.CurrentTick,
                "The loaded engine's clock must resume at the saved tick N.");

            for (int i = 0; i < k; i++)
            {
                c.RunTick();
                CollectionAssert.AreEqual(
                    reference[i], c.CurrentSnapshotDigest,
                    $"Disk round-trip digest diverged at tick {n + i + 1} — the on-disk save/load is not " +
                    "byte-identical to an uninterrupted run.");
            }
        }

        [Test]
        public void DiskRoundTrip_NeutralKickoff_IsDeterministic()
        {
            AssertDiskRoundTripDeterministic(setup: null, n: 200, k: 90);
        }

        [Test]
        public void DiskRoundTrip_BookingCursorBeforeSave_IsDeterministic()
        {
            // The KD-8 card-severity RNG cursor is cross-tick state (v17) that must survive the file.
            AssertDiskRoundTripDeterministic(
                setup: e => e.TestOnly_SetCardSeverityStreamCursor(rngCursor: 12345UL, actionOrdinal: 7UL),
                n: 150, k: 60);
        }

        [Test]
        public void DiskRoundTrip_DistinctSquad_IsDeterministic()
        {
            // A ConfigureSquads match's attribute records are not serialized; Load must re-project them
            // from the provider's rosters (Phase 2 / KD-4) — the same roster the save loaded, resolved by
            // ClubId (which is all the file carries).
            Squad home = DistinctSquad(1);
            Squad away = DistinctSquad(2);
            AssertDiskRoundTripDeterministic(
                setup: e => e.ConfigureSquads(home, away),
                n: 150, k: 60,
                squads: Provider(home, away));
        }

        // ── MatchSaveManager fail-loud ─────────────────────────────────────────────

        [Test]
        public void Load_MissingFile_Throws()
        {
            Assert.Throws<FileNotFoundException>(
                () => MatchSaveManager.Load(TempPath("does-not-exist.save")),
                "Loading a missing save file must surface the IO failure, not return a bogus engine.");
        }

        [Test]
        public void Load_CorruptFile_FailsLoud()
        {
            MatchEngine a = new MatchEngine(MatchSeed);
            for (int i = 0; i < 30; i++) a.RunTick();
            string path = TempPath("corrupt.save");
            MatchSaveManager.Save(a, path);

            // Truncate the file mid-blob — the codec's bound / trailing-byte guards must fail loud.
            byte[] bytes = File.ReadAllBytes(path);
            File.WriteAllBytes(path, new ArraySegment<byte>(bytes, 0, bytes.Length / 2).ToArray());

            Assert.Throws<InvalidOperationException>(
                () => MatchSaveManager.Load(path),
                "A truncated save file must fail loud.");
        }

        [Test]
        public void Load_DistinctSquadNoProvider_FailsLoud()
        {
            MatchEngine a = new MatchEngine(MatchSeed);
            a.ConfigureSquads(DistinctSquad(1), DistinctSquad(2));
            for (int i = 0; i < 30; i++) a.RunTick();
            string path = TempPath("distinct.save");
            MatchSaveManager.Save(a, path);

            Assert.Throws<NotSupportedException>(
                () => MatchSaveManager.Load(path),
                "A distinct-squad save Loaded without an ISquadProvider must fail loud (KD-4 / R4).");
        }

        [Test]
        public void Save_NullEngine_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => MatchSaveManager.Save(null, TempPath("x.save")));
        }

        [Test]
        public void Save_OverwritesExistingFile_Atomically()
        {
            MatchEngine a = new MatchEngine(MatchSeed);
            for (int i = 0; i < 20; i++) a.RunTick();
            string path = TempPath("overwrite.save");
            MatchSaveManager.Save(a, path);
            long firstLen = new FileInfo(path).Length;

            for (int i = 0; i < 40; i++) a.RunTick();
            Assert.DoesNotThrow(() => MatchSaveManager.Save(a, path),
                "Re-saving over an existing file must atomically replace it (File.Replace), not throw.");
            Assert.IsFalse(File.Exists(path + ".tmp"), "The temp file must not survive a successful save.");
            // A later-tick save is a different snapshot; the file was genuinely replaced.
            MatchEngine c = MatchSaveManager.Load(path);
            Assert.AreEqual((ulong)60, c.CurrentTick);
            Assert.Greater(firstLen, 0);
        }

        // ── MatchSaveCodec (in-memory) ─────────────────────────────────────────────

        private static (SnapshotHeader, SnapshotPayload) CaptureAt(int n)
        {
            MatchEngine a = new MatchEngine(MatchSeed);
            for (int i = 0; i < n; i++) a.RunTick();
            return (a.CaptureDurableHeader(), a.CaptureDurablePayload());
        }

        [Test]
        public void Codec_RoundTrips_SeedHeaderPayload()
        {
            (SnapshotHeader header, SnapshotPayload payload) = CaptureAt(40);

            byte[] blob = MatchSaveCodec.Encode(MatchSeed, header, payload);
            MatchSaveContents got = MatchSaveCodec.Decode(blob);

            Assert.AreEqual(MatchSeed, got.MatchSeed);
            Assert.AreEqual(header.SchemaVersion, got.Header.SchemaVersion);
            Assert.AreEqual(header.DigestVersion, got.Header.DigestVersion);
            Assert.AreEqual(header.Tick, got.Header.Tick);
            CollectionAssert.AreEqual(header.PrevSnapshotDigest, got.Header.PrevSnapshotDigest);
            CollectionAssert.AreEqual(header.CurrentSnapshotDigest, got.Header.CurrentSnapshotDigest);
            Assert.AreEqual(header.Cursor.Tick, got.Header.Cursor.Tick);
            Assert.AreEqual(header.Cursor.PhaseOrdinal, got.Header.Cursor.PhaseOrdinal);
            Assert.AreEqual(payload.BytesWritten, got.Payload.BytesWritten);
            for (int i = 0; i < payload.BytesWritten; i++)
            {
                Assert.AreEqual(payload.PayloadBytes[i], got.Payload.PayloadBytes[i], $"payload byte {i}");
            }

            // Fingerprint round-trips its 6 ValidateAgainst fields, so the reconstructed one matches the
            // live dev tuple (KD-3 / R3 — the on-disk KD-6 gate is a real check).
            Assert.IsNotNull(got.Header.Fingerprint);
            Assert.AreEqual(0,
                got.Header.Fingerprint.ValidateAgainst(EnvironmentFingerprint.CreateStage0Dev()),
                "The reconstructed fingerprint must validate against the live dev tuple (KD-3).");
        }

        [Test]
        public void Codec_NullFingerprint_RoundTripsToNull()
        {
            (SnapshotHeader header, SnapshotPayload payload) = CaptureAt(10);
            header.Fingerprint = null;

            MatchSaveContents got = MatchSaveCodec.Decode(MatchSaveCodec.Encode(MatchSeed, header, payload));
            Assert.IsNull(got.Header.Fingerprint, "A null fingerprint must round-trip to null (KD-3 flag byte).");
        }

        [Test]
        public void Codec_WrongFormatVersion_FailsLoud()
        {
            (SnapshotHeader header, SnapshotPayload payload) = CaptureAt(10);
            byte[] blob = MatchSaveCodec.Encode(MatchSeed, header, payload);
            blob[0] ^= 0xFF; // corrupt the leading format-version u32

            Assert.Throws<InvalidOperationException>(() => MatchSaveCodec.Decode(blob),
                "A format-version mismatch must fail loud (KD-1 — no Stage-0 migration).");
        }

        [Test]
        public void Codec_TrailingBytes_FailsLoud()
        {
            (SnapshotHeader header, SnapshotPayload payload) = CaptureAt(10);
            byte[] blob = MatchSaveCodec.Encode(MatchSeed, header, payload);
            var padded = new byte[blob.Length + 1];
            Array.Copy(blob, padded, blob.Length);

            Assert.Throws<InvalidOperationException>(() => MatchSaveCodec.Decode(padded),
                "Trailing bytes after the declared content must fail loud (KD-6 / R1).");
        }

        [Test]
        public void Codec_TruncatedBlob_FailsLoud()
        {
            (SnapshotHeader header, SnapshotPayload payload) = CaptureAt(10);
            byte[] blob = MatchSaveCodec.Encode(MatchSeed, header, payload);
            var chopped = new byte[blob.Length - 5];
            Array.Copy(blob, chopped, chopped.Length);

            Assert.Throws<InvalidOperationException>(() => MatchSaveCodec.Decode(chopped),
                "A truncated blob (short payload) must fail loud (KD-6 bound guard).");
        }

        [Test]
        public void Codec_NullBlob_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => MatchSaveCodec.Decode(null));
        }

        [Test]
        public void Codec_OversizePayloadLength_FailsLoud()
        {
            (SnapshotHeader header, SnapshotPayload payload) = CaptureAt(10);
            byte[] blob = MatchSaveCodec.Encode(MatchSeed, header, payload);

            // The payload-length u32 sits at (blob.Length - payloadBytes - 4). Overwrite it with a huge
            // value: the codec must refuse it (capacity check / overflow-safe bound), never allocate/read.
            int lenPos = blob.Length - payload.BytesWritten - 4;
            blob[lenPos] = 0xFF; blob[lenPos + 1] = 0xFF; blob[lenPos + 2] = 0xFF; blob[lenPos + 3] = 0xFF;

            Assert.Throws<InvalidOperationException>(() => MatchSaveCodec.Decode(blob),
                "A payload length exceeding capacity must fail loud, not over-read (KD-6).");
        }

        [Test]
        public void Codec_TamperedFingerprint_RestoreFailsLoud()
        {
            // Decode a genuine save, swap its fingerprint for a foreign one, and hand the reconstructed
            // (header, payload) straight to the restore factory: the KD-6 gate (running against the
            // reconstructed fingerprint) must reject the environment mismatch. This is the on-disk
            // fidelity of the fingerprint gate (R3).
            (SnapshotHeader header, SnapshotPayload payload) = CaptureAt(20);
            MatchSaveContents got = MatchSaveCodec.Decode(MatchSaveCodec.Encode(MatchSeed, header, payload));

            got.Header.Fingerprint = new EnvironmentFingerprint(
                workerCount: 4, schedulerPolicy: "foreign", reductionTopology: "Tree",
                simdFeatureLevel: "AVX2", floatModelHash: "deadbeef", unicodeNormalizationVersion: "9.0");

            Assert.Throws<InvalidOperationException>(
                () => MatchEngine.RestoreFromSnapshot(got.Header, got.Payload, got.MatchSeed),
                "A save whose fingerprint does not match the live environment must be refused (KD-6).");
        }

        // ── Distinct-squad helpers (mirror MatchEngineSnapshotRestoreTests) ─────────

        private static int RequiredCount =>
            MatchEngineConstants.PLAYERS_PER_TEAM + MatchEngineConstants.SUBSTITUTES_PER_TEAM;

        private static PlayerPosition PosFor(int localIndex)
        {
            if (localIndex == 0)  return PlayerPosition.Goalkeeper;
            if (localIndex <= 4)  return PlayerPosition.Defender;
            if (localIndex <= 8)  return PlayerPosition.Midfielder;
            if (localIndex <= 10) return PlayerPosition.Forward;
            switch ((localIndex - 11) % 3)
            {
                case 0:  return PlayerPosition.Defender;
                case 1:  return PlayerPosition.Midfielder;
                default: return PlayerPosition.Forward;
            }
        }

        private static Squad DistinctSquad(int clubId)
        {
            var players = new PlayerRecord[RequiredCount];
            for (int k = 0; k < players.Length; k++)
            {
                PlayerRecord p = PlayerRecord.CreateDefault(clubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + k);
                p.Position = PosFor(k);

                int[] a = new int[31];
                for (int f = 0; f < a.Length; f++)
                {
                    a[f] = 1 + ((k * 7 + f * 3 + clubId) % 20);
                }
                var attrs = new PlayerAttributes();
                attrs.FromArray(a);
                attrs.WeakFootRating = 1 + ((k + clubId) % 5);
                p.Attributes = attrs;

                players[k] = p;
            }
            return new Squad(clubId, players);
        }

        private static ISquadProvider Provider(params Squad[] squads)
        {
            var p = new DictionarySquadProvider();
            foreach (Squad s in squads) p.Add(s);
            return p;
        }

        private sealed class DictionarySquadProvider : ISquadProvider
        {
            private readonly Dictionary<int, Squad> _byClubId = new Dictionary<int, Squad>();
            public void Add(Squad s) => _byClubId[s.ClubId] = s;
            public Squad ResolveByClubId(int clubId) =>
                _byClubId.TryGetValue(clubId, out Squad s) ? s : null;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-21 | —      | Initial on-disk save-format acceptance tests — disk round-trip |
// |         |            |        | determinism (neutral / booking-cursor / distinct-squad),      |
// |         |            |        | MatchSaveCodec round-trip + fail-loud guards, MatchSaveManager |
// |         |            |        | missing-file / corrupt-file / no-provider / overwrite paths.   |
#endregion
