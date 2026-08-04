// File:     src/match-engine/tests/GkRushTriggerTests.cs
// Created:  2026-08-04
// Modified: 2026-08-04
// Author:   —
// Spec:     Keeper rush trigger design supplement (docs/tracking/gk-rush-trigger-design.md) §2;
//           Goalkeeper Mechanics #11 §3.7 / §3.7.0 (ERR-011-010) / KD-15; Testing Strategy #19;
//           Code Standards #20
// Purpose:  Locks for wiring backlog W1 — the trigger that gave GoalkeeperMechanics.CommitRushIntent
//           its first production caller. Three layers: the pure §4.4 geometry (RushArmed), the
//           goal-side cover test that decides whether anyone is already in the shot's path, and the
//           composed predicate → commit → Rushing chain driven through a real MatchEngine.
//
//           The behaviour these pin: a keeper comes out to REDUCE THE SHOOTING ANGLE. A defender
//           chasing the carrier down does not reduce it, so the keeper comes anyway — only a
//           team-mate already GOAL-SIDE of the ball, in the shot corridor, makes the trip
//           unnecessary. The "when" is the keeper's own attributes (§3.7.0), not a fixed range.
//
//           Every geometry case is mirrored home and away. Three home/away asymmetry defects have
//           shipped in this project because every spec example and every fixture used the home team
//           (#8 ERR-008-002), and this predicate branches on the defended goal line.

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.GoalkeeperMechanics;

namespace TacticalDirector.MatchEngine
{
    /// <summary>W1 rush-trigger locks — pure geometry, the cover test, then the composed chain.</summary>
    [TestFixture]
    public sealed class GkRushTriggerTests
    {
        private const float PitchY = 34f;

        /// <summary>A rush speed comfortably above any ball speed used below, so a refusal in these
        /// fixtures is always the predicate's decision and never an accidental lost race.</summary>
        private const float RushSpeed = 5.5f;

        /// <summary>A generous §3.7.0 commit distance, so each geometry case is decided by the condition
        /// it is named for rather than by the distance gate. The gate has its own case below, and the
        /// attribute formula that produces the number is locked in <c>GoalkeeperRushTests</c>.</summary>
        private const float CommitDistance = 20f;

        private static float GoalX(int keeperTeam) =>
            keeperTeam == 0 ? 0f : MatchEngineConstants.PITCH_LENGTH_M;

        /// <summary>A point <paramref name="metres"/> in front of the goal this keeper defends.</summary>
        private static float OutFromGoal(int keeperTeam, float metres) =>
            keeperTeam == 0 ? metres : MatchEngineConstants.PITCH_LENGTH_M - metres;

        // ── §4.4 RushArmed: the cases a keeper SHOULD come for ───────────────────────────

        [TestCase(0)]
        [TestCase(1)]
        public void RushArmed_LooseBallInFrontOfGoalWithNoCover_Arms(int keeperTeam)
        {
            bool armed = GkHeadingIntentSource.RushArmed(
                keeperTeam,
                new Vector3(OutFromGoal(keeperTeam, 4f), PitchY, 0f),
                new Vector3(OutFromGoal(keeperTeam, 12f), PitchY, 0.11f),
                Vector3.zero,
                ballLoose: true,
                ballHeldByKeeperTeam: false,
                hasGoalSideCover: false,
                rushCommitDistanceM: CommitDistance,
                rushSpeedMps: RushSpeed,
                out Vector3 target);

            Assert.IsTrue(armed, "a loose ball in front of goal with nobody goal-side is the sweeper case");
            Assert.AreEqual(OutFromGoal(keeperTeam, 12f), target.x, 0.01f,
                "a stationary ball's meeting point is the ball itself");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RushArmed_UnopposedCarrier_Arms(int keeperTeam)
        {
            bool armed = GkHeadingIntentSource.RushArmed(
                keeperTeam,
                new Vector3(OutFromGoal(keeperTeam, 4f), PitchY, 0f),
                new Vector3(OutFromGoal(keeperTeam, 12f), PitchY, 0.11f),
                Vector3.zero,
                ballLoose: false,
                ballHeldByKeeperTeam: false,
                hasGoalSideCover: false,
                rushCommitDistanceM: CommitDistance,
                rushSpeedMps: RushSpeed,
                out Vector3 target);

            Assert.IsTrue(armed, "an opponent dribbling at goal with nobody goal-side is the 1v1");
            Assert.AreEqual(OutFromGoal(keeperTeam, 12f), target.x, 0.01f,
                "against a carrier the target is the ball — closing the angle is the whole point");
        }

        /// <summary>The case that produced this version of the trigger. A defender chasing the carrier —
        /// or wrestling him for the ball — narrows no shooting angle, so the keeper still comes.
        /// <c>hasGoalSideCover</c> is false for exactly that geometry (see the cover locks below), and
        /// the predicate must arm on it.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void RushArmed_CarrierWithADefenderGivingChase_StillArms(int keeperTeam)
        {
            bool armed = GkHeadingIntentSource.RushArmed(
                keeperTeam,
                new Vector3(OutFromGoal(keeperTeam, 4f), PitchY, 0f),
                new Vector3(OutFromGoal(keeperTeam, 14f), PitchY, 0.11f),
                Vector3.zero,
                ballLoose: false,
                ballHeldByKeeperTeam: false,
                // A chaser is behind the ball, so he is not goal-side and is not cover.
                hasGoalSideCover: false,
                rushCommitDistanceM: CommitDistance,
                rushSpeedMps: RushSpeed,
                out _);

            Assert.IsTrue(armed,
                "a keeper comes out to reduce the shooting angle; a chasing defender does not reduce it");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RushArmed_MovingLooseBall_LocksTheMeetingPointNotTheBall(int keeperTeam)
        {
            // The ball rolls across the face of goal at 2 m/s — slow enough that a 5.5 m/s keeper wins
            // the race, which a perpendicular 6 m/s ball at this separation correctly would not be.
            // KD-15 locks the target at commit, so aiming at where the ball IS sends him to where it was.
            bool armed = GkHeadingIntentSource.RushArmed(
                keeperTeam,
                new Vector3(OutFromGoal(keeperTeam, 4f), PitchY, 0f),
                new Vector3(OutFromGoal(keeperTeam, 10f), PitchY, 0.11f),
                new Vector3(0f, 2f, 0f),
                ballLoose: true,
                ballHeldByKeeperTeam: false,
                hasGoalSideCover: false,
                rushCommitDistanceM: CommitDistance,
                rushSpeedMps: RushSpeed,
                out Vector3 target);

            Assert.IsTrue(armed);
            Assert.Greater(target.y, PitchY,
                "the locked target must lead the ball along its travel, not sit on its current position");
        }

        // ── §4.4 RushArmed: the cases a keeper should NOT come for ───────────────────────

        [TestCase(0)]
        [TestCase(1)]
        public void RushArmed_TeammateGoalSideOfTheBall_DoesNotArm(int keeperTeam)
        {
            bool armed = GkHeadingIntentSource.RushArmed(
                keeperTeam,
                new Vector3(OutFromGoal(keeperTeam, 4f), PitchY, 0f),
                new Vector3(OutFromGoal(keeperTeam, 12f), PitchY, 0.11f),
                Vector3.zero,
                ballLoose: false,
                ballHeldByKeeperTeam: false,
                hasGoalSideCover: true,
                rushCommitDistanceM: CommitDistance,
                rushSpeedMps: RushSpeed,
                out _);

            Assert.IsFalse(armed,
                "a team-mate already in the shot's path is narrowing the angle the keeper would come " +
                "out to narrow, and two bodies converging on one line is how a keeper gets rounded");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RushArmed_OwnTeamHasTheBall_DoesNotArm(int keeperTeam)
        {
            bool armed = GkHeadingIntentSource.RushArmed(
                keeperTeam,
                new Vector3(OutFromGoal(keeperTeam, 4f), PitchY, 0f),
                new Vector3(OutFromGoal(keeperTeam, 12f), PitchY, 0.11f),
                Vector3.zero,
                ballLoose: false,
                ballHeldByKeeperTeam: true,
                hasGoalSideCover: false,
                rushCommitDistanceM: CommitDistance,
                rushSpeedMps: RushSpeed,
                out _);

            Assert.IsFalse(armed, "there is nothing to come for when a team-mate has the ball");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RushArmed_BallBeyondThisKeepersCommitDistance_DoesNotArm(int keeperTeam)
        {
            float far = CommitDistance + 10f;

            bool armed = GkHeadingIntentSource.RushArmed(
                keeperTeam,
                new Vector3(OutFromGoal(keeperTeam, far - 2f), PitchY, 0f),
                new Vector3(OutFromGoal(keeperTeam, far), PitchY, 0.11f),
                Vector3.zero,
                ballLoose: true,
                ballHeldByKeeperTeam: false,
                hasGoalSideCover: false,
                rushCommitDistanceM: CommitDistance,
                rushSpeedMps: RushSpeed,
                out _);

            Assert.IsFalse(armed, "beyond the distance THIS keeper comes out to, he stays");
        }

        /// <summary>The §3.7.0 distance is per-keeper, so identical geometry must arm for a keeper who
        /// comes out far and refuse for one who does not. This is the attribute model reaching the
        /// trigger; the formula behind the number is locked in <c>GoalkeeperRushTests</c>.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void RushArmed_SameBall_ArmsForABoldKeeperAndNotATimidOne(int keeperTeam)
        {
            Vector3 gk = new Vector3(OutFromGoal(keeperTeam, 4f), PitchY, 0f);
            Vector3 ball = new Vector3(OutFromGoal(keeperTeam, 12f), PitchY, 0.11f);

            bool bold = GkHeadingIntentSource.RushArmed(
                keeperTeam, in gk, in ball, Vector3.zero,
                ballLoose: true, ballHeldByKeeperTeam: false, hasGoalSideCover: false,
                rushCommitDistanceM: 18f, rushSpeedMps: RushSpeed, out _);

            bool timid = GkHeadingIntentSource.RushArmed(
                keeperTeam, in gk, in ball, Vector3.zero,
                ballLoose: true, ballHeldByKeeperTeam: false, hasGoalSideCover: false,
                rushCommitDistanceM: 8f, rushSpeedMps: RushSpeed, out _);

            Assert.IsTrue(bold, "a keeper who comes out 18 m takes a ball 12 m from his goal");
            Assert.IsFalse(timid, "a keeper who comes out 8 m does not");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RushArmed_HighBall_DoesNotArm(int keeperTeam)
        {
            float high = MatchEngineConstants.GkRushMaxBallHeightM + 1f;

            bool armed = GkHeadingIntentSource.RushArmed(
                keeperTeam,
                new Vector3(OutFromGoal(keeperTeam, 4f), PitchY, 0f),
                new Vector3(OutFromGoal(keeperTeam, 12f), PitchY, high),
                Vector3.zero,
                ballLoose: true,
                ballHeldByKeeperTeam: false,
                hasGoalSideCover: false,
                rushCommitDistanceM: CommitDistance,
                rushSpeedMps: RushSpeed,
                out _);

            Assert.IsFalse(armed, "that ball is a cross to be claimed (backlog W3), not one to be swept");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RushArmed_ClearanceOutrunningTheKeeper_DoesNotArm(int keeperTeam)
        {
            // A cleared ball leaving at 25 m/s cannot be caught by a keeper running at 5.5 m/s, so the
            // intercept quadratic has no non-negative root. The solve IS the guard: without it the
            // keeper would set off after every clearance and never come back.
            float away = keeperTeam == 0 ? +25f : -25f;

            bool armed = GkHeadingIntentSource.RushArmed(
                keeperTeam,
                new Vector3(OutFromGoal(keeperTeam, 4f), PitchY, 0f),
                new Vector3(OutFromGoal(keeperTeam, 10f), PitchY, 0.11f),
                new Vector3(away, 0f, 0f),
                ballLoose: true,
                ballHeldByKeeperTeam: false,
                hasGoalSideCover: false,
                rushCommitDistanceM: CommitDistance,
                rushSpeedMps: RushSpeed,
                out _);

            Assert.IsFalse(armed, "a ball outrunning the keeper is not a race he can win");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RushArmed_RunLongerThanTheTimeBudget_DoesNotArm(int keeperTeam)
        {
            // 21 m at 5.5 m/s is 3.8 s against a 2.0 s budget — refused even though the ball is inside
            // this keeper's (deliberately generous) commit distance.
            bool armed = GkHeadingIntentSource.RushArmed(
                keeperTeam,
                new Vector3(GoalX(keeperTeam), PitchY, 0f),
                new Vector3(OutFromGoal(keeperTeam, 21f), PitchY, 0.11f),
                Vector3.zero,
                ballLoose: true,
                ballHeldByKeeperTeam: false,
                hasGoalSideCover: false,
                rushCommitDistanceM: 22f,
                rushSpeedMps: RushSpeed,
                out _);

            Assert.IsFalse(armed, "the keeper does not commit to a run longer than GkRushMaxInterceptS");
        }

        // ── §4.4 HasGoalSideCover ────────────────────────────────────────────────────────

        [TestCase(0)]
        [TestCase(1)]
        public void HasGoalSideCover_TeammateBetweenBallAndGoal_IsCover(int keeperTeam)
        {
            var agents = new[] { AgentAt(OutFromGoal(keeperTeam, 6f), PitchY) };

            bool cover = GkHeadingIntentSource.HasGoalSideCover(
                keeperTeam, new Vector3(OutFromGoal(keeperTeam, 14f), PitchY, 0.11f),
                agents, new[] { keeperTeam }, new[] { false }, new[] { false }, agents.Length);

            Assert.IsTrue(cover, "a team-mate in front of the ball, on the shot line, is cover");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void HasGoalSideCover_ChasingDefenderBehindTheBall_IsNotCover(int keeperTeam)
        {
            // Further from the defended goal than the ball is: he is chasing, not covering. This is the
            // case the first cut of the trigger got wrong.
            var agents = new[] { AgentAt(OutFromGoal(keeperTeam, 17f), PitchY) };

            bool cover = GkHeadingIntentSource.HasGoalSideCover(
                keeperTeam, new Vector3(OutFromGoal(keeperTeam, 14f), PitchY, 0.11f),
                agents, new[] { keeperTeam }, new[] { false }, new[] { false }, agents.Length);

            Assert.IsFalse(cover,
                "a defender giving chase narrows no angle — the keeper must still be free to come out");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void HasGoalSideCover_DefenderLevelWithTheCarrier_IsNotCover(int keeperTeam)
        {
            // Shoulder to shoulder, trying to muscle him off the ball: inside the goal-side margin, so
            // not cover — the carrier's sight of goal is unobstructed.
            float levelish = 14f - (MatchEngineConstants.GkRushCoverGoalSideMarginM * 0.5f);
            var agents = new[] { AgentAt(OutFromGoal(keeperTeam, levelish), PitchY) };

            bool cover = GkHeadingIntentSource.HasGoalSideCover(
                keeperTeam, new Vector3(OutFromGoal(keeperTeam, 14f), PitchY, 0.11f),
                agents, new[] { keeperTeam }, new[] { false }, new[] { false }, agents.Length);

            Assert.IsFalse(cover, "level with the carrier is not in front of him");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void HasGoalSideCover_GoalSideButOutsideTheCorridor_IsNotCover(int keeperTeam)
        {
            float wide = PitchY + MatchEngineConstants.GkRushCoverCorridorHalfWidthM + 5f;
            var agents = new[] { AgentAt(OutFromGoal(keeperTeam, 6f), wide) };

            bool cover = GkHeadingIntentSource.HasGoalSideCover(
                keeperTeam, new Vector3(OutFromGoal(keeperTeam, 14f), PitchY, 0.11f),
                agents, new[] { keeperTeam }, new[] { false }, new[] { false }, agents.Length);

            Assert.IsFalse(cover,
                "a full-back stranded wide is goal-side of a central ball and blocks nothing");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void HasGoalSideCover_OpponentsGoalkeepersAndSentOff_AreNotCover(int keeperTeam)
        {
            int other = 1 - keeperTeam;
            // Three bodies all perfectly placed to be cover, none of which counts: an opponent, this
            // team's own keeper, and a sent-off team-mate.
            var agents = new[]
            {
                AgentAt(OutFromGoal(keeperTeam, 6f), PitchY),
                AgentAt(OutFromGoal(keeperTeam, 6f), PitchY),
                AgentAt(OutFromGoal(keeperTeam, 6f), PitchY),
            };

            bool cover = GkHeadingIntentSource.HasGoalSideCover(
                keeperTeam, new Vector3(OutFromGoal(keeperTeam, 14f), PitchY, 0.11f),
                agents,
                new[] { other, keeperTeam, keeperTeam },
                new[] { false, true, false },
                new[] { false, false, true },
                agents.Length);

            Assert.IsFalse(cover,
                "an opponent, the keeper himself and a frozen sent-off agent are not cover");
        }

        // ── The intercept solve ──────────────────────────────────────────────────────────

        [Test]
        public void TrySolveRushIntercept_StationaryBall_MeetsItWhereItLies()
        {
            bool solved = GkHeadingIntentSource.TrySolveRushIntercept(
                new Vector3(0f, 0f, 0f), new Vector3(11f, 0f, 0.11f), Vector3.zero, 5.5f,
                out float t, out Vector3 meet);

            Assert.IsTrue(solved);
            Assert.AreEqual(2.0f, t, 0.01f, "11 m at 5.5 m/s is 2 s");
            Assert.AreEqual(11f, meet.x, 0.01f);
        }

        [Test]
        public void TrySolveRushIntercept_BallAtTheKeeper_SolvesAtZero()
        {
            bool solved = GkHeadingIntentSource.TrySolveRushIntercept(
                new Vector3(10f, 34f, 0f), new Vector3(10f, 34f, 0.11f), Vector3.zero, 5.5f,
                out float t, out _);

            Assert.IsTrue(solved);
            Assert.AreEqual(0f, t, 1e-4f, "already there — the earliest meeting time is now");
        }

        [Test]
        public void TrySolveRushIntercept_BallFasterAndReceding_HasNoSolution()
        {
            bool solved = GkHeadingIntentSource.TrySolveRushIntercept(
                new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0.11f), new Vector3(20f, 0f, 0f), 5.5f,
                out _, out _);

            Assert.IsFalse(solved, "a receding ball faster than the keeper is never met");
        }

        [Test]
        public void TrySolveRushIntercept_BallFasterButClosing_IsMet()
        {
            // A fast ball travelling TOWARD the keeper is caught even though he could never chase it —
            // the sign of the closing term, not the speed comparison, is what decides.
            bool solved = GkHeadingIntentSource.TrySolveRushIntercept(
                new Vector3(0f, 0f, 0f), new Vector3(20f, 0f, 0.11f), new Vector3(-20f, 0f, 0f), 5.5f,
                out float t, out Vector3 meet);

            Assert.IsTrue(solved);
            Assert.Greater(t, 0f);
            Assert.Less(meet.x, 20f, "they meet somewhere between them");
        }

        [Test]
        public void TrySolveRushIntercept_ZeroRushSpeed_HasNoSolution()
        {
            bool solved = GkHeadingIntentSource.TrySolveRushIntercept(
                new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0.11f), Vector3.zero, 0f, out _, out _);

            Assert.IsFalse(solved, "a keeper who cannot move reaches nothing");
        }

        // ── The composed chain: predicate → commit → Rushing ─────────────────────────────

        [TestCase(0)]
        [TestCase(1)]
        public void ComposedEngine_UnopposedLooseBallInTheBox_CommitsARushAndLaunchesIt(int keeperTeam)
        {
            var engine = new MatchEngine(0x0F1E2D3C4B5A6978UL);
            PlaceFixture(engine, keeperTeam, coverOutFromGoalM: -1f);

            Assert.AreEqual(0, engine.TestOnly_RushCommitCount,
                "no rush has ever been committed before the drive — the W1 baseline for every match " +
                "this engine has ever played");

            // Three tactical drives: Resting → Set (the commit happens here, from a state that has a
            // → Rushing row) → Anticipate → Rushing. The commit precedes the tactical tick inside the
            // drive, which is why the intent is seen the same stride it is made.
            engine.TestOnly_DriveGkHeadingTactical();
            engine.TestOnly_DriveGkHeadingTactical();
            engine.TestOnly_DriveGkHeadingTactical();

            Assert.AreEqual(1, engine.TestOnly_RushCommitCount,
                "the trigger must commit exactly once per episode (KD-15 locks the target at commit)");
            Assert.AreEqual(GoalkeeperState.Rushing, engine.TestOnly_GkState(keeperTeam),
                "a committed rush above RushCommitThreshold must launch");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void ComposedEngine_TeammateGoalSideOfTheBall_CommitsNothing(int keeperTeam)
        {
            var engine = new MatchEngine(0x0F1E2D3C4B5A6978UL);
            PlaceFixture(engine, keeperTeam, coverOutFromGoalM: 3f);

            engine.TestOnly_DriveGkHeadingTactical();
            engine.TestOnly_DriveGkHeadingTactical();
            engine.TestOnly_DriveGkHeadingTactical();

            Assert.AreEqual(0, engine.TestOnly_RushCommitCount,
                "a team-mate already in the shot's path keeps the keeper home");
            Assert.AreNotEqual(GoalkeeperState.Rushing, engine.TestOnly_GkState(keeperTeam));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void ComposedEngine_CommittedRush_MovesTheKeeperOffHisLineAndTerminates(int keeperTeam)
        {
            // W1 AR-1 (M): the two tests above stop at GkState == Rushing, which is true of an engine
            // where UpdateRushFrame's position write-back is discarded — the exact defect #11 v1.4 H-2
            // already fixed once. The claim W1 exists to make is that the keeper LEAVES HIS LINE, so
            // assert the displacement, not the state. Drives the 60 Hz physics half as well, because
            // the tactical drive alone only produces an intent.
            var engine = new MatchEngine(0x0F1E2D3C4B5A6978UL);
            PlaceFixture(engine, keeperTeam, coverOutFromGoalM: -1f);

            int keeper = KeeperAgentId(engine, keeperTeam);
            float startFromGoalM = Mathf.Abs(engine.AgentView(keeper).Position.x - GoalX(keeperTeam));

            // Three tactical drives reach Rushing (see the test above); then run the rush out at 60 Hz.
            // The run is 4 m and the SLOWEST possible rush (RushLaunchBaseMps at PaceNorm = 0) covers
            // 0.075 m/frame, so arrival takes 47 frames — 120 completes it at any pace the roster
            // produces, and frames after arrival are inert (Recovering has no physics row).
            engine.TestOnly_DriveGkHeadingTactical();
            engine.TestOnly_DriveGkHeadingTactical();
            engine.TestOnly_DriveGkHeadingTactical();
            Assert.AreEqual(GoalkeeperState.Rushing, engine.TestOnly_GkState(keeperTeam),
                "fixture must reach Rushing before the run is measured");

            for (int f = 0; f < 120; f++)
            {
                engine.TestOnly_DriveGkHeadingPhysics();
            }

            float endFromGoalM = Mathf.Abs(engine.AgentView(keeper).Position.x - GoalX(keeperTeam));
            Assert.Greater(endFromGoalM, startFromGoalM + 1.0f,
                "a committed rush must physically advance the keeper away from his own goal line — " +
                "the whole point of W1, and true of neither the pre-W1 engine nor one whose rush " +
                "position write-back is dropped");

            Assert.AreNotEqual(GoalkeeperState.Rushing, engine.TestOnly_GkState(keeperTeam),
                "the run must END once the locked target is reached (ERR-011-009) — for a LOOSE ball " +
                "neither the 1v1 nor the smother trigger can fire and F-08 needs a possessor, so " +
                "without the Reached row the keeper stands over the ball for the rest of the match");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void ComposedEngine_SweptLooseBall_DoesNotReArmTheRushForever(int keeperTeam)
        {
            // W1 AR-1 (H): ERR-011-009 ended the STALL; this pins that it did not become a CHURN.
            // A keeper who sweeps a loose ball cannot collect it (§5.Z.15/16 excludes him by role), so
            // he ends the run standing over a live ball with every arming condition still true. Without
            // RushArmed's minimum-run guard he re-commits a zero-length rush every third tactical tick
            // — Set -> commit -> Anticipate -> Rushing -> reached -> Recovering -> Set — pinned to a
            // dead ball, publishing a RushPhase.Reached each cycle and never holding Anticipate long
            // enough for the §3.3.6 dive gate to fire.
            var engine = new MatchEngine(0x0F1E2D3C4B5A6978UL);
            PlaceFixture(engine, keeperTeam, coverOutFromGoalM: -1f);

            // 30 tactical strides, each followed by a stride's worth of physics frames — long enough
            // for ten full oscillation cycles if the guard is missing.
            for (int t = 0; t < 30; t++)
            {
                engine.TestOnly_DriveGkHeadingTactical();
                for (int f = 0; f < GoalkeeperConstants.FramesPerTacticalTick; f++)
                {
                    engine.TestOnly_DriveGkHeadingPhysics();
                }
            }

            Assert.AreEqual(1, engine.TestOnly_RushCommitCount,
                "exactly one rush for one ball: once the keeper has arrived, the run he would commit " +
                "to is zero-length and RushArmed must refuse it");
        }

        // ── Dismissal and slot ownership ─────────────────────────────────────────────────

        /// <summary>W1 AR-2 (M): the AR-1 sent-off fix shipped with no lock at all. A red-carded keeper is
        /// frozen by <c>_commands[i] = Stop</c>, which governs MOVEMENT — and #11's Rushing branch writes
        /// the position itself, after movement, so before the fix a dismissed keeper kept sprinting to his
        /// locked target. Nothing asserted it then and nothing would catch its return.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void ComposedEngine_KeeperSentOffMidRush_StopsDead(int keeperTeam)
        {
            var engine = new MatchEngine(0x0F1E2D3C4B5A6978UL);
            PlaceFixture(engine, keeperTeam, coverOutFromGoalM: -1f);

            int keeper = KeeperAgentId(engine, keeperTeam);
            engine.TestOnly_DriveGkHeadingTactical();
            engine.TestOnly_DriveGkHeadingTactical();
            engine.TestOnly_DriveGkHeadingTactical();
            Assert.AreEqual(GoalkeeperState.Rushing, engine.TestOnly_GkState(keeperTeam),
                "fixture must be mid-rush before the red card");

            engine.TestOnly_SetIsSentOff(keeper, true);
            Vector2 atDismissal = engine.AgentView(keeper).Position;

            for (int f = 0; f < 60; f++)
            {
                engine.TestOnly_DriveGkHeadingPhysics();
            }

            Assert.AreEqual(atDismissal.x, engine.AgentView(keeper).Position.x, 1e-4f,
                "a sent-off keeper is off the pitch; nothing may move him, least of all a rush he can no " +
                "longer be running");
            Assert.AreEqual(atDismissal.y, engine.AgentView(keeper).Position.y, 1e-4f);
        }

        /// <summary>
        /// W1 AR-2 (H): #11 indexes every per-keeper array by gkIndex — the TEAM — while the engine keys
        /// identity by roster slot, so the occupant can change mid-match. The real sequence: keeper sent
        /// off, bench keeper on in a DIFFERENT slot. Without <c>ResetSlot</c> the substitute inherits the
        /// dismissed keeper's live RushIntent, target still locked (KD-15) to a point chosen for a player
        /// who has left the field, and the <c>Set → Rushing</c> row launches him at it — his first act on
        /// the pitch is to sprint somewhere nobody chose for him.
        /// </summary>
        [TestCase(0)]
        [TestCase(1)]
        public void ComposedEngine_SubstituteKeeper_DoesNotInheritTheDismissedKeepersRush(int keeperTeam)
        {
            var engine = new MatchEngine(0x0F1E2D3C4B5A6978UL);
            PlaceFixture(engine, keeperTeam, coverOutFromGoalM: -1f);

            int keeper = KeeperAgentId(engine, keeperTeam);
            engine.TestOnly_DriveGkHeadingTactical();
            engine.TestOnly_DriveGkHeadingTactical();
            engine.TestOnly_DriveGkHeadingTactical();
            Assert.AreEqual(GoalkeeperState.Rushing, engine.TestOnly_GkState(keeperTeam),
                "the inherited state has to be a live rush for the test to mean anything");
            Assert.IsTrue(engine.TestOnly_GoalkeeperState.RushIntentActive[keeperTeam],
                "and the intent has to be armed");

            // Red card, then the reserve keeper on for an outfielder — SubstitutePlayer refuses the
            // sent-off slot itself, so this is the only shape the real sequence can take.
            engine.TestOnly_SetIsSentOff(keeper, true);
            engine.TestOnly_SetBenchSlot(keeperTeam, 0, isGoalkeeper: true);

            int outfielder = -1;
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (i != keeper && engine.AgentTeamId(i) == keeperTeam && !engine.AgentIsGoalkeeper(i))
                {
                    outfielder = i;
                    break;
                }
            }
            Assert.GreaterOrEqual(outfielder, 0, "fixture needs an outfielder to withdraw");

            engine.SubstitutePlayer(keeperTeam, outfielder, 0, SubstitutionReason.Tactical);
            Vector2 onArrival = engine.AgentView(outfielder).Position;

            for (int f = 0; f < 60; f++)
            {
                engine.TestOnly_DriveGkHeadingPhysics();
            }

            Assert.AreEqual(onArrival.x, engine.AgentView(outfielder).Position.x, 1e-4f,
                "the substitute keeper must not be dragged along a run committed by the man he replaced");
            Assert.AreEqual(onArrival.y, engine.AgentView(outfielder).Position.y, 1e-4f);
            Assert.IsFalse(engine.TestOnly_GoalkeeperState.RushIntentActive[keeperTeam],
                "the slot belongs to whoever occupies it — a new occupant starts with no intent armed");
            Assert.AreEqual(GoalkeeperState.Resting, engine.TestOnly_GkState(keeperTeam),
                "and at the machine's own default state, not the dismissed keeper's frozen one");
        }

        // ── Cross-catalogue invariant ────────────────────────────────────────────────────

        [Test]
        public void GkRushCommitment_MustExceedTheStateMachinesRushCommitThreshold()
        {
            // W1 AR-1 (L): the engine writes GkRushCommitment into every RushIntent and #11's
            // Set/Anticipate -> Rushing rows ignore any intent at or below RushCommitThreshold. The two
            // live in DIFFERENT catalogues and are independently config-overridable, and the requirement
            // was recorded only in a doc comment — so a config file could silently return the rush
            // subsystem to the state W1 found it in (built, tested, never fires) with nothing failing.
            Assert.Greater(
                MatchEngineConstants.GkRushCommitment,
                GoalkeeperConstants.RushCommitThreshold,
                "a RushIntent at or below RushCommitThreshold is ignored by #11's state machine — " +
                "the whole trigger goes silently dead");
        }

        // ── Fixture ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds the composed scenario: the keeper 2 m off his line, a stationary loose ball 6 m out,
        /// and every team-mate parked at the halfway line — except one placed
        /// <paramref name="coverOutFromGoalM"/> metres in front of the defended goal when that value is
        /// non-negative, i.e. goal-side of the ball and on the shot line.
        ///
        /// <para>6 m is inside <c>RushCommitBaseM</c>, the §3.7.0 distance for a keeper with zero
        /// OneVsOne and zero Composure, so the fixture arms for ANY attribute set and does not depend on
        /// what roster generation produced. The ball is stationary, so <c>SaveArmed</c> (which needs
        /// ≥ 3 m/s) cannot fire and take priority.</para>
        /// </summary>
        private static void PlaceFixture(MatchEngine engine, int keeperTeam, float coverOutFromGoalM)
        {
            int keeper = KeeperAgentId(engine, keeperTeam);
            Assert.GreaterOrEqual(keeper, 0, "fixture needs a keeper on the pitch");

            engine.TestOnly_SetAgent(keeper, AgentAt(OutFromGoal(keeperTeam, 2f), PitchY));

            bool coverPlaced = coverOutFromGoalM < 0f;
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (i == keeper || engine.AgentTeamId(i) != keeperTeam)
                {
                    continue;
                }
                if (!coverPlaced)
                {
                    engine.TestOnly_SetAgent(i, AgentAt(OutFromGoal(keeperTeam, coverOutFromGoalM), PitchY));
                    coverPlaced = true;
                }
                else
                {
                    engine.TestOnly_SetAgent(i, AgentAt(MatchEngineConstants.PITCH_LENGTH_M * 0.5f, PitchY));
                }
            }
            Assert.IsTrue(coverPlaced, "fixture needs at least one outfielder");

            engine.TestOnly_ForceBallLoose(
                new Vector3(OutFromGoal(keeperTeam, 6f), PitchY, 0.11f), Vector3.zero);
        }

        private static AgentState AgentAt(float x, float y)
        {
            var a = new AgentState();
            a.Position = new Vector2(x, y);
            return a;
        }

        private static int KeeperAgentId(MatchEngine engine, int teamId)
        {
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (engine.AgentIsGoalkeeper(i) && engine.AgentTeamId(i) == teamId)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-04 | —      | Initial. Wiring backlog W1: RushArmed's arming and refusing    |
// |         |            |        | cases (every one mirrored home and away), the goal-side cover  |
// |         |            |        | test — a CHASING defender is not cover, so the keeper still    |
// |         |            |        | comes out — the per-keeper §3.7.0 commit distance reaching the  |
// |         |            |        | trigger, the intercept solve including the receding-clearance   |
// |         |            |        | no-solution guard, and the composed predicate → commit →       |
// |         |            |        | Rushing chain through a real MatchEngine.                      |
// | 1.1     | 2026-08-04 | —      | W1 AR-1. + the composed DISPLACEMENT test (H/A): the prior     |
// |         |            |        | composed locks stopped at GkState == Rushing, which is true of  |
// |         |            |        | an engine whose rush position write-back is dropped — the #11   |
// |         |            |        | v1.4 H-2 defect — so nothing proved the keeper leaves his line. |
// |         |            |        | + the re-arm lock (H/A): 30 strides over a swept loose ball     |
// |         |            |        | must produce exactly ONE commit, pinning that ERR-011-009's     |
// |         |            |        | end-of-run did not turn the stall into a 3-tick churn. + the    |
// |         |            |        | cross-catalogue GkRushCommitment > RushCommitThreshold          |
// |         |            |        | invariant, which was recorded only in a doc comment.           |
// | 1.2     | 2026-08-04 | —      | W1 AR-2. + the dismissal lock (H/A): AR-1's sent-off fix       |
// |         |            |        | shipped with nothing asserting it, so its return would be      |
// |         |            |        | silent. + the substitute-keeper lock (H/A): red card, bench    |
// |         |            |        | keeper on for an outfielder, and the incoming keeper must NOT  |
// |         |            |        | be dragged along the run his predecessor committed — #11 keys  |
// |         |            |        | its state by team, the engine keys identity by roster slot,    |
// |         |            |        | and the slot changed hands.                                    |
#endregion
