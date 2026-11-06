// File:     src/match-engine/tests/W8CHandControlTests.cs
// Created:  2026-11-06
// Modified: 2026-11-06
// Author:   —
// Spec:     #11 §3.8.5 / T-5.1.10.1–13; match-flow §5.3 (ERR-011-021).
// Purpose:  Mirrored exact-frame hand offence, CONTACT precedence, feet guard and durable-state locks.

using System;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;
using TacticalDirector.GoalkeeperMechanics;
using TacticalDirector.PassMechanics;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>Composed W8 C law and schema-v28 regression tests, mirrored for both ends.</summary>
    [TestFixture]
    public sealed class W8CHandControlTests
    {
        private const int Claim = 1001; // Last frame of its tactical cell: no 10 Hz quantization.

        [TestCase(0, 0)] [TestCase(1, 0)]
        [TestCase(0, 479)] [TestCase(1, 479)]
        [TestCase(0, 480)] [TestCase(1, 480)]
        [TestCase(0, 481)] [TestCase(1, 481)]
        public void OriginalExactClaimFrame_DeterminesCorner(int team, int elapsed)
        {
            MatchEngine engine = Holding(team);
            engine.TestOnly_SetPhysicsFrame(Claim + elapsed - 1);
            engine.RunTick();
            Assert.AreEqual(elapsed > 480 ? RestartCue.Corner : RestartCue.None, engine.RestartAppliedThisTick);
            if (elapsed > 480) AssertCorner(engine, team, 34f);
            else Assert.AreEqual((long)Claim, engine.TestOnly_GoalkeeperState.HandControlStartFrame[team]);
            Assert.AreEqual(0, Count<DistributionExecutedEvent>(engine));
        }

        [TestCase(0, 14f)] [TestCase(1, 14f)]
        [TestCase(0, 34f)] [TestCase(1, 34f)]
        [TestCase(0, 50f)] [TestCase(1, 50f)]
        public void Corner_UsesKeeperSideAndSharedWhistleFinish(int team, float y)
        {
            MatchEngine engine = Holding(team);
            Place(engine, team * 11, 8f, y);
            engine.TestOnly_SetPhysicsFrame(Claim + 480);
            engine.RunTick();
            AssertCorner(engine, team, y);
            Assert.AreEqual(1, engine.TestOnly_KeeperWhistleFinishCount);
            Assert.AreEqual(0, engine.TestOnly_GkHoldTicks);
            Assert.AreEqual(-1, engine.TestOnly_GkReleasedAgentId);
            Assert.AreEqual(0, engine.TestOnly_GkReleaseCooldownRemaining);
            Assert.AreEqual(-1L, engine.TestOnly_GoalkeeperState.HandControlStartFrame[team]);
            Assert.AreEqual(GoalkeeperState.Recovering, engine.TestOnly_GkState(team));
            int restart = Index<RestartAwardedEvent>(engine), possession = Index<PossessionChangedEvent>(engine);
            Assert.Greater(possession, restart, "whistle is published before possession change");
            engine.RunTick();
            Assert.AreEqual(0, Count<RestartAwardedEvent>(engine), "no delayed second whistle");
        }

        [TestCase(0, 16.5f, 34f)] [TestCase(1, 16.5f, 34f)]
        [TestCase(0, 10f, 13.84f)] [TestCase(1, 10f, 13.84f)]
        [TestCase(0, 16.5f, 54.16f)] [TestCase(1, 16.5f, 54.16f)]
        [TestCase(0, 0f, 34f)] [TestCase(1, 0f, 34f)]
        public void InclusiveAreaFacesAndCorners_AreTimed(int team, float depth, float y)
        {
            MatchEngine engine = Holding(team);
            Place(engine, team * 11, depth, y);
            // This represents a lawful claim at the boundary, rather than an outward carry.
            BallState ball = engine.TestOnly_BallSnapshot;
            ball.Position = new Vector3(team == 0 ? depth : 105f - depth, y, BallPhysicsConstants.Ball.RADIUS);
            engine.TestOnly_SetBall(in ball);
            engine.TestOnly_SetPhysicsFrame(Claim + 480);
            engine.RunTick();
            AssertCorner(engine, team, y);
        }

        [TestCase(0, GoalkeeperDeliveryVariant.Roll)] [TestCase(1, GoalkeeperDeliveryVariant.Roll)]
        [TestCase(0, GoalkeeperDeliveryVariant.Throw)] [TestCase(1, GoalkeeperDeliveryVariant.Throw)]
        [TestCase(0, GoalkeeperDeliveryVariant.Kick)] [TestCase(1, GoalkeeperDeliveryVariant.Kick)]
        public void AtomicContactAt480_IsLegalAndClearsClock(int team, GoalkeeperDeliveryVariant delivery)
        {
            MatchEngine engine = Holding(team);
            StartDefensiveRequest(engine, team, Claim + 479, delivery);
            engine.TestOnly_SetPhysicsFrame(Claim + 478);
            engine.RunTick(); // Decrement at 479, CONTACT next frame.
            Assert.AreEqual((long)Claim, engine.TestOnly_GoalkeeperState.HandControlStartFrame[team]);
            int contactFrame = -1;
            engine.TestOnly_W8BTerminalFeedbackObserver = (id, feedback, missing) => contactFrame = feedback.ContactFrame;
            engine.RunTick();
            Assert.AreEqual(Claim + 480, contactFrame);
            Assert.AreEqual(1, Count<DistributionExecutedEvent>(engine));
            Assert.AreEqual(1, Count<PassAttemptEvent>(engine));
            Assert.AreEqual(0, Count<RestartAwardedEvent>(engine));
            Assert.AreEqual(-1L, engine.TestOnly_GoalkeeperState.HandControlStartFrame[team]);
        }

        [TestCase(0)] [TestCase(1)]
        public void AcceptedWindupAt481_CancelsOnceAndDrainsNextResolve(int team)
        {
            MatchEngine engine = Holding(team, allowProducer: true);
            engine.TestOnly_SetPhysicsFrame(1212);
            engine.TestOnly_RunGoalkeeperDistributionProducer();
            Assert.AreEqual(GoalkeeperState.Distributing, engine.TestOnly_GkState(team));
            int terminals = 0;
            engine.TestOnly_W8BTerminalFeedbackObserver = (id, feedback, missing) =>
            {
                Assert.AreEqual(GoalkeeperDistributionFeedbackKind.Cancelled, feedback.Kind);
                terminals++;
            };
            engine.TestOnly_SetPhysicsFrame(Claim + 480);
            engine.RunTick();
            AssertCorner(engine, team, 34f);
            Assert.AreEqual(0, terminals);
            Assert.IsTrue(engine.TestOnly_PassExecutorState(team * 11).GoalkeeperFeedbackPending);
            engine = RoundTrip(engine); // Sentinel clock and terminal-pending is a valid body.
            engine.TestOnly_W8BTerminalFeedbackObserver = (id, feedback, missing) => terminals++;
            engine.RunTick();
            Assert.AreEqual(1, terminals);
            Assert.IsFalse(engine.TestOnly_PassExecutorState(team * 11).GoalkeeperFeedbackPending);
            Assert.AreEqual(GoalkeeperState.Recovering, engine.TestOnly_GkState(team));
            Assert.AreEqual(0, Count<DistributionExecutedEvent>(engine));
        }

        [TestCase(0)] [TestCase(1)]
        public void HandsDoNotAccumulateFeetGuard_AndFeetNeverConcedeCorner(int team)
        {
            MatchEngine engine = Holding(team);
            for (int i = 0; i < 300; i++) engine.TestOnly_RunGoalkeeperReleaseRule();
            Assert.AreEqual(0, engine.TestOnly_GkHoldTicks);
            GoalkeeperTickState state = engine.TestOnly_GoalkeeperState;
            state.States[team] = GoalkeeperState.Set;
            state.HandControlStartFrame[team] = -1;
            engine.TestOnly_RestoreGoalkeeperState(in state);
            for (int i = 0; i < 359; i++) engine.TestOnly_RunGoalkeeperReleaseRule();
            Assert.AreEqual(team * 11, engine.PossessingAgentId);
            engine.TestOnly_RunGoalkeeperReleaseRule();
            Assert.AreEqual(-1, engine.PossessingAgentId);
            Assert.AreEqual(0, Count<RestartAwardedEvent>(engine));
            engine.TestOnly_SetPossession(team * 11);
            engine.TestOnly_SuppressFeetGuard = true;
            engine.TestOnly_SetPhysicsFrame(Claim + 1000);
            engine.RunTick();
            Assert.AreEqual(0, Count<RestartAwardedEvent>(engine));
        }

        [TestCase(0, 0)] [TestCase(1, 0)]
        [TestCase(0, 479)] [TestCase(1, 479)]
        [TestCase(0, 480)] [TestCase(1, 480)]
        public void RestoreBeforeDeadline_HasIdenticalCornerContinuation(int team, int elapsed)
        {
            MatchEngine engine = Holding(team);
            engine.TestOnly_SetPhysicsFrame(Claim + elapsed - 1);
            engine.RunTick();
            SnapshotHeader header = engine.CaptureDurableHeader();
            SnapshotPayload payload = engine.CaptureDurablePayload();
            var digests = new byte[483 - elapsed][];
            var cues = new RestartCue[digests.Length];
            for (int i = 0; i < digests.Length; i++)
            {
                engine.RunTick(); digests[i] = engine.CurrentSnapshotDigest;
                cues[i] = engine.RestartAppliedThisTick;
            }
            MatchEngine restored = MatchEngine.RestoreFromSnapshot(header, payload, engine.MatchSeed);
            for (int i = 0; i < digests.Length; i++)
            {
                restored.RunTick();
                Assert.AreEqual(cues[i], restored.RestartAppliedThisTick);
                CollectionAssert.AreEqual(digests[i], restored.CurrentSnapshotDigest);
            }
        }

        [TestCase(0, -1)] [TestCase(1, -1)]
        [TestCase(0, 1)] [TestCase(1, 1)]
        [TestCase(0, -481)] [TestCase(1, -481)]
        [TestCase(0, 2)] [TestCase(1, 2)]
        [TestCase(0, 3)] [TestCase(1, 3)]
        [TestCase(0, 4)] [TestCase(1, 4)]
        public void CorruptClockOrOwnership_IsRejectedByBodyReader(int team, int corruption)
        {
            MatchEngine engine = Holding(team);
            engine.RunTick();
            GoalkeeperTickState state = engine.TestOnly_GoalkeeperState;
            if (corruption == -1) state.HandControlStartFrame[team] = -1;
            if (corruption == 1) state.HandControlStartFrame[team] = (long)engine.CurrentTick + 1;
            if (corruption == -481) state.HandControlStartFrame[team] = (long)engine.CurrentTick - 481;
            if (corruption == 2) state.States[team] = GoalkeeperState.Set;
            engine.TestOnly_RestoreGoalkeeperState(in state);
            if (corruption == 3)
            {
                engine.TestOnly_SetPossession(team * 11 + 1);
                engine.RunTick();
                state.States[team] = GoalkeeperState.HandsOnBall;
                state.HandControlStartFrame[team] = Claim;
                engine.TestOnly_RestoreGoalkeeperState(in state);
            }
            if (corruption == 4)
            {
                BallState ball = engine.TestOnly_BallSnapshot; ball.State = BallStateType.Stationary;
                engine.TestOnly_SetBall(in ball);
            }
            Assert.Throws<InvalidOperationException>(() => RoundTrip(engine));
        }

        [TestCase(0, false)] [TestCase(1, false)]
        [TestCase(0, true)] [TestCase(1, true)]
        public void FullTime_FreezesHandEpisodeAndWindupAcrossRestore(int team, bool windup)
        {
            MatchEngine engine = Holding(team);
            engine.TestOnly_SetPhysicsFrame((int)MatchEngineConstants.HALF_TIME_BOUNDARY_TICK - 1);
            engine.RunTick(); // Complete the actual half-time restart before staging the final hold.
            int start = (int)MatchEngineConstants.MATCH_TICKS_TOTAL - 481;
            engine.TestOnly_SetPhysicsFrame(start);
            engine.TestOnly_SetPossession(team * 11);
            GoalkeeperTickState state = engine.TestOnly_GoalkeeperState;
            state.States[team] = GoalkeeperState.HandsOnBall;
            state.HandControlStartFrame[team] = start;
            state.ClaimTick[team] = start / 6;
            state.ReleaseTickEarliest[team] = windup ? start / 6 + 1 : int.MaxValue;
            engine.TestOnly_RestoreGoalkeeperState(in state);
            engine.RunTick(); // Commit the final hold's possessor to MatchContext before the freeze.
            if (windup)
            {
                engine.TestOnly_SetPhysicsFrame((start / 6 + 35) * 6);
                engine.TestOnly_RunGoalkeeperDistributionProducer();
                Assert.AreEqual(GoalkeeperState.Distributing, engine.TestOnly_GkState(team));
            }
            engine.TestOnly_SetPhysicsFrame((int)MatchEngineConstants.MATCH_TICKS_TOTAL - 1);
            engine.RunTick();
            Assert.AreEqual(0, Count<RestartAwardedEvent>(engine));
            Assert.AreEqual((long)start, engine.TestOnly_GoalkeeperState.HandControlStartFrame[team]);
            SnapshotHeader header = engine.CaptureDurableHeader();
            SnapshotPayload payload = engine.CaptureDurablePayload();
            var digests = new byte[3][];
            for (int i = 0; i < digests.Length; i++)
            {
                engine.RunTick(); digests[i] = engine.CurrentSnapshotDigest;
            }
            MatchEngine restored = MatchEngine.RestoreFromSnapshot(header, payload, engine.MatchSeed);
            for (int i = 0; i < digests.Length; i++)
            {
                restored.RunTick();
                CollectionAssert.AreEqual(digests[i], restored.CurrentSnapshotDigest);
                Assert.AreEqual(0, Count<DistributionExecutedEvent>(restored));
                Assert.AreEqual(0, Count<RestartAwardedEvent>(restored));
            }
            RoundTrip(restored); // A body several ticks past full time is also valid.
            state = engine.TestOnly_GoalkeeperState; state.HandControlStartFrame[team] = -1;
            engine.TestOnly_RestoreGoalkeeperState(in state);
            Assert.Throws<InvalidOperationException>(() => RoundTrip(engine), "full time retains structural checks");
            state.HandControlStartFrame[team] = (long)engine.CurrentTick + 1;
            engine.TestOnly_RestoreGoalkeeperState(in state);
            Assert.Throws<InvalidOperationException>(() => RoundTrip(engine), "full time rejects a future clock");
            if (windup)
            {
                state.States[team] = GoalkeeperState.Set;
                state.HandControlStartFrame[team] = -1;
                engine.TestOnly_RestoreGoalkeeperState(in state);
                Assert.Throws<InvalidOperationException>(() => RoundTrip(engine),
                    "full time retains the in-flight request's active-clock requirement");
            }
        }

        [TestCase(0, -0.30f, false)] [TestCase(1, -0.30f, false)]
        [TestCase(0, -0.05f, true)] [TestCase(1, -0.05f, true)]
        public void CompletedBoundaryCrossing_PrecedesHandCorner(int team, float depth, bool corner)
        {
            MatchEngine engine = Holding(team);
            BallState ball = engine.TestOnly_BallSnapshot;
            ball.Position = new Vector3(team == 0 ? depth : 105f - depth, 34f, BallPhysicsConstants.Ball.RADIUS);
            engine.TestOnly_SetBall(in ball);
            Assert.AreEqual(corner, engine.TestOnly_IsKeeperHandControlOffenceEligible(team * 11));
            engine.TestOnly_PostReattachmentBall = ball;
            engine.TestOnly_SetPhysicsFrame(Claim + 480);
            engine.RunTick();
            Assert.AreEqual(corner ? RestartCue.Corner : RestartCue.KickOff, engine.RestartAppliedThisTick);
            Assert.AreEqual(corner ? 1 : 0, engine.TestOnly_KeeperWhistleFinishCount);
        }

        [TestCase(0, 480)] [TestCase(1, 480)]
        [TestCase(0, 481)] [TestCase(1, 481)]
        public void OutsideHandling_HasPriorityAndUsesSameWhistleFinish(int team, int elapsed)
        {
            MatchEngine engine = Holding(team);
            Vector3 contact = new Vector3(team == 0 ? 20f : 85f, 34f, BallPhysicsConstants.Ball.RADIUS);
            Assert.IsFalse(engine.TestOnly_TryKeeperHandContact(team * 11, contact));
            engine.TestOnly_SetPhysicsFrame(Claim + elapsed - 1);
            engine.RunTick();
            Assert.AreEqual(RestartCue.FreeKick, engine.RestartAppliedThisTick);
            Assert.AreEqual(1, engine.TestOnly_KeeperWhistleFinishCount);
            int index = Index<RestartAwardedEvent>(engine);
            Assert.AreEqual(RestartAwardedEvent.DIRECT_FREE_KICK,
                engine.TickLedgerRecord<RestartAwardedEvent>(index).RestartKind);
        }

        [TestCase(0)] [TestCase(1)]
        public void Corner_DiscardsSameFrameFoulWithoutCardDraw(int team)
        {
            MatchEngine engine = Holding(team);
            engine.TestOnly_SetPhysicsFrame(Claim + 481);
            engine.TestOnly_InjectFoulCandidate(team * 11 + 1, (1 - team) * 11 + 1);
            ulong draws = engine.TestOnly_CardSeverityCursor;
            EventBus.BeginPhase(PhaseId.Resolve);
            engine.TestOnly_RunResolvePhase();
            Assert.AreEqual(draws, engine.TestOnly_CardSeverityCursor);
            Assert.AreEqual(0f, engine.TestOnly_FoulCandidateForceN);
            Assert.AreEqual(1, engine.TestOnly_KeeperWhistleFinishCount);
            Assert.AreEqual(0, Count<CardIssuedEvent>(engine));
            Assert.AreEqual(0, Count<FoulCommittedEvent>(engine));
        }

        [TestCase(0, false)] [TestCase(1, false)]
        [TestCase(0, true)] [TestCase(1, true)]
        public void OwnPolicyBudget_AdmitsContactAt480_Refuses481(int team, bool tooLate)
        {
            MatchEngine engine = Holding(team, allowProducer: true);
            const int commit = 1398;
            int windup = Mathf.CeilToInt(GoalkeeperDistribution.ComputeWindupMs(DeliveryKind.Roll) * 60f / 1000f);
            int start = commit + windup - 480 - (tooLate ? 1 : 0);
            GoalkeeperTickState state = engine.TestOnly_GoalkeeperState;
            state.HandControlStartFrame[team] = start;
            state.ClaimTick[team] = start / 6;
            state.ReleaseTickEarliest[team] = start / 6 + 1;
            engine.TestOnly_RestoreGoalkeeperState(in state);
            engine.TestOnly_SetPhysicsFrame(commit - 1);
            int contact = -1;
            engine.TestOnly_W8BTerminalFeedbackObserver = (id, feedback, missing) => contact = feedback.ContactFrame;
            engine.RunTick();
            Assert.AreEqual(tooLate ? GoalkeeperState.HandsOnBall : GoalkeeperState.Distributing,
                engine.TestOnly_GkState(team));
            for (int i = 0; i < windup; i++) engine.RunTick();
            if (tooLate) AssertCorner(engine, team, engine.AgentView(team * 11).Position.y);
            else
            {
                Assert.AreEqual(commit + windup, contact);
                Assert.AreEqual(1, Count<DistributionExecutedEvent>(engine));
            }
        }

        [TestCase(82, true)] [TestCase(83, false)] [TestCase(84, false)]
        public void MaximumWindupRetry_UsesExactAdmissionPredicate(int windup, bool fits)
        {
            Assert.AreEqual(fits, MatchEngine.TestOnly_DistributionContactFits(1398, 1000L, windup));
            Assert.IsTrue(MatchEngine.TestOnly_DistributionContactFits(1300, 1000L, 84),
                "a retained cancellation near 294 frames can retry on the next heartbeat");
        }

        [TestCase(0)] [TestCase(1)]
        public void InFlightRestore_RejectsRemainingWindupBeyond480(int team)
        {
            MatchEngine engine = Holding(team, allowProducer: true);
            engine.TestOnly_SetPhysicsFrame(1212);
            engine.TestOnly_RunGoalkeeperDistributionProducer();
            engine.TestOnly_SetPhysicsFrame(Claim + 479);
            engine.RunTick(); // Still legal hand time, but the skipped executor frames corrupt its remaining budget.
            Assert.AreEqual(GoalkeeperState.Distributing, engine.TestOnly_GkState(team));
            Assert.Throws<InvalidOperationException>(() => RoundTrip(engine));
        }

        [TestCase(0)] [TestCase(1)]
        public void ResetSlot_ClearsClock_WhileOrdinaryRefreshPreservesIt(int team)
        {
            MatchEngine engine = Holding(team);
            engine.RunTick();
            Assert.AreEqual((long)Claim, engine.TestOnly_GoalkeeperState.HandControlStartFrame[team]);
            engine.TestOnly_ResetGoalkeeperSlot(team);
            Assert.AreEqual(-1L, engine.TestOnly_GoalkeeperState.HandControlStartFrame[team]);
            engine.TestOnly_SetPhysicsFrame(Claim + 480);
            engine.RunTick();
            Assert.AreEqual(0, Count<RestartAwardedEvent>(engine));
            Assert.AreEqual(0, Count<DistributionExecutedEvent>(engine));
        }

        [TestCase(0, false)] [TestCase(1, false)]
        [TestCase(0, true)] [TestCase(1, true)]
        public void EveryEligibilityPredicate_IsRequiredInBothHandStates(int team, bool distributing)
        {
            for (int missing = 0; missing <= 5; missing++)
            {
                MatchEngine engine = Holding(team);
                int keeper = team * 11;
                GoalkeeperState hand = distributing ? GoalkeeperState.Distributing : GoalkeeperState.HandsOnBall;
                GoalkeeperTickState state = engine.TestOnly_GoalkeeperState;
                state.States[team] = hand;
                engine.TestOnly_RestoreGoalkeeperState(in state);
                if (missing == 1) state.States[team] = GoalkeeperState.Set;
                if (missing == 2)
                {
                    engine.TestOnly_SetPossession(keeper + 1);
                    state.States[team] = hand;
                    state.HandControlStartFrame[team] = Claim;
                }
                if (missing == 3) state.HandControlStartFrame[team] = -1;
                engine.TestOnly_RestoreGoalkeeperState(in state);
                if (missing == 4)
                    Assert.IsFalse(engine.TestOnly_TryKeeperHandContact(keeper,
                        new Vector3(team == 0 ? 20f : 85f, 34f, 0.11f)));
                if (missing == 5)
                {
                    BallState ball = engine.TestOnly_BallSnapshot; ball.State = BallStateType.Stationary;
                    engine.TestOnly_SetBall(in ball);
                }
                Assert.AreEqual(missing == 0, engine.TestOnly_IsKeeperHandControlOffenceEligible(keeper),
                    "state/holder/episode/handling/Controlled gate " + missing);
            }
        }

        [TestCase(0, false)] [TestCase(1, false)]
        [TestCase(0, true)] [TestCase(1, true)]
        public void LegalCatchInside_WithBodyOutside_IsBoundedByCornerAcrossRestore(int team, bool restore)
        {
            MatchEngine engine = Holding(team);
            Vector3 contact = new Vector3(team == 0 ? 16f : 89f, 34f, BallPhysicsConstants.Ball.RADIUS);
            Assert.IsTrue(engine.TestOnly_TryKeeperHandContact(team * 11, contact));
            Place(engine, team * 11, 17.2f, 34f);
            engine.TestOnly_SetPossession(team * 11); // The lawful acquisition snap is not an outward carry.
            engine.TestOnly_SetPhysicsFrame(Claim + 479);
            engine.RunTick();
            Assert.AreEqual(RestartCue.None, engine.RestartAppliedThisTick);
            if (restore) engine = RoundTrip(engine);
            engine.RunTick();
            AssertCorner(engine, team, 34f);
        }

        private static MatchEngine Holding(int team, bool allowProducer = false)
        {
            MatchEngine engine = W8BDistributionTests.Fixture(GkDistributionPolicy.SlowDown, team, false);
            GoalkeeperTickState state = engine.TestOnly_GoalkeeperState;
            if (!allowProducer) state.ReleaseTickEarliest[team] = int.MaxValue;
            engine.TestOnly_RestoreGoalkeeperState(in state);
            return engine;
        }

        private static void StartDefensiveRequest(MatchEngine engine, int team, int frame, GoalkeeperDeliveryVariant delivery)
        {
            var request = new GoalkeeperDistributionRequest
            {
                AgentId = team * 11, TeamId = team, Delivery = delivery, TargetAgentId = -1,
                TargetPosition = new Vector3(team == 0 ? 35f : 70f, 34f, 0f), EmittedPower01 = 0.5f,
                ReleaseHeightM = 0.2f, WindupFrames = 1, FrameNumber = frame
            };
            Assert.IsTrue(engine.TestOnly_StartGoalkeeperDistribution(in request, out _));
            GoalkeeperTickState state = engine.TestOnly_GoalkeeperState;
            state.States[team] = GoalkeeperState.Distributing; state.DistributionPending[team] = true;
            engine.TestOnly_RestoreGoalkeeperState(in state);
        }

        private static MatchEngine RoundTrip(MatchEngine engine)
        {
            SnapshotHeader header = engine.CaptureDurableHeader();
            SnapshotPayload payload = engine.CaptureDurablePayload();
            SnapshotPayload overlay = engine.CapturePostSnapshotWorldState();
            MatchEngine restored = MatchEngine.RestoreFromSnapshot(header, payload, engine.MatchSeed);
            restored.ApplyPostSnapshotWorldState(overlay, null);
            return restored;
        }

        private static void Place(MatchEngine engine, int keeper, float depth, float y)
        {
            AgentState agent = engine.TestOnly_AgentSnapshot(keeper);
            agent.Position = new Vector2(keeper == 0 ? depth : 105f - depth, y);
            agent.Velocity = Vector2.zero; agent.Speed = 0f; agent.LastValidPosition = agent.Position;
            engine.TestOnly_SetAgent(keeper, in agent);
            MovementCommand stop = MovementCommand.Stop(agent.Position);
            engine.TestOnly_SetCommand(keeper, in stop);
        }

        private static void AssertCorner(MatchEngine engine, int team, float y)
        {
            Assert.AreEqual(RestartCue.Corner, engine.RestartAppliedThisTick);
            Assert.AreEqual(1 - team, engine.RestartAwardedTeam);
            Assert.AreEqual(1, Count<RestartAwardedEvent>(engine));
            RestartAwardedEvent evt = engine.TickLedgerRecord<RestartAwardedEvent>(Index<RestartAwardedEvent>(engine));
            Assert.AreEqual((byte)RestartType.Corner, evt.RestartKind);
            Assert.AreEqual(1 - team, evt.AwardedTeam);
            float r = BallPhysicsConstants.Ball.RADIUS;
            Assert.AreEqual(team == 0 ? r : 105f - r, evt.Location.x);
            Assert.AreEqual(y < 34f ? r : 68f - r, evt.Location.y);
            Assert.AreEqual(1 - team, engine.AgentTeamId(engine.PossessingAgentId));
        }

        private static int Index<T>(MatchEngine engine) where T : struct
        {
            byte ordinal = EventRegistry.GetOrdinal<T>();
            for (int i = 0; i < engine.TickLedgerCount; i++)
                if (engine.TickLedgerOrdinal(i) == ordinal) return i;
            return -1;
        }

        private static int Count<T>(MatchEngine engine) where T : struct
        {
            byte ordinal = EventRegistry.GetOrdinal<T>(); int count = 0;
            for (int i = 0; i < engine.TickLedgerCount; i++)
                if (engine.TickLedgerOrdinal(i) == ordinal) count++;
            return count;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0 | 2026-11-06 | — | Mirrored W8 C exact-boundary, precedence, CONTACT, feet and v28 restore coverage. |
#endregion
