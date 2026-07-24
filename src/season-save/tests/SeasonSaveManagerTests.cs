// File:     src/season-save/tests/SeasonSaveManagerTests.cs
// Created:  2026-07-22
// Modified: 2026-07-24 (arc-triggers E2 §8.9(a): flag-on world resumes evaluating through the season
//           file when Load threads a canon source, with a null-canon negative control)
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §5 acceptance;
//           Match Engine design note §5 Phase G-Phase 3; Living World #22 §4.6/§7.1; Code Standards #20
// Purpose:  Acceptance tests for the unified season save — disk round-trip determinism for a no-match
//           season (world field-identical + world.text resumes) and a season with an in-progress match
//           (neutral + distinct-squad-via-ISquadProvider; the match digest chain byte-identical AND the
//           world field-identical, both through one file), plus the SeasonSaveCodec fail-loud guards and
//           the SeasonSaveManager fail-loud paths.

using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;

using MEngine = TacticalDirector.MatchEngine.MatchEngine;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// On-disk save/load tests for <see cref="SeasonSaveManager"/> + <see cref="SeasonSaveCodec"/>. The
    /// central property is disk round-trip determinism of the whole season through one file: the
    /// living-world <see cref="WorldStore"/> resumes field-identical (re-<c>Snapshot()</c> byte-equal +
    /// <c>world.text</c> continues) and, when present, the match's digest chain continues byte-for-byte.
    /// </summary>
    [TestFixture]
    public sealed class SeasonSaveManagerTests
    {
        private const int Manager = 0;
        private const byte AffinityTrust = 0b110; // Affinity (bit 1) | Trust (bit 2)
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;

        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "td-seasonsave-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch (Exception) { }
        }

        private string TempPath(string name) => Path.Combine(_tempDir, name);

        // ── World fixtures (mirror WorldStoreTests.PopulatedStore) ──────────────────

        private static SpawnCause Cause(uint worldTick) =>
            new SpawnCause(42, new[] { new SpawnCause.Input(1, 0.5f) }, 999UL, worldTick);

        private static WorldStore PopulatedStore()
        {
            WorldStore s = new WorldStore(Manager);
            uint ep5 = s.RecordInteraction(5, isOwnClub: false, AffinityTrust, EventKind.ManagerCriticism, 11);
            s.Arcs.SpawnArc(ArcKind.MediaVendetta, Cause(s.CurrentWorldTick),
                new[] { new Arc.PinnedEpisode(Manager, 5, ep5) }, s.CurrentWorldTick, 30u);
            s.RecordInteraction(8, isOwnClub: true, AffinityTrust, EventKind.ManagerDefence, 0);
            s.RecordInteraction(3, isOwnClub: false, AffinityTrust, EventKind.Benching, 0);
            s.RecordInteraction(12, isOwnClub: true, AffinityTrust, EventKind.ContractSnub, 0);
            s.Membership.Depart(12);

            InteractionSlots slots = new InteractionSlots("Boss", "Rivals FC", 1, 2);
            s.GenerateInteractionText(InteractionIntent.MediaProvokeTitlePressure, in slots);
            s.GenerateInteractionText(InteractionIntent.BoardSignalsConfidence, in slots);

            s.AdvanceDay();
            s.AdvanceDay();
            return s;
        }

        // Advances a store through a fixed post-load sequence and returns its resulting composite bytes +
        // the last generated text — the observable the WorldStore round-trip is proven against.
        private static (byte[] snapshot, string text) AdvanceReference(WorldStore s)
        {
            s.AdvanceDay();
            s.RecordInteraction(5, isOwnClub: false, AffinityTrust, EventKind.TransferRumour, 0);
            InteractionSlots slots = new InteractionSlots("Boss", "Rivals FC", 3, 0);
            string text = s.GenerateInteractionText(InteractionIntent.MediaProvokeTitlePressure, in slots);
            s.AdvanceDay();
            return (s.Snapshot(), text);
        }

        // ── Disk round-trip determinism (G5) ───────────────────────────────────────

        [Test]
        public void DiskRoundTrip_NoMatchSeason_IsDeterministic()
        {
            WorldStore world = PopulatedStore();
            string path = TempPath("season.save");
            SeasonSaveManager.Save(world, matchOrNull: null, path);
            Assert.IsTrue(File.Exists(path), "Save must produce the destination file atomically.");

            // Capture is non-mutating, so the saved store itself is a valid uninterrupted reference.
            (byte[] refSnap, string refText) = AdvanceReference(world);

            SeasonSaveContents contents = SeasonSaveManager.Load(path);
            Assert.IsNull(contents.Match, "A no-match season Loads with a null Match (KD-3).");
            (byte[] gotSnap, string gotText) = AdvanceReference(contents.World);

            CollectionAssert.AreEqual(refSnap, gotSnap,
                "The Loaded world must resume field-identical (re-Snapshot byte-equal) after an identical advance.");
            Assert.AreEqual(refText, gotText,
                "The Loaded world.text stream must resume byte-identically across the season file.");
        }

        // ── Arc-triggers E2 §8.9(a): a flag-on world keeps evaluating after a season restore ─────

        [Test]
        public void DiskRoundTrip_FlagOnWorld_ResumesEvaluating_ThroughSeasonFile()
        {
            // A flag-on canon whose ego-clash signal crosses the WonderkidVsVeteran threshold for the
            // manager→5 contact (entity 5 is populated by PopulatedStore).
            ArcCanonSource above = new ArcCanonSource.Builder()
                .SetEntitySignal(5, LivingWorldConstants.ARC_SIGNAL_KEY_EGO_CLASH, 0.9f)
                .Build();

            WorldStore world = PopulatedStore();
            world.SetArcCanon(above);   // canon set but NOT yet advanced ⇒ the trigger has not fired
            Assert.AreEqual(1, world.Arcs.ArcCount, "only PopulatedStore's manual arc exists; the trigger has not fired yet");

            string path = TempPath("season-flagon.save");
            SeasonSaveManager.Save(world, matchOrNull: null, path);

            // Uninterrupted reference: the saved (non-mutated) world advances one day and fires.
            world.AdvanceDay();
            byte[] refSnap = world.Snapshot();
            Assert.AreEqual(2, world.Arcs.ArcCount, "the uninterrupted flag-on world fires the WonderkidVsVeteran trigger on the next day");

            // Load WITH the canon threaded (§8.9(a)) → the restored world fires identically.
            SeasonSaveContents contents = SeasonSaveManager.Load(path, squads: null, canon: above);
            contents.World.AdvanceDay();
            CollectionAssert.AreEqual(refSnap, contents.World.Snapshot(),
                "§8.9(a): a flag-on world keeps evaluating after a season restore when Load threads the canon source.");

            // Negative control: Load WITHOUT the canon ⇒ arc evaluation is skipped ⇒ the world diverges
            // (the exact silent-stop-evaluating regression §8.9(a) prevents).
            SeasonSaveContents noCanon = SeasonSaveManager.Load(path);   // canon defaults to null
            noCanon.World.AdvanceDay();
            CollectionAssert.AreNotEqual(refSnap, noCanon.World.Snapshot(),
                "a season Load without the canon source stops evaluating and diverges from the uninterrupted run.");
        }

        /// <summary>
        /// Saves a populated world plus a running match at tick N, then: (a) advances the saved world and
        /// continues the saved match K ticks to build the two reference chains, and (b) Loads a fresh
        /// season from the file, asserting the world resumes field-identical AND the match digest chain
        /// continues byte-for-byte. Both halves come solely from the one file.
        /// </summary>
        private void AssertSeasonRoundTripWithMatch(
            Action<MEngine> matchSetup, int n, int k, ISquadProvider squads = null)
        {
            WorldStore world = PopulatedStore();
            MEngine match = new MEngine(MatchSeed);
            matchSetup?.Invoke(match);
            for (int i = 0; i < n; i++) match.RunTick();
            Assert.AreEqual((ulong)n, match.CurrentTick);

            string path = TempPath("season-match.save");
            SeasonSaveManager.Save(world, match, path);
            Assert.IsTrue(File.Exists(path));

            // Reference chains from the saved (non-mutated) objects.
            (byte[] refWorldSnap, string refText) = AdvanceReference(world);
            var refDigests = new List<byte[]>(k);
            for (int i = 0; i < k; i++)
            {
                match.RunTick();
                refDigests.Add(match.CurrentSnapshotDigest);
            }

            SeasonSaveContents contents = SeasonSaveManager.Load(path, squads);
            Assert.IsNotNull(contents.Match, "A season with a match must Load a non-null Match.");
            Assert.AreEqual((ulong)n, contents.Match.CurrentTick,
                "The loaded match's clock must resume at the saved tick N.");

            (byte[] gotWorldSnap, string gotText) = AdvanceReference(contents.World);
            CollectionAssert.AreEqual(refWorldSnap, gotWorldSnap, "world must resume field-identical.");
            Assert.AreEqual(refText, gotText, "world.text must resume byte-identically.");

            for (int i = 0; i < k; i++)
            {
                contents.Match.RunTick();
                CollectionAssert.AreEqual(refDigests[i], contents.Match.CurrentSnapshotDigest,
                    $"Match digest diverged at tick {n + i + 1} — the season file's match blob is not " +
                    "byte-identical to an uninterrupted run.");
            }
        }

        [Test]
        public void DiskRoundTrip_SeasonWithNeutralMatch_IsDeterministic()
        {
            AssertSeasonRoundTripWithMatch(matchSetup: null, n: 200, k: 60);
        }

        [Test]
        public void DiskRoundTrip_SeasonWithDistinctSquadMatch_IsDeterministic()
        {
            // Exercises the ISquadProvider threading through the season Load (KD-6): the match blob carries
            // only ClubId, so Load must re-project the rosters from the caller's provider.
            Squad home = DistinctSquad(1);
            Squad away = DistinctSquad(2);
            AssertSeasonRoundTripWithMatch(
                matchSetup: e => e.ConfigureSquads(home, away),
                n: 150, k: 60,
                squads: Provider(home, away));
        }

        // ── SeasonSaveManager fail-loud ─────────────────────────────────────────────

        [Test]
        public void Load_NoMatchSeason_WithProvider_IgnoresProvider()
        {
            // R4: a provider supplied for a no-match season is harmless — Load reconstructs the world and
            // never touches the (absent) match, returning a null Match.
            WorldStore world = PopulatedStore();
            string path = TempPath("nomatch-provider.save");
            SeasonSaveManager.Save(world, matchOrNull: null, path);

            SeasonSaveContents contents = SeasonSaveManager.Load(path, Provider(DistinctSquad(1)));
            Assert.IsNull(contents.Match,
                "A no-match season Loaded with a provider must ignore it and return a null Match (R4).");
            Assert.IsNotNull(contents.World);
        }

        [Test]
        public void Load_MissingFile_Throws()
        {
            Assert.Throws<FileNotFoundException>(
                () => SeasonSaveManager.Load(TempPath("nope.save")),
                "Loading a missing season file must surface the IO failure.");
        }

        [Test]
        public void Load_CorruptFile_FailsLoud()
        {
            WorldStore world = PopulatedStore();
            string path = TempPath("corrupt.save");
            SeasonSaveManager.Save(world, matchOrNull: null, path);

            byte[] bytes = File.ReadAllBytes(path);
            File.WriteAllBytes(path, new ArraySegment<byte>(bytes, 0, bytes.Length / 2).ToArray());

            Assert.Throws<InvalidOperationException>(
                () => SeasonSaveManager.Load(path),
                "A truncated season file must fail loud.");
        }

        [Test]
        public void Load_DistinctSquadMatchNoProvider_FailsLoud()
        {
            WorldStore world = PopulatedStore();
            MEngine match = new MEngine(MatchSeed);
            match.ConfigureSquads(DistinctSquad(1), DistinctSquad(2));
            for (int i = 0; i < 30; i++) match.RunTick();
            string path = TempPath("distinct.save");
            SeasonSaveManager.Save(world, match, path);

            Assert.Throws<NotSupportedException>(
                () => SeasonSaveManager.Load(path),
                "A distinct-squad match season Loaded without an ISquadProvider must fail loud (KD-6 / R4).");
        }

        [Test]
        public void Save_NullWorld_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => SeasonSaveManager.Save(null, matchOrNull: null, TempPath("x.save")));
        }

        [Test]
        public void Save_OverwritesExistingFile_Atomically()
        {
            WorldStore world = PopulatedStore();
            string path = TempPath("overwrite.save");
            SeasonSaveManager.Save(world, matchOrNull: null, path);

            world.AdvanceDay();
            Assert.DoesNotThrow(() => SeasonSaveManager.Save(world, matchOrNull: null, path),
                "Re-saving over an existing file must atomically replace it (File.Replace), not throw.");
            Assert.IsFalse(File.Exists(path + ".tmp"), "The temp file must not survive a successful save.");
        }

        // ── SeasonSaveCodec (in-memory) ─────────────────────────────────────────────

        [Test]
        public void Codec_RoundTrips_WithMatch()
        {
            byte[] worldBlob = new byte[] { 1, 2, 3, 4, 5 };
            byte[] matchBlob = new byte[] { 9, 8, 7 };
            SeasonSaveBlobs got = SeasonSaveCodec.Decode(SeasonSaveCodec.Encode(worldBlob, matchBlob));
            CollectionAssert.AreEqual(worldBlob, got.WorldBlob);
            CollectionAssert.AreEqual(matchBlob, got.MatchBlob);
        }

        [Test]
        public void Codec_RoundTrips_NoMatch()
        {
            byte[] worldBlob = new byte[] { 42, 42, 42 };
            SeasonSaveBlobs got = SeasonSaveCodec.Decode(SeasonSaveCodec.Encode(worldBlob, matchBlobOrNull: null));
            CollectionAssert.AreEqual(worldBlob, got.WorldBlob);
            Assert.IsNull(got.MatchBlob, "A null match blob must round-trip to a null MatchBlob (KD-3).");
        }

        [Test]
        public void Codec_EmptyWorldBlob_RoundTrips()
        {
            SeasonSaveBlobs got = SeasonSaveCodec.Decode(
                SeasonSaveCodec.Encode(Array.Empty<byte>(), matchBlobOrNull: null));
            Assert.AreEqual(0, got.WorldBlob.Length);
            Assert.IsNull(got.MatchBlob);
        }

        [Test]
        public void Codec_NullWorldBlob_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => SeasonSaveCodec.Encode(worldBlob: null, matchBlobOrNull: new byte[] { 1 }));
        }

        [Test]
        public void Codec_NullBlob_Decode_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SeasonSaveCodec.Decode(null));
        }

        [Test]
        public void Codec_WrongFormatVersion_FailsLoud()
        {
            byte[] blob = SeasonSaveCodec.Encode(new byte[] { 1, 2 }, matchBlobOrNull: null);
            blob[0] ^= 0xFF; // corrupt the leading format-version u32
            Assert.Throws<InvalidOperationException>(() => SeasonSaveCodec.Decode(blob),
                "A season format-version mismatch must fail loud (KD-4).");
        }

        [Test]
        public void Codec_BadMatchPresentFlag_FailsLoud()
        {
            byte[] blob = SeasonSaveCodec.Encode(new byte[] { 1, 2 }, matchBlobOrNull: null);
            blob[4] = 2; // the matchPresent flag sits right after the u32 version
            Assert.Throws<InvalidOperationException>(() => SeasonSaveCodec.Decode(blob),
                "A matchPresent flag other than 0/1 must fail loud (KD-8).");
        }

        [Test]
        public void Codec_OversizeWorldLength_FailsLoud()
        {
            byte[] blob = SeasonSaveCodec.Encode(new byte[] { 1, 2, 3 }, matchBlobOrNull: null);
            // The world length u32 sits at offset 5 (u32 version + u8 flag). Overwrite with a huge value.
            blob[5] = 0xFF; blob[6] = 0xFF; blob[7] = 0xFF; blob[8] = 0xFF;
            Assert.Throws<InvalidOperationException>(() => SeasonSaveCodec.Decode(blob),
                "A world length exceeding the blob must fail loud, not over-read (KD-8).");
        }

        [Test]
        public void Codec_TrailingBytes_FailsLoud()
        {
            byte[] blob = SeasonSaveCodec.Encode(new byte[] { 1, 2 }, matchBlobOrNull: new byte[] { 3 });
            var padded = new byte[blob.Length + 1];
            Array.Copy(blob, padded, blob.Length);
            Assert.Throws<InvalidOperationException>(() => SeasonSaveCodec.Decode(padded),
                "Trailing bytes after the declared content must fail loud (KD-8 / R1).");
        }

        [Test]
        public void Codec_TruncatedMatchBlock_FailsLoud()
        {
            byte[] blob = SeasonSaveCodec.Encode(new byte[] { 1, 2 }, matchBlobOrNull: new byte[] { 3, 4, 5 });
            var chopped = new byte[blob.Length - 2];
            Array.Copy(blob, chopped, chopped.Length);
            Assert.Throws<InvalidOperationException>(() => SeasonSaveCodec.Decode(chopped),
                "A truncated match block must fail loud (KD-8 bound guard).");
        }

        // ── Distinct-squad helpers (mirror MatchSaveManagerTests) ───────────────────

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
// | 1.0     | 2026-07-22 | —      | Initial season-save acceptance tests — disk round-trip         |
// |         |            |        | determinism (no-match season; season with neutral / distinct-  |
// |         |            |        | squad match via ISquadProvider), SeasonSaveCodec round-trip +  |
// |         |            |        | fail-loud guards, SeasonSaveManager fail-loud paths.           |
// | 1.1     | 2026-07-22 | —      | Code AR L-2: + Load_NoMatchSeason_WithProvider_IgnoresProvider |
// |         |            |        | (locks R4 — a provider on a no-match season is ignored).       |
#endregion
