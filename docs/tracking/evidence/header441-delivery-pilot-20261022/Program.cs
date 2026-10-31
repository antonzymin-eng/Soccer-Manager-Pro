// File:     docs/tracking/evidence/header441-delivery-pilot-20261022/Program.cs
// Created:  2026-10-22
// Author:   —
// Purpose:  Explicit read-only #441 seed-1 prefix trace of observed ball-event provenance
//           at every independent broad height/proximity encounter; no gameplay consumer.
// Run:      dotnet build docs/tracking/evidence/header441-delivery-pilot-20261022/Probe.csproj -p:NuGetAudit=false --ignore-failed-sources -m:1 -nodeReuse:false -p:UseSharedCompilation=false --verbosity minimal
//           dotnet docs/tracking/evidence/header441-delivery-pilot-20261022/bin/Debug/net8.0/TacticalDirector.MatchEngine.Tests.dll --self-test
//           dotnet docs/tracking/evidence/header441-delivery-pilot-20261022/bin/Debug/net8.0/TacticalDirector.MatchEngine.Tests.dll --pilot Temp/header441-delivery-pilot-20261022.txt
// History:  2026-10-22 — Initial bounded pilot with synthetic and real-hook controls.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;

using TacticalDirector.AgentMovement;
using TacticalDirector.MatchEngine;
using UnityEngine;

internal static class Program
{
    private const ulong Seed = 0x0000000000000001UL;
    private const int LastTick = 125500;
    private const int ProgressStride = 12500;
    private const float MinZ = 1.5f;
    private const float MaxZ = 2.6f;
    private const float MaxXySquared = 1f;
    private const string ExpectedDigest = "CF64C07C29EDDE36ED456865DCA3CDE20CD6148D576C59EAC57D3FF8E2808E00";

    private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);
    private static string V(Vector3 value) => F(value.x) + "," + F(value.y) + "," + F(value.z);
    private static string XY(Vector2 value) => F(value.x) + "," + F(value.y);
    private static string Id(int value) => value < 0 ? "NA" : value.ToString(CultureInfo.InvariantCulture);
    private static void Require(bool condition, string reason)
    {
        if (!condition) throw new InvalidOperationException("PILOT_FAILURE," + reason);
    }

    private readonly struct Player
    {
        internal readonly int Id;
        internal readonly int Team;
        internal readonly bool IsGoalkeeper;
        internal readonly bool IsSentOff;
        internal readonly AgentMovementState State;
        internal readonly Vector2 Position;
        internal readonly Vector2 Velocity;

        internal Player(int id, int team, bool goalkeeper, bool sentOff,
            AgentMovementState state, Vector2 position, Vector2 velocity)
        {
            Id = id;
            Team = team;
            IsGoalkeeper = goalkeeper;
            IsSentOff = sentOff;
            State = state;
            Position = position;
            Velocity = velocity;
        }

        internal bool ActiveOutfielder => !IsGoalkeeper && !IsSentOff;
    }

    private sealed class Source
    {
        internal int Sequence;
        internal int Frame;
        internal W8StageAKind Kind;
        internal int Actor = -1;
        internal int ActorTeam = -1;
        internal int Target = -1;
        internal string EventPhase;
        internal Vector3 EventBallPosition;
        internal Vector3 EventBallVelocity;
        internal bool HasPostMutationSample;
        internal string PostSamplePhase = "NA";
        internal Vector3 PostBallPosition;
        internal Vector3 PostBallVelocity;
        internal Player[] PlayersAtEvent;
        internal bool PairAmbiguous;
        internal bool ReversalSinceSource;
    }

    private sealed class SourceTracker
    {
        private readonly TextWriter _writer;
        private int _sequence;
        private Source _pendingKick;
        internal Source Latest { get; private set; }
        internal int ReversalSignals { get; private set; }
        internal int UnpairedPostKickSignals { get; private set; }
        internal int EventCount => _sequence;

        internal SourceTracker(TextWriter writer) => _writer = writer;

        private static bool ValidAgent(int id, Player[] players) =>
            id >= 0 && id < players.Length && players[id].Id == id;

        private static bool IsKick(W8StageAKind kind) =>
            kind == W8StageAKind.PassKick || kind == W8StageAKind.ShotKick
            || kind == W8StageAKind.GkHeadingKick;

        private static bool IsSource(W8StageAKind kind) => IsKick(kind)
            || kind == W8StageAKind.FirstTouch
            || kind == W8StageAKind.UnattributedDeflection
            || kind == W8StageAKind.Restart
            || kind == W8StageAKind.SixSecondDrop;

        private static string EventPhase(W8StageAKind kind) =>
            kind == W8StageAKind.FirstTouch || kind == W8StageAKind.UnattributedDeflection
                ? "postMutation" : kind == W8StageAKind.Release
                    ? "preMutation" : "preMutationOrControl";

        internal void Observe(W8StageAEvent evt, MatchEngine engine)
        {
            _sequence++;
            if (evt.Kind == W8StageAKind.TacticalBefore
                || evt.Kind == W8StageAKind.TacticalAfter
                || evt.Kind == W8StageAKind.KeeperDecision)
                return;
            Player[] players = CapturePlayers(engine);
            var ball = engine.BallView;
            int currentHolder = engine.TestOnly_PossessingAgentId;
            _writer?.WriteLine($"event,seq={_sequence},frame={evt.Frame},kind={evt.Kind}," +
                $"phase={EventPhase(evt.Kind)},rawAgent={evt.Agent},rawOther={evt.Other}," +
                $"holderBefore={evt.HolderBefore},holderObserved={currentHolder}," +
                $"eventBallPos={V(evt.BallPosition)},observedBallPos={V(ball.Position)}," +
                $"observedBallVel={V(ball.Velocity)}");

            if (IsSource(evt.Kind))
            {
                // Restart.Agent is an awarded TEAM, deflection has no actor, and
                // FirstTouch.Other is an outcome enum. Only PassKick.Other exposes an
                // existing intended target id; never index a negative/invalid value.
                int actor = evt.Kind == W8StageAKind.Restart
                    || evt.Kind == W8StageAKind.UnattributedDeflection
                    || !ValidAgent(evt.Agent, players) ? -1 : evt.Agent;
                int target = evt.Kind == W8StageAKind.PassKick
                    && ValidAgent(evt.Other, players) ? evt.Other : -1;
                Source source = SetSource(_sequence, evt.Frame, evt.Kind, actor, target,
                    EventPhase(evt.Kind), evt.BallPosition, ball.Velocity, players);
                if (IsKick(evt.Kind))
                {
                    if (_pendingKick != null && _pendingKick.Frame == evt.Frame)
                    {
                        _pendingKick.PairAmbiguous = true;
                        source.PairAmbiguous = true;
                    }
                    _pendingKick = source;
                }
                else _pendingKick = null;
                return;
            }

            if (evt.Kind == W8StageAKind.KickRelease)
            {
                bool paired = _pendingKick != null && ReferenceEquals(Latest, _pendingKick)
                    && !_pendingKick.PairAmbiguous && _pendingKick.Frame == evt.Frame
                    && _pendingKick.Actor >= 0 && _pendingKick.Actor == evt.Agent;
                if (paired)
                    RecordPostSample(_pendingKick, "postKickRelease", ball.Position, ball.Velocity);
                else
                {
                    UnpairedPostKickSignals++;
                    _writer?.WriteLine($"sourcePair,seq={_sequence},frame={evt.Frame}," +
                        $"status=unpairedOrAmbiguous,rawAgent={evt.Agent}," +
                        $"pendingSeq={Id(_pendingKick?.Sequence ?? -1)}");
                }
                _pendingKick = null;
                return;
            }

            if (evt.Kind == W8StageAKind.Acquire)
            {
                if (Latest != null && Latest.Kind == W8StageAKind.Restart
                    && Latest.Frame == evt.Frame)
                    RecordPostSample(Latest, "postRestartAcquire", ball.Position, ball.Velocity);
                else
                {
                    _writer?.WriteLine($"sourceReset,seq={_sequence},frame={evt.Frame},reason=Acquire");
                    Latest = null;
                }
                _pendingKick = null;
            }
            else if (evt.Kind == W8StageAKind.Release)
            {
                if (Latest != null && Latest.Kind == W8StageAKind.SixSecondDrop
                    && Latest.Frame == evt.Frame && Latest.Actor == evt.Agent)
                    _writer?.WriteLine($"sourcePending,sourceSeq={Latest.Sequence}," +
                        $"frame={evt.Frame},reason=ReleaseObservedBeforeMutation,postSample=Unknown");
                else
                {
                    _writer?.WriteLine($"sourceReset,seq={_sequence},frame={evt.Frame},reason=Release");
                    Latest = null;
                }
                _pendingKick = null;
            }
        }

        private Source SetSource(int sequence, int frame, W8StageAKind kind, int actor,
            int target, string phase, Vector3 ballPosition, Vector3 ballVelocity, Player[] players)
        {
            var source = new Source
            {
                Sequence = sequence, Frame = frame, Kind = kind, Actor = actor,
                ActorTeam = actor >= 0 ? players[actor].Team : -1,
                Target = target, EventPhase = phase, EventBallPosition = ballPosition,
                EventBallVelocity = ballVelocity, PlayersAtEvent = players
            };
            Latest = source;
            // Only these callbacks occur after their named mutation. A pre-kick or
            // pre-restart BallView is NOT an observed launch velocity.
            if (phase == "postMutation")
                RecordPostSample(source, "eventPostMutation", ballPosition, ballVelocity);
            _writer?.WriteLine($"source,seq={sequence},frame={frame},kind={kind}," +
                $"actor={Id(actor)},actorTeam={Id(source.ActorTeam)},target={Id(target)}," +
                $"phase={phase},eventBallPos={V(ballPosition)},eventBallVel={V(ballVelocity)}," +
                $"postSample={source.PostSamplePhase}");
            for (int i = 0; i < players.Length; i++)
            {
                Player player = players[i];
                _writer?.WriteLine($"sourcePose,sourceSeq={sequence},frame={frame}," +
                    $"agent={player.Id},team={player.Team},activeOutfielder={player.ActiveOutfielder}," +
                    $"state={player.State},xy={XY(player.Position)},velocityXY={XY(player.Velocity)}");
            }
            return source;
        }

        private void RecordPostSample(Source source, string phase, Vector3 position, Vector3 velocity)
        {
            source.HasPostMutationSample = true;
            source.PostSamplePhase = phase;
            source.PostBallPosition = position;
            source.PostBallVelocity = velocity;
            _writer?.WriteLine($"sourcePost,sourceSeq={source.Sequence},frame={source.Frame}," +
                $"phase={phase},ballPos={V(position)},ballVel={V(velocity)}");
        }

        internal void NoteReversal(int frame, Vector3 previous, Vector3 current)
        {
            if (Vector3.Dot(previous, current) >= 0f) return;
            ReversalSignals++;
            if (Latest != null) Latest.ReversalSinceSource = true;
            _writer?.WriteLine($"signal,frame={frame},kind=sampledVelocityReversal," +
                $"sourceSeq={Id(Latest?.Sequence ?? -1)},previous={V(previous)},current={V(current)}," +
                "interpretation=unattributedPossibleMutationOrPhysics");
        }

    }

    private sealed class Passage
    {
        internal int Agent;
        internal int Team;
        internal int Start;
        internal int End;
        internal int Duration;
        internal int MinFrame;
        internal float MinSquared = float.PositiveInfinity;
        internal Vector3 BallAtMin;
        internal Vector3 BallVelocityAtMin;
        internal int SourceAtStart = -1;
        internal int SourceAtMin = -1;
        internal int SourceAtEnd = -1;
    }

    private sealed class Counter
    {
        private readonly TextWriter _writer;
        private readonly Dictionary<int, Passage> _open = new();
        private readonly List<int> _close = new();
        private readonly HashSet<int> _near = new();
        internal int LastFrame = -1;
        internal int CallbackFrames;
        internal int HeightBandLooseFrames;
        internal int NearFrames;
        internal int NearPairs;
        internal int PassageCount;
        internal int PassageDurationSum;
        internal int SourceChangesWithinPassages;

        internal Counter(TextWriter writer) => _writer = writer;

        internal void Observe(int frame, int possession, Vector3 ballPosition,
            Vector3 ballVelocity, Player[] players, Source source)
        {
            Require(frame > LastFrame, "callback frame not strictly increasing");
            if (LastFrame >= 0 && frame != LastFrame + 1) CloseAll();
            LastFrame = frame;
            CallbackFrames++;
            _near.Clear();
            bool inBand = possession == MatchEngineConstants.NO_POSSESSION
                && ballPosition.z >= MinZ && ballPosition.z <= MaxZ;
            if (inBand) HeightBandLooseFrames++;
            foreach (Player player in players)
            {
                if (!inBand || !player.ActiveOutfielder) continue;
                float dx = ballPosition.x - player.Position.x;
                float dy = ballPosition.y - player.Position.y;
                float squared = dx * dx + dy * dy;
                if (squared > MaxXySquared) continue;
                Require(_near.Add(player.Id), "duplicate player id in callback");
                NearPairs++;
                int sourceSeq = source?.Sequence ?? -1;
                string relation = Relation(source, player.Id);
                Player atSource = source != null && (uint)player.Id < (uint)source.PlayersAtEvent.Length
                    ? source.PlayersAtEvent[player.Id] : default;
                string agentAtSource = source != null && atSource.Id == player.Id
                    ? $"{XY(atSource.Position)},{XY(atSource.Velocity)},{atSource.State}" : "NA,NA,NA,NA,NA";
                _writer?.WriteLine($"near,frame={frame},agent={player.Id},team={player.Team}," +
                    $"xyDistance={F(MathF.Sqrt(squared))},ballPos={V(ballPosition)}," +
                    $"ballVel={V(ballVelocity)},agentXY={XY(player.Position)}," +
                    $"agentVelXY={XY(player.Velocity)},state={player.State}," +
                    $"sourceSeq={Id(sourceSeq)},sourceFrame={Id(source?.Frame ?? -1)}," +
                    $"sourceAgeFrames={(source == null ? "NA" : (frame - source.Frame).ToString(CultureInfo.InvariantCulture))}," +
                    $"sourceKind={source?.Kind.ToString() ?? "Unknown"}," +
                    $"sourceActor={Id(source?.Actor ?? -1)},sourceTeam={Id(source?.ActorTeam ?? -1)}," +
                    $"sourceTarget={Id(source?.Target ?? -1)},relation={relation}," +
                    $"sourceEventPhase={source?.EventPhase ?? "NA"}," +
                    $"sourceEventBallPos={(source == null ? "NA,NA,NA" : V(source.EventBallPosition))}," +
                    $"sourceEventBallVel={(source == null ? "NA,NA,NA" : V(source.EventBallVelocity))}," +
                    $"sourcePostPhase={source?.PostSamplePhase ?? "NA"}," +
                    $"sourcePostBallPos={(source != null && source.HasPostMutationSample ? V(source.PostBallPosition) : "NA,NA,NA")}," +
                    $"sourcePostBallVel={(source != null && source.HasPostMutationSample ? V(source.PostBallVelocity) : "NA,NA,NA")}," +
                    $"agentAtSourceXYVelState={agentAtSource}," +
                    $"sampledReversalSinceSource={source?.ReversalSinceSource ?? false}");

                if (!_open.TryGetValue(player.Id, out Passage passage))
                {
                    passage = new Passage { Agent = player.Id, Team = player.Team,
                        Start = frame, SourceAtStart = sourceSeq };
                    _open.Add(player.Id, passage);
                }
                else
                {
                    Require(passage.Team == player.Team, "team changed inside passage");
                    if (passage.SourceAtEnd != sourceSeq) SourceChangesWithinPassages++;
                }
                passage.End = frame;
                passage.Duration++;
                passage.SourceAtEnd = sourceSeq;
                if (squared < passage.MinSquared)
                {
                    passage.MinSquared = squared;
                    passage.MinFrame = frame;
                    passage.BallAtMin = ballPosition;
                    passage.BallVelocityAtMin = ballVelocity;
                    passage.SourceAtMin = sourceSeq;
                }
            }
            if (_near.Count > 0) NearFrames++;
            _close.Clear();
            foreach (int agent in _open.Keys)
                if (!_near.Contains(agent)) _close.Add(agent);
            foreach (int agent in _close) Close(agent);
        }

        internal static string Relation(Source source, int agent) => source == null
            || source.Actor < 0 ? "Unknown" : source.Actor == agent ? "SameObservedActor" : "OtherObservedActor";

        internal void Finish()
        {
            CloseAll();
            Require(PassageDurationSum == NearPairs, "passage duration != near pair count");
            Require(NearFrames <= NearPairs && NearPairs <= CallbackFrames * MatchEngineConstants.SQUAD_SIZE,
                "near frame/pair reconciliation");
        }

        private void CloseAll()
        {
            _close.Clear();
            foreach (int agent in _open.Keys) _close.Add(agent);
            foreach (int agent in _close) Close(agent);
        }

        private void Close(int agent)
        {
            Passage passage = _open[agent];
            _open.Remove(agent);
            Require(passage.Duration == passage.End - passage.Start + 1,
                "nonconsecutive passage without gap split");
            PassageCount++;
            PassageDurationSum += passage.Duration;
            _writer?.WriteLine($"passage,agent={passage.Agent},team={passage.Team}," +
                $"start={passage.Start},end={passage.End},duration={passage.Duration}," +
                $"minFrame={passage.MinFrame},minXY={F(MathF.Sqrt(passage.MinSquared))}," +
                $"ballAtMin={V(passage.BallAtMin)},ballVelocityAtMin={V(passage.BallVelocityAtMin)}," +
                $"sourceSeqStart={Id(passage.SourceAtStart)}," +
                $"sourceSeqMin={Id(passage.SourceAtMin)},sourceSeqEnd={Id(passage.SourceAtEnd)}");
        }
    }

    private static Player[] CapturePlayers(MatchEngine engine)
    {
        var players = new Player[MatchEngineConstants.SQUAD_SIZE];
        for (int i = 0; i < players.Length; i++)
        {
            AgentState agent = engine.AgentView(i);
            players[i] = new Player(i, engine.AgentTeamId(i), engine.AgentIsGoalkeeper(i),
                engine.TestOnly_IsSentOff(i), agent.CurrentState, agent.Position, agent.Velocity);
        }
        return players;
    }

    private static void ObserveWorld(MatchEngine engine, Counter counter,
        SourceTracker sources, ref Vector3 previousVelocity, ref bool hasPreviousVelocity)
    {
        var ball = engine.BallView;
        if (hasPreviousVelocity)
            sources.NoteReversal((int)engine.CurrentTick, previousVelocity, ball.Velocity);
        previousVelocity = ball.Velocity;
        hasPreviousVelocity = true;
        counter.Observe((int)engine.CurrentTick, engine.TestOnly_PossessingAgentId,
            ball.Position, ball.Velocity, CapturePlayers(engine), sources.Latest);
    }

    private static void SelfTest()
    {
        Player home = new(0, 0, false, false, AgentMovementState.GROUNDED,
            Vector2.zero, Vector2.zero);
        Player away = new(1, 1, false, false, AgentMovementState.STUMBLING,
            Vector2.zero, Vector2.zero);
        Player[] two = { home, away };
        var count = new Counter(null);
        count.Observe(1, MatchEngineConstants.NO_POSSESSION,
            new Vector3(1f, 0f, MinZ), Vector3.zero, two, null);
        count.Observe(2, MatchEngineConstants.NO_POSSESSION,
            new Vector3(0.5f, 0f, MaxZ), Vector3.zero, two, null);
        count.Observe(3, MatchEngineConstants.NO_POSSESSION,
            new Vector3(MathF.BitIncrement(1f), 0f, 2f), Vector3.zero, two, null);
        count.Observe(4, MatchEngineConstants.NO_POSSESSION,
            new Vector3(0f, 0f, MathF.BitDecrement(MinZ)), Vector3.zero, two, null);
        count.Observe(5, 0, new Vector3(0f, 0f, 2f), Vector3.zero, two, null);
        count.Observe(6, MatchEngineConstants.NO_POSSESSION,
            new Vector3(0f, 0f, 2f), Vector3.zero,
            new[] { new Player(0, 0, true, false, AgentMovementState.IDLE, Vector2.zero, Vector2.zero),
                new Player(1, 1, false, true, AgentMovementState.IDLE, Vector2.zero, Vector2.zero) }, null);
        count.Observe(7, MatchEngineConstants.NO_POSSESSION,
            new Vector3(0f, 0f, MathF.BitIncrement(MaxZ)), Vector3.zero, two, null);
        count.Finish();
        Require(count.CallbackFrames == 7 && count.HeightBandLooseFrames == 4
            && count.NearFrames == 2 && count.NearPairs == 4
            && count.PassageCount == 2 && count.PassageDurationSum == 4,
            "inclusive boundaries, exclusions, grounded/stumbling, multiagent counts");
        Require(Counter.Relation(null, 0) == "Unknown", "no observed source is Unknown");
        var mirroredHome = new Counter(null);
        var mirroredAway = new Counter(null);
        mirroredHome.Observe(1, -1, new Vector3(30f, 20f, 2f), Vector3.zero,
            new[] { new Player(0, 0, false, false, AgentMovementState.IDLE,
                new Vector2(30.5f, 20f), Vector2.zero) }, null);
        mirroredAway.Observe(1, -1, new Vector3(75f, 48f, 2f), Vector3.zero,
            new[] { new Player(1, 1, false, false, AgentMovementState.IDLE,
                new Vector2(74.5f, 48f), Vector2.zero) }, null);
        mirroredHome.Finish();
        mirroredAway.Finish();
        Require(mirroredHome.NearPairs == 1 && mirroredAway.NearPairs == 1,
            "mirrored home/away world-coordinate geometry");

        using var syntheticRows = new StringWriter(CultureInfo.InvariantCulture);
        var sources = new SourceTracker(syntheticRows);
        var passage = new Counter(syntheticRows);
        var sourceEngine = new MatchEngine(Seed);
        W8StageAEvent Event(W8StageAKind kind, int frame, int actor, int other = -1) =>
            new(kind, frame, actor, other, default, sourceEngine.BallView.Position,
                sourceEngine.TestOnly_PossessingAgentId);
        sources.Observe(Event(W8StageAKind.PassKick, 7, 0, 1), sourceEngine);
        Source first = sources.Latest;
        Require(SourceTrackerTestActor(first, 0, sourceEngine.AgentTeamId(0))
            && first.Target == 1 && Counter.Relation(first, 0) == "SameObservedActor"
            && Counter.Relation(first, 1) == "OtherObservedActor", "valid source actor identity");
        passage.Observe(8, -1, new Vector3(0.5f, 0f, 2f), Vector3.zero,
            new[] { home }, first);
        passage.Observe(9, -1, new Vector3(0.5f, 0f, 2f), Vector3.zero,
            new[] { home }, first);
        sources.Observe(Event(W8StageAKind.UnattributedDeflection, 10, -1), sourceEngine);
        Source deflection = sources.Latest;
        Require(deflection.Actor == -1 && Counter.Relation(deflection, 0) == "Unknown",
            "unattributed deflection never indexes invalid actor");
        passage.Observe(10, -1, new Vector3(0.75f, 0f, 2f), Vector3.zero,
            new[] { home }, deflection);
        passage.Observe(11, -1, new Vector3(1.1f, 0f, 2f), Vector3.zero,
            new[] { home }, deflection);
        passage.Finish();
        Require(passage.PassageCount == 1 && passage.PassageDurationSum == 3
            && passage.SourceChangesWithinPassages == 1
            && syntheticRows.ToString().Contains("minFrame=8", StringComparison.Ordinal)
            && syntheticRows.ToString().Contains("sourceSeqStart=1,sourceSeqMin=1,sourceSeqEnd=2",
                StringComparison.Ordinal), "earliest tied minimum and within-passage source change");
        sources.Observe(Event(W8StageAKind.PassKick, 20, 0), sourceEngine);
        Source wrongActor = sources.Latest;
        sources.Observe(Event(W8StageAKind.KickRelease, 20, 1), sourceEngine);
        Require(!wrongActor.HasPostMutationSample && sources.UnpairedPostKickSignals == 1,
            "wrong actor cannot fabricate post-kick sample");
        sources.Observe(Event(W8StageAKind.ShotKick, 21, 0), sourceEngine);
        Source wrongFrame = sources.Latest;
        sources.Observe(Event(W8StageAKind.KickRelease, 22, 0), sourceEngine);
        Require(!wrongFrame.HasPostMutationSample && sources.UnpairedPostKickSignals == 2,
            "wrong frame cannot fabricate post-kick sample");
        sources.Observe(Event(W8StageAKind.PassKick, 23, 0), sourceEngine);
        Source noPair = sources.Latest;
        sources.Observe(Event(W8StageAKind.UnattributedDeflection, 24, -1), sourceEngine);
        Require(!noPair.HasPostMutationSample,
            "kick with no release remains unpaired after an intervening mutation");
        sources.Observe(Event(W8StageAKind.GkHeadingKick, 25, 0), sourceEngine);
        Source matched = sources.Latest;
        Require(!matched.HasPostMutationSample, "pre-kick event alone has no post sample");
        sources.Observe(Event(W8StageAKind.KickRelease, 25, 0), sourceEngine);
        Require(matched.HasPostMutationSample && matched.PostSamplePhase == "postKickRelease",
            "matching kick release yields post-mutation observed sample");
        sources.Observe(Event(W8StageAKind.PassKick, 26, 0), sourceEngine);
        Source firstSameFrame = sources.Latest;
        sources.Observe(Event(W8StageAKind.ShotKick, 26, 0), sourceEngine);
        Source secondSameFrame = sources.Latest;
        sources.Observe(Event(W8StageAKind.KickRelease, 26, 0), sourceEngine);
        Require(!firstSameFrame.HasPostMutationSample && !secondSameFrame.HasPostMutationSample
            && sources.UnpairedPostKickSignals == 3,
            "ambiguous same-frame kicks cannot fabricate a paired post sample");
        Require(syntheticRows.ToString().Contains("status=unpairedOrAmbiguous",
            StringComparison.Ordinal), "actual W8 handler emits unpaired diagnostics");

        // Stage visibly different pre- and post-kick worlds through the real
        // Observe handler for EACH supported kick kind. The prior near row is
        // already serialized before KickRelease, so it must not be retrofilled.
        W8StageAKind[] kickKinds =
        {
            W8StageAKind.PassKick, W8StageAKind.ShotKick, W8StageAKind.GkHeadingKick
        };
        for (int index = 0; index < kickKinds.Length; index++)
        {
            W8StageAKind kind = kickKinds[index];
            int frame = 40 + index * 10;
            var kickEngine = new MatchEngine(Seed);
            using var kickRows = new StringWriter(CultureInfo.InvariantCulture);
            var kickTracker = new SourceTracker(kickRows);
            var kickCounter = new Counter(kickRows);
            Vector3 prePosition = new(10f + index, 20f + index, 2f);
            Vector3 preVelocity = new(1f + index, 2f + index, 3f + index);
            Vector3 postPosition = new(30f + index, 40f + index, 2.2f);
            Vector3 postVelocity = new(4f + index, 5f + index, 6f + index);
            kickEngine.TestOnly_ForceBallLoose(prePosition, preVelocity);
            var observedPre = kickEngine.BallView;
            Require(V(observedPre.Position) == V(prePosition)
                && V(observedPre.Velocity) == V(preVelocity),
                "staged pre-kick state changed unexpectedly for " + kind);
            int target = kind == W8StageAKind.PassKick ? 1 : -1;
            kickTracker.Observe(new W8StageAEvent(kind, frame, 0, target, default,
                observedPre.Position, -1), kickEngine);
            Source kickSource = kickTracker.Latest;
            Require(!kickSource.HasPostMutationSample
                && V(kickSource.EventBallPosition) == V(prePosition)
                && V(kickSource.EventBallVelocity) == V(preVelocity),
                "pre-kick capture and unknown post sample for " + kind);
            var playerBefore = new Player(0, kickEngine.AgentTeamId(0), false, false,
                AgentMovementState.IDLE, new Vector2(prePosition.x, prePosition.y), Vector2.zero);
            kickCounter.Observe(frame, -1, observedPre.Position, observedPre.Velocity,
                new[] { playerBefore }, kickSource);
            string beforeNear = NearRow(kickRows.ToString(), frame);
            Require(beforeNear.Contains("sourcePostPhase=NA", StringComparison.Ordinal)
                && beforeNear.Contains("sourcePostBallPos=NA,NA,NA", StringComparison.Ordinal)
                && beforeNear.Contains("sourcePostBallVel=NA,NA,NA", StringComparison.Ordinal),
                "pre-pair near row must show unknown post state for " + kind);

            kickEngine.TestOnly_ForceBallLoose(postPosition, postVelocity);
            var observedPost = kickEngine.BallView;
            Require(V(observedPost.Position) == V(postPosition)
                && V(observedPost.Velocity) == V(postVelocity)
                && V(observedPost.Position) != V(observedPre.Position)
                && V(observedPost.Velocity) != V(observedPre.Velocity),
                "staged post-kick state not distinguishable for " + kind);
            kickTracker.Observe(new W8StageAEvent(W8StageAKind.KickRelease, frame,
                0, -1, default, observedPost.Position, -1), kickEngine);
            Require(kickSource.HasPostMutationSample
                && kickSource.PostSamplePhase == "postKickRelease"
                && V(kickSource.EventBallPosition) == V(prePosition)
                && V(kickSource.EventBallVelocity) == V(preVelocity)
                && V(kickSource.PostBallPosition) == V(postPosition)
                && V(kickSource.PostBallVelocity) == V(postVelocity),
                "post-kick capture exact values and preserved pre-values for " + kind);
            var playerAfter = new Player(0, kickEngine.AgentTeamId(0), false, false,
                AgentMovementState.IDLE, new Vector2(postPosition.x, postPosition.y), Vector2.zero);
            kickCounter.Observe(frame + 1, -1, observedPost.Position, observedPost.Velocity,
                new[] { playerAfter }, kickSource);
            kickCounter.Finish();
            string trace = kickRows.ToString();
            string afterNear = NearRow(trace, frame + 1);
            Require(NearRow(trace, frame) == beforeNear
                && afterNear.Contains("sourceEventBallPos=" + V(prePosition), StringComparison.Ordinal)
                && afterNear.Contains("sourceEventBallVel=" + V(preVelocity), StringComparison.Ordinal)
                && afterNear.Contains("sourcePostPhase=postKickRelease", StringComparison.Ordinal)
                && afterNear.Contains("sourcePostBallPos=" + V(postPosition), StringComparison.Ordinal)
                && afterNear.Contains("sourcePostBallVel=" + V(postVelocity), StringComparison.Ordinal)
                && kickCounter.NearPairs == 2 && kickCounter.PassageCount == 1,
                "near rows distinguish pre/post capture without retroactive fill for " + kind);
        }

        // W8 Release is observed before ReleaseControlledPossession mutates the ball.
        // Drive the actual event handler, not a parallel transition, so a
        // mistakenly named post-release sample fails this control.
        using var releaseRows = new StringWriter(CultureInfo.InvariantCulture);
        var releaseEngine = new MatchEngine(Seed);
        var releaseTracker = new SourceTracker(releaseRows);
        Vector3 releaseBall = releaseEngine.BallView.Position;
        releaseTracker.Observe(new W8StageAEvent(W8StageAKind.SixSecondDrop, 30,
            0, -1, default, releaseBall, -1), releaseEngine);
        Source dropSource = releaseTracker.Latest;
        releaseTracker.Observe(new W8StageAEvent(W8StageAKind.Release, 30,
            0, -1, default, releaseBall, 0), releaseEngine);
        Require(ReferenceEquals(releaseTracker.Latest, dropSource)
            && !dropSource.HasPostMutationSample
            && releaseRows.ToString().Contains("kind=Release,phase=preMutation",
                StringComparison.Ordinal)
            && !releaseRows.ToString().Contains("phase=postSixSecondRelease",
                StringComparison.Ordinal), "real W8 pre-release cannot claim post-release state");

        // Actual RunTick → Physics → pre-Heading callback, not a direct Counter call.
        var engine = new MatchEngine(Seed);
        engine.EnableGkHeading();
        Player[] world = CapturePlayers(engine);
        int chosen = -1;
        foreach (Player player in world)
            if (player.ActiveOutfielder && player.Team == 0) { chosen = player.Id; break; }
        Require(chosen >= 0, "real engine has home outfielder");
        AgentState staged = engine.AgentView(chosen);
        staged.CurrentState = AgentMovementState.IDLE;
        staged.Velocity = Vector2.zero;
        engine.TestOnly_SetAgent(chosen, in staged);
        engine.TestOnly_SetPhysicsFrame(24);
        engine.TestOnly_ForceBallLoose(new Vector3(staged.Position.x, staged.Position.y, 2f),
            Vector3.zero);
        var real = new Counter(null);
        engine.TestOnly_HeaderArrivalObserver = () =>
        {
            var ball = engine.BallView;
            real.Observe((int)engine.CurrentTick, engine.TestOnly_PossessingAgentId,
                ball.Position, ball.Velocity, CapturePlayers(engine), null);
        };
        engine.RunTick();
        engine.TestOnly_HeaderArrivalObserver = null;
        real.Finish();
        Require(real.CallbackFrames == 1 && real.NearPairs >= 1,
            "real pre-Heading staged positive callback");

        var cadenceEngine = new MatchEngine(Seed);
        cadenceEngine.EnableGkHeading();
        var callbacks = new List<int>();
        cadenceEngine.TestOnly_HeaderArrivalObserver = () => callbacks.Add((int)cadenceEngine.CurrentTick);
        cadenceEngine.RunTick();
        cadenceEngine.RunTick();
        cadenceEngine.TestOnly_HeaderArrivalObserver = null;
        Require(callbacks.Count == 2 && callbacks[0] == 1 && callbacks[1] == 2
            && cadenceEngine.CurrentTick == 2 && !cadenceEngine.MatchEnded,
            "first two real callbacks exactly once at same tick");
        Console.WriteLine("SELF_TEST,PASS,bounds=1,multiagent=1,mirror=1,tiedMin=1," +
            "sourceIdentity=1,sourceMutation=1,kickPairControls=5,kickValueControls=3," +
            "preRelease=1,realPositive=1,realCadence=2");
    }

    private static string NearRow(string trace, int frame)
    {
        string prefix = "near,frame=" + frame.ToString(CultureInfo.InvariantCulture) + ",";
        foreach (string row in trace.Split('\n'))
            if (row.StartsWith(prefix, StringComparison.Ordinal)) return row.TrimEnd('\r');
        throw new InvalidOperationException("PILOT_FAILURE,missing near row for frame " + frame);
    }

    private static bool SourceTrackerTestActor(Source source, int expectedActor, int expectedTeam) =>
        source.Actor == expectedActor && source.ActorTeam == expectedTeam;

    private static string HashFile(string path) => File.Exists(path)
        ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) : "NA";

    private static string FindWorkspaceFile(string relative)
    {
        DirectoryInfo directory = new(Environment.CurrentDirectory);
        while (directory != null)
        {
            string path = Path.Combine(directory.FullName, relative);
            if (File.Exists(path)) return path;
            directory = directory.Parent;
        }
        return "";
    }

    private static void Pilot(string path)
    {
        string output = Path.GetFullPath(path);
        Require(output.Contains(Path.DirectorySeparatorChar + "Temp" + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase), "pilot output must be an ignored Temp path");
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write);
        using var writer = new StreamWriter(stream);
        var engine = new MatchEngine(Seed);
        engine.EnableGkHeading();
        var sources = new SourceTracker(writer);
        var counter = new Counter(writer);
        Vector3 previousVelocity = default;
        bool hasPreviousVelocity = false;
        int callbackCount = 0;
        int attempted = 0;
        int completed = 0;
        string probeSource = FindWorkspaceFile(Path.Combine("docs", "tracking", "evidence",
            "header441-delivery-pilot-20261022", "Program.cs"));
        string engineSource = FindWorkspaceFile(Path.Combine("src", "match-engine", "MatchEngine.cs"));
        writer.WriteLine("# Issue 441, created 2026-10-22: bounded seed-1 observed delivery-provenance pilot");
        writer.WriteLine("# Broad pair: primary NO_POSSESSION; centre Z [1.5,2.6] inclusive; active non-GK/non-sent-off; XY^2<=1; all movement states");
        writer.WriteLine("# Every near row names the latest OBSERVED W8 source at that frame, not a complete flight or causal attribution. No jump, commit, ArrivalCensus or pass label filters.");
        writer.WriteLine("# Pass/Shot/GkHeadingKick and Restart W8 events precede mutation; FirstTouch and UnattributedDeflection follow mutation. KickRelease pairs only same frame+actor with unambiguous latest kick. Source post samples are observational, not modeled intent.");
        writer.WriteLine("# W8 observer installed after MatchEngine construction, at tick 0 before first RunTick; constructor/boot mutations are unobserved. Latest source begins Unknown; feedComplete=false.");
        writer.WriteLine("# W8 event seq counts all callbacks, including omitted tactical/keeper telemetry rows. W8 is incomplete: woodwork, physics bounces and other mutations may be unreported; velocity reversal flags are signals, not classified departures. Restart.Agent is awarded team, not actor. Unknown/NA is intentional.");
        writer.WriteLine($"identity,seed=0x{Seed:X16},runtime={Environment.Version}," +
            $"probeSourceSha256={HashFile(probeSource)},probeDllSha256={HashFile(typeof(Program).Assembly.Location)}," +
            $"engineSourceSha256={HashFile(engineSource)},engineDllSha256={HashFile(typeof(MatchEngine).Assembly.Location)}");
        writer.Flush();
        engine.TestOnly_W8StageAObserver = evt => sources.Observe(evt, engine);
        engine.TestOnly_HeaderArrivalObserver = () =>
        {
            callbackCount++;
            Require((int)engine.CurrentTick == attempted, "callback tick differs from attempted RunTick");
            ObserveWorld(engine, counter, sources, ref previousVelocity, ref hasPreviousVelocity);
        };
        try
        {
            for (int tick = 1; tick <= LastTick; tick++)
            {
                attempted++;
                int before = callbackCount;
                engine.RunTick();
                completed++;
                Require(callbackCount - before == 1 && (int)engine.CurrentTick == tick && !engine.MatchEnded,
                    $"cadence at tick={tick},delta={callbackCount - before},currentTick={engine.CurrentTick},ended={engine.MatchEnded}");
                if (tick % ProgressStride == 0)
                {
                    writer.Flush();
                    Console.WriteLine($"progress,tick={tick},callbacks={callbackCount},nearPairs={counter.NearPairs}");
                    Console.Out.Flush();
                }
            }
        }
        catch (Exception exception)
        {
            writer.WriteLine($"FAIL,attempted={attempted},completed={completed},callbacks={callbackCount}," +
                $"tick={engine.CurrentTick},ended={engine.MatchEnded},error={exception.GetType().Name}:{exception.Message}");
            writer.Flush();
            Console.Error.WriteLine(exception);
            Console.Error.Flush();
            throw;
        }
        finally
        {
            engine.TestOnly_W8StageAObserver = null;
            engine.TestOnly_HeaderArrivalObserver = null;
        }
        counter.Finish();
        string digest = Convert.ToHexString(engine.CurrentSnapshotDigest);
        writer.WriteLine($"end,seed=0x{Seed:X16},attempted={attempted},completed={completed}," +
            $"callbacks={callbackCount},firstCallback=1,lastCallback={counter.LastFrame}," +
            $"tick={engine.CurrentTick},ended={engine.MatchEnded},digest={digest}," +
            $"heightBandLooseFrames={counter.HeightBandLooseFrames},nearFrames={counter.NearFrames}," +
            $"nearPairs={counter.NearPairs},passages={counter.PassageCount}," +
            $"passageDurationSum={counter.PassageDurationSum},sourceChangesWithinPassages={counter.SourceChangesWithinPassages}," +
            $"observedW8Events={sources.EventCount},reversalSignals={sources.ReversalSignals}," +
            $"unpairedPostKickSignals={sources.UnpairedPostKickSignals}");
        writer.Flush();
        Require(attempted == LastTick && completed == LastTick && callbackCount == LastTick
            && counter.CallbackFrames == LastTick && counter.LastFrame == LastTick
            && digest == ExpectedDigest, "prefix endpoint or pinned digest mismatch");
        Console.WriteLine($"PILOT_PASS,seed=0x{Seed:X16},tick={LastTick},callbacks={callbackCount}," +
            $"nearPairs={counter.NearPairs},passages={counter.PassageCount},digest={digest}");
        Console.WriteLine("output=" + output);
    }

    private static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--self-test") SelfTest();
        else if (args.Length == 2 && args[0] == "--pilot") Pilot(args[1]);
        else
        {
            Console.Error.WriteLine("Usage: Probe --self-test | --pilot <new ignored Temp output path>");
            return 2;
        }
        return 0;
    }
}
