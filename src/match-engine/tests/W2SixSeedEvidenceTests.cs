// File:     src/match-engine/tests/W2SixSeedEvidenceTests.cs
// Created:  2026-09-18
// Author:   —
// Purpose:  Temporary dedicated Step-1b result driver for the broader six-seed W2 evidence.
//           The six sample floors below were frozen from run 35389373818 before this driver emitted
//           any six-seed possession-share result. This file is evidence scaffolding, not a PR gate.

using System;
using System.Globalization;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    internal class W2SixSeedEvidenceTests
    {
        private const int NumTicks = 324000;
        private const int SampleStrideTicks = 6;
        private const float ExpectedProductionRadiusM = 1.0f;
        private const float PossessionShareThreshold = 0.70f;
        private const float FinalThirdDepthM = MatchEngineConstants.PITCH_LENGTH_M / 3.0f;

        private static readonly ulong[] Seeds =
        {
            0x0F1E2D3C4B5A6978UL,
            0x00000000D1A6D05EUL,
            0x5EED000000000003UL,
            0x5EED000000000004UL,
            0x00000000D1A6D05FUL,
            0x1A2B3C4D5E6F7081UL,
        };

        private static readonly int[] MinimumSamplesBySeed =
        {
            12_664,
            15_127,
            12_536,
            12_440,
            14_529,
            13_138,
        };

        private struct Observation
        {
            public int Samples;
            public int HomeViewPossession;
            public int AwayViewPossession;
            public int Won;
            public int Loose;
            public int Foul;
            public int Missed;
        }

        [Test]
        [Category("Calibration")]
        public void Revalidation_ReportsSelectedSeedProductionOrDisarmedResult()
        {
            string mode = Environment.GetEnvironmentVariable("TD_W2_SIX_SEED_MODE") ?? string.Empty;
            Assert.That(
                mode == "production" || mode == "disarmed",
                Is.True,
                "TD_W2_SIX_SEED_MODE must be production or disarmed.");

            string seedText =
                Environment.GetEnvironmentVariable("TD_W2_SIX_SEED_HEX") ?? string.Empty;
            Assert.That(seedText, Does.Match("^0x[0-9A-F]{16}$"),
                "TD_W2_SIX_SEED_HEX must be a canonical preregistered seed.");
            ulong seed = ulong.Parse(
                seedText.Substring(2),
                NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture);
            int seedIndex = Array.IndexOf(Seeds, seed);
            Assert.That(seedIndex, Is.GreaterThanOrEqualTo(0),
                "workflow selected a seed outside the preregistered six-seed corpus");

            string floorText =
                Environment.GetEnvironmentVariable("TD_W2_SIX_SEED_FLOOR") ?? string.Empty;
            int workflowFloor;
            Assert.That(
                int.TryParse(
                    floorText,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out workflowFloor),
                Is.True,
                "TD_W2_SIX_SEED_FLOOR must be a canonical non-negative integer.");
            int frozenFloor = MinimumSamplesBySeed[seedIndex];
            Assert.That(workflowFloor, Is.EqualTo(frozenFloor),
                "workflow floor drifted from the preregistered driver floor");

            Assert.That(MatchEngineConstants.TackleContactRadiusM,
                Is.EqualTo(ExpectedProductionRadiusM).Within(0.000001f),
                "evidence config drifted from the preregistered production W2 radius");
            Assert.That(MatchEngineConstants.LooseBallPickupRadiusM,
                Is.EqualTo(ExpectedProductionRadiusM).Within(0.000001f),
                "evidence config drifted from the preregistered reclaim radius");
            Assert.That(MatchEngineConstants.TackleContactRadiusM, Is.GreaterThan(0f));
            Assert.That(
                MatchEngineConstants.TackleContactRadiusM,
                Is.LessThanOrEqualTo(MatchEngineConstants.LooseBallPickupRadiusM));

            bool disarmed = mode == "disarmed";
            Observation observation;
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            try
            {
                observation = RunOne(seed, disarmed);
            }
            finally
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            }

            float homeShare = observation.Samples > 0
                ? (float)observation.HomeViewPossession / observation.Samples
                : 0f;
            float awayShare = observation.Samples > 0
                ? (float)observation.AwayViewPossession / observation.Samples
                : 0f;
            string classification = observation.Samples < frozenFloor
                ? "INSUFFICIENT"
                : homeShare > PossessionShareThreshold && awayShare > PossessionShareThreshold
                    ? "PASS"
                    : "LOCALIZE";

            TestContext.WriteLine(
                "W2_SIX_SEED_RESULT"
                + " mode=" + mode
                + " seed=0x" + seed.ToString("X16", CultureInfo.InvariantCulture)
                + " samples=" + observation.Samples.ToString(CultureInfo.InvariantCulture)
                + " floor=" + frozenFloor.ToString(CultureInfo.InvariantCulture)
                + " homeShare=" + homeShare.ToString("F6", CultureInfo.InvariantCulture)
                + " awayShare=" + awayShare.ToString("F6", CultureInfo.InvariantCulture)
                + " classification=" + classification
                + " won=" + observation.Won.ToString(CultureInfo.InvariantCulture)
                + " loose=" + observation.Loose.ToString(CultureInfo.InvariantCulture)
                + " foul=" + observation.Foul.ToString(CultureInfo.InvariantCulture)
                + " missed=" + observation.Missed.ToString(CultureInfo.InvariantCulture));

            int resolved =
                observation.Won + observation.Loose + observation.Foul + observation.Missed;

            if (disarmed)
            {
                Assert.That(resolved, Is.EqualTo(0),
                    "disarmed causal-control arm resolved a tackle despite zero override");
                return;
            }

            Assert.That(
                observation.Samples,
                Is.GreaterThanOrEqualTo(frozenFloor),
                "production seed is below its preregistered non-vacuity floor");
            Assert.That(
                homeShare,
                Is.GreaterThan(PossessionShareThreshold),
                "production seed failed the preregistered home-view possession criterion");
            Assert.That(
                awayShare,
                Is.GreaterThan(PossessionShareThreshold),
                "production seed failed the preregistered away-view possession criterion");
        }

        private static Observation RunOne(ulong seed, bool disarmed)
        {
            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, clubId: 1), BuildSquad(seed, clubId: 2));
            if (disarmed)
            {
                engine.TestOnly_ArmTackleChallenge(0f);
            }

            var observation = new Observation();
            for (int tick = 0; tick < NumTicks; tick++)
            {
                engine.RunTick();
                if (tick % SampleStrideTicks != 0)
                {
                    continue;
                }

                UnityEngine.Vector3 ballPos = engine.BallView.Position;
                float depthHome = MatchEngineConstants.PITCH_LENGTH_M - ballPos.x;
                float depthAway = ballPos.x;
                if (depthHome > FinalThirdDepthM && depthAway > FinalThirdDepthM)
                {
                    continue;
                }

                observation.Samples++;
                if (IsPossessionPhase(engine.TestOnly_PositioningPhase(0)))
                {
                    observation.HomeViewPossession++;
                }
                if (IsPossessionPhase(engine.TestOnly_PositioningPhase(1)))
                {
                    observation.AwayViewPossession++;
                }
            }

            var outcomes = engine.TestOnly_TackleOutcomeCounts;
            observation.Won = outcomes.Won;
            observation.Loose = outcomes.Loose;
            observation.Foul = outcomes.Foul;
            observation.Missed = outcomes.Missed;
            return observation;
        }

        private static bool IsPossessionPhase(PositioningAI.Phase phase) =>
            phase == PositioningAI.Phase.InPoss || phase == PositioningAI.Phase.OutOfPoss;

        private static Squad BuildSquad(ulong seed, int clubId)
        {
            var rng = new DeterministicRngService(seed ^ (ulong)clubId);
            int stream = rng.RegisterStream(
                "scenario.roster",
                SubsystemOrdinals.PlayerDatabase,
                entityId: clubId,
                streamVersion: 1);

            var template = new PlayerPosition[PlayerDatabaseConstants.CLUB_SQUAD_SIZE];
            int i = 0;
            for (int k = 0; k < 3; k++) template[i++] = PlayerPosition.Goalkeeper;
            for (int k = 0; k < 8; k++) template[i++] = PlayerPosition.Defender;
            for (int k = 0; k < 8; k++) template[i++] = PlayerPosition.Midfielder;
            while (i < template.Length) template[i++] = PlayerPosition.Forward;

            return RosterGenerator.Generate(rng, stream, clubId, template);
        }
    }
}
