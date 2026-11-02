// File:     src/match-engine/tests/HeaderReachabilityDiagnosticTests.cs
// Created:  2026-10-10
// Modified: 2026-10-20 (#441 arrival census: include deferred jump starts during possession)
// Modified: 2026-10-18 (#441 arrival census: trigger-independent feasible-head passages, launch attribution,
//           and correlation with the shipped trigger's actual commits; measurement only)
// Modified: 2026-10-11 (PR #497 review: timing-valid counterfactuals; exact-frame replica check)
// Author:   —
// Spec:     Heading Mechanics #10 §3.2/§3.3/§4.6; issue #441; docs/tracking/header-reachability-441-counters.md
// Purpose:  Env-gated, observation-only #441 instrument. Decomposes why committed headers never reach a
//           prepared Head contact on the frozen six seeds, plus a structural gate proving that attaching
//           the observer leaves every snapshot digest unchanged.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.HeadingMechanics;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    internal sealed class HeaderReachabilityDiagnosticTests
    {
        private const int TicksPerSeed = 324000;
        private const int NeutralityFrames = 18000;
        private const int ForecastExtraFrames = 60;

        private static readonly ulong[] Seeds =
        {
            0x0F1E2D3C4B5A6978UL, 0x00000000D1A6D05EUL,
            0x0000000000000001UL, 0x00000000ABCDEF12UL,
            0x0000000099887766UL, 0x000000005A5A5A5AUL
        };

        // Distance bucket upper edges in metres. The first edge is replaced by the head-volume radius.
        private static readonly float[] DistanceEdgesM = { 0.0f, 0.5f, 1.0f, 2.0f, 5.0f };
        private static readonly float[] CommitBallZEdgesM = { 1.0f, 1.6f, 2.0f, 2.6f };
        private static readonly int[] JumpLagEdgesFrames = { 0, 5, 11 };

        [Test, Category("Calibration")]
        public void HeaderReachabilityDiagnostic_ReportsFrozenSixSeedCounters()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TD_HEADER441_DIAGNOSTIC")))
            {
                Assert.Ignore("Set TD_HEADER441_DIAGNOSTIC=1 to run the #441 header-reachability counters.");
            }

            bool priorIgnore = UnityEngine.TestTools.LogAssert.ignoreFailingMessages;
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            try
            {
                var report = new StringBuilder();
                report.AppendLine("=== #441 header reachability counters (frozen six seeds) ===");
                report.AppendLine(Inv($"constants,radiusM={HeadingMechanicsConstants.HeadContactVolumeRadiusM:R},heightM={HeadingMechanicsConstants.HeadContactVolumeHeightM:R},framesEarly={HeadingMechanicsConstants.FramesEarlyTolerance},framesLate={HeadingMechanicsConstants.FramesLateTolerance},jumpMs={HeadingMechanicsConstants.JumpPhaseDurationMs:R},apexFraction={HeadingMechanicsConstants.JumpApexFraction:R}"));
                report.AppendLine(Episode.CsvHeader);
                report.AppendLine(Arrival.CsvHeader);

                var total = new Counters();
                var arrivalTotal = new ArrivalCounters();
                foreach (ulong seed in Seeds)
                {
                    var engine = new MatchEngine(seed);
                    engine.EnableGkHeading();
                    var census = new Census(seed, report);
                    var arrivals = new ArrivalCensus(seed, engine, report);
                    engine.TestOnly_Heading.TestOnly_ReachabilityObserver = sample =>
                    {
                        census.Observe(sample);
                        arrivals.ObserveHeading(sample);
                    };
                    engine.TestOnly_W8StageAObserver = arrivals.ObserveWorldEvent;
                    engine.TestOnly_HeaderArrivalObserver = arrivals.ObserveFrame;
                    for (int frame = 0; frame < TicksPerSeed; frame++)
                    {
                        engine.RunTick();
                    }
                    engine.TestOnly_Heading.TestOnly_ReachabilityObserver = null;
                    engine.TestOnly_W8StageAObserver = null;
                    engine.TestOnly_HeaderArrivalObserver = null;
                    census.Finish();
                    arrivals.Finish();

                    census.Counters.Write(report, Inv($"seed,0x{seed:X16}"));
                    arrivals.Counters.Write(report, Inv($"seedArrival,0x{seed:X16}"));
                    report.AppendLine(Inv($"seedEnd,0x{seed:X16},tick={engine.CurrentTick},digest={Digest(engine.CurrentSnapshotDigest)}"));
                    total.Add(census.Counters);
                    arrivalTotal.Add(arrivals.Counters);
                }

                total.Write(report, "AGGREGATE");
                arrivalTotal.Write(report, "AGGREGATE_ARRIVAL");
                TestContext.WriteLine(report.ToString());

                // Internal consistency only. No figure is compared with an expected football value.
                Assert.That(total.Commits, Is.GreaterThan(0), "frozen corpus produced no header commits");
                Assert.That(total.JumpStarts, Is.LessThanOrEqualTo(total.Commits), "jump starts exceed commits");
                Assert.That(total.EverPredicted, Is.LessThanOrEqualTo(total.JumpStarts), "predicted episodes exceed jump starts");
                Assert.That(total.Prepared, Is.LessThanOrEqualTo(total.EverPredicted), "prepared contacts exceed predicted episodes");
                Assert.That(total.Executed, Is.LessThanOrEqualTo(total.Prepared), "executed headers exceed prepared contacts");
                Assert.That(total.FailedPoorly, Is.EqualTo(total.PoorlyAerialCheck + total.PoorlyNoContactFrame),
                    "PositionedPoorly decomposition does not sum");
                // The counterfactuals are comparable only if the restated predictor reproduces #10 exactly
                // (header-reachability-441-counters.md §3.3). Report first, then fail the run as invalid.
                Assert.That(total.ReplicaMismatchFrames, Is.EqualTo(0),
                    "restated §3.2 predictor disagrees with HeadingEligibility; counterfactual counters are invalid");
                Assert.That(arrivalTotal.PotentialWindows, Is.GreaterThan(0),
                    "trigger-independent census produced no feasible jump windows");
                Assert.That(arrivalTotal.ActualCommits, Is.EqualTo(total.Commits),
                    "arrival census did not observe the same #10 commits as the lifecycle census");
                Assert.That(arrivalTotal.Arrivals,
                    Is.EqualTo(arrivalTotal.CoveredByCurrentCommit + arrivalTotal.MissedByCurrentTrigger),
                    "arrival coverage decomposition does not sum");
            }
            finally
            {
                UnityEngine.TestTools.LogAssert.ignoreFailingMessages = priorIgnore;
            }
        }

        [Test]
        public void HeaderReachabilityObserver_DoesNotChangeSnapshotDigests()
        {
            const ulong seed = 0x0F1E2D3C4B5A6978UL;
            int samples = 0;
            int evaluations = 0;

            var observed = new MatchEngine(seed);
            observed.EnableGkHeading();
            var arrivalReport = new StringBuilder();
            var arrivalCensus = new ArrivalCensus(seed, observed, arrivalReport);
            observed.TestOnly_Heading.TestOnly_ReachabilityObserver = sample =>
            {
                samples++;
                arrivalCensus.ObserveHeading(sample);
                if (sample.Kind == HeadingReachabilityKind.Pending
                    || sample.Kind == HeadingReachabilityKind.Prepared
                    || sample.Kind == HeadingReachabilityKind.Failed)
                {
                    evaluations++;
                }
            };
            observed.TestOnly_W8StageAObserver = arrivalCensus.ObserveWorldEvent;
            observed.TestOnly_HeaderArrivalObserver = arrivalCensus.ObserveFrame;
            var digests = new byte[NeutralityFrames][];
            for (int frame = 0; frame < NeutralityFrames; frame++)
            {
                observed.RunTick();
                digests[frame] = observed.CurrentSnapshotDigest;
            }
            observed.TestOnly_Heading.TestOnly_ReachabilityObserver = null;
            observed.TestOnly_W8StageAObserver = null;
            observed.TestOnly_HeaderArrivalObserver = null;
            arrivalCensus.Finish();

            // Non-vacuity: the window must contain real header evaluations, or identity proves nothing.
            Assert.That(evaluations, Is.GreaterThan(0),
                "no #10 evaluation was observed in the neutrality window; lengthen it");
            Assert.That(arrivalCensus.Counters.PotentialWindows, Is.GreaterThan(0),
                "no trigger-independent jump windows were observed in the neutrality window; lengthen it");

            // EventBus is process-global: run the second engine only after the first has finished.
            var plain = new MatchEngine(seed);
            plain.EnableGkHeading();
            for (int frame = 0; frame < NeutralityFrames; frame++)
            {
                plain.RunTick();
                CollectionAssert.AreEqual(digests[frame], plain.CurrentSnapshotDigest,
                    "#441 observer altered the snapshot at frame " + (frame + 1).ToString(CultureInfo.InvariantCulture)
                    + " (samples observed: " + samples.ToString(CultureInfo.InvariantCulture) + ")");
            }
        }

        [TestCase(0, false, false)]
        [TestCase(1, false, false)]
        [TestCase(0, true, false)]
        [TestCase(1, true, false)]
        [TestCase(0, true, true)]
        [TestCase(1, true, true)]
        public void ArrivalCensus_StagedApexContact_IsCounted(int teamId, bool deflected, bool outside)
        {
            const ulong seed = 0x441UL;
            var engine = new MatchEngine(seed);
            engine.EnableGkHeading();

            int agent = -1;
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (!engine.AgentIsGoalkeeper(i) && engine.AgentTeamId(i) == teamId)
                {
                    agent = i;
                    break;
                }
            }
            Assert.That(agent, Is.GreaterThanOrEqualTo(0), "fixture requires an outfield player");

            AgentState state = engine.AgentView(agent);
            state.Velocity = Vector2.zero;
            state.CurrentState = AgentMovementState.IDLE;
            engine.TestOnly_SetAgent(agent, in state);

            int startFrame = HeadingMechanicsConstants.FramesPerTacticalTick;
            engine.TestOnly_SetPhysicsFrame(startFrame);
            engine.TestOnly_ForceBallLoose(
                new Vector3(state.Position.x, state.Position.y, 0.0f), Vector3.zero);

            var report = new StringBuilder();
            var census = new ArrivalCensus(seed, engine, report);
            census.ObserveFrame();

            if (deflected)
            {
                census.ObserveWorldEvent(new W8StageAEvent(
                    W8StageAKind.UnattributedDeflection, startFrame + 1, -1, -1,
                    RestartCue.None, engine.BallView.Position, MatchEngineConstants.NO_POSSESSION));
            }

            int team = engine.AgentTeamId(agent);
            TacticalDirector.PlayerDatabase.PlayerAttributes canonical =
                engine.TestOnly_CanonicalAttributes(agent);
            HeadingAgentAttributes attrs = PlayerAttributeProjection.ToHeading(
                in canonical, team, fatigue: 0.0f);
            float headZ = HeadingJumpKinematics.ComputeJumpReach(attrs);
            int apexFrame = HeadingJumpKinematics.ComputeApexFrame(startFrame);

            engine.TestOnly_SetPhysicsFrame(apexFrame);
            float ballX = state.Position.x + (outside ? 2.0f * HeadingMechanicsConstants.HeadContactVolumeRadiusM : 0.0f);
            engine.TestOnly_ForceBallLoose(
                new Vector3(ballX, state.Position.y, headZ), Vector3.zero);
            census.ObserveFrame();

            // A continuous passage remains one arrival even when several windows/frames intersect.
            engine.TestOnly_SetPhysicsFrame(apexFrame + 1);
            float nextHeadZ = HeadingJumpKinematics.ComputeHeadZ(startFrame, headZ, apexFrame + 1);
            engine.TestOnly_ForceBallLoose(new Vector3(ballX, state.Position.y, nextHeadZ), Vector3.zero);
            census.ObserveFrame();

            census.Finish();
            int expected = outside ? 0 : 1;
            Assert.That(census.Counters.PotentialWindows, Is.GreaterThan(0));
            Assert.That(census.Counters.Arrivals, Is.EqualTo(expected),
                "detect the in-volume passage, retain pre-deflection starts, and reject the horizontal miss");
            Assert.That(census.Counters.MissedByCurrentTrigger, Is.EqualTo(expected),
                "the staged arrival has no production commit and must be classified as missed");
            Assert.That(census.Counters.TacticalStartArrivals, Is.EqualTo(expected));
            Assert.That(census.Counters.BySource[(int)(deflected
                ? ArrivalLaunchKind.Deflection : ArrivalLaunchKind.Unknown)], Is.EqualTo(expected));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void ArrivalCensus_DeferredStartDuringPossession_CoversLooseContact(int teamId)
        {
            const ulong seed = 0x441UL;
            var engine = new MatchEngine(seed);
            engine.EnableGkHeading();
            int agent = -1;
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (!engine.AgentIsGoalkeeper(i) && engine.AgentTeamId(i) == teamId)
                {
                    agent = i;
                    break;
                }
            }
            Assert.That(agent, Is.GreaterThanOrEqualTo(0), "fixture requires an outfield player");

            AgentState state = engine.AgentView(agent);
            state.Velocity = Vector2.zero;
            state.CurrentState = AgentMovementState.GROUNDED;
            engine.TestOnly_SetAgent(agent, in state);
            var report = new StringBuilder();
            var census = new ArrivalCensus(seed, engine, report);
            int stride = HeadingMechanicsConstants.FramesPerTacticalTick;
            int commitFrame = stride;
            int startFrame = 2 * stride;
            var states = new AgentState[MatchEngineConstants.SQUAD_SIZE];
            for (int i = 0; i < states.Length; i++) states[i] = engine.AgentView(i);
            TacticalDirector.PlayerDatabase.PlayerAttributes canonical =
                engine.TestOnly_CanonicalAttributes(agent);
            HeadingAgentAttributes attrs = PlayerAttributeProjection.ToHeading(
                in canonical, teamId, fatigue: 0.0f);
            var intent = new HeaderIntent
            {
                PowerIntent = 0.5f,
                ContactPointIntent = Vector2.zero,
                TargetIntent = new Vector3(state.Position.x, state.Position.y, 0.0f),
                AttemptCommittedTick = commitFrame / stride,
                SetPieceContext = SetPieceContext.OpenPlay,
            };
            engine.TestOnly_Heading.TestOnly_ReachabilityObserver = census.ObserveHeading;
            engine.TestOnly_SetPhysicsFrame(commitFrame);
            engine.TestOnly_Heading.CommitIntent(agent, intent, attrs, engine.BallView, commitFrame);
            engine.TestOnly_Heading.Update(states, engine.BallView, commitFrame,
                commitFrame * HeadingMechanicsConstants.FrameS);
            Assert.That(engine.TestOnly_HeadingState.ContactStates[agent].JumpStartFrame, Is.EqualTo(-1),
                "the grounded commit must remain deferred");

            state.CurrentState = AgentMovementState.IDLE;
            engine.TestOnly_SetAgent(agent, in state);
            states[agent] = state;
            engine.TestOnly_SetPhysicsFrame(startFrame);
            engine.TestOnly_SetPossession(agent);
            Assert.That(engine.TestOnly_PossessingAgentId, Is.EqualTo(agent));
            census.ObserveFrame();
            engine.TestOnly_Heading.Update(states, engine.BallView, startFrame,
                startFrame * HeadingMechanicsConstants.FrameS);
            Assert.That(engine.TestOnly_HeadingState.ContactStates[agent].JumpStartFrame, Is.EqualTo(startFrame),
                "#10 must start the deferred intent despite current possession");

            int contactFrame = HeadingJumpKinematics.ComputeApexFrame(startFrame);
            engine.TestOnly_SetPhysicsFrame(contactFrame);
            engine.TestOnly_ForceBallLoose(new Vector3(state.Position.x, state.Position.y,
                HeadingJumpKinematics.ComputeHeadZ(startFrame, JumpReach(engine, agent), contactFrame)),
                Vector3.zero);
            Assert.That(engine.TestOnly_PossessingAgentId, Is.EqualTo(MatchEngineConstants.NO_POSSESSION));
            census.ObserveFrame();
            census.Finish();
            engine.TestOnly_Heading.TestOnly_ReachabilityObserver = null;

            Assert.That(census.Counters.ActualCommits, Is.EqualTo(1));
            Assert.That(census.Counters.Arrivals, Is.EqualTo(1),
                "a deferred jump that starts during possession can meet a later loose ball");
            Assert.That(census.Counters.CoveredByCurrentCommit, Is.EqualTo(1),
                "the actual start must match the hypothetical start at contact");
            Assert.That(census.Counters.MissedByCurrentTrigger, Is.EqualTo(0));
            StringAssert.Contains($",True,True,True,False,{contactFrame},{startFrame},", report.ToString());
        }

        [Test]
        public void ArrivalCensus_TwoActualStarts_CoversContactFromOlderStart()
        {
            const ulong seed = 0x441UL;
            var engine = new MatchEngine(seed);
            engine.EnableGkHeading();
            int agent = FirstOutfielder(engine);
            AgentState state = engine.AgentView(agent);
            state.CurrentState = AgentMovementState.IDLE;
            engine.TestOnly_SetAgent(agent, in state);
            var report = new StringBuilder();
            var census = new ArrivalCensus(seed, engine, report);
            int first = HeadingMechanicsConstants.FramesPerTacticalTick;
            int second = first + 2 * HeadingMechanicsConstants.FramesPerTacticalTick;
            engine.TestOnly_SetPhysicsFrame(first);
            engine.TestOnly_ForceBallLoose(new Vector3(state.Position.x, state.Position.y, 0), Vector3.zero);
            census.ObserveFrame();
            census.ObserveHeading(JumpStartSample(engine, agent, first, first));
            engine.TestOnly_SetPhysicsFrame(second);
            census.ObserveFrame();
            census.ObserveHeading(JumpStartSample(engine, agent, second, second));

            int contact = HeadingJumpKinematics.ComputeApexFrame(first);
            float reach = JumpReach(engine, agent);
            engine.TestOnly_SetPhysicsFrame(contact);
            engine.TestOnly_ForceBallLoose(new Vector3(state.Position.x, state.Position.y,
                HeadingJumpKinematics.ComputeHeadZ(first, reach, contact)), Vector3.zero);
            census.ObserveFrame();
            census.Finish();

            Assert.That(census.Counters.Arrivals, Is.EqualTo(1));
            Assert.That(census.Counters.CoveredByCurrentCommit, Is.EqualTo(1));
            StringAssert.Contains($",True,True,False,False,{contact},{first},", report.ToString());
        }

        [Test]
        public void ArrivalCensus_PostSamplePassKick_RetainsPassThroughNextLowRise()
        {
            const ulong seed = 0x441UL;
            var engine = new MatchEngine(seed);
            engine.EnableGkHeading();
            int agent = FirstOutfielder(engine);
            var report = new StringBuilder();
            var census = new ArrivalCensus(seed, engine, report);
            int frame = HeadingMechanicsConstants.FramesPerTacticalTick;
            engine.TestOnly_SetPhysicsFrame(frame);
            engine.TestOnly_ForceBallLoose(new Vector3(50, 34, 0.1f), new Vector3(0, 0, -1));
            census.ObserveFrame();
            census.ObserveWorldEvent(new W8StageAEvent(W8StageAKind.PassKick, frame, agent, -1,
                RestartCue.None, engine.BallView.Position, MatchEngineConstants.NO_POSSESSION));
            engine.TestOnly_SetPhysicsFrame(frame + 1);
            engine.TestOnly_ForceBallLoose(new Vector3(50, 34, 0.1f), new Vector3(0, 0, 1));
            census.ObserveFrame();
            census.ObserveHeading(CommitSample(engine, agent, frame + 1));

            Assert.That(census.Counters.CommitsBySource[(int)ArrivalLaunchKind.Pass], Is.EqualTo(1));
            StringAssert.Contains($",{frame + 1},{agent},{engine.AgentTeamId(agent)},1,Pass,{frame},{agent},",
                report.ToString());
        }

        [Test]
        public void ArrivalCensus_LaterCoverageAndTacticalStart_KeepFirstContactRowWithWitness()
        {
            const ulong seed = 0x441UL;
            var engine = new MatchEngine(seed);
            engine.EnableGkHeading();
            int agent = FirstOutfielder(engine);
            AgentState state = engine.AgentView(agent);
            state.CurrentState = AgentMovementState.IDLE;
            engine.TestOnly_SetAgent(agent, in state);
            var report = new StringBuilder();
            var census = new ArrivalCensus(seed, engine, report);
            int tactical = 2 * HeadingMechanicsConstants.FramesPerTacticalTick;
            int offStride = tactical + 3;
            engine.TestOnly_SetPhysicsFrame(tactical);
            engine.TestOnly_ForceBallLoose(new Vector3(state.Position.x, state.Position.y, 0), Vector3.zero);
            census.ObserveFrame();
            engine.TestOnly_SetPhysicsFrame(offStride);
            census.ObserveFrame();
            int firstContact = offStride + HeadingJumpKinematics.ComputeApexFrame(0)
                - HeadingMechanicsConstants.FramesEarlyTolerance;
            float reach = JumpReach(engine, agent);
            engine.TestOnly_SetPhysicsFrame(firstContact);
            engine.TestOnly_ForceBallLoose(new Vector3(state.Position.x, state.Position.y,
                HeadingJumpKinematics.ComputeHeadZ(offStride, reach, firstContact)), Vector3.zero);
            census.ObserveFrame();
            int coveredContact = firstContact + 1;
            engine.TestOnly_SetPhysicsFrame(coveredContact);
            engine.TestOnly_ForceBallLoose(new Vector3(state.Position.x, state.Position.y,
                HeadingJumpKinematics.ComputeHeadZ(tactical, reach, coveredContact)), Vector3.zero);
            census.ObserveFrame();
            // #10 reports after the pre-Heading sample on this same frame.
            census.ObserveHeading(JumpStartSample(engine, agent, coveredContact, tactical));
            census.Finish();

            Assert.That(census.Counters.Arrivals, Is.EqualTo(1));
            Assert.That(census.Counters.CoveredByCurrentCommit, Is.EqualTo(1));
            Assert.That(census.Counters.TacticalStartArrivals, Is.EqualTo(1));
            string row = report.ToString();
            StringAssert.Contains($"arrival,0x{seed:X16},0,{firstContact},{agent},", row);
            StringAssert.Contains($",True,True,False,False,{coveredContact},{tactical},{coveredContact},{tactical}", row);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ArrivalCensus_EarlierSamePlayerCommit_UsesFirstCommitOnCurrentFlight(bool resetFlight)
        {
            const ulong seed = 0x441UL;
            var engine = new MatchEngine(seed);
            engine.EnableGkHeading();
            int agent = FirstOutfielder(engine);
            AgentState state = engine.AgentView(agent);
            state.CurrentState = AgentMovementState.IDLE;
            engine.TestOnly_SetAgent(agent, in state);
            var report = new StringBuilder();
            var census = new ArrivalCensus(seed, engine, report);
            int firstCommit = HeadingMechanicsConstants.FramesPerTacticalTick;
            int start = 2 * firstCommit;
            int secondCommit = 4 * firstCommit;
            int contact = HeadingJumpKinematics.ComputeApexFrame(start);

            engine.TestOnly_SetPhysicsFrame(firstCommit);
            engine.TestOnly_ForceBallLoose(new Vector3(state.Position.x, state.Position.y, 0), Vector3.zero);
            census.ObserveHeading(CommitSample(engine, agent, firstCommit));
            engine.TestOnly_SetPhysicsFrame(start);
            census.ObserveFrame();
            if (resetFlight)
            {
                census.ObserveWorldEvent(new W8StageAEvent(W8StageAKind.PassKick, secondCommit - 1,
                    agent, -1, RestartCue.None, engine.BallView.Position, MatchEngineConstants.NO_POSSESSION));
            }
            engine.TestOnly_SetPhysicsFrame(secondCommit);
            census.ObserveHeading(CommitSample(engine, agent, secondCommit));
            engine.TestOnly_SetPhysicsFrame(contact);
            engine.TestOnly_ForceBallLoose(new Vector3(state.Position.x, state.Position.y,
                HeadingJumpKinematics.ComputeHeadZ(start, JumpReach(engine, agent), contact)), Vector3.zero);
            census.ObserveFrame();
            census.Finish();

            Assert.That(census.Counters.ActualCommits, Is.EqualTo(2));
            Assert.That(census.Counters.Arrivals, Is.EqualTo(1));
            Assert.That(census.Counters.EarlierSamePlayerCommit, Is.EqualTo(resetFlight ? 0 : 1));
            StringAssert.Contains($",False,True,{!resetFlight},False,", report.ToString());
        }

        private static int FirstOutfielder(MatchEngine engine)
        {
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
                if (!engine.AgentIsGoalkeeper(i)) return i;
            throw new AssertionException("fixture requires an outfield player");
        }

        private static float JumpReach(MatchEngine engine, int agent)
        {
            TacticalDirector.PlayerDatabase.PlayerAttributes canonical = engine.TestOnly_CanonicalAttributes(agent);
            HeadingAgentAttributes attrs = PlayerAttributeProjection.ToHeading(
                in canonical, engine.AgentTeamId(agent), fatigue: 0.0f);
            return HeadingJumpKinematics.ComputeJumpReach(attrs);
        }

        private static HeadingReachabilitySample JumpStartSample(MatchEngine engine, int agent, int frame, int start) =>
            new HeadingReachabilitySample(HeadingReachabilityKind.JumpStart, agent, frame, false,
                engine.AgentView(agent), engine.BallView, 0, start, JumpReach(engine, agent),
                default(EligibilityResult), default(FailureCause));

        private static HeadingReachabilitySample CommitSample(MatchEngine engine, int agent, int frame) =>
            new HeadingReachabilitySample(HeadingReachabilityKind.Commit, agent, frame, false,
                engine.AgentView(agent), engine.BallView, 0, -1, 0,
                default(EligibilityResult), default(FailureCause));

        private static string Inv(FormattableString value) => FormattableString.Invariant(value);

        private static string Digest(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();

        private static int Bucket(float value, float[] edges)
        {
            for (int i = 0; i < edges.Length; i++)
            {
                if (value <= edges[i])
                {
                    return i;
                }
            }
            return edges.Length;
        }

        private static int Bucket(int value, int[] edges)
        {
            for (int i = 0; i < edges.Length; i++)
            {
                if (value <= edges[i])
                {
                    return i;
                }
            }
            return edges.Length;
        }

        private static float[] DistanceEdges()
        {
            var edges = (float[])DistanceEdgesM.Clone();
            edges[0] = HeadingMechanicsConstants.HeadContactVolumeRadiusM;
            return edges;
        }

        // The §3.2 gravity-only ball predictor, restated so counterfactual heads can be tested against it.
        private static Vector3 PredictBall(in BallState ball, float dtS)
        {
            return new Vector3(
                ball.Position.x + ball.Velocity.x * dtS,
                ball.Position.y + ball.Velocity.y * dtS,
                ball.Position.z + ball.Velocity.z * dtS
                    - HeadingMechanicsConstants.KINEMATIC_HALF_COEFF * HeadingMechanicsConstants.GravityMps2 * dtS * dtS);
        }

        private enum HeadModel
        {
            StaticHeight,       // §3.2 as shipped: head held at this frame's height and position
            TrajectoryHeight,   // head height follows the §3.3 jump parabola; position held
            TrajectoryMoving,   // as above, and the head moves at the agent's current velocity
        }

        // Searches [frame, apex + late] as §3.2 FindContactFrame does, under a chosen head model, and returns
        // the FIRST frame whose predicted ball centre is inside the head volume, or -1. A hit is geometric
        // only: #10 then rejects a first frame outside the timing window (§3.2 step 5), see IsTimingValid.
        private static int FirstContactFrame(in HeadingReachabilitySample s, HeadModel model)
        {
            float radiusSq = HeadingMechanicsConstants.HeadContactVolumeRadiusM * HeadingMechanicsConstants.HeadContactVolumeRadiusM;
            int apex = HeadingJumpKinematics.ComputeApexFrame(s.JumpStartFrame);
            int end = apex + HeadingMechanicsConstants.FramesLateTolerance;
            for (int f = s.Frame; f <= end; f++)
            {
                float dt = (f - s.Frame) * HeadingMechanicsConstants.FrameS;
                Vector3 ball = PredictBall(in s.Ball, dt);
                Vector2 head = s.Agent.Position;
                float headZ = s.HeadZ;
                if (model != HeadModel.StaticHeight)
                {
                    headZ = HeadingJumpKinematics.ComputeHeadZ(s.JumpStartFrame, s.JumpReachM, f);
                }
                if (model == HeadModel.TrajectoryMoving)
                {
                    head += s.Agent.Velocity * dt;
                }
                float dx = ball.x - head.x;
                float dy = ball.y - head.y;
                float dz = ball.z - headZ;
                if (dx * dx + dy * dy + dz * dz <= radiusSq)
                {
                    return f;
                }
            }
            return -1;
        }

        // §3.2 step 5: a first contact frame earlier than apex - early is MistimedEarly, later than
        // apex + late is MistimedLate. Only a frame inside the window could become a prepared contact.
        private static bool IsTimingValid(int contactFrame, int jumpStartFrame)
        {
            int apex = HeadingJumpKinematics.ComputeApexFrame(jumpStartFrame);
            return contactFrame >= apex - HeadingMechanicsConstants.FramesEarlyTolerance
                && contactFrame <= apex + HeadingMechanicsConstants.FramesLateTolerance;
        }

        private static bool IsAerialCheckFailure(in AgentState agent) =>
            agent.CurrentState == AgentMovementState.GROUNDED || agent.CurrentState == AgentMovementState.STUMBLING;

        private enum ArrivalLaunchKind
        {
            Unknown,
            Pass,
            Shot,
            GkOrHeader,
            FirstTouch,
            Deflection,
            Bounce,
            Restart,
        }

        private struct ArrivalHit
        {
            internal bool Any;
            internal bool Covered;
            internal bool TacticalStart;
            internal bool EarlierSamePlayerCommit;
            internal bool OtherPlayerCommit;
            internal int StartFrame;
            internal float DistanceM;
            internal float HeadZM;
            internal int CoveredStartFrame;
            internal int TacticalStartFrame;
        }

        private sealed class Arrival
        {
            internal const string CsvHeader =
                "arrival,seed,flight,frame,agent,team,startFrame,distanceM,ballX,ballY,ballZ,ballVx,ballVy,ballVz,headZ,source,sourceFrame,sourceAgent,sourceTeam,covered,tacticalStart,earlierSamePlayerCommit,otherPlayerCommit,coveredContactFrame,coveredStartFrame,tacticalContactFrame,tacticalStartFrame";

            internal int FlightId;
            internal int Frame;
            internal int Agent;
            internal int Team;
            internal int StartFrame;
            internal float DistanceM;
            internal float HeadZM;
            internal BallState Ball;
            internal ArrivalLaunchKind Source;
            internal int SourceFrame;
            internal int SourceAgent;
            internal int SourceTeam;
            internal bool Covered;
            internal bool TacticalStart;
            internal bool EarlierSamePlayerCommit;
            internal bool OtherPlayerCommit;
            internal int CoveredContactFrame = -1;
            internal int CoveredStartFrame = -1;
            internal int TacticalContactFrame = -1;
            internal int TacticalStartFrame = -1;
        }

        private sealed class ArrivalCounters
        {
            internal int PotentialWindows;
            internal int ActualCommits;
            internal int Arrivals;
            internal int TacticalStartArrivals;
            internal int CoveredByCurrentCommit;
            internal int MissedByCurrentTrigger;
            internal int EarlierSamePlayerCommit;
            internal int OtherPlayerCommit;
            internal readonly int[] BySource = new int[Enum.GetValues(typeof(ArrivalLaunchKind)).Length];
            internal readonly int[] CommitsBySource = new int[Enum.GetValues(typeof(ArrivalLaunchKind)).Length];

            internal void Add(ArrivalCounters other)
            {
                PotentialWindows += other.PotentialWindows;
                ActualCommits += other.ActualCommits;
                Arrivals += other.Arrivals;
                TacticalStartArrivals += other.TacticalStartArrivals;
                CoveredByCurrentCommit += other.CoveredByCurrentCommit;
                MissedByCurrentTrigger += other.MissedByCurrentTrigger;
                EarlierSamePlayerCommit += other.EarlierSamePlayerCommit;
                OtherPlayerCommit += other.OtherPlayerCommit;
                for (int i = 0; i < BySource.Length; i++)
                {
                    BySource[i] += other.BySource[i];
                    CommitsBySource[i] += other.CommitsBySource[i];
                }
            }

            internal void Write(StringBuilder report, string label)
            {
                report.AppendLine(Inv(
                    $"{label},windows={PotentialWindows},actualCommits={ActualCommits},arrivals={Arrivals},tacticalStartArrivals={TacticalStartArrivals},covered={CoveredByCurrentCommit},missed={MissedByCurrentTrigger},earlierSamePlayerCommit={EarlierSamePlayerCommit},otherPlayerCommit={OtherPlayerCommit},sources[unknown/pass/shot/gkOrHeader/firstTouch/deflection/bounce/restart]={Join(BySource)}"));
                report.AppendLine(Inv($"{label},commitSources[unknown/pass/shot/gkOrHeader/firstTouch/deflection/bounce/restart]={Join(CommitsBySource)}"));
            }

            private static string Join(int[] values)
            {
                var text = new StringBuilder();
                for (int i = 0; i < values.Length; i++)
                {
                    if (i > 0)
                    {
                        text.Append('/');
                    }
                    text.Append(values[i].ToString(CultureInfo.InvariantCulture));
                }
                return text.ToString();
            }
        }

        /// <summary>
        /// Trigger-independent #441 census. Every 60 Hz frame opens a hypothetical production-shaped
        /// jump for every active outfielder even while possession is assigned, since a deferred #10
        /// intent may start then. The following real 60 Hz trajectory
        /// is then tested against #10's own jump curve and contact/timing envelope. No production trigger,
        /// pass label or predicted ball path participates in deciding that an arrival existed.
        /// Off-stride starts deliberately form an upper bound; tactical-start arrivals are separate.
        /// </summary>
        private sealed class ArrivalCensus
        {
            private readonly ulong _seed;
            private readonly MatchEngine _engine;
            private readonly StringBuilder _report;
            // Bounded start history: one jump reach (zero means ineligible) per agent/physics frame.
            // Contact checks first reject horizontal misses, then examine only timing-valid ages.
            private readonly int _firstContactAge;
            private readonly int _lastContactAge;
            private readonly int[] _startFrames;
            private readonly float[] _jumpReachHistory;
            private readonly ArrivalHit[] _hits = new ArrivalHit[MatchEngineConstants.SQUAD_SIZE];
            private readonly int[] _firstCommitFrameOnFlight = new int[MatchEngineConstants.SQUAD_SIZE];
            private readonly int[] _actualStartFrames;
            private readonly int[] _contactStartFrames;
            private readonly int[] _firstCommitFlightByAgent = new int[MatchEngineConstants.SQUAD_SIZE];
            private readonly int[] _lastArrivalFrame = new int[MatchEngineConstants.SQUAD_SIZE];
            private readonly Arrival[] _pendingArrivals = new Arrival[MatchEngineConstants.SQUAD_SIZE];
            private readonly int[] _lastCommitByFlightAgent = new int[MatchEngineConstants.SQUAD_SIZE];

            private BallState _previousBall;
            private bool _hasPreviousBall;
            private int _flightId;
            private int _worldMutationSequence;
            private int _sampledMutationSequence;
            private int _lastObservedFrame = -1;
            private ArrivalLaunchKind _source = ArrivalLaunchKind.Unknown;
            private int _sourceFrame = -1;
            private int _sourceAgent = -1;
            private int _sourceTeam = -1;

            internal readonly ArrivalCounters Counters = new ArrivalCounters();

            internal ArrivalCensus(ulong seed, MatchEngine engine, StringBuilder report)
            {
                _seed = seed;
                _engine = engine;
                _report = report;
                int apexAge = HeadingJumpKinematics.ComputeApexFrame(0);
                _firstContactAge = Math.Max(0, apexAge - HeadingMechanicsConstants.FramesEarlyTolerance);
                _lastContactAge = apexAge + HeadingMechanicsConstants.FramesLateTolerance;
                _startFrames = new int[_lastContactAge + 1];
                _jumpReachHistory = new float[_startFrames.Length * MatchEngineConstants.SQUAD_SIZE];
                _actualStartFrames = new int[_jumpReachHistory.Length];
                _contactStartFrames = new int[_jumpReachHistory.Length];
                for (int i = 0; i < _startFrames.Length; i++) _startFrames[i] = -1;
                for (int i = 0; i < _actualStartFrames.Length; i++)
                {
                    _actualStartFrames[i] = -1;
                    _contactStartFrames[i] = -1;
                }
                for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
                {
                    _firstCommitFrameOnFlight[i] = -1;
                    _firstCommitFlightByAgent[i] = -1;
                    _lastArrivalFrame[i] = int.MinValue;
                    _lastCommitByFlightAgent[i] = -1;
                }
            }

            internal void ObserveHeading(HeadingReachabilitySample sample)
            {
                if (sample.Kind == HeadingReachabilityKind.JumpStart)
                {
                    int startFrame = sample.JumpStartFrame;
                    int index = (startFrame % _startFrames.Length) * _hits.Length + sample.AgentId;
                    _actualStartFrames[index] = startFrame;
                    // Heading reports this start after the pre-Heading world sample. A contact
                    // on that same frame must still see the actual start that produced it.
                    if (_lastObservedFrame == sample.Frame
                        && _contactStartFrames[index] == startFrame
                        && _pendingArrivals[sample.AgentId] is Arrival pending
                        && pending.FlightId == _flightId)
                    {
                        SetCoverageWitness(pending, sample.Frame, startFrame);
                    }
                }
                if (sample.Kind != HeadingReachabilityKind.Commit)
                {
                    return;
                }

                Counters.ActualCommits++;
                Counters.CommitsBySource[(int)_source]++;
                _report.AppendLine(Inv($"commitSource,0x{_seed:X16},{sample.Frame},{sample.AgentId},{_engine.AgentTeamId(sample.AgentId)},{_flightId},{_source},{_sourceFrame},{_sourceAgent},{_sourceTeam}"));
                if (_firstCommitFlightByAgent[sample.AgentId] != _flightId)
                {
                    _firstCommitFrameOnFlight[sample.AgentId] = sample.Frame;
                    _firstCommitFlightByAgent[sample.AgentId] = _flightId;
                }
                _lastCommitByFlightAgent[sample.AgentId] = _flightId;
            }

            internal void ObserveWorldEvent(W8StageAEvent evt)
            {
                ArrivalLaunchKind kind;
                switch (evt.Kind)
                {
                    case W8StageAKind.PassKick: kind = ArrivalLaunchKind.Pass; break;
                    case W8StageAKind.ShotKick: kind = ArrivalLaunchKind.Shot; break;
                    case W8StageAKind.GkHeadingKick: kind = ArrivalLaunchKind.GkOrHeader; break;
                    case W8StageAKind.FirstTouch: kind = ArrivalLaunchKind.FirstTouch; break;
                    case W8StageAKind.UnattributedDeflection: kind = ArrivalLaunchKind.Deflection; break;
                    case W8StageAKind.Restart: kind = ArrivalLaunchKind.Restart; break;
                    default: return;
                }

                int team = -1;
                if (evt.Kind == W8StageAKind.Restart)
                {
                    team = evt.Agent;
                }
                else if (evt.Agent >= 0 && evt.Agent < MatchEngineConstants.SQUAD_SIZE)
                {
                    team = _engine.AgentTeamId(evt.Agent);
                }
                BeginFlight(kind, evt.Frame, evt.Agent, team);
            }

            internal void ObserveFrame()
            {
                int frame = (int)_engine.CurrentTick;
                BallState ball = _engine.BallView;

                // A bounce is a new physical leg. Prefer an authoritative same-frame kick/touch event
                // when one exists; otherwise detect the falling-to-rising reversal at the ball surface.
                if (_hasPreviousBall
                    && _worldMutationSequence == _sampledMutationSequence
                    && _previousBall.Velocity.z < 0.0f
                    && ball.Velocity.z > 0.0f
                    && ball.Position.z <= HeadingMechanicsConstants.HeadContactVolumeHeightM)
                {
                    BeginFlight(ArrivalLaunchKind.Bounce, frame, -1, -1);
                }

                OpenPotentialWindows(frame);

                Array.Clear(_hits, 0, _hits.Length);
                for (int i = 0; i < _contactStartFrames.Length; i++) _contactStartFrames[i] = -1;
                float radius = HeadingMechanicsConstants.HeadContactVolumeRadiusM;
                float radiusSq = radius * radius;

                for (int agentId = 0; agentId < _hits.Length; agentId++)
                {
                    if (_engine.TestOnly_PossessingAgentId != MatchEngineConstants.NO_POSSESSION
                        || _engine.AgentIsGoalkeeper(agentId) || _engine.TestOnly_IsSentOff(agentId))
                    {
                        continue;
                    }

                    AgentState agent = _engine.AgentView(agentId);
                    if (IsAerialCheckFailure(in agent))
                    {
                        continue;
                    }

                    float dx = ball.Position.x - agent.Position.x;
                    float dy = ball.Position.y - agent.Position.y;
                    float horizontalSq = dx * dx + dy * dy;
                    if (horizontalSq > radiusSq)
                    {
                        continue;
                    }

                    // Source/flight changes never erase this history: a jump can meet a deflection.
                    for (int age = _firstContactAge; age <= _lastContactAge && age <= frame; age++)
                    {
                        int startFrame = frame - age;
                        int slot = startFrame % _startFrames.Length;
                        if (_startFrames[slot] != startFrame) continue;
                        float reach = _jumpReachHistory[slot * _hits.Length + agentId];
                        if (reach <= 0.0f) continue;
                        float headZ = HeadingJumpKinematics.ComputeHeadZ(startFrame, reach, frame);
                        float dz = ball.Position.z - headZ;
                        float distanceSq = horizontalSq + dz * dz;
                        if (distanceSq > radiusSq
                            || Mathf.Abs(dz) > HeadingMechanicsConstants.HeadContactVolumeHeightM) continue;

                        ref ArrivalHit hit = ref _hits[agentId];
                        _contactStartFrames[slot * _hits.Length + agentId] = startFrame;
                        float distance = Mathf.Sqrt(distanceSq);
                        bool covered = _actualStartFrames[slot * _hits.Length + agentId] == startFrame;
                        bool tacticalStart = startFrame % HeadingMechanicsConstants.FramesPerTacticalTick == 0;
                        if (!hit.Any || (covered && !hit.Covered) || (!hit.Covered && distance < hit.DistanceM))
                        {
                            hit.Any = true;
                            hit.StartFrame = startFrame;
                            hit.DistanceM = distance;
                            hit.HeadZM = headZ;
                        }
                        if (covered && !hit.Covered)
                        {
                            hit.Covered = true;
                            hit.CoveredStartFrame = startFrame;
                        }
                        if (tacticalStart && !hit.TacticalStart) hit.TacticalStartFrame = startFrame;
                        hit.TacticalStart |= tacticalStart;
                        hit.EarlierSamePlayerCommit |= _firstCommitFlightByAgent[agentId] == _flightId
                                                       && _firstCommitFrameOnFlight[agentId] < startFrame;
                        hit.OtherPlayerCommit |= AnyOtherPlayerCommittedOnFlight(agentId);
                    }
                }

                for (int agent = 0; agent < _hits.Length; agent++)
                {
                    ArrivalHit hit = _hits[agent];
                    Arrival pending = _pendingArrivals[agent];
                    bool continuing = hit.Any && pending != null && pending.FlightId == _flightId
                                      && frame - _lastArrivalFrame[agent] == 1;
                    if (pending != null && !continuing)
                    {
                        WriteArrival(pending);
                        _pendingArrivals[agent] = null;
                    }
                    if (!hit.Any)
                    {
                        continue;
                    }
                    if (!continuing)
                    {
                        RecordArrival(frame, agent, in ball, in hit);
                    }
                    else
                    {
                        pending.Covered |= hit.Covered;
                        pending.TacticalStart |= hit.TacticalStart;
                        if (hit.Covered && pending.CoveredContactFrame < 0)
                            SetCoverageWitness(pending, frame, hit.CoveredStartFrame);
                        if (hit.TacticalStart && pending.TacticalContactFrame < 0)
                            SetTacticalWitness(pending, frame, hit.TacticalStartFrame);
                        pending.EarlierSamePlayerCommit |= hit.EarlierSamePlayerCommit;
                        pending.OtherPlayerCommit |= hit.OtherPlayerCommit;
                    }
                    _lastArrivalFrame[agent] = frame;
                }

                _previousBall = ball;
                _hasPreviousBall = true;
                _sampledMutationSequence = _worldMutationSequence;
                _lastObservedFrame = frame;
            }

            internal void Finish()
            {
                for (int agent = 0; agent < _pendingArrivals.Length; agent++)
                {
                    if (_pendingArrivals[agent] != null)
                    {
                        WriteArrival(_pendingArrivals[agent]);
                        _pendingArrivals[agent] = null;
                    }
                }
            }

            private void OpenPotentialWindows(int frame)
            {
                int slot = frame % _startFrames.Length;
                _startFrames[slot] = frame;
                int offset = slot * _hits.Length;
                Array.Clear(_jumpReachHistory, offset, _hits.Length);
                for (int agentId = 0; agentId < MatchEngineConstants.SQUAD_SIZE; agentId++)
                {
                    if (_engine.AgentIsGoalkeeper(agentId) || _engine.TestOnly_IsSentOff(agentId))
                    {
                        continue;
                    }

                    AgentState state = _engine.AgentView(agentId);
                    if (IsAerialCheckFailure(in state))
                    {
                        continue;
                    }

                    int team = _engine.AgentTeamId(agentId);
                    TacticalDirector.PlayerDatabase.PlayerAttributes canonical =
                        _engine.TestOnly_CanonicalAttributes(agentId);
                    HeadingAgentAttributes attrs = PlayerAttributeProjection.ToHeading(
                        in canonical, team, fatigue: 0.0f);
                    _jumpReachHistory[offset + agentId] = HeadingJumpKinematics.ComputeJumpReach(attrs);
                    Counters.PotentialWindows++;
                }
            }

            private bool AnyOtherPlayerCommittedOnFlight(int agentId)
            {
                for (int i = 0; i < _lastCommitByFlightAgent.Length; i++)
                {
                    if (i != agentId && _lastCommitByFlightAgent[i] == _flightId)
                    {
                        return true;
                    }
                }
                return false;
            }

            private void BeginFlight(ArrivalLaunchKind kind, int frame, int agent, int team)
            {
                _flightId++;
                _source = kind;
                _sourceFrame = frame;
                _sourceAgent = agent;
                _sourceTeam = team;
                _worldMutationSequence++;
            }

            private void RecordArrival(int frame, int agent, in BallState ball, in ArrivalHit hit)
            {
                var arrival = new Arrival
                {
                    FlightId = _flightId,
                    Frame = frame,
                    Agent = agent,
                    Team = _engine.AgentTeamId(agent),
                    StartFrame = hit.StartFrame,
                    DistanceM = hit.DistanceM,
                    HeadZM = hit.HeadZM,
                    Ball = ball,
                    Source = _source,
                    SourceFrame = _sourceFrame,
                    SourceAgent = _sourceAgent,
                    SourceTeam = _sourceTeam,
                    Covered = hit.Covered,
                    TacticalStart = hit.TacticalStart,
                    EarlierSamePlayerCommit = hit.EarlierSamePlayerCommit,
                    OtherPlayerCommit = hit.OtherPlayerCommit,
                };
                _pendingArrivals[agent] = arrival;
                if (hit.Covered) SetCoverageWitness(arrival, frame, hit.CoveredStartFrame);
                if (hit.TacticalStart) SetTacticalWitness(arrival, frame, hit.TacticalStartFrame);
            }

            private static void SetCoverageWitness(Arrival arrival, int contactFrame, int startFrame)
            {
                arrival.Covered = true;
                if (arrival.CoveredContactFrame >= 0) return;
                arrival.CoveredContactFrame = contactFrame;
                arrival.CoveredStartFrame = startFrame;
            }

            private static void SetTacticalWitness(Arrival arrival, int contactFrame, int startFrame)
            {
                arrival.TacticalStart = true;
                if (arrival.TacticalContactFrame >= 0) return;
                arrival.TacticalContactFrame = contactFrame;
                arrival.TacticalStartFrame = startFrame;
            }

            private void WriteArrival(Arrival arrival)
            {
                Counters.Arrivals++;
                if (arrival.TacticalStart) Counters.TacticalStartArrivals++;
                if (arrival.Covered) Counters.CoveredByCurrentCommit++;
                else Counters.MissedByCurrentTrigger++;
                if (arrival.EarlierSamePlayerCommit) Counters.EarlierSamePlayerCommit++;
                if (arrival.OtherPlayerCommit) Counters.OtherPlayerCommit++;
                Counters.BySource[(int)arrival.Source]++;

                _report.AppendLine(
                    Inv($"arrival,0x{_seed:X16},{arrival.FlightId},{arrival.Frame},{arrival.Agent},{arrival.Team},{arrival.StartFrame},{arrival.DistanceM:F3},")
                    + Inv($"{arrival.Ball.Position.x:F3},{arrival.Ball.Position.y:F3},{arrival.Ball.Position.z:F3},")
                    + Inv($"{arrival.Ball.Velocity.x:F3},{arrival.Ball.Velocity.y:F3},{arrival.Ball.Velocity.z:F3},{arrival.HeadZM:F3},")
                    + Inv($"{arrival.Source},{arrival.SourceFrame},{arrival.SourceAgent},{arrival.SourceTeam},{arrival.Covered},{arrival.TacticalStart},{arrival.EarlierSamePlayerCommit},{arrival.OtherPlayerCommit},{arrival.CoveredContactFrame},{arrival.CoveredStartFrame},{arrival.TacticalContactFrame},{arrival.TacticalStartFrame}"));
            }
        }

        private sealed class Episode
        {
            internal const string CsvHeader =
                "ep,seed,agent,outcome,commitFrame,jumpStartFrame,apexFrame,terminalFrame,"
                + "commitBallX,commitBallY,commitBallZ,commitBallVx,commitBallVy,commitBallVz,"
                + "jumpAgentX,jumpAgentY,jumpAgentState,jumpReachM,evaluations,"
                + "minDist3dM,minDistXyM,dzAtMinXyM,everPredicted,bodyPartMismatch,"
                + "cfTrajectoryHit,cfTrajectoryTimingValid,cfTrajectoryMovingHit,cfTrajectoryMovingTimingValid,forecastMinXyM,forecastFrameMinusApex,forecastZAtMinM,"
                + "terminalAgentState";

            internal int Agent;
            internal int CommitFrame;
            internal BallState CommitBall;
            internal int JumpStartFrame = -1;
            internal int ApexFrame = -1;
            internal Vector2 JumpAgentPosition;
            internal AgentMovementState JumpAgentState;
            internal float JumpReachM;
            internal int Evaluations;
            internal float MinDist3d = float.MaxValue;
            internal float MinDistXy = float.MaxValue;
            internal float DzAtMinXy;
            internal bool EverPredicted;
            internal bool BodyPartMismatch;
            internal bool CfTrajectoryHit;
            internal bool CfTrajectoryTimingValid;
            internal bool CfTrajectoryMovingHit;
            internal bool CfTrajectoryMovingTimingValid;
            internal float ForecastMinXy = float.NaN;
            internal int ForecastFrameMinusApex;
            internal float ForecastZAtMin = float.NaN;
            internal string TerminalAgentState = "na";
        }

        private sealed class Counters
        {
            internal int Commits, Overwrites, LiveCancels, JumpStarts, Evaluations;
            internal int EverPredicted, BodyPartMismatch, Prepared, Executed;
            internal int FailedEarly, FailedLate, FailedPoorly, FailedDisturbed, LandingDrops;
            internal int OpenAtEnd, NeverJumped;
            internal int PoorlyAerialCheck, PoorlyNoContactFrame;
            internal int PoorlyCfTrajectoryHit, PoorlyCfTrajectoryTimingValid;
            internal int PoorlyCfTrajectoryMovingHit, PoorlyCfTrajectoryMovingTimingValid;
            internal int ReplicaMismatchFrames;
            internal int[] PoorlyMinDist3d = new int[DistanceEdgesM.Length + 1];
            internal int[] PoorlyMinDistXy = new int[DistanceEdgesM.Length + 1];
            internal int PoorlyBallAboveHead, PoorlyBallWithinHeadBand, PoorlyBallBelowHead;
            internal int[] PoorlyForecastMinXy = new int[DistanceEdgesM.Length + 1];
            internal int PoorlyForecastBeforeWindow, PoorlyForecastInWindow, PoorlyForecastAfterWindow;
            internal int[] CommitBallZ = new int[CommitBallZEdgesM.Length + 1];
            internal int[] JumpLag = new int[JumpLagEdgesFrames.Length + 1];

            internal void Add(Counters o)
            {
                Commits += o.Commits; Overwrites += o.Overwrites; LiveCancels += o.LiveCancels;
                JumpStarts += o.JumpStarts; Evaluations += o.Evaluations;
                EverPredicted += o.EverPredicted; BodyPartMismatch += o.BodyPartMismatch;
                Prepared += o.Prepared; Executed += o.Executed;
                FailedEarly += o.FailedEarly; FailedLate += o.FailedLate;
                FailedPoorly += o.FailedPoorly; FailedDisturbed += o.FailedDisturbed;
                LandingDrops += o.LandingDrops; OpenAtEnd += o.OpenAtEnd; NeverJumped += o.NeverJumped;
                PoorlyAerialCheck += o.PoorlyAerialCheck; PoorlyNoContactFrame += o.PoorlyNoContactFrame;
                PoorlyCfTrajectoryHit += o.PoorlyCfTrajectoryHit;
                PoorlyCfTrajectoryTimingValid += o.PoorlyCfTrajectoryTimingValid;
                PoorlyCfTrajectoryMovingHit += o.PoorlyCfTrajectoryMovingHit;
                PoorlyCfTrajectoryMovingTimingValid += o.PoorlyCfTrajectoryMovingTimingValid;
                ReplicaMismatchFrames += o.ReplicaMismatchFrames;
                AddArray(PoorlyMinDist3d, o.PoorlyMinDist3d);
                AddArray(PoorlyMinDistXy, o.PoorlyMinDistXy);
                PoorlyBallAboveHead += o.PoorlyBallAboveHead;
                PoorlyBallWithinHeadBand += o.PoorlyBallWithinHeadBand;
                PoorlyBallBelowHead += o.PoorlyBallBelowHead;
                AddArray(PoorlyForecastMinXy, o.PoorlyForecastMinXy);
                PoorlyForecastBeforeWindow += o.PoorlyForecastBeforeWindow;
                PoorlyForecastInWindow += o.PoorlyForecastInWindow;
                PoorlyForecastAfterWindow += o.PoorlyForecastAfterWindow;
                AddArray(CommitBallZ, o.CommitBallZ);
                AddArray(JumpLag, o.JumpLag);
            }

            private static void AddArray(int[] into, int[] from)
            {
                for (int i = 0; i < into.Length; i++)
                {
                    into[i] += from[i];
                }
            }

            private static string Join(int[] values)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < values.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append('/');
                    }
                    sb.Append(values[i].ToString(CultureInfo.InvariantCulture));
                }
                return sb.ToString();
            }

            internal void Write(StringBuilder report, string label)
            {
                report.AppendLine(Inv($"{label},lifecycle,commits={Commits},overwrites={Overwrites},liveCancels={LiveCancels},jumpStarts={JumpStarts},neverJumped={NeverJumped},evaluations={Evaluations},everPredicted={EverPredicted},bodyPartMismatch={BodyPartMismatch},prepared={Prepared},executed={Executed},failedEarly={FailedEarly},failedLate={FailedLate},failedPositionedPoorly={FailedPoorly},failedDisturbed={FailedDisturbed},landingDrops={LandingDrops},openAtEnd={OpenAtEnd},replicaMismatchFrames={ReplicaMismatchFrames}"));
                report.AppendLine(Inv($"{label},poorly,aerialCheck={PoorlyAerialCheck},noContactFrame={PoorlyNoContactFrame},cfTrajectoryHit={PoorlyCfTrajectoryHit},cfTrajectoryTimingValid={PoorlyCfTrajectoryTimingValid},cfTrajectoryMovingHit={PoorlyCfTrajectoryMovingHit},cfTrajectoryMovingTimingValid={PoorlyCfTrajectoryMovingTimingValid},ballAboveHead={PoorlyBallAboveHead},ballWithinHeadBand={PoorlyBallWithinHeadBand},ballBelowHead={PoorlyBallBelowHead}"));
                report.AppendLine(Inv($"{label},poorlyMinDist3d[r/0.5/1/2/5/inf]={Join(PoorlyMinDist3d)},poorlyMinDistXy[r/0.5/1/2/5/inf]={Join(PoorlyMinDistXy)}"));
                report.AppendLine(Inv($"{label},poorlyForecast,minXy[r/0.5/1/2/5/inf]={Join(PoorlyForecastMinXy)},beforeWindow={PoorlyForecastBeforeWindow},inWindow={PoorlyForecastInWindow},afterWindow={PoorlyForecastAfterWindow}"));
                report.AppendLine(Inv($"{label},commits,ballZ[1.0/1.6/2.0/2.6/inf]={Join(CommitBallZ)},jumpLagFrames[0/5/11/inf]={Join(JumpLag)}"));
            }
        }

        private sealed class Census
        {
            private readonly ulong _seed;
            private readonly StringBuilder _report;
            private readonly Dictionary<int, Episode> _open = new Dictionary<int, Episode>();
            private readonly float[] _distanceEdges = DistanceEdges();

            internal readonly Counters Counters = new Counters();

            internal Census(ulong seed, StringBuilder report)
            {
                _seed = seed;
                _report = report;
            }

            internal void Observe(HeadingReachabilitySample s)
            {
                switch (s.Kind)
                {
                    case HeadingReachabilityKind.Commit:
                        if (s.WasActive && _open.TryGetValue(s.AgentId, out Episode overwritten))
                        {
                            Counters.Overwrites++;
                            Close(overwritten, "overwritten", s.Frame, "na");
                        }
                        Counters.Commits++;
                        Counters.CommitBallZ[Bucket(s.Ball.Position.z, CommitBallZEdgesM)]++;
                        _open[s.AgentId] = new Episode
                        {
                            Agent = s.AgentId,
                            CommitFrame = s.Frame,
                            CommitBall = s.Ball,
                        };
                        break;

                    case HeadingReachabilityKind.Cancel:
                        if (s.WasActive && _open.TryGetValue(s.AgentId, out Episode cancelled))
                        {
                            Counters.LiveCancels++;
                            Close(cancelled, "cancelled", s.Frame, "na");
                        }
                        break;

                    case HeadingReachabilityKind.JumpStart:
                        if (_open.TryGetValue(s.AgentId, out Episode jumping))
                        {
                            Counters.JumpStarts++;
                            jumping.JumpStartFrame = s.JumpStartFrame;
                            jumping.ApexFrame = HeadingJumpKinematics.ComputeApexFrame(s.JumpStartFrame);
                            jumping.JumpAgentPosition = s.Agent.Position;
                            jumping.JumpAgentState = s.Agent.CurrentState;
                            jumping.JumpReachM = s.JumpReachM;
                            Counters.JumpLag[Bucket(s.Frame - jumping.CommitFrame, JumpLagEdgesFrames)]++;
                            Forecast(jumping, in s);
                        }
                        break;

                    case HeadingReachabilityKind.Pending:
                    case HeadingReachabilityKind.Prepared:
                        if (_open.TryGetValue(s.AgentId, out Episode live))
                        {
                            Evaluate(live, in s);
                            if (s.Kind == HeadingReachabilityKind.Prepared)
                            {
                                Counters.Prepared++;
                            }
                        }
                        break;

                    case HeadingReachabilityKind.Failed:
                        if (_open.TryGetValue(s.AgentId, out Episode failed))
                        {
                            if (s.Cause != FailureCause.DisturbedInDuel)
                            {
                                Evaluate(failed, in s);
                            }
                            CountFailure(failed, in s);
                            Close(failed, s.Cause.ToString(), s.Frame, s.Agent.CurrentState.ToString());
                        }
                        break;

                    case HeadingReachabilityKind.LandingDrop:
                        if (_open.TryGetValue(s.AgentId, out Episode landed))
                        {
                            Counters.LandingDrops++;
                            Close(landed, "landingDrop", s.Frame, s.Agent.CurrentState.ToString());
                        }
                        break;

                    case HeadingReachabilityKind.Executed:
                        if (_open.TryGetValue(s.AgentId, out Episode executed))
                        {
                            Counters.Executed++;
                            Close(executed, "executed", s.Frame, s.Agent.CurrentState.ToString());
                        }
                        break;
                }
            }

            internal void Finish()
            {
                var remaining = new List<Episode>(_open.Values);
                remaining.Sort((a, b) => a.Agent.CompareTo(b.Agent));
                foreach (Episode e in remaining)
                {
                    Counters.OpenAtEnd++;
                    Close(e, "openAtEnd", -1, "na");
                }
            }

            private void Evaluate(Episode e, in HeadingReachabilitySample s)
            {
                Counters.Evaluations++;
                e.Evaluations++;

                Vector3 head = new Vector3(s.Agent.Position.x, s.Agent.Position.y, s.HeadZ);
                Vector3 delta = s.Ball.Position - head;
                float dist3d = delta.magnitude;
                float distXy = new Vector2(delta.x, delta.y).magnitude;
                if (dist3d < e.MinDist3d)
                {
                    e.MinDist3d = dist3d;
                }
                if (distXy < e.MinDistXy)
                {
                    e.MinDistXy = distXy;
                    e.DzAtMinXy = delta.z;
                }

                if (s.Eligibility.PredictedContactFrame >= 0)
                {
                    e.EverPredicted = true;
                    if (!s.Eligibility.IsEligible && s.Eligibility.MistimedDirection == MistimedDirection.None)
                    {
                        e.BodyPartMismatch = true;
                    }
                }

                if (IsAerialCheckFailure(in s.Agent))
                {
                    return;
                }

                // Self-check: the restated predictor must reproduce #10's first contact frame exactly on the
                // shipped head model (#10 returns that frame, or -1, from every branch past the aerial check).
                if (FirstContactFrame(in s, HeadModel.StaticHeight) != s.Eligibility.PredictedContactFrame)
                {
                    Counters.ReplicaMismatchFrames++;
                }

                // Counterfactuals keep #10's first-frame semantics: a geometric hit whose first frame is
                // outside the timing window would be a Mistimed failure, not a contact.
                int trajectoryFrame = FirstContactFrame(in s, HeadModel.TrajectoryHeight);
                if (trajectoryFrame >= 0)
                {
                    e.CfTrajectoryHit = true;
                    if (IsTimingValid(trajectoryFrame, s.JumpStartFrame))
                    {
                        e.CfTrajectoryTimingValid = true;
                    }
                }
                int movingFrame = FirstContactFrame(in s, HeadModel.TrajectoryMoving);
                if (movingFrame >= 0)
                {
                    e.CfTrajectoryMovingHit = true;
                    if (IsTimingValid(movingFrame, s.JumpStartFrame))
                    {
                        e.CfTrajectoryMovingTimingValid = true;
                    }
                }
            }

            // Where the ball, predicted from jump start, passes closest to the agent's jump-start position.
            private static void Forecast(Episode e, in HeadingReachabilitySample s)
            {
                int landingOffset = HeadingJumpKinematics.ComputeLandingFrame(s.JumpStartFrame) - s.JumpStartFrame;
                int horizon = landingOffset + ForecastExtraFrames;
                float best = float.MaxValue;
                int bestOffset = 0;
                float bestZ = 0.0f;
                for (int f = 0; f <= horizon; f++)
                {
                    Vector3 ball = PredictBall(in s.Ball, f * HeadingMechanicsConstants.FrameS);
                    float dxy = new Vector2(ball.x - s.Agent.Position.x, ball.y - s.Agent.Position.y).magnitude;
                    if (dxy < best)
                    {
                        best = dxy;
                        bestOffset = f;
                        bestZ = ball.z;
                    }
                }
                e.ForecastMinXy = best;
                e.ForecastFrameMinusApex = s.JumpStartFrame + bestOffset - e.ApexFrame;
                e.ForecastZAtMin = bestZ;
            }

            private void CountFailure(Episode e, in HeadingReachabilitySample s)
            {
                switch (s.Cause)
                {
                    case FailureCause.MistimedEarly:
                        Counters.FailedEarly++;
                        return;
                    case FailureCause.MistimedLate:
                        Counters.FailedLate++;
                        return;
                    case FailureCause.DisturbedInDuel:
                        Counters.FailedDisturbed++;
                        return;
                    case FailureCause.PositionedPoorly:
                        break;
                    default:
                        return;
                }

                Counters.FailedPoorly++;
                if (IsAerialCheckFailure(in s.Agent))
                {
                    Counters.PoorlyAerialCheck++;
                }
                else
                {
                    Counters.PoorlyNoContactFrame++;
                }
                if (e.CfTrajectoryHit)
                {
                    Counters.PoorlyCfTrajectoryHit++;
                }
                if (e.CfTrajectoryTimingValid)
                {
                    Counters.PoorlyCfTrajectoryTimingValid++;
                }
                if (e.CfTrajectoryMovingHit)
                {
                    Counters.PoorlyCfTrajectoryMovingHit++;
                }
                if (e.CfTrajectoryMovingTimingValid)
                {
                    Counters.PoorlyCfTrajectoryMovingTimingValid++;
                }

                Counters.PoorlyMinDist3d[Bucket(e.MinDist3d, _distanceEdges)]++;
                Counters.PoorlyMinDistXy[Bucket(e.MinDistXy, _distanceEdges)]++;
                float band = HeadingMechanicsConstants.HeadContactVolumeHeightM;
                if (e.DzAtMinXy > band)
                {
                    Counters.PoorlyBallAboveHead++;
                }
                else if (e.DzAtMinXy < -band)
                {
                    Counters.PoorlyBallBelowHead++;
                }
                else
                {
                    Counters.PoorlyBallWithinHeadBand++;
                }

                if (!float.IsNaN(e.ForecastMinXy))
                {
                    Counters.PoorlyForecastMinXy[Bucket(e.ForecastMinXy, _distanceEdges)]++;
                    if (e.ForecastFrameMinusApex < -HeadingMechanicsConstants.FramesEarlyTolerance)
                    {
                        Counters.PoorlyForecastBeforeWindow++;
                    }
                    else if (e.ForecastFrameMinusApex > HeadingMechanicsConstants.FramesLateTolerance)
                    {
                        Counters.PoorlyForecastAfterWindow++;
                    }
                    else
                    {
                        Counters.PoorlyForecastInWindow++;
                    }
                }
            }

            private void Close(Episode e, string outcome, int terminalFrame, string terminalState)
            {
                _open.Remove(e.Agent);
                if (e.JumpStartFrame < 0)
                {
                    Counters.NeverJumped++;
                }
                if (e.EverPredicted)
                {
                    Counters.EverPredicted++;
                }
                if (e.BodyPartMismatch)
                {
                    Counters.BodyPartMismatch++;
                }
                e.TerminalAgentState = terminalState;

                _report.AppendLine(
                    Inv($"ep,0x{_seed:X16},{e.Agent},{outcome},{e.CommitFrame},{e.JumpStartFrame},{e.ApexFrame},{terminalFrame},")
                    + Inv($"{e.CommitBall.Position.x:F3},{e.CommitBall.Position.y:F3},{e.CommitBall.Position.z:F3},")
                    + Inv($"{e.CommitBall.Velocity.x:F3},{e.CommitBall.Velocity.y:F3},{e.CommitBall.Velocity.z:F3},")
                    + Inv($"{e.JumpAgentPosition.x:F3},{e.JumpAgentPosition.y:F3},{e.JumpAgentState},{e.JumpReachM:F3},{e.Evaluations},")
                    + Inv($"{Finite(e.MinDist3d)},{Finite(e.MinDistXy)},{e.DzAtMinXy:F3},{e.EverPredicted},{e.BodyPartMismatch},")
                    + Inv($"{e.CfTrajectoryHit},{e.CfTrajectoryTimingValid},{e.CfTrajectoryMovingHit},{e.CfTrajectoryMovingTimingValid},{Finite(e.ForecastMinXy)},{e.ForecastFrameMinusApex},{Finite(e.ForecastZAtMin)},")
                    + Inv($"{e.TerminalAgentState}"));
            }

            private static string Finite(float value) =>
                float.IsNaN(value) || value == float.MaxValue
                    ? "na"
                    : value.ToString("F3", CultureInfo.InvariantCulture);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                       |
// | 1.0     | 2026-10-10 | —      | #441 counters: env-gated six-seed instrument plus observer digest gate.     |
// | 1.1     | 2026-10-11 | —      | PR #497 review: counterfactuals split geometric hits from timing-valid     |
// |         |            |        | first frames (§3.2 step 5); replica self-check compares the exact frame.    |
// | 1.2     | 2026-10-18 | —      | #441 trigger-independent arrival census: every tactical frame opens       |
// |         |            |        | production-shaped hypothetical jumps; actual ball/head intersections are  |
// |         |            |        | correlated with launch source and shipped-trigger commits. Measurement    |
// |         |            |        | only; no gameplay, snapshot, RNG or constant change.                       |
// | 1.3     | 2026-10-18 | —      | Review: retain windows across flight changes; sample before Heading.Update; |
// |         |            |        | 60 Hz start envelope plus tactical subset, commit provenance, complete   |
// |         |            |        | passage deduplication, mirrored deflection controls. Initial result invalid. |
// | 1.4     | 2026-10-19 | —      | Review: bounded actual-start history, between-sample mutation sequence, |
// |         |            |        | explicit CSV witness frames and three regression controls. Ten focused |
// |         |            |        | cases pass including digest neutrality; fresh corpus pending.           |
// | 1.5     | 2026-10-19 | —      | Preserve each agent's first commit on the current flight for the earlier  |
// |         |            |        | same-player context; add same-flight and reset-flight staged controls.   |
// | 1.6     | 2026-10-20 | —      | Include hypothetical starts during possession to cover deferred #10     |
// |         |            |        | takeoffs; mirror staged possession-at-start/loose-contact controls.      |
#endregion
