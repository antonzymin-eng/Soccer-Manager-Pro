// Created: 2026-09-30. Purpose: capture existing reference client output; changes no production code.
// Non-certifying Linux evidence only. Run from capture-reference.sh.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchClientWeb;
using TacticalDirector.MatchAnalytics;
using TacticalDirector.TacticalInstructions;
using TacticalDirector.MatchEngine;

static object Statline(MatchStatline line, AdvancedStatline advanced) => new {
    goals = line.Goals, possession = line.PossessionSharePercent, fouls = line.Fouls,
    yellow = line.YellowCards, red = line.RedCards, offsides = line.Offsides,
    corners = line.Corners, throwIns = line.ThrowIns, goalKicks = line.GoalKicks,
    substitutions = line.Substitutions, territory = advanced.TerritorialPercent,
    xgAvailable = advanced.LiveXgAvailable, xg = advanced.LiveXgAvailable ? (float?)advanced.XgSum : null
};
var host = new MatchClientHost(new MatchSetup(0x00C11E7B6D0CUL, awayManagerMode: ManagerMode.AI));
var snapshots = new List<object>();
for (int index = 0; index < 324000; index++) {
    host.Session.TickOnce();
    var frame = host.Project();
    if (index != 0 && (index + 1) % 3600 != 0 && !frame.MatchEnded) continue;
    var report = host.BuildReport();
    var agents = new List<object>();
    for (int id = 0; id < frame.AgentPositions.Count; id++) {
        var p = frame.AgentPositions[id];
        agents.Add(new { id, x = p.x, y = p.y, sentOff = frame.AgentCues[id].IsSentOff,
            benchSlot = frame.AgentCues[id].BenchSlot });
    }
    snapshots.Add(new { tick = frame.Tick, observedTicks = report.ObservedTicks,
        minute = frame.Tick / 3600.0, score = new[] { frame.Score.Home, frame.Score.Away },
        ended = frame.MatchEnded, period = frame.Period.ToString(), restart = frame.Restart.Cue.ToString(),
        ball = new[] { frame.BallPosition.x, frame.BallPosition.y, frame.BallPosition.z },
        possessing = frame.PossessingAgentId, agents,
        home = Statline(report.Result.Home, report.Result.HomeAdvanced),
        away = Statline(report.Result.Away, report.Result.AwayAdvanced), healthy = host.AnalyticsFault == null });
    if (frame.MatchEnded) break;
}
var data = new { sourceCommit = args[1], seed = "0x00C11E7B6D0C", homeManager = "Human", awayManager = "AI",
    snapshots, healthy = host.AnalyticsFault == null };
File.WriteAllText(args[0], "// Created: 2026-09-30. Purpose: captured, unmodified reference-client frame/statistics evidence.\nwindow.S0Reference = " + JsonSerializer.Serialize(data) + ";\n");
Console.WriteLine($"Captured {snapshots.Count} snapshots, healthy={host.AnalyticsFault == null}.");
