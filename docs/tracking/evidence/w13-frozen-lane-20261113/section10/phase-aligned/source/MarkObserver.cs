// Created 2026-11-13. Read-only W13 section 10 capture additions; no callbacks or game writes.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using TacticalDirector.DecisionTree;
using TacticalDirector.DefensiveAI;
using TacticalDirector.EventSystem;
using TacticalDirector.MatchEngine;
using TacticalDirector.PositioningAI;
using TacticalDirector.PressingAI;
using TacticalDirector.ShotMechanics;
using UnityEngine;

internal sealed class MarkObserver : IDisposable
{
    private readonly MatchEngine _engine;
    private readonly DefensiveAITick[] _defensive;
    private readonly DefensiveSnapshot[] _snapshots;
    private readonly PositioningPerceptionSnapshot[] _positioningSnapshots;
    private readonly TacticalContext[] _contexts;
    private readonly TacticalDirector.DecisionTree.DecisionTree[] _trees;
    private readonly FieldInfo _hasMark = typeof(TacticalContext).GetField("HasMarkTarget");
    private readonly FieldInfo _target = typeof(TacticalContext).GetField("MarkTarget");
    private readonly bool _armed;
    private readonly BinaryWriter _marks, _witness;
    private int _decisionCarrier;
    private readonly long[] _modes = new long[8], _intents = new long[6], _offside = new long[2];
    private readonly long[] _markSamples = new long[2], _markDecisions = new long[2], _markPress = new long[2], _nonCarrierPress = new long[2];
    private long _samples;
    private readonly long[] _retainedPossessionLagSamples = new long[2], _retainedInactiveLagSamples = new long[2];
    private readonly BinaryWriter _retainedLag;
    private static object Field(object value, string name) => value.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
    private static void Need(bool value, string why) { if (!value) throw new Exception(why); }
    private static bool Finite(Vector2 v) => float.IsFinite(v.x) && float.IsFinite(v.y);

    public MarkObserver(MatchEngine engine, string output, bool armed)
    {
        _engine = engine; _armed = armed;
        _defensive = (DefensiveAITick[])Field(engine, "_defensive");
        _snapshots = (DefensiveSnapshot[])Field(engine, "_defSnapshots");
        _positioningSnapshots = (PositioningPerceptionSnapshot[])Field(engine, "_posSnapshots");
        _contexts = (TacticalContext[])Field(engine, "_tacticalContexts");
        _trees = (TacticalDirector.DecisionTree.DecisionTree[])Field(engine, "_decisionTrees");
        _marks = Writer(Path.Combine(output, "marking.bin.gz"));
        _witness = Writer(Path.Combine(output, "mark-witness.bin.gz"));
        _retainedLag = Writer(Path.Combine(output, "retained-lag.bin.gz"));
        WriteLayouts(output);
        File.WriteAllText(Path.Combine(output, "stream-layouts.json"), JsonSerializer.Serialize(new {
            marking = new { recordBytes = 246, description = "frame i32, then 22*(mode u8,target i8,hasTarget u8,targetX f32,targetY f32)" },
            witness = new { recordBytes = 312, description = "frame i32, then 22*14 byte source-phase witness, as listed in MarkObserver.cs" },
            retainedLag = new { recordBytes = 16, description = "every frame: frame i32, hasTargetMask u32, possessionLagMask u32, inactiveLagMask u32; bit index = entity id" }
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
    private static BinaryWriter Writer(string path) => new BinaryWriter(new System.IO.Compression.GZipStream(File.Create(path), System.IO.Compression.CompressionLevel.Fastest));
    public void BeforeTick() { _decisionCarrier = ((MatchContext)Field(_engine, "_matchContext")).PossessingAgentId; }
    public void Event(byte ordinal, ReadOnlySpan<byte> payload)
    {
        if (ordinal == EventRegistry.GetOrdinal<OffsideCalledEvent>())
        {
            int team = MemoryMarshal.Read<OffsideCalledEvent>(payload).Team;
            Need(team >= 0 && team < 2, "offside attribution"); _offside[team]++;
        }
    }
    private static DefensiveAgentSnapshot Agent(DefensiveSnapshot snapshot, int id)
    {
        for (int i = 0; i < snapshot.AgentCount; i++) if (snapshot.Agents[i].EntityId == id) return snapshot.Agents[i];
        throw new Exception("agent missing from defensive snapshot: " + id);
    }
    public void Sample(int frame)
    {
        _samples++; _marks.Write(frame); _witness.Write(frame);
        for (int team = 0; team < 2; team++)
        {
            DefensiveSnapshot snap = _snapshots[team];
            bool ownPossession = _positioningSnapshots[team].HasTeamPossession && _positioningSnapshots[team].TeamPossessionIsOwnTeam;
            int backline = 0, backlineZonal = 0, manMarks = 0;
            var targetIds = new HashSet<int>();
            for (int local = 0; local < 11; local++)
            {
                int id = team * 11 + local;
                var agent = Agent(snap, id);
                MarkAssignment assignment = _defensive[team].GetAssignment(id);
                object context = _contexts[id];
                bool has = _hasMark != null && (bool)_hasMark.GetValue(context);
                Vector2 target = has ? (Vector2)_target.GetValue(context) : Vector2.zero;
                bool pool = agent.IsActive && !agent.IsGoalkeeper && agent.PressRole == PressRole.HoldShape;
                bool fresh = assignment.ValidThroughTick == snap.TickIndex;
                Need((byte)assignment.Mode <= 3, "unknown mark mode");
                Need(assignment.TargetEntityId >= -1 && assignment.TargetEntityId < 22, "mark target id outside roster");
                Need(Finite(target), "nonfinite mark target");
                bool targetActive = assignment.TargetEntityId < 0 || Agent(snap, assignment.TargetEntityId).IsActive;
                if (has)
                {
                    Need(_armed, "target while switch off");
                    Need(pool && fresh && !ownPossession, "target on GK/presser/inactive/possessing team, or stale assignment at frame " + frame + " agent " + id);
                    Need(assignment.Mode != MarkMode.Zonal && targetActive, "invalid composed target at frame " + frame + " agent " + id);
                    if (assignment.TargetEntityId >= 0) Need(Agent(snap, assignment.TargetEntityId).TeamId != team, "marking own team");
                    // Eligibility is checked against the same AI-composition snapshot that produced this input.
                    // Post-Resolve changes are recorded separately by RetainedTick, not joined to an earlier input.
                    _markSamples[team]++;
                }
                if (pool)
                {
                    if (agent.Line == LineId.Defense) { backline++; if (assignment.Mode == MarkMode.Zonal) backlineZonal++; }
                    if (assignment.Mode == MarkMode.ManMark) manMarks++;
                    if (_armed && assignment.Mode != MarkMode.Zonal && assignment.TargetEntityId >= 0)
                        Need(targetIds.Add(assignment.TargetEntityId), "Invariant 4 duplicate opponent at frame " + frame);
                    if (assignment.Mode != MarkMode.Zonal && !assignment.OverriddenThisTick)
                        Need(Vector2.Distance(agent.BaselineSlot, assignment.TargetPosition) <= DefensiveAIConstants.MaxMarkDisplacementM, "Invariant 3 displacement at frame " + frame);
                }
                _modes[team * 4 + (int)assignment.Mode]++;
                _marks.Write((byte)assignment.Mode); _marks.Write((sbyte)assignment.TargetEntityId); _marks.Write((byte)(has ? 1 : 0));
                _marks.Write(target.x); _marks.Write(target.y);
                AgentAction action = _trees[id].LastAction;
                bool freshDecision = action.HeartbeatTick == frame / 6;
                if (has && freshDecision)
                {
                    _markDecisions[team]++;
                    if (action.Type == ActionType.PRESS)
                    {
                        _markPress[team]++;
                        if (action.TargetAgentId != _decisionCarrier) _nonCarrierPress[team]++;
                    }
                }
                // 14 bytes/agent, in roster order. Geometry samples and assignment-input witnesses share a frame key.
                _witness.Write((byte)(agent.IsActive ? 1 : 0)); _witness.Write((byte)(agent.IsGoalkeeper ? 1 : 0));
                _witness.Write((byte)agent.PressRole); _witness.Write((byte)(ownPossession ? 1 : 0));
                _witness.Write((byte)action.Type); _witness.Write((sbyte)action.TargetAgentId);
                _witness.Write((byte)(freshDecision ? 1 : 0)); _witness.Write((sbyte)_decisionCarrier);
                _witness.Write((byte)(targetActive ? 1 : 0)); _witness.Write(assignment.ValidThroughTick);
                _witness.Write((byte)(assignment.OverriddenThisTick ? 1 : 0));
            }
            Need(backline < DefensiveAIConstants.MinBacklineAgents || backlineZonal >= DefensiveAIConstants.MinBacklineAgents, "Invariant 1 backline at frame " + frame);
            Need(manMarks <= DefensiveAIConstants.MaxManMarkAssignments, "Invariant 2 cap at frame " + frame);
            foreach (var request in _defensive[team].GetTackleIntentRequests())
            {
                int mode = (int)request.Mode; Need(mode >= 0 && mode < 3, "tackle mode"); _intents[team * 3 + mode]++;
            }
        }
        Need(_nonCarrierPress[0] == 0 && _nonCarrierPress[1] == 0, "marker selected PRESS on a non-carrier");
    }
    public object Summary() => new {
        samples = _samples, assignmentModesByTeam = _modes, targetSamplesByTeam = _markSamples,
        retainedPossessionLagMarkerFramesByTeam = _retainedPossessionLagSamples,
        retainedInactiveLagMarkerFramesByTeam = _retainedInactiveLagSamples,
        freshMarkerDecisionsByTeam = _markDecisions, markerPressByTeam = _markPress,
        nonCarrierMarkerPressByTeam = _nonCarrierPress, tackleIntentModesByTeam = _intents,
        tackleResolvedByTeam = new[] { _engine.TestOnly_TackleResolvedByTeam(0), _engine.TestOnly_TackleResolvedByTeam(1) },
        offsidesByTeam = _offside, armedThroughConfig = _armed
    };
    private static object Layout<T>(byte ordinal) where T : struct
    {
        var fields = new Dictionary<string, int>();
        foreach (var f in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)) fields[f.Name] = Marshal.OffsetOf<T>(f.Name).ToInt32();
        return new { name = typeof(T).FullName, ordinal, size = Marshal.SizeOf<T>(), fields };
    }
    private static void WriteLayouts(string output)
    {
        object[] events = {
            Layout<ShotExecutedEvent>(EventRegistry.GetOrdinal<ShotExecutedEvent>()),
            Layout<GoalAwardedEvent>(EventRegistry.GetOrdinal<GoalAwardedEvent>()),
            Layout<RestartAwardedEvent>(EventRegistry.GetOrdinal<RestartAwardedEvent>()),
            Layout<CardIssuedEvent>(EventRegistry.GetOrdinal<CardIssuedEvent>()),
            Layout<SubstitutionEvent>(EventRegistry.GetOrdinal<SubstitutionEvent>()),
            Layout<FoulCommittedEvent>(EventRegistry.GetOrdinal<FoulCommittedEvent>()),
            Layout<OffsideCalledEvent>(EventRegistry.GetOrdinal<OffsideCalledEvent>())
        };
        File.WriteAllText(Path.Combine(output, "event-layouts.json"), JsonSerializer.Serialize(events, new JsonSerializerOptions { WriteIndented = true }));
    }
    public void RetainedTick(int frame)
    {
        int holder = _engine.PossessingAgentId;
        int receiver = (int)Field(_engine, "_passInFlightReceiverId");
        int possessor = holder >= 0 ? holder : receiver;
        int team = possessor >= 0 ? _engine.AgentTeamId(possessor) : -1;
        uint targets = 0, possessionLag = 0, inactiveLag = 0;
        if (_hasMark != null)
            for (int id = 0; id < 22; id++)
            {
                if (!(bool)_hasMark.GetValue(_contexts[id])) continue;
                targets |= 1u << id;
                int own = id / 11;
                if (own == team)
                {
                    possessionLag |= 1u << id;
                    if (!_engine.MatchEnded) _retainedPossessionLagSamples[own]++;
                }
                int target = _defensive[own].GetAssignment(id).TargetEntityId;
                if (_engine.AgentIsSentOff(id) || (target >= 0 && _engine.AgentIsSentOff(target)))
                {
                    inactiveLag |= 1u << id;
                    if (!_engine.MatchEnded) _retainedInactiveLagSamples[own]++;
                }
            }
        _retainedLag.Write(frame); _retainedLag.Write(targets); _retainedLag.Write(possessionLag); _retainedLag.Write(inactiveLag);
    }
    public void Dispose() { _marks.Dispose(); _witness.Dispose(); _retainedLag.Dispose(); }
}
