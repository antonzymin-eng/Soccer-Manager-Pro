// ============================================================================
// File:     src/transfers/tests/TransfersT2Tests.cs
// Created:  2026-10-20
// Modified: 2026-10-20
// Author:   —
// Specs:    Spec #31 §3.5 (as corrected by ERR-031-008), §3.7, §3.8, FR-TX-007/019/028, G14;
//           §5.5 T-TX-DET-002, T-TX-INIT-001
// Purpose:  Locks #31 T2's pure state operations: genesis seeding at the [GT] terms, the season-boundary
//           aging with the retiree/regen lockstep and the spend/window reset, and the summer-window
//           derivation's inclusive length.
// ============================================================================

using System;

using NUnit.Framework;

namespace TacticalDirector.Transfers.Tests
{
    [TestFixture]
    public sealed class TransfersT2Tests
    {
        private const int Managed = 3;

        [Test]
        public void SummerWindowFrom_IsExactlyTheConfiguredLength_Inclusive()
        {
            TransferWindow w = TransferWindow.SummerWindowFrom(280);

            Assert.AreEqual(280u, w.OpenWorldDay);
            Assert.AreEqual(280u + (uint)TransfersConstants.SummerWindowLengthDays - 1u, w.CloseWorldDay);
            int openDays = 0;
            for (uint d = 270; d < 400; d++)
            {
                if (w.IsWindowOpen(d))
                {
                    openDays++;
                }
            }

            Assert.AreEqual(TransfersConstants.SummerWindowLengthDays, openDays, "ERR-031-008: N days, not N + 1");
        }

        [Test]
        public void SummerWindowFrom_PastTheLastWorldDay_FailsLoud()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TransferWindow.SummerWindowFrom(uint.MaxValue));
        }

        [Test]
        public void CreateAtGenesis_SeedsOneContractPerPlayer_AtTheGenesisTerms()
        {
            TransfersState s = TransfersState.CreateAtGenesis(Managed, TransferWindow.SummerWindowFrom(0), new[] { 75, 77, 76 });

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
                Managed, 9_999, TransferWindow.SummerWindowFrom(0),
                new[]
                {
                    new Contract(75, 500, 1),    // expires: would reach 0
                    new Contract(76, 600, 3),    // ages to 2
                    new Contract(77, 700, 2),    // retires this boundary
                });
            TransferWindow next = TransferWindow.SummerWindowFrom(300);

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
        public void PrepareSeasonBoundary_AnArrivalThatAlreadyHoldsAContract_FailsLoud()
        {
            TransfersState s = TransfersState.Restore(
                Managed, 0, TransferWindow.SummerWindowFrom(0), new[] { new Contract(76, 600, 3) });

            Assert.Throws<InvalidOperationException>(
                () => s.PrepareSeasonBoundary(Array.Empty<int>(), new[] { 76 }, TransferWindow.SummerWindowFrom(300)));
        }

        [Test]
        public void ThreeBoundaries_ExpireEveryGenesisContract_WithoutAThrow()
        {
            TransfersState s = TransfersState.CreateAtGenesis(Managed, TransferWindow.SummerWindowFrom(0), new[] { 75, 76 });
            for (int b = 0; b < TransfersConstants.DefaultContractSeasons; b++)
            {
                s = s.PrepareSeasonBoundary(Array.Empty<int>(), Array.Empty<int>(), TransferWindow.SummerWindowFrom((uint)(300 * (b + 1))));
            }

            Assert.AreEqual(0, s.ContractCount, "every genesis contract has aged out — F7: never stored as 0");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-10-20 | —      | #31 T2: genesis seeding, boundary aging + retiree/regen lockstep + resets, window derivation locks. |
#endregion
