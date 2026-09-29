// ============================================================================
// File:     src/club-finances/tests/ClubFinancesT2BootstrapTests.cs
// Created:  2026-09-11
// Modified: 2026-09-11 (review follow-up — cite acceptance ids and delimit T2a's partial lifecycle coverage)
// Author:   —
// Specs:    Spec #20 §3.9.4 (general-test allocation carve-out)
//           Spec #40 FR-FN-002/025/027, §4.1-§4.3, §5.2-§5.3, §7.1 T2a
// Purpose:  Locks T2a's first real #27 consumer: one canonical initial finance entry per Squad.ClubId.
// ============================================================================
// §3.9.4 general-unit-test — allocation rules relaxed in test body

using System;

using NUnit.Framework;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.ClubFinances.Tests
{
    /// <summary>Regression locks for the T2a finance bootstrap over canonical #27 squads.</summary>
    [TestFixture]
    public sealed class ClubFinancesT2BootstrapTests
    {
        private static Squad MakeSquad(int clubId)
        {
            return new Squad(
                clubId,
                new[] { PlayerRecord.CreateDefault(clubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE) });
        }

        /// <summary>T-FN-LIFE-001 bootstrap half + T-FN-NEU-002: every supplied ClubId gets one canonical identity entry.</summary>
        [Test]
        public void CreateInitialForSquads_CreatesCanonicalInitialEntryPerClub()
        {
            Squad[] squads = { MakeSquad(7), MakeSquad(3), MakeSquad(11) };

            ClubFinanceEntry[] entries = ClubFinanceEntry.CreateInitialForSquads(squads);

            Assert.That(entries.Length, Is.EqualTo(3));
            Assert.That(entries[0].ClubId, Is.EqualTo(3));
            Assert.That(entries[1].ClubId, Is.EqualTo(7));
            Assert.That(entries[2].ClubId, Is.EqualTo(11));

            for (int i = 0; i < entries.Length; i++)
            {
                Assert.That(entries[i].Finances.Balance, Is.EqualTo(ClubFinancesConstants.StartingClubBalance));
                Assert.That(entries[i].Finances.TransferBudget, Is.Zero);
                Assert.That(entries[i].Finances.WageBudget, Is.Zero);
                Assert.That(entries[i].Finances.WageBillAggregate, Is.Zero);
                Assert.That(entries[i].Finances.SeasonRevenueAccrued, Is.Zero);
                Assert.That(entries[i].Finances.FfpBalanceWindow, Is.Zero);
            }

            Assert.That(squads[0].ClubId, Is.EqualTo(7), "Bootstrap must not reorder the caller's squad array.");
            Assert.That(squads[1].ClubId, Is.EqualTo(3), "Bootstrap must not reorder the caller's squad array.");
        }

        /// <summary>T-FN-LIFE-001 bootstrap boundary: a missing club-universe input fails loud instead of creating implicit state.</summary>
        [Test]
        public void CreateInitialForSquads_NullArray_FailsLoud()
        {
            Assert.Throws<ArgumentNullException>(() => ClubFinanceEntry.CreateInitialForSquads(null));
        }

        /// <summary>T-FN-LIFE-001 bootstrap boundary: an empty club universe cannot masquerade as a bootstrapped game.</summary>
        [Test]
        public void CreateInitialForSquads_EmptyArray_FailsLoud()
        {
            Assert.Throws<ArgumentException>(
                () => ClubFinanceEntry.CreateInitialForSquads(Array.Empty<Squad>()));
        }

        /// <summary>T-FN-LIFE-001 bootstrap boundary: every bootstrap slot must carry a canonical #27 squad identity.</summary>
        [Test]
        public void CreateInitialForSquads_NullSquad_FailsLoud()
        {
            Assert.Throws<ArgumentException>(
                () => ClubFinanceEntry.CreateInitialForSquads(new[] { MakeSquad(2), null }));
        }

        /// <summary>T-FN-LIFE-001 bootstrap boundary: exactly one finance entry exists for each distinct ClubId.</summary>
        [Test]
        public void CreateInitialForSquads_DuplicateClubId_FailsLoud()
        {
            Assert.Throws<ArgumentException>(
                () => ClubFinanceEntry.CreateInitialForSquads(new[] { MakeSquad(4), MakeSquad(4) }));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                        |
// | 1.0     | 2026-09-11 | —      | T2a bootstrap factory: canonical ordering, identity values,  |
// |         |            |        | null/empty/null-element/duplicate ClubId failure gates.      |
// | 1.1     | 2026-09-11 | —      | Review follow-up: acceptance IDs cited; unnecessary unchecked helper arithmetic removed. |
// | 1.2     | 2026-09-11 | —      | T-FN-LIFE-001 citations explicitly identify T2a as its bootstrap half; across-roll half remains T2b. |
#endregion
