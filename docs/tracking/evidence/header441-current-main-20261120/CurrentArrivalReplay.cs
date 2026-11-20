// Created 2026-11-20. Observe the current-main arrival in seed 1 and compare
// experimental start frames through the live MatchEngine tick pipeline.
using System;
using System.Globalization;
using System.Reflection;

using UnityEngine;

using TacticalDirector.EventSystem;
using TacticalDirector.HeadingMechanics;
using TacticalDirector.MatchEngine;

internal static class CurrentArrivalReplay
{
    private const int Receiver = 15;
    private const int TacticalFrame = 113400;
    private const int ShippedCommitFrame = 113406;
    private const int LastFrame = 113428;

    internal static void Run(string[] args)
    {
        int forcedStart = args.Length == 1 ? -1 : int.Parse(args[1], CultureInfo.InvariantCulture);
        if (args.Length > 2 || (forcedStart != -1 && (forcedStart < 113398 || forcedStart > TacticalFrame)))
            throw new ArgumentException("Use current with optional start 113398, 113399 or 113400.");

        var engine = new MatchEngine(1);
        engine.EnableGkHeading();
        var latchField = typeof(MatchEngine).GetField(
            "_headerCommittedThisEpisode", BindingFlags.NonPublic | BindingFlags.Instance);
        bool[] latches = latchField?.GetValue(engine) as bool[]
            ?? throw new InvalidOperationException("Missing production header episode latches.");
        float tacticalDistance = float.NaN;
        float physicsDistance = float.NaN;
        int start = -1;
        int prepared = -1;
        int failed = -1;
        int predictionAtFailure = -1;
        FailureCause? failure = null;
        int headers = 0;
        int headerFrame = -1;

        engine.TestOnly_W8StageAObserver = evt =>
        {
            if (evt.Kind != W8StageAKind.TacticalAfter)
                return;

            if (evt.Frame == TacticalFrame)
                tacticalDistance = Distance(engine);

            // Keep the injected attempt alive against the shipped stride's duplicate commit.
            // This mirrors the latch the production trigger sets when it commits an intent.
            if (forcedStart >= 0 && evt.Frame == ShippedCommitFrame)
                latches[Receiver] = true;
        };
        engine.TestOnly_HeaderArrivalObserver = () =>
        {
            int frame = (int)engine.CurrentTick;
            if (frame == TacticalFrame)
                physicsDistance = Distance(engine);
            if (frame != forcedStart)
                return;

            var player = engine.AgentView(Receiver);
            int team = engine.AgentTeamId(Receiver);
            var canonical = engine.TestOnly_CanonicalAttributes(Receiver);
            var attrs = PlayerAttributeProjection.ToHeading(in canonical, team, fatigue: 0f);
            var intent = new HeaderIntent
            {
                PowerIntent = MatchEngineConstants.HeaderTriggerPowerIntent,
                TargetIntent = GkHeadingIntentSource.HeaderAimTarget(in player.Position, team),
                AttemptCommittedTick = frame / HeadingMechanicsConstants.FramesPerTacticalTick,
                SetPieceContext = SetPieceContext.OpenPlay,
            };
            engine.TestOnly_Heading.CommitIntent(Receiver, intent, attrs, engine.BallView, frame);
        };
        engine.TestOnly_Heading.TestOnly_ReachabilityObserver = sample =>
        {
            if (sample.AgentId != Receiver || sample.Frame < 113398)
                return;

            if (sample.Kind == HeadingReachabilityKind.JumpStart)
                start = sample.Frame;
            if (sample.Kind == HeadingReachabilityKind.Prepared)
                prepared = sample.Frame;
            if (sample.Kind == HeadingReachabilityKind.Failed)
            {
                failed = sample.Frame;
                failure = sample.Cause;
                predictionAtFailure = sample.Eligibility.PredictedContactFrame;
            }
        };
        EventBus.Subscribe<HeaderExecutedEvent>((in HeaderExecutedEvent evt) =>
        {
            if (evt.AgentId == Receiver && engine.CurrentTick >= 113398)
            {
                headers++;
                headerFrame = (int)engine.CurrentTick;
            }
        });

        while (engine.CurrentTick < LastFrame)
            engine.RunTick();

        if (forcedStart == -1 && !(tacticalDistance > MatchEngineConstants.HeaderTriggerRangeM
                                  && physicsDistance <= MatchEngineConstants.HeaderTriggerRangeM))
            throw new InvalidOperationException("Receiver did not cross trigger range at the measured stride.");

        Console.WriteLine(FormattableString.Invariant(
            $"receiver={Receiver},forcedStart={forcedStart},tacticalDistance={tacticalDistance:R},physicsDistance={physicsDistance:R},start={start},prepared={prepared},headerEvents={headers},headerFrame={headerFrame},failed={failed},failure={failure},predictionAtFailure={predictionAtFailure}"));

        bool executed = forcedStart == 113398;
        int expectedStart = forcedStart < 0 ? ShippedCommitFrame : forcedStart;
        int expectedFailure = forcedStart < 0 ? 113427 : executed ? -1 : 113409;
        FailureCause? expectedCause = forcedStart < 0
            ? FailureCause.PositionedPoorly
            : executed ? (FailureCause?)null : FailureCause.MistimedEarly;
        int expectedPrediction = forcedStart == TacticalFrame ? 113410 : forcedStart == 113399 ? 113409 : -1;
        int expectedContact = executed ? 113410 : -1;
        if (start != expectedStart || prepared != expectedContact || headerFrame != expectedContact
            || headers != (executed ? 1 : 0) || failed != expectedFailure || failure != expectedCause
            || predictionAtFailure != expectedPrediction)
            throw new InvalidOperationException("Current arrival replay did not reproduce its recorded outcome.");
    }

    private static float Distance(MatchEngine engine)
    {
        Vector3 ball = engine.BallView.Position;
        Vector2 agent = engine.AgentView(Receiver).Position;
        return (new Vector2(ball.x, ball.y) - agent).magnitude;
    }
}
