// File:     src/match-engine/tests/W8BDistributionDiagnosticTests.cs
// Created:  2026-10-15
// Modified: 2026-10-15
// Author:   —
// Spec:     W8 preregistration, #21 §3.4.1, #11 §3.8, #5 §3.8.13
// Purpose:  Observe frozen B corpus and composed LongKick landing trajectories without tuning gameplay.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.BallPhysics;
using TacticalDirector.EventSystem;
using TacticalDirector.GoalkeeperMechanics;
using TacticalDirector.PassMechanics;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>Opt-in frozen-corpus and composed LongKick landing census.</summary>
    [TestFixture]
    public sealed class W8BDistributionDiagnosticTests
    {
        private static readonly ulong[] Seeds = { 0x0F1E2D3C4B5A6978UL, 0xD1A6D05EUL, 1UL, 0xABCDEF12UL, 0x99887766UL, 0x5A5A5A5AUL };

        [Test, Category("Calibration")]
        public void W8BDistributionDiagnostic_ReportsFrozenCorpusAndLongKickLandings()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TD_W8_B")))
                Assert.Ignore("Set TD_W8_B=1 for frozen B distribution and landing evidence.");
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            try
            {
                var report = new StringBuilder("=== W8 B frozen corpus and landing evidence ===\n");
                foreach (ulong seed in Seeds)
                {
                    var engine = new MatchEngine(seed); engine.EnableGkHeading();
                    var census = new Census(engine, seed.ToString("X16"), report);
                    census.Attach();
                    for (int frame = 0; frame < 324000; frame++) { engine.RunTick(); census.AfterTick(); }
                    census.Finish();
                }
                // The frozen corpus uses SlowDown only. These named composed fixtures supply
                // LongKick trajectory evidence and are excluded from all frozen-corpus totals.
                for (int team = 0; team < 2; team++)
                {
                    MatchEngine engine = W8BDistributionTests.Fixture(GkDistributionPolicy.LongKick, team, false);
                    var census = new Census(engine, "composed-LongKick-team-" + team, report);
                    census.Attach();
                    for (int frame = 0; frame < 600; frame++) { engine.RunTick(); census.AfterTick(); }
                    census.Finish();
                }
                TestContext.WriteLine(report.ToString());
            }
            finally { UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false; }
        }

        private sealed class Flight
        {
            internal int Keeper, Frame;
            internal Vector3 Source, Target;
            internal GkDistributionPolicy Policy;
        }

        private sealed class Census
        {
            private readonly MatchEngine _engine;
            private readonly string _seed;
            private readonly StringBuilder _report;
            private readonly Dictionary<int, GoalkeeperDistributionRequest> _requests = new Dictionary<int, GoalkeeperDistributionRequest>();
            private readonly Dictionary<int, GkDistributionPolicy> _policies = new Dictionary<int, GkDistributionPolicy>();
            private readonly Dictionary<int, int> _claims = new Dictionary<int, int>();
            private readonly Dictionary<int, int> _claimContacts = new Dictionary<int, int>();
            private int _lastContactKeeper = -1, _lastContactFrame = -1;
            private Flight _flight;
            private int _commits, _completed, _cancelled, _rejected, _events, _releases, _w5Receiver, _zones, _missing;
            private int _drops, _dtRaces, _outOfPlayRun, _maxOutOfPlayRun, _stationaryRun, _maxStationaryRun;
            private Vector3 _previousPosition;
            private int _fouls, _yellows, _reds, _secondYellows;
            private static readonly byte DistributionOrdinal = EventRegistry.GetOrdinal<DistributionExecutedEvent>();
            private static readonly byte FoulOrdinal = EventRegistry.GetOrdinal<FoulCommittedEvent>();
            private static readonly byte CardOrdinal = EventRegistry.GetOrdinal<CardIssuedEvent>();

            internal Census(MatchEngine engine, string seed, StringBuilder report) { _engine = engine; _seed = seed; _report = report; }
            private void Row(FormattableString row) => _report.AppendLine(FormattableString.Invariant(row));
            internal void Attach()
            {
                _engine.TestOnly_W8StageAObserver = e =>
                {
                    if (e.Kind == W8StageAKind.HandClaim)
                    {
                        EndFlight("interrupted-by-hand-claim");
                        _claims[e.Agent] = e.Frame; _claimContacts[e.Agent] = 0;
                    }
                    if (e.Kind == W8StageAKind.SixSecondDrop) _drops++;
                    if (e.Kind == W8StageAKind.KeeperDecision && e.Agent == _engine.PossessingAgentId
                        && _engine.TestOnly_GoalkeeperState.States[_engine.TestOnly_W8TeamId(e.Agent)] == GoalkeeperState.HandsOnBall) _dtRaces++;
                    if (e.Kind == W8StageAKind.KickRelease && _requests.ContainsKey(e.Agent)) _releases++;
                    if (e.Kind == W8StageAKind.Restart) EndFlight("restart");
                    if (e.Kind == W8StageAKind.PassKick || e.Kind == W8StageAKind.ShotKick || e.Kind == W8StageAKind.GkHeadingKick)
                        EndFlight("interrupted-by-kick");
                };
                _engine.TestOnly_W8BCommitObserver = (keeper, policy, request) =>
                {
                    _commits++; _requests[keeper] = request; _policies[keeper] = policy;

                    _claims.TryGetValue(keeper, out int claimFrame);
                    Row($"commit,{_seed},keeper={keeper},policy={policy},frame={request.FrameNumber},claimFrame={claimFrame},receiver={request.TargetAgentId},targetX={request.TargetPosition.x:R},targetY={request.TargetPosition.y:R},windup={request.WindupFrames},power={request.EmittedPower01:R}");
                };
                _engine.TestOnly_W8BTerminalFeedbackObserver = (keeper, feedback, missing) =>
                {
                    if (feedback.Kind == GoalkeeperDistributionFeedbackKind.Rejected) { _rejected++; return; }
                    if (feedback.Kind == GoalkeeperDistributionFeedbackKind.Cancelled) { _cancelled++; _requests.Remove(keeper); return; }
                    _completed++; if (missing) _missing++;
                    _lastContactKeeper = keeper; _lastContactFrame = feedback.ContactFrame;
                    GoalkeeperDistributionRequest request = _requests[keeper];
                    GkDistributionPolicy policy = _policies[keeper];
                    _claims.TryGetValue(keeper, out int claimFrame);
                    if (claimFrame > 0)
                    {
                        Assert.Less(feedback.ContactFrame - claimFrame, 360, "ordinary voluntary CONTACT before inherited drop");
                        Assert.Greater(feedback.ContactFrame - claimFrame, 0);
                        Assert.Less(feedback.ContactFrame, (claimFrame / 6 + GoalkeeperConstants.GK_HOLD_MAX_TICKS) * 6, "CONTACT before earlier #11 guard");
                        Assert.AreEqual(0, _claimContacts[keeper], "one launch per hand claim");
                        _claimContacts[keeper]++;
                    }
                    if (feedback.EffectiveTargetAgentId < 0) { _zones++; Assert.AreEqual(-1, _engine.TestOnly_PassInFlightReceiverId); }
                    else { _w5Receiver++; Assert.AreEqual(feedback.EffectiveTargetAgentId, _engine.TestOnly_PassInFlightReceiverId); }
                    Row($"contact,{_seed},keeper={keeper},policy={policy},frame={feedback.ContactFrame},claimFrame={claimFrame},hold={feedback.ContactFrame - claimFrame},commitToContact={feedback.ContactFrame - request.FrameNumber},guardMargin={360 - (feedback.ContactFrame - claimFrame)},receiver={feedback.EffectiveTargetAgentId},sourceX={feedback.ReleasePoint.x:R},sourceY={feedback.ReleasePoint.y:R},sourceZ={feedback.ReleasePoint.z:R},targetX={feedback.EffectiveTargetPosition.x:R},targetY={feedback.EffectiveTargetPosition.y:R},speed={feedback.FinalVelocity.magnitude:R},missing={missing}");
                    EndFlight("replaced-by-distribution");
                    _flight = new Flight
                    {
                        Keeper = keeper,
                        Frame = feedback.ContactFrame,
                        Source = feedback.ReleasePoint,
                        Target = feedback.EffectiveTargetPosition,
                        Policy = policy
                    };
                    _requests.Remove(keeper);
                };
            }
            internal void AfterTick()
            {
                if (_lastContactFrame == (int)_engine.CurrentTick)
                {
                    int team = _engine.TestOnly_W8TeamId(_lastContactKeeper);
                    Assert.IsTrue(_engine.TestOnly_TryGetPressPassEvent(1 - team, out PassAttemptEvent pass), "W5 must observe CONTACT");
                    Assert.AreEqual(_lastContactKeeper, pass.AgentId);
                    Assert.AreEqual(_lastContactFrame, pass.Frame);
                }
                BallState ball = _engine.BallView;
                _outOfPlayRun = ball.State == BallStateType.OutOfPlay ? _outOfPlayRun + 1 : 0;
                _maxOutOfPlayRun = Math.Max(_maxOutOfPlayRun, _outOfPlayRun);
                _stationaryRun = ball.Position.Equals(_previousPosition) ? _stationaryRun + 1 : 1;
                _maxStationaryRun = Math.Max(_maxStationaryRun, _stationaryRun); _previousPosition = ball.Position;
                if (_flight != null && (int)_engine.CurrentTick > _flight.Frame)
                {
                    // Control can attach an intercepted ball to ground height in Resolve. Never
                    // report that attachment as a physical landing; simultaneous contact is censored.
                    if (_engine.PossessingAgentId >= 0) EndFlight("controlled-before-observed-ground");
                    else if (ball.State == BallStateType.Bouncing || ball.Position.z <= BallPhysicsConstants.Ball.RADIUS)
                        EndFlight("first-ground-contact", ball.Position);
                }
                for (int i = 0; i < _engine.TickLedgerCount; i++)
                {
                    byte ordinal = _engine.TickLedgerOrdinal(i);
                    if (ordinal == DistributionOrdinal) _events++;
                    if (ordinal == FoulOrdinal) _fouls++;
                    if (ordinal == CardOrdinal)
                    {
                        var card = _engine.TickLedgerRecord<CardIssuedEvent>(i);
                        // Same source-complete card convention as the frozen W8 instrument.
                        if (card.CardKind == MatchEngineConstants.CardKindSecondYellow) { _secondYellows++; _yellows++; }
                        else if (card.CardKind == MatchEngineConstants.CardKindRed) _reds++;
                        else if (card.CardKind == MatchEngineConstants.CardKindYellow) _yellows++;
                    }
                }
            }
            private void EndFlight(string reason, Vector3? ground = null)
            {
                if (_flight == null) return;
                Flight f = _flight; _flight = null;
                float intended = new Vector2(f.Target.x - f.Source.x, f.Target.y - f.Source.y).magnitude;
                if (ground.HasValue)
                {
                    Vector3 p = ground.Value;
                    float actual = new Vector2(p.x - f.Source.x, p.y - f.Source.y).magnitude;
                    Row($"landing,{_seed},keeper={f.Keeper},policy={f.Policy},contactFrame={f.Frame},endFrame={_engine.CurrentTick},reason={reason},requestedDistance={intended:R},actualDistance={actual:R},radialShortfall={intended - actual:R},groundX={p.x:R},groundY={p.y:R}");
                }
                else Row($"landing,{_seed},keeper={f.Keeper},policy={f.Policy},contactFrame={f.Frame},endFrame={_engine.CurrentTick},reason={reason},requestedDistance={intended:R}");
            }
            internal void Finish()
            {
                EndFlight("censored-at-end");
                Row($"bSummary,{_seed},commits={_commits},completed={_completed},cancelled={_cancelled},rejected={_rejected},events={_events},releases={_releases},w5Receiver={_w5Receiver},zones={_zones},missing={_missing},drops={_drops},dtRaces={_dtRaces},maxOutOfPlayRun={_maxOutOfPlayRun},maxStationaryRun={_maxStationaryRun},fouls={_fouls},yellows={_yellows},reds={_reds},secondYellows={_secondYellows},digest={BitConverter.ToString(_engine.CurrentSnapshotDigest).Replace("-", "").ToLowerInvariant()}");
                Assert.AreEqual(_completed, _events, "executed event requires one canonical CONTACT");
                Assert.AreEqual(_completed, _releases, "one canonical release per CONTACT");
                Assert.AreEqual(0, _drops); Assert.AreEqual(0, _dtRaces);
                Assert.Less(_maxOutOfPlayRun, 600, "no long OutOfPlay stall");
                _engine.TestOnly_W8StageAObserver = null; _engine.TestOnly_W8BCommitObserver = null;
                _engine.TestOnly_W8BTerminalFeedbackObserver = null;
            }
        }
    }
}

#region VersionHistory
// | Version | Date | Author | Notes |
// | 1.0 | 2026-10-15 | — | Frozen six-seed distribution census plus separately labelled LongKick landing fixtures. |
// | 1.1 | 2026-10-15 | — | Conservative landing observation censors control attachment before testing height; assert both inherited boundaries. |
#endregion
