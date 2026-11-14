// Created 2026-11-13. Independent read-only reproduction of the section 10 preflight stop.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using TacticalDirector.MatchEngine;
using TacticalDirector.ProjectConstants;
using TacticalDirector.TacticalInstructions;
using TacticalDirector.DecisionTree;
using TacticalDirector.DefensiveAI;
using TacticalDirector.PositioningAI;

static class TracePossessionLag
{
    static object Field(object o, string name) => o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(o);
    static TeamTactic Tactic() => new TeamTactic(Mentality.Balanced,TacticFormation.F4231,Tempo.Standard,TacticWidth.Standard,
        TacticPassing.Mixed,TacticPressing.Medium,LineOfEngagement.Standard,0.5f,TacticDefWidth.Standard,
        TransitionPlan.HoldShape,TransitionPlan.Regroup,false,TacticTriggerMask.None,FocusPlay.Mixed,GkDistributionPolicy.SlowDown,0);
    static int Main(string[] args)
    {
        GameplayConfigHolder.Bind(new GameplayConfig(new Dictionary<string,string>{{"match-engine.MarkMovementEnabled","true"}}));
        return Run(args[0]);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Run(string output)
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture; Directory.CreateDirectory(output);
        using var log=new StreamWriter(Path.Combine(output,"engine.log"));var original=Console.Out;Console.SetOut(log);
        var e=new MatchEngine(1);e.EnableGkHeading();for(int team=0;team<2;team++)e.SetTeamTactic(team,Tactic());
        if(!MatchEngineConstants.MarkMovementEnabled)throw new Exception("config activation failed");
        var contexts=(TacticalContext[])Field(e,"_tacticalContexts");var snapshots=(DefensiveSnapshot[])Field(e,"_defSnapshots");
        var pos=(PositioningPerceptionSnapshot[])Field(e,"_posSnapshots");
        using var csv=new StreamWriter(Path.Combine(output,"trace.csv"));
        using var digests=new GZipStream(File.Create(Path.Combine(output,"digests.bin.gz")),CompressionLevel.Fastest);
        csv.WriteLine("frame,beforeOwner,afterOwner,beforeReceiver,afterReceiver,compositionOwner,compositionOwnPossession,beforeHasTarget,afterHasTarget,targetEntityId,assignmentTick,lastAction,lastActionHeartbeat,commandState,commandTargetX,commandTargetY,digest");
        for(int frame=1;frame<=420;frame++)
        {
            int before=e.PossessingAgentId,preReceiver=(int)Field(e,"_passInFlightReceiverId");bool preMark=contexts[16].HasMarkTarget;
            e.RunTick();byte[] digest=e.CurrentSnapshotDigest;digests.Write(digest,0,digest.Length);
            var assignment=e.TestOnly_MarkAssignment(16);var action=e.TestOnly_DtLastAction(16);var command=e.TestOnly_Command(16);
            bool own=pos[1].HasTeamPossession&&pos[1].TeamPossessionIsOwnTeam;
            csv.WriteLine($"{frame},{before},{e.PossessingAgentId},{preReceiver},{(int)Field(e,"_passInFlightReceiverId")},{snapshots[1].PossessionOwnerEntityId},{own},{preMark},{contexts[16].HasMarkTarget},{assignment.TargetEntityId},{assignment.ValidThroughTick},{action.Type},{action.HeartbeatTick},{command.DesiredState},{command.TargetPosition.x:R},{command.TargetPosition.y:R},{Convert.ToHexString(digest).ToLowerInvariant()}");
        }
        var result=new{seed="0000000000000001",@case=9,frames=420,armedThroughConfig=true,buildHash=MatchEngineBuildIdentity.BuildHash,
            description="No subscriptions, callbacks, overrides, simulation writes or assertions about post-frame target eligibility. Both teams F4231; other tactics default Balanced."};
        File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
        Console.SetOut(original);return 0;
    }
}
