// File:     src/season-save/tests/SeasonSaveTransfersTests.cs
// Created:  2026-10-18
// Modified: 2026-10-20 (M6 orphan-contract negatives at Load and composition; L2 stale comment — v1.1)
// Author:   —
// Spec:     Transfers, Contracts & Negotiation #31 §4.4 / §5.5 T-TX-DET-001 / §7.1 T1, FR-TX-012/014/027/028;
//           Season & Competition Loop #30 Appendix B (frame v8); ERR-031-003; Code Standards #20
// Purpose:  Locks #31 T1's composition into the season frame: the transfers block survives the file and
//           the documented resume path field-identically, sits in its own frame slot, is refused when it
//           describes another career, and cannot be silently deleted by an unwired save (the fifth
//           destination guard, keyed on wiring rather than on emptiness).

using System;
using System.IO;

using NUnit.Framework;

using TacticalDirector.ClubFinances;
using TacticalDirector.Discipline;
using TacticalDirector.InjuriesMedical;
using TacticalDirector.LivingWorld;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.PlayerProgression;
using TacticalDirector.TrainingSystem;
using TacticalDirector.Transfers;

namespace TacticalDirector.SeasonSave
{
    [TestFixture]
    public sealed class SeasonSaveTransfersTests
    {
        private const ulong WorldSeed = 0x31A1C0DEUL;
        private const int ManagerId = 1;
        private const int ClubCount = 4;
        private const int ManagedClubId = 2;
        private const int FirstManagedPlayerId = ManagedClubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE;

        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "td-transfers-t1-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch (Exception) { }
        }

        private string TempPath(string name) => Path.Combine(_tempDir, name);

        // ── round-trip ───────────────────────────────────────────────────────────────

        [Test]
        public void SaveLoad_ThroughTheLoop_RoundTripsNonDefaultTransfersState_AndResumesIt()
        {
            SeasonLoop loop = NewLoop(NonDefaultState(), out _);
            loop.AdvanceDays(2);                                     // mid-window (T-TX-DET-001)
            string path = TempPath("wired.season");

            SeasonSaveManager.Save(loop, matchOrNull: null, path);
            SeasonSaveContents contents = SeasonSaveManager.Load(path);

            AssertSameState(NonDefaultState(), contents.Transfers);

            // The documented resume path must be able to carry it (the ERR-030-036 lesson), and a
            // Save As from the resumed loop must write the identical block.
            SeasonLoop resumed = SeasonLoop.Restore(
                contents.World, SeasonStateCodec.Encode(contents.Season), RoundResolutionMode.QuickSimAll,
                progressionOrNull: contents.Progression, financesOrNull: contents.Finances,
                transfersOrNull: contents.Transfers);
            string copy = TempPath("resumed.season");
            SeasonSaveManager.Save(resumed, matchOrNull: null, copy);

            Assert.That(
                SeasonSaveCodec.Decode(File.ReadAllBytes(copy)).TransfersBlob,
                Is.EqualTo(TransfersSaveCodec.Encode(NonDefaultState())));
        }

        [Test]
        public void TheTransfersBlock_SitsInItsOwnFrameSlot()
        {
            SeasonLoop loop = NewLoop(NonDefaultState(), out _);
            string path = TempPath("slot.season");

            SeasonSaveManager.Save(loop, matchOrNull: null, path);
            SeasonSaveBlobs blobs = SeasonSaveCodec.Decode(File.ReadAllBytes(path));

            Assert.That(blobs.TransfersBlob, Is.EqualTo(TransfersSaveCodec.Encode(NonDefaultState())));
            Assert.That(blobs.FinanceBlob, Is.Not.EqualTo(blobs.TransfersBlob));
            Assert.Throws<InvalidOperationException>(
                () => TransfersSaveCodec.Decode(blobs.FinanceBlob),
                "the finance block in the transfers slot must be refused by the XFER magic");
        }

        [Test]
        public void AnUnwiredCareer_SavesAZeroClubBlock_AndLoadsNull()
        {
            SeasonLoop loop = NewLoop(null, out _);
            string path = TempPath("unwired.season");

            SeasonSaveManager.Save(loop, matchOrNull: null, path);

            Assert.That(SeasonSaveManager.Load(path).Transfers, Is.Null);
            Assert.That(
                SeasonSaveCodec.Decode(File.ReadAllBytes(path)).TransfersBlob,
                Is.EqualTo(TransfersSaveCodec.Encode(null)));
        }

        [Test]
        public void TheLoopClonesItsState_SoTheCallersReferenceIsNotASecondWriter()
        {
            TransfersState mine = NonDefaultState();
            SeasonLoop loop = NewLoop(mine, out _);
            mine.RemoveContract(FirstManagedPlayerId);              // after composition
            string path = TempPath("clone.season");

            SeasonSaveManager.Save(loop, matchOrNull: null, path);

            Assert.That(SeasonSaveManager.Load(path).Transfers.ContractCount, Is.EqualTo(3));
        }

        // ── the fifth destination guard ──────────────────────────────────────────────

        [Test]
        public void AnUnwiredSave_OverAFileCarryingTransfers_IsRefused()
        {
            string path = TempPath("guarded.season");
            SeasonSaveManager.Save(NewLoop(NonDefaultState(), out _), matchOrNull: null, path);
            byte[] before = File.ReadAllBytes(path);

            var e = Assert.Throws<InvalidOperationException>(
                () => SeasonSaveManager.Save(NewLoop(null, out _), matchOrNull: null, path));

            StringAssert.Contains("#31 transfers state", e.Message);
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(before), "a refused save must not touch the file");
        }

        [Test]
        public void AWiredCareerWithNoContractsLeft_SavesOverItsOwnFile()
        {
            // ERR-030-038's distinction by construction: every contract expired or sold is still WIRED.
            string path = TempPath("drained.season");
            SeasonSaveManager.Save(NewLoop(NonDefaultState(), out _), matchOrNull: null, path);
            var drained = new TransfersState(ManagedClubId, new TransferWindow(5, 50));

            Assert.DoesNotThrow(() => SeasonSaveManager.Save(NewLoop(drained, out _), matchOrNull: null, path));
            Assert.That(SeasonSaveManager.Load(path).Transfers.ContractCount, Is.EqualTo(0));
        }

        // ── cross-blob coherence (SeasonTransfersCoherence) ──────────────────────────

        [Test]
        public void Composition_WithAnotherClubsTransfersState_IsRefused()
        {
            var foreign = new TransfersState(ManagedClubId + 1, new TransferWindow(5, 50));

            var e = Assert.Throws<ArgumentException>(() => NewLoop(foreign, out _));

            Assert.That(e.ParamName, Is.EqualTo("transfersOrNull"));
            StringAssert.Contains("managed club", e.Message);
        }

        [Test]
        public void Save_AContractForAPlayerTheRosterDoesNotCarry_IsRefused()
        {
            // One-player squads, so a contract can name a managed-club id the roster does not carry — the
            // roster rule is then the only thing that can refuse it (TransfersState checks no membership since
            // ERR-031-005 — any non-negative id is insertable).
            Squad[] squads = new Squad[ClubCount];
            for (int c = 0; c < ClubCount; c++)
            {
                squads[c] = new Squad(c, new[] { PlayerRecord.CreateDefault(c * PlayerDatabaseConstants.CLUB_SQUAD_SIZE) });
            }

            var world = new WorldStore(ManagerId, WorldSeed);
            SeasonState season = SeasonState.CreateNew(
                ClubIdsOf(ClubCount), ManagedClubId, 7UL, new BoardObjective(2), 5u, 7u, 1);
            ProgressionEngine progression = ProgressionEngine.SeedFrom(squads, world.CurrentWorldTick);
            var state = new TransfersState(ManagedClubId, new TransferWindow(5, 50));
            state.InsertContract(new Contract(FirstManagedPlayerId + 1, 10, 2));

            var e = Assert.Throws<ArgumentException>(() => SeasonSaveManager.Save(
                world, season, null, TempPath("orphan.season"),
                Array.Empty<ClubTrainingStates>(), Array.Empty<ClubInjuryStates>(),
                Array.Empty<ClubAppearanceStates>(), progression, new DisciplineState(),
                disciplineWired: false, Array.Empty<ClubFinanceEntry>(), state));

            Assert.That(e.ParamName, Is.EqualTo("transfersOrNull"));
            StringAssert.Contains("does not carry", e.Message);
        }

        [Test]
        public void Load_AFrameWhoseTransfersBlockNamesAnotherManagedClub_IsRefused()
        {
            string path = TempPath("mispaired.season");
            SeasonSaveManager.Save(NewLoop(NonDefaultState(), out _), matchOrNull: null, path);
            SeasonSaveBlobs b = SeasonSaveCodec.Decode(File.ReadAllBytes(path));
            byte[] foreign = TransfersSaveCodec.Encode(new TransfersState(ManagedClubId + 1, new TransferWindow(5, 50)));
            File.WriteAllBytes(path, SeasonSaveCodec.Encode(
                b.WorldBlob, b.SeasonBlob, new TrainingBlock(b.TrainingBlob), new MedicalBlock(b.MedicalBlob),
                new AppearanceBlock(b.AppearanceBlob), new ProgressionBlock(b.ProgressionBlob),
                new DisciplineBlock(b.DisciplineBlob), new FinanceBlock(b.FinanceBlob),
                new TransfersBlock(foreign), b.MatchBlob));

            var e = Assert.Throws<ArgumentException>(() => SeasonSaveManager.Load(path));
            StringAssert.Contains("managed club", e.Message);
        }

        [Test]
        public void Load_AFrameWhoseTransfersBlockHoldsAnOrphanContract_IsRefused()
        {
            // M6: the file's PROG projection (populated) is the roster authority at Load. A contract for a
            // player id that projection's managed club does not carry — here another club's player, which
            // TransfersState itself would insert and the codec would round-trip — is refused.
            string path = TempPath("orphan-frame.season");
            SeasonSaveManager.Save(NewLoop(NonDefaultState(), out _), matchOrNull: null, path);
            SeasonSaveBlobs b = SeasonSaveCodec.Decode(File.ReadAllBytes(path));
            byte[] orphan = TransfersSaveCodec.Encode(OrphanContractState());
            File.WriteAllBytes(path, SeasonSaveCodec.Encode(
                b.WorldBlob, b.SeasonBlob, new TrainingBlock(b.TrainingBlob), new MedicalBlock(b.MedicalBlob),
                new AppearanceBlock(b.AppearanceBlob), new ProgressionBlock(b.ProgressionBlob),
                new DisciplineBlock(b.DisciplineBlob), new FinanceBlock(b.FinanceBlob),
                new TransfersBlock(orphan), b.MatchBlob));

            var e = Assert.Throws<ArgumentException>(() => SeasonSaveManager.Load(path));
            StringAssert.Contains("does not carry", e.Message);
        }

        [Test]
        public void Composition_WithAnOrphanContract_IsRefused()
        {
            // M6: composition checks against the loop's resolved provider (here the progression
            // projection), not the file — a state naming another club's player is refused before a loop
            // exists, with the caller's parameter name.
            var e = Assert.Throws<ArgumentException>(() => NewLoop(OrphanContractState(), out _));

            Assert.That(e.ParamName, Is.EqualTo("transfersOrNull"));
            StringAssert.Contains("does not carry", e.Message);
        }

        // ── F3 at the frame: D-10 / #50, saves are breakable — a v7 file is refused, not migrated ──

        [Test]
        public void Load_AVersion7Frame_IsRefusedNotMigrated()
        {
            string path = TempPath("v7.season");
            SeasonSaveManager.Save(NewLoop(null, out _), matchOrNull: null, path);
            byte[] bytes = File.ReadAllBytes(path);
            bytes[0] = 7;
            File.WriteAllBytes(path, bytes);

            var e = Assert.Throws<InvalidOperationException>(() => SeasonSaveManager.Load(path));
            StringAssert.Contains("format version 7", e.Message);
            Assert.That(SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION, Is.EqualTo(8u));
        }

        // ── helpers ──────────────────────────────────────────────────────────────────

        private static SeasonLoop NewLoop(TransfersState transfersOrNull, out League league)
        {
            league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(ManagerId, WorldSeed);
            SeasonState season = league.CreateSeason(managedClubId: ManagedClubId);
            var squads = new Squad[ClubCount];
            for (int c = 0; c < ClubCount; c++)
            {
                squads[c] = league.ResolveByClubId(c);
            }

            ProgressionEngine progression = ProgressionEngine.SeedFrom(squads, world.CurrentWorldTick);
            return new SeasonLoop(
                world, season, RoundResolutionMode.QuickSimAll,
                progressionOrNull: progression, transfersOrNull: transfersOrNull);
        }

        // Spend is only writable through SubmitBid (KD-2), so the non-default value is set the way a file
        // carries it: through the codec's own restore path.
        private static TransfersState NonDefaultState()
        {
            var state = new TransfersState(ManagedClubId, new TransferWindow(5, 50));
            state.InsertContract(new Contract(FirstManagedPlayerId, 1_500, 1));
            state.InsertContract(new Contract(FirstManagedPlayerId + 7, 0, 4));
            state.InsertContract(new Contract(FirstManagedPlayerId + 22, 99_999, 2));
            byte[] blob = TransfersSaveCodec.Encode(state);
            int spend = TransfersConstants.TRANSFERS_SAVE_HEADER_BYTES + 4;
            long committed = 123_456_789L;
            for (int i = 0; i < 8; i++)
            {
                blob[spend + i] = (byte)(committed >> (8 * i));
            }

            return TransfersSaveCodec.Decode(blob);
        }

        // A managed-club state whose only contract names a player of ANOTHER club (club 0's first player):
        // legal to build (no membership check in TransfersState, ERR-031-005) and to encode, wrong for this
        // career.
        private static TransfersState OrphanContractState()
        {
            var state = new TransfersState(ManagedClubId, new TransferWindow(5, 50));
            state.InsertContract(new Contract(0, 10, 2));
            return state;
        }

        private static void AssertSameState(TransfersState expected, TransfersState actual)
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.ManagedClubId, Is.EqualTo(expected.ManagedClubId));
            Assert.That(actual.CommittedSpendThisWindow, Is.EqualTo(expected.CommittedSpendThisWindow));
            Assert.That(actual.CommittedSpendThisWindow, Is.EqualTo(123_456_789L), "non-default, so a reseed cannot pass");
            Assert.That(actual.ActiveWindow.OpenWorldDay, Is.EqualTo(expected.ActiveWindow.OpenWorldDay));
            Assert.That(actual.ActiveWindow.CloseWorldDay, Is.EqualTo(expected.ActiveWindow.CloseWorldDay));
            Contract[] e = expected.ContractsSnapshot();
            Contract[] a = actual.ContractsSnapshot();
            Assert.That(a.Length, Is.EqualTo(e.Length));
            for (int i = 0; i < e.Length; i++)
            {
                Assert.That(a[i].PlayerId, Is.EqualTo(e[i].PlayerId));
                Assert.That(a[i].WagePerPeriod, Is.EqualTo(e[i].WagePerPeriod));
                Assert.That(a[i].LengthSeasons, Is.EqualTo(e[i].LengthSeasons));
            }
        }

        private static int[] ClubIdsOf(int count)
        {
            var ids = new int[count];
            for (int c = 0; c < count; c++)
            {
                ids[c] = c;
            }

            return ids;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-10-18 | —      | Initial implementation (#31 T1): frame-v8 transfers composition  |
// |         |            |        | locks — round-trip/resume, slot, clone, guard, coherence, F3.    |
// | 1.1     | 2026-10-20 | —      | M6: Load_AFrameWhoseTransfersBlockHoldsAnOrphanContract_IsRefused and |
// |         |            |        | Composition_WithAnOrphanContract_IsRefused (the missing negatives for |
// |         |            |        | the roster rule at Load and at composition); stale "TransfersState's  |
// |         |            |        | own id check" comment corrected (deleted by ERR-031-005).             |
#endregion
