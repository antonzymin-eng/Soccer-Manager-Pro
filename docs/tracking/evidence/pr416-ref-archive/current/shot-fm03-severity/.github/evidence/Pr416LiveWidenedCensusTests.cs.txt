using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using TacticalDirector.BallPhysics;
using TacticalDirector.DefensiveAI;
using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    internal sealed class Pr416MechanismCensusTests
    {
        private const int Ticks = 324_000;
        private const int AiStride = 6;

        private static readonly ulong[] Seeds =
        {
            0x0F1E2D3C4B5A6978UL,
            0x00000000D1A6D05EUL,
            0x5EED000000000003UL,
        };

        private sealed class RunRecord
        {
            public int Length;
            public int StartTick;
            public int EndTick;
            public int Holder;
            public bool IsGoalkeeper;
            public float StartHolderX;
            public float StartHolderY;
            public float EndHolderX;
            public float EndHolderY;
            public float StartBallX;
            public float StartBallY;
            public float StartBallZ;
            public float EndBallX;
            public float EndBallY;
            public float EndBallZ;
            public float MinBallZ;
            public float MaxBallZ;
            public int PassBusyHeartbeats;
            public int ShotBusyHeartbeats;
            public readonly SortedDictionary<string, int> Actions = new SortedDictionary<string, int>();
        }

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

        private static string F(float value) =>
            value.ToString("F6", CultureInfo.InvariantCulture);

        private static string ActionSummary(SortedDictionary<string, int> actions)
        {
            if (actions.Count == 0) return "none";
            var parts = new List<string>(actions.Count);
            foreach (KeyValuePair<string, int> pair in actions)
            {
                parts.Add(pair.Key + ":" + pair.Value.ToString(CultureInfo.InvariantCulture));
            }
            return string.Join(",", parts);
        }

        [Test]
        public void MeasurePr416MechanismCensus()
        {
            foreach (ulong seed in Seeds)
            {
                MatchEngine engine = Booted(seed);
                var before = engine.TestOnly_TackleOutcomeCounts;
                int previousHolder = MatchEngineConstants.NO_POSSESSION;
                int dispossessions = 0;
                int controlledToNonControlled = 0;
                int controlledHolderChanges = 0;
                bool previousWasControlled = false;

                long[] controlledTicksByHolder = new long[MatchEngineConstants.SQUAD_SIZE];

                RunRecord current = null;
                RunRecord longest = null;

                for (int t = 0; t < Ticks; t++)
                {
                    engine.RunTick();

                    var ball = engine.TestOnly_BallSnapshot;
                    int holder = engine.PossessingAgentId;
                    bool controlled =
                        ball.State == BallStateType.Controlled
                        && holder >= 0
                        && holder < MatchEngineConstants.SQUAD_SIZE;

                    var now = engine.TestOnly_TackleOutcomeCounts;
                    bool landed = (now.Won + now.Loose) > (before.Won + before.Loose);
                    if (landed && previousHolder >= 0 && holder != previousHolder)
                    {
                        dispossessions++;
                    }

                    if (previousWasControlled && !controlled)
                    {
                        controlledToNonControlled++;
                    }

                    if (controlled)
                    {
                        controlledTicksByHolder[holder]++;

                        if (current == null || current.Holder != holder)
                        {
                            if (current != null)
                            {
                                current.EndTick = t;
                                if (longest == null || current.Length > longest.Length)
                                {
                                    longest = current;
                                }
                                controlledHolderChanges++;
                            }

                            var agent = engine.TestOnly_AgentSnapshot(holder);
                            current = new RunRecord
                            {
                                Length = 0,
                                StartTick = t + 1,
                                EndTick = t + 1,
                                Holder = holder,
                                IsGoalkeeper = engine.TestOnly_IsGoalkeeper(holder),
                                StartHolderX = agent.Position.x,
                                StartHolderY = agent.Position.y,
                                EndHolderX = agent.Position.x,
                                EndHolderY = agent.Position.y,
                                StartBallX = ball.Position.x,
                                StartBallY = ball.Position.y,
                                StartBallZ = ball.Position.z,
                                EndBallX = ball.Position.x,
                                EndBallY = ball.Position.y,
                                EndBallZ = ball.Position.z,
                                MinBallZ = ball.Position.z,
                                MaxBallZ = ball.Position.z,
                            };
                        }

                        current.Length++;
                        current.EndTick = t + 1;
                        var currentAgent = engine.TestOnly_AgentSnapshot(holder);
                        current.EndHolderX = currentAgent.Position.x;
                        current.EndHolderY = currentAgent.Position.y;
                        current.EndBallX = ball.Position.x;
                        current.EndBallY = ball.Position.y;
                        current.EndBallZ = ball.Position.z;
                        if (ball.Position.z < current.MinBallZ) current.MinBallZ = ball.Position.z;
                        if (ball.Position.z > current.MaxBallZ) current.MaxBallZ = ball.Position.z;

                        if (((t + 1) % AiStride) == 0)
                        {
                            string action = engine.TestOnly_DtHasDispatched(holder)
                                ? engine.TestOnly_DtLastActionType(holder).ToString()
                                : "UNDISPATCHED";
                            int count;
                            current.Actions.TryGetValue(action, out count);
                            current.Actions[action] = count + 1;

                            if (!engine.TestOnly_PassExecutorIdle(holder)) current.PassBusyHeartbeats++;
                            if (!engine.TestOnly_ShotExecutorIdle(holder)) current.ShotBusyHeartbeats++;
                        }
                    }
                    else if (current != null)
                    {
                        current.EndTick = t;
                        if (longest == null || current.Length > longest.Length)
                        {
                            longest = current;
                        }
                        current = null;
                    }

                    before = now;
                    previousHolder = holder;
                    previousWasControlled = controlled;
                }

                if (current != null)
                {
                    current.EndTick = Ticks;
                    if (longest == null || current.Length > longest.Length)
                    {
                        longest = current;
                    }
                }

                var counts = engine.TestOnly_TackleOutcomeCounts;
                var gate = engine.TestOnly_TackleGateAnatomy;

                var holderParts = new List<string>();
                for (int i = 0; i < controlledTicksByHolder.Length; i++)
                {
                    if (controlledTicksByHolder[i] > 0)
                    {
                        holderParts.Add(
                            i.ToString(CultureInfo.InvariantCulture) + ":"
                            + controlledTicksByHolder[i].ToString(CultureInfo.InvariantCulture)
                            + (engine.TestOnly_IsGoalkeeper(i) ? "(GK)" : ""));
                    }
                }

                TestContext.WriteLine(
                    "PR416_TACKLE_CENSUS seed=0x" + seed.ToString("X16", CultureInfo.InvariantCulture)
                    + " won=" + counts.Won
                    + " loose=" + counts.Loose
                    + " foul=" + counts.Foul
                    + " missed=" + counts.Missed
                    + " dispossessions=" + dispossessions
                    + " gateEligible=" + gate.Eligible
                    + " gateInRadius=" + gate.InRadius
                    + " gateMeanNearestM=" + F(gate.MeanNearestM));

                TestContext.WriteLine(
                    "PR416_CONTROLLED_HOLDERS seed=0x" + seed.ToString("X16", CultureInfo.InvariantCulture)
                    + " ticksByHolder=" + string.Join(",", holderParts)
                    + " controlledToNonControlled=" + controlledToNonControlled
                    + " controlledHolderChanges=" + controlledHolderChanges);

                if (longest != null)
                {
                    TestContext.WriteLine(
                        "PR416_LONGEST_CONTROLLED seed=0x" + seed.ToString("X16", CultureInfo.InvariantCulture)
                        + " lengthTicks=" + longest.Length
                        + " startTick=" + longest.StartTick
                        + " endTick=" + longest.EndTick
                        + " holder=" + longest.Holder
                        + " isGoalkeeper=" + longest.IsGoalkeeper
                        + " holderStart=(" + F(longest.StartHolderX) + "," + F(longest.StartHolderY) + ")"
                        + " holderEnd=(" + F(longest.EndHolderX) + "," + F(longest.EndHolderY) + ")"
                        + " ballStart=(" + F(longest.StartBallX) + "," + F(longest.StartBallY) + "," + F(longest.StartBallZ) + ")"
                        + " ballEnd=(" + F(longest.EndBallX) + "," + F(longest.EndBallY) + "," + F(longest.EndBallZ) + ")"
                        + " ballZRange=(" + F(longest.MinBallZ) + "," + F(longest.MaxBallZ) + ")"
                        + " actions=" + ActionSummary(longest.Actions)
                        + " passBusyHeartbeats=" + longest.PassBusyHeartbeats
                        + " shotBusyHeartbeats=" + longest.ShotBusyHeartbeats);
                }
            }

            Assert.Pass("measurement complete");
        }
    }
}
