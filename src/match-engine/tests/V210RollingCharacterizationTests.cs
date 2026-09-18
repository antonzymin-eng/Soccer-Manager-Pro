// File:     src/match-engine/tests/V210RollingCharacterizationTests.cs
// Created:  2026-09-18
// Author:   —
// Purpose:  Temporary env-gated Step-3.3 characterization of Ball Physics v2.10 elevated-Rolling
//           distribution effects. Assertion-free on measured values (ERR-030-014 convention).

using System;
using System.Globalization;

using NUnit.Framework;

using TacticalDirector.BallPhysics;
using TacticalDirector.DecisionTree;
using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    internal class V210RollingCharacterizationTests
    {
        private static readonly int NumTicks = (int)MatchEngineConstants.MATCH_TICKS_TOTAL;
        private const int ActionTypeCount = 8;
        private const int TrajectorySampleStride = 60;

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
        public void CharacterizeSelectedSeed()
        {
            if ((Environment.GetEnvironmentVariable("TD_V210_ROLLING_CHARACTERIZATION") ?? "") != "1")
            {
                Assert.Ignore("Set TD_V210_ROLLING_CHARACTERIZATION=1 for the Step-3.3 evidence run.");
            }

            string mode = Environment.GetEnvironmentVariable("TD_V210_MODE") ?? "";
            Assert.That(mode == "v210" || mode == "narrow", Is.True,
                "TD_V210_MODE must be v210 or narrow.");

            string seedText = Environment.GetEnvironmentVariable("TD_V210_SEED") ?? "";
            Assert.That(seedText, Does.Match("^0x[0-9A-F]{16}$"));
            ulong seed = ulong.Parse(
                seedText.Substring(2),
                NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture);
            Assert.That(Array.IndexOf(Seeds, seed), Is.GreaterThanOrEqualTo(0),
                "Seed is outside the frozen six-seed corpus.");

            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, 1), BuildSquad(seed, 2));

            var actionCounts = new long[ActionTypeCount];
            var lastHeartbeat = new int[MatchEngineConstants.SQUAD_SIZE];
            for (int i = 0; i < lastHeartbeat.Length; i++) lastHeartbeat[i] = int.MinValue;

            var stateTicks = new long[6];
            long finalThirdTicks = 0;
            long possessedTicks = 0;
            long possessionChanges = 0;
            int previousHolder = engine.PossessingAgentId;

            double distanceProxyM = 0.0;
            double zSum = 0.0;
            double speedSum = 0.0;
            float maxZ = 0f;
            float maxSpeed = 0f;
            ulong trajectoryHash = 1469598103934665603UL;

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            try
            {
                for (int tick = 0; tick < NumTicks; tick++)
                {
                    engine.RunTick();

                    BallState ball = engine.BallView;
                    float speed = ball.Velocity.magnitude;
                    distanceProxyM += speed * DeterministicSimConstants.FrameSeconds;
                    zSum += ball.Position.z;
                    speedSum += speed;
                    if (ball.Position.z > maxZ) maxZ = ball.Position.z;
                    if (speed > maxSpeed) maxSpeed = speed;

                    int stateOrdinal = (int)ball.State;
                    if (stateOrdinal >= 0 && stateOrdinal < stateTicks.Length) stateTicks[stateOrdinal]++;

                    float third = MatchEngineConstants.PITCH_LENGTH_M / 3f;
                    if (ball.Position.x <= third || ball.Position.x >= MatchEngineConstants.PITCH_LENGTH_M - third)
                        finalThirdTicks++;

                    int holder = engine.PossessingAgentId;
                    if (holder >= 0) possessedTicks++;
                    if (holder != previousHolder)
                    {
                        possessionChanges++;
                        previousHolder = holder;
                    }

                    for (int agent = 0; agent < MatchEngineConstants.SQUAD_SIZE; agent++)
                    {
                        if (!engine.TestOnly_DtHasDispatched(agent)) continue;
                        AgentAction action = engine.TestOnly_DtLastAction(agent);
                        if (action.HeartbeatTick == lastHeartbeat[agent]) continue;
                        lastHeartbeat[agent] = action.HeartbeatTick;
                        int ordinal = (int)action.Type;
                        if (ordinal >= 0 && ordinal < actionCounts.Length) actionCounts[ordinal]++;
                    }

                    if ((tick + 1) % TrajectorySampleStride == 0)
                    {
                        trajectoryHash = Mix(trajectoryHash, Quantize(ball.Position.x));
                        trajectoryHash = Mix(trajectoryHash, Quantize(ball.Position.y));
                        trajectoryHash = Mix(trajectoryHash, Quantize(ball.Position.z));
                        trajectoryHash = Mix(trajectoryHash, Quantize(ball.Velocity.x));
                        trajectoryHash = Mix(trajectoryHash, Quantize(ball.Velocity.y));
                        trajectoryHash = Mix(trajectoryHash, Quantize(ball.Velocity.z));
                        trajectoryHash = Mix(trajectoryHash, (long)ball.State);
                        trajectoryHash = Mix(trajectoryHash, holder);
                    }
                }
            }
            finally
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            }

            int erTicks = engine.TestOnly_V210ElevatedRollingTicks;
            double erMeanZ = erTicks > 0
                ? engine.TestOnly_V210ElevatedRollingHeightSum / erTicks
                : 0.0;
            double erMeanSpeed = erTicks > 0
                ? engine.TestOnly_V210ElevatedRollingSpeedSum / erTicks
                : 0.0;

            TestContext.WriteLine(
                "V210_ROLLING_RESULT"
                + "\t" + mode
                + "\t0x" + seed.ToString("X16", CultureInfo.InvariantCulture)
                + "\t" + engine.TestOnly_V210ElevatedRollingEpisodes
                + "\t" + erTicks
                + "\t" + engine.TestOnly_V210ElevatedRollingMovingEpisodes
                + "\t" + engine.TestOnly_V210ElevatedRollingSlowEpisodes
                + "\t" + engine.TestOnly_V210ElevatedRollingMovingTicks
                + "\t" + engine.TestOnly_V210ElevatedRollingSlowTicks
                + "\t" + engine.TestOnly_V210ElevatedRollingPostAirborne
                + "\t" + engine.TestOnly_V210ElevatedRollingPostRolling
                + "\t" + engine.TestOnly_V210ElevatedRollingPostOther
                + "\t" + erMeanZ.ToString("F6", CultureInfo.InvariantCulture)
                + "\t" + erMeanSpeed.ToString("F6", CultureInfo.InvariantCulture)
                + "\t" + engine.TestOnly_V210ElevatedRollingMaxHeight.ToString("F6", CultureInfo.InvariantCulture)
                + "\t" + engine.TestOnly_V210ElevatedRollingMaxSpeed.ToString("F6", CultureInfo.InvariantCulture)
                + "\t" + distanceProxyM.ToString("F6", CultureInfo.InvariantCulture)
                + "\t" + (zSum / NumTicks).ToString("F6", CultureInfo.InvariantCulture)
                + "\t" + maxZ.ToString("F6", CultureInfo.InvariantCulture)
                + "\t" + (speedSum / NumTicks).ToString("F6", CultureInfo.InvariantCulture)
                + "\t" + maxSpeed.ToString("F6", CultureInfo.InvariantCulture)
                + "\t" + stateTicks[0]
                + "\t" + stateTicks[1]
                + "\t" + stateTicks[2]
                + "\t" + stateTicks[3]
                + "\t" + stateTicks[4]
                + "\t" + stateTicks[5]
                + "\t" + finalThirdTicks
                + "\t" + possessedTicks
                + "\t" + possessionChanges
                + "\t" + engine.HomeScore
                + "\t" + engine.AwayScore
                + "\t" + actionCounts[(int)ActionType.PASS]
                + "\t" + actionCounts[(int)ActionType.SHOOT]
                + "\t" + actionCounts[(int)ActionType.DRIBBLE]
                + "\t" + actionCounts[(int)ActionType.HOLD]
                + "\t" + actionCounts[(int)ActionType.MOVE_TO_POSITION]
                + "\t" + actionCounts[(int)ActionType.PRESS]
                + "\t" + actionCounts[(int)ActionType.INTERCEPT]
                + "\t" + actionCounts[(int)ActionType.SAVE]
                + "\t0x" + trajectoryHash.ToString("X16", CultureInfo.InvariantCulture));
        }

        private static long Quantize(float value) =>
            (long)Math.Round(value * 1000.0, MidpointRounding.AwayFromZero);

        private static ulong Mix(ulong hash, long value)
        {
            unchecked
            {
                ulong v = (ulong)value;
                for (int i = 0; i < 8; i++)
                {
                    hash ^= (byte)(v & 0xffUL);
                    hash *= 1099511628211UL;
                    v >>= 8;
                }
                return hash;
            }
        }

        private static Squad BuildSquad(ulong seed, int clubId)
        {
            var rng = new DeterministicRngService(seed ^ (ulong)clubId);
            int stream = rng.RegisterStream(
                "diagnostic.roster",
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
