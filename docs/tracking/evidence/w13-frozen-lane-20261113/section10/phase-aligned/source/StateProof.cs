// Created 2026-11-13. Config-armed restore and repeatability proofs, using unmodified engine APIs.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TacticalDirector.MatchEngine;
using TacticalDirector.TacticalInstructions;

static partial class Program
{
    static MatchEngine StateEngine(ulong seed, int c)
    {
        var engine = new MatchEngine(seed); engine.EnableGkHeading();
        int[] families = { c == 3 || c == 9 ? 2 : 0, c == 4 || c == 9 ? 2 : 0 };
        for (int team = 0; team < 2; team++) engine.SetTeamTactic(team, Tactic((TacticFormation)families[team]));
        if (c == 5 || c == 7)
            for (int i = 1; i < 11; i++) engine.SetPlayerTactic(i, new PlayerTactic(PlayerRole.Default, c == 5 ? Duty.Defend : Duty.Attack, PlayerInstructions.Default));
        return engine;
    }
    static void ProveState(string output, bool armed)
    {
        Directory.CreateDirectory(output);
        using var log = new StreamWriter(Path.Combine(output, "engine.log"));
        TextWriter original = Console.Out; Console.SetOut(log);
        var records = new List<object>();
        const ulong seed = 1;
        foreach (int c in new[] { 0, 3, 4, 9, 5, 7 })
        {
            var engine = StateEngine(seed, c);
            for (int frame = 0; frame < 1333; frame++) engine.RunTick();
            var header = engine.CaptureDurableHeader(); var payload = engine.CaptureDurablePayload();
            var expected = new List<byte[]>();
            for (int frame = 0; frame < 600; frame++) { engine.RunTick(); expected.Add((byte[])engine.CurrentSnapshotDigest.Clone()); }
            var restored = MatchEngine.RestoreFromSnapshot(header, payload, seed);
            for (int frame = 0; frame < 600; frame++)
            {
                restored.RunTick(); Need(expected[frame].AsSpan().SequenceEqual(restored.CurrentSnapshotDigest), "restore continuation failed case C" + c + " offset " + frame);
            }
            records.Add(new { @case = c, captureTick = 1333, continuationTicks = 600, equal = true });
        }
        var first = StateEngine(seed, 9); var chain = new List<byte[]>();
        for (int frame = 0; frame < 6000; frame++) { first.RunTick(); chain.Add((byte[])first.CurrentSnapshotDigest.Clone()); }
        var second = StateEngine(seed, 9);
        for (int frame = 0; frame < chain.Count; frame++)
        {
            second.RunTick(); Need(chain[frame].AsSpan().SequenceEqual(second.CurrentSnapshotDigest), "two-run determinism failed at " + frame);
        }
        var result = new { armedThroughConfig = armed, restores = records, deterministicFrames = chain.Count, passed = true };
        File.WriteAllText(Path.Combine(output, "state-proof.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        Console.SetOut(original); Console.WriteLine(JsonSerializer.Serialize(result));
    }
}
