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
#endregion
