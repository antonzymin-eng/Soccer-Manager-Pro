// File:     src/season-save/tests/SeasonSaveManagerTests.cs
// Created:  2026-07-22
// Modified: 2026-08-13 (#44 C1/C2 AR round 4, H4/ERR-030-039 — the public long form's own
//           unwired-over-populated lock, and every call site updated for the required
//           disciplineWired parameter — v1.20)
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §5 acceptance;
//           Season & Competition Loop #30 FR-SN-019..023, Appendix B; Training System #29 FR-TR-018/019;
//           Injuries & Medical #41 FR-MD-017/018; Player Progression & Lifecycle #28 §3.5, KD-4,
//           ERR-028-007 (the fourth persisted cursor), ERR-028-008 (refuse to overwrite a roster with
//           an empty one); Discipline & Suspensions #44 Appendix B;
//           Match Engine design note §5 Phase G-Phase 3; Living World #22 §4.6/§7.1; Code Standards #20
// Purpose:  Acceptance tests for the unified season save — disk round-trip determinism for a no-match
//           season (world field-identical + world.text resumes + the season state field-identical) and a
//           season with an in-progress match (neutral + distinct-squad-via-ISquadProvider; the match
//           digest chain byte-identical AND the world + season field-identical, all through one file),
//           plus the SeasonSaveCodec fail-loud guards and the SeasonSaveManager fail-loud paths —
//           including the #28 progression cursor's cursor-vs-clock gate at Save and Load, and the
//           empty-roster-overwrite refusal.

using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.Discipline;
using TacticalDirector.InjuriesMedical;
using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.PlayerProgression;
using TacticalDirector.TrainingSystem;

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

        // "This season tracks no training / medical state" — said explicitly, because Save requires it
        // to be said. A default that let a caller stay silent is what would make a T2 omission look
        // exactly like an unwired game (see SeasonSaveManager.Save's null guards).
        private static ClubTrainingStates[] NoTraining => Array.Empty<ClubTrainingStates>();

        private static ClubInjuryStates[] NoMedical => Array.Empty<ClubInjuryStates>();

        private static ClubAppearanceStates[] NoAppearance => Array.Empty<ClubAppearanceStates>();

        // The frame treats the discipline block as opaque, and none of the SeasonSaveCodec (in-memory)
        // fixtures below care about its content — only that a well-formed one is present (#44 Appendix
        // B), on the same terms as TrainingStub / MedicalStub / AppearanceStub further down. A fresh
        // wrapper each read, since DisciplineBlock only wraps a byte[].
        private static DisciplineBlock EmptyDisciplineBlock =>
            new DisciplineBlock(DisciplineSaveCodec.Encode(new DisciplineState()));

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
            SeasonSaveManager.Save(world, season, matchOrNull: null, path, NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true);
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
            SeasonSaveManager.Save(world, MidSeasonState(), matchOrNull: null, path, NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true);

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
            SeasonSaveManager.Save(world, season, match, path, NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true);
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

        // ── #28 T2a (KD-4): Load must restore the match against the FILE's evolved roster,
        //    never the caller-supplied provider, once the file carries a populated progression block ──

        [Test]
        public void Load_MatchWithPopulatedProgression_RestoresFromFileRosterNotCallerBootstrap()
        {
            // Mutation-audit lock on SeasonSaveManager.Load's roster-source selection:
            //   rosterSource = progression.ClubCount > 0 ? new ProgressionSquads(progression) : squads;
            // Forcing `rosterSource = squads` unconditionally left the whole suite green, because
            // every other in-progress-match round-trip test hands the SAME roster to both
            // ConfigureSquads and the caller-supplied provider — the two branches are
            // indistinguishable there. Here they are made to diverge on purpose: the caller's
            // provider is the DAY-0 bootstrap and the file's progression has banked real #28 growth,
            // so only the correct branch reproduces the boot-time simulation.
            //
            // Discriminated by continuing the digest chain, the same technique the ISquadProvider
            // round-trip tests above use — TestOnly_CanonicalAttributes is match-engine-TEST-internal
            // (InternalsVisibleTo scopes it to TacticalDirector.MatchEngine.Tests only) and is not
            // reachable from this assembly. Per-player attributes feed AI/physics every tick and the
            // lineup selection itself is attribute-driven, so a wrong roster diverges the chain
            // overwhelmingly likely within a handful of ticks.
            const int homeClub = 5301;
            const int awayClub = 5302;
            Squad homeDay0 = GrowableSquad(homeClub);
            Squad awayDay0 = GrowableSquad(awayClub);

            ProgressionEngine progression =
                ProgressionEngine.SeedFrom(new[] { homeDay0, awayDay0 }, newGameWorldDay: 0u);

            // Bank exactly one whole attribute point per (Growth-band) player: GROWTH_DAILY_POINTS =
            // +1/day, POINT_COST = DAYS_PER_YEAR. The first AdvanceDay call is the sentinel branch
            // (accrues only that one day's point); the second REPLAYS the gap up to the year boundary
            // (T-PG-DET-002), landing the cursor on the spend threshold exactly once.
            progression.AdvanceDay(1u, TrainingInputBatch.Neutral);
            progression.AdvanceDay((uint)PlayerProgressionConstants.DAYS_PER_YEAR, TrainingInputBatch.Neutral);

            Squad homeEvolved = progression.SquadFor(homeClub);
            Squad awayEvolved = progression.SquadFor(awayClub);

            // Sanity: the growth step must have measurably diverged the roster from the day-0
            // bootstrap BEFORE the save is even written, or the rest of this test proves nothing.
            Assert.IsTrue(
                SquadDiverges(homeDay0, homeEvolved) || SquadDiverges(awayDay0, awayEvolved),
                "sanity: the year-boundary growth step produced no measurable attribute change vs the " +
                "day-0 bootstrap — this test would pass for proving nothing.");

            WorldStore world = PopulatedStore();
            while (world.CurrentWorldTick < PlayerProgressionConstants.DAYS_PER_YEAR)
            {
                world.AdvanceDay();
            }

            // A completed season: the KD-4 calendar-cursor invariant is then vacuous at ANY world day
            // (Load_CompletedSeason_PassesTheCursorInvariantVacuously), which is what lets the world
            // clock sit a year ahead of MidSeasonState's fixture schedule.
            SeasonState season = MidSeasonState();
            while (!season.Calendar.IsSeasonComplete)
            {
                season.AdvanceCursorOneRound();
            }

            MEngine match = new MEngine(MatchSeed);
            match.ConfigureSquads(homeEvolved, awayEvolved);
            const int n = 150;
            for (int i = 0; i < n; i++) match.RunTick();
            Assert.AreEqual((ulong)n, match.CurrentTick);

            string path = TempPath("progression-roster-restore.season");
            SeasonSaveManager.Save(
                world, season, match, path,
                TrainingBlocksMatching(progression), MedicalBlocksMatching(progression),
                AppearanceBlocksMatching(progression), progression, new DisciplineState(), disciplineWired: true);
            Assert.IsTrue(File.Exists(path));

            // Reference chain from the SAVED (non-mutated) match object — it is still configured with
            // the evolved squad, so this is what a correct restore must reproduce byte-for-byte.
            const int k = 60;
            var refDigests = new List<byte[]>(k);
            for (int i = 0; i < k; i++)
            {
                match.RunTick();
                refDigests.Add(match.CurrentSnapshotDigest);
            }

            // The caller passes the DAY-0 BOOTSTRAP as its provider — deliberately the WRONG roster.
            // The file's own populated progression block must win over it.
            SeasonSaveContents contents = SeasonSaveManager.Load(path, Provider(homeDay0, awayDay0));
            Assert.IsNotNull(contents.Match, "a season with a match must Load a non-null Match.");
            Assert.AreEqual((ulong)n, contents.Match.CurrentTick,
                "the loaded match's clock must resume at the saved tick.");

            for (int i = 0; i < k; i++)
            {
                contents.Match.RunTick();
                CollectionAssert.AreEqual(refDigests[i], contents.Match.CurrentSnapshotDigest,
                    $"tick {n + i + 1}: the restored match diverged from the evolved-squad reference " +
                    "chain — Load must restore the in-progress match against the FILE's #28 roster, " +
                    "never the caller-supplied provider, once the file carries a populated " +
                    "progression block (KD-4).");
            }
        }

        // ── SeasonSaveManager fail-loud ─────────────────────────────────────────────

        [Test]
        public void Load_NoMatchSeason_WithProvider_IgnoresProvider()
        {
            // R4: a provider supplied for a no-match season is harmless — Load reconstructs the world and
            // never touches the (absent) match, returning a null Match.
            WorldStore world = PopulatedStore();
            string path = TempPath("nomatch-provider.save");
            SeasonSaveManager.Save(world, MidSeasonState(), matchOrNull: null, path, NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true);

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
            SeasonSaveManager.Save(world, MidSeasonState(), matchOrNull: null, path, NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true);

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
            SeasonSaveManager.Save(world, MidSeasonState(), match, path, NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true);

            Assert.Throws<NotSupportedException>(
                () => SeasonSaveManager.Load(path),
                "A distinct-squad match season Loaded without an ISquadProvider must fail loud (KD-6 / R4).");
        }

        [Test]
        public void Save_NullWorld_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => SeasonSaveManager.Save(null, MidSeasonState(), matchOrNull: null, TempPath("x.save"), NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true));
        }

        [Test]
        public void Save_NullSeason_Throws()
        {
            // FR-SN-019: unlike the match, the season is never optional — a null must fail loud rather
            // than write a file that Load could not reconstruct a season from.
            Assert.Throws<ArgumentNullException>(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), null, matchOrNull: null, TempPath("x.save"), NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true));
        }

        [Test]
        public void Save_NullProgression_Throws()
        {
            // Mutation-audit lock: deleting this guard left the whole suite green. #28's block IS the
            // roster (KD-4) — "this season tracks no careers" is said with an empty ProgressionEngine,
            // never with null, on the same terms as trainingClubs/medicalClubs/appearanceClubs above.
            var ex = Assert.Throws<ArgumentNullException>(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    NoTraining, NoMedical, NoAppearance, progression: null, discipline: new DisciplineState(), disciplineWired: true),
                "Save must refuse a null progression — null is not the empty set, and this block " +
                "carries the roster (FR-PG-017 / #28 KD-4).");
            Assert.AreEqual("progression", ex.ParamName);
        }

        [Test]
        public void Save_OverwritesExistingFile_Atomically()
        {
            WorldStore world = PopulatedStore();
            string path = TempPath("overwrite.save");
            SeasonSaveManager.Save(world, MidSeasonState(), matchOrNull: null, path, NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true);

            world.AdvanceDay();
            Assert.DoesNotThrow(() => SeasonSaveManager.Save(world, MidSeasonState(), matchOrNull: null, path, NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "Re-saving over an existing file must atomically replace it (File.Replace), not throw.");
            Assert.IsFalse(File.Exists(path + ".tmp"), "The temp file must not survive a successful save.");
        }

        // ── #29 / #41 blocks through the whole file (T1) ────────────────────────────

        [Test]
        public void SaveLoad_TrainingAndMedicalState_ResumeFieldIdenticalThroughOneFile()
        {
            // The T1 property this landing exists for: #29/#41 state is SERIALIZED, not regenerated
            // (FR-TR-019 / FR-MD-018), and it travels in the same file as the world and the season.
            // Every field is given a non-default value, so a field the codec forgot to write shows up
            // as a mismatch rather than as a coincidentally-equal default.
            string path = TempPath("training-medical.season");

            var training = new[]
            {
                new ClubTrainingStates(
                    7,
                    new[] { 300, 100 },              // deliberately NOT ascending — order is not state
                    new[]
                    {
                        new TrainingState
                        {
                            Focus = TrainingFocus.Tactical,
                            Condition = 6543,
                            TrainingFatigue = 1234,
                            LastAdvancedWorldDay = 1,   // within the fixture world clock (2) — the M4 cursor gate
                        },
                        new TrainingState
                        {
                            Focus = TrainingFocus.Rest,
                            Condition = 8888,
                            TrainingFatigue = 0,
                            LastAdvancedWorldDay =
                                TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL,
                        },
                    }),
            };

            var medical = new[]
            {
                new ClubInjuryStates(
                    7,
                    new[] { 100, 300 },
                    new[]
                    {
                        new InjuryState
                        {
                            Severity = InjurySeverity.Moderate,
                            RecoveryRemaining = 21,
                            InjuryCount = 4,
                            LastAdvancedWorldDay = 1,   // within the fixture world clock (2) — the M4 cursor gate
                        },
                        new InjuryState
                        {
                            Severity = InjurySeverity.None,
                            RecoveryRemaining = 0,
                            InjuryCount = 1,
                            LastAdvancedWorldDay = 1,   // within the fixture world clock (2) — the M4 cursor gate
                        },
                    }),
            };

            // The appearance set must describe the same club/players as its siblings — Save gates the
            // triple's coherence (AR pass 3), because an incoherent file is one FromBlocks refuses.
            var appearance = new[]
            {
                new ClubAppearanceStates(
                    7,
                    new[] { 100, 300 },
                    new[]
                    {
                        new AppearanceState { RecentBits = 0b101u, BitsAsOfWorldDay = 1 },
                        default(AppearanceState),
                    }),
            };

            SeasonSaveManager.Save(
                PopulatedStore(), MidSeasonState(), matchOrNull: null, path, training, medical, appearance,
                ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true);

            SeasonSaveContents got = SeasonSaveManager.Load(path);

            Assert.AreEqual(1, got.TrainingClubs.Length, "one training club must survive the file");
            ClubTrainingStates club = got.TrainingClubs[0];
            Assert.AreEqual(7, club.ClubId, "the club id is written, not inferred from position");
            CollectionAssert.AreEqual(new[] { 100, 300 }, club.PlayerIds,
                "players come back keyed and in canonical ascending id order");
            Assert.AreEqual(TrainingFocus.Rest, club.States[0].Focus);
            Assert.AreEqual(8888, club.States[0].Condition);
            Assert.AreEqual(0, club.States[0].TrainingFatigue);
            Assert.AreEqual(TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL,
                club.States[0].LastAdvancedWorldDay,
                "the never-advanced sentinel must survive as itself, not collapse to day 0");
            Assert.AreEqual(TrainingFocus.Tactical, club.States[1].Focus);
            Assert.AreEqual(6543, club.States[1].Condition);
            Assert.AreEqual(1234, club.States[1].TrainingFatigue);
            Assert.AreEqual(1u, club.States[1].LastAdvancedWorldDay);

            Assert.AreEqual(1, got.MedicalClubs.Length, "one medical club must survive the file");
            ClubInjuryStates med = got.MedicalClubs[0];
            Assert.AreEqual(7, med.ClubId);
            CollectionAssert.AreEqual(new[] { 100, 300 }, med.PlayerIds);
            Assert.AreEqual(InjurySeverity.Moderate, med.States[0].Severity);
            Assert.AreEqual(21, med.States[0].RecoveryRemaining);
            Assert.AreEqual(4, med.States[0].InjuryCount);
            Assert.AreEqual(1u, med.States[0].LastAdvancedWorldDay);
            Assert.AreEqual(InjurySeverity.None, med.States[1].Severity);
            Assert.AreEqual(0, med.States[1].RecoveryRemaining);
            Assert.AreEqual(1, med.States[1].InjuryCount);
        }

        [Test]
        public void SaveLoad_WithNoTrainingOrMedicalState_YieldsEmptySetsNotNulls()
        {
            // Today nothing constructs #29/#41 state (both assemblies are inert until T2), so this is
            // the shape of EVERY save written right now. It must be an empty set, not a null and not an
            // absent block — otherwise the day T2 wires the producers is a second frame bump. Note the
            // caller SAYS "no training state" with NoTraining/NoMedical; Save has no default that would
            // let it stay silent, because silence and real-state-dropped look identical on reload.
            string path = TempPath("no-training.season");
            SeasonSaveManager.Save(PopulatedStore(), MidSeasonState(), matchOrNull: null, path, NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true);

            SeasonSaveContents got = SeasonSaveManager.Load(path);

            Assert.IsNotNull(got.TrainingClubs, "an unwired #29 must load as an empty set, not null");
            Assert.IsNotNull(got.MedicalClubs, "an unwired #41 must load as an empty set, not null");
            Assert.IsNotNull(got.AppearanceClubs, "an unwired appearance record likewise (AR pass 3)");
            Assert.AreEqual(0, got.TrainingClubs.Length);
            Assert.AreEqual(0, got.MedicalClubs.Length);
            Assert.AreEqual(0, got.AppearanceClubs.Length);
        }

        [Test]
        public void Save_NullTrainingOrMedicalClubs_Throws()
        {
            // The parameters are required and null is NOT the empty set. If null quietly meant "empty",
            // then at T2 a call site that omitted a season's conditioning, focus and injury history
            // would compile, save, and load back as empty arrays — indistinguishable from a game that
            // never had any. Nothing would throw and no assertion could fire. The `season` parameter is
            // required for exactly this reason (FR-SN-019); these are no more optional than it is.
            Assert.Throws<ArgumentNullException>(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    null, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "A null training set must fail loud — say Array.Empty to mean empty (FR-TR-018).");

            Assert.Throws<ArgumentNullException>(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    NoTraining, null, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "A null medical set must fail loud — say Array.Empty to mean empty (FR-MD-017).");

            Assert.Throws<ArgumentNullException>(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    NoTraining, NoMedical, null, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "A null appearance set must fail loud on the same terms (#30 Appendix B) — its two " +
                "siblings had this lock from T1 and it did not (AR pass 3).");
        }

        [Test]
        public void Save_IncoherentCareerBlockTriple_FailsLoud()
        {
            // AR pass 3: Save could write a file its own documented restore path
            // (PlayerCareerStates.FromBlocks) refuses on the coherence gate — the "Encode writes what
            // Decode refuses" class the T1 AR filed against TrainingSaveCodec. One training club with
            // zero medical/appearance clubs is exactly the file this suite itself used to write.
            var oneTrainingClub = new[]
            {
                new ClubTrainingStates(
                    7, new[] { 100 }, new[] { TrainingState.Create(TrainingFocus.Balanced) }),
            };

            Assert.Throws<ArgumentException>(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    oneTrainingClub, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "a career triple whose sets disagree on the club set must be refused at Save");
        }

        private static ClubTrainingStates TBlock(int club, params int[] ids)
        {
            var states = new TrainingState[ids.Length];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = TrainingState.Create(TrainingFocus.Balanced);
            }

            return new ClubTrainingStates(club, ids, states);
        }

        private static ClubInjuryStates MBlock(int club, params int[] ids)
        {
            var states = new InjuryState[ids.Length];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = InjuryState.Create();
            }

            return new ClubInjuryStates(club, ids, states);
        }

        private static ClubAppearanceStates ABlock(int club, params int[] ids) =>
            new ClubAppearanceStates(club, ids, new AppearanceState[ids.Length]);

        [Test]
        public void Save_CrossClubDuplicatePlayerId_FailsLoud()
        {
            // AR pass 4 (M1): the coherence gate's stated contract — never write a file FromBlocks
            // refuses — was one predicate short of FromBlocks' actual refusal surface: ERR-041-019's
            // global-id uniqueness, added to FromBlocks in the SAME commit as the gate. The
            // reviewer demonstrated a two-club triple sharing one id saving cleanly and the
            // documented restore path then refusing the loaded contents.
            Assert.Throws<ArgumentException>(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    new[] { TBlock(0, 7), TBlock(1, 7) },
                    new[] { MBlock(0, 7), MBlock(1, 7) },
                    new[] { ABlock(0, 7), ABlock(1, 7) },
                    ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "a cross-club duplicate PlayerId is refused at Save exactly as FromBlocks refuses "
                + "it at load (ERR-041-019)");
        }

        [Test]
        public void Save_DefaultCareerBlock_IsRefusedByName_NotByNullReference()
        {
            // AR pass 4 (L1): default(ClubTrainingStates) passed the gate's length checks and
            // NullReferenceException'd at the clone — before the gate existed, the codecs produced
            // the diagnosed ArgumentException. The refusal must name the cause again.
            // The siblings are EMPTY constructed blocks so the walk gets past the count check and
            // reaches the clone the null-guard protects (AR pass 5 M3: with 1-player siblings, the
            // reverted code threw the same exception TYPE from the count branch — this test killed
            // no mutant). The message assert pins WHICH branch refused.
            var ex = Assert.Throws<ArgumentException>(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    new ClubTrainingStates[1],
                    new[] { MBlock(0) },
                    new[] { ABlock(0) },
                    ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "a never-constructed block is refused by name, not by NullReferenceException");
            Assert.That(ex.Message, Does.Contain("default value"),
                "the refusal must come from the default-block branch, not a downstream mismatch");
            Assert.AreEqual("trainingClubs", ex.ParamName,
                "…and must name the OFFENDING set (AR pass 5 L10)");
        }

        [Test]
        public void Save_CoherenceGate_RefusesEachDisagreementShape_AndAcceptsPermutedClubOrder()
        {
            // AR pass 4 (L2): only the set-length branch had a lock. Each remaining refusal branch,
            // plus the documented club-order insensitivity as a PASS case — the codecs canonicalize
            // at encode, so a caller may hand the three sets in different club orders.
            Assert.Throws<ArgumentException>(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    new[] { TBlock(0, 7), TBlock(1, 30) },
                    new[] { MBlock(0, 7), MBlock(2, 30) },
                    new[] { ABlock(0, 7), ABlock(1, 30) },
                    ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "same lengths, different club SET — refused");

            Assert.Throws<ArgumentException>(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    new[] { TBlock(0, 7, 9) },
                    new[] { MBlock(0, 7) },
                    new[] { ABlock(0, 7) },
                    ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "same club, different player COUNT — refused");

            Assert.Throws<ArgumentException>(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    new[] { TBlock(0, 7) },
                    new[] { MBlock(0, 8) },
                    new[] { ABlock(0, 7) },
                    ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "same club, same count, different player IDS — refused");

            string path = TempPath("permuted.season");
            Assert.DoesNotThrow(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, path,
                    new[] { TBlock(1, 30), TBlock(0, 7) },
                    new[] { MBlock(0, 7), MBlock(1, 30) },
                    new[] { ABlock(1, 30), ABlock(0, 7) },
                    ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "the gate is order-insensitive on clubs — the codecs canonicalize at encode");

            SeasonSaveContents got = SeasonSaveManager.Load(path);
            Assert.AreEqual(2, got.TrainingClubs.Length,
                "…and the permuted-order save loads back canonicalized");
        }

        [Test]
        public void Save_FutureDatedCareerCursor_FailsLoud()
        {
            // AR pass 5 (M4): a per-player world-day cursor AHEAD of the world clock is a mispaired
            // or hand-edited save. The appearance anchor is the acute case — with the dial armed,
            // the slot-4 window read throws on every later day and the day steps run BEFORE the
            // clock increment, so the career wedges PERMANENTLY while saving and reloading cleanly.
            // The #29/#41 cursors fail the other way (silent per-player no-op). Checked at Save so
            // this root never writes a file its own Load refuses.
            WorldStore world = PopulatedStore();   // world clock sits at 2

            var futureAppearance = new ClubAppearanceStates(
                7, new[] { 100 },
                new[] { new AppearanceState { RecentBits = 1u, BitsAsOfWorldDay = 500u } });
            Assert.Throws<InvalidOperationException>(
                () => SeasonSaveManager.Save(
                    world, MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    new[] { TBlock(7, 100) }, new[] { MBlock(7, 100) }, new[] { futureAppearance },
                    ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "a future-dated appearance anchor wedges the career; refuse it at the write");

            var futureTraining = TBlock(7, 100);
            futureTraining.States[0].LastAdvancedWorldDay = 500u;
            Assert.Throws<InvalidOperationException>(
                () => SeasonSaveManager.Save(
                    world, MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    new[] { futureTraining }, new[] { MBlock(7, 100) }, new[] { ABlock(7, 100) },
                    ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "a future-dated training cursor silently freezes the player out of the day step");

            var futureMedical = MBlock(7, 100);
            futureMedical.States[0].LastAdvancedWorldDay = 500u;
            Assert.Throws<InvalidOperationException>(
                () => SeasonSaveManager.Save(
                    world, MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    new[] { TBlock(7, 100) }, new[] { futureMedical }, new[] { ABlock(7, 100) },
                    ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "…and the medical cursor likewise");
        }

        [Test]
        public void Save_DuplicateClubId_IsRefusedByName()
        {
            // AR pass 7 (L4): the pass-6 duplicate-ClubId refusal had no test. Without it the
            // duplicate is still caught downstream (the codec's canonical order), so the risk is a
            // MISLEADING diagnostic — the unstable sorts pair duplicate clubs arbitrarily and the
            // gate would name a phantom mismatch. Message + paramName pinned, the pass-5 M3 shape.
            var ex = Assert.Throws<ArgumentException>(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    new[] { TBlock(7, 100), TBlock(7, 101) },
                    new[] { MBlock(7, 100), MBlock(7, 101) },
                    new[] { ABlock(7, 100), ABlock(7, 101) },
                    ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "a club appearing twice in the career triple is refused by name");
            Assert.That(ex.Message, Does.Contain("twice"),
                "the refusal must come from the duplicate-club branch, not a phantom mismatch");
            Assert.AreEqual("trainingClubs", ex.ParamName);
        }

        [Test]
        public void Save_CursorLaggingTheClockByTwoOrMore_FailsLoud()
        {
            // AR pass 6 (M2): the mirror case is WORSE than ahead — the F7 gap refusal fires on every
            // later advance and the day steps run before the clock increment, so the career wedges
            // permanently while the file saves and reloads cleanly (demonstrated by the reviewer
            // through public API). A legitimate save sits at worldTick − cursor ∈ {0, 1}; the
            // sentinel (fresh state) is exempt, and the appearance anchor has no gap contract.
            WorldStore laggedWorld = PopulatedStore();   // clock 2

            var laggingTraining = TBlock(7, 100);
            laggingTraining.States[0].LastAdvancedWorldDay = 0u;   // clock 2 → gap of 2
            Assert.Throws<InvalidOperationException>(
                () => SeasonSaveManager.Save(
                    laggedWorld, MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    new[] { laggingTraining }, new[] { MBlock(7, 100) }, new[] { ABlock(7, 100) },
                    ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "a training cursor two behind the clock wedges the career on the next advance (F7)");

            // Medical-only lag (AR pass 9 M1): the training cursor sits at the LEGITIMATE lag of 1,
            // so only the medical predicate can refuse — pass 8's isolation lesson at the file
            // boundary. Before the shared-owner collapse this predicate had no isolating case at
            // Save or Load: deleting the medical gap clause left the whole suite green.
            var okT = TBlock(7, 100);
            okT.States[0].LastAdvancedWorldDay = 1u;               // clock 2 → legitimate lag of 1
            var laggingMedical = MBlock(7, 100);
            laggingMedical.States[0].LastAdvancedWorldDay = 0u;    // clock 2 → gap of 2
            Assert.Throws<InvalidOperationException>(
                () => SeasonSaveManager.Save(
                    laggedWorld, MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    new[] { okT }, new[] { laggingMedical }, new[] { ABlock(7, 100) },
                    ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "a medical cursor two behind the clock is refused on its own — the training cursor "
                + "is in-band, so this throw can only come from the medical predicate");

            var okTraining = TBlock(7, 100);
            okTraining.States[0].LastAdvancedWorldDay = 1u;        // clock 2 → the legitimate lag of 1
            Assert.DoesNotThrow(
                () => SeasonSaveManager.Save(
                    laggedWorld, MidSeasonState(), matchOrNull: null, TempPath("ok.season"),
                    new[] { okTraining }, new[] { MBlock(7, 100) }, new[] { ABlock(7, 100) },
                    ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "the pre-increment convention's lag of exactly one is the NORMAL saved state");
        }

        [Test]
        public void Load_FutureDatedCareerCursor_FailsLoud()
        {
            // The same rule at the layer a hand-edited or mispaired file actually arrives through.
            // Save now refuses this content, so the file is crafted through SeasonSaveCodec.Encode
            // directly — exactly what "hand-edited" means.
            WorldStore world = PopulatedStore();
            var futureAppearance = new[]
            {
                new ClubAppearanceStates(
                    7, new[] { 100 },
                    new[] { new AppearanceState { RecentBits = 1u, BitsAsOfWorldDay = 500u } }),
            };

            var trainingBlock = new TrainingBlock(TrainingSaveCodec.Encode(new[] { TBlock(7, 100) }));
            var medicalBlock = new MedicalBlock(MedicalSaveCodec.Encode(new[] { MBlock(7, 100) }));
            var appearanceBlock = new AppearanceBlock(AppearanceSaveCodec.Encode(futureAppearance));
            var progressionBlock = new ProgressionBlock(
                ProgressionSaveCodec.Encode(Array.Empty<ClubCareerStates>(), 0));
            byte[] frame = SeasonSaveCodec.Encode(
                world.Snapshot(), SeasonStateCodec.Encode(MidSeasonState()),
                in trainingBlock, in medicalBlock, in appearanceBlock, in progressionBlock, EmptyDisciplineBlock, null);

            string path = TempPath("wedged.season");
            File.WriteAllBytes(path, frame);

            Assert.Throws<InvalidOperationException>(() => SeasonSaveManager.Load(path),
                "a future-dated appearance anchor must be refused at load — undetected, the career "
                + "cannot advance a single day and cannot be diagnosed from any later symptom");
        }

        // ── ERR-028-007: the fourth persisted cursor, at the Save / Load boundaries ──

        [Test]
        public void Save_FutureDatedProgressionCursor_FailsLoud()
        {
            // The #28 twin of Save_FutureDatedCareerCursor_FailsLoud above, checked on the same terms.
            WorldStore world = PopulatedStore();   // world clock sits at 2
            ProgressionEngine future = ProgressionFor(PBlock(7, 100, lastAdvancedWorldDay: 500u));

            Assert.Throws<InvalidOperationException>(
                () => SeasonSaveManager.Save(
                    world, MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    TrainingMatching(7, 100), MedicalMatching(7, 100), AppearanceMatching(7, 100),
                    future, new DisciplineState(), disciplineWired: true),
                "ERR-028-007: a future-dated progression cursor must be refused at Save, exactly like " +
                "its #29/#41 siblings.");
        }

        [Test]
        public void Save_ProgressionCursorLaggingTheClockByTwoOrMore_FailsLoud()
        {
            WorldStore laggedWorld = PopulatedStore();   // clock 2
            ProgressionEngine lagging = ProgressionFor(PBlock(7, 100, lastAdvancedWorldDay: 0u));   // gap of 2

            Assert.Throws<InvalidOperationException>(
                () => SeasonSaveManager.Save(
                    laggedWorld, MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    TrainingMatching(7, 100), MedicalMatching(7, 100), AppearanceMatching(7, 100),
                    lagging, new DisciplineState(), disciplineWired: true),
                "ERR-028-007: a progression cursor two behind the clock must be refused — AdvanceDay " +
                "REPLAYS a gap, so a mispaired file would bank days of growth from one day's inputs, " +
                "invisibly (worse than the sibling cursors' silent-freeze failure mode).");
        }

        [Test]
        public void Save_ProgressionCursorLaggingTheClockByOne_IsAccepted()
        {
            WorldStore laggedWorld = PopulatedStore();   // clock 2
            ProgressionEngine ok = ProgressionFor(PBlock(7, 100, lastAdvancedWorldDay: 1u));   // legitimate lag of 1

            Assert.DoesNotThrow(
                () => SeasonSaveManager.Save(
                    laggedWorld, MidSeasonState(), matchOrNull: null, TempPath("ok-progression.season"),
                    TrainingMatching(7, 100), MedicalMatching(7, 100), AppearanceMatching(7, 100),
                    ok, new DisciplineState(), disciplineWired: true),
                "the pre-increment convention's lag of exactly one is the NORMAL saved state.");
        }

        [Test]
        public void Load_FutureDatedProgressionCursor_FailsLoud()
        {
            // The same rule at the layer a hand-edited or mispaired file actually arrives through —
            // crafted through SeasonSaveCodec.Encode directly (Save now refuses to WRITE this content,
            // proven above), which isolates the SEPARATE check Load performs on its own: the third of
            // ERR-028-007's three boundaries.
            WorldStore world = PopulatedStore();

            var trainingBlock = new TrainingBlock(TrainingSaveCodec.Encode(NoTraining));
            var medicalBlock = new MedicalBlock(MedicalSaveCodec.Encode(NoMedical));
            var appearanceBlock = new AppearanceBlock(AppearanceSaveCodec.Encode(NoAppearance));
            var progressionBlock = new ProgressionBlock(
                ProgressionSaveCodec.Encode(new[] { PBlock(7, 100, lastAdvancedWorldDay: 500u) }, 101));
            byte[] frame = SeasonSaveCodec.Encode(
                world.Snapshot(), SeasonStateCodec.Encode(MidSeasonState()),
                in trainingBlock, in medicalBlock, in appearanceBlock, in progressionBlock, EmptyDisciplineBlock, null);

            string path = TempPath("wedged-progression.season");
            File.WriteAllBytes(path, frame);

            Assert.Throws<InvalidOperationException>(() => SeasonSaveManager.Load(path),
                "ERR-028-007: a future-dated progression cursor must be refused at Load too — checked " +
                "independently of Save, the third of the three boundaries.");
        }

        // ── AR pass 6, M2(b): the BirthWorldDay-vs-clock sibling, at the same two boundaries ──

        [Test]
        public void Save_FutureDatedBirthWorldDay_FailsLoud()
        {
            // The M2(b) twin of Save_FutureDatedProgressionCursor_FailsLoud above, checked on the same
            // terms. lastAdvancedWorldDay stays at the ordinary lag of 1 (world clock 2) so only the
            // BirthWorldDay predicate can be what refuses this.
            WorldStore world = PopulatedStore();   // world clock sits at 2
            ProgressionEngine future = ProgressionFor(
                PBlockWithBirthDay(7, 100, lastAdvancedWorldDay: 1u, birthWorldDay: 500L));

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => SeasonSaveManager.Save(
                    world, MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    TrainingMatching(7, 100), MedicalMatching(7, 100), AppearanceMatching(7, 100),
                    future, new DisciplineState(), disciplineWired: true),
                "M2(b): a future-dated BirthWorldDay must be refused at Save, exactly like the " +
                "progression cursor above.");
            StringAssert.Contains("BirthWorldDay", ex.Message,
                "…and it must be the BirthWorldDay predicate that refuses this, not the cursor one.");
        }

        [Test]
        public void Load_FutureDatedBirthWorldDay_FailsLoud()
        {
            // The same rule at the layer a hand-edited or mispaired file actually arrives through —
            // crafted through ProgressionSaveCodec.Encode directly (Save now refuses to WRITE this
            // content, proven above), isolating Load's own separate check.
            WorldStore world = PopulatedStore();

            var trainingBlock = new TrainingBlock(TrainingSaveCodec.Encode(NoTraining));
            var medicalBlock = new MedicalBlock(MedicalSaveCodec.Encode(NoMedical));
            var appearanceBlock = new AppearanceBlock(AppearanceSaveCodec.Encode(NoAppearance));
            var progressionBlock = new ProgressionBlock(
                ProgressionSaveCodec.Encode(
                    new[] { PBlockWithBirthDay(7, 100, lastAdvancedWorldDay: 1u, birthWorldDay: 500L) },
                    101));
            byte[] frame = SeasonSaveCodec.Encode(
                world.Snapshot(), SeasonStateCodec.Encode(MidSeasonState()),
                in trainingBlock, in medicalBlock, in appearanceBlock, in progressionBlock, EmptyDisciplineBlock, null);

            string path = TempPath("wedged-birthday.season");
            File.WriteAllBytes(path, frame);

            Assert.Throws<InvalidOperationException>(() => SeasonSaveManager.Load(path),
                "M2(b): a future-dated BirthWorldDay must be refused at Load too — checked " +
                "independently of Save.");
        }

        // The PBlock twin with a caller-supplied BirthWorldDay, for the two cases above — PBlock itself
        // fixes it at 0, which is never ahead of any non-negative world clock.
        private static ClubCareerStates PBlockWithBirthDay(
            int club, int playerId, uint lastAdvancedWorldDay, long birthWorldDay)
        {
            PlayerRecord rec = PlayerRecord.CreateDefault(playerId);
            var life = new PlayerLifecycle
            {
                PotentialAbility = PlayerProgressionConstants.PA_MIN,
                CurrentAbility = PBlockDefaultCurrentAbility,
                GrowthCursor = 0,
                BirthWorldDay = birthWorldDay,
                RetirementFlag = false,
                RetirementDay = 0,
                LastAdvancedWorldDay = lastAdvancedWorldDay,
            };
            return new ClubCareerStates(club, new[] { rec }, new[] { life });
        }

        // Mirrors TBlock/MBlock/ABlock above: a one-player #28 block with a hand-set cursor, so the
        // fixture controls exactly the field the cursor-vs-clock gate reads. Everything else is filler
        // (irrelevant to that gate).
        // Career blocks that AGREE with a PBlock's club and player. The four-way coherence gate
        // (ERR-028-011c) refuses a populated progression set beside empty career sets — correctly, since
        // a file claiming a roster but no career state for it is exactly the mismatch that gate exists
        // to catch. These cursor tests are about the CURSOR rule, so they supply a coherent career.
        [Test]
        public void Save_ProgressionSetDisagreeingWithTheCareerSets_IsRefused()
        {
            // ERR-028-011(c). Before the four-way gate, Save wrote and Load accepted a file whose PROG
            // block described one club set while training/medical/appearance described another. The
            // failure then surfaced somewhere else entirely — a null squad mid-restore, or the
            // SeasonLoop constructor throwing at composition — long after the file had been declared
            // good. The whole point of this gate is that Save never writes what Load refuses.
            ProgressionEngine progression = ProgressionFor(PBlock(7, 100, lastAdvancedWorldDay: 1u));

            Assert.Throws<ArgumentException>(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    TrainingMatching(8, 100), MedicalMatching(8, 100), AppearanceMatching(8, 100),
                    progression, new DisciplineState(), disciplineWired: true),
                "the progression set carries club 7 and the career sets club 8 — all four describe one "
                + "career and must agree.");
        }

        [Test]
        public void Save_ProgressionSetWithADifferentPlayer_IsRefused()
        {
            // The same gate one level down: same club, different member set. Club agreement alone is
            // not coherence — the career blocks must describe the same PLAYERS the roster carries.
            ProgressionEngine progression = ProgressionFor(PBlock(7, 100, lastAdvancedWorldDay: 1u));

            Assert.Throws<ArgumentException>(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    TrainingMatching(7, 999), MedicalMatching(7, 999), AppearanceMatching(7, 999),
                    progression, new DisciplineState(), disciplineWired: true));
        }

        [Test]
        public void Save_AnEmptyProgressionSet_BesideEmptyCareerSets_IsAccepted()
        {
            // The exemption is deliberate and must stay: a career whose rosters come from the bootstrap
            // (the pre-#28 composition) writes an empty progression block, and that is coherent. If the
            // gate ever refuses this it has become over-broad — the mistake made and corrected once
            // already at ERR-028-008.
            Assert.DoesNotThrow(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, TempPath("x.season"),
                    NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true));
        }

        private static ClubTrainingStates[] TrainingMatching(int club, int playerId) =>
            new[] { new ClubTrainingStates(club, new[] { playerId },
                new[] { TrainingState.Create(TrainingFocus.Balanced) }) };

        private static ClubInjuryStates[] MedicalMatching(int club, int playerId) =>
            new[] { new ClubInjuryStates(club, new[] { playerId }, new[] { InjuryState.Create() }) };

        private static ClubAppearanceStates[] AppearanceMatching(int club, int playerId) =>
            new[] { new ClubAppearanceStates(club, new[] { playerId }, new AppearanceState[1]) };

        // L-4 (AR pass 8): DescribeOutOfRangeValues now requires CurrentAbility == ComputeCA(attributes)
        // exactly. PBlock/PBlockWithBirthDay always build PlayerRecord.CreateDefault (Midfielder,
        // uniform [1,20] attributes), so their CurrentAbility owes this one computed value, not
        // PA_MIN — PotentialAbility keeps PA_MIN (its own, independent range check).
        private static readonly PlayerAttributes PBlockDefaultAttributes = PlayerAttributes.CreateDefault();
        private static readonly int PBlockDefaultCurrentAbility =
            AbilityModel.ComputeCA(in PBlockDefaultAttributes, PlayerPosition.Midfielder);

        private static ClubCareerStates PBlock(int club, int playerId, uint lastAdvancedWorldDay)
        {
            PlayerRecord rec = PlayerRecord.CreateDefault(playerId);
            var life = new PlayerLifecycle
            {
                PotentialAbility = PlayerProgressionConstants.PA_MIN,
                CurrentAbility = PBlockDefaultCurrentAbility,
                GrowthCursor = 0,
                BirthWorldDay = 0,
                RetirementFlag = false,
                RetirementDay = 0,
                LastAdvancedWorldDay = lastAdvancedWorldDay,
            };
            return new ClubCareerStates(club, new[] { rec }, new[] { life });
        }

        private static ProgressionEngine ProgressionFor(ClubCareerStates block) =>
            ProgressionEngine.FromBlocks(new[] { block }, nextPlayerId: block.Records[0].PlayerId + 1);

        // ── ERR-028-008: refuse to overwrite a roster with an empty one ──────────────

        [Test]
        public void Save_AnEmptyProgressionStore_OverAPopulatedRoster_IsRefused()
        {
            // #28's block IS the roster (KD-4), so writing a zero-club block over a file that carries
            // one would delete a career's banked growth with every OTHER gate green (world, season,
            // training, medical and appearance all intact around the hole).
            WorldStore world = PopulatedStore();
            ProgressionEngine populated = ProgressionFor(PBlock(7, 100, lastAdvancedWorldDay: 1u));
            string path = TempPath("populated-roster.season");

            SeasonSaveManager.Save(
                world, MidSeasonState(), matchOrNull: null, path,
                TrainingMatching(7, 100), MedicalMatching(7, 100), AppearanceMatching(7, 100),
                populated, new DisciplineState(), disciplineWired: true);

            Assert.Throws<InvalidOperationException>(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, path,
                    NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "ERR-028-008: an empty progression store must not overwrite a file carrying a roster.");

            SeasonSaveContents reloaded = SeasonSaveManager.Load(path);
            Assert.AreEqual(1, reloaded.Progression.ClubCount,
                "the original file must be untouched by the refused write — the roster must still be there.");
        }

        [Test]
        public void Save_AnEmptyCareerTriple_OverAPopulatedCareer_IsRefused()
        {
            // AR pass 8, and the sibling of the lock above one block-family over. The roster guard was
            // written when "populated store, no career" was an ILLEGAL composition; ERR-028-013 made it
            // legal and the AR pass 5 coherence carve-out made it saveable, and neither revisited this.
            //
            // Demonstrated before the fix: load a populated save, resume through the blessed
            // progression-only constructor, save back to the same path — no throw, reload returns
            // TrainingClubs=0 / MedicalClubs=0 / AppearanceClubs=0 beside Progression=1. A season of
            // conditioning, injury history and appearance records deleted with every gate green.
            WorldStore world = PopulatedStore();
            ProgressionEngine populated = ProgressionFor(PBlock(7, 100, lastAdvancedWorldDay: 1u));
            string path = TempPath("populated-career.season");

            SeasonSaveManager.Save(
                world, MidSeasonState(), matchOrNull: null, path,
                TrainingMatching(7, 100), MedicalMatching(7, 100), AppearanceMatching(7, 100),
                populated, new DisciplineState(), disciplineWired: true);

            Assert.Throws<InvalidOperationException>(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, path,
                    NoTraining, NoMedical, NoAppearance,
                    ProgressionFor(PBlock(7, 100, lastAdvancedWorldDay: 1u)), new DisciplineState(), disciplineWired: true),
                "an empty career triple must not overwrite a file carrying one — the roster surviving "
                + "beside the hole is what makes this silent.");

            SeasonSaveContents reloaded = SeasonSaveManager.Load(path);
            Assert.AreEqual(1, reloaded.TrainingClubs.Length,
                "the refused write must leave the original career intact.");
            Assert.AreEqual(1, reloaded.MedicalClubs.Length);
            Assert.AreEqual(1, reloaded.AppearanceClubs.Length);
        }

        [Test]
        public void Save_AnEmptyCareerTriple_ToANewPath_Succeeds()
        {
            // The carve-out the guard must not break: a career-less composition is legal and must be
            // able to create a file. Only OVERWRITING a populated one is refused.
            string path = TempPath("fresh-empty-career.season");

            Assert.DoesNotThrow(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, path,
                    NoTraining, NoMedical, NoAppearance,
                    ProgressionFor(PBlock(7, 100, lastAdvancedWorldDay: 1u)), new DisciplineState(), disciplineWired: true),
                "an empty career triple may freely create a new file — nothing there to protect.");
            Assert.IsTrue(File.Exists(path));
        }

        [Test]
        public void Save_AnEmptyCareerTriple_OverAnAlreadyEmptyCareer_Succeeds()
        {
            // The second half of the carve-out: repeated saves of a legitimately career-less game must
            // keep working, or the guard breaks the composition ERR-028-013 blessed.
            string path = TempPath("empty-career-twice.season");

            SeasonSaveManager.Save(
                PopulatedStore(), MidSeasonState(), matchOrNull: null, path,
                NoTraining, NoMedical, NoAppearance,
                ProgressionFor(PBlock(7, 100, lastAdvancedWorldDay: 1u)), new DisciplineState(), disciplineWired: true);

            Assert.DoesNotThrow(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, path,
                    NoTraining, NoMedical, NoAppearance,
                    ProgressionFor(PBlock(7, 100, lastAdvancedWorldDay: 1u)), new DisciplineState(), disciplineWired: true),
                "overwriting an already career-less file is not a loss and must stay legal.");
        }

        [Test]
        public void Save_AnEmptyProgressionStore_ToANewPath_Succeeds()
        {
            string path = TempPath("fresh-empty-roster.season");

            Assert.DoesNotThrow(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, path,
                    NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "an empty store may freely create a new file — there is nothing there to protect.");
            Assert.IsTrue(File.Exists(path));
        }

        [Test]
        public void Save_AnEmptyProgressionStore_OverAnAlreadyEmptyFile_Succeeds()
        {
            string path = TempPath("already-empty-roster.season");
            SeasonSaveManager.Save(
                PopulatedStore(), MidSeasonState(), matchOrNull: null, path,
                NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true);

            Assert.DoesNotThrow(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, path,
                    NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "an empty store may overwrite a file that itself already carries an empty roster.");
        }

        // ── ERR-030-036: a resume-and-save cycle must not forgive every outstanding suspension ──

        /// <summary>A tally in which <paramref name="playerId"/> carries a live ban and two yellows.</summary>
        private static DisciplineState TallyFor(int playerId) =>
            DisciplineState.FromEntries(new[]
            {
                new DisciplineEntry(
                    playerId, DisciplineConstants.LEAGUE_COMPETITION_KEY, yellows: 2,
                    banMatchesRemaining: 3),
            });

        [Test]
        public void Restore_CarriesTheDisciplineTallyIntoTheResumedLoop()
        {
            // ERR-030-036, the half that makes a correct resume EXPRESSIBLE. SeasonLoop.Restore took six
            // parameters and none of them was the tally, so a loop rebuilt through the documented
            // restore path had Discipline == null whatever the file carried — and the next
            // Save(SeasonLoop, …) then wrote `loop.Discipline ?? new DisciplineState()`, a well-formed
            // ZERO-ENTRY block, over the live one.
            WorldStore world = PopulatedStore();
            DisciplineState tally = TallyFor(100);

            SeasonLoop restored = SeasonLoop.Restore(
                world, SeasonStateCodec.Encode(MidSeasonState()), RoundResolutionMode.QuickSimAll,
                careerOrNull: null, careerSquadsOrNull: null, progressionOrNull: null,
                disciplineOrNull: tally);

            Assert.IsNotNull(restored.Discipline,
                "The restored loop drives no discipline at all, so every card it would show is lost and "
                + "every ban it holds is already forgiven — Restore must forward disciplineOrNull.");
            Assert.AreSame(tally, restored.Discipline,
                "The loop must drive the very tally handed to it: #44's state is mutated in place by "
                + "the fold and the serving decrement, so a copy would strand every update.");
            Assert.AreEqual(1, restored.Discipline.Count);
            Assert.AreEqual(3,
                restored.Discipline.EntryFor(100, DisciplineConstants.LEAGUE_COMPETITION_KEY)
                    .BanMatchesRemaining,
                "the outstanding ban must survive the restore.");
        }

        [Test]
        public void Save_AnUnwiredDiscipline_OverAPopulatedTally_IsRefused()
        {
            // ERR-030-036's headline: ERR-028-008's measured shape in the third block family, and the
            // one neither existing destination guard can see. RequireDestinationCarriesNoRoster fires
            // only when the #28 store is empty and RequireDestinationCarriesNoCareer only when all
            // three career blocks are, so a resumed career with BOTH populated and the tally dropped
            // sails through both — and every ban and every yellow in the destination is gone, frame
            // still v6, Load still succeeding. FR-DC-014 retains no ledgers, so nothing can recompute
            // them.
            //
            // Driven with disciplineWired: false since ERR-030-038, because that is the fact the guard
            // keys on. It is a parameter of the ONE public long form since ERR-030-039 — this comment
            // previously read "the public long form cannot express this case at all", which was the
            // false premise behind that form hardcoding disciplineWired: true and bypassing this guard
            // entirely (H4). An empty tally is FR-DC-017's ordinary state rather than evidence of a
            // drop; see AWiredTallyThatDrainsToZero_SavesOverItsOwnPopulatedFile below for the case the
            // old Count == 0 keying wrongly refused, and
            // Save_ThePublicLongForm_DrivingNoDiscipline_CannotEmptyAPopulatedTally for the entry point
            // the flag used to be answered on the caller's behalf at.
            WorldStore world = PopulatedStore();
            ProgressionEngine populated = ProgressionFor(PBlock(7, 100, lastAdvancedWorldDay: 1u));
            string path = TempPath("populated-discipline.season");

            SeasonSaveManager.Save(
                world, MidSeasonState(), matchOrNull: null, path,
                TrainingMatching(7, 100), MedicalMatching(7, 100), AppearanceMatching(7, 100),
                populated, TallyFor(100), disciplineWired: true);

            // Everything else about the second save is legitimate — the roster and the career triple are
            // both populated, so neither sibling guard has anything to say. Only the tally is missing.
            Assert.Throws<InvalidOperationException>(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, path,
                    TrainingMatching(7, 100), MedicalMatching(7, 100), AppearanceMatching(7, 100),
                    ProgressionFor(PBlock(7, 100, lastAdvancedWorldDay: 1u)), new DisciplineState(),
                    disciplineWired: false),
                "a save driving no discipline must not overwrite a file carrying a tally — the roster "
                + "and the whole career surviving beside the hole is exactly what makes this silent.");

            SeasonSaveContents reloaded = SeasonSaveManager.Load(path);
            Assert.AreEqual(1, reloaded.Discipline.Count,
                "the refused write must leave the original tally intact.");
            Assert.AreEqual(3,
                reloaded.Discipline.EntryFor(100, DisciplineConstants.LEAGUE_COMPETITION_KEY)
                    .BanMatchesRemaining);
        }

        [Test]
        public void Save_AnUnwiredDiscipline_ToANewPath_Succeeds()
        {
            // The carve-out the guard must not break: a game that drives no discipline writes a
            // well-formed zero-entry block, and that is the honest state, not a loss. Driven unwired
            // (ERR-030-038) so it exercises the guard rather than the branch that never calls it.
            string path = TempPath("fresh-empty-discipline.season");

            Assert.DoesNotThrow(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, path,
                    NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(),
                    disciplineWired: false),
                "an unwired save may freely create a new file — there is nothing there to protect.");
            Assert.IsTrue(File.Exists(path));
        }

        [Test]
        public void Save_AnUnwiredDiscipline_OverAnAlreadyEmptyOne_Succeeds()
        {
            // The second half of the carve-out: a discipline-less career must stay repeatedly saveable,
            // or the guard breaks every save written before the first card of the season.
            string path = TempPath("empty-discipline-twice.season");
            SeasonSaveManager.Save(
                PopulatedStore(), MidSeasonState(), matchOrNull: null, path,
                NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(),
                disciplineWired: false);

            Assert.DoesNotThrow(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, path,
                    NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(),
                    disciplineWired: false),
                "overwriting an already card-less file is not a loss and must stay legal.");
        }

        [Test]
        public void ResumeThroughRestore_AndSaveBack_KeepsEveryOutstandingSuspension()
        {
            // The two halves of ERR-030-036 driven end to end, through the documented resume path: Load
            // the file, rebuild the loop with SeasonSaveContents.Discipline, save back to the same path.
            // Before the fix the parameter did not exist, so this sequence was unwritable — and the
            // sequence that WAS writable (omit the tally) wrote the empty block with nothing to stop it.
            WorldStore world = PopulatedStore();
            string path = TempPath("resume-discipline.season");

            SeasonSaveManager.Save(
                world, MidSeasonState(), matchOrNull: null, path,
                NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, TallyFor(100), disciplineWired: true);

            SeasonSaveContents contents = SeasonSaveManager.Load(path);
            SeasonLoop resumed = SeasonLoop.Restore(
                contents.World, SeasonStateCodec.Encode(contents.Season),
                RoundResolutionMode.QuickSimAll,
                careerOrNull: null, careerSquadsOrNull: null, progressionOrNull: null,
                disciplineOrNull: contents.Discipline);

            Assert.DoesNotThrow(
                () => SeasonSaveManager.Save(resumed, matchOrNull: null, path),
                "a loop resumed WITH the file's tally must save back over its own file — the guard "
                + "protects against dropping the tally, not against carrying it.");

            SeasonSaveContents reloaded = SeasonSaveManager.Load(path);
            Assert.AreEqual(1, reloaded.Discipline.Count,
                "the resume-and-save cycle silently forgave the whole season's discipline.");
            Assert.AreEqual(3,
                reloaded.Discipline.EntryFor(100, DisciplineConstants.LEAGUE_COMPETITION_KEY)
                    .BanMatchesRemaining,
                "the outstanding ban must survive a resume-and-save cycle unchanged.");
            Assert.AreEqual(2,
                reloaded.Discipline.EntryFor(100, DisciplineConstants.LEAGUE_COMPETITION_KEY).Yellows,
                "the accumulated yellows must survive it too — FR-DC-014 keeps no ledger to rebuild "
                + "them from.");
        }

        [Test]
        public void ResumeThatDropsTheTally_AndSavesBack_IsRefused()
        {
            // The negative control the guard exists for, and the one a caller actually reaches: the same
            // resume as above with the tally simply not threaded through. It compiles, the loop runs, and
            // before the guard the save succeeded — deleting the season's discipline in a green write.
            WorldStore world = PopulatedStore();
            string path = TempPath("resume-drops-discipline.season");

            SeasonSaveManager.Save(
                world, MidSeasonState(), matchOrNull: null, path,
                NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, TallyFor(100), disciplineWired: true);

            SeasonSaveContents contents = SeasonSaveManager.Load(path);
            SeasonLoop resumed = SeasonLoop.Restore(
                contents.World, SeasonStateCodec.Encode(contents.Season),
                RoundResolutionMode.QuickSimAll);

            Assert.IsNull(resumed.Discipline, "Precondition: this resume drives no discipline.");
            Assert.Throws<InvalidOperationException>(
                () => SeasonSaveManager.Save(resumed, matchOrNull: null, path),
                "Save(SeasonLoop, …) writes `loop.Discipline ?? new DisciplineState()`, so a resume that "
                + "dropped the tally writes a zero-entry block over the live one. It must be refused at "
                + "the write — the loss is a property of the DESTINATION, which no signature can force a "
                + "caller to consider.");

            SeasonSaveContents reloaded = SeasonSaveManager.Load(path);
            Assert.AreEqual(1, reloaded.Discipline.Count,
                "the refused write must leave the file's tally intact.");
        }

        // ── ERR-030-039: no write entry point may assert the wiring fact for its caller ──

        [Test]
        public void Save_ThePublicLongForm_DrivingNoDiscipline_CannotEmptyAPopulatedTally()
        {
            // ERR-030-039 (H4): a defect in ERR-030-038's own fix. That fix put disciplineWired on an
            // INTERNAL overload and had the public nine-argument form pass `true` unconditionally, on
            // the reasoning that a caller handing over a tally drives #44 by construction. That
            // excludes only null — and `new DisciplineState()` is exactly what an unwired caller
            // passes, because the `discipline` parameter's own documentation sanctions it as the way
            // to say "no cards recorded yet". So the guard was live on one write entry point and
            // bypassed on the other, and the bypassed one is the form every external caller reaches:
            // two identical public calls, the second with a fresh tally, silently took the destination
            // from one row to zero (measured against the built assemblies before the fix).
            //
            // This drives the PUBLIC form on both calls. It is the entry point half of the lock; the
            // guard's own behaviour is locked above.
            string path = TempPath("public-form-unwired-discipline.season");

            SeasonSaveManager.Save(
                PopulatedStore(), MidSeasonState(), matchOrNull: null, path,
                NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, TallyFor(100),
                disciplineWired: true);
            Assert.AreEqual(1, SeasonSaveManager.Load(path).Discipline.Count,
                "Precondition: the destination carries the row the second save must not be able to "
                + "delete.");

            Assert.Throws<InvalidOperationException>(
                () => SeasonSaveManager.Save(
                    PopulatedStore(), MidSeasonState(), matchOrNull: null, path,
                    NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty,
                    new DisciplineState(), disciplineWired: false),
                "the public long form must be able to SAY it drives no discipline, and be refused for "
                + "it. While it asserted disciplineWired: true on the caller's behalf, this exact call "
                + "wrote a zero-entry DISC block over a populated one with every gate green — and "
                + "FR-DC-014 retains no ledgers, so the bans and yellows were unrecoverable.");

            Assert.AreEqual(1, SeasonSaveManager.Load(path).Discipline.Count,
                "the refused write must leave the destination's tally intact.");
            Assert.AreEqual(3,
                SeasonSaveManager.Load(path).Discipline
                    .EntryFor(100, DisciplineConstants.LEAGUE_COMPETITION_KEY).BanMatchesRemaining);
        }

        // ── ERR-030-038: an empty tally is FR-DC-017's clean state, not evidence of a loss ──

        /// <summary>
        /// A loop resumed with <paramref name="tally"/> wired — the composition every one of the two
        /// locks below saves from, and the one ERR-030-036's guard was written to protect rather than
        /// to refuse.
        /// </summary>
        private static SeasonLoop LoopDriving(WorldStore world, DisciplineState tally) =>
            SeasonLoop.Restore(
                world, SeasonStateCodec.Encode(MidSeasonState()), RoundResolutionMode.QuickSimAll,
                careerOrNull: null, careerSquadsOrNull: null, progressionOrNull: null,
                disciplineOrNull: tally);

        [Test]
        public void AWiredTallyThatDrainsToZero_SavesOverItsOwnPopulatedFile()
        {
            // ERR-030-038, the mid-season half. ERR-030-036's guard keyed on `discipline.Count == 0` and
            // read that as proof the tally had been DROPPED. That inference holds for the two sibling
            // families — a career roster never legitimately empties — and is false here: FR-DC-017 drops
            // a row the instant it reaches (0, 0), which for a player serving his last match of a ban
            // with no residual yellows happens the moment his club plays. So a correctly wired,
            // correctly resumed loop became permanently unable to overwrite its own file, and stayed
            // that way until fresh cards accrued, because the destination keeps the old rows forever —
            // while the thrown message told the operator to resume with SeasonSaveContents.Discipline,
            // which is exactly what they had done.
            WorldStore world = PopulatedStore();
            string path = TempPath("drained-discipline.season");

            // One outstanding match of ban, no residual yellows: FR-DC-017's mid-season drop case.
            DisciplineState tally = DisciplineState.FromEntries(new[]
            {
                new DisciplineEntry(
                    100, DisciplineConstants.LEAGUE_COMPETITION_KEY, yellows: 0,
                    banMatchesRemaining: 1),
            });
            SeasonLoop loop = LoopDriving(world, tally);

            SeasonSaveManager.Save(loop, matchOrNull: null, path);
            Assert.AreEqual(1, SeasonSaveManager.Load(path).Discipline.Count,
                "Precondition: the destination now carries the row the second save must be allowed to "
                + "clear.");

            // The ban is served by the club playing (FR-DC-011), and #27's club-scoped id formula puts
            // player 100 in club 4. The row reaches (0, 0) and is dropped by FR-DC-017 — the tally is
            // still wired, still the loop's own, and now empty.
            new DisciplineRules(tally).OnClubFixturePlayed(
                100 / PlayerDatabaseConstants.CLUB_SQUAD_SIZE, Array.Empty<int>());
            Assert.AreEqual(0, tally.Count,
                "Precondition: serving the last match of a ban with no residual yellows drops the row "
                + "immediately (FR-DC-017).");
            Assert.AreSame(tally, loop.Discipline,
                "Precondition: the loop still drives this tally — nothing about it became unwired.");

            Assert.DoesNotThrow(
                () => SeasonSaveManager.Save(loop, matchOrNull: null, path),
                "a wired tally that drained to zero must save over its own populated file — refusing it "
                + "makes a career's own save permanently unwriteable the first time its last "
                + "suspension is served out.");

            Assert.AreEqual(0, SeasonSaveManager.Load(path).Discipline.Count,
                "and the drained tally must actually reach the file: the row is served out, so keeping "
                + "the destination's copy would resurrect a ban the player has already sat.");
        }

        [Test]
        public void ASeasonRollThatClearsEveryYellow_LeavesTheCareerSaveable()
        {
            // ERR-030-038, the boundary half — the same defect on FR-DC-017's other drop path, and the
            // one that would hit every save of a clean season's first day. The boundary sweep resets
            // every yellow count to 0 and carries unserved bans; a tally holding only yellows therefore
            // empties completely, and under the Count == 0 keying the next save of that career was
            // refused outright.
            WorldStore world = PopulatedStore();
            string path = TempPath("rolled-discipline.season");

            DisciplineState tally = DisciplineState.FromEntries(new[]
            {
                new DisciplineEntry(
                    100, DisciplineConstants.LEAGUE_COMPETITION_KEY, yellows: 2,
                    banMatchesRemaining: 0),
            });
            SeasonLoop loop = LoopDriving(world, tally);

            SeasonSaveManager.Save(loop, matchOrNull: null, path);
            Assert.AreEqual(1, SeasonSaveManager.Load(path).Discipline.Count,
                "Precondition: the destination carries the yellows-only row.");

            new DisciplineRules(tally).RollToNextSeason();
            Assert.AreEqual(0, tally.Count,
                "Precondition: the boundary sweep zeroes the yellows and FR-DC-017 drops the resulting "
                + "(0, 0) row.");

            Assert.DoesNotThrow(
                () => SeasonSaveManager.Save(loop, matchOrNull: null, path),
                "a career whose every yellow was cleared at the season boundary must stay saveable — "
                + "this is the state a new season STARTS in, so refusing it breaks the ordinary save.");

            Assert.AreEqual(0, SeasonSaveManager.Load(path).Discipline.Count,
                "and the swept tally must reach the file, or next season resumes carrying last "
                + "season's yellows.");
        }

        [Test]
        public void SaveLoad_TransposedTrainingAndMedicalBlocks_FailLoud()
        {
            // The defect this landing's AR pass caught (ERR-029-005 / ERR-041-009). The #29 and #41
            // blocks have the SAME byte shape and both sat at format version 1, so each codec decoded
            // the other's bytes cleanly and silently: an injury tier arrived as a training focus, a
            // recovery counter as a conditioning cursor, with no gate tripped anywhere in the file.
            // TrainingBlock / MedicalBlock now make the transposition a build error at Encode, so this
            // test has to fake it by putting the wrong BYTES in a correctly-typed wrapper — which is
            // what a hand-edited or third-party-written file would look like. The leading magic is the
            // only thing standing between that file and a silently corrupted squad.
            byte[] trainingBytes = TrainingSaveCodec.Encode(new[]
            {
                new ClubTrainingStates(
                    7,
                    new[] { 100 },
                    new[]
                    {
                        new TrainingState
                        {
                            Focus = TrainingFocus.Fitness,
                            Condition = 6543,
                            TrainingFatigue = 1234,
                            LastAdvancedWorldDay = 19,
                        },
                    }),
            });

            byte[] medicalBytes = MedicalSaveCodec.Encode(new[]
            {
                new ClubInjuryStates(
                    7,
                    new[] { 100 },
                    new[]
                    {
                        new InjuryState
                        {
                            Severity = InjurySeverity.Moderate,
                            RecoveryRemaining = 21,
                            InjuryCount = 4,
                            LastAdvancedWorldDay = 19,
                        },
                    }),
            });

            Assert.AreEqual(trainingBytes.Length, medicalBytes.Length,
                "the two blocks really are the same byte shape — that is why the magic is load-bearing");

            var progressionBlock = new ProgressionBlock(
                ProgressionSaveCodec.Encode(Array.Empty<ClubCareerStates>(), 0));
            byte[] transposed = SeasonSaveCodec.Encode(
                PopulatedStore().Snapshot(),
                SeasonStateCodec.Encode(MidSeasonState()),
                new TrainingBlock(medicalBytes),   // <- the medical bytes, in the training slot
                new MedicalBlock(trainingBytes),
                AppearanceStub,
                in progressionBlock,
                discipline: EmptyDisciplineBlock,
                matchBlobOrNull: null);

            SeasonSaveBlobs blobs = SeasonSaveCodec.Decode(transposed);

            Assert.Throws<InvalidOperationException>(
                () => TrainingSaveCodec.Decode(blobs.TrainingBlob),
                "a medical block in the training slot must be refused by its magic, not decoded as " +
                "six players' training focuses");
            Assert.Throws<InvalidOperationException>(
                () => MedicalSaveCodec.Decode(blobs.MedicalBlob),
                "and the same in the other direction");
        }

        // ── SeasonSaveCodec (in-memory) ─────────────────────────────────────────────

        // The frame treats every sub-blob as opaque, so these codec tests use short distinguishable
        // stand-ins rather than real #29/#41 blocks — the point is the framing, not the payloads.
        private static readonly byte[] TrainingStubBytes = { 0xA1, 0xA2 };

        private static readonly byte[] MedicalStubBytes = { 0xB1, 0xB2, 0xB3 };

        private static readonly byte[] AppearanceStubBytes = { 0xC1, 0xC2, 0xC3, 0xC4 };

        private static readonly byte[] DisciplineStubBytes = { 0xD1, 0xD2, 0xD3, 0xD4, 0xD5 };

        private static TrainingBlock TrainingStub => new TrainingBlock(TrainingStubBytes);

        private static MedicalBlock MedicalStub => new MedicalBlock(MedicalStubBytes);

        private static AppearanceBlock AppearanceStub => new AppearanceBlock(AppearanceStubBytes);

        private static DisciplineBlock DisciplineStub => new DisciplineBlock(DisciplineStubBytes);

        [Test]
        public void Codec_RoundTrips_WithMatch()
        {
            byte[] worldBlob = new byte[] { 1, 2, 3, 4, 5 };
            byte[] seasonBlob = new byte[] { 6, 6 };
            byte[] matchBlob = new byte[] { 9, 8, 7 };
            var progressionBlock = new ProgressionBlock(
                ProgressionSaveCodec.Encode(Array.Empty<ClubCareerStates>(), 0));
            SeasonSaveBlobs got = SeasonSaveCodec.Decode(
                SeasonSaveCodec.Encode(
                    worldBlob, seasonBlob, TrainingStub, MedicalStub, AppearanceStub, progressionBlock, DisciplineStub,
                    matchBlob));
            CollectionAssert.AreEqual(worldBlob, got.WorldBlob);
            CollectionAssert.AreEqual(seasonBlob, got.SeasonBlob);
            CollectionAssert.AreEqual(TrainingStubBytes, got.TrainingBlob);
            CollectionAssert.AreEqual(MedicalStubBytes, got.MedicalBlob);
            CollectionAssert.AreEqual(AppearanceStubBytes, got.AppearanceBlob);
            CollectionAssert.AreEqual(DisciplineStubBytes, got.DisciplineBlob);
            CollectionAssert.AreEqual(matchBlob, got.MatchBlob);
        }

        [Test]
        public void Codec_RoundTrips_NoMatch()
        {
            byte[] worldBlob = new byte[] { 42, 42, 42 };
            byte[] seasonBlob = new byte[] { 7 };
            var progressionBlock = new ProgressionBlock(
                ProgressionSaveCodec.Encode(Array.Empty<ClubCareerStates>(), 0));
            SeasonSaveBlobs got = SeasonSaveCodec.Decode(
                SeasonSaveCodec.Encode(
                    worldBlob, seasonBlob, TrainingStub, MedicalStub, AppearanceStub, progressionBlock, DisciplineStub,
                    matchBlobOrNull: null));
            CollectionAssert.AreEqual(worldBlob, got.WorldBlob);
            CollectionAssert.AreEqual(seasonBlob, got.SeasonBlob);
            CollectionAssert.AreEqual(TrainingStubBytes, got.TrainingBlob);
            CollectionAssert.AreEqual(MedicalStubBytes, got.MedicalBlob);
            CollectionAssert.AreEqual(AppearanceStubBytes, got.AppearanceBlob);
            CollectionAssert.AreEqual(DisciplineStubBytes, got.DisciplineBlob);
            Assert.IsNull(got.MatchBlob, "A null match blob must round-trip to a null MatchBlob (KD-3).");
        }

        [Test]
        public void Load_WorldPastTheNextFixtureDay_FailsLoud()
        {
            // FR-SN-011 (MUST) / F4: the KD-4 cursor invariant is one of the two coherence rules
            // spanning the world and season blobs, so neither codec can check it — only this root.
            // Save now refuses this content too (AR pass 6 M1 — the gate's own doc had stated the
            // never-write-what-Load-refuses rule while this very test recorded the asymmetry as
            // intended), so the incoherent file is crafted through SeasonSaveCodec.Encode, exactly
            // as the cursor-vs-clock Load test does.
            WorldStore world = PopulatedStore();
            for (int i = 0; i < 20; i++)
            {
                world.AdvanceDay();
            }

            Assert.Throws<InvalidOperationException>(
                () => SeasonSaveManager.Save(
                    world, MidSeasonState(), matchOrNull: null, TempPath("cursor-behind.season"),
                    NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true),
                "Save must refuse a world already past the season's next fixture day — Load refuses "
                + "this file, and this root never writes what its own Load refuses.");

            var emptyTraining = new TrainingBlock(TrainingSaveCodec.Encode(NoTraining));
            var emptyMedical = new MedicalBlock(MedicalSaveCodec.Encode(NoMedical));
            var emptyAppearance = new AppearanceBlock(AppearanceSaveCodec.Encode(NoAppearance));
            var emptyProgression = new ProgressionBlock(
                ProgressionSaveCodec.Encode(Array.Empty<ClubCareerStates>(), 0));
            byte[] frame = SeasonSaveCodec.Encode(
                world.Snapshot(), SeasonStateCodec.Encode(MidSeasonState()),
                in emptyTraining, in emptyMedical, in emptyAppearance, in emptyProgression, EmptyDisciplineBlock, null);
            string path = TempPath("cursor-behind.season");
            File.WriteAllBytes(path, frame);

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

            SeasonSaveManager.Save(world, done, matchOrNull: null, path, NoTraining, NoMedical, NoAppearance, ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true);

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
            byte[] trainingBlob = { 4, 4, 4, 4 };      // 4 bytes
            byte[] medicalBlob = { 5, 5, 5, 5, 5, 5 }; // 6 bytes
            byte[] appearanceBlob = { 6, 6, 6, 6, 6, 6, 6 }; // 7 bytes
            byte[] disciplineBlob = { 8, 8, 8, 8, 8, 8, 8, 8, 8 }; // 9 bytes
            byte[] matchBlob = { 3, 3, 3 };            // 3 bytes
            var progressionBlock = new ProgressionBlock(
                ProgressionSaveCodec.Encode(Array.Empty<ClubCareerStates>(), 0));
            var disciplineBlock = new DisciplineBlock(disciplineBlob);
            byte[] blob = SeasonSaveCodec.Encode(
                worldBlob, seasonBlob, new TrainingBlock(trainingBlob), new MedicalBlock(medicalBlob),
                new AppearanceBlock(appearanceBlob), progressionBlock, disciplineBlock, matchBlob);

            int o = 0;
            Assert.AreEqual(SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION,
                CanonicalSerializer.ReadU32(blob, ref o), "frame field 1: format version");
            Assert.AreEqual(1, blob[o], "frame field 2: matchPresent flag");
            o += 1;
            Assert.AreEqual((uint)worldBlob.Length, CanonicalSerializer.ReadU32(blob, ref o),
                "frame field 3: the WORLD block's length prefix comes first");
            o += worldBlob.Length;
            Assert.AreEqual((uint)seasonBlob.Length, CanonicalSerializer.ReadU32(blob, ref o),
                "frame field 4: the SEASON block follows the world block (FR-SN-019)");
            o += seasonBlob.Length;
            Assert.AreEqual((uint)trainingBlob.Length, CanonicalSerializer.ReadU32(blob, ref o),
                "frame field 5: the #29 TRAINING block follows the season block (FR-TR-018)");
            o += trainingBlob.Length;
            Assert.AreEqual((uint)medicalBlob.Length, CanonicalSerializer.ReadU32(blob, ref o),
                "frame field 6: the #41 MEDICAL block follows the training block (FR-MD-017)");
            o += medicalBlob.Length;
            Assert.AreEqual((uint)appearanceBlob.Length, CanonicalSerializer.ReadU32(blob, ref o),
                "frame field 7: the #30 APPEARANCE block follows the medical block (ERR-041-010(b))");
            o += appearanceBlob.Length;
            Assert.AreEqual((uint)progressionBlock.Bytes.Length, CanonicalSerializer.ReadU32(blob, ref o),
                "frame field 8: the #28 PROGRESSION block follows the appearance block (FR-PG-017) — " +
                "mandatory, so it sits ahead of the optional match block");
            o += progressionBlock.Bytes.Length;
            Assert.AreEqual((uint)disciplineBlob.Length, CanonicalSerializer.ReadU32(blob, ref o),
                "frame field 9: the #44 DISCIPLINE block follows the progression block (roadmap C1) — " +
                "mandatory, so it sits ahead of the optional match block");
            o += disciplineBlob.Length;
            Assert.AreEqual((uint)matchBlob.Length, CanonicalSerializer.ReadU32(blob, ref o),
                "frame field 10: the MATCH block is last — it is the only optional one, so it stays at " +
                "the end where a presence flag can govern it");
            Assert.AreEqual(blob.Length, o + matchBlob.Length, "no trailing bytes");
        }

        [Test]
        public void Codec_EmptyWorldBlob_RoundTrips()
        {
            var progressionBlock = new ProgressionBlock(
                ProgressionSaveCodec.Encode(Array.Empty<ClubCareerStates>(), 0));
            SeasonSaveBlobs got = SeasonSaveCodec.Decode(
                SeasonSaveCodec.Encode(
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    new TrainingBlock(Array.Empty<byte>()),
                    new MedicalBlock(Array.Empty<byte>()),
                    new AppearanceBlock(Array.Empty<byte>()),
                    progressionBlock, EmptyDisciplineBlock,
                    matchBlobOrNull: null));
            Assert.AreEqual(0, got.WorldBlob.Length);
            Assert.AreEqual(0, got.SeasonBlob.Length);
            Assert.AreEqual(0, got.TrainingBlob.Length);
            Assert.AreEqual(0, got.MedicalBlob.Length);
            Assert.AreEqual(0, got.AppearanceBlob.Length);
            Assert.IsNull(got.MatchBlob);
        }

        [Test]
        public void Codec_NullWorldBlob_Throws()
        {
            var progressionBlock = new ProgressionBlock(
                ProgressionSaveCodec.Encode(Array.Empty<ClubCareerStates>(), 0));
            Assert.Throws<ArgumentNullException>(
                () => SeasonSaveCodec.Encode(
                    worldBlob: null,
                    seasonBlob: new byte[] { 1 },
                    training: TrainingStub,
                    medical: MedicalStub,
                    appearance: AppearanceStub,
                    progression: progressionBlock, discipline: EmptyDisciplineBlock,
                    matchBlobOrNull: new byte[] { 1 }));
        }

        [Test]
        public void Codec_NullTrainingOrMedicalBlob_Throws()
        {
            // Unlike the match, these two blocks are MANDATORY: "no training state" is an empty block,
            // not an absent one. A null here is a caller bug (the manager encodes the empty set), and
            // accepting it would need a presence flag for a case that cannot arise.
            Assert.Throws<ArgumentNullException>(() => new TrainingBlock(null),
                "A null training block must fail loud at the wrapper (FR-TR-018).");
            Assert.Throws<ArgumentNullException>(() => new MedicalBlock(null),
                "A null medical block must fail loud at the wrapper (FR-MD-017).");
        }

        [Test]
        public void Codec_DefaultTrainingOrMedicalBlock_Throws()
        {
            // `default(TrainingBlock)` skips the wrapper's constructor and reads Bytes == null, the
            // same hole `default(ClubTrainingStates)` opens one layer down. Encode has to close it, or
            // the type-safety the wrapper buys evaporates for anyone who writes `default`.
            var progressionBlock = new ProgressionBlock(
                ProgressionSaveCodec.Encode(Array.Empty<ClubCareerStates>(), 0));
            Assert.Throws<ArgumentNullException>(
                () => SeasonSaveCodec.Encode(
                    worldBlob: new byte[] { 1 },
                    seasonBlob: new byte[] { 1 },
                    training: default,
                    medical: MedicalStub,
                    appearance: AppearanceStub,
                    progression: progressionBlock, discipline: EmptyDisciplineBlock,
                    matchBlobOrNull: null),
                "An unbound default(TrainingBlock) must fail loud, not encode as an empty block.");

            Assert.Throws<ArgumentNullException>(
                () => SeasonSaveCodec.Encode(
                    worldBlob: new byte[] { 1 },
                    seasonBlob: new byte[] { 1 },
                    training: TrainingStub,
                    medical: default,
                    appearance: AppearanceStub,
                    progression: progressionBlock, discipline: EmptyDisciplineBlock,
                    matchBlobOrNull: null),
                "An unbound default(MedicalBlock) must fail loud, not encode as an empty block.");
        }

        [Test]
        public void Codec_NullBlob_Decode_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SeasonSaveCodec.Decode(null));
        }

        [Test]
        public void Codec_WrongFormatVersion_FailsLoud()
        {
            var progressionBlock = new ProgressionBlock(
                ProgressionSaveCodec.Encode(Array.Empty<ClubCareerStates>(), 0));
            byte[] blob = SeasonSaveCodec.Encode(
                new byte[] { 1, 2 }, new byte[] { 3 }, TrainingStub, MedicalStub, AppearanceStub,
                progressionBlock, EmptyDisciplineBlock, matchBlobOrNull: null);
            blob[0] ^= 0xFF; // corrupt the leading format-version u32
            Assert.Throws<InvalidOperationException>(() => SeasonSaveCodec.Decode(blob),
                "A season format-version mismatch must fail loud (KD-4).");
        }

        [Test]
        public void Codec_PreT1FrameVersions_FailLoud()
        {
            // FR-SN-020: the frame bumped 1 -> 2 when the season sub-blob landed, 2 -> 3 when the
            // #29/#41 blocks did, 3 -> 4 when the #30 appearance block did (ERR-041-010(b)), 4 -> 5
            // when the #28 progression block did (FR-PG-017), and 5 -> 6 when the #44 discipline block
            // did (roadmap C1). Each
            // older layout, read as the current one, would deframe some later block as an earlier one —
            // a v3 file's match blob would be deframed as an appearance block. Refused outright: there
            // is no cross-version migration at Stage 0 (KD-4).
            var progressionBlock = new ProgressionBlock(
                ProgressionSaveCodec.Encode(Array.Empty<ClubCareerStates>(), 0));
            foreach (uint stale in new uint[] { 1u, 2u, 3u, 4u, 5u })
            {
                byte[] blob = SeasonSaveCodec.Encode(
                    new byte[] { 1, 2 }, new byte[] { 3 }, TrainingStub, MedicalStub, AppearanceStub,
                    progressionBlock, EmptyDisciplineBlock, matchBlobOrNull: null);
                int o = 0;
                CanonicalSerializer.WriteU32(blob, ref o, stale);
                Assert.Throws<InvalidOperationException>(() => SeasonSaveCodec.Decode(blob),
                    "A pre-current (v" + stale + ") season frame must be rejected, not reinterpreted.");
            }
        }

        [Test]
        public void Codec_BadMatchPresentFlag_FailsLoud()
        {
            var progressionBlock = new ProgressionBlock(
                ProgressionSaveCodec.Encode(Array.Empty<ClubCareerStates>(), 0));
            byte[] blob = SeasonSaveCodec.Encode(
                new byte[] { 1, 2 }, new byte[] { 3 }, TrainingStub, MedicalStub, AppearanceStub,
                progressionBlock, EmptyDisciplineBlock, matchBlobOrNull: null);
            blob[4] = 2; // the matchPresent flag sits right after the u32 version
            Assert.Throws<InvalidOperationException>(() => SeasonSaveCodec.Decode(blob),
                "A matchPresent flag other than 0/1 must fail loud (KD-8).");
        }

        [Test]
        public void Codec_OversizeWorldLength_FailsLoud()
        {
            var progressionBlock = new ProgressionBlock(
                ProgressionSaveCodec.Encode(Array.Empty<ClubCareerStates>(), 0));
            byte[] blob = SeasonSaveCodec.Encode(
                new byte[] { 1, 2, 3 }, new byte[] { 4 }, TrainingStub, MedicalStub, AppearanceStub,
                progressionBlock, EmptyDisciplineBlock, matchBlobOrNull: null);
            // The world length u32 sits at offset 5 (u32 version + u8 flag). Overwrite with a huge value.
            blob[5] = 0xFF; blob[6] = 0xFF; blob[7] = 0xFF; blob[8] = 0xFF;
            Assert.Throws<InvalidOperationException>(() => SeasonSaveCodec.Decode(blob),
                "A world length exceeding the blob must fail loud, not over-read (KD-8).");
        }

        [Test]
        public void Codec_TrailingBytes_FailsLoud()
        {
            var progressionBlock = new ProgressionBlock(
                ProgressionSaveCodec.Encode(Array.Empty<ClubCareerStates>(), 0));
            byte[] blob = SeasonSaveCodec.Encode(
                new byte[] { 1, 2 }, new byte[] { 9 }, TrainingStub, MedicalStub, AppearanceStub,
                progressionBlock, EmptyDisciplineBlock, matchBlobOrNull: new byte[] { 3 });
            var padded = new byte[blob.Length + 1];
            Array.Copy(blob, padded, blob.Length);
            Assert.Throws<InvalidOperationException>(() => SeasonSaveCodec.Decode(padded),
                "Trailing bytes after the declared content must fail loud (KD-8 / R1).");
        }

        [Test]
        public void Codec_TruncatedMatchBlock_FailsLoud()
        {
            var progressionBlock = new ProgressionBlock(
                ProgressionSaveCodec.Encode(Array.Empty<ClubCareerStates>(), 0));
            byte[] blob = SeasonSaveCodec.Encode(
                new byte[] { 1, 2 }, new byte[] { 9 }, TrainingStub, MedicalStub, AppearanceStub,
                progressionBlock, EmptyDisciplineBlock, matchBlobOrNull: new byte[] { 3, 4, 5 });
            var chopped = new byte[blob.Length - 2];
            Array.Copy(blob, chopped, chopped.Length);
            Assert.Throws<InvalidOperationException>(() => SeasonSaveCodec.Decode(chopped),
                "A truncated match block must fail loud (KD-8 bound guard).");
        }

        [Test]
        public void Codec_TruncatedTrainingBlock_FailsLoud()
        {
            // The bound guard has to hold at the two NEW block boundaries too, not just the old ones:
            // an oversize training length must be refused rather than swallowing the medical block.
            var progressionBlock = new ProgressionBlock(
                ProgressionSaveCodec.Encode(Array.Empty<ClubCareerStates>(), 0));
            byte[] blob = SeasonSaveCodec.Encode(
                new byte[] { 1, 2 }, new byte[] { 9 }, TrainingStub, MedicalStub, AppearanceStub,
                progressionBlock, EmptyDisciplineBlock, matchBlobOrNull: null);

            // version(4) + flag(1) + worldLen(4) + world(2) + seasonLen(4) + season(1) = 16
            const int TrainingLengthOffset = 4 + 1 + 4 + 2 + 4 + 1;
            blob[TrainingLengthOffset] = 0xFF;
            blob[TrainingLengthOffset + 1] = 0xFF;
            blob[TrainingLengthOffset + 2] = 0xFF;
            blob[TrainingLengthOffset + 3] = 0xFF;

            Assert.Throws<InvalidOperationException>(() => SeasonSaveCodec.Decode(blob),
                "A training length exceeding the blob must fail loud, not over-read.");
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

        // ── Fixtures for the #28 roster-source mutation-audit lock ─────────────────

        private const int GrowthBandPlayerAge = 20; // < GROWTH_AGE (24) — CreateDefault's Age (25) is Stable

        /// <summary>
        /// A distinct squad of <see cref="RequiredCount"/> Growth-band (age &lt; GROWTH_AGE) players,
        /// so a #28 daily step banks measurable attribute growth. Mirrors <see cref="DistinctSquad"/>
        /// except for the age override, and keeps every attribute below ATTRIBUTE_MAX so a spent point
        /// always has somewhere to land.
        /// </summary>
        private static Squad GrowableSquad(int clubId)
        {
            var players = new PlayerRecord[RequiredCount];
            for (int k = 0; k < players.Length; k++)
            {
                PlayerRecord p = PlayerRecord.CreateDefault(clubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + k);
                p.Position = PosFor(k);
                p.Age = GrowthBandPlayerAge;

                int[] a = new int[31];
                for (int f = 0; f < a.Length; f++)
                {
                    a[f] = 1 + ((k * 7 + f * 3 + clubId) % 18);
                }
                var attrs = new PlayerAttributes();
                attrs.FromArray(a);
                attrs.WeakFootRating = 1 + ((k + clubId) % 5);
                p.Attributes = attrs;

                players[k] = p;
            }
            return new Squad(clubId, players);
        }

        /// <summary>True if any of the two squads' first <see cref="RequiredCount"/> players' attribute arrays differ.</summary>
        private static bool SquadDiverges(Squad before, Squad after)
        {
            for (int k = 0; k < RequiredCount; k++)
            {
                int[] a = before.GetPlayer(k).Attributes.ToArray();
                int[] b = after.GetPlayer(k).Attributes.ToArray();
                for (int f = 0; f < a.Length; f++)
                {
                    if (a[f] != b[f])
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // The four-way career-block coherence gate (SeasonSaveManager.RequireCoherentCareerBlocks)
        // requires the training / medical / appearance sets to describe EXACTLY the same clubs and
        // players as a non-empty progression store, so these build the matching (otherwise inert)
        // siblings straight from the progression's own blocks — the single-player TrainingMatching /
        // MedicalMatching / AppearanceMatching helpers above only cover a one-club-one-player fixture.

        private static ClubTrainingStates[] TrainingBlocksMatching(ProgressionEngine progression)
        {
            ClubCareerStates[] blocks = progression.ToBlocks();
            var result = new ClubTrainingStates[blocks.Length];
            for (int c = 0; c < blocks.Length; c++)
            {
                var ids = new int[blocks[c].Count];
                var states = new TrainingState[blocks[c].Count];
                for (int p = 0; p < blocks[c].Count; p++)
                {
                    ids[p] = blocks[c].Records[p].PlayerId;
                    states[p] = TrainingState.Create(TrainingFocus.Balanced);
                }
                result[c] = new ClubTrainingStates(blocks[c].ClubId, ids, states);
            }
            return result;
        }

        private static ClubInjuryStates[] MedicalBlocksMatching(ProgressionEngine progression)
        {
            ClubCareerStates[] blocks = progression.ToBlocks();
            var result = new ClubInjuryStates[blocks.Length];
            for (int c = 0; c < blocks.Length; c++)
            {
                var ids = new int[blocks[c].Count];
                var states = new InjuryState[blocks[c].Count];
                for (int p = 0; p < blocks[c].Count; p++)
                {
                    ids[p] = blocks[c].Records[p].PlayerId;
                    states[p] = InjuryState.Create();
                }
                result[c] = new ClubInjuryStates(blocks[c].ClubId, ids, states);
            }
            return result;
        }

        private static ClubAppearanceStates[] AppearanceBlocksMatching(ProgressionEngine progression)
        {
            ClubCareerStates[] blocks = progression.ToBlocks();
            var result = new ClubAppearanceStates[blocks.Length];
            for (int c = 0; c < blocks.Length; c++)
            {
                var ids = new int[blocks[c].Count];
                for (int p = 0; p < blocks[c].Count; p++)
                {
                    ids[p] = blocks[c].Records[p].PlayerId;
                }
                result[c] = new ClubAppearanceStates(blocks[c].ClubId, ids, new AppearanceState[blocks[c].Count]);
            }
            return result;
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
// | 1.5     | 2026-08-06 | —      | #29/#41 T1: the frame-codec cases carry training + medical    |
// |         |            |        | blocks and the order lock covers all five; + the two new      |
// |         |            |        | mandatory-block guards, the training bound guard, the v2      |
// |         |            |        | frame rejection, and the two whole-file round-trips (state    |
// |         |            |        | field-identical; unwired ⇒ empty sets, never null).           |
// | 1.6     | 2026-08-07 | —      | Balance pass D2: frame v4 — AppearanceStub joins every codec case, |
// |         |            |        | the frame-order lock gains field 7 (APPR), the stale-version loop  |
// |         |            |        | adds v3, Save call sites carry the required appearance set.        |
// | 1.7     | 2026-08-08 | —      | Balance-pass AR pass 3 (L3/L7): the null-appearance refusal joins  |
// |         |            |        | its two siblings; the empty-sets lock asserts AppearanceClubs too; |
// |         |            |        | + Save_IncoherentCareerBlockTriple_FailsLoud — the round-trip      |
// |         |            |        | test itself had been writing a 1-training/1-medical/0-appearance   |
// |         |            |        | file FromBlocks refuses, now coherent.                             |
// | 1.8     | 2026-08-08 | —      | Balance-pass AR pass 4 (M1 + L1 + L2): Save_CrossClubDuplicate-    |
// |         |            |        | PlayerId_FailsLoud (the gate's missing ERR-041-019 predicate);     |
// |         |            |        | Save_DefaultCareerBlock refused by name, not NRE; every coherence  |
// |         |            |        | refusal branch locked + the permuted-club-order PASS case.         |
// | 1.9     | 2026-08-08 | —      | Balance-pass AR pass 5 (M3 + M4): the default-block refusal    |
// |         |            |        | lock rebuilt to kill its mutant (empty siblings so the walk    |
// |         |            |        | reaches the clone; message + paramName pinned — the reverted   |
// |         |            |        | code threw the same TYPE from the count branch); + the         |
// |         |            |        | future-dated-cursor refusals at Save (all three cursor kinds)  |
// |         |            |        | and at Load (via a hand-crafted frame — what "hand-edited"     |
// |         |            |        | means).                                                        |
// | 1.10    | 2026-08-08 | —      | Balance-pass AR pass 6 (M1 + M2): Load_WorldPastTheNextFixture |
// |         |            |        | Day rebuilt through SeasonSaveCodec.Encode (Save now refuses   |
// |         |            |        | the content — and the test asserts THAT first); + the lagging- |
// |         |            |        | cursor refusal with the lag-of-exactly-one PASS case (the     |
// |         |            |        | pre-increment convention's normal saved state).               |
// | 1.11    | 2026-08-08 | —      | Balance-pass AR pass 7 (L4): the duplicate-ClubId refusal gains   |
// |         |            |        | its message+paramName lock — without it the unstable sorts pair  |
// |         |            |        | duplicates arbitrarily and the gate names a phantom mismatch.    |
// | 1.12    | 2026-08-08 | —      | Balance-pass AR pass 9 (M1): + the medical-only-lag case in      |
// |         |            |        | Save_CursorLaggingTheClockByTwoOrMore_FailsLoud (training at the |
// |         |            |        | legitimate lag of 1, medical at gap 2) — the shared medical-lag  |
// |         |            |        | predicate now has an isolating case at the file boundary too.   |
// | 1.13    | 2026-08-08 | —      | #28 T1: call sites updated for SeasonSaveManager.Save's / Season-|
// |         |            |        | SaveCodec.Encode's new required progression-block parameter. No |
// |         |            |        | assertion or intent change.                                      |
// | 1.14    | 2026-08-08 | —      | Locks for two fixes just applied. ERR-028-007 (the fourth        |
// |         |            |        | persisted cursor): the #28 twin of the existing #29/#41          |
// |         |            |        | future-dated / lagging-by-two-or-more / lagging-by-one Save      |
// |         |            |        | cases, plus a Load case crafted through SeasonSaveCodec.Encode   |
// |         |            |        | directly so it isolates the Load-side check from the Save-side   |
// |         |            |        | one Save now refuses to write. ERR-028-008 (refuse to overwrite  |
// |         |            |        | a roster with an empty one): populated-then-empty-refused with   |
// |         |            |        | the original file proven intact, empty-to-new-path accepted,     |
// |         |            |        | empty-over-already-empty accepted. + PBlock/ProgressionFor       |
// |         |            |        | fixtures mirroring TBlock/MBlock/ABlock.                         |
// | 1.15    | 2026-08-11 | —      | AR pass 6, M2(b): Save_FutureDatedBirthWorldDay_FailsLoud and    |
// |         |            |        | Load_FutureDatedBirthWorldDay_FailsLoud — the Save/Load twins of |
// |         |            |        | SeasonLoopProgressionTests' composition-boundary lock, checked   |
// |         |            |        | on the same terms as the ERR-028-007 progression-cursor cases    |
// |         |            |        | above. + PBlockWithBirthDay fixture (PBlock's twin with a        |
// |         |            |        | caller-supplied BirthWorldDay). Mutation-verified: reverting     |
// |         |            |        | either RequireBirthWorldDayWithinClock call in SeasonSaveManager |
// |         |            |        | fails its own test and no other.                                 |
// | 1.16    | 2026-08-11 | —      | AR pass 8, L-4. PBlock/PBlockWithBirthDay set CurrentAbility =   |
// |         |            |        | PlayerProgressionConstants.PA_MIN (4000) against a               |
// |         |            |        | PlayerRecord.CreateDefault record — ProgressionSaveCodec.cs      |
// |         |            |        | 1.5's new CurrentAbility == ComputeCA(attributes) gate refused   |
// |         |            |        | every one of the (many) tests routing through these two          |
// |         |            |        | fixtures. Retuned to a computed PBlockDefaultCurrentAbility       |
// |         |            |        | field; PotentialAbility stays PA_MIN (its own, independent range |
// |         |            |        | check — unaffected). No test assertions changed.                 |
// | 1.17    | 2026-08-13 | —      | #44 T1 (roadmap C1): every SeasonSaveManager.Save / SeasonSave-  |
// |         |            |        | Codec.Encode call site updated for the new required discipline   |
// |         |            |        | parameter (EmptyDisciplineBlock / DisciplineStub helpers added,  |
// |         |            |        | mirroring TrainingStub/MedicalStub/AppearanceStub); Codec_Frame- |
// |         |            |        | BlocksSitInTheirPinnedOrder gains field 9 (DISC) and renumbers   |
// |         |            |        | the match block to field 10; Codec_RoundTrips_WithMatch / _No-   |
// |         |            |        | Match assert the discipline block round-trips too; Codec_PreT1-  |
// |         |            |        | FrameVersions_FailLoud's stale-version list gains v4 and v5. No  |
// |         |            |        | other assertion or intent change.                                 |
// | 1.18    | 2026-08-13 | —      | #44 C1/C2 adversarial review, H1 (ERR-030-036): six locks on the  |
// |         |            |        | resume-and-save cycle. Restore_CarriesTheDisciplineTallyIntoThe-  |
// |         |            |        | ResumedLoop (the forwarding half — fails if SeasonLoop.Restore's  |
// |         |            |        | new disciplineOrNull stops reaching the constructor); Save_An-    |
// |         |            |        | EmptyDisciplineTally_OverAPopulatedOne_IsRefused, with BOTH       |
// |         |            |        | sibling guards' subjects deliberately populated so only the new   |
// |         |            |        | one can fire; its two carve-outs (new path, already-empty         |
// |         |            |        | destination); and the two end-to-end resume cases — threading     |
// |         |            |        | SeasonSaveContents.Discipline keeps the ban and the yellows,      |
// |         |            |        | dropping it is refused at the write. + the TallyFor fixture.      |
// | 1.19    | 2026-08-13 | —      | #44 C1/C2 AR round 3, H3 (ERR-030-038): two locks on the case     |
// |         |            |        | v1.18's four missed by construction — a tally that DRAINS.        |
// |         |            |        | AWiredTallyThatDrainsToZero_SavesOverItsOwnPopulatedFile (a       |
// |         |            |        | served ban with no residual yellows, FR-DC-017's mid-season       |
// |         |            |        | drop) and ASeasonRollThatClearsEveryYellow_LeavesTheCareer-       |
// |         |            |        | Saveable (the boundary sweep) both save a WIRED loop over its     |
// |         |            |        | own populated file and assert the empty tally reaches disk.       |
// |         |            |        | Both fail if the guard is re-keyed on discipline.Count == 0.      |
// |         |            |        | The three v1.18 locks that exercise the guard now drive it        |
// |         |            |        | through the internal disciplineWired overload — renamed Save_An-  |
// |         |            |        | Unwired* — because the public long form cannot express "no        |
// |         |            |        | discipline wired" and its empty tally is no longer a refusal.     |
// |         |            |        | + the LoopDriving fixture.                                        |
// | 1.20    | 2026-08-13 | —      | #44 C1/C2 AR round 4, H4 (ERR-030-039). New lock:                  |
// |         |            |        | Save_ThePublicLongForm_DrivingNoDiscipline_CannotEmptyAPopulated- |
// |         |            |        | Tally — two PUBLIC long-form saves over one path, the second      |
// |         |            |        | driving no discipline, asserting the refusal and that the         |
// |         |            |        | destination's row survives. Before the fix that form hardcoded    |
// |         |            |        | disciplineWired: true, so the same call emptied the tally with    |
// |         |            |        | every gate green. Every long-form call site in this file now      |
// |         |            |        | passes disciplineWired explicitly (true where the previous        |
// |         |            |        | forwarding overload asserted it), and v1.19's claim that "the      |
// |         |            |        | public long form cannot express 'no discipline wired'" — the      |
// |         |            |        | false premise the defect rested on — is corrected in the comment  |
// |         |            |        | that carried it. Mutation-verified: reinstating the hardcode      |
// |         |            |        | (disciplineWired = true at the top of Save) fails the new lock.   |
#endregion
