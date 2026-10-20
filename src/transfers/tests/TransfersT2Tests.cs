// ============================================================================
// File:     src/transfers/tests/TransfersT2Tests.cs
// Created:  2026-10-20
// Modified: 2026-10-20 (AR round 3 — v1.3)
// Author:   —
// Specs:    Spec #31 §3.5 (as corrected by ERR-031-008 and ERR-031-011), §3.7, §3.8, FR-TX-007/019/028, G14;
//           §5.5 T-TX-DET-002, T-TX-INIT-001
// Purpose:  Locks #31 T2's pure state operations: genesis seeding at the [GT] terms, the season-boundary
//           aging with the retiree/regen lockstep and the spend/window reset, and the summer-window
//           derivation (begin-day open, opening-fixture-anchored inclusive close).
// ============================================================================

using System;

using NUnit.Framework;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.Transfers.Tests
{
    [TestFixture]
    public sealed class TransfersT2Tests
    {
        private const int Managed = 3;

        [Test]
        public void SummerWindowFor_OpensOnTheBeginDay_AndClosesInclusiveFromTheOpeningFixtureDay()
        {
            // §3.5 worked example (ERR-031-011): season begun on day 267, opening fixture on day 322.
            TransferWindow w = TransferWindow.SummerWindowFor(267, 322);

            Assert.AreEqual(267u, w.OpenWorldDay);
            Assert.AreEqual(322u + (uint)TransfersConstants.SummerWindowInSeasonDays - 1u, w.CloseWorldDay);
            int openDaysFromOpeningFixture = 0;
            for (uint d = 270; d < 500; d++)
            {
                if (d >= 322 && w.IsWindowOpen(d))
                {
                    openDaysFromOpeningFixture++;
                }
            }

            Assert.AreEqual(TransfersConstants.SummerWindowInSeasonDays, openDaysFromOpeningFixture,
                "inclusive: exactly N open days from the opening fixture day on, not N + 1");
            Assert.IsFalse(w.IsWindowOpen(266u), "nothing transacts before the season begins");
        }

        [Test]
        public void SummerWindowFor_GenesisDefaults_AreDays0Through44()
        {
            // Pinned against the shipped defaults: 38 in-season days from season 1's opening fixture day 7
            // (LeagueBootstrapConstants.FirstRoundDay, which this assembly cannot reference) give the genesis
            // window [0, 44] the roll-day-length derivation also produced, so genesis bytes are unchanged. A
            // tuned [GT] is not a failure of this lock, so the test is skipped (inconclusive) under one rather
            // than asserting a formula it would only be restating.
            Assume.That(TransfersConstants.SummerWindowInSeasonDays, Is.EqualTo(38), "the [GT] default this pin is written against");
            TransferWindow w = TransferWindow.SummerWindowFor(0, 7);

            Assert.AreEqual(0u, w.OpenWorldDay);
            Assert.AreEqual(44u, w.CloseWorldDay);
        }

        [Test]
        public void SummerWindowFor_OpeningOnTheBeginDay_IsLegal()
        {
            TransferWindow w = TransferWindow.SummerWindowFor(50, 50);

            Assert.AreEqual(50u, w.OpenWorldDay);
            Assert.AreEqual(50u + (uint)TransfersConstants.SummerWindowInSeasonDays - 1u, w.CloseWorldDay);
        }

        [Test]
        public void SummerWindowFor_OpeningBeforeTheBeginDay_FailsLoud()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TransferWindow.SummerWindowFor(100, 99));
        }

        [Test]
        public void SummerWindowFor_PastTheLastWorldDay_FailsLoud()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TransferWindow.SummerWindowFor(0, uint.MaxValue));

            // The last opening day whose close still fits succeeds.
            uint lastFitting = uint.MaxValue - (uint)TransfersConstants.SummerWindowInSeasonDays + 1u;
            TransferWindow w = TransferWindow.SummerWindowFor(0, lastFitting);
            Assert.AreEqual(uint.MaxValue, w.CloseWorldDay);
            Assert.Throws<ArgumentOutOfRangeException>(() => TransferWindow.SummerWindowFor(0, lastFitting + 1u));
        }

        [Test]
        public void CreateAtGenesis_SeedsOneContractPerPlayer_AtTheGenesisTerms()
        {
            TransfersState s = TransfersState.CreateAtGenesis(Managed, TransferWindow.SummerWindowFor(0, 7), new[] { 75, 77, 76 });

            Assert.AreEqual(Managed, s.ManagedClubId);
            Assert.AreEqual(0L, s.CommittedSpendThisWindow);
            Contract[] c = s.ContractsSnapshot();
            Assert.AreEqual(3, c.Length);
            for (int i = 0; i < c.Length; i++)
            {
                Assert.AreEqual(75 + i, c[i].PlayerId);
                Assert.AreEqual((long)TransfersConstants.DefaultWagePerPeriod, c[i].WagePerPeriod);
                Assert.AreEqual(TransfersConstants.DefaultContractSeasons, c[i].LengthSeasons);
            }
        }

        [Test]
        public void PrepareSeasonBoundary_AgesRemovesExpiredAndRetired_SeedsArrivals_AndResetsSpendAndWindow()
        {
            TransfersState s = TransfersState.Restore(
                Managed, 9_999, TransferWindow.SummerWindowFor(0, 7),
                new[]
                {
                    new Contract(75, 500, 1),    // expires: would reach 0
                    new Contract(76, 600, 3),    // ages to 2
                    new Contract(77, 700, 2),    // retires this boundary
                });
            TransferWindow next = TransferWindow.SummerWindowFor(300, 355);

            TransfersState after = s.PrepareSeasonBoundary(new[] { 77, 4242 }, new[] { 900 }, in next);

            Contract[] c = after.ContractsSnapshot();
            Assert.AreEqual(2, c.Length);
            Assert.AreEqual(76, c[0].PlayerId);
            Assert.AreEqual(2, c[0].LengthSeasons, "aged one season");
            Assert.AreEqual(600L, c[0].WagePerPeriod);
            Assert.AreEqual(900, c[1].PlayerId, "the regen joining the managed club is contracted (G14)");
            Assert.AreEqual(TransfersConstants.DefaultContractSeasons, c[1].LengthSeasons, "fresh, not aged");
            Assert.AreEqual(0L, after.CommittedSpendThisWindow, "FR-TX-007: spend resets at the boundary");
            Assert.AreEqual(300u, after.ActiveWindow.OpenWorldDay);

            Assert.AreEqual(3, s.ContractCount, "pure: the source state is untouched");
            Assert.AreEqual(9_999L, s.CommittedSpendThisWindow);
        }

        [Test]
        public void PrepareSeasonBoundary_AnArrivalThatAlreadyHoldsAContract_FailsLoud_EvenOneExpiringNow()
        {
            TransferWindow next = TransferWindow.SummerWindowFor(300, 355);
            TransfersState s = TransfersState.Restore(
                Managed, 0, TransferWindow.SummerWindowFor(0, 7),
                new[] { new Contract(76, 600, 3), new Contract(77, 700, 1) });   // 77 would expire at this boundary

            Assert.Throws<ArgumentException>(
                () => s.PrepareSeasonBoundary(Array.Empty<int>(), new[] { 76 }, in next), "a live contract");
            Assert.Throws<ArgumentException>(
                () => s.PrepareSeasonBoundary(Array.Empty<int>(), new[] { 77 }, in next),
                "L4: a contract expiring this boundary must not be silently replaced by a fresh genesis one");
            Assert.AreEqual(2, s.ContractCount, "pure: a refused boundary leaves the source untouched");
        }

        [Test]
        public void PrepareSeasonBoundary_AnArrivalThatIsAlsoRetired_FailsLoud()
        {
            TransferWindow next = TransferWindow.SummerWindowFor(300, 355);
            TransfersState s = TransfersState.Restore(
                Managed, 0, TransferWindow.SummerWindowFor(0, 7), new[] { new Contract(76, 600, 3) });

            Assert.Throws<ArgumentException>(
                () => s.PrepareSeasonBoundary(new[] { 900 }, new[] { 900 }, in next),
                "L4: a regen id is new — it can neither retire in the same boundary nor already be held");
        }

        [Test]
        public void PrepareSeasonBoundary_ADuplicatedArrival_FailsLoudAsAnArgumentException()
        {
            // AR round 3 T-L8: a repeated id among the arrivals is an input defect, refused in the pre-build
            // guard — never an InvalidOperationException leaking from the new state's InsertContract.
            TransferWindow next = TransferWindow.SummerWindowFor(300, 355);
            TransfersState s = TransfersState.Restore(
                Managed, 0, TransferWindow.SummerWindowFor(0, 7), new[] { new Contract(76, 600, 3) });

            Assert.Throws<ArgumentException>(() => s.PrepareSeasonBoundary(Array.Empty<int>(), new[] { 900, 901, 900 }, in next));
            Assert.AreEqual(1, s.ContractCount, "pure: a refused boundary leaves the source untouched");
        }

        [Test]
        public void PrepareSeasonBoundary_AgedContractsPlusArrivalsAboveTheSquadCap_FailsLoudAsAnArgumentException()
        {
            // AR round 3 T-L8: the surviving aged contracts plus the arrivals are checked against
            // CLUB_SQUAD_SIZE before anything is built; the cap itself is legal.
            int cap = PlayerDatabaseConstants.CLUB_SQUAD_SIZE;
            var held = new Contract[cap - 2];
            for (int i = 0; i < held.Length; i++)
            {
                held[i] = new Contract(100 + i, 600, 3);   // every one survives the boundary
            }

            TransfersState s = TransfersState.Restore(Managed, 0, TransferWindow.SummerWindowFor(0, 7), held);
            TransferWindow next = TransferWindow.SummerWindowFor(300, 355);

            Assert.Throws<ArgumentException>(
                () => s.PrepareSeasonBoundary(Array.Empty<int>(), new[] { 900, 901, 902 }, in next), "cap + 1");
            Assert.AreEqual(cap - 2, s.ContractCount, "pure: a refused boundary leaves the source untouched");
            TransfersState atTheCap = s.PrepareSeasonBoundary(Array.Empty<int>(), new[] { 900, 901 }, in next);
            Assert.AreEqual(cap, atTheCap.ContractCount, "exactly the cap is legal");

            // Retirees and expiring contracts free room before the comparison, so they are not counted.
            TransfersState withRetiree = s.PrepareSeasonBoundary(new[] { 100 }, new[] { 900, 901, 902 }, in next);
            Assert.AreEqual(cap, withRetiree.ContractCount);
        }

        [Test]
        public void ThreeBoundaries_ExpireEveryGenesisContract_WithoutAThrow()
        {
            TransfersState s = TransfersState.CreateAtGenesis(Managed, TransferWindow.SummerWindowFor(0, 7), new[] { 75, 76 });
            for (int b = 0; b < TransfersConstants.DefaultContractSeasons; b++)
            {
                s = s.PrepareSeasonBoundary(Array.Empty<int>(), Array.Empty<int>(), TransferWindow.SummerWindowFor((uint)(300 * (b + 1)), (uint)(300 * (b + 1)) + 55u));
            }

            Assert.AreEqual(0, s.ContractCount, "every genesis contract has aged out — F7: never stored as 0");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-10-20 | —      | #31 T2: genesis seeding, boundary aging + retiree/regen lockstep + resets, window derivation locks. |
// | 1.1     | 2026-10-20 | —      | ERR-031-011 (AR round 2 M6): window locks rewritten for SummerWindowFor(begin, openingFixture) — inclusive close, genesis [0,44] pin, opening<begin and uint-overflow edges. |
// | 1.2     | 2026-10-20 | —      | AR round 2 L4: PrepareSeasonBoundary refuses an arrival that holds a contract (live or expiring this boundary) or is retired — ArgumentException, source untouched. |
// | 1.3     | 2026-10-20 | —      | AR round 3: T-L8 locks (a duplicated arrival and aged contracts plus arrivals above CLUB_SQUAD_SIZE are ArgumentExceptions, the cap itself is legal); T-L9: the genesis [0,44] pin is an unconditional assertion under Assume.That(SummerWindowInSeasonDays == 38) and no longer restates the formula. |
#endregion
