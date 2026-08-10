// File:     src/player-progression/tests/RegenGeneratorTests.cs
// Created:  2026-07-24
// Modified: 2026-08-10
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §3.3 / §4.3; Deterministic Simulation #16 (RNG); Code Standards #20
// Purpose:  T-PG-REG-001/003 — regen determinism, the exact PROGRESSION_REGEN_FIELDS budget, generated
//           bounds, and the "attrs below PA / room to grow" contract. Registers the regen stream under
//           a documented test-local ordinal literal (KD-B — the production const lands at T2). Also
//           (M3, ERR-028-014 carryforward) that a generated regen never seeds the retired never-advanced
//           sentinel and that a block built from one is accepted by ProgressionEngine.FromBlocks.

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.PlayerProgression.Tests
{
    [TestFixture]
    public sealed class RegenGeneratorTests
    {
        private const string SiteId = "player-progression.regen";

        // = the T2 SubsystemOrdinals.PlayerProgression (#28 §4.3 / FR-PG-020); kept a local literal until
        // T2 registers the production stream (KD-B — no early deterministic-sim change in T0).
        private const int OrdinalPlayerProgression = 82;

        private const uint WorldDay = 100000;

        private static DeterministicRngService NewRng(ulong seed, int clubId, out int streamIndex)
        {
            var rng = new DeterministicRngService(seed);
            streamIndex = rng.RegisterStream(SiteId, OrdinalPlayerProgression, clubId, streamVersion: 1);
            return rng;
        }

        [Test]
        public void GenerateRegen_SameSeedAndClub_ProducesIdenticalRegen()
        {
            var rngA = NewRng(4242UL, clubId: 7, out int idxA);
            var rngB = NewRng(4242UL, clubId: 7, out int idxB);

            (PlayerRecord recA, PlayerLifecycle lifeA) = RegenGenerator.GenerateRegen(rngA, idxA, clubId: 7, newPlayerId: 631, WorldDay);
            (PlayerRecord recB, PlayerLifecycle lifeB) = RegenGenerator.GenerateRegen(rngB, idxB, clubId: 7, newPlayerId: 631, WorldDay);

            Assert.AreEqual(recA.FirstName, recB.FirstName);
            Assert.AreEqual(recA.LastName, recB.LastName);
            Assert.AreEqual(recA.Age, recB.Age);
            Assert.AreEqual(recA.Position, recB.Position);
            CollectionAssert.AreEqual(recA.Attributes.ToArray(), recB.Attributes.ToArray());
            Assert.AreEqual(recA.Attributes.WeakFootRating, recB.Attributes.WeakFootRating);
            Assert.AreEqual(lifeA.PotentialAbility, lifeB.PotentialAbility);
            Assert.AreEqual(lifeA.CurrentAbility, lifeB.CurrentAbility);
            Assert.AreEqual(lifeA.BirthWorldDay, lifeB.BirthWorldDay);
        }

        [Test]
        public void GenerateRegen_AdvancesCursorByExactlyTheReservationBudget()
        {
            var rng = NewRng(1UL, clubId: 0, out int idx);
            RegenGenerator.GenerateRegen(rng, idx, clubId: 0, newPlayerId: 100, WorldDay);

            Assert.AreEqual(
                (ulong)PlayerProgressionConstants.PROGRESSION_REGEN_FIELDS,
                rng.GetStreamState(idx).RngCursor,
                "one regen must consume exactly PROGRESSION_REGEN_FIELDS draws.");
        }

        [Test]
        public void GenerateRegen_UsesTheSuppliedFreshPlayerId()
        {
            var rng = NewRng(9UL, clubId: 3, out int idx);
            (PlayerRecord rec, _) = RegenGenerator.GenerateRegen(rng, idx, clubId: 3, newPlayerId: 999, WorldDay);

            Assert.AreEqual(999, rec.PlayerId, "the regen must take the caller-supplied fresh id (FR-PG-011).");
        }

        [Test]
        public void GenerateRegen_CurrentAbilityIsBelowPotential_WithRoomToGrow()
        {
            var rng = NewRng(55UL, clubId: 2, out int idx);
            (PlayerRecord rec, PlayerLifecycle life) = RegenGenerator.GenerateRegen(rng, idx, clubId: 2, newPlayerId: 55, WorldDay);

            Assert.AreEqual(
                AbilityModel.ComputeCA(in rec.Attributes, rec.Position),
                life.CurrentAbility,
                "CurrentAbility must be the recomputed CA of the generated attributes.");
            Assert.LessOrEqual(life.CurrentAbility, life.PotentialAbility, "a regen must have room to grow (§3.3).");
            Assert.GreaterOrEqual(life.PotentialAbility, PlayerProgressionConstants.PA_MIN);
            Assert.LessOrEqual(life.PotentialAbility, PlayerProgressionConstants.ABILITY_MAX);
            Assert.AreEqual(0L, life.GrowthCursor);
            Assert.IsFalse(life.RetirementFlag);
        }

        [Test]
        public void GenerateRegen_GeneratedValues_AreWithinDeclaredBounds()
        {
            var rng = NewRng(7UL, clubId: 4, out int idx);
            (PlayerRecord rec, PlayerLifecycle life) = RegenGenerator.GenerateRegen(rng, idx, clubId: 4, newPlayerId: 4, WorldDay);

            Assert.GreaterOrEqual(rec.Age, PlayerProgressionConstants.REGEN_AGE_MIN);
            Assert.LessOrEqual(rec.Age, PlayerProgressionConstants.REGEN_AGE_MAX);
            Assert.IsTrue(System.Enum.IsDefined(typeof(PlayerPosition), rec.Position));
            Assert.GreaterOrEqual(rec.Attributes.WeakFootRating, PlayerDatabaseConstants.WEAK_FOOT_MIN);
            Assert.LessOrEqual(rec.Attributes.WeakFootRating, PlayerDatabaseConstants.WEAK_FOOT_MAX);

            int[] attrs = rec.Attributes.ToArray();
            for (int i = 0; i < attrs.Length; i++)
            {
                Assert.GreaterOrEqual(attrs[i], PlayerProgressionConstants.ATTRIBUTE_MIN, $"attr {i}");
                Assert.LessOrEqual(attrs[i], PlayerProgressionConstants.ATTRIBUTE_MAX, $"attr {i}");
            }

            // BirthWorldDay anchors the derived age back to the generated age (KD-A).
            long ageDays = (long)WorldDay - life.BirthWorldDay;
            Assert.AreEqual(rec.Age, (int)(ageDays / PlayerProgressionConstants.DAYS_PER_YEAR),
                "the derived age from BirthWorldDay must equal the generated age.");
        }

        [Test]
        public void GenerateRegen_LastAdvancedWorldDay_EqualsWorldDayPassedIn()
        {
            // M3 (ERR-028-014 carryforward): a regen must not seed the never-advanced sentinel — it was
            // retired from the set of legal STORE states, and every store/codec boundary refuses it by
            // name. A regen describes the roster as of worldDay, so that is its anchor, exactly like a
            // seeded player.
            var rng = NewRng(21UL, clubId: 5, out int idx);
            (_, PlayerLifecycle life) = RegenGenerator.GenerateRegen(rng, idx, clubId: 5, newPlayerId: 21, WorldDay);

            Assert.AreEqual(WorldDay, life.LastAdvancedWorldDay,
                "a generated regen's cursor must anchor at the world day it was generated on.");
            Assert.AreNotEqual(PlayerProgressionConstants.PROGRESSION_NOT_ADVANCED_SENTINEL, life.LastAdvancedWorldDay);
        }

        [Test]
        public void GenerateRegen_BlockBuiltFromAGeneratedRegen_IsAcceptedByFromBlocks()
        {
            // The half of M3 that actually fails if the sentinel comes back: ProgressionEngine.FromBlocks
            // refuses PROGRESSION_NOT_ADVANCED_SENTINEL as a store state (ERR-028-014) — a block built
            // straight from GenerateRegen's output must pass that gate without a caller having to patch
            // LastAdvancedWorldDay first.
            var rng = NewRng(87UL, clubId: 9, out int idx);
            (PlayerRecord rec, PlayerLifecycle life) = RegenGenerator.GenerateRegen(rng, idx, clubId: 9, newPlayerId: 500, WorldDay);

            var block = new ClubCareerStates(9, new[] { rec }, new[] { life });

            Assert.DoesNotThrow(
                () => ProgressionEngine.FromBlocks(new[] { block }, nextPlayerId: 501),
                "a block built straight from a generated regen must be a legal store state (ERR-028-014).");
        }

        [Test]
        public void GenerateRegen_NullRng_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(
                () => RegenGenerator.GenerateRegen(null, 0, 0, 1, WorldDay));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial implementation.                                        |
// | 1.1     | 2026-08-10 | —      | M3 lock (ERR-028-014 carryforward): GenerateRegen_LastAdvanced |
// |         |            |        | WorldDay_EqualsWorldDayPassedIn + GenerateRegen_BlockBuiltFrom |
// |         |            |        | AGeneratedRegen_IsAcceptedByFromBlocks, over RegenGenerator    |
// |         |            |        | 1.4's fix (seed worldDay, not the retired sentinel).           |
#endregion
