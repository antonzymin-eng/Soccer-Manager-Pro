// File:     src/season-save/tests/SeasonTransfersT2Tests.cs
// Created:  2026-10-20
// Modified: 2026-10-20 (AR round 4 — v1.4)
// Author:   —
// Spec:     Transfers, Contracts & Negotiation #31 §7.1 T2, FR-TX-005/007/009/021..025/028, §5 T-TX-BID-001..004,
//           T-TX-REKEY-005, T-TX-DET-001/002/003, T-TX-INIT-001, T-TX-NEU-001; ERR-031-005/007/008/009/010/011/012;
//           ERR-044-014; Season & Competition Loop #30 §3.5 (d″), ERR-030-056; Code Standards #20
// Purpose:  #31 T2 end to end through SeasonLoop: genesis seeding, the bid seam against the club's real #40
//           entry, the production re-home carrying the player's whole career under his id, every
//           non-accepted outcome leaving #40 byte-identical, the staged season-boundary step, and the
//           save/load path never re-seeding. AR round 2 adds the production SeasonTransferRoster adapter's
//           throw paths, the whole-career REKEY lock (lifecycle + #29 + #30 rows), the ERR-044-014 ban
//           property, the League.CreateLoop production composition, and a snapshot wide enough to back
//           "career untouched". AR round 3 adds the full-snapshot neutrality lock (T-TX-NEU-001: a played
//           season, the roll and a season-2 round, wired vs unwired, with a one-sale non-vacuity leg),
//           T-TX-DET-003, the odd-club (bye) ban property, the full-InjuryState and yellow-count REKEY
//           legs, and the League.CreateLoop KD-4 refusal. AR round 4 adds the #28 PlayerRecord field-equality
//           leg to T-TX-REKEY-005, ParamName assertions on the KD-4 refusals, and Assume.That guards for the
//           default-[GT] window tests.

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
using TacticalDirector.TrainingSystem;
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

        /// <summary>The seller-club squad index NewCareer bans by default, and the length of that ban.</summary>
        private const int BannedIndex = 12;
        private const int BanMatches = 50;

        /// <summary>The yellow-card count NewCareer can seed beside the ban (REKEY-005's #44 tally leg).</summary>
        private const int Yellows = 2;

        /// <summary>Close − openingFixtureDay under ERR-031-011 (world days).</summary>
        private static readonly uint InSeasonTail = (uint)TransfersConstants.SummerWindowInSeasonDays - 1u;

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
            Assert.That(t.ActiveWindow.CloseWorldDay, Is.EqualTo(loop.State.Calendar.DayOfRound(0) + InSeasonTail),
                "ERR-031-011: the close is anchored to season 1's opening fixture day");
            foreach (Contract c in t.ContractsSnapshot())
            {
                Assert.That(c.LengthSeasons, Is.EqualTo(TransfersConstants.DefaultContractSeasons));
            }
        }

        [Test]
        public void CreateLoop_WithTheClockPastTheOpeningFixtureDay_ThrowsTheKd4Refusal_NotTheWindowsRangeError()
        {
            // AR round 3 S-L5: the cursor invariant is checked BEFORE the #31 window is derived. Unchecked, a
            // wired composition surfaced SummerWindowFor's ArgumentOutOfRangeException (opening < begin) —
            // Assert.Throws<ArgumentException> is exact-type, so that regression fails this test.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(1, WorldSeed);
            ProgressionEngine progression = SeedFor(league, world);
            uint opening = league.CreateSeason(Managed).Calendar.DayOfRound(0);
            for (uint d = 0; d <= opening; d++)
            {
                world.AdvanceDay();
            }

            ArgumentException wired = Assert.Throws<ArgumentException>(
                () => league.CreateLoop(world, Managed, RoundResolutionMode.QuickSimAll, progressionOrNull: progression));
            StringAssert.Contains("KD-4", wired.Message);
            Assert.That(wired.ParamName, Is.EqualTo("world"));

            ArgumentException unwired = Assert.Throws<ArgumentException>(
                () => league.CreateLoop(world, Managed, RoundResolutionMode.QuickSimAll));
            StringAssert.Contains("KD-4", unwired.Message);
            Assert.That(unwired.ParamName, Is.EqualTo("world"));
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
            // T-TX-REKEY-005: ONE player carries every kind of per-player state — a banned, injured,
            // retirement-flagged, trained, appeared-in Seller player — and all of it must follow him.
            Rig c = NewCareer(sellerRetireeIndex: BannedIndex, bannedYellows: Yellows);
            int target = BannedAt(c);
            Assert.That(target, Is.EqualTo(c.SellerRetiree), "the banned player is also the aged one");
            c.Loop.AdvanceDays(6);   // lived days: growth accrues, the age-45 flag is raised, training advances
            var knock = InjuryState.Create();
            knock.Severity = InjurySeverity.Moderate;
            knock.RecoveryRemaining = 9;
            c.Career.SetMedicalState(Seller, target, in knock);
            Assert.That(c.Career.TrySetFocus(Seller, target, TrainingFocus.Fitness), Is.True);
            c.Career.RecordAppearances(Seller, new[] { target }, c.Loop.CurrentWorldDay);
            Assert.That(c.Progression.TryFindPlayer(target, out PlayerRecord recordBefore, out int ownerBefore), Is.True);
            Assert.That(ownerBefore, Is.EqualTo(Seller));
            int[] attributesBefore = recordBefore.Attributes.ToArray();
            PlayerLifecycle lifeBefore = LifecycleOf(c.Progression, Seller, target);
            TrainingState trainingBefore = TrainingOf(c.Career, Seller, target);
            AppearanceState appearanceBefore = AppearanceOf(c.Career, Seller, target);
            InjuryState injuryBefore = InjuryOf(c.Career, Seller, target);
            DisciplineEntry tallyBefore = c.Discipline.EntryFor(target, DisciplineConstants.LeagueCompetitionKey);
            Assert.That(tallyBefore.Yellows, Is.EqualTo(Yellows), "non-vacuity: the #44 tally carries a yellow count…");
            Assert.That(tallyBefore.BanMatchesRemaining, Is.EqualTo(BanMatches), "…and an unserved ban");
            Assert.That(injuryBefore.Severity, Is.EqualTo(InjurySeverity.Moderate), "non-vacuity: the #41 state is non-default");
            Assert.That(lifeBefore.RetirementFlag, Is.True, "non-vacuity: the #28 retirement state is non-default");
            Assert.That(lifeBefore.RetirementDay, Is.GreaterThan(0u));
            Assert.That(lifeBefore.BirthWorldDay, Is.LessThan(0L));
            Assert.That(lifeBefore.GrowthCursor, Is.Not.EqualTo(0L), "non-vacuity: the accumulator has moved");
            Assert.That(trainingBefore.Focus, Is.EqualTo(TrainingFocus.Fitness));
            Assert.That(appearanceBefore.RecentBits, Is.EqualTo(1u));
            long fee = ValueFor(c, target, buy: true);
            long balanceBefore = c.Loop.FinanceView(Managed).Balance;

            TransferSubmissionOutcome outcome = c.Loop.SubmitTransferBid(new Offer(target, Seller, fee, 2_000L, 2, true));

            Assert.That(outcome, Is.EqualTo(TransferSubmissionOutcome.Accepted));
            Assert.That(IndexOf(c.Progression.SquadFor(Managed), target), Is.GreaterThanOrEqualTo(0), "re-homed to the buyer…");
            Assert.That(IndexOf(c.Progression.SquadFor(Seller), target), Is.LessThan(0), "…and gone from the seller");
            Assert.That(c.Career.IsAvailable(Managed, target), Is.False, "his injury travelled with him");
            AssertSameInjury(injuryBefore, InjuryOf(c.Career, Managed, target));
            Assert.That(c.Discipline.EntryFor(target, DisciplineConstants.LeagueCompetitionKey), Is.EqualTo(tallyBefore),
                "the #44 tally (yellows and ban) is keyed by the unchanged id");
            Assert.Throws<ArgumentException>(() => c.Career.IsAvailable(Seller, target), "no row left behind");

            // #28 record (the roster entry itself), lifecycle, #29 training row and #30 appearance row: moved verbatim…
            Assert.That(c.Progression.TryFindPlayer(target, out PlayerRecord recordAfter, out int ownerAfter), Is.True);
            Assert.That(ownerAfter, Is.EqualTo(Managed));
            AssertSameRecord(recordBefore, attributesBefore, recordAfter);
            AssertSameLifecycle(lifeBefore, LifecycleOf(c.Progression, Managed, target));
            AssertSameTraining(trainingBefore, TrainingOf(c.Career, Managed, target));
            AssertSameAppearance(appearanceBefore, AppearanceOf(c.Career, Managed, target));

            // …and absent at the source, in every store that keys by club.
            Assert.Throws<ArgumentException>(() => c.Progression.LifecycleView(Seller, target), "#28 row left behind");
            Assert.Throws<ArgumentException>(() => c.Career.TrainingView(Seller, target), "#29 row left behind");
            Assert.Throws<ArgumentException>(() => c.Career.MedicalView(Seller, target), "#41 row left behind");
            Assert.That(AppearanceHas(c.Career, Seller, target), Is.False, "#30 row left behind");

            Assert.That(c.Loop.FinanceView(Managed).Balance, Is.EqualTo(balanceBefore - fee), "exactly one fee debit");
            TransfersState t = c.Loop.TransfersSnapshot();
            Assert.That(t.TryGetContract(target, out Contract contract), Is.True);
            Assert.That(contract.LengthSeasons, Is.EqualTo(2));
            Assert.That(t.CommittedSpendThisWindow, Is.EqualTo(fee));
        }

        [Test]
        public void ASoldPlayer_IsSelectedByTheBuyersNextPlayedRound_AndIsGoneFromTheSellers()
        {
            // T-TX-REKEY-005, the downstream half: the buyer's next played round can actually field him. The
            // subject is a managed starter the engine's own selector puts in the SELLER club's eleven (the
            // managed club is the strongest in this league, so no buy into it could make its eleven).
            int index = SelectableManagedIndex();
            Rig c = NewCareer(bannedIndex: -1);
            int target = c.Progression.SquadFor(Managed).GetPlayer(index).PlayerId;
            Assert.That(c.Loop.SubmitTransferBid(new Offer(target, Seller, ValueFor(c, target, buy: false), 0L, 1, false)),
                Is.EqualTo(TransferSubmissionOutcome.Accepted));

            c.Loop.AdvanceToNextFixtureDay();
            uint day = c.Loop.CurrentWorldDay;
            c.Loop.AdvanceAndPlayNextRound();

            Assert.That(IndexOf(c.Progression.SquadFor(Seller), target), Is.GreaterThanOrEqualTo(0), "in the buyer's squad…");
            Assert.That(IndexOf(c.Progression.SquadFor(Managed), target), Is.LessThan(0), "…and absent from the seller's");
            Assert.That(FieldedOn(c.Career, Seller, target, day), Is.True, "the buyer's selector fielded him");
            Assert.That(AppearanceHas(c.Career, Managed, target), Is.False, "and the seller's record carries nothing of him");
        }

        [Test]
        public void ABannedPlayer_ServesHisBanAtTheBuyer_AndSitsOutThatClubsNextRound()
        {
            // ERR-044-014: whose ban a fixture serves is decided by the roster, never by the id. Three clubs
            // (odd N: one club has a bye each round) make that observable. The banned player's id is the
            // MANAGED club's and he is sold to the Seller club; over the three rounds of the first leg each
            // of those clubs sits out one round, so 'the club his id names' and 'the club that holds him'
            // are told apart. His ban must tick exactly in the rounds the Seller plays and not otherwise.
            // The control (same sale, no ban) shows he WOULD have been fielded, so the ban is what kept him out.
            const int clubs = 3;
            int index = SelectableManagedIndex(clubs);
            Rig banned = NewCareer(bannedIndex: index, bannedClub: Managed, clubCount: clubs);
            Rig control = NewCareer(bannedIndex: -1, clubCount: clubs);
            int target = BannedAt(banned);
            Assert.That(control.Progression.SquadFor(Managed).GetPlayer(index).PlayerId, Is.EqualTo(target));
            foreach (Rig rig in new[] { banned, control })
            {
                Assert.That(rig.Loop.SubmitTransferBid(new Offer(target, Seller, ValueFor(rig, target, buy: false), 0L, 1, false)),
                    Is.EqualTo(TransferSubmissionOutcome.Accepted));
            }

            int remaining = banned.Discipline.EntryFor(target, DisciplineConstants.LeagueCompetitionKey).BanMatchesRemaining;
            Assert.That(remaining, Is.EqualTo(BanMatches), "the ban travelled under the unchanged id");
            bool sawSellerPlayWhileIdClubSits = false;
            bool sawIdClubPlayWhileSellerSits = false;
            bool controlFielded = false;
            for (int round = 0; round < clubs; round++)
            {
                banned.Loop.AdvanceToNextFixtureDay();
                control.Loop.AdvanceToNextFixtureDay();
                uint day = banned.Loop.CurrentWorldDay;
                int roundIndex = banned.Loop.State.Calendar.NextRoundIndex;
                bool sellerPlays = ClubPlaysIn(banned.Loop.State, roundIndex, Seller);
                bool idClubPlays = ClubPlaysIn(banned.Loop.State, roundIndex, Managed);
                banned.Loop.AdvanceAndPlayNextRound();
                control.Loop.AdvanceAndPlayNextRound();

                remaining -= sellerPlays ? 1 : 0;
                Assert.That(banned.Discipline.EntryFor(target, DisciplineConstants.LeagueCompetitionKey).BanMatchesRemaining,
                    Is.EqualTo(remaining), "round " + roundIndex + ": served by the club that holds him, and only when it plays");
                if (sellerPlays)
                {
                    Assert.That(FieldedOn(banned.Career, Seller, target, day), Is.False, "he sat out the buyer's round");
                    controlFielded |= FieldedOn(control.Career, Seller, target, day);
                }

                sawSellerPlayWhileIdClubSits |= sellerPlays && !idClubPlays;
                sawIdClubPlayWhileSellerSits |= idClubPlays && !sellerPlays;
            }

            Assert.That(sawSellerPlayWhileIdClubSits && sawIdClubPlayWhileSellerSits, Is.True,
                "non-vacuity: the rounds separate 'the club that holds him' from 'the club his id names'");
            Assert.That(controlFielded, Is.True, "non-vacuity: unbanned, the same sale fields him");
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
        public void AnAcceptedTransfer_LeavesTheCounterpartyClubsLedgerUntouched_ByDesign()
        {
            // KD-2 / FR-TX-005 (AR round 3, M16): the minimal tier posts the fee to the MANAGED club's #40 entry
            // only. The counterparty AI club's entry never sees the money — so AI clubs' FinanceView and
            // SettleFinances never see transfer cash, and money enters or leaves the league on every transfer.
            // This pins the INTERIM behaviour (option (b), the minimal tier as shipped); the (a)-vs-(b) choice is an OPEN
            // owner decision (spec §7.2, open-issues transfer-counterparty-ledger), so no owner choice is claimed here.
            // A mirror-posting change must update this test deliberately.
            Rig c = NewCareer();
            int bought = PlayerAt(c, Seller, PlayerPosition.Midfielder);
            int sold = PlayerAt(c, Managed, PlayerPosition.Defender);
            byte[] sellerBefore = LedgerOf(c, Seller);
            long managedBefore = c.Loop.FinanceView(Managed).Balance;

            Assert.That(c.Loop.SubmitTransferBid(new Offer(bought, Seller, ValueFor(c, bought, buy: true), 0L, 1, true)),
                Is.EqualTo(TransferSubmissionOutcome.Accepted));
            Assert.That(c.Loop.SubmitTransferBid(new Offer(sold, Seller, ValueFor(c, sold, buy: false), 0L, 1, false)),
                Is.EqualTo(TransferSubmissionOutcome.Accepted));

            Assert.That(c.Loop.FinanceView(Managed).Balance, Is.Not.EqualTo(managedBefore), "non-vacuity: the managed ledger moved");
            Assert.That(LedgerOf(c, Seller), Is.EqualTo(sellerBefore), "the counterparty club's #40 entry is byte-identical");
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

            // Sell goalkeepers to club 3 until the managed club has one left; selling that last one would leave
            // no matchday squad. The refusal goes through AssertNoMutation, so it is proven byte-identical.
            Rig s = NewCareer();
            int goalkeepers = CountAt(s.Progression.SquadFor(Managed), PlayerPosition.Goalkeeper);
            Assert.That(goalkeepers, Is.GreaterThan(1), "precondition: the template carries spare goalkeepers");
            for (int sold = 0; sold < goalkeepers - 1; sold++)
            {
                int spare = PlayerAt(s, Managed, PlayerPosition.Goalkeeper);
                Assert.That(
                    s.Loop.SubmitTransferBid(new Offer(spare, 3, ValueFor(s, spare, false, buyer: 3), 0L, 1, false)),
                    Is.EqualTo(TransferSubmissionOutcome.Accepted));
            }

            int lastKeeper = PlayerAt(s, Managed, PlayerPosition.Goalkeeper);
            AssertNoMutation(s, new Offer(lastKeeper, 3, ValueFor(s, lastKeeper, false, buyer: 3), 0L, 1, false),
                TransferSubmissionOutcome.SquadDepleted);   // ERR-031-007: an ordinary outcome, never a throw
            Assert.That(CountAt(s.Progression.SquadFor(Managed), PlayerPosition.Goalkeeper), Is.EqualTo(1));
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

        // ── the production roster adapter, driven directly (AR round 2 M7a / L3) ─────────────────────────
        // SubmitBid checks ownership before it ever asks the port, so none of these throw paths is reachable
        // through the loop; each is an integration-defect guard and is locked here against the real adapter.

        [Test]
        public void TheAdapter_RefusesToBeBuiltWithoutTheRosterStore()
        {
            Assert.Throws<ArgumentNullException>(() => new SeasonTransferRoster(null, null));
        }

        [Test]
        public void CanRelease_AnUnknownClub_Throws_NeverSquadDepleted()
        {
            Rig c = NewCareer();
            SeasonTransferRoster port = AdapterOf(c);
            int player = PlayerAt(c, Managed, PlayerPosition.Defender);

            Assert.Throws<InvalidOperationException>(() => port.CanRelease(99, player));
            Assert.Throws<InvalidOperationException>(() => port.CanRelease(-1, player));
        }

        [Test]
        public void CanRelease_APlayerNotOnThatClubsRoster_Throws_NeverSquadDepleted()
        {
            Rig c = NewCareer();
            SeasonTransferRoster port = AdapterOf(c);
            int sellers = PlayerAt(c, Seller, PlayerPosition.Defender);

            Assert.Throws<InvalidOperationException>(() => port.CanRelease(Managed, sellers), "owned by another club");
            Assert.Throws<InvalidOperationException>(() => port.CanRelease(Managed, 987_654), "owned by nobody");
        }

        [Test]
        public void CanRelease_IsFalseOnlyWhenTheClubCouldNoLongerFieldAMatchdaySquad()
        {
            Rig c = NewCareer();
            SeasonTransferRoster port = AdapterOf(c);
            int defender = PlayerAt(c, Managed, PlayerPosition.Defender);
            Assert.That(port.CanRelease(Managed, defender), Is.True, "a full squad can spare a defender");

            // Strip the managed club to one goalkeeper (store-level, so no bid is involved).
            int keepers = CountAt(c.Progression.SquadFor(Managed), PlayerPosition.Goalkeeper);
            for (int i = 0; i < keepers - 1; i++)
            {
                c.Progression.MovePlayer(PlayerAt(c, Managed, PlayerPosition.Goalkeeper), Managed, 3);
            }

            int lastKeeper = PlayerAt(c, Managed, PlayerPosition.Goalkeeper);
            Assert.That(port.CanRelease(Managed, lastKeeper), Is.False, "ERR-031-007: no goalkeeper left to field");
            Assert.That(port.CanRelease(Managed, defender), Is.True, "he can still spare a defender");
        }

        [Test]
        public void TryPreviewRosterCommit_AnImpossibleMove_Throws_NeverFull()
        {
            Rig c = NewCareer();
            SeasonTransferRoster port = AdapterOf(c);
            int mine = PlayerAt(c, Managed, PlayerPosition.Defender);
            int theirs = PlayerAt(c, Seller, PlayerPosition.Defender);
            byte[] before = RosterSnapshot(c.Progression, c.Career);

            Assert.Throws<InvalidOperationException>(() => port.TryPreviewRosterCommit(99, Seller, mine), "unknown source club");
            Assert.Throws<InvalidOperationException>(() => port.TryPreviewRosterCommit(Managed, 99, mine), "unknown destination club");
            Assert.Throws<InvalidOperationException>(() => port.TryPreviewRosterCommit(Managed, Managed, mine), "same club");
            Assert.Throws<InvalidOperationException>(() => port.TryPreviewRosterCommit(Managed, Seller, theirs), "player is on the destination, not the source");
            Assert.Throws<InvalidOperationException>(() => port.TryPreviewRosterCommit(Managed, Seller, 987_654), "player is nobody's");
            Assert.That(RosterSnapshot(c.Progression, c.Career), Is.EqualTo(before), "a preview never writes");
        }

        [Test]
        public void TryPreviewRosterCommit_WhenTheStoreAndTheCareerRowsDisagree_Throws_NeverFull()
        {
            // No test hook is needed: a career built over a store where the player has ALREADY moved
            // carries him at the buyer, while the adapter's own store still has him at the seller.
            Rig c = NewCareer();
            int target = PlayerAt(c, Seller, PlayerPosition.Defender);
            PlayerCareerStates skewed = CareerWhereAlreadyMoved(target, Seller, Managed);
            SeasonTransferRoster port = new SeasonTransferRoster(c.Progression, skewed);

            Assert.That(c.Progression.CanMovePlayer(target, Seller, Managed), Is.True, "the store allows the move…");
            Assert.That(skewed.CanMovePlayerRows(target, Seller, Managed), Is.False, "…the career rows do not");
            var e = Assert.Throws<InvalidOperationException>(() => port.TryPreviewRosterCommit(Seller, Managed, target));
            StringAssert.Contains("disagree", e.Message);
        }

        [Test]
        public void RequestRosterCommit_WithoutAPossibleMove_Throws_AndMovesNothing()
        {
            Rig c = NewCareer();
            SeasonTransferRoster port = AdapterOf(c);
            int mine = PlayerAt(c, Managed, PlayerPosition.Defender);
            int theirs = PlayerAt(c, Seller, PlayerPosition.Defender);
            byte[] before = RosterSnapshot(c.Progression, c.Career);

            Assert.Throws<InvalidOperationException>(() => port.RequestRosterCommit(99, Seller, mine), "unknown club");
            Assert.Throws<InvalidOperationException>(() => port.RequestRosterCommit(Managed, Managed, mine), "same club");
            Assert.Throws<InvalidOperationException>(() => port.RequestRosterCommit(Managed, Seller, theirs), "not on the source");
            Assert.Throws<InvalidOperationException>(() => port.RequestRosterCommit(Managed, Seller, 987_654), "nobody's player");
            Assert.That(RosterSnapshot(c.Progression, c.Career), Is.EqualTo(before), "a refused commit moves nothing");
        }

        [Test]
        public void RequestRosterCommit_WhenThePreviewHasGoneStale_Throws_AndMovesNothing()
        {
            Rig c = NewCareer();
            SeasonTransferRoster port = AdapterOf(c);
            int target = PlayerAt(c, Seller, PlayerPosition.Defender);
            Assert.That(port.TryPreviewRosterCommit(Seller, Managed, target), Is.True, "the preview succeeded…");

            // …then the roster changes under it (a second writer — exactly what the preview contract forbids).
            c.Progression.MovePlayer(target, Seller, 3);
            byte[] before = RosterSnapshot(c.Progression, c.Career);

            Assert.Throws<InvalidOperationException>(() => port.RequestRosterCommit(Seller, Managed, target));
            Assert.That(RosterSnapshot(c.Progression, c.Career), Is.EqualTo(before), "nothing moved on the stale commit");
        }

        [Test]
        public void RequestRosterCommit_WhenTheDestinationFilledAfterThePreview_Throws_AndMovesNothing()
        {
            Rig c = NewCareer();
            SeasonTransferRoster port = AdapterOf(c);
            int target = PlayerAt(c, Seller, PlayerPosition.Defender);
            c.Progression.MovePlayer(PlayerAt(c, 1, PlayerPosition.Midfielder), 1, Managed);   // 23 → 24
            Assert.That(port.TryPreviewRosterCommit(Seller, Managed, target), Is.True, "one slot left at preview time");
            c.Progression.MovePlayer(PlayerAt(c, 1, PlayerPosition.Defender), 1, Managed);     // 24 → 25: full
            byte[] before = RosterSnapshot(c.Progression, c.Career);

            Assert.Throws<InvalidOperationException>(() => port.RequestRosterCommit(Seller, Managed, target));
            Assert.That(RosterSnapshot(c.Progression, c.Career), Is.EqualTo(before), "nothing moved on the stale commit");
        }

        [Test]
        public void RequestRosterCommit_WhenTheCareerRowsDisagree_ThrowsBeforeEitherWriter()
        {
            // Both predicates are asked before either write: the store must NOT move when the career refuses.
            Rig c = NewCareer();
            int target = PlayerAt(c, Seller, PlayerPosition.Defender);
            PlayerCareerStates skewed = CareerWhereAlreadyMoved(target, Seller, Managed);
            SeasonTransferRoster port = new SeasonTransferRoster(c.Progression, skewed);
            byte[] before = RosterSnapshot(c.Progression, skewed);

            Assert.Throws<InvalidOperationException>(() => port.RequestRosterCommit(Seller, Managed, target));
            Assert.That(RosterSnapshot(c.Progression, skewed), Is.EqualTo(before), "neither the store nor the career moved");
            Assert.That(IndexOf(c.Progression.SquadFor(Seller), target), Is.GreaterThanOrEqualTo(0), "he is still at the seller");
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
            Assert.That(t.ActiveWindow.CloseWorldDay, Is.EqualTo(c.Loop.State.Calendar.DayOfRound(0) + InSeasonTail),
                "ERR-031-011: closes N − 1 days after the new season's opening fixture day");
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

        // ── ERR-031-011: the summer window does not depend on when the client rolls ──────────────

        [Test]
        public void APromptRoll_OpensOnTheRollDay_AndCoversTheOpeningFixtureDay()
        {
            Rig c = NewCareer();
            PlayWholeSeason(c.Loop);
            uint rollDay = c.Loop.CurrentWorldDay;

            c.Loop.RollToNextSeason();

            TransferWindow w = c.Loop.TransfersSnapshot().ActiveWindow;
            uint opening = c.Loop.State.Calendar.DayOfRound(0);
            Assert.That(w.OpenWorldDay, Is.EqualTo(rollDay), "nothing transacts before the season exists");
            Assume.That(opening, Is.GreaterThan(rollDay + (uint)TransfersConstants.SummerWindowInSeasonDays),
                "non-vacuity needs the default close-season break: a roll-day-length window must close before the opening fixture");
            Assert.That(w.IsWindowOpen(opening), Is.True, "AR round 2 M6: the window is still open on the opening fixture day");
            Assert.That(w.CloseWorldDay, Is.EqualTo(opening + InSeasonTail));
        }

        [Test]
        public void APromptRollAndALateRoll_CloseTheWindowOnTheSameDay()
        {
            Rig prompt = NewCareer();
            PlayWholeSeason(prompt.Loop);
            prompt.Loop.RollToNextSeason();

            Rig late = NewCareer();
            PlayWholeSeason(late.Loop);
            late.Loop.AdvanceDays(30);   // linger in the close season (AdvanceDays allows up to the opening day)
            uint lateRollDay = late.Loop.CurrentWorldDay;
            late.Loop.RollToNextSeason();

            TransferWindow p = prompt.Loop.TransfersSnapshot().ActiveWindow;
            TransferWindow l = late.Loop.TransfersSnapshot().ActiveWindow;
            Assert.That(late.Loop.State.Calendar.DayOfRound(0), Is.EqualTo(prompt.Loop.State.Calendar.DayOfRound(0)),
                "precondition: the next calendar is roll-time independent (KD-6)");
            Assert.That(l.OpenWorldDay, Is.EqualTo(lateRollDay), "only the open day follows the roll");
            Assert.That(l.OpenWorldDay, Is.Not.EqualTo(p.OpenWorldDay), "non-vacuity: the two rolls really differ in time");
            Assert.That(l.CloseWorldDay, Is.EqualTo(p.CloseWorldDay), "ERR-031-011: the close does not depend on roll timing");
        }

        [Test]
        public void TheCloseSitsTheSameDistanceAfterTheOpeningFixture_AtGenesisAndAtABoundary()
        {
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(1, WorldSeed);
            SeasonLoop loop = league.CreateLoop(
                world, Managed, RoundResolutionMode.QuickSimAll, progressionOrNull: SeedFor(league, world));
            uint genesisGap = loop.TransfersSnapshot().ActiveWindow.CloseWorldDay - loop.State.Calendar.DayOfRound(0);

            PlayWholeSeason(loop);
            loop.RollToNextSeason();
            uint boundaryGap = loop.TransfersSnapshot().ActiveWindow.CloseWorldDay - loop.State.Calendar.DayOfRound(0);

            Assert.That(genesisGap, Is.EqualTo(InSeasonTail));
            Assert.That(boundaryGap, Is.EqualTo(InSeasonTail), "one derivation at genesis and at every boundary");
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
            // T-TX-NEU-001: a wired career that never bids is byte-identical to an unwired one in EVERYTHING
            // but the #31 block. The path under test is the only #31 code a no-action career runs: genesis
            // seeding (NewCareer) and the roll's staged step (d″). So the comparison covers a played season,
            // the roll, and one played round of season 2 — across #40, #28, #44, #29, #41, #30's appearance
            // rows and the season state (Snapshot minus the transfers block).
            Rig wired = NewCareer();
            Rig unwired = NewCareer(transfersUnwired: true);
            Assert.That(Snapshot(wired, includeTransfers: false), Is.EqualTo(Snapshot(unwired, includeTransfers: false)),
                "precondition: genesis seeding alone perturbs nothing outside #31");

            foreach (Rig rig in new[] { wired, unwired })
            {
                PlayWholeSeason(rig.Loop);
                rig.Loop.RollToNextSeason();
                rig.Loop.AdvanceToNextFixtureDay();
                rig.Loop.AdvanceAndPlayNextRound();
            }

            Assert.That(wired.Loop.State.SeasonNumber, Is.EqualTo(1), "the roll really ran on the wired rig");
            Assert.That(wired.Loop.TransfersSnapshot().ActiveWindow.OpenWorldDay, Is.GreaterThan(0u),
                "and its (d″) step really ran (the window was redrawn)");
            Assert.That(Snapshot(wired, includeTransfers: false), Is.EqualTo(Snapshot(unwired, includeTransfers: false)));
            Assert.That(wired.Progression.Snapshot(), Is.EqualTo(unwired.Progression.Snapshot()));
        }

        [Test]
        public void OneAcceptedSale_MovesTheProgressionSnapshot_SoTheNeutralityComparisonIsNotVacuous()
        {
            // T-TX-NEU-001 non-vacuity: the comparison above has teeth — one accepted transfer through the
            // same wired rig makes it fail, because a sale always re-homes a #28 record. (A "transfer changes
            // the played season" leg is not used: the quick-sim's coarse rating model ignores any single sale;
            // transfer sensitivity of play is witnessed by the selected-player and ban-served tests.)
            Rig sale = NewCareer();
            Rig unwired = NewCareer(transfersUnwired: true);
            Assert.That(sale.Progression.Snapshot(), Is.EqualTo(unwired.Progression.Snapshot()), "precondition");
            int target = PlayerAt(sale, Managed, PlayerPosition.Defender);

            Assert.That(sale.Loop.SubmitTransferBid(new Offer(target, Seller, ValueFor(sale, target, buy: false), 0L, 1, false)),
                Is.EqualTo(TransferSubmissionOutcome.Accepted));

            Assert.That(sale.Progression.Snapshot(), Is.Not.EqualTo(unwired.Progression.Snapshot()));
            Assert.That(Snapshot(sale, includeTransfers: false), Is.Not.EqualTo(Snapshot(unwired, includeTransfers: false)));
        }

        [Test]
        public void TwoRunsOverIdenticalTransferActivity_ProduceByteIdenticalState()
        {
            // T-TX-DET-003 (FR-TX-027 MUST): the same bids on the same career, then a played season and a
            // roll, leave identical #31 bytes, identical #40 entries and an identical #28 store — and the
            // whole career with them.
            Rig a = NewCareer(forceManagedRetiree: true);
            Rig b = NewCareer(forceManagedRetiree: true);
            RunTransferScript(a);
            RunTransferScript(b);
            Rig idle = NewCareer(forceManagedRetiree: true);
            PlayWholeSeason(idle.Loop);
            idle.Loop.RollToNextSeason();

            Assert.That(TransfersSaveCodec.Encode(a.Loop.TransfersSnapshot()), Is.EqualTo(TransfersSaveCodec.Encode(b.Loop.TransfersSnapshot())));
            Assert.That(ClubFinancesSaveCodec.Encode(a.Loop.FinanceEntriesForSave()), Is.EqualTo(ClubFinancesSaveCodec.Encode(b.Loop.FinanceEntriesForSave())));
            Assert.That(a.Progression.Snapshot(), Is.EqualTo(b.Progression.Snapshot()));
            Assert.That(Snapshot(a), Is.EqualTo(Snapshot(b)));

            // Non-vacuity: the script really moved state — the same career without it ends elsewhere in #31,
            // #40 and #28.
            Assert.That(TransfersSaveCodec.Encode(a.Loop.TransfersSnapshot()), Is.Not.EqualTo(TransfersSaveCodec.Encode(idle.Loop.TransfersSnapshot())));
            Assert.That(ClubFinancesSaveCodec.Encode(a.Loop.FinanceEntriesForSave()), Is.Not.EqualTo(ClubFinancesSaveCodec.Encode(idle.Loop.FinanceEntriesForSave())));
            Assert.That(a.Progression.Snapshot(), Is.Not.EqualTo(idle.Progression.Snapshot()));
        }

        // ── the production composition: League.CreateLoop → sale → roll → Save → Load → Restore → play ──────

        [Test]
        public void ASaleThroughCreateLoop_SurvivesTheRoll_ASaveLoadCycle_AndThePlayedRoundAfterIt()
        {
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(1, WorldSeed);
            ProgressionEngine progression = SeedFor(league, world);
            var career = PlayerCareerStates.ForLeague(new ProgressionSquads(progression), ClubIds(), injuryOccurrenceEnabled: false);
            SeasonLoop loop = league.CreateLoop(
                world, Managed, RoundResolutionMode.QuickSimAll,
                careerOrNull: career, progressionOrNull: progression, disciplineOrNull: new DisciplineState());

            // The genesis budget is 0, so a SALE: the youngest managed defender (young, so he cannot retire at
            // the boundary and vanish from the assertions below).
            Squad managed = progression.SquadFor(Managed);
            int sold = -1;
            int youngest = int.MaxValue;
            for (int i = 0; i < managed.Count; i++)
            {
                PlayerRecord p = managed.GetPlayer(i);
                if (p.Position == PlayerPosition.Defender && p.Age < youngest)
                {
                    youngest = p.Age;
                    sold = p.PlayerId;
                }
            }

            Assert.That(sold, Is.GreaterThanOrEqualTo(0));
            long fee = ValueFor(progression, sold, buy: false, buyer: Seller);
            long balanceBefore = loop.FinanceView(Managed).Balance;
            Assert.That(loop.SubmitTransferBid(new Offer(sold, Seller, fee, 0L, 1, false)),
                Is.EqualTo(TransferSubmissionOutcome.Accepted));
            Assert.That(loop.FinanceView(Managed).Balance, Is.EqualTo(balanceBefore + fee));

            PlayWholeSeason(loop);
            loop.RollToNextSeason();
            string path = Path.Combine(_tempDir, "create-loop.season");
            SeasonSaveManager.Save(loop, matchOrNull: null, path);

            SeasonSaveContents loaded = SeasonSaveManager.Load(path);
            var restoredCareer = PlayerCareerStates.FromBlocks(
                loaded.TrainingClubs, loaded.MedicalClubs, loaded.AppearanceClubs, injuryOccurrenceEnabled: false);
            SeasonLoop resumed = SeasonLoop.Restore(
                loaded.World, SeasonStateCodec.Encode(loaded.Season), RoundResolutionMode.QuickSimAll,
                restoredCareer, null, loaded.Progression, loaded.Discipline, loaded.Finances, loaded.Transfers);

            Assert.That(SeasonStateCodec.Encode(resumed.State), Is.EqualTo(SeasonStateCodec.Encode(loop.State)));
            Assert.That(IndexOf(loaded.Progression.SquadFor(Seller), sold), Is.GreaterThanOrEqualTo(0), "he still plays for the buyer");
            Assert.That(IndexOf(loaded.Progression.SquadFor(Managed), sold), Is.LessThan(0));
            Assert.That(resumed.TransfersSnapshot().TryGetContract(sold, out Contract _), Is.False, "his contract left with him");
            Assert.That(resumed.TransfersSnapshot().ContractCount, Is.EqualTo(loop.TransfersSnapshot().ContractCount));
            Assert.That(restoredCareer.TrainingView(Seller, sold).Focus, Is.EqualTo(TrainingFocus.Balanced), "his #29 row is at the buyer");
            Assert.Throws<ArgumentException>(() => restoredCareer.TrainingView(Managed, sold));

            resumed.AdvanceToNextFixtureDay();
            MatchResult[] played = resumed.AdvanceAndPlayNextRound();

            Assert.That(played.Length, Is.EqualTo(ClubCount / 2), "the restored career plays a round with the moved roster");
        }

        // ── fixture ──────────────────────────────────────────────────────────────────

        private sealed class Rig
        {
            internal SeasonLoop Loop;
            internal ProgressionEngine Progression;
            internal PlayerCareerStates Career_;
            internal DisciplineState Discipline;
            internal int ForcedRetiree = -1;
            internal int SellerRetiree = -1;

            internal PlayerCareerStates Career => Career_;
        }

        // bannedIndex: the squad index (in bannedClub) carrying the unserved #44 ban (-1 = a clean league).
        // sellerRetireeIndex: a seller-club squad index to age to 45, flagged on the first lived day (-1 = none).
        private static Rig NewCareer(
            long budget = Budget, TransfersState transfers = null, bool transfersUnwired = false, bool forceManagedRetiree = false,
            int bannedIndex = BannedIndex, int sellerRetireeIndex = -1, int bannedClub = Seller,
            int clubCount = ClubCount, int bannedYellows = 0)
        {
            League league = LeagueBootstrap.Generate(WorldSeed, clubCount);
            var world = new WorldStore(1, WorldSeed);
            SeasonState season = league.CreateSeason(managedClubId: Managed);
            var squads = new Squad[clubCount];
            for (int club = 0; club < clubCount; club++)
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

            int sellerRetiree = -1;
            if (sellerRetireeIndex >= 0)
            {
                var players = new PlayerRecord[squads[Seller].Count];
                for (int i = 0; i < players.Length; i++)
                {
                    players[i] = squads[Seller].GetPlayer(i);
                }

                players[sellerRetireeIndex].Age = 45;
                sellerRetiree = players[sellerRetireeIndex].PlayerId;
                squads[Seller] = new Squad(Seller, players);
            }

            ProgressionEngine progression = ProgressionEngine.SeedFrom(squads, world.CurrentWorldTick);
            var career = PlayerCareerStates.ForLeague(new ProgressionSquads(progression), ClubIds(clubCount), injuryOccurrenceEnabled: false);

            // One unserved ban (and, optionally, a yellow-card count) on a seller-club player, to prove a #44
            // tally follows a transfer.
            DisciplineState discipline = bannedIndex < 0
                ? DisciplineState.FromEntries(new DisciplineEntry[0])
                : DisciplineState.FromEntries(new[]
                {
                    new DisciplineEntry(
                        squads[bannedClub].GetPlayer(bannedIndex).PlayerId, DisciplineConstants.LeagueCompetitionKey, bannedYellows, BanMatches)
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

                transfers = TransfersState.CreateAtGenesis(
                    Managed, TransferWindow.SummerWindowFor(world.CurrentWorldTick, season.Calendar.DayOfRound(0)), ids);
            }

            var loop = new SeasonLoop(
                world, season, RoundResolutionMode.QuickSimAll, career, null, progression, discipline, finances,
                transfersUnwired ? null : transfers);
            return new Rig
            {
                Loop = loop, Progression = progression, Career_ = career, Discipline = discipline, ForcedRetiree = forced,
                SellerRetiree = sellerRetiree
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

        // Whether the club has a fixture in the round (an odd-N league gives one club a bye each round).
        private static bool ClubPlaysIn(SeasonState state, int roundIndex, int clubId)
        {
            for (int i = 0; i < state.FixtureCount; i++)
            {
                Fixture f = state.FixtureAt(i);
                if (f.RoundIndex == roundIndex && f.Involves(clubId))
                {
                    return true;
                }
            }

            return false;
        }

        // #31's own counterparty valuation, re-derived from the roster the same way SubmitBid does.
        private static long ValueFor(Rig c, int playerId, bool buy, int buyer = Seller) =>
            ValueFor(c.Progression, playerId, buy, buyer);

        private static long ValueFor(ProgressionEngine progression, int playerId, bool buy, int buyer = Seller)
        {
            Assert.That(progression.TryFindPlayer(playerId, out PlayerRecord rec, out int owner), Is.True);
            int counterparty = buy ? owner : buyer;
            Squad squad = progression.SquadFor(counterparty);
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

        // One fixed stretch of transfer activity: an accepted buy, a refused counter-offer, an accepted sale, a
        // played season and the roll. Every pick is a function of the rig, so two fresh rigs replay it exactly.
        private static void RunTransferScript(Rig c)
        {
            int bought = PlayerAt(c, Seller, PlayerPosition.Midfielder);
            Assert.That(c.Loop.SubmitTransferBid(new Offer(bought, Seller, ValueFor(c, bought, buy: true), 1_500L, 2, true)),
                Is.EqualTo(TransferSubmissionOutcome.Accepted));
            int forward = PlayerAt(c, 1, PlayerPosition.Forward);
            Assert.That(c.Loop.SubmitTransferBid(new Offer(forward, 1, ValueFor(c, forward, buy: true) - 1L, 0L, 1, true)),
                Is.EqualTo(TransferSubmissionOutcome.CounterOffered));
            int sold = PlayerAt(c, Managed, PlayerPosition.Defender);
            Assert.That(c.Loop.SubmitTransferBid(new Offer(sold, Seller, ValueFor(c, sold, buy: false), 0L, 1, false)),
                Is.EqualTo(TransferSubmissionOutcome.Accepted));
            PlayWholeSeason(c.Loop);
            c.Loop.RollToNextSeason();
        }

        private static void AssertNoMutation(Rig c, Offer offer, TransferSubmissionOutcome expected)
        {
            byte[] before = Snapshot(c);

            Assert.That(c.Loop.SubmitTransferBid(offer), Is.EqualTo(expected));
            Assert.That(Snapshot(c), Is.EqualTo(before), expected + " must leave #40, #28, #31 and the career untouched");
        }

        // Everything a refused or non-accepted action must leave untouched: #40, #28, #31, #44, #29, #41, #30's
        // appearance rows and the #30 season state itself. includeTransfers: false drops ONLY the #31 block —
        // the comparison a wired career must win against an unwired one (T-TX-NEU-001), whose #31 block is
        // legitimately absent.
        private static byte[] Snapshot(Rig c, bool includeTransfers = true)
        {
            using (var ms = new MemoryStream())
            {
                Append(ms, ClubFinancesSaveCodec.Encode(c.Loop.FinanceEntriesForSave()));
                Append(ms, c.Progression.Snapshot());
                if (includeTransfers)
                {
                    Append(ms, TransfersSaveCodec.Encode(c.Loop.TransfersSnapshot()));
                }

                Append(ms, DisciplineSaveCodec.Encode(c.Discipline));
                Append(ms, TrainingSaveCodec.Encode(c.Career.TrainingBlocks()));
                Append(ms, MedicalSaveCodec.Encode(c.Career.MedicalBlocks()));
                Append(ms, AppearanceSaveCodec.Encode(c.Career.AppearanceBlocks()));
                Append(ms, SeasonStateCodec.Encode(c.Loop.State));
                return ms.ToArray();
            }
        }

        // The roster authority and the career rows it must stay in lockstep with: the adapter's whole surface.
        private static byte[] RosterSnapshot(ProgressionEngine progression, PlayerCareerStates career)
        {
            using (var ms = new MemoryStream())
            {
                Append(ms, progression.Snapshot());
                Append(ms, TrainingSaveCodec.Encode(career.TrainingBlocks()));
                Append(ms, MedicalSaveCodec.Encode(career.MedicalBlocks()));
                Append(ms, AppearanceSaveCodec.Encode(career.AppearanceBlocks()));
                return ms.ToArray();
            }
        }

        private static void Append(MemoryStream ms, byte[] bytes) => ms.Write(bytes, 0, bytes.Length);

        // One club's live #40 entry, encoded through the #40 codec.
        private static byte[] LedgerOf(Rig c, int clubId)
        {
            foreach (ClubFinanceEntry entry in c.Loop.FinanceEntriesForSave())
            {
                if (entry.ClubId == clubId)
                {
                    return ClubFinancesSaveCodec.Encode(new[] { entry });
                }
            }

            throw new ArgumentException("no #40 entry for club " + clubId);
        }

        private static SeasonTransferRoster AdapterOf(Rig c) => new SeasonTransferRoster(c.Progression, c.Career);

        // A career whose rows already have the player at `to`: a lockstep violation against any store that
        // still has him at `from`, built with no test-only hook.
        private static PlayerCareerStates CareerWhereAlreadyMoved(int playerId, int from, int to)
        {
            Rig other = NewCareer();
            other.Progression.MovePlayer(playerId, from, to);
            return PlayerCareerStates.ForLeague(new ProgressionSquads(other.Progression), ClubIds(), injuryOccurrenceEnabled: false);
        }

        // The index (in the league's managed squad) of a managed starter who, once re-homed to the seller club,
        // is in THAT club's own starting eleven: found by asking the engine's selector, never assumed.
        private static int SelectableManagedIndex(int clubCount = ClubCount)
        {
            Squad managedSquad = LeagueBootstrap.Generate(WorldSeed, clubCount).ResolveByClubId(Managed);
            int[] starters = SquadRating.StartingElevenPlayerIds(NewCareer(clubCount: clubCount).Progression.SquadFor(Managed));
            for (int i = 0; i < starters.Length; i++)
            {
                Rig probe = NewCareer(clubCount: clubCount);
                probe.Progression.MovePlayer(starters[i], Managed, Seller);
                if (Array.IndexOf(SquadRating.StartingElevenPlayerIds(probe.Progression.SquadFor(Seller)), starters[i]) >= 0)
                {
                    return IndexOf(managedSquad, starters[i]);
                }
            }

            Assert.Fail("no managed starter would make the seller club's eleven; the fixture has lost its subject");
            return -1;
        }

        private static int CountAt(Squad squad, PlayerPosition position)
        {
            int count = 0;
            for (int i = 0; i < squad.Count; i++)
            {
                count += squad.GetPlayer(i).Position == position ? 1 : 0;
            }

            return count;
        }

        private static PlayerLifecycle LifecycleOf(ProgressionEngine progression, int club, int playerId)
        {
            foreach (ClubCareerStates block in progression.ToBlocks())
            {
                if (block.ClubId != club)
                {
                    continue;
                }

                for (int i = 0; i < block.Count; i++)
                {
                    if (block.Records[i].PlayerId == playerId)
                    {
                        return block.Lifecycles[i];
                    }
                }
            }

            throw new ArgumentException("not carried");
        }

        private static TrainingState TrainingOf(PlayerCareerStates career, int club, int playerId)
        {
            foreach (ClubTrainingStates block in career.TrainingBlocks())
            {
                int at = block.ClubId == club ? Array.IndexOf(block.PlayerIds, playerId) : -1;
                if (at >= 0)
                {
                    return block.States[at];
                }
            }

            throw new ArgumentException("not carried");
        }

        private static AppearanceState AppearanceOf(PlayerCareerStates career, int club, int playerId)
        {
            foreach (ClubAppearanceStates block in career.AppearanceBlocks())
            {
                int at = block.ClubId == club ? Array.IndexOf(block.PlayerIds, playerId) : -1;
                if (at >= 0)
                {
                    return block.States[at];
                }
            }

            throw new ArgumentException("not carried");
        }

        private static bool AppearanceHas(PlayerCareerStates career, int club, int playerId)
        {
            foreach (ClubAppearanceStates block in career.AppearanceBlocks())
            {
                if (block.ClubId == club && Array.IndexOf(block.PlayerIds, playerId) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        // Fielded on `day`: the appearance record's bit 0 is anchored to that very day.
        private static bool FieldedOn(PlayerCareerStates career, int club, int playerId, uint day)
        {
            AppearanceState row = AppearanceOf(career, club, playerId);
            return row.BitsAsOfWorldDay == day && (row.RecentBits & 1u) != 0u;
        }

        private static void AssertSameLifecycle(PlayerLifecycle expected, PlayerLifecycle actual)
        {
            Assert.That(actual.PotentialAbility, Is.EqualTo(expected.PotentialAbility), "PA");
            Assert.That(actual.CurrentAbility, Is.EqualTo(expected.CurrentAbility), "CA");
            Assert.That(actual.GrowthCursor, Is.EqualTo(expected.GrowthCursor), "GrowthCursor");
            Assert.That(actual.BirthWorldDay, Is.EqualTo(expected.BirthWorldDay), "BirthWorldDay");
            Assert.That(actual.RetirementFlag, Is.EqualTo(expected.RetirementFlag), "RetirementFlag");
            Assert.That(actual.RetirementDay, Is.EqualTo(expected.RetirementDay), "RetirementDay");
            Assert.That(actual.LastAdvancedWorldDay, Is.EqualTo(expected.LastAdvancedWorldDay), "LastAdvancedWorldDay");
        }

        private static void AssertSameTraining(TrainingState expected, TrainingState actual)
        {
            Assert.That(actual.Focus, Is.EqualTo(expected.Focus), "Focus");
            Assert.That(actual.Condition, Is.EqualTo(expected.Condition), "Condition");
            Assert.That(actual.TrainingFatigue, Is.EqualTo(expected.TrainingFatigue), "TrainingFatigue");
            Assert.That(actual.LastAdvancedWorldDay, Is.EqualTo(expected.LastAdvancedWorldDay), "LastAdvancedWorldDay");
        }

        private static void AssertSameRecord(PlayerRecord expected, int[] expectedAttributes, PlayerRecord actual)
        {
            Assert.That(actual.PlayerId, Is.EqualTo(expected.PlayerId), "PlayerId");
            Assert.That(actual.FirstName, Is.EqualTo(expected.FirstName), "FirstName");
            Assert.That(actual.LastName, Is.EqualTo(expected.LastName), "LastName");
            Assert.That(actual.Age, Is.EqualTo(expected.Age), "Age");
            Assert.That(actual.Position, Is.EqualTo(expected.Position), "Position");
            int[] after = actual.Attributes.ToArray();
            Assert.That(after.Length, Is.EqualTo(expectedAttributes.Length), "attribute count");
            for (int i = 0; i < after.Length; i++)
            {
                Assert.That(after[i], Is.EqualTo(expectedAttributes[i]), "Attributes[" + i + "]");
            }
        }

        private static void AssertSameInjury(InjuryState expected, InjuryState actual)
        {
            Assert.That(actual.Severity, Is.EqualTo(expected.Severity), "Severity");
            Assert.That(actual.RecoveryRemaining, Is.EqualTo(expected.RecoveryRemaining), "RecoveryRemaining");
            Assert.That(actual.InjuryCount, Is.EqualTo(expected.InjuryCount), "InjuryCount");
            Assert.That(actual.LastAdvancedWorldDay, Is.EqualTo(expected.LastAdvancedWorldDay), "LastAdvancedWorldDay");
        }

        private static void AssertSameAppearance(AppearanceState expected, AppearanceState actual)
        {
            Assert.That(actual.RecentBits, Is.EqualTo(expected.RecentBits), "RecentBits");
            Assert.That(actual.BitsAsOfWorldDay, Is.EqualTo(expected.BitsAsOfWorldDay), "BitsAsOfWorldDay");
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

        private static int[] ClubIds(int clubCount = ClubCount)
        {
            var ids = new int[clubCount];
            for (int c = 0; c < clubCount; c++)
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
// | 1.1     | 2026-10-20 | —      | ERR-031-011 (AR round 2 M6): roll-timing-independent window close — prompt vs late roll, genesis vs boundary gap, prompt roll covers the opening fixture; rig/genesis/roll locks use SummerWindowFor. |
// | 1.2     | 2026-10-20 | —      | AR round 2: M7 (direct SeasonTransferRoster throw-path tests; REKEY-005 lifecycle/#29/#30 rows + buyer selection; depletion through AssertNoMutation; wider Snapshot), the ERR-044-014 ban property replacing the vacuous HasEntry, a non-vacuous encoded-season leg, the League.CreateLoop sale/roll/save/load/play composition, L3 CanRelease throws (ERR-031-012). |
// | 1.3     | 2026-10-20 | —      | AR round 3: M16 AnAcceptedTransfer_LeavesTheCounterpartyClubsLedgerUntouched_ByDesign (pins the KD-2 decision); M14 NoTransferAction_IsBehaviourNeutral rewritten (played season → RollToNextSeason → one season-2 round; the full Snapshot minus the #31 block, wired vs unwired) + OneAcceptedSale_… non-vacuity; M15 TwoRunsOverIdenticalTransferActivity_… (T-TX-DET-003); S-L3 the ban test runs a 3-club (bye) league and asserts the decrement lands only in rounds the holding club plays; T-L9 REKEY-005 compares the full InjuryState and a yellow-count tally; S-L5 CreateLoop KD-4 refusal test. |
// | 1.4     | 2026-10-20 | —      | AR round 4: L9 T-TX-REKEY-005 asserts the #28 PlayerRecord (id, names, age, position, every attribute) is unchanged by the re-home; S4-L2 the unwired CreateLoop KD-4 refusal asserts ParamName == "world"; R4-L2 the roll-window non-vacuity leg is an Assume.That. |
#endregion
