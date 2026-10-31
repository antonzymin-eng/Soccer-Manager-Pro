// File:     docs/tracking/evidence/header441-reachability-20261020/sanity/Program.cs
// Created:  2026-10-20
// Author:   —
// Purpose:  Independent, explicitly run pre-Heading proximity sanity counter for issue #441.
//           Reads real callback worlds; does not use ArrivalCensus, hypothetical jumps, or commits.
// History:  2026-10-22 — Separate attempted ticks from callbacks; validate the full-time boundary.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using TacticalDirector.AgentMovement;
using TacticalDirector.MatchEngine;

internal static class Program
{
    private const int FramesPerSeed = 324000;
    private const int ProgressInterval = 32400;
    private const float LowerBallZ = 1.5f;
    private const float UpperBallZ = 2.6f;
    private const float NearXySquared = 1.0f;

    private static readonly ulong[] Seeds =
    {
        0x0F1E2D3C4B5A6978UL, 0x00000000D1A6D05EUL,
        0x0000000000000001UL, 0x00000000ABCDEF12UL,
        0x0000000099887766UL, 0x000000005A5A5A5AUL
    };

    private static readonly string[] Run3Digests =
    {
        "6EF1E77F9DCF86C4BCE5E1F66A989A64B42F26C268EAD53340B9C57D282B4072",
        "06E3DAF03F1C45A112F958C4BFAC980C30956971E175B35B7D815D685B4CAF93",
        "25CEAC27F7D4E87FB1166227F683EAFF87336EFD301FA5500B6D212A28DEBA59",
        "77B4E0CC08A4AC903FBB364F2BB28612EAA39B380955111C58FE040EBF474A80",
        "CCC99BA78DCE889ED07D18609C568486920FFD97C45CB283F15D7F5C203F6E4F",
        "6961B03FF4CB1512967C01C46158274B3516BB4C632D921F75E5F6BAAD936EB3"
    };

    private static string F(float x) => x.ToString("R", CultureInfo.InvariantCulture);

    // Real-engine cadence is stricter than the synthetic counter's intentional gap-split policy.
    private sealed class Cadence
    {
        private readonly ulong _seed;
        private readonly int _startTick;
        private readonly TextWriter _output;
        private long _callbacksBefore;
        private bool _wrongCallbackTick;
        internal int AttemptedRunTicks;
        internal int CompletedRunTicks;
        internal long CallbackCount;
        internal int FirstSample = -1;
        internal int LastSample = -1;

        internal Cadence(ulong seed, TextWriter output, int startTick = 0)
        {
            _seed = seed;
            _output = output;
            _startTick = startTick;
        }

        internal void BeginAttempt()
        {
            AttemptedRunTicks++;
            _callbacksBefore = CallbackCount;
            _wrongCallbackTick = false;
        }

        internal void ObserveCallback(int tick)
        {
            CallbackCount++;
            if (FirstSample < 0) FirstSample = tick;
            LastSample = tick;
            if (tick != _startTick + AttemptedRunTicks) _wrongCallbackTick = true;
        }

        internal void CompleteAttempt(int tick, bool ended)
        {
            CompletedRunTicks++;
            int expectedTick = _startTick + AttemptedRunTicks;
            bool expectedEnded = expectedTick >= FramesPerSeed;
            long expectedDelta = expectedEnded ? 0 : 1;
            if (CompletedRunTicks != AttemptedRunTicks || tick != expectedTick
                || ended != expectedEnded || CallbackCount - _callbacksBefore != expectedDelta
                || _wrongCallbackTick)
                Fail("per-attempt cadence", tick, ended);
        }

        internal void RunTick(MatchEngine engine)
        {
            BeginAttempt();
            try { engine.RunTick(); }
            catch (Exception exception)
            {
                Fail("RunTick threw " + exception.GetType().Name, (int)engine.CurrentTick,
                    engine.MatchEnded, exception);
            }
            CompleteAttempt((int)engine.CurrentTick, engine.MatchEnded);
        }

        internal void CheckCorpusEnd(MatchEngine engine, Counter counter)
        {
            if (AttemptedRunTicks != FramesPerSeed || CompletedRunTicks != FramesPerSeed
                || CallbackCount != FramesPerSeed - 1 || FirstSample != 1
                || LastSample != FramesPerSeed - 1 || engine.CurrentTick != FramesPerSeed
                || !engine.MatchEnded || counter.Counts.CallbackFrames != CallbackCount
                || counter.LastFrame != LastSample)
                Fail("corpus endpoint", (int)engine.CurrentTick, engine.MatchEnded);
        }

        private void Fail(string reason, int tick, bool ended, Exception inner = null)
        {
            int expectedTick = _startTick + AttemptedRunTicks;
            int expectedDelta = expectedTick >= FramesPerSeed ? 0 : 1;
            string message = $"CADENCE_FAILURE,seed=0x{_seed:X16},reason={reason}," +
                $"attemptedRunTicks={AttemptedRunTicks},completedRunTicks={CompletedRunTicks}," +
                $"expectedDelta={expectedDelta},actualDelta={CallbackCount - _callbacksBefore}," +
                $"expectedTick={expectedTick},currentTick={tick},callbacks={CallbackCount}," +
                $"firstSample={FirstSample},lastSample={LastSample},ended={ended}," +
                $"wrongCallbackTick={_wrongCallbackTick}";
            _output.WriteLine(message);
            _output.Flush();
            Console.Error.WriteLine(message);
            Console.Error.Flush();
            Console.Out.Flush();
            throw new InvalidOperationException(message, inner);
        }
    }

    private readonly struct Player
    {
        internal readonly int Id;
        internal readonly int Team;
        internal readonly bool Goalkeeper;
        internal readonly bool SentOff;
        internal readonly AgentMovementState State;
        internal readonly float X;
        internal readonly float Y;

        internal Player(int id, int team, float x, float y, AgentMovementState state,
            bool goalkeeper = false, bool sentOff = false)
        {
            Id = id;
            Team = team;
            X = x;
            Y = y;
            State = state;
            Goalkeeper = goalkeeper;
            SentOff = sentOff;
        }

        internal bool ActiveOutfielder => !Goalkeeper && !SentOff;
    }

    private sealed class Passage
    {
        internal ulong Seed;
        internal int Agent;
        internal int Team;
        internal int Start;
        internal int End;
        internal int Duration;
        internal float MinXySquared = float.PositiveInfinity;
        internal float BallXAtMin;
        internal float BallYAtMin;
        internal float BallZAtMin;
        internal float BallVzAtMin;

        internal string Csv => string.Join(",", "passage", $"0x{Seed:X16}", Agent, Team, Start,
            End, Duration, F(MathF.Sqrt(MinXySquared)), F(BallXAtMin), F(BallYAtMin),
            F(BallZAtMin), F(BallVzAtMin));
    }

    private sealed class Counts
    {
        internal long AttemptedRunTicks;
        internal long CompletedRunTicks;
        internal long CallbackFrames;
        internal long LooseFrames;
        internal long HeightBandLooseFrames;
        internal long AllFramesOutfielderObservations;
        internal long OutfielderObservations;
        internal long AnyNearFrames;
        internal long NearAgentFrames;
        internal long PassageCount;
        internal long DurationSum;
        internal readonly long[] NearByTeam = new long[2];
        // <=.18, (.18,.5], (.5,1], (1,2], >2, no active
        internal readonly long[] Nearest = new long[6];
        // rising, falling, stationary; each frame counted once, regardless of player count.
        internal readonly long[] HeightVz = new long[3];
        internal readonly long[] NearVz = new long[3];
        internal readonly Dictionary<AgentMovementState, long> NearByState = new();
        internal readonly SortedDictionary<int, long> PassageDurations = new();

        internal void Add(Counts other)
        {
            AttemptedRunTicks += other.AttemptedRunTicks;
            CompletedRunTicks += other.CompletedRunTicks;
            CallbackFrames += other.CallbackFrames;
            LooseFrames += other.LooseFrames;
            HeightBandLooseFrames += other.HeightBandLooseFrames;
            AllFramesOutfielderObservations += other.AllFramesOutfielderObservations;
            OutfielderObservations += other.OutfielderObservations;
            AnyNearFrames += other.AnyNearFrames;
            NearAgentFrames += other.NearAgentFrames;
            PassageCount += other.PassageCount;
            DurationSum += other.DurationSum;
            for (int i = 0; i < NearByTeam.Length; i++) NearByTeam[i] += other.NearByTeam[i];
            for (int i = 0; i < Nearest.Length; i++) Nearest[i] += other.Nearest[i];
            for (int i = 0; i < HeightVz.Length; i++)
            {
                HeightVz[i] += other.HeightVz[i];
                NearVz[i] += other.NearVz[i];
            }
            foreach (var entry in other.NearByState)
            {
                NearByState.TryGetValue(entry.Key, out long prior);
                NearByState[entry.Key] = prior + entry.Value;
            }
            foreach (var entry in other.PassageDurations)
            {
                PassageDurations.TryGetValue(entry.Key, out long prior);
                PassageDurations[entry.Key] = prior + entry.Value;
            }
        }

        internal void Check()
        {
            Require(Nearest.Sum() == HeightBandLooseFrames, "nearest histogram denominator");
            Require(HeightVz.Sum() == HeightBandLooseFrames, "height vz denominator");
            Require(NearVz.Sum() == AnyNearFrames, "near vz denominator");
            Require(NearByTeam.Sum() == NearAgentFrames, "team pairs denominator");
            Require(NearByState.Values.Sum() == NearAgentFrames, "state pairs denominator");
            Require(DurationSum == NearAgentFrames, "passage durations denominator");
            Require(PassageDurations.Values.Sum() == PassageCount, "passage count histogram");
            Require(PassageDurations.Sum(x => (long)x.Key * x.Value) == DurationSum,
                "passage duration histogram");
            Require(AnyNearFrames <= HeightBandLooseFrames, "near frames exceed height frames");
            Require(NearAgentFrames <= OutfielderObservations, "near pairs exceed outfielder observations");
        }

        internal string Summary(string label)
        {
            var state = string.Join("/", Enum.GetValues<AgentMovementState>()
                .Select(s => $"{s}:{(NearByState.TryGetValue(s, out long n) ? n : 0)}"));
            var durations = string.Join("/", PassageDurations.Select(x => $"{x.Key}:{x.Value}"));
            return $"{label},attemptedRunTicks={AttemptedRunTicks},completedRunTicks={CompletedRunTicks}," +
                $"callbackFrames={CallbackFrames},sampledFrames={CallbackFrames},looseFrames={LooseFrames}," +
                $"heightBandLooseFrames={HeightBandLooseFrames}," +
                $"allFramesOutfielderObservations={AllFramesOutfielderObservations}," +
                $"outfielderObservations={OutfielderObservations},anyNearFrames={AnyNearFrames}," +
                $"nearAgentFrames={NearAgentFrames},nearByTeam[0/1]={NearByTeam[0]}/{NearByTeam[1]}," +
                $"nearByState={state},nearest[.18/.5/1/2/>2/noActive]={string.Join("/", Nearest)}," +
                $"heightVz[rising/falling/stationary]={string.Join("/", HeightVz)}," +
                $"nearVz[rising/falling/stationary]={string.Join("/", NearVz)}," +
                $"passages={PassageCount},durationSum={DurationSum},durationFrames[count]={durations}";
        }
    }

    private sealed class Counter
    {
        private readonly ulong _seed;
        private readonly TextWriter _output;
        private readonly Dictionary<int, Passage> _open = new();
        private readonly HashSet<int> _qualifying = new();
        private readonly List<int> _toClose = new();
        internal readonly Counts Counts = new();
        internal readonly List<Passage> Closed = new();
        internal readonly HashSet<int> LastNearAgents = new();
        internal int LastFrame = int.MinValue;

        internal Counter(ulong seed, TextWriter output = null)
        {
            _seed = seed;
            _output = output;
        }

        internal void Observe(int frame, int possession, float ballX, float ballY, float ballZ,
            float ballVz, IReadOnlyList<Player> players)
        {
            if (LastFrame != int.MinValue && frame <= LastFrame)
                throw new InvalidOperationException("Frame order is not strictly increasing.");
            if (LastFrame != int.MinValue && frame != LastFrame + 1) CloseAll();
            LastFrame = frame;
            Counts.CallbackFrames++;
            LastNearAgents.Clear();
            _qualifying.Clear();

            bool loose = possession == MatchEngineConstants.NO_POSSESSION;
            if (loose) Counts.LooseFrames++;
            bool height = loose && ballZ >= LowerBallZ && ballZ <= UpperBallZ;
            if (height)
            {
                Counts.HeightBandLooseFrames++;
                Counts.HeightVz[VzIndex(ballVz)]++;
            }

            float nearestSq = float.PositiveInfinity;
            bool anyActive = false;
            foreach (Player player in players)
            {
                if (!player.ActiveOutfielder) continue;
                Counts.AllFramesOutfielderObservations++;
                if (!height) continue;
                Counts.OutfielderObservations++;
                anyActive = true;
                float dx = ballX - player.X;
                float dy = ballY - player.Y;
                float distanceSq = dx * dx + dy * dy;
                if (distanceSq < nearestSq) nearestSq = distanceSq;
                if (distanceSq > NearXySquared) continue;

                if ((uint)player.Team >= 2) throw new InvalidOperationException("Invalid team id.");
                if (!_qualifying.Add(player.Id)) throw new InvalidOperationException("Duplicate player observation.");
                LastNearAgents.Add(player.Id);
                Counts.NearAgentFrames++;
                Counts.NearByTeam[player.Team]++;
                Counts.NearByState.TryGetValue(player.State, out long stateCount);
                Counts.NearByState[player.State] = stateCount + 1;

                if (!_open.TryGetValue(player.Id, out Passage passage))
                {
                    passage = new Passage { Seed = _seed, Agent = player.Id, Team = player.Team,
                        Start = frame };
                    _open.Add(player.Id, passage);
                }
                else if (passage.Team != player.Team)
                {
                    throw new InvalidOperationException("Player team changed during passage.");
                }
                passage.End = frame;
                passage.Duration++;
                if (distanceSq < passage.MinXySquared)
                {
                    passage.MinXySquared = distanceSq;
                    passage.BallXAtMin = ballX;
                    passage.BallYAtMin = ballY;
                    passage.BallZAtMin = ballZ;
                    passage.BallVzAtMin = ballVz;
                }
            }

            if (height)
            {
                Counts.Nearest[anyActive ? DistanceBucket(nearestSq) : 5]++;
                if (_qualifying.Count > 0)
                {
                    Counts.AnyNearFrames++;
                    Counts.NearVz[VzIndex(ballVz)]++;
                }
            }

            _toClose.Clear();
            foreach (int id in _open.Keys)
                if (!_qualifying.Contains(id)) _toClose.Add(id);
            foreach (int id in _toClose) Close(id);
        }

        internal void Finish()
        {
            CloseAll();
            Counts.Check();
        }

        private void CloseAll()
        {
            _toClose.Clear();
            _toClose.AddRange(_open.Keys);
            foreach (int id in _toClose) Close(id);
        }

        private void Close(int id)
        {
            Passage passage = _open[id];
            _open.Remove(id);
            Require(passage.Duration == passage.End - passage.Start + 1, "noncontiguous passage");
            Counts.PassageCount++;
            Counts.DurationSum += passage.Duration;
            Counts.PassageDurations.TryGetValue(passage.Duration, out long prior);
            Counts.PassageDurations[passage.Duration] = prior + 1;
            if (_output == null) Closed.Add(passage);
            _output?.WriteLine(passage.Csv);
        }
    }

    private static int VzIndex(float vz) => vz > 0f ? 0 : vz < 0f ? 1 : 2;

    private static int DistanceBucket(float squared) =>
        squared <= 0.18f * 0.18f ? 0 :
        squared <= 0.5f * 0.5f ? 1 :
        squared <= 1.0f ? 2 :
        squared <= 2.0f * 2.0f ? 3 : 4;

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Sanity self-check failed: " + message);
    }

    private static Player P(int id, int team, float x, float y,
        AgentMovementState state = AgentMovementState.IDLE,
        bool goalkeeper = false, bool sentOff = false) =>
        new(id, team, x, y, state, goalkeeper, sentOff);

    private static void SelfTest()
    {
        var two = new Counter(1);
        two.Observe(0, MatchEngineConstants.NO_POSSESSION, 30, 20, 1.5f, 0,
            new[] { P(1, 0, 30, 20, AgentMovementState.GROUNDED),
                P(12, 1, 30, 20, AgentMovementState.STUMBLING) });
        Require(two.Counts.AnyNearFrames == 1 && two.Counts.NearAgentFrames == 2,
            "two players on one frame are one unique frame and two pairs");
        two.Finish();
        Require(two.Counts.NearByState[AgentMovementState.GROUNDED] == 1
            && two.Counts.NearByState[AgentMovementState.STUMBLING] == 1,
            "grounded and stumbling remain included");
        Require(two.Closed.Count == 2 && two.Closed.All(x => x.Duration == 1),
            "two player passage multiplicity");

        var consecutive = new Counter(1);
        for (int frame = 4; frame <= 6; frame++)
            consecutive.Observe(frame, -1, 30, 20, 2.6f, 1, new[] { P(1, 0, 30, 20) });
        consecutive.Finish();
        Require(consecutive.Closed.Count == 1 && consecutive.Closed[0].Duration == 3,
            "consecutive frame passage");
        Require(consecutive.Counts.DurationSum == consecutive.Counts.NearAgentFrames,
            "duration reconciliation");

        var gap = new Counter(1);
        gap.Observe(4, -1, 30, 20, 2f, 0, new[] { P(1, 0, 30, 20) });
        gap.Observe(6, -1, 30, 20, 2f, 0, new[] { P(1, 0, 30, 20) });
        gap.Finish();
        Require(gap.Closed.Count == 2 && gap.Closed.All(x => x.Duration == 1),
            "missing frame splits passage");

        var edges = new Counter(1);
        Player one = P(1, 0, 0, 0);
        edges.Observe(0, -1, 1f, 0, LowerBallZ, 1, new[] { one });
        edges.Observe(1, -1, 1f, 0, UpperBallZ, -1, new[] { one });
        edges.Observe(2, -1, 1f, 0, MathF.BitDecrement(LowerBallZ), 0, new[] { one });
        edges.Observe(3, -1, 1f, 0, MathF.BitIncrement(UpperBallZ), 0, new[] { one });
        edges.Observe(4, -1, MathF.BitIncrement(1f), 0, 2f, 0, new[] { one });
        edges.Observe(5, -1, 0, 0, 2f, 0, new[] { P(1, 0, 0, 0, sentOff: true) });
        edges.Observe(6, -1, 0, 0, 2f, 0, new[] { P(1, 0, 0, 0, goalkeeper: true) });
        edges.Observe(7, 1, 0, 0, 2f, 0, new[] { one });
        edges.Finish();
        Require(edges.Counts.CallbackFrames == 8 && edges.Counts.LooseFrames == 7
            && edges.Counts.HeightBandLooseFrames == 5
            && edges.Counts.OutfielderObservations == 3
            && edges.Counts.AnyNearFrames == 2 && edges.Counts.NearAgentFrames == 2,
            "exact Z/XY boundaries and possession/GK/sent-off exclusions");
        Require(edges.Counts.Nearest[2] == 2 && edges.Counts.Nearest[3] == 1
            && edges.Counts.Nearest[5] == 2, "nearest histogram before 1m cutoff");
        Require(edges.Counts.HeightVz[0] == 1 && edges.Counts.HeightVz[1] == 1
            && edges.Counts.HeightVz[2] == 3, "strict vz denominator and zero bucket");

        var home = new Counter(1);
        var away = new Counter(1);
        home.Observe(0, -1, 30, 20, 2f, 0, new[] { P(1, 0, 30.5f, 20) });
        away.Observe(0, -1, 75, 48, 2f, 0, new[] { P(12, 1, 74.5f, 48) });
        home.Finish();
        away.Finish();
        Require(home.Counts.NearAgentFrames == away.Counts.NearAgentFrames
            && home.Closed[0].MinXySquared == away.Closed[0].MinXySquared
            && home.Counts.NearByTeam[0] == away.Counts.NearByTeam[1],
            "mirrored home/away geometry");

        // This invokes the real RunTick → Physics → DriveGkHeadingPhysics callback, rather
        // than calling Counter.Observe directly. The staged ball supplies only a positive world.
        var engine = new MatchEngine(0x441UL);
        engine.EnableGkHeading();
        int chosen = -1;
        for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            if (!engine.AgentIsGoalkeeper(i) && engine.AgentTeamId(i) == 0) { chosen = i; break; }
        Require(chosen >= 0, "real engine has home outfielder");
        AgentState staged = engine.AgentView(chosen);
        staged.CurrentState = AgentMovementState.IDLE;
        staged.Velocity = UnityEngine.Vector2.zero;
        engine.TestOnly_SetAgent(chosen, in staged);
        engine.TestOnly_SetPhysicsFrame(24);
        engine.TestOnly_ForceBallLoose(new UnityEngine.Vector3(staged.Position.x,
            staged.Position.y, 2f), UnityEngine.Vector3.zero);
        var real = new Counter(0x441UL);
        engine.TestOnly_HeaderArrivalObserver = () => ObserveEngine(engine, real);
        engine.RunTick();
        engine.TestOnly_HeaderArrivalObserver = null;
        Require(real.Counts.CallbackFrames == 1 && real.LastNearAgents.Contains(chosen),
            "real pre-Heading callback observed staged positive world");
        real.Finish();

        CadenceSelfTest();
        Console.WriteLine("SELF_TEST,PASS,syntheticCases=6,realRunTickCallback=1," +
            "realCadenceControls=2,cadenceRejectionControls=4");
    }

    private static void CadenceSelfTest()
    {
        CheckRealCadence(0, new[] { 1, 2 }, new[] { 1, 1 }, new[] { false, false });
        CheckRealCadence(323997, new[] { 323998, 323999, 324000, 324001 },
            new[] { 1, 1, 0, 0 }, new[] { false, false, true, true });
        RejectCadence(0, Array.Empty<int>(), 1, false, "skipped live callback");
        RejectCadence(0, new[] { 1, 1 }, 1, false, "duplicate live callback");
        RejectCadence(323999, new[] { 324000 }, 324000, true, "callback at full-time");
        RejectCadence(0, new[] { 2 }, 1, false, "wrong callback tick");
    }

    private static void CheckRealCadence(int startTick, int[] expectedTicks, int[] expectedDeltas,
        bool[] expectedEnded)
    {
        const ulong seed = 0x441UL;
        var engine = new MatchEngine(seed);
        engine.EnableGkHeading();
        if (startTick != 0) engine.TestOnly_SetPhysicsFrame(startTick);
        var counter = new Counter(seed);
        var cadence = new Cadence(seed, TextWriter.Null, startTick);
        var callbacks = new List<int>();
        engine.TestOnly_HeaderArrivalObserver = () =>
        {
            callbacks.Add((int)engine.CurrentTick);
            ObserveEngine(engine, counter, cadence);
        };
        for (int i = 0; i < expectedTicks.Length; i++)
        {
            long before = cadence.CallbackCount;
            cadence.RunTick(engine);
            Require((int)engine.CurrentTick == expectedTicks[i] && engine.MatchEnded == expectedEnded[i]
                && cadence.CallbackCount - before == expectedDeltas[i], "real cadence boundary step");
        }
        engine.TestOnly_HeaderArrivalObserver = null;
        int[] expectedCallbacks = expectedTicks.Where((_, i) => expectedDeltas[i] == 1).ToArray();
        Require(callbacks.SequenceEqual(expectedCallbacks), "exact real callback sequence");
        Require(cadence.AttemptedRunTicks == expectedTicks.Length
            && cadence.CompletedRunTicks == expectedTicks.Length
            && counter.Counts.CallbackFrames == expectedCallbacks.Length,
            "real cadence call and sample totals");
        counter.Finish();
        Console.WriteLine($"CADENCE_CONTROL,PASS,startTick={startTick}," +
            $"callbacks={string.Join("/", callbacks)},deltas={string.Join("/", expectedDeltas)}," +
            $"ticks={string.Join("/", expectedTicks)},ended={string.Join("/", expectedEnded)}");
    }

    private static void RejectCadence(int startTick, int[] callbackTicks, int tick, bool ended,
        string label)
    {
        using var diagnostic = new StringWriter(CultureInfo.InvariantCulture);
        var cadence = new Cadence(0x441UL, diagnostic, startTick);
        cadence.BeginAttempt();
        foreach (int callbackTick in callbackTicks) cadence.ObserveCallback(callbackTick);
        bool rejected = false;
        try { cadence.CompleteAttempt(tick, ended); }
        catch (InvalidOperationException exception)
        {
            rejected = exception.Message.StartsWith("CADENCE_FAILURE,", StringComparison.Ordinal)
                && diagnostic.ToString().Contains(exception.Message, StringComparison.Ordinal);
        }
        Require(rejected, label + " rejected with diagnostics");
        Console.WriteLine("CADENCE_REJECTION,PASS," + label);
    }

    private static void ObserveEngine(MatchEngine engine, Counter counter, Cadence cadence = null)
    {
        cadence?.ObserveCallback((int)engine.CurrentTick);
        var ball = engine.BallView;
        var players = new Player[MatchEngineConstants.SQUAD_SIZE];
        for (int i = 0; i < players.Length; i++)
        {
            AgentState agent = engine.AgentView(i);
            players[i] = P(i, engine.AgentTeamId(i), agent.Position.x, agent.Position.y,
                agent.CurrentState, engine.AgentIsGoalkeeper(i), engine.TestOnly_IsSentOff(i));
        }
        counter.Observe((int)engine.CurrentTick, engine.TestOnly_PossessingAgentId,
            ball.Position.x, ball.Position.y, ball.Position.z, ball.Velocity.z, players);
    }

    private static void Corpus(string outputPath)
    {
        string fullPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        using var writer = new StreamWriter(fullPath, false);
        writer.WriteLine("#441 independent pre-Heading proximity sanity; explicit local diagnostic");
        writer.WriteLine("definitions,loose=NO_POSSESSION,heightZ=[1.5,2.6],active=outfield+notSentOff,nearXY2<=1,states=all");
        writer.WriteLine("passage,seed,agent,team,startFrame,endFrame,durationFrames,minXYm,ballXAtMin,ballYAtMin,ballZAtMin,ballVzAtMin");
        var aggregate = new Counts();
        for (int index = 0; index < Seeds.Length; index++)
        {
            ulong seed = Seeds[index];
            var engine = new MatchEngine(seed);
            engine.EnableGkHeading();
            var counter = new Counter(seed, writer);
            var cadence = new Cadence(seed, writer);
            engine.TestOnly_HeaderArrivalObserver = () => ObserveEngine(engine, counter, cadence);
            for (int i = 0; i < FramesPerSeed; i++)
            {
                cadence.RunTick(engine);
                if (cadence.AttemptedRunTicks % ProgressInterval == 0)
                {
                    Console.WriteLine($"progress,seed=0x{seed:X16},attemptedRunTicks={cadence.AttemptedRunTicks}," +
                        $"callbacks={cadence.CallbackCount},currentTick={engine.CurrentTick}");
                    Console.Out.Flush();
                }
            }
            engine.TestOnly_HeaderArrivalObserver = null;
            cadence.CheckCorpusEnd(engine, counter);
            counter.Counts.AttemptedRunTicks = cadence.AttemptedRunTicks;
            counter.Counts.CompletedRunTicks = cadence.CompletedRunTicks;
            counter.Finish();
            string digest = Convert.ToHexString(engine.CurrentSnapshotDigest);
            Require(digest == Run3Digests[index], "Run 3 pinned digest mismatch for seed " + seed);
            writer.WriteLine(counter.Counts.Summary($"seed,0x{seed:X16}"));
            writer.WriteLine($"seedEnd,0x{seed:X16},tick={engine.CurrentTick}," +
                $"firstSample={cadence.FirstSample},lastSample={cadence.LastSample},ended={engine.MatchEnded},digest={digest}");
            writer.Flush();
            Console.WriteLine($"seedEnd,0x{seed:X16},tick={engine.CurrentTick},digest={digest}");
            aggregate.Add(counter.Counts);
        }
        aggregate.Check();
        writer.WriteLine(aggregate.Summary("AGGREGATE"));
        Console.WriteLine(aggregate.Summary("AGGREGATE"));
        Console.WriteLine("output=" + fullPath);
    }

    private static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--self-test") SelfTest();
        else if (args.Length == 2 && args[0] == "--corpus") Corpus(args[1]);
        else
        {
            Console.Error.WriteLine("Usage: Probe --self-test | --corpus <ignored Temp output path>");
            return 2;
        }
        return 0;
    }
}
