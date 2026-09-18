// File:     src/match-engine/tests/W2SixSeedEvidenceTests.cs
// Created:  2026-09-18
// Author:   —
// Purpose:  Temporary dedicated Step-1b denominator capture for the broader six-seed W2 evidence.
//           Phase 1 deliberately emits sample counts only. Possession shares remain unobserved until
//           seed-specific floors are frozen in a later commit.

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

        [Test]
        [Category("Calibration")]
        public void BaselineCapture_ReportsOnlyPerSeedFinalThirdSampleCounts()
        {
            string mode = Environment.GetEnvironmentVariable("TD_W2_SIX_SEED_MODE") ?? string.Empty;
            Assert.That(mode, Is.EqualTo("baseline"),
                "Phase-1 driver is baseline-only until numeric seed floors are frozen.");

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

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            try
            {
                foreach (ulong seed in Seeds)
                {
                    int samples = CountFinalThirdSamples(seed);
                    TestContext.WriteLine(
                        "W2_SIX_SEED_BASELINE seed=0x"
                        + seed.ToString("X16", CultureInfo.InvariantCulture)
                        + " samples=" + samples.ToString(CultureInfo.InvariantCulture));
                }
            }
            finally
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            }
        }

        private static int CountFinalThirdSamples(ulong seed)
        {
            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, clubId: 1), BuildSquad(seed, clubId: 2));

            int samples = 0;
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

                samples++;
            }

            return samples;
        }

        private static Squad BuildSquad(ulong seed, int clubId)
        {
            var rng = new DeterministicRngService(seed ^ (ulong)clubId);
            int stream = rng.RegisterStream(
                "w2-six-seed-evidence.roster",
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
