// Created 2026-10-20. Purpose: classify every witness of #441's sole measured arrival.
// Local diagnostic only: observes fixed trajectories; never selects or commits an intent.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TacticalDirector.AgentMovement;
using TacticalDirector.HeadingMechanics;
using TacticalDirector.MatchEngine;
using UnityEngine;

internal static class Program
{
    private const int Agent = 6;
    private const int SearchFrom = 125430;
    private const int SearchUntil = 125500;
    private static string I(FormattableString value) => value.ToString(CultureInfo.InvariantCulture);

    private static bool Eligible(MatchEngine engine, AgentState state) =>
        !engine.AgentIsGoalkeeper(Agent) && !engine.TestOnly_IsSentOff(Agent)
        && state.CurrentState != AgentMovementState.GROUNDED
        && state.CurrentState != AgentMovementState.STUMBLING;

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static void Main(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("Supply the classification output path.");
        var engine = new MatchEngine(1UL);
        engine.EnableGkHeading();
        int stride = HeadingMechanicsConstants.FramesPerTacticalTick;
        int firstAge = Math.Max(0, HeadingJumpKinematics.ComputeApexFrame(0)
            - HeadingMechanicsConstants.FramesEarlyTolerance);
        int lastAge = HeadingJumpKinematics.ComputeApexFrame(0)
            + HeadingMechanicsConstants.FramesLateTolerance;
        int traceFrom = SearchFrom - lastAge - stride;
        float radius = HeadingMechanicsConstants.HeadContactVolumeRadiusM;
        var states = new Dictionary<int, bool>();
        var reaches = new Dictionary<int, float>();
        var starts = new SortedSet<int>();
        var report = new StringBuilder();
        report.AppendLine("#441 fixed-trajectory witness classification; seed=0x0000000000000001; agent=6; sampling=pre-Heading");
        report.AppendLine(I($"constants,stride={stride},firstAge={firstAge},lastAge={lastAge},radius={radius:R},heightBand={HeadingMechanicsConstants.HeadContactVolumeHeightM:R},earliestRetainedStart={traceFrom},searchFrom={SearchFrom},searchUntil={SearchUntil}"));
        report.AppendLine("engineAssemblySha256=" + Hash(typeof(MatchEngine).Assembly.Location));
        report.AppendLine("headingAssemblySha256=" + Hash(typeof(HeadingJumpKinematics).Assembly.Location));
        report.AppendLine("probeAssemblySha256=" + Hash(typeof(Program).Assembly.Location));
        report.AppendLine("runtime=" + Environment.Version);
        int pairCount = 0;
        int passageCount = 0;
        int firstContact = -1;
        int lastContact = -1;
        int closedAt = -1;
        bool open = false;
        engine.TestOnly_W8StageAObserver = sample =>
        {
            if (sample.Frame >= traceFrom && sample.Frame <= SearchUntil)
                report.AppendLine(I($"world,{sample.Frame},{sample.Kind},agent={sample.Agent}"));
        };
        engine.TestOnly_Heading.TestOnly_ReachabilityObserver = sample =>
        {
            if (sample.AgentId == Agent && sample.Frame >= traceFrom && sample.Frame <= SearchUntil)
                report.AppendLine(I($"heading,{sample.Frame},{sample.Kind},start={sample.JumpStartFrame},state={sample.Agent.CurrentState},headZ={sample.HeadZ:R},predicted={sample.Eligibility.PredictedContactFrame},cause={sample.Cause}"));
        };
        engine.TestOnly_HeaderArrivalObserver = () =>
        {
            int frame = (int)engine.CurrentTick;
            if (frame < traceFrom || frame > SearchUntil) return;
            AgentState player = engine.AgentView(Agent);
            bool eligible = Eligible(engine, player);
            states.Add(frame, eligible);
            if (eligible)
            {
                var canonical = engine.TestOnly_CanonicalAttributes(Agent);
                var attrs = PlayerAttributeProjection.ToHeading(in canonical, engine.AgentTeamId(Agent), fatigue: 0f);
                reaches.Add(frame, HeadingJumpKinematics.ComputeJumpReach(attrs));
            }
            var ball = engine.BallView;
            int possession = engine.TestOnly_PossessingAgentId;
            report.AppendLine(I($"frame,{frame},state={player.CurrentState},eligible={eligible},possession={possession},x={player.Position.x:R},y={player.Position.y:R},ball={ball.Position.x:R}/{ball.Position.y:R}/{ball.Position.z:R},velocity={ball.Velocity.x:R}/{ball.Velocity.y:R}/{ball.Velocity.z:R}"));
            if (frame < SearchFrom) return;
            bool hit = false;
            if (eligible && possession == MatchEngineConstants.NO_POSSESSION)
            {
                float dx = ball.Position.x - player.Position.x;
                float dy = ball.Position.y - player.Position.y;
                for (int age = firstAge; age <= lastAge; age++)
                {
                    int start = frame - age;
                    if (!reaches.TryGetValue(start, out float reach)) continue;
                    float headZ = HeadingJumpKinematics.ComputeHeadZ(start, reach, frame);
                    float dz = ball.Position.z - headZ;
                    float distanceSquared = dx * dx + dy * dy + dz * dz;
                    if (distanceSquared > radius * radius
                        || Mathf.Abs(dz) > HeadingMechanicsConstants.HeadContactVolumeHeightM) continue;
                    int apex = HeadingJumpKinematics.ComputeApexFrame(start);
                    report.AppendLine(I($"witness,contact={frame},start={start},distance={Mathf.Sqrt(distanceSquared):R},distanceSquared={distanceSquared:R},headZ={headZ:R},reach={reach:R},apex={apex},window={apex - HeadingMechanicsConstants.FramesEarlyTolerance}/{apex + HeadingMechanicsConstants.FramesLateTolerance},possession={possession},eligible={eligible}"));
                    starts.Add(start);
                    pairCount++;
                    hit = true;
                }
            }
            if (hit)
            {
                if (!open)
                {
                    passageCount++;
                    firstContact = frame;
                    open = true;
                }
                lastContact = frame;
            }
            else if (open)
            {
                open = false;
                closedAt = frame;
                report.AppendLine(I($"passageClosed,first={firstContact},last={lastContact},firstNoHitFrame={closedAt}"));
            }
        };
        while ((int)engine.CurrentTick < SearchUntil) engine.RunTick();
        engine.TestOnly_HeaderArrivalObserver = null;
        engine.TestOnly_Heading.TestOnly_ReachabilityObserver = null;
        engine.TestOnly_W8StageAObserver = null;
        if (open) throw new InvalidOperationException("Passage still open: extend the bounded replay before classifying.");
        bool allOutside = starts.Count > 0;
        foreach (int start in starts)
        {
            int before = (start / stride) * stride;
            int after = before + stride;
            bool beforeEligible = states[before];
            bool outside = before < start && start < after && beforeEligible;
            allOutside &= outside;
            report.AppendLine(I($"classification,start={start},previousTactical={before},previousPreHeadingEligible={beforeEligible},nextTactical={after},outsideTimingOnlyContract={outside}"));
        }
        report.AppendLine(I($"summary,pairs={pairCount},distinctStarts={starts.Count},passages={passageCount},firstContact={firstContact},lastContact={lastContact},closedAt={closedAt},allOutsideTimingOnlyContract={allOutside},endTick={engine.CurrentTick}"));
        report.AppendLine("endDigest=" + Convert.ToHexString(engine.CurrentSnapshotDigest));
        File.WriteAllText(args[0], report.ToString());
        Console.Write(report.ToString());
    }
}
