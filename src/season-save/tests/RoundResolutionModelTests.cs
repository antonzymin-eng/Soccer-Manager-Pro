// File:     src/season-save/tests/RoundResolutionModelTests.cs
// Created:  2026-07-26
// Modified: 2026-07-26
// Author:   —
// Spec:     Season & Competition Loop #30 §3.4.1 (FR-SN-013a), FR-SN-027, §5.4 (T-SN-CAL-003c);
//           League Bootstrap design supplement KD-7; Code Standards #20
// Purpose:  Locks the round-resolution model's contract: keyed (not cursor-positioned) determinism,
//           per-input key sensitivity, the exp-shaped lambda with its safety clamps, the inverse-CDF
//           quantile's endpoints and cap, home-advantage asymmetry, and the fail-loud non-finite gate.

using NUnit.Framework;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    internal class RoundResolutionModelTests
    {
        private const ulong Seed = 0xC0FFEE1234567890UL;
        private const uint Day = 42u;

        private static Fixture Fx(int round, int home, int away) => new Fixture(round, home, away);

        // ── Determinism (FR-SN-013a) ────────────────────────────────────────────────────────

        [Test]
        public void Resolve_SameInputs_IsDeterministic()
        {
            Fixture f = Fx(3, 5, 11);
            MatchResult a = RoundResolutionModel.Resolve(in f, Seed, 0, 12.5f, 10.25f, Day);
            MatchResult b = RoundResolutionModel.Resolve(in f, Seed, 0, 12.5f, 10.25f, Day);

            Assert.AreEqual(a.HomeGoals, b.HomeGoals);
            Assert.AreEqual(a.AwayGoals, b.AwayGoals);
        }

        [Test]
        public void Resolve_IsIndependentOfCallOrder()
        {
            // The T-SN-CAL-003c property at the model level: a fixture's scoreline is a pure function of
            // its own identity, so interleaving other resolutions cannot change it. This is what a
            // cursor-positioned stream would break.
            Fixture target = Fx(0, 2, 7);
            MatchResult alone = RoundResolutionModel.Resolve(in target, Seed, 0, 11f, 11f, Day);

            Fixture other0 = Fx(0, 1, 4);
            Fixture other1 = Fx(0, 3, 9);
            RoundResolutionModel.Resolve(in other0, Seed, 0, 13f, 9f, Day);
            RoundResolutionModel.Resolve(in other1, Seed, 0, 8f, 15f, Day);
            MatchResult afterOthers = RoundResolutionModel.Resolve(in target, Seed, 0, 11f, 11f, Day);

            Assert.AreEqual(alone.HomeGoals, afterOthers.HomeGoals);
            Assert.AreEqual(alone.AwayGoals, afterOthers.AwayGoals);
        }

        [Test]
        public void FixtureKey_IsSensitiveToEveryInput()
        {
            ulong baseKey = RoundResolutionModel.FixtureKey(Seed, 0, 0, 1, 2);

            Assert.AreNotEqual(baseKey, RoundResolutionModel.FixtureKey(Seed + 1UL, 0, 0, 1, 2),
                "seasonSeed must change the key.");
            Assert.AreNotEqual(baseKey, RoundResolutionModel.FixtureKey(Seed, 1, 0, 1, 2),
                "seasonNumber must change the key — the same fixture in a later season must differ.");
            Assert.AreNotEqual(baseKey, RoundResolutionModel.FixtureKey(Seed, 0, 1, 1, 2),
                "roundIndex must change the key.");
            Assert.AreNotEqual(baseKey, RoundResolutionModel.FixtureKey(Seed, 0, 0, 3, 2),
                "homeClubId must change the key.");
            Assert.AreNotEqual(baseKey, RoundResolutionModel.FixtureKey(Seed, 0, 0, 1, 4),
                "awayClubId must change the key.");
        }

        [Test]
        public void FixtureKey_ReversedVenue_DiffersFromForward()
        {
            // The two legs of the same pairing must not share a scoreline.
            Assert.AreNotEqual(
                RoundResolutionModel.FixtureKey(Seed, 0, 0, 1, 2),
                RoundResolutionModel.FixtureKey(Seed, 0, 0, 2, 1));
        }

        [Test]
        public void MatchSeedFor_IsDeterministicAndDistinctFromTheFixtureKey()
        {
            Fixture f = Fx(5, 4, 6);
            ulong a = RoundResolutionModel.MatchSeedFor(in f, Seed, 0);
            ulong b = RoundResolutionModel.MatchSeedFor(in f, Seed, 0);
            Assert.AreEqual(a, b, "A managed fixture must reboot its engine from the same seed.");

            // Distinct domain constant ⇒ the engine's seed does not equal the quick-sim's key, so the two
            // producers for the same fixture cannot correlate.
            Assert.AreNotEqual(RoundResolutionModel.FixtureKey(Seed, 0, 5, 4, 6), a);
        }

        // ── Lambda shape + clamps (KD-7) ───────────────────────────────────────────────────

        [Test]
        public void Lambda_AtZeroEdge_IsBaseGoals()
        {
            Assert.AreEqual(SeasonLoopConstants.QuickSimBaseGoals, RoundResolutionModel.Lambda(0f), 1e-6f);
        }

        [Test]
        public void Lambda_IsMonotoneInEdge()
        {
            float low = RoundResolutionModel.Lambda(-2f);
            float mid = RoundResolutionModel.Lambda(0f);
            float high = RoundResolutionModel.Lambda(2f);

            Assert.Less(low, mid);
            Assert.Less(mid, high);
        }

        [Test]
        public void Lambda_IsClampedBothWays()
        {
            Assert.AreEqual(SeasonLoopConstants.QuickSimLambdaMin, RoundResolutionModel.Lambda(-1000f));
            Assert.AreEqual(SeasonLoopConstants.QuickSimLambdaMax, RoundResolutionModel.Lambda(+1000f));
        }

        [Test]
        public void Lambda_ClampsAreCoherent()
        {
            // A retune that inverted these would make Lambda return the floor for every edge, silently
            // flattening the whole league table to one scoreline distribution.
            Assert.Less(SeasonLoopConstants.QuickSimLambdaMin, SeasonLoopConstants.QuickSimLambdaMax);
            Assert.Greater(SeasonLoopConstants.QuickSimLambdaMin, 0f);
            Assert.GreaterOrEqual(SeasonLoopConstants.QuickSimBaseGoals, SeasonLoopConstants.QuickSimLambdaMin);
            Assert.LessOrEqual(SeasonLoopConstants.QuickSimBaseGoals, SeasonLoopConstants.QuickSimLambdaMax);
        }

        // ── Poisson inverse CDF (KD-7 / AR-7 M-1) ──────────────────────────────────────────

        [Test]
        public void PoissonInverseCdf_AtZeroUniform_ReturnsZero()
        {
            // u = 0 is below exp(-lambda) = cdf(0) for any finite lambda, so the loop never runs.
            Assert.AreEqual(0, RoundResolutionModel.PoissonInverseCdf(1.35f, 0f));
            Assert.AreEqual(0, RoundResolutionModel.PoissonInverseCdf(6.0f, 0f));
        }

        [Test]
        public void PoissonInverseCdf_IsNonDecreasingInTheUniform()
        {
            int previous = -1;
            for (int i = 0; i <= 100; i++)
            {
                int k = RoundResolutionModel.PoissonInverseCdf(1.35f, i / 101f);
                Assert.GreaterOrEqual(k, previous);
                previous = k;
            }
        }

        [Test]
        public void PoissonInverseCdf_TerminatesAtTheCap()
        {
            // A uniform above the whole reachable CDF must stop at the cap rather than spin. 0.9999999f is
            // the closest float below 1 that still exercises the tail.
            int k = RoundResolutionModel.PoissonInverseCdf(SeasonLoopConstants.QuickSimLambdaMax, 0.9999999f);
            Assert.LessOrEqual(k, SeasonLoopConstants.MAX_GOALS_PER_SIDE);
        }

        [Test]
        public void PoissonInverseCdf_MeanApproximatesLambda()
        {
            // The distributional sanity check the calibration corpus later refines: inverting a uniform
            // grid must reproduce the intended mean, or the model is not Poisson at all.
            const float Lambda = 1.5f;
            const int Samples = 2000;
            long total = 0;
            for (int i = 0; i < Samples; i++)
            {
                total += RoundResolutionModel.PoissonInverseCdf(Lambda, (i + 0.5f) / Samples);
            }

            Assert.AreEqual(Lambda, total / (float)Samples, 0.05f);
        }

        [Test]
        public void UnitFloat_IsInUnitInterval()
        {
            for (ulong i = 0; i < 512UL; i++)
            {
                float u = RoundResolutionModel.UnitFloat(i * 0x9E3779B97F4A7C15UL);
                Assert.GreaterOrEqual(u, 0f);
                Assert.Less(u, 1f);
            }
        }

        // ── Model behaviour (KD-7) ─────────────────────────────────────────────────────────

        [Test]
        public void Resolve_StrongerHomeSide_OutscoresOverASpreadOfFixtures()
        {
            // Not a statistical test of one fixture (a single scoreline is noise) — a directional check
            // over a fixed, reproducible spread of fixture identities. If this fails, the ramp's sign is
            // wrong and the league table would reward the weakest squads.
            int strongTotal = 0;
            int weakTotal = 0;
            for (int round = 0; round < 40; round++)
            {
                Fixture f = Fx(round, 0, 1);
                MatchResult r = RoundResolutionModel.Resolve(in f, Seed, 0, 14f, 8f, Day);
                strongTotal += r.HomeGoals;
                weakTotal += r.AwayGoals;
            }

            Assert.Greater(strongTotal, weakTotal);
        }

        [Test]
        public void Resolve_HomeAdvantage_ShowsAsAsymmetryBetweenEvenlyMatchedVenues()
        {
            // dSquad = 0 is the bucket that actually tests the fitted HomeAdvantageRating (KD-8's
            // acceptance bar): with equal squads the home side must still score more in aggregate.
            int homeTotal = 0;
            int awayTotal = 0;
            for (int round = 0; round < 60; round++)
            {
                Fixture f = Fx(round, 2, 3);
                MatchResult r = RoundResolutionModel.Resolve(in f, Seed, 0, 11f, 11f, Day);
                homeTotal += r.HomeGoals;
                awayTotal += r.AwayGoals;
            }

            Assert.Greater(SeasonLoopConstants.QuickSimHomeAdvantageRating, 0f,
                "A non-positive home advantage would make this assertion vacuous.");
            Assert.Greater(homeTotal, awayTotal);
        }

        [Test]
        public void Resolve_CarriesFixtureIdentityOntoTheResult()
        {
            Fixture f = Fx(7, 12, 3);
            MatchResult r = RoundResolutionModel.Resolve(in f, Seed, 0, 11f, 11f, Day);

            Assert.AreEqual(12, r.HomeClubId);
            Assert.AreEqual(3, r.AwayClubId);
            Assert.AreEqual(7, r.RoundIndex);
            Assert.AreEqual(Day, r.WorldDay);
        }

        // ── Fail-loud gates ───────────────────────────────────────────────────────────────

        [Test]
        public void Resolve_NonFiniteRating_FailsLoud()
        {
            Fixture f = Fx(0, 1, 2);

            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => RoundResolutionModel.Resolve(in f, Seed, 0, float.NaN, 11f, Day));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => RoundResolutionModel.Resolve(in f, Seed, 0, 11f, float.NaN, Day));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => RoundResolutionModel.Resolve(in f, Seed, 0, float.PositiveInfinity, 11f, Day));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => RoundResolutionModel.Resolve(in f, Seed, 0, 11f, float.NegativeInfinity, Day));
        }

        [Test]
        public void DomainTag_MirrorsTheSixteenAllocation()
        {
            // ERR-030-001: the tag is allocated in #16 and mirrored here; a drift would silently move
            // every scoreline in every save.
            Assert.AreEqual(
                TacticalDirector.DeterministicSim.DeterministicSimConstants.DOMAIN_TAG_SEASON_LOOP,
                SeasonLoopConstants.DomainTagSeasonLoop);
            Assert.AreEqual((byte)0x22, SeasonLoopConstants.DomainTagSeasonLoop);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-26 | —      | Initial suite (#30 T2): keyed determinism + order-independence,     |
// |         |            |        | per-input key sensitivity, lambda shape/clamps, inverse-CDF         |
// |         |            |        | endpoints/cap/mean, strength + home-advantage direction, non-finite |
// |         |            |        | gate, and the #16 domain-tag mirror lock.                           |
#endregion
