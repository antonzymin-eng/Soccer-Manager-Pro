// File:     src/season-save/tests/SeasonSaveManagerTests.cs
// Created:  2026-07-22
// Modified: 2026-07-25 (#30 T1: every save carries a season state; the round-trip asserts the season
//           resumes field-identical alongside the world and the match)
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §5 acceptance;
//           Season & Competition Loop #30 FR-SN-019..023, Appendix B;
//           Match Engine design note §5 Phase G-Phase 3; Living World #22 §4.6/§7.1; Code Standards #20
// Purpose:  Acceptance tests for the unified season save — disk round-trip determinism for a no-match
//           season (world field-identical + world.text resumes + the season state field-identical) and a
//           season with an in-progress match (neutral + distinct-squad-via-ISquadProvider; the match
//           digest chain byte-identical AND the world + season field-identical, all through one file),
//           plus the SeasonSaveCodec fail-loud guards and the SeasonSaveManager fail-loud paths.

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

        // ── Season fixture (#30 T1) ────────────────────────────────────────────────

        private const int ManagedClub = 11;
        private static int[] SeasonClubs => new[] { 10, 11, 12, 13 };

        /// <summary>
        /// A season part-way through: round 0 resolved (so the table carries wins/draws/goals, those
        /// fixtures carry <c>Played</c>, and the calendar cursor has moved) with a non-default board.
        /// Every field the Appendix B layout carries is off its construction default, so a codec that
        /// dropped or mis-ordered one cannot round-trip.
        /// </summary>
        private static SeasonState MidSeasonState()
        {
            SeasonState s = SeasonState.CreateNew(
                SeasonClubs, ManagedClub, seed: 0xABCDEF0123456789UL,
                objective: new BoardObjective(2),
                firstRoundDay: 5u, daysBetweenRounds: 7u, seasonNumber: 3);

            int[] round0 = s.UnplayedFixtureIndicesInRound(0);
            for (int i = 0; i < round0.Length; i++)
            {
                Fixture f = s.FixtureAt(round0[i]);
                // Distinct scorelines so wins / draws / losses and both goal columns are all exercised.
                s.ApplyResult(new MatchResult(
                    f.HomeClubId, f.AwayClubId, homeGoals: 2 + i, awayGoals: i,
                    roundIndex: f.RoundIndex, worldDay: 5u));
                s.MarkFixturePlayed(round0[i]);
            }

            s.AdvanceCursorOneRound();
            s.SetBoard(s.Board.WithJobSecurity(742));
            return s;
        }

        // ── Disk round-trip determinism (G5) ───────────────────────────────────────

        [Test]
        public void DiskRoundTrip_NoMatchSeason_IsDeterministic()
        {
            WorldStore world = PopulatedStore();
            SeasonState season = MidSeasonState();
            string path = TempPath("season.save");
            SeasonSaveManager.Save(world, season, matchOrNull: null, path);
            Assert.IsTrue(File.Exists(path), "Save must produce the destination file atomically.");

            // Capture is non-mutating, so the saved store itself is a valid uninterrupted reference.
            (byte[] refSnap, string refText) = AdvanceReference(world);

            SeasonSaveContents contents = SeasonSaveManager.Load(path);
            Assert.IsNull(contents.Match, "A no-match season Loads with a null Match (KD-3).");
            Assert.IsNotNull(contents.Season, "A season save always reconstructs a season (FR-SN-019).");
            Assert.IsTrue(season.FieldsEqual(contents.Season),
                "FR-SN-022: the season state must round-trip field-identically through the file.");
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
            SeasonSaveManager.Save(world, MidSeasonState(), matchOrNull: null, path);

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
            SeasonState season = MidSeasonState();
            MEngine match = new MEngine(MatchSeed);
            matchSetup?.Invoke(match);
            for (int i = 0; i < n; i++) match.RunTick();
            Assert.AreEqual((ulong)n, match.CurrentTick);

            string path = TempPath("season-match.save");
            SeasonSaveManager.Save(world, season, match, path);
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
            Assert.IsTrue(season.FieldsEqual(contents.Season),
                "FR-SN-022: the season state must round-trip field-identically alongside the match.");

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

        [Test]
        public void DiskRoundTrip_SeasonWithLeagueBootstrappedMatch_IsDeterministic()
        {
            // A3's headline deliverable exercised end to end: a bootstrapped `League` IS the
            // ISquadProvider, so it must serve the match restore through a real save file without an
            // adapter. Previously only hand-rolled providers were tested, so an incompatibility between
            // bootstrapped squads and the restore path (lineup re-projection, roster ClubId matching)
            // would have shipped unnoticed.
            League league = LeagueBootstrap.Generate(0xB007_5EED_0000_0001UL, clubCount: 4);

            AssertSeasonRoundTripWithMatch(
                matchSetup: e => e.ConfigureSquads(league.ResolveByClubId(0), league.ResolveByClubId(1)),
                n: 150, k: 60,
                squads: league);
        }

        // ── SeasonSaveManager fail-loud ─────────────────────────────────────────────

        [Test]
        public void Load_NoMatchSeason_WithProvider_IgnoresProvider()
        {
            // R4: a provider supplied for a no-match season is harmless — Load reconstructs the world and
            // never touches the (absent) match, returning a null Match.
            WorldStore world = PopulatedStore();
            string path = TempPath("nomatch-provider.save");
            SeasonSaveManager.Save(world, MidSeasonState(), matchOrNull: null, path);

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
            SeasonSaveManager.Save(world, MidSeasonState(), matchOrNull: null, path);

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
            SeasonSaveManager.Save(world, MidSeasonState(), match, path);

            Assert.Throws<NotSupportedException>(
                () => SeasonSaveManager.Load(path),
                "A distinct-squad match season Loaded without an ISquadProvider must fail loud (KD-6 / R4).");
        }

        [Test]
        public void Save_NullWorld_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => SeasonSaveManager.Save(null, MidSeasonState(), matchOrNull: null, TempPath("x.save")));
        }

        [Test]
        public void Save_NullSeason_Throws()
        {
            // FR-SN-019: unlike the match, the season is never optional — a null must fail loud rather
            // than write a file that Load could not reconstruct a season from.
            Assert.Throws<ArgumentNullException>(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), null, matchOrNull: null, TempPath("x.save")));
        }

        [Test]
        public void Save_OverwritesExistingFile_Atomically()
        {
            WorldStore world = PopulatedStore();
            string path = TempPath("overwrite.save");
            SeasonSaveManager.Save(world, MidSeasonState(), matchOrNull: null, path);

            world.AdvanceDay();
            Assert.DoesNotThrow(() => SeasonSaveManager.Save(world, MidSeasonState(), matchOrNull: null, path),
                "Re-saving over an existing file must atomically replace it (File.Replace), not throw.");
            Assert.IsFalse(File.Exists(path + ".tmp"), "The temp file must not survive a successful save.");
        }

        // ── SeasonSaveCodec (in-memory) ─────────────────────────────────────────────

        [Test]
        public void Codec_RoundTrips_WithMatch()
        {
            byte[] worldBlob = new byte[] { 1, 2, 3, 4, 5 };
            byte[] seasonBlob = new byte[] { 6, 6 };
            byte[] matchBlob = new byte[] { 9, 8, 7 };
            SeasonSaveBlobs got = SeasonSaveCodec.Decode(
                SeasonSaveCodec.Encode(worldBlob, seasonBlob, matchBlob));
            CollectionAssert.AreEqual(worldBlob, got.WorldBlob);
            CollectionAssert.AreEqual(seasonBlob, got.SeasonBlob);
            CollectionAssert.AreEqual(matchBlob, got.MatchBlob);
        }

        [Test]
        public void Codec_RoundTrips_NoMatch()
        {
            byte[] worldBlob = new byte[] { 42, 42, 42 };
            byte[] seasonBlob = new byte[] { 7 };
            SeasonSaveBlobs got = SeasonSaveCodec.Decode(
                SeasonSaveCodec.Encode(worldBlob, seasonBlob, matchBlobOrNull: null));
            CollectionAssert.AreEqual(worldBlob, got.WorldBlob);
            CollectionAssert.AreEqual(seasonBlob, got.SeasonBlob);
            Assert.IsNull(got.MatchBlob, "A null match blob must round-trip to a null MatchBlob (KD-3).");
        }

        [Test]
        public void Load_WorldPastTheNextFixtureDay_FailsLoud()
        {
            // FR-SN-011 (MUST) / F4: the KD-4 cursor invariant is the ONE coherence rule spanning the
            // world and season blobs, so neither codec can check it — only this root, which holds both.
            // MidSeasonState's cursor points at round 1 (day 12); advancing the world past it makes the
            // pair incoherent, and T2's day-advance loop (which advances UP TO the next fixture day)
            // would be undefined. Save does not check (the requirement is on restore), so the corruption
            // must surface at Load.
            string path = TempPath("cursor-behind.season");
            WorldStore world = PopulatedStore();
            for (int i = 0; i < 20; i++)
            {
                world.AdvanceDay();
            }

            SeasonSaveManager.Save(world, MidSeasonState(), matchOrNull: null, path);

            Assert.Throws<InvalidOperationException>(() => SeasonSaveManager.Load(path),
                "A season whose next fixture day is behind the restored world day must be rejected (F4).");
        }

        [Test]
        public void Load_CompletedSeason_PassesTheCursorInvariantVacuously()
        {
            // The other side of the same gate: a completed season has no next fixture, so the invariant
            // is vacuously satisfied at ANY world day and must not be turned into a spurious refusal.
            string path = TempPath("season-complete.season");
            WorldStore world = PopulatedStore();
            for (int i = 0; i < 50; i++)
            {
                world.AdvanceDay();
            }

            SeasonState done = MidSeasonState();
            while (!done.Calendar.IsSeasonComplete)
            {
                done.AdvanceCursorOneRound();
            }

            SeasonSaveManager.Save(world, done, matchOrNull: null, path);

            SeasonSaveContents got = SeasonSaveManager.Load(path);
            Assert.IsTrue(got.Season.Calendar.IsSeasonComplete,
                "A completed season must load at any world day (the invariant is vacuous).");
        }

        [Test]
        public void Codec_FrameBlocksSitInTheirPinnedOrder()
        {
            // Locks the Appendix B frame ORDER, not just the round-trip: a self-consistent transposition
            // of the world and season blocks would round-trip green while writing a layout no other
            // reader of this format expects. Distinct lengths make each block's position identifiable.
            byte[] worldBlob = { 1, 1, 1, 1, 1 };      // 5 bytes
            byte[] seasonBlob = { 2, 2 };              // 2 bytes
            byte[] matchBlob = { 3, 3, 3 };            // 3 bytes
            byte[] blob = SeasonSaveCodec.Encode(worldBlob, seasonBlob, matchBlob);

            int o = 0;
            Assert.AreEqual(SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION,
                CanonicalSerializer.ReadU32(blob, ref o), "frame field 1: format version");
            Assert.AreEqual(1, blob[o], "frame field 2: matchPresent flag");
            o += 1;
            Assert.AreEqual((uint)worldBlob.Length, CanonicalSerializer.ReadU32(blob, ref o),
                "frame field 3: the WORLD block's length prefix comes first");
            o += worldBlob.Length;
            Assert.AreEqual((uint)seasonBlob.Length, CanonicalSerializer.ReadU32(blob, ref o),
                "frame field 4: the SEASON block sits between the world and match blocks (FR-SN-019)");
            o += seasonBlob.Length;
            Assert.AreEqual((uint)matchBlob.Length, CanonicalSerializer.ReadU32(blob, ref o),
                "frame field 5: the MATCH block is last");
            Assert.AreEqual(blob.Length, o + matchBlob.Length, "no trailing bytes");
        }

        [Test]
        public void Codec_EmptyWorldBlob_RoundTrips()
        {
            SeasonSaveBlobs got = SeasonSaveCodec.Decode(
                SeasonSaveCodec.Encode(Array.Empty<byte>(), Array.Empty<byte>(), matchBlobOrNull: null));
            Assert.AreEqual(0, got.WorldBlob.Length);
            Assert.AreEqual(0, got.SeasonBlob.Length);
            Assert.IsNull(got.MatchBlob);
        }

        [Test]
        public void Codec_NullWorldBlob_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => SeasonSaveCodec.Encode(
                    worldBlob: null, seasonBlob: new byte[] { 1 }, matchBlobOrNull: new byte[] { 1 }));
        }

        [Test]
        public void Codec_NullBlob_Decode_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SeasonSaveCodec.Decode(null));
        }

        [Test]
        public void Codec_WrongFormatVersion_FailsLoud()
        {
            byte[] blob = SeasonSaveCodec.Encode(new byte[] { 1, 2 }, new byte[] { 3 }, matchBlobOrNull: null);
            blob[0] ^= 0xFF; // corrupt the leading format-version u32
            Assert.Throws<InvalidOperationException>(() => SeasonSaveCodec.Decode(blob),
                "A season format-version mismatch must fail loud (KD-4).");
        }

        [Test]
        public void Codec_PreT1FrameVersion_FailsLoud()
        {
            // FR-SN-020: the frame bumped 1 -> 2 when the season sub-blob landed. A v1 file has no
            // season block, so accepting one would deframe the match blob as a season. Refused outright
            // — there is no cross-version migration at Stage 0 (KD-4).
            byte[] blob = SeasonSaveCodec.Encode(new byte[] { 1, 2 }, new byte[] { 3 }, matchBlobOrNull: null);
            int o = 0;
            CanonicalSerializer.WriteU32(blob, ref o, 1u);
            Assert.Throws<InvalidOperationException>(() => SeasonSaveCodec.Decode(blob),
                "A pre-#30-T1 (v1) season frame must be rejected, not reinterpreted.");
        }

        [Test]
        public void Codec_BadMatchPresentFlag_FailsLoud()
        {
            byte[] blob = SeasonSaveCodec.Encode(new byte[] { 1, 2 }, new byte[] { 3 }, matchBlobOrNull: null);
            blob[4] = 2; // the matchPresent flag sits right after the u32 version
            Assert.Throws<InvalidOperationException>(() => SeasonSaveCodec.Decode(blob),
                "A matchPresent flag other than 0/1 must fail loud (KD-8).");
        }

        [Test]
        public void Codec_OversizeWorldLength_FailsLoud()
        {
            byte[] blob = SeasonSaveCodec.Encode(new byte[] { 1, 2, 3 }, new byte[] { 4 }, matchBlobOrNull: null);
            // The world length u32 sits at offset 5 (u32 version + u8 flag). Overwrite with a huge value.
            blob[5] = 0xFF; blob[6] = 0xFF; blob[7] = 0xFF; blob[8] = 0xFF;
            Assert.Throws<InvalidOperationException>(() => SeasonSaveCodec.Decode(blob),
                "A world length exceeding the blob must fail loud, not over-read (KD-8).");
        }

        [Test]
        public void Codec_TrailingBytes_FailsLoud()
        {
            byte[] blob = SeasonSaveCodec.Encode(
                new byte[] { 1, 2 }, new byte[] { 9 }, matchBlobOrNull: new byte[] { 3 });
            var padded = new byte[blob.Length + 1];
            Array.Copy(blob, padded, blob.Length);
            Assert.Throws<InvalidOperationException>(() => SeasonSaveCodec.Decode(padded),
                "Trailing bytes after the declared content must fail loud (KD-8 / R1).");
        }

        [Test]
        public void Codec_TruncatedMatchBlock_FailsLoud()
        {
            byte[] blob = SeasonSaveCodec.Encode(
                new byte[] { 1, 2 }, new byte[] { 9 }, matchBlobOrNull: new byte[] { 3, 4, 5 });
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
// | 1.2     | 2026-07-24 | —      | Arc-triggers E2 §8.9(a): flag-on world resumes evaluating      |
// |         |            |        | through the season file (+ null-canon negative control).       |
// | 1.3     | 2026-07-25 | —      | #30 T1: MidSeasonState fixture; every Save carries a season;   |
// |         |            |        | the no-match and with-match round-trips assert the season      |
// |         |            |        | resumes field-identical (FR-SN-022); the frame-codec cases     |
// |         |            |        | carry a season blob; + Save_NullSeason_Throws (FR-SN-019).     |
// | 1.4     | 2026-07-25 | —      | #30 T1 AR pass 2: + Load_WorldPastTheNextFixtureDay_FailsLoud  |
// |         |            |        | and Load_CompletedSeason_PassesTheCursorInvariantVacuously    |
// |         |            |        | (FR-SN-011 / F4), + Codec_FrameBlocksSitInTheirPinnedOrder    |
// |         |            |        | (AR pass 1 — the frame had no field-order lock).              |
#endregion
