// Created 2026-11-20. Compare immediate and delayed take-off on the archived
// seed-1 pre-Heading trace. This probe stages inputs and leaves engine code untouched.
using System;
using System.Globalization;
using System.IO;

using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;
using TacticalDirector.HeadingMechanics;
using TacticalDirector.MatchEngine;

internal static class ArchivedArrivalReplay
{
    private const int Agent = 6;
    private const int CommitFrame = 125454;
    private const int LastFrame = 125482;

    private static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "current")
        {
            CurrentArrivalReplay.Run(args);
            return;
        }

        if (args.Length != 1)
            throw new ArgumentException("Supply the decompressed archived classification trace.");

        string[] trace = File.ReadAllLines(args[0]);
        Replay(trace, delay: 0, expectedContact: -1, expectedFailureFrame: 125475);
        Replay(trace, delay: 1, expectedContact: 125466, expectedFailureFrame: -1);
        Replay(trace, delay: 2, expectedContact: 125467, expectedFailureFrame: -1);
    }

    private static void Replay(string[] trace, int delay, int expectedContact, int expectedFailureFrame)
    {
        var engine = new MatchEngine(1);
        engine.EnableGkHeading();

        // Isolate the archived candidate from unrelated players and keeper claims.
        for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
        {
            var far = AgentState.CreateAtPosition(new Vector2(10, i + 5), Vector2.right);
            engine.TestOnly_SetAgent(i, in far);
        }

        int commits = 0;
        int startFrame = -1;
        int preparedFrame = -1;
        int executedFrame = -1;
        int failureFrame = -1;
        int headerEvents = 0;
        int eventFrame = -1;
        engine.TestOnly_Heading.TestOnly_ReachabilityObserver = sample =>
        {
            Require(sample.AgentId == Agent, "Unexpected header candidate.");
            switch (sample.Kind)
            {
                case HeadingReachabilityKind.Commit:
                    Require(sample.Frame == CommitFrame, "Unexpected commit frame.");
                    commits++;
                    break;
                case HeadingReachabilityKind.JumpStart:
                    Require(startFrame < 0, "Duplicate jump.");
                    startFrame = sample.Frame;
                    break;
                case HeadingReachabilityKind.Prepared:
                    Require(preparedFrame < 0, "Duplicate preparation.");
                    preparedFrame = sample.Frame;
                    break;
                case HeadingReachabilityKind.Executed:
                    Require(executedFrame < 0, "Duplicate execution.");
                    executedFrame = sample.Frame;
                    break;
                case HeadingReachabilityKind.Failed:
                    Require(failureFrame < 0 && sample.Cause == FailureCause.PositionedPoorly,
                        "Unexpected terminal failure.");
                    failureFrame = sample.Frame;
                    break;
            }
        };
        EventBus.Subscribe<HeaderExecutedEvent>((in HeaderExecutedEvent evt) =>
        {
            Require(evt.AgentId == Agent, "Unexpected header event agent.");
            headerEvents++;
            eventFrame = (int)engine.CurrentTick;
        });

        for (int frame = CommitFrame; frame <= LastFrame; frame++)
        {
            string row = Array.Find(trace, line => line.StartsWith($"frame,{frame},", StringComparison.Ordinal));
            Require(row != null, $"Missing archived frame {frame}.");
            string[] columns = row.Split(',');
            Require(columns[2] == "state=WALKING" && columns[4] == "possession=-1",
                $"Unexpected archived movement or possession at {frame}.");

            var position = new Vector2(Number(Value(columns[5])), Number(Value(columns[6])));
            var state = AgentState.CreateAtPosition(position, Vector2.left);
            state.CurrentState = AgentMovementState.WALKING;
            engine.TestOnly_SetAgent(Agent, in state);
            engine.TestOnly_SetPhysicsFrame(frame);
            engine.TestOnly_ForceBallLoose(Vector(Value(columns[7])), Vector(Value(columns[8])));

            EventBus.BeginTick((uint)frame);
            EventBus.BeginPhase(PhaseId.Physics);
            if (frame == CommitFrame)
                engine.TestOnly_DriveGkHeadingTactical();

            // Skip only the initial physics drive in the delayed arms. There is one intent
            // and no keeper participant. Subsequent frames run the normal Heading pipeline.
            if (frame >= CommitFrame + delay)
                engine.TestOnly_DriveGkHeadingPhysics();

            EventBus.DrainTick();
            EventBus.OnTickBoundary();
        }

        Require(commits == 1 && startFrame == CommitFrame + delay, "Unexpected commit or take-off.");
        Require(preparedFrame == expectedContact && executedFrame == expectedContact,
            "Unexpected prepared or executed contact.");
        Require(headerEvents == (expectedContact < 0 ? 0 : 1) && eventFrame == expectedContact,
            "Unexpected HeaderExecutedEvent.");
        Require(failureFrame == expectedFailureFrame, "Unexpected failure frame.");
        Console.WriteLine($"delay={delay},commit={CommitFrame},start={startFrame},prepared={preparedFrame}," +
            $"executed={executedFrame},headerEvents={headerEvents},eventFrame={eventFrame},failure={failureFrame}");
    }

    private static string Value(string column) => column.Substring(column.IndexOf('=') + 1);

    private static float Number(string value) => float.Parse(value, CultureInfo.InvariantCulture);

    private static Vector3 Vector(string value)
    {
        string[] parts = value.Split('/');
        return new Vector3(Number(parts[0]), Number(parts[1]), Number(parts[2]));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
