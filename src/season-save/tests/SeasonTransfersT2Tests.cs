// File:     src/season-save/tests/SeasonTransfersT2Tests.cs
// Created:  2026-10-20
// Modified: 2026-10-20
// Author:   —
// Spec:     Transfers, Contracts & Negotiation #31 §7.1 T2, FR-TX-005/007/009/021..025/028, §5 T-TX-BID-001..004,
//           T-TX-REKEY-005, T-TX-DET-001/002, T-TX-INIT-001, T-TX-NEU-001; ERR-031-005/007/008/009/010;
//           Season & Competition Loop #30 §3.5 (d″), ERR-030-056; Code Standards #20
// Purpose:  #31 T2 end to end through SeasonLoop: genesis seeding, the bid seam against the club's real #40
//           entry, the production re-home carrying the player's whole career under his id, every
//           non-accepted outcome leaving #40 byte-identical, the staged season-boundary step, and the
//           save/load path never re-seeding.

using System;
using System.IO;

using NUnit.Framework;

using TacticalDirector.ClubFinances;
using TacticalDirector.Discipline;
using TacticalDirector.InjuriesMedical;
using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.PlayerProgression;
using TacticalDirector.Transfers;

using CFinances = TacticalDirector.ClubFinances.ClubFinances;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    public sealed class SeasonTransfersT2Tests
    {
        private const ulong WorldSeed = 0x31F2A0BEUL;
        private const int ClubCount = 4;
        private const int Managed = 0;
        private const int Seller = 2;
        private const long Budget = 50_000_000L;

        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "td-transfers-t2-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch (Exception) { }
        }

        // ── genesis ──────────────────────────────────────────────────────────────────

        [Test]
        public void CreateLoop_WithAProgressionStore_SeedsOneGenesisContractPerManagedPlayer()
        {
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(1, WorldSeed);
            ProgressionEngine progression = SeedFor(league, world);

            SeasonLoop loop = league.CreateLoop(world, Managed, RoundResolutionMode.QuickSimAll, progressionOrNull: progression);

            TransfersState t = loop.TransfersSnapshot();
            Assert.That(t, Is.Not.Null, "FR-TX-028: a career with a #28 store is transfers-wired at genesis");
            Assert.That(t.ContractCount, Is.EqualTo(progression.SquadFor(Managed).Count));
            Assert.That(t.ActiveWindow.OpenWorldDay, Is.EqualTo(0u), "ERR-031-008: the window opens on the new-game day");
            foreach (Contract c in t.ContractsSnapshot())
            {
                Assert.That(c.LengthSeasons, Is.EqualTo(TransfersConstants.DefaultContractSeasons));
            }
        }

        [Test]
        public void CreateLoop_WithoutAProgressionStore_LeavesTransfersUnwired()
        {
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            SeasonLoop loop = league.CreateLoop(new WorldStore(1, WorldSeed), Managed, RoundResolutionMode.QuickSimAll);

            Assert.That(loop.TransfersSnapshot(), Is.Null);
        }

        // ── the bid seam ─────────────────────────────────────────────────────────────

        [Test]
        public void ABuy_ReHomesTheWholeCareer_UnderTheSameId_AndDebitsExactlyTheFee()
        {
            Rig c = NewCareer();
            int target = BannedAt(c);
            var knock = InjuryState.Create();
            knock.Severity = InjurySeverity.Moderate;
            knock.RecoveryRemaining = 9;
            c.Career.SetMedicalState(Seller, target, in knock);
            long fee = ValueFor(c, target, buy: true);
            long balanceBefore = c.Loop.FinanceView(Managed).Balance;

            TransferSubmissionOutcome outcome = c.Loop.SubmitTransferBid(new Offer(target, Seller, fee, 2_000L, 2, true));

            Assert.That(outcome, Is.EqualTo(TransferSubmissionOutcome.Accepted));
            Assert.That(IndexOf(c.Progression.SquadFor(Managed), target), Is.GreaterThanOrEqualTo(0), "re-homed to the buyer…");
            Assert.That(IndexOf(c.Progression.SquadFor(Seller), target), Is.LessThan(0), "…and gone from the seller");
            Assert.That(c.Career.IsAvailable(Managed, target), Is.False, "T-TX-REKEY-005: his injury travelled with him");
            Assert.That(InjuryOf(c.Career, Managed, target).RecoveryRemaining, Is.EqualTo(9));
            Assert.Throws<ArgumentException>(() => c.Career.IsAvailable(Seller, target), "no row left behind");
            Assert.That(c.Discipline.HasEntry(target, DisciplineConstants.LeagueCompetitionKey), Is.True,
                "his ban is keyed by the unchanged id and so followed him");
            Assert.That(c.Loop.FinanceView(Managed).Balance, Is.EqualTo(balanceBefore - fee), "exactly one fee debit");
            TransfersState t = c.Loop.TransfersSnapshot();
            Assert.That(t.TryGetContract(target, out Contract contract), Is.True);
            Assert.That(contract.LengthSeasons, Is.EqualTo(2));
            Assert.That(t.CommittedSpendThisWindow, Is.EqualTo(fee));
        }

        [Test]
        public void ASell_ReHomesToTheBuyer_RemovesTheContract_AndCreditsTheFee()
        {
            Rig c = NewCareer();
            int target = PlayerAt(c, Managed, PlayerPosition.Defender);
            long fee = ValueFor(c, target, buy: false);
            long balanceBefore = c.Loop.FinanceView(Managed).Balance;

            Assert.That(c.Loop.SubmitTransferBid(new Offer(target, Seller, fee, 0L, 1, false)),
                Is.EqualTo(TransferSubmissionOutcome.Accepted));

            Assert.That(IndexOf(c.Progression.SquadFor(Seller), target), Is.GreaterThanOrEqualTo(0));
            Assert.That(c.Career.IsAvailable(Seller, target), Is.True, "the buyer carries his rows");
            Assert.That(c.Loop.TransfersSnapshot().TryGetContract(target, out Contract _), Is.False);
            Assert.That(c.Loop.FinanceView(Managed).Balance, Is.EqualTo(balanceBefore + fee));
            Assert.That(c.Loop.TransfersSnapshot().CommittedSpendThisWindow, Is.EqualTo(0L), "a sale adds no spend (KD-2)");
        }

        [Test]
        public void EveryNonAcceptedOutcome_LeavesFinancesRosterAndContractsByteIdentical()
        {
            Rig c = NewCareer();
            int target = PlayerAt(c, Seller, PlayerPosition.Forward);
            long value = ValueFor(c, target, buy: true);

            AssertNoMutation(c, new Offer(target, Seller, value - 1L, 0L, 1, true), TransferSubmissionOutcome.CounterOffered);
            AssertNoMutation(c, new Offer(target, Seller, value / 2L, 0L, 1, true), TransferSubmissionOutcome.Rejected);

            Rig poor = NewCareer(budget: 1L);
            AssertNoMutation(poor, new Offer(target, Seller, value, 0L, 1, true), TransferSubmissionOutcome.InsufficientBudget);
        }

        [Test]
        public void AFullDestination_IsSquadFull_AndADepletedSeller_IsSquadDepleted()
        {
            Rig c = NewCareer();

            // Fill the managed club to the cap (23 → 25), then the third buy is SquadFull.
            for (int i = 0; i < PlayerDatabaseConstants.CLUB_SQUAD_SIZE - 23; i++)
            {
                int buy = PlayerAt(c, 1 + i, PlayerPosition.Midfielder);
                Assert.That(c.Loop.SubmitTransferBid(new Offer(buy, 1 + i, ValueFor(c, buy, true), 0L, 1, true)),
                    Is.EqualTo(TransferSubmissionOutcome.Accepted));
            }

            int third = PlayerAt(c, 3, PlayerPosition.Midfielder);
            AssertNoMutation(c, new Offer(third, 3, ValueFor(c, third, true), 0L, 1, true), TransferSubmissionOutcome.SquadFull);

            // Sell defenders to club 3 until the managed club could no longer name a matchday squad.
            Rig s = NewCareer();
            TransferSubmissionOutcome last = TransferSubmissionOutcome.Accepted;
            int sold = 0;
            while (last == TransferSubmissionOutcome.Accepted && sold < 23)
            {
                int d = PlayerAt(s, Managed, PlayerPosition.Goalkeeper);
                last = s.Loop.SubmitTransferBid(new Offer(d, 3, ValueFor(s, d, false, buyer: 3), 0L, 1, false));
                sold++;
            }

            Assert.That(last, Is.EqualTo(TransferSubmissionOutcome.SquadDepleted),
                "ERR-031-007: selling the last goalkeeper is refused as an ordinary outcome");
            Assert.That(SquadRating.CanFieldStartingEleven(s.Progression.SquadFor(Managed)), Is.True);
        }

        [Test]
        public void ABidOutsideTheWindow_FailsLoud_WithNoMutation()
        {
            Rig c = NewCareer();
            int target = PlayerAt(c, Seller, PlayerPosition.Forward);
            var closed = TransfersState.CreateAtGenesis(Managed, new TransferWindow(100, 120), new int[0]);
            Rig outside = NewCareer(transfers: closed);
            byte[] before = Snapshot(outside);

            Assert.Throws<InvalidOperationException>(
                () => outside.Loop.SubmitTransferBid(new Offer(target, Seller, 1L, 0L, 1, true)), "F4");
            Assert.That(Snapshot(outside), Is.EqualTo(before));
        }

        [Test]
        public void ARawTransferFeePosting_IsRefusedOnceTransfersAreWired()
        {
            Rig c = NewCareer();
            var fee = new FinanceTransaction(FinanceTransactionKind.Debit, FinanceLineItem.TransferFee, 10L);

            Assert.Throws<InvalidOperationException>(() => c.Loop.ApplyTransaction(Managed, in fee));
        }

        // ── save / load / the boundary ───────────────────────────────────────────────

        [Test]
        public void ALoadNeverReSeeds_TheResumedLoopCarriesTheSavedContracts()
        {
            Rig c = NewCareer();
            int sold = PlayerAt(c, Managed, PlayerPosition.Defender);
            c.Loop.SubmitTransferBid(new Offer(sold, Seller, ValueFor(c, sold, false), 0L, 1, false));
            string path = Path.Combine(_tempDir, "after-sale.season");
            SeasonSaveManager.Save(c.Loop, matchOrNull: null, path);

            SeasonSaveContents loaded = SeasonSaveManager.Load(path);
            var career = PlayerCareerStates.FromBlocks(
                loaded.TrainingClubs, loaded.MedicalClubs, loaded.AppearanceClubs, injuryOccurrenceEnabled: false);
            SeasonLoop resumed = SeasonLoop.Restore(
                loaded.World, SeasonStateCodec.Encode(loaded.Season), RoundResolutionMode.QuickSimAll,
                career, null, loaded.Progression, loaded.Discipline, loaded.Finances, loaded.Transfers);

            TransfersState t = resumed.TransfersSnapshot();
            Assert.That(t.ContractCount, Is.EqualTo(22), "23 genesis contracts minus the sold player — not re-seeded");
            Assert.That(t.TryGetContract(sold, out Contract _), Is.False);
        }

        [Test]
        public void TheRoll_AgesContracts_ResetsSpend_RedrawsTheWindow_AndKeepsTheCareerSaveable()
        {
            Rig c = NewCareer(forceManagedRetiree: true);
            int bought = PlayerAt(c, Seller, PlayerPosition.Midfielder);
            c.Loop.SubmitTransferBid(new Offer(bought, Seller, ValueFor(c, bought, true), 0L, 3, true));
            int cursorBefore = c.Progression.NextPlayerId;
            PlayWholeSeason(c.Loop);
            uint rollDay = c.Loop.CurrentWorldDay;

            c.Loop.RollToNextSeason();

            TransfersState t = c.Loop.TransfersSnapshot();
            Assert.That(t.CommittedSpendThisWindow, Is.EqualTo(0L), "FR-TX-007");
            Assert.That(t.ActiveWindow.OpenWorldDay, Is.EqualTo(rollDay), "ERR-031-008: opens on the roll day");
            Assert.That(t.TryGetContract(bought, out Contract boughtContract), Is.True);
            Assert.That(boughtContract.LengthSeasons, Is.EqualTo(2), "aged one season");
            Assert.That(t.TryGetContract(c.ForcedRetiree, out Contract _), Is.False, "the retiree's contract left with him (G14)");
            bool regenContracted = false;
            foreach (Contract k in t.ContractsSnapshot())
            {
                regenContracted |= k.PlayerId >= cursorBefore;
            }

            Assert.That(regenContracted, Is.True, "the managed regen is contracted at the genesis terms (G14)");

            string path = Path.Combine(_tempDir, "rolled.season");
            SeasonSaveManager.Save(c.Loop, matchOrNull: null, path);
            Assert.That(SeasonSaveManager.Load(path).Transfers.ContractCount, Is.EqualTo(t.ContractCount),
                "AR round 1 M2: a roll that retires a contracted player leaves a saveable career");
        }

        [Test]
        public void AfterEveryGenesisContractExpires_TheManagerCanStillSell()
        {
            Rig c = NewCareer();
            for (int s = 0; s < TransfersConstants.DefaultContractSeasons; s++)
            {
                PlayWholeSeason(c.Loop);
                c.Loop.RollToNextSeason();
            }

            int genesisPlayer = PlayerAt(c, Managed, PlayerPosition.Defender);
            Assert.That(genesisPlayer, Is.LessThan(ClubCount * PlayerDatabaseConstants.CLUB_SQUAD_SIZE), "a genesis-id player");
            Assert.That(c.Loop.TransfersSnapshot().TryGetContract(genesisPlayer, out Contract _), Is.False, "expired");
            Assert.That(c.Loop.SubmitTransferBid(new Offer(genesisPlayer, Seller, ValueFor(c, genesisPlayer, false), 0L, 1, false)),
                Is.EqualTo(TransferSubmissionOutcome.Accepted), "ERR-031-009: an un-contracted player is sellable");
        }

        [Test]
        public void NoTransferAction_IsBehaviourNeutral()
        {
            // T-TX-NEU-001: a wired career that never bids plays the season byte-identically to an unwired one.
            Rig wired = NewCareer();
            Rig unwired = NewCareer(transfersUnwired: true);
            PlayWholeSeason(wired.Loop);
            PlayWholeSeason(unwired.Loop);

            Assert.That(SeasonStateCodec.Encode(wired.Loop.State), Is.EqualTo(SeasonStateCodec.Encode(unwired.Loop.State)));
            Assert.That(wired.Progression.Snapshot(), Is.EqualTo(unwired.Progression.Snapshot()));

            // Non-vacuity: one transfer moves the season.
            Rig moved = NewCareer();
            int target = PlayerAt(moved, Seller, PlayerPosition.Forward);
            moved.Loop.SubmitTransferBid(new Offer(target, Seller, ValueFor(moved, target, true), 0L, 1, true));
            PlayWholeSeason(moved.Loop);
            Assert.That(moved.Progression.Snapshot(), Is.Not.EqualTo(unwired.Progression.Snapshot()));
        }

        // ── fixture ──────────────────────────────────────────────────────────────────

        private sealed class Rig
        {
            internal SeasonLoop Loop;
            internal ProgressionEngine Progression;
            internal PlayerCareerStates Career_;
            internal DisciplineState Discipline;
            internal int ForcedRetiree = -1;

            internal PlayerCareerStates Career => Career_;
        }

        private static Rig NewCareer(
            long budget = Budget, TransfersState transfers = null, bool transfersUnwired = false, bool forceManagedRetiree = false)
        {
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(1, WorldSeed);
            SeasonState season = league.CreateSeason(managedClubId: Managed);
            var squads = new Squad[ClubCount];
            for (int club = 0; club < ClubCount; club++)
            {
                squads[club] = league.ResolveByClubId(club);
            }

            int forced = -1;
            if (forceManagedRetiree)
            {
                var players = new PlayerRecord[squads[Managed].Count];
                for (int i = 0; i < players.Length; i++)
                {
                    players[i] = squads[Managed].GetPlayer(i);
                }

                players[4].Age = 45;   // a defender, flagged on the first lived day
                forced = players[4].PlayerId;
                squads[Managed] = new Squad(Managed, players);
            }

            ProgressionEngine progression = ProgressionEngine.SeedFrom(squads, world.CurrentWorldTick);
            var career = PlayerCareerStates.ForLeague(new ProgressionSquads(progression), ClubIds(), injuryOccurrenceEnabled: false);

            // One unserved ban on a seller-club player, to prove a #44 tally follows a transfer.
            int banned = squads[Seller].GetPlayer(12).PlayerId;
            DisciplineState discipline = DisciplineState.FromEntries(new[]
            {
                new DisciplineEntry(banned, DisciplineConstants.LeagueCompetitionKey, 0, 50)
            });

            ClubFinanceEntry[] finances = ClubFinanceEntry.CreateInitialForSquads(squads);
            for (int i = 0; i < finances.Length; i++)
            {
                CFinances f = finances[i].Finances;
                f.TransferBudget = budget;
                finances[i] = new ClubFinanceEntry(finances[i].ClubId, in f);
            }

            if (transfers == null && !transfersUnwired)
            {
                var ids = new int[squads[Managed].Count];
                for (int i = 0; i < ids.Length; i++)
                {
                    ids[i] = squads[Managed].GetPlayer(i).PlayerId;
                }

                transfers = TransfersState.CreateAtGenesis(Managed, TransferWindow.SummerWindowFrom(0), ids);
            }

            var loop = new SeasonLoop(
                world, season, RoundResolutionMode.QuickSimAll, career, null, progression, discipline, finances,
                transfersUnwired ? null : transfers);
            return new Rig
            {
                Loop = loop, Progression = progression, Career_ = career, Discipline = discipline, ForcedRetiree = forced
            };
        }

        // The lowest-id player at the given position on the club's CURRENT roster — a genesis player
        // wherever one remains, so a pick is stable across regens.
        private static int PlayerAt(Rig c, int club, PlayerPosition position)
        {
            Squad squad = c.Progression.SquadFor(club);
            for (int i = 0; i < squad.Count; i++)
            {
                if (squad.GetPlayer(i).Position == position)
                {
                    return squad.GetPlayer(i).PlayerId;
                }
            }

            return -1;
        }

        private static int BannedAt(Rig c) => c.Discipline.EntryAt(0).PlayerId;

        // #31's own counterparty valuation, re-derived from the roster the same way SubmitBid does.
        private static long ValueFor(Rig c, int playerId, bool buy, int buyer = Seller)
        {
            Assert.That(c.Progression.TryFindPlayer(playerId, out PlayerRecord rec, out int owner), Is.True);
            int counterparty = buy ? owner : buyer;
            Squad squad = c.Progression.SquadFor(counterparty);
            int stock = 0;
            for (int i = 0; i < squad.Count; i++)
            {
                if (squad.GetPlayer(i).Position == rec.Position)
                {
                    stock++;
                }
            }

            if (buy)
            {
                stock--;
            }

            return PlayerValuation.CounterpartyValue(in rec.Attributes, rec.Age, stock);
        }

        private static ProgressionEngine SeedFor(League league, WorldStore world)
        {
            var squads = new Squad[ClubCount];
            for (int club = 0; club < ClubCount; club++)
            {
                squads[club] = league.ResolveByClubId(club);
            }

            return ProgressionEngine.SeedFrom(squads, world.CurrentWorldTick);
        }

        private static void AssertNoMutation(Rig c, Offer offer, TransferSubmissionOutcome expected)
        {
            byte[] before = Snapshot(c);

            Assert.That(c.Loop.SubmitTransferBid(offer), Is.EqualTo(expected));
            Assert.That(Snapshot(c), Is.EqualTo(before), expected + " must leave #40, #28, #31 and the career untouched");
        }

        private static byte[] Snapshot(Rig c)
        {
            using (var ms = new MemoryStream())
            {
                byte[] f = ClubFinancesSaveCodec.Encode(c.Loop.FinanceEntriesForSave());
                ms.Write(f, 0, f.Length);
                byte[] p = c.Progression.Snapshot();
                ms.Write(p, 0, p.Length);
                byte[] t = TransfersSaveCodec.Encode(c.Loop.TransfersSnapshot());
                ms.Write(t, 0, t.Length);
                byte[] m = MedicalSaveCodec.Encode(c.Career.MedicalBlocks());
                ms.Write(m, 0, m.Length);
                return ms.ToArray();
            }
        }

        private static InjuryState InjuryOf(PlayerCareerStates career, int club, int playerId)
        {
            foreach (ClubInjuryStates block in career.MedicalBlocks())
            {
                if (block.ClubId != club)
                {
                    continue;
                }

                for (int i = 0; i < block.PlayerIds.Length; i++)
                {
                    if (block.PlayerIds[i] == playerId)
                    {
                        return block.States[i];
                    }
                }
            }

            throw new ArgumentException("not carried");
        }

        private static void PlayWholeSeason(SeasonLoop loop)
        {
            while (!loop.IsSeasonComplete)
            {
                loop.AdvanceToNextFixtureDay();
                loop.AdvanceAndPlayNextRound();
            }

            loop.AdvanceDays(1);
        }

        private static int[] ClubIds()
        {
            var ids = new int[ClubCount];
            for (int c = 0; c < ClubCount; c++)
            {
                ids[c] = c;
            }

            return ids;
        }

        private static int IndexOf(Squad squad, int playerId)
        {
            for (int i = 0; i < squad.Count; i++)
            {
                if (squad.GetPlayer(i).PlayerId == playerId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-10-20 | —      | Initial implementation (#31 T2): end-to-end transfer locks.       |
#endregion
