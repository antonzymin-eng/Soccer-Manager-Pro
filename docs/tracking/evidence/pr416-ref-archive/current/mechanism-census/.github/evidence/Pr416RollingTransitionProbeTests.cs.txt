using System;
using System.Globalization;
using NUnit.Framework;
using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    internal sealed class Pr416RollingTransitionProbeTests
    {
        private const int Ticks = 150_000;
        private static readonly ulong[] Seeds =
        {
            0x0F1E2D3C4B5A6978UL,
            0x00000000D1A6D05EUL,
        };

        private static Squad BuildSquad(ulong seed, int clubId)
        {
            var rng = new DeterministicRngService(seed ^ (ulong)clubId);
            int stream = rng.RegisterStream(
                "tackle.roster", SubsystemOrdinals.PlayerDatabase, entityId: clubId, streamVersion: 1);
            var template = new PlayerPosition[PlayerDatabaseConstants.CLUB_SQUAD_SIZE];
            int i = 0;
            for (int k = 0; k < 3; k++) template[i++] = PlayerPosition.Goalkeeper;
            for (int k = 0; k < 8; k++) template[i++] = PlayerPosition.Defender;
            for (int k = 0; k < 8; k++) template[i++] = PlayerPosition.Midfielder;
            while (i < template.Length) template[i++] = PlayerPosition.Forward;
            return RosterGenerator.Generate(rng, stream, clubId, template);
        }

        private static MatchEngine Booted(ulong seed)
        {
            var engine = new MatchEngine(seed);
            engine.ConfigureSquads(BuildSquad(seed, 1), BuildSquad(seed, 2));
            return engine;
        }

        [Test]
        public void MeasureRollingTransitions()
        {
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            foreach (ulong seed in Seeds)
            {
                MatchEngine engine = Booted(seed);
                int bouncingToRolling = 0;
                int rollingPreElevated = 0;
                int rollingPreElevatedBelowMin = 0;
                int rollingToAirborne = 0;
                int rollingToStationary = 0;
                float maxRollingPreZ = float.MinValue;
                float maxBouncingToRollingZ = float.MinValue;
                float maxBouncingToRollingVz = float.MinValue;
                int samplesPrinted = 0;

                for (int tick = 1; tick <= Ticks; tick++)
                {
                    var before = engine.BallView;
                    float beforeSpeed = before.Velocity.magnitude;

                    if (before.State == BallStateType.Rolling)
                    {
                        if (before.Position.z > maxRollingPreZ)
                            maxRollingPreZ = before.Position.z;

                        if (before.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
                        {
                            rollingPreElevated++;
                            if (beforeSpeed < BallPhysicsConstants.State.MinVelocity)
                                rollingPreElevatedBelowMin++;

                            if (samplesPrinted < 12)
                            {
                                TestContext.Progress.WriteLine(
                                    "PR416_ROLLING_PRE_ELEVATED seed=0x" +
                                    seed.ToString("X16", CultureInfo.InvariantCulture) +
                                    " tick=" + tick.ToString(CultureInfo.InvariantCulture) +
                                    " z=" + before.Position.z.ToString("F6", CultureInfo.InvariantCulture) +
                                    " vz=" + before.Velocity.z.ToString("F6", CultureInfo.InvariantCulture) +
                                    " speed=" + beforeSpeed.ToString("F6", CultureInfo.InvariantCulture));
                                samplesPrinted++;
                            }
                        }
                    }

                    engine.RunTick();
                    var after = engine.BallView;

                    if (before.State == BallStateType.Bouncing && after.State == BallStateType.Rolling)
                    {
                        bouncingToRolling++;
                        if (after.Position.z > maxBouncingToRollingZ)
                            maxBouncingToRollingZ = after.Position.z;
                        if (Math.Abs(after.Velocity.z) > maxBouncingToRollingVz)
                            maxBouncingToRollingVz = Math.Abs(after.Velocity.z);
                    }

                    if (before.State == BallStateType.Rolling && after.State == BallStateType.Airborne)
                        rollingToAirborne++;
                    if (before.State == BallStateType.Rolling && after.State == BallStateType.Stationary)
                        rollingToStationary++;
                }

                TestContext.Progress.WriteLine(
                    "PR416_ROLLING_TRANSITIONS seed=0x" +
                    seed.ToString("X16", CultureInfo.InvariantCulture) +
                    " bouncingToRolling=" + bouncingToRolling.ToString(CultureInfo.InvariantCulture) +
                    " maxBouncingToRollingZ=" + maxBouncingToRollingZ.ToString("F6", CultureInfo.InvariantCulture) +
                    " maxBouncingToRollingAbsVz=" + maxBouncingToRollingVz.ToString("F6", CultureInfo.InvariantCulture) +
                    " rollingPreElevated=" + rollingPreElevated.ToString(CultureInfo.InvariantCulture) +
                    " rollingPreElevatedBelowMin=" + rollingPreElevatedBelowMin.ToString(CultureInfo.InvariantCulture) +
                    " rollingToAirborne=" + rollingToAirborne.ToString(CultureInfo.InvariantCulture) +
                    " rollingToStationary=" + rollingToStationary.ToString(CultureInfo.InvariantCulture) +
                    " maxRollingPreZ=" + maxRollingPreZ.ToString("F6", CultureInfo.InvariantCulture));
            }
        }
    }
}
