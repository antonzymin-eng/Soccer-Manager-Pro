using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using TacticalDirector.EventSystem;
using TacticalDirector.ShotMechanics;
using UnityEngine;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class W8CComparisonDiagnosticTests
    {
        private static readonly ulong[] Seeds = { 0x0F1E2D3C4B5A6978UL, 0xD1A6D05EUL, 1UL, 0xABCDEF12UL, 0x99887766UL, 0x5A5A5A5AUL };
        [Test]
        public void ProbeNeutrality600()
        {
            var engine = new MatchEngine(0xD1A6D05EUL);engine.EnableGkHeading();
            engine.W8ProbeObserver = e => { };engine.AttachW8BodyProbe();
            engine.W8ProbeLedgerObserver = (bytes, offset, count) => { };
            var digests=new byte[600][];
            for(int i=0;i<600;i++){engine.RunTick();digests[i]=engine.CurrentSnapshotDigest;}
            var plain=new MatchEngine(0xD1A6D05EUL);plain.EnableGkHeading();
            for(int i=0;i<600;i++){plain.RunTick();CollectionAssert.AreEqual(digests[i],plain.CurrentSnapshotDigest);}
        }
        [Test,Category("Calibration")]
        public void PairedSixSeedProbe()
        {
            string output=Environment.GetEnvironmentVariable("TD_W8_PROBE_OUT");
            if(string.IsNullOrEmpty(output)) Assert.Ignore("Set TD_W8_PROBE_OUT for paired six-seed evidence.");
            Directory.CreateDirectory(output);
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages=true;
            try
            {
                using var summary=new StreamWriter(Path.Combine(output,"summary.csv"));
                summary.WriteLine("seed,frames,homeGoals,awayGoals,snapshotDigest");
                foreach(ulong seed in Seeds)
                {
                    string id=seed.ToString("X16");
                    var engine=new MatchEngine(seed);engine.EnableGkHeading();
                    using var trace=new BinaryWriter(File.Create(Path.Combine(output,id+".trace")));
                    using var raw=new StreamWriter(Path.Combine(output,id+"-raw.csv"));
                    raw.WriteLine("frame,order,kind,agent,team,x,y,z,scoringTeam,restrictionKeeper");
                    using var events=new StreamWriter(Path.Combine(output,id+"-events.csv"));
                    events.WriteLine("frame,ledgerIndex,kind,agent,creditedTeam,scoringTeam,x,y,z");
                    int lastFrame=-1,order=0;
                    engine.W8ProbeObserver=e=>
                    {
                        if(e.Frame!=lastFrame){lastFrame=e.Frame;order=0;}
                        raw.WriteLine(FormattableString.Invariant($"{e.Frame},{order++},{e.Kind},{e.Agent},{e.Team},{e.Position.x:R},{e.Position.y:R},{e.Position.z:R},{e.ScoringTeam},{e.RestrictionKeeper}"));
                    };
                    engine.AttachW8BodyProbe();
                    using var sha=SHA256.Create();byte[] ledgerDigest=null;
                    engine.W8ProbeLedgerObserver=(bytes,offset,count)=>ledgerDigest=sha.ComputeHash(bytes,offset,count);
                    // Bind the unchanged frozen observer once, then invoke typed delegates each frame.
                    var report=new StringBuilder();
                    Type censusType=typeof(W8StageADiagnosticTests).GetNestedType("Census",BindingFlags.NonPublic);
                    object census=Activator.CreateInstance(censusType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new object[]{engine,seed,report},null);
                    const BindingFlags methods=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
                    engine.TestOnly_W8StageAObserver=(Action<W8StageAEvent>)Delegate.CreateDelegate(typeof(Action<W8StageAEvent>),census,censusType.GetMethod("Observe",methods));
                    var after=(Action)Delegate.CreateDelegate(typeof(Action),census,censusType.GetMethod("AfterTick",methods));
                    var finish=(Action)Delegate.CreateDelegate(typeof(Action),census,censusType.GetMethod("Finish",methods));
                    for(int frame=1;frame<=324000;frame++)
                    {
                        engine.RunTick();after();
                        var ball=engine.TestOnly_BallSnapshot;
                        trace.Write(frame);trace.Write(engine.PossessingAgentId);trace.Write((byte)ball.State);
                        trace.Write(ball.Position.x);trace.Write(ball.Position.y);trace.Write(ball.Position.z);
                        trace.Write(ball.Velocity.x);trace.Write(ball.Velocity.y);trace.Write(ball.Velocity.z);
                        trace.Write(ball.AngularVelocity.x);trace.Write(ball.AngularVelocity.y);trace.Write(ball.AngularVelocity.z);
                        trace.Write(ledgerDigest);
                        for(int i=0;i<engine.TickLedgerCount;i++)
                        {
                            byte ordinal=engine.TickLedgerOrdinal(i);
                            if(ordinal==EventRegistry.GetOrdinal<ShotExecutedEvent>())
                            {
                                var shot=engine.TickLedgerRecord<ShotExecutedEvent>(i);int agent=shot.ShootingAgentId;
                                events.WriteLine($"{frame},{i},shot,{agent},{engine.AgentTeamId(agent)},-1,0,0,0");
                            }
                            if(ordinal==EventRegistry.GetOrdinal<GoalAwardedEvent>())
                            {
                                var goal=engine.TickLedgerRecord<GoalAwardedEvent>(i);int agent=goal.Scorer;
                                events.WriteLine(FormattableString.Invariant($"{frame},{i},goal,{agent},{(agent<0?-1:engine.AgentTeamId(agent))},{goal.ScoringTeam},{goal.BallPosition.x:R},{goal.BallPosition.y:R},{goal.BallPosition.z:R}"));
                            }
                        }
                    }
                    finish();File.WriteAllText(Path.Combine(output,id+"-frozen-w8.txt"),report.ToString());
                    string digest=BitConverter.ToString(engine.CurrentSnapshotDigest).Replace("-","").ToLowerInvariant();
                    summary.WriteLine($"{id},324000,{engine.HomeScore},{engine.AwayScore},{digest}");summary.Flush();
                    TestContext.WriteLine($"seedEnd,{id},digest={digest}");
                }
            }
            finally{UnityEngine.TestTools.LogAssert.ignoreFailingMessages=false;}
        }
    }
}
