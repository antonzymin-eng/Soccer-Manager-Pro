// File:     src/match-engine/tests/HeaderReachabilityDiagnosticTests.cs
// Created:  2026-10-10
// Modified: 2026-10-10
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

                var total = new Counters();
                foreach (ulong seed in Seeds)
                {
                    var engine = new MatchEngine(seed);
                    engine.EnableGkHeading();
                    var census = new Census(seed, report);
                    engine.TestOnly_Heading.TestOnly_ReachabilityObserver = census.Observe;
                    for (int frame = 0; frame < TicksPerSeed; frame++)
                    {
                        engine.RunTick();
                    }
                    engine.TestOnly_Heading.TestOnly_ReachabilityObserver = null;
                    census.Finish();

                    census.Counters.Write(report, Inv($"seed,0x{seed:X16}"));
                    report.AppendLine(Inv($"seedEnd,0x{seed:X16},tick={engine.CurrentTick},digest={Digest(engine.CurrentSnapshotDigest)}"));
                    total.Add(census.Counters);
                }

                total.Write(report, "AGGREGATE");
                TestContext.WriteLine(report.ToString());

                // Internal consistency only. No figure is compared with an expected football value.
                Assert.That(total.Commits, Is.GreaterThan(0), "frozen corpus produced no header commits");
                Assert.That(total.JumpStarts, Is.LessThanOrEqualTo(total.Commits), "jump starts exceed commits");
                Assert.That(total.EverPredicted, Is.LessThanOrEqualTo(total.JumpStarts), "predicted episodes exceed jump starts");
                Assert.That(total.Prepared, Is.LessThanOrEqualTo(total.EverPredicted), "prepared contacts exceed predicted episodes");
                Assert.That(total.Executed, Is.LessThanOrEqualTo(total.Prepared), "executed headers exceed prepared contacts");
                Assert.That(total.FailedPoorly, Is.EqualTo(total.PoorlyAerialCheck + total.PoorlyNoContactFrame),
                    "PositionedPoorly decomposition does not sum");
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
            observed.TestOnly_Heading.TestOnly_ReachabilityObserver = sample =>
            {
                samples++;
                if (sample.Kind == HeadingReachabilityKind.Pending
                    || sample.Kind == HeadingReachabilityKind.Prepared
                    || sample.Kind == HeadingReachabilityKind.Failed)
                {
                    evaluations++;
                }
            };
            var digests = new byte[NeutralityFrames][];
            for (int frame = 0; frame < NeutralityFrames; frame++)
            {
                observed.RunTick();
                digests[frame] = observed.CurrentSnapshotDigest;
            }
            observed.TestOnly_Heading.TestOnly_ReachabilityObserver = null;

            // Non-vacuity: the window must contain real header evaluations, or identity proves nothing.
            Assert.That(evaluations, Is.GreaterThan(0),
                "no #10 evaluation was observed in the neutrality window; lengthen it");

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

        // Searches [frame, apex + late] as §3.2 FindContactFrame does, under a chosen head model.
        private static bool AnyContactFrame(in HeadingReachabilitySample s, HeadModel model)
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
                    return true;
                }
            }
            return false;
        }

        private static bool IsAerialCheckFailure(in AgentState agent) =>
            agent.CurrentState == AgentMovementState.GROUNDED || agent.CurrentState == AgentMovementState.STUMBLING;

        private sealed class Episode
        {
            internal const string CsvHeader =
                "ep,seed,agent,outcome,commitFrame,jumpStartFrame,apexFrame,terminalFrame,"
                + "commitBallX,commitBallY,commitBallZ,commitBallVx,commitBallVy,commitBallVz,"
                + "jumpAgentX,jumpAgentY,jumpAgentState,jumpReachM,evaluations,"
                + "minDist3dM,minDistXyM,dzAtMinXyM,everPredicted,bodyPartMismatch,"
                + "cfTrajectoryHit,cfTrajectoryMovingHit,forecastMinXyM,forecastFrameMinusApex,forecastZAtMinM,"
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
            internal bool CfTrajectoryMovingHit;
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
            internal int PoorlyCfTrajectoryHit, PoorlyCfTrajectoryMovingHit;
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
                PoorlyCfTrajectoryMovingHit += o.PoorlyCfTrajectoryMovingHit;
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
                report.AppendLine(Inv($"{label},poorly,aerialCheck={PoorlyAerialCheck},noContactFrame={PoorlyNoContactFrame},cfTrajectoryHit={PoorlyCfTrajectoryHit},cfTrajectoryMovingHit={PoorlyCfTrajectoryMovingHit},ballAboveHead={PoorlyBallAboveHead},ballWithinHeadBand={PoorlyBallWithinHeadBand},ballBelowHead={PoorlyBallBelowHead}"));
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

                // Self-check: the restated predictor must agree with #10 on the shipped head model.
                if (AnyContactFrame(in s, HeadModel.StaticHeight) != (s.Eligibility.PredictedContactFrame >= 0))
                {
                    Counters.ReplicaMismatchFrames++;
                }
                if (!e.CfTrajectoryHit && AnyContactFrame(in s, HeadModel.TrajectoryHeight))
                {
                    e.CfTrajectoryHit = true;
                }
                if (!e.CfTrajectoryMovingHit && AnyContactFrame(in s, HeadModel.TrajectoryMoving))
                {
                    e.CfTrajectoryMovingHit = true;
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
                if (e.CfTrajectoryMovingHit)
                {
                    Counters.PoorlyCfTrajectoryMovingHit++;
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
                    + Inv($"{e.CfTrajectoryHit},{e.CfTrajectoryMovingHit},{Finite(e.ForecastMinXy)},{e.ForecastFrameMinusApex},{Finite(e.ForecastZAtMin)},")
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
#endregion
